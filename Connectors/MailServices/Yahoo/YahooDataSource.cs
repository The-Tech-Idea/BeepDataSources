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
using TheTechIdea.Beep.Connectors.Yahoo.Models;
using TheTechIdea.Beep.Connectors.Yahoo.Models;

namespace TheTechIdea.Beep.Connectors.Yahoo
{
    /// <summary>
    /// Yahoo data source implementation using WebAPIDataSource as base class
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Yahoo)]
    public class YahooDataSource : WebAPIDataSource
    {
        // Entity endpoints mapping for Yahoo Mail API
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // Messages
            ["messages"] = "messages",
            ["messages.get"] = "messages/{id}",
            ["messages.send"] = "messages/send",
            ["messages.delete"] = "messages/{id}",
            ["messages.move"] = "messages/{id}/move",
            // Contacts
            ["contacts"] = "contacts",
            ["contacts.get"] = "contacts/{id}",
            // Folders
            ["folders"] = "folders",
            ["folders.get"] = "folders/{id}"
        };

        // Required filters for each entity
        private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            ["messages.get"] = new[] { "id" },
            ["messages.delete"] = new[] { "id" },
            ["messages.move"] = new[] { "id" },
            ["contacts.get"] = new[] { "id" },
            ["folders.get"] = new[] { "id" }
        };

        public YahooDataSource(
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
                throw new InvalidOperationException($"Unknown Yahoo entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            // Build the full URL
            var url = $"https://mail.yahooapis.com/ws/mail/v1.1/{endpoint}";
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
                "messages" => ParseMessages(json),
                "messages.get" => ParseMessage(json),
                "contacts" => ParseContacts(json),
                "contacts.get" => ParseContact(json),
                "folders" => ParseFolders(json),
                "folders.get" => ParseFolder(json),
                _ => throw new NotSupportedException($"Entity '{EntityName}' parsing not implemented.")
            };
        }

        // Helper methods for parsing
        private IEnumerable<object> ParseMessages(string json)
        {
            var response = JsonSerializer.Deserialize<YahooMessagesResponse>(json);
            return response?.Messages ?? Array.Empty<YahooMessage>();
        }

        private IEnumerable<object> ParseMessage(string json)
        {
            var message = JsonSerializer.Deserialize<YahooMessage>(json);
            return message != null ? new[] { message } : Array.Empty<YahooMessage>();
        }

        private IEnumerable<object> ParseContacts(string json)
        {
            var response = JsonSerializer.Deserialize<YahooContactsResponse>(json);
            return response?.Contacts ?? Array.Empty<YahooContact>();
        }

        private IEnumerable<object> ParseContact(string json)
        {
            var contact = JsonSerializer.Deserialize<YahooContact>(json);
            return contact != null ? new[] { contact } : Array.Empty<YahooContact>();
        }

        private IEnumerable<object> ParseFolders(string json)
        {
            var response = JsonSerializer.Deserialize<YahooFoldersResponse>(json);
            return response?.Folders ?? Array.Empty<YahooFolder>();
        }

        private IEnumerable<object> ParseFolder(string json)
        {
            var folder = JsonSerializer.Deserialize<YahooFolder>(json);
            return folder != null ? new[] { folder } : Array.Empty<YahooFolder>();
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
                throw new ArgumentException($"Yahoo entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
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
        private class YahooMessagesResponse
        {
            [JsonPropertyName("messages")] public List<YahooMessage> Messages { get; set; }
        }

        private class YahooContactsResponse
        {
            [JsonPropertyName("contacts")] public List<YahooContact> Contacts { get; set; }
        }

        private class YahooFoldersResponse
        {
            [JsonPropertyName("folders")] public List<YahooFolder> Folders { get; set; }
        }
    }
}