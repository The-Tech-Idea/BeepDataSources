using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.ChromaDBDatasource
{
    /// <summary>
    /// ChromaDB vector database data source implementation
    /// ChromaDB is an open-source embedding database focused on simplicity and developer experience
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.VectorDB, DatasourceType = DataSourceType.ChromaDB)]
    public class ChromaDBDataSource : IDataSource
    {
        #region IDataSource Properties
        public string ColumnDelimiter { get; set; } = "\"";
        public string ParameterDelimiter { get; set; } = ":";
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public string Id { get; set; }
        public DataSourceType DatasourceType { get; set; } = DataSourceType.ChromaDB;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.VectorDB;
        public IDataConnection Dataconnection { get; set; }
        public string DatasourceName { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public IDMLogger Logger { get; set; }
        public List<string> EntitiesNames { get; set; } = new();
        public List<EntityStructure> Entities { get; set; } = new();
        public IDMEEditor DMEEditor { get; set; }
        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Closed;

        public event EventHandler<PassedArgs> PassEvent;
        #endregion

        #region Private Fields
        private readonly HttpClient _httpClient;
        private string _baseUrl;
        
        // ChromaDB REST API endpoints mapping
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // Collection management
            ["collections.list"] = "api/v1/collections",
            ["collections.create"] = "api/v1/collections",
            ["collections.get"] = "api/v1/collections/{collection_name}",
            ["collections.delete"] = "api/v1/collections/{collection_name}",
            ["collections.update"] = "api/v1/collections/{collection_id}",
            ["collections.count"] = "api/v1/collections/{collection_id}/count",
            
            // Embeddings operations
            ["embeddings.add"] = "api/v1/collections/{collection_id}/add",
            ["embeddings.query"] = "api/v1/collections/{collection_id}/query",
            ["embeddings.get"] = "api/v1/collections/{collection_id}/get",
            ["embeddings.update"] = "api/v1/collections/{collection_id}/update",
            ["embeddings.delete"] = "api/v1/collections/{collection_id}/delete",
            
            // System operations
            ["system.heartbeat"] = "api/v1/heartbeat",
            ["system.version"] = "api/v1/version"
        };

        private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            ["collections.get"] = new[] { "collection_name" },
            ["collections.delete"] = new[] { "collection_name" },
            ["collections.update"] = new[] { "collection_id", "new_name" },
            ["collections.count"] = new[] { "collection_id" },
            ["embeddings.add"] = new[] { "collection_id", "ids", "embeddings" },
            ["embeddings.query"] = new[] { "collection_id", "query_embeddings" },
            ["embeddings.get"] = new[] { "collection_id" },
            ["embeddings.update"] = new[] { "collection_id", "ids" },
            ["embeddings.delete"] = new[] { "collection_id", "ids" }
        };
        #endregion

        #region Constructor
        public ChromaDBDataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor dmeEditor,
            DataSourceType databasetype,
            IErrorsInfo errorObject)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            DMEEditor = dmeEditor;
            DatasourceType = DataSourceType.ChromaDB;
            Category = DatasourceCategory.VectorDB;
            ErrorObject = errorObject ?? dmeEditor?.ErrorObject;
            Id = Guid.NewGuid().ToString();
            
            _httpClient = new HttpClient();
            InitializeConnection();
            
            EntitiesNames = EntityEndpoints.Keys.ToList();
            Entities = EntitiesNames
                .Select(n => CreateChromaDBEntityStructure(n))
                .ToList();
        }

        private void InitializeConnection()
        {
            if (Dataconnection == null)
            {
                Dataconnection = new ChromaDBDataConnection(DMEEditor)
                {
                    DMEEditor = DMEEditor,
                    ConnectionProp = new ConnectionProperties
                    {
                        Host = "localhost",
                        Port = 8000, // Default ChromaDB port
                        DatabaseType = DataSourceType.ChromaDB,
                        ConnectionName = DatasourceName
                    }
                };
            }
            
            UpdateBaseUrl();
        }

        private void UpdateBaseUrl()
        {
            var props = Dataconnection?.ConnectionProp;
            if (props != null)
            {
                var scheme = props.UseSSL ? "https" : "http";
                _baseUrl = $"{scheme}://{props.Host}:{props.Port}/";
            }
        }
        #endregion

        #region IDataSource Core Methods
        
        public IEnumerable<string> GetEntitesList() => EntitiesNames;

        public IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            var data = GetEntityAsync(EntityName, filter).ConfigureAwait(false).GetAwaiter().GetResult();
            return data ?? Array.Empty<object>();
        }

        public async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            if (!EntityEndpoints.TryGetValue(EntityName, out var endpoint))
                throw new InvalidOperationException($"Unknown ChromaDB operation '{EntityName}'.");

            var queryParams = FiltersToChromaDBQuery(Filter);
            RequireFilters(EntityName, queryParams, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            return EntityName.Split('.')[0] switch
            {
                "collections" => await HandleCollectionOperation(EntityName, endpoint, queryParams),
                "embeddings" => await HandleEmbeddingsOperation(EntityName, endpoint, queryParams, Filter),
                "system" => await HandleSystemOperation(EntityName, endpoint),
                _ => Array.Empty<object>()
            };
        }

        public PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            ErrorObject.Flag = Errors.Ok;
            ErrorObject.Message = "Successfully retrieved data";
            try
            {
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
                return new PagedResult(Array.Empty<object>(), pageNumber, pageSize, 0);
            }
        }

        public IEnumerable<object> RunQuery(string qrystr)
        {
            try
            {
                Logger?.WriteLog($"Executing ChromaDB query: {qrystr}");
                return Array.Empty<object>();
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error executing query: {ex.Message}");
                return Array.Empty<object>();
            }
        }

        public ConnectionState Openconnection()
        {
            try
            {
                Logger?.WriteLog("Opening connection to ChromaDB");
                UpdateBaseUrl();
                ConnectionStatus = ConnectionState.Open;
                return ConnectionStatus;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error opening connection: {ex.Message}");
                ConnectionStatus = ConnectionState.Broken;
                return ConnectionStatus;
            }
        }

        public ConnectionState Closeconnection()
        {
            try
            {
                Logger?.WriteLog("Closing connection to ChromaDB");
                ConnectionStatus = ConnectionState.Closed;
                return ConnectionStatus;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error closing connection: {ex.Message}");
                return ConnectionStatus;
            }
        }

        #endregion

        #region IDataSource Entity Management

        public bool CheckEntityExist(string EntityName) => 
            EntitiesNames.Contains(EntityName, StringComparer.OrdinalIgnoreCase);

        public int GetEntityIdx(string entityName) => 
            EntitiesNames.FindIndex(e => e.Equals(entityName, StringComparison.OrdinalIgnoreCase));

        public Type GetEntityType(string EntityName) => typeof(Dictionary<string, object>);

        public EntityStructure GetEntityStructure(string EntityName, bool refresh = false) =>
            Entities.FirstOrDefault(e => e.EntityName.Equals(EntityName, StringComparison.OrdinalIgnoreCase));

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            if (fnd == null) return null;
            return GetEntityStructure(fnd.EntityName, refresh);
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            try
            {
                Logger?.WriteLog($"Creating ChromaDB collection: {entity.EntityName}");
                if (!EntitiesNames.Contains(entity.EntityName))
                {
                    EntitiesNames.Add(entity.EntityName);
                    Entities.Add(entity);
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error creating entity {entity.EntityName}: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region IDataSource Data Operations

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            try
            {
                Logger?.WriteLog($"Inserting into ChromaDB entity: {EntityName}");
                return ErrorObject;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error inserting: {ex.Message}");
                return ErrorObject;
            }
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            try
            {
                Logger?.WriteLog($"Updating ChromaDB entity: {EntityName}");
                return ErrorObject;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error updating: {ex.Message}");
                return ErrorObject;
            }
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            try
            {
                Logger?.WriteLog($"Bulk updating ChromaDB entity: {EntityName}");
                return ErrorObject;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error bulk updating: {ex.Message}");
                return ErrorObject;
            }
        }

        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            try
            {
                Logger?.WriteLog($"Deleting from ChromaDB entity: {EntityName}");
                return ErrorObject;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error deleting: {ex.Message}");
                return ErrorObject;
            }
        }

        #endregion

        #region IDataSource Script and Schema Operations

        public IErrorsInfo ExecuteSql(string sql)
        {
            try
            {
                Logger?.WriteLog($"Executing ChromaDB command: {sql}");
                return ErrorObject;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error executing: {ex.Message}");
                return ErrorObject;
            }
        }

        public IEnumerable<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            var scripts = new List<ETLScriptDet>();
            foreach (var entity in entities ?? Entities)
            {
                scripts.Add(new ETLScriptDet
                {
                    Ddl = $"-- Create ChromaDB collection: {entity.EntityName}",
                   ScriptType= DDLScriptType.CreateEntity
                });
            }
            return scripts;
        }

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            try
            {
                Logger?.WriteLog($"Running ChromaDB script");
                return ErrorObject;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error running script: {ex.Message}");
                return ErrorObject;
            }
        }

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            try
            {
                foreach (var entity in entities ?? new List<EntityStructure>())
                {
                    CreateEntityAs(entity);
                }
                return ErrorObject;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error creating entities: {ex.Message}");
                return ErrorObject;
            }
        }

        #endregion

        #region IDataSource Relationships and Metadata

        public IEnumerable<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters) =>
            new List<ChildRelation>();

        public IEnumerable<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName) =>
            new List<RelationShipKeys>();

        #endregion

        #region IDataSource Transaction Support

        public IErrorsInfo BeginTransaction(PassedArgs args)
        {
            try
            {
                Logger?.WriteLog("Beginning ChromaDB transaction");
                return ErrorObject;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error beginning transaction: {ex.Message}");
                return ErrorObject;
            }
        }

        public IErrorsInfo EndTransaction(PassedArgs args)
        {
            try
            {
                Logger?.WriteLog("Ending ChromaDB transaction");
                return ErrorObject;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error ending transaction: {ex.Message}");
                return ErrorObject;
            }
        }

        public IErrorsInfo Commit(PassedArgs args)
        {
            try
            {
                Logger?.WriteLog("Committing ChromaDB transaction");
                return ErrorObject;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error committing transaction: {ex.Message}");
                return ErrorObject;
            }
        }

        #endregion

        #region IDataSource Async Operations

        public async Task<double> GetScalarAsync(string query)
        {
            await Task.CompletedTask;
            return 0.0;
        }

        public double GetScalar(string query) => 
            GetScalarAsync(query).ConfigureAwait(false).GetAwaiter().GetResult();

        #endregion

        #region ChromaDB-specific Operations

        private async Task<IEnumerable<object>> HandleCollectionOperation(string operation, string endpoint, Dictionary<string, string> queryParams)
        {
            try
            {
                switch (operation)
                {
                    case "collections.list":
                        return await ListCollectionsAsync();
                    case "collections.get":
                        return await GetCollectionAsync(queryParams["collection_name"]);
                    case "collections.count":
                        return await GetCollectionCountAsync(queryParams["collection_id"]);
                    default:
                        return Array.Empty<object>();
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error in collection operation {operation}: {ex.Message}");
                return Array.Empty<object>();
            }
        }

        private async Task<IEnumerable<object>> HandleEmbeddingsOperation(string operation, string endpoint, Dictionary<string, string> queryParams, List<AppFilter> filters)
        {
            try
            {
                switch (operation)
                {
                    case "embeddings.query":
                        return await QueryEmbeddingsAsync(queryParams["collection_id"], filters);
                    case "embeddings.get":
                        return await GetEmbeddingsAsync(queryParams["collection_id"], filters);
                    default:
                        return Array.Empty<object>();
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error in embeddings operation {operation}: {ex.Message}");
                return Array.Empty<object>();
            }
        }

        private async Task<IEnumerable<object>> HandleSystemOperation(string operation, string endpoint)
        {
            try
            {
                switch (operation)
                {
                    case "system.heartbeat":
                        return await GetHeartbeatAsync();
                    case "system.version":
                        return await GetVersionAsync();
                    default:
                        return Array.Empty<object>();
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error in system operation {operation}: {ex.Message}");
                return Array.Empty<object>();
            }
        }

        private async Task<IEnumerable<object>> ListCollectionsAsync()
        {
            var response = await GetJsonAsync("api/v1/collections");
            if (response != null && response is JsonElement element && element.ValueKind == JsonValueKind.Array)
            {
                return element.EnumerateArray()
                    .Select(c => (object)JsonSerializer.Deserialize<Dictionary<string, object>>(c.GetRawText()))
                    .ToList();
            }
            return Array.Empty<object>();
        }

        private async Task<IEnumerable<object>> GetCollectionAsync(string collectionName)
        {
            var response = await GetJsonAsync($"api/v1/collections/{collectionName}");
            return response != null ? new[] { response } : Array.Empty<object>();
        }

        private async Task<IEnumerable<object>> GetCollectionCountAsync(string collectionId)
        {
            var response = await GetJsonAsync($"api/v1/collections/{collectionId}/count");
            return response != null ? new[] { response } : Array.Empty<object>();
        }

        private async Task<IEnumerable<object>> QueryEmbeddingsAsync(string collectionId, List<AppFilter> filters)
        {
            var embeddingFilter = filters?.FirstOrDefault(f => f.FieldName.Equals("query_embeddings", StringComparison.OrdinalIgnoreCase));
            if (embeddingFilter?.FilterValue is not string embeddingStr || !TryParseFloatArray(embeddingStr, out float[] queryEmbedding))
            {
                throw new ArgumentException("Query requires 'query_embeddings' filter with float array value");
            }

            var nResultsFilter = filters?.FirstOrDefault(f => f.FieldName.Equals("n_results", StringComparison.OrdinalIgnoreCase));
            var nResults = 10;
            if (nResultsFilter != null && int.TryParse(nResultsFilter.FilterValue?.ToString(), out int n))
            {
                nResults = n;
            }

            var request = new
            {
                query_embeddings = new[] { queryEmbedding },
                n_results = nResults
            };

            var response = await PostJsonAsync($"api/v1/collections/{collectionId}/query", request);
            if (response != null && response.TryGetValue("results", out var results))
            {
                if (results is JsonElement element && element.ValueKind == JsonValueKind.Array)
                {
                    return element.EnumerateArray()
                        .Select(item => (object)JsonSerializer.Deserialize<Dictionary<string, object>>(item.GetRawText()))
                        .ToList();
                }
            }
            return Array.Empty<object>();
        }

        private async Task<IEnumerable<object>> GetEmbeddingsAsync(string collectionId, List<AppFilter> filters)
        {
            var limitFilter = filters?.FirstOrDefault(f => f.FieldName.Equals("limit", StringComparison.OrdinalIgnoreCase));
            var limit = 10;
            if (limitFilter != null && int.TryParse(limitFilter.FilterValue?.ToString(), out int l))
            {
                limit = l;
            }

            var request = new
            {
                limit = limit
            };

            var response = await PostJsonAsync($"api/v1/collections/{collectionId}/get", request);
            if (response != null && response.TryGetValue("embeddings", out var embeddings))
            {
                if (embeddings is JsonElement element && element.ValueKind == JsonValueKind.Array)
                {
                    return element.EnumerateArray()
                        .Select(item => (object)JsonSerializer.Deserialize<Dictionary<string, object>>(item.GetRawText()))
                        .ToList();
                }
            }
            return Array.Empty<object>();
        }

        private async Task<IEnumerable<object>> GetHeartbeatAsync()
        {
            var response = await GetJsonAsync("api/v1/heartbeat");
            return response != null ? new[] { response } : Array.Empty<object>();
        }

        private async Task<IEnumerable<object>> GetVersionAsync()
        {
            var response = await GetJsonAsync("api/v1/version");
            return response != null ? new[] { response } : Array.Empty<object>();
        }

        #endregion

        #region Helper Methods

        private Dictionary<string, string> FiltersToChromaDBQuery(List<AppFilter> filters)
        {
            var queryParams = new Dictionary<string, string>();
            foreach (var filter in filters ?? new List<AppFilter>())
            {
                if (!string.IsNullOrEmpty(filter.FieldName) && filter.FilterValue != null)
                {
                    queryParams[filter.FieldName] = filter.FilterValue.ToString();
                }
            }
            return queryParams;
        }

        private void RequireFilters(string operation, Dictionary<string, string> queryParams, string[] requiredFields)
        {
            foreach (var field in requiredFields)
            {
                if (!queryParams.ContainsKey(field))
                {
                    throw new InvalidOperationException($"ChromaDB operation '{operation}' requires filter '{field}'");
                }
            }
        }

        private bool TryParseFloatArray(string input, out float[] result)
        {
            result = null;
            try
            {
                if (input.StartsWith("[") && input.EndsWith("]"))
                {
                    result = JsonSerializer.Deserialize<float[]>(input);
                    return true;
                }
                
                var parts = input.Split(',');
                result = parts.Select(p => float.Parse(p.Trim())).ToArray();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private EntityStructure CreateChromaDBEntityStructure(string operationName)
        {
            var entity = new EntityStructure
            {
                EntityName = operationName,
                DatasourceEntityName = operationName,
                DataSourceID = DatasourceName,
                DatabaseType = DataSourceType.ChromaDB,
                Caption = operationName.Replace(".", " ").Replace("_", " "),
                KeyToken = Guid.NewGuid().ToString(),
                Fields = new List<EntityField>()
            };

            var operationType = operationName.Split('.')[0];
            switch (operationType)
            {
                case "collections":
                    entity.Fields.AddRange(new[]
                    {
                        new EntityField { FieldName = "id", Fieldtype = "System.String", IsKey = true },
                        new EntityField { FieldName = "name", Fieldtype = "System.String" },
                        new EntityField { FieldName = "metadata", Fieldtype = "System.Object" }
                    });
                    break;
                case "embeddings":
                    entity.Fields.AddRange(new[]
                    {
                        new EntityField { FieldName = "id", Fieldtype = "System.String", IsKey = true },
                        new EntityField { FieldName = "embedding", Fieldtype = "System.Single[]" },
                        new EntityField { FieldName = "document", Fieldtype = "System.String" },
                        new EntityField { FieldName = "metadata", Fieldtype = "System.Object" },
                        new EntityField { FieldName = "distance", Fieldtype = "System.Single" }
                    });
                    break;
                case "system":
                    entity.Fields.AddRange(new[]
                    {
                        new EntityField { FieldName = "status", Fieldtype = "System.String" },
                        new EntityField { FieldName = "version", Fieldtype = "System.String" },
                        new EntityField { FieldName = "time", Fieldtype = "System.DateTime" }
                    });
                    break;
            }

            return entity;
        }

        private async Task<object> GetJsonAsync(string endpoint)
        {
            try
            {
                var url = $"{_baseUrl}{endpoint}";
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<object>(content);
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                Logger?.WriteLog($"Error in GetJsonAsync to {endpoint}: {ex.Message}");
            }
            return new Dictionary<string, object>();
        }

        private async Task<Dictionary<string, object>> PostJsonAsync(string endpoint, object requestData)
        {
            try
            {
                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var url = $"{_baseUrl}{endpoint}";
                
                var response = await _httpClient.PostAsync(url, content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                Logger?.WriteLog($"Error in PostJsonAsync to {endpoint}: {ex.Message}");
            }
            return new Dictionary<string, object>();
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            try
            {
                Closeconnection();
                _httpClient?.Dispose();
                Logger?.WriteLog("ChromaDB datasource disposed");
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error disposing datasource: {ex.Message}");
            }
        }

        #endregion
    }
}