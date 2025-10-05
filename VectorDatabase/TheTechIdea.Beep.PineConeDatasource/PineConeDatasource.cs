using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;

namespace TheTechIdea.Beep.PineConeDatasource
{
    //[AddinAttribute(Category = DatasourceCategory.VectorDB, DatasourceType = DataSourceType.PineCone)]
    public class PineConeDatasource : IDataSource, IInMemoryDB
    {
        private HttpClient _httpClient;
        private string _baseUrl;
        private JsonSerializerOptions _jsonOptions;

        // All IDatasource Constructors should be public and with same signature 
        public PineConeDatasource(string pdatasourcename, IDMLogger plogger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
        {
            DMEEditor = pDMEEditor;
            Logger = plogger;
            ErrorObject = per ?? DMEEditor.ErrorObject;
            DatasourceName = pdatasourcename;
            DatasourceType = DataSourceType.PineCone;
            Category = DatasourceCategory.VectorDB;

            if (pdatasourcename != null)
            {
                Dataconnection = new PineconeDataConnection(DMEEditor)
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
        public DataSourceType DatasourceType { get; set; } = DataSourceType.PineCone;
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

                // Initialize HTTP client for Pinecone API
                _httpClient = new HttpClient();

                // According to Pinecone API documentation:
                // - ApiKey should be passed in the "Api-Key" header
                // - Environment URL is needed for API access

                // Use ApiKey from connection properties
                string apiKey = Dataconnection.ConnectionProp.ApiKey;
                if (string.IsNullOrEmpty(apiKey))
                {
                    // Fall back to Password if ApiKey is not set
                    apiKey = Dataconnection.ConnectionProp.Password;
                }

                if (string.IsNullOrEmpty(apiKey))
                {
                    DMEEditor.AddLogMessage("Error", "Pinecone API key is missing", DateTime.Now, -1, "", Errors.Failed);
                    ConnectionStatus = ConnectionState.Broken;
                    return ConnectionStatus;
                }

                // Set default headers for all requests
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _httpClient.DefaultRequestHeaders.Add("Api-Key", apiKey);

                // Determine the base URL
                // If Host is not provided, use the default Pinecone API endpoint
                string environment = Dataconnection.ConnectionProp.Host ?? "api.pinecone.io";
                _baseUrl = $"https://{environment}";

                // Test connection by trying to list indexes
                try
                {
                    var indexes = ListIndexes().Result;
                    ConnectionStatus = ConnectionState.Open;
                    DMEEditor.AddLogMessage("Success", "Connected to Pinecone", DateTime.Now, -1, "", Errors.Ok);
                }
                catch (Exception ex)
                {
                    DMEEditor.AddLogMessage("Error", $"Failed to connect to Pinecone: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                    ConnectionStatus = ConnectionState.Broken;
                }

                return ConnectionStatus;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", $"Error opening Pinecone connection: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                ConnectionStatus = ConnectionState.Broken;
                return ConnectionStatus;
            }
        }

        public ConnectionState Closeconnection()
        {
            _httpClient?.Dispose();
            _httpClient = null;
            ConnectionStatus = ConnectionState.Closed;
            DMEEditor.AddLogMessage("Success", "Closed Pinecone connection", DateTime.Now, -1, "", Errors.Ok);
            return ConnectionStatus;
        }

        public IErrorsInfo BeginTransaction(PassedArgs args)
        {
            // Pinecone doesn't support traditional transactions
            ErrorObject.Flag =Errors.Warning;
            ErrorObject.Message = "Transactions are not supported by Pinecone";
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

                var indexes = ListIndexes().Result;
                return indexes.Contains(EntityName);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", $"Error checking if index exists: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                return false;
            }
        }

        public IErrorsInfo Commit(PassedArgs args)
        {
            // Pinecone doesn't support traditional transactions
            ErrorObject.Flag =Errors.Warning;
            ErrorObject.Message = "Transactions are not supported by Pinecone";
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
                        // Create a Pinecone index for each entity
                        CreateIndex(entity.EntityName, 1536, "cosine").Wait(); // Default dimension and metric
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

                CreateIndex(entity.EntityName, dimension, metric).Wait();
                return true;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", $"Failed to create index {entity.EntityName}: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                return false;
            }
        }

        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            ErrorObject.Flag = Errors.Ok;

            try
            {
                // For Pinecone, this would be deleting vectors from an index
                if (UploadDataRow is List<string> ids)
                {
                    DeleteVectors(EntityName, ids).Wait();
                }
                else
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = "Expected a List<string> of vector IDs";
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
            // Pinecone doesn't support traditional transactions
            ErrorObject.Flag =Errors.Warning;
            ErrorObject.Message = "Transactions are not supported by Pinecone";
            return ErrorObject;
        }

        public IErrorsInfo ExecuteSql(string sql)
        {
            ErrorObject.Flag =Errors.Warning;
            ErrorObject.Message = "SQL execution is not supported by Pinecone";
            return ErrorObject;
        }

        public IEnumerable<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            // Pinecone doesn't have traditional parent-child relationships
            return new List<ChildRelation>();
        }

        public IEnumerable<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            // Not applicable for Pinecone
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

                EntitiesNames = ListIndexes().Result;
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

                // For Pinecone, we'll return information about the index
                var indexStats = GetIndexStats(EntityName).Result;
                return indexStats != null ? new[] { indexStats } : Array.Empty<object>();
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", $"Error getting index: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                return Array.Empty<object>();
            }
        }

        public PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
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

        public Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            return Task.FromResult(GetEntity(EntityName, Filter));
        }

        public IEnumerable<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            // Pinecone doesn't have foreign keys
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
                    // Get index description
                    var indexDescription = DescribeIndex(EntityName).Result;

                    entity = new EntityStructure
                    {
                        EntityName = EntityName,
                        DataSourceID = DatasourceName,
                        DatabaseType = DataSourceType.PineCone,
                        Caption = EntityName,
                        KeyToken = Guid.NewGuid().ToString(),
                        Fields = new List<EntityField>()
                    };

                    // Add standard fields for Pinecone vector
                    entity.Fields.Add(new EntityField
                    {
                        fieldname = "id",
                        fieldtype = "System.String",
                        IsKey = true
                    });

                    entity.Fields.Add(new EntityField
                    {
                        fieldname = "values",
                        fieldtype = "System.Single[]",
                        Description = "Vector values"
                    });

                    entity.Fields.Add(new EntityField
                    {
                        fieldname = "metadata",
                        fieldtype = "System.Object",
                        Description = "Vector metadata"
                    });

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
            // Not applicable for Pinecone
            return typeof(object);
        }

        public double GetScalar(string query)
        {
            // Not applicable for Pinecone
            throw new NotImplementedException("Scalar queries are not supported by Pinecone");
        }

        public Task<double> GetScalarAsync(string query)
        {
            // Not applicable for Pinecone
            throw new NotImplementedException("Scalar queries are not supported by Pinecone");
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

                // For Pinecone, this would be upserting vector(s)
                if (InsertedData is Dictionary<string, object> vectorData)
                {
                    // Single vector upsert
                    var id = vectorData["id"]?.ToString();
                    var values = vectorData["values"] as IEnumerable<float>;
                    var metadata = vectorData["metadata"] as Dictionary<string, object>;

                    if (id != null && values != null)
                    {
                        UpsertVector(EntityName, id, values.ToArray(), metadata).Wait();
                    }
                    else
                    {
                        ErrorObject.Flag = Errors.Failed;
                        ErrorObject.Message = "Invalid vector data format";
                    }
                }
                else if (InsertedData is List<Dictionary<string, object>> vectorsData)
                {
                    // Batch upsert
                    var vectors = new List<(string id, float[] values, Dictionary<string, object> metadata)>();

                    foreach (var item in vectorsData)
                    {
                        var id = item["id"]?.ToString();
                        var values = item["values"] as IEnumerable<float>;
                        var metadata = item["metadata"] as Dictionary<string, object>;

                        if (id != null && values != null)
                        {
                            vectors.Add((id, values.ToArray(), metadata));
                        }
                    }

                    UpsertVectors(EntityName, vectors).Wait();
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
                ErrorObject.Message = $"Error inserting vectors: {ex.Message}";
            }

            return ErrorObject;
        }

        public IEnumerable<object> RunQuery(string qrystr)
        {
            // Not applicable for traditional SQL queries
            throw new NotImplementedException("SQL queries are not supported by Pinecone");
        }

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            // Not applicable for Pinecone
            ErrorObject.Flag =Errors.Warning;
            ErrorObject.Message = "Scripts are not supported by Pinecone";
            return ErrorObject;
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            // For Pinecone, this is the same as insert (upsert)
            return InsertEntity(EntityName, UploadData);
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            // For Pinecone, this is the same as insert (upsert)
            return InsertEntity(EntityName, UploadDataRow);
        }
        #endregion "IDataSource METHODS"

        #region "IInMemoryDB METHODS"
        public IErrorsInfo OpenDatabaseInMemory(string databasename)
        {
            ErrorObject.Flag =Errors.Warning;
            ErrorObject.Message = "In-memory operation not supported by Pinecone";
            return ErrorObject;
        }

        public string GetConnectionString()
        {
            return $"ApiKey={Dataconnection.ConnectionProp.ApiKey};Environment={Dataconnection.ConnectionProp.Host}";
        }

        public IErrorsInfo SaveStructure()
        {
            ErrorObject.Flag =Errors.Warning;
            ErrorObject.Message = "Structure saving not supported by Pinecone";
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

                var indexes = ListIndexes().Result;
                EntitiesNames = indexes;

                foreach (var index in indexes)
                {
                    if (token.IsCancellationRequested)
                        break;

                    progress?.Report(new PassedArgs { Messege = $"Loading structure for index {index}", EventType = "Loading" });
                    GetEntityStructure(index, true);
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
            ErrorObject.Message = "Structure creation not supported by Pinecone";
            return ErrorObject;
        }

        public IErrorsInfo LoadData(IProgress<PassedArgs> progress, CancellationToken token)
        {
            ErrorObject.Flag =Errors.Warning;
            ErrorObject.Message = "Data loading not supported by Pinecone";
            return ErrorObject;
        }

        public IErrorsInfo SyncData(IProgress<PassedArgs> progress, CancellationToken token)
        {
            ErrorObject.Flag =Errors.Warning;
            ErrorObject.Message = "Data syncing not supported by Pinecone";
            return ErrorObject;
        }

        public IErrorsInfo SyncData(string entityname, IProgress<PassedArgs> progress, CancellationToken token)
        {
            ErrorObject.Flag =Errors.Warning;
            ErrorObject.Message = "Data syncing not supported by Pinecone";
            return ErrorObject;
        }

        public IErrorsInfo RefreshData(IProgress<PassedArgs> progress, CancellationToken token)
        {
            ErrorObject.Flag =Errors.Warning;
            ErrorObject.Message = "Data refreshing not supported by Pinecone";
            return ErrorObject;
        }

        public IErrorsInfo RefreshData(string entityname, IProgress<PassedArgs> progress, CancellationToken token)
        {
            ErrorObject.Flag =Errors.Warning;
            ErrorObject.Message = "Data refreshing not supported by Pinecone";
            return ErrorObject;
        }
        #endregion "IInMemoryDB METHODS"

        #region "Pinecone API Helpers"
        private async Task<List<string>> ListIndexes()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/indexes");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content, _jsonOptions);

            var indexes = new List<string>();
            if (result.TryGetValue("indexes", out var indexesElement) && indexesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var index in indexesElement.EnumerateArray())
                {
                    if (index.TryGetProperty("name", out var nameElement))
                    {
                        indexes.Add(nameElement.GetString());
                    }
                }
            }

            return indexes;
        }

        private async Task<Dictionary<string, object>> DescribeIndex(string indexName)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/describe_index/{indexName}");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(content, _jsonOptions);

            return result;
        }

        private async Task<Dictionary<string, object>> GetIndexStats(string indexName)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/describe_index_stats/{indexName}");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(content, _jsonOptions);

            return result;
        }

        private async Task CreateIndex(string indexName, int dimension, string metric)
        {
            var requestData = new
            {
                name = indexName,
                dimension = dimension,
                metric = metric
            };

            var json = JsonSerializer.Serialize(requestData, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/indexes", content);
            response.EnsureSuccessStatusCode();
        }

        private async Task DeleteIndex(string indexName)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/indexes/{indexName}");
            response.EnsureSuccessStatusCode();
        }

        private async Task UpsertVector(string indexName, string id, float[] values, Dictionary<string, object> metadata = null)
        {
            var requestData = new
            {
                vectors = new[]
                {
                    new
                    {
                        id = id,
                        values = values,
                        metadata = metadata
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestData, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/vectors/upsert/{indexName}", content);
            response.EnsureSuccessStatusCode();
        }

        private async Task UpsertVectors(string indexName, List<(string id, float[] values, Dictionary<string, object> metadata)> vectors)
        {
            var vectorsList = vectors.Select(v => new
            {
                id = v.id,
                values = v.values,
                metadata = v.metadata
            }).ToList();

            var requestData = new
            {
                vectors = vectorsList
            };

            var json = JsonSerializer.Serialize(requestData, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/vectors/upsert/{indexName}", content);
            response.EnsureSuccessStatusCode();
        }

        private async Task DeleteVectors(string indexName, List<string> ids)
        {
            var requestData = new
            {
                ids = ids
            };

            var json = JsonSerializer.Serialize(requestData, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/vectors/delete/{indexName}", content);
            response.EnsureSuccessStatusCode();
        }

        private async Task<List<Dictionary<string, object>>> QueryVectors(string indexName, float[] queryVector, int topK = 10, Dictionary<string, object> filter = null)
        {
            var requestData = new
            {
                vector = queryVector,
                topK = topK,
                filter = filter,
                includeValues = true,
                includeMetadata = true
            };

            var json = JsonSerializer.Serialize(requestData, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/query/{indexName}", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(responseContent, _jsonOptions);

            var matches = new List<Dictionary<string, object>>();
            if (result.TryGetValue("matches", out var matchesElement) && matchesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var match in matchesElement.EnumerateArray())
                {
                    var matchDict = new Dictionary<string, object>();

                    if (match.TryGetProperty("id", out var idElement))
                        matchDict["id"] = idElement.GetString();

                    if (match.TryGetProperty("score", out var scoreElement))
                        matchDict["score"] = scoreElement.GetDouble();

                    if (match.TryGetProperty("values", out var valuesElement) && valuesElement.ValueKind == JsonValueKind.Array)
                    {
                        var values = new List<float>();
                        foreach (var val in valuesElement.EnumerateArray())
                        {
                            values.Add(val.GetSingle());
                        }
                        matchDict["values"] = values.ToArray();
                    }

                    if (match.TryGetProperty("metadata", out var metadataElement))
                    {
                        matchDict["metadata"] = JsonSerializer.Deserialize<Dictionary<string, object>>(
                            metadataElement.GetRawText(), _jsonOptions);
                    }

                    matches.Add(matchDict);
                }
            }

            return matches;
        }
        #endregion "Pinecone API Helpers"

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
