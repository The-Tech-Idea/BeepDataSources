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
using TheTechIdea.Beep.Connectors.Communication.Telegram.Models;

namespace TheTechIdea.Beep.Connectors.Communication.Telegram
{
    /// <summary>
    /// Telegram Bot API data source built on WebAPIDataSource.
    /// Configure WebAPIConnectionProperties.Url to your bot endpoint, e.g.:
    ///   https://api.telegram.org/bot<YOUR_TOKEN>/
    /// Auth lives entirely in WebAPIConnectionProperties (no defaults here).
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Telegram)]
    public class TelegramDataSource : WebAPIDataSource
    {
        // Supported logical entities -> Telegram method + result root + required filters
        // All Telegram methods return { ok: bool, result: <array|object> }, so root="result".
        private static readonly Dictionary<string, (string endpoint, string root, string[] requiredFilters)> Map
            = new(StringComparer.OrdinalIgnoreCase)
            {
                ["updates"] = ("getUpdates", "result", Array.Empty<string>()),               // optional: offset, limit, timeout, allowed_updates
                ["me"] = ("getMe", "result", Array.Empty<string>()),
                ["chat"] = ("getChat", "result", new[] { "chat_id" }),
                ["chatMembersCount"] = ("getChatMemberCount", "result", new[] { "chat_id" }),
                ["chatMember"] = ("getChatMember", "result", new[] { "chat_id", "user_id" }),
                ["chatAdministrators"] = ("getChatAdministrators", "result", new[] { "chat_id" }),
                ["userProfilePhotos"] = ("getUserProfilePhotos", "result", new[] { "user_id" }),                 // optional: offset, limit
                ["file"] = ("getFile", "result", new[] { "file_id" }),
                ["webhookInfo"] = ("getWebhookInfo", "result", Array.Empty<string>()),
                ["myCommands"] = ("getMyCommands", "result", Array.Empty<string>())
            };

        public TelegramDataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor dmeEditor,
            DataSourceType databasetype,
            IErrorsInfo errorObject)
            : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
        {
            // Ensure WebAPI props exist; caller configures Url/Auth outside this class.
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
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
                throw new InvalidOperationException($"Unknown Telegram entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            using var resp = await GetAsync(m.endpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, m.root);
        }

        // paged
        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            if (!Map.TryGetValue(EntityName, out var m))
                throw new InvalidOperationException($"Unknown Telegram entity '{EntityName}'.");

            var q = FiltersToQuery(filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            // Only getUpdates supports real paging (offset + limit).
            // For other endpoints we just fetch once and fill PagedResult heuristically.
            if (!EntityName.Equals("updates", StringComparison.OrdinalIgnoreCase))
            {
                var respNon = GetAsync(m.endpoint, q).ConfigureAwait(false).GetAwaiter().GetResult();
                var itemsNon = ExtractArray(respNon, m.root);

                int page = Math.Max(1, pageNumber);
                return new PagedResult
                {
                    Data = itemsNon,
                    PageNumber = page,
                    PageSize = Math.Max(1, pageSize),
                    TotalRecords = itemsNon.Count,
                    TotalPages = 1,
                    HasPreviousPage = false,
                    HasNextPage = false
                };
            }

            // ---- Paging for getUpdates ----
            int limit = Math.Max(1, Math.Min(pageSize <= 0 ? 100 : pageSize, 100));
            q["limit"] = limit.ToString();

            // Use offset as our "cursor"; the server returns updates with monotonically increasing update_id
            long? offset = null;
            if (q.TryGetValue("offset", out var off) && long.TryParse(off, out var offL)) offset = offL;

            int targetPage = Math.Max(1, pageNumber);
            for (int i = 1; i < targetPage; i++)
            {
                var stepResp = GetAsync(m.endpoint, MergeOffset(q, offset)).ConfigureAwait(false).GetAwaiter().GetResult();
                var stepItems = ExtractArray(stepResp, m.root);
                offset = ComputeNextOffset(offset, stepItems); // offset = lastUpdateId + 1
                if (stepItems.Count < limit) break; // no more pages
            }

            var finalResp = GetAsync(m.endpoint, MergeOffset(q, offset)).ConfigureAwait(false).GetAwaiter().GetResult();
            var pageItems = ExtractArray(finalResp, m.root);
            var nextOffset = ComputeNextOffset(offset, pageItems);

            bool hasNext = pageItems.Count == limit; // if we filled the page, assume possibly more
            int pageIdx = targetPage;

            return new PagedResult
            {
                Data = pageItems,
                PageNumber = pageIdx,
                PageSize = limit,
                TotalRecords = (pageIdx - 1) * limit + pageItems.Count, // best-effort total-so-far
                TotalPages = hasNext ? pageIdx + 1 : pageIdx,         // heuristic
                HasPreviousPage = pageIdx > 1,
                HasNextPage = hasNext
            };
        }

        // ---------------- helpers ----------------

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
            var missing = new List<string>();
            foreach (var r in required)
                if (!q.ContainsKey(r) || string.IsNullOrWhiteSpace(q[r])) missing.Add(r);

            if (missing.Count > 0)
                throw new ArgumentException($"Telegram entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static Dictionary<string, string> MergeOffset(Dictionary<string, string> q, long? offset)
        {
            var m = new Dictionary<string, string>(q, StringComparer.OrdinalIgnoreCase);
            if (offset.HasValue) m["offset"] = offset.Value.ToString();
            return m;
        }

        // Compute next offset = (max update_id on this page) + 1
        private static long? ComputeNextOffset(long? existing, List<object> items)
        {
            long maxId = existing ?? 0;
            foreach (var o in items)
            {
                try
                {
                    if (o is Dictionary<string, object> row &&
                        row.TryGetValue("update_id", out var v) &&
                        v is JsonElement je &&
                        je.ValueKind == JsonValueKind.Number &&
                        je.TryGetInt64(out var asLong))
                    {
                        if (asLong > maxId) maxId = asLong;
                    }
                    else if (o is Dictionary<string, object> row2 &&
                             row2.TryGetValue("update_id", out var v2) &&
                             v2 is long l2)
                    {
                        if (l2 > maxId) maxId = l2;
                    }
                }
                catch { /* ignore */ }
            }
            if (maxId == 0 && !existing.HasValue) return existing;
            return maxId + 1;
        }

        private async Task<HttpResponseMessage> GetAsync(string endpoint, Dictionary<string, string> query, CancellationToken ct = default)
            => await base.GetAsync(endpoint, query, cancellationToken: ct).ConfigureAwait(false);

        // Telegram always wraps data under "result"
        private static List<object> ExtractArray(HttpResponseMessage resp, string root)
        {
            var list = new List<object>();
            if (resp == null) return list;

            var json = resp.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(json);

            JsonElement node = doc.RootElement;
            if (!string.IsNullOrWhiteSpace(root))
            {
                if (!node.TryGetProperty(root, out node))
                    return list;
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
                var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(node.GetRawText(), opts);
                if (obj != null) list.Add(obj);
            }

            return list;
        }

        // ------------ CommandAttribute Methods ------------

    [CommandAttribute(Name = "GetUpdates", Caption = "Get Telegram Updates",
        ObjectType = "TgUpdate", PointType = EnumPointType.Function,
        Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Telegram,
        ClassType = "WebAPIDataSource", Showin = ShowinType.Both, Order = 1,
        iconimage = "telegram.png", misc = "Optional: offset, limit, timeout, allowed_updates")]
    public async Task<IEnumerable<TgUpdate>> GetUpdates(List<AppFilter> filters = null)
    {
        var result = await GetEntityAsync("updates", filters ?? new List<AppFilter>());
        return result.Cast<TgUpdate>().Select(u => u.Attach<TgUpdate>(this));
    }

    [CommandAttribute(Name = "GetMe", Caption = "Get Telegram Bot Info",
        ObjectType = "TgUser", PointType = EnumPointType.Function,
        Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Telegram,
        ClassType = "WebAPIDataSource", Showin = ShowinType.Both, Order = 2,
        iconimage = "telegram.png", misc = "Get current bot information")]
    public async Task<IEnumerable<TgUser>> GetMe(List<AppFilter> filters = null)
    {
        var result = await GetEntityAsync("me", filters ?? new List<AppFilter>());
        return result.Cast<TgUser>().Select(u => u.Attach<TgUser>(this));
    }

    [CommandAttribute(Name = "GetChat", Caption = "Get Telegram Chat Info",
        ObjectType = "TgChat", PointType = EnumPointType.Function,
        Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Telegram,
        ClassType = "WebAPIDataSource", Showin = ShowinType.Both, Order = 3,
        iconimage = "telegram.png", misc = "Requires chat_id filter")]
    public async Task<IEnumerable<TgChat>> GetChat(List<AppFilter> filters = null)
    {
        var result = await GetEntityAsync("chat", filters ?? new List<AppFilter>());
        return result.Cast<TgChat>().Select(c => c.Attach<TgChat>(this));
    }

    [CommandAttribute(Name = "GetChatMember", Caption = "Get Telegram Chat Member",
        ObjectType = "TgChatMember", PointType = EnumPointType.Function,
        Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Telegram,
        ClassType = "WebAPIDataSource", Showin = ShowinType.Both, Order = 4,
        iconimage = "telegram.png", misc = "Requires chat_id and user_id filters")]
    public async Task<IEnumerable<TgChatMember>> GetChatMember(List<AppFilter> filters = null)
    {
        var result = await GetEntityAsync("chatMember", filters ?? new List<AppFilter>());
        return result.Cast<TgChatMember>().Select(m => m.Attach<TgChatMember>(this));
    }

    [CommandAttribute(Name = "GetChatAdministrators", Caption = "Get Telegram Chat Administrators",
        ObjectType = "TgChatMember", PointType = EnumPointType.Function,
        Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Telegram,
        ClassType = "WebAPIDataSource", Showin = ShowinType.Both, Order = 5,
        iconimage = "telegram.png", misc = "Requires chat_id filter")]
    public async Task<IEnumerable<TgChatMember>> GetChatAdministrators(List<AppFilter> filters = null)
    {
        var result = await GetEntityAsync("chatAdministrators", filters ?? new List<AppFilter>());
        return result.Cast<TgChatMember>().Select(m => m.Attach<TgChatMember>(this));
    }

    [CommandAttribute(Name = "GetUserProfilePhotos", Caption = "Get Telegram User Profile Photos",
        ObjectType = "TgUserProfilePhotos", PointType = EnumPointType.Function,
        Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Telegram,
        ClassType = "WebAPIDataSource", Showin = ShowinType.Both, Order = 6,
        iconimage = "telegram.png", misc = "Requires user_id filter, optional: offset, limit")]
    public async Task<IEnumerable<TgUserProfilePhotos>> GetUserProfilePhotos(List<AppFilter> filters = null)
    {
        var result = await GetEntityAsync("userProfilePhotos", filters ?? new List<AppFilter>());
        return result.Cast<TgUserProfilePhotos>().Select(p => p.Attach<TgUserProfilePhotos>(this));
    }

    [CommandAttribute(Name = "GetFile", Caption = "Get Telegram File Info",
        ObjectType = "TgFile", PointType = EnumPointType.Function,
        Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Telegram,
        ClassType = "WebAPIDataSource", Showin = ShowinType.Both, Order = 7,
        iconimage = "telegram.png", misc = "Requires file_id filter")]
    public async Task<IEnumerable<TgFile>> GetFile(List<AppFilter> filters = null)
    {
        var result = await GetEntityAsync("file", filters ?? new List<AppFilter>());
        return result.Cast<TgFile>().Select(f => f.Attach<TgFile>(this));
    }

    [CommandAttribute(Name = "GetWebhookInfo", Caption = "Get Telegram Webhook Info",
        ObjectType = "TgWebhookInfo", PointType = EnumPointType.Function,
        Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Telegram,
        ClassType = "WebAPIDataSource", Showin = ShowinType.Both, Order = 8,
        iconimage = "telegram.png", misc = "Get current webhook information")]
    public async Task<IEnumerable<TgWebhookInfo>> GetWebhookInfo(List<AppFilter> filters = null)
    {
        var result = await GetEntityAsync("webhookInfo", filters ?? new List<AppFilter>());
        return result.Cast<TgWebhookInfo>().Select(w => w.Attach<TgWebhookInfo>(this));
    }

    [CommandAttribute(Name = "GetMyCommands", Caption = "Get Telegram Bot Commands",
        ObjectType = "TgBotCommand", PointType = EnumPointType.Function,
        Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Telegram,
        ClassType = "WebAPIDataSource", Showin = ShowinType.Both, Order = 9,
        iconimage = "telegram.png", misc = "Get current bot commands")]
    public async Task<IEnumerable<TgBotCommand>> GetMyCommands(List<AppFilter> filters = null)
    {
        var result = await GetEntityAsync("myCommands", filters ?? new List<AppFilter>());
        return result.Cast<TgBotCommand>().Select(c => c.Attach<TgBotCommand>(this));
    }

    [CommandAttribute(Name = "SendMessageAsync", Caption = "Send Telegram Message",
        ObjectType = "TgMessage", PointType = EnumPointType.Function,
        Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Telegram,
        ClassType = "WebAPIDataSource", Showin = ShowinType.Both, Order = 10,
        iconimage = "telegram.png", misc = "Send a message to a chat")]
    public async Task<IEnumerable<TgMessage>> SendMessageAsync(TgMessage message, List<AppFilter> filters = null)
    {
        var result = await PostAsync("sendMessage", message, filters ?? new List<AppFilter>());
        return JsonSerializer.Deserialize<IEnumerable<TgMessage>>(result);
    }

    [CommandAttribute(Name = "SendPhotoAsync", Caption = "Send Telegram Photo",
        ObjectType = "TgMessage", PointType = EnumPointType.Function,
        Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Telegram,
        ClassType = "WebAPIDataSource", Showin = ShowinType.Both, Order = 11,
        iconimage = "telegram.png", misc = "Send a photo to a chat")]
    public async Task<IEnumerable<TgMessage>> SendPhotoAsync(TgMessage message, List<AppFilter> filters = null)
    {
        var result = await PostAsync("sendPhoto", message, filters ?? new List<AppFilter>());
        return JsonSerializer.Deserialize<IEnumerable<TgMessage>>(result);
    }

    [CommandAttribute(Name = "SendDocumentAsync", Caption = "Send Telegram Document",
        ObjectType = "TgMessage", PointType = EnumPointType.Function,
        Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Telegram,
        ClassType = "WebAPIDataSource", Showin = ShowinType.Both, Order = 12,
        iconimage = "telegram.png", misc = "Send a document to a chat")]
    public async Task<IEnumerable<TgMessage>> SendDocumentAsync(TgMessage message, List<AppFilter> filters = null)
    {
        var result = await PostAsync("sendDocument", message, filters ?? new List<AppFilter>());
        return JsonSerializer.Deserialize<IEnumerable<TgMessage>>(result);
    }
}
}
