using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TheTechIdea.Beep.Connectors.SageIntacct
{
    // Base class for Sage Intacct entities
    public class SageIntacctBaseEntity
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("key")]
        public string? Key { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("whenCreated")]
        public DateTime? WhenCreated { get; set; }

        [JsonPropertyName("whenModified")]
        public DateTime? WhenModified { get; set; }

        [JsonPropertyName("createdBy")]
        public string? CreatedBy { get; set; }

        [JsonPropertyName("modifiedBy")]
        public string? ModifiedBy { get; set; }
    }

    // Customer entity
    public class Customer : SageIntacctBaseEntity
    {
        [JsonPropertyName("customerId")]
        public string? CustomerId { get; set; }

        [JsonPropertyName("customerName")]
        public string? CustomerName { get; set; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("companyName")]
        public string? CompanyName { get; set; }

        [JsonPropertyName("printAs")]
        public string? PrintAs { get; set; }

        [JsonPropertyName("taxId")]
        public string? TaxId { get; set; }

        [JsonPropertyName("taxGroup")]
        public TaxGroup? TaxGroup { get; set; }

        [JsonPropertyName("term")]
        public Term? Term { get; set; }

        [JsonPropertyName("creditLimit")]
        public decimal? CreditLimit { get; set; }

        [JsonPropertyName("onHold")]
        public bool? OnHold { get; set; }

        [JsonPropertyName("doNotShip")]
        public bool? DoNotShip { get; set; }

        [JsonPropertyName("doNotBill")]
        public bool? DoNotBill { get; set; }

        [JsonPropertyName("comments")]
        public string? Comments { get; set; }

        [JsonPropertyName("defaultRevenueGLAccount")]
        public GLAccount? DefaultRevenueGLAccount { get; set; }

        [JsonPropertyName("defaultReceivablesGLAccount")]
        public GLAccount? DefaultReceivablesGLAccount { get; set; }

        [JsonPropertyName("shippingMethod")]
        public string? ShippingMethod { get; set; }

        [JsonPropertyName("resaleNumber")]
        public string? ResaleNumber { get; set; }

        [JsonPropertyName("inserviceDate")]
        public DateTime? InserviceDate { get; set; }

        [JsonPropertyName("terminationDate")]
        public DateTime? TerminationDate { get; set; }

        [JsonPropertyName("billToContact")]
        public Contact? BillToContact { get; set; }

        [JsonPropertyName("shipToContact")]
        public Contact? ShipToContact { get; set; }

        [JsonPropertyName("primaryContact")]
        public Contact? PrimaryContact { get; set; }

        [JsonPropertyName("contacts")]
        public List<Contact>? Contacts { get; set; }

        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonPropertyName("parentCustomer")]
        public Customer? ParentCustomer { get; set; }

        [JsonPropertyName("customerType")]
        public CustomerType? CustomerType { get; set; }

        [JsonPropertyName("territory")]
        public Territory? Territory { get; set; }

        [JsonPropertyName("salesRep")]
        public Employee? SalesRep { get; set; }

        [JsonPropertyName("priceList")]
        public string? PriceList { get; set; }

        [JsonPropertyName("defaultPriceList")]
        public string? DefaultPriceList { get; set; }
    }

    // Invoice entity
    public class Invoice : SageIntacctBaseEntity
    {
        [JsonPropertyName("recordNo")]
        public string? RecordNo { get; set; }

        [JsonPropertyName("invoiceNumber")]
        public string? InvoiceNumber { get; set; }

        [JsonPropertyName("customerId")]
        public string? CustomerId { get; set; }

        [JsonPropertyName("customerName")]
        public string? CustomerName { get; set; }

        [JsonPropertyName("documentDate")]
        public DateTime? DocumentDate { get; set; }

        [JsonPropertyName("glPostingDate")]
        public DateTime? GlPostingDate { get; set; }

        [JsonPropertyName("dueDate")]
        public DateTime? DueDate { get; set; }

        [JsonPropertyName("discountDate")]
        public DateTime? DiscountDate { get; set; }

        [JsonPropertyName("term")]
        public Term? Term { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("subtotal")]
        public decimal? Subtotal { get; set; }

        [JsonPropertyName("total")]
        public decimal? Total { get; set; }

        [JsonPropertyName("taxSolution")]
        public TaxSolution? TaxSolution { get; set; }

        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonPropertyName("exchangeRate")]
        public decimal? ExchangeRate { get; set; }

        [JsonPropertyName("baseCurrency")]
        public string? BaseCurrency { get; set; }

        [JsonPropertyName("transactionCurrency")]
        public string? TransactionCurrency { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("moduleKey")]
        public string? ModuleKey { get; set; }

        [JsonPropertyName("billToContact")]
        public Contact? BillToContact { get; set; }

        [JsonPropertyName("shipToContact")]
        public Contact? ShipToContact { get; set; }

        [JsonPropertyName("lines")]
        public List<InvoiceLine>? Lines { get; set; }

        [JsonPropertyName("taxEntries")]
        public List<TaxEntry>? TaxEntries { get; set; }

        [JsonPropertyName("attachments")]
        public List<Attachment>? Attachments { get; set; }

        [JsonPropertyName("customFields")]
        public List<CustomField>? CustomFields { get; set; }

        [JsonPropertyName("printingTemplate")]
        public string? PrintingTemplate { get; set; }

        [JsonPropertyName("history")]
        public List<InvoiceHistory>? History { get; set; }
    }

    // Bill entity
    public class Bill : SageIntacctBaseEntity
    {
        [JsonPropertyName("recordNo")]
        public string? RecordNo { get; set; }

        [JsonPropertyName("billNumber")]
        public string? BillNumber { get; set; }

        [JsonPropertyName("vendorId")]
        public string? VendorId { get; set; }

        [JsonPropertyName("vendorName")]
        public string? VendorName { get; set; }

        [JsonPropertyName("documentDate")]
        public DateTime? DocumentDate { get; set; }

        [JsonPropertyName("glPostingDate")]
        public DateTime? GlPostingDate { get; set; }

        [JsonPropertyName("dueDate")]
        public DateTime? DueDate { get; set; }

        [JsonPropertyName("discountDate")]
        public DateTime? DiscountDate { get; set; }

        [JsonPropertyName("term")]
        public Term? Term { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("subtotal")]
        public decimal? Subtotal { get; set; }

        [JsonPropertyName("total")]
        public decimal? Total { get; set; }

        [JsonPropertyName("taxSolution")]
        public TaxSolution? TaxSolution { get; set; }

        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonPropertyName("exchangeRate")]
        public decimal? ExchangeRate { get; set; }

        [JsonPropertyName("baseCurrency")]
        public string? BaseCurrency { get; set; }

        [JsonPropertyName("transactionCurrency")]
        public string? TransactionCurrency { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("moduleKey")]
        public string? ModuleKey { get; set; }

        [JsonPropertyName("lines")]
        public List<BillLine>? Lines { get; set; }

        [JsonPropertyName("taxEntries")]
        public List<TaxEntry>? TaxEntries { get; set; }

        [JsonPropertyName("attachments")]
        public List<Attachment>? Attachments { get; set; }

        [JsonPropertyName("customFields")]
        public List<CustomField>? CustomFields { get; set; }

        [JsonPropertyName("printingTemplate")]
        public string? PrintingTemplate { get; set; }

        [JsonPropertyName("history")]
        public List<BillHistory>? History { get; set; }
    }

    // Vendor entity
    public class Vendor : SageIntacctBaseEntity
    {
        [JsonPropertyName("vendorId")]
        public string? VendorId { get; set; }

        [JsonPropertyName("vendorName")]
        public string? VendorName { get; set; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("companyName")]
        public string? CompanyName { get; set; }

        [JsonPropertyName("printAs")]
        public string? PrintAs { get; set; }

        [JsonPropertyName("taxId")]
        public string? TaxId { get; set; }

        [JsonPropertyName("taxGroup")]
        public TaxGroup? TaxGroup { get; set; }

        [JsonPropertyName("term")]
        public Term? Term { get; set; }

        [JsonPropertyName("creditLimit")]
        public decimal? CreditLimit { get; set; }

        [JsonPropertyName("onHold")]
        public bool? OnHold { get; set; }

        [JsonPropertyName("doNotPay")]
        public bool? DoNotPay { get; set; }

        [JsonPropertyName("comments")]
        public string? Comments { get; set; }

        [JsonPropertyName("defaultExpenseGLAccount")]
        public GLAccount? DefaultExpenseGLAccount { get; set; }

        [JsonPropertyName("defaultPayablesGLAccount")]
        public GLAccount? DefaultPayablesGLAccount { get; set; }

        [JsonPropertyName("vendorType")]
        public VendorType? VendorType { get; set; }

        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonPropertyName("primaryContact")]
        public Contact? PrimaryContact { get; set; }

        [JsonPropertyName("contacts")]
        public List<Contact>? Contacts { get; set; }

        [JsonPropertyName("parentVendor")]
        public Vendor? ParentVendor { get; set; }

        [JsonPropertyName("mergePaymentRequests")]
        public bool? MergePaymentRequests { get; set; }

        [JsonPropertyName("restrictPaymentMethods")]
        public bool? RestrictPaymentMethods { get; set; }

        [JsonPropertyName("paymentMethod")]
        public string? PaymentMethod { get; set; }
    }

    // Item entity
    public class Item : SageIntacctBaseEntity
    {
        [JsonPropertyName("itemId")]
        public string? ItemId { get; set; }

        [JsonPropertyName("itemName")]
        public string? ItemName { get; set; }

        [JsonPropertyName("itemType")]
        public string? ItemType { get; set; }

        [JsonPropertyName("productLineId")]
        public string? ProductLineId { get; set; }

        [JsonPropertyName("costMethod")]
        public string? CostMethod { get; set; }

        [JsonPropertyName("standardCost")]
        public decimal? StandardCost { get; set; }

        [JsonPropertyName("averageCost")]
        public decimal? AverageCost { get; set; }

        [JsonPropertyName("defaultPrice")]
        public decimal? DefaultPrice { get; set; }

        [JsonPropertyName("salePrice")]
        public decimal? SalePrice { get; set; }

        [JsonPropertyName("taxable")]
        public bool? Taxable { get; set; }

        [JsonPropertyName("taxGroup")]
        public TaxGroup? TaxGroup { get; set; }

        [JsonPropertyName("incomeGLAccount")]
        public GLAccount? IncomeGLAccount { get; set; }

        [JsonPropertyName("expenseGLAccount")]
        public GLAccount? ExpenseGLAccount { get; set; }

        [JsonPropertyName("inventoryGLAccount")]
        public GLAccount? InventoryGLAccount { get; set; }

        [JsonPropertyName("adjustmentGLAccount")]
        public GLAccount? AdjustmentGLAccount { get; set; }

        [JsonPropertyName("cogsGLAccount")]
        public GLAccount? CogsGLAccount { get; set; }

        [JsonPropertyName("assetGLAccount")]
        public GLAccount? AssetGLAccount { get; set; }

        [JsonPropertyName("revenueGLAccount")]
        public GLAccount? RevenueGLAccount { get; set; }

        [JsonPropertyName("uom")]
        public string? Uom { get; set; }

        [JsonPropertyName("active")]
        public bool? Active { get; set; }

        [JsonPropertyName("extendedDescription")]
        public string? ExtendedDescription { get; set; }

        [JsonPropertyName("weight")]
        public decimal? Weight { get; set; }

        [JsonPropertyName("weightUom")]
        public string? WeightUom { get; set; }

        [JsonPropertyName("dimensions")]
        public Dimensions? Dimensions { get; set; }

        [JsonPropertyName("upc")]
        public string? Upc { get; set; }

        [JsonPropertyName("mpn")]
        public string? Mpn { get; set; }

        [JsonPropertyName("isbn")]
        public string? Isbn { get; set; }

        [JsonPropertyName("customFields")]
        public List<CustomField>? CustomFields { get; set; }
    }

    // Account entity
    public class Account : SageIntacctBaseEntity
    {
        [JsonPropertyName("accountNo")]
        public string? AccountNo { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("accountType")]
        public string? AccountType { get; set; }

        [JsonPropertyName("normalBalance")]
        public string? NormalBalance { get; set; }

        [JsonPropertyName("closingType")]
        public string? ClosingType { get; set; }

        [JsonPropertyName("closingAccountNo")]
        public string? ClosingAccountNo { get; set; }

        [JsonPropertyName("requireDepartment")]
        public bool? RequireDepartment { get; set; }

        [JsonPropertyName("requireLocation")]
        public bool? RequireLocation { get; set; }

        [JsonPropertyName("requireProject")]
        public bool? RequireProject { get; set; }

        [JsonPropertyName("requireCustomer")]
        public bool? RequireCustomer { get; set; }

        [JsonPropertyName("requireVendor")]
        public bool? RequireVendor { get; set; }

        [JsonPropertyName("requireEmployee")]
        public bool? RequireEmployee { get; set; }

        [JsonPropertyName("requireItem")]
        public bool? RequireItem { get; set; }

        [JsonPropertyName("requireClass")]
        public bool? RequireClass { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("subCategory")]
        public string? SubCategory { get; set; }

        [JsonPropertyName("taxable")]
        public bool? Taxable { get; set; }

        [JsonPropertyName("taxCode")]
        public string? TaxCode { get; set; }

        [JsonPropertyName("mrp")]
        public bool? Mrp { get; set; }

        [JsonPropertyName("active")]
        public bool? Active { get; set; }

        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonPropertyName("level")]
        public int? Level { get; set; }

        [JsonPropertyName("parentAccountNo")]
        public string? ParentAccountNo { get; set; }

        [JsonPropertyName("rollupAccountNo")]
        public string? RollupAccountNo { get; set; }

        [JsonPropertyName("customFields")]
        public List<CustomField>? CustomFields { get; set; }
    }

    // Employee entity
    public class Employee : SageIntacctBaseEntity
    {
        [JsonPropertyName("employeeId")]
        public string? EmployeeId { get; set; }

        [JsonPropertyName("prefix")]
        public string? Prefix { get; set; }

        [JsonPropertyName("firstName")]
        public string? FirstName { get; set; }

        [JsonPropertyName("middleName")]
        public string? MiddleName { get; set; }

        [JsonPropertyName("lastName")]
        public string? LastName { get; set; }

        [JsonPropertyName("suffix")]
        public string? Suffix { get; set; }

        [JsonPropertyName("preferredName")]
        public string? PreferredName { get; set; }

        [JsonPropertyName("printAs")]
        public string? PrintAs { get; set; }

        [JsonPropertyName("primaryPhoneNo")]
        public string? PrimaryPhoneNo { get; set; }

        [JsonPropertyName("secondaryPhoneNo")]
        public string? SecondaryPhoneNo { get; set; }

        [JsonPropertyName("cellularPhoneNo")]
        public string? CellularPhoneNo { get; set; }

        [JsonPropertyName("pagerNo")]
        public string? PagerNo { get; set; }

        [JsonPropertyName("faxNo")]
        public string? FaxNo { get; set; }

        [JsonPropertyName("primaryEmailAddress")]
        public string? PrimaryEmailAddress { get; set; }

        [JsonPropertyName("secondaryEmailAddress")]
        public string? SecondaryEmailAddress { get; set; }

        [JsonPropertyName("webAddress")]
        public string? WebAddress { get; set; }

        [JsonPropertyName("mailingAddress")]
        public Address? MailingAddress { get; set; }

        [JsonPropertyName("printAsAddress")]
        public Address? PrintAsAddress { get; set; }

        [JsonPropertyName("employeeType")]
        public string? EmployeeType { get; set; }

        [JsonPropertyName("employeeStatus")]
        public string? EmployeeStatus { get; set; }

        [JsonPropertyName("hireDate")]
        public DateTime? HireDate { get; set; }

        [JsonPropertyName("terminationDate")]
        public DateTime? TerminationDate { get; set; }

        [JsonPropertyName("birthDate")]
        public DateTime? BirthDate { get; set; }

        [JsonPropertyName("ssn")]
        public string? Ssn { get; set; }

        [JsonPropertyName("gender")]
        public string? Gender { get; set; }

        [JsonPropertyName("maritalStatus")]
        public string? MaritalStatus { get; set; }

        [JsonPropertyName("department")]
        public Department? Department { get; set; }

        [JsonPropertyName("location")]
        public Location? Location { get; set; }

        [JsonPropertyName("manager")]
        public Employee? Manager { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("startDate")]
        public DateTime? StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public DateTime? EndDate { get; set; }

        [JsonPropertyName("customFields")]
        public List<CustomField>? CustomFields { get; set; }
    }

    // Supporting classes
    public class Contact
    {
        [JsonPropertyName("contactName")]
        public string? ContactName { get; set; }

        [JsonPropertyName("printAs")]
        public string? PrintAs { get; set; }

        [JsonPropertyName("prefix")]
        public string? Prefix { get; set; }

        [JsonPropertyName("firstName")]
        public string? FirstName { get; set; }

        [JsonPropertyName("middleName")]
        public string? MiddleName { get; set; }

        [JsonPropertyName("lastName")]
        public string? LastName { get; set; }

        [JsonPropertyName("suffix")]
        public string? Suffix { get; set; }

        [JsonPropertyName("companyName")]
        public string? CompanyName { get; set; }

        [JsonPropertyName("taxGroup")]
        public TaxGroup? TaxGroup { get; set; }

        [JsonPropertyName("phone1")]
        public string? Phone1 { get; set; }

        [JsonPropertyName("phone2")]
        public string? Phone2 { get; set; }

        [JsonPropertyName("cellular")]
        public string? Cellular { get; set; }

        [JsonPropertyName("pager")]
        public string? Pager { get; set; }

        [JsonPropertyName("fax")]
        public string? Fax { get; set; }

        [JsonPropertyName("email1")]
        public string? Email1 { get; set; }

        [JsonPropertyName("email2")]
        public string? Email2 { get; set; }

        [JsonPropertyName("url1")]
        public string? Url1 { get; set; }

        [JsonPropertyName("url2")]
        public string? Url2 { get; set; }

        [JsonPropertyName("mailingAddress")]
        public Address? MailingAddress { get; set; }
    }

    public class Address
    {
        [JsonPropertyName("address1")]
        public string? Address1 { get; set; }

        [JsonPropertyName("address2")]
        public string? Address2 { get; set; }

        [JsonPropertyName("address3")]
        public string? Address3 { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("zip")]
        public string? Zip { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("isoCountryCode")]
        public string? IsoCountryCode { get; set; }
    }

    public class GLAccount
    {
        [JsonPropertyName("accountNo")]
        public string? AccountNo { get; set; }

        [JsonPropertyName("accountTitle")]
        public string? AccountTitle { get; set; }
    }

    public class Term
    {
        [JsonPropertyName("termName")]
        public string? TermName { get; set; }

        [JsonPropertyName("due")]
        public int? Due { get; set; }

        [JsonPropertyName("discount")]
        public int? Discount { get; set; }

        [JsonPropertyName("discountPercent")]
        public decimal? DiscountPercent { get; set; }
    }

    public class TaxGroup
    {
        [JsonPropertyName("taxGroupName")]
        public string? TaxGroupName { get; set; }
    }

    public class TaxSolution
    {
        [JsonPropertyName("taxSolutionName")]
        public string? TaxSolutionName { get; set; }
    }

    public class CustomerType
    {
        [JsonPropertyName("customerTypeId")]
        public string? CustomerTypeId { get; set; }

        [JsonPropertyName("customerTypeName")]
        public string? CustomerTypeName { get; set; }
    }

    public class VendorType
    {
        [JsonPropertyName("vendorTypeId")]
        public string? VendorTypeId { get; set; }

        [JsonPropertyName("vendorTypeName")]
        public string? VendorTypeName { get; set; }
    }

    public class Territory
    {
        [JsonPropertyName("territoryId")]
        public string? TerritoryId { get; set; }

        [JsonPropertyName("territoryName")]
        public string? TerritoryName { get; set; }
    }

    public class Department
    {
        [JsonPropertyName("departmentId")]
        public string? DepartmentId { get; set; }

        [JsonPropertyName("departmentName")]
        public string? DepartmentName { get; set; }
    }

    public class Location
    {
        [JsonPropertyName("locationId")]
        public string? LocationId { get; set; }

        [JsonPropertyName("locationName")]
        public string? LocationName { get; set; }
    }

    public class Dimensions
    {
        [JsonPropertyName("length")]
        public decimal? Length { get; set; }

        [JsonPropertyName("width")]
        public decimal? Width { get; set; }

        [JsonPropertyName("height")]
        public decimal? Height { get; set; }

        [JsonPropertyName("unit")]
        public string? Unit { get; set; }
    }

    public class InvoiceLine
    {
        [JsonPropertyName("lineNo")]
        public int? LineNo { get; set; }

        [JsonPropertyName("itemId")]
        public string? ItemId { get; set; }

        [JsonPropertyName("itemName")]
        public string? ItemName { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("quantity")]
        public decimal? Quantity { get; set; }

        [JsonPropertyName("unit")]
        public string? Unit { get; set; }

        [JsonPropertyName("price")]
        public decimal? Price { get; set; }

        [JsonPropertyName("amount")]
        public decimal? Amount { get; set; }

        [JsonPropertyName("taxable")]
        public bool? Taxable { get; set; }

        [JsonPropertyName("taxRate")]
        public decimal? TaxRate { get; set; }

        [JsonPropertyName("locationId")]
        public string? LocationId { get; set; }

        [JsonPropertyName("departmentId")]
        public string? DepartmentId { get; set; }

        [JsonPropertyName("projectId")]
        public string? ProjectId { get; set; }

        [JsonPropertyName("customerId")]
        public string? CustomerId { get; set; }

        [JsonPropertyName("vendorId")]
        public string? VendorId { get; set; }

        [JsonPropertyName("employeeId")]
        public string? EmployeeId { get; set; }

        [JsonPropertyName("itemId")]
        public string? ItemId2 { get; set; }

        [JsonPropertyName("classId")]
        public string? ClassId { get; set; }

        [JsonPropertyName("customFields")]
        public List<CustomField>? CustomFields { get; set; }
    }

    public class BillLine
    {
        [JsonPropertyName("lineNo")]
        public int? LineNo { get; set; }

        [JsonPropertyName("itemId")]
        public string? ItemId { get; set; }

        [JsonPropertyName("itemName")]
        public string? ItemName { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("quantity")]
        public decimal? Quantity { get; set; }

        [JsonPropertyName("unit")]
        public string? Unit { get; set; }

        [JsonPropertyName("price")]
        public decimal? Price { get; set; }

        [JsonPropertyName("amount")]
        public decimal? Amount { get; set; }

        [JsonPropertyName("taxable")]
        public bool? Taxable { get; set; }

        [JsonPropertyName("taxRate")]
        public decimal? TaxRate { get; set; }

        [JsonPropertyName("locationId")]
        public string? LocationId { get; set; }

        [JsonPropertyName("departmentId")]
        public string? DepartmentId { get; set; }

        [JsonPropertyName("projectId")]
        public string? ProjectId { get; set; }

        [JsonPropertyName("customerId")]
        public string? CustomerId { get; set; }

        [JsonPropertyName("vendorId")]
        public string? VendorId { get; set; }

        [JsonPropertyName("employeeId")]
        public string? EmployeeId { get; set; }

        [JsonPropertyName("itemId")]
        public string? ItemId2 { get; set; }

        [JsonPropertyName("classId")]
        public string? ClassId { get; set; }

        [JsonPropertyName("customFields")]
        public List<CustomField>? CustomFields { get; set; }
    }

    public class TaxEntry
    {
        [JsonPropertyName("taxId")]
        public string? TaxId { get; set; }

        [JsonPropertyName("taxName")]
        public string? TaxName { get; set; }

        [JsonPropertyName("taxAuthority")]
        public string? TaxAuthority { get; set; }

        [JsonPropertyName("taxRate")]
        public decimal? TaxRate { get; set; }

        [JsonPropertyName("taxableAmount")]
        public decimal? TaxableAmount { get; set; }

        [JsonPropertyName("taxAmount")]
        public decimal? TaxAmount { get; set; }
    }

    public class Attachment
    {
        [JsonPropertyName("attachmentId")]
        public string? AttachmentId { get; set; }

        [JsonPropertyName("fileName")]
        public string? FileName { get; set; }

        [JsonPropertyName("fileType")]
        public string? FileType { get; set; }

        [JsonPropertyName("fileSize")]
        public long? FileSize { get; set; }

        [JsonPropertyName("attachmentType")]
        public string? AttachmentType { get; set; }
    }

    public class CustomField
    {
        [JsonPropertyName("fieldName")]
        public string? FieldName { get; set; }

        [JsonPropertyName("fieldValue")]
        public string? FieldValue { get; set; }
    }

    public class InvoiceHistory
    {
        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("user")]
        public string? User { get; set; }
    }

    public class BillHistory
    {
        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("user")]
        public string? User { get; set; }
    }
}