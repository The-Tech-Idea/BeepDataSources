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

namespace BeepDM.Connectors.Communication.Telegram
{
    public class TelegramConfig
    {
        public string BotToken { get; set; }
        public string WebhookUrl { get; set; }
        public int? ChatId { get; set; } // For specific chat operations
        public string Username { get; set; } // Bot username
    }

    public class TelegramDataSource : IDataSource
    {
        private readonly ILogger<TelegramDataSource> _logger;
        private HttpClient _httpClient;
        private TelegramConfig _config;
        private bool _isConnected;

        public string DataSourceName => "Telegram";
        public string DataSourceType => "Communication";
        public string Version => "1.0.0";
        public string Description => "Telegram Bot API Communication Platform Data Source";

        public TelegramDataSource(ILogger<TelegramDataSource> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> ConnectAsync(Dictionary<string, object> parameters)
        {
            try
            {
                _config = new TelegramConfig();

                // Bot token is required for Telegram Bot API
                if (!parameters.ContainsKey("BotToken"))
                {
                    throw new ArgumentException("BotToken is required for Telegram Bot API authentication");
                }

                _config.BotToken = parameters["BotToken"].ToString();
                _config.WebhookUrl = parameters.ContainsKey("WebhookUrl") ? parameters["WebhookUrl"].ToString() : null;
                _config.ChatId = parameters.ContainsKey("ChatId") ? Convert.ToInt32(parameters["ChatId"]) : null;

                var handler = new HttpClientHandler();
                _httpClient = new HttpClient(handler)
                {
                    BaseAddress = new Uri($"https://api.telegram.org/bot{_config.BotToken}/")
                };

                _httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");

                // Test connection by getting bot info
                var response = await _httpClient.GetAsync("getMe");
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    using var document = JsonDocument.Parse(jsonString);
                    var result = document.RootElement.GetProperty("result");

                    _config.Username = result.GetProperty("username").GetString();
                    _isConnected = true;
                    _logger.LogInformation($"Successfully connected to Telegram API as bot: {_config.Username}");
                    return true;
                }
                else
                {
                    _logger.LogError($"Failed to connect to Telegram API: {response.StatusCode} - {response.ReasonPhrase}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to Telegram API");
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
                _logger.LogInformation("Disconnected from Telegram API");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from Telegram API");
                return false;
            }
        }

        public async Task<DataTable> GetEntityAsync(string entityName, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Telegram API");
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
                    _logger.LogError($"Telegram API error: {response.StatusCode} - {response.ReasonPhrase}");
                    return new DataTable();
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                return ParseJsonToDataTable(jsonString, entityName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting entity {entityName} from Telegram");
                throw;
            }
        }

        public async Task<bool> UpdateEntityAsync(string entityName, DataTable data, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Telegram API");
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
                        // Telegram API doesn't support direct updates for most entities
                        // This would typically involve sending a new message or editing existing one
                        var messageId = row["message_id"]?.ToString();
                        if (!string.IsNullOrEmpty(messageId))
                        {
                            var editData = new Dictionary<string, object>
                            {
                                ["chat_id"] = row["chat_id"],
                                ["message_id"] = messageId,
                                ["text"] = row["text"]
                            };
                            response = await _httpClient.PostAsync("editMessageText", JsonContent.Create(editData));
                        }
                        else
                        {
                            _logger.LogWarning("Cannot update entity without message_id");
                            continue;
                        }
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
                _logger.LogError(ex, $"Error updating entity {entityName} in Telegram");
                return false;
            }
        }

        public async Task<bool> DeleteEntityAsync(string entityName, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Telegram API");
            }

            if (!parameters.ContainsKey("chat_id") || !parameters.ContainsKey("message_id"))
            {
                throw new ArgumentException("chat_id and message_id parameters are required for deletion");
            }

            try
            {
                var deleteData = new Dictionary<string, object>
                {
                    ["chat_id"] = parameters["chat_id"],
                    ["message_id"] = parameters["message_id"]
                };

                var response = await _httpClient.PostAsync("deleteMessage", JsonContent.Create(deleteData));

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to delete message: {response.StatusCode} - {response.ReasonPhrase}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting entity {entityName} from Telegram");
                return false;
            }
        }

        public async Task<List<string>> GetEntitiesAsync()
        {
            return new List<string>
            {
                "me",
                "updates",
                "messages",
                "chats",
                "users",
                "files",
                "stickers",
                "webhooks",
                "commands",
                "chat_members",
                "chat_member_count",
                "administrators",
                "game_high_scores"
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
                case "me":
                case "users":
                    metadata.Rows.Add("id", "integer", false, "Unique identifier for this user or bot");
                    metadata.Rows.Add("is_bot", "boolean", false, "True, if this user is a bot");
                    metadata.Rows.Add("first_name", "string", false, "User's or bot's first name");
                    metadata.Rows.Add("last_name", "string", true, "User's or bot's last name");
                    metadata.Rows.Add("username", "string", true, "User's or bot's username");
                    metadata.Rows.Add("language_code", "string", true, "IETF language tag of the user's language");
                    metadata.Rows.Add("is_premium", "boolean", true, "True, if this user is a Telegram Premium user");
                    metadata.Rows.Add("added_to_attachment_menu", "boolean", true, "True, if this user added the bot to the attachment menu");
                    metadata.Rows.Add("can_join_groups", "boolean", true, "True, if the bot can be invited to groups");
                    metadata.Rows.Add("can_read_all_group_messages", "boolean", true, "True, if privacy mode is disabled for the bot");
                    metadata.Rows.Add("supports_inline_queries", "boolean", true, "True, if the bot supports inline queries");
                    break;

                case "messages":
                    metadata.Rows.Add("message_id", "integer", false, "Unique message identifier inside this chat");
                    metadata.Rows.Add("message_thread_id", "integer", true, "Unique identifier of a message thread to which the message belongs");
                    metadata.Rows.Add("from", "object", true, "Sender of the message");
                    metadata.Rows.Add("sender_chat", "object", true, "Sender of the message, sent on behalf of a chat");
                    metadata.Rows.Add("date", "integer", false, "Date the message was sent in Unix time");
                    metadata.Rows.Add("chat", "object", false, "Conversation the message belongs to");
                    metadata.Rows.Add("forward_origin", "object", true, "Information about the original message for forwarded messages");
                    metadata.Rows.Add("is_topic_message", "boolean", true, "True, if the message is sent to a forum topic");
                    metadata.Rows.Add("is_automatic_forward", "boolean", true, "True, if the message is a service message");
                    metadata.Rows.Add("reply_to_message", "object", true, "For replies, the original message");
                    metadata.Rows.Add("external_reply", "object", true, "Information about the message that is being replied to");
                    metadata.Rows.Add("quote", "object", true, "For replies that quote part of the original message");
                    metadata.Rows.Add("reply_to_story", "object", true, "For replies to a story");
                    metadata.Rows.Add("via_bot", "object", true, "Bot through which the message was sent");
                    metadata.Rows.Add("edit_date", "integer", true, "Date the message was last edited in Unix time");
                    metadata.Rows.Add("has_protected_content", "boolean", true, "True, if the message can't be forwarded");
                    metadata.Rows.Add("is_from_offline", "boolean", true, "True, if the message was sent by an offline user");
                    metadata.Rows.Add("media_group_id", "string", true, "The unique identifier of a media message group this message belongs to");
                    metadata.Rows.Add("author_signature", "string", true, "Signature of the post author for messages in channels");
                    metadata.Rows.Add("text", "string", true, "For text messages, the actual UTF-8 text of the message");
                    metadata.Rows.Add("entities", "array", true, "For text messages, special entities like usernames, URLs, bot commands, etc.");
                    metadata.Rows.Add("link_preview_options", "object", true, "Options used for link preview generation for the message");
                    metadata.Rows.Add("animation", "object", true, "Message is an animation, information about the animation");
                    metadata.Rows.Add("audio", "object", true, "Message is an audio file, information about the file");
                    metadata.Rows.Add("document", "object", true, "Message is a general file, information about the file");
                    metadata.Rows.Add("photo", "array", true, "Message is a photo, available sizes of the photo");
                    metadata.Rows.Add("sticker", "object", true, "Message is a sticker, information about the sticker");
                    metadata.Rows.Add("story", "object", true, "Message is a forwarded story");
                    metadata.Rows.Add("video", "object", true, "Message is a video, information about the video");
                    metadata.Rows.Add("video_note", "object", true, "Message is a video note, information about the video message");
                    metadata.Rows.Add("voice", "object", true, "Message is a voice message, information about the file");
                    metadata.Rows.Add("caption", "string", true, "Caption for the animation, audio, document, photo, video or voice");
                    metadata.Rows.Add("caption_entities", "array", true, "For messages with a caption, special entities like usernames, URLs, bot commands, etc.");
                    metadata.Rows.Add("has_media_spoiler", "boolean", true, "True, if the message media is covered by a spoiler animation");
                    metadata.Rows.Add("contact", "object", true, "Message is a shared contact, information about the contact");
                    metadata.Rows.Add("dice", "object", true, "Message is a dice with random value");
                    metadata.Rows.Add("game", "object", true, "Message is a game, information about the game");
                    metadata.Rows.Add("poll", "object", true, "Message is a native poll, information about the poll");
                    metadata.Rows.Add("venue", "object", true, "Message is a venue, information about the venue");
                    metadata.Rows.Add("location", "object", true, "Message is a shared location, information about the location");
                    metadata.Rows.Add("new_chat_members", "array", true, "New members that were added to the group or supergroup and information about them");
                    metadata.Rows.Add("left_chat_member", "object", true, "A member was removed from the group, information about them");
                    metadata.Rows.Add("new_chat_title", "string", true, "A chat title was changed to this value");
                    metadata.Rows.Add("new_chat_photo", "array", true, "A chat photo was changed to this value");
                    metadata.Rows.Add("delete_chat_photo", "boolean", true, "Service message: the chat photo was deleted");
                    metadata.Rows.Add("group_chat_created", "boolean", true, "Service message: the group has been created");
                    metadata.Rows.Add("supergroup_chat_created", "boolean", true, "Service message: the supergroup has been created");
                    metadata.Rows.Add("channel_chat_created", "boolean", true, "Service message: the channel has been created");
                    metadata.Rows.Add("message_auto_delete_timer_changed", "object", true, "Service message: auto-delete timer settings changed in the chat");
                    metadata.Rows.Add("migrate_to_chat_id", "integer", true, "The group has been migrated to a supergroup with the specified identifier");
                    metadata.Rows.Add("migrate_from_chat_id", "integer", true, "The supergroup has been migrated from a group with the specified identifier");
                    metadata.Rows.Add("pinned_message", "object", true, "Specified message was pinned");
                    metadata.Rows.Add("invoice", "object", true, "Message is an invoice for a payment, information about the invoice");
                    metadata.Rows.Add("successful_payment", "object", true, "Message is a service message about a successful payment, information about the payment");
                    metadata.Rows.Add("user_shared", "object", true, "Service message: a user was shared with the bot");
                    metadata.Rows.Add("chat_shared", "object", true, "Service message: a chat was shared with the bot");
                    metadata.Rows.Add("connected_website", "string", true, "The domain name of the website on which the user has logged in");
                    metadata.Rows.Add("write_access_allowed", "object", true, "Service message: the user allowed the bot to write messages after adding it to the attachment menu or launching a Web App from a link");
                    metadata.Rows.Add("passport_data", "object", true, "Telegram Passport data");
                    metadata.Rows.Add("proximity_alert_triggered", "object", true, "Service message: user was within proximity of another user");
                    metadata.Rows.Add("boost_added", "object", true, "Service message: user boosted the chat");
                    metadata.Rows.Add("chat_background_set", "object", true, "Service message: chat background was set");
                    metadata.Rows.Add("forum_topic_created", "object", true, "Service message: forum topic created");
                    metadata.Rows.Add("forum_topic_edited", "object", true, "Service message: forum topic edited");
                    metadata.Rows.Add("forum_topic_closed", "object", true, "Service message: forum topic closed");
                    metadata.Rows.Add("forum_topic_reopened", "object", true, "Service message: forum topic reopened");
                    metadata.Rows.Add("general_forum_topic_hidden", "object", true, "Service message: general forum topic hidden");
                    metadata.Rows.Add("general_forum_topic_unhidden", "object", true, "Service message: general forum topic unhidden");
                    metadata.Rows.Add("giveaway_created", "object", true, "Service message: a giveaway was created");
                    metadata.Rows.Add("giveaway", "object", true, "The message is a giveaway message");
                    metadata.Rows.Add("giveaway_winners", "object", true, "A giveaway with public winners was completed");
                    metadata.Rows.Add("giveaway_completed", "object", true, "Service message: giveaway completed");
                    metadata.Rows.Add("video_chat_scheduled", "object", true, "Service message: video chat scheduled");
                    metadata.Rows.Add("video_chat_started", "object", true, "Service message: video chat started");
                    metadata.Rows.Add("video_chat_ended", "object", true, "Service message: video chat ended");
                    metadata.Rows.Add("video_chat_participants_invited", "object", true, "Service message: new participants invited to video chat");
                    metadata.Rows.Add("web_app_data", "object", true, "Service message: data sent by a Web App");
                    metadata.Rows.Add("reply_markup", "object", true, "Inline keyboard attached to the message");
                    break;

                case "chats":
                    metadata.Rows.Add("id", "integer", false, "Unique identifier for this chat");
                    metadata.Rows.Add("type", "string", false, "Type of chat (private, group, supergroup, channel)");
                    metadata.Rows.Add("title", "string", true, "Title, for supergroups, channels and group chats");
                    metadata.Rows.Add("username", "string", true, "Username, for private chats, supergroups and channels if available");
                    metadata.Rows.Add("first_name", "string", true, "First name of the other party in a private chat");
                    metadata.Rows.Add("last_name", "string", true, "Last name of the other party in a private chat");
                    metadata.Rows.Add("is_forum", "boolean", true, "True, if the supergroup chat is a forum (has topics enabled)");
                    metadata.Rows.Add("photo", "object", true, "Chat photo");
                    metadata.Rows.Add("active_usernames", "array", true, "If non-empty, the list of all active chat usernames");
                    metadata.Rows.Add("emoji_status_custom_emoji_id", "string", true, "Custom emoji identifier of emoji status");
                    metadata.Rows.Add("bio", "string", true, "Bio of the other party in a private chat");
                    metadata.Rows.Add("has_private_forwards", "boolean", true, "True, if privacy settings of the other party in the private chat allows to use tg://user?id=<user_id> links only in chats with the user");
                    metadata.Rows.Add("has_restricted_voice_and_video_messages", "boolean", true, "True, if the privacy settings of the other party restrict sending voice and video messages in the private chat");
                    metadata.Rows.Add("join_to_send_messages", "boolean", true, "True, if users need to join the supergroup before they can send messages");
                    metadata.Rows.Add("join_by_request", "boolean", true, "True, if all users directly joining the supergroup need to be approved by supergroup administrators");
                    metadata.Rows.Add("description", "string", true, "Description, for groups, supergroups and channel chats");
                    metadata.Rows.Add("invite_link", "string", true, "Primary invite link, for groups, supergroups and channel chats");
                    metadata.Rows.Add("pinned_message", "object", true, "The most recent pinned message (by sending date)");
                    metadata.Rows.Add("permissions", "object", true, "Default chat member permissions, for groups and supergroups");
                    metadata.Rows.Add("slow_mode_delay", "integer", true, "For supergroups, the minimum allowed delay between consecutive messages sent by each unprivileged user");
                    metadata.Rows.Add("message_auto_delete_time", "integer", true, "The time after which all messages sent to the chat will be automatically deleted");
                    metadata.Rows.Add("has_aggressive_anti_spam_enabled", "boolean", true, "True, if the chat has enabled aggressive anti-spam protection");
                    metadata.Rows.Add("has_hidden_members", "boolean", true, "True, if the supergroup or channel has a hidden member list");
                    metadata.Rows.Add("has_protected_content", "boolean", true, "True, if messages from the chat can't be forwarded to other chats");
                    metadata.Rows.Add("has_visible_history", "boolean", true, "True, if new chat members will have access to old messages");
                    metadata.Rows.Add("sticker_set_name", "string", true, "For supergroups, name of group sticker set");
                    metadata.Rows.Add("can_set_sticker_set", "boolean", true, "True, if the bot can change the group sticker set");
                    metadata.Rows.Add("linked_chat_id", "integer", true, "Unique identifier for the linked chat, i.e. the discussion group identifier for a channel and vice versa");
                    metadata.Rows.Add("location", "object", true, "For supergroups, the location to which the supergroup is connected");
                    break;

                case "updates":
                    metadata.Rows.Add("update_id", "integer", false, "The update's unique identifier");
                    metadata.Rows.Add("message", "object", true, "New incoming message of any kind - text, photo, sticker, etc.");
                    metadata.Rows.Add("edited_message", "object", true, "New version of a message that is known to the bot and was edited");
                    metadata.Rows.Add("channel_post", "object", true, "New incoming channel post of any kind - text, photo, sticker, etc.");
                    metadata.Rows.Add("edited_channel_post", "object", true, "New version of a channel post that is known to the bot and was edited");
                    metadata.Rows.Add("business_connection", "object", true, "The bot was connected to or disconnected from a business account");
                    metadata.Rows.Add("business_message", "object", true, "New message from a connected business account");
                    metadata.Rows.Add("edited_business_message", "object", true, "New version of a message from a connected business account");
                    metadata.Rows.Add("deleted_business_messages", "object", true, "Messages were deleted from a connected business account");
                    metadata.Rows.Add("message_reaction", "object", true, "A reaction to a message was changed by a user");
                    metadata.Rows.Add("message_reaction_count", "object", true, "Reactions to a message with anonymous reactions were changed");
                    metadata.Rows.Add("inline_query", "object", true, "New incoming inline query");
                    metadata.Rows.Add("chosen_inline_result", "object", true, "The result of an inline query that was chosen by a user");
                    metadata.Rows.Add("callback_query", "object", true, "New incoming callback query");
                    metadata.Rows.Add("shipping_query", "object", true, "New incoming shipping query");
                    metadata.Rows.Add("pre_checkout_query", "object", true, "New incoming pre-checkout query");
                    metadata.Rows.Add("purchased_paid_media", "object", true, "A user purchased paid media with a non-empty payload sent by the bot in a non-channel chat");
                    metadata.Rows.Add("poll", "object", true, "New poll state");
                    metadata.Rows.Add("poll_answer", "object", true, "A user changed their answer in a non-anonymous poll");
                    metadata.Rows.Add("my_chat_member", "object", true, "The bot's chat member status was updated in a chat");
                    metadata.Rows.Add("chat_member", "object", true, "A chat member's status was updated in a chat");
                    metadata.Rows.Add("chat_join_request", "object", true, "A request to join the chat has been sent");
                    metadata.Rows.Add("chat_boost", "object", true, "A chat boost was added or changed");
                    metadata.Rows.Add("removed_chat_boost", "object", true, "A boost was removed from a chat");
                    break;

                default:
                    metadata.Rows.Add("id", "integer", false, "Unique identifier");
                    metadata.Rows.Add("date", "integer", true, "Creation timestamp");
                    metadata.Rows.Add("update_date", "integer", true, "Last update timestamp");
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
                var response = await _httpClient.GetAsync("getMe");
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
                ["BotUsername"] = _config?.Username ?? "Not connected",
                ["ChatId"] = _config?.ChatId?.ToString() ?? "Not configured",
                ["HasWebhookUrl"] = !string.IsNullOrEmpty(_config?.WebhookUrl),
                ["AuthType"] = "Bot Token"
            };
        }

        private string GetEndpointForEntity(string entityName)
        {
            return entityName.ToLower() switch
            {
                "me" => "getMe",
                "updates" => "getUpdates",
                "messages" => "sendMessage",
                "chats" => "getChat",
                "users" => "getMe", // Bot info
                "files" => "getFile",
                "stickers" => "getStickerSet",
                "webhooks" => "setWebhook",
                "commands" => "setMyCommands",
                "chat_members" => "getChatMember",
                "chat_member_count" => "getChatMemberCount",
                "administrators" => "getChatAdministrators",
                "game_high_scores" => "getGameHighScores",
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

                // Handle Telegram API response structure
                if (root.TryGetProperty("result", out var resultElement))
                {
                    if (resultElement.ValueKind == JsonValueKind.Array)
                    {
                        // Array response (multiple items)
                        foreach (var item in resultElement.EnumerateArray())
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
                            foreach (var property in resultElement.EnumerateObject())
                            {
                                dataTable.Columns.Add(property.Name, typeof(string));
                            }
                        }

                        var row = dataTable.NewRow();
                        foreach (var property in resultElement.EnumerateObject())
                        {
                            row[property.Name] = property.Value.ToString();
                        }
                        dataTable.Rows.Add(row);
                    }
                }
                else
                {
                    // Direct response (like getMe)
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
                _logger.LogError(ex, "Error parsing JSON response from Telegram");
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
