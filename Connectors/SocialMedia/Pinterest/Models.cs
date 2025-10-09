// File: Connectors/SocialMedia/Pinterest/Models.cs
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Pinterest.Models
{
    // -------------------------------------------------------
    // Base Entity
    // -------------------------------------------------------
    public abstract class PinterestEntityBase
    {
        [JsonIgnore] public IDataSource DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : PinterestEntityBase { DataSource = ds; return (T)this; }
    }

    // -------------------------------------------------------
    // User
    // -------------------------------------------------------
    public sealed class PinterestUser : PinterestEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("username")] public string Username { get; set; }
        [JsonPropertyName("first_name")] public string FirstName { get; set; }
        [JsonPropertyName("last_name")] public string LastName { get; set; }
        [JsonPropertyName("bio")] public string Bio { get; set; }
        [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; set; }
        [JsonPropertyName("counts")] public PinterestUserCounts Counts { get; set; }
        [JsonPropertyName("image")] public PinterestImage Image { get; set; }
        [JsonPropertyName("account_type")] public string AccountType { get; set; }
        [JsonPropertyName("website_url")] public string WebsiteUrl { get; set; }
        [JsonPropertyName("business_name")] public string BusinessName { get; set; }
        [JsonPropertyName("about")] public string About { get; set; }
        [JsonPropertyName("location")] public string Location { get; set; }
        [JsonPropertyName("is_verified")] public bool? IsVerified { get; set; }
        [JsonPropertyName("indexed")] public bool? Indexed { get; set; }
    }

    public sealed class PinterestUserCounts
    {
        [JsonPropertyName("pins")] public int? Pins { get; set; }
        [JsonPropertyName("following")] public int? Following { get; set; }
        [JsonPropertyName("followers")] public int? Followers { get; set; }
        [JsonPropertyName("boards")] public int? Boards { get; set; }
        [JsonPropertyName("likes")] public int? Likes { get; set; }
    }

    public sealed class PinterestImage
    {
        [JsonPropertyName("60x60")] public PinterestImageSize Size60x60 { get; set; }
        [JsonPropertyName("170x")] public PinterestImageSize Size170x { get; set; }
        [JsonPropertyName("236x")] public PinterestImageSize Size236x { get; set; }
        [JsonPropertyName("474x")] public PinterestImageSize Size474x { get; set; }
        [JsonPropertyName("564x")] public PinterestImageSize Size564x { get; set; }
        [JsonPropertyName("736x")] public PinterestImageSize Size736x { get; set; }
        [JsonPropertyName("orig")] public PinterestImageSize Original { get; set; }
    }

    public sealed class PinterestImageSize
    {
        [JsonPropertyName("url")] public string Url { get; set; }
        [JsonPropertyName("width")] public int? Width { get; set; }
        [JsonPropertyName("height")] public int? Height { get; set; }
    }

    // -------------------------------------------------------
    // Board
    // -------------------------------------------------------
    public sealed class PinterestBoard : PinterestEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("owner")] public PinterestUser Owner { get; set; }
        [JsonPropertyName("privacy")] public string Privacy { get; set; }
        [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; set; }
        [JsonPropertyName("counts")] public PinterestBoardCounts Counts { get; set; }
        [JsonPropertyName("image")] public PinterestImage Image { get; set; }
        [JsonPropertyName("url")] public string Url { get; set; }
        [JsonPropertyName("collaborator_count")] public int? CollaboratorCount { get; set; }
        [JsonPropertyName("collaborators")] public List<PinterestUser> Collaborators { get; set; } = new();
        [JsonPropertyName("layout")] public string Layout { get; set; }
        [JsonPropertyName("category")] public string Category { get; set; }
    }

    public sealed class PinterestBoardCounts
    {
        [JsonPropertyName("pins")] public int? Pins { get; set; }
        [JsonPropertyName("collaborators")] public int? Collaborators { get; set; }
        [JsonPropertyName("followers")] public int? Followers { get; set; }
    }

    // -------------------------------------------------------
    // Pin
    // -------------------------------------------------------
    public sealed class PinterestPin : PinterestEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("link")] public string Link { get; set; }
        [JsonPropertyName("url")] public string Url { get; set; }
        [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; set; }
        [JsonPropertyName("board")] public PinterestBoard Board { get; set; }
        [JsonPropertyName("counts")] public PinterestPinCounts Counts { get; set; }
        [JsonPropertyName("images")] public PinterestImage Images { get; set; }
        [JsonPropertyName("dominant_color")] public string DominantColor { get; set; }
        [JsonPropertyName("color")] public string Color { get; set; }
        [JsonPropertyName("media")] public PinterestMedia Media { get; set; }
        [JsonPropertyName("attribution")] public PinterestAttribution Attribution { get; set; }
        [JsonPropertyName("note")] public string Note { get; set; }
        [JsonPropertyName("metadata")] public PinterestMetadata Metadata { get; set; }
        [JsonPropertyName("creator")] public PinterestUser Creator { get; set; }
        [JsonPropertyName("is_promotable")] public bool? IsPromotable { get; set; }
        [JsonPropertyName("is_repin")] public bool? IsRepin { get; set; }
        [JsonPropertyName("is_video")] public bool? IsVideo { get; set; }
        [JsonPropertyName("tracked_link")] public string TrackedLink { get; set; }
    }

    public sealed class PinterestPinCounts
    {
        [JsonPropertyName("saves")] public int? Saves { get; set; }
        [JsonPropertyName("comments")] public int? Comments { get; set; }
    }

    public sealed class PinterestMedia
    {
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("images")] public PinterestImage Images { get; set; }
        [JsonPropertyName("cover_images")] public PinterestImage CoverImages { get; set; }
        [JsonPropertyName("video_url")] public string VideoUrl { get; set; }
        [JsonPropertyName("duration")] public double? Duration { get; set; }
    }

    public sealed class PinterestAttribution
    {
        [JsonPropertyName("provider_name")] public string ProviderName { get; set; }
        [JsonPropertyName("provider_icon_url")] public string ProviderIconUrl { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("url")] public string Url { get; set; }
        [JsonPropertyName("author_name")] public string AuthorName { get; set; }
        [JsonPropertyName("author_url")] public string AuthorUrl { get; set; }
    }

    public sealed class PinterestMetadata
    {
        [JsonPropertyName("article")] public PinterestArticleMetadata Article { get; set; }
        [JsonPropertyName("recipe")] public PinterestRecipeMetadata Recipe { get; set; }
        [JsonPropertyName("product")] public PinterestProductMetadata Product { get; set; }
        [JsonPropertyName("movie")] public PinterestMovieMetadata Movie { get; set; }
        [JsonPropertyName("tv_show")] public PinterestTVShowMetadata TVShow { get; set; }
    }

    public sealed class PinterestArticleMetadata
    {
        [JsonPropertyName("published_at")] public DateTime? PublishedAt { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("authors")] public List<PinterestAuthor> Authors { get; set; } = new();
    }

    public sealed class PinterestRecipeMetadata
    {
        [JsonPropertyName("servings")] public int? Servings { get; set; }
        [JsonPropertyName("ingredients")] public List<string> Ingredients { get; set; } = new();
        [JsonPropertyName("instructions")] public List<string> Instructions { get; set; } = new();
    }

    public sealed class PinterestProductMetadata
    {
        [JsonPropertyName("availability")] public string Availability { get; set; }
        [JsonPropertyName("price")] public string Price { get; set; }
        [JsonPropertyName("currency")] public string Currency { get; set; }
    }

    public sealed class PinterestMovieMetadata
    {
        [JsonPropertyName("rating")] public double? Rating { get; set; }
        [JsonPropertyName("duration")] public int? Duration { get; set; }
        [JsonPropertyName("release_date")] public DateTime? ReleaseDate { get; set; }
    }

    public sealed class PinterestTVShowMetadata
    {
        [JsonPropertyName("rating")] public double? Rating { get; set; }
        [JsonPropertyName("episode")] public string Episode { get; set; }
        [JsonPropertyName("season")] public string Season { get; set; }
    }

    public sealed class PinterestAuthor
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("url")] public string Url { get; set; }
    }

    // -------------------------------------------------------
    // Analytics
    // -------------------------------------------------------
    public sealed class PinterestAnalytics : PinterestEntityBase
    {
        [JsonPropertyName("date")] public DateTime? Date { get; set; }
        [JsonPropertyName("pin_id")] public string PinId { get; set; }
        [JsonPropertyName("impressions")] public long? Impressions { get; set; }
        [JsonPropertyName("saves")] public long? Saves { get; set; }
        [JsonPropertyName("clicks")] public long? Clicks { get; set; }
        [JsonPropertyName("outbound_clicks")] public long? OutboundClicks { get; set; }
        [JsonPropertyName("pin_clicks")] public long? PinClicks { get; set; }
        [JsonPropertyName("closeups")] public long? Closeups { get; set; }
        [JsonPropertyName("impressions_from_home")] public long? ImpressionsFromHome { get; set; }
        [JsonPropertyName("impressions_from_profile")] public long? ImpressionsFromProfile { get; set; }
        [JsonPropertyName("saves_from_home")] public long? SavesFromHome { get; set; }
        [JsonPropertyName("saves_from_profile")] public long? SavesFromProfile { get; set; }
        [JsonPropertyName("clicks_from_home")] public long? ClicksFromHome { get; set; }
        [JsonPropertyName("clicks_from_profile")] public long? ClicksFromProfile { get; set; }
        [JsonPropertyName("all_clicks")] public long? AllClicks { get; set; }
        [JsonPropertyName("all_saves")] public long? AllSaves { get; set; }
        [JsonPropertyName("all_impressions")] public long? AllImpressions { get; set; }
        [JsonPropertyName("video_views")] public long? VideoViews { get; set; }
        [JsonPropertyName("video_starts")] public long? VideoStarts { get; set; }
        [JsonPropertyName("video_completions")] public long? VideoCompletions { get; set; }
        [JsonPropertyName("video_completion_rate")] public double? VideoCompletionRate { get; set; }
        [JsonPropertyName("video_avg_watch_time")] public double? VideoAvgWatchTime { get; set; }
    }

    // -------------------------------------------------------
    // Following
    // -------------------------------------------------------
    public sealed class PinterestFollowing : PinterestEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("username")] public string Username { get; set; }
        [JsonPropertyName("first_name")] public string FirstName { get; set; }
        [JsonPropertyName("last_name")] public string LastName { get; set; }
        [JsonPropertyName("followed_at")] public DateTime? FollowedAt { get; set; }
        [JsonPropertyName("image")] public PinterestImage Image { get; set; }
        [JsonPropertyName("bio")] public string Bio { get; set; }
        [JsonPropertyName("is_verified")] public bool? IsVerified { get; set; }
    }

    // -------------------------------------------------------
    // Follower
    // -------------------------------------------------------
    public sealed class PinterestFollower : PinterestEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("username")] public string Username { get; set; }
        [JsonPropertyName("first_name")] public string FirstName { get; set; }
        [JsonPropertyName("last_name")] public string LastName { get; set; }
        [JsonPropertyName("followed_at")] public DateTime? FollowedAt { get; set; }
        [JsonPropertyName("image")] public PinterestImage Image { get; set; }
        [JsonPropertyName("bio")] public string Bio { get; set; }
        [JsonPropertyName("is_verified")] public bool? IsVerified { get; set; }
    }

    // -------------------------------------------------------
    // Search Results
    // -------------------------------------------------------
    public sealed class PinterestSearchResults : PinterestEntityBase
    {
        [JsonPropertyName("items")] public List<PinterestPin> Items { get; set; } = new();
        [JsonPropertyName("bookmark")] public string Bookmark { get; set; }
    }

    // -------------------------------------------------------
    // Interest
    // -------------------------------------------------------
    public sealed class PinterestInterest : PinterestEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("canonical_url")] public string CanonicalUrl { get; set; }
        [JsonPropertyName("image")] public PinterestImage Image { get; set; }
        [JsonPropertyName("follower_count")] public int? FollowerCount { get; set; }
        [JsonPropertyName("is_followed")] public bool? IsFollowed { get; set; }
    }

    // -------------------------------------------------------
    // Section
    // -------------------------------------------------------
    public sealed class PinterestSection : PinterestEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("board")] public PinterestBoard Board { get; set; }
        [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; set; }
    }

    // -------------------------------------------------------
    // Comment
    // -------------------------------------------------------
    public sealed class PinterestComment : PinterestEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("text")] public string Text { get; set; }
        [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; set; }
        [JsonPropertyName("author")] public PinterestUser Author { get; set; }
        [JsonPropertyName("pin")] public PinterestPin Pin { get; set; }
    }

    // -------------------------------------------------------
    // Ad Account
    // -------------------------------------------------------
    public sealed class PinterestAdAccount : PinterestEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("owner")] public PinterestUser Owner { get; set; }
        [JsonPropertyName("country")] public string Country { get; set; }
        [JsonPropertyName("currency")] public string Currency { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; }
        [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; set; }
        [JsonPropertyName("updated_at")] public DateTime? UpdatedAt { get; set; }
    }

    // -------------------------------------------------------
    // Campaign
    // -------------------------------------------------------
    public sealed class PinterestCampaign : PinterestEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("ad_account_id")] public string AdAccountId { get; set; }
        [JsonPropertyName("objective_type")] public string ObjectiveType { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; }
        [JsonPropertyName("lifetime_spend_cap")] public long? LifetimeSpendCap { get; set; }
        [JsonPropertyName("daily_spend_cap")] public long? DailySpendCap { get; set; }
        [JsonPropertyName("order_line_id")] public string OrderLineId { get; set; }
        [JsonPropertyName("tracking_urls")] public PinterestTrackingUrls TrackingUrls { get; set; }
        [JsonPropertyName("start_time")] public long? StartTime { get; set; }
        [JsonPropertyName("end_time")] public long? EndTime { get; set; }
        [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; set; }
        [JsonPropertyName("updated_at")] public DateTime? UpdatedAt { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("is_flexible_daily_budgets")] public bool? IsFlexibleDailyBudgets { get; set; }
        [JsonPropertyName("is_automated_campaign")] public bool? IsAutomatedCampaign { get; set; }
    }

    public sealed class PinterestTrackingUrls
    {
        [JsonPropertyName("impression")] public List<string> Impression { get; set; } = new();
        [JsonPropertyName("click")] public List<string> Click { get; set; } = new();
        [JsonPropertyName("engagement")] public List<string> Engagement { get; set; } = new();
        [JsonPropertyName("buyable_button")] public List<string> BuyableButton { get; set; } = new();
        [JsonPropertyName("audience_verification")] public List<string> AudienceVerification { get; set; } = new();
    }

    // -------------------------------------------------------
    // Ad Group
    // -------------------------------------------------------
    public sealed class PinterestAdGroup : PinterestEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("campaign_id")] public string CampaignId { get; set; }
        [JsonPropertyName("billable_event")] public string BillableEvent { get; set; }
        [JsonPropertyName("targeting_spec")] public PinterestTargetingSpec TargetingSpec { get; set; }
        [JsonPropertyName("lifetime_frequency_cap")] public int? LifetimeFrequencyCap { get; set; }
        [JsonPropertyName("tracking_urls")] public PinterestTrackingUrls TrackingUrls { get; set; }
        [JsonPropertyName("auto_targeting_enabled")] public bool? AutoTargetingEnabled { get; set; }
        [JsonPropertyName("placement_group")] public string PlacementGroup { get; set; }
        [JsonPropertyName("pacing_delivery_type")] public string PacingDeliveryType { get; set; }
        [JsonPropertyName("campaign_id")] public string CampaignIdDuplicate { get; set; }
        [JsonPropertyName("rf_predicted_cpc")] public double? RfPredictedCpc { get; set; }
        [JsonPropertyName("rf_minimum_cpc")] public double? RfMinimumCpc { get; set; }
        [JsonPropertyName("rf_maximum_cpc")] public double? RfMaximumCpc { get; set; }
        [JsonPropertyName("budget_type")] public string BudgetType { get; set; }
        [JsonPropertyName("daily_budget_in_micro_currency")] public long? DailyBudgetInMicroCurrency { get; set; }
        [JsonPropertyName("lifetime_budget_in_micro_currency")] public long? LifetimeBudgetInMicroCurrency { get; set; }
        [JsonPropertyName("budget_in_micro_currency")] public long? BudgetInMicroCurrency { get; set; }
        [JsonPropertyName("start_time")] public long? StartTime { get; set; }
        [JsonPropertyName("end_time")] public long? EndTime { get; set; }
        [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; set; }
        [JsonPropertyName("updated_at")] public DateTime? UpdatedAt { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("conversion_learning_mode_type")] public string ConversionLearningModeType { get; set; }
        [JsonPropertyName("summary_status")] public string SummaryStatus { get; set; }
    }

    public sealed class PinterestTargetingSpec
    {
        [JsonPropertyName("AGE_BUCKET")] public List<string> AgeBucket { get; set; } = new();
        [JsonPropertyName("APPTYPE")] public List<string> AppType { get; set; } = new();
        [JsonPropertyName("AUDIENCE_EXCLUDE")] public List<string> AudienceExclude { get; set; } = new();
        [JsonPropertyName("AUDIENCE_INCLUDE")] public List<string> AudienceInclude { get; set; } = new();
        [JsonPropertyName("GENDER")] public List<string> Gender { get; set; } = new();
        [JsonPropertyName("GEO")] public List<string> Geo { get; set; } = new();
        [JsonPropertyName("INTEREST")] public List<string> Interest { get; set; } = new();
        [JsonPropertyName("LOCALE")] public List<string> Locale { get; set; } = new();
        [JsonPropertyName("LOCATION")] public List<string> Location { get; set; } = new();
        [JsonPropertyName("SHOPPING_RETARGETING")] public List<string> ShoppingRetargeting { get; set; } = new();
        [JsonPropertyName("TARGETING_STRATEGY")] public List<string> TargetingStrategy { get; set; } = new();
    }

    // -------------------------------------------------------
    // Ad
    // -------------------------------------------------------
    public sealed class PinterestAd : PinterestEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("ad_group_id")] public string AdGroupId { get; set; }
        [JsonPropertyName("creative_type")] public string CreativeType { get; set; }
        [JsonPropertyName("carousel_android_deep_links")] public List<string> CarouselAndroidDeepLinks { get; set; } = new();
        [JsonPropertyName("carousel_destination_urls")] public List<string> CarouselDestinationUrls { get; set; } = new();
        [JsonPropertyName("carousel_ios_deep_links")] public List<string> CarouselIosDeepLinks { get; set; } = new();
        [JsonPropertyName("click_tracking_url")] public string ClickTrackingUrl { get; set; }
        [JsonPropertyName("destination_url")] public string DestinationUrl { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("pin_id")] public string PinId { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; }
        [JsonPropertyName("tracking_urls")] public PinterestTrackingUrls TrackingUrls { get; set; }
        [JsonPropertyName("view_tracking_url")] public string ViewTrackingUrl { get; set; }
        [JsonPropertyName("rejected_reasons")] public List<string> RejectedReasons { get; set; } = new();
        [JsonPropertyName("review_status")] public string ReviewStatus { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; set; }
        [JsonPropertyName("updated_at")] public DateTime? UpdatedAt { get; set; }
        [JsonPropertyName("summary_status")] public string SummaryStatus { get; set; }
    }

    // ---------------- Response Wrappers ----------------
    public sealed class PinterestResponse<T>
    {
        [JsonPropertyName("data")] public List<T> Data { get; set; } = new();
        [JsonPropertyName("bookmark")] public string Bookmark { get; set; }
        [JsonPropertyName("page")] public PinterestPageInfo Page { get; set; }
    }

    public sealed class PinterestPageInfo
    {
        [JsonPropertyName("cursor")] public string Cursor { get; set; }
        [JsonPropertyName("size")] public int? Size { get; set; }
    }
}