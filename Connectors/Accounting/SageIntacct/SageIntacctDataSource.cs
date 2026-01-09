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

namespace TheTechIdea.Beep.Connectors.SageIntacct
{
    /// <summary>
    /// Sage Intacct data source implementation using WebAPIDataSource as base class
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.SageIntacct)]
    public class SageIntacctDataSource : WebAPIDataSource
    {
        // Entity endpoints mapping for Sage Intacct API
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // Customers
            ["customers"] = "objects/accounts-receivable/customer",
            ["customers.query"] = "objects/accounts-receivable/customer",
            // Invoices
            ["invoices"] = "objects/accounts-receivable/invoice",
            ["invoices.query"] = "objects/accounts-receivable/invoice",
            // Bills
            ["bills"] = "objects/accounts-payable/bill",
            ["bills.query"] = "objects/accounts-payable/bill",
            // Vendors
            ["vendors"] = "objects/accounts-payable/vendor",
            ["vendors.query"] = "objects/accounts-payable/vendor",
            // Items
            ["items"] = "objects/inventory-control/item",
            ["items.query"] = "objects/inventory-control/item",
            // Accounts
            ["accounts"] = "objects/general-ledger/account",
            ["accounts.query"] = "objects/general-ledger/account",
            // Employees
            ["employees"] = "objects/company-config/employee",
            ["employees.query"] = "objects/company-config/employee",
            // Departments
            ["departments"] = "objects/company-config/department",
            ["departments.query"] = "objects/company-config/department",
            // Locations
            ["locations"] = "objects/company-config/location",
            ["locations.query"] = "objects/company-config/location",
            // Classes
            ["classes"] = "objects/company-config/class",
            ["classes.query"] = "objects/company-config/class",
            // Projects
            ["projects"] = "objects/project/project",
            ["projects.query"] = "objects/project/project",
            // Journal Entries
            ["journalentries"] = "objects/general-ledger/journal-entry",
            ["journalentries.query"] = "objects/general-ledger/journal-entry",
            // Purchase Orders
            ["purchaseorders"] = "objects/procurement/purchase-order",
            ["purchaseorders.query"] = "objects/procurement/purchase-order",
            // Sales Orders
            ["salesorders"] = "objects/order-entry/sales-order",
            ["salesorders.query"] = "objects/order-entry/sales-order",
            // Inventory Transactions
            ["inventorytransactions"] = "objects/inventory-control/transaction",
            ["inventorytransactions.query"] = "objects/inventory-control/transaction",
            // Tax Codes
            ["taxcodes"] = "objects/company-config/tax-code",
            ["taxcodes.query"] = "objects/company-config/tax-code",
            // Currencies
            ["currencies"] = "objects/company-config/currency",
            ["currencies.query"] = "objects/company-config/currency"
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
            ["vendors"] = Array.Empty<string>(),
            ["vendors.query"] = Array.Empty<string>(),
            ["items"] = Array.Empty<string>(),
            ["items.query"] = Array.Empty<string>(),
            ["accounts"] = Array.Empty<string>(),
            ["accounts.query"] = Array.Empty<string>(),
            ["employees"] = Array.Empty<string>(),
            ["employees.query"] = Array.Empty<string>(),
            ["departments"] = Array.Empty<string>(),
            ["departments.query"] = Array.Empty<string>(),
            ["locations"] = Array.Empty<string>(),
            ["locations.query"] = Array.Empty<string>(),
            ["classes"] = Array.Empty<string>(),
            ["classes.query"] = Array.Empty<string>(),
            ["projects"] = Array.Empty<string>(),
            ["projects.query"] = Array.Empty<string>(),
            ["journalentries"] = Array.Empty<string>(),
            ["journalentries.query"] = Array.Empty<string>(),
            ["purchaseorders"] = Array.Empty<string>(),
            ["purchaseorders.query"] = Array.Empty<string>(),
            ["salesorders"] = Array.Empty<string>(),
            ["salesorders.query"] = Array.Empty<string>(),
            ["inventorytransactions"] = Array.Empty<string>(),
            ["inventorytransactions.query"] = Array.Empty<string>(),
            ["taxcodes"] = Array.Empty<string>(),
            ["taxcodes.query"] = Array.Empty<string>(),
            ["currencies"] = Array.Empty<string>(),
            ["currencies.query"] = Array.Empty<string>()
        };

        public SageIntacctDataSource(
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
                throw new InvalidOperationException($"Unknown Sage Intacct entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            var resolvedEndpoint = ResolveEndpoint(endpoint, q);

            using var resp = await GetAsync(resolvedEndpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp);
        }

        // Paged (Sage Intacct uses page and pagesize parameters)
        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            if (!EntityEndpoints.TryGetValue(EntityName, out var endpoint))
                throw new InvalidOperationException($"Unknown Sage Intacct entity '{EntityName}'.");

            var q = FiltersToQuery(filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            // Sage Intacct pagination parameters
            q["page"] = pageNumber.ToString();
            q["pagesize"] = Math.Min(pageSize, 2000).ToString(); // Sage Intacct max is 2000

            string resolvedEndpoint = ResolveEndpoint(endpoint, q);

            var resp = CallSageIntacct(resolvedEndpoint, q).ConfigureAwait(false).GetAwaiter().GetResult();
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
                throw new ArgumentException($"Sage Intacct entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ResolveEndpoint(string template, Dictionary<string, string> q)
        {
            // Sage Intacct endpoints are generally static
            return template;
        }

        private async Task<HttpResponseMessage> CallSageIntacct(string endpoint, Dictionary<string, string> query, CancellationToken ct = default)
            => await GetAsync(endpoint, query, cancellationToken: ct).ConfigureAwait(false);

        // Extracts data from Sage Intacct API response
        private static List<object> ExtractArray(HttpResponseMessage resp)
        {
            var list = new List<object>();
            if (resp == null) return list;

            var json = resp.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(json);

            JsonElement node = doc.RootElement;

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Sage Intacct returns data in "data" array
            if (node.TryGetProperty("data", out var dataNode) && dataNode.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in dataNode.EnumerateArray())
                {
                    var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(el.GetRawText(), opts);
                    if (obj != null) list.Add(obj);
                }
            }
            else if (node.ValueKind == JsonValueKind.Array)
            {
                // Handle direct array responses
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
        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.SageIntacct, PointType = EnumPointType.Function, ObjectType = "Customers", ClassName = "SageIntacctDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<Customer>")]
        public IEnumerable<Customer> GetCustomers(List<AppFilter> filter) => GetEntity("customers", filter).Cast<Customer>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.SageIntacct, PointType = EnumPointType.Function, ObjectType = "Invoices", ClassName = "SageIntacctDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<Invoice>")]
        public IEnumerable<Invoice> GetInvoices(List<AppFilter> filter) => GetEntity("invoices", filter).Cast<Invoice>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.SageIntacct, PointType = EnumPointType.Function, ObjectType = "Bills", ClassName = "SageIntacctDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<Bill>")]
        public IEnumerable<Bill> GetBills(List<AppFilter> filter) => GetEntity("bills", filter).Cast<Bill>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.SageIntacct, PointType = EnumPointType.Function, ObjectType = "Vendors", ClassName = "SageIntacctDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<Vendor>")]
        public IEnumerable<Vendor> GetVendors(List<AppFilter> filter) => GetEntity("vendors", filter).Cast<Vendor>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.SageIntacct, PointType = EnumPointType.Function, ObjectType = "Items", ClassName = "SageIntacctDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<Item>")]
        public IEnumerable<Item> GetItems(List<AppFilter> filter) => GetEntity("items", filter).Cast<Item>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.SageIntacct, PointType = EnumPointType.Function, ObjectType = "Accounts", ClassName = "SageIntacctDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<Account>")]
        public IEnumerable<Account> GetAccounts(List<AppFilter> filter) => GetEntity("accounts", filter).Cast<Account>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.SageIntacct, PointType = EnumPointType.Function, ObjectType = "Employees", ClassName = "SageIntacctDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<Employee>")]
        public IEnumerable<Employee> GetEmployees(List<AppFilter> filter) => GetEntity("employees", filter).Cast<Employee>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.SageIntacct, PointType = EnumPointType.Function, ObjectType = "Departments", ClassName = "SageIntacctDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<Department>")]
        public IEnumerable<Department> GetDepartments(List<AppFilter> filter) => GetEntity("departments", filter).Cast<Department>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.SageIntacct, PointType = EnumPointType.Function, ObjectType = "Locations", ClassName = "SageIntacctDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<Location>")]
        public IEnumerable<Location> GetLocations(List<AppFilter> filter) => GetEntity("locations", filter).Cast<Location>();
    }
}