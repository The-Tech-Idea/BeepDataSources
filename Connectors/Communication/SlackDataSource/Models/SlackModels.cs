using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Connectors.Communication.Slack.Models
{
    public class SlackMessage
    {
        public string Type { get; set; } = string.Empty;
        public string? Subtype { get; set; }
        public string Text { get; set; } = string.Empty;
        public string? User { get; set; }
        public string? BotId { get; set; }
        public string? Username { get; set; }
        public string Ts { get; set; } = string.Empty;
        public string? ThreadTs { get; set; }
        public int ReplyCount { get; set; }
        public List<string> ReplyUsers { get; set; } = new();
        public int ReplyUsersCount { get; set; }
        public string? LatestReply { get; set; }
        public List<SlackMessage> Replies { get; set; } = new();
        public List<SlackAttachment> Attachments { get; set; } = new();
        public List<SlackBlock> Blocks { get; set; } = new();
        public SlackFile? File { get; set; }
        public List<SlackReaction> Reactions { get; set; } = new();
        public bool IsStarred { get; set; }
        public List<string> PinnedTo { get; set; } = new();
        public string? ClientMsgId { get; set; }
        public string? Team { get; set; }
        public string? Channel { get; set; }
        public string? EventTs { get; set; }
        public string? ChannelType { get; set; }
        public List<SlackFile> Files { get; set; } = new();
        public bool Upload { get; set; }
        public bool DisplayAsBot { get; set; }
        public string? XFiles { get; set; }
        public string? Inviter { get; set; }
        public string? Purpose { get; set; }
        public string? Topic { get; set; }
        public string? Name { get; set; }
        public string? OldName { get; set; }
        public List<string> Members { get; set; } = new();
        public string? Creator { get; set; }
        public bool IsChannel { get; set; }
        public bool IsGroup { get; set; }
        public bool IsIm { get; set; }
        public bool IsMpim { get; set; }
        public bool IsPrivate { get; set; }
        public DateTime? Created { get; set; }
        public string? UserProfile { get; set; }
        public string? ItemType { get; set; }
        public string? Item { get; set; }
        public string? Reaction { get; set; }
        public string? ItemUser { get; set; }
        public string? AddedTs { get; set; }
        public string? RemovedTs { get; set; }
        public string? Hidden { get; set; }
    }

    public class SlackChannel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsChannel { get; set; }
        public bool IsGroup { get; set; }
        public bool IsIm { get; set; }
        public bool IsMpim { get; set; }
        public bool IsPrivate { get; set; }
        public DateTime Created { get; set; }
        public string Creator { get; set; } = string.Empty;
        public bool IsArchived { get; set; }
        public bool IsGeneral { get; set; }
        public List<string> Members { get; set; } = new();
        public string? Topic { get; set; }
        public string? Purpose { get; set; }
        public string? LastRead { get; set; }
        public int UnreadCount { get; set; }
        public int UnreadCountDisplay { get; set; }
        public string? Latest { get; set; }
        public string? User { get; set; }
        public SlackChannelTopic? TopicDetails { get; set; }
        public SlackChannelPurpose? PurposeDetails { get; set; }
        public int Priority { get; set; }
    }

    public class SlackUser
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool Deleted { get; set; }
        public string? Color { get; set; }
        public string? RealName { get; set; }
        public string? Tz { get; set; }
        public string? TzLabel { get; set; }
        public int TzOffset { get; set; }
        public SlackUserProfile? Profile { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsOwner { get; set; }
        public bool IsPrimaryOwner { get; set; }
        public bool IsRestricted { get; set; }
        public bool IsUltraRestricted { get; set; }
        public bool IsBot { get; set; }
        public bool IsAppUser { get; set; }
        public DateTime? Updated { get; set; }
        public bool Has2Fa { get; set; }
        public string? Locale { get; set; }
    }

    public class SlackFile
    {
        public string Id { get; set; } = string.Empty;
        public DateTime Created { get; set; }
        public DateTime Timestamp { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Mimetype { get; set; } = string.Empty;
        public string Filetype { get; set; } = string.Empty;
        public string PrettyType { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public bool Editable { get; set; }
        public int Size { get; set; }
        public string Mode { get; set; } = string.Empty;
        public bool IsExternal { get; set; }
        public string? ExternalType { get; set; }
        public bool IsPublic { get; set; }
        public bool PublicUrlShared { get; set; }
        public bool DisplayAsBot { get; set; }
        public string? Username { get; set; }
        public string? UrlPrivate { get; set; }
        public string? UrlPrivateDownload { get; set; }
        public string? Thumb64 { get; set; }
        public string? Thumb80 { get; set; }
        public string? Thumb360 { get; set; }
        public string? Thumb360W { get; set; }
        public string? Thumb360H { get; set; }
        public string? Thumb480 { get; set; }
        public string? Thumb480W { get; set; }
        public string? Thumb480H { get; set; }
        public string? Thumb160 { get; set; }
        public string? Thumb720 { get; set; }
        public string? Thumb720W { get; set; }
        public string? Thumb720H { get; set; }
        public string? Thumb800 { get; set; }
        public string? Thumb800W { get; set; }
        public string? Thumb800H { get; set; }
        public string? Thumb960 { get; set; }
        public string? Thumb960W { get; set; }
        public string? Thumb960H { get; set; }
        public string? Thumb1024 { get; set; }
        public string? Thumb1024W { get; set; }
        public string? Thumb1024H { get; set; }
        public string? Permalink { get; set; }
        public string? PermalinkPublic { get; set; }
        public bool HasRichPreview { get; set; }
        public string? MediaDisplayType { get; set; }
        public List<SlackReaction> Reactions { get; set; } = new();
        public List<string> Channels { get; set; } = new();
        public List<string> Groups { get; set; } = new();
        public List<string> Ims { get; set; } = new();
        public int CommentsCount { get; set; }
        public int NumStars { get; set; }
        public bool IsStarred { get; set; }
        public List<string> PinnedTo { get; set; } = new();
    }

    public class SlackTeam
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string EmailDomain { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string? EnterpriseId { get; set; }
        public string? EnterpriseName { get; set; }
    }

    // Additional models for remaining Map entities
    public class SlackStar
    {
        public string Type { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string File { get; set; } = string.Empty;
        public string FileComment { get; set; } = string.Empty;
        public DateTime DateCreate { get; set; }
    }

    public class SlackPin
    {
        public string Type { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public SlackMessage? Message { get; set; }
        public DateTime Created { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class SlackReminder
    {
        public string Id { get; set; } = string.Empty;
        public string Creator { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public bool Recurring { get; set; }
        public DateTime Time { get; set; }
        public DateTime CompleteTs { get; set; }
    }

    public class SlackSearchResult
    {
        public List<SlackMessage> Messages { get; set; } = new();
        public List<SlackFile> Files { get; set; } = new();
        public string Query { get; set; } = string.Empty;
    }

    public class SlackAuth
    {
        public string User { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Team { get; set; } = string.Empty;
        public string TeamId { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }

    public class SlackBot
    {
        public string Id { get; set; } = string.Empty;
        public bool Deleted { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime Updated { get; set; }
        public string? AppId { get; set; }
        public SlackUser? User { get; set; }
        public List<string> Icons { get; set; } = new();
    }

    public class SlackApp
    {
        public string AppId { get; set; } = string.Empty;
        public string AppName { get; set; } = string.Empty;
        public List<SlackAppScope> Scopes { get; set; } = new();
        public string? Description { get; set; }
        public bool IsDistributed { get; set; }
        public bool IsUnderTeamDevelopment { get; set; }
    }

    public class SlackAppScope
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsSensitive { get; set; }
        public string TokenType { get; set; } = string.Empty;
    }

    public class SlackUserGroup
    {
        public string Id { get; set; } = string.Empty;
        public string TeamId { get; set; } = string.Empty;
        public bool IsUsergroup { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Handle { get; set; } = string.Empty;
        public bool IsExternal { get; set; }
        public DateTime DateCreate { get; set; }
        public DateTime DateUpdate { get; set; }
        public DateTime DateDelete { get; set; }
        public string AutoType { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public string DeletedBy { get; set; } = string.Empty;
        public string Prefs { get; set; } = string.Empty;
        public List<string> Users { get; set; } = new();
        public List<string> UserCount { get; set; } = new();
    }

    // Supporting classes
    public class SlackAttachment
    {
        public string? Fallback { get; set; }
        public string? Color { get; set; }
        public string? Pretext { get; set; }
        public string? AuthorName { get; set; }
        public string? AuthorLink { get; set; }
        public string? AuthorIcon { get; set; }
        public string? Title { get; set; }
        public string? TitleLink { get; set; }
        public string? Text { get; set; }
        public List<SlackAttachmentField> Fields { get; set; } = new();
        public string? ImageUrl { get; set; }
        public string? ThumbUrl { get; set; }
        public string? Footer { get; set; }
        public string? FooterIcon { get; set; }
        public string? Ts { get; set; }
        public List<string> MrkdwnIn { get; set; } = new();
        public string? CallbackId { get; set; }
        public List<SlackAttachmentAction> Actions { get; set; } = new();
    }

    public class SlackBlock
    {
        public string Type { get; set; } = string.Empty;
        public string? BlockId { get; set; }
        public SlackBlockText? Text { get; set; }
        public List<SlackBlockElement> Elements { get; set; } = new();
        public List<SlackBlock> Blocks { get; set; } = new();
        public string? ExternalId { get; set; }
        public string? PrivateMetadata { get; set; }
        public string? CallbackId { get; set; }
        public string? State { get; set; }
        public string? Hash { get; set; }
        public string? ClearOnClose { get; set; }
        public string? NotifyOnClose { get; set; }
        public string? Submit { get; set; }
        public string? Close { get; set; }
        public string? ActionId { get; set; }
        public string? Url { get; set; }
        public string? Value { get; set; }
        public string? Style { get; set; }
        public string? Confirm { get; set; }
        public string? Placeholder { get; set; }
        public string? InitialValue { get; set; }
        public string? Multiline { get; set; }
        public string? MinLength { get; set; }
        public string? MaxLength { get; set; }
        public string? DispatchAction { get; set; }
        public string? InitialOption { get; set; }
        public string? InitialDate { get; set; }
        public string? InitialTime { get; set; }
        public string? Timezone { get; set; }
    }

    public class SlackReaction
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Users { get; set; } = new();
        public int Count { get; set; }
    }

    // Simplified supporting classes
    public class SlackUserProfile { public string? AvatarHash { get; set; } public string? StatusText { get; set; } public string? StatusEmoji { get; set; } public string? RealName { get; set; } public string? Caption { get; set; } public string? RealNameNormalized { get; set; } public string? CaptionNormalized { get; set; } public string? Email { get; set; } public string? TeamId { get; set; } }
    public class SlackChannelTopic { public string? Value { get; set; } public string? Creator { get; set; } public DateTime? LastSet { get; set; } }
    public class SlackChannelPurpose { public string? Value { get; set; } public string? Creator { get; set; } public DateTime? LastSet { get; set; } }
    public class SlackAttachmentField { public string? Title { get; set; } public string? Value { get; set; } public bool? Short { get; set; } }
    public class SlackAttachmentAction { public string? Name { get; set; } public string? Text { get; set; } public string? Type { get; set; } public string? Value { get; set; } public string? Url { get; set; } public string? Style { get; set; } public SlackAttachmentConfirm? Confirm { get; set; } }
    public class SlackAttachmentConfirm { public string? Title { get; set; } public string? Text { get; set; } public string? OkText { get; set; } public string? DismissText { get; set; } }
    public class SlackBlockText { public string Type { get; set; } = string.Empty; public string Text { get; set; } = string.Empty; public bool? Emoji { get; set; } public bool? Verbatim { get; set; } }
    public class SlackBlockElement { public string? Type { get; set; } public string? ActionId { get; set; } public string? Text { get; set; } public string? Value { get; set; } public string? Url { get; set; } public string? Style { get; set; } public SlackBlockConfirm? Confirm { get; set; } public string? Placeholder { get; set; } public string? InitialValue { get; set; } public string? Multiline { get; set; } public string? MinLength { get; set; } public string? MaxLength { get; set; } public string? DispatchAction { get; set; } public string? InitialOption { get; set; } public string? InitialDate { get; set; } public string? InitialTime { get; set; } public string? Timezone { get; set; } public List<SlackBlockOption> Options { get; set; } = new(); public List<SlackBlockOptionGroup> OptionGroups { get; set; } = new(); }
    public class SlackBlockConfirm { public SlackBlockText? Title { get; set; } public SlackBlockText? Text { get; set; } public SlackBlockText? Confirm { get; set; } public SlackBlockText? Deny { get; set; } public string? Style { get; set; } }
    public class SlackBlockOption { public SlackBlockText? Text { get; set; } public string? Value { get; set; } public string? Url { get; set; } }
    public class SlackBlockOptionGroup { public SlackBlockText? Label { get; set; } public List<SlackBlockOption> Options { get; set; } = new(); }
}