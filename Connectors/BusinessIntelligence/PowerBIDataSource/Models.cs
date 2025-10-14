using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TheTechIdea.Beep.Connectors.BusinessIntelligence.PowerBI.Models
{
    /// <summary>
    /// Power BI API response wrapper
    /// </summary>
    public class PowerBIApiResponse<T>
    {
        [JsonPropertyName("@odata.context")]
        public string OdataContext { get; set; }

        [JsonPropertyName("@odata.count")]
        public int? OdataCount { get; set; }

        [JsonPropertyName("value")]
        public List<T> Value { get; set; }

        [JsonPropertyName("@odata.nextLink")]
        public string OdataNextLink { get; set; }
    }

    /// <summary>
    /// Power BI API single entity response
    /// </summary>
    public class PowerBISingleResponse<T>
    {
        [JsonPropertyName("@odata.context")]
        public string OdataContext { get; set; }

        [JsonPropertyName("value")]
        public T Value { get; set; }
    }

    /// <summary>
    /// Power BI Workspace entity
    /// </summary>
    public class PowerBIWorkspace
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("isReadOnly")]
        public bool IsReadOnly { get; set; }

        [JsonPropertyName("isOnDedicatedCapacity")]
        public bool IsOnDedicatedCapacity { get; set; }

        [JsonPropertyName("capacityId")]
        public string CapacityId { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }

        [JsonPropertyName("isOrphaned")]
        public bool IsOrphaned { get; set; }

        [JsonPropertyName("users")]
        public List<PowerBIWorkspaceUser> Users { get; set; }

        [JsonPropertyName("reports")]
        public List<PowerBIReport> Reports { get; set; }

        [JsonPropertyName("dashboards")]
        public List<PowerBIDashboard> Dashboards { get; set; }

        [JsonPropertyName("datasets")]
        public List<PowerBIDataset> Datasets { get; set; }

        [JsonPropertyName("dataflows")]
        public List<PowerBIDataflow> Dataflows { get; set; }

        [JsonPropertyName("workbooks")]
        public List<PowerBIWorkbook> Workbooks { get; set; }
    }

    /// <summary>
    /// Power BI Workspace User
    /// </summary>
    public class PowerBIWorkspaceUser
    {
        [JsonPropertyName("identifier")]
        public string Identifier { get; set; }

        [JsonPropertyName("principalType")]
        public string PrincipalType { get; set; }

        [JsonPropertyName("groupUserAccessRight")]
        public string GroupUserAccessRight { get; set; }

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }

        [JsonPropertyName("emailAddress")]
        public string EmailAddress { get; set; }

        [JsonPropertyName("graphId")]
        public string GraphId { get; set; }
    }

    /// <summary>
    /// Power BI Dataset entity
    /// </summary>
    public class PowerBIDataset
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("owner")]
        public string Owner { get; set; }

        [JsonPropertyName("webUrl")]
        public string WebUrl { get; set; }

        [JsonPropertyName("addRowsAPIEnabled")]
        public bool AddRowsAPIEnabled { get; set; }

        [JsonPropertyName("configuredBy")]
        public string ConfiguredBy { get; set; }

        [JsonPropertyName("isRefreshable")]
        public bool IsRefreshable { get; set; }

        [JsonPropertyName("isEffectiveIdentityRequired")]
        public bool IsEffectiveIdentityRequired { get; set; }

        [JsonPropertyName("isEffectiveIdentityRolesRequired")]
        public bool IsEffectiveIdentityRolesRequired { get; set; }

        [JsonPropertyName("isOnPremGatewayRequired")]
        public bool IsOnPremGatewayRequired { get; set; }

        [JsonPropertyName("targetStorageMode")]
        public string TargetStorageMode { get; set; }

        [JsonPropertyName("actualStorage")]
        public string ActualStorage { get; set; }

        [JsonPropertyName("createdDate")]
        public DateTime? CreatedDate { get; set; }

        [JsonPropertyName("contentProviderType")]
        public string ContentProviderType { get; set; }

        [JsonPropertyName("upstreamDatasets")]
        public List<PowerBIUpstreamDataset> UpstreamDatasets { get; set; }

        [JsonPropertyName("tables")]
        public List<PowerBITable> Tables { get; set; }

        [JsonPropertyName("datasources")]
        public List<PowerBIDataSource> Datasources { get; set; }

        [JsonPropertyName("queryScaleOutSettings")]
        public PowerBIQueryScaleOutSettings QueryScaleOutSettings { get; set; }

        [JsonPropertyName("isInPlaceSharingEnabled")]
        public bool IsInPlaceSharingEnabled { get; set; }
    }

    /// <summary>
    /// Power BI Dataset Table
    /// </summary>
    public class PowerBITable
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("columns")]
        public List<PowerBIColumn> Columns { get; set; }

        [JsonPropertyName("measures")]
        public List<PowerBIMeasure> Measures { get; set; }

        [JsonPropertyName("source")]
        public List<PowerBITableSource> Source { get; set; }

        [JsonPropertyName("rows")]
        public List<Dictionary<string, object>> Rows { get; set; }
    }

    /// <summary>
    /// Power BI Column
    /// </summary>
    public class PowerBIColumn
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("dataType")]
        public string DataType { get; set; }

        [JsonPropertyName("isHidden")]
        public bool IsHidden { get; set; }

        [JsonPropertyName("columnType")]
        public string ColumnType { get; set; }

        [JsonPropertyName("aggregateFunction")]
        public string AggregateFunction { get; set; }

        [JsonPropertyName("sortByColumn")]
        public string SortByColumn { get; set; }

        [JsonPropertyName("dataCategory")]
        public string DataCategory { get; set; }

        [JsonPropertyName("summarizeBy")]
        public string SummarizeBy { get; set; }
    }

    /// <summary>
    /// Power BI Measure
    /// </summary>
    public class PowerBIMeasure
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("expression")]
        public string Expression { get; set; }

        [JsonPropertyName("formatString")]
        public string FormatString { get; set; }

        [JsonPropertyName("isHidden")]
        public bool IsHidden { get; set; }

        [JsonPropertyName("dataCategory")]
        public string DataCategory { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }
    }

    /// <summary>
    /// Power BI Table Source
    /// </summary>
    public class PowerBITableSource
    {
        [JsonPropertyName("expression")]
        public string Expression { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }
    }

    /// <summary>
    /// Power BI Data Source
    /// </summary>
    public class PowerBIDataSource
    {
        [JsonPropertyName("datasourceType")]
        public string DatasourceType { get; set; }

        [JsonPropertyName("connectionDetails")]
        public PowerBIConnectionDetails ConnectionDetails { get; set; }

        [JsonPropertyName("datasourceId")]
        public string DatasourceId { get; set; }

        [JsonPropertyName("gatewayId")]
        public string GatewayId { get; set; }
    }

    /// <summary>
    /// Power BI Connection Details
    /// </summary>
    public class PowerBIConnectionDetails
    {
        [JsonPropertyName("server")]
        public string Server { get; set; }

        [JsonPropertyName("database")]
        public string Database { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("connectionString")]
        public string ConnectionString { get; set; }
    }

    /// <summary>
    /// Power BI Upstream Dataset
    /// </summary>
    public class PowerBIUpstreamDataset
    {
        [JsonPropertyName("targetDatasetId")]
        public string TargetDatasetId { get; set; }
    }

    /// <summary>
    /// Power BI Query Scale Out Settings
    /// </summary>
    public class PowerBIQueryScaleOutSettings
    {
        [JsonPropertyName("autoSyncReadOnlyReplicas")]
        public bool AutoSyncReadOnlyReplicas { get; set; }

        [JsonPropertyName("maxReadOnlyReplicas")]
        public int MaxReadOnlyReplicas { get; set; }
    }

    /// <summary>
    /// Power BI Report entity
    /// </summary>
    public class PowerBIReport
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("webUrl")]
        public string WebUrl { get; set; }

        [JsonPropertyName("embedUrl")]
        public string EmbedUrl { get; set; }

        [JsonPropertyName("datasetId")]
        public string DatasetId { get; set; }

        [JsonPropertyName("createdDateTime")]
        public DateTime? CreatedDateTime { get; set; }

        [JsonPropertyName("modifiedDateTime")]
        public DateTime? ModifiedDateTime { get; set; }

        [JsonPropertyName("createdBy")]
        public string CreatedBy { get; set; }

        [JsonPropertyName("modifiedBy")]
        public string ModifiedBy { get; set; }

        [JsonPropertyName("isOwnedByMe")]
        public bool IsOwnedByMe { get; set; }

        [JsonPropertyName("isOriginalPbixReport")]
        public bool IsOriginalPbixReport { get; set; }

        [JsonPropertyName("originalReportId")]
        public string OriginalReportId { get; set; }

        [JsonPropertyName("reportType")]
        public string ReportType { get; set; }

        [JsonPropertyName("users")]
        public List<PowerBIReportUser> Users { get; set; }

        [JsonPropertyName("subscriptions")]
        public List<PowerBISubscription> Subscriptions { get; set; }
    }

    /// <summary>
    /// Power BI Report User
    /// </summary>
    public class PowerBIReportUser
    {
        [JsonPropertyName("identifier")]
        public string Identifier { get; set; }

        [JsonPropertyName("principalType")]
        public string PrincipalType { get; set; }

        [JsonPropertyName("reportUserAccessRight")]
        public string ReportUserAccessRight { get; set; }

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }

        [JsonPropertyName("emailAddress")]
        public string EmailAddress { get; set; }

        [JsonPropertyName("graphId")]
        public string GraphId { get; set; }
    }

    /// <summary>
    /// Power BI Dashboard entity
    /// </summary>
    public class PowerBIDashboard
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("isReadOnly")]
        public bool IsReadOnly { get; set; }

        [JsonPropertyName("webUrl")]
        public string WebUrl { get; set; }

        [JsonPropertyName("embedUrl")]
        public string EmbedUrl { get; set; }

        [JsonPropertyName("tiles")]
        public List<PowerBITile> Tiles { get; set; }

        [JsonPropertyName("users")]
        public List<PowerBIDashboardUser> Users { get; set; }
    }

    /// <summary>
    /// Power BI Dashboard Tile
    /// </summary>
    public class PowerBITile
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("embedUrl")]
        public string EmbedUrl { get; set; }

        [JsonPropertyName("rowSpan")]
        public int RowSpan { get; set; }

        [JsonPropertyName("colSpan")]
        public int ColSpan { get; set; }

        [JsonPropertyName("reportId")]
        public string ReportId { get; set; }

        [JsonPropertyName("datasetId")]
        public string DatasetId { get; set; }
    }

    /// <summary>
    /// Power BI Dashboard User
    /// </summary>
    public class PowerBIDashboardUser
    {
        [JsonPropertyName("identifier")]
        public string Identifier { get; set; }

        [JsonPropertyName("principalType")]
        public string PrincipalType { get; set; }

        [JsonPropertyName("dashboardUserAccessRight")]
        public string DashboardUserAccessRight { get; set; }

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }

        [JsonPropertyName("emailAddress")]
        public string EmailAddress { get; set; }

        [JsonPropertyName("graphId")]
        public string GraphId { get; set; }
    }

    /// <summary>
    /// Power BI Dataflow entity
    /// </summary>
    public class PowerBIDataflow
    {
        [JsonPropertyName("objectId")]
        public string ObjectId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("modelUrl")]
        public string ModelUrl { get; set; }

        [JsonPropertyName("configuredBy")]
        public string ConfiguredBy { get; set; }

        [JsonPropertyName("contentProviderType")]
        public string ContentProviderType { get; set; }
    }

    /// <summary>
    /// Power BI Workbook entity
    /// </summary>
    public class PowerBIWorkbook
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("webUrl")]
        public string WebUrl { get; set; }

        [JsonPropertyName("embedUrl")]
        public string EmbedUrl { get; set; }
    }

    /// <summary>
    /// Power BI App entity
    /// </summary>
    public class PowerBIApp
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("publishedBy")]
        public string PublishedBy { get; set; }

        [JsonPropertyName("lastUpdate")]
        public DateTime? LastUpdate { get; set; }

        [JsonPropertyName("users")]
        public List<PowerBIAppUser> Users { get; set; }
    }

    /// <summary>
    /// Power BI App User
    /// </summary>
    public class PowerBIAppUser
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }

        [JsonPropertyName("emailAddress")]
        public string EmailAddress { get; set; }

        [JsonPropertyName("appUserAccessRight")]
        public string AppUserAccessRight { get; set; }

        [JsonPropertyName("principalType")]
        public string PrincipalType { get; set; }
    }

    /// <summary>
    /// Power BI Subscription entity
    /// </summary>
    public class PowerBISubscription
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("artifactId")]
        public string ArtifactId { get; set; }

        [JsonPropertyName("artifactDisplayName")]
        public string ArtifactDisplayName { get; set; }

        [JsonPropertyName("artifactType")]
        public string ArtifactType { get; set; }

        [JsonPropertyName("subArtifactDisplayName")]
        public string SubArtifactDisplayName { get; set; }

        [JsonPropertyName("frequency")]
        public string Frequency { get; set; }

        [JsonPropertyName("startDate")]
        public string StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public string EndDate { get; set; }

        [JsonPropertyName("isEnabled")]
        public bool IsEnabled { get; set; }

        [JsonPropertyName("users")]
        public List<PowerBISubscriptionUser> Users { get; set; }
    }

    /// <summary>
    /// Power BI Subscription User
    /// </summary>
    public class PowerBISubscriptionUser
    {
        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }

        [JsonPropertyName("emailAddress")]
        public string EmailAddress { get; set; }

        [JsonPropertyName("identifier")]
        public string Identifier { get; set; }

        [JsonPropertyName("principalType")]
        public string PrincipalType { get; set; }

        [JsonPropertyName("graphId")]
        public string GraphId { get; set; }
    }

    /// <summary>
    /// Power BI Refresh History
    /// </summary>
    public class PowerBIRefreshHistory
    {
        [JsonPropertyName("refreshType")]
        public string RefreshType { get; set; }

        [JsonPropertyName("startTime")]
        public DateTime? StartTime { get; set; }

        [JsonPropertyName("endTime")]
        public DateTime? EndTime { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("serviceExceptionJson")]
        public string ServiceExceptionJson { get; set; }

        [JsonPropertyName("refreshAttempts")]
        public List<PowerBIRefreshAttempt> RefreshAttempts { get; set; }
    }

    /// <summary>
    /// Power BI Refresh Attempt
    /// </summary>
    public class PowerBIRefreshAttempt
    {
        [JsonPropertyName("attemptId")]
        public string AttemptId { get; set; }

        [JsonPropertyName("startTime")]
        public DateTime? StartTime { get; set; }

        [JsonPropertyName("endTime")]
        public DateTime? EndTime { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("serviceExceptionJson")]
        public string ServiceExceptionJson { get; set; }
    }

    /// <summary>
    /// Power BI Gateway entity
    /// </summary>
    public class PowerBIGateway
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("gatewayAnnotation")]
        public string GatewayAnnotation { get; set; }

        [JsonPropertyName("publicKey")]
        public PowerBIGatewayPublicKey PublicKey { get; set; }

        [JsonPropertyName("gatewayStatus")]
        public string GatewayStatus { get; set; }
    }

    /// <summary>
    /// Power BI Gateway Public Key
    /// </summary>
    public class PowerBIGatewayPublicKey
    {
        [JsonPropertyName("exponent")]
        public string Exponent { get; set; }

        [JsonPropertyName("modulus")]
        public string Modulus { get; set; }
    }

    /// <summary>
    /// Power BI Activity Event
    /// </summary>
    public class PowerBIActivityEvent
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("recordType")]
        public string RecordType { get; set; }

        [JsonPropertyName("creationTime")]
        public DateTime? CreationTime { get; set; }

        [JsonPropertyName("operation")]
        public string Operation { get; set; }

        [JsonPropertyName("organizationId")]
        public string OrganizationId { get; set; }

        [JsonPropertyName("userType")]
        public string UserType { get; set; }

        [JsonPropertyName("userKey")]
        public string UserKey { get; set; }

        [JsonPropertyName("workload")]
        public string Workload { get; set; }

        [JsonPropertyName("userId")]
        public string UserId { get; set; }

        [JsonPropertyName("clientIP")]
        public string ClientIP { get; set; }

        [JsonPropertyName("userAgent")]
        public string UserAgent { get; set; }

        [JsonPropertyName("activity")]
        public string Activity { get; set; }

        [JsonPropertyName("requestId")]
        public string RequestId { get; set; }

        [JsonPropertyName("activityId")]
        public string ActivityId { get; set; }

        [JsonPropertyName("itemName")]
        public string ItemName { get; set; }

        [JsonPropertyName("workSpaceName")]
        public string WorkSpaceName { get; set; }

        [JsonPropertyName("datasetName")]
        public string DatasetName { get; set; }

        [JsonPropertyName("reportName")]
        public string ReportName { get; set; }

        [JsonPropertyName("workspaceId")]
        public string WorkspaceId { get; set; }

        [JsonPropertyName("objectId")]
        public string ObjectId { get; set; }

        [JsonPropertyName("datasetId")]
        public string DatasetId { get; set; }

        [JsonPropertyName("reportId")]
        public string ReportId { get; set; }

        [JsonPropertyName("distributionMethod")]
        public string DistributionMethod { get; set; }

        [JsonPropertyName("consumptionMethod")]
        public string ConsumptionMethod { get; set; }

        [JsonPropertyName("capacityId")]
        public string CapacityId { get; set; }

        [JsonPropertyName("capacityName")]
        public string CapacityName { get; set; }

        [JsonPropertyName("appName")]
        public string AppName { get; set; }

        [JsonPropertyName("appReportId")]
        public string AppReportId { get; set; }

        [JsonPropertyName("isSuccess")]
        public bool IsSuccess { get; set; }

        [JsonPropertyName("refreshType")]
        public string RefreshType { get; set; }

        [JsonPropertyName("requestId")]
        public string RequestId2 { get; set; }

        [JsonPropertyName("activityId")]
        public string ActivityId2 { get; set; }

        [JsonPropertyName("subActivityId")]
        public string SubActivityId { get; set; }

        [JsonPropertyName("appId")]
        public string AppId { get; set; }

        [JsonPropertyName("dataflowId")]
        public string DataflowId { get; set; }

        [JsonPropertyName("dataflowName")]
        public string DataflowName { get; set; }

        [JsonPropertyName("dataflowType")]
        public string DataflowType { get; set; }

        [JsonPropertyName("gatewayId")]
        public string GatewayId { get; set; }

        [JsonPropertyName("gatewayName")]
        public string GatewayName { get; set; }

        [JsonPropertyName("gatewayType")]
        public string GatewayType { get; set; }

        [JsonPropertyName("exportEventStartDateTimeParameter")]
        public DateTime? ExportEventStartDateTimeParameter { get; set; }

        [JsonPropertyName("exportEventEndDateTimeParameter")]
        public DateTime? ExportEventEndDateTimeParameter { get; set; }

        [JsonPropertyName("artifactsRead")]
        public List<PowerBIArtifact> ArtifactsRead { get; set; }

        [JsonPropertyName("artifactsCreated")]
        public List<PowerBIArtifact> ArtifactsCreated { get; set; }

        [JsonPropertyName("artifactProperties")]
        public List<PowerBIArtifactProperty> ArtifactProperties { get; set; }
    }

    /// <summary>
    /// Power BI Artifact
    /// </summary>
    public class PowerBIArtifact
    {
        [JsonPropertyName("artifactId")]
        public string ArtifactId { get; set; }

        [JsonPropertyName("artifactName")]
        public string ArtifactName { get; set; }

        [JsonPropertyName("artifactType")]
        public string ArtifactType { get; set; }
    }

    /// <summary>
    /// Power BI Artifact Property
    /// </summary>
    public class PowerBIArtifactProperty
    {
        [JsonPropertyName("artifactId")]
        public string ArtifactId { get; set; }

        [JsonPropertyName("artifactName")]
        public string ArtifactName { get; set; }

        [JsonPropertyName("artifactType")]
        public string ArtifactType { get; set; }

        [JsonPropertyName("propertyName")]
        public string PropertyName { get; set; }

        [JsonPropertyName("propertyValue")]
        public string PropertyValue { get; set; }
    }
}