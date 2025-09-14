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

namespace TheTechIdea.Beep.Connectors.Zoho
{
    /// <summary>
    /// Zoho CRM Data Source (API v2) built on WebAPIDataSource.
    /// Configure WebAPIConnectionProperties externally (Url like https://www.zohoapis.com/crm/v2/ and OAuth2/Bearer).
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zoho)]
    public class ZohoDataSource : WebAPIDataSource
    {
        // Fixed Zoho modules (extend as required)
        private static readonly List<string> KnownEntities = new()
        {
            "Leads","Contacts","Accounts","Deals","Campaigns","Tasks","Events","Calls","Notes",
            "Products","Price_Books","Quotes","Sales_Orders","Invoices","Vendors","Users"
        };

        // Entity -> (path, root property name)
        private static readonly Dictionary<string, (string path, string root)> Map =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Leads"] = ("Leads", "data"),
                ["Contacts"] = ("Contacts", "data"),
                ["Accounts"] = ("Accounts", "data"),
                ["Deals"] = ("Deals", "data"),
                ["Campaigns"] = ("Campaigns", "data"),
                ["Tasks"] = ("Tasks", "data"),
                ["Events"] = ("Events", "data"),
                ["Calls"] = ("Calls", "data"),
                ["Notes"] = ("Notes", "data"),
                ["Products"] = ("Products", "data"),
                ["Price_Books"] = ("Price_Books", "data"),
                ["Quotes"] = ("Quotes", "data"),
                ["Sales_Orders"] = ("Sales_Orders", "data"),
                ["Invoices"] = ("Invoices", "data"),
                ["Vendors"] = ("Vendors", "data"),
                ["Users"] = ("users", "users")
            };

        public ZohoDataSource(string datasourcename, IDMLogger logger, IDMEEditor dmeEditor, DataSourceType databasetype, IErrorsInfo errorObject)
            : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
        {
            // ensure WebAPI props exist (no implicit defaults here)
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
                Dataconnection.ConnectionProp = new WebAPIConnectionProperties();

            // register fixed entities
            EntitiesNames = KnownEntities.ToList();
            Entities = EntitiesNames
                .Select(n => new EntityStructure { EntityName = n, DatasourceEntityName = n })
                .ToList();
        }

        // fixed names for this connector
        public new IEnumerable<string> GetEntitesList() => EntitiesNames;

        // -------------------- overrides (same signatures) --------------------

        public override IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            var data = GetEntityAsync(EntityName, filter).ConfigureAwait(false).GetAwaiter().GetResult();
            return data ?? Array.Empty<object>();
        }

        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            if (!Map.TryGetValue(EntityName, out var m))
                throw new InvalidOperationException($"Unknown Zoho entity '{EntityName}'.");

            // Users has a different shape and typically doesn't support /search
            if (EntityName.Equals("Users", StringComparison.OrdinalIgnoreCase))
            {
                var qUsers = FiltersToQuery(Filter);
                using var respUsers = await GetAsync(m.path, qUsers).ConfigureAwait(false);
                if (respUsers is null || !respUsers.IsSuccessStatusCode) return Array.Empty<object>();
                return ExtractArray(respUsers, m.root);
            }

            var (query, useSearch) = BuildZohoQuery(Filter);
            var endpoint = useSearch ? $"{m.path}/search" : m.path;

            using var resp = await GetAsync(endpoint, query).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();
            return ExtractArray(resp, m.root);
        }

        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            if (!Map.TryGetValue(EntityName, out var m))
                throw new InvalidOperationException($"Unknown Zoho entity '{EntityName}'.");

            int page = Math.Max(1, pageNumber);
            int size = Math.Max(1, Math.Min(pageSize, 200));

            Dictionary<string, string> q;
            string endpoint;

            if (EntityName.Equals("Users", StringComparison.OrdinalIgnoreCase))
            {
                q = FiltersToQuery(filter);
                endpoint = m.path;
            }
            else
            {
                var tmp = BuildZohoQuery(filter);
                q = tmp.query;
                endpoint = tmp.useSearch ? $"{m.path}/search" : m.path;
            }

            q["page"] = page.ToString();
            q["per_page"] = size.ToString();

            var resp = GetAsync(endpoint, q).ConfigureAwait(false).GetAwaiter().GetResult();
            var items = ExtractArray(resp, m.root);
            var (countOnPage, more) = ExtractInfo(resp); // count is this page’s count

            int pageCount = (int)(countOnPage ?? items.Count);
            int totalSoFar = (page - 1) * size + pageCount;

            return new PagedResult
            {
                Data = items,
                PageNumber = page,
                PageSize = size,
                TotalRecords = totalSoFar,                        // true grand total not exposed directly
                TotalPages = more ? page + 1 : page,            // best-effort
                HasPreviousPage = page > 1,
                HasNextPage = more
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

        /// <summary>
        /// Builds Zoho query dictionary. If filters exist, uses /search with criteria=...; otherwise plain list endpoint.
        /// </summary>
        private static (Dictionary<string, string> query, bool useSearch) BuildZohoQuery(List<AppFilter> filters)
        {
            var q = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (filters == null || filters.Count == 0)
                return (q, false); // no filters -> list

            var crit = BuildCriteria(filters);
            if (!string.IsNullOrWhiteSpace(crit))
            {
                q["criteria"] = crit;
                return (q, true);
            }
            return (q, false);
        }

        /// <summary>
        /// Convert AppFilter list into Zoho "criteria" string: (Field op value) and (Field op value) ...
        /// Supported ops: =, !=, <, <=, >, >=, contains, starts_with, ends_with, in
        /// </summary>
        private static string BuildCriteria(List<AppFilter> filters)
        {
            var parts = new List<string>();
            foreach (var f in filters)
            {
                if (f == null || string.IsNullOrWhiteSpace(f.FieldName)) continue;

                var field = f.FieldName.Trim();
                var opRaw = (f.Operator ?? "=").Trim().ToLowerInvariant();
                var value = f.FilterValue?.ToString();

                string op = opRaw switch
                {
                    "!=" or "<>" => "!=",
                    "<=" => "<=",
                    ">=" => ">=",
                    "<" => "<",
                    ">" => ">",
                    "contains" => "contains",
                    "starts_with" => "starts_with",
                    "ends_with" => "ends_with",
                    "in" => "in",
                    _ => "="
                };

                if (op == "in")
                {
                    var list = SplitList(value).Select(EscapeValue).ToArray();
                    if (list.Length > 0)
                        parts.Add($"({field}:in:{string.Join(",", list)})");
                    continue;
                }

                var lit = EscapeValue(value);
                parts.Add($"({field}:{op}:{lit})");
            }

            return parts.Count == 0 ? null : string.Join(" and ", parts);
        }

        private static string EscapeValue(string s)
        {
            if (s == null) return "\"\"";
            // try booleans/numbers/dates? Zoho /search expects literals as strings in most cases; quote safely.
            var esc = s.Replace("\\", "\\\\").Replace("\"", "\\\"");
            return $"\"{esc}\"";
        }

        private static string[] SplitList(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return Array.Empty<string>();
            return s.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => x.Length > 0)
                    .ToArray();
        }

        private async Task<HttpResponseMessage> GetAsync(string endpoint, Dictionary<string, string> query, CancellationToken ct = default)
            => await base.GetAsync(endpoint, query, cancellationToken: ct).ConfigureAwait(false);

        /// <summary>
        /// Extract array under root (e.g., "data" or "users"); wrap single object if needed.
        /// </summary>
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

        /// <summary>
        /// Extract paging info: info.count and info.more_records
        /// </summary>
        private static (long? count, bool more) ExtractInfo(HttpResponseMessage resp)
        {
            if (resp == null) return (null, false);
            try
            {
                var json = resp.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("info", out var info) && info.ValueKind == JsonValueKind.Object)
                {
                    long? count = null;
                    bool more = false;

                    if (info.TryGetProperty("count", out var c) && c.ValueKind == JsonValueKind.Number && c.TryGetInt64(out var cv))
                        count = cv;
                    if (info.TryGetProperty("more_records", out var m) && m.ValueKind == JsonValueKind.True)
                        more = true;

                    return (count, more);
                }
            }
            catch { /* ignore */ }
            return (null, false);
        }
    }
}
