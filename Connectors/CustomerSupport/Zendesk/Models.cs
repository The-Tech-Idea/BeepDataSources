using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TheTechIdea.Beep.ZendeskDataSource
{
    /// <summary>
    /// Zendesk Ticket entity
    /// </summary>
    public class ZendeskTicket
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("external_id")]
        public string ExternalId { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("subject")]
        public string Subject { get; set; }

        [JsonPropertyName("raw_subject")]
        public string RawSubject { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("priority")]
        public string Priority { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("recipient")]
        public string Recipient { get; set; }

        [JsonPropertyName("requester_id")]
        public long? RequesterId { get; set; }

        [JsonPropertyName("submitter_id")]
        public long? SubmitterId { get; set; }

        [JsonPropertyName("assignee_id")]
        public long? AssigneeId { get; set; }

        [JsonPropertyName("organization_id")]
        public long? OrganizationId { get; set; }

        [JsonPropertyName("group_id")]
        public long? GroupId { get; set; }

        [JsonPropertyName("collaborator_ids")]
        public List<long> CollaboratorIds { get; set; }

        [JsonPropertyName("follower_ids")]
        public List<long> FollowerIds { get; set; }

        [JsonPropertyName("email_cc_ids")]
        public List<long> EmailCcIds { get; set; }

        [JsonPropertyName("forum_topic_id")]
        public long? ForumTopicId { get; set; }

        [JsonPropertyName("problem_id")]
        public long? ProblemId { get; set; }

        [JsonPropertyName("has_incidents")]
        public bool HasIncidents { get; set; }

        [JsonPropertyName("is_public")]
        public bool IsPublic { get; set; }

        [JsonPropertyName("due_at")]
        public DateTime? DueAt { get; set; }

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; }

        [JsonPropertyName("custom_fields")]
        public List<ZendeskCustomField> CustomFields { get; set; }

        [JsonPropertyName("satisfaction_rating")]
        public ZendeskSatisfactionRating SatisfactionRating { get; set; }

        [JsonPropertyName("sharing_agreement_ids")]
        public List<long> SharingAgreementIds { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Zendesk User entity
    /// </summary>
    public class ZendeskUser
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("time_zone")]
        public string TimeZone { get; set; }

        [JsonPropertyName("phone")]
        public string Phone { get; set; }

        [JsonPropertyName("shared_phone_number")]
        public bool? SharedPhoneNumber { get; set; }

        [JsonPropertyName("photo")]
        public ZendeskPhoto Photo { get; set; }

        [JsonPropertyName("locale_id")]
        public int? LocaleId { get; set; }

        [JsonPropertyName("locale")]
        public string Locale { get; set; }

        [JsonPropertyName("organization_id")]
        public long? OrganizationId { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("verified")]
        public bool? Verified { get; set; }

        [JsonPropertyName("external_id")]
        public string ExternalId { get; set; }

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; }

        [JsonPropertyName("alias")]
        public string Alias { get; set; }

        [JsonPropertyName("active")]
        public bool Active { get; set; }

        [JsonPropertyName("shared")]
        public bool Shared { get; set; }

        [JsonPropertyName("shared_agent")]
        public bool? SharedAgent { get; set; }

        [JsonPropertyName("last_login_at")]
        public DateTime? LastLoginAt { get; set; }

        [JsonPropertyName("two_factor_auth_enabled")]
        public bool? TwoFactorAuthEnabled { get; set; }

        [JsonPropertyName("signature")]
        public string Signature { get; set; }

        [JsonPropertyName("details")]
        public string Details { get; set; }

        [JsonPropertyName("notes")]
        public string Notes { get; set; }

        [JsonPropertyName("role_type")]
        public int? RoleType { get; set; }

        [JsonPropertyName("custom_role_id")]
        public long? CustomRoleId { get; set; }

        [JsonPropertyName("moderator")]
        public bool? Moderator { get; set; }

        [JsonPropertyName("ticket_restriction")]
        public string TicketRestriction { get; set; }

        [JsonPropertyName("only_private_comments")]
        public bool? OnlyPrivateComments { get; set; }

        [JsonPropertyName("restricted_agent")]
        public bool? RestrictedAgent { get; set; }

        [JsonPropertyName("suspended")]
        public bool Suspended { get; set; }

        [JsonPropertyName("chat_only")]
        public bool? ChatOnly { get; set; }

        [JsonPropertyName("default_group_id")]
        public long? DefaultGroupId { get; set; }

        [JsonPropertyName("report_csv")]
        public bool? ReportCsv { get; set; }

        [JsonPropertyName("user_fields")]
        public Dictionary<string, object> UserFields { get; set; }
    }

    /// <summary>
    /// Zendesk Organization entity
    /// </summary>
    public class ZendeskOrganization
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("external_id")]
        public string ExternalId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("domain_names")]
        public List<string> DomainNames { get; set; }

        [JsonPropertyName("details")]
        public string Details { get; set; }

        [JsonPropertyName("notes")]
        public string Notes { get; set; }

        [JsonPropertyName("group_id")]
        public long? GroupId { get; set; }

        [JsonPropertyName("shared_tickets")]
        public bool SharedTickets { get; set; }

        [JsonPropertyName("shared_comments")]
        public bool SharedComments { get; set; }

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; }

        [JsonPropertyName("organization_fields")]
        public Dictionary<string, object> OrganizationFields { get; set; }
    }

    /// <summary>
    /// Zendesk Group entity
    /// </summary>
    public class ZendeskGroup
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("default")]
        public bool IsDefault { get; set; }

        [JsonPropertyName("deleted")]
        public bool Deleted { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Zendesk Comment entity
    /// </summary>
    public class ZendeskComment
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("author_id")]
        public long AuthorId { get; set; }

        [JsonPropertyName("body")]
        public string Body { get; set; }

        [JsonPropertyName("html_body")]
        public string HtmlBody { get; set; }

        [JsonPropertyName("plain_body")]
        public string PlainBody { get; set; }

        [JsonPropertyName("public")]
        public bool IsPublic { get; set; }

        [JsonPropertyName("attachments")]
        public List<ZendeskAttachment> Attachments { get; set; }

        [JsonPropertyName("audit_id")]
        public long? AuditId { get; set; }

        [JsonPropertyName("via")]
        public ZendeskVia Via { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("metadata")]
        public ZendeskMetadata Metadata { get; set; }
    }

    /// <summary>
    /// Zendesk Macro entity
    /// </summary>
    public class ZendeskMacro
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("active")]
        public bool Active { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("actions")]
        public List<ZendeskMacroAction> Actions { get; set; }

        [JsonPropertyName("restriction")]
        public ZendeskRestriction Restriction { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }
    }

    /// <summary>
    /// Zendesk View entity
    /// </summary>
    public class ZendeskView
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("active")]
        public bool Active { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("position")]
        public int? Position { get; set; }

        [JsonPropertyName("execution")]
        public ZendeskViewExecution Execution { get; set; }

        [JsonPropertyName("conditions")]
        public ZendeskViewConditions Conditions { get; set; }

        [JsonPropertyName("restriction")]
        public ZendeskRestriction Restriction { get; set; }

        [JsonPropertyName("watchable")]
        public bool? Watchable { get; set; }
    }

    // Supporting classes
    public class ZendeskCustomField
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("value")]
        public object Value { get; set; }
    }

    public class ZendeskSatisfactionRating
    {
        [JsonPropertyName("score")]
        public string Score { get; set; }

        [JsonPropertyName("comment")]
        public string Comment { get; set; }
    }

    public class ZendeskPhoto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("filename")]
        public string Filename { get; set; }

        [JsonPropertyName("content_url")]
        public string ContentUrl { get; set; }

        [JsonPropertyName("content_type")]
        public string ContentType { get; set; }

        [JsonPropertyName("size")]
        public int Size { get; set; }

        [JsonPropertyName("thumbnails")]
        public List<ZendeskThumbnail> Thumbnails { get; set; }
    }

    public class ZendeskThumbnail
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("content_url")]
        public string ContentUrl { get; set; }

        [JsonPropertyName("filename")]
        public string Filename { get; set; }
    }

    public class ZendeskAttachment
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("filename")]
        public string Filename { get; set; }

        [JsonPropertyName("content_url")]
        public string ContentUrl { get; set; }

        [JsonPropertyName("content_type")]
        public string ContentType { get; set; }

        [JsonPropertyName("size")]
        public int Size { get; set; }

        [JsonPropertyName("thumbnails")]
        public List<ZendeskThumbnail> Thumbnails { get; set; }
    }

    public class ZendeskVia
    {
        [JsonPropertyName("channel")]
        public string Channel { get; set; }

        [JsonPropertyName("source")]
        public ZendeskViaSource Source { get; set; }
    }

    public class ZendeskViaSource
    {
        [JsonPropertyName("from")]
        public Dictionary<string, object> From { get; set; }

        [JsonPropertyName("to")]
        public Dictionary<string, object> To { get; set; }

        [JsonPropertyName("rel")]
        public string Rel { get; set; }
    }

    public class ZendeskMetadata
    {
        [JsonPropertyName("system")]
        public Dictionary<string, object> System { get; set; }

        [JsonPropertyName("custom")]
        public Dictionary<string, object> Custom { get; set; }
    }

    public class ZendeskMacroAction
    {
        [JsonPropertyName("field")]
        public string Field { get; set; }

        [JsonPropertyName("value")]
        public object Value { get; set; }
    }

    public class ZendeskRestriction
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("id")]
        public long? Id { get; set; }

        [JsonPropertyName("ids")]
        public List<long> Ids { get; set; }
    }

    public class ZendeskViewExecution
    {
        [JsonPropertyName("group_by")]
        public string GroupBy { get; set; }

        [JsonPropertyName("group_order")]
        public string GroupOrder { get; set; }

        [JsonPropertyName("sort_by")]
        public string SortBy { get; set; }

        [JsonPropertyName("sort_order")]
        public string SortOrder { get; set; }

        [JsonPropertyName("group")]
        public ZendeskViewGroup Group { get; set; }
    }

    public class ZendeskViewGroup
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("order")]
        public string Order { get; set; }
    }

    public class ZendeskViewConditions
    {
        [JsonPropertyName("all")]
        public List<ZendeskViewCondition> All { get; set; }

        [JsonPropertyName("any")]
        public List<ZendeskViewCondition> Any { get; set; }
    }

    public class ZendeskViewCondition
    {
        [JsonPropertyName("field")]
        public string Field { get; set; }

        [JsonPropertyName("operator")]
        public string Operator { get; set; }

        [JsonPropertyName("value")]
        public object Value { get; set; }
    }
}