// File: Connectors/Gmail/Models/GmailModels.cs
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Gmail.Models
{
    // -------------------------------------------------------
    // Base
    // -------------------------------------------------------
    public abstract class GmailEntityBase
    {
        [JsonIgnore] public IDataSource DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : GmailEntityBase { DataSource = ds; return (T)this; }
    }

    // -------------------------------------------------------
    // Message (Email)
    // -------------------------------------------------------
    public sealed class GmailMessage : GmailEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("threadId")] public string ThreadId { get; set; }
        [JsonPropertyName("labelIds")] public List<string> LabelIds { get; set; }
        [JsonPropertyName("snippet")] public string Snippet { get; set; }
        [JsonPropertyName("payload")] public GmailMessagePayload Payload { get; set; }
        [JsonPropertyName("sizeEstimate")] public long SizeEstimate { get; set; }
        [JsonPropertyName("historyId")] public string HistoryId { get; set; }
        [JsonPropertyName("internalDate")] public string InternalDate { get; set; }
    }

    public sealed class GmailMessagePayload
    {
        [JsonPropertyName("partId")] public string PartId { get; set; }
        [JsonPropertyName("mimeType")] public string MimeType { get; set; }
        [JsonPropertyName("filename")] public string Filename { get; set; }
        [JsonPropertyName("headers")] public List<GmailHeader> Headers { get; set; }
        [JsonPropertyName("body")] public GmailBody Body { get; set; }
        [JsonPropertyName("parts")] public List<GmailMessagePart> Parts { get; set; }
    }

    public sealed class GmailHeader
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("value")] public string Value { get; set; }
    }

    public sealed class GmailBody
    {
        [JsonPropertyName("attachmentId")] public string AttachmentId { get; set; }
        [JsonPropertyName("size")] public int Size { get; set; }
        [JsonPropertyName("data")] public string Data { get; set; }
    }

    public sealed class GmailMessagePart
    {
        [JsonPropertyName("partId")] public string PartId { get; set; }
        [JsonPropertyName("mimeType")] public string MimeType { get; set; }
        [JsonPropertyName("filename")] public string Filename { get; set; }
        [JsonPropertyName("headers")] public List<GmailHeader> Headers { get; set; }
        [JsonPropertyName("body")] public GmailBody Body { get; set; }
        [JsonPropertyName("parts")] public List<GmailMessagePart> Parts { get; set; }
    }

    // -------------------------------------------------------
    // Thread
    // -------------------------------------------------------
    public sealed class GmailThread : GmailEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("snippet")] public string Snippet { get; set; }
        [JsonPropertyName("historyId")] public string HistoryId { get; set; }
        [JsonPropertyName("messages")] public List<GmailMessage> Messages { get; set; }
    }

    // -------------------------------------------------------
    // Label
    // -------------------------------------------------------
    public sealed class GmailLabel : GmailEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("messageListVisibility")] public string MessageListVisibility { get; set; }
        [JsonPropertyName("labelListVisibility")] public string LabelListVisibility { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("messagesTotal")] public int MessagesTotal { get; set; }
        [JsonPropertyName("messagesUnread")] public int MessagesUnread { get; set; }
        [JsonPropertyName("threadsTotal")] public int ThreadsTotal { get; set; }
        [JsonPropertyName("threadsUnread")] public int ThreadsUnread { get; set; }
    }

    // -------------------------------------------------------
    // Profile
    // -------------------------------------------------------
    public sealed class GmailProfile : GmailEntityBase
    {
        [JsonPropertyName("emailAddress")] public string EmailAddress { get; set; }
        [JsonPropertyName("messagesTotal")] public int MessagesTotal { get; set; }
        [JsonPropertyName("threadsTotal")] public int ThreadsTotal { get; set; }
        [JsonPropertyName("historyId")] public string HistoryId { get; set; }
    }

    // -------------------------------------------------------
    // Draft
    // -------------------------------------------------------
    public sealed class GmailDraft : GmailEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("message")] public GmailMessage Message { get; set; }
    }

    // -------------------------------------------------------
    // History
    // -------------------------------------------------------
    public sealed class GmailHistory : GmailEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("messages")] public List<GmailMessage> Messages { get; set; }
        [JsonPropertyName("messagesAdded")] public List<GmailMessage> MessagesAdded { get; set; }
        [JsonPropertyName("messagesDeleted")] public List<GmailMessage> MessagesDeleted { get; set; }
        [JsonPropertyName("labelsAdded")] public List<GmailLabel> LabelsAdded { get; set; }
        [JsonPropertyName("labelsRemoved")] public List<GmailLabel> LabelsRemoved { get; set; }
    }
}