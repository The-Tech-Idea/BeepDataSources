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

namespace TheTechIdea.Beep.Connectors.AzureIoTHub
{
    /// <summary>
    /// Azure IoT Hub data source implementation using WebAPIDataSource as base class
    /// Incorporates Azure best practices: async patterns, error handling, structured logging
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WebApi)]
    public class AzureIoTHubDataSource : WebAPIDataSource
    {
        // Entity endpoints mapping for Azure IoT Hub API
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // Devices
            ["devices"] = "devices",
            ["device_twins"] = "twins/{device_id}",
            ["device_methods"] = "twins/{device_id}/methods",
            ["device_jobs"] = "jobs/devices/{device_id}",
            // Jobs
            ["jobs"] = "jobs",
            ["job_details"] = "jobs/{job_id}",
            // Configurations
            ["configurations"] = "configurations",
            ["configuration_details"] = "configurations/{configuration_id}",
            // Telemetry
            ["telemetry"] = "devices/{device_id}/messages/events",
            // Modules
            ["modules"] = "devices/{device_id}/modules",
            ["module_twins"] = "twins/{device_id}/modules/{module_id}",
            // Statistics
            ["statistics"] = "statistics/service",
            ["device_statistics"] = "statistics/devices"
        };

        // Required filters for each entity
        private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            ["devices"] = Array.Empty<string>(),
            ["device_twins"] = new[] { "device_id" },
            ["device_methods"] = new[] { "device_id" },
            ["device_jobs"] = new[] { "device_id" },
            ["jobs"] = Array.Empty<string>(),
            ["job_details"] = new[] { "job_id" },
            ["configurations"] = Array.Empty<string>(),
            ["configuration_details"] = new[] { "configuration_id" },
            ["telemetry"] = new[] { "device_id" },
            ["modules"] = new[] { "device_id" },
            ["module_twins"] = new[] { "device_id", "module_id" },
            ["statistics"] = Array.Empty<string>(),
            ["device_statistics"] = Array.Empty<string>()
        };

        public AzureIoTHubDataSource(
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

        // Async - Azure best practice: use async patterns
        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            if (!EntityEndpoints.TryGetValue(EntityName, out var endpoint))
                throw new InvalidOperationException($"Unknown Azure IoT Hub entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            var resolvedEndpoint = ResolveEndpoint(endpoint, q);

            using var resp = await GetAsync(resolvedEndpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode)
            {
                // Azure best practice: structured error handling
                Logger?.WriteLog($"Azure IoT Hub API call failed for entity '{EntityName}': {resp?.StatusCode}", LogType.ERROR);
                return Array.Empty<object>();
            }

            return ExtractArray(resp, "value");
        }

        // Paged
        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            if (!EntityEndpoints.TryGetValue(EntityName, out var endpoint))
                throw new InvalidOperationException($"Unknown Azure IoT Hub entity '{EntityName}'.");

            var q = FiltersToQuery(filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));
            q["top"] = Math.Max(10, Math.Min(pageSize, 1000)).ToString(); // Azure IoT Hub supports up to 1000

            string resolvedEndpoint = ResolveEndpoint(endpoint, q);

            // Azure IoT Hub uses continuation tokens for pagination
            if (pageNumber > 1 && q.TryGetValue("continuation_token", out var token))
            {
                q["continuationToken"] = token;
            }

            var finalResp = CallAzureIoTHub(resolvedEndpoint, q).ConfigureAwait(false).GetAwaiter().GetResult();
            var items = ExtractArray(finalResp, "value");

            return new PagedResult
            {
                Data = items,
                PageNumber = Math.Max(1, pageNumber),
                PageSize = pageSize,
                TotalRecords = items.Count, // Azure IoT Hub doesn't provide total count
                TotalPages = 1, // Azure IoT Hub doesn't provide page count
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
                throw new ArgumentException($"Azure IoT Hub entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ResolveEndpoint(string template, Dictionary<string, string> q)
        {
            // Substitute parameters from filters if present
            var result = template;
            
            if (template.Contains("{device_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("device_id", out var deviceId) || string.IsNullOrWhiteSpace(deviceId))
                    throw new ArgumentException("Missing required 'device_id' filter for this endpoint.");
                result = result.Replace("{device_id}", Uri.EscapeDataString(deviceId));
            }
            
            if (template.Contains("{job_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("job_id", out var jobId) || string.IsNullOrWhiteSpace(jobId))
                    throw new ArgumentException("Missing required 'job_id' filter for this endpoint.");
                result = result.Replace("{job_id}", Uri.EscapeDataString(jobId));
            }
            
            if (template.Contains("{configuration_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("configuration_id", out var configId) || string.IsNullOrWhiteSpace(configId))
                    throw new ArgumentException("Missing required 'configuration_id' filter for this endpoint.");
                result = result.Replace("{configuration_id}", Uri.EscapeDataString(configId));
            }
            
            if (template.Contains("{module_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("module_id", out var moduleId) || string.IsNullOrWhiteSpace(moduleId))
                    throw new ArgumentException("Missing required 'module_id' filter for this endpoint.");
                result = result.Replace("{module_id}", Uri.EscapeDataString(moduleId));
            }
            
            return result;
        }

        private async Task<HttpResponseMessage> CallAzureIoTHub(string endpoint, Dictionary<string, string> query, CancellationToken ct = default)
            => await GetAsync(endpoint, query, cancellationToken: ct).ConfigureAwait(false);

        // Extracts "value" (array or object) into a List<object> (Dictionary<string,object> per item).
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
                    return list; // no "value" -> empty
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