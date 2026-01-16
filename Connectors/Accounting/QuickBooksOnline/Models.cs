using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TheTechIdea.Beep.Connectors.QuickBooksOnline
{
    // Base class for QuickBooks entities
    public class QuickBooksBaseEntity
    {
        [JsonPropertyName("Id")]
        public string? Id { get; set; }

        [JsonPropertyName("SyncToken")]
        public string? SyncToken { get; set; }

        [JsonPropertyName("MetaData")]
        public MetaData? MetaData { get; set; }

        [JsonPropertyName("domain")]
        public string? Domain { get; set; }

        [JsonPropertyName("sparse")]
        public bool? Sparse { get; set; }
    }

    public class MetaData
    {
        [JsonPropertyName("CreateTime")]
        public DateTime? CreateTime { get; set; }

        [JsonPropertyName("LastUpdatedTime")]
        public DateTime? LastUpdatedTime { get; set; }
    }

    // Customer entity
    public class Customer : QuickBooksBaseEntity
    {
        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        [JsonPropertyName("CompanyName")]
        public string? CompanyName { get; set; }

        [JsonPropertyName("GivenName")]
        public string? GivenName { get; set; }

        [JsonPropertyName("FamilyName")]
        public string? FamilyName { get; set; }

        [JsonPropertyName("Caption")]
        public string? Caption { get; set; }

        [JsonPropertyName("PrintOnCheckName")]
        public string? PrintOnCheckName { get; set; }

        [JsonPropertyName("Active")]
        public bool? Active { get; set; }

        [JsonPropertyName("PrimaryPhone")]
        public TelephoneNumber? PrimaryPhone { get; set; }

        [JsonPropertyName("AlternatePhone")]
        public TelephoneNumber? AlternatePhone { get; set; }

        [JsonPropertyName("Mobile")]
        public TelephoneNumber? Mobile { get; set; }

        [JsonPropertyName("Fax")]
        public TelephoneNumber? Fax { get; set; }

        [JsonPropertyName("PrimaryEmailAddr")]
        public EmailAddress? PrimaryEmailAddr { get; set; }

        [JsonPropertyName("WebAddr")]
        public WebSiteAddress? WebAddr { get; set; }

        [JsonPropertyName("BillAddr")]
        public PhysicalAddress? BillAddr { get; set; }

        [JsonPropertyName("ShipAddr")]
        public PhysicalAddress? ShipAddr { get; set; }

        [JsonPropertyName("Job")]
        public bool? Job { get; set; }

        [JsonPropertyName("BillWithParent")]
        public bool? BillWithParent { get; set; }

        [JsonPropertyName("ParentRef")]
        public ReferenceType? ParentRef { get; set; }

        [JsonPropertyName("Level")]
        public int? Level { get; set; }

        [JsonPropertyName("SalesTermRef")]
        public ReferenceType? SalesTermRef { get; set; }

        [JsonPropertyName("TaxExemptionRef")]
        public ReferenceType? TaxExemptionRef { get; set; }

        [JsonPropertyName("Taxable")]
        public bool? Taxable { get; set; }

        [JsonPropertyName("PercentBased")]
        public bool? PercentBased { get; set; }

        [JsonPropertyName("Type")]
        public string? Type { get; set; }

        [JsonPropertyName("TermRef")]
        public ReferenceType? TermRef { get; set; }

        [JsonPropertyName("Source")]
        public string? Source { get; set; }

        [JsonPropertyName("CurrencyRef")]
        public ReferenceType? CurrencyRef { get; set; }

        [JsonPropertyName("Balance")]
        public decimal? Balance { get; set; }

        [JsonPropertyName("BalanceWithJobs")]
        public decimal? BalanceWithJobs { get; set; }

        [JsonPropertyName("PreferredDeliveryMethod")]
        public string? PreferredDeliveryMethod { get; set; }

        [JsonPropertyName("Notes")]
        public string? Notes { get; set; }
    }

    // Invoice entity
    public class Invoice : QuickBooksBaseEntity
    {
        [JsonPropertyName("DocNumber")]
        public string? DocNumber { get; set; }

        [JsonPropertyName("TxnDate")]
        public DateTime? TxnDate { get; set; }

        [JsonPropertyName("DueDate")]
        public DateTime? DueDate { get; set; }

        [JsonPropertyName("TotalAmt")]
        public decimal? TotalAmt { get; set; }

        [JsonPropertyName("Balance")]
        public decimal? Balance { get; set; }

        [JsonPropertyName("CustomerRef")]
        public ReferenceType? CustomerRef { get; set; }

        [JsonPropertyName("CustomerMemo")]
        public MemoRef? CustomerMemo { get; set; }

        [JsonPropertyName("BillAddr")]
        public PhysicalAddress? BillAddr { get; set; }

        [JsonPropertyName("ShipAddr")]
        public PhysicalAddress? ShipAddr { get; set; }

        [JsonPropertyName("ClassRef")]
        public ReferenceType? ClassRef { get; set; }

        [JsonPropertyName("SalesTermRef")]
        public ReferenceType? SalesTermRef { get; set; }

        [JsonPropertyName("DepartmentRef")]
        public ReferenceType? DepartmentRef { get; set; }

        [JsonPropertyName("BillEmail")]
        public EmailAddress? BillEmail { get; set; }

        [JsonPropertyName("ReplyEmail")]
        public string? ReplyEmail { get; set; }

        [JsonPropertyName("Line")]
        public List<Line>? Line { get; set; }

        [JsonPropertyName("TxnTaxDetail")]
        public TxnTaxDetail? TxnTaxDetail { get; set; }

        [JsonPropertyName("CustomField")]
        public List<CustomField>? CustomField { get; set; }

        [JsonPropertyName("CurrencyRef")]
        public ReferenceType? CurrencyRef { get; set; }

        [JsonPropertyName("ExchangeRate")]
        public decimal? ExchangeRate { get; set; }

        [JsonPropertyName("GlobalTaxCalculation")]
        public string? GlobalTaxCalculation { get; set; }

        [JsonPropertyName("HomeBalance")]
        public decimal? HomeBalance { get; set; }

        [JsonPropertyName("RecurDataRef")]
        public ReferenceType? RecurDataRef { get; set; }

        [JsonPropertyName("TaxExemptionRef")]
        public ReferenceType? TaxExemptionRef { get; set; }

        [JsonPropertyName("PrintStatus")]
        public string? PrintStatus { get; set; }

        [JsonPropertyName("EmailStatus")]
        public string? EmailStatus { get; set; }

        [JsonPropertyName("Manual")]
        public string? Manual { get; set; }

        [JsonPropertyName("AllowIPNPayment")]
        public bool? AllowIPNPayment { get; set; }

        [JsonPropertyName("AllowOnlinePayment")]
        public bool? AllowOnlinePayment { get; set; }

        [JsonPropertyName("AllowOnlineCreditCardPayment")]
        public bool? AllowOnlineCreditCardPayment { get; set; }

        [JsonPropertyName("AllowOnlineACHPayment")]
        public bool? AllowOnlineACHPayment { get; set; }
    }

    // Bill entity
    public class Bill : QuickBooksBaseEntity
    {
        [JsonPropertyName("DocNumber")]
        public string? DocNumber { get; set; }

        [JsonPropertyName("TxnDate")]
        public DateTime? TxnDate { get; set; }

        [JsonPropertyName("DueDate")]
        public DateTime? DueDate { get; set; }

        [JsonPropertyName("TotalAmt")]
        public decimal? TotalAmt { get; set; }

        [JsonPropertyName("Balance")]
        public decimal? Balance { get; set; }

        [JsonPropertyName("VendorRef")]
        public ReferenceType? VendorRef { get; set; }

        [JsonPropertyName("APAccountRef")]
        public ReferenceType? APAccountRef { get; set; }

        [JsonPropertyName("VendorAddr")]
        public PhysicalAddress? VendorAddr { get; set; }

        [JsonPropertyName("Line")]
        public List<Line>? Line { get; set; }

        [JsonPropertyName("TxnTaxDetail")]
        public TxnTaxDetail? TxnTaxDetail { get; set; }

        [JsonPropertyName("CustomField")]
        public List<CustomField>? CustomField { get; set; }

        [JsonPropertyName("CurrencyRef")]
        public ReferenceType? CurrencyRef { get; set; }

        [JsonPropertyName("ExchangeRate")]
        public decimal? ExchangeRate { get; set; }

        [JsonPropertyName("GlobalTaxCalculation")]
        public string? GlobalTaxCalculation { get; set; }

        [JsonPropertyName("HomeBalance")]
        public decimal? HomeBalance { get; set; }

        [JsonPropertyName("RecurDataRef")]
        public ReferenceType? RecurDataRef { get; set; }

        [JsonPropertyName("TaxExemptionRef")]
        public ReferenceType? TaxExemptionRef { get; set; }

        [JsonPropertyName("PrintStatus")]
        public string? PrintStatus { get; set; }

        [JsonPropertyName("EmailStatus")]
        public string? EmailStatus { get; set; }

        [JsonPropertyName("Manual")]
        public string? Manual { get; set; }
    }

    // Account entity
    public class Account : QuickBooksBaseEntity
    {
        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        [JsonPropertyName("SubAccount")]
        public bool? SubAccount { get; set; }

        [JsonPropertyName("ParentRef")]
        public ReferenceType? ParentRef { get; set; }

        [JsonPropertyName("Description")]
        public string? Description { get; set; }

        [JsonPropertyName("FullyQualifiedName")]
        public string? FullyQualifiedName { get; set; }

        [JsonPropertyName("Active")]
        public bool? Active { get; set; }

        [JsonPropertyName("Classification")]
        public string? Classification { get; set; }

        [JsonPropertyName("AccountType")]
        public string? AccountType { get; set; }

        [JsonPropertyName("AccountSubType")]
        public string? AccountSubType { get; set; }

        [JsonPropertyName("AcctNum")]
        public string? AcctNum { get; set; }

        [JsonPropertyName("CurrentBalance")]
        public decimal? CurrentBalance { get; set; }

        [JsonPropertyName("CurrentBalanceWithSubAccounts")]
        public decimal? CurrentBalanceWithSubAccounts { get; set; }

        [JsonPropertyName("CurrencyRef")]
        public ReferenceType? CurrencyRef { get; set; }

        [JsonPropertyName("TaxCodeRef")]
        public ReferenceType? TaxCodeRef { get; set; }

        [JsonPropertyName("Source")]
        public string? Source { get; set; }

        [JsonPropertyName("OpeningBalanceDate")]
        public DateTime? OpeningBalanceDate { get; set; }

        [JsonPropertyName("OpeningBalance")]
        public decimal? OpeningBalance { get; set; }
    }

    // Item entity
    public class Item : QuickBooksBaseEntity
    {
        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        [JsonPropertyName("Description")]
        public string? Description { get; set; }

        [JsonPropertyName("Active")]
        public bool? Active { get; set; }

        [JsonPropertyName("FullyQualifiedName")]
        public string? FullyQualifiedName { get; set; }

        [JsonPropertyName("Taxable")]
        public bool? Taxable { get; set; }

        [JsonPropertyName("SalesTaxIncluded")]
        public bool? SalesTaxIncluded { get; set; }

        [JsonPropertyName("PercentBased")]
        public bool? PercentBased { get; set; }

        [JsonPropertyName("Type")]
        public string? Type { get; set; }

        [JsonPropertyName("IncomeAccountRef")]
        public ReferenceType? IncomeAccountRef { get; set; }

        [JsonPropertyName("PurchaseDesc")]
        public string? PurchaseDesc { get; set; }

        [JsonPropertyName("PurchaseTaxIncluded")]
        public bool? PurchaseTaxIncluded { get; set; }

        [JsonPropertyName("PurchaseCost")]
        public decimal? PurchaseCost { get; set; }

        [JsonPropertyName("ExpenseAccountRef")]
        public ReferenceType? ExpenseAccountRef { get; set; }

        [JsonPropertyName("COGSAccountRef")]
        public ReferenceType? COGSAccountRef { get; set; }

        [JsonPropertyName("AssetAccountRef")]
        public ReferenceType? AssetAccountRef { get; set; }

        [JsonPropertyName("PrefVendorRef")]
        public ReferenceType? PrefVendorRef { get; set; }

        [JsonPropertyName("AvgCost")]
        public decimal? AvgCost { get; set; }

        [JsonPropertyName("TrackQtyOnHand")]
        public bool? TrackQtyOnHand { get; set; }

        [JsonPropertyName("QtyOnHand")]
        public decimal? QtyOnHand { get; set; }

        [JsonPropertyName("SalesTaxCodeRef")]
        public ReferenceType? SalesTaxCodeRef { get; set; }

        [JsonPropertyName("PurchaseTaxCodeRef")]
        public ReferenceType? PurchaseTaxCodeRef { get; set; }

        [JsonPropertyName("ClassRef")]
        public ReferenceType? ClassRef { get; set; }

        [JsonPropertyName("Source")]
        public string? Source { get; set; }

        [JsonPropertyName("PurchaseTaxRateRef")]
        public ReferenceType? PurchaseTaxRateRef { get; set; }

        [JsonPropertyName("SalesTaxRateRef")]
        public ReferenceType? SalesTaxRateRef { get; set; }

        [JsonPropertyName("UnitPrice")]
        public decimal? UnitPrice { get; set; }

        [JsonPropertyName("RatePercent")]
        public decimal? RatePercent { get; set; }

        [JsonPropertyName("BaseRef")]
        public ReferenceType? BaseRef { get; set; }

        [JsonPropertyName("SKU")]
        public string? SKU { get; set; }

        [JsonPropertyName("ManPartNum")]
        public string? ManPartNum { get; set; }
    }

    // Employee entity
    public class Employee : QuickBooksBaseEntity
    {
        [JsonPropertyName("GivenName")]
        public string? GivenName { get; set; }

        [JsonPropertyName("FamilyName")]
        public string? FamilyName { get; set; }

        [JsonPropertyName("Caption")]
        public string? Caption { get; set; }

        [JsonPropertyName("PrintOnCheckName")]
        public string? PrintOnCheckName { get; set; }

        [JsonPropertyName("Active")]
        public bool? Active { get; set; }

        [JsonPropertyName("PrimaryPhone")]
        public TelephoneNumber? PrimaryPhone { get; set; }

        [JsonPropertyName("Mobile")]
        public TelephoneNumber? Mobile { get; set; }

        [JsonPropertyName("PrimaryEmailAddr")]
        public EmailAddress? PrimaryEmailAddr { get; set; }

        [JsonPropertyName("Address")]
        public PhysicalAddress? Address { get; set; }

        [JsonPropertyName("EmployeeNumber")]
        public string? EmployeeNumber { get; set; }

        [JsonPropertyName("SSN")]
        public string? SSN { get; set; }

        [JsonPropertyName("HiredDate")]
        public DateTime? HiredDate { get; set; }

        [JsonPropertyName("ReleasedDate")]
        public DateTime? ReleasedDate { get; set; }

        [JsonPropertyName("BirthDate")]
        public DateTime? BirthDate { get; set; }

        [JsonPropertyName("Gender")]
        public string? Gender { get; set; }

        [JsonPropertyName("BillableTime")]
        public bool? BillableTime { get; set; }
    }

    // Vendor entity
    public class Vendor : QuickBooksBaseEntity
    {
        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        [JsonPropertyName("CompanyName")]
        public string? CompanyName { get; set; }

        [JsonPropertyName("GivenName")]
        public string? GivenName { get; set; }

        [JsonPropertyName("FamilyName")]
        public string? FamilyName { get; set; }

        [JsonPropertyName("Caption")]
        public string? Caption { get; set; }

        [JsonPropertyName("PrintOnCheckName")]
        public string? PrintOnCheckName { get; set; }

        [JsonPropertyName("Active")]
        public bool? Active { get; set; }

        [JsonPropertyName("PrimaryPhone")]
        public TelephoneNumber? PrimaryPhone { get; set; }

        [JsonPropertyName("AlternatePhone")]
        public TelephoneNumber? AlternatePhone { get; set; }

        [JsonPropertyName("Mobile")]
        public TelephoneNumber? Mobile { get; set; }

        [JsonPropertyName("Fax")]
        public TelephoneNumber? Fax { get; set; }

        [JsonPropertyName("PrimaryEmailAddr")]
        public EmailAddress? PrimaryEmailAddr { get; set; }

        [JsonPropertyName("WebAddr")]
        public WebSiteAddress? WebAddr { get; set; }

        [JsonPropertyName("BillAddr")]
        public PhysicalAddress? BillAddr { get; set; }

        [JsonPropertyName("TermRef")]
        public ReferenceType? TermRef { get; set; }

        [JsonPropertyName("Source")]
        public string? Source { get; set; }

        [JsonPropertyName("CurrencyRef")]
        public ReferenceType? CurrencyRef { get; set; }

        [JsonPropertyName("HasTPAR")]
        public bool? HasTPAR { get; set; }

        [JsonPropertyName("TaxReportingBasis")]
        public string? TaxReportingBasis { get; set; }

        [JsonPropertyName("BusinessNumber")]
        public string? BusinessNumber { get; set; }

        [JsonPropertyName("Vendor1099")]
        public bool? Vendor1099 { get; set; }

        [JsonPropertyName("Balance")]
        public decimal? Balance { get; set; }
    }

    // Common supporting classes
    public class ReferenceType
    {
        [JsonPropertyName("value")]
        public string? Value { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    public class TelephoneNumber
    {
        [JsonPropertyName("FreeFormNumber")]
        public string? FreeFormNumber { get; set; }
    }

    public class EmailAddress
    {
        [JsonPropertyName("Address")]
        public string? Address { get; set; }
    }

    public class WebSiteAddress
    {
        [JsonPropertyName("URI")]
        public string? URI { get; set; }
    }

    public class PhysicalAddress
    {
        [JsonPropertyName("Id")]
        public string? Id { get; set; }

        [JsonPropertyName("Line1")]
        public string? Line1 { get; set; }

        [JsonPropertyName("Line2")]
        public string? Line2 { get; set; }

        [JsonPropertyName("Line3")]
        public string? Line3 { get; set; }

        [JsonPropertyName("Line4")]
        public string? Line4 { get; set; }

        [JsonPropertyName("Line5")]
        public string? Line5 { get; set; }

        [JsonPropertyName("City")]
        public string? City { get; set; }

        [JsonPropertyName("Country")]
        public string? Country { get; set; }

        [JsonPropertyName("CountrySubDivisionCode")]
        public string? CountrySubDivisionCode { get; set; }

        [JsonPropertyName("PostalCode")]
        public string? PostalCode { get; set; }
    }

    public class MemoRef
    {
        [JsonPropertyName("value")]
        public string? Value { get; set; }
    }

    public class Line
    {
        [JsonPropertyName("Id")]
        public string? Id { get; set; }

        [JsonPropertyName("LineNum")]
        public int? LineNum { get; set; }

        [JsonPropertyName("Description")]
        public string? Description { get; set; }

        [JsonPropertyName("Amount")]
        public decimal? Amount { get; set; }

        [JsonPropertyName("DetailType")]
        public string? DetailType { get; set; }

        [JsonPropertyName("SalesItemLineDetail")]
        public SalesItemLineDetail? SalesItemLineDetail { get; set; }

        [JsonPropertyName("ItemBasedExpenseLineDetail")]
        public ItemBasedExpenseLineDetail? ItemBasedExpenseLineDetail { get; set; }

        [JsonPropertyName("AccountBasedExpenseLineDetail")]
        public AccountBasedExpenseLineDetail? AccountBasedExpenseLineDetail { get; set; }
    }

    public class SalesItemLineDetail
    {
        [JsonPropertyName("ItemRef")]
        public ReferenceType? ItemRef { get; set; }

        [JsonPropertyName("UnitPrice")]
        public decimal? UnitPrice { get; set; }

        [JsonPropertyName("Qty")]
        public decimal? Qty { get; set; }

        [JsonPropertyName("TaxCodeRef")]
        public ReferenceType? TaxCodeRef { get; set; }
    }

    public class ItemBasedExpenseLineDetail
    {
        [JsonPropertyName("ItemRef")]
        public ReferenceType? ItemRef { get; set; }

        [JsonPropertyName("UnitPrice")]
        public decimal? UnitPrice { get; set; }

        [JsonPropertyName("Qty")]
        public decimal? Qty { get; set; }

        [JsonPropertyName("TaxCodeRef")]
        public ReferenceType? TaxCodeRef { get; set; }
    }

    public class AccountBasedExpenseLineDetail
    {
        [JsonPropertyName("AccountRef")]
        public ReferenceType? AccountRef { get; set; }

        [JsonPropertyName("TaxCodeRef")]
        public ReferenceType? TaxCodeRef { get; set; }
    }

    public class TxnTaxDetail
    {
        [JsonPropertyName("TxnTaxCodeRef")]
        public ReferenceType? TxnTaxCodeRef { get; set; }

        [JsonPropertyName("TotalTax")]
        public decimal? TotalTax { get; set; }

        [JsonPropertyName("TaxLine")]
        public List<TaxLine>? TaxLine { get; set; }
    }

    public class TaxLine
    {
        [JsonPropertyName("Amount")]
        public decimal? Amount { get; set; }

        [JsonPropertyName("DetailType")]
        public string? DetailType { get; set; }

        [JsonPropertyName("TaxLineDetail")]
        public TaxLineDetail? TaxLineDetail { get; set; }
    }

    public class TaxLineDetail
    {
        [JsonPropertyName("TaxRateRef")]
        public ReferenceType? TaxRateRef { get; set; }

        [JsonPropertyName("PercentBased")]
        public bool? PercentBased { get; set; }

        [JsonPropertyName("TaxPercent")]
        public decimal? TaxPercent { get; set; }

        [JsonPropertyName("NetAmountTaxable")]
        public decimal? NetAmountTaxable { get; set; }
    }

    public class CustomField
    {
        [JsonPropertyName("DefinitionId")]
        public string? DefinitionId { get; set; }

        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        [JsonPropertyName("Type")]
        public string? Type { get; set; }

        [JsonPropertyName("StringValue")]
        public string? StringValue { get; set; }
    }

    // Company Info entity
    public class CompanyInfo : QuickBooksBaseEntity
    {
        [JsonPropertyName("CompanyName")]
        public string? CompanyName { get; set; }

        [JsonPropertyName("LegalName")]
        public string? LegalName { get; set; }

        [JsonPropertyName("CompanyAddr")]
        public PhysicalAddress? CompanyAddr { get; set; }

        [JsonPropertyName("CustomerCommunicationAddr")]
        public PhysicalAddress? CustomerCommunicationAddr { get; set; }

        [JsonPropertyName("LegalAddr")]
        public PhysicalAddress? LegalAddr { get; set; }

        [JsonPropertyName("CompanyStartDate")]
        public DateTime? CompanyStartDate { get; set; }

        [JsonPropertyName("FiscalYearStartMonth")]
        public string? FiscalYearStartMonth { get; set; }

        [JsonPropertyName("TaxYearStartMonth")]
        public string? TaxYearStartMonth { get; set; }

        [JsonPropertyName("Country")]
        public string? Country { get; set; }

        [JsonPropertyName("Email")]
        public EmailAddress? Email { get; set; }

        [JsonPropertyName("WebAddr")]
        public WebSiteAddress? WebAddr { get; set; }

        [JsonPropertyName("PrimaryPhone")]
        public TelephoneNumber? PrimaryPhone { get; set; }

        [JsonPropertyName("LegalEntityType")]
        public string? LegalEntityType { get; set; }

        [JsonPropertyName("SubscriptionStatus")]
        public string? SubscriptionStatus { get; set; }
    }

    // Tax Code entity
    public class TaxCode : QuickBooksBaseEntity
    {
        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        [JsonPropertyName("Description")]
        public string? Description { get; set; }

        [JsonPropertyName("Active")]
        public bool? Active { get; set; }

        [JsonPropertyName("Taxable")]
        public bool? Taxable { get; set; }

        [JsonPropertyName("TaxGroup")]
        public bool? TaxGroup { get; set; }

        [JsonPropertyName("PercentBased")]
        public bool? PercentBased { get; set; }

        [JsonPropertyName("RateValue")]
        public decimal? RateValue { get; set; }

        [JsonPropertyName("AgencyRef")]
        public ReferenceType? AgencyRef { get; set; }

        [JsonPropertyName("TaxRateRef")]
        public ReferenceType? TaxRateRef { get; set; }
    }
}