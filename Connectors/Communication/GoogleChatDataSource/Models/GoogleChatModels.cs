using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Connectors.Communication.GoogleChat.Models
{
    public class GoogleChatSpace
    {
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? DisplayName { get; set; }
        public GoogleChatSpaceDetails? SpaceDetails { get; set; }
        public string? Threaded { get; set; }
    }

    public class GoogleChatMessage
    {
        public string? Name { get; set; }
        public string? Sender { get; set; }
        public string? CreateTime { get; set; }
        public string? Text { get; set; }
        public List<GoogleChatMessage>? ThreadReplies { get; set; }
        public GoogleChatCardWithId? CardsV2 { get; set; }
        public string? PreviewText { get; set; }
        public List<string>? Annotations { get; set; }
        public string? Thread { get; set; }
        public string? Space { get; set; }
        public string? FallbackText { get; set; }
        public string? ActionResponse { get; set; }
        public string? ArgumentText { get; set; }
        public string? SlashCommand { get; set; }
        public string? Attachment { get; set; }
        public List<GoogleChatReaction>? Reactions { get; set; }
    }

    public class GoogleChatMember
    {
        public string? Name { get; set; }
        public GoogleChatUser? User { get; set; }
        public string? Role { get; set; }
        public string? State { get; set; }
    }

    public class GoogleChatUser
    {
        public string? Name { get; set; }
        public string? DisplayName { get; set; }
        public string? DomainId { get; set; }
        public string? Type { get; set; }
        public bool IsAnonymous { get; set; }
    }

    // Supporting classes for Google Chat
    public class GoogleChatSpaceDetails { public string? Description { get; set; } public string? Guidelines { get; set; } }
    public class GoogleChatCardWithId { public string? CardId { get; set; } public GoogleChatCard? Card { get; set; } }
    public class GoogleChatCard { public string? Header { get; set; } public List<GoogleChatSection>? Sections { get; set; } public string? CardActions { get; set; } public string? Name { get; set; } }
    public class GoogleChatSection { public string? Header { get; set; } public List<GoogleChatWidget>? Widgets { get; set; } }
    public class GoogleChatWidget { public string? TextParagraph { get; set; } public GoogleChatButtonList? ButtonList { get; set; } public string? Image { get; set; } }
    public class GoogleChatButtonList { public List<GoogleChatButton>? Buttons { get; set; } }
    public class GoogleChatButton { public string? Text { get; set; } public string? OnClick { get; set; } }
    public class GoogleChatReaction { public string? Emoji { get; set; } public List<GoogleChatUser>? Users { get; set; } }

    // Missing model classes for Google Chat Map entities
    public class GoogleChatMessageAttachment
    {
        public string? Name { get; set; }
        public string? ContentName { get; set; }
        public string? ContentType { get; set; }
        public string? AttachmentDataRef { get; set; }
        public long? SizeBytes { get; set; }
        public string? DownloadUri { get; set; }
        public string? ThumbnailUri { get; set; }
        public string? CreateTime { get; set; }
    }

    public class GoogleChatMessageThread
    {
        public string? Name { get; set; }
        public string? ThreadKey { get; set; }
        public List<GoogleChatMessage>? Messages { get; set; }
    }

    public class GoogleChatUserSpace
    {
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? DisplayName { get; set; }
        public GoogleChatSpaceDetails? SpaceDetails { get; set; }
        public string? Threaded { get; set; }
    }

    public class GoogleChatUserMembership
    {
        public string? Name { get; set; }
        public string? State { get; set; }
        public string? Role { get; set; }
        public string? CreateTime { get; set; }
        public GoogleChatUser? Member { get; set; }
    }

    public class GoogleChatMediaLink
    {
        public string? ResourceName { get; set; }
        public string? DownloadUri { get; set; }
        public string? ContentType { get; set; }
        public long? SizeBytes { get; set; }
        public string? CreateTime { get; set; }
    }
}