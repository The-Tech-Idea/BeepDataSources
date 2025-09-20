using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.AWSIoT
{
    // Base class for AWS IoT entities
    public class AWSIoTBaseEntity
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("source")]
        public string? Source { get; set; }
    }

    // Device/Thing entity
    public class Device : AWSIoTBaseEntity
    {
        [JsonPropertyName("thing_name")]
        public string? ThingName { get; set; }

        [JsonPropertyName("thing_type_name")]
        public string? ThingTypeName { get; set; }

        [JsonPropertyName("thing_group_names")]
        public List<string>? ThingGroupNames { get; set; }

        [JsonPropertyName("attributes")]
        public Dictionary<string, string>? Attributes { get; set; }

        [JsonPropertyName("registry")]
        public string? Registry { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }

    // Shadow entity
    public class Shadow : AWSIoTBaseEntity
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("payload")]
        public ShadowPayload? Payload { get; set; }

        [JsonPropertyName("thing_name")]
        public string? ThingName { get; set; }
    }

    // Shadow Payload
    public class ShadowPayload
    {
        [JsonPropertyName("state")]
        public ShadowState? State { get; set; }

        [JsonPropertyName("metadata")]
        public ShadowMetadata? Metadata { get; set; }

        [JsonPropertyName("timestamp")]
        public long? Timestamp { get; set; }

        [JsonPropertyName("client_token")]
        public string? ClientToken { get; set; }
    }

    // Shadow State
    public class ShadowState
    {
        [JsonPropertyName("desired")]
        public Dictionary<string, object>? Desired { get; set; }

        [JsonPropertyName("reported")]
        public Dictionary<string, object>? Reported { get; set; }

        [JsonPropertyName("delta")]
        public Dictionary<string, object>? Delta { get; set; }
    }

    // Shadow Metadata
    public class ShadowMetadata
    {
        [JsonPropertyName("desired")]
        public Dictionary<string, ShadowMetadataItem>? Desired { get; set; }

        [JsonPropertyName("reported")]
        public Dictionary<string, ShadowMetadataItem>? Reported { get; set; }
    }

    // Shadow Metadata Item
    public class ShadowMetadataItem
    {
        [JsonPropertyName("timestamp")]
        public long? Timestamp { get; set; }
    }

    // Job entity
    public class Job : AWSIoTBaseEntity
    {
        [JsonPropertyName("job_id")]
        public string? JobId { get; set; }

        [JsonPropertyName("job_arn")]
        public string? JobArn { get; set; }

        [JsonPropertyName("document_source")]
        public string? DocumentSource { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("force_canceled")]
        public bool? ForceCanceled { get; set; }

        [JsonPropertyName("reason_code")]
        public string? ReasonCode { get; set; }

        [JsonPropertyName("comment")]
        public string? Comment { get; set; }

        [JsonPropertyName("targets")]
        public List<string>? Targets { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("presigned_url_config")]
        public PresignedUrlConfig? PresignedUrlConfig { get; set; }

        [JsonPropertyName("job_executions_rollout_config")]
        public JobExecutionsRolloutConfig? JobExecutionsRolloutConfig { get; set; }

        [JsonPropertyName("abort_config")]
        public AbortConfig? AbortConfig { get; set; }

        [JsonPropertyName("timeout_config")]
        public TimeoutConfig? TimeoutConfig { get; set; }

        [JsonPropertyName("job_process_details")]
        public JobProcessDetails? JobProcessDetails { get; set; }

        [JsonPropertyName("namespace_id")]
        public string? NamespaceId { get; set; }

        [JsonPropertyName("job_template_arn")]
        public string? JobTemplateArn { get; set; }
    }

    // Rule entity
    public class Rule : AWSIoTBaseEntity
    {
        [JsonPropertyName("rule_name")]
        public string? RuleName { get; set; }

        [JsonPropertyName("topic_rule_payload")]
        public TopicRulePayload? TopicRulePayload { get; set; }

        [JsonPropertyName("rule_arn")]
        public string? RuleArn { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("error_action")]
        public Action? ErrorAction { get; set; }

        [JsonPropertyName("actions")]
        public List<Action>? Actions { get; set; }
    }

    // Certificate entity
    public class Certificate : AWSIoTBaseEntity
    {
        [JsonPropertyName("certificate_arn")]
        public string? CertificateArn { get; set; }

        [JsonPropertyName("certificate_id")]
        public string? CertificateId { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("certificate_pem")]
        public string? CertificatePem { get; set; }

        [JsonPropertyName("owned_by")]
        public string? OwnedBy { get; set; }

        [JsonPropertyName("previous_owned_by")]
        public string? PreviousOwnedBy { get; set; }

        [JsonPropertyName("transfer_data")]
        public TransferData? TransferData { get; set; }

        [JsonPropertyName("creation_date")]
        public DateTime? CreationDate { get; set; }

        [JsonPropertyName("last_modified_date")]
        public DateTime? LastModifiedDate { get; set; }

        [JsonPropertyName("customer_version")]
        public int? CustomerVersion { get; set; }
    }

    // Policy entity
    public class Policy : AWSIoTBaseEntity
    {
        [JsonPropertyName("policy_name")]
        public string? PolicyName { get; set; }

        [JsonPropertyName("policy_arn")]
        public string? PolicyArn { get; set; }

        [JsonPropertyName("policy_document")]
        public string? PolicyDocument { get; set; }

        [JsonPropertyName("default_version_id")]
        public string? DefaultVersionId { get; set; }
    }

    // Telemetry entity
    public class Telemetry : AWSIoTBaseEntity
    {
        [JsonPropertyName("topic")]
        public string? Topic { get; set; }

        [JsonPropertyName("payload")]
        public string? Payload { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime? Timestamp { get; set; }

        [JsonPropertyName("qos")]
        public int? Qos { get; set; }

        [JsonPropertyName("retain")]
        public bool? Retain { get; set; }
    }

    // Endpoint entity
    public class Endpoint : AWSIoTBaseEntity
    {
        [JsonPropertyName("endpoint_address")]
        public string? EndpointAddress { get; set; }

        [JsonPropertyName("endpoint_type")]
        public string? EndpointType { get; set; }

        [JsonPropertyName("endpoint_region")]
        public string? EndpointRegion { get; set; }
    }

    // Supporting classes
    public class PresignedUrlConfig
    {
        [JsonPropertyName("role_arn")]
        public string? RoleArn { get; set; }

        [JsonPropertyName("expires_in_sec")]
        public long? ExpiresInSec { get; set; }
    }

    public class JobExecutionsRolloutConfig
    {
        [JsonPropertyName("maximum_per_minute")]
        public int? MaximumPerMinute { get; set; }

        [JsonPropertyName("exponential_rate")]
        public ExponentialRolloutRate? ExponentialRate { get; set; }
    }

    public class ExponentialRolloutRate
    {
        [JsonPropertyName("base_rate_per_minute")]
        public int? BaseRatePerMinute { get; set; }

        [JsonPropertyName("increment_factor")]
        public double? IncrementFactor { get; set; }

        [JsonPropertyName("rate_increase_criteria")]
        public RateIncreaseCriteria? RateIncreaseCriteria { get; set; }
    }

    public class RateIncreaseCriteria
    {
        [JsonPropertyName("number_of_notified_things")]
        public int? NumberOfNotifiedThings { get; set; }

        [JsonPropertyName("number_of_succeeded_things")]
        public int? NumberOfSucceededThings { get; set; }
    }

    public class AbortConfig
    {
        [JsonPropertyName("criteria_list")]
        public List<AbortCriteria>? CriteriaList { get; set; }
    }

    public class AbortCriteria
    {
        [JsonPropertyName("failure_type")]
        public string? FailureType { get; set; }

        [JsonPropertyName("action")]
        public string? Action { get; set; }

        [JsonPropertyName("threshold_percentage")]
        public double? ThresholdPercentage { get; set; }

        [JsonPropertyName("min_number_of_executed_things")]
        public int? MinNumberOfExecutedThings { get; set; }
    }

    public class TimeoutConfig
    {
        [JsonPropertyName("in_progress_timeout_in_minutes")]
        public long? InProgressTimeoutInMinutes { get; set; }
    }

    public class JobProcessDetails
    {
        [JsonPropertyName("processing_targets")]
        public List<string>? ProcessingTargets { get; set; }

        [JsonPropertyName("number_of_canceled_things")]
        public int? NumberOfCanceledThings { get; set; }

        [JsonPropertyName("number_of_succeeded_things")]
        public int? NumberOfSucceededThings { get; set; }

        [JsonPropertyName("number_of_failed_things")]
        public int? NumberOfFailedThings { get; set; }

        [JsonPropertyName("number_of_rejected_things")]
        public int? NumberOfRejectedThings { get; set; }

        [JsonPropertyName("number_of_queued_things")]
        public int? NumberOfQueuedThings { get; set; }

        [JsonPropertyName("number_of_in_progress_things")]
        public int? NumberOfInProgressThings { get; set; }

        [JsonPropertyName("number_of_removed_things")]
        public int? NumberOfRemovedThings { get; set; }
    }

    public class TopicRulePayload
    {
        [JsonPropertyName("sql")]
        public string? Sql { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("actions")]
        public List<Action>? Actions { get; set; }

        [JsonPropertyName("rule_disabled")]
        public bool? RuleDisabled { get; set; }

        [JsonPropertyName("aws_iot_sql_version")]
        public string? AwsIotSqlVersion { get; set; }

        [JsonPropertyName("error_action")]
        public Action? ErrorAction { get; set; }
    }

    public class Action
    {
        [JsonPropertyName("dynamo_db")]
        public DynamoDBAction? DynamoDB { get; set; }

        [JsonPropertyName("dynamo_dbv2")]
        public DynamoDBv2Action? DynamoDBv2 { get; set; }

        [JsonPropertyName("lambda")]
        public LambdaAction? Lambda { get; set; }

        [JsonPropertyName("sns")]
        public SnsAction? Sns { get; set; }

        [JsonPropertyName("sqs")]
        public SqsAction? Sqs { get; set; }

        [JsonPropertyName("kinesis")]
        public KinesisAction? Kinesis { get; set; }

        [JsonPropertyName("republish")]
        public RepublishAction? Republish { get; set; }

        [JsonPropertyName("s3")]
        public S3Action? S3 { get; set; }

        [JsonPropertyName("firehose")]
        public FirehoseAction? Firehose { get; set; }

        [JsonPropertyName("cloudwatch_metric")]
        public CloudwatchMetricAction? CloudwatchMetric { get; set; }

        [JsonPropertyName("cloudwatch_alarm")]
        public CloudwatchAlarmAction? CloudwatchAlarm { get; set; }

        [JsonPropertyName("cloudwatch_logs")]
        public CloudwatchLogsAction? CloudwatchLogs { get; set; }

        [JsonPropertyName("elasticsearch")]
        public ElasticsearchAction? Elasticsearch { get; set; }

        [JsonPropertyName("salesforce")]
        public SalesforceAction? Salesforce { get; set; }

        [JsonPropertyName("iot_analytics")]
        public IotAnalyticsAction? IotAnalytics { get; set; }

        [JsonPropertyName("iot_events")]
        public IotEventsAction? IotEvents { get; set; }

        [JsonPropertyName("iot_site_wise")]
        public IotSiteWiseAction? IotSiteWise { get; set; }

        [JsonPropertyName("step_functions")]
        public StepFunctionsAction? StepFunctions { get; set; }

        [JsonPropertyName("timestream")]
        public TimestreamAction? Timestream { get; set; }

        [JsonPropertyName("http")]
        public HttpAction? Http { get; set; }

        [JsonPropertyName("https")]
        public HttpAction? Https { get; set; }

        [JsonPropertyName("kafka")]
        public KafkaAction? Kafka { get; set; }
    }

    // Action subclasses (simplified)
    public class DynamoDBAction
    {
        [JsonPropertyName("table_name")]
        public string? TableName { get; set; }

        [JsonPropertyName("role_arn")]
        public string? RoleArn { get; set; }

        [JsonPropertyName("operation")]
        public string? Operation { get; set; }

        [JsonPropertyName("hash_key_field")]
        public string? HashKeyField { get; set; }

        [JsonPropertyName("hash_key_value")]
        public string? HashKeyValue { get; set; }

        [JsonPropertyName("hash_key_type")]
        public string? HashKeyType { get; set; }

        [JsonPropertyName("range_key_field")]
        public string? RangeKeyField { get; set; }

        [JsonPropertyName("range_key_value")]
        public string? RangeKeyValue { get; set; }

        [JsonPropertyName("range_key_type")]
        public string? RangeKeyType { get; set; }

        [JsonPropertyName("payload_field")]
        public string? PayloadField { get; set; }
    }

    public class DynamoDBv2Action
    {
        [JsonPropertyName("role_arn")]
        public string? RoleArn { get; set; }

        [JsonPropertyName("put_item")]
        public PutItemInput? PutItem { get; set; }
    }

    public class PutItemInput
    {
        [JsonPropertyName("table_name")]
        public string? TableName { get; set; }
    }

    public class LambdaAction
    {
        [JsonPropertyName("function_arn")]
        public string? FunctionArn { get; set; }
    }

    public class SnsAction
    {
        [JsonPropertyName("target_arn")]
        public string? TargetArn { get; set; }

        [JsonPropertyName("role_arn")]
        public string? RoleArn { get; set; }

        [JsonPropertyName("message_format")]
        public string? MessageFormat { get; set; }
    }

    public class SqsAction
    {
        [JsonPropertyName("role_arn")]
        public string? RoleArn { get; set; }

        [JsonPropertyName("queue_url")]
        public string? QueueUrl { get; set; }

        [JsonPropertyName("use_base64")]
        public bool? UseBase64 { get; set; }
    }

    public class KinesisAction
    {
        [JsonPropertyName("role_arn")]
        public string? RoleArn { get; set; }

        [JsonPropertyName("stream_name")]
        public string? StreamName { get; set; }

        [JsonPropertyName("partition_key")]
        public string? PartitionKey { get; set; }
    }

    public class RepublishAction
    {
        [JsonPropertyName("role_arn")]
        public string? RoleArn { get; set; }

        [JsonPropertyName("topic")]
        public string? Topic { get; set; }

        [JsonPropertyName("qos")]
        public int? Qos { get; set; }
    }

    public class S3Action
    {
        [JsonPropertyName("role_arn")]
        public string? RoleArn { get; set; }

        [JsonPropertyName("bucket_name")]
        public string? BucketName { get; set; }

        [JsonPropertyName("key")]
        public string? Key { get; set; }

        [JsonPropertyName("canned_acl")]
        public string? CannedAcl { get; set; }
    }

    public class FirehoseAction
    {
        [JsonPropertyName("role_arn")]
        public string? RoleArn { get; set; }

        [JsonPropertyName("delivery_stream_name")]
        public string? DeliveryStreamName { get; set; }

        [JsonPropertyName("separator")]
        public string? Separator { get; set; }

        [JsonPropertyName("batch_mode")]
        public bool? BatchMode { get; set; }
    }

    public class CloudwatchMetricAction
    {
        [JsonPropertyName("role_arn")]
        public string? RoleArn { get; set; }

        [JsonPropertyName("metric_namespace")]
        public string? MetricNamespace { get; set; }

        [JsonPropertyName("metric_name")]
        public string? MetricName { get; set; }

        [JsonPropertyName("metric_value")]
        public string? MetricValue { get; set; }

        [JsonPropertyName("metric_unit")]
        public string? MetricUnit { get; set; }

        [JsonPropertyName("metric_timestamp")]
        public string? MetricTimestamp { get; set; }
    }

    public class CloudwatchAlarmAction
    {
        [JsonPropertyName("role_arn")]
        public string? RoleArn { get; set; }

        [JsonPropertyName("alarm_name")]
        public string? AlarmName { get; set; }

        [JsonPropertyName("state_reason")]
        public string? StateReason { get; set; }

        [JsonPropertyName("state_value")]
        public string? StateValue { get; set; }
    }

    public class CloudwatchLogsAction
    {
        [JsonPropertyName("role_arn")]
        public string? RoleArn { get; set; }

        [JsonPropertyName("log_group_name")]
        public string? LogGroupName { get; set; }
    }

    public class ElasticsearchAction
    {
        [JsonPropertyName("role_arn")]
        public string? RoleArn { get; set; }

        [JsonPropertyName("endpoint")]
        public string? Endpoint { get; set; }

        [JsonPropertyName("index")]
        public string? Index { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }

    public class SalesforceAction
    {
        [JsonPropertyName("token")]
        public string? Token { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }

    public class IotAnalyticsAction
    {
        [JsonPropertyName("channel_arn")]
        public string? ChannelArn { get; set; }

        [JsonPropertyName("channel_name")]
        public string? ChannelName { get; set; }

        [JsonPropertyName("batch_mode")]
        public bool? BatchMode { get; set; }

        [JsonPropertyName("role_arn")]
        public string? RoleArn { get; set; }
    }

    public class IotEventsAction
    {
        [JsonPropertyName("input_name")]
        public string? InputName { get; set; }

        [JsonPropertyName("message_id")]
        public string? MessageId { get; set; }

        [JsonPropertyName("batch_mode")]
        public bool? BatchMode { get; set; }

        [JsonPropertyName("role_arn")]
        public string? RoleArn { get; set; }
    }

    public class IotSiteWiseAction
    {
        [JsonPropertyName("put_asset_property_value_entries")]
        public List<PutAssetPropertyValueEntry>? PutAssetPropertyValueEntries { get; set; }

        [JsonPropertyName("role_arn")]
        public string? RoleArn { get; set; }
    }

    public class PutAssetPropertyValueEntry
    {
        [JsonPropertyName("entry_id")]
        public string? EntryId { get; set; }

        [JsonPropertyName("asset_id")]
        public string? AssetId { get; set; }

        [JsonPropertyName("property_id")]
        public string? PropertyId { get; set; }

        [JsonPropertyName("property_alias")]
        public string? PropertyAlias { get; set; }

        [JsonPropertyName("property_values")]
        public List<PropertyValue>? PropertyValues { get; set; }
    }

    public class PropertyValue
    {
        [JsonPropertyName("value")]
        public Variant? Value { get; set; }

        [JsonPropertyName("timestamp")]
        public TimeInNanos? Timestamp { get; set; }

        [JsonPropertyName("quality")]
        public string? Quality { get; set; }
    }

    public class Variant
    {
        [JsonPropertyName("string_value")]
        public string? StringValue { get; set; }

        [JsonPropertyName("integer_value")]
        public int? IntegerValue { get; set; }

        [JsonPropertyName("double_value")]
        public double? DoubleValue { get; set; }

        [JsonPropertyName("boolean_value")]
        public bool? BooleanValue { get; set; }
    }

    public class TimeInNanos
    {
        [JsonPropertyName("time_in_seconds")]
        public long? TimeInSeconds { get; set; }

        [JsonPropertyName("offset_in_nanos")]
        public int? OffsetInNanos { get; set; }
    }

    public class StepFunctionsAction
    {
        [JsonPropertyName("execution_name_prefix")]
        public string? ExecutionNamePrefix { get; set; }

        [JsonPropertyName("state_machine_name")]
        public string? StateMachineName { get; set; }

        [JsonPropertyName("role_arn")]
        public string? RoleArn { get; set; }
    }

    public class TimestreamAction
    {
        [JsonPropertyName("role_arn")]
        public string? RoleArn { get; set; }

        [JsonPropertyName("database_name")]
        public string? DatabaseName { get; set; }

        [JsonPropertyName("table_name")]
        public string? TableName { get; set; }

        [JsonPropertyName("dimensions")]
        public List<Dimension>? Dimensions { get; set; }
    }

    public class Dimension
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("value")]
        public string? Value { get; set; }
    }

    public class HttpAction
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("confirmation_url")]
        public string? ConfirmationUrl { get; set; }

        [JsonPropertyName("headers")]
        public List<Header>? Headers { get; set; }

        [JsonPropertyName("auth")]
        public HttpAuthorization? Auth { get; set; }
    }

    public class Header
    {
        [JsonPropertyName("key")]
        public string? Key { get; set; }

        [JsonPropertyName("value")]
        public string? Value { get; set; }
    }

    public class HttpAuthorization
    {
        [JsonPropertyName("sigv4")]
        public SigV4Authorization? Sigv4 { get; set; }
    }

    public class SigV4Authorization
    {
        [JsonPropertyName("signing_region")]
        public string? SigningRegion { get; set; }

        [JsonPropertyName("service_name")]
        public string? ServiceName { get; set; }

        [JsonPropertyName("role_arn")]
        public string? RoleArn { get; set; }
    }

    public class KafkaAction
    {
        [JsonPropertyName("destination_arn")]
        public string? DestinationArn { get; set; }

        [JsonPropertyName("topic")]
        public string? Topic { get; set; }

        [JsonPropertyName("key")]
        public string? Key { get; set; }

        [JsonPropertyName("partition")]
        public string? Partition { get; set; }

        [JsonPropertyName("client_properties")]
        public Dictionary<string, string>? ClientProperties { get; set; }
    }

    public class TransferData
    {
        [JsonPropertyName("transfer_message")]
        public string? TransferMessage { get; set; }

        [JsonPropertyName("reject_reason")]
        public string? RejectReason { get; set; }

        [JsonPropertyName("transfer_date")]
        public DateTime? TransferDate { get; set; }

        [JsonPropertyName("accept_date")]
        public DateTime? AcceptDate { get; set; }

        [JsonPropertyName("reject_date")]
        public DateTime? RejectDate { get; set; }
    }

    // Registry (maps your fixed entity names to CLR types)
    public static class AWSIoTEntityRegistry
    {
        public static readonly Dictionary<string, Type> Types = new(StringComparer.OrdinalIgnoreCase)
        {
            ["devices"] = typeof(Device),
            ["things"] = typeof(Device),
            ["shadows"] = typeof(Shadow),
            ["jobs"] = typeof(Job),
            ["rules"] = typeof(Rule),
            ["certificates"] = typeof(Certificate),
            ["policies"] = typeof(Policy),
            ["telemetry"] = typeof(Telemetry),
            ["endpoints"] = typeof(Endpoint)
        };

        public static IReadOnlyList<string> Names => new List<string>(Types.Keys);
    }
}