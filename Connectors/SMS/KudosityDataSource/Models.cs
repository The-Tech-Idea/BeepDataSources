using System.Text.Json.Serialization;

namespace TheTechIdea.Beep.Connectors.Kudosity.Models
{
    // Kudosity API Models
    public class KudositySMS
    {
        [JsonPropertyName("to")]
        public string To { get; set; }

        [JsonPropertyName("from")]
        public string From { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("campaign_id")]
        public string CampaignId { get; set; }

        [JsonPropertyName("scheduled_at")]
        public DateTime? ScheduledAt { get; set; }

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; }
    }

    public class KudositySMSResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("data")]
        public KudositySMSData Data { get; set; }

        [JsonPropertyName("errors")]
        public List<string> Errors { get; set; }
    }

    public class KudositySMSData
    {
        [JsonPropertyName("message_id")]
        public string MessageId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("cost")]
        public decimal Cost { get; set; }

        [JsonPropertyName("credits_used")]
        public int CreditsUsed { get; set; }

        [JsonPropertyName("recipients")]
        public List<KudosityRecipient> Recipients { get; set; }
    }

    public class KudosityRecipient
    {
        [JsonPropertyName("phone_number")]
        public string PhoneNumber { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("delivered_at")]
        public DateTime? DeliveredAt { get; set; }

        [JsonPropertyName("error_message")]
        public string ErrorMessage { get; set; }
    }

    public class KudosityCampaign
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("scheduled_at")]
        public DateTime? ScheduledAt { get; set; }

        [JsonPropertyName("sent_at")]
        public DateTime? SentAt { get; set; }

        [JsonPropertyName("total_recipients")]
        public int TotalRecipients { get; set; }

        [JsonPropertyName("delivered_count")]
        public int DeliveredCount { get; set; }

        [JsonPropertyName("failed_count")]
        public int FailedCount { get; set; }

        [JsonPropertyName("pending_count")]
        public int PendingCount { get; set; }

        [JsonPropertyName("total_cost")]
        public decimal TotalCost { get; set; }

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; }
    }

    public class KudosityContact
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("phone_number")]
        public string PhoneNumber { get; set; }

        [JsonPropertyName("first_name")]
        public string FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string LastName { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("country_code")]
        public string CountryCode { get; set; }

        [JsonPropertyName("carrier")]
        public string Carrier { get; set; }

        [JsonPropertyName("opt_in_status")]
        public string OptInStatus { get; set; }

        [JsonPropertyName("opt_in_date")]
        public DateTime? OptInDate { get; set; }

        [JsonPropertyName("opt_out_date")]
        public DateTime? OptOutDate { get; set; }

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; }

        [JsonPropertyName("custom_fields")]
        public Dictionary<string, string> CustomFields { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    public class KudosityContactList
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("contact_count")]
        public int ContactCount { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; }
    }

    public class KudosityMessageHistory
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("campaign_id")]
        public string CampaignId { get; set; }

        [JsonPropertyName("to")]
        public string To { get; set; }

        [JsonPropertyName("from")]
        public string From { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("delivered_at")]
        public DateTime? DeliveredAt { get; set; }

        [JsonPropertyName("failed_at")]
        public DateTime? FailedAt { get; set; }

        [JsonPropertyName("error_message")]
        public string ErrorMessage { get; set; }

        [JsonPropertyName("cost")]
        public decimal Cost { get; set; }

        [JsonPropertyName("credits_used")]
        public int CreditsUsed { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; }
    }

    public class KudosityAccount
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("first_name")]
        public string FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string LastName { get; set; }

        [JsonPropertyName("company")]
        public string Company { get; set; }

        [JsonPropertyName("timezone")]
        public string Timezone { get; set; }

        [JsonPropertyName("credits_balance")]
        public decimal CreditsBalance { get; set; }

        [JsonPropertyName("monthly_usage")]
        public decimal MonthlyUsage { get; set; }

        [JsonPropertyName("plan_name")]
        public string PlanName { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("last_login")]
        public DateTime? LastLogin { get; set; }
    }

    public class KudosityWebhook
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("events")]
        public List<string> Events { get; set; }

        [JsonPropertyName("active")]
        public bool Active { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    public class KudosityTemplate
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("variables")]
        public List<string> Variables { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    public class KudosityBulkSMSRequest
    {
        [JsonPropertyName("messages")]
        public List<KudositySMS> Messages { get; set; }

        [JsonPropertyName("campaign_name")]
        public string CampaignName { get; set; }

        [JsonPropertyName("scheduled_at")]
        public DateTime? ScheduledAt { get; set; }
    }

    public class KudosityPaginationResponse<T>
    {
        [JsonPropertyName("data")]
        public List<T> Data { get; set; }

        [JsonPropertyName("current_page")]
        public int CurrentPage { get; set; }

        [JsonPropertyName("last_page")]
        public int LastPage { get; set; }

        [JsonPropertyName("per_page")]
        public int PerPage { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("from")]
        public int From { get; set; }

        [JsonPropertyName("to")]
        public int To { get; set; }
    }
}