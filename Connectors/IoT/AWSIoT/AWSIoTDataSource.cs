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
using TheTechIdea.Beep.Connectors.AWSIoT;

namespace TheTechIdea.Beep.Connectors.AWSIoT
{
    /// <summary>
    /// AWS IoT data source implementation using WebAPIDataSource as base class
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WebApi)]
    public class AWSIoTDataSource : WebAPIDataSource
    {
        // Entity endpoints mapping for AWS IoT API
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // Devices and Things
            ["devices"] = "things",
            ["things"] = "things",
            ["shadows"] = "things/{thing_name}/shadow",
            ["jobs"] = "jobs",
            ["rules"] = "rules",
            ["certificates"] = "certificates",
            ["policies"] = "policies",
            ["telemetry"] = "topics/{topic}",
            ["endpoints"] = "endpoints"
        };

        // Required filters for each entity
        private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            ["devices"] = Array.Empty<string>(),
            ["things"] = Array.Empty<string>(),
            ["shadows"] = new[] { "thing_name" },
            ["jobs"] = Array.Empty<string>(),
            ["rules"] = Array.Empty<string>(),
            ["certificates"] = Array.Empty<string>(),
            ["policies"] = Array.Empty<string>(),
            ["telemetry"] = new[] { "topic" },
            ["endpoints"] = Array.Empty<string>()
        };

        public AWSIoTDataSource(
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
                throw new InvalidOperationException($"Unknown AWS IoT entity '{EntityName}'.");

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
                throw new InvalidOperationException($"Unknown AWS IoT entity '{EntityName}'.");

            var q = FiltersToQuery(filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));
            q["page_size"] = Math.Max(10, Math.Min(pageSize, 100)).ToString();

            string resolvedEndpoint = ResolveEndpoint(endpoint, q);

            // For AWS IoT, we'll use simple pagination
            if (pageNumber > 1)
            {
                q["page"] = pageNumber.ToString();
            }

            var finalResp = CallAWSIoT(resolvedEndpoint, q).ConfigureAwait(false).GetAwaiter().GetResult();
            var items = ExtractArray(finalResp, "data");

            return new PagedResult
            {
                Data = items,
                PageNumber = Math.Max(1, pageNumber),
                PageSize = pageSize,
                TotalRecords = items.Count, // AWS IoT doesn't provide total count
                TotalPages = 1, // AWS IoT doesn't provide page count
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
                throw new ArgumentException($"AWS IoT entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ResolveEndpoint(string template, Dictionary<string, string> q)
        {
            // Substitute parameters from filters if present
            var result = template;
            
            if (template.Contains("{thing_name}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("thing_name", out var thingName) || string.IsNullOrWhiteSpace(thingName))
                    throw new ArgumentException("Missing required 'thing_name' filter for this endpoint.");
                result = result.Replace("{thing_name}", Uri.EscapeDataString(thingName));
            }
            
            if (template.Contains("{topic}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("topic", out var topic) || string.IsNullOrWhiteSpace(topic))
                    throw new ArgumentException("Missing required 'topic' filter for this endpoint.");
                result = result.Replace("{topic}", Uri.EscapeDataString(topic));
            }
            
            return result;
        }

        private async Task<HttpResponseMessage> CallAWSIoT(string endpoint, Dictionary<string, string> query, CancellationToken ct = default)
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

        // CommandAttribute methods for framework integration
        [CommandAttribute(ObjectType = typeof(Device), PointType = PointType.Function, Name = "GetThings", Caption = "Get Things", ClassName = "AWSIoTDataSource", misc = "GetThings")]
        public IEnumerable<Device> GetThings()
        {
            return GetEntity("things", null).Cast<Device>();
        }

        [CommandAttribute(ObjectType = typeof(Device), PointType = PointType.Function, Name = "GetThing", Caption = "Get Thing", ClassName = "AWSIoTDataSource", misc = "GetThing")]
        public Device GetThing(string thingName)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "thing_name", FilterValue = thingName } };
            return GetEntity("things", filters).Cast<Device>().FirstOrDefault();
        }

        [CommandAttribute(ObjectType = typeof(Shadow), PointType = PointType.Function, Name = "GetThingShadows", Caption = "Get Thing Shadows", ClassName = "AWSIoTDataSource", misc = "GetThingShadows")]
        public IEnumerable<Shadow> GetThingShadows(string thingName)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "thing_name", FilterValue = thingName } };
            return GetEntity("shadows", filters).Cast<Shadow>();
        }

        [CommandAttribute(ObjectType = typeof(Job), PointType = PointType.Function, Name = "GetJobs", Caption = "Get Jobs", ClassName = "AWSIoTDataSource", misc = "GetJobs")]
        public IEnumerable<Job> GetJobs()
        {
            return GetEntity("jobs", null).Cast<Job>();
        }

        [CommandAttribute(ObjectType = typeof(Rule), PointType = PointType.Function, Name = "GetRules", Caption = "Get Rules", ClassName = "AWSIoTDataSource", misc = "GetRules")]
        public IEnumerable<Rule> GetRules()
        {
            return GetEntity("rules", null).Cast<Rule>();
        }

        [CommandAttribute(ObjectType = typeof(Certificate), PointType = PointType.Function, Name = "GetCertificates", Caption = "Get Certificates", ClassName = "AWSIoTDataSource", misc = "GetCertificates")]
        public IEnumerable<Certificate> GetCertificates()
        {
            return GetEntity("certificates", null).Cast<Certificate>();
        }

        [CommandAttribute(ObjectType = typeof(Policy), PointType = PointType.Function, Name = "GetPolicies", Caption = "Get Policies", ClassName = "AWSIoTDataSource", misc = "GetPolicies")]
        public IEnumerable<Policy> GetPolicies()
        {
            return GetEntity("policies", null).Cast<Policy>();
        }

        [CommandAttribute(ObjectType = typeof(Telemetry), PointType = PointType.Function, Name = "GetTelemetry", Caption = "Get Telemetry", ClassName = "AWSIoTDataSource", misc = "GetTelemetry")]
        public IEnumerable<Telemetry> GetTelemetry(string topic)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "topic", FilterValue = topic } };
            return GetEntity("telemetry", filters).Cast<Telemetry>();
        }
    }
}