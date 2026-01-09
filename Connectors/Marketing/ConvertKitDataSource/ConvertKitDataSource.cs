using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.WebAPI;
using TheTechIdea.Beep.Connectors.Marketing.ConvertKitDataSource.Models;

namespace TheTechIdea.Beep.Connectors.Marketing.ConvertKitDataSource

{
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.ConvertKit)]
    public class ConvertKitDataSource : WebAPIDataSource
    {
        // ConvertKit API Map with endpoints, roots, and required filters
        public static readonly Dictionary<string, (string endpoint, string? root, string[] requiredFilters)> Map = new()
        {
            // Subscribers
            ["subscribers"] = ("subscribers", "subscribers", new[] { "api_secret" }),
            ["subscriber"] = ("subscribers/{subscriber_id}", "", new[] { "api_secret", "subscriber_id" }),
            ["subscriber_tags"] = ("subscribers/{subscriber_id}/tags", "tags", new[] { "api_secret", "subscriber_id" }),

            // Tags
            ["tags"] = ("tags", "tags", new[] { "api_secret" }),
            ["tag"] = ("tags/{tag_id}", "", new[] { "api_secret", "tag_id" }),
            ["tag_subscribers"] = ("tags/{tag_id}/subscribers", "subscribers", new[] { "api_secret", "tag_id" }),

            // Sequences
            ["sequences"] = ("sequences", "courses", new[] { "api_secret" }),
            ["sequence"] = ("sequences/{sequence_id}", "", new[] { "api_secret", "sequence_id" }),
            ["sequence_subscribers"] = ("sequences/{sequence_id}/subscribers", "subscribers", new[] { "api_secret", "sequence_id" }),

            // Forms
            ["forms"] = ("forms", "forms", new[] { "api_secret" }),
            ["form"] = ("forms/{form_id}", "", new[] { "api_secret", "form_id" }),
            ["form_subscriptions"] = ("forms/{form_id}/subscriptions", "subscriptions", new[] { "api_secret", "form_id" }),

            // Broadcasts
            ["broadcasts"] = ("broadcasts", "broadcasts", new[] { "api_secret" }),
            ["broadcast"] = ("broadcasts/{broadcast_id}", "", new[] { "api_secret", "broadcast_id" }),
            ["broadcast_stats"] = ("broadcasts/{broadcast_id}/stats", "", new[] { "api_secret", "broadcast_id" }),

            // Webhooks
            ["webhooks"] = ("webhooks", "webhooks", new[] { "api_secret" }),
            ["webhook"] = ("webhooks/{webhook_id}", "", new[] { "api_secret", "webhook_id" }),

            // Account
            ["account"] = ("account", "", new[] { "api_secret" })
        };

        public ConvertKitDataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor dmeEditor,
            DataSourceType databasetype,
            IErrorsInfo errorObject)
            : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
        {
            // Ensure we're on WebAPI connection properties
            if (Dataconnection != null)
                Dataconnection.ConnectionProp = new WebAPIConnectionProperties();

            // Register entities from Map
            EntitiesNames = Map.Keys.ToList();
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
            if (!Map.TryGetValue(EntityName, out var mapping))
                throw new InvalidOperationException($"Unknown ConvertKit entity '{EntityName}'.");

            var (endpoint, root, requiredFilters) = mapping;
            var q = FiltersToQuery(Filter ?? new List<AppFilter>());
            RequireFilters(EntityName, q, requiredFilters);

            var resolvedEndpoint = ResolveEndpoint(endpoint, q);

            using var resp = await GetAsync(resolvedEndpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, root);
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

        // ---------------------------- helpers ----------------------------

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
                throw new ArgumentException($"ConvertKit entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ResolveEndpoint(string template, Dictionary<string, string> q)
        {
            var result = template;
            foreach (var param in new[] { "subscriber_id", "tag_id", "sequence_id", "form_id", "broadcast_id", "webhook_id" })
            {
                if (result.Contains($"{{{param}}}", StringComparison.Ordinal))
                {
                    if (!q.TryGetValue(param, out var value) || string.IsNullOrWhiteSpace(value))
                        throw new ArgumentException($"Missing required '{param}' filter for this endpoint.");
                    result = result.Replace($"{{{param}}}", Uri.EscapeDataString(value));
                }
            }
            return result;
        }

        // Extracts array from response
        private static List<object> ExtractArray(HttpResponseMessage resp, string? root)
        {
            var list = new List<object>();
            if (resp == null) return list;

            var json = resp.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(json);

            JsonElement node = doc.RootElement;

            if (!string.IsNullOrWhiteSpace(root))
            {
                if (!node.TryGetProperty(root, out node))
                    return list;
            }

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            if (node.ValueKind == JsonValueKind.Array)
            {
                list.Capacity = node.GetArrayLength();
                foreach (var el in node.EnumerateArray())
                {
                    var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(el.GetRawText(), opts);
                    if (obj != null) list.Add(obj);
                }
            }
            else if (node.ValueKind == JsonValueKind.Object)
            {
                var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(node.GetRawText(), opts);
                if (obj != null) list.Add(obj);
            }

            return list;
        }

        // CommandAttribute methods for framework integration
        [CommandAttribute(ObjectType = nameof(ConvertKitSubscriber), PointType = EnumPointType.Function, Name = "GetSubscribers", Caption = "Get Subscribers", ClassName = "ConvertKitDataSource", misc = "GetSubscribers")]
        public IEnumerable<ConvertKitSubscriber> GetSubscribers(string apiSecret)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "api_secret", FilterValue = apiSecret } };
            return GetEntity("subscribers", filters).Cast<ConvertKitSubscriber>();
        }

        [CommandAttribute(ObjectType = nameof(ConvertKitTag), PointType = EnumPointType.Function, Name = "GetTags", Caption = "Get Tags", ClassName = "ConvertKitDataSource", misc = "GetTags")]
        public IEnumerable<ConvertKitTag> GetTags(string apiSecret)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "api_secret", FilterValue = apiSecret } };
            return GetEntity("tags", filters).Cast<ConvertKitTag>();
        }

        [CommandAttribute(ObjectType = nameof(ConvertKitSequence), PointType = EnumPointType.Function, Name = "GetSequences", Caption = "Get Sequences", ClassName = "ConvertKitDataSource", misc = "GetSequences")]
        public IEnumerable<ConvertKitSequence> GetSequences(string apiSecret)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "api_secret", FilterValue = apiSecret } };
            return GetEntity("sequences", filters).Cast<ConvertKitSequence>();
        }

        [CommandAttribute(ObjectType = nameof(ConvertKitForm), PointType = EnumPointType.Function, Name = "GetForms", Caption = "Get Forms", ClassName = "ConvertKitDataSource", misc = "GetForms")]
        public IEnumerable<ConvertKitForm> GetForms(string apiSecret)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "api_secret", FilterValue = apiSecret } };
            return GetEntity("forms", filters).Cast<ConvertKitForm>();
        }

        [CommandAttribute(ObjectType = nameof(ConvertKitBroadcast), PointType = EnumPointType.Function, Name = "GetBroadcasts", Caption = "Get Broadcasts", ClassName = "ConvertKitDataSource", misc = "GetBroadcasts")]
        public IEnumerable<ConvertKitBroadcast> GetBroadcasts(string apiSecret)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "api_secret", FilterValue = apiSecret } };
            return GetEntity("broadcasts", filters).Cast<ConvertKitBroadcast>();
        }

        // POST/PUT methods for creating and updating entities
        [CommandAttribute(ObjectType = nameof(ConvertKitSubscriber), PointType = EnumPointType.Function, Name = "CreateSubscriber", Caption = "Create Subscriber", ClassName = "ConvertKitDataSource", misc = "CreateSubscriber")]
        public async Task<ConvertKitSubscriber> CreateSubscriber(string apiSecret, ConvertKitSubscriber subscriber)
        {
            var endpoint = "subscribers";
            var filters = new List<AppFilter> { new AppFilter { FieldName = "api_secret", FilterValue = apiSecret } };
            var response = await PostAsync<ConvertKitSubscriber>(endpoint, subscriber, FiltersToDictionary(filters));
            return response;
        }

        [CommandAttribute(ObjectType = nameof(ConvertKitSubscriber), PointType = EnumPointType.Function, Name = "UpdateSubscriber", Caption = "Update Subscriber", ClassName = "ConvertKitDataSource", misc = "UpdateSubscriber")]
        public async Task<ConvertKitSubscriber> UpdateSubscriber(string apiSecret, string subscriberId, ConvertKitSubscriber subscriber)
        {
            var endpoint = $"subscribers/{subscriberId}";
            var filters = new List<AppFilter> { new AppFilter { FieldName = "api_secret", FilterValue = apiSecret } };
            var response = await PutAsync<ConvertKitSubscriber>(endpoint, subscriber, FiltersToDictionary(filters));
            return response;
        }

        [CommandAttribute(ObjectType = nameof(ConvertKitTag), PointType = EnumPointType.Function, Name = "CreateTag", Caption = "Create Tag", ClassName = "ConvertKitDataSource", misc = "CreateTag")]
        public async Task<ConvertKitTag> CreateTag(string apiSecret, ConvertKitTag tag)
        {
            var endpoint = "tags";
            var filters = new List<AppFilter> { new AppFilter { FieldName = "api_secret", FilterValue = apiSecret } };
            var response = await PostAsync<ConvertKitTag>(endpoint, tag, FiltersToDictionary(filters));
            return response;
        }

        [CommandAttribute(ObjectType = nameof(ConvertKitTag), PointType = EnumPointType.Function, Name = "UpdateTag", Caption = "Update Tag", ClassName = "ConvertKitDataSource", misc = "UpdateTag")]
        public async Task<ConvertKitTag> UpdateTag(string apiSecret, string tagId, ConvertKitTag tag)
        {
            var endpoint = $"tags/{tagId}";
            var filters = new List<AppFilter> { new AppFilter { FieldName = "api_secret", FilterValue = apiSecret } };
            var response = await PutAsync<ConvertKitTag>(endpoint, tag, FiltersToDictionary(filters));
            return response;
        }

        [CommandAttribute(ObjectType = nameof(ConvertKitSequence), PointType = EnumPointType.Function, Name = "CreateSequence", Caption = "Create Sequence", ClassName = "ConvertKitDataSource", misc = "CreateSequence")]
        public async Task<ConvertKitSequence> CreateSequence(string apiSecret, ConvertKitSequence sequence)
        {
            var endpoint = "sequences";
            var filters = new List<AppFilter> { new AppFilter { FieldName = "api_secret", FilterValue = apiSecret } };
            var response = await PostAsync<ConvertKitSequence>(endpoint, sequence, FiltersToDictionary(filters));
            return response;
        }

        [CommandAttribute(ObjectType = nameof(ConvertKitSequence), PointType = EnumPointType.Function, Name = "UpdateSequence", Caption = "Update Sequence", ClassName = "ConvertKitDataSource", misc = "UpdateSequence")]
        public async Task<ConvertKitSequence> UpdateSequence(string apiSecret, string sequenceId, ConvertKitSequence sequence)
        {
            var endpoint = $"sequences/{sequenceId}";
            var filters = new List<AppFilter> { new AppFilter { FieldName = "api_secret", FilterValue = apiSecret } };
            var response = await PutAsync<ConvertKitSequence>(endpoint, sequence, FiltersToDictionary(filters));
            return response;
        }

        [CommandAttribute(ObjectType = nameof(ConvertKitForm), PointType = EnumPointType.Function, Name = "CreateForm", Caption = "Create Form", ClassName = "ConvertKitDataSource", misc = "CreateForm")]
        public async Task<ConvertKitForm> CreateForm(string apiSecret, ConvertKitForm form)
        {
            var endpoint = "forms";
            var filters = new List<AppFilter> { new AppFilter { FieldName = "api_secret", FilterValue = apiSecret } };
            var response = await PostAsync<ConvertKitForm>(endpoint, form, FiltersToDictionary(filters));
            return response;
        }

        [CommandAttribute(ObjectType = nameof(ConvertKitForm), PointType = EnumPointType.Function, Name = "UpdateForm", Caption = "Update Form", ClassName = "ConvertKitDataSource", misc = "UpdateForm")]
        public async Task<ConvertKitForm> UpdateForm(string apiSecret, string formId, ConvertKitForm form)
        {
            var endpoint = $"forms/{formId}";
            var filters = new List<AppFilter> { new AppFilter { FieldName = "api_secret", FilterValue = apiSecret } };
            var response = await PutAsync<ConvertKitForm>(endpoint, form, FiltersToDictionary(filters));
            return response;
        }

        [CommandAttribute(ObjectType = nameof(ConvertKitBroadcast), PointType = EnumPointType.Function, Name = "CreateBroadcast", Caption = "Create Broadcast", ClassName = "ConvertKitDataSource", misc = "CreateBroadcast")]
        public async Task<ConvertKitBroadcast> CreateBroadcast(string apiSecret, ConvertKitBroadcast broadcast)
        {
            var endpoint = "broadcasts";
            var filters = new List<AppFilter> { new AppFilter { FieldName = "api_secret", FilterValue = apiSecret } };
            var response = await PostAsync<ConvertKitBroadcast>(endpoint, broadcast, FiltersToDictionary(filters));
            return response;
        }

        [CommandAttribute(ObjectType = nameof(ConvertKitBroadcast), PointType = EnumPointType.Function, Name = "UpdateBroadcast", Caption = "Update Broadcast", ClassName = "ConvertKitDataSource", misc = "UpdateBroadcast")]
        public async Task<ConvertKitBroadcast> UpdateBroadcast(string apiSecret, string broadcastId, ConvertKitBroadcast broadcast)
        {
            var endpoint = $"broadcasts/{broadcastId}";
            var filters = new List<AppFilter> { new AppFilter { FieldName = "api_secret", FilterValue = apiSecret } };
            var response = await PutAsync<ConvertKitBroadcast>(endpoint, broadcast, FiltersToDictionary(filters));
            return response;
        }

        // Helper method to convert filters to dictionary
        private static Dictionary<string, string> FiltersToDictionary(List<AppFilter> filters)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (filters == null) return dict;
            foreach (var f in filters)
            {
                if (f == null || string.IsNullOrWhiteSpace(f.FieldName)) continue;
                dict[f.FieldName.Trim()] = f.FilterValue?.ToString() ?? string.Empty;
            }
            return dict;
        }
    }
}