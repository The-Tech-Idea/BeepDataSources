// File: BeepDM/Connectors/Marketing/MailerLiteDataSource/Models/MailerLiteModels.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Marketing.MailerLiteDataSource.Models
{
    // Base
    public abstract class MailerLiteEntityBase
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; set; }

        [JsonIgnore] public IDataSource? DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : MailerLiteEntityBase { DataSource = ds; return (T)this; }
    }

    // ---------- Subscriber objects ----------

    public sealed class MailerLiteSubscriber : MailerLiteEntityBase
    {
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("source")] public string? Source { get; set; }
        [JsonPropertyName("sent")] public int? Sent { get; set; }
        [JsonPropertyName("opens_count")] public int? OpensCount { get; set; }
        [JsonPropertyName("clicks_count")] public int? ClicksCount { get; set; }
        [JsonPropertyName("open_rate")] public double? OpenRate { get; set; }
        [JsonPropertyName("click_rate")] public double? ClickRate { get; set; }
        [JsonPropertyName("ip_address")] public string? IpAddress { get; set; }
        [JsonPropertyName("subscribed_at")] public DateTime? SubscribedAt { get; set; }
        [JsonPropertyName("unsubscribed_at")] public DateTime? UnsubscribedAt { get; set; }
        [JsonPropertyName("fields")] public Dictionary<string, string>? Fields { get; set; }
    }

    // ---------- Group objects ----------

    public sealed class MailerLiteGroup : MailerLiteEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("active_count")] public int? ActiveCount { get; set; }
        [JsonPropertyName("sent_count")] public int? SentCount { get; set; }
        [JsonPropertyName("opens_count")] public int? OpensCount { get; set; }
        [JsonPropertyName("clicks_count")] public int? ClicksCount { get; set; }
        [JsonPropertyName("unsubscribed_count")] public int? UnsubscribedCount { get; set; }
        [JsonPropertyName("unconfirmed_count")] public int? UnconfirmedCount { get; set; }
        [JsonPropertyName("bounced_count")] public int? BouncedCount { get; set; }
        [JsonPropertyName("junk_count")] public int? JunkCount { get; set; }
    }

    // ---------- Campaign objects ----------

    public sealed class MailerLiteCampaign : MailerLiteEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("subject")] public string? Subject { get; set; }
        [JsonPropertyName("from_name")] public string? FromName { get; set; }
        [JsonPropertyName("from")] public string? From { get; set; }
        [JsonPropertyName("reply_to")] public string? ReplyTo { get; set; }
        [JsonPropertyName("content")] public string? Content { get; set; }
        [JsonPropertyName("plaintext")] public string? Plaintext { get; set; }
        [JsonPropertyName("sent_at")] public DateTime? SentAt { get; set; }
        [JsonPropertyName("queued_at")] public DateTime? QueuedAt { get; set; }
        [JsonPropertyName("delivered_count")] public int? DeliveredCount { get; set; }
        [JsonPropertyName("opens_count")] public int? OpensCount { get; set; }
        [JsonPropertyName("clicks_count")] public int? ClicksCount { get; set; }
        [JsonPropertyName("unsubscribed_count")] public int? UnsubscribedCount { get; set; }
        [JsonPropertyName("bounced_count")] public int? BouncedCount { get; set; }
        [JsonPropertyName("complained_count")] public int? ComplainedCount { get; set; }
        [JsonPropertyName("open_rate")] public double? OpenRate { get; set; }
        [JsonPropertyName("click_rate")] public double? ClickRate { get; set; }
    }

    // ---------- Form objects ----------

    public sealed class MailerLiteForm : MailerLiteEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("language")] public string? Language { get; set; }
        [JsonPropertyName("embed_code")] public string? EmbedCode { get; set; }
        [JsonPropertyName("embed_url")] public string? EmbedUrl { get; set; }
        [JsonPropertyName("subscribers_count")] public int? SubscribersCount { get; set; }
        [JsonPropertyName("conversion_rate")] public double? ConversionRate { get; set; }
    }

    // ---------- Segment objects ----------

    public sealed class MailerLiteSegment : MailerLiteEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("count")] public int? Count { get; set; }
        [JsonPropertyName("rules")] public List<MailerLiteSegmentRule>? Rules { get; set; }
    }

    public sealed class MailerLiteSegmentRule
    {
        [JsonPropertyName("rule_type")] public string? RuleType { get; set; }
        [JsonPropertyName("field")] public string? Field { get; set; }
        [JsonPropertyName("operator")] public string? Operator { get; set; }
        [JsonPropertyName("value")] public string? Value { get; set; }
    }

    // ---------- Automation objects ----------

    public sealed class MailerLiteAutomation : MailerLiteEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("trigger_type")] public string? TriggerType { get; set; }
        [JsonPropertyName("trigger_data")] public Dictionary<string, object>? TriggerData { get; set; }
        [JsonPropertyName("stats")] public MailerLiteAutomationStats? Stats { get; set; }
    }

    public sealed class MailerLiteAutomationStats
    {
        [JsonPropertyName("sent")] public int? Sent { get; set; }
        [JsonPropertyName("opened")] public int? Opened { get; set; }
        [JsonPropertyName("clicked")] public int? Clicked { get; set; }
        [JsonPropertyName("completed")] public int? Completed { get; set; }
    }

    // ---------- Website objects ----------

    public sealed class MailerLiteWebsite : MailerLiteEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("subscribers_count")] public int? SubscribersCount { get; set; }
        [JsonPropertyName("conversion_rate")] public double? ConversionRate { get; set; }
    }

    // ---------- E-commerce objects ----------

    public sealed class MailerLiteEcommerceCustomer : MailerLiteEntityBase
    {
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("company")] public string? Company { get; set; }
        [JsonPropertyName("country")] public string? Country { get; set; }
        [JsonPropertyName("city")] public string? City { get; set; }
        [JsonPropertyName("phone")] public string? Phone { get; set; }
        [JsonPropertyName("orders_count")] public int? OrdersCount { get; set; }
        [JsonPropertyName("total_spent")] public decimal? TotalSpent { get; set; }
    }

    public sealed class MailerLiteEcommerceOrder : MailerLiteEntityBase
    {
        [JsonPropertyName("total")] public decimal? Total { get; set; }
        [JsonPropertyName("currency")] public string? Currency { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("customer")] public MailerLiteEcommerceCustomer? Customer { get; set; }
        [JsonPropertyName("items")] public List<MailerLiteEcommerceOrderItem>? Items { get; set; }
    }

    public sealed class MailerLiteEcommerceOrderItem
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("price")] public decimal? Price { get; set; }
        [JsonPropertyName("quantity")] public int? Quantity { get; set; }
        [JsonPropertyName("total")] public decimal? Total { get; set; }
        [JsonPropertyName("product")] public MailerLiteEcommerceProduct? Product { get; set; }
    }

    public sealed class MailerLiteEcommerceProduct : MailerLiteEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("price")] public decimal? Price { get; set; }
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("image")] public string? Image { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
    }

    // ---------- Account objects ----------

    public sealed class MailerLiteAccount : MailerLiteEntityBase
    {
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("country")] public string? Country { get; set; }
        [JsonPropertyName("timezone")] public string? Timezone { get; set; }
        [JsonPropertyName("subscribers_limit")] public int? SubscribersLimit { get; set; }
        [JsonPropertyName("subscribers_count")] public int? SubscribersCount { get; set; }
        [JsonPropertyName("plan")] public string? Plan { get; set; }
    }

    // ---------- Stats objects ----------

    public sealed class MailerLiteStats
    {
        [JsonPropertyName("subscribers")] public MailerLiteSubscriberStats? Subscribers { get; set; }
        [JsonPropertyName("campaigns")] public MailerLiteCampaignStats? Campaigns { get; set; }
        [JsonPropertyName("automations")] public MailerLiteAutomationStats? Automations { get; set; }
    }

    public sealed class MailerLiteSubscriberStats
    {
        [JsonPropertyName("active")] public int? Active { get; set; }
        [JsonPropertyName("unsubscribed")] public int? Unsubscribed { get; set; }
        [JsonPropertyName("bounced")] public int? Bounced { get; set; }
        [JsonPropertyName("junk")] public int? Junk { get; set; }
        [JsonPropertyName("unconfirmed")] public int? Unconfirmed { get; set; }
    }

    public sealed class MailerLiteCampaignStats
    {
        [JsonPropertyName("sent")] public int? Sent { get; set; }
        [JsonPropertyName("opened")] public int? Opened { get; set; }
        [JsonPropertyName("clicked")] public int? Clicked { get; set; }
        [JsonPropertyName("unsubscribed")] public int? Unsubscribed { get; set; }
        [JsonPropertyName("bounced")] public int? Bounced { get; set; }
        [JsonPropertyName("complained")] public int? Complained { get; set; }
    }
}