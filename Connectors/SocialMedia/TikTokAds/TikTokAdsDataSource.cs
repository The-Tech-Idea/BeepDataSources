using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using DataManagementEngineStandard;
using DataManagementModelsStandard;

namespace BeepDataSources.Connectors.SocialMedia.TikTokAds
{
    /// <summary>
    /// Configuration class for TikTok Ads data source
    /// </summary>
    public class TikTokAdsConfig
    {
        /// <summary>
        /// TikTok Ads Access Token
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// TikTok Ads App ID
        /// </summary>
        public string AppId { get; set; } = string.Empty;

        /// <summary>
        /// TikTok Ads Secret
        /// </summary>
        public string Secret { get; set; } = string.Empty;

        /// <summary>
        /// Advertiser ID for TikTok Ads
        /// </summary>
        public string AdvertiserId { get; set; } = string.Empty;

        /// <summary>
        /// API version for TikTok Ads API (default: v1.3)
        /// </summary>
        public string ApiVersion { get; set; } = "v1.3";

        /// <summary>
        /// Base URL for TikTok Ads API
        /// </summary>
        public string BaseUrl => $"https://business-api.tiktok.com/open_api/{ApiVersion}";

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
    /// TikTok Ads data source implementation for Beep framework
    /// Supports TikTok Ads API
    /// </summary>
    public class TikTokAdsDataSource : IDataSource
    {
        private readonly TikTokAdsConfig _config;
        private HttpClient _httpClient;
        private bool _isConnected;
        private readonly Dictionary<string, EntityMetadata> _entityMetadata;

        /// <summary>
        /// Constructor for TikTokAdsDataSource
        /// </summary>
        /// <param name="config">TikTok Ads configuration</param>
        public TikTokAdsDataSource(TikTokAdsConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _entityMetadata = InitializeEntityMetadata();
        }

        /// <summary>
        /// Constructor for TikTokAdsDataSource with connection string
        /// </summary>
        /// <param name="connectionString">Connection string in format: AccessToken=xxx;AppId=xxx;Secret=xxx;AdvertiserId=xxx</param>
        public TikTokAdsDataSource(string connectionString)
        {
            _config = ParseConnectionString(connectionString);
            _entityMetadata = InitializeEntityMetadata();
        }

        /// <summary>
        /// Parse connection string into TikTokAdsConfig
        /// </summary>
        private TikTokAdsConfig ParseConnectionString(string connectionString)
        {
            var config = new TikTokAdsConfig();
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
                        case "accesstoken":
                            config.AccessToken = value;
                            break;
                        case "appid":
                            config.AppId = value;
                            break;
                        case "secret":
                            config.Secret = value;
                            break;
                        case "advertiserid":
                            config.AdvertiserId = value;
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
        /// Initialize entity metadata for TikTok Ads entities
        /// </summary>
        private Dictionary<string, EntityMetadata> InitializeEntityMetadata()
        {
            var metadata = new Dictionary<string, EntityMetadata>();

            // Campaigns
            metadata["campaigns"] = new EntityMetadata
            {
                EntityName = "campaigns",
                DisplayName = "Campaigns",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "campaign_id", Type = "string", IsPrimaryKey = true, DisplayName = "Campaign ID" },
                    new EntityField { Name = "campaign_name", Type = "string", DisplayName = "Campaign Name" },
                    new EntityField { Name = "advertiser_id", Type = "string", DisplayName = "Advertiser ID" },
                    new EntityField { Name = "campaign_type", Type = "string", DisplayName = "Campaign Type" },
                    new EntityField { Name = "objective_type", Type = "string", DisplayName = "Objective Type" },
                    new EntityField { Name = "budget_mode", Type = "string", DisplayName = "Budget Mode" },
                    new EntityField { Name = "budget", Type = "decimal", DisplayName = "Budget" },
                    new EntityField { Name = "status", Type = "string", DisplayName = "Status" },
                    new EntityField { Name = "opt_status", Type = "string", DisplayName = "Optimization Status" },
                    new EntityField { Name = "create_time", Type = "datetime", DisplayName = "Create Time" },
                    new EntityField { Name = "modify_time", Type = "datetime", DisplayName = "Modify Time" },
                    new EntityField { Name = "campaign_app_profile_page_state", Type = "string", DisplayName = "App Profile Page State" }
                }
            };

            // Ad Groups
            metadata["adgroups"] = new EntityMetadata
            {
                EntityName = "adgroups",
                DisplayName = "Ad Groups",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "adgroup_id", Type = "string", IsPrimaryKey = true, DisplayName = "Ad Group ID" },
                    new EntityField { Name = "adgroup_name", Type = "string", DisplayName = "Ad Group Name" },
                    new EntityField { Name = "campaign_id", Type = "string", DisplayName = "Campaign ID" },
                    new EntityField { Name = "advertiser_id", Type = "string", DisplayName = "Advertiser ID" },
                    new EntityField { Name = "placement_type", Type = "string", DisplayName = "Placement Type" },
                    new EntityField { Name = "billing_event", Type = "string", DisplayName = "Billing Event" },
                    new EntityField { Name = "bid_type", Type = "string", DisplayName = "Bid Type" },
                    new EntityField { Name = "bid_price", Type = "decimal", DisplayName = "Bid Price" },
                    new EntityField { Name = "budget", Type = "decimal", DisplayName = "Budget" },
                    new EntityField { Name = "status", Type = "string", DisplayName = "Status" },
                    new EntityField { Name = "opt_status", Type = "string", DisplayName = "Optimization Status" },
                    new EntityField { Name = "create_time", Type = "datetime", DisplayName = "Create Time" },
                    new EntityField { Name = "modify_time", Type = "datetime", DisplayName = "Modify Time" },
                    new EntityField { Name = "age_groups", Type = "string", DisplayName = "Age Groups" },
                    new EntityField { Name = "genders", Type = "string", DisplayName = "Genders" },
                    new EntityField { Name = "languages", Type = "string", DisplayName = "Languages" },
                    new EntityField { Name = "location_ids", Type = "string", DisplayName = "Location IDs" },
                    new EntityField { Name = "interests", Type = "string", DisplayName = "Interests" },
                    new EntityField { Name = "device_models", Type = "string", DisplayName = "Device Models" },
                    new EntityField { Name = "operating_systems", Type = "string", DisplayName = "Operating Systems" },
                    new EntityField { Name = "network_types", Type = "string", DisplayName = "Network Types" }
                }
            };

            // Ads
            metadata["ads"] = new EntityMetadata
            {
                EntityName = "ads",
                DisplayName = "Ads",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "ad_id", Type = "string", IsPrimaryKey = true, DisplayName = "Ad ID" },
                    new EntityField { Name = "ad_name", Type = "string", DisplayName = "Ad Name" },
                    new EntityField { Name = "adgroup_id", Type = "string", DisplayName = "Ad Group ID" },
                    new EntityField { Name = "advertiser_id", Type = "string", DisplayName = "Advertiser ID" },
                    new EntityField { Name = "ad_format", Type = "string", DisplayName = "Ad Format" },
                    new EntityField { Name = "status", Type = "string", DisplayName = "Status" },
                    new EntityField { Name = "opt_status", Type = "string", DisplayName = "Optimization Status" },
                    new EntityField { Name = "call_to_action", Type = "string", DisplayName = "Call to Action" },
                    new EntityField { Name = "impression_tracking_url", Type = "string", DisplayName = "Impression Tracking URL" },
                    new EntityField { Name = "click_tracking_url", Type = "string", DisplayName = "Click Tracking URL" },
                    new EntityField { Name = "video_id", Type = "string", DisplayName = "Video ID" },
                    new EntityField { Name = "image_ids", Type = "string", DisplayName = "Image IDs" },
                    new EntityField { Name = "title", Type = "string", DisplayName = "Title" },
                    new EntityField { Name = "description", Type = "string", DisplayName = "Description" },
                    new EntityField { Name = "landing_page_url", Type = "string", DisplayName = "Landing Page URL" },
                    new EntityField { Name = "create_time", Type = "datetime", DisplayName = "Create Time" },
                    new EntityField { Name = "modify_time", Type = "datetime", DisplayName = "Modify Time" }
                }
            };

            // Analytics
            metadata["analytics"] = new EntityMetadata
            {
                EntityName = "analytics",
                DisplayName = "Analytics",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "ad_id", Type = "string", IsPrimaryKey = true, DisplayName = "Ad ID" },
                    new EntityField { Name = "date", Type = "datetime", IsPrimaryKey = true, DisplayName = "Date" },
                    new EntityField { Name = "advertiser_id", Type = "string", DisplayName = "Advertiser ID" },
                    new EntityField { Name = "campaign_id", Type = "string", DisplayName = "Campaign ID" },
                    new EntityField { Name = "adgroup_id", Type = "string", DisplayName = "Ad Group ID" },
                    new EntityField { Name = "impressions", Type = "integer", DisplayName = "Impressions" },
                    new EntityField { Name = "clicks", Type = "integer", DisplayName = "Clicks" },
                    new EntityField { Name = "cost_per_click", Type = "decimal", DisplayName = "Cost Per Click" },
                    new EntityField { Name = "cost_per_mille", Type = "decimal", DisplayName = "Cost Per Mille" },
                    new EntityField { Name = "cost_per_result", Type = "decimal", DisplayName = "Cost Per Result" },
                    new EntityField { Name = "cost_per_conversion", Type = "decimal", DisplayName = "Cost Per Conversion" },
                    new EntityField { Name = "spend", Type = "decimal", DisplayName = "Spend" },
                    new EntityField { Name = "reach", Type = "integer", DisplayName = "Reach" },
                    new EntityField { Name = "frequency", Type = "decimal", DisplayName = "Frequency" },
                    new EntityField { Name = "conversion", Type = "integer", DisplayName = "Conversions" },
                    new EntityField { Name = "result", Type = "integer", DisplayName = "Results" },
                    new EntityField { Name = "likes", Type = "integer", DisplayName = "Likes" },
                    new EntityField { Name = "comments", Type = "integer", DisplayName = "Comments" },
                    new EntityField { Name = "shares", Type = "integer", DisplayName = "Shares" },
                    new EntityField { Name = "profile_visits", Type = "integer", DisplayName = "Profile Visits" },
                    new EntityField { Name = "follows", Type = "integer", DisplayName = "Follows" },
                    new EntityField { Name = "click_through_rate", Type = "decimal", DisplayName = "Click Through Rate" },
                    new EntityField { Name = "conversion_rate", Type = "decimal", DisplayName = "Conversion Rate" }
                }
            };

            // Advertisers
            metadata["advertisers"] = new EntityMetadata
            {
                EntityName = "advertisers",
                DisplayName = "Advertisers",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "advertiser_id", Type = "string", IsPrimaryKey = true, DisplayName = "Advertiser ID" },
                    new EntityField { Name = "advertiser_name", Type = "string", DisplayName = "Advertiser Name" },
                    new EntityField { Name = "advertiser_role", Type = "string", DisplayName = "Advertiser Role" },
                    new EntityField { Name = "currency", Type = "string", DisplayName = "Currency" },
                    new EntityField { Name = "timezone", Type = "string", DisplayName = "Timezone" },
                    new EntityField { Name = "company", Type = "string", DisplayName = "Company" },
                    new EntityField { Name = "industry", Type = "string", DisplayName = "Industry" },
                    new EntityField { Name = "address", Type = "string", DisplayName = "Address" },
                    new EntityField { Name = "phone_number", Type = "string", DisplayName = "Phone Number" },
                    new EntityField { Name = "email", Type = "string", DisplayName = "Email" },
                    new EntityField { Name = "status", Type = "string", DisplayName = "Status" },
                    new EntityField { Name = "create_time", Type = "datetime", DisplayName = "Create Time" },
                    new EntityField { Name = "balance", Type = "decimal", DisplayName = "Balance" }
                }
            };

            return metadata;
        }

        /// <summary>
        /// Connect to TikTok Ads API
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_config.AccessToken))
                {
                    throw new InvalidOperationException("Access token is required for TikTok Ads connection");
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

                // Test connection by getting advertiser info
                var testUrl = $"{_config.BaseUrl}/advertiser/info/?advertiser_ids=[\"{_config.AdvertiserId}\"]";
                var response = await _httpClient.GetAsync(testUrl);

                if (response.IsSuccessStatusCode)
                {
                    _isConnected = true;
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"TikTok Ads API connection failed: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                _isConnected = false;
                throw new Exception($"Failed to connect to TikTok Ads API: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Disconnect from TikTok Ads API
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
        /// Get data from TikTok Ads API
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
                    case "campaigns":
                        var campaignAdvertiserId = parameters.ContainsKey("advertiser_id") ? parameters["advertiser_id"].ToString() : _config.AdvertiserId;
                        var campaignIds = parameters.ContainsKey("campaign_ids") ? parameters["campaign_ids"].ToString() : "";
                        var campaignParam = string.IsNullOrEmpty(campaignIds) ? "" : $"&campaign_ids={campaignIds}";
                        url = $"{_config.BaseUrl}/campaign/get/?advertiser_id={campaignAdvertiserId}{campaignParam}";
                        break;

                    case "adgroups":
                        var adgroupAdvertiserId = parameters.ContainsKey("advertiser_id") ? parameters["advertiser_id"].ToString() : _config.AdvertiserId;
                        var adgroupIds = parameters.ContainsKey("adgroup_ids") ? parameters["adgroup_ids"].ToString() : "";
                        var adgroupParam = string.IsNullOrEmpty(adgroupIds) ? "" : $"&adgroup_ids={adgroupIds}";
                        url = $"{_config.BaseUrl}/adgroup/get/?advertiser_id={adgroupAdvertiserId}{adgroupParam}";
                        break;

                    case "ads":
                        var adAdvertiserId = parameters.ContainsKey("advertiser_id") ? parameters["advertiser_id"].ToString() : _config.AdvertiserId;
                        var adIds = parameters.ContainsKey("ad_ids") ? parameters["ad_ids"].ToString() : "";
                        var adParam = string.IsNullOrEmpty(adIds) ? "" : $"&ad_ids={adIds}";
                        url = $"{_config.BaseUrl}/ad/get/?advertiser_id={adAdvertiserId}{adParam}";
                        break;

                    case "analytics":
                        var analyticsAdvertiserId = parameters.ContainsKey("advertiser_id") ? parameters["advertiser_id"].ToString() : _config.AdvertiserId;
                        var startDate = parameters.ContainsKey("start_date") ? parameters["start_date"].ToString() : DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd");
                        var endDate = parameters.ContainsKey("end_date") ? parameters["end_date"].ToString() : DateTime.Now.ToString("yyyy-MM-dd");
                        var analyticsLevel = parameters.ContainsKey("level") ? parameters["level"].ToString() : "AUCTION_AD";
                        url = $"{_config.BaseUrl}/report/integrated/get/?advertiser_id={analyticsAdvertiserId}&report_type=BASIC&data_level={analyticsLevel}&dimensions=[\"ad_id\",\"stat_time_day\"]&start_date={startDate}&end_date={endDate}";
                        break;

                    case "advertisers":
                        var advertiserIds = parameters.ContainsKey("advertiser_ids") ? parameters["advertiser_ids"].ToString() : $"[\"{_config.AdvertiserId}\"]";
                        url = $"{_config.BaseUrl}/advertiser/info/?advertiser_ids={advertiserIds}";
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
                    throw new Exception($"TikTok Ads API request failed: {response.StatusCode} - {jsonContent}");
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

                // Handle TikTok Ads API response structure
                JsonElement dataElement;
                if (root.TryGetProperty("data", out var dataProp))
                {
                    if (dataProp.TryGetProperty("list", out var listProp))
                    {
                        dataElement = listProp;
                    }
                    else if (dataProp.TryGetProperty("advertiser_infos", out var advertiserInfosProp))
                    {
                        dataElement = advertiserInfosProp;
                    }
                    else if (dataProp.TryGetProperty("report_data", out var reportDataProp))
                    {
                        dataElement = reportDataProp;
                    }
                    else
                    {
                        dataElement = dataProp;
                    }
                }
                else
                {
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
                else if (dataElement.ValueKind == JsonValueKind.Array && dataElement.GetArrayLength() > 0)
                {
                    var firstItem = dataElement[0];
                    foreach (var property in firstItem.EnumerateObject())
                    {
                        dataTable.Columns.Add(property.Name, typeof(string));
                    }
                }
                else if (dataElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in dataElement.EnumerateObject())
                    {
                        dataTable.Columns.Add(property.Name, typeof(string));
                    }
                }

                // Add rows
                if (dataElement.ValueKind == JsonValueKind.Array)
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
                else if (dataElement.ValueKind == JsonValueKind.Object)
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
            return new List<string> { "campaigns", "adgroups", "ads", "analytics", "advertisers" };
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
        /// Insert data (limited support for TikTok Ads API)
        /// </summary>
        public async Task<int> InsertEntityAsync(string entityName, DataTable data)
        {
            throw new NotSupportedException("Insert operations are not supported for TikTok Ads API");
        }

        /// <summary>
        /// Update data (limited support for TikTok Ads API)
        /// </summary>
        public async Task<int> UpdateEntityAsync(string entityName, DataTable data, Dictionary<string, object> filter)
        {
            throw new NotSupportedException("Update operations are not supported for TikTok Ads API");
        }

        /// <summary>
        /// Delete data (limited support for TikTok Ads API)
        /// </summary>
        public async Task<int> DeleteEntityAsync(string entityName, Dictionary<string, object> filter)
        {
            throw new NotSupportedException("Delete operations are not supported for TikTok Ads API");
        }

        /// <summary>
        /// Execute custom query
        /// </summary>
        public async Task<DataTable> ExecuteQueryAsync(string query, Dictionary<string, object> parameters = null)
        {
            // For TikTok Ads, we'll treat query as entity name with parameters
            return await GetEntityAsync(query, parameters);
        }

        /// <summary>
        /// Get connection status
        /// </summary>
        public bool IsConnected => _isConnected;

        /// <summary>
        /// Get data source type
        /// </summary>
        public string DataSourceType => "TikTokAds";

        /// <summary>
        /// Get data source name
        /// </summary>
        public string DataSourceName => "TikTok Ads Data Source";

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
