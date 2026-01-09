using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Connections;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;

namespace TheTechIdea.Beep.ShapVectorDatasource
{
    //[AddinAttribute(Category = DatasourceCategory.VectorDB, DatasourceType = DataSourceType.ShapVector)]
    public class SharpVectorDatasource : IDataSource, IInMemoryDB
    {
        private HttpClient _httpClient;
        private string _baseUrl;
        private JsonSerializerOptions _jsonOptions;

        // Constructor with standard signature for IDataSource implementations
        public SharpVectorDatasource(string pdatasourcename, IDMLogger plogger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
        {
            DMEEditor = pDMEEditor;
            Logger = plogger;
            ErrorObject = per ?? DMEEditor.ErrorObject;
            DatasourceName = pdatasourcename;
            DatasourceType = DataSourceType.ShapVector;
            Category = DatasourceCategory.VectorDB;
            EntitiesNames = new List<string>();
            Entities = new List<EntityStructure>();
            InMemoryStructures = new List<EntityStructure>();

            if (pdatasourcename != null)
            {
                // Initialize data connection with connection properties
                Dataconnection = new DefaulDataConnection
                {
                    DMEEditor = DMEEditor,
                    ConnectionProp = DMEEditor.ConfigEditor.DataConnections.FirstOrDefault(
                        p => p.ConnectionName.Equals(pdatasourcename, StringComparison.InvariantCultureIgnoreCase))
                };

                if (Dataconnection.ConnectionProp == null)
                {
                    Dataconnection.ConnectionProp = new ConnectionProperties();
                }
            }

            ColumnDelimiter = "[]";
            ParameterDelimiter = "$";
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
        }

        #region "IDataSource EVENTS"
        public event EventHandler<PassedArgs> PassEvent;
        #endregion "IDataSource EVENTS"

        #region "IInMemoryDB EVENTS"
        public event EventHandler<PassedArgs> OnLoadData;
        public event EventHandler<PassedArgs> OnLoadStructure;
        public event EventHandler<PassedArgs> OnSaveStructure;
        public event EventHandler<PassedArgs> OnCreateStructure;
        public event EventHandler<PassedArgs> OnRefreshData;
        public event EventHandler<PassedArgs> OnRefreshDataEntity;
        public event EventHandler<PassedArgs> OnSyncData;
        #endregion "IInMemoryDB EVENTS"

        #region "IDataSource PROPERTIES"
        public string ColumnDelimiter { get; set; }
        public string ParameterDelimiter { get; set; }
        public string GuidID { get; set; }
        public DataSourceType DatasourceType { get; set; }
        public DatasourceCategory Category { get; set; }
        public IDataConnection Dataconnection { get; set; }
        public string DatasourceName { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public string Id { get; set; }
        public IDMLogger Logger { get; set; }
        public List<string> EntitiesNames { get; set; }
        public List<EntityStructure> Entities { get; set; }
        public IDMEEditor DMEEditor { get; set; }
        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Closed;
        public bool IsCreated { get; set; }
        public bool IsLoaded { get; set; }
        public bool IsSaved { get; set; }
        public bool IsSynced { get; set; }
        public ETLScriptHDR CreateScript { get; set; }
        public bool IsStructureCreated { get; set; }
        public List<EntityStructure> InMemoryStructures { get; set; }
        #endregion "IDataSource PROPERTIES"

        #region "IDataSource METHODS"
        public ConnectionState Openconnection()
        {
            try
            {
                if (ConnectionStatus == ConnectionState.Open)
                {
                    return ConnectionStatus;
                }

                // Initialize HTTP client for SharpVector API
                _httpClient = new HttpClient();

                // Get connection details from connection properties
                string apiKey = Dataconnection.ConnectionProp.ApiKey;
                if (string.IsNullOrEmpty(apiKey))
                {
                    // Fall back to Password if ApiKey is not set
                    apiKey = Dataconnection.ConnectionProp.Password;
                }

                // Set default headers for all requests
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add API key if provided
                if (!string.IsNullOrEmpty(apiKey))
                {
                    _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                }

                // Determine the base URL
                if (!string.IsNullOrEmpty(Dataconnection.ConnectionProp.Url))
                {
                    _baseUrl = Dataconnection.ConnectionProp.Url.TrimEnd('/');
                }
                else
                {
                    string host = Dataconnection.ConnectionProp.Host ?? "localhost";
                    int port = Dataconnection.ConnectionProp.Port > 0 ? Dataconnection.ConnectionProp.Port : 8080;
                    _baseUrl = $"http://{host}:{port}";
                }

                // Test connection by trying to list indexes
                try
                {
                    var collections = ListCollections().Result;
                    ConnectionStatus = ConnectionState.Open;
                    DMEEditor.AddLogMessage("Success", $"Connected to SharpVector at {_baseUrl}", DateTime.Now, -1, "", Errors.Ok);
                }
                catch (Exception ex)
                {
                    DMEEditor.AddLogMessage("Error", $"Failed to connect to SharpVector: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                    ConnectionStatus = ConnectionState.Broken;
                }

                return ConnectionStatus;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", $"Error opening SharpVector connection: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                ConnectionStatus = ConnectionState.Broken;
                return ConnectionStatus;
            }
        }

        public ConnectionState Closeconnection()
        {
            _httpClient?.Dispose();
            _httpClient = null;
            ConnectionStatus = ConnectionState.Closed;
            DMEEditor.AddLogMessage("Success", "Closed SharpVector connection", DateTime.Now, -1, "", Errors.Ok);
            return ConnectionStatus;
        }

        public IErrorsInfo BeginTransaction(PassedArgs args)
        {
            // SharpVector doesn't support traditional transactions
            ErrorObject.Flag = Errors.Warning;
            ErrorObject.Message = "Transactions are not supported by SharpVector";
            return ErrorObject;
        }

        public bool CheckEntityExist(string EntityName)
        {
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                var collections = ListCollections().Result;
                return collections.Contains(EntityName);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", $"Error checking if collection exists: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                return false;
            }
        }

        public IErrorsInfo Commit(PassedArgs args)
        {
            // SharpVector doesn't support traditional transactions
            ErrorObject.Flag = Errors.Warning;
            ErrorObject.Message = "Transactions are not supported by SharpVector";
            return ErrorObject;
        }

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            ErrorObject.Flag = Errors.Ok;

            try
            {
                foreach (var entity in entities)
                {
                    try
                    {
                        // Create a SharpVector index for each entity
                        int dimension = 1536; // Default dimension
                        string metric = "cosine"; // Default metric

                        // Try to get dimension from entity properties if available
                        var dimensionField = entity.Fields.FirstOrDefault(f => f.fieldname.Equals("dimension", StringComparison.OrdinalIgnoreCase));
                        if (dimensionField != null && int.TryParse(dimensionField.DefaultValue, out int dim))
                        {
                            dimension = dim;
                        }

                        // Try to get metric from entity properties if available
                        var metricField = entity.Fields.FirstOrDefault(f => f.fieldname.Equals("metric", StringComparison.OrdinalIgnoreCase));
                        if (metricField != null && !string.IsNullOrEmpty(metricField.DefaultValue))
                        {
                            metric = metricField.DefaultValue;
                        }

                        CreateCollection(entity.EntityName, dimension, metric).Wait();
                    }
                    catch (Exception ex)
                    {
                        DMEEditor.AddLogMessage("Error", $"Failed to create index {entity.EntityName}: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                        ErrorObject.Flag = Errors.Failed;
                        ErrorObject.Message = $"Failed to create one or more indexes";
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Error creating entities: {ex.Message}";
            }

            return ErrorObject;
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            try
            {
                int dimension = 1536; // Default dimension
                string metric = "cosine"; // Default metric

                // Try to get dimension from entity properties if available
                var dimensionField = entity.Fields.FirstOrDefault(f => f.fieldname.Equals("dimension", StringComparison.OrdinalIgnoreCase));
                if (dimensionField != null && int.TryParse(dimensionField.DefaultValue, out int dim))
                {
                    dimension = dim;
                }

                // Try to get metric from entity properties if available
                var metricField = entity.Fields.FirstOrDefault(f => f.fieldname.Equals("metric", StringComparison.OrdinalIgnoreCase));
                if (metricField != null && !string.IsNullOrEmpty(metricField.DefaultValue))
                {
                    metric = metricField.DefaultValue;
                }

                CreateCollection(entity.EntityName, dimension, metric).Wait();
                return true;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", $"Failed to create collection {entity.EntityName}: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                return false;
            }
        }

        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            ErrorObject.Flag = Errors.Ok;

            try
            {
                if (UploadDataRow is List<string> ids)
                {
                    DeleteVectors(EntityName, ids).Wait();
                }
                else if (UploadDataRow is string id)
                {
                    DeleteVectors(EntityName, new List<string> { id }).Wait();
                }
                else
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = "Expected a List<string> of vector IDs or a single string ID";
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Error deleting vectors: {ex.Message}";
            }

            return ErrorObject;
        }

        public IErrorsInfo EndTransaction(PassedArgs args)
        {
            // SharpVector doesn't support traditional transactions
            ErrorObject.Flag = Errors.Warning;
            ErrorObject.Message = "Transactions are not supported by SharpVector";
            return ErrorObject;
        }

        public IErrorsInfo ExecuteSql(string sql)
        {
            ErrorObject.Flag = Errors.Warning;
            ErrorObject.Message = "SQL execution is not supported by SharpVector";
            return ErrorObject;
        }

        public IEnumerable<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            // SharpVector doesn't have traditional parent-child relationships
            return new List<ChildRelation>();
        }

        public IEnumerable<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            // Not applicable for SharpVector
            return new List<ETLScriptDet>();
        }

        public IEnumerable<string> GetEntitesList()
        {
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                EntitiesNames = ListCollections().Result;
                return EntitiesNames;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", $"Error getting index list: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                return new List<string>();
            }
        }

        public IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                // For SharpVector, we'll return information about the index
                var indexInfo = GetCollectionInfo(EntityName).Result;
                return indexInfo != null ? new[] { indexInfo } : Array.Empty<object>();
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", $"Error getting index: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                return Array.Empty<object>();
            }
        }

        public PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                var allData = GetEntity(EntityName, filter).ToList();
                var totalRecords = allData.Count;
                var pagedData = allData.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
                
                return new PagedResult
                {
                    Data = pagedData,
                    PageNumber = Math.Max(1, pageNumber),
                    PageSize = pageSize,
                    TotalRecords = totalRecords,
                    TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                    HasPreviousPage = pageNumber > 1,
                    HasNextPage = pageNumber * pageSize < totalRecords
                };
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor.AddLogMessage("Error", $"Error getting index with pagination: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                return new PagedResult(Array.Empty<object>(), pageNumber, pageSize, 0);
            }
        }

        public Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            return Task.FromResult(GetEntity(EntityName, Filter));
        }

        public IEnumerable<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            // SharpVector doesn't have traditional foreign keys
            return new List<RelationShipKeys>();
        }

        public int GetEntityIdx(string entityName)
        {
            if (EntitiesNames == null || !EntitiesNames.Any())
            {
                GetEntitesList();
            }

            return EntitiesNames.FindIndex(e => e.Equals(entityName, StringComparison.OrdinalIgnoreCase));
        }

        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                // Try to get existing entity structure
                var entity = Entities.FirstOrDefault(e => e.EntityName.Equals(EntityName, StringComparison.OrdinalIgnoreCase));

                if (entity == null || refresh)
                {
                    // Get index information
                    var indexInfo = GetCollectionInfo(EntityName).Result;

                    entity = new EntityStructure
                    {
                        EntityName = EntityName,
                        DatasourceEntityName = EntityName,
                        DataSourceID = DatasourceName,
                        DatabaseType = DataSourceType.ShapVector,
                        Caption = EntityName,
                        KeyToken = Guid.NewGuid().ToString(),
                        Fields = new List<EntityField>()
                    };

                    // Add standard fields for SharpVector index
                    entity.Fields.Add(new EntityField
                    {
                        fieldname = "id",
                        fieldtype = "System.String",
                        IsKey = true
                    });

                    entity.Fields.Add(new EntityField
                    {
                        fieldname = "vector",
                        fieldtype = "System.Single[]",
                        Description = "Vector values"
                    });

                    entity.Fields.Add(new EntityField
                    {
                        fieldname = "metadata",
                        fieldtype = "System.Object",
                        Description = "Vector metadata"
                    });

                    // Add any collection-specific fields from the index info
                    if (indexInfo.TryGetValue("dimension", out var dimensionValue))
                    {
                        entity.Fields.Add(new EntityField
                        {
                            fieldname = "dimension",
                            fieldtype = "System.Int32",
                            DefaultValue = dimensionValue.ToString(),
                            Description = "Vector dimension"
                        });
                    }

                    if (indexInfo.TryGetValue("metric", out var metricValue))
                    {
                        entity.Fields.Add(new EntityField
                        {
                            fieldname = "metric",
                            fieldtype = "System.String",
                            DefaultValue = metricValue.ToString(),
                            Description = "Distance metric"
                        });
                    }

                    // Add to the entities collection if new
                    if (!Entities.Any(e => e.EntityName.Equals(EntityName, StringComparison.OrdinalIgnoreCase)))
                    {
                        Entities.Add(entity);
                    }
                }

                return entity;
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor.AddLogMessage("Error", $"Error getting entity structure: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                return new EntityStructure { EntityName = EntityName };
            }
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            return GetEntityStructure(fnd.EntityName, refresh);
        }

        public Type GetEntityType(string EntityName)
        {
            // Not applicable for SharpVector
            return typeof(object);
        }

        public double GetScalar(string query)
        {
            // Not applicable for SharpVector
            throw new NotImplementedException("Scalar queries are not supported by SharpVector");
        }

        public Task<double> GetScalarAsync(string query)
        {
            // Not applicable for SharpVector
            throw new NotImplementedException("Scalar queries are not supported by SharpVector");
        }

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            ErrorObject.Flag = Errors.Ok;

            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                // For SharpVector, this would be upserting vector(s)
                if (InsertedData is Dictionary<string, object> vectorData)
                {
                    // Single vector upsert
                    var id = vectorData["id"]?.ToString() ?? Guid.NewGuid().ToString();
                    var vector = vectorData["vector"] as float[] ?? new float[0];
                    var metadata = vectorData["metadata"] as Dictionary<string, object> ?? new Dictionary<string, object>();

                    UpsertVector(EntityName, id, vector, metadata).Wait();
                }
                else if (InsertedData is List<Dictionary<string, object>> vectorsData)
                {
                    // Batch upsert
                    var vectors = new List<VectorData>();

                    foreach (var item in vectorsData)
                    {
                        var id = item["id"]?.ToString() ?? Guid.NewGuid().ToString();
                        var vector = item["vector"] as float[] ?? new float[0];
                        var metadata = item["metadata"] as Dictionary<string, object> ?? new Dictionary<string, object>();

                        vectors.Add(new VectorData { Id = id, Vector = vector, Metadata = metadata });
                    }

                    UpsertVectors(EntityName, vectors).Wait();
                }
                else
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = "Expected a Dictionary<string, object> or List<Dictionary<string, object>> containing vector data";
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Error inserting vectors: {ex.Message}";
            }

            return ErrorObject;
        }

        public IEnumerable<object> RunQuery(string qrystr)
        {
            // Not applicable for traditional SQL queries
            throw new NotImplementedException("SQL queries are not supported by SharpVector");
        }

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            // Not applicable for SharpVector
            ErrorObject.Flag = Errors.Warning;
            ErrorObject.Message = "Scripts are not supported by SharpVector";
            return ErrorObject;
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            // For SharpVector, this is the same as insert (upsert)
            return InsertEntity(EntityName, UploadData);
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            // For SharpVector, this is the same as insert (upsert)
            return InsertEntity(EntityName, UploadDataRow);
        }
        #endregion "IDataSource METHODS"

        #region "IInMemoryDB METHODS"
        public IErrorsInfo OpenDatabaseInMemory(string databasename)
        {
            ErrorObject.Flag = Errors.Warning;
            ErrorObject.Message = "In-memory operation not supported by SharpVector";
            return ErrorObject;
        }

        public string GetConnectionString()
        {
            return $"Url={_baseUrl};ApiKey={Dataconnection.ConnectionProp.ApiKey}";
        }

        public IErrorsInfo SaveStructure()
        {
            ErrorObject.Flag = Errors.Warning;
            ErrorObject.Message = "Structure saving not supported by SharpVector";
            return ErrorObject;
        }

        public IErrorsInfo LoadStructure(IProgress<PassedArgs> progress, CancellationToken token, bool copydata = false)
        {
            ErrorObject.Flag = Errors.Ok;

            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                var collections = ListCollections().Result;
                EntitiesNames = collections;

                foreach (var collection in collections)
                {
                    if (token.IsCancellationRequested)
                        break;

                    progress?.Report(new PassedArgs { Messege = $"Loading structure for index {collection}", EventType = "Loading" });
                    GetEntityStructure(collection, true);
                }

                IsStructureCreated = true;
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Error loading structure: {ex.Message}";
            }

            return ErrorObject;
        }

        public IErrorsInfo CreateStructure(IProgress<PassedArgs> progress, CancellationToken token)
        {
            ErrorObject.Flag = Errors.Warning;
            ErrorObject.Message = "Structure creation not supported by SharpVector";
            return ErrorObject;
        }

        public IErrorsInfo LoadData(IProgress<PassedArgs> progress, CancellationToken token)
        {
            ErrorObject.Flag = Errors.Warning;
            ErrorObject.Message = "Data loading not supported by SharpVector";
            return ErrorObject;
        }

        public IErrorsInfo SyncData(IProgress<PassedArgs> progress, CancellationToken token)
        {
            ErrorObject.Flag = Errors.Warning;
            ErrorObject.Message = "Data syncing not supported by SharpVector";
            return ErrorObject;
        }

        public IErrorsInfo SyncData(string entityname, IProgress<PassedArgs> progress, CancellationToken token)
        {
            ErrorObject.Flag = Errors.Warning;
            ErrorObject.Message = "Data syncing not supported by SharpVector";
            return ErrorObject;
        }

        public IErrorsInfo RefreshData(IProgress<PassedArgs> progress, CancellationToken token)
        {
            ErrorObject.Flag = Errors.Warning;
            ErrorObject.Message = "Data refreshing not supported by SharpVector";
            return ErrorObject;
        }

        public IErrorsInfo RefreshData(string entityname, IProgress<PassedArgs> progress, CancellationToken token)
        {
            ErrorObject.Flag = Errors.Warning;
            ErrorObject.Message = "Data refreshing not supported by SharpVector";
            return ErrorObject;
        }
        #endregion "IInMemoryDB METHODS"

        #region "SharpVector API Helpers"
        private class VectorData
        {
            public string Id { get; set; }
            public float[] Vector { get; set; }
            public Dictionary<string, object> Metadata { get; set; }
        }

        private async Task<List<string>> ListCollections()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/collections");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content, _jsonOptions);

            var collections = new List<string>();
            if (result.TryGetValue("collections", out var collectionsElement) &&
                collectionsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var collection in collectionsElement.EnumerateArray())
                {
                    if (collection.ValueKind == JsonValueKind.String)
                    {
                        collections.Add(collection.GetString());
                    }
                    else if (collection.TryGetProperty("name", out var nameElement))
                    {
                        collections.Add(nameElement.GetString());
                    }
                }
            }

            return collections;
        }

        private async Task<Dictionary<string, object>> GetCollectionInfo(string collectionName)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/collections/{collectionName}");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(content);

            var info = new Dictionary<string, object>();
            if (result.RootElement.ValueKind == JsonValueKind.Object)
            {
                ConvertJsonElementToDictionary(result.RootElement, info);
            }

            return info;
        }

        private void ConvertJsonElementToDictionary(JsonElement element, Dictionary<string, object> dict)
        {
            foreach (var property in element.EnumerateObject())
            {
                switch (property.Value.ValueKind)
                {
                    case JsonValueKind.Object:
                        var nestedDict = new Dictionary<string, object>();
                        ConvertJsonElementToDictionary(property.Value, nestedDict);
                        dict[property.Name] = nestedDict;
                        break;
                    case JsonValueKind.Array:
                        dict[property.Name] = ConvertJsonElementToList(property.Value);
                        break;
                    case JsonValueKind.String:
                        dict[property.Name] = property.Value.GetString();
                        break;
                    case JsonValueKind.Number:
                        if (property.Value.TryGetInt64(out long intValue))
                            dict[property.Name] = intValue;
                        else
                            dict[property.Name] = property.Value.GetDouble();
                        break;
                    case JsonValueKind.True:
                        dict[property.Name] = true;
                        break;
                    case JsonValueKind.False:
                        dict[property.Name] = false;
                        break;
                    case JsonValueKind.Null:
                        dict[property.Name] = null;
                        break;
                    default:
                        dict[property.Name] = property.Value.ToString();
                        break;
                }
            }
        }

        private List<object> ConvertJsonElementToList(JsonElement element)
        {
            var list = new List<object>();
            foreach (var item in element.EnumerateArray())
            {
                switch (item.ValueKind)
                {
                    case JsonValueKind.Object:
                        var dict = new Dictionary<string, object>();
                        ConvertJsonElementToDictionary(item, dict);
                        list.Add(dict);
                        break;
                    case JsonValueKind.Array:
                        list.Add(ConvertJsonElementToList(item));
                        break;
                    case JsonValueKind.String:
                        list.Add(item.GetString());
                        break;
                    case JsonValueKind.Number:
                        if (item.TryGetInt64(out long intValue))
                            list.Add(intValue);
                        else
                            list.Add(item.GetDouble());
                        break;
                    case JsonValueKind.True:
                        list.Add(true);
                        break;
                    case JsonValueKind.False:
                        list.Add(false);
                        break;
                    case JsonValueKind.Null:
                        list.Add(null);
                        break;
                    default:
                        list.Add(item.ToString());
                        break;
                }
            }
            return list;
        }

        private async Task CreateCollection(string collectionName, int dimension, string metric)
        {
            // Prepare collection creation request
            var requestData = new Dictionary<string, object>
            {
                ["name"] = collectionName,
                ["dimension"] = dimension,
                ["metric"] = metric
            };

            var json = JsonSerializer.Serialize(requestData, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/collections", content);
            response.EnsureSuccessStatusCode();
        }

        private async Task DeleteCollection(string collectionName)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/collections/{collectionName}");
            response.EnsureSuccessStatusCode();
        }

        private async Task UpsertVector(string collectionName, string id, float[] vector, Dictionary<string, object> metadata)
        {
            var requestData = new Dictionary<string, object>
            {
                ["id"] = id,
                ["vector"] = vector,
                ["metadata"] = metadata
            };

            var json = JsonSerializer.Serialize(requestData, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/collections/{collectionName}/vectors", content);
            response.EnsureSuccessStatusCode();
        }

        private async Task UpsertVectors(string collectionName, List<VectorData> vectors)
        {
            var vectorsList = vectors.Select(v => new Dictionary<string, object>
            {
                ["id"] = v.Id,
                ["vector"] = v.Vector,
                ["metadata"] = v.Metadata ?? new Dictionary<string, object>()
            }).ToList();

            var requestData = new Dictionary<string, object>
            {
                ["vectors"] = vectorsList
            };

            var json = JsonSerializer.Serialize(requestData, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/collections/{collectionName}/vectors/batch", content);
            response.EnsureSuccessStatusCode();
        }

        private async Task DeleteVectors(string collectionName, List<string> ids)
        {
            var requestData = new Dictionary<string, object>
            {
                ["ids"] = ids
            };

            var json = JsonSerializer.Serialize(requestData, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/collections/{collectionName}/vectors/delete", content);
            response.EnsureSuccessStatusCode();
        }

        private async Task<List<Dictionary<string, object>>> SearchVectors(string collectionName, float[] queryVector, int k = 10, Dictionary<string, object> filter = null)
        {
            var requestData = new Dictionary<string, object>
            {
                ["vector"] = queryVector,
                ["k"] = k
            };

            if (filter != null && filter.Count > 0)
            {
                requestData["filter"] = filter;
            }

            var json = JsonSerializer.Serialize(requestData, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/collections/{collectionName}/search", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var resultDocument = JsonDocument.Parse(responseContent);
            var results = new List<Dictionary<string, object>>();

            if (resultDocument.RootElement.TryGetProperty("matches", out var matchesElement) &&
                matchesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var match in matchesElement.EnumerateArray())
                {
                    var resultItem = new Dictionary<string, object>();
                    ConvertJsonElementToDictionary(match, resultItem);
                    results.Add(resultItem);
                }
            }

            return results;
        }
        #endregion "SharpVector API Helpers"

        #region "IDisposable Support"
        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _httpClient?.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion "IDisposable Support"
    }
}
