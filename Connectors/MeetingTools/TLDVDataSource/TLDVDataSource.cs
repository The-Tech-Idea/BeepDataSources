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
using TheTechIdea.Beep.Connectors.TLDV.Models;

namespace TheTechIdea.Beep.Connectors.TLDV
{
    /// <summary>
    /// tl;dv data source implementation using WebAPIDataSource as base class
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.TLDV)]
    public class TLDVDataSource : WebAPIDataSource
    {
        // Entity endpoints mapping for tl;dv API
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // Meetings
            ["meetings"] = "v1/meetings",
            ["meetings.get"] = "v1/meetings/{id}",
            // Transcriptions
            ["transcriptions"] = "v1/meetings/{meeting_id}/transcription",
            // Summaries
            ["summaries"] = "v1/meetings/{meeting_id}/summary",
            // Chapters
            ["chapters"] = "v1/meetings/{meeting_id}/chapters",
            // Highlights
            ["highlights"] = "v1/meetings/{meeting_id}/highlights"
        };

        // Required filters for each entity
        private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            ["meetings.get"] = new[] { "id" },
            ["transcriptions"] = new[] { "meeting_id" },
            ["summaries"] = new[] { "meeting_id" },
            ["chapters"] = new[] { "meeting_id" },
            ["highlights"] = new[] { "meeting_id" }
        };

        public TLDVDataSource(
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
                throw new InvalidOperationException($"Unknown tl;dv entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            // Build the full URL
            var url = $"https://api.tldv.io/{endpoint}";
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
                "meetings" => ParseMeetings(json),
                "meetings.get" => ParseMeeting(json),
                "transcriptions" => ParseTranscription(json),
                "summaries" => ParseSummary(json),
                "chapters" => ParseChapters(json),
                "highlights" => ParseHighlights(json),
                _ => throw new NotSupportedException($"Entity '{EntityName}' parsing not implemented.")
            };
        }

        // Helper methods for parsing
        private IEnumerable<object> ParseMeetings(string json)
        {
            var response = JsonSerializer.Deserialize<TLDVMeetingsResponse>(json);
            return response?.Meetings ?? new List<TLDVMeeting>();
        }

        private IEnumerable<object> ParseMeeting(string json)
        {
            var meeting = JsonSerializer.Deserialize<TLDVMeeting>(json);
            return meeting != null ? new[] { meeting } : Array.Empty<TLDVMeeting>();
        }

        private IEnumerable<object> ParseTranscription(string json)
        {
            var transcription = JsonSerializer.Deserialize<TLDVTranscription>(json);
            return transcription != null ? new[] { transcription } : Array.Empty<TLDVTranscription>();
        }

        private IEnumerable<object> ParseSummary(string json)
        {
            var summary = JsonSerializer.Deserialize<TLDVSummary>(json);
            return summary != null ? new[] { summary } : Array.Empty<TLDVSummary>();
        }

        private IEnumerable<object> ParseChapters(string json)
        {
            var chapters = JsonSerializer.Deserialize<List<TLDVChapter>>(json);
            return chapters ?? new List<TLDVChapter>();
        }

        private IEnumerable<object> ParseHighlights(string json)
        {
            var highlights = JsonSerializer.Deserialize<List<TLDVHighlight>>(json);
            return highlights ?? new List<TLDVHighlight>();
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
                throw new ArgumentException($"tl;dv entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ReplacePlaceholders(string url, Dictionary<string, string> q)
        {
            // Substitute {id} and {meeting_id} from filters if present
            if (url.Contains("{id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("id", out var id) || string.IsNullOrWhiteSpace(id))
                    throw new ArgumentException("Missing required 'id' filter for this endpoint.");
                url = url.Replace("{id}", Uri.EscapeDataString(id));
            }
            if (url.Contains("{meeting_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("meeting_id", out var meetingId) || string.IsNullOrWhiteSpace(meetingId))
                    throw new ArgumentException("Missing required 'meeting_id' filter for this endpoint.");
                url = url.Replace("{meeting_id}", Uri.EscapeDataString(meetingId));
            }
            return url;
        }

        private static string BuildQueryParameters(Dictionary<string, string> q)
        {
            var query = new List<string>();
            foreach (var kvp in q)
            {
                if (!string.IsNullOrWhiteSpace(kvp.Value) && !kvp.Key.Contains("{") && !kvp.Key.Contains("}"))
                    query.Add($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}");
            }
            return string.Join("&", query);
        }
    }
}