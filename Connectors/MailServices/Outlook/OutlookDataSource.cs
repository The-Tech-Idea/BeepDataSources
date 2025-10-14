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

        // CommandAttribute methods for Outlook API
        [CommandAttribute(Name = "GetMessages", Caption = "Get Outlook Messages", ObjectType = "OutlookMessage", PointType = EnumPointType.Function, Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Outlook, ClassType = "OutlookMessage", Showin = ShowinType.Grid, Order = 1, iconimage = "mail.png")]
        public async Task<IEnumerable<OutlookMessage>> GetMessages(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("messages", filters);
            return result.Cast<OutlookMessage>();
        }

        [CommandAttribute(Name = "GetMessage", Caption = "Get Outlook Message", ObjectType = "OutlookMessage", PointType = EnumPointType.Function, Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Outlook, ClassType = "OutlookMessage", Showin = ShowinType.Grid, Order = 2, iconimage = "mail.png")]
        public async Task<IEnumerable<OutlookMessage>> GetMessage(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("messages.get", filters);
            return result.Cast<OutlookMessage>();
        }

        [CommandAttribute(Name = "GetMailFolders", Caption = "Get Outlook Mail Folders", ObjectType = "OutlookMailFolder", PointType = EnumPointType.Function, Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Outlook, ClassType = "OutlookMailFolder", Showin = ShowinType.Grid, Order = 3, iconimage = "folder.png")]
        public async Task<IEnumerable<OutlookMailFolder>> GetMailFolders(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("mailFolders", filters);
            return result.Cast<OutlookMailFolder>();
        }

        [CommandAttribute(Name = "GetContacts", Caption = "Get Outlook Contacts", ObjectType = "OutlookContact", PointType = EnumPointType.Function, Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Outlook, ClassType = "OutlookContact", Showin = ShowinType.Grid, Order = 4, iconimage = "contact.png")]
        public async Task<IEnumerable<OutlookContact>> GetContacts(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("contacts", filters);
            return result.Cast<OutlookContact>();
        }

        [CommandAttribute(Name = "GetEvents", Caption = "Get Outlook Events", ObjectType = "OutlookEvent", PointType = EnumPointType.Function, Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Outlook, ClassType = "OutlookEvent", Showin = ShowinType.Grid, Order = 5, iconimage = "calendar.png")]
        public async Task<IEnumerable<OutlookEvent>> GetEvents(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("events", filters);
            return result.Cast<OutlookEvent>();
        }

        [CommandAttribute(Name = "GetCalendars", Caption = "Get Outlook Calendars", ObjectType = "OutlookCalendar", PointType = EnumPointType.Function, Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Outlook, ClassType = "OutlookCalendar", Showin = ShowinType.Grid, Order = 6, iconimage = "calendar.png")]
        public async Task<IEnumerable<OutlookCalendar>> GetCalendars(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("calendars", filters);
            return result.Cast<OutlookCalendar>();
        }

        /// <summary>
        /// Sends an Outlook message
        /// </summary>
        [CommandAttribute(
            Name = "SendMessage",
            Caption = "Send Outlook Message",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Outlook,
            PointType = EnumPointType.Function,
            ObjectType = "OutlookMessage",
            ClassType = "OutlookDataSource",
            Showin = ShowinType.Both,
            Order = 7,
            iconimage = "mail.png",
            misc = "ReturnType: IEnumerable<OutlookMessage>"
        )]
        public async Task<IEnumerable<OutlookMessage>> SendMessageAsync(OutlookMessage message)
        {
            var url = "https://graph.microsoft.com/v1.0/me/sendMail";
            var response = await PostAsync(url, message);
            // SendMail returns no content, so return empty or the original message
            return new[] { message };
        }

        /// <summary>
        /// Creates an Outlook mail folder
        /// </summary>
        [CommandAttribute(
            Name = "CreateMailFolder",
            Caption = "Create Outlook Mail Folder",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Outlook,
            PointType = EnumPointType.Function,
            ObjectType = "OutlookMailFolder",
            ClassType = "OutlookDataSource",
            Showin = ShowinType.Both,
            Order = 8,
            iconimage = "folder.png",
            misc = "ReturnType: IEnumerable<OutlookMailFolder>"
        )]
        public async Task<IEnumerable<OutlookMailFolder>> CreateMailFolderAsync(OutlookMailFolder folder)
        {
            var url = "https://graph.microsoft.com/v1.0/me/mailFolders";
            var response = await PostAsync(url, folder);
            var json = await response.Content.ReadAsStringAsync();
            var createdFolder = JsonSerializer.Deserialize<OutlookMailFolder>(json);
            return createdFolder != null ? new[] { createdFolder } : Array.Empty<OutlookMailFolder>();
        }

        /// <summary>
        /// Updates an Outlook mail folder
        /// </summary>
        [CommandAttribute(
            Name = "UpdateMailFolder",
            Caption = "Update Outlook Mail Folder",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Outlook,
            PointType = EnumPointType.Function,
            ObjectType = "OutlookMailFolder",
            ClassType = "OutlookDataSource",
            Showin = ShowinType.Both,
            Order = 9,
            iconimage = "folder.png",
            misc = "ReturnType: IEnumerable<OutlookMailFolder>"
        )]
        public async Task<IEnumerable<OutlookMailFolder>> UpdateMailFolderAsync(string id, OutlookMailFolder folder)
        {
            var url = $"https://graph.microsoft.com/v1.0/me/mailFolders/{id}";
            var response = await PatchAsync(url, folder);
            var json = await response.Content.ReadAsStringAsync();
            var updatedFolder = JsonSerializer.Deserialize<OutlookMailFolder>(json);
            return updatedFolder != null ? new[] { updatedFolder } : Array.Empty<OutlookMailFolder>();
        }

        /// <summary>
        /// Creates an Outlook contact
        /// </summary>
        [CommandAttribute(
            Name = "CreateContact",
            Caption = "Create Outlook Contact",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Outlook,
            PointType = EnumPointType.Function,
            ObjectType = "OutlookContact",
            ClassType = "OutlookDataSource",
            Showin = ShowinType.Both,
            Order = 10,
            iconimage = "contact.png",
            misc = "ReturnType: IEnumerable<OutlookContact>"
        )]
        public async Task<IEnumerable<OutlookContact>> CreateContactAsync(OutlookContact contact)
        {
            var url = "https://graph.microsoft.com/v1.0/me/contacts";
            var response = await PostAsync(url, contact);
            var json = await response.Content.ReadAsStringAsync();
            var createdContact = JsonSerializer.Deserialize<OutlookContact>(json);
            return createdContact != null ? new[] { createdContact } : Array.Empty<OutlookContact>();
        }

        /// <summary>
        /// Updates an Outlook contact
        /// </summary>
        [CommandAttribute(
            Name = "UpdateContact",
            Caption = "Update Outlook Contact",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Outlook,
            PointType = EnumPointType.Function,
            ObjectType = "OutlookContact",
            ClassType = "OutlookDataSource",
            Showin = ShowinType.Both,
            Order = 11,
            iconimage = "contact.png",
            misc = "ReturnType: IEnumerable<OutlookContact>"
        )]
        public async Task<IEnumerable<OutlookContact>> UpdateContactAsync(string id, OutlookContact contact)
        {
            var url = $"https://graph.microsoft.com/v1.0/me/contacts/{id}";
            var response = await PatchAsync(url, contact);
            var json = await response.Content.ReadAsStringAsync();
            var updatedContact = JsonSerializer.Deserialize<OutlookContact>(json);
            return updatedContact != null ? new[] { updatedContact } : Array.Empty<OutlookContact>();
        }

        /// <summary>
        /// Creates an Outlook event
        /// </summary>
        [CommandAttribute(
            Name = "CreateEvent",
            Caption = "Create Outlook Event",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Outlook,
            PointType = EnumPointType.Function,
            ObjectType = "OutlookEvent",
            ClassType = "OutlookDataSource",
            Showin = ShowinType.Both,
            Order = 12,
            iconimage = "calendar.png",
            misc = "ReturnType: IEnumerable<OutlookEvent>"
        )]
        public async Task<IEnumerable<OutlookEvent>> CreateEventAsync(OutlookEvent eventItem)
        {
            var url = "https://graph.microsoft.com/v1.0/me/events";
            var response = await PostAsync(url, eventItem);
            var json = await response.Content.ReadAsStringAsync();
            var createdEvent = JsonSerializer.Deserialize<OutlookEvent>(json);
            return createdEvent != null ? new[] { createdEvent } : Array.Empty<OutlookEvent>();
        }

        /// <summary>
        /// Updates an Outlook event
        /// </summary>
        [CommandAttribute(
            Name = "UpdateEvent",
            Caption = "Update Outlook Event",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Outlook,
            PointType = EnumPointType.Function,
            ObjectType = "OutlookEvent",
            ClassType = "OutlookDataSource",
            Showin = ShowinType.Both,
            Order = 13,
            iconimage = "calendar.png",
            misc = "ReturnType: IEnumerable<OutlookEvent>"
        )]
        public async Task<IEnumerable<OutlookEvent>> UpdateEventAsync(string id, OutlookEvent eventItem)
        {
            var url = $"https://graph.microsoft.com/v1.0/me/events/{id}";
            var response = await PatchAsync(url, eventItem);
            var json = await response.Content.ReadAsStringAsync();
            var updatedEvent = JsonSerializer.Deserialize<OutlookEvent>(json);
            return updatedEvent != null ? new[] { updatedEvent } : Array.Empty<OutlookEvent>();
        }

        /// <summary>
        /// Creates an Outlook calendar
        /// </summary>
        [CommandAttribute(
            Name = "CreateCalendar",
            Caption = "Create Outlook Calendar",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Outlook,
            PointType = EnumPointType.Function,
            ObjectType = "OutlookCalendar",
            ClassType = "OutlookDataSource",
            Showin = ShowinType.Both,
            Order = 14,
            iconimage = "calendar.png",
            misc = "ReturnType: IEnumerable<OutlookCalendar>"
        )]
        public async Task<IEnumerable<OutlookCalendar>> CreateCalendarAsync(OutlookCalendar calendar)
        {
            var url = "https://graph.microsoft.com/v1.0/me/calendars";
            var response = await PostAsync(url, calendar);
            var json = await response.Content.ReadAsStringAsync();
            var createdCalendar = JsonSerializer.Deserialize<OutlookCalendar>(json);
            return createdCalendar != null ? new[] { createdCalendar } : Array.Empty<OutlookCalendar>();
        }

        /// <summary>
        /// Updates an Outlook calendar
        /// </summary>
        [CommandAttribute(
            Name = "UpdateCalendar",
            Caption = "Update Outlook Calendar",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Outlook,
            PointType = EnumPointType.Function,
            ObjectType = "OutlookCalendar",
            ClassType = "OutlookDataSource",
            Showin = ShowinType.Both,
            Order = 15,
            iconimage = "calendar.png",
            misc = "ReturnType: IEnumerable<OutlookCalendar>"
        )]
        public async Task<IEnumerable<OutlookCalendar>> UpdateCalendarAsync(string id, OutlookCalendar calendar)
        {
            var url = $"https://graph.microsoft.com/v1.0/me/calendars/{id}";
            var response = await PatchAsync(url, calendar);
            var json = await response.Content.ReadAsStringAsync();
            var updatedCalendar = JsonSerializer.Deserialize<OutlookCalendar>(json);
            return updatedCalendar != null ? new[] { updatedCalendar } : Array.Empty<OutlookCalendar>();
        }
    }
}