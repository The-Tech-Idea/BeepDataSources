using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
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
using TheTechIdea.Beep.Connectors.Gmail.Models;

namespace TheTechIdea.Beep.Connectors.Gmail
{
    /// <summary>
    /// Gmail data source implementation using WebAPIDataSource as base class
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Gmail)]
    public class GmailDataSource : WebAPIDataSource
    {
        // Entity endpoints mapping for Gmail API
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // Messages
            ["messages"] = "messages",
            ["messages.get"] = "messages/{id}",
            ["messages.list"] = "messages",
            ["messages.send"] = "messages/send",
            ["messages.trash"] = "messages/{id}/trash",
            ["messages.untrash"] = "messages/{id}/untrash",
            ["messages.delete"] = "messages/{id}",
            ["messages.modify"] = "messages/{id}/modify",
            // Threads
            ["threads"] = "threads",
            ["threads.get"] = "threads/{id}",
            ["threads.list"] = "threads",
            ["threads.trash"] = "threads/{id}/trash",
            ["threads.untrash"] = "threads/{id}/untrash",
            ["threads.delete"] = "threads/{id}",
            ["threads.modify"] = "threads/{id}/modify",
            // Labels
            ["labels"] = "labels",
            ["labels.get"] = "labels/{id}",
            ["labels.list"] = "labels",
            ["labels.create"] = "labels",
            ["labels.update"] = "labels/{id}",
            ["labels.delete"] = "labels/{id}",
            ["labels.patch"] = "labels/{id}",
            // Drafts
            ["drafts"] = "drafts",
            ["drafts.get"] = "drafts/{id}",
            ["drafts.list"] = "drafts",
            ["drafts.create"] = "drafts",
            ["drafts.update"] = "drafts/{id}",
            ["drafts.delete"] = "drafts/{id}",
            ["drafts.send"] = "drafts/send",
            // History
            ["history"] = "history",
            ["history.list"] = "history",
            // Profile
            ["profile"] = "profile"
        };

        // Required filters for each entity
        private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            ["messages.get"] = new[] { "id" },
            ["messages.trash"] = new[] { "id" },
            ["messages.untrash"] = new[] { "id" },
            ["messages.delete"] = new[] { "id" },
            ["messages.modify"] = new[] { "id" },
            ["threads.get"] = new[] { "id" },
            ["threads.trash"] = new[] { "id" },
            ["threads.untrash"] = new[] { "id" },
            ["threads.delete"] = new[] { "id" },
            ["threads.modify"] = new[] { "id" },
            ["labels.get"] = new[] { "id" },
            ["labels.update"] = new[] { "id" },
            ["labels.delete"] = new[] { "id" },
            ["labels.patch"] = new[] { "id" },
            ["drafts.get"] = new[] { "id" },
            ["drafts.update"] = new[] { "id" },
            ["drafts.delete"] = new[] { "id" }
        };

        public GmailDataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor dmeEditor,
            DataSourceType databasetype,
            IErrorsInfo errorObject)
            : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
        {
            // Ensure WebAPI connection props exist
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
            {
                if (Dataconnection != null)
                    Dataconnection.ConnectionProp = new WebAPIConnectionProperties();
            }

            // Register entities
            EntitiesNames = EntityEndpoints.Keys.ToList();
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
        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            if (!EntityEndpoints.TryGetValue(EntityName, out var endpoint))
                throw new InvalidOperationException($"Unknown Gmail entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            // Build the full URL
            var url = $"https://gmail.googleapis.com/gmail/v1/users/me/{endpoint}";
            url = ReplacePlaceholders(url, q);

            // Add query parameters
            var queryParams = BuildQueryParameters(q);
            if (!string.IsNullOrEmpty(queryParams))
                url += "?" + queryParams;

            // Make the request
            var response = await GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            // Parse based on entity
            return EntityName switch
            {
                "messages" or "messages.list" => ParseMessages(json),
                "messages.get" => ParseMessage(json),
                "threads" or "threads.list" => ParseThreads(json),
                "threads.get" => ParseThread(json),
                "labels" or "labels.list" => ParseLabels(json),
                "labels.get" => ParseLabel(json),
                "drafts" or "drafts.list" => ParseDrafts(json),
                "drafts.get" => ParseDraft(json),
                "history" or "history.list" => ParseHistory(json),
                "profile" => ParseProfile(json),
                _ => throw new NotSupportedException($"Entity '{EntityName}' parsing not implemented.")
            };
        }

        /// <summary>
        /// Gets Gmail messages
        /// </summary>
        [CommandAttribute(
            Name = "GetMessages",
            Caption = "Get Gmail Messages",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Gmail,
            PointType = EnumPointType.Function,
            ObjectType = "GmailMessage",
            ClassType = "GmailDataSource",
            Showin = ShowinType.Both,
            Order = 1,
            iconimage = "gmail.png",
            misc = "ReturnType: IEnumerable<GmailMessage>"
        )]
        public async Task<IEnumerable<GmailMessage>> GetMessages(int maxResults = 10, string? q = null, string? labelIds = null)
        {
            var filters = new List<AppFilter>
            {
                new AppFilter { FieldName = "maxResults", FilterValue = maxResults.ToString(), Operator = "=" }
            };
            if (!string.IsNullOrEmpty(q))
                filters.Add(new AppFilter { FieldName = "q", FilterValue = q, Operator = "=" });
            if (!string.IsNullOrEmpty(labelIds))
                filters.Add(new AppFilter { FieldName = "labelIds", FilterValue = labelIds, Operator = "=" });

            var result = await GetEntityAsync("messages.list", filters);
            return result.Cast<GmailMessage>();
        }

        /// <summary>
        /// Gets a specific Gmail message
        /// </summary>
        [CommandAttribute(
            Name = "GetMessage",
            Caption = "Get Gmail Message",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Gmail,
            PointType = EnumPointType.Function,
            ObjectType = "GmailMessage",
            ClassType = "GmailDataSource",
            Showin = ShowinType.Both,
            Order = 2,
            iconimage = "gmail.png",
            misc = "ReturnType: IEnumerable<GmailMessage>"
        )]
        public async Task<IEnumerable<GmailMessage>> GetMessage(string id)
        {
            var filters = new List<AppFilter>
            {
                new AppFilter { FieldName = "id", FilterValue = id, Operator = "=" }
            };

            var result = await GetEntityAsync("messages.get", filters);
            return result.Cast<GmailMessage>();
        }

        /// <summary>
        /// Gets Gmail threads
        /// </summary>
        [CommandAttribute(
            Name = "GetThreads",
            Caption = "Get Gmail Threads",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Gmail,
            PointType = EnumPointType.Function,
            ObjectType = "GmailThread",
            ClassType = "GmailDataSource",
            Showin = ShowinType.Both,
            Order = 3,
            iconimage = "gmail.png",
            misc = "ReturnType: IEnumerable<GmailThread>"
        )]
        public async Task<IEnumerable<GmailThread>> GetThreads(int maxResults = 10, string? q = null)
        {
            var filters = new List<AppFilter>
            {
                new AppFilter { FieldName = "maxResults", FilterValue = maxResults.ToString(), Operator = "=" }
            };
            if (!string.IsNullOrEmpty(q))
                filters.Add(new AppFilter { FieldName = "q", FilterValue = q, Operator = "=" });

            var result = await GetEntityAsync("threads.list", filters);
            return result.Cast<GmailThread>();
        }

        /// <summary>
        /// Gets Gmail labels
        /// </summary>
        [CommandAttribute(
            Name = "GetLabels",
            Caption = "Get Gmail Labels",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Gmail,
            PointType = EnumPointType.Function,
            ObjectType = "GmailLabel",
            ClassType = "GmailDataSource",
            Showin = ShowinType.Both,
            Order = 4,
            iconimage = "gmail.png",
            misc = "ReturnType: IEnumerable<GmailLabel>"
        )]
        public async Task<IEnumerable<GmailLabel>> GetLabels()
        {
            var result = await GetEntityAsync("labels.list", new List<AppFilter>());
            return result.Cast<GmailLabel>();
        }

        /// <summary>
        /// Gets Gmail profile
        /// </summary>
        [CommandAttribute(
            Name = "GetProfile",
            Caption = "Get Gmail Profile",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Gmail,
            PointType = EnumPointType.Function,
            ObjectType = "GmailProfile",
            ClassType = "GmailDataSource",
            Showin = ShowinType.Both,
            Order = 5,
            iconimage = "gmail.png",
            misc = "ReturnType: IEnumerable<GmailProfile>"
        )]
        public async Task<IEnumerable<GmailProfile>> GetProfile()
        {
            var result = await GetEntityAsync("profile", new List<AppFilter>());
            return result.Cast<GmailProfile>();
        }

        /// <summary>
        /// Sends a Gmail message
        /// </summary>
        [CommandAttribute(
            Name = "SendMessage",
            Caption = "Send Gmail Message",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Gmail,
            PointType = EnumPointType.Function,
            ObjectType = "GmailMessage",
            ClassType = "GmailDataSource",
            Showin = ShowinType.Both,
            Order = 6,
            iconimage = "gmail.png",
            misc = "ReturnType: IEnumerable<GmailMessage>"
        )]
        public async Task<IEnumerable<GmailMessage>> SendMessageAsync(GmailMessage message)
        {
            var url = "https://gmail.googleapis.com/gmail/v1/users/me/messages/send";
            var response = await PostAsync(url, message);
            var json = await response.Content.ReadAsStringAsync();
            var sentMessage = JsonSerializer.Deserialize<GmailMessage>(json);
            return sentMessage != null ? new[] { sentMessage } : Array.Empty<GmailMessage>();
        }

        /// <summary>
        /// Creates a Gmail label
        /// </summary>
        [CommandAttribute(
            Name = "CreateLabel",
            Caption = "Create Gmail Label",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Gmail,
            PointType = EnumPointType.Function,
            ObjectType = "GmailLabel",
            ClassType = "GmailDataSource",
            Showin = ShowinType.Both,
            Order = 7,
            iconimage = "gmail.png",
            misc = "ReturnType: IEnumerable<GmailLabel>"
        )]
        public async Task<IEnumerable<GmailLabel>> CreateLabelAsync(GmailLabel label)
        {
            var url = "https://gmail.googleapis.com/gmail/v1/users/me/labels";
            var response = await PostAsync(url, label);
            var json = await response.Content.ReadAsStringAsync();
            var createdLabel = JsonSerializer.Deserialize<GmailLabel>(json);
            return createdLabel != null ? new[] { createdLabel } : Array.Empty<GmailLabel>();
        }

        /// <summary>
        /// Updates a Gmail label
        /// </summary>
        [CommandAttribute(
            Name = "UpdateLabel",
            Caption = "Update Gmail Label",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Gmail,
            PointType = EnumPointType.Function,
            ObjectType = "GmailLabel",
            ClassType = "GmailDataSource",
            Showin = ShowinType.Both,
            Order = 8,
            iconimage = "gmail.png",
            misc = "ReturnType: IEnumerable<GmailLabel>"
        )]
        public async Task<IEnumerable<GmailLabel>> UpdateLabelAsync(string id, GmailLabel label)
        {
            var url = $"https://gmail.googleapis.com/gmail/v1/users/me/labels/{id}";
            var response = await PutAsync(url, label);
            var json = await response.Content.ReadAsStringAsync();
            var updatedLabel = JsonSerializer.Deserialize<GmailLabel>(json);
            return updatedLabel != null ? new[] { updatedLabel } : Array.Empty<GmailLabel>();
        }

        /// <summary>
        /// Creates a Gmail draft
        /// </summary>
        [CommandAttribute(
            Name = "CreateDraft",
            Caption = "Create Gmail Draft",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Gmail,
            PointType = EnumPointType.Function,
            ObjectType = "GmailDraft",
            ClassType = "GmailDataSource",
            Showin = ShowinType.Both,
            Order = 9,
            iconimage = "gmail.png",
            misc = "ReturnType: IEnumerable<GmailDraft>"
        )]
        public async Task<IEnumerable<GmailDraft>> CreateDraftAsync(GmailDraft draft)
        {
            var url = "https://gmail.googleapis.com/gmail/v1/users/me/drafts";
            var response = await PostAsync(url, draft);
            var json = await response.Content.ReadAsStringAsync();
            var createdDraft = JsonSerializer.Deserialize<GmailDraft>(json);
            return createdDraft != null ? new[] { createdDraft } : Array.Empty<GmailDraft>();
        }

        /// <summary>
        /// Updates a Gmail draft
        /// </summary>
        [CommandAttribute(
            Name = "UpdateDraft",
            Caption = "Update Gmail Draft",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Gmail,
            PointType = EnumPointType.Function,
            ObjectType = "GmailDraft",
            ClassType = "GmailDataSource",
            Showin = ShowinType.Both,
            Order = 10,
            iconimage = "gmail.png",
            misc = "ReturnType: IEnumerable<GmailDraft>"
        )]
        public async Task<IEnumerable<GmailDraft>> UpdateDraftAsync(string id, GmailDraft draft)
        {
            var url = $"https://gmail.googleapis.com/gmail/v1/users/me/drafts/{id}";
            var response = await PutAsync(url, draft);
            var json = await response.Content.ReadAsStringAsync();
            var updatedDraft = JsonSerializer.Deserialize<GmailDraft>(json);
            return updatedDraft != null ? new[] { updatedDraft } : Array.Empty<GmailDraft>();
        }

        // Helper methods for parsing
        private IEnumerable<object> ParseMessages(string json)
        {
            var response = JsonSerializer.Deserialize<GmailMessagesListResponse>(json);
            return response?.Messages ?? Array.Empty<GmailMessage>();
        }

        private IEnumerable<object> ParseMessage(string json)
        {
            var message = JsonSerializer.Deserialize<GmailMessage>(json);
            return message != null ? new[] { message } : Array.Empty<GmailMessage>();
        }

        private IEnumerable<object> ParseThreads(string json)
        {
            var response = JsonSerializer.Deserialize<GmailThreadsListResponse>(json);
            return response?.Threads ?? Array.Empty<GmailThread>();
        }

        private IEnumerable<object> ParseThread(string json)
        {
            var thread = JsonSerializer.Deserialize<GmailThread>(json);
            return thread != null ? new[] { thread } : Array.Empty<GmailThread>();
        }

        private IEnumerable<object> ParseLabels(string json)
        {
            var response = JsonSerializer.Deserialize<GmailLabelsListResponse>(json);
            return response?.Labels ?? Array.Empty<GmailLabel>();
        }

        private IEnumerable<object> ParseLabel(string json)
        {
            var label = JsonSerializer.Deserialize<GmailLabel>(json);
            return label != null ? new[] { label } : Array.Empty<GmailLabel>();
        }

        private IEnumerable<object> ParseDrafts(string json)
        {
            var response = JsonSerializer.Deserialize<GmailDraftsListResponse>(json);
            return response?.Drafts ?? Array.Empty<GmailDraft>();
        }

        private IEnumerable<object> ParseDraft(string json)
        {
            var draft = JsonSerializer.Deserialize<GmailDraft>(json);
            return draft != null ? new[] { draft } : Array.Empty<GmailDraft>();
        }

        private IEnumerable<object> ParseHistory(string json)
        {
            var response = JsonSerializer.Deserialize<GmailHistoryListResponse>(json);
            return response?.History ?? Array.Empty<GmailHistory>();
        }

        private IEnumerable<object> ParseProfile(string json)
        {
            var profile = JsonSerializer.Deserialize<GmailProfile>(json);
            return profile != null ? new[] { profile } : Array.Empty<GmailProfile>();
        }

        // Helper methods
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
            if (required == null || required.Length == 0) return;
            var missing = required.Where(r => !q.ContainsKey(r) || string.IsNullOrWhiteSpace(q[r])).ToList();
            if (missing.Count > 0)
                throw new ArgumentException($"Gmail entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ReplacePlaceholders(string url, Dictionary<string, string> q)
        {
            // Substitute {id} from filters if present
            if (url.Contains("{id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("id", out var id) || string.IsNullOrWhiteSpace(id))
                    throw new ArgumentException("Missing required 'id' filter for this endpoint.");
                url = url.Replace("{id}", Uri.EscapeDataString(id));
            }
            return url;
        }

        private static string BuildQueryParameters(Dictionary<string, string> q)
        {
            var query = new List<string>();
            foreach (var kvp in q)
            {
                if (!string.IsNullOrWhiteSpace(kvp.Value))
                    query.Add($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}");
            }
            return string.Join("&", query);
        }

        // Response classes
        private class GmailMessagesListResponse
        {
            [System.Text.Json.Serialization.JsonPropertyName("messages")] public List<GmailMessage> Messages { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("nextPageToken")] public string NextPageToken { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("resultSizeEstimate")] public int ResultSizeEstimate { get; set; }
        }

        private class GmailThreadsListResponse
        {
            [System.Text.Json.Serialization.JsonPropertyName("threads")] public List<GmailThread> Threads { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("nextPageToken")] public string NextPageToken { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("resultSizeEstimate")] public int ResultSizeEstimate { get; set; }
        }

        private class GmailLabelsListResponse
        {
            [System.Text.Json.Serialization.JsonPropertyName("labels")] public List<GmailLabel> Labels { get; set; }
        }

        private class GmailDraftsListResponse
        {
            [System.Text.Json.Serialization.JsonPropertyName("drafts")] public List<GmailDraft> Drafts { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("nextPageToken")] public string NextPageToken { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("resultSizeEstimate")] public int ResultSizeEstimate { get; set; }
        }

        private class GmailHistoryListResponse
        {
            [System.Text.Json.Serialization.JsonPropertyName("history")] public List<GmailHistory> History { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("nextPageToken")] public string NextPageToken { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("historyId")] public string HistoryId { get; set; }
        }
    }
}