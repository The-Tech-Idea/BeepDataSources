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

namespace TheTechIdea.Beep.TikTokAdsDataSource
{
    /// <summary>
    /// TikTok Ads data source implementation using WebAPIDataSource as base class
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.TikTokAds)]
    public class TikTokAdsDataSource : WebAPIDataSource
    {
        // -------- Fixed, known entities (TikTok Ads API v1.3) --------
        private static readonly List<string> KnownEntities = new()
        {
            // Campaigns
            "campaigns",              // GET /campaign/get/
            // Ad Groups
            "adgroups",               // GET /adgroup/get/
            // Ads
            "ads",                    // GET /ad/get/
            // Analytics
            "analytics",              // GET /report/integrated/get/
            // Advertisers
            "advertisers"             // GET /advertiser/get/
        };

        // entity -> (endpoint template, root path, required filter keys)
        // endpoint supports {id} substitution taken from filters.
        private static readonly Dictionary<string, (string endpoint, string root, string[] requiredFilters)> Map
            = new(StringComparer.OrdinalIgnoreCase)
            {
                ["campaigns"] = ("campaign/get/", "data.list", new string[] { }),
                ["adgroups"] = ("adgroup/get/", "data.list", new string[] { }),
                ["ads"] = ("ad/get/", "data.list", new string[] { }),
                ["analytics"] = ("report/integrated/get/", "data.list", new string[] { }),
                ["advertisers"] = ("advertiser/get/", "data.list", new string[] { })
            };

        public TikTokAdsDataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor dmeEditor,
            DataSourceType databasetype,
            IErrorsInfo errorObject)
            : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
        {
            // Ensure WebAPI connection props exist (URL/Auth configured outside this class)
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
                Dataconnection.ConnectionProp = new WebAPIConnectionProperties();

            // Register fixed entities
            EntitiesNames = KnownEntities.ToList();
            Entities = EntitiesNames
                .Select(n => new EntityStructure { EntityName = n, DatasourceEntityName = n })
                .ToList();
        }

        // Return the fixed list (use 'override' if base is virtual; otherwise this hides the base)
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
            if (!Map.TryGetValue(EntityName, out var m))
                throw new InvalidOperationException($"Unknown TikTok Ads entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            // Build the full URL
            var endpoint = m.endpoint;
            var fullUrl = $"{BaseURL}/open_api/v1.3/{endpoint}{q}";

            // Make the request
            var response = await GetAsync(fullUrl);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"TikTok Ads API request failed: {response.StatusCode}");

            var json = await response.Content.ReadAsStringAsync();

            // Parse based on entity type
            var result = EntityName switch
            {
                "campaigns" => ParseCampaigns(json),
                "adgroups" => ParseAdGroups(json),
                "ads" => ParseAds(json),
                "analytics" => ParseAnalytics(json),
                "advertisers" => ParseAdvertisers(json),
                _ => throw new InvalidOperationException($"Unsupported entity: {EntityName}")
            };

            return result;
        }

        // -------------------- Entity-specific parsers --------------------

        private IEnumerable<TikTokCampaign> ParseCampaigns(string json)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                var response = JsonSerializer.Deserialize<TikTokAdsCampaignsResponse>(json, options);
                return response?.Data?.List ?? Array.Empty<TikTokCampaign>();
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error parsing TikTok Ads campaigns: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return Array.Empty<TikTokCampaign>();
            }
        }

        private IEnumerable<TikTokAdGroup> ParseAdGroups(string json)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                var response = JsonSerializer.Deserialize<TikTokAdsAdGroupsResponse>(json, options);
                return response?.Data?.List ?? Array.Empty<TikTokAdGroup>();
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error parsing TikTok Ads ad groups: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return Array.Empty<TikTokAdGroup>();
            }
        }

        private IEnumerable<TikTokAd> ParseAds(string json)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                var response = JsonSerializer.Deserialize<TikTokAdsAdsResponse>(json, options);
                return response?.Data?.List ?? Array.Empty<TikTokAd>();
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error parsing TikTok Ads ads: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return Array.Empty<TikTokAd>();
            }
        }

        private IEnumerable<TikTokAnalytics> ParseAnalytics(string json)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                var response = JsonSerializer.Deserialize<TikTokAdsAnalyticsResponse>(json, options);
                return response?.Data?.List ?? Array.Empty<TikTokAnalytics>();
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error parsing TikTok Ads analytics: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return Array.Empty<TikTokAnalytics>();
            }
        }

        private IEnumerable<TikTokAdvertiser> ParseAdvertisers(string json)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                var response = JsonSerializer.Deserialize<TikTokAdsAdvertisersResponse>(json, options);
                return response?.Data?.List ?? Array.Empty<TikTokAdvertiser>();
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error parsing TikTok Ads advertisers: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return Array.Empty<TikTokAdvertiser>();
            }
        }

        // -------------------- Helper methods --------------------

        private string FiltersToQuery(List<AppFilter> filters)
        {
            if (filters == null || !filters.Any())
                return string.Empty;

            var queryParts = new List<string>();
            foreach (var f in filters)
            {
                if (f.FieldName != null && f.FieldValue != null)
                {
                    var value = Uri.EscapeDataString(f.FieldValue.ToString() ?? "");
                    queryParts.Add($"{f.FieldName}={value}");
                }
            }

            return queryParts.Any() ? "?" + string.Join("&", queryParts) : string.Empty;
        }

        private void RequireFilters(string entityName, string query, string[] required)
        {
            foreach (var req in required)
            {
                if (!query.Contains(req))
                    throw new InvalidOperationException($"Entity '{entityName}' requires filter '{req}'");
            }
        }

        // -------------------- Response classes for JSON parsing --------------------

        private class TikTokAdsResponseData<T>
        {
            [JsonPropertyName("list")]
            public List<T>? List { get; set; }
        }

        private class TikTokAdsResponse<T>
        {
            [JsonPropertyName("data")]
            public TikTokAdsResponseData<T>? Data { get; set; }

            [JsonPropertyName("code")]
            public int Code { get; set; }

            [JsonPropertyName("message")]
            public string? Message { get; set; }
        }

        private class TikTokAdsCampaignsResponse : TikTokAdsResponse<TikTokCampaign> { }
        private class TikTokAdsAdGroupsResponse : TikTokAdsResponse<TikTokAdGroup> { }
        private class TikTokAdsAdsResponse : TikTokAdsResponse<TikTokAd> { }
        private class TikTokAdsAnalyticsResponse : TikTokAdsResponse<TikTokAnalytics> { }
        private class TikTokAdsAdvertisersResponse : TikTokAdsResponse<TikTokAdvertiser> { }
    }
}
