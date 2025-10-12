using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TheTechIdea.Beep.DataSources.CRM.Copper
{
    /// <summary>
    /// Base POCO used by all Copper entities. Captures common identifiers and timestamps.
    /// </summary>
    public abstract class CopperEntityBase
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("date_created")] public int? DateCreated { get; set; }
        [JsonPropertyName("date_modified")] public int? DateModified { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? AdditionalFields { get; set; }
    }

    public sealed class CopperLead : CopperEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("prefix")] public string? Prefix { get; set; }
        [JsonPropertyName("first_name")] public string? FirstName { get; set; }
        [JsonPropertyName("last_name")] public string? LastName { get; set; }
        [JsonPropertyName("middle_name")] public string? MiddleName { get; set; }
        [JsonPropertyName("suffix")] public string? Suffix { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("company_name")] public string? CompanyName { get; set; }
        [JsonPropertyName("customer_source_id")] public long? CustomerSourceId { get; set; }
        [JsonPropertyName("assignee_id")] public long? AssigneeId { get; set; }
        [JsonPropertyName("contact_type_id")] public long? ContactTypeId { get; set; }
        [JsonPropertyName("details")] public string? Details { get; set; }
        [JsonPropertyName("email")] public CopperEmail? Email { get; set; }
        [JsonPropertyName("phone_numbers")] public List<CopperPhoneNumber>? PhoneNumbers { get; set; }
        [JsonPropertyName("socials")] public List<CopperSocial>? Socials { get; set; }
        [JsonPropertyName("websites")] public List<CopperWebsite>? Websites { get; set; }
        [JsonPropertyName("address")] public CopperAddress? Address { get; set; }
        [JsonPropertyName("custom_fields")] public List<CopperCustomField>? CustomFields { get; set; }
        [JsonPropertyName("tags")] public List<string>? Tags { get; set; }
        [JsonPropertyName("interaction_count")] public int? InteractionCount { get; set; }
    }

    public sealed class CopperContact : CopperEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("prefix")] public string? Prefix { get; set; }
        [JsonPropertyName("first_name")] public string? FirstName { get; set; }
        [JsonPropertyName("last_name")] public string? LastName { get; set; }
        [JsonPropertyName("middle_name")] public string? MiddleName { get; set; }
        [JsonPropertyName("suffix")] public string? Suffix { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("company_name")] public string? CompanyName { get; set; }
        [JsonPropertyName("customer_source_id")] public long? CustomerSourceId { get; set; }
        [JsonPropertyName("assignee_id")] public long? AssigneeId { get; set; }
        [JsonPropertyName("contact_type_id")] public long? ContactTypeId { get; set; }
        [JsonPropertyName("details")] public string? Details { get; set; }
        [JsonPropertyName("email")] public CopperEmail? Email { get; set; }
        [JsonPropertyName("phone_numbers")] public List<CopperPhoneNumber>? PhoneNumbers { get; set; }
        [JsonPropertyName("socials")] public List<CopperSocial>? Socials { get; set; }
        [JsonPropertyName("websites")] public List<CopperWebsite>? Websites { get; set; }
        [JsonPropertyName("address")] public CopperAddress? Address { get; set; }
        [JsonPropertyName("custom_fields")] public List<CopperCustomField>? CustomFields { get; set; }
        [JsonPropertyName("tags")] public List<string>? Tags { get; set; }
        [JsonPropertyName("interaction_count")] public int? InteractionCount { get; set; }
    }

    public sealed class CopperAccount : CopperEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("assignee_id")] public long? AssigneeId { get; set; }
        [JsonPropertyName("contact_type_id")] public long? ContactTypeId { get; set; }
        [JsonPropertyName("details")] public string? Details { get; set; }
        [JsonPropertyName("email_domain")] public string? EmailDomain { get; set; }
        [JsonPropertyName("phone_numbers")] public List<CopperPhoneNumber>? PhoneNumbers { get; set; }
        [JsonPropertyName("socials")] public List<CopperSocial>? Socials { get; set; }
        [JsonPropertyName("websites")] public List<CopperWebsite>? Websites { get; set; }
        [JsonPropertyName("address")] public CopperAddress? Address { get; set; }
        [JsonPropertyName("custom_fields")] public List<CopperCustomField>? CustomFields { get; set; }
        [JsonPropertyName("tags")] public List<string>? Tags { get; set; }
        [JsonPropertyName("interaction_count")] public int? InteractionCount { get; set; }
    }

    public sealed class CopperDeal : CopperEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("primary_contact_id")] public long? PrimaryContactId { get; set; }
        [JsonPropertyName("company_id")] public long? CompanyId { get; set; }
        [JsonPropertyName("company_name")] public string? CompanyName { get; set; }
        [JsonPropertyName("customer_source_id")] public long? CustomerSourceId { get; set; }
        [JsonPropertyName("pipeline_id")] public long? PipelineId { get; set; }
        [JsonPropertyName("pipeline_stage_id")] public long? PipelineStageId { get; set; }
        [JsonPropertyName("pipeline_stage_name")] public string? PipelineStageName { get; set; }
        [JsonPropertyName("assignee_id")] public long? AssigneeId { get; set; }
        [JsonPropertyName("close_date")] public int? CloseDate { get; set; }
        [JsonPropertyName("details")] public string? Details { get; set; }
        [JsonPropertyName("priority")] public string? Priority { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("value")] public CopperValue? Value { get; set; }
        [JsonPropertyName("custom_fields")] public List<CopperCustomField>? CustomFields { get; set; }
        [JsonPropertyName("tags")] public List<string>? Tags { get; set; }
        [JsonPropertyName("interaction_count")] public int? InteractionCount { get; set; }
    }

    public sealed class CopperTask : CopperEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("related_resource")] public CopperRelatedResource? RelatedResource { get; set; }
        [JsonPropertyName("assignee_id")] public long? AssigneeId { get; set; }
        [JsonPropertyName("due_date")] public int? DueDate { get; set; }
        [JsonPropertyName("reminder_date")] public int? ReminderDate { get; set; }
        [JsonPropertyName("completed_date")] public int? CompletedDate { get; set; }
        [JsonPropertyName("priority")] public string? Priority { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("details")] public string? Details { get; set; }
        [JsonPropertyName("tags")] public List<string>? Tags { get; set; }
    }

    public sealed class CopperActivity : CopperEntityBase
    {
        [JsonPropertyName("type")] public CopperActivityType? Type { get; set; }
        [JsonPropertyName("details")] public string? Details { get; set; }
        [JsonPropertyName("activity_date")] public int? ActivityDate { get; set; }
        [JsonPropertyName("old_value")] public object? OldValue { get; set; }
        [JsonPropertyName("new_value")] public object? NewValue { get; set; }
        [JsonPropertyName("parent")] public CopperRelatedResource? Parent { get; set; }
        [JsonPropertyName("user_id")] public long? UserId { get; set; }
        [JsonPropertyName("primary_resource_name")] public string? PrimaryResourceName { get; set; }
    }

    public sealed class CopperUser : CopperEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("groups")] public List<CopperGroup>? Groups { get; set; }
    }

    // Supporting classes
    public sealed class CopperEmail
    {
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("category")] public string? Category { get; set; }
    }

    public sealed class CopperPhoneNumber
    {
        [JsonPropertyName("number")] public string? Number { get; set; }
        [JsonPropertyName("category")] public string? Category { get; set; }
    }

    public sealed class CopperSocial
    {
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("category")] public string? Category { get; set; }
    }

    public sealed class CopperWebsite
    {
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("category")] public string? Category { get; set; }
    }

    public sealed class CopperAddress
    {
        [JsonPropertyName("street")] public string? Street { get; set; }
        [JsonPropertyName("city")] public string? City { get; set; }
        [JsonPropertyName("state")] public string? State { get; set; }
        [JsonPropertyName("postal_code")] public string? PostalCode { get; set; }
        [JsonPropertyName("country")] public string? Country { get; set; }
    }

    public sealed class CopperCustomField
    {
        [JsonPropertyName("custom_field_definition_id")] public long? CustomFieldDefinitionId { get; set; }
        [JsonPropertyName("value")] public object? Value { get; set; }
    }

    public sealed class CopperValue
    {
        [JsonPropertyName("amount")] public decimal? Amount { get; set; }
        [JsonPropertyName("currency")] public string? Currency { get; set; }
    }

    public sealed class CopperRelatedResource
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
    }

    public sealed class CopperActivityType
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("category")] public string? Category { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
    }

    public sealed class CopperGroup
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
    }

    public sealed class CopperPagination
    {
        [JsonPropertyName("page")] public int? Page { get; set; }
        [JsonPropertyName("per_page")] public int? PerPage { get; set; }
        [JsonPropertyName("total")] public int? Total { get; set; }
    }

    public sealed class CopperResponse<T>
    {
        [JsonPropertyName("data")] public List<T> Data { get; set; } = new();
        [JsonPropertyName("pagination")] public CopperPagination? Pagination { get; set; }
    }
}