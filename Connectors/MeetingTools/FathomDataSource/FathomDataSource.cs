using System.Text.Json;
using System.Data;
using TheTechIdea.Beep.Connectors.Fathom.Models;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.WebAPI;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Connectors.Fathom
{
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Fathom)]
    public class FathomDataSource : WebAPIDataSource
    {
        public FathomDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per) : base(datasourcename, logger, DMEEditor, databasetype, per)
        {
            InitializeFathomEntities();
        }

        private void InitializeFathomEntities()
        {
            // Videos entity
            EntityStructure videoEntity = CreateVideoEntity();
            Entities.Add(videoEntity);

            // Analytics entity
            EntityStructure analyticsEntity = CreateAnalyticsEntity();
            Entities.Add(analyticsEntity);

            // Insights entity
            EntityStructure insightsEntity = CreateInsightsEntity();
            Entities.Add(insightsEntity);

            // Chapters entity
            EntityStructure chaptersEntity = CreateChaptersEntity();
            Entities.Add(chaptersEntity);

            // Transcripts entity
            EntityStructure transcriptsEntity = CreateTranscriptsEntity();
            Entities.Add(transcriptsEntity);

            // Summaries entity
            EntityStructure summariesEntity = CreateSummariesEntity();
            Entities.Add(summariesEntity);

            // Comments entity
            EntityStructure commentsEntity = CreateCommentsEntity();
            Entities.Add(commentsEntity);

            // Shares entity
            EntityStructure sharesEntity = CreateSharesEntity();
            Entities.Add(sharesEntity);

            // Users entity
            EntityStructure usersEntity = CreateUsersEntity();
            Entities.Add(usersEntity);

            // Folders entity
            EntityStructure foldersEntity = CreateFoldersEntity();
            Entities.Add(foldersEntity);

            // Teams entity
            EntityStructure teamsEntity = CreateTeamsEntity();
            Entities.Add(teamsEntity);
        }

        private EntityStructure CreateVideoEntity()
        {
            EntityStructure entity = new EntityStructure();
            entity.EntityName = "videos";
            entity.Caption = "Videos";
            entity.DatasourceEntityName = "videos";
            entity.PrimaryKeys = new List<string> { "id" };

            // Add fields
            entity.Fields.Add(CreateField("id", "string", "Id", true));
            entity.Fields.Add(CreateField("title", "string", "Title"));
            entity.Fields.Add(CreateField("description", "string", "Description"));
            entity.Fields.Add(CreateField("duration", "int", "Duration"));
            entity.Fields.Add(CreateField("status", "string", "Status"));
            entity.Fields.Add(CreateField("created_at", "datetime", "Created At"));
            entity.Fields.Add(CreateField("updated_at", "datetime", "Updated At"));
            entity.Fields.Add(CreateField("thumbnail_url", "string", "Thumbnail URL"));
            entity.Fields.Add(CreateField("video_url", "string", "Video URL"));
            entity.Fields.Add(CreateField("download_url", "string", "Download URL"));
            entity.Fields.Add(CreateField("size", "long", "Size"));
            entity.Fields.Add(CreateField("format", "string", "Format"));
            entity.Fields.Add(CreateField("resolution", "string", "Resolution"));
            entity.Fields.Add(CreateField("frame_rate", "decimal", "Frame Rate"));
            entity.Fields.Add(CreateField("bitrate", "int", "Bitrate"));
            entity.Fields.Add(CreateField("tags", "string", "Tags"));

            return entity;
        }

        private EntityStructure CreateAnalyticsEntity()
        {
            EntityStructure entity = new EntityStructure();
            entity.EntityName = "analytics";
            entity.Caption = "Analytics";
            entity.DatasourceEntityName = "analytics";
            entity.PrimaryKeys = new List<string> { "video_id" };

            // Add fields
            entity.Fields.Add(CreateField("video_id", "string", "Video ID", true));
            entity.Fields.Add(CreateField("views", "int", "Views"));
            entity.Fields.Add(CreateField("unique_viewers", "int", "Unique Viewers"));
            entity.Fields.Add(CreateField("total_watch_time", "int", "Total Watch Time"));
            entity.Fields.Add(CreateField("average_watch_time", "decimal", "Average Watch Time"));
            entity.Fields.Add(CreateField("completion_rate", "decimal", "Completion Rate"));
            entity.Fields.Add(CreateField("engagement_score", "decimal", "Engagement Score"));
            entity.Fields.Add(CreateField("peak_concurrent_viewers", "int", "Peak Concurrent Viewers"));

            return entity;
        }

        private EntityStructure CreateInsightsEntity()
        {
            EntityStructure entity = new EntityStructure();
            entity.EntityName = "insights";
            entity.Caption = "Insights";
            entity.DatasourceEntityName = "insights";
            entity.PrimaryKeys = new List<string> { "id" };

            // Add fields
            entity.Fields.Add(CreateField("id", "string", "Id", true));
            entity.Fields.Add(CreateField("video_id", "string", "Video ID"));
            entity.Fields.Add(CreateField("type", "string", "Type"));
            entity.Fields.Add(CreateField("title", "string", "Title"));
            entity.Fields.Add(CreateField("description", "string", "Description"));
            entity.Fields.Add(CreateField("timestamp", "int", "Timestamp"));
            entity.Fields.Add(CreateField("duration", "int", "Duration"));
            entity.Fields.Add(CreateField("confidence", "decimal", "Confidence"));
            entity.Fields.Add(CreateField("thumbnail_url", "string", "Thumbnail URL"));
            entity.Fields.Add(CreateField("created_at", "datetime", "Created At"));

            return entity;
        }

        private EntityStructure CreateChaptersEntity()
        {
            EntityStructure entity = new EntityStructure();
            entity.EntityName = "chapters";
            entity.Caption = "Chapters";
            entity.DatasourceEntityName = "chapters";
            entity.PrimaryKeys = new List<string> { "id" };

            // Add fields
            entity.Fields.Add(CreateField("id", "string", "Id", true));
            entity.Fields.Add(CreateField("video_id", "string", "Video ID"));
            entity.Fields.Add(CreateField("title", "string", "Title"));
            entity.Fields.Add(CreateField("description", "string", "Description"));
            entity.Fields.Add(CreateField("start_time", "int", "Start Time"));
            entity.Fields.Add(CreateField("end_time", "int", "End Time"));
            entity.Fields.Add(CreateField("duration", "int", "Duration"));
            entity.Fields.Add(CreateField("thumbnail_url", "string", "Thumbnail URL"));
            entity.Fields.Add(CreateField("summary", "string", "Summary"));
            entity.Fields.Add(CreateField("created_at", "datetime", "Created At"));
            entity.Fields.Add(CreateField("updated_at", "datetime", "Updated At"));

            return entity;
        }

        private EntityStructure CreateTranscriptsEntity()
        {
            EntityStructure entity = new EntityStructure();
            entity.EntityName = "transcripts";
            entity.Caption = "Transcripts";
            entity.DatasourceEntityName = "transcripts";
            entity.PrimaryKeys = new List<string> { "id" };

            // Add fields
            entity.Fields.Add(CreateField("id", "string", "Id", true));
            entity.Fields.Add(CreateField("video_id", "string", "Video ID"));
            entity.Fields.Add(CreateField("language", "string", "Language"));
            entity.Fields.Add(CreateField("status", "string", "Status"));
            entity.Fields.Add(CreateField("content", "string", "Content"));
            entity.Fields.Add(CreateField("created_at", "datetime", "Created At"));
            entity.Fields.Add(CreateField("updated_at", "datetime", "Updated At"));

            return entity;
        }

        private EntityStructure CreateSummariesEntity()
        {
            EntityStructure entity = new EntityStructure();
            entity.EntityName = "summaries";
            entity.Caption = "Summaries";
            entity.DatasourceEntityName = "summaries";
            entity.PrimaryKeys = new List<string> { "id" };

            // Add fields
            entity.Fields.Add(CreateField("id", "string", "Id", true));
            entity.Fields.Add(CreateField("video_id", "string", "Video ID"));
            entity.Fields.Add(CreateField("type", "string", "Type"));
            entity.Fields.Add(CreateField("title", "string", "Title"));
            entity.Fields.Add(CreateField("content", "string", "Content"));
            entity.Fields.Add(CreateField("sentiment", "string", "Sentiment"));
            entity.Fields.Add(CreateField("confidence", "decimal", "Confidence"));
            entity.Fields.Add(CreateField("created_at", "datetime", "Created At"));
            entity.Fields.Add(CreateField("updated_at", "datetime", "Updated At"));

            return entity;
        }

        private EntityStructure CreateCommentsEntity()
        {
            EntityStructure entity = new EntityStructure();
            entity.EntityName = "comments";
            entity.Caption = "Comments";
            entity.DatasourceEntityName = "comments";
            entity.PrimaryKeys = new List<string> { "id" };

            // Add fields
            entity.Fields.Add(CreateField("id", "string", "Id", true));
            entity.Fields.Add(CreateField("video_id", "string", "Video ID"));
            entity.Fields.Add(CreateField("user_id", "string", "User ID"));
            entity.Fields.Add(CreateField("content", "string", "Content"));
            entity.Fields.Add(CreateField("timestamp", "int", "Timestamp"));
            entity.Fields.Add(CreateField("parent_id", "string", "Parent ID"));
            entity.Fields.Add(CreateField("likes", "int", "Likes"));
            entity.Fields.Add(CreateField("is_resolved", "bool", "Is Resolved"));
            entity.Fields.Add(CreateField("created_at", "datetime", "Created At"));
            entity.Fields.Add(CreateField("updated_at", "datetime", "Updated At"));

            return entity;
        }

        private EntityStructure CreateSharesEntity()
        {
            EntityStructure entity = new EntityStructure();
            entity.EntityName = "shares";
            entity.Caption = "Shares";
            entity.DatasourceEntityName = "shares";
            entity.PrimaryKeys = new List<string> { "id" };

            // Add fields
            entity.Fields.Add(CreateField("id", "string", "Id", true));
            entity.Fields.Add(CreateField("video_id", "string", "Video ID"));
            entity.Fields.Add(CreateField("share_type", "string", "Share Type"));
            entity.Fields.Add(CreateField("recipient_email", "string", "Recipient Email"));
            entity.Fields.Add(CreateField("recipient_name", "string", "Recipient Name"));
            entity.Fields.Add(CreateField("expires_at", "datetime", "Expires At"));
            entity.Fields.Add(CreateField("view_count", "int", "View Count"));
            entity.Fields.Add(CreateField("last_viewed_at", "datetime", "Last Viewed At"));
            entity.Fields.Add(CreateField("created_at", "datetime", "Created At"));

            return entity;
        }

        private EntityStructure CreateUsersEntity()
        {
            EntityStructure entity = new EntityStructure();
            entity.EntityName = "users";
            entity.Caption = "Users";
            entity.DatasourceEntityName = "users";
            entity.PrimaryKeys = new List<string> { "id" };

            // Add fields
            entity.Fields.Add(CreateField("id", "string", "Id", true));
            entity.Fields.Add(CreateField("email", "string", "Email"));
            entity.Fields.Add(CreateField("first_name", "string", "First Name"));
            entity.Fields.Add(CreateField("last_name", "string", "Last Name"));
            entity.Fields.Add(CreateField("avatar_url", "string", "Avatar URL"));
            entity.Fields.Add(CreateField("role", "string", "Role"));
            entity.Fields.Add(CreateField("is_active", "bool", "Is Active"));
            entity.Fields.Add(CreateField("last_login", "datetime", "Last Login"));

            return entity;
        }

        private EntityStructure CreateFoldersEntity()
        {
            EntityStructure entity = new EntityStructure();
            entity.EntityName = "folders";
            entity.Caption = "Folders";
            entity.DatasourceEntityName = "folders";
            entity.PrimaryKeys = new List<string> { "id" };

            // Add fields
            entity.Fields.Add(CreateField("id", "string", "Id", true));
            entity.Fields.Add(CreateField("name", "string", "Name"));
            entity.Fields.Add(CreateField("description", "string", "Description"));
            entity.Fields.Add(CreateField("parent_id", "string", "Parent ID"));
            entity.Fields.Add(CreateField("created_at", "datetime", "Created At"));
            entity.Fields.Add(CreateField("updated_at", "datetime", "Updated At"));

            return entity;
        }

        private EntityStructure CreateTeamsEntity()
        {
            EntityStructure entity = new EntityStructure();
            entity.EntityName = "teams";
            entity.Caption = "Teams";
            entity.DatasourceEntityName = "teams";
            entity.PrimaryKeys = new List<string> { "id" };

            // Add fields
            entity.Fields.Add(CreateField("id", "string", "Id", true));
            entity.Fields.Add(CreateField("name", "string", "Name"));
            entity.Fields.Add(CreateField("description", "string", "Description"));
            entity.Fields.Add(CreateField("created_at", "datetime", "Created At"));
            entity.Fields.Add(CreateField("updated_at", "datetime", "Updated At"));

            return entity;
        }

        private EntityField CreateField(string fieldname, string fieldtype, string caption, bool isprimary = false)
        {
            EntityField field = new EntityField();
            field.fieldname = fieldname;
            field.fieldtype = fieldtype;
            field.Caption = caption;
            field.IsPrimaryKey = isprimary;
            field.AllowDBNull = !isprimary;
            return field;
        }

        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> filter)
        {
            try
            {
                string endpoint = GetEndpointForEntity(EntityName);
                if (string.IsNullOrEmpty(endpoint))
                    return null;

                string url = $"{BaseUri.TrimEnd('/')}{endpoint}";
                string queryString = BuildQueryString(filter);
                if (!string.IsNullOrEmpty(queryString))
                    url += "?" + queryString;

                var response = await GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    return ParseEntityResponse(EntityName, content);
                }
                else
                {
                    DMEEditor.AddLogMessage("Fathom", $"Failed to get {EntityName}: {response.StatusCode}", DateTime.Now, -1, null, Errors.Failed);
                    return null;
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fathom", $"Error getting {EntityName}: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return null;
            }
        }

        private string GetEndpointForEntity(string entityName)
        {
            return entityName.ToLower() switch
            {
                "videos" => "/videos",
                "analytics" => "/analytics",
                "insights" => "/insights",
                "chapters" => "/chapters",
                "transcripts" => "/transcripts",
                "summaries" => "/summaries",
                "comments" => "/comments",
                "shares" => "/shares",
                "users" => "/users",
                "folders" => "/folders",
                "teams" => "/teams",
                _ => null
            };
        }

        private object ParseEntityResponse(string entityName, string content)
        {
            try
            {
                return entityName.ToLower() switch
                {
                    "videos" => JsonSerializer.Deserialize<FathomPaginationResponse<FathomVideo>>(content, JsonOptions),
                    "analytics" => JsonSerializer.Deserialize<FathomPaginationResponse<FathomAnalytics>>(content, JsonOptions),
                    "insights" => JsonSerializer.Deserialize<FathomPaginationResponse<FathomInsight>>(content, JsonOptions),
                    "chapters" => JsonSerializer.Deserialize<FathomPaginationResponse<FathomChapter>>(content, JsonOptions),
                    "transcripts" => JsonSerializer.Deserialize<FathomPaginationResponse<FathomTranscript>>(content, JsonOptions),
                    "summaries" => JsonSerializer.Deserialize<FathomPaginationResponse<FathomSummary>>(content, JsonOptions),
                    "comments" => JsonSerializer.Deserialize<FathomPaginationResponse<FathomComment>>(content, JsonOptions),
                    "shares" => JsonSerializer.Deserialize<FathomPaginationResponse<FathomShare>>(content, JsonOptions),
                    "users" => JsonSerializer.Deserialize<FathomPaginationResponse<FathomUser>>(content, JsonOptions),
                    "folders" => JsonSerializer.Deserialize<FathomPaginationResponse<FathomFolder>>(content, JsonOptions),
                    "teams" => JsonSerializer.Deserialize<FathomPaginationResponse<FathomTeam>>(content, JsonOptions),
                    _ => null
                };
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fathom", $"Error parsing {entityName} response: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return null;
            }
        }

        private string BuildQueryString(List<AppFilter> filters)
        {
            if (filters == null || filters.Count == 0)
                return string.Empty;

            var queryParams = new List<string>();
            foreach (var filter in filters)
            {
                if (!string.IsNullOrEmpty(filter.FieldName) && filter.FilterValue != null)
                {
                    string value = filter.FilterValue.ToString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        queryParams.Add($"{filter.FieldName}={Uri.EscapeDataString(value)}");
                    }
                }
            }

            return string.Join("&", queryParams);
        }
    }
}