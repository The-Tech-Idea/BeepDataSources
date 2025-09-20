using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Connectors.Communication.Telegram.Models
{
    public class TelegramUpdate
    {
        public int UpdateId { get; set; }
        public TelegramMessage? Message { get; set; }
        public TelegramMessage? EditedMessage { get; set; }
        public TelegramMessage? ChannelPost { get; set; }
        public TelegramMessage? EditedChannelPost { get; set; }
        public TelegramInlineQuery? InlineQuery { get; set; }
        public TelegramChosenInlineResult? ChosenInlineResult { get; set; }
        public TelegramCallbackQuery? CallbackQuery { get; set; }
        public TelegramShippingQuery? ShippingQuery { get; set; }
        public TelegramPreCheckoutQuery? PreCheckoutQuery { get; set; }
        public TelegramPoll? Poll { get; set; }
        public TelegramPollAnswer? PollAnswer { get; set; }
        public TelegramChatMemberUpdated? ChatMember { get; set; }
        public TelegramChatMemberUpdated? MyChatMember { get; set; }
        public TelegramChatJoinRequest? ChatJoinRequest { get; set; }
    }

    public class TelegramUser
    {
        public long Id { get; set; }
        public bool IsBot { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Username { get; set; }
        public string? LanguageCode { get; set; }
        public bool? IsPremium { get; set; }
        public bool? AddedToAttachmentMenu { get; set; }
        public bool? CanJoinGroups { get; set; }
        public bool? CanReadAllGroupMessages { get; set; }
        public bool? SupportsInlineQueries { get; set; }
    }

    public class TelegramChat
    {
        public long Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? Username { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public bool? IsForum { get; set; }
        public TelegramChatPhoto? Photo { get; set; }
        public List<string> ActiveUsernames { get; set; } = new();
        public string? EmojiStatusCustomEmojiId { get; set; }
        public string? Bio { get; set; }
        public bool? HasPrivateForwards { get; set; }
        public bool? HasRestrictedVoiceAndVideoMessages { get; set; }
        public bool? JoinToSendMessages { get; set; }
        public bool? JoinByRequest { get; set; }
        public string? Description { get; set; }
        public string? InviteLink { get; set; }
        public TelegramMessage? PinnedMessage { get; set; }
        public TelegramChatPermissions? Permissions { get; set; }
        public int? SlowModeDelay { get; set; }
        public int? MessageAutoDeleteTime { get; set; }
        public bool? HasAggressiveAntiSpamEnabled { get; set; }
        public bool? HasHiddenMembers { get; set; }
        public bool? HasProtectedContent { get; set; }
        public string? StickerSetName { get; set; }
        public bool? CanSetStickerSet { get; set; }
        public long? LinkedChatId { get; set; }
        public TelegramChatLocation? Location { get; set; }
    }

    public class TelegramMessage
    {
        public int MessageId { get; set; }
        public TelegramUser? From { get; set; }
        public TelegramChat? SenderChat { get; set; }
        public DateTime Date { get; set; }
        public TelegramChat Chat { get; set; } = new();
        public TelegramUser? ForwardFrom { get; set; }
        public TelegramChat? ForwardFromChat { get; set; }
        public int? ForwardFromMessageId { get; set; }
        public string? ForwardSignature { get; set; }
        public string? ForwardSenderName { get; set; }
        public DateTime? ForwardDate { get; set; }
        public bool IsTopicMessage { get; set; }
        public bool IsAutomaticForward { get; set; }
        public TelegramMessage? ReplyToMessage { get; set; }
        public TelegramUser? ViaBot { get; set; }
        public DateTime? EditDate { get; set; }
        public bool HasProtectedContent { get; set; }
        public string? MediaGroupId { get; set; }
        public string? AuthorSignature { get; set; }
        public string? Text { get; set; }
        public List<TelegramMessageEntity> Entities { get; set; } = new();
        public TelegramAnimation? Animation { get; set; }
        public TelegramAudio? Audio { get; set; }
        public TelegramDocument? Document { get; set; }
        public List<TelegramPhotoSize> Photo { get; set; } = new();
        public TelegramSticker? Sticker { get; set; }
        public TelegramVideo? Video { get; set; }
        public TelegramVideoNote? VideoNote { get; set; }
        public TelegramVoice? Voice { get; set; }
        public string? Caption { get; set; }
        public List<TelegramMessageEntity> CaptionEntities { get; set; } = new();
        public bool HasMediaSpoiler { get; set; }
        public TelegramContact? Contact { get; set; }
        public TelegramDice? Dice { get; set; }
        public TelegramGame? Game { get; set; }
        public TelegramPoll? Poll { get; set; }
        public TelegramVenue? Venue { get; set; }
        public TelegramLocation? Location { get; set; }
        public List<TelegramUser> NewChatMembers { get; set; } = new();
        public TelegramUser? LeftChatMember { get; set; }
        public string? NewChatTitle { get; set; }
        public List<TelegramPhotoSize> NewChatPhoto { get; set; } = new();
        public bool DeleteChatPhoto { get; set; }
        public bool GroupChatCreated { get; set; }
        public bool SupergroupChatCreated { get; set; }
        public bool ChannelChatCreated { get; set; }
        public TelegramMessageAutoDeleteTimerChanged? MessageAutoDeleteTimerChanged { get; set; }
        public long? MigrateToChatId { get; set; }
        public long? MigrateFromChatId { get; set; }
        public TelegramMessage? PinnedMessage { get; set; }
        public TelegramInvoice? Invoice { get; set; }
        public TelegramSuccessfulPayment? SuccessfulPayment { get; set; }
        public string? ConnectedWebsite { get; set; }
        public TelegramWriteAccessAllowed? WriteAccessAllowed { get; set; }
        public TelegramPassportData? PassportData { get; set; }
        public TelegramProximityAlertTriggered? ProximityAlertTriggered { get; set; }
        public TelegramForumTopicCreated? ForumTopicCreated { get; set; }
        public TelegramForumTopicEdited? ForumTopicEdited { get; set; }
        public TelegramForumTopicClosed? ForumTopicClosed { get; set; }
        public TelegramForumTopicReopened? ForumTopicReopened { get; set; }
        public TelegramGeneralForumTopicHidden? GeneralForumTopicHidden { get; set; }
        public TelegramGeneralForumTopicUnhidden? GeneralForumTopicUnhidden { get; set; }
        public TelegramVideoChatScheduled? VideoChatScheduled { get; set; }
        public TelegramVideoChatStarted? VideoChatStarted { get; set; }
        public TelegramVideoChatEnded? VideoChatEnded { get; set; }
        public TelegramVideoChatParticipantsInvited? VideoChatParticipantsInvited { get; set; }
        public TelegramWebAppData? WebAppData { get; set; }
        public TelegramInlineKeyboardMarkup? ReplyMarkup { get; set; }
    }

    public class TelegramFile
    {
        public string FileId { get; set; } = string.Empty;
        public string FileUniqueId { get; set; } = string.Empty;
        public long? FileSize { get; set; }
        public string? FilePath { get; set; }
    }

    public class TelegramUserProfilePhotos
    {
        public int TotalCount { get; set; }
        public List<List<TelegramPhotoSize>> Photos { get; set; } = new();
    }

    public class TelegramChatMember
    {
        public TelegramUser User { get; set; } = new();
        public string Status { get; set; } = string.Empty;
        public string? CustomTitle { get; set; }
        public bool? IsAnonymous { get; set; }
        public bool? CanBeEdited { get; set; }
        public bool? CanManageChat { get; set; }
        public bool? CanDeleteMessages { get; set; }
        public bool? CanManageVideoChats { get; set; }
        public bool? CanRestrictMembers { get; set; }
        public bool? CanPromoteMembers { get; set; }
        public bool? CanChangeInfo { get; set; }
        public bool? CanInviteUsers { get; set; }
        public bool? CanPostMessages { get; set; }
        public bool? CanEditMessages { get; set; }
        public bool? CanPinMessages { get; set; }
        public bool? CanManageTopics { get; set; }
        public bool? IsMember { get; set; }
        public bool? CanSendMessages { get; set; }
        public bool? CanSendMediaMessages { get; set; }
        public bool? CanSendPolls { get; set; }
        public bool? CanSendOtherMessages { get; set; }
        public bool? CanAddWebPagePreviews { get; set; }
        public DateTime? UntilDate { get; set; }
        public bool? CanSendAudios { get; set; }
        public bool? CanSendDocuments { get; set; }
        public bool? CanSendPhotos { get; set; }
        public bool? CanSendVideos { get; set; }
        public bool? CanSendVoiceNotes { get; set; }
    }

    public class TelegramWebhookInfo
    {
        public string Url { get; set; } = string.Empty;
        public bool HasCustomCertificate { get; set; }
        public int PendingUpdateCount { get; set; }
        public string? IpAddress { get; set; }
        public DateTime? LastErrorDate { get; set; }
        public string? LastErrorMessage { get; set; }
        public DateTime? LastSynchronizationErrorDate { get; set; }
        public int MaxConnections { get; set; }
        public List<string> AllowedUpdates { get; set; } = new();
    }

    public class TelegramBotCommand
    {
        public string Command { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    // Supporting classes
    public class TelegramChatPhoto { public string SmallFileId { get; set; } = string.Empty; public string SmallFileUniqueId { get; set; } = string.Empty; public string BigFileId { get; set; } = string.Empty; public string BigFileUniqueId { get; set; } = string.Empty; }
    public class TelegramChatPermissions { public bool? CanSendMessages { get; set; } public bool? CanSendMediaMessages { get; set; } public bool? CanSendPolls { get; set; } public bool? CanSendOtherMessages { get; set; } public bool? CanAddWebPagePreviews { get; set; } public bool? CanChangeInfo { get; set; } public bool? CanInviteUsers { get; set; } public bool? CanPinMessages { get; set; } public bool? CanManageTopics { get; set; } }
    public class TelegramChatLocation { public TelegramLocation? Location { get; set; } public string? Address { get; set; } }
    public class TelegramInlineQuery { public string Id { get; set; } = string.Empty; public TelegramUser From { get; set; } = new(); public TelegramLocation? Location { get; set; } public string Query { get; set; } = string.Empty; public string Offset { get; set; } = string.Empty; public string? ChatType { get; set; } }
    public class TelegramChosenInlineResult { public string ResultId { get; set; } = string.Empty; public TelegramUser From { get; set; } = new(); public TelegramLocation? Location { get; set; } public string? InlineMessageId { get; set; } public string Query { get; set; } = string.Empty; }
    public class TelegramCallbackQuery { public string Id { get; set; } = string.Empty; public TelegramUser From { get; set; } = new(); public TelegramMessage? Message { get; set; } public string? InlineMessageId { get; set; } public string ChatInstance { get; set; } = string.Empty; public string? Data { get; set; } public string? GameShortName { get; set; } }
    public class TelegramShippingQuery { public string Id { get; set; } = string.Empty; public TelegramUser From { get; set; } = new(); public string InvoicePayload { get; set; } = string.Empty; public TelegramShippingAddress? ShippingAddress { get; set; } }
    public class TelegramPreCheckoutQuery { public string Id { get; set; } = string.Empty; public TelegramUser From { get; set; } = new(); public string Currency { get; set; } = string.Empty; public int TotalAmount { get; set; } public string InvoicePayload { get; set; } = string.Empty; public string? ShippingOptionId { get; set; } public TelegramOrderInfo? OrderInfo { get; set; } }
    public class TelegramPoll { public string Id { get; set; } public string Question { get; set; } public List<TelegramPollOption> Options { get; set; } public int TotalVoterCount { get; set; } public bool IsClosed { get; set; } public bool IsAnonymous { get; set; } public string Type { get; set; } public bool AllowsMultipleAnswers { get; set; } public int? CorrectOptionId { get; set; } public string Explanation { get; set; } public List<TelegramMessageEntity> ExplanationEntities { get; set; } public int? OpenPeriod { get; set; } public DateTime? CloseDate { get; set; } }
    public class TelegramPollAnswer { public string PollId { get; set; } public TelegramUser User { get; set; } public List<int> OptionIds { get; set; } }
    public class TelegramChatMemberUpdated { public TelegramChat Chat { get; set; } public TelegramUser From { get; set; } public DateTime Date { get; set; } public TelegramChatMember OldChatMember { get; set; } public TelegramChatMember NewChatMember { get; set; } public TelegramChatInviteLink InviteLink { get; set; } }
    public class TelegramChatJoinRequest { public TelegramChat Chat { get; set; } public TelegramUser From { get; set; } public DateTime Date { get; set; } public string Bio { get; set; } public TelegramChatInviteLink InviteLink { get; set; } }
    public class TelegramMessageEntity { public string Type { get; set; } public int Offset { get; set; } public int Length { get; set; } public string Url { get; set; } public TelegramUser User { get; set; } public string Language { get; set; } public string CustomEmojiId { get; set; } }
    public class TelegramAnimation { public string FileId { get; set; } public string FileUniqueId { get; set; } public int Width { get; set; } public int Height { get; set; } public int Duration { get; set; } public TelegramPhotoSize Thumbnail { get; set; } public string FileName { get; set; } public string MimeType { get; set; } public long? FileSize { get; set; } }
    public class TelegramAudio { public string FileId { get; set; } public string FileUniqueId { get; set; } public int Duration { get; set; } public string Performer { get; set; } public string Title { get; set; } public string FileName { get; set; } public string MimeType { get; set; } public long? FileSize { get; set; } public TelegramPhotoSize Thumbnail { get; set; } }
    public class TelegramDocument { public string FileId { get; set; } public string FileUniqueId { get; set; } public TelegramPhotoSize Thumbnail { get; set; } public string FileName { get; set; } public string MimeType { get; set; } public long? FileSize { get; set; } }
    public class TelegramPhotoSize { public string FileId { get; set; } public string FileUniqueId { get; set; } public int Width { get; set; } public int Height { get; set; } public long? FileSize { get; set; } }
    public class TelegramSticker { public string FileId { get; set; } public string FileUniqueId { get; set; } public string Type { get; set; } public int Width { get; set; } public int Height { get; set; } public bool IsAnimated { get; set; } public bool IsVideo { get; set; } public TelegramPhotoSize Thumbnail { get; set; } public string Emoji { get; set; } public string SetName { get; set; } public TelegramFile PremiumAnimation { get; set; } public TelegramMaskPosition MaskPosition { get; set; } public string CustomEmojiId { get; set; } public bool? NeedsRepainting { get; set; } public long? FileSize { get; set; } }
    public class TelegramVideo { public string FileId { get; set; } public string FileUniqueId { get; set; } public int Width { get; set; } public int Height { get; set; } public int Duration { get; set; } public TelegramPhotoSize Thumbnail { get; set; } public string FileName { get; set; } public string MimeType { get; set; } public long? FileSize { get; set; } }
    public class TelegramVideoNote { public string FileId { get; set; } public string FileUniqueId { get; set; } public int Length { get; set; } public int Duration { get; set; } public TelegramPhotoSize Thumbnail { get; set; } public long? FileSize { get; set; } }
    public class TelegramVoice { public string FileId { get; set; } public string FileUniqueId { get; set; } public int Duration { get; set; } public string MimeType { get; set; } public long? FileSize { get; set; } }
    public class TelegramContact { public string PhoneNumber { get; set; } public string FirstName { get; set; } public string LastName { get; set; } public string UserId { get; set; } public string Vcard { get; set; } }
    public class TelegramDice { public string Emoji { get; set; } public int Value { get; set; } }
    public class TelegramGame { public string Title { get; set; } public string Description { get; set; } public List<TelegramPhotoSize> Photo { get; set; } public string Text { get; set; } public List<TelegramMessageEntity> TextEntities { get; set; } public TelegramAnimation Animation { get; set; } }
    public class TelegramVenue { public TelegramLocation Location { get; set; } public string Title { get; set; } public string Address { get; set; } public string FoursquareId { get; set; } public string FoursquareType { get; set; } public string GooglePlaceId { get; set; } public string GooglePlaceType { get; set; } }
    public class TelegramLocation { public double Latitude { get; set; } public double Longitude { get; set; } public double? HorizontalAccuracy { get; set; } public int? LivePeriod { get; set; } public int? Heading { get; set; } public int? ProximityAlertRadius { get; set; } }
    public class TelegramMessageAutoDeleteTimerChanged { public int MessageAutoDeleteTime { get; set; } }
    public class TelegramInvoice { public string Title { get; set; } public string Description { get; set; } public string StartParameter { get; set; } public string Currency { get; set; } public int TotalAmount { get; set; } }
    public class TelegramSuccessfulPayment { public string Currency { get; set; } public int TotalAmount { get; set; } public string InvoicePayload { get; set; } public string ShippingOptionId { get; set; } public TelegramOrderInfo OrderInfo { get; set; } public string TelegramPaymentChargeId { get; set; } public string ProviderPaymentChargeId { get; set; } }
    public class TelegramWriteAccessAllowed { public bool? WebAppName { get; set; } }
    public class TelegramPassportData { }
    public class TelegramProximityAlertTriggered { public TelegramUser Traveler { get; set; } public TelegramUser Watcher { get; set; } public int Distance { get; set; } }
    public class TelegramForumTopicCreated { public string Name { get; set; } public string IconColor { get; set; } public string IconCustomEmojiId { get; set; } }
    public class TelegramForumTopicEdited { public string Name { get; set; } public string IconCustomEmojiId { get; set; } }
    public class TelegramForumTopicClosed { }
    public class TelegramForumTopicReopened { }
    public class TelegramGeneralForumTopicHidden { }
    public class TelegramGeneralForumTopicUnhidden { }
    public class TelegramVideoChatScheduled { public DateTime StartDate { get; set; } }
    public class TelegramVideoChatStarted { }
    public class TelegramVideoChatEnded { public int Duration { get; set; } }
    public class TelegramVideoChatParticipantsInvited { public List<TelegramUser> Users { get; set; } }
    public class TelegramWebAppData { public string Data { get; set; } public string ButtonText { get; set; } }
    public class TelegramInlineKeyboardMarkup { public List<List<TelegramInlineKeyboardButton>> InlineKeyboard { get; set; } }
    public class TelegramInlineKeyboardButton { public string Text { get; set; } public string Url { get; set; } public string CallbackData { get; set; } public string WebApp { get; set; } public string LoginUrl { get; set; } public string SwitchInlineQuery { get; set; } public string SwitchInlineQueryCurrentChat { get; set; } public TelegramCallbackGame CallbackGame { get; set; } public bool? Pay { get; set; } }
    public class TelegramCallbackGame { }
    public class TelegramShippingAddress { public string CountryCode { get; set; } public string State { get; set; } public string City { get; set; } public string StreetLine1 { get; set; } public string StreetLine2 { get; set; } public string PostCode { get; set; } }
    public class TelegramOrderInfo { public string Name { get; set; } public string PhoneNumber { get; set; } public string Email { get; set; } public TelegramShippingAddress ShippingAddress { get; set; } }
    public class TelegramPollOption { public string Text { get; set; } public int VoterCount { get; set; } }
    public class TelegramChatInviteLink { public string InviteLink { get; set; } public TelegramUser Creator { get; set; } public bool CreatesJoinRequest { get; set; } public bool IsPrimary { get; set; } public bool IsRevoked { get; set; } public string Name { get; set; } public DateTime? ExpireDate { get; set; } public int? MemberLimit { get; set; } public int? PendingJoinRequestCount { get; set; } }
    public class TelegramMaskPosition { public string Point { get; set; } public double XShift { get; set; } public double YShift { get; set; } public double Scale { get; set; } }
}