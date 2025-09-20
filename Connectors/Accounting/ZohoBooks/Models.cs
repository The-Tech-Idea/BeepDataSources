using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.ZohoBooks
{
    // Base class for ZohoBooks entities
    public class ZohoBooksBaseEntity
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("created_time")]
        public DateTime? CreatedTime { get; set; }

        [JsonPropertyName("last_modified_time")]
        public DateTime? LastModifiedTime { get; set; }

        [JsonPropertyName("source")]
        public string? Source { get; set; }
    }

    // Organization entity
    public class Organization : ZohoBooksBaseEntity
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("org_id")]
        public string? OrgId { get; set; }

        [JsonPropertyName("is_default_org")]
        public bool? IsDefaultOrg { get; set; }

        [JsonPropertyName("account_created_date")]
        public DateTime? AccountCreatedDate { get; set; }

        [JsonPropertyName("time_zone")]
        public string? TimeZone { get; set; }

        [JsonPropertyName("language_code")]
        public string? LanguageCode { get; set; }

        [JsonPropertyName("date_format")]
        public string? DateFormat { get; set; }

        [JsonPropertyName("currency_id")]
        public string? CurrencyId { get; set; }

        [JsonPropertyName("currency_code")]
        public string? CurrencyCode { get; set; }

        [JsonPropertyName("currency_symbol")]
        public string? CurrencySymbol { get; set; }

        [JsonPropertyName("contact_name")]
        public string? ContactName { get; set; }

        [JsonPropertyName("company_name")]
        public string? CompanyName { get; set; }

        [JsonPropertyName("company_address")]
        public string? CompanyAddress { get; set; }

        [JsonPropertyName("website")]
        public string? Website { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("mobile")]
        public string? Mobile { get; set; }

        [JsonPropertyName("fax")]
        public string? Fax { get; set; }

        [JsonPropertyName("tax_id")]
        public string? TaxId { get; set; }

        [JsonPropertyName("fiscal_year_start_month")]
        public int? FiscalYearStartMonth { get; set; }

        [JsonPropertyName("fiscal_year_start_day")]
        public int? FiscalYearStartDay { get; set; }

        [JsonPropertyName("is_org_active")]
        public bool? IsOrgActive { get; set; }
    }

    // Contact entity (base for Customer and Vendor)
    public class Contact : ZohoBooksBaseEntity
    {
        [JsonPropertyName("contact_name")]
        public string? ContactName { get; set; }

        [JsonPropertyName("company_name")]
        public string? CompanyName { get; set; }

        [JsonPropertyName("contact_type")]
        public string? ContactType { get; set; } // customer or vendor

        [JsonPropertyName("customer_sub_type")]
        public string? CustomerSubType { get; set; }

        [JsonPropertyName("vendor_sub_type")]
        public string? VendorSubType { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("payment_terms")]
        public int? PaymentTerms { get; set; }

        [JsonPropertyName("payment_terms_label")]
        public string? PaymentTermsLabel { get; set; }

        [JsonPropertyName("currency_id")]
        public string? CurrencyId { get; set; }

        [JsonPropertyName("currency_code")]
        public string? CurrencyCode { get; set; }

        [JsonPropertyName("outstanding_receivable_amount")]
        public decimal? OutstandingReceivableAmount { get; set; }

        [JsonPropertyName("outstanding_payable_amount")]
        public decimal? OutstandingPayableAmount { get; set; }

        [JsonPropertyName("unused_credits_receivable_amount")]
        public decimal? UnusedCreditsReceivableAmount { get; set; }

        [JsonPropertyName("unused_credits_payable_amount")]
        public decimal? UnusedCreditsPayableAmount { get; set; }

        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string? LastName { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("mobile")]
        public string? Mobile { get; set; }

        [JsonPropertyName("website")]
        public string? Website { get; set; }

        [JsonPropertyName("billing_address")]
        public Address? BillingAddress { get; set; }

        [JsonPropertyName("shipping_address")]
        public Address? ShippingAddress { get; set; }

        [JsonPropertyName("contact_persons")]
        public List<ContactPerson>? ContactPersons { get; set; }

        [JsonPropertyName("default_templates")]
        public DefaultTemplates? DefaultTemplates { get; set; }

        [JsonPropertyName("custom_fields")]
        public List<CustomField>? CustomFields { get; set; }

        [JsonPropertyName("is_taxable")]
        public bool? IsTaxable { get; set; }

        [JsonPropertyName("tax_id")]
        public string? TaxId { get; set; }

        [JsonPropertyName("tax_name")]
        public string? TaxName { get; set; }

        [JsonPropertyName("tax_percentage")]
        public decimal? TaxPercentage { get; set; }

        [JsonPropertyName("facebook")]
        public string? Facebook { get; set; }

        [JsonPropertyName("twitter")]
        public string? Twitter { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("price_precision")]
        public int? PricePrecision { get; set; }
    }

    // Customer entity (inherits from Contact)
    public class Customer : Contact
    {
        // Inherits all properties from Contact
        // Additional customer-specific properties can be added here if needed
    }

    // Vendor entity (inherits from Contact)
    public class Vendor : Contact
    {
        // Inherits all properties from Contact
        // Additional vendor-specific properties can be added here if needed
    }

    // Item entity
    public class Item : ZohoBooksBaseEntity
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("rate")]
        public decimal? Rate { get; set; }

        [JsonPropertyName("tax_id")]
        public string? TaxId { get; set; }

        [JsonPropertyName("tax_name")]
        public string? TaxName { get; set; }

        [JsonPropertyName("tax_percentage")]
        public decimal? TaxPercentage { get; set; }

        [JsonPropertyName("tax_type")]
        public string? TaxType { get; set; }

        [JsonPropertyName("sku")]
        public string? Sku { get; set; }

        [JsonPropertyName("product_type")]
        public string? ProductType { get; set; }

        [JsonPropertyName("item_type")]
        public string? ItemType { get; set; }

        [JsonPropertyName("is_taxable")]
        public bool? IsTaxable { get; set; }

        [JsonPropertyName("purchase_rate")]
        public decimal? PurchaseRate { get; set; }

        [JsonPropertyName("purchase_description")]
        public string? PurchaseDescription { get; set; }

        [JsonPropertyName("purchase_tax_id")]
        public string? PurchaseTaxId { get; set; }

        [JsonPropertyName("purchase_tax_name")]
        public string? PurchaseTaxName { get; set; }

        [JsonPropertyName("purchase_tax_percentage")]
        public decimal? PurchaseTaxPercentage { get; set; }

        [JsonPropertyName("purchase_tax_type")]
        public string? PurchaseTaxType { get; set; }

        [JsonPropertyName("sales_channels")]
        public List<string>? SalesChannels { get; set; }

        [JsonPropertyName("purchase_channels")]
        public List<string>? PurchaseChannels { get; set; }

        [JsonPropertyName("source")]
        public string? Source { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("account_id")]
        public string? AccountId { get; set; }

        [JsonPropertyName("account_name")]
        public string? AccountName { get; set; }

        [JsonPropertyName("inventory_account_id")]
        public string? InventoryAccountId { get; set; }

        [JsonPropertyName("inventory_account_name")]
        public string? InventoryAccountName { get; set; }

        [JsonPropertyName("unit")]
        public string? Unit { get; set; }

        [JsonPropertyName("reorder_level")]
        public decimal? ReorderLevel { get; set; }

        [JsonPropertyName("initial_stock")]
        public decimal? InitialStock { get; set; }

        [JsonPropertyName("initial_stock_rate")]
        public decimal? InitialStockRate { get; set; }

        [JsonPropertyName("vendor_id")]
        public string? VendorId { get; set; }

        [JsonPropertyName("vendor_name")]
        public string? VendorName { get; set; }

        [JsonPropertyName("stock_on_hand")]
        public decimal? StockOnHand { get; set; }

        [JsonPropertyName("available_stock")]
        public decimal? AvailableStock { get; set; }

        [JsonPropertyName("actual_available_stock")]
        public decimal? ActualAvailableStock { get; set; }

        [JsonPropertyName("committed_stock")]
        public decimal? CommittedStock { get; set; }

        [JsonPropertyName("custom_fields")]
        public List<CustomField>? CustomFields { get; set; }
    }

    // Invoice entity
    public class Invoice : ZohoBooksBaseEntity
    {
        [JsonPropertyName("invoice_number")]
        public string? InvoiceNumber { get; set; }

        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("due_date")]
        public DateTime? DueDate { get; set; }

        [JsonPropertyName("reference_number")]
        public string? ReferenceNumber { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("customer_id")]
        public string? CustomerId { get; set; }

        [JsonPropertyName("customer_name")]
        public string? CustomerName { get; set; }

        [JsonPropertyName("contact_persons")]
        public List<ContactPerson>? ContactPersons { get; set; }

        [JsonPropertyName("currency_id")]
        public string? CurrencyId { get; set; }

        [JsonPropertyName("currency_code")]
        public string? CurrencyCode { get; set; }

        [JsonPropertyName("exchange_rate")]
        public decimal? ExchangeRate { get; set; }

        [JsonPropertyName("discount")]
        public decimal? Discount { get; set; }

        [JsonPropertyName("discount_type")]
        public string? DiscountType { get; set; }

        [JsonPropertyName("is_discount_before_tax")]
        public bool? IsDiscountBeforeTax { get; set; }

        [JsonPropertyName("discount_applied_on_amount")]
        public decimal? DiscountAppliedOnAmount { get; set; }

        [JsonPropertyName("is_inclusive_tax")]
        public bool? IsInclusiveTax { get; set; }

        [JsonPropertyName("line_items")]
        public List<InvoiceLineItem>? LineItems { get; set; }

        [JsonPropertyName("sub_total")]
        public decimal? SubTotal { get; set; }

        [JsonPropertyName("tax_total")]
        public decimal? TaxTotal { get; set; }

        [JsonPropertyName("total")]
        public decimal? Total { get; set; }

        [JsonPropertyName("balance")]
        public decimal? Balance { get; set; }

        [JsonPropertyName("price_precision")]
        public int? PricePrecision { get; set; }

        [JsonPropertyName("payment_terms")]
        public int? PaymentTerms { get; set; }

        [JsonPropertyName("payment_terms_label")]
        public string? PaymentTermsLabel { get; set; }

        [JsonPropertyName("custom_fields")]
        public List<CustomField>? CustomFields { get; set; }

        [JsonPropertyName("documents")]
        public List<Document>? Documents { get; set; }

        [JsonPropertyName("billing_address")]
        public Address? BillingAddress { get; set; }

        [JsonPropertyName("shipping_address")]
        public Address? ShippingAddress { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("terms")]
        public string? Terms { get; set; }

        [JsonPropertyName("template_id")]
        public string? TemplateId { get; set; }

        [JsonPropertyName("template_name")]
        public string? TemplateName { get; set; }

        [JsonPropertyName("template_type")]
        public string? TemplateType { get; set; }

        [JsonPropertyName("attachment_name")]
        public string? AttachmentName { get; set; }

        [JsonPropertyName("can_send_in_mail")]
        public bool? CanSendInMail { get; set; }

        [JsonPropertyName("salesperson_id")]
        public string? SalespersonId { get; set; }

        [JsonPropertyName("salesperson_name")]
        public string? SalespersonName { get; set; }
    }

    // Invoice Line Item entity
    public class InvoiceLineItem : ZohoBooksBaseEntity
    {
        [JsonPropertyName("item_id")]
        public string? ItemId { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("item_order")]
        public int? ItemOrder { get; set; }

        [JsonPropertyName("rate")]
        public decimal? Rate { get; set; }

        [JsonPropertyName("quantity")]
        public decimal? Quantity { get; set; }

        [JsonPropertyName("unit")]
        public string? Unit { get; set; }

        [JsonPropertyName("discount")]
        public decimal? Discount { get; set; }

        [JsonPropertyName("discount_amount")]
        public decimal? DiscountAmount { get; set; }

        [JsonPropertyName("item_total")]
        public decimal? ItemTotal { get; set; }

        [JsonPropertyName("tax_id")]
        public string? TaxId { get; set; }

        [JsonPropertyName("tax_name")]
        public string? TaxName { get; set; }

        [JsonPropertyName("tax_type")]
        public string? TaxType { get; set; }

        [JsonPropertyName("tax_percentage")]
        public decimal? TaxPercentage { get; set; }

        [JsonPropertyName("tax_amount")]
        public decimal? TaxAmount { get; set; }

        [JsonPropertyName("account_id")]
        public string? AccountId { get; set; }

        [JsonPropertyName("account_name")]
        public string? AccountName { get; set; }

        [JsonPropertyName("project_id")]
        public string? ProjectId { get; set; }

        [JsonPropertyName("project_name")]
        public string? ProjectName { get; set; }

        [JsonPropertyName("time_entry_ids")]
        public List<string>? TimeEntryIds { get; set; }

        [JsonPropertyName("expense_id")]
        public string? ExpenseId { get; set; }

        [JsonPropertyName("expense_receipt_name")]
        public string? ExpenseReceiptName { get; set; }
    }

    // Bill entity
    public class Bill : ZohoBooksBaseEntity
    {
        [JsonPropertyName("bill_number")]
        public string? BillNumber { get; set; }

        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("due_date")]
        public DateTime? DueDate { get; set; }

        [JsonPropertyName("reference_number")]
        public string? ReferenceNumber { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("vendor_id")]
        public string? VendorId { get; set; }

        [JsonPropertyName("vendor_name")]
        public string? VendorName { get; set; }

        [JsonPropertyName("currency_id")]
        public string? CurrencyId { get; set; }

        [JsonPropertyName("currency_code")]
        public string? CurrencyCode { get; set; }

        [JsonPropertyName("exchange_rate")]
        public decimal? ExchangeRate { get; set; }

        [JsonPropertyName("line_items")]
        public List<BillLineItem>? LineItems { get; set; }

        [JsonPropertyName("sub_total")]
        public decimal? SubTotal { get; set; }

        [JsonPropertyName("tax_total")]
        public decimal? TaxTotal { get; set; }

        [JsonPropertyName("total")]
        public decimal? Total { get; set; }

        [JsonPropertyName("balance")]
        public decimal? Balance { get; set; }

        [JsonPropertyName("price_precision")]
        public int? PricePrecision { get; set; }

        [JsonPropertyName("payment_terms")]
        public int? PaymentTerms { get; set; }

        [JsonPropertyName("payment_terms_label")]
        public string? PaymentTermsLabel { get; set; }

        [JsonPropertyName("custom_fields")]
        public List<CustomField>? CustomFields { get; set; }

        [JsonPropertyName("documents")]
        public List<Document>? Documents { get; set; }

        [JsonPropertyName("billing_address")]
        public Address? BillingAddress { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("terms")]
        public string? Terms { get; set; }

        [JsonPropertyName("attachment_name")]
        public string? AttachmentName { get; set; }
    }

    // Bill Line Item entity
    public class BillLineItem : ZohoBooksBaseEntity
    {
        [JsonPropertyName("item_id")]
        public string? ItemId { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("item_order")]
        public int? ItemOrder { get; set; }

        [JsonPropertyName("rate")]
        public decimal? Rate { get; set; }

        [JsonPropertyName("quantity")]
        public decimal? Quantity { get; set; }

        [JsonPropertyName("unit")]
        public string? Unit { get; set; }

        [JsonPropertyName("discount")]
        public decimal? Discount { get; set; }

        [JsonPropertyName("discount_amount")]
        public decimal? DiscountAmount { get; set; }

        [JsonPropertyName("item_total")]
        public decimal? ItemTotal { get; set; }

        [JsonPropertyName("tax_id")]
        public string? TaxId { get; set; }

        [JsonPropertyName("tax_name")]
        public string? TaxName { get; set; }

        [JsonPropertyName("tax_type")]
        public string? TaxType { get; set; }

        [JsonPropertyName("tax_percentage")]
        public decimal? TaxPercentage { get; set; }

        [JsonPropertyName("tax_amount")]
        public decimal? TaxAmount { get; set; }

        [JsonPropertyName("account_id")]
        public string? AccountId { get; set; }

        [JsonPropertyName("account_name")]
        public string? AccountName { get; set; }

        [JsonPropertyName("project_id")]
        public string? ProjectId { get; set; }

        [JsonPropertyName("project_name")]
        public string? ProjectName { get; set; }
    }

    // Payment entity
    public class Payment : ZohoBooksBaseEntity
    {
        [JsonPropertyName("payment_number")]
        public string? PaymentNumber { get; set; }

        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("amount")]
        public decimal? Amount { get; set; }

        [JsonPropertyName("payment_mode")]
        public string? PaymentMode { get; set; }

        [JsonPropertyName("reference_number")]
        public string? ReferenceNumber { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("customer_id")]
        public string? CustomerId { get; set; }

        [JsonPropertyName("customer_name")]
        public string? CustomerName { get; set; }

        [JsonPropertyName("vendor_id")]
        public string? VendorId { get; set; }

        [JsonPropertyName("vendor_name")]
        public string? VendorName { get; set; }

        [JsonPropertyName("currency_id")]
        public string? CurrencyId { get; set; }

        [JsonPropertyName("currency_code")]
        public string? CurrencyCode { get; set; }

        [JsonPropertyName("exchange_rate")]
        public decimal? ExchangeRate { get; set; }

        [JsonPropertyName("bank_charges")]
        public decimal? BankCharges { get; set; }

        [JsonPropertyName("tax_amount_withheld")]
        public decimal? TaxAmountWithheld { get; set; }

        [JsonPropertyName("account_id")]
        public string? AccountId { get; set; }

        [JsonPropertyName("account_name")]
        public string? AccountName { get; set; }

        [JsonPropertyName("invoices")]
        public List<PaymentInvoice>? Invoices { get; set; }

        [JsonPropertyName("bills")]
        public List<PaymentBill>? Bills { get; set; }

        [JsonPropertyName("custom_fields")]
        public List<CustomField>? CustomFields { get; set; }
    }

    // Credit Note entity
    public class CreditNote : ZohoBooksBaseEntity
    {
        [JsonPropertyName("creditnote_number")]
        public string? CreditnoteNumber { get; set; }

        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("reference_number")]
        public string? ReferenceNumber { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("customer_id")]
        public string? CustomerId { get; set; }

        [JsonPropertyName("customer_name")]
        public string? CustomerName { get; set; }

        [JsonPropertyName("currency_id")]
        public string? CurrencyId { get; set; }

        [JsonPropertyName("currency_code")]
        public string? CurrencyCode { get; set; }

        [JsonPropertyName("exchange_rate")]
        public decimal? ExchangeRate { get; set; }

        [JsonPropertyName("line_items")]
        public List<CreditNoteLineItem>? LineItems { get; set; }

        [JsonPropertyName("sub_total")]
        public decimal? SubTotal { get; set; }

        [JsonPropertyName("tax_total")]
        public decimal? TaxTotal { get; set; }

        [JsonPropertyName("total")]
        public decimal? Total { get; set; }

        [JsonPropertyName("balance")]
        public decimal? Balance { get; set; }

        [JsonPropertyName("price_precision")]
        public int? PricePrecision { get; set; }

        [JsonPropertyName("custom_fields")]
        public List<CustomField>? CustomFields { get; set; }

        [JsonPropertyName("documents")]
        public List<Document>? Documents { get; set; }

        [JsonPropertyName("billing_address")]
        public Address? BillingAddress { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("terms")]
        public string? Terms { get; set; }
    }

    // Estimate entity
    public class Estimate : ZohoBooksBaseEntity
    {
        [JsonPropertyName("estimate_number")]
        public string? EstimateNumber { get; set; }

        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("expiry_date")]
        public DateTime? ExpiryDate { get; set; }

        [JsonPropertyName("reference_number")]
        public string? ReferenceNumber { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("customer_id")]
        public string? CustomerId { get; set; }

        [JsonPropertyName("customer_name")]
        public string? CustomerName { get; set; }

        [JsonPropertyName("currency_id")]
        public string? CurrencyId { get; set; }

        [JsonPropertyName("currency_code")]
        public string? CurrencyCode { get; set; }

        [JsonPropertyName("exchange_rate")]
        public decimal? ExchangeRate { get; set; }

        [JsonPropertyName("discount")]
        public decimal? Discount { get; set; }

        [JsonPropertyName("discount_type")]
        public string? DiscountType { get; set; }

        [JsonPropertyName("is_discount_before_tax")]
        public bool? IsDiscountBeforeTax { get; set; }

        [JsonPropertyName("line_items")]
        public List<EstimateLineItem>? LineItems { get; set; }

        [JsonPropertyName("sub_total")]
        public decimal? SubTotal { get; set; }

        [JsonPropertyName("tax_total")]
        public decimal? TaxTotal { get; set; }

        [JsonPropertyName("total")]
        public decimal? Total { get; set; }

        [JsonPropertyName("price_precision")]
        public int? PricePrecision { get; set; }

        [JsonPropertyName("custom_fields")]
        public List<CustomField>? CustomFields { get; set; }

        [JsonPropertyName("billing_address")]
        public Address? BillingAddress { get; set; }

        [JsonPropertyName("shipping_address")]
        public Address? ShippingAddress { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("terms")]
        public string? Terms { get; set; }

        [JsonPropertyName("salesperson_id")]
        public string? SalespersonId { get; set; }

        [JsonPropertyName("salesperson_name")]
        public string? SalespersonName { get; set; }
    }

    // Purchase Order entity
    public class PurchaseOrder : ZohoBooksBaseEntity
    {
        [JsonPropertyName("purchaseorder_number")]
        public string? PurchaseorderNumber { get; set; }

        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("delivery_date")]
        public DateTime? DeliveryDate { get; set; }

        [JsonPropertyName("reference_number")]
        public string? ReferenceNumber { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("vendor_id")]
        public string? VendorId { get; set; }

        [JsonPropertyName("vendor_name")]
        public string? VendorName { get; set; }

        [JsonPropertyName("currency_id")]
        public string? CurrencyId { get; set; }

        [JsonPropertyName("currency_code")]
        public string? CurrencyCode { get; set; }

        [JsonPropertyName("exchange_rate")]
        public decimal? ExchangeRate { get; set; }

        [JsonPropertyName("line_items")]
        public List<PurchaseOrderLineItem>? LineItems { get; set; }

        [JsonPropertyName("sub_total")]
        public decimal? SubTotal { get; set; }

        [JsonPropertyName("tax_total")]
        public decimal? TaxTotal { get; set; }

        [JsonPropertyName("total")]
        public decimal? Total { get; set; }

        [JsonPropertyName("price_precision")]
        public int? PricePrecision { get; set; }

        [JsonPropertyName("custom_fields")]
        public List<CustomField>? CustomFields { get; set; }

        [JsonPropertyName("billing_address")]
        public Address? BillingAddress { get; set; }

        [JsonPropertyName("delivery_address")]
        public Address? DeliveryAddress { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("terms")]
        public string? Terms { get; set; }

        [JsonPropertyName("attachment_name")]
        public string? AttachmentName { get; set; }
    }

    // Journal entity
    public class Journal : ZohoBooksBaseEntity
    {
        [JsonPropertyName("journal_number")]
        public string? JournalNumber { get; set; }

        [JsonPropertyName("reference_number")]
        public string? ReferenceNumber { get; set; }

        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("total")]
        public decimal? Total { get; set; }

        [JsonPropertyName("line_items")]
        public List<JournalLineItem>? LineItems { get; set; }

        [JsonPropertyName("custom_fields")]
        public List<CustomField>? CustomFields { get; set; }

        [JsonPropertyName("documents")]
        public List<Document>? Documents { get; set; }
    }

    // Chart of Accounts entity
    public class ChartOfAccount : ZohoBooksBaseEntity
    {
        [JsonPropertyName("account_name")]
        public string? AccountName { get; set; }

        [JsonPropertyName("account_code")]
        public string? AccountCode { get; set; }

        [JsonPropertyName("account_type")]
        public string? AccountType { get; set; }

        [JsonPropertyName("is_user_created")]
        public bool? IsUserCreated { get; set; }

        [JsonPropertyName("is_system_account")]
        public bool? IsSystemAccount { get; set; }

        [JsonPropertyName("is_active")]
        public bool? IsActive { get; set; }

        [JsonPropertyName("can_delete")]
        public bool? CanDelete { get; set; }

        [JsonPropertyName("parent_account_id")]
        public string? ParentAccountId { get; set; }

        [JsonPropertyName("parent_account_name")]
        public string? ParentAccountName { get; set; }

        [JsonPropertyName("depth")]
        public int? Depth { get; set; }

        [JsonPropertyName("has_children")]
        public bool? HasChildren { get; set; }

        [JsonPropertyName("currency_id")]
        public string? CurrencyId { get; set; }

        [JsonPropertyName("currency_code")]
        public string? CurrencyCode { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("current_balance")]
        public decimal? CurrentBalance { get; set; }

        [JsonPropertyName("bank_balance")]
        public decimal? BankBalance { get; set; }
    }

    // Bank Account entity
    public class BankAccount : ZohoBooksBaseEntity
    {
        [JsonPropertyName("account_name")]
        public string? AccountName { get; set; }

        [JsonPropertyName("account_code")]
        public string? AccountCode { get; set; }

        [JsonPropertyName("account_type")]
        public string? AccountType { get; set; }

        [JsonPropertyName("currency_id")]
        public string? CurrencyId { get; set; }

        [JsonPropertyName("currency_code")]
        public string? CurrencyCode { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("account_number")]
        public string? AccountNumber { get; set; }

        [JsonPropertyName("routing_number")]
        public string? RoutingNumber { get; set; }

        [JsonPropertyName("is_primary_account")]
        public bool? IsPrimaryAccount { get; set; }

        [JsonPropertyName("is_paypal_account")]
        public bool? IsPaypalAccount { get; set; }

        [JsonPropertyName("paypal_email_address")]
        public string? PaypalEmailAddress { get; set; }

        [JsonPropertyName("current_balance")]
        public decimal? CurrentBalance { get; set; }

        [JsonPropertyName("bank_balance")]
        public decimal? BankBalance { get; set; }

        [JsonPropertyName("uncategorized_transactions")]
        public int? UncategorizedTransactions { get; set; }

        [JsonPropertyName("is_active")]
        public bool? IsActive { get; set; }
    }

    // Bank Transaction entity
    public class BankTransaction : ZohoBooksBaseEntity
    {
        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("reference_number")]
        public string? ReferenceNumber { get; set; }

        [JsonPropertyName("transaction_type")]
        public string? TransactionType { get; set; }

        [JsonPropertyName("amount")]
        public decimal? Amount { get; set; }

        [JsonPropertyName("account_id")]
        public string? AccountId { get; set; }

        [JsonPropertyName("account_name")]
        public string? AccountName { get; set; }

        [JsonPropertyName("payee")]
        public string? Payee { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("cleared_status")]
        public string? ClearedStatus { get; set; }

        [JsonPropertyName("reconcile_status")]
        public string? ReconcileStatus { get; set; }

        [JsonPropertyName("customer_id")]
        public string? CustomerId { get; set; }

        [JsonPropertyName("customer_name")]
        public string? CustomerName { get; set; }

        [JsonPropertyName("vendor_id")]
        public string? VendorId { get; set; }

        [JsonPropertyName("vendor_name")]
        public string? VendorName { get; set; }

        [JsonPropertyName("invoice_id")]
        public string? InvoiceId { get; set; }

        [JsonPropertyName("invoice_number")]
        public string? InvoiceNumber { get; set; }

        [JsonPropertyName("bill_id")]
        public string? BillId { get; set; }

        [JsonPropertyName("bill_number")]
        public string? BillNumber { get; set; }

        [JsonPropertyName("currency_id")]
        public string? CurrencyId { get; set; }

        [JsonPropertyName("currency_code")]
        public string? CurrencyCode { get; set; }

        [JsonPropertyName("exchange_rate")]
        public decimal? ExchangeRate { get; set; }

        [JsonPropertyName("debit_or_credit")]
        public string? DebitOrCredit { get; set; }

        [JsonPropertyName("offset_account_name")]
        public string? OffsetAccountName { get; set; }

        [JsonPropertyName("imported_transaction_id")]
        public string? ImportedTransactionId { get; set; }

        [JsonPropertyName("split_transactions")]
        public List<SplitTransaction>? SplitTransactions { get; set; }
    }

    // Expense entity
    public class Expense : ZohoBooksBaseEntity
    {
        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("account_id")]
        public string? AccountId { get; set; }

        [JsonPropertyName("account_name")]
        public string? AccountName { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("currency_id")]
        public string? CurrencyId { get; set; }

        [JsonPropertyName("currency_code")]
        public string? CurrencyCode { get; set; }

        [JsonPropertyName("exchange_rate")]
        public decimal? ExchangeRate { get; set; }

        [JsonPropertyName("amount")]
        public decimal? Amount { get; set; }

        [JsonPropertyName("tax_id")]
        public string? TaxId { get; set; }

        [JsonPropertyName("tax_name")]
        public string? TaxName { get; set; }

        [JsonPropertyName("tax_percentage")]
        public decimal? TaxPercentage { get; set; }

        [JsonPropertyName("tax_amount")]
        public decimal? TaxAmount { get; set; }

        [JsonPropertyName("is_inclusive_tax")]
        public bool? IsInclusiveTax { get; set; }

        [JsonPropertyName("reference_number")]
        public string? ReferenceNumber { get; set; }

        [JsonPropertyName("is_billable")]
        public bool? IsBillable { get; set; }

        [JsonPropertyName("customer_id")]
        public string? CustomerId { get; set; }

        [JsonPropertyName("customer_name")]
        public string? CustomerName { get; set; }

        [JsonPropertyName("vendor_id")]
        public string? VendorId { get; set; }

        [JsonPropertyName("vendor_name")]
        public string? VendorName { get; set; }

        [JsonPropertyName("project_id")]
        public string? ProjectId { get; set; }

        [JsonPropertyName("project_name")]
        public string? ProjectName { get; set; }

        [JsonPropertyName("expense_receipt_name")]
        public string? ExpenseReceiptName { get; set; }

        [JsonPropertyName("mileage_rate")]
        public decimal? MileageRate { get; set; }

        [JsonPropertyName("mileage_type")]
        public string? MileageType { get; set; }

        [JsonPropertyName("start_reading")]
        public decimal? StartReading { get; set; }

        [JsonPropertyName("end_reading")]
        public decimal? EndReading { get; set; }

        [JsonPropertyName("distance")]
        public decimal? Distance { get; set; }

        [JsonPropertyName("employee_rate")]
        public decimal? EmployeeRate { get; set; }

        [JsonPropertyName("custom_fields")]
        public List<CustomField>? CustomFields { get; set; }

        [JsonPropertyName("line_items")]
        public List<ExpenseLineItem>? LineItems { get; set; }
    }

    // Project entity
    public class Project : ZohoBooksBaseEntity
    {
        [JsonPropertyName("project_name")]
        public string? ProjectName { get; set; }

        [JsonPropertyName("project_number")]
        public string? ProjectNumber { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("customer_id")]
        public string? CustomerId { get; set; }

        [JsonPropertyName("customer_name")]
        public string? CustomerName { get; set; }

        [JsonPropertyName("currency_id")]
        public string? CurrencyId { get; set; }

        [JsonPropertyName("currency_code")]
        public string? CurrencyCode { get; set; }

        [JsonPropertyName("billing_type")]
        public string? BillingType { get; set; }

        [JsonPropertyName("billing_rate")]
        public decimal? BillingRate { get; set; }

        [JsonPropertyName("budget_type")]
        public string? BudgetType { get; set; }

        [JsonPropertyName("budget_amount")]
        public decimal? BudgetAmount { get; set; }

        [JsonPropertyName("total_hours")]
        public decimal? TotalHours { get; set; }

        [JsonPropertyName("billed_hours")]
        public decimal? BilledHours { get; set; }

        [JsonPropertyName("unbilled_hours")]
        public decimal? UnbilledHours { get; set; }

        [JsonPropertyName("user_id")]
        public string? UserId { get; set; }

        [JsonPropertyName("user_name")]
        public string? UserName { get; set; }

        [JsonPropertyName("task_id")]
        public string? TaskId { get; set; }

        [JsonPropertyName("task_name")]
        public string? TaskName { get; set; }

        [JsonPropertyName("rate")]
        public decimal? Rate { get; set; }

        [JsonPropertyName("project_start_date")]
        public DateTime? ProjectStartDate { get; set; }

        [JsonPropertyName("project_end_date")]
        public DateTime? ProjectEndDate { get; set; }

        [JsonPropertyName("custom_fields")]
        public List<CustomField>? CustomFields { get; set; }
    }

    // Timesheet entity
    public class Timesheet : ZohoBooksBaseEntity
    {
        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("employee_id")]
        public string? EmployeeId { get; set; }

        [JsonPropertyName("employee_name")]
        public string? EmployeeName { get; set; }

        [JsonPropertyName("project_id")]
        public string? ProjectId { get; set; }

        [JsonPropertyName("project_name")]
        public string? ProjectName { get; set; }

        [JsonPropertyName("task_id")]
        public string? TaskId { get; set; }

        [JsonPropertyName("task_name")]
        public string? TaskName { get; set; }

        [JsonPropertyName("log_time")]
        public decimal? LogTime { get; set; }

        [JsonPropertyName("start_time")]
        public string? StartTime { get; set; }

        [JsonPropertyName("end_time")]
        public string? EndTime { get; set; }

        [JsonPropertyName("is_billable")]
        public bool? IsBillable { get; set; }

        [JsonPropertyName("billed_status")]
        public string? BilledStatus { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("timer_started_at")]
        public DateTime? TimerStartedAt { get; set; }

        [JsonPropertyName("timer_duration_in_minutes")]
        public int? TimerDurationInMinutes { get; set; }
    }

    // Tax entity
    public class Tax : ZohoBooksBaseEntity
    {
        [JsonPropertyName("tax_name")]
        public string? TaxName { get; set; }

        [JsonPropertyName("tax_percentage")]
        public decimal? TaxPercentage { get; set; }

        [JsonPropertyName("tax_type")]
        public string? TaxType { get; set; }

        [JsonPropertyName("tax_specific_type")]
        public string? TaxSpecificType { get; set; }

        [JsonPropertyName("is_default")]
        public bool? IsDefault { get; set; }

        [JsonPropertyName("is_editable")]
        public bool? IsEditable { get; set; }

        [JsonPropertyName("is_active")]
        public bool? IsActive { get; set; }
    }

    // User entity
    public class User : ZohoBooksBaseEntity
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("user_role")]
        public string? UserRole { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("role_id")]
        public string? RoleId { get; set; }

        [JsonPropertyName("role_name")]
        public string? RoleName { get; set; }

        [JsonPropertyName("is_live_user")]
        public bool? IsLiveUser { get; set; }

        [JsonPropertyName("photo_url")]
        public string? PhotoUrl { get; set; }

        [JsonPropertyName("invite_link")]
        public string? InviteLink { get; set; }

        [JsonPropertyName("invited_time")]
        public DateTime? InvitedTime { get; set; }

        [JsonPropertyName("confirmed_time")]
        public DateTime? ConfirmedTime { get; set; }

        [JsonPropertyName("last_login_time")]
        public DateTime? LastLoginTime { get; set; }
    }

    // Currency entity
    public class Currency : ZohoBooksBaseEntity
    {
        [JsonPropertyName("currency_name")]
        public string? CurrencyName { get; set; }

        [JsonPropertyName("currency_code")]
        public string? CurrencyCode { get; set; }

        [JsonPropertyName("currency_symbol")]
        public string? CurrencySymbol { get; set; }

        [JsonPropertyName("price_precision")]
        public int? PricePrecision { get; set; }

        [JsonPropertyName("is_base_currency")]
        public bool? IsBaseCurrency { get; set; }

        [JsonPropertyName("exchange_rate")]
        public decimal? ExchangeRate { get; set; }

        [JsonPropertyName("effective_date")]
        public DateTime? EffectiveDate { get; set; }

        [JsonPropertyName("is_active")]
        public bool? IsActive { get; set; }
    }

    // Custom Field entity
    public class CustomField : ZohoBooksBaseEntity
    {
        [JsonPropertyName("field_id")]
        public string? FieldId { get; set; }

        [JsonPropertyName("field_name")]
        public string? FieldName { get; set; }

        [JsonPropertyName("field_value")]
        public string? FieldValue { get; set; }

        [JsonPropertyName("data_type")]
        public string? DataType { get; set; }

        [JsonPropertyName("is_required")]
        public bool? IsRequired { get; set; }

        [JsonPropertyName("is_active")]
        public bool? IsActive { get; set; }

        [JsonPropertyName("show_in_all_pdf")]
        public bool? ShowInAllPdf { get; set; }

        [JsonPropertyName("index")]
        public int? Index { get; set; }

        [JsonPropertyName("placeholder")]
        public string? Placeholder { get; set; }
    }

    // Supporting classes
    public class Address
    {
        [JsonPropertyName("address")]
        public string? AddressLine { get; set; }

        [JsonPropertyName("street2")]
        public string? Street2 { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("zip")]
        public string? Zip { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("fax")]
        public string? Fax { get; set; }
    }

    public class ContactPerson
    {
        [JsonPropertyName("contact_person_id")]
        public string? ContactPersonId { get; set; }

        [JsonPropertyName("salutation")]
        public string? Salutation { get; set; }

        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string? LastName { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("mobile")]
        public string? Mobile { get; set; }

        [JsonPropertyName("is_primary_contact")]
        public bool? IsPrimaryContact { get; set; }

        [JsonPropertyName("skype")]
        public string? Skype { get; set; }

        [JsonPropertyName("designation")]
        public string? Designation { get; set; }

        [JsonPropertyName("department")]
        public string? Department { get; set; }
    }

    public class DefaultTemplates
    {
        [JsonPropertyName("invoice_template_id")]
        public string? InvoiceTemplateId { get; set; }

        [JsonPropertyName("invoice_template_name")]
        public string? InvoiceTemplateName { get; set; }

        [JsonPropertyName("bill_template_id")]
        public string? BillTemplateId { get; set; }

        [JsonPropertyName("bill_template_name")]
        public string? BillTemplateName { get; set; }

        [JsonPropertyName("estimate_template_id")]
        public string? EstimateTemplateId { get; set; }

        [JsonPropertyName("estimate_template_name")]
        public string? EstimateTemplateName { get; set; }

        [JsonPropertyName("creditnote_template_id")]
        public string? CreditnoteTemplateId { get; set; }

        [JsonPropertyName("creditnote_template_name")]
        public string? CreditnoteTemplateName { get; set; }

        [JsonPropertyName("purchaseorder_template_id")]
        public string? PurchaseorderTemplateId { get; set; }

        [JsonPropertyName("purchaseorder_template_name")]
        public string? PurchaseorderTemplateName { get; set; }
    }

    public class Document
    {
        [JsonPropertyName("document_id")]
        public string? DocumentId { get; set; }

        [JsonPropertyName("file_name")]
        public string? FileName { get; set; }

        [JsonPropertyName("file_type")]
        public string? FileType { get; set; }

        [JsonPropertyName("file_size_formatted")]
        public string? FileSizeFormatted { get; set; }

        [JsonPropertyName("attachment_order")]
        public int? AttachmentOrder { get; set; }

        [JsonPropertyName("can_send_in_mail")]
        public bool? CanSendInMail { get; set; }
    }

    public class PaymentInvoice
    {
        [JsonPropertyName("invoice_id")]
        public string? InvoiceId { get; set; }

        [JsonPropertyName("invoice_number")]
        public string? InvoiceNumber { get; set; }

        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("invoice_amount")]
        public decimal? InvoiceAmount { get; set; }

        [JsonPropertyName("amount_applied")]
        public decimal? AmountApplied { get; set; }

        [JsonPropertyName("balance_amount")]
        public decimal? BalanceAmount { get; set; }
    }

    public class PaymentBill
    {
        [JsonPropertyName("bill_id")]
        public string? BillId { get; set; }

        [JsonPropertyName("bill_number")]
        public string? BillNumber { get; set; }

        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("bill_amount")]
        public decimal? BillAmount { get; set; }

        [JsonPropertyName("amount_applied")]
        public decimal? AmountApplied { get; set; }

        [JsonPropertyName("balance_amount")]
        public decimal? BalanceAmount { get; set; }
    }

    public class CreditNoteLineItem
    {
        [JsonPropertyName("item_id")]
        public string? ItemId { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("rate")]
        public decimal? Rate { get; set; }

        [JsonPropertyName("quantity")]
        public decimal? Quantity { get; set; }

        [JsonPropertyName("discount")]
        public decimal? Discount { get; set; }

        [JsonPropertyName("tax_id")]
        public string? TaxId { get; set; }

        [JsonPropertyName("tax_name")]
        public string? TaxName { get; set; }

        [JsonPropertyName("tax_type")]
        public string? TaxType { get; set; }

        [JsonPropertyName("tax_percentage")]
        public decimal? TaxPercentage { get; set; }

        [JsonPropertyName("item_total")]
        public decimal? ItemTotal { get; set; }

        [JsonPropertyName("account_id")]
        public string? AccountId { get; set; }

        [JsonPropertyName("account_name")]
        public string? AccountName { get; set; }
    }

    public class EstimateLineItem
    {
        [JsonPropertyName("item_id")]
        public string? ItemId { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("rate")]
        public decimal? Rate { get; set; }

        [JsonPropertyName("quantity")]
        public decimal? Quantity { get; set; }

        [JsonPropertyName("discount")]
        public decimal? Discount { get; set; }

        [JsonPropertyName("tax_id")]
        public string? TaxId { get; set; }

        [JsonPropertyName("tax_name")]
        public string? TaxName { get; set; }

        [JsonPropertyName("tax_type")]
        public string? TaxType { get; set; }

        [JsonPropertyName("tax_percentage")]
        public decimal? TaxPercentage { get; set; }

        [JsonPropertyName("item_total")]
        public decimal? ItemTotal { get; set; }

        [JsonPropertyName("account_id")]
        public string? AccountId { get; set; }

        [JsonPropertyName("account_name")]
        public string? AccountName { get; set; }
    }

    public class PurchaseOrderLineItem
    {
        [JsonPropertyName("item_id")]
        public string? ItemId { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("rate")]
        public decimal? Rate { get; set; }

        [JsonPropertyName("quantity")]
        public decimal? Quantity { get; set; }

        [JsonPropertyName("discount")]
        public decimal? Discount { get; set; }

        [JsonPropertyName("tax_id")]
        public string? TaxId { get; set; }

        [JsonPropertyName("tax_name")]
        public string? TaxName { get; set; }

        [JsonPropertyName("tax_type")]
        public string? TaxType { get; set; }

        [JsonPropertyName("tax_percentage")]
        public decimal? TaxPercentage { get; set; }

        [JsonPropertyName("item_total")]
        public decimal? ItemTotal { get; set; }

        [JsonPropertyName("account_id")]
        public string? AccountId { get; set; }

        [JsonPropertyName("account_name")]
        public string? AccountName { get; set; }
    }

    public class JournalLineItem
    {
        [JsonPropertyName("account_id")]
        public string? AccountId { get; set; }

        [JsonPropertyName("account_name")]
        public string? AccountName { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("debit_or_credit")]
        public string? DebitOrCredit { get; set; }

        [JsonPropertyName("amount")]
        public decimal? Amount { get; set; }

        [JsonPropertyName("tax_id")]
        public string? TaxId { get; set; }

        [JsonPropertyName("tax_name")]
        public string? TaxName { get; set; }

        [JsonPropertyName("tax_type")]
        public string? TaxType { get; set; }

        [JsonPropertyName("tax_percentage")]
        public decimal? TaxPercentage { get; set; }

        [JsonPropertyName("tags")]
        public List<string>? Tags { get; set; }
    }

    public class SplitTransaction
    {
        [JsonPropertyName("transaction_id")]
        public string? TransactionId { get; set; }

        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("reference_number")]
        public string? ReferenceNumber { get; set; }

        [JsonPropertyName("customer_id")]
        public string? CustomerId { get; set; }

        [JsonPropertyName("customer_name")]
        public string? CustomerName { get; set; }

        [JsonPropertyName("vendor_id")]
        public string? VendorId { get; set; }

        [JsonPropertyName("vendor_name")]
        public string? VendorName { get; set; }

        [JsonPropertyName("amount")]
        public decimal? Amount { get; set; }

        [JsonPropertyName("debit_or_credit")]
        public string? DebitOrCredit { get; set; }

        [JsonPropertyName("account_id")]
        public string? AccountId { get; set; }

        [JsonPropertyName("account_name")]
        public string? AccountName { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("tax_id")]
        public string? TaxId { get; set; }

        [JsonPropertyName("tax_name")]
        public string? TaxName { get; set; }

        [JsonPropertyName("exchange_rate")]
        public decimal? ExchangeRate { get; set; }

        [JsonPropertyName("currency_id")]
        public string? CurrencyId { get; set; }

        [JsonPropertyName("currency_code")]
        public string? CurrencyCode { get; set; }
    }

    public class ExpenseLineItem
    {
        [JsonPropertyName("account_id")]
        public string? AccountId { get; set; }

        [JsonPropertyName("account_name")]
        public string? AccountName { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("amount")]
        public decimal? Amount { get; set; }

        [JsonPropertyName("tax_id")]
        public string? TaxId { get; set; }

        [JsonPropertyName("tax_name")]
        public string? TaxName { get; set; }

        [JsonPropertyName("tax_type")]
        public string? TaxType { get; set; }

        [JsonPropertyName("tax_percentage")]
        public decimal? TaxPercentage { get; set; }

        [JsonPropertyName("customer_id")]
        public string? CustomerId { get; set; }

        [JsonPropertyName("customer_name")]
        public string? CustomerName { get; set; }

        [JsonPropertyName("project_id")]
        public string? ProjectId { get; set; }

        [JsonPropertyName("project_name")]
        public string? ProjectName { get; set; }
    }

    // Registry (maps your fixed entity names to CLR types)
    public static class ZohoBooksEntityRegistry
    {
        public static readonly Dictionary<string, Type> Types = new(StringComparer.OrdinalIgnoreCase)
        {
            // Organization
            ["organizations"] = typeof(Organization),

            // Contacts
            ["contacts"] = typeof(Contact),
            ["customers"] = typeof(Customer),
            ["vendors"] = typeof(Vendor),

            // Items
            ["items"] = typeof(Item),

            // Invoices
            ["invoices"] = typeof(Invoice),
            ["invoice_items"] = typeof(InvoiceLineItem),

            // Bills
            ["bills"] = typeof(Bill),
            ["bill_items"] = typeof(BillLineItem),

            // Payments
            ["payments"] = typeof(Payment),
            ["vendorpayments"] = typeof(Payment),

            // Credit Notes
            ["creditnotes"] = typeof(CreditNote),

            // Estimates
            ["estimates"] = typeof(Estimate),

            // Purchase Orders
            ["purchaseorders"] = typeof(PurchaseOrder),

            // Journals
            ["journals"] = typeof(Journal),

            // Chart of Accounts
            ["chartofaccounts"] = typeof(ChartOfAccount),
            ["accounts"] = typeof(ChartOfAccount),

            // Bank Accounts & Transactions
            ["bankaccounts"] = typeof(BankAccount),
            ["banktransactions"] = typeof(BankTransaction),

            // Expenses
            ["expenses"] = typeof(Expense),

            // Projects
            ["projects"] = typeof(Project),

            // Time Entries
            ["timesheets"] = typeof(Timesheet),

            // Taxes
            ["taxes"] = typeof(Tax),

            // Users
            ["users"] = typeof(User),

            // Currencies
            ["currencies"] = typeof(Currency),

            // Custom Fields
            ["customfields"] = typeof(CustomField)
        };

        public static IReadOnlyList<string> Names => new List<string>(Types.Keys);
    }
}