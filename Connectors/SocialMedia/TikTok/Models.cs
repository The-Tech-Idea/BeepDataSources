// File: Connectors/SocialMedia/TikTok/Models.cs
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Connectors.TikTok.Models
{
    // -------------------------------------------------------
    // Base Entity
    // -------------------------------------------------------
    public abstract class TikTokEntityBase
    {
        [JsonIgnore] public IDataSource DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : TikTokEntityBase { DataSource = ds; return (T)this; }
    }

    // -------------------------------------------------------
    // User
    // -------------------------------------------------------
    public sealed class TikTokUser : TikTokEntityBase
    {
        [JsonPropertyName("open_id")] public string OpenId { get; set; }
        [JsonPropertyName("union_id")] public string UnionId { get; set; }
        [JsonPropertyName("avatar_url")] public string AvatarUrl { get; set; }
        [JsonPropertyName("avatar_url_100")] public string AvatarUrl100 { get; set; }
        [JsonPropertyName("avatar_large_url")] public string AvatarLargeUrl { get; set; }
        [JsonPropertyName("display_name")] public string Caption { get; set; }
        [JsonPropertyName("bio_description")] public string BioDescription { get; set; }
        [JsonPropertyName("profile_deep_link")] public string ProfileDeepLink { get; set; }
        [JsonPropertyName("is_verified")] public bool? IsVerified { get; set; }
        [JsonPropertyName("follower_count")] public long? FollowerCount { get; set; }
        [JsonPropertyName("following_count")] public long? FollowingCount { get; set; }
        [JsonPropertyName("likes_count")] public long? LikesCount { get; set; }
        [JsonPropertyName("video_count")] public long? VideoCount { get; set; }
        [JsonPropertyName("username")] public string Username { get; set; }
        [JsonPropertyName("create_time")] public long? CreateTime { get; set; }
        [JsonPropertyName("is_private_account")] public bool? IsPrivateAccount { get; set; }
        [JsonPropertyName("sec_uid")] public string SecUid { get; set; }
        [JsonPropertyName("ftc")] public bool? Ftc { get; set; }
        [JsonPropertyName("tt_seller")] public bool? TtSeller { get; set; }
    }

    // -------------------------------------------------------
    // Video
    // -------------------------------------------------------
    public sealed class TikTokVideo : TikTokEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("create_time")] public long? CreateTime { get; set; }
        [JsonPropertyName("cover_image_url")] public string CoverImageUrl { get; set; }
        [JsonPropertyName("share_url")] public string ShareUrl { get; set; }
        [JsonPropertyName("video_description")] public string VideoDescription { get; set; }
        [JsonPropertyName("duration")] public int? Duration { get; set; }
        [JsonPropertyName("height")] public int? Height { get; set; }
        [JsonPropertyName("width")] public int? Width { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("embed_html")] public string EmbedHtml { get; set; }
        [JsonPropertyName("embed_link")] public string EmbedLink { get; set; }
        [JsonPropertyName("like_count")] public long? LikeCount { get; set; }
        [JsonPropertyName("comment_count")] public long? CommentCount { get; set; }
        [JsonPropertyName("share_count")] public long? ShareCount { get; set; }
        [JsonPropertyName("view_count")] public long? ViewCount { get; set; }
        [JsonPropertyName("play_count")] public long? PlayCount { get; set; }
        [JsonPropertyName("music_id")] public string MusicId { get; set; }
        [JsonPropertyName("region_code")] public string RegionCode { get; set; }
        [JsonPropertyName("hashtag_names")] public List<string> HashtagNames { get; set; } = new();
        [JsonPropertyName("is_ad")] public bool? IsAd { get; set; }
        [JsonPropertyName("is_top")] public bool? IsTop { get; set; }
        [JsonPropertyName("format")] public string Format { get; set; }
        [JsonPropertyName("video_quality")] public string VideoQuality { get; set; }
        [JsonPropertyName("bit_rate")] public List<TikTokBitRate> BitRate { get; set; } = new();
        [JsonPropertyName("play_addr")] public TikTokPlayAddr PlayAddr { get; set; }
        [JsonPropertyName("download_addr")] public TikTokDownloadAddr DownloadAddr { get; set; }
        [JsonPropertyName("share_info")] public TikTokShareInfo ShareInfo { get; set; }
        [JsonPropertyName("music")] public TikTokMusic Music { get; set; }
        [JsonPropertyName("author")] public TikTokUser Author { get; set; }
        [JsonPropertyName("statistics")] public TikTokVideoStatistics Statistics { get; set; }
        [JsonPropertyName("is_stitch_enabled")] public bool? IsStitchEnabled { get; set; }
        [JsonPropertyName("is_duet_enabled")] public bool? IsDuetEnabled { get; set; }
        [JsonPropertyName("is_comment_disabled")] public bool? IsCommentDisabled { get; set; }
        [JsonPropertyName("is_download_enabled")] public bool? IsDownloadEnabled { get; set; }
        [JsonPropertyName("is_private")] public bool? IsPrivate { get; set; }
        [JsonPropertyName("is_live")] public bool? IsLive { get; set; }
        [JsonPropertyName("live_info")] public TikTokLiveInfo LiveInfo { get; set; }
    }

    public sealed class TikTokBitRate
    {
        [JsonPropertyName("play_addr")] public TikTokPlayAddr PlayAddr { get; set; }
        [JsonPropertyName("bit_rate")] public long? BitRateValue { get; set; }
        [JsonPropertyName("quality_type")] public int? QualityType { get; set; }
        [JsonPropertyName("gear_name")] public string GearName { get; set; }
    }

    public sealed class TikTokPlayAddr
    {
        [JsonPropertyName("url_list")] public List<string> UrlList { get; set; } = new();
        [JsonPropertyName("uri")] public string Uri { get; set; }
        [JsonPropertyName("url_key")] public string UrlKey { get; set; }
        [JsonPropertyName("data_size")] public long? DataSize { get; set; }
        [JsonPropertyName("file_hash")] public string FileHash { get; set; }
        [JsonPropertyName("file_cs")] public string FileCs { get; set; }
    }

    public sealed class TikTokDownloadAddr
    {
        [JsonPropertyName("url_list")] public List<string> UrlList { get; set; } = new();
        [JsonPropertyName("uri")] public string Uri { get; set; }
        [JsonPropertyName("url_key")] public string UrlKey { get; set; }
        [JsonPropertyName("data_size")] public long? DataSize { get; set; }
        [JsonPropertyName("file_hash")] public string FileHash { get; set; }
        [JsonPropertyName("file_cs")] public string FileCs { get; set; }
    }

    public sealed class TikTokShareInfo
    {
        [JsonPropertyName("share_url")] public string ShareUrl { get; set; }
        [JsonPropertyName("share_desc")] public string ShareDesc { get; set; }
        [JsonPropertyName("share_title")] public string ShareTitle { get; set; }
        [JsonPropertyName("bool_persist")] public bool? BoolPersist { get; set; }
        [JsonPropertyName("share_quote")] public string ShareQuote { get; set; }
        [JsonPropertyName("share_signature_url")] public string ShareSignatureUrl { get; set; }
        [JsonPropertyName("share_signature_desc")] public string ShareSignatureDesc { get; set; }
    }

    public sealed class TikTokMusic
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("author")] public string Author { get; set; }
        [JsonPropertyName("album")] public string Album { get; set; }
        [JsonPropertyName("play_url")] public TikTokPlayAddr PlayUrl { get; set; }
        [JsonPropertyName("cover_thumb")] public TikTokPlayAddr CoverThumb { get; set; }
        [JsonPropertyName("cover_medium")] public TikTokPlayAddr CoverMedium { get; set; }
        [JsonPropertyName("cover_large")] public TikTokPlayAddr CoverLarge { get; set; }
        [JsonPropertyName("duration")] public int? Duration { get; set; }
        [JsonPropertyName("user_count")] public long? UserCount { get; set; }
        [JsonPropertyName("is_original")] public bool? IsOriginal { get; set; }
        [JsonPropertyName("is_author_artist")] public bool? IsAuthorArtist { get; set; }
        [JsonPropertyName("is_commerce_music")] public bool? IsCommerceMusic { get; set; }
        [JsonPropertyName("is_original_sound")] public bool? IsOriginalSound { get; set; }
    }

    public sealed class TikTokVideoStatistics
    {
        [JsonPropertyName("play_count")] public long? PlayCount { get; set; }
        [JsonPropertyName("download_count")] public long? DownloadCount { get; set; }
        [JsonPropertyName("share_count")] public long? ShareCount { get; set; }
        [JsonPropertyName("forward_count")] public long? ForwardCount { get; set; }
        [JsonPropertyName("lose_count")] public long? LoseCount { get; set; }
        [JsonPropertyName("lose_comment_count")] public long? LoseCommentCount { get; set; }
        [JsonPropertyName("whatsapp_share_count")] public long? WhatsappShareCount { get; set; }
        [JsonPropertyName("digg_count")] public long? DiggCount { get; set; }
        [JsonPropertyName("collect_count")] public long? CollectCount { get; set; }
    }

    public sealed class TikTokLiveInfo
    {
        [JsonPropertyName("start_time")] public long? StartTime { get; set; }
        [JsonPropertyName("end_time")] public long? EndTime { get; set; }
        [JsonPropertyName("status")] public int? Status { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("cover_url")] public string CoverUrl { get; set; }
    }

    // -------------------------------------------------------
    // Video List
    // -------------------------------------------------------
    public sealed class TikTokVideoList : TikTokEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("create_time")] public long? CreateTime { get; set; }
        [JsonPropertyName("cover_image_url")] public string CoverImageUrl { get; set; }
        [JsonPropertyName("share_url")] public string ShareUrl { get; set; }
        [JsonPropertyName("video_description")] public string VideoDescription { get; set; }
        [JsonPropertyName("region_code")] public string RegionCode { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("like_count")] public long? LikeCount { get; set; }
        [JsonPropertyName("comment_count")] public long? CommentCount { get; set; }
        [JsonPropertyName("share_count")] public long? ShareCount { get; set; }
        [JsonPropertyName("view_count")] public long? ViewCount { get; set; }
        [JsonPropertyName("play_count")] public long? PlayCount { get; set; }
        [JsonPropertyName("music_id")] public string MusicId { get; set; }
        [JsonPropertyName("hashtag_names")] public List<string> HashtagNames { get; set; } = new();
        [JsonPropertyName("is_ad")] public bool? IsAd { get; set; }
        [JsonPropertyName("is_top")] public bool? IsTop { get; set; }
        [JsonPropertyName("author")] public TikTokUser Author { get; set; }
        [JsonPropertyName("statistics")] public TikTokVideoStatistics Statistics { get; set; }
    }

    // -------------------------------------------------------
    // Comment
    // -------------------------------------------------------
    public sealed class TikTokComment : TikTokEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("video_id")] public string VideoId { get; set; }
        [JsonPropertyName("text")] public string Text { get; set; }
        [JsonPropertyName("create_time")] public long? CreateTime { get; set; }
        [JsonPropertyName("parent_comment_id")] public string ParentCommentId { get; set; }
        [JsonPropertyName("reply_comment_total")] public int? ReplyCommentTotal { get; set; }
        [JsonPropertyName("like_count")] public long? LikeCount { get; set; }
        [JsonPropertyName("reply_to_reply_id")] public string ReplyToReplyId { get; set; }
        [JsonPropertyName("user")] public TikTokUser User { get; set; }
        [JsonPropertyName("stickers")] public List<TikTokSticker> Stickers { get; set; } = new();
        [JsonPropertyName("is_author_digged")] public bool? IsAuthorDigged { get; set; }
        [JsonPropertyName("is_hot")] public bool? IsHot { get; set; }
        [JsonPropertyName("is_pinned")] public bool? IsPinned { get; set; }
        [JsonPropertyName("status")] public int? Status { get; set; }
        [JsonPropertyName("cursor")] public long? Cursor { get; set; }
    }

    public sealed class TikTokSticker
    {
        [JsonPropertyName("sticker_type")] public int? StickerType { get; set; }
        [JsonPropertyName("sticker_text")] public List<string> StickerText { get; set; } = new();
    }

    // -------------------------------------------------------
    // Analytics
    // -------------------------------------------------------
    public sealed class TikTokAnalytics : TikTokEntityBase
    {
        [JsonPropertyName("date")] public DateTime? Date { get; set; }
        [JsonPropertyName("video_id")] public string VideoId { get; set; }
        [JsonPropertyName("view_count")] public long? ViewCount { get; set; }
        [JsonPropertyName("like_count")] public long? LikeCount { get; set; }
        [JsonPropertyName("comment_count")] public long? CommentCount { get; set; }
        [JsonPropertyName("share_count")] public long? ShareCount { get; set; }
        [JsonPropertyName("play_count")] public long? PlayCount { get; set; }
        [JsonPropertyName("download_count")] public long? DownloadCount { get; set; }
        [JsonPropertyName("reach")] public long? Reach { get; set; }
        [JsonPropertyName("video_views_paid")] public long? VideoViewsPaid { get; set; }
        [JsonPropertyName("video_views_organic")] public long? VideoViewsOrganic { get; set; }
        [JsonPropertyName("profile_views")] public long? ProfileViews { get; set; }
        [JsonPropertyName("follower_count")] public long? FollowerCount { get; set; }
        [JsonPropertyName("following_count")] public long? FollowingCount { get; set; }
        [JsonPropertyName("total_likes")] public long? TotalLikes { get; set; }
        [JsonPropertyName("video_count")] public long? VideoCount { get; set; }
        [JsonPropertyName("live_views")] public long? LiveViews { get; set; }
        [JsonPropertyName("live_duration")] public long? LiveDuration { get; set; }
        [JsonPropertyName("live_gifts")] public long? LiveGifts { get; set; }
        [JsonPropertyName("live_comments")] public long? LiveComments { get; set; }
        [JsonPropertyName("live_shares")] public long? LiveShares { get; set; }
    }

    // -------------------------------------------------------
    // Follower
    // -------------------------------------------------------
    public sealed class TikTokFollower : TikTokEntityBase
    {
        [JsonPropertyName("open_id")] public string OpenId { get; set; }
        [JsonPropertyName("union_id")] public string UnionId { get; set; }
        [JsonPropertyName("avatar_url")] public string AvatarUrl { get; set; }
        [JsonPropertyName("display_name")] public string Caption { get; set; }
        [JsonPropertyName("bio_description")] public string BioDescription { get; set; }
        [JsonPropertyName("follower_count")] public long? FollowerCount { get; set; }
        [JsonPropertyName("following_count")] public long? FollowingCount { get; set; }
        [JsonPropertyName("likes_count")] public long? LikesCount { get; set; }
        [JsonPropertyName("video_count")] public long? VideoCount { get; set; }
        [JsonPropertyName("username")] public string Username { get; set; }
        [JsonPropertyName("create_time")] public long? CreateTime { get; set; }
        [JsonPropertyName("is_verified")] public bool? IsVerified { get; set; }
        [JsonPropertyName("sec_uid")] public string SecUid { get; set; }
        [JsonPropertyName("follow_status")] public int? FollowStatus { get; set; }
        [JsonPropertyName("follow_time")] public long? FollowTime { get; set; }
    }

    // -------------------------------------------------------
    // Hashtag
    // -------------------------------------------------------
    public sealed class TikTokHashtag : TikTokEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("cover")] public string Cover { get; set; }
        [JsonPropertyName("user_count")] public long? UserCount { get; set; }
        [JsonPropertyName("view_count")] public long? ViewCount { get; set; }
        [JsonPropertyName("video_count")] public long? VideoCount { get; set; }
        [JsonPropertyName("is_commerce")] public bool? IsCommerce { get; set; }
        [JsonPropertyName("create_time")] public long? CreateTime { get; set; }
        [JsonPropertyName("desc")] public string Desc { get; set; }
        [JsonPropertyName("music_id")] public string MusicId { get; set; }
        [JsonPropertyName("is_effect_artist")] public bool? IsEffectArtist { get; set; }
    }

    // -------------------------------------------------------
    // Music
    // -------------------------------------------------------
    public sealed class TikTokMusicInfo : TikTokEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("author")] public string Author { get; set; }
        [JsonPropertyName("album")] public string Album { get; set; }
        [JsonPropertyName("play_url")] public TikTokPlayAddr PlayUrl { get; set; }
        [JsonPropertyName("cover_thumb")] public TikTokPlayAddr CoverThumb { get; set; }
        [JsonPropertyName("cover_medium")] public TikTokPlayAddr CoverMedium { get; set; }
        [JsonPropertyName("cover_large")] public TikTokPlayAddr CoverLarge { get; set; }
        [JsonPropertyName("duration")] public int? Duration { get; set; }
        [JsonPropertyName("user_count")] public long? UserCount { get; set; }
        [JsonPropertyName("is_original")] public bool? IsOriginal { get; set; }
        [JsonPropertyName("is_author_artist")] public bool? IsAuthorArtist { get; set; }
        [JsonPropertyName("is_commerce_music")] public bool? IsCommerceMusic { get; set; }
        [JsonPropertyName("is_original_sound")] public bool? IsOriginalSound { get; set; }
        [JsonPropertyName("status")] public int? Status { get; set; }
        [JsonPropertyName("offline_desc")] public string OfflineDesc { get; set; }
        [JsonPropertyName("effects_data")] public TikTokEffectsData EffectsData { get; set; }
        [JsonPropertyName("video_duration")] public int? VideoDuration { get; set; }
        [JsonPropertyName("audition_duration")] public int? AuditionDuration { get; set; }
    }

    public sealed class TikTokEffectsData
    {
        [JsonPropertyName("url")] public string Url { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("icon_url")] public string IconUrl { get; set; }
        [JsonPropertyName("id")] public string Id { get; set; }
    }

    // -------------------------------------------------------
    // Search Results
    // -------------------------------------------------------
    public sealed class TikTokSearchResult : TikTokEntityBase
    {
        [JsonPropertyName("videos")] public List<TikTokVideo> Videos { get; set; } = new();
        [JsonPropertyName("cursor")] public long? Cursor { get; set; }
        [JsonPropertyName("has_more")] public bool? HasMore { get; set; }
        [JsonPropertyName("search_id")] public string SearchId { get; set; }
        [JsonPropertyName("qa_search_invocation_id")] public string QaSearchInvocationId { get; set; }
    }

    // -------------------------------------------------------
    // Live Room
    // -------------------------------------------------------
    public sealed class TikTokLiveRoom : TikTokEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("cover_url")] public string CoverUrl { get; set; }
        [JsonPropertyName("start_time")] public long? StartTime { get; set; }
        [JsonPropertyName("status")] public int? Status { get; set; }
        [JsonPropertyName("audience_count")] public long? AudienceCount { get; set; }
        [JsonPropertyName("total_user_count")] public long? TotalUserCount { get; set; }
        [JsonPropertyName("live_type")] public string LiveType { get; set; }
        [JsonPropertyName("stream_url")] public TikTokPlayAddr StreamUrl { get; set; }
        [JsonPropertyName("share_url")] public string ShareUrl { get; set; }
        [JsonPropertyName("anchor_info")] public TikTokUser AnchorInfo { get; set; }
        [JsonPropertyName("gifts")] public List<TikTokGift> Gifts { get; set; } = new();
        [JsonPropertyName("like_count")] public long? LikeCount { get; set; }
        [JsonPropertyName("comment_count")] public long? CommentCount { get; set; }
        [JsonPropertyName("share_count")] public long? ShareCount { get; set; }
    }

    public sealed class TikTokGift
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("image")] public TikTokPlayAddr Image { get; set; }
        [JsonPropertyName("diamond_count")] public long? DiamondCount { get; set; }
        [JsonPropertyName("user")] public TikTokUser User { get; set; }
        [JsonPropertyName("send_time")] public long? SendTime { get; set; }
    }

    // -------------------------------------------------------
    // Challenge (Hashtag Challenge)
    // -------------------------------------------------------
    public sealed class TikTokChallenge : TikTokEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("desc")] public string Desc { get; set; }
        [JsonPropertyName("profile_thumb")] public string ProfileThumb { get; set; }
        [JsonPropertyName("profile_medium")] public string ProfileMedium { get; set; }
        [JsonPropertyName("profile_larger")] public string ProfileLarger { get; set; }
        [JsonPropertyName("cover_thumb")] public string CoverThumb { get; set; }
        [JsonPropertyName("cover_medium")] public string CoverMedium { get; set; }
        [JsonPropertyName("cover_larger")] public string CoverLarger { get; set; }
        [JsonPropertyName("is_commerce")] public bool? IsCommerce { get; set; }
        [JsonPropertyName("view_count")] public long? ViewCount { get; set; }
        [JsonPropertyName("user_count")] public long? UserCount { get; set; }
        [JsonPropertyName("video_count")] public long? VideoCount { get; set; }
        [JsonPropertyName("connect_music")] public List<TikTokMusicInfo> ConnectMusic { get; set; } = new();
        [JsonPropertyName("creator")] public TikTokUser Creator { get; set; }
        [JsonPropertyName("stickers")] public List<TikTokSticker> Stickers { get; set; } = new();
    }

    // -------------------------------------------------------
    // POI (Point of Interest)
    // -------------------------------------------------------
    public sealed class TikTokPOI : TikTokEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("address")] public string Address { get; set; }
        [JsonPropertyName("city")] public string City { get; set; }
        [JsonPropertyName("province")] public string Province { get; set; }
        [JsonPropertyName("country")] public string Country { get; set; }
        [JsonPropertyName("latitude")] public double? Latitude { get; set; }
        [JsonPropertyName("longitude")] public double? Longitude { get; set; }
        [JsonPropertyName("cover")] public TikTokPlayAddr Cover { get; set; }
        [JsonPropertyName("checkin_count")] public long? CheckinCount { get; set; }
        [JsonPropertyName("country_code")] public string CountryCode { get; set; }
        [JsonPropertyName("district_info")] public string DistrictInfo { get; set; }
        [JsonPropertyName("father_poi_id")] public string FatherPoiId { get; set; }
        [JsonPropertyName("father_poi_name")] public string FatherPoiName { get; set; }
        [JsonPropertyName("poi_id")] public string PoiId { get; set; }
        [JsonPropertyName("poi_name")] public string PoiName { get; set; }
        [JsonPropertyName("recommend_index")] public long? RecommendIndex { get; set; }
        [JsonPropertyName("type_code")] public string TypeCode { get; set; }
        [JsonPropertyName("type_name")] public string TypeName { get; set; }
        [JsonPropertyName("videos")] public List<TikTokVideo> Videos { get; set; } = new();
    }

    // -------------------------------------------------------
    // Effect
    // -------------------------------------------------------
    public sealed class TikTokEffect : TikTokEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("icon_url")] public string IconUrl { get; set; }
        [JsonPropertyName("owner_id")] public string OwnerId { get; set; }
        [JsonPropertyName("owner_nickname")] public string OwnerNickname { get; set; }
        [JsonPropertyName("effect_status")] public string EffectStatus { get; set; }
        [JsonPropertyName("desc")] public string Desc { get; set; }
        [JsonPropertyName("backend_effect_id")] public string BackendEffectId { get; set; }
        [JsonPropertyName("device_platform")] public string DevicePlatform { get; set; }
        [JsonPropertyName("effect_type")] public string EffectType { get; set; }
        [JsonPropertyName("is_commerce")] public bool? IsCommerce { get; set; }
        [JsonPropertyName("uploaded_time")] public long? UploadedTime { get; set; }
        [JsonPropertyName("user_count")] public long? UserCount { get; set; }
        [JsonPropertyName("video_count")] public long? VideoCount { get; set; }
        [JsonPropertyName("url")] public string Url { get; set; }
        [JsonPropertyName("zip_url")] public string ZipUrl { get; set; }
    }

    // -------------------------------------------------------
    // Response Wrappers
    // -------------------------------------------------------
    public sealed class TikTokResponse<T>
    {
        [JsonPropertyName("data")] public TikTokResponseData<T> Data { get; set; }
        [JsonPropertyName("error")] public TikTokError Error { get; set; }
        [JsonPropertyName("extra")] public TikTokExtra Extra { get; set; }
    }

    public sealed class TikTokResponseData<T>
    {
        [JsonPropertyName("cursor")] public long? Cursor { get; set; }
        [JsonPropertyName("has_more")] public bool? HasMore { get; set; }
        [JsonPropertyName("list")] public List<T> List { get; set; } = new();
        [JsonPropertyName("user_list")] public List<T> UserList { get; set; } = new();
        [JsonPropertyName("video_list")] public List<T> VideoList { get; set; } = new();
        [JsonPropertyName("music_list")] public List<T> MusicList { get; set; } = new();
        [JsonPropertyName("hashtag_list")] public List<T> HashtagList { get; set; } = new();
        [JsonPropertyName("effect_list")] public List<T> EffectList { get; set; } = new();
        [JsonPropertyName("poi_list")] public List<T> PoiList { get; set; } = new();
    }

    public sealed class TikTokError
    {
        [JsonPropertyName("code")] public int? Code { get; set; }
        [JsonPropertyName("message")] public string Message { get; set; }
        [JsonPropertyName("log_id")] public string LogId { get; set; }
    }

    public sealed class TikTokExtra
    {
        [JsonPropertyName("now")] public long? Now { get; set; }
        [JsonPropertyName("logid")] public string Logid { get; set; }
        [JsonPropertyName("fatal_item_ids")] public List<string> FatalItemIds { get; set; } = new();
        [JsonPropertyName("description")] public string Description { get; set; }
    }
}