using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.WebAPI;
using Couchbase.Lite;
using Couchbase.Lite.Query;
using System.IO;
using System.Reflection;
using System.ComponentModel;
using System.Collections;

namespace TheTechIdea.Beep.Local.CouchbaseLite
{
    [AddinAttribute(Category = DatasourceCategory.NOSQL, DatasourceType = DataSourceType.CouchBaseLite)]
    public class CouchBaseLiteDataSource : IDataSource, ILocalDB
    {
        public string GuidID { get; set; }
        public event EventHandler<PassedArgs> PassEvent;
        public DataSourceType DatasourceType { get; set; }
        public DatasourceCategory Category { get; set; }
        public IDataConnection Dataconnection { get; set; }
        public string DatasourceName { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public string Id { get; set; }
        public IDMLogger Logger { get; set; }
        public List<string> EntitiesNames { get; set; } = new List<string>();
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
        public IDMEEditor DMEEditor { get; set; }
        public ConnectionState ConnectionStatus { get; set; }
        public virtual string ColumnDelimiter { get; set; } = "''";
        public virtual string ParameterDelimiter { get; set; } = ":";
        public bool CanCreateLocal { get; set; } = true;
        public bool InMemory { get; set; } = false;
        public string Extension { get; set; } = ".cblite2";

        private Database _database;
        private string _databasePath;

        public CouchBaseLiteDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = databasetype;
            Category = DatasourceCategory.NOSQL;

            Dataconnection = new WebAPIDataConnection
            {
                Logger = logger,
                ErrorObject = ErrorObject
            };

            if (DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == datasourcename).Any())
            {
                Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == datasourcename).FirstOrDefault();
            }
            else
            {
                ConnectionDriversConfig driversConfig = DMEEditor.ConfigEditor.DataDriversClasses.FirstOrDefault(p => p.DatasourceType == databasetype);
                Dataconnection.ConnectionProp = new ConnectionProperties
                {
                    ConnectionName = datasourcename,
                    ConnectionString = driversConfig?.ConnectionString ?? "",
                    DriverName = driversConfig?.PackageName ?? "",
                    DriverVersion = driversConfig?.version ?? "",
                    DatabaseType = DataSourceType.CouchBaseLite,
                    Category = DatasourceCategory.NOSQL
                };
            }

            var dbName = Dataconnection.ConnectionProp?.Database ?? "default";
            _databasePath = Path.Combine(
                Dataconnection.ConnectionProp?.FilePath ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                $"{dbName}{Extension}");
            
            GuidID = Guid.NewGuid().ToString();
        }

        public int GetEntityIdx(string entityName)
        {
            if (Entities != null && Entities.Count > 0)
            {
                return Entities.FindIndex(p => p.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
            }
            return -1;
        }

        public virtual IErrorsInfo BeginTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            return ErrorObject;
        }

        public virtual IErrorsInfo EndTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            return ErrorObject;
        }

        public virtual IErrorsInfo Commit(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            return ErrorObject;
        }

        public ConnectionState Openconnection()
        {
            try
            {
                if (_database == null)
                {
                    var config = new DatabaseConfiguration();
                    if (InMemory)
                    {
                        config = DatabaseConfiguration.InMemory;
                    }

                    _database = new Database(Path.GetFileNameWithoutExtension(_databasePath), config);
                    ConnectionStatus = ConnectionState.Open;
                    GetEntitesList();
                    DMEEditor?.AddLogMessage("Beep", $"Connected to Couchbase Lite: {_databasePath}", DateTime.Now, 0, null, Errors.Ok);
                }
                else
                {
                    ConnectionStatus = ConnectionState.Open;
                }
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Closed;
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error connecting to Couchbase Lite: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return ConnectionStatus;
        }

        public ConnectionState Closeconnection()
        {
            try
            {
                if (_database != null)
                {
                    _database.Close();
                    _database = null;
                }
                ConnectionStatus = ConnectionState.Closed;
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error closing connection: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return ConnectionStatus;
        }

        public IEnumerable<string> GetEntitesList()
        {
            List<string> collectionNames = new List<string>();
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open && _database != null)
                {
                    // Query distinct types from documents
                    var query = QueryBuilder.Select(SelectResult.All())
                        .From(DataSource.Database(_database))
                        .GroupBy(Expression.Property("type"));

                    var result = query.Execute();
                    var types = new HashSet<string>();

                    foreach (var row in result)
                    {
                        var dict = row.ToDictionary();
                        if (dict.ContainsKey("type") && dict["type"] != null)
                        {
                            types.Add(dict["type"].ToString());
                        }
                    }

                    collectionNames = types.ToList();
                    EntitiesNames = collectionNames;
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error getting entities list: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return collectionNames;
        }

        public IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            List<object> results = new List<object>();
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open && _database != null)
                {
                    var query = QueryBuilder.Select(SelectResult.All())
                        .From(DataSource.Database(_database))
                        .Where(Expression.Property("type").EqualTo(Expression.String(EntityName)));

                    // Apply filters
                    if (filter != null && filter.Count > 0)
                    {
                        foreach (var f in filter)
                        {
                            if (!string.IsNullOrEmpty(f.FieldName) && !string.IsNullOrEmpty(f.Operator))
                            {
                                var fieldExpr = Expression.Property(f.FieldName);
                                var valueExpr = BuildExpressionValue(f.FilterValue, f.valueType);
                                query = query.Where(BuildFilterExpression(fieldExpr, valueExpr, f.Operator));
                            }
                        }
                    }

                    var result = query.Execute();
                    foreach (var row in result)
                    {
                        results.Add(row.ToDictionary());
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error getting entity: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return results;
        }

        private Expression BuildExpressionValue(string value, string type)
        {
            if (string.IsNullOrEmpty(type))
                return Expression.String(value);

            switch (type.ToLower())
            {
                case "system.int32":
                case "int":
                    return Expression.Integer(int.Parse(value));
                case "system.int64":
                case "long":
                    return Expression.Long(long.Parse(value));
                case "system.double":
                    return Expression.Double(double.Parse(value));
                case "system.boolean":
                case "bool":
                    return Expression.Boolean(bool.Parse(value));
                default:
                    return Expression.String(value);
            }
        }

        private Expression BuildFilterExpression(Expression field, Expression value, string op)
        {
            switch (op.ToUpper())
            {
                case "=": return field.EqualTo(value);
                case "!=": return field.NotEqualTo(value);
                case ">": return field.GreaterThan(value);
                case ">=": return field.GreaterThanOrEqualTo(value);
                case "<": return field.LessThan(value);
                case "<=": return field.LessThanOrEqualTo(value);
                default: return field.EqualTo(value);
            }
        }

        public PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            PagedResult pagedResult = new PagedResult();
            try
            {
                var allData = GetEntity(EntityName, filter).ToList();
                int totalRecords = allData.Count;
                int offset = (pageNumber - 1) * pageSize;
                var pagedData = allData.Skip(offset).Take(pageSize).ToList();

                pagedResult.Data = pagedData;
                pagedResult.TotalRecords = totalRecords;
                pagedResult.PageNumber = pageNumber;
                pagedResult.PageSize = pageSize;
                pagedResult.TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
                pagedResult.HasNextPage = pageNumber < pagedResult.TotalPages;
                pagedResult.HasPreviousPage = pageNumber > 1;
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
            }
            return pagedResult;
        }

        public Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            return Task.Run(() => GetEntity(EntityName, Filter));
        }

        public IEnumerable<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            return new List<RelationShipKeys>();
        }

        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            try
            {
                if (!refresh && Entities != null && Entities.Count > 0)
                {
                    var existing = Entities.FirstOrDefault(e => e.EntityName.Equals(EntityName, StringComparison.OrdinalIgnoreCase));
                    if (existing != null)
                    {
                        return existing;
                    }
                }

                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open && _database != null)
                {
                    // Get sample documents to infer schema
                    var sampleDocs = GetEntity(EntityName, null).Take(10).ToList();
                    
                    EntityStructure entity = new EntityStructure
                    {
                        EntityName = EntityName,
                        DatasourceEntityName = EntityName,
                        Fields = new List<EntityField>()
                    };

                    if (sampleDocs.Count > 0)
                    {
                        var firstDoc = sampleDocs[0] as Dictionary<string, object>;
                        if (firstDoc != null)
                        {
                            foreach (var kvp in firstDoc)
                            {
                                if (!kvp.Key.StartsWith("_")) // Skip internal fields
                                {
                                    entity.Fields.Add(new EntityField
                                    {
                                        fieldname = kvp.Key,
                                        Originalfieldname = kvp.Key,
                                        fieldtype = GetDotNetType(kvp.Value?.GetType() ?? typeof(string)),
                                        EntityName = EntityName
                                    });
                                }
                            }
                        }
                    }

                    if (Entities == null)
                    {
                        Entities = new List<EntityStructure>();
                    }

                    var idx = GetEntityIdx(EntityName);
                    if (idx >= 0)
                    {
                        Entities[idx] = entity;
                    }
                    else
                    {
                        Entities.Add(entity);
                    }

                    return entity;
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error getting entity structure: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return null;
        }

        private string GetDotNetType(Type type)
        {
            if (type == typeof(int)) return "System.Int32";
            if (type == typeof(long)) return "System.Int64";
            if (type == typeof(double)) return "System.Double";
            if (type == typeof(bool)) return "System.Boolean";
            if (type == typeof(DateTime)) return "System.DateTime";
            return "System.String";
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            return GetEntityStructure(fnd?.EntityName ?? "", refresh);
        }

        public Type GetEntityType(string EntityName)
        {
            try
            {
                var entity = GetEntityStructure(EntityName, false);
                if (entity != null)
                {
                    return DMTypeBuilder.CreateTypeFromEntityStructure(entity, DMEEditor);
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error getting entity type: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return typeof(object);
        }

        public bool CheckEntityExist(string EntityName)
        {
            return EntitiesNames.Contains(EntityName, StringComparer.OrdinalIgnoreCase);
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            try
            {
                if (Entities == null)
                {
                    Entities = new List<EntityStructure>();
                }
                if (!Entities.Any(e => e.EntityName.Equals(entity.EntityName, StringComparison.OrdinalIgnoreCase)))
                {
                    Entities.Add(entity);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public IErrorsInfo ExecuteSql(string sql)
        {
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = "SQL execution not supported for Couchbase Lite. Use N1QL queries.";
            return ErrorObject;
        }

        public IEnumerable<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            return new List<ChildRelation>();
        }

        public IEnumerable<object> RunQuery(string qrystr)
        {
            List<object> results = new List<object>();
            try
            {
                // Couchbase Lite uses N1QL queries
                // Implementation would parse and execute N1QL
                DMEEditor?.AddLogMessage("Beep", "N1QL queries not fully implemented", DateTime.Now, 0, null, Errors.Failed);
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error running query: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return results;
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok };
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open && _database != null)
                {
                    Dictionary<string, object> docDict;
                    if (UploadDataRow is Dictionary<string, object> dict)
                    {
                        docDict = dict;
                    }
                    else
                    {
                        docDict = ConvertToDictionary(UploadDataRow);
                    }

                    string docId = docDict.ContainsKey("_id") ? docDict["_id"].ToString() : Guid.NewGuid().ToString();
                    docDict["type"] = EntityName;

                    var mutableDoc = new MutableDocument(docId);
                    foreach (var kvp in docDict)
                    {
                        mutableDoc.SetValue(kvp.Key, kvp.Value);
                    }

                    _database.Save(mutableDoc);
                    retval.Message = "Entity updated successfully";
                }
            }
            catch (Exception ex)
            {
                retval.Flag = Errors.Failed;
                retval.Message = ex.Message;
            }
            return retval;
        }

        private Dictionary<string, object> ConvertToDictionary(object obj)
        {
            var dict = new Dictionary<string, object>();
            if (obj != null)
            {
                foreach (var prop in obj.GetType().GetProperties())
                {
                    dict[prop.Name] = prop.GetValue(obj);
                }
            }
            return dict;
        }

        public IErrorsInfo DeleteEntity(string EntityName, object DeletedDataRow)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok };
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open && _database != null)
                {
                    string docId = null;
                    if (DeletedDataRow is Dictionary<string, object> dict && dict.ContainsKey("_id"))
                    {
                        docId = dict["_id"].ToString();
                    }
                    else
                    {
                        var props = DeletedDataRow.GetType().GetProperty("_id") ?? DeletedDataRow.GetType().GetProperty("Id");
                        docId = props?.GetValue(DeletedDataRow)?.ToString();
                    }

                    if (!string.IsNullOrEmpty(docId))
                    {
                        var doc = _database.GetDocument(docId);
                        if (doc != null)
                        {
                            _database.Delete(doc);
                            retval.Message = "Entity deleted successfully";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                retval.Flag = Errors.Failed;
                retval.Message = ex.Message;
            }
            return retval;
        }

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = "Script execution not supported for Couchbase Lite";
            return ErrorObject;
        }

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            ErrorObject.Flag = Errors.Ok;
            foreach (var entity in entities)
            {
                CreateEntityAs(entity);
            }
            return ErrorObject;
        }

        public IEnumerable<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            List<ETLScriptDet> scripts = new List<ETLScriptDet>();
            var entitiesToScript = entities ?? Entities;
            if (entitiesToScript != null)
            {
                foreach (var entity in entitiesToScript)
                {
                    scripts.Add(new ETLScriptDet
                    {
                        EntityName = entity.EntityName,
                        ScriptType = "CREATE",
                        ScriptText = $"# Couchbase Lite entity: {entity.EntityName}\n# Documents with type='{entity.EntityName}'"
                    });
                }
            }
            return scripts;
        }

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok };
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open && _database != null)
                {
                    Dictionary<string, object> docDict = ConvertToDictionary(InsertedData);
                    string docId = Guid.NewGuid().ToString();
                    docDict["type"] = EntityName;
                    docDict["_id"] = docId;

                    var mutableDoc = new MutableDocument(docId);
                    foreach (var kvp in docDict)
                    {
                        mutableDoc.SetValue(kvp.Key, kvp.Value);
                    }

                    _database.Save(mutableDoc);
                    retval.Message = "Entity inserted successfully";
                }
            }
            catch (Exception ex)
            {
                retval.Flag = Errors.Failed;
                retval.Message = ex.Message;
            }
            return retval;
        }

        public virtual double GetScalar(string query)
        {
            return 0.0;
        }

        public virtual Task<double> GetScalarAsync(string query)
        {
            return Task.Run(() => GetScalar(query));
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok };
            try
            {
                if (UploadData is IEnumerable enumerable)
                {
                    int count = 0;
                    int total = 0;
                    if (enumerable is ICollection collection)
                    {
                        total = collection.Count;
                    }

                    foreach (var item in enumerable)
                    {
                        UpdateEntity(EntityName, item);
                        count++;
                        if (progress != null && total > 0)
                        {
                            progress.Report(new PassedArgs
                            {
                                Message = $"Updated {count} of {total}",
                                Percentage = (count * 100) / total
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                retval.Flag = Errors.Failed;
                retval.Message = ex.Message;
            }
            return retval;
        }

        public bool CreateDB(bool inMemory)
        {
            try
            {
                InMemory = inMemory;
                Openconnection();
                return ConnectionStatus == ConnectionState.Open;
            }
            catch
            {
                return false;
            }
        }

        public bool CreateDB(string filepathandname)
        {
            try
            {
                _databasePath = filepathandname;
                if (!_databasePath.EndsWith(Extension))
                {
                    _databasePath += Extension;
                }
                Openconnection();
                return ConnectionStatus == ConnectionState.Open;
            }
            catch
            {
                return false;
            }
        }

        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Closeconnection();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}