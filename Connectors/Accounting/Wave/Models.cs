using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Wave
{
    // Base class for Wave entities
    public class WaveBaseEntity
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("modified_at")]
        public DateTime? ModifiedAt { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }

    // Business entity
    public class Business : WaveBaseEntity
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("is_personal")]
        public bool? IsPersonal { get; set; }

        [JsonPropertyName("type")]
        public BusinessType? Type { get; set; }

        [JsonPropertyName("currency")]
        public Currency? Currency { get; set; }

        [JsonPropertyName("timezone")]
        public string? Timezone { get; set; }

        [JsonPropertyName("address")]
        public Address? Address { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("fax")]
        public string? Fax { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("website")]
        public string? Website { get; set; }

        [JsonPropertyName("tax_number")]
        public string? TaxNumber { get; set; }

        [JsonPropertyName("fiscal_year_start_month")]
        public int? FiscalYearStartMonth { get; set; }

        [JsonPropertyName("date_format")]
        public string? DateFormat { get; set; }

        [JsonPropertyName("is_active")]
        public bool? IsActive { get; set; }
    }

    // Customer entity
    public class Customer : WaveBaseEntity
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string? LastName { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("mobile")]
        public string? Mobile { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("fax")]
        public string? Fax { get; set; }

        [JsonPropertyName("toll_free")]
        public string? TollFree { get; set; }

        [JsonPropertyName("website")]
        public string? Website { get; set; }

        [JsonPropertyName("internal_notes")]
        public string? InternalNotes { get; set; }

        [JsonPropertyName("address")]
        public Address? Address { get; set; }

        [JsonPropertyName("shipping_address")]
        public Address? ShippingAddress { get; set; }

        [JsonPropertyName("currency")]
        public Currency? Currency { get; set; }

        [JsonPropertyName("is_archived")]
        public bool? IsArchived { get; set; }
    }

    // Product entity
    public class Product : WaveBaseEntity
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("unit_price")]
        public decimal? UnitPrice { get; set; }

        [JsonPropertyName("default_quantity")]
        public decimal? DefaultQuantity { get; set; }

        [JsonPropertyName("income_account")]
        public Account? IncomeAccount { get; set; }

        [JsonPropertyName("expense_account")]
        public Account? ExpenseAccount { get; set; }

        [JsonPropertyName("asset_account")]
        public Account? AssetAccount { get; set; }

        [JsonPropertyName("is_sold")]
        public bool? IsSold { get; set; }

        [JsonPropertyName("is_bought")]
        public bool? IsBought { get; set; }

        [JsonPropertyName("is_active")]
        public bool? IsActive { get; set; }

        [JsonPropertyName("product_type")]
        public ProductType? ProductType { get; set; }

        [JsonPropertyName("sales_tax")]
        public SalesTax? SalesTax { get; set; }

        [JsonPropertyName("purchase_tax")]
        public PurchaseTax? PurchaseTax { get; set; }
    }

    // Invoice entity
    public class Invoice : WaveBaseEntity
    {
        [JsonPropertyName("invoice_number")]
        public string? InvoiceNumber { get; set; }

        [JsonPropertyName("invoice_date")]
        public DateTime? InvoiceDate { get; set; }

        [JsonPropertyName("due_date")]
        public DateTime? DueDate { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("customer")]
        public Customer? Customer { get; set; }

        [JsonPropertyName("currency")]
        public Currency? Currency { get; set; }

        [JsonPropertyName("memo")]
        public string? Memo { get; set; }

        [JsonPropertyName("footer")]
        public string? Footer { get; set; }

        [JsonPropertyName("disable_credit_card_payments")]
        public bool? DisableCreditCardPayments { get; set; }

        [JsonPropertyName("disable_bank_payments")]
        public bool? DisableBankPayments { get; set; }

        [JsonPropertyName("item_title")]
        public string? ItemTitle { get; set; }

        [JsonPropertyName("unit_title")]
        public string? UnitTitle { get; set; }

        [JsonPropertyName("price_title")]
        public string? PriceTitle { get; set; }

        [JsonPropertyName("amount_title")]
        public string? AmountTitle { get; set; }

        [JsonPropertyName("hide_item_name")]
        public bool? HideItemName { get; set; }

        [JsonPropertyName("hide_item_description")]
        public bool? HideItemDescription { get; set; }

        [JsonPropertyName("hide_quantity")]
        public bool? HideQuantity { get; set; }

        [JsonPropertyName("hide_price")]
        public bool? HidePrice { get; set; }

        [JsonPropertyName("hide_amount")]
        public bool? HideAmount { get; set; }

        [JsonPropertyName("items")]
        public List<InvoiceItem>? Items { get; set; }

        [JsonPropertyName("subtotal")]
        public Money? Subtotal { get; set; }

        [JsonPropertyName("tax_total")]
        public Money? TaxTotal { get; set; }

        [JsonPropertyName("total")]
        public Money? Total { get; set; }

        [JsonPropertyName("amount_due")]
        public Money? AmountDue { get; set; }

        [JsonPropertyName("last_sent_at")]
        public DateTime? LastSentAt { get; set; }

        [JsonPropertyName("last_sent_via")]
        public string? LastSentVia { get; set; }

        [JsonPropertyName("last_viewed_at")]
        public DateTime? LastViewedAt { get; set; }
    }

    // Invoice Item entity
    public class InvoiceItem : WaveBaseEntity
    {
        [JsonPropertyName("product")]
        public Product? Product { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("quantity")]
        public decimal? Quantity { get; set; }

        [JsonPropertyName("unit_price")]
        public Money? UnitPrice { get; set; }

        [JsonPropertyName("line_amount")]
        public Money? LineAmount { get; set; }

        [JsonPropertyName("account")]
        public Account? Account { get; set; }

        [JsonPropertyName("tax_amount")]
        public Money? TaxAmount { get; set; }

        [JsonPropertyName("taxes")]
        public List<SalesTax>? Taxes { get; set; }
    }

    // Payment entity
    public class Payment : WaveBaseEntity
    {
        [JsonPropertyName("amount")]
        public Money? Amount { get; set; }

        [JsonPropertyName("customer")]
        public Customer? Customer { get; set; }

        [JsonPropertyName("invoice")]
        public Invoice? Invoice { get; set; }

        [JsonPropertyName("payment_date")]
        public DateTime? PaymentDate { get; set; }

        [JsonPropertyName("payment_method")]
        public string? PaymentMethod { get; set; }

        [JsonPropertyName("reference_number")]
        public string? ReferenceNumber { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("items")]
        public List<PaymentItem>? Items { get; set; }
    }

    // Payment Item entity
    public class PaymentItem : WaveBaseEntity
    {
        [JsonPropertyName("invoice")]
        public Invoice? Invoice { get; set; }

        [JsonPropertyName("amount")]
        public Money? Amount { get; set; }
    }

    // Bill entity
    public class Bill : WaveBaseEntity
    {
        [JsonPropertyName("bill_number")]
        public string? BillNumber { get; set; }

        [JsonPropertyName("bill_date")]
        public DateTime? BillDate { get; set; }

        [JsonPropertyName("due_date")]
        public DateTime? DueDate { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("vendor")]
        public Customer? Vendor { get; set; }

        [JsonPropertyName("currency")]
        public Currency? Currency { get; set; }

        [JsonPropertyName("memo")]
        public string? Memo { get; set; }

        [JsonPropertyName("items")]
        public List<BillItem>? Items { get; set; }

        [JsonPropertyName("subtotal")]
        public Money? Subtotal { get; set; }

        [JsonPropertyName("tax_total")]
        public Money? TaxTotal { get; set; }

        [JsonPropertyName("total")]
        public Money? Total { get; set; }

        [JsonPropertyName("amount_due")]
        public Money? AmountDue { get; set; }
    }

    // Bill Item entity
    public class BillItem : WaveBaseEntity
    {
        [JsonPropertyName("product")]
        public Product? Product { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("quantity")]
        public decimal? Quantity { get; set; }

        [JsonPropertyName("unit_price")]
        public Money? UnitPrice { get; set; }

        [JsonPropertyName("line_amount")]
        public Money? LineAmount { get; set; }

        [JsonPropertyName("account")]
        public Account? Account { get; set; }

        [JsonPropertyName("tax_amount")]
        public Money? TaxAmount { get; set; }

        [JsonPropertyName("taxes")]
        public List<PurchaseTax>? Taxes { get; set; }
    }

    // Account entity
    public class Account : WaveBaseEntity
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("subtype")]
        public string? Subtype { get; set; }

        [JsonPropertyName("is_archived")]
        public bool? IsArchived { get; set; }

        [JsonPropertyName("currency")]
        public Currency? Currency { get; set; }

        [JsonPropertyName("normal_balance_type")]
        public string? NormalBalanceType { get; set; }
    }

    // Transaction entity
    public class Transaction : WaveBaseEntity
    {
        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("amount")]
        public Money? Amount { get; set; }

        [JsonPropertyName("balance")]
        public Money? Balance { get; set; }

        [JsonPropertyName("account")]
        public Account? Account { get; set; }

        [JsonPropertyName("transaction_type")]
        public string? TransactionType { get; set; }

        [JsonPropertyName("attachments")]
        public List<Attachment>? Attachments { get; set; }
    }

    // Tax entity
    public class Tax : WaveBaseEntity
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("abbreviation")]
        public string? Abbreviation { get; set; }

        [JsonPropertyName("rate")]
        public decimal? Rate { get; set; }

        [JsonPropertyName("is_recoverable")]
        public bool? IsRecoverable { get; set; }

        [JsonPropertyName("is_compound")]
        public bool? IsCompound { get; set; }

        [JsonPropertyName("tax_components")]
        public List<TaxComponent>? TaxComponents { get; set; }
    }

    // Sales Tax entity
    public class SalesTax : Tax
    {
        // Inherits all properties from Tax
    }

    // Purchase Tax entity
    public class PurchaseTax : Tax
    {
        // Inherits all properties from Tax
    }

    // User entity
    public class User : WaveBaseEntity
    {
        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string? LastName { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("is_owner")]
        public bool? IsOwner { get; set; }

        [JsonPropertyName("is_active")]
        public bool? IsActive { get; set; }

        [JsonPropertyName("last_login_at")]
        public DateTime? LastLoginAt { get; set; }
    }

    // Attachment entity
    public class Attachment : WaveBaseEntity
    {
        [JsonPropertyName("file_name")]
        public string? FileName { get; set; }

        [JsonPropertyName("file_size")]
        public long? FileSize { get; set; }

        [JsonPropertyName("content_type")]
        public string? ContentType { get; set; }

        [JsonPropertyName("download_url")]
        public string? DownloadUrl { get; set; }

        [JsonPropertyName("preview_url")]
        public string? PreviewUrl { get; set; }
    }

    // Currency entity
    public class Currency : WaveBaseEntity
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("symbol")]
        public string? Symbol { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("plural_name")]
        public string? PluralName { get; set; }

        [JsonPropertyName("decimal_places")]
        public int? DecimalPlaces { get; set; }

        [JsonPropertyName("is_base_currency")]
        public bool? IsBaseCurrency { get; set; }
    }

    // Country entity
    public class Country : WaveBaseEntity
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("currency")]
        public Currency? Currency { get; set; }

        [JsonPropertyName("taxes")]
        public List<Tax>? Taxes { get; set; }
    }

    // Business Type entity
    public class BusinessType : WaveBaseEntity
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("is_active")]
        public bool? IsActive { get; set; }
    }

    // Supporting classes
    public class Address
    {
        [JsonPropertyName("address_line1")]
        public string? AddressLine1 { get; set; }

        [JsonPropertyName("address_line2")]
        public string? AddressLine2 { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("province")]
        public Province? Province { get; set; }

        [JsonPropertyName("country")]
        public Country? Country { get; set; }

        [JsonPropertyName("postal_code")]
        public string? PostalCode { get; set; }
    }

    public class Province
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("country")]
        public Country? Country { get; set; }
    }

    public class Money
    {
        [JsonPropertyName("value")]
        public decimal? Value { get; set; }

        [JsonPropertyName("currency")]
        public Currency? Currency { get; set; }
    }

    public class ProductType
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("is_active")]
        public bool? IsActive { get; set; }
    }

    public class TaxComponent
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("rate")]
        public decimal? Rate { get; set; }

        [JsonPropertyName("is_compound")]
        public bool? IsCompound { get; set; }
    }

    // Registry (maps your fixed entity names to CLR types)
    public static class WaveEntityRegistry
    {
        public static readonly Dictionary<string, Type> Types = new(StringComparer.OrdinalIgnoreCase)
        {
            // Businesses
            ["businesses"] = typeof(Business),

            // Customers
            ["customers"] = typeof(Customer),

            // Products
            ["products"] = typeof(Product),

            // Invoices
            ["invoices"] = typeof(Invoice),
            ["invoice_items"] = typeof(InvoiceItem),

            // Payments
            ["payments"] = typeof(Payment),
            ["payment_items"] = typeof(PaymentItem),

            // Bills
            ["bills"] = typeof(Bill),
            ["bill_items"] = typeof(BillItem),

            // Accounts
            ["accounts"] = typeof(Account),

            // Transactions
            ["transactions"] = typeof(Transaction),

            // Taxes
            ["taxes"] = typeof(Tax),
            ["sales_taxes"] = typeof(SalesTax),
            ["purchase_taxes"] = typeof(PurchaseTax),

            // Users
            ["users"] = typeof(User),

            // Attachments
            ["attachments"] = typeof(Attachment),

            // Reference data
            ["currencies"] = typeof(Currency),
            ["countries"] = typeof(Country),
            ["business_types"] = typeof(BusinessType)
        };

        public static IReadOnlyList<string> Names => new List<string>(Types.Keys);
    }
}