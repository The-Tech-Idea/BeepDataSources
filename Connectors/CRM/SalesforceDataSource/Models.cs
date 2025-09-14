// File: Connectors/Salesforce/Models/SalesforceModels.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Salesforce.Models
{
    // Common base for SFDC records
    public abstract class SalesforceEntityBase
    {
        [JsonPropertyName("Id")] public string Id { get; set; }
        [JsonPropertyName("Name")] public string Name { get; set; }
        [JsonPropertyName("CreatedDate")] public DateTime? CreatedDate { get; set; }
        [JsonPropertyName("LastModifiedDate")] public DateTime? LastModifiedDate { get; set; }
        [JsonPropertyName("IsDeleted")] public bool? IsDeleted { get; set; }

        // Optional: Let POCOs know their datasource after hydration
        [JsonIgnore] public IDataSource DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : SalesforceEntityBase { DataSource = ds; return (T)this; }
    }

    // ---- Core Standard Objects (trim or extend as you like) ----

    public sealed class Account : SalesforceEntityBase
    {
        [JsonPropertyName("AccountNumber")] public string AccountNumber { get; set; }
        [JsonPropertyName("Type")] public string Type { get; set; }
        [JsonPropertyName("Industry")] public string Industry { get; set; }
        [JsonPropertyName("Phone")] public string Phone { get; set; }
        [JsonPropertyName("Website")] public string Website { get; set; }
        [JsonPropertyName("BillingCity")] public string BillingCity { get; set; }
        [JsonPropertyName("BillingCountry")] public string BillingCountry { get; set; }
    }

    public sealed class Contact : SalesforceEntityBase
    {
        [JsonPropertyName("FirstName")] public string FirstName { get; set; }
        [JsonPropertyName("LastName")] public string LastName { get; set; }
        [JsonPropertyName("Email")] public string Email { get; set; }
        [JsonPropertyName("Phone")] public string Phone { get; set; }
        [JsonPropertyName("AccountId")] public string AccountId { get; set; }
    }

    public sealed class Lead : SalesforceEntityBase
    {
        [JsonPropertyName("FirstName")] public string FirstName { get; set; }
        [JsonPropertyName("LastName")] public string LastName { get; set; }
        [JsonPropertyName("Company")] public string Company { get; set; }
        [JsonPropertyName("Status")] public string Status { get; set; }
        [JsonPropertyName("Email")] public string Email { get; set; }
        [JsonPropertyName("Phone")] public string Phone { get; set; }
    }

    public sealed class Opportunity : SalesforceEntityBase
    {
        [JsonPropertyName("StageName")] public string StageName { get; set; }
        [JsonPropertyName("CloseDate")] public DateTime? CloseDate { get; set; }
        [JsonPropertyName("Amount")] public decimal? Amount { get; set; }
        [JsonPropertyName("AccountId")] public string AccountId { get; set; }
    }

    public sealed class OpportunityLineItem
    {
        [JsonPropertyName("Id")] public string Id { get; set; }
        [JsonPropertyName("OpportunityId")] public string OpportunityId { get; set; }
        [JsonPropertyName("PricebookEntryId")] public string PricebookEntryId { get; set; }
        [JsonPropertyName("Quantity")] public decimal? Quantity { get; set; }
        [JsonPropertyName("UnitPrice")] public decimal? UnitPrice { get; set; }
        [JsonPropertyName("TotalPrice")] public decimal? TotalPrice { get; set; }

        [JsonIgnore] public IDataSource DataSource { get; private set; }
        public OpportunityLineItem Attach(IDataSource ds) { DataSource = ds; return this; }
    }

    public sealed class Product2 : SalesforceEntityBase
    {
        [JsonPropertyName("ProductCode")] public string ProductCode { get; set; }
        [JsonPropertyName("IsActive")] public bool? IsActive { get; set; }
    }

    public sealed class Pricebook2 : SalesforceEntityBase
    {
        [JsonPropertyName("IsActive")] public bool? IsActive { get; set; }
    }

    public sealed class Case : SalesforceEntityBase
    {
        [JsonPropertyName("CaseNumber")] public string CaseNumber { get; set; }
        [JsonPropertyName("Subject")] public string Subject { get; set; }
        [JsonPropertyName("Status")] public string Status { get; set; }
        [JsonPropertyName("Priority")] public string Priority { get; set; }
        [JsonPropertyName("AccountId")] public string AccountId { get; set; }
        [JsonPropertyName("ContactId")] public string ContactId { get; set; }
    }

    public sealed class User : SalesforceEntityBase
    {
        [JsonPropertyName("Username")] public string Username { get; set; }
        [JsonPropertyName("Email")] public string Email { get; set; }
        [JsonPropertyName("IsActive")] public bool? IsActive { get; set; }
    }

    public sealed class SFTask : SalesforceEntityBase
    {
        [JsonPropertyName("Subject")] public string Subject { get; set; }
        [JsonPropertyName("Status")] public string Status { get; set; }
        [JsonPropertyName("Priority")] public string Priority { get; set; }
        [JsonPropertyName("ActivityDate")] public DateTime? ActivityDate { get; set; }
        [JsonPropertyName("WhatId")] public string WhatId { get; set; }
        [JsonPropertyName("WhoId")] public string WhoId { get; set; }
        [JsonPropertyName("OwnerId")] public string OwnerId { get; set; }
    }

    // Wrapper for /query and /queryAll responses
    public sealed class QueryResult<T>
    {
        [JsonPropertyName("totalSize")] public int TotalSize { get; set; }
        [JsonPropertyName("done")] public bool Done { get; set; }
        [JsonPropertyName("records")] public List<T> Records { get; set; } = new();
        [JsonPropertyName("nextRecordsUrl")] public string NextRecordsUrl { get; set; }
    }

    // Registry so the connector can advertise its fixed entities
    public static class SalesforceEntityRegistry
    {
        public static readonly Dictionary<string, Type> Types = new()
        {
            ["Account"] = typeof(Account),
            ["Contact"] = typeof(Contact),
            ["Lead"] = typeof(Lead),
            ["Opportunity"] = typeof(Opportunity),
            ["OpportunityLineItem"] = typeof(OpportunityLineItem),
            ["Product2"] = typeof(Product2),
            ["Pricebook2"] = typeof(Pricebook2),
            ["Case"] = typeof(Case),
            ["User"] = typeof(User),
            ["Task"] = typeof(Task)
        };

        public static IReadOnlyList<string> Names => Types.Keys.ToList();

        // Utility: get the SOQL field list from POCO public props, honoring JsonPropertyName if present
        public static IEnumerable<string> GetSoqlFields(Type t)
        {
            foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var j = p.GetCustomAttribute<JsonPropertyNameAttribute>();
                yield return j?.Name ?? p.Name;
            }
        }
    }
}
