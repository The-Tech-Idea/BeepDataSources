// File: Connectors/CRM/FreshsalesDataSource/Models.cs

using System;        /// <summary>

using System.Collections.Generic;        /// Freshsales entity metadata

using System.Text.Json.Serialization;        /// </summary>

using TheTechIdea.Beep.DataBase;        public class FreshsalesEntity

        {

namespace TheTechIdea.Beep.Connectors.Freshsales.Models            public string EntityName { get; set; } = string.Empty;

{            public string DisplayName { get; set; } = string.Empty;

    // Common base for Freshsales objects            public string ApiEndpoint { get; set; } = string.Empty;

    public abstract class FreshsalesEntityBase            public Dictionary<string, string> Fields { get; set; } = new();

    {        }

        [JsonPropertyName("id")] public long? Id { get; set; }

        [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; set; }        /// <summary>

        [JsonPropertyName("updated_at")] public DateTime? UpdatedAt { get; set; }        /// Freshsales API response wrapper

        [JsonPropertyName("active")] public bool? Active { get; set; }        /// </summary>

        public class FreshsalesApiResponse<T>

        // Optional: Let POCOs know their datasource after hydration        {

        [JsonIgnore] public IDataSource DataSource { get; private set; }            public List<T> Contacts { get; set; } = new();

        public T Attach<T>(IDataSource ds) where T : FreshsalesEntityBase { DataSource = ds; return (T)this; }            public List<T> Leads { get; set; } = new();

    }            public List<T> Accounts { get; set; } = new();

            public List<T> Deals { get; set; } = new();

    // ---- Core CRM Objects ----            public FreshsalesMeta Meta { get; set; } = new();

        }

    public sealed class Lead : FreshsalesEntityBase

    {        /// <summary>

        [JsonPropertyName("first_name")] public string FirstName { get; set; }        /// Freshsales API metadata

        [JsonPropertyName("last_name")] public string LastName { get; set; }        /// </summary>

        [JsonPropertyName("display_name")] public string DisplayName { get; set; }        public class FreshsalesMeta

        [JsonPropertyName("avatar")] public string Avatar { get; set; }        {

        [JsonPropertyName("job_title")] public string JobTitle { get; set; }            public int Total { get; set; }

        [JsonPropertyName("city")] public string City { get; set; }            public int PerPage { get; set; }

        [JsonPropertyName("state")] public string State { get; set; }            public int CurrentPage { get; set; }

        [JsonPropertyName("zipcode")] public string Zipcode { get; set; }            public int TotalPages { get; set; }

        [JsonPropertyName("country")] public string Country { get; set; }        }

        [JsonPropertyName("email")] public string Email { get; set; }
        [JsonPropertyName("emails")] public List<FreshsalesEmail> Emails { get; set; } = new();
        [JsonPropertyName("phone")] public string Phone { get; set; }
        [JsonPropertyName("mobile_number")] public string MobileNumber { get; set; }
        [JsonPropertyName("work_number")] public string WorkNumber { get; set; }
        [JsonPropertyName("address")] public string Address { get; set; }
        [JsonPropertyName("website")] public string Website { get; set; }
        [JsonPropertyName("lead_source_id")] public long? LeadSourceId { get; set; }
        [JsonPropertyName("lead_reason_id")] public long? LeadReasonId { get; set; }
        [JsonPropertyName("status_id")] public long? StatusId { get; set; }
        [JsonPropertyName("owner_id")] public long? OwnerId { get; set; }
        [JsonPropertyName("company_name")] public string CompanyName { get; set; }
        [JsonPropertyName("territory_id")] public long? TerritoryId { get; set; }
        [JsonPropertyName("medium")] public string Medium { get; set; }
        [JsonPropertyName("keyword")] public string Keyword { get; set; }
        [JsonPropertyName("facebook")] public string Facebook { get; set; }
        [JsonPropertyName("twitter")] public string Twitter { get; set; }
        [JsonPropertyName("linkedin")] public string Linkedin { get; set; }
        [JsonPropertyName("is_deleted")] public bool? IsDeleted { get; set; }
        [JsonPropertyName("team_user_ids")] public List<long> TeamUserIds { get; set; } = new();
        [JsonPropertyName("external_id")] public string ExternalId { get; set; }
        [JsonPropertyName("last_assigned_at")] public DateTime? LastAssignedAt { get; set; }
        [JsonPropertyName("last_contacted_mode")] public FreshsalesContactMode LastContactedMode { get; set; }
        [JsonPropertyName("recent_note")] public string RecentNote { get; set; }
        [JsonPropertyName("won_time")] public DateTime? WonTime { get; set; }
        [JsonPropertyName("last_contacted_via_sales_activity")] public DateTime? LastContactedViaSalesActivity { get; set; }
        [JsonPropertyName("completed_sales_sequences")] public Dictionary<string, object> CompletedSalesSequences { get; set; } = new();
        [JsonPropertyName("active_sales_sequences")] public Dictionary<string, object> ActiveSalesSequences { get; set; } = new();
        [JsonPropertyName("web_form_ids")] public List<string> WebFormIds { get; set; } = new();
        [JsonPropertyName("open_deals_count")] public int? OpenDealsCount { get; set; }
        [JsonPropertyName("won_deals_count")] public int? WonDealsCount { get; set; }
        [JsonPropertyName("open_deals_amount")] public decimal? OpenDealsAmount { get; set; }
        [JsonPropertyName("won_deals_amount")] public decimal? WonDealsAmount { get; set; }
        [JsonPropertyName("last_contacted")] public DateTime? LastContacted { get; set; }
        [JsonPropertyName("last_contacted_sales_activity_mode")] public string LastContactedSalesActivityMode { get; set; }
        [JsonPropertyName("custom_field")] public Dictionary<string, object> CustomFields { get; set; } = new();
        [JsonPropertyName("links")] public FreshsalesLinks Links { get; set; }
    }

    public sealed class Contact : FreshsalesEntityBase
    {
        [JsonPropertyName("first_name")] public string FirstName { get; set; }
        [JsonPropertyName("last_name")] public string LastName { get; set; }
        [JsonPropertyName("display_name")] public string DisplayName { get; set; }
        [JsonPropertyName("avatar")] public string Avatar { get; set; }
        [JsonPropertyName("job_title")] public string JobTitle { get; set; }
        [JsonPropertyName("city")] public string City { get; set; }
        [JsonPropertyName("state")] public string State { get; set; }
        [JsonPropertyName("zipcode")] public string Zipcode { get; set; }
        [JsonPropertyName("country")] public string Country { get; set; }
        [JsonPropertyName("email")] public string Email { get; set; }
        [JsonPropertyName("emails")] public List<FreshsalesEmail> Emails { get; set; } = new();
        [JsonPropertyName("phone")] public string Phone { get; set; }
        [JsonPropertyName("mobile_number")] public string MobileNumber { get; set; }
        [JsonPropertyName("work_number")] public string WorkNumber { get; set; }
        [JsonPropertyName("address")] public string Address { get; set; }
        [JsonPropertyName("website")] public string Website { get; set; }
        [JsonPropertyName("sales_account_id")] public long? SalesAccountId { get; set; }
        [JsonPropertyName("owner_id")] public long? OwnerId { get; set; }
        [JsonPropertyName("territory_id")] public long? TerritoryId { get; set; }
        [JsonPropertyName("medium")] public string Medium { get; set; }
        [JsonPropertyName("facebook")] public string Facebook { get; set; }
        [JsonPropertyName("twitter")] public string Twitter { get; set; }
        [JsonPropertyName("linkedin")] public string Linkedin { get; set; }
        [JsonPropertyName("is_deleted")] public bool? IsDeleted { get; set; }
        [JsonPropertyName("team_user_ids")] public List<long> TeamUserIds { get; set; } = new();
        [JsonPropertyName("external_id")] public string ExternalId { get; set; }
        [JsonPropertyName("work_email")] public string WorkEmail { get; set; }
        [JsonPropertyName("subscription_status")] public int? SubscriptionStatus { get; set; }
        [JsonPropertyName("subscription_types")] public string SubscriptionTypes { get; set; }
        [JsonPropertyName("customer_fit")] public int? CustomerFit { get; set; }
        [JsonPropertyName("record_type_id")] public string RecordTypeId { get; set; }
        [JsonPropertyName("whatsapp_subscription_status")] public int? WhatsappSubscriptionStatus { get; set; }
        [JsonPropertyName("sms_subscription_status")] public int? SmsSubscriptionStatus { get; set; }
        [JsonPropertyName("last_contacted")] public DateTime? LastContacted { get; set; }
        [JsonPropertyName("last_contacted_sales_activity_mode")] public string LastContactedSalesActivityMode { get; set; }
        [JsonPropertyName("recent_note")] public string RecentNote { get; set; }
        [JsonPropertyName("last_contacted_mode")] public FreshsalesContactMode LastContactedMode { get; set; }
        [JsonPropertyName("last_contacted_via_sales_activity")] public DateTime? LastContactedViaSalesActivity { get; set; }
        [JsonPropertyName("completed_sales_sequences")] public Dictionary<string, object> CompletedSalesSequences { get; set; } = new();
        [JsonPropertyName("active_sales_sequences")] public Dictionary<string, object> ActiveSalesSequences { get; set; } = new();
        [JsonPropertyName("last_assigned_at")] public DateTime? LastAssignedAt { get; set; }
        [JsonPropertyName("open_deals_count")] public int? OpenDealsCount { get; set; }
        [JsonPropertyName("won_deals_count")] public int? WonDealsCount { get; set; }
        [JsonPropertyName("open_deals_amount")] public decimal? OpenDealsAmount { get; set; }
        [JsonPropertyName("won_deals_amount")] public decimal? WonDealsAmount { get; set; }
        [JsonPropertyName("custom_field")] public Dictionary<string, object> CustomFields { get; set; } = new();
        [JsonPropertyName("links")] public FreshsalesLinks Links { get; set; }
    }

    public sealed class SalesAccount : FreshsalesEntityBase
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("address")] public string Address { get; set; }
        [JsonPropertyName("city")] public string City { get; set; }
        [JsonPropertyName("state")] public string State { get; set; }
        [JsonPropertyName("zipcode")] public string Zipcode { get; set; }
        [JsonPropertyName("country")] public string Country { get; set; }
        [JsonPropertyName("number_of_employees")] public int? NumberOfEmployees { get; set; }
        [JsonPropertyName("annual_revenue")] public decimal? AnnualRevenue { get; set; }
        [JsonPropertyName("website")] public string Website { get; set; }
        [JsonPropertyName("owner_id")] public long? OwnerId { get; set; }
        [JsonPropertyName("phone")] public string Phone { get; set; }
        [JsonPropertyName("industry_type_id")] public long? IndustryTypeId { get; set; }
        [JsonPropertyName("business_type_id")] public long? BusinessTypeId { get; set; }
        [JsonPropertyName("territory_id")] public long? TerritoryId { get; set; }
        [JsonPropertyName("account_tier_id")] public long? AccountTierId { get; set; }
        [JsonPropertyName("is_deleted")] public bool? IsDeleted { get; set; }
        [JsonPropertyName("team_user_ids")] public List<long> TeamUserIds { get; set; } = new();
        [JsonPropertyName("avatar")] public string Avatar { get; set; }
        [JsonPropertyName("parent_sales_account_id")] public long? ParentSalesAccountId { get; set; }
        [JsonPropertyName("recent_note")] public string RecentNote { get; set; }
        [JsonPropertyName("last_contacted")] public DateTime? LastContacted { get; set; }
        [JsonPropertyName("last_contacted_mode")] public FreshsalesContactMode LastContactedMode { get; set; }
        [JsonPropertyName("facebook")] public string Facebook { get; set; }
        [JsonPropertyName("twitter")] public string Twitter { get; set; }
        [JsonPropertyName("linkedin")] public string Linkedin { get; set; }
        [JsonPropertyName("open_deals_count")] public int? OpenDealsCount { get; set; }
        [JsonPropertyName("won_deals_count")] public int? WonDealsCount { get; set; }
        [JsonPropertyName("open_deals_amount")] public decimal? OpenDealsAmount { get; set; }
        [JsonPropertyName("won_deals_amount")] public decimal? WonDealsAmount { get; set; }
        [JsonPropertyName("custom_field")] public Dictionary<string, object> CustomFields { get; set; } = new();
        [JsonPropertyName("links")] public FreshsalesLinks Links { get; set; }
    }

    public sealed class Deal : FreshsalesEntityBase
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("amount")] public decimal? Amount { get; set; }
        [JsonPropertyName("base_currency_amount")] public decimal? BaseCurrencyAmount { get; set; }
        [JsonPropertyName("expected_close")] public DateTime? ExpectedClose { get; set; }
        [JsonPropertyName("closed_date")] public DateTime? ClosedDate { get; set; }
        [JsonPropertyName("stage_updated_time")] public DateTime? StageUpdatedTime { get; set; }
        [JsonPropertyName("probability")] public int? Probability { get; set; }
        [JsonPropertyName("deal_stage_id")] public long? DealStageId { get; set; }
        [JsonPropertyName("deal_reason_id")] public long? DealReasonId { get; set; }
        [JsonPropertyName("deal_type_id")] public long? DealTypeId { get; set; }
        [JsonPropertyName("deal_payment_status_id")] public long? DealPaymentStatusId { get; set; }
        [JsonPropertyName("deal_product_id")] public long? DealProductId { get; set; }
        [JsonPropertyName("sales_account_id")] public long? SalesAccountId { get; set; }
        [JsonPropertyName("primary_contact_id")] public long? PrimaryContactId { get; set; }
        [JsonPropertyName("owner_id")] public long? OwnerId { get; set; }
        [JsonPropertyName("currency_id")] public long? CurrencyId { get; set; }
        [JsonPropertyName("territory_id")] public long? TerritoryId { get; set; }
        [JsonPropertyName("deal_pipeline_id")] public long? DealPipelineId { get; set; }
        [JsonPropertyName("campaign_id")] public long? CampaignId { get; set; }
        [JsonPropertyName("lead_source_id")] public long? LeadSourceId { get; set; }
        [JsonPropertyName("is_deleted")] public bool? IsDeleted { get; set; }
        [JsonPropertyName("team_user_ids")] public List<long> TeamUserIds { get; set; } = new();
        [JsonPropertyName("avatar")] public string Avatar { get; set; }
        [JsonPropertyName("recent_note")] public string RecentNote { get; set; }
        [JsonPropertyName("deal_contact_ids")] public List<long> DealContactIds { get; set; } = new();
        [JsonPropertyName("won_time")] public DateTime? WonTime { get; set; }
        [JsonPropertyName("lost_time")] public DateTime? LostTime { get; set; }
        [JsonPropertyName("last_contacted")] public DateTime? LastContacted { get; set; }
        [JsonPropertyName("last_contacted_mode")] public FreshsalesContactMode LastContactedMode { get; set; }
        [JsonPropertyName("last_contacted_via_sales_activity")] public DateTime? LastContactedViaSalesActivity { get; set; }
        [JsonPropertyName("last_contacted_sales_activity_mode")] public string LastContactedSalesActivityMode { get; set; }
        [JsonPropertyName("completed_sales_sequences")] public Dictionary<string, object> CompletedSalesSequences { get; set; } = new();
        [JsonPropertyName("active_sales_sequences")] public Dictionary<string, object> ActiveSalesSequences { get; set; } = new();
        [JsonPropertyName("web_form_id")] public string WebFormId { get; set; }
        [JsonPropertyName("custom_field")] public Dictionary<string, object> CustomFields { get; set; } = new();
        [JsonPropertyName("links")] public FreshsalesLinks Links { get; set; }
    }

    // ---- Supporting Classes ----

    public class FreshsalesEmail
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("value")] public string Value { get; set; }
        [JsonPropertyName("is_primary")] public bool? IsPrimary { get; set; }
        [JsonPropertyName("label")] public string Label { get; set; }
        [JsonPropertyName("destroy")] public bool? Destroy { get; set; }
    }

    public class FreshsalesContactMode
    {
        [JsonPropertyName("email")] public DateTime? Email { get; set; }
        [JsonPropertyName("phone")] public DateTime? Phone { get; set; }
        [JsonPropertyName("chat")] public DateTime? Chat { get; set; }
        [JsonPropertyName("sms")] public DateTime? Sms { get; set; }
        [JsonPropertyName("whatsapp")] public DateTime? Whatsapp { get; set; }
        [JsonPropertyName("facebook")] public DateTime? Facebook { get; set; }
        [JsonPropertyName("twitter")] public DateTime? Twitter { get; set; }
        [JsonPropertyName("linkedin")] public DateTime? Linkedin { get; set; }
    }

    public class FreshsalesLinks
    {
        [JsonPropertyName("conversations")] public string Conversations { get; set; }
        [JsonPropertyName("activities")] public string Activities { get; set; }
        [JsonPropertyName("notes")] public string Notes { get; set; }
        [JsonPropertyName("tasks")] public string Tasks { get; set; }
        [JsonPropertyName("appointments")] public string Appointments { get; set; }
        [JsonPropertyName("reminders")] public string Reminders { get; set; }
        [JsonPropertyName("duplicates")] public string Duplicates { get; set; }
        [JsonPropertyName("documents")] public string Documents { get; set; }
    }

    // Response wrapper for Freshsales API responses
    public class FreshsalesResponse<T>
    {
        [JsonPropertyName("data")] public List<T> Data { get; set; } = new();
        [JsonPropertyName("meta")] public FreshsalesMeta Meta { get; set; }
    }

    public class FreshsalesMeta
    {
        [JsonPropertyName("total_pages")] public int? TotalPages { get; set; }
        [JsonPropertyName("total")] public int? Total { get; set; }
    }
}