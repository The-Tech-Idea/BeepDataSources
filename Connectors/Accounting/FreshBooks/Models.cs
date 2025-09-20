using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TheTechIdea.Beep.Connectors.FreshBooks
{
    // Base class for FreshBooks entities
    public class FreshBooksBaseEntity
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("vis_state")]
        public int? VisState { get; set; }
    }

    // Client entity
    public class Client : FreshBooksBaseEntity
    {
        [JsonPropertyName("fname")]
        public string? FirstName { get; set; }

        [JsonPropertyName("lname")]
        public string? LastName { get; set; }

        [JsonPropertyName("organization")]
        public string? Organization { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("home_phone")]
        public string? HomePhone { get; set; }

        [JsonPropertyName("mobile")]
        public string? Mobile { get; set; }

        [JsonPropertyName("business_phone")]
        public string? BusinessPhone { get; set; }

        [JsonPropertyName("fax")]
        public string? Fax { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("company_industry")]
        public string? CompanyIndustry { get; set; }

        [JsonPropertyName("company_size")]
        public string? CompanySize { get; set; }

        [JsonPropertyName("note")]
        public string? Note { get; set; }

        [JsonPropertyName("p_street")]
        public string? PrimaryStreet { get; set; }

        [JsonPropertyName("p_street2")]
        public string? PrimaryStreet2 { get; set; }

        [JsonPropertyName("p_city")]
        public string? PrimaryCity { get; set; }

        [JsonPropertyName("p_state")]
        public string? PrimaryState { get; set; }

        [JsonPropertyName("p_country")]
        public string? PrimaryCountry { get; set; }

        [JsonPropertyName("p_code")]
        public string? PrimaryCode { get; set; }

        [JsonPropertyName("s_street")]
        public string? SecondaryStreet { get; set; }

        [JsonPropertyName("s_street2")]
        public string? SecondaryStreet2 { get; set; }

        [JsonPropertyName("s_city")]
        public string? SecondaryCity { get; set; }

        [JsonPropertyName("s_state")]
        public string? SecondaryState { get; set; }

        [JsonPropertyName("s_country")]
        public string? SecondaryCountry { get; set; }

        [JsonPropertyName("s_code")]
        public string? SecondaryCode { get; set; }

        [JsonPropertyName("vat_name")]
        public string? VatName { get; set; }

        [JsonPropertyName("vat_number")]
        public string? VatNumber { get; set; }

        [JsonPropertyName("currency_code")]
        public string? CurrencyCode { get; set; }

        [JsonPropertyName("language")]
        public string? Language { get; set; }

        [JsonPropertyName("pref_email")]
        public bool? PrefEmail { get; set; }

        [JsonPropertyName("pref_gmail")]
        public bool? PrefGmail { get; set; }

        [JsonPropertyName("last_activity")]
        public DateTime? LastActivity { get; set; }

        [JsonPropertyName("signup_date")]
        public DateTime? SignupDate { get; set; }

        [JsonPropertyName("client_since")]
        public DateTime? ClientSince { get; set; }
    }

    // Invoice entity
    public class Invoice : FreshBooksBaseEntity
    {
        [JsonPropertyName("invoice_number")]
        public string? InvoiceNumber { get; set; }

        [JsonPropertyName("customerid")]
        public string? CustomerId { get; set; }

        [JsonPropertyName("create_date")]
        public DateTime? CreateDate { get; set; }

        [JsonPropertyName("due_offset_days")]
        public int? DueOffsetDays { get; set; }

        [JsonPropertyName("estimateid")]
        public string? EstimateId { get; set; }

        [JsonPropertyName("first_sent_at")]
        public DateTime? FirstSentAt { get; set; }

        [JsonPropertyName("last_sent_at")]
        public DateTime? LastSentAt { get; set; }

        [JsonPropertyName("status")]
        public int? Status { get; set; }

        [JsonPropertyName("discount_description")]
        public string? DiscountDescription { get; set; }

        [JsonPropertyName("discount_total")]
        public decimal? DiscountTotal { get; set; }

        [JsonPropertyName("discount_value")]
        public decimal? DiscountValue { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("terms")]
        public string? Terms { get; set; }

        [JsonPropertyName("currency_code")]
        public string? CurrencyCode { get; set; }

        [JsonPropertyName("language")]
        public string? Language { get; set; }

        [JsonPropertyName("po_number")]
        public string? PoNumber { get; set; }

        [JsonPropertyName("template")]
        public string? Template { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("return_uri")]
        public string? ReturnUri { get; set; }

        [JsonPropertyName("vat_name")]
        public string? VatName { get; set; }

        [JsonPropertyName("vat_number")]
        public string? VatNumber { get; set; }

        [JsonPropertyName("show_attachments")]
        public bool? ShowAttachments { get; set; }

        [JsonPropertyName("send_email")]
        public bool? SendEmail { get; set; }

        [JsonPropertyName("auto_bill")]
        public bool? AutoBill { get; set; }

        [JsonPropertyName("amount")]
        public Amount? Amount { get; set; }

        [JsonPropertyName("outstanding")]
        public Amount? Outstanding { get; set; }

        [JsonPropertyName("paid")]
        public Amount? Paid { get; set; }

        [JsonPropertyName("lines")]
        public List<InvoiceLine>? Lines { get; set; }

        [JsonPropertyName("presentation")]
        public Presentation? Presentation { get; set; }
    }

    // Estimate entity
    public class Estimate : FreshBooksBaseEntity
    {
        [JsonPropertyName("estimate_number")]
        public string? EstimateNumber { get; set; }

        [JsonPropertyName("customerid")]
        public string? CustomerId { get; set; }

        [JsonPropertyName("create_date")]
        public DateTime? CreateDate { get; set; }

        [JsonPropertyName("status")]
        public int? Status { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("terms")]
        public string? Terms { get; set; }

        [JsonPropertyName("currency_code")]
        public string? CurrencyCode { get; set; }

        [JsonPropertyName("language")]
        public string? Language { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("discount_description")]
        public string? DiscountDescription { get; set; }

        [JsonPropertyName("discount_total")]
        public decimal? DiscountTotal { get; set; }

        [JsonPropertyName("discount_value")]
        public decimal? DiscountValue { get; set; }

        [JsonPropertyName("amount")]
        public Amount? Amount { get; set; }

        [JsonPropertyName("lines")]
        public List<EstimateLine>? Lines { get; set; }
    }

    // Expense entity
    public class Expense : FreshBooksBaseEntity
    {
        [JsonPropertyName("amount")]
        public Amount? Amount { get; set; }

        [JsonPropertyName("categoryid")]
        public string? CategoryId { get; set; }

        [JsonPropertyName("clientid")]
        public string? ClientId { get; set; }

        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("markup_percent")]
        public decimal? MarkupPercent { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("projectid")]
        public string? ProjectId { get; set; }

        [JsonPropertyName("staffid")]
        public string? StaffId { get; set; }

        [JsonPropertyName("status")]
        public int? Status { get; set; }

        [JsonPropertyName("taxAmount1")]
        public decimal? TaxAmount1 { get; set; }

        [JsonPropertyName("taxAmount2")]
        public decimal? TaxAmount2 { get; set; }

        [JsonPropertyName("taxName1")]
        public string? TaxName1 { get; set; }

        [JsonPropertyName("taxName2")]
        public string? TaxName2 { get; set; }

        [JsonPropertyName("taxPercent1")]
        public decimal? TaxPercent1 { get; set; }

        [JsonPropertyName("taxPercent2")]
        public decimal? TaxPercent2 { get; set; }

        [JsonPropertyName("vendor")]
        public string? Vendor { get; set; }

        [JsonPropertyName("has_receipt")]
        public bool? HasReceipt { get; set; }

        [JsonPropertyName("receipts")]
        public List<Receipt>? Receipts { get; set; }
    }

    // Item entity
    public class Item : FreshBooksBaseEntity
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("qty")]
        public decimal? Qty { get; set; }

        [JsonPropertyName("unit_cost")]
        public Amount? UnitCost { get; set; }

        [JsonPropertyName("inventory")]
        public decimal? Inventory { get; set; }

        [JsonPropertyName("tax1")]
        public decimal? Tax1 { get; set; }

        [JsonPropertyName("tax2")]
        public decimal? Tax2 { get; set; }

        [JsonPropertyName("type")]
        public int? Type { get; set; }

        [JsonPropertyName("active")]
        public bool? Active { get; set; }

        [JsonPropertyName("sku")]
        public string? Sku { get; set; }

        [JsonPropertyName("accounting_systemid")]
        public string? AccountingSystemId { get; set; }
    }

    // Payment entity
    public class Payment : FreshBooksBaseEntity
    {
        [JsonPropertyName("amount")]
        public Amount? Amount { get; set; }

        [JsonPropertyName("clientid")]
        public string? ClientId { get; set; }

        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("from_credit")]
        public string? FromCredit { get; set; }

        [JsonPropertyName("overpayment_id")]
        public string? OverpaymentId { get; set; }

        [JsonPropertyName("invoiceid")]
        public string? InvoiceId { get; set; }

        [JsonPropertyName("depositid")]
        public string? DepositId { get; set; }

        [JsonPropertyName("send_receipt")]
        public bool? SendReceipt { get; set; }

        [JsonPropertyName("send_snail_mail")]
        public bool? SendSnailMail { get; set; }
    }

    // Project entity
    public class Project : FreshBooksBaseEntity
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("client_id")]
        public string? ClientId { get; set; }

        [JsonPropertyName("project_managerid")]
        public string? ProjectManagerId { get; set; }

        [JsonPropertyName("rate")]
        public decimal? Rate { get; set; }

        [JsonPropertyName("bill_method")]
        public string? BillMethod { get; set; }

        [JsonPropertyName("project_type")]
        public string? ProjectType { get; set; }

        [JsonPropertyName("budget")]
        public decimal? Budget { get; set; }

        [JsonPropertyName("fixed_price")]
        public decimal? FixedPrice { get; set; }

        [JsonPropertyName("hour_budget")]
        public decimal? HourBudget { get; set; }

        [JsonPropertyName("start_date")]
        public DateTime? StartDate { get; set; }

        [JsonPropertyName("end_date")]
        public DateTime? EndDate { get; set; }

        [JsonPropertyName("completed")]
        public bool? Completed { get; set; }

        [JsonPropertyName("billed_amount")]
        public decimal? BilledAmount { get; set; }

        [JsonPropertyName("billed_status")]
        public string? BilledStatus { get; set; }

        [JsonPropertyName("due_date")]
        public DateTime? DueDate { get; set; }

        [JsonPropertyName("internal")]
        public bool? Internal { get; set; }

        [JsonPropertyName("group")]
        public ProjectGroup? Group { get; set; }

        [JsonPropertyName("services")]
        public List<ProjectService>? Services { get; set; }

        [JsonPropertyName("tasks")]
        public List<ProjectTask>? Tasks { get; set; }
    }

    // Time Entry entity
    public class TimeEntry : FreshBooksBaseEntity
    {
        [JsonPropertyName("staff_id")]
        public string? StaffId { get; set; }

        [JsonPropertyName("project_id")]
        public string? ProjectId { get; set; }

        [JsonPropertyName("task_id")]
        public string? TaskId { get; set; }

        [JsonPropertyName("service_id")]
        public string? ServiceId { get; set; }

        [JsonPropertyName("client_id")]
        public string? ClientId { get; set; }

        [JsonPropertyName("billed")]
        public bool? Billed { get; set; }

        [JsonPropertyName("billable")]
        public bool? Billable { get; set; }

        [JsonPropertyName("hours")]
        public decimal? Hours { get; set; }

        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("started_at")]
        public DateTime? StartedAt { get; set; }

        [JsonPropertyName("ended_at")]
        public DateTime? EndedAt { get; set; }

        [JsonPropertyName("timer_started_at")]
        public DateTime? TimerStartedAt { get; set; }

        [JsonPropertyName("retainer_id")]
        public string? RetainerId { get; set; }

        [JsonPropertyName("cost")]
        public Amount? Cost { get; set; }
    }

    // Task entity
    public class Task : FreshBooksBaseEntity
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("rate")]
        public decimal? Rate { get; set; }

        [JsonPropertyName("billable")]
        public bool? Billable { get; set; }

        [JsonPropertyName("tax1")]
        public decimal? Tax1 { get; set; }

        [JsonPropertyName("tax2")]
        public decimal? Tax2 { get; set; }

        [JsonPropertyName("active")]
        public bool? Active { get; set; }
    }

    // Staff entity
    public class Staff : FreshBooksBaseEntity
    {
        [JsonPropertyName("business_id")]
        public string? BusinessId { get; set; }

        [JsonPropertyName("fname")]
        public string? FirstName { get; set; }

        [JsonPropertyName("lname")]
        public string? LastName { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("rate")]
        public decimal? Rate { get; set; }

        [JsonPropertyName("business_phone")]
        public string? BusinessPhone { get; set; }

        [JsonPropertyName("mobile_phone")]
        public string? MobilePhone { get; set; }

        [JsonPropertyName("home_phone")]
        public string? HomePhone { get; set; }

        [JsonPropertyName("language")]
        public string? Language { get; set; }

        [JsonPropertyName("level")]
        public string? Level { get; set; }

        [JsonPropertyName("active")]
        public bool? Active { get; set; }

        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("signup_date")]
        public DateTime? SignupDate { get; set; }

        [JsonPropertyName("last_login")]
        public DateTime? LastLogin { get; set; }

        [JsonPropertyName("number_of_logins")]
        public int? NumberOfLogins { get; set; }

        [JsonPropertyName("roadmap_responded")]
        public bool? RoadmapResponded { get; set; }

        [JsonPropertyName("roadmap_response")]
        public string? RoadmapResponse { get; set; }
    }

    // Service entity
    public class Service : FreshBooksBaseEntity
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("rate")]
        public decimal? Rate { get; set; }

        [JsonPropertyName("billable")]
        public bool? Billable { get; set; }

        [JsonPropertyName("tax1")]
        public decimal? Tax1 { get; set; }

        [JsonPropertyName("tax2")]
        public decimal? Tax2 { get; set; }

        [JsonPropertyName("active")]
        public bool? Active { get; set; }
    }

    // Tax entity
    public class Tax : FreshBooksBaseEntity
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("rate")]
        public decimal? Rate { get; set; }

        [JsonPropertyName("number")]
        public string? Number { get; set; }

        [JsonPropertyName("compound")]
        public bool? Compound { get; set; }

        [JsonPropertyName("taxable")]
        public bool? Taxable { get; set; }

        [JsonPropertyName("system_tax")]
        public bool? SystemTax { get; set; }
    }

    // Category entity
    public class Category : FreshBooksBaseEntity
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("category")]
        public string? CategoryType { get; set; }

        [JsonPropertyName("parentid")]
        public string? ParentId { get; set; }

        [JsonPropertyName("lft")]
        public int? Left { get; set; }

        [JsonPropertyName("rgt")]
        public int? Right { get; set; }

        [JsonPropertyName("normalized_name")]
        public string? NormalizedName { get; set; }

        [JsonPropertyName("billable")]
        public bool? Billable { get; set; }

        [JsonPropertyName("taxable")]
        public bool? Taxable { get; set; }
    }

    // Supporting classes
    public class Amount
    {
        [JsonPropertyName("amount")]
        public decimal? Value { get; set; }

        [JsonPropertyName("code")]
        public string? Code { get; set; }
    }

    public class InvoiceLine
    {
        [JsonPropertyName("lineid")]
        public string? LineId { get; set; }

        [JsonPropertyName("amount")]
        public Amount? Amount { get; set; }

        [JsonPropertyName("base_amount")]
        public Amount? BaseAmount { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("qty")]
        public decimal? Qty { get; set; }

        [JsonPropertyName("rate")]
        public decimal? Rate { get; set; }

        [JsonPropertyName("taxAmount1")]
        public decimal? TaxAmount1 { get; set; }

        [JsonPropertyName("taxAmount2")]
        public decimal? TaxAmount2 { get; set; }

        [JsonPropertyName("taxName1")]
        public string? TaxName1 { get; set; }

        [JsonPropertyName("taxName2")]
        public string? TaxName2 { get; set; }

        [JsonPropertyName("taxPercent1")]
        public decimal? TaxPercent1 { get; set; }

        [JsonPropertyName("taxPercent2")]
        public decimal? TaxPercent2 { get; set; }

        [JsonPropertyName("type")]
        public int? Type { get; set; }

        [JsonPropertyName("unit_cost")]
        public Amount? UnitCost { get; set; }

        [JsonPropertyName("updated")]
        public DateTime? Updated { get; set; }
    }

    public class EstimateLine
    {
        [JsonPropertyName("lineid")]
        public string? LineId { get; set; }

        [JsonPropertyName("amount")]
        public Amount? Amount { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("qty")]
        public decimal? Qty { get; set; }

        [JsonPropertyName("rate")]
        public decimal? Rate { get; set; }

        [JsonPropertyName("taxAmount1")]
        public decimal? TaxAmount1 { get; set; }

        [JsonPropertyName("taxAmount2")]
        public decimal? TaxAmount2 { get; set; }

        [JsonPropertyName("taxName1")]
        public string? TaxName1 { get; set; }

        [JsonPropertyName("taxName2")]
        public string? TaxName2 { get; set; }

        [JsonPropertyName("taxPercent1")]
        public decimal? TaxPercent1 { get; set; }

        [JsonPropertyName("taxPercent2")]
        public decimal? TaxPercent2 { get; set; }

        [JsonPropertyName("type")]
        public int? Type { get; set; }

        [JsonPropertyName("unit_cost")]
        public Amount? UnitCost { get; set; }

        [JsonPropertyName("updated")]
        public DateTime? Updated { get; set; }
    }

    public class Presentation
    {
        [JsonPropertyName("theme_font")]
        public string? ThemeFont { get; set; }

        [JsonPropertyName("theme_color")]
        public string? ThemeColor { get; set; }

        [JsonPropertyName("theme_layout")]
        public string? ThemeLayout { get; set; }

        [JsonPropertyName("image_banner_position")]
        public string? ImageBannerPosition { get; set; }

        [JsonPropertyName("image_banner_src")]
        public string? ImageBannerSrc { get; set; }
    }

    public class Receipt
    {
        [JsonPropertyName("receiptid")]
        public string? ReceiptId { get; set; }

        [JsonPropertyName("filename")]
        public string? FileName { get; set; }

        [JsonPropertyName("size")]
        public long? Size { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }

    public class ProjectGroup
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    public class ProjectService
    {
        [JsonPropertyName("serviceid")]
        public string? ServiceId { get; set; }

        [JsonPropertyName("rate")]
        public decimal? Rate { get; set; }
    }

    public class ProjectTask
    {
        [JsonPropertyName("taskid")]
        public string? TaskId { get; set; }

        [JsonPropertyName("rate")]
        public decimal? Rate { get; set; }

        [JsonPropertyName("billable")]
        public bool? Billable { get; set; }
    }
}