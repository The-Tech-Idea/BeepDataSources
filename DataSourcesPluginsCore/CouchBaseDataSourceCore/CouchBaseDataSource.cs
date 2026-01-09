using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using System.Reflection.Metadata;
using System.Net.Http.Headers;
using TheTechIdea;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.WebAPI;
using TheTechIdea.Beep.Helpers;
using Newtonsoft.Json;
using Couchbase;
using Couchbase.Query;
using Couchbase.KeyValue;
using Newtonsoft.Json.Linq;

namespace CouchBaseDataSourceCore
{
    public class CouchBaseDataSource : IDataSource
    {
        private bool disposedValue;
     
        public CouchBaseDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
        {
            this.disposedValue = false;
            // You can generate an API token from the "API Tokens Tab" in the UI



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
            Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == datasourcename).FirstOrDefault();
            Dataconnection.ConnectionProp.Category = DatasourceCategory.NOSQL;
            Dataconnection.ConnectionProp.DatabaseType = DataSourceType.Couchbase;

            CurrentDatabase = Dataconnection.ConnectionProp.Database;
            if (Dataconnection.ConnectionProp.Url.Length > 0)
            {
                baseUrl = Dataconnection.ConnectionProp.Url;
            }
            if (Dataconnection.ConnectionProp.Port > 0)
            {
                port = Dataconnection.ConnectionProp.Port;
            }
            if (Dataconnection.ConnectionProp.KeyToken.Length > 0)
            {
                keyToken = Dataconnection.ConnectionProp.KeyToken;
            }
            else
            {
                if (Environment.GetEnvironmentVariable("CouchBase_TOKEN") != null)
                {
                    baseUrl = Environment.GetEnvironmentVariable("CouchBase_TOKEN")!;
                }
                else
                {
                    keyToken = "MH8XkA5B_dDp99-tEurjOsYTU8tuBNu7bigSGg77YfsBdMQ0bHeDyqyhiVqKOWyEIqxkqzfgDayEaJPinyCRDA==";
                }
            }
            if (Dataconnection.ConnectionProp != null)
            {
                Category = Dataconnection.ConnectionProp.Category;
                _connectionString = Dataconnection.ConnectionProp.Url; // Ensure this contains the correct connection format
                _username = Dataconnection.ConnectionProp.UserID;
                _password = Dataconnection.ConnectionProp.Password;
            }
            if (CurrentDatabase != null)
            {
                if (CurrentDatabase.Length > 0)
                {
                    BucketName = CurrentDatabase;
                    //   _client = new InfluxDBClient($"{url}:{port}", keyToken);
                    GetEntitesList();
                }
            }

            GuidID = Guid.NewGuid().ToString();
        }
        public string baseUrl { get; set; } = "http://localhost";
        public int port { get; set; } = 8086;
        public string keyToken { get; set; }
        public string BucketName { get; set; }  
        public ICluster _cluster { get; set; }
        public HttpClient httpClient { get; set; } = new HttpClient();
        public string CurrentDatabase { get; set; }
        public string ColumnDelimiter { get  ; set  ; }
        public string ParameterDelimiter { get  ; set  ; }
        public string GuidID { get  ; set  ; }
        public DataSourceType DatasourceType { get  ; set  ; }= DataSourceType.Couchbase;
        public DatasourceCategory Category { get  ; set  ; }= DatasourceCategory.NOSQL;

        private string _connectionString;
        private string _username;
        private string _password;

        public IDataConnection Dataconnection { get  ; set  ; }
        public string DatasourceName { get  ; set  ; }
        public IErrorsInfo ErrorObject { get  ; set  ; }
        public string Id { get  ; set  ; }
        public IDMLogger Logger { get  ; set  ; }
        public List<string> EntitiesNames { get  ; set  ; }=new List<string>();
        public List<EntityStructure> Entities { get  ; set  ; }=new List<EntityStructure>();
        public IDMEEditor DMEEditor { get  ; set  ; }
        public ConnectionState ConnectionStatus { get  ; set  ; }

        public event EventHandler<PassedArgs> PassEvent;

        public IErrorsInfo BeginTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                // Couchbase transactions are handled at the cluster level
                // Transactions are managed implicitly with write operations
                if (_cluster == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in Begin Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public bool CheckEntityExist(string EntityName)
        {
            try
            {
                if (EntitiesNames != null && EntitiesNames.Count > 0)
                {
                    return EntitiesNames.Any(e => e.Equals(EntityName, StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    GetEntitesList();
                    return EntitiesNames.Any(e => e.Equals(EntityName, StringComparison.OrdinalIgnoreCase));
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error checking entity existence: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return false;
            }
        }
        public ConnectionState Openconnection()
        {
            try
            {
                var options = new ClusterOptions
                {
                    ConnectionString = _connectionString,
                    UserName = _username,
                    Password = _password
                };
                _cluster =  Cluster.ConnectAsync(options).Result;
                ConnectionStatus = ConnectionState.Open;
                DMEEditor.AddLogMessage("Info", "Connection opened successfully.", DateTime.Now, -1, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Closed;
                DMEEditor.AddLogMessage("Beep", $"Failed to open connection: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ConnectionStatus;
        }

        public ConnectionState Closeconnection()
        {
            try
            {
                if (_cluster != null)
                {
                    _cluster.Dispose();
                    _cluster = null;
                }
                ConnectionStatus = ConnectionState.Closed;
                DMEEditor.AddLogMessage("Info", "Connection closed successfully.", DateTime.Now, -1, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                DMEEditor.AddLogMessage("Beep", $"Failed to close connection: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ConnectionStatus;
        }
       

        public IErrorsInfo Commit(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                // Couchbase commits are handled automatically with write operations
                // No explicit commit needed
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in Commit Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                foreach (var entity in entities)
                {
                    CreateEntityAs(entity);
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in CreateEntities: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            bool retval = false;
            try
            {
                if (_cluster == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    // In Couchbase, collections are created through the management API
                    // For now, we'll add the entity to our local structures
                    if (Entities == null)
                    {
                        Entities = new List<EntityStructure>();
                    }
                    if (!Entities.Any(p => p.EntityName == entity.EntityName))
                    {
                        Entities.Add(entity);
                    }
                    if (EntitiesNames == null)
                    {
                        EntitiesNames = new List<string>();
                    }
                    if (!EntitiesNames.Contains(entity.EntityName))
                    {
                        EntitiesNames.Add(entity.EntityName);
                    }
                    retval = true;
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in CreateEntityAs: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                retval = false;
            }
            return retval;
        }

        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok, Message = "Data deleted successfully." };
            try
            {
                if (_cluster == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    var bucket = _cluster.BucketAsync(BucketName).Result;
                    var collection = bucket.DefaultCollection();
                    
                    // Get document ID from UploadDataRow
                    string docId = GetDocumentId(UploadDataRow);
                    if (string.IsNullOrEmpty(docId))
                    {
                        retval.Flag = Errors.Failed;
                        retval.Message = "Could not extract document ID from UploadDataRow";
                        return retval;
                    }

                    var result = collection.RemoveAsync(docId).Result;
                    if (result.Status == KeyValueStatus.Success)
                    {
                        retval.Flag = Errors.Ok;
                        retval.Message = "Document deleted successfully.";
                    }
                    else
                    {
                        retval.Flag = Errors.Failed;
                        retval.Message = $"Failed to delete document: {result.Status}";
                    }
                }
                else
                {
                    retval.Flag = Errors.Failed;
                    retval.Message = "Database connection is not open.";
                }
            }
            catch (Exception ex)
            {
                retval.Flag = Errors.Failed;
                retval.Message = $"Error deleting document: {ex.Message}";
                DMEEditor?.AddLogMessage("Beep", $"Error deleting document: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return retval;
        }

        public IErrorsInfo EndTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                // Couchbase transactions end automatically with write operations
                // No explicit end needed
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in End Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public IErrorsInfo ExecuteSql(string sql)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (_cluster == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    var result = _cluster.QueryAsync<dynamic>(sql).Result;
                    // Execute the query
                    foreach (var row in result)
                    {
                        // Process results if needed
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in ExecuteSql: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public IEnumerable<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            // Couchbase doesn't have traditional child tables
            return new List<ChildRelation>();
        }

        public IEnumerable<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            List<ETLScriptDet> scripts = new List<ETLScriptDet>();
            try
            {
                var entitiesToScript = entities ?? Entities;
                if (entitiesToScript != null && entitiesToScript.Count > 0)
                {
                    foreach (var entity in entitiesToScript)
                    {
                        var script = new ETLScriptDet
                        {
                            EntityName = entity.EntityName,
                            ScriptType = "CREATE",
                            ScriptText = $"# Couchbase collection: {entity.EntityName}\n# Collections are created through management API"
                        };
                        scripts.Add(script);
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in GetCreateEntityScript: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return scripts;
        }

        public IEnumerable<string> GetEntitesList()
        {
            List<string> collectionNames = new List<string>();
            try
            {
                if (_cluster == null || ConnectionStatus != ConnectionState.Open)
                {
                     Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    var bucket =  _cluster.BucketAsync(BucketName).Result;
                    var collections =  bucket.Collections.GetAllScopesAsync().Result;
                    foreach (var scope in collections)
                    {
                        foreach (var collection in scope.Collections)
                        {
                            collectionNames.Add($"{scope.Name}.{collection.Name}"); // Format: scopeName.collectionName
                        }
                    }
                    DMEEditor.AddLogMessage("Info", "Successfully retrieved list of collections.", DateTime.Now, -1, null, Errors.Ok);
                }
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Closed;
                DMEEditor.AddLogMessage("Beep", $"Failed to retrieve collections: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return collectionNames;


        }

        public IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            List<object> results = new List<object>();
            try
            {
                if (_cluster == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    // Build N1QL query
                    var queryBuilder = new StringBuilder($"SELECT * FROM `{BucketName}` WHERE _type = '{EntityName}'");
                    
                    if (filter != null && filter.Count > 0)
                    {
                        foreach (var f in filter)
                        {
                            queryBuilder.Append($" AND `{f.FieldName}` {f.Operator} {PrepareValue(f.FilterValue, f.valueType)}");
                        }
                    }

                    var query = queryBuilder.ToString();
                    var result = _cluster.QueryAsync<dynamic>(query).Result;

                    foreach (var row in result)
                    {
                        results.Add(row);
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in GetEntity: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return results;
        }

        public PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            PagedResult pagedResult = new PagedResult();
            ErrorObject.Flag = Errors.Ok;

            try
            {
                if (_cluster == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    // Build count query
                    var countQueryBuilder = new StringBuilder($"SELECT COUNT(*) as total FROM `{BucketName}` WHERE _type = '{EntityName}'");
                    if (filter != null && filter.Count > 0)
                    {
                        foreach (var f in filter)
                        {
                            countQueryBuilder.Append($" AND `{f.FieldName}` {f.Operator} {PrepareValue(f.FilterValue, f.valueType)}");
                        }
                    }

                    var countQuery = countQueryBuilder.ToString();
                    var countResult = _cluster.QueryAsync<dynamic>(countQuery).Result;
                    int totalRecords = 0;
                    foreach (var row in countResult)
                    {
                        totalRecords = Convert.ToInt32(row.total);
                        break;
                    }

                    // Build paginated query
                    int offset = (pageNumber - 1) * pageSize;
                    var queryBuilder = new StringBuilder($"SELECT * FROM `{BucketName}` WHERE _type = '{EntityName}'");
                    if (filter != null && filter.Count > 0)
                    {
                        foreach (var f in filter)
                        {
                            queryBuilder.Append($" AND `{f.FieldName}` {f.Operator} {PrepareValue(f.FilterValue, f.valueType)}");
                        }
                    }
                    queryBuilder.Append($" LIMIT {pageSize} OFFSET {offset}");

                    var query = queryBuilder.ToString();
                    var result = _cluster.QueryAsync<dynamic>(query).Result;

                    List<object> results = new List<object>();
                    foreach (var row in result)
                    {
                        results.Add(row);
                    }

                    pagedResult.Data = results;
                    pagedResult.TotalRecords = totalRecords;
                    pagedResult.PageNumber = pageNumber;
                    pagedResult.PageSize = pageSize;
                    pagedResult.TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
                    pagedResult.HasNextPage = pageNumber < pagedResult.TotalPages;
                    pagedResult.HasPreviousPage = pageNumber > 1;
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in GetEntity with pagination: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return pagedResult;
        }

        public Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            return Task.Run(() => GetEntity(EntityName, Filter));
        }

        public IEnumerable<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            // Couchbase doesn't have traditional foreign keys
            return new List<RelationShipKeys>();
        }

        public int GetEntityIdx(string entityName)
        {
            try
            {
                if (Entities == null || Entities.Count == 0)
                {
                    GetEntitesList();
                }
                return Entities.FindIndex(e => e.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in GetEntityIdx: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return -1;
            }
        }

        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            try
            {
                if (!refresh && Entities != null && Entities.Count > 0)
                {
                    var entity = Entities.FirstOrDefault(e => e.EntityName.Equals(EntityName, StringComparison.OrdinalIgnoreCase));
                    if (entity != null)
                    {
                        return entity;
                    }
                }

                // Try to get sample document to infer structure
                if (_cluster != null && ConnectionStatus == ConnectionState.Open)
                {
                    var query = $"SELECT * FROM `{BucketName}` WHERE _type = '{EntityName}' LIMIT 1";
                    var result = _cluster.QueryAsync<JObject>(query).Result;

                    foreach (var row in result)
                    {
                        return InferSchemaFromDocument(row, BucketName, EntityName);
                    }
                }

                // Return empty structure if not found
                return new EntityStructure
                {
                    EntityName = EntityName,
                    Fields = new List<EntityField>()
                };
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in GetEntityStructure: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return new EntityStructure { EntityName = EntityName, Fields = new List<EntityField>() };
            }
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            return GetEntityStructure(fnd.EntityName, refresh);
        }

        public Type GetEntityType(string EntityName)
        {
            try
            {
                var entityStructure = GetEntityStructure(EntityName, false);
                if (entityStructure != null && entityStructure.Fields != null && entityStructure.Fields.Count > 0)
                {
                    // Use DMTypeBuilder to create type from entity structure
                    if (DMEEditor != null)
                    {
                        string code = DMTypeBuilder.ConvertPOCOClassToEntity(DMEEditor, entityStructure, "CouchbaseGeneratedTypes");
                        return DMTypeBuilder.CreateTypeFromCode(DMEEditor, code, EntityName);
                    }
                }
                return typeof(object);
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in GetEntityType: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return typeof(object);
            }
        }

        public double GetScalar(string query)
        {
            try
            {
                if (_cluster == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    var result = _cluster.QueryAsync<dynamic>(query).Result;
                    foreach (var row in result)
                    {
                        if (row != null)
                        {
                            var firstValue = row.Values.FirstOrDefault();
                            if (firstValue != null && double.TryParse(firstValue.ToString(), out double value))
                            {
                                return value;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in GetScalar: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return 0.0;
        }

        public Task<double> GetScalarAsync(string query)
        {
            return Task.Run(() => GetScalar(query));
        }

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            ErrorsInfo retval = new ErrorsInfo();
            try
            {
                if (_cluster == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    
                    
                    // Serialize the inserted data to JSON
                    string jsonData = SerializeInsertedData(InsertedData);

                    // Generate a unique ID for the document, in real scenarios consider a meaningful ID strategy
                    var docId = Guid.NewGuid().ToString();
                    string uri = $"{baseUrl}/pools/default/buckets/{BucketName}/docs/{docId}"; // Assuming baseUrl is the base URL for your Couchbase server

                    using (var httpClient = new HttpClient())
                    {
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_username}:{_password}")));

                        var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                        var response =  httpClient.PostAsync(uri, content).Result;

                        if (response.IsSuccessStatusCode)
                        {
                            retval.Flag = Errors.Ok;
                            retval.Message = "Document inserted successfully.";
                            DMEEditor.AddLogMessage("Info", "Document inserted successfully.", DateTime.Now, -1, null, Errors.Ok);
                        }
                        else
                        {
                            retval.Flag = Errors.Failed;
                            retval.Message = $"Failed to insert document: {response.StatusCode} - {response.ReasonPhrase}";
                            DMEEditor.AddLogMessage("Beep", $"Failed to insert document: {response.StatusCode} - {response.ReasonPhrase}", DateTime.Now, -1, null, Errors.Failed);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                retval.Flag = Errors.Failed;
                retval.Message = $"Error inserting document: {ex.Message}";
                DMEEditor.AddLogMessage("Beep", $"Error inserting document: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return retval;
        }

    

        public IEnumerable<object> RunQuery(string qrystr)
        {
            List<object> results = new List<object>();
            try
            {
                if (_cluster == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    var result = _cluster.QueryAsync<dynamic>(qrystr).Result;
                    foreach (var row in result)
                    {
                        results.Add(row);
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in RunQuery: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return results;
        }

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (dDLScripts != null && !string.IsNullOrEmpty(dDLScripts.ScriptText))
                {
                    ExecuteSql(dDLScripts.ScriptText);
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in RunScript: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok, Message = "Data updated successfully." };
            try
            {
                if (UploadData is IEnumerable<object> dataList)
                {
                    int count = 0;
                    foreach (var item in dataList)
                    {
                        UpdateEntity(EntityName, item);
                        count++;
                        progress?.Report(new PassedArgs { Message = $"Updated {count} records" });
                    }
                    retval.Message = $"Updated {count} records successfully.";
                }
                else
                {
                    retval.Flag = Errors.Failed;
                    retval.Message = "UploadData must be an IEnumerable<object>.";
                }
            }
            catch (Exception ex)
            {
                retval.Flag = Errors.Failed;
                retval.Message = $"Error in UpdateEntities: {ex.Message}";
                DMEEditor?.AddLogMessage("Beep", $"Error in UpdateEntities: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return retval;
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok, Message = "Data updated successfully." };
            try
            {
                if (_cluster == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    var bucket = _cluster.BucketAsync(BucketName).Result;
                    var collection = bucket.DefaultCollection();

                    // Get document ID from UploadDataRow
                    string docId = GetDocumentId(UploadDataRow);
                    if (string.IsNullOrEmpty(docId))
                    {
                        retval.Flag = Errors.Failed;
                        retval.Message = "Could not extract document ID from UploadDataRow";
                        return retval;
                    }

                    // Serialize the updated data
                    string jsonData = SerializeInsertedData(UploadDataRow);
                    var result = collection.UpsertAsync(docId, jsonData).Result;

                    if (result.Status == KeyValueStatus.Success)
                    {
                        retval.Flag = Errors.Ok;
                        retval.Message = "Document updated successfully.";
                    }
                    else
                    {
                        retval.Flag = Errors.Failed;
                        retval.Message = $"Failed to update document: {result.Status}";
                    }
                }
                else
                {
                    retval.Flag = Errors.Failed;
                    retval.Message = "Database connection is not open.";
                }
            }
            catch (Exception ex)
            {
                retval.Flag = Errors.Failed;
                retval.Message = $"Error updating document: {ex.Message}";
                DMEEditor?.AddLogMessage("Beep", $"Error updating document: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return retval;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    httpClient?.Dispose(); // Dispose the HttpClient when done
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~CouchDataSource()
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
        public async Task<List<string>> GetDocumentTypes(string bucketName)
        {
            var types = new List<string>();
            try
            {
                if (_cluster == null || ConnectionStatus != ConnectionState.Open)
                {
                     Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    // Constructing a query to get distinct document types
                    var query = $"SELECT DISTINCT type FROM `{bucketName}` WHERE type IS NOT MISSING";
                    // Executing the query through the cluster
                    var result = await _cluster.QueryAsync<dynamic>(query);

                    // Iterating through the query results and collecting types
                    await foreach (var row in result)
                    {
                        types.Add(row.type.ToString());
                    }
                    DMEEditor.AddLogMessage("Info", "Successfully retrieved document types.", DateTime.Now, -1, null, Errors.Ok);
                }
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Closed;
                DMEEditor.AddLogMessage("Beep", $"Error retrieving document types: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return types;
        }

        public async Task LoadBucketAndCollectionData()
        {
            try
            {
                EntitiesNames.Clear();
                Entities.Clear();
                // Step 1: Retrieve all buckets
                var buckets = await GetBucketsAsync(); // This should return a List<string> of bucket names

                // Step 2: For each bucket, retrieve collections and fill EntitiesNames and Entities
                foreach (var bucketName in buckets)
                {
                    var collections = await GetCollectionsAsync(bucketName); // This should return List<string> of collection names
                    foreach (var collectionName in collections)
                    {
                        // Add collection name to EntitiesNames
                        EntitiesNames.Add(collectionName);

                        // Attempt to fetch a sample document to infer its schema
                        var sampleDocument = await GetSampleDocumentAsync(bucketName, collectionName);
                        if (sampleDocument != null)
                        {
                            // Create and add new EntityStructure to Entities
                            var entity = InferSchemaFromDocument(sampleDocument, bucketName, collectionName);
                            Entities.Add(entity);
                        }
                        else
                        {
                            // Handle cases where no sample document is found
                            var entity = new EntityStructure
                            {
                                EntityName = collectionName,
                                SchemaOrOwnerOrDatabase = bucketName, // Use bucket name as the schema/database owner
                                Fields = new List<EntityField>() // Initialize without fields if no document is available
                            };
                            Entities.Add(entity);
                        }
                    }
                }

                // Step 3: Set bucket names to Databases
                Dataconnection.ConnectionProp.Databases = buckets;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error loading bucket and collection data: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
        }
        private async Task<JObject> GetSampleDocumentAsync(string bucketName, string collectionName)
        {
            
            var bucket = await _cluster.BucketAsync(bucketName);
            var collection = bucket.Scope("_default").Collection(collectionName);
            try
            {
                var result = await collection.GetAsync("known_document_id"); // Use a known ID or modify to use a query
                return JObject.Parse(result.ContentAs<string>());
            }
            catch
            {
                // Handle the case where the document cannot be fetched
                return null;
            }
        }

        private async Task<List<string>> GetBucketsAsync()
        {
            var client = new HttpClient(); // Ensure this client is configured to authenticate with your Couchbase server
            var url = $"http://{baseUrl}:8091/pools/default/buckets"; // Replace with your actual Couchbase server URL and port
            var bucketNames = new List<string>();

            try
            {
                HttpResponseMessage response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    var buckets = JsonConvert.DeserializeObject<List<dynamic>>(content);
                    foreach (var bucket in buckets)
                    {
                        bucketNames.Add((string)bucket.name);
                    }
                }
                else
                {
                    throw new Exception($"Failed to retrieve buckets: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                // Log or handle exceptions here
                throw new Exception($"Error retrieving buckets from Couchbase: {ex.Message}");
            }

            return bucketNames;
        }

        private async Task<List<string>> GetCollectionsAsync(string bucketName)
        {
            var client = new HttpClient(); // Ensure this client is configured to authenticate with your Couchbase server
            var url = $"http://{baseUrl}:8091/pools/default/buckets/{bucketName}/scopes"; // Replace with your actual Couchbase server URL and port
            var collectionNames = new List<string>();

            try
            {
                HttpResponseMessage response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    var scopes = JsonConvert.DeserializeObject<dynamic>(content).scopes;

                    foreach (var scope in scopes)
                    {
                        foreach (var collection in scope.collections)
                        {
                            collectionNames.Add((string)collection.name);
                        }
                    }
                }
                else
                {
                    throw new Exception($"Failed to retrieve collections for bucket {bucketName}: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                // Log or handle exceptions here
                throw new Exception($"Error retrieving collections from Couchbase bucket '{bucketName}': {ex.Message}");
            }

            return collectionNames;
        }
        private EntityStructure InferSchemaFromDocument(JObject document, string bucketName, string collectionName)
        {
            var entity = new EntityStructure
            {
                EntityName = collectionName,
                SchemaOrOwnerOrDatabase = bucketName,
                Fields = new List<EntityField>()
            };

            foreach (var property in document.Properties())
            {
                var field = new EntityField
                {
                    fieldname = property.Name,
                    fieldtype = GetTypeNameFromJToken(property.Value)
                };
                entity.Fields.Add(field);
            }

            return entity;
        }

        private string GetTypeNameFromJToken(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.String:
                    return "string";
                case JTokenType.Integer:
                    return "int";
                case JTokenType.Float:
                    return "float";
                case JTokenType.Boolean:
                    return "bool";
                case JTokenType.Date:
                    return "DateTime";
                case JTokenType.Array:
                    return "array";
                case JTokenType.Object:
                    return "object";
                default:
                    return "unknown";
            }
        }

        private string SerializeInsertedData(object data)
        {
            if (data is DataRow)
            {
                // Convert DataRow to Dictionary for serialization
                DataRow dataRow = (DataRow)data;
                Dictionary<string, object> rowDict = dataRow.Table.Columns
                    .Cast<DataColumn>()
                    .ToDictionary(col => col.ColumnName, col => dataRow[col]);
                return JsonConvert.SerializeObject(rowDict);
            }
            else if (data.GetType().IsClass)
            {
                // Directly serialize POCO classes
                return JsonConvert.SerializeObject(data);
            }
            else
            {
                throw new ArgumentException("Unsupported data type for insertion. Expected DataRow or POCO class.");
            }
        }

        private string GetDocumentId(object data)
        {
            try
            {
                if (data is Dictionary<string, object> dict)
                {
                    if (dict.ContainsKey("id")) return dict["id"].ToString();
                    if (dict.ContainsKey("_id")) return dict["_id"].ToString();
                    if (dict.ContainsKey("Id")) return dict["Id"].ToString();
                }
                else if (data != null)
                {
                    var idProp = data.GetType().GetProperty("id") ?? 
                                 data.GetType().GetProperty("_id") ?? 
                                 data.GetType().GetProperty("Id");
                    if (idProp != null)
                    {
                        return idProp.GetValue(data)?.ToString();
                    }
                }
                return Guid.NewGuid().ToString();
            }
            catch
            {
                return Guid.NewGuid().ToString();
            }
        }

        private string PrepareValue(string value, string valueType)
        {
            if (string.IsNullOrEmpty(value))
                return "NULL";

            // For string types, wrap in quotes
            if (valueType == null || valueType.ToLower().Contains("string") || valueType.ToLower().Contains("text"))
            {
                return $"'{value.Replace("'", "''")}'";
            }

            // For numeric types, return as-is
            return value;
        }
    }
}
