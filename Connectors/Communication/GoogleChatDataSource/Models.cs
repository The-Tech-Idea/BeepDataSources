using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Communication.GoogleChat.Models
{
    /// <summary>
    /// Base class for all Google Chat entities with Attach method for Beep framework integration
    /// </summary>
    public abstract class GoogleChatEntityBase
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
        public T Attach<T>(IDataSource dataSource) where T : GoogleChatEntityBase
        {
            DataSource = dataSource;
            return (T)this;
        }
    }

    /// <summary>
    /// Represents a Google Chat space
    /// </summary>
    public sealed class GoogleChatSpace : GoogleChatEntityBase
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("Caption")]
        public string? Caption { get; set; }

        [JsonPropertyName("spaceDetails")]
        public GoogleChatSpaceDetails? SpaceDetails { get; set; }

        [JsonPropertyName("threaded")]
        public string? Threaded { get; set; }
    }

    /// <summary>
    /// Represents a Google Chat message
    /// </summary>
    public sealed class GoogleChatMessage : GoogleChatEntityBase
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("sender")]
        public string? Sender { get; set; }

        [JsonPropertyName("createTime")]
        public string? CreateTime { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("threadReplies")]
        public List<GoogleChatMessage>? ThreadReplies { get; set; }

        [JsonPropertyName("cardsV2")]
        public GoogleChatCardWithId? CardsV2 { get; set; }

        [JsonPropertyName("previewText")]
        public string? PreviewText { get; set; }

        [JsonPropertyName("annotations")]
        public List<string>? Annotations { get; set; }

        [JsonPropertyName("thread")]
        public string? Thread { get; set; }

        [JsonPropertyName("space")]
        public string? Space { get; set; }

        [JsonPropertyName("fallbackText")]
        public string? FallbackText { get; set; }

        [JsonPropertyName("actionResponse")]
        public string? ActionResponse { get; set; }

        [JsonPropertyName("argumentText")]
        public string? ArgumentText { get; set; }

        [JsonPropertyName("slashCommand")]
        public string? SlashCommand { get; set; }

        [JsonPropertyName("attachment")]
        public string? Attachment { get; set; }

        [JsonPropertyName("reactions")]
        public List<GoogleChatReaction>? Reactions { get; set; }
    }

    /// <summary>
    /// Represents a Google Chat member
    /// </summary>
    public sealed class GoogleChatMember : GoogleChatEntityBase
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("user")]
        public GoogleChatUser? User { get; set; }

        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }
    }

    /// <summary>
    /// Represents a Google Chat user
    /// </summary>
    public sealed class GoogleChatUser : GoogleChatEntityBase
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("Caption")]
        public string? Caption { get; set; }

        [JsonPropertyName("domainId")]
        public string? DomainId { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("isAnonymous")]
        public bool IsAnonymous { get; set; }
    }

    /// <summary>
    /// Represents a Google Chat message attachment
    /// </summary>
    public sealed class GoogleChatMessageAttachment : GoogleChatEntityBase
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("contentName")]
        public string? ContentName { get; set; }

        [JsonPropertyName("contentType")]
        public string? ContentType { get; set; }

        [JsonPropertyName("attachmentDataRef")]
        public string? AttachmentDataRef { get; set; }

        [JsonPropertyName("sizeBytes")]
        public long? SizeBytes { get; set; }

        [JsonPropertyName("downloadUri")]
        public string? DownloadUri { get; set; }

        [JsonPropertyName("thumbnailUri")]
        public string? ThumbnailUri { get; set; }

        [JsonPropertyName("createTime")]
        public string? CreateTime { get; set; }
    }

    /// <summary>
    /// Represents a Google Chat message thread
    /// </summary>
    public sealed class GoogleChatMessageThread : GoogleChatEntityBase
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("threadKey")]
        public string? ThreadKey { get; set; }

        [JsonPropertyName("messages")]
        public List<GoogleChatMessage>? Messages { get; set; }
    }

    /// <summary>
    /// Represents a Google Chat user space
    /// </summary>
    public sealed class GoogleChatUserSpace : GoogleChatEntityBase
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("Caption")]
        public string? Caption { get; set; }

        [JsonPropertyName("spaceDetails")]
        public GoogleChatSpaceDetails? SpaceDetails { get; set; }

        [JsonPropertyName("threaded")]
        public string? Threaded { get; set; }
    }

    /// <summary>
    /// Represents a Google Chat user membership
    /// </summary>
    public sealed class GoogleChatUserMembership : GoogleChatEntityBase
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("createTime")]
        public string? CreateTime { get; set; }

        [JsonPropertyName("member")]
        public GoogleChatUser? Member { get; set; }
    }

    /// <summary>
    /// Represents a Google Chat media link
    /// </summary>
    public sealed class GoogleChatMediaLink : GoogleChatEntityBase
    {
        [JsonPropertyName("resourceName")]
        public string? ResourceName { get; set; }

        [JsonPropertyName("downloadUri")]
        public string? DownloadUri { get; set; }

        [JsonPropertyName("contentType")]
        public string? ContentType { get; set; }

        [JsonPropertyName("sizeBytes")]
        public long? SizeBytes { get; set; }

        [JsonPropertyName("createTime")]
        public string? CreateTime { get; set; }
    }

    // Supporting classes for Google Chat API responses

    /// <summary>
    /// Represents Google Chat space details
    /// </summary>
    public sealed class GoogleChatSpaceDetails : GoogleChatEntityBase
    {
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("guidelines")]
        public string? Guidelines { get; set; }
    }

    /// <summary>
    /// Represents a Google Chat card with ID
    /// </summary>
    public sealed class GoogleChatCardWithId : GoogleChatEntityBase
    {
        [JsonPropertyName("cardId")]
        public string? CardId { get; set; }

        [JsonPropertyName("card")]
        public GoogleChatCard? Card { get; set; }
    }

    /// <summary>
    /// Represents a Google Chat card
    /// </summary>
    public sealed class GoogleChatCard : GoogleChatEntityBase
    {
        [JsonPropertyName("header")]
        public string? Header { get; set; }

        [JsonPropertyName("sections")]
        public List<GoogleChatSection>? Sections { get; set; }

        [JsonPropertyName("cardActions")]
        public string? CardActions { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    /// <summary>
    /// Represents a Google Chat section
    /// </summary>
    public sealed class GoogleChatSection : GoogleChatEntityBase
    {
        [JsonPropertyName("header")]
        public string? Header { get; set; }

        [JsonPropertyName("widgets")]
        public List<GoogleChatWidget>? Widgets { get; set; }
    }

    /// <summary>
    /// Represents a Google Chat widget
    /// </summary>
    public sealed class GoogleChatWidget : GoogleChatEntityBase
    {
        [JsonPropertyName("textParagraph")]
        public string? TextParagraph { get; set; }

        [JsonPropertyName("buttonList")]
        public GoogleChatButtonList? ButtonList { get; set; }

        [JsonPropertyName("image")]
        public string? Image { get; set; }
    }

    /// <summary>
    /// Represents a Google Chat button list
    /// </summary>
    public sealed class GoogleChatButtonList : GoogleChatEntityBase
    {
        [JsonPropertyName("buttons")]
        public List<GoogleChatButton>? Buttons { get; set; }
    }

    /// <summary>
    /// Represents a Google Chat button
    /// </summary>
    public sealed class GoogleChatButton : GoogleChatEntityBase
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("onClick")]
        public string? OnClick { get; set; }
    }

    /// <summary>
    /// Represents a Google Chat reaction
    /// </summary>
    public sealed class GoogleChatReaction : GoogleChatEntityBase
    {
        [JsonPropertyName("emoji")]
        public string? Emoji { get; set; }

        [JsonPropertyName("users")]
        public List<GoogleChatUser>? Users { get; set; }
    }
}
