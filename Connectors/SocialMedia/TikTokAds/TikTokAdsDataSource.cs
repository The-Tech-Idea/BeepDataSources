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

namespace TheTechIdea.Beep.TikTokAdsDataSource
{
    /// <summary>
    /// TikTok Ads data source implementation using WebAPIDataSource as base class
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.TikTokAds)]
    public class TikTokAdsDataSource : WebAPIDataSource
    {
        // Fixed, supported entities
        private static readonly List<string> KnownEntities = new()
        {
            "advertisers",
            "campaigns",
            "adgroups",
            "ads",
            "analytics"
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

        /// <summary>
        /// Gets entity data asynchronously
        /// </summary>
        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            // Implementation will go here
            await Task.CompletedTask;
            return new List<object>();
        }

        /// <summary>
        /// Gets advertisers from TikTok Ads
        /// </summary>
        [CommandAttribute(
            Name = "GetAdvertisers",
            Caption = "Get TikTok Advertisers",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.TikTokAds,
            PointType = EnumPointType.Function,
            ObjectType = "TikTokAdvertiser",
            ClassType = "TikTokAdsDataSource",
            Showin = ShowinType.Both,
            Order = 1,
            iconimage = "tiktokads.png",
            misc = "ReturnType: IEnumerable<TikTokAdvertiser>"
        )]
        public async Task<IEnumerable<TikTokAdvertiser>> GetAdvertisers()
        {
            var result = await GetEntityAsync("advertisers", new List<AppFilter>());
            return result.Cast<TikTokAdvertiser>();
        }

        /// <summary>
        /// Gets campaigns from TikTok Ads
        /// </summary>
        [CommandAttribute(
            Name = "GetCampaigns",
            Caption = "Get TikTok Campaigns",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.TikTokAds,
            PointType = EnumPointType.Function,
            ObjectType = "TikTokCampaign",
            ClassType = "TikTokAdsDataSource",
            Showin = ShowinType.Both,
            Order = 2,
            iconimage = "tiktokads.png",
            misc = "ReturnType: IEnumerable<TikTokCampaign>"
        )]
        public async Task<IEnumerable<TikTokCampaign>> GetCampaigns()
        {
            var result = await GetEntityAsync("campaigns", new List<AppFilter>());
            return result.Cast<TikTokCampaign>();
        }

        /// <summary>
        /// Gets ad groups from TikTok Ads
        /// </summary>
        [CommandAttribute(
            Name = "GetAdGroups",
            Caption = "Get TikTok Ad Groups",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.TikTokAds,
            PointType = EnumPointType.Function,
            ObjectType = "TikTokAdGroup",
            ClassType = "TikTokAdsDataSource",
            Showin = ShowinType.Both,
            Order = 3,
            iconimage = "tiktokads.png",
            misc = "ReturnType: IEnumerable<TikTokAdGroup>"
        )]
        public async Task<IEnumerable<TikTokAdGroup>> GetAdGroups()
        {
            var result = await GetEntityAsync("adgroups", new List<AppFilter>());
            return result.Cast<TikTokAdGroup>();
        }

        /// <summary>
        /// Gets ads from TikTok Ads
        /// </summary>
        [CommandAttribute(
            Name = "GetAds",
            Caption = "Get TikTok Ads",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.TikTokAds,
            PointType = EnumPointType.Function,
            ObjectType = "TikTokAd",
            ClassType = "TikTokAdsDataSource",
            Showin = ShowinType.Both,
            Order = 4,
            iconimage = "tiktokads.png",
            misc = "ReturnType: IEnumerable<TikTokAd>"
        )]
        public async Task<IEnumerable<TikTokAd>> GetAds()
        {
            var result = await GetEntityAsync("ads", new List<AppFilter>());
            return result.Cast<TikTokAd>();
        }

        /// <summary>
        /// Gets analytics from TikTok Ads
        /// </summary>
        [CommandAttribute(
            Name = "GetAnalytics",
            Caption = "Get TikTok Analytics",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.TikTokAds,
            PointType = EnumPointType.Function,
            ObjectType = "TikTokAnalytics",
            ClassType = "TikTokAdsDataSource",
            Showin = ShowinType.Both,
            Order = 5,
            iconimage = "tiktokads.png",
            misc = "ReturnType: IEnumerable<TikTokAnalytics>"
        )]
        public async Task<IEnumerable<TikTokAnalytics>> GetAnalytics()
        {
            var result = await GetEntityAsync("analytics", new List<AppFilter>());
            return result.Cast<TikTokAnalytics>();
        }

        // POST methods for creating entities
        [CommandAttribute(ObjectType = "TikTokCampaign", PointType = EnumPointType.Function, Name = "CreateCampaign", Caption = "Create TikTok Campaign", ClassName = "TikTokAdsDataSource", misc = "ReturnType: TikTokCampaign")]
        public async Task<TikTokCampaign> CreateCampaignAsync(TikTokCampaign campaign)
        {
            var response = await PostAsync("campaigns", campaign);
            if (response == null) return null;
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TikTokCampaign>(json);
        }

        [CommandAttribute(ObjectType = "TikTokCampaign", PointType = EnumPointType.Function, Name = "UpdateCampaign", Caption = "Update TikTok Campaign", ClassName = "TikTokAdsDataSource", misc = "ReturnType: TikTokCampaign")]
        public async Task<TikTokCampaign> UpdateCampaignAsync(string campaignId, TikTokCampaign campaign)
        {
            var response = await PutAsync($"campaigns/{campaignId}", campaign);
            if (response == null) return null;
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TikTokCampaign>(json);
        }
    }
}
