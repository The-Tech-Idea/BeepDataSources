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
            [JsonPropertyName("messages")] public List<GmailMessage> Messages { get; set; }
            [JsonPropertyName("nextPageToken")] public string NextPageToken { get; set; }
            [JsonPropertyName("resultSizeEstimate")] public int ResultSizeEstimate { get; set; }
        }

        private class GmailThreadsListResponse
        {
            [JsonPropertyName("threads")] public List<GmailThread> Threads { get; set; }
            [JsonPropertyName("nextPageToken")] public string NextPageToken { get; set; }
            [JsonPropertyName("resultSizeEstimate")] public int ResultSizeEstimate { get; set; }
        }

        private class GmailLabelsListResponse
        {
            [JsonPropertyName("labels")] public List<GmailLabel> Labels { get; set; }
        }

        private class GmailDraftsListResponse
        {
            [JsonPropertyName("drafts")] public List<GmailDraft> Drafts { get; set; }
            [JsonPropertyName("nextPageToken")] public string NextPageToken { get; set; }
            [JsonPropertyName("resultSizeEstimate")] public int ResultSizeEstimate { get; set; }
        }

        private class GmailHistoryListResponse
        {
            [JsonPropertyName("history")] public List<GmailHistory> History { get; set; }
            [JsonPropertyName("nextPageToken")] public string NextPageToken { get; set; }
            [JsonPropertyName("historyId")] public string HistoryId { get; set; }
        }
    }
}