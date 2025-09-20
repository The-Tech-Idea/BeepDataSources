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

namespace TheTechIdea.Beep.Connectors.HubSpot
{
    /// <summary>
    /// HubSpot data source implementation using WebAPIDataSource as base class
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.HubSpot)]
    public class HubSpotDataSource : WebAPIDataSource
    {
        // -------- Fixed, known entities (HubSpot CRM API v3) --------
        private static readonly List<string> KnownEntities = new()
        {
            "contacts",
            "companies", 
            "deals",
            "tickets",
            "products",
            "line_items",
            "quotes",
            "owners",
            "pipelines.deals",
            "properties.contacts"
        };

        // entity -> (endpoint template, root path, required filter keys)
        private static readonly Dictionary<string, (string endpoint, string root, string[] requiredFilters)> Map
            = new(StringComparer.OrdinalIgnoreCase)
            {
                ["contacts"] = ("/crm/v3/objects/contacts", "results", Array.Empty<string>()),
                ["companies"] = ("/crm/v3/objects/companies", "results", Array.Empty<string>()),
                ["deals"] = ("/crm/v3/objects/deals", "results", Array.Empty<string>()),
                ["tickets"] = ("/crm/v3/objects/tickets", "results", Array.Empty<string>()),
                ["products"] = ("/crm/v3/objects/products", "results", Array.Empty<string>()),
                ["line_items"] = ("/crm/v3/objects/line_items", "results", Array.Empty<string>()),
                ["quotes"] = ("/crm/v3/objects/quotes", "results", Array.Empty<string>()),
                ["owners"] = ("/crm/v3/owners", "results", Array.Empty<string>()),
                ["pipelines.deals"] = ("/crm/v3/pipelines/deals", "results", Array.Empty<string>()),
                ["properties.contacts"] = ("/crm/v3/properties/contacts", "results", Array.Empty<string>()),
            };

        public HubSpotDataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor dmeEditor,
            DataSourceType databasetype,
            IErrorsInfo errorObject)
            : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
        {
            // Ensure WebAPI connection props exist (API Key configuration is provided externally)
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
                Dataconnection.ConnectionProp = new WebAPIConnectionProperties();

            // Register fixed entities
            EntitiesNames = KnownEntities.ToList();
            Entities = EntitiesNames
                .Select(n => new EntityStructure { EntityName = n, DatasourceEntityName = n })
                .ToList();
        }

        // Return the fixed list (use 'new' if base is virtual; otherwise this hides the base)
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
            if (!Map.TryGetValue(EntityName, out var m))
                throw new InvalidOperationException($"Unknown HubSpot entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            var endpoint = ResolveEndpoint(m.endpoint, q);

            using var resp = await GetAsync(endpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, m.root);
        }

        // Paged (HubSpot uses offset-based pagination with "after" parameter)
        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            if (!Map.TryGetValue(EntityName, out var m))
                throw new InvalidOperationException($"Unknown HubSpot entity '{EntityName}'.");

            var q = FiltersToQuery(filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            // HubSpot pagination
            q["limit"] = Math.Max(1, Math.Min(pageSize, 100)).ToString();
            if (pageNumber > 1)
            {
                // HubSpot uses "after" token for pagination, simplified to offset calculation
                q["after"] = ((pageNumber - 1) * pageSize).ToString();
            }

            var endpoint = ResolveEndpoint(m.endpoint, q);

            var finalResp = CallHubSpot(endpoint, q).ConfigureAwait(false).GetAwaiter().GetResult();
            var items = ExtractArray(finalResp, m.root);

            return new PagedResult
            {
                Data = items,
                PageNumber = Math.Max(1, pageNumber),
                PageSize = pageSize,
                TotalRecords = items.Count, // HubSpot doesn't provide total count in standard response
                TotalPages = pageNumber, // Conservative estimate
                HasPreviousPage = pageNumber > 1,
                HasNextPage = items.Count == pageSize // Assume more data if we got a full page
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
                q[f.FieldName] = f.FilterValue?.ToString() ?? string.Empty;
            }
            return q;
        }

        private static void RequireFilters(string entity, Dictionary<string, string> q, string[] required)
        {
            if (required == null || required.Length == 0) return;
            var missing = required.Where(r => !q.ContainsKey(r) || string.IsNullOrWhiteSpace(q[r])).ToList();
            if (missing.Count > 0)
                throw new ArgumentException($"HubSpot entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ResolveEndpoint(string template, Dictionary<string, string> q)
        {
            // Substitute any {parameter} placeholders from filters if present
            foreach (var kvp in q)
            {
                if (template.Contains($"{{{kvp.Key}}}", StringComparison.Ordinal))
                {
                    template = template.Replace($"{{{kvp.Key}}}", Uri.EscapeDataString(kvp.Value));
                }
            }
            return template;
        }

        private async Task<HttpResponseMessage> CallHubSpot(string endpoint, Dictionary<string, string> query, CancellationToken ct = default)
            => await GetAsync(endpoint, query, cancellationToken: ct).ConfigureAwait(false);

        // Extracts "results" (array) into a List<object> (Dictionary<string,object> per item).
        // If root is null, wraps whole payload as a single object.
        private static List<object> ExtractArray(HttpResponseMessage resp, string root)
        {
            var list = new List<object>();
            if (resp == null) return list;

            var json = resp.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(json);

            JsonElement node = doc.RootElement;

            if (!string.IsNullOrWhiteSpace(root))
            {
                if (!node.TryGetProperty(root, out node))
                    return list; // no "results" -> empty
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
    }
}
