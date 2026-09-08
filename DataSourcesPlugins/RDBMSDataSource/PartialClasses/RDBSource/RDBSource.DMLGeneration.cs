using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Data.SqlTypes;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Helpers.RDBMSHelpers;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.DataBase
{
    public partial class RDBSource : IRDBSource
    {
        /// <summary>
        /// Clears the per-operation parameter-allocation state. Call once at the start of every
        /// statement build, before <see cref="AllocateParameterName"/>.
        /// </summary>
        private void ResetParameterAllocation()
        {
            usedParameterNames = new HashSet<string>();
            parameterNamesByField = new Dictionary<string, string>(StringComparer.Ordinal);
        }

        /// <summary>
        /// Allocates a unique parameter name for <paramref name="fieldName"/> and remembers the
        /// association, so binding can look it up exactly rather than guessing by prefix.
        /// </summary>
        /// <remarks>
        /// The 30-character clamp matches the pre-existing behaviour. It is not correct for every
        /// provider. It now asks <c>RDBMSHelper.GetMaxIdentifierLength(DataSourceType)</c> instead
        /// of assuming 30 — Oracle's pre-12.2 limit — which needlessly truncated names on SQL Server
        /// (128), MySQL (64), PostgreSQL (63) and every other engine, and each truncation is another
        /// chance for two distinct fields to collide onto one parameter.
        /// </remarks>
        private string AllocateParameterName(string fieldName)
        {
            // Every call site emits {ParameterDelimiter}p_{name}. The delimiter is not part of the
            // identifier, but the "p_" prefix is, so the budget for the normalised field name is the
            // provider's limit less those two characters.
            int budget = Math.Max(1, RDBMSHelper.GetMaxIdentifierLength(DatasourceType) - 2);

            string paramName = Regex.Replace(fieldName, @"\s+", "_");
            if (paramName.Length > budget)
            {
                paramName = paramName.Substring(0, budget);
            }

            // Two different fields can normalise to the same parameter name ("My Field" and
            // "My_Field" both become My_Field, and the clamp above can make long names collide),
            // so keep suffixing until the name is unused — trimming the stem so the suffix stays
            // inside the budget rather than pushing the name back over the provider's limit.
            int suffix = 1;
            string originalParamName = paramName;
            while (usedParameterNames.Contains(paramName))
            {
                string tail = "_" + suffix++;
                string head = originalParamName.Length + tail.Length > budget
                    ? originalParamName.Substring(0, Math.Max(1, budget - tail.Length))
                    : originalParamName;
                paramName = head + tail;
            }

            usedParameterNames.Add(paramName);
            parameterNamesByField[fieldName] = paramName;
            return paramName;
        }

        /// <summary>
        /// Returns the parameter name allocated for <paramref name="fieldName"/> during statement
        /// generation, or null when the field was not part of the generated statement.
        /// </summary>
        private string ResolveParameterName(string fieldName)
        {
            return parameterNamesByField.TryGetValue(fieldName, out string paramName) ? paramName : null;
        }

        private IDbCommand CreateCommandParameters(IDbCommand command, object InsertedData, EntityStructure DataStruct)
        {

            foreach (var field in DataStruct.Fields.OrderBy(o => o.FieldName))
            {
                // Skip auto-increment (identity) fields
                if (field.IsAutoIncrement)
                {
                    continue;
                }

                var property = FindPropertyCaseInsensitive(InsertedData.GetType(), field.FieldName);
                if (property != null)
                {
                    var value = property.GetValue(InsertedData) ?? DBNull.Value;
                    var parameter = command.CreateParameter();

                    string matchingParamName = ResolveParameterName(field.FieldName);
                    if (string.IsNullOrEmpty(matchingParamName))
                    {
                        throw new InvalidOperationException($"Parameter name for field '{field.FieldName}' was not allocated during statement generation.");
                    }

                    parameter.ParameterName = $"{ParameterDelimiter}p_" + matchingParamName;


                    parameter.DbType = GetDbType(field.Fieldtype);
                    if (value != DBNull.Value && value.GetType() != typeof(DBNull))
                    {
                        parameter.Value = ConvertToDbTypeValue(value, field.Fieldtype);
                    }
                    else
                    {
                        parameter.Value = DBNull.Value;
                    }
                    command.Parameters.Add(parameter);
                }
                else
                {
                    DMEEditor?.AddLogMessage("Beep", $"Field '{field.FieldName}' has no matching property on type '{InsertedData.GetType().Name}'; its SQL placeholder will be left unbound.", DateTime.Now, 0, DataStruct.EntityName, Errors.Warning);
                }
            }

            return command;
        }
        private IDbCommand CreateUpdateCommandParameters(IDbCommand command, object InsertedData, EntityStructure DataStruct)
        {
            for (int i = 0; i < UpdateFieldSequnce.Count; i++)
            {
                EntityField field = UpdateFieldSequnce[i];

                // Skip auto-increment fields — EXCEPT primary keys.
                //
                // Skipping identity columns is right for INSERT, where the server
                // assigns them. This is UPDATE, and GetUpdateString deliberately
                // appends the primary keys to UpdateFieldSequnce (see the
                // AddRange after the SET clause) precisely so they can be bound
                // for "where Id = @p_Id". Skipping an IDENTITY primary key left
                // that placeholder in the SQL with nothing bound to it:
                //
                //   Must declare the scalar variable "@p_Id".
                //
                // So no row could ever be updated on a table whose key is an
                // IDENTITY column — which is most SQL Server tables. SQLite keys
                // are not flagged auto-increment by its schema reader, so a
                // file-based driver never hit this. (2026-08-03)
                if (field.IsAutoIncrement &&
                    !DataStruct.PrimaryKeys.Any(pk =>
                        string.Equals(pk.FieldName, field.FieldName, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var property = FindPropertyCaseInsensitive(InsertedData.GetType(), field.FieldName);
                if (property != null)
                {
                    var value = property.GetValue(InsertedData) ?? DBNull.Value;
                    var parameter = command.CreateParameter();

                    string matchingParamName = ResolveParameterName(field.FieldName);
                    if (string.IsNullOrEmpty(matchingParamName))
                    {
                        throw new InvalidOperationException($"Parameter name for field '{field.FieldName}' was not allocated during statement generation.");
                    }

                    parameter.ParameterName = $"{ParameterDelimiter}p_" + matchingParamName;


                    parameter.DbType = GetDbType(field.Fieldtype);
                    if (value != DBNull.Value && value.GetType() != typeof(DBNull))
                    {
                        parameter.Value = ConvertToDbTypeValue(value, field.Fieldtype);
                    }
                    else
                    {
                        parameter.Value = DBNull.Value;
                    }
                    command.Parameters.Add(parameter);
                }
                else
                {
                    DMEEditor?.AddLogMessage("Beep", $"Field '{field.FieldName}' has no matching property on type '{InsertedData.GetType().Name}'; its SQL placeholder will be left unbound.", DateTime.Now, 0, DataStruct.EntityName, Errors.Warning);
                }
            }

            return command;
        }

        /// <summary>
        /// Creates parameters for a DELETE database command based on the provided DataRow and EntityStructure.
        /// </summary>
        /// <param name="command">The DELETE database command to add parameters to.</param>
        /// <param name="r">The DataRow containing parameter values for the DELETE operation.</param>
        /// <param name="DataStruct">The EntityStructure defining the primary keys for the DELETE operation.</param>
        /// <returns>The updated IDbCommand with parameters added.</returns>
        private IDbCommand CreateDeleteCommandParameters(IDbCommand command, object r, EntityStructure DataStruct)
        {
            command.Parameters.Clear();

            foreach (EntityField field in DataStruct.PrimaryKeys.OrderBy(o => o.FieldName))
            {

                var property = FindPropertyCaseInsensitive(r.GetType(), field.FieldName);
                if (property != null)
                {
                    // Coalesce here, not after the null test below. Its two sibling binders do
                    // `?? DBNull.Value` at this point; this one did not, so a null primary-key value
                    // reached `value.GetType()` and threw NullReferenceException instead of binding
                    // DBNull like the INSERT and UPDATE paths do.
                    var value = property.GetValue(r) ?? DBNull.Value;
                    var parameter = command.CreateParameter();

                    string matchingParamName = ResolveParameterName(field.FieldName);
                    if (string.IsNullOrEmpty(matchingParamName))
                    {
                        throw new InvalidOperationException($"Parameter name for field '{field.FieldName}' was not allocated during statement generation.");
                    }

                    parameter.ParameterName = $"{ParameterDelimiter}p_" + matchingParamName;
                    parameter.DbType = GetDbType(field.Fieldtype);
                    if (value != DBNull.Value && value.GetType() != typeof(DBNull))
                    {
                        parameter.Value = ConvertToDbTypeValue(value, field.Fieldtype);
                    }
                    else
                    {
                        parameter.Value = DBNull.Value;
                    }

                    command.Parameters.Add(parameter);
                }
                else
                {
                    DMEEditor?.AddLogMessage("Beep", $"Field '{field.FieldName}' has no matching property on type '{r.GetType().Name}'; its SQL placeholder will be left unbound.", DateTime.Now, 0, DataStruct.EntityName, Errors.Warning);
                }

            }
            return command;
        }

        public virtual string GetInsertString(string EntityName, EntityStructure DataStruct)
        {
            List<EntityField> SourceEntityFields = new List<EntityField>();
            List<EntityField> DestEntityFields = new List<EntityField>();

            // Qualify the entity name directly rather than building the statement and then parsing
            // the table back out of it with GetTableName. That routine rewrote SQL by string
            // surgery: it lowercased the whole fragment (destroying identifier case on
            // case-sensitive servers) and dispatched on IndexOf("insert"/"update"/"delete"), so an
            // entity whose NAME merely contains one of those words — `updates`, `deleted_records`,
            // `insert_log` — took the wrong branch and corrupted the statement.
            string Insertstr = "INSERT INTO " + QualifyWithSchema(EntityName) + " (";
            string Valuestr = ") VALUES (";

            int t = 0;
            foreach (EntityField item in DataStruct.Fields.OrderBy(o => o.FieldName))
            {
                if (!(item.IsAutoIncrement))
                {
                    string FieldName = GetFieldName(item.FieldName);
                    string paramName = AllocateParameterName(item.FieldName);

                    Insertstr += $"{FieldName},";
                    Valuestr += $"{ParameterDelimiter}p_" + paramName + ",";
                }

                t += 1;
            }
            Insertstr = Insertstr.Remove(Insertstr.Length - 1);
            Valuestr = Valuestr.Remove(Valuestr.Length - 1);
            Valuestr += ")";
            return Insertstr + Valuestr;
        }
        public virtual string GetUpdateString(string EntityName, EntityStructure DataStruct)
        {
            List<EntityField> SourceEntityFields = new List<EntityField>();
            List<EntityField> DestEntityFields = new List<EntityField>();

            // Schema-qualified like INSERT. The GetTableName call that used to do this was commented
            // out, so INSERT addressed schema.Table while UPDATE and DELETE addressed Table — on any
            // datasource with a non-default SchemaName, two different objects.
            string Updatestr = @"Update " + QualifyWithSchema(EntityName) + " set " + Environment.NewLine;
            // i want a new list of fields that are the primary key at the end of the list
            UpdateFieldSequnce = new List<EntityField>();
            for (int i = 0; i < DataStruct.Fields.Count; i++)
            {
                EntityField field = DataStruct.Fields[i];
                if (!DataStruct.PrimaryKeys.Any(l => l.FieldName == field.FieldName))
                {
                    UpdateFieldSequnce.Add(field);
                }
            }
            for (int i = 0; i < UpdateFieldSequnce.Count; i++)
            {
                EntityField item = UpdateFieldSequnce[i];
                if (!DataStruct.PrimaryKeys.Any(l => l.FieldName == item.FieldName))
                {
                    string paramName = AllocateParameterName(item.FieldName);
                    Updatestr += $"{GetFieldName(item.FieldName)}= {ParameterDelimiter}p_{paramName},";
                }
            }



            // Refuse to build a keyless UPDATE rather than emitting a broken one.
            //
            // With no primary keys the WHERE loop below appends nothing, leaving "... where" — a
            // syntax error the caller saw as an opaque provider message. With no non-key fields
            // (every column is part of the key) the SET clause is empty and the Remove below chops a
            // character off " set \r\n" instead of a trailing comma, producing a mangled statement.
            // Neither ever updated the wrong rows, but neither said what was wrong either.
            if (DataStruct?.PrimaryKeys == null || DataStruct.PrimaryKeys.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Cannot build an UPDATE for '{EntityName}': its entity structure has no primary key, " +
                    "so the statement would have no WHERE clause. Refresh the structure " +
                    "(GetEntityStructure with refresh: true) or declare a key.");
            }

            if (!Updatestr.TrimEnd().EndsWith(","))
            {
                throw new InvalidOperationException(
                    $"Cannot build an UPDATE for '{EntityName}': every column is part of the primary key, " +
                    "so there is nothing to SET.");
            }

            Updatestr = Updatestr.Remove(Updatestr.Length - 1); // Remove the trailing comma
            UpdateFieldSequnce.AddRange(DataStruct.PrimaryKeys);
            Updatestr += @" where " + Environment.NewLine;
            int t = 1;
            for (int i = 0; i < DataStruct.PrimaryKeys.Count; i++)
            {
                EntityField item = DataStruct.PrimaryKeys[i];

                // Primary keys are excluded from the SET clause above (UpdateFieldSequnce is built
                // from the non-key fields), so each one is allocated fresh here.
                //
                // This replaces a substring lookup that read:
                //     if (usedParameterNames.Contains(paramName))
                //         paramName = usedParameterNames.FirstOrDefault(p => p.Contains(paramName));
                // For primary key "Id" with an existing SET parameter "ProductId", that returned
                // "ProductId" — so the WHERE clause became "where Id = @p_ProductId" and the UPDATE
                // was steered onto a row selected by another column's value, with no error.
                string paramName = AllocateParameterName(item.FieldName);

                if (t == 1)
                {
                    Updatestr += $"{GetFieldName(item.FieldName)}= {ParameterDelimiter}p_{paramName}";
                }
                else
                {
                    Updatestr += $" and {GetFieldName(item.FieldName)}= {ParameterDelimiter}p_{paramName}";
                }
                t += 1;
            }

            return Updatestr;
        }
        public virtual string GetDeleteString(string EntityName, EntityStructure DataStruct)
        {
            // Without keys the loop below appends nothing, leaving "DELETE FROM t WHERE " — a syntax
            // error rather than a mass delete, but an unattributable one. Say what is actually wrong.
            if (DataStruct?.PrimaryKeys == null || DataStruct.PrimaryKeys.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Cannot build a DELETE for '{EntityName}': its entity structure has no primary key, " +
                    "so the statement would have no WHERE clause and would target every row. Refresh the " +
                    "structure (GetEntityStructure with refresh: true) or declare a key.");
            }

            string deleteStr = $"DELETE FROM {QualifyWithSchema(EntityName)} WHERE ";
            int t = 1;
            foreach (EntityField item in DataStruct.PrimaryKeys.OrderBy(o => o.FieldName))
            {
                string paramName = AllocateParameterName(item.FieldName);
                if (t > 1)
                {
                    deleteStr += " AND ";
                }
                deleteStr += $"{GetFieldName(item.FieldName)} = {ParameterDelimiter}p_{paramName}";
                t += 1;
            }
            return deleteStr;
        }

        protected virtual string GenerateCreateEntityScript(EntityStructure t1)
        {
            string createtablestring = "Create table ";
            try
            {//-- Create Create string
                t1.EntityName = Regex.Replace(t1.EntityName, @"\s+", "_");
                createtablestring += " " + t1.EntityName + "\n(";

                if (t1.Fields.Count == 0)
                {
                    // Empty fields collection, add error log
                    DMEEditor?.AddLogMessage("Fail", $"No fields defined for entity {t1.EntityName}", DateTime.Now, 0, t1.EntityName, Errors.Failed);
                    return createtablestring + ")";
                }

                // Filter out fields with empty names before calculating total
                var validFields = t1.Fields.Where(p => !string.IsNullOrEmpty(p.FieldName?.Trim())).ToList();
                int totalValidFields = validFields.Count;

                if (totalValidFields == 0)
                {
                    DMEEditor?.AddLogMessage("Fail", $"All field names are empty for {t1.EntityName}", DateTime.Now, 0, t1.EntityName, Errors.Failed);
                    return createtablestring + ")";
                }

                int processedFields = 0;

                foreach (EntityField dbf in t1.Fields)
                {
                    // Skip fields with empty names
                    if (string.IsNullOrEmpty(dbf.FieldName))
                    {
                        DMEEditor?.AddLogMessage("Fail", $"Field Name is empty for {t1.EntityName}", DateTime.Now, 0, t1.EntityName, Errors.Failed);
                        continue;
                    }

                    string FieldName = dbf.FieldName;
                    if (DatasourceType == DataSourceType.Mysql)
                    {
                       FieldName = FieldName.Replace(" ", "_");
                       FieldName = "`" + FieldName + "`";
                    }

                    // Get database-specific type with null safety
                    string dbType = null;
                    if (DMEEditor?.typesHelper != null)
                    {
                        dbType = DMEEditor.typesHelper.GetDataType(DatasourceName, dbf);
                    }

                    // The lookup falls back to .NET type names ("System.Boolean") when it cannot
                    // resolve a mapping for this datasource. Those are not SQL types — emitting one
                    // produces DDL the server rejects ("Cannot find data type BOOLEAN"). Detect that
                    // and map to a real type for THIS provider.
                    if (string.IsNullOrWhiteSpace(dbType) ||
                        dbType.StartsWith("System.", StringComparison.OrdinalIgnoreCase))
                    {
                        var unresolved = dbType;
                        dbType = GetFallbackDbType(dbf.Fieldtype, DatasourceType);
                        DMEEditor?.AddLogMessage("Beep",
                            $"No datasource type mapping for field '{dbf.FieldName}' (Fieldtype='{dbf.Fieldtype}'" +
                            (string.IsNullOrWhiteSpace(unresolved) ? "" : $"', resolved to '{unresolved}'") +
                            $"); using '{dbType}' for {DatasourceType}.",
                            DateTime.Now, 0, t1.EntityName, Errors.Warning);
                    }

                    // The configured mappings are keyed loosely enough that a type belonging to a
                    // different provider can come back (e.g. BOOLEAN for SQL Server, which has none).
                    // Translate those to the target provider's equivalent rather than emitting DDL
                    // the server will reject.
                    dbType = NormalizeDbTypeForProvider(dbType, DatasourceType);
                    
                    createtablestring += "\n " + FieldName + " " + dbType + " ";

                    if (dbf.IsAutoIncrement)
                    {
                        string autonumberstring = CreateAutoNumber(dbf);
                        // Check RDBSource's own ErrorObject (what CreateAutoNumber actually sets),
                        // NOT DMEEditor.ErrorObject which may have been set by prior operations (e.g. GetDataType)
                        if (ErrorObject.Flag == Errors.Ok)
                        {
                            createtablestring += autonumberstring;
                        }
                        else
                        {
                            throw new Exception(ErrorObject.Message);
                        }
                    }

                    if (dbf.AllowDBNull == false)
                    {
                        createtablestring += " NOT NULL ";
                    }

                    if (dbf.IsUnique == true)
                    {
                        createtablestring += " UNIQUE ";
                    }

                    processedFields++;

                    // Only add comma if this is not the last valid field
                    if (processedFields < totalValidFields)
                    {
                        createtablestring += ",";
                    }
                }

                // Add primary key constraint if there are primary keys
                // For SQLite: skip separate PRIMARY KEY constraint if an auto-increment field
                // already has inline PRIMARY KEY AUTOINCREMENT (SQLite doesn't allow both)
                bool hasInlineAutoIncrementPK = (DatasourceType == DataSourceType.SqlLite || 
                    (Dataconnection?.ConnectionProp?.DatabaseType ?? DataSourceType.Unknown) == DataSourceType.SqlLite) &&
                    t1.PrimaryKeys != null && t1.PrimaryKeys.Count == 1 && 
                    t1.PrimaryKeys[0].IsAutoIncrement;

                if (t1.PrimaryKeys != null && t1.PrimaryKeys.Count > 0 && !hasInlineAutoIncrementPK)
                {
                    // Add comma before primary key only if we have valid fields
                    if (totalValidFields > 0)
                    {
                        createtablestring += ",";
                    }
                    createtablestring += "\n" + CreatePrimaryKeyString(t1);
                }

                // Close the CREATE TABLE statement
                createtablestring += ")";
            }
            catch (Exception ex)
            {
                string innerMsg = ex.InnerException != null ? $" Inner: {ex.InnerException.Message}" : "";
                DMEEditor?.AddLogMessage("Fail", $"Error Creating Entity {t1.EntityName}: {ex.GetType().Name}: {ex.Message}{innerMsg} | SQL so far: [{createtablestring}]", DateTime.Now, 0, t1.EntityName, Errors.Failed);
                createtablestring = "";
            }

            return createtablestring;
        }

        public virtual List<ETLScriptDet> GenerateCreatEntityScript(List<EntityStructure> entities)
        {
            SetSuccess();
            int i = 0;
            List<ETLScriptDet> rt = new List<ETLScriptDet>();
            try
            {
                // Generate Create Table First
                foreach (EntityStructure item in entities)
                {
                    ETLScriptDet x = new ETLScriptDet();
                    x.DestinationDataSourceEntityName = DatasourceName;
                    x.Ddl = CreateEntity(item);
                    x.SourceEntityName = item.EntityName;
                    x.SourceDataSourceEntityName = item.DatasourceEntityName;
                    x.ScriptType = DDLScriptType.CreateEntity;
                    rt.Add(x);
                    rt.AddRange(CreateForKeyRelationScripts(item));
                    i += 1;
                }
            }
            catch (Exception ex)
            {
                string errmsg = "Error in Generating Script";
                DMEEditor?.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);

            }
            return rt;

        }
        public virtual List<ETLScriptDet> GenerateCreatEntityScript(EntityStructure entity)
        {
            SetSuccess();

            List<ETLScriptDet> rt = new List<ETLScriptDet>();
            try
            {
                // Generate Create Table First

                ETLScriptDet x = new ETLScriptDet();
                x.DestinationDataSourceEntityName = DatasourceName;
                x.Ddl = CreateEntity(entity);
                x.SourceEntityName = entity.EntityName;
                x.SourceDataSourceEntityName = entity.DatasourceEntityName;
                x.ScriptType = DDLScriptType.CreateEntity;
                rt.Add(x);
                rt.AddRange(CreateForKeyRelationScripts(entity));
            }
            catch (Exception ex)
            {
                string errmsg = "Error in Generating Script";
                DMEEditor?.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);

            }
            return rt;

        }
        private List<ETLScriptDet> GetDDLScriptfromDatabase(string entity)
        {
            List<ETLScriptDet> rt = new List<ETLScriptDet>();

            try
            {
                // Called directly. These three were each Task.Run(...) followed by .Wait() on a
                // synchronous method: the calling thread blocked anyway, so nothing was gained, one
                // pool thread was consumed per call, and a limited scheduler (a UI SynchronizationContext,
                // an ASP.NET request pool) could deadlock on it. Worse for diagnosis, .Wait() rewraps
                // whatever the method threw in an AggregateException, so the catch below logged
                // "One or more errors occurred." instead of the actual failure.
                EntityStructure entstructure = GetEntityStructure(entity, true);
                entstructure.IsCreated = false;

                // Read this datasource's own flag. DMEEditor.ErrorObject is usually the same
                // instance -- the standard creation path passes it as the constructor's `per` --
                // but that is an aliasing coincidence, not a contract, and it is null whenever no
                // editor is attached. Every failure path in this class sets both.
                if (ErrorObject?.Flag == Errors.Ok)
                {
                    Entities[Entities.FindIndex(x => x.EntityName == entity)] = entstructure;

                }
                else
                {
                    DMEEditor?.AddLogMessage("Fail", $"Error getting entity structure for {entity}", DateTime.Now, entstructure.Id, entstructure.DataSourceID, Errors.Failed);
                }
                rt.AddRange(GenerateCreatEntityScript(entstructure));
                rt.AddRange(CreateForKeyRelationScripts(entstructure));
            }
            catch (System.Exception ex)
            {
                DMEEditor?.AddLogMessage("Fail", $"Error in getting entities from Database ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);
            }
            return rt;
        }
        private List<ETLScriptDet> GetDDLScriptfromDatabase(List<EntityStructure> structureentities)
        {
            List<ETLScriptDet> rt = new List<ETLScriptDet>();
            try
            {
                if (structureentities.Count > 0)
                {
                    // See the overload above: Task.Run + Wait on a synchronous method.
                    rt.AddRange(GenerateCreatEntityScript(structureentities));
                }
            }
            catch (System.Exception ex)
            {
                DMEEditor?.AddLogMessage("Fail", $"Error in getting entities from Database ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);
            }
            return rt;
        }
        private string CreatePrimaryKeyString(EntityStructure t1)
        {
            string retval = null;
            try
            {
                if (t1.PrimaryKeys.Count > 0)
                {
                    retval = @" PRIMARY KEY ( ";
                }
                else
                {
                    return string.Empty;
                }

                ErrorObject.Flag = Errors.Ok;
                int i = 0;
                foreach (EntityField dbf in t1.PrimaryKeys)
                {
                    retval += dbf.FieldName + ",";

                    i += 1;
                }
                if (retval.EndsWith(","))
                {
                    retval = retval.Remove(retval.Length - 1, 1);
                }
                retval += ")\n";
                return retval;
            }
            catch (Exception ex)
            {
                string mes = "";
                DMEEditor?.AddLogMessage(ex.Message, "Could not  Create Primery Key" + mes, DateTime.Now, -1, mes, Errors.Failed);
                return null;
            };
        }
        private string CreateAlterRalationString(EntityStructure t1)
        {
            string retval = "";
            ErrorObject.Flag = Errors.Ok;
            try
            {
                int i = 0;
                foreach (string item in t1.Relations.Select(o => o.RelatedEntityID).Distinct())
                {
                    string forkeys = "";
                    string refkeys = "";
                    foreach (RelationShipKeys fk in t1.Relations.Where(p => p.RelatedEntityID == item))
                    {
                        forkeys += fk.EntityColumnID + ",";
                        refkeys += fk.RelatedEntityColumnID + ",";
                    }
                    i += 1;
                    forkeys = forkeys.Remove(forkeys.Length - 1, 1);
                    refkeys = refkeys.Remove(refkeys.Length - 1, 1);
                    retval += @" ALTER TABLE " + t1.EntityName + " ADD CONSTRAINT " + t1.EntityName + i + Random.Shared.Next(10, 1000) + "  FOREIGN KEY (" + forkeys + ")  REFERENCES " + item + "(" + refkeys + "); \n";
                }
                if (i == 0)
                {
                    retval = "";
                }
                return retval;
            }

            catch (Exception ex)
            {
                string mes = "";
                DMEEditor?.AddLogMessage(ex.Message, "Could not Create Relation" + mes, DateTime.Now, -1, mes, Errors.Failed);
                return null;
            };
        }
        private List<ETLScriptDet> CreateForKeyRelationScripts(EntityStructure entity)
        {
            List<ETLScriptDet> rt = new List<ETLScriptDet>();
            try
            {
                int i = 0;
                IDataSource ds;
                // Generate Forign Keys
                if (entity.Relations != null)
                {
                    if (entity.Relations.Count > 0)
                    {
                        string relations = CreateAlterRalationString(entity);
                        string[] rels = relations.Split(';');
                        foreach (string rl in rels)
                        {
                            ETLScriptDet x = new ETLScriptDet();
                            x.DestinationDataSourceEntityName = DatasourceName;
                            // The result is discarded -- it always was. The call is kept and guarded
                            // rather than deleted because GetDataSource registers and opens a
                            // datasource as a side effect, and dropping that silently is a bigger
                            // change than removing an unused local.
                            ds = DMEEditor?.GetDataSource(entity.DataSourceID);
                            x.SourceDataSourceEntityName = entity.DatasourceEntityName;
                            x.Ddl = rl;
                            x.SourceEntityName = entity.EntityName;
                            x.ScriptType = DDLScriptType.AlterFor;
                            rt.Add(x);
                        }
                        i += 1;
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Fail", $"Error in getting For. Keys from Database ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);
            }
            return rt;
        }
        private List<ETLScriptDet> CreateForKeyRelationScripts(List<EntityStructure> entities)
        {
            List<ETLScriptDet> rt = new List<ETLScriptDet>();

            try
            {
                int i = 0;
                IDataSource ds;
                // Generate Forign Keys
                foreach (EntityStructure item in entities)
                {
                    if (item.Relations != null)
                    {
                        if (item.Relations.Count > 0)
                        {
                            ETLScriptDet x = new ETLScriptDet();
                            x.DestinationDataSourceEntityName = item.DataSourceID;
                            // See the sibling above: the result is discarded, the side effect is not.
                            ds = DMEEditor?.GetDataSource(item.DataSourceID);
                            x.SourceDataSourceName = item.DatasourceEntityName;
                            x.Ddl = CreateAlterRalationString(item);
                            x.SourceEntityName = item.EntityName;
                            x.ScriptType = DDLScriptType.AlterFor;
                            rt.Add(x);
                            //alteraddForignKey.Add(x);
                            i += 1;
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Fail", $"Error in getting For. Keys from Database ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);

            }
            return rt;
        }
        public virtual string CreateAutoNumber(EntityField f)
        {
            ErrorObject.Flag = Errors.Ok;
            string AutnumberString = "";
            try
            {
                if (f.IsAutoIncrement)
                {
                    var dbType = Dataconnection?.ConnectionProp?.DatabaseType ?? DatasourceType;
                    // Delegate to centralized helper for database-specific auto-increment syntax
                    AutnumberString = TheTechIdea.Beep.Helpers.RDBMSHelpers.DMLHelpers.DatabaseDMLSpecificHelpers.GetAutoIncrementSyntax(dbType);
                    
                    // Handle special cases not covered by the centralized helper
                    if (string.IsNullOrEmpty(AutnumberString))
                    {
                        switch (dbType)
                        {
                            case DataSourceType.SqlCompact:
                                AutnumberString = "IDENTITY(1,1)";
                                break;
                        }
                    }

                    if (string.IsNullOrEmpty(AutnumberString))
                    {
                        // No known syntax for this provider (Snowflake, Hana, Presto, Spanner,
                        // CockroachDB, Firebolt). The caller appends whatever comes back when the
                        // flag is Ok, so the table is created WITHOUT the identity property and
                        // nothing said so.
                        //
                        // Logged rather than flagged deliberately: the caller throws on a non-Ok
                        // flag, so failing here would turn "table created without identity" into
                        // "CreateEntityAs throws" on six engines. That is arguably the right
                        // behaviour, but it is a behavioural decision, not an error-reporting fix —
                        // tracked in docs/10-known-issues.md rather than changed here.
                        Logger?.WriteLog($"CreateAutoNumber: no auto-increment syntax is known for {dbType}; " +
                                         $"column {f.EntityName}.{f.FieldName} will be created without the " +
                                         $"identity property.");
                    }
                }

                SetSuccess();
            }
            catch (System.Exception ex)
            {
                HandleDatabaseError(ex, f?.EntityName, $"create the auto-number clause for column {f?.FieldName} of");
            }
            return AutnumberString;
        }
        /// <summary>
        /// Translates a resolved column type into one the target provider actually has.
        /// </summary>
        /// <remarks>
        /// The configured type mappings are keyed by datasource name and can return a type belonging
        /// to a different provider. Emitting it produces DDL the server rejects — SQL Server, for
        /// instance, has no BOOLEAN, TEXT-as-unicode, or BLOB. Only genuinely foreign spellings are
        /// rewritten; anything the provider understands (including sized types like NVARCHAR(200))
        /// is passed through untouched.
        /// </remarks>
        /// <remarks>
        /// This runs on every provider's CREATE TABLE, not only the three engines that reach it
        /// through the bulk temp-table path -- it is <see cref="GenerateCreateEntityScript"/>'s own
        /// guard against <c>DMEEditor.typesHelper.GetDataType</c> returning a type name that belongs
        /// to a DIFFERENT provider (the method's own comment: "e.g. BOOLEAN for SQL Server, which has
        /// none"). It originally normalised only for <see cref="DataSourceType.SqlServer"/> and
        /// passed every other provider's input straight through unchanged -- so the same leak on
        /// MySQL, PostgreSQL, Oracle, CockroachDB, HANA, Firebird, Presto/Trino, Snowflake or Spanner
        /// reached the server as-is. Extended to every dialect <see cref="GetFallbackDbType"/> has a
        /// confident mapping for, reusing that method's own target vocabulary so the two never
        /// disagree about what "this provider's TEXT type" is called.
        /// </remarks>
        private static string NormalizeDbTypeForProvider(string dbType, DataSourceType datasourceType)
        {
            if (string.IsNullOrWhiteSpace(dbType)) return dbType;

            // Compare on the bare type name so sized types (NUMBER(10), VARCHAR2(200)) still match.
            var bare = dbType.Trim();
            var paren = bare.IndexOf('(');
            var name = (paren > 0 ? bare.Substring(0, paren) : bare).Trim().ToUpperInvariant();
            var args = paren > 0 ? bare.Substring(paren) : string.Empty;
            string Sized(string type, string fallbackArgs) => type + (string.IsNullOrEmpty(args) ? fallbackArgs : args);

            switch (datasourceType)
            {
                case DataSourceType.SqlServer:
                case DataSourceType.AzureSQL:
                case DataSourceType.SqlCompact:
                    return name switch
                    {
                        "BOOLEAN" or "BOOL" => "BIT",
                        "TEXT" or "CLOB" or "NCLOB" or "LONGTEXT" or "STRING" => "NVARCHAR(MAX)",
                        "BLOB" or "BYTEA" or "LONGBLOB" or "BYTES" or "BINARY" or "VARBINARY" => "VARBINARY(MAX)",
                        "DOUBLE" or "DOUBLE PRECISION" or "FLOAT64" => "FLOAT",
                        "INTEGER" or "INT64" => "INT",
                        "TIMESTAMP" or "TIMESTAMP_NTZ" => "DATETIME2",
                        "TIMESTAMPTZ" or "TIMESTAMP_TZ" => "DATETIMEOFFSET",
                        "UUID" => "UNIQUEIDENTIFIER",
                        "NUMBER" => Sized("DECIMAL", "(18,4)"),
                        "NVARCHAR2" => Sized("NVARCHAR", "(MAX)"),
                        "VARCHAR2" or "VARCHAR" => Sized("NVARCHAR", "(MAX)"),
                        _ => dbType
                    };

                case DataSourceType.Postgre:
                    return name switch
                    {
                        "BIT" => "BOOLEAN",
                        "CLOB" or "NCLOB" or "LONGTEXT" or "STRING" => "TEXT",
                        "BLOB" or "LONGBLOB" or "BYTES" or "BINARY" or "VARBINARY" => "BYTEA",
                        "DOUBLE" or "FLOAT64" => "DOUBLE PRECISION",
                        "INT64" => "BIGINT",
                        "DATETIME2" or "TIMESTAMP_NTZ" => "TIMESTAMP",
                        "DATETIMEOFFSET" or "TIMESTAMP_TZ" => "TIMESTAMPTZ",
                        "UNIQUEIDENTIFIER" => "UUID",
                        "NUMBER" => Sized("NUMERIC", "(18,4)"),
                        "NVARCHAR2" or "NVARCHAR" or "VARCHAR2" => Sized("VARCHAR", ""),
                        _ => dbType
                    };

                case DataSourceType.Cockroach:
                    // Same PostgreSQL-compatible vocabulary as above, except CockroachDB's own name
                    // for its binary type is BYTES -- BYTEA is only a Postgres-compatibility alias,
                    // and GetFallbackDbType already emits BYTES as Cockroach's canonical spelling.
                    // Normalizing an incoming BYTES back to BYTEA here would fight that choice.
                    return name switch
                    {
                        "BIT" => "BOOLEAN",
                        "CLOB" or "NCLOB" or "LONGTEXT" or "STRING" => "TEXT",
                        "BLOB" or "LONGBLOB" or "BYTEA" or "BINARY" or "VARBINARY" => "BYTES",
                        "DOUBLE" or "FLOAT64" => "DOUBLE PRECISION",
                        "INT64" => "BIGINT",
                        "DATETIME2" or "TIMESTAMP_NTZ" => "TIMESTAMP",
                        "DATETIMEOFFSET" or "TIMESTAMP_TZ" => "TIMESTAMPTZ",
                        "UNIQUEIDENTIFIER" => "UUID",
                        "NUMBER" => Sized("NUMERIC", "(18,4)"),
                        "NVARCHAR2" or "NVARCHAR" or "VARCHAR2" => Sized("VARCHAR", ""),
                        _ => dbType
                    };

                case DataSourceType.Mysql:
                case DataSourceType.MariaDB:
                    return name switch
                    {
                        "BOOLEAN" or "BOOL" or "BIT" => "TINYINT(1)",
                        "TEXT" or "CLOB" or "NCLOB" or "STRING" => "LONGTEXT",
                        "BLOB" or "BYTEA" or "BYTES" or "BINARY" or "VARBINARY" => "LONGBLOB",
                        "DOUBLE PRECISION" or "FLOAT64" => "DOUBLE",
                        "INTEGER" or "INT64" => "INT",
                        "TIMESTAMPTZ" or "TIMESTAMP_TZ" or "TIMESTAMP_NTZ" or "DATETIMEOFFSET" => "DATETIME",
                        "TIMESTAMP" => "DATETIME",
                        "UUID" or "UNIQUEIDENTIFIER" => "CHAR(36)",
                        "NUMBER" => Sized("DECIMAL", "(18,4)"),
                        "NVARCHAR2" or "NVARCHAR" or "VARCHAR2" => Sized("VARCHAR", "(255)"),
                        _ => dbType
                    };

                case DataSourceType.Oracle:
                    return name switch
                    {
                        "BOOLEAN" or "BOOL" or "BIT" => "NUMBER(1)",
                        "TEXT" or "CLOB" or "LONGTEXT" or "STRING" => "NCLOB",
                        "BLOB" or "BYTEA" or "LONGBLOB" or "BYTES" or "BINARY" or "VARBINARY" => "BLOB",
                        "DOUBLE" or "DOUBLE PRECISION" or "FLOAT64" => "BINARY_DOUBLE",
                        "INTEGER" => "NUMBER(10)",
                        "INT64" or "BIGINT" => "NUMBER(19)",
                        "TIMESTAMP_NTZ" => "TIMESTAMP",
                        "DATETIMEOFFSET" or "TIMESTAMPTZ" or "TIMESTAMP_TZ" => "TIMESTAMP WITH TIME ZONE",
                        "UUID" or "UNIQUEIDENTIFIER" => "RAW(16)",
                        "VARCHAR" or "NVARCHAR" => Sized("NVARCHAR2", "(2000)"),
                        _ => dbType
                    };

                case DataSourceType.Hana:
                    return name switch
                    {
                        "BOOL" or "BIT" => "BOOLEAN",
                        "TEXT" or "CLOB" or "LONGTEXT" or "STRING" => "NVARCHAR(5000)",
                        "BLOB" or "BYTEA" or "LONGBLOB" or "BYTES" or "BINARY" => "VARBINARY(5000)",
                        "DOUBLE PRECISION" or "FLOAT64" => "DOUBLE",
                        "INT64" => "BIGINT",
                        "DATETIME2" or "TIMESTAMP_NTZ" => "TIMESTAMP",
                        "DATETIMEOFFSET" or "TIMESTAMPTZ" or "TIMESTAMP_TZ" => "TIMESTAMP",
                        "UUID" or "UNIQUEIDENTIFIER" => "VARCHAR(36)",
                        "NUMBER" => Sized("DECIMAL", "(18,4)"),
                        "NVARCHAR2" or "VARCHAR2" or "VARCHAR" => Sized("NVARCHAR", "(5000)"),
                        _ => dbType
                    };

                case DataSourceType.FireBird:
                    return name switch
                    {
                        "BOOL" or "BIT" => "BOOLEAN",
                        "TEXT" or "CLOB" or "NCLOB" or "LONGTEXT" or "STRING" => "BLOB SUB_TYPE TEXT",
                        "BYTEA" or "LONGBLOB" or "BYTES" or "BINARY" or "VARBINARY" => "BLOB",
                        "DOUBLE" or "FLOAT64" => "DOUBLE PRECISION",
                        "INT64" => "BIGINT",
                        "DATETIME2" or "TIMESTAMP_NTZ" => "TIMESTAMP",
                        "DATETIMEOFFSET" or "TIMESTAMPTZ" or "TIMESTAMP_TZ" => "TIMESTAMP",
                        "UUID" or "UNIQUEIDENTIFIER" => "CHAR(36)",
                        "NUMBER" => Sized("NUMERIC", "(18,4)"),
                        "NVARCHAR2" or "NVARCHAR" or "VARCHAR2" or "VARCHAR" => "BLOB SUB_TYPE TEXT",
                        _ => dbType
                    };

                case DataSourceType.Presto:
                case DataSourceType.Trino:
                    return name switch
                    {
                        "BOOL" or "BIT" => "BOOLEAN",
                        "TEXT" or "CLOB" or "NCLOB" or "LONGTEXT" or "STRING" => "VARCHAR",
                        "BLOB" or "BYTEA" or "LONGBLOB" or "BYTES" or "BINARY" => "VARBINARY",
                        "DOUBLE PRECISION" or "FLOAT64" => "DOUBLE",
                        "INT64" => "BIGINT",
                        "DATETIME2" or "TIMESTAMP_NTZ" => "TIMESTAMP",
                        "DATETIMEOFFSET" or "TIMESTAMPTZ" or "TIMESTAMP_TZ" => "TIMESTAMP",
                        "UNIQUEIDENTIFIER" => "UUID",
                        "NUMBER" => Sized("DECIMAL", "(18,4)"),
                        "NVARCHAR2" or "NVARCHAR" or "VARCHAR2" => "VARCHAR",
                        _ => dbType
                    };

                case DataSourceType.SnowFlake:
                    return name switch
                    {
                        "BOOL" or "BIT" => "BOOLEAN",
                        "CLOB" or "NCLOB" or "LONGTEXT" or "STRING" => "VARCHAR",
                        "BLOB" or "BYTEA" or "LONGBLOB" or "BYTES" or "VARBINARY" => "BINARY",
                        "DOUBLE PRECISION" or "FLOAT64" => "FLOAT",
                        "INT64" => "BIGINT",
                        "DATETIME2" => "TIMESTAMP_NTZ",
                        "TIMESTAMP" => "TIMESTAMP_NTZ",
                        "DATETIMEOFFSET" or "TIMESTAMPTZ" => "TIMESTAMP_TZ",
                        "UUID" or "UNIQUEIDENTIFIER" => "VARCHAR(36)",
                        "NUMBER" => Sized("NUMBER", "(18,4)"),
                        "NVARCHAR2" or "NVARCHAR" or "VARCHAR2" => "VARCHAR",
                        _ => dbType
                    };

                case DataSourceType.Spanner:
                    return name switch
                    {
                        "BOOL" or "BOOLEAN" or "BIT" => "BOOL",
                        "TEXT" or "CLOB" or "NCLOB" or "LONGTEXT" or "VARCHAR" or "NVARCHAR" or "VARCHAR2" or "NVARCHAR2" or "STRING" => "STRING(MAX)",
                        "BLOB" or "BYTEA" or "LONGBLOB" or "BINARY" or "VARBINARY" or "BYTES" => "BYTES(MAX)",
                        "DOUBLE" or "DOUBLE PRECISION" or "FLOAT" => "FLOAT64",
                        "INTEGER" or "INT" or "BIGINT" or "SMALLINT" or "TINYINT" => "INT64",
                        "TIMESTAMP2" or "DATETIME2" or "TIMESTAMP_NTZ" or "TIMESTAMPTZ" or "TIMESTAMP_TZ" or "DATETIMEOFFSET" => "TIMESTAMP",
                        "UUID" or "UNIQUEIDENTIFIER" => "STRING(36)",
                        "NUMBER" or "DECIMAL" => "NUMERIC",
                        _ => dbType
                    };

                default:
                    // SQLite, DuckDB and anything else with permissive type-name aliasing: left as
                    // received, same as before this method covered any provider beyond SQL Server.
                    return dbType;
            }
        }

        /// <summary>
        /// Fallback type mapping used when the configured type mappings cannot resolve a field.
        /// Maps .NET FullName types to a real SQL type for the target provider.
        /// </summary>
        /// <remarks>
        /// This must be provider-aware. It previously returned SQLite types unconditionally, so a
        /// SQL Server / Oracle / Postgres CREATE TABLE could be emitted with types those servers do
        /// not have, and the whole migration failed on types like BOOLEAN.
        /// </remarks>
        private static string GetFallbackDbType(string fieldtype, DataSourceType datasourceType)
        {
            if (string.IsNullOrWhiteSpace(fieldtype))
                fieldtype = "System.String";

            switch (datasourceType)
            {
                case DataSourceType.SqlServer:
                    return fieldtype switch
                    {
                        "System.Int32" or "System.Int16" => "INT",
                        "System.Byte" => "TINYINT",
                        "System.Int64" => "BIGINT",
                        "System.String" => "NVARCHAR(MAX)",
                        "System.Decimal" => "DECIMAL(18,4)",
                        "System.Double" or "System.Single" => "FLOAT",
                        "System.Boolean" => "BIT",
                        "System.DateTime" => "DATETIME2",
                        "System.DateTimeOffset" => "DATETIMEOFFSET",
                        "System.TimeSpan" => "TIME",
                        "System.Guid" => "UNIQUEIDENTIFIER",
                        "System.Byte[]" => "VARBINARY(MAX)",
                        _ => "NVARCHAR(MAX)"
                    };

                case DataSourceType.Postgre:
                    return fieldtype switch
                    {
                        "System.Int32" or "System.Int16" or "System.Byte" => "INTEGER",
                        "System.Int64" => "BIGINT",
                        "System.String" => "TEXT",
                        "System.Decimal" => "NUMERIC(18,4)",
                        "System.Double" or "System.Single" => "DOUBLE PRECISION",
                        "System.Boolean" => "BOOLEAN",
                        "System.DateTime" => "TIMESTAMP",
                        "System.DateTimeOffset" => "TIMESTAMPTZ",
                        "System.TimeSpan" => "INTERVAL",
                        "System.Guid" => "UUID",
                        "System.Byte[]" => "BYTEA",
                        _ => "TEXT"
                    };

                case DataSourceType.Mysql:
                    return fieldtype switch
                    {
                        "System.Int32" or "System.Int16" => "INT",
                        "System.Byte" => "TINYINT",
                        "System.Int64" => "BIGINT",
                        "System.String" => "TEXT",
                        "System.Decimal" => "DECIMAL(18,4)",
                        "System.Double" or "System.Single" => "DOUBLE",
                        "System.Boolean" => "TINYINT(1)",
                        "System.DateTime" or "System.DateTimeOffset" => "DATETIME",
                        "System.TimeSpan" => "TIME",
                        "System.Guid" => "CHAR(36)",
                        "System.Byte[]" => "BLOB",
                        _ => "TEXT"
                    };

                case DataSourceType.Oracle:
                    return fieldtype switch
                    {
                        "System.Int32" or "System.Int16" or "System.Byte" => "NUMBER(10)",
                        "System.Int64" => "NUMBER(19)",
                        "System.String" => "NVARCHAR2(2000)",
                        "System.Decimal" => "NUMBER(18,4)",
                        "System.Double" or "System.Single" => "BINARY_DOUBLE",
                        "System.Boolean" => "NUMBER(1)",
                        "System.DateTime" => "TIMESTAMP",
                        "System.DateTimeOffset" => "TIMESTAMP WITH TIME ZONE",
                        "System.TimeSpan" => "INTERVAL DAY TO SECOND",
                        "System.Guid" => "RAW(16)",
                        "System.Byte[]" => "BLOB",
                        _ => "NVARCHAR2(2000)"
                    };

                case DataSourceType.Cockroach:
                    // CockroachDB is PostgreSQL wire- and type-compatible; use Postgres names rather
                    // than falling through to SQLite's, which Cockroach does not all accept (SQLite's
                    // bare TEXT/REAL/BLOB affinity typing is not CockroachDB's type system). BYTES is
                    // Cockroach's own name for its binary type (BYTEA is accepted only as a
                    // Postgres-compatibility alias).
                    return fieldtype switch
                    {
                        "System.Int32" or "System.Int16" or "System.Byte" => "INTEGER",
                        "System.Int64" => "BIGINT",
                        "System.String" => "TEXT",
                        "System.Decimal" => "NUMERIC(18,4)",
                        "System.Double" or "System.Single" => "DOUBLE PRECISION",
                        "System.Boolean" => "BOOLEAN",
                        "System.DateTime" => "TIMESTAMP",
                        "System.DateTimeOffset" => "TIMESTAMPTZ",
                        "System.TimeSpan" => "INTERVAL",
                        "System.Guid" => "UUID",
                        "System.Byte[]" => "BYTES",
                        _ => "TEXT"
                    };

                case DataSourceType.Hana:
                    // SAP HANA has no bare TEXT/REAL/BLOB type names; NVARCHAR(5000) is HANA's
                    // in-row string cap (NCLOB exists for larger values but is not needed for the
                    // typical entity field this method is sizing). HANA has no native GUID type;
                    // VARCHAR(36) is the conventional string-form mapping other HANA tooling uses.
                    return fieldtype switch
                    {
                        "System.Int32" or "System.Int16" => "INTEGER",
                        "System.Byte" => "TINYINT",
                        "System.Int64" => "BIGINT",
                        "System.String" => "NVARCHAR(5000)",
                        "System.Decimal" => "DECIMAL(18,4)",
                        "System.Double" or "System.Single" => "DOUBLE",
                        "System.Boolean" => "BOOLEAN",
                        "System.DateTime" or "System.DateTimeOffset" => "TIMESTAMP",
                        "System.Guid" => "VARCHAR(36)",
                        "System.Byte[]" => "VARBINARY(5000)",
                        _ => "NVARCHAR(5000)"
                    };

                case DataSourceType.FireBird:
                    // Firebird recognises neither a bare TEXT nor a bare BLOB-as-binary the way
                    // SQLite does; BLOB SUB_TYPE TEXT is Firebird's own unbounded-text type, and
                    // BLOB SUB_TYPE 0 (the default) is its binary one. Firebird added BOOLEAN in 3.0;
                    // every driver this base class ships a config for is 3.0+.
                    return fieldtype switch
                    {
                        "System.Int32" or "System.Int16" or "System.Byte" => "INTEGER",
                        "System.Int64" => "BIGINT",
                        "System.String" => "BLOB SUB_TYPE TEXT",
                        "System.Decimal" => "NUMERIC(18,4)",
                        "System.Double" or "System.Single" => "DOUBLE PRECISION",
                        "System.Boolean" => "BOOLEAN",
                        "System.DateTime" or "System.DateTimeOffset" => "TIMESTAMP",
                        "System.Guid" => "CHAR(36)",
                        "System.Byte[]" => "BLOB",
                        _ => "BLOB SUB_TYPE TEXT"
                    };

                case DataSourceType.Presto:
                case DataSourceType.Trino:
                    // Presto/Trino have no TEXT, REAL-as-affinity or BLOB type names; VARCHAR with no
                    // length is unbounded, and both have a native UUID type unlike most engines here.
                    return fieldtype switch
                    {
                        "System.Int32" or "System.Int16" or "System.Byte" => "INTEGER",
                        "System.Int64" => "BIGINT",
                        "System.String" => "VARCHAR",
                        "System.Decimal" => "DECIMAL(18,4)",
                        "System.Double" or "System.Single" => "DOUBLE",
                        "System.Boolean" => "BOOLEAN",
                        "System.DateTime" or "System.DateTimeOffset" => "TIMESTAMP",
                        "System.Guid" => "UUID",
                        "System.Byte[]" => "VARBINARY",
                        _ => "VARCHAR"
                    };

                case DataSourceType.SnowFlake:
                    // Snowflake is unusually permissive about type-name aliases (it does accept TEXT
                    // and REAL), but BLOB is not one of its aliases -- its binary type is BINARY.
                    // Unqualified VARCHAR/NUMBER take Snowflake's own defaults (16 MB / 38,0), which
                    // is more headroom than the fixed sizes other engines need here.
                    return fieldtype switch
                    {
                        "System.Int32" or "System.Int16" or "System.Byte" => "INTEGER",
                        "System.Int64" => "BIGINT",
                        "System.String" => "VARCHAR",
                        "System.Decimal" => "NUMBER(18,4)",
                        "System.Double" or "System.Single" => "FLOAT",
                        "System.Boolean" => "BOOLEAN",
                        "System.DateTime" => "TIMESTAMP_NTZ",
                        "System.DateTimeOffset" => "TIMESTAMP_TZ",
                        "System.Guid" => "VARCHAR(36)",
                        "System.Byte[]" => "BINARY",
                        _ => "VARCHAR"
                    };

                case DataSourceType.Spanner:
                    // Spanner's type vocabulary shares essentially no names with SQLite's affinities
                    // (INT64/STRING/FLOAT64/BOOL/BYTES, not INTEGER/TEXT/REAL/BOOLEAN/BLOB) -- the
                    // fallback this replaces would have produced a CREATE TABLE Spanner outright
                    // rejects, not merely a suboptimal one. STRING/BYTES require an explicit length;
                    // MAX is Spanner's own token for "unbounded".
                    return fieldtype switch
                    {
                        "System.Int32" or "System.Int16" or "System.Byte" or "System.Int64" => "INT64",
                        "System.String" => "STRING(MAX)",
                        "System.Decimal" => "NUMERIC",
                        "System.Double" or "System.Single" => "FLOAT64",
                        "System.Boolean" => "BOOL",
                        "System.DateTime" or "System.DateTimeOffset" => "TIMESTAMP",
                        "System.Guid" => "STRING(36)",
                        "System.Byte[]" => "BYTES(MAX)",
                        _ => "STRING(MAX)"
                    };

                default:
                    // SQLite, DuckDB and anything else with permissive, SQLite-like type affinities
                    // (DuckDB accepts TEXT/REAL/BLOB as aliases for VARCHAR/DOUBLE/BLOB natively).
                    // Deliberately NOT extended to Firebolt or SQL Server Compact: Firebolt's exact
                    // DDL type vocabulary was not verified against documentation with the same
                    // confidence as the branches above, and SQL Compact has no reachable ADO.NET
                    // provider in this environment to verify against (see the Tier 5 register).
                    return fieldtype switch
                    {
                        "System.Int32" or "System.Int16" or "System.Byte" => "INTEGER",
                        "System.Int64" => "BIGINT",
                        "System.String" => "TEXT",
                        "System.Decimal" or "System.Double" or "System.Single" => "REAL",
                        "System.Boolean" => "INTEGER",
                        "System.DateTime" or "System.DateTimeOffset" => "TEXT",
                        "System.Guid" => "TEXT",
                        "System.Byte[]" => "BLOB",
                        _ => "TEXT"
                    };
            }
        }

        private string CreateEntity(EntityStructure t1)
        {
            string createtablestring = null;
            SetSuccess();
            try
            {
                createtablestring = GenerateCreateEntityScript(t1);
            }
            catch (System.Exception ex)
            {
                createtablestring = null;
                // "({ex.Message})" sat in a segment with no $ on it, so the log printed that text
                // literally and the exception message never appeared anywhere (CS0168).
                DMEEditor?.AddLogMessage("Fail", $"Error in Creating Table {t1.EntityName} ({ex.Message})", DateTime.Now, 0, "", Errors.Failed);
            }
            return createtablestring;
        }

    }
}
