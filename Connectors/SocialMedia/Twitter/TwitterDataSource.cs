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

namespace BeepDataSources.Twitter
{
    /// <summary>
    /// Configuration class for Twitter data source
    /// </summary>
    public class TwitterConfig
    {
        /// <summary>
        /// Twitter API Key (Consumer Key)
        /// </summary>
        public string ApiKey { get; set; } = "";

        /// <summary>
        /// Twitter API Secret (Consumer Secret)
        /// </summary>
        public string ApiSecret { get; set; } = "";

        /// <summary>
        /// Access Token
        /// </summary>
        public string AccessToken { get; set; } = "";

        /// <summary>
        /// Access Token Secret
        /// </summary>
        public string AccessTokenSecret { get; set; } = "";

        /// <summary>
        /// Bearer Token for API v2
        /// </summary>
        public string BearerToken { get; set; } = "";

        /// <summary>
        /// Twitter User ID
        /// </summary>
        public string UserId { get; set; } = "";

        /// <summary>
        /// Twitter Username
        /// </summary>
        public string Username { get; set; } = "";

        /// <summary>
        /// API Version to use
        /// </summary>
        public string ApiVersion { get; set; } = "2";

        /// <summary>
        /// Twitter API endpoint URL
        /// </summary>
        public string BaseUrl { get; set; } = "https://api.twitter.com";

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
    /// Twitter data source implementation for Beep framework
    /// </summary>
    public class TwitterDataSource : IDataSource
    {
        private readonly TwitterConfig _config;
        private HttpClient _httpClient;
        private bool _isConnected;
        private readonly Dictionary<string, EntityMetadata> _entityMetadata;

        /// <summary>
        /// Constructor for TwitterDataSource
        /// </summary>
        /// <param name="config">Configuration object</param>
        public TwitterDataSource(object config)
        {
            _config = config as TwitterConfig ?? new TwitterConfig();
            _entityMetadata = InitializeEntityMetadata();
        }

        /// <summary>
        /// Initialize entity metadata for Twitter entities
        /// </summary>
        private Dictionary<string, EntityMetadata> InitializeEntityMetadata()
        {
            var metadata = new Dictionary<string, EntityMetadata>();

            // Tweets entity
            metadata["tweets"] = new EntityMetadata
            {
                EntityName = "tweets",
                DisplayName = "Tweets",
                PrimaryKey = "id",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Tweet ID" },
                    new EntityField { Name = "text", Type = "string", DisplayName = "Tweet Text" },
                    new EntityField { Name = "created_at", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "author_id", Type = "string", DisplayName = "Author ID" },
                    new EntityField { Name = "conversation_id", Type = "string", DisplayName = "Conversation ID" },
                    new EntityField { Name = "in_reply_to_user_id", Type = "string", DisplayName = "Reply To User ID" },
                    new EntityField { Name = "referenced_tweets", Type = "string", DisplayName = "Referenced Tweets" },
                    new EntityField { Name = "attachments", Type = "string", DisplayName = "Attachments" },
                    new EntityField { Name = "geo", Type = "string", DisplayName = "Geo Location" },
                    new EntityField { Name = "context_annotations", Type = "string", DisplayName = "Context Annotations" },
                    new EntityField { Name = "entities", Type = "string", DisplayName = "Entities" },
                    new EntityField { Name = "public_metrics", Type = "string", DisplayName = "Public Metrics" },
                    new EntityField { Name = "non_public_metrics", Type = "string", DisplayName = "Non Public Metrics" },
                    new EntityField { Name = "organic_metrics", Type = "string", DisplayName = "Organic Metrics" },
                    new EntityField { Name = "promoted_metrics", Type = "string", DisplayName = "Promoted Metrics" },
                    new EntityField { Name = "possibly_sensitive", Type = "boolean", DisplayName = "Possibly Sensitive" },
                    new EntityField { Name = "lang", Type = "string", DisplayName = "Language" },
                    new EntityField { Name = "source", Type = "string", DisplayName = "Source" },
                    new EntityField { Name = "withheld", Type = "string", DisplayName = "Withheld" },
                    new EntityField { Name = "reply_settings", Type = "string", DisplayName = "Reply Settings" }
                }
            };

            // Users entity
            metadata["users"] = new EntityMetadata
            {
                EntityName = "users",
                DisplayName = "Users",
                PrimaryKey = "id",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "User ID" },
                    new EntityField { Name = "name", Type = "string", DisplayName = "Display Name" },
                    new EntityField { Name = "username", Type = "string", DisplayName = "Username" },
                    new EntityField { Name = "created_at", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "description", Type = "string", DisplayName = "Description" },
                    new EntityField { Name = "entities", Type = "string", DisplayName = "Entities" },
                    new EntityField { Name = "location", Type = "string", DisplayName = "Location" },
                    new EntityField { Name = "pinned_tweet_id", Type = "string", DisplayName = "Pinned Tweet ID" },
                    new EntityField { Name = "profile_image_url", Type = "string", DisplayName = "Profile Image URL" },
                    new EntityField { Name = "protected", Type = "boolean", DisplayName = "Is Protected" },
                    new EntityField { Name = "public_metrics", Type = "string", DisplayName = "Public Metrics" },
                    new EntityField { Name = "url", Type = "string", DisplayName = "Website URL" },
                    new EntityField { Name = "verified", Type = "boolean", DisplayName = "Is Verified" },
                    new EntityField { Name = "verified_type", Type = "string", DisplayName = "Verified Type" },
                    new EntityField { Name = "withheld", Type = "string", DisplayName = "Withheld" }
                }
            };

            // Spaces entity
            metadata["spaces"] = new EntityMetadata
            {
                EntityName = "spaces",
                DisplayName = "Spaces",
                PrimaryKey = "id",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Space ID" },
                    new EntityField { Name = "state", Type = "string", DisplayName = "State" },
                    new EntityField { Name = "title", Type = "string", DisplayName = "Title" },
                    new EntityField { Name = "host_ids", Type = "string", DisplayName = "Host IDs" },
                    new EntityField { Name = "created_at", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "started_at", Type = "datetime", DisplayName = "Started At" },
                    new EntityField { Name = "ended_at", Type = "datetime", DisplayName = "Ended At" },
                    new EntityField { Name = "updated_at", Type = "datetime", DisplayName = "Updated At" },
                    new EntityField { Name = "is_ticketed", Type = "boolean", DisplayName = "Is Ticketed" },
                    new EntityField { Name = "scheduled_start", Type = "datetime", DisplayName = "Scheduled Start" },
                    new EntityField { Name = "speaker_ids", Type = "string", DisplayName = "Speaker IDs" },
                    new EntityField { Name = "invited_user_ids", Type = "string", DisplayName = "Invited User IDs" },
                    new EntityField { Name = "participant_count", Type = "int", DisplayName = "Participant Count" },
                    new EntityField { Name = "subscriber_count", Type = "int", DisplayName = "Subscriber Count" },
                    new EntityField { Name = "topic_ids", Type = "string", DisplayName = "Topic IDs" },
                    new EntityField { Name = "lang", Type = "string", DisplayName = "Language" }
                }
            };

            // Lists entity
            metadata["lists"] = new EntityMetadata
            {
                EntityName = "lists",
                DisplayName = "Lists",
                PrimaryKey = "id",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "List ID" },
                    new EntityField { Name = "name", Type = "string", DisplayName = "List Name" },
                    new EntityField { Name = "description", Type = "string", DisplayName = "Description" },
                    new EntityField { Name = "owner_id", Type = "string", DisplayName = "Owner ID" },
                    new EntityField { Name = "created_at", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "follower_count", Type = "int", DisplayName = "Follower Count" },
                    new EntityField { Name = "member_count", Type = "int", DisplayName = "Member Count" },
                    new EntityField { Name = "private", Type = "boolean", DisplayName = "Is Private" }
                }
            };

            // Analytics entity
            metadata["analytics"] = new EntityMetadata
            {
                EntityName = "analytics",
                DisplayName = "Analytics",
                PrimaryKey = "id",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Analytics ID" },
                    new EntityField { Name = "tweet_id", Type = "string", DisplayName = "Tweet ID" },
                    new EntityField { Name = "date", Type = "datetime", DisplayName = "Date" },
                    new EntityField { Name = "impressions", Type = "int", DisplayName = "Impressions" },
                    new EntityField { Name = "engagements", Type = "int", DisplayName = "Engagements" },
                    new EntityField { Name = "engagement_rate", Type = "decimal", DisplayName = "Engagement Rate" },
                    new EntityField { Name = "retweets", Type = "int", DisplayName = "Retweets" },
                    new EntityField { Name = "replies", Type = "int", DisplayName = "Replies" },
                    new EntityField { Name = "likes", Type = "int", DisplayName = "Likes" },
                    new EntityField { Name = "clicks", Type = "int", DisplayName = "Clicks" },
                    new EntityField { Name = "card_clicks", Type = "int", DisplayName = "Card Clicks" },
                    new EntityField { Name = "follows", Type = "int", DisplayName = "Follows" },
                    new EntityField { Name = "unfollows", Type = "int", DisplayName = "Unfollows" },
                    new EntityField { Name = "qualified_impressions", Type = "int", DisplayName = "Qualified Impressions" }
                }
            };

            return metadata;
        }

        /// <summary>
        /// Connect to Twitter API
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_config.BearerToken) &&
                    (string.IsNullOrEmpty(_config.ApiKey) || string.IsNullOrEmpty(_config.ApiSecret)))
                {
                    throw new ArgumentException("Bearer Token or API Key/Secret are required");
                }

                // Initialize HTTP client
                _httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
                };

                // Set authorization header
                if (!string.IsNullOrEmpty(_config.BearerToken))
                {
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _config.BearerToken);
                }
                else
                {
                    // Use API Key/Secret for authentication
                    var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.ApiKey}:{_config.ApiSecret}"));
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeader);
                }

                // Test connection by getting user info
                var testUrl = $"{_config.BaseUrl}/{_config.ApiVersion}/users/me";
                var response = await _httpClient.GetAsync(testUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var userInfo = JsonSerializer.Deserialize<JsonElement>(content);

                    if (userInfo.TryGetProperty("data", out var data) &&
                        data.TryGetProperty("id", out var userId))
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
                throw new Exception($"Failed to connect to Twitter: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Disconnect from Twitter API
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
                throw new Exception($"Failed to disconnect from Twitter: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get entity data from Twitter
        /// </summary>
        public async Task<DataTable> GetEntityAsync(string entityName, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Twitter. Call ConnectAsync first.");
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
                    throw new Exception($"Twitter API request failed: {response.StatusCode} - {response.ReasonPhrase}");
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
                "tweets" => $"{baseUrl}/tweets",
                "users" => $"{baseUrl}/users",
                "spaces" => $"{baseUrl}/spaces",
                "lists" => $"{baseUrl}/lists",
                "analytics" => $"{baseUrl}/tweets/{_config.UserId}/analytics",
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
                queryParams.Add(fields);
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
                "tweets" => "tweet.fields=created_at,author_id,conversation_id,in_reply_to_user_id,referenced_tweets,attachments,geo,context_annotations,entities,public_metrics,non_public_metrics,organic_metrics,promoted_metrics,possibly_sensitive,lang,source,withheld,reply_settings&expansions=author_id,referenced_tweets.id,referenced_tweets.id.author_id,entities.mentions.username,attachments.poll_ids,attachments.media_keys,geo.place_id&user.fields=created_at,description,entities,id,location,name,pinned_tweet_id,profile_image_url,protected,public_metrics,url,username,verified,verified_type,withheld",
                "users" => "user.fields=created_at,description,entities,id,location,name,pinned_tweet_id,profile_image_url,protected,public_metrics,url,username,verified,verified_type,withheld&tweet.fields=created_at,author_id,conversation_id,entities,id,in_reply_to_user_id,lang,possibly_sensitive,public_metrics,referenced_tweets,source,text,withheld",
                "spaces" => "space.fields=created_at,creator_id,ended_at,host_ids,invited_user_ids,is_ticketed,lang,participant_count,scheduled_start,speaker_ids,started_at,state,title,topic_ids,updated_at",
                "lists" => "list.fields=created_at,description,follower_count,id,member_count,name,owner_id,private",
                "analytics" => "",
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

                if (jsonDoc.TryGetProperty("data", out var data))
                {
                    if (data.ValueKind == JsonValueKind.Array)
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
                    else if (data.ValueKind == JsonValueKind.Object)
                    {
                        var row = dataTable.NewRow();
                        foreach (var field in metadata.Fields)
                        {
                            if (data.TryGetProperty(field.Name, out var value))
                            {
                                row[field.Name] = ParseJsonValue(value, field.Type);
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
                throw new InvalidOperationException("Not connected to Twitter. Call ConnectAsync first.");
            }

            try
            {
                var endpoint = GetCreateEndpoint(entityName);
                var jsonData = JsonSerializer.Serialize(data);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(endpoint, content);
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
                "tweets" => $"{baseUrl}/tweets",
                "lists" => $"{baseUrl}/lists",
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
                throw new InvalidOperationException("Not connected to Twitter. Call ConnectAsync first.");
            }

            try
            {
                var endpoint = $"{GetEntityEndpoint(entityName)}/{id}";
                var jsonData = JsonSerializer.Serialize(data);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync(endpoint, content);
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
                throw new InvalidOperationException("Not connected to Twitter. Call ConnectAsync first.");
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
            var newConfig = config as TwitterConfig;
            if (newConfig != null)
            {
                _config.ApiKey = newConfig.ApiKey;
                _config.ApiSecret = newConfig.ApiSecret;
                _config.AccessToken = newConfig.AccessToken;
                _config.AccessTokenSecret = newConfig.AccessTokenSecret;
                _config.BearerToken = newConfig.BearerToken;
                _config.UserId = newConfig.UserId;
                _config.Username = newConfig.Username;
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
