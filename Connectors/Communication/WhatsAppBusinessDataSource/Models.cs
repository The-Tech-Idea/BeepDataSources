using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Communication.WhatsAppBusiness.Models
{
    public abstract class WhatsAppBusinessEntityBase
    {
        [JsonIgnore] public IDataSource? DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : WhatsAppBusinessEntityBase { DataSource = ds; return (T)this; }
    }

    // Phone number
    public sealed class WabaPhoneNumber : WhatsAppBusinessEntityBase
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("display_phone_number")] public string? DisplayPhoneNumber { get; set; }
        [JsonPropertyName("verified_name")] public string? VerifiedName { get; set; }
        [JsonPropertyName("quality_rating")] public string? QualityRating { get; set; }
        [JsonPropertyName("code_verification_status")] public string? CodeVerificationStatus { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
    }

    // Message template
    public sealed class WabaMessageTemplate : WhatsAppBusinessEntityBase
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("language")] public string? Language { get; set; } // e.g., en_US
        [JsonPropertyName("category")] public string? Category { get; set; } // MARKETING, UTILITY, AUTHENTICATION
        [JsonPropertyName("status")] public string? Status { get; set; }   // APPROVED, REJECTED, PENDING
        [JsonPropertyName("components")] public List<WabaTemplateComponent>? Components { get; set; }
    }

    public sealed class WabaTemplateComponent
    {
        [JsonPropertyName("type")] public string? Type { get; set; } // HEADER, BODY, FOOTER, BUTTONS
        [JsonPropertyName("format")] public string? Format { get; set; } // TEXT, IMAGE, DOCUMENT, VIDEO, LOCATION
        [JsonPropertyName("text")] public string? Text { get; set; }
        [JsonPropertyName("buttons")] public List<WabaTemplateButton>? Buttons { get; set; }
        [JsonPropertyName("example")] public object? Example { get; set; } // shape varies
    }

    public sealed class WabaTemplateButton
    {
        [JsonPropertyName("type")] public string? Type { get; set; }  // QUICK_REPLY, URL, PHONE_NUMBER, OTP
        [JsonPropertyName("text")] public string? Text { get; set; }
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("phone_number")] public string? PhoneNumber { get; set; }
    }

    // Subscribed app
    public sealed class WabaSubscribedApp : WhatsAppBusinessEntityBase
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("scopes")] public List<string>? Scopes { get; set; }
    }

    // Business profile (list endpoint returns data array)
    public sealed class WabaBusinessProfile : WhatsAppBusinessEntityBase
    {
        [JsonPropertyName("about")] public string? About { get; set; }
        [JsonPropertyName("address")] public string? Address { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("profile_picture_url")] public string? ProfilePictureUrl { get; set; }
        [JsonPropertyName("vertical")] public string? Vertical { get; set; }
        [JsonPropertyName("websites")] public List<string>? Websites { get; set; }
    }

    // Media (single-object lookup)
    public sealed class WabaMedia : WhatsAppBusinessEntityBase
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("mime_type")] public string? MimeType { get; set; }
        [JsonPropertyName("sha256")] public string? Sha256 { get; set; }
        [JsonPropertyName("file_size")] public long? FileSize { get; set; }
        [JsonPropertyName("url")] public string? Url { get; set; } // only when requested with proper permissions
    }

    // Registry
    public static class WabaEntityRegistry
    {
        public static readonly Dictionary<string, Type> Types = new(StringComparer.OrdinalIgnoreCase)
        {
            ["waba.phone_numbers"] = typeof(WabaPhoneNumber),
            ["waba.message_templates"] = typeof(WabaMessageTemplate),
            ["waba.subscribed_apps"] = typeof(WabaSubscribedApp),
            ["waba.business_profiles"] = typeof(WabaBusinessProfile),
            ["media.by_id"] = typeof(WabaMedia)
        };

        public static IReadOnlyList<string> Names => new List<string>(Types.Keys);
        public static Type? Resolve(string entityName) =>
            entityName != null && Types.TryGetValue(entityName, out var t) ? t : null;
    }
}
