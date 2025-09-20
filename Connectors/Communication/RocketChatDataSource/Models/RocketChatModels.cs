using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Connectors.Communication.RocketChat.Models
{
    public class RocketChatUser
    {
        public string? Id { get; set; }
        public string? Username { get; set; }
        public string? Name { get; set; }
        public string? Status { get; set; }
        public string? UtcOffset { get; set; }
        public bool Active { get; set; }
        public List<string>? Roles { get; set; }
        public RocketChatUserSettings? Settings { get; set; }
        public RocketChatUserEmails? Emails { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastLogin { get; set; }
    }

    public class RocketChatChannel
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Fname { get; set; }
        public string? T { get; set; }
        public int Msgs { get; set; }
        public int UsersCount { get; set; }
        public string? U { get; set; }
        public bool Ts { get; set; }
        public bool Ro { get; set; }
        public bool SysMes { get; set; }
        public bool Default { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? Lm { get; set; }
    }

    public class RocketChatMessage
    {
        public string? Id { get; set; }
        public string? Rid { get; set; }
        public string? Msg { get; set; }
        public string? Ts { get; set; }
        public string? U { get; set; }
        public string? Alias { get; set; }
        public List<RocketChatAttachment>? Attachments { get; set; }
        public bool ParseUrls { get; set; }
        public bool Groupable { get; set; }
        public bool Tshow { get; set; }
        public string? Tmid { get; set; }
        public string? Tmsg { get; set; }
        public bool Tcount { get; set; }
        public string? Replies { get; set; }
        public bool EditedBy { get; set; }
        public string? EditedAt { get; set; }
        public bool Pinned { get; set; }
        public string? PinnedBy { get; set; }
        public string? PinnedAt { get; set; }
        public bool Starred { get; set; }
        public string? StarredBy { get; set; }
        public string? StarredAt { get; set; }
        public bool Mentions { get; set; }
        public bool Channels { get; set; }
    }

    // Supporting classes for Rocket.Chat
    public class RocketChatUserSettings { public RocketChatUserSettingsPreferences? Preferences { get; set; } }
    public class RocketChatUserSettingsPreferences { public bool DesktopNotifications { get; set; } public bool PushNotifications { get; set; } public bool EmailNotifications { get; set; } public string? Language { get; set; } }
    public class RocketChatUserEmails { public string? Address { get; set; } public bool Verified { get; set; } }
    public class RocketChatAttachment { public string? Title { get; set; } public string? Type { get; set; } public string? Description { get; set; } public string? TitleLink { get; set; } public string? TitleLinkDownload { get; set; } public string? ImageUrl { get; set; } public string? ImageType { get; set; } public int ImageSize { get; set; } public string? VideoUrl { get; set; } public string? VideoType { get; set; } public int VideoSize { get; set; } public string? AudioUrl { get; set; } public string? AudioType { get; set; } public int AudioSize { get; set; } }

    // Missing model classes for Rocket.Chat Map entities
    public class RocketChatChannelMember
    {
        public string? Id { get; set; }
        public string? Username { get; set; }
        public string? Name { get; set; }
        public string? Status { get; set; }
        public List<string>? Roles { get; set; }
    }

    public class RocketChatGroup
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Fname { get; set; }
        public string? T { get; set; }
        public int Msgs { get; set; }
        public int UsersCount { get; set; }
        public string? U { get; set; }
        public bool Ts { get; set; }
        public bool Ro { get; set; }
        public bool SysMes { get; set; }
        public bool Default { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? Lm { get; set; }
    }

    public class RocketChatGroupMember
    {
        public string? Id { get; set; }
        public string? Username { get; set; }
        public string? Name { get; set; }
        public string? Status { get; set; }
        public List<string>? Roles { get; set; }
    }

    public class RocketChatImList
    {
        public string? Id { get; set; }
        public string? T { get; set; }
        public int Msgs { get; set; }
        public int UsersCount { get; set; }
        public string? U { get; set; }
        public DateTime Ts { get; set; }
        public bool Ro { get; set; }
        public bool SysMes { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? Lm { get; set; }
    }

    public class RocketChatRoom
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Fname { get; set; }
        public string? T { get; set; }
        public int Msgs { get; set; }
        public int UsersCount { get; set; }
        public string? U { get; set; }
        public DateTime Ts { get; set; }
        public bool Ro { get; set; }
        public bool SysMes { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? Lm { get; set; }
    }

    public class RocketChatSubscription
    {
        public string? Id { get; set; }
        public string? Rid { get; set; }
        public string? Name { get; set; }
        public string? Fname { get; set; }
        public string? T { get; set; }
        public int Msgs { get; set; }
        public int UsersCount { get; set; }
        public string? U { get; set; }
        public DateTime Ts { get; set; }
        public bool Ro { get; set; }
        public bool Alert { get; set; }
        public bool Open { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? Lm { get; set; }
    }

    public class RocketChatRole
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool Protected { get; set; }
        public bool Mandatory2fa { get; set; }
        public List<string>? Scope { get; set; }
    }

    public class RocketChatPermission
    {
        public string? Id { get; set; }
        public string? Level { get; set; }
        public List<string>? Roles { get; set; }
        public List<string>? Group { get; set; }
    }

    public class RocketChatSetting
    {
        public string? Id { get; set; }
        public string? Type { get; set; }
        public object? Value { get; set; }
        public string? Group { get; set; }
        public bool Public { get; set; }
        public bool Autoupdate { get; set; }
        public bool Hidden { get; set; }
        public bool Blocked { get; set; }
        public bool RequiredOnWizard { get; set; }
        public string? I18nLabel { get; set; }
        public string? I18nDescription { get; set; }
    }

    public class RocketChatStatistics
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int NonActiveUsers { get; set; }
        public int OnlineUsers { get; set; }
        public int AwayUsers { get; set; }
        public int OfflineUsers { get; set; }
        public int TotalRooms { get; set; }
        public int TotalChannels { get; set; }
        public int TotalPrivateGroups { get; set; }
        public int TotalDirect { get; set; }
        public int TotalLivechat { get; set; }
        public int TotalMessages { get; set; }
        public int TotalLivechatMessages { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class RocketChatIntegration
    {
        public string? Id { get; set; }
        public string? Type { get; set; }
        public string? Name { get; set; }
        public bool Enabled { get; set; }
        public string? Username { get; set; }
        public string? Channel { get; set; }
        public string? Script { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class RocketChatWebhook
    {
        public string? Id { get; set; }
        public string? Type { get; set; }
        public string? Name { get; set; }
        public bool Enabled { get; set; }
        public string? Username { get; set; }
        public string? Channel { get; set; }
        public string? Script { get; set; }
        public string? Url { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}