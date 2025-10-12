using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.TikTokAdsDataSource
{
    /// <summary>
    /// Base class for TikTok Ads entities
    /// </summary>
    public abstract class TikTokAdsEntityBase
    {
        [JsonIgnore] public IDataSource DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : TikTokAdsEntityBase { DataSource = ds; return (T)this; }
    }

    /// <summary>
    /// Represents an advertiser in TikTok Ads
    /// </summary>
    public sealed class TikTokAdvertiser : TikTokAdsEntityBase
    {
        /// <summary>
        /// Advertiser ID
        /// </summary>
        [JsonPropertyName("advertiser_id")]
        public string? AdvertiserId { get; set; }

        /// <summary>
        /// Advertiser na        /// <summary>
        /// Attribution window
        /// </summary>
        [JsonPropertyName("attribution_window")]
        public decimal? AttributionWindow { get; set; }
    }

    /// <summary>
    /// Response wrapper for TikTok Ads API
    /// </summary>
    public class TikTokAdsResponse<T>
    {
        [JsonPropertyName("data")]
        public TikTokAdsResponseData<T>? Data { get; set; }

        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }

    /// <summary>
    /// Data container for TikTok Ads response
    /// </summary>
    public class TikTokAdsResponseData<T>
    {
        [JsonPropertyName("list")]
        public List<T>? List { get; set; }
    } /// </summary>

    /// <summary>
    /// Represents an advertiser in TikTok Ads
    /// </summary>
    public sealed class TikTokAdvertiser : TikTokAdsEntityBase
    {
        /// <summary>
        /// Advertiser ID
        /// </summary>
        [JsonPropertyName("advertiser_id")]
        public string? AdvertiserId { get; set; }

        /// <summary>
        /// Advertiser name
        /// </summary>
        [JsonPropertyName("advertiser_name")]
        public string? AdvertiserName { get; set; }

        /// <summary>
        /// Advertiser role
        /// </summary>
        [JsonPropertyName("advertiser_role")]
        public string? AdvertiserRole { get; set; }

        /// <summary>
        /// Currency used by the advertiser
        /// </summary>
        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        /// <summary>
        /// Timezone of the advertiser
        /// </summary>
        [JsonPropertyName("timezone")]
        public string? Timezone { get; set; }

        /// <summary>
        /// Company name
        /// </summary>
        [JsonPropertyName("company")]
        public string? Company { get; set; }

        /// <summary>
        /// Industry of the advertiser
        /// </summary>
        [JsonPropertyName("industry")]
        public string? Industry { get; set; }

        /// <summary>
        /// Address information
        /// </summary>
        [JsonPropertyName("address")]
        public string? Address { get; set; }

        /// <summary>
        /// Phone number
        /// </summary>
        [JsonPropertyName("phone_number")]
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Email address
        /// </summary>
        [JsonPropertyName("email")]
        public string? Email { get; set; }

        /// <summary>
        /// Status of the advertiser account
        /// </summary>
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        /// <summary>
        /// Account balance
        /// </summary>
        [JsonPropertyName("balance")]
        public decimal? Balance { get; set; }

        /// <summary>
        /// Associated campaigns
        /// </summary>
        [JsonPropertyName("campaigns")]
        public List<TikTokCampaign>? Campaigns { get; set; }
    }

    /// <summary>
    /// Represents a campaign in TikTok Ads
    /// </summary>
    public sealed class TikTokCampaign : TikTokAdsEntityBase
    {
        /// <summary>
        /// Campaign ID
        /// </summary>
        [JsonPropertyName("campaign_id")]
        public string? CampaignId { get; set; }

        /// <summary>
        /// Campaign name
        /// </summary>
        [JsonPropertyName("campaign_name")]
        public string? CampaignName { get; set; }

        /// <summary>
        /// Advertiser ID this campaign belongs to
        /// </summary>
        [JsonPropertyName("advertiser_id")]
        public string? AdvertiserId { get; set; }

        /// <summary>
        /// Campaign type
        /// </summary>
        [JsonPropertyName("campaign_type")]
        public string? CampaignType { get; set; }

        /// <summary>
        /// Objective type of the campaign
        /// </summary>
        [JsonPropertyName("objective_type")]
        public string? ObjectiveType { get; set; }

        /// <summary>
        /// Budget mode (BUDGET_MODE_INFINITE, BUDGET_MODE_DAY, BUDGET_MODE_TOTAL)
        /// </summary>
        [JsonPropertyName("budget_mode")]
        public string? BudgetMode { get; set; }

        /// <summary>
        /// Campaign budget
        /// </summary>
        [JsonPropertyName("budget")]
        public decimal? Budget { get; set; }

        /// <summary>
        /// Campaign status
        /// </summary>
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        /// <summary>
        /// Optimization status
        /// </summary>
        [JsonPropertyName("opt_status")]
        public string? OptStatus { get; set; }

        /// <summary>
        /// App profile page state
        /// </summary>
        [JsonPropertyName("campaign_app_profile_page_state")]
        public string? CampaignAppProfilePageState { get; set; }

        /// <summary>
        /// Associated ad groups
        /// </summary>
        [JsonPropertyName("adgroups")]
        public List<TikTokAdGroup>? AdGroups { get; set; }

        /// <summary>
        /// Campaign performance metrics
        /// </summary>
        [JsonPropertyName("metrics")]
        public TikTokCampaignMetrics? Metrics { get; set; }
    }

    /// <summary>
    /// Campaign performance metrics
    /// </summary>
    public class TikTokCampaignMetrics
    {
        /// <summary>
        /// Total impressions
        /// </summary>
        [JsonPropertyName("impressions")]
        public long? Impressions { get; set; }

        /// <summary>
        /// Total clicks
        /// </summary>
        [JsonPropertyName("clicks")]
        public long? Clicks { get; set; }

        /// <summary>
        /// Total spend
        /// </summary>
        [JsonPropertyName("spend")]
        public decimal? Spend { get; set; }

        /// <summary>
        /// Total conversions
        /// </summary>
        [JsonPropertyName("conversions")]
        public long? Conversions { get; set; }

        /// <summary>
        /// Click-through rate
        /// </summary>
        [JsonPropertyName("ctr")]
        public decimal? Ctr { get; set; }

        /// <summary>
        /// Cost per click
        /// </summary>
        [JsonPropertyName("cpc")]
        public decimal? Cpc { get; set; }

        /// <summary>
        /// Cost per mille
        /// </summary>
        [JsonPropertyName("cpm")]
        public decimal? Cpm { get; set; }
    }

    /// <summary>
    /// Represents an ad group in TikTok Ads
    /// </summary>
    public sealed class TikTokAdGroup : TikTokAdsEntityBase
    {
        /// <summary>
        /// Ad group ID
        /// </summary>
        [JsonPropertyName("adgroup_id")]
        public string? AdgroupId { get; set; }

        /// <summary>
        /// Ad group name
        /// </summary>
        [JsonPropertyName("adgroup_name")]
        public string? AdgroupName { get; set; }

        /// <summary>
        /// Campaign ID this ad group belongs to
        /// </summary>
        [JsonPropertyName("campaign_id")]
        public string? CampaignId { get; set; }

        /// <summary>
        /// Advertiser ID
        /// </summary>
        [JsonPropertyName("advertiser_id")]
        public string? AdvertiserId { get; set; }

        /// <summary>
        /// Placement type
        /// </summary>
        [JsonPropertyName("placement_type")]
        public string? PlacementType { get; set; }

        /// <summary>
        /// Billing event
        /// </summary>
        [JsonPropertyName("billing_event")]
        public string? BillingEvent { get; set; }

        /// <summary>
        /// Bid type
        /// </summary>
        [JsonPropertyName("bid_type")]
        public string? BidType { get; set; }

        /// <summary>
        /// Bid price
        /// </summary>
        [JsonPropertyName("bid_price")]
        public decimal? BidPrice { get; set; }

        /// <summary>
        /// Ad group budget
        /// </summary>
        [JsonPropertyName("budget")]
        public decimal? Budget { get; set; }

        /// <summary>
        /// Ad group status
        /// </summary>
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        /// <summary>
        /// Optimization status
        /// </summary>
        [JsonPropertyName("opt_status")]
        public string? OptStatus { get; set; }

        /// <summary>
        /// Target audience configuration
        /// </summary>
        [JsonPropertyName("audience")]
        public TikTokAudience? Audience { get; set; }

        /// <summary>
        /// Associated ads
        /// </summary>
        [JsonPropertyName("ads")]
        public List<TikTokAd>? Ads { get; set; }

        /// <summary>
        /// Ad group performance metrics
        /// </summary>
        [JsonPropertyName("metrics")]
        public TikTokAdGroupMetrics? Metrics { get; set; }
    }

    /// <summary>
    /// Target audience configuration for TikTok Ads
    /// </summary>
    public class TikTokAudience
    {
        /// <summary>
        /// Target age groups
        /// </summary>
        [JsonPropertyName("age_groups")]
        public List<string>? AgeGroups { get; set; }

        /// <summary>
        /// Target genders
        /// </summary>
        [JsonPropertyName("genders")]
        public List<string>? Genders { get; set; }

        /// <summary>
        /// Target languages
        /// </summary>
        [JsonPropertyName("languages")]
        public List<string>? Languages { get; set; }

        /// <summary>
        /// Target location IDs
        /// </summary>
        [JsonPropertyName("location_ids")]
        public List<string>? LocationIds { get; set; }

        /// <summary>
        /// Target interests
        /// </summary>
        [JsonPropertyName("interests")]
        public List<string>? Interests { get; set; }

        /// <summary>
        /// Target device models
        /// </summary>
        [JsonPropertyName("device_models")]
        public List<string>? DeviceModels { get; set; }

        /// <summary>
        /// Target operating systems
        /// </summary>
        [JsonPropertyName("operating_systems")]
        public List<string>? OperatingSystems { get; set; }

        /// <summary>
        /// Target network types
        /// </summary>
        [JsonPropertyName("network_types")]
        public List<string>? NetworkTypes { get; set; }

        /// <summary>
        /// Custom audience IDs
        /// </summary>
        [JsonPropertyName("custom_audience_ids")]
        public List<string>? CustomAudienceIds { get; set; }

        /// <summary>
        /// Lookalike audience IDs
        /// </summary>
        [JsonPropertyName("lookalike_audience_ids")]
        public List<string>? LookalikeAudienceIds { get; set; }
    }

    /// <summary>
    /// Ad group performance metrics
    /// </summary>
    public class TikTokAdGroupMetrics
    {
        /// <summary>
        /// Total impressions
        /// </summary>
        [JsonPropertyName("impressions")]
        public long? Impressions { get; set; }

        /// <summary>
        /// Total clicks
        /// </summary>
        [JsonPropertyName("clicks")]
        public long? Clicks { get; set; }

        /// <summary>
        /// Total spend
        /// </summary>
        [JsonPropertyName("spend")]
        public decimal? Spend { get; set; }

        /// <summary>
        /// Total conversions
        /// </summary>
        [JsonPropertyName("conversions")]
        public long? Conversions { get; set; }

        /// <summary>
        /// Click-through rate
        /// </summary>
        [JsonPropertyName("ctr")]
        public decimal? Ctr { get; set; }

        /// <summary>
        /// Cost per click
        /// </summary>
        [JsonPropertyName("cpc")]
        public decimal? Cpc { get; set; }

        /// <summary>
        /// Cost per mille
        /// </summary>
        [JsonPropertyName("cpm")]
        public decimal? Cpm { get; set; }

        /// <summary>
        /// Reach
        /// </summary>
        [JsonPropertyName("reach")]
        public long? Reach { get; set; }

        /// <summary>
        /// Frequency
        /// </summary>
        [JsonPropertyName("frequency")]
        public decimal? Frequency { get; set; }
    }

    /// <summary>
    /// Represents an ad in TikTok Ads
    /// </summary>
    public sealed class TikTokAd : TikTokAdsEntityBase
    {
        /// <summary>
        /// Ad ID
        /// </summary>
        [JsonPropertyName("ad_id")]
        public string? AdId { get; set; }

        /// <summary>
        /// Ad name
        /// </summary>
        [JsonPropertyName("ad_name")]
        public string? AdName { get; set; }

        /// <summary>
        /// Ad group ID this ad belongs to
        /// </summary>
        [JsonPropertyName("adgroup_id")]
        public string? AdgroupId { get; set; }

        /// <summary>
        /// Advertiser ID
        /// </summary>
        [JsonPropertyName("advertiser_id")]
        public string? AdvertiserId { get; set; }

        /// <summary>
        /// Ad format
        /// </summary>
        [JsonPropertyName("ad_format")]
        public string? AdFormat { get; set; }

        /// <summary>
        /// Ad status
        /// </summary>
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        /// <summary>
        /// Optimization status
        /// </summary>
        [JsonPropertyName("opt_status")]
        public string? OptStatus { get; set; }

        /// <summary>
        /// Call to action
        /// </summary>
        [JsonPropertyName("call_to_action")]
        public string? CallToAction { get; set; }

        /// <summary>
        /// Impression tracking URL
        /// </summary>
        [JsonPropertyName("impression_tracking_url")]
        public string? ImpressionTrackingUrl { get; set; }

        /// <summary>
        /// Click tracking URL
        /// </summary>
        [JsonPropertyName("click_tracking_url")]
        public string? ClickTrackingUrl { get; set; }

        /// <summary>
        /// Video ID for video ads
        /// </summary>
        [JsonPropertyName("video_id")]
        public string? VideoId { get; set; }

        /// <summary>
        /// Image IDs for image ads
        /// </summary>
        [JsonPropertyName("image_ids")]
        public List<string>? ImageIds { get; set; }

        /// <summary>
        /// Ad title
        /// </summary>
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        /// <summary>
        /// Ad description
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Landing page URL
        /// </summary>
        [JsonPropertyName("landing_page_url")]
        public string? LandingPageUrl { get; set; }

        /// <summary>
        /// Ad creative content
        /// </summary>
        [JsonPropertyName("creative")]
        public TikTokAdCreative? Creative { get; set; }

        /// <summary>
        /// Ad performance metrics
        /// </summary>
        [JsonPropertyName("metrics")]
        public TikTokAdMetrics? Metrics { get; set; }
    }

    /// <summary>
    /// Ad creative content
    /// </summary>
    public class TikTokAdCreative
    {
        /// <summary>
        /// Creative type
        /// </summary>
        [JsonPropertyName("creative_type")]
        public string? CreativeType { get; set; }

        /// <summary>
        /// Creative material
        /// </summary>
        [JsonPropertyName("materials")]
        public List<TikTokCreativeMaterial>? Materials { get; set; }

        /// <summary>
        /// Creative text content
        /// </summary>
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        /// <summary>
        /// Creative title
        /// </summary>
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        /// <summary>
        /// Creative description
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    /// <summary>
    /// Creative material (image, video, etc.)
    /// </summary>
    public class TikTokCreativeMaterial
    {
        /// <summary>
        /// Material ID
        /// </summary>
        [JsonPropertyName("material_id")]
        public string? MaterialId { get; set; }

        /// <summary>
        /// Material type
        /// </summary>
        [JsonPropertyName("material_type")]
        public string? MaterialType { get; set; }

        /// <summary>
        /// Material URL
        /// </summary>
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        /// <summary>
        /// Material width
        /// </summary>
        [JsonPropertyName("width")]
        public int? Width { get; set; }

        /// <summary>
        /// Material height
        /// </summary>
        [JsonPropertyName("height")]
        public int? Height { get; set; }

        /// <summary>
        /// Material file size
        /// </summary>
        [JsonPropertyName("file_size")]
        public long? FileSize { get; set; }

        /// <summary>
        /// Material format
        /// </summary>
        [JsonPropertyName("format")]
        public string? Format { get; set; }
    }

    /// <summary>
    /// Ad performance metrics
    /// </summary>
    public class TikTokAdMetrics
    {
        /// <summary>
        /// Total impressions
        /// </summary>
        [JsonPropertyName("impressions")]
        public long? Impressions { get; set; }

        /// <summary>
        /// Total clicks
        /// </summary>
        [JsonPropertyName("clicks")]
        public long? Clicks { get; set; }

        /// <summary>
        /// Total spend
        /// </summary>
        [JsonPropertyName("spend")]
        public decimal? Spend { get; set; }

        /// <summary>
        /// Total conversions
        /// </summary>
        [JsonPropertyName("conversions")]
        public long? Conversions { get; set; }

        /// <summary>
        /// Click-through rate
        /// </summary>
        [JsonPropertyName("ctr")]
        public decimal? Ctr { get; set; }

        /// <summary>
        /// Cost per click
        /// </summary>
        [JsonPropertyName("cpc")]
        public decimal? Cpc { get; set; }

        /// <summary>
        /// Cost per mille
        /// </summary>
        [JsonPropertyName("cpm")]
        public decimal? Cpm { get; set; }

        /// <summary>
        /// Cost per result
        /// </summary>
        [JsonPropertyName("cpr")]
        public decimal? Cpr { get; set; }

        /// <summary>
        /// Cost per conversion
        /// </summary>
        [JsonPropertyName("cp_conversion")]
        public decimal? CpConversion { get; set; }

        /// <summary>
        /// Reach
        /// </summary>
        [JsonPropertyName("reach")]
        public long? Reach { get; set; }

        /// <summary>
        /// Frequency
        /// </summary>
        [JsonPropertyName("frequency")]
        public decimal? Frequency { get; set; }

        /// <summary>
        /// Likes
        /// </summary>
        [JsonPropertyName("likes")]
        public long? Likes { get; set; }

        /// <summary>
        /// Comments
        /// </summary>
        [JsonPropertyName("comments")]
        public long? Comments { get; set; }

        /// <summary>
        /// Shares
        /// </summary>
        [JsonPropertyName("shares")]
        public long? Shares { get; set; }

        /// <summary>
        /// Profile visits
        /// </summary>
        [JsonPropertyName("profile_visits")]
        public long? ProfileVisits { get; set; }

        /// <summary>
        /// Follows
        /// </summary>
        [JsonPropertyName("follows")]
        public long? Follows { get; set; }

        /// <summary>
        /// Conversion rate
        /// </summary>
        [JsonPropertyName("conversion_rate")]
        public decimal? ConversionRate { get; set; }
    }

    /// <summary>
    /// Analytics data for TikTok Ads
    /// </summary>
    public sealed class TikTokAnalytics : TikTokAdsEntityBase
    {
        /// <summary>
        /// Ad ID
        /// </summary>
        [JsonPropertyName("ad_id")]
        public string? AdId { get; set; }

        /// <summary>
        /// Date for these analytics
        /// </summary>
        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }

        /// <summary>
        /// Advertiser ID
        /// </summary>
        [JsonPropertyName("advertiser_id")]
        public string? AdvertiserId { get; set; }

        /// <summary>
        /// Campaign ID
        /// </summary>
        [JsonPropertyName("campaign_id")]
        public string? CampaignId { get; set; }

        /// <summary>
        /// Ad group ID
        /// </summary>
        [JsonPropertyName("adgroup_id")]
        public string? AdgroupId { get; set; }

        /// <summary>
        /// Impressions
        /// </summary>
        [JsonPropertyName("impressions")]
        public long? Impressions { get; set; }

        /// <summary>
        /// Clicks
        /// </summary>
        [JsonPropertyName("clicks")]
        public long? Clicks { get; set; }

        /// <summary>
        /// Cost per click
        /// </summary>
        [JsonPropertyName("cost_per_click")]
        public decimal? CostPerClick { get; set; }

        /// <summary>
        /// Cost per mille
        /// </summary>
        [JsonPropertyName("cost_per_mille")]
        public decimal? CostPerMille { get; set; }

        /// <summary>
        /// Cost per result
        /// </summary>
        [JsonPropertyName("cost_per_result")]
        public decimal? CostPerResult { get; set; }

        /// <summary>
        /// Cost per conversion
        /// </summary>
        [JsonPropertyName("cost_per_conversion")]
        public decimal? CostPerConversion { get; set; }

        /// <summary>
        /// Total spend
        /// </summary>
        [JsonPropertyName("spend")]
        public decimal? Spend { get; set; }

        /// <summary>
        /// Reach
        /// </summary>
        [JsonPropertyName("reach")]
        public long? Reach { get; set; }

        /// <summary>
        /// Frequency
        /// </summary>
        [JsonPropertyName("frequency")]
        public decimal? Frequency { get; set; }

        /// <summary>
        /// Conversions
        /// </summary>
        [JsonPropertyName("conversion")]
        public long? Conversion { get; set; }

        /// <summary>
        /// Results
        /// </summary>
        [JsonPropertyName("result")]
        public long? Result { get; set; }

        /// <summary>
        /// Likes
        /// </summary>
        [JsonPropertyName("likes")]
        public long? Likes { get; set; }

        /// <summary>
        /// Comments
        /// </summary>
        [JsonPropertyName("comments")]
        public long? Comments { get; set; }

        /// <summary>
        /// Shares
        /// </summary>
        [JsonPropertyName("shares")]
        public long? Shares { get; set; }

        /// <summary>
        /// Profile visits
        /// </summary>
        [JsonPropertyName("profile_visits")]
        public long? ProfileVisits { get; set; }

        /// <summary>
        /// Follows
        /// </summary>
        [JsonPropertyName("follows")]
        public long? Follows { get; set; }

        /// <summary>
        /// Click-through rate
        /// </summary>
        [JsonPropertyName("click_through_rate")]
        public decimal? ClickThroughRate { get; set; }

        /// <summary>
        /// Conversion rate
        /// </summary>
        [JsonPropertyName("conversion_rate")]
        public decimal? ConversionRate { get; set; }

        /// <summary>
        /// Attribution window
        /// </summary>
        [JsonPropertyName("attribution_window")]
        public string? AttributionWindow { get; set; }
    }
}