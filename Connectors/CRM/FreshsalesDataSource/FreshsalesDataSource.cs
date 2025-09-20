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

namespace TheTechIdea.Beep.Connectors.Freshsales
{
    /// <summary>
    /// Freshsales data source implementation using WebAPIDataSource as base class
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Freshsales)]
    public class FreshsalesDataSource : WebAPIDataSource
    {
        // Known Freshsales entities with their API endpoints
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            ["leads"] = "leads",
            ["contacts"] = "contacts", 
            ["accounts"] = "sales_accounts",
            ["deals"] = "deals",
            ["tasks"] = "tasks",
            ["appointments"] = "appointments",
            ["notes"] = "notes",
            ["products"] = "products",
            ["sales_activities"] = "sales_activities",
            ["users"] = "users",
            ["territories"] = "territories",
            ["teams"] = "teams",
            ["currencies"] = "currencies"
        };

        public FreshsalesDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per) : base(datasourcename, logger, DMEEditor, databasetype, per)
        {
            // Initialize WebAPI connection properties if needed
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
            {
                if (Dataconnection != null)
                    Dataconnection.ConnectionProp = new WebAPIConnectionProperties();
            }

            // Set up entity list
            EntitiesNames = EntityEndpoints.Keys.ToList();
            Entities = EntitiesNames.Select(name => new EntityStructure 
            { 
                EntityName = name, 
                DatasourceEntityName = name 
            }).ToList();
        }

        // Entity list method following Twitter pattern
        public override List<string> GetEntitesList()
        {
            return EntityEndpoints.Keys.ToList();
        }

        // Sync method following Twitter pattern
        public override IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            var result = GetEntityAsync(EntityName, filter).ConfigureAwait(false).GetAwaiter().GetResult();
            return result ?? new List<object>();
        }

        // Async method following Twitter pattern
        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> filter)
        {
            try
            {
                if (!EntityEndpoints.TryGetValue(EntityName, out string? endpoint) || endpoint == null)
                {
                    Logger?.WriteLog($"Unknown entity: {EntityName}");
                    return new List<object>();
                }

                var queryParams = BuildFreshsalesQuery(filter);
                
                // Handle pagination similar to Twitter pattern
                var allResults = new List<object>();
                int page = 1;
                int perPage = 100; // Freshsales default page size
                bool hasMore = true;

                while (hasMore)
                {
                    var paginatedQuery = new Dictionary<string, string>(queryParams)
                    {
                        ["page"] = page.ToString(),
                        ["per_page"] = perPage.ToString()
                    };

                    using var response = await GetAsync(endpoint, paginatedQuery).ConfigureAwait(false);
                    if (response?.IsSuccessStatusCode != true)
                    {
                        Logger?.WriteLog($"Failed to fetch {EntityName} from Freshsales API");
                        break;
                    }

                    var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var results = ExtractFreshsalesData(content);
                    
                    if (results?.Any() == true)
                    {
                        allResults.AddRange(results);
                        page++;
                        
                        // Check if we got fewer results than requested (indicates last page)
                        hasMore = results.Count() >= perPage;
                    }
                    else
                    {
                        hasMore = false;
                    }
                }

                return allResults;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error fetching {EntityName}: {ex.Message}");
                if (ErrorObject != null)
                    ErrorObject.Flag = Errors.Failed;
                return new List<object>();
            }
        }

        // Build query parameters for Freshsales API
        private Dictionary<string, string> BuildFreshsalesQuery(List<AppFilter> filters)
        {
            var queryParams = new Dictionary<string, string>();

            if (filters?.Any() != true) return queryParams;

            // Handle basic filters - Freshsales supports various filter formats
            foreach (var filter in filters)
            {
                if (string.IsNullOrWhiteSpace(filter.FieldName) || filter.FilterValue == null)
                    continue;

                // Map common filter operations to Freshsales format
                switch (filter.Operator?.ToLowerInvariant())
                {
                    case "=":
                    case "eq":
                        queryParams[$"filter[{filter.FieldName}]"] = filter.FilterValue.ToString();
                        break;
                    case "like":
                    case "contains":
                        // Freshsales search parameter
                        queryParams["q"] = filter.FilterValue.ToString();
                        break;
                    case ">":
                    case "gt":
                        queryParams[$"filter[{filter.FieldName}][gt]"] = filter.FilterValue.ToString();
                        break;
                    case "<":
                    case "lt":
                        queryParams[$"filter[{filter.FieldName}][lt]"] = filter.FilterValue.ToString();
                        break;
                    default:
                        // Default to equality
                        queryParams[$"filter[{filter.FieldName}]"] = filter.FilterValue.ToString();
                        break;
                }
            }

            return queryParams;
        }

        // Extract data from Freshsales JSON response
        private IEnumerable<object> ExtractFreshsalesData(string jsonContent)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jsonContent))
                    return Enumerable.Empty<object>();

                using var document = JsonDocument.Parse(jsonContent);
                var root = document.RootElement;

                // Freshsales typically returns data in: { "data": [...] }
                if (root.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Array)
                {
                    return ExtractArrayItems(dataElement);
                }

                // If root is an array
                if (root.ValueKind == JsonValueKind.Array)
                {
                    return ExtractArrayItems(root);
                }

                // If single object, wrap in array
                if (root.ValueKind == JsonValueKind.Object)
                {
                    var obj = JsonSerializer.Deserialize<object>(root.GetRawText());
                    return obj != null ? new[] { obj } : Enumerable.Empty<object>();
                }

                return Enumerable.Empty<object>();
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error parsing Freshsales response: {ex.Message}");
                return Enumerable.Empty<object>();
            }
        }

        // Helper method to extract items from JSON array
        private IEnumerable<object> ExtractArrayItems(JsonElement arrayElement)
        {
            var results = new List<object>();
            
            foreach (var item in arrayElement.EnumerateArray())
            {
                try
                {
                    var obj = JsonSerializer.Deserialize<object>(item.GetRawText());
                    if (obj != null)
                        results.Add(obj);
                }
                catch (Exception ex)
                {
                    Logger?.WriteLog($"Error deserializing array item: {ex.Message}");
                }
            }

            return results;
        }
    }
}