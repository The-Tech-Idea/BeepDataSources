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
using TheTechIdea.Beep.Connectors.WordPress.Models;

namespace TheTechIdea.Beep.Connectors.WordPress
{
    /// <summary>
    /// WordPress data source implementation using WebAPIDataSource as base class
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WordPress)]
    public class WordPressDataSource : WebAPIDataSource
    {
        // Entity endpoints mapping for WordPress REST API
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // Posts
            ["posts"] = "wp/v2/posts",
            ["posts.get"] = "wp/v2/posts/{id}",
            // Pages
            ["pages"] = "wp/v2/pages",
            ["pages.get"] = "wp/v2/pages/{id}",
            // Users
            ["users"] = "wp/v2/users",
            ["users.get"] = "wp/v2/users/{id}",
            // Comments
            ["comments"] = "wp/v2/comments",
            ["comments.get"] = "wp/v2/comments/{id}",
            // Categories
            ["categories"] = "wp/v2/categories",
            ["categories.get"] = "wp/v2/categories/{id}",
            // Tags
            ["tags"] = "wp/v2/tags",
            ["tags.get"] = "wp/v2/tags/{id}",
            // Media
            ["media"] = "wp/v2/media",
            ["media.get"] = "wp/v2/media/{id}"
        };

        // Required filters for each entity
        private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            ["posts.get"] = new[] { "id" },
            ["pages.get"] = new[] { "id" },
            ["users.get"] = new[] { "id" },
            ["comments.get"] = new[] { "id" },
            ["categories.get"] = new[] { "id" },
            ["tags.get"] = new[] { "id" },
            ["media.get"] = new[] { "id" }
        };

        public WordPressDataSource(
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

        // Async
        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            if (!EntityEndpoints.TryGetValue(EntityName, out var endpoint))
                throw new InvalidOperationException($"Unknown WordPress entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            // Build the full URL
            var url = $"{GetBaseUrl()}/{endpoint}";
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
                "posts" => ParsePosts(json),
                "posts.get" => ParsePost(json),
                "pages" => ParsePages(json),
                "pages.get" => ParsePage(json),
                "users" => ParseUsers(json),
                "users.get" => ParseUser(json),
                "comments" => ParseComments(json),
                "comments.get" => ParseComment(json),
                "categories" => ParseCategories(json),
                "categories.get" => ParseCategory(json),
                "tags" => ParseTags(json),
                "tags.get" => ParseTag(json),
                "media" => ParseMedia(json),
                "media.get" => ParseMediaItem(json),
                _ => throw new NotSupportedException($"Entity '{EntityName}' parsing not implemented.")
            };
        }

        // Helper methods for parsing
        private IEnumerable<object> ParsePosts(string json)
        {
            var posts = JsonSerializer.Deserialize<List<WordPressPost>>(json);
            return posts ?? new List<WordPressPost>();
        }

        private IEnumerable<object> ParsePost(string json)
        {
            var post = JsonSerializer.Deserialize<WordPressPost>(json);
            return post != null ? new[] { post } : Array.Empty<WordPressPost>();
        }

        private IEnumerable<object> ParsePages(string json)
        {
            var pages = JsonSerializer.Deserialize<List<WordPressPage>>(json);
            return pages ?? new List<WordPressPage>();
        }

        private IEnumerable<object> ParsePage(string json)
        {
            var page = JsonSerializer.Deserialize<WordPressPage>(json);
            return page != null ? new[] { page } : Array.Empty<WordPressPage>();
        }

        private IEnumerable<object> ParseUsers(string json)
        {
            var users = JsonSerializer.Deserialize<List<WordPressUser>>(json);
            return users ?? new List<WordPressUser>();
        }

        private IEnumerable<object> ParseUser(string json)
        {
            var user = JsonSerializer.Deserialize<WordPressUser>(json);
            return user != null ? new[] { user } : Array.Empty<WordPressUser>();
        }

        private IEnumerable<object> ParseComments(string json)
        {
            var comments = JsonSerializer.Deserialize<List<WordPressComment>>(json);
            return comments ?? new List<WordPressComment>();
        }

        private IEnumerable<object> ParseComment(string json)
        {
            var comment = JsonSerializer.Deserialize<WordPressComment>(json);
            return comment != null ? new[] { comment } : Array.Empty<WordPressComment>();
        }

        private IEnumerable<object> ParseCategories(string json)
        {
            var categories = JsonSerializer.Deserialize<List<WordPressCategory>>(json);
            return categories ?? new List<WordPressCategory>();
        }

        private IEnumerable<object> ParseCategory(string json)
        {
            var category = JsonSerializer.Deserialize<WordPressCategory>(json);
            return category != null ? new[] { category } : Array.Empty<WordPressCategory>();
        }

        private IEnumerable<object> ParseTags(string json)
        {
            var tags = JsonSerializer.Deserialize<List<WordPressTag>>(json);
            return tags ?? new List<WordPressTag>();
        }

        private IEnumerable<object> ParseTag(string json)
        {
            var tag = JsonSerializer.Deserialize<WordPressTag>(json);
            return tag != null ? new[] { tag } : Array.Empty<WordPressTag>();
        }

        private IEnumerable<object> ParseMedia(string json)
        {
            var media = JsonSerializer.Deserialize<List<WordPressMedia>>(json);
            return media ?? new List<WordPressMedia>();
        }

        private IEnumerable<object> ParseMediaItem(string json)
        {
            var mediaItem = JsonSerializer.Deserialize<WordPressMedia>(json);
            return mediaItem != null ? new[] { mediaItem } : Array.Empty<WordPressMedia>();
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
                throw new ArgumentException($"WordPress entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
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

        private string GetBaseUrl()
        {
            // Get the base URL from connection properties
            var baseUrl = Dataconnection?.ConnectionProp?.Url;
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException("WordPress site URL is required in connection properties.");

            // Ensure it ends with /wp-json
            if (!baseUrl.EndsWith("/wp-json", StringComparison.OrdinalIgnoreCase))
            {
                if (baseUrl.EndsWith("/"))
                    baseUrl += "wp-json";
                else
                    baseUrl += "/wp-json";
            }

            return baseUrl;
        }

        // -------------------- CommandAttribute Methods --------------------

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WordPress, PointType = EnumPointType.Function, ObjectType ="WordPressPost", ClassName = "WordPressDataSource", Showin = ShowinType.Both, misc = "List<WordPressPost>")]
        public List<WordPressPost> GetPosts()
        {
            return GetEntity("posts", new List<AppFilter>()).Cast<WordPressPost>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WordPress, PointType = EnumPointType.Function, ObjectType ="WordPressPost", ClassName = "WordPressDataSource", Showin = ShowinType.Both, misc = "WordPressPost")]
        public WordPressPost GetPost(int id)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "id", FilterValue = id.ToString(), Operator = "=" } };
            return GetEntity("posts.get", filters).Cast<WordPressPost>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WordPress, PointType = EnumPointType.Function, ObjectType ="WordPressPage", ClassName = "WordPressDataSource", Showin = ShowinType.Both, misc = "List<WordPressPage>")]
        public List<WordPressPage> GetPages()
        {
            return GetEntity("pages", new List<AppFilter>()).Cast<WordPressPage>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WordPress, PointType = EnumPointType.Function, ObjectType ="WordPressPage", ClassName = "WordPressDataSource", Showin = ShowinType.Both, misc = "WordPressPage")]
        public WordPressPage GetPage(int id)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "id", FilterValue = id.ToString(), Operator = "=" } };
            return GetEntity("pages.get", filters).Cast<WordPressPage>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WordPress, PointType = EnumPointType.Function, ObjectType ="WordPressUser", ClassName = "WordPressDataSource", Showin = ShowinType.Both, misc = "List<WordPressUser>")]
        public List<WordPressUser> GetUsers()
        {
            return GetEntity("users", new List<AppFilter>()).Cast<WordPressUser>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WordPress, PointType = EnumPointType.Function, ObjectType ="WordPressUser", ClassName = "WordPressDataSource", Showin = ShowinType.Both, misc = "WordPressUser")]
        public WordPressUser GetUser(int id)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "id", FilterValue = id.ToString(), Operator = "=" } };
            return GetEntity("users.get", filters).Cast<WordPressUser>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WordPress, PointType = EnumPointType.Function, ObjectType ="WordPressComment", ClassName = "WordPressDataSource", Showin = ShowinType.Both, misc = "List<WordPressComment>")]
        public List<WordPressComment> GetComments()
        {
            return GetEntity("comments", new List<AppFilter>()).Cast<WordPressComment>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WordPress, PointType = EnumPointType.Function, ObjectType ="WordPressComment", ClassName = "WordPressDataSource", Showin = ShowinType.Both, misc = "WordPressComment")]
        public WordPressComment GetComment(int id)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "id", FilterValue = id.ToString(), Operator = "=" } };
            return GetEntity("comments.get", filters).Cast<WordPressComment>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WordPress, PointType = EnumPointType.Function, ObjectType ="WordPressCategory", ClassName = "WordPressDataSource", Showin = ShowinType.Both, misc = "List<WordPressCategory>")]
        public List<WordPressCategory> GetCategories()
        {
            return GetEntity("categories", new List<AppFilter>()).Cast<WordPressCategory>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WordPress, PointType = EnumPointType.Function, ObjectType ="WordPressCategory", ClassName = "WordPressDataSource", Showin = ShowinType.Both, misc = "WordPressCategory")]
        public WordPressCategory GetCategory(int id)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "id", FilterValue = id.ToString(), Operator = "=" } };
            return GetEntity("categories.get", filters).Cast<WordPressCategory>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WordPress, PointType = EnumPointType.Function, ObjectType ="WordPressTag", ClassName = "WordPressDataSource", Showin = ShowinType.Both, misc = "List<WordPressTag>")]
        public List<WordPressTag> GetTags()
        {
            return GetEntity("tags", new List<AppFilter>()).Cast<WordPressTag>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WordPress, PointType = EnumPointType.Function, ObjectType ="WordPressTag", ClassName = "WordPressDataSource", Showin = ShowinType.Both, misc = "WordPressTag")]
        public WordPressTag GetTag(int id)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "id", FilterValue = id.ToString(), Operator = "=" } };
            return GetEntity("tags.get", filters).Cast<WordPressTag>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WordPress, PointType = EnumPointType.Function, ObjectType ="WordPressMedia", ClassName = "WordPressDataSource", Showin = ShowinType.Both, misc = "List<WordPressMedia>")]
        public List<WordPressMedia> GetMedia()
        {
            return GetEntity("media", new List<AppFilter>()).Cast<WordPressMedia>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WordPress, PointType = EnumPointType.Function, ObjectType ="WordPressMedia", ClassName = "WordPressDataSource", Showin = ShowinType.Both, misc = "WordPressMedia")]
        public WordPressMedia GetMediaItem(int id)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "id", FilterValue = id.ToString(), Operator = "=" } };
            return GetEntity("media.get", filters).Cast<WordPressMedia>().FirstOrDefault();
        }
    }
}