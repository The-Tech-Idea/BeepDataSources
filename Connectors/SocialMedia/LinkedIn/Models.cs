// File: Connectors/LinkedIn/Models/LinkedInModels.cs
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.LinkedIn.Models
{
    // -------------------------------------------------------
    // Base Entity
    // -------------------------------------------------------
    public abstract class LinkedInEntityBase
    {
        [JsonIgnore] public IDataSource DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : LinkedInEntityBase { DataSource = ds; return (T)this; }
    }

    // -------------------------------------------------------
    // Post/UGC Post
    // -------------------------------------------------------
    public sealed class LinkedInPost : LinkedInEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("author")] public string Author { get; set; }
        [JsonPropertyName("lifecycleState")] public string LifecycleState { get; set; }
        [JsonPropertyName("visibility")] public string Visibility { get; set; }
        [JsonPropertyName("lastModified")] public LinkedInTime LastModified { get; set; }
        [JsonPropertyName("created")] public LinkedInTime Created { get; set; }
        [JsonPropertyName("specificContent")] public LinkedInPostContent SpecificContent { get; set; }
        [JsonPropertyName("isReshareDisabledByAuthor")] public bool? IsReshareDisabledByAuthor { get; set; }
    }

    public sealed class LinkedInPostContent
    {
        [JsonPropertyName("com.linkedin.ugc.ShareContent")] public LinkedInShareContent ShareContent { get; set; }
    }

    public sealed class LinkedInShareContent
    {
        [JsonPropertyName("shareCommentary")] public LinkedInTextContent ShareCommentary { get; set; }
        [JsonPropertyName("shareMediaCategory")] public string ShareMediaCategory { get; set; }
        [JsonPropertyName("media")] public List<LinkedInMedia> Media { get; set; } = new();
    }

    public sealed class LinkedInTextContent
    {
        [JsonPropertyName("text")] public string Text { get; set; }
        [JsonPropertyName("attributes")] public List<LinkedInAttribute> Attributes { get; set; } = new();
    }

    public sealed class LinkedInMedia
    {
        [JsonPropertyName("status")] public string Status { get; set; }
        [JsonPropertyName("description")] public LinkedInTextContent Description { get; set; }
        [JsonPropertyName("media")] public string Media { get; set; }
        [JsonPropertyName("title")] public LinkedInTextContent Title { get; set; }
    }

    public sealed class LinkedInAttribute
    {
        [JsonPropertyName("length")] public int? Length { get; set; }
        [JsonPropertyName("start")] public int? Start { get; set; }
        [JsonPropertyName("value")] public LinkedInAttributeValue Value { get; set; }
    }

    public sealed class LinkedInAttributeValue
    {
        [JsonPropertyName("com.linkedin.common.CompanyAttributedEntity")] public LinkedInCompany Company { get; set; }
        [JsonPropertyName("com.linkedin.common.MemberAttributedEntity")] public LinkedInPerson Person { get; set; }
    }

    public sealed class LinkedInTime
    {
        [JsonPropertyName("time")] public long? Time { get; set; }
    }

    // -------------------------------------------------------
    // Company
    // -------------------------------------------------------
    public sealed class LinkedInCompany : LinkedInEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public LinkedInLocalizedString Name { get; set; }
        [JsonPropertyName("universalName")] public string UniversalName { get; set; }
        [JsonPropertyName("localizedName")] public string LocalizedName { get; set; }
        [JsonPropertyName("description")] public LinkedInLocalizedString Description { get; set; }
        [JsonPropertyName("localizedDescription")] public string LocalizedDescription { get; set; }
        [JsonPropertyName("foundedYear")] public int? FoundedYear { get; set; }
        [JsonPropertyName("locations")] public List<LinkedInLocation> Locations { get; set; } = new();
        [JsonPropertyName("logo")] public LinkedInImage Logo { get; set; }
        [JsonPropertyName("cover")] public LinkedInImage Cover { get; set; }
        [JsonPropertyName("staffCountRange")] public LinkedInStaffCountRange StaffCountRange { get; set; }
        [JsonPropertyName("website")] public string Website { get; set; }
        [JsonPropertyName("industry")] public LinkedInLocalizedString Industry { get; set; }
        [JsonPropertyName("localizedIndustry")] public string LocalizedIndustry { get; set; }
        [JsonPropertyName("specialties")] public List<LinkedInLocalizedString> Specialties { get; set; } = new();
        [JsonPropertyName("localizedSpecialties")] public List<string> LocalizedSpecialties { get; set; } = new();
        [JsonPropertyName("followerCount")] public long? FollowerCount { get; set; }
        [JsonPropertyName("following")] public bool? Following { get; set; }
    }

    public sealed class LinkedInLocalizedString
    {
        [JsonPropertyName("localized")] public Dictionary<string, string> Localized { get; set; } = new();
        [JsonPropertyName("preferredLocale")] public LinkedInLocale PreferredLocale { get; set; }
    }

    public sealed class LinkedInLocale
    {
        [JsonPropertyName("country")] public string Country { get; set; }
        [JsonPropertyName("language")] public string Language { get; set; }
    }

    public sealed class LinkedInLocation
    {
        [JsonPropertyName("countryCode")] public string CountryCode { get; set; }
        [JsonPropertyName("geographicArea")] public string GeographicArea { get; set; }
        [JsonPropertyName("city")] public string City { get; set; }
        [JsonPropertyName("postalCode")] public string PostalCode { get; set; }
        [JsonPropertyName("headquarter")] public bool? Headquarter { get; set; }
        [JsonPropertyName("line1")] public string Line1 { get; set; }
        [JsonPropertyName("line2")] public string Line2 { get; set; }
    }

    public sealed class LinkedInImage
    {
        [JsonPropertyName("displayImage")] public string DisplayImage { get; set; }
        [JsonPropertyName("displayImageOriginal")] public string DisplayImageOriginal { get; set; }
    }

    public sealed class LinkedInStaffCountRange
    {
        [JsonPropertyName("start")] public int? Start { get; set; }
        [JsonPropertyName("end")] public int? End { get; set; }
    }

    // -------------------------------------------------------
    // Person/User
    // -------------------------------------------------------
    public sealed class LinkedInPerson : LinkedInEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("localizedFirstName")] public string LocalizedFirstName { get; set; }
        [JsonPropertyName("localizedLastName")] public string LocalizedLastName { get; set; }
        [JsonPropertyName("firstName")] public LinkedInLocalizedString FirstName { get; set; }
        [JsonPropertyName("lastName")] public LinkedInLocalizedString LastName { get; set; }
        [JsonPropertyName("headline")] public LinkedInLocalizedString Headline { get; set; }
        [JsonPropertyName("localizedHeadline")] public string LocalizedHeadline { get; set; }
        [JsonPropertyName("vanityName")] public string VanityName { get; set; }
        [JsonPropertyName("profilePicture")] public LinkedInImage ProfilePicture { get; set; }
        [JsonPropertyName("backgroundPicture")] public LinkedInImage BackgroundPicture { get; set; }
        [JsonPropertyName("publicProfileUrl")] public string PublicProfileUrl { get; set; }
        [JsonPropertyName("summary")] public LinkedInLocalizedString Summary { get; set; }
        [JsonPropertyName("localizedSummary")] public string LocalizedSummary { get; set; }
        [JsonPropertyName("industry")] public LinkedInLocalizedString Industry { get; set; }
        [JsonPropertyName("localizedIndustry")] public string LocalizedIndustry { get; set; }
        [JsonPropertyName("location")] public LinkedInLocation Location { get; set; }
        [JsonPropertyName("experience")] public List<LinkedInPosition> Experience { get; set; } = new();
        [JsonPropertyName("education")] public List<LinkedInEducation> Education { get; set; } = new();
        [JsonPropertyName("skills")] public List<LinkedInSkill> Skills { get; set; } = new();
        [JsonPropertyName("following")] public LinkedInFollowing Following { get; set; }
        [JsonPropertyName("followerCount")] public long? FollowerCount { get; set; }
    }

    public sealed class LinkedInPosition
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("title")] public LinkedInLocalizedString Title { get; set; }
        [JsonPropertyName("localizedTitle")] public string LocalizedTitle { get; set; }
        [JsonPropertyName("company")] public LinkedInCompany Company { get; set; }
        [JsonPropertyName("location")] public LinkedInLocation Location { get; set; }
        [JsonPropertyName("description")] public LinkedInLocalizedString Description { get; set; }
        [JsonPropertyName("localizedDescription")] public string LocalizedDescription { get; set; }
        [JsonPropertyName("startDate")] public LinkedInDate StartDate { get; set; }
        [JsonPropertyName("endDate")] public LinkedInDate EndDate { get; set; }
        [JsonPropertyName("companyName")] public LinkedInLocalizedString CompanyName { get; set; }
        [JsonPropertyName("localizedCompanyName")] public string LocalizedCompanyName { get; set; }
    }

    public sealed class LinkedInEducation
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("school")] public LinkedInEducationalInstitution School { get; set; }
        [JsonPropertyName("degree")] public LinkedInLocalizedString Degree { get; set; }
        [JsonPropertyName("localizedDegree")] public string LocalizedDegree { get; set; }
        [JsonPropertyName("fieldOfStudy")] public LinkedInLocalizedString FieldOfStudy { get; set; }
        [JsonPropertyName("localizedFieldOfStudy")] public string LocalizedFieldOfStudy { get; set; }
        [JsonPropertyName("startDate")] public LinkedInDate StartDate { get; set; }
        [JsonPropertyName("endDate")] public LinkedInDate EndDate { get; set; }
        [JsonPropertyName("grade")] public string Grade { get; set; }
        [JsonPropertyName("activities")] public string Activities { get; set; }
        [JsonPropertyName("notes")] public string Notes { get; set; }
    }

    public sealed class LinkedInEducationalInstitution
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public LinkedInLocalizedString Name { get; set; }
        [JsonPropertyName("localizedName")] public string LocalizedName { get; set; }
    }

    public sealed class LinkedInSkill
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public LinkedInLocalizedString Name { get; set; }
        [JsonPropertyName("localizedName")] public string LocalizedName { get; set; }
    }

    public sealed class LinkedInFollowing
    {
        [JsonPropertyName("companies")] public LinkedInCompaniesFollowing Companies { get; set; }
        [JsonPropertyName("people")] public LinkedInPeopleFollowing People { get; set; }
        [JsonPropertyName("news")] public LinkedInNewsFollowing News { get; set; }
        [JsonPropertyName("channels")] public LinkedInChannelsFollowing Channels { get; set; }
        [JsonPropertyName("schools")] public LinkedInSchoolsFollowing Schools { get; set; }
    }

    public sealed class LinkedInCompaniesFollowing
    {
        [JsonPropertyName("count")] public long? Count { get; set; }
        [JsonPropertyName("elements")] public List<LinkedInCompany> Elements { get; set; } = new();
    }

    public sealed class LinkedInPeopleFollowing
    {
        [JsonPropertyName("count")] public long? Count { get; set; }
        [JsonPropertyName("elements")] public List<LinkedInPerson> Elements { get; set; } = new();
    }

    public sealed class LinkedInNewsFollowing
    {
        [JsonPropertyName("count")] public long? Count { get; set; }
        [JsonPropertyName("elements")] public List<LinkedInNews> Elements { get; set; } = new();
    }

    public sealed class LinkedInChannelsFollowing
    {
        [JsonPropertyName("count")] public long? Count { get; set; }
        [JsonPropertyName("elements")] public List<LinkedInChannel> Elements { get; set; } = new();
    }

    public sealed class LinkedInSchoolsFollowing
    {
        [JsonPropertyName("count")] public long? Count { get; set; }
        [JsonPropertyName("elements")] public List<LinkedInEducationalInstitution> Elements { get; set; } = new();
    }

    public sealed class LinkedInNews
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("content")] public string Content { get; set; }
    }

    public sealed class LinkedInChannel
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
    }

    public sealed class LinkedInDate
    {
        [JsonPropertyName("year")] public int? Year { get; set; }
        [JsonPropertyName("month")] public int? Month { get; set; }
        [JsonPropertyName("day")] public int? Day { get; set; }
    }

    // -------------------------------------------------------
    // Campaign Management
    // -------------------------------------------------------
    public sealed class LinkedInCampaign : LinkedInEntityBase
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("account")] public string Account { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("campaignGroup")] public string CampaignGroup { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("objectiveType")] public string ObjectiveType { get; set; }
        [JsonPropertyName("creativeSelection")] public string CreativeSelection { get; set; }
        [JsonPropertyName("locale")] public LinkedInLocale Locale { get; set; }
        [JsonPropertyName("runSchedule")] public LinkedInRunSchedule RunSchedule { get; set; }
        [JsonPropertyName("targeting")] public LinkedInTargeting Targeting { get; set; }
        [JsonPropertyName("dailyBudget")] public LinkedInBudget DailyBudget { get; set; }
        [JsonPropertyName("totalBudget")] public LinkedInBudget TotalBudget { get; set; }
        [JsonPropertyName("unitCost")] public LinkedInBudget UnitCost { get; set; }
        [JsonPropertyName("costType")] public string CostType { get; set; }
        [JsonPropertyName("format")] public string Format { get; set; }
        [JsonPropertyName("optimizationTargetType")] public string OptimizationTargetType { get; set; }
        [JsonPropertyName("associatedEntity")] public string AssociatedEntity { get; set; }
        [JsonPropertyName("version")] public LinkedInVersion Version { get; set; }
        [JsonPropertyName("lastModifiedAt")] public long? LastModifiedAt { get; set; }
        [JsonPropertyName("createdAt")] public long? CreatedAt { get; set; }
        [JsonPropertyName("test")] public bool? Test { get; set; }
        [JsonPropertyName("audienceExpansionEnabled")] public bool? AudienceExpansionEnabled { get; set; }
        [JsonPropertyName("offsiteDeliveryEnabled")] public bool? OffsiteDeliveryEnabled { get; set; }
    }

    public sealed class LinkedInRunSchedule
    {
        [JsonPropertyName("start")] public long? Start { get; set; }
        [JsonPropertyName("end")] public long? End { get; set; }
    }

    public sealed class LinkedInTargeting
    {
        [JsonPropertyName("locations")] public List<LinkedInLocation> Locations { get; set; } = new();
        [JsonPropertyName("industries")] public List<string> Industries { get; set; } = new();
        [JsonPropertyName("jobFunctions")] public List<string> JobFunctions { get; set; } = new();
        [JsonPropertyName("seniorities")] public List<string> Seniorities { get; set; } = new();
        [JsonPropertyName("companySizes")] public List<string> CompanySizes { get; set; } = new();
        [JsonPropertyName("degrees")] public List<string> Degrees { get; set; } = new();
        [JsonPropertyName("fieldsOfStudy")] public List<string> FieldsOfStudy { get; set; } = new();
        [JsonPropertyName("genders")] public List<string> Genders { get; set; } = new();
        [JsonPropertyName("ageRanges")] public List<string> AgeRanges { get; set; } = new();
        [JsonPropertyName("languages")] public List<string> Languages { get; set; } = new();
    }

    public sealed class LinkedInBudget
    {
        [JsonPropertyName("amount")] public string Amount { get; set; }
        [JsonPropertyName("currencyCode")] public string CurrencyCode { get; set; }
    }

    public sealed class LinkedInVersion
    {
        [JsonPropertyName("versionTag")] public string VersionTag { get; set; }
    }

    // -------------------------------------------------------
    // Analytics
    // -------------------------------------------------------
    public sealed class LinkedInAnalytics : LinkedInEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("campaign")] public string Campaign { get; set; }
        [JsonPropertyName("creative")] public string Creative { get; set; }
        [JsonPropertyName("dateRange")] public LinkedInDateRange DateRange { get; set; }
        [JsonPropertyName("pivot")] public string Pivot { get; set; }
        [JsonPropertyName("pivotValues")] public List<string> PivotValues { get; set; } = new();
        [JsonPropertyName("metrics")] public LinkedInMetrics Metrics { get; set; }
    }

    public sealed class LinkedInDateRange
    {
        [JsonPropertyName("start")] public LinkedInDate Start { get; set; }
        [JsonPropertyName("end")] public LinkedInDate End { get; set; }
    }

    public sealed class LinkedInMetrics
    {
        [JsonPropertyName("impressions")] public long? Impressions { get; set; }
        [JsonPropertyName("clicks")] public long? Clicks { get; set; }
        [JsonPropertyName("costInLocalCurrency")] public decimal? CostInLocalCurrency { get; set; }
        [JsonPropertyName("costInUsd")] public decimal? CostInUsd { get; set; }
        [JsonPropertyName("conversions")] public long? Conversions { get; set; }
        [JsonPropertyName("externalWebsiteConversions")] public long? ExternalWebsiteConversions { get; set; }
        [JsonPropertyName("externalWebsitePostClickConversions")] public long? ExternalWebsitePostClickConversions { get; set; }
        [JsonPropertyName("externalWebsitePostViewConversions")] public long? ExternalWebsitePostViewConversions { get; set; }
        [JsonPropertyName("follows")] public long? Follows { get; set; }
        [JsonPropertyName("impressionsFrequency")] public decimal? ImpressionsFrequency { get; set; }
        [JsonPropertyName("landingPageClicks")] public long? LandingPageClicks { get; set; }
        [JsonPropertyName("leadGenerationMailContactInfoShares")] public long? LeadGenerationMailContactInfoShares { get; set; }
        [JsonPropertyName("leadGenerationMailInterestedClicks")] public long? LeadGenerationMailInterestedClicks { get; set; }
        [JsonPropertyName("likes")] public long? Likes { get; set; }
        [JsonPropertyName("oneClickLeadFormOpens")] public long? OneClickLeadFormOpens { get; set; }
        [JsonPropertyName("oneClickLeads")] public long? OneClickLeads { get; set; }
        [JsonPropertyName("opens")] public long? Opens { get; set; }
        [JsonPropertyName("reactions")] public long? Reactions { get; set; }
        [JsonPropertyName("sends")] public long? Sends { get; set; }
        [JsonPropertyName("shares")] public long? Shares { get; set; }
        [JsonPropertyName("textUrlClicks")] public long? TextUrlClicks { get; set; }
        [JsonPropertyName("totalEngagements")] public long? TotalEngagements { get; set; }
        [JsonPropertyName("videoCompletions")] public long? VideoCompletions { get; set; }
        [JsonPropertyName("videoFirstQuartileCompletions")] public long? VideoFirstQuartileCompletions { get; set; }
        [JsonPropertyName("videoMidpointCompletions")] public long? VideoMidpointCompletions { get; set; }
        [JsonPropertyName("videoStarts")] public long? VideoStarts { get; set; }
        [JsonPropertyName("videoThirdQuartileCompletions")] public long? VideoThirdQuartileCompletions { get; set; }
        [JsonPropertyName("videoViews")] public long? VideoViews { get; set; }
        [JsonPropertyName("viralClicks")] public long? ViralClicks { get; set; }
        [JsonPropertyName("viralExternalWebsiteConversions")] public long? ViralExternalWebsiteConversions { get; set; }
        [JsonPropertyName("viralImpressions")] public long? ViralImpressions { get; set; }
        [JsonPropertyName("viralLandingPageClicks")] public long? ViralLandingPageClicks { get; set; }
        [JsonPropertyName("viralLikes")] public long? ViralLikes { get; set; }
        [JsonPropertyName("viralOneClickLeadFormOpens")] public long? ViralOneClickLeadFormOpens { get; set; }
        [JsonPropertyName("viralOneClickLeads")] public long? ViralOneClickLeads { get; set; }
        [JsonPropertyName("viralReactions")] public long? ViralReactions { get; set; }
        [JsonPropertyName("viralShares")] public long? ViralShares { get; set; }
        [JsonPropertyName("viralTotalEngagements")] public long? ViralTotalEngagements { get; set; }
        [JsonPropertyName("viralVideoCompletions")] public long? ViralVideoCompletions { get; set; }
        [JsonPropertyName("viralVideoFirstQuartileCompletions")] public long? ViralVideoFirstQuartileCompletions { get; set; }
        [JsonPropertyName("viralVideoMidpointCompletions")] public long? ViralVideoMidpointCompletions { get; set; }
        [JsonPropertyName("viralVideoStarts")] public long? ViralVideoStarts { get; set; }
        [JsonPropertyName("viralVideoThirdQuartileCompletions")] public long? ViralVideoThirdQuartileCompletions { get; set; }
        [JsonPropertyName("viralVideoViews")] public long? ViralVideoViews { get; set; }
    }

    // -------------------------------------------------------
    // Creative
    // -------------------------------------------------------
    public sealed class LinkedInCreative : LinkedInEntityBase
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("campaign")] public string Campaign { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; }
        [JsonPropertyName("intendedStatus")] public string IntendedStatus { get; set; }
        [JsonPropertyName("lastModifiedAt")] public long? LastModifiedAt { get; set; }
        [JsonPropertyName("createdAt")] public long? CreatedAt { get; set; }
        [JsonPropertyName("version")] public LinkedInVersion Version { get; set; }
        [JsonPropertyName("servingHoldReasons")] public List<string> ServingHoldReasons { get; set; } = new();
        [JsonPropertyName("review")] public LinkedInReview Review { get; set; }
        [JsonPropertyName("variables")] public LinkedInCreativeVariables Variables { get; set; }
    }

    public sealed class LinkedInReview
    {
        [JsonPropertyName("status")] public string Status { get; set; }
        [JsonPropertyName("rejectionReasons")] public List<string> RejectionReasons { get; set; } = new();
    }

    public sealed class LinkedInCreativeVariables
    {
        [JsonPropertyName("data")] public LinkedInCreativeData Data { get; set; }
    }

    public sealed class LinkedInCreativeData
    {
        [JsonPropertyName("com.linkedin.ads.TextAdCreativeVariables")] public LinkedInTextAdVariables TextAd { get; set; }
        [JsonPropertyName("com.linkedin.ads.SponsoredMessageCreativeVariables")] public LinkedInSponsoredMessageVariables SponsoredMessage { get; set; }
        [JsonPropertyName("com.linkedin.ads.DynamicCreativeVariables")] public LinkedInDynamicVariables Dynamic { get; set; }
    }

    public sealed class LinkedInTextAdVariables
    {
        [JsonPropertyName("text")] public string Text { get; set; }
        [JsonPropertyName("headline")] public string Headline { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("landingPage")] public LinkedInLandingPage LandingPage { get; set; }
    }

    public sealed class LinkedInSponsoredMessageVariables
    {
        [JsonPropertyName("page")] public string Page { get; set; }
        [JsonPropertyName("message")] public string Message { get; set; }
        [JsonPropertyName("callToAction")] public LinkedInCallToAction CallToAction { get; set; }
    }

    public sealed class LinkedInDynamicVariables
    {
        [JsonPropertyName("dynamicContent")] public LinkedInDynamicContent DynamicContent { get; set; }
    }

    public sealed class LinkedInLandingPage
    {
        [JsonPropertyName("url")] public string Url { get; set; }
    }

    public sealed class LinkedInCallToAction
    {
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("url")] public string Url { get; set; }
    }

    public sealed class LinkedInDynamicContent
    {
        [JsonPropertyName("dynamicContentItems")] public List<LinkedInDynamicContentItem> Items { get; set; } = new();
    }

    public sealed class LinkedInDynamicContentItem
    {
        [JsonPropertyName("content")] public string Content { get; set; }
        [JsonPropertyName("locale")] public LinkedInLocale Locale { get; set; }
    }

    // -------------------------------------------------------
    // Account
    // -------------------------------------------------------
    public sealed class LinkedInAccount : LinkedInEntityBase
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; }
        [JsonPropertyName("currency")] public string Currency { get; set; }
        [JsonPropertyName("notifiedOnCampaignOptimization")] public bool? NotifiedOnCampaignOptimization { get; set; }
        [JsonPropertyName("notifiedOnCreativeApproval")] public bool? NotifiedOnCreativeApproval { get; set; }
        [JsonPropertyName("notifiedOnCreativeRejection")] public bool? NotifiedOnCreativeRejection { get; set; }
        [JsonPropertyName("notifiedOnEndOfCampaign")] public bool? NotifiedOnEndOfCampaign { get; set; }
        [JsonPropertyName("reference")] public string Reference { get; set; }
        [JsonPropertyName("version")] public LinkedInVersion Version { get; set; }
        [JsonPropertyName("lastModifiedAt")] public long? LastModifiedAt { get; set; }
        [JsonPropertyName("createdAt")] public long? CreatedAt { get; set; }
        [JsonPropertyName("test")] public bool? Test { get; set; }
    }

    // -------------------------------------------------------
    // Campaign Group
    // -------------------------------------------------------
    public sealed class LinkedInCampaignGroup : LinkedInEntityBase
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("account")] public string Account { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; }
        [JsonPropertyName("runSchedule")] public LinkedInRunSchedule RunSchedule { get; set; }
        [JsonPropertyName("totalBudget")] public LinkedInBudget TotalBudget { get; set; }
        [JsonPropertyName("backfilled")] public bool? Backfilled { get; set; }
        [JsonPropertyName("servingStatuses")] public List<string> ServingStatuses { get; set; } = new();
        [JsonPropertyName("version")] public LinkedInVersion Version { get; set; }
        [JsonPropertyName("lastModifiedAt")] public long? LastModifiedAt { get; set; }
        [JsonPropertyName("createdAt")] public long? CreatedAt { get; set; }
        [JsonPropertyName("test")] public bool? Test { get; set; }
    }

    // -------------------------------------------------------
    // Video
    // -------------------------------------------------------
    public sealed class LinkedInVideo : LinkedInEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("owner")] public string Owner { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("duration")] public long? Duration { get; set; }
        [JsonPropertyName("thumbnail")] public string Thumbnail { get; set; }
        [JsonPropertyName("downloadUrls")] public LinkedInDownloadUrls DownloadUrls { get; set; }
        [JsonPropertyName("content")] public LinkedInVideoContent Content { get; set; }
        [JsonPropertyName("recipes")] public List<LinkedInRecipe> Recipes { get; set; } = new();
        [JsonPropertyName("createdAt")] public long? CreatedAt { get; set; }
        [JsonPropertyName("publishedAt")] public long? PublishedAt { get; set; }
    }

    public sealed class LinkedInDownloadUrls
    {
        [JsonPropertyName("downloadUrl")] public string DownloadUrl { get; set; }
        [JsonPropertyName("progressiveDownloadUrl")] public string ProgressiveDownloadUrl { get; set; }
    }

    public sealed class LinkedInVideoContent
    {
        [JsonPropertyName("bitRate")] public long? BitRate { get; set; }
        [JsonPropertyName("fileSize")] public long? FileSize { get; set; }
        [JsonPropertyName("height")] public int? Height { get; set; }
        [JsonPropertyName("width")] public int? Width { get; set; }
        [JsonPropertyName("mediaType")] public string MediaType { get; set; }
    }

    public sealed class LinkedInRecipe
    {
        [JsonPropertyName("recipe")] public string Recipe { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; }
        [JsonPropertyName("downloadUrl")] public string DownloadUrl { get; set; }
    }

    // -------------------------------------------------------
    // Organization
    // -------------------------------------------------------
    public sealed class LinkedInOrganization : LinkedInEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("localizedName")] public string LocalizedName { get; set; }
        [JsonPropertyName("name")] public LinkedInLocalizedString Name { get; set; }
        [JsonPropertyName("vanityName")] public string VanityName { get; set; }
        [JsonPropertyName("localizedWebsite")] public string LocalizedWebsite { get; set; }
        [JsonPropertyName("website")] public LinkedInLocalizedString Website { get; set; }
        [JsonPropertyName("localizedDescription")] public string LocalizedDescription { get; set; }
        [JsonPropertyName("description")] public LinkedInLocalizedString Description { get; set; }
        [JsonPropertyName("logo")] public LinkedInImage Logo { get; set; }
        [JsonPropertyName("cover")] public LinkedInImage Cover { get; set; }
        [JsonPropertyName("locations")] public List<LinkedInLocation> Locations { get; set; } = new();
        [JsonPropertyName("staffCountRange")] public LinkedInStaffCountRange StaffCountRange { get; set; }
        [JsonPropertyName("foundedYear")] public int? FoundedYear { get; set; }
        [JsonPropertyName("industry")] public LinkedInLocalizedString Industry { get; set; }
        [JsonPropertyName("localizedIndustry")] public string LocalizedIndustry { get; set; }
        [JsonPropertyName("specialties")] public List<LinkedInLocalizedString> Specialties { get; set; } = new();
        [JsonPropertyName("localizedSpecialties")] public List<string> LocalizedSpecialties { get; set; } = new();
        [JsonPropertyName("followerCount")] public long? FollowerCount { get; set; }
        [JsonPropertyName("following")] public bool? Following { get; set; }
        [JsonPropertyName("pages")] public List<LinkedInCompany> Pages { get; set; } = new();
    }
}