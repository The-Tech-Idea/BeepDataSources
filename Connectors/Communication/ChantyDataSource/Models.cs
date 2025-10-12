using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Communication.Chanty.Models
{
    /// <summary>
    /// Base class for all Chanty entities with Attach method for Beep framework integration
    /// </summary>
    public abstract class ChantyEntityBase
    {
        /// <summary>
        /// Reference to the data source (ignored during JSON serialization)
        /// </summary>
        [JsonIgnore]
        public IDataSource? DataSource { get; set; }

        /// <summary>
        /// Attaches the entity to a data source
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="dataSource">The data source to attach to</param>
        /// <returns>The entity instance with data source attached</returns>
        public T Attach<T>(IDataSource dataSource) where T : ChantyEntityBase
        {
            DataSource = dataSource;
            return (T)this;
        }
    }

    /// <summary>
    /// Represents a Chanty team
    /// </summary>
    public sealed class ChantyTeam : ChantyEntityBase
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("avatar")]
        public string? Avatar { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("owner")]
        public ChantyUser? Owner { get; set; }
    }

    /// <summary>
    /// Represents a Chanty channel
    /// </summary>
    public sealed class ChantyChannel : ChantyEntityBase
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("is_private")]
        public bool IsPrivate { get; set; }

        [JsonPropertyName("creator")]
        public ChantyUser? Creator { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("members")]
        public List<ChantyUser>? Members { get; set; }
    }

    /// <summary>
    /// Represents a Chanty message
    /// </summary>
    public sealed class ChantyMessage : ChantyEntityBase
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("sender")]
        public ChantyUser? Sender { get; set; }

        [JsonPropertyName("channel_id")]
        public string? ChannelId { get; set; }

        [JsonPropertyName("sent_at")]
        public DateTime SentAt { get; set; }

        [JsonPropertyName("attachments")]
        public List<ChantyAttachment>? Attachments { get; set; }

        [JsonPropertyName("reactions")]
        public List<ChantyReaction>? Reactions { get; set; }

        [JsonPropertyName("is_edited")]
        public bool IsEdited { get; set; }

        [JsonPropertyName("edited_at")]
        public DateTime? EditedAt { get; set; }
    }

    /// <summary>
    /// Represents a Chanty user
    /// </summary>
    public sealed class ChantyUser : ChantyEntityBase
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string? LastName { get; set; }

        [JsonPropertyName("avatar")]
        public string? Avatar { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("last_seen")]
        public DateTime LastSeen { get; set; }
    }

    /// <summary>
    /// Represents a Chanty team member
    /// </summary>
    public sealed class ChantyTeamMember : ChantyEntityBase
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("user_id")]
        public string? UserId { get; set; }

        [JsonPropertyName("team_id")]
        public string? TeamId { get; set; }

        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("joined_at")]
        public DateTime JoinedAt { get; set; }

        [JsonPropertyName("is_owner")]
        public bool IsOwner { get; set; }

        [JsonPropertyName("user")]
        public ChantyUser? User { get; set; }
    }

    /// <summary>
    /// Represents a Chanty channel member
    /// </summary>
    public sealed class ChantyChannelMember : ChantyEntityBase
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("user_id")]
        public string? UserId { get; set; }

        [JsonPropertyName("channel_id")]
        public string? ChannelId { get; set; }

        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("joined_at")]
        public DateTime JoinedAt { get; set; }

        [JsonPropertyName("is_admin")]
        public bool IsAdmin { get; set; }

        [JsonPropertyName("user")]
        public ChantyUser? User { get; set; }
    }

    /// <summary>
    /// Represents a Chanty message reply
    /// </summary>
    public sealed class ChantyMessageReply : ChantyEntityBase
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("parent_message_id")]
        public string? ParentMessageId { get; set; }

        [JsonPropertyName("sender")]
        public ChantyUser? Sender { get; set; }

        [JsonPropertyName("sent_at")]
        public DateTime SentAt { get; set; }

        [JsonPropertyName("attachments")]
        public List<ChantyAttachment>? Attachments { get; set; }

        [JsonPropertyName("reactions")]
        public List<ChantyReaction>? Reactions { get; set; }

        [JsonPropertyName("is_edited")]
        public bool IsEdited { get; set; }

        [JsonPropertyName("edited_at")]
        public DateTime? EditedAt { get; set; }
    }

    /// <summary>
    /// Represents a Chanty file
    /// </summary>
    public sealed class ChantyFile : ChantyEntityBase
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("channel_id")]
        public string? ChannelId { get; set; }

        [JsonPropertyName("uploaded_by")]
        public ChantyUser? UploadedBy { get; set; }

        [JsonPropertyName("uploaded_at")]
        public DateTime UploadedAt { get; set; }

        [JsonPropertyName("thumbnail_url")]
        public string? ThumbnailUrl { get; set; }
    }

    /// <summary>
    /// Represents a Chanty notification
    /// </summary>
    public sealed class ChantyNotification : ChantyEntityBase
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("is_read")]
        public bool IsRead { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("related_entity_id")]
        public string? RelatedEntityId { get; set; }

        [JsonPropertyName("related_entity_type")]
        public string? RelatedEntityType { get; set; }

        [JsonPropertyName("sender")]
        public ChantyUser? Sender { get; set; }
    }

    /// <summary>
    /// Represents Chanty user settings
    /// </summary>
    public sealed class ChantyUserSettings : ChantyEntityBase
    {
        [JsonPropertyName("user_id")]
        public string? UserId { get; set; }

        [JsonPropertyName("email_notifications")]
        public bool EmailNotifications { get; set; }

        [JsonPropertyName("push_notifications")]
        public bool PushNotifications { get; set; }

        [JsonPropertyName("desktop_notifications")]
        public bool DesktopNotifications { get; set; }

        [JsonPropertyName("theme")]
        public string? Theme { get; set; }

        [JsonPropertyName("language")]
        public string? Language { get; set; }

        [JsonPropertyName("show_online_status")]
        public bool ShowOnlineStatus { get; set; }

        [JsonPropertyName("allow_direct_messages")]
        public bool AllowDirectMessages { get; set; }

        [JsonPropertyName("muted_channels")]
        public List<string>? MutedChannels { get; set; }
    }

    /// <summary>
    /// Represents Chanty team settings
    /// </summary>
    public sealed class ChantyTeamSettings : ChantyEntityBase
    {
        [JsonPropertyName("team_id")]
        public string? TeamId { get; set; }

        [JsonPropertyName("allow_public_channels")]
        public bool AllowPublicChannels { get; set; }

        [JsonPropertyName("require_email_verification")]
        public bool RequireEmailVerification { get; set; }

        [JsonPropertyName("allow_guest_users")]
        public bool AllowGuestUsers { get; set; }

        [JsonPropertyName("default_channel_name")]
        public string? DefaultChannelName { get; set; }

        [JsonPropertyName("max_file_size")]
        public int MaxFileSize { get; set; }

        [JsonPropertyName("allowed_domains")]
        public List<string>? AllowedDomains { get; set; }

        [JsonPropertyName("two_factor_required")]
        public bool TwoFactorRequired { get; set; }
    }

    /// <summary>
    /// Represents a Chanty integration
    /// </summary>
    public sealed class ChantyIntegration : ChantyEntityBase
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("provider")]
        public string? Provider { get; set; }

        [JsonPropertyName("is_enabled")]
        public bool IsEnabled { get; set; }

        [JsonPropertyName("config")]
        public string? Config { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("created_by")]
        public ChantyUser? CreatedBy { get; set; }
    }

    /// <summary>
    /// Represents a Chanty webhook
    /// </summary>
    public sealed class ChantyWebhook : ChantyEntityBase
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("secret")]
        public string? Secret { get; set; }

        [JsonPropertyName("events")]
        public List<string>? Events { get; set; }

        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("created_by")]
        public ChantyUser? CreatedBy { get; set; }
    }

    /// <summary>
    /// Represents a Chanty audit log entry
    /// </summary>
    public sealed class ChantyAuditLog : ChantyEntityBase
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("action")]
        public string? Action { get; set; }

        [JsonPropertyName("entity_type")]
        public string? EntityType { get; set; }

        [JsonPropertyName("entity_id")]
        public string? EntityId { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("old_value")]
        public object? OldValue { get; set; }

        [JsonPropertyName("new_value")]
        public object? NewValue { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("user")]
        public ChantyUser? User { get; set; }

        [JsonPropertyName("ip_address")]
        public string? IpAddress { get; set; }

        [JsonPropertyName("user_agent")]
        public string? UserAgent { get; set; }
    }

    // Supporting classes for Chanty API responses

    /// <summary>
    /// Represents a Chanty attachment
    /// </summary>
    public sealed class ChantyAttachment : ChantyEntityBase
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }
    }

    /// <summary>
    /// Represents a Chanty reaction
    /// </summary>
    public sealed class ChantyReaction : ChantyEntityBase
    {
        [JsonPropertyName("emoji")]
        public string? Emoji { get; set; }

        [JsonPropertyName("user")]
        public ChantyUser? User { get; set; }

        [JsonPropertyName("reacted_at")]
        public DateTime ReactedAt { get; set; }
    }
}