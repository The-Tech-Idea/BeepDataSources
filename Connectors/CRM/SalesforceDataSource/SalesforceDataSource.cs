using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
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
using TheTechIdea.Beep.Connectors.Salesforce.Models;

namespace TheTechIdea.Beep.Connectors.Salesforce
{
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Salesforce)]
    public class SalesforceDataSource : WebAPIDataSource
    {
        // Fixed SObjects (add/remove as your connector requires)
        private static readonly List<string> KnownEntities = new()
        {
            "Account","Contact","Lead","Opportunity","OpportunityLineItem",
            "Product2","Pricebook2","Case","User","Task"
        };

        public SalesforceDataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor editor,
            DataSourceType type,
            IErrorsInfo errors)
            : base(datasourcename, logger, editor, type, errors)
        {
            // Ensure WebAPI connection props exist (OAuth/Bearer configuration is provided externally)
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
                Dataconnection.ConnectionProp = new WebAPIConnectionProperties();

            // Register fixed entities
            EntitiesNames = KnownEntities.ToList();
            Entities = EntitiesNames
                .Select(n => new EntityStructure { EntityName = n, DatasourceEntityName = n })
                .ToList();
        }

        public new IEnumerable<string> GetEntitesList() => EntitiesNames;

        // ------------------------------------------------------------------
        // OVERRIDES to make Salesforce work with the standard signatures:
        // Translate (EntityName, filters[, page, size]) -> SOQL and call /query
        // via the base HTTP helper (auth/headers/retry handled by base).
        // ------------------------------------------------------------------

        public override IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            EnsureKnown(EntityName);
            var soql = BuildSoql(EntityName, filter, limit: null, offset: null, orderBy: null);
            return QueryRecords(soql, EntityName);
        }

        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            EnsureKnown(EntityName);

            var limit = Math.Max(1, pageSize);
            var offset = Math.Max(0, (Math.Max(1, pageNumber) - 1) * limit);
            var soql = BuildSoql(EntityName, filter, limit, offset, orderBy: null);
            var data = QueryRecords(soql, EntityName).ToList();

            return new PagedResult
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = data.Count, // True total requires a separate COUNT() query
                Data = data
            };
        }

        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            EnsureKnown(EntityName);
            var soql = BuildSoql(EntityName, Filter, limit: null, offset: null, orderBy: null);
            return await QueryRecordsAsync(soql, EntityName).ConfigureAwait(false);
        }

        #region Response Parsing

        private IEnumerable<object>? ParseResponse(string jsonContent, string entityName)
        {
            try
            {
                return entityName switch
                {
                    "Account" => ExtractArray<Account>(jsonContent),
                    "Contact" => ExtractArray<Contact>(jsonContent),
                    "Lead" => ExtractArray<Lead>(jsonContent),
                    "Opportunity" => ExtractArray<Opportunity>(jsonContent),
                    "User" => ExtractArray<User>(jsonContent),
                    _ => ExtractRecords(jsonContent)
                };
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error parsing response for {entityName}: {ex.Message}");
                return ExtractRecords(jsonContent);
            }
        }

        private List<T> ExtractArray<T>(string jsonContent) where T : SalesforceEntityBase
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var queryResult = JsonSerializer.Deserialize<QueryResult<T>>(jsonContent, options);
                if (queryResult?.Records != null)
                {
                    foreach (var item in queryResult.Records)
                    {
                        item.Attach<T>((IDataSource)this);
                    }
                }
                return queryResult?.Records ?? new List<T>();
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error extracting array of {typeof(T).Name}: {ex.Message}");
                return new List<T>();
            }
        }

        #endregion

        #region Command Methods

        private static void EnsureKnown(string entity)
        {
            if (!KnownEntities.Contains(entity, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Unknown Salesforce entity '{entity}'.");
        }

        // Use FIELDS(ALL) for simplicity (available in recent API versions). Replace with explicit fields if needed.
        private static string BuildSoql(string entity, List<AppFilter> filters, int? limit, int? offset, string orderBy)
        {
            var sb = new StringBuilder();
            sb.Append("SELECT FIELDS(ALL) FROM ").Append(entity);

            var where = FiltersToWhere(filters);
            if (!string.IsNullOrWhiteSpace(where)) sb.Append(" WHERE ").Append(where);
            if (!string.IsNullOrWhiteSpace(orderBy)) sb.Append(" ORDER BY ").Append(orderBy);
            if (limit.HasValue) sb.Append(" LIMIT ").Append(limit.Value);
            if (offset.HasValue) sb.Append(" OFFSET ").Append(offset.Value);

            return sb.ToString();
        }

        // Minimal AppFilter -> WHERE translation. Extend as needed for LIKE, IN, etc.
        // --- helpers (private) ---

        private static string FiltersToWhere(List<AppFilter> filters)
        {
            if (filters == null || filters.Count == 0) return null;

            var parts = new List<string>();
            foreach (var f in filters)
            {
                if (string.IsNullOrWhiteSpace(f?.FieldName)) continue;

                var op = string.IsNullOrWhiteSpace(f.Operator) ? "=" : f.Operator.Trim();

                // NULL handling
                if (f.FilterValue == null)
                {
                    if (op == "=") op = "IS";
                    else if (op == "!=" || op == "<>") op = "IS NOT";
                    parts.Add($"{f.FieldName} {op} NULL");
                    continue;
                }

                // Build a SOQL literal from the value (string -> try parse -> fallback to quoted)
                var literal = BuildSoqlLiteral(f.FilterValue);
                parts.Add($"{f.FieldName} {op} {literal}");
            }
            return string.Join(" AND ", parts);
        }

        private static string BuildSoqlLiteral(object value)
        {
            // If the value isn't a string, handle common CLR types directly
            switch (value)
            {
                case bool b:
                    return b ? "true" : "false";
                case byte or sbyte or short or ushort or int or uint or long or ulong:
                    return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
                case float or double or decimal:
                    return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
                case DateTime dt:
                    return $"'{dt.ToUniversalTime():yyyy-MM-ddTHH:mm:ss'Z'}'";
                case DateTimeOffset dto:
                    return $"'{dto.UtcDateTime:yyyy-MM-ddTHH:mm:ss'Z'}'";
                case string s:
                    return BuildSoqlLiteralFromString(s);
                default:
                    // Fallback: quote as string
                    return QuoteSoqlString(value.ToString());
            }
        }

        private static string BuildSoqlLiteralFromString(string s)
        {
            if (string.IsNullOrEmpty(s)) return "''";

            // Try bool
            if (bool.TryParse(s, out var b))
                return b ? "true" : "false";

            // Try integer
            if (long.TryParse(s, System.Globalization.NumberStyles.Integer,
                              System.Globalization.CultureInfo.InvariantCulture, out var l))
                return l.ToString(System.Globalization.CultureInfo.InvariantCulture);

            // Try decimal/float
            if (decimal.TryParse(s, System.Globalization.NumberStyles.Float,
                                 System.Globalization.CultureInfo.InvariantCulture, out var d))
                return d.ToString(System.Globalization.CultureInfo.InvariantCulture);

            // Try DateTimeOffset (covers DateTime too)
            if (DateTimeOffset.TryParse(s, System.Globalization.CultureInfo.InvariantCulture,
                                        System.Globalization.DateTimeStyles.AssumeUniversal |
                                        System.Globalization.DateTimeStyles.AdjustToUniversal, out var dto))
                return $"'{dto.UtcDateTime:yyyy-MM-ddTHH:mm:ss'Z'}'";

            // Fallback: quoted string
            return QuoteSoqlString(s);
        }

        private static string QuoteSoqlString(string s)
        {
            // Escape \ and ' for SOQL
            var escaped = s.Replace("\\", "\\\\").Replace("'", "\\'");
            return $"'{escaped}'";
        }

        // Execute GET /query?q=... using the base HTTP helper (which injects auth/headers)
        private IEnumerable<object> QueryRecords(string soql, string entityName)
            => QueryRecordsAsync(soql, entityName).ConfigureAwait(false).GetAwaiter().GetResult();

        private async Task<IEnumerable<object>> QueryRecordsAsync(string soql, string entityName, CancellationToken ct = default)
        {
            var qp = new Dictionary<string, string> { ["q"] = soql };

            // Assumes BaseUrl ends with /services/data/vXX.X/
            using HttpResponseMessage resp = await GetAsync("query", qp, cancellationToken: ct).ConfigureAwait(false);
            if (resp == null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            var json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return ParseResponse(json, entityName) ?? Array.Empty<object>();
        }

        // Unwraps { totalSize, done, records: [ {...}, ... ] } -> IEnumerable<object> (Dictionary<string,object>)
        private static IEnumerable<object> ExtractRecords(string json)
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("records", out var arr) &&
                arr.ValueKind == JsonValueKind.Array)
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var list = new List<object>(arr.GetArrayLength());
                foreach (var el in arr.EnumerateArray())
                {
                    var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(el.GetRawText(), opts);
                    if (dict != null) list.Add(dict);
                }
                return list;
            }
            return Array.Empty<object>();
        }

        // CommandAttribute methods for Salesforce API
        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Salesforce, PointType = EnumPointType.Function, ObjectType = "Accounts", ClassName = "SalesforceDataSource", Showin = ShowinType.Both, misc = "IEnumerable<Account>")]
        public async Task<IEnumerable<Account>> GetAccounts(AppFilter filter)
        {
            var result = await GetEntityAsync("Account", new List<AppFilter> { filter });
            return result.Cast<Account>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Salesforce, PointType = EnumPointType.Function, ObjectType = "Contacts", ClassName = "SalesforceDataSource", Showin = ShowinType.Both, misc = "IEnumerable<Contact>")]
        public async Task<IEnumerable<Contact>> GetContacts(AppFilter filter)
        {
            var result = await GetEntityAsync("Contact", new List<AppFilter> { filter });
            return result.Cast<Contact>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Salesforce, PointType = EnumPointType.Function, ObjectType = "Leads", ClassName = "SalesforceDataSource", Showin = ShowinType.Both, misc = "IEnumerable<Lead>")]
        public async Task<IEnumerable<Lead>> GetLeads(AppFilter filter)
        {
            var result = await GetEntityAsync("Lead", new List<AppFilter> { filter });
            return result.Cast<Lead>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Salesforce, PointType = EnumPointType.Function, ObjectType = "Opportunities", ClassName = "SalesforceDataSource", Showin = ShowinType.Both, misc = "IEnumerable<Opportunity>")]
        public async Task<IEnumerable<Opportunity>> GetOpportunities(AppFilter filter)
        {
            var result = await GetEntityAsync("Opportunity", new List<AppFilter> { filter });
            return result.Cast<Opportunity>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Salesforce, PointType = EnumPointType.Function, ObjectType = "Users", ClassName = "SalesforceDataSource", Showin = ShowinType.Both, misc = "IEnumerable<User>")]
        public async Task<IEnumerable<User>> GetUsers(AppFilter filter)
        {
            var result = await GetEntityAsync("User", new List<AppFilter> { filter });
            return result.Cast<User>();
        }

        #endregion
    }
}
