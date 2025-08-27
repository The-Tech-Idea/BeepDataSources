using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using Salesforce.Common;
using Salesforce.Force;
using Microsoft.Identity.Client;

namespace TheTechIdea.Beep.Connectors.Salesforce
{
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Salesforce)]
    public class SalesforceDataSource : IDataSource
    {
        #region "Properties"
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public DataSourceType DatasourceType { get; set; } = DataSourceType.Salesforce;
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

        // Salesforce-specific properties
        private ForceClient _forceClient;
        private AuthenticationClient _authClient;
        private SalesforceConfig _config;
        #endregion

        #region "Constructor"
        public SalesforceDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType pDatasourceType, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = pDatasourceType;

            // Initialize Salesforce configuration from connection properties
            InitializeConfiguration();
        }
        #endregion

        #region "Configuration"
        private void InitializeConfiguration()
        {
            _config = new SalesforceConfig();

            // Get configuration from DMEEditor connection properties
            if (DMEEditor?.ConfigEditor?.DataConnections != null)
            {
                var connection = DMEEditor.ConfigEditor.DataConnections
                    .Find(c => c.ConnectionName.Equals(DatasourceName, StringComparison.InvariantCultureIgnoreCase));

                if (connection != null)
                {
                    _config.ConsumerKey = connection.UserID;
                    _config.ConsumerSecret = connection.Password;
                    _config.Username = connection.Database;
                    _config.Password = connection.SchemaName;
                    _config.TokenRequestEndpointUrl = connection.ConnectionString;
                    _config.InstanceUrl = connection.Url;
                }
            }
        }
        #endregion

        #region "Connection Methods"
        public ConnectionState Openconnection()
        {
            try
            {
                Logger.WriteLog("Opening connection to Salesforce");
                ConnectAsync().Wait();
                ConnectionStatus = ConnectionState.Open;
                Logger.WriteLog("Successfully connected to Salesforce");
                return ConnectionStatus;
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Failed to connect to Salesforce: {ex.Message}");
                ErrorObject.AddError("Salesforce Connection", ex.Message);
                ConnectionStatus = ConnectionState.Broken;
                return ConnectionStatus;
            }
        }

        public ConnectionState Closeconnection()
        {
            try
            {
                Logger.WriteLog("Closing connection to Salesforce");
                DisconnectAsync().Wait();
                ConnectionStatus = ConnectionState.Closed;
                Logger.WriteLog("Successfully disconnected from Salesforce");
                return ConnectionStatus;
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error closing Salesforce connection: {ex.Message}");
                ConnectionStatus = ConnectionState.Broken;
                return ConnectionStatus;
            }
        }

        private async Task<bool> ConnectAsync()
        {
            try
            {
                _authClient = new AuthenticationClient();
                await _authClient.UsernamePasswordAsync(
                    _config.ConsumerKey,
                    _config.ConsumerSecret,
                    _config.Username,
                    _config.Password,
                    _config.TokenRequestEndpointUrl
                );

                _forceClient = new ForceClient(
                    _config.InstanceUrl,
                    _authClient.AccessToken,
                    _config.ApiVersion
                );

                // Load entity names
                await LoadEntityNamesAsync();

                return true;
            }
            catch (Exception ex)
            {
                throw new CRMDriverException("Failed to connect to Salesforce", ex);
            }
        }

        private async Task DisconnectAsync()
        {
            _forceClient = null;
            _authClient = null;
        }

        private async Task LoadEntityNamesAsync()
        {
            try
            {
                var result = await _forceClient.DescribeGlobalAsync();
                EntitiesNames.Clear();
                EntitiesNames.AddRange(result.SObjects.Select(s => s.Name));
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
            Logger.WriteLog("Creating entities in Salesforce");
            // Implementation for creating entities
            return ErrorObject;
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            Logger.WriteLog($"Creating entity: {entity.EntityName}");
            // Implementation for creating entity structure
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

                // Convert to PagedResult
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
                ErrorObject.AddError("Salesforce GetEntity", ex.Message);
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
                ErrorObject.AddError("Salesforce GetEntity", ex.Message);
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
                ErrorObject.AddError("Salesforce GetEntityStructure", ex.Message);
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
                ErrorObject.AddError("Salesforce InsertEntity", ex.Message);
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
                ErrorObject.AddError("Salesforce UpdateEntity", ex.Message);
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
                ErrorObject.AddError("Salesforce DeleteEntity", ex.Message);
                return ErrorObject;
            }
        }
        #endregion

        #region "Async Data Operations"
        private async Task<IEnumerable<dynamic>> GetEntityAsync(string entityName, List<AppFilter> filters = null)
        {
            if (_forceClient == null)
                throw new InvalidOperationException("Not connected to Salesforce");

            try
            {
                var query = $"SELECT * FROM {entityName}";
                if (filters != null && filters.Count > 0)
                {
                    var whereClause = BuildWhereClause(filters);
                    query += $" WHERE {whereClause}";
                }

                var results = await _forceClient.QueryAsync<dynamic>(query);
                return results.Records;
            }
            catch (Exception ex)
            {
                throw new CRMDriverException($"Failed to retrieve {entityName} entities", ex);
            }
        }

        private async Task<EntityStructure> GetEntityStructureAsync(string entityName)
        {
            if (_forceClient == null)
                throw new InvalidOperationException("Not connected to Salesforce");

            try
            {
                var metadata = await _forceClient.DescribeAsync<dynamic>(entityName);
                return ConvertToEntityStructure(metadata);
            }
            catch (Exception ex)
            {
                throw new CRMDriverException($"Failed to retrieve metadata for {entityName}", ex);
            }
        }

        private async Task<dynamic> InsertEntityAsync(string entityName, object data)
        {
            if (_forceClient == null)
                throw new InvalidOperationException("Not connected to Salesforce");

            try
            {
                var result = await _forceClient.CreateAsync(entityName, data);
                return result;
            }
            catch (Exception ex)
            {
                throw new CRMDriverException($"Failed to create {entityName} entity", ex);
            }
        }

        private async Task<bool> UpdateEntityAsync(string entityName, object data)
        {
            if (_forceClient == null)
                throw new InvalidOperationException("Not connected to Salesforce");

            try
            {
                // Extract ID from data and update
                var dataDict = data as Dictionary<string, object>;
                if (dataDict != null && dataDict.ContainsKey("Id"))
                {
                    var entityId = dataDict["Id"].ToString();
                    dataDict.Remove("Id");
                    var success = await _forceClient.UpdateAsync(entityName, entityId, dataDict);
                    return success;
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
            if (_forceClient == null)
                throw new InvalidOperationException("Not connected to Salesforce");

            try
            {
                // Extract ID from data
                var dataDict = data as Dictionary<string, object>;
                if (dataDict != null && dataDict.ContainsKey("Id"))
                {
                    var entityId = dataDict["Id"].ToString();
                    var success = await _forceClient.DeleteAsync(entityName, entityId);
                    return success;
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
        private string BuildWhereClause(List<AppFilter> filters)
        {
            var conditions = new List<string>();
            foreach (var filter in filters)
            {
                if (filter.FilterValue is string)
                    conditions.Add($"{filter.FieldName} = '{filter.FilterValue}'");
                else
                    conditions.Add($"{filter.FieldName} = {filter.FilterValue}");
            }
            return string.Join(" AND ", conditions);
        }

        private EntityStructure ConvertToEntityStructure(dynamic metadata)
        {
            var entityStructure = new EntityStructure
            {
                EntityName = metadata.Name,
                Caption = metadata.Label
            };

            if (metadata.Fields != null)
            {
                foreach (var field in metadata.Fields)
                {
                    entityStructure.Fields.Add(new EntityField
                    {
                        FieldName = field.Name,
                        FieldType = MapSalesforceTypeToSystemType(field.Type),
                        Caption = field.Label,
                        IsKey = field.Name == "Id",
                        AllowNull = !field.Nillable,
                        IsAutoIncrement = false
                    });
                }
            }

            return entityStructure;
        }

        private Type MapSalesforceTypeToSystemType(string salesforceType)
        {
            return salesforceType.ToLower() switch
            {
                "string" => typeof(string),
                "int" => typeof(int),
                "double" => typeof(double),
                "boolean" => typeof(bool),
                "date" => typeof(DateTime),
                "datetime" => typeof(DateTime),
                _ => typeof(string)
            };
        }
        #endregion

        #region "Standard Interface Methods"
        public IErrorsInfo ExecuteSql(string sql)
        {
            try
            {
                Logger.WriteLog("Executing SQL command");
                var task = ExecuteSqlAsync(sql);
                task.Wait();
                return ErrorObject;
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error executing SQL: {ex.Message}");
                ErrorObject.AddError("Salesforce ExecuteSql", ex.Message);
                return ErrorObject;
            }
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
            try
            {
                Logger.WriteLog("Getting scalar value");
                var task = GetScalarAsync(query);
                task.Wait();
                return task.Result;
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error getting scalar: {ex.Message}");
                return 0.0;
            }
        }

        public Task<double> GetScalarAsync(string query)
        {
            return Task.FromResult(0.0);
        }

        public IBindingList RunQuery(string qrystr)
        {
            try
            {
                Logger.WriteLog("Running query");
                var task = RunQueryAsync(qrystr);
                task.Wait();
                return task.Result;
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error running query: {ex.Message}");
                ErrorObject.AddError("Salesforce RunQuery", ex.Message);
                return new BindingList<object>();
            }
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

        #region "Async Helper Methods"
        private async Task<IBindingList> RunQueryAsync(string qrystr)
        {
            if (_forceClient == null)
                throw new InvalidOperationException("Not connected to Salesforce");

            try
            {
                var results = await _forceClient.QueryAsync<dynamic>(qrystr);
                return new BindingList<object>(results.Records.Cast<object>().ToList());
            }
            catch (Exception ex)
            {
                throw new CRMDriverException("Failed to run query", ex);
            }
        }

        private async Task ExecuteSqlAsync(string sql)
        {
            if (_forceClient == null)
                throw new InvalidOperationException("Not connected to Salesforce");

            try
            {
                await _forceClient.QueryAsync<dynamic>(sql);
            }
            catch (Exception ex)
            {
                throw new CRMDriverException("Failed to execute SQL", ex);
            }
        }
        #endregion
    }

    #region "Configuration and Helper Classes"
    public class SalesforceConfig
    {
        public string ConsumerKey { get; set; }
        public string ConsumerSecret { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string TokenRequestEndpointUrl { get; set; }
        public string InstanceUrl { get; set; }
        public string ApiVersion { get; set; } = "v59.0";
    }

    public class CRMDriverException : Exception
    {
        public CRMDriverException(string message) : base(message) { }
        public CRMDriverException(string message, Exception innerException) : base(message, innerException) { }
    }
    #endregion
}
