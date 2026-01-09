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
using TheTechIdea.Beep.Connectors.Drupal.Models;

namespace TheTechIdea.Beep.Connectors.Drupal
{
    /// <summary>
    /// Drupal data source implementation using WebAPIDataSource as base class
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Drupal)]
    public class DrupalDataSource : WebAPIDataSource
    {
        // Entity endpoints mapping for Drupal JSON:API
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // Nodes (content)
            ["nodes"] = "jsonapi/node/article",
            ["nodes.get"] = "jsonapi/node/article/{id}",
            // Taxonomy terms
            ["taxonomy"] = "jsonapi/taxonomy_term/tags",
            ["taxonomy.get"] = "jsonapi/taxonomy_term/tags/{id}",
            // Users
            ["users"] = "jsonapi/user/user",
            ["users.get"] = "jsonapi/user/user/{id}",
            // Media
            ["media"] = "jsonapi/media/image",
            ["media.get"] = "jsonapi/media/image/{id}",
            // Files
            ["files"] = "jsonapi/file/file",
            ["files.get"] = "jsonapi/file/file/{id}"
        };

        // Required filters for each entity
        private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            ["nodes.get"] = new[] { "id" },
            ["taxonomy.get"] = new[] { "id" },
            ["users.get"] = new[] { "id" },
            ["media.get"] = new[] { "id" },
            ["files.get"] = new[] { "id" }
        };

        public DrupalDataSource(
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
                throw new InvalidOperationException($"Unknown Drupal entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            // Build the full URL
            var url = $"{BaseURL?.TrimEnd('/')}/{endpoint}";
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
                "nodes" => ParseNodes(json),
                "nodes.get" => ParseNode(json),
                "taxonomy" => ParseTaxonomyTerms(json),
                "taxonomy.get" => ParseTaxonomyTerm(json),
                "users" => ParseUsers(json),
                "users.get" => ParseUser(json),
                "media" => ParseMedia(json),
                "media.get" => ParseMediaItem(json),
                "files" => ParseFiles(json),
                "files.get" => ParseFile(json),
                _ => throw new NotSupportedException($"Entity '{EntityName}' parsing not implemented.")
            };
        }

        // Helper methods for parsing
        private IEnumerable<object> ParseEntityResponse(string entityName, string json)
        {
            return entityName.ToLower() switch
            {
                "nodes" => ParseNodes(json),
                "nodes.get" => ParseNode(json),
                "taxonomy" => ParseTaxonomyTerms(json),
                "taxonomy.get" => ParseTaxonomyTerm(json),
                "users" => ParseUsers(json),
                "users.get" => ParseUser(json),
                "media" => ParseMedia(json),
                "media.get" => ParseMediaItem(json),
                "files" => ParseFiles(json),
                "files.get" => ParseFile(json),
                _ => Array.Empty<object>()
            };
        }

        private IEnumerable<object> ParseNodes(string json)
        {
            var response = JsonSerializer.Deserialize<DrupalApiResponse<List<DrupalNode>>>(json);
            return response?.Data ?? new List<DrupalNode>();
        }

        private IEnumerable<object> ParseNode(string json)
        {
            var response = JsonSerializer.Deserialize<DrupalApiResponse<DrupalNode>>(json);
            return response?.Data != null ? new[] { response.Data } : Array.Empty<DrupalNode>();
        }

        private IEnumerable<object> ParseTaxonomyTerms(string json)
        {
            var response = JsonSerializer.Deserialize<DrupalApiResponse<List<DrupalTaxonomyTerm>>>(json);
            return response?.Data ?? new List<DrupalTaxonomyTerm>();
        }

        private IEnumerable<object> ParseTaxonomyTerm(string json)
        {
            var response = JsonSerializer.Deserialize<DrupalApiResponse<DrupalTaxonomyTerm>>(json);
            return response?.Data != null ? new[] { response.Data } : Array.Empty<DrupalTaxonomyTerm>();
        }

        private IEnumerable<object> ParseUsers(string json)
        {
            var response = JsonSerializer.Deserialize<DrupalApiResponse<List<DrupalUser>>>(json);
            return response?.Data ?? new List<DrupalUser>();
        }

        private IEnumerable<object> ParseUser(string json)
        {
            var response = JsonSerializer.Deserialize<DrupalApiResponse<DrupalUser>>(json);
            return response?.Data != null ? new[] { response.Data } : Array.Empty<DrupalUser>();
        }

        private IEnumerable<object> ParseMedia(string json)
        {
            var response = JsonSerializer.Deserialize<DrupalApiResponse<List<DrupalMedia>>>(json);
            return response?.Data ?? new List<DrupalMedia>();
        }

        private IEnumerable<object> ParseMediaItem(string json)
        {
            var response = JsonSerializer.Deserialize<DrupalApiResponse<DrupalMedia>>(json);
            return response?.Data != null ? new[] { response.Data } : Array.Empty<DrupalMedia>();
        }

        private IEnumerable<object> ParseFiles(string json)
        {
            var response = JsonSerializer.Deserialize<DrupalApiResponse<List<DrupalFile>>>(json);
            return response?.Data ?? new List<DrupalFile>();
        }

        private IEnumerable<object> ParseFile(string json)
        {
            var response = JsonSerializer.Deserialize<DrupalApiResponse<DrupalFile>>(json);
            return response?.Data != null ? new[] { response.Data } : Array.Empty<DrupalFile>();
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
                throw new ArgumentException($"Drupal entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
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
            ObjectType = "DrupalNode",
            PointType = EnumPointType.Function,
            Name = "GetNodes",
            Caption = "Get Drupal Nodes",
            ClassName = "DrupalDataSource",
            misc = "ReturnType: IEnumerable<DrupalNode>"
        )]
        public IEnumerable<DrupalNode> GetNodes()
        {
            return GetEntity("nodes", null).Cast<DrupalNode>();
        }

        [CommandAttribute(
            ObjectType = "DrupalNode",
            PointType = EnumPointType.Function,
            Name = "GetNode",
            Caption = "Get Drupal Node by ID",
            ClassName = "DrupalDataSource",
            misc = "ReturnType: IEnumerable<DrupalNode>"
        )]
        public IEnumerable<DrupalNode> GetNode(string id)
        {
            return GetEntity("nodes.get", new List<AppFilter> { new AppFilter { FieldName = "id", FilterValue = id } }).Cast<DrupalNode>();
        }

        [CommandAttribute(
            ObjectType = "DrupalTaxonomyTerm",
            PointType = EnumPointType.Function,
            Name = "GetTaxonomyTerms",
            Caption = "Get Drupal Taxonomy Terms",
            ClassName = "DrupalDataSource",
            misc = "ReturnType: IEnumerable<DrupalTaxonomyTerm>"
        )]
        public IEnumerable<DrupalTaxonomyTerm> GetTaxonomyTerms()
        {
            return GetEntity("taxonomy", null).Cast<DrupalTaxonomyTerm>();
        }

        [CommandAttribute(
            ObjectType = "DrupalTaxonomyTerm",
            PointType = EnumPointType.Function,
            Name = "GetTaxonomyTerm",
            Caption = "Get Drupal Taxonomy Term by ID",
            ClassName = "DrupalDataSource",
            misc = "ReturnType: IEnumerable<DrupalTaxonomyTerm>"
        )]
        public IEnumerable<DrupalTaxonomyTerm> GetTaxonomyTerm(string id)
        {
            return GetEntity("taxonomy.get", new List<AppFilter> { new AppFilter { FieldName = "id", FilterValue = id } }).Cast<DrupalTaxonomyTerm>();
        }

        [CommandAttribute(
            ObjectType = "DrupalUser",
            PointType = EnumPointType.Function,
            Name = "GetUsers",
            Caption = "Get Drupal Users",
            ClassName = "DrupalDataSource",
            misc = "ReturnType: IEnumerable<DrupalUser>"
        )]
        public IEnumerable<DrupalUser> GetUsers()
        {
            return GetEntity("users", null).Cast<DrupalUser>();
        }

        [CommandAttribute(
            ObjectType = "DrupalUser",
            PointType = EnumPointType.Function,
            Name = "GetUser",
            Caption = "Get Drupal User by ID",
            ClassName = "DrupalDataSource",
            misc = "ReturnType: IEnumerable<DrupalUser>"
        )]
        public IEnumerable<DrupalUser> GetUser(string id)
        {
            return GetEntity("users.get", new List<AppFilter> { new AppFilter { FieldName = "id", FilterValue = id } }).Cast<DrupalUser>();
        }

        [CommandAttribute(
            ObjectType = "DrupalMedia",
            PointType = EnumPointType.Function,
            Name = "GetMedia",
            Caption = "Get Drupal Media Items",
            ClassName = "DrupalDataSource",
            misc = "ReturnType: IEnumerable<DrupalMedia>"
        )]
        public IEnumerable<DrupalMedia> GetMedia()
        {
            return GetEntity("media", null).Cast<DrupalMedia>();
        }

        [CommandAttribute(
            ObjectType = "DrupalMedia",
            PointType = EnumPointType.Function,
            Name = "GetMediaItem",
            Caption = "Get Drupal Media Item by ID",
            ClassName = "DrupalDataSource",
            misc = "ReturnType: IEnumerable<DrupalMedia>"
        )]
        public IEnumerable<DrupalMedia> GetMediaItem(string id)
        {
            return GetEntity("media.get", new List<AppFilter> { new AppFilter { FieldName = "id", FilterValue = id } }).Cast<DrupalMedia>();
        }

        [CommandAttribute(
            ObjectType = "DrupalFile",
            PointType = EnumPointType.Function,
            Name = "GetFiles",
            Caption = "Get Drupal Files",
            ClassName = "DrupalDataSource",
            misc = "ReturnType: IEnumerable<DrupalFile>"
        )]
        public IEnumerable<DrupalFile> GetFiles()
        {
            return GetEntity("files", null).Cast<DrupalFile>();
        }

        [CommandAttribute(
            ObjectType = "DrupalFile",
            PointType = EnumPointType.Function,
            Name = "GetFile",
            Caption = "Get Drupal File by ID",
            ClassName = "DrupalDataSource",
            misc = "ReturnType: IEnumerable<DrupalFile>"
        )]
        public IEnumerable<DrupalFile> GetFile(string id)
        {
            return GetEntity("files.get", new List<AppFilter> { new AppFilter { FieldName = "id", FilterValue = id } }).Cast<DrupalFile>();
        }

        // POST methods for creating entities
        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Drupal, PointType = EnumPointType.Function, ObjectType = "DrupalNode", Name = "CreateNode", Caption = "Create Drupal Node", ClassType = "DrupalDataSource", Showin = ShowinType.Both, Order = 10, iconimage = "drupal.png", misc = "ReturnType: IEnumerable<DrupalNode>")]
        public async Task<IEnumerable<DrupalNode>> CreateNodeAsync(DrupalNode node)
        {
            try
            {
                var result = await PostAsync("jsonapi/node/article", node);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdNode = JsonSerializer.Deserialize<DrupalNode>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<DrupalNode> { createdNode }.Select(n => n.Attach<DrupalNode>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating node: {ex.Message}");
            }
            return new List<DrupalNode>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Drupal, PointType = EnumPointType.Function, ObjectType = "DrupalTaxonomyTerm", Name = "CreateTaxonomyTerm", Caption = "Create Drupal Taxonomy Term", ClassType = "DrupalDataSource", Showin = ShowinType.Both, Order = 11, iconimage = "drupal.png", misc = "ReturnType: IEnumerable<DrupalTaxonomyTerm>")]
        public async Task<IEnumerable<DrupalTaxonomyTerm>> CreateTaxonomyTermAsync(DrupalTaxonomyTerm term)
        {
            try
            {
                var result = await PostAsync("jsonapi/taxonomy_term/tags", term);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdTerm = JsonSerializer.Deserialize<DrupalTaxonomyTerm>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<DrupalTaxonomyTerm> { createdTerm }.Select(t => t.Attach<DrupalTaxonomyTerm>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating taxonomy term: {ex.Message}");
            }
            return new List<DrupalTaxonomyTerm>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Drupal, PointType = EnumPointType.Function, ObjectType = "DrupalMedia", Name = "CreateMedia", Caption = "Create Drupal Media", ClassType = "DrupalDataSource", Showin = ShowinType.Both, Order = 12, iconimage = "drupal.png", misc = "ReturnType: IEnumerable<DrupalMedia>")]
        public async Task<IEnumerable<DrupalMedia>> CreateMediaAsync(DrupalMedia media)
        {
            try
            {
                var result = await PostAsync("jsonapi/media/image", media);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdMedia = JsonSerializer.Deserialize<DrupalMedia>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<DrupalMedia> { createdMedia }.Select(m => m.Attach<DrupalMedia>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating media: {ex.Message}");
            }
            return new List<DrupalMedia>();
        }

        // PATCH methods for updating entities (JSON:API uses PATCH for updates)
        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Drupal, PointType = EnumPointType.Function, ObjectType = "DrupalNode", Name = "UpdateNode", Caption = "Update Drupal Node", ClassType = "DrupalDataSource", Showin = ShowinType.Both, Order = 13, iconimage = "drupal.png", misc = "ReturnType: IEnumerable<DrupalNode>")]
        public async Task<IEnumerable<DrupalNode>> UpdateNodeAsync(DrupalNode node)
        {
            try
            {
                var result = await PatchAsync($"jsonapi/node/article/{node.Id}", node);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedNode = JsonSerializer.Deserialize<DrupalNode>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<DrupalNode> { updatedNode }.Select(n => n.Attach<DrupalNode>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating node: {ex.Message}");
            }
            return new List<DrupalNode>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Drupal, PointType = EnumPointType.Function, ObjectType = "DrupalTaxonomyTerm", Name = "UpdateTaxonomyTerm", Caption = "Update Drupal Taxonomy Term", ClassType = "DrupalDataSource", Showin = ShowinType.Both, Order = 14, iconimage = "drupal.png", misc = "ReturnType: IEnumerable<DrupalTaxonomyTerm>")]
        public async Task<IEnumerable<DrupalTaxonomyTerm>> UpdateTaxonomyTermAsync(DrupalTaxonomyTerm term)
        {
            try
            {
                var result = await PatchAsync($"jsonapi/taxonomy_term/tags/{term.Id}", term);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedTerm = JsonSerializer.Deserialize<DrupalTaxonomyTerm>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<DrupalTaxonomyTerm> { updatedTerm }.Select(t => t.Attach<DrupalTaxonomyTerm>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating taxonomy term: {ex.Message}");
            }
            return new List<DrupalTaxonomyTerm>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Drupal, PointType = EnumPointType.Function, ObjectType = "DrupalMedia", Name = "UpdateMedia", Caption = "Update Drupal Media", ClassType = "DrupalDataSource", Showin = ShowinType.Both, Order = 15, iconimage = "drupal.png", misc = "ReturnType: IEnumerable<DrupalMedia>")]
        public async Task<IEnumerable<DrupalMedia>> UpdateMediaAsync(DrupalMedia media)
        {
            try
            {
                var result = await PatchAsync($"jsonapi/media/image/{media.Id}", media);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedMedia = JsonSerializer.Deserialize<DrupalMedia>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<DrupalMedia> { updatedMedia }.Select(m => m.Attach<DrupalMedia>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating media: {ex.Message}");
            }
            return new List<DrupalMedia>();
        }

        // DELETE methods for deleting entities
        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Drupal, PointType = EnumPointType.Function, ObjectType = "DrupalNode", Name = "DeleteNode", Caption = "Delete Drupal Node", ClassType = "DrupalDataSource", Showin = ShowinType.Both, Order = 16, iconimage = "drupal.png", misc = "ReturnType: bool")]
        public async Task<bool> DeleteNodeAsync(string nodeId)
        {
            try
            {
                var result = await DeleteAsync($"jsonapi/node/article/{nodeId}");
                return result.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error deleting node: {ex.Message}");
                return false;
            }
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Drupal, PointType = EnumPointType.Function, ObjectType = "DrupalTaxonomyTerm", Name = "DeleteTaxonomyTerm", Caption = "Delete Drupal Taxonomy Term", ClassType = "DrupalDataSource", Showin = ShowinType.Both, Order = 17, iconimage = "drupal.png", misc = "ReturnType: bool")]
        public async Task<bool> DeleteTaxonomyTermAsync(string termId)
        {
            try
            {
                var result = await DeleteAsync($"jsonapi/taxonomy_term/tags/{termId}");
                return result.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error deleting taxonomy term: {ex.Message}");
                return false;
            }
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Drupal, PointType = EnumPointType.Function, ObjectType = "DrupalMedia", Name = "DeleteMedia", Caption = "Delete Drupal Media", ClassType = "DrupalDataSource", Showin = ShowinType.Both, Order = 18, iconimage = "drupal.png", misc = "ReturnType: bool")]
        public async Task<bool> DeleteMediaAsync(string mediaId)
        {
            try
            {
                var result = await DeleteAsync($"jsonapi/media/image/{mediaId}");
                return result.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error deleting media: {ex.Message}");
                return false;
            }
        }
    }
}