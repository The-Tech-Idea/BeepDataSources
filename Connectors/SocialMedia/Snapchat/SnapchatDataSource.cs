using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using DataManagementEngineStandard;
using DataManagementModelsStandard;

namespace BeepDataSources.Connectors.SocialMedia.Snapchat
{
    /// <summary>
    /// Configuration class for Snapchat data source
    /// </summary>
    public class SnapchatConfig
    {
        /// <summary>
        /// Snapchat App Client ID
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Snapchat App Client Secret
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
        /// Snapchat Organization ID
        /// </summary>
        public string OrganizationId { get; set; } = string.Empty;

        /// <summary>
        /// Snapchat Ad Account ID
        /// </summary>
        public string AdAccountId { get; set; } = string.Empty;

        /// <summary>
        /// API version for Snapchat Marketing API (default: v1)
        /// </summary>
        public string ApiVersion { get; set; } = "v1";

        /// <summary>
        /// Base URL for Snapchat Marketing API
        /// </summary>
        public string BaseUrl => $"https://adsapi.snapchat.com/{ApiVersion}";

        /// <summary>
        /// Timeout for API requests in seconds
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Maximum number of retries for failed requests
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Rate limit delay between requests in milliseconds
        /// </summary>
        public int RateLimitDelayMs { get; set; } = 1000;
    }

    /// <summary>
    /// Snapchat data source implementation for Beep framework
    /// Supports Snapchat Marketing API
    /// </summary>
    public class SnapchatDataSource : IDataSource
    {
        private readonly SnapchatConfig _config;
        private HttpClient _httpClient;
        private bool _isConnected;
        private readonly Dictionary<string, EntityMetadata> _entityMetadata;

        /// <summary>
        /// Constructor for SnapchatDataSource
        /// </summary>
        /// <param name="config">Snapchat configuration</param>
        public SnapchatDataSource(SnapchatConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _entityMetadata = InitializeEntityMetadata();
        }

        /// <summary>
        /// Constructor for SnapchatDataSource with connection string
        /// </summary>
        /// <param name="connectionString">Connection string in format: ClientId=xxx;ClientSecret=xxx;AccessToken=xxx;OrganizationId=xxx</param>
        public SnapchatDataSource(string connectionString)
        {
            _config = ParseConnectionString(connectionString);
            _entityMetadata = InitializeEntityMetadata();
        }

        /// <summary>
        /// Parse connection string into SnapchatConfig
        /// </summary>
        private SnapchatConfig ParseConnectionString(string connectionString)
        {
            var config = new SnapchatConfig();
            var parts = connectionString.Split(';');

            foreach (var part in parts)
            {
                var keyValue = part.Split('=');
                if (keyValue.Length == 2)
                {
                    var key = keyValue[0].Trim();
                    var value = keyValue[1].Trim();

                    switch (key.ToLower())
                    {
                        case "clientid":
                            config.ClientId = value;
                            break;
                        case "clientsecret":
                            config.ClientSecret = value;
                            break;
                        case "accesstoken":
                            config.AccessToken = value;
                            break;
                        case "refreshtoken":
                            config.RefreshToken = value;
                            break;
                        case "organizationid":
                            config.OrganizationId = value;
                            break;
                        case "adaccountid":
                            config.AdAccountId = value;
                            break;
                        case "apiversion":
                            config.ApiVersion = value;
                            break;
                        case "timeoutseconds":
                            if (int.TryParse(value, out var timeout))
                                config.TimeoutSeconds = timeout;
                            break;
                        case "maxretries":
                            if (int.TryParse(value, out var retries))
                                config.MaxRetries = retries;
                            break;
                        case "ratelimitdelayms":
                            if (int.TryParse(value, out var delay))
                                config.RateLimitDelayMs = delay;
                            break;
                    }
                }
            }

            return config;
        }

        /// <summary>
        /// Initialize entity metadata for Snapchat entities
        /// </summary>
        private Dictionary<string, EntityMetadata> InitializeEntityMetadata()
        {
            var metadata = new Dictionary<string, EntityMetadata>();

            // Organizations
            metadata["organizations"] = new EntityMetadata
            {
                EntityName = "organizations",
                DisplayName = "Organizations",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Organization ID" },
                    new EntityField { Name = "name", Type = "string", DisplayName = "Organization Name" },
                    new EntityField { Name = "type", Type = "string", DisplayName = "Organization Type" },
                    new EntityField { Name = "country", Type = "string", DisplayName = "Country" },
                    new EntityField { Name = "timezone", Type = "string", DisplayName = "Timezone" },
                    new EntityField { Name = "created_at", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "updated_at", Type = "datetime", DisplayName = "Updated At" }
                }
            };

            // Ad Accounts
            metadata["adaccounts"] = new EntityMetadata
            {
                EntityName = "adaccounts",
                DisplayName = "Ad Accounts",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Ad Account ID" },
                    new EntityField { Name = "name", Type = "string", DisplayName = "Ad Account Name" },
                    new EntityField { Name = "organization_id", Type = "string", DisplayName = "Organization ID" },
                    new EntityField { Name = "currency", Type = "string", DisplayName = "Currency" },
                    new EntityField { Name = "timezone", Type = "string", DisplayName = "Timezone" },
                    new EntityField { Name = "status", Type = "string", DisplayName = "Status" },
                    new EntityField { Name = "created_at", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "updated_at", Type = "datetime", DisplayName = "Updated At" }
                }
            };

            // Campaigns
            metadata["campaigns"] = new EntityMetadata
            {
                EntityName = "campaigns",
                DisplayName = "Campaigns",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Campaign ID" },
                    new EntityField { Name = "name", Type = "string", DisplayName = "Campaign Name" },
                    new EntityField { Name = "ad_account_id", Type = "string", DisplayName = "Ad Account ID" },
                    new EntityField { Name = "status", Type = "string", DisplayName = "Status" },
                    new EntityField { Name = "objective", Type = "string", DisplayName = "Objective" },
                    new EntityField { Name = "start_time", Type = "datetime", DisplayName = "Start Time" },
                    new EntityField { Name = "end_time", Type = "datetime", DisplayName = "End Time" },
                    new EntityField { Name = "budget_micro", Type = "long", DisplayName = "Budget (Micro)" },
                    new EntityField { Name = "created_at", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "updated_at", Type = "datetime", DisplayName = "Updated At" }
                }
            };

            // Ads
            metadata["ads"] = new EntityMetadata
            {
                EntityName = "ads",
                DisplayName = "Ads",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Ad ID" },
                    new EntityField { Name = "name", Type = "string", DisplayName = "Ad Name" },
                    new EntityField { Name = "campaign_id", Type = "string", DisplayName = "Campaign ID" },
                    new EntityField { Name = "ad_squad_id", Type = "string", DisplayName = "Ad Squad ID" },
                    new EntityField { Name = "status", Type = "string", DisplayName = "Status" },
                    new EntityField { Name = "type", Type = "string", DisplayName = "Ad Type" },
                    new EntityField { Name = "creative_id", Type = "string", DisplayName = "Creative ID" },
                    new EntityField { Name = "created_at", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "updated_at", Type = "datetime", DisplayName = "Updated At" }
                }
            };

            // Ad Squads
            metadata["adsquads"] = new EntityMetadata
            {
                EntityName = "adsquads",
                DisplayName = "Ad Squads",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Ad Squad ID" },
                    new EntityField { Name = "name", Type = "string", DisplayName = "Ad Squad Name" },
                    new EntityField { Name = "campaign_id", Type = "string", DisplayName = "Campaign ID" },
                    new EntityField { Name = "status", Type = "string", DisplayName = "Status" },
                    new EntityField { Name = "targeting", Type = "string", DisplayName = "Targeting" },
                    new EntityField { Name = "billing_event", Type = "string", DisplayName = "Billing Event" },
                    new EntityField { Name = "bid_micro", Type = "long", DisplayName = "Bid (Micro)" },
                    new EntityField { Name = "created_at", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "updated_at", Type = "datetime", DisplayName = "Updated At" }
                }
            };

            // Creatives
            metadata["creatives"] = new EntityMetadata
            {
                EntityName = "creatives",
                DisplayName = "Creatives",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Creative ID" },
                    new EntityField { Name = "name", Type = "string", DisplayName = "Creative Name" },
                    new EntityField { Name = "ad_account_id", Type = "string", DisplayName = "Ad Account ID" },
                    new EntityField { Name = "type", Type = "string", DisplayName = "Creative Type" },
                    new EntityField { Name = "headline", Type = "string", DisplayName = "Headline" },
                    new EntityField { Name = "call_to_action", Type = "string", DisplayName = "Call to Action" },
                    new EntityField { Name = "media", Type = "string", DisplayName = "Media" },
                    new EntityField { Name = "created_at", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "updated_at", Type = "datetime", DisplayName = "Updated At" }
                }
            };

            // Analytics
            metadata["analytics"] = new EntityMetadata
            {
                EntityName = "analytics",
                DisplayName = "Analytics",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "date", Type = "datetime", IsPrimaryKey = true, DisplayName = "Date" },
                    new EntityField { Name = "campaign_id", Type = "string", IsPrimaryKey = true, DisplayName = "Campaign ID" },
                    new EntityField { Name = "impressions", Type = "integer", DisplayName = "Impressions" },
                    new EntityField { Name = "swipes", Type = "integer", DisplayName = "Swipes" },
                    new EntityField { Name = "spend", Type = "decimal", DisplayName = "Spend" },
                    new EntityField { Name = "clicks", Type = "integer", DisplayName = "Clicks" },
                    new EntityField { Name = "conversions", Type = "integer", DisplayName = "Conversions" },
                    new EntityField { Name = "frequency", Type = "decimal", DisplayName = "Frequency" },
                    new EntityField { Name = "reach", Type = "integer", DisplayName = "Reach" }
                }
            };

            // Audiences
            metadata["audiences"] = new EntityMetadata
            {
                EntityName = "audiences",
                DisplayName = "Audiences",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Audience ID" },
                    new EntityField { Name = "name", Type = "string", DisplayName = "Audience Name" },
                    new EntityField { Name = "ad_account_id", Type = "string", DisplayName = "Ad Account ID" },
                    new EntityField { Name = "size", Type = "integer", DisplayName = "Audience Size" },
                    new EntityField { Name = "status", Type = "string", DisplayName = "Status" },
                    new EntityField { Name = "type", Type = "string", DisplayName = "Audience Type" },
                    new EntityField { Name = "created_at", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "updated_at", Type = "datetime", DisplayName = "Updated At" }
                }
            };

            return metadata;
        }

        /// <summary>
        /// Connect to Snapchat API
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_config.AccessToken))
                {
                    throw new InvalidOperationException("Access token is required for Snapchat connection");
                }

                // Initialize HTTP client
                var handler = new HttpClientHandler();
                _httpClient = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
                };

                // Add authorization header
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _config.AccessToken);

                // Test connection by getting organizations
                var testUrl = $"{_config.BaseUrl}/organizations";
                var response = await _httpClient.GetAsync(testUrl);

                if (response.IsSuccessStatusCode)
                {
                    _isConnected = true;
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Snapchat API connection failed: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                _isConnected = false;
                throw new Exception($"Failed to connect to Snapchat API: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Disconnect from Snapchat API
        /// </summary>
        public async Task<bool> DisconnectAsync()
        {
            if (_httpClient != null)
            {
                _httpClient.Dispose();
                _httpClient = null;
            }
            _isConnected = false;
            return true;
        }

        /// <summary>
        /// Get data from Snapchat API
        /// </summary>
        public async Task<DataTable> GetEntityAsync(string entityName, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                await ConnectAsync();
            }

            parameters ??= new Dictionary<string, object>();

            try
            {
                string url;

                switch (entityName.ToLower())
                {
                    case "organizations":
                        url = $"{_config.BaseUrl}/organizations";
                        break;

                    case "adaccounts":
                        var orgId = parameters.ContainsKey("organization_id") ? parameters["organization_id"].ToString() : _config.OrganizationId;
                        url = $"{_config.BaseUrl}/organizations/{orgId}/adaccounts";
                        break;

                    case "campaigns":
                        var adAccountId = parameters.ContainsKey("ad_account_id") ? parameters["ad_account_id"].ToString() : _config.AdAccountId;
                        url = $"{_config.BaseUrl}/adaccounts/{adAccountId}/campaigns";
                        break;

                    case "ads":
                        var campaignId = parameters.ContainsKey("campaign_id") ? parameters["campaign_id"].ToString() : "";
                        url = $"{_config.BaseUrl}/campaigns/{campaignId}/ads";
                        break;

                    case "adsquads":
                        var adsquadCampaignId = parameters.ContainsKey("campaign_id") ? parameters["campaign_id"].ToString() : "";
                        url = $"{_config.BaseUrl}/campaigns/{adsquadCampaignId}/adsquads";
                        break;

                    case "creatives":
                        var creativeAdAccountId = parameters.ContainsKey("ad_account_id") ? parameters["ad_account_id"].ToString() : _config.AdAccountId;
                        url = $"{_config.BaseUrl}/adaccounts/{creativeAdAccountId}/creatives";
                        break;

                    case "analytics":
                        var analyticsAdAccountId = parameters.ContainsKey("ad_account_id") ? parameters["ad_account_id"].ToString() : _config.AdAccountId;
                        var startTime = parameters.ContainsKey("start_time") ? parameters["start_time"].ToString() : DateTime.Now.AddDays(-30).ToString("yyyy-MM-ddTHH:mm:ssZ");
                        var endTime = parameters.ContainsKey("end_time") ? parameters["end_time"].ToString() : DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ");
                        url = $"{_config.BaseUrl}/adaccounts/{analyticsAdAccountId}/stats?start_time={startTime}&end_time={endTime}";
                        break;

                    case "audiences":
                        var audienceAdAccountId = parameters.ContainsKey("ad_account_id") ? parameters["ad_account_id"].ToString() : _config.AdAccountId;
                        url = $"{_config.BaseUrl}/adaccounts/{audienceAdAccountId}/audiences";
                        break;

                    default:
                        throw new ArgumentException($"Unsupported entity: {entityName}");
                }

                // Rate limiting delay
                if (_config.RateLimitDelayMs > 0)
                {
                    await Task.Delay(_config.RateLimitDelayMs);
                }

                var response = await _httpClient.GetAsync(url);
                var jsonContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Snapchat API request failed: {response.StatusCode} - {jsonContent}");
                }

                return ParseJsonToDataTable(jsonContent, entityName);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get {entityName} data: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Parse JSON response to DataTable
        /// </summary>
        private DataTable ParseJsonToDataTable(string jsonContent, string entityName)
        {
            var dataTable = new DataTable(entityName);
            var metadata = _entityMetadata.ContainsKey(entityName.ToLower()) ? _entityMetadata[entityName.ToLower()] : null;

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
                if (metadata != null)
                {
                    foreach (var field in metadata.Fields)
                    {
                        dataTable.Columns.Add(field.Name, GetFieldType(field.Type));
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
        private Type GetFieldType(string fieldType)
        {
            return fieldType.ToLower() switch
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

        /// <summary>
        /// Get available entities
        /// </summary>
        public List<string> GetEntities()
        {
            return new List<string> { "organizations", "adaccounts", "campaigns", "ads", "adsquads", "creatives", "analytics", "audiences" };
        }

        /// <summary>
        /// Get entity metadata
        /// </summary>
        public EntityMetadata GetEntityMetadata(string entityName)
        {
            if (_entityMetadata.ContainsKey(entityName.ToLower()))
            {
                return _entityMetadata[entityName.ToLower()];
            }
            throw new ArgumentException($"Entity '{entityName}' not found");
        }

        /// <summary>
        /// Insert data (limited support for Snapchat API)
        /// </summary>
        public async Task<int> InsertEntityAsync(string entityName, DataTable data)
        {
            if (entityName.ToLower() != "campaigns")
            {
                throw new NotSupportedException($"Insert operations are not supported for {entityName}");
            }

            // Implementation for creating campaigns would go here
            // This is a placeholder as Snapchat API has specific requirements for campaign creation
            throw new NotImplementedException("Campaign creation not yet implemented");
        }

        /// <summary>
        /// Update data (limited support for Snapchat API)
        /// </summary>
        public async Task<int> UpdateEntityAsync(string entityName, DataTable data, Dictionary<string, object> filter)
        {
            throw new NotSupportedException("Update operations are not supported for Snapchat API");
        }

        /// <summary>
        /// Delete data (limited support for Snapchat API)
        /// </summary>
        public async Task<int> DeleteEntityAsync(string entityName, Dictionary<string, object> filter)
        {
            throw new NotSupportedException("Delete operations are not supported for Snapchat API");
        }

        /// <summary>
        /// Execute custom query
        /// </summary>
        public async Task<DataTable> ExecuteQueryAsync(string query, Dictionary<string, object> parameters = null)
        {
            // For Snapchat, we'll treat query as entity name with parameters
            return await GetEntityAsync(query, parameters);
        }

        /// <summary>
        /// Get connection status
        /// </summary>
        public bool IsConnected => _isConnected;

        /// <summary>
        /// Get data source type
        /// </summary>
        public string DataSourceType => "Snapchat";

        /// <summary>
        /// Get data source name
        /// </summary>
        public string DataSourceName => "Snapchat Data Source";

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            if (_httpClient != null)
            {
                _httpClient.Dispose();
                _httpClient = null;
            }
            _isConnected = false;
        }
    }
}
