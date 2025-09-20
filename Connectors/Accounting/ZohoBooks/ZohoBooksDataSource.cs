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

namespace TheTechIdea.Beep.Connectors.ZohoBooks
{
    /// <summary>
    /// ZohoBooks data source implementation using WebAPIDataSource as base class
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.ZohoBooks)]
    public class ZohoBooksDataSource : WebAPIDataSource
    {
        // Entity endpoints mapping for ZohoBooks API
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // Organization
            ["organizations"] = "organizations",

            // Contacts (Customers & Vendors)
            ["contacts"] = "contacts",
            ["customers"] = "customers",
            ["vendors"] = "vendors",

            // Items
            ["items"] = "items",

            // Invoices
            ["invoices"] = "invoices",
            ["invoice_items"] = "invoices/{invoice_id}/items",

            // Bills
            ["bills"] = "bills",
            ["bill_items"] = "bills/{bill_id}/items",

            // Payments
            ["payments"] = "customerpayments",
            ["vendorpayments"] = "vendorpayments",

            // Credit Notes
            ["creditnotes"] = "creditnotes",

            // Estimates/Quotes
            ["estimates"] = "estimates",

            // Purchase Orders
            ["purchaseorders"] = "purchaseorders",

            // Journals
            ["journals"] = "journals",

            // Chart of Accounts
            ["chartofaccounts"] = "chartofaccounts",
            ["accounts"] = "chartofaccounts",

            // Bank Accounts & Transactions
            ["bankaccounts"] = "bankaccounts",
            ["banktransactions"] = "banktransactions",

            // Expenses
            ["expenses"] = "expenses",

            // Projects
            ["projects"] = "projects",

            // Time Entries
            ["timesheets"] = "projects/timesheets",

            // Taxes
            ["taxes"] = "settings/taxes",

            // Users
            ["users"] = "users",

            // Currencies
            ["currencies"] = "settings/currencies",

            // Custom Fields
            ["customfields"] = "settings/customfields"
        };

        // Required filters for each entity
        private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            ["organizations"] = Array.Empty<string>(),
            ["contacts"] = new[] { "organization_id" },
            ["customers"] = new[] { "organization_id" },
            ["vendors"] = new[] { "organization_id" },
            ["items"] = new[] { "organization_id" },
            ["invoices"] = new[] { "organization_id" },
            ["invoice_items"] = new[] { "organization_id", "invoice_id" },
            ["bills"] = new[] { "organization_id" },
            ["bill_items"] = new[] { "organization_id", "bill_id" },
            ["payments"] = new[] { "organization_id" },
            ["vendorpayments"] = new[] { "organization_id" },
            ["creditnotes"] = new[] { "organization_id" },
            ["estimates"] = new[] { "organization_id" },
            ["purchaseorders"] = new[] { "organization_id" },
            ["journals"] = new[] { "organization_id" },
            ["chartofaccounts"] = new[] { "organization_id" },
            ["accounts"] = new[] { "organization_id" },
            ["bankaccounts"] = new[] { "organization_id" },
            ["banktransactions"] = new[] { "organization_id" },
            ["expenses"] = new[] { "organization_id" },
            ["projects"] = new[] { "organization_id" },
            ["timesheets"] = new[] { "organization_id" },
            ["taxes"] = new[] { "organization_id" },
            ["users"] = new[] { "organization_id" },
            ["currencies"] = new[] { "organization_id" },
            ["customfields"] = new[] { "organization_id" }
        };

        public ZohoBooksDataSource(
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
                throw new InvalidOperationException($"Unknown ZohoBooks entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            var resolvedEndpoint = ResolveEndpoint(endpoint, q);

            using var resp = await GetAsync(resolvedEndpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, EntityName);
        }

        // Paged
        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            if (!EntityEndpoints.TryGetValue(EntityName, out var endpoint))
                throw new InvalidOperationException($"Unknown ZohoBooks entity '{EntityName}'.");

            var q = FiltersToQuery(filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));
            q["per_page"] = Math.Max(10, Math.Min(pageSize, 200)).ToString();
            q["page"] = pageNumber.ToString();

            string resolvedEndpoint = ResolveEndpoint(endpoint, q);

            var finalResp = CallZohoBooks(resolvedEndpoint, q).ConfigureAwait(false).GetAwaiter().GetResult();
            var items = ExtractArray(finalResp, EntityName);

            return new PagedResult
            {
                Data = items,
                PageNumber = Math.Max(1, pageNumber),
                PageSize = pageSize,
                TotalRecords = items.Count, // ZohoBooks provides pagination info in response
                TotalPages = 1, // Will be updated when we parse pagination metadata
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
                throw new ArgumentException($"ZohoBooks entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
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

            if (template.Contains("{bill_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("bill_id", out var billId) || string.IsNullOrWhiteSpace(billId))
                    throw new ArgumentException("Missing required 'bill_id' filter for this endpoint.");
                result = result.Replace("{bill_id}", Uri.EscapeDataString(billId));
            }

            return result;
        }

        private async Task<HttpResponseMessage> CallZohoBooks(string endpoint, Dictionary<string, string> query, CancellationToken ct = default)
            => await GetAsync(endpoint, query, cancellationToken: ct).ConfigureAwait(false);

        // Extracts entity data into a List<object> (Dictionary<string,object> per item).
        private static List<object> ExtractArray(HttpResponseMessage resp, string entityName)
        {
            var list = new List<object>();
            if (resp == null) return list;

            var json = resp.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(json);

            JsonElement root = doc.RootElement;

            // ZohoBooks API responses vary by entity
            string dataProperty = GetDataPropertyName(entityName);

            if (root.TryGetProperty(dataProperty, out JsonElement dataNode))
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                if (dataNode.ValueKind == JsonValueKind.Array)
                {
                    list.Capacity = dataNode.GetArrayLength();
                    foreach (var el in dataNode.EnumerateArray())
                    {
                        var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(el.GetRawText(), opts);
                        if (obj != null) list.Add(obj);
                    }
                }
                else if (dataNode.ValueKind == JsonValueKind.Object)
                {
                    var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(dataNode.GetRawText(), opts);
                    if (obj != null) list.Add(obj);
                }
            }

            return list;
        }

        private static string GetDataPropertyName(string entityName)
        {
            // ZohoBooks uses different property names for different entities
            return entityName.ToLower() switch
            {
                "organizations" => "organizations",
                "contacts" => "contacts",
                "customers" => "customers",
                "vendors" => "vendors",
                "items" => "items",
                "invoices" => "invoices",
                "invoice_items" => "invoice_items",
                "bills" => "bills",
                "bill_items" => "bill_items",
                "payments" => "customerpayments",
                "vendorpayments" => "vendorpayments",
                "creditnotes" => "creditnotes",
                "estimates" => "estimates",
                "purchaseorders" => "purchaseorders",
                "journals" => "journals",
                "chartofaccounts" => "chartofaccounts",
                "accounts" => "chartofaccounts",
                "bankaccounts" => "bankaccounts",
                "banktransactions" => "transactions",
                "expenses" => "expenses",
                "projects" => "projects",
                "timesheets" => "timesheets",
                "taxes" => "taxes",
                "users" => "users",
                "currencies" => "currencies",
                "customfields" => "customfields",
                _ => entityName
            };
        }
    }
}