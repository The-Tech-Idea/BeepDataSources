using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TheTechIdea.Beep.DataSources.CRM.Freshsales
{
    /// <summary>
    /// Base POCO used by all Freshsales entities. Captures common identifiers and timestamps.
    /// </summary>
    public abstract class FreshsalesEntityBase
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("uuid")] public Guid? Uuid { get; set; }
        [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; set; }
        [JsonPropertyName("updated_at")] public DateTime? UpdatedAt { get; set; }
        [JsonPropertyName("active")] public bool? Active { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? AdditionalFields { get; set; }
    }

    public sealed class FreshsalesLead : FreshsalesEntityBase
    {
        [JsonPropertyName("first_name")] public string? FirstName { get; set; }
        [JsonPropertyName("last_name")] public string? LastName { get; set; }
        [JsonPropertyName("display_name")] public string? DisplayName { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("mobile_number")] public string? MobileNumber { get; set; }
        [JsonPropertyName("work_number")] public string? WorkNumber { get; set; }
        [JsonPropertyName("owner_id")] public long? OwnerId { get; set; }
        [JsonPropertyName("source_id")] public long? SourceId { get; set; }
        [JsonPropertyName("status_id")] public long? StatusId { get; set; }
        [JsonPropertyName("account_id")] public long? AccountId { get; set; }
    }

    public sealed class FreshsalesContact : FreshsalesEntityBase
    {
        [JsonPropertyName("first_name")] public string? FirstName { get; set; }
        [JsonPropertyName("last_name")] public string? LastName { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("mobile_number")] public string? MobileNumber { get; set; }
        [JsonPropertyName("work_number")] public string? WorkNumber { get; set; }
        [JsonPropertyName("sales_account_id")] public long? SalesAccountId { get; set; }
        [JsonPropertyName("owner_id")] public long? OwnerId { get; set; }
    }

    public sealed class FreshsalesAccount : FreshsalesEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("website")] public string? Website { get; set; }
        [JsonPropertyName("phone")] public string? Phone { get; set; }
        [JsonPropertyName("owner_id")] public long? OwnerId { get; set; }
        [JsonPropertyName("industry_id")] public long? IndustryId { get; set; }
        [JsonPropertyName("territory_id")] public long? TerritoryId { get; set; }
    }

    public sealed class FreshsalesDeal : FreshsalesEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("amount")] public decimal? Amount { get; set; }
        [JsonPropertyName("currency_id")] public long? CurrencyId { get; set; }
        [JsonPropertyName("deal_stage_id")] public long? StageId { get; set; }
        [JsonPropertyName("probability")] public int? Probability { get; set; }
        [JsonPropertyName("expected_close")] public DateTime? ExpectedClose { get; set; }
        [JsonPropertyName("owner_id")] public long? OwnerId { get; set; }
        [JsonPropertyName("sales_account_id")] public long? SalesAccountId { get; set; }
    }

    public sealed class FreshsalesTask : FreshsalesEntityBase
    {
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("due_at")] public DateTime? DueAt { get; set; }
        [JsonPropertyName("task_type_id")] public long? TaskTypeId { get; set; }
        [JsonPropertyName("owner_id")] public long? OwnerId { get; set; }
        [JsonPropertyName("completed")] public bool? Completed { get; set; }
        [JsonPropertyName("notes")] public string? Notes { get; set; }
    }

    public sealed class FreshsalesAppointment : FreshsalesEntityBase
    {
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("from_time")] public DateTime? FromTime { get; set; }
        [JsonPropertyName("to_time")] public DateTime? ToTime { get; set; }
        [JsonPropertyName("location")] public string? Location { get; set; }
        [JsonPropertyName("owner_id")] public long? OwnerId { get; set; }
    }

    public sealed class FreshsalesNote : FreshsalesEntityBase
    {
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("note_type_id")] public long? NoteTypeId { get; set; }
        [JsonPropertyName("owner_id")] public long? OwnerId { get; set; }
        [JsonPropertyName("parent_type")] public string? ParentType { get; set; }
        [JsonPropertyName("parent_id")] public long? ParentId { get; set; }
    }

    public sealed class FreshsalesProduct : FreshsalesEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("sku")] public string? Sku { get; set; }
        [JsonPropertyName("price")] public decimal? Price { get; set; }
        [JsonPropertyName("currency_id")] public long? CurrencyId { get; set; }
    }

    public sealed class FreshsalesSalesActivity : FreshsalesEntityBase
    {
        [JsonPropertyName("activity_type")] public string? ActivityType { get; set; }
        [JsonPropertyName("subject")] public string? Subject { get; set; }
        [JsonPropertyName("owner_id")] public long? OwnerId { get; set; }
        [JsonPropertyName("performed_at")] public DateTime? PerformedAt { get; set; }
    }

    public sealed class FreshsalesUser : FreshsalesEntityBase
    {
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("first_name")] public string? FirstName { get; set; }
        [JsonPropertyName("last_name")] public string? LastName { get; set; }
        [JsonPropertyName("role_name")] public string? RoleName { get; set; }
        [JsonPropertyName("timezone")] public string? TimeZone { get; set; }
    }

    public sealed class FreshsalesTerritory : FreshsalesEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("territory_type")] public string? TerritoryType { get; set; }
        [JsonPropertyName("parent_id")] public long? ParentId { get; set; }
    }

    public sealed class FreshsalesTeam : FreshsalesEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("team_type")] public string? TeamType { get; set; }
        [JsonPropertyName("supervisor_id")] public long? SupervisorId { get; set; }
    }

    public sealed class FreshsalesCurrency : FreshsalesEntityBase
    {
        [JsonPropertyName("code")] public string? Code { get; set; }
        [JsonPropertyName("exchange_rate")] public decimal? ExchangeRate { get; set; }
        [JsonPropertyName("precision")] public int? Precision { get; set; }
        [JsonPropertyName("symbol")] public string? Symbol { get; set; }
    }

    public sealed class FreshsalesEmail
    {
        [JsonPropertyName("value")] public string? Value { get; set; }
        [JsonPropertyName("label")] public string? Label { get; set; }
        [JsonPropertyName("is_primary")] public bool? IsPrimary { get; set; }
    }

    public sealed class FreshsalesLinks
    {
        [JsonPropertyName("conversations")] public string? Conversations { get; set; }
        [JsonPropertyName("activities")] public string? Activities { get; set; }
        [JsonPropertyName("appointments")] public string? Appointments { get; set; }
        [JsonPropertyName("notes")] public string? Notes { get; set; }
        [JsonPropertyName("tasks")] public string? Tasks { get; set; }
    }

    public sealed class FreshsalesContactMode
    {
        [JsonPropertyName("email")] public DateTime? Email { get; set; }
        [JsonPropertyName("phone")] public DateTime? Phone { get; set; }
        [JsonPropertyName("chat")] public DateTime? Chat { get; set; }
        [JsonPropertyName("sms")] public DateTime? Sms { get; set; }
        [JsonPropertyName("whatsapp")] public DateTime? Whatsapp { get; set; }
    }

    public sealed class FreshsalesPagination
    {
        [JsonPropertyName("per_page")] public int? PerPage { get; set; }
        [JsonPropertyName("current_page")] public int? CurrentPage { get; set; }
        [JsonPropertyName("total_pages")] public int? TotalPages { get; set; }
        [JsonPropertyName("total")] public int? Total { get; set; }
    }

    public sealed class FreshsalesMeta
    {
        [JsonPropertyName("pagination")] public FreshsalesPagination? Pagination { get; set; }
    }

    public sealed class FreshsalesResponse<T>
    {
        [JsonPropertyName("data")] public List<T> Data { get; set; } = new();
        [JsonPropertyName("meta")] public FreshsalesMeta? Meta { get; set; }
    }
}