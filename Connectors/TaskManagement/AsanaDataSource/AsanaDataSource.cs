using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.WebAPI;
using TheTechIdea.Beep.Connectors.TaskManagement.Asana.Models;

namespace TheTechIdea.Beep.Connectors.TaskManagement.Asana
{
    /// <summary>
    /// Asana data source implementation using WebAPIDataSource as base class
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Asana)]
    public class AsanaDataSource : WebAPIDataSource
    {
        // Asana API base URL
        private const string API_BASE_URL = "https://app.asana.com/api/1.0";

        // Entity endpoints mapping
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // Workspaces
            ["workspaces"] = "workspaces",
            ["workspaces.get"] = "workspaces/{workspaceId}",

            // Projects
            ["projects"] = "projects",
            ["projects.get"] = "projects/{projectId}",
            ["projects.workspace"] = "workspaces/{workspaceId}/projects",
            ["projects.team"] = "teams/{teamId}/projects",

            // Tasks
            ["tasks"] = "tasks",
            ["tasks.get"] = "tasks/{taskId}",
            ["tasks.project"] = "projects/{projectId}/tasks",
            ["tasks.workspace"] = "workspaces/{workspaceId}/tasks",
            ["tasks.section"] = "sections/{sectionId}/tasks",

            // Users
            ["users"] = "users",
            ["users.get"] = "users/{userId}",
            ["users.workspace"] = "workspaces/{workspaceId}/users",

            // Teams
            ["teams"] = "teams",
            ["teams.get"] = "teams/{teamId}",
            ["teams.workspace"] = "organizations/{organizationId}/teams",

            // Sections
            ["sections"] = "projects/{projectId}/sections",
            ["sections.get"] = "sections/{sectionId}",

            // Tags
            ["tags"] = "workspaces/{workspaceId}/tags",
            ["tags.get"] = "tags/{tagId}",

            // Stories (Comments/Activities)
            ["stories"] = "tasks/{taskId}/stories",
            ["stories.get"] = "stories/{storyId}",

            // Webhooks
            ["webhooks"] = "webhooks",
            ["webhooks.get"] = "webhooks/{webhookId}"
        };

        // Required filters for each entity
        private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            ["workspaces.get"] = new[] { "workspaceId" },
            ["projects.get"] = new[] { "projectId" },
            ["projects.workspace"] = new[] { "workspaceId" },
            ["projects.team"] = new[] { "teamId" },
            ["tasks.get"] = new[] { "taskId" },
            ["tasks.project"] = new[] { "projectId" },
            ["tasks.workspace"] = new[] { "workspaceId" },
            ["tasks.section"] = new[] { "sectionId" },
            ["users.get"] = new[] { "userId" },
            ["users.workspace"] = new[] { "workspaceId" },
            ["teams.get"] = new[] { "teamId" },
            ["teams.workspace"] = new[] { "organizationId" },
            ["sections"] = new[] { "projectId" },
            ["sections.get"] = new[] { "sectionId" },
            ["tags"] = new[] { "workspaceId" },
            ["tags.get"] = new[] { "tagId" },
            ["stories"] = new[] { "taskId" },
            ["stories.get"] = new[] { "storyId" },
            ["webhooks.get"] = new[] { "webhookId" }
        };

        public AsanaDataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor dmeEditor,
            DataSourceType databasetype,
            IErrorsInfo errorObject)
            : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
        {
            // Ensure WebAPI connection props exist
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
            {
                if (Dataconnection != null)
                    Dataconnection.ConnectionProp = new WebAPIConnectionProperties();
            }

            // Register entities
            EntitiesNames = EntityEndpoints.Keys.ToList();
            Entities = EntitiesNames
                .Select(n => new EntityStructure { EntityName = n, DatasourceEntityName = n })
                .ToList();
        }

        // Return the fixed list
        public new IEnumerable<string> GetEntitesList() => EntitiesNames;

        // Override to add Asana-specific headers
        protected override HttpClient CreateHttpClient()
        {
            var client = base.CreateHttpClient();

            // Asana uses Bearer token authentication
            if (Dataconnection?.ConnectionProp is WebAPIConnectionProperties webApiProps)
            {
                var token = webApiProps.Headers?.FirstOrDefault(h => h.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase)).Value?
                    .Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);

                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
            }

            return client;
        }

        // -------------------- Overrides (same signatures) --------------------

        // Sync
        public override IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            var data = GetEntityAsync(EntityName, filter).ConfigureAwait(false).GetAwaiter().GetResult();
            return data ?? Array.Empty<object>();
        }

        // Async
        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            if (!EntityEndpoints.TryGetValue(EntityName, out var endpoint))
                throw new InvalidOperationException($"Unknown Asana entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            // Build the full URL
            var url = $"{API_BASE_URL}/{endpoint}";
            url = ReplacePlaceholders(url, q);

            // Add query parameters
            var queryParams = BuildQueryParameters(q);
            if (!string.IsNullOrEmpty(queryParams))
                url += "?" + queryParams;

            // Make the request
            var response = await GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            // Parse based on entity
            return EntityName switch
            {
                "workspaces" => ParseWorkspaces(json),
                "workspaces.get" => ParseWorkspace(json),
                "projects" => ParseProjects(json),
                "projects.get" => ParseProject(json),
                "projects.workspace" => ParseProjects(json),
                "projects.team" => ParseProjects(json),
                "tasks" => ParseTasks(json),
                "tasks.get" => ParseTask(json),
                "tasks.project" => ParseTasks(json),
                "tasks.workspace" => ParseTasks(json),
                "tasks.section" => ParseTasks(json),
                "users" => ParseUsers(json),
                "users.get" => ParseUser(json),
                "users.workspace" => ParseUsers(json),
                "teams" => ParseTeams(json),
                "teams.get" => ParseTeam(json),
                "teams.workspace" => ParseTeams(json),
                "sections" => ParseSections(json),
                "sections.get" => ParseSection(json),
                "tags" => ParseTags(json),
                "tags.get" => ParseTag(json),
                "stories" => ParseStories(json),
                "stories.get" => ParseStory(json),
                "webhooks" => ParseWebhooks(json),
                "webhooks.get" => ParseWebhook(json),
                _ => throw new NotSupportedException($"Entity '{EntityName}' parsing not implemented.")
            };
        }

        // Helper methods for parsing
        private IEnumerable<object> ParseWorkspaces(string json)
        {
            var response = JsonSerializer.Deserialize<AsanaCollectionResponse<AsanaWorkspace>>(json);
            return response?.Data ?? new List<AsanaWorkspace>();
        }

        private IEnumerable<object> ParseWorkspace(string json)
        {
            var response = JsonSerializer.Deserialize<AsanaApiResponse<AsanaWorkspace>>(json);
            return response?.Data != null ? new[] { response.Data } : Array.Empty<AsanaWorkspace>();
        }

        private IEnumerable<object> ParseProjects(string json)
        {
            var response = JsonSerializer.Deserialize<AsanaCollectionResponse<AsanaProject>>(json);
            return response?.Data ?? new List<AsanaProject>();
        }

        private IEnumerable<object> ParseProject(string json)
        {
            var response = JsonSerializer.Deserialize<AsanaApiResponse<AsanaProject>>(json);
            return response?.Data != null ? new[] { response.Data } : Array.Empty<AsanaProject>();
        }

        private IEnumerable<object> ParseTasks(string json)
        {
            var response = JsonSerializer.Deserialize<AsanaCollectionResponse<AsanaTask>>(json);
            return response?.Data ?? new List<AsanaTask>();
        }

        private IEnumerable<object> ParseTask(string json)
        {
            var response = JsonSerializer.Deserialize<AsanaApiResponse<AsanaTask>>(json);
            return response?.Data != null ? new[] { response.Data } : Array.Empty<AsanaTask>();
        }

        private IEnumerable<object> ParseUsers(string json)
        {
            var response = JsonSerializer.Deserialize<AsanaCollectionResponse<AsanaUser>>(json);
            return response?.Data ?? new List<AsanaUser>();
        }

        private IEnumerable<object> ParseUser(string json)
        {
            var response = JsonSerializer.Deserialize<AsanaApiResponse<AsanaUser>>(json);
            return response?.Data != null ? new[] { response.Data } : Array.Empty<AsanaUser>();
        }

        private IEnumerable<object> ParseTeams(string json)
        {
            var response = JsonSerializer.Deserialize<AsanaCollectionResponse<AsanaTeam>>(json);
            return response?.Data ?? new List<AsanaTeam>();
        }

        private IEnumerable<object> ParseTeam(string json)
        {
            var response = JsonSerializer.Deserialize<AsanaApiResponse<AsanaTeam>>(json);
            return response?.Data != null ? new[] { response.Data } : Array.Empty<AsanaTeam>();
        }

        private IEnumerable<object> ParseSections(string json)
        {
            var response = JsonSerializer.Deserialize<AsanaCollectionResponse<AsanaSection>>(json);
            return response?.Data ?? new List<AsanaSection>();
        }

        private IEnumerable<object> ParseSection(string json)
        {
            var response = JsonSerializer.Deserialize<AsanaApiResponse<AsanaSection>>(json);
            return response?.Data != null ? new[] { response.Data } : Array.Empty<AsanaSection>();
        }

        private IEnumerable<object> ParseTags(string json)
        {
            var response = JsonSerializer.Deserialize<AsanaCollectionResponse<AsanaTag>>(json);
            return response?.Data ?? new List<AsanaTag>();
        }

        private IEnumerable<object> ParseTag(string json)
        {
            var response = JsonSerializer.Deserialize<AsanaApiResponse<AsanaTag>>(json);
            return response?.Data != null ? new[] { response.Data } : Array.Empty<AsanaTag>();
        }

        private IEnumerable<object> ParseStories(string json)
        {
            var response = JsonSerializer.Deserialize<AsanaCollectionResponse<AsanaStory>>(json);
            return response?.Data ?? new List<AsanaStory>();
        }

        private IEnumerable<object> ParseStory(string json)
        {
            var response = JsonSerializer.Deserialize<AsanaApiResponse<AsanaStory>>(json);
            return response?.Data != null ? new[] { response.Data } : Array.Empty<AsanaStory>();
        }

        private IEnumerable<object> ParseWebhooks(string json)
        {
            var response = JsonSerializer.Deserialize<AsanaCollectionResponse<AsanaWebhook>>(json);
            return response?.Data ?? new List<AsanaWebhook>();
        }

        private IEnumerable<object> ParseWebhook(string json)
        {
            var response = JsonSerializer.Deserialize<AsanaApiResponse<AsanaWebhook>>(json);
            return response?.Data != null ? new[] { response.Data } : Array.Empty<AsanaWebhook>();
        }

        // Helper methods
        private static Dictionary<string, string> FiltersToQuery(List<AppFilter> filters)
        {
            var q = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (filters == null) return q;
            foreach (var f in filters)
            {
                if (f == null || string.IsNullOrWhiteSpace(f.FieldName)) continue;
                q[f.FieldName.Trim()] = f.FilterValue?.ToString() ?? string.Empty;
            }
            return q;
        }

        private static void RequireFilters(string entity, Dictionary<string, string> q, string[] required)
        {
            if (required == null || required.Length == 0) return;
            var missing = required.Where(r => !q.ContainsKey(r) || string.IsNullOrWhiteSpace(q[r])).ToList();
            if (missing.Count > 0)
                throw new ArgumentException($"Asana entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private string ReplacePlaceholders(string url, Dictionary<string, string> q)
        {
            // Replace entity-specific placeholders
            if (url.Contains("{workspaceId}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("workspaceId", out var workspaceId) || string.IsNullOrWhiteSpace(workspaceId))
                    throw new ArgumentException("Missing required 'workspaceId' filter for this endpoint.");
                url = url.Replace("{workspaceId}", Uri.EscapeDataString(workspaceId));
            }

            if (url.Contains("{organizationId}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("organizationId", out var organizationId) || string.IsNullOrWhiteSpace(organizationId))
                    throw new ArgumentException("Missing required 'organizationId' filter for this endpoint.");
                url = url.Replace("{organizationId}", Uri.EscapeDataString(organizationId));
            }

            if (url.Contains("{projectId}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("projectId", out var projectId) || string.IsNullOrWhiteSpace(projectId))
                    throw new ArgumentException("Missing required 'projectId' filter for this endpoint.");
                url = url.Replace("{projectId}", Uri.EscapeDataString(projectId));
            }

            if (url.Contains("{taskId}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("taskId", out var taskId) || string.IsNullOrWhiteSpace(taskId))
                    throw new ArgumentException("Missing required 'taskId' filter for this endpoint.");
                url = url.Replace("{taskId}", Uri.EscapeDataString(taskId));
            }

            if (url.Contains("{userId}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("userId", out var userId) || string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentException("Missing required 'userId' filter for this endpoint.");
                url = url.Replace("{userId}", Uri.EscapeDataString(userId));
            }

            if (url.Contains("{teamId}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("teamId", out var teamId) || string.IsNullOrWhiteSpace(teamId))
                    throw new ArgumentException("Missing required 'teamId' filter for this endpoint.");
                url = url.Replace("{teamId}", Uri.EscapeDataString(teamId));
            }

            if (url.Contains("{sectionId}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("sectionId", out var sectionId) || string.IsNullOrWhiteSpace(sectionId))
                    throw new ArgumentException("Missing required 'sectionId' filter for this endpoint.");
                url = url.Replace("{sectionId}", Uri.EscapeDataString(sectionId));
            }

            if (url.Contains("{tagId}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("tagId", out var tagId) || string.IsNullOrWhiteSpace(tagId))
                    throw new ArgumentException("Missing required 'tagId' filter for this endpoint.");
                url = url.Replace("{tagId}", Uri.EscapeDataString(tagId));
            }

            if (url.Contains("{storyId}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("storyId", out var storyId) || string.IsNullOrWhiteSpace(storyId))
                    throw new ArgumentException("Missing required 'storyId' filter for this endpoint.");
                url = url.Replace("{storyId}", Uri.EscapeDataString(storyId));
            }

            if (url.Contains("{webhookId}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("webhookId", out var webhookId) || string.IsNullOrWhiteSpace(webhookId))
                    throw new ArgumentException("Missing required 'webhookId' filter for this endpoint.");
                url = url.Replace("{webhookId}", Uri.EscapeDataString(webhookId));
            }

            return url;
        }

        private static string BuildQueryParameters(Dictionary<string, string> q)
        {
            var query = new List<string>();
            foreach (var kvp in q)
            {
                if (!string.IsNullOrWhiteSpace(kvp.Value) &&
                    !kvp.Key.Contains("{") && !kvp.Key.Contains("}") &&
                    !RequiredFilters.Values.Any(required => required.Contains(kvp.Key)))
                    query.Add($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}");
            }
            return string.Join("&", query);
        }

        // -------------------- CommandAttribute Methods --------------------

        [CommandAttribute(
            ObjectType = "AsanaWorkspace",
            PointType = EnumPointType.Function,
            Name = "GetWorkspaces",
            Caption = "Get Asana Workspaces",
            ClassName = "AsanaDataSource",
            misc = "ReturnType: IEnumerable<AsanaWorkspace>"
        )]
        public IEnumerable<AsanaWorkspace> GetWorkspaces()
        {
            return GetEntity("workspaces", null).Cast<AsanaWorkspace>();
        }

        [CommandAttribute(
            ObjectType = "AsanaWorkspace",
            PointType = EnumPointType.Function,
            Name = "GetWorkspace",
            Caption = "Get Asana Workspace by ID",
            ClassName = "AsanaDataSource",
            misc = "ReturnType: IEnumerable<AsanaWorkspace>"
        )]
        public IEnumerable<AsanaWorkspace> GetWorkspace(string workspaceId)
        {
            return GetEntity("workspaces.get", new List<AppFilter> { new AppFilter { FieldName = "workspaceId", FilterValue = workspaceId } }).Cast<AsanaWorkspace>();
        }

        [CommandAttribute(
            ObjectType = "AsanaProject",
            PointType = EnumPointType.Function,
            Name = "GetProjects",
            Caption = "Get Asana Projects",
            ClassName = "AsanaDataSource",
            misc = "ReturnType: IEnumerable<AsanaProject>"
        )]
        public IEnumerable<AsanaProject> GetProjects()
        {
            return GetEntity("projects", null).Cast<AsanaProject>();
        }

        [CommandAttribute(
            ObjectType = "AsanaProject",
            PointType = EnumPointType.Function,
            Name = "GetProject",
            Caption = "Get Asana Project by ID",
            ClassName = "AsanaDataSource",
            misc = "ReturnType: IEnumerable<AsanaProject>"
        )]
        public IEnumerable<AsanaProject> GetProject(string projectId)
        {
            return GetEntity("projects.get", new List<AppFilter> { new AppFilter { FieldName = "projectId", FilterValue = projectId } }).Cast<AsanaProject>();
        }

        [CommandAttribute(
            ObjectType = "AsanaProject",
            PointType = EnumPointType.Function,
            Name = "GetWorkspaceProjects",
            Caption = "Get Projects in Workspace",
            ClassName = "AsanaDataSource",
            misc = "ReturnType: IEnumerable<AsanaProject>"
        )]
        public IEnumerable<AsanaProject> GetWorkspaceProjects(string workspaceId)
        {
            return GetEntity("projects.workspace", new List<AppFilter> { new AppFilter { FieldName = "workspaceId", FilterValue = workspaceId } }).Cast<AsanaProject>();
        }

        [CommandAttribute(
            ObjectType = "AsanaTask",
            PointType = EnumPointType.Function,
            Name = "GetTasks",
            Caption = "Get Asana Tasks",
            ClassName = "AsanaDataSource",
            misc = "ReturnType: IEnumerable<AsanaTask>"
        )]
        public IEnumerable<AsanaTask> GetTasks()
        {
            return GetEntity("tasks", null).Cast<AsanaTask>();
        }

        [CommandAttribute(
            ObjectType = "AsanaTask",
            PointType = EnumPointType.Function,
            Name = "GetTask",
            Caption = "Get Asana Task by ID",
            ClassName = "AsanaDataSource",
            misc = "ReturnType: IEnumerable<AsanaTask>"
        )]
        public IEnumerable<AsanaTask> GetTask(string taskId)
        {
            return GetEntity("tasks.get", new List<AppFilter> { new AppFilter { FieldName = "taskId", FilterValue = taskId } }).Cast<AsanaTask>();
        }

        [CommandAttribute(
            ObjectType = "AsanaTask",
            PointType = EnumPointType.Function,
            Name = "GetProjectTasks",
            Caption = "Get Tasks in Project",
            ClassName = "AsanaDataSource",
            misc = "ReturnType: IEnumerable<AsanaTask>"
        )]
        public IEnumerable<AsanaTask> GetProjectTasks(string projectId)
        {
            return GetEntity("tasks.project", new List<AppFilter> { new AppFilter { FieldName = "projectId", FilterValue = projectId } }).Cast<AsanaTask>();
        }

        [CommandAttribute(
            ObjectType = "AsanaUser",
            PointType = EnumPointType.Function,
            Name = "GetUsers",
            Caption = "Get Asana Users",
            ClassName = "AsanaDataSource",
            misc = "ReturnType: IEnumerable<AsanaUser>"
        )]
        public IEnumerable<AsanaUser> GetUsers()
        {
            return GetEntity("users", null).Cast<AsanaUser>();
        }

        [CommandAttribute(
            ObjectType = "AsanaUser",
            PointType = EnumPointType.Function,
            Name = "GetUser",
            Caption = "Get Asana User by ID",
            ClassName = "AsanaDataSource",
            misc = "ReturnType: IEnumerable<AsanaUser>"
        )]
        public IEnumerable<AsanaUser> GetUser(string userId)
        {
            return GetEntity("users.get", new List<AppFilter> { new AppFilter { FieldName = "userId", FilterValue = userId } }).Cast<AsanaUser>();
        }

        [CommandAttribute(
            ObjectType = "AsanaTeam",
            PointType = EnumPointType.Function,
            Name = "GetTeams",
            Caption = "Get Asana Teams",
            ClassName = "AsanaDataSource",
            misc = "ReturnType: IEnumerable<AsanaTeam>"
        )]
        public IEnumerable<AsanaTeam> GetTeams()
        {
            return GetEntity("teams", null).Cast<AsanaTeam>();
        }

        [CommandAttribute(
            ObjectType = "AsanaTeam",
            PointType = EnumPointType.Function,
            Name = "GetTeam",
            Caption = "Get Asana Team by ID",
            ClassName = "AsanaDataSource",
            misc = "ReturnType: IEnumerable<AsanaTeam>"
        )]
        public IEnumerable<AsanaTeam> GetTeam(string teamId)
        {
            return GetEntity("teams.get", new List<AppFilter> { new AppFilter { FieldName = "teamId", FilterValue = teamId } }).Cast<AsanaTeam>();
        }

        [CommandAttribute(
            ObjectType = "AsanaSection",
            PointType = EnumPointType.Function,
            Name = "GetSections",
            Caption = "Get Sections in Project",
            ClassName = "AsanaDataSource",
            misc = "ReturnType: IEnumerable<AsanaSection>"
        )]
        public IEnumerable<AsanaSection> GetSections(string projectId)
        {
            return GetEntity("sections", new List<AppFilter> { new AppFilter { FieldName = "projectId", FilterValue = projectId } }).Cast<AsanaSection>();
        }

        [CommandAttribute(
            ObjectType = "AsanaTag",
            PointType = EnumPointType.Function,
            Name = "GetTags",
            Caption = "Get Tags in Workspace",
            ClassName = "AsanaDataSource",
            misc = "ReturnType: IEnumerable<AsanaTag>"
        )]
        public IEnumerable<AsanaTag> GetTags(string workspaceId)
        {
            return GetEntity("tags", new List<AppFilter> { new AppFilter { FieldName = "workspaceId", FilterValue = workspaceId } }).Cast<AsanaTag>();
        }

        [CommandAttribute(
            ObjectType = "AsanaStory",
            PointType = EnumPointType.Function,
            Name = "GetStories",
            Caption = "Get Stories for Task",
            ClassName = "AsanaDataSource",
            misc = "ReturnType: IEnumerable<AsanaStory>"
        )]
        public IEnumerable<AsanaStory> GetStories(string taskId)
        {
            return GetEntity("stories", new List<AppFilter> { new AppFilter { FieldName = "taskId", FilterValue = taskId } }).Cast<AsanaStory>();
        }

        // POST methods for creating entities
        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Asana, PointType = EnumPointType.Function, ObjectType = "AsanaProject", Name = "CreateProject", Caption = "Create Asana Project", ClassType = "AsanaDataSource", Showin = ShowinType.Both, Order = 10, iconimage = "asana.png", misc = "ReturnType: IEnumerable<AsanaProject>")]
        public async Task<IEnumerable<AsanaProject>> CreateProjectAsync(AsanaProject project)
        {
            try
            {
                var url = $"{API_BASE_URL}/projects";
                var result = await PostAsync(url, project);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdProject = JsonSerializer.Deserialize<AsanaApiResponse<AsanaProject>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<AsanaProject> { createdProject.Data }.Select(p => p.Attach<AsanaProject>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating project: {ex.Message}");
            }
            return new List<AsanaProject>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Asana, PointType = EnumPointType.Function, ObjectType = "AsanaTask", Name = "CreateTask", Caption = "Create Asana Task", ClassType = "AsanaDataSource", Showin = ShowinType.Both, Order = 11, iconimage = "asana.png", misc = "ReturnType: IEnumerable<AsanaTask>")]
        public async Task<IEnumerable<AsanaTask>> CreateTaskAsync(AsanaTask task)
        {
            try
            {
                var url = $"{API_BASE_URL}/tasks";
                var result = await PostAsync(url, task);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdTask = JsonSerializer.Deserialize<AsanaApiResponse<AsanaTask>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<AsanaTask> { createdTask.Data }.Select(t => t.Attach<AsanaTask>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating task: {ex.Message}");
            }
            return new List<AsanaTask>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Asana, PointType = EnumPointType.Function, ObjectType = "AsanaSection", Name = "CreateSection", Caption = "Create Asana Section", ClassType = "AsanaDataSource", Showin = ShowinType.Both, Order = 12, iconimage = "asana.png", misc = "ReturnType: IEnumerable<AsanaSection>")]
        public async Task<IEnumerable<AsanaSection>> CreateSectionAsync(string projectId, AsanaSection section)
        {
            try
            {
                var url = $"{API_BASE_URL}/projects/{projectId}/sections";
                var result = await PostAsync(url, section);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdSection = JsonSerializer.Deserialize<AsanaApiResponse<AsanaSection>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<AsanaSection> { createdSection.Data }.Select(s => s.Attach<AsanaSection>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating section: {ex.Message}");
            }
            return new List<AsanaSection>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Asana, PointType = EnumPointType.Function, ObjectType = "AsanaTag", Name = "CreateTag", Caption = "Create Asana Tag", ClassType = "AsanaDataSource", Showin = ShowinType.Both, Order = 13, iconimage = "asana.png", misc = "ReturnType: IEnumerable<AsanaTag>")]
        public async Task<IEnumerable<AsanaTag>> CreateTagAsync(string workspaceId, AsanaTag tag)
        {
            try
            {
                var url = $"{API_BASE_URL}/workspaces/{workspaceId}/tags";
                var result = await PostAsync(url, tag);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdTag = JsonSerializer.Deserialize<AsanaApiResponse<AsanaTag>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<AsanaTag> { createdTag.Data }.Select(t => t.Attach<AsanaTag>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating tag: {ex.Message}");
            }
            return new List<AsanaTag>();
        }

        // PUT methods for updating entities
        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Asana, PointType = EnumPointType.Function, ObjectType = "AsanaProject", Name = "UpdateProject", Caption = "Update Asana Project", ClassType = "AsanaDataSource", Showin = ShowinType.Both, Order = 14, iconimage = "asana.png", misc = "ReturnType: IEnumerable<AsanaProject>")]
        public async Task<IEnumerable<AsanaProject>> UpdateProjectAsync(string projectId, AsanaProject project)
        {
            try
            {
                var url = $"{API_BASE_URL}/projects/{projectId}";
                var result = await PutAsync(url, project);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedProject = JsonSerializer.Deserialize<AsanaApiResponse<AsanaProject>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<AsanaProject> { updatedProject.Data }.Select(p => p.Attach<AsanaProject>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating project: {ex.Message}");
            }
            return new List<AsanaProject>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Asana, PointType = EnumPointType.Function, ObjectType = "AsanaTask", Name = "UpdateTask", Caption = "Update Asana Task", ClassType = "AsanaDataSource", Showin = ShowinType.Both, Order = 15, iconimage = "asana.png", misc = "ReturnType: IEnumerable<AsanaTask>")]
        public async Task<IEnumerable<AsanaTask>> UpdateTaskAsync(string taskId, AsanaTask task)
        {
            try
            {
                var url = $"{API_BASE_URL}/tasks/{taskId}";
                var result = await PutAsync(url, task);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedTask = JsonSerializer.Deserialize<AsanaApiResponse<AsanaTask>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<AsanaTask> { updatedTask.Data }.Select(t => t.Attach<AsanaTask>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating task: {ex.Message}");
            }
            return new List<AsanaTask>();
        }

        // DELETE methods for deleting entities
        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Asana, PointType = EnumPointType.Function, ObjectType = "AsanaProject", Name = "DeleteProject", Caption = "Delete Asana Project", ClassType = "AsanaDataSource", Showin = ShowinType.Both, Order = 16, iconimage = "asana.png", misc = "ReturnType: bool")]
        public async Task<bool> DeleteProjectAsync(string projectId)
        {
            try
            {
                var url = $"{API_BASE_URL}/projects/{projectId}";
                var result = await DeleteAsync(url);
                return result.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error deleting project: {ex.Message}");
                return false;
            }
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Asana, PointType = EnumPointType.Function, ObjectType = "AsanaTask", Name = "DeleteTask", Caption = "Delete Asana Task", ClassType = "AsanaDataSource", Showin = ShowinType.Both, Order = 17, iconimage = "asana.png", misc = "ReturnType: bool")]
        public async Task<bool> DeleteTaskAsync(string taskId)
        {
            try
            {
                var url = $"{API_BASE_URL}/tasks/{taskId}";
                var result = await DeleteAsync(url);
                return result.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error deleting task: {ex.Message}");
                return false;
            }
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Asana, PointType = EnumPointType.Function, ObjectType = "AsanaSection", Name = "DeleteSection", Caption = "Delete Asana Section", ClassType = "AsanaDataSource", Showin = ShowinType.Both, Order = 18, iconimage = "asana.png", misc = "ReturnType: bool")]
        public async Task<bool> DeleteSectionAsync(string sectionId)
        {
            try
            {
                var url = $"{API_BASE_URL}/sections/{sectionId}";
                var result = await DeleteAsync(url);
                return result.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error deleting section: {ex.Message}");
                return false;
            }
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Asana, PointType = EnumPointType.Function, ObjectType = "AsanaTag", Name = "DeleteTag", Caption = "Delete Asana Tag", ClassType = "AsanaDataSource", Showin = ShowinType.Both, Order = 19, iconimage = "asana.png", misc = "ReturnType: bool")]
        public async Task<bool> DeleteTagAsync(string tagId)
        {
            try
            {
                var url = $"{API_BASE_URL}/tags/{tagId}";
                var result = await DeleteAsync(url);
                return result.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error deleting tag: {ex.Message}");
                return false;
            }
        }
    }
}