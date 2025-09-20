using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TheTechIdea.Beep.FreshdeskDataSource
{
    /// <summary>
    /// Freshdesk Ticket entity
    /// </summary>
    public class FreshdeskTicket
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("subject")]
        public string Subject { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("description_text")]
        public string DescriptionText { get; set; }

        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("priority")]
        public int Priority { get; set; }

        [JsonPropertyName("source")]
        public int Source { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("due_by")]
        public DateTime? DueBy { get; set; }

        [JsonPropertyName("fr_due_by")]
        public DateTime? FrDueBy { get; set; }

        [JsonPropertyName("is_escalated")]
        public bool IsEscalated { get; set; }

        [JsonPropertyName("requester_id")]
        public long RequesterId { get; set; }

        [JsonPropertyName("responder_id")]
        public long? ResponderId { get; set; }

        [JsonPropertyName("company_id")]
        public long? CompanyId { get; set; }

        [JsonPropertyName("stats")]
        public TicketStats Stats { get; set; }

        [JsonPropertyName("custom_fields")]
        public Dictionary<string, object> CustomFields { get; set; }
    }

    /// <summary>
    /// Ticket statistics
    /// </summary>
    public class TicketStats
    {
        [JsonPropertyName("resolution_time_in_secs")]
        public int? ResolutionTimeInSecs { get; set; }

        [JsonPropertyName("first_responded_in_secs")]
        public int? FirstRespondedInSecs { get; set; }

        [JsonPropertyName("agent_responded_in_secs")]
        public int? AgentRespondedInSecs { get; set; }

        [JsonPropertyName("requester_waited_in_secs")]
        public int? RequesterWaitedInSecs { get; set; }
    }

    /// <summary>
    /// Freshdesk Contact entity
    /// </summary>
    public class FreshdeskContact
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("phone")]
        public string Phone { get; set; }

        [JsonPropertyName("mobile")]
        public string Mobile { get; set; }

        [JsonPropertyName("twitter_id")]
        public string TwitterId { get; set; }

        [JsonPropertyName("unique_external_id")]
        public string UniqueExternalId { get; set; }

        [JsonPropertyName("other_emails")]
        public List<string> OtherEmails { get; set; }

        [JsonPropertyName("company_id")]
        public long? CompanyId { get; set; }

        [JsonPropertyName("view_all_tickets")]
        public bool ViewAllTickets { get; set; }

        [JsonPropertyName("avatar")]
        public Avatar Avatar { get; set; }

        [JsonPropertyName("custom_fields")]
        public Dictionary<string, object> CustomFields { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Avatar information
    /// </summary>
    public class Avatar
    {
        [JsonPropertyName("id")]
        public long? Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("content_type")]
        public string ContentType { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Freshdesk Company entity
    /// </summary>
    public class FreshdeskCompany
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("domains")]
        public List<string> Domains { get; set; }

        [JsonPropertyName("note")]
        public string Note { get; set; }

        [JsonPropertyName("custom_fields")]
        public Dictionary<string, object> CustomFields { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Freshdesk Agent entity
    /// </summary>
    public class FreshdeskAgent
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("phone")]
        public string Phone { get; set; }

        [JsonPropertyName("mobile")]
        public string Mobile { get; set; }

        [JsonPropertyName("job_title")]
        public string JobTitle { get; set; }

        [JsonPropertyName("language")]
        public string Language { get; set; }

        [JsonPropertyName("last_login_at")]
        public DateTime? LastLoginAt { get; set; }

        [JsonPropertyName("available")]
        public bool Available { get; set; }

        [JsonPropertyName("available_since")]
        public DateTime? AvailableSince { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("signature")]
        public string Signature { get; set; }

        [JsonPropertyName("ticket_scope")]
        public int TicketScope { get; set; }

        [JsonPropertyName("group_ids")]
        public List<long> GroupIds { get; set; }

        [JsonPropertyName("role_ids")]
        public List<long> RoleIds { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Freshdesk Group entity
    /// </summary>
    public class FreshdeskGroup
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("agent_ids")]
        public List<long> AgentIds { get; set; }

        [JsonPropertyName("auto_ticket_assign")]
        public bool AutoTicketAssign { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Freshdesk Product entity
    /// </summary>
    public class FreshdeskProduct
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Freshdesk Conversation entity
    /// </summary>
    public class FreshdeskConversation
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("body")]
        public string Body { get; set; }

        [JsonPropertyName("body_text")]
        public string BodyText { get; set; }

        [JsonPropertyName("incoming")]
        public bool Incoming { get; set; }

        [JsonPropertyName("private")]
        public bool Private { get; set; }

        [JsonPropertyName("user_id")]
        public long UserId { get; set; }

        [JsonPropertyName("support_email")]
        public string SupportEmail { get; set; }

        [JsonPropertyName("source")]
        public int Source { get; set; }

        [JsonPropertyName("ticket_id")]
        public long TicketId { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("attachments")]
        public List<Attachment> Attachments { get; set; }
    }

    /// <summary>
    /// Attachment entity
    /// </summary>
    public class Attachment
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("content_type")]
        public string ContentType { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Freshdesk Time Entry entity
    /// </summary>
    public class FreshdeskTimeEntry
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("agent_id")]
        public long AgentId { get; set; }

        [JsonPropertyName("ticket_id")]
        public long TicketId { get; set; }

        [JsonPropertyName("time_spent")]
        public string TimeSpent { get; set; }

        [JsonPropertyName("note")]
        public string Note { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("executed_at")]
        public DateTime ExecutedAt { get; set; }
    }

    /// <summary>
    /// Freshdesk Satisfaction Rating entity
    /// </summary>
    public class FreshdeskSatisfactionRating
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("ticket_id")]
        public long TicketId { get; set; }

        [JsonPropertyName("user_id")]
        public long UserId { get; set; }

        [JsonPropertyName("survey_id")]
        public long SurveyId { get; set; }

        [JsonPropertyName("ratings")]
        public Rating Ratings { get; set; }

        [JsonPropertyName("feedback")]
        public string Feedback { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Rating information
    /// </summary>
    public class Rating
    {
        [JsonPropertyName("default_question")]
        public string DefaultQuestion { get; set; }
    }

    /// <summary>
    /// Freshdesk Canned Response entity
    /// </summary>
    public class FreshdeskCannedResponse
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("content_html")]
        public string ContentHtml { get; set; }

        [JsonPropertyName("folder_id")]
        public long FolderId { get; set; }

        [JsonPropertyName("attachments")]
        public List<Attachment> Attachments { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Freshdesk Ticket Field entity
    /// </summary>
    public class FreshdeskTicketField
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("label")]
        public string Label { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("position")]
        public int Position { get; set; }

        [JsonPropertyName("required_for_closure")]
        public bool RequiredForClosure { get; set; }

        [JsonPropertyName("required_for_agents")]
        public bool RequiredForAgents { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("default")]
        public bool Default { get; set; }

        [JsonPropertyName("customers_can_edit")]
        public bool CustomersCanEdit { get; set; }

        [JsonPropertyName("label_for_customers")]
        public string LabelForCustomers { get; set; }

        [JsonPropertyName("displayed_to_customers")]
        public bool DisplayedToCustomers { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Freshdesk Contact Field entity
    /// </summary>
    public class FreshdeskContactField
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("label")]
        public string Label { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("default")]
        public bool Default { get; set; }

        [JsonPropertyName("required_for_agents")]
        public bool RequiredForAgents { get; set; }

        [JsonPropertyName("editable_in_signup")]
        public bool EditableInSignup { get; set; }

        [JsonPropertyName("position")]
        public int Position { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Freshdesk Company Field entity
    /// </summary>
    public class FreshdeskCompanyField
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("label")]
        public string Label { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("default")]
        public bool Default { get; set; }

        [JsonPropertyName("required_for_agents")]
        public bool RequiredForAgents { get; set; }

        [JsonPropertyName("position")]
        public int Position { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}