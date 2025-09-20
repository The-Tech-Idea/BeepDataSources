// File: BeepDM/Connectors/Marketing/SendinblueDataSource/Models/SendinblueModels.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Marketing.SendinblueDataSource.Models
{
    // Base
    public abstract class SendinblueEntityBase
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("createdAt")] public DateTimeOffset? CreatedAt { get; set; }
        [JsonPropertyName("modifiedAt")] public DateTimeOffset? ModifiedAt { get; set; }

        [JsonIgnore] public IDataSource? DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : SendinblueEntityBase { DataSource = ds; return (T)this; }
    }

    // ---------- Contact objects ----------

    public sealed class SendinblueContact : SendinblueEntityBase
    {
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("emailBlacklisted")] public bool? EmailBlacklisted { get; set; }
        [JsonPropertyName("smsBlacklisted")] public bool? SmsBlacklisted { get; set; }
        [JsonPropertyName("listIds")] public List<long>? ListIds { get; set; } = new();
        [JsonPropertyName("listUnsubscribed")] public List<long>? ListUnsubscribed { get; set; } = new();
        [JsonPropertyName("attributes")] public Dictionary<string, object>? Attributes { get; set; }
        [JsonPropertyName("ext_id")] public string? ExternalId { get; set; }
    }

    public sealed class SendinblueContactAttribute
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("category")] public string? Category { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("enumeration")] public List<SendinblueAttributeEnumeration>? Enumeration { get; set; } = new();
        [JsonPropertyName("calculatedValue")] public string? CalculatedValue { get; set; }
    }

    public sealed class SendinblueAttributeEnumeration
    {
        [JsonPropertyName("value")] public int? Value { get; set; }
        [JsonPropertyName("label")] public string? Label { get; set; }
    }

    public sealed class SendinblueFolder
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("uniqueId")] public string? UniqueId { get; set; }
        [JsonPropertyName("isDynamic")] public bool? IsDynamic { get; set; }
    }

    public sealed class SendinblueList
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("totalBlacklisted")] public long? TotalBlacklisted { get; set; }
        [JsonPropertyName("totalSubscribers")] public long? TotalSubscribers { get; set; }
        [JsonPropertyName("uniqueSubscribers")] public long? UniqueSubscribers { get; set; }
        [JsonPropertyName("folderId")] public long? FolderId { get; set; }
    }

    // ---------- Campaign objects ----------

    public sealed class SendinblueEmailCampaign : SendinblueEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("subject")] public string? Subject { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("tag")] public string? Tag { get; set; }
        [JsonPropertyName("sender")] public SendinblueSender? Sender { get; set; }
        [JsonPropertyName("replyTo")] public string? ReplyTo { get; set; }
        [JsonPropertyName("recipients")] public SendinblueRecipients? Recipients { get; set; }
        [JsonPropertyName("statistics")] public SendinblueCampaignStatistics? Statistics { get; set; }
        [JsonPropertyName("inlineImageActivation")] public bool? InlineImageActivation { get; set; }
        [JsonPropertyName("mirrorActive")] public bool? MirrorActive { get; set; }
        [JsonPropertyName("recurring")] public bool? Recurring { get; set; }
        [JsonPropertyName("scheduledAt")] public DateTimeOffset? ScheduledAt { get; set; }
        [JsonPropertyName("abTesting")] public bool? AbTesting { get; set; }
        [JsonPropertyName("subjectA")] public string? SubjectA { get; set; }
        [JsonPropertyName("subjectB")] public string? SubjectB { get; set; }
        [JsonPropertyName("splitRule")] public int? SplitRule { get; set; }
        [JsonPropertyName("winnerCriteria")] public string? WinnerCriteria { get; set; }
        [JsonPropertyName("winnerDelay")] public int? WinnerDelay { get; set; }
        [JsonPropertyName("ipWarmupEnable")] public bool? IpWarmupEnable { get; set; }
        [JsonPropertyName("initialQuota")] public int? InitialQuota { get; set; }
        [JsonPropertyName("sendAtBestTime")] public bool? SendAtBestTime { get; set; }
        [JsonPropertyName("mirrors")] public List<string>? Mirrors { get; set; } = new();
        [JsonPropertyName("footer")] public string? Footer { get; set; }
        [JsonPropertyName("header")] public string? Header { get; set; }
        [JsonPropertyName("utmCampaign")] public string? UtmCampaign { get; set; }
        [JsonPropertyName("params")] public Dictionary<string, object>? Params { get; set; }
        [JsonPropertyName("htmlContent")] public string? HtmlContent { get; set; }
        [JsonPropertyName("templateId")] public long? TemplateId { get; set; }
    }

    public sealed class SendinblueSender
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("id")] public long? Id { get; set; }
    }

    public sealed class SendinblueRecipients
    {
        [JsonPropertyName("lists")] public List<long>? Lists { get; set; } = new();
        [JsonPropertyName("exclusionLists")] public List<long>? ExclusionLists { get; set; } = new();
    }

    public sealed class SendinblueCampaignStatistics
    {
        [JsonPropertyName("globalStats")] public SendinblueGlobalStats? GlobalStats { get; set; }
        [JsonPropertyName("campaignStats")] public List<SendinblueCampaignStat>? CampaignStats { get; set; } = new();
        [JsonPropertyName("mirrorClick")] public long? MirrorClick { get; set; }
        [JsonPropertyName("remaining")] public long? Remaining { get; set; }
        [JsonPropertyName("linksStats")] public List<SendinblueLinkStats>? LinksStats { get; set; } = new();
    }

    public sealed class SendinblueGlobalStats
    {
        [JsonPropertyName("uniqueClicks")] public long? UniqueClicks { get; set; }
        [JsonPropertyName("clickers")] public long? Clickers { get; set; }
        [JsonPropertyName("complaints")] public long? Complaints { get; set; }
        [JsonPropertyName("delivered")] public long? Delivered { get; set; }
        [JsonPropertyName("sent")] public long? Sent { get; set; }
        [JsonPropertyName("softBounces")] public long? SoftBounces { get; set; }
        [JsonPropertyName("hardBounces")] public long? HardBounces { get; set; }
        [JsonPropertyName("uniqueViews")] public long? UniqueViews { get; set; }
        [JsonPropertyName("trackableViews")] public long? TrackableViews { get; set; }
        [JsonPropertyName("unsubscriptions")] public long? Unsubscriptions { get; set; }
        [JsonPropertyName("viewed")] public long? Viewed { get; set; }
    }

    public sealed class SendinblueCampaignStat
    {
        [JsonPropertyName("listId")] public long? ListId { get; set; }
        [JsonPropertyName("uniqueClicks")] public long? UniqueClicks { get; set; }
        [JsonPropertyName("clickers")] public long? Clickers { get; set; }
        [JsonPropertyName("complaints")] public long? Complaints { get; set; }
        [JsonPropertyName("delivered")] public long? Delivered { get; set; }
        [JsonPropertyName("sent")] public long? Sent { get; set; }
        [JsonPropertyName("softBounces")] public long? SoftBounces { get; set; }
        [JsonPropertyName("hardBounces")] public long? HardBounces { get; set; }
        [JsonPropertyName("uniqueViews")] public long? UniqueViews { get; set; }
        [JsonPropertyName("trackableViews")] public long? TrackableViews { get; set; }
        [JsonPropertyName("unsubscriptions")] public long? Unsubscriptions { get; set; }
        [JsonPropertyName("viewed")] public long? Viewed { get; set; }
    }

    public sealed class SendinblueLinkStats
    {
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("totalClicks")] public long? TotalClicks { get; set; }
        [JsonPropertyName("uniqueClicks")] public long? UniqueClicks { get; set; }
        [JsonPropertyName("clickers")] public long? Clickers { get; set; }
    }

    // ---------- SMS Campaign objects ----------

    public sealed class SendinblueSmsCampaign : SendinblueEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("content")] public string? Content { get; set; }
        [JsonPropertyName("sender")] public string? Sender { get; set; }
        [JsonPropertyName("recipients")] public SendinblueSmsRecipients? Recipients { get; set; }
        [JsonPropertyName("statistics")] public SendinblueSmsStatistics? Statistics { get; set; }
        [JsonPropertyName("scheduledAt")] public DateTimeOffset? ScheduledAt { get; set; }
        [JsonPropertyName("unicodeEnabled")] public bool? UnicodeEnabled { get; set; }
    }

    public sealed class SendinblueSmsRecipients
    {
        [JsonPropertyName("lists")] public List<long>? Lists { get; set; } = new();
        [JsonPropertyName("exclusionLists")] public List<long>? ExclusionLists { get; set; } = new();
    }

    public sealed class SendinblueSmsStatistics
    {
        [JsonPropertyName("delivered")] public long? Delivered { get; set; }
        [JsonPropertyName("sent")] public long? Sent { get; set; }
        [JsonPropertyName("hardBounces")] public long? HardBounces { get; set; }
        [JsonPropertyName("softBounces")] public long? SoftBounces { get; set; }
        [JsonPropertyName("unsubscriptions")] public long? Unsubscriptions { get; set; }
        [JsonPropertyName("answered")] public long? Answered { get; set; }
    }

    // ---------- Template objects ----------

    public sealed class SendinblueTemplate : SendinblueEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("subject")] public string? Subject { get; set; }
        [JsonPropertyName("htmlContent")] public string? HtmlContent { get; set; }
        [JsonPropertyName("textContent")] public string? TextContent { get; set; }
        [JsonPropertyName("replyTo")] public string? ReplyTo { get; set; }
        [JsonPropertyName("toField")] public string? ToField { get; set; }
        [JsonPropertyName("tag")] public string? Tag { get; set; }
        [JsonPropertyName("isActive")] public bool? IsActive { get; set; }
        [JsonPropertyName("testSent")] public bool? TestSent { get; set; }
        [JsonPropertyName("sender")] public SendinblueSender? Sender { get; set; }
        [JsonPropertyName("attachment")] public List<SendinblueAttachment>? Attachment { get; set; } = new();
    }

    public sealed class SendinblueAttachment
    {
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
    }

    // ---------- Webhook objects ----------

    public sealed class SendinblueWebhook : SendinblueEntityBase
    {
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("events")] public List<string>? Events { get; set; } = new();
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("auth")] public SendinblueWebhookAuth? Auth { get; set; }
    }

    public sealed class SendinblueWebhookAuth
    {
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("username")] public string? Username { get; set; }
        [JsonPropertyName("password")] public string? Password { get; set; }
    }

    // ---------- Account objects ----------

    public sealed class SendinblueAccount
    {
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("firstName")] public string? FirstName { get; set; }
        [JsonPropertyName("lastName")] public string? LastName { get; set; }
        [JsonPropertyName("companyName")] public string? CompanyName { get; set; }
        [JsonPropertyName("address")] public SendinblueAccountAddress? Address { get; set; }
        [JsonPropertyName("plan")] public List<SendinbluePlan>? Plan { get; set; } = new();
        [JsonPropertyName("relay")] public SendinblueRelay? Relay { get; set; }
    }

    public sealed class SendinblueAccountAddress
    {
        [JsonPropertyName("streetAddress")] public string? StreetAddress { get; set; }
        [JsonPropertyName("city")] public string? City { get; set; }
        [JsonPropertyName("zipCode")] public string? ZipCode { get; set; }
        [JsonPropertyName("country")] public string? Country { get; set; }
    }

    public sealed class SendinbluePlan
    {
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("credits")] public long? Credits { get; set; }
        [JsonPropertyName("creditsType")] public string? CreditsType { get; set; }
    }

    public sealed class SendinblueRelay
    {
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
        [JsonPropertyName("data")] public SendinblueRelayData? Data { get; set; }
    }

    public sealed class SendinblueRelayData
    {
        [JsonPropertyName("userName")] public string? UserName { get; set; }
        [JsonPropertyName("relay")] public string? Relay { get; set; }
        [JsonPropertyName("port")] public int? Port { get; set; }
    }

    // ---------- Process objects ----------

    public sealed class SendinblueProcess : SendinblueEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
    }

    // ---------- Segment objects ----------

    public sealed class SendinblueSegment : SendinblueEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("category")] public string? Category { get; set; }
        [JsonPropertyName("folderId")] public long? FolderId { get; set; }
    }

    // ---------- Company objects ----------

    public sealed class SendinblueCompany : SendinblueEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("domain")] public string? Domain { get; set; }
        [JsonPropertyName("address")] public SendinblueCompanyAddress? Address { get; set; }
        [JsonPropertyName("attributes")] public Dictionary<string, object>? Attributes { get; set; }
    }

    public sealed class SendinblueCompanyAddress
    {
        [JsonPropertyName("streetAddress")] public string? StreetAddress { get; set; }
        [JsonPropertyName("city")] public string? City { get; set; }
        [JsonPropertyName("zipCode")] public string? ZipCode { get; set; }
        [JsonPropertyName("country")] public string? Country { get; set; }
    }

    // ---------- Deal objects ----------

    public sealed class SendinblueDeal : SendinblueEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("attributes")] public Dictionary<string, object>? Attributes { get; set; }
    }

    public sealed class SendinblueDealAttribute
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("enumeration")] public List<SendinblueAttributeEnumeration>? Enumeration { get; set; } = new();
    }

    // ---------- Task objects ----------

    public sealed class SendinblueTask : SendinblueEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("duration")] public long? Duration { get; set; }
        [JsonPropertyName("reminder")] public SendinblueReminder? Reminder { get; set; }
        [JsonPropertyName("assignees")] public List<SendinblueAssignee>? Assignees { get; set; } = new();
        [JsonPropertyName("contacts")] public List<SendinblueTaskContact>? Contacts { get; set; } = new();
        [JsonPropertyName("companies")] public List<SendinblueTaskCompany>? Companies { get; set; } = new();
        [JsonPropertyName("deals")] public List<SendinblueTaskDeal>? Deals { get; set; } = new();
    }

    public sealed class SendinblueReminder
    {
        [JsonPropertyName("date")] public DateTimeOffset? Date { get; set; }
    }

    public sealed class SendinblueAssignee
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
    }

    public sealed class SendinblueTaskContact
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
    }

    public sealed class SendinblueTaskCompany
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
    }

    public sealed class SendinblueTaskDeal
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
    }

    public sealed class SendinblueTaskType
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("color")] public string? Color { get; set; }
    }

    // ---------- Note objects ----------

    public sealed class SendinblueNote : SendinblueEntityBase
    {
        [JsonPropertyName("text")] public string? Text { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("contact")] public SendinblueNoteContact? Contact { get; set; }
        [JsonPropertyName("deal")] public SendinblueNoteDeal? Deal { get; set; }
        [JsonPropertyName("company")] public SendinblueNoteCompany? Company { get; set; }
    }

    public sealed class SendinblueNoteContact
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
    }

    public sealed class SendinblueNoteDeal
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
    }

    public sealed class SendinblueNoteCompany
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
    }

    // ---------- Event objects ----------

    public sealed class SendinblueEvent
    {
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("date")] public DateTimeOffset? Date { get; set; }
        [JsonPropertyName("event")] public string? Event { get; set; }
        [JsonPropertyName("ip")] public string? Ip { get; set; }
        [JsonPropertyName("ts")] public long? Timestamp { get; set; }
        [JsonPropertyName("ts_event")] public long? EventTimestamp { get; set; }
        [JsonPropertyName("ts_epoch")] public long? EpochTimestamp { get; set; }
        [JsonPropertyName("subject")] public string? Subject { get; set; }
        [JsonPropertyName("campaign_id")] public long? CampaignId { get; set; }
        [JsonPropertyName("tag")] public string? Tag { get; set; }
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("asc")] public bool? Asc { get; set; }
        [JsonPropertyName("user_agent")] public string? UserAgent { get; set; }
        [JsonPropertyName("template_id")] public long? TemplateId { get; set; }
        [JsonPropertyName("sending_ip")] public string? SendingIp { get; set; }
        [JsonPropertyName("ts_sent")] public long? SentTimestamp { get; set; }
        [JsonPropertyName("list_id")] public List<long>? ListId { get; set; } = new();
    }

    // ---------- Domain objects ----------

    public sealed class SendinblueDomain
    {
        [JsonPropertyName("domain")] public string? Domain { get; set; }
        [JsonPropertyName("verified")] public bool? Verified { get; set; }
        [JsonPropertyName("authenticated")] public bool? Authenticated { get; set; }
    }

    // ---------- Sender objects ----------

    public sealed class SendinblueSenderInfo
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("active")] public bool? Active { get; set; }
        [JsonPropertyName("ips")] public List<SendinblueSenderIp>? Ips { get; set; } = new();
    }

    public sealed class SendinblueSenderIp
    {
        [JsonPropertyName("ip")] public string? Ip { get; set; }
        [JsonPropertyName("domain")] public string? Domain { get; set; }
        [JsonPropertyName("weight")] public int? Weight { get; set; }
    }

    // ---------- IP objects ----------

    public sealed class SendinblueIp
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("ip")] public string? Ip { get; set; }
        [JsonPropertyName("domain")] public string? Domain { get; set; }
        [JsonPropertyName("weight")] public int? Weight { get; set; }
    }

    // ---------- Report objects ----------

    public sealed class SendinblueReport
    {
        [JsonPropertyName("date_from")] public DateTimeOffset? DateFrom { get; set; }
        [JsonPropertyName("date_to")] public DateTimeOffset? DateTo { get; set; }
        [JsonPropertyName("requests")] public long? Requests { get; set; }
        [JsonPropertyName("delivered")] public long? Delivered { get; set; }
        [JsonPropertyName("hardBounces")] public long? HardBounces { get; set; }
        [JsonPropertyName("softBounces")] public long? SoftBounces { get; set; }
        [JsonPropertyName("clicks")] public long? Clicks { get; set; }
        [JsonPropertyName("uniqueClicks")] public long? UniqueClicks { get; set; }
        [JsonPropertyName("opens")] public long? Opens { get; set; }
        [JsonPropertyName("uniqueOpens")] public long? UniqueOpens { get; set; }
        [JsonPropertyName("spamReports")] public long? SpamReports { get; set; }
        [JsonPropertyName("blocked")] public long? Blocked { get; set; }
        [JsonPropertyName("invalid")] public long? Invalid { get; set; }
        [JsonPropertyName("unsubscribed")] public long? Unsubscribed { get; set; }
    }
}