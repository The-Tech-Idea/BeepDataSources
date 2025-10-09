using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.BufferDataSource
{
    /// <summary>
    /// Base class for Buffer entities
    /// </summary>
    public abstract class BufferEntityBase : Entity
    {
        /// <summary>
        /// Unique identifier for the entity
        /// </summary>
        [JsonIgnore]
        public IDataSource DataSource { get; set; }
    }

    /// <summary>
    /// Represents a Buffer post/update
    /// </summary>
    public sealed class BufferPost : BufferEntityBase
    {
        /// <summary>
        /// The unique identifier for this update
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// The date and time when this update was created
        /// </summary>
        [JsonPropertyName("created_at")]
        public long CreatedAt { get; set; }

        /// <summary>
        /// The date and time when this update is scheduled to be sent
        /// </summary>
        [JsonPropertyName("scheduled_at")]
        public long? ScheduledAt { get; set; }

        /// <summary>
        /// The date and time when this update was sent
        /// </summary>
        [JsonPropertyName("sent_at")]
        public long? SentAt { get; set; }

        /// <summary>
        /// The text of the update
        /// </summary>
        [JsonPropertyName("text")]
        public string Text { get; set; }

        /// <summary>
        /// The HTML text of the update
        /// </summary>
        [JsonPropertyName("text_formatted")]
        public string TextFormatted { get; set; }

        /// <summary>
        /// The status of the update
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; }

        /// <summary>
        /// The service where this update will be posted
        /// </summary>
        [JsonPropertyName("service")]
        public string Service { get; set; }

        /// <summary>
        /// The profile ID where this update will be posted
        /// </summary>
        [JsonPropertyName("profile_id")]
        public string ProfileId { get; set; }

        /// <summary>
        /// The user ID who created this update
        /// </summary>
        [JsonPropertyName("user_id")]
        public string UserId { get; set; }

        /// <summary>
        /// Whether this update is pinned
        /// </summary>
        [JsonPropertyName("pinned")]
        public bool Pinned { get; set; }

        /// <summary>
        /// The media attachments for this update
        /// </summary>
        [JsonPropertyName("media")]
        public BufferMedia Media { get; set; }

        /// <summary>
        /// Statistics for this update
        /// </summary>
        [JsonPropertyName("statistics")]
        public BufferStatistics Statistics { get; set; }
    }

    /// <summary>
    /// Represents media attached to a Buffer post
    /// </summary>
    public sealed class BufferMedia : BufferEntityBase
    {
        /// <summary>
        /// The URL of the media
        /// </summary>
        [JsonPropertyName("url")]
        public string Url { get; set; }

        /// <summary>
        /// The thumbnail URL of the media
        /// </summary>
        [JsonPropertyName("thumbnail")]
        public string Thumbnail { get; set; }

        /// <summary>
        /// The type of media
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; }

        /// <summary>
        /// The title of the media
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; }

        /// <summary>
        /// The description of the media
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; }
    }

    /// <summary>
    /// Represents statistics for a Buffer post
    /// </summary>
    public sealed class BufferStatistics : BufferEntityBase
    {
        /// <summary>
        /// Number of clicks
        /// </summary>
        [JsonPropertyName("clicks")]
        public int Clicks { get; set; }

        /// <summary>
        /// Number of favorites/likes
        /// </summary>
        [JsonPropertyName("favorites")]
        public int Favorites { get; set; }

        /// <summary>
        /// Number of retweets/shares
        /// </summary>
        [JsonPropertyName("shares")]
        public int Shares { get; set; }

        /// <summary>
        /// Number of impressions/views
        /// </summary>
        [JsonPropertyName("impressions")]
        public int Impressions { get; set; }
    }

    /// <summary>
    /// Represents a Buffer social media profile
    /// </summary>
    public sealed class BufferProfile : BufferEntityBase
    {
        /// <summary>
        /// The unique identifier for this profile
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// The service type (twitter, facebook, etc.)
        /// </summary>
        [JsonPropertyName("service")]
        public string Service { get; set; }

        /// <summary>
        /// The service username
        /// </summary>
        [JsonPropertyName("service_username")]
        public string ServiceUsername { get; set; }

        /// <summary>
        /// The service user ID
        /// </summary>
        [JsonPropertyName("service_id")]
        public string ServiceId { get; set; }

        /// <summary>
        /// The timezone for this profile
        /// </summary>
        [JsonPropertyName("timezone")]
        public string Timezone { get; set; }

        /// <summary>
        /// Whether this profile is active
        /// </summary>
        [JsonPropertyName("active")]
        public bool Active { get; set; }

        /// <summary>
        /// The profile statistics
        /// </summary>
        [JsonPropertyName("statistics")]
        public BufferProfileStatistics Statistics { get; set; }
    }

    /// <summary>
    /// Represents statistics for a Buffer profile
    /// </summary>
    public sealed class BufferProfileStatistics : BufferEntityBase
    {
        /// <summary>
        /// Number of followers
        /// </summary>
        [JsonPropertyName("followers")]
        public int Followers { get; set; }

        /// <summary>
        /// Number of following
        /// </summary>
        [JsonPropertyName("following")]
        public int Following { get; set; }

        /// <summary>
        /// Number of posts
        /// </summary>
        [JsonPropertyName("posts")]
        public int Posts { get; set; }
    }

    /// <summary>
    /// Represents a Buffer campaign
    /// </summary>
    public sealed class BufferCampaign : BufferEntityBase
    {
        /// <summary>
        /// The unique identifier for this campaign
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// The name of the campaign
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// The description of the campaign
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; }

        /// <summary>
        /// The date the campaign was created
        /// </summary>
        [JsonPropertyName("created_at")]
        public long CreatedAt { get; set; }

        /// <summary>
        /// The date the campaign was last updated
        /// </summary>
        [JsonPropertyName("updated_at")]
        public long UpdatedAt { get; set; }

        /// <summary>
        /// Whether the campaign is active
        /// </summary>
        [JsonPropertyName("active")]
        public bool Active { get; set; }

        /// <summary>
        /// The campaign color
        /// </summary>
        [JsonPropertyName("color")]
        public string Color { get; set; }
    }

    /// <summary>
    /// Represents a Buffer link
    /// </summary>
    public sealed class BufferLink : BufferEntityBase
    {
        /// <summary>
        /// The unique identifier for this link
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// The URL of the link
        /// </summary>
        [JsonPropertyName("url")]
        public string Url { get; set; }

        /// <summary>
        /// The shortened URL
        /// </summary>
        [JsonPropertyName("shortened_url")]
        public string ShortenedUrl { get; set; }

        /// <summary>
        /// The title of the link
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; }

        /// <summary>
        /// The description of the link
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; }

        /// <summary>
        /// The thumbnail URL for the link
        /// </summary>
        [JsonPropertyName("thumbnail")]
        public string Thumbnail { get; set; }

        /// <summary>
        /// Statistics for this link
        /// </summary>
        [JsonPropertyName("statistics")]
        public BufferLinkStatistics Statistics { get; set; }
    }

    /// <summary>
    /// Represents statistics for a Buffer link
    /// </summary>
    public sealed class BufferLinkStatistics : BufferEntityBase
    {
        /// <summary>
        /// Number of clicks on the link
        /// </summary>
        [JsonPropertyName("clicks")]
        public int Clicks { get; set; }
    }

    /// <summary>
    /// Represents Buffer analytics data
    /// </summary>
    public sealed class BufferAnalytics : BufferEntityBase
    {
        /// <summary>
        /// The profile ID these analytics are for
        /// </summary>
        [JsonPropertyName("profile_id")]
        public string ProfileId { get; set; }

        /// <summary>
        /// The date range for these analytics
        /// </summary>
        [JsonPropertyName("date")]
        public string Date { get; set; }

        /// <summary>
        /// Total impressions
        /// </summary>
        [JsonPropertyName("impressions")]
        public int Impressions { get; set; }

        /// <summary>
        /// Total clicks
        /// </summary>
        [JsonPropertyName("clicks")]
        public int Clicks { get; set; }

        /// <summary>
        /// Total favorites/likes
        /// </summary>
        [JsonPropertyName("favorites")]
        public int Favorites { get; set; }

        /// <summary>
        /// Total shares/retweets
        /// </summary>
        [JsonPropertyName("shares")]
        public int Shares { get; set; }

        /// <summary>
        /// Total mentions
        /// </summary>
        [JsonPropertyName("mentions")]
        public int Mentions { get; set; }
    }

    /// <summary>
    /// Generic response wrapper for Buffer API responses
    /// </summary>
    /// <typeparam name="T">The type of data in the response</typeparam>
    public sealed class BufferResponse<T> : BufferEntityBase
    {
        /// <summary>
        /// The list of items in the response
        /// </summary>
        [JsonPropertyName("data")]
        public List<T> Data { get; set; }

        /// <summary>
        /// Pagination information
        /// </summary>
        [JsonPropertyName("pagination")]
        public BufferPagination Pagination { get; set; }
    }

    /// <summary>
    /// Represents pagination information from Buffer API
    /// </summary>
    public sealed class BufferPagination : BufferEntityBase
    {
        /// <summary>
        /// The total number of items
        /// </summary>
        [JsonPropertyName("total")]
        public int Total { get; set; }

        /// <summary>
        /// The current page number
        /// </summary>
        [JsonPropertyName("page")]
        public int Page { get; set; }

        /// <summary>
        /// The number of items per page
        /// </summary>
        [JsonPropertyName("per_page")]
        public int PerPage { get; set; }

        /// <summary>
        /// The URL for the next page
        /// </summary>
        [JsonPropertyName("next")]
        public string Next { get; set; }

        /// <summary>
        /// The URL for the previous page
        /// </summary>
        [JsonPropertyName("previous")]
        public string Previous { get; set; }
    }
}