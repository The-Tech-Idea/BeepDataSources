using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WooCommerceDataSourceCore
{
    public class OrderNote
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("author")]
        public string Author { get; set; }

        [JsonProperty("date_created")]
        public DateTime DateCreated { get; set; }

        [JsonProperty("date_created_gmt")]
        public DateTime DateCreatedGmt { get; set; }

        [JsonProperty("note")]
        public string Note { get; set; }

        [JsonProperty("customer_note")]
        public bool CustomerNote { get; set; }

        [JsonProperty("added_by_user")]
        public bool AddedByUser { get; set; }
    }
}
