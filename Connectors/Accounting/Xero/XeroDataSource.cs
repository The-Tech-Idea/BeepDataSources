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

namespace TheTechIdea.Beep.Connectors.Xero
{
    /// <summary>
    /// Xero data source implementation using WebAPIDataSource as base class
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Xero)]
    public class XeroDataSource : WebAPIDataSource
    {
        // Entity endpoints mapping for Xero API
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // Accounting
            ["accounts"] = "api.xro/2.0/Accounts",
            ["banktransactions"] = "api.xro/2.0/BankTransactions",
            ["banktransfers"] = "api.xro/2.0/BankTransfers",
            ["batchpayments"] = "api.xro/2.0/BatchPayments",
            ["brandingthemes"] = "api.xro/2.0/BrandingThemes",
            ["contacts"] = "api.xro/2.0/Contacts",
            ["contactgroups"] = "api.xro/2.0/ContactGroups",
            ["creditnotes"] = "api.xro/2.0/CreditNotes",
            ["currencies"] = "api.xro/2.0/Currencies",
            ["employees"] = "api.xro/2.0/Employees",
            ["expenseclaims"] = "api.xro/2.0/ExpenseClaims",
            ["invoices"] = "api.xro/2.0/Invoices",
            ["invoiceattachments"] = "api.xro/2.0/Invoices/{InvoiceID}/Attachments",
            ["items"] = "api.xro/2.0/Items",
            ["journals"] = "api.xro/2.0/Journals",
            ["linkedtransactions"] = "api.xro/2.0/LinkedTransactions",
            ["manualjournals"] = "api.xro/2.0/ManualJournals",
            ["organisations"] = "api.xro/2.0/Organisations",
            ["overpayments"] = "api.xro/2.0/Overpayments",
            ["payments"] = "api.xro/2.0/Payments",
            ["prepayments"] = "api.xro/2.0/Prepayments",
            ["purchaseorders"] = "api.xro/2.0/PurchaseOrders",
            ["quotes"] = "api.xro/2.0/Quotes",
            ["receipts"] = "api.xro/2.0/Receipts",
            ["repeatinginvoices"] = "api.xro/2.0/RepeatingInvoices",
            ["reports"] = "api.xro/2.0/Reports",
            ["taxrates"] = "api.xro/2.0/TaxRates",
            ["trackingcategories"] = "api.xro/2.0/TrackingCategories",
            ["users"] = "api.xro/2.0/Users"
        };

        // Required filters for each entity
        private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            ["accounts"] = Array.Empty<string>(),
            ["banktransactions"] = Array.Empty<string>(),
            ["banktransfers"] = Array.Empty<string>(),
            ["batchpayments"] = Array.Empty<string>(),
            ["brandingthemes"] = Array.Empty<string>(),
            ["contacts"] = Array.Empty<string>(),
            ["contactgroups"] = Array.Empty<string>(),
            ["creditnotes"] = Array.Empty<string>(),
            ["currencies"] = Array.Empty<string>(),
            ["employees"] = Array.Empty<string>(),
            ["expenseclaims"] = Array.Empty<string>(),
            ["invoices"] = Array.Empty<string>(),
            ["invoiceattachments"] = new[] { "InvoiceID" },
            ["items"] = Array.Empty<string>(),
            ["journals"] = Array.Empty<string>(),
            ["linkedtransactions"] = Array.Empty<string>(),
            ["manualjournals"] = Array.Empty<string>(),
            ["organisations"] = Array.Empty<string>(),
            ["overpayments"] = Array.Empty<string>(),
            ["payments"] = Array.Empty<string>(),
            ["prepayments"] = Array.Empty<string>(),
            ["purchaseorders"] = Array.Empty<string>(),
            ["quotes"] = Array.Empty<string>(),
            ["receipts"] = Array.Empty<string>(),
            ["repeatinginvoices"] = Array.Empty<string>(),
            ["reports"] = Array.Empty<string>(),
            ["taxrates"] = Array.Empty<string>(),
            ["trackingcategories"] = Array.Empty<string>(),
            ["users"] = Array.Empty<string>()
        };

        public XeroDataSource(
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
                throw new InvalidOperationException($"Unknown Xero entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            var resolvedEndpoint = ResolveEndpoint(endpoint, q);

            using var resp = await GetAsync(resolvedEndpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp);
        }

        // Paged (Xero uses page parameter for pagination)
        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            if (!EntityEndpoints.TryGetValue(EntityName, out var endpoint))
                throw new InvalidOperationException($"Unknown Xero entity '{EntityName}'.");

            var q = FiltersToQuery(filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            // Xero pagination parameters
            q["page"] = pageNumber.ToString();
            if (pageSize > 0 && pageSize != 100) // 100 is default
            {
                q["pageSize"] = Math.Min(pageSize, 100).ToString(); // Xero max is 100
            }

            string resolvedEndpoint = ResolveEndpoint(endpoint, q);

            var resp = CallXero(resolvedEndpoint, q).ConfigureAwait(false).GetAwaiter().GetResult();
            var items = ExtractArray(resp);

            // Xero doesn't provide total count in all responses, estimate based on page size
            int totalRecordsSoFar = (pageNumber - 1) * Math.Max(1, pageSize) + items.Count;

            return new PagedResult
            {
                Data = items,
                PageNumber = Math.Max(1, pageNumber),
                PageSize = pageSize > 0 ? pageSize : 100,
                TotalRecords = totalRecordsSoFar,
                TotalPages = items.Count < (pageSize > 0 ? pageSize : 100) ? pageNumber : pageNumber + 1, // estimate
                HasPreviousPage = pageNumber > 1,
                HasNextPage = items.Count >= (pageSize > 0 ? pageSize : 100)
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
                throw new ArgumentException($"Xero entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ResolveEndpoint(string template, Dictionary<string, string> q)
        {
            // Substitute parameters in endpoint template
            if (template.Contains("{InvoiceID}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("InvoiceID", out var invoiceId) || string.IsNullOrWhiteSpace(invoiceId))
                    throw new ArgumentException("Missing required 'InvoiceID' filter for this endpoint.");
                template = template.Replace("{InvoiceID}", Uri.EscapeDataString(invoiceId));
            }
            return template;
        }

        private async Task<HttpResponseMessage> CallXero(string endpoint, Dictionary<string, string> query, CancellationToken ct = default)
            => await GetAsync(endpoint, query, cancellationToken: ct).ConfigureAwait(false);

        // Extracts data from Xero API response
        private static List<object> ExtractArray(HttpResponseMessage resp)
        {
            var list = new List<object>();
            if (resp == null) return list;

            var json = resp.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(json);

            JsonElement node = doc.RootElement;

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Xero returns data as arrays directly
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
                // Handle single object responses
                var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(node.GetRawText(), opts);
                if (obj != null) list.Add(obj);
            }

            return list;
        }

        // CommandAttribute methods for framework integration
        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Xero, PointType = EnumPointType.Function, ObjectType = "Contacts", ClassName = "XeroDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<Contact>")]
        public IEnumerable<Contact> GetContacts(List<AppFilter> filter) => GetEntity("contacts", filter).Cast<Contact>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Xero, PointType = EnumPointType.Function, ObjectType = "Invoices", ClassName = "XeroDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<Invoice>")]
        public IEnumerable<Invoice> GetInvoices(List<AppFilter> filter) => GetEntity("invoices", filter).Cast<Invoice>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Xero, PointType = EnumPointType.Function, ObjectType = "Accounts", ClassName = "XeroDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<Account>")]
        public IEnumerable<Account> GetAccounts(List<AppFilter> filter) => GetEntity("accounts", filter).Cast<Account>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Xero, PointType = EnumPointType.Function, ObjectType = "Items", ClassName = "XeroDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<Item>")]
        public IEnumerable<Item> GetItems(List<AppFilter> filter) => GetEntity("items", filter).Cast<Item>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Xero, PointType = EnumPointType.Function, ObjectType = "Employees", ClassName = "XeroDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<Employee>")]
        public IEnumerable<Employee> GetEmployees(List<AppFilter> filter) => GetEntity("employees", filter).Cast<Employee>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Xero, PointType = EnumPointType.Function, ObjectType = "Payments", ClassName = "XeroDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<Payment>")]
        public IEnumerable<Payment> GetPayments(List<AppFilter> filter) => GetEntity("payments", filter).Cast<Payment>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Xero, PointType = EnumPointType.Function, ObjectType = "BankTransactions", ClassName = "XeroDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<BankTransaction>")]
        public IEnumerable<BankTransaction> GetBankTransactions(List<AppFilter> filter) => GetEntity("banktransactions", filter).Cast<BankTransaction>();
    }
}