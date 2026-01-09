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

namespace TheTechIdea.Beep.MilvusDatasource
{
    /// <summary>
    /// Milvus vector database data source implementation
    /// Milvus is an open-source vector database built for scalable similarity search
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.VectorDB, DatasourceType = DataSourceType.Milvus)]
    public class MilvusDataSource : IDataSource
    {
        #region IDataSource Properties
        public string ColumnDelimiter { get; set; } = "\"";
        public string ParameterDelimiter { get; set; } = ":";
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public string Id { get; set; }
        public DataSourceType DatasourceType { get; set; } = DataSourceType.Milvus;
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
        
        // Milvus REST API endpoints mapping
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // Collection management
            ["collections.list"] = "v1/vector/collections",
            ["collections.describe"] = "v1/vector/collections/describe",
            ["collections.create"] = "v1/vector/collections/create",
            ["collections.drop"] = "v1/vector/collections/drop",
            ["collections.load"] = "v1/vector/collections/load",
            ["collections.release"] = "v1/vector/collections/release",
            ["collections.stats"] = "v1/vector/collections/get_stats",
            
            // Vector operations
            ["vectors.insert"] = "v1/vector/insert",
            ["vectors.search"] = "v1/vector/search",
            ["vectors.query"] = "v1/vector/query",
            ["vectors.delete"] = "v1/vector/delete",
            
            // Index operations
            ["indexes.create"] = "v1/vector/indexes/create",
            ["indexes.describe"] = "v1/vector/indexes/describe",
            ["indexes.drop"] = "v1/vector/indexes/drop"
        };

        private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            ["collections.describe"] = new[] { "collection_name" },
            ["collections.drop"] = new[] { "collection_name" },
            ["collections.load"] = new[] { "collection_name" },
            ["collections.release"] = new[] { "collection_name" },
            ["collections.stats"] = new[] { "collection_name" },
            ["vectors.insert"] = new[] { "collection_name", "vectors" },
            ["vectors.search"] = new[] { "collection_name", "vector" },
            ["vectors.query"] = new[] { "collection_name" },
            ["vectors.delete"] = new[] { "collection_name" },
            ["indexes.create"] = new[] { "collection_name", "field_name" },
            ["indexes.describe"] = new[] { "collection_name" },
            ["indexes.drop"] = new[] { "collection_name", "field_name" }
        };
        #endregion

        #region Constructor
        public MilvusDataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor dmeEditor,
            DataSourceType databasetype,
            IErrorsInfo errorObject)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            DMEEditor = dmeEditor;
            DatasourceType = DataSourceType.Milvus;
            Category = DatasourceCategory.VectorDB;
            ErrorObject = errorObject ?? dmeEditor?.ErrorObject;
            Id = Guid.NewGuid().ToString();
            
            _httpClient = new HttpClient();
            InitializeConnection();
            
            EntitiesNames = EntityEndpoints.Keys.ToList();
            Entities = EntitiesNames
                .Select(n => CreateMilvusEntityStructure(n))
                .ToList();
        }

        private void InitializeConnection()
        {
            if (Dataconnection == null)
            {
                Dataconnection = new MilvusDataConnection(DMEEditor)
                {
                    ConnectionProp = new ConnectionProperties
                    {
                        Host = "localhost",
                        Port = 9091, // Default Milvus port
                        DatabaseType = DataSourceType.Milvus,
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
                throw new InvalidOperationException($"Unknown Milvus operation '{EntityName}'.");

            var queryParams = FiltersToMilvusQuery(Filter);
            RequireFilters(EntityName, queryParams, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            return EntityName.Split('.')[0] switch
            {
                "collections" => await HandleCollectionOperation(EntityName, endpoint, queryParams),
                "vectors" => await HandleVectorOperation(EntityName, endpoint, queryParams, Filter),
                "indexes" => await HandleIndexOperation(EntityName, endpoint, queryParams),
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
                Logger?.WriteLog($"Executing Milvus query: {qrystr}");
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
                Logger?.WriteLog("Opening connection to Milvus");
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
                Logger?.WriteLog("Closing connection to Milvus");
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
                Logger?.WriteLog($"Creating Milvus collection: {entity.EntityName}");
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
                Logger?.WriteLog($"Inserting into Milvus entity: {EntityName}");
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
                Logger?.WriteLog($"Updating Milvus entity: {EntityName}");
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
                Logger?.WriteLog($"Bulk updating Milvus entity: {EntityName}");
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
                Logger?.WriteLog($"Deleting from Milvus entity: {EntityName}");
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
                Logger?.WriteLog($"Executing Milvus command: {sql}");
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
                    ddl = $"-- Create Milvus collection: {entity.EntityName}",
                    scriptType = DDLScriptType.CreateEntity
                });
            }
            return scripts;
        }

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            try
            {
                Logger?.WriteLog($"Running Milvus script");
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
                Logger?.WriteLog("Beginning Milvus transaction");
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
                Logger?.WriteLog("Ending Milvus transaction");
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
                Logger?.WriteLog("Committing Milvus transaction");
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

        #region Milvus-specific Operations

        private async Task<IEnumerable<object>> HandleCollectionOperation(string operation, string endpoint, Dictionary<string, string> queryParams)
        {
            try
            {
                switch (operation)
                {
                    case "collections.list":
                        return await ListCollectionsAsync();
                    case "collections.describe":
                        return await DescribeCollectionAsync(queryParams["collection_name"]);
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

        private async Task<IEnumerable<object>> HandleVectorOperation(string operation, string endpoint, Dictionary<string, string> queryParams, List<AppFilter> filters)
        {
            try
            {
                switch (operation)
                {
                    case "vectors.search":
                        return await SearchVectorsAsync(queryParams["collection_name"], filters);
                    case "vectors.query":
                        return await QueryVectorsAsync(queryParams["collection_name"], filters);
                    default:
                        return Array.Empty<object>();
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error in vector operation {operation}: {ex.Message}");
                return Array.Empty<object>();
            }
        }

        private async Task<IEnumerable<object>> HandleIndexOperation(string operation, string endpoint, Dictionary<string, string> queryParams)
        {
            try
            {
                switch (operation)
                {
                    case "indexes.describe":
                        return await DescribeIndexAsync(queryParams["collection_name"]);
                    default:
                        return Array.Empty<object>();
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error in index operation {operation}: {ex.Message}");
                return Array.Empty<object>();
            }
        }

        private async Task<IEnumerable<object>> ListCollectionsAsync()
        {
            var response = await GetJsonAsync("v1/vector/collections");
            if (response != null && response.TryGetValue("data", out var data))
            {
                if (data is JsonElement element && element.ValueKind == JsonValueKind.Array)
                {
                    return element.EnumerateArray()
                        .Select(c => (object)JsonSerializer.Deserialize<Dictionary<string, object>>(c.GetRawText()))
                        .ToList();
                }
            }
            return Array.Empty<object>();
        }

        private async Task<IEnumerable<object>> DescribeCollectionAsync(string collectionName)
        {
            var request = new { collectionName = collectionName };
            var response = await PostJsonAsync("v1/vector/collections/describe", request);
            return response != null ? new[] { response } : Array.Empty<object>();
        }

        private async Task<IEnumerable<object>> SearchVectorsAsync(string collectionName, List<AppFilter> filters)
        {
            var vectorFilter = filters?.FirstOrDefault(f => f.FieldName.Equals("vector", StringComparison.OrdinalIgnoreCase));
            if (vectorFilter?.FilterValue is not string vectorStr || !TryParseFloatArray(vectorStr, out float[] searchVector))
            {
                throw new ArgumentException("Search requires a 'vector' filter with float array value");
            }

            var topKFilter = filters?.FirstOrDefault(f => f.FieldName.Equals("topK", StringComparison.OrdinalIgnoreCase) || 
                                                           f.FieldName.Equals("limit", StringComparison.OrdinalIgnoreCase));
            var topK = 10;
            if (topKFilter != null && int.TryParse(topKFilter.FilterValue?.ToString(), out int k))
            {
                topK = k;
            }

            var request = new
            {
                collectionName = collectionName,
                vector = searchVector,
                topK = topK,
                outputFields = new[] { "*" }
            };

            var response = await PostJsonAsync("v1/vector/search", request);
            if (response != null && response.TryGetValue("data", out var data))
            {
                if (data is JsonElement element && element.ValueKind == JsonValueKind.Array)
                {
                    return element.EnumerateArray()
                        .Select(item => (object)JsonSerializer.Deserialize<Dictionary<string, object>>(item.GetRawText()))
                        .ToList();
                }
            }
            return Array.Empty<object>();
        }

        private async Task<IEnumerable<object>> QueryVectorsAsync(string collectionName, List<AppFilter> filters)
        {
            var limitFilter = filters?.FirstOrDefault(f => f.FieldName.Equals("limit", StringComparison.OrdinalIgnoreCase));
            var limit = 100;
            if (limitFilter != null && int.TryParse(limitFilter.FilterValue?.ToString(), out int l))
            {
                limit = l;
            }

            var request = new
            {
                collectionName = collectionName,
                limit = limit,
                outputFields = new[] { "*" }
            };

            var response = await PostJsonAsync("v1/vector/query", request);
            if (response != null && response.TryGetValue("data", out var data))
            {
                if (data is JsonElement element && element.ValueKind == JsonValueKind.Array)
                {
                    return element.EnumerateArray()
                        .Select(item => (object)JsonSerializer.Deserialize<Dictionary<string, object>>(item.GetRawText()))
                        .ToList();
                }
            }
            return Array.Empty<object>();
        }

        private async Task<IEnumerable<object>> DescribeIndexAsync(string collectionName)
        {
            var request = new { collectionName = collectionName };
            var response = await PostJsonAsync("v1/vector/indexes/describe", request);
            return response != null ? new[] { response } : Array.Empty<object>();
        }

        #endregion

        #region Helper Methods

        private Dictionary<string, string> FiltersToMilvusQuery(List<AppFilter> filters)
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
                    throw new InvalidOperationException($"Milvus operation '{operation}' requires filter '{field}'");
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

        private EntityStructure CreateMilvusEntityStructure(string operationName)
        {
            var entity = new EntityStructure
            {
                EntityName = operationName,
                DatasourceEntityName = operationName,
                DataSourceID = DatasourceName,
                DatabaseType = DataSourceType.Milvus,
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
                        new EntityField { fieldname = "collection_name", fieldtype = "System.String", IsKey = true },
                        new EntityField { fieldname = "description", fieldtype = "System.String" },
                        new EntityField { fieldname = "vector_count", fieldtype = "System.Int64" },
                        new EntityField { fieldname = "index_count", fieldtype = "System.Int32" }
                    });
                    break;
                case "vectors":
                    entity.Fields.AddRange(new[]
                    {
                        new EntityField { fieldname = "id", fieldtype = "System.String", IsKey = true },
                        new EntityField { fieldname = "vector", fieldtype = "System.Single[]" },
                        new EntityField { fieldname = "distance", fieldtype = "System.Single" },
                        new EntityField { fieldname = "fields", fieldtype = "System.Object" }
                    });
                    break;
                case "indexes":
                    entity.Fields.AddRange(new[]
                    {
                        new EntityField { fieldname = "index_name", fieldtype = "System.String", IsKey = true },
                        new EntityField { fieldname = "field_name", fieldtype = "System.String" },
                        new EntityField { fieldname = "metric_type", fieldtype = "System.String" },
                        new EntityField { fieldname = "index_type", fieldtype = "System.String" }
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
                Logger?.WriteLog("Milvus datasource disposed");
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error disposing datasource: {ex.Message}");
            }
        }

        #endregion
    }
}
