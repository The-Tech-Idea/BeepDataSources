using System.Text.Json.Serialization;

namespace TheTechIdea.Beep.Connectors.Fathom.Models
{
    // Fathom API Models
    public class FathomVideo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("duration")]
        public int Duration { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("thumbnail_url")]
        public string ThumbnailUrl { get; set; }

        [JsonPropertyName("video_url")]
        public string VideoUrl { get; set; }

        [JsonPropertyName("download_url")]
        public string DownloadUrl { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }

        [JsonPropertyName("format")]
        public string Format { get; set; }

        [JsonPropertyName("resolution")]
        public string Resolution { get; set; }

        [JsonPropertyName("frame_rate")]
        public decimal FrameRate { get; set; }

        [JsonPropertyName("bitrate")]
        public int Bitrate { get; set; }

        [JsonPropertyName("owner")]
        public FathomUser Owner { get; set; }

        [JsonPropertyName("folder")]
        public FathomFolder Folder { get; set; }

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; }

        [JsonPropertyName("analytics")]
        public FathomAnalytics Analytics { get; set; }
    }

    public class FathomUser
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("first_name")]
        public string FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string LastName { get; set; }

        [JsonPropertyName("avatar_url")]
        public string AvatarUrl { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; }

        [JsonPropertyName("last_login")]
        public DateTime? LastLogin { get; set; }
    }

    public class FathomFolder
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("parent_id")]
        public string ParentId { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    public class FathomAnalytics
    {
        [JsonPropertyName("views")]
        public int Views { get; set; }

        [JsonPropertyName("unique_viewers")]
        public int UniqueViewers { get; set; }

        [JsonPropertyName("total_watch_time")]
        public int TotalWatchTime { get; set; }

        [JsonPropertyName("average_watch_time")]
        public decimal AverageWatchTime { get; set; }

        [JsonPropertyName("completion_rate")]
        public decimal CompletionRate { get; set; }

        [JsonPropertyName("engagement_score")]
        public decimal EngagementScore { get; set; }

        [JsonPropertyName("peak_concurrent_viewers")]
        public int PeakConcurrentViewers { get; set; }

        [JsonPropertyName("viewership_trend")]
        public List<FathomViewershipData> ViewershipTrend { get; set; }

        [JsonPropertyName("geographic_data")]
        public List<FathomGeographicData> GeographicData { get; set; }

        [JsonPropertyName("device_data")]
        public List<FathomDeviceData> DeviceData { get; set; }

        [JsonPropertyName("referrer_data")]
        public List<FathomReferrerData> ReferrerData { get; set; }
    }

    public class FathomViewershipData
    {
        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("views")]
        public int Views { get; set; }

        [JsonPropertyName("unique_viewers")]
        public int UniqueViewers { get; set; }

        [JsonPropertyName("watch_time")]
        public int WatchTime { get; set; }
    }

    public class FathomGeographicData
    {
        [JsonPropertyName("country")]
        public string Country { get; set; }

        [JsonPropertyName("region")]
        public string Region { get; set; }

        [JsonPropertyName("city")]
        public string City { get; set; }

        [JsonPropertyName("views")]
        public int Views { get; set; }

        [JsonPropertyName("percentage")]
        public decimal Percentage { get; set; }
    }

    public class FathomDeviceData
    {
        [JsonPropertyName("device_type")]
        public string DeviceType { get; set; }

        [JsonPropertyName("operating_system")]
        public string OperatingSystem { get; set; }

        [JsonPropertyName("browser")]
        public string Browser { get; set; }

        [JsonPropertyName("views")]
        public int Views { get; set; }

        [JsonPropertyName("percentage")]
        public decimal Percentage { get; set; }
    }

    public class FathomReferrerData
    {
        [JsonPropertyName("referrer")]
        public string Referrer { get; set; }

        [JsonPropertyName("views")]
        public int Views { get; set; }

        [JsonPropertyName("percentage")]
        public decimal Percentage { get; set; }
    }

    public class FathomInsight
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("video_id")]
        public string VideoId { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("timestamp")]
        public int Timestamp { get; set; }

        [JsonPropertyName("duration")]
        public int Duration { get; set; }

        [JsonPropertyName("confidence")]
        public decimal Confidence { get; set; }

        [JsonPropertyName("thumbnail_url")]
        public string ThumbnailUrl { get; set; }

        [JsonPropertyName("data")]
        public Dictionary<string, object> Data { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    public class FathomChapter
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("video_id")]
        public string VideoId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("start_time")]
        public int StartTime { get; set; }

        [JsonPropertyName("end_time")]
        public int EndTime { get; set; }

        [JsonPropertyName("duration")]
        public int Duration { get; set; }

        [JsonPropertyName("thumbnail_url")]
        public string ThumbnailUrl { get; set; }

        [JsonPropertyName("summary")]
        public string Summary { get; set; }

        [JsonPropertyName("key_points")]
        public List<string> KeyPoints { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    public class FathomTranscript
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("video_id")]
        public string VideoId { get; set; }

        [JsonPropertyName("language")]
        public string Language { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("segments")]
        public List<FathomTranscriptSegment> Segments { get; set; }

        [JsonPropertyName("speakers")]
        public List<FathomSpeaker> Speakers { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    public class FathomTranscriptSegment
    {
        [JsonPropertyName("start_time")]
        public decimal StartTime { get; set; }

        [JsonPropertyName("end_time")]
        public decimal EndTime { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("speaker_id")]
        public string SpeakerId { get; set; }

        [JsonPropertyName("confidence")]
        public decimal Confidence { get; set; }
    }

    public class FathomSpeaker
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("is_host")]
        public bool IsHost { get; set; }

        [JsonPropertyName("total_speaking_time")]
        public int TotalSpeakingTime { get; set; }

        [JsonPropertyName("word_count")]
        public int WordCount { get; set; }
    }

    public class FathomSummary
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("video_id")]
        public string VideoId { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("key_points")]
        public List<string> KeyPoints { get; set; }

        [JsonPropertyName("action_items")]
        public List<string> ActionItems { get; set; }

        [JsonPropertyName("decisions")]
        public List<string> Decisions { get; set; }

        [JsonPropertyName("questions")]
        public List<string> Questions { get; set; }

        [JsonPropertyName("sentiment")]
        public string Sentiment { get; set; }

        [JsonPropertyName("confidence")]
        public decimal Confidence { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    public class FathomComment
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("video_id")]
        public string VideoId { get; set; }

        [JsonPropertyName("user_id")]
        public string UserId { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("timestamp")]
        public int Timestamp { get; set; }

        [JsonPropertyName("parent_id")]
        public string ParentId { get; set; }

        [JsonPropertyName("replies")]
        public List<FathomComment> Replies { get; set; }

        [JsonPropertyName("likes")]
        public int Likes { get; set; }

        [JsonPropertyName("is_resolved")]
        public bool IsResolved { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    public class FathomShare
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("video_id")]
        public string VideoId { get; set; }

        [JsonPropertyName("share_type")]
        public string ShareType { get; set; }

        [JsonPropertyName("recipient_email")]
        public string RecipientEmail { get; set; }

        [JsonPropertyName("recipient_name")]
        public string RecipientName { get; set; }

        [JsonPropertyName("permissions")]
        public List<string> Permissions { get; set; }

        [JsonPropertyName("expires_at")]
        public DateTime? ExpiresAt { get; set; }

        [JsonPropertyName("view_count")]
        public int ViewCount { get; set; }

        [JsonPropertyName("last_viewed_at")]
        public DateTime? LastViewedAt { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    public class FathomTeam
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("owner")]
        public FathomUser Owner { get; set; }

        [JsonPropertyName("members")]
        public List<FathomUser> Members { get; set; }

        [JsonPropertyName("settings")]
        public FathomTeamSettings Settings { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    public class FathomTeamSettings
    {
        [JsonPropertyName("default_folder_id")]
        public string DefaultFolderId { get; set; }

        [JsonPropertyName("auto_transcribe")]
        public bool AutoTranscribe { get; set; }

        [JsonPropertyName("auto_summarize")]
        public bool AutoSummarize { get; set; }

        [JsonPropertyName("auto_chapter")]
        public bool AutoChapter { get; set; }

        [JsonPropertyName("default_sharing_permissions")]
        public List<string> DefaultSharingPermissions { get; set; }

        [JsonPropertyName("branding_enabled")]
        public bool BrandingEnabled { get; set; }
    }

    public class FathomPaginationResponse<T>
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

    public class FathomApiResponse<T>
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

    public class FathomUploadResponse
    {
        [JsonPropertyName("upload_url")]
        public string UploadUrl { get; set; }

        [JsonPropertyName("video_id")]
        public string VideoId { get; set; }

        [JsonPropertyName("fields")]
        public Dictionary<string, string> Fields { get; set; }
    }
}