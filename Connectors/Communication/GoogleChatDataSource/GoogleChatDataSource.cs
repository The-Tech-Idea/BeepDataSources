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
using TheTechIdea.Beep.Connectors.Communication.GoogleChat.Models;

namespace TheTechIdea.Beep.Connectors.Communication.GoogleChat
{
    /// <summary>
    /// Google Chat API data source built on WebAPIDataSource.
    /// Configure WebAPIConnectionProperties.Url to Google Chat API base URL and set Authorization header.
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.GoogleChat)]
    public class GoogleChatDataSource : WebAPIDataSource
    {
        // Supported Google Chat entities -> Google Chat endpoint + result root + required filters
        private static readonly Dictionary<string, (string endpoint, string? root, string[] requiredFilters)> Map
            = new(StringComparer.OrdinalIgnoreCase)
            {
                ["spaces"] = ("v1/spaces", null, Array.Empty<string>()),
                ["space"] = ("v1/spaces/{space_name}", null, new[] { "space_name" }),
                ["space_members"] = ("v1/spaces/{space_name}/members", null, new[] { "space_name" }),
                ["space_messages"] = ("v1/spaces/{space_name}/messages", null, new[] { "space_name" }),
                ["message"] = ("v1/spaces/{space_name}/messages/{message_name}", null, new[] { "space_name", "message_name" }),
                ["message_reactions"] = ("v1/spaces/{space_name}/messages/{message_name}/reactions", null, new[] { "space_name", "message_name" }),
                ["message_attachments"] = ("v1/spaces/{space_name}/messages/{message_name}/attachments", null, new[] { "space_name", "message_name" }),
                ["message_threads"] = ("v1/spaces/{space_name}/messages/{message_name}/thread", null, new[] { "space_name", "message_name" }),
                ["user_spaces"] = ("v1/spaces:search", null, Array.Empty<string>()),
                ["user_memberships"] = ("v1/users/{user_name}/memberships", null, new[] { "user_name" }),
                ["media_links"] = ("v1/media/{resource_name}/download", null, new[] { "resource_name" })
            };

        public GoogleChatDataSource(string datasourcename, IDMLogger logger, IDMEEditor dmeEditor, DataSourceType databasetype, IErrorsInfo errorObject)
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
                throw new InvalidOperationException($"Unknown Google Chat entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            // Replace path parameters in endpoint
            var endpoint = ReplacePathParameters(m.endpoint, q);

            using var resp = await GetAsync(endpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, m.root, EntityName);
        }

        // paged
        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            if (!Map.TryGetValue(EntityName, out var m))
                throw new InvalidOperationException($"Unknown Google Chat entity '{EntityName}'.");

            var q = FiltersToQuery(filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            // Google Chat API supports pagination with pageSize and pageToken
            q["pageSize"] = Math.Max(1, Math.Min(pageSize, 1000)).ToString(); // Google Chat max is 1000

            if (pageNumber > 1 && filter.Any(f => f.FieldName == "pageToken"))
            {
                // Keep existing pagination token
            }

            var endpoint = ReplacePathParameters(m.endpoint, q);

            var resp = GetAsync(endpoint, q).ConfigureAwait(false).GetAwaiter().GetResult();
            var items = ExtractArray(resp, m.root, EntityName);

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
                    throw new ArgumentException($"Google Chat entity '{entity}' requires '{req}' parameter in filters.");
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

        private static List<object> ExtractArray(HttpResponseMessage resp, string? rootPath, string entityName)
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
                    var obj = DeserializeEntity(el.GetRawText(), entityName, opts);
                    if (obj != null) list.Add(obj);
                }
            }
            else if (node.ValueKind == JsonValueKind.Object)
            {
                // wrap single object
                var obj = DeserializeEntity(node.GetRawText(), entityName, opts);
                if (obj != null) list.Add(obj);
            }

            return list;
        }

        private static object? DeserializeEntity(string json, string entityName, JsonSerializerOptions opts)
        {
            return entityName.ToLowerInvariant() switch
            {
                "spaces" or "space" => JsonSerializer.Deserialize<GoogleChatSpace>(json, opts),
                "space_members" => JsonSerializer.Deserialize<GoogleChatMember>(json, opts),
                "space_messages" or "message" => JsonSerializer.Deserialize<GoogleChatMessage>(json, opts),
                "message_reactions" => JsonSerializer.Deserialize<GoogleChatReaction>(json, opts),
                "message_attachments" => JsonSerializer.Deserialize<GoogleChatMessageAttachment>(json, opts),
                "message_threads" => JsonSerializer.Deserialize<GoogleChatMessageThread>(json, opts),
                "user_spaces" => JsonSerializer.Deserialize<GoogleChatUserSpace>(json, opts),
                "user_memberships" => JsonSerializer.Deserialize<GoogleChatUserMembership>(json, opts),
                "media_links" => JsonSerializer.Deserialize<GoogleChatMediaLink>(json, opts),
                _ => JsonSerializer.Deserialize<Dictionary<string, object>>(json, opts)
            };
        }

        #region CommandAttribute Methods

        /// <summary>
        /// Gets all spaces from Google Chat
        /// </summary>
        /// <returns>Enumerable of GoogleChatSpace objects</returns>
        [CommandAttribute(
            ObjectType ="GoogleChatSpace",
            PointType = EnumPointType.Function,
           Name = "GetSpaces",
            Caption = "Get Spaces",
            ClassName = "GoogleChatDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.GoogleChat,
            Showin = ShowinType.Both,
            Order = 1,
            iconimage = "googlechat.png",
            misc = "ReturnType: IEnumerable<GoogleChatSpace>"
        )]
        public IEnumerable<GoogleChatSpace> GetSpaces()
        {
            return GetEntityAsync("spaces", new List<AppFilter>())
                .ConfigureAwait(false).GetAwaiter().GetResult()
                .Cast<GoogleChatSpace>()
                .Select(x => x.Attach<GoogleChatSpace>(this));
        }

        /// <summary>
        /// Gets a specific space by name
        /// </summary>
        /// <param name="spaceName">The space name</param>
        /// <returns>GoogleChatSpace object</returns>
        [CommandAttribute(
            ObjectType ="GoogleChatSpace",
            PointType = EnumPointType.Function,
           Name = "GetSpace",
            Caption = "Get Space",
            ClassName = "GoogleChatDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.GoogleChat,
            Showin = ShowinType.Both,
            Order = 2,
            iconimage = "googlechat.png",
            misc = "ReturnType: IEnumerable<GoogleChatSpace>, Filter: space_name"
        )]
        public IEnumerable<GoogleChatSpace> GetSpace(string spaceName)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "space_name", FilterValue = spaceName } };
            return GetEntityAsync("space", filters)
                .ConfigureAwait(false).GetAwaiter().GetResult()
                .Cast<GoogleChatSpace>()
                .Select(x => x.Attach<GoogleChatSpace>(this));
        }

        /// <summary>
        /// Gets all messages for a space
        /// </summary>
        /// <param name="spaceName">The space name</param>
        /// <returns>Enumerable of GoogleChatMessage objects</returns>
        [CommandAttribute(
            ObjectType ="GoogleChatMessage",
            PointType = EnumPointType.Function,
           Name = "GetSpaceMessages",
            Caption = "Get Space Messages",
            ClassName = "GoogleChatDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.GoogleChat,
            Showin = ShowinType.Both,
            Order = 3,
            iconimage = "googlechat.png",
            misc = "ReturnType: IEnumerable<GoogleChatMessage>, Filter: space_name"
        )]
        public IEnumerable<GoogleChatMessage> GetSpaceMessages(string spaceName)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "space_name", FilterValue = spaceName } };
            return GetEntityAsync("space_messages", filters)
                .ConfigureAwait(false).GetAwaiter().GetResult()
                .Cast<GoogleChatMessage>()
                .Select(x => x.Attach<GoogleChatMessage>(this));
        }

        /// <summary>
        /// Gets a specific message by space and message name
        /// </summary>
        /// <param name="spaceName">The space name</param>
        /// <param name="messageName">The message name</param>
        /// <returns>GoogleChatMessage object</returns>
        [CommandAttribute(
            ObjectType ="GoogleChatMessage",
            PointType = EnumPointType.Function,
           Name = "GetMessage",
            Caption = "Get Message",
            ClassName = "GoogleChatDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.GoogleChat,
            Showin = ShowinType.Both,
            Order = 4,
            iconimage = "googlechat.png",
            misc = "ReturnType: IEnumerable<GoogleChatMessage>, Filter: space_name,message_name"
        )]
        public IEnumerable<GoogleChatMessage> GetMessage(string spaceName, string messageName)
        {
            var filters = new List<AppFilter>
            {
                new AppFilter { FieldName = "space_name", FilterValue = spaceName },
                new AppFilter { FieldName = "message_name", FilterValue = messageName }
            };
            return GetEntityAsync("message", filters)
                .ConfigureAwait(false).GetAwaiter().GetResult()
                .Cast<GoogleChatMessage>()
                .Select(x => x.Attach<GoogleChatMessage>(this));
        }

        /// <summary>
        /// Gets all members of a space
        /// </summary>
        /// <param name="spaceName">The space name</param>
        /// <returns>Enumerable of GoogleChatMember objects</returns>
        [CommandAttribute(
            ObjectType ="GoogleChatMember",
            PointType = EnumPointType.Function,
           Name = "GetSpaceMembers",
            Caption = "Get Space Members",
            ClassName = "GoogleChatDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.GoogleChat,
            Showin = ShowinType.Both,
            Order = 5,
            iconimage = "googlechat.png",
            misc = "ReturnType: IEnumerable<GoogleChatMember>, Filter: space_name"
        )]
        public IEnumerable<GoogleChatMember> GetSpaceMembers(string spaceName)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "space_name", FilterValue = spaceName } };
            return GetEntityAsync("space_members", filters)
                .ConfigureAwait(false).GetAwaiter().GetResult()
                .Cast<GoogleChatMember>()
                .Select(x => x.Attach<GoogleChatMember>(this));
        }

        /// <summary>
        /// Gets user spaces
        /// </summary>
        /// <returns>Enumerable of GoogleChatUserSpace objects</returns>
        [CommandAttribute(
            ObjectType ="GoogleChatUserSpace",
            PointType = EnumPointType.Function,
           Name = "GetUserSpaces",
            Caption = "Get User Spaces",
            ClassName = "GoogleChatDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.GoogleChat,
            Showin = ShowinType.Both,
            Order = 6,
            iconimage = "googlechat.png",
            misc = "ReturnType: IEnumerable<GoogleChatUserSpace>"
        )]
        public IEnumerable<GoogleChatUserSpace> GetUserSpaces()
        {
            return GetEntityAsync("user_spaces", new List<AppFilter>())
                .ConfigureAwait(false).GetAwaiter().GetResult()
                .Cast<GoogleChatUserSpace>()
                .Select(x => x.Attach<GoogleChatUserSpace>(this));
        }

        /// <summary>
        /// Gets user memberships
        /// </summary>
        /// <param name="userName">The user name</param>
        /// <returns>Enumerable of GoogleChatUserMembership objects</returns>
        [CommandAttribute(
            ObjectType ="GoogleChatUserMembership",
            PointType = EnumPointType.Function,
           Name = "GetUserMemberships",
            Caption = "Get User Memberships",
            ClassName = "GoogleChatDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.GoogleChat,
            Showin = ShowinType.Both,
            Order = 7,
            iconimage = "googlechat.png",
            misc = "ReturnType: IEnumerable<GoogleChatUserMembership>, Filter: user_name"
        )]
        public IEnumerable<GoogleChatUserMembership> GetUserMemberships(string userName)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "user_name", FilterValue = userName } };
            return GetEntityAsync("user_memberships", filters)
                .ConfigureAwait(false).GetAwaiter().GetResult()
                .Cast<GoogleChatUserMembership>()
                .Select(x => x.Attach<GoogleChatUserMembership>(this));
        }

        /// <summary>
        /// Creates a message in a Google Chat space
        /// </summary>
        [CommandAttribute(
           Name = "CreateMessageAsync",
            Caption = "Create Google Chat Message",
            ObjectType ="GoogleChatMessage",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.GoogleChat,
            ClassType ="GoogleChatDataSource",
            Showin = ShowinType.Both,
            Order = 8,
            iconimage = "createmessage.png",
            misc = "ReturnType: IEnumerable<GoogleChatMessage>"
        )]
        public async Task<IEnumerable<GoogleChatMessage>> CreateMessageAsync(GoogleChatMessage message)
        {
            try
            {
                var result = await PostAsync("v1/spaces/{space_name}/messages", message);
                var content = await result.Content.ReadAsStringAsync();
                var messages = JsonSerializer.Deserialize<IEnumerable<GoogleChatMessage>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (messages != null)
                {
                    foreach (var m in messages)
                    {
                        m.Attach<GoogleChatMessage>(this);
                    }
                }
                return messages;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating message: {ex.Message}");
            }
            return new List<GoogleChatMessage>();
        }

        /// <summary>
        /// Creates a space in Google Chat
        /// </summary>
        [CommandAttribute(
           Name = "CreateSpaceAsync",
            Caption = "Create Google Chat Space",
            ObjectType ="GoogleChatSpace",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.GoogleChat,
            ClassType ="GoogleChatDataSource",
            Showin = ShowinType.Both,
            Order = 9,
            iconimage = "createspace.png",
            misc = "ReturnType: IEnumerable<GoogleChatSpace>"
        )]
        public async Task<IEnumerable<GoogleChatSpace>> CreateSpaceAsync(GoogleChatSpace space)
        {
            try
            {
                var result = await PostAsync("v1/spaces", space);
                var content = await result.Content.ReadAsStringAsync();
                var spaces = JsonSerializer.Deserialize<IEnumerable<GoogleChatSpace>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (spaces != null)
                {
                    foreach (var s in spaces)
                    {
                        s.Attach<GoogleChatSpace>(this);
                    }
                }
                return spaces;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating space: {ex.Message}");
            }
            return new List<GoogleChatSpace>();
        }

        /// <summary>
        /// Updates a message in a Google Chat space
        /// </summary>
        [CommandAttribute(
           Name = "UpdateMessageAsync",
            Caption = "Update Google Chat Message",
            ObjectType ="GoogleChatMessage",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.GoogleChat,
            ClassType ="GoogleChatDataSource",
            Showin = ShowinType.Both,
            Order = 10,
            iconimage = "updatemessage.png",
            misc = "ReturnType: IEnumerable<GoogleChatMessage>"
        )]
        public async Task<IEnumerable<GoogleChatMessage>> UpdateMessageAsync(GoogleChatMessage message)
        {
            try
            {
                var result = await PutAsync("v1/spaces/{space_name}/messages/{message_name}", message);
                var content = await result.Content.ReadAsStringAsync();
                var messages = JsonSerializer.Deserialize<IEnumerable<GoogleChatMessage>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (messages != null)
                {
                    foreach (var m in messages)
                    {
                        m.Attach<GoogleChatMessage>(this);
                    }
                }
                return messages;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating message: {ex.Message}");
            }
            return new List<GoogleChatMessage>();
        }

        /// <summary>
        /// Updates a space in Google Chat
        /// </summary>
        [CommandAttribute(
           Name = "UpdateSpaceAsync",
            Caption = "Update Google Chat Space",
            ObjectType ="GoogleChatSpace",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.GoogleChat,
            ClassType ="GoogleChatDataSource",
            Showin = ShowinType.Both,
            Order = 11,
            iconimage = "updatespace.png",
            misc = "ReturnType: IEnumerable<GoogleChatSpace>"
        )]
        public async Task<IEnumerable<GoogleChatSpace>> UpdateSpaceAsync(GoogleChatSpace space)
        {
            try
            {
                var result = await PatchAsync("v1/spaces/{space_name}", space);
                var content = await result.Content.ReadAsStringAsync();
                var spaces = JsonSerializer.Deserialize<IEnumerable<GoogleChatSpace>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (spaces != null)
                {
                    foreach (var s in spaces)
                    {
                        s.Attach<GoogleChatSpace>(this);
                    }
                }
                return spaces;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating space: {ex.Message}");
            }
            return new List<GoogleChatSpace>();
        }

        #endregion
    }
}
