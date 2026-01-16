// File: Connectors/CRM/Dynamics365DataSource/Models.cs
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Dynamics365.Models
{
    // Common base for Dynamics 365 CRM entities
    public abstract class Dynamics365EntityBase
    {
        [JsonPropertyName("@odata.etag")] public string? ODataEtag { get; set; }
        [JsonPropertyName("@odata.id")] public string? ODataId { get; set; }
        [JsonPropertyName("@odata.editLink")] public string? ODataEditLink { get; set; }
        [JsonPropertyName("createdon")] public DateTime? CreatedOn { get; set; }
        [JsonPropertyName("modifiedon")] public DateTime? ModifiedOn { get; set; }
        [JsonPropertyName("statecode")] public int? StateCode { get; set; }
        [JsonPropertyName("statuscode")] public int? StatusCode { get; set; }

        // Optional: Let POCOs know their datasource after hydration
        [JsonIgnore] public IDataSource? DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : Dynamics365EntityBase { DataSource = ds; return (T)this; }
    }

    // ---- Core CRM Entities ----

    public sealed class Account : Dynamics365EntityBase
    {
        [JsonPropertyName("accountid")] public string? AccountId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("accountnumber")] public string? AccountNumber { get; set; }
        [JsonPropertyName("telephone1")] public string? Telephone1 { get; set; }
        [JsonPropertyName("websiteurl")] public string? WebsiteUrl { get; set; }
        [JsonPropertyName("emailaddress1")] public string? EmailAddress1 { get; set; }
        [JsonPropertyName("address1_line1")] public string? Address1Line1 { get; set; }
        [JsonPropertyName("address1_city")] public string? Address1City { get; set; }
        [JsonPropertyName("address1_stateorprovince")] public string? Address1StateOrProvince { get; set; }
        [JsonPropertyName("address1_postalcode")] public string? Address1PostalCode { get; set; }
        [JsonPropertyName("address1_country")] public string? Address1Country { get; set; }
        [JsonPropertyName("industrycode")] public int? IndustryCode { get; set; }
        [JsonPropertyName("revenue")] public decimal? Revenue { get; set; }
        [JsonPropertyName("numberofemployees")] public int? NumberOfEmployees { get; set; }
        [JsonPropertyName("ownerid")] public string? OwnerId { get; set; }
        [JsonPropertyName("parentaccountid")] public string? ParentAccountId { get; set; }
    }

    public sealed class Contact : Dynamics365EntityBase
    {
        [JsonPropertyName("contactid")] public string? ContactId { get; set; }
        [JsonPropertyName("firstname")] public string? FirstName { get; set; }
        [JsonPropertyName("lastname")] public string? LastName { get; set; }
        [JsonPropertyName("fullname")] public string? FullName { get; set; }
        [JsonPropertyName("emailaddress1")] public string? EmailAddress1 { get; set; }
        [JsonPropertyName("telephone1")] public string? Telephone1 { get; set; }
        [JsonPropertyName("mobilephone")] public string? MobilePhone { get; set; }
        [JsonPropertyName("jobtitle")] public string? JobTitle { get; set; }
        [JsonPropertyName("department")] public string? Department { get; set; }
        [JsonPropertyName("parentcustomerid")] public string? ParentCustomerId { get; set; }
        [JsonPropertyName("ownerid")] public string? OwnerId { get; set; }
        [JsonPropertyName("address1_line1")] public string? Address1Line1 { get; set; }
        [JsonPropertyName("address1_city")] public string? Address1City { get; set; }
        [JsonPropertyName("address1_stateorprovince")] public string? Address1StateOrProvince { get; set; }
        [JsonPropertyName("address1_postalcode")] public string? Address1PostalCode { get; set; }
        [JsonPropertyName("address1_country")] public string? Address1Country { get; set; }
        [JsonPropertyName("birthdate")] public DateTime? BirthDate { get; set; }
        [JsonPropertyName("gendercode")] public int? GenderCode { get; set; }
    }

    public sealed class Lead : Dynamics365EntityBase
    {
        [JsonPropertyName("leadid")] public string? LeadId { get; set; }
        [JsonPropertyName("firstname")] public string? FirstName { get; set; }
        [JsonPropertyName("lastname")] public string? LastName { get; set; }
        [JsonPropertyName("fullname")] public string? FullName { get; set; }
        [JsonPropertyName("companyname")] public string? CompanyName { get; set; }
        [JsonPropertyName("emailaddress1")] public string? EmailAddress1 { get; set; }
        [JsonPropertyName("telephone1")] public string? Telephone1 { get; set; }
        [JsonPropertyName("mobilephone")] public string? MobilePhone { get; set; }
        [JsonPropertyName("jobtitle")] public string? JobTitle { get; set; }
        [JsonPropertyName("websiteurl")] public string? WebsiteUrl { get; set; }
        [JsonPropertyName("industrycode")] public int? IndustryCode { get; set; }
        [JsonPropertyName("revenue")] public decimal? Revenue { get; set; }
        [JsonPropertyName("numberofemployees")] public int? NumberOfEmployees { get; set; }
        [JsonPropertyName("leadqualitycode")] public int? LeadQualityCode { get; set; }
        [JsonPropertyName("leadsourcecode")] public int? LeadSourceCode { get; set; }
        [JsonPropertyName("ownerid")] public string? OwnerId { get; set; }
        [JsonPropertyName("address1_line1")] public string? Address1Line1 { get; set; }
        [JsonPropertyName("address1_city")] public string? Address1City { get; set; }
        [JsonPropertyName("address1_stateorprovince")] public string? Address1StateOrProvince { get; set; }
        [JsonPropertyName("address1_postalcode")] public string? Address1PostalCode { get; set; }
        [JsonPropertyName("address1_country")] public string? Address1Country { get; set; }
    }

    public sealed class Opportunity : Dynamics365EntityBase
    {
        [JsonPropertyName("opportunityid")] public string? OpportunityId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("estimatedvalue")] public decimal? EstimatedValue { get; set; }
        [JsonPropertyName("actualvalue")] public decimal? ActualValue { get; set; }
        [JsonPropertyName("estimatedclosedate")] public DateTime? EstimatedCloseDate { get; set; }
        [JsonPropertyName("actualclosedate")] public DateTime? ActualCloseDate { get; set; }
        [JsonPropertyName("closeprobability")] public int? CloseProbability { get; set; }
        [JsonPropertyName("stepname")] public string? StepName { get; set; }
        [JsonPropertyName("salesstage")] public int? SalesStage { get; set; }
        [JsonPropertyName("salesstagecode")] public int? SalesStageCode { get; set; }
        [JsonPropertyName("customerid")] public string? CustomerId { get; set; }
        [JsonPropertyName("ownerid")] public string? OwnerId { get; set; }
        [JsonPropertyName("originatingleadid")] public string? OriginatingLeadId { get; set; }
        [JsonPropertyName("campaignid")] public string? CampaignId { get; set; }
        [JsonPropertyName("currencyid")] public string? CurrencyId { get; set; }
    }

    public sealed class SystemUser : Dynamics365EntityBase
    {
        [JsonPropertyName("systemuserid")] public string? SystemUserId { get; set; }
        [JsonPropertyName("fullname")] public string? FullName { get; set; }
        [JsonPropertyName("firstname")] public string? FirstName { get; set; }
        [JsonPropertyName("lastname")] public string? LastName { get; set; }
        [JsonPropertyName("domainname")] public string? DomainName { get; set; }
        [JsonPropertyName("internalemailaddress")] public string? InternalEmailAddress { get; set; }
        [JsonPropertyName("businessunitid")] public string? BusinessUnitId { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("personalemailaddress")] public string? PersonalEmailAddress { get; set; }
        [JsonPropertyName("homephone")] public string? HomePhone { get; set; }
        [JsonPropertyName("mobilephone")] public string? MobilePhone { get; set; }
        [JsonPropertyName("address1_line1")] public string? Address1Line1 { get; set; }
        [JsonPropertyName("address1_city")] public string? Address1City { get; set; }
        [JsonPropertyName("address1_stateorprovince")] public string? Address1StateOrProvince { get; set; }
        [JsonPropertyName("address1_postalcode")] public string? Address1PostalCode { get; set; }
        [JsonPropertyName("address1_country")] public string? Address1Country { get; set; }
        [JsonPropertyName("isdisabled")] public bool? IsDisabled { get; set; }
    }

    public sealed class BusinessUnit : Dynamics365EntityBase
    {
        [JsonPropertyName("businessunitid")] public string? BusinessUnitId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("parentbusinessunitid")] public string? ParentBusinessUnitId { get; set; }
        [JsonPropertyName("organizationid")] public string? OrganizationId { get; set; }
        [JsonPropertyName("address1_line1")] public string? Address1Line1 { get; set; }
        [JsonPropertyName("address1_city")] public string? Address1City { get; set; }
        [JsonPropertyName("address1_stateorprovince")] public string? Address1StateOrProvince { get; set; }
        [JsonPropertyName("address1_postalcode")] public string? Address1PostalCode { get; set; }
        [JsonPropertyName("address1_country")] public string? Address1Country { get; set; }
        [JsonPropertyName("emailaddress")] public string? EmailAddress { get; set; }
        [JsonPropertyName("telephone1")] public string? Telephone1 { get; set; }
        [JsonPropertyName("websiteurl")] public string? WebsiteUrl { get; set; }
    }

    public sealed class Team : Dynamics365EntityBase
    {
        [JsonPropertyName("teamid")] public string? TeamId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("businessunitid")] public string? BusinessUnitId { get; set; }
        [JsonPropertyName("administratorid")] public string? AdministratorId { get; set; }
        [JsonPropertyName("teamtype")] public int? TeamType { get; set; }
        [JsonPropertyName("isdefault")] public bool? IsDefault { get; set; }
        [JsonPropertyName("emailaddress")] public string? EmailAddress { get; set; }
    }

    public sealed class Incident : Dynamics365EntityBase
    {
        [JsonPropertyName("incidentid")] public string? IncidentId { get; set; }
        [JsonPropertyName("ticketnumber")] public string? TicketNumber { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("prioritycode")] public int? PriorityCode { get; set; }
        [JsonPropertyName("severitycode")] public int? SeverityCode { get; set; }
        [JsonPropertyName("caseorigincode")] public int? CaseOriginCode { get; set; }
        [JsonPropertyName("casetypecode")] public int? CaseTypeCode { get; set; }
        [JsonPropertyName("customerid")] public string? CustomerId { get; set; }
        [JsonPropertyName("primarycontactid")] public string? PrimaryContactId { get; set; }
        [JsonPropertyName("ownerid")] public string? OwnerId { get; set; }
        [JsonPropertyName("responseby")] public DateTime? ResponseBy { get; set; }
        [JsonPropertyName("resolvebyslastatus")] public int? ResolveBySLAStatus { get; set; }
        [JsonPropertyName("escalatedon")] public DateTime? EscalatedOn { get; set; }
    }

    public sealed class Product : Dynamics365EntityBase
    {
        [JsonPropertyName("productid")] public string? ProductId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("productnumber")] public string? ProductNumber { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("price")] public decimal? Price { get; set; }
        [JsonPropertyName("standardcost")] public decimal? StandardCost { get; set; }
        [JsonPropertyName("currentcost")] public decimal? CurrentCost { get; set; }
        [JsonPropertyName("stockweight")] public decimal? StockWeight { get; set; }
        [JsonPropertyName("stockvolume")] public decimal? StockVolume { get; set; }
        [JsonPropertyName("quantityonhand")] public decimal? QuantityOnHand { get; set; }
        [JsonPropertyName("quantitydecimal")] public int? QuantityDecimal { get; set; }
        [JsonPropertyName("defaultuomid")] public string? DefaultUomId { get; set; }
        [JsonPropertyName("defaultuomscheduleid")] public string? DefaultUomScheduleId { get; set; }
        [JsonPropertyName("vendorid")] public string? VendorId { get; set; }
        [JsonPropertyName("vendorpartnumber")] public string? VendorPartNumber { get; set; }
        [JsonPropertyName("vendorname")] public string? VendorName { get; set; }
        [JsonPropertyName("producttypecode")] public int? ProductTypeCode { get; set; }
        [JsonPropertyName("productstructure")] public int? ProductStructure { get; set; }
    }

    // Response wrapper for Dynamics 365 OData responses
    public class Dynamics365Response<T>
    {
        [JsonPropertyName("@odata.context")] public string? ODataContext { get; set; }
        [JsonPropertyName("@odata.count")] public int? ODataCount { get; set; }
        [JsonPropertyName("@odata.nextLink")] public string? ODataNextLink { get; set; }
        [JsonPropertyName("value")] public List<T>? Value { get; set; } = new();
    }

    // Metadata response
    public class Dynamics365EntityMetadata
    {
        [JsonPropertyName("LogicalName")] public string? LogicalName { get; set; }
        [JsonPropertyName("Caption")] public Dynamics365LocalizedLabel? Caption { get; set; }
        [JsonPropertyName("SchemaName")] public string? SchemaName { get; set; }
        [JsonPropertyName("EntityTypeName")] public string? EntityTypeName { get; set; }
        [JsonPropertyName("PrimaryIdAttribute")] public string? PrimaryIdAttribute { get; set; }
        [JsonPropertyName("PrimaryNameAttribute")] public string? PrimaryNameAttribute { get; set; }
        [JsonPropertyName("Attributes")] public List<Dynamics365AttributeMetadata>? Attributes { get; set; } = new();
        [JsonPropertyName("IsCustomEntity")] public bool IsCustomEntity { get; set; }
        [JsonPropertyName("IsActivity")] public bool IsActivity { get; set; }
        [JsonPropertyName("IsValidForAdvancedFind")] public bool IsValidForAdvancedFind { get; set; }

        [JsonIgnore] public IDataSource? DataSource { get; private set; }
        public Dynamics365EntityMetadata Attach(IDataSource ds) { DataSource = ds; return this; }
    }

    public class Dynamics365LocalizedLabel
    {
        [JsonPropertyName("LocalizedLabels")] public List<Dynamics365Label>? LocalizedLabels { get; set; } = new();
        [JsonPropertyName("UserLocalizedLabel")] public Dynamics365Label? UserLocalizedLabel { get; set; }
    }

    public class Dynamics365Label
    {
        [JsonPropertyName("Label")] public string? Label { get; set; }
        [JsonPropertyName("LanguageCode")] public int LanguageCode { get; set; }
        [JsonPropertyName("IsManaged")] public bool IsManaged { get; set; }
    }

    public class Dynamics365AttributeMetadata
    {
        [JsonPropertyName("LogicalName")] public string? LogicalName { get; set; }
        [JsonPropertyName("SchemaName")] public string? SchemaName { get; set; }
        [JsonPropertyName("Caption")] public Dynamics365LocalizedLabel? Caption { get; set; }
        [JsonPropertyName("AttributeType")] public string? AttributeType { get; set; }
        [JsonPropertyName("AttributeTypeName")] public Dynamics365AttributeTypeName? AttributeTypeName { get; set; }
        [JsonPropertyName("IsPrimaryId")] public bool IsPrimaryId { get; set; }
        [JsonPropertyName("IsPrimaryName")] public bool IsPrimaryName { get; set; }
        [JsonPropertyName("IsValidForCreate")] public bool IsValidForCreate { get; set; }
        [JsonPropertyName("IsValidForRead")] public bool IsValidForRead { get; set; }
        [JsonPropertyName("IsValidForUpdate")] public bool IsValidForUpdate { get; set; }
        [JsonPropertyName("RequiredLevel")] public Dynamics365AttributeRequiredLevel? RequiredLevel { get; set; }
        [JsonPropertyName("IsCustomAttribute")] public bool IsCustomAttribute { get; set; }
    }

    public class Dynamics365AttributeTypeName
    {
        [JsonPropertyName("Value")] public string? Value { get; set; }
    }

    public class Dynamics365AttributeRequiredLevel
    {
        [JsonPropertyName("Value")] public string? Value { get; set; }
    }
}