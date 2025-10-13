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
        
        // -------------------- Create / Update (POST/PUT) methods --------------------

        [CommandAttribute(
            Name = "CreateThingAsync",
            Caption = "Create AWS IoT Thing",
            ObjectType = "Device",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.AWSIoT,
            ClassType = "AWSIoTDataSource",
            Showin = ShowinType.Both,
            Order = 1,
            iconimage = "creatething.png",
            misc = "ReturnType: IEnumerable<Device>"
        )]
        public async Task<IEnumerable<Device>> CreateThingAsync(Device thing)
        {
            try
            {
                var result = await PostAsync("things", thing);
                var devices = JsonSerializer.Deserialize<IEnumerable<Device>>(result);
                if (devices != null)
                {
                    foreach (var d in devices)
                    {
                        d.Attach<Device>(this);
                    }
                }
                return devices;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating thing: {ex.Message}");
            }
            return new List<Device>();
        }

        [CommandAttribute(ObjectType = typeof(Device), PointType = PointType.Function, Name = "UpdateThing", Caption = "Update Thing", ClassName = "AWSIoTDataSource", misc = "UpdateThing")]
        public async Task<IEnumerable<Device>> UpdateThingAsync(string thingName, Device thing)
        {
            if (string.IsNullOrWhiteSpace(thingName) || thing == null) return Array.Empty<Device>();
            var endpoint = $"things/{Uri.EscapeDataString(thingName)}";
            using var resp = await PutAsync(endpoint, thing).ConfigureAwait(false);
            if (resp == null || !resp.IsSuccessStatusCode) return Array.Empty<Device>();
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {
                return JsonSerializer.Deserialize<IEnumerable<Device>>(json, opts) ?? Array.Empty<Device>();
            }
            catch
            {
                return Array.Empty<Device>();
            }
        }

        [CommandAttribute(ObjectType = typeof(Shadow), PointType = PointType.Function, Name = "UpdateThingShadow", Caption = "Update Thing Shadow", ClassName = "AWSIoTDataSource", misc = "UpdateThingShadow")]
        public async Task<IEnumerable<Shadow>> UpdateShadowAsync(string thingName, Shadow shadow)
        {
            if (string.IsNullOrWhiteSpace(thingName) || shadow == null) return Array.Empty<Shadow>();
            var endpoint = $"things/{Uri.EscapeDataString(thingName)}/shadow";
            using var resp = await PutAsync(endpoint, shadow).ConfigureAwait(false);
            if (resp == null || !resp.IsSuccessStatusCode) return Array.Empty<Shadow>();
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {
                return JsonSerializer.Deserialize<IEnumerable<Shadow>>(json, opts) ?? Array.Empty<Shadow>();
            }
            catch
            {
                return Array.Empty<Shadow>();
            }
        }

        [CommandAttribute(
            Name = "CreateJobAsync",
            Caption = "Create AWS IoT Job",
            ObjectType = "Job",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.AWSIoT,
            ClassType = "AWSIoTDataSource",
            Showin = ShowinType.Both,
            Order = 2,
            iconimage = "createjob.png",
            misc = "ReturnType: IEnumerable<Job>"
        )]
        public async Task<IEnumerable<Job>> CreateJobAsync(Job job)
        {
            try
            {
                var result = await PostAsync("jobs", job);
                var jobs = JsonSerializer.Deserialize<IEnumerable<Job>>(result);
                if (jobs != null)
                {
                    foreach (var j in jobs)
                    {
                        j.Attach<Job>(this);
                    }
                }
                return jobs;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating job: {ex.Message}");
            }
            return new List<Job>();
        }

        [CommandAttribute(
            Name = "CreateRuleAsync",
            Caption = "Create AWS IoT Rule",
            ObjectType = "Rule",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.AWSIoT,
            ClassType = "AWSIoTDataSource",
            Showin = ShowinType.Both,
            Order = 3,
            iconimage = "createrule.png",
            misc = "ReturnType: IEnumerable<Rule>"
        )]
        public async Task<IEnumerable<Rule>> CreateRuleAsync(Rule rule)
        {
            try
            {
                var result = await PostAsync("rules", rule);
                var rules = JsonSerializer.Deserialize<IEnumerable<Rule>>(result);
                if (rules != null)
                {
                    foreach (var r in rules)
                    {
                        r.Attach<Rule>(this);
                    }
                }
                return rules;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating rule: {ex.Message}");
            }
            return new List<Rule>();
        }

        [CommandAttribute(
            Name = "CreateCertificateAsync",
            Caption = "Create AWS IoT Certificate",
            ObjectType = "Certificate",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.AWSIoT,
            ClassType = "AWSIoTDataSource",
            Showin = ShowinType.Both,
            Order = 4,
            iconimage = "createcertificate.png",
            misc = "ReturnType: IEnumerable<Certificate>"
        )]
        public async Task<IEnumerable<Certificate>> CreateCertificateAsync(Certificate certificate)
        {
            try
            {
                var result = await PostAsync("certificates", certificate);
                var certificates = JsonSerializer.Deserialize<IEnumerable<Certificate>>(result);
                if (certificates != null)
                {
                    foreach (var c in certificates)
                    {
                        c.Attach<Certificate>(this);
                    }
                }
                return certificates;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating certificate: {ex.Message}");
            }
            return new List<Certificate>();
        }

        [CommandAttribute(
            Name = "CreatePolicyAsync",
            Caption = "Create AWS IoT Policy",
            ObjectType = "Policy",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.AWSIoT,
            ClassType = "AWSIoTDataSource",
            Showin = ShowinType.Both,
            Order = 5,
            iconimage = "createpolicy.png",
            misc = "ReturnType: IEnumerable<Policy>"
        )]
        public async Task<IEnumerable<Policy>> CreatePolicyAsync(Policy policy)
        {
            try
            {
                var result = await PostAsync("policies", policy);
                var policies = JsonSerializer.Deserialize<IEnumerable<Policy>>(result);
                if (policies != null)
                {
                    foreach (var p in policies)
                    {
                        p.Attach<Policy>(this);
                    }
                }
                return policies;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating policy: {ex.Message}");
            }
            return new List<Policy>();
        }

        [CommandAttribute(
            Name = "PublishTelemetryAsync",
            Caption = "Publish AWS IoT Telemetry",
            ObjectType = "Telemetry",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.AWSIoT,
            ClassType = "AWSIoTDataSource",
            Showin = ShowinType.Both,
            Order = 6,
            iconimage = "publish.png",
            misc = "ReturnType: IEnumerable<Telemetry>"
        )]
        public async Task<IEnumerable<Telemetry>> PublishTelemetryAsync(string topic, Telemetry telemetry)
        {
            try
            {
                var result = await PostAsync("telemetry", telemetry);
                var telemetries = JsonSerializer.Deserialize<IEnumerable<Telemetry>>(result);
                if (telemetries != null)
                {
                    foreach (var t in telemetries)
                    {
                        t.Attach<Telemetry>(this);
                    }
                }
                return telemetries;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error publishing telemetry: {ex.Message}");
            }
            return new List<Telemetry>();
        }

        [CommandAttribute(
            Name = "UpdateThingAsync",
            Caption = "Update AWS IoT Thing",
            ObjectType = "Device",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.AWSIoT,
            ClassType = "AWSIoTDataSource",
            Showin = ShowinType.Both,
            Order = 7,
            iconimage = "updatething.png",
            misc = "ReturnType: IEnumerable<Device>"
        )]
        public async Task<IEnumerable<Device>> UpdateThingAsync(Device thing)
        {
            try
            {
                var result = await PatchAsync("things", thing);
                var devices = JsonSerializer.Deserialize<IEnumerable<Device>>(result);
                if (devices != null)
                {
                    foreach (var d in devices)
                    {
                        d.Attach<Device>(this);
                    }
                }
                return devices;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating thing: {ex.Message}");
            }
            return new List<Device>();
        }

        [CommandAttribute(
            Name = "UpdateJobAsync",
            Caption = "Update AWS IoT Job",
            ObjectType = "Job",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.AWSIoT,
            ClassType = "AWSIoTDataSource",
            Showin = ShowinType.Both,
            Order = 8,
            iconimage = "updatejob.png",
            misc = "ReturnType: IEnumerable<Job>"
        )]
        public async Task<IEnumerable<Job>> UpdateJobAsync(Job job)
        {
            try
            {
                var result = await PatchAsync("jobs", job);
                var jobs = JsonSerializer.Deserialize<IEnumerable<Job>>(result);
                if (jobs != null)
                {
                    foreach (var j in jobs)
                    {
                        j.Attach<Job>(this);
                    }
                }
                return jobs;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating job: {ex.Message}");
            }
            return new List<Job>();
        }

        [CommandAttribute(
            Name = "UpdateRuleAsync",
            Caption = "Update AWS IoT Rule",
            ObjectType = "Rule",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.AWSIoT,
            ClassType = "AWSIoTDataSource",
            Showin = ShowinType.Both,
            Order = 9,
            iconimage = "updaterule.png",
            misc = "ReturnType: IEnumerable<Rule>"
        )]
        public async Task<IEnumerable<Rule>> UpdateRuleAsync(Rule rule)
        {
            try
            {
                var result = await PatchAsync("rules", rule);
                var rules = JsonSerializer.Deserialize<IEnumerable<Rule>>(result);
                if (rules != null)
                {
                    foreach (var r in rules)
                    {
                        r.Attach<Rule>(this);
                    }
                }
                return rules;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating rule: {ex.Message}");
            }
            return new List<Rule>();
        }

        [CommandAttribute(
            Name = "UpdateCertificateAsync",
            Caption = "Update AWS IoT Certificate",
            ObjectType = "Certificate",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.AWSIoT,
            ClassType = "AWSIoTDataSource",
            Showin = ShowinType.Both,
            Order = 10,
            iconimage = "updatecertificate.png",
            misc = "ReturnType: IEnumerable<Certificate>"
        )]
        public async Task<IEnumerable<Certificate>> UpdateCertificateAsync(Certificate certificate)
        {
            try
            {
                var result = await PatchAsync("certificates", certificate);
                var certificates = JsonSerializer.Deserialize<IEnumerable<Certificate>>(result);
                if (certificates != null)
                {
                    foreach (var c in certificates)
                    {
                        c.Attach<Certificate>(this);
                    }
                }
                return certificates;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating certificate: {ex.Message}");
            }
            return new List<Certificate>();
        }

        [CommandAttribute(
            Name = "UpdatePolicyAsync",
            Caption = "Update AWS IoT Policy",
            ObjectType = "Policy",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.AWSIoT,
            ClassType = "AWSIoTDataSource",
            Showin = ShowinType.Both,
            Order = 11,
            iconimage = "updatepolicy.png",
            misc = "ReturnType: IEnumerable<Policy>"
        )]
        public async Task<IEnumerable<Policy>> UpdatePolicyAsync(Policy policy)
        {
            try
            {
                var result = await PatchAsync("policies", policy);
                var policies = JsonSerializer.Deserialize<IEnumerable<Policy>>(result);
                if (policies != null)
                {
                    foreach (var p in policies)
                    {
                        p.Attach<Policy>(this);
                    }
                }
                return policies;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating policy: {ex.Message}");
            }
            return new List<Policy>();
        }

        [CommandAttribute(
            Name = "UpdateTelemetryAsync",
            Caption = "Update AWS IoT Telemetry",
            ObjectType = "Telemetry",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.AWSIoT,
            ClassType = "AWSIoTDataSource",
            Showin = ShowinType.Both,
            Order = 12,
            iconimage = "update.png",
            misc = "ReturnType: IEnumerable<Telemetry>"
        )]
        public async Task<IEnumerable<Telemetry>> UpdateTelemetryAsync(Telemetry telemetry)
        {
            try
            {
                var result = await PatchAsync("telemetry", telemetry);
                var telemetries = JsonSerializer.Deserialize<IEnumerable<Telemetry>>(result);
                if (telemetries != null)
                {
                    foreach (var t in telemetries)
                    {
                        t.Attach<Telemetry>(this);
                    }
                }
                return telemetries;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating telemetry: {ex.Message}");
            }
            return new List<Telemetry>();
        }
    }
}