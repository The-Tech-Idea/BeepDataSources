using System.Text.Json.Serialization;

namespace TheTechIdea.Beep.Connectors.ClickSend.Models
{
    // ClickSend API Models
    public class ClickSendSMS
    {
        [JsonPropertyName("to")]
        public string To { get; set; }

        [JsonPropertyName("from")]
        public string From { get; set; }

        [JsonPropertyName("body")]
        public string Body { get; set; }

        [JsonPropertyName("schedule")]
        public long Schedule { get; set; }

        [JsonPropertyName("custom_string")]
        public string CustomString { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }

        [JsonPropertyName("source")]
        public string Source { get; set; }
    }

    public class ClickSendSMSResponse
    {
        [JsonPropertyName("http_code")]
        public int HttpCode { get; set; }

        [JsonPropertyName("response_code")]
        public string ResponseCode { get; set; }

        [JsonPropertyName("response_msg")]
        public string ResponseMsg { get; set; }

        [JsonPropertyName("data")]
        public ClickSendSMSData Data { get; set; }
    }

    public class ClickSendSMSData
    {
        [JsonPropertyName("total_price")]
        public decimal TotalPrice { get; set; }

        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }

        [JsonPropertyName("queued_count")]
        public int QueuedCount { get; set; }

        [JsonPropertyName("messages")]
        public List<ClickSendSMSMessage> Messages { get; set; }
    }

    public class ClickSendSMSMessage
    {
        [JsonPropertyName("direction")]
        public string Direction { get; set; }

        [JsonPropertyName("date")]
        public long Date { get; set; }

        [JsonPropertyName("to")]
        public string To { get; set; }

        [JsonPropertyName("from")]
        public string From { get; set; }

        [JsonPropertyName("body")]
        public string Body { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("message_id")]
        public string MessageId { get; set; }

        [JsonPropertyName("message_parts")]
        public int MessageParts { get; set; }

        [JsonPropertyName("message_price")]
        public decimal MessagePrice { get; set; }

        [JsonPropertyName("custom_string")]
        public string CustomString { get; set; }

        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("subaccount_id")]
        public int SubaccountId { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }

        [JsonPropertyName("carrier")]
        public string Carrier { get; set; }

        [JsonPropertyName("list_id")]
        public int ListId { get; set; }
    }

    public class ClickSendContact
    {
        [JsonPropertyName("phone_number")]
        public string PhoneNumber { get; set; }

        [JsonPropertyName("first_name")]
        public string FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string LastName { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("organization_name")]
        public string OrganizationName { get; set; }

        [JsonPropertyName("custom_1")]
        public string Custom1 { get; set; }

        [JsonPropertyName("custom_2")]
        public string Custom2 { get; set; }

        [JsonPropertyName("custom_3")]
        public string Custom3 { get; set; }

        [JsonPropertyName("custom_4")]
        public string Custom4 { get; set; }
    }

    public class ClickSendContactList
    {
        [JsonPropertyName("list_name")]
        public string ListName { get; set; }

        [JsonPropertyName("list_id")]
        public int ListId { get; set; }
    }

    public class ClickSendContactResponse
    {
        [JsonPropertyName("http_code")]
        public int HttpCode { get; set; }

        [JsonPropertyName("response_code")]
        public string ResponseCode { get; set; }

        [JsonPropertyName("response_msg")]
        public string ResponseMsg { get; set; }

        [JsonPropertyName("data")]
        public ClickSendContactData Data { get; set; }
    }

    public class ClickSendContactData
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("per_page")]
        public int PerPage { get; set; }

        [JsonPropertyName("current_page")]
        public int CurrentPage { get; set; }

        [JsonPropertyName("last_page")]
        public int LastPage { get; set; }

        [JsonPropertyName("next_page_url")]
        public string NextPageUrl { get; set; }

        [JsonPropertyName("prev_page_url")]
        public string PrevPageUrl { get; set; }

        [JsonPropertyName("from")]
        public int From { get; set; }

        [JsonPropertyName("to")]
        public int To { get; set; }

        [JsonPropertyName("data")]
        public List<ClickSendContact> Data { get; set; }
    }

    public class ClickSendAccount
    {
        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("phone_number")]
        public string PhoneNumber { get; set; }

        [JsonPropertyName("first_name")]
        public string FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string LastName { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }

        [JsonPropertyName("timezone")]
        public string Timezone { get; set; }

        [JsonPropertyName("balance")]
        public decimal Balance { get; set; }

        [JsonPropertyName("account_type")]
        public string AccountType { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }
    }

    public class ClickSendAccountResponse
    {
        [JsonPropertyName("http_code")]
        public int HttpCode { get; set; }

        [JsonPropertyName("response_code")]
        public string ResponseCode { get; set; }

        [JsonPropertyName("response_msg")]
        public string ResponseMsg { get; set; }

        [JsonPropertyName("data")]
        public ClickSendAccount Data { get; set; }
    }

    public class ClickSendDeliveryReceipt
    {
        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }

        [JsonPropertyName("message_id")]
        public string MessageId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("status_text")]
        public string StatusText { get; set; }

        [JsonPropertyName("error_code")]
        public string ErrorCode { get; set; }

        [JsonPropertyName("error_text")]
        public string ErrorText { get; set; }

        [JsonPropertyName("custom_string")]
        public string CustomString { get; set; }
    }
}