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

namespace TheTechIdea.Beep.Connectors.MYOB
{
    /// <summary>
    /// MYOB data source implementation using WebAPIDataSource as base class
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.MYOB)]
    public class MYOBDataSource : WebAPIDataSource
    {
        // Entity endpoints mapping for MYOB API
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // Company File
            ["companyfile"] = "Company",
            // Customers
            ["customers"] = "Customer",
            // Suppliers
            ["suppliers"] = "Supplier",
            // Items
            ["items"] = "Item",
            // Invoices
            ["invoices"] = "Sale/Invoice",
            ["invoiceitems"] = "Sale/Invoice/Item",
            // Bills
            ["bills"] = "Purchase/Bill",
            ["billitems"] = "Purchase/Bill/Item",
            // Payments
            ["payments"] = "Sale/CustomerPayment",
            ["supplierpayments"] = "Purchase/SupplierPayment",
            // Journals
            ["journals"] = "GeneralLedger/JournalTransaction",
            // Accounts
            ["accounts"] = "GeneralLedger/Account",
            // Tax Codes
            ["taxcodes"] = "GeneralLedger/TaxCode",
            // Employees
            ["employees"] = "Contact/Employee",
            // Payroll
            ["payrollcategories"] = "Payroll/PayrollCategory",
            ["pays"] = "Payroll/Pay"
        };

        // Required filters for each entity
        private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            ["companyfile"] = Array.Empty<string>(),
            ["customers"] = Array.Empty<string>(),
            ["suppliers"] = Array.Empty<string>(),
            ["items"] = Array.Empty<string>(),
            ["invoices"] = Array.Empty<string>(),
            ["invoiceitems"] = new[] { "InvoiceID" },
            ["bills"] = Array.Empty<string>(),
            ["billitems"] = new[] { "BillID" },
            ["payments"] = Array.Empty<string>(),
            ["supplierpayments"] = Array.Empty<string>(),
            ["journals"] = Array.Empty<string>(),
            ["accounts"] = Array.Empty<string>(),
            ["taxcodes"] = Array.Empty<string>(),
            ["employees"] = Array.Empty<string>(),
            ["payrollcategories"] = Array.Empty<string>(),
            ["pays"] = Array.Empty<string>()
        };

        public MYOBDataSource(
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
                throw new InvalidOperationException($"Unknown MYOB entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            var resolvedEndpoint = ResolveEndpoint(endpoint, q);

            using var resp = await GetAsync(resolvedEndpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, "Items");
        }

        // Paged
        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            if (!EntityEndpoints.TryGetValue(EntityName, out var endpoint))
                throw new InvalidOperationException($"Unknown MYOB entity '{EntityName}'.");

            var q = FiltersToQuery(filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));
            q["$top"] = Math.Max(1, Math.Min(pageSize, 100)).ToString();
            q["$skip"] = ((pageNumber - 1) * pageSize).ToString();

            string resolvedEndpoint = ResolveEndpoint(endpoint, q);

            var resp = CallMYOB(resolvedEndpoint, q).ConfigureAwait(false).GetAwaiter().GetResult();
            var items = ExtractArray(resp, "Items");

            // MYOB doesn't provide total count in response, so we estimate
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
                throw new ArgumentException($"MYOB entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ResolveEndpoint(string template, Dictionary<string, string> q)
        {
            // Substitute {id} from filters if present
            if (template.Contains("{InvoiceID}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("InvoiceID", out var id) || string.IsNullOrWhiteSpace(id))
                    throw new ArgumentException("Missing required 'InvoiceID' filter for this endpoint.");
                template = template.Replace("{InvoiceID}", Uri.EscapeDataString(id));
            }
            if (template.Contains("{BillID}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("BillID", out var id) || string.IsNullOrWhiteSpace(id))
                    throw new ArgumentException("Missing required 'BillID' filter for this endpoint.");
                template = template.Replace("{BillID}", Uri.EscapeDataString(id));
            }
            return template;
        }

        private async Task<HttpResponseMessage> CallMYOB(string endpoint, Dictionary<string, string> query, CancellationToken ct = default)
            => await GetAsync(endpoint, query, cancellationToken: ct).ConfigureAwait(false);

        // Extracts "Items" (array) into a List<object> (Dictionary<string,object> per item).
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
                    return list; // no "Items" -> empty
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
        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.MYOB, PointType = EnumPointType.Function, ObjectType ="Customers", ClassName = "MYOBDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<Customer>")]
        public IEnumerable<Customer> GetCustomers(List<AppFilter> filter) => GetEntity("customers", filter).Cast<Customer>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.MYOB, PointType = EnumPointType.Function, ObjectType ="Suppliers", ClassName = "MYOBDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<Supplier>")]
        public IEnumerable<Supplier> GetSuppliers(List<AppFilter> filter) => GetEntity("suppliers", filter).Cast<Supplier>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.MYOB, PointType = EnumPointType.Function, ObjectType ="Items", ClassName = "MYOBDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<Item>")]
        public IEnumerable<Item> GetItems(List<AppFilter> filter) => GetEntity("items", filter).Cast<Item>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.MYOB, PointType = EnumPointType.Function, ObjectType ="Invoices", ClassName = "MYOBDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<Invoice>")]
        public IEnumerable<Invoice> GetInvoices(List<AppFilter> filter) => GetEntity("invoices", filter).Cast<Invoice>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.MYOB, PointType = EnumPointType.Function, ObjectType ="Bills", ClassName = "MYOBDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<Bill>")]
        public IEnumerable<Bill> GetBills(List<AppFilter> filter) => GetEntity("bills", filter).Cast<Bill>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.MYOB, PointType = EnumPointType.Function, ObjectType ="Payments", ClassName = "MYOBDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<Payment>")]
        public IEnumerable<Payment> GetPayments(List<AppFilter> filter) => GetEntity("payments", filter).Cast<Payment>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.MYOB, PointType = EnumPointType.Function, ObjectType ="SupplierPayments", ClassName = "MYOBDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<SupplierPayment>")]
        public IEnumerable<SupplierPayment> GetSupplierPayments(List<AppFilter> filter) => GetEntity("supplierpayments", filter).Cast<SupplierPayment>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.MYOB, PointType = EnumPointType.Function, ObjectType ="Journals", ClassName = "MYOBDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<JournalTransaction>")]
        public IEnumerable<JournalTransaction> GetJournals(List<AppFilter> filter) => GetEntity("journals", filter).Cast<JournalTransaction>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.MYOB, PointType = EnumPointType.Function, ObjectType ="Accounts", ClassName = "MYOBDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<Account>")]
        public IEnumerable<Account> GetAccounts(List<AppFilter> filter) => GetEntity("accounts", filter).Cast<Account>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.MYOB, PointType = EnumPointType.Function, ObjectType ="TaxCodes", ClassName = "MYOBDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<TaxCode>")]
        public IEnumerable<TaxCode> GetTaxCodes(List<AppFilter> filter) => GetEntity("taxcodes", filter).Cast<TaxCode>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.MYOB, PointType = EnumPointType.Function, ObjectType ="Employees", ClassName = "MYOBDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<Employee>")]
        public IEnumerable<Employee> GetEmployees(List<AppFilter> filter) => GetEntity("employees", filter).Cast<Employee>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.MYOB, PointType = EnumPointType.Function, ObjectType ="PayrollCategories", ClassName = "MYOBDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<PayrollCategory>")]
        public IEnumerable<PayrollCategory> GetPayrollCategories(List<AppFilter> filter) => GetEntity("payrollcategories", filter).Cast<PayrollCategory>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.MYOB, PointType = EnumPointType.Function, ObjectType ="Pays", ClassName = "MYOBDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<Pay>")]
        public IEnumerable<Pay> GetPays(List<AppFilter> filter) => GetEntity("pays", filter).Cast<Pay>();
    }
}
