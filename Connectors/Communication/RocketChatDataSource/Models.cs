using System.Text.Json.Serialization;

namespace TheTechIdea.Beep.Connectors.Communication.RocketChat.Models
{
    /// <summary>
    /// Base class for all Rocket.Chat entities
    /// </summary>
    public abstract class RocketChatEntityBase
    {
        [JsonIgnore]
        public IDataSource? DataSource { get; set; }

        /// <summary>
        /// Attaches the entity to a data source
        /// </summary>
        public T Attach<T>(IDataSource dataSource) where T : RocketChatEntityBase
        {
            DataSource = dataSource;
            return (T)this;
        }
    }

    /// <summary>
    /// Rocket.Chat User entity
    /// </summary>
    public sealed class RocketChatUser : RocketChatEntityBase
    {
        [JsonPropertyName("_id")]
        public string? Id { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("utcOffset")]
        public string? UtcOffset { get; set; }

        [JsonPropertyName("active")]
        public bool Active { get; set; }

        [JsonPropertyName("roles")]
        public List<string>? Roles { get; set; }

        [JsonPropertyName("settings")]
        public RocketChatUserSettings? Settings { get; set; }

        [JsonPropertyName("emails")]
        public List<RocketChatUserEmails>? Emails { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("lastLogin")]
        public DateTime? LastLogin { get; set; }
    }

    /// <summary>
    /// Rocket.Chat Channel entity
    /// </summary>
    public sealed class RocketChatChannel : RocketChatEntityBase
    {
        [JsonPropertyName("_id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("fname")]
        public string? Fname { get; set; }

        [JsonPropertyName("t")]
        public string? T { get; set; }

        [JsonPropertyName("msgs")]
        public int Msgs { get; set; }

        [JsonPropertyName("usersCount")]
        public int UsersCount { get; set; }

        [JsonPropertyName("u")]
        public RocketChatUserRef? U { get; set; }

        [JsonPropertyName("ts")]
        public DateTime? Ts { get; set; }

        [JsonPropertyName("ro")]
        public bool Ro { get; set; }

        [JsonPropertyName("sysMes")]
        public bool SysMes { get; set; }

        [JsonPropertyName("default")]
        public bool Default { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("lm")]
        public DateTime? Lm { get; set; }
    }

    /// <summary>
    /// Rocket.Chat Message entity
    /// </summary>
    public sealed class RocketChatMessage : RocketChatEntityBase
    {
        [JsonPropertyName("_id")]
        public string? Id { get; set; }

        [JsonPropertyName("rid")]
        public string? Rid { get; set; }

        [JsonPropertyName("msg")]
        public string? Msg { get; set; }

        [JsonPropertyName("ts")]
        public DateTime? Ts { get; set; }

        [JsonPropertyName("u")]
        public RocketChatUserRef? U { get; set; }

        [JsonPropertyName("alias")]
        public string? Alias { get; set; }

        [JsonPropertyName("attachments")]
        public List<RocketChatAttachment>? Attachments { get; set; }

        [JsonPropertyName("parseUrls")]
        public bool ParseUrls { get; set; }

        [JsonPropertyName("groupable")]
        public bool Groupable { get; set; }

        [JsonPropertyName("tshow")]
        public bool Tshow { get; set; }

        [JsonPropertyName("tmid")]
        public string? Tmid { get; set; }

        [JsonPropertyName("tmsg")]
        public string? Tmsg { get; set; }

        [JsonPropertyName("tcount")]
        public int? Tcount { get; set; }

        [JsonPropertyName("replies")]
        public List<string>? Replies { get; set; }

        [JsonPropertyName("editedBy")]
        public RocketChatUserRef? EditedBy { get; set; }

        [JsonPropertyName("editedAt")]
        public DateTime? EditedAt { get; set; }

        [JsonPropertyName("pinned")]
        public bool Pinned { get; set; }

        [JsonPropertyName("pinnedBy")]
        public RocketChatUserRef? PinnedBy { get; set; }

        [JsonPropertyName("pinnedAt")]
        public DateTime? PinnedAt { get; set; }

        [JsonPropertyName("starred")]
        public bool Starred { get; set; }

        [JsonPropertyName("starredBy")]
        public RocketChatUserRef? StarredBy { get; set; }

        [JsonPropertyName("starredAt")]
        public DateTime? StarredAt { get; set; }

        [JsonPropertyName("mentions")]
        public List<RocketChatUserRef>? Mentions { get; set; }

        [JsonPropertyName("channels")]
        public List<RocketChatChannelRef>? Channels { get; set; }
    }

    /// <summary>
    /// Rocket.Chat Group entity
    /// </summary>
    public sealed class RocketChatGroup : RocketChatEntityBase
    {
        [JsonPropertyName("_id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("fname")]
        public string? Fname { get; set; }

        [JsonPropertyName("t")]
        public string? T { get; set; }

        [JsonPropertyName("msgs")]
        public int Msgs { get; set; }

        [JsonPropertyName("usersCount")]
        public int UsersCount { get; set; }

        [JsonPropertyName("u")]
        public RocketChatUserRef? U { get; set; }

        [JsonPropertyName("ts")]
        public DateTime? Ts { get; set; }

        [JsonPropertyName("ro")]
        public bool Ro { get; set; }

        [JsonPropertyName("sysMes")]
        public bool SysMes { get; set; }

        [JsonPropertyName("default")]
        public bool Default { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("lm")]
        public DateTime? Lm { get; set; }
    }

    /// <summary>
    /// Rocket.Chat IM (Direct Message) entity
    /// </summary>
    public sealed class RocketChatIm : RocketChatEntityBase
    {
        [JsonPropertyName("_id")]
        public string? Id { get; set; }

        [JsonPropertyName("t")]
        public string? T { get; set; }

        [JsonPropertyName("msgs")]
        public int Msgs { get; set; }

        [JsonPropertyName("usersCount")]
        public int UsersCount { get; set; }

        [JsonPropertyName("u")]
        public RocketChatUserRef? U { get; set; }

        [JsonPropertyName("ts")]
        public DateTime? Ts { get; set; }

        [JsonPropertyName("ro")]
        public bool Ro { get; set; }

        [JsonPropertyName("sysMes")]
        public bool SysMes { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("lm")]
        public DateTime? Lm { get; set; }
    }

    /// <summary>
    /// Rocket.Chat Room entity
    /// </summary>
    public sealed class RocketChatRoom : RocketChatEntityBase
    {
        [JsonPropertyName("_id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("fname")]
        public string? Fname { get; set; }

        [JsonPropertyName("t")]
        public string? T { get; set; }

        [JsonPropertyName("msgs")]
        public int Msgs { get; set; }

        [JsonPropertyName("usersCount")]
        public int UsersCount { get; set; }

        [JsonPropertyName("u")]
        public RocketChatUserRef? U { get; set; }

        [JsonPropertyName("ts")]
        public DateTime? Ts { get; set; }

        [JsonPropertyName("ro")]
        public bool Ro { get; set; }

        [JsonPropertyName("sysMes")]
        public bool SysMes { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("lm")]
        public DateTime? Lm { get; set; }
    }

    /// <summary>
    /// Rocket.Chat Subscription entity
    /// </summary>
    public sealed class RocketChatSubscription : RocketChatEntityBase
    {
        [JsonPropertyName("_id")]
        public string? Id { get; set; }

        [JsonPropertyName("rid")]
        public string? Rid { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("fname")]
        public string? Fname { get; set; }

        [JsonPropertyName("t")]
        public string? T { get; set; }

        [JsonPropertyName("msgs")]
        public int Msgs { get; set; }

        [JsonPropertyName("usersCount")]
        public int UsersCount { get; set; }

        [JsonPropertyName("u")]
        public RocketChatUserRef? U { get; set; }

        [JsonPropertyName("ts")]
        public DateTime? Ts { get; set; }

        [JsonPropertyName("ro")]
        public bool Ro { get; set; }

        [JsonPropertyName("alert")]
        public bool Alert { get; set; }

        [JsonPropertyName("open")]
        public bool Open { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("lm")]
        public DateTime? Lm { get; set; }
    }

    /// <summary>
    /// Rocket.Chat Role entity
    /// </summary>
    public sealed class RocketChatRole : RocketChatEntityBase
    {
        [JsonPropertyName("_id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("protected")]
        public bool Protected { get; set; }

        [JsonPropertyName("mandatory2fa")]
        public bool Mandatory2fa { get; set; }

        [JsonPropertyName("scope")]
        public List<string>? Scope { get; set; }
    }

    /// <summary>
    /// Rocket.Chat Permission entity
    /// </summary>
    public sealed class RocketChatPermission : RocketChatEntityBase
    {
        [JsonPropertyName("_id")]
        public string? Id { get; set; }

        [JsonPropertyName("level")]
        public string? Level { get; set; }

        [JsonPropertyName("roles")]
        public List<string>? Roles { get; set; }

        [JsonPropertyName("group")]
        public List<string>? Group { get; set; }
    }

    /// <summary>
    /// Rocket.Chat Setting entity
    /// </summary>
    public sealed class RocketChatSetting : RocketChatEntityBase
    {
        [JsonPropertyName("_id")]
        public string? Id { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("value")]
        public object? Value { get; set; }

        [JsonPropertyName("group")]
        public string? Group { get; set; }

        [JsonPropertyName("public")]
        public bool Public { get; set; }

        [JsonPropertyName("autoupdate")]
        public bool Autoupdate { get; set; }

        [JsonPropertyName("hidden")]
        public bool Hidden { get; set; }

        [JsonPropertyName("blocked")]
        public bool Blocked { get; set; }

        [JsonPropertyName("requiredOnWizard")]
        public bool RequiredOnWizard { get; set; }

        [JsonPropertyName("i18nLabel")]
        public string? I18nLabel { get; set; }

        [JsonPropertyName("i18nDescription")]
        public string? I18nDescription { get; set; }
    }

    /// <summary>
    /// Rocket.Chat Statistics entity
    /// </summary>
    public sealed class RocketChatStatistics : RocketChatEntityBase
    {
        [JsonPropertyName("totalUsers")]
        public int TotalUsers { get; set; }

        [JsonPropertyName("activeUsers")]
        public int ActiveUsers { get; set; }

        [JsonPropertyName("nonActiveUsers")]
        public int NonActiveUsers { get; set; }

        [JsonPropertyName("onlineUsers")]
        public int OnlineUsers { get; set; }

        [JsonPropertyName("awayUsers")]
        public int AwayUsers { get; set; }

        [JsonPropertyName("offlineUsers")]
        public int OfflineUsers { get; set; }

        [JsonPropertyName("totalRooms")]
        public int TotalRooms { get; set; }

        [JsonPropertyName("totalChannels")]
        public int TotalChannels { get; set; }

        [JsonPropertyName("totalPrivateGroups")]
        public int TotalPrivateGroups { get; set; }

        [JsonPropertyName("totalDirect")]
        public int TotalDirect { get; set; }

        [JsonPropertyName("totalLivechat")]
        public int TotalLivechat { get; set; }

        [JsonPropertyName("totalMessages")]
        public int TotalMessages { get; set; }

        [JsonPropertyName("totalLivechatMessages")]
        public int TotalLivechatMessages { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }
    }

    /// <summary>
    /// Rocket.Chat Integration entity
    /// </summary>
    public sealed class RocketChatIntegration : RocketChatEntityBase
    {
        [JsonPropertyName("_id")]
        public string? Id { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("channel")]
        public string? Channel { get; set; }

        [JsonPropertyName("script")]
        public string? Script { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Rocket.Chat Webhook entity
    /// </summary>
    public sealed class RocketChatWebhook : RocketChatEntityBase
    {
        [JsonPropertyName("_id")]
        public string? Id { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("channel")]
        public string? Channel { get; set; }

        [JsonPropertyName("script")]
        public string? Script { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }
    }

    // Supporting classes
    public sealed class RocketChatUserSettings
    {
        [JsonPropertyName("preferences")]
        public RocketChatUserSettingsPreferences? Preferences { get; set; }
    }

    public sealed class RocketChatUserSettingsPreferences
    {
        [JsonPropertyName("desktopNotifications")]
        public bool DesktopNotifications { get; set; }

        [JsonPropertyName("pushNotifications")]
        public bool PushNotifications { get; set; }

        [JsonPropertyName("emailNotifications")]
        public bool EmailNotifications { get; set; }

        [JsonPropertyName("language")]
        public string? Language { get; set; }
    }

    public sealed class RocketChatUserEmails
    {
        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("verified")]
        public bool Verified { get; set; }
    }

    public sealed class RocketChatAttachment
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("title_link")]
        public string? TitleLink { get; set; }

        [JsonPropertyName("title_link_download")]
        public bool TitleLinkDownload { get; set; }

        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; set; }

        [JsonPropertyName("image_type")]
        public string? ImageType { get; set; }

        [JsonPropertyName("image_size")]
        public int ImageSize { get; set; }

        [JsonPropertyName("video_url")]
        public string? VideoUrl { get; set; }

        [JsonPropertyName("video_type")]
        public string? VideoType { get; set; }

        [JsonPropertyName("video_size")]
        public int VideoSize { get; set; }

        [JsonPropertyName("audio_url")]
        public string? AudioUrl { get; set; }

        [JsonPropertyName("audio_type")]
        public string? AudioType { get; set; }

        [JsonPropertyName("audio_size")]
        public int AudioSize { get; set; }
    }

    public sealed class RocketChatUserRef
    {
        [JsonPropertyName("_id")]
        public string? Id { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    public sealed class RocketChatChannelRef
    {
        [JsonPropertyName("_id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}