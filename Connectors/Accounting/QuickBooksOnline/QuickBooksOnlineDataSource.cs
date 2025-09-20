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

namespace TheTechIdea.Beep.Connectors.QuickBooksOnline
{
    /// <summary>
    /// QuickBooks Online data source implementation using WebAPIDataSource as base class
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.QuickBooks)]
    public class QuickBooksOnlineDataSource : WebAPIDataSource
    {
        // Entity endpoints mapping for QuickBooks Online API
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // Customers
            ["customers"] = "customers",
            ["customers.query"] = "customers",
            // Invoices
            ["invoices"] = "invoices",
            ["invoices.query"] = "invoices",
            // Bills
            ["bills"] = "bills",
            ["bills.query"] = "bills",
            // Accounts
            ["accounts"] = "accounts",
            ["accounts.query"] = "accounts",
            // Items
            ["items"] = "items",
            ["items.query"] = "items",
            // Employees
            ["employees"] = "employees",
            ["employees.query"] = "employees",
            // Vendors
            ["vendors"] = "vendors",
            ["vendors.query"] = "vendors",
            // Payments
            ["payments"] = "payments",
            ["payments.query"] = "payments",
            // Estimates
            ["estimates"] = "estimates",
            ["estimates.query"] = "estimates",
            // Purchase Orders
            ["purchaseorders"] = "purchaseorders",
            ["purchaseorders.query"] = "purchaseorders",
            // Company Info
            ["companyinfo"] = "companyinfo",
            // Tax Codes
            ["taxcodes"] = "taxcodes",
            ["taxcodes.query"] = "taxcodes"
        };

        // Required filters for each entity
        private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            ["customers"] = Array.Empty<string>(),
            ["customers.query"] = Array.Empty<string>(),
            ["invoices"] = Array.Empty<string>(),
            ["invoices.query"] = Array.Empty<string>(),
            ["bills"] = Array.Empty<string>(),
            ["bills.query"] = Array.Empty<string>(),
            ["accounts"] = Array.Empty<string>(),
            ["accounts.query"] = Array.Empty<string>(),
            ["items"] = Array.Empty<string>(),
            ["items.query"] = Array.Empty<string>(),
            ["employees"] = Array.Empty<string>(),
            ["employees.query"] = Array.Empty<string>(),
            ["vendors"] = Array.Empty<string>(),
            ["vendors.query"] = Array.Empty<string>(),
            ["payments"] = Array.Empty<string>(),
            ["payments.query"] = Array.Empty<string>(),
            ["estimates"] = Array.Empty<string>(),
            ["estimates.query"] = Array.Empty<string>(),
            ["purchaseorders"] = Array.Empty<string>(),
            ["purchaseorders.query"] = Array.Empty<string>(),
            ["companyinfo"] = Array.Empty<string>(),
            ["taxcodes"] = Array.Empty<string>(),
            ["taxcodes.query"] = Array.Empty<string>()
        };

        public QuickBooksOnlineDataSource(
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
                throw new InvalidOperationException($"Unknown QuickBooks Online entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            var resolvedEndpoint = ResolveEndpoint(endpoint, q);

            using var resp = await GetAsync(resolvedEndpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, "QueryResponse");
        }

        // Paged (QuickBooks uses maxresults and startposition for pagination)
        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            if (!EntityEndpoints.TryGetValue(EntityName, out var endpoint))
                throw new InvalidOperationException($"Unknown QuickBooks Online entity '{EntityName}'.");

            var q = FiltersToQuery(filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            // QuickBooks pagination parameters
            q["maxresults"] = Math.Max(1, Math.Min(pageSize, 1000)).ToString();
            if (pageNumber > 1)
            {
                q["startposition"] = ((pageNumber - 1) * pageSize + 1).ToString();
            }

            string resolvedEndpoint = ResolveEndpoint(endpoint, q);

            var resp = CallQuickBooks(resolvedEndpoint, q).ConfigureAwait(false).GetAwaiter().GetResult();
            var items = ExtractArray(resp, "QueryResponse");

            // Estimate pagination info (QuickBooks doesn't provide total count in all responses)
            int totalRecordsSoFar = (pageNumber - 1) * pageSize + items.Count;

            return new PagedResult
            {
                Data = items,
                PageNumber = Math.Max(1, pageNumber),
                PageSize = pageSize,
                TotalRecords = totalRecordsSoFar,
                TotalPages = items.Count < pageSize ? pageNumber : pageNumber + 1, // estimate
                HasPreviousPage = pageNumber > 1,
                HasNextPage = items.Count >= pageSize
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
                throw new ArgumentException($"QuickBooks Online entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ResolveEndpoint(string template, Dictionary<string, string> q)
        {
            // QuickBooks endpoints are generally static, but we can extend this for dynamic endpoints
            return template;
        }

        private async Task<HttpResponseMessage> CallQuickBooks(string endpoint, Dictionary<string, string> query, CancellationToken ct = default)
            => await GetAsync(endpoint, query, cancellationToken: ct).ConfigureAwait(false);

        // Extracts data from QuickBooks QueryResponse structure
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
                    return list; // no QueryResponse -> empty
            }

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // QuickBooks returns data in arrays like Customer[], Invoice[], etc.
            foreach (var property in node.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in property.Value.EnumerateArray())
                    {
                        var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(el.GetRawText(), opts);
                        if (obj != null) list.Add(obj);
                    }
                }
                else if (property.Value.ValueKind == JsonValueKind.Object)
                {
                    var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(property.Value.GetRawText(), opts);
                    if (obj != null) list.Add(obj);
                }
            }

            return list;
        }
    }
}