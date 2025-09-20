// File: BeepDM/Connectors/Marketing/ConstantContactDataSource/Models/ConstantContactModels.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Marketing.ConstantContactDataSource.Models
{
    // Base
    public abstract class ConstantContactEntityBase
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("created_date")] public string? CreatedDate { get; set; }
        [JsonPropertyName("modified_date")] public string? ModifiedDate { get; set; }

        [JsonIgnore] public IDataSource? DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : ConstantContactEntityBase { DataSource = ds; return (T)this; }
    }

    // ---------- Contact objects ----------

    public sealed class ConstantContactContact : ConstantContactEntityBase
    {
        [JsonPropertyName("email_addresses")] public List<ConstantContactEmailAddress>? EmailAddresses { get; set; } = new();
        [JsonPropertyName("first_name")] public string? FirstName { get; set; }
        [JsonPropertyName("last_name")] public string? LastName { get; set; }
        [JsonPropertyName("company_name")] public string? CompanyName { get; set; }
        [JsonPropertyName("job_title")] public string? JobTitle { get; set; }
        [JsonPropertyName("home_phone")] public string? HomePhone { get; set; }
        [JsonPropertyName("work_phone")] public string? WorkPhone { get; set; }
        [JsonPropertyName("cell_phone")] public string? CellPhone { get; set; }
        [JsonPropertyName("fax")] public string? Fax { get; set; }
        [JsonPropertyName("addresses")] public List<ConstantContactAddress>? Addresses { get; set; } = new();
        [JsonPropertyName("lists")] public List<ConstantContactListMembership>? Lists { get; set; } = new();
        [JsonPropertyName("confirmed")] public bool? Confirmed { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("custom_fields")] public List<ConstantContactCustomField>? CustomFields { get; set; } = new();
        [JsonPropertyName("notes")] public List<ConstantContactNote>? Notes { get; set; } = new();
    }

    public sealed class ConstantContactEmailAddress
    {
        [JsonPropertyName("email_address")] public string? EmailAddress { get; set; }
        [JsonPropertyName("opt_in_date")] public string? OptInDate { get; set; }
        [JsonPropertyName("opt_out_date")] public string? OptOutDate { get; set; }
        [JsonPropertyName("opt_in_source")] public string? OptInSource { get; set; }
        [JsonPropertyName("confirm_status")] public string? ConfirmStatus { get; set; }
    }

    public sealed class ConstantContactAddress
    {
        [JsonPropertyName("address_type")] public string? AddressType { get; set; }
        [JsonPropertyName("line1")] public string? Line1 { get; set; }
        [JsonPropertyName("line2")] public string? Line2 { get; set; }
        [JsonPropertyName("line3")] public string? Line3 { get; set; }
        [JsonPropertyName("city")] public string? City { get; set; }
        [JsonPropertyName("state")] public string? State { get; set; }
        [JsonPropertyName("state_code")] public string? StateCode { get; set; }
        [JsonPropertyName("country_code")] public string? CountryCode { get; set; }
        [JsonPropertyName("postal_code")] public string? PostalCode { get; set; }
        [JsonPropertyName("sub_postal_code")] public string? SubPostalCode { get; set; }
    }

    public sealed class ConstantContactListMembership
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
    }

    public sealed class ConstantContactCustomField
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("value")] public string? Value { get; set; }
    }

    public sealed class ConstantContactNote
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("note")] public string? Note { get; set; }
        [JsonPropertyName("created_date")] public string? CreatedDate { get; set; }
        [JsonPropertyName("modified_date")] public string? ModifiedDate { get; set; }
    }

    // ---------- List objects ----------

    public sealed class ConstantContactList : ConstantContactEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("contact_count")] public int? ContactCount { get; set; }
    }

    // ---------- Campaign objects ----------

    public sealed class ConstantContactCampaign : ConstantContactEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("subject")] public string? Subject { get; set; }
        [JsonPropertyName("from_name")] public string? FromName { get; set; }
        [JsonPropertyName("from_email")] public string? FromEmail { get; set; }
        [JsonPropertyName("reply_to_email")] public string? ReplyToEmail { get; set; }
        [JsonPropertyName("template_type")] public string? TemplateType { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("email_content")] public string? EmailContent { get; set; }
        [JsonPropertyName("email_content_format")] public string? EmailContentFormat { get; set; }
        [JsonPropertyName("text_content")] public string? TextContent { get; set; }
        [JsonPropertyName("sent_to_contact_lists")] public List<ConstantContactSentToList>? SentToContactLists { get; set; } = new();
        [JsonPropertyName("click_through_details")] public List<ConstantContactClickThroughDetail>? ClickThroughDetails { get; set; } = new();
        [JsonPropertyName("tracking_summary")] public ConstantContactTrackingSummary? TrackingSummary { get; set; }
    }

    public sealed class ConstantContactSentToList
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
    }

    public sealed class ConstantContactClickThroughDetail
    {
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("url_uid")] public string? UrlUid { get; set; }
        [JsonPropertyName("click_count")] public int? ClickCount { get; set; }
    }

    public sealed class ConstantContactTrackingSummary
    {
        [JsonPropertyName("sends")] public int? Sends { get; set; }
        [JsonPropertyName("opens")] public int? Opens { get; set; }
        [JsonPropertyName("clicks")] public int? Clicks { get; set; }
        [JsonPropertyName("forwards")] public int? Forwards { get; set; }
        [JsonPropertyName("unsubscribes")] public int? Unsubscribes { get; set; }
        [JsonPropertyName("bounces")] public int? Bounces { get; set; }
    }

    // ---------- Email Campaign objects (v3) ----------

    public sealed class ConstantContactEmailCampaign : ConstantContactEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("current_status")] public string? CurrentStatus { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("campaign_activities")] public List<ConstantContactCampaignActivity>? CampaignActivities { get; set; } = new();
    }

    public sealed class ConstantContactCampaignActivity
    {
        [JsonPropertyName("campaign_activity_id")] public string? CampaignActivityId { get; set; }
        [JsonPropertyName("role")] public string? Role { get; set; }
    }

    // ---------- Activity objects ----------

    public sealed class ConstantContactActivity : ConstantContactEntityBase
    {
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("start_date")] public string? StartDate { get; set; }
        [JsonPropertyName("finish_date")] public string? FinishDate { get; set; }
        [JsonPropertyName("file_name")] public string? FileName { get; set; }
        [JsonPropertyName("error_count")] public int? ErrorCount { get; set; }
        [JsonPropertyName("contact_count")] public int? ContactCount { get; set; }
    }

    // ---------- Account objects ----------

    public sealed class ConstantContactAccount
    {
        [JsonPropertyName("website")] public string? Website { get; set; }
        [JsonPropertyName("organization_name")] public string? OrganizationName { get; set; }
        [JsonPropertyName("time_zone")] public string? TimeZone { get; set; }
        [JsonPropertyName("first_name")] public string? FirstName { get; set; }
        [JsonPropertyName("last_name")] public string? LastName { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("phone")] public string? Phone { get; set; }
        [JsonPropertyName("company_logo")] public string? CompanyLogo { get; set; }
        [JsonPropertyName("country_code")] public string? CountryCode { get; set; }
        [JsonPropertyName("state_code")] public string? StateCode { get; set; }
        [JsonPropertyName("city")] public string? City { get; set; }
        [JsonPropertyName("address_line1")] public string? AddressLine1 { get; set; }
        [JsonPropertyName("address_line2")] public string? AddressLine2 { get; set; }
        [JsonPropertyName("address_line3")] public string? AddressLine3 { get; set; }
        [JsonPropertyName("postal_code")] public string? PostalCode { get; set; }
    }

    // ---------- Tag objects ----------

    public sealed class ConstantContactTag : ConstantContactEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("calculated_contacts")] public int? CalculatedContacts { get; set; }
    }

    // ---------- Custom Field objects ----------

    public sealed class ConstantContactCustomFieldDefinition : ConstantContactEntityBase
    {
        [JsonPropertyName("label")] public string? Label { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("display_order")] public int? DisplayOrder { get; set; }
    }
}