using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
using TheTechIdea.Beep.Connectors.AzureIoTHub;

namespace TheTechIdea.Beep.Connectors.AzureIoTHub
{
    /// <summary>
    /// Azure IoT Hub data source implementation using WebAPIDataSource as base class
    /// Incorporates Azure best practices: async patterns, error handling, structured logging
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.AzureIoTHub)]
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
                Logger?.LogError($"Azure IoT Hub API call failed for entity '{EntityName}': {resp?.StatusCode}");
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

        // CommandAttribute methods for framework integration
        [CommandAttribute(
            ObjectType = "Device",
            PointType = EnumPointType.Function,
            Name = "GetDevices",
            Caption = "Get Azure IoT Hub Devices",
            ClassName = "AzureIoTHubDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.AzureIoTHub,
            Showin = ShowinType.Both,
            Order = 1,
            iconimage = "getdevices.png",
            misc = "ReturnType: IEnumerable<Device>"
        )]
        public IEnumerable<Device> GetDevices()
        {
            return GetEntity("devices", null).Cast<Device>();
        }

        [CommandAttribute(
            ObjectType = "Device",
            PointType = EnumPointType.Function,
            Name = "GetDevice",
            Caption = "Get Azure IoT Hub Device",
            ClassName = "AzureIoTHubDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.AzureIoTHub,
            Showin = ShowinType.Both,
            Order = 2,
            iconimage = "getdevice.png",
            misc = "ReturnType: Device"
        )]
        public Device GetDevice(string deviceId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "device_id", FilterValue = deviceId } };
            return GetEntity("devices", filters).Cast<Device>().FirstOrDefault();
        }

        [CommandAttribute(
            ObjectType = "DeviceTwin",
            PointType = EnumPointType.Function,
            Name = "GetDeviceTwins",
            Caption = "Get Azure IoT Hub Device Twins",
            ClassName = "AzureIoTHubDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.AzureIoTHub,
            Showin = ShowinType.Both,
            Order = 3,
            iconimage = "getdevicetwins.png",
            misc = "ReturnType: IEnumerable<DeviceTwin>"
        )]
        public IEnumerable<DeviceTwin> GetDeviceTwins(string deviceId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "device_id", FilterValue = deviceId } };
            return GetEntity("device_twins", filters).Cast<DeviceTwin>();
        }

        [CommandAttribute(
            ObjectType = "Job",
            PointType = EnumPointType.Function,
            Name = "GetJobs",
            Caption = "Get Azure IoT Hub Jobs",
            ClassName = "AzureIoTHubDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.AzureIoTHub,
            Showin = ShowinType.Both,
            Order = 4,
            iconimage = "getjobs.png",
            misc = "ReturnType: IEnumerable<Job>"
        )]
        public IEnumerable<Job> GetJobs()
        {
            return GetEntity("jobs", null).Cast<Job>();
        }

        [CommandAttribute(
            ObjectType = "Configuration",
            PointType = EnumPointType.Function,
            Name = "GetConfigurations",
            Caption = "Get Azure IoT Hub Configurations",
            ClassName = "AzureIoTHubDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.AzureIoTHub,
            Showin = ShowinType.Both,
            Order = 5,
            iconimage = "getconfigurations.png",
            misc = "ReturnType: IEnumerable<Configuration>"
        )]
        public IEnumerable<Configuration> GetConfigurations()
        {
            return GetEntity("configurations", null).Cast<Configuration>();
        }

        [CommandAttribute(
            ObjectType = "Telemetry",
            PointType = EnumPointType.Function,
            Name = "GetTelemetry",
            Caption = "Get Azure IoT Hub Telemetry",
            ClassName = "AzureIoTHubDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.AzureIoTHub,
            Showin = ShowinType.Both,
            Order = 6,
            iconimage = "gettelemetry.png",
            misc = "ReturnType: IEnumerable<Telemetry>"
        )]
        public IEnumerable<Telemetry> GetTelemetry(string deviceId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "device_id", FilterValue = deviceId } };
            return GetEntity("telemetry", filters).Cast<Telemetry>();
        }

        [CommandAttribute(
            ObjectType = "Module",
            PointType = EnumPointType.Function,
            Name = "GetModules",
            Caption = "Get Azure IoT Hub Modules",
            ClassName = "AzureIoTHubDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.AzureIoTHub,
            Showin = ShowinType.Both,
            Order = 7,
            iconimage = "getmodules.png",
            misc = "ReturnType: IEnumerable<Module>"
        )]
        public IEnumerable<Module> GetModules(string deviceId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "device_id", FilterValue = deviceId } };
            return GetEntity("modules", filters).Cast<Module>();
        }

        // -------------------- Create / Update (POST/PUT) methods --------------------

        [CommandAttribute(
            Name = "CreateDeviceAsync",
            Caption = "Create Azure IoT Hub Device",
            ObjectType = "Device",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.AzureIoTHub,
            ClassType = "AzureIoTHubDataSource",
            Showin = ShowinType.Both,
            Order = 1,
            iconimage = "createdevice.png",
            misc = "ReturnType: IEnumerable<Device>"
        )]
        public async Task<IEnumerable<Device>> CreateDeviceAsync(Device device)
        {
            try
            {
                using var resp = await PostAsync("devices", device);
                if (resp == null || !resp.IsSuccessStatusCode) return new List<Device>();
                var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var createdDevice = JsonSerializer.Deserialize<Device>(json, opts);
                if (createdDevice != null)
                {
                    createdDevice.Attach<Device>(this);
                    return new List<Device> { createdDevice };
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating device: {ex.Message}");
            }
            return new List<Device>();
        }

        [CommandAttribute(
            Name = "UpdateDeviceAsync",
            Caption = "Update Azure IoT Hub Device",
            ObjectType = "Device",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.AzureIoTHub,
            ClassType = "AzureIoTHubDataSource",
            Showin = ShowinType.Both,
            Order = 2,
            iconimage = "updatedevice.png",
            misc = "ReturnType: IEnumerable<Device>"
        )]
        public async Task<IEnumerable<Device>> UpdateDeviceAsync(string deviceId, Device device)
        {
            try
            {
                using var resp = await PutAsync($"devices/{deviceId}", device);
                if (resp == null || !resp.IsSuccessStatusCode) return new List<Device>();
                var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var updatedDevice = JsonSerializer.Deserialize<Device>(json, opts);
                if (updatedDevice != null)
                {
                    updatedDevice.Attach<Device>(this);
                    return new List<Device> { updatedDevice };
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating device: {ex.Message}");
            }
            return new List<Device>();
        }

        [CommandAttribute(
            Name = "CreateDeviceTwinAsync",
            Caption = "Create Azure IoT Hub Device Twin",
            ObjectType = "DeviceTwin",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.AzureIoTHub,
            ClassType = "AzureIoTHubDataSource",
            Showin = ShowinType.Both,
            Order = 3,
            iconimage = "createdevicetwin.png",
            misc = "ReturnType: IEnumerable<DeviceTwin>"
        )]
        public async Task<IEnumerable<DeviceTwin>> CreateDeviceTwinAsync(string deviceId, DeviceTwin twin)
        {
            try
            {
                using var resp = await PutAsync($"twins/{deviceId}", twin);
                if (resp == null || !resp.IsSuccessStatusCode) return new List<DeviceTwin>();
                var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var createdTwin = JsonSerializer.Deserialize<DeviceTwin>(json, opts);
                if (createdTwin != null)
                {
                    createdTwin.Attach<DeviceTwin>(this);
                    return new List<DeviceTwin> { createdTwin };
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating device twin: {ex.Message}");
            }
            return new List<DeviceTwin>();
        }

        [CommandAttribute(
            Name = "UpdateDeviceTwinAsync",
            Caption = "Update Azure IoT Hub Device Twin",
            ObjectType = "DeviceTwin",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.AzureIoTHub,
            ClassType = "AzureIoTHubDataSource",
            Showin = ShowinType.Both,
            Order = 3,
            iconimage = "updatedevicetwin.png",
            misc = "ReturnType: IEnumerable<DeviceTwin>"
        )]
        public async Task<IEnumerable<DeviceTwin>> UpdateDeviceTwinAsync(string deviceId, DeviceTwin twin)
        {
            try
            {
                using var resp = await PatchAsync($"twins/{deviceId}", twin);
                if (resp == null || !resp.IsSuccessStatusCode) return new List<DeviceTwin>();
                var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var updatedTwin = JsonSerializer.Deserialize<DeviceTwin>(json, opts);
                if (updatedTwin != null)
                {
                    updatedTwin.Attach<DeviceTwin>(this);
                    return new List<DeviceTwin> { updatedTwin };
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating device twin: {ex.Message}");
            }
            return new List<DeviceTwin>();
        }

        [CommandAttribute(
            Name = "CreateJobAsync",
            Caption = "Create Azure IoT Hub Job",
            ObjectType = "Job",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.AzureIoTHub,
            ClassType = "AzureIoTHubDataSource",
            Showin = ShowinType.Both,
            Order = 4,
            iconimage = "createjob.png",
            misc = "ReturnType: IEnumerable<Job>"
        )]
        public async Task<IEnumerable<Job>> CreateJobAsync(Job job)
        {
            try
            {
                using var resp = await PostAsync("jobs", job);
                if (resp == null || !resp.IsSuccessStatusCode) return new List<Job>();
                var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var createdJob = JsonSerializer.Deserialize<Job>(json, opts);
                if (createdJob != null)
                {
                    createdJob.Attach<Job>(this);
                    return new List<Job> { createdJob };
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating job: {ex.Message}");
            }
            return new List<Job>();
        }

        [CommandAttribute(
            Name = "UpdateJobAsync",
            Caption = "Update Azure IoT Hub Job",
            ObjectType = "Job",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.AzureIoTHub,
            ClassType = "AzureIoTHubDataSource",
            Showin = ShowinType.Both,
            Order = 5,
            iconimage = "updatejob.png",
            misc = "ReturnType: IEnumerable<Job>"
        )]
        public async Task<IEnumerable<Job>> UpdateJobAsync(string jobId, Job job)
        {
            try
            {
                var endpoint = $"jobs/{Uri.EscapeDataString(jobId)}";
                using var resp = await PutAsync(endpoint, job);
                if (resp == null || !resp.IsSuccessStatusCode) return new List<Job>();
                var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var updatedJob = JsonSerializer.Deserialize<Job>(json, opts);
                if (updatedJob != null)
                {
                    updatedJob.Attach<Job>(this);
                    return new List<Job> { updatedJob };
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating job: {ex.Message}");
            }
            return new List<Job>();
        }

        [CommandAttribute(
            Name = "CreateConfigurationAsync",
            Caption = "Create Azure IoT Hub Configuration",
            ObjectType = "Configuration",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.AzureIoTHub,
            ClassType = "AzureIoTHubDataSource",
            Showin = ShowinType.Both,
            Order = 5,
            iconimage = "createconfiguration.png",
            misc = "ReturnType: IEnumerable<Configuration>"
        )]
        public async Task<IEnumerable<Configuration>> CreateConfigurationAsync(Configuration configuration)
        {
            try
            {
                using var resp = await PostAsync("configurations", configuration);
                if (resp == null || !resp.IsSuccessStatusCode) return new List<Configuration>();
                var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var createdConfiguration = JsonSerializer.Deserialize<Configuration>(json, opts);
                if (createdConfiguration != null)
                {
                    createdConfiguration.Attach<Configuration>(this);
                    return new List<Configuration> { createdConfiguration };
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating configuration: {ex.Message}");
            }
            return new List<Configuration>();
        }

        [CommandAttribute(
            Name = "UpdateConfigurationAsync",
            Caption = "Update Azure IoT Hub Configuration",
            ObjectType = "Configuration",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.AzureIoTHub,
            ClassType = "AzureIoTHubDataSource",
            Showin = ShowinType.Both,
            Order = 6,
            iconimage = "updateconfiguration.png",
            misc = "ReturnType: IEnumerable<Configuration>"
        )]
        public async Task<IEnumerable<Configuration>> UpdateConfigurationAsync(string configurationId, Configuration configuration)
        {
            try
            {
                var endpoint = $"configurations/{Uri.EscapeDataString(configurationId)}";
                using var resp = await PutAsync(endpoint, configuration);
                if (resp == null || !resp.IsSuccessStatusCode) return new List<Configuration>();
                var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var updatedConfiguration = JsonSerializer.Deserialize<Configuration>(json, opts);
                if (updatedConfiguration != null)
                {
                    updatedConfiguration.Attach<Configuration>(this);
                    return new List<Configuration> { updatedConfiguration };
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating configuration: {ex.Message}");
            }
            return new List<Configuration>();
        }

        [CommandAttribute(
            Name = "CreateModuleAsync",
            Caption = "Create Azure IoT Hub Module",
            ObjectType = "Module",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.AzureIoTHub,
            ClassType = "AzureIoTHubDataSource",
            Showin = ShowinType.Both,
            Order = 7,
            iconimage = "createmodule.png",
            misc = "ReturnType: IEnumerable<Module>"
        )]
        public async Task<IEnumerable<Module>> CreateModuleAsync(string deviceId, Module module)
        {
            try
            {
                var endpoint = $"devices/{Uri.EscapeDataString(deviceId)}/modules";
                using var resp = await PostAsync(endpoint, module);
                if (resp == null || !resp.IsSuccessStatusCode) return new List<Module>();
                var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var createdModule = JsonSerializer.Deserialize<Module>(json, opts);
                if (createdModule != null)
                {
                    createdModule.Attach<Module>(this);
                    return new List<Module> { createdModule };
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating module: {ex.Message}");
            }
            return new List<Module>();
        }

        [CommandAttribute(
            Name = "UpdateModuleAsync",
            Caption = "Update Azure IoT Hub Module",
            ObjectType = "Module",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.AzureIoTHub,
            ClassType = "AzureIoTHubDataSource",
            Showin = ShowinType.Both,
            Order = 8,
            iconimage = "updatemodule.png",
            misc = "ReturnType: IEnumerable<Module>"
        )]
        public async Task<IEnumerable<Module>> UpdateModuleAsync(string deviceId, string moduleId, Module module)
        {
            try
            {
                var endpoint = $"devices/{Uri.EscapeDataString(deviceId)}/modules/{Uri.EscapeDataString(moduleId)}";
                using var resp = await PutAsync(endpoint, module);
                if (resp == null || !resp.IsSuccessStatusCode) return new List<Module>();
                var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var updatedModule = JsonSerializer.Deserialize<Module>(json, opts);
                if (updatedModule != null)
                {
                    updatedModule.Attach<Module>(this);
                    return new List<Module> { updatedModule };
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating module: {ex.Message}");
            }
            return new List<Module>();
        }

        [CommandAttribute(
            Name = "CreateModuleTwinAsync",
            Caption = "Create Azure IoT Hub Module Twin",
            ObjectType = "ModuleTwin",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.AzureIoTHub,
            ClassType = "AzureIoTHubDataSource",
            Showin = ShowinType.Both,
            Order = 9,
            iconimage = "createmoduletwin.png",
            misc = "ReturnType: IEnumerable<ModuleTwin>"
        )]
        public async Task<IEnumerable<ModuleTwin>> CreateModuleTwinAsync(string deviceId, string moduleId, ModuleTwin twin)
        {
            try
            {
                var endpoint = $"twins/{Uri.EscapeDataString(deviceId)}/modules/{Uri.EscapeDataString(moduleId)}";
                using var resp = await PutAsync(endpoint, twin);
                if (resp == null || !resp.IsSuccessStatusCode) return new List<ModuleTwin>();
                var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var createdTwin = JsonSerializer.Deserialize<ModuleTwin>(json, opts);
                if (createdTwin != null)
                {
                    createdTwin.Attach<ModuleTwin>(this);
                    return new List<ModuleTwin> { createdTwin };
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating module twin: {ex.Message}");
            }
            return new List<ModuleTwin>();
        }

        [CommandAttribute(
            Name = "UpdateModuleTwinAsync",
            Caption = "Update Azure IoT Hub Module Twin",
            ObjectType = "ModuleTwin",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.AzureIoTHub,
            ClassType = "AzureIoTHubDataSource",
            Showin = ShowinType.Both,
            Order = 10,
            iconimage = "updatemoduletwin.png",
            misc = "ReturnType: IEnumerable<ModuleTwin>"
        )]
        public async Task<IEnumerable<ModuleTwin>> UpdateModuleTwinAsync(string deviceId, string moduleId, ModuleTwin twin)
        {
            try
            {
                var endpoint = $"twins/{Uri.EscapeDataString(deviceId)}/modules/{Uri.EscapeDataString(moduleId)}";
                using var resp = await PatchAsync(endpoint, twin);
                if (resp == null || !resp.IsSuccessStatusCode) return new List<ModuleTwin>();
                var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var updatedTwin = JsonSerializer.Deserialize<ModuleTwin>(json, opts);
                if (updatedTwin != null)
                {
                    updatedTwin.Attach<ModuleTwin>(this);
                    return new List<ModuleTwin> { updatedTwin };
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating module twin: {ex.Message}");
            }
            return new List<ModuleTwin>();
        }

        [CommandAttribute(
            Name = "SendTelemetryAsync",
            Caption = "Send Azure IoT Hub Telemetry",
            ObjectType = "Telemetry",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.AzureIoTHub,
            ClassType = "AzureIoTHubDataSource",
            Showin = ShowinType.Both,
            Order = 11,
            iconimage = "sendtelemetry.png",
            misc = "ReturnType: IEnumerable<Telemetry>"
        )]
        public async Task<IEnumerable<Telemetry>> SendTelemetryAsync(string deviceId, Telemetry telemetry)
        {
            try
            {
                var endpoint = $"devices/{Uri.EscapeDataString(deviceId)}/messages/events";
                var result = await PostAsync(endpoint, telemetry);
                // Telemetry sending typically returns success/failure, not the telemetry data
                // Return the sent telemetry for consistency with the pattern
                telemetry.Attach<Telemetry>(this);
                return new List<Telemetry> { telemetry };
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error sending telemetry: {ex.Message}");
            }
            return new List<Telemetry>();
        }
    }
}