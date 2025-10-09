// File: Connectors/Outlook/Models/OutlookModels.cs
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Outlook.Models
{
    // -------------------------------------------------------
    // Base
    // -------------------------------------------------------
    public abstract class OutlookEntityBase
    {
        [JsonIgnore] public IDataSource DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : OutlookEntityBase { DataSource = ds; return (T)this; }
    }

    // -------------------------------------------------------
    // Message (Email)
    // -------------------------------------------------------
    public sealed class OutlookMessage : OutlookEntityBase
    {
        [JsonPropertyName("@odata.etag")] public string OdataEtag { get; set; }
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("createdDateTime")] public DateTimeOffset? CreatedDateTime { get; set; }
        [JsonPropertyName("lastModifiedDateTime")] public DateTimeOffset? LastModifiedDateTime { get; set; }
        [JsonPropertyName("changeKey")] public string ChangeKey { get; set; }
        [JsonPropertyName("categories")] public List<string> Categories { get; set; }
        [JsonPropertyName("receivedDateTime")] public DateTimeOffset? ReceivedDateTime { get; set; }
        [JsonPropertyName("sentDateTime")] public DateTimeOffset? SentDateTime { get; set; }
        [JsonPropertyName("hasAttachments")] public bool HasAttachments { get; set; }
        [JsonPropertyName("internetMessageId")] public string InternetMessageId { get; set; }
        [JsonPropertyName("subject")] public string Subject { get; set; }
        [JsonPropertyName("bodyPreview")] public string BodyPreview { get; set; }
        [JsonPropertyName("importance")] public string Importance { get; set; }
        [JsonPropertyName("parentFolderId")] public string ParentFolderId { get; set; }
        [JsonPropertyName("conversationId")] public string ConversationId { get; set; }
        [JsonPropertyName("conversationIndex")] public string ConversationIndex { get; set; }
        [JsonPropertyName("isDeliveryReceiptRequested")] public bool? IsDeliveryReceiptRequested { get; set; }
        [JsonPropertyName("isReadReceiptRequested")] public bool? IsReadReceiptRequested { get; set; }
        [JsonPropertyName("isRead")] public bool IsRead { get; set; }
        [JsonPropertyName("isDraft")] public bool IsDraft { get; set; }
        [JsonPropertyName("webLink")] public string WebLink { get; set; }
        [JsonPropertyName("inferenceClassification")] public string InferenceClassification { get; set; }
        [JsonPropertyName("body")] public OutlookItemBody Body { get; set; }
        [JsonPropertyName("sender")] public OutlookRecipient Sender { get; set; }
        [JsonPropertyName("from")] public OutlookRecipient From { get; set; }
        [JsonPropertyName("toRecipients")] public List<OutlookRecipient> ToRecipients { get; set; }
        [JsonPropertyName("ccRecipients")] public List<OutlookRecipient> CcRecipients { get; set; }
        [JsonPropertyName("bccRecipients")] public List<OutlookRecipient> BccRecipients { get; set; }
        [JsonPropertyName("replyTo")] public List<OutlookRecipient> ReplyTo { get; set; }
        [JsonPropertyName("flag")] public OutlookFollowupFlag Flag { get; set; }
        [JsonPropertyName("attachments")] public List<OutlookAttachment> Attachments { get; set; }
    }

    public sealed class OutlookItemBody
    {
        [JsonPropertyName("contentType")] public string ContentType { get; set; }
        [JsonPropertyName("content")] public string Content { get; set; }
    }

    public sealed class OutlookRecipient
    {
        [JsonPropertyName("emailAddress")] public OutlookEmailAddress EmailAddress { get; set; }
    }

    public sealed class OutlookEmailAddress
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("address")] public string Address { get; set; }
    }

    public sealed class OutlookFollowupFlag
    {
        [JsonPropertyName("flagStatus")] public string FlagStatus { get; set; }
    }

    public sealed class OutlookAttachment
    {
        [JsonPropertyName("@odata.type")] public string OdataType { get; set; }
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("lastModifiedDateTime")] public DateTimeOffset? LastModifiedDateTime { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("contentType")] public string ContentType { get; set; }
        [JsonPropertyName("size")] public int Size { get; set; }
        [JsonPropertyName("isInline")] public bool IsInline { get; set; }
        [JsonPropertyName("contentId")] public string ContentId { get; set; }
        [JsonPropertyName("contentLocation")] public string ContentLocation { get; set; }
        [JsonPropertyName("contentBytes")] public string ContentBytes { get; set; }
    }

    // -------------------------------------------------------
    // MailFolder
    // -------------------------------------------------------
    public sealed class OutlookMailFolder : OutlookEntityBase
    {
        [JsonPropertyName("@odata.etag")] public string OdataEtag { get; set; }
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("displayName")] public string DisplayName { get; set; }
        [JsonPropertyName("parentFolderId")] public string ParentFolderId { get; set; }
        [JsonPropertyName("childFolderCount")] public int ChildFolderCount { get; set; }
        [JsonPropertyName("unreadItemCount")] public int UnreadItemCount { get; set; }
        [JsonPropertyName("totalItemCount")] public int TotalItemCount { get; set; }
        [JsonPropertyName("wellKnownName")] public string WellKnownName { get; set; }
    }

    // -------------------------------------------------------
    // Contact
    // -------------------------------------------------------
    public sealed class OutlookContact : OutlookEntityBase
    {
        [JsonPropertyName("@odata.etag")] public string OdataEtag { get; set; }
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("createdDateTime")] public DateTimeOffset? CreatedDateTime { get; set; }
        [JsonPropertyName("lastModifiedDateTime")] public DateTimeOffset? LastModifiedDateTime { get; set; }
        [JsonPropertyName("changeKey")] public string ChangeKey { get; set; }
        [JsonPropertyName("categories")] public List<string> Categories { get; set; }
        [JsonPropertyName("parentFolderId")] public string ParentFolderId { get; set; }
        [JsonPropertyName("birthday")] public DateTimeOffset? Birthday { get; set; }
        [JsonPropertyName("fileAs")] public string FileAs { get; set; }
        [JsonPropertyName("displayName")] public string DisplayName { get; set; }
        [JsonPropertyName("givenName")] public string GivenName { get; set; }
        [JsonPropertyName("initials")] public string Initials { get; set; }
        [JsonPropertyName("middleName")] public string MiddleName { get; set; }
        [JsonPropertyName("nickName")] public string NickName { get; set; }
        [JsonPropertyName("surname")] public string Surname { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("yomiGivenName")] public string YomiGivenName { get; set; }
        [JsonPropertyName("yomiSurname")] public string YomiSurname { get; set; }
        [JsonPropertyName("yomiCompanyName")] public string YomiCompanyName { get; set; }
        [JsonPropertyName("generation")] public string Generation { get; set; }
        [JsonPropertyName("emailAddresses")] public List<OutlookEmailAddress> EmailAddresses { get; set; }
        [JsonPropertyName("websites")] public List<OutlookWebsite> Websites { get; set; }
        [JsonPropertyName("imAddresses")] public List<string> ImAddresses { get; set; }
        [JsonPropertyName("jobTitle")] public string JobTitle { get; set; }
        [JsonPropertyName("companyName")] public string CompanyName { get; set; }
        [JsonPropertyName("department")] public string Department { get; set; }
        [JsonPropertyName("officeLocation")] public string OfficeLocation { get; set; }
        [JsonPropertyName("profession")] public string Profession { get; set; }
        [JsonPropertyName("businessHomePage")] public string BusinessHomePage { get; set; }
        [JsonPropertyName("assistantName")] public string AssistantName { get; set; }
        [JsonPropertyName("manager")] public string Manager { get; set; }
        [JsonPropertyName("homePhones")] public List<string> HomePhones { get; set; }
        [JsonPropertyName("mobilePhone")] public string MobilePhone { get; set; }
        [JsonPropertyName("businessPhones")] public List<string> BusinessPhones { get; set; }
        [JsonPropertyName("homeAddress")] public OutlookPhysicalAddress HomeAddress { get; set; }
        [JsonPropertyName("businessAddress")] public OutlookPhysicalAddress BusinessAddress { get; set; }
        [JsonPropertyName("otherAddress")] public OutlookPhysicalAddress OtherAddress { get; set; }
        [JsonPropertyName("spouseName")] public string SpouseName { get; set; }
        [JsonPropertyName("personalNotes")] public string PersonalNotes { get; set; }
        [JsonPropertyName("children")] public List<string> Children { get; set; }
    }

    public sealed class OutlookWebsite
    {
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("address")] public string Address { get; set; }
        [JsonPropertyName("displayName")] public string DisplayName { get; set; }
    }

    public sealed class OutlookPhysicalAddress
    {
        [JsonPropertyName("street")] public string Street { get; set; }
        [JsonPropertyName("city")] public string City { get; set; }
        [JsonPropertyName("state")] public string State { get; set; }
        [JsonPropertyName("countryOrRegion")] public string CountryOrRegion { get; set; }
        [JsonPropertyName("postalCode")] public string PostalCode { get; set; }
    }

    // -------------------------------------------------------
    // Event (Calendar)
    // -------------------------------------------------------
    public sealed class OutlookEvent : OutlookEntityBase
    {
        [JsonPropertyName("@odata.etag")] public string OdataEtag { get; set; }
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("createdDateTime")] public DateTimeOffset? CreatedDateTime { get; set; }
        [JsonPropertyName("lastModifiedDateTime")] public DateTimeOffset? LastModifiedDateTime { get; set; }
        [JsonPropertyName("changeKey")] public string ChangeKey { get; set; }
        [JsonPropertyName("categories")] public List<string> Categories { get; set; }
        [JsonPropertyName("originalStartTimeZone")] public string OriginalStartTimeZone { get; set; }
        [JsonPropertyName("originalEndTimeZone")] public string OriginalEndTimeZone { get; set; }
        [JsonPropertyName("iCalUId")] public string ICalUId { get; set; }
        [JsonPropertyName("reminderMinutesBeforeStart")] public int ReminderMinutesBeforeStart { get; set; }
        [JsonPropertyName("isReminderOn")] public bool IsReminderOn { get; set; }
        [JsonPropertyName("hasAttachments")] public bool HasAttachments { get; set; }
        [JsonPropertyName("subject")] public string Subject { get; set; }
        [JsonPropertyName("bodyPreview")] public string BodyPreview { get; set; }
        [JsonPropertyName("importance")] public string Importance { get; set; }
        [JsonPropertyName("sensitivity")] public string Sensitivity { get; set; }
        [JsonPropertyName("isAllDay")] public bool IsAllDay { get; set; }
        [JsonPropertyName("isCancelled")] public bool IsCancelled { get; set; }
        [JsonPropertyName("isOrganizer")] public bool IsOrganizer { get; set; }
        [JsonPropertyName("responseRequested")] public bool ResponseRequested { get; set; }
        [JsonPropertyName("seriesMasterId")] public string SeriesMasterId { get; set; }
        [JsonPropertyName("showAs")] public string ShowAs { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("webLink")] public string WebLink { get; set; }
        [JsonPropertyName("onlineMeetingUrl")] public string OnlineMeetingUrl { get; set; }
        [JsonPropertyName("isOnlineMeeting")] public bool IsOnlineMeeting { get; set; }
        [JsonPropertyName("onlineMeetingProvider")] public string OnlineMeetingProvider { get; set; }
        [JsonPropertyName("allowNewTimeProposals")] public bool AllowNewTimeProposals { get; set; }
        [JsonPropertyName("occurrenceId")] public string OccurrenceId { get; set; }
        [JsonPropertyName("isDraft")] public bool IsDraft { get; set; }
        [JsonPropertyName("hideAttendees")] public bool HideAttendees { get; set; }
        [JsonPropertyName("responseStatus")] public OutlookResponseStatus ResponseStatus { get; set; }
        [JsonPropertyName("body")] public OutlookItemBody Body { get; set; }
        [JsonPropertyName("start")] public OutlookDateTimeTimeZone Start { get; set; }
        [JsonPropertyName("end")] public OutlookDateTimeTimeZone End { get; set; }
        [JsonPropertyName("location")] public OutlookLocation Location { get; set; }
        [JsonPropertyName("locations")] public List<OutlookLocation> Locations { get; set; }
        [JsonPropertyName("recurrence")] public OutlookPatternedRecurrence Recurrence { get; set; }
        [JsonPropertyName("attendees")] public List<OutlookAttendee> Attendees { get; set; }
        [JsonPropertyName("organizer")] public OutlookRecipient Organizer { get; set; }
        [JsonPropertyName("onlineMeeting")] public OutlookOnlineMeetingInfo OnlineMeeting { get; set; }
    }

    public sealed class OutlookResponseStatus
    {
        [JsonPropertyName("response")] public string Response { get; set; }
        [JsonPropertyName("time")] public DateTimeOffset? Time { get; set; }
    }

    public sealed class OutlookDateTimeTimeZone
    {
        [JsonPropertyName("dateTime")] public string DateTime { get; set; }
        [JsonPropertyName("timeZone")] public string TimeZone { get; set; }
    }

    public sealed class OutlookLocation
    {
        [JsonPropertyName("displayName")] public string DisplayName { get; set; }
        [JsonPropertyName("locationEmailAddress")] public string LocationEmailAddress { get; set; }
        [JsonPropertyName("locationUri")] public string LocationUri { get; set; }
        [JsonPropertyName("locationType")] public string LocationType { get; set; }
        [JsonPropertyName("uniqueId")] public string UniqueId { get; set; }
        [JsonPropertyName("uniqueIdType")] public string UniqueIdType { get; set; }
        [JsonPropertyName("address")] public OutlookPhysicalAddress Address { get; set; }
    }

    public sealed class OutlookPatternedRecurrence
    {
        [JsonPropertyName("pattern")] public OutlookRecurrencePattern Pattern { get; set; }
        [JsonPropertyName("range")] public OutlookRecurrenceRange Range { get; set; }
    }

    public sealed class OutlookRecurrencePattern
    {
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("interval")] public int Interval { get; set; }
        [JsonPropertyName("month")] public int Month { get; set; }
        [JsonPropertyName("dayOfMonth")] public int DayOfMonth { get; set; }
        [JsonPropertyName("daysOfWeek")] public List<string> DaysOfWeek { get; set; }
        [JsonPropertyName("firstDayOfWeek")] public string FirstDayOfWeek { get; set; }
        [JsonPropertyName("index")] public string Index { get; set; }
    }

    public sealed class OutlookRecurrenceRange
    {
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("startDate")] public DateTimeOffset? StartDate { get; set; }
        [JsonPropertyName("endDate")] public DateTimeOffset? EndDate { get; set; }
        [JsonPropertyName("recurrenceTimeZone")] public string RecurrenceTimeZone { get; set; }
        [JsonPropertyName("numberOfOccurrences")] public int NumberOfOccurrences { get; set; }
    }

    public sealed class OutlookAttendee
    {
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("status")] public OutlookResponseStatus Status { get; set; }
        [JsonPropertyName("emailAddress")] public OutlookEmailAddress EmailAddress { get; set; }
    }

    public sealed class OutlookOnlineMeetingInfo
    {
        [JsonPropertyName("joinUrl")] public string JoinUrl { get; set; }
        [JsonPropertyName("conferenceId")] public string ConferenceId { get; set; }
        [JsonPropertyName("tollNumber")] public string TollNumber { get; set; }
        [JsonPropertyName("tollFreeNumber")] public string TollFreeNumber { get; set; }
        [JsonPropertyName("phones")] public List<OutlookPhone> Phones { get; set; }
        [JsonPropertyName("quickDial")] public string QuickDial { get; set; }
    }

    public sealed class OutlookPhone
    {
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("number")] public string Number { get; set; }
    }
}