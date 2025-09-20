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

namespace TheTechIdea.Beep.Connectors.Wave
{
    /// <summary>
    /// Wave data source implementation using WebAPIDataSource as base class
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WebApi)]
    public class WaveDataSource : WebAPIDataSource
    {
        // Entity endpoints mapping for Wave API
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // Businesses
            ["businesses"] = "businesses",
            // Customers
            ["customers"] = "customers",
            // Products
            ["products"] = "products",
            // Invoices
            ["invoices"] = "invoices",
            ["invoice_items"] = "invoices/{invoice_id}/items",
            // Payments
            ["payments"] = "payments",
            ["payment_items"] = "payments/{payment_id}/items",
            // Bills
            ["bills"] = "bills",
            ["bill_items"] = "bills/{bill_id}/items",
            // Accounts
            ["accounts"] = "accounts",
            // Transactions
            ["transactions"] = "transactions",
            // Taxes
            ["taxes"] = "taxes",
            ["sales_taxes"] = "sales_taxes",
            ["purchase_taxes"] = "purchase_taxes",
            // Users
            ["users"] = "users",
            // Attachments
            ["attachments"] = "attachments",
            // Reference data
            ["currencies"] = "currencies",
            ["countries"] = "countries",
            ["business_types"] = "business_types"
        };

        // Required filters for each entity
        private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            ["businesses"] = new[] { "business_id" },
            ["customers"] = new[] { "business_id" },
            ["products"] = new[] { "business_id" },
            ["invoices"] = new[] { "business_id" },
            ["invoice_items"] = new[] { "business_id", "invoice_id" },
            ["payments"] = new[] { "business_id" },
            ["payment_items"] = new[] { "business_id", "payment_id" },
            ["bills"] = new[] { "business_id" },
            ["bill_items"] = new[] { "business_id", "bill_id" },
            ["accounts"] = new[] { "business_id" },
            ["transactions"] = new[] { "business_id" },
            ["taxes"] = new[] { "business_id" },
            ["sales_taxes"] = new[] { "business_id" },
            ["purchase_taxes"] = new[] { "business_id" },
            ["users"] = new[] { "business_id" },
            ["attachments"] = new[] { "business_id" },
            ["currencies"] = Array.Empty<string>(),
            ["countries"] = Array.Empty<string>(),
            ["business_types"] = Array.Empty<string>()
        };

        public WaveDataSource(
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
                throw new InvalidOperationException($"Unknown Wave entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            var resolvedEndpoint = ResolveEndpoint(endpoint, q);

            using var resp = await GetAsync(resolvedEndpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, "data");
        }

        // Paged
        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            if (!EntityEndpoints.TryGetValue(EntityName, out var endpoint))
                throw new InvalidOperationException($"Unknown Wave entity '{EntityName}'.");

            var q = FiltersToQuery(filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));
            q["page_size"] = Math.Max(10, Math.Min(pageSize, 100)).ToString();

            string resolvedEndpoint = ResolveEndpoint(endpoint, q);
            string? cursor = q.TryGetValue("page", out var page) ? page : null;

            // For Wave API, we'll use simple pagination
            if (pageNumber > 1)
            {
                q["page"] = pageNumber.ToString();
            }

            var finalResp = CallWave(resolvedEndpoint, q).ConfigureAwait(false).GetAwaiter().GetResult();
            var items = ExtractArray(finalResp, "data");

            return new PagedResult
            {
                Data = items,
                PageNumber = Math.Max(1, pageNumber),
                PageSize = pageSize,
                TotalRecords = items.Count, // Wave doesn't provide total count
                TotalPages = 1, // Wave doesn't provide page count
                HasPreviousPage = pageNumber > 1,
                HasNextPage = items.Count == pageSize // Assume more pages if we got full page
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
                throw new ArgumentException($"Wave entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ResolveEndpoint(string template, Dictionary<string, string> q)
        {
            // Substitute parameters from filters if present
            var result = template;
            
            if (template.Contains("{invoice_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("invoice_id", out var invoiceId) || string.IsNullOrWhiteSpace(invoiceId))
                    throw new ArgumentException("Missing required 'invoice_id' filter for this endpoint.");
                result = result.Replace("{invoice_id}", Uri.EscapeDataString(invoiceId));
            }
            
            if (template.Contains("{payment_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("payment_id", out var paymentId) || string.IsNullOrWhiteSpace(paymentId))
                    throw new ArgumentException("Missing required 'payment_id' filter for this endpoint.");
                result = result.Replace("{payment_id}", Uri.EscapeDataString(paymentId));
            }
            
            if (template.Contains("{bill_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("bill_id", out var billId) || string.IsNullOrWhiteSpace(billId))
                    throw new ArgumentException("Missing required 'bill_id' filter for this endpoint.");
                result = result.Replace("{bill_id}", Uri.EscapeDataString(billId));
            }
            
            return result;
        }

        private async Task<HttpResponseMessage> CallWave(string endpoint, Dictionary<string, string> query, CancellationToken ct = default)
            => await GetAsync(endpoint, query, cancellationToken: ct).ConfigureAwait(false);

        // Extracts "data" (array or object) into a List<object> (Dictionary<string,object> per item).
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
                    return list; // no "data" -> empty
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