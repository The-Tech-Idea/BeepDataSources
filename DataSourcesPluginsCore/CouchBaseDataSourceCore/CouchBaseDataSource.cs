using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using TheTechIdea;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using TheTechIdea.Beep.WebAPI;
using Newtonsoft.Json;
using Couchbase;
using Newtonsoft.Json.Linq;
using System.Reflection.Metadata;
using System.Net.Http.Headers;

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
            throw new NotImplementedException();
        }

        public bool CheckEntityExist(string EntityName)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            throw new NotImplementedException();
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo EndTransaction(PassedArgs args)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo ExecuteSql(string sql)
        {
            throw new NotImplementedException();
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

        public object GetEntity(string EntityName, List<AppFilter> filter)
        {
            throw new NotImplementedException();
        }

        public object GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            throw new NotImplementedException();
        }

        public Task<object> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            throw new NotImplementedException();
        }

        public List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            throw new NotImplementedException();
        }

        public int GetEntityIdx(string entityName)
        {
            throw new NotImplementedException();
        }

        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            throw new NotImplementedException();
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            throw new NotImplementedException();
        }

        public Type GetEntityType(string EntityName)
        {
            throw new NotImplementedException();
        }

        public double GetScalar(string query)
        {
            throw new NotImplementedException();
        }

        public Task<double> GetScalarAsync(string query)
        {
            throw new NotImplementedException();
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

    

        public object RunQuery(string qrystr)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            throw new NotImplementedException();
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
    }
}
