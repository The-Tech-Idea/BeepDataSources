using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Communication.Telegram.Models
{
    public abstract class TgEntityBase
    {
        [JsonIgnore] public IDataSource DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : TgEntityBase { DataSource = ds; return (T)this; }
    }

    // ---------- Core types ----------

    public sealed class TgUser : TgEntityBase
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("is_bot")] public bool? IsBot { get; set; }
        [JsonPropertyName("first_name")] public string FirstName { get; set; }
        [JsonPropertyName("last_name")] public string LastName { get; set; }
        [JsonPropertyName("username")] public string Username { get; set; }
        [JsonPropertyName("language_code")] public string LanguageCode { get; set; }
        [JsonPropertyName("is_premium")] public bool? IsPremium { get; set; }
    }

    public sealed class TgChat : TgEntityBase
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; } // private, group, supergroup, channel
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("username")] public string Username { get; set; }
        [JsonPropertyName("first_name")] public string FirstName { get; set; }
        [JsonPropertyName("last_name")] public string LastName { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
    }

    public sealed class TgMessage : TgEntityBase
    {
        [JsonPropertyName("message_id")] public int? MessageId { get; set; }
        [JsonPropertyName("date")] public int? DateUnix { get; set; }
        [JsonPropertyName("text")] public string Text { get; set; }
        [JsonPropertyName("from")] public TgUser From { get; set; }
        [JsonPropertyName("chat")] public TgChat Chat { get; set; }
        [JsonPropertyName("photo")] public List<TgPhotoSize> Photo { get; set; } = new();
        [JsonPropertyName("caption")] public string Caption { get; set; }
    }

    public sealed class TgPhotoSize
    {
        [JsonPropertyName("file_id")] public string FileId { get; set; }
        [JsonPropertyName("file_unique_id")] public string FileUniqueId { get; set; }
        [JsonPropertyName("width")] public int? Width { get; set; }
        [JsonPropertyName("height")] public int? Height { get; set; }
        [JsonPropertyName("file_size")] public int? FileSize { get; set; }
    }

    public sealed class TgUpdate : TgEntityBase
    {
        [JsonPropertyName("update_id")] public long? UpdateId { get; set; }
        [JsonPropertyName("message")] public TgMessage Message { get; set; }
        [JsonPropertyName("edited_message")] public TgMessage EditedMessage { get; set; }
        [JsonPropertyName("channel_post")] public TgMessage ChannelPost { get; set; }
        [JsonPropertyName("edited_channel_post")] public TgMessage EditedChannelPost { get; set; }
        // (You can add callback_query, inline_query, etc., as needed)
    }

    public sealed class TgChatMember : TgEntityBase
    {
        [JsonPropertyName("user")] public TgUser User { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; } // creator, administrator, member, restricted, left, kicked
        [JsonPropertyName("is_anonymous")] public bool? IsAnonymous { get; set; }
        [JsonPropertyName("can_manage_chat")] public bool? CanManageChat { get; set; }
    }

    public sealed class TgUserProfilePhotos : TgEntityBase
    {
        [JsonPropertyName("total_count")] public int? TotalCount { get; set; }
        [JsonPropertyName("photos")] public List<List<TgPhotoSize>> Photos { get; set; } = new();
    }

    public sealed class TgFile : TgEntityBase
    {
        [JsonPropertyName("file_id")] public string FileId { get; set; }
        [JsonPropertyName("file_unique_id")] public string FileUniqueId { get; set; }
        [JsonPropertyName("file_size")] public int? FileSize { get; set; }
        [JsonPropertyName("file_path")] public string FilePath { get; set; }
    }

    public sealed class TgWebhookInfo : TgEntityBase
    {
        [JsonPropertyName("url")] public string Url { get; set; }
        [JsonPropertyName("has_custom_certificate")] public bool? HasCustomCertificate { get; set; }
        [JsonPropertyName("pending_update_count")] public int? PendingUpdateCount { get; set; }
        [JsonPropertyName("ip_address")] public string IpAddress { get; set; }
        [JsonPropertyName("last_error_date")] public int? LastErrorDateUnix { get; set; }
        [JsonPropertyName("last_error_message")] public string LastErrorMessage { get; set; }
    }

    public sealed class TgBotCommand : TgEntityBase
    {
        [JsonPropertyName("command")] public string Command { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
    }

    // ---------- Registry ----------

    public static class TgEntityRegistry
    {
        public static readonly Dictionary<string, Type> Types = new(StringComparer.OrdinalIgnoreCase)
        {
            ["updates"] = typeof(TgUpdate),
            ["me"] = typeof(TgUser),
            ["chat"] = typeof(TgChat),
            ["chatMembersCount"] = typeof(int),           // Telegram returns a number in result
            ["chatMember"] = typeof(TgChatMember),
            ["chatAdministrators"] = typeof(TgChatMember),
            ["userProfilePhotos"] = typeof(TgUserProfilePhotos),
            ["file"] = typeof(TgFile),
            ["webhookInfo"] = typeof(TgWebhookInfo),
            ["myCommands"] = typeof(TgBotCommand)
        };

        public static IReadOnlyList<string> Names => new List<string>(Types.Keys);
        public static Type Resolve(string entityName) =>
            entityName != null && Types.TryGetValue(entityName, out var t) ? t : null;
    }
}
