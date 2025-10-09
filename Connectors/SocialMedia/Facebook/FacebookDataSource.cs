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

namespace TheTechIdea.Beep.FacebookDataSource
{
    /// <summary>
    /// Facebook data source implementation using WebAPIDataSource as base class
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Facebook)]
    public class FacebookDataSource : WebAPIDataSource
    {
        // Fixed, supported entities
        private static readonly List<string> KnownEntities = new()
        {
            "posts",
            "pages", 
            "users",
            "events",
            "photos"
        };

        /// <summary>
        /// Initializes a new instance of the FacebookDataSource class
        /// </summary>
        public FacebookDataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor dmeEditor,
            DataSourceType databasetype,
            IErrorsInfo errorObject)
            : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
        {
            // Ensure WebAPI props (Url/Auth) exist (configure outside this class)
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
                Dataconnection.ConnectionProp = new WebAPIConnectionProperties();

            // Register fixed entities
            EntitiesNames = KnownEntities.ToList();
            Entities = EntitiesNames
                .Select(n => new EntityStructure { EntityName = n, DatasourceEntityName = n })
                .ToList();
        }

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
        /// Gets posts from a Facebook page
        /// </summary>
        [CommandAttribute(ObjectType = "FacebookPost", PointType = EnumPointType.Function, Name = "GetPosts", Caption = "Get Facebook Posts", ClassName = "FacebookDataSource", misc = "ReturnType: IEnumerable<FacebookPost>")]
        public async Task<IEnumerable<FacebookPost>> GetPosts(string pageId, int limit = 10)
        {
            string fields = "id,message,story,created_time,updated_time,type,status_type,permalink_url,full_picture,picture,source,name,caption,description,link,likes.summary(true),comments.summary(true),shares.summary(true),reactions.summary(true)";
            string endpoint = $"{pageId}/posts?limit={limit}&fields={fields}";
            var response = await GetAsync(endpoint);
            string json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<FacebookResponse<FacebookPost>>(json);
            return apiResponse?.Data ?? new List<FacebookPost>();
        }

        /// <summary>
        /// Gets information about a Facebook page
        /// </summary>
        [CommandAttribute(ObjectType = "FacebookPage", PointType = EnumPointType.Function, Name = "GetPageInfo", Caption = "Get Facebook Page Info", ClassName = "FacebookDataSource", misc = "ReturnType: IEnumerable<FacebookPage>")]
        public async Task<IEnumerable<FacebookPage>> GetPageInfo(string pageId)
        {
            string fields = "id,name,category,description,website,phone,emails,location,hours,cover,picture,about,link,followers_count,fan_count";
            string endpoint = $"{pageId}?fields={fields}";
            var response = await GetAsync(endpoint);
            string json = await response.Content.ReadAsStringAsync();
            var page = JsonSerializer.Deserialize<FacebookPage>(json);
            return page != null ? new List<FacebookPage> { page } : new List<FacebookPage>();
        }

        /// <summary>
        /// Gets posts from a Facebook group
        /// </summary>
        [CommandAttribute(ObjectType = "FacebookPost", PointType = EnumPointType.Function, Name = "GetGroupPosts", Caption = "Get Facebook Group Posts", ClassName = "FacebookDataSource", misc = "ReturnType: IEnumerable<FacebookPost>")]
        public async Task<IEnumerable<FacebookPost>> GetGroupPosts(string groupId, int limit = 10)
        {
            string fields = "id,message,story,created_time,updated_time,type,status_type,permalink_url,full_picture,picture,source,name,caption,description,link,likes.summary(true),comments.summary(true),shares.summary(true),reactions.summary(true)";
            string endpoint = $"{groupId}/feed?limit={limit}&fields={fields}";
            var response = await GetAsync(endpoint);
            string json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<FacebookResponse<FacebookPost>>(json);
            return apiResponse?.Data ?? new List<FacebookPost>();
        }

        /// <summary>
        /// Gets user profile information
        /// </summary>
        [CommandAttribute(ObjectType = "FacebookUser", PointType = EnumPointType.Function, Name = "GetUserProfile", Caption = "Get Facebook User Profile", ClassName = "FacebookDataSource", misc = "ReturnType: IEnumerable<FacebookUser>")]
        public async Task<IEnumerable<FacebookUser>> GetUserProfile(string userId)
        {
            string fields = "id,name,first_name,last_name,email,birthday,gender,location,hometown,about,website,picture";
            string endpoint = $"{userId}?fields={fields}";
            var response = await GetAsync(endpoint);
            string json = await response.Content.ReadAsStringAsync();
            var user = JsonSerializer.Deserialize<FacebookUser>(json);
            return user != null ? new List<FacebookUser> { user } : new List<FacebookUser>();
        }

        /// <summary>
        /// Gets events from a Facebook page
        /// </summary>
        [CommandAttribute(ObjectType = "FacebookEvent", PointType = EnumPointType.Function, Name = "GetEvents", Caption = "Get Facebook Events", ClassName = "FacebookDataSource", misc = "ReturnType: IEnumerable<FacebookEvent>")]
        public async Task<IEnumerable<FacebookEvent>> GetEvents(string pageId, int limit = 10)
        {
            string fields = "id,name,description,start_time,end_time,place,attending_count,interested_count,maybe_count,noreply_count,cover,picture,type,category,ticket_uri";
            string endpoint = $"{pageId}/events?limit={limit}&fields={fields}";
            var response = await GetAsync(endpoint);
            string json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<FacebookResponse<FacebookEvent>>(json);
            return apiResponse?.Data ?? new List<FacebookEvent>();
        }

        /// <summary>
        /// Gets photos from a Facebook album or page
        /// </summary>
        [CommandAttribute(ObjectType = "FacebookPicture", PointType = EnumPointType.Function, Name = "GetPhotos", Caption = "Get Facebook Photos", ClassName = "FacebookDataSource", misc = "ReturnType: IEnumerable<FacebookPicture>")]
        public async Task<IEnumerable<FacebookPicture>> GetPhotos(string albumId, int limit = 10)
        {
            string fields = "id,name,source,images,height,width,created_time,updated_time,link,likes.summary(true),comments.summary(true)";
            string endpoint = $"{albumId}/photos?limit={limit}&fields={fields}";
            var response = await GetAsync(endpoint);
            string json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<FacebookResponse<FacebookPicture>>(json);
            return apiResponse?.Data ?? new List<FacebookPicture>();
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public new void Dispose()
        {
            base.Dispose();
        }
    }
}