using System.Text.Json.Serialization;

namespace TheTechIdea.Beep.Connectors.Jotform.Models
{
    // Jotform API Models
    public class JotformForm
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("height")]
        public string Height { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("new")]
        public bool New { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }
    }

    public class JotformSubmission
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("form_id")]
        public string FormId { get; set; }

        [JsonPropertyName("ip")]
        public string Ip { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("new")]
        public bool New { get; set; }

        [JsonPropertyName("flag")]
        public bool Flag { get; set; }

        [JsonPropertyName("notes")]
        public string Notes { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("answers")]
        public Dictionary<string, JotformAnswer> Answers { get; set; }
    }

    public class JotformAnswer
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("order")]
        public string Order { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("answer")]
        public object Answer { get; set; }

        [JsonPropertyName("prettyFormat")]
        public string PrettyFormat { get; set; }
    }

    public class JotformQuestion
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("order")]
        public string Order { get; set; }

        [JsonPropertyName("qid")]
        public string Qid { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("required")]
        public string Required { get; set; }
    }

    public class JotformFormDetail
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("height")]
        public string Height { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("new")]
        public bool New { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("questions")]
        public Dictionary<string, JotformQuestion> Questions { get; set; }

        [JsonPropertyName("properties")]
        public JotformProperties Properties { get; set; }
    }

    public class JotformProperties
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("active")]
        public string Active { get; set; }

        [JsonPropertyName("splash")]
        public string Splash { get; set; }

        [JsonPropertyName("thankurl")]
        public string ThankUrl { get; set; }

        [JsonPropertyName("formWidth")]
        public string FormWidth { get; set; }

        [JsonPropertyName("labelWidth")]
        public string LabelWidth { get; set; }

        [JsonPropertyName("styles")]
        public JotformStyles Styles { get; set; }
    }

    public class JotformStyles
    {
        [JsonPropertyName("background")]
        public string Background { get; set; }

        [JsonPropertyName("color")]
        public string Color { get; set; }

        [JsonPropertyName("font")]
        public string Font { get; set; }

        [JsonPropertyName("fontSize")]
        public string FontSize { get; set; }

        [JsonPropertyName("theme")]
        public string Theme { get; set; }
    }

    // Response models
    public class JotformFormsResponse
    {
        [JsonPropertyName("resultSet")]
        public JotformResultSet ResultSet { get; set; }

        [JsonPropertyName("content")]
        public List<JotformForm> Content { get; set; }

        [JsonPropertyName("responseCode")]
        public int ResponseCode { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("limit-left")]
        public int LimitLeft { get; set; }
    }

    public class JotformResultSet
    {
        [JsonPropertyName("offset")]
        public int Offset { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

    public class JotformSubmissionsResponse
    {
        [JsonPropertyName("resultSet")]
        public JotformResultSet ResultSet { get; set; }

        [JsonPropertyName("content")]
        public List<JotformSubmission> Content { get; set; }

        [JsonPropertyName("responseCode")]
        public int ResponseCode { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("limit-left")]
        public int LimitLeft { get; set; }
    }

    public class JotformFormResponse
    {
        [JsonPropertyName("content")]
        public JotformFormDetail Content { get; set; }

        [JsonPropertyName("responseCode")]
        public int ResponseCode { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("limit-left")]
        public int LimitLeft { get; set; }
    }

    public class JotformSubmissionResponse
    {
        [JsonPropertyName("content")]
        public JotformSubmission Content { get; set; }

        [JsonPropertyName("responseCode")]
        public int ResponseCode { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("limit-left")]
        public int LimitLeft { get; set; }
    }
}