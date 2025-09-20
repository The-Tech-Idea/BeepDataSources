using System.Text.Json.Serialization;

namespace TheTechIdea.Beep.Connectors.Marketing.MarketoDataSource.Models
{
    // Lead Models
    public class MarketoLead
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("firstName")]
        public string? FirstName { get; set; }

        [JsonPropertyName("lastName")]
        public string? LastName { get; set; }

        [JsonPropertyName("company")]
        public string? Company { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("mobilePhone")]
        public string? MobilePhone { get; set; }

        [JsonPropertyName("fax")]
        public string? Fax { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("postalCode")]
        public string? PostalCode { get; set; }

        [JsonPropertyName("dateOfBirth")]
        public DateTime? DateOfBirth { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("isLead")]
        public bool? IsLead { get; set; }

        [JsonPropertyName("leadScore")]
        public int? LeadScore { get; set; }

        [JsonPropertyName("leadStatus")]
        public string? LeadStatus { get; set; }

        [JsonPropertyName("leadSource")]
        public string? LeadSource { get; set; }

        [JsonPropertyName("leadPartitionId")]
        public int? LeadPartitionId { get; set; }

        [JsonPropertyName("acquisitionDate")]
        public DateTime? AcquisitionDate { get; set; }

        [JsonPropertyName("lastInterestingMomentDate")]
        public DateTime? LastInterestingMomentDate { get; set; }

        [JsonPropertyName("lastInterestingMomentDesc")]
        public string? LastInterestingMomentDesc { get; set; }

        [JsonPropertyName("lastInterestingMomentSource")]
        public string? LastInterestingMomentSource { get; set; }

        [JsonPropertyName("lastInterestingMomentType")]
        public string? LastInterestingMomentType { get; set; }

        [JsonPropertyName("leadRevenueCycleModelId")]
        public int? LeadRevenueCycleModelId { get; set; }

        [JsonPropertyName("leadRevenueStage")]
        public string? LeadRevenueStage { get; set; }

        [JsonPropertyName("priority")]
        public int? Priority { get; set; }

        [JsonPropertyName("relativeScore")]
        public int? RelativeScore { get; set; }

        [JsonPropertyName("relativeUrgency")]
        public int? RelativeUrgency { get; set; }

        [JsonPropertyName("urgency")]
        public int? Urgency { get; set; }

        [JsonPropertyName("website")]
        public string? Website { get; set; }

        [JsonPropertyName("facebookDisplayName")]
        public string? FacebookDisplayName { get; set; }

        [JsonPropertyName("facebookId")]
        public string? FacebookId { get; set; }

        [JsonPropertyName("facebookPhotoURL")]
        public string? FacebookPhotoURL { get; set; }

        [JsonPropertyName("facebookProfileURL")]
        public string? FacebookProfileURL { get; set; }

        [JsonPropertyName("facebookReach")]
        public int? FacebookReach { get; set; }

        [JsonPropertyName("facebookReferredEnrollments")]
        public int? FacebookReferredEnrollments { get; set; }

        [JsonPropertyName("facebookReferredVisits")]
        public int? FacebookReferredVisits { get; set; }

        [JsonPropertyName("gender")]
        public string? Gender { get; set; }

        [JsonPropertyName("inferredCompany")]
        public string? InferredCompany { get; set; }

        [JsonPropertyName("inferredCountry")]
        public string? InferredCountry { get; set; }

        [JsonPropertyName("industry")]
        public string? Industry { get; set; }

        [JsonPropertyName("linkedinDisplayName")]
        public string? LinkedinDisplayName { get; set; }

        [JsonPropertyName("linkedinId")]
        public string? LinkedinId { get; set; }

        [JsonPropertyName("linkedinPhotoURL")]
        public string? LinkedinPhotoURL { get; set; }

        [JsonPropertyName("linkedinProfileURL")]
        public string? LinkedinProfileURL { get; set; }

        [JsonPropertyName("numberOfEmployees")]
        public int? NumberOfEmployees { get; set; }

        [JsonPropertyName("sicCode")]
        public string? SicCode { get; set; }

        [JsonPropertyName("site")]
        public string? Site { get; set; }

        [JsonPropertyName("twitterDisplayName")]
        public string? TwitterDisplayName { get; set; }

        [JsonPropertyName("twitterId")]
        public string? TwitterId { get; set; }

        [JsonPropertyName("twitterPhotoURL")]
        public string? TwitterPhotoURL { get; set; }

        [JsonPropertyName("twitterProfileURL")]
        public string? TwitterProfileURL { get; set; }

        [JsonPropertyName("unsubscribed")]
        public bool? Unsubscribed { get; set; }

        [JsonPropertyName("unsubscribedReason")]
        public string? UnsubscribedReason { get; set; }
    }

    // List Models
    public class MarketoList
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("programName")]
        public string? ProgramName { get; set; }

        [JsonPropertyName("workspaceName")]
        public string? WorkspaceName { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("folder")]
        public MarketoFolder? Folder { get; set; }

        [JsonPropertyName("computedUrl")]
        public string? ComputedUrl { get; set; }
    }

    // Smart List Models
    public class MarketoSmartList
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("programName")]
        public string? ProgramName { get; set; }

        [JsonPropertyName("workspaceName")]
        public string? WorkspaceName { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("folder")]
        public MarketoFolder? Folder { get; set; }

        [JsonPropertyName("computedUrl")]
        public string? ComputedUrl { get; set; }

        [JsonPropertyName("smartListId")]
        public long? SmartListId { get; set; }
    }

    // Campaign Models
    public class MarketoCampaign
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("programName")]
        public string? ProgramName { get; set; }

        [JsonPropertyName("programId")]
        public long? ProgramId { get; set; }

        [JsonPropertyName("workspaceName")]
        public string? WorkspaceName { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("active")]
        public bool? Active { get; set; }

        [JsonPropertyName("folder")]
        public MarketoFolder? Folder { get; set; }

        [JsonPropertyName("computedUrl")]
        public string? ComputedUrl { get; set; }
    }

    // Program Models
    public class MarketoProgram
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("channel")]
        public string? Channel { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("workspaceName")]
        public string? WorkspaceName { get; set; }

        [JsonPropertyName("tags")]
        public List<MarketoTag>? Tags { get; set; }

        [JsonPropertyName("costs")]
        public List<MarketoCost>? Costs { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("folder")]
        public MarketoFolder? Folder { get; set; }

        [JsonPropertyName("computedUrl")]
        public string? ComputedUrl { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("sfdcId")]
        public string? SfdcId { get; set; }

        [JsonPropertyName("sfdcName")]
        public string? SfdcName { get; set; }
    }

    // Email Template Models
    public class MarketoEmailTemplate
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("workspaceName")]
        public string? WorkspaceName { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("folder")]
        public MarketoFolder? Folder { get; set; }

        [JsonPropertyName("computedUrl")]
        public string? ComputedUrl { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("version")]
        public int? Version { get; set; }
    }

    // Email Models
    public class MarketoEmail
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("subject")]
        public MarketoEmailSubject? Subject { get; set; }

        [JsonPropertyName("fromEmail")]
        public MarketoEmailFrom? FromEmail { get; set; }

        [JsonPropertyName("fromName")]
        public MarketoEmailFrom? FromName { get; set; }

        [JsonPropertyName("replyEmail")]
        public MarketoEmailFrom? ReplyEmail { get; set; }

        [JsonPropertyName("template")]
        public long? Template { get; set; }

        [JsonPropertyName("workspaceName")]
        public string? WorkspaceName { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("textOnly")]
        public bool? TextOnly { get; set; }

        [JsonPropertyName("webView")]
        public bool? WebView { get; set; }

        [JsonPropertyName("autoCopyToText")]
        public bool? AutoCopyToText { get; set; }

        [JsonPropertyName("operational")]
        public bool? Operational { get; set; }

        [JsonPropertyName("publishToMSI")]
        public bool? PublishToMSI { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("folder")]
        public MarketoFolder? Folder { get; set; }

        [JsonPropertyName("computedUrl")]
        public string? ComputedUrl { get; set; }

        [JsonPropertyName("version")]
        public int? Version { get; set; }

        [JsonPropertyName("bccFields")]
        public string? BccFields { get; set; }

        [JsonPropertyName("preHeader")]
        public string? PreHeader { get; set; }

        [JsonPropertyName("autoSaveToText")]
        public bool? AutoSaveToText { get; set; }
    }

    // Landing Page Models
    public class MarketoLandingPage
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("workspaceName")]
        public string? WorkspaceName { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("template")]
        public long? Template { get; set; }

        [JsonPropertyName("robots")]
        public string? Robots { get; set; }

        [JsonPropertyName("formPrefill")]
        public bool? FormPrefill { get; set; }

        [JsonPropertyName("mobileEnabled")]
        public bool? MobileEnabled { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("folder")]
        public MarketoFolder? Folder { get; set; }

        [JsonPropertyName("computedUrl")]
        public string? ComputedUrl { get; set; }

        [JsonPropertyName("URL")]
        public string? URL { get; set; }

        [JsonPropertyName("version")]
        public int? Version { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("keywords")]
        public string? Keywords { get; set; }

        [JsonPropertyName("facebookOgTags")]
        public MarketoFacebookTags? FacebookOgTags { get; set; }
    }

    // Form Models
    public class MarketoForm
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("theme")]
        public string? Theme { get; set; }

        [JsonPropertyName("language")]
        public string? Language { get; set; }

        [JsonPropertyName("locale")]
        public string? Locale { get; set; }

        [JsonPropertyName("progressiveProfiling")]
        public bool? ProgressiveProfiling { get; set; }

        [JsonPropertyName("labelPosition")]
        public string? LabelPosition { get; set; }

        [JsonPropertyName("fontFamily")]
        public string? FontFamily { get; set; }

        [JsonPropertyName("fontSize")]
        public string? FontSize { get; set; }

        [JsonPropertyName("folder")]
        public MarketoFolder? Folder { get; set; }

        [JsonPropertyName("knownVisitor")]
        public MarketoKnownVisitor? KnownVisitor { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("computedUrl")]
        public string? ComputedUrl { get; set; }
    }

    // Opportunity Models
    public class MarketoOpportunity
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("externalOpportunityId")]
        public string? ExternalOpportunityId { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("amount")]
        public decimal? Amount { get; set; }

        [JsonPropertyName("source")]
        public string? Source { get; set; }

        [JsonPropertyName("stage")]
        public string? Stage { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("leadSource")]
        public string? LeadSource { get; set; }

        [JsonPropertyName("isClosed")]
        public bool? IsClosed { get; set; }

        [JsonPropertyName("isWon")]
        public bool? IsWon { get; set; }

        [JsonPropertyName("forecastCategory")]
        public string? ForecastCategory { get; set; }

        [JsonPropertyName("fiscalQuarter")]
        public int? FiscalQuarter { get; set; }

        [JsonPropertyName("fiscalYear")]
        public int? FiscalYear { get; set; }

        [JsonPropertyName("closeDate")]
        public DateTime? CloseDate { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("externalCreatedDate")]
        public DateTime? ExternalCreatedDate { get; set; }

        [JsonPropertyName("externalSalesPersonId")]
        public string? ExternalSalesPersonId { get; set; }

        [JsonPropertyName("accountId")]
        public long? AccountId { get; set; }

        [JsonPropertyName("externalAccountId")]
        public string? ExternalAccountId { get; set; }
    }

    // Company Models
    public class MarketoCompany
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("externalCompanyId")]
        public string? ExternalCompanyId { get; set; }

        [JsonPropertyName("company")]
        public string? Company { get; set; }

        [JsonPropertyName("website")]
        public string? Website { get; set; }

        [JsonPropertyName("mainPhone")]
        public string? MainPhone { get; set; }

        [JsonPropertyName("billingStreet")]
        public string? BillingStreet { get; set; }

        [JsonPropertyName("billingCity")]
        public string? BillingCity { get; set; }

        [JsonPropertyName("billingState")]
        public string? BillingState { get; set; }

        [JsonPropertyName("billingCountry")]
        public string? BillingCountry { get; set; }

        [JsonPropertyName("billingPostalCode")]
        public string? BillingPostalCode { get; set; }

        [JsonPropertyName("annualRevenue")]
        public decimal? AnnualRevenue { get; set; }

        [JsonPropertyName("numberOfEmployees")]
        public int? NumberOfEmployees { get; set; }

        [JsonPropertyName("industry")]
        public string? Industry { get; set; }

        [JsonPropertyName("sicCode")]
        public string? SicCode { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("externalCreatedDate")]
        public DateTime? ExternalCreatedDate { get; set; }
    }

    // Custom Object Models
    public class MarketoCustomObject
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("apiName")]
        public string? ApiName { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("pluralName")]
        public string? PluralName { get; set; }

        [JsonPropertyName("showInLeadDetail")]
        public bool? ShowInLeadDetail { get; set; }

        [JsonPropertyName("relationships")]
        public List<MarketoRelationship>? Relationships { get; set; }

        [JsonPropertyName("fields")]
        public List<MarketoField>? Fields { get; set; }
    }

    // Segment Models
    public class MarketoSegment
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("folder")]
        public MarketoFolder? Folder { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("workspaceName")]
        public string? WorkspaceName { get; set; }
    }

    // Folder Models
    public class MarketoFolder
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("folderId")]
        public MarketoFolderId? FolderId { get; set; }

        [JsonPropertyName("folderType")]
        public string? FolderType { get; set; }

        [JsonPropertyName("parent")]
        public MarketoFolderId? Parent { get; set; }

        [JsonPropertyName("path")]
        public string? Path { get; set; }

        [JsonPropertyName("isArchive")]
        public bool? IsArchive { get; set; }

        [JsonPropertyName("isSystem")]
        public bool? IsSystem { get; set; }

        [JsonPropertyName("accessZoneId")]
        public int? AccessZoneId { get; set; }

        [JsonPropertyName("workspace")]
        public string? Workspace { get; set; }
    }

    // Activity Models
    public class MarketoActivity
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("leadId")]
        public long LeadId { get; set; }

        [JsonPropertyName("activityDate")]
        public DateTime ActivityDate { get; set; }

        [JsonPropertyName("activityTypeId")]
        public int ActivityTypeId { get; set; }

        [JsonPropertyName("primaryAttributeValueId")]
        public long? PrimaryAttributeValueId { get; set; }

        [JsonPropertyName("primaryAttributeValue")]
        public string? PrimaryAttributeValue { get; set; }

        [JsonPropertyName("attributes")]
        public List<MarketoActivityAttribute>? Attributes { get; set; }

        [JsonPropertyName("campaignId")]
        public long? CampaignId { get; set; }

        [JsonPropertyName("apiName")]
        public string? ApiName { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    // Supporting Models
    public class MarketoTag
    {
        [JsonPropertyName("tagType")]
        public string? TagType { get; set; }

        [JsonPropertyName("tagValue")]
        public string? TagValue { get; set; }
    }

    public class MarketoCost
    {
        [JsonPropertyName("costDate")]
        public DateTime? CostDate { get; set; }

        [JsonPropertyName("amount")]
        public decimal? Amount { get; set; }

        [JsonPropertyName("note")]
        public string? Note { get; set; }

        [JsonPropertyName("programId")]
        public long? ProgramId { get; set; }

        [JsonPropertyName("costId")]
        public long? CostId { get; set; }
    }

    public class MarketoEmailSubject
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("value")]
        public string? Value { get; set; }
    }

    public class MarketoEmailFrom
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("value")]
        public string? Value { get; set; }
    }

    public class MarketoFacebookTags
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("image")]
        public string? Image { get; set; }

        [JsonPropertyName("siteName")]
        public string? SiteName { get; set; }
    }

    public class MarketoKnownVisitor
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("template")]
        public string? Template { get; set; }

        [JsonPropertyName("value")]
        public string? Value { get; set; }
    }

    public class MarketoRelationship
    {
        [JsonPropertyName("field")]
        public string? Field { get; set; }

        [JsonPropertyName("relatedTo")]
        public MarketoRelatedTo? RelatedTo { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }

    public class MarketoRelatedTo
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("apiName")]
        public string? ApiName { get; set; }
    }

    public class MarketoField
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("dataType")]
        public string? DataType { get; set; }

        [JsonPropertyName("length")]
        public int? Length { get; set; }

        [JsonPropertyName("isPrimary")]
        public bool? IsPrimary { get; set; }

        [JsonPropertyName("isCustom")]
        public bool? IsCustom { get; set; }

        [JsonPropertyName("isRequired")]
        public bool? IsRequired { get; set; }

        [JsonPropertyName("isUpdateable")]
        public bool? IsUpdateable { get; set; }
    }

    public class MarketoFolderId
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }

    public class MarketoActivityAttribute
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("value")]
        public string? Value { get; set; }
    }

    public class MarketoActivityType
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("primaryAttribute")]
        public MarketoActivityAttribute? PrimaryAttribute { get; set; }

        [JsonPropertyName("attributes")]
        public List<MarketoActivityAttribute>? Attributes { get; set; }
    }

    public class MarketoStats
    {
        [JsonPropertyName("requestCount")]
        public int? RequestCount { get; set; }

        [JsonPropertyName("remaining")]
        public int? Remaining { get; set; }

        [JsonPropertyName("resetDate")]
        public DateTime? ResetDate { get; set; }

        [JsonPropertyName("expiresIn")]
        public int? ExpiresIn { get; set; }
    }

    public class MarketoUsage
    {
        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("dailyQuota")]
        public int? DailyQuota { get; set; }

        [JsonPropertyName("dailyUsed")]
        public int? DailyUsed { get; set; }

        [JsonPropertyName("hourlyQuota")]
        public int? HourlyQuota { get; set; }

        [JsonPropertyName("hourlyUsed")]
        public int? HourlyUsed { get; set; }

        [JsonPropertyName("apiName")]
        public string? ApiName { get; set; }
    }
}