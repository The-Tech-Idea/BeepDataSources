// File: Connectors/CRM/SugarCRMDataSource/Models.cs
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.SugarCRM.Models
{
    // Common base for SugarCRM entities
    public abstract class SugarCRMEntityBase
    {
        [JsonIgnore]
        public IDataSource? DataSource { get; set; }

        public void Attach<T>(IDataSource dataSource) where T : SugarCRMEntityBase
        {
            DataSource = dataSource;
        }
    }

    // SugarCRM Contact entity
    public sealed class Contact : SugarCRMEntityBase
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string? LastName { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("phone_work")]
        public string? PhoneWork { get; set; }

        [JsonPropertyName("phone_mobile")]
        public string? PhoneMobile { get; set; }

        [JsonPropertyName("account_id")]
        public string? AccountId { get; set; }

        [JsonPropertyName("account_name")]
        public string? AccountName { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("department")]
        public string? Department { get; set; }

        [JsonPropertyName("date_entered")]
        public DateTime? DateEntered { get; set; }

        [JsonPropertyName("date_modified")]
        public DateTime? DateModified { get; set; }
    }

    // SugarCRM Account entity
    public sealed class Account : SugarCRMEntityBase
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("website")]
        public string? Website { get; set; }

        [JsonPropertyName("phone_office")]
        public string? PhoneOffice { get; set; }

        [JsonPropertyName("phone_fax")]
        public string? PhoneFax { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("billing_address_street")]
        public string? BillingAddressStreet { get; set; }

        [JsonPropertyName("billing_address_city")]
        public string? BillingAddressCity { get; set; }

        [JsonPropertyName("billing_address_state")]
        public string? BillingAddressState { get; set; }

        [JsonPropertyName("billing_address_postalcode")]
        public string? BillingAddressPostalCode { get; set; }

        [JsonPropertyName("billing_address_country")]
        public string? BillingAddressCountry { get; set; }

        [JsonPropertyName("industry")]
        public string? Industry { get; set; }

        [JsonPropertyName("annual_revenue")]
        public string? AnnualRevenue { get; set; }

        [JsonPropertyName("date_entered")]
        public DateTime? DateEntered { get; set; }

        [JsonPropertyName("date_modified")]
        public DateTime? DateModified { get; set; }
    }

    // SugarCRM Lead entity
    public sealed class Lead : SugarCRMEntityBase
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string? LastName { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("phone_work")]
        public string? PhoneWork { get; set; }

        [JsonPropertyName("phone_mobile")]
        public string? PhoneMobile { get; set; }

        [JsonPropertyName("account_name")]
        public string? AccountName { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("department")]
        public string? Department { get; set; }

        [JsonPropertyName("website")]
        public string? Website { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("lead_source")]
        public string? LeadSource { get; set; }

        [JsonPropertyName("date_entered")]
        public DateTime? DateEntered { get; set; }

        [JsonPropertyName("date_modified")]
        public DateTime? DateModified { get; set; }
    }

    // SugarCRM Opportunity entity
    public sealed class Opportunity : SugarCRMEntityBase
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("amount")]
        public decimal? Amount { get; set; }

        [JsonPropertyName("amount_usdollar")]
        public decimal? AmountUSDollar { get; set; }

        [JsonPropertyName("currency_id")]
        public string? CurrencyId { get; set; }

        [JsonPropertyName("date_closed")]
        public DateTime? DateClosed { get; set; }

        [JsonPropertyName("sales_stage")]
        public string? SalesStage { get; set; }

        [JsonPropertyName("probability")]
        public decimal? Probability { get; set; }

        [JsonPropertyName("account_id")]
        public string? AccountId { get; set; }

        [JsonPropertyName("account_name")]
        public string? AccountName { get; set; }

        [JsonPropertyName("contact_id")]
        public string? ContactId { get; set; }

        [JsonPropertyName("contact_name")]
        public string? ContactName { get; set; }

        [JsonPropertyName("lead_source")]
        public string? LeadSource { get; set; }

        [JsonPropertyName("date_entered")]
        public DateTime? DateEntered { get; set; }

        [JsonPropertyName("date_modified")]
        public DateTime? DateModified { get; set; }
    }
}