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

namespace TheTechIdea.Beep.Connectors.Wix
{
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Wix)]
    public class WixDataSource : WebAPIDataSource
    {
        // ---- Fixed Wix entities you want to expose ----
        private static readonly List<string> KnownEntities = new()
        {
            "Products","Orders","Collections","Contacts","Coupons","InventoryItems"
        };

        // Map entity -> (endpoint, root property)
        // NOTE: These are common Wix v1/v2/v3 styles. Adjust to your exact API/version if needed.
        private static readonly Dictionary<string, (string endpoint, string root)> Map
            = new(StringComparer.OrdinalIgnoreCase)
            {
                ["Products"] = ("stores/v1/products/query", "products"),
                ["Orders"] = ("stores/v1/orders/query", "orders"),
                ["Collections"] = ("stores/v1/collections/query", "collections"),
                ["Contacts"] = ("crm/v1/contacts/query", "contacts"),
                ["Coupons"] = ("stores/v1/coupons/query", "coupons"),
                ["InventoryItems"] = ("stores/v1/inventoryItems/query", "inventoryItems")
            };

        public WixDataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor dmeEditor,
            DataSourceType databasetype,
            IErrorsInfo errorObject)
            : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
        {
            // Ensure we’re using WebAPI connection properties (no implicit defaults here)
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
                Dataconnection.ConnectionProp = new WebAPIConnectionProperties();

            // Register fixed entities
            EntitiesNames = KnownEntities.ToList();
            Entities = EntitiesNames
                .Select(n => new EntityStructure { EntityName = n, DatasourceEntityName = n })
                .ToList();
        }

        // Return the fixed list (use 'override' if base is virtual; otherwise this hides the base)
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
                throw new InvalidOperationException($"Unknown Wix entity '{EntityName}'.");

            var body = BuildQueryPayload(Filter, limit: 100, offset: 0);
            using var resp = await PostAsync(m.endpoint, body).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, m.root);
        }

        // Paged (Wix query endpoints support paging { limit, offset })
        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            if (!Map.TryGetValue(EntityName, out var m))
                throw new InvalidOperationException($"Unknown Wix entity '{EntityName}'.");

            int limit = Math.Max(1, pageSize);
            int offset = Math.Max(0, (Math.Max(1, pageNumber) - 1) * limit);

            var body = BuildQueryPayload(filter, limit, offset);
            var resp = PostAsync(m.endpoint, body).ConfigureAwait(false).GetAwaiter().GetResult();
            var items = ExtractArray(resp, m.root);

            // Wix `/query` typically doesn’t return a grand total unless you request it separately.
            // Provide best-effort counters.
            int page = Math.Max(1, pageNumber);
            int pageCount = items.Count;
            int totalSoFar = offset + pageCount;

            return new PagedResult
            {
                Data = items,
                PageNumber = page,
                PageSize = pageSize,
                TotalRecords = totalSoFar,
                TotalPages = pageCount < limit ? page : page + 1, // heuristic
                HasPreviousPage = page > 1,
                HasNextPage = pageCount == limit                   // if we filled the page, assume there might be more
            };
        }


        // ---------------------------- helpers ----------------------------

        // Build Wix POST /query body:
        // {
        //   "query": {
        //     "filter": { ... },     // built from AppFilter (basic eq/gt/lt/in/contains mapping)
        //     "paging": { "limit": n, "offset": m },
        //     "sort":   [ { "fieldName": "...", "order": "ASC"/"DESC" } ]  // optional, not set here
        //   }
        // }
        private static object BuildQueryPayload(List<AppFilter> filters, int? limit, int? offset)
        {
            var filterObj = BuildFilter(filters);
            var pagingObj = new Dictionary<string, int>();
            if (limit.HasValue) pagingObj["limit"] = limit.Value;
            if (offset.HasValue) pagingObj["offset"] = offset.Value;

            var query = new Dictionary<string, object>();
            if (filterObj != null) query["filter"] = filterObj;
            if (pagingObj.Count > 0) query["paging"] = pagingObj;

            return new Dictionary<string, object> { ["query"] = query };
        }

        // Very basic filter mapping to Wix-style operators (adjust as needed):
        // =  -> { "$eq": value }
        // != -> { "$ne": value }
        // >  -> { "$gt": value },  >= -> { "$ge": value }
        // <  -> { "$lt": value },  <= -> { "$le": value }
        // in -> { "$in": [v1,v2,...] }   (comma/semicolon-separated)
        // contains -> { "$contains": value }
        private static Dictionary<string, object> BuildFilter(List<AppFilter> filters)
        {
            if (filters == null || filters.Count == 0) return null;

            var filter = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            foreach (var f in filters)
            {
                if (f == null || string.IsNullOrWhiteSpace(f.FieldName)) continue;

                var op = (f.Operator ?? "=").Trim().ToLowerInvariant();
                var val = f.FilterValue?.ToString();

                object literal = BuildLiteral(val);

                object expr = op switch
                {
                    "!=" or "<>" => new Dictionary<string, object> { ["$ne"] = literal },
                    ">" => new Dictionary<string, object> { ["$gt"] = literal },
                    ">=" => new Dictionary<string, object> { ["$ge"] = literal },
                    "<" => new Dictionary<string, object> { ["$lt"] = literal },
                    "<=" => new Dictionary<string, object> { ["$le"] = literal },
                    "in" => new Dictionary<string, object> { ["$in"] = SplitList(val) },
                    "contains" => new Dictionary<string, object> { ["$contains"] = literal },
                    _ => new Dictionary<string, object> { ["$eq"] = literal }
                };

                filter[f.FieldName.Trim()] = expr;
            }

            return filter.Count == 0 ? null : filter;
        }

        private static List<object> SplitList(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return new List<object>();
            var parts = s.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                         .Select(p => p.Trim())
                         .Where(p => p.Length > 0)
                         .Select(BuildLiteral)
                         .ToList();
            return parts;
        }

        // Try to coerce strings to bool/number/date, else keep as string
        private static object BuildLiteral(string s)
        {
            if (s == null) return null;

            if (bool.TryParse(s, out var b)) return b;

            if (long.TryParse(s, System.Globalization.NumberStyles.Integer,
                              System.Globalization.CultureInfo.InvariantCulture, out var l))
                return l;

            if (decimal.TryParse(s, System.Globalization.NumberStyles.Float,
                                 System.Globalization.CultureInfo.InvariantCulture, out var d))
                return d;

            if (DateTimeOffset.TryParse(s, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
                    out var dto))
                return dto.UtcDateTime.ToString("o"); // ISO8601; Wix accepts ISO strings

            return s; // fallback string
        }

        // Read JSON and extract an array under 'root'; if object, wrap as single item
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
                    return list; // root not found
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

        // Convenience wrapper to issue POST via base (auth/headers handled centrally)
        private async Task<HttpResponseMessage> PostAsync(string relativeEndpoint, object body, CancellationToken ct = default)
            => await base.PostAsync(relativeEndpoint, body, cancellationToken: ct).ConfigureAwait(false);
    }
}
