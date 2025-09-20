using System;using System;

using System.Collections.Generic;using System.Collections.Generic;

using System.Linq;using System.Linq;

using System.Net.Http;using System.Net.Http;

using System.Text.Json;using System.Text.Json;

using System.Threading;using System.Threading;

using System.Threading.Tasks;using System.Threading.Tasks;

using TheTechIdea.Beep.ConfigUtil;using TheTechIdea.Beep.ConfigUtil;

using TheTechIdea.Beep.DataBase;using TheTechIdea.Beep.DataBase;

using TheTechIdea.Beep.Editor;using TheTechIdea.Beep.Editor;

using TheTechIdea.Beep.Logger;using TheTechIdea.Beep.Logger;

using TheTechIdea.Beep.Report;using TheTechIdea.Beep.Report;

using TheTechIdea.Beep.Utilities;using TheTechIdea.Beep.Utilities;

using TheTechIdea.Beep.Vis;using TheTechIdea.Beep.Vis;

using TheTechIdea.Beep.WebAPI;using TheTechIdea.Beep.WebAPI;



namespace TheTechIdea.Beep.Connectors.Zohonamespace TheTechIdea.Beep.Connectors.Zoho

{{

    /// <summary>    /// <summary>

    /// Zoho CRM data source implementation using WebAPIDataSource as base class    /// Zoho CRM data source implementation using WebAPIDataSource as base class

    /// </summary>    /// </summary>

    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zoho)]    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zoho)]

    public class ZohoDataSource : WebAPIDataSource    public class ZohoDataSource : WebAPIDataSource

    {    {

        // Known Zoho CRM entities with their API endpoints        // Known Zoho CRM entities with their API endpoints

        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)

        {        {

            ["leads"] = "Leads",            ["leads"] = "Leads",

            ["contacts"] = "Contacts",            ["contacts"] = "Contacts",

            ["accounts"] = "Accounts",            ["accounts"] = "Accounts",

            ["deals"] = "Deals",            ["deals"] = "Deals",

            ["campaigns"] = "Campaigns",            ["campaigns"] = "Campaigns",

            ["tasks"] = "Tasks",            ["tasks"] = "Tasks",

            ["events"] = "Events",            ["events"] = "Events",

            ["calls"] = "Calls",            ["calls"] = "Calls",

            ["notes"] = "Notes",            ["notes"] = "Notes",

            ["products"] = "Products",            ["products"] = "Products",

            ["price_books"] = "Price_Books",            ["price_books"] = "Price_Books",

            ["quotes"] = "Quotes",            ["quotes"] = "Quotes",

            ["sales_orders"] = "Sales_Orders",            ["sales_orders"] = "Sales_Orders",

            ["invoices"] = "Invoices",            ["invoices"] = "Invoices",

            ["vendors"] = "Vendors",            ["vendors"] = "Vendors",

            ["users"] = "users"            ["users"] = "users"

        };        };



        public ZohoDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per) : base(datasourcename, logger, DMEEditor, databasetype, per)        public ZohoDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per) : base(datasourcename, logger, DMEEditor, databasetype, per)

        {        {

            // Initialize WebAPI connection properties if needed            // Initialize WebAPI connection properties if needed

            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)

            {            {

                if (Dataconnection != null)                if (Dataconnection != null)

                    Dataconnection.ConnectionProp = new WebAPIConnectionProperties();                    Dataconnection.ConnectionProp = new WebAPIConnectionProperties();

            }            }



            // Set up entity list            // Set up entity list

            EntitiesNames = EntityEndpoints.Keys.ToList();            EntitiesNames = EntityEndpoints.Keys.ToList();

            Entities = EntitiesNames.Select(name => new EntityStructure             Entities = EntitiesNames.Select(name => new EntityStructure 

            {             { 

                EntityName = name,                 EntityName = name, 

                DatasourceEntityName = name                 DatasourceEntityName = name 

            }).ToList();            }).ToList();

        }        }



        // Entity list method following Twitter pattern        // Entity list method following Twitter pattern

        public override List<string> GetEntitesList()        public override List<string> GetEntitesList()

        {        {

            return EntityEndpoints.Keys.ToList();            return EntityEndpoints.Keys.ToList();

        }        }



        // Sync method following Twitter pattern        // Sync method following Twitter pattern

        public override IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)        public override IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)

        {        {

            var result = GetEntityAsync(EntityName, filter).ConfigureAwait(false).GetAwaiter().GetResult();            var result = GetEntityAsync(EntityName, filter).ConfigureAwait(false).GetAwaiter().GetResult();

            return result ?? new List<object>();            return result ?? new List<object>();

        }        }



        // Async method following Twitter pattern        // Async method following Twitter pattern

        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> filter)        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> filter)

        {        {

            try            try

            {            {

                if (!EntityEndpoints.TryGetValue(EntityName, out string? endpoint) || endpoint == null)                if (!EntityEndpoints.TryGetValue(EntityName, out string? endpoint) || endpoint == null)

                {                {

                    Logger?.WriteLog($"Unknown entity: {EntityName}");                    Logger?.WriteLog($"Unknown entity: {EntityName}");

                    return new List<object>();                    return new List<object>();

                }                }



                var queryParams = BuildZohoQuery(filter);                var queryParams = BuildZohoQuery(filter);

                                

                // Zoho uses different endpoints for search vs. regular queries                // Zoho uses different endpoints for search vs. regular queries

                var useSearch = filter?.Any(f => !string.IsNullOrWhiteSpace(f.FieldName)) == true;                var useSearch = filter?.Any(f => !string.IsNullOrWhiteSpace(f.FieldName)) == true;

                var finalEndpoint = useSearch && !endpoint.Equals("users", StringComparison.OrdinalIgnoreCase)                 var finalEndpoint = useSearch && !endpoint.Equals("users", StringComparison.OrdinalIgnoreCase) 

                    ? $"{endpoint}/search"                     ? $"{endpoint}/search" 

                    : endpoint;                    : endpoint;



                // Handle pagination similar to Twitter pattern                // Handle pagination similar to Twitter pattern

                var allResults = new List<object>();                var allResults = new List<object>();

                int page = 1;                int page = 1;

                int perPage = 200; // Zoho default page size                int perPage = 200; // Zoho default page size

                bool hasMore = true;                bool hasMore = true;



                while (hasMore)                while (hasMore)

                {                {

                    var paginatedQuery = new Dictionary<string, string>(queryParams)                    var paginatedQuery = new Dictionary<string, string>(queryParams)

                    {                    {

                        ["page"] = page.ToString(),                        ["page"] = page.ToString(),

                        ["per_page"] = perPage.ToString()                        ["per_page"] = perPage.ToString()

                    };                    };



                    using var response = await GetAsync(finalEndpoint, paginatedQuery).ConfigureAwait(false);                    using var response = await GetAsync(finalEndpoint, paginatedQuery).ConfigureAwait(false);

                    if (response?.IsSuccessStatusCode != true)                    if (response?.IsSuccessStatusCode != true)

                    {                    {

                        Logger?.WriteLog($"Failed to fetch {EntityName} from Zoho API");                        Logger?.WriteLog($"Failed to fetch {EntityName} from Zoho API");

                        break;                        break;

                    }                    }



                    var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);                    var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    var results = ExtractZohoData(content, endpoint);                    var results = ExtractZohoData(content, endpoint);

                                        

                    if (results?.Any() == true)                    if (results?.Any() == true)

                    {                    {

                        allResults.AddRange(results);                        allResults.AddRange(results);

                        page++;                        page++;

                                                

                        // Check if we got fewer results than requested (indicates last page)                        // Check if we got fewer results than requested (indicates last page)

                        hasMore = results.Count() >= perPage;                        hasMore = results.Count() >= perPage;

                    }                    }

                    else                    else

                    {                    {

                        hasMore = false;                        hasMore = false;

                    }                    }

                }                }



                return allResults;                return allResults;

            }            }

            catch (Exception ex)            catch (Exception ex)

            {            {

                Logger?.WriteLog($"Error fetching {EntityName}: {ex.Message}");                Logger?.WriteLog($"Error fetching {EntityName}: {ex.Message}");

                if (ErrorObject != null)                if (ErrorObject != null)

                    ErrorObject.Flag = Errors.Failed;                    ErrorObject.Flag = Errors.Failed;

                return new List<object>();                return new List<object>();

            }            }

        }        }



        // Build query parameters for Zoho CRM API        // Build query parameters for Zoho CRM API

        private Dictionary<string, string> BuildZohoQuery(List<AppFilter> filters)        private Dictionary<string, string> BuildZohoQuery(List<AppFilter> filters)

        {        {

            var queryParams = new Dictionary<string, string>();            var queryParams = new Dictionary<string, string>();



            if (filters?.Any() != true) return queryParams;            if (filters?.Any() != true) return queryParams;



            // Build Zoho CRM search criteria            // Build Zoho CRM search criteria

            var criteria = new List<string>();            var criteria = new List<string>();

                        

            foreach (var filter in filters)            foreach (var filter in filters)

            {            {

                if (string.IsNullOrWhiteSpace(filter.FieldName) || filter.FilterValue == null)                if (string.IsNullOrWhiteSpace(filter.FieldName) || filter.FilterValue == null)

                    continue;                    continue;



                var fieldName = filter.FieldName;                var fieldName = filter.FieldName;

                var value = filter.FilterValue.ToString();                var value = filter.FilterValue.ToString();



                // Map common filter operations to Zoho format                // Map common filter operations to Zoho format

                switch (filter.Operator?.ToLowerInvariant())                switch (filter.Operator?.ToLowerInvariant())

                {                {

                    case "=":                    case "=":

                    case "eq":                    case "eq":

                        criteria.Add($"({fieldName}:equals:{EscapeZohoValue(value)})");                        criteria.Add($"({fieldName}:equals:{EscapeZohoValue(value)})");

                        break;                        break;

                    case "like":                    case "like":

                    case "contains":                    case "contains":

                        criteria.Add($"({fieldName}:contains:{EscapeZohoValue(value)})");                        criteria.Add($"({fieldName}:contains:{EscapeZohoValue(value)})");

                        break;                        break;

                    case ">":                    case ">":

                    case "gt":                    case "gt":

                        criteria.Add($"({fieldName}:greater_than:{EscapeZohoValue(value)})");                        criteria.Add($"({fieldName}:greater_than:{EscapeZohoValue(value)})");

                        break;                        break;

                    case "<":                    case "<":

                    case "lt":                    case "lt":

                        criteria.Add($"({fieldName}:less_than:{EscapeZohoValue(value)})");                        criteria.Add($"({fieldName}:less_than:{EscapeZohoValue(value)})");

                        break;                        break;

                    case ">=":                    case ">=":

                    case "gte":                    case "gte":

                        criteria.Add($"({fieldName}:greater_equal:{EscapeZohoValue(value)})");                        criteria.Add($"({fieldName}:greater_equal:{EscapeZohoValue(value)})");

                        break;                        break;

                    case "<=":                    case "<=":

                    case "lte":                    case "lte":

                        criteria.Add($"({fieldName}:less_equal:{EscapeZohoValue(value)})");                        criteria.Add($"({fieldName}:less_equal:{EscapeZohoValue(value)})");

                        break;                        break;

                    default:                    default:

                        // Default to equality                        // Default to equality

                        criteria.Add($"({fieldName}:equals:{EscapeZohoValue(value)})");                        criteria.Add($"({fieldName}:equals:{EscapeZohoValue(value)})");

                        break;                        break;

                }                }

            }            }



            if (criteria.Any())            if (criteria.Any())

            {            {

                queryParams["criteria"] = string.Join("and", criteria);                queryParams["criteria"] = string.Join("and", criteria);

            }            }



            return queryParams;            return queryParams;

        }        }



        // Escape special characters in Zoho search values        // Escape special characters in Zoho search values

        private string EscapeZohoValue(string? value)        private string EscapeZohoValue(string value)

        {        {

            if (string.IsNullOrEmpty(value)) return value ?? "";            if (string.IsNullOrEmpty(value)) return value;

                        

            // Escape common special characters for Zoho CRM API            // Escape common special characters for Zoho CRM API

            return value.Replace("'", "\\'").Replace("\"", "\\\"");            return value.Replace("'", "\\'").Replace("\"", "\\\"");

        }        }



        // Extract data from Zoho JSON response        // Extract data from Zoho JSON response

        private IEnumerable<object> ExtractZohoData(string jsonContent, string endpoint)        private IEnumerable<object> ExtractZohoData(string jsonContent, string endpoint)

        {        {

            try            try

            {            {

                if (string.IsNullOrWhiteSpace(jsonContent))                if (string.IsNullOrWhiteSpace(jsonContent))

                    return Enumerable.Empty<object>();                    return Enumerable.Empty<object>();



                using var document = JsonDocument.Parse(jsonContent);                using var document = JsonDocument.Parse(jsonContent);

                var root = document.RootElement;                var root = document.RootElement;



                // Zoho returns different structures:                // Zoho returns different structures:

                // For most entities: { "data": [...] }                // For most entities: { "data": [...] }

                // For users: { "users": [...] }                // For users: { "users": [...] }

                                

                var propertyName = endpoint.Equals("users", StringComparison.OrdinalIgnoreCase) ? "users" : "data";                var propertyName = endpoint.Equals("users", StringComparison.OrdinalIgnoreCase) ? "users" : "data";

                                

                if (root.TryGetProperty(propertyName, out var dataElement) && dataElement.ValueKind == JsonValueKind.Array)                if (root.TryGetProperty(propertyName, out var dataElement) && dataElement.ValueKind == JsonValueKind.Array)

                {                {

                    return ExtractArrayItems(dataElement);                    return ExtractArrayItems(dataElement);

                }                }



                // If root is an array                // If root is an array

                if (root.ValueKind == JsonValueKind.Array)                if (root.ValueKind == JsonValueKind.Array)

                {                {

                    return ExtractArrayItems(root);                    return ExtractArrayItems(root);

                }                }



                // If single object, wrap in array                // If single object, wrap in array

                if (root.ValueKind == JsonValueKind.Object)                if (root.ValueKind == JsonValueKind.Object)

                {                {

                    var obj = JsonSerializer.Deserialize<object>(root.GetRawText());                    var obj = JsonSerializer.Deserialize<object>(root.GetRawText());

                    return obj != null ? new[] { obj } : Enumerable.Empty<object>();                    return obj != null ? new[] { obj } : Enumerable.Empty<object>();

                }                }



                return Enumerable.Empty<object>();                return Enumerable.Empty<object>();

            }            }

            catch (Exception ex)            catch (Exception ex)

            {            {

                Logger?.WriteLog($"Error parsing Zoho response: {ex.Message}");                Logger?.WriteLog($"Error parsing Zoho response: {ex.Message}");

                return Enumerable.Empty<object>();                return Enumerable.Empty<object>();

            }            }

        }        }



        // Helper method to extract items from JSON array        // Helper method to extract items from JSON array

        private IEnumerable<object> ExtractArrayItems(JsonElement arrayElement)        private IEnumerable<object> ExtractArrayItems(JsonElement arrayElement)

        {        {

            var results = new List<object>();            var results = new List<object>();

                        

            foreach (var item in arrayElement.EnumerateArray())            foreach (var item in arrayElement.EnumerateArray())

            {            {

                try                try

                {                {

                    var obj = JsonSerializer.Deserialize<object>(item.GetRawText());                    var obj = JsonSerializer.Deserialize<object>(item.GetRawText());

                    if (obj != null)                    if (obj != null)

                        results.Add(obj);                        results.Add(obj);

                }                }

                catch (Exception ex)                catch (Exception ex)

                {                {

                    Logger?.WriteLog($"Error deserializing array item: {ex.Message}");                    Logger?.WriteLog($"Error deserializing array item: {ex.Message}");

                }                }

            }            }



            return results;            return results;

        }        }

    }    }

}}
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
            var (countOnPage, more) = ExtractInfo(resp); // count is this page�s count

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
