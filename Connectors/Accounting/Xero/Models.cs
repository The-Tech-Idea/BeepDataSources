using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TheTechIdea.Beep.Connectors.Xero
{
    // Base class for Xero entities
    public class XeroBaseEntity
    {
        [JsonPropertyName("ContactID")]
        public string? ContactID { get; set; }

        [JsonPropertyName("ContactStatus")]
        public string? ContactStatus { get; set; }

        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        [JsonPropertyName("UpdatedDateUTC")]
        public DateTime? UpdatedDateUTC { get; set; }
    }

    // Contact entity
    public class Contact : XeroBaseEntity
    {
        [JsonPropertyName("ContactNumber")]
        public string? ContactNumber { get; set; }

        [JsonPropertyName("AccountNumber")]
        public string? AccountNumber { get; set; }

        [JsonPropertyName("ContactStatus")]
        public string? Status { get; set; }

        [JsonPropertyName("FirstName")]
        public string? FirstName { get; set; }

        [JsonPropertyName("LastName")]
        public string? LastName { get; set; }

        [JsonPropertyName("EmailAddress")]
        public string? EmailAddress { get; set; }

        [JsonPropertyName("SkypeUserName")]
        public string? SkypeUserName { get; set; }

        [JsonPropertyName("ContactPersons")]
        public List<ContactPerson>? ContactPersons { get; set; }

        [JsonPropertyName("BankAccountDetails")]
        public string? BankAccountDetails { get; set; }

        [JsonPropertyName("Addresses")]
        public List<Address>? Addresses { get; set; }

        [JsonPropertyName("Phones")]
        public List<Phone>? Phones { get; set; }

        [JsonPropertyName("IsSupplier")]
        public bool? IsSupplier { get; set; }

        [JsonPropertyName("IsCustomer")]
        public bool? IsCustomer { get; set; }

        [JsonPropertyName("DefaultCurrency")]
        public string? DefaultCurrency { get; set; }

        [JsonPropertyName("XeroNetworkKey")]
        public string? XeroNetworkKey { get; set; }

        [JsonPropertyName("SalesDefaultAccountCode")]
        public string? SalesDefaultAccountCode { get; set; }

        [JsonPropertyName("PurchasesDefaultAccountCode")]
        public string? PurchasesDefaultAccountCode { get; set; }

        [JsonPropertyName("SalesTrackingCategories")]
        public List<TrackingCategory>? SalesTrackingCategories { get; set; }

        [JsonPropertyName("PurchasesTrackingCategories")]
        public List<TrackingCategory>? PurchasesTrackingCategories { get; set; }

        [JsonPropertyName("TrackingCategoryName")]
        public string? TrackingCategoryName { get; set; }

        [JsonPropertyName("TrackingCategoryOption")]
        public string? TrackingCategoryOption { get; set; }

        [JsonPropertyName("PaymentTerms")]
        public PaymentTerm? PaymentTerms { get; set; }

        [JsonPropertyName("ContactGroups")]
        public List<ContactGroup>? ContactGroups { get; set; }

        [JsonPropertyName("Website")]
        public string? Website { get; set; }

        [JsonPropertyName("BrandingTheme")]
        public BrandingTheme? BrandingTheme { get; set; }

        [JsonPropertyName("BatchPayments")]
        public BatchPayment? BatchPayments { get; set; }

        [JsonPropertyName("Discount")]
        public decimal? Discount { get; set; }

        [JsonPropertyName("Balances")]
        public ContactBalances? Balances { get; set; }

        [JsonPropertyName("HasAttachments")]
        public bool? HasAttachments { get; set; }
    }

    // Invoice entity
    public class Invoice
    {
        [JsonPropertyName("InvoiceID")]
        public string? InvoiceID { get; set; }

        [JsonPropertyName("InvoiceNumber")]
        public string? InvoiceNumber { get; set; }

        [JsonPropertyName("Reference")]
        public string? Reference { get; set; }

        [JsonPropertyName("Type")]
        public string? Type { get; set; }

        [JsonPropertyName("Contact")]
        public Contact? Contact { get; set; }

        [JsonPropertyName("Date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("DueDate")]
        public DateTime? DueDate { get; set; }

        [JsonPropertyName("Status")]
        public string? Status { get; set; }

        [JsonPropertyName("LineAmountTypes")]
        public string? LineAmountTypes { get; set; }

        [JsonPropertyName("LineItems")]
        public List<LineItem>? LineItems { get; set; }

        [JsonPropertyName("SubTotal")]
        public decimal? SubTotal { get; set; }

        [JsonPropertyName("TotalTax")]
        public decimal? TotalTax { get; set; }

        [JsonPropertyName("Total")]
        public decimal? Total { get; set; }

        [JsonPropertyName("UpdatedDateUTC")]
        public DateTime? UpdatedDateUTC { get; set; }

        [JsonPropertyName("CurrencyCode")]
        public string? CurrencyCode { get; set; }

        [JsonPropertyName("CurrencyRate")]
        public decimal? CurrencyRate { get; set; }

        [JsonPropertyName("InvoiceAddresses")]
        public List<InvoiceAddress>? InvoiceAddresses { get; set; }

        [JsonPropertyName("Payments")]
        public List<Payment>? Payments { get; set; }

        [JsonPropertyName("CreditNotes")]
        public List<CreditNote>? CreditNotes { get; set; }

        [JsonPropertyName("Prepayments")]
        public List<Prepayment>? Prepayments { get; set; }

        [JsonPropertyName("Overpayments")]
        public List<Overpayment>? Overpayments { get; set; }

        [JsonPropertyName("AmountDue")]
        public decimal? AmountDue { get; set; }

        [JsonPropertyName("AmountPaid")]
        public decimal? AmountPaid { get; set; }

        [JsonPropertyName("AmountCredited")]
        public decimal? AmountCredited { get; set; }

        [JsonPropertyName("SentToContact")]
        public bool? SentToContact { get; set; }

        [JsonPropertyName("ExpectedPaymentDate")]
        public DateTime? ExpectedPaymentDate { get; set; }

        [JsonPropertyName("PlannedPaymentDate")]
        public DateTime? PlannedPaymentDate { get; set; }

        [JsonPropertyName("HasAttachments")]
        public bool? HasAttachments { get; set; }

        [JsonPropertyName("BrandingThemeID")]
        public string? BrandingThemeID { get; set; }

        [JsonPropertyName("Url")]
        public string? Url { get; set; }

        [JsonPropertyName("RepeatingInvoiceID")]
        public string? RepeatingInvoiceID { get; set; }
    }

    // Account entity
    public class Account
    {
        [JsonPropertyName("AccountID")]
        public string? AccountID { get; set; }

        [JsonPropertyName("Code")]
        public string? Code { get; set; }

        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        [JsonPropertyName("Type")]
        public string? Type { get; set; }

        [JsonPropertyName("BankAccountNumber")]
        public string? BankAccountNumber { get; set; }

        [JsonPropertyName("Status")]
        public string? Status { get; set; }

        [JsonPropertyName("Description")]
        public string? Description { get; set; }

        [JsonPropertyName("BankAccountType")]
        public string? BankAccountType { get; set; }

        [JsonPropertyName("CurrencyCode")]
        public string? CurrencyCode { get; set; }

        [JsonPropertyName("TaxType")]
        public string? TaxType { get; set; }

        [JsonPropertyName("EnablePaymentsToAccount")]
        public bool? EnablePaymentsToAccount { get; set; }

        [JsonPropertyName("ShowInExpenseClaims")]
        public bool? ShowInExpenseClaims { get; set; }

        [JsonPropertyName("Class")]
        public string? Class { get; set; }

        [JsonPropertyName("SystemAccount")]
        public string? SystemAccount { get; set; }

        [JsonPropertyName("ReportingCode")]
        public string? ReportingCode { get; set; }

        [JsonPropertyName("ReportingCodeName")]
        public string? ReportingCodeName { get; set; }

        [JsonPropertyName("HasAttachments")]
        public bool? HasAttachments { get; set; }

        [JsonPropertyName("UpdatedDateUTC")]
        public DateTime? UpdatedDateUTC { get; set; }
    }

    // Item entity
    public class Item
    {
        [JsonPropertyName("ItemID")]
        public string? ItemID { get; set; }

        [JsonPropertyName("Code")]
        public string? Code { get; set; }

        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        [JsonPropertyName("IsSold")]
        public bool? IsSold { get; set; }

        [JsonPropertyName("IsPurchased")]
        public bool? IsPurchased { get; set; }

        [JsonPropertyName("Description")]
        public string? Description { get; set; }

        [JsonPropertyName("PurchaseDescription")]
        public string? PurchaseDescription { get; set; }

        [JsonPropertyName("PurchaseDetails")]
        public PurchaseDetails? PurchaseDetails { get; set; }

        [JsonPropertyName("SalesDetails")]
        public SalesDetails? SalesDetails { get; set; }

        [JsonPropertyName("IsTrackedAsInventory")]
        public bool? IsTrackedAsInventory { get; set; }

        [JsonPropertyName("TotalCostPool")]
        public decimal? TotalCostPool { get; set; }

        [JsonPropertyName("QuantityOnHand")]
        public decimal? QuantityOnHand { get; set; }

        [JsonPropertyName("UpdatedDateUTC")]
        public DateTime? UpdatedDateUTC { get; set; }

        [JsonPropertyName("InventoryAssetAccountCode")]
        public string? InventoryAssetAccountCode { get; set; }
    }

    // Employee entity
    public class Employee
    {
        [JsonPropertyName("EmployeeID")]
        public string? EmployeeID { get; set; }

        [JsonPropertyName("Status")]
        public string? Status { get; set; }

        [JsonPropertyName("FirstName")]
        public string? FirstName { get; set; }

        [JsonPropertyName("LastName")]
        public string? LastName { get; set; }

        [JsonPropertyName("ExternalLink")]
        public ExternalLink? ExternalLink { get; set; }

        [JsonPropertyName("UpdatedDateUTC")]
        public DateTime? UpdatedDateUTC { get; set; }

        [JsonPropertyName("HomeAddress")]
        public HomeAddress? HomeAddress { get; set; }
    }

    // Payment entity
    public class Payment
    {
        [JsonPropertyName("PaymentID")]
        public string? PaymentID { get; set; }

        [JsonPropertyName("Date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("Amount")]
        public decimal? Amount { get; set; }

        [JsonPropertyName("PaymentType")]
        public string? PaymentType { get; set; }

        [JsonPropertyName("Status")]
        public string? Status { get; set; }

        [JsonPropertyName("Reference")]
        public string? Reference { get; set; }

        [JsonPropertyName("IsReconciled")]
        public bool? IsReconciled { get; set; }

        [JsonPropertyName("UpdatedDateUTC")]
        public DateTime? UpdatedDateUTC { get; set; }

        [JsonPropertyName("BatchPaymentID")]
        public string? BatchPaymentID { get; set; }

        [JsonPropertyName("BankAccountNumber")]
        public string? BankAccountNumber { get; set; }

        [JsonPropertyName("Particulars")]
        public string? Particulars { get; set; }

        [JsonPropertyName("Details")]
        public string? Details { get; set; }

        [JsonPropertyName("HasAccount")]
        public bool? HasAccount { get; set; }

        [JsonPropertyName("Contact")]
        public Contact? Contact { get; set; }

        [JsonPropertyName("Invoice")]
        public Invoice? Invoice { get; set; }
    }

    // Bank Transaction entity
    public class BankTransaction
    {
        [JsonPropertyName("BankTransactionID")]
        public string? BankTransactionID { get; set; }

        [JsonPropertyName("Type")]
        public string? Type { get; set; }

        [JsonPropertyName("Reference")]
        public string? Reference { get; set; }

        [JsonPropertyName("IsReconciled")]
        public bool? IsReconciled { get; set; }

        [JsonPropertyName("Date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("Status")]
        public string? Status { get; set; }

        [JsonPropertyName("LineAmountTypes")]
        public string? LineAmountTypes { get; set; }

        [JsonPropertyName("LineItems")]
        public List<LineItem>? LineItems { get; set; }

        [JsonPropertyName("SubTotal")]
        public decimal? SubTotal { get; set; }

        [JsonPropertyName("TotalTax")]
        public decimal? TotalTax { get; set; }

        [JsonPropertyName("Total")]
        public decimal? Total { get; set; }

        [JsonPropertyName("UpdatedDateUTC")]
        public DateTime? UpdatedDateUTC { get; set; }

        [JsonPropertyName("CurrencyCode")]
        public string? CurrencyCode { get; set; }

        [JsonPropertyName("BankAccount")]
        public Account? BankAccount { get; set; }

        [JsonPropertyName("Contact")]
        public Contact? Contact { get; set; }

        [JsonPropertyName("PrepaymentID")]
        public string? PrepaymentID { get; set; }

        [JsonPropertyName("OverpaymentID")]
        public string? OverpaymentID { get; set; }

        [JsonPropertyName("HasAttachments")]
        public bool? HasAttachments { get; set; }

        [JsonPropertyName("Url")]
        public string? Url { get; set; }
    }

    // Supporting classes
    public class ContactPerson
    {
        [JsonPropertyName("FirstName")]
        public string? FirstName { get; set; }

        [JsonPropertyName("LastName")]
        public string? LastName { get; set; }

        [JsonPropertyName("EmailAddress")]
        public string? EmailAddress { get; set; }

        [JsonPropertyName("IncludeInEmails")]
        public bool? IncludeInEmails { get; set; }
    }

    public class Address
    {
        [JsonPropertyName("AddressType")]
        public string? AddressType { get; set; }

        [JsonPropertyName("AddressLine1")]
        public string? AddressLine1 { get; set; }

        [JsonPropertyName("AddressLine2")]
        public string? AddressLine2 { get; set; }

        [JsonPropertyName("City")]
        public string? City { get; set; }

        [JsonPropertyName("Region")]
        public string? Region { get; set; }

        [JsonPropertyName("PostalCode")]
        public string? PostalCode { get; set; }

        [JsonPropertyName("Country")]
        public string? Country { get; set; }

        [JsonPropertyName("AttentionTo")]
        public string? AttentionTo { get; set; }
    }

    public class Phone
    {
        [JsonPropertyName("PhoneType")]
        public string? PhoneType { get; set; }

        [JsonPropertyName("PhoneNumber")]
        public string? PhoneNumber { get; set; }

        [JsonPropertyName("PhoneAreaCode")]
        public string? PhoneAreaCode { get; set; }

        [JsonPropertyName("PhoneCountryCode")]
        public string? PhoneCountryCode { get; set; }
    }

    public class PaymentTerm
    {
        [JsonPropertyName("Bills")]
        public BillTerm? Bills { get; set; }

        [JsonPropertyName("Sales")]
        public SaleTerm? Sales { get; set; }
    }

    public class BillTerm
    {
        [JsonPropertyName("Day")]
        public int? Day { get; set; }

        [JsonPropertyName("Type")]
        public string? Type { get; set; }
    }

    public class SaleTerm
    {
        [JsonPropertyName("Day")]
        public int? Day { get; set; }

        [JsonPropertyName("Type")]
        public string? Type { get; set; }
    }

    public class ContactGroup
    {
        [JsonPropertyName("ContactGroupID")]
        public string? ContactGroupID { get; set; }

        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        [JsonPropertyName("Status")]
        public string? Status { get; set; }
    }

    public class BrandingTheme
    {
        [JsonPropertyName("BrandingThemeID")]
        public string? BrandingThemeID { get; set; }

        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        [JsonPropertyName("SortOrder")]
        public int? SortOrder { get; set; }

        [JsonPropertyName("CreatedDateUTC")]
        public DateTime? CreatedDateUTC { get; set; }
    }

    public class BatchPayment
    {
        [JsonPropertyName("BatchPaymentID")]
        public string? BatchPaymentID { get; set; }

        [JsonPropertyName("Account")]
        public Account? Account { get; set; }

        [JsonPropertyName("Reference")]
        public string? Reference { get; set; }

        [JsonPropertyName("Particulars")]
        public string? Particulars { get; set; }

        [JsonPropertyName("Code")]
        public string? Code { get; set; }

        [JsonPropertyName("Details")]
        public string? Details { get; set; }

        [JsonPropertyName("Narrative")]
        public string? Narrative { get; set; }

        [JsonPropertyName("Status")]
        public string? Status { get; set; }

        [JsonPropertyName("Amount")]
        public decimal? Amount { get; set; }

        [JsonPropertyName("Date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("Type")]
        public string? Type { get; set; }

        [JsonPropertyName("UpdatedDateUTC")]
        public DateTime? UpdatedDateUTC { get; set; }

        [JsonPropertyName("IsReconciled")]
        public bool? IsReconciled { get; set; }
    }

    public class ContactBalances
    {
        [JsonPropertyName("AccountsReceivable")]
        public BalanceDetails? AccountsReceivable { get; set; }

        [JsonPropertyName("AccountsPayable")]
        public BalanceDetails? AccountsPayable { get; set; }
    }

    public class BalanceDetails
    {
        [JsonPropertyName("Outstanding")]
        public decimal? Outstanding { get; set; }

        [JsonPropertyName("Overdue")]
        public decimal? Overdue { get; set; }
    }

    public class LineItem
    {
        [JsonPropertyName("LineItemID")]
        public string? LineItemID { get; set; }

        [JsonPropertyName("Description")]
        public string? Description { get; set; }

        [JsonPropertyName("Quantity")]
        public decimal? Quantity { get; set; }

        [JsonPropertyName("UnitAmount")]
        public decimal? UnitAmount { get; set; }

        [JsonPropertyName("ItemCode")]
        public string? ItemCode { get; set; }

        [JsonPropertyName("AccountCode")]
        public string? AccountCode { get; set; }

        [JsonPropertyName("TaxType")]
        public string? TaxType { get; set; }

        [JsonPropertyName("TaxAmount")]
        public decimal? TaxAmount { get; set; }

        [JsonPropertyName("LineAmount")]
        public decimal? LineAmount { get; set; }

        [JsonPropertyName("Tracking")]
        public List<TrackingCategory>? TrackingCategories { get; set; }

        [JsonPropertyName("DiscountRate")]
        public decimal? DiscountRate { get; set; }
    }

    public class InvoiceAddress
    {
        [JsonPropertyName("AddressType")]
        public string? AddressType { get; set; }

        [JsonPropertyName("AddressLine1")]
        public string? AddressLine1 { get; set; }

        [JsonPropertyName("AddressLine2")]
        public string? AddressLine2 { get; set; }

        [JsonPropertyName("City")]
        public string? City { get; set; }

        [JsonPropertyName("Region")]
        public string? Region { get; set; }

        [JsonPropertyName("PostalCode")]
        public string? PostalCode { get; set; }

        [JsonPropertyName("Country")]
        public string? Country { get; set; }
    }

    public class CreditNote
    {
        [JsonPropertyName("CreditNoteID")]
        public string? CreditNoteID { get; set; }

        [JsonPropertyName("CreditNoteNumber")]
        public string? CreditNoteNumber { get; set; }

        [JsonPropertyName("Type")]
        public string? Type { get; set; }

        [JsonPropertyName("Status")]
        public string? Status { get; set; }

        [JsonPropertyName("Date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("LineAmountTypes")]
        public string? LineAmountTypes { get; set; }

        [JsonPropertyName("SubTotal")]
        public decimal? SubTotal { get; set; }

        [JsonPropertyName("TotalTax")]
        public decimal? TotalTax { get; set; }

        [JsonPropertyName("Total")]
        public decimal? Total { get; set; }

        [JsonPropertyName("UpdatedDateUTC")]
        public DateTime? UpdatedDateUTC { get; set; }

        [JsonPropertyName("CurrencyCode")]
        public string? CurrencyCode { get; set; }

        [JsonPropertyName("FullyPaidOnDate")]
        public DateTime? FullyPaidOnDate { get; set; }

        [JsonPropertyName("CreditNoteID")]
        public string? CreditNoteID2 { get; set; }

        [JsonPropertyName("HasAttachments")]
        public bool? HasAttachments { get; set; }
    }

    public class Prepayment
    {
        [JsonPropertyName("PrepaymentID")]
        public string? PrepaymentID { get; set; }

        [JsonPropertyName("Type")]
        public string? Type { get; set; }

        [JsonPropertyName("Status")]
        public string? Status { get; set; }

        [JsonPropertyName("Date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("LineAmountTypes")]
        public string? LineAmountTypes { get; set; }

        [JsonPropertyName("SubTotal")]
        public decimal? SubTotal { get; set; }

        [JsonPropertyName("TotalTax")]
        public decimal? TotalTax { get; set; }

        [JsonPropertyName("Total")]
        public decimal? Total { get; set; }

        [JsonPropertyName("UpdatedDateUTC")]
        public DateTime? UpdatedDateUTC { get; set; }

        [JsonPropertyName("CurrencyCode")]
        public string? CurrencyCode { get; set; }

        [JsonPropertyName("RemainingCredit")]
        public decimal? RemainingCredit { get; set; }

        [JsonPropertyName("HasAttachments")]
        public bool? HasAttachments { get; set; }
    }

    public class Overpayment
    {
        [JsonPropertyName("OverpaymentID")]
        public string? OverpaymentID { get; set; }

        [JsonPropertyName("Type")]
        public string? Type { get; set; }

        [JsonPropertyName("Date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("Status")]
        public string? Status { get; set; }

        [JsonPropertyName("LineAmountTypes")]
        public string? LineAmountTypes { get; set; }

        [JsonPropertyName("SubTotal")]
        public decimal? SubTotal { get; set; }

        [JsonPropertyName("TotalTax")]
        public decimal? TotalTax { get; set; }

        [JsonPropertyName("Total")]
        public decimal? Total { get; set; }

        [JsonPropertyName("UpdatedDateUTC")]
        public DateTime? UpdatedDateUTC { get; set; }

        [JsonPropertyName("CurrencyCode")]
        public string? CurrencyCode { get; set; }

        [JsonPropertyName("RemainingCredit")]
        public decimal? RemainingCredit { get; set; }

        [JsonPropertyName("HasAttachments")]
        public bool? HasAttachments { get; set; }
    }

    public class PurchaseDetails
    {
        [JsonPropertyName("UnitPrice")]
        public decimal? UnitPrice { get; set; }

        [JsonPropertyName("TaxType")]
        public string? TaxType { get; set; }

        [JsonPropertyName("AccountCode")]
        public string? AccountCode { get; set; }

        [JsonPropertyName("COGSAccountCode")]
        public string? COGSAccountCode { get; set; }
    }

    public class SalesDetails
    {
        [JsonPropertyName("UnitPrice")]
        public decimal? UnitPrice { get; set; }

        [JsonPropertyName("TaxType")]
        public string? TaxType { get; set; }

        [JsonPropertyName("AccountCode")]
        public string? AccountCode { get; set; }
    }

    public class ExternalLink
    {
        [JsonPropertyName("LinkType")]
        public string? LinkType { get; set; }

        [JsonPropertyName("Url")]
        public string? Url { get; set; }
    }

    public class HomeAddress
    {
        [JsonPropertyName("AddressLine1")]
        public string? AddressLine1 { get; set; }

        [JsonPropertyName("AddressLine2")]
        public string? AddressLine2 { get; set; }

        [JsonPropertyName("City")]
        public string? City { get; set; }

        [JsonPropertyName("Region")]
        public string? Region { get; set; }

        [JsonPropertyName("PostalCode")]
        public string? PostalCode { get; set; }

        [JsonPropertyName("Country")]
        public string? Country { get; set; }
    }

    // Organisation entity
    public class Organisation
    {
        [JsonPropertyName("OrganisationID")]
        public string? OrganisationID { get; set; }

        [JsonPropertyName("APIKey")]
        public string? APIKey { get; set; }

        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        [JsonPropertyName("LegalName")]
        public string? LegalName { get; set; }

        [JsonPropertyName("PaysTax")]
        public bool? PaysTax { get; set; }

        [JsonPropertyName("Version")]
        public string? Version { get; set; }

        [JsonPropertyName("OrganisationType")]
        public string? OrganisationType { get; set; }

        [JsonPropertyName("BaseCurrency")]
        public string? BaseCurrency { get; set; }

        [JsonPropertyName("CountryCode")]
        public string? CountryCode { get; set; }

        [JsonPropertyName("IsDemoCompany")]
        public bool? IsDemoCompany { get; set; }

        [JsonPropertyName("OrganisationStatus")]
        public string? OrganisationStatus { get; set; }

        [JsonPropertyName("RegistrationNumber")]
        public string? RegistrationNumber { get; set; }

        [JsonPropertyName("TaxNumber")]
        public string? TaxNumber { get; set; }

        [JsonPropertyName("FinancialYearEndDay")]
        public int? FinancialYearEndDay { get; set; }

        [JsonPropertyName("FinancialYearEndMonth")]
        public int? FinancialYearEndMonth { get; set; }

        [JsonPropertyName("SalesTaxBasis")]
        public string? SalesTaxBasis { get; set; }

        [JsonPropertyName("SalesTaxPeriod")]
        public string? SalesTaxPeriod { get; set; }

        [JsonPropertyName("DefaultSalesTax")]
        public string? DefaultSalesTax { get; set; }

        [JsonPropertyName("DefaultPurchasesTax")]
        public string? DefaultPurchasesTax { get; set; }

        [JsonPropertyName("PeriodLockDate")]
        public DateTime? PeriodLockDate { get; set; }

        [JsonPropertyName("EndOfYearLockDate")]
        public DateTime? EndOfYearLockDate { get; set; }

        [JsonPropertyName("CreatedDateUTC")]
        public DateTime? CreatedDateUTC { get; set; }

        [JsonPropertyName("Timezone")]
        public string? Timezone { get; set; }

        [JsonPropertyName("OrganisationEntityType")]
        public string? OrganisationEntityType { get; set; }

        [JsonPropertyName("ShortCode")]
        public string? ShortCode { get; set; }

        [JsonPropertyName("LineOfBusiness")]
        public string? LineOfBusiness { get; set; }

        [JsonPropertyName("Addresses")]
        public List<Address>? Addresses { get; set; }

        [JsonPropertyName("Phones")]
        public List<Phone>? Phones { get; set; }

        [JsonPropertyName("ExternalLinks")]
        public List<ExternalLink>? ExternalLinks { get; set; }

        [JsonPropertyName("PaymentTerms")]
        public PaymentTerm? PaymentTerms { get; set; }
    }

    // Tax Rate entity
    public class TaxRate
    {
        [JsonPropertyName("TaxType")]
        public string? TaxType { get; set; }

        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        [JsonPropertyName("TaxComponents")]
        public List<TaxComponent>? TaxComponents { get; set; }

        [JsonPropertyName("Status")]
        public string? Status { get; set; }

        [JsonPropertyName("ReportTaxType")]
        public string? ReportTaxType { get; set; }

        [JsonPropertyName("CanApplyToAssets")]
        public bool? CanApplyToAssets { get; set; }

        [JsonPropertyName("CanApplyToEquity")]
        public bool? CanApplyToEquity { get; set; }

        [JsonPropertyName("CanApplyToExpenses")]
        public bool? CanApplyToExpenses { get; set; }

        [JsonPropertyName("CanApplyToLiabilities")]
        public bool? CanApplyToLiabilities { get; set; }

        [JsonPropertyName("CanApplyToRevenue")]
        public bool? CanApplyToRevenue { get; set; }

        [JsonPropertyName("DisplayTaxRate")]
        public decimal? DisplayTaxRate { get; set; }

        [JsonPropertyName("EffectiveRate")]
        public decimal? EffectiveRate { get; set; }
    }

    public class TaxComponent
    {
        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        [JsonPropertyName("Rate")]
        public decimal? Rate { get; set; }

        [JsonPropertyName("IsCompound")]
        public bool? IsCompound { get; set; }
    }

    // Tracking Category entity
    public class TrackingCategory
    {
        [JsonPropertyName("TrackingCategoryID")]
        public string? TrackingCategoryID { get; set; }

        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        [JsonPropertyName("Status")]
        public string? Status { get; set; }

        [JsonPropertyName("Options")]
        public List<TrackingOption>? Options { get; set; }
    }

    public class TrackingOption
    {
        [JsonPropertyName("TrackingOptionID")]
        public string? TrackingOptionID { get; set; }

        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        [JsonPropertyName("Status")]
        public string? Status { get; set; }
    }
}