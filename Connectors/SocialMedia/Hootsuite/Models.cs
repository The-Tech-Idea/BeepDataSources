using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.HootsuiteDataSource
{
    /// <summary>
    /// Base class for Hootsuite entities
    /// </summary>
    public abstract class HootsuiteEntityBase : Entity
    {
        /// <summary>
        /// Unique identifier for the entity
        /// </summary>
        [JsonIgnore]
        public IDataSource DataSource { get; set; }

        public T Attach<T>(IDataSource dataSource) where T : HootsuiteEntityBase
        {
            DataSource = dataSource;
            return (T)this;
        }
    }

    /// <summary>
    /// Represents a post/message in Hootsuite
    /// </summary>
    public sealed class HootsuitePost : HootsuiteEntityBase
    {
        /// <summary>
        /// The unique identifier for this post
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// The text content of the post
        /// </summary>
        [JsonPropertyName("text")]
        public string Text { get; set; }

        /// <summary>
        /// ID of the social profile this post belongs to
        /// </summary>
        [JsonPropertyName("socialProfileId")]
        public string SocialProfileId { get; set; }

        /// <summary>
        /// ID of the social network
        /// </summary>
        [JsonPropertyName("socialNetworkId")]
        public string SocialNetworkId { get; set; }

        /// <summary>
        /// Name of the social network
        /// </summary>
        [JsonPropertyName("socialNetworkName")]
        public string SocialNetworkName { get; set; }

        /// <summary>
        /// Current state of the post (DRAFT, SCHEDULED, PUBLISHED, etc.)
        /// </summary>
        [JsonPropertyName("state")]
        public string State { get; set; }

        /// <summary>
        /// Scheduled send time for the post
        /// </summary>
        [JsonPropertyName("scheduledSendTime")]
        public DateTime? ScheduledSendTime { get; set; }

        /// <summary>
        /// URLs of media attachments
        /// </summary>
        [JsonPropertyName("mediaUrls")]
        public List<string> MediaUrls { get; set; }

        /// <summary>
        /// Web links included in the post
        /// </summary>
        [JsonPropertyName("webLinks")]
        public List<HootsuiteWebLink> WebLinks { get; set; }

        /// <summary>
        /// Performance statistics for the post
        /// </summary>
        [JsonPropertyName("statistics")]
        public HootsuitePostStatistics Statistics { get; set; }

        /// <summary>
        /// Creation timestamp
        /// </summary>
        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Last update timestamp
        /// </summary>
        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Represents a web link in a Hootsuite post
    /// </summary>
    public sealed class HootsuiteWebLink : HootsuiteEntityBase
    {
        /// <summary>
        /// The URL of the link
        /// </summary>
        [JsonPropertyName("url")]
        public string Url { get; set; }

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
        [JsonPropertyName("thumbnailUrl")]
        public string ThumbnailUrl { get; set; }
    }

    /// <summary>
    /// Represents performance statistics for a Hootsuite post
    /// </summary>
    public sealed class HootsuitePostStatistics : HootsuiteEntityBase
    {
        /// <summary>
        /// Number of clicks
        /// </summary>
        [JsonPropertyName("clicks")]
        public int Clicks { get; set; }

        /// <summary>
        /// Number of likes/favorites
        /// </summary>
        [JsonPropertyName("likes")]
        public int Likes { get; set; }

        /// <summary>
        /// Number of shares/retweets
        /// </summary>
        [JsonPropertyName("shares")]
        public int Shares { get; set; }

        /// <summary>
        /// Number of comments
        /// </summary>
        [JsonPropertyName("comments")]
        public int Comments { get; set; }

        /// <summary>
        /// Number of impressions/views
        /// </summary>
        [JsonPropertyName("impressions")]
        public int Impressions { get; set; }
    }

    /// <summary>
    /// Represents a social media profile in Hootsuite
    /// </summary>
    public sealed class HootsuiteSocialProfile : HootsuiteEntityBase
    {
        /// <summary>
        /// The unique identifier for this social profile
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// The type of social network
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; }

        /// <summary>
        /// The username/handle for this profile
        /// </summary>
        [JsonPropertyName("username")]
        public string Username { get; set; }

        /// <summary>
        /// The display name for this profile
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// The avatar/profile picture URL
        /// </summary>
        [JsonPropertyName("avatarUrl")]
        public string AvatarUrl { get; set; }

        /// <summary>
        /// The social network ID
        /// </summary>
        [JsonPropertyName("socialNetworkId")]
        public string SocialNetworkId { get; set; }

        /// <summary>
        /// The social network name
        /// </summary>
        [JsonPropertyName("socialNetworkName")]
        public string SocialNetworkName { get; set; }

        /// <summary>
        /// Whether this profile is active
        /// </summary>
        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }

        /// <summary>
        /// Profile statistics
        /// </summary>
        [JsonPropertyName("statistics")]
        public HootsuiteProfileStatistics Statistics { get; set; }
    }

    /// <summary>
    /// Represents statistics for a Hootsuite social profile
    /// </summary>
    public sealed class HootsuiteProfileStatistics : HootsuiteEntityBase
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
    /// Represents an organization in Hootsuite
    /// </summary>
    public sealed class HootsuiteOrganization : HootsuiteEntityBase
    {
        /// <summary>
        /// The unique identifier for this organization
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// The name of the organization
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// The type of organization
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; }

        /// <summary>
        /// The timezone of the organization
        /// </summary>
        [JsonPropertyName("timezone")]
        public string Timezone { get; set; }

        /// <summary>
        /// The currency used by the organization
        /// </summary>
        [JsonPropertyName("currency")]
        public string Currency { get; set; }
    }

    /// <summary>
    /// Represents a team in Hootsuite
    /// </summary>
    public sealed class HootsuiteTeam : HootsuiteEntityBase
    {
        /// <summary>
        /// The unique identifier for this team
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// The name of the team
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// The description of the team
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; }

        /// <summary>
        /// The organization ID this team belongs to
        /// </summary>
        [JsonPropertyName("organizationId")]
        public string OrganizationId { get; set; }

        /// <summary>
        /// The number of members in this team
        /// </summary>
        [JsonPropertyName("memberCount")]
        public int MemberCount { get; set; }
    }

    /// <summary>
    /// Represents analytics data in Hootsuite
    /// </summary>
    public sealed class HootsuiteAnalytics : HootsuiteEntityBase
    {
        /// <summary>
        /// The social profile ID these analytics are for
        /// </summary>
        [JsonPropertyName("socialProfileId")]
        public string SocialProfileId { get; set; }

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
        /// Total engagements
        /// </summary>
        [JsonPropertyName("engagements")]
        public int Engagements { get; set; }

        /// <summary>
        /// Total shares
        /// </summary>
        [JsonPropertyName("shares")]
        public int Shares { get; set; }

        /// <summary>
        /// Total comments
        /// </summary>
        [JsonPropertyName("comments")]
        public int Comments { get; set; }
    }

    /// <summary>
    /// Generic response wrapper for Hootsuite API responses
    /// </summary>
    /// <typeparam name="T">The type of data in the response</typeparam>
    public sealed class HootsuiteResponse<T> : HootsuiteEntityBase
    {
        /// <summary>
        /// The list of items in the response
        /// </summary>
        [JsonPropertyName("data")]
        public List<T> Data { get; set; }

        /// <summary>
        /// Pagination information
        /// </summary>
        [JsonPropertyName("meta")]
        public HootsuitePagination Pagination { get; set; }
    }

    /// <summary>
    /// Represents pagination information from Hootsuite API
    /// </summary>
    public sealed class HootsuitePagination : HootsuiteEntityBase
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
        [JsonPropertyName("limit")]
        public int Limit { get; set; }

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
