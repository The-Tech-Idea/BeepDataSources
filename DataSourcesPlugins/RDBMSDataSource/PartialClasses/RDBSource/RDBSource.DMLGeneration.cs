using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Data.SqlTypes;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.DataBase
{
    public partial class RDBSource : IRDBSource
    {
        /// <summary>
        /// Creates and adds parameters to a database command based on the provided DataRow and EntityStructure.
        /// </summary>
        /// <param name="command">The database command to add parameters to.</param>
        /// <param name="r">The DataRow containing parameter values.</param>
        /// <param name="DataStruct">The EntityStructure defining the structure of the entity.</param>
        /// <returns>The updated IDbCommand with parameters added.</returns>
        private IDbCommand CreateCommandParameters(IDbCommand command, DataRow r, EntityStructure DataStruct)
        {
            command.Parameters.Clear();

            foreach (EntityField item in DataStruct.Fields.OrderBy(o => o.fieldname))
            {

                if (!command.Parameters.Contains("p_" + Regex.Replace(item.fieldname, @"\s+", "_")))
                {
                    IDbDataParameter parameter = command.CreateParameter();
                    switch (item.fieldtype)
                    {
                        case "System.DateTime":
                            parameter.DbType = DbType.DateTime;  // Set this once as it's common for both branches

                            if (r[item.fieldname] == DBNull.Value || string.IsNullOrWhiteSpace(r[item.fieldname].ToString()))
                            {
                                parameter.Value = DBNull.Value;
                            }
                            else
                            {
                                if (DateTime.TryParse(r[item.fieldname].ToString(), out DateTime dateValue))
                                {
                                    // Ensuring the DateTime Kind is correctly set
                                    if (dateValue.Kind == DateTimeKind.Unspecified)
                                    {
                                        // Assuming the unspecified DateTime is in UTC as required by PostgreSQL
                                        dateValue = DateTime.SpecifyKind(dateValue, DateTimeKind.Utc);
                                    }
                                    else if (dateValue.Kind == DateTimeKind.Local)
                                    {
                                        // Convert local DateTime to UTC
                                        dateValue = dateValue.ToUniversalTime();
                                    }
                                    parameter.Value = dateValue;
                                }
                                else
                                {
                                    parameter.Value = DBNull.Value;
                                }
                            }

                            break;
                        case "System.Double":
                            parameter.DbType = DbType.Double;
                            parameter.Value = Convert.ToDouble(r[item.fieldname]);
                            break;
                        case "System.Single": // Single is equivalent to float in C#
                            parameter.DbType = DbType.Single;
                            parameter.Value = Convert.ToSingle(r[item.fieldname]);
                            break;
                        case "System.Byte":
                            parameter.DbType = DbType.Byte;
                            parameter.Value = Convert.ToByte(r[item.fieldname]);
                            break;
                        case "System.Guid":
                            parameter.DbType = DbType.Guid;
                            parameter.Value = Guid.Parse(r[item.fieldname].ToString());
                            break;
                        case "System.String":  // For VARCHAR2 and NVARCHAR2
                            parameter.DbType = DbType.String;
                            parameter.Value = r[item.fieldname] ?? DBNull.Value;
                            break;
                        case "System.Decimal":  // For NUMBER without scale
                            parameter.DbType = DbType.Decimal;
                            parameter.Value = r.IsNull(item.fieldname) ? DBNull.Value : (object)Convert.ToDecimal(r[item.fieldname]);
                            break;
                        case "System.Int32":  // For NUMBER that fits into Int32
                            parameter.DbType = DbType.Int32;
                            parameter.Value = r.IsNull(item.fieldname) ? DBNull.Value : (object)Convert.ToInt32(r[item.fieldname]);
                            break;
                        case "System.Int64":  // For NUMBER that fits into Int64
                            parameter.DbType = DbType.Int64;
                            parameter.Value = r.IsNull(item.fieldname) ? DBNull.Value : (object)Convert.ToInt64(r[item.fieldname]);
                            break;
                        case "System.Boolean":  // If you have a boolean in .NET mapped to VARCHAR2(3 CHAR) in Oracle
                            parameter.DbType = DbType.Boolean;
                            parameter.Value = r.IsNull(item.fieldname) ? DBNull.Value : (object)Convert.ToBoolean(r[item.fieldname]);
                            break;
                        // Add more cases as needed for other types
                        default:
                            parameter.Value = r.IsNull(item.fieldname) ? DBNull.Value : r[item.fieldname];
                            break;
                    }
                    parameter.ParameterName = "p_" + Regex.Replace(item.fieldname, @"\s+", "_");
                    //   parameter.DbType = TypeToDbType(tb.Columns[item.fieldname].DataType);
                    command.Parameters.Add(parameter);
                }

            }
            return command;
        }
        private IDbCommand CreateCommandParameters(IDbCommand command, object InsertedData, EntityStructure DataStruct)
        {

            foreach (var field in DataStruct.Fields.OrderBy(o => o.fieldname))
            {
                // Skip auto-increment (identity) fields
                if (field.IsAutoIncrement)
                {
                    continue;
                }

                var property = InsertedData.GetType().GetProperty(field.fieldname);
                if (property != null)
                {
                    var value = InsertedData.GetType().GetProperty(field.fieldname).GetValue(InsertedData) ?? DBNull.Value;
                    var parameter = command.CreateParameter();

                    // Find the corresponding parameter name in usedParameterNames
                    string paramName = Regex.Replace(field.fieldname, @"\s+", "_");
                    if (paramName.Length > 30)
                    {
                        paramName = paramName.Substring(0, 30);
                    }

                    string matchingParamName = usedParameterNames.FirstOrDefault(p => p.StartsWith(paramName));
                    if (string.IsNullOrEmpty(matchingParamName))
                    {
                        throw new InvalidOperationException($"Parameter name for field '{field.fieldname}' not found in usedParameterNames.");
                    }

                    parameter.ParameterName = $"{ParameterDelimiter}p_" + matchingParamName;


                    parameter.DbType = GetDbType(field.fieldtype);
                    if (value != DBNull.Value && value.GetType() != typeof(DBNull))
                    {
                        parameter.Value = ConvertToDbTypeValue(value, field.fieldtype);
                    }
                    else
                    {
                        parameter.Value = DBNull.Value;
                    }
                    command.Parameters.Add(parameter);
                }
            }

            return command;
        }
        private IDbCommand CreateUpdateCommandParameters(IDbCommand command, object InsertedData, EntityStructure DataStruct)
        {
            for (int i = 0; i < UpdateFieldSequnce.Count; i++)
            {
                EntityField field = UpdateFieldSequnce[i];
                // Skip auto-increment (identity) fields
                if (field.IsAutoIncrement)
                {
                    continue;
                }

                var property = InsertedData.GetType().GetProperty(field.fieldname);
                if (property != null)
                {
                    var value = InsertedData.GetType().GetProperty(field.fieldname).GetValue(InsertedData) ?? DBNull.Value;
                    var parameter = command.CreateParameter();

                    // Find the corresponding parameter name in usedParameterNames
                    string paramName = Regex.Replace(field.fieldname, @"\s+", "_");
                    if (paramName.Length > 30)
                    {
                        paramName = paramName.Substring(0, 30);
                    }

                    string matchingParamName = usedParameterNames.FirstOrDefault(p => p.StartsWith(paramName));
                    if (string.IsNullOrEmpty(matchingParamName))
                    {
                        throw new InvalidOperationException($"Parameter name for field '{field.fieldname}' not found in usedParameterNames.");
                    }

                    parameter.ParameterName = $"{ParameterDelimiter}p_" + matchingParamName;


                    parameter.DbType = GetDbType(field.fieldtype);
                    if (value != DBNull.Value && value.GetType() != typeof(DBNull))
                    {
                        parameter.Value = ConvertToDbTypeValue(value, field.fieldtype);
                    }
                    else
                    {
                        parameter.Value = DBNull.Value;
                    }
                    command.Parameters.Add(parameter);
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

            foreach (EntityField field in DataStruct.PrimaryKeys.OrderBy(o => o.fieldname))
            {

                var property = r.GetType().GetProperty(field.fieldname);
                if (property != null)
                {
                    var value = property.GetValue(r);
                    var parameter = command.CreateParameter();

                    // Find the corresponding parameter name in usedParameterNames
                    string paramName = Regex.Replace(field.fieldname, @"\s+", "_");
                    if (paramName.Length > 30)
                    {
                        paramName = paramName.Substring(0, 30);
                    }

                    string matchingParamName = usedParameterNames.FirstOrDefault(p => p.StartsWith(paramName));
                    if (string.IsNullOrEmpty(matchingParamName))
                    {
                        throw new InvalidOperationException($"Parameter name for field '{field.fieldname}' not found in usedParameterNames.");
                    }

                    parameter.ParameterName = $"{ParameterDelimiter}p_" + matchingParamName;
                    parameter.Value = value ?? DBNull.Value;
                    parameter.DbType = GetDbType(field.fieldtype);
                    if (value != DBNull.Value && value.GetType() != typeof(DBNull))
                    {
                        parameter.Value = ConvertToDbTypeValue(value, field.fieldtype);
                    }
                    else
                    {
                        parameter.Value = DBNull.Value;
                    }

                    command.Parameters.Add(parameter);
                }

            }
            return command;
        }

        public virtual string GetInsertString(string EntityName, EntityStructure DataStruct)
        {
            List<EntityField> SourceEntityFields = new List<EntityField>();
            List<EntityField> DestEntityFields = new List<EntityField>();

            string Insertstr = "INSERT INTO " + EntityName + " (";
            Insertstr = GetTableName(Insertstr.ToLower());
            string Valuestr = ") VALUES (";

            int t = 0;
            foreach (EntityField item in DataStruct.Fields.OrderBy(o => o.fieldname))
            {
                if (!(item.IsAutoIncrement))
                {
                    string fieldName = GetFieldName(item.fieldname);
                    string paramName = Regex.Replace(item.fieldname, @"\s+", "_");

                    // Ensure the field name and parameter name are within the Oracle identifier length limit
                    if (fieldName.Length > 30)
                    {
                        fieldName = fieldName.Substring(0, 30);
                    }

                    if (paramName.Length > 30)
                    {
                        paramName = paramName.Substring(0, 30);
                    }

                    // Ensure unique parameter names
                    int suffix = 1;
                    string originalParamName = paramName;
                    while (usedParameterNames.Contains(paramName))
                    {
                        paramName = originalParamName + "_" + suffix++;
                    }
                    usedParameterNames.Add(paramName);

                    Insertstr += $"{fieldName},";
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

            string Updatestr = @"Update " + EntityName + " set " + Environment.NewLine;
            //      Updatestr = GetTableName(Updatestr.ToLower());
            string Valuestr = "";
            // i want a new list of fields that are the primary key at the end of the list
            UpdateFieldSequnce = new List<EntityField>();
            for (int i = 0; i < DataStruct.Fields.Count; i++)
            {
                EntityField field = DataStruct.Fields[i];
                if (!DataStruct.PrimaryKeys.Any(l => l.fieldname == field.fieldname))
                {
                    UpdateFieldSequnce.Add(field);
                }
            }
            for (int i = 0; i < UpdateFieldSequnce.Count; i++)
            {
                EntityField item = UpdateFieldSequnce[i];
                if (!DataStruct.PrimaryKeys.Any(l => l.fieldname == item.fieldname))
                {
                    string fieldName = GetFieldName(item.fieldname);
                    string paramName = Regex.Replace(item.fieldname, @"\s+", "_");

                    // Ensure the field name and parameter name are within the Oracle identifier length limit
                    if (fieldName.Length > 30)
                    {
                        fieldName = fieldName.Substring(0, 30);
                    }

                    if (paramName.Length > 30)
                    {
                        paramName = paramName.Substring(0, 30);
                    }

                    // Ensure unique parameter names
                    int suffix = 1;
                    string originalParamName = paramName;
                    while (usedParameterNames.Contains(paramName))
                    {
                        paramName = originalParamName + "_" + suffix++;
                    }
                    usedParameterNames.Add(paramName);

                    Updatestr += $"{GetFieldName(item.fieldname)}= {ParameterDelimiter}p_{paramName},";
                }
            }



            Updatestr = Updatestr.Remove(Updatestr.Length - 1); // Remove the trailing comma
            UpdateFieldSequnce.AddRange(DataStruct.PrimaryKeys);
            Updatestr += @" where " + Environment.NewLine;
            int t = 1;
            for (int i = 0; i < DataStruct.PrimaryKeys.Count; i++)
            {
                EntityField item = DataStruct.PrimaryKeys[i];
                string fieldName = GetFieldName(item.fieldname);
                string paramName = Regex.Replace(item.fieldname, @"\s+", "_");
                if (usedParameterNames.Contains(paramName))
                {
                    paramName = usedParameterNames.FirstOrDefault(p => p.Contains(paramName));
                }
                else
                {
                    // Ensure the field name and parameter name are within the Oracle identifier length limit
                    if (fieldName.Length > 30)
                    {
                        fieldName = fieldName.Substring(0, 30);
                    }

                    if (paramName.Length > 30)
                    {
                        paramName = paramName.Substring(0, 30);
                    }

                    // Ensure unique parameter names
                    int suffix = 1;
                    string originalParamName = paramName;
                    while (usedParameterNames.Contains(paramName))
                    {
                        paramName = originalParamName + "_" + suffix++;
                    }
                    usedParameterNames.Add(paramName);
                }

                if (t == 1)
                {
                    Updatestr += $"{GetFieldName(item.fieldname)}= {ParameterDelimiter}p_{paramName}";
                }
                else
                {
                    Updatestr += $" and {GetFieldName(item.fieldname)}= {ParameterDelimiter}p_{paramName}";
                }
                t += 1;
            }

            return Updatestr;
        }
        public virtual string GetDeleteString(string EntityName, EntityStructure DataStruct)
        {
            string deleteStr = $"DELETE FROM {EntityName} WHERE ";
            int t = 1;
            foreach (EntityField item in DataStruct.PrimaryKeys.OrderBy(o => o.fieldname))
            {
                string fieldName = GetFieldName(item.fieldname);
                string paramName = Regex.Replace(item.fieldname, @"\s+", "_");

                // Ensure the field name and parameter name are within the Oracle identifier length limit
                if (fieldName.Length > 30)
                {
                    fieldName = fieldName.Substring(0, 30);
                }

                if (paramName.Length > 30)
                {
                    paramName = paramName.Substring(0, 30);
                }

                // Ensure unique parameter names
                int suffix = 1;
                string originalParamName = paramName;
                while (usedParameterNames.Contains(paramName))
                {
                    paramName = originalParamName + "_" + suffix++;
                }
                usedParameterNames.Add(paramName);
                if (t > 1)
                {
                    deleteStr += " AND ";
                }
                deleteStr += $"{GetFieldName(item.fieldname)} = {ParameterDelimiter}p_{paramName}";
                t += 1;
            }
            return deleteStr;
        }

        private string GenerateCreateEntityScript(EntityStructure t1)
        {
            string createtablestring = "Create table ";
            try
            {//-- Create Create string
                t1.EntityName = Regex.Replace(t1.EntityName, @"\s+", "_");
                createtablestring += " " + t1.EntityName + "\n(";

                if (t1.Fields.Count == 0)
                {
                    // Empty fields collection, add error log
                    DMEEditor.AddLogMessage("Fail", $"No fields defined for entity {t1.EntityName}", DateTime.Now, 0, t1.EntityName, Errors.Failed);
                    return createtablestring + ")";
                }

                // Filter out fields with empty names before calculating total
                var validFields = t1.Fields.Where(p => !string.IsNullOrEmpty(p.fieldname?.Trim())).ToList();
                int totalValidFields = validFields.Count;

                if (totalValidFields == 0)
                {
                    DMEEditor.AddLogMessage("Fail", $"All field names are empty for {t1.EntityName}", DateTime.Now, 0, t1.EntityName, Errors.Failed);
                    return createtablestring + ")";
                }

                int processedFields = 0;

                foreach (EntityField dbf in t1.Fields)
                {
                    // Skip fields with empty names
                    if (string.IsNullOrEmpty(dbf.fieldname))
                    {
                        DMEEditor.AddLogMessage("Fail", $"Field Name is empty for {t1.EntityName}", DateTime.Now, 0, t1.EntityName, Errors.Failed);
                        continue;
                    }

                    string fieldName = dbf.fieldname;
                    if (DatasourceType == DataSourceType.Mysql)
                    {
                        fieldName = fieldName.Replace(" ", "_");
                        fieldName = "`" + fieldName + "`";
                    }

                    createtablestring += "\n " + fieldName + " " + DMEEditor.typesHelper.GetDataType(DatasourceName, dbf) + " ";

                    if (dbf.IsAutoIncrement)
                    {
                        string autonumberstring = CreateAutoNumber(dbf);
                        if (DMEEditor.ErrorObject.Flag == Errors.Ok)
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
                if (t1.PrimaryKeys != null && t1.PrimaryKeys.Count > 0)
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
                DMEEditor.AddLogMessage("Fail", $"Error Creating Entity {t1.EntityName} ({ex.Message})", DateTime.Now, 0, "", Errors.Failed);
                createtablestring = "";
            }

            return createtablestring;
        }

        public List<ETLScriptDet> GenerateCreatEntityScript(List<EntityStructure> entities)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            int i = 0;
            List<ETLScriptDet> rt = new List<ETLScriptDet>();
            try
            {
                // Generate Create Table First
                foreach (EntityStructure item in entities)
                {
                    ETLScriptDet x = new ETLScriptDet();
                    x.destinationdatasourcename = DatasourceName;

                    x.ddl = CreateEntity(item);
                    x.sourceentityname = item.EntityName;
                    x.sourceDatasourceEntityName = item.DatasourceEntityName;
                    x.scriptType = DDLScriptType.CreateEntity;
                    rt.Add(x);
                    rt.AddRange(CreateForKeyRelationScripts(item));
                    i += 1;
                }
            }
            catch (Exception ex)
            {
                string errmsg = "Error in Generating Script";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);

            }
            return rt;

        }
        public List<ETLScriptDet> GenerateCreatEntityScript(EntityStructure entity)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;

            int i = 0;

            List<ETLScriptDet> rt = new List<ETLScriptDet>();
            try
            {
                // Generate Create Table First

                ETLScriptDet x = new ETLScriptDet();
                x.destinationdatasourcename = DatasourceName;
                x.ddl = CreateEntity(entity);
                x.sourceDatasourceEntityName = entity.DatasourceEntityName;
                x.sourceentityname = entity.EntityName;
                x.scriptType = DDLScriptType.CreateEntity;
                rt.Add(x);
                rt.AddRange(CreateForKeyRelationScripts(entity));
            }
            catch (Exception ex)
            {
                string errmsg = "Error in Generating Script";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);

            }
            return rt;

        }
        private List<ETLScriptDet> GetDDLScriptfromDatabase(string entity)
        {
            List<ETLScriptDet> rt = new List<ETLScriptDet>();

            try
            {
                var t = Task.Run<EntityStructure>(() => { return GetEntityStructure(entity, true); });
                t.Wait();
                EntityStructure entstructure = t.Result;
                entstructure.IsCreated = false;
                if (DMEEditor.ErrorObject.Flag == Errors.Ok)
                {
                    Entities[Entities.FindIndex(x => x.EntityName == entity)] = entstructure;

                }
                else
                {
                    DMEEditor.AddLogMessage("Fail", $"Error getting entity structure for {entity}", DateTime.Now, entstructure.Id, entstructure.DataSourceID, Errors.Failed);
                }
                var t2 = Task.Run<List<ETLScriptDet>>(() => { return GenerateCreatEntityScript(entstructure); });
                t2.Wait();
                rt.AddRange(t2.Result);
                t2 = Task.Run<List<ETLScriptDet>>(() => { return CreateForKeyRelationScripts(entstructure); });
                t2.Wait();
                rt.AddRange(t2.Result);
            }
            catch (System.Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error in getting entities from Database ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);
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
                    var t = Task.Run<List<ETLScriptDet>>(() => { return GenerateCreatEntityScript(structureentities); });
                    t.Wait();
                    rt.AddRange(t.Result);
                }
            }
            catch (System.Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error in getting entities from Database ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);
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
                    retval += dbf.fieldname + ",";

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
                DMEEditor.AddLogMessage(ex.Message, "Could not  Create Primery Key" + mes, DateTime.Now, -1, mes, Errors.Failed);
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
                DMEEditor.AddLogMessage(ex.Message, "Could not Create Relation" + mes, DateTime.Now, -1, mes, Errors.Failed);
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
                            x.destinationdatasourcename = DatasourceName;
                            ds = DMEEditor.GetDataSource(entity.DataSourceID);
                            x.sourceDatasourceEntityName = entity.DatasourceEntityName;
                            x.ddl = rl;
                            x.sourceentityname = entity.EntityName;
                            x.scriptType = DDLScriptType.AlterFor;
                            rt.Add(x);
                        }
                        i += 1;
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error in getting For. Keys from Database ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);
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
                            x.destinationdatasourcename = item.DataSourceID;
                            ds = DMEEditor.GetDataSource(item.DataSourceID);
                            x.sourceDatasourceEntityName = item.DatasourceEntityName;
                            x.ddl = CreateAlterRalationString(item);
                            x.sourceentityname = item.EntityName;
                            x.scriptType = DDLScriptType.AlterFor;
                            rt.Add(x);
                            //alteraddForignKey.Add(x);
                            i += 1;
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error in getting For. Keys from Database ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);

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
                    switch (Dataconnection.ConnectionProp.DatabaseType)
                    {
                        //case DataSourceType.Excel:
                        //    break;
                        case DataSourceType.Mysql:
                            AutnumberString = "NULL AUTO_INCREMENT";
                            break;
                        case DataSourceType.Oracle:
                            AutnumberString = " GENERATED BY DEFAULT ON NULL AS IDENTITY";// "CREATE SEQUENCE " + f.fieldname + "_seq MINVALUE 1 START WITH 1 INCREMENT BY 1 CACHE 1 ";
                            break;
                        case DataSourceType.SqlCompact:
                            AutnumberString = "IDENTITY(1,1)";
                            break;
                        case DataSourceType.SqlLite:
                            AutnumberString = "AUTOINCREMENT";
                            break;
                        case DataSourceType.SqlServer:
                            AutnumberString = "IDENTITY(1,1)";
                            break;
                        default:
                            AutnumberString = "";
                            break;
                    }
                }
            }
            catch (System.Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error Creating Auto number Field {f.EntityName} and {f.fieldname} ({ex.Message})", DateTime.Now, 0, "", Errors.Failed);

            }
            return AutnumberString;
        }
        private string CreateEntity(EntityStructure t1)
        {
            string createtablestring = null;
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                createtablestring = GenerateCreateEntityScript(t1);
            }
            catch (System.Exception ex)
            {
                createtablestring = null;
                DMEEditor.AddLogMessage("Fail", $"Error in  Creating Table " + t1.EntityName + "   ({ex.Message})", DateTime.Now, 0, "", Errors.Failed);
            }
            return createtablestring;
        }
    }
}
