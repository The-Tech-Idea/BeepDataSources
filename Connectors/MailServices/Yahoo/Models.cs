// File: Connectors/Yahoo/Models/YahooModels.cs
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Yahoo.Models
{
    // -------------------------------------------------------
    // Base
    // -------------------------------------------------------
    public abstract class YahooEntityBase
    {
        [JsonIgnore] public IDataSource DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : YahooEntityBase { DataSource = ds; return (T)this; }
    }

    // -------------------------------------------------------
    // Message (Email)
    // -------------------------------------------------------
    public sealed class YahooMessage : YahooEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("messageId")] public string MessageId { get; set; }
        [JsonPropertyName("threadId")] public string ThreadId { get; set; }
        [JsonPropertyName("subject")] public string Subject { get; set; }
        [JsonPropertyName("from")] public YahooEmailAddress From { get; set; }
        [JsonPropertyName("to")] public List<YahooEmailAddress> To { get; set; }
        [JsonPropertyName("cc")] public List<YahooEmailAddress> Cc { get; set; }
        [JsonPropertyName("bcc")] public List<YahooEmailAddress> Bcc { get; set; }
        [JsonPropertyName("replyTo")] public List<YahooEmailAddress> ReplyTo { get; set; }
        [JsonPropertyName("receivedDate")] public long ReceivedDate { get; set; }
        [JsonPropertyName("sentDate")] public long SentDate { get; set; }
        [JsonPropertyName("size")] public int Size { get; set; }
        [JsonPropertyName("partIds")] public List<string> PartIds { get; set; }
        [JsonPropertyName("mimeType")] public string MimeType { get; set; }
        [JsonPropertyName("body")] public YahooMessageBody Body { get; set; }
        [JsonPropertyName("headers")] public List<YahooHeader> Headers { get; set; }
        [JsonPropertyName("attachments")] public List<YahooAttachment> Attachments { get; set; }
        [JsonPropertyName("flags")] public YahooMessageFlags Flags { get; set; }
    }

    public sealed class YahooEmailAddress
    {
        [JsonPropertyName("email")] public string Email { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
    }

    public sealed class YahooMessageBody
    {
        [JsonPropertyName("text")] public string Text { get; set; }
        [JsonPropertyName("html")] public string Html { get; set; }
    }

    public sealed class YahooHeader
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("value")] public string Value { get; set; }
    }

    public sealed class YahooAttachment
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("filename")] public string Filename { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("size")] public int Size { get; set; }
        [JsonPropertyName("contentId")] public string ContentId { get; set; }
    }

    public sealed class YahooMessageFlags
    {
        [JsonPropertyName("isRead")] public bool IsRead { get; set; }
        [JsonPropertyName("isStarred")] public bool IsStarred { get; set; }
        [JsonPropertyName("isAnswered")] public bool IsAnswered { get; set; }
        [JsonPropertyName("isForwarded")] public bool IsForwarded { get; set; }
        [JsonPropertyName("isDeleted")] public bool IsDeleted { get; set; }
        [JsonPropertyName("isDraft")] public bool IsDraft { get; set; }
    }

    // -------------------------------------------------------
    // Contact
    // -------------------------------------------------------
    public sealed class YahooContact : YahooEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public YahooContactName Name { get; set; }
        [JsonPropertyName("emails")] public List<YahooContactEmail> Emails { get; set; }
        [JsonPropertyName("phones")] public List<YahooContactPhone> Phones { get; set; }
        [JsonPropertyName("addresses")] public List<YahooContactAddress> Addresses { get; set; }
        [JsonPropertyName("company")] public string Company { get; set; }
        [JsonPropertyName("jobTitle")] public string JobTitle { get; set; }
        [JsonPropertyName("notes")] public string Notes { get; set; }
        [JsonPropertyName("created")] public long Created { get; set; }
        [JsonPropertyName("updated")] public long Updated { get; set; }
    }

    public sealed class YahooContactName
    {
        [JsonPropertyName("givenName")] public string GivenName { get; set; }
        [JsonPropertyName("middleName")] public string MiddleName { get; set; }
        [JsonPropertyName("familyName")] public string FamilyName { get; set; }
        [JsonPropertyName("prefix")] public string Prefix { get; set; }
        [JsonPropertyName("suffix")] public string Suffix { get; set; }
        [JsonPropertyName("displayName")] public string DisplayName { get; set; }
    }

    public sealed class YahooContactEmail
    {
        [JsonPropertyName("address")] public string Address { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("primary")] public bool Primary { get; set; }
    }

    public sealed class YahooContactPhone
    {
        [JsonPropertyName("number")] public string Number { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("primary")] public bool Primary { get; set; }
    }

    public sealed class YahooContactAddress
    {
        [JsonPropertyName("street")] public string Street { get; set; }
        [JsonPropertyName("city")] public string City { get; set; }
        [JsonPropertyName("stateOrProvince")] public string StateOrProvince { get; set; }
        [JsonPropertyName("postalCode")] public string PostalCode { get; set; }
        [JsonPropertyName("country")] public string Country { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("primary")] public bool Primary { get; set; }
    }

    // -------------------------------------------------------
    // Folder
    // -------------------------------------------------------
    public sealed class YahooFolder : YahooEntityBase
    {
        [JsonPropertyName("folderId")] public string FolderId { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("total")] public int Total { get; set; }
        [JsonPropertyName("unread")] public int Unread { get; set; }
    }
}