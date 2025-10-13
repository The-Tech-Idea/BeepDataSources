using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.AzureIoTHub
{
    // Base class for Azure IoT Hub entities
    public class AzureIoTHubBaseEntity
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("etag")]
        public string? ETag { get; set; }

        [JsonPropertyName("version")]
        public long? Version { get; set; }

        [JsonPropertyName("lastActivityTime")]
        public DateTime? LastActivityTime { get; set; }

        [JsonPropertyName("source")]
        public string? Source { get; set; }

        /// <summary>
        /// Reference to the data source (ignored during JSON serialization)
        /// </summary>
        [JsonIgnore]
        public IDataSource? DataSource { get; set; }

        /// <summary>
        /// Attaches the entity to a data source
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="dataSource">The data source to attach to</param>
        /// <returns>The entity instance with data source attached</returns>
        public T Attach<T>(IDataSource dataSource) where T : AzureIoTHubBaseEntity
        {
            DataSource = dataSource;
            return (T)this;
        }
    }

    // Device entity
    public class Device : AzureIoTHubBaseEntity
    {
        [JsonPropertyName("deviceId")]
        public string? DeviceId { get; set; }

        [JsonPropertyName("generationId")]
        public string? GenerationId { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("statusReason")]
        public string? StatusReason { get; set; }

        [JsonPropertyName("statusUpdateTime")]
        public DateTime? StatusUpdateTime { get; set; }

        [JsonPropertyName("connectionState")]
        public string? ConnectionState { get; set; }

        [JsonPropertyName("lastActivityTime")]
        public DateTime? LastActivityTime { get; set; }

        [JsonPropertyName("cloudToDeviceMessageCount")]
        public int? CloudToDeviceMessageCount { get; set; }

        [JsonPropertyName("authentication")]
        public DeviceAuthentication? Authentication { get; set; }

        [JsonPropertyName("capabilities")]
        public DeviceCapabilities? Capabilities { get; set; }

        [JsonPropertyName("deviceScope")]
        public string? DeviceScope { get; set; }

        [JsonPropertyName("parentScopes")]
        public List<string>? ParentScopes { get; set; }
    }

    // Device Twin entity
    public class DeviceTwin : AzureIoTHubBaseEntity
    {
        [JsonPropertyName("deviceId")]
        public string? DeviceId { get; set; }

        [JsonPropertyName("deviceEtag")]
        public string? DeviceETag { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("statusUpdateTime")]
        public DateTime? StatusUpdateTime { get; set; }

        [JsonPropertyName("connectionState")]
        public string? ConnectionState { get; set; }

        [JsonPropertyName("lastActivityTime")]
        public DateTime? LastActivityTime { get; set; }

        [JsonPropertyName("cloudToDeviceMessageCount")]
        public int? CloudToDeviceMessageCount { get; set; }

        [JsonPropertyName("authenticationType")]
        public string? AuthenticationType { get; set; }

        [JsonPropertyName("x509Thumbprint")]
        public X509Thumbprint? X509Thumbprint { get; set; }

        [JsonPropertyName("capabilities")]
        public DeviceCapabilities? Capabilities { get; set; }

        [JsonPropertyName("deviceScope")]
        public string? DeviceScope { get; set; }

        [JsonPropertyName("parentScopes")]
        public List<string>? ParentScopes { get; set; }

        [JsonPropertyName("properties")]
        public TwinProperties? Properties { get; set; }

        [JsonPropertyName("tags")]
        public Dictionary<string, object>? Tags { get; set; }
    }

    // Twin Properties
    public class TwinProperties
    {
        [JsonPropertyName("desired")]
        public TwinPropertyCollection? Desired { get; set; }

        [JsonPropertyName("reported")]
        public TwinPropertyCollection? Reported { get; set; }
    }

    // Twin Property Collection
    public class TwinPropertyCollection
    {
        [JsonPropertyName("$metadata")]
        public TwinMetadata? Metadata { get; set; }

        [JsonPropertyName("$version")]
        public long? Version { get; set; }

        // Additional properties can be added dynamically
        [JsonExtensionData]
        public Dictionary<string, object>? Properties { get; set; }
    }

    // Twin Metadata
    public class TwinMetadata
    {
        [JsonPropertyName("$lastUpdated")]
        public DateTime? LastUpdated { get; set; }

        [JsonPropertyName("$lastUpdatedVersion")]
        public long? LastUpdatedVersion { get; set; }

        // Additional metadata properties can be added dynamically
        [JsonExtensionData]
        public Dictionary<string, TwinPropertyMetadata>? PropertyMetadata { get; set; }
    }

    // Twin Property Metadata
    public class TwinPropertyMetadata
    {
        [JsonPropertyName("$lastUpdated")]
        public DateTime? LastUpdated { get; set; }

        [JsonPropertyName("$lastUpdatedVersion")]
        public long? LastUpdatedVersion { get; set; }
    }

    // Device Authentication
    public class DeviceAuthentication
    {
        [JsonPropertyName("symmetricKey")]
        public SymmetricKey? SymmetricKey { get; set; }

        [JsonPropertyName("x509Thumbprint")]
        public X509Thumbprint? X509Thumbprint { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }

    // Symmetric Key
    public class SymmetricKey
    {
        [JsonPropertyName("primaryKey")]
        public string? PrimaryKey { get; set; }

        [JsonPropertyName("secondaryKey")]
        public string? SecondaryKey { get; set; }
    }

    // X509 Thumbprint
    public class X509Thumbprint
    {
        [JsonPropertyName("primaryThumbprint")]
        public string? PrimaryThumbprint { get; set; }

        [JsonPropertyName("secondaryThumbprint")]
        public string? SecondaryThumbprint { get; set; }
    }

    // Device Capabilities
    public class DeviceCapabilities
    {
        [JsonPropertyName("iotEdge")]
        public bool? IotEdge { get; set; }
    }

    // Job entity
    public class Job : AzureIoTHubBaseEntity
    {
        [JsonPropertyName("jobId")]
        public string? JobId { get; set; }

        [JsonPropertyName("queryCondition")]
        public string? QueryCondition { get; set; }

        [JsonPropertyName("createdTimeUtc")]
        public DateTime? CreatedTimeUtc { get; set; }

        [JsonPropertyName("startTimeUtc")]
        public DateTime? StartTimeUtc { get; set; }

        [JsonPropertyName("endTimeUtc")]
        public DateTime? EndTimeUtc { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("failureReason")]
        public string? FailureReason { get; set; }

        [JsonPropertyName("statusMessage")]
        public string? StatusMessage { get; set; }

        [JsonPropertyName("deviceJobStatistics")]
        public DeviceJobStatistics? DeviceJobStatistics { get; set; }

        [JsonPropertyName("updateTwin")]
        public TwinUpdate? UpdateTwin { get; set; }

        [JsonPropertyName("cloudToDeviceMethod")]
        public CloudToDeviceMethod? CloudToDeviceMethod { get; set; }

        [JsonPropertyName("deviceMethodParameter")]
        public DeviceMethodParameter? DeviceMethodParameter { get; set; }
    }

    // Device Job Statistics
    public class DeviceJobStatistics
    {
        [JsonPropertyName("deviceCount")]
        public int? DeviceCount { get; set; }

        [JsonPropertyName("failedCount")]
        public int? FailedCount { get; set; }

        [JsonPropertyName("succeededCount")]
        public int? SucceededCount { get; set; }

        [JsonPropertyName("runningCount")]
        public int? RunningCount { get; set; }

        [JsonPropertyName("pendingCount")]
        public int? PendingCount { get; set; }
    }

    // Twin Update
    public class TwinUpdate
    {
        [JsonPropertyName("deviceId")]
        public string? DeviceId { get; set; }

        [JsonPropertyName("etag")]
        public string? ETag { get; set; }

        [JsonPropertyName("tags")]
        public Dictionary<string, object>? Tags { get; set; }

        [JsonPropertyName("properties")]
        public TwinProperties? Properties { get; set; }
    }

    // Cloud to Device Method
    public class CloudToDeviceMethod
    {
        [JsonPropertyName("methodName")]
        public string? MethodName { get; set; }

        [JsonPropertyName("payload")]
        public object? Payload { get; set; }

        [JsonPropertyName("responseTimeoutInSeconds")]
        public int? ResponseTimeoutInSeconds { get; set; }

        [JsonPropertyName("connectTimeoutInSeconds")]
        public int? ConnectTimeoutInSeconds { get; set; }
    }

    // Device Method Parameter
    public class DeviceMethodParameter
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("jsonPayload")]
        public string? JsonPayload { get; set; }

        [JsonPropertyName("responseTimeoutInSeconds")]
        public int? ResponseTimeoutInSeconds { get; set; }

        [JsonPropertyName("connectTimeoutInSeconds")]
        public int? ConnectTimeoutInSeconds { get; set; }
    }

    // Configuration entity
    public class Configuration : AzureIoTHubBaseEntity
    {
        [JsonPropertyName("configurationId")]
        public string? ConfigurationId { get; set; }

        [JsonPropertyName("schemaVersion")]
        public string? SchemaVersion { get; set; }

        [JsonPropertyName("labels")]
        public Dictionary<string, string>? Labels { get; set; }

        [JsonPropertyName("content")]
        public ConfigurationContent? Content { get; set; }

        [JsonPropertyName("targetCondition")]
        public string? TargetCondition { get; set; }

        [JsonPropertyName("createdTimeUtc")]
        public DateTime? CreatedTimeUtc { get; set; }

        [JsonPropertyName("lastUpdatedTimeUtc")]
        public DateTime? LastUpdatedTimeUtc { get; set; }

        [JsonPropertyName("priority")]
        public int? Priority { get; set; }

        [JsonPropertyName("systemMetrics")]
        public ConfigurationMetrics? SystemMetrics { get; set; }

        [JsonPropertyName("metrics")]
        public ConfigurationMetrics? Metrics { get; set; }

        [JsonPropertyName("etag")]
        public string? ETag { get; set; }
    }

    // Configuration Content
    public class ConfigurationContent
    {
        [JsonPropertyName("modulesContent")]
        public Dictionary<string, Dictionary<string, object>>? ModulesContent { get; set; }

        [JsonPropertyName("deviceContent")]
        public Dictionary<string, object>? DeviceContent { get; set; }
    }

    // Configuration Metrics
    public class ConfigurationMetrics
    {
        [JsonPropertyName("results")]
        public Dictionary<string, long>? Results { get; set; }

        [JsonPropertyName("queries")]
        public Dictionary<string, string>? Queries { get; set; }
    }

    // Telemetry entity
    public class Telemetry : AzureIoTHubBaseEntity
    {
        [JsonPropertyName("body")]
        public string? Body { get; set; }

        [JsonPropertyName("enqueuedTime")]
        public DateTime? EnqueuedTime { get; set; }

        [JsonPropertyName("deviceId")]
        public string? DeviceId { get; set; }

        [JsonPropertyName("properties")]
        public Dictionary<string, string>? Properties { get; set; }

        [JsonPropertyName("systemProperties")]
        public Dictionary<string, string>? SystemProperties { get; set; }
    }

    // Module entity
    public class Module : AzureIoTHubBaseEntity
    {
        [JsonPropertyName("moduleId")]
        public string? ModuleId { get; set; }

        [JsonPropertyName("deviceId")]
        public string? DeviceId { get; set; }

        [JsonPropertyName("generationId")]
        public string? GenerationId { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("statusReason")]
        public string? StatusReason { get; set; }

        [JsonPropertyName("statusUpdateTime")]
        public DateTime? StatusUpdateTime { get; set; }

        [JsonPropertyName("connectionState")]
        public string? ConnectionState { get; set; }

        [JsonPropertyName("lastActivityTime")]
        public DateTime? LastActivityTime { get; set; }

        [JsonPropertyName("cloudToDeviceMessageCount")]
        public int? CloudToDeviceMessageCount { get; set; }

        [JsonPropertyName("authentication")]
        public DeviceAuthentication? Authentication { get; set; }
    }

    // Module Twin entity
    public class ModuleTwin : AzureIoTHubBaseEntity
    {
        [JsonPropertyName("deviceId")]
        public string? DeviceId { get; set; }

        [JsonPropertyName("moduleId")]
        public string? ModuleId { get; set; }

        [JsonPropertyName("properties")]
        public TwinProperties? Properties { get; set; }

        [JsonPropertyName("tags")]
        public Dictionary<string, object>? Tags { get; set; }
    }

    // Statistics entity
    public class ServiceStatistics
    {
        [JsonPropertyName("connectedDeviceCount")]
        public int? ConnectedDeviceCount { get; set; }

        [JsonPropertyName("totalDeviceCount")]
        public int? TotalDeviceCount { get; set; }

        [JsonPropertyName("disabledDeviceCount")]
        public int? DisabledDeviceCount { get; set; }

        [JsonPropertyName("enabledDeviceCount")]
        public int? EnabledDeviceCount { get; set; }

        [JsonPropertyName("source")]
        public string? Source { get; set; }
    }

    // Device Statistics entity
    public class DeviceStatistics
    {
        [JsonPropertyName("deviceId")]
        public string? DeviceId { get; set; }

        [JsonPropertyName("moduleId")]
        public string? ModuleId { get; set; }

        [JsonPropertyName("cloudToDeviceMessageCount")]
        public int? CloudToDeviceMessageCount { get; set; }

        [JsonPropertyName("deviceToCloudMessageCount")]
        public int? DeviceToCloudMessageCount { get; set; }

        [JsonPropertyName("lastActivityTime")]
        public DateTime? LastActivityTime { get; set; }

        [JsonPropertyName("lastConnectionState")]
        public string? LastConnectionState { get; set; }

        [JsonPropertyName("lastStateUpdatedTime")]
        public DateTime? LastStateUpdatedTime { get; set; }

        [JsonPropertyName("source")]
        public string? Source { get; set; }
    }

    // Registry (maps your fixed entity names to CLR types)
    public static class AzureIoTHubEntityRegistry
    {
        public static readonly Dictionary<string, Type> Types = new(StringComparer.OrdinalIgnoreCase)
        {
            ["devices"] = typeof(Device),
            ["device_twins"] = typeof(DeviceTwin),
            ["device_methods"] = typeof(CloudToDeviceMethod),
            ["device_jobs"] = typeof(Job),
            ["jobs"] = typeof(Job),
            ["job_details"] = typeof(Job),
            ["configurations"] = typeof(Configuration),
            ["configuration_details"] = typeof(Configuration),
            ["telemetry"] = typeof(Telemetry),
            ["modules"] = typeof(Module),
            ["module_twins"] = typeof(ModuleTwin),
            ["statistics"] = typeof(ServiceStatistics),
            ["device_statistics"] = typeof(DeviceStatistics)
        };

        public static IReadOnlyList<string> Names => new List<string>(Types.Keys);
    }
}