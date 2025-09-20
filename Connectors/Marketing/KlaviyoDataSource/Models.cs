// File: BeepDM/Connectors/Marketing/KlaviyoDataSource/Models/KlaviyoModels.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Marketing.KlaviyoDataSource.Models
{
    // Base
    public abstract class KlaviyoEntityBase
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("created")] public DateTimeOffset? Created { get; set; }
        [JsonPropertyName("updated")] public DateTimeOffset? Updated { get; set; }

        [JsonIgnore] public IDataSource? DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : KlaviyoEntityBase { DataSource = ds; return (T)this; }
    }

    // ---------- Profile objects ----------

    public sealed class KlaviyoProfile : KlaviyoEntityBase
    {
        [JsonPropertyName("attributes")] public KlaviyoProfileAttributes? Attributes { get; set; }
        [JsonPropertyName("relationships")] public KlaviyoProfileRelationships? Relationships { get; set; }
        [JsonPropertyName("links")] public KlaviyoLinks? Links { get; set; }
    }

    public sealed class KlaviyoProfileAttributes
    {
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("phone_number")] public string? PhoneNumber { get; set; }
        [JsonPropertyName("external_id")] public string? ExternalId { get; set; }
        [JsonPropertyName("anonymous_id")] public string? AnonymousId { get; set; }
        [JsonPropertyName("first_name")] public string? FirstName { get; set; }
        [JsonPropertyName("last_name")] public string? LastName { get; set; }
        [JsonPropertyName("organization")] public string? Organization { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("image")] public string? Image { get; set; }
        [JsonPropertyName("location")] public KlaviyoLocation? Location { get; set; }
        [JsonPropertyName("properties")] public Dictionary<string, object>? Properties { get; set; }
    }

    public sealed class KlaviyoLocation
    {
        [JsonPropertyName("address1")] public string? Address1 { get; set; }
        [JsonPropertyName("address2")] public string? Address2 { get; set; }
        [JsonPropertyName("city")] public string? City { get; set; }
        [JsonPropertyName("country")] public string? Country { get; set; }
        [JsonPropertyName("latitude")] public double? Latitude { get; set; }
        [JsonPropertyName("longitude")] public double? Longitude { get; set; }
        [JsonPropertyName("region")] public string? Region { get; set; }
        [JsonPropertyName("zip")] public string? Zip { get; set; }
        [JsonPropertyName("timezone")] public string? Timezone { get; set; }
    }

    public sealed class KlaviyoProfileRelationships
    {
        [JsonPropertyName("lists")] public KlaviyoRelationshipData? Lists { get; set; }
        [JsonPropertyName("segments")] public KlaviyoRelationshipData? Segments { get; set; }
    }

    public sealed class KlaviyoRelationshipData
    {
        [JsonPropertyName("data")] public List<KlaviyoRelationshipItem>? Data { get; set; } = new();
        [JsonPropertyName("links")] public KlaviyoLinks? Links { get; set; }
    }

    public sealed class KlaviyoRelationshipItem
    {
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("id")] public string? Id { get; set; }
    }

    public sealed class KlaviyoLinks
    {
        [JsonPropertyName("self")] public string? Self { get; set; }
        [JsonPropertyName("related")] public string? Related { get; set; }
    }

    // ---------- List objects ----------

    public sealed class KlaviyoList : KlaviyoEntityBase
    {
        [JsonPropertyName("attributes")] public KlaviyoListAttributes? Attributes { get; set; }
        [JsonPropertyName("relationships")] public KlaviyoListRelationships? Relationships { get; set; }
        [JsonPropertyName("links")] public KlaviyoLinks? Links { get; set; }
    }

    public sealed class KlaviyoListAttributes
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("created")] public DateTimeOffset? Created { get; set; }
        [JsonPropertyName("updated")] public DateTimeOffset? Updated { get; set; }
        [JsonPropertyName("opt_in_process")] public string? OptInProcess { get; set; }
        [JsonPropertyName("list_type")] public string? ListType { get; set; }
        [JsonPropertyName("folder")] public string? Folder { get; set; }
    }

    public sealed class KlaviyoListRelationships
    {
        [JsonPropertyName("profiles")] public KlaviyoRelationshipData? Profiles { get; set; }
        [JsonPropertyName("tags")] public KlaviyoRelationshipData? Tags { get; set; }
    }

    // ---------- Segment objects ----------

    public sealed class KlaviyoSegment : KlaviyoEntityBase
    {
        [JsonPropertyName("attributes")] public KlaviyoSegmentAttributes? Attributes { get; set; }
        [JsonPropertyName("relationships")] public KlaviyoSegmentRelationships? Relationships { get; set; }
        [JsonPropertyName("links")] public KlaviyoLinks? Links { get; set; }
    }

    public sealed class KlaviyoSegmentAttributes
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("created")] public DateTimeOffset? Created { get; set; }
        [JsonPropertyName("updated")] public DateTimeOffset? Updated { get; set; }
        [JsonPropertyName("definition")] public KlaviyoSegmentDefinition? Definition { get; set; }
        [JsonPropertyName("is_active")] public bool? IsActive { get; set; }
        [JsonPropertyName("is_processing")] public bool? IsProcessing { get; set; }
        [JsonPropertyName("is_starred")] public bool? IsStarred { get; set; }
    }

    public sealed class KlaviyoSegmentDefinition
    {
        [JsonPropertyName("conditions")] public List<KlaviyoSegmentCondition>? Conditions { get; set; } = new();
        [JsonPropertyName("included_filters")] public List<KlaviyoSegmentFilter>? IncludedFilters { get; set; } = new();
        [JsonPropertyName("excluded_filters")] public List<KlaviyoSegmentFilter>? ExcludedFilters { get; set; } = new();
    }

    public sealed class KlaviyoSegmentCondition
    {
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("definition")] public Dictionary<string, object>? Definition { get; set; }
    }

    public sealed class KlaviyoSegmentFilter
    {
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("field")] public string? Field { get; set; }
        [JsonPropertyName("operator")] public string? Operator { get; set; }
        [JsonPropertyName("values")] public List<string>? Values { get; set; } = new();
    }

    public sealed class KlaviyoSegmentRelationships
    {
        [JsonPropertyName("profiles")] public KlaviyoRelationshipData? Profiles { get; set; }
        [JsonPropertyName("tags")] public KlaviyoRelationshipData? Tags { get; set; }
    }

    // ---------- Campaign objects ----------

    public sealed class KlaviyoCampaign : KlaviyoEntityBase
    {
        [JsonPropertyName("attributes")] public KlaviyoCampaignAttributes? Attributes { get; set; }
        [JsonPropertyName("relationships")] public KlaviyoCampaignRelationships? Relationships { get; set; }
        [JsonPropertyName("links")] public KlaviyoLinks? Links { get; set; }
    }

    public sealed class KlaviyoCampaignAttributes
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("archived")] public bool? Archived { get; set; }
        [JsonPropertyName("audiences")] public KlaviyoCampaignAudiences? Audiences { get; set; }
        [JsonPropertyName("send_options")] public KlaviyoSendOptions? SendOptions { get; set; }
        [JsonPropertyName("tracking_options")] public KlaviyoTrackingOptions? TrackingOptions { get; set; }
        [JsonPropertyName("send_time")] public DateTimeOffset? SendTime { get; set; }
        [JsonPropertyName("created")] public DateTimeOffset? Created { get; set; }
        [JsonPropertyName("scheduled")] public DateTimeOffset? Scheduled { get; set; }
        [JsonPropertyName("updated")] public DateTimeOffset? Updated { get; set; }
    }

    public sealed class KlaviyoCampaignAudiences
    {
        [JsonPropertyName("included")] public List<string>? Included { get; set; } = new();
        [JsonPropertyName("excluded")] public List<string>? Excluded { get; set; } = new();
    }

    public sealed class KlaviyoSendOptions
    {
        [JsonPropertyName("use_smart_sending")] public bool? UseSmartSending { get; set; }
        [JsonPropertyName("ignore_unsubscribes")] public bool? IgnoreUnsubscribes { get; set; }
    }

    public sealed class KlaviyoTrackingOptions
    {
        [JsonPropertyName("is_tracking_opens")] public bool? IsTrackingOpens { get; set; }
        [JsonPropertyName("is_tracking_clicks")] public bool? IsTrackingClicks { get; set; }
        [JsonPropertyName("is_adding_utm")] public bool? IsAddingUtm { get; set; }
        [JsonPropertyName("utm_params")] public List<KlaviyoUtmParam>? UtmParams { get; set; } = new();
    }

    public sealed class KlaviyoUtmParam
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("value")] public string? Value { get; set; }
    }

    public sealed class KlaviyoCampaignRelationships
    {
        [JsonPropertyName("campaign-messages")] public KlaviyoRelationshipData? CampaignMessages { get; set; }
        [JsonPropertyName("tags")] public KlaviyoRelationshipData? Tags { get; set; }
    }

    // ---------- Flow objects ----------

    public sealed class KlaviyoFlow : KlaviyoEntityBase
    {
        [JsonPropertyName("attributes")] public KlaviyoFlowAttributes? Attributes { get; set; }
        [JsonPropertyName("relationships")] public KlaviyoFlowRelationships? Relationships { get; set; }
        [JsonPropertyName("links")] public KlaviyoLinks? Links { get; set; }
    }

    public sealed class KlaviyoFlowAttributes
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("archived")] public bool? Archived { get; set; }
        [JsonPropertyName("created")] public DateTimeOffset? Created { get; set; }
        [JsonPropertyName("updated")] public DateTimeOffset? Updated { get; set; }
        [JsonPropertyName("trigger")] public KlaviyoFlowTrigger? Trigger { get; set; }
    }

    public sealed class KlaviyoFlowTrigger
    {
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("frequency")] public string? Frequency { get; set; }
        [JsonPropertyName("predicate")] public string? Predicate { get; set; }
    }

    public sealed class KlaviyoFlowRelationships
    {
        [JsonPropertyName("flow-actions")] public KlaviyoRelationshipData? FlowActions { get; set; }
        [JsonPropertyName("tags")] public KlaviyoRelationshipData? Tags { get; set; }
    }

    // ---------- Event objects ----------

    public sealed class KlaviyoEvent : KlaviyoEntityBase
    {
        [JsonPropertyName("attributes")] public KlaviyoEventAttributes? Attributes { get; set; }
        [JsonPropertyName("relationships")] public KlaviyoEventRelationships? Relationships { get; set; }
        [JsonPropertyName("links")] public KlaviyoLinks? Links { get; set; }
    }

    public sealed class KlaviyoEventAttributes
    {
        [JsonPropertyName("timestamp")] public long? Timestamp { get; set; }
        [JsonPropertyName("event_properties")] public Dictionary<string, object>? EventProperties { get; set; }
        [JsonPropertyName("datetime")] public DateTimeOffset? DateTime { get; set; }
        [JsonPropertyName("uuid")] public string? Uuid { get; set; }
    }

    public sealed class KlaviyoEventRelationships
    {
        [JsonPropertyName("profile")] public KlaviyoRelationshipData? Profile { get; set; }
        [JsonPropertyName("metric")] public KlaviyoRelationshipData? Metric { get; set; }
    }

    // ---------- Metric objects ----------

    public sealed class KlaviyoMetric : KlaviyoEntityBase
    {
        [JsonPropertyName("attributes")] public KlaviyoMetricAttributes? Attributes { get; set; }
        [JsonPropertyName("relationships")] public KlaviyoMetricRelationships? Relationships { get; set; }
        [JsonPropertyName("links")] public KlaviyoLinks? Links { get; set; }
    }

    public sealed class KlaviyoMetricAttributes
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("created")] public DateTimeOffset? Created { get; set; }
        [JsonPropertyName("updated")] public DateTimeOffset? Updated { get; set; }
        [JsonPropertyName("integration")] public KlaviyoIntegration? Integration { get; set; }
    }

    public sealed class KlaviyoIntegration
    {
        [JsonPropertyName("object")] public string? Object { get; set; }
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("category")] public string? Category { get; set; }
    }

    public sealed class KlaviyoMetricRelationships
    {
        [JsonPropertyName("events")] public KlaviyoRelationshipData? Events { get; set; }
    }

    // ---------- Tag objects ----------

    public sealed class KlaviyoTag : KlaviyoEntityBase
    {
        [JsonPropertyName("attributes")] public KlaviyoTagAttributes? Attributes { get; set; }
        [JsonPropertyName("relationships")] public KlaviyoTagRelationships? Relationships { get; set; }
        [JsonPropertyName("links")] public KlaviyoLinks? Links { get; set; }
    }

    public sealed class KlaviyoTagAttributes
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("definition")] public string? Definition { get; set; }
    }

    public sealed class KlaviyoTagRelationships
    {
        [JsonPropertyName("campaigns")] public KlaviyoRelationshipData? Campaigns { get; set; }
        [JsonPropertyName("flows")] public KlaviyoRelationshipData? Flows { get; set; }
        [JsonPropertyName("lists")] public KlaviyoRelationshipData? Lists { get; set; }
        [JsonPropertyName("segments")] public KlaviyoRelationshipData? Segments { get; set; }
    }

    // ---------- Template objects ----------

    public sealed class KlaviyoTemplate : KlaviyoEntityBase
    {
        [JsonPropertyName("attributes")] public KlaviyoTemplateAttributes? Attributes { get; set; }
        [JsonPropertyName("relationships")] public KlaviyoTemplateRelationships? Relationships { get; set; }
        [JsonPropertyName("links")] public KlaviyoLinks? Links { get; set; }
    }

    public sealed class KlaviyoTemplateAttributes
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("editor_type")] public string? EditorType { get; set; }
        [JsonPropertyName("html")] public string? Html { get; set; }
        [JsonPropertyName("text")] public string? Text { get; set; }
        [JsonPropertyName("created")] public DateTimeOffset? Created { get; set; }
        [JsonPropertyName("updated")] public DateTimeOffset? Updated { get; set; }
    }

    public sealed class KlaviyoTemplateRelationships
    {
        [JsonPropertyName("campaigns")] public KlaviyoRelationshipData? Campaigns { get; set; }
    }

    // ---------- Coupon objects ----------

    public sealed class KlaviyoCoupon : KlaviyoEntityBase
    {
        [JsonPropertyName("attributes")] public KlaviyoCouponAttributes? Attributes { get; set; }
        [JsonPropertyName("relationships")] public KlaviyoCouponRelationships? Relationships { get; set; }
        [JsonPropertyName("links")] public KlaviyoLinks? Links { get; set; }
    }

    public sealed class KlaviyoCouponAttributes
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("code")] public string? Code { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("value")] public double? Value { get; set; }
        [JsonPropertyName("minimum")] public double? Minimum { get; set; }
        [JsonPropertyName("maximum")] public double? Maximum { get; set; }
        [JsonPropertyName("expiration")] public DateTimeOffset? Expiration { get; set; }
        [JsonPropertyName("created")] public DateTimeOffset? Created { get; set; }
        [JsonPropertyName("updated")] public DateTimeOffset? Updated { get; set; }
    }

    public sealed class KlaviyoCouponRelationships
    {
        [JsonPropertyName("campaigns")] public KlaviyoRelationshipData? Campaigns { get; set; }
    }

    // ---------- Catalog objects ----------

    public sealed class KlaviyoCatalog : KlaviyoEntityBase
    {
        [JsonPropertyName("attributes")] public KlaviyoCatalogAttributes? Attributes { get; set; }
        [JsonPropertyName("relationships")] public KlaviyoCatalogRelationships? Relationships { get; set; }
        [JsonPropertyName("links")] public KlaviyoLinks? Links { get; set; }
    }

    public sealed class KlaviyoCatalogAttributes
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("created")] public DateTimeOffset? Created { get; set; }
        [JsonPropertyName("updated")] public DateTimeOffset? Updated { get; set; }
    }

    public sealed class KlaviyoCatalogRelationships
    {
        [JsonPropertyName("items")] public KlaviyoRelationshipData? Items { get; set; }
        [JsonPropertyName("categories")] public KlaviyoRelationshipData? Categories { get; set; }
        [JsonPropertyName("variants")] public KlaviyoRelationshipData? Variants { get; set; }
    }

    // ---------- Webhook objects ----------

    public sealed class KlaviyoWebhook : KlaviyoEntityBase
    {
        [JsonPropertyName("attributes")] public KlaviyoWebhookAttributes? Attributes { get; set; }
        [JsonPropertyName("links")] public KlaviyoLinks? Links { get; set; }
    }

    public sealed class KlaviyoWebhookAttributes
    {
        [JsonPropertyName("topics")] public List<string>? Topics { get; set; } = new();
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("created")] public DateTimeOffset? Created { get; set; }
        [JsonPropertyName("updated")] public DateTimeOffset? Updated { get; set; }
    }

    // ---------- Account objects ----------

    public sealed class KlaviyoAccount : KlaviyoEntityBase
    {
        [JsonPropertyName("attributes")] public KlaviyoAccountAttributes? Attributes { get; set; }
        [JsonPropertyName("links")] public KlaviyoLinks? Links { get; set; }
    }

    public sealed class KlaviyoAccountAttributes
    {
        [JsonPropertyName("contact_information")] public KlaviyoContactInformation? ContactInformation { get; set; }
        [JsonPropertyName("industry")] public string? Industry { get; set; }
        [JsonPropertyName("timezone")] public string? Timezone { get; set; }
        [JsonPropertyName("currency")] public string? Currency { get; set; }
        [JsonPropertyName("public_api_key")] public string? PublicApiKey { get; set; }
    }

    public sealed class KlaviyoContactInformation
    {
        [JsonPropertyName("organization_name")] public string? OrganizationName { get; set; }
        [JsonPropertyName("address")] public KlaviyoAddress? Address { get; set; }
        [JsonPropertyName("email_address")] public string? EmailAddress { get; set; }
        [JsonPropertyName("phone_number")] public string? PhoneNumber { get; set; }
    }

    public sealed class KlaviyoAddress
    {
        [JsonPropertyName("address1")] public string? Address1 { get; set; }
        [JsonPropertyName("address2")] public string? Address2 { get; set; }
        [JsonPropertyName("city")] public string? City { get; set; }
        [JsonPropertyName("country")] public string? Country { get; set; }
        [JsonPropertyName("region")] public string? Region { get; set; }
        [JsonPropertyName("zip")] public string? Zip { get; set; }
    }
}