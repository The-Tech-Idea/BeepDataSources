using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.WebAPI;
using TheTechIdea.Beep.Connectors.AnyDo.Models;

namespace TheTechIdea.Beep.Connectors.AnyDo
{
    /// <summary>
    /// Any.do data source implementation using WebAPIDataSource as base class
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.AnyDo)]
    public class AnyDoDataSource : WebAPIDataSource
    {
        // Entity endpoints mapping for Any.do API
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // Tasks
            ["tasks"] = "api/v2/me/tasks",
            ["tasks.get"] = "api/v2/me/tasks/{id}",
            ["tasks.list"] = "api/v2/me/tasks?listId={list_id}",
            // Lists
            ["lists"] = "api/v2/me/lists",
            ["lists.get"] = "api/v2/me/lists/{id}",
            // Categories
            ["categories"] = "api/v2/me/categories",
            ["categories.get"] = "api/v2/me/categories/{id}",
            // User
            ["user"] = "api/v2/me"
        };

        // Required filters for each entity
        private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            ["tasks.get"] = new[] { "id" },
            ["tasks.list"] = new[] { "list_id" },
            ["lists.get"] = new[] { "id" },
            ["categories.get"] = new[] { "id" }
        };

        public AnyDoDataSource(
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
                throw new InvalidOperationException($"Unknown Any.do entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            // Build the full URL
            var url = $"https://sm-prod2.any.do/{endpoint}";
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
                "tasks" => ParseTasks(json),
                "tasks.get" => ParseTask(json),
                "tasks.list" => ParseTasks(json),
                "lists" => ParseLists(json),
                "lists.get" => ParseList(json),
                "categories" => ParseCategories(json),
                "categories.get" => ParseCategory(json),
                "user" => ParseUser(json),
                _ => throw new NotSupportedException($"Entity '{EntityName}' parsing not implemented.")
            };
        }

        // Helper methods for parsing
        private IEnumerable<object> ParseTasks(string json)
        {
            var response = JsonSerializer.Deserialize<AnyDoTasksResponse>(json);
            return response?.Tasks ?? new List<AnyDoTask>();
        }

        private IEnumerable<object> ParseTask(string json)
        {
            var task = JsonSerializer.Deserialize<AnyDoTask>(json);
            return task != null ? new[] { task } : Array.Empty<AnyDoTask>();
        }

        private IEnumerable<object> ParseLists(string json)
        {
            var response = JsonSerializer.Deserialize<AnyDoListsResponse>(json);
            return response?.Lists ?? new List<AnyDoList>();
        }

        private IEnumerable<object> ParseList(string json)
        {
            var list = JsonSerializer.Deserialize<AnyDoList>(json);
            return list != null ? new[] { list } : Array.Empty<AnyDoList>();
        }

        private IEnumerable<object> ParseCategories(string json)
        {
            var response = JsonSerializer.Deserialize<AnyDoCategoriesResponse>(json);
            return response?.Categories ?? new List<AnyDoCategory>();
        }

        private IEnumerable<object> ParseCategory(string json)
        {
            var category = JsonSerializer.Deserialize<AnyDoCategory>(json);
            return category != null ? new[] { category } : Array.Empty<AnyDoCategory>();
        }

        private IEnumerable<object> ParseUser(string json)
        {
            var user = JsonSerializer.Deserialize<AnyDoUser>(json);
            return user != null ? new[] { user } : Array.Empty<AnyDoUser>();
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
                throw new ArgumentException($"Any.do entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ReplacePlaceholders(string url, Dictionary<string, string> q)
        {
            // Substitute {id} and {list_id} from filters if present
            if (url.Contains("{id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("id", out var id) || string.IsNullOrWhiteSpace(id))
                    throw new ArgumentException("Missing required 'id' filter for this endpoint.");
                url = url.Replace("{id}", Uri.EscapeDataString(id));
            }
            if (url.Contains("{list_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("list_id", out var listId) || string.IsNullOrWhiteSpace(listId))
                    throw new ArgumentException("Missing required 'list_id' filter for this endpoint.");
                url = url.Replace("{list_id}", Uri.EscapeDataString(listId));
            }
            return url;
        }

        private static string BuildQueryParameters(Dictionary<string, string> q)
        {
            var query = new List<string>();
            foreach (var kvp in q)
            {
                if (!string.IsNullOrWhiteSpace(kvp.Value) && !kvp.Key.Contains("{") && !kvp.Key.Contains("}"))
                    query.Add($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}");
            }
            return string.Join("&", query);
        }

        // Paged
        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            var items = GetEntity(EntityName, filter).ToList();
            var totalRecords = items.Count;
            var pagedItems = items.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            return new PagedResult
            {
                Data = pagedItems,
                PageNumber = Math.Max(1, pageNumber),
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                HasPreviousPage = pageNumber > 1,
                HasNextPage = pageNumber * pageSize < totalRecords
            };
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.AnyDo, PointType = EnumPointType.Function, ObjectType = "AnyDoTask", Name = "GetTasks", Caption = "Get Tasks", ClassName = "AnyDoDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<AnyDoTask>")]
        public async Task<List<AnyDoTask>> GetTasks()
        {
            var result = await GetEntityAsync("tasks", new List<AppFilter>());
            return result.Select(item => JsonSerializer.Deserialize<AnyDoTask>(JsonSerializer.Serialize(item))).Where(x => x != null).Cast<AnyDoTask>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.AnyDo, PointType = EnumPointType.Function, ObjectType = "AnyDoTask", Name = "GetTask", Caption = "Get Task", ClassName = "AnyDoDataSource", Showin = ShowinType.Both, misc = "ReturnType: AnyDoTask")]
        public async Task<AnyDoTask> GetTask(string id)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "id", FilterValue = id, Operator = "=" } };
            var result = await GetEntityAsync("tasks.get", filters);
            return result.FirstOrDefault() as AnyDoTask;
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.AnyDo, PointType = EnumPointType.Function, ObjectType = "AnyDoTask", Name = "GetTasksByList", Caption = "Get Tasks by List", ClassName = "AnyDoDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<AnyDoTask>")]
        public async Task<List<AnyDoTask>> GetTasksByList(string listId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "list_id", FilterValue = listId, Operator = "=" } };
            var result = await GetEntityAsync("tasks.list", filters);
            return result.Select(item => JsonSerializer.Deserialize<AnyDoTask>(JsonSerializer.Serialize(item))).Where(x => x != null).Cast<AnyDoTask>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.AnyDo, PointType = EnumPointType.Function, ObjectType = "AnyDoList", Name = "GetLists", Caption = "Get Lists", ClassName = "AnyDoDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<AnyDoList>")]
        public async Task<List<AnyDoList>> GetLists()
        {
            var result = await GetEntityAsync("lists", new List<AppFilter>());
            return result.Select(item => JsonSerializer.Deserialize<AnyDoList>(JsonSerializer.Serialize(item))).Where(x => x != null).Cast<AnyDoList>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.AnyDo, PointType = EnumPointType.Function, ObjectType = "AnyDoList", Name = "GetList", Caption = "Get List", ClassName = "AnyDoDataSource", Showin = ShowinType.Both, misc = "ReturnType: AnyDoList")]
        public async Task<AnyDoList> GetList(string id)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "id", FilterValue = id, Operator = "=" } };
            var result = await GetEntityAsync("lists.get", filters);
            return result.FirstOrDefault() as AnyDoList;
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.AnyDo, PointType = EnumPointType.Function, ObjectType = "AnyDoCategory", Name = "GetCategories", Caption = "Get Categories", ClassName = "AnyDoDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<AnyDoCategory>")]
        public async Task<List<AnyDoCategory>> GetCategories()
        {
            var result = await GetEntityAsync("categories", new List<AppFilter>());
            return result.Select(item => JsonSerializer.Deserialize<AnyDoCategory>(JsonSerializer.Serialize(item))).Where(x => x != null).Cast<AnyDoCategory>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.AnyDo, PointType = EnumPointType.Function, ObjectType = "AnyDoCategory", Name = "GetCategory", Caption = "Get Category", ClassName = "AnyDoDataSource", Showin = ShowinType.Both, misc = "ReturnType: AnyDoCategory")]
        public async Task<AnyDoCategory> GetCategory(string id)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "id", FilterValue = id, Operator = "=" } };
            var result = await GetEntityAsync("categories.get", filters);
            return result.FirstOrDefault() as AnyDoCategory;
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.AnyDo, PointType = EnumPointType.Function, ObjectType = "AnyDoUser", Name = "GetUser", Caption = "Get User", ClassName = "AnyDoDataSource", Showin = ShowinType.Both, misc = "ReturnType: AnyDoUser")]
        public async Task<AnyDoUser> GetUser()
        {
            var result = await GetEntityAsync("user", new List<AppFilter>());
            return result.FirstOrDefault() as AnyDoUser;
        }

        // POST/PUT methods for creating and updating entities
        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.AnyDo, PointType = EnumPointType.Function, ObjectType = "AnyDoTask", Name = "CreateTask", Caption = "Create Task", ClassName = "AnyDoDataSource", Showin = ShowinType.Both, misc = "ReturnType: AnyDoTask")]
        public async Task<AnyDoTask> CreateTask(AnyDoTask task)
        {
            var endpoint = "api/v2/me/tasks";
            var response = await PostAsync<AnyDoTask>(endpoint, task);
            return response;
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.AnyDo, PointType = EnumPointType.Function, ObjectType = "AnyDoTask", Name = "UpdateTask", Caption = "Update Task", ClassName = "AnyDoDataSource", Showin = ShowinType.Both, misc = "ReturnType: AnyDoTask")]
        public async Task<AnyDoTask> UpdateTask(string taskId, AnyDoTask task)
        {
            var endpoint = $"api/v2/me/tasks/{taskId}";
            var response = await PutAsync<AnyDoTask>(endpoint, task);
            return response;
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.AnyDo, PointType = EnumPointType.Function, ObjectType = "AnyDoList", Name = "CreateList", Caption = "Create List", ClassName = "AnyDoDataSource", Showin = ShowinType.Both, misc = "ReturnType: AnyDoList")]
        public async Task<AnyDoList> CreateList(AnyDoList list)
        {
            var endpoint = "api/v2/me/lists";
            var response = await PostAsync<AnyDoList>(endpoint, list);
            return response;
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.AnyDo, PointType = EnumPointType.Function, ObjectType = "AnyDoList", Name = "UpdateList", Caption = "Update List", ClassName = "AnyDoDataSource", Showin = ShowinType.Both, misc = "ReturnType: AnyDoList")]
        public async Task<AnyDoList> UpdateList(string listId, AnyDoList list)
        {
            var endpoint = $"api/v2/me/lists/{listId}";
            var response = await PutAsync<AnyDoList>(endpoint, list);
            return response;
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.AnyDo, PointType = EnumPointType.Function, ObjectType = "AnyDoCategory", Name = "CreateCategory", Caption = "Create Category", ClassName = "AnyDoDataSource", Showin = ShowinType.Both, misc = "ReturnType: AnyDoCategory")]
        public async Task<AnyDoCategory> CreateCategory(AnyDoCategory category)
        {
            var endpoint = "api/v2/me/categories";
            var response = await PostAsync<AnyDoCategory>(endpoint, category);
            return response;
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.AnyDo, PointType = EnumPointType.Function, ObjectType = "AnyDoCategory", Name = "UpdateCategory", Caption = "Update Category", ClassName = "AnyDoDataSource", Showin = ShowinType.Both, misc = "ReturnType: AnyDoCategory")]
        public async Task<AnyDoCategory> UpdateCategory(string categoryId, AnyDoCategory category)
        {
            var endpoint = $"api/v2/me/categories/{categoryId}";
            var response = await PutAsync<AnyDoCategory>(endpoint, category);
            return response;
        }
    }
}