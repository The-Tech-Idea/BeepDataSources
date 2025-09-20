using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Connectors.Communication.Discord.Models
{
    // Basic supporting classes first
    public class DiscordPermissionOverwrite { public string Id { get; set; } = string.Empty; public string Type { get; set; } = string.Empty; public string Allow { get; set; } = string.Empty; public string Deny { get; set; } = string.Empty; }
    public class DiscordThreadMetadata { public bool Archived { get; set; } public int AutoArchiveDuration { get; set; } public DateTime ArchiveTimestamp { get; set; } public bool Locked { get; set; } public bool Invitable { get; set; } public DateTime? CreateTimestamp { get; set; } }
    public class DiscordThreadMember { public string? Id { get; set; } public string? UserId { get; set; } public DateTime JoinTimestamp { get; set; } public int Flags { get; set; } }
    public class DiscordWelcomeScreen { public string? Description { get; set; } public List<DiscordWelcomeScreenChannel> WelcomeChannels { get; set; } = new(); }
    public class DiscordWelcomeScreenChannel { public string? ChannelId { get; set; } public string? Description { get; set; } public string? EmojiId { get; set; } public string? EmojiName { get; set; } }
    public class DiscordMessageActivity { public int Type { get; set; } public string? PartyId { get; set; } }
    public class DiscordMessageApplication { public string Id { get; set; } = string.Empty; public string? CoverImage { get; set; } public string? Description { get; set; } public string? Icon { get; set; } public string Name { get; set; } = string.Empty; }
    public class DiscordMessageReference { public string? MessageId { get; set; } public string? ChannelId { get; set; } public string? GuildId { get; set; } public bool? FailIfNotExists { get; set; } }
    public class DiscordComponent { public int Type { get; set; } public int? Style { get; set; } public string? Label { get; set; } public string? Emoji { get; set; } public string? CustomId { get; set; } public string? Url { get; set; } public bool? Disabled { get; set; } public List<DiscordComponent> Components { get; set; } = new(); }
    public class DiscordStickerItem { public string Id { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string FormatType { get; set; } = string.Empty; }
    public class DiscordRoleTags { public string? BotId { get; set; } public string? IntegrationId { get; set; } public bool? PremiumSubscriber { get; set; } public string? SubscriptionListingId { get; set; } public bool? AvailableForPurchase { get; set; } public bool? GuildConnections { get; set; } }
    public class DiscordEmbedFooter { public string Text { get; set; } = string.Empty; public string? IconUrl { get; set; } public string? ProxyIconUrl { get; set; } }
    public class DiscordEmbedImage { public string? Url { get; set; } public string? ProxyUrl { get; set; } public int? Height { get; set; } public int? Width { get; set; } }
    public class DiscordEmbedThumbnail { public string? Url { get; set; } public string? ProxyUrl { get; set; } public int? Height { get; set; } public int? Width { get; set; } }
    public class DiscordEmbedVideo { public string? Url { get; set; } public string? ProxyUrl { get; set; } public int? Height { get; set; } public int? Width { get; set; } }
    public class DiscordEmbedProvider { public string? Name { get; set; } public string? Url { get; set; } }
    public class DiscordEmbedAuthor { public string? Name { get; set; } public string? Url { get; set; } public string? IconUrl { get; set; } public string? ProxyIconUrl { get; set; } }
    public class DiscordEmbedField { public string Name { get; set; } = string.Empty; public string Value { get; set; } = string.Empty; public bool? Inline { get; set; } }
    public class DiscordApplicationInstallParams { public List<string> Scopes { get; set; } = new(); public string? Permissions { get; set; } }
    public class DiscordAuditLogChange { public string? NewValue { get; set; } public string? OldValue { get; set; } public string Key { get; set; } = string.Empty; }
    public class DiscordAuditLogEntryOptions { public string? DeleteMemberDays { get; set; } public string? MembersRemoved { get; set; } public string? ChannelId { get; set; } public string? Count { get; set; } public string? Id { get; set; } public string? Type { get; set; } public string? RoleName { get; set; } }
    public class DiscordIntegrationAccount { public string Id { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; }
    public class DiscordGuildScheduledEventEntityMetadata { public string? Location { get; set; } }
    public class DiscordInviteStageInstance { public List<string> Members { get; set; } = new(); public int ParticipantCount { get; set; } public int SpeakerCount { get; set; } public string? Topic { get; set; } }
    public class DiscordAutoModerationTriggerMetadata { public List<string> KeywordFilter { get; set; } = new(); public List<string> RegexPatterns { get; set; } = new(); public List<int> Presets { get; set; } = new(); public List<string> AllowList { get; set; } = new(); public int? MentionTotalLimit { get; set; } public bool? MentionRaidProtectionEnabled { get; set; } }
    public class DiscordAutoModerationActionMetadata { public int? ChannelId { get; set; } public int? DurationSeconds { get; set; } public string? CustomMessage { get; set; } }

    // Core entity classes
    public class DiscordUser
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? Discriminator { get; set; }
        public string? Avatar { get; set; }
        public bool? Bot { get; set; }
        public bool? System { get; set; }
        public bool? MfaEnabled { get; set; }
        public string? Banner { get; set; }
        public int? AccentColor { get; set; }
        public string? Locale { get; set; }
        public bool? Verified { get; set; }
        public string? Email { get; set; }
        public int Flags { get; set; }
        public int PremiumType { get; set; }
        public int PublicFlags { get; set; }
    }

    public class DiscordEmoji
    {
        public string? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public DiscordUser? User { get; set; }
        public bool? RequireColons { get; set; }
        public bool? Managed { get; set; }
        public bool? Animated { get; set; }
        public bool? Available { get; set; }
    }

    public class DiscordRole
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Color { get; set; }
        public bool Hoist { get; set; }
        public string? Icon { get; set; }
        public string? UnicodeEmoji { get; set; }
        public int Position { get; set; }
        public string Permissions { get; set; } = string.Empty;
        public bool Managed { get; set; }
        public bool Mentionable { get; set; }
        public DiscordRoleTags? Tags { get; set; }
    }

    public class DiscordSticker
    {
        public string Id { get; set; } = string.Empty;
        public string? PackId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Tags { get; set; }
        public string Type { get; set; } = string.Empty;
        public string FormatType { get; set; } = string.Empty;
        public bool? Available { get; set; }
        public string? GuildId { get; set; }
        public DiscordUser? User { get; set; }
        public int? SortValue { get; set; }
    }

    public class DiscordEmbed
    {
        public string? Title { get; set; }
        public string? Type { get; set; }
        public string? Description { get; set; }
        public string? Url { get; set; }
        public DateTime? Timestamp { get; set; }
        public int? Color { get; set; }
        public DiscordEmbedFooter? Footer { get; set; }
        public DiscordEmbedImage? Image { get; set; }
        public DiscordEmbedThumbnail? Thumbnail { get; set; }
        public DiscordEmbedVideo? Video { get; set; }
        public DiscordEmbedProvider? Provider { get; set; }
        public DiscordEmbedAuthor? Author { get; set; }
        public List<DiscordEmbedField> Fields { get; set; } = new();
    }

    public class DiscordAttachment
    {
        public string Id { get; set; } = string.Empty;
        public string Filename { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ContentType { get; set; }
        public int Size { get; set; }
        public string Url { get; set; } = string.Empty;
        public string ProxyUrl { get; set; } = string.Empty;
        public int? Height { get; set; }
        public int? Width { get; set; }
        public bool? Ephemeral { get; set; }
    }

    public class DiscordReaction
    {
        public int Count { get; set; }
        public bool Me { get; set; }
        public DiscordEmoji? Emoji { get; set; }
    }

    public class DiscordTeam
    {
        public string? Icon { get; set; }
        public string Id { get; set; } = string.Empty;
        public List<DiscordTeamMember> Members { get; set; } = new();
        public string Name { get; set; } = string.Empty;
        public string? OwnerUserId { get; set; }
    }

    public class DiscordTeamMember
    {
        public int MembershipState { get; set; }
        public List<string> Permissions { get; set; } = new();
        public string? TeamId { get; set; }
        public DiscordUser? User { get; set; }
    }

    public class DiscordApplication
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string? Description { get; set; }
        public string? RpcOrigins { get; set; }
        public bool BotPublic { get; set; }
        public bool BotRequireCodeGrant { get; set; }
        public string? Bot { get; set; }
        public string? TermsOfServiceUrl { get; set; }
        public string? PrivacyPolicyUrl { get; set; }
        public DiscordUser? Owner { get; set; }
        public string? VerifyKey { get; set; }
        public DiscordTeam? Team { get; set; }
        public string? GuildId { get; set; }
        public string? PrimarySkuId { get; set; }
        public string? Slug { get; set; }
        public string? CoverImage { get; set; }
        public int Flags { get; set; }
        public string? Tags { get; set; }
        public DiscordApplicationInstallParams? InstallParams { get; set; }
        public string? CustomInstallUrl { get; set; }
        public string? RoleConnectionsVerificationUrl { get; set; }
    }

    public class DiscordIntegrationApplication
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string? Description { get; set; }
        public string? Summary { get; set; }
        public DiscordUser? Bot { get; set; }
    }

    public class DiscordAuditLogEntry
    {
        public string? TargetId { get; set; }
        public List<DiscordAuditLogChange> Changes { get; set; } = new();
        public DiscordUser? UserId { get; set; }
        public string Id { get; set; } = string.Empty;
        public int ActionType { get; set; }
        public DiscordAuditLogEntryOptions? Options { get; set; }
        public string? Reason { get; set; }
    }

    public class DiscordAutoModerationAction
    {
        public int Type { get; set; }
        public DiscordAutoModerationActionMetadata? Metadata { get; set; }
    }

    // Main entity classes that depend on the above
    public class DiscordGuildMember
    {
        public DiscordUser? User { get; set; }
        public string? Nick { get; set; }
        public string? Avatar { get; set; }
        public List<string> Roles { get; set; } = new();
        public DateTime JoinedAt { get; set; }
        public DateTime? PremiumSince { get; set; }
        public bool Deaf { get; set; }
        public bool Mute { get; set; }
        public int? Flags { get; set; }
        public bool? Pending { get; set; }
        public string? Permissions { get; set; }
        public DateTime? CommunicationDisabledUntil { get; set; }
    }

    public class DiscordChannel
    {
        public string Id { get; set; } = string.Empty;
        public int Type { get; set; }
        public string? GuildId { get; set; }
        public int? Position { get; set; }
        public List<DiscordPermissionOverwrite> PermissionOverwrites { get; set; } = new();
        public string? Name { get; set; }
        public string? Topic { get; set; }
        public bool? Nsfw { get; set; }
        public string? LastMessageId { get; set; }
        public int? Bitrate { get; set; }
        public int? UserLimit { get; set; }
        public int? RateLimitPerUser { get; set; }
        public List<DiscordUser> Recipients { get; set; } = new();
        public string? Icon { get; set; }
        public string? OwnerId { get; set; }
        public string? ApplicationId { get; set; }
        public string? ParentId { get; set; }
        public DateTime? LastPinTimestamp { get; set; }
        public string? RtcRegion { get; set; }
        public int? VideoQualityMode { get; set; }
        public int? MessageCount { get; set; }
        public int? MemberCount { get; set; }
        public DiscordThreadMetadata? ThreadMetadata { get; set; }
        public DiscordThreadMember? Member { get; set; }
        public int? DefaultAutoArchiveDuration { get; set; }
        public string? Permissions { get; set; }
        public int Flags { get; set; }
        public int? TotalMessageSent { get; set; }
        public List<DiscordForumTag> AvailableTags { get; set; } = new();
        public List<string> AppliedTags { get; set; } = new();
        public string? DefaultReactionEmoji { get; set; }
        public int? DefaultThreadRateLimitPerUser { get; set; }
        public int? DefaultSortOrder { get; set; }
        public int? DefaultForumLayout { get; set; }
    }

    public class DiscordForumTag { public string Id { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string? Moderated { get; set; } public DiscordEmoji? Emoji { get; set; } }

    public class DiscordMessageInteraction { public string Id { get; set; } = string.Empty; public int Type { get; set; } public string Name { get; set; } = string.Empty; public DiscordUser? User { get; set; } public DiscordUser? Member { get; set; } }

    public class DiscordMessage
    {
        public int MessageId { get; set; }
        public string ChannelId { get; set; } = string.Empty;
        public DiscordUser? Author { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool Edited { get; set; }
        public DateTime? EditedTimestamp { get; set; }
        public bool Tts { get; set; }
        public bool MentionEveryone { get; set; }
        public List<DiscordUser> Mentions { get; set; } = new();
        public List<string> MentionRoles { get; set; } = new();
        public List<DiscordChannel> MentionChannels { get; set; } = new();
        public List<DiscordAttachment> Attachments { get; set; } = new();
        public List<DiscordEmbed> Embeds { get; set; } = new();
        public List<DiscordReaction> Reactions { get; set; } = new();
        public string? Nonce { get; set; }
        public bool Pinned { get; set; }
        public string? WebhookId { get; set; }
        public int Type { get; set; }
        public DiscordMessageActivity? Activity { get; set; }
        public DiscordMessageApplication? Application { get; set; }
        public string? ApplicationId { get; set; }
        public DiscordMessageReference? MessageReference { get; set; }
        public int Flags { get; set; }
        public DiscordMessage? ReferencedMessage { get; set; }
        public DiscordMessageInteraction? Interaction { get; set; }
        public DiscordChannel? Thread { get; set; }
        public List<DiscordComponent> Components { get; set; } = new();
        public List<DiscordStickerItem> StickerItems { get; set; } = new();
        public int Position { get; set; }
        public DiscordRole? RoleSubscriptionData { get; set; }
    }

    public class DiscordGuild
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string? IconHash { get; set; }
        public string? Splash { get; set; }
        public string? DiscoverySplash { get; set; }
        public bool? Owner { get; set; }
        public string? OwnerId { get; set; }
        public string? Permissions { get; set; }
        public string? Region { get; set; }
        public string? AfkChannelId { get; set; }
        public int AfkTimeout { get; set; }
        public bool? WidgetEnabled { get; set; }
        public string? WidgetChannelId { get; set; }
        public int VerificationLevel { get; set; }
        public int DefaultMessageNotifications { get; set; }
        public int ExplicitContentFilter { get; set; }
        public List<DiscordRole> Roles { get; set; } = new();
        public List<DiscordEmoji> Emojis { get; set; } = new();
        public List<string> Features { get; set; } = new();
        public int MfaLevel { get; set; }
        public string? ApplicationId { get; set; }
        public string? SystemChannelId { get; set; }
        public int SystemChannelFlags { get; set; }
        public string? RulesChannelId { get; set; }
        public int? MaxPresences { get; set; }
        public int? MaxMembers { get; set; }
        public string? VanityUrlCode { get; set; }
        public string? Description { get; set; }
        public string? Banner { get; set; }
        public int PremiumTier { get; set; }
        public int? PremiumSubscriptionCount { get; set; }
        public string? PreferredLocale { get; set; }
        public string? PublicUpdatesChannelId { get; set; }
        public int? MaxVideoChannelUsers { get; set; }
        public int? ApproximateMemberCount { get; set; }
        public int? ApproximatePresenceCount { get; set; }
        public DiscordWelcomeScreen? WelcomeScreen { get; set; }
        public int NsfwLevel { get; set; }
        public List<DiscordSticker> Stickers { get; set; } = new();
        public bool PremiumProgressBarEnabled { get; set; }
        public string? SafetyAlertsChannelId { get; set; }
    }

    // Remaining classes that depend on the main entities
    public class DiscordInvite
    {
        public string Code { get; set; } = string.Empty;
        public DiscordGuild? Guild { get; set; }
        public DiscordChannel? Channel { get; set; }
        public DiscordUser? Inviter { get; set; }
        public int TargetType { get; set; }
        public DiscordUser? TargetUser { get; set; }
        public DiscordApplication? TargetApplication { get; set; }
        public int? ApproximatePresenceCount { get; set; }
        public int? ApproximateMemberCount { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DiscordInviteStageInstance? StageInstance { get; set; }
        public DiscordGuildScheduledEvent? GuildScheduledEvent { get; set; }
    }

    public class DiscordVoiceState
    {
        public string? GuildId { get; set; }
        public string? ChannelId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DiscordGuildMember? Member { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public bool Deaf { get; set; }
        public bool Mute { get; set; }
        public bool SelfDeaf { get; set; }
        public bool SelfMute { get; set; }
        public bool? SelfStream { get; set; }
        public bool SelfVideo { get; set; }
        public bool Suppress { get; set; }
        public DateTime? RequestToSpeakTimestamp { get; set; }
    }

    public class DiscordWebhook
    {
        public string Id { get; set; } = string.Empty;
        public int Type { get; set; }
        public string? GuildId { get; set; }
        public string? ChannelId { get; set; }
        public DiscordUser? User { get; set; }
        public string? Name { get; set; }
        public string? Avatar { get; set; }
        public string? Token { get; set; }
        public string? ApplicationId { get; set; }
        public string? SourceGuild { get; set; }
        public string? SourceChannel { get; set; }
        public string? Url { get; set; }
    }

    public class DiscordAuditLog
    {
        public List<DiscordAuditLogEntry> AuditLogEntries { get; set; } = new();
        public List<DiscordGuildScheduledEvent> GuildScheduledEvents { get; set; } = new();
        public List<DiscordIntegration> Integrations { get; set; } = new();
        public List<DiscordChannel> Threads { get; set; } = new();
        public List<DiscordUser> Users { get; set; } = new();
        public List<DiscordWebhook> Webhooks { get; set; } = new();
    }

    public class DiscordIntegration
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool Enabled { get; set; }
        public bool? Syncing { get; set; }
        public string? RoleId { get; set; }
        public bool? EnableEmoticons { get; set; }
        public int? ExpireBehavior { get; set; }
        public int? ExpireGracePeriod { get; set; }
        public DiscordUser? User { get; set; }
        public DiscordIntegrationAccount? Account { get; set; }
        public DateTime? SyncedAt { get; set; }
        public int? SubscriberCount { get; set; }
        public bool? Revoked { get; set; }
        public DiscordIntegrationApplication? Application { get; set; }
        public List<string> Scopes { get; set; } = new();
    }

    public class DiscordGuildScheduledEvent
    {
        public string Id { get; set; } = string.Empty;
        public string GuildId { get; set; } = string.Empty;
        public string? ChannelId { get; set; }
        public string? CreatorId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? ScheduledStartTime { get; set; }
        public DateTime? ScheduledEndTime { get; set; }
        public int PrivacyLevel { get; set; }
        public int Status { get; set; }
        public int EntityType { get; set; }
        public string? EntityId { get; set; }
        public DiscordGuildScheduledEventEntityMetadata? EntityMetadata { get; set; }
        public DiscordUser? Creator { get; set; }
        public int? UserCount { get; set; }
        public string? Image { get; set; }
    }

    public class DiscordStageInstance
    {
        public string Id { get; set; } = string.Empty;
        public string GuildId { get; set; } = string.Empty;
        public string ChannelId { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public int PrivacyLevel { get; set; }
        public bool DiscoverableDisabled { get; set; }
        public string? GuildScheduledEventId { get; set; }
    }

    public class DiscordAutoModerationRule
    {
        public string Id { get; set; } = string.Empty;
        public string GuildId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CreatorId { get; set; } = string.Empty;
        public int EventType { get; set; }
        public int TriggerType { get; set; }
        public DiscordAutoModerationTriggerMetadata? TriggerMetadata { get; set; }
        public List<DiscordAutoModerationAction> Actions { get; set; } = new();
        public bool Enabled { get; set; }
        public List<string> ExemptRoles { get; set; } = new();
        public List<string> ExemptChannels { get; set; } = new();
    }
}