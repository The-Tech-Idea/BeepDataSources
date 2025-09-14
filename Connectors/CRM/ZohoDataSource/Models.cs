// File: Connectors/Zoho/Models/ZohoModels.cs
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Zoho.Models
{
    // =======================================================
    // Base
    // =======================================================
    public abstract class ZohoEntityBase
    {
        [JsonIgnore] public IDataSource DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : ZohoEntityBase { DataSource = ds; return (T)this; }

        // Common meta
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("Owner")] public ZohoUserRef Owner { get; set; }
        [JsonPropertyName("Created_Time")] public DateTimeOffset? CreatedTime { get; set; }
        [JsonPropertyName("Modified_Time")] public DateTimeOffset? ModifiedTime { get; set; }
        [JsonPropertyName("$editable")] public bool? Editable { get; set; }
        [JsonPropertyName("$in_merge")] public bool? InMerge { get; set; }
        [JsonPropertyName("$approval_state")] public string ApprovalState { get; set; }
        [JsonPropertyName("$review_process")] public ZohoReviewProcess ReviewProcess { get; set; }
        [JsonPropertyName("$orchestration")] public object Orchestration { get; set; }
        [JsonPropertyName("Tag")] public List<ZohoTag> Tags { get; set; } = new();
    }

    public sealed class ZohoUserRef
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("email")] public string Email { get; set; }
    }

    public sealed class ZohoReviewProcess
    {
        [JsonPropertyName("approve")] public bool? Approve { get; set; }
        [JsonPropertyName("reject")] public bool? Reject { get; set; }
        [JsonPropertyName("resubmit")] public bool? Resubmit { get; set; }
    }

    public sealed class ZohoTag
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("id")] public string Id { get; set; }
    }

    // =======================================================
    // Leads
    // =======================================================
    public sealed class ZohoLead : ZohoEntityBase
    {
        [JsonPropertyName("Company")] public string Company { get; set; }
        [JsonPropertyName("First_Name")] public string FirstName { get; set; }
        [JsonPropertyName("Last_Name")] public string LastName { get; set; }
        [JsonPropertyName("Email")] public string Email { get; set; }
        [JsonPropertyName("Phone")] public string Phone { get; set; }
        [JsonPropertyName("Mobile")] public string Mobile { get; set; }
        [JsonPropertyName("Lead_Source")] public string LeadSource { get; set; }
        [JsonPropertyName("Lead_Status")] public string LeadStatus { get; set; }
        [JsonPropertyName("Industry")] public string Industry { get; set; }
        [JsonPropertyName("Annual_Revenue")] public decimal? AnnualRevenue { get; set; }
        [JsonPropertyName("Website")] public string Website { get; set; }
        [JsonPropertyName("Street")] public string Street { get; set; }
        [JsonPropertyName("City")] public string City { get; set; }
        [JsonPropertyName("State")] public string State { get; set; }
        [JsonPropertyName("Zip_Code")] public string ZipCode { get; set; }
        [JsonPropertyName("Country")] public string Country { get; set; }
        [JsonPropertyName("Description")] public string Description { get; set; }
        [JsonPropertyName("Campaign_Source")] public ZohoLookupRef CampaignSource { get; set; }
    }

    // =======================================================
    // Contacts
    // =======================================================
    public sealed class ZohoContact : ZohoEntityBase
    {
        [JsonPropertyName("First_Name")] public string FirstName { get; set; }
        [JsonPropertyName("Last_Name")] public string LastName { get; set; }
        [JsonPropertyName("Email")] public string Email { get; set; }
        [JsonPropertyName("Phone")] public string Phone { get; set; }
        [JsonPropertyName("Mobile")] public string Mobile { get; set; }
        [JsonPropertyName("Department")] public string Department { get; set; }
        [JsonPropertyName("Title")] public string Title { get; set; }
        [JsonPropertyName("Account_Name")] public ZohoLookupRef AccountName { get; set; }
        [JsonPropertyName("Mailing_Street")] public string MailingStreet { get; set; }
        [JsonPropertyName("Mailing_City")] public string MailingCity { get; set; }
        [JsonPropertyName("Mailing_State")] public string MailingState { get; set; }
        [JsonPropertyName("Mailing_Zip")] public string MailingZip { get; set; }
        [JsonPropertyName("Mailing_Country")] public string MailingCountry { get; set; }
        [JsonPropertyName("Other_Street")] public string OtherStreet { get; set; }
        [JsonPropertyName("Other_City")] public string OtherCity { get; set; }
        [JsonPropertyName("Other_State")] public string OtherState { get; set; }
        [JsonPropertyName("Other_Zip")] public string OtherZip { get; set; }
        [JsonPropertyName("Other_Country")] public string OtherCountry { get; set; }
        [JsonPropertyName("Description")] public string Description { get; set; }
    }

    // =======================================================
    // Accounts
    // =======================================================
    public sealed class ZohoAccount : ZohoEntityBase
    {
        [JsonPropertyName("Account_Name")] public string AccountName { get; set; }
        [JsonPropertyName("Phone")] public string Phone { get; set; }
        [JsonPropertyName("Website")] public string Website { get; set; }
        [JsonPropertyName("Industry")] public string Industry { get; set; }
        [JsonPropertyName("Account_Type")] public string AccountType { get; set; }
        [JsonPropertyName("Employees")] public int? Employees { get; set; }
        [JsonPropertyName("Annual_Revenue")] public decimal? AnnualRevenue { get; set; }
        [JsonPropertyName("Billing_Street")] public string BillingStreet { get; set; }
        [JsonPropertyName("Billing_City")] public string BillingCity { get; set; }
        [JsonPropertyName("Billing_State")] public string BillingState { get; set; }
        [JsonPropertyName("Billing_Code")] public string BillingCode { get; set; }
        [JsonPropertyName("Billing_Country")] public string BillingCountry { get; set; }
        [JsonPropertyName("Description")] public string Description { get; set; }
    }

    // =======================================================
    // Deals
    // =======================================================
    public sealed class ZohoDeal : ZohoEntityBase
    {
        [JsonPropertyName("Deal_Name")] public string DealName { get; set; }
        [JsonPropertyName("Stage")] public string Stage { get; set; }
        [JsonPropertyName("Amount")] public decimal? Amount { get; set; }
        [JsonPropertyName("Closing_Date")] public DateTimeOffset? ClosingDate { get; set; }
        [JsonPropertyName("Probability")] public int? Probability { get; set; }
        [JsonPropertyName("Type")] public string Type { get; set; }
        [JsonPropertyName("Lead_Source")] public string LeadSource { get; set; }
        [JsonPropertyName("Account_Name")] public ZohoLookupRef AccountName { get; set; }
        [JsonPropertyName("Contact_Name")] public ZohoLookupRef ContactName { get; set; }
        [JsonPropertyName("Description")] public string Description { get; set; }
        [JsonPropertyName("Campaign_Source")] public ZohoLookupRef CampaignSource { get; set; }
    }

    // =======================================================
    // Campaigns
    // =======================================================
    public sealed class ZohoCampaign : ZohoEntityBase
    {
        [JsonPropertyName("Campaign_Name")] public string CampaignName { get; set; }
        [JsonPropertyName("Type")] public string Type { get; set; }
        [JsonPropertyName("Status")] public string Status { get; set; }
        [JsonPropertyName("Start_Date")] public DateTimeOffset? StartDate { get; set; }
        [JsonPropertyName("End_Date")] public DateTimeOffset? EndDate { get; set; }
        [JsonPropertyName("Expected_Revenue")] public decimal? ExpectedRevenue { get; set; }
        [JsonPropertyName("Budgeted_Cost")] public decimal? BudgetedCost { get; set; }
        [JsonPropertyName("Actual_Cost")] public decimal? ActualCost { get; set; }
        [JsonPropertyName("Description")] public string Description { get; set; }
    }

    // =======================================================
    // Tasks
    // =======================================================
    public sealed class ZohoTask : ZohoEntityBase
    {
        [JsonPropertyName("Subject")] public string Subject { get; set; }
        [JsonPropertyName("Due_Date")] public DateTimeOffset? DueDate { get; set; }
        [JsonPropertyName("Status")] public string Status { get; set; }
        [JsonPropertyName("Priority")] public string Priority { get; set; }
        [JsonPropertyName("What_Id")] public ZohoLookupRef WhatId { get; set; }     // Related to (Account/Deal/…)
        [JsonPropertyName("Who_Id")] public ZohoLookupRef WhoId { get; set; }      // Related contact/lead
        [JsonPropertyName("Description")] public string Description { get; set; }
        [JsonPropertyName("Recurring_Activity")] public string RecurringActivity { get; set; }
    }

    // =======================================================
    // Events
    // =======================================================
    public sealed class ZohoEvent : ZohoEntityBase
    {
        [JsonPropertyName("Event_Title")] public string EventTitle { get; set; }
        [JsonPropertyName("Start_DateTime")] public DateTimeOffset? StartDateTime { get; set; }
        [JsonPropertyName("End_DateTime")] public DateTimeOffset? EndDateTime { get; set; }
        [JsonPropertyName("Location")] public string Location { get; set; }
        [JsonPropertyName("What_Id")] public ZohoLookupRef WhatId { get; set; }
        [JsonPropertyName("Who_Id")] public ZohoLookupRef WhoId { get; set; }
        [JsonPropertyName("Description")] public string Description { get; set; }
    }

    // =======================================================
    // Calls
    // =======================================================
    public sealed class ZohoCall : ZohoEntityBase
    {
        [JsonPropertyName("Call_Type")] public string CallType { get; set; } // Inbound/Outbound
        [JsonPropertyName("Subject")] public string Subject { get; set; }
        [JsonPropertyName("Call_Start_Time")] public DateTimeOffset? CallStartTime { get; set; }
        [JsonPropertyName("Call_Duration")] public string CallDuration { get; set; } // "00:15"
        [JsonPropertyName("Call_Purpose")] public string CallPurpose { get; set; }
        [JsonPropertyName("What_Id")] public ZohoLookupRef WhatId { get; set; }
        [JsonPropertyName("Who_Id")] public ZohoLookupRef WhoId { get; set; }
        [JsonPropertyName("Description")] public string Description { get; set; }
    }

    // =======================================================
    // Notes
    // =======================================================
    public sealed class ZohoNote : ZohoEntityBase
    {
        [JsonPropertyName("Note_Title")] public string NoteTitle { get; set; }
        [JsonPropertyName("Note_Content")] public string NoteContent { get; set; }
        [JsonPropertyName("Parent_Id")] public ZohoLookupRef ParentId { get; set; }
        [JsonPropertyName("Created_By")] public ZohoUserRef CreatedBy { get; set; }
        [JsonPropertyName("Modified_By")] public ZohoUserRef ModifiedBy { get; set; }
    }

    // =======================================================
    // Products
    // =======================================================
    public sealed class ZohoProduct : ZohoEntityBase
    {
        [JsonPropertyName("Product_Name")] public string ProductName { get; set; }
        [JsonPropertyName("Product_Code")] public string ProductCode { get; set; }
        [JsonPropertyName("Part_Number")] public string PartNumber { get; set; }
        [JsonPropertyName("Unit_Price")] public decimal? UnitPrice { get; set; }
        [JsonPropertyName("Tax")] public decimal? Tax { get; set; }
        [JsonPropertyName("Description")] public string Description { get; set; }
        [JsonPropertyName("Manufacturer")] public string Manufacturer { get; set; }
    }

    // =======================================================
    // Price Books
    // =======================================================
    public sealed class ZohoPriceBook : ZohoEntityBase
    {
        [JsonPropertyName("Price_Book_Name")] public string PriceBookName { get; set; }
        [JsonPropertyName("Active")] public bool? Active { get; set; }
        [JsonPropertyName("Description")] public string Description { get; set; }
    }

    // =======================================================
    // Quotes
    // =======================================================
    public sealed class ZohoQuote : ZohoEntityBase
    {
        [JsonPropertyName("Subject")] public string Subject { get; set; }
        [JsonPropertyName("Deal_Name")] public ZohoLookupRef DealName { get; set; }
        [JsonPropertyName("Account_Name")] public ZohoLookupRef AccountName { get; set; }
        [JsonPropertyName("Contact_Name")] public ZohoLookupRef ContactName { get; set; }
        [JsonPropertyName("Valid_Till")] public DateTimeOffset? ValidTill { get; set; }
        [JsonPropertyName("Sub_Total")] public decimal? SubTotal { get; set; }
        [JsonPropertyName("Discount")] public decimal? Discount { get; set; }
        [JsonPropertyName("Grand_Total")] public decimal? GrandTotal { get; set; }
        [JsonPropertyName("Terms_and_Conditions")] public string TermsAndConditions { get; set; }
    }

    // =======================================================
    // Sales Orders
    // =======================================================
    public sealed class ZohoSalesOrder : ZohoEntityBase
    {
        [JsonPropertyName("Subject")] public string Subject { get; set; }
        [JsonPropertyName("Deal_Name")] public ZohoLookupRef DealName { get; set; }
        [JsonPropertyName("Account_Name")] public ZohoLookupRef AccountName { get; set; }
        [JsonPropertyName("Contact_Name")] public ZohoLookupRef ContactName { get; set; }
        [JsonPropertyName("Status")] public string Status { get; set; }
        [JsonPropertyName("Due_Date")] public DateTimeOffset? DueDate { get; set; }
        [JsonPropertyName("Sub_Total")] public decimal? SubTotal { get; set; }
        [JsonPropertyName("Discount")] public decimal? Discount { get; set; }
        [JsonPropertyName("Grand_Total")] public decimal? GrandTotal { get; set; }
        [JsonPropertyName("Billing_Street")] public string BillingStreet { get; set; }
        [JsonPropertyName("Shipping_Street")] public string ShippingStreet { get; set; }
        [JsonPropertyName("Terms_and_Conditions")] public string TermsAndConditions { get; set; }
    }

    // =======================================================
    // Invoices
    // =======================================================
    public sealed class ZohoInvoice : ZohoEntityBase
    {
        [JsonPropertyName("Subject")] public string Subject { get; set; }
        [JsonPropertyName("Invoice_Date")] public DateTimeOffset? InvoiceDate { get; set; }
        [JsonPropertyName("Due_Date")] public DateTimeOffset? DueDate { get; set; }
        [JsonPropertyName("Account_Name")] public ZohoLookupRef AccountName { get; set; }
        [JsonPropertyName("Contact_Name")] public ZohoLookupRef ContactName { get; set; }
        [JsonPropertyName("Status")] public string Status { get; set; }
        [JsonPropertyName("Sub_Total")] public decimal? SubTotal { get; set; }
        [JsonPropertyName("Discount")] public decimal? Discount { get; set; }
        [JsonPropertyName("Grand_Total")] public decimal? GrandTotal { get; set; }
        [JsonPropertyName("Terms_and_Conditions")] public string TermsAndConditions { get; set; }
    }

    // =======================================================
    // Vendors
    // =======================================================
    public sealed class ZohoVendor : ZohoEntityBase
    {
        [JsonPropertyName("Vendor_Name")] public string VendorName { get; set; }
        [JsonPropertyName("Phone")] public string Phone { get; set; }
        [JsonPropertyName("Email")] public string Email { get; set; }
        [JsonPropertyName("Website")] public string Website { get; set; }
        [JsonPropertyName("Street")] public string Street { get; set; }
        [JsonPropertyName("City")] public string City { get; set; }
        [JsonPropertyName("State")] public string State { get; set; }
        [JsonPropertyName("Zip_Code")] public string ZipCode { get; set; }
        [JsonPropertyName("Country")] public string Country { get; set; }
        [JsonPropertyName("Description")] public string Description { get; set; }
    }

    // =======================================================
    // Users
    // =======================================================
    public sealed class ZohoUser : ZohoEntityBase
    {
        [JsonPropertyName("full_name")] public string FullName { get; set; }
        [JsonPropertyName("email")] public string Email { get; set; }
        [JsonPropertyName("role")] public ZohoUserRole Role { get; set; }
        [JsonPropertyName("profile")] public ZohoUserProfile Profile { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; } // active, inactive, deleted
        [JsonPropertyName("alias")] public string Alias { get; set; }
        [JsonPropertyName("timezone")] public string Timezone { get; set; }
    }

    public sealed class ZohoUserRole
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("id")] public string Id { get; set; }
    }

    public sealed class ZohoUserProfile
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("id")] public string Id { get; set; }
    }

    // =======================================================
    // Shared
    // =======================================================
    public sealed class ZohoLookupRef
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("id")] public string Id { get; set; }
    }

    // =======================================================
    // Registry (module name -> CLR type)
    // =======================================================
    public static class ZohoEntityRegistry
    {
        public static readonly Dictionary<string, Type> Types = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Leads"] = typeof(ZohoLead),
            ["Contacts"] = typeof(ZohoContact),
            ["Accounts"] = typeof(ZohoAccount),
            ["Deals"] = typeof(ZohoDeal),
            ["Campaigns"] = typeof(ZohoCampaign),
            ["Tasks"] = typeof(ZohoTask),
            ["Events"] = typeof(ZohoEvent),
            ["Calls"] = typeof(ZohoCall),
            ["Notes"] = typeof(ZohoNote),
            ["Products"] = typeof(ZohoProduct),
            ["Price_Books"] = typeof(ZohoPriceBook),
            ["Quotes"] = typeof(ZohoQuote),
            ["Sales_Orders"] = typeof(ZohoSalesOrder),
            ["Invoices"] = typeof(ZohoInvoice),
            ["Vendors"] = typeof(ZohoVendor),
            ["Users"] = typeof(ZohoUser),
        };

        public static IReadOnlyList<string> Names => new List<string>(Types.Keys);
        public static Type Resolve(string entityName) => entityName != null && Types.TryGetValue(entityName, out var t) ? t : null;
    }
}
