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
        /// <summary>
        /// Gets entity data asynchronously
        /// </summary>
        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            try
            {
                if (!KnownEntities.Contains(EntityName, StringComparer.OrdinalIgnoreCase))
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = $"Unknown Facebook entity: {EntityName}";
                    return Array.Empty<object>();
                }

                // Map entity name to appropriate method
                return EntityName.ToLower() switch
                {
                    "posts" => (await GetPosts(Filter?.FirstOrDefault(f => f.FieldName == "pageId")?.FilterValue?.ToString() ?? "", 
                        int.TryParse(Filter?.FirstOrDefault(f => f.FieldName == "limit")?.FilterValue?.ToString(), out var limit) ? limit : 10)).Cast<object>(),
                    "pages" => (await GetPageInfo(Filter?.FirstOrDefault(f => f.FieldName == "pageId")?.FilterValue?.ToString() ?? "")).Cast<object>(),
                    "users" => (await GetUserProfile(Filter?.FirstOrDefault(f => f.FieldName == "userId")?.FilterValue?.ToString() ?? "")).Cast<object>(),
                    "events" => (await GetEvents(Filter?.FirstOrDefault(f => f.FieldName == "pageId")?.FilterValue?.ToString() ?? "", 
                        int.TryParse(Filter?.FirstOrDefault(f => f.FieldName == "limit")?.FilterValue?.ToString(), out var eventLimit) ? eventLimit : 10)).Cast<object>(),
                    "photos" => (await GetPhotos(Filter?.FirstOrDefault(f => f.FieldName == "albumId")?.FilterValue?.ToString() ?? "", 
                        int.TryParse(Filter?.FirstOrDefault(f => f.FieldName == "limit")?.FilterValue?.ToString(), out var photoLimit) ? photoLimit : 10)).Cast<object>(),
                    _ => Array.Empty<object>()
                };
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                return Array.Empty<object>();
            }
        }

        // Paged
        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            var items = GetEntity(EntityName, filter).ToList();
            var totalRecords = items.Count;
            var pagedItems = items.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            return new PagedResult
            {
                Data = pagedItems,
                PageNumber = Math.Max(1, pageNumber),
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                HasPreviousPage = pageNumber > 1,
                HasNextPage = pageNumber * pageSize < totalRecords
            };
        }

        /// <summary>
        /// Gets posts from a Facebook page
        /// </summary>
        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Facebook, PointType = EnumPointType.Function, ObjectType = "FacebookPost", Name = "GetPosts", Caption = "Get Facebook Posts", ClassName = "FacebookDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<FacebookPost>")]
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
        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Facebook, PointType = EnumPointType.Function, ObjectType = "FacebookPage", Name = "GetPageInfo", Caption = "Get Facebook Page Info", ClassName = "FacebookDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<FacebookPage>")]
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
        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Facebook, PointType = EnumPointType.Function, ObjectType = "FacebookPost", Name = "GetGroupPosts", Caption = "Get Facebook Group Posts", ClassName = "FacebookDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<FacebookPost>")]
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
        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Facebook, PointType = EnumPointType.Function, ObjectType = "FacebookUser", Name = "GetUserProfile", Caption = "Get Facebook User Profile", ClassName = "FacebookDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<FacebookUser>")]
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
        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Facebook, PointType = EnumPointType.Function, ObjectType = "FacebookEvent", Name = "GetEvents", Caption = "Get Facebook Events", ClassName = "FacebookDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<FacebookEvent>")]
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
        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Facebook, PointType = EnumPointType.Function, ObjectType = "FacebookPicture", Name = "GetPhotos", Caption = "Get Facebook Photos", ClassName = "FacebookDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<FacebookPicture>")]
        public async Task<IEnumerable<FacebookPicture>> GetPhotos(string albumId, int limit = 10)
        {
            string fields = "id,name,source,images,height,width,created_time,updated_time,link,likes.summary(true),comments.summary(true)";
            string endpoint = $"{albumId}/photos?limit={limit}&fields={fields}";
            var response = await GetAsync(endpoint);
            string json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<FacebookResponse<FacebookPicture>>(json);
            return apiResponse?.Data ?? new List<FacebookPicture>();
        }

        // POST methods for creating entities
        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Facebook, PointType = EnumPointType.Function, ObjectType = "FacebookPost", Name = "CreatePost", Caption = "Create Facebook Post", ClassType = "FacebookDataSource", Showin = ShowinType.Both, Order = 10, iconimage = "facebook.png", misc = "ReturnType: IEnumerable<FacebookPost>")]
        public async Task<IEnumerable<FacebookPost>> CreatePostAsync(FacebookPost post)
        {
            try
            {
                var result = await PostAsync("me/feed", post);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdPost = JsonSerializer.Deserialize<FacebookPost>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<FacebookPost> { createdPost }.Select(p => p.Attach<FacebookPost>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating post: {ex.Message}");
            }
            return new List<FacebookPost>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Facebook, PointType = EnumPointType.Function, ObjectType = "FacebookEvent", Name = "CreateEvent", Caption = "Create Facebook Event", ClassType = "FacebookDataSource", Showin = ShowinType.Both, Order = 11, iconimage = "facebook.png", misc = "ReturnType: IEnumerable<FacebookEvent>")]
        public async Task<IEnumerable<FacebookEvent>> CreateEventAsync(FacebookEvent fbEvent)
        {
            try
            {
                var result = await PostAsync("me/events", fbEvent);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdEvent = JsonSerializer.Deserialize<FacebookEvent>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<FacebookEvent> { createdEvent }.Select(e => e.Attach<FacebookEvent>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating event: {ex.Message}");
            }
            return new List<FacebookEvent>();
        }

        // PUT methods for updating entities
        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Facebook, PointType = EnumPointType.Function, ObjectType = "FacebookPost", Name = "UpdatePost", Caption = "Update Facebook Post", ClassType = "FacebookDataSource", Showin = ShowinType.Both, Order = 12, iconimage = "facebook.png", misc = "ReturnType: IEnumerable<FacebookPost>")]
        public async Task<IEnumerable<FacebookPost>> UpdatePostAsync(FacebookPost post)
        {
            try
            {
                var result = await PutAsync($"{post.Id}", post);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedPost = JsonSerializer.Deserialize<FacebookPost>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<FacebookPost> { updatedPost }.Select(p => p.Attach<FacebookPost>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating post: {ex.Message}");
            }
            return new List<FacebookPost>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Facebook, PointType = EnumPointType.Function, ObjectType = "FacebookEvent", Name = "UpdateEvent", Caption = "Update Facebook Event", ClassType = "FacebookDataSource", Showin = ShowinType.Both, Order = 13, iconimage = "facebook.png", misc = "ReturnType: IEnumerable<FacebookEvent>")]
        public async Task<IEnumerable<FacebookEvent>> UpdateEventAsync(FacebookEvent fbEvent)
        {
            try
            {
                var result = await PutAsync($"{fbEvent.Id}", fbEvent);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedEvent = JsonSerializer.Deserialize<FacebookEvent>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<FacebookEvent> { updatedEvent }.Select(e => e.Attach<FacebookEvent>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating event: {ex.Message}");
            }
            return new List<FacebookEvent>();
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