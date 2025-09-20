using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.MYOB
{
    // Base class for MYOB entities
    public class MYOBBaseEntity
    {
        [JsonPropertyName("UID")]
        public string? UID { get; set; }

        [JsonPropertyName("RowVersion")]
        public int? RowVersion { get; set; }

        [JsonPropertyName("LastModified")]
        public DateTime? LastModified { get; set; }

        [JsonPropertyName("URI")]
        public string? URI { get; set; }
    }

    // Company File entity
    public class CompanyFile : MYOBBaseEntity
    {
        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        [JsonPropertyName("LegalName")]
        public string? LegalName { get; set; }

        [JsonPropertyName("Address")]
        public string? Address { get; set; }

        [JsonPropertyName("ABN")]
        public string? ABN { get; set; }

        [JsonPropertyName("PhoneNumber")]
        public string? PhoneNumber { get; set; }

        [JsonPropertyName("FaxNumber")]
        public string? FaxNumber { get; set; }

        [JsonPropertyName("Email")]
        public string? Email { get; set; }

        [JsonPropertyName("Website")]
        public string? Website { get; set; }

        [JsonPropertyName("IsReadOnly")]
        public bool? IsReadOnly { get; set; }

        [JsonPropertyName("CurrentFinancialYear")]
        public int? CurrentFinancialYear { get; set; }
    }

    // Customer entity
    public class Customer : MYOBBaseEntity
    {
        [JsonPropertyName("CompanyName")]
        public string? CompanyName { get; set; }

        [JsonPropertyName("FirstName")]
        public string? FirstName { get; set; }

        [JsonPropertyName("LastName")]
        public string? LastName { get; set; }

        [JsonPropertyName("IsIndividual")]
        public bool? IsIndividual { get; set; }

        [JsonPropertyName("DisplayID")]
        public string? DisplayID { get; set; }

        [JsonPropertyName("Addresses")]
        public List<Address>? Addresses { get; set; }

        [JsonPropertyName("Phones")]
        public List<Phone>? Phones { get; set; }

        [JsonPropertyName("Email")]
        public string? Email { get; set; }

        [JsonPropertyName("Website")]
        public string? Website { get; set; }

        [JsonPropertyName("Notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("SellingDetails")]
        public SellingDetails? SellingDetails { get; set; }

        [JsonPropertyName("PaymentDetails")]
        public PaymentDetails? PaymentDetails { get; set; }

        [JsonPropertyName("IsActive")]
        public bool? IsActive { get; set; }
    }

    // Supplier entity
    public class Supplier : MYOBBaseEntity
    {
        [JsonPropertyName("CompanyName")]
        public string? CompanyName { get; set; }

        [JsonPropertyName("FirstName")]
        public string? FirstName { get; set; }

        [JsonPropertyName("LastName")]
        public string? LastName { get; set; }

        [JsonPropertyName("IsIndividual")]
        public bool? IsIndividual { get; set; }

        [JsonPropertyName("DisplayID")]
        public string? DisplayID { get; set; }

        [JsonPropertyName("Addresses")]
        public List<Address>? Addresses { get; set; }

        [JsonPropertyName("Phones")]
        public List<Phone>? Phones { get; set; }

        [JsonPropertyName("Email")]
        public string? Email { get; set; }

        [JsonPropertyName("Website")]
        public string? Website { get; set; }

        [JsonPropertyName("Notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("BuyingDetails")]
        public BuyingDetails? BuyingDetails { get; set; }

        [JsonPropertyName("PaymentDetails")]
        public PaymentDetails? PaymentDetails { get; set; }

        [JsonPropertyName("IsActive")]
        public bool? IsActive { get; set; }
    }

    // Item entity
    public class Item : MYOBBaseEntity
    {
        [JsonPropertyName("Number")]
        public string? Number { get; set; }

        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        [JsonPropertyName("Description")]
        public string? Description { get; set; }

        [JsonPropertyName("Type")]
        public string? Type { get; set; } // Service, Non-inventory, Inventory

        [JsonPropertyName("IsActive")]
        public bool? IsActive { get; set; }

        [JsonPropertyName("IsInventoried")]
        public bool? IsInventoried { get; set; }

        [JsonPropertyName("UnitOfMeasure")]
        public string? UnitOfMeasure { get; set; }

        [JsonPropertyName("UnitPrice")]
        public decimal? UnitPrice { get; set; }

        [JsonPropertyName("BaseSellingPrice")]
        public decimal? BaseSellingPrice { get; set; }

        [JsonPropertyName("SellingDetails")]
        public ItemSellingDetails? SellingDetails { get; set; }

        [JsonPropertyName("BuyingDetails")]
        public ItemBuyingDetails? BuyingDetails { get; set; }

        [JsonPropertyName("CustomList1")]
        public CustomList? CustomList1 { get; set; }

        [JsonPropertyName("CustomList2")]
        public CustomList? CustomList2 { get; set; }

        [JsonPropertyName("CustomList3")]
        public CustomList? CustomList3 { get; set; }

        [JsonPropertyName("CustomField1")]
        public string? CustomField1 { get; set; }

        [JsonPropertyName("CustomField2")]
        public string? CustomField2 { get; set; }

        [JsonPropertyName("CustomField3")]
        public string? CustomField3 { get; set; }
    }

    // Invoice entity
    public class Invoice : MYOBBaseEntity
    {
        [JsonPropertyName("Number")]
        public string? Number { get; set; }

        [JsonPropertyName("Date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("Customer")]
        public CustomerPurchase? Customer { get; set; }

        [JsonPropertyName("ShipToAddress")]
        public string? ShipToAddress { get; set; }

        [JsonPropertyName("Terms")]
        public PaymentTerms? Terms { get; set; }

        [JsonPropertyName("IsTaxInclusive")]
        public bool? IsTaxInclusive { get; set; }

        [JsonPropertyName("Lines")]
        public List<InvoiceLine>? Lines { get; set; }

        [JsonPropertyName("Subtotal")]
        public decimal? Subtotal { get; set; }

        [JsonPropertyName("Freight")]
        public decimal? Freight { get; set; }

        [JsonPropertyName("TotalTax")]
        public decimal? TotalTax { get; set; }

        [JsonPropertyName("TotalAmount")]
        public decimal? TotalAmount { get; set; }

        [JsonPropertyName("Status")]
        public string? Status { get; set; }

        [JsonPropertyName("BalanceDueAmount")]
        public decimal? BalanceDueAmount { get; set; }

        [JsonPropertyName("InvoiceType")]
        public string? InvoiceType { get; set; }

        [JsonPropertyName("Comment")]
        public string? Comment { get; set; }

        [JsonPropertyName("JournalMemo")]
        public string? JournalMemo { get; set; }

        [JsonPropertyName("PromisedDate")]
        public DateTime? PromisedDate { get; set; }
    }

    // Bill entity
    public class Bill : MYOBBaseEntity
    {
        [JsonPropertyName("Number")]
        public string? Number { get; set; }

        [JsonPropertyName("Date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("Supplier")]
        public SupplierPurchase? Supplier { get; set; }

        [JsonPropertyName("ShipToAddress")]
        public string? ShipToAddress { get; set; }

        [JsonPropertyName("Terms")]
        public PaymentTerms? Terms { get; set; }

        [JsonPropertyName("IsTaxInclusive")]
        public bool? IsTaxInclusive { get; set; }

        [JsonPropertyName("Lines")]
        public List<BillLine>? Lines { get; set; }

        [JsonPropertyName("Subtotal")]
        public decimal? Subtotal { get; set; }

        [JsonPropertyName("Freight")]
        public decimal? Freight { get; set; }

        [JsonPropertyName("TotalTax")]
        public decimal? TotalTax { get; set; }

        [JsonPropertyName("TotalAmount")]
        public decimal? TotalAmount { get; set; }

        [JsonPropertyName("Status")]
        public string? Status { get; set; }

        [JsonPropertyName("BalanceDueAmount")]
        public decimal? BalanceDueAmount { get; set; }

        [JsonPropertyName("BillType")]
        public string? BillType { get; set; }

        [JsonPropertyName("Comment")]
        public string? Comment { get; set; }

        [JsonPropertyName("JournalMemo")]
        public string? JournalMemo { get; set; }

        [JsonPropertyName("PromisedDate")]
        public DateTime? PromisedDate { get; set; }
    }

    // Payment entity
    public class Payment : MYOBBaseEntity
    {
        [JsonPropertyName("Date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("AmountReceived")]
        public decimal? AmountReceived { get; set; }

        [JsonPropertyName("PaymentMethod")]
        public string? PaymentMethod { get; set; }

        [JsonPropertyName("Memo")]
        public string? Memo { get; set; }

        [JsonPropertyName("Account")]
        public Account? Account { get; set; }

        [JsonPropertyName("Customer")]
        public CustomerPurchase? Customer { get; set; }

        [JsonPropertyName("Invoices")]
        public List<PaymentInvoice>? Invoices { get; set; }
    }

    // Supplier Payment entity
    public class SupplierPayment : MYOBBaseEntity
    {
        [JsonPropertyName("Date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("AmountPaid")]
        public decimal? AmountPaid { get; set; }

        [JsonPropertyName("PaymentMethod")]
        public string? PaymentMethod { get; set; }

        [JsonPropertyName("Memo")]
        public string? Memo { get; set; }

        [JsonPropertyName("Account")]
        public Account? Account { get; set; }

        [JsonPropertyName("Supplier")]
        public SupplierPurchase? Supplier { get; set; }

        [JsonPropertyName("Bills")]
        public List<PaymentBill>? Bills { get; set; }
    }

    // Journal Transaction entity
    public class JournalTransaction : MYOBBaseEntity
    {
        [JsonPropertyName("DateOccurred")]
        public DateTime? DateOccurred { get; set; }

        [JsonPropertyName("DatePosted")]
        public DateTime? DatePosted { get; set; }

        [JsonPropertyName("Memo")]
        public string? Memo { get; set; }

        [JsonPropertyName("ReferenceNumber")]
        public string? ReferenceNumber { get; set; }

        [JsonPropertyName("IsYearEndAdjustment")]
        public bool? IsYearEndAdjustment { get; set; }

        [JsonPropertyName("Lines")]
        public List<JournalLine>? Lines { get; set; }

        [JsonPropertyName("Category")]
        public string? Category { get; set; }
    }

    // Account entity
    public class Account : MYOBBaseEntity
    {
        [JsonPropertyName("DisplayID")]
        public string? DisplayID { get; set; }

        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        [JsonPropertyName("Type")]
        public string? Type { get; set; }

        [JsonPropertyName("Classification")]
        public string? Classification { get; set; }

        [JsonPropertyName("IsActive")]
        public bool? IsActive { get; set; }

        [JsonPropertyName("Description")]
        public string? Description { get; set; }

        [JsonPropertyName("TaxCode")]
        public TaxCode? TaxCode { get; set; }

        [JsonPropertyName("OpeningBalance")]
        public decimal? OpeningBalance { get; set; }

        [JsonPropertyName("CurrentBalance")]
        public decimal? CurrentBalance { get; set; }
    }

    // Tax Code entity
    public class TaxCode : MYOBBaseEntity
    {
        [JsonPropertyName("Code")]
        public string? Code { get; set; }

        [JsonPropertyName("Description")]
        public string? Description { get; set; }

        [JsonPropertyName("Rate")]
        public decimal? Rate { get; set; }

        [JsonPropertyName("Type")]
        public string? Type { get; set; }

        [JsonPropertyName("IsActive")]
        public bool? IsActive { get; set; }
    }

    // Employee entity
    public class Employee : MYOBBaseEntity
    {
        [JsonPropertyName("EmployeeNumber")]
        public string? EmployeeNumber { get; set; }

        [JsonPropertyName("FirstName")]
        public string? FirstName { get; set; }

        [JsonPropertyName("LastName")]
        public string? LastName { get; set; }

        [JsonPropertyName("IsActive")]
        public bool? IsActive { get; set; }

        [JsonPropertyName("Addresses")]
        public List<Address>? Addresses { get; set; }

        [JsonPropertyName("Phones")]
        public List<Phone>? Phones { get; set; }

        [JsonPropertyName("Email")]
        public string? Email { get; set; }

        [JsonPropertyName("DateOfBirth")]
        public DateTime? DateOfBirth { get; set; }

        [JsonPropertyName("Gender")]
        public string? Gender { get; set; }

        [JsonPropertyName("StartDate")]
        public DateTime? StartDate { get; set; }

        [JsonPropertyName("TerminationDate")]
        public DateTime? TerminationDate { get; set; }

        [JsonPropertyName("PayrollDetails")]
        public PayrollDetails? PayrollDetails { get; set; }
    }

    // Payroll Category entity
    public class PayrollCategory : MYOBBaseEntity
    {
        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        [JsonPropertyName("Type")]
        public string? Type { get; set; }

        [JsonPropertyName("IsActive")]
        public bool? IsActive { get; set; }

        [JsonPropertyName("Wage")]
        public WageDetails? Wage { get; set; }

        [JsonPropertyName("Deduction")]
        public DeductionDetails? Deduction { get; set; }

        [JsonPropertyName("Expense")]
        public ExpenseDetails? Expense { get; set; }
    }

    // Pay entity
    public class Pay : MYOBBaseEntity
    {
        [JsonPropertyName("Employee")]
        public EmployeePay? Employee { get; set; }

        [JsonPropertyName("Date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("PayNumber")]
        public string? PayNumber { get; set; }

        [JsonPropertyName("Lines")]
        public List<PayLine>? Lines { get; set; }

        [JsonPropertyName("TotalEarnings")]
        public decimal? TotalEarnings { get; set; }

        [JsonPropertyName("TotalDeductions")]
        public decimal? TotalDeductions { get; set; }

        [JsonPropertyName("TotalReimbursements")]
        public decimal? TotalReimbursements { get; set; }

        [JsonPropertyName("TotalSuperannuation")]
        public decimal? TotalSuperannuation { get; set; }

        [JsonPropertyName("NetPay")]
        public decimal? NetPay { get; set; }

        [JsonPropertyName("Status")]
        public string? Status { get; set; }
    }

    // Supporting classes
    public class Address
    {
        [JsonPropertyName("Street")]
        public string? Street { get; set; }

        [JsonPropertyName("City")]
        public string? City { get; set; }

        [JsonPropertyName("State")]
        public string? State { get; set; }

        [JsonPropertyName("PostCode")]
        public string? PostCode { get; set; }

        [JsonPropertyName("Country")]
        public string? Country { get; set; }

        [JsonPropertyName("Location")]
        public int? Location { get; set; } // 1=Street, 2=Postal, 3=Other
    }

    public class Phone
    {
        [JsonPropertyName("PhoneType")]
        public string? PhoneType { get; set; }

        [JsonPropertyName("PhoneNumber")]
        public string? PhoneNumber { get; set; }

        [JsonPropertyName("AreaCode")]
        public string? AreaCode { get; set; }
    }

    public class SellingDetails
    {
        [JsonPropertyName("SaleLayout")]
        public string? SaleLayout { get; set; }

        [JsonPropertyName("PrintedForm")]
        public string? PrintedForm { get; set; }

        [JsonPropertyName("InvoiceDelivery")]
        public string? InvoiceDelivery { get; set; }
    }

    public class BuyingDetails
    {
        [JsonPropertyName("PurchaseLayout")]
        public string? PurchaseLayout { get; set; }

        [JsonPropertyName("PrintedForm")]
        public string? PrintedForm { get; set; }

        [JsonPropertyName("ExpenseAccount")]
        public Account? ExpenseAccount { get; set; }
    }

    public class PaymentDetails
    {
        [JsonPropertyName("Method")]
        public string? Method { get; set; }

        [JsonPropertyName("CardNumber")]
        public string? CardNumber { get; set; }

        [JsonPropertyName("NameOnCard")]
        public string? NameOnCard { get; set; }

        [JsonPropertyName("ExpiryDate")]
        public string? ExpiryDate { get; set; }

        [JsonPropertyName("BSBNumber")]
        public string? BSBNumber { get; set; }

        [JsonPropertyName("BankAccountNumber")]
        public string? BankAccountNumber { get; set; }

        [JsonPropertyName("BankAccountName")]
        public string? BankAccountName { get; set; }
    }

    public class ItemSellingDetails
    {
        [JsonPropertyName("BaseSellingPrice")]
        public decimal? BaseSellingPrice { get; set; }

        [JsonPropertyName("IncomeAccount")]
        public Account? IncomeAccount { get; set; }

        [JsonPropertyName("TaxCode")]
        public TaxCode? TaxCode { get; set; }
    }

    public class ItemBuyingDetails
    {
        [JsonPropertyName("BasePurchasePrice")]
        public decimal? BasePurchasePrice { get; set; }

        [JsonPropertyName("ExpenseAccount")]
        public Account? ExpenseAccount { get; set; }

        [JsonPropertyName("TaxCode")]
        public TaxCode? TaxCode { get; set; }
    }

    public class CustomList
    {
        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        [JsonPropertyName("Value")]
        public string? Value { get; set; }
    }

    public class CustomerPurchase
    {
        [JsonPropertyName("UID")]
        public string? UID { get; set; }

        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        [JsonPropertyName("DisplayID")]
        public string? DisplayID { get; set; }
    }

    public class SupplierPurchase
    {
        [JsonPropertyName("UID")]
        public string? UID { get; set; }

        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        [JsonPropertyName("DisplayID")]
        public string? DisplayID { get; set; }
    }

    public class PaymentTerms
    {
        [JsonPropertyName("PaymentIsDue")]
        public string? PaymentIsDue { get; set; }

        [JsonPropertyName("DiscountDays")]
        public int? DiscountDays { get; set; }

        [JsonPropertyName("BalanceDueDate")]
        public int? BalanceDueDate { get; set; }

        [JsonPropertyName("DiscountForEarlyPayment")]
        public decimal? DiscountForEarlyPayment { get; set; }

        [JsonPropertyName("VolumeDiscount")]
        public decimal? VolumeDiscount { get; set; }
    }

    public class InvoiceLine
    {
        [JsonPropertyName("RowID")]
        public int? RowID { get; set; }

        [JsonPropertyName("Item")]
        public ItemPurchase? Item { get; set; }

        [JsonPropertyName("Description")]
        public string? Description { get; set; }

        [JsonPropertyName("ShipQuantity")]
        public decimal? ShipQuantity { get; set; }

        [JsonPropertyName("UnitPrice")]
        public decimal? UnitPrice { get; set; }

        [JsonPropertyName("DiscountPercent")]
        public decimal? DiscountPercent { get; set; }

        [JsonPropertyName("Total")]
        public decimal? Total { get; set; }

        [JsonPropertyName("Account")]
        public Account? Account { get; set; }

        [JsonPropertyName("TaxCode")]
        public TaxCode? TaxCode { get; set; }

        [JsonPropertyName("Job")]
        public string? Job { get; set; }
    }

    public class BillLine
    {
        [JsonPropertyName("RowID")]
        public int? RowID { get; set; }

        [JsonPropertyName("Item")]
        public ItemPurchase? Item { get; set; }

        [JsonPropertyName("Description")]
        public string? Description { get; set; }

        [JsonPropertyName("Quantity")]
        public decimal? Quantity { get; set; }

        [JsonPropertyName("UnitPrice")]
        public decimal? UnitPrice { get; set; }

        [JsonPropertyName("DiscountPercent")]
        public decimal? DiscountPercent { get; set; }

        [JsonPropertyName("Total")]
        public decimal? Total { get; set; }

        [JsonPropertyName("Account")]
        public Account? Account { get; set; }

        [JsonPropertyName("TaxCode")]
        public TaxCode? TaxCode { get; set; }

        [JsonPropertyName("Job")]
        public string? Job { get; set; }
    }

    public class ItemPurchase
    {
        [JsonPropertyName("UID")]
        public string? UID { get; set; }

        [JsonPropertyName("Number")]
        public string? Number { get; set; }

        [JsonPropertyName("Name")]
        public string? Name { get; set; }
    }

    public class PaymentInvoice
    {
        [JsonPropertyName("UID")]
        public string? UID { get; set; }

        [JsonPropertyName("Number")]
        public string? Number { get; set; }

        [JsonPropertyName("AmountApplied")]
        public decimal? AmountApplied { get; set; }
    }

    public class PaymentBill
    {
        [JsonPropertyName("UID")]
        public string? UID { get; set; }

        [JsonPropertyName("Number")]
        public string? Number { get; set; }

        [JsonPropertyName("AmountApplied")]
        public decimal? AmountApplied { get; set; }
    }

    public class JournalLine
    {
        [JsonPropertyName("RowID")]
        public int? RowID { get; set; }

        [JsonPropertyName("Account")]
        public Account? Account { get; set; }

        [JsonPropertyName("Description")]
        public string? Description { get; set; }

        [JsonPropertyName("DebitAmount")]
        public decimal? DebitAmount { get; set; }

        [JsonPropertyName("CreditAmount")]
        public decimal? CreditAmount { get; set; }

        [JsonPropertyName("TaxCode")]
        public TaxCode? TaxCode { get; set; }

        [JsonPropertyName("Job")]
        public string? Job { get; set; }
    }

    public class PayrollDetails
    {
        [JsonPropertyName("PayrollID")]
        public string? PayrollID { get; set; }

        [JsonPropertyName("TaxFileNumber")]
        public string? TaxFileNumber { get; set; }

        [JsonPropertyName("EmploymentBasis")]
        public string? EmploymentBasis { get; set; }

        [JsonPropertyName("StandardHoursPerWeek")]
        public decimal? StandardHoursPerWeek { get; set; }

        [JsonPropertyName("HourlyRate")]
        public decimal? HourlyRate { get; set; }

        [JsonPropertyName("WageExpenseAccount")]
        public Account? WageExpenseAccount { get; set; }
    }

    public class WageDetails
    {
        [JsonPropertyName("Type")]
        public string? Type { get; set; }

        [JsonPropertyName("Rate")]
        public decimal? Rate { get; set; }

        [JsonPropertyName("ExpenseAccount")]
        public Account? ExpenseAccount { get; set; }

        [JsonPropertyName("LiabilityAccount")]
        public Account? LiabilityAccount { get; set; }
    }

    public class DeductionDetails
    {
        [JsonPropertyName("Type")]
        public string? Type { get; set; }

        [JsonPropertyName("Rate")]
        public decimal? Rate { get; set; }

        [JsonPropertyName("ExpenseAccount")]
        public Account? ExpenseAccount { get; set; }

        [JsonPropertyName("LiabilityAccount")]
        public Account? LiabilityAccount { get; set; }
    }

    public class ExpenseDetails
    {
        [JsonPropertyName("Type")]
        public string? Type { get; set; }

        [JsonPropertyName("Rate")]
        public decimal? Rate { get; set; }

        [JsonPropertyName("ExpenseAccount")]
        public Account? ExpenseAccount { get; set; }
    }

    public class EmployeePay
    {
        [JsonPropertyName("UID")]
        public string? UID { get; set; }

        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        [JsonPropertyName("EmployeeNumber")]
        public string? EmployeeNumber { get; set; }
    }

    public class PayLine
    {
        [JsonPropertyName("PayrollCategory")]
        public PayrollCategory? PayrollCategory { get; set; }

        [JsonPropertyName("Memo")]
        public string? Memo { get; set; }

        [JsonPropertyName("Hours")]
        public decimal? Hours { get; set; }

        [JsonPropertyName("Rate")]
        public decimal? Rate { get; set; }

        [JsonPropertyName("Amount")]
        public decimal? Amount { get; set; }

        [JsonPropertyName("Job")]
        public string? Job { get; set; }
    }

    // Registry (maps your fixed entity names to CLR types)
    public static class MYOBEntityRegistry
    {
        public static readonly Dictionary<string, Type> Types = new(StringComparer.OrdinalIgnoreCase)
        {
            // Company File
            ["companyfile"] = typeof(CompanyFile),

            // Customers
            ["customers"] = typeof(Customer),

            // Suppliers
            ["suppliers"] = typeof(Supplier),

            // Items
            ["items"] = typeof(Item),

            // Invoices
            ["invoices"] = typeof(Invoice),
            ["invoiceitems"] = typeof(InvoiceLine),

            // Bills
            ["bills"] = typeof(Bill),
            ["billitems"] = typeof(BillLine),

            // Payments
            ["payments"] = typeof(Payment),
            ["supplierpayments"] = typeof(SupplierPayment),

            // Journals
            ["journals"] = typeof(JournalTransaction),

            // Accounts
            ["accounts"] = typeof(Account),

            // Tax Codes
            ["taxcodes"] = typeof(TaxCode),

            // Employees
            ["employees"] = typeof(Employee),

            // Payroll
            ["payrollcategories"] = typeof(PayrollCategory),
            ["pays"] = typeof(Pay)
        };

        public static IReadOnlyList<string> Names => new List<string>(Types.Keys);
    }
}