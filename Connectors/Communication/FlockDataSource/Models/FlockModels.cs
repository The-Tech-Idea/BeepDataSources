using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Connectors.Communication.Flock.Models
{
    public class FlockUser
    {
        public string? Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Uid { get; set; }
        public string? Role { get; set; }
        public string? Status { get; set; }
        public string? Timezone { get; set; }
        public FlockUserProfile? Profile { get; set; }
    }

    public class FlockUserPresence
    {
        public string? UserId { get; set; }
        public string? Status { get; set; }
        public DateTime? LastSeen { get; set; }
    }

    public class FlockGroup
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Type { get; set; }
        public List<FlockUser>? Members { get; set; } = new();
        public DateTime? Created { get; set; }
    }

    public class FlockGroupMember
    {
        public string? GroupId { get; set; }
        public FlockUser? User { get; set; }
        public string? Role { get; set; }
        public DateTime? JoinedAt { get; set; }
    }

    public class FlockChannel
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool? IsPublic { get; set; }
        public FlockUser? Creator { get; set; }
        public List<FlockUser>? Members { get; set; } = new();
    }

    public class FlockChannelMember
    {
        public string? ChannelId { get; set; }
        public FlockUser? User { get; set; }
        public string? Role { get; set; }
        public DateTime? JoinedAt { get; set; }
    }

    public class FlockMessage
    {
        public string? Id { get; set; }
        public string? Uid { get; set; }
        public string? Text { get; set; }
        public string? To { get; set; }
        public DateTime? Timestamp { get; set; }
        public List<FlockAttachment>? Attachments { get; set; } = new();
        public List<FlockMention>? Mentions { get; set; } = new();
        public bool? IsEdited { get; set; }
        public FlockMessage? From { get; set; }
    }

    public class FlockMessageReaction
    {
        public string? MessageId { get; set; }
        public string? Emoji { get; set; }
        public FlockUser? User { get; set; }
        public DateTime? Timestamp { get; set; }
    }

    public class FlockMessageReply
    {
        public string? ParentMessageId { get; set; }
        public FlockMessage? Message { get; set; }
        public int? ThreadLevel { get; set; }
    }

    public class FlockFile
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public long? Size { get; set; }
        public string? DownloadUrl { get; set; }
        public FlockUser? UploadedBy { get; set; }
        public DateTime? UploadedAt { get; set; }
        public string? ChannelId { get; set; }
    }

    public class FlockContact
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Status { get; set; }
    }

    public class FlockApp
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? IconUrl { get; set; }
        public FlockUser? Creator { get; set; }
        public bool? IsPublic { get; set; }
    }

    public class FlockWebhook
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Url { get; set; }
        public string? Events { get; set; }
        public FlockUser? Creator { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool? IsActive { get; set; }
    }

    public class FlockToken
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public FlockUser? CreatedBy { get; set; }
        public bool? IsActive { get; set; }
    }

    // Supporting classes for Flock
    public class FlockUserProfile
    {
        public string? Title { get; set; }
        public string? Department { get; set; }
        public string? Image { get; set; }
    }

    public class FlockAttachment
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public long? Size { get; set; }
        public string? DownloadUrl { get; set; }
    }

    public class FlockMention
    {
        public FlockUser? User { get; set; }
        public int? Start { get; set; }
        public int? End { get; set; }
    }
}