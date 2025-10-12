using System.Text.Json.Serialization;

namespace TheTechIdea.Beep.Connectors.Tableau.Models
{
    // Tableau API Models
    public class TableauSignInRequest
    {
        [JsonPropertyName("credentials")]
        public TableauCredentials Credentials { get; set; }
    }

    public class TableauCredentials
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("password")]
        public string Password { get; set; }

        [JsonPropertyName("site")]
        public TableauSite Site { get; set; }
    }

    public class TableauSite
    {
        [JsonPropertyName("contentUrl")]
        public string ContentUrl { get; set; }
    }

    public class TableauSignInResponse
    {
        [JsonPropertyName("credentials")]
        public TableauCredentialsResponse Credentials { get; set; }
    }

    public class TableauCredentialsResponse
    {
        [JsonPropertyName("site")]
        public TableauSiteResponse Site { get; set; }

        [JsonPropertyName("user")]
        public TableauUser User { get; set; }

        [JsonPropertyName("token")]
        public string Token { get; set; }
    }

    public class TableauSiteResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("contentUrl")]
        public string ContentUrl { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("adminMode")]
        public string AdminMode { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }
    }

    public class TableauUser
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("fullName")]
        public string FullName { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("siteRole")]
        public string SiteRole { get; set; }

        [JsonPropertyName("lastLogin")]
        public DateTime? LastLogin { get; set; }
    }

    public class TableauWorkbook
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("contentUrl")]
        public string ContentUrl { get; set; }

        [JsonPropertyName("webpageUrl")]
        public string WebpageUrl { get; set; }

        [JsonPropertyName("showTabs")]
        public bool ShowTabs { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("refreshableExtracts")]
        public bool RefreshableExtracts { get; set; }

        [JsonPropertyName("extractsRefreshedAt")]
        public DateTime? ExtractsRefreshedAt { get; set; }

        [JsonPropertyName("encryption")]
        public string Encryption { get; set; }

        [JsonPropertyName("defaultViewId")]
        public string DefaultViewId { get; set; }

        [JsonPropertyName("owner")]
        public TableauUser Owner { get; set; }

        [JsonPropertyName("project")]
        public TableauProject Project { get; set; }

        [JsonPropertyName("tags")]
        public TableauTags Tags { get; set; }

        [JsonPropertyName("views")]
        public TableauViews Views { get; set; }
    }

    public class TableauProject
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("contentPermissions")]
        public string ContentPermissions { get; set; }

        [JsonPropertyName("parentProjectId")]
        public string ParentProjectId { get; set; }

        [JsonPropertyName("owner")]
        public TableauUser Owner { get; set; }
    }

    public class TableauTags
    {
        [JsonPropertyName("tag")]
        public List<TableauTag>? Tag { get; set; }
    }

    public class TableauTag
    {
        [JsonPropertyName("label")]
        public string Label { get; set; }
    }

    public class TableauViews
    {
        [JsonPropertyName("view")]
        public List<TableauView>? View { get; set; }
    }

    public class TableauView
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("contentUrl")]
        public string ContentUrl { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("sheetType")]
        public string SheetType { get; set; }

        [JsonPropertyName("usage")]
        public TableauUsage Usage { get; set; }
    }

    public class TableauUsage
    {
        [JsonPropertyName("hitCountTotal")]
        public int HitCountTotal { get; set; }

        [JsonPropertyName("hitCountLastMonth")]
        public int HitCountLastMonth { get; set; }

        [JsonPropertyName("hitCountLastWeek")]
        public int HitCountLastWeek { get; set; }

        [JsonPropertyName("hitCountLastDay")]
        public int HitCountLastDay { get; set; }
    }

    public class TableauDataSource
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("contentUrl")]
        public string ContentUrl { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("isCertified")]
        public bool IsCertified { get; set; }

        [JsonPropertyName("certificationNote")]
        public string CertificationNote { get; set; }

        [JsonPropertyName("useRemoteQueryAgent")]
        public bool UseRemoteQueryAgent { get; set; }

        [JsonPropertyName("webpageUrl")]
        public string WebpageUrl { get; set; }

        [JsonPropertyName("encryptExtracts")]
        public string EncryptExtracts { get; set; }

        [JsonPropertyName("hasExtracts")]
        public bool HasExtracts { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }

        [JsonPropertyName("refreshableExtracts")]
        public bool RefreshableExtracts { get; set; }

        [JsonPropertyName("extractsRefreshedAt")]
        public DateTime? ExtractsRefreshedAt { get; set; }

        [JsonPropertyName("owner")]
        public TableauUser Owner { get; set; }

        [JsonPropertyName("project")]
        public TableauProject Project { get; set; }

        [JsonPropertyName("tags")]
        public TableauTags Tags { get; set; }
    }

    public class TableauDashboard
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("path")]
        public string Path { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("sheetType")]
        public string SheetType { get; set; }

        [JsonPropertyName("workbook")]
        public TableauWorkbook Workbook { get; set; }

        [JsonPropertyName("usage")]
        public TableauUsage Usage { get; set; }
    }

    public class TableauJob
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("jobType")]
        public string JobType { get; set; }

        [JsonPropertyName("progress")]
        public int Progress { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("startedAt")]
        public DateTime? StartedAt { get; set; }

        [JsonPropertyName("endedAt")]
        public DateTime? EndedAt { get; set; }

        [JsonPropertyName("finishCode")]
        public int FinishCode { get; set; }

        [JsonPropertyName("notes")]
        public string Notes { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("subtitle")]
        public string Subtitle { get; set; }

        [JsonPropertyName("arguments")]
        public TableauJobArguments Arguments { get; set; }
    }

    public class TableauJobArguments
    {
        [JsonPropertyName("workbookId")]
        public string WorkbookId { get; set; }

        [JsonPropertyName("datasourceId")]
        public string DatasourceId { get; set; }

        [JsonPropertyName("targetSiteId")]
        public string TargetSiteId { get; set; }
    }

    public class TableauSubscription
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("subject")]
        public string Subject { get; set; }

        [JsonPropertyName("content")]
        public TableauSubscriptionContent Content { get; set; }

        [JsonPropertyName("schedule")]
        public TableauSchedule Schedule { get; set; }

        [JsonPropertyName("user")]
        public TableauUser User { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("suspended")]
        public bool Suspended { get; set; }
    }

    public class TableauSubscriptionContent
    {
        [JsonPropertyName("workbook")]
        public TableauWorkbook Workbook { get; set; }

        [JsonPropertyName("view")]
        public TableauView View { get; set; }
    }

    public class TableauSchedule
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }

        [JsonPropertyName("priority")]
        public int Priority { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("frequency")]
        public string Frequency { get; set; }

        [JsonPropertyName("nextRunAt")]
        public DateTime? NextRunAt { get; set; }
    }

    public class TableauGroup
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("domain")]
        public TableauDomain Domain { get; set; }

        [JsonPropertyName("import")]
        public TableauImport Import { get; set; }
    }

    public class TableauDomain
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }
    }

    public class TableauImport
    {
        [JsonPropertyName("domainName")]
        public string DomainName { get; set; }

        [JsonPropertyName("domainId")]
        public string DomainId { get; set; }

        [JsonPropertyName("siteRole")]
        public string SiteRole { get; set; }
    }

    public class TableauPaginationResponse<T>
    {
        [JsonPropertyName("pagination")]
        public TableauPagination Pagination { get; set; }

        [JsonPropertyName("workbooks")]
        public TableauWorkbooks Workbooks { get; set; }

        [JsonPropertyName("datasources")]
        public TableauDatasources Datasources { get; set; }

        [JsonPropertyName("views")]
        public TableauViews Views { get; set; }

        [JsonPropertyName("users")]
        public TableauUsers Users { get; set; }

        [JsonPropertyName("groups")]
        public TableauGroups Groups { get; set; }

        [JsonPropertyName("projects")]
        public TableauProjects Projects { get; set; }

        [JsonPropertyName("jobs")]
        public TableauJobs Jobs { get; set; }

        [JsonPropertyName("subscriptions")]
        public TableauSubscriptions Subscriptions { get; set; }

        [JsonPropertyName("schedules")]
        public TableauSchedules Schedules { get; set; }
    }

    public class TableauPagination
    {
        [JsonPropertyName("pageNumber")]
        public int PageNumber { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }

        [JsonPropertyName("totalAvailable")]
        public int TotalAvailable { get; set; }
    }

    public class TableauWorkbooks
    {
        [JsonPropertyName("workbook")]
        public List<TableauWorkbook>? Workbook { get; set; }
    }

    public class TableauDatasources
    {
        [JsonPropertyName("datasource")]
        public List<TableauDataSource>? Datasource { get; set; }
    }

    public class TableauUsers
    {
        [JsonPropertyName("user")]
        public List<TableauUser>? User { get; set; }
    }

    public class TableauGroups
    {
        [JsonPropertyName("group")]
        public List<TableauGroup>? Group { get; set; }
    }

    public class TableauProjects
    {
        [JsonPropertyName("project")]
        public List<TableauProject>? Project { get; set; }
    }

    public class TableauJobs
    {
        [JsonPropertyName("job")]
        public List<TableauJob>? Job { get; set; }
    }

    public class TableauSubscriptions
    {
        [JsonPropertyName("subscription")]
        public List<TableauSubscription>? Subscription { get; set; }
    }

    public class TableauSchedules
    {
        [JsonPropertyName("schedule")]
        public List<TableauSchedule>? Schedule { get; set; }
    }

    public class TableauError
    {
        [JsonPropertyName("summary")]
        public string Summary { get; set; }

        [JsonPropertyName("detail")]
        public string Detail { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; }
    }

    public class TableauErrorResponse
    {
        [JsonPropertyName("error")]
        public TableauError Error { get; set; }
    }
}