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

namespace TheTechIdea.Beep.Connectors.Communication.Zoom
{
    using Models;
    /// <summary>
    /// Zoom API data source built on WebAPIDataSource.
    /// Configure WebAPIConnectionProperties.Url to Zoom API base URL and set Authorization header.
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zoom)]
    public class ZoomDataSource : WebAPIDataSource
    {
        // Supported Zoom entities -> Zoom endpoint + result root + required filters
        private static readonly Dictionary<string, (string endpoint, string? root, string[] requiredFilters)> Map
            = new(StringComparer.OrdinalIgnoreCase)
            {
                ["users"] = ("users", null, Array.Empty<string>()),
                ["meetings"] = ("users/{user_id}/meetings", null, new[] { "user_id" }),
                ["meeting"] = ("meetings/{meeting_id}", null, new[] { "meeting_id" }),
                ["meeting_participants"] = ("report/meetings/{meeting_id}/participants", null, new[] { "meeting_id" }),
                ["meeting_recordings"] = ("meetings/{meeting_id}/recordings", null, new[] { "meeting_id" }),
                ["webinars"] = ("users/{user_id}/webinars", null, new[] { "user_id" }),
                ["webinar"] = ("webinars/{webinar_id}", null, new[] { "webinar_id" }),
                ["webinar_participants"] = ("report/webinars/{webinar_id}/participants", null, new[] { "webinar_id" }),
                ["webinar_recordings"] = ("webinars/{webinar_id}/recordings", null, new[] { "webinar_id" }),
                ["groups"] = ("groups", null, Array.Empty<string>()),
                ["group_members"] = ("groups/{group_id}/members", null, new[] { "group_id" }),
                ["channels"] = ("chat/users/{user_id}/channels", null, new[] { "user_id" }),
                ["channel_messages"] = ("chat/channels/{channel_id}/messages", null, new[] { "channel_id" }),
                ["account_settings"] = ("accounts/{account_id}/settings", null, new[] { "account_id" }),
                ["user_settings"] = ("users/{user_id}/settings", null, new[] { "user_id" })
            };

        public ZoomDataSource(string datasourcename, IDMLogger logger, IDMEEditor dmeEditor, DataSourceType databasetype, IErrorsInfo errorObject)
            : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
        {
            // Ensure WebAPI props exist; caller configures Url/Auth outside this class.
            if (Dataconnection != null && Dataconnection.ConnectionProp is not WebAPIConnectionProperties)
                Dataconnection.ConnectionProp = new WebAPIConnectionProperties();

            EntitiesNames = Map.Keys.ToList();
            Entities = EntitiesNames
                .Select(n => new EntityStructure { EntityName = n, DatasourceEntityName = n })
                .ToList();
        }

        // Keep your interface exactly
        public new IEnumerable<string> GetEntitesList() => EntitiesNames;

        // ---------------- overrides (same signatures) ----------------

        // sync
        public override IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            var data = GetEntityAsync(EntityName, filter).ConfigureAwait(false).GetAwaiter().GetResult();
            return data ?? Array.Empty<object>();
        }

        // async
        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            if (!Map.TryGetValue(EntityName, out var m))
                throw new InvalidOperationException($"Unknown Zoom entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            // Replace path parameters in endpoint
            var endpoint = ReplacePathParameters(m.endpoint, q);

            using var resp = await GetAsync(endpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, m.root);
        }

        // paged
        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            if (!Map.TryGetValue(EntityName, out var m))
                throw new InvalidOperationException($"Unknown Zoom entity '{EntityName}'.");

            var q = FiltersToQuery(filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            // Zoom API supports pagination with page_size and next_page_token
            q["page_size"] = Math.Max(1, Math.Min(pageSize, 300)).ToString(); // Zoom max is 300

            if (pageNumber > 1 && filter.Any(f => f.FieldName == "next_page_token"))
            {
                // Keep existing pagination token
            }

            var endpoint = ReplacePathParameters(m.endpoint, q);

            var resp = GetAsync(endpoint, q).ConfigureAwait(false).GetAwaiter().GetResult();
            var items = ExtractArray(resp, m.root);

            // Basic pagination estimate
            int totalRecordsSoFar = (pageNumber - 1) * Math.Max(1, pageSize) + items.Count;

            return new PagedResult
            {
                Data = items,
                PageNumber = Math.Max(1, pageNumber),
                PageSize = pageSize,
                TotalRecords = totalRecordsSoFar,
                TotalPages = pageNumber, // Can't determine total pages without count
                HasPreviousPage = pageNumber > 1,
                HasNextPage = items.Count == pageSize // Assume more if we got full page
            };
        }

        // -------------------------- helpers --------------------------

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
            foreach (var req in required)
            {
                if (!q.ContainsKey(req) || string.IsNullOrWhiteSpace(q[req]))
                    throw new ArgumentException($"Zoom entity '{entity}' requires '{req}' parameter in filters.");
            }
        }

        private static string ReplacePathParameters(string endpoint, Dictionary<string, string> parameters)
        {
            var result = endpoint;
            foreach (var param in parameters)
            {
                result = result.Replace($"{{{param.Key}}}", param.Value);
            }
            return result;
        }

        private static List<object> ExtractArray(HttpResponseMessage resp, string? rootPath)
        {
            var list = new List<object>();
            if (resp == null) return list;

            var json = resp.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(json);

            JsonElement node = doc.RootElement;

            if (!string.IsNullOrWhiteSpace(rootPath))
            {
                foreach (var part in rootPath.Split('.'))
                {
                    if (node.ValueKind != JsonValueKind.Object || !node.TryGetProperty(part, out node))
                        return list; // path not found
                }
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
                // wrap single object
                var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(node.GetRawText(), opts);
                if (obj != null) list.Add(obj);
            }

            return list;
        }

        // -------------------------- Command Methods --------------------------

        [CommandAttribute(Name = "GetUsers", Caption = "Get Zoom Users",
            ObjectType = "ZoomUser", PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zoom,
            ClassType = "WebAPIDataSource", Showin = ShowinType.Both, Order = 1,
            iconimage = "zoom.png", misc = "Get list of users")]
        public async Task<IEnumerable<ZoomUser>> GetUsers(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("users", filters ?? new List<AppFilter>());
            return result.Cast<ZoomUser>().Select(u => u.Attach<ZoomUser>(this));
        }

        [CommandAttribute(Name = "GetMeetings", Caption = "Get Zoom Meetings",
            ObjectType = "ZoomMeeting", PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zoom,
            ClassType = "WebAPIDataSource", Showin = ShowinType.Both, Order = 2,
            iconimage = "zoom.png", misc = "Get meetings for a user (requires user_id)")]
        public async Task<IEnumerable<ZoomMeeting>> GetMeetings(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("meetings", filters ?? new List<AppFilter>());
            return result.Cast<ZoomMeeting>().Select(m => m.Attach<ZoomMeeting>(this));
        }

        [CommandAttribute(Name = "GetMeeting", Caption = "Get Zoom Meeting Details",
            ObjectType = "ZoomMeeting", PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zoom,
            ClassType = "WebAPIDataSource", Showin = ShowinType.Both, Order = 3,
            iconimage = "zoom.png", misc = "Get specific meeting details (requires meeting_id)")]
        public async Task<IEnumerable<ZoomMeeting>> GetMeeting(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("meeting", filters ?? new List<AppFilter>());
            return result.Cast<ZoomMeeting>().Select(m => m.Attach<ZoomMeeting>(this));
        }

        [CommandAttribute(Name = "GetMeetingParticipants", Caption = "Get Zoom Meeting Participants",
            ObjectType = "ZoomMeetingParticipant", PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zoom,
            ClassType = "WebAPIDataSource", Showin = ShowinType.Both, Order = 4,
            iconimage = "zoom.png", misc = "Get meeting participants report (requires meeting_id)")]
        public async Task<IEnumerable<ZoomMeetingParticipant>> GetMeetingParticipants(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("meeting_participants", filters ?? new List<AppFilter>());
            return result.Cast<ZoomMeetingParticipant>().Select(p => p.Attach<ZoomMeetingParticipant>(this));
        }

        [CommandAttribute(Name = "GetMeetingRecordings", Caption = "Get Zoom Meeting Recordings",
            ObjectType = "ZoomMeetingRecording", PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zoom,
            ClassType = "WebAPIDataSource", Showin = ShowinType.Both, Order = 5,
            iconimage = "zoom.png", misc = "Get meeting recordings (requires meeting_id)")]
        public async Task<IEnumerable<ZoomMeetingRecording>> GetMeetingRecordings(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("meeting_recordings", filters ?? new List<AppFilter>());
            return result.Cast<ZoomMeetingRecording>().Select(r => r.Attach<ZoomMeetingRecording>(this));
        }

        [CommandAttribute(Name = "GetWebinars", Caption = "Get Zoom Webinars",
            ObjectType = "ZoomWebinar", PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zoom,
            ClassType = "WebAPIDataSource", Showin = ShowinType.Both, Order = 6,
            iconimage = "zoom.png", misc = "Get webinars for a user (requires user_id)")]
        public async Task<IEnumerable<ZoomWebinar>> GetWebinars(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("webinars", filters ?? new List<AppFilter>());
            return result.Cast<ZoomWebinar>().Select(w => w.Attach<ZoomWebinar>(this));
        }

        [CommandAttribute(Name = "GetWebinar", Caption = "Get Zoom Webinar Details",
            ObjectType = "ZoomWebinar", PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zoom,
            ClassType = "WebAPIDataSource", Showin = ShowinType.Both, Order = 7,
            iconimage = "zoom.png", misc = "Get specific webinar details (requires webinar_id)")]
        public async Task<IEnumerable<ZoomWebinar>> GetWebinar(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("webinar", filters ?? new List<AppFilter>());
            return result.Cast<ZoomWebinar>().Select(w => w.Attach<ZoomWebinar>(this));
        }

        [CommandAttribute(Name = "GetWebinarParticipants", Caption = "Get Zoom Webinar Participants",
            ObjectType = "ZoomWebinarParticipant", PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zoom,
            ClassType = "WebAPIDataSource", Showin = ShowinType.Both, Order = 8,
            iconimage = "zoom.png", misc = "Get webinar participants report (requires webinar_id)")]
        public async Task<IEnumerable<ZoomWebinarParticipant>> GetWebinarParticipants(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("webinar_participants", filters ?? new List<AppFilter>());
            return result.Cast<ZoomWebinarParticipant>().Select(p => p.Attach<ZoomWebinarParticipant>(this));
        }

        [CommandAttribute(Name = "GetWebinarRecordings", Caption = "Get Zoom Webinar Recordings",
            ObjectType = "ZoomWebinarRecording", PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zoom,
            ClassType = "WebAPIDataSource", Showin = ShowinType.Both, Order = 9,
            iconimage = "zoom.png", misc = "Get webinar recordings (requires webinar_id)")]
        public async Task<IEnumerable<ZoomWebinarRecording>> GetWebinarRecordings(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("webinar_recordings", filters ?? new List<AppFilter>());
            return result.Cast<ZoomWebinarRecording>().Select(r => r.Attach<ZoomWebinarRecording>(this));
        }

        [CommandAttribute(Name = "GetGroups", Caption = "Get Zoom Groups",
            ObjectType = "ZoomGroup", PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zoom,
            ClassType = "WebAPIDataSource", Showin = ShowinType.Both, Order = 10,
            iconimage = "zoom.png", misc = "Get list of groups")]
        public async Task<IEnumerable<ZoomGroup>> GetGroups(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("groups", filters ?? new List<AppFilter>());
            return result.Cast<ZoomGroup>().Select(g => g.Attach<ZoomGroup>(this));
        }

        [CommandAttribute(Name = "GetGroupMembers", Caption = "Get Zoom Group Members",
            ObjectType = "ZoomGroupMember", PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zoom,
            ClassType = "WebAPIDataSource", Showin = ShowinType.Both, Order = 11,
            iconimage = "zoom.png", misc = "Get group members (requires group_id)")]
        public async Task<IEnumerable<ZoomGroupMember>> GetGroupMembers(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("group_members", filters ?? new List<AppFilter>());
            return result.Cast<ZoomGroupMember>().Select(m => m.Attach<ZoomGroupMember>(this));
        }

        [CommandAttribute(Name = "GetChannels", Caption = "Get Zoom Channels",
            ObjectType = "ZoomChannel", PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zoom,
            ClassType = "WebAPIDataSource", Showin = ShowinType.Both, Order = 12,
            iconimage = "zoom.png", misc = "Get chat channels for a user (requires user_id)")]
        public async Task<IEnumerable<ZoomChannel>> GetChannels(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("channels", filters ?? new List<AppFilter>());
            return result.Cast<ZoomChannel>().Select(c => c.Attach<ZoomChannel>(this));
        }

        [CommandAttribute(Name = "GetChannelMessages", Caption = "Get Zoom Channel Messages",
            ObjectType = "ZoomChannelMessage", PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zoom,
            ClassType = "WebAPIDataSource", Showin = ShowinType.Both, Order = 13,
            iconimage = "zoom.png", misc = "Get channel messages (requires channel_id)")]
        public async Task<IEnumerable<ZoomChannelMessage>> GetChannelMessages(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("channel_messages", filters ?? new List<AppFilter>());
            return result.Cast<ZoomChannelMessage>().Select(m => m.Attach<ZoomChannelMessage>(this));
        }

        [CommandAttribute(Name = "GetAccountSettings", Caption = "Get Zoom Account Settings",
            ObjectType = "ZoomAccountSettings", PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zoom,
            ClassType = "WebAPIDataSource", Showin = ShowinType.Both, Order = 14,
            iconimage = "zoom.png", misc = "Get account settings (requires account_id)")]
        public async Task<IEnumerable<ZoomAccountSettings>> GetAccountSettings(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("account_settings", filters ?? new List<AppFilter>());
            return result.Cast<ZoomAccountSettings>().Select(s => s.Attach<ZoomAccountSettings>(this));
        }

        [CommandAttribute(Name = "GetUserSettings", Caption = "Get Zoom User Settings",
            ObjectType = "ZoomUserSettings", PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zoom,
            ClassType = "WebAPIDataSource", Showin = ShowinType.Both, Order = 15,
            iconimage = "zoom.png", misc = "Get user settings (requires user_id)")]
        public async Task<IEnumerable<ZoomUserSettings>> GetUserSettings(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("user_settings", filters ?? new List<AppFilter>());
            return result.Cast<ZoomUserSettings>().Select(s => s.Attach<ZoomUserSettings>(this));
        }

        [CommandAttribute(
            Name = "CreateMeetingAsync",
            Caption = "Create Zoom Meeting",
            ObjectType = "ZoomMeeting",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Zoom,
            ClassType = "ZoomDataSource",
            Showin = ShowinType.Both,
            Order = 16,
            iconimage = "createmeeting.png",
            misc = "ReturnType: IEnumerable<ZoomMeeting>"
        )]
        public async Task<IEnumerable<ZoomMeeting>> CreateMeetingAsync(ZoomMeeting meeting)
        {
            try
            {
                var result = await PostAsync("users/{user_id}/meetings", meeting);
                var meetings = JsonSerializer.Deserialize<IEnumerable<ZoomMeeting>>(await result.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (meetings != null)
                {
                    foreach (var m in meetings)
                    {
                        m.Attach<ZoomMeeting>(this);
                    }
                }
                return meetings;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating meeting: {ex.Message}");
            }
            return new List<ZoomMeeting>();
        }

        [CommandAttribute(
            Name = "CreateWebinarAsync",
            Caption = "Create Zoom Webinar",
            ObjectType = "ZoomWebinar",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Zoom,
            ClassType = "ZoomDataSource",
            Showin = ShowinType.Both,
            Order = 17,
            iconimage = "createwebinar.png",
            misc = "ReturnType: IEnumerable<ZoomWebinar>"
        )]
        public async Task<IEnumerable<ZoomWebinar>> CreateWebinarAsync(ZoomWebinar webinar)
        {
            try
            {
                var result = await PostAsync("users/{user_id}/webinars", webinar);
                var webinars = JsonSerializer.Deserialize<IEnumerable<ZoomWebinar>>(await result.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (webinars != null)
                {
                    foreach (var w in webinars)
                    {
                        w.Attach<ZoomWebinar>(this);
                    }
                }
                return webinars;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating webinar: {ex.Message}");
            }
            return new List<ZoomWebinar>();
        }

        [CommandAttribute(
            Name = "CreateChannelAsync",
            Caption = "Create Zoom Channel",
            ObjectType = "ZoomChannel",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Zoom,
            ClassType = "ZoomDataSource",
            Showin = ShowinType.Both,
            Order = 18,
            iconimage = "createchannel.png",
            misc = "ReturnType: IEnumerable<ZoomChannel>"
        )]
        public async Task<IEnumerable<ZoomChannel>> CreateChannelAsync(ZoomChannel channel)
        {
            try
            {
                var result = await PostAsync("chat/users/{user_id}/channels", channel);
                var channels = JsonSerializer.Deserialize<IEnumerable<ZoomChannel>>(await result.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (channels != null)
                {
                    foreach (var c in channels)
                    {
                        c.Attach<ZoomChannel>(this);
                    }
                }
                return channels;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating channel: {ex.Message}");
            }
            return new List<ZoomChannel>();
        }

        [CommandAttribute(
            Name = "SendChannelMessageAsync",
            Caption = "Send Zoom Channel Message",
            ObjectType = "ZoomChannelMessage",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Zoom,
            ClassType = "ZoomDataSource",
            Showin = ShowinType.Both,
            Order = 19,
            iconimage = "sendmessage.png",
            misc = "ReturnType: IEnumerable<ZoomChannelMessage>"
        )]
        public async Task<IEnumerable<ZoomChannelMessage>> SendChannelMessageAsync(ZoomChannelMessage message)
        {
            try
            {
                var result = await PostAsync("chat/channels/{channel_id}/messages", message);
                var messages = JsonSerializer.Deserialize<IEnumerable<ZoomChannelMessage>>(await result.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (messages != null)
                {
                    foreach (var m in messages)
                    {
                        m.Attach<ZoomChannelMessage>(this);
                    }
                }
                return messages;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error sending channel message: {ex.Message}");
            }
            return new List<ZoomChannelMessage>();
        }

        [CommandAttribute(
            Name = "UpdateMeetingAsync",
            Caption = "Update Zoom Meeting",
            ObjectType = "ZoomMeeting",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Zoom,
            ClassType = "ZoomDataSource",
            Showin = ShowinType.Both,
            Order = 20,
            iconimage = "updatemeeting.png",
            misc = "ReturnType: IEnumerable<ZoomMeeting>"
        )]
        public async Task<IEnumerable<ZoomMeeting>> UpdateMeetingAsync(ZoomMeeting meeting)
        {
            try
            {
                var result = await PutAsync("meetings/{meeting_id}", meeting);
                var meetings = JsonSerializer.Deserialize<IEnumerable<ZoomMeeting>>(await result.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (meetings != null)
                {
                    foreach (var m in meetings)
                    {
                        m.Attach<ZoomMeeting>(this);
                    }
                }
                return meetings;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating meeting: {ex.Message}");
            }
            return new List<ZoomMeeting>();
        }

        [CommandAttribute(
            Name = "UpdateWebinarAsync",
            Caption = "Update Zoom Webinar",
            ObjectType = "ZoomWebinar",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Zoom,
            ClassType = "ZoomDataSource",
            Showin = ShowinType.Both,
            Order = 21,
            iconimage = "updatewebinar.png",
            misc = "ReturnType: IEnumerable<ZoomWebinar>"
        )]
        public async Task<IEnumerable<ZoomWebinar>> UpdateWebinarAsync(ZoomWebinar webinar)
        {
            try
            {
                var result = await PutAsync("webinars/{webinar_id}", webinar);
                var webinars = JsonSerializer.Deserialize<IEnumerable<ZoomWebinar>>(await result.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (webinars != null)
                {
                    foreach (var w in webinars)
                    {
                        w.Attach<ZoomWebinar>(this);
                    }
                }
                return webinars;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating webinar: {ex.Message}");
            }
            return new List<ZoomWebinar>();
        }
    }
}
