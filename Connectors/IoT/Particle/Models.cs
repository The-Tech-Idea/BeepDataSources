using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Particle
{
    // Base class for Particle entities
    public class ParticleBaseEntity
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        public T Attach<T>(T entity) where T : ParticleBaseEntity
        {
            return entity;
        }
    }

    // Device entity
    public class Device : ParticleBaseEntity
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("device_id")]
        public string? DeviceId { get; set; }

        [JsonPropertyName("product_id")]
        public int? ProductId { get; set; }

        [JsonPropertyName("platform_id")]
        public int? PlatformId { get; set; }

        [JsonPropertyName("connected")]
        public bool? Connected { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("last_handshake_at")]
        public DateTime? LastHandshakeAt { get; set; }

        [JsonPropertyName("last_heard")]
        public DateTime? LastHeard { get; set; }

        [JsonPropertyName("serial_number")]
        public string? SerialNumber { get; set; }

        [JsonPropertyName("imei")]
        public string? Imei { get; set; }

        [JsonPropertyName("iccid")]
        public string? Iccid { get; set; }

        [JsonPropertyName("current_build_target")]
        public string? CurrentBuildTarget { get; set; }

        [JsonPropertyName("default_build_target")]
        public string? DefaultBuildTarget { get; set; }

        [JsonPropertyName("system_firmware_version")]
        public string? SystemFirmwareVersion { get; set; }

        [JsonPropertyName("firmware_updates_enabled")]
        public bool? FirmwareUpdatesEnabled { get; set; }

        [JsonPropertyName("firmware_updates_forced")]
        public bool? FirmwareUpdatesForced { get; set; }

        [JsonPropertyName("variables")]
        public Dictionary<string, DeviceVariable>? Variables { get; set; }

        [JsonPropertyName("functions")]
        public List<string>? Functions { get; set; }

        [JsonPropertyName("groups")]
        public List<string>? Groups { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("cellular")]
        public bool? Cellular { get; set; }

        [JsonPropertyName("network")]
        public DeviceNetwork? Network { get; set; }
    }

    // Device Variable
    public class DeviceVariable
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("value")]
        public object? Value { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    // Device Network
    public class DeviceNetwork
    {
        [JsonPropertyName("connected")]
        public bool? Connected { get; set; }

        [JsonPropertyName("last_handshake_at")]
        public DateTime? LastHandshakeAt { get; set; }

        [JsonPropertyName("signal_strength")]
        public int? SignalStrength { get; set; }

        [JsonPropertyName("signal_strength_units")]
        public string? SignalStrengthUnits { get; set; }

        [JsonPropertyName("cellular")]
        public bool? Cellular { get; set; }

        [JsonPropertyName("carrier")]
        public string? Carrier { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("ip_address")]
        public string? IpAddress { get; set; }
    }

    // Event entity
    public class Event : ParticleBaseEntity
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("data")]
        public string? Data { get; set; }

        [JsonPropertyName("ttl")]
        public int? Ttl { get; set; }

        [JsonPropertyName("published_at")]
        public DateTime? PublishedAt { get; set; }

        [JsonPropertyName("device_id")]
        public string? DeviceId { get; set; }

        [JsonPropertyName("product_id")]
        public int? ProductId { get; set; }

        [JsonPropertyName("user_id")]
        public string? UserId { get; set; }

        [JsonPropertyName("version")]
        public int? Version { get; set; }

        [JsonPropertyName("public")]
        public bool? Public { get; set; }

        [JsonPropertyName("coreid")]
        public string? CoreId { get; set; }
    }

    // Product entity
    public class Product : ParticleBaseEntity
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("slug")]
        public string? Slug { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("platform_id")]
        public int? PlatformId { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("hardware_version")]
        public string? HardwareVersion { get; set; }

        [JsonPropertyName("organization")]
        public string? Organization { get; set; }

        [JsonPropertyName("groups")]
        public List<string>? Groups { get; set; }

        [JsonPropertyName("device_count")]
        public int? DeviceCount { get; set; }

        [JsonPropertyName("subscription_id")]
        public string? SubscriptionId { get; set; }

        [JsonPropertyName("config")]
        public ProductConfig? Config { get; set; }

        [JsonPropertyName("settings")]
        public ProductSettings? Settings { get; set; }
    }

    // Product Configuration
    public class ProductConfig
    {
        [JsonPropertyName("webhooks")]
        public List<Webhook>? Webhooks { get; set; }

        [JsonPropertyName("integrations")]
        public List<Integration>? Integrations { get; set; }

        [JsonPropertyName("firmware")]
        public ProductFirmware? Firmware { get; set; }
    }

    // Product Settings
    public class ProductSettings
    {
        [JsonPropertyName("firmware_updates_enabled")]
        public bool? FirmwareUpdatesEnabled { get; set; }

        [JsonPropertyName("firmware_updates_forced")]
        public bool? FirmwareUpdatesForced { get; set; }

        [JsonPropertyName("device_notes")]
        public bool? DeviceNotes { get; set; }

        [JsonPropertyName("device_location")]
        public bool? DeviceLocation { get; set; }

        [JsonPropertyName("device_firmware")]
        public bool? DeviceFirmware { get; set; }
    }

    // Product Firmware
    public class ProductFirmware
    {
        [JsonPropertyName("default")]
        public string? Default { get; set; }

        [JsonPropertyName("versions")]
        public List<FirmwareVersion>? Versions { get; set; }
    }

    // Firmware Version
    public class FirmwareVersion
    {
        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("product_default")]
        public bool? ProductDefault { get; set; }

        [JsonPropertyName("groups")]
        public List<string>? Groups { get; set; }
    }

    // Integration entity
    public class Integration : ParticleBaseEntity
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("enabled")]
        public bool? Enabled { get; set; }

        [JsonPropertyName("product_id")]
        public int? ProductId { get; set; }

        [JsonPropertyName("event")]
        public string? Event { get; set; }

        [JsonPropertyName("device_id")]
        public string? DeviceId { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("request_type")]
        public string? RequestType { get; set; }

        [JsonPropertyName("headers")]
        public Dictionary<string, string>? Headers { get; set; }

        [JsonPropertyName("query")]
        public Dictionary<string, string>? Query { get; set; }

        [JsonPropertyName("form")]
        public Dictionary<string, string>? Form { get; set; }

        [JsonPropertyName("json")]
        public Dictionary<string, object>? Json { get; set; }

        [JsonPropertyName("auth")]
        public IntegrationAuth? Auth { get; set; }

        [JsonPropertyName("response_template")]
        public string? ResponseTemplate { get; set; }

        [JsonPropertyName("response_topic")]
        public string? ResponseTopic { get; set; }

        [JsonPropertyName("error_response_topic")]
        public string? ErrorResponseTopic { get; set; }
    }

    // Integration Authentication
    public class IntegrationAuth
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("password")]
        public string? Password { get; set; }

        [JsonPropertyName("token")]
        public string? Token { get; set; }

        [JsonPropertyName("bearer")]
        public string? Bearer { get; set; }
    }

    // Webhook entity
    public class Webhook : ParticleBaseEntity
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("method")]
        public string? Method { get; set; }

        [JsonPropertyName("headers")]
        public Dictionary<string, string>? Headers { get; set; }

        [JsonPropertyName("query")]
        public Dictionary<string, string>? Query { get; set; }

        [JsonPropertyName("form")]
        public Dictionary<string, string>? Form { get; set; }

        [JsonPropertyName("json")]
        public Dictionary<string, object>? Json { get; set; }

        [JsonPropertyName("auth")]
        public WebhookAuth? Auth { get; set; }

        [JsonPropertyName("event")]
        public string? Event { get; set; }

        [JsonPropertyName("device_id")]
        public string? DeviceId { get; set; }

        [JsonPropertyName("product_id")]
        public int? ProductId { get; set; }

        [JsonPropertyName("request_type")]
        public string? RequestType { get; set; }

        [JsonPropertyName("response_template")]
        public string? ResponseTemplate { get; set; }

        [JsonPropertyName("response_topic")]
        public string? ResponseTopic { get; set; }

        [JsonPropertyName("error_response_topic")]
        public string? ErrorResponseTopic { get; set; }

        [JsonPropertyName("no_mime_type")]
        public bool? NoMimeType { get; set; }

        [JsonPropertyName("reject_unauthorized")]
        public bool? RejectUnauthorized { get; set; }
    }

    // Webhook Authentication
    public class WebhookAuth
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("password")]
        public string? Password { get; set; }

        [JsonPropertyName("token")]
        public string? Token { get; set; }

        [JsonPropertyName("bearer")]
        public string? Bearer { get; set; }
    }

    // Access Token entity
    public class AccessToken : ParticleBaseEntity
    {
        [JsonPropertyName("token")]
        public string? Token { get; set; }

        [JsonPropertyName("expires_at")]
        public DateTime? ExpiresAt { get; set; }

        [JsonPropertyName("client")]
        public string? Client { get; set; }

        [JsonPropertyName("scopes")]
        public List<string>? Scopes { get; set; }

        [JsonPropertyName("is_current")]
        public bool? IsCurrent { get; set; }
    }

    // SIM entity
    public class Sim : ParticleBaseEntity
    {
        [JsonPropertyName("iccid")]
        public string? Iccid { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("last_device_id")]
        public string? LastDeviceId { get; set; }

        [JsonPropertyName("activated_at")]
        public DateTime? ActivatedAt { get; set; }

        [JsonPropertyName("deactivated_at")]
        public DateTime? DeactivatedAt { get; set; }

        [JsonPropertyName("mb_used")]
        public long? MbUsed { get; set; }

        [JsonPropertyName("data_limit")]
        public long? DataLimit { get; set; }

        [JsonPropertyName("carrier")]
        public string? Carrier { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("plan")]
        public string? Plan { get; set; }
    }

    // Billing entity
    public class Billing
    {
        [JsonPropertyName("current_period_start")]
        public DateTime? CurrentPeriodStart { get; set; }

        [JsonPropertyName("current_period_end")]
        public DateTime? CurrentPeriodEnd { get; set; }

        [JsonPropertyName("subscription")]
        public BillingSubscription? Subscription { get; set; }

        [JsonPropertyName("usage")]
        public BillingUsage? Usage { get; set; }

        [JsonPropertyName("invoices")]
        public List<BillingInvoice>? Invoices { get; set; }
    }

    // Billing Subscription
    public class BillingSubscription
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("current_period_start")]
        public DateTime? CurrentPeriodStart { get; set; }

        [JsonPropertyName("current_period_end")]
        public DateTime? CurrentPeriodEnd { get; set; }

        [JsonPropertyName("cancel_at_period_end")]
        public bool? CancelAtPeriodEnd { get; set; }

        [JsonPropertyName("canceled_at")]
        public DateTime? CanceledAt { get; set; }
    }

    // Billing Usage
    public class BillingUsage
    {
        [JsonPropertyName("devices")]
        public int? Devices { get; set; }

        [JsonPropertyName("data_transfer")]
        public long? DataTransfer { get; set; }

        [JsonPropertyName("data_limit")]
        public long? DataLimit { get; set; }

        [JsonPropertyName("overage")]
        public long? Overage { get; set; }
    }

    // Billing Invoice
    public class BillingInvoice
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("number")]
        public string? Number { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("amount_due")]
        public decimal? AmountDue { get; set; }

        [JsonPropertyName("amount_paid")]
        public decimal? AmountPaid { get; set; }

        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("due_date")]
        public DateTime? DueDate { get; set; }

        [JsonPropertyName("period_start")]
        public DateTime? PeriodStart { get; set; }

        [JsonPropertyName("period_end")]
        public DateTime? PeriodEnd { get; set; }
    }

    // Diagnostics entity
    public class Diagnostics
    {
        [JsonPropertyName("device_id")]
        public string? DeviceId { get; set; }

        [JsonPropertyName("diagnostics")]
        public List<DiagnosticEntry>? DiagnosticEntries { get; set; }

        [JsonPropertyName("last_updated")]
        public DateTime? LastUpdated { get; set; }
    }

    // Diagnostic Entry
    public class DiagnosticEntry
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("value")]
        public object? Value { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime? Timestamp { get; set; }

        [JsonPropertyName("severity")]
        public string? Severity { get; set; }
    }

    // Registry (maps your fixed entity names to CLR types)
    public static class ParticleEntityRegistry
    {
        public static readonly Dictionary<string, Type> Types = new(StringComparer.OrdinalIgnoreCase)
        {
            ["devices"] = typeof(Device),
            ["device_details"] = typeof(Device),
            ["device_events"] = typeof(Event),
            ["device_variables"] = typeof(DeviceVariable),
            ["device_functions"] = typeof(string), // Functions are just strings
            ["products"] = typeof(Product),
            ["product_details"] = typeof(Product),
            ["product_devices"] = typeof(Device),
            ["product_firmware"] = typeof(FirmwareVersion),
            ["integrations"] = typeof(Integration),
            ["integration_details"] = typeof(Integration),
            ["webhooks"] = typeof(Webhook),
            ["webhook_details"] = typeof(Webhook),
            ["events"] = typeof(Event),
            ["event_details"] = typeof(Event),
            ["tokens"] = typeof(AccessToken),
            ["token_details"] = typeof(AccessToken),
            ["sims"] = typeof(Sim),
            ["sim_details"] = typeof(Sim),
            ["billing"] = typeof(Billing),
            ["diagnostics"] = typeof(Diagnostics)
        };

        public static IReadOnlyList<string> Names => new List<string>(Types.Keys);
    }
}