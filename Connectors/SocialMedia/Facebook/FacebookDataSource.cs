using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DataManagementEngineStandard;
using DataManagementModelsStandard;

namespace BeepDataSources.Facebook
{
    /// <summary>
    /// Configuration class for Facebook data source
    /// </summary>
    public class FacebookConfig
    {
        /// <summary>
        /// Facebook App ID
        /// </summary>
        public string AppId { get; set; } = "";

        /// <summary>
        /// Facebook App Secret
        /// </summary>
        public string AppSecret { get; set; } = "";

        /// <summary>
        /// Access Token (obtained after OAuth)
        /// </summary>
        public string AccessToken { get; set; } = "";

        /// <summary>
        /// Page Access Token for page management
        /// </summary>
        public string PageAccessToken { get; set; } = "";

        /// <summary>
        /// Facebook Page ID
        /// </summary>
        public string PageId { get; set; } = "";

        /// <summary>
        /// Facebook User ID
        /// </summary>
        public string UserId { get; set; } = "";

        /// <summary>
        /// API Version to use
        /// </summary>
        public string ApiVersion { get; set; } = "v18.0";

        /// <summary>
        /// Facebook Graph API endpoint URL
        /// </summary>
        public string BaseUrl { get; set; } = "https://graph.facebook.com";

        /// <summary>
        /// Connection timeout in seconds
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Maximum number of retry attempts
        /// </summary>
        public int MaxRetries { get; set; } = 3;
    }

    /// <summary>
    /// Facebook data source implementation for Beep framework
    /// </summary>
    public class FacebookDataSource : IDataSource
    {
        private readonly FacebookConfig _config;
        private HttpClient _httpClient;
        private bool _isConnected;
        private readonly Dictionary<string, EntityMetadata> _entityMetadata;

        /// <summary>
        /// Constructor for FacebookDataSource
        /// </summary>
        /// <param name="config">Configuration object</param>
        public FacebookDataSource(object config)
        {
            _config = config as FacebookConfig ?? new FacebookConfig();
            _entityMetadata = InitializeEntityMetadata();
        }

        /// <summary>
        /// Initialize entity metadata for Facebook entities
        /// </summary>
        private Dictionary<string, EntityMetadata> InitializeEntityMetadata()
        {
            var metadata = new Dictionary<string, EntityMetadata>();

            // Posts entity
            metadata["posts"] = new EntityMetadata
            {
                EntityName = "posts",
                DisplayName = "Posts",
                PrimaryKey = "id",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Post ID" },
                    new EntityField { Name = "message", Type = "string", DisplayName = "Message" },
                    new EntityField { Name = "story", Type = "string", DisplayName = "Story" },
                    new EntityField { Name = "created_time", Type = "datetime", DisplayName = "Created Time" },
                    new EntityField { Name = "updated_time", Type = "datetime", DisplayName = "Updated Time" },
                    new EntityField { Name = "type", Type = "string", DisplayName = "Post Type" },
                    new EntityField { Name = "status_type", Type = "string", DisplayName = "Status Type" },
                    new EntityField { Name = "permalink_url", Type = "string", DisplayName = "Permalink URL" },
                    new EntityField { Name = "full_picture", Type = "string", DisplayName = "Full Picture URL" },
                    new EntityField { Name = "picture", Type = "string", DisplayName = "Picture URL" },
                    new EntityField { Name = "source", Type = "string", DisplayName = "Source URL" },
                    new EntityField { Name = "name", Type = "string", DisplayName = "Name" },
                    new EntityField { Name = "caption", Type = "string", DisplayName = "Caption" },
                    new EntityField { Name = "description", Type = "string", DisplayName = "Description" },
                    new EntityField { Name = "link", Type = "string", DisplayName = "Link" },
                    new EntityField { Name = "likes", Type = "int", DisplayName = "Likes Count" },
                    new EntityField { Name = "comments", Type = "int", DisplayName = "Comments Count" },
                    new EntityField { Name = "shares", Type = "int", DisplayName = "Shares Count" },
                    new EntityField { Name = "reactions", Type = "int", DisplayName = "Reactions Count" },
                    new EntityField { Name = "is_published", Type = "boolean", DisplayName = "Is Published" },
                    new EntityField { Name = "is_hidden", Type = "boolean", DisplayName = "Is Hidden" },
                    new EntityField { Name = "is_expired", Type = "boolean", DisplayName = "Is Expired" },
                    new EntityField { Name = "scheduled_publish_time", Type = "datetime", DisplayName = "Scheduled Publish Time" }
                }
            };

            // Pages entity
            metadata["pages"] = new EntityMetadata
            {
                EntityName = "pages",
                DisplayName = "Pages",
                PrimaryKey = "id",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Page ID" },
                    new EntityField { Name = "name", Type = "string", DisplayName = "Page Name" },
                    new EntityField { Name = "category", Type = "string", DisplayName = "Category" },
                    new EntityField { Name = "about", Type = "string", DisplayName = "About" },
                    new EntityField { Name = "description", Type = "string", DisplayName = "Description" },
                    new EntityField { Name = "website", Type = "string", DisplayName = "Website" },
                    new EntityField { Name = "phone", Type = "string", DisplayName = "Phone" },
                    new EntityField { Name = "emails", Type = "string", DisplayName = "Emails" },
                    new EntityField { Name = "location", Type = "string", DisplayName = "Location" },
                    new EntityField { Name = "hours", Type = "string", DisplayName = "Hours" },
                    new EntityField { Name = "parking", Type = "string", DisplayName = "Parking" },
                    new EntityField { Name = "cover", Type = "string", DisplayName = "Cover Photo" },
                    new EntityField { Name = "picture", Type = "string", DisplayName = "Profile Picture" },
                    new EntityField { Name = "likes", Type = "int", DisplayName = "Likes Count" },
                    new EntityField { Name = "followers_count", Type = "int", DisplayName = "Followers Count" },
                    new EntityField { Name = "checkins", Type = "int", DisplayName = "Checkins Count" },
                    new EntityField { Name = "were_here_count", Type = "int", DisplayName = "Were Here Count" },
                    new EntityField { Name = "talking_about_count", Type = "int", DisplayName = "Talking About Count" },
                    new EntityField { Name = "is_published", Type = "boolean", DisplayName = "Is Published" },
                    new EntityField { Name = "is_unclaimed", Type = "boolean", DisplayName = "Is Unclaimed" },
                    new EntityField { Name = "is_permanently_closed", Type = "boolean", DisplayName = "Is Permanently Closed" }
                }
            };

            // Groups entity
            metadata["groups"] = new EntityMetadata
            {
                EntityName = "groups",
                DisplayName = "Groups",
                PrimaryKey = "id",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Group ID" },
                    new EntityField { Name = "name", Type = "string", DisplayName = "Group Name" },
                    new EntityField { Name = "description", Type = "string", DisplayName = "Description" },
                    new EntityField { Name = "email", Type = "string", DisplayName = "Email" },
                    new EntityField { Name = "privacy", Type = "string", DisplayName = "Privacy" },
                    new EntityField { Name = "icon", Type = "string", DisplayName = "Icon URL" },
                    new EntityField { Name = "cover", Type = "string", DisplayName = "Cover Photo URL" },
                    new EntityField { Name = "member_count", Type = "int", DisplayName = "Member Count" },
                    new EntityField { Name = "member_request_count", Type = "int", DisplayName = "Member Request Count" },
                    new EntityField { Name = "administrator", Type = "boolean", DisplayName = "Is Administrator" },
                    new EntityField { Name = "moderator", Type = "boolean", DisplayName = "Is Moderator" },
                    new EntityField { Name = "updated_time", Type = "datetime", DisplayName = "Updated Time" },
                    new EntityField { Name = "archived", Type = "boolean", DisplayName = "Is Archived" }
                }
            };

            // Events entity
            metadata["events"] = new EntityMetadata
            {
                EntityName = "events",
                DisplayName = "Events",
                PrimaryKey = "id",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Event ID" },
                    new EntityField { Name = "name", Type = "string", DisplayName = "Event Name" },
                    new EntityField { Name = "description", Type = "string", DisplayName = "Description" },
                    new EntityField { Name = "start_time", Type = "datetime", DisplayName = "Start Time" },
                    new EntityField { Name = "end_time", Type = "datetime", DisplayName = "End Time" },
                    new EntityField { Name = "timezone", Type = "string", DisplayName = "Timezone" },
                    new EntityField { Name = "location", Type = "string", DisplayName = "Location" },
                    new EntityField { Name = "venue", Type = "string", DisplayName = "Venue" },
                    new EntityField { Name = "cover", Type = "string", DisplayName = "Cover Photo" },
                    new EntityField { Name = "picture", Type = "string", DisplayName = "Picture" },
                    new EntityField { Name = "attending_count", Type = "int", DisplayName = "Attending Count" },
                    new EntityField { Name = "declined_count", Type = "int", DisplayName = "Declined Count" },
                    new EntityField { Name = "interested_count", Type = "int", DisplayName = "Interested Count" },
                    new EntityField { Name = "noreply_count", Type = "int", DisplayName = "No Reply Count" },
                    new EntityField { Name = "maybe_count", Type = "int", DisplayName = "Maybe Count" },
                    new EntityField { Name = "is_canceled", Type = "boolean", DisplayName = "Is Canceled" },
                    new EntityField { Name = "ticket_uri", Type = "string", DisplayName = "Ticket URI" },
                    new EntityField { Name = "category", Type = "string", DisplayName = "Category" },
                    new EntityField { Name = "type", Type = "string", DisplayName = "Event Type" }
                }
            };

            // Ads entity
            metadata["ads"] = new EntityMetadata
            {
                EntityName = "ads",
                DisplayName = "Ads",
                PrimaryKey = "id",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Ad ID" },
                    new EntityField { Name = "name", Type = "string", DisplayName = "Ad Name" },
                    new EntityField { Name = "status", Type = "string", DisplayName = "Status" },
                    new EntityField { Name = "creative", Type = "string", DisplayName = "Creative" },
                    new EntityField { Name = "created_time", Type = "datetime", DisplayName = "Created Time" },
                    new EntityField { Name = "updated_time", Type = "datetime", DisplayName = "Updated Time" },
                    new EntityField { Name = "campaign_id", Type = "string", DisplayName = "Campaign ID" },
                    new EntityField { Name = "adset_id", Type = "string", DisplayName = "Ad Set ID" },
                    new EntityField { Name = "account_id", Type = "string", DisplayName = "Account ID" },
                    new EntityField { Name = "tracking_specs", Type = "string", DisplayName = "Tracking Specs" },
                    new EntityField { Name = "conversion_specs", Type = "string", DisplayName = "Conversion Specs" },
                    new EntityField { Name = "bid_amount", Type = "int", DisplayName = "Bid Amount" },
                    new EntityField { Name = "bid_type", Type = "string", DisplayName = "Bid Type" },
                    new EntityField { Name = "daily_budget", Type = "string", DisplayName = "Daily Budget" },
                    new EntityField { Name = "lifetime_budget", Type = "string", DisplayName = "Lifetime Budget" }
                }
            };

            // Insights entity
            metadata["insights"] = new EntityMetadata
            {
                EntityName = "insights",
                DisplayName = "Insights",
                PrimaryKey = "id",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Insight ID" },
                    new EntityField { Name = "name", Type = "string", DisplayName = "Metric Name" },
                    new EntityField { Name = "period", Type = "string", DisplayName = "Period" },
                    new EntityField { Name = "values", Type = "string", DisplayName = "Values" },
                    new EntityField { Name = "title", Type = "string", DisplayName = "Title" },
                    new EntityField { Name = "description", Type = "string", DisplayName = "Description" },
                    new EntityField { Name = "account_id", Type = "string", DisplayName = "Account ID" },
                    new EntityField { Name = "campaign_id", Type = "string", DisplayName = "Campaign ID" },
                    new EntityField { Name = "adset_id", Type = "string", DisplayName = "Ad Set ID" },
                    new EntityField { Name = "ad_id", Type = "string", DisplayName = "Ad ID" },
                    new EntityField { Name = "date_start", Type = "datetime", DisplayName = "Date Start" },
                    new EntityField { Name = "date_stop", Type = "datetime", DisplayName = "Date Stop" }
                }
            };

            return metadata;
        }

        /// <summary>
        /// Connect to Facebook Graph API
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_config.AccessToken))
                {
                    throw new ArgumentException("Access Token is required");
                }

                // Initialize HTTP client
                _httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
                };

                // Set authorization header
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _config.AccessToken);

                // Test connection by getting user info
                var testUrl = $"{_config.BaseUrl}/{_config.ApiVersion}/me?fields=id,name";
                var response = await _httpClient.GetAsync(testUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var userInfo = JsonSerializer.Deserialize<JsonElement>(content);

                    if (userInfo.TryGetProperty("id", out var userId))
                    {
                        _config.UserId = userId.GetString();
                        _isConnected = true;
                        return true;
                    }
                }

                _isConnected = false;
                return false;
            }
            catch (Exception ex)
            {
                _isConnected = false;
                throw new Exception($"Failed to connect to Facebook: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Disconnect from Facebook Graph API
        /// </summary>
        public async Task<bool> DisconnectAsync()
        {
            try
            {
                if (_httpClient != null)
                {
                    _httpClient.Dispose();
                    _httpClient = null;
                }
                _isConnected = false;
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to disconnect from Facebook: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get entity data from Facebook
        /// </summary>
        public async Task<DataTable> GetEntityAsync(string entityName, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Facebook. Call ConnectAsync first.");
            }

            if (!_entityMetadata.ContainsKey(entityName.ToLower()))
            {
                throw new ArgumentException($"Entity '{entityName}' is not supported");
            }

            try
            {
                var endpoint = GetEntityEndpoint(entityName, parameters);
                var response = await _httpClient.GetAsync(endpoint);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Facebook API request failed: {response.StatusCode} - {response.ReasonPhrase}");
                }

                var content = await response.Content.ReadAsStringAsync();
                return ParseJsonToDataTable(content, entityName);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get entity '{entityName}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get entity endpoint URL
        /// </summary>
        private string GetEntityEndpoint(string entityName, Dictionary<string, object> parameters = null)
        {
            var baseUrl = $"{_config.BaseUrl}/{_config.ApiVersion}";
            var endpoint = entityName.ToLower() switch
            {
                "posts" => _config.PageId != null ? $"{baseUrl}/{_config.PageId}/posts" : $"{baseUrl}/me/posts",
                "pages" => $"{baseUrl}/me/accounts",
                "groups" => $"{baseUrl}/me/groups",
                "events" => _config.PageId != null ? $"{baseUrl}/{_config.PageId}/events" : $"{baseUrl}/me/events",
                "ads" => $"{baseUrl}/act_{_config.UserId}/ads",
                "insights" => $"{baseUrl}/me/insights",
                _ => throw new ArgumentException($"Unknown entity: {entityName}")
            };

            var queryParams = new List<string>();
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    queryParams.Add($"{param.Key}={Uri.EscapeDataString(param.Value?.ToString() ?? "")}");
                }
            }

            // Add default fields for better data retrieval
            var fields = GetDefaultFields(entityName);
            if (!string.IsNullOrEmpty(fields))
            {
                queryParams.Add($"fields={fields}");
            }

            if (queryParams.Count > 0)
            {
                endpoint += "?" + string.Join("&", queryParams);
            }

            return endpoint;
        }

        /// <summary>
        /// Get default fields for entity
        /// </summary>
        private string GetDefaultFields(string entityName)
        {
            return entityName.ToLower() switch
            {
                "posts" => "id,message,story,created_time,updated_time,type,status_type,permalink_url,full_picture,picture,source,name,caption,description,link,likes.summary(true),comments.summary(true),shares,reactions.summary(true),is_published,is_hidden,is_expired,scheduled_publish_time",
                "pages" => "id,name,category,about,description,website,phone,emails,location,hours,parking,cover,picture,likes,followers_count,checkins,were_here_count,talking_about_count,is_published,is_unclaimed,is_permanently_closed",
                "groups" => "id,name,description,email,privacy,icon,cover,member_count,member_request_count,administrator,moderator,updated_time,archived",
                "events" => "id,name,description,start_time,end_time,timezone,location,venue,cover,picture,attending_count,declined_count,interested_count,noreply_count,maybe_count,is_canceled,ticket_uri,category,type",
                "ads" => "id,name,status,creative,created_time,updated_time,campaign_id,adset_id,account_id,tracking_specs,conversion_specs,bid_amount,bid_type,daily_budget,lifetime_budget",
                "insights" => "account_id,campaign_id,adset_id,ad_id,date_start,date_stop,impressions,clicks,spend,reach,frequency,cpp,cpm,ctr,cpc",
                _ => ""
            };
        }

        /// <summary>
        /// Parse JSON response to DataTable
        /// </summary>
        private DataTable ParseJsonToDataTable(string jsonContent, string entityName)
        {
            var dataTable = new DataTable(entityName);
            var metadata = _entityMetadata[entityName.ToLower()];

            // Add columns based on entity metadata
            foreach (var field in metadata.Fields)
            {
                dataTable.Columns.Add(field.Name, GetFieldType(field.Type));
            }

            try
            {
                var jsonDoc = JsonSerializer.Deserialize<JsonElement>(jsonContent);

                if (jsonDoc.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in data.EnumerateArray())
                    {
                        var row = dataTable.NewRow();
                        foreach (var field in metadata.Fields)
                        {
                            if (item.TryGetProperty(field.Name, out var value))
                            {
                                row[field.Name] = ParseJsonValue(value, field.Type);
                            }
                        }
                        dataTable.Rows.Add(row);
                    }
                }
                else if (jsonDoc.ValueKind == JsonValueKind.Object)
                {
                    var row = dataTable.NewRow();
                    foreach (var field in metadata.Fields)
                    {
                        if (jsonDoc.TryGetProperty(field.Name, out var value))
                        {
                            row[field.Name] = ParseJsonValue(value, field.Type);
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
        /// Parse JSON value based on field type
        /// </summary>
        private object ParseJsonValue(JsonElement value, string fieldType)
        {
            return fieldType.ToLower() switch
            {
                "string" => value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString(),
                "int" => value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var intVal) ? intVal : 0,
                "long" => value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var longVal) ? longVal : 0L,
                "boolean" => value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False ? value.GetBoolean() : false,
                "datetime" => value.ValueKind == JsonValueKind.String && DateTime.TryParse(value.GetString(), out var dateVal) ? dateVal : DBNull.Value,
                "decimal" => value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var decimalVal) ? decimalVal : 0m,
                "double" => value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var doubleVal) ? doubleVal : 0.0,
                _ => value.ToString()
            };
        }

        /// <summary>
        /// Get .NET type from field type string
        /// </summary>
        private Type GetFieldType(string fieldType)
        {
            return fieldType.ToLower() switch
            {
                "string" => typeof(string),
                "int" => typeof(int),
                "long" => typeof(long),
                "boolean" => typeof(bool),
                "datetime" => typeof(DateTime),
                "decimal" => typeof(decimal),
                "double" => typeof(double),
                _ => typeof(string)
            };
        }

        /// <summary>
        /// Get list of available entities
        /// </summary>
        public List<string> GetEntities()
        {
            return _entityMetadata.Keys.ToList();
        }

        /// <summary>
        /// Get metadata for a specific entity
        /// </summary>
        public EntityMetadata GetEntityMetadata(string entityName)
        {
            if (_entityMetadata.TryGetValue(entityName.ToLower(), out var metadata))
            {
                return metadata;
            }
            throw new ArgumentException($"Entity '{entityName}' not found");
        }

        /// <summary>
        /// Create a new record in the specified entity
        /// </summary>
        public async Task<bool> CreateAsync(string entityName, Dictionary<string, object> data)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Facebook. Call ConnectAsync first.");
            }

            try
            {
                var endpoint = GetCreateEndpoint(entityName);
                var formData = new MultipartFormDataContent();

                // Add data fields
                foreach (var item in data)
                {
                    formData.Add(new StringContent(item.Value?.ToString() ?? ""), item.Key);
                }

                var response = await _httpClient.PostAsync(endpoint, formData);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create record in '{entityName}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get create endpoint for entity
        /// </summary>
        private string GetCreateEndpoint(string entityName)
        {
            var baseUrl = $"{_config.BaseUrl}/{_config.ApiVersion}";
            return entityName.ToLower() switch
            {
                "posts" => _config.PageId != null ? $"{baseUrl}/{_config.PageId}/feed" : $"{baseUrl}/me/feed",
                "events" => _config.PageId != null ? $"{baseUrl}/{_config.PageId}/events" : $"{baseUrl}/me/events",
                "ads" => $"{baseUrl}/act_{_config.UserId}/ads",
                _ => throw new ArgumentException($"Create not supported for entity: {entityName}")
            };
        }

        /// <summary>
        /// Update an existing record in the specified entity
        /// </summary>
        public async Task<bool> UpdateAsync(string entityName, Dictionary<string, object> data, string id)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Facebook. Call ConnectAsync first.");
            }

            try
            {
                var endpoint = $"{GetEntityEndpoint(entityName)}/{id}";
                var formData = new MultipartFormDataContent();

                // Add data fields
                foreach (var item in data)
                {
                    formData.Add(new StringContent(item.Value?.ToString() ?? ""), item.Key);
                }

                var response = await _httpClient.PostAsync(endpoint, formData);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update record in '{entityName}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Delete a record from the specified entity
        /// </summary>
        public async Task<bool> DeleteAsync(string entityName, string id)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Facebook. Call ConnectAsync first.");
            }

            try
            {
                var endpoint = $"{GetEntityEndpoint(entityName)}/{id}";
                var response = await _httpClient.DeleteAsync(endpoint);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete record from '{entityName}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get connection status
        /// </summary>
        public bool IsConnected()
        {
            return _isConnected;
        }

        /// <summary>
        /// Get data source configuration
        /// </summary>
        public object GetConfig()
        {
            return _config;
        }

        /// <summary>
        /// Set data source configuration
        /// </summary>
        public void SetConfig(object config)
        {
            var newConfig = config as FacebookConfig;
            if (newConfig != null)
            {
                _config.AppId = newConfig.AppId;
                _config.AppSecret = newConfig.AppSecret;
                _config.AccessToken = newConfig.AccessToken;
                _config.PageAccessToken = newConfig.PageAccessToken;
                _config.PageId = newConfig.PageId;
                _config.UserId = newConfig.UserId;
                _config.ApiVersion = newConfig.ApiVersion;
                _config.BaseUrl = newConfig.BaseUrl;
                _config.TimeoutSeconds = newConfig.TimeoutSeconds;
                _config.MaxRetries = newConfig.MaxRetries;
            }
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            DisconnectAsync().Wait();
        }
    }
}
