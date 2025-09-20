// File: Connectors/SocialMedia/Snapchat/Models.cs
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Snapchat.Models
{
    // -------------------------------------------------------
    // Base Entity
    // -------------------------------------------------------
    public abstract class SnapchatEntityBase
    {
        [JsonIgnore] public IDataSource DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : SnapchatEntityBase { DataSource = ds; return (T)this; }
    }

    // -------------------------------------------------------
    // Organization
    // -------------------------------------------------------
    public sealed class SnapchatOrganization : SnapchatEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("country")] public string Country { get; set; }
        [JsonPropertyName("timezone")] public string Timezone { get; set; }
        [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; set; }
        [JsonPropertyName("updated_at")] public DateTime? UpdatedAt { get; set; }
        [JsonPropertyName("address")] public SnapchatAddress Address { get; set; }
        [JsonPropertyName("contact")] public SnapchatContact Contact { get; set; }
        [JsonPropertyName("features")] public List<string> Features { get; set; } = new();
        [JsonPropertyName("status")] public string Status { get; set; }
    }

    public sealed class SnapchatAddress
    {
        [JsonPropertyName("street_address")] public string StreetAddress { get; set; }
        [JsonPropertyName("city")] public string City { get; set; }
        [JsonPropertyName("region")] public string Region { get; set; }
        [JsonPropertyName("country")] public string Country { get; set; }
        [JsonPropertyName("postal_code")] public string PostalCode { get; set; }
    }

    public sealed class SnapchatContact
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("email")] public string Email { get; set; }
        [JsonPropertyName("phone")] public string Phone { get; set; }
    }

    // -------------------------------------------------------
    // Ad Account
    // -------------------------------------------------------
    public sealed class SnapchatAdAccount : SnapchatEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("organization_id")] public string OrganizationId { get; set; }
        [JsonPropertyName("currency")] public string Currency { get; set; }
        [JsonPropertyName("timezone")] public string Timezone { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; }
        [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; set; }
        [JsonPropertyName("updated_at")] public DateTime? UpdatedAt { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("funding_source_ids")] public List<string> FundingSourceIds { get; set; } = new();
        [JsonPropertyName("lifecycle_stage")] public string LifecycleStage { get; set; }
        [JsonPropertyName("spend_cap")] public long? SpendCap { get; set; }
        [JsonPropertyName("spend_cap_type")] public string SpendCapType { get; set; }
    }

    // -------------------------------------------------------
    // Campaign
    // -------------------------------------------------------
    public sealed class SnapchatCampaign : SnapchatEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("ad_account_id")] public string AdAccountId { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; }
        [JsonPropertyName("objective")] public string Objective { get; set; }
        [JsonPropertyName("start_time")] public DateTime? StartTime { get; set; }
        [JsonPropertyName("end_time")] public DateTime? EndTime { get; set; }
        [JsonPropertyName("budget_micro")] public long? BudgetMicro { get; set; }
        [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; set; }
        [JsonPropertyName("updated_at")] public DateTime? UpdatedAt { get; set; }
        [JsonPropertyName("delivery_status")] public string DeliveryStatus { get; set; }
        [JsonPropertyName("optimization_goal")] public string OptimizationGoal { get; set; }
        [JsonPropertyName("pacing_type")] public string PacingType { get; set; }
        [JsonPropertyName("campaign_type")] public string CampaignType { get; set; }
        [JsonPropertyName("buy_model")] public string BuyModel { get; set; }
        [JsonPropertyName("auto_bid")] public bool? AutoBid { get; set; }
        [JsonPropertyName("target_bid")] public long? TargetBid { get; set; }
        [JsonPropertyName("daily_budget_micro")] public long? DailyBudgetMicro { get; set; }
        [JsonPropertyName("lifetime_budget_micro")] public long? LifetimeBudgetMicro { get; set; }
    }

    // -------------------------------------------------------
    // Ad Squad
    // -------------------------------------------------------
    public sealed class SnapchatAdSquad : SnapchatEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("campaign_id")] public string CampaignId { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; }
        [JsonPropertyName("targeting")] public SnapchatTargeting Targeting { get; set; }
        [JsonPropertyName("billing_event")] public string BillingEvent { get; set; }
        [JsonPropertyName("bid_micro")] public long? BidMicro { get; set; }
        [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; set; }
        [JsonPropertyName("updated_at")] public DateTime? UpdatedAt { get; set; }
        [JsonPropertyName("optimization_goal")] public string OptimizationGoal { get; set; }
        [JsonPropertyName("pacing_type")] public string PacingType { get; set; }
        [JsonPropertyName("placement")] public SnapchatPlacement Placement { get; set; }
        [JsonPropertyName("start_time")] public DateTime? StartTime { get; set; }
        [JsonPropertyName("end_time")] public DateTime? EndTime { get; set; }
        [JsonPropertyName("daily_budget_micro")] public long? DailyBudgetMicro { get; set; }
        [JsonPropertyName("lifetime_budget_micro")] public long? LifetimeBudgetMicro { get; set; }
        [JsonPropertyName("frequency_caps")] public List<SnapchatFrequencyCap> FrequencyCaps { get; set; } = new();
        [JsonPropertyName("delivery_constraint")] public string DeliveryConstraint { get; set; }
        [JsonPropertyName("auto_bid")] public bool? AutoBid { get; set; }
        [JsonPropertyName("target_bid")] public long? TargetBid { get; set; }
        [JsonPropertyName("reach_goal")] public long? ReachGoal { get; set; }
        [JsonPropertyName("impression_goal")] public long? ImpressionGoal { get; set; }
    }

    public sealed class SnapchatTargeting
    {
        [JsonPropertyName("geos")] public List<SnapchatGeo> Geos { get; set; } = new();
        [JsonPropertyName("demographics")] public SnapchatDemographics Demographics { get; set; }
        [JsonPropertyName("interests")] public List<string> Interests { get; set; } = new();
        [JsonPropertyName("device_types")] public List<string> DeviceTypes { get; set; } = new();
        [JsonPropertyName("os_types")] public List<string> OsTypes { get; set; } = new();
        [JsonPropertyName("carrier_ids")] public List<string> CarrierIds { get; set; } = new();
        [JsonPropertyName("connection_types")] public List<string> ConnectionTypes { get; set; } = new();
        [JsonPropertyName("languages")] public List<string> Languages { get; set; } = new();
        [JsonPropertyName("regulated_content")] public bool? RegulatedContent { get; set; }
        [JsonPropertyName("advanced_demographics")] public SnapchatAdvancedDemographics AdvancedDemographics { get; set; }
        [JsonPropertyName("segments")] public List<SnapchatSegment> Segments { get; set; } = new();
        [JsonPropertyName("locations")] public List<SnapchatLocation> Locations { get; set; } = new();
    }

    public sealed class SnapchatGeo
    {
        [JsonPropertyName("country_code")] public string CountryCode { get; set; }
        [JsonPropertyName("region_id")] public string RegionId { get; set; }
        [JsonPropertyName("city_id")] public string CityId { get; set; }
        [JsonPropertyName("metro_id")] public string MetroId { get; set; }
        [JsonPropertyName("postal_code")] public string PostalCode { get; set; }
        [JsonPropertyName("location_type")] public string LocationType { get; set; }
    }

    public sealed class SnapchatDemographics
    {
        [JsonPropertyName("age_groups")] public List<string> AgeGroups { get; set; } = new();
        [JsonPropertyName("gender")] public string Gender { get; set; }
        [JsonPropertyName("min_age")] public int? MinAge { get; set; }
        [JsonPropertyName("max_age")] public int? MaxAge { get; set; }
    }

    public sealed class SnapchatAdvancedDemographics
    {
        [JsonPropertyName("household_income")] public List<string> HouseholdIncome { get; set; } = new();
        [JsonPropertyName("marital_status")] public List<string> MaritalStatus { get; set; } = new();
        [JsonPropertyName("education")] public List<string> Education { get; set; } = new();
        [JsonPropertyName("employment")] public List<string> Employment { get; set; } = new();
    }

    public sealed class SnapchatSegment
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("source")] public string Source { get; set; }
        [JsonPropertyName("retention_days")] public int? RetentionDays { get; set; }
    }

    public sealed class SnapchatLocation
    {
        [JsonPropertyName("latitude")] public double? Latitude { get; set; }
        [JsonPropertyName("longitude")] public double? Longitude { get; set; }
        [JsonPropertyName("radius_meters")] public int? RadiusMeters { get; set; }
    }

    public sealed class SnapchatPlacement
    {
        [JsonPropertyName("snapchat_positions")] public List<string> SnapchatPositions { get; set; } = new();
        [JsonPropertyName("publisher_placements")] public List<string> PublisherPlacements { get; set; } = new();
        [JsonPropertyName("excluded_publisher_list_ids")] public List<string> ExcludedPublisherListIds { get; set; } = new();
        [JsonPropertyName("included_publisher_list_ids")] public List<string> IncludedPublisherListIds { get; set; } = new();
    }

    public sealed class SnapchatFrequencyCap
    {
        [JsonPropertyName("frequency")] public int? Frequency { get; set; }
        [JsonPropertyName("interval_type")] public string IntervalType { get; set; }
    }

    // -------------------------------------------------------
    // Ad
    // -------------------------------------------------------
    public sealed class SnapchatAd : SnapchatEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("campaign_id")] public string CampaignId { get; set; }
        [JsonPropertyName("ad_squad_id")] public string AdSquadId { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("creative_id")] public string CreativeId { get; set; }
        [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; set; }
        [JsonPropertyName("updated_at")] public DateTime? UpdatedAt { get; set; }
        [JsonPropertyName("review_status")] public string ReviewStatus { get; set; }
        [JsonPropertyName("delivery_status")] public string DeliveryStatus { get; set; }
        [JsonPropertyName("render_type")] public string RenderType { get; set; }
    }

    // -------------------------------------------------------
    // Creative
    // -------------------------------------------------------
    public sealed class SnapchatCreative : SnapchatEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("ad_account_id")] public string AdAccountId { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("headline")] public string Headline { get; set; }
        [JsonPropertyName("call_to_action")] public string CallToAction { get; set; }
        [JsonPropertyName("media")] public SnapchatMedia Media { get; set; }
        [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; set; }
        [JsonPropertyName("updated_at")] public DateTime? UpdatedAt { get; set; }
        [JsonPropertyName("brand_name")] public string BrandName { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("longform_description")] public string LongformDescription { get; set; }
        [JsonPropertyName("shareable")] public bool? Shareable { get; set; }
        [JsonPropertyName("web_view_properties")] public SnapchatWebViewProperties WebViewProperties { get; set; }
        [JsonPropertyName("deep_link_properties")] public SnapchatDeepLinkProperties DeepLinkProperties { get; set; }
        [JsonPropertyName("top_snap_properties")] public SnapchatTopSnapProperties TopSnapProperties { get; set; }
        [JsonPropertyName("collection_properties")] public SnapchatCollectionProperties CollectionProperties { get; set; }
        [JsonPropertyName("app_install_properties")] public SnapchatAppInstallProperties AppInstallProperties { get; set; }
        [JsonPropertyName("review_status")] public string ReviewStatus { get; set; }
        [JsonPropertyName("render_type")] public string RenderType { get; set; }
    }

    public sealed class SnapchatMedia
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("url")] public string Url { get; set; }
        [JsonPropertyName("thumbnail_url")] public string ThumbnailUrl { get; set; }
        [JsonPropertyName("duration_seconds")] public int? DurationSeconds { get; set; }
        [JsonPropertyName("width")] public int? Width { get; set; }
        [JsonPropertyName("height")] public int? Height { get; set; }
        [JsonPropertyName("file_size_bytes")] public long? FileSizeBytes { get; set; }
        [JsonPropertyName("ad_account_id")] public string AdAccountId { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; set; }
        [JsonPropertyName("updated_at")] public DateTime? UpdatedAt { get; set; }
    }

    public sealed class SnapchatWebViewProperties
    {
        [JsonPropertyName("url")] public string Url { get; set; }
        [JsonPropertyName("allow_snap_javascript_sdk")] public bool? AllowSnapJavascriptSdk { get; set; }
    }

    public sealed class SnapchatDeepLinkProperties
    {
        [JsonPropertyName("deep_link_uri")] public string DeepLinkUri { get; set; }
        [JsonPropertyName("ios_app_id")] public string IosAppId { get; set; }
        [JsonPropertyName("android_app_url")] public string AndroidAppUrl { get; set; }
        [JsonPropertyName("fallback_type")] public string FallbackType { get; set; }
        [JsonPropertyName("ios_fallback_url")] public string IosFallbackUrl { get; set; }
        [JsonPropertyName("android_fallback_url")] public string AndroidFallbackUrl { get; set; }
    }

    public sealed class SnapchatTopSnapProperties
    {
        [JsonPropertyName("top_snap_media_id")] public string TopSnapMediaId { get; set; }
        [JsonPropertyName("top_snap_type")] public string TopSnapType { get; set; }
        [JsonPropertyName("top_snap_title")] public string TopSnapTitle { get; set; }
        [JsonPropertyName("top_snap_subtitle")] public string TopSnapSubtitle { get; set; }
    }

    public sealed class SnapchatCollectionProperties
    {
        [JsonPropertyName("collection_id")] public string CollectionId { get; set; }
        [JsonPropertyName("collection_title")] public string CollectionTitle { get; set; }
        [JsonPropertyName("collection_description")] public string CollectionDescription { get; set; }
        [JsonPropertyName("collection_items")] public List<SnapchatCollectionItem> CollectionItems { get; set; } = new();
    }

    public sealed class SnapchatCollectionItem
    {
        [JsonPropertyName("position")] public int? Position { get; set; }
        [JsonPropertyName("media_id")] public string MediaId { get; set; }
        [JsonPropertyName("headline")] public string Headline { get; set; }
        [JsonPropertyName("call_to_action")] public string CallToAction { get; set; }
        [JsonPropertyName("url")] public string Url { get; set; }
    }

    public sealed class SnapchatAppInstallProperties
    {
        [JsonPropertyName("ios_app_id")] public string IosAppId { get; set; }
        [JsonPropertyName("android_app_url")] public string AndroidAppUrl { get; set; }
        [JsonPropertyName("fallback_type")] public string FallbackType { get; set; }
        [JsonPropertyName("ios_fallback_url")] public string IosFallbackUrl { get; set; }
        [JsonPropertyName("android_fallback_url")] public string AndroidFallbackUrl { get; set; }
    }

    // -------------------------------------------------------
    // Analytics
    // -------------------------------------------------------
    public sealed class SnapchatAnalytics : SnapchatEntityBase
    {
        [JsonPropertyName("date")] public DateTime? Date { get; set; }
        [JsonPropertyName("campaign_id")] public string CampaignId { get; set; }
        [JsonPropertyName("ad_squad_id")] public string AdSquadId { get; set; }
        [JsonPropertyName("ad_id")] public string AdId { get; set; }
        [JsonPropertyName("creative_id")] public string CreativeId { get; set; }
        [JsonPropertyName("impressions")] public long? Impressions { get; set; }
        [JsonPropertyName("swipes")] public long? Swipes { get; set; }
        [JsonPropertyName("spend")] public decimal? Spend { get; set; }
        [JsonPropertyName("clicks")] public long? Clicks { get; set; }
        [JsonPropertyName("conversions")] public long? Conversions { get; set; }
        [JsonPropertyName("frequency")] public decimal? Frequency { get; set; }
        [JsonPropertyName("reach")] public long? Reach { get; set; }
        [JsonPropertyName("video_views")] public long? VideoViews { get; set; }
        [JsonPropertyName("video_completions")] public long? VideoCompletions { get; set; }
        [JsonPropertyName("video_quartile_1")] public long? VideoQuartile1 { get; set; }
        [JsonPropertyName("video_quartile_2")] public long? VideoQuartile2 { get; set; }
        [JsonPropertyName("video_quartile_3")] public long? VideoQuartile3 { get; set; }
        [JsonPropertyName("saves")] public long? Saves { get; set; }
        [JsonPropertyName("shares")] public long? Shares { get; set; }
        [JsonPropertyName("story_opens")] public long? StoryOpens { get; set; }
        [JsonPropertyName("story_completions")] public long? StoryCompletions { get; set; }
        [JsonPropertyName("attachment_frequency")] public decimal? AttachmentFrequency { get; set; }
        [JsonPropertyName("attachment_quartile_1")] public long? AttachmentQuartile1 { get; set; }
        [JsonPropertyName("attachment_quartile_2")] public long? AttachmentQuartile2 { get; set; }
        [JsonPropertyName("attachment_quartile_3")] public long? AttachmentQuartile3 { get; set; }
        [JsonPropertyName("attachment_uniques")] public long? AttachmentUniques { get; set; }
        [JsonPropertyName("attachment_total_views")] public long? AttachmentTotalViews { get; set; }
        [JsonPropertyName("attachment_avg_view_time")] public decimal? AttachmentAvgViewTime { get; set; }
        [JsonPropertyName("swipe_up_percent")] public decimal? SwipeUpPercent { get; set; }
        [JsonPropertyName("view_time_millis")] public long? ViewTimeMillis { get; set; }
        [JsonPropertyName("screen_time_millis")] public long? ScreenTimeMillis { get; set; }
        [JsonPropertyName("quartile_1")] public long? Quartile1 { get; set; }
        [JsonPropertyName("quartile_2")] public long? Quartile2 { get; set; }
        [JsonPropertyName("quartile_3")] public long? Quartile3 { get; set; }
        [JsonPropertyName("uniques")] public long? Uniques { get; set; }
        [JsonPropertyName("total_views")] public long? TotalViews { get; set; }
        [JsonPropertyName("avg_view_time")] public decimal? AvgViewTime { get; set; }
        [JsonPropertyName("avg_screen_time")] public decimal? AvgScreenTime { get; set; }
        [JsonPropertyName("conversion_purchases")] public long? ConversionPurchases { get; set; }
        [JsonPropertyName("conversion_purchases_value")] public decimal? ConversionPurchasesValue { get; set; }
        [JsonPropertyName("conversion_save")] public long? ConversionSave { get; set; }
        [JsonPropertyName("conversion_start_trial")] public long? ConversionStartTrial { get; set; }
        [JsonPropertyName("conversion_subscribe")] public long? ConversionSubscribe { get; set; }
        [JsonPropertyName("conversion_view_content")] public long? ConversionViewContent { get; set; }
        [JsonPropertyName("conversion_add_cart")] public long? ConversionAddCart { get; set; }
        [JsonPropertyName("conversion_add_billing")] public long? ConversionAddBilling { get; set; }
        [JsonPropertyName("conversion_searches")] public long? ConversionSearches { get; set; }
        [JsonPropertyName("conversion_level_complete")] public long? ConversionLevelComplete { get; set; }
        [JsonPropertyName("conversion_app_installs")] public long? ConversionAppInstalls { get; set; }
        [JsonPropertyName("conversion_page_views")] public long? ConversionPageViews { get; set; }
        [JsonPropertyName("conversion_sign_ups")] public long? ConversionSignUps { get; set; }
        [JsonPropertyName("conversion_custom_event_1")] public long? ConversionCustomEvent1 { get; set; }
        [JsonPropertyName("conversion_custom_event_2")] public long? ConversionCustomEvent2 { get; set; }
        [JsonPropertyName("conversion_custom_event_3")] public long? ConversionCustomEvent3 { get; set; }
        [JsonPropertyName("conversion_custom_event_4")] public long? ConversionCustomEvent4 { get; set; }
        [JsonPropertyName("conversion_custom_event_5")] public long? ConversionCustomEvent5 { get; set; }
    }

    // -------------------------------------------------------
    // Audience
    // -------------------------------------------------------
    public sealed class SnapchatAudience : SnapchatEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("ad_account_id")] public string AdAccountId { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("source_type")] public string SourceType { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; }
        [JsonPropertyName("size")] public long? Size { get; set; }
        [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; set; }
        [JsonPropertyName("updated_at")] public DateTime? UpdatedAt { get; set; }
        [JsonPropertyName("retention_days")] public int? RetentionDays { get; set; }
        [JsonPropertyName("targetable")] public bool? Targetable { get; set; }
        [JsonPropertyName("approximate_size")] public long? ApproximateSize { get; set; }
        [JsonPropertyName("match_rate")] public decimal? MatchRate { get; set; }
        [JsonPropertyName("upload_status")] public string UploadStatus { get; set; }
        [JsonPropertyName("expires_at")] public DateTime? ExpiresAt { get; set; }
    }

    // -------------------------------------------------------
    // Funding Source
    // -------------------------------------------------------
    public sealed class SnapchatFundingSource : SnapchatEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("organization_id")] public string OrganizationId { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("currency")] public string Currency { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; }
        [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; set; }
        [JsonPropertyName("updated_at")] public DateTime? UpdatedAt { get; set; }
        [JsonPropertyName("credit_limit_micro")] public long? CreditLimitMicro { get; set; }
        [JsonPropertyName("balance_micro")] public long? BalanceMicro { get; set; }
        [JsonPropertyName("payment_method_id")] public string PaymentMethodId { get; set; }
    }

    // -------------------------------------------------------
    // Media Library
    // -------------------------------------------------------
    public sealed class SnapchatMediaLibrary : SnapchatEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("ad_account_id")] public string AdAccountId { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("url")] public string Url { get; set; }
        [JsonPropertyName("thumbnail_url")] public string ThumbnailUrl { get; set; }
        [JsonPropertyName("duration_seconds")] public int? DurationSeconds { get; set; }
        [JsonPropertyName("width")] public int? Width { get; set; }
        [JsonPropertyName("height")] public int? Height { get; set; }
        [JsonPropertyName("file_size_bytes")] public long? FileSizeBytes { get; set; }
        [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; set; }
        [JsonPropertyName("updated_at")] public DateTime? UpdatedAt { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; }
        [JsonPropertyName("download_url")] public string DownloadUrl { get; set; }
        [JsonPropertyName("direct_download_url")] public string DirectDownloadUrl { get; set; }
    }

    // -------------------------------------------------------
    // Pixel
    // -------------------------------------------------------
    public sealed class SnapchatPixel : SnapchatEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("ad_account_id")] public string AdAccountId { get; set; }
        [JsonPropertyName("pixel_id")] public string PixelId { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; }
        [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; set; }
        [JsonPropertyName("updated_at")] public DateTime? UpdatedAt { get; set; }
        [JsonPropertyName("last_fired_time")] public DateTime? LastFiredTime { get; set; }
        [JsonPropertyName("fired_count")] public long? FiredCount { get; set; }
    }
}