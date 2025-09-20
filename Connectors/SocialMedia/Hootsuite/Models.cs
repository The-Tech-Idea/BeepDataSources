using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TheTechIdea.Beep.HootsuiteDataSource
{
    /// <summary>
    /// Base class for all Hootsuite entities
    /// </summary>
    public abstract class HootsuiteEntity
    {
        /// <summary>
        /// Unique identifier for the entity
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

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
    /// Represents a post/message in Hootsuite
    /// </summary>
    public class HootsuitePost : HootsuiteEntity
    {
        /// <summary>
        /// The text content of the post
        /// </summary>
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        /// <summary>
        /// ID of the social profile this post belongs to
        /// </summary>
        [JsonPropertyName("socialProfileId")]
        public string? SocialProfileId { get; set; }

        /// <summary>
        /// ID of the social network
        /// </summary>
        [JsonPropertyName("socialNetworkId")]
        public string? SocialNetworkId { get; set; }

        /// <summary>
        /// Name of the social network
        /// </summary>
        [JsonPropertyName("socialNetworkName")]
        public string? SocialNetworkName { get; set; }

        /// <summary>
        /// Current state of the post (DRAFT, SCHEDULED, PUBLISHED, etc.)
        /// </summary>
        [JsonPropertyName("state")]
        public string? State { get; set; }

        /// <summary>
        /// Scheduled send time for the post
        /// </summary>
        [JsonPropertyName("scheduledSendTime")]
        public DateTime? ScheduledSendTime { get; set; }

        /// <summary>
        /// URLs of media attachments
        /// </summary>
        [JsonPropertyName("mediaUrls")]
        public List<string>? MediaUrls { get; set; }

        /// <summary>
        /// Web links included in the post
        /// </summary>
        [JsonPropertyName("webLinks")]
        public List<HootsuiteWebLink>? WebLinks { get; set; }

        /// <summary>
        /// Performance statistics for the post
        /// </summary>
        [JsonPropertyName("statistics")]
        public HootsuitePostStatistics? Statistics { get; set; }

        /// <summary>
        /// Whether this is a draft post
        /// </summary>
        [JsonPropertyName("isDraft")]
        public bool? IsDraft { get; set; }

        /// <summary>
        /// Whether this post has been published
        /// </summary>
        [JsonPropertyName("isPublished")]
        public bool? IsPublished { get; set; }

        /// <summary>
        /// Tags associated with the post
        /// </summary>
        [JsonPropertyName("tags")]
        public List<string>? Tags { get; set; }

        /// <summary>
        /// Review state of the post
        /// </summary>
        [JsonPropertyName("reviewState")]
        public string? ReviewState { get; set; }
    }

    /// <summary>
    /// Web link information for Hootsuite posts
    /// </summary>
    public class HootsuiteWebLink
    {
        /// <summary>
        /// The URL of the link
        /// </summary>
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        /// <summary>
        /// Title of the linked page
        /// </summary>
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        /// <summary>
        /// Description of the linked page
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Thumbnail URL for the link
        /// </summary>
        [JsonPropertyName("thumbnailUrl")]
        public string? ThumbnailUrl { get; set; }
    }

    /// <summary>
    /// Statistics for Hootsuite posts
    /// </summary>
    public class HootsuitePostStatistics
    {
        /// <summary>
        /// Number of impressions/views
        /// </summary>
        [JsonPropertyName("impressions")]
        public long? Impressions { get; set; }

        /// <summary>
        /// Number of clicks
        /// </summary>
        [JsonPropertyName("clicks")]
        public long? Clicks { get; set; }

        /// <summary>
        /// Number of likes/favorites
        /// </summary>
        [JsonPropertyName("likes")]
        public long? Likes { get; set; }

        /// <summary>
        /// Number of shares/retweets
        /// </summary>
        [JsonPropertyName("shares")]
        public long? Shares { get; set; }

        /// <summary>
        /// Number of comments
        /// </summary>
        [JsonPropertyName("comments")]
        public long? Comments { get; set; }

        /// <summary>
        /// Engagement rate as percentage
        /// </summary>
        [JsonPropertyName("engagementRate")]
        public decimal? EngagementRate { get; set; }
    }

    /// <summary>
    /// Represents a social media profile in Hootsuite
    /// </summary>
    public class HootsuiteSocialProfile : HootsuiteEntity
    {
        /// <summary>
        /// ID of the social network this profile belongs to
        /// </summary>
        [JsonPropertyName("socialNetworkId")]
        public string? SocialNetworkId { get; set; }

        /// <summary>
        /// Name of the social network
        /// </summary>
        [JsonPropertyName("socialNetworkName")]
        public string? SocialNetworkName { get; set; }

        /// <summary>
        /// Username/handle on the social network
        /// </summary>
        [JsonPropertyName("socialNetworkUsername")]
        public string? SocialNetworkUsername { get; set; }

        /// <summary>
        /// Display name on the social network
        /// </summary>
        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        /// <summary>
        /// URL to the profile's avatar image
        /// </summary>
        [JsonPropertyName("avatarUrl")]
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// URL to the social profile
        /// </summary>
        [JsonPropertyName("profileUrl")]
        public string? ProfileUrl { get; set; }

        /// <summary>
        /// Timezone of the profile
        /// </summary>
        [JsonPropertyName("timezone")]
        public string? Timezone { get; set; }

        /// <summary>
        /// Whether this profile is active
        /// </summary>
        [JsonPropertyName("isActive")]
        public bool? IsActive { get; set; }

        /// <summary>
        /// Whether this is a business account
        /// </summary>
        [JsonPropertyName("isBusinessAccount")]
        public bool? IsBusinessAccount { get; set; }

        /// <summary>
        /// Type of the profile (PERSONAL, BUSINESS, etc.)
        /// </summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>
        /// Follower count
        /// </summary>
        [JsonPropertyName("followerCount")]
        public long? FollowerCount { get; set; }

        /// <summary>
        /// Following count
        /// </summary>
        [JsonPropertyName("followingCount")]
        public long? FollowingCount { get; set; }

        /// <summary>
        /// Organization this profile belongs to
        /// </summary>
        [JsonPropertyName("organization")]
        public HootsuiteOrganization? Organization { get; set; }
    }

    /// <summary>
    /// Analytics data for Hootsuite profiles
    /// </summary>
    public class HootsuiteAnalytics : HootsuiteEntity
    {
        /// <summary>
        /// ID of the social profile these analytics belong to
        /// </summary>
        [JsonPropertyName("socialProfileId")]
        public string? SocialProfileId { get; set; }

        /// <summary>
        /// Date for these analytics
        /// </summary>
        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }

        /// <summary>
        /// Number of impressions/views
        /// </summary>
        [JsonPropertyName("impressions")]
        public long? Impressions { get; set; }

        /// <summary>
        /// Number of clicks
        /// </summary>
        [JsonPropertyName("clicks")]
        public long? Clicks { get; set; }

        /// <summary>
        /// Number of likes/favorites
        /// </summary>
        [JsonPropertyName("likes")]
        public long? Likes { get; set; }

        /// <summary>
        /// Number of shares/retweets
        /// </summary>
        [JsonPropertyName("shares")]
        public long? Shares { get; set; }

        /// <summary>
        /// Number of comments
        /// </summary>
        [JsonPropertyName("comments")]
        public long? Comments { get; set; }

        /// <summary>
        /// Total reach
        /// </summary>
        [JsonPropertyName("reach")]
        public long? Reach { get; set; }

        /// <summary>
        /// Total engagement
        /// </summary>
        [JsonPropertyName("engagement")]
        public long? Engagement { get; set; }

        /// <summary>
        /// Engagement rate as percentage
        /// </summary>
        [JsonPropertyName("engagementRate")]
        public decimal? EngagementRate { get; set; }

        /// <summary>
        /// Follower count at this date
        /// </summary>
        [JsonPropertyName("followerCount")]
        public long? FollowerCount { get; set; }

        /// <summary>
        /// Following count at this date
        /// </summary>
        [JsonPropertyName("followingCount")]
        public long? FollowingCount { get; set; }

        /// <summary>
        /// Top performing posts for this period
        /// </summary>
        [JsonPropertyName("topPosts")]
        public List<HootsuitePostAnalytics>? TopPosts { get; set; }
    }

    /// <summary>
    /// Analytics data for individual posts
    /// </summary>
    public class HootsuitePostAnalytics
    {
        /// <summary>
        /// ID of the post
        /// </summary>
        [JsonPropertyName("postId")]
        public string? PostId { get; set; }

        /// <summary>
        /// Text of the post
        /// </summary>
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        /// <summary>
        /// Impressions for this post
        /// </summary>
        [JsonPropertyName("impressions")]
        public long? Impressions { get; set; }

        /// <summary>
        /// Engagement for this post
        /// </summary>
        [JsonPropertyName("engagement")]
        public long? Engagement { get; set; }

        /// <summary>
        /// Engagement rate for this post
        /// </summary>
        [JsonPropertyName("engagementRate")]
        public decimal? EngagementRate { get; set; }
    }

    /// <summary>
    /// Represents an organization in Hootsuite
    /// </summary>
    public class HootsuiteOrganization : HootsuiteEntity
    {
        /// <summary>
        /// Name of the organization
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Company name
        /// </summary>
        [JsonPropertyName("companyName")]
        public string? CompanyName { get; set; }

        /// <summary>
        /// Website URL
        /// </summary>
        [JsonPropertyName("website")]
        public string? Website { get; set; }

        /// <summary>
        /// Timezone of the organization
        /// </summary>
        [JsonPropertyName("timezone")]
        public string? Timezone { get; set; }

        /// <summary>
        /// Country of the organization
        /// </summary>
        [JsonPropertyName("country")]
        public string? Country { get; set; }

        /// <summary>
        /// City of the organization
        /// </summary>
        [JsonPropertyName("city")]
        public string? City { get; set; }

        /// <summary>
        /// Industry of the organization
        /// </summary>
        [JsonPropertyName("industry")]
        public string? Industry { get; set; }

        /// <summary>
        /// Size of the organization
        /// </summary>
        [JsonPropertyName("size")]
        public string? Size { get; set; }

        /// <summary>
        /// Contact email
        /// </summary>
        [JsonPropertyName("email")]
        public string? Email { get; set; }

        /// <summary>
        /// Contact phone number
        /// </summary>
        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        /// <summary>
        /// Teams within this organization
        /// </summary>
        [JsonPropertyName("teams")]
        public List<HootsuiteTeam>? Teams { get; set; }

        /// <summary>
        /// Social profiles owned by this organization
        /// </summary>
        [JsonPropertyName("socialProfiles")]
        public List<HootsuiteSocialProfile>? SocialProfiles { get; set; }
    }

    /// <summary>
    /// Represents a team in Hootsuite
    /// </summary>
    public class HootsuiteTeam : HootsuiteEntity
    {
        /// <summary>
        /// Name of the team
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// ID of the organization this team belongs to
        /// </summary>
        [JsonPropertyName("organizationId")]
        public string? OrganizationId { get; set; }

        /// <summary>
        /// Description of the team
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Whether this team is active
        /// </summary>
        [JsonPropertyName("isActive")]
        public bool? IsActive { get; set; }

        /// <summary>
        /// Role of the team (ADMIN, MEMBER, etc.)
        /// </summary>
        [JsonPropertyName("role")]
        public string? Role { get; set; }

        /// <summary>
        /// Permissions for this team
        /// </summary>
        [JsonPropertyName("permissions")]
        public List<string>? Permissions { get; set; }

        /// <summary>
        /// Members of this team
        /// </summary>
        [JsonPropertyName("members")]
        public List<HootsuiteTeamMember>? Members { get; set; }

        /// <summary>
        /// Social profiles assigned to this team
        /// </summary>
        [JsonPropertyName("socialProfiles")]
        public List<HootsuiteSocialProfile>? SocialProfiles { get; set; }
    }

    /// <summary>
    /// Represents a team member in Hootsuite
    /// </summary>
    public class HootsuiteTeamMember
    {
        /// <summary>
        /// User ID
        /// </summary>
        [JsonPropertyName("userId")]
        public string? UserId { get; set; }

        /// <summary>
        /// User's full name
        /// </summary>
        [JsonPropertyName("fullName")]
        public string? FullName { get; set; }

        /// <summary>
        /// User's email address
        /// </summary>
        [JsonPropertyName("email")]
        public string? Email { get; set; }

        /// <summary>
        /// User's role in the team
        /// </summary>
        [JsonPropertyName("role")]
        public string? Role { get; set; }

        /// <summary>
        /// Whether the user is active
        /// </summary>
        [JsonPropertyName("isActive")]
        public bool? IsActive { get; set; }

        /// <summary>
        /// When the user joined the team
        /// </summary>
        [JsonPropertyName("joinedAt")]
        public DateTime? JoinedAt { get; set; }
    }

    /// <summary>
    /// Represents a user in Hootsuite
    /// </summary>
    public class HootsuiteUser : HootsuiteEntity
    {
        /// <summary>
        /// User's full name
        /// </summary>
        [JsonPropertyName("fullName")]
        public string? FullName { get; set; }

        /// <summary>
        /// User's email address
        /// </summary>
        [JsonPropertyName("email")]
        public string? Email { get; set; }

        /// <summary>
        /// User's language preference
        /// </summary>
        [JsonPropertyName("language")]
        public string? Language { get; set; }

        /// <summary>
        /// User's timezone
        /// </summary>
        [JsonPropertyName("timezone")]
        public string? Timezone { get; set; }

        /// <summary>
        /// User's avatar URL
        /// </summary>
        [JsonPropertyName("avatarUrl")]
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// Whether the user is active
        /// </summary>
        [JsonPropertyName("isActive")]
        public bool? IsActive { get; set; }

        /// <summary>
        /// User's bio/description
        /// </summary>
        [JsonPropertyName("bio")]
        public string? Bio { get; set; }

        /// <summary>
        /// Organizations this user belongs to
        /// </summary>
        [JsonPropertyName("organizations")]
        public List<HootsuiteOrganization>? Organizations { get; set; }

        /// <summary>
        /// Teams this user is a member of
        /// </summary>
        [JsonPropertyName("teams")]
        public List<HootsuiteTeam>? Teams { get; set; }
    }

    /// <summary>
    /// Represents a social network in Hootsuite
    /// </summary>
    public class HootsuiteSocialNetwork : HootsuiteEntity
    {
        /// <summary>
        /// Name of the social network
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Display name of the social network
        /// </summary>
        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        /// <summary>
        /// Whether this social network is active
        /// </summary>
        [JsonPropertyName("isActive")]
        public bool? IsActive { get; set; }

        /// <summary>
        /// URL to the social network's logo
        /// </summary>
        [JsonPropertyName("logoUrl")]
        public string? LogoUrl { get; set; }

        /// <summary>
        /// Character limit for posts on this network
        /// </summary>
        [JsonPropertyName("characterLimit")]
        public int? CharacterLimit { get; set; }

        /// <summary>
        /// Supported media types for this network
        /// </summary>
        [JsonPropertyName("supportedMediaTypes")]
        public List<string>? SupportedMediaTypes { get; set; }

        /// <summary>
        /// Whether this network supports scheduling
        /// </summary>
        [JsonPropertyName("supportsScheduling")]
        public bool? SupportsScheduling { get; set; }

        /// <summary>
        /// Whether this network supports analytics
        /// </summary>
        [JsonPropertyName("supportsAnalytics")]
        public bool? SupportsAnalytics { get; set; }
    }
}