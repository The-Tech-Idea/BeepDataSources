using System.Text.Json.Serialization;

namespace TheTechIdea.Beep.Connectors.Typeform.Models
{
    // Typeform API Models
    public class TypeformForm
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("theme")]
        public TypeformTheme Theme { get; set; }

        [JsonPropertyName("workspace")]
        public TypeformWorkspace Workspace { get; set; }

        [JsonPropertyName("settings")]
        public TypeformSettings Settings { get; set; }

        [JsonPropertyName("thankyou_screens")]
        public List<TypeformThankYouScreen> ThankYouScreens { get; set; }

        [JsonPropertyName("fields")]
        public List<TypeformField> Fields { get; set; }

        [JsonPropertyName("logic")]
        public List<TypeformLogic> Logic { get; set; }

        [JsonPropertyName("_links")]
        public TypeformLinks Links { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("last_updated_at")]
        public DateTime LastUpdatedAt { get; set; }
    }

    public class TypeformTheme
    {
        [JsonPropertyName("href")]
        public string Href { get; set; }
    }

    public class TypeformWorkspace
    {
        [JsonPropertyName("href")]
        public string Href { get; set; }
    }

    public class TypeformSettings
    {
        [JsonPropertyName("is_public")]
        public bool IsPublic { get; set; }

        [JsonPropertyName("is_trial")]
        public bool IsTrial { get; set; }

        [JsonPropertyName("language")]
        public string Language { get; set; }

        [JsonPropertyName("progress_bar")]
        public string ProgressBar { get; set; }

        [JsonPropertyName("show_progress_bar")]
        public bool ShowProgressBar { get; set; }

        [JsonPropertyName("show_typeform_branding")]
        public bool ShowTypeformBranding { get; set; }

        [JsonPropertyName("meta")]
        public TypeformMeta Meta { get; set; }
    }

    public class TypeformMeta
    {
        [JsonPropertyName("allow_indexing")]
        public bool AllowIndexing { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("image")]
        public TypeformImage Image { get; set; }
    }

    public class TypeformImage
    {
        [JsonPropertyName("href")]
        public string Href { get; set; }
    }

    public class TypeformThankYouScreen
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("properties")]
        public TypeformThankYouProperties Properties { get; set; }
    }

    public class TypeformThankYouProperties
    {
        [JsonPropertyName("show_button")]
        public bool ShowButton { get; set; }

        [JsonPropertyName("button_text")]
        public string ButtonText { get; set; }

        [JsonPropertyName("button_mode")]
        public string ButtonMode { get; set; }
    }

    public class TypeformField
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("properties")]
        public TypeformFieldProperties Properties { get; set; }

        [JsonPropertyName("validations")]
        public TypeformValidations Validations { get; set; }
    }

    public class TypeformFieldProperties
    {
        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("choices")]
        public List<TypeformChoice> Choices { get; set; }

        [JsonPropertyName("allow_multiple_selection")]
        public bool AllowMultipleSelection { get; set; }

        [JsonPropertyName("allow_other_choice")]
        public bool AllowOtherChoice { get; set; }

        [JsonPropertyName("randomize")]
        public bool Randomize { get; set; }

        [JsonPropertyName("required")]
        public bool Required { get; set; }

        [JsonPropertyName("shape")]
        public string Shape { get; set; }
    }

    public class TypeformChoice
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("label")]
        public string Label { get; set; }
    }

    public class TypeformValidations
    {
        [JsonPropertyName("required")]
        public bool Required { get; set; }

        [JsonPropertyName("max_length")]
        public int MaxLength { get; set; }

        [JsonPropertyName("min_value")]
        public int MinValue { get; set; }

        [JsonPropertyName("max_value")]
        public int MaxValue { get; set; }
    }

    public class TypeformLogic
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("actions")]
        public List<TypeformAction> Actions { get; set; }

        [JsonPropertyName("conditions")]
        public List<TypeformCondition> Conditions { get; set; }
    }

    public class TypeformAction
    {
        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("details")]
        public string Details { get; set; }

        [JsonPropertyName("to")]
        public TypeformTo To { get; set; }

        [JsonPropertyName("condition")]
        public string Condition { get; set; }
    }

    public class TypeformTo
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }
    }

    public class TypeformCondition
    {
        [JsonPropertyName("op")]
        public string Op { get; set; }

        [JsonPropertyName("vars")]
        public List<TypeformVariable> Vars { get; set; }
    }

    public class TypeformVariable
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("value")]
        public object Value { get; set; }
    }

    public class TypeformLinks
    {
        [JsonPropertyName("display")]
        public string Display { get; set; }

        [JsonPropertyName("responses")]
        public string Responses { get; set; }
    }

    // Response models
    public class TypeformFormsResponse
    {
        [JsonPropertyName("total_items")]
        public int TotalItems { get; set; }

        [JsonPropertyName("page_count")]
        public int PageCount { get; set; }

        [JsonPropertyName("items")]
        public List<TypeformForm> Items { get; set; }
    }

    public class TypeformResponse
    {
        [JsonPropertyName("response_id")]
        public string ResponseId { get; set; }

        [JsonPropertyName("form_id")]
        public string FormId { get; set; }

        [JsonPropertyName("token")]
        public string Token { get; set; }

        [JsonPropertyName("submitted_at")]
        public DateTime SubmittedAt { get; set; }

        [JsonPropertyName("landed_at")]
        public DateTime LandedAt { get; set; }

        [JsonPropertyName("calculated")]
        public TypeformCalculated Calculated { get; set; }

        [JsonPropertyName("variables")]
        public List<TypeformVariable> Variables { get; set; }

        [JsonPropertyName("hidden")]
        public Dictionary<string, object> Hidden { get; set; }

        [JsonPropertyName("definition")]
        public TypeformDefinition Definition { get; set; }

        [JsonPropertyName("answers")]
        public List<TypeformAnswer> Answers { get; set; }

        [JsonPropertyName("ending")]
        public TypeformEnding Ending { get; set; }
    }

    public class TypeformCalculated
    {
        [JsonPropertyName("score")]
        public int Score { get; set; }
    }

    public class TypeformDefinition
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("fields")]
        public List<TypeformField> Fields { get; set; }

        [JsonPropertyName("endings")]
        public List<TypeformEnding> Endings { get; set; }

        [JsonPropertyName("logic")]
        public List<TypeformLogic> Logic { get; set; }
    }

    public class TypeformAnswer
    {
        [JsonPropertyName("field")]
        public TypeformFieldRef Field { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("choice")]
        public TypeformChoice Choice { get; set; }

        [JsonPropertyName("choices")]
        public TypeformChoices Choices { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("number")]
        public int Number { get; set; }

        [JsonPropertyName("boolean")]
        public bool Boolean { get; set; }

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("file_url")]
        public string FileUrl { get; set; }

        [JsonPropertyName("payment")]
        public TypeformPayment Payment { get; set; }
    }

    public class TypeformFieldRef
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }
    }

    public class TypeformChoices
    {
        [JsonPropertyName("labels")]
        public List<string> Labels { get; set; }

        [JsonPropertyName("other")]
        public string Other { get; set; }
    }

    public class TypeformPayment
    {
        [JsonPropertyName("amount")]
        public string Amount { get; set; }

        [JsonPropertyName("last4")]
        public string Last4 { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class TypeformEnding
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("properties")]
        public TypeformEndingProperties Properties { get; set; }
    }

    public class TypeformEndingProperties
    {
        [JsonPropertyName("button_text")]
        public string ButtonText { get; set; }

        [JsonPropertyName("show_button")]
        public bool ShowButton { get; set; }

        [JsonPropertyName("button_mode")]
        public string ButtonMode { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }
    }

    public class TypeformResponsesResponse
    {
        [JsonPropertyName("total_items")]
        public int TotalItems { get; set; }

        [JsonPropertyName("page_count")]
        public int PageCount { get; set; }

        [JsonPropertyName("items")]
        public List<TypeformResponse> Items { get; set; }
    }
}