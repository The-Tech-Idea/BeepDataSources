using System;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.InsightlyDataSource.Models
{
    /// <summary>
    /// Base class for all Insightly entities
    /// </summary>
    public abstract class InsightlyEntityBase
    {
        [JsonIgnore]
        public IDataSource DataSource { get; set; }

        public void Attach(IDataSource dataSource)
        {
            DataSource = dataSource;
        }
    }

    /// <summary>
    /// Insightly Contact entity
    /// </summary>
    public sealed class Contact : InsightlyEntityBase
    {
        [JsonPropertyName("CONTACT_ID")]
        public int? CONTACT_ID { get; set; }

        [JsonPropertyName("FIRST_NAME")]
        public string? FIRST_NAME { get; set; }

        [JsonPropertyName("LAST_NAME")]
        public string? LAST_NAME { get; set; }

        [JsonPropertyName("SALUTATION")]
        public string? SALUTATION { get; set; }

        [JsonPropertyName("DATE_CREATED_UTC")]
        public DateTime? DATE_CREATED_UTC { get; set; }

        [JsonPropertyName("DATE_UPDATED_UTC")]
        public DateTime? DATE_UPDATED_UTC { get; set; }

        [JsonPropertyName("EMAIL_ADDRESS")]
        public string? EMAIL_ADDRESS { get; set; }

        [JsonPropertyName("PHONE")]
        public string? PHONE { get; set; }

        [JsonPropertyName("MOBILE")]
        public string? MOBILE { get; set; }

        [JsonPropertyName("ORGANISATION_ID")]
        public int? ORGANISATION_ID { get; set; }
    }

    /// <summary>
    /// Insightly Organisation entity
    /// </summary>
    public sealed class Organisation : InsightlyEntityBase
    {
        [JsonPropertyName("ORGANISATION_ID")]
        public int? ORGANISATION_ID { get; set; }

        [JsonPropertyName("ORGANISATION_NAME")]
        public string? ORGANISATION_NAME { get; set; }

        [JsonPropertyName("DATE_CREATED_UTC")]
        public DateTime? DATE_CREATED_UTC { get; set; }

        [JsonPropertyName("DATE_UPDATED_UTC")]
        public DateTime? DATE_UPDATED_UTC { get; set; }

        [JsonPropertyName("PHONE")]
        public string? PHONE { get; set; }

        [JsonPropertyName("FAX")]
        public string? FAX { get; set; }

        [JsonPropertyName("WEBSITE")]
        public string? WEBSITE { get; set; }

        [JsonPropertyName("ADDRESS_BILLING_STREET")]
        public string? ADDRESS_BILLING_STREET { get; set; }

        [JsonPropertyName("ADDRESS_BILLING_CITY")]
        public string? ADDRESS_BILLING_CITY { get; set; }

        [JsonPropertyName("ADDRESS_BILLING_STATE")]
        public string? ADDRESS_BILLING_STATE { get; set; }

        [JsonPropertyName("ADDRESS_BILLING_COUNTRY")]
        public string? ADDRESS_BILLING_COUNTRY { get; set; }
    }

    /// <summary>
    /// Insightly Opportunity entity
    /// </summary>
    public sealed class Opportunity : InsightlyEntityBase
    {
        [JsonPropertyName("OPPORTUNITY_ID")]
        public int? OPPORTUNITY_ID { get; set; }

        [JsonPropertyName("OPPORTUNITY_NAME")]
        public string? OPPORTUNITY_NAME { get; set; }

        [JsonPropertyName("OPPORTUNITY_DETAILS")]
        public string? OPPORTUNITY_DETAILS { get; set; }

        [JsonPropertyName("PROBABILITY")]
        public decimal? PROBABILITY { get; set; }

        [JsonPropertyName("BID_AMOUNT")]
        public decimal? BID_AMOUNT { get; set; }

        [JsonPropertyName("BID_CURRENCY")]
        public string? BID_CURRENCY { get; set; }

        [JsonPropertyName("DATE_CREATED_UTC")]
        public DateTime? DATE_CREATED_UTC { get; set; }

        [JsonPropertyName("DATE_UPDATED_UTC")]
        public DateTime? DATE_UPDATED_UTC { get; set; }

        [JsonPropertyName("ORGANISATION_ID")]
        public int? ORGANISATION_ID { get; set; }

        [JsonPropertyName("CONTACT_ID")]
        public int? CONTACT_ID { get; set; }
    }

    /// <summary>
    /// Insightly Lead entity
    /// </summary>
    public sealed class Lead : InsightlyEntityBase
    {
        [JsonPropertyName("LEAD_ID")]
        public int? LEAD_ID { get; set; }

        [JsonPropertyName("FIRST_NAME")]
        public string? FIRST_NAME { get; set; }

        [JsonPropertyName("LAST_NAME")]
        public string? LAST_NAME { get; set; }

        [JsonPropertyName("ORGANISATION_NAME")]
        public string? ORGANISATION_NAME { get; set; }

        [JsonPropertyName("PHONE_NUMBER")]
        public string? PHONE_NUMBER { get; set; }

        [JsonPropertyName("EMAIL_ADDRESS")]
        public string? EMAIL_ADDRESS { get; set; }

        [JsonPropertyName("DATE_CREATED_UTC")]
        public DateTime? DATE_CREATED_UTC { get; set; }

        [JsonPropertyName("DATE_UPDATED_UTC")]
        public DateTime? DATE_UPDATED_UTC { get; set; }

        [JsonPropertyName("LEAD_STATUS_ID")]
        public int? LEAD_STATUS_ID { get; set; }

        [JsonPropertyName("LEAD_SOURCE_ID")]
        public int? LEAD_SOURCE_ID { get; set; }
    }
}