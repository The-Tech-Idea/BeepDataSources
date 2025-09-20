// File: BeepDM/Connectors/Marketing/GoogleAdsDataSource/Models/GoogleAdsModels.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Marketing.GoogleAdsDataSource.Models
{
    // Base
    public abstract class GoogleAdsEntityBase
    {
        [JsonPropertyName("resourceName")] public string? ResourceName { get; set; }
        [JsonPropertyName("id")] public string? Id { get; set; }

        [JsonIgnore] public IDataSource? DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : GoogleAdsEntityBase { DataSource = ds; return (T)this; }
    }

    // ---------- Customer objects ----------

    public sealed class GoogleAdsCustomer : GoogleAdsEntityBase
    {
        [JsonPropertyName("descriptiveName")] public string? DescriptiveName { get; set; }
        [JsonPropertyName("currencyCode")] public string? CurrencyCode { get; set; }
        [JsonPropertyName("timeZone")] public string? TimeZone { get; set; }
        [JsonPropertyName("manager")] public bool? Manager { get; set; }
        [JsonPropertyName("testAccount")] public bool? TestAccount { get; set; }
        [JsonPropertyName("autoTaggingEnabled")] public bool? AutoTaggingEnabled { get; set; }
        [JsonPropertyName("hasPartnersBadge")] public bool? HasPartnersBadge { get; set; }
        [JsonPropertyName("optimizationScore")] public double? OptimizationScore { get; set; }
        [JsonPropertyName("optimizationScoreWeight")] public double? OptimizationScoreWeight { get; set; }
    }

    // ---------- Campaign objects ----------

    public sealed class GoogleAdsCampaign : GoogleAdsEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("advertisingChannelType")] public string? AdvertisingChannelType { get; set; }
        [JsonPropertyName("advertisingChannelSubType")] public string? AdvertisingChannelSubType { get; set; }
        [JsonPropertyName("targetCpa")] public GoogleAdsMoney? TargetCpa { get; set; }
        [JsonPropertyName("targetRoas")] public double? TargetRoas { get; set; }
        [JsonPropertyName("budget")] public string? Budget { get; set; }
        [JsonPropertyName("biddingStrategy")] public string? BiddingStrategy { get; set; }
        [JsonPropertyName("startDate")] public string? StartDate { get; set; }
        [JsonPropertyName("endDate")] public string? EndDate { get; set; }
        [JsonPropertyName("finalUrlSuffix")] public string? FinalUrlSuffix { get; set; }
        [JsonPropertyName("urlCustomParameters")] public List<GoogleAdsCustomParameter>? UrlCustomParameters { get; set; }
        [JsonPropertyName("frequencyCaps")] public List<GoogleAdsFrequencyCapEntry>? FrequencyCaps { get; set; }
    }

    // ---------- Ad Group objects ----------

    public sealed class GoogleAdsAdGroup : GoogleAdsEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("campaign")] public string? Campaign { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("cpcBidMicros")] public long? CpcBidMicros { get; set; }
        [JsonPropertyName("cpmBidMicros")] public long? CpmBidMicros { get; set; }
        [JsonPropertyName("targetCpaMicros")] public long? TargetCpaMicros { get; set; }
        [JsonPropertyName("targetRoas")] public double? TargetRoas { get; set; }
        [JsonPropertyName("percentCpcBidMicros")] public long? PercentCpcBidMicros { get; set; }
        [JsonPropertyName("optimizedTargetingEnabled")] public bool? OptimizedTargetingEnabled { get; set; }
        [JsonPropertyName("displayCustomBidDimension")] public string? DisplayCustomBidDimension { get; set; }
        [JsonPropertyName("finalUrlSuffix")] public string? FinalUrlSuffix { get; set; }
        [JsonPropertyName("urlCustomParameters")] public List<GoogleAdsCustomParameter>? UrlCustomParameters { get; set; }
    }

    // ---------- Ad objects ----------

    public sealed class GoogleAdsAd : GoogleAdsEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("adGroup")] public string? AdGroup { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("finalUrls")] public List<string>? FinalUrls { get; set; }
        [JsonPropertyName("finalUrlSuffix")] public string? FinalUrlSuffix { get; set; }
        [JsonPropertyName("urlCustomParameters")] public List<GoogleAdsCustomParameter>? UrlCustomParameters { get; set; }
        [JsonPropertyName("displayPath")] public List<string>? DisplayPath { get; set; }
        [JsonPropertyName("automated")] public bool? Automated { get; set; }
        [JsonPropertyName("systemManagedEntitySource")] public string? SystemManagedEntitySource { get; set; }
    }

    // ---------- Keyword objects ----------

    public sealed class GoogleAdsKeyword : GoogleAdsEntityBase
    {
        [JsonPropertyName("text")] public string? Text { get; set; }
        [JsonPropertyName("matchType")] public string? MatchType { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("cpcBidMicros")] public long? CpcBidMicros { get; set; }
        [JsonPropertyName("qualityScore")] public int? QualityScore { get; set; }
        [JsonPropertyName("approvalStatus")] public string? ApprovalStatus { get; set; }
        [JsonPropertyName("topic")] public string? Topic { get; set; }
    }

    // ---------- Budget objects ----------

    public sealed class GoogleAdsCampaignBudget : GoogleAdsEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("amountMicros")] public long? AmountMicros { get; set; }
        [JsonPropertyName("deliveryMethod")] public string? DeliveryMethod { get; set; }
        [JsonPropertyName("explicitlyShared")] public bool? ExplicitlyShared { get; set; }
        [JsonPropertyName("referenceCount")] public long? ReferenceCount { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("period")] public string? Period { get; set; }
    }

    // ---------- Performance objects ----------

    public sealed class GoogleAdsCampaignPerformance : GoogleAdsEntityBase
    {
        [JsonPropertyName("campaign")] public string? Campaign { get; set; }
        [JsonPropertyName("campaignName")] public string? CampaignName { get; set; }
        [JsonPropertyName("impressions")] public long? Impressions { get; set; }
        [JsonPropertyName("clicks")] public long? Clicks { get; set; }
        [JsonPropertyName("costMicros")] public long? CostMicros { get; set; }
        [JsonPropertyName("conversions")] public double? Conversions { get; set; }
        [JsonPropertyName("conversionValue")] public double? ConversionValue { get; set; }
        [JsonPropertyName("ctr")] public double? Ctr { get; set; }
        [JsonPropertyName("averageCpc")] public long? AverageCpc { get; set; }
        [JsonPropertyName("averageCpm")] public long? AverageCpm { get; set; }
        [JsonPropertyName("segments.date")] public string? Date { get; set; }
    }

    // ---------- Supporting objects ----------

    public sealed class GoogleAdsMoney
    {
        [JsonPropertyName("currencyCode")] public string? CurrencyCode { get; set; }
        [JsonPropertyName("amountMicros")] public long? AmountMicros { get; set; }
    }

    public sealed class GoogleAdsCustomParameter
    {
        [JsonPropertyName("key")] public string? Key { get; set; }
        [JsonPropertyName("value")] public string? Value { get; set; }
    }

    public sealed class GoogleAdsFrequencyCapEntry
    {
        [JsonPropertyName("key")] public GoogleAdsFrequencyCapKey? Key { get; set; }
        [JsonPropertyName("cap")] public long? Cap { get; set; }
    }

    public sealed class GoogleAdsFrequencyCapKey
    {
        [JsonPropertyName("level")] public string? Level { get; set; }
        [JsonPropertyName("eventType")] public string? EventType { get; set; }
        [JsonPropertyName("timeUnit")] public string? TimeUnit { get; set; }
        [JsonPropertyName("timeLength")] public long? TimeLength { get; set; }
    }

    // ---------- Geographic objects ----------

    public sealed class GoogleAdsGeoTargetConstant : GoogleAdsEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("countryCode")] public string? CountryCode { get; set; }
        [JsonPropertyName("targetType")] public string? TargetType { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
    }

    // ---------- Language objects ----------

    public sealed class GoogleAdsLanguageConstant : GoogleAdsEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("targetable")] public bool? Targetable { get; set; }
    }

    // ---------- Asset objects ----------

    public sealed class GoogleAdsAsset : GoogleAdsEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("finalUrls")] public List<string>? FinalUrls { get; set; }
        [JsonPropertyName("imageAsset")] public GoogleAdsImageAsset? ImageAsset { get; set; }
        [JsonPropertyName("textAsset")] public GoogleAdsTextAsset? TextAsset { get; set; }
        [JsonPropertyName("youtubeVideoAsset")] public GoogleAdsYoutubeVideoAsset? YoutubeVideoAsset { get; set; }
    }

    public sealed class GoogleAdsImageAsset
    {
        [JsonPropertyName("fileSize")] public long? FileSize { get; set; }
        [JsonPropertyName("mimeType")] public string? MimeType { get; set; }
        [JsonPropertyName("fullSize")] public GoogleAdsImageDimension? FullSize { get; set; }
    }

    public sealed class GoogleAdsTextAsset
    {
        [JsonPropertyName("text")] public string? Text { get; set; }
    }

    public sealed class GoogleAdsYoutubeVideoAsset
    {
        [JsonPropertyName("youtubeVideoId")] public string? YoutubeVideoId { get; set; }
    }

    public sealed class GoogleAdsImageDimension
    {
        [JsonPropertyName("heightPixels")] public long? HeightPixels { get; set; }
        [JsonPropertyName("widthPixels")] public long? WidthPixels { get; set; }
        [JsonPropertyName("url")] public string? Url { get; set; }
    }

    // ---------- User List objects ----------

    public sealed class GoogleAdsUserList : GoogleAdsEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("size")] public long? Size { get; set; }
        [JsonPropertyName("sizeForDisplay")] public long? SizeForDisplay { get; set; }
        [JsonPropertyName("sizeForSearch")] public long? SizeForSearch { get; set; }
    }

    // ---------- Label objects ----------

    public sealed class GoogleAdsLabel : GoogleAdsEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("textLabel")] public GoogleAdsTextLabel? TextLabel { get; set; }
    }

    public sealed class GoogleAdsTextLabel
    {
        [JsonPropertyName("description")] public string? Description { get; set; }
    }
}