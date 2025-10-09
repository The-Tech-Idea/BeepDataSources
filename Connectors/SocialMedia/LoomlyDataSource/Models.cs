using System.Text.Json.Serialization;

namespace TheTechIdea.Beep.Connectors.Loomly.Models
{
    // Loomly API Models
    public class LoomlyPost
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("post_type")]
        public string PostType { get; set; }

        [JsonPropertyName("scheduled_at")]
        public DateTime? ScheduledAt { get; set; }

        [JsonPropertyName("published_at")]
        public DateTime? PublishedAt { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("social_accounts")]
        public List<LoomlySocialAccount> SocialAccounts { get; set; }

        [JsonPropertyName("media")]
        public List<LoomlyMedia> Media { get; set; }

        [JsonPropertyName("hashtags")]
        public List<string> Hashtags { get; set; }

        [JsonPropertyName("mentions")]
        public List<string> Mentions { get; set; }

        [JsonPropertyName("links")]
        public List<LoomlyLink> Links { get; set; }

        [JsonPropertyName("campaign")]
        public LoomlyCampaign Campaign { get; set; }

        [JsonPropertyName("calendar")]
        public LoomlyCalendar Calendar { get; set; }

        [JsonPropertyName("creator")]
        public LoomlyUser Creator { get; set; }

        [JsonPropertyName("approver")]
        public LoomlyUser Approver { get; set; }

        [JsonPropertyName("performance")]
        public LoomlyPerformance Performance { get; set; }
    }

    public class LoomlySocialAccount
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("platform")]
        public string Platform { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; }

        [JsonPropertyName("profile_picture")]
        public string ProfilePicture { get; set; }

        [JsonPropertyName("is_connected")]
        public bool IsConnected { get; set; }

        [JsonPropertyName("permissions")]
        public List<string> Permissions { get; set; }
    }

    public class LoomlyMedia
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("thumbnail_url")]
        public string ThumbnailUrl { get; set; }

        [JsonPropertyName("alt_text")]
        public string AltText { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }
    }

    public class LoomlyLink
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("image")]
        public string Image { get; set; }
    }

    public class LoomlyCampaign
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("start_date")]
        public DateTime? StartDate { get; set; }

        [JsonPropertyName("end_date")]
        public DateTime? EndDate { get; set; }

        [JsonPropertyName("budget")]
        public decimal Budget { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        [JsonPropertyName("goals")]
        public List<string> Goals { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    public class LoomlyCalendar
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("timezone")]
        public string Timezone { get; set; }

        [JsonPropertyName("color")]
        public string Color { get; set; }

        [JsonPropertyName("is_default")]
        public bool IsDefault { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    public class LoomlyUser
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("first_name")]
        public string FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string LastName { get; set; }

        [JsonPropertyName("avatar")]
        public string Avatar { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; }

        [JsonPropertyName("last_login")]
        public DateTime? LastLogin { get; set; }
    }

    public class LoomlyPerformance
    {
        [JsonPropertyName("impressions")]
        public int Impressions { get; set; }

        [JsonPropertyName("reach")]
        public int Reach { get; set; }

        [JsonPropertyName("engagement")]
        public int Engagement { get; set; }

        [JsonPropertyName("clicks")]
        public int Clicks { get; set; }

        [JsonPropertyName("likes")]
        public int Likes { get; set; }

        [JsonPropertyName("comments")]
        public int Comments { get; set; }

        [JsonPropertyName("shares")]
        public int Shares { get; set; }

        [JsonPropertyName("saves")]
        public int Saves { get; set; }

        [JsonPropertyName("engagement_rate")]
        public decimal EngagementRate { get; set; }

        [JsonPropertyName("click_through_rate")]
        public decimal ClickThroughRate { get; set; }
    }

    public class LoomlyComment
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("post_id")]
        public string PostId { get; set; }

        [JsonPropertyName("social_account_id")]
        public string SocialAccountId { get; set; }

        [JsonPropertyName("author")]
        public LoomlyCommentAuthor Author { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("likes")]
        public int Likes { get; set; }

        [JsonPropertyName("replies")]
        public int Replies { get; set; }

        [JsonPropertyName("is_reply")]
        public bool IsReply { get; set; }

        [JsonPropertyName("parent_comment_id")]
        public string ParentCommentId { get; set; }
    }

    public class LoomlyCommentAuthor
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; }

        [JsonPropertyName("avatar")]
        public string Avatar { get; set; }

        [JsonPropertyName("is_verified")]
        public bool IsVerified { get; set; }
    }

    public class LoomlyHashtag
    {
        [JsonPropertyName("tag")]
        public string Tag { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("last_used")]
        public DateTime LastUsed { get; set; }

        [JsonPropertyName("performance")]
        public LoomlyHashtagPerformance Performance { get; set; }
    }

    public class LoomlyHashtagPerformance
    {
        [JsonPropertyName("avg_impressions")]
        public int AvgImpressions { get; set; }

        [JsonPropertyName("avg_engagement")]
        public int AvgEngagement { get; set; }

        [JsonPropertyName("avg_engagement_rate")]
        public decimal AvgEngagementRate { get; set; }
    }

    public class LoomlyAnalytics
    {
        [JsonPropertyName("period")]
        public string Period { get; set; }

        [JsonPropertyName("start_date")]
        public DateTime StartDate { get; set; }

        [JsonPropertyName("end_date")]
        public DateTime EndDate { get; set; }

        [JsonPropertyName("total_posts")]
        public int TotalPosts { get; set; }

        [JsonPropertyName("published_posts")]
        public int PublishedPosts { get; set; }

        [JsonPropertyName("scheduled_posts")]
        public int ScheduledPosts { get; set; }

        [JsonPropertyName("draft_posts")]
        public int DraftPosts { get; set; }

        [JsonPropertyName("total_impressions")]
        public long TotalImpressions { get; set; }

        [JsonPropertyName("total_engagement")]
        public long TotalEngagement { get; set; }

        [JsonPropertyName("avg_engagement_rate")]
        public decimal AvgEngagementRate { get; set; }

        [JsonPropertyName("top_performing_posts")]
        public List<LoomlyPost> TopPerformingPosts { get; set; }

        [JsonPropertyName("platform_breakdown")]
        public Dictionary<string, LoomlyPlatformAnalytics> PlatformBreakdown { get; set; }
    }

    public class LoomlyPlatformAnalytics
    {
        [JsonPropertyName("posts")]
        public int Posts { get; set; }

        [JsonPropertyName("impressions")]
        public long Impressions { get; set; }

        [JsonPropertyName("engagement")]
        public long Engagement { get; set; }

        [JsonPropertyName("engagement_rate")]
        public decimal EngagementRate { get; set; }

        [JsonPropertyName("best_posting_time")]
        public string BestPostingTime { get; set; }
    }

    public class LoomlyWorkflow
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("steps")]
        public List<LoomlyWorkflowStep> Steps { get; set; }

        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    public class LoomlyWorkflowStep
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("assignees")]
        public List<LoomlyUser> Assignees { get; set; }

        [JsonPropertyName("due_date")]
        public DateTime? DueDate { get; set; }

        [JsonPropertyName("is_required")]
        public bool IsRequired { get; set; }

        [JsonPropertyName("order")]
        public int Order { get; set; }
    }

    public class LoomlyPaginationResponse<T>
    {
        [JsonPropertyName("data")]
        public List<T> Data { get; set; }

        [JsonPropertyName("current_page")]
        public int CurrentPage { get; set; }

        [JsonPropertyName("last_page")]
        public int LastPage { get; set; }

        [JsonPropertyName("per_page")]
        public int PerPage { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("from")]
        public int From { get; set; }

        [JsonPropertyName("to")]
        public int To { get; set; }
    }

    public class LoomlyApiResponse<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("data")]
        public T Data { get; set; }

        [JsonPropertyName("errors")]
        public List<string> Errors { get; set; }
    }
}