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
using TheTechIdea.Beep.WebAPI;

namespace TheTechIdea.Beep.QdrantDatasource
{
    /// <summary>
    /// Qdrant vector database data source implementation
    /// Qdrant is a vector similarity search engine with extended filtering support
    /// </summary>
    //[AddinAttribute(Category = DatasourceCategory.VectorDB, DatasourceType = DataSourceType.Qdrant)]
    public class QdrantDataSource : IDataSource
    {
        #region IDataSource Properties
        public string ColumnDelimiter { get; set; } = "\"";
        public string ParameterDelimiter { get; set; } = ":";
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public string Id { get; set; }
        public DataSourceType DatasourceType { get; set; } = DataSourceType.Qdrant;
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
        
        // Qdrant REST API endpoints mapping
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // Collection management
            ["collections.list"] = "collections",
            ["collections.create"] = "collections/{collection_name}",
            ["collections.get"] = "collections/{collection_name}",
            ["collections.delete"] = "collections/{collection_name}",
            ["collections.update"] = "collections/{collection_name}",
            
            // Points (vectors) operations
            ["points.upsert"] = "collections/{collection_name}/points",
            ["points.search"] = "collections/{collection_name}/points/search",
            ["points.scroll"] = "collections/{collection_name}/points/scroll",
            ["points.get"] = "collections/{collection_name}/points/{id}",
            ["points.delete"] = "collections/{collection_name}/points/delete",
            ["points.recommend"] = "collections/{collection_name}/points/recommend",
            
            // Snapshots
            ["snapshots.create"] = "collections/{collection_name}/snapshots",
            ["snapshots.list"] = "collections/{collection_name}/snapshots"
        };

        private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            ["collections.get"] = new[] { "collection_name" },
            ["collections.delete"] = new[] { "collection_name" },
            ["collections.update"] = new[] { "collection_name" },
            ["points.search"] = new[] { "collection_name", "vector" },
            ["points.scroll"] = new[] { "collection_name" },
            ["points.get"] = new[] { "collection_name", "id" },
            ["points.delete"] = new[] { "collection_name" },
            ["points.recommend"] = new[] { "collection_name", "positive" }
        };
        #endregion

        #region Constructor
        public QdrantDataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor dmeEditor,
            DataSourceType databasetype,
            IErrorsInfo errorObject)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            DMEEditor = dmeEditor;
            DatasourceType = DataSourceType.Qdrant;
            Category = DatasourceCategory.VectorDB;
            ErrorObject = errorObject ?? dmeEditor?.ErrorObject;
            Id = Guid.NewGuid().ToString();
            
            _httpClient = new HttpClient();
            InitializeConnection();
            
            EntitiesNames = EntityEndpoints.Keys.ToList();
            Entities = EntitiesNames
                .Select(n => CreateQdrantEntityStructure(n))
                .ToList();
        }

        private void InitializeConnection()
        {
            if (Dataconnection == null)
            {
                Dataconnection = new WebAPIDataConnection
                {
                    DMEEditor = DMEEditor,
                    ConnectionProp = new ConnectionProperties
                    {
                        Host = "localhost",
                        Port = 6333, // Default Qdrant port
                        DatabaseType = DataSourceType.Qdrant,
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
                throw new InvalidOperationException($"Unknown Qdrant operation '{EntityName}'.");

            var queryParams = FiltersToQdrantQuery(Filter);
            RequireFilters(EntityName, queryParams, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            return EntityName.Split('.')[0] switch
            {
                "collections" => await HandleCollectionOperation(EntityName, endpoint, queryParams),
                "points" => await HandlePointsOperation(EntityName, endpoint, queryParams, Filter),
                "snapshots" => await HandleSnapshotsOperation(EntityName, endpoint, queryParams),
                _ => Array.Empty<object>()
            };
        }

        public PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            var allData = GetEntity(EntityName, filter).ToList();
            var pagedData = allData.Skip(pageNumber * pageSize).Take(pageSize).ToList();
            
            return new PagedResult
            {
                Data = pagedData,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public IEnumerable<object> RunQuery(string qrystr)
        {
            try
            {
                Logger?.WriteLog($"Executing Qdrant query: {qrystr}");
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
                Logger?.WriteLog("Opening connection to Qdrant");
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
                Logger?.WriteLog("Closing connection to Qdrant");
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
                Logger?.WriteLog($"Creating Qdrant collection: {entity.EntityName}");
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
                Logger?.WriteLog($"Inserting into Qdrant entity: {EntityName}");
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
                Logger?.WriteLog($"Updating Qdrant entity: {EntityName}");
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
                Logger?.WriteLog($"Bulk updating Qdrant entity: {EntityName}");
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
                Logger?.WriteLog($"Deleting from Qdrant entity: {EntityName}");
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
                Logger?.WriteLog($"Executing Qdrant command: {sql}");
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
                    ddl = $"-- Create Qdrant collection: {entity.EntityName}",
                    scriptType = DDLScriptType.CreateEntity
                });
            }
            return scripts;
        }

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            try
            {
                Logger?.WriteLog($"Running Qdrant script");
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
                Logger?.WriteLog("Beginning Qdrant transaction");
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
                Logger?.WriteLog("Ending Qdrant transaction");
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
                Logger?.WriteLog("Committing Qdrant transaction");
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

        #region Qdrant-specific Operations

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

        private async Task<IEnumerable<object>> HandlePointsOperation(string operation, string endpoint, Dictionary<string, string> queryParams, List<AppFilter> filters)
        {
            try
            {
                switch (operation)
                {
                    case "points.search":
                        return await SearchPointsAsync(queryParams["collection_name"], filters);
                    case "points.scroll":
                        return await ScrollPointsAsync(queryParams["collection_name"], filters);
                    case "points.recommend":
                        return await RecommendPointsAsync(queryParams["collection_name"], filters);
                    default:
                        return Array.Empty<object>();
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error in points operation {operation}: {ex.Message}");
                return Array.Empty<object>();
            }
        }

        private async Task<IEnumerable<object>> HandleSnapshotsOperation(string operation, string endpoint, Dictionary<string, string> queryParams)
        {
            try
            {
                switch (operation)
                {
                    case "snapshots.list":
                        return await ListSnapshotsAsync(queryParams["collection_name"]);
                    default:
                        return Array.Empty<object>();
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error in snapshots operation {operation}: {ex.Message}");
                return Array.Empty<object>();
            }
        }

        private async Task<IEnumerable<object>> ListCollectionsAsync()
        {
            var response = await GetJsonAsync("collections");
            if (response != null && response.TryGetValue("result", out var result))
            {
                if (result is JsonElement element && element.ValueKind == JsonValueKind.Object)
                {
                    if (element.TryGetProperty("collections", out var collections) && 
                        collections.ValueKind == JsonValueKind.Array)
                    {
                        return collections.EnumerateArray()
                            .Select(c => (object)JsonSerializer.Deserialize<Dictionary<string, object>>(c.GetRawText()))
                            .ToList();
                    }
                }
            }
            return Array.Empty<object>();
        }

        private async Task<IEnumerable<object>> GetCollectionAsync(string collectionName)
        {
            var response = await GetJsonAsync($"collections/{collectionName}");
            return response != null ? new[] { response } : Array.Empty<object>();
        }

        private async Task<IEnumerable<object>> SearchPointsAsync(string collectionName, List<AppFilter> filters)
        {
            var vectorFilter = filters?.FirstOrDefault(f => f.FieldName.Equals("vector", StringComparison.OrdinalIgnoreCase));
            if (vectorFilter?.FilterValue is not string vectorStr || !TryParseFloatArray(vectorStr, out float[] searchVector))
            {
                throw new ArgumentException("Search requires a 'vector' filter with float array value");
            }

            var limitFilter = filters?.FirstOrDefault(f => f.FieldName.Equals("limit", StringComparison.OrdinalIgnoreCase));
            var limit = 10;
            if (limitFilter != null && int.TryParse(limitFilter.FilterValue?.ToString(), out int l))
            {
                limit = l;
            }

            var request = new
            {
                vector = searchVector,
                limit = limit,
                with_payload = true,
                with_vector = false
            };

            var response = await PostJsonAsync($"collections/{collectionName}/points/search", request);
            if (response != null && response.TryGetValue("result", out var result))
            {
                if (result is JsonElement element && element.ValueKind == JsonValueKind.Array)
                {
                    return element.EnumerateArray()
                        .Select(item => (object)JsonSerializer.Deserialize<Dictionary<string, object>>(item.GetRawText()))
                        .ToList();
                }
            }
            return Array.Empty<object>();
        }

        private async Task<IEnumerable<object>> ScrollPointsAsync(string collectionName, List<AppFilter> filters)
        {
            var limitFilter = filters?.FirstOrDefault(f => f.FieldName.Equals("limit", StringComparison.OrdinalIgnoreCase));
            var limit = 10;
            if (limitFilter != null && int.TryParse(limitFilter.FilterValue?.ToString(), out int l))
            {
                limit = l;
            }

            var request = new
            {
                limit = limit,
                with_payload = true,
                with_vector = false
            };

            var response = await PostJsonAsync($"collections/{collectionName}/points/scroll", request);
            if (response != null && response.TryGetValue("result", out var result))
            {
                if (result is JsonElement element && element.TryGetProperty("points", out var points) &&
                    points.ValueKind == JsonValueKind.Array)
                {
                    return points.EnumerateArray()
                        .Select(item => (object)JsonSerializer.Deserialize<Dictionary<string, object>>(item.GetRawText()))
                        .ToList();
                }
            }
            return Array.Empty<object>();
        }

        private async Task<IEnumerable<object>> RecommendPointsAsync(string collectionName, List<AppFilter> filters)
        {
            var positiveFilter = filters?.FirstOrDefault(f => f.FieldName.Equals("positive", StringComparison.OrdinalIgnoreCase));
            if (positiveFilter?.FilterValue == null)
            {
                throw new ArgumentException("Recommend requires a 'positive' filter with point IDs");
            }

            var positive = positiveFilter.FilterValue.ToString().Split(',').Select(id => id.Trim()).ToList();
            var limitFilter = filters?.FirstOrDefault(f => f.FieldName.Equals("limit", StringComparison.OrdinalIgnoreCase));
            var limit = 10;
            if (limitFilter != null && int.TryParse(limitFilter.FilterValue?.ToString(), out int l))
            {
                limit = l;
            }

            var request = new
            {
                positive = positive,
                limit = limit,
                with_payload = true
            };

            var response = await PostJsonAsync($"collections/{collectionName}/points/recommend", request);
            if (response != null && response.TryGetValue("result", out var result))
            {
                if (result is JsonElement element && element.ValueKind == JsonValueKind.Array)
                {
                    return element.EnumerateArray()
                        .Select(item => (object)JsonSerializer.Deserialize<Dictionary<string, object>>(item.GetRawText()))
                        .ToList();
                }
            }
            return Array.Empty<object>();
        }

        private async Task<IEnumerable<object>> ListSnapshotsAsync(string collectionName)
        {
            var response = await GetJsonAsync($"collections/{collectionName}/snapshots");
            if (response != null && response.TryGetValue("result", out var result))
            {
                if (result is JsonElement element && element.ValueKind == JsonValueKind.Array)
                {
                    return element.EnumerateArray()
                        .Select(item => (object)JsonSerializer.Deserialize<Dictionary<string, object>>(item.GetRawText()))
                        .ToList();
                }
            }
            return Array.Empty<object>();
        }

        #endregion

        #region Helper Methods

        private Dictionary<string, string> FiltersToQdrantQuery(List<AppFilter> filters)
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
                    throw new InvalidOperationException($"Qdrant operation '{operation}' requires filter '{field}'");
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

        private EntityStructure CreateQdrantEntityStructure(string operationName)
        {
            var entity = new EntityStructure
            {
                EntityName = operationName,
                DatasourceEntityName = operationName,
                DataSourceID = DatasourceName,
                DatabaseType = DataSourceType.Qdrant,
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
                        new EntityField { fieldname = "name", fieldtype = "System.String", IsKey = true },
                        new EntityField { fieldname = "vectors_count", fieldtype = "System.Int64" },
                        new EntityField { fieldname = "status", fieldtype = "System.String" },
                        new EntityField { fieldname = "optimizer_status", fieldtype = "System.String" }
                    });
                    break;
                case "points":
                    entity.Fields.AddRange(new[]
                    {
                        new EntityField { fieldname = "id", fieldtype = "System.String", IsKey = true },
                        new EntityField { fieldname = "vector", fieldtype = "System.Single[]" },
                        new EntityField { fieldname = "payload", fieldtype = "System.Object" },
                        new EntityField { fieldname = "score", fieldtype = "System.Single" }
                    });
                    break;
                case "snapshots":
                    entity.Fields.AddRange(new[]
                    {
                        new EntityField { fieldname = "name", fieldtype = "System.String", IsKey = true },
                        new EntityField { fieldname = "creation_time", fieldtype = "System.DateTime" },
                        new EntityField { fieldname = "size", fieldtype = "System.Int64" }
                    });
                    break;
            }

            return entity;
        }

        private async Task<Dictionary<string, object>> GetJsonAsync(string endpoint)
        {
            try
            {
                var url = $"{_baseUrl}{endpoint}";
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<Dictionary<string, object>>(content);
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error in GetJsonAsync to {endpoint}: {ex.Message}");
            }
            return null;
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
                Logger?.WriteLog($"Error in PostJsonAsync to {endpoint}: {ex.Message}");
            }
            return null;
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            try
            {
                Closeconnection();
                _httpClient?.Dispose();
                Logger?.WriteLog("Qdrant datasource disposed");
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error disposing datasource: {ex.Message}");
            }
        }

        #endregion
    }
}
