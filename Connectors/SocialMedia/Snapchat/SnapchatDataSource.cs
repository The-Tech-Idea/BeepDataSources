using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.WebAPI;
using TheTechIdea.Beep.Connectors.Snapchat.Models;

namespace TheTechIdea.Beep.Connectors.Snapchat
{
    /// <summary>
    /// Snapchat configuration class
    /// </summary>
    public class SnapchatConfig
    {
        /// <summary>
        /// Client ID for Snapchat API
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Client Secret for Snapchat API
        /// </summary>
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>
        /// Access token for Snapchat API
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// Refresh token for Snapchat API
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// Organization ID for Snapchat API
        /// </summary>
        public string OrganizationId { get; set; } = string.Empty;

        /// <summary>
        /// Ad Account ID for Snapchat API
        /// </summary>
        public string AdAccountId { get; set; } = string.Empty;

        /// <summary>
        /// API version for Snapchat API
        /// </summary>
        public string ApiVersion { get; set; } = "v1";

        /// <summary>
        /// Base URL for Snapchat API
        /// </summary>
        public string BaseUrl { get; set; } = "https://adsapi.snapchat.com";

        /// <summary>
        /// Timeout in seconds for API calls
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Maximum number of retries for failed requests
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Rate limit delay in milliseconds between requests
        /// </summary>
        public int RateLimitDelayMs { get; set; } = 1000;
    }

    /// <summary>
    /// Snapchat data source implementation for Beep framework
    /// Supports Snapchat Marketing API
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Snapchat)]
    public class SnapchatDataSource : WebAPIDataSource
    {
        protected WebAPIConnectionProperties? ConnectionProperties => Dataconnection?.ConnectionProp as WebAPIConnectionProperties;

        private readonly SnapchatConfig _config = new();
        private readonly Dictionary<string, EntityStructure> _entities = new(StringComparer.OrdinalIgnoreCase);

        private string ApiBaseUrl
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(ConnectionProperties?.Url))
                    return ConnectionProperties.Url!.TrimEnd('/');

                return $"{_config.BaseUrl.TrimEnd('/')}/{_config.ApiVersion}".TrimEnd('/');
            }
        }

        private void HydrateConfigFromConnectionProperties()
        {
            if (ConnectionProperties is null) return;

            _config.AccessToken = ConnectionProperties.AccessToken
                                  ?? ConnectionProperties.BearerToken
                                  ?? ConnectionProperties.OAuthAccessToken
                                  ?? _config.AccessToken;

            _config.ClientId = ConnectionProperties.ClientId ?? _config.ClientId;
            _config.ClientSecret = ConnectionProperties.ClientSecret ?? _config.ClientSecret;
            _config.ApiVersion = ConnectionProperties.ApiVersion ?? _config.ApiVersion;

            if (ConnectionProperties.TimeoutMs > 0)
                _config.TimeoutSeconds = Math.Max(1, ConnectionProperties.TimeoutMs / 1000);

            if (ConnectionProperties.MaxRetries > 0)
                _config.MaxRetries = ConnectionProperties.MaxRetries;

            if (ConnectionProperties.RetryDelayMs > 0)
                _config.RateLimitDelayMs = ConnectionProperties.RetryDelayMs;

            if (ConnectionProperties.ParameterList != null)
            {
                if (ConnectionProperties.ParameterList.TryGetValue("OrganizationId", out var orgId))
                    _config.OrganizationId = orgId ?? _config.OrganizationId;
                if (ConnectionProperties.ParameterList.TryGetValue("AdAccountId", out var adAccountId))
                    _config.AdAccountId = adAccountId ?? _config.AdAccountId;
            }
        }

        private Dictionary<string, string> BuildAuthHeaders()
        {
            HydrateConfigFromConnectionProperties();

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(_config.AccessToken))
                headers["Authorization"] = $"Bearer {_config.AccessToken}";

            return headers;
        }


    /// <summary>
    /// Constructor for SnapchatDataSource
    /// </summary>
    public SnapchatDataSource(string datasourcename, IDMLogger logger, IDMEEditor editor, DataSourceType type, IErrorsInfo errors)
        : base(datasourcename, logger, editor, type, errors)
    {
        InitializeEntities();
    }

    /// <summary>
    /// Initialize entities for Snapchat data source
    /// </summary>
    private void InitializeEntities()
    {
        // Organizations
        _entities["organizations"] = new EntityStructure
        {
            EntityName = "organizations",
            ViewID = 1,
            Fields = new List<EntityField>
            {
                new EntityField { FieldName = "id", Fieldtype ="string", ValueRetrievedFromParent = false, IsKey = true },
                new EntityField { FieldName = "name", Fieldtype ="string", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "type", Fieldtype ="string", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "country", Fieldtype ="string", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "timezone", Fieldtype ="string", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "created_at", Fieldtype ="datetime", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "updated_at", Fieldtype ="datetime", ValueRetrievedFromParent = false }
            }
        };

        // Ad Accounts
        _entities["adaccounts"] = new EntityStructure
        {
            EntityName = "adaccounts",
            ViewID = 2,
            Fields = new List<EntityField>
            {
                new EntityField { FieldName = "id", Fieldtype ="string", ValueRetrievedFromParent = false, IsKey = true },
                new EntityField { FieldName = "name", Fieldtype ="string", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "organization_id", Fieldtype ="string", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "currency", Fieldtype ="string", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "timezone", Fieldtype ="string", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "status", Fieldtype ="string", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "created_at", Fieldtype ="datetime", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "updated_at", Fieldtype ="datetime", ValueRetrievedFromParent = false }
            }
        };

        // Campaigns
        _entities["campaigns"] = new EntityStructure
        {
            EntityName = "campaigns",
            ViewID = 3,
            Fields = new List<EntityField>
            {
                new EntityField { FieldName = "id", Fieldtype ="string", ValueRetrievedFromParent = false, IsKey = true },
                new EntityField { FieldName = "name", Fieldtype ="string", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "ad_account_id", Fieldtype ="string", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "status", Fieldtype ="string", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "objective", Fieldtype ="string", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "start_time", Fieldtype ="datetime", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "end_time", Fieldtype ="datetime", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "budget_micro", Fieldtype ="long", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "created_at", Fieldtype ="datetime", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "updated_at", Fieldtype ="datetime", ValueRetrievedFromParent = false }
            }
        };

        // Ads
        _entities["ads"] = new EntityStructure
        {
            EntityName = "ads",
            ViewID = 4,
            Fields = new List<EntityField>
            {
                new EntityField { FieldName = "id", Fieldtype ="string", ValueRetrievedFromParent = false, IsKey = true },
                new EntityField { FieldName = "name", Fieldtype ="string", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "campaign_id", Fieldtype ="string", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "ad_squad_id", Fieldtype ="string", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "status", Fieldtype ="string", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "type", Fieldtype ="string", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "creative_id", Fieldtype ="string", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "created_at", Fieldtype ="datetime", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "updated_at", Fieldtype ="datetime", ValueRetrievedFromParent = false }
            }
        };

        // Ad Squads
        _entities["adsquads"] = new EntityStructure
        {
            EntityName = "adsquads",
            ViewID = 5,
            Fields = new List<EntityField>
            {
                new EntityField { FieldName = "id", Fieldtype ="string", ValueRetrievedFromParent = false, IsKey = true },
                new EntityField { FieldName = "name", Fieldtype ="string", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "campaign_id", Fieldtype ="string", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "status", Fieldtype ="string", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "targeting", Fieldtype ="string", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "billing_event", Fieldtype ="string", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "bid_micro", Fieldtype ="long", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "created_at", Fieldtype ="datetime", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "updated_at", Fieldtype ="datetime", ValueRetrievedFromParent = false }
            }
        };

        // Creatives
        _entities["creatives"] = new EntityStructure
        {
            EntityName = "creatives",
            ViewID = 6,
            Fields = new List<EntityField>
            {
                new EntityField { FieldName = "id", Fieldtype ="string", ValueRetrievedFromParent = false, IsKey = true },
                new EntityField { FieldName = "name", Fieldtype ="string", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "ad_account_id", Fieldtype ="string", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "type", Fieldtype ="string", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "headline", Fieldtype ="string", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "call_to_action", Fieldtype ="string", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "media", Fieldtype ="string", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "created_at", Fieldtype ="datetime", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "updated_at", Fieldtype ="datetime", ValueRetrievedFromParent = false }
            }
        };

        // Analytics
        _entities["analytics"] = new EntityStructure
        {
            EntityName = "analytics",
            ViewID = 7,
            Fields = new List<EntityField>
            {
                new EntityField { FieldName = "date", Fieldtype ="datetime", ValueRetrievedFromParent = false, IsKey = true },
                new EntityField { FieldName = "campaign_id", Fieldtype ="string", ValueRetrievedFromParent = false, IsKey = true },
                new EntityField { FieldName = "impressions", Fieldtype ="integer", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "swipes", Fieldtype ="integer", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "spend", Fieldtype ="decimal", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "clicks", Fieldtype ="integer", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "conversions", Fieldtype ="integer", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "frequency", Fieldtype ="decimal", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "reach", Fieldtype ="integer", ValueRetrievedFromParent = false }
            }
        };

        // Audiences
        _entities["audiences"] = new EntityStructure
        {
            EntityName = "audiences",
            ViewID = 8,
            Fields = new List<EntityField>
            {
                new EntityField { FieldName = "id", Fieldtype ="string", ValueRetrievedFromParent = false, IsKey = true },
                new EntityField { FieldName = "name", Fieldtype ="string", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "ad_account_id", Fieldtype ="string", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "size", Fieldtype ="integer", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "status", Fieldtype ="string", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "type", Fieldtype ="string", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "created_at", Fieldtype ="datetime", ValueRetrievedFromParent = false },
                new EntityField { FieldName = "updated_at", Fieldtype ="datetime", ValueRetrievedFromParent = false }
            }
        };

        EntitiesNames = _entities.Keys.ToList();
        Entities = EntitiesNames.Select(k =>
        {
            var entity = _entities[k];
            entity.DatasourceEntityName = k;
            return entity;
        }).ToList();
    }

    public async Task<bool> ConnectAsync()
    {
        try
        {
            HydrateConfigFromConnectionProperties();
            if (string.IsNullOrEmpty(_config.AccessToken))
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = "Access token is required for Snapchat connection";
                return false;
            }

            using var response = await GetAsync($"{ApiBaseUrl}/organizations", null, BuildAuthHeaders(), default).ConfigureAwait(false);
            var content = response != null ? await response.Content.ReadAsStringAsync().ConfigureAwait(false) : "No response";

            if (response is not null && response.IsSuccessStatusCode)
            {
                ErrorObject.Flag = Errors.Ok;
                ErrorObject.Message = "Successfully connected to Snapchat API";
                ConnectionStatus = ConnectionState.Open;
                return true;
            }

            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = $"Snapchat API connection failed: {response?.StatusCode} - {content}";
            return false;
        }
        catch (Exception ex)
        {
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = $"Failed to connect to Snapchat API: {ex.Message}";
            return false;
        }
    }

    public Task<bool> DisconnectAsync()
    {
        try
        {
            Closeconnection();
            ConnectionStatus = ConnectionState.Closed;
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
    {
        var dt = await GetEntityDataTableAsync(EntityName, Filter).ConfigureAwait(false);
        if (dt == null) return Array.Empty<object>();

        var result = new List<object>(dt.Rows.Count);
        foreach (DataRow row in dt.Rows)
        {
            var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (DataColumn col in dt.Columns)
                dict[col.ColumnName] = row[col];
            result.Add(dict);
        }

        return result;
    }

    private async Task<DataTable?> GetEntityDataTableAsync(string entityName, List<AppFilter>? filters)
    {
        try
        {
            HydrateConfigFromConnectionProperties();
            filters ??= new List<AppFilter>();

            string url;
            switch (entityName.ToLowerInvariant())
            {
                case "organizations":
                    url = $"{ApiBaseUrl}/organizations";
                    break;

                case "adaccounts":
                {
                    var orgId = filters.FirstOrDefault(f => f.FieldName == "organization_id")?.FilterValue?.ToString()
                                ?? _config.OrganizationId;
                    url = $"{ApiBaseUrl}/organizations/{orgId}/adaccounts";
                    break;
                }

                case "campaigns":
                {
                    var adAccountId = filters.FirstOrDefault(f => f.FieldName == "ad_account_id")?.FilterValue?.ToString()
                                      ?? _config.AdAccountId;
                    url = $"{ApiBaseUrl}/adaccounts/{adAccountId}/campaigns";
                    break;
                }

                case "ads":
                {
                    var campaignId = filters.FirstOrDefault(f => f.FieldName == "campaign_id")?.FilterValue?.ToString() ?? "";
                    url = $"{ApiBaseUrl}/campaigns/{campaignId}/ads";
                    break;
                }

                case "adsquads":
                {
                    var campaignId = filters.FirstOrDefault(f => f.FieldName == "campaign_id")?.FilterValue?.ToString() ?? "";
                    url = $"{ApiBaseUrl}/campaigns/{campaignId}/adsquads";
                    break;
                }

                case "creatives":
                {
                    var adAccountId = filters.FirstOrDefault(f => f.FieldName == "ad_account_id")?.FilterValue?.ToString()
                                      ?? _config.AdAccountId;
                    url = $"{ApiBaseUrl}/adaccounts/{adAccountId}/creatives";
                    break;
                }

                case "analytics":
                {
                    var adAccountId = filters.FirstOrDefault(f => f.FieldName == "ad_account_id")?.FilterValue?.ToString()
                                      ?? _config.AdAccountId;
                    var startTime = filters.FirstOrDefault(f => f.FieldName == "start_time")?.FilterValue?.ToString()
                                    ?? DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-ddTHH:mm:ssZ");
                    var endTime = filters.FirstOrDefault(f => f.FieldName == "end_time")?.FilterValue?.ToString()
                                  ?? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    url = $"{ApiBaseUrl}/adaccounts/{adAccountId}/stats?start_time={startTime}&end_time={endTime}";
                    break;
                }

                case "audiences":
                {
                    var adAccountId = filters.FirstOrDefault(f => f.FieldName == "ad_account_id")?.FilterValue?.ToString()
                                      ?? _config.AdAccountId;
                    url = $"{ApiBaseUrl}/adaccounts/{adAccountId}/audiences";
                    break;
                }

                default:
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = $"Unsupported entity: {entityName}";
                    return null;
            }

            if (_config.RateLimitDelayMs > 0)
                await Task.Delay(_config.RateLimitDelayMs).ConfigureAwait(false);

            using var response = await GetAsync(url, null, BuildAuthHeaders(), default).ConfigureAwait(false);
            var jsonContent = response != null ? await response.Content.ReadAsStringAsync().ConfigureAwait(false) : string.Empty;

            if (response is null || !response.IsSuccessStatusCode)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Snapchat API request failed: {response?.StatusCode} - {jsonContent}";
                return null;
            }

            ErrorObject.Flag = Errors.Ok;
            ErrorObject.Message = $"Successfully retrieved {entityName} data";
            return ParseJsonToDataTable(jsonContent, entityName);
        }
        catch (Exception ex)
        {
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = $"Failed to get {entityName} data: {ex.Message}";
            return null;
        }
    }

        /// <summary>
        /// Parse JSON response to DataTable
        /// </summary>
        private DataTable ParseJsonToDataTable(string jsonContent, string entityName)
        {
            var dataTable = new DataTable(entityName);
            var entityStructure = _entities.ContainsKey(entityName.ToLowerInvariant()) ? _entities[entityName.ToLowerInvariant()] : null;

            try
            {
                using var document = JsonDocument.Parse(jsonContent);
                var root = document.RootElement;

                // Handle Snapchat API response structure
                JsonElement dataElement;
                if (root.TryGetProperty("organizations", out var organizationsProp))
                {
                    dataElement = organizationsProp;
                }
                else if (root.TryGetProperty("adaccounts", out var adaccountsProp))
                {
                    dataElement = adaccountsProp;
                }
                else if (root.TryGetProperty("campaigns", out var campaignsProp))
                {
                    dataElement = campaignsProp;
                }
                else if (root.TryGetProperty("ads", out var adsProp))
                {
                    dataElement = adsProp;
                }
                else if (root.TryGetProperty("adsquads", out var adsquadsProp))
                {
                    dataElement = adsquadsProp;
                }
                else if (root.TryGetProperty("creatives", out var creativesProp))
                {
                    dataElement = creativesProp;
                }
                else if (root.TryGetProperty("stats", out var statsProp))
                {
                    dataElement = statsProp;
                }
                else if (root.TryGetProperty("audiences", out var audiencesProp))
                {
                    dataElement = audiencesProp;
                }
                else
                {
                    // Single object response
                    dataElement = root;
                }

                // Create columns based on metadata or first object
                if (entityStructure != null)
                {
                    foreach (var field in entityStructure.Fields)
                    {
                        dataTable.Columns.Add(field.FieldName, GetFieldtype(field.Fieldtype));
                    }
                }
                else if (dataElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in dataElement.EnumerateObject())
                    {
                        dataTable.Columns.Add(property.Name, typeof(string));
                    }
                }
                else if (dataElement.ValueKind == JsonValueKind.Array && dataElement.GetArrayLength() > 0)
                {
                    var firstItem = dataElement[0];
                    foreach (var property in firstItem.EnumerateObject())
                    {
                        dataTable.Columns.Add(property.Name, typeof(string));
                    }
                }

                // Add rows
                if (dataElement.ValueKind == JsonValueKind.Object)
                {
                    var row = dataTable.NewRow();
                    foreach (var property in dataElement.EnumerateObject())
                    {
                        if (dataTable.Columns.Contains(property.Name))
                        {
                            row[property.Name] = GetJsonValue(property.Value);
                        }
                    }
                    dataTable.Rows.Add(row);
                }
                else if (dataElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in dataElement.EnumerateArray())
                    {
                        var row = dataTable.NewRow();
                        foreach (var property in item.EnumerateObject())
                        {
                            if (dataTable.Columns.Contains(property.Name))
                            {
                                row[property.Name] = GetJsonValue(property.Value);
                            }
                        }
                        dataTable.Rows.Add(row);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse JSON response: {ex.Message}", ex);
            }

            return dataTable;
        }

        /// <summary>
        /// Get .NET type from field type string
        /// </summary>
        private Type GetFieldtype(string Fieldtype)
        {
            return Fieldtype.ToLower() switch
            {
                "string" => typeof(string),
                "integer" => typeof(int),
                "long" => typeof(long),
                "decimal" => typeof(decimal),
                "boolean" => typeof(bool),
                "datetime" => typeof(DateTime),
                _ => typeof(string)
            };
        }

        /// <summary>
        /// Get value from JSON element
        /// </summary>
        private object GetJsonValue(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt32(out var intValue) ? intValue : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => element.GetRawText()
            };
        }

        [CommandAttribute(ObjectType ="SnapchatOrganization", PointType = EnumPointType.Function,Name = "GetOrganizations", Caption = "Get Snapchat Organizations", ClassName = "SnapchatDataSource")]
        public async Task<SnapchatResponse<SnapchatOrganization>> GetOrganizations()
        {
            var url = $"{ApiBaseUrl}/organizations";
            using var response = await GetAsync(url, null, BuildAuthHeaders(), default).ConfigureAwait(false);
            string json = response != null ? await response.Content.ReadAsStringAsync().ConfigureAwait(false) : string.Empty;
            return JsonSerializer.Deserialize<SnapchatResponse<SnapchatOrganization>>(json);
        }

        [CommandAttribute(ObjectType ="SnapchatAdAccount", PointType = EnumPointType.Function,Name = "GetAdAccounts", Caption = "Get Snapchat Ad Accounts", ClassName = "SnapchatDataSource")]
        public async Task<SnapchatResponse<SnapchatAdAccount>> GetAdAccounts(string organizationId = null)
        {
            var orgParam = string.IsNullOrEmpty(organizationId) ? "" : $"?organization_id={organizationId}";
            var url = $"{ApiBaseUrl}/adaccounts{orgParam}";
            using var response = await GetAsync(url, null, BuildAuthHeaders(), default).ConfigureAwait(false);
            string json = response != null ? await response.Content.ReadAsStringAsync().ConfigureAwait(false) : string.Empty;
            return JsonSerializer.Deserialize<SnapchatResponse<SnapchatAdAccount>>(json);
        }

        [CommandAttribute(ObjectType ="SnapchatCampaign", PointType = EnumPointType.Function,Name = "GetCampaigns", Caption = "Get Snapchat Campaigns", ClassName = "SnapchatDataSource")]
        public async Task<SnapchatResponse<SnapchatCampaign>> GetCampaigns(string adAccountId)
        {
            var url = $"{ApiBaseUrl}/campaigns?ad_account_id={adAccountId}";
            using var response = await GetAsync(url, null, BuildAuthHeaders(), default).ConfigureAwait(false);
            string json = response != null ? await response.Content.ReadAsStringAsync().ConfigureAwait(false) : string.Empty;
            return JsonSerializer.Deserialize<SnapchatResponse<SnapchatCampaign>>(json);
        }

        [CommandAttribute(ObjectType ="SnapchatAdSquad", PointType = EnumPointType.Function,Name = "GetAdSquads", Caption = "Get Snapchat Ad Squads", ClassName = "SnapchatDataSource")]
        public async Task<SnapchatResponse<SnapchatAdSquad>> GetAdSquads(string campaignId)
        {
            var url = $"{ApiBaseUrl}/adsquads?campaign_id={campaignId}";
            using var response = await GetAsync(url, null, BuildAuthHeaders(), default).ConfigureAwait(false);
            string json = response != null ? await response.Content.ReadAsStringAsync().ConfigureAwait(false) : string.Empty;
            return JsonSerializer.Deserialize<SnapchatResponse<SnapchatAdSquad>>(json);
        }

        [CommandAttribute(ObjectType ="SnapchatAd", PointType = EnumPointType.Function,Name = "GetAds", Caption = "Get Snapchat Ads", ClassName = "SnapchatDataSource")]
        public async Task<SnapchatResponse<SnapchatAd>> GetAds(string adSquadId)
        {
            var url = $"{ApiBaseUrl}/ads?ad_squad_id={adSquadId}";
            using var response = await GetAsync(url, null, BuildAuthHeaders(), default).ConfigureAwait(false);
            string json = response != null ? await response.Content.ReadAsStringAsync().ConfigureAwait(false) : string.Empty;
            return JsonSerializer.Deserialize<SnapchatResponse<SnapchatAd>>(json);
        }

        [CommandAttribute(ObjectType ="SnapchatCreative", PointType = EnumPointType.Function,Name = "GetCreatives", Caption = "Get Snapchat Creatives", ClassName = "SnapchatDataSource")]
        public async Task<SnapchatResponse<SnapchatCreative>> GetCreatives(string adAccountId)
        {
            var url = $"{ApiBaseUrl}/creatives?ad_account_id={adAccountId}";
            using var response = await GetAsync(url, null, BuildAuthHeaders(), default).ConfigureAwait(false);
            string json = response != null ? await response.Content.ReadAsStringAsync().ConfigureAwait(false) : string.Empty;
            return JsonSerializer.Deserialize<SnapchatResponse<SnapchatCreative>>(json);
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Snapchat, PointType = EnumPointType.Function, ObjectType ="SnapchatCampaign",Name = "CreateCampaign", Caption = "Create Snapchat Campaign", ClassType ="SnapchatDataSource", Showin = ShowinType.Both, Order = 10, iconimage = "snapchat.png", misc = "ReturnType: IEnumerable<SnapchatCampaign>")]
        public async Task<IEnumerable<SnapchatCampaign>> CreateCampaignAsync(SnapchatCampaign campaign)
        {
            try
            {
                using var result = await PostAsync($"{ApiBaseUrl}/campaigns", campaign, null, BuildAuthHeaders(), default).ConfigureAwait(false);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var response = JsonSerializer.Deserialize<SnapchatResponse<SnapchatCampaign>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    var createdCampaign = response?.Campaigns?.FirstOrDefault();
                    if (createdCampaign != null)
                    {
                        return new List<SnapchatCampaign> { createdCampaign }.Select(c => c.Attach<SnapchatCampaign>(this));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating campaign: {ex.Message}");
            }
            return new List<SnapchatCampaign>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Snapchat, PointType = EnumPointType.Function, ObjectType ="SnapchatCampaign",Name = "UpdateCampaign", Caption = "Update Snapchat Campaign", ClassType ="SnapchatDataSource", Showin = ShowinType.Both, Order = 11, iconimage = "snapchat.png", misc = "ReturnType: IEnumerable<SnapchatCampaign>")]
        public async Task<IEnumerable<SnapchatCampaign>> UpdateCampaignAsync(SnapchatCampaign campaign)
        {
            try
            {
                using var result = await PutAsync($"{ApiBaseUrl}/campaigns/{campaign.Id}", campaign, null, BuildAuthHeaders(), default).ConfigureAwait(false);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var response = JsonSerializer.Deserialize<SnapchatResponse<SnapchatCampaign>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    var updatedCampaign = response?.Campaigns?.FirstOrDefault();
                    if (updatedCampaign != null)
                    {
                        return new List<SnapchatCampaign> { updatedCampaign }.Select(c => c.Attach<SnapchatCampaign>(this));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating campaign: {ex.Message}");
            }
            return new List<SnapchatCampaign>();
        }
    }
}
