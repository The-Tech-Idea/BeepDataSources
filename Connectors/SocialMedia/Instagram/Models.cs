// File: Connectors/Instagram/Models/InstagramModels.cs
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Instagram.Models
{
    public abstract class IgEntityBase
    {
        [JsonIgnore] public IDataSource DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : IgEntityBase { DataSource = ds; return (T)this; }
    }

    // ---------------- Users / Accounts ----------------
    public sealed class IgUser : IgEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("username")] public string Username { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }          // Basic Display may not include
        [JsonPropertyName("profile_picture_url")] public string ProfilePictureUrl { get; set; }
        [JsonPropertyName("account_type")] public string AccountType { get; set; }   // BUSINESS, CREATOR, PERSONAL
        [JsonPropertyName("media_count")] public int? MediaCount { get; set; }
    }

    // ---------------- Media ----------------
    public sealed class IgMedia : IgEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("caption")] public string Caption { get; set; }
        [JsonPropertyName("media_type")] public string MediaType { get; set; }    // IMAGE, VIDEO, CAROUSEL_ALBUM
        [JsonPropertyName("media_url")] public string MediaUrl { get; set; }
        [JsonPropertyName("thumbnail_url")] public string ThumbnailUrl { get; set; }
        [JsonPropertyName("permalink")] public string Permalink { get; set; }
        [JsonPropertyName("shortcode")] public string Shortcode { get; set; }
        [JsonPropertyName("timestamp")] public DateTimeOffset? Timestamp { get; set; }
        [JsonPropertyName("username")] public string Username { get; set; }
        [JsonPropertyName("children")] public IgChildren Children { get; set; } // when included via fields
    }

    public sealed class IgChildren
    {
        [JsonPropertyName("data")] public List<IgChild> Data { get; set; } = new();
    }

    public sealed class IgChild
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("media_type")] public string MediaType { get; set; }
        [JsonPropertyName("media_url")] public string MediaUrl { get; set; }
        [JsonPropertyName("thumbnail_url")] public string ThumbnailUrl { get; set; }
    }

    // ---------------- Comments & Replies ----------------
    public sealed class IgComment : IgEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("text")] public string Text { get; set; }
        [JsonPropertyName("username")] public string Username { get; set; }
        [JsonPropertyName("timestamp")] public DateTimeOffset? Timestamp { get; set; }
        [JsonPropertyName("like_count")] public int? LikeCount { get; set; }
    }

    // ---------------- Insights (simplified) ----------------
    public sealed class IgInsight : IgEntityBase
    {
        [JsonPropertyName("name")] public string Name { get; set; }     // metric
        [JsonPropertyName("period")] public string Period { get; set; }   // lifetime, day, etc.
        [JsonPropertyName("values")] public List<IgInsightValue> Values { get; set; } = new();
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("id")] public string Id { get; set; }
    }

    public sealed class IgInsightValue
    {
        [JsonPropertyName("value")] public object Value { get; set; }
        [JsonPropertyName("end_time")] public DateTimeOffset? EndTime { get; set; }
    }

    // ---------------- Registry ----------------
    public static class IgEntityRegistry
    {
        public static readonly Dictionary<string, Type> Types = new(StringComparer.OrdinalIgnoreCase)
        {
            ["me"] = typeof(IgUser),
            ["me.accounts"] = typeof(IgUser),
            ["me.media"] = typeof(IgMedia),
            ["me.stories"] = typeof(IgMedia),

            ["users.by_id"] = typeof(IgUser),
            ["users.media"] = typeof(IgMedia),
            ["users.stories"] = typeof(IgMedia),

            ["media.by_id"] = typeof(IgMedia),
            ["media.children"] = typeof(IgChild),
            ["media.comments"] = typeof(IgComment),
            ["comments.replies"] = typeof(IgComment),

            ["media.insights"] = typeof(IgInsight),
        };

        public static IReadOnlyList<string> Names => new List<string>(Types.Keys);
        public static Type Resolve(string entityName) =>
            entityName != null && Types.TryGetValue(entityName, out var t) ? t : null;
    }
}
