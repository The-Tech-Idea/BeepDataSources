using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.WebAPI;

namespace TheTechIdea.Beep.Connectors.Particle
{
    public class ParticleDataSource : WebAPIDataSource
    {
        private const string BaseUrl = "https://api.particle.io/v1";

        public ParticleDataSource(string datasourcename, IConfigEditor configEditor) : base(datasourcename, configEditor)
        {
            InitializeEntityEndpoints();
            InitializeRequiredFilters();
        }

        private void InitializeEntityEndpoints()
        {
            EntityEndpoints = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["devices"] = $"{BaseUrl}/devices",
                ["device_details"] = $"{BaseUrl}/devices/{{device_id}}",
                ["device_events"] = $"{BaseUrl}/devices/{{device_id}}/events",
                ["device_variables"] = $"{BaseUrl}/devices/{{device_id}}/{{variable_name}}",
                ["device_functions"] = $"{BaseUrl}/devices/{{device_id}}/{{function_name}}",
                ["products"] = $"{BaseUrl}/products",
                ["product_details"] = $"{BaseUrl}/products/{{product_id}}",
                ["product_devices"] = $"{BaseUrl}/products/{{product_id}}/devices",
                ["product_firmware"] = $"{BaseUrl}/products/{{product_id}}/firmware",
                ["integrations"] = $"{BaseUrl}/integrations",
                ["integration_details"] = $"{BaseUrl}/integrations/{{integration_id}}",
                ["webhooks"] = $"{BaseUrl}/webhooks",
                ["webhook_details"] = $"{BaseUrl}/webhooks/{{webhook_id}}",
                ["events"] = $"{BaseUrl}/events",
                ["event_details"] = $"{BaseUrl}/events/{{event_name}}",
                ["tokens"] = $"{BaseUrl}/access_tokens",
                ["token_details"] = $"{BaseUrl}/access_tokens/{{token_id}}",
                ["sims"] = $"{BaseUrl}/sims",
                ["sim_details"] = $"{BaseUrl}/sims/{{sim_id}}",
                ["billing"] = $"{BaseUrl}/billing",
                ["diagnostics"] = $"{BaseUrl}/diagnostics"
            };
        }

        private void InitializeRequiredFilters()
        {
            RequiredFilters = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["devices"] = new List<string> { "access_token" },
                ["device_details"] = new List<string> { "access_token", "device_id" },
                ["device_events"] = new List<string> { "access_token", "device_id" },
                ["device_variables"] = new List<string> { "access_token", "device_id", "variable_name" },
                ["device_functions"] = new List<string> { "access_token", "device_id", "function_name" },
                ["products"] = new List<string> { "access_token" },
                ["product_details"] = new List<string> { "access_token", "product_id" },
                ["product_devices"] = new List<string> { "access_token", "product_id" },
                ["product_firmware"] = new List<string> { "access_token", "product_id" },
                ["integrations"] = new List<string> { "access_token" },
                ["integration_details"] = new List<string> { "access_token", "integration_id" },
                ["webhooks"] = new List<string> { "access_token" },
                ["webhook_details"] = new List<string> { "access_token", "webhook_id" },
                ["events"] = new List<string> { "access_token" },
                ["event_details"] = new List<string> { "access_token", "event_name" },
                ["tokens"] = new List<string> { "access_token" },
                ["token_details"] = new List<string> { "access_token", "token_id" },
                ["sims"] = new List<string> { "access_token" },
                ["sim_details"] = new List<string> { "access_token", "sim_id" },
                ["billing"] = new List<string> { "access_token" },
                ["diagnostics"] = new List<string> { "access_token" }
            };
        }

        public override async Task<object> GetEntity(string EntityName, List<ChildRelation> Parententity, List<string> ParentIds, string filter, int pageNumber = 1, int pageSize = 100)
        {
            try
            {
                if (!EntityEndpoints.ContainsKey(EntityName))
                {
                    throw new ArgumentException($"Entity '{EntityName}' is not supported by Particle API");
                }

                var endpoint = EntityEndpoints[EntityName];
                var requiredFilters = RequiredFilters.ContainsKey(EntityName) ? RequiredFilters[EntityName] : new List<string>();

                // Validate required filters
                foreach (var requiredFilter in requiredFilters)
                {
                    if (!string.IsNullOrEmpty(requiredFilter) && !filter.Contains(requiredFilter))
                    {
                        throw new ArgumentException($"Required filter '{requiredFilter}' is missing for entity '{EntityName}'");
                    }
                }

                // Build query parameters
                var queryParams = BuildQueryParameters(filter, pageNumber, pageSize);

                // Make API call
                var response = await MakeApiCall(endpoint, queryParams);

                if (string.IsNullOrEmpty(response))
                {
                    return new List<object>();
                }

                // Parse response based on entity type
                return ParseResponse(EntityName, response);
            }
            catch (Exception ex)
            {
                // Log error and return empty list
                Console.WriteLine($"Error getting entity '{EntityName}': {ex.Message}");
                return new List<object>();
            }
        }

        private Dictionary<string, string> BuildQueryParameters(string filter, int pageNumber, int pageSize)
        {
            var queryParams = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(filter))
            {
                // Parse filter string into key-value pairs
                var filterPairs = ParseFilterString(filter);
                foreach (var pair in filterPairs)
                {
                    queryParams[pair.Key] = pair.Value;
                }
            }

            // Add pagination if supported
            if (pageNumber > 1)
            {
                queryParams["page"] = pageNumber.ToString();
            }

            if (pageSize != 100)
            {
                queryParams["per_page"] = pageSize.ToString();
            }

            return queryParams;
        }

        private Dictionary<string, string> ParseFilterString(string filter)
        {
            var result = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(filter))
            {
                return result;
            }

            // Simple filter parsing - can be enhanced based on needs
            var pairs = filter.Split('&');
            foreach (var pair in pairs)
            {
                var keyValue = pair.Split('=');
                if (keyValue.Length == 2)
                {
                    result[keyValue[0].Trim()] = keyValue[1].Trim();
                }
            }

            return result;
        }

        private async Task<string> MakeApiCall(string endpoint, Dictionary<string, string> queryParams)
        {
            try
            {
                var url = endpoint;

                // Add query parameters to URL
                if (queryParams.Count > 0)
                {
                    var queryString = string.Join("&", queryParams.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
                    url += $"?{queryString}";
                }

                // Use base class HTTP functionality
                return await base.GetDataAsync(url);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error making API call to {endpoint}: {ex.Message}");
                return string.Empty;
            }
        }

        private object ParseResponse(string entityName, string response)
        {
            try
            {
                if (string.IsNullOrEmpty(response))
                {
                    return new List<object>();
                }

                // Get the appropriate type from registry
                if (!ParticleEntityRegistry.Types.ContainsKey(entityName))
                {
                    // Return raw JSON for unknown entities
                    return JsonSerializer.Deserialize<List<Dictionary<string, object>>>(response) ?? new List<Dictionary<string, object>>();
                }

                var entityType = ParticleEntityRegistry.Types[entityName];

                // Try to parse as array first
                try
                {
                    return ExtractArray(response, entityType);
                }
                catch
                {
                    // If array parsing fails, try single object
                    var singleObject = JsonSerializer.Deserialize(response, entityType);
                    return singleObject != null ? new List<object> { singleObject } : new List<object>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing response for entity '{entityName}': {ex.Message}");
                return new List<object>();
            }
        }

        private List<object> ExtractArray(string jsonString, Type entityType)
        {
            try
            {
                // Try to deserialize as array
                var result = JsonSerializer.Deserialize(jsonString, typeof(List<>).MakeGenericType(entityType));
                return result as List<object> ?? new List<object>();
            }
            catch
            {
                // If direct array deserialization fails, try to extract from wrapper
                try
                {
                    var wrapper = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);
                    if (wrapper != null && wrapper.ContainsKey("data"))
                    {
                        var dataJson = JsonSerializer.Serialize(wrapper["data"]);
                        var result = JsonSerializer.Deserialize(dataJson, typeof(List<>).MakeGenericType(entityType));
                        return result as List<object> ?? new List<object>();
                    }
                }
                catch
                {
                    // Last resort - return empty list
                }

                return new List<object>();
            }
        }

        public override async Task<object> GetEntityAsync(string EntityName, List<ChildRelation> Parententity, List<string> ParentIds, string filter, int pageNumber = 1, int pageSize = 100)
        {
            return await GetEntity(EntityName, Parententity, ParentIds, filter, pageNumber, pageSize);
        }

        public override async Task<List<object>> GetEntitiesAsync(string EntityName, List<ChildRelation> Parententity, List<string> ParentIds, string filter, int pageNumber = 1, int pageSize = 100)
        {
            var result = await GetEntity(EntityName, Parententity, ParentIds, filter, pageNumber, pageSize);
            return result as List<object> ?? new List<object>();
        }

        public override async Task<List<object>> GetEntities(string EntityName, List<ChildRelation> Parententity, List<string> ParentIds, string filter, int pageNumber = 1, int pageSize = 100)
        {
            var result = await GetEntity(EntityName, Parententity, ParentIds, filter, pageNumber, pageSize);
            return result as List<object> ?? new List<object>();
        }

        // Additional Particle-specific methods can be added here
        public async Task<List<Device>> GetDevicesAsync(string accessToken)
        {
            var filter = $"access_token={accessToken}";
            var result = await GetEntity("devices", null, null, filter);
            return result as List<Device> ?? new List<Device>();
        }

        public async Task<Device> GetDeviceDetailsAsync(string deviceId, string accessToken)
        {
            var filter = $"access_token={accessToken}&device_id={deviceId}";
            var result = await GetEntity("device_details", null, null, filter);
            var devices = result as List<Device> ?? new List<Device>();
            return devices.FirstOrDefault() ?? new Device();
        }

        public async Task<List<Event>> GetDeviceEventsAsync(string deviceId, string accessToken)
        {
            var filter = $"access_token={accessToken}&device_id={deviceId}";
            var result = await GetEntity("device_events", null, null, filter);
            return result as List<Event> ?? new List<Event>();
        }
    }
}