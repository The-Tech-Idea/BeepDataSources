using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Connectors.Communication.Chanty.Models
{
    public class ChantyTeam
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Avatar { get; set; }
        public DateTime CreatedAt { get; set; }
        public ChantyUser? Owner { get; set; }
    }

    public class ChantyChannel
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Type { get; set; }
        public bool IsPrivate { get; set; }
        public ChantyUser? Creator { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ChantyUser>? Members { get; set; }
    }

    public class ChantyMessage
    {
        public string? Id { get; set; }
        public string? Text { get; set; }
        public ChantyUser? Sender { get; set; }
        public string? ChannelId { get; set; }
        public DateTime SentAt { get; set; }
        public List<ChantyAttachment>? Attachments { get; set; }
        public List<ChantyReaction>? Reactions { get; set; }
        public bool IsEdited { get; set; }
        public DateTime? EditedAt { get; set; }
    }

    public class ChantyUser
    {
        public string? Id { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Avatar { get; set; }
        public string? Status { get; set; }
        public DateTime LastSeen { get; set; }
    }

    // Supporting classes for Chanty
    public class ChantyAttachment { public string? Id { get; set; } public string? Type { get; set; } public string? Url { get; set; } public string? Name { get; set; } public long Size { get; set; } }
    public class ChantyReaction { public string? Emoji { get; set; } public ChantyUser? User { get; set; } public DateTime ReactedAt { get; set; } }

    // Missing model classes for Chanty Map entities
    public class ChantyTeamMember
    {
        public string? Id { get; set; }
        public string? UserId { get; set; }
        public string? TeamId { get; set; }
        public string? Role { get; set; }
        public DateTime JoinedAt { get; set; }
        public bool IsOwner { get; set; }
        public ChantyUser? User { get; set; }
    }

    public class ChantyChannelMember
    {
        public string? Id { get; set; }
        public string? UserId { get; set; }
        public string? ChannelId { get; set; }
        public string? Role { get; set; }
        public DateTime JoinedAt { get; set; }
        public bool IsAdmin { get; set; }
        public ChantyUser? User { get; set; }
    }

    public class ChantyMessageReply
    {
        public string? Id { get; set; }
        public string? Text { get; set; }
        public string? ParentMessageId { get; set; }
        public ChantyUser? Sender { get; set; }
        public DateTime SentAt { get; set; }
        public List<ChantyAttachment>? Attachments { get; set; }
        public List<ChantyReaction>? Reactions { get; set; }
        public bool IsEdited { get; set; }
        public DateTime? EditedAt { get; set; }
    }

    public class ChantyFile
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public long Size { get; set; }
        public string? Url { get; set; }
        public string? ChannelId { get; set; }
        public ChantyUser? UploadedBy { get; set; }
        public DateTime UploadedAt { get; set; }
        public string? ThumbnailUrl { get; set; }
    }

    public class ChantyNotification
    {
        public string? Id { get; set; }
        public string? Type { get; set; }
        public string? Title { get; set; }
        public string? Message { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? RelatedEntityId { get; set; }
        public string? RelatedEntityType { get; set; }
        public ChantyUser? Sender { get; set; }
    }

    public class ChantyUserSettings
    {
        public string? UserId { get; set; }
        public bool EmailNotifications { get; set; }
        public bool PushNotifications { get; set; }
        public bool DesktopNotifications { get; set; }
        public string? Theme { get; set; }
        public string? Language { get; set; }
        public bool ShowOnlineStatus { get; set; }
        public bool AllowDirectMessages { get; set; }
        public List<string>? MutedChannels { get; set; }
    }

    public class ChantyTeamSettings
    {
        public string? TeamId { get; set; }
        public bool AllowPublicChannels { get; set; }
        public bool RequireEmailVerification { get; set; }
        public bool AllowGuestUsers { get; set; }
        public string? DefaultChannelName { get; set; }
        public int MaxFileSize { get; set; }
        public List<string>? AllowedDomains { get; set; }
        public bool TwoFactorRequired { get; set; }
    }

    public class ChantyIntegration
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Provider { get; set; }
        public bool IsEnabled { get; set; }
        public string? Config { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public ChantyUser? CreatedBy { get; set; }
    }

    public class ChantyWebhook
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Url { get; set; }
        public string? Secret { get; set; }
        public List<string>? Events { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public ChantyUser? CreatedBy { get; set; }
    }

    public class ChantyAuditLog
    {
        public string? Id { get; set; }
        public string? Action { get; set; }
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
        public string? Description { get; set; }
        public object? OldValue { get; set; }
        public object? NewValue { get; set; }
        public DateTime Timestamp { get; set; }
        public ChantyUser? User { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}