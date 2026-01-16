// File: Connectors/CRM/HubSpotDataSource/Models.cs
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.HubSpot.Models
{
    // Common base for HubSpot CRM objects
    public abstract class HubSpotEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("createdAt")] public DateTime? CreatedAt { get; set; }
        [JsonPropertyName("updatedAt")] public DateTime? UpdatedAt { get; set; }
        [JsonPropertyName("archived")] public bool? Archived { get; set; }
        [JsonPropertyName("properties")] public HubSpotProperties Properties { get; set; } = new();

        // Optional: Let POCOs know their datasource after hydration
        [JsonIgnore] public IDataSource DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : HubSpotEntityBase { DataSource = ds; return (T)this; }
    }

    // Base class for properties (common pattern in HubSpot)
    public class HubSpotProperties
    {
        [JsonPropertyName("createdate")] public string CreateDate { get; set; }
        [JsonPropertyName("lastmodifieddate")] public string LastModifiedDate { get; set; }
        [JsonPropertyName("hs_object_id")] public string HsObjectId { get; set; }
    }

    // ---- Core CRM Objects ----

    public sealed class Contact : HubSpotEntityBase
    {
        [JsonPropertyName("properties")] public new ContactProperties Properties { get; set; } = new();
    }

    public class ContactProperties : HubSpotProperties
    {
        [JsonPropertyName("firstname")] public string FirstName { get; set; }
        [JsonPropertyName("lastname")] public string LastName { get; set; }
        [JsonPropertyName("email")] public string Email { get; set; }
        [JsonPropertyName("phone")] public string Phone { get; set; }
        [JsonPropertyName("company")] public string Company { get; set; }
        [JsonPropertyName("website")] public string Website { get; set; }
        [JsonPropertyName("jobtitle")] public string JobTitle { get; set; }
        [JsonPropertyName("lifecyclestage")] public string LifecycleStage { get; set; }
        [JsonPropertyName("hubspot_owner_id")] public string HubspotOwnerId { get; set; }
    }

    public sealed class Company : HubSpotEntityBase
    {
        [JsonPropertyName("properties")] public new CompanyProperties Properties { get; set; } = new();
    }

    public class CompanyProperties : HubSpotProperties
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("domain")] public string Domain { get; set; }
        [JsonPropertyName("industry")] public string Industry { get; set; }
        [JsonPropertyName("phone")] public string Phone { get; set; }
        [JsonPropertyName("city")] public string City { get; set; }
        [JsonPropertyName("state")] public string State { get; set; }
        [JsonPropertyName("country")] public string Country { get; set; }
        [JsonPropertyName("website")] public string Website { get; set; }
        [JsonPropertyName("numberofemployees")] public string NumberOfEmployees { get; set; }
        [JsonPropertyName("annualrevenue")] public string AnnualRevenue { get; set; }
        [JsonPropertyName("hubspot_owner_id")] public string HubspotOwnerId { get; set; }
    }

    public sealed class Deal : HubSpotEntityBase
    {
        [JsonPropertyName("properties")] public new DealProperties Properties { get; set; } = new();
    }

    public class DealProperties : HubSpotProperties
    {
        [JsonPropertyName("dealname")] public string DealName { get; set; }
        [JsonPropertyName("amount")] public string Amount { get; set; }
        [JsonPropertyName("closedate")] public string CloseDate { get; set; }
        [JsonPropertyName("dealstage")] public string DealStage { get; set; }
        [JsonPropertyName("pipeline")] public string Pipeline { get; set; }
        [JsonPropertyName("dealtype")] public string DealType { get; set; }
        [JsonPropertyName("hubspot_owner_id")] public string HubspotOwnerId { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
    }

    public sealed class Ticket : HubSpotEntityBase
    {
        [JsonPropertyName("properties")] public new TicketProperties Properties { get; set; } = new();
    }

    public class TicketProperties : HubSpotProperties
    {
        [JsonPropertyName("subject")] public string Subject { get; set; }
        [JsonPropertyName("content")] public string Content { get; set; }
        [JsonPropertyName("hs_ticket_priority")] public string Priority { get; set; }
        [JsonPropertyName("hs_pipeline_stage")] public string Stage { get; set; }
        [JsonPropertyName("hs_pipeline")] public string Pipeline { get; set; }
        [JsonPropertyName("hubspot_owner_id")] public string HubspotOwnerId { get; set; }
        [JsonPropertyName("hs_ticket_category")] public string Category { get; set; }
        [JsonPropertyName("source_type")] public string SourceType { get; set; }
    }

    public sealed class Product : HubSpotEntityBase
    {
        [JsonPropertyName("properties")] public new ProductProperties Properties { get; set; } = new();
    }

    public class ProductProperties : HubSpotProperties
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("price")] public string Price { get; set; }
        [JsonPropertyName("hs_sku")] public string Sku { get; set; }
        [JsonPropertyName("hs_cost_of_goods_sold")] public string CostOfGoodsSold { get; set; }
        [JsonPropertyName("hs_product_type")] public string ProductType { get; set; }
        [JsonPropertyName("recurringbillingfrequency")] public string RecurringBillingFrequency { get; set; }
    }

    public sealed class LineItem : HubSpotEntityBase
    {
        [JsonPropertyName("properties")] public new LineItemProperties Properties { get; set; } = new();
    }

    public class LineItemProperties : HubSpotProperties
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("price")] public string Price { get; set; }
        [JsonPropertyName("quantity")] public string Quantity { get; set; }
        [JsonPropertyName("amount")] public string Amount { get; set; }
        [JsonPropertyName("hs_product_id")] public string ProductId { get; set; }
        [JsonPropertyName("hs_line_item_currency_code")] public string CurrencyCode { get; set; }
    }

    public sealed class Quote : HubSpotEntityBase
    {
        [JsonPropertyName("properties")] public new QuoteProperties Properties { get; set; } = new();
    }

    public class QuoteProperties : HubSpotProperties
    {
        [JsonPropertyName("hs_title")] public string Title { get; set; }
        [JsonPropertyName("hs_expiration_date")] public string ExpirationDate { get; set; }
        [JsonPropertyName("hs_status")] public string Status { get; set; }
        [JsonPropertyName("hs_quote_amount")] public string QuoteAmount { get; set; }
        [JsonPropertyName("hs_currency")] public string Currency { get; set; }
        [JsonPropertyName("hubspot_owner_id")] public string HubspotOwnerId { get; set; }
    }

    public sealed class Owner
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("email")] public string Email { get; set; }
        [JsonPropertyName("firstName")] public string FirstName { get; set; }
        [JsonPropertyName("lastName")] public string LastName { get; set; }
        [JsonPropertyName("userId")] public int? UserId { get; set; }
        [JsonPropertyName("createdAt")] public DateTime? CreatedAt { get; set; }
        [JsonPropertyName("updatedAt")] public DateTime? UpdatedAt { get; set; }
        [JsonPropertyName("archived")] public bool? Archived { get; set; }
        [JsonPropertyName("teams")] public List<OwnerTeam> Teams { get; set; } = new();

        [JsonIgnore] public IDataSource DataSource { get; private set; }
        public Owner Attach(IDataSource ds) { DataSource = ds; return this; }
    }

    public class OwnerTeam
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("primary")] public bool Primary { get; set; }
    }

    // Response wrapper for HubSpot API responses
    public class HubSpotResponse<T>
    {
        [JsonPropertyName("results")] public List<T> Results { get; set; } = new();
        [JsonPropertyName("paging")] public HubSpotPaging Paging { get; set; }
    }

    public class HubSpotPaging
    {
        [JsonPropertyName("next")] public HubSpotPageLink Next { get; set; }
        [JsonPropertyName("prev")] public HubSpotPageLink Prev { get; set; }
    }

    public class HubSpotPageLink
    {
        [JsonPropertyName("after")] public string After { get; set; }
        [JsonPropertyName("link")] public string Link { get; set; }
    }

    // Properties metadata response
    public class HubSpotProperty
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("label")] public string Label { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("groupName")] public string GroupName { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("Fieldtype")] public string Fieldtype { get; set; }
        [JsonPropertyName("options")] public List<HubSpotPropertyOption> Options { get; set; } = new();
        [JsonPropertyName("calculated")] public bool Calculated { get; set; }
        [JsonPropertyName("externalOptions")] public bool ExternalOptions { get; set; }
        [JsonPropertyName("hasUniqueValue")] public bool HasUniqueValue { get; set; }
        [JsonPropertyName("hidden")] public bool Hidden { get; set; }
        [JsonPropertyName("hubspotDefined")] public bool HubspotDefined { get; set; }
        [JsonPropertyName("showCurrencySymbol")] public bool ShowCurrencySymbol { get; set; }
        [JsonPropertyName("modificationMetadata")] public HubSpotModificationMetadata ModificationMetadata { get; set; }

        [JsonIgnore] public IDataSource DataSource { get; private set; }
        public HubSpotProperty Attach(IDataSource ds) { DataSource = ds; return this; }
    }

    public class HubSpotPropertyOption
    {
        [JsonPropertyName("label")] public string Label { get; set; }
        [JsonPropertyName("value")] public string Value { get; set; }
        [JsonPropertyName("displayOrder")] public int DisplayOrder { get; set; }
        [JsonPropertyName("hidden")] public bool Hidden { get; set; }
    }

    public class HubSpotModificationMetadata
    {
        [JsonPropertyName("archivable")] public bool Archivable { get; set; }
        [JsonPropertyName("readOnlyDefinition")] public bool ReadOnlyDefinition { get; set; }
        [JsonPropertyName("readOnlyValue")] public bool ReadOnlyValue { get; set; }
    }
}