using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.WebAPI;
using TheTechIdea.Beep.Connectors.Marketing.GoogleAdsDataSource.Models;

namespace TheTechIdea.Beep.Connectors.Marketing.GoogleAds
{
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.GoogleAds)]
    public class GoogleAdsDataSource : WebAPIDataSource
    {
        // Google Ads API Map with endpoints, roots, and required filters
        public static readonly Dictionary<string, (string endpoint, string? root, string[] requiredFilters)> Map = new()
        {
            // Customer (Account) level
            ["customers"] = ("v14/customers", "customers", new[] { "" }),
            ["customer"] = ("v14/customers/{customer_id}", "", new[] { "customer_id" }),

            // Campaigns
            ["campaigns"] = ("v14/customers/{customer_id}/campaigns", "campaigns", new[] { "customer_id" }),
            ["campaign"] = ("v14/customers/{customer_id}/campaigns/{campaign_id}", "", new[] { "customer_id", "campaign_id" }),

            // Ad Groups
            ["ad_groups"] = ("v14/customers/{customer_id}/adGroups", "adGroups", new[] { "customer_id" }),
            ["ad_group"] = ("v14/customers/{customer_id}/adGroups/{ad_group_id}", "", new[] { "customer_id", "ad_group_id" }),

            // Ads
            ["ads"] = ("v14/customers/{customer_id}/ads", "ads", new[] { "customer_id" }),
            ["ad"] = ("v14/customers/{customer_id}/ads/{ad_id}", "", new[] { "customer_id", "ad_id" }),

            // Keywords
            ["keywords"] = ("v14/customers/{customer_id}/keywords", "keywords", new[] { "customer_id" }),
            ["keyword"] = ("v14/customers/{customer_id}/keywords/{keyword_id}", "", new[] { "customer_id", "keyword_id" }),

            // Ad Group Criteria
            ["ad_group_criteria"] = ("v14/customers/{customer_id}/adGroupCriteria", "adGroupCriteria", new[] { "customer_id" }),

            // Campaign Criteria
            ["campaign_criteria"] = ("v14/customers/{customer_id}/campaignCriteria", "campaignCriteria", new[] { "customer_id" }),

            // Budgets
            ["campaign_budgets"] = ("v14/customers/{customer_id}/campaignBudgets", "campaignBudgets", new[] { "customer_id" }),

            // Conversion Actions
            ["conversion_actions"] = ("v14/customers/{customer_id}/conversionActions", "conversionActions", new[] { "customer_id" }),

            // Reports
            ["campaign_performance"] = ("v14/customers/{customer_id}/googleAds:search", "results", new[] { "customer_id" }),
            ["ad_group_performance"] = ("v14/customers/{customer_id}/googleAds:search", "results", new[] { "customer_id" }),
            ["ad_performance"] = ("v14/customers/{customer_id}/googleAds:search", "results", new[] { "customer_id" }),
            ["keyword_performance"] = ("v14/customers/{customer_id}/googleAds:search", "results", new[] { "customer_id" }),

            // Geographic
            ["geo_targets"] = ("v14/geoTargetConstants", "geoTargetConstants", new[] { "" }),

            // Language
            ["language_constants"] = ("v14/languageConstants", "languageConstants", new[] { "" }),

            // Account Performance
            ["account_performance"] = ("v14/customers/{customer_id}/googleAds:search", "results", new[] { "customer_id" }),

            // Change History
            ["change_history"] = ("v14/customers/{customer_id}/googleAds:search", "results", new[] { "customer_id" }),

            // Assets
            ["assets"] = ("v14/customers/{customer_id}/assets", "assets", new[] { "customer_id" }),

            // Asset Groups
            ["asset_groups"] = ("v14/customers/{customer_id}/assetGroups", "assetGroups", new[] { "customer_id" }),

            // Labels
            ["labels"] = ("v14/customers/{customer_id}/labels", "labels", new[] { "customer_id" }),

            // Topics
            ["topics"] = ("v14/topicConstants", "topicConstants", new[] { "" }),

            // User Lists (Audiences)
            ["user_lists"] = ("v14/customers/{customer_id}/userLists", "userLists", new[] { "customer_id" }),

            // Remarketing Actions
            ["remarketing_actions"] = ("v14/customers/{customer_id}/remarketingActions", "remarketingActions", new[] { "customer_id" }),

            // Feeds
            ["feeds"] = ("v14/customers/{customer_id}/feeds", "feeds", new[] { "customer_id" }),

            // Feed Items
            ["feed_items"] = ("v14/customers/{customer_id}/feedItems", "feedItems", new[] { "customer_id" })
        };

        public GoogleAdsDataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor dmeEditor,
            DataSourceType databasetype,
            IErrorsInfo errorObject)
            : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
        {
            // Ensure we're on WebAPI connection properties
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
            {
                if (Dataconnection != null)
                    Dataconnection.ConnectionProp = new WebAPIConnectionProperties();
            }

            // Register entities from the Map
            var entitiesNames = Map.Keys.ToList();
            Entities = entitiesNames
                .Select(n => new EntityStructure { EntityName = n, DatasourceEntityName = n })
                .ToList();
        }

        public override async Task<IEnumerable<object>> GetEntityAsync(string entityName, List<AppFilter>? filter)
        {
            if (!Map.TryGetValue(entityName, out var mapping))
            {
                throw new ArgumentException($"Entity '{entityName}' not found in Google Ads API map");
            }

            var (endpoint, root, requiredFilters) = mapping;

            // Validate required filters
            if (requiredFilters.Length > 0 && requiredFilters[0] != "" && (filter == null || !filter.Any()))
            {
                throw new ArgumentException($"Entity '{entityName}' requires filters: {string.Join(", ", requiredFilters)}");
            }

            // Replace placeholders in endpoint
            var finalEndpoint = endpoint;
            if (filter != null)
            {
                foreach (var f in filter)
                {
                    finalEndpoint = finalEndpoint.Replace($"{{{f.FieldName}}}", f.FilterValue?.ToString() ?? "");
                }
            }

            // Make the API call
            var response = await GetAsync(finalEndpoint);
            if (response == null)
            {
                return new List<object>();
            }

            // Extract the array from the response
            var result = ExtractArray(response, root);
            return result ?? new List<object>();
        }

        // Extracts array from response
        private static List<object> ExtractArray(HttpResponseMessage resp, string? root)
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

        // CommandAttribute methods for framework integration
        [CommandAttribute(ObjectType = typeof(GoogleAdsCustomer), PointType = EnumPointType.Function, Name = "GetCustomers", Caption = "Get Customers", ClassName = "GoogleAdsDataSource", misc = "GetCustomers")]
        public IEnumerable<GoogleAdsCustomer> GetCustomers()
        {
            return GetEntity("customers", null).Cast<GoogleAdsCustomer>();
        }

        [CommandAttribute(ObjectType = typeof(GoogleAdsCampaign), PointType = EnumPointType.Function, Name = "GetCampaigns", Caption = "Get Campaigns", ClassName = "GoogleAdsDataSource", misc = "GetCampaigns")]
        public IEnumerable<GoogleAdsCampaign> GetCampaigns(string customerId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "customer_id", FilterValue = customerId } };
            return GetEntity("campaigns", filters).Cast<GoogleAdsCampaign>();
        }

        [CommandAttribute(ObjectType = typeof(GoogleAdsAdGroup), PointType = EnumPointType.Function, Name = "GetAdGroups", Caption = "Get Ad Groups", ClassName = "GoogleAdsDataSource", misc = "GetAdGroups")]
        public IEnumerable<GoogleAdsAdGroup> GetAdGroups(string customerId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "customer_id", FilterValue = customerId } };
            return GetEntity("ad_groups", filters).Cast<GoogleAdsAdGroup>();
        }

        [CommandAttribute(ObjectType = typeof(GoogleAdsAd), PointType = EnumPointType.Function, Name = "GetAds", Caption = "Get Ads", ClassName = "GoogleAdsDataSource", misc = "GetAds")]
        public IEnumerable<GoogleAdsAd> GetAds(string customerId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "customer_id", FilterValue = customerId } };
            return GetEntity("ads", filters).Cast<GoogleAdsAd>();
        }

        [CommandAttribute(ObjectType = typeof(GoogleAdsKeyword), PointType = EnumPointType.Function, Name = "GetKeywords", Caption = "Get Keywords", ClassName = "GoogleAdsDataSource", misc = "GetKeywords")]
        public IEnumerable<GoogleAdsKeyword> GetKeywords(string customerId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "customer_id", FilterValue = customerId } };
            return GetEntity("keywords", filters).Cast<GoogleAdsKeyword>();
        }

        [CommandAttribute(ObjectType = typeof(GoogleAdsCampaignBudget), PointType = EnumPointType.Function, Name = "GetCampaignBudgets", Caption = "Get Campaign Budgets", ClassName = "GoogleAdsDataSource", misc = "GetCampaignBudgets")]
        public IEnumerable<GoogleAdsCampaignBudget> GetCampaignBudgets(string customerId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "customer_id", FilterValue = customerId } };
            return GetEntity("campaign_budgets", filters).Cast<GoogleAdsCampaignBudget>();
        }

        // POST/PUT methods for creating and updating entities
        [CommandAttribute(ObjectType = typeof(GoogleAdsCampaign), PointType = EnumPointType.Function, Name = "CreateCampaign", Caption = "Create Campaign", ClassName = "GoogleAdsDataSource", misc = "CreateCampaign")]
        public async Task<GoogleAdsCampaign> CreateCampaign(string customerId, GoogleAdsCampaign campaign)
        {
            var endpoint = $"v14/customers/{customerId}/campaigns";
            var response = await PostAsync<GoogleAdsCampaign>(endpoint, campaign);
            return response;
        }

        [CommandAttribute(ObjectType = typeof(GoogleAdsCampaign), PointType = EnumPointType.Function, Name = "UpdateCampaign", Caption = "Update Campaign", ClassName = "GoogleAdsDataSource", misc = "UpdateCampaign")]
        public async Task<GoogleAdsCampaign> UpdateCampaign(string customerId, string campaignId, GoogleAdsCampaign campaign)
        {
            var endpoint = $"v14/customers/{customerId}/campaigns/{campaignId}";
            var response = await PutAsync<GoogleAdsCampaign>(endpoint, campaign);
            return response;
        }

        [CommandAttribute(ObjectType = typeof(GoogleAdsAdGroup), PointType = EnumPointType.Function, Name = "CreateAdGroup", Caption = "Create Ad Group", ClassName = "GoogleAdsDataSource", misc = "CreateAdGroup")]
        public async Task<GoogleAdsAdGroup> CreateAdGroup(string customerId, GoogleAdsAdGroup adGroup)
        {
            var endpoint = $"v14/customers/{customerId}/adGroups";
            var response = await PostAsync<GoogleAdsAdGroup>(endpoint, adGroup);
            return response;
        }

        [CommandAttribute(ObjectType = typeof(GoogleAdsAdGroup), PointType = EnumPointType.Function, Name = "UpdateAdGroup", Caption = "Update Ad Group", ClassName = "GoogleAdsDataSource", misc = "UpdateAdGroup")]
        public async Task<GoogleAdsAdGroup> UpdateAdGroup(string customerId, string adGroupId, GoogleAdsAdGroup adGroup)
        {
            var endpoint = $"v14/customers/{customerId}/adGroups/{adGroupId}";
            var response = await PutAsync<GoogleAdsAdGroup>(endpoint, adGroup);
            return response;
        }

        [CommandAttribute(ObjectType = typeof(GoogleAdsAd), PointType = EnumPointType.Function, Name = "CreateAd", Caption = "Create Ad", ClassName = "GoogleAdsDataSource", misc = "CreateAd")]
        public async Task<GoogleAdsAd> CreateAd(string customerId, GoogleAdsAd ad)
        {
            var endpoint = $"v14/customers/{customerId}/ads";
            var response = await PostAsync<GoogleAdsAd>(endpoint, ad);
            return response;
        }

        [CommandAttribute(ObjectType = typeof(GoogleAdsAd), PointType = EnumPointType.Function, Name = "UpdateAd", Caption = "Update Ad", ClassName = "GoogleAdsDataSource", misc = "UpdateAd")]
        public async Task<GoogleAdsAd> UpdateAd(string customerId, string adId, GoogleAdsAd ad)
        {
            var endpoint = $"v14/customers/{customerId}/ads/{adId}";
            var response = await PutAsync<GoogleAdsAd>(endpoint, ad);
            return response;
        }
    }
}