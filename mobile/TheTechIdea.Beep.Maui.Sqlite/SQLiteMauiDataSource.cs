using SQLite;
using System;
using System.Data;
using TheTechIdea.Beep.Connections;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Maui.DataSource.Sqlite
{
    [AddinAttribute(Category = DatasourceCategory.RDBMS, DatasourceType = DataSourceType.SqlLite)]
    public class SQLiteMauiDataSource : IDataSource,IDisposable

    {
        private bool disposedValue;
        public string DbPath { get; set; }
        public string dbname { get; set; }
        private  SQLiteConnection db;
        public SQLiteMauiDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = databasetype;
            Category = DatasourceCategory.RDBMS;
            Dataconnection = new DefaulDataConnection();
            Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == datasourcename).FirstOrDefault();
            ConnectionStatus = ConnectionState.Closed;
            if (Dataconnection.ConnectionProp == null)
            {
                Dataconnection.ConnectionProp = new ConnectionProperties();
                Dataconnection.DataSourceDriver = DMEEditor.ConfigEditor.DataDriversClasses.Find(p => p.classHandler == "SQLiteMauiDataSource");    
            }
            dbname = "MyData.db";


        }
        public string ColumnDelimiter { get  ; set  ; }
        public string ParameterDelimiter { get  ; set  ; }
        public string GuidID { get  ; set  ; }
        public DataSourceType DatasourceType { get  ; set  ; }
        public DatasourceCategory Category { get  ; set  ; }
        public IDataConnection Dataconnection { get  ; set  ; }
        public string DatasourceName { get  ; set  ; }
        public IErrorsInfo ErrorObject { get  ; set  ; }
        public string Id { get  ; set  ; }
        public IDMLogger Logger { get  ; set  ; }
        public List<string> EntitiesNames { get  ; set  ; }
        public List<EntityStructure> Entities { get  ; set  ; }
        public IDMEEditor DMEEditor { get  ; set  ; }
        public ConnectionState ConnectionStatus { get  ; set  ; }

        public event EventHandler<PassedArgs> PassEvent;


       
        public IErrorsInfo BeginTransaction(PassedArgs args)
        {
            var errorsInfo = new ErrorsInfo();

            try
            {
                if (db != null)
                {
                    db.BeginTransaction();
                    errorsInfo.Flag = Errors.Ok;
                    errorsInfo.Message = "Transaction started successfully";
                }
                else
                {
                    throw new Exception("Database connection is null");
                }
            }
            catch (Exception ex)
            {
                errorsInfo.Flag = Errors.Failed;
                errorsInfo.Message = ex.Message;
            }

            return errorsInfo;
        }

        public bool CheckEntityExist(string EntityName)
        {
            try
            {
                if (db != null)
                {
                    var query = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{EntityName}';";
                    var result = db.ExecuteScalar<string>(query);
                    return !string.IsNullOrEmpty(result);
                }
                else
                {
                    throw new Exception("Database connection is null");
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public ConnectionState Closeconnection()
        {
            try
            {
                if (db != null)
                {
                    db.Close();
                    ConnectionStatus = ConnectionState.Closed;
                    return ConnectionState.Closed;
                }
                else
                {
                    throw new Exception("Database connection is null");
                }
            }
            catch (Exception)
            {
                ConnectionStatus = ConnectionState.Broken;
                return ConnectionState.Broken;
            }
        }

        public IErrorsInfo Commit(PassedArgs args)
        {
            var errorsInfo = new ErrorsInfo();

            try
            {
                if (db != null)
                {
                    db.Commit();
                    errorsInfo.Flag = Errors.Ok;
                    errorsInfo.Message = "Transaction committed successfully.";
                }
                else
                {
                    throw new Exception("Database connection is null");
                }
            }
            catch (Exception ex)
            {
                errorsInfo.Flag = Errors.Failed;
                errorsInfo.Message = ex.Message;
            }

            return errorsInfo;
        }


        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            var errorsInfo = new ErrorsInfo ();

            try
            {
                if (db != null)
                {
                    foreach (var entity in entities)
                    {
                        var result = CreateEntityAs(entity);
                        if (!result)
                        {
                            errorsInfo.Flag = Errors.Ok;
                            errorsInfo.Message += $"Failed to create entity {entity.EntityName}. ";
                        }
                    }
                }
                else
                {
                    throw new Exception("Database connection is null");
                }
            }
            catch (Exception ex)
            {
                errorsInfo.Flag = Errors.Failed;
                errorsInfo.Message = ex.Message;
            }

            return errorsInfo;
        }


        public bool CreateEntityAs(EntityStructure entity)
        {
            try
            {
                if (db != null)
                {
                    var columns = entity.Fields.Select(c => $"{c.fieldname} {c.fieldtype} {(c.AllowDBNull ? "" : "NOT NULL")}");
                    var primaryKey = entity.PrimaryKeys.Any() ? $", PRIMARY KEY ({string.Join(", ", entity.PrimaryKeys.Select(pk => pk.fieldname))})" : "";
                    var createTableQuery = $"CREATE TABLE IF NOT EXISTS {entity.EntityName} ({string.Join(", ", columns)}{primaryKey});";
                    db.Execute(createTableQuery);
                    return true;
                }
                else
                {
                    throw new Exception("Database connection is null");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create entity: {ex.Message}");
            }
        }


        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            try
            {
                if (db != null)
                {
                    var mapping = db.GetMapping(typeof(object)); // Assuming entity has a mapping
                    var primaryKey = mapping.PK;

                    if (primaryKey == null)
                    {
                        throw new Exception("Primary key not defined for the table");
                    }

                    var primaryKeyValue = primaryKey.GetValue(UploadDataRow);
                    var deleteQuery = $"DELETE FROM {EntityName} WHERE {primaryKey.Name} = ?";

                    db.Execute(deleteQuery, primaryKeyValue);

                    return new ErrorsInfo { Flag = Errors.Ok, Message = "Entity deleted successfully" };
                }
                else
                {
                    throw new Exception("Database connection is null");
                }
            }
            catch (Exception ex)
            {
                return new ErrorsInfo { Flag= Errors.Failed, Message = ex.Message };
            }
        }


        public IErrorsInfo EndTransaction(PassedArgs args)
        {
            var errorsInfo = new ErrorsInfo();

            try
            {
                if (db != null)
                {
                    if (!args.IsError)
                    {
                        db.Commit();
                        errorsInfo.Flag = Errors.Ok;
                        errorsInfo.Message = "Transaction committed successfully.";
                    }
                    else
                    {
                        db.Rollback();
                        errorsInfo.Flag = Errors.Ok;
                        errorsInfo.Message = "Transaction rolled back successfully.";
                    }
                }
                else
                {
                    throw new Exception("Database connection is null");
                }
            }
            catch (Exception ex)
            {
                errorsInfo.Flag = Errors.Failed;
                errorsInfo.Message = ex.Message;
            }

            return errorsInfo;
        }


        public IErrorsInfo ExecuteSql(string sql)
        {
            var errorsInfo = new ErrorsInfo();

            try
            {
                if (db != null)
                {
                    db.Execute(sql);
                    errorsInfo.Flag = Errors.Ok;
                    errorsInfo.Message = "SQL executed successfully.";
                }
                else
                {
                    throw new Exception("Database connection is null");
                }
            }
            catch (Exception ex)
            {
                errorsInfo.Flag = Errors.Failed;
                errorsInfo.Message = ex.Message;
            }

            return errorsInfo;
        }


        public List<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            throw new NotImplementedException();
        }

        public List<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            throw new NotImplementedException();
        }

        public List<string> GetEntitesList()
        {
            try
            {
                if (db != null)
                {
                    var query = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';";
                    var tableNames = db.Query<TableName>(query).Select(t => t.Name).ToList();
                    return tableNames;
                }
                else
                {
                    throw new Exception("Database connection is null");
                }
            }
            catch (Exception ex)
            {
                // Handle exception
                throw new Exception($"Failed to get entities list: {ex.Message}");
            }
        }


        public object GetEntity(string EntityName, List<AppFilter> filter)
        {
            try
            {
                if (db != null)
                {
                    // Build the WHERE clause from the filters
                    var whereClauses = filter.Select(f => $"{f.FieldName} {f.Operator} ?");
                    var whereClause = string.Join(" AND ", whereClauses);

                    // Prepare the query
                    var query = $"SELECT * FROM {EntityName} WHERE {whereClause}";
                    var filterValues = filter.Select(f => f.FilterValue).ToArray();

                    // Execute the query and return the result
                    var result = db.Query<object>(query, filterValues);
                    return result;
                }
                else
                {
                    throw new Exception("Database connection is null");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get entity: {ex.Message}");
            }
        }


        public object GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            try
            {
                if (db != null)
                {
                    // Build the WHERE clause from the filters
                    var whereClauses = filter.Select(f => $"{f.FieldName} {f.Operator} ?");
                    var whereClause = string.Join(" AND ", whereClauses);

                    // Calculate the offset
                    int offset = (pageNumber - 1) * pageSize;

                    // Prepare the query with pagination
                    var query = $"SELECT * FROM {EntityName} WHERE {whereClause} LIMIT {pageSize} OFFSET {offset}";
                    var filterValues = filter.Select(f => f.FilterValue).ToArray();

                    // Execute the query and return the result
                    var result = db.Query<object>(query, filterValues);
                    return result;
                }
                else
                {
                    throw new Exception("Database connection is null");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get entity with pagination: {ex.Message}");
            }
        }

        public async Task<object> GetEntityAsync(string EntityName, List<AppFilter> filter)
        {
            try
            {
                if (db != null)
                {
                    // Build the WHERE clause from the filters
                    var whereClauses = filter.Select(f => $"{f.FieldName} {f.Operator} ?");
                    var whereClause = string.Join(" AND ", whereClauses);

                    // Prepare the query
                    var query = $"SELECT * FROM {EntityName} WHERE {whereClause}";
                    var filterValues = filter.Select(f => f.FilterValue).ToArray();

                    // Execute the query asynchronously and return the result
                    var result = await Task.Run(() => db.Query<object>(query, filterValues));
                    return result;
                }
                else
                {
                    throw new Exception("Database connection is null");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get entity asynchronously: {ex.Message}");
            }
        }


        public List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            throw new NotImplementedException();
        }

        public int GetEntityIdx(string entityName)
        {
            try
            {
                if (Entities != null)
                {
                    for (int i = 0; i < Entities.Count; i++)
                    {
                        if (Entities[i].EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase))
                        {
                            return i;
                        }
                    }
                }
                else
                {
                    throw new Exception("Entities list is null");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get entity index: {ex.Message}");
            }
            return -1; // Return -1 if the entity is not found
        }


        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            try
            {
                if (db != null)
                {
                    var tableInfo = db.GetTableInfo(EntityName);
                    var entityStructure = new EntityStructure
                    {
                        EntityName = EntityName,
                        Fields = tableInfo.Select(column => new EntityField
                        {
                            fieldname = column.Name,
                            fieldtype=column.GetType().Name,
                            IsKey = column.Name.Equals("id", StringComparison.OrdinalIgnoreCase), // Assuming "id" as primary key column name
                            AllowDBNull = column.notnull == 0,
                            IsAutoIncrement = false, // Set based on your logic
                            IsUnique = false, // Set based on your logic
                            IsIdentity = false, // Set based on your logic
                                                // Add more field initializations as needed
                        }).ToList()
                    };
                    if(Entities.Contains(entityStructure)==false)
                    {
                        Entities.Add(entityStructure);
                    }
                    return entityStructure;
                }
                else
                {
                    throw new Exception("Database connection is null");
                }
            }
            catch (Exception ex)
            {
                // Handle exception
                throw new Exception($"Failed to get entity structure: {ex.Message}");
            }
        }


        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            if (refresh || fnd == null)
            {
                return GetEntityStructure(fnd.EntityName, refresh);
            }
            return fnd;
        }

        public Type GetEntityType(string EntityName)
        {
            Type t = null;
            EntityStructure entityStructure= GetEntityStructure(EntityName, false); 
            t=DMEEditor.Utilfunction.GetEntityType(DMEEditor, EntityName, entityStructure.Fields);
            return t;
        }

        public double GetScalar(string query)
        {
            try
            {
                if (db != null)
                {
                    return db.ExecuteScalar<double>(query);
                }
                else
                {
                    throw new Exception("Database connection is null");
                }
            }
            catch (Exception ex)
            {
                // Handle exception
                throw new Exception($"Failed to execute scalar query: {ex.Message}");
            }
        }


        public async Task<double> GetScalarAsync(string query)
        {
            try
            {
                if (db != null)
                {
                    return await Task.Run(() => db.ExecuteScalar<double>(query));
                }
                else
                {
                    throw new Exception("Database connection is null");
                }
            }
            catch (Exception ex)
            {
                // Handle exception
                throw new Exception($"Failed to execute scalar query asynchronously: {ex.Message}");
            }
        }


        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            try
            {
                if (db != null)
                {
                    db.Insert(InsertedData);
                    return new ErrorsInfo {  Flag= Errors.Ok, Message = "Insert successful" };
                }
                else
                {
                    return new ErrorsInfo { Flag = Errors.Failed, Message = "Database connection is null" };
                }
            }
            catch (Exception ex)
            {
                return new ErrorsInfo { Flag = Errors.Failed, Message = ex.Message };
            }
        }


        public ConnectionState Openconnection()
        {
            try
            {
                // Get an absolute path to the database file
                var databasePath = Path.Combine(DbPath, dbname);
                 db = new SQLiteConnection(databasePath);
                ConnectionStatus= ConnectionState.Open;
                return ConnectionState.Open;
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                return ConnectionState.Broken;

            }
        }
        public List<T> RunQuery<T>(string qrystr) where T : new()
        {
            try
            {
                if (db != null)
                {
                    return db.Query<T>(qrystr);
                }
                return new List<T>();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public object RunQuery(string qrystr)
        {
            return null;
        }

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            try
            {
                if (db != null)
                {
                    db.BeginTransaction();
                    var entities = UploadData as IEnumerable<object>;
                    int total = entities.Count();
                    int count = 0;
                    foreach (var entity in entities)
                    {
                        db.Update(entity);
                        count++;
                        progress.Report(new PassedArgs { ParameterInt1=(count * 100) / total });
                    }
                    db.Commit();
                    return new ErrorsInfo { Flag= Errors.Ok, Message = "Entities updated successfully" };
                }
                else
                {
                    throw new Exception("Database connection is null");
                }
            }
            catch (Exception ex)
            {
                db.Rollback();
                return new ErrorsInfo { Flag = Errors.Failed, Message = ex.Message };
            }
        }


        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            try
            {
                if (db != null)
                {
                    db.Update(UploadDataRow);
                    return new ErrorsInfo { Flag = Errors.Ok, Message = "Entity updated successfully" };
                }
                else
                {
                    throw new Exception("Database connection is null");
                }
            }
            catch (Exception ex)
            {
                return new ErrorsInfo { Flag = Errors.Failed, Message = ex.Message };
            }
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~SQLiteMauiDataSource()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
    public class TableName
    {
        public string Name { get; set; }
    }

}
