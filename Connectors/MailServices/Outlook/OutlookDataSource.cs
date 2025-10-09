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
using TheTechIdea.Beep.Connectors.Outlook.Models;
using TheTechIdea.Beep.Connectors.Outlook.Models;

namespace TheTechIdea.Beep.Connectors.Outlook
{
    /// <summary>
    /// Outlook data source implementation using WebAPIDataSource as base class
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Outlook)]
    public class OutlookDataSource : WebAPIDataSource
    {
        // Entity endpoints mapping for Microsoft Graph API
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // Messages
            ["messages"] = "me/messages",
            ["messages.get"] = "me/messages/{id}",
            ["messages.send"] = "me/sendMail",
            ["messages.reply"] = "me/messages/{id}/reply",
            ["messages.replyAll"] = "me/messages/{id}/replyAll",
            ["messages.forward"] = "me/messages/{id}/forward",
            ["messages.move"] = "me/messages/{id}/move",
            ["messages.copy"] = "me/messages/{id}/copy",
            ["messages.delete"] = "me/messages/{id}",
            // MailFolders
            ["mailFolders"] = "me/mailFolders",
            ["mailFolders.get"] = "me/mailFolders/{id}",
            ["mailFolders.messages"] = "me/mailFolders/{id}/messages",
            ["mailFolders.childFolders"] = "me/mailFolders/{id}/childFolders",
            // Contacts
            ["contacts"] = "me/contacts",
            ["contacts.get"] = "me/contacts/{id}",
            // Events (Calendar)
            ["events"] = "me/events",
            ["events.get"] = "me/events/{id}",
            ["calendars"] = "me/calendars",
            ["calendars.get"] = "me/calendars/{id}",
            ["calendars.events"] = "me/calendars/{id}/events"
        };

        // Required filters for each entity
        private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            ["messages.get"] = new[] { "id" },
            ["messages.reply"] = new[] { "id" },
            ["messages.replyAll"] = new[] { "id" },
            ["messages.forward"] = new[] { "id" },
            ["messages.move"] = new[] { "id" },
            ["messages.copy"] = new[] { "id" },
            ["messages.delete"] = new[] { "id" },
            ["mailFolders.get"] = new[] { "id" },
            ["mailFolders.messages"] = new[] { "id" },
            ["mailFolders.childFolders"] = new[] { "id" },
            ["contacts.get"] = new[] { "id" },
            ["events.get"] = new[] { "id" },
            ["calendars.get"] = new[] { "id" },
            ["calendars.events"] = new[] { "id" }
        };

        public OutlookDataSource(
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
                throw new InvalidOperationException($"Unknown Outlook entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            // Build the full URL
            var url = $"https://graph.microsoft.com/v1.0/{endpoint}";
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
                "messages" or "mailFolders.messages" => ParseMessages(json),
                "messages.get" => ParseMessage(json),
                "mailFolders" => ParseMailFolders(json),
                "mailFolders.get" => ParseMailFolder(json),
                "contacts" => ParseContacts(json),
                "contacts.get" => ParseContact(json),
                "events" or "calendars.events" => ParseEvents(json),
                "events.get" => ParseEvent(json),
                "calendars" => ParseCalendars(json),
                "calendars.get" => ParseCalendar(json),
                _ => throw new NotSupportedException($"Entity '{EntityName}' parsing not implemented.")
            };
        }

        // Helper methods for parsing
        private IEnumerable<object> ParseMessages(string json)
        {
            var response = JsonSerializer.Deserialize<OutlookMessagesResponse>(json);
            return response?.Value ?? Array.Empty<OutlookMessage>();
        }

        private IEnumerable<object> ParseMessage(string json)
        {
            var message = JsonSerializer.Deserialize<OutlookMessage>(json);
            return message != null ? new[] { message } : Array.Empty<OutlookMessage>();
        }

        private IEnumerable<object> ParseMailFolders(string json)
        {
            var response = JsonSerializer.Deserialize<OutlookMailFoldersResponse>(json);
            return response?.Value ?? Array.Empty<OutlookMailFolder>();
        }

        private IEnumerable<object> ParseMailFolder(string json)
        {
            var folder = JsonSerializer.Deserialize<OutlookMailFolder>(json);
            return folder != null ? new[] { folder } : Array.Empty<OutlookMailFolder>();
        }

        private IEnumerable<object> ParseContacts(string json)
        {
            var response = JsonSerializer.Deserialize<OutlookContactsResponse>(json);
            return response?.Value ?? Array.Empty<OutlookContact>();
        }

        private IEnumerable<object> ParseContact(string json)
        {
            var contact = JsonSerializer.Deserialize<OutlookContact>(json);
            return contact != null ? new[] { contact } : Array.Empty<OutlookContact>();
        }

        private IEnumerable<object> ParseEvents(string json)
        {
            var response = JsonSerializer.Deserialize<OutlookEventsResponse>(json);
            return response?.Value ?? Array.Empty<OutlookEvent>();
        }

        private IEnumerable<object> ParseEvent(string json)
        {
            var eventItem = JsonSerializer.Deserialize<OutlookEvent>(json);
            return eventItem != null ? new[] { eventItem } : Array.Empty<OutlookEvent>();
        }

        private IEnumerable<object> ParseCalendars(string json)
        {
            var response = JsonSerializer.Deserialize<OutlookCalendarsResponse>(json);
            return response?.Value ?? Array.Empty<OutlookCalendar>();
        }

        private IEnumerable<object> ParseCalendar(string json)
        {
            var calendar = JsonSerializer.Deserialize<OutlookCalendar>(json);
            return calendar != null ? new[] { calendar } : Array.Empty<OutlookCalendar>();
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
                throw new ArgumentException($"Outlook entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
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
        private class OutlookMessagesResponse
        {
            [JsonPropertyName("@odata.context")] public string OdataContext { get; set; }
            [JsonPropertyName("@odata.nextLink")] public string OdataNextLink { get; set; }
            [JsonPropertyName("value")] public List<OutlookMessage> Value { get; set; }
        }

        private class OutlookMailFoldersResponse
        {
            [JsonPropertyName("@odata.context")] public string OdataContext { get; set; }
            [JsonPropertyName("value")] public List<OutlookMailFolder> Value { get; set; }
        }

        private class OutlookContactsResponse
        {
            [JsonPropertyName("@odata.context")] public string OdataContext { get; set; }
            [JsonPropertyName("@odata.nextLink")] public string OdataNextLink { get; set; }
            [JsonPropertyName("value")] public List<OutlookContact> Value { get; set; }
        }

        private class OutlookEventsResponse
        {
            [JsonPropertyName("@odata.context")] public string OdataContext { get; set; }
            [JsonPropertyName("@odata.nextLink")] public string OdataNextLink { get; set; }
            [JsonPropertyName("value")] public List<OutlookEvent> Value { get; set; }
        }

        private class OutlookCalendarsResponse
        {
            [JsonPropertyName("@odata.context")] public string OdataContext { get; set; }
            [JsonPropertyName("value")] public List<OutlookCalendar> Value { get; set; }
        }

        // Calendar model (simplified)
        private class OutlookCalendar
        {
            [JsonPropertyName("@odata.etag")] public string OdataEtag { get; set; }
            [JsonPropertyName("id")] public string Id { get; set; }
            [JsonPropertyName("name")] public string Name { get; set; }
            [JsonPropertyName("color")] public string Color { get; set; }
            [JsonPropertyName("changeKey")] public string ChangeKey { get; set; }
            [JsonPropertyName("canShare")] public bool CanShare { get; set; }
            [JsonPropertyName("canViewPrivateItems")] public bool CanViewPrivateItems { get; set; }
            [JsonPropertyName("canEdit")] public bool CanEdit { get; set; }
            [JsonPropertyName("owner")] public OutlookEmailAddress Owner { get; set; }
        }
    }
}