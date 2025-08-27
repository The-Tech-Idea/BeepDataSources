using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BeepDM.DataManagementModelsStandard;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace BeepDM.Connectors.Communication.Discord
{
    public class DiscordConfig
    {
        public string BotToken { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string GuildId { get; set; } // For guild-specific operations
    }

    public class DiscordDataSource : IDataSource
    {
        private readonly ILogger<DiscordDataSource> _logger;
        private HttpClient _httpClient;
        private DiscordConfig _config;
        private bool _isConnected;

        public string DataSourceName => "Discord";
        public string DataSourceType => "Communication";
        public string Version => "1.0.0";
        public string Description => "Discord Community Communication Platform Data Source";

        public DiscordDataSource(ILogger<DiscordDataSource> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> ConnectAsync(Dictionary<string, object> parameters)
        {
            try
            {
                _config = new DiscordConfig();

                // Support both Bot token and OAuth 2.0 authentication
                if (parameters.ContainsKey("BotToken"))
                {
                    // Bot token authentication (for bots)
                    _config.BotToken = parameters["BotToken"].ToString();
                    _config.AccessToken = _config.BotToken;
                }
                else if (parameters.ContainsKey("ClientId") && parameters.ContainsKey("ClientSecret"))
                {
                    // OAuth 2.0 flow (for user access)
                    _config.ClientId = parameters["ClientId"].ToString();
                    _config.ClientSecret = parameters["ClientSecret"].ToString();

                    if (parameters.ContainsKey("Code"))
                    {
                        // Exchange authorization code for access token
                        var tokenResponse = await ExchangeCodeForTokenAsync(parameters["Code"].ToString());
                        if (tokenResponse == null)
                        {
                            _logger.LogError("Failed to get OAuth access token for Discord");
                            return false;
                        }
                        _config.AccessToken = tokenResponse.AccessToken;
                        _config.RefreshToken = tokenResponse.RefreshToken;
                    }
                    else if (parameters.ContainsKey("AccessToken"))
                    {
                        // Use provided access token
                        _config.AccessToken = parameters["AccessToken"].ToString();
                    }
                    else
                    {
                        throw new ArgumentException("Either Code or AccessToken is required for OAuth 2.0 flow");
                    }
                }
                else
                {
                    throw new ArgumentException("Either BotToken for bot authentication or (ClientId, ClientSecret) for OAuth 2.0 is required");
                }

                _config.GuildId = parameters.ContainsKey("GuildId") ? parameters["GuildId"].ToString() : null;

                var handler = new HttpClientHandler();
                _httpClient = new HttpClient(handler)
                {
                    BaseAddress = new Uri("https://discord.com/api/v10/")
                };

                // Set appropriate authorization header
                if (!string.IsNullOrEmpty(_config.BotToken))
                {
                    _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bot {_config.AccessToken}");
                }
                else
                {
                    _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.AccessToken}");
                }

                _httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "DiscordBot (https://example.com, 1.0.0)");

                // Test connection by getting current user/bot info
                var response = await _httpClient.GetAsync("@me");
                if (response.IsSuccessStatusCode)
                {
                    _isConnected = true;
                    _logger.LogInformation("Successfully connected to Discord API");
                    return true;
                }
                else
                {
                    _logger.LogError($"Failed to connect to Discord API: {response.StatusCode} - {response.ReasonPhrase}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to Discord API");
                return false;
            }
        }

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
                _logger.LogInformation("Disconnected from Discord API");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from Discord API");
                return false;
            }
        }

        public async Task<DataTable> GetEntityAsync(string entityName, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Discord API");
            }

            try
            {
                string endpoint = GetEndpointForEntity(entityName, parameters);
                var queryParams = BuildQueryParameters(parameters);

                HttpResponseMessage response;
                if (!string.IsNullOrEmpty(queryParams))
                {
                    response = await _httpClient.GetAsync($"{endpoint}?{queryParams}");
                }
                else
                {
                    response = await _httpClient.GetAsync(endpoint);
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Discord API error: {response.StatusCode} - {response.ReasonPhrase}");
                    return new DataTable();
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                return ParseJsonToDataTable(jsonString, entityName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting entity {entityName} from Discord");
                throw;
            }
        }

        public async Task<bool> UpdateEntityAsync(string entityName, DataTable data, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Discord API");
            }

            try
            {
                string endpoint = GetEndpointForEntity(entityName, parameters);

                foreach (DataRow row in data.Rows)
                {
                    var jsonData = ConvertDataRowToJson(row, entityName);
                    var content = JsonContent.Create(jsonData);

                    HttpResponseMessage response;
                    if (row.RowState == DataRowState.Added)
                    {
                        response = await _httpClient.PostAsync(endpoint, content);
                    }
                    else if (row.RowState == DataRowState.Modified)
                    {
                        var id = row["id"]?.ToString();
                        if (string.IsNullOrEmpty(id))
                        {
                            _logger.LogError("Cannot update entity without ID");
                            continue;
                        }
                        response = await _httpClient.PatchAsync($"{endpoint}/{id}", content);
                    }
                    else
                    {
                        continue;
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogError($"Failed to update {entityName}: {response.StatusCode} - {response.ReasonPhrase}. Details: {errorContent}");
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating entity {entityName} in Discord");
                return false;
            }
        }

        public async Task<bool> DeleteEntityAsync(string entityName, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Discord API");
            }

            if (!parameters.ContainsKey("id"))
            {
                throw new ArgumentException("id parameter is required for deletion");
            }

            try
            {
                string endpoint = GetEndpointForEntity(entityName, parameters);
                var id = parameters["id"].ToString();

                var response = await _httpClient.DeleteAsync($"{endpoint}/{id}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to delete {entityName} with id {id}: {response.StatusCode} - {response.ReasonPhrase}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting entity {entityName} from Discord");
                return false;
            }
        }

        public async Task<List<string>> GetEntitiesAsync()
        {
            return new List<string>
            {
                "guilds",
                "channels",
                "messages",
                "users",
                "roles",
                "members",
                "emojis",
                "stickers",
                "invites",
                "voice_states",
                "webhooks",
                "applications",
                "audit_logs",
                "integrations",
                "interactions",
                "scheduled_events",
                "threads",
                "stage_instances",
                "auto_moderation"
            };
        }

        public async Task<DataTable> GetEntityMetadataAsync(string entityName)
        {
            var metadata = new DataTable("Metadata");
            metadata.Columns.Add("ColumnName", typeof(string));
            metadata.Columns.Add("DataType", typeof(string));
            metadata.Columns.Add("IsNullable", typeof(bool));
            metadata.Columns.Add("Description", typeof(string));

            // Add common metadata fields based on entity type
            switch (entityName.ToLower())
            {
                case "guilds":
                    metadata.Rows.Add("id", "string", false, "Guild unique identifier");
                    metadata.Rows.Add("name", "string", false, "Guild name");
                    metadata.Rows.Add("icon", "string", true, "Guild icon hash");
                    metadata.Rows.Add("icon_hash", "string", true, "Guild icon hash (new)");
                    metadata.Rows.Add("splash", "string", true, "Guild splash image hash");
                    metadata.Rows.Add("discovery_splash", "string", true, "Guild discovery splash image hash");
                    metadata.Rows.Add("owner", "boolean", true, "Whether current user is owner");
                    metadata.Rows.Add("owner_id", "string", false, "ID of guild owner");
                    metadata.Rows.Add("permissions", "string", true, "Total permissions for current user");
                    metadata.Rows.Add("region", "string", true, "Voice region ID for guild (deprecated)");
                    metadata.Rows.Add("afk_channel_id", "string", true, "ID of AFK channel");
                    metadata.Rows.Add("afk_timeout", "integer", false, "AFK timeout in seconds");
                    metadata.Rows.Add("widget_enabled", "boolean", true, "Whether widget is enabled");
                    metadata.Rows.Add("widget_channel_id", "string", true, "Channel ID for widget");
                    metadata.Rows.Add("verification_level", "integer", false, "Verification level required");
                    metadata.Rows.Add("default_message_notifications", "integer", false, "Default message notification level");
                    metadata.Rows.Add("explicit_content_filter", "integer", false, "Explicit content filter level");
                    metadata.Rows.Add("roles", "array", false, "Roles in the guild");
                    metadata.Rows.Add("emojis", "array", false, "Custom emojis in the guild");
                    metadata.Rows.Add("features", "array", false, "Enabled guild features");
                    metadata.Rows.Add("mfa_level", "integer", false, "Required MFA level");
                    metadata.Rows.Add("application_id", "string", true, "Application ID of guild creator");
                    metadata.Rows.Add("system_channel_id", "string", true, "ID of system channel");
                    metadata.Rows.Add("system_channel_flags", "integer", false, "System channel flags");
                    metadata.Rows.Add("rules_channel_id", "string", true, "ID of rules channel");
                    metadata.Rows.Add("max_presences", "integer", true, "Maximum number of presences");
                    metadata.Rows.Add("max_members", "integer", true, "Maximum number of members");
                    metadata.Rows.Add("vanity_url_code", "string", true, "Vanity URL code");
                    metadata.Rows.Add("description", "string", true, "Guild description");
                    metadata.Rows.Add("banner", "string", true, "Guild banner hash");
                    metadata.Rows.Add("premium_tier", "integer", false, "Premium tier (Server Boost level)");
                    metadata.Rows.Add("premium_subscription_count", "integer", true, "Number of premium subscriptions");
                    metadata.Rows.Add("preferred_locale", "string", false, "Preferred locale of guild");
                    metadata.Rows.Add("public_updates_channel_id", "string", true, "ID of public updates channel");
                    metadata.Rows.Add("max_video_channel_users", "integer", true, "Maximum video channel users");
                    metadata.Rows.Add("max_stage_video_channel_users", "integer", true, "Maximum stage video channel users");
                    metadata.Rows.Add("approximate_member_count", "integer", true, "Approximate member count");
                    metadata.Rows.Add("approximate_presence_count", "integer", true, "Approximate presence count");
                    metadata.Rows.Add("welcome_screen", "object", true, "Welcome screen object");
                    metadata.Rows.Add("nsfw_level", "integer", false, "Guild NSFW level");
                    metadata.Rows.Add("stickers", "array", true, "Custom stickers in the guild");
                    metadata.Rows.Add("premium_progress_bar_enabled", "boolean", false, "Whether premium progress bar is enabled");
                    metadata.Rows.Add("safety_alerts_channel_id", "string", true, "ID of safety alerts channel");
                    break;

                case "channels":
                    metadata.Rows.Add("id", "string", false, "Channel unique identifier");
                    metadata.Rows.Add("type", "integer", false, "Channel type (0=Guild Text, 1=DM, 2=Guild Voice, 3=Group DM, 4=Guild Category, 5=Guild Announcement, 6=Announcement Thread, 10=Public Thread, 11=Private Thread, 12=Guild Stage Voice, 13=Guild Directory, 14=Guild Forum)");
                    metadata.Rows.Add("guild_id", "string", true, "ID of guild containing channel");
                    metadata.Rows.Add("position", "integer", true, "Sorting position of channel");
                    metadata.Rows.Add("permission_overwrites", "array", true, "Permission overwrites for members/roles");
                    metadata.Rows.Add("name", "string", true, "Channel name");
                    metadata.Rows.Add("topic", "string", true, "Channel topic");
                    metadata.Rows.Add("nsfw", "boolean", true, "Whether channel is NSFW");
                    metadata.Rows.Add("last_message_id", "string", true, "ID of last message sent in channel");
                    metadata.Rows.Add("bitrate", "integer", true, "Bitrate of voice channel");
                    metadata.Rows.Add("user_limit", "integer", true, "User limit of voice channel");
                    metadata.Rows.Add("rate_limit_per_user", "integer", true, "Rate limit per user in seconds");
                    metadata.Rows.Add("recipients", "array", true, "Recipients of DM/group DM");
                    metadata.Rows.Add("icon", "string", true, "Icon hash of group DM");
                    metadata.Rows.Add("owner_id", "string", true, "Owner ID of group DM");
                    metadata.Rows.Add("application_id", "string", true, "Application ID of group DM creator");
                    metadata.Rows.Add("managed", "boolean", true, "Whether channel is managed by application");
                    metadata.Rows.Add("parent_id", "string", true, "ID of parent category");
                    metadata.Rows.Add("last_pin_timestamp", "datetime", true, "Timestamp of last pinned message");
                    metadata.Rows.Add("rtc_region", "string", true, "Voice region ID for channel");
                    metadata.Rows.Add("video_quality_mode", "integer", true, "Video quality mode of voice channel");
                    metadata.Rows.Add("message_count", "integer", true, "Approximate message count");
                    metadata.Rows.Add("member_count", "integer", true, "Approximate member count");
                    metadata.Rows.Add("thread_metadata", "object", true, "Thread-specific metadata");
                    metadata.Rows.Add("member", "object", true, "Thread member object for current user");
                    metadata.Rows.Add("default_auto_archive_duration", "integer", true, "Default auto archive duration");
                    metadata.Rows.Add("permissions", "string", true, "Computed permissions for current user");
                    metadata.Rows.Add("flags", "integer", true, "Channel flags");
                    metadata.Rows.Add("available_tags", "array", true, "Available tags for forum channels");
                    metadata.Rows.Add("applied_tags", "array", true, "Applied tags for forum threads");
                    metadata.Rows.Add("default_reaction_emoji", "object", true, "Default reaction emoji");
                    metadata.Rows.Add("default_thread_rate_limit_per_user", "integer", true, "Default thread rate limit per user");
                    metadata.Rows.Add("default_sort_order", "integer", true, "Default sort order");
                    metadata.Rows.Add("default_forum_layout", "integer", true, "Default forum layout");
                    break;

                case "messages":
                    metadata.Rows.Add("id", "string", false, "Message unique identifier");
                    metadata.Rows.Add("channel_id", "string", false, "ID of channel containing message");
                    metadata.Rows.Add("guild_id", "string", true, "ID of guild containing message");
                    metadata.Rows.Add("author", "object", false, "Author of message");
                    metadata.Rows.Add("member", "object", true, "Member properties for message author");
                    metadata.Rows.Add("content", "string", false, "Contents of message");
                    metadata.Rows.Add("timestamp", "datetime", false, "Timestamp when message was sent");
                    metadata.Rows.Add("edited_timestamp", "datetime", true, "Timestamp when message was edited");
                    metadata.Rows.Add("tts", "boolean", false, "Whether message is TTS");
                    metadata.Rows.Add("mention_everyone", "boolean", false, "Whether message mentions everyone");
                    metadata.Rows.Add("mentions", "array", false, "Users specifically mentioned in message");
                    metadata.Rows.Add("mention_roles", "array", false, "Roles specifically mentioned in message");
                    metadata.Rows.Add("mention_channels", "array", true, "Channels specifically mentioned in message");
                    metadata.Rows.Add("attachments", "array", false, "Any attached files");
                    metadata.Rows.Add("embeds", "array", false, "Any embedded content");
                    metadata.Rows.Add("reactions", "array", true, "Reactions to message");
                    metadata.Rows.Add("nonce", "string", true, "Used for validating message was sent");
                    metadata.Rows.Add("pinned", "boolean", false, "Whether message is pinned");
                    metadata.Rows.Add("webhook_id", "string", true, "ID of webhook that sent message");
                    metadata.Rows.Add("type", "integer", false, "Type of message");
                    metadata.Rows.Add("activity", "object", true, "Activity sent with Rich Presence-related messages");
                    metadata.Rows.Add("application", "object", true, "Application sent with Rich Presence-related messages");
                    metadata.Rows.Add("message_reference", "object", true, "Message referenced by this message");
                    metadata.Rows.Add("flags", "integer", true, "Message flags");
                    metadata.Rows.Add("referenced_message", "object", true, "Message that was replied to");
                    metadata.Rows.Add("interaction", "object", true, "Interaction that created this message");
                    metadata.Rows.Add("components", "array", true, "Components for message");
                    metadata.Rows.Add("sticker_items", "array", true, "Sticker items in message");
                    metadata.Rows.Add("stickers", "array", true, "Stickers in message");
                    metadata.Rows.Add("position", "integer", true, "Approximate position of message");
                    metadata.Rows.Add("role_subscription_data", "object", true, "Role subscription data");
                    break;

                case "users":
                    metadata.Rows.Add("id", "string", false, "User unique identifier");
                    metadata.Rows.Add("username", "string", false, "Username (not unique across platform)");
                    metadata.Rows.Add("discriminator", "string", false, "Discriminator (deprecated)");
                    metadata.Rows.Add("global_name", "string", true, "Global display name");
                    metadata.Rows.Add("avatar", "string", true, "User avatar hash");
                    metadata.Rows.Add("bot", "boolean", true, "Whether user is bot");
                    metadata.Rows.Add("system", "boolean", true, "Whether user is official Discord system user");
                    metadata.Rows.Add("mfa_enabled", "boolean", true, "Whether user has two-factor authentication enabled");
                    metadata.Rows.Add("banner", "string", true, "User banner hash");
                    metadata.Rows.Add("accent_color", "integer", true, "User accent color");
                    metadata.Rows.Add("locale", "string", true, "User locale");
                    metadata.Rows.Add("verified", "boolean", true, "Whether user's email is verified");
                    metadata.Rows.Add("email", "string", true, "User email");
                    metadata.Rows.Add("flags", "integer", true, "User flags");
                    metadata.Rows.Add("premium_type", "integer", true, "Type of Nitro subscription");
                    metadata.Rows.Add("public_flags", "integer", true, "Public user flags");
                    metadata.Rows.Add("avatar_decoration_data", "object", true, "Avatar decoration data");
                    break;

                default:
                    metadata.Rows.Add("id", "string", false, "Unique identifier");
                    metadata.Rows.Add("created_at", "datetime", true, "Creation timestamp");
                    metadata.Rows.Add("updated_at", "datetime", true, "Last update timestamp");
                    break;
            }

            return metadata;
        }

        public async Task<bool> TestConnectionAsync()
        {
            if (!_isConnected)
            {
                return false;
            }

            try
            {
                var response = await _httpClient.GetAsync("@me");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<Dictionary<string, object>> GetConnectionInfoAsync()
        {
            return new Dictionary<string, object>
            {
                ["DataSourceName"] = DataSourceName,
                ["DataSourceType"] = DataSourceType,
                ["Version"] = Version,
                ["IsConnected"] = _isConnected,
                ["ClientId"] = _config?.ClientId ?? "Not configured",
                ["GuildId"] = _config?.GuildId ?? "Not configured",
                ["HasBotToken"] = !string.IsNullOrEmpty(_config?.BotToken),
                ["HasAccessToken"] = !string.IsNullOrEmpty(_config?.AccessToken),
                ["AuthType"] = !string.IsNullOrEmpty(_config?.BotToken) ? "Bot Token" : "OAuth 2.0"
            };
        }

        private async Task<TokenResponse> ExchangeCodeForTokenAsync(string code)
        {
            try
            {
                var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://discord.com/api/oauth2/token");
                var tokenParams = new Dictionary<string, string>
                {
                    ["client_id"] = _config.ClientId,
                    ["client_secret"] = _config.ClientSecret,
                    ["grant_type"] = "authorization_code",
                    ["code"] = code
                };

                tokenRequest.Content = new FormUrlEncodedContent(tokenParams);

                using var tokenClient = new HttpClient();
                var tokenResponse = await tokenClient.SendAsync(tokenRequest);

                if (tokenResponse.IsSuccessStatusCode)
                {
                    var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
                    using var tokenDoc = JsonDocument.Parse(tokenJson);
                    var root = tokenDoc.RootElement;

                    return new TokenResponse
                    {
                        AccessToken = root.GetProperty("access_token").GetString(),
                        RefreshToken = root.GetProperty("refresh_token").GetString(),
                        ExpiresIn = root.GetProperty("expires_in").GetInt32()
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exchanging code for token with Discord");
                return null;
            }
        }

        private string GetEndpointForEntity(string entityName, Dictionary<string, object> parameters = null)
        {
            var guildId = parameters?.ContainsKey("guild_id") == true ? parameters["guild_id"].ToString() :
                         _config?.GuildId;

            return entityName.ToLower() switch
            {
                "guilds" => "guilds",
                "channels" => guildId != null ? $"guilds/{guildId}/channels" : "channels",
                "messages" => parameters?.ContainsKey("channel_id") == true ?
                             $"channels/{parameters["channel_id"]}/messages" : "channels/{channel_id}/messages",
                "users" => "users",
                "members" => guildId != null ? $"guilds/{guildId}/members" : "guilds/{guild_id}/members",
                "roles" => guildId != null ? $"guilds/{guildId}/roles" : "guilds/{guild_id}/roles",
                "emojis" => guildId != null ? $"guilds/{guildId}/emojis" : "guilds/{guild_id}/emojis",
                "stickers" => guildId != null ? $"guilds/{guildId}/stickers" : "guilds/{guild_id}/stickers",
                "invites" => guildId != null ? $"guilds/{guildId}/invites" : "guilds/{guild_id}/invites",
                "voice_states" => guildId != null ? $"guilds/{guildId}/voice-states" : "guilds/{guild_id}/voice-states",
                "webhooks" => guildId != null ? $"guilds/{guildId}/webhooks" : "guilds/{guild_id}/webhooks",
                "applications" => "applications",
                "audit_logs" => guildId != null ? $"guilds/{guildId}/audit-logs" : "guilds/{guild_id}/audit-logs",
                "integrations" => guildId != null ? $"guilds/{guildId}/integrations" : "guilds/{guild_id}/integrations",
                "interactions" => "interactions",
                "scheduled_events" => guildId != null ? $"guilds/{guildId}/scheduled-events" : "guilds/{guild_id}/scheduled-events",
                "threads" => parameters?.ContainsKey("channel_id") == true ?
                           $"channels/{parameters["channel_id"]}/threads" : "channels/{channel_id}/threads",
                "stage_instances" => guildId != null ? $"guilds/{guildId}/stage-instances" : "guilds/{guild_id}/stage-instances",
                "auto_moderation" => guildId != null ? $"guilds/{guildId}/auto-moderation/rules" : "guilds/{guild_id}/auto-moderation/rules",
                _ => $"{entityName}"
            };
        }

        private string BuildQueryParameters(Dictionary<string, object> parameters)
        {
            if (parameters == null || !parameters.Any())
            {
                return "limit=100"; // Default pagination
            }

            var queryParams = new List<string> { "limit=100" }; // Default pagination
            foreach (var param in parameters)
            {
                if (param.Value != null && !param.Key.StartsWith("guild_id") && !param.Key.StartsWith("channel_id"))
                {
                    queryParams.Add($"{param.Key}={Uri.EscapeDataString(param.Value.ToString())}");
                }
            }

            return string.Join("&", queryParams);
        }

        private DataTable ParseJsonToDataTable(string jsonString, string entityName)
        {
            var dataTable = new DataTable(entityName);

            try
            {
                using var document = JsonDocument.Parse(jsonString);
                var root = document.RootElement;

                // Handle Discord API response structure
                if (root.ValueKind == JsonValueKind.Array)
                {
                    // Array response (multiple items)
                    foreach (var item in root.EnumerateArray())
                    {
                        if (dataTable.Columns.Count == 0)
                        {
                            foreach (var property in item.EnumerateObject())
                            {
                                dataTable.Columns.Add(property.Name, typeof(string));
                            }
                        }

                        var row = dataTable.NewRow();
                        foreach (var property in item.EnumerateObject())
                        {
                            row[property.Name] = property.Value.ToString();
                        }
                        dataTable.Rows.Add(row);
                    }
                }
                else
                {
                    // Single object response
                    if (dataTable.Columns.Count == 0)
                    {
                        foreach (var property in root.EnumerateObject())
                        {
                            dataTable.Columns.Add(property.Name, typeof(string));
                        }
                    }

                    var row = dataTable.NewRow();
                    foreach (var property in root.EnumerateObject())
                    {
                        row[property.Name] = property.Value.ToString();
                    }
                    dataTable.Rows.Add(row);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing JSON response from Discord");
            }

            return dataTable;
        }

        private object ConvertDataRowToJson(DataRow row, string entityName)
        {
            var jsonObject = new Dictionary<string, object>();

            foreach (DataColumn column in row.Table.Columns)
            {
                if (row[column] != DBNull.Value)
                {
                    jsonObject[column.ColumnName] = row[column];
                }
            }

            return jsonObject;
        }

        public void Dispose()
        {
            DisconnectAsync().Wait();
        }

        private class TokenResponse
        {
            public string AccessToken { get; set; }
            public string RefreshToken { get; set; }
            public int ExpiresIn { get; set; }
        }
    }
}
