using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TheTechIdea.Beep.TwitterDataSource.Config;
using TheTechIdea.Beep.TwitterDataSource.Entities;
using TheTechIdea.Beep.WebAPI;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.TwitterDataSource
{
    /// <summary>
    /// Twitter data source implementation using WebAPIDataSource as base class
    /// </summary>
    public class TwitterDataSource : WebAPIDataSource
    {
        private readonly TwitterDataSourceConfig _config;
        private readonly JsonSerializerOptions _jsonOptions;
        private Dictionary<string, EntityStructure> _entityMetadata;

        /// <summary>
        /// Initializes a new instance of the TwitterDataSource class
        /// </summary>
        public TwitterDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per, TwitterDataSourceConfig config)
            : base(datasourcename, logger, pDMEEditor, databasetype, per)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            if (!_config.IsValid())
            {
                throw new ArgumentException("Invalid Twitter configuration. Bearer token or OAuth credentials are required.");
            }

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            // Initialize entity metadata
            _entityMetadata = InitializeEntityMetadata();
            EntitiesNames = _entityMetadata.Keys.ToList();
            Entities = _entityMetadata.Values.ToList();

            // Set up connection properties from config
            if (Dataconnection?.ConnectionProp is WebAPIConnectionProperties webApiProps)
            {
                // Copy config properties to connection properties
                webApiProps.ApiKey = _config.BearerToken;
                webApiProps.ClientId = _config.ConsumerKey;
                webApiProps.ClientSecret = _config.ConsumerSecret;
                webApiProps.UserID = _config.AccessToken;
                webApiProps.Password = _config.AccessTokenSecret;
                webApiProps.ConnectionString = $"https://api.twitter.com/{_config.ApiVersion}";
                webApiProps.TimeoutMs = _config.TimeoutMs;
                webApiProps.MaxRetries = _config.MaxRetries;
                webApiProps.EnableRateLimit = _config.UseRateLimiting;
                webApiProps.RateLimitRequestsPerMinute = _config.RateLimitPer15Min / 15 * 60; // Convert to per minute
            }
        }

        /// <summary>
        /// Initialize entity metadata for Twitter entities
        /// </summary>
        private Dictionary<string, EntityStructure> InitializeEntityMetadata()
        {
            var metadata = new Dictionary<string, EntityStructure>();

            // Tweets entity
            metadata["tweets"] = new EntityStructure
            {
                EntityName = "tweets",
                DatasourceEntityName = "tweets",
                Caption = "Tweets",
                Viewtype = ViewType.Table,
                Fields = new List<EntityField>
                {
                    new EntityField { fieldname = "id", fieldtype = "System.String", IsKey = true, AllowDBNull = false },
                    new EntityField { fieldname = "text", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "created_at", fieldtype = "System.DateTime", AllowDBNull = true },
                    new EntityField { fieldname = "author_id", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "conversation_id", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "in_reply_to_user_id", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "referenced_tweets", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "attachments", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "geo", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "context_annotations", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "entities", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "public_metrics", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "non_public_metrics", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "organic_metrics", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "promoted_metrics", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "possibly_sensitive", fieldtype = "System.Boolean", AllowDBNull = true },
                    new EntityField { fieldname = "lang", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "source", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "withheld", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "reply_settings", fieldtype = "System.String", AllowDBNull = true }
                },
                PrimaryKeys = new List<EntityField>
                {
                    new EntityField { fieldname = "id", fieldtype = "System.String", IsKey = true, AllowDBNull = false }
                }
            };

            // Users entity
            metadata["users"] = new EntityStructure
            {
                EntityName = "users",
                DatasourceEntityName = "users",
                Caption = "Users",
                Viewtype = ViewType.Table,
                Fields = new List<EntityField>
                {
                    new EntityField { fieldname = "id", fieldtype = "System.String", IsKey = true, AllowDBNull = false },
                    new EntityField { fieldname = "name", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "username", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "created_at", fieldtype = "System.DateTime", AllowDBNull = true },
                    new EntityField { fieldname = "description", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "entities", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "location", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "pinned_tweet_id", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "profile_image_url", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "protected", fieldtype = "System.Boolean", AllowDBNull = true },
                    new EntityField { fieldname = "public_metrics", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "url", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "verified", fieldtype = "System.Boolean", AllowDBNull = true },
                    new EntityField { fieldname = "verified_type", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "withheld", fieldtype = "System.String", AllowDBNull = true }
                },
                PrimaryKeys = new List<EntityField>
                {
                    new EntityField { fieldname = "id", fieldtype = "System.String", IsKey = true, AllowDBNull = false }
                }
            };

            // Spaces entity
            metadata["spaces"] = new EntityStructure
            {
                EntityName = "spaces",
                DatasourceEntityName = "spaces",
                Caption = "Spaces",
                Viewtype = ViewType.Table,
                Fields = new List<EntityField>
                {
                    new EntityField { fieldname = "id", fieldtype = "System.String", IsKey = true, AllowDBNull = false },
                    new EntityField { fieldname = "state", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "title", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "host_ids", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "created_at", fieldtype = "System.DateTime", AllowDBNull = true },
                    new EntityField { fieldname = "started_at", fieldtype = "System.DateTime", AllowDBNull = true },
                    new EntityField { fieldname = "ended_at", fieldtype = "System.DateTime", AllowDBNull = true },
                    new EntityField { fieldname = "updated_at", fieldtype = "System.DateTime", AllowDBNull = true },
                    new EntityField { fieldname = "is_ticketed", fieldtype = "System.Boolean", AllowDBNull = true },
                    new EntityField { fieldname = "scheduled_start", fieldtype = "System.DateTime", AllowDBNull = true },
                    new EntityField { fieldname = "speaker_ids", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "invited_user_ids", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "participant_count", fieldtype = "System.Int32", AllowDBNull = true },
                    new EntityField { fieldname = "subscriber_count", fieldtype = "System.Int32", AllowDBNull = true },
                    new EntityField { fieldname = "topic_ids", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "lang", fieldtype = "System.String", AllowDBNull = true }
                },
                PrimaryKeys = new List<EntityField>
                {
                    new EntityField { fieldname = "id", fieldtype = "System.String", IsKey = true, AllowDBNull = false }
                }
            };

            // Lists entity
            metadata["lists"] = new EntityStructure
            {
                EntityName = "lists",
                DatasourceEntityName = "lists",
                Caption = "Lists",
                Viewtype = ViewType.Table,
                Fields = new List<EntityField>
                {
                    new EntityField { fieldname = "id", fieldtype = "System.String", IsKey = true, AllowDBNull = false },
                    new EntityField { fieldname = "name", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "description", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "owner_id", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "created_at", fieldtype = "System.DateTime", AllowDBNull = true },
                    new EntityField { fieldname = "follower_count", fieldtype = "System.Int32", AllowDBNull = true },
                    new EntityField { fieldname = "member_count", fieldtype = "System.Int32", AllowDBNull = true },
                    new EntityField { fieldname = "private", fieldtype = "System.Boolean", AllowDBNull = true }
                },
                PrimaryKeys = new List<EntityField>
                {
                    new EntityField { fieldname = "id", fieldtype = "System.String", IsKey = true, AllowDBNull = false }
                }
            };

            // Analytics entity
            metadata["analytics"] = new EntityStructure
            {
                EntityName = "analytics",
                DatasourceEntityName = "analytics",
                Caption = "Analytics",
                Viewtype = ViewType.Table,
                Fields = new List<EntityField>
                {
                    new EntityField { fieldname = "id", fieldtype = "System.String", IsKey = true, AllowDBNull = false },
                    new EntityField { fieldname = "tweet_id", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "date", fieldtype = "System.DateTime", AllowDBNull = true },
                    new EntityField { fieldname = "impressions", fieldtype = "System.Int32", AllowDBNull = true },
                    new EntityField { fieldname = "engagements", fieldtype = "System.Int32", AllowDBNull = true },
                    new EntityField { fieldname = "engagement_rate", fieldtype = "System.Decimal", AllowDBNull = true },
                    new EntityField { fieldname = "retweets", fieldtype = "System.Int32", AllowDBNull = true },
                    new EntityField { fieldname = "replies", fieldtype = "System.Int32", AllowDBNull = true },
                    new EntityField { fieldname = "likes", fieldtype = "System.Int32", AllowDBNull = true },
                    new EntityField { fieldname = "clicks", fieldtype = "System.Int32", AllowDBNull = true },
                    new EntityField { fieldname = "card_clicks", fieldtype = "System.Int32", AllowDBNull = true },
                    new EntityField { fieldname = "follows", fieldtype = "System.Int32", AllowDBNull = true },
                    new EntityField { fieldname = "unfollows", fieldtype = "System.Int32", AllowDBNull = true },
                    new EntityField { fieldname = "qualified_impressions", fieldtype = "System.Int32", AllowDBNull = true }
                },
                PrimaryKeys = new List<EntityField>
                {
                    new EntityField { fieldname = "id", fieldtype = "System.String", IsKey = true, AllowDBNull = false }
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
                    (string.IsNullOrEmpty(_config.ConsumerKey) || string.IsNullOrEmpty(_config.ConsumerSecret)))
                {
                    throw new ArgumentException("Bearer Token or API Key/Secret are required");
                }

                // Test connection by getting user info
                var baseUrl = Dataconnection?.ConnectionProp?.ConnectionString ?? $"https://api.twitter.com/{_config.ApiVersion}";
                var testUrl = $"{baseUrl}/users/me";

                // Use base class connection method
                ConnectionStatus = ConnectionState.Connecting;

                // Use WebAPIDataSource HTTP client - access through reflection or protected method
                var response = await base.GetAsync(testUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var userInfo = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);

                    if (userInfo.TryGetProperty("data", out var data) &&
                        data.TryGetProperty("id", out var userId))
                    {
                        _config.TwitterUserId = userId.GetString();
                        ConnectionStatus = ConnectionState.Open;
                        return true;
                    }
                }

                ConnectionStatus = ConnectionState.Closed;
                return false;
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Closed;
                throw new Exception($"Failed to connect to Twitter: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Disconnect from Twitter API
        /// </summary>
        public Task<bool> DisconnectAsync()
        {
            try
            {
                // Use base class disconnect method
                ConnectionStatus = ConnectionState.Closed;
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to disconnect from Twitter: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get entity data from Twitter
        /// </summary>
        public async Task<DataTable> GetEntityAsync(string entityName, Dictionary<string, object>? parameters = null)
        {
            if (ConnectionStatus != ConnectionState.Open)
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
                var response = await base.GetAsync(endpoint);

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
        private string GetEntityEndpoint(string entityName, Dictionary<string, object>? parameters = null)
        {
            var baseUrl = Dataconnection?.ConnectionProp?.ConnectionString ?? $"https://api.twitter.com/{_config.ApiVersion}";
            var endpoint = entityName.ToLower() switch
            {
                "tweets" => $"{baseUrl}/tweets",
                "users" => $"{baseUrl}/users",
                "spaces" => $"{baseUrl}/spaces",
                "lists" => $"{baseUrl}/lists",
                "analytics" => $"{baseUrl}/tweets/{_config.TwitterUserId}/analytics",
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
                dataTable.Columns.Add(field.fieldname, GetFieldType(field.fieldtype));
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
                                if (item.TryGetProperty(field.fieldname, out var value))
                                {
                                    row[field.fieldname] = ParseJsonValue(value, field.fieldtype);
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
                            if (data.TryGetProperty(field.fieldname, out var value))
                            {
                                row[field.fieldname] = ParseJsonValue(value, field.fieldtype);
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
        public EntityStructure GetEntityMetadata(string entityName)
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
        public Task<bool> CreateAsync(string entityName, Dictionary<string, object> data)
        {
            if (ConnectionStatus != ConnectionState.Open)
            {
                throw new InvalidOperationException("Not connected to Twitter. Call ConnectAsync first.");
            }

            try
            {
                // Use high-level InsertEntity method from base
                var res = base.InsertEntity(entityName, data);
                return Task.FromResult(res != null && !string.Equals(res.Flag.ToString(), "Failed", StringComparison.OrdinalIgnoreCase));
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
            var baseUrl = Dataconnection?.ConnectionProp?.ConnectionString ?? $"https://api.twitter.com/{_config.ApiVersion}";
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
            if (ConnectionStatus != ConnectionState.Open)
            {
                throw new InvalidOperationException("Not connected to Twitter. Call ConnectAsync first.");
            }

            try
            {
                // Use base UpdateEntity helper which accepts an object
                var payload = new Dictionary<string, object>(data) { { "id", id } } as object;
                var res = base.UpdateEntity(entityName, payload);
                return await Task.FromResult(res != null && !string.Equals(res.Flag.ToString(), "Failed", StringComparison.OrdinalIgnoreCase));
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
            if (ConnectionStatus != ConnectionState.Open)
            {
                throw new InvalidOperationException("Not connected to Twitter. Call ConnectAsync first.");
            }

            try
            {
                // Use base DeleteEntity helper
                var payload = new { id = id } as object;
                var res = base.DeleteEntity(entityName, payload);
                return await Task.FromResult(res != null && !string.Equals(res.Flag.ToString(), "Failed", StringComparison.OrdinalIgnoreCase));
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
            return ConnectionStatus == ConnectionState.Open;
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
            var newConfig = config as TwitterDataSourceConfig;
            if (newConfig != null)
            {
                _config.BearerToken = newConfig.BearerToken;
                _config.ConsumerKey = newConfig.ConsumerKey;
                _config.ConsumerSecret = newConfig.ConsumerSecret;
                _config.AccessToken = newConfig.AccessToken;
                _config.AccessTokenSecret = newConfig.AccessTokenSecret;
                _config.TwitterUserId = newConfig.TwitterUserId;
                _config.TwitterUsername = newConfig.TwitterUsername;
                _config.ApiVersion = newConfig.ApiVersion;
                _config.ConnectionString = newConfig.ConnectionString;
                _config.TimeoutMs = newConfig.TimeoutMs;
                _config.MaxRetries = newConfig.MaxRetries;
            }
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public new void Dispose()
        {
            DisconnectAsync().Wait();
        }
    }
}
