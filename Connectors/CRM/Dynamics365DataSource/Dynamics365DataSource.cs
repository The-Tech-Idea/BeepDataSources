using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Client;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Connectors.Dynamics365DataSource
{
    /// <summary>
    /// Dynamics 365 CRM Data Source implementation using Microsoft Graph API
    /// </summary>
    public class Dynamics365DataSource : IDataSource
    {
        #region Configuration Classes

        /// <summary>
        /// Configuration for Dynamics 365 connection
        /// </summary>
        public class Dynamics365Config
        {
            public string TenantId { get; set; } = string.Empty;
            public string ClientId { get; set; } = string.Empty;
            public string ClientSecret { get; set; } = string.Empty;
            public string OrganizationUrl { get; set; } = string.Empty;
            public string Environment { get; set; } = "production"; // production, sandbox
            public bool UseInteractiveAuth { get; set; } = false;
            public string[] Scopes { get; set; } = new[] { "https://graph.microsoft.com/.default" };
        }

        /// <summary>
        /// Dynamics 365 entity metadata
        /// </summary>
        public class Dynamics365Entity
        {
            public string LogicalName { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public string PrimaryIdAttribute { get; set; } = string.Empty;
            public Dictionary<string, string> Attributes { get; set; } = new();
            public bool IsCustomEntity { get; set; }
        }

        #endregion

        #region Private Fields

        private readonly Dynamics365Config _config;
        private GraphServiceClient? _graphClient;
        private IConfidentialClientApplication? _confidentialClient;
        private readonly IDMEEditor _dmeEditor;
        private readonly IErrorsInfo _errorsInfo;
        private readonly IJsonLoader _jsonLoader;
        private readonly IDMLogger _logger;
        private readonly IUtil _util;
        private ConnectionState _connectionState = ConnectionState.Closed;
        private string _connectionString = string.Empty;
        private readonly Dictionary<string, Dynamics365Entity> _entityCache = new();

        #endregion

        #region Constructor

        public Dynamics365DataSource(string datasourcename, IDMEEditor dmeEditor, IDataConnection cn, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            _dmeEditor = dmeEditor;
            _errorsInfo = per;
            _jsonLoader = new JsonLoader();
            _logger = new DMLogger();
            _util = new Util();
            _config = new Dynamics365Config();

            // Initialize connection properties
            Dataconnection = cn;
            if (cn != null)
            {
                _connectionString = cn.ConnectionString;
                ParseConnectionString();
            }
        }

        #endregion

        #region IDataSource Implementation

        public string DatasourceName { get; set; } = string.Empty;
        public string DatasourceType { get; set; } = "Dynamics365";
        public DatasourceCategory Category { get; set; } = DatasourceCategory.CRM;
        public IDataConnection? Dataconnection { get; set; }
        public object? DatasourceConnection { get; set; }
        public ConnectionState ConnectionStatus => _connectionState;
        public bool InMemory { get; set; } = false;
        public List<string> EntitiesNames { get; set; } = new();
        public List<EntityStructure> Entities { get; set; } = new();
        public IDMLogger Logger => _logger;
        public IErrorsInfo ErrorObject => _errorsInfo;
        public IUtil util => _util;
        public IJsonLoader jsonLoader => _jsonLoader;
        public IDMEEditor DMEEditor => _dmeEditor;

        public async Task<bool> ConnectAsync()
        {
            try
            {
                _logger.WriteLog($"Connecting to Dynamics 365: {_config.OrganizationUrl}");

                if (_config.UseInteractiveAuth)
                {
                    // Interactive authentication (for desktop apps)
                    var options = new InteractiveBrowserCredentialOptions
                    {
                        TenantId = _config.TenantId,
                        ClientId = _config.ClientId,
                        RedirectUri = new Uri("http://localhost:8080")
                    };

                    var credential = new InteractiveBrowserCredential(options);
                    _graphClient = new GraphServiceClient(credential);
                }
                else
                {
                    // Client credentials flow (for server apps)
                    _confidentialClient = ConfidentialClientApplicationBuilder
                        .Create(_config.ClientId)
                        .WithClientSecret(_config.ClientSecret)
                        .WithAuthority(new Uri($"https://login.microsoftonline.com/{_config.TenantId}"))
                        .Build();

                    var authProvider = new ClientCredentialProvider(_confidentialClient);
                    _graphClient = new GraphServiceClient(authProvider);
                }

                // Test connection by getting organization info
                var organization = await _graphClient.Organization.GetAsync();
                if (organization != null)
                {
                    _connectionState = ConnectionState.Open;
                    DatasourceConnection = _graphClient;
                    _logger.WriteLog("Successfully connected to Dynamics 365");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("Dynamics365", $"Connection failed: {ex.Message}", ex);
                _logger.WriteLog($"Dynamics 365 connection error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DisconnectAsync()
        {
            try
            {
                _graphClient = null;
                _confidentialClient = null;
                _connectionState = ConnectionState.Closed;
                DatasourceConnection = null;
                _entityCache.Clear();
                _logger.WriteLog("Disconnected from Dynamics 365");
                return true;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("Dynamics365", $"Disconnect failed: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> OpenconnectionAsync()
        {
            return await ConnectAsync();
        }

        public async Task<bool> CloseconnectionAsync()
        {
            return await DisconnectAsync();
        }

        public async Task<List<string>> GetEntitiesNamesAsync()
        {
            try
            {
                if (_graphClient == null)
                    throw new InvalidOperationException("Not connected to Dynamics 365");

                // Get available entities from Dynamics 365 Web API
                var entities = await GetDynamics365EntitiesAsync();
                EntitiesNames = entities.Select(e => e.LogicalName).ToList();
                return EntitiesNames;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("Dynamics365", $"Failed to get entities: {ex.Message}", ex);
                return new List<string>();
            }
        }

        public async Task<List<EntityStructure>> GetEntityStructuresAsync(bool refresh = false)
        {
            try
            {
                if (_graphClient == null)
                    throw new InvalidOperationException("Not connected to Dynamics 365");

                if (!refresh && Entities.Any())
                    return Entities;

                var dynamicsEntities = await GetDynamics365EntitiesAsync();
                Entities = new List<EntityStructure>();

                foreach (var entity in dynamicsEntities)
                {
                    var structure = new EntityStructure
                    {
                        EntityName = entity.LogicalName,
                        DisplayName = entity.DisplayName,
                        SchemaName = "Dynamics365",
                        Fields = new List<EntityField>()
                    };

                    foreach (var attr in entity.Attributes)
                    {
                        structure.Fields.Add(new EntityField
                        {
                            fieldname = attr.Key,
                            fieldtype = attr.Value,
                            FieldDisplayName = attr.Key
                        });
                    }

                    Entities.Add(structure);
                }

                return Entities;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("Dynamics365", $"Failed to get entity structures: {ex.Message}", ex);
                return new List<EntityStructure>();
            }
        }

        public async Task<object?> GetEntityAsync(string entityName, List<AppFilter>? filter = null)
        {
            try
            {
                if (_graphClient == null)
                    throw new InvalidOperationException("Not connected to Dynamics 365");

                var queryOptions = BuildQueryOptions(filter);
                var requestUrl = $"/crm/v9.2/{entityName}s{queryOptions}";

                // Use Dynamics 365 Web API directly since Graph API doesn't cover all CRM entities
                var response = await _graphClient.HttpProvider.SendAsync(
                    new HttpRequestMessage(HttpMethod.Get, requestUrl));

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<JsonElement>(content);
                }

                return null;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("Dynamics365", $"Failed to get entity data: {ex.Message}", ex);
                return null;
            }
        }

        public async Task<bool> InsertEntityAsync(string entityName, object entityData)
        {
            try
            {
                if (_graphClient == null)
                    throw new InvalidOperationException("Not connected to Dynamics 365");

                var jsonData = JsonSerializer.Serialize(entityData);
                var requestUrl = $"/crm/v9.2/{entityName}s";

                var response = await _graphClient.HttpProvider.SendAsync(
                    new HttpRequestMessage(HttpMethod.Post, requestUrl)
                    {
                        Content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json")
                    });

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("Dynamics365", $"Failed to insert entity: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> UpdateEntityAsync(string entityName, object entityData, string entityId)
        {
            try
            {
                if (_graphClient == null)
                    throw new InvalidOperationException("Not connected to Dynamics 365");

                var jsonData = JsonSerializer.Serialize(entityData);
                var requestUrl = $"/crm/v9.2/{entityName}s({entityId})";

                var response = await _graphClient.HttpProvider.SendAsync(
                    new HttpRequestMessage(HttpMethod.Patch, requestUrl)
                    {
                        Content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json")
                    });

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("Dynamics365", $"Failed to update entity: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> DeleteEntityAsync(string entityName, string entityId)
        {
            try
            {
                if (_graphClient == null)
                    throw new InvalidOperationException("Not connected to Dynamics 365");

                var requestUrl = $"/crm/v9.2/{entityName}s({entityId})";

                var response = await _graphClient.HttpProvider.SendAsync(
                    new HttpRequestMessage(HttpMethod.Delete, requestUrl));

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("Dynamics365", $"Failed to delete entity: {ex.Message}", ex);
                return false;
            }
        }

        #endregion

        #region Private Methods

        private void ParseConnectionString()
        {
            if (string.IsNullOrEmpty(_connectionString))
                return;

            // Parse connection string format: TenantId=xxx;ClientId=xxx;ClientSecret=xxx;OrganizationUrl=xxx;Environment=xxx
            var parts = _connectionString.Split(';');
            foreach (var part in parts)
            {
                var keyValue = part.Split('=');
                if (keyValue.Length == 2)
                {
                    var key = keyValue[0].Trim();
                    var value = keyValue[1].Trim();

                    switch (key.ToLower())
                    {
                        case "tenantid":
                            _config.TenantId = value;
                            break;
                        case "clientid":
                            _config.ClientId = value;
                            break;
                        case "clientsecret":
                            _config.ClientSecret = value;
                            break;
                        case "organizationurl":
                            _config.OrganizationUrl = value;
                            break;
                        case "environment":
                            _config.Environment = value;
                            break;
                        case "useinteractiveauth":
                            _config.UseInteractiveAuth = bool.Parse(value);
                            break;
                    }
                }
            }
        }

        private async Task<List<Dynamics365Entity>> GetDynamics365EntitiesAsync()
        {
            if (_entityCache.Any())
                return _entityCache.Values.ToList();

            // Common Dynamics 365 entities
            var entities = new List<Dynamics365Entity>
            {
                new Dynamics365Entity
                {
                    LogicalName = "account",
                    DisplayName = "Account",
                    PrimaryIdAttribute = "accountid",
                    Attributes = new Dictionary<string, string>
                    {
                        ["accountid"] = "Guid",
                        ["name"] = "String",
                        ["address1_city"] = "String",
                        ["address1_country"] = "String",
                        ["telephone1"] = "String",
                        ["emailaddress1"] = "String",
                        ["websiteurl"] = "String"
                    }
                },
                new Dynamics365Entity
                {
                    LogicalName = "contact",
                    DisplayName = "Contact",
                    PrimaryIdAttribute = "contactid",
                    Attributes = new Dictionary<string, string>
                    {
                        ["contactid"] = "Guid",
                        ["firstname"] = "String",
                        ["lastname"] = "String",
                        ["fullname"] = "String",
                        ["emailaddress1"] = "String",
                        ["telephone1"] = "String",
                        ["mobilephone"] = "String"
                    }
                },
                new Dynamics365Entity
                {
                    LogicalName = "lead",
                    DisplayName = "Lead",
                    PrimaryIdAttribute = "leadid",
                    Attributes = new Dictionary<string, string>
                    {
                        ["leadid"] = "Guid",
                        ["firstname"] = "String",
                        ["lastname"] = "String",
                        ["companyname"] = "String",
                        ["emailaddress1"] = "String",
                        ["telephone1"] = "String"
                    }
                },
                new Dynamics365Entity
                {
                    LogicalName = "opportunity",
                    DisplayName = "Opportunity",
                    PrimaryIdAttribute = "opportunityid",
                    Attributes = new Dictionary<string, string>
                    {
                        ["opportunityid"] = "Guid",
                        ["name"] = "String",
                        ["customerid"] = "Guid",
                        ["estimatedvalue"] = "Decimal",
                        ["statuscode"] = "Int32"
                    }
                }
            };

            foreach (var entity in entities)
            {
                _entityCache[entity.LogicalName] = entity;
            }

            return entities;
        }

        private string BuildQueryOptions(List<AppFilter>? filters)
        {
            if (filters == null || !filters.Any())
                return string.Empty;

            var queryParts = new List<string>();

            foreach (var filter in filters)
            {
                if (!string.IsNullOrEmpty(filter.FilterValue))
                {
                    queryParts.Add($"{filter.FieldName} eq '{filter.FilterValue}'");
                }
            }

            return queryParts.Any() ? $"?$filter={string.Join(" and ", queryParts)}" : string.Empty;
        }

        #endregion

        #region Standard Interface Methods

        public bool CreateEntityAsAsync(string entityname, object entitydata)
        {
            return Task.Run(() => InsertEntityAsync(entityname, entitydata)).GetAwaiter().GetResult();
        }

        public bool UpdateEntity(string entityname, object entitydata, string entityid)
        {
            return Task.Run(() => UpdateEntityAsync(entityname, entitydata, entityid)).GetAwaiter().GetResult();
        }

        public bool DeleteEntity(string entityname, string entityid)
        {
            return Task.Run(() => DeleteEntityAsync(entityname, entityid)).GetAwaiter().GetResult();
        }

        public object GetEntity(string entityname, List<AppFilter> filter)
        {
            return Task.Run(() => GetEntityAsync(entityname, filter)).GetAwaiter().GetResult();
        }

        public List<EntityStructure> GetEntityStructures(bool refresh = false)
        {
            return Task.Run(() => GetEntityStructuresAsync(refresh)).GetAwaiter().GetResult();
        }

        public List<string> GetEntitesList()
        {
            return Task.Run(() => GetEntitiesNamesAsync()).GetAwaiter().GetResult();
        }

        public bool Openconnection()
        {
            return Task.Run(() => OpenconnectionAsync()).GetAwaiter().GetResult();
        }

        public bool Closeconnection()
        {
            return Task.Run(() => CloseconnectionAsync()).GetAwaiter().GetResult();
        }

        public bool CreateEntityAs(string entityname, object entitydata)
        {
            return CreateEntityAsAsync(entityname, entitydata);
        }

        public object RunQuery(string qrystr)
        {
            // Dynamics 365 doesn't support arbitrary SQL queries
            // This would need to be implemented using FetchXML or OData queries
            _errorsInfo.AddError("Dynamics365", "RunQuery not supported. Use GetEntity with filters instead.");
            return null;
        }

        public object RunScript(ETLScriptDet dDLScripts)
        {
            _errorsInfo.AddError("Dynamics365", "RunScript not supported for Dynamics 365");
            return null;
        }

        public void Dispose()
        {
            Task.Run(() => DisconnectAsync()).GetAwaiter().GetResult();
            _entityCache.Clear();
        }

        #endregion
    }

    /// <summary>
    /// Client credential provider for Microsoft Graph
    /// </summary>
    public class ClientCredentialProvider : IAuthenticationProvider
    {
        private readonly IConfidentialClientApplication _confidentialClient;

        public ClientCredentialProvider(IConfidentialClientApplication confidentialClient)
        {
            _confidentialClient = confidentialClient;
        }

        public async Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            var result = await _confidentialClient.AcquireTokenForClient(new[] { "https://graph.microsoft.com/.default" })
                .ExecuteAsync();

            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", result.AccessToken);
        }
    }
}
