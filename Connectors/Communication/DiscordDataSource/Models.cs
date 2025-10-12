using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Communication.Discord
{
    // -------------------------------------------------------
    // Base
    // -------------------------------------------------------
    public abstract class DiscordEntityBase
    {
        [JsonIgnore] public IDataSource DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : DiscordEntityBase { DataSource = ds; return (T)this; }
    }

    /// <summary>
    /// Discord Guild (Server) entity
    /// </summary>
    public sealed class DiscordGuild : DiscordEntityBase
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("icon")]
        public string Icon { get; set; }

        [JsonPropertyName("icon_hash")]
        public string IconHash { get; set; }

        [JsonPropertyName("splash")]
        public string Splash { get; set; }

        [JsonPropertyName("discovery_splash")]
        public string DiscoverySplash { get; set; }

        [JsonPropertyName("owner")]
        public bool Owner { get; set; }

        [JsonPropertyName("owner_id")]
        public string OwnerId { get; set; }

        [JsonPropertyName("permissions")]
        public string Permissions { get; set; }

        [JsonPropertyName("region")]
        public string Region { get; set; }

        [JsonPropertyName("afk_channel_id")]
        public string AfkChannelId { get; set; }

        [JsonPropertyName("afk_timeout")]
        public int AfkTimeout { get; set; }

        [JsonPropertyName("widget_enabled")]
        public bool WidgetEnabled { get; set; }

        [JsonPropertyName("widget_channel_id")]
        public string WidgetChannelId { get; set; }

        [JsonPropertyName("verification_level")]
        public int VerificationLevel { get; set; }

        [JsonPropertyName("default_message_notifications")]
        public int DefaultMessageNotifications { get; set; }

        [JsonPropertyName("explicit_content_filter")]
        public int ExplicitContentFilter { get; set; }

        [JsonPropertyName("roles")]
        public List<DiscordRole> Roles { get; set; }

        [JsonPropertyName("emojis")]
        public List<DiscordEmoji> Emojis { get; set; }

        [JsonPropertyName("features")]
        public List<string> Features { get; set; }

        [JsonPropertyName("mfa_level")]
        public int MfaLevel { get; set; }

        [JsonPropertyName("application_id")]
        public string ApplicationId { get; set; }

        [JsonPropertyName("system_channel_id")]
        public string SystemChannelId { get; set; }

        [JsonPropertyName("system_channel_flags")]
        public int SystemChannelFlags { get; set; }

        [JsonPropertyName("rules_channel_id")]
        public string RulesChannelId { get; set; }

        [JsonPropertyName("joined_at")]
        public DateTime? JoinedAt { get; set; }

        [JsonPropertyName("large")]
        public bool Large { get; set; }

        [JsonPropertyName("unavailable")]
        public bool Unavailable { get; set; }

        [JsonPropertyName("member_count")]
        public int MemberCount { get; set; }

        [JsonPropertyName("voice_states")]
        public List<DiscordVoiceState> VoiceStates { get; set; }

        [JsonPropertyName("members")]
        public List<DiscordGuildMember> Members { get; set; }

        [JsonPropertyName("channels")]
        public List<DiscordChannel> Channels { get; set; }

        [JsonPropertyName("threads")]
        public List<DiscordChannel> Threads { get; set; }

        [JsonPropertyName("presences")]
        public List<DiscordPresence> Presences { get; set; }

        [JsonPropertyName("max_presences")]
        public int? MaxPresences { get; set; }

        [JsonPropertyName("max_members")]
        public int MaxMembers { get; set; }

        [JsonPropertyName("vanity_url_code")]
        public string VanityUrlCode { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("banner")]
        public string Banner { get; set; }

        [JsonPropertyName("premium_tier")]
        public int PremiumTier { get; set; }

        [JsonPropertyName("premium_subscription_count")]
        public int PremiumSubscriptionCount { get; set; }

        [JsonPropertyName("preferred_locale")]
        public string PreferredLocale { get; set; }

        [JsonPropertyName("public_updates_channel_id")]
        public string PublicUpdatesChannelId { get; set; }

        [JsonPropertyName("max_video_channel_users")]
        public int MaxVideoChannelUsers { get; set; }

        [JsonPropertyName("approximate_member_count")]
        public int ApproximateMemberCount { get; set; }

        [JsonPropertyName("approximate_presence_count")]
        public int ApproximatePresenceCount { get; set; }

        [JsonPropertyName("welcome_screen")]
        public DiscordWelcomeScreen WelcomeScreen { get; set; }

        [JsonPropertyName("nsfw_level")]
        public int NsfwLevel { get; set; }

        [JsonPropertyName("stage_instances")]
        public List<DiscordStageInstance> StageInstances { get; set; }

        [JsonPropertyName("stickers")]
        public List<DiscordSticker> Stickers { get; set; }

        [JsonPropertyName("guild_scheduled_events")]
        public List<DiscordGuildScheduledEvent> GuildScheduledEvents { get; set; }

        [JsonPropertyName("premium_progress_bar_enabled")]
        public bool PremiumProgressBarEnabled { get; set; }
    }

    /// <summary>
    /// Discord Channel entity
    /// </summary>
    public sealed class DiscordChannel : DiscordEntityBase
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("type")]
        public int Type { get; set; }

        [JsonPropertyName("guild_id")]
        public string GuildId { get; set; }

        [JsonPropertyName("position")]
        public int Position { get; set; }

        [JsonPropertyName("permission_overwrites")]
        public List<DiscordPermissionOverwrite> PermissionOverwrites { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("topic")]
        public string Topic { get; set; }

        [JsonPropertyName("nsfw")]
        public bool Nsfw { get; set; }

        [JsonPropertyName("last_message_id")]
        public string LastMessageId { get; set; }

        [JsonPropertyName("bitrate")]
        public int Bitrate { get; set; }

        [JsonPropertyName("user_limit")]
        public int UserLimit { get; set; }

        [JsonPropertyName("rate_limit_per_user")]
        public int RateLimitPerUser { get; set; }

        [JsonPropertyName("recipients")]
        public List<DiscordUser> Recipients { get; set; }

        [JsonPropertyName("icon")]
        public string Icon { get; set; }

        [JsonPropertyName("owner_id")]
        public string OwnerId { get; set; }

        [JsonPropertyName("application_id")]
        public string ApplicationId { get; set; }

        [JsonPropertyName("managed")]
        public bool Managed { get; set; }

        [JsonPropertyName("parent_id")]
        public string ParentId { get; set; }

        [JsonPropertyName("last_pin_timestamp")]
        public DateTime? LastPinTimestamp { get; set; }

        [JsonPropertyName("rtc_region")]
        public string RtcRegion { get; set; }

        [JsonPropertyName("video_quality_mode")]
        public int VideoQualityMode { get; set; }

        [JsonPropertyName("message_count")]
        public int MessageCount { get; set; }

        [JsonPropertyName("member_count")]
        public int MemberCount { get; set; }

        [JsonPropertyName("thread_metadata")]
        public DiscordThreadMetadata ThreadMetadata { get; set; }

        [JsonPropertyName("member")]
        public DiscordThreadMember Member { get; set; }

        [JsonPropertyName("default_auto_archive_duration")]
        public int DefaultAutoArchiveDuration { get; set; }

        [JsonPropertyName("permissions")]
        public string Permissions { get; set; }

        [JsonPropertyName("flags")]
        public int Flags { get; set; }
    }

    /// <summary>
    /// Discord Message entity
    /// </summary>
    public sealed class DiscordMessage : DiscordEntityBase
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("channel_id")]
        public string ChannelId { get; set; }

        [JsonPropertyName("guild_id")]
        public string GuildId { get; set; }

        [JsonPropertyName("author")]
        public DiscordUser Author { get; set; }

        [JsonPropertyName("member")]
        public DiscordGuildMember Member { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("edited_timestamp")]
        public DateTime? EditedTimestamp { get; set; }

        [JsonPropertyName("tts")]
        public bool Tts { get; set; }

        [JsonPropertyName("mention_everyone")]
        public bool MentionEveryone { get; set; }

        [JsonPropertyName("mentions")]
        public List<DiscordUser> Mentions { get; set; }

        [JsonPropertyName("mention_roles")]
        public List<string> MentionRoles { get; set; }

        [JsonPropertyName("mention_channels")]
        public List<DiscordChannelMention> MentionChannels { get; set; }

        [JsonPropertyName("attachments")]
        public List<DiscordAttachment> Attachments { get; set; }

        [JsonPropertyName("embeds")]
        public List<DiscordEmbed> Embeds { get; set; }

        [JsonPropertyName("reactions")]
        public List<DiscordReaction> Reactions { get; set; }

        [JsonPropertyName("nonce")]
        public object Nonce { get; set; }

        [JsonPropertyName("pinned")]
        public bool Pinned { get; set; }

        [JsonPropertyName("webhook_id")]
        public string WebhookId { get; set; }

        [JsonPropertyName("type")]
        public int Type { get; set; }

        [JsonPropertyName("activity")]
        public DiscordMessageActivity Activity { get; set; }

        [JsonPropertyName("application")]
        public DiscordApplication Application { get; set; }

        [JsonPropertyName("message_reference")]
        public DiscordMessageReference MessageReference { get; set; }

        [JsonPropertyName("flags")]
        public int Flags { get; set; }

        [JsonPropertyName("referenced_message")]
        public DiscordMessage ReferencedMessage { get; set; }

        [JsonPropertyName("interaction")]
        public DiscordMessageInteraction Interaction { get; set; }

        [JsonPropertyName("components")]
        public List<DiscordComponent> Components { get; set; }

        [JsonPropertyName("sticker_items")]
        public List<DiscordStickerItem> StickerItems { get; set; }

        [JsonPropertyName("stickers")]
        public List<DiscordSticker> Stickers { get; set; }

        [JsonPropertyName("position")]
        public int Position { get; set; }

        [JsonPropertyName("role_subscription_data")]
        public DiscordRoleSubscriptionData RoleSubscriptionData { get; set; }
    }

    /// <summary>
    /// Discord User entity
    /// </summary>
    public sealed class DiscordUser : DiscordEntityBase
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("discriminator")]
        public string Discriminator { get; set; }

        [JsonPropertyName("global_name")]
        public string GlobalName { get; set; }

        [JsonPropertyName("avatar")]
        public string Avatar { get; set; }

        [JsonPropertyName("bot")]
        public bool Bot { get; set; }

        [JsonPropertyName("system")]
        public bool System { get; set; }

        [JsonPropertyName("mfa_enabled")]
        public bool MfaEnabled { get; set; }

        [JsonPropertyName("banner")]
        public string Banner { get; set; }

        [JsonPropertyName("accent_color")]
        public int? AccentColor { get; set; }

        [JsonPropertyName("locale")]
        public string Locale { get; set; }

        [JsonPropertyName("verified")]
        public bool Verified { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("flags")]
        public int Flags { get; set; }

        [JsonPropertyName("premium_type")]
        public int PremiumType { get; set; }

        [JsonPropertyName("public_flags")]
        public int PublicFlags { get; set; }

        [JsonPropertyName("avatar_decoration")]
        public string AvatarDecoration { get; set; }
    }

    /// <summary>
    /// Discord Guild Member entity
    /// </summary>
    public sealed class DiscordGuildMember : DiscordEntityBase
    {
        [JsonPropertyName("user")]
        public DiscordUser User { get; set; }

        [JsonPropertyName("nick")]
        public string Nick { get; set; }

        [JsonPropertyName("avatar")]
        public string Avatar { get; set; }

        [JsonPropertyName("roles")]
        public List<string> Roles { get; set; }

        [JsonPropertyName("joined_at")]
        public DateTime JoinedAt { get; set; }

        [JsonPropertyName("premium_since")]
        public DateTime? PremiumSince { get; set; }

        [JsonPropertyName("deaf")]
        public bool Deaf { get; set; }

        [JsonPropertyName("mute")]
        public bool Mute { get; set; }

        [JsonPropertyName("flags")]
        public int Flags { get; set; }

        [JsonPropertyName("pending")]
        public bool Pending { get; set; }

        [JsonPropertyName("permissions")]
        public string Permissions { get; set; }

        [JsonPropertyName("communication_disabled_until")]
        public DateTime? CommunicationDisabledUntil { get; set; }
    }

    /// <summary>
    /// Discord Role entity
    /// </summary>
    public class DiscordRole
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("color")]
        public int Color { get; set; }

        [JsonPropertyName("hoist")]
        public bool Hoist { get; set; }

        [JsonPropertyName("icon")]
        public string Icon { get; set; }

        [JsonPropertyName("unicode_emoji")]
        public string UnicodeEmoji { get; set; }

        [JsonPropertyName("position")]
        public int Position { get; set; }

        [JsonPropertyName("permissions")]
        public string Permissions { get; set; }

        [JsonPropertyName("managed")]
        public bool Managed { get; set; }

        [JsonPropertyName("mentionable")]
        public bool Mentionable { get; set; }

        [JsonPropertyName("tags")]
        public DiscordRoleTags Tags { get; set; }
    }

    /// <summary>
    /// Discord Emoji entity
    /// </summary>
    public class DiscordEmoji
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("roles")]
        public List<string> Roles { get; set; }

        [JsonPropertyName("user")]
        public DiscordUser User { get; set; }

        [JsonPropertyName("require_colons")]
        public bool RequireColons { get; set; }

        [JsonPropertyName("managed")]
        public bool Managed { get; set; }

        [JsonPropertyName("animated")]
        public bool Animated { get; set; }

        [JsonPropertyName("available")]
        public bool Available { get; set; }
    }

    /// <summary>
    /// Discord Sticker entity
    /// </summary>
    public class DiscordSticker
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("pack_id")]
        public string PackId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("tags")]
        public string Tags { get; set; }

        [JsonPropertyName("asset")]
        public string Asset { get; set; }

        [JsonPropertyName("type")]
        public int Type { get; set; }

        [JsonPropertyName("format_type")]
        public int FormatType { get; set; }

        [JsonPropertyName("available")]
        public bool Available { get; set; }

        [JsonPropertyName("guild_id")]
        public string GuildId { get; set; }

        [JsonPropertyName("user")]
        public DiscordUser User { get; set; }

        [JsonPropertyName("sort_value")]
        public int SortValue { get; set; }
    }

    /// <summary>
    /// Discord Invite entity
    /// </summary>
    public class DiscordInvite
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("guild")]
        public DiscordGuild Guild { get; set; }

        [JsonPropertyName("channel")]
        public DiscordChannel Channel { get; set; }

        [JsonPropertyName("inviter")]
        public DiscordUser Inviter { get; set; }

        [JsonPropertyName("target_type")]
        public int? TargetType { get; set; }

        [JsonPropertyName("target_user")]
        public DiscordUser TargetUser { get; set; }

        [JsonPropertyName("target_application")]
        public DiscordApplication TargetApplication { get; set; }

        [JsonPropertyName("approximate_presence_count")]
        public int ApproximatePresenceCount { get; set; }

        [JsonPropertyName("approximate_member_count")]
        public int ApproximateMemberCount { get; set; }

        [JsonPropertyName("expires_at")]
        public DateTime? ExpiresAt { get; set; }

        [JsonPropertyName("stage_instance")]
        public DiscordInviteStageInstance StageInstance { get; set; }

        [JsonPropertyName("guild_scheduled_event")]
        public DiscordGuildScheduledEvent GuildScheduledEvent { get; set; }
    }

    /// <summary>
    /// Discord Voice State entity
    /// </summary>
    public class DiscordVoiceState
    {
        [JsonPropertyName("channel_id")]
        public string ChannelId { get; set; }

        [JsonPropertyName("user_id")]
        public string UserId { get; set; }

        [JsonPropertyName("session_id")]
        public string SessionId { get; set; }

        [JsonPropertyName("deaf")]
        public bool Deaf { get; set; }

        [JsonPropertyName("mute")]
        public bool Mute { get; set; }

        [JsonPropertyName("self_deaf")]
        public bool SelfDeaf { get; set; }

        [JsonPropertyName("self_mute")]
        public bool SelfMute { get; set; }

        [JsonPropertyName("self_stream")]
        public bool SelfStream { get; set; }

        [JsonPropertyName("self_video")]
        public bool SelfVideo { get; set; }

        [JsonPropertyName("suppress")]
        public bool Suppress { get; set; }

        [JsonPropertyName("request_to_speak_timestamp")]
        public DateTime? RequestToSpeakTimestamp { get; set; }

        [JsonPropertyName("member")]
        public DiscordGuildMember Member { get; set; }
    }

    /// <summary>
    /// Discord Webhook entity
    /// </summary>
    public class DiscordWebhook
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("type")]
        public int Type { get; set; }

        [JsonPropertyName("guild_id")]
        public string GuildId { get; set; }

        [JsonPropertyName("channel_id")]
        public string ChannelId { get; set; }

        [JsonPropertyName("user")]
        public DiscordUser User { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("avatar")]
        public string Avatar { get; set; }

        [JsonPropertyName("token")]
        public string Token { get; set; }

        [JsonPropertyName("application_id")]
        public string ApplicationId { get; set; }

        [JsonPropertyName("source_guild")]
        public DiscordGuild SourceGuild { get; set; }

        [JsonPropertyName("source_channel")]
        public DiscordChannel SourceChannel { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }
    }

    /// <summary>
    /// Discord Application entity
    /// </summary>
    public class DiscordApplication
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("icon")]
        public string Icon { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("rpc_origins")]
        public List<string> RpcOrigins { get; set; }

        [JsonPropertyName("bot_public")]
        public bool BotPublic { get; set; }

        [JsonPropertyName("bot_require_code_grant")]
        public bool BotRequireCodeGrant { get; set; }

        [JsonPropertyName("bot")]
        public DiscordUser Bot { get; set; }

        [JsonPropertyName("terms_of_service_url")]
        public string TermsOfServiceUrl { get; set; }

        [JsonPropertyName("privacy_policy_url")]
        public string PrivacyPolicyUrl { get; set; }

        [JsonPropertyName("owner")]
        public DiscordUser Owner { get; set; }

        [JsonPropertyName("summary")]
        public string Summary { get; set; }

        [JsonPropertyName("verify_key")]
        public string VerifyKey { get; set; }

        [JsonPropertyName("team")]
        public DiscordTeam Team { get; set; }

        [JsonPropertyName("guild_id")]
        public string GuildId { get; set; }

        [JsonPropertyName("primary_sku_id")]
        public string PrimarySkuId { get; set; }

        [JsonPropertyName("slug")]
        public string Slug { get; set; }

        [JsonPropertyName("cover_image")]
        public string CoverImage { get; set; }

        [JsonPropertyName("flags")]
        public int Flags { get; set; }

        [JsonPropertyName("approximate_guild_count")]
        public int ApproximateGuildCount { get; set; }

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; }

        [JsonPropertyName("install_params")]
        public DiscordInstallParams InstallParams { get; set; }

        [JsonPropertyName("custom_install_url")]
        public string CustomInstallUrl { get; set; }
    }

    /// <summary>
    /// Discord Audit Log entity
    /// </summary>
    public class DiscordAuditLog
    {
        [JsonPropertyName("audit_log_entries")]
        public List<DiscordAuditLogEntry> AuditLogEntries { get; set; }

        [JsonPropertyName("guild_scheduled_events")]
        public List<DiscordGuildScheduledEvent> GuildScheduledEvents { get; set; }

        [JsonPropertyName("integrations")]
        public List<DiscordIntegration> Integrations { get; set; }

        [JsonPropertyName("threads")]
        public List<DiscordChannel> Threads { get; set; }

        [JsonPropertyName("users")]
        public List<DiscordUser> Users { get; set; }

        [JsonPropertyName("webhooks")]
        public List<DiscordWebhook> Webhooks { get; set; }
    }

    /// <summary>
    /// Discord Audit Log Entry entity
    /// </summary>
    public class DiscordAuditLogEntry
    {
        [JsonPropertyName("target_id")]
        public string TargetId { get; set; }

        [JsonPropertyName("changes")]
        public List<DiscordAuditLogChange> Changes { get; set; }

        [JsonPropertyName("user_id")]
        public string UserId { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("action_type")]
        public int ActionType { get; set; }

        [JsonPropertyName("options")]
        public DiscordOptionalAuditEntryInfo Options { get; set; }

        [JsonPropertyName("reason")]
        public string Reason { get; set; }
    }

    /// <summary>
    /// Discord Integration entity
    /// </summary>
    public class DiscordIntegration
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [JsonPropertyName("syncing")]
        public bool Syncing { get; set; }

        [JsonPropertyName("role_id")]
        public string RoleId { get; set; }

        [JsonPropertyName("enable_emoticons")]
        public bool EnableEmoticons { get; set; }

        [JsonPropertyName("expire_behavior")]
        public int ExpireBehavior { get; set; }

        [JsonPropertyName("expire_grace_period")]
        public int ExpireGracePeriod { get; set; }

        [JsonPropertyName("user")]
        public DiscordUser User { get; set; }

        [JsonPropertyName("account")]
        public DiscordIntegrationAccount Account { get; set; }

        [JsonPropertyName("synced_at")]
        public DateTime SyncedAt { get; set; }

        [JsonPropertyName("subscriber_count")]
        public int SubscriberCount { get; set; }

        [JsonPropertyName("revoked")]
        public bool Revoked { get; set; }

        [JsonPropertyName("application")]
        public DiscordIntegrationApplication Application { get; set; }

        [JsonPropertyName("scopes")]
        public List<string> Scopes { get; set; }
    }

    /// <summary>
    /// Discord Guild Scheduled Event entity
    /// </summary>
    public class DiscordGuildScheduledEvent
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("guild_id")]
        public string GuildId { get; set; }

        [JsonPropertyName("channel_id")]
        public string ChannelId { get; set; }

        [JsonPropertyName("creator_id")]
        public string CreatorId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("scheduled_start_time")]
        public DateTime ScheduledStartTime { get; set; }

        [JsonPropertyName("scheduled_end_time")]
        public DateTime? ScheduledEndTime { get; set; }

        [JsonPropertyName("privacy_level")]
        public int PrivacyLevel { get; set; }

        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("entity_type")]
        public int EntityType { get; set; }

        [JsonPropertyName("entity_id")]
        public string EntityId { get; set; }

        [JsonPropertyName("entity_metadata")]
        public DiscordGuildScheduledEventEntityMetadata EntityMetadata { get; set; }

        [JsonPropertyName("creator")]
        public DiscordUser Creator { get; set; }

        [JsonPropertyName("user_count")]
        public int UserCount { get; set; }

        [JsonPropertyName("image")]
        public string Image { get; set; }
    }

    /// <summary>
    /// Discord Stage Instance entity
    /// </summary>
    public class DiscordStageInstance
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("guild_id")]
        public string GuildId { get; set; }

        [JsonPropertyName("channel_id")]
        public string ChannelId { get; set; }

        [JsonPropertyName("topic")]
        public string Topic { get; set; }

        [JsonPropertyName("privacy_level")]
        public int PrivacyLevel { get; set; }

        [JsonPropertyName("discoverable_disabled")]
        public bool DiscoverableDisabled { get; set; }

        [JsonPropertyName("guild_scheduled_event_id")]
        public string GuildScheduledEventId { get; set; }
    }

    /// <summary>
    /// Discord Auto Moderation Rule entity
    /// </summary>
    public class DiscordAutoModerationRule
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("guild_id")]
        public string GuildId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("creator_id")]
        public string CreatorId { get; set; }

        [JsonPropertyName("event_type")]
        public int EventType { get; set; }

        [JsonPropertyName("trigger_type")]
        public int TriggerType { get; set; }

        [JsonPropertyName("trigger_metadata")]
        public DiscordAutoModerationTriggerMetadata TriggerMetadata { get; set; }

        [JsonPropertyName("actions")]
        public List<DiscordAutoModerationAction> Actions { get; set; }

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [JsonPropertyName("exempt_roles")]
        public List<string> ExemptRoles { get; set; }

        [JsonPropertyName("exempt_channels")]
        public List<string> ExemptChannels { get; set; }
    }

    // Supporting classes for Discord models

    public class DiscordPermissionOverwrite
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("type")]
        public int Type { get; set; }

        [JsonPropertyName("allow")]
        public string Allow { get; set; }

        [JsonPropertyName("deny")]
        public string Deny { get; set; }
    }

    public class DiscordThreadMetadata
    {
        [JsonPropertyName("archived")]
        public bool Archived { get; set; }

        [JsonPropertyName("auto_archive_duration")]
        public int AutoArchiveDuration { get; set; }

        [JsonPropertyName("archive_timestamp")]
        public DateTime ArchiveTimestamp { get; set; }

        [JsonPropertyName("locked")]
        public bool Locked { get; set; }

        [JsonPropertyName("invitable")]
        public bool Invitable { get; set; }

        [JsonPropertyName("create_timestamp")]
        public DateTime? CreateTimestamp { get; set; }
    }

    public class DiscordThreadMember
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("user_id")]
        public string UserId { get; set; }

        [JsonPropertyName("join_timestamp")]
        public DateTime JoinTimestamp { get; set; }

        [JsonPropertyName("flags")]
        public int Flags { get; set; }
    }

    public class DiscordWelcomeScreen
    {
        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("welcome_channels")]
        public List<DiscordWelcomeScreenChannel> WelcomeChannels { get; set; }
    }

    public class DiscordWelcomeScreenChannel
    {
        [JsonPropertyName("channel_id")]
        public string ChannelId { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("emoji_id")]
        public string EmojiId { get; set; }

        [JsonPropertyName("emoji_name")]
        public string EmojiName { get; set; }
    }

    public class DiscordPresence
    {
        [JsonPropertyName("user")]
        public DiscordUser User { get; set; }

        [JsonPropertyName("guild_id")]
        public string GuildId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("activities")]
        public List<DiscordActivity> Activities { get; set; }

        [JsonPropertyName("client_status")]
        public DiscordClientStatus ClientStatus { get; set; }
    }

    public class DiscordActivity
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public int Type { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("created_at")]
        public long CreatedAt { get; set; }

        [JsonPropertyName("timestamps")]
        public DiscordActivityTimestamps Timestamps { get; set; }

        [JsonPropertyName("application_id")]
        public string ApplicationId { get; set; }

        [JsonPropertyName("details")]
        public string Details { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }

        [JsonPropertyName("emoji")]
        public DiscordActivityEmoji Emoji { get; set; }

        [JsonPropertyName("party")]
        public DiscordActivityParty Party { get; set; }

        [JsonPropertyName("assets")]
        public DiscordActivityAssets Assets { get; set; }

        [JsonPropertyName("secrets")]
        public DiscordActivitySecrets Secrets { get; set; }

        [JsonPropertyName("instance")]
        public bool Instance { get; set; }

        [JsonPropertyName("flags")]
        public int Flags { get; set; }

        [JsonPropertyName("buttons")]
        public List<DiscordActivityButton> Buttons { get; set; }
    }

    // Additional supporting classes would go here...
    // For brevity, I'll include the most essential ones

    public class DiscordAttachment
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("filename")]
        public string Filename { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("content_type")]
        public string ContentType { get; set; }

        [JsonPropertyName("size")]
        public int Size { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("proxy_url")]
        public string ProxyUrl { get; set; }

        [JsonPropertyName("height")]
        public int? Height { get; set; }

        [JsonPropertyName("width")]
        public int? Width { get; set; }

        [JsonPropertyName("ephemeral")]
        public bool Ephemeral { get; set; }
    }

    public class DiscordEmbed
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime? Timestamp { get; set; }

        [JsonPropertyName("color")]
        public int? Color { get; set; }

        [JsonPropertyName("footer")]
        public DiscordEmbedFooter Footer { get; set; }

        [JsonPropertyName("image")]
        public DiscordEmbedImage Image { get; set; }

        [JsonPropertyName("thumbnail")]
        public DiscordEmbedThumbnail Thumbnail { get; set; }

        [JsonPropertyName("video")]
        public DiscordEmbedVideo Video { get; set; }

        [JsonPropertyName("provider")]
        public DiscordEmbedProvider Provider { get; set; }

        [JsonPropertyName("author")]
        public DiscordEmbedAuthor Author { get; set; }

        [JsonPropertyName("fields")]
        public List<DiscordEmbedField> Fields { get; set; }
    }

    public class DiscordReaction
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("me")]
        public bool Me { get; set; }

        [JsonPropertyName("emoji")]
        public DiscordEmoji Emoji { get; set; }
    }

    public class DiscordRoleTags
    {
        [JsonPropertyName("bot_id")]
        public string BotId { get; set; }

        [JsonPropertyName("integration_id")]
        public string IntegrationId { get; set; }

        [JsonPropertyName("premium_subscriber")]
        public object PremiumSubscriber { get; set; }

        [JsonPropertyName("subscription_listing_id")]
        public string SubscriptionListingId { get; set; }

        [JsonPropertyName("available_for_purchase")]
        public object AvailableForPurchase { get; set; }

        [JsonPropertyName("guild_connections")]
        public object GuildConnections { get; set; }
    }

    public class DiscordInviteStageInstance
    {
        [JsonPropertyName("members")]
        public List<DiscordGuildMember> Members { get; set; }

        [JsonPropertyName("participant_count")]
        public int ParticipantCount { get; set; }

        [JsonPropertyName("speaker_count")]
        public int SpeakerCount { get; set; }

        [JsonPropertyName("topic")]
        public string Topic { get; set; }
    }

    public class DiscordTeam
    {
        [JsonPropertyName("icon")]
        public string Icon { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("members")]
        public List<DiscordTeamMember> Members { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("owner_user_id")]
        public string OwnerUserId { get; set; }
    }

    public class DiscordInstallParams
    {
        [JsonPropertyName("scopes")]
        public List<string> Scopes { get; set; }

        [JsonPropertyName("permissions")]
        public string Permissions { get; set; }
    }

    public class DiscordAuditLogChange
    {
        [JsonPropertyName("new_value")]
        public object NewValue { get; set; }

        [JsonPropertyName("old_value")]
        public object OldValue { get; set; }

        [JsonPropertyName("key")]
        public string Key { get; set; }
    }

    public class DiscordOptionalAuditEntryInfo
    {
        [JsonPropertyName("delete_member_days")]
        public string DeleteMemberDays { get; set; }

        [JsonPropertyName("members_removed")]
        public string MembersRemoved { get; set; }

        [JsonPropertyName("channel_id")]
        public string ChannelId { get; set; }

        [JsonPropertyName("count")]
        public string Count { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("role_name")]
        public string RoleName { get; set; }
    }

    public class DiscordIntegrationAccount
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class DiscordIntegrationApplication
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("icon")]
        public string Icon { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("summary")]
        public string Summary { get; set; }

        [JsonPropertyName("bot")]
        public DiscordUser Bot { get; set; }
    }

    public class DiscordGuildScheduledEventEntityMetadata
    {
        [JsonPropertyName("location")]
        public string Location { get; set; }
    }

    public class DiscordAutoModerationTriggerMetadata
    {
        [JsonPropertyName("keyword_filter")]
        public List<string> KeywordFilter { get; set; }

        [JsonPropertyName("regex_patterns")]
        public List<string> RegexPatterns { get; set; }

        [JsonPropertyName("presets")]
        public List<int> Presets { get; set; }

        [JsonPropertyName("allow_list")]
        public List<string> AllowList { get; set; }

        [JsonPropertyName("mention_total_limit")]
        public int MentionTotalLimit { get; set; }

        [JsonPropertyName("mention_raid_protection_enabled")]
        public bool MentionRaidProtectionEnabled { get; set; }
    }

    public class DiscordAutoModerationAction
    {
        [JsonPropertyName("type")]
        public int Type { get; set; }

        [JsonPropertyName("metadata")]
        public DiscordAutoModerationActionMetadata Metadata { get; set; }
    }

    public class DiscordAutoModerationActionMetadata
    {
        [JsonPropertyName("channel_id")]
        public string ChannelId { get; set; }

        [JsonPropertyName("duration_seconds")]
        public int DurationSeconds { get; set; }

        [JsonPropertyName("custom_message")]
        public string CustomMessage { get; set; }
    }

    // Additional supporting classes for messages and embeds
    public class DiscordChannelMention
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("guild_id")]
        public string GuildId { get; set; }

        [JsonPropertyName("type")]
        public int Type { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class DiscordMessageActivity
    {
        [JsonPropertyName("type")]
        public int Type { get; set; }

        [JsonPropertyName("party_id")]
        public string PartyId { get; set; }
    }

    public class DiscordMessageReference
    {
        [JsonPropertyName("message_id")]
        public string MessageId { get; set; }

        [JsonPropertyName("channel_id")]
        public string ChannelId { get; set; }

        [JsonPropertyName("guild_id")]
        public string GuildId { get; set; }

        [JsonPropertyName("fail_if_not_exists")]
        public bool FailIfNotExists { get; set; }
    }

    public class DiscordMessageInteraction
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("type")]
        public int Type { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("user")]
        public DiscordUser User { get; set; }

        [JsonPropertyName("member")]
        public DiscordGuildMember Member { get; set; }
    }

    public class DiscordComponent
    {
        [JsonPropertyName("type")]
        public int Type { get; set; }

        [JsonPropertyName("custom_id")]
        public string CustomId { get; set; }

        [JsonPropertyName("disabled")]
        public bool Disabled { get; set; }

        [JsonPropertyName("style")]
        public int Style { get; set; }

        [JsonPropertyName("label")]
        public string Label { get; set; }

        [JsonPropertyName("emoji")]
        public DiscordComponentEmoji Emoji { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("options")]
        public List<DiscordSelectOption> Options { get; set; }

        [JsonPropertyName("placeholder")]
        public string Placeholder { get; set; }

        [JsonPropertyName("min_values")]
        public int MinValues { get; set; }

        [JsonPropertyName("max_values")]
        public int MaxValues { get; set; }

        [JsonPropertyName("components")]
        public List<DiscordComponent> Components { get; set; }
    }

    public class DiscordStickerItem
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("format_type")]
        public int FormatType { get; set; }
    }

    public class DiscordRoleSubscriptionData
    {
        [JsonPropertyName("role_subscription_listing_id")]
        public string RoleSubscriptionListingId { get; set; }

        [JsonPropertyName("tier_name")]
        public string TierName { get; set; }

        [JsonPropertyName("total_months_subscribed")]
        public int TotalMonthsSubscribed { get; set; }

        [JsonPropertyName("is_renewal")]
        public bool IsRenewal { get; set; }
    }

    // Supporting classes for embeds
    public class DiscordEmbedFooter
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("icon_url")]
        public string IconUrl { get; set; }

        [JsonPropertyName("proxy_icon_url")]
        public string ProxyIconUrl { get; set; }
    }

    public class DiscordEmbedImage
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("proxy_url")]
        public string ProxyUrl { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }
    }

    public class DiscordEmbedThumbnail
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("proxy_url")]
        public string ProxyUrl { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }
    }

    public class DiscordEmbedVideo
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("proxy_url")]
        public string ProxyUrl { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }
    }

    public class DiscordEmbedProvider
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }
    }

    public class DiscordEmbedAuthor
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("icon_url")]
        public string IconUrl { get; set; }

        [JsonPropertyName("proxy_icon_url")]
        public string ProxyIconUrl { get; set; }
    }

    public class DiscordEmbedField
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }

        [JsonPropertyName("inline")]
        public bool Inline { get; set; }
    }

    // Additional supporting classes
    public class DiscordClientStatus
    {
        [JsonPropertyName("desktop")]
        public string Desktop { get; set; }

        [JsonPropertyName("mobile")]
        public string Mobile { get; set; }

        [JsonPropertyName("web")]
        public string Web { get; set; }
    }

    public class DiscordActivityTimestamps
    {
        [JsonPropertyName("start")]
        public long? Start { get; set; }

        [JsonPropertyName("end")]
        public long? End { get; set; }
    }

    public class DiscordActivityEmoji
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("animated")]
        public bool Animated { get; set; }
    }

    public class DiscordActivityParty
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("size")]
        public List<int> Size { get; set; }
    }

    public class DiscordActivityAssets
    {
        [JsonPropertyName("large_image")]
        public string LargeImage { get; set; }

        [JsonPropertyName("large_text")]
        public string LargeText { get; set; }

        [JsonPropertyName("small_image")]
        public string SmallImage { get; set; }

        [JsonPropertyName("small_text")]
        public string SmallText { get; set; }
    }

    public class DiscordActivitySecrets
    {
        [JsonPropertyName("join")]
        public string Join { get; set; }

        [JsonPropertyName("spectate")]
        public string Spectate { get; set; }

        [JsonPropertyName("match")]
        public string Match { get; set; }
    }

    public class DiscordActivityButton
    {
        [JsonPropertyName("label")]
        public string Label { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }
    }

    public class DiscordTeamMember
    {
        [JsonPropertyName("membership_state")]
        public int MembershipState { get; set; }

        [JsonPropertyName("permissions")]
        public List<string> Permissions { get; set; }

        [JsonPropertyName("team_id")]
        public string TeamId { get; set; }

        [JsonPropertyName("user")]
        public DiscordUser User { get; set; }
    }

    public class DiscordComponentEmoji
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("animated")]
        public bool Animated { get; set; }
    }

    public class DiscordSelectOption
    {
        [JsonPropertyName("label")]
        public string Label { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("emoji")]
        public DiscordComponentEmoji Emoji { get; set; }

        [JsonPropertyName("default")]
        public bool Default { get; set; }
    }
}