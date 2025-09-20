// File: BeepDM/Connectors/Marketing/ConvertKitDataSource/Models/ConvertKitModels.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Marketing.ConvertKitDataSource.Models
{
    // Base
    public abstract class ConvertKitEntityBase
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; set; }

        [JsonIgnore] public IDataSource? DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : ConvertKitEntityBase { DataSource = ds; return (T)this; }
    }

    // ---------- Subscriber objects ----------

    public sealed class ConvertKitSubscriber : ConvertKitEntityBase
    {
        [JsonPropertyName("email_address")] public string? EmailAddress { get; set; }
        [JsonPropertyName("first_name")] public string? FirstName { get; set; }
        [JsonPropertyName("state")] public string? State { get; set; }
        [JsonPropertyName("fields")] public Dictionary<string, string>? Fields { get; set; }
        [JsonPropertyName("tags")] public List<ConvertKitTag>? Tags { get; set; }
    }

    // ---------- Tag objects ----------

    public sealed class ConvertKitTag : ConvertKitEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; set; }
    }

    // ---------- Sequence objects ----------

    public sealed class ConvertKitSequence : ConvertKitEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("email_count")] public int? EmailCount { get; set; }
        [JsonPropertyName("subscriber_count")] public int? SubscriberCount { get; set; }
        [JsonPropertyName("hold")] public bool? Hold { get; set; }
        [JsonPropertyName("repeat")] public bool? Repeat { get; set; }
    }

    // ---------- Form objects ----------

    public sealed class ConvertKitForm : ConvertKitEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("embed_js")] public string? EmbedJs { get; set; }
        [JsonPropertyName("embed_css")] public string? EmbedCss { get; set; }
        [JsonPropertyName("uid")] public string? Uid { get; set; }
        [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; set; }
    }

    // ---------- Broadcast objects ----------

    public sealed class ConvertKitBroadcast : ConvertKitEntityBase
    {
        [JsonPropertyName("subject")] public string? Subject { get; set; }
        [JsonPropertyName("content")] public string? Content { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("public")] public bool? Public { get; set; }
        [JsonPropertyName("published_at")] public DateTime? PublishedAt { get; set; }
        [JsonPropertyName("send_at")] public DateTime? SendAt { get; set; }
        [JsonPropertyName("thumbnail_url")] public string? ThumbnailUrl { get; set; }
        [JsonPropertyName("email_layout_template")] public string? EmailLayoutTemplate { get; set; }
    }

    // ---------- Webhook objects ----------

    public sealed class ConvertKitWebhook : ConvertKitEntityBase
    {
        [JsonPropertyName("target_url")] public string? TargetUrl { get; set; }
        [JsonPropertyName("event")] public string? Event { get; set; }
        [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; set; }
    }

    // ---------- Account objects ----------

    public sealed class ConvertKitAccount : ConvertKitEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("primary_email_address")] public string? PrimaryEmailAddress { get; set; }
        [JsonPropertyName("plan_type")] public string? PlanType { get; set; }
        [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; set; }
    }

    // ---------- Custom field objects ----------

    public sealed class ConvertKitCustomField : ConvertKitEntityBase
    {
        [JsonPropertyName("label")] public string? Label { get; set; }
        [JsonPropertyName("key")] public string? Key { get; set; }
        [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; set; }
    }

    // ---------- Purchase objects ----------

    public sealed class ConvertKitPurchase : ConvertKitEntityBase
    {
        [JsonPropertyName("transaction_id")] public string? TransactionId { get; set; }
        [JsonPropertyName("email_address")] public string? EmailAddress { get; set; }
        [JsonPropertyName("first_name")] public string? FirstName { get; set; }
        [JsonPropertyName("product_name")] public string? ProductName { get; set; }
        [JsonPropertyName("product_id")] public string? ProductId { get; set; }
        [JsonPropertyName("currency")] public string? Currency { get; set; }
        [JsonPropertyName("amount")] public decimal? Amount { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; set; }
    }
}