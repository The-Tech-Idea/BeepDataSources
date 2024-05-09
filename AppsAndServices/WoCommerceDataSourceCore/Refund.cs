using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace WooCommerceDataSourceCore
{
    public class Refund
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("date_created")]
        public DateTime DateCreated { get; set; }

        [JsonProperty("date_created_gmt")]
        public DateTime DateCreatedGmt { get; set; }

        [JsonProperty("amount")]
        public string Amount { get; set; }

        [JsonProperty("reason")]
        public string Reason { get; set; }

        [JsonProperty("refunded_by")]
        public int RefundedBy { get; set; }

        [JsonProperty("refunded_payment")]
        public bool RefundedPayment { get; set; }

        [JsonProperty("meta_data")]
        public List<MetaData> MetaData { get; set; } = new List<MetaData>();

        [JsonProperty("line_items")]
        public List<LineItem> LineItems { get; set; } = new List<LineItem>();

        [JsonProperty("api_refund")]
        public bool ApiRefund { get; set; }

        [JsonProperty("api_restock")]
        public bool ApiRestock { get; set; }
    }
}
