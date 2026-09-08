using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Data.Common;
using System.Diagnostics;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Helpers.RDBMSHelpers;
using TheTechIdea.Beep.Helpers.RDBMSHelpers.EntityHelpers;
using System.Threading.Tasks;

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
            // Use thread-safe cache for entity structures
            return EntityCache.Get(EntityName, refresh);
        }

        /// <summary>
        /// Internal method to load entity structure - called by EntityStructureCache.
        /// </summary>
        private EntityStructure LoadEntityStructure(string EntityName, bool refresh)
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
        private bool GetBooleanField(DataRow r, string FieldName)
        {
            try
            {
                return r.Field<bool>(FieldName);
            }
            catch
            {
                return false;
            }
        }

        private bool IsNumericType(string fieldType)
        {
            return fieldType == "System.Decimal" || fieldType == "System.Float" || fieldType == "System.Double"
                || fieldType == "System.Int16" || fieldType == "System.Int32" || fieldType == "System.Int64";
        }
        // helper to read a typed column only if it exists (otherwise return default)
        private static T SafeField<T>(DataRow row, string colName, T defaultValue = default)
        {
            if (row.Table.Columns.Contains(colName) && !row.IsNull(colName))
                return row.Field<T>(colName);
            return defaultValue;
        }
        // ADO.NET providers disagree on the NumericPrecision/NumericScale column type:
        // the schema spec says Int16 but System.Data.SQLite returns Int32, so a
        // row.Field<short> unbox throws InvalidCastException. Convert.ToInt16 handles
        // boxed Int16/Int32/Int64/string uniformly.
        private static short SafeShort(DataRow row, string colName)
        {
            if (row.Table.Columns.Contains(colName) && !row.IsNull(colName))
                return Convert.ToInt16(row[colName]);
            return 0;
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
                            x.FieldName = SafeField<string>(r, "ColumnName");
                            x.Fieldtype = SafeField<Type>(r, "DataType")?.ToString() ?? "System.String";

                            // Oracle FLOAT → .NET mapping
                            if (DatasourceType == DataSourceType.Oracle
                             && x.Fieldtype.Equals("FLOAT", StringComparison.OrdinalIgnoreCase))
                            {
                                int precision = GetFloatPrecision(x.EntityName, x.FieldName);
                                x.Fieldtype = MapOracleFloatToDotNetType(precision);
                            }

                            // Oracle NUMBER(p,0) → int/long. ODP.NET's schema table
                            // maps every NUMBER to System.Decimal; without this an
                            // INTEGER/NUMBER(10,0) key surfaces as decimal in POCOs,
                            // FieldTypeMapper and generated editors.
                            if (DatasourceType == DataSourceType.Oracle
                             && x.Fieldtype.Equals("System.Decimal", StringComparison.OrdinalIgnoreCase))
                            {
                                short numberPrecision = SafeShort(r, "NumericPrecision");
                                short numberScale = SafeShort(r, "NumericScale");
                                x.Fieldtype = MapOracleNumberToDotNetType(numberPrecision, numberScale);
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
                            if (IsNumericType(x.Fieldtype))
                            {
                                x.NumericPrecision = SafeShort(r, "NumericPrecision");
                                x.NumericScale = SafeShort(r, "NumericScale");
                            }
                        }
                        catch (Exception ex)
                        {
                            DMEEditor?.AddLogMessage(
                              "Fail",
                              $"Error creating Field metadata for {entname}.{x.FieldName}: {ex.Message}",
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
                            // ToList(), not a cast. GetEntityforeignkeys is declared
                            // IEnumerable<RelationShipKeys> and is public virtual, so any driver
                            // that overrides it to return a LINQ projection, an array or an iterator
                            // hit an InvalidCastException here at runtime — with no compile-time
                            // warning that its override had to return a List. This was the most
                            // dangerous override point in the class.
                            fnd.Relations = GetEntityforeignkeys(entname, Dataconnection.ConnectionProp.SchemaName)?.ToList()
                                            ?? new List<RelationShipKeys>();
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
                field.FieldName = row[columnNameKey].ToString();
                field.Fieldtype = row[dataTypeKey].ToString();
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
            // Take the type from the OBJECT this call returns, not from
            // DMTypeBuilder.MyType, a mutable static holding the LAST type
            // built anywhere, so any thread that resolved a different entity between
            // the call above and the read below made this method return the wrong
            // type. The returned instance is local and cannot be raced.
            // (2026-08-03)
            // Namespace the generated type "Beep." + DatasourceName, matching every other connector
            // in this repository and the rule recorded in CLAUDE.md.
            //
            // This passed a bare DatasourceName, so RDBMS entity types landed in a different
            // namespace root from all the other drivers. More importantly, DMTypeBuilder falls back
            // to the shared literal "TheTechIdea.Classes" when the namespace it is given is empty —
            // and nothing validates `datasourcename` in the constructor. Two connections with a
            // same-named entity would then have shared one generated type, which is exactly the
            // collision commit ea222c4b fixed everywhere else.
            var beepEntityType = DMTypeBuilder.CreateNewObject(DMEEditor, "Beep." + DatasourceName, EntityName, x.Fields)?.GetType();
            enttype = beepEntityType;
            return beepEntityType;
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
                    // The engine's query catalogue is the only source of this statement when a driver
                    // has not supplied GetListofEntitiesSql. Say which service is missing, rather
                    // than letting the NullReferenceException fall into the catch below and be
                    // reported as "Object reference not set" while listing tables.
                    if (DMEEditor?.ConfigEditor == null)
                    {
                        SetFailure($"Cannot list the tables in {DatasourceName}: no ConfigEditor is available to supply the query, and this driver sets no GetListofEntitiesSql.");
                        return EntitiesNames;
                    }

                    sql = DMEEditor.ConfigEditor.GetSql(Sqlcommandtype.getlistoftables, null, Dataconnection.ConnectionProp.SchemaName, null, DMEEditor.ConfigEditor.QueryList, DatasourceType);
                }

                adp = GetDataAdapter(sql, null);
                adp.Fill(ds);
#if DEBUG
                // Errors.Ok, not Errors.Failed: this is a diagnostic trace of a query that just
                // succeeded. Logging it as a failure sets ErrorObject.Flag, and every caller that
                // checks the flag afterwards — MigrationManager.CheckEntityExist among them — reads a
                // failure that never happened, in Debug builds only.
                DMEEditor?.AddLogMessage("Beep", $"Get Tables List Query {sql}", DateTime.Now, 0, DatasourceName, Errors.Ok);
                Debug.WriteLine($" -- Get Tables List Query {sql}");
#endif

                tb = ds.Tables[0];
                EntitiesNames = new List<string>();
                int i = 0;
                foreach (DataRow row in tb.Rows)
                {
                    // Keep the name exactly as the catalog reports it. The .ToUpper() that used to
                    // be here corrupts case-sensitive identifiers on PostgreSQL, Snowflake and any
                    // quoted-identifier setup.
                    EntitiesNames.Add(row.Field<string>("TABLE_NAME"));

                    i += 1;
                }
                List<string> EntitiesnotinEntitiesNames = new List<string>();
                if (Entities.Count > 0)
                {
                    // Case-insensitive. With the ordinal comparison this used to do — against a list
                    // that had just been uppercased — any cached entity whose name was not already
                    // uppercase was judged missing and demoted from a physical table to
                    // EntityType.InMemory below.
                    EntitiesnotinEntitiesNames = Entities.Where(p => !EntitiesNames.Contains(p.EntityName, StringComparer.OrdinalIgnoreCase)).Select(p => p.EntityName).ToList();
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
                // EntitiesNames keeps its previous contents and is returned regardless, so without
                // the flag a caller cannot tell a refreshed list from a stale one.
                HandleDatabaseError(ex, DatasourceName, "get the table list for");
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
        /// Checks if an entity exists in the system with name validation.
        /// </summary>
        /// <param name="EntityName">The name of the entity to check.</param>
        /// <returns>True if the entity exists, otherwise false.</returns>
        /// <remarks>
        /// This method validates entity name for SQL injection and reserved keywords before checking existence.
        /// </remarks>
        public bool CheckEntityExist(string EntityName)
        {
            // Validate entity name for security and naming conventions
            if (string.IsNullOrWhiteSpace(EntityName))
            {
                DMEEditor?.AddLogMessage("Fail", "Entity name cannot be null or empty", 
                    DateTime.Now, 0, null, Errors.Failed);
                return false;
            }

            // Check for SQL injection attempts and invalid characters
            if (!DatabaseEntityNamingValidator.IsValidIdentifier(EntityName))
            {
                DMEEditor?.AddLogMessage("Fail", $"Invalid entity name '{EntityName}' - contains invalid characters", 
                    DateTime.Now, 0, EntityName, Errors.Failed);
                return false;
            }

            // Check for reserved keywords
            if (DatabaseEntityReservedKeywordChecker.IsReservedKeyword(EntityName, DatasourceType))
            {
                DMEEditor?.AddLogMessage("Fail", $"Entity name '{EntityName}' is a reserved keyword in {DatasourceType}", 
                    DateTime.Now, 0, EntityName, Errors.Failed);
                // Return false to prevent using reserved keywords without escaping
                return false;
            }

            // Start from a clean slate: ErrorObject is shared state on the datasource, and callers
            // (MigrationManager among them) treat a Failed flag after this call as "the check itself
            // broke". Without this, a failure left behind by an unrelated earlier operation is
            // misreported as an existence-check failure for every entity.
            ErrorObject.Flag = Errors.Ok;
            ErrorObject.Message = string.Empty;

            GetEntitesList();

            // Compare against the table names actually read from the database, not the Entities
            // structure cache. The cache is populated lazily and is empty in a fresh process, so
            // testing it reports "does not exist" for tables that are really there — and migration
            // then tries to re-create every one of them.
            bool retval = EntitiesNames != null &&
                          EntitiesNames.Any(n => string.Equals(n, EntityName, StringComparison.OrdinalIgnoreCase));

            // Fall back to the structure cache only when the name list yielded nothing, so
            // in-memory-only entities are still discoverable.
            if (!retval && Entities != null && Entities.Count > 0)
            {
                retval = Entities.Any(p =>
                    string.Equals(p.EntityName, EntityName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(p.OriginalEntityName, EntityName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(p.DatasourceEntityName, EntityName, StringComparison.OrdinalIgnoreCase));
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
        /// Creates an entity in the database as per the specified structure with comprehensive validation.
        /// </summary>
        /// <param name="entity">The entity structure to create in the database.</param>
        /// <returns>True if creation is successful, otherwise false.</returns>
        /// <remarks>
        /// This method validates the entity structure for naming conventions, reserved keywords, 
        /// field validation, and database-specific constraints before creating the entity.
        /// </remarks>
        public virtual bool CreateEntityAs(EntityStructure entity)
        {
            bool retval = false;

            // Comprehensive entity validation
            if (entity == null)
            {
                DMEEditor?.AddLogMessage("Fail", "Entity structure cannot be null", 
                    DateTime.Now, 0, null, Errors.Failed);
                return false;
            }

            // Set database type if not already set
            if (entity.DatabaseType == DataSourceType.NONE || entity.DatabaseType == DataSourceType.Unknown)
            {
                entity.DatabaseType = DatasourceType;
            }

            // Validate entity structure (naming, fields, constraints)
            var (isValid, validationErrors) = DatabaseEntityValidator.ValidateEntityStructure(entity);
            
            if (!isValid)
            {
                // One call that sets this datasource's ErrorObject, the engine's, and logs -- all
                // null-guarded. Written longhand, the two DMEEditor.ErrorObject writes threw
                // NullReferenceException with no editor attached.
                SetFailure($"Entity validation failed for '{entity.EntityName}': {string.Join("; ", validationErrors)}",
                           entity.EntityName);
                return false;
            }

            // Check if entity already exists
            if (CheckEntityExist(entity.EntityName) == false)
            {
                string createstring = CreateEntity(entity);
                
                // Guard against null/empty SQL - GenerateCreateEntityScript may have failed silently
                if (string.IsNullOrWhiteSpace(createstring))
                {
                    SetFailure($"Failed to generate CREATE TABLE script for '{entity.EntityName}'. Check log for details.",
                               entity.EntityName);
                    return false;
                }
                
                // Read the result; do not assign it to DMEEditor.ErrorObject. ExecuteSql returns
                // THIS datasource's ErrorObject, so the assignment permanently aliased engine-wide
                // state to one datasource's field -- the same defect as K38, and this was its fourth
                // site. It also NRE'd outright when no editor was attached.
                var createResult = ExecuteSql(createstring);
                if (createResult == null || createResult.Flag == Errors.Failed)
                {
                    retval = false;
                }
                else
                {
                    Entities.Add(entity);
                    EntitiesNames.Add(entity.EntityName);
                    retval = true;
                    SetSuccess($"Entity '{entity.EntityName}' created successfully");
                    DMEEditor?.AddLogMessage("Success", $"Entity '{entity.EntityName}' created successfully", 
                        DateTime.Now, 0, entity.EntityName, Errors.Ok);
                }
            }
            else
            {
                // Reported deterministically rather than through AddLogMessage alone, which does
                // nothing to the flag when no logger is attached. Note this is why CreateEntityAs
                // returns false for an entity that already exists -- callers treating "false" as
                // "not present" must check CheckEntityExist themselves.
                SetFailure($"Entity '{entity.EntityName}' already exists", entity.EntityName);
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
                                // A plain property read that should not throw. It previously set
                                // Failed and stashed the exception with no message and no log, so
                                // the method still returned a populated list while the flag said
                                // failure and nothing said why.
                                SetFailure($"Could not read the constraint name for a foreign key on {entityname}: {ex.Message}", entityname);
                                ErrorObject.Ex = ex;
                            }
                            fk.Add(rfk);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                HandleDatabaseError(ex, entityname, "get foreign keys for");
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
                if (DMEEditor?.ConfigEditor == null)
                {
                    SetFailure($"Cannot read the child tables of {tablename}: no ConfigEditor is available to supply the query.", tablename);
                    return null;
                }

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
                HandleDatabaseError(ex, tablename, "get child entities for");
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
            // Was Task.Run(() => ExecuteSql(...)) followed by .Wait(): the caller blocked regardless,
            // so the offload bought nothing, and .Wait() rewrapped any failure in an
            // AggregateException on its way out of a method whose whole job is to report what
            // happened.
            var result = ExecuteSql(scripts?.Ddl);

            if (scripts != null)
                scripts.ErrorMessage = result?.Message;

            // Read the result; do not re-point DMEEditor.ErrorObject at it. ExecuteSql returns THIS
            // datasource's ErrorObject, so the old assignment aliased engine-wide state to one
            // datasource's field -- the same defect as K38 in InMemoryRDBSource, in the base class.
            return result;
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
                // Returns an empty DataTable on failure, which GetEntityStructure then turns into a
                // field-less, key-less EntityStructure — indistinguishable from a real table with no
                // columns unless the flag says otherwise.
                HandleDatabaseError(ex, TableName, "fetch the schema for");
            }

            return tb;
        }
        public virtual List<ChildRelation> GetTablesFKColumnList(string tablename, string SchemaName, string Filterparamters)
        {
            ErrorObject.Flag = Errors.Ok;
            DataSet ds = new DataSet();
            try
            {
                if (DMEEditor?.ConfigEditor == null)
                {
                    SetFailure($"Cannot read the foreign-key columns of {tablename}: no ConfigEditor is available to supply the query.", tablename);
                    return null;
                }

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
                HandleDatabaseError(ex, tablename, "retrieve the foreign-key column list for");
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

        /// <summary>
        /// Maps an Oracle NUMBER column to the tightest .NET integral/decimal
        /// type from its decimal precision and scale. ODP.NET's GetSchemaTable
        /// reports every NUMBER as System.Decimal, which is too coarse for
        /// NUMBER(p,0) integer keys.
        /// </summary>
        public static string MapOracleNumberToDotNetType(short precision, short scale)
        {
            // Fractional or unconstrained NUMBER stays decimal — decimal is the
            // only .NET type that can hold the full Oracle NUMBER range/semantics.
            if (scale > 0 || precision <= 0)
            {
                return "System.Decimal";
            }

            // Integral NUMBER(p,0) narrows to the smallest integer type that fits.
            if (precision <= 4)
            {
                return "System.Int16";
            }
            if (precision <= 9)
            {
                return "System.Int32";
            }
            if (precision <= 18)
            {
                return "System.Int64";
            }

            // 19–38 digits exceed Int64; decimal is required.
            return "System.Decimal";
        }
        public int GetFloatPrecision(string tableName, string FieldName)
        {
            int precision = 0;
            string query = $"SELECT DATA_PRECISION FROM ALL_TAB_COLUMNS WHERE TABLE_NAME = '{tableName.ToUpper()}' AND COLUMN_NAME = '{FieldName.ToUpper()}'";


            IDbCommand command = GetDataCommand();

            // GetDataCommand returns null on a closed connection; this used to dereference it.
            if (command == null)
                return precision;

            try
            {
                command.CommandText = query;
                using (IDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        // Assuming the precision is not null, adjust as needed if it could be
                        precision = reader.GetInt32(0);
                    }
                }
            }
            catch (Exception ex)
            {
                // Was Console.WriteLine — the only place in this class that reported anywhere but
                // the engine's log, so an Oracle precision lookup failed invisibly.
                HandleDatabaseError(ex, tableName, "read column precision for", query);
            }
            finally
            {
                command.Dispose();
            }

            return precision;
        }
    }
}
