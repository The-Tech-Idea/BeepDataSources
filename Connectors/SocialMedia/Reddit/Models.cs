// File: Connectors/SocialMedia/Reddit/Models.cs
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Connectors.Reddit.Models
{
    // -------------------------------------------------------
    // Base Entity
    // -------------------------------------------------------
    public abstract class RedditEntityBase
    {
        [JsonIgnore] public IDataSource DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : RedditEntityBase { DataSource = ds; return (T)this; }
    }

    // -------------------------------------------------------
    // Post/Submission
    // -------------------------------------------------------
    public class RedditPost : RedditEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("selftext")] public string SelfText { get; set; }
        [JsonPropertyName("url")] public string Url { get; set; }
        [JsonPropertyName("subreddit")] public string Subreddit { get; set; }
        [JsonPropertyName("subreddit_id")] public string SubredditId { get; set; }
        [JsonPropertyName("author")] public string Author { get; set; }
        [JsonPropertyName("author_fullname")] public string AuthorFullname { get; set; }
        [JsonPropertyName("score")] public int? Score { get; set; }
        [JsonPropertyName("upvote_ratio")] public double? UpvoteRatio { get; set; }
        [JsonPropertyName("num_comments")] public int? NumComments { get; set; }
        [JsonPropertyName("created_utc")] public double? CreatedUtc { get; set; }
        [JsonPropertyName("created")] public double? Created { get; set; }
        [JsonPropertyName("is_self")] public bool? IsSelf { get; set; }
        [JsonPropertyName("over_18")] public bool? Over18 { get; set; }
        [JsonPropertyName("stickied")] public bool? Stickied { get; set; }
        [JsonPropertyName("locked")] public bool? Locked { get; set; }
        [JsonPropertyName("archived")] public bool? Archived { get; set; }
        [JsonPropertyName("spoiler")] public bool? Spoiler { get; set; }
        [JsonPropertyName("pinned")] public bool? Pinned { get; set; }
        [JsonPropertyName("is_video")] public bool? IsVideo { get; set; }
        [JsonPropertyName("is_original_content")] public bool? IsOriginalContent { get; set; }
        [JsonPropertyName("link_flair_text")] public string LinkFlairText { get; set; }
        [JsonPropertyName("link_flair_type")] public string LinkFlairType { get; set; }
        [JsonPropertyName("thumbnail")] public string Thumbnail { get; set; }
        [JsonPropertyName("thumbnail_width")] public int? ThumbnailWidth { get; set; }
        [JsonPropertyName("thumbnail_height")] public int? ThumbnailHeight { get; set; }
        [JsonPropertyName("preview")] public RedditPreview Preview { get; set; }
        [JsonPropertyName("media")] public RedditMedia Media { get; set; }
        [JsonPropertyName("secure_media")] public RedditMedia SecureMedia { get; set; }
        [JsonPropertyName("media_embed")] public RedditMediaEmbed MediaEmbed { get; set; }
        [JsonPropertyName("permalink")] public string Permalink { get; set; }
        [JsonPropertyName("domain")] public string Domain { get; set; }
        [JsonPropertyName("post_hint")] public string PostHint { get; set; }
        [JsonPropertyName("crosspost_parent_list")] public List<RedditPost> CrosspostParentList { get; set; } = new();
        [JsonPropertyName("edited")] public object Edited { get; set; } // Can be boolean or timestamp
        [JsonPropertyName("gilded")] public int? Gilded { get; set; }
        [JsonPropertyName("can_gild")] public bool? CanGild { get; set; }
        [JsonPropertyName("saved")] public bool? Saved { get; set; }
        [JsonPropertyName("hidden")] public bool? Hidden { get; set; }
        [JsonPropertyName("quarantine")] public bool? Quarantine { get; set; }
        [JsonPropertyName("visited")] public bool? Visited { get; set; }
        [JsonPropertyName("is_reddit_media_domain")] public bool? IsRedditMediaDomain { get; set; }
        [JsonPropertyName("is_meta")] public bool? IsMeta { get; set; }
        [JsonPropertyName("distinguished")] public string Distinguished { get; set; }
        [JsonPropertyName("removal_reason")] public string RemovalReason { get; set; }
        [JsonPropertyName("approved_at_utc")] public double? ApprovedAtUtc { get; set; }
        [JsonPropertyName("banned_at_utc")] public double? BannedAtUtc { get; set; }
        [JsonPropertyName("mod_reason_title")] public string ModReasonTitle { get; set; }
        [JsonPropertyName("mod_reason_by")] public string ModReasonBy { get; set; }
        [JsonPropertyName("num_reports")] public int? NumReports { get; set; }
        [JsonPropertyName("report_reasons")] public List<string> ReportReasons { get; set; } = new();
        [JsonPropertyName("author_patreon_flair")] public bool? AuthorPatreonFlair { get; set; }
        [JsonPropertyName("author_flair_text")] public string AuthorFlairText { get; set; }
        [JsonPropertyName("author_flair_type")] public string AuthorFlairType { get; set; }
        [JsonPropertyName("author_flair_background_color")] public string AuthorFlairBackgroundColor { get; set; }
        [JsonPropertyName("author_flair_text_color")] public string AuthorFlairTextColor { get; set; }
        [JsonPropertyName("link_flair_background_color")] public string LinkFlairBackgroundColor { get; set; }
        [JsonPropertyName("link_flair_text_color")] public string LinkFlairTextColor { get; set; }
        [JsonPropertyName("all_awardings")] public List<RedditAward> AllAwardings { get; set; } = new();
        [JsonPropertyName("total_awards_received")] public int? TotalAwardsReceived { get; set; }
        [JsonPropertyName("treatment_tags")] public List<string> TreatmentTags { get; set; } = new();
        [JsonPropertyName("top_awarded_type")] public string TopAwardedType { get; set; }
        [JsonPropertyName("allow_live_comments")] public bool? AllowLiveComments { get; set; }
        [JsonPropertyName("contest_mode")] public bool? ContestMode { get; set; }
        [JsonPropertyName("is_crosspostable")] public bool? IsCrosspostable { get; set; }
        [JsonPropertyName("is_robot_indexable")] public bool? IsRobotIndexable { get; set; }
        [JsonPropertyName("send_replies")] public bool? SendReplies { get; set; }
        [JsonPropertyName("whitelist_status")] public string WhitelistStatus { get; set; }
        [JsonPropertyName("parent_whitelist_status")] public string ParentWhitelistStatus { get; set; }
        [JsonPropertyName("wls")] public int? Wls { get; set; }
        [JsonPropertyName("subreddit_name_prefixed")] public string SubredditNamePrefixed { get; set; }
        [JsonPropertyName("subreddit_subscribers")] public int? SubredditSubscribers { get; set; }
        [JsonPropertyName("subreddit_type")] public string SubredditType { get; set; }
        [JsonPropertyName("suggested_sort")] public string SuggestedSort { get; set; }
        [JsonPropertyName("no_follow")] public bool? NoFollow { get; set; }
        [JsonPropertyName("num_crossposts")] public int? NumCrossposts { get; set; }
        [JsonPropertyName("media_only")] public bool? MediaOnly { get; set; }
        [JsonPropertyName("can_mod_post")] public bool? CanModPost { get; set; }
        [JsonPropertyName("category")] public string Category { get; set; }
        [JsonPropertyName("content_categories")] public List<string> ContentCategories { get; set; } = new();
        [JsonPropertyName("discussion_type")] public string DiscussionType { get; set; }
        [JsonPropertyName("event_end")] public double? EventEnd { get; set; }
        [JsonPropertyName("event_is_live")] public bool? EventIsLive { get; set; }
        [JsonPropertyName("event_start")] public double? EventStart { get; set; }
        [JsonPropertyName("gallery_data")] public RedditGalleryData GalleryData { get; set; }
        [JsonPropertyName("is_gallery")] public bool? IsGallery { get; set; }
        [JsonPropertyName("media_metadata")] public Dictionary<string, RedditMediaMetadata> MediaMetadata { get; set; } = new();
        [JsonPropertyName("poll_data")] public RedditPollData PollData { get; set; }
        [JsonPropertyName("rte_mode")] public string RteMode { get; set; }
        [JsonPropertyName("secure_media_embed")] public RedditMediaEmbed SecureMediaEmbed { get; set; }
        [JsonPropertyName("selftext_html")] public string SelftextHtml { get; set; }
        [JsonPropertyName("subreddit_id36")] public string SubredditId36 { get; set; }
        [JsonPropertyName("third_party_trackers")] public List<string> ThirdPartyTrackers { get; set; } = new();
        [JsonPropertyName("view_count")] public int? ViewCount { get; set; }
    }

    public sealed class RedditPreview
    {
        [JsonPropertyName("images")] public List<RedditPreviewImage> Images { get; set; } = new();
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
    }

    public sealed class RedditPreviewImage
    {
        [JsonPropertyName("source")] public RedditImageSource Source { get; set; }
        [JsonPropertyName("resolutions")] public List<RedditImageSource> Resolutions { get; set; } = new();
        [JsonPropertyName("variants")] public RedditImageVariants Variants { get; set; }
        [JsonPropertyName("id")] public string Id { get; set; }
    }

    public sealed class RedditImageSource
    {
        [JsonPropertyName("url")] public string Url { get; set; }
        [JsonPropertyName("width")] public int? Width { get; set; }
        [JsonPropertyName("height")] public int? Height { get; set; }
    }

    public sealed class RedditImageVariants
    {
        [JsonPropertyName("gif")] public RedditPreviewImage Gif { get; set; }
        [JsonPropertyName("mp4")] public RedditPreviewImage Mp4 { get; set; }
        [JsonPropertyName("nsfw")] public RedditPreviewImage Nsfw { get; set; }
        [JsonPropertyName("obfuscated")] public RedditPreviewImage Obfuscated { get; set; }
    }

    public sealed class RedditMedia
    {
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("oembed")] public RedditOembed Oembed { get; set; }
        [JsonPropertyName("reddit_video")] public RedditVideo RedditVideo { get; set; }
    }

    public sealed class RedditOembed
    {
        [JsonPropertyName("provider_url")] public string ProviderUrl { get; set; }
        [JsonPropertyName("version")] public string Version { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("thumbnail_width")] public int? ThumbnailWidth { get; set; }
        [JsonPropertyName("height")] public int? Height { get; set; }
        [JsonPropertyName("width")] public int? Width { get; set; }
        [JsonPropertyName("html")] public string Html { get; set; }
        [JsonPropertyName("author_name")] public string AuthorName { get; set; }
        [JsonPropertyName("provider_name")] public string ProviderName { get; set; }
        [JsonPropertyName("thumbnail_url")] public string ThumbnailUrl { get; set; }
        [JsonPropertyName("thumbnail_height")] public int? ThumbnailHeight { get; set; }
        [JsonPropertyName("author_url")] public string AuthorUrl { get; set; }
    }

    public sealed class RedditVideo
    {
        [JsonPropertyName("bitrate_kbps")] public int? BitrateKbps { get; set; }
        [JsonPropertyName("fallback_url")] public string FallbackUrl { get; set; }
        [JsonPropertyName("height")] public int? Height { get; set; }
        [JsonPropertyName("width")] public int? Width { get; set; }
        [JsonPropertyName("scrubber_media_url")] public string ScrubberMediaUrl { get; set; }
        [JsonPropertyName("dash_url")] public string DashUrl { get; set; }
        [JsonPropertyName("duration")] public int? Duration { get; set; }
        [JsonPropertyName("hls_url")] public string HlsUrl { get; set; }
        [JsonPropertyName("is_gif")] public bool? IsGif { get; set; }
        [JsonPropertyName("transcoding_status")] public string TranscodingStatus { get; set; }
    }

    public sealed class RedditMediaEmbed
    {
        [JsonPropertyName("content")] public string Content { get; set; }
        [JsonPropertyName("width")] public int? Width { get; set; }
        [JsonPropertyName("scrolling")] public bool? Scrolling { get; set; }
        [JsonPropertyName("height")] public int? Height { get; set; }
        [JsonPropertyName("media_domain_url")] public string MediaDomainUrl { get; set; }
    }

    public sealed class RedditAward
    {
        [JsonPropertyName("giver_coin_reward")] public int? GiverCoinReward { get; set; }
        [JsonPropertyName("subreddit_id")] public string SubredditId { get; set; }
        [JsonPropertyName("is_new")] public bool? IsNew { get; set; }
        [JsonPropertyName("days_of_drip_extension")] public int? DaysOfDripExtension { get; set; }
        [JsonPropertyName("coin_price")] public int? CoinPrice { get; set; }
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("penny_donate")] public int? PennyDonate { get; set; }
        [JsonPropertyName("award_sub_type")] public string AwardSubType { get; set; }
        [JsonPropertyName("coin_reward")] public int? CoinReward { get; set; }
        [JsonPropertyName("icon_url")] public string IconUrl { get; set; }
        [JsonPropertyName("days_of_premium")] public int? DaysOfPremium { get; set; }
        [JsonPropertyName("tiers_by_required_awardings")] public object TiersByRequiredAwardings { get; set; }
        [JsonPropertyName("resized_icons")] public List<RedditImageSource> ResizedIcons { get; set; } = new();
        [JsonPropertyName("icon_width")] public int? IconWidth { get; set; }
        [JsonPropertyName("static_icon_width")] public int? StaticIconWidth { get; set; }
        [JsonPropertyName("start_date")] public double? StartDate { get; set; }
        [JsonPropertyName("is_enabled")] public bool? IsEnabled { get; set; }
        [JsonPropertyName("awardings_required_to_grant_benefits")] public int? AwardingsRequiredToGrantBenefits { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("end_date")] public double? EndDate { get; set; }
        [JsonPropertyName("subreddit_coin_reward")] public int? SubredditCoinReward { get; set; }
        [JsonPropertyName("count")] public int? Count { get; set; }
        [JsonPropertyName("static_icon_height")] public int? StaticIconHeight { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("resized_static_icons")] public List<RedditImageSource> ResizedStaticIcons { get; set; } = new();
        [JsonPropertyName("icon_height")] public int? IconHeight { get; set; }
        [JsonPropertyName("static_icon_url")] public string StaticIconUrl { get; set; }
    }

    public sealed class RedditGalleryData
    {
        [JsonPropertyName("items")] public List<RedditGalleryItem> Items { get; set; } = new();
    }

    public sealed class RedditGalleryItem
    {
        [JsonPropertyName("media_id")] public string MediaId { get; set; }
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("caption")] public string Caption { get; set; }
        [JsonPropertyName("outbound_url")] public string OutboundUrl { get; set; }
    }

    public sealed class RedditMediaMetadata
    {
        [JsonPropertyName("status")] public string Status { get; set; }
        [JsonPropertyName("e")] public string E { get; set; }
        [JsonPropertyName("m")] public string M { get; set; }
        [JsonPropertyName("p")] public List<RedditImageSource> P { get; set; } = new();
        [JsonPropertyName("s")] public RedditImageSource S { get; set; }
        [JsonPropertyName("id")] public string Id { get; set; }
    }

    public sealed class RedditPollData
    {
        [JsonPropertyName("is_prediction")] public bool? IsPrediction { get; set; }
        [JsonPropertyName("options")] public List<RedditPollOption> Options { get; set; } = new();
        [JsonPropertyName("resolved_option_id")] public int? ResolvedOptionId { get; set; }
        [JsonPropertyName("total_stake_amount")] public int? TotalStakeAmount { get; set; }
        [JsonPropertyName("total_vote_count")] public int? TotalVoteCount { get; set; }
        [JsonPropertyName("user_selection")] public int? UserSelection { get; set; }
        [JsonPropertyName("user_won_amount")] public int? UserWonAmount { get; set; }
        [JsonPropertyName("vote_updates_remained")] public int? VoteUpdatesRemained { get; set; }
        [JsonPropertyName("voting_end_timestamp")] public double? VotingEndTimestamp { get; set; }
    }

    public sealed class RedditPollOption
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("text")] public string Text { get; set; }
        [JsonPropertyName("vote_count")] public int? VoteCount { get; set; }
    }

    // -------------------------------------------------------
    // Comment
    // -------------------------------------------------------
    public sealed class RedditComment : RedditEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("body")] public string Body { get; set; }
        [JsonPropertyName("body_html")] public string BodyHtml { get; set; }
        [JsonPropertyName("author")] public string Author { get; set; }
        [JsonPropertyName("author_fullname")] public string AuthorFullname { get; set; }
        [JsonPropertyName("subreddit")] public string Subreddit { get; set; }
        [JsonPropertyName("subreddit_id")] public string SubredditId { get; set; }
        [JsonPropertyName("link_id")] public string LinkId { get; set; }
        [JsonPropertyName("parent_id")] public string ParentId { get; set; }
        [JsonPropertyName("score")] public int? Score { get; set; }
        [JsonPropertyName("created_utc")] public double? CreatedUtc { get; set; }
        [JsonPropertyName("created")] public double? Created { get; set; }
        [JsonPropertyName("edited")] public object Edited { get; set; } // Can be boolean or timestamp
        [JsonPropertyName("is_submitter")] public bool? IsSubmitter { get; set; }
        [JsonPropertyName("stickied")] public bool? Stickied { get; set; }
        [JsonPropertyName("locked")] public bool? Locked { get; set; }
        [JsonPropertyName("collapsed")] public bool? Collapsed { get; set; }
        [JsonPropertyName("collapsed_reason")] public string CollapsedReason { get; set; }
        [JsonPropertyName("collapsed_reason_code")] public string CollapsedReasonCode { get; set; }
        [JsonPropertyName("replies")] public RedditCommentReplies Replies { get; set; }
        [JsonPropertyName("saved")] public bool? Saved { get; set; }
        [JsonPropertyName("gilded")] public int? Gilded { get; set; }
        [JsonPropertyName("can_gild")] public bool? CanGild { get; set; }
        [JsonPropertyName("controversiality")] public int? Controversiality { get; set; }
        [JsonPropertyName("depth")] public int? Depth { get; set; }
        [JsonPropertyName("is_submitter")] public bool? IsSubmitter2 { get; set; } // Duplicate in some responses
        [JsonPropertyName("no_follow")] public bool? NoFollow { get; set; }
        [JsonPropertyName("permalink")] public string Permalink { get; set; }
        [JsonPropertyName("score_hidden")] public bool? ScoreHidden { get; set; }
        [JsonPropertyName("send_replies")] public bool? SendReplies { get; set; }
        [JsonPropertyName("subreddit_name_prefixed")] public string SubredditNamePrefixed { get; set; }
        [JsonPropertyName("subreddit_type")] public string SubredditType { get; set; }
        [JsonPropertyName("total_awards_received")] public int? TotalAwardsReceived { get; set; }
        [JsonPropertyName("all_awardings")] public List<RedditAward> AllAwardings { get; set; } = new();
        [JsonPropertyName("associated_award")] public RedditAward AssociatedAward { get; set; }
        [JsonPropertyName("unrepliable_reason")] public string UnrepliableReason { get; set; }
        [JsonPropertyName("author_flair_background_color")] public string AuthorFlairBackgroundColor { get; set; }
        [JsonPropertyName("author_flair_css_class")] public string AuthorFlairCssClass { get; set; }
        [JsonPropertyName("author_flair_richtext")] public List<RedditFlairRichtext> AuthorFlairRichtext { get; set; } = new();
        [JsonPropertyName("author_flair_text")] public string AuthorFlairText { get; set; }
        [JsonPropertyName("author_flair_text_color")] public string AuthorFlairTextColor { get; set; }
        [JsonPropertyName("author_flair_type")] public string AuthorFlairType { get; set; }
        [JsonPropertyName("author_patreon_flair")] public bool? AuthorPatreonFlair { get; set; }
        [JsonPropertyName("author_premium")] public bool? AuthorPremium { get; set; }
        [JsonPropertyName("treatment_tags")] public List<string> TreatmentTags { get; set; } = new();
        [JsonPropertyName("distinguished")] public string Distinguished { get; set; }
        [JsonPropertyName("mod_note")] public string ModNote { get; set; }
        [JsonPropertyName("mod_reason_by")] public string ModReasonBy { get; set; }
        [JsonPropertyName("mod_reason_title")] public string ModReasonTitle { get; set; }
        [JsonPropertyName("num_reports")] public int? NumReports { get; set; }
        [JsonPropertyName("removal_reason")] public string RemovalReason { get; set; }
        [JsonPropertyName("report_reasons")] public List<string> ReportReasons { get; set; } = new();
        [JsonPropertyName("approved_at_utc")] public double? ApprovedAtUtc { get; set; }
        [JsonPropertyName("banned_at_utc")] public double? BannedAtUtc { get; set; }
        [JsonPropertyName("mod_reports")] public List<RedditModReport> ModReports { get; set; } = new();
        [JsonPropertyName("user_reports")] public List<RedditUserReport> UserReports { get; set; } = new();
        [JsonPropertyName("can_mod_post")] public bool? CanModPost { get; set; }
        [JsonPropertyName("top_awarded_type")] public string TopAwardedType { get; set; }
        [JsonPropertyName("ignored_replies_to_this")] public bool? IgnoredRepliesToThis { get; set; }
    }

    public sealed class RedditCommentReplies
    {
        [JsonPropertyName("kind")] public string Kind { get; set; }
        [JsonPropertyName("data")] public RedditCommentRepliesData Data { get; set; }
    }

    public sealed class RedditCommentRepliesData
    {
        [JsonPropertyName("after")] public string After { get; set; }
        [JsonPropertyName("before")] public string Before { get; set; }
        [JsonPropertyName("children")] public List<string> Children { get; set; } = new();
        [JsonPropertyName("dist")] public int? Dist { get; set; }
    }

    public sealed class RedditFlairRichtext
    {
        [JsonPropertyName("e")] public string E { get; set; }
        [JsonPropertyName("t")] public string T { get; set; }
        [JsonPropertyName("u")] public string U { get; set; }
        [JsonPropertyName("a")] public string A { get; set; }
    }

    public sealed class RedditModReport
    {
        [JsonPropertyName("report_reason")] public string ReportReason { get; set; }
        [JsonPropertyName("other_reason")] public string OtherReason { get; set; }
        [JsonPropertyName("rule_name")] public string RuleName { get; set; }
        [JsonPropertyName("rule_reason")] public string RuleReason { get; set; }
        [JsonPropertyName("mod_action")] public string ModAction { get; set; }
        [JsonPropertyName("created_utc")] public double? CreatedUtc { get; set; }
        [JsonPropertyName("id")] public string Id { get; set; }
    }

    public sealed class RedditUserReport
    {
        [JsonPropertyName("report_reason")] public string ReportReason { get; set; }
        [JsonPropertyName("other_reason")] public string OtherReason { get; set; }
        [JsonPropertyName("created_utc")] public double? CreatedUtc { get; set; }
        [JsonPropertyName("id")] public string Id { get; set; }
    }

    // -------------------------------------------------------
    // Subreddit
    // -------------------------------------------------------
    public sealed class RedditSubreddit : RedditEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("display_name")] public string Caption { get; set; }
        [JsonPropertyName("display_name_prefixed")] public string CaptionPrefixed { get; set; }
        [JsonPropertyName("subscribers")] public int? Subscribers { get; set; }
        [JsonPropertyName("active_user_count")] public int? ActiveUserCount { get; set; }
        [JsonPropertyName("accounts_active")] public int? AccountsActive { get; set; }
        [JsonPropertyName("accounts_active_is_fuzzed")] public bool? AccountsActiveIsFuzzed { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("description_html")] public string DescriptionHtml { get; set; }
        [JsonPropertyName("public_description")] public string PublicDescription { get; set; }
        [JsonPropertyName("public_description_html")] public string PublicDescriptionHtml { get; set; }
        [JsonPropertyName("created_utc")] public double? CreatedUtc { get; set; }
        [JsonPropertyName("created")] public double? Created { get; set; }
        [JsonPropertyName("over18")] public bool? Over18 { get; set; }
        [JsonPropertyName("spoilers_enabled")] public bool? SpoilersEnabled { get; set; }
        [JsonPropertyName("subreddit_type")] public string SubredditType { get; set; }
        [JsonPropertyName("submission_type")] public string SubmissionType { get; set; }
        [JsonPropertyName("suggested_comment_sort")] public string SuggestedCommentSort { get; set; }
        [JsonPropertyName("lang")] public string Lang { get; set; }
        [JsonPropertyName("url")] public string Url { get; set; }
        [JsonPropertyName("whitelist_status")] public string WhitelistStatus { get; set; }
        [JsonPropertyName("wls")] public int? Wls { get; set; }
        [JsonPropertyName("user_is_banned")] public bool? UserIsBanned { get; set; }
        [JsonPropertyName("user_is_muted")] public bool? UserIsMuted { get; set; }
        [JsonPropertyName("user_is_moderator")] public bool? UserIsModerator { get; set; }
        [JsonPropertyName("user_is_subscriber")] public bool? UserIsSubscriber { get; set; }
        [JsonPropertyName("user_is_contributor")] public bool? UserIsContributor { get; set; }
        [JsonPropertyName("user_has_favorited")] public bool? UserHasFavorited { get; set; }
        [JsonPropertyName("accept_followers")] public bool? AcceptFollowers { get; set; }
        [JsonPropertyName("allow_chat_post_creation")] public bool? AllowChatPostCreation { get; set; }
        [JsonPropertyName("allow_discovery")] public bool? AllowDiscovery { get; set; }
        [JsonPropertyName("allow_galleries")] public bool? AllowGalleries { get; set; }
        [JsonPropertyName("allow_images")] public bool? AllowImages { get; set; }
        [JsonPropertyName("allow_polls")] public bool? AllowPolls { get; set; }
        [JsonPropertyName("allow_prediction_contributors")] public bool? AllowPredictionContributors { get; set; }
        [JsonPropertyName("allow_predictions")] public bool? AllowPredictions { get; set; }
        [JsonPropertyName("allow_predictions_tournament")] public bool? AllowPredictionsTournament { get; set; }
        [JsonPropertyName("allow_talks")] public bool? AllowTalks { get; set; }
        [JsonPropertyName("allow_videogifs")] public bool? AllowVideogifs { get; set; }
        [JsonPropertyName("allow_videos")] public bool? AllowVideos { get; set; }
        [JsonPropertyName("banner_background_color")] public string BannerBackgroundColor { get; set; }
        [JsonPropertyName("banner_background_image")] public string BannerBackgroundImage { get; set; }
        [JsonPropertyName("banner_img")] public string BannerImg { get; set; }
        [JsonPropertyName("banner_size")] public List<int> BannerSize { get; set; } = new();
        [JsonPropertyName("can_assign_link_flair")] public bool? CanAssignLinkFlair { get; set; }
        [JsonPropertyName("can_assign_user_flair")] public bool? CanAssignUserFlair { get; set; }
        [JsonPropertyName("collapse_deleted_comments")] public bool? CollapseDeletedComments { get; set; }
        [JsonPropertyName("comment_score_hide_mins")] public int? CommentScoreHideMins { get; set; }
        [JsonPropertyName("community_icon")] public string CommunityIcon { get; set; }
        [JsonPropertyName("community_reviewed")] public bool? CommunityReviewed { get; set; }
        [JsonPropertyName("emojis_custom_size")] public List<int> EmojisCustomSize { get; set; } = new();
        [JsonPropertyName("emojis_enabled")] public bool? EmojisEnabled { get; set; }
        [JsonPropertyName("has_menu_widget")] public bool? HasMenuWidget { get; set; }
        [JsonPropertyName("header_img")] public string HeaderImg { get; set; }
        [JsonPropertyName("header_size")] public List<int> HeaderSize { get; set; } = new();
        [JsonPropertyName("header_title")] public string HeaderTitle { get; set; }
        [JsonPropertyName("hide_ads")] public bool? HideAds { get; set; }
        [JsonPropertyName("icon_img")] public string IconImg { get; set; }
        [JsonPropertyName("icon_size")] public List<int> IconSize { get; set; } = new();
        [JsonPropertyName("is_chat_post_feature_enabled")] public bool? IsChatPostFeatureEnabled { get; set; }
        [JsonPropertyName("is_crosspostable_subreddit")] public bool? IsCrosspostableSubreddit { get; set; }
        [JsonPropertyName("is_enrolled_in_new_modmail")] public bool? IsEnrolledInNewModmail { get; set; }
        [JsonPropertyName("key_color")] public string KeyColor { get; set; }
        [JsonPropertyName("link_flair_enabled")] public bool? LinkFlairEnabled { get; set; }
        [JsonPropertyName("link_flair_position")] public string LinkFlairPosition { get; set; }
        [JsonPropertyName("mobile_banner_image")] public string MobileBannerImage { get; set; }
        [JsonPropertyName("notification_level")] public string NotificationLevel { get; set; }
        [JsonPropertyName("original_content_tag_enabled")] public bool? OriginalContentTagEnabled { get; set; }
        [JsonPropertyName("over_18")] public bool? Over18Alt { get; set; } // Alternative field
        [JsonPropertyName("prediction_leaderboard_entry_type")] public string PredictionLeaderboardEntryType { get; set; }
        [JsonPropertyName("primary_color")] public string PrimaryColor { get; set; }
        [JsonPropertyName("restrict_commenting")] public bool? RestrictCommenting { get; set; }
        [JsonPropertyName("restrict_posting")] public bool? RestrictPosting { get; set; }
        [JsonPropertyName("should_archive_posts")] public bool? ShouldArchivePosts { get; set; }
        [JsonPropertyName("should_show_media_in_comments_setting")] public bool? ShouldShowMediaInCommentsSetting { get; set; }
        [JsonPropertyName("show_media")] public bool? ShowMedia { get; set; }
        [JsonPropertyName("show_media_preview")] public bool? ShowMediaPreview { get; set; }
        [JsonPropertyName("spoilers_enabled")] public bool? SpoilersEnabledAlt { get; set; } // Alternative field
        [JsonPropertyName("submit_link_label")] public string SubmitLinkLabel { get; set; }
        [JsonPropertyName("submit_text")] public string SubmitText { get; set; }
        [JsonPropertyName("submit_text_html")] public string SubmitTextHtml { get; set; }
        [JsonPropertyName("submit_text_label")] public string SubmitTextLabel { get; set; }
        [JsonPropertyName("subreddit_name_prefixed")] public string SubredditNamePrefixed { get; set; }
        [JsonPropertyName("suppress_join_button")] public bool? SuppressJoinButton { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("user_flair_background_color")] public string UserFlairBackgroundColor { get; set; }
        [JsonPropertyName("user_flair_css_class")] public string UserFlairCssClass { get; set; }
        [JsonPropertyName("user_flair_enabled_in_sr")] public bool? UserFlairEnabledInSr { get; set; }
        [JsonPropertyName("user_flair_position")] public string UserFlairPosition { get; set; }
        [JsonPropertyName("user_flair_richtext")] public List<RedditFlairRichtext> UserFlairRichtext { get; set; } = new();
        [JsonPropertyName("user_flair_text")] public string UserFlairText { get; set; }
        [JsonPropertyName("user_flair_text_color")] public string UserFlairTextColor { get; set; }
        [JsonPropertyName("user_flair_type")] public string UserFlairType { get; set; }
        [JsonPropertyName("user_has_favorited")] public bool? UserHasFavoritedAlt { get; set; } // Alternative field
        [JsonPropertyName("user_is_banned")] public bool? UserIsBannedAlt { get; set; } // Alternative field
        [JsonPropertyName("user_is_moderator")] public bool? UserIsModeratorAlt { get; set; } // Alternative field
        [JsonPropertyName("user_is_muted")] public bool? UserIsMutedAlt { get; set; } // Alternative field
        [JsonPropertyName("user_is_subscriber")] public bool? UserIsSubscriberAlt { get; set; } // Alternative field
        [JsonPropertyName("user_sr_flair_enabled")] public bool? UserSrFlairEnabled { get; set; }
        [JsonPropertyName("user_sr_theme_enabled")] public bool? UserSrThemeEnabled { get; set; }
        [JsonPropertyName("videostream_links_count")] public int? VideostreamLinksCount { get; set; }
        [JsonPropertyName("wiki_enabled")] public bool? WikiEnabled { get; set; }
        [JsonPropertyName("advertiser_category")] public string AdvertiserCategory { get; set; }
        [JsonPropertyName("all_original_content")] public bool? AllOriginalContent { get; set; }
        [JsonPropertyName("audience_targeting")] public string AudienceTargeting { get; set; }
        [JsonPropertyName("available_layouts")] public RedditAvailableLayouts AvailableLayouts { get; set; }
        [JsonPropertyName("brand_color")] public string BrandColor { get; set; }
        [JsonPropertyName("brand_safe")] public bool? BrandSafe { get; set; }
        [JsonPropertyName("chat_config")] public RedditChatConfig ChatConfig { get; set; }
        [JsonPropertyName("collections_enabled")] public bool? CollectionsEnabled { get; set; }
        [JsonPropertyName("content_category")] public string ContentCategory { get; set; }
        [JsonPropertyName("crosspostable")] public bool? Crosspostable { get; set; }
        [JsonPropertyName("disable_contributor_requests")] public bool? DisableContributorRequests { get; set; }
        [JsonPropertyName("event_posts_enabled")] public bool? EventPostsEnabled { get; set; }
        [JsonPropertyName("free_form_reports")] public bool? FreeFormReports { get; set; }
        [JsonPropertyName("has_subscribed")] public bool? HasSubscribed { get; set; }
        [JsonPropertyName("is_chat_post_feature_enabled")] public bool? IsChatPostFeatureEnabledAlt { get; set; } // Alternative field
        [JsonPropertyName("is_crosspostable_subreddit")] public bool? IsCrosspostableSubredditAlt { get; set; } // Alternative field
        [JsonPropertyName("is_enrolled_in_new_modmail")] public bool? IsEnrolledInNewModmailAlt { get; set; } // Alternative field
        [JsonPropertyName("link_flair_enabled")] public bool? LinkFlairEnabledAlt { get; set; } // Alternative field
        [JsonPropertyName("mobile_banner_image")] public string MobileBannerImageAlt { get; set; } // Alternative field
        [JsonPropertyName("notification_level")] public string NotificationLevelAlt { get; set; } // Alternative field
        [JsonPropertyName("original_content_tag_enabled")] public bool? OriginalContentTagEnabledAlt { get; set; } // Alternative field
        [JsonPropertyName("restrict_commenting")] public bool? RestrictCommentingAlt { get; set; } // Alternative field
        [JsonPropertyName("restrict_posting")] public bool? RestrictPostingAlt { get; set; } // Alternative field
        [JsonPropertyName("should_archive_posts")] public bool? ShouldArchivePostsAlt { get; set; } // Alternative field
        [JsonPropertyName("should_show_media_in_comments_setting")] public bool? ShouldShowMediaInCommentsSettingAlt { get; set; } // Alternative field
        [JsonPropertyName("show_media")] public bool? ShowMediaAlt { get; set; } // Alternative field
        [JsonPropertyName("show_media_preview")] public bool? ShowMediaPreviewAlt { get; set; } // Alternative field
        [JsonPropertyName("submit_link_label")] public string SubmitLinkLabelAlt { get; set; } // Alternative field
        [JsonPropertyName("submit_text")] public string SubmitTextAlt { get; set; } // Alternative field
        [JsonPropertyName("submit_text_html")] public string SubmitTextHtmlAlt { get; set; } // Alternative field
        [JsonPropertyName("submit_text_label")] public string SubmitTextLabelAlt { get; set; } // Alternative field
        [JsonPropertyName("subreddit_name_prefixed")] public string SubredditNamePrefixedAlt { get; set; } // Alternative field
        [JsonPropertyName("suppress_join_button")] public bool? SuppressJoinButtonAlt { get; set; } // Alternative field
        [JsonPropertyName("title")] public string TitleAlt { get; set; } // Alternative field
        [JsonPropertyName("user_flair_background_color")] public string UserFlairBackgroundColorAlt { get; set; } // Alternative field
        [JsonPropertyName("user_flair_css_class")] public string UserFlairCssClassAlt { get; set; } // Alternative field
        [JsonPropertyName("user_flair_enabled_in_sr")] public bool? UserFlairEnabledInSrAlt { get; set; } // Alternative field
        [JsonPropertyName("user_flair_position")] public string UserFlairPositionAlt { get; set; } // Alternative field
        [JsonPropertyName("user_flair_richtext")] public List<RedditFlairRichtext> UserFlairRichtextAlt { get; set; } = new(); // Alternative field
        [JsonPropertyName("user_flair_text")] public string UserFlairTextAlt { get; set; } // Alternative field
        [JsonPropertyName("user_flair_text_color")] public string UserFlairTextColorAlt { get; set; } // Alternative field
        [JsonPropertyName("user_flair_type")] public string UserFlairTypeAlt { get; set; } // Alternative field
        [JsonPropertyName("user_has_favorited")] public bool? UserHasFavoritedAlt2 { get; set; } // Alternative field
        [JsonPropertyName("user_is_banned")] public bool? UserIsBannedAlt2 { get; set; } // Alternative field
        [JsonPropertyName("user_is_moderator")] public bool? UserIsModeratorAlt2 { get; set; } // Alternative field
        [JsonPropertyName("user_is_muted")] public bool? UserIsMutedAlt2 { get; set; } // Alternative field
        [JsonPropertyName("user_is_subscriber")] public bool? UserIsSubscriberAlt2 { get; set; } // Alternative field
        [JsonPropertyName("user_sr_flair_enabled")] public bool? UserSrFlairEnabledAlt { get; set; } // Alternative field
        [JsonPropertyName("user_sr_theme_enabled")] public bool? UserSrThemeEnabledAlt { get; set; } // Alternative field
        [JsonPropertyName("videostream_links_count")] public int? VideostreamLinksCountAlt { get; set; } // Alternative field
        [JsonPropertyName("wiki_enabled")] public bool? WikiEnabledAlt { get; set; } // Alternative field
    }

    public sealed class RedditAvailableLayouts
    {
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
    }

    public sealed class RedditChatConfig
    {
        [JsonPropertyName("should_display_chat")] public bool? ShouldDisplayChat { get; set; }
        [JsonPropertyName("should_turn_on_chat")] public bool? ShouldTurnOnChat { get; set; }
    }

    // -------------------------------------------------------
    // User
    // -------------------------------------------------------
    public sealed class RedditUser : RedditEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("date")] public double? Date { get; set; }
        [JsonPropertyName("comment_karma")] public int? CommentKarma { get; set; }
        [JsonPropertyName("link_karma")] public int? LinkKarma { get; set; }
        [JsonPropertyName("is_gold")] public bool? IsGold { get; set; }
        [JsonPropertyName("is_mod")] public bool? IsMod { get; set; }
        [JsonPropertyName("is_employee")] public bool? IsEmployee { get; set; }
        [JsonPropertyName("has_verified_email")] public bool? HasVerifiedEmail { get; set; }
        [JsonPropertyName("created_utc")] public double? CreatedUtc { get; set; }
        [JsonPropertyName("snoovatar_img")] public string SnoovatarImg { get; set; }
        [JsonPropertyName("snoovatar_size")] public List<int> SnoovatarSize { get; set; } = new();
        [JsonPropertyName("icon_img")] public string IconImg { get; set; }
        [JsonPropertyName("pref_show_snoovatar")] public bool? PrefShowSnoovatar { get; set; }
        [JsonPropertyName("is_sponsor")] public bool? IsSponsor { get; set; }
        [JsonPropertyName("is_blocked")] public bool? IsBlocked { get; set; }
        [JsonPropertyName("has_subscribed")] public bool? HasSubscribed { get; set; }
        [JsonPropertyName("subreddit")] public RedditUserSubreddit Subreddit { get; set; }
        [JsonPropertyName("accept_chats")] public bool? AcceptChats { get; set; }
        [JsonPropertyName("accept_pms")] public bool? AcceptPms { get; set; }
        [JsonPropertyName("accept_followers")] public bool? AcceptFollowers { get; set; }
        [JsonPropertyName("awardee_karma")] public int? AwardeeKarma { get; set; }
        [JsonPropertyName("awarder_karma")] public int? AwarderKarma { get; set; }
        [JsonPropertyName("can_create_subreddit")] public bool? CanCreateSubreddit { get; set; }
        [JsonPropertyName("can_edit_name")] public bool? CanEditName { get; set; }
        [JsonPropertyName("coins")] public int? Coins { get; set; }
        [JsonPropertyName("force_password_reset")] public bool? ForcePasswordReset { get; set; }
        [JsonPropertyName("gold_creddits")] public int? GoldCreddits { get; set; }
        [JsonPropertyName("gold_expiration")] public double? GoldExpiration { get; set; }
        [JsonPropertyName("has_android_subscription")] public bool? HasAndroidSubscription { get; set; }
        [JsonPropertyName("has_external_account")] public bool? HasExternalAccount { get; set; }
        [JsonPropertyName("has_ios_subscription")] public bool? HasIosSubscription { get; set; }
        [JsonPropertyName("has_paypal_subscription")] public bool? HasPaypalSubscription { get; set; }
        [JsonPropertyName("has_stripe_subscription")] public bool? HasStripeSubscription { get; set; }
        [JsonPropertyName("has_subscribed_to_premium")] public bool? HasSubscribedToPremium { get; set; }
        [JsonPropertyName("has_verified_email")] public bool? HasVerifiedEmailAlt { get; set; } // Alternative field
        [JsonPropertyName("has_visited_new_profile")] public bool? HasVisitedNewProfile { get; set; }
        [JsonPropertyName("hide_from_robots")] public bool? HideFromRobots { get; set; }
        [JsonPropertyName("icon_size")] public List<int> IconSize { get; set; } = new();
        [JsonPropertyName("id")] public string IdAlt { get; set; } // Alternative field
        [JsonPropertyName("in_beta")] public bool? InBeta { get; set; }
        [JsonPropertyName("in_chat")] public bool? InChat { get; set; }
        [JsonPropertyName("in_redesign_beta")] public bool? InRedesignBeta { get; set; }
        [JsonPropertyName("inbox_count")] public int? InboxCount { get; set; }
        [JsonPropertyName("is_employee")] public bool? IsEmployeeAlt { get; set; } // Alternative field
        [JsonPropertyName("is_gold")] public bool? IsGoldAlt { get; set; } // Alternative field
        [JsonPropertyName("is_mod")] public bool? IsModAlt { get; set; } // Alternative field
        [JsonPropertyName("is_sponsor")] public bool? IsSponsorAlt { get; set; } // Alternative field
        [JsonPropertyName("is_suspended")] public bool? IsSuspended { get; set; }
        [JsonPropertyName("linked_identities")] public List<string> LinkedIdentities { get; set; } = new();
        [JsonPropertyName("modhash")] public string Modhash { get; set; }
        [JsonPropertyName("new_modmail_exists")] public bool? NewModmailExists { get; set; }
        [JsonPropertyName("num_friends")] public int? NumFriends { get; set; }
        [JsonPropertyName("oauth_client_id")] public string OauthClientId { get; set; }
        [JsonPropertyName("over_18")] public bool? Over18 { get; set; }
        [JsonPropertyName("password_set")] public bool? PasswordSet { get; set; }
        [JsonPropertyName("pref_autoplay")] public bool? PrefAutoplay { get; set; }
        [JsonPropertyName("pref_clickgadget")] public int? PrefClickgadget { get; set; }
        [JsonPropertyName("pref_geopopular")] public string PrefGeopopular { get; set; }
        [JsonPropertyName("pref_nightmode")] public bool? PrefNightmode { get; set; }
        [JsonPropertyName("pref_no_profanity")] public bool? PrefNoProfanity { get; set; }
        [JsonPropertyName("pref_show_snoovatar")] public bool? PrefShowSnoovatarAlt { get; set; } // Alternative field
        [JsonPropertyName("pref_show_trending")] public bool? PrefShowTrending { get; set; }
        [JsonPropertyName("pref_show_twitter")] public bool? PrefShowTwitter { get; set; }
        [JsonPropertyName("pref_top_karma_subreddits")] public bool? PrefTopKarmaSubreddits { get; set; }
        [JsonPropertyName("pref_video_autoplay")] public bool? PrefVideoAutoplay { get; set; }
        [JsonPropertyName("seen_give_award_tooltip")] public bool? SeenGiveAwardTooltip { get; set; }
        [JsonPropertyName("seen_layout_switch")] public bool? SeenLayoutSwitch { get; set; }
        [JsonPropertyName("seen_premium_adblock_modal")] public bool? SeenPremiumAdblockModal { get; set; }
        [JsonPropertyName("seen_redesign_modal")] public bool? SeenRedesignModal { get; set; }
        [JsonPropertyName("seen_subreddit_chat_ftux")] public bool? SeenSubredditChatFtux { get; set; }
        [JsonPropertyName("suspension_expiration_utc")] public double? SuspensionExpirationUtc { get; set; }
        [JsonPropertyName("total_karma")] public int? TotalKarma { get; set; }
        [JsonPropertyName("verified")] public bool? Verified { get; set; }
    }

    public sealed class RedditUserSubreddit
    {
        [JsonPropertyName("banner_img")] public string BannerImg { get; set; }
        [JsonPropertyName("banner_size")] public List<int> BannerSize { get; set; } = new();
        [JsonPropertyName("default_set")] public bool? DefaultSet { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("disable_contributor_requests")] public bool? DisableContributorRequests { get; set; }
        [JsonPropertyName("display_name")] public string Caption { get; set; }
        [JsonPropertyName("display_name_prefixed")] public string CaptionPrefixed { get; set; }
        [JsonPropertyName("free_form_reports")] public bool? FreeFormReports { get; set; }
        [JsonPropertyName("header_img")] public string HeaderImg { get; set; }
        [JsonPropertyName("header_size")] public List<int> HeaderSize { get; set; } = new();
        [JsonPropertyName("icon_color")] public string IconColor { get; set; }
        [JsonPropertyName("icon_img")] public string IconImg { get; set; }
        [JsonPropertyName("icon_size")] public List<int> IconSize { get; set; } = new();
        [JsonPropertyName("is_default_banner")] public bool? IsDefaultBanner { get; set; }
        [JsonPropertyName("is_default_icon")] public bool? IsDefaultIcon { get; set; }
        [JsonPropertyName("key_color")] public string KeyColor { get; set; }
        [JsonPropertyName("link_flair_enabled")] public bool? LinkFlairEnabled { get; set; }
        [JsonPropertyName("link_flair_position")] public string LinkFlairPosition { get; set; }
        [JsonPropertyName("over_18")] public bool? Over18 { get; set; }
        [JsonPropertyName("previous_names")] public List<string> PreviousNames { get; set; } = new();
        [JsonPropertyName("primary_color")] public string PrimaryColor { get; set; }
        [JsonPropertyName("public_description")] public string PublicDescription { get; set; }
        [JsonPropertyName("restrict_commenting")] public bool? RestrictCommenting { get; set; }
        [JsonPropertyName("restrict_posting")] public bool? RestrictPosting { get; set; }
        [JsonPropertyName("show_media")] public bool? ShowMedia { get; set; }
        [JsonPropertyName("show_media_preview")] public bool? ShowMediaPreview { get; set; }
        [JsonPropertyName("spoilers_enabled")] public bool? SpoilersEnabled { get; set; }
        [JsonPropertyName("subreddit_type")] public string SubredditType { get; set; }
        [JsonPropertyName("submit_link_label")] public string SubmitLinkLabel { get; set; }
        [JsonPropertyName("submit_text")] public string SubmitText { get; set; }
        [JsonPropertyName("submit_text_label")] public string SubmitTextLabel { get; set; }
        [JsonPropertyName("subscribers")] public int? Subscribers { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("url")] public string Url { get; set; }
        [JsonPropertyName("user_flair_background_color")] public string UserFlairBackgroundColor { get; set; }
        [JsonPropertyName("user_flair_css_class")] public string UserFlairCssClass { get; set; }
        [JsonPropertyName("user_flair_enabled_in_sr")] public bool? UserFlairEnabledInSr { get; set; }
        [JsonPropertyName("user_flair_position")] public string UserFlairPosition { get; set; }
        [JsonPropertyName("user_flair_richtext")] public List<RedditFlairRichtext> UserFlairRichtext { get; set; } = new();
        [JsonPropertyName("user_flair_text")] public string UserFlairText { get; set; }
        [JsonPropertyName("user_flair_text_color")] public string UserFlairTextColor { get; set; }
        [JsonPropertyName("user_flair_type")] public string UserFlairType { get; set; }
        [JsonPropertyName("user_has_favorited")] public bool? UserHasFavorited { get; set; }
        [JsonPropertyName("user_is_banned")] public bool? UserIsBanned { get; set; }
        [JsonPropertyName("user_is_contributor")] public bool? UserIsContributor { get; set; }
        [JsonPropertyName("user_is_moderator")] public bool? UserIsModerator { get; set; }
        [JsonPropertyName("user_is_muted")] public bool? UserIsMuted { get; set; }
        [JsonPropertyName("user_is_subscriber")] public bool? UserIsSubscriber { get; set; }
        [JsonPropertyName("user_sr_flair_enabled")] public bool? UserSrFlairEnabled { get; set; }
        [JsonPropertyName("user_sr_theme_enabled")] public bool? UserSrThemeEnabled { get; set; }
        [JsonPropertyName("whitelist_status")] public string WhitelistStatus { get; set; }
        [JsonPropertyName("wls")] public int? Wls { get; set; }
        [JsonPropertyName("wiki_enabled")] public bool? WikiEnabled { get; set; }
    }

    // -------------------------------------------------------
    // Response Wrappers
    // -------------------------------------------------------
    public sealed class RedditResponse<T>
    {
        [JsonPropertyName("kind")] public string Kind { get; set; }
        [JsonPropertyName("data")] public RedditResponseData<T> Data { get; set; }
    }

    public sealed class RedditResponseData<T>
    {
        [JsonPropertyName("after")] public string After { get; set; }
        [JsonPropertyName("before")] public string Before { get; set; }
        [JsonPropertyName("children")] public List<RedditResponseChild<T>> Children { get; set; } = new();
        [JsonPropertyName("dist")] public int? Dist { get; set; }
        [JsonPropertyName("modhash")] public string Modhash { get; set; }
    }

    public sealed class RedditResponseChild<T>
    {
        [JsonPropertyName("kind")] public string Kind { get; set; }
        [JsonPropertyName("data")] public T Data { get; set; }
    }

    public sealed class RedditUserResponse
    {
        [JsonPropertyName("kind")] public string Kind { get; set; }
        [JsonPropertyName("data")] public RedditUser Data { get; set; }
    }

    public sealed class RedditSubredditResponse
    {
        [JsonPropertyName("kind")] public string Kind { get; set; }
        [JsonPropertyName("data")] public RedditSubreddit Data { get; set; }
    }

    public sealed class RedditSubmitResponse
    {
        [JsonPropertyName("success")] public bool Success { get; set; }
        [JsonPropertyName("data")] public RedditSubmitData Data { get; set; }
    }

    public sealed class RedditSubmitData
    {
        [JsonPropertyName("name")] public string Name { get; set; } // e.g., "t3_xxxxx"
    }

    // -------------------------------------------------------
    // Search Result (same as Post for Reddit)
    // -------------------------------------------------------
    public sealed class RedditSearchResult : RedditPost
    {
        // Inherits all properties from RedditPost
    }
}