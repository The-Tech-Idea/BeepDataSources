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

namespace TheTechIdea.Beep.Connectors.FreshBooks
{
    /// <summary>
    /// FreshBooks data source implementation using WebAPIDataSource as base class
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WebApi)]
    public class FreshBooksDataSource : WebAPIDataSource
    {
        // Entity endpoints mapping for FreshBooks API
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // Clients
            ["clients"] = "accounting/account/{accountId}/users/clients",
            ["clients.query"] = "accounting/account/{accountId}/users/clients",
            // Invoices
            ["invoices"] = "accounting/account/{accountId}/invoices/invoices",
            ["invoices.query"] = "accounting/account/{accountId}/invoices/invoices",
            // Estimates
            ["estimates"] = "accounting/account/{accountId}/estimates/estimates",
            ["estimates.query"] = "accounting/account/{accountId}/estimates/estimates",
            // Expenses
            ["expenses"] = "accounting/account/{accountId}/expenses/expenses",
            ["expenses.query"] = "accounting/account/{accountId}/expenses/expenses",
            // Items
            ["items"] = "accounting/account/{accountId}/items/items",
            ["items.query"] = "accounting/account/{accountId}/items/items",
            // Payments
            ["payments"] = "accounting/account/{accountId}/payments/payments",
            ["payments.query"] = "accounting/account/{accountId}/payments/payments",
            // Projects
            ["projects"] = "projects/business/{businessId}/projects",
            ["projects.query"] = "projects/business/{businessId}/projects",
            // Time Entries
            ["timeentries"] = "projects/business/{businessId}/time_entries",
            ["timeentries.query"] = "projects/business/{businessId}/time_entries",
            // Tasks
            ["tasks"] = "projects/business/{businessId}/tasks",
            ["tasks.query"] = "projects/business/{businessId}/tasks",
            // Staff
            ["staff"] = "projects/business/{businessId}/staff",
            ["staff.query"] = "projects/business/{businessId}/staff",
            // Services
            ["services"] = "projects/business/{businessId}/services",
            ["services.query"] = "projects/business/{businessId}/services",
            // Tax Rates
            ["taxes"] = "accounting/account/{accountId}/taxes/taxes",
            ["taxes.query"] = "accounting/account/{accountId}/taxes/taxes",
            // Categories
            ["categories"] = "accounting/account/{accountId}/categories/categories",
            ["categories.query"] = "accounting/account/{accountId}/categories/categories"
        };

        // Required filters for each entity
        private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            ["clients"] = new[] { "accountId" },
            ["clients.query"] = new[] { "accountId" },
            ["invoices"] = new[] { "accountId" },
            ["invoices.query"] = new[] { "accountId" },
            ["estimates"] = new[] { "accountId" },
            ["estimates.query"] = new[] { "accountId" },
            ["expenses"] = new[] { "accountId" },
            ["expenses.query"] = new[] { "accountId" },
            ["items"] = new[] { "accountId" },
            ["items.query"] = new[] { "accountId" },
            ["payments"] = new[] { "accountId" },
            ["payments.query"] = new[] { "accountId" },
            ["projects"] = new[] { "businessId" },
            ["projects.query"] = new[] { "businessId" },
            ["timeentries"] = new[] { "businessId" },
            ["timeentries.query"] = new[] { "businessId" },
            ["tasks"] = new[] { "businessId" },
            ["tasks.query"] = new[] { "businessId" },
            ["staff"] = new[] { "businessId" },
            ["staff.query"] = new[] { "businessId" },
            ["services"] = new[] { "businessId" },
            ["services.query"] = new[] { "businessId" },
            ["taxes"] = new[] { "accountId" },
            ["taxes.query"] = new[] { "accountId" },
            ["categories"] = new[] { "accountId" },
            ["categories.query"] = new[] { "accountId" }
        };

        public FreshBooksDataSource(
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
                throw new InvalidOperationException($"Unknown FreshBooks entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            var resolvedEndpoint = ResolveEndpoint(endpoint, q);

            using var resp = await GetAsync(resolvedEndpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp);
        }

        // Paged (FreshBooks uses page and per_page parameters)
        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            if (!EntityEndpoints.TryGetValue(EntityName, out var endpoint))
                throw new InvalidOperationException($"Unknown FreshBooks entity '{EntityName}'.");

            var q = FiltersToQuery(filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            // FreshBooks pagination parameters
            q["page"] = pageNumber.ToString();
            q["per_page"] = Math.Min(pageSize, 100).ToString(); // FreshBooks max is 100

            string resolvedEndpoint = ResolveEndpoint(endpoint, q);

            var resp = CallFreshBooks(resolvedEndpoint, q).ConfigureAwait(false).GetAwaiter().GetResult();
            var items = ExtractArray(resp);

            // Estimate pagination info
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
                throw new ArgumentException($"FreshBooks entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ResolveEndpoint(string template, Dictionary<string, string> q)
        {
            // Substitute parameters in endpoint template
            if (template.Contains("{accountId}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("accountId", out var accountId) || string.IsNullOrWhiteSpace(accountId))
                    throw new ArgumentException("Missing required 'accountId' filter for this endpoint.");
                template = template.Replace("{accountId}", Uri.EscapeDataString(accountId));
            }

            if (template.Contains("{businessId}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("businessId", out var businessId) || string.IsNullOrWhiteSpace(businessId))
                    throw new ArgumentException("Missing required 'businessId' filter for this endpoint.");
                template = template.Replace("{businessId}", Uri.EscapeDataString(businessId));
            }

            return template;
        }

        private async Task<HttpResponseMessage> CallFreshBooks(string endpoint, Dictionary<string, string> query, CancellationToken ct = default)
            => await GetAsync(endpoint, query, cancellationToken: ct).ConfigureAwait(false);

        // Extracts data from FreshBooks API response
        private static List<object> ExtractArray(HttpResponseMessage resp)
        {
            var list = new List<object>();
            if (resp == null) return list;

            var json = resp.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(json);

            JsonElement node = doc.RootElement;

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // FreshBooks returns data in various structures
            // Try different common patterns
            if (node.TryGetProperty("response", out var response))
            {
                if (response.TryGetProperty("result", out var result))
                {
                    // Handle pagination structure
                    if (result.TryGetProperty("page", out var page) && result.TryGetProperty("pages", out var pages) && result.TryGetProperty("per_page", out var perPage) && result.TryGetProperty("total", out var total))
                    {
                        if (result.TryGetProperty("clients", out var clients) && clients.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var el in clients.EnumerateArray())
                            {
                                var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(el.GetRawText(), opts);
                                if (obj != null) list.Add(obj);
                            }
                        }
                        else if (result.TryGetProperty("invoices", out var invoices) && invoices.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var el in invoices.EnumerateArray())
                            {
                                var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(el.GetRawText(), opts);
                                if (obj != null) list.Add(obj);
                            }
                        }
                        else if (result.TryGetProperty("expenses", out var expenses) && expenses.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var el in expenses.EnumerateArray())
                            {
                                var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(el.GetRawText(), opts);
                                if (obj != null) list.Add(obj);
                            }
                        }
                        else if (result.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var el in items.EnumerateArray())
                            {
                                var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(el.GetRawText(), opts);
                                if (obj != null) list.Add(obj);
                            }
                        }
                        else if (result.TryGetProperty("payments", out var payments) && payments.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var el in payments.EnumerateArray())
                            {
                                var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(el.GetRawText(), opts);
                                if (obj != null) list.Add(obj);
                            }
                        }
                        else if (result.TryGetProperty("projects", out var projects) && projects.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var el in projects.EnumerateArray())
                            {
                                var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(el.GetRawText(), opts);
                                if (obj != null) list.Add(obj);
                            }
                        }
                        else if (result.TryGetProperty("time_entries", out var timeEntries) && timeEntries.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var el in timeEntries.EnumerateArray())
                            {
                                var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(el.GetRawText(), opts);
                                if (obj != null) list.Add(obj);
                            }
                        }
                        else if (result.TryGetProperty("tasks", out var tasks) && tasks.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var el in tasks.EnumerateArray())
                            {
                                var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(el.GetRawText(), opts);
                                if (obj != null) list.Add(obj);
                            }
                        }
                        else if (result.TryGetProperty("staff", out var staff) && staff.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var el in staff.EnumerateArray())
                            {
                                var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(el.GetRawText(), opts);
                                if (obj != null) list.Add(obj);
                            }
                        }
                        else if (result.TryGetProperty("services", out var services) && services.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var el in services.EnumerateArray())
                            {
                                var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(el.GetRawText(), opts);
                                if (obj != null) list.Add(obj);
                            }
                        }
                        else if (result.TryGetProperty("taxes", out var taxes) && taxes.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var el in taxes.EnumerateArray())
                            {
                                var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(el.GetRawText(), opts);
                                if (obj != null) list.Add(obj);
                            }
                        }
                        else if (result.TryGetProperty("categories", out var categories) && categories.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var el in categories.EnumerateArray())
                            {
                                var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(el.GetRawText(), opts);
                                if (obj != null) list.Add(obj);
                            }
                        }
                        else if (result.TryGetProperty("estimates", out var estimates) && estimates.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var el in estimates.EnumerateArray())
                            {
                                var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(el.GetRawText(), opts);
                                if (obj != null) list.Add(obj);
                            }
                        }
                    }
                }
            }

            // Fallback: try direct array or object
            if (list.Count == 0)
            {
                if (node.ValueKind == JsonValueKind.Array)
                {
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
            }

            return list;
        }
    }
}