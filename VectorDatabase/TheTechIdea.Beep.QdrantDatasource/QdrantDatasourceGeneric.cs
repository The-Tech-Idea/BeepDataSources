﻿using System;
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
using TheTechIdea.Beep.WebAPI;

using Qdrant.Client;

namespace TheTechIdea.Beep.QdrantDatasource
{
    [AddinAttribute(Category = DatasourceCategory.VectorDB, DatasourceType = DataSourceType.Qdrant)]
    public class QdrantDatasourceGeneric : IDataSource, IInMemoryDB
    {
        private HttpClient _httpClient;
        private string _baseUrl;
        private JsonSerializerOptions _jsonOptions;


// Constructor with standard signature for IDataSource implementations
public QdrantDatasourceGeneric(string pdatasourcename, IDMLogger plogger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
        {
            DMEEditor = pDMEEditor;
            Logger = plogger;
            ErrorObject = per ?? DMEEditor.ErrorObject;
            DatasourceName = pdatasourcename;
            DatasourceType = DataSourceType.Qdrant;
            Category = DatasourceCategory.VectorDB;

        var client = new QdrantClient(
          host: "dee8c6d0-dac2-4556-be4c-39c8042b329d.eu-central-1-0.aws.cloud.qdrant.io",
          https: true,
          apiKey: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJhY2Nlc3MiOiJtIn0.CyOWUH0fCYDYNVVd99HH7u3oGOEFiNfTpM3yHEwSyWo"
        );

       // var collections = await client.ListCollectionsAsync();

        //foreach (var collection in collections)
        //{
        //    Console.WriteLine(collection);
        //}
        if (pdatasourcename != null)
            {
                // Initialize data connection with connection properties
                Dataconnection = new WebAPIDataConnection
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
        public DataSourceType DatasourceType { get; set; } = DataSourceType.Qdrant;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.VectorDB;
        public IDataConnection Dataconnection { get; set; }
        public string DatasourceName { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public string Id { get; set; }
        public IDMLogger Logger { get; set; }
        public List<string> EntitiesNames { get; set; } = new List<string>();
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
        public IDMEEditor DMEEditor { get; set; }
        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Closed;
        public bool IsCreated { get; set; }
        public bool IsLoaded { get; set; }
        public bool IsSaved { get; set; }
        public bool IsSynced { get; set; }
        public ETLScriptHDR CreateScript { get; set; }
        public bool IsStructureCreated { get; set; }
        public List<EntityStructure> InMemoryStructures { get; set; } = new List<EntityStructure>();
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

                // Initialize HTTP client for Qdrant API
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
                    _httpClient.DefaultRequestHeaders.Add("api-key", apiKey);
                }

                // Determine the base URL
                string host = Dataconnection.ConnectionProp.Host ?? "localhost";
                int port = Dataconnection.ConnectionProp.Port > 0 ? Dataconnection.ConnectionProp.Port : 6333;

                // Handle URL from connection props if provided
                if (!string.IsNullOrEmpty(Dataconnection.ConnectionProp.Url))
                {
                    _baseUrl = Dataconnection.ConnectionProp.Url.TrimEnd('/');
                }
                else
                {
                    // Construct URL from host and port
                    _baseUrl = $"http://{host}:{port}";
                }

                // Test connection by trying to list collections
                try
                {
                    var collections = ListCollections().Result;
                    ConnectionStatus = ConnectionState.Open;
                    DMEEditor.AddLogMessage("Success", $"Connected to Qdrant at {_baseUrl}", DateTime.Now, -1, "", Errors.Ok);
                }
                catch (Exception ex)
                {
                    DMEEditor.AddLogMessage("Error", $"Failed to connect to Qdrant: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                    ConnectionStatus = ConnectionState.Broken;
                }

                return ConnectionStatus;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", $"Error opening Qdrant connection: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                ConnectionStatus = ConnectionState.Broken;
                return ConnectionStatus;
            }
        }

        public ConnectionState Closeconnection()
        {
            _httpClient?.Dispose();
            _httpClient = null;
            ConnectionStatus = ConnectionState.Closed;
            DMEEditor.AddLogMessage("Success", "Closed Qdrant connection", DateTime.Now, -1, "", Errors.Ok);
            return ConnectionStatus;
        }

        public IErrorsInfo BeginTransaction(PassedArgs args)
        {
            // Qdrant doesn't support traditional transactions
            ErrorObject.Flag = Errors.Warning;
            ErrorObject.Message = "Transactions are not supported by Qdrant";
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
            // Qdrant doesn't support traditional transactions
            ErrorObject.Flag = Errors.Warning;
            ErrorObject.Message = "Transactions are not supported by Qdrant";
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
                        // Create a Qdrant collection for each entity
                        int dimension = 1536; // Default dimension
                        string distance = "Cosine"; // Default distance metric

                        // Try to get dimension from entity properties if available
                        var dimensionField = entity.Fields.FirstOrDefault(f => f.fieldname.Equals("dimension", StringComparison.OrdinalIgnoreCase));
                        if (dimensionField != null && int.TryParse(dimensionField.DefaultValue, out int dim))
                        {
                            dimension = dim;
                        }

                        // Try to get distance metric from entity properties if available
                        var metricField = entity.Fields.FirstOrDefault(f => f.fieldname.Equals("distance", StringComparison.OrdinalIgnoreCase));
                        if (metricField != null && !string.IsNullOrEmpty(metricField.DefaultValue))
                        {
                            distance = metricField.DefaultValue;
                        }

                        CreateCollection(entity.EntityName, dimension, distance).Wait();
                    }
                    catch (Exception ex)
                    {
                        DMEEditor.AddLogMessage("Error", $"Failed to create collection {entity.EntityName}: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                        ErrorObject.Flag = Errors.Failed;
                        ErrorObject.Message = $"Failed to create one or more collections";
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
                string distance = "Cosine"; // Default distance

                // Try to get dimension from entity properties if available
                var dimensionField = entity.Fields.FirstOrDefault(f => f.fieldname.Equals("dimension", StringComparison.OrdinalIgnoreCase));
                if (dimensionField != null && int.TryParse(dimensionField.DefaultValue, out int dim))
                {
                    dimension = dim;
                }

                // Try to get distance metric from entity properties if available
                var metricField = entity.Fields.FirstOrDefault(f => f.fieldname.Equals("distance", StringComparison.OrdinalIgnoreCase));
                if (metricField != null && !string.IsNullOrEmpty(metricField.DefaultValue))
                {
                    distance = metricField.DefaultValue;
                }

                CreateCollection(entity.EntityName, dimension, distance).Wait();
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
                // For Qdrant, this would be deleting points from a collection
                if (UploadDataRow is List<string> ids)
                {
                    DeletePoints(EntityName, ids).Wait();
                }
                else if (UploadDataRow is List<ulong> numericIds)
                {
                    DeletePoints(EntityName, numericIds.Select(id => id.ToString()).ToList()).Wait();
                }
                else
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = "Expected a List<string> or List<ulong> of point IDs";
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Error deleting points: {ex.Message}";
            }

            return ErrorObject;
        }

        public IErrorsInfo EndTransaction(PassedArgs args)
        {
            // Qdrant doesn't support traditional transactions
            ErrorObject.Flag = Errors.Warning;
            ErrorObject.Message = "Transactions are not supported by Qdrant";
            return ErrorObject;
        }

        public IErrorsInfo ExecuteSql(string sql)
        {
            ErrorObject.Flag = Errors.Warning;
            ErrorObject.Message = "SQL execution is not supported by Qdrant";
            return ErrorObject;
        }

        public IEnumerable<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            // Qdrant doesn't have traditional parent-child relationships
            return Array.Empty<ChildRelation>();
        }

        public IEnumerable<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            // Not applicable for Qdrant
            return Array.Empty<ETLScriptDet>();
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
                DMEEditor.AddLogMessage("Error", $"Error getting collection list: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                return Array.Empty<string>();
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

                // For Qdrant, we'll return information about the collection
                var collectionInfo = GetCollectionInfo(EntityName).Result;
                return collectionInfo != null ? new[] { collectionInfo } : Array.Empty<object>();
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", $"Error getting collection: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
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
                var pagedData = allData.Skip(pageNumber * pageSize).Take(pageSize).ToList();
                
                return new PagedResult
                {
                    Data = pagedData,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalRecords = allData.Count,
                    TotalPages = (int)Math.Ceiling((double)allData.Count / pageSize)
                };
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", $"Error getting collection with pagination: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                return new PagedResult { Data = Array.Empty<object>(), PageNumber = pageNumber, PageSize = pageSize, TotalRecords = 0, TotalPages = 0 };
            }
        }

        public Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            return Task.FromResult(GetEntity(EntityName, Filter));
        }

        public IEnumerable<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            // Qdrant doesn't have traditional foreign keys
            return Array.Empty<RelationShipKeys>();
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
                    // Get collection info
                    var collectionInfo = GetCollectionInfo(EntityName).Result;

                    entity = new EntityStructure
                    {
                        EntityName = EntityName,
                        DatasourceEntityName = EntityName,
                        DataSourceID = DatasourceName,
                        DatabaseType = DataSourceType.Qdrant,
                        Caption = EntityName,
                        KeyToken = Guid.NewGuid().ToString(),
                        Fields = new List<EntityField>()
                    };

                    // Add standard fields for Qdrant vectors
                    entity.Fields.Add(new EntityField
                    {
                        fieldname = "id",
                        fieldtype = "System.String",
                        IsKey = true,
                        Description = "Point ID"
                    });

                    entity.Fields.Add(new EntityField
                    {
                        fieldname = "vector",
                        fieldtype = "System.Single[]",
                        Description = "Vector values"
                    });

                    entity.Fields.Add(new EntityField
                    {
                        fieldname = "payload",
                        fieldtype = "System.Object",
                        Description = "Point payload/metadata"
                    });

                    // Get vector dimension from collection info
                    if (collectionInfo.TryGetValue("config", out var config) &&
                        config is JsonElement configElement &&
                        configElement.TryGetProperty("params", out var paramsElement) &&
                        paramsElement.TryGetProperty("vectors", out var vectorsElement) &&
                        vectorsElement.TryGetProperty("size", out var sizeElement))
                    {
                        var dimension = sizeElement.GetInt32();
                        entity.Fields.Add(new EntityField
                        {
                            fieldname = "dimension",
                            fieldtype = "System.Int32",
                            DefaultValue = dimension.ToString(),
                            Description = "Vector dimension"
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
                DMEEditor.AddLogMessage("Error", $"Error getting entity structure: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                return null;
            }
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            return GetEntityStructure(fnd.EntityName, refresh);
        }

        public Type GetEntityType(string EntityName)
        {
            // Not applicable for Qdrant
            return typeof(Dictionary<string, object>);
        }

        public double GetScalar(string query)
        {
            // Not applicable for Qdrant
            throw new NotImplementedException("Scalar queries are not supported by Qdrant");
        }

        public Task<double> GetScalarAsync(string query)
        {
            // Not applicable for Qdrant
            throw new NotImplementedException("Scalar queries are not supported by Qdrant");
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

                // For Qdrant, this would be upserting point(s)
                if (InsertedData is Dictionary<string, object> pointData)
                {
                    // Single point upsert
                    string id = pointData["id"]?.ToString() ?? Guid.NewGuid().ToString();
                    float[] vector = pointData["vector"] as float[] ?? new float[0];
                    var payload = pointData["payload"] as Dictionary<string, object> ?? new Dictionary<string, object>();

                    UpsertPoint(EntityName, id, vector, payload).Wait();
                }
                else if (InsertedData is List<Dictionary<string, object>> pointsData)
                {
                    // Batch upsert
                    var points = new List<QdrantPoint>();

                    foreach (var item in pointsData)
                    {
                        string id = item["id"]?.ToString() ?? Guid.NewGuid().ToString();
                        float[] vector = item["vector"] as float[] ?? new float[0];
                        var payload = item["payload"] as Dictionary<string, object> ?? new Dictionary<string, object>();

                        points.Add(new QdrantPoint { Id = id, Vector = vector, Payload = payload });
                    }

                    UpsertPoints(EntityName, points).Wait();
                }
                else
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = "Expected a Dictionary<string, object> or List<Dictionary<string, object>>";
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Error inserting points: {ex.Message}";
            }

            return ErrorObject;
        }

        public IEnumerable<object> RunQuery(string qrystr)
        {
            // Not applicable for traditional SQL queries
            throw new NotImplementedException("SQL queries are not supported by Qdrant");
        }

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            // Not applicable for Qdrant
            ErrorObject.Flag = Errors.Warning;
            ErrorObject.Message = "Scripts are not supported by Qdrant";
            return ErrorObject;
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            // For Qdrant, this is essentially the same as insert (upsert)
            return InsertEntity(EntityName, UploadData);
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            // For Qdrant, this is essentially the same as insert (upsert)
            return InsertEntity(EntityName, UploadDataRow);
        }
        #endregion "IDataSource METHODS"

        #region "IInMemoryDB METHODS"
        public IErrorsInfo OpenDatabaseInMemory(string databasename)
        {
            ErrorObject.Flag = Errors.Warning;
            ErrorObject.Message = "In-memory operation not supported by Qdrant";
            return ErrorObject;
        }

        public string GetConnectionString()
        {
            return $"Url={_baseUrl};ApiKey={Dataconnection.ConnectionProp.ApiKey}";
        }

        public IErrorsInfo SaveStructure()
        {
            ErrorObject.Flag = Errors.Warning;
            ErrorObject.Message = "Structure saving not supported by Qdrant";
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

                    progress?.Report(new PassedArgs { Messege = $"Loading structure for collection {collection}", EventType = "Loading" });
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
            ErrorObject.Message = "Structure creation not supported by Qdrant";
            return ErrorObject;
        }

        public IErrorsInfo LoadData(IProgress<PassedArgs> progress, CancellationToken token)
        {
            ErrorObject.Flag = Errors.Warning;
            ErrorObject.Message = "Data loading not supported by Qdrant";
            return ErrorObject;
        }

        public IErrorsInfo SyncData(IProgress<PassedArgs> progress, CancellationToken token)
        {
            ErrorObject.Flag = Errors.Warning;
            ErrorObject.Message = "Data syncing not supported by Qdrant";
            return ErrorObject;
        }

        public IErrorsInfo SyncData(string entityname, IProgress<PassedArgs> progress, CancellationToken token)
        {
            ErrorObject.Flag = Errors.Warning;
            ErrorObject.Message = "Data syncing not supported by Qdrant";
            return ErrorObject;
        }

        public IErrorsInfo RefreshData(IProgress<PassedArgs> progress, CancellationToken token)
        {
            ErrorObject.Flag = Errors.Warning;
            ErrorObject.Message = "Data refreshing not supported by Qdrant";
            return ErrorObject;
        }

        public IErrorsInfo RefreshData(string entityname, IProgress<PassedArgs> progress, CancellationToken token)
        {
            ErrorObject.Flag = Errors.Warning;
            ErrorObject.Message = "Data refreshing not supported by Qdrant";
            return ErrorObject;
        }
        #endregion "IInMemoryDB METHODS"



        #region "Qdrant API Helpers"
        private class QdrantPoint
        {
            public string Id { get; set; }
            public float[] Vector { get; set; }
            public Dictionary<string, object> Payload { get; set; }
        }

        private async Task<List<string>> ListCollections()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/collections");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);

            var collections = new List<string>();
            if (result.TryGetProperty("result", out var resultElement) &&
                resultElement.TryGetProperty("collections", out var collectionsElement) &&
                collectionsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var collection in collectionsElement.EnumerateArray())
                {
                    if (collection.TryGetProperty("name", out var nameElement))
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

            // Parse the JSON response
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;

            // Convert to dictionary
            var result = new Dictionary<string, object>();
            if (root.TryGetProperty("result", out var resultElement))
            {
                ConvertJsonElementToDictionary(resultElement, result);
            }

            return result;
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

        private async Task CreateCollection(string collectionName, int dimension, string distance)
        {
            // Convert distance string to Qdrant distance type
            string qdrantDistance = distance.ToLower() switch
            {
                "cosine" => "Cosine",
                "euclidean" => "Euclidean",
                "dot" => "Dot",
                _ => "Cosine" // Default to cosine
            };

            var requestData = new
            {
                vectors = new
                {
                    size = dimension,
                    distance = qdrantDistance
                }
            };

            var json = JsonSerializer.Serialize(requestData, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"{_baseUrl}/collections/{collectionName}", content);
            response.EnsureSuccessStatusCode();
        }

        private async Task DeleteCollection(string collectionName)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/collections/{collectionName}");
            response.EnsureSuccessStatusCode();
        }

        private async Task UpsertPoint(string collectionName, string id, float[] vector, Dictionary<string, object> payload)
        {
            var requestData = new
            {
                points = new[]
                {
                    new
                    {
                        id = id,
                        vector = vector,
                        payload = payload
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestData, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"{_baseUrl}/collections/{collectionName}/points", content);
            response.EnsureSuccessStatusCode();
        }

        private async Task UpsertPoints(string collectionName, List<QdrantPoint> points)
        {
            var pointsList = points.Select(p => new
            {
                id = p.Id,
                vector = p.Vector,
                payload = p.Payload
            }).ToList();

            var requestData = new
            {
                points = pointsList
            };

            var json = JsonSerializer.Serialize(requestData, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"{_baseUrl}/collections/{collectionName}/points", content);
            response.EnsureSuccessStatusCode();
        }

        private async Task DeletePoints(string collectionName, List<string> ids)
        {
            var requestData = new
            {
                points = ids,
            };

            var json = JsonSerializer.Serialize(requestData, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/collections/{collectionName}/points/delete", content);
            response.EnsureSuccessStatusCode();
        }

        private async Task<List<Dictionary<string, object>>> SearchVectors(string collectionName, float[] queryVector, int limit = 10, Dictionary<string, object> filter = null)
        {
            var requestData = new
            {
                vector = queryVector,
                limit = limit,
                filter = filter,
                with_vectors = true,
                with_payload = true
            };

            var json = JsonSerializer.Serialize(requestData, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/collections/{collectionName}/points/search", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();

            // Parse the JSON response
            using var document = JsonDocument.Parse(responseContent);
            var result = document.RootElement;

            var points = new List<Dictionary<string, object>>();

            if (result.TryGetProperty("result", out var resultElement) &&
                resultElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var point in resultElement.EnumerateArray())
                {
                    var pointDict = new Dictionary<string, object>();

                    if (point.TryGetProperty("id", out var idElement))
                        pointDict["id"] = idElement.GetString();

                    if (point.TryGetProperty("score", out var scoreElement))
                        pointDict["score"] = scoreElement.GetDouble();

                    if (point.TryGetProperty("vector", out var vectorElement) && vectorElement.ValueKind == JsonValueKind.Array)
                    {
                        var vector = new List<float>();
                        foreach (var val in vectorElement.EnumerateArray())
                        {
                            vector.Add(val.GetSingle());
                        }
                        pointDict["vector"] = vector.ToArray();
                    }

                    if (point.TryGetProperty("payload", out var payloadElement))
                    {
                        var payloadDict = new Dictionary<string, object>();
                        ConvertJsonElementToDictionary(payloadElement, payloadDict);
                        pointDict["payload"] = payloadDict;
                    }

                    points.Add(pointDict);
                }
            }

            return points;
        }
        #endregion "Qdrant API Helpers"

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
