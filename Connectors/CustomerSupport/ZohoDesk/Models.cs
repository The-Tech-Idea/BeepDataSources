using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TheTechIdea.Beep.DataSources
{
    /// <summary>
    /// Zoho Desk Ticket entity
    /// </summary>
    public class ZohoDeskTicket
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("ticketNumber")]
        public string TicketNumber { get; set; }

        [JsonPropertyName("subject")]
        public string Subject { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("priority")]
        public string Priority { get; set; }

        [JsonPropertyName("channel")]
        public string Channel { get; set; }

        [JsonPropertyName("classification")]
        public string Classification { get; set; }

        [JsonPropertyName("assigneeId")]
        public string AssigneeId { get; set; }

        [JsonPropertyName("contactId")]
        public string ContactId { get; set; }

        [JsonPropertyName("departmentId")]
        public string DepartmentId { get; set; }

        [JsonPropertyName("teamId")]
        public string TeamId { get; set; }

        [JsonPropertyName("productId")]
        public string ProductId { get; set; }

        [JsonPropertyName("accountId")]
        public string AccountId { get; set; }

        [JsonPropertyName("createdTime")]
        public DateTime CreatedTime { get; set; }

        [JsonPropertyName("modifiedTime")]
        public DateTime ModifiedTime { get; set; }

        [JsonPropertyName("dueDate")]
        public DateTime? DueDate { get; set; }

        [JsonPropertyName("closedTime")]
        public DateTime? ClosedTime { get; set; }

        [JsonPropertyName("lastThread")]
        public ZohoDeskThread LastThread { get; set; }

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; }

        [JsonPropertyName("customFields")]
        public Dictionary<string, object> CustomFields { get; set; }

        [JsonPropertyName("sla")]
        public ZohoDeskSla Sla { get; set; }
    }

    /// <summary>
    /// Zoho Desk Thread entity
    /// </summary>
    public class ZohoDeskThread
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("contentType")]
        public string ContentType { get; set; }

        [JsonPropertyName("direction")]
        public string Direction { get; set; }

        [JsonPropertyName("author")]
        public ZohoDeskAuthor Author { get; set; }

        [JsonPropertyName("createdTime")]
        public DateTime CreatedTime { get; set; }

        [JsonPropertyName("modifiedTime")]
        public DateTime ModifiedTime { get; set; }

        [JsonPropertyName("attachments")]
        public List<ZohoDeskAttachment> Attachments { get; set; }
    }

    /// <summary>
    /// Zoho Desk Author entity
    /// </summary>
    public class ZohoDeskAuthor
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }
    }

    /// <summary>
    /// Zoho Desk Contact entity
    /// </summary>
    public class ZohoDeskContact
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("firstName")]
        public string FirstName { get; set; }

        [JsonPropertyName("lastName")]
        public string LastName { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("phone")]
        public string Phone { get; set; }

        [JsonPropertyName("mobile")]
        public string Mobile { get; set; }

        [JsonPropertyName("accountId")]
        public string AccountId { get; set; }

        [JsonPropertyName("ownerId")]
        public string OwnerId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("language")]
        public string Language { get; set; }

        [JsonPropertyName("timeZone")]
        public string TimeZone { get; set; }

        [JsonPropertyName("createdTime")]
        public DateTime CreatedTime { get; set; }

        [JsonPropertyName("modifiedTime")]
        public DateTime ModifiedTime { get; set; }

        [JsonPropertyName("lastLogin")]
        public DateTime? LastLogin { get; set; }

        [JsonPropertyName("customFields")]
        public Dictionary<string, object> CustomFields { get; set; }
    }

    /// <summary>
    /// Zoho Desk Account entity
    /// </summary>
    public class ZohoDeskAccount
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("accountName")]
        public string AccountName { get; set; }

        [JsonPropertyName("website")]
        public string Website { get; set; }

        [JsonPropertyName("accountNumber")]
        public string AccountNumber { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("industry")]
        public string Industry { get; set; }

        [JsonPropertyName("annualRevenue")]
        public decimal? AnnualRevenue { get; set; }

        [JsonPropertyName("phone")]
        public string Phone { get; set; }

        [JsonPropertyName("fax")]
        public string Fax { get; set; }

        [JsonPropertyName("billingStreet")]
        public string BillingStreet { get; set; }

        [JsonPropertyName("billingCity")]
        public string BillingCity { get; set; }

        [JsonPropertyName("billingState")]
        public string BillingState { get; set; }

        [JsonPropertyName("billingCode")]
        public string BillingCode { get; set; }

        [JsonPropertyName("billingCountry")]
        public string BillingCountry { get; set; }

        [JsonPropertyName("ownerId")]
        public string OwnerId { get; set; }

        [JsonPropertyName("createdTime")]
        public DateTime CreatedTime { get; set; }

        [JsonPropertyName("modifiedTime")]
        public DateTime ModifiedTime { get; set; }

        [JsonPropertyName("customFields")]
        public Dictionary<string, object> CustomFields { get; set; }
    }

    /// <summary>
    /// Zoho Desk Agent entity
    /// </summary>
    public class ZohoDeskAgent
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("roleId")]
        public string RoleId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("phone")]
        public string Phone { get; set; }

        [JsonPropertyName("mobile")]
        public string Mobile { get; set; }

        [JsonPropertyName("photoURL")]
        public string PhotoURL { get; set; }

        [JsonPropertyName("language")]
        public string Language { get; set; }

        [JsonPropertyName("timeZone")]
        public string TimeZone { get; set; }

        [JsonPropertyName("lastLogin")]
        public DateTime? LastLogin { get; set; }

        [JsonPropertyName("createdTime")]
        public DateTime CreatedTime { get; set; }

        [JsonPropertyName("modifiedTime")]
        public DateTime ModifiedTime { get; set; }

        [JsonPropertyName("zuid")]
        public string Zuid { get; set; }
    }

    /// <summary>
    /// Zoho Desk Department entity
    /// </summary>
    public class ZohoDeskDepartment
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("isVisible")]
        public bool IsVisible { get; set; }

        [JsonPropertyName("isDefault")]
        public bool IsDefault { get; set; }

        [JsonPropertyName("createdTime")]
        public DateTime CreatedTime { get; set; }

        [JsonPropertyName("modifiedTime")]
        public DateTime ModifiedTime { get; set; }
    }

    /// <summary>
    /// Zoho Desk Comment entity
    /// </summary>
    public class ZohoDeskComment
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("contentType")]
        public string ContentType { get; set; }

        [JsonPropertyName("author")]
        public ZohoDeskAuthor Author { get; set; }

        [JsonPropertyName("createdTime")]
        public DateTime CreatedTime { get; set; }

        [JsonPropertyName("modifiedTime")]
        public DateTime ModifiedTime { get; set; }

        [JsonPropertyName("isPublic")]
        public bool IsPublic { get; set; }

        [JsonPropertyName("attachments")]
        public List<ZohoDeskAttachment> Attachments { get; set; }
    }

    /// <summary>
    /// Zoho Desk Task entity
    /// </summary>
    public class ZohoDeskTask
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("subject")]
        public string Subject { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("priority")]
        public string Priority { get; set; }

        [JsonPropertyName("assigneeId")]
        public string AssigneeId { get; set; }

        [JsonPropertyName("departmentId")]
        public string DepartmentId { get; set; }

        [JsonPropertyName("dueDate")]
        public DateTime? DueDate { get; set; }

        [JsonPropertyName("createdTime")]
        public DateTime CreatedTime { get; set; }

        [JsonPropertyName("modifiedTime")]
        public DateTime ModifiedTime { get; set; }

        [JsonPropertyName("completedTime")]
        public DateTime? CompletedTime { get; set; }

        [JsonPropertyName("percentage")]
        public int Percentage { get; set; }

        [JsonPropertyName("ownerId")]
        public string OwnerId { get; set; }
    }

    /// <summary>
    /// Zoho Desk Organization entity
    /// </summary>
    public class ZohoDeskOrganization
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("companyName")]
        public string CompanyName { get; set; }

        [JsonPropertyName("website")]
        public string Website { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("industry")]
        public string Industry { get; set; }

        [JsonPropertyName("phone")]
        public string Phone { get; set; }

        [JsonPropertyName("fax")]
        public string Fax { get; set; }

        [JsonPropertyName("street")]
        public string Street { get; set; }

        [JsonPropertyName("city")]
        public string City { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }

        [JsonPropertyName("zip")]
        public string Zip { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }

        [JsonPropertyName("createdTime")]
        public DateTime CreatedTime { get; set; }

        [JsonPropertyName("modifiedTime")]
        public DateTime ModifiedTime { get; set; }

        [JsonPropertyName("customFields")]
        public Dictionary<string, object> CustomFields { get; set; }
    }

    /// <summary>
    /// Zoho Desk Attachment entity
    /// </summary>
    public class ZohoDeskAttachment
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }

        [JsonPropertyName("contentType")]
        public string ContentType { get; set; }

        [JsonPropertyName("createdTime")]
        public DateTime CreatedTime { get; set; }

        [JsonPropertyName("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// Zoho Desk SLA entity
    /// </summary>
    public class ZohoDeskSla
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }
    }
}