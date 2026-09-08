using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Data.Common;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Helpers.RDBMSHelpers.EntityHelpers;
using static TheTechIdea.Beep.Utils.Util;

namespace TheTechIdea.Beep.DataBase
{
    public partial class RDBSource : IRDBSource
    {
        #region "Error Handling Helpers"

        /// <summary>
        /// Handles database operation errors with consistent logging and error object assignment.
        /// </summary>
        /// <param name="ex">The exception that occurred.</param>
        /// <param name="entityName">The entity name involved in the operation.</param>
        /// <param name="operation">The operation being performed (Insert, Update, Delete, etc.).</param>
        /// <param name="sqlCommand">Optional SQL command text for logging.</param>
        private void HandleDatabaseError(Exception ex, string entityName, string operation, string sqlCommand = null)
        {
            string errorMsg = $"Failed to {operation} record in {entityName}: {ex.Message}";

            // Null-guarded throughout: this is the class's error handler, and an error handler that
            // throws replaces the real diagnostic with a NullReferenceException from inside itself.
            // The constructor validates neither `per` nor `pDMEEditor`, so both can legitimately be
            // absent here.
            if (ErrorObject != null)
            {
                ErrorObject.Ex = ex;
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = errorMsg;
            }

            if (DMEEditor?.ErrorObject != null)
            {
                DMEEditor.ErrorObject.Message = errorMsg;
                DMEEditor.ErrorObject.Flag = Errors.Failed;
            }

            string logMessage = string.IsNullOrEmpty(sqlCommand)
                ? errorMsg
                : $"{errorMsg} | SQL: {sqlCommand}";

            DMEEditor?.AddLogMessage("Beep", logMessage, DateTime.Now, 0, entityName, Errors.Failed);
        }

        /// <summary>
        /// Records a failure that has no exception behind it — a closed connection, a driver type
        /// that could not be resolved, a command that could not be created.
        /// </summary>
        /// <remarks>
        /// Never report a failure through <c>DMEEditor.AddLogMessage</c> alone. That method returns
        /// BEFORE it assigns <c>ErrorObject.Flag</c> when no logger is attached (see
        /// <c>DMEEditor.AddLogMessage</c> in BeepDM), so in any headless or test configuration the
        /// caller reads back the optimistic <see cref="Errors.Ok"/> that most methods here set on
        /// entry, and treats a failed operation as a successful one. Setting the flag here first
        /// makes the report independent of whether logging happens to be configured.
        /// </remarks>
        /// <param name="message">What went wrong, in caller-facing terms.</param>
        /// <param name="entityName">Entity involved, for the log context. Defaults to the datasource name.</param>
        private void SetFailure(string message, string entityName = null)
        {
            if (ErrorObject != null)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = message;
            }

            // ErrorObject and DMEEditor.ErrorObject are usually the same instance — the standard
            // creation path passes the engine's own error object as the constructor's `per`. That
            // is an aliasing coincidence, not a contract, so set both.
            if (DMEEditor?.ErrorObject != null)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = message;
            }

            DMEEditor?.AddLogMessage("Fail", message, DateTime.Now, -1, entityName ?? DatasourceName, Errors.Failed);
        }

        /// <summary>
        /// Records the outcome of a keyed single-row write from the row count the server reported.
        /// </summary>
        /// <remarks>
        /// A keyed UPDATE or DELETE that affects no row did not do what the caller asked, and saying
        /// so is what makes a silently-ineffective write visible. These paths previously logged
        /// <see cref="Errors.Failed"/> but left the flag on the optimistic <see cref="Errors.Ok"/>
        /// set at method entry — so the outcome depended on whether a logger happened to be attached
        /// (with one, <c>AddLogMessage</c> set the flag; without one it did nothing).
        ///
        /// The MySQL/MariaDB carve-out is not a hedge: those engines report 0 affected rows for an
        /// UPDATE that MATCHED a row but changed nothing, unless the connection enables
        /// CLIENT_FOUND_ROWS. Treating that as failure would make every re-save of an unchanged
        /// entity fail. DELETE carries no such ambiguity on any engine.
        /// </remarks>
        /// <param name="rowsAffected">Rows the provider reported.</param>
        /// <param name="entityName">Entity written to.</param>
        /// <param name="operation">Past-tense verb for the message, e.g. "updated".</param>
        /// <param name="isUpdate">True for UPDATE, false for DELETE.</param>
        private void ReportAffectedRows(int rowsAffected, string entityName, string operation, bool isUpdate)
        {
            if (rowsAffected > 0)
            {
                SetSuccess();
                return;
            }

            bool zeroIsAmbiguous = isUpdate &&
                (DatasourceType == DataSourceType.Mysql || DatasourceType == DataSourceType.MariaDB);

            if (zeroIsAmbiguous)
            {
                SetSuccess($"No rows reported changed in {entityName}. On {DatasourceType} a zero count " +
                           $"also means the row matched but was already up to date, so this is not " +
                           $"treated as a failure.");
                return;
            }

            SetFailure($"No records {operation} in {entityName}: the key matched no row.", entityName);
        }

        /// <summary>
        /// Records success explicitly, so a caller cannot read back a stale <see cref="Errors.Failed"/>
        /// left on the shared error object by an earlier, unrelated operation.
        /// </summary>
        /// <param name="message">Optional message to record alongside the flag.</param>
        private void SetSuccess(string message = null)
        {
            if (ErrorObject != null)
            {
                ErrorObject.Flag = Errors.Ok;
                if (message != null) ErrorObject.Message = message;
            }

            if (DMEEditor?.ErrorObject != null)
            {
                DMEEditor.ErrorObject.Flag = Errors.Ok;
                if (message != null) DMEEditor.ErrorObject.Message = message;
            }
        }

        #endregion

        #region "RDBSSource Database Methods"

        private int GetCtorForAdapter(List<ConstructorInfo> ls)
        {
            if (ls == null || ls.Count == 0)
                return -1;

            int i = 0;
            foreach (ConstructorInfo c in ls)
            {
                ParameterInfo[] d = c.GetParameters();
                if (d.Length == 2)
                {
                    if (d[0].ParameterType == typeof(string))
                    {
                        if (d[1].ParameterType != typeof(string))
                        {
                            return i;
                        }
                    }
                }
                i += 1;
            }
            // No matching constructor found — return 0 as safe default
            return 0;
        }

        private int GetCtorForCommandBuilder(List<ConstructorInfo> ls)
        {
            if (ls == null || ls.Count == 0)
                return -1;

            int i = 0;
            foreach (ConstructorInfo c in ls)
            {
                ParameterInfo[] d = c.GetParameters();
                if (d.Length == 1)
                {
                    return i;
                }
                i += 1;
            }
            // No matching constructor found — return 0 as safe default
            return 0;
        }
        public virtual IDbCommand GetDataCommand()
        {
            IDbCommand cmd = null;
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (Dataconnection.ConnectionStatus == ConnectionState.Open)
                {
                    cmd = RDBMSConnection.DbConn.CreateCommand();

                    // Carry the open transaction. Providers that track a pending
                    // local transaction reject any command that does not:
                    // "ExecuteNonQuery requires the command to have a transaction
                    // when the connection assigned to the command is in a pending
                    // local transaction." Every command in this class comes from
                    // here, so this is the one place it has to happen.
                    // (2026-08-03)
                    var tx = ActiveTransaction;
                    if (tx != null) cmd.Transaction = tx;

                    ConfigureCommand(cmd);
                    SetSuccess();
                }
                else
                {
                    cmd = null;

                    // Flag the failure explicitly. Callers cannot distinguish "no command" from
                    // "command created" by the return value alone — eleven of them do not check it
                    // at all — so a closed connection used to surface as an NRE one line later with
                    // ErrorObject.Flag still reading Ok.
                    SetFailure($"Cannot create a command: the connection to {DatasourceName} is {Dataconnection?.ConnectionStatus.ToString() ?? "unavailable"}, not Open.");
                }
            }
            catch (Exception ex)
            {
                cmd = null;
                HandleDatabaseError(ex, DatasourceName, "create a database command");
            }
            return cmd;
        }

        /// <summary>
        /// Provider-specific command configuration hook, invoked once per command
        /// by <see cref="GetDataCommand"/>. The base implementation is a no-op;
        /// overrides (e.g. Oracle's BindByName) plug in here.
        /// </summary>
        protected virtual void ConfigureCommand(IDbCommand command)
        {
        }

        public virtual IDbDataAdapter GetDataAdapter(string Sql, List<AppFilter> Filter = null)
        {
            IDbDataAdapter adp = null;

            try
            {
                // Both services are required and neither has a local substitute: Utilfunction
                // resolves the driver config, assemblyHandler loads the provider's adapter and
                // command-builder types by name. Unguarded, a missing one surfaced as
                // "Object reference not set" from inside adapter construction.
                if (DMEEditor?.Utilfunction == null || DMEEditor?.assemblyHandler == null)
                {
                    SetFailure($"Cannot build a data adapter for {DatasourceName}: the engine's driver-resolution and assembly-loading services are not available.");
                    return null;
                }

                ConnectionDriversConfig driversConfig = DMEEditor.Utilfunction.LinkConnection2Drivers(Dataconnection.ConnectionProp);
                string adtype = Dataconnection.DataSourceDriver.AdapterType;
                string cmdtype = Dataconnection.DataSourceDriver.CommandBuilderType;
                string cmdbuildername = driversConfig.CommandBuilderType;
                if (string.IsNullOrEmpty(cmdbuildername))
                {
                    // Was a bare `return null` with no diagnostic at all.
                    SetFailure($"No CommandBuilder type is configured for the driver behind adapter '{adtype}'.");
                    return null;
                }
                Type? adcbuilderType = DMEEditor.assemblyHandler.GetType(cmdbuildername);
                if (adcbuilderType == null)
                {
                    SetFailure($"CommandBuilder type '{cmdbuildername}' not found");
                    return null;
                }

                var adapterInstance = DMEEditor.assemblyHandler.GetInstance(adtype);
                var builderInstance = DMEEditor.assemblyHandler.GetInstance(cmdbuildername);
                if (adapterInstance == null || builderInstance == null)
                {
                    SetFailure("Failed to create adapter or command builder instance");
                    return null;
                }

                List<ConstructorInfo> lsc = adapterInstance.GetType().GetConstructors().ToList();
                List<ConstructorInfo> lsc2 = builderInstance.GetType().GetConstructors().ToList();

                int adapterIdx = GetCtorForAdapter(lsc);
                int builderIdx = GetCtorForCommandBuilder(adcbuilderType.GetConstructors().ToList());
                if (adapterIdx < 0 || builderIdx < 0 || adapterIdx >= lsc.Count || builderIdx >= lsc2.Count)
                {
                    SetFailure("No suitable constructor found for adapter or command builder");
                    return null;
                }

                ConstructorInfo ctor = lsc[adapterIdx];
                ConstructorInfo BuilderConstructer = lsc2[builderIdx];
                ObjectActivator<IDbDataAdapter> adpActivator = GetActivator<IDbDataAdapter>(ctor);
                ObjectActivator<DbCommandBuilder> cmdbuilderActivator = GetActivator<DbCommandBuilder>(BuilderConstructer);

                // create an instance:
                adp = (IDbDataAdapter)adpActivator(Sql, RDBMSConnection.DbConn);
                try
                {
                    DbCommandBuilder cmdBuilder = cmdbuilderActivator(adp);
                    if (Filter != null)
                    {
                        if (Filter.Where(p => !string.IsNullOrEmpty(p.FilterValue) && !string.IsNullOrWhiteSpace(p.FilterValue) && !string.IsNullOrEmpty(p.Operator) && !string.IsNullOrWhiteSpace(p.Operator)).Any())
                        {

                            foreach (AppFilter item in Filter.Where(p => !string.IsNullOrEmpty(p.FilterValue) && !string.IsNullOrWhiteSpace(p.FilterValue)))
                            {

                                IDbDataParameter parameter = adp.SelectCommand.CreateParameter();
                                string dr = Filter.Where(i => i.FieldName == item.FieldName).FirstOrDefault().FilterValue;
                                parameter.ParameterName = "p_" + item.FieldName;
                                if (item.valueType == "System.DateTime")
                                {
                                    parameter.DbType = DbType.DateTime;
                                    parameter.Value = DateTime.Parse(dr).ToShortDateString();

                                }
                                else
                                { parameter.Value = dr; }

                                if (item.Operator.ToLower() == "between")
                                {
                                    IDbDataParameter parameter1 = adp.SelectCommand.CreateParameter();
                                    parameter1.ParameterName = "p_" + item.FieldName + "1";
                                    parameter1.DbType = DbType.DateTime;
                                    string dr1 = Filter.Where(i => i.FieldName == item.FieldName).FirstOrDefault().FilterValue1;
                                    parameter1.Value = DateTime.Parse(dr1).ToShortDateString();
                                    adp.SelectCommand.Parameters.Add(parameter1);
                                }

                                //  parameter.DbType = TypeToDbType(tb.Columns[item.FieldName].DataType);
                                adp.SelectCommand.Parameters.Add(parameter);

                            }

                        }
                    }
                    adp.InsertCommand = cmdBuilder.GetInsertCommand(true);
                    adp.UpdateCommand = cmdBuilder.GetUpdateCommand(true);
                    adp.DeleteCommand = cmdBuilder.GetDeleteCommand(true);
                }
                catch (Exception ex)
                {
                    // Command-builder generation legitimately fails for any SELECT the provider
                    // cannot map back to a single keyed base table, and the adapter is still usable
                    // for Fill — so this is not a failure of the adapter's own contract and must not
                    // set the error flag. It was previously swallowed with the log line commented
                    // out, which left a later NullReferenceException on adp.Update() with nothing to
                    // attribute it to. Logged through Logger rather than AddLogMessage precisely so
                    // it records the diagnostic without moving ErrorObject.Flag.
                    Logger?.WriteLog($"GetDataAdapter: command-builder generation failed for {DatasourceName} " +
                                     $"({ex.Message}). Insert/Update/Delete commands are unavailable on this " +
                                     $"adapter; Fill still works. SQL=[{Sql}]");
                }

                adp.MissingSchemaAction = MissingSchemaAction.AddWithKey;
                adp.MissingMappingAction = MissingMappingAction.Passthrough;

                SetSuccess();
            }
            catch (Exception ex)
            {
                HandleDatabaseError(ex, DatasourceName, "create a data adapter", Sql);
                adp = null;
            }

            return adp;
        }

        private string GetUniqueParameterName(string baseName, HashSet<string> usedParameterNames)
        {
            string parameterName = "p_" + Regex.Replace(baseName, @"\s+", "_");
            string uniqueParameterName = parameterName;
            int counter = 1;

            while (usedParameterNames.Contains(uniqueParameterName))
            {
                uniqueParameterName = $"{parameterName}_{counter}";
                counter++;
            }

            usedParameterNames.Add(uniqueParameterName);
            return uniqueParameterName;
        }

        /// <summary>
        /// Renders a column name for use in generated SQL, quoting it when the dialect requires it.
        /// </summary>
        /// <remarks>
        /// This used to quote with <see cref="ColumnDelimiter"/>, whose base default is <c>"''"</c>
        /// and which six drivers override to <c>"'"</c>. Both render <c>Cust Id</c> as
        /// <c>'Cust Id'</c> — a STRING LITERAL, not an identifier. In a predicate that becomes
        /// <c>WHERE 'Cust Id' = @p</c>, which compares a constant to a parameter: it matches nothing,
        /// and before the K2 fix a zero-row write reported success. In an ORDER BY it is a constant
        /// sort key, so OFFSET/FETCH paging returned overlapping and missing rows. Only SQLite, with
        /// <c>"[]"</c>, produced a correct quoted identifier.
        ///
        /// <see cref="ColumnDelimiter"/> is left in place — drivers set it and it is part of
        /// <c>IDataSource</c> — but it is no longer the quoting mechanism.
        /// </remarks>
        public virtual string GetFieldName(string FieldName)
        {
            return QuoteIdentifier(FieldName);
        }

        /// <summary>
        /// Quotes an identifier with the characters the current dialect actually uses, but only when
        /// quoting is needed.
        /// </summary>
        /// <remarks>
        /// Deliberately conservative: a plain identifier that is not a reserved word is returned
        /// unchanged. Quoting unconditionally would be simpler, but on PostgreSQL and Oracle a quoted
        /// identifier is CASE-SENSITIVE while an unquoted one folds (to lower and upper
        /// respectively). Any hand-authored <c>EntityStructure</c> whose column case does not match
        /// the stored case works today precisely because the name goes out unquoted; quoting it would
        /// break it. Names that come from schema discovery already carry the stored case and are
        /// unaffected either way.
        ///
        /// So this quotes exactly the two cases that are broken without it: names that are not plain
        /// identifiers (a space, a dash, anything outside [A-Za-z0-9_], or a leading digit), and names
        /// that are reserved words in the dialect.
        /// </remarks>
        protected virtual string QuoteIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return identifier;

            string name = identifier.Trim();

            // Already delimited by the caller, or a qualified name — leave it alone.
            if (name.Length > 1 &&
                ((name[0] == '[' && name[name.Length - 1] == ']') ||
                 (name[0] == '"' && name[name.Length - 1] == '"') ||
                 (name[0] == '`' && name[name.Length - 1] == '`')))
            {
                return identifier;
            }

            if (!NeedsQuoting(name))
                return identifier;

            switch (DatasourceType)
            {
                case DataSourceType.SqlServer:
                case DataSourceType.AzureSQL:
                case DataSourceType.SqlCompact:
                case DataSourceType.VistaDB:
                    return "[" + name.Replace("]", "]]") + "]";

                case DataSourceType.Mysql:
                case DataSourceType.MariaDB:
                    return "`" + name.Replace("`", "``") + "`";

                default:
                    // ANSI double quotes: Oracle, PostgreSQL, SQLite, Firebird, DB2, Hana,
                    // Snowflake, Spanner, Presto/Trino, CockroachDB, DuckDB and the rest.
                    return "\"" + name.Replace("\"", "\"\"") + "\"";
            }
        }

        /// <summary>
        /// True when an identifier cannot be written bare in this dialect.
        /// </summary>
        private bool NeedsQuoting(string name)
        {
            if (!char.IsLetter(name[0]) && name[0] != '_')
                return true;

            foreach (char c in name)
            {
                if (!char.IsLetterOrDigit(c) && c != '_')
                    return true;
            }

            try
            {
                return DatabaseEntityReservedKeywordChecker.IsReservedKeyword(name, DatasourceType);
            }
            catch
            {
                // The keyword table is advisory. If it cannot answer for this dialect, fall back to
                // the character test above rather than failing the whole statement build.
                return false;
            }
        }

        /// <summary>
        /// Prefixes an entity name with the configured schema, when one applies.
        /// </summary>
        /// <remarks>
        /// For statements that are BUILT rather than rewritten. Six call sites interpolated
        /// <c>{ConnectionProp.SchemaName}{entityName}</c> straight into their SQL and produced
        /// <c>dboCustomers</c> — no separator — which breaks every bulk operation and, because two
        /// of them are the async read paths, paged and streaming reads too.
        ///
        /// The "schema differs from the user id" test mirrors <see cref="GetTableName"/>, so a
        /// statement built here qualifies exactly when a statement rewritten there would. Without
        /// that, bulk would qualify where CRUD does not and the two would address different objects
        /// again, which is the problem this is meant to remove.
        /// </remarks>
        protected virtual string QualifyWithSchema(string entityName)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                return entityName;

            // Already qualified by the caller — leave it alone.
            if (entityName.Contains("."))
                return entityName;

            string schemaName = Dataconnection?.ConnectionProp?.SchemaName;
            string userId = Dataconnection?.ConnectionProp?.UserID;

            if (string.IsNullOrWhiteSpace(schemaName))
                return entityName;

            if (!string.IsNullOrEmpty(userId) &&
                schemaName.Equals(userId, StringComparison.InvariantCultureIgnoreCase))
            {
                return entityName;
            }

            return schemaName.Trim() + "." + entityName;
        }

        public virtual string GetTableName(string querystring)
        {
            string schname = Dataconnection.ConnectionProp.SchemaName;
            string userid = Dataconnection.ConnectionProp.UserID;
            string schemastring = "";
            if (!string.IsNullOrEmpty(schname) && !schname.Equals(userid, StringComparison.InvariantCultureIgnoreCase))
            {
                if (schname.Length > 0)
                {
                    schemastring = schname + ".";
                }
            }
            else
                schemastring = "";
            if (querystring.IndexOf("select") > 0)
            {
                int frompos = querystring.IndexOf("from", StringComparison.InvariantCultureIgnoreCase);
                int wherepos = querystring.IndexOf("where", StringComparison.InvariantCultureIgnoreCase);
                if (wherepos == 0)
                {
                    wherepos = querystring.Length - 1;

                }

                int firstcharindex = querystring.IndexOf(' ', frompos);
                int lastcharindex = querystring.IndexOf(' ', firstcharindex + 2);
                string tablename = querystring.Substring(firstcharindex + 1, lastcharindex - firstcharindex - 1);
                querystring = querystring.Replace(' ' + tablename + ' ', $" {schemastring}{tablename} ");
            }
            else if (querystring.IndexOf("insert") >= 0)
            {
                int intopos = querystring.IndexOf("into", StringComparison.InvariantCultureIgnoreCase);
                string[] instokens = querystring.Split(' ');
                querystring = querystring.Replace(instokens[2], $" {schemastring}{instokens[2]} ");
            }
            else if (querystring.IndexOf("update") >= 0)
            {
                int setpos = querystring.IndexOf("set", StringComparison.InvariantCultureIgnoreCase);
                string[] uptokens = querystring.Split(' ');
                querystring = querystring.Replace(uptokens[1], $" {schemastring}{uptokens[1]} ");
            }
            else if (querystring.IndexOf("delete") >= 0)
            {
                int frompos = querystring.IndexOf("from", StringComparison.InvariantCultureIgnoreCase);
                string[] fromtokens = querystring.Split(' ');
                querystring = querystring.Replace(fromtokens[1], $" {schemastring}{fromtokens[2]} ");
            }

            return querystring;
        }
        #endregion
    }
}
