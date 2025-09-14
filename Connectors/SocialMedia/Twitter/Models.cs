// File: Connectors/Twitter/Models/TwitterModels.cs
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Twitter.Models
{

    public sealed class TwitterContextAnnotation
    {
        [JsonPropertyName("domain")] public TwitterContextDomain Domain { get; set; }
        [JsonPropertyName("entity")] public TwitterContextEntity Entity { get; set; }
    }

    public sealed class TwitterContextDomain
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
    }

    public sealed class TwitterContextEntity
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
    }
    // -------------------------------------------------------
    // Base
    // -------------------------------------------------------
    public abstract class TwitterEntityBase
    {
        [JsonIgnore] public IDataSource DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : TwitterEntityBase { DataSource = ds; return (T)this; }
    }

    // -------------------------------------------------------
    // Tweet
    // -------------------------------------------------------
    public sealed class TwitterTweet : TwitterEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("text")] public string Text { get; set; }
        [JsonPropertyName("author_id")] public string AuthorId { get; set; }
        [JsonPropertyName("conversation_id")] public string ConversationId { get; set; }
        [JsonPropertyName("in_reply_to_user_id")] public string InReplyToUserId { get; set; }
        [JsonPropertyName("created_at")] public DateTimeOffset? CreatedAt { get; set; }
        [JsonPropertyName("lang")] public string Lang { get; set; }
        [JsonPropertyName("source")] public string Source { get; set; }
        [JsonPropertyName("possibly_sensitive")] public bool? PossiblySensitive { get; set; }

        [JsonPropertyName("public_metrics")] public TwitterTweetPublicMetrics PublicMetrics { get; set; }
        [JsonPropertyName("entities")] public TwitterTweetEntities Entities { get; set; }
        [JsonPropertyName("attachments")] public TwitterTweetAttachments Attachments { get; set; }
        [JsonPropertyName("referenced_tweets")] public List<TwitterTweetRef> ReferencedTweets { get; set; } = new();
        [JsonPropertyName("context_annotations")] public List<TwitterContextAnnotation> ContextAnnotations { get; set; } = new();
        [JsonPropertyName("geo")] public TwitterTweetGeo Geo { get; set; }
    }

    public sealed class TwitterTweetPublicMetrics
    {
        [JsonPropertyName("retweet_count")] public int? RetweetCount { get; set; }
        [JsonPropertyName("reply_count")] public int? ReplyCount { get; set; }
        [JsonPropertyName("like_count")] public int? LikeCount { get; set; }
        [JsonPropertyName("quote_count")] public int? QuoteCount { get; set; }
        [JsonPropertyName("impression_count")] public int? ImpressionCount { get; set; }
    }

    public sealed class TwitterTweetEntities
    {
        [JsonPropertyName("hashtags")] public List<TwitterHashtag> Hashtags { get; set; } = new();
        [JsonPropertyName("mentions")] public List<TwitterMention> Mentions { get; set; } = new();
        [JsonPropertyName("urls")] public List<TwitterUrl> Urls { get; set; } = new();
        [JsonPropertyName("cashtags")] public List<TwitterCashtag> Cashtags { get; set; } = new();
    }

    public sealed class TwitterTweetAttachments
    {
        [JsonPropertyName("media_keys")] public List<string> MediaKeys { get; set; } = new();
        [JsonPropertyName("poll_ids")] public List<string> PollIds { get; set; } = new();
    }

    public sealed class TwitterTweetRef
    {
        [JsonPropertyName("type")] public string Type { get; set; } // "retweeted","quoted","replied_to"
        [JsonPropertyName("id")] public string Id { get; set; }
    }

    public sealed class TwitterTweetGeo
    {
        [JsonPropertyName("place_id")] public string PlaceId { get; set; }
    }

    // Entities: hashtags, mentions, urls, cashtags
    public sealed class TwitterHashtag
    {
        [JsonPropertyName("tag")] public string Tag { get; set; }
        [JsonPropertyName("start")] public int? Start { get; set; }
        [JsonPropertyName("end")] public int? End { get; set; }
    }

    public sealed class TwitterMention
    {
        [JsonPropertyName("username")] public string Username { get; set; }
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("start")] public int? Start { get; set; }
        [JsonPropertyName("end")] public int? End { get; set; }
    }

    public sealed class TwitterUrl
    {
        [JsonPropertyName("url")] public string Url { get; set; }
        [JsonPropertyName("expanded_url")] public string ExpandedUrl { get; set; }
        [JsonPropertyName("display_url")] public string DisplayUrl { get; set; }
        [JsonPropertyName("unwound_url")] public string UnwoundUrl { get; set; }
        [JsonPropertyName("status")] public int? Status { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("images")] public List<TwitterUrlImage> Images { get; set; } = new();
        [JsonPropertyName("start")] public int? Start { get; set; }
        [JsonPropertyName("end")] public int? End { get; set; }
    }

    public sealed class TwitterUrlImage
    {
        [JsonPropertyName("url")] public string Url { get; set; }
        [JsonPropertyName("width")] public int? Width { get; set; }
        [JsonPropertyName("height")] public int? Height { get; set; }
    }

    public sealed class TwitterCashtag
    {
        [JsonPropertyName("tag")] public string Tag { get; set; }
        [JsonPropertyName("start")] public int? Start { get; set; }
        [JsonPropertyName("end")] public int? End { get; set; }
    }

    // -------------------------------------------------------
    // User
    // -------------------------------------------------------
    public sealed class TwitterUser : TwitterEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("username")] public string Username { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("verified")] public bool? Verified { get; set; }
        [JsonPropertyName("protected")] public bool? Protected { get; set; }
        [JsonPropertyName("location")] public string Location { get; set; }
        [JsonPropertyName("url")] public string Url { get; set; }
        [JsonPropertyName("profile_image_url")] public string ProfileImageUrl { get; set; }
        [JsonPropertyName("created_at")] public DateTimeOffset? CreatedAt { get; set; }

        [JsonPropertyName("public_metrics")] public TwitterUserPublicMetrics PublicMetrics { get; set; }
        [JsonPropertyName("entities")] public TwitterUserEntities Entities { get; set; }
    }

    public sealed class TwitterUserPublicMetrics
    {
        [JsonPropertyName("followers_count")] public int? FollowersCount { get; set; }
        [JsonPropertyName("following_count")] public int? FollowingCount { get; set; }
        [JsonPropertyName("tweet_count")] public int? TweetCount { get; set; }
        [JsonPropertyName("listed_count")] public int? ListedCount { get; set; }
    }

    public sealed class TwitterUserEntities
    {
        [JsonPropertyName("url")] public TwitterUserUrl Url { get; set; }
        [JsonPropertyName("description")] public TwitterUserDescription Description { get; set; }
    }

    public sealed class TwitterUserUrl
    {
        [JsonPropertyName("urls")] public List<TwitterUrl> Urls { get; set; } = new();
    }

    public sealed class TwitterUserDescription
    {
        [JsonPropertyName("hashtags")] public List<TwitterHashtag> Hashtags { get; set; } = new();
        [JsonPropertyName("mentions")] public List<TwitterMention> Mentions { get; set; } = new();
        [JsonPropertyName("urls")] public List<TwitterUrl> Urls { get; set; } = new();
        [JsonPropertyName("cashtags")] public List<TwitterCashtag> Cashtags { get; set; } = new();
    }

    // -------------------------------------------------------
    // Media
    // -------------------------------------------------------
    public sealed class TwitterMedia : TwitterEntityBase
    {
        [JsonPropertyName("media_key")] public string MediaKey { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; } // photo, video, animated_gif
        [JsonPropertyName("url")] public string Url { get; set; }  // for photos
        [JsonPropertyName("preview_image_url")] public string PreviewImageUrl { get; set; } // for videos/gifs
        [JsonPropertyName("width")] public int? Width { get; set; }
        [JsonPropertyName("height")] public int? Height { get; set; }
        [JsonPropertyName("duration_ms")] public int? DurationMs { get; set; }
        [JsonPropertyName("alt_text")] public string AltText { get; set; }
        [JsonPropertyName("public_metrics")] public TwitterMediaPublicMetrics PublicMetrics { get; set; }
    }

    public sealed class TwitterMediaPublicMetrics
    {
        [JsonPropertyName("view_count")] public int? ViewCount { get; set; }
    }

    // -------------------------------------------------------
    // Place (geo)
    // -------------------------------------------------------
    public sealed class TwitterPlace : TwitterEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("full_name")] public string FullName { get; set; }
        [JsonPropertyName("country")] public string Country { get; set; }
        [JsonPropertyName("country_code")] public string CountryCode { get; set; }
        [JsonPropertyName("place_type")] public string PlaceType { get; set; }
        [JsonPropertyName("geo")] public TwitterPlaceGeo Geo { get; set; }
    }

    public sealed class TwitterPlaceGeo
    {
        [JsonPropertyName("type")] public string Type { get; set; } // "Feature"
        [JsonPropertyName("bbox")] public List<double> Bbox { get; set; } = new(); // [west, south, east, north]
        [JsonPropertyName("properties")] public Dictionary<string, object> Properties { get; set; } = new();
    }

    // -------------------------------------------------------
    // Lists
    // -------------------------------------------------------
    public sealed class TwitterList : TwitterEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("owner_id")] public string OwnerId { get; set; }
        [JsonPropertyName("follower_count")] public int? FollowerCount { get; set; }
        [JsonPropertyName("member_count")] public int? MemberCount { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("private")] public bool? Private { get; set; }
        [JsonPropertyName("created_at")] public DateTimeOffset? CreatedAt { get; set; }
    }

    // -------------------------------------------------------
    // Spaces
    // -------------------------------------------------------
    public sealed class TwitterSpace : TwitterEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("state")] public string State { get; set; } // live, scheduled, ended
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("scheduled_start")] public DateTimeOffset? ScheduledStart { get; set; }
        [JsonPropertyName("started_at")] public DateTimeOffset? StartedAt { get; set; }
        [JsonPropertyName("ended_at")] public DateTimeOffset? EndedAt { get; set; }
        [JsonPropertyName("lang")] public string Lang { get; set; }
        [JsonPropertyName("host_ids")] public List<string> HostIds { get; set; } = new();
        [JsonPropertyName("speaker_ids")] public List<string> SpeakerIds { get; set; } = new();
        [JsonPropertyName("topic_ids")] public List<string> TopicIds { get; set; } = new();
    }

    // -------------------------------------------------------
    // Registry (maps your fixed entity names to CLR types)
    // -------------------------------------------------------
    public static class TwitterEntityRegistry
    {
        public static readonly Dictionary<string, Type> Types = new(StringComparer.OrdinalIgnoreCase)
        {
            // Tweets
            ["tweets.search"] = typeof(TwitterTweet),
            ["tweets.by_id"] = typeof(TwitterTweet),
            ["users.tweets"] = typeof(TwitterTweet),
            ["lists.tweets"] = typeof(TwitterTweet),

            // Users
            ["users.by_username"] = typeof(TwitterUser),
            ["users.by_id"] = typeof(TwitterUser),
            ["users.followers"] = typeof(TwitterUser),
            ["users.following"] = typeof(TwitterUser),

            // Lists & Spaces
            ["lists.by_user"] = typeof(TwitterList),
            ["spaces.search"] = typeof(TwitterSpace),

            // Expansions (optional)
            ["media"] = typeof(TwitterMedia),
            ["places"] = typeof(TwitterPlace),
        };

        public static IReadOnlyList<string> Names => new List<string>(Types.Keys);
    }
}
