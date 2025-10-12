using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.WebAPI;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Connectors.Particle;

namespace TheTechIdea.Beep.Connectors.Particle
{
    public class ParticleDataSource : WebAPIDataSource
    {
        private const string BaseUrl = "https://api.particle.io/v1";

        // Entity endpoints mapping for Particle API
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            ["devices"] = "devices",
            ["device_details"] = "devices/{device_id}",
            ["device_events"] = "devices/{device_id}/events",
            ["products"] = "products",
            ["product_details"] = "products/{product_id}",
            ["customers"] = "products/{product_id}/customers",
            ["customer_details"] = "products/{product_id}/customers/{customer_id}",
            ["sims"] = "sims",
            ["sim_details"] = "sims/{sim_id}",
            ["billing"] = "billing",
            ["diagnostics"] = "diagnostics",
            ["tokens"] = "access_tokens",
            ["token_details"] = "access_tokens/{token_id}"
        };

        // Required filters for each entity
        private static readonly Dictionary<string, List<string>> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            ["devices"] = new List<string> { "access_token" },
            ["device_details"] = new List<string> { "access_token", "device_id" },
            ["device_events"] = new List<string> { "access_token", "device_id" },
            ["products"] = new List<string> { "access_token" },
            ["product_details"] = new List<string> { "access_token", "product_id" },
            ["customers"] = new List<string> { "access_token", "product_id" },
            ["customer_details"] = new List<string> { "access_token", "product_id", "customer_id" },
            ["sims"] = new List<string> { "access_token" },
            ["sim_details"] = new List<string> { "access_token", "sim_id" },
            ["billing"] = new List<string> { "access_token" },
            ["diagnostics"] = new List<string> { "access_token" },
            ["tokens"] = new List<string> { "access_token" },
            ["token_details"] = new List<string> { "access_token", "token_id" }
        };

        public ParticleDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
            : base(datasourcename, logger, pDMEEditor, databasetype, per)
        {
            if (Dataconnection is not WebAPIDataConnection)
            {
                Dataconnection = new WebAPIDataConnection
                {
                    Logger = Logger,
                    ErrorObject = ErrorObject,
                    DMEEditor = DMEEditor
                };
            }

            if (Dataconnection.ConnectionProp is not WebAPIConnectionProperties)
            {
                Dataconnection.ConnectionProp = new WebAPIConnectionProperties();
            }
        }

        public async Task<object> GetEntity(string EntityName, List<ChildRelation> Parententity, List<string> ParentIds, string filter, int pageNumber = 1, int pageSize = 100)
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
                using var response = await base.GetAsync(url);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
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

        // Additional Particle-specific methods can be added here
        public async Task<List<Device>> GetDevicesAsync(string accessToken)
        {
            var filter = $"access_token={accessToken}";
            var result = await GetEntity("devices", new List<ChildRelation>(), new List<string>(), filter);
            return result as List<Device> ?? new List<Device>();
        }

        public async Task<Device> GetDeviceDetailsAsync(string deviceId, string accessToken)
        {
            var filter = $"access_token={accessToken}&device_id={deviceId}";
            var result = await GetEntity("device_details", new List<ChildRelation>(), new List<string>(), filter);
            var devices = result as List<Device> ?? new List<Device>();
            return devices.FirstOrDefault() ?? new Device();
        }

        public async Task<List<Event>> GetDeviceEventsAsync(string deviceId, string accessToken)
        {
            var filter = $"access_token={accessToken}&device_id={deviceId}";
            var result = await GetEntity("device_events", new List<ChildRelation>(), new List<string>(), filter);
            return result as List<Event> ?? new List<Event>();
        }

        // CommandAttribute methods for framework integration
        [CommandAttribute(ObjectType = typeof(Device), PointType = PointType.Function, Name = "GetDevices", Caption = "Get Devices", ClassName = "ParticleDataSource", misc = "GetDevices")]
        public IEnumerable<Device> GetDevices(string accessToken)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "access_token", FilterValue = accessToken } };
            return GetEntity("devices", filters).Cast<Device>();
        }

        [CommandAttribute(ObjectType = typeof(Device), PointType = PointType.Function, Name = "GetDevice", Caption = "Get Device", ClassName = "ParticleDataSource", misc = "GetDevice")]
        public Device GetDevice(string deviceId, string accessToken)
        {
            var filters = new List<AppFilter> {
                new AppFilter { FieldName = "access_token", FilterValue = accessToken },
                new AppFilter { FieldName = "device_id", FilterValue = deviceId }
            };
            return GetEntity("device_details", filters).Cast<Device>().FirstOrDefault();
        }

        [CommandAttribute(ObjectType = typeof(Event), PointType = PointType.Function, Name = "GetDeviceEvents", Caption = "Get Device Events", ClassName = "ParticleDataSource", misc = "GetDeviceEvents")]
        public IEnumerable<Event> GetDeviceEvents(string deviceId, string accessToken)
        {
            var filters = new List<AppFilter> {
                new AppFilter { FieldName = "access_token", FilterValue = accessToken },
                new AppFilter { FieldName = "device_id", FilterValue = deviceId }
            };
            return GetEntity("device_events", filters).Cast<Event>();
        }

        [CommandAttribute(ObjectType = typeof(Product), PointType = PointType.Function, Name = "GetProducts", Caption = "Get Products", ClassName = "ParticleDataSource", misc = "GetProducts")]
        public IEnumerable<Product> GetProducts(string accessToken)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "access_token", FilterValue = accessToken } };
            return GetEntity("products", filters).Cast<Product>();
        }

        [CommandAttribute(ObjectType = typeof(Sim), PointType = PointType.Function, Name = "GetSims", Caption = "Get SIMs", ClassName = "ParticleDataSource", misc = "GetSims")]
        public IEnumerable<Sim> GetSims(string accessToken)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "access_token", FilterValue = accessToken } };
            return GetEntity("sims", filters).Cast<Sim>();
        }

        [CommandAttribute(ObjectType = typeof(AccessToken), PointType = PointType.Function, Name = "GetAccessTokens", Caption = "Get Access Tokens", ClassName = "ParticleDataSource", misc = "GetAccessTokens")]
        public IEnumerable<AccessToken> GetAccessTokens(string accessToken)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "access_token", FilterValue = accessToken } };
            return GetEntity("tokens", filters).Cast<AccessToken>();
        }
    }
}