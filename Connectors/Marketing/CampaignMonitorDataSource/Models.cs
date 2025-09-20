// File: BeepDM/Connectors/Marketing/CampaignMonitorDataSource/Models/CampaignMonitorModels.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Marketing.CampaignMonitorDataSource.Models
{
    // Base
    public abstract class CampaignMonitorEntityBase
    {
        [JsonPropertyName("Date")] public DateTimeOffset? Date { get; set; }

        [JsonIgnore] public IDataSource? DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : CampaignMonitorEntityBase { DataSource = ds; return (T)this; }
    }

    // ---------- Client objects ----------

    public sealed class CampaignMonitorClient
    {
        [JsonPropertyName("ClientID")] public string? ClientId { get; set; }
        [JsonPropertyName("Name")] public string? Name { get; set; }
        [JsonPropertyName("CompanyName")] public string? CompanyName { get; set; }
        [JsonPropertyName("ContactName")] public string? ContactName { get; set; }
        [JsonPropertyName("EmailAddress")] public string? EmailAddress { get; set; }
        [JsonPropertyName("Country")] public string? Country { get; set; }
        [JsonPropertyName("TimeZone")] public string? TimeZone { get; set; }
    }

    // ---------- List objects ----------

    public sealed class CampaignMonitorList
    {
        [JsonPropertyName("ListID")] public string? ListId { get; set; }
        [JsonPropertyName("Name")] public string? Name { get; set; }
        [JsonPropertyName("CreatedDate")] public DateTimeOffset? CreatedDate { get; set; }
        [JsonPropertyName("UnsubscribePage")] public string? UnsubscribePage { get; set; }
        [JsonPropertyName("UnsubscribeSetting")] public string? UnsubscribeSetting { get; set; }
        [JsonPropertyName("ConfirmedOptIn")] public bool? ConfirmedOptIn { get; set; }
        [JsonPropertyName("ConfirmationSuccessPage")] public string? ConfirmationSuccessPage { get; set; }
    }

    public sealed class CampaignMonitorListStats
    {
        [JsonPropertyName("TotalActiveSubscribers")] public long? TotalActiveSubscribers { get; set; }
        [JsonPropertyName("TotalUnsubscribes")] public long? TotalUnsubscribes { get; set; }
        [JsonPropertyName("TotalDeleted")] public long? TotalDeleted { get; set; }
        [JsonPropertyName("TotalBounces")] public long? TotalBounces { get; set; }
        [JsonPropertyName("TotalComplaints")] public long? TotalComplaints { get; set; }
        [JsonPropertyName("TotalActiveSubscribersToday")] public long? TotalActiveSubscribersToday { get; set; }
        [JsonPropertyName("TotalUnsubscribesToday")] public long? TotalUnsubscribesToday { get; set; }
        [JsonPropertyName("TotalDeletedToday")] public long? TotalDeletedToday { get; set; }
        [JsonPropertyName("TotalBouncesToday")] public long? TotalBouncesToday { get; set; }
        [JsonPropertyName("TotalComplaintsToday")] public long? TotalComplaintsToday { get; set; }
        [JsonPropertyName("TotalNewSubscribersToday")] public long? TotalNewSubscribersToday { get; set; }
    }

    public sealed class CampaignMonitorCustomField
    {
        [JsonPropertyName("FieldName")] public string? FieldName { get; set; }
        [JsonPropertyName("Key")] public string? Key { get; set; }
        [JsonPropertyName("DataType")] public string? DataType { get; set; }
        [JsonPropertyName("FieldOptions")] public List<string>? FieldOptions { get; set; } = new();
        [JsonPropertyName("VisibleInPreferenceCenter")] public bool? VisibleInPreferenceCenter { get; set; }
    }

    // ---------- Subscriber objects ----------

    public sealed class CampaignMonitorSubscriber
    {
        [JsonPropertyName("EmailAddress")] public string? EmailAddress { get; set; }
        [JsonPropertyName("Name")] public string? Name { get; set; }
        [JsonPropertyName("Date")] public DateTimeOffset? Date { get; set; }
        [JsonPropertyName("State")] public string? State { get; set; }
        [JsonPropertyName("CustomFields")] public List<CampaignMonitorSubscriberCustomField>? CustomFields { get; set; } = new();
        [JsonPropertyName("ReadsEmailWith")] public string? ReadsEmailWith { get; set; }
    }

    public sealed class CampaignMonitorSubscriberCustomField
    {
        [JsonPropertyName("Key")] public string? Key { get; set; }
        [JsonPropertyName("Value")] public string? Value { get; set; }
    }

    public sealed class CampaignMonitorSubscriberHistory
    {
        [JsonPropertyName("Type")] public string? Type { get; set; }
        [JsonPropertyName("Name")] public string? Name { get; set; }
        [JsonPropertyName("Actions")] public List<CampaignMonitorSubscriberAction>? Actions { get; set; } = new();
    }

    public sealed class CampaignMonitorSubscriberAction
    {
        [JsonPropertyName("Event")] public string? Event { get; set; }
        [JsonPropertyName("Date")] public DateTimeOffset? Date { get; set; }
        [JsonPropertyName("IPAddress")] public string? IpAddress { get; set; }
        [JsonPropertyName("Detail")] public string? Detail { get; set; }
    }

    // ---------- Campaign objects ----------

    public sealed class CampaignMonitorCampaign
    {
        [JsonPropertyName("CampaignID")] public string? CampaignId { get; set; }
        [JsonPropertyName("Name")] public string? Name { get; set; }
        [JsonPropertyName("Subject")] public string? Subject { get; set; }
        [JsonPropertyName("FromName")] public string? FromName { get; set; }
        [JsonPropertyName("FromEmail")] public string? FromEmail { get; set; }
        [JsonPropertyName("ReplyTo")] public string? ReplyTo { get; set; }
        [JsonPropertyName("SentDate")] public DateTimeOffset? SentDate { get; set; }
        [JsonPropertyName("TotalRecipients")] public long? TotalRecipients { get; set; }
        [JsonPropertyName("WebVersionURL")] public string? WebVersionUrl { get; set; }
        [JsonPropertyName("WebVersionTextURL")] public string? WebVersionTextUrl { get; set; }
    }

    public sealed class CampaignMonitorCampaignSummary
    {
        [JsonPropertyName("Recipients")] public long? Recipients { get; set; }
        [JsonPropertyName("TotalOpened")] public long? TotalOpened { get; set; }
        [JsonPropertyName("Clicks")] public long? Clicks { get; set; }
        [JsonPropertyName("Unsubscribed")] public long? Unsubscribed { get; set; }
        [JsonPropertyName("Bounced")] public long? Bounced { get; set; }
        [JsonPropertyName("UniqueOpened")] public long? UniqueOpened { get; set; }
        [JsonPropertyName("WebVersionURL")] public string? WebVersionUrl { get; set; }
        [JsonPropertyName("WebVersionTextURL")] public string? WebVersionTextUrl { get; set; }
        [JsonPropertyName("WorldviewURL")] public string? WorldviewUrl { get; set; }
        [JsonPropertyName("ForwardToFriendURL")] public string? ForwardToFriendUrl { get; set; }
        [JsonPropertyName("FacebookLikeURL")] public string? FacebookLikeUrl { get; set; }
        [JsonPropertyName("TwitterTweetURL")] public string? TwitterTweetUrl { get; set; }
    }

    public sealed class CampaignMonitorCampaignRecipient
    {
        [JsonPropertyName("ListID")] public string? ListId { get; set; }
        [JsonPropertyName("Name")] public string? Name { get; set; }
    }

    public sealed class CampaignMonitorCampaignOpen
    {
        [JsonPropertyName("EmailAddress")] public string? EmailAddress { get; set; }
        [JsonPropertyName("ListID")] public string? ListId { get; set; }
        [JsonPropertyName("Date")] public DateTimeOffset? Date { get; set; }
        [JsonPropertyName("IPAddress")] public string? IpAddress { get; set; }
        [JsonPropertyName("Latitude")] public double? Latitude { get; set; }
        [JsonPropertyName("Longitude")] public double? Longitude { get; set; }
        [JsonPropertyName("City")] public string? City { get; set; }
        [JsonPropertyName("Region")] public string? Region { get; set; }
        [JsonPropertyName("CountryCode")] public string? CountryCode { get; set; }
        [JsonPropertyName("CountryName")] public string? CountryName { get; set; }
    }

    public sealed class CampaignMonitorCampaignClick
    {
        [JsonPropertyName("EmailAddress")] public string? EmailAddress { get; set; }
        [JsonPropertyName("ListID")] public string? ListId { get; set; }
        [JsonPropertyName("Date")] public DateTimeOffset? Date { get; set; }
        [JsonPropertyName("IPAddress")] public string? IpAddress { get; set; }
        [JsonPropertyName("URL")] public string? Url { get; set; }
        [JsonPropertyName("Latitude")] public double? Latitude { get; set; }
        [JsonPropertyName("Longitude")] public double? Longitude { get; set; }
        [JsonPropertyName("City")] public string? City { get; set; }
        [JsonPropertyName("Region")] public string? Region { get; set; }
        [JsonPropertyName("CountryCode")] public string? CountryCode { get; set; }
        [JsonPropertyName("CountryName")] public string? CountryName { get; set; }
    }

    public sealed class CampaignMonitorCampaignUnsubscribe
    {
        [JsonPropertyName("EmailAddress")] public string? EmailAddress { get; set; }
        [JsonPropertyName("ListID")] public string? ListId { get; set; }
        [JsonPropertyName("Date")] public DateTimeOffset? Date { get; set; }
        [JsonPropertyName("IPAddress")] public string? IpAddress { get; set; }
    }

    public sealed class CampaignMonitorCampaignBounce
    {
        [JsonPropertyName("EmailAddress")] public string? EmailAddress { get; set; }
        [JsonPropertyName("ListID")] public string? ListId { get; set; }
        [JsonPropertyName("BounceType")] public string? BounceType { get; set; }
        [JsonPropertyName("Date")] public DateTimeOffset? Date { get; set; }
        [JsonPropertyName("Reason")] public string? Reason { get; set; }
    }

    // ---------- Template objects ----------

    public sealed class CampaignMonitorTemplate
    {
        [JsonPropertyName("TemplateID")] public string? TemplateId { get; set; }
        [JsonPropertyName("Name")] public string? Name { get; set; }
        [JsonPropertyName("PreviewURL")] public string? PreviewUrl { get; set; }
        [JsonPropertyName("ScreenshotURL")] public string? ScreenshotUrl { get; set; }
    }

    // ---------- Segment objects ----------

    public sealed class CampaignMonitorSegment
    {
        [JsonPropertyName("SegmentID")] public string? SegmentId { get; set; }
        [JsonPropertyName("Title")] public string? Title { get; set; }
        [JsonPropertyName("ActiveSubscribers")] public long? ActiveSubscribers { get; set; }
    }

    // ---------- Administrator objects ----------

    public sealed class CampaignMonitorAdministrator
    {
        [JsonPropertyName("EmailAddress")] public string? EmailAddress { get; set; }
        [JsonPropertyName("Name")] public string? Name { get; set; }
        [JsonPropertyName("AccessLevel")] public int? AccessLevel { get; set; }
        [JsonPropertyName("Status")] public string? Status { get; set; }
    }

    // ---------- Suppression objects ----------

    public sealed class CampaignMonitorSuppression
    {
        [JsonPropertyName("SuppressionReason")] public string? SuppressionReason { get; set; }
        [JsonPropertyName("EmailAddress")] public string? EmailAddress { get; set; }
        [JsonPropertyName("Date")] public DateTimeOffset? Date { get; set; }
        [JsonPropertyName("State")] public string? State { get; set; }
    }

    // ---------- Journey objects ----------

    public sealed class CampaignMonitorJourney
    {
        [JsonPropertyName("JourneyID")] public string? JourneyId { get; set; }
        [JsonPropertyName("Name")] public string? Name { get; set; }
        [JsonPropertyName("Status")] public string? Status { get; set; }
        [JsonPropertyName("TriggerType")] public string? TriggerType { get; set; }
    }

    // ---------- Account objects ----------

    public sealed class CampaignMonitorAccount
    {
        [JsonPropertyName("EmailAddress")] public string? EmailAddress { get; set; }
        [JsonPropertyName("Name")] public string? Name { get; set; }
        [JsonPropertyName("AccessLevel")] public int? AccessLevel { get; set; }
        [JsonPropertyName("ApiKey")] public string? ApiKey { get; set; }
        [JsonPropertyName("AccountTimezone")] public string? AccountTimezone { get; set; }
    }

    // ---------- Timezone objects ----------

    public sealed class CampaignMonitorTimezone
    {
        [JsonPropertyName("Timezone")] public string? Timezone { get; set; }
        [JsonPropertyName("Description")] public string? Description { get; set; }
    }

    // ---------- Country objects ----------

    public sealed class CampaignMonitorCountry
    {
        [JsonPropertyName("Code")] public string? Code { get; set; }
        [JsonPropertyName("Name")] public string? Name { get; set; }
    }
}