using System.Text.Json.Serialization;

namespace TheTechIdea.Beep.Connectors.Communication.Flock.Models
{
    /// <summary>
    /// Base class for all Flock entities
    /// </summary>
    public abstract class FlockEntityBase
    {
        [JsonIgnore]
        public IDataSource? DataSource { get; set; }

        /// <summary>
        /// Attaches the entity to a data source
        /// </summary>
        public T Attach<T>(IDataSource dataSource) where T : FlockEntityBase
        {
            DataSource = dataSource;
            return (T)this;
        }
    }

    /// <summary>
    /// Flock User entity
    /// </summary>
    public sealed class FlockUser : FlockEntityBase
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("firstName")]
        public string? FirstName { get; set; }

        [JsonPropertyName("lastName")]
        public string? LastName { get; set; }

        [JsonPropertyName("uid")]
        public string? Uid { get; set; }

        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("timezone")]
        public string? Timezone { get; set; }

        [JsonPropertyName("profile")]
        public FlockUserProfile? Profile { get; set; }
    }

    /// <summary>
    /// Flock User Presence entity
    /// </summary>
    public sealed class FlockUserPresence : FlockEntityBase
    {
        [JsonPropertyName("userId")]
        public string? UserId { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("lastSeen")]
        public DateTime? LastSeen { get; set; }
    }

    /// <summary>
    /// Flock Group entity
    /// </summary>
    public sealed class FlockGroup : FlockEntityBase
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("members")]
        public List<FlockUser>? Members { get; set; }

        [JsonPropertyName("created")]
        public DateTime? Created { get; set; }
    }

    /// <summary>
    /// Flock Group Member entity
    /// </summary>
    public sealed class FlockGroupMember : FlockEntityBase
    {
        [JsonPropertyName("groupId")]
        public string? GroupId { get; set; }

        [JsonPropertyName("user")]
        public FlockUser? User { get; set; }

        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("joinedAt")]
        public DateTime? JoinedAt { get; set; }
    }

    /// <summary>
    /// Flock Channel entity
    /// </summary>
    public sealed class FlockChannel : FlockEntityBase
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("isPublic")]
        public bool? IsPublic { get; set; }

        [JsonPropertyName("creator")]
        public FlockUser? Creator { get; set; }

        [JsonPropertyName("members")]
        public List<FlockUser>? Members { get; set; }
    }

    /// <summary>
    /// Flock Channel Member entity
    /// </summary>
    public sealed class FlockChannelMember : FlockEntityBase
    {
        [JsonPropertyName("channelId")]
        public string? ChannelId { get; set; }

        [JsonPropertyName("user")]
        public FlockUser? User { get; set; }

        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("joinedAt")]
        public DateTime? JoinedAt { get; set; }
    }

    /// <summary>
    /// Flock Message entity
    /// </summary>
    public sealed class FlockMessage : FlockEntityBase
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("uid")]
        public string? Uid { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("to")]
        public string? To { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime? Timestamp { get; set; }

        [JsonPropertyName("attachments")]
        public List<FlockAttachment>? Attachments { get; set; }

        [JsonPropertyName("mentions")]
        public List<FlockMention>? Mentions { get; set; }

        [JsonPropertyName("isEdited")]
        public bool? IsEdited { get; set; }

        [JsonPropertyName("from")]
        public FlockMessage? From { get; set; }
    }

    /// <summary>
    /// Flock Message Reaction entity
    /// </summary>
    public sealed class FlockMessageReaction : FlockEntityBase
    {
        [JsonPropertyName("messageId")]
        public string? MessageId { get; set; }

        [JsonPropertyName("emoji")]
        public string? Emoji { get; set; }

        [JsonPropertyName("user")]
        public FlockUser? User { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime? Timestamp { get; set; }
    }

    /// <summary>
    /// Flock Message Reply entity
    /// </summary>
    public sealed class FlockMessageReply : FlockEntityBase
    {
        [JsonPropertyName("parentMessageId")]
        public string? ParentMessageId { get; set; }

        [JsonPropertyName("message")]
        public FlockMessage? Message { get; set; }

        [JsonPropertyName("threadLevel")]
        public int? ThreadLevel { get; set; }
    }

    /// <summary>
    /// Flock File entity
    /// </summary>
    public sealed class FlockFile : FlockEntityBase
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("size")]
        public long? Size { get; set; }

        [JsonPropertyName("downloadUrl")]
        public string? DownloadUrl { get; set; }

        [JsonPropertyName("uploadedBy")]
        public FlockUser? UploadedBy { get; set; }

        [JsonPropertyName("uploadedAt")]
        public DateTime? UploadedAt { get; set; }

        [JsonPropertyName("channelId")]
        public string? ChannelId { get; set; }
    }

    /// <summary>
    /// Flock Contact entity
    /// </summary>
    public sealed class FlockContact : FlockEntityBase
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }

    /// <summary>
    /// Flock App entity
    /// </summary>
    public sealed class FlockApp : FlockEntityBase
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("iconUrl")]
        public string? IconUrl { get; set; }

        [JsonPropertyName("creator")]
        public FlockUser? Creator { get; set; }

        [JsonPropertyName("isPublic")]
        public bool? IsPublic { get; set; }
    }

    /// <summary>
    /// Flock Webhook entity
    /// </summary>
    public sealed class FlockWebhook : FlockEntityBase
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("events")]
        public string? Events { get; set; }

        [JsonPropertyName("creator")]
        public FlockUser? Creator { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("isActive")]
        public bool? IsActive { get; set; }
    }

    /// <summary>
    /// Flock Token entity
    /// </summary>
    public sealed class FlockToken : FlockEntityBase
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("expiresAt")]
        public DateTime? ExpiresAt { get; set; }

        [JsonPropertyName("createdBy")]
        public FlockUser? CreatedBy { get; set; }

        [JsonPropertyName("isActive")]
        public bool? IsActive { get; set; }
    }

    // Supporting classes
    public sealed class FlockUserProfile
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("department")]
        public string? Department { get; set; }

        [JsonPropertyName("image")]
        public string? Image { get; set; }
    }

    public sealed class FlockAttachment
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("size")]
        public long? Size { get; set; }

        [JsonPropertyName("downloadUrl")]
        public string? DownloadUrl { get; set; }
    }

    public sealed class FlockMention
    {
        [JsonPropertyName("user")]
        public FlockUser? User { get; set; }

        [JsonPropertyName("start")]
        public int? Start { get; set; }

        [JsonPropertyName("end")]
        public int? End { get; set; }
    }
}