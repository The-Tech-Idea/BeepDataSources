using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Data.Common;
using System.Diagnostics;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.DataBase
{
    public partial class RDBSource : IRDBSource
    {
        #region "Get Entity Structure"
        // <summary>
        /// Retrieves the detailed structure of an entity, including its fields, primary keys, and relationships.
        /// It optionally refreshes the entity structure if the 'refresh' parameter is true.
        /// </summary>
        /// <param name="fnd">The entity structure to be filled or refreshed.</param>
        /// <param name="refresh">Boolean flag indicating whether to refresh the entity's metadata.</param>
        /// <returns>The updated or refreshed EntityStructure object.</returns>
        public virtual EntityStructure GetEntityStructure(string EntityName, bool refresh = false)
        {
            EntityStructure retval = new EntityStructure();

            if (Entities.Count == 0)
            {
                GetEntitesList();
            }
            retval = Entities.FirstOrDefault(d => d.EntityName.Equals(EntityName, StringComparison.InvariantCultureIgnoreCase));
            //if (retval == null)
            //{
            //    List<EntityStructure> ls = Entities.Where(d => !string.IsNullOrEmpty(d.OriginalEntityName)).ToList();
            //    retval = ls.Where(d => d.OriginalEntityName.Equals(EntityName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            //}

            if (retval == null)
            {
                retval = new EntityStructure();
                refresh = true;
                retval.DataSourceID = DatasourceName;
                retval.EntityName = EntityName;
                retval.DatasourceEntityName = EntityName;
                retval.Caption = EntityName;

                if (RDBMSHelper.IsSqlStatementValid(EntityName))
                {
                    retval.Viewtype = ViewType.Query;
                    retval.CustomBuildQuery = EntityName;
                }
                else
                {
                    retval.Viewtype = ViewType.Table;
                    retval.CustomBuildQuery = null;
                }
                refresh = true;
            }



            return GetEntityStructure(retval, refresh);
        }
        private bool GetBooleanField(DataRow r, string fieldName)
        {
            try
            {
                return r.Field<bool>(fieldName);
            }
            catch
            {
                return false;
            }
        }

        private bool IsNumericType(string fieldType)
        {
            return fieldType == "System.Decimal" || fieldType == "System.Float" || fieldType == "System.Double";
        }
        // helper to read a typed column only if it exists (otherwise return default)
        private static T SafeField<T>(DataRow row, string colName, T defaultValue = default)
        {
            if (row.Table.Columns.Contains(colName) && !row.IsNull(colName))
                return row.Field<T>(colName);
            return defaultValue;
        }

        public virtual EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            DataTable tb = new DataTable();
            string entname = fnd.EntityName;
            if (string.IsNullOrEmpty(fnd.DatasourceEntityName))
            {
                fnd.DatasourceEntityName = fnd.EntityName;
            }
            //if (fnd.Created == false && fnd.Viewtype!= ViewType.Table)
            //{
            //    fnd.Created = false;
            //    fnd.Drawn = false;
            //    fnd.Editable = true;
            //    return fnd;

            //}
            if (refresh)
            {
                if (!fnd.EntityName.Equals(fnd.DatasourceEntityName, StringComparison.InvariantCultureIgnoreCase) && !string.IsNullOrEmpty(fnd.DatasourceEntityName))
                {
                    entname = fnd.DatasourceEntityName;
                }
                if (string.IsNullOrEmpty(fnd.DatasourceEntityName))
                {
                    fnd.DatasourceEntityName = entname;
                }
                if (string.IsNullOrEmpty(fnd.Caption))
                {
                    fnd.Caption = entname;
                }
                //fnd.DataSourceID = DatasourceName;
                //  fnd.EntityName = EntityName;
                if (fnd.Viewtype == ViewType.Query)
                {
                    tb = GetTableSchema(fnd.CustomBuildQuery, true);
                }
                else
                {

                    tb = GetTableSchema(entname, false);
                }
                if (tb.Rows.Count > 0)
                {
                    fnd.Fields = new List<EntityField>();
                    fnd.PrimaryKeys = new List<EntityField>();
                    DataRow rt = tb.Rows[0];
                    fnd.IsCreated = true;
                    fnd.EntityType = EntityType.Table;
                    fnd.Editable = false;
                    fnd.Drawn = true;
                    foreach (DataRow r in rt.Table.Rows)
                    {
                        EntityField x = new EntityField();
                        try
                        {
                            x.fieldname = SafeField<string>(r, "ColumnName");
                            x.fieldtype = SafeField<Type>(r, "DataType")?.ToString() ?? "System.String";

                            // Oracle FLOAT → .NET mapping
                            if (DatasourceType == DataSourceType.Oracle
                             && x.fieldtype.Equals("FLOAT", StringComparison.OrdinalIgnoreCase))
                            {
                                int precision = GetFloatPrecision(x.EntityName, x.fieldname);
                                x.fieldtype = MapOracleFloatToDotNetType(precision);
                            }

                            x.Size1 = SafeField<int>(r, "ColumnSize");
                            x.IsAutoIncrement = SafeField<bool>(r, "IsAutoIncrement");
                            x.AllowDBNull = SafeField<bool>(r, "AllowDBNull");
                            x.IsIdentity = SafeField<bool>(r, "IsIdentity");
                            x.IsKey = SafeField<bool>(r, "IsKey");
                            x.IsUnique = SafeField<bool>(r, "IsUnique");
                            x.OrdinalPosition = SafeField<int>(r, "OrdinalPosition");  // no more exception

                            x.IsReadOnly = SafeField<bool>(r, "IsReadOnly");
                            x.IsRowVersion = SafeField<bool>(r, "IsRowVersion");
                            x.IsLong = SafeField<bool>(r, "IsLong");
                            x.DefaultValue = SafeField<string>(r, "DefaultValue", null);
                            x.Expression = SafeField<string>(r, "Expression", null);
                            x.BaseTableName = SafeField<string>(r, "BaseTableName", null);
                            x.BaseColumnName = SafeField<string>(r, "BaseColumnName", null);

                            // MaxLength is same as ColumnSize
                            x.MaxLength = x.Size1;
                            x.IsFixedLength = SafeField<bool>(r, "IsFixedLength");
                            x.IsHidden = SafeField<bool>(r, "IsHidden");

                            // NumericPrecision/Scale only if the schema provides them
                            if (IsNumericType(x.fieldtype))
                            {
                                x.NumericPrecision = SafeField<short>(r, "NumericPrecision");
                                x.NumericScale = SafeField<short>(r, "NumericScale");
                            }
                        }
                        catch (Exception ex)
                        {
                            DMEEditor.AddLogMessage(
                              "Fail",
                              $"Error creating Field metadata for {entname}.{x.fieldname}: {ex.Message}",
                              DateTime.Now, 0, entname, Errors.Failed
                            );
                        }

                        if (x.IsKey)
                        {
                            fnd.PrimaryKeys.Add(x);
                        }
                        fnd.Fields.Add(x);
                    }
                    if (fnd.Viewtype == ViewType.Table)
                    {
                        if ((fnd.Relations.Count == 0) || refresh)
                        {
                            fnd.Relations = new List<RelationShipKeys>();
                            fnd.Relations = (List<RelationShipKeys>)GetEntityforeignkeys(entname, Dataconnection.ConnectionProp.SchemaName);
                        }
                    }

                    //   EntityStructure exist = Entities.Where(d => d.EntityName.Equals(fnd.EntityName,StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                    int idx = Entities.FindIndex(o => o.EntityName.Equals(fnd.EntityName, StringComparison.InvariantCultureIgnoreCase));
                    if (idx == -1)
                    {
                        Entities.Add(fnd);
                    }
                    else
                    {

                        Entities[idx].IsCreated = true;
                        Entities[idx].Editable = false;
                        Entities[idx].Drawn = true;
                        Entities[idx].Fields = fnd.Fields;
                        Entities[idx].Relations = fnd.Relations;
                        Entities[idx].PrimaryKeys = fnd.PrimaryKeys;

                    }
                }
                else
                {
                    fnd.IsCreated = false;
                }

            }
            return fnd;
        }
        /// <summary>
        /// <summary>
        /// Retrieves the structure of a specific entity (e.g., a database table) using a database connection.
        /// </summary>
        /// <param name="connection">Database connection to access the schema.</param>
        /// <param name="tableName">The name of the table for which the structure is required.</param>
        /// <returns>An EntityStructure representing the table's schema.</returns>
        public EntityStructure GetEntityStructureForQuery(DbConnection connection, string query)
        {
            EntityStructure entityStructure = new EntityStructure();
            // Assuming entityStructure properties are appropriately set

            DataTable schemaTable = new DataTable();
            using (DbCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText = query;
                using (DbDataReader reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly))
                {
                    schemaTable = reader.GetSchemaTable();
                }
            }

            // Now you can map schema information to your EntityStructure or EntityField instances
            // ...

            return GetEntityStructure(schemaTable);
        }
        /// <summary>
        /// Creates an entity structure from a given schema table.
        /// </summary>
        /// <param name="schemaTable">A DataTable containing schema information.</param>
        /// <returns>The constructed EntityStructure based on the schema table.</returns>
        private EntityStructure GetEntityStructure(DataTable schemaTable)
        {
            EntityStructure entityStructure = new EntityStructure();
            string columnNameKey = "COLUMN_NAME";
            string dataTypeKey = "DATA_TYPE";
            string maxLengthKey = "CHARACTER_MAXIMUM_LENGTH";
            string numericPrecisionKey = "NUMERIC_PRECISION";
            string numericScaleKey = "NUMERIC_SCALE";
            string isNullableKey = "IS_NULLABLE";
            string isAutoIncrementKey = "AUTOINCREMENT";
            string isKeyKey = "PRIMARY_KEY";
            string isUniqueKey = "UNIQUE";
            // Add more keys for other properties

            foreach (DataRow row in schemaTable.Rows)
            {
                EntityField field = new EntityField();
                field.fieldname = row[columnNameKey].ToString();
                field.fieldtype = row[dataTypeKey].ToString();
                field.Size1 = Convert.ToInt32(row[maxLengthKey]);
                field.NumericPrecision = Convert.ToInt16(row[numericPrecisionKey]);
                field.NumericScale = Convert.ToInt16(row[numericScaleKey]);
                field.AllowDBNull = row[isNullableKey].ToString() == "YES";
                field.IsAutoIncrement = row[isAutoIncrementKey].ToString() == "YES";
                field.IsKey = row[isKeyKey].ToString() == "YES";
                field.IsUnique = row[isUniqueKey].ToString() == "YES";
                // Map other schema properties to the EntityField instance
                // ...

                entityStructure.Fields.Add(field);
            }
            return entityStructure;
        }
        public EntityStructure GetEntityStructure(DbConnection connection, string tableName)
        {
            EntityStructure entityStructure = new EntityStructure();
            entityStructure.EntityName = tableName;

            DataTable schemaTable = connection.GetSchema("Columns", new[] { null, null, tableName, null });




            return GetEntityStructure(schemaTable);
        }
        #endregion "Get Entity Structure"

        public virtual Type GetEntityType(string EntityName)
        {
            EntityStructure x = GetEntityStructure(EntityName);
            DMTypeBuilder.CreateNewObject(DMEEditor, DatasourceName, DatasourceName, EntityName, x.Fields);
            enttype = DMTypeBuilder.MyType;
            return DMTypeBuilder.MyType;
        }
        /// <summary>
        /// Retrieves a list of all entity names (like tables) from the database.
        /// </summary>
        /// <remarks>
        /// This method queries the database to get a list of all tables. It handles different schema configurations
        /// and adapts to various database types as defined in the Dataconnection's properties.
        /// </remarks>
        /// <returns>A List of strings, each representing the name of a table in the database.</returns>
        public virtual IEnumerable<string> GetEntitesList()
        {
            ErrorObject.Flag = Errors.Ok;
            DataSet ds = new DataSet();
            IDbDataAdapter adp;
            DataTable tb = new DataTable();
            try
            {
                if (Dataconnection != null)
                {
                    if (Dataconnection.ConnectionProp != null)
                    {
                        if (Dataconnection.ConnectionProp.SchemaName != null)
                        {
                            if (Dataconnection.ConnectionProp.SchemaName.Contains(','))
                            {
                                string[] schemas = Dataconnection.ConnectionProp.SchemaName.Split(',');
                            }
                        }
                    }
                }
                string sql = GetListofEntitiesSql;
                if (String.IsNullOrEmpty(sql))
                {
                    sql = DMEEditor.ConfigEditor.GetSql(Sqlcommandtype.getlistoftables, null, Dataconnection.ConnectionProp.SchemaName, null, DMEEditor.ConfigEditor.QueryList, DatasourceType);
                }

                adp = GetDataAdapter(sql, null);
                adp.Fill(ds);
#if DEBUG
                DMEEditor.AddLogMessage("Beep", $"Get Tables List Query {sql}", DateTime.Now, 0, DatasourceName, Errors.Failed);
                Debug.WriteLine($" -- Get Tables List Query {sql}");
#endif

                tb = ds.Tables[0];
                EntitiesNames = new List<string>();
                int i = 0;
                foreach (DataRow row in tb.Rows)
                {
                    EntitiesNames.Add(row.Field<string>("TABLE_NAME").ToUpper());

                    i += 1;
                }
                List<string> EntitiesnotinEntitiesNames = new List<string>();
                if (Entities.Count > 0)
                {
                    EntitiesnotinEntitiesNames = Entities.Where(p => !EntitiesNames.Contains(p.EntityName)).Select(p => p.EntityName).ToList();
                    foreach (string item in EntitiesnotinEntitiesNames)
                    {
                        int idx = Entities.FindIndex(p => p.EntityName == item);
                        Entities[idx].IsCreated = false;
                        Entities[idx].EntityType = EntityType.InMemory;
                        Entities[idx].Drawn = false;
                        // update EntitiesNames and add to the list
                        if (!EntitiesNames.Contains(item))
                        {
                            EntitiesNames.Add(item);
                        }
                    }
                }


            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", $"Error in getting  Table List ({ex.Message})", DateTime.Now, 0, DatasourceName, Errors.Failed);

            }
            tb = null;
            adp = null;
            return EntitiesNames;



        }
        // <summary>
        /// Adds a new entity to the system.
        /// </summary>
        /// <param name="entityName">The name of the new entity.</param>
        /// <param name="schemaname">The database schema name associated with the entity.</param>
        /// <returns>A string message indicating the result of the operation.</returns>
        /// <remarks>
        /// This method validates the input and adds the entity to the collection if it doesn't already exist.
        /// </remarks>
        public virtual string AddNewEntity(string entityName, string schemaname)
        {
            if (entityName == null)
            {
                return "Entity Name is null";
            }
            if (schemaname == null)
            {
                return "schema Name is null";
            }
            if (!string.IsNullOrEmpty(schemaname))
            {
                int ent = Entities.FindIndex(p => p.EntityName.ToUpper() == entityName.ToUpper());
                if (ent > -1)
                {
                    return "Entity Exist";
                }

            }
            EntityStructure entity = new EntityStructure();
            entity.EntityName = entityName;
            entity.SchemaOrOwnerOrDatabase = schemaname;
            Entities.Add(entity);
            return null;
        }
        /// <summary>
        /// Retrieves the schema name from the connection properties.
        /// </summary>
        /// <returns>The schema name as a string.</returns>
        /// <remarks>
        /// If the schema name is not explicitly set in the connection properties, defaults are used based on the database type.
        /// </remarks>
        public virtual string GetSchemaName()
        {
            string schemaname = null;

            if (!string.IsNullOrEmpty(Dataconnection.ConnectionProp.SchemaName))
            {
                schemaname = Dataconnection.ConnectionProp.SchemaName.ToUpper();
            }
            if (Dataconnection.ConnectionProp.DatabaseType == DataSourceType.SqlServer && string.IsNullOrEmpty(Dataconnection.ConnectionProp.SchemaName))
            {
                schemaname = "dbo";
            }
            return schemaname;
        }
        /// <summary>
        /// Checks if an entity exists in the system.
        /// </summary>
        /// <param name="EntityName">The name of the entity to check.</param>
        /// <returns>True if the entity exists, otherwise false.</returns>
        /// <remarks>
        /// This method checks for the existence of an entity by its name in the Entities collection.
        /// </remarks>
        public bool CheckEntityExist(string EntityName)
        {
            bool retval = false;
            GetEntitesList();
            if (EntitiesNames.Count == 0)
            {
                retval = false;
            }
            if (Entities.Count > 0)
            {
                retval = Entities.Any(p => p.EntityName == EntityName || p.OriginalEntityName == EntityName || p.DatasourceEntityName == EntityName);
            }

            return retval;
        }
        /// <summary>
        /// Retrieves the index of a specific entity in the Entities collection.
        /// </summary>
        /// <param name="entityName">The name of the entity.</param>
        /// <returns>The index of the entity or -1 if not found.</returns>
        public int GetEntityIdx(string entityName)
        {
            if (Entities.Count > 0)
            {
                return Entities.FindIndex(p => p.EntityName.Equals(entityName, StringComparison.InvariantCultureIgnoreCase) || p.DatasourceEntityName.Equals(entityName, StringComparison.InvariantCultureIgnoreCase));
            }
            else
            {
                return -1;
            }
        }
        /// <summary>
        /// Creates an entity in the database as per the specified structure.
        /// </summary>
        /// <param name="entity">The entity structure to create in the database.</param>
        /// <returns>True if creation is successful, otherwise false.</returns>
        /// <remarks>
        /// This method attempts to create a new entity in the database if it does not already exist.
        /// </remarks>
        public virtual bool CreateEntityAs(EntityStructure entity)
        {
            bool retval = false;
            if (CheckEntityExist(entity.EntityName) == false)
            {
                string createstring = CreateEntity(entity);
                DMEEditor.ErrorObject = ExecuteSql(createstring);
                if (DMEEditor.ErrorObject.Flag == Errors.Failed)
                {
                    retval = false;
                }
                else
                {
                    Entities.Add(entity);
                    EntitiesNames.Add(entity.EntityName);
                    retval = true;
                }
            }


            return retval;
        }
        /// <summary>
        /// Retrieves foreign key relationships for a specific entity.
        /// </summary>
        /// <param name="entityname">The name of the entity to retrieve foreign keys for.</param>
        /// <param name="SchemaName">The database schema name.</param>
        /// <returns>A list of foreign key relationships.</returns>
        /// <remarks>
        /// This method fetches foreign key information for the given entity from the database.
        /// </remarks>
        public virtual IEnumerable<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            List<RelationShipKeys> fk = new List<RelationShipKeys>();
            ErrorObject.Flag = Errors.Ok;
            try
            {
                List<ChildRelation> ds = GetTablesFKColumnList(entityname, GetSchemaName(), null);
                //-------------------------------
                // Create Parent Record First
                //-------------------------------
                if (ds != null)
                {
                    if (ds.Count > 0)
                    {
                        foreach (ChildRelation r in ds)
                        {
                            RelationShipKeys rfk = new RelationShipKeys
                            {
                                RelatedEntityID = r.parent_table,
                                RelatedEntityColumnID = r.parent_column,
                                EntityColumnID = r.child_column,
                            };
                            try
                            {
                                rfk.RalationName = r.Constraint_Name;
                            }
                            catch (Exception ex)
                            {
                                ErrorObject.Flag = Errors.Failed;
                                ErrorObject.Ex = ex;
                            }
                            fk.Add(rfk);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Could not get forgien key  for {entityname} ({ex.Message})", DateTime.Now, 0, entityname, Errors.Failed);
            }
            return fk;
        }
        /// <summary>
        /// Retrieves a list of child tables related to the specified table.
        /// </summary>
        /// <param name="tablename">The name of the table to find child tables for.</param>
        /// <param name="SchemaName">The database schema name.</param>
        /// <param name="Filterparamters">Additional filter parameters.</param>
        /// <returns>A list of child relations.</returns>
        /// <remarks>
        /// This method provides information about child tables related to a specified table.
        /// </remarks>
        public virtual IEnumerable<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                string sql = DMEEditor.ConfigEditor.GetSql(Sqlcommandtype.getChildTable, tablename, SchemaName, Filterparamters, DMEEditor.ConfigEditor.QueryList, DatasourceType);
                if (!string.IsNullOrEmpty(sql) && !string.IsNullOrWhiteSpace(sql))
                {
                    return GetData<ChildRelation>(sql);
                }
                else
                    return null;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error in getting  child entities for {tablename} ({ex.Message})", DateTime.Now, 0, tablename, Errors.Failed);
                return null;
            }
        }
        /// <summary>
        /// Executes a provided SQL script.
        /// </summary>
        /// <param name="scripts">The script details to execute.</param>
        /// <returns>IErrorsInfo object with information about the execution outcome.</returns>
        /// <remarks>
        /// This method runs an SQL script and provides detailed information about its execution.
        /// </remarks>
        public virtual IErrorsInfo RunScript(ETLScriptDet scripts)
        {
            var t = Task.Run<IErrorsInfo>(() => { return ExecuteSql(scripts.ddl); });
            t.Wait();
            DMEEditor.ErrorObject = t.Result;
            scripts.errormessage = DMEEditor.ErrorObject.Message;

            return DMEEditor.ErrorObject;
        }
        /// <summary>
        /// Generates SQL scripts for creating entities based on their structure.
        /// </summary>
        /// <param name="entities">A list of entities to generate scripts for.</param>
        /// <returns>A list of ETLScriptDet containing the SQL create scripts.</returns>
        /// <remarks>
        /// This method is useful for generating database creation scripts from entity structures.
        /// </remarks>
        public virtual IEnumerable<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities)
        {
            return GetDDLScriptfromDatabase(entities);
        }

        public virtual DataTable GetTableSchema(string TableName, bool Isquery = false)
        {
            ErrorObject.Flag = Errors.Ok;
            DataTable tb = new DataTable();
            IDataReader reader;
            IDbCommand cmd = GetDataCommand();
            //  EntityStructure entityStructure = GetEntityStructure(TableName, false);
            try
            {
                string cmdtxt = "";
                if (!Isquery)
                {
                    if (!string.IsNullOrEmpty(Dataconnection.ConnectionProp.SchemaName) && !string.IsNullOrWhiteSpace(Dataconnection.ConnectionProp.SchemaName))
                    {
                        TableName = Dataconnection.ConnectionProp.SchemaName + "." + TableName;
                    }
                    cmdtxt = "Select * from " + TableName + " where 1=2";
                }
                else
                {
                    cmdtxt = TableName;
                }
                cmd.CommandText = cmdtxt;
                reader = cmd.ExecuteReader(CommandBehavior.KeyInfo);

                tb = reader.GetSchemaTable();
                reader.Close();
                cmd.Dispose();
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error Fetching Schema for {TableName} -{ex.Message}", DateTime.Now, 0, TableName, Errors.Failed);
            }

            return tb;
        }
        public virtual List<ChildRelation> GetTablesFKColumnList(string tablename, string SchemaName, string Filterparamters)
        {
            ErrorObject.Flag = Errors.Ok;
            DataSet ds = new DataSet();
            try
            {
                string sql = DMEEditor.ConfigEditor.GetSql(Sqlcommandtype.getFKforTable, tablename, SchemaName, Filterparamters, DMEEditor.ConfigEditor.QueryList, DatasourceType);
                if (!string.IsNullOrEmpty(sql) && !string.IsNullOrWhiteSpace(sql))
                {
                    return GetData<ChildRelation>(sql);
                }
                else
                    return null;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Unsuccessfully Retrieve Child tables list {ex.Message}", DateTime.Now, -1, ex.Message, Errors.Failed);
                return null;
            }
        }
        public virtual string DisableFKConstraints(EntityStructure t1)
        {
            // Disable all foreign key constraints
            return string.Empty;
        }
        public virtual string EnableFKConstraints(EntityStructure t1)
        {
            return string.Empty;
        }
        public static string MapOracleFloatToDotNetType(int precision)
        {
            if (precision <= 24)
            {
                // Fits in .NET float
                return "System.Single";
            }
            else if (precision <= 53)
            {
                // Fits in .NET double
                return "System.Double";
            }
            else
            {
                // Use .NET decimal for higher precision
                return "System.Decimal";
            }
        }
        public int GetFloatPrecision(string tableName, string fieldName)
        {
            int precision = 0;
            string query = $"SELECT DATA_PRECISION FROM ALL_TAB_COLUMNS WHERE TABLE_NAME = '{tableName.ToUpper()}' AND COLUMN_NAME = '{fieldName.ToUpper()}'";


            IDbCommand command = GetDataCommand();
            try
            {
                command.CommandText = query;
                IDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    // Assuming the precision is not null, adjust as needed if it could be
                    precision = reader.GetInt32(0);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                // Handle exceptions
                Console.WriteLine(ex.Message);
            }


            return precision;
        }
    }
}
