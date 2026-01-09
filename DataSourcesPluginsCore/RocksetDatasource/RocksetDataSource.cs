using System;
using System.Collections.Generic;
using System.Data;
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
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;

namespace TheTechIdea.Beep.Cloud.Rockset
{
    [AddinAttribute(Category = DatasourceCategory.CLOUD, DatasourceType = DataSourceType.Rockset)]
    public class RocksetDataSource : IDataSource
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

        private HttpClient _httpClient;
        private string _apiKey;
        private string _apiServer;
        private string _workspace;

        public RocksetDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = databasetype;
            Category = DatasourceCategory.CLOUD;

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
                    DatabaseType = DataSourceType.Rockset,
                    Category = DatasourceCategory.CLOUD
                };
            }

            _apiKey = Dataconnection.ConnectionProp?.KeyToken ?? "";
            _apiServer = Dataconnection.ConnectionProp?.Url ?? "https://api.rs2.usw2.rockset.com";
            _workspace = Dataconnection.ConnectionProp?.Database ?? "commons";
            
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
                if (string.IsNullOrEmpty(_apiKey))
                {
                    ConnectionStatus = ConnectionState.Closed;
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = "API key is required";
                    return ConnectionStatus;
                }

                _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("ApiKey", _apiKey);
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                
                ConnectionStatus = ConnectionState.Open;
                GetEntitesList();
                DMEEditor?.AddLogMessage("Beep", $"Connected to Rockset: {_apiServer}", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Closed;
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error connecting to Rockset: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return ConnectionStatus;
        }

        public ConnectionState Closeconnection()
        {
            try
            {
                _httpClient?.Dispose();
                _httpClient = null;
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

                if (ConnectionStatus == ConnectionState.Open && _httpClient != null)
                {
                    // Query Rockset API to get collections
                    string url = $"{_apiServer}/v1/orgs/self/ws/{_workspace}/collections";
                    var response = _httpClient.GetAsync(url).Result;
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var content = response.Content.ReadAsStringAsync().Result;
                        var json = JObject.Parse(content);
                        var collections = json["data"] as JArray;
                        
                        if (collections != null)
                        {
                            foreach (var collection in collections)
                            {
                                var name = collection["name"]?.ToString();
                                if (!string.IsNullOrEmpty(name))
                                {
                                    collectionNames.Add(name);
                                }
                            }
                        }
                    }
                    
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

                if (ConnectionStatus == ConnectionState.Open && _httpClient != null)
                {
                    // Build SQL query
                    var query = new StringBuilder($"SELECT * FROM {EntityName}");
                    
                    if (filter != null && filter.Count > 0)
                    {
                        query.Append(" WHERE ");
                        bool first = true;
                        foreach (var f in filter)
                        {
                            if (!first) query.Append(" AND ");
                            query.Append($"{f.FieldName} {f.Operator} {PrepareValue(f.FilterValue, f.valueType)}");
                            first = false;
                        }
                    }

                    // Execute query via Rockset API
                    string url = $"{_apiServer}/v1/orgs/self/ws/{_workspace}/queries";
                    var queryRequest = new
                    {
                        sql = new
                        {
                            query = query.ToString()
                        }
                    };

                    var jsonContent = System.Text.Json.JsonSerializer.Serialize(queryRequest);
                    var httpContent = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
                    var response = _httpClient.PostAsync(url, httpContent).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        var content = response.Content.ReadAsStringAsync().Result;
                        var json = JObject.Parse(content);
                        var resultsArray = json["results"] as JArray;
                        
                        if (resultsArray != null)
                        {
                            foreach (var item in resultsArray)
                            {
                                results.Add(item.ToObject<Dictionary<string, object>>());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error getting entity: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return results;
        }

        private string PrepareValue(string value, string type)
        {
            if (string.IsNullOrEmpty(type) || type == "System.String")
            {
                return $"'{value?.Replace("'", "''")}'";
            }
            return value ?? "NULL";
        }

        public PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            PagedResult pagedResult = new PagedResult();
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open && _httpClient != null)
                {
                    // Build count query
                    var countQuery = new StringBuilder($"SELECT COUNT(*) as total FROM {EntityName}");
                    if (filter != null && filter.Count > 0)
                    {
                        countQuery.Append(" WHERE ");
                        bool first = true;
                        foreach (var f in filter)
                        {
                            if (!first) countQuery.Append(" AND ");
                            countQuery.Append($"{f.FieldName} {f.Operator} {PrepareValue(f.FilterValue, f.valueType)}");
                            first = false;
                        }
                    }

                    // Get total count
                    string countUrl = $"{_apiServer}/v1/orgs/self/ws/{_workspace}/queries";
                    var countRequest = new
                    {
                        sql = new
                        {
                            query = countQuery.ToString()
                        }
                    };

                    var countJson = System.Text.Json.JsonSerializer.Serialize(countRequest);
                    var countContent = new StringContent(countJson, System.Text.Encoding.UTF8, "application/json");
                    var countResponse = _httpClient.PostAsync(countUrl, countContent).Result;

                    int totalRecords = 0;
                    if (countResponse.IsSuccessStatusCode)
                    {
                        var countResult = countResponse.Content.ReadAsStringAsync().Result;
                        var countJsonObj = JObject.Parse(countResult);
                        var countResults = countJsonObj["results"] as JArray;
                        if (countResults != null && countResults.Count > 0)
                        {
                            totalRecords = countResults[0]["total"].Value<int>();
                        }
                    }

                    // Build paginated query
                    int offset = (pageNumber - 1) * pageSize;
                    var query = new StringBuilder($"SELECT * FROM {EntityName}");
                    if (filter != null && filter.Count > 0)
                    {
                        query.Append(" WHERE ");
                        bool first = true;
                        foreach (var f in filter)
                        {
                            if (!first) query.Append(" AND ");
                            query.Append($"{f.FieldName} {f.Operator} {PrepareValue(f.FilterValue, f.valueType)}");
                            first = false;
                        }
                    }
                    query.Append($" LIMIT {pageSize} OFFSET {offset}");

                    // Execute paginated query
                    string queryUrl = $"{_apiServer}/v1/orgs/self/ws/{_workspace}/queries";
                    var queryRequest = new
                    {
                        sql = new
                        {
                            query = query.ToString()
                        }
                    };

                    var queryJson = System.Text.Json.JsonSerializer.Serialize(queryRequest);
                    var queryHttpContent = new StringContent(queryJson, System.Text.Encoding.UTF8, "application/json");
                    var queryResponse = _httpClient.PostAsync(queryUrl, queryHttpContent).Result;

                    List<object> results = new List<object>();
                    if (queryResponse.IsSuccessStatusCode)
                    {
                        var queryResult = queryResponse.Content.ReadAsStringAsync().Result;
                        var queryJsonObj = JObject.Parse(queryResult);
                        var resultsArray = queryJsonObj["results"] as JArray;
                        
                        if (resultsArray != null)
                        {
                            foreach (var item in resultsArray)
                            {
                                results.Add(item.ToObject<Dictionary<string, object>>());
                            }
                        }
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
                DMEEditor?.AddLogMessage("Beep", $"Error in GetEntity with pagination: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
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

                if (ConnectionStatus == ConnectionState.Open && _httpClient != null)
                {
                    // Query sample data to infer schema
                    var sampleQuery = $"SELECT * FROM {EntityName} LIMIT 1";
                    string url = $"{_apiServer}/v1/orgs/self/ws/{_workspace}/queries";
                    var queryRequest = new
                    {
                        sql = new
                        {
                            query = sampleQuery
                        }
                    };

                    var jsonContent = System.Text.Json.JsonSerializer.Serialize(queryRequest);
                    var httpContent = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
                    var response = _httpClient.PostAsync(url, httpContent).Result;

                    EntityStructure entity = new EntityStructure
                    {
                        EntityName = EntityName,
                        DatasourceEntityName = EntityName,
                        Fields = new List<EntityField>()
                    };

                    if (response.IsSuccessStatusCode)
                    {
                        var content = response.Content.ReadAsStringAsync().Result;
                        var json = JObject.Parse(content);
                        var resultsArray = json["results"] as JArray;
                        
                        if (resultsArray != null && resultsArray.Count > 0)
                        {
                            var sampleDoc = resultsArray[0] as JObject;
                            if (sampleDoc != null)
                            {
                                foreach (var prop in sampleDoc.Properties())
                                {
                                    entity.Fields.Add(new EntityField
                                    {
                                        fieldname = prop.Name,
                                        Originalfieldname = prop.Name,
                                        fieldtype = GetFieldType(prop.Value),
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

        private string GetFieldType(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.String:
                    return "System.String";
                case JTokenType.Integer:
                    return "System.Int64";
                case JTokenType.Float:
                    return "System.Double";
                case JTokenType.Boolean:
                    return "System.Boolean";
                case JTokenType.Date:
                    return "System.DateTime";
                case JTokenType.Object:
                case JTokenType.Array:
                    return "System.String"; // Serialize complex types as JSON strings
                default:
                    return "System.String";
            }
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
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open && _httpClient != null)
                {
                    string url = $"{_apiServer}/v1/orgs/self/ws/{_workspace}/queries";
                    var queryRequest = new
                    {
                        sql = new
                        {
                            query = sql
                        }
                    };

                    var jsonContent = System.Text.Json.JsonSerializer.Serialize(queryRequest);
                    var httpContent = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
                    var response = _httpClient.PostAsync(url, httpContent).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        ErrorObject.Message = "Query executed successfully";
                    }
                    else
                    {
                        ErrorObject.Flag = Errors.Failed;
                        ErrorObject.Message = $"Query failed: {response.ReasonPhrase}";
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error executing SQL: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
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
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open && _httpClient != null)
                {
                    string url = $"{_apiServer}/v1/orgs/self/ws/{_workspace}/queries";
                    var queryRequest = new
                    {
                        sql = new
                        {
                            query = qrystr
                        }
                    };

                    var jsonContent = System.Text.Json.JsonSerializer.Serialize(queryRequest);
                    var httpContent = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
                    var response = _httpClient.PostAsync(url, httpContent).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        var content = response.Content.ReadAsStringAsync().Result;
                        var json = JObject.Parse(content);
                        var resultsArray = json["results"] as JArray;
                        
                        if (resultsArray != null)
                        {
                            foreach (var item in resultsArray)
                            {
                                results.Add(item.ToObject<Dictionary<string, object>>());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error running query: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return results;
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Failed, Message = "Update operation not directly supported. Use Rockset API for document updates." };
            DMEEditor?.AddLogMessage("Beep", "Use Rockset API for document updates", DateTime.Now, 0, null, Errors.Failed);
            return retval;
        }

        public IErrorsInfo DeleteEntity(string EntityName, object DeletedDataRow)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Failed, Message = "Delete operation not directly supported. Use Rockset API for document deletion." };
            DMEEditor?.AddLogMessage("Beep", "Use Rockset API for document deletion", DateTime.Now, 0, null, Errors.Failed);
            return retval;
        }

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                ExecuteSql(dDLScripts.ScriptText);
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
            }
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
                        ScriptText = $"# Rockset collection: {entity.EntityName}\n# Create via Rockset API or console"
                    });
                }
            }
            return scripts;
        }

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Failed, Message = "Insert operation not directly supported. Use Rockset API for document insertion." };
            DMEEditor?.AddLogMessage("Beep", "Use Rockset API for document insertion", DateTime.Now, 0, null, Errors.Failed);
            return retval;
        }

        public virtual double GetScalar(string query)
        {
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open && _httpClient != null)
                {
                    string url = $"{_apiServer}/v1/orgs/self/ws/{_workspace}/queries";
                    var queryRequest = new
                    {
                        sql = new
                        {
                            query = query
                        }
                    };

                    var jsonContent = System.Text.Json.JsonSerializer.Serialize(queryRequest);
                    var httpContent = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
                    var response = _httpClient.PostAsync(url, httpContent).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        var content = response.Content.ReadAsStringAsync().Result;
                        var json = JObject.Parse(content);
                        var resultsArray = json["results"] as JArray;
                        
                        if (resultsArray != null && resultsArray.Count > 0)
                        {
                            var firstValue = resultsArray[0].Values().FirstOrDefault();
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
                DMEEditor?.AddLogMessage("Fail", $"Error in executing scalar query ({ex.Message})", DateTime.Now, 0, "", Errors.Failed);
            }
            return 0.0;
        }

        public virtual Task<double> GetScalarAsync(string query)
        {
            return Task.Run(() => GetScalar(query));
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Failed, Message = "Bulk update not directly supported. Use Rockset API." };
            return retval;
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