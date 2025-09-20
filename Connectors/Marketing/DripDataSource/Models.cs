// File: BeepDM/Connectors/Marketing/DripDataSource/Models/DripModels.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Marketing.DripDataSource.Models
{
    // Base
    public abstract class DripEntityBase
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; set; }

        [JsonIgnore] public IDataSource? DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : DripEntityBase { DataSource = ds; return (T)this; }
    }

    // ---------- Subscriber objects ----------

    public sealed class DripSubscriber : DripEntityBase
    {
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("first_name")] public string? FirstName { get; set; }
        [JsonPropertyName("last_name")] public string? LastName { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("time_zone")] public string? TimeZone { get; set; }
        [JsonPropertyName("ip_address")] public string? IpAddress { get; set; }
        [JsonPropertyName("user_agent")] public string? UserAgent { get; set; }
        [JsonPropertyName("lifetime_value")] public decimal? LifetimeValue { get; set; }
        [JsonPropertyName("original_referrer")] public string? OriginalReferrer { get; set; }
        [JsonPropertyName("landing_url")] public string? LandingUrl { get; set; }
        [JsonPropertyName("prospect")] public bool? Prospect { get; set; }
        [JsonPropertyName("lead_score")] public int? LeadScore { get; set; }
        [JsonPropertyName("tags")] public List<string>? Tags { get; set; }
        [JsonPropertyName("custom_fields")] public Dictionary<string, string>? CustomFields { get; set; }
    }

    // ---------- Campaign objects ----------

    public sealed class DripCampaign : DripEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("from_name")] public string? FromName { get; set; }
        [JsonPropertyName("from_email")] public string? FromEmail { get; set; }
        [JsonPropertyName("subject")] public string? Subject { get; set; }
        [JsonPropertyName("send_at")] public DateTime? SendAt { get; set; }
        [JsonPropertyName("reply_to_email")] public string? ReplyToEmail { get; set; }
        [JsonPropertyName("bcc_email")] public string? BccEmail { get; set; }
        [JsonPropertyName("html_body")] public string? HtmlBody { get; set; }
        [JsonPropertyName("text_body")] public string? TextBody { get; set; }
        [JsonPropertyName("api_template_id")] public string? ApiTemplateId { get; set; }
        [JsonPropertyName("template_id")] public string? TemplateId { get; set; }
    }

    // ---------- Tag objects ----------

    public sealed class DripTag : DripEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
    }

    // ---------- Workflow objects ----------

    public sealed class DripWorkflow : DripEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("trigger_type")] public string? TriggerType { get; set; }
        [JsonPropertyName("trigger_settings")] public Dictionary<string, object>? TriggerSettings { get; set; }
    }

    public sealed class DripWorkflowTrigger : DripEntityBase
    {
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("settings")] public Dictionary<string, object>? Settings { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
    }

    public sealed class DripWorkflowAction : DripEntityBase
    {
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("settings")] public Dictionary<string, object>? Settings { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
        [JsonPropertyName("position")] public int? Position { get; set; }
    }

    // ---------- Form objects ----------

    public sealed class DripForm : DripEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("headline")] public string? Headline { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("button_text")] public string? ButtonText { get; set; }
        [JsonPropertyName("button_color")] public string? ButtonColor { get; set; }
        [JsonPropertyName("button_text_color")] public string? ButtonTextColor { get; set; }
        [JsonPropertyName("ends_at")] public DateTime? EndsAt { get; set; }
        [JsonPropertyName("thank_you_message")] public string? ThankYouMessage { get; set; }
        [JsonPropertyName("redirect_url")] public string? RedirectUrl { get; set; }
        [JsonPropertyName("embed_code")] public string? EmbedCode { get; set; }
        [JsonPropertyName("url")] public string? Url { get; set; }
    }

    public sealed class DripFormSubmission : DripEntityBase
    {
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("first_name")] public string? FirstName { get; set; }
        [JsonPropertyName("last_name")] public string? LastName { get; set; }
        [JsonPropertyName("fields")] public Dictionary<string, string>? Fields { get; set; }
        [JsonPropertyName("ip_address")] public string? IpAddress { get; set; }
        [JsonPropertyName("user_agent")] public string? UserAgent { get; set; }
    }

    // ---------- Account objects ----------

    public sealed class DripAccount : DripEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("default_from_name")] public string? DefaultFromName { get; set; }
        [JsonPropertyName("default_from_email")] public string? DefaultFromEmail { get; set; }
        [JsonPropertyName("default_postal_address")] public string? DefaultPostalAddress { get; set; }
    }

    // ---------- Custom field objects ----------

    public sealed class DripCustomField : DripEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("choices")] public List<string>? Choices { get; set; }
    }

    // ---------- Event objects ----------

    public sealed class DripEvent : DripEntityBase
    {
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("action")] public string? Action { get; set; }
        [JsonPropertyName("properties")] public Dictionary<string, object>? Properties { get; set; }
        [JsonPropertyName("occurred_at")] public DateTime? OccurredAt { get; set; }
    }

    // ---------- Webhook objects ----------

    public sealed class DripWebhook : DripEntityBase
    {
        [JsonPropertyName("post_url")] public string? PostUrl { get; set; }
        [JsonPropertyName("include_received_email")] public bool? IncludeReceivedEmail { get; set; }
        [JsonPropertyName("events")] public List<string>? Events { get; set; }
    }

    // ---------- Report objects ----------

    public sealed class DripReport : DripEntityBase
    {
        [JsonPropertyName("opens")] public int? Opens { get; set; }
        [JsonPropertyName("clicks")] public int? Clicks { get; set; }
        [JsonPropertyName("sends")] public int? Sends { get; set; }
        [JsonPropertyName("complaints")] public int? Complaints { get; set; }
        [JsonPropertyName("unsubscribes")] public int? Unsubscribes { get; set; }
        [JsonPropertyName("bounces")] public int? Bounces { get; set; }
        [JsonPropertyName("revenue")] public decimal? Revenue { get; set; }
    }
}