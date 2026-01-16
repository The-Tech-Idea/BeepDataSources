// File: Connectors/Slack/Models/SlackModels.cs
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Slack.Models
{
    // -------------------------------------------------------
    // Base
    // -------------------------------------------------------
    public abstract class SlackEntityBase
    {
        [JsonIgnore] public IDataSource DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : SlackEntityBase { DataSource = ds; return (T)this; }
    }

    // -------------------------------------------------------
    // Conversations / Channels
    // -------------------------------------------------------
    public sealed class SlackChannel : SlackEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("is_channel")] public bool? IsChannel { get; set; }
        [JsonPropertyName("is_group")] public bool? IsGroup { get; set; }
        [JsonPropertyName("is_im")] public bool? IsIm { get; set; }
        [JsonPropertyName("is_mpim")] public bool? IsMpim { get; set; }
        [JsonPropertyName("is_private")] public bool? IsPrivate { get; set; }
        [JsonPropertyName("is_archived")] public bool? IsArchived { get; set; }
        [JsonPropertyName("num_members")] public int? NumMembers { get; set; }
        [JsonPropertyName("created")] public long? Created { get; set; } // unix seconds
        [JsonPropertyName("topic")] public SlackTopic Topic { get; set; }
        [JsonPropertyName("purpose")] public SlackTopic Purpose { get; set; }
    }

    public sealed class SlackTopic
    {
        [JsonPropertyName("value")] public string Value { get; set; }
        [JsonPropertyName("creator")] public string Creator { get; set; }
        [JsonPropertyName("last_set")] public long? LastSet { get; set; } // unix seconds
    }

    // -------------------------------------------------------
    // Messages
    // -------------------------------------------------------
    public sealed class SlackMessage : SlackEntityBase
    {
        [JsonPropertyName("type")] public string Type { get; set; }          // e.g., "message"
        [JsonPropertyName("ts")] public string Ts { get; set; }            // e.g., "1692123456.000200"
        [JsonPropertyName("user")] public string User { get; set; }
        [JsonPropertyName("text")] public string Text { get; set; }
        [JsonPropertyName("bot_id")] public string BotId { get; set; }
        [JsonPropertyName("thread_ts")] public string ThreadTs { get; set; }
        [JsonPropertyName("reply_count")] public int? ReplyCount { get; set; }
        [JsonPropertyName("files")] public List<SlackFile> Files { get; set; } = new();
        [JsonPropertyName("reactions")] public List<SlackReaction> Reactions { get; set; } = new();
        [JsonPropertyName("subtype")] public string Subtype { get; set; }       // e.g., "bot_message"
        [JsonPropertyName("parent_user_id")] public string ParentUserId { get; set; }
        [JsonPropertyName("latest_reply")] public string LatestReply { get; set; }
    }

    public sealed class SlackReaction
    {
        [JsonPropertyName("name")] public string Name { get; set; }   // e.g., "thumbsup"
        [JsonPropertyName("count")] public int? Count { get; set; }
        [JsonPropertyName("users")] public List<string> Users { get; set; } = new();
    }

    // -------------------------------------------------------
    // Users
    // -------------------------------------------------------
    public sealed class SlackUser : SlackEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("team_id")] public string TeamId { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("real_name")] public string RealName { get; set; }
        [JsonPropertyName("deleted")] public bool? Deleted { get; set; }
        [JsonPropertyName("is_admin")] public bool? IsAdmin { get; set; }
        [JsonPropertyName("is_owner")] public bool? IsOwner { get; set; }
        [JsonPropertyName("is_bot")] public bool? IsBot { get; set; }
        [JsonPropertyName("tz")] public string TimeZone { get; set; }
        [JsonPropertyName("profile")] public SlackUserProfile Profile { get; set; }
    }

    public sealed class SlackUserProfile
    {
        [JsonPropertyName("display_name")] public string Caption { get; set; }
        [JsonPropertyName("real_name")] public string RealName { get; set; }
        [JsonPropertyName("email")] public string Email { get; set; }
        [JsonPropertyName("image_72")] public string Image72 { get; set; }
        [JsonPropertyName("image_192")] public string Image192 { get; set; }
        [JsonPropertyName("image_512")] public string Image512 { get; set; }
        [JsonPropertyName("phone")] public string Phone { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; }
    }

    // -------------------------------------------------------
    // Files
    // -------------------------------------------------------
    public sealed class SlackFile : SlackEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("created")] public long? Created { get; set; }  // unix seconds
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("mimetype")] public string MimeType { get; set; }
        [JsonPropertyName("filetype")] public string FileType { get; set; }
        [JsonPropertyName("size")] public long? Size { get; set; }
        [JsonPropertyName("url_private")] public string UrlPrivate { get; set; }
        [JsonPropertyName("url_private_download")] public string UrlPrivateDownload { get; set; }
        [JsonPropertyName("user")] public string User { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("permalink")] public string Permalink { get; set; }
    }

    // -------------------------------------------------------
    // IM / MPIM (DMs & group DMs)
    // -------------------------------------------------------
    public sealed class SlackIm : SlackEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("is_im")] public bool? IsIm { get; set; }
        [JsonPropertyName("user")] public string User { get; set; }
        [JsonPropertyName("created")] public long? Created { get; set; }
    }

    public sealed class SlackMpim : SlackEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("is_mpim")] public bool? IsMpim { get; set; }
        [JsonPropertyName("created")] public long? Created { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("members")] public List<string> Members { get; set; } = new();
    }

    // -------------------------------------------------------
    // Team / Auth / Bots / Apps
    // -------------------------------------------------------
    public sealed class SlackTeam : SlackEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("domain")] public string Domain { get; set; }
        [JsonPropertyName("email_domain")] public string EmailDomain { get; set; }
        [JsonPropertyName("icon")] public SlackTeamIcon Icon { get; set; }
    }

    public sealed class SlackTeamIcon
    {
        [JsonPropertyName("image_34")] public string Image34 { get; set; }
        [JsonPropertyName("image_44")] public string Image44 { get; set; }
        [JsonPropertyName("image_68")] public string Image68 { get; set; }
        [JsonPropertyName("image_88")] public string Image88 { get; set; }
        [JsonPropertyName("image_102")] public string Image102 { get; set; }
        [JsonPropertyName("image_132")] public string Image132 { get; set; }
        [JsonPropertyName("image_230")] public string Image230 { get; set; }
    }

    public sealed class SlackAuthInfo : SlackEntityBase
    {
        [JsonPropertyName("ok")] public bool Ok { get; set; }
        [JsonPropertyName("url")] public string Url { get; set; }
        [JsonPropertyName("team")] public string Team { get; set; }
        [JsonPropertyName("user")] public string User { get; set; }
        [JsonPropertyName("team_id")] public string TeamId { get; set; }
        [JsonPropertyName("user_id")] public string UserId { get; set; }
    }

    public sealed class SlackBot : SlackEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("app_id")] public string AppId { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("updated")] public long? Updated { get; set; }
        [JsonPropertyName("team_id")] public string TeamId { get; set; }
        [JsonPropertyName("icons")] public Dictionary<string, string> Icons { get; set; } = new();
    }

    // apps.permissions.scopes.list returns a "scopes" payload; keep simple
    public sealed class SlackAppScope : SlackEntityBase
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("is_customized")] public bool? IsCustomized { get; set; }
    }

    // -------------------------------------------------------
    // Pins / Stars / Reactions list items (generic item refs)
    // -------------------------------------------------------
    public sealed class SlackItemRef
    {
        [JsonPropertyName("type")] public string Type { get; set; }    // "message", "file", "file_comment"
        [JsonPropertyName("channel")] public string Channel { get; set; } // may be null
        [JsonPropertyName("message")] public SlackMessage Message { get; set; }
        [JsonPropertyName("file")] public SlackFile File { get; set; }
    }

    public sealed class SlackPinItem : SlackEntityBase
    {
        [JsonPropertyName("channel")] public string Channel { get; set; }
        [JsonPropertyName("created")] public long? Created { get; set; }
        [JsonPropertyName("created_by")] public string CreatedBy { get; set; }
        [JsonPropertyName("item")] public SlackItemRef Item { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; }
    }

    public sealed class SlackStarItem : SlackEntityBase
    {
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("channel")] public string Channel { get; set; }
        [JsonPropertyName("message")] public SlackMessage Message { get; set; }
        [JsonPropertyName("file")] public SlackFile File { get; set; }
    }

    // -------------------------------------------------------
    // Reminders
    // -------------------------------------------------------
    public sealed class SlackReminder : SlackEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("creator")] public string Creator { get; set; }
        [JsonPropertyName("user")] public string User { get; set; }
        [JsonPropertyName("text")] public string Text { get; set; }
        [JsonPropertyName("recurring")] public bool? Recurring { get; set; }
        [JsonPropertyName("time")] public long? Time { get; set; }        // unix seconds
        [JsonPropertyName("complete_ts")] public long? CompleteTs { get; set; } // unix seconds
    }

    // -------------------------------------------------------
    // Search (messages)
    // -------------------------------------------------------
    public sealed class SlackSearchMessage : SlackEntityBase
    {
        [JsonPropertyName("channel")] public SlackSearchChannel Channel { get; set; }
        [JsonPropertyName("username")] public string Username { get; set; }
        [JsonPropertyName("user")] public string User { get; set; }
        [JsonPropertyName("permalink")] public string Permalink { get; set; }
        [JsonPropertyName("text")] public string Text { get; set; }
        [JsonPropertyName("ts")] public string Ts { get; set; }
    }

    public sealed class SlackSearchChannel
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
    }

    // -------------------------------------------------------
    // User Groups
    // -------------------------------------------------------
    public sealed class SlackUserGroup : SlackEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("team_id")] public string TeamId { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("handle")] public string Handle { get; set; }
        [JsonPropertyName("is_external")] public bool? IsExternal { get; set; }
        [JsonPropertyName("is_usergroup")] public bool? IsUserGroup { get; set; }
        [JsonPropertyName("users")] public List<string> Users { get; set; } = new();
    }

    // -------------------------------------------------------
    // Registry: expose the fixed list so your IDataSource can advertise types
    // -------------------------------------------------------
    public static class SlackEntityRegistry
    {
        // Logical entity name -> CLR type
        public static readonly Dictionary<string, Type> Types = new(StringComparer.OrdinalIgnoreCase)
        {
            ["conversations"] = typeof(SlackChannel),
            ["channels"] = typeof(SlackChannel),
            ["groups"] = typeof(SlackChannel),
            ["im"] = typeof(SlackIm),
            ["mpim"] = typeof(SlackMpim),

            ["messages"] = typeof(SlackMessage),
            ["users"] = typeof(SlackUser),
            ["files"] = typeof(SlackFile),

            ["reactions"] = typeof(SlackReaction),   // note: reactions.list actually returns items; keep simple
            ["pins"] = typeof(SlackPinItem),
            ["stars"] = typeof(SlackStarItem),

            ["reminders"] = typeof(SlackReminder),
            ["search"] = typeof(SlackSearchMessage),

            ["team"] = typeof(SlackTeam),
            ["teams"] = typeof(SlackTeam),
            ["auth"] = typeof(SlackAuthInfo),
            ["bots"] = typeof(SlackBot),
            ["apps"] = typeof(SlackAppScope),

            ["usergroups"] = typeof(SlackUserGroup),
        };

        public static IReadOnlyList<string> Names => new List<string>(Types.Keys);
    }
}
