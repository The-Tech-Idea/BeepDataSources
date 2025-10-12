using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Communication.Twist.Models
{
    /// <summary>
    /// Base class for all Twist entities with Attach method for Beep framework integration
    /// </summary>
    public abstract class TwistEntityBase
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
        public T Attach<T>(IDataSource dataSource) where T : TwistEntityBase
        {
            DataSource = dataSource;
            return (T)this;
        }
    }

    /// <summary>
    /// Represents a Twist workspace
    /// </summary>
    public sealed class TwistWorkspace : TwistEntityBase
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("desc")]
        public string? Desc { get; set; }

        [JsonPropertyName("color")]
        public string? Color { get; set; }

        [JsonPropertyName("created_ts")]
        public DateTime? CreatedTs { get; set; }

        [JsonPropertyName("creator")]
        public TwistUser? Creator { get; set; }

        [JsonPropertyName("users")]
        public List<TwistUser>? Users { get; set; } = new();

        [JsonPropertyName("channels")]
        public List<TwistChannel>? Channels { get; set; } = new();

        [JsonPropertyName("groups")]
        public List<TwistGroup>? Groups { get; set; } = new();
    }

    /// <summary>
    /// Represents a Twist channel
    /// </summary>
    public sealed class TwistChannel : TwistEntityBase
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("desc")]
        public string? Desc { get; set; }

        [JsonPropertyName("creator")]
        public TwistUser? Creator { get; set; }

        [JsonPropertyName("created_ts")]
        public DateTime? CreatedTs { get; set; }

        [JsonPropertyName("users")]
        public List<TwistUser>? Users { get; set; } = new();
    }

    /// <summary>
    /// Represents a Twist thread
    /// </summary>
    public sealed class TwistThread : TwistEntityBase
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("creator")]
        public TwistUser? Creator { get; set; }

        [JsonPropertyName("created_ts")]
        public DateTime? CreatedTs { get; set; }

        [JsonPropertyName("last_ts")]
        public DateTime? LastTs { get; set; }

        [JsonPropertyName("posts_count")]
        public int? PostsCount { get; set; }

        [JsonPropertyName("participants_count")]
        public int? ParticipantsCount { get; set; }

        [JsonPropertyName("is_private")]
        public bool? IsPrivate { get; set; }

        [JsonPropertyName("is_archived")]
        public bool? IsArchived { get; set; }
    }

    /// <summary>
    /// Represents a Twist message
    /// </summary>
    public sealed class TwistMessage : TwistEntityBase
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("sender")]
        public TwistUser? Sender { get; set; }

        [JsonPropertyName("sent_ts")]
        public DateTime? SentTs { get; set; }

        [JsonPropertyName("attachments")]
        public List<TwistAttachment>? Attachments { get; set; } = new();

        [JsonPropertyName("reactions")]
        public List<TwistReaction>? Reactions { get; set; } = new();

        [JsonPropertyName("is_edited")]
        public bool? IsEdited { get; set; }

        [JsonPropertyName("edited_ts")]
        public DateTime? EditedTs { get; set; }
    }

    /// <summary>
    /// Represents a Twist comment
    /// </summary>
    public sealed class TwistComment : TwistEntityBase
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("author")]
        public TwistUser? Author { get; set; }

        [JsonPropertyName("created_ts")]
        public DateTime? CreatedTs { get; set; }

        [JsonPropertyName("thread_id")]
        public int? ThreadId { get; set; }

        [JsonPropertyName("message_id")]
        public int? MessageId { get; set; }

        [JsonPropertyName("is_edited")]
        public bool? IsEdited { get; set; }

        [JsonPropertyName("edited_ts")]
        public DateTime? EditedTs { get; set; }
    }

    /// <summary>
    /// Represents a Twist user
    /// </summary>
    public sealed class TwistUser : TwistEntityBase
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("timezone")]
        public string? Timezone { get; set; }

        [JsonPropertyName("is_bot")]
        public bool? IsBot { get; set; }

        [JsonPropertyName("profile")]
        public TwistUserProfile? Profile { get; set; }
    }

    /// <summary>
    /// Represents a Twist group
    /// </summary>
    public sealed class TwistGroup : TwistEntityBase
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("desc")]
        public string? Desc { get; set; }

        [JsonPropertyName("creator")]
        public TwistUser? Creator { get; set; }

        [JsonPropertyName("created_ts")]
        public DateTime? CreatedTs { get; set; }

        [JsonPropertyName("users")]
        public List<TwistUser>? Users { get; set; } = new();
    }

    /// <summary>
    /// Represents a Twist integration
    /// </summary>
    public sealed class TwistIntegration : TwistEntityBase
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("creator")]
        public TwistUser? Creator { get; set; }

        [JsonPropertyName("created_ts")]
        public DateTime? CreatedTs { get; set; }

        [JsonPropertyName("is_active")]
        public bool? IsActive { get; set; }

        [JsonPropertyName("config")]
        public string? Config { get; set; }
    }

    /// <summary>
    /// Represents a Twist attachment
    /// </summary>
    public sealed class TwistAttachment : TwistEntityBase
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("size")]
        public long? Size { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("uploader")]
        public TwistUser? Uploader { get; set; }

        [JsonPropertyName("uploaded_ts")]
        public DateTime? UploadedTs { get; set; }

        [JsonPropertyName("message_id")]
        public int? MessageId { get; set; }
    }

    /// <summary>
    /// Represents a Twist notification
    /// </summary>
    public sealed class TwistNotification : TwistEntityBase
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("recipient")]
        public TwistUser? Recipient { get; set; }

        [JsonPropertyName("created_ts")]
        public DateTime? CreatedTs { get; set; }

        [JsonPropertyName("is_read")]
        public bool? IsRead { get; set; }

        [JsonPropertyName("action_url")]
        public string? ActionUrl { get; set; }
    }

    // Supporting classes for Twist API responses

    /// <summary>
    /// Represents a Twist user profile
    /// </summary>
    public sealed class TwistUserProfile : TwistEntityBase
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("bio")]
        public string? Bio { get; set; }

        [JsonPropertyName("image")]
        public string? Image { get; set; }
    }

    /// <summary>
    /// Represents a Twist message reaction
    /// </summary>
    public sealed class TwistReaction : TwistEntityBase
    {
        [JsonPropertyName("emoji")]
        public string? Emoji { get; set; }

        [JsonPropertyName("user")]
        public TwistUser? User { get; set; }

        [JsonPropertyName("reacted_ts")]
        public DateTime? ReactedTs { get; set; }
    }
}