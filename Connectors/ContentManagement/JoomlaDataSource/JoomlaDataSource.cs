using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.WebAPI;
using TheTechIdea.Beep.Connectors.Joomla.Models;

namespace TheTechIdea.Beep.Connectors.Joomla
{
    /// <summary>
    /// Joomla data source implementation using WebAPIDataSource as base class
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Joomla)]
    public class JoomlaDataSource : WebAPIDataSource
    {
        // Entity endpoints mapping for Joomla REST API
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // Articles (equivalent to WordPress posts)
            ["articles"] = "content/articles",
            ["articles.get"] = "content/articles/{id}",
            // Categories
            ["categories"] = "content/categories",
            ["categories.get"] = "content/categories/{id}",
            // Users
            ["users"] = "users",
            ["users.get"] = "users/{id}",
            // Tags
            ["tags"] = "content/tags",
            ["tags.get"] = "content/tags/{id}",
            // Media
            ["media"] = "media",
            ["media.get"] = "media/{id}",
            // Menus
            ["menus"] = "menus",
            ["menus.get"] = "menus/{id}"
        };

        // Required filters for each entity
        private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            ["articles.get"] = new[] { "id" },
            ["categories.get"] = new[] { "id" },
            ["users.get"] = new[] { "id" },
            ["tags.get"] = new[] { "id" },
            ["media.get"] = new[] { "id" },
            ["menus.get"] = new[] { "id" }
        };

        public JoomlaDataSource(
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
                throw new InvalidOperationException($"Unknown Joomla entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            // Build the full URL
            var url = $"https://api.joomla.org/{endpoint}";
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
                "articles" => ParseArticles(json),
                "articles.get" => ParseArticle(json),
                "categories" => ParseCategories(json),
                "categories.get" => ParseCategory(json),
                "users" => ParseUsers(json),
                "users.get" => ParseUser(json),
                "tags" => ParseTags(json),
                "tags.get" => ParseTag(json),
                "media" => ParseMedia(json),
                "media.get" => ParseMediaItem(json),
                "menus" => ParseMenus(json),
                "menus.get" => ParseMenu(json),
                _ => throw new NotSupportedException($"Entity '{EntityName}' parsing not implemented.")
            };
        }

        // Helper methods for parsing
        private IEnumerable<object> ParseEntityResponse(string entityName, string json)
        {
            return entityName.ToLower() switch
            {
                "articles" => ParseArticles(json),
                "articles.get" => ParseArticle(json),
                "categories" => ParseCategories(json),
                "categories.get" => ParseCategory(json),
                "users" => ParseUsers(json),
                "users.get" => ParseUser(json),
                "tags" => ParseTags(json),
                "tags.get" => ParseTag(json),
                "media" => ParseMedia(json),
                "media.get" => ParseMediaItem(json),
                "menus" => ParseMenus(json),
                "menus.get" => ParseMenu(json),
                _ => Array.Empty<object>()
            };
        }

        private IEnumerable<object> ParseArticles(string json)
        {
            var response = JsonSerializer.Deserialize<JoomlaApiResponse<List<JoomlaArticle>>>(json);
            return response?.Data ?? new List<JoomlaArticle>();
        }

        private IEnumerable<object> ParseArticle(string json)
        {
            var response = JsonSerializer.Deserialize<JoomlaApiResponse<JoomlaArticle>>(json);
            return response?.Data != null ? new[] { response.Data } : Array.Empty<JoomlaArticle>();
        }

        private IEnumerable<object> ParseCategories(string json)
        {
            var response = JsonSerializer.Deserialize<JoomlaApiResponse<List<JoomlaCategory>>>(json);
            return response?.Data ?? new List<JoomlaCategory>();
        }

        private IEnumerable<object> ParseCategory(string json)
        {
            var response = JsonSerializer.Deserialize<JoomlaApiResponse<JoomlaCategory>>(json);
            return response?.Data != null ? new[] { response.Data } : Array.Empty<JoomlaCategory>();
        }

        private IEnumerable<object> ParseUsers(string json)
        {
            var response = JsonSerializer.Deserialize<JoomlaApiResponse<List<JoomlaUser>>>(json);
            return response?.Data ?? new List<JoomlaUser>();
        }

        private IEnumerable<object> ParseUser(string json)
        {
            var response = JsonSerializer.Deserialize<JoomlaApiResponse<JoomlaUser>>(json);
            return response?.Data != null ? new[] { response.Data } : Array.Empty<JoomlaUser>();
        }

        private IEnumerable<object> ParseTags(string json)
        {
            var response = JsonSerializer.Deserialize<JoomlaApiResponse<List<JoomlaTag>>>(json);
            return response?.Data ?? new List<JoomlaTag>();
        }

        private IEnumerable<object> ParseTag(string json)
        {
            var response = JsonSerializer.Deserialize<JoomlaApiResponse<JoomlaTag>>(json);
            return response?.Data != null ? new[] { response.Data } : Array.Empty<JoomlaTag>();
        }

        private IEnumerable<object> ParseMedia(string json)
        {
            var response = JsonSerializer.Deserialize<JoomlaApiResponse<List<JoomlaMedia>>>(json);
            return response?.Data ?? new List<JoomlaMedia>();
        }

        private IEnumerable<object> ParseMediaItem(string json)
        {
            var response = JsonSerializer.Deserialize<JoomlaApiResponse<JoomlaMedia>>(json);
            return response?.Data != null ? new[] { response.Data } : Array.Empty<JoomlaMedia>();
        }

        private IEnumerable<object> ParseMenus(string json)
        {
            var response = JsonSerializer.Deserialize<JoomlaApiResponse<List<JoomlaMenu>>>(json);
            return response?.Data ?? new List<JoomlaMenu>();
        }

        private IEnumerable<object> ParseMenu(string json)
        {
            var response = JsonSerializer.Deserialize<JoomlaApiResponse<JoomlaMenu>>(json);
            return response?.Data != null ? new[] { response.Data } : Array.Empty<JoomlaMenu>();
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
                throw new ArgumentException($"Joomla entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ReplacePlaceholders(string url, Dictionary<string, string> q)
        {
            // Substitute {id} from filters if present
            if (url.Contains("{id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("id", out var id) || string.IsNullOrWhiteSpace(id))
                    throw new ArgumentException("Missing required 'id' filter for this endpoint.");
                url = url.Replace("{id}", Uri.EscapeDataString(id));
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

        // -------------------- CommandAttribute Methods --------------------

        [CommandAttribute(
            ObjectType = "JoomlaArticle",
            PointType = EnumPointType.Function,
            Name = "GetArticles",
            Caption = "Get Joomla Articles",
            ClassName = "JoomlaDataSource",
            misc = "ReturnType: IEnumerable<JoomlaArticle>"
        )]
        public IEnumerable<JoomlaArticle> GetArticles()
        {
            return GetEntity("articles", null).Cast<JoomlaArticle>();
        }

        [CommandAttribute(
            ObjectType = "JoomlaArticle",
            PointType = EnumPointType.Function,
            Name = "GetArticle",
            Caption = "Get Joomla Article by ID",
            ClassName = "JoomlaDataSource",
            misc = "ReturnType: IEnumerable<JoomlaArticle>"
        )]
        public IEnumerable<JoomlaArticle> GetArticle(string id)
        {
            return GetEntity("articles.get", new List<AppFilter> { new AppFilter { FieldName = "id", FilterValue = id } }).Cast<JoomlaArticle>();
        }

        [CommandAttribute(
            ObjectType = "JoomlaCategory",
            PointType = EnumPointType.Function,
            Name = "GetCategories",
            Caption = "Get Joomla Categories",
            ClassName = "JoomlaDataSource",
            misc = "ReturnType: IEnumerable<JoomlaCategory>"
        )]
        public IEnumerable<JoomlaCategory> GetCategories()
        {
            return GetEntity("categories", null).Cast<JoomlaCategory>();
        }

        [CommandAttribute(
            ObjectType = "JoomlaCategory",
            PointType = EnumPointType.Function,
            Name = "GetCategory",
            Caption = "Get Joomla Category by ID",
            ClassName = "JoomlaDataSource",
            misc = "ReturnType: IEnumerable<JoomlaCategory>"
        )]
        public IEnumerable<JoomlaCategory> GetCategory(string id)
        {
            return GetEntity("categories.get", new List<AppFilter> { new AppFilter { FieldName = "id", FilterValue = id } }).Cast<JoomlaCategory>();
        }

        [CommandAttribute(
            ObjectType = "JoomlaUser",
            PointType = EnumPointType.Function,
            Name = "GetUsers",
            Caption = "Get Joomla Users",
            ClassName = "JoomlaDataSource",
            misc = "ReturnType: IEnumerable<JoomlaUser>"
        )]
        public IEnumerable<JoomlaUser> GetUsers()
        {
            return GetEntity("users", null).Cast<JoomlaUser>();
        }

        [CommandAttribute(
            ObjectType = "JoomlaUser",
            PointType = EnumPointType.Function,
            Name = "GetUser",
            Caption = "Get Joomla User by ID",
            ClassName = "JoomlaDataSource",
            misc = "ReturnType: IEnumerable<JoomlaUser>"
        )]
        public IEnumerable<JoomlaUser> GetUser(string id)
        {
            return GetEntity("users.get", new List<AppFilter> { new AppFilter { FieldName = "id", FilterValue = id } }).Cast<JoomlaUser>();
        }

        [CommandAttribute(
            ObjectType = "JoomlaTag",
            PointType = EnumPointType.Function,
            Name = "GetTags",
            Caption = "Get Joomla Tags",
            ClassName = "JoomlaDataSource",
            misc = "ReturnType: IEnumerable<JoomlaTag>"
        )]
        public IEnumerable<JoomlaTag> GetTags()
        {
            return GetEntity("tags", null).Cast<JoomlaTag>();
        }

        [CommandAttribute(
            ObjectType = "JoomlaTag",
            PointType = EnumPointType.Function,
            Name = "GetTag",
            Caption = "Get Joomla Tag by ID",
            ClassName = "JoomlaDataSource",
            misc = "ReturnType: IEnumerable<JoomlaTag>"
        )]
        public IEnumerable<JoomlaTag> GetTag(string id)
        {
            return GetEntity("tags.get", new List<AppFilter> { new AppFilter { FieldName = "id", FilterValue = id } }).Cast<JoomlaTag>();
        }

        [CommandAttribute(
            ObjectType = "JoomlaMedia",
            PointType = EnumPointType.Function,
            Name = "GetMedia",
            Caption = "Get Joomla Media Items",
            ClassName = "JoomlaDataSource",
            misc = "ReturnType: IEnumerable<JoomlaMedia>"
        )]
        public IEnumerable<JoomlaMedia> GetMedia()
        {
            return GetEntity("media", null).Cast<JoomlaMedia>();
        }

        [CommandAttribute(
            ObjectType = "JoomlaMedia",
            PointType = EnumPointType.Function,
            Name = "GetMediaItem",
            Caption = "Get Joomla Media Item by ID",
            ClassName = "JoomlaDataSource",
            misc = "ReturnType: IEnumerable<JoomlaMedia>"
        )]
        public IEnumerable<JoomlaMedia> GetMediaItem(string id)
        {
            return GetEntity("media.get", new List<AppFilter> { new AppFilter { FieldName = "id", FilterValue = id } }).Cast<JoomlaMedia>();
        }

        [CommandAttribute(
            ObjectType = "JoomlaMenu",
            PointType = EnumPointType.Function,
            Name = "GetMenus",
            Caption = "Get Joomla Menu Items",
            ClassName = "JoomlaDataSource",
            misc = "ReturnType: IEnumerable<JoomlaMenu>"
        )]
        public IEnumerable<JoomlaMenu> GetMenus()
        {
            return GetEntity("menus", null).Cast<JoomlaMenu>();
        }

        [CommandAttribute(
            ObjectType = "JoomlaMenu",
            PointType = EnumPointType.Function,
            Name = "GetMenu",
            Caption = "Get Joomla Menu Item by ID",
            ClassName = "JoomlaDataSource",
            misc = "ReturnType: IEnumerable<JoomlaMenu>"
        )]
        public IEnumerable<JoomlaMenu> GetMenu(string id)
        {
            return GetEntity("menus.get", new List<AppFilter> { new AppFilter { FieldName = "id", FilterValue = id } }).Cast<JoomlaMenu>();
        }

        // POST methods for creating entities
        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Joomla, PointType = EnumPointType.Function, ObjectType = "JoomlaArticle", Name = "CreateArticle", Caption = "Create Joomla Article", ClassType = "JoomlaDataSource", Showin = ShowinType.Both, Order = 10, iconimage = "joomla.png", misc = "ReturnType: IEnumerable<JoomlaArticle>")]
        public async Task<IEnumerable<JoomlaArticle>> CreateArticleAsync(JoomlaArticle article)
        {
            try
            {
                var result = await PostAsync("content/articles", article);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdArticle = JsonSerializer.Deserialize<JoomlaArticle>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<JoomlaArticle> { createdArticle }.Select(a => a.Attach<JoomlaArticle>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating article: {ex.Message}");
            }
            return new List<JoomlaArticle>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Joomla, PointType = EnumPointType.Function, ObjectType = "JoomlaCategory", Name = "CreateCategory", Caption = "Create Joomla Category", ClassType = "JoomlaDataSource", Showin = ShowinType.Both, Order = 11, iconimage = "joomla.png", misc = "ReturnType: IEnumerable<JoomlaCategory>")]
        public async Task<IEnumerable<JoomlaCategory>> CreateCategoryAsync(JoomlaCategory category)
        {
            try
            {
                var result = await PostAsync("content/categories", category);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdCategory = JsonSerializer.Deserialize<JoomlaCategory>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<JoomlaCategory> { createdCategory }.Select(c => c.Attach<JoomlaCategory>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating category: {ex.Message}");
            }
            return new List<JoomlaCategory>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Joomla, PointType = EnumPointType.Function, ObjectType = "JoomlaTag", Name = "CreateTag", Caption = "Create Joomla Tag", ClassType = "JoomlaDataSource", Showin = ShowinType.Both, Order = 12, iconimage = "joomla.png", misc = "ReturnType: IEnumerable<JoomlaTag>")]
        public async Task<IEnumerable<JoomlaTag>> CreateTagAsync(JoomlaTag tag)
        {
            try
            {
                var result = await PostAsync("content/tags", tag);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdTag = JsonSerializer.Deserialize<JoomlaTag>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<JoomlaTag> { createdTag }.Select(t => t.Attach<JoomlaTag>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating tag: {ex.Message}");
            }
            return new List<JoomlaTag>();
        }

        // PUT methods for updating entities
        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Joomla, PointType = EnumPointType.Function, ObjectType = "JoomlaArticle", Name = "UpdateArticle", Caption = "Update Joomla Article", ClassType = "JoomlaDataSource", Showin = ShowinType.Both, Order = 13, iconimage = "joomla.png", misc = "ReturnType: IEnumerable<JoomlaArticle>")]
        public async Task<IEnumerable<JoomlaArticle>> UpdateArticleAsync(JoomlaArticle article)
        {
            try
            {
                var result = await PutAsync($"content/articles/{article.Id}", article);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedArticle = JsonSerializer.Deserialize<JoomlaArticle>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<JoomlaArticle> { updatedArticle }.Select(a => a.Attach<JoomlaArticle>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating article: {ex.Message}");
            }
            return new List<JoomlaArticle>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Joomla, PointType = EnumPointType.Function, ObjectType = "JoomlaCategory", Name = "UpdateCategory", Caption = "Update Joomla Category", ClassType = "JoomlaDataSource", Showin = ShowinType.Both, Order = 14, iconimage = "joomla.png", misc = "ReturnType: IEnumerable<JoomlaCategory>")]
        public async Task<IEnumerable<JoomlaCategory>> UpdateCategoryAsync(JoomlaCategory category)
        {
            try
            {
                var result = await PutAsync($"content/categories/{category.Id}", category);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedCategory = JsonSerializer.Deserialize<JoomlaCategory>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<JoomlaCategory> { updatedCategory }.Select(c => c.Attach<JoomlaCategory>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating category: {ex.Message}");
            }
            return new List<JoomlaCategory>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Joomla, PointType = EnumPointType.Function, ObjectType = "JoomlaTag", Name = "UpdateTag", Caption = "Update Joomla Tag", ClassType = "JoomlaDataSource", Showin = ShowinType.Both, Order = 15, iconimage = "joomla.png", misc = "ReturnType: IEnumerable<JoomlaTag>")]
        public async Task<IEnumerable<JoomlaTag>> UpdateTagAsync(JoomlaTag tag)
        {
            try
            {
                var result = await PutAsync($"content/tags/{tag.Id}", tag);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedTag = JsonSerializer.Deserialize<JoomlaTag>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<JoomlaTag> { updatedTag }.Select(t => t.Attach<JoomlaTag>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating tag: {ex.Message}");
            }
            return new List<JoomlaTag>();
        }

        // DELETE methods for deleting entities
        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Joomla, PointType = EnumPointType.Function, ObjectType = "JoomlaArticle", Name = "DeleteArticle", Caption = "Delete Joomla Article", ClassType = "JoomlaDataSource", Showin = ShowinType.Both, Order = 16, iconimage = "joomla.png", misc = "ReturnType: bool")]
        public async Task<bool> DeleteArticleAsync(int articleId)
        {
            try
            {
                var result = await DeleteAsync($"content/articles/{articleId}");
                return result.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error deleting article: {ex.Message}");
                return false;
            }
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Joomla, PointType = EnumPointType.Function, ObjectType = "JoomlaCategory", Name = "DeleteCategory", Caption = "Delete Joomla Category", ClassType = "JoomlaDataSource", Showin = ShowinType.Both, Order = 17, iconimage = "joomla.png", misc = "ReturnType: bool")]
        public async Task<bool> DeleteCategoryAsync(int categoryId)
        {
            try
            {
                var result = await DeleteAsync($"content/categories/{categoryId}");
                return result.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error deleting category: {ex.Message}");
                return false;
            }
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Joomla, PointType = EnumPointType.Function, ObjectType = "JoomlaTag", Name = "DeleteTag", Caption = "Delete Joomla Tag", ClassType = "JoomlaDataSource", Showin = ShowinType.Both, Order = 18, iconimage = "joomla.png", misc = "ReturnType: bool")]
        public async Task<bool> DeleteTagAsync(int tagId)
        {
            try
            {
                var result = await DeleteAsync($"content/tags/{tagId}");
                return result.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error deleting tag: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Generic Joomla API Response wrapper
    /// </summary>
    public class JoomlaApiResponse<T>
    {
        [JsonPropertyName("data")]
        public T Data { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("errors")]
        public List<string> Errors { get; set; }
    }
}