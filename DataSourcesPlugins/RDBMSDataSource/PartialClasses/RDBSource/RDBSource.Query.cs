using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Helpers.RDBMSHelpers;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase.Helpers;
using TheTechIdea.Beep.Extensions;

namespace TheTechIdea.Beep.DataBase
{
    public partial class RDBSource : IRDBSource
    {
        #region "Repo Methods"
        /// <summary>
        /// Executes a SQL query asynchronously and retrieves the first column of the first row in the result set.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <returns>The scalar value as a double. Returns 0.0 if the query fails or doesn't return a valid double.</returns>
        /// <remarks>
        /// This method uses true async/await when the underlying provider supports it (DbCommand),
        /// falling back to Task.Run for legacy IDbCommand-only providers.
        /// </remarks>
        public virtual async Task<double> GetScalarAsync(string query)
        {
            ErrorObject.Flag = Errors.Ok;

            try
            {
                using (var command = GetDataCommand())
                {
                    // GetDataCommand returns null on a closed connection. Without this the NRE
                    // below was caught and reported as a query error, hiding the real cause; and
                    // because 0.0 is also a legitimate result, the caller had no way to tell a
                    // failed scalar from a genuine zero.
                    if (command == null)
                        return 0.0;

                    command.CommandText = query;

                    // Try to use async if DbCommand is available
                    if (command is System.Data.Common.DbCommand dbCommand)
                    {
                        var result = await dbCommand.ExecuteScalarAsync().ConfigureAwait(false);
                        if (result != null && result != DBNull.Value)
                        {
                            SetSuccess();
                            return Convert.ToDouble(result);
                        }
                    }
                    else
                    {
                        // Fallback for IDbCommand-only providers
                        var result = command.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            SetSuccess();
                            return Convert.ToDouble(result);
                        }
                    }
                }

                // Executed cleanly but produced no scalar. Distinct from a failure, and distinct
                // from a real 0 — say so rather than leaving the caller to guess.
                SetSuccess($"Scalar query returned no value: {query}");
            }
            catch (Exception ex)
            {
                HandleDatabaseError(ex, DatasourceName, "execute a scalar query", query);
            }

            return 0.0;
        }

        /// <summary>
        /// Synchronously retrieves a single scalar value from the database.
        /// </summary>
        /// <param name="query">The SQL query to be executed.</param>
        /// <returns>A task representing the asynchronous operation, resulting in the scalar value.</returns>
        public virtual double GetScalar(string query)
        {
            ErrorObject.Flag = Errors.Ok;

            try
            {
                // Assuming you have a database connection and command objects.

                using (var command = GetDataCommand())
                {
                    // See GetScalarAsync: null command means the connection is closed, and 0.0 is
                    // indistinguishable from a real result, so the flag has to carry the outcome.
                    if (command == null)
                        return 0.0;

                    command.CommandText = query;
                    using (IDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // GetValue + Convert.ToDouble is provider- and type-safe:
                            // GetDecimal(0) throws on any non-decimal scalar (an
                            // integer identity, a string, a timestamp). Matches
                            // GetScalarAsync's behaviour.
                            var result = reader.GetValue(0);
                            SetSuccess();
                            return result == null || result == DBNull.Value
                                ? 0.0
                                : Convert.ToDouble(result);
                        }
                    }
                }

                // Executed cleanly, no row. Not a failure, but not a real zero either.
                SetSuccess($"Scalar query returned no rows: {query}");
            }
            catch (Exception ex)
            {
                HandleDatabaseError(ex, DatasourceName, "execute a scalar query", query);
            }

            // Return a default value or throw an exception if the query failed.
            return 0.0; // You can change this default value as needed.
        }
        /// <summary>
        /// Executes a SQL command that does not return a result set.
        /// </summary>
        /// <param name="sql">The SQL command to execute.</param>
        /// <returns>An IErrorsInfo object indicating the success or failure of the operation.</returns>
        /// <remarks>
        /// Use this method for SQL commands like INSERT, UPDATE, DELETE, etc.
        /// </remarks>
        public virtual IErrorsInfo ExecuteSql(string sql)
        {
            ErrorObject.Flag = Errors.Ok;
            
            // Guard against null/empty SQL to prevent NullReferenceException in database drivers
            if (string.IsNullOrWhiteSpace(sql))
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = "SQL command is null or empty - cannot execute";
                DMEEditor?.AddLogMessage("Fail", "ExecuteSql called with null or empty SQL", DateTime.Now, -1, null, Errors.Failed);
                return ErrorObject;
            }
            
            // CurrentSql = sql;
            IDbCommand cmd = GetDataCommand();
            if (cmd != null)
            {
                try
                {
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                    SetSuccess();
                }
                catch (Exception ex)
                {
                    // DMEEditor and DMEEditor.ErrorObject were dereferenced unguarded here, on the
                    // one path that runs when a statement fails. With no editor attached, a failing
                    // statement threw NullReferenceException out of the method whose whole job is to
                    // report that failure -- replacing the real error with an unrelated one. This is
                    // what SetFailure is for: it null-guards both error objects and logs.
                    SetFailure($" Could not run Script - {sql} -" + ex.Message);
                }
                finally
                {
                    // Was disposed separately in the try and the catch, so a throw from
                    // `cmd.CommandText = sql` — which some providers validate — leaked the command.
                    cmd.Dispose();
                }
            }
            else
            {
                // There was no `else` here. GetDataCommand returns null whenever the connection is
                // not open, so this method returned the Errors.Ok set on line 123 having executed
                // nothing. CreateEntityAs checks that flag and logged "Entity created successfully"
                // for a CREATE TABLE that never ran; RunScript reported the same for un-executed
                // DDL; InMemoryRDBSource treated it as proof a table had been truncated and reloaded
                // data on top of rows that were still there.
                SetFailure($"Could not run Script - {sql} - no command could be created " +
                           $"(the connection to {DatasourceName} is not open).");
            }

            return ErrorObject;
        }
        /// <summary>
        /// Executes a SQL query and returns the result set.
        /// </summary>
        /// <param name="qrystr">The SQL query string.</param>
        /// <returns>A DataTable containing the query results or null if an error occurs.</returns>
        /// <remarks>
        /// This method is suitable for queries that return multiple rows.
        /// </remarks>
        public virtual IEnumerable<object> RunQuery(string qrystr)
        {
            ErrorObject.Flag = Errors.Ok;

            try
            {
                if (string.IsNullOrWhiteSpace(qrystr))
                {
                    // An empty result is what a query with no rows also returns, so these paths have
                    // to set the flag or the caller cannot tell "nothing matched" from "nothing ran".
                    SetFailure("RunQuery: query string is null or empty");
                    return Enumerable.Empty<object>();
                }

                if (Dataconnection.ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                using (var cmd = GetDataCommand())
                {
                    if (cmd == null)
                    {
                        SetFailure($"RunQuery: failed to create data command (the connection to {DatasourceName} is not open)");
                        return Enumerable.Empty<object>();
                    }

                    cmd.CommandText = qrystr;

                    using (var reader = cmd.ExecuteReader(CommandBehavior.Default))
                    {
                        var dt = new DataTable();
                        dt.Load(reader);
                        SetSuccess();
                        return dt.AsEnumerable().Select(row => row.ItemArray);
                    }
                }
            }
            catch (Exception ex)
            {
                HandleDatabaseError(ex, DatasourceName, "execute a query", qrystr);
                return Enumerable.Empty<object>();
            }
        }

        // The dynamic query builder that used to live here has been removed.
        //
        // BuildQuery, BuildQueryInternal, ParseQueryComponents, FindNextClausePosition,
        // GetSchemaPrefix, BuildWhereClause, IsValidFilter, FormatFilterCondition,
        // SanitizeParameterName and AppendClauseIfExists formed one mutually-referential cluster
        // whose only entry point, BuildQuery, had no callers anywhere in the solution — roughly
        // 350 lines of unreachable code. It was also the sole remaining caller of GetTableName and
        // of the query-string cache in RDBSource.Cache.cs, which is why both are now unused.
        //
        // The live query path is GetEntity below, which builds its SQL through
        // BuildSelectQueryDefinition (BeepDM's DataSourceAppFilterExtensions) and binds every
        // filter value as a parameter. Do not resurrect this cluster: it lowercased whole
        // statements, detected clauses with Contains("where") and no word boundary, and
        // concatenated AppFilter.FieldName and Operator straight into the SQL.


        /// <summary>
        /// Retrieves data for a specified entity from the database, with the option to apply filters.
        /// </summary>
        /// <param name="EntityName">The name of the entity (table) to retrieve data from.</param>
        /// <param name="Filter">A list of filters to apply to the query.</param>
        /// <remarks>
        /// This method supports both direct table queries and custom queries. It uses dynamic SQL generation and can adapt to different database types. The method also converts the retrieved DataTable to a list of objects based on the entity's structure and type.
        /// </remarks>
        /// <returns>An object representing the data retrieved, which could be a list or another type based on the entity structure.</returns>
        /// <exception cref="Exception">Catches and logs any exceptions that occur during the data retrieval process.</exception>
        public virtual IEnumerable<object> GetEntity(string EntityName, List<AppFilter> Filter)
        {
            ErrorObject.Flag = Errors.Ok;
            string inname = string.Empty;
            string qrystr = "select * from ";

            // Determine base query (table name vs full select)
            if (!string.IsNullOrWhiteSpace(EntityName))
            {
                if (!EntityName.Contains("select", StringComparison.OrdinalIgnoreCase) &&
                    !EntityName.Contains("from", StringComparison.OrdinalIgnoreCase))
                {
                    qrystr = "select * from " + QualifyWithSchema(EntityName);
                    inname = EntityName;
                }
                else
                {
                    // A caller-supplied SELECT. GetTableName used to try to inject the schema into
                    // it by string surgery; the caller wrote the query and any qualification it
                    // needs, so leave it alone.
                    string[] stringSeparators = { " from ", " where ", " group by ", " order by " };
                    var sp = EntityName.ToLower().Split(stringSeparators, StringSplitOptions.None);
                    qrystr = EntityName;
                    if (sp.Length > 1)
                        inname = sp[1].Trim();
                }
            }

            // Allow custom query from metadata
            try
            {
                var ent = GetEntityStructure(inname);
                if (ent != null && !string.IsNullOrEmpty(ent.CustomBuildQuery))
                {
                    qrystr = ent.CustomBuildQuery;
                }
            }
            catch (Exception ex)
            {
                // Non-fatal: a missing or unreadable structure only costs the CustomBuildQuery
                // override, and the plain SELECT below still works. But it used to be discarded
                // entirely, so a broken structure looked identical to an entity that simply has no
                // custom query.
                Logger?.WriteLog($"Could not read the structure of '{inname}' while starting a streaming read on {DatasourceName}; using the default query. {ex.Message}");
            }

            var streamQueryDefinition = this.BuildSelectQueryDefinition(qrystr, Filter);

            if (Dataconnection.ConnectionStatus != ConnectionState.Open)
                Openconnection();

            IDbCommand cmd = null;
            IDataReader reader = null;

            // Prepare command & reader inside try (no yield here)
            try
            {
                cmd = GetDataCommand();
                if (cmd == null)
                    yield break;

                cmd.ApplyFilterQueryDefinition(streamQueryDefinition, this);

                reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess);
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                // Include the SQL and the parameter delimiter actually used. Without them a dialect
                // mismatch (a '$p_' parameter reaching SQL Server, say) is unattributable — the
                // message alone names neither the query nor the datasource that produced it.
                DMEEditor?.AddLogMessage("Fail",
                    $"Error preparing entity stream ({ex.Message}) | source={GetType().Name} " +
                    $"type={DatasourceType} delimiter='{ParameterDelimiter}' | sql=[{streamQueryDefinition?.QueryText}]",
                    DateTime.Now, 0, inname, Errors.Failed);
                if (reader != null) { try { reader.Close(); } catch { } }
                cmd?.Dispose();
                yield break;
            }

            // Resolve entity type for converting dictionary rows to typed objects
            Type entityType = null;
            PropertyInfo[] entityProperties = null;
            try
            {
                entityType = GetEntityType(inname);
                if (entityType != null)
                {
                    entityProperties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                }
            }
            catch (Exception ex)
            {
                // The fallback is real -- the loop below yields dictionaries when entityType is
                // null -- but callers expecting typed rows got them silently swapped for
                // dictionaries with nothing logged.
                Logger?.WriteLog($"Could not resolve an entity type for '{inname}' on {DatasourceName}; streaming rows as dictionaries instead. {ex.Message}");
            }

            // One entry per column that failed conversion during this read, so the log records the
            // problem once rather than once per row.
            var reportedConversionFailures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Streaming loop using DataStreamer helper
            using (cmd)
            {
                foreach (var row in DataBase.Helpers.DataStreamer.Stream(reader))
                {
                    if (entityType != null && entityProperties != null)
                    {
                        // Create typed object and populate from dictionary
                        var obj = Activator.CreateInstance(entityType);
                        foreach (var prop in entityProperties)
                        {
                            if (!prop.CanWrite) continue;

                            // Dictionary uses OrdinalIgnoreCase comparer, so TryGetValue handles case
                            if (row.TryGetValue(prop.Name, out var value) && value != null)
                            {
                                try
                                {
                                    Type targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                                    object convertedValue;

                                    if (targetType == value.GetType())
                                    {
                                        convertedValue = value;
                                    }
                                    else if (targetType == typeof(DateTime) && value is string dateStr)
                                    {
                                        convertedValue = DateTime.TryParse(dateStr, out var dt) ? dt : default(DateTime);
                                    }
                                    else if (targetType == typeof(Guid) && value is string guidStr)
                                    {
                                        convertedValue = Guid.TryParse(guidStr, out var g) ? g : Guid.Empty;
                                    }
                                    else if (targetType.IsEnum)
                                    {
                                        convertedValue = Enum.Parse(targetType, value.ToString(), true);
                                    }
                                    else
                                    {
                                        convertedValue = Convert.ChangeType(value, targetType);
                                    }

                                    prop.SetValue(obj, convertedValue);
                                }
                                catch (Exception convEx)
                                {
                                    // Report each problem column once per read, not once per row.
                                    //
                                    // This was a completely empty catch, so a column whose value
                                    // could not be converted silently produced the property's
                                    // default on EVERY row — a table of zeros and nulls that looked
                                    // like data. Logging per row would be unusable on a large read,
                                    // so the first failure for each column is recorded and the rest
                                    // suppressed.
                                    if (reportedConversionFailures.Add(prop.Name))
                                    {
                                        Logger?.WriteLog($"Column '{prop.Name}' could not be converted to " +
                                                         $"{prop.PropertyType.Name} while reading {EntityName} " +
                                                         $"({convEx.Message}); this property will be left at its " +
                                                         $"default for the affected rows.");
                                    }
                                }
                            }
                        }
                        yield return obj;
                    }
                    else
                    {
                        // Fallback: yield raw dictionary if entity type not resolved
                        yield return row;
                    }
                }
            }
        }
        /// <summary>
        /// Retrieves data for a specified entity from the database with pagination support.
        /// </summary>
        /// <param name="EntityName">The name of the entity (table) to retrieve data from.</param>
        /// <param name="Filter">A list of filters to apply to the query.</param>
        /// <param name="pageNumber">The page number to retrieve (1-based).</param>
        /// <param name="pageSize">The number of records per page.</param>
        /// <returns>A PagedResult object containing the data and pagination metadata.</returns>
        public virtual PagedResult GetEntity(string EntityName, List<AppFilter> Filter, int pageNumber, int pageSize)
        {
            ErrorObject.Flag = Errors.Ok;

            if (string.IsNullOrWhiteSpace(EntityName))
            {
                SetFailure("Entity name cannot be null or empty");
                return null;
            }
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 20;

            bool isCustomQuery = EntityName.Contains("select", StringComparison.OrdinalIgnoreCase)
                                 && EntityName.Contains("from", StringComparison.OrdinalIgnoreCase);

            string baseQuery = isCustomQuery ? EntityName : $"SELECT * FROM {EntityName}";
            string inname = EntityName;
            string entityForStruct = isCustomQuery ? ExtractFirstTableName(baseQuery) ?? EntityName : EntityName;

            // Try to resolve structure (may override with CustomBuildQuery)
            EntityStructure ent = null;
            try
            {
                ent = GetEntityStructure(entityForStruct);
                if (ent != null && !string.IsNullOrEmpty(ent.CustomBuildQuery))
                {
                    baseQuery = ent.CustomBuildQuery;
                    isCustomQuery = true;
                }
            }
            catch (Exception ex)
            {
                // Same as the streaming path: losing the structure costs CustomBuildQuery and the
                // primary key used for the ORDER BY that makes paging deterministic, so it is worth
                // a line in the log even though the read continues.
                Logger?.WriteLog($"Could not read the structure of '{entityForStruct}' while paging on {DatasourceName}; paging with the default query. {ex.Message}");
            }

            // Ensure deterministic ORDER BY for paging
            if (!baseQuery.Contains("order by", StringComparison.OrdinalIgnoreCase))
            {
                if (ent?.PrimaryKeys != null && ent.PrimaryKeys.Count > 0)
                {
                    baseQuery += $" ORDER BY {GetFieldName(ent.PrimaryKeys[0].FieldName)}";
                }
                else
                {
                    baseQuery += " ORDER BY 1";
                }
            }

            var queryDefinition = this.BuildSelectQueryDefinition(baseQuery, Filter);
            string filteredQuery = queryDefinition.QueryText;

            // Count query
            string countQuery;
            if (!isCustomQuery)
            {
                string tablePart = ExtractFirstTableName(baseQuery) ?? EntityName;
                countQuery = $"SELECT COUNT(*) FROM {tablePart}";
                string whereClause = ExtractWhereClause(filteredQuery);
                if (!string.IsNullOrEmpty(whereClause))
                {
                    countQuery += " " + whereClause;
                }
            }
            else
            {
                string noOrder = StripTrailingOrderBy(filteredQuery);
                countQuery = $"SELECT COUNT(*) FROM ({noOrder}) __q";
            }

            // Paging syntax (e.g. OFFSET/FETCH, LIMIT/OFFSET, etc.)
            string pagingSyntax = RDBMSHelper.GetPagingSyntax(DatasourceType, pageNumber, pageSize);
            string pagedQuery = $"{filteredQuery} {pagingSyntax}";

            int totalRecords = 0;
            if (Dataconnection.ConnectionStatus != ConnectionState.Open)
                Openconnection();

            // Execute count
            try
            {
                using var countCmd = GetDataCommand();
                if (countCmd == null)
                {
                    SetFailure($"Paged read of {EntityName}: could not create the count command (the connection to {DatasourceName} is not open).");
                    return null;
                }
                var countQueryDef = new AppFilterQueryDefinition
                {
                    QueryText = countQuery,
                    Parameters = queryDefinition.Parameters
                };
                countCmd.ApplyFilterQueryDefinition(countQueryDef, this);
                totalRecords = (int)Convert.ToInt64(countCmd.ExecuteScalar());
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Warning", $"Count failed: {ex.Message}", DateTime.Now, 0, EntityName, Errors.Warning);
            }

            // Resolve entity type for converting dictionary rows to typed objects
            Type pagedEntityType = null;
            PropertyInfo[] pagedEntityProperties = null;
            try
            {
                pagedEntityType = GetEntityType(entityForStruct);
                if (pagedEntityType != null)
                {
                    pagedEntityProperties = pagedEntityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                }
            }
            catch (Exception ex)
            {
                // See the streaming read: the fallback works, it just used to be invisible.
                Logger?.WriteLog($"Could not resolve an entity type for '{entityForStruct}' on {DatasourceName}; returning the page as dictionaries instead. {ex.Message}");
            }

            // Execute paged data query
            var rows = new List<object>();

            // One entry per column that failed conversion during this read; see the streaming read
            // above for why this is reported once rather than once per row.
            var reportedConversionFailures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var dataCmd = GetDataCommand();
                if (dataCmd == null)
                {
                    SetFailure($"Paged read of {EntityName}: could not create the data command (the connection to {DatasourceName} is not open).");
                    return null;
                }
                var pagedQueryDef = new AppFilterQueryDefinition
                {
                    QueryText = pagedQuery,
                    Parameters = queryDefinition.Parameters
                };
                dataCmd.ApplyFilterQueryDefinition(pagedQueryDef, this);

                using var reader = dataCmd.ExecuteReader(CommandBehavior.SequentialAccess);
                foreach (var row in DataBase.Helpers.DataStreamer.Stream(reader))
                {
                    if (pagedEntityType != null && pagedEntityProperties != null)
                    {
                        var obj = Activator.CreateInstance(pagedEntityType);
                        foreach (var prop in pagedEntityProperties)
                        {
                            if (!prop.CanWrite) continue;
                            if (row.TryGetValue(prop.Name, out var value) && value != null)
                            {
                                try
                                {
                                    Type targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                                    object convertedValue;

                                    if (targetType == value.GetType())
                                        convertedValue = value;
                                    else if (targetType == typeof(DateTime) && value is string dateStr)
                                        convertedValue = DateTime.TryParse(dateStr, out var dt) ? dt : default(DateTime);
                                    else if (targetType == typeof(Guid) && value is string guidStr)
                                        convertedValue = Guid.TryParse(guidStr, out var g) ? g : Guid.Empty;
                                    else if (targetType.IsEnum)
                                        convertedValue = Enum.Parse(targetType, value.ToString(), true);
                                    else
                                        convertedValue = Convert.ChangeType(value, targetType);

                                    prop.SetValue(obj, convertedValue);
                                }
                                catch (Exception convEx)
                                {
                                    // Report each problem column once per read, not once per row.
                                    //
                                    // This was a completely empty catch, so a column whose value
                                    // could not be converted silently produced the property's
                                    // default on EVERY row — a table of zeros and nulls that looked
                                    // like data. Logging per row would be unusable on a large read,
                                    // so the first failure for each column is recorded and the rest
                                    // suppressed.
                                    if (reportedConversionFailures.Add(prop.Name))
                                    {
                                        Logger?.WriteLog($"Column '{prop.Name}' could not be converted to " +
                                                         $"{prop.PropertyType.Name} while reading {EntityName} " +
                                                         $"({convEx.Message}); this property will be left at its " +
                                                         $"default for the affected rows.");
                                    }
                                }
                            }
                        }
                        rows.Add(obj);
                    }
                    else
                    {
                        rows.Add(row);
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Fail", $"Error executing paginated query: {ex.Message}", DateTime.Now, 0, EntityName, Errors.Failed);
                return null;
            }

            return new PagedResult
            {
                Data = rows,                 // IEnumerable<object>
                TotalRecords = totalRecords,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalRecords > 0 ? (int)Math.Ceiling((double)totalRecords / pageSize) : 0,
                HasNextPage = pageNumber * pageSize < totalRecords,
                HasPreviousPage = pageNumber > 1
            };
        }

        // Helper: remove trailing ORDER BY for wrapping in COUNT
        private static string StripTrailingOrderBy(string sql)
        {
            int idx = sql.LastIndexOf(" order by ", StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                // Ensure no closing parenthesis after (naive but sufficient here)
                string tail = sql.Substring(idx);
                if (!tail.Contains(")"))
                {
                    return sql.Substring(0, idx);
                }
            }
            return sql;
        }

        // Helper: crude extraction of first table identifier (used only for fallback scenarios)
        private static string ExtractFirstTableName(string sql)
        {
            try
            {
                var low = sql.ToLower();
                int fromIdx = low.IndexOf(" from ");
                if (fromIdx < 0) return null;
                int start = fromIdx + 6;
                int end = low.IndexOfAny(new[] { ' ', '\r', '\n', '\t', ',' }, start);
                if (end < 0) end = sql.Length;
                string token = sql.Substring(start, end - start).Trim();
                // Remove schema alias patterns
                if (token.Contains(")")) return null;
                if (token.Equals("select", StringComparison.OrdinalIgnoreCase)) return null;
                return token;
            }
            catch
            {
                // Best-effort text scan over arbitrary SQL, and the only failure it can produce is
                // an out-of-range index on a malformed statement. Returning null is the contract --
                // the caller falls back to its own naming -- and this method is static, so there is
                // no logger to report to.
                return null;
            }
        }
        /// <summary>
        /// Asynchronously retrieves data for a specified entity from the database, with the option to apply filters.
        /// </summary>
        /// <param name="EntityName">The name of the entity (table) to retrieve data from.</param>
        /// <param name="Filter">A list of filters to apply to the query.</param>
        /// <remarks>
        /// Runs the read on a thread-pool thread and completes when the rows are in hand.
        ///
        /// The offload used to be a no-op. <see cref="GetEntity"/> is an iterator, so
        /// <c>Task.Run(() =&gt; GetEntity(...))</c> returned the un-enumerated sequence immediately:
        /// opening the connection, executing the reader and materialising every row all happened on
        /// the *consuming* thread, when it got round to enumerating. The task completed in
        /// microseconds having done nothing, and the XML comment claiming the application stayed
        /// responsive was false.
        ///
        /// Enumerating inside the task is what makes the offload real, and it is also what the
        /// signature promises: a caller awaiting a <c>Task&lt;IEnumerable&lt;object&gt;&gt;</c>
        /// reasonably expects the awaited result to be data, not a reader still attached to the
        /// shared connection that will do its I/O later -- or never, if the caller abandons it.
        ///
        /// The cost is buffering: the whole result set is materialised. Callers that need to stream
        /// should use <c>GetEntityStreamAsync&lt;T&gt;</c> (Modernization.cs), which is a genuine
        /// <c>IAsyncEnumerable</c> over <c>ExecuteReaderAsync</c>.
        /// </remarks>
        /// <returns>A task that completes with the rows read.</returns>
        public virtual Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            return Task.Run<IEnumerable<object>>(() =>
            {
                var rows = GetEntity(EntityName, Filter);
                if (rows == null)
                    return new List<object>();

                // Enumerate here, on the pool thread, so the connection work actually happens off
                // the caller's thread. A List is already a List; anything else is drained.
                return rows as IList<object> ?? rows.Cast<object>().ToList();
            });
        }

        // Helper method to extract WHERE clause from a query
        private string ExtractWhereClause(string query)
        {
            string lowerQuery = query.ToLower();
            int wherePos = lowerQuery.IndexOf(" where ");

            if (wherePos >= 0)
            {
                // Find the position after "where"
                int startPos = wherePos + 7; // length of " where "

                // Find the next clause, if any
                int endPos = lowerQuery.Length;
                string[] endClauses = { " group by ", " having ", " order by " };

                foreach (string clause in endClauses)
                {
                    int pos = lowerQuery.IndexOf(clause, startPos);
                    if (pos >= 0 && pos < endPos)
                    {
                        endPos = pos;
                    }
                }

                return "WHERE " + query.Substring(startPos, endPos - startPos).Trim();
            }

            return string.Empty;
        }

        /// <summary>
        /// Sets up necessary objects and structures for database operations based on the provided entity name.
        /// </summary>
        /// <param name="Entityname">The name of the entity for which the database command objects will be set up.</param>
        /// <remarks>
        /// This method is essential for initializing and reusing database commands and structures, improving efficiency and maintainability.
        /// </remarks>
        private void SetObjects(string Entityname)
        {
            if (!ObjectsCreated || Entityname != lastentityname)
            {
                DataStruct = GetEntityStructure(Entityname, false);

                // A cached structure with no fields is unusable: every statement built from it comes
                // out column-less (an INSERT degenerates to "() VALUES ()"). That is the normal state
                // for a table just created by MigrationManager — the cache predates it — so re-read
                // the structure from the datasource before giving up.
                if (DataStruct == null || DataStruct.Fields == null || DataStruct.Fields.Count == 0)
                {
                    DataStruct = GetEntityStructure(Entityname, true);
                }

                if (DataStruct == null || DataStruct.Fields == null || DataStruct.Fields.Count == 0)
                {
                    DMEEditor?.AddLogMessage("Fail",
                        $"Entity structure not found (or has no fields) for '{Entityname}' — statements built " +
                        "from it would have no columns.", DateTime.Now, 0, null, Errors.Failed);
                }
                // Dispose the command this field was holding before replacing it. SetObjects runs
                // on every entity switch, and the previous command was simply dropped — one leaked
                // IDbCommand per switch, for the lifetime of the datasource.
                if (command != null)
                {
                    try { command.Dispose(); }
                    catch (Exception ex) { Logger?.WriteLog($"Error disposing the cached command for {DatasourceName}: {ex.Message}"); }
                    command = null;
                }

                command = RDBMSConnection.DbConn?.CreateCommand();

                // Same reason as GetDataCommand: a command created while a
                // transaction is open must carry it. (2026-08-03)
                var activeTx = ActiveTransaction;
                if (command != null && activeTx != null) command.Transaction = activeTx;
                enttype = GetEntityType(Entityname);
                ObjectsCreated = true;
                lastentityname = Entityname;
            }
        }

        public virtual IDataReader GetDataReader(string querystring)
        {
            IDbCommand cmd = GetDataCommand();

            // GetDataCommand returns null on a closed connection; this used to dereference it and
            // throw a bare NullReferenceException out of an IRDBSource contract method.
            // GetDataCommand has already recorded the reason on ErrorObject.
            if (cmd == null)
                return null;

            try
            {
                cmd.CommandText = querystring;
                IDataReader dt = cmd.ExecuteReader();

                SetSuccess();

                // The caller only ever gets the reader back, so the reader has to own the command:
                // GetDataCommand() creates a new one on every call and nothing here disposed it.
                // Disposing it before returning is not an option either -- on SQLite and others
                // that finalises the statement the open reader is reading through.
                return new DataBase.Helpers.CommandOwningDataReader(dt, cmd);
            }
            catch
            {
                // No reader to carry ownership, so the command has to go now.
                cmd.Dispose();
                throw;
            }
        }

        #endregion
    }
}
