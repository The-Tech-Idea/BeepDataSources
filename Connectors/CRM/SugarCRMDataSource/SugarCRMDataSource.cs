using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Http;
using TheTechIdea.Beep.AppManager;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.WebAPI;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Connectors.SugarCRM.Models;

namespace TheTechIdea.Beep.Connectors.SugarCRM
{
    /// <summary>
    /// SugarCRM Data Source implementation using SugarCRM REST API v11
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.SugarCRM)]
    public class SugarCRMDataSource : WebAPIDataSource
    {
        // Entity endpoints mapping for SugarCRM API
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            ["contacts"] = "Contacts",
            ["accounts"] = "Accounts",
            ["leads"] = "Leads",
            ["opportunities"] = "Opportunities"
        };

        private readonly Dictionary<string, Type> _entityCache = new(StringComparer.OrdinalIgnoreCase);
        private HttpClient? _httpClient;

        public SugarCRMDataSource(string datasourcename, IDMLogger logger, IDMEEditor dmeEditor, DataSourceType databasetype, IErrorsInfo errorObject) : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
        {
            // Initialize HTTP client
            var handler = new HttpClientHandler();
            _httpClient = new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "BeepDataConnector/1.0");

            // Register entities
            EntitiesNames.Add("Contacts");
            EntitiesNames.Add("Accounts");
            EntitiesNames.Add("Leads");
            EntitiesNames.Add("Opportunities");

            Entities.Add(new EntityStructure { EntityName = "Contacts", DatasourceEntityName = "Contacts" });
            Entities.Add(new EntityStructure { EntityName = "Accounts", DatasourceEntityName = "Accounts" });
            Entities.Add(new EntityStructure { EntityName = "Leads", DatasourceEntityName = "Leads" });
            Entities.Add(new EntityStructure { EntityName = "Opportunities", DatasourceEntityName = "Opportunities" });

            _entityCache["contacts"] = typeof(Contact);
            _entityCache["accounts"] = typeof(Account);
            _entityCache["leads"] = typeof(Lead);
            _entityCache["opportunities"] = typeof(Opportunity);
        }

        #region Entity Methods

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
                if (!EntityEndpoints.TryGetValue(EntityName.ToLower(), out var endpoint))
                {
                    Logger.WriteLog($"Unknown entity: {EntityName}");
                    return Array.Empty<object>();
                }

                var baseUrl = Dataconnection?.ConnectionProp?.ConnectionString ?? "https://your-instance.sugarondemand.com/rest/v11";
                var url = $"{baseUrl}/{endpoint}";

                var request = new HttpRequestMessage(HttpMethod.Get, url);

                // Add authentication headers if available
                if (Dataconnection?.ConnectionProp is WebAPIConnectionProperties webApiProps && !string.IsNullOrEmpty(webApiProps.ApiKey))
                {
                    request.Headers.Add("OAuth-Token", webApiProps.ApiKey);
                }

                if (_httpClient == null)
                {
                    Logger.WriteLog("HTTP client not initialized");
                    return Array.Empty<object>();
                }

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    Logger.WriteLog($"API request failed: {response.StatusCode} - {response.ReasonPhrase}");
                    return Array.Empty<object>();
                }

                var jsonContent = await response.Content.ReadAsStringAsync();
                var result = ParseResponse(jsonContent, EntityName.ToLower());
                return result ?? Array.Empty<object>();
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error in GetEntityAsync for {EntityName}: {ex.Message}");
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
                    "contacts" => ExtractArray<Contact>(jsonContent),
                    "accounts" => ExtractArray<Account>(jsonContent),
                    "leads" => ExtractArray<Lead>(jsonContent),
                    "opportunities" => ExtractArray<Opportunity>(jsonContent),
                    _ => JsonSerializer.Deserialize<SugarCRMApiResponse<JsonElement>>(jsonContent)?.Records.EnumerateArray().Select(x => (object)x) ?? new List<object>()
                };
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error parsing response for {entityName}: {ex.Message}");
                return JsonSerializer.Deserialize<SugarCRMApiResponse<JsonElement>>(jsonContent)?.Records.EnumerateArray().Select(x => (object)x) ?? new List<object>();
            }
        }

        private List<T> ExtractArray<T>(string jsonContent) where T : SugarCRMEntityBase
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var apiResponse = JsonSerializer.Deserialize<SugarCRMApiResponse<List<T>>>(jsonContent, options);
                if (apiResponse?.Records != null)
                {
                    foreach (var item in apiResponse.Records)
                    {
                        item.Attach<T>((IDataSource)this);
                    }
                }
                return apiResponse?.Records ?? new List<T>();
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error extracting array of {typeof(T).Name}: {ex.Message}");
                return new List<T>();
            }
        }

        #endregion

        #region Command Methods

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.SugarCRM, PointType = EnumPointType.Function, ObjectType = "Contacts", ClassName = "SugarCRMDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<Contact>")]
        public IEnumerable<Contact> GetContacts(List<AppFilter> filter) => GetEntity("contacts", filter).Cast<Contact>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.SugarCRM, PointType = EnumPointType.Function, ObjectType = "Accounts", ClassName = "SugarCRMDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<Account>")]
        public IEnumerable<Account> GetAccounts(List<AppFilter> filter) => GetEntity("accounts", filter).Cast<Account>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.SugarCRM, PointType = EnumPointType.Function, ObjectType = "Leads", ClassName = "SugarCRMDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<Lead>")]
        public IEnumerable<Lead> GetLeads(List<AppFilter> filter) => GetEntity("leads", filter).Cast<Lead>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.SugarCRM, PointType = EnumPointType.Function, ObjectType = "Opportunities", ClassName = "SugarCRMDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<Opportunity>")]
        public IEnumerable<Opportunity> GetOpportunities(List<AppFilter> filter) => GetEntity("opportunities", filter).Cast<Opportunity>();

        #endregion

        #region Configuration Classes

        private class SugarCRMApiResponse<T>
        {
            [JsonPropertyName("records")]
            public T? Records { get; set; }

            [JsonPropertyName("next_offset")]
            public int? NextOffset { get; set; }

            [JsonPropertyName("total_count")]
            public int? TotalCount { get; set; }
        }

        #endregion
    }
}
