using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Net.Http;
using System.Net.Http.Headers;
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

namespace TheTechIdea.Beep.Connectors.HubSpot
{
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.HubSpot)]
    public class HubSpotDataSource : IDataSource
    {
        #region "Properties"
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public DataSourceType DatasourceType { get; set; } = DataSourceType.HubSpot;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.Connector;
        public IDataConnection Dataconnection { get; set; }
        public string DatasourceName { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public string Id { get; set; }
        public IDMLogger Logger { get; set; }
        public List<string> EntitiesNames { get; set; } = new List<string>();
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
        public IDMEEditor DMEEditor { get; set; }
        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Closed;
        public string ColumnDelimiter { get; set; }
        public string ParameterDelimiter { get; set; }
        public event EventHandler<PassedArgs> PassEvent;

        // HubSpot-specific properties
        private readonly HttpClient _httpClient;
        private HubSpotConfig _config;
        private bool _isConnected;
        #endregion

        #region "Constructor"
        public HubSpotDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType pDatasourceType, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = pDatasourceType;

            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://api.hubapi.com/");

            // Initialize HubSpot configuration from connection properties
            InitializeConfiguration();
        }
        #endregion

        #region "Configuration"
        private void InitializeConfiguration()
        {
            _config = new HubSpotConfig();

            // Get configuration from DMEEditor connection properties
            if (DMEEditor?.ConfigEditor?.DataConnections != null)
            {
                var connection = DMEEditor.ConfigEditor.DataConnections
                    .Find(c => c.ConnectionName.Equals(DatasourceName, StringComparison.InvariantCultureIgnoreCase));

                if (connection != null)
                {
                    _config.AccessToken = connection.Password;
                    _config.ApiKey = connection.UserID;
                }
            }
        }
        #endregion

        #region "Connection Methods"
        public ConnectionState Openconnection()
        {
            try
            {
                Logger.WriteLog("Opening connection to HubSpot");
                ConnectAsync().Wait();
                ConnectionStatus = ConnectionState.Open;
                Logger.WriteLog("Successfully connected to HubSpot");
                return ConnectionStatus;
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Failed to connect to HubSpot: {ex.Message}");
                ErrorObject.AddError("HubSpot Connection", ex.Message);
                ConnectionStatus = ConnectionState.Broken;
                return ConnectionStatus;
            }
        }

        public ConnectionState Closeconnection()
        {
            try
            {
                Logger.WriteLog("Closing connection to HubSpot");
                DisconnectAsync().Wait();
                ConnectionStatus = ConnectionState.Closed;
                Logger.WriteLog("Successfully disconnected from HubSpot");
                return ConnectionStatus;
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error closing HubSpot connection: {ex.Message}");
                ConnectionStatus = ConnectionState.Broken;
                return ConnectionStatus;
            }
        }

        private async Task<bool> ConnectAsync()
        {
            try
            {
                // Set authorization header
                if (!string.IsNullOrEmpty(_config.AccessToken))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _config.AccessToken);
                }
                else if (!string.IsNullOrEmpty(_config.ApiKey))
                {
                    // Alternative: Use API key as query parameter
                    _httpClient.DefaultRequestHeaders.Add("hapikey", _config.ApiKey);
                }
                else
                {
                    throw new InvalidOperationException("No authentication method provided for HubSpot");
                }

                _isConnected = true;

                // Load entity names
                await LoadEntityNamesAsync();

                return true;
            }
            catch (Exception ex)
            {
                throw new CRMDriverException("Failed to connect to HubSpot", ex);
            }
        }

        private async Task DisconnectAsync()
        {
            _isConnected = false;
            _httpClient.DefaultRequestHeaders.Authorization = null;
            _httpClient.DefaultRequestHeaders.Remove("hapikey");
        }

        private async Task LoadEntityNamesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("crm/v3/objects");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<dynamic>(content);

                EntitiesNames.Clear();
                if (result.results != null)
                {
                    foreach (var entity in result.results)
                    {
                        EntitiesNames.Add(entity.name.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Failed to load entity names: {ex.Message}");
            }
        }
        #endregion

        #region "Data Operations"
        public bool CheckEntityExist(string EntityName) => EntitiesNames.Contains(EntityName);

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            Logger.WriteLog("Creating entities in HubSpot");
            return ErrorObject;
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            Logger.WriteLog($"Creating entity: {entity.EntityName}");
            return true;
        }

        public TheTechIdea.Beep.DataBase.PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            try
            {
                Logger.WriteLog($"Retrieving paginated entity: {EntityName}");
                var task = GetEntityAsync(EntityName, filter);
                task.Wait();
                var result = task.Result;

                return new TheTechIdea.Beep.DataBase.PagedResult
                {
                    Data = result,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = result.Count
                };
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error retrieving entity {EntityName}: {ex.Message}");
                ErrorObject.AddError("HubSpot GetEntity", ex.Message);
                return new TheTechIdea.Beep.DataBase.PagedResult();
            }
        }

        public IBindingList GetEntity(string EntityName, List<AppFilter> filter)
        {
            try
            {
                Logger.WriteLog($"Retrieving entity: {EntityName}");
                var task = GetEntityAsync(EntityName, filter);
                task.Wait();
                return new BindingList<object>(task.Result.Cast<object>().ToList());
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error retrieving entity {EntityName}: {ex.Message}");
                ErrorObject.AddError("HubSpot GetEntity", ex.Message);
                return new BindingList<object>();
            }
        }

        public Task<IBindingList> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            return Task.FromResult(GetEntity(EntityName, Filter));
        }

        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            try
            {
                Logger.WriteLog($"Retrieving entity structure: {EntityName}");
                var task = GetEntityStructureAsync(EntityName);
                task.Wait();
                return task.Result;
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error retrieving entity structure {EntityName}: {ex.Message}");
                ErrorObject.AddError("HubSpot GetEntityStructure", ex.Message);
                return new EntityStructure();
            }
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            return GetEntityStructure(fnd?.EntityName, refresh);
        }

        public Type GetEntityType(string EntityName)
        {
            Logger.WriteLog($"Getting entity type: {EntityName}");
            return typeof(object);
        }

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            try
            {
                Logger.WriteLog($"Inserting data into entity: {EntityName}");
                var task = InsertEntityAsync(EntityName, InsertedData);
                task.Wait();
                return ErrorObject;
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error inserting entity {EntityName}: {ex.Message}");
                ErrorObject.AddError("HubSpot InsertEntity", ex.Message);
                return ErrorObject;
            }
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            try
            {
                Logger.WriteLog($"Updating entity: {EntityName}");
                var task = UpdateEntityAsync(EntityName, UploadDataRow);
                task.Wait();
                return ErrorObject;
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error updating entity {EntityName}: {ex.Message}");
                ErrorObject.AddError("HubSpot UpdateEntity", ex.Message);
                return ErrorObject;
            }
        }

        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            try
            {
                Logger.WriteLog($"Deleting entity: {EntityName}");
                var task = DeleteEntityAsync(EntityName, UploadDataRow);
                task.Wait();
                return ErrorObject;
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error deleting entity {EntityName}: {ex.Message}");
                ErrorObject.AddError("HubSpot DeleteEntity", ex.Message);
                return ErrorObject;
            }
        }
        #endregion

        #region "Async Data Operations"
        private async Task<IEnumerable<dynamic>> GetEntityAsync(string entityName, List<AppFilter> filters = null)
        {
            if (!_isConnected)
                throw new InvalidOperationException("Not connected to HubSpot");

            try
            {
                var endpoint = $"crm/v3/objects/{entityName}";
                var queryString = BuildQueryString(filters);

                var response = await _httpClient.GetAsync($"{endpoint}?{queryString}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<dynamic>(content);

                return ExtractRecordsFromResponse(result);
            }
            catch (Exception ex)
            {
                throw new CRMDriverException($"Failed to retrieve {entityName} entities", ex);
            }
        }

        private async Task<EntityStructure> GetEntityStructureAsync(string entityName)
        {
            if (!_isConnected)
                throw new InvalidOperationException("Not connected to HubSpot");

            try
            {
                var response = await _httpClient.GetAsync($"crm/v3/objects/{entityName}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<dynamic>(content);

                return ConvertToEntityStructure(result);
            }
            catch (Exception ex)
            {
                throw new CRMDriverException($"Failed to retrieve metadata for {entityName}", ex);
            }
        }

        private async Task<dynamic> InsertEntityAsync(string entityName, object data)
        {
            if (!_isConnected)
                throw new InvalidOperationException("Not connected to HubSpot");

            try
            {
                var endpoint = $"crm/v3/objects/{entityName}";
                var jsonContent = JsonSerializer.Serialize(new { properties = data });
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(endpoint, content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<dynamic>(responseContent);
            }
            catch (Exception ex)
            {
                throw new CRMDriverException($"Failed to create {entityName} entity", ex);
            }
        }

        private async Task<bool> UpdateEntityAsync(string entityName, object data)
        {
            if (!_isConnected)
                throw new InvalidOperationException("Not connected to HubSpot");

            try
            {
                var dataDict = data as Dictionary<string, object>;
                if (dataDict != null && dataDict.ContainsKey("id"))
                {
                    var entityId = dataDict["id"].ToString();
                    dataDict.Remove("id");

                    var endpoint = $"crm/v3/objects/{entityName}/{entityId}";
                    var jsonContent = JsonSerializer.Serialize(new { properties = dataDict });
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    var response = await _httpClient.PatchAsync(endpoint, content);
                    response.EnsureSuccessStatusCode();

                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new CRMDriverException($"Failed to update {entityName} entity", ex);
            }
        }

        private async Task<bool> DeleteEntityAsync(string entityName, object data)
        {
            if (!_isConnected)
                throw new InvalidOperationException("Not connected to HubSpot");

            try
            {
                var dataDict = data as Dictionary<string, object>;
                if (dataDict != null && dataDict.ContainsKey("id"))
                {
                    var entityId = dataDict["id"].ToString();
                    var endpoint = $"crm/v3/objects/{entityName}/{entityId}";

                    var response = await _httpClient.DeleteAsync(endpoint);
                    response.EnsureSuccessStatusCode();

                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new CRMDriverException($"Failed to delete {entityName} entity", ex);
            }
        }
        #endregion

        #region "Utility Methods"
        private string BuildQueryString(List<AppFilter> filters)
        {
            if (filters == null || filters.Count == 0)
                return string.Empty;

            var queryParams = new List<string>();
            foreach (var filter in filters)
            {
                queryParams.Add($"properties={Uri.EscapeDataString(filter.FieldName)}");
            }
            return string.Join("&", queryParams);
        }

        private IEnumerable<dynamic> ExtractRecordsFromResponse(dynamic response)
        {
            var records = new List<dynamic>();
            if (response.results != null)
            {
                foreach (var record in response.results)
                {
                    records.Add(record);
                }
            }
            return records;
        }

        private EntityStructure ConvertToEntityStructure(dynamic metadata)
        {
            var entityStructure = new EntityStructure
            {
                EntityName = metadata.name,
                Caption = metadata.label ?? metadata.name
            };

            if (metadata.properties != null)
            {
                foreach (var property in metadata.properties)
                {
                    entityStructure.Fields.Add(new EntityField
                    {
                        FieldName = property.name,
                        FieldType = MapHubSpotTypeToSystemType(property.type),
                        Caption = property.label ?? property.name,
                        IsKey = property.name == "id",
                        AllowNull = !property.required,
                        IsAutoIncrement = false
                    });
                }
            }

            return entityStructure;
        }

        private Type MapHubSpotTypeToSystemType(string hubSpotType)
        {
            return hubSpotType.ToLower() switch
            {
                "string" => typeof(string),
                "number" => typeof(double),
                "bool" => typeof(bool),
                "date" => typeof(DateTime),
                "datetime" => typeof(DateTime),
                "enumeration" => typeof(string),
                _ => typeof(string)
            };
        }
        #endregion

        #region "Standard Interface Methods"
        public IErrorsInfo ExecuteSql(string sql)
        {
            Logger.WriteLog("Executing SQL command");
            return ErrorObject;
        }

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            Logger.WriteLog("Running script");
            return ErrorObject;
        }

        public List<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            Logger.WriteLog("Getting child tables list");
            return new List<ChildRelation>();
        }

        public List<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            Logger.WriteLog("Getting create entity script");
            return new List<ETLScriptDet>();
        }

        public List<string> GetEntitesList()
        {
            Logger.WriteLog("Getting entities list");
            return EntitiesNames;
        }

        public int GetEntityIdx(string entityName)
        {
            Logger.WriteLog($"Getting entity index: {entityName}");
            return EntitiesNames.IndexOf(entityName);
        }

        public List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            Logger.WriteLog("Getting entity foreign keys");
            return new List<RelationShipKeys>();
        }

        public double GetScalar(string query)
        {
            Logger.WriteLog("Getting scalar value");
            return 0.0;
        }

        public Task<double> GetScalarAsync(string query)
        {
            return Task.FromResult(0.0);
        }

        public IBindingList RunQuery(string qrystr)
        {
            Logger.WriteLog("Running query");
            return new BindingList<object>();
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            Logger.WriteLog("Updating entities");
            return ErrorObject;
        }

        public IErrorsInfo BeginTransaction(PassedArgs args)
        {
            Logger.WriteLog("Beginning transaction");
            return ErrorObject;
        }

        public IErrorsInfo Commit(PassedArgs args)
        {
            Logger.WriteLog("Committing transaction");
            return ErrorObject;
        }

        public IErrorsInfo EndTransaction(PassedArgs args)
        {
            Logger.WriteLog("Ending transaction");
            return ErrorObject;
        }

        public void Dispose()
        {
            Closeconnection();
        }
        #endregion
    }

    #region "Configuration and Helper Classes"
    public class HubSpotConfig
    {
        public string AccessToken { get; set; }
        public string ApiKey { get; set; } // Alternative authentication method
    }

    public class CRMDriverException : Exception
    {
        public CRMDriverException(string message) : base(message) { }
        public CRMDriverException(string message, Exception innerException) : base(message, innerException) { }
    }
    #endregion
}
