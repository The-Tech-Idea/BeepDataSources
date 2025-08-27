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

namespace BeepDM.Connectors.Communication.Slack
{
    public class SlackConfig
    {
        public string BotToken { get; set; }
        public string UserToken { get; set; }
        public string TeamId { get; set; }
    }

    public class SlackDataSource : IDataSource
    {
        private readonly ILogger<SlackDataSource> _logger;
        private HttpClient _httpClient;
        private SlackConfig _config;
        private bool _isConnected;

        public string DataSourceName => "Slack";
        public string DataSourceType => "Communication";
        public string Version => "1.0.0";
        public string Description => "Slack Team Communication Platform Data Source";

        public SlackDataSource(ILogger<SlackDataSource> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> ConnectAsync(Dictionary<string, object> parameters)
        {
            try
            {
                if (!parameters.ContainsKey("BotToken") && !parameters.ContainsKey("UserToken"))
                {
                    throw new ArgumentException("Either BotToken or UserToken is required");
                }

                _config = new SlackConfig
                {
                    BotToken = parameters.ContainsKey("BotToken") ? parameters["BotToken"].ToString() : null,
                    UserToken = parameters.ContainsKey("UserToken") ? parameters["UserToken"].ToString() : null,
                    TeamId = parameters.ContainsKey("TeamId") ? parameters["TeamId"].ToString() : null
                };

                var handler = new HttpClientHandler();
                _httpClient = new HttpClient(handler)
                {
                    BaseAddress = new Uri("https://slack.com/api/")
                };

                // Set authentication header
                var token = _config.BotToken ?? _config.UserToken;
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                _httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");

                // Test connection by getting team info
                var response = await _httpClient.GetAsync("team.info");
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var jsonDoc = JsonDocument.Parse(jsonString);
                    if (jsonDoc.RootElement.GetProperty("ok").GetBoolean())
                    {
                        _isConnected = true;
                        _logger.LogInformation("Successfully connected to Slack API");
                        return true;
                    }
                }

                _logger.LogError("Failed to connect to Slack API");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to Slack API");
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
                _logger.LogInformation("Disconnected from Slack API");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from Slack API");
                return false;
            }
        }

        public async Task<DataTable> GetEntityAsync(string entityName, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Slack API");
            }

            try
            {
                string endpoint = GetEndpointForEntity(entityName);
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
                    _logger.LogError($"Slack API error: {response.StatusCode} - {response.ReasonPhrase}");
                    return new DataTable();
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                return ParseJsonToDataTable(jsonString, entityName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting entity {entityName} from Slack");
                throw;
            }
        }

        public async Task<bool> UpdateEntityAsync(string entityName, DataTable data, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Slack API");
            }

            try
            {
                string endpoint = GetEndpointForEntity(entityName);

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
                        var id = row["id"]?.ToString() ?? row["channel"]?.ToString();
                        if (string.IsNullOrEmpty(id))
                        {
                            _logger.LogError("Cannot update entity without ID");
                            continue;
                        }
                        response = await _httpClient.PostAsync($"{endpoint}?id={id}", content);
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
                _logger.LogError(ex, $"Error updating entity {entityName} in Slack");
                return false;
            }
        }

        public async Task<bool> DeleteEntityAsync(string entityName, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Slack API");
            }

            if (!parameters.ContainsKey("id") && !parameters.ContainsKey("channel"))
            {
                throw new ArgumentException("id or channel parameter is required for deletion");
            }

            try
            {
                string endpoint = GetEndpointForEntity(entityName);
                var id = parameters.ContainsKey("id") ? parameters["id"].ToString() : parameters["channel"].ToString();

                var response = await _httpClient.PostAsync($"{endpoint}?id={id}", null);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to delete {entityName} with id {id}: {response.StatusCode} - {response.ReasonPhrase}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting entity {entityName} from Slack");
                return false;
            }
        }

        public async Task<List<string>> GetEntitiesAsync()
        {
            return new List<string>
            {
                "channels",
                "messages",
                "users",
                "files",
                "reactions",
                "teams",
                "groups",
                "im",
                "mpim",
                "bots",
                "apps",
                "auth",
                "conversations",
                "pins",
                "reminders",
                "search",
                "stars",
                "team",
                "usergroups",
                "users"
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
                case "channels":
                    metadata.Rows.Add("id", "string", false, "Channel ID");
                    metadata.Rows.Add("name", "string", false, "Channel name");
                    metadata.Rows.Add("is_channel", "boolean", false, "Whether this is a channel");
                    metadata.Rows.Add("is_group", "boolean", false, "Whether this is a group");
                    metadata.Rows.Add("is_im", "boolean", false, "Whether this is a direct message");
                    metadata.Rows.Add("created", "datetime", false, "Channel creation timestamp");
                    metadata.Rows.Add("creator", "string", false, "User ID of channel creator");
                    metadata.Rows.Add("is_archived", "boolean", false, "Whether the channel is archived");
                    metadata.Rows.Add("is_general", "boolean", false, "Whether this is the #general channel");
                    metadata.Rows.Add("members", "array", false, "List of user IDs in the channel");
                    metadata.Rows.Add("topic", "object", true, "Channel topic information");
                    metadata.Rows.Add("purpose", "object", true, "Channel purpose information");
                    metadata.Rows.Add("num_members", "integer", false, "Number of members in the channel");
                    break;

                case "messages":
                    metadata.Rows.Add("type", "string", false, "Message type");
                    metadata.Rows.Add("ts", "string", false, "Message timestamp");
                    metadata.Rows.Add("user", "string", true, "User ID who sent the message");
                    metadata.Rows.Add("text", "string", true, "Message text");
                    metadata.Rows.Add("channel", "string", false, "Channel ID where message was sent");
                    metadata.Rows.Add("thread_ts", "string", true, "Timestamp of the thread's parent message");
                    metadata.Rows.Add("reply_count", "integer", true, "Number of replies in the thread");
                    metadata.Rows.Add("replies", "array", true, "List of replies in the thread");
                    metadata.Rows.Add("files", "array", true, "List of files attached to the message");
                    metadata.Rows.Add("attachments", "array", true, "List of message attachments");
                    metadata.Rows.Add("reactions", "array", true, "List of reactions to the message");
                    metadata.Rows.Add("edited", "object", true, "Edit information if message was edited");
                    metadata.Rows.Add("deleted_ts", "string", true, "Timestamp if message was deleted");
                    break;

                case "users":
                    metadata.Rows.Add("id", "string", false, "User ID");
                    metadata.Rows.Add("name", "string", false, "Username");
                    metadata.Rows.Add("real_name", "string", true, "Real name");
                    metadata.Rows.Add("display_name", "string", true, "Display name");
                    metadata.Rows.Add("email", "string", true, "Email address");
                    metadata.Rows.Add("team_id", "string", false, "Team ID");
                    metadata.Rows.Add("is_admin", "boolean", false, "Whether user is admin");
                    metadata.Rows.Add("is_owner", "boolean", false, "Whether user is owner");
                    metadata.Rows.Add("is_primary_owner", "boolean", false, "Whether user is primary owner");
                    metadata.Rows.Add("is_restricted", "boolean", false, "Whether user is restricted");
                    metadata.Rows.Add("is_ultra_restricted", "boolean", false, "Whether user is ultra restricted");
                    metadata.Rows.Add("is_bot", "boolean", false, "Whether user is a bot");
                    metadata.Rows.Add("updated", "datetime", false, "Last update timestamp");
                    metadata.Rows.Add("profile", "object", true, "User profile information");
                    metadata.Rows.Add("tz", "string", true, "Timezone");
                    metadata.Rows.Add("tz_label", "string", true, "Timezone label");
                    metadata.Rows.Add("tz_offset", "integer", true, "Timezone offset");
                    break;

                case "files":
                    metadata.Rows.Add("id", "string", false, "File ID");
                    metadata.Rows.Add("created", "datetime", false, "File creation timestamp");
                    metadata.Rows.Add("timestamp", "datetime", false, "File timestamp");
                    metadata.Rows.Add("name", "string", true, "File name");
                    metadata.Rows.Add("title", "string", true, "File title");
                    metadata.Rows.Add("mimetype", "string", false, "File MIME type");
                    metadata.Rows.Add("filetype", "string", false, "File type");
                    metadata.Rows.Add("pretty_type", "string", false, "Pretty file type");
                    metadata.Rows.Add("user", "string", false, "User ID who uploaded the file");
                    metadata.Rows.Add("editable", "boolean", false, "Whether file is editable");
                    metadata.Rows.Add("size", "integer", false, "File size in bytes");
                    metadata.Rows.Add("mode", "string", false, "File mode (hosted, external, snippet, post)");
                    metadata.Rows.Add("is_external", "boolean", false, "Whether file is external");
                    metadata.Rows.Add("external_type", "string", true, "External file type");
                    metadata.Rows.Add("is_public", "boolean", false, "Whether file is public");
                    metadata.Rows.Add("public_url_shared", "boolean", false, "Whether public URL is shared");
                    metadata.Rows.Add("display_as_bot", "boolean", false, "Whether to display as bot");
                    metadata.Rows.Add("username", "string", true, "Username for bot messages");
                    metadata.Rows.Add("url_private", "string", true, "Private URL");
                    metadata.Rows.Add("url_private_download", "string", true, "Private download URL");
                    metadata.Rows.Add("permalink", "string", true, "Public permalink");
                    metadata.Rows.Add("permalink_public", "string", true, "Public permalink (public)");
                    metadata.Rows.Add("edit_link", "string", true, "Edit link");
                    metadata.Rows.Add("preview", "string", true, "File preview");
                    metadata.Rows.Add("preview_highlight", "string", true, "Preview highlight");
                    metadata.Rows.Add("lines", "integer", true, "Number of lines (for text files)");
                    metadata.Rows.Add("lines_more", "integer", true, "Additional lines count");
                    metadata.Rows.Add("is_starred", "boolean", true, "Whether file is starred");
                    metadata.Rows.Add("has_rich_preview", "boolean", false, "Whether file has rich preview");
                    break;

                default:
                    metadata.Rows.Add("id", "string", false, "Unique identifier");
                    metadata.Rows.Add("name", "string", true, "Name");
                    metadata.Rows.Add("created", "datetime", true, "Creation timestamp");
                    metadata.Rows.Add("updated", "datetime", true, "Last update timestamp");
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
                var response = await _httpClient.GetAsync("auth.test");
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var jsonDoc = JsonDocument.Parse(jsonString);
                    return jsonDoc.RootElement.GetProperty("ok").GetBoolean();
                }
                return false;
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
                ["HasBotToken"] = !string.IsNullOrEmpty(_config?.BotToken),
                ["HasUserToken"] = !string.IsNullOrEmpty(_config?.UserToken),
                ["TeamId"] = _config?.TeamId ?? "Not specified"
            };
        }

        private string GetEndpointForEntity(string entityName)
        {
            return entityName.ToLower() switch
            {
                "channels" => "channels.list",
                "messages" => "conversations.history",
                "users" => "users.list",
                "files" => "files.list",
                "reactions" => "reactions.list",
                "teams" => "team.info",
                "groups" => "groups.list",
                "im" => "im.list",
                "mpim" => "mpim.list",
                "bots" => "bots.info",
                "apps" => "apps.list",
                "auth" => "auth.test",
                "conversations" => "conversations.list",
                "pins" => "pins.list",
                "reminders" => "reminders.list",
                "search" => "search.all",
                "stars" => "stars.list",
                "team" => "team.info",
                "usergroups" => "usergroups.list",
                "users" => "users.list",
                _ => $"{entityName}.list"
            };
        }

        private string BuildQueryParameters(Dictionary<string, object> parameters)
        {
            if (parameters == null || !parameters.Any())
            {
                return string.Empty;
            }

            var queryParams = new List<string>();
            foreach (var param in parameters)
            {
                if (param.Value != null)
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

                // Check if the response is successful
                if (!root.GetProperty("ok").GetBoolean())
                {
                    _logger.LogError($"Slack API returned error: {root.GetProperty("error").GetString()}");
                    return dataTable;
                }

                // Handle different response structures based on entity type
                if (root.TryGetProperty("channels", out var channels))
                {
                    // Channels response
                    foreach (var channel in channels.EnumerateArray())
                    {
                        if (dataTable.Columns.Count == 0)
                        {
                            foreach (var property in channel.EnumerateObject())
                            {
                                dataTable.Columns.Add(property.Name, typeof(string));
                            }
                        }

                        var row = dataTable.NewRow();
                        foreach (var property in channel.EnumerateObject())
                        {
                            row[property.Name] = property.Value.ToString();
                        }
                        dataTable.Rows.Add(row);
                    }
                }
                else if (root.TryGetProperty("messages", out var messages))
                {
                    // Messages response
                    foreach (var message in messages.EnumerateArray())
                    {
                        if (dataTable.Columns.Count == 0)
                        {
                            foreach (var property in message.EnumerateObject())
                            {
                                dataTable.Columns.Add(property.Name, typeof(string));
                            }
                        }

                        var row = dataTable.NewRow();
                        foreach (var property in message.EnumerateObject())
                        {
                            row[property.Name] = property.Value.ToString();
                        }
                        dataTable.Rows.Add(row);
                    }
                }
                else if (root.TryGetProperty("members", out var members))
                {
                    // Users response
                    foreach (var member in members.EnumerateArray())
                    {
                        if (dataTable.Columns.Count == 0)
                        {
                            foreach (var property in member.EnumerateObject())
                            {
                                dataTable.Columns.Add(property.Name, typeof(string));
                            }
                        }

                        var row = dataTable.NewRow();
                        foreach (var property in member.EnumerateObject())
                        {
                            row[property.Name] = property.Value.ToString();
                        }
                        dataTable.Rows.Add(row);
                    }
                }
                else if (root.TryGetProperty("files", out var files))
                {
                    // Files response
                    foreach (var file in files.EnumerateArray())
                    {
                        if (dataTable.Columns.Count == 0)
                        {
                            foreach (var property in file.EnumerateObject())
                            {
                                dataTable.Columns.Add(property.Name, typeof(string));
                            }
                        }

                        var row = dataTable.NewRow();
                        foreach (var property in file.EnumerateObject())
                        {
                            row[property.Name] = property.Value.ToString();
                        }
                        dataTable.Rows.Add(row);
                    }
                }
                else
                {
                    // Single object response or other structure
                    if (dataTable.Columns.Count == 0)
                    {
                        foreach (var property in root.EnumerateObject())
                        {
                            if (property.Name != "ok" && property.Name != "error")
                            {
                                dataTable.Columns.Add(property.Name, typeof(string));
                            }
                        }
                    }

                    var row = dataTable.NewRow();
                    foreach (var property in root.EnumerateObject())
                    {
                        if (property.Name != "ok" && property.Name != "error" && dataTable.Columns.Contains(property.Name))
                        {
                            row[property.Name] = property.Value.ToString();
                        }
                    }
                    if (row.ItemArray.Any(item => item != null && !string.IsNullOrEmpty(item.ToString())))
                    {
                        dataTable.Rows.Add(row);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing JSON response from Slack");
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
    }
}
