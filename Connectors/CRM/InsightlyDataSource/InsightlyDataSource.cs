using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Http;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.WebAPI;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Connectors.InsightlyDataSource.Models;

namespace TheTechIdea.Beep.Connectors.InsightlyDataSource
{
    /// <summary>
    /// Insightly CRM Data Source implementation using Insightly REST API
    /// </summary>
    public class InsightlyDataSource : WebAPIDataSource
    {
        #region Configuration Classes

        /// <summary>
        /// Configuration for Insightly connection
        /// </summary>
        public class InsightlyConfig
        {
            public string ApiKey { get; set; } = string.Empty;
            public string BaseUrl { get; set; } = "https://api.insightly.com/v3.1";
        }

        #endregion

        #region Private Fields

        private readonly InsightlyConfig _config;
        private HttpClient? _httpClient;
        private ConnectionState _connectionState = ConnectionState.Closed;
        private string _connectionString = string.Empty;

        #endregion

        #region Constructor

        public InsightlyDataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor editor,
            DataSourceType type,
            IErrorsInfo errors)
            : base(datasourcename, logger, editor, type, errors)
        {
            _config = new InsightlyConfig();

            // Initialize connection properties
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
            {
                if (Dataconnection != null)
                    Dataconnection.ConnectionProp = new WebAPIConnectionProperties();
            }

            // Initialize HTTP client
            var handler = new HttpClientHandler();
            _httpClient = new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "BeepDataConnector/1.0");

            // Register entities
            RegisterEntities();
        }

        #endregion

        #region Entity Registration

        private void RegisterEntities()
        {
            EntitiesNames = new List<string>
            {
                "Contacts",
                "Organisations",
                "Opportunities",
                "Leads"
            };

            Entities = new List<EntityStructure>
            {
                new EntityStructure
                {
                    EntityName = "Contacts",
                    Caption = "Contacts",
                    Fields = new List<EntityField>
                    {
                        new EntityField { fieldname = "CONTACT_ID", fieldtype = "Int32" },
                        new EntityField { fieldname = "FIRST_NAME", fieldtype = "String" },
                        new EntityField { fieldname = "LAST_NAME", fieldtype = "String" },
                        new EntityField { fieldname = "SALUTATION", fieldtype = "String" },
                        new EntityField { fieldname = "DATE_CREATED_UTC", fieldtype = "DateTime" },
                        new EntityField { fieldname = "DATE_UPDATED_UTC", fieldtype = "DateTime" },
                        new EntityField { fieldname = "EMAIL_ADDRESS", fieldtype = "String" },
                        new EntityField { fieldname = "PHONE", fieldtype = "String" },
                        new EntityField { fieldname = "MOBILE", fieldtype = "String" },
                        new EntityField { fieldname = "ORGANISATION_ID", fieldtype = "Int32" }
                    }
                },
                new EntityStructure
                {
                    EntityName = "Organisations",
                    Caption = "Organisations",
                    Fields = new List<EntityField>
                    {
                        new EntityField { fieldname = "ORGANISATION_ID", fieldtype = "Int32" },
                        new EntityField { fieldname = "ORGANISATION_NAME", fieldtype = "String" },
                        new EntityField { fieldname = "DATE_CREATED_UTC", fieldtype = "DateTime" },
                        new EntityField { fieldname = "DATE_UPDATED_UTC", fieldtype = "DateTime" },
                        new EntityField { fieldname = "PHONE", fieldtype = "String" },
                        new EntityField { fieldname = "FAX", fieldtype = "String" },
                        new EntityField { fieldname = "WEBSITE", fieldtype = "String" },
                        new EntityField { fieldname = "ADDRESS_BILLING_STREET", fieldtype = "String" },
                        new EntityField { fieldname = "ADDRESS_BILLING_CITY", fieldtype = "String" },
                        new EntityField { fieldname = "ADDRESS_BILLING_STATE", fieldtype = "String" },
                        new EntityField { fieldname = "ADDRESS_BILLING_COUNTRY", fieldtype = "String" }
                    }
                },
                new EntityStructure
                {
                    EntityName = "Opportunities",
                    Caption = "Opportunities",
                    Fields = new List<EntityField>
                    {
                        new EntityField { fieldname = "OPPORTUNITY_ID", fieldtype = "Int32" },
                        new EntityField { fieldname = "OPPORTUNITY_NAME", fieldtype = "String" },
                        new EntityField { fieldname = "OPPORTUNITY_DETAILS", fieldtype = "String" },
                        new EntityField { fieldname = "PROBABILITY", fieldtype = "Decimal" },
                        new EntityField { fieldname = "BID_AMOUNT", fieldtype = "Decimal" },
                        new EntityField { fieldname = "BID_CURRENCY", fieldtype = "String" },
                        new EntityField { fieldname = "DATE_CREATED_UTC", fieldtype = "DateTime" },
                        new EntityField { fieldname = "DATE_UPDATED_UTC", fieldtype = "DateTime" },
                        new EntityField { fieldname = "ORGANISATION_ID", fieldtype = "Int32" },
                        new EntityField { fieldname = "CONTACT_ID", fieldtype = "Int32" }
                    }
                },
                new EntityStructure
                {
                    EntityName = "Leads",
                    Caption = "Leads",
                    Fields = new List<EntityField>
                    {
                        new EntityField { fieldname = "LEAD_ID", fieldtype = "Int32" },
                        new EntityField { fieldname = "FIRST_NAME", fieldtype = "String" },
                        new EntityField { fieldname = "LAST_NAME", fieldtype = "String" },
                        new EntityField { fieldname = "ORGANISATION_NAME", fieldtype = "String" },
                        new EntityField { fieldname = "PHONE_NUMBER", fieldtype = "String" },
                        new EntityField { fieldname = "EMAIL_ADDRESS", fieldtype = "String" },
                        new EntityField { fieldname = "DATE_CREATED_UTC", fieldtype = "DateTime" },
                        new EntityField { fieldname = "DATE_UPDATED_UTC", fieldtype = "DateTime" },
                        new EntityField { fieldname = "LEAD_STATUS_ID", fieldtype = "Int32" },
                        new EntityField { fieldname = "LEAD_SOURCE_ID", fieldtype = "Int32" }
                    }
                }
            };
        }

        #endregion

        #region IDataSource Implementation

        public async Task<bool> ConnectAsync()
        {
            try
            {
                Logger.WriteLog($"Connecting to Insightly: {_config.BaseUrl}");

                // Set authorization header with API key
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{_config.ApiKey}:")));

                // Test connection by getting contacts
                var testResponse = await _httpClient.GetAsync($"{_config.BaseUrl}/Contacts");
                if (testResponse.IsSuccessStatusCode)
                {
                    ConnectionStatus = ConnectionState.Open;
                    Logger.WriteLog("Successfully connected to Insightly");
                    return true;
                }

                var errorContent = await testResponse.Content.ReadAsStringAsync();
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Connection test failed: {testResponse.StatusCode} - {errorContent}";
                return false;
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Connection failed: {ex.Message}";
                Logger.WriteLog($"Insightly connection error: {ex.Message}");
                return false;
            }
        }

        public Task<bool> DisconnectAsync()
        {
            try
            {
                _httpClient?.Dispose();
                _httpClient = null;
                ConnectionStatus = ConnectionState.Closed;
                Logger.WriteLog("Disconnected from Insightly");
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Disconnect failed: {ex.Message}";
                return Task.FromResult(false);
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

        public Task<List<string>> GetEntitiesNamesAsync()
        {
            return Task.FromResult(EntitiesNames);
        }

        public Task<List<EntityStructure>> GetEntityStructuresAsync(bool refresh = false)
        {
            return Task.FromResult(Entities);
        }

        // Return the fixed list
        public new IEnumerable<string> GetEntitesList() => EntitiesNames;

        // Sync
        public override IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            var data = GetEntityAsync(EntityName, filter).ConfigureAwait(false).GetAwaiter().GetResult();
            return data ?? Array.Empty<object>();
        }

        // Async
        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            try
            {
                if (_httpClient == null)
                    throw new InvalidOperationException("Not connected to Insightly");

                var queryParams = BuildQueryParameters(Filter);
                var url = $"{_config.BaseUrl}/{EntityName}{queryParams}";

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return ParseResponse(content, EntityName);
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Failed to get {EntityName}: {response.StatusCode} - {errorContent}";
                return Array.Empty<object>();
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Failed to get entity data: {ex.Message}";
                return Array.Empty<object>();
            }
        }

        // Paged
        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            var items = GetEntity(EntityName, filter).ToList();
            var totalRecords = items.Count;
            var pagedItems = items.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            return new PagedResult
            {
                Data = pagedItems,
                PageNumber = Math.Max(1, pageNumber),
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                HasPreviousPage = pageNumber > 1,
                HasNextPage = pageNumber * pageSize < totalRecords
            };
        }

        #endregion

        #region Response Parsing

        private IEnumerable<object> ParseResponse(string jsonContent, string entityName)
        {
            try
            {
                return entityName switch
                {
                    "Contacts" => ExtractArray<Contact>(jsonContent).Cast<object>(),
                    "Organisations" => ExtractArray<Organisation>(jsonContent).Cast<object>(),
                    "Opportunities" => ExtractArray<Opportunity>(jsonContent).Cast<object>(),
                    "Leads" => ExtractArray<Lead>(jsonContent).Cast<object>(),
                    _ => new List<object> { JsonSerializer.Deserialize<JsonElement>(jsonContent) }
                };
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error parsing response for {entityName}: {ex.Message}");
                return Array.Empty<object>();
            }
        }

        private List<T> ExtractArray<T>(string jsonContent) where T : InsightlyEntityBase
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var result = JsonSerializer.Deserialize<List<T>>(jsonContent, options);
                if (result != null)
                {
                    foreach (var item in result)
                    {
                        item.Attach(this);
                    }
                }
                return result ?? new List<T>();
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error extracting array of {typeof(T).Name}: {ex.Message}");
                return new List<T>();
            }
        }

        #endregion

        #region Command Methods

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Insightly, PointType = EnumPointType.Function, ObjectType = "Contact", ClassName = "InsightlyDataSource", Showin = ShowinType.Both, misc = "List<Contact>")]
        public async Task<List<Contact>> GetContactsAsync(List<AppFilter>? filter = null)
        {
            var result = await GetEntityAsync("Contacts", filter);
            return result as List<Contact> ?? new List<Contact>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Insightly, PointType = EnumPointType.Function, ObjectType = "Organisation", ClassName = "InsightlyDataSource", Showin = ShowinType.Both, misc = "List<Organisation>")]
        public async Task<List<Organisation>> GetOrganisationsAsync(List<AppFilter>? filter = null)
        {
            var result = await GetEntityAsync("Organisations", filter);
            return result as List<Organisation> ?? new List<Organisation>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Insightly, PointType = EnumPointType.Function, ObjectType = "Opportunity", ClassName = "InsightlyDataSource", Showin = ShowinType.Both, misc = "List<Opportunity>")]
        public async Task<List<Opportunity>> GetOpportunitiesAsync(List<AppFilter>? filter = null)
        {
            var result = await GetEntityAsync("Opportunities", filter);
            return result as List<Opportunity> ?? new List<Opportunity>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Insightly, PointType = EnumPointType.Function, ObjectType = "Lead", ClassName = "InsightlyDataSource", Showin = ShowinType.Both, misc = "List<Lead>")]
        public async Task<List<Lead>> GetLeadsAsync(List<AppFilter>? filter = null)
        {
            var result = await GetEntityAsync("Leads", filter);
            return result as List<Lead> ?? new List<Lead>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Insightly, PointType = EnumPointType.Function, ObjectType = "Contact", ClassName = "InsightlyDataSource", Showin = ShowinType.Both, misc = "Contact")]
        public async Task<List<Contact>> CreateContactAsync(Contact contact)
        {
            if (contact == null) return new List<Contact>();
            using var resp = await PostAsync("Contacts", contact).ConfigureAwait(false);
            if (resp == null || !resp.IsSuccessStatusCode) return new List<Contact>();
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {
                var result = JsonSerializer.Deserialize<Contact>(json, opts);
                return result != null ? new List<Contact> { result } : new List<Contact>();
            }
            catch
            {
                return new List<Contact>();
            }
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Insightly, PointType = EnumPointType.Function, ObjectType = "Contact", ClassName = "InsightlyDataSource", Showin = ShowinType.Both, misc = "Contact")]
        public async Task<List<Contact>> UpdateContactAsync(int contactId, Contact contact)
        {
            if (contactId <= 0 || contact == null) return new List<Contact>();
            var endpoint = $"Contacts/{contactId}";
            using var resp = await PutAsync(endpoint, contact).ConfigureAwait(false);
            if (resp == null || !resp.IsSuccessStatusCode) return new List<Contact>();
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {
                var result = JsonSerializer.Deserialize<Contact>(json, opts);
                return result != null ? new List<Contact> { result } : new List<Contact>();
            }
            catch
            {
                return new List<Contact>();
            }
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Insightly, PointType = EnumPointType.Function, ObjectType = "Organisation", ClassName = "InsightlyDataSource", Showin = ShowinType.Both, misc = "Organisation")]
        public async Task<List<Organisation>> CreateOrganisationAsync(Organisation organisation)
        {
            if (organisation == null) return new List<Organisation>();
            using var resp = await PostAsync("Organisations", organisation).ConfigureAwait(false);
            if (resp == null || !resp.IsSuccessStatusCode) return new List<Organisation>();
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {
                var result = JsonSerializer.Deserialize<Organisation>(json, opts);
                return result != null ? new List<Organisation> { result } : new List<Organisation>();
            }
            catch
            {
                return new List<Organisation>();
            }
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Insightly, PointType = EnumPointType.Function, ObjectType = "Organisation", ClassName = "InsightlyDataSource", Showin = ShowinType.Both, misc = "Organisation")]
        public async Task<List<Organisation>> UpdateOrganisationAsync(int organisationId, Organisation organisation)
        {
            if (organisationId <= 0 || organisation == null) return new List<Organisation>();
            var endpoint = $"Organisations/{organisationId}";
            using var resp = await PutAsync(endpoint, organisation).ConfigureAwait(false);
            if (resp == null || !resp.IsSuccessStatusCode) return new List<Organisation>();
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {
                var result = JsonSerializer.Deserialize<Organisation>(json, opts);
                return result != null ? new List<Organisation> { result } : new List<Organisation>();
            }
            catch
            {
                return new List<Organisation>();
            }
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Insightly, PointType = EnumPointType.Function, ObjectType = "Opportunity", ClassName = "InsightlyDataSource", Showin = ShowinType.Both, misc = "Opportunity")]
        public async Task<List<Opportunity>> CreateOpportunityAsync(Opportunity opportunity)
        {
            if (opportunity == null) return new List<Opportunity>();
            using var resp = await PostAsync("Opportunities", opportunity).ConfigureAwait(false);
            if (resp == null || !resp.IsSuccessStatusCode) return new List<Opportunity>();
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {
                var result = JsonSerializer.Deserialize<Opportunity>(json, opts);
                return result != null ? new List<Opportunity> { result } : new List<Opportunity>();
            }
            catch
            {
                return new List<Opportunity>();
            }
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Insightly, PointType = EnumPointType.Function, ObjectType = "Opportunity", ClassName = "InsightlyDataSource", Showin = ShowinType.Both, misc = "Opportunity")]
        public async Task<List<Opportunity>> UpdateOpportunityAsync(int opportunityId, Opportunity opportunity)
        {
            if (opportunityId <= 0 || opportunity == null) return new List<Opportunity>();
            var endpoint = $"Opportunities/{opportunityId}";
            using var resp = await PutAsync(endpoint, opportunity).ConfigureAwait(false);
            if (resp == null || !resp.IsSuccessStatusCode) return new List<Opportunity>();
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {
                var result = JsonSerializer.Deserialize<Opportunity>(json, opts);
                return result != null ? new List<Opportunity> { result } : new List<Opportunity>();
            }
            catch
            {
                return new List<Opportunity>();
            }
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Insightly, PointType = EnumPointType.Function, ObjectType = "Lead", ClassName = "InsightlyDataSource", Showin = ShowinType.Both, misc = "Lead")]
        public async Task<List<Lead>> CreateLeadAsync(Lead lead)
        {
            if (lead == null) return new List<Lead>();
            using var resp = await PostAsync("Leads", lead).ConfigureAwait(false);
            if (resp == null || !resp.IsSuccessStatusCode) return new List<Lead>();
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {
                var result = JsonSerializer.Deserialize<Lead>(json, opts);
                return result != null ? new List<Lead> { result } : new List<Lead>();
            }
            catch
            {
                return new List<Lead>();
            }
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Insightly, PointType = EnumPointType.Function, ObjectType = "Lead", ClassName = "InsightlyDataSource", Showin = ShowinType.Both, misc = "Lead")]
        public async Task<List<Lead>> UpdateLeadAsync(int leadId, Lead lead)
        {
            if (leadId <= 0 || lead == null) return new List<Lead>();
            var endpoint = $"Leads/{leadId}";
            using var resp = await PutAsync(endpoint, lead).ConfigureAwait(false);
            if (resp == null || !resp.IsSuccessStatusCode) return new List<Lead>();
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {
                var result = JsonSerializer.Deserialize<Lead>(json, opts);
                return result != null ? new List<Lead> { result } : new List<Lead>();
            }
            catch
            {
                return new List<Lead>();
            }
        }

        #endregion

        #region Private Methods

        private void ParseConnectionString()
        {
            if (string.IsNullOrEmpty(_connectionString))
                return;

            // Parse connection string format: ApiKey=xxx;BaseUrl=xxx
            var parts = _connectionString.Split(";");
            foreach (var part in parts)
            {
                var keyValue = part.Split("=");
                if (keyValue.Length == 2)
                {
                    var key = keyValue[0].Trim();
                    var value = keyValue[1].Trim();

                    switch (key.ToLower())
                    {
                        case "apikey":
                            _config.ApiKey = value;
                            break;
                        case "baseurl":
                            _config.BaseUrl = value;
                            break;
                    }
                }
            }
        }

        private string BuildQueryParameters(List<AppFilter>? filters)
        {
            if (filters == null || !filters.Any())
                return string.Empty;

            var queryParams = new List<string>();
            foreach (var filter in filters)
            {
                if (!string.IsNullOrEmpty(filter.FieldName) && !string.IsNullOrEmpty(filter.FilterValue))
                {
                    queryParams.Add($"{filter.FieldName}={Uri.EscapeDataString(filter.FilterValue)}");
                }
            }

            return queryParams.Any() ? "?" + string.Join("&", queryParams) : string.Empty;
        }

        #endregion
    }
}
