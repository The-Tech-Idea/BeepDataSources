// File: Connectors/SocialMedia/YouTube/Models.cs
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.YouTube.Models
{
    // -------------------------------------------------------
    // Base Entity
    // -------------------------------------------------------
    public abstract class YouTubeEntityBase
    {
        [JsonIgnore] public IDataSource DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : YouTubeEntityBase { DataSource = ds; return (T)this; }
    }

    // -------------------------------------------------------
    // Channel
    // -------------------------------------------------------
    public sealed class YouTubeChannel : YouTubeEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("snippet")] public YouTubeChannelSnippet Snippet { get; set; }
        [JsonPropertyName("statistics")] public YouTubeChannelStatistics Statistics { get; set; }
        [JsonPropertyName("status")] public YouTubeChannelStatus Status { get; set; }
        [JsonPropertyName("brandingSettings")] public YouTubeChannelBrandingSettings BrandingSettings { get; set; }
        [JsonPropertyName("contentDetails")] public YouTubeChannelContentDetails ContentDetails { get; set; }
        [JsonPropertyName("topicDetails")] public YouTubeChannelTopicDetails TopicDetails { get; set; }
        [JsonPropertyName("localizations")] public Dictionary<string, YouTubeChannelLocalization> Localizations { get; set; } = new();
    }

    public sealed class YouTubeChannelSnippet
    {
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("customUrl")] public string CustomUrl { get; set; }
        [JsonPropertyName("publishedAt")] public DateTime? PublishedAt { get; set; }
        [JsonPropertyName("thumbnails")] public YouTubeThumbnails Thumbnails { get; set; }
        [JsonPropertyName("defaultLanguage")] public string DefaultLanguage { get; set; }
        [JsonPropertyName("localized")] public YouTubeChannelLocalization Localized { get; set; }
        [JsonPropertyName("country")] public string Country { get; set; }
    }

    public sealed class YouTubeChannelStatistics
    {
        [JsonPropertyName("viewCount")] public ulong? ViewCount { get; set; }
        [JsonPropertyName("subscriberCount")] public ulong? SubscriberCount { get; set; }
        [JsonPropertyName("hiddenSubscriberCount")] public bool? HiddenSubscriberCount { get; set; }
        [JsonPropertyName("videoCount")] public ulong? VideoCount { get; set; }
    }

    public sealed class YouTubeChannelStatus
    {
        [JsonPropertyName("privacyStatus")] public string PrivacyStatus { get; set; }
        [JsonPropertyName("isLinked")] public bool? IsLinked { get; set; }
        [JsonPropertyName("longUploadsStatus")] public string LongUploadsStatus { get; set; }
        [JsonPropertyName("madeForKids")] public bool? MadeForKids { get; set; }
        [JsonPropertyName("selfDeclaredMadeForKids")] public bool? SelfDeclaredMadeForKids { get; set; }
    }

    public sealed class YouTubeChannelBrandingSettings
    {
        [JsonPropertyName("channel")] public YouTubeChannelBrandingChannel Channel { get; set; }
        [JsonPropertyName("image")] public YouTubeChannelBrandingImage Image { get; set; }
        [JsonPropertyName("hints")] public List<YouTubePropertyValue> Hints { get; set; } = new();
    }

    public sealed class YouTubeChannelBrandingChannel
    {
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("keywords")] public string Keywords { get; set; }
        [JsonPropertyName("defaultTab")] public string DefaultTab { get; set; }
        [JsonPropertyName("trackingAnalyticsAccountId")] public string TrackingAnalyticsAccountId { get; set; }
        [JsonPropertyName("moderateComments")] public bool? ModerateComments { get; set; }
        [JsonPropertyName("showRelatedChannels")] public bool? ShowRelatedChannels { get; set; }
        [JsonPropertyName("showBrowseView")] public bool? ShowBrowseView { get; set; }
        [JsonPropertyName("featuredChannelsTitle")] public string FeaturedChannelsTitle { get; set; }
        [JsonPropertyName("featuredChannelsUrls")] public List<string> FeaturedChannelsUrls { get; set; } = new();
        [JsonPropertyName("unsubscribedTrailer")] public string UnsubscribedTrailer { get; set; }
        [JsonPropertyName("profileColor")] public string ProfileColor { get; set; }
        [JsonPropertyName("defaultLanguage")] public string DefaultLanguage { get; set; }
        [JsonPropertyName("country")] public string Country { get; set; }
    }

    public sealed class YouTubeChannelBrandingImage
    {
        [JsonPropertyName("bannerExternalUrl")] public string BannerExternalUrl { get; set; }
        [JsonPropertyName("bannerImageUrl")] public string BannerImageUrl { get; set; }
        [JsonPropertyName("bannerTabletExtraHdImageUrl")] public string BannerTabletExtraHdImageUrl { get; set; }
        [JsonPropertyName("bannerTabletHdImageUrl")] public string BannerTabletHdImageUrl { get; set; }
        [JsonPropertyName("bannerTabletImageUrl")] public string BannerTabletImageUrl { get; set; }
        [JsonPropertyName("bannerTabletLowImageUrl")] public string BannerTabletLowImageUrl { get; set; }
        [JsonPropertyName("bannerMobileExtraHdImageUrl")] public string BannerMobileExtraHdImageUrl { get; set; }
        [JsonPropertyName("bannerMobileHdImageUrl")] public string BannerMobileHdImageUrl { get; set; }
        [JsonPropertyName("bannerMobileImageUrl")] public string BannerMobileImageUrl { get; set; }
        [JsonPropertyName("bannerMobileLowImageUrl")] public string BannerMobileLowImageUrl { get; set; }
        [JsonPropertyName("bannerMobileMediumHdImageUrl")] public string BannerMobileMediumHdImageUrl { get; set; }
        [JsonPropertyName("bannerTvHighImageUrl")] public string BannerTvHighImageUrl { get; set; }
        [JsonPropertyName("bannerTvImageUrl")] public string BannerTvImageUrl { get; set; }
        [JsonPropertyName("bannerTvLowImageUrl")] public string BannerTvLowImageUrl { get; set; }
        [JsonPropertyName("bannerTvMediumImageUrl")] public string BannerTvMediumImageUrl { get; set; }
        [JsonPropertyName("largeBrandedBannerImageImapScript")] public YouTubeLocalizedProperty LargeBrandedBannerImageImapScript { get; set; }
        [JsonPropertyName("largeBrandedBannerImageUrl")] public string LargeBrandedBannerImageUrl { get; set; }
        [JsonPropertyName("smallBrandedBannerImageImapScript")] public YouTubeLocalizedProperty SmallBrandedBannerImageImapScript { get; set; }
        [JsonPropertyName("smallBrandedBannerImageUrl")] public string SmallBrandedBannerImageUrl { get; set; }
        [JsonPropertyName("watchIconImageUrl")] public string WatchIconImageUrl { get; set; }
        [JsonPropertyName("trackingImageUrl")] public string TrackingImageUrl { get; set; }
        [JsonPropertyName("bannerImageImapScript")] public YouTubeLocalizedProperty BannerImageImapScript { get; set; }
    }

    public sealed class YouTubePropertyValue
    {
        [JsonPropertyName("property")] public string Property { get; set; }
        [JsonPropertyName("value")] public string Value { get; set; }
    }

    public sealed class YouTubeLocalizedProperty
    {
        [JsonPropertyName("default")] public string Default { get; set; }
        [JsonPropertyName("localized")] public Dictionary<string, string> Localized { get; set; } = new();
    }

    public sealed class YouTubeChannelContentDetails
    {
        [JsonPropertyName("relatedPlaylists")] public YouTubeRelatedPlaylists RelatedPlaylists { get; set; }
    }

    public sealed class YouTubeRelatedPlaylists
    {
        [JsonPropertyName("likes")] public string Likes { get; set; }
        [JsonPropertyName("favorites")] public string Favorites { get; set; }
        [JsonPropertyName("uploads")] public string Uploads { get; set; }
        [JsonPropertyName("watchHistory")] public string WatchHistory { get; set; }
        [JsonPropertyName("watchLater")] public string WatchLater { get; set; }
    }

    public sealed class YouTubeChannelTopicDetails
    {
        [JsonPropertyName("topicIds")] public List<string> TopicIds { get; set; } = new();
        [JsonPropertyName("topicCategories")] public List<string> TopicCategories { get; set; } = new();
    }

    public sealed class YouTubeChannelLocalization
    {
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
    }

    // -------------------------------------------------------
    // Video
    // -------------------------------------------------------
    public sealed class YouTubeVideo : YouTubeEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("snippet")] public YouTubeVideoSnippet Snippet { get; set; }
        [JsonPropertyName("contentDetails")] public YouTubeVideoContentDetails ContentDetails { get; set; }
        [JsonPropertyName("status")] public YouTubeVideoStatus Status { get; set; }
        [JsonPropertyName("statistics")] public YouTubeVideoStatistics Statistics { get; set; }
        [JsonPropertyName("player")] public YouTubeVideoPlayer Player { get; set; }
        [JsonPropertyName("topicDetails")] public YouTubeVideoTopicDetails TopicDetails { get; set; }
        [JsonPropertyName("recordingDetails")] public YouTubeVideoRecordingDetails RecordingDetails { get; set; }
        [JsonPropertyName("fileDetails")] public YouTubeVideoFileDetails FileDetails { get; set; }
        [JsonPropertyName("processingDetails")] public YouTubeVideoProcessingDetails ProcessingDetails { get; set; }
        [JsonPropertyName("suggestions")] public YouTubeVideoSuggestions Suggestions { get; set; }
        [JsonPropertyName("liveStreamingDetails")] public YouTubeVideoLiveStreamingDetails LiveStreamingDetails { get; set; }
        [JsonPropertyName("localizations")] public Dictionary<string, YouTubeVideoLocalization> Localizations { get; set; } = new();
        [JsonPropertyName("paidProductPlacementDetails")] public YouTubeVideoPaidProductPlacementDetails PaidProductPlacementDetails { get; set; }
    }

    public sealed class YouTubeVideoSnippet
    {
        [JsonPropertyName("publishedAt")] public DateTime? PublishedAt { get; set; }
        [JsonPropertyName("channelId")] public string ChannelId { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("thumbnails")] public YouTubeThumbnails Thumbnails { get; set; }
        [JsonPropertyName("channelTitle")] public string ChannelTitle { get; set; }
        [JsonPropertyName("tags")] public List<string> Tags { get; set; } = new();
        [JsonPropertyName("categoryId")] public string CategoryId { get; set; }
        [JsonPropertyName("liveBroadcastContent")] public string LiveBroadcastContent { get; set; }
        [JsonPropertyName("defaultLanguage")] public string DefaultLanguage { get; set; }
        [JsonPropertyName("localized")] public YouTubeVideoLocalization Localized { get; set; }
        [JsonPropertyName("defaultAudioLanguage")] public string DefaultAudioLanguage { get; set; }
    }

    public sealed class YouTubeThumbnails
    {
        [JsonPropertyName("default")] public YouTubeThumbnail Default { get; set; }
        [JsonPropertyName("medium")] public YouTubeThumbnail Medium { get; set; }
        [JsonPropertyName("high")] public YouTubeThumbnail High { get; set; }
        [JsonPropertyName("standard")] public YouTubeThumbnail Standard { get; set; }
        [JsonPropertyName("maxres")] public YouTubeThumbnail Maxres { get; set; }
    }

    public sealed class YouTubeThumbnail
    {
        [JsonPropertyName("url")] public string Url { get; set; }
        [JsonPropertyName("width")] public int? Width { get; set; }
        [JsonPropertyName("height")] public int? Height { get; set; }
    }

    public sealed class YouTubeVideoContentDetails
    {
        [JsonPropertyName("duration")] public string Duration { get; set; }
        [JsonPropertyName("dimension")] public string Dimension { get; set; }
        [JsonPropertyName("definition")] public string Definition { get; set; }
        [JsonPropertyName("caption")] public string Caption { get; set; }
        [JsonPropertyName("licensedContent")] public bool? LicensedContent { get; set; }
        [JsonPropertyName("regionRestriction")] public YouTubeVideoRegionRestriction RegionRestriction { get; set; }
        [JsonPropertyName("contentRating")] public YouTubeVideoContentRating ContentRating { get; set; }
        [JsonPropertyName("projection")] public string Projection { get; set; }
        [JsonPropertyName("hasCustomThumbnail")] public bool? HasCustomThumbnail { get; set; }
    }

    public sealed class YouTubeVideoRegionRestriction
    {
        [JsonPropertyName("allowed")] public List<string> Allowed { get; set; } = new();
        [JsonPropertyName("blocked")] public List<string> Blocked { get; set; } = new();
    }

    public sealed class YouTubeVideoContentRating
    {
        [JsonPropertyName("acbRating")] public string AcbRating { get; set; }
        [JsonPropertyName("agcomRating")] public string AgcomRating { get; set; }
        [JsonPropertyName("anatelRating")] public string AnatelRating { get; set; }
        [JsonPropertyName("bbfcRating")] public string BbfcRating { get; set; }
        [JsonPropertyName("bfvcRating")] public string BfvcRating { get; set; }
        [JsonPropertyName("bmukkRating")] public string BmukkRating { get; set; }
        [JsonPropertyName("catvRating")] public string CatvRating { get; set; }
        [JsonPropertyName("catvfrRating")] public string CatvfrRating { get; set; }
        [JsonPropertyName("cbfcRating")] public string CbfcRating { get; set; }
        [JsonPropertyName("cccRating")] public string CccRating { get; set; }
        [JsonPropertyName("cceRating")] public string CceRating { get; set; }
        [JsonPropertyName("chfilmRating")] public string ChfilmRating { get; set; }
        [JsonPropertyName("chvrsRating")] public string ChvrsRating { get; set; }
        [JsonPropertyName("cicfRating")] public string CicfRating { get; set; }
        [JsonPropertyName("cnaRating")] public string CnaRating { get; set; }
        [JsonPropertyName("cncRating")] public string CncRating { get; set; }
        [JsonPropertyName("csaRating")] public string CsaRating { get; set; }
        [JsonPropertyName("cscfRating")] public string CscfRating { get; set; }
        [JsonPropertyName("czfilmRating")] public string CzfilmRating { get; set; }
        [JsonPropertyName("djctqRating")] public string DjctqRating { get; set; }
        [JsonPropertyName("djctqRatingReasons")] public List<string> DjctqRatingReasons { get; set; } = new();
        [JsonPropertyName("ecbmctRating")] public string EcbmctRating { get; set; }
        [JsonPropertyName("eefilmRating")] public string EefilmRating { get; set; }
        [JsonPropertyName("egfilmRating")] public string EgfilmRating { get; set; }
        [JsonPropertyName("eirinRating")] public string EirinRating { get; set; }
        [JsonPropertyName("fcbmRating")] public string FcbmRating { get; set; }
        [JsonPropertyName("fcoRating")] public string FcoRating { get; set; }
        [JsonPropertyName("fmocRating")] public string FmocRating { get; set; }
        [JsonPropertyName("fpbRating")] public string FpbRating { get; set; }
        [JsonPropertyName("fpbRatingReasons")] public List<string> FpbRatingReasons { get; set; } = new();
        [JsonPropertyName("fskRating")] public string FskRating { get; set; }
        [JsonPropertyName("grfilmRating")] public string GrfilmRating { get; set; }
        [JsonPropertyName("icaaRating")] public string IcaaRating { get; set; }
        [JsonPropertyName("ifcoRating")] public string IfcoRating { get; set; }
        [JsonPropertyName("ilfilmRating")] public string IlfilmRating { get; set; }
        [JsonPropertyName("incaaRating")] public string IncaRating { get; set; }
        [JsonPropertyName("kfcbRating")] public string KfcbRating { get; set; }
        [JsonPropertyName("kijkwijzerRating")] public string KijkwijzerRating { get; set; }
        [JsonPropertyName("kmrbRating")] public string KmrbRating { get; set; }
        [JsonPropertyName("lsfRating")] public string LsfRating { get; set; }
        [JsonPropertyName("mccaaRating")] public string MccaaRating { get; set; }
        [JsonPropertyName("mccypRating")] public string MccypRating { get; set; }
        [JsonPropertyName("mcstRating")] public string McstRating { get; set; }
        [JsonPropertyName("mdaRating")] public string MdaRating { get; set; }
        [JsonPropertyName("medietilsynetRating")] public string MedietilsynetRating { get; set; }
        [JsonPropertyName("mekuRating")] public string MekuRating { get; set; }
        [JsonPropertyName("menaMpaaRating")] public string MenaMpaaRating { get; set; }
        [JsonPropertyName("mibacRating")] public string MibacRating { get; set; }
        [JsonPropertyName("mocRating")] public string MocRating { get; set; }
        [JsonPropertyName("moctwRating")] public string MoctwRating { get; set; }
        [JsonPropertyName("mpaaRating")] public string MpaaRating { get; set; }
        [JsonPropertyName("mpaatRating")] public string MpaatRating { get; set; }
        [JsonPropertyName("mtrcbRating")] public string MtrcbRating { get; set; }
        [JsonPropertyName("nbcRating")] public string NbcRating { get; set; }
        [JsonPropertyName("nbcplRating")] public string NbcplRating { get; set; }
        [JsonPropertyName("nfrcRating")] public string NfrcRating { get; set; }
        [JsonPropertyName("nfvcbRating")] public string NfvcbRating { get; set; }
        [JsonPropertyName("nkclvRating")] public string NkclvRating { get; set; }
        [JsonPropertyName("oflcRating")] public string OflcRating { get; set; }
        [JsonPropertyName("pefilmRating")] public string PefilmRating { get; set; }
        [JsonPropertyName("rcnofRating")] public string RcnofRating { get; set; }
        [JsonPropertyName("resorteviolenciaRating")] public string ResorteviolenciaRating { get; set; }
        [JsonPropertyName("rtcRating")] public string RtcRating { get; set; }
        [JsonPropertyName("rteRating")] public string RteRating { get; set; }
        [JsonPropertyName("russiaRating")] public string RussiaRating { get; set; }
        [JsonPropertyName("skfilmRating")] public string SkfilmRating { get; set; }
        [JsonPropertyName("smaisRating")] public string SmaisRating { get; set; }
        [JsonPropertyName("smsaRating")] public string SmsaRating { get; set; }
        [JsonPropertyName("tvpgRating")] public string TvpgRating { get; set; }
        [JsonPropertyName("ytRating")] public string YtRating { get; set; }
    }

    public sealed class YouTubeVideoStatus
    {
        [JsonPropertyName("uploadStatus")] public string UploadStatus { get; set; }
        [JsonPropertyName("failureReason")] public string FailureReason { get; set; }
        [JsonPropertyName("rejectionReason")] public string RejectionReason { get; set; }
        [JsonPropertyName("privacyStatus")] public string PrivacyStatus { get; set; }
        [JsonPropertyName("publishAt")] public DateTime? PublishAt { get; set; }
        [JsonPropertyName("license")] public string License { get; set; }
        [JsonPropertyName("embeddable")] public bool? Embeddable { get; set; }
        [JsonPropertyName("publicStatsViewable")] public bool? PublicStatsViewable { get; set; }
        [JsonPropertyName("madeForKids")] public bool? MadeForKids { get; set; }
        [JsonPropertyName("selfDeclaredMadeForKids")] public bool? SelfDeclaredMadeForKids { get; set; }
    }

    public sealed class YouTubeVideoStatistics
    {
        [JsonPropertyName("viewCount")] public ulong? ViewCount { get; set; }
        [JsonPropertyName("likeCount")] public ulong? LikeCount { get; set; }
        [JsonPropertyName("dislikeCount")] public ulong? DislikeCount { get; set; }
        [JsonPropertyName("favoriteCount")] public ulong? FavoriteCount { get; set; }
        [JsonPropertyName("commentCount")] public ulong? CommentCount { get; set; }
    }

    public sealed class YouTubeVideoPlayer
    {
        [JsonPropertyName("embedHtml")] public string EmbedHtml { get; set; }
        [JsonPropertyName("embedHeight")] public long? EmbedHeight { get; set; }
        [JsonPropertyName("embedWidth")] public long? EmbedWidth { get; set; }
    }

    public sealed class YouTubeVideoTopicDetails
    {
        [JsonPropertyName("topicIds")] public List<string> TopicIds { get; set; } = new();
        [JsonPropertyName("relevantTopicIds")] public List<string> RelevantTopicIds { get; set; } = new();
        [JsonPropertyName("topicCategories")] public List<string> TopicCategories { get; set; } = new();
    }

    public sealed class YouTubeVideoRecordingDetails
    {
        [JsonPropertyName("recordingDate")] public DateTime? RecordingDate { get; set; }
        [JsonPropertyName("location")] public YouTubeGeoPoint Location { get; set; }
        [JsonPropertyName("locationDescription")] public string LocationDescription { get; set; }
    }

    public sealed class YouTubeGeoPoint
    {
        [JsonPropertyName("latitude")] public double? Latitude { get; set; }
        [JsonPropertyName("longitude")] public double? Longitude { get; set; }
        [JsonPropertyName("altitude")] public double? Altitude { get; set; }
    }

    public sealed class YouTubeVideoFileDetails
    {
        [JsonPropertyName("fileName")] public string FileName { get; set; }
        [JsonPropertyName("fileSize")] public ulong? FileSize { get; set; }
        [JsonPropertyName("fileType")] public string FileType { get; set; }
        [JsonPropertyName("container")] public string Container { get; set; }
        [JsonPropertyName("videoStreams")] public List<YouTubeVideoStream> VideoStreams { get; set; } = new();
        [JsonPropertyName("audioStreams")] public List<YouTubeAudioStream> AudioStreams { get; set; } = new();
        [JsonPropertyName("durationMs")] public ulong? DurationMs { get; set; }
        [JsonPropertyName("bitrateBps")] public ulong? BitrateBps { get; set; }
        [JsonPropertyName("creationTime")] public string CreationTime { get; set; }
    }

    public sealed class YouTubeVideoStream
    {
        [JsonPropertyName("widthPixels")] public uint? WidthPixels { get; set; }
        [JsonPropertyName("heightPixels")] public uint? HeightPixels { get; set; }
        [JsonPropertyName("frameRateFps")] public double? FrameRateFps { get; set; }
        [JsonPropertyName("aspectRatio")] public double? AspectRatio { get; set; }
        [JsonPropertyName("codec")] public string Codec { get; set; }
        [JsonPropertyName("bitrateBps")] public ulong? BitrateBps { get; set; }
        [JsonPropertyName("rotation")] public string Rotation { get; set; }
        [JsonPropertyName("vendor")] public string Vendor { get; set; }
    }

    public sealed class YouTubeAudioStream
    {
        [JsonPropertyName("channelCount")] public uint? ChannelCount { get; set; }
        [JsonPropertyName("codec")] public string Codec { get; set; }
        [JsonPropertyName("bitrateBps")] public ulong? BitrateBps { get; set; }
        [JsonPropertyName("vendor")] public string Vendor { get; set; }
    }

    public sealed class YouTubeVideoProcessingDetails
    {
        [JsonPropertyName("processingStatus")] public string ProcessingStatus { get; set; }
        [JsonPropertyName("processingProgress")] public YouTubeVideoProcessingProgress ProcessingProgress { get; set; }
        [JsonPropertyName("processingFailureReason")] public string ProcessingFailureReason { get; set; }
        [JsonPropertyName("fileDetailsAvailability")] public string FileDetailsAvailability { get; set; }
        [JsonPropertyName("processingIssuesAvailability")] public string ProcessingIssuesAvailability { get; set; }
        [JsonPropertyName("tagSuggestionsAvailability")] public string TagSuggestionsAvailability { get; set; }
        [JsonPropertyName("editorSuggestionsAvailability")] public string EditorSuggestionsAvailability { get; set; }
        [JsonPropertyName("thumbnailsAvailability")] public string ThumbnailsAvailability { get; set; }
    }

    public sealed class YouTubeVideoProcessingProgress
    {
        [JsonPropertyName("partsTotal")] public ulong? PartsTotal { get; set; }
        [JsonPropertyName("partsProcessed")] public ulong? PartsProcessed { get; set; }
        [JsonPropertyName("timeLeftMs")] public ulong? TimeLeftMs { get; set; }
    }

    public sealed class YouTubeVideoSuggestions
    {
        [JsonPropertyName("processingErrors")] public List<string> ProcessingErrors { get; set; } = new();
        [JsonPropertyName("processingWarnings")] public List<string> ProcessingWarnings { get; set; } = new();
        [JsonPropertyName("processingHints")] public List<string> ProcessingHints { get; set; } = new();
        [JsonPropertyName("tagSuggestions")] public List<YouTubeVideoTagSuggestion> TagSuggestions { get; set; } = new();
        [JsonPropertyName("editorSuggestions")] public List<string> EditorSuggestions { get; set; } = new();
    }

    public sealed class YouTubeVideoTagSuggestion
    {
        [JsonPropertyName("tag")] public string Tag { get; set; }
        [JsonPropertyName("categoryRestricts")] public List<string> CategoryRestricts { get; set; } = new();
    }

    public sealed class YouTubeVideoLiveStreamingDetails
    {
        [JsonPropertyName("actualStartTime")] public DateTime? ActualStartTime { get; set; }
        [JsonPropertyName("actualEndTime")] public DateTime? ActualEndTime { get; set; }
        [JsonPropertyName("scheduledStartTime")] public DateTime? ScheduledStartTime { get; set; }
        [JsonPropertyName("scheduledEndTime")] public DateTime? ScheduledEndTime { get; set; }
        [JsonPropertyName("concurrentViewers")] public ulong? ConcurrentViewers { get; set; }
        [JsonPropertyName("activeLiveChatId")] public string ActiveLiveChatId { get; set; }
    }

    public sealed class YouTubeVideoLocalization
    {
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
    }

    public sealed class YouTubeVideoPaidProductPlacementDetails
    {
        [JsonPropertyName("hasPaidProductPlacement")] public bool? HasPaidProductPlacement { get; set; }
    }

    // -------------------------------------------------------
    // Playlist
    // -------------------------------------------------------
    public sealed class YouTubePlaylist : YouTubeEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("snippet")] public YouTubePlaylistSnippet Snippet { get; set; }
        [JsonPropertyName("status")] public YouTubePlaylistStatus Status { get; set; }
        [JsonPropertyName("contentDetails")] public YouTubePlaylistContentDetails ContentDetails { get; set; }
        [JsonPropertyName("player")] public YouTubePlaylistPlayer Player { get; set; }
        [JsonPropertyName("localizations")] public Dictionary<string, YouTubePlaylistLocalization> Localizations { get; set; } = new();
    }

    public sealed class YouTubePlaylistSnippet
    {
        [JsonPropertyName("publishedAt")] public DateTime? PublishedAt { get; set; }
        [JsonPropertyName("channelId")] public string ChannelId { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("thumbnails")] public YouTubeThumbnails Thumbnails { get; set; }
        [JsonPropertyName("channelTitle")] public string ChannelTitle { get; set; }
        [JsonPropertyName("defaultLanguage")] public string DefaultLanguage { get; set; }
        [JsonPropertyName("localized")] public YouTubePlaylistLocalization Localized { get; set; }
        [JsonPropertyName("tags")] public List<string> Tags { get; set; } = new();
    }

    public sealed class YouTubePlaylistStatus
    {
        [JsonPropertyName("privacyStatus")] public string PrivacyStatus { get; set; }
    }

    public sealed class YouTubePlaylistContentDetails
    {
        [JsonPropertyName("itemCount")] public uint? ItemCount { get; set; }
    }

    public sealed class YouTubePlaylistPlayer
    {
        [JsonPropertyName("embedHtml")] public string EmbedHtml { get; set; }
    }

    public sealed class YouTubePlaylistLocalization
    {
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
    }

    // -------------------------------------------------------
    // Playlist Item
    // -------------------------------------------------------
    public sealed class YouTubePlaylistItem : YouTubeEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("snippet")] public YouTubePlaylistItemSnippet Snippet { get; set; }
        [JsonPropertyName("contentDetails")] public YouTubePlaylistItemContentDetails ContentDetails { get; set; }
        [JsonPropertyName("status")] public YouTubePlaylistItemStatus Status { get; set; }
    }

    public sealed class YouTubePlaylistItemSnippet
    {
        [JsonPropertyName("publishedAt")] public DateTime? PublishedAt { get; set; }
        [JsonPropertyName("channelId")] public string ChannelId { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("thumbnails")] public YouTubeThumbnails Thumbnails { get; set; }
        [JsonPropertyName("channelTitle")] public string ChannelTitle { get; set; }
        [JsonPropertyName("playlistId")] public string PlaylistId { get; set; }
        [JsonPropertyName("position")] public uint? Position { get; set; }
        [JsonPropertyName("resourceId")] public YouTubeResourceId ResourceId { get; set; }
    }

    public sealed class YouTubeResourceId
    {
        [JsonPropertyName("kind")] public string Kind { get; set; }
        [JsonPropertyName("videoId")] public string VideoId { get; set; }
        [JsonPropertyName("channelId")] public string ChannelId { get; set; }
        [JsonPropertyName("playlistId")] public string PlaylistId { get; set; }
    }

    public sealed class YouTubePlaylistItemContentDetails
    {
        [JsonPropertyName("videoId")] public string VideoId { get; set; }
        [JsonPropertyName("startAt")] public string StartAt { get; set; }
        [JsonPropertyName("endAt")] public string EndAt { get; set; }
        [JsonPropertyName("note")] public string Note { get; set; }
        [JsonPropertyName("videoPublishedAt")] public DateTime? VideoPublishedAt { get; set; }
    }

    public sealed class YouTubePlaylistItemStatus
    {
        [JsonPropertyName("privacyStatus")] public string PrivacyStatus { get; set; }
    }

    // -------------------------------------------------------
    // Comment Thread
    // -------------------------------------------------------
    public sealed class YouTubeCommentThread : YouTubeEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("snippet")] public YouTubeCommentThreadSnippet Snippet { get; set; }
        [JsonPropertyName("replies")] public YouTubeCommentThreadReplies Replies { get; set; }
    }

    public sealed class YouTubeCommentThreadSnippet
    {
        [JsonPropertyName("channelId")] public string ChannelId { get; set; }
        [JsonPropertyName("videoId")] public string VideoId { get; set; }
        [JsonPropertyName("topLevelComment")] public YouTubeComment TopLevelComment { get; set; }
        [JsonPropertyName("canReply")] public bool? CanReply { get; set; }
        [JsonPropertyName("totalReplyCount")] public uint? TotalReplyCount { get; set; }
        [JsonPropertyName("isPublic")] public bool? IsPublic { get; set; }
    }

    public sealed class YouTubeCommentThreadReplies
    {
        [JsonPropertyName("comments")] public List<YouTubeComment> Comments { get; set; } = new();
    }

    // -------------------------------------------------------
    // Comment
    // -------------------------------------------------------
    public sealed class YouTubeComment : YouTubeEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("snippet")] public YouTubeCommentSnippet Snippet { get; set; }
    }

    public sealed class YouTubeCommentSnippet
    {
        [JsonPropertyName("authorDisplayName")] public string AuthorDisplayName { get; set; }
        [JsonPropertyName("authorProfileImageUrl")] public string AuthorProfileImageUrl { get; set; }
        [JsonPropertyName("authorChannelUrl")] public string AuthorChannelUrl { get; set; }
        [JsonPropertyName("authorChannelId")] public YouTubeCommentAuthorChannelId AuthorChannelId { get; set; }
        [JsonPropertyName("channelId")] public string ChannelId { get; set; }
        [JsonPropertyName("videoId")] public string VideoId { get; set; }
        [JsonPropertyName("textDisplay")] public string TextDisplay { get; set; }
        [JsonPropertyName("textOriginal")] public string TextOriginal { get; set; }
        [JsonPropertyName("parentId")] public string ParentId { get; set; }
        [JsonPropertyName("canRate")] public bool? CanRate { get; set; }
        [JsonPropertyName("viewerRating")] public string ViewerRating { get; set; }
        [JsonPropertyName("likeCount")] public uint? LikeCount { get; set; }
        [JsonPropertyName("moderationStatus")] public string ModerationStatus { get; set; }
        [JsonPropertyName("publishedAt")] public DateTime? PublishedAt { get; set; }
        [JsonPropertyName("updatedAt")] public DateTime? UpdatedAt { get; set; }
    }

    public sealed class YouTubeCommentAuthorChannelId
    {
        [JsonPropertyName("value")] public string Value { get; set; }
    }

    // -------------------------------------------------------
    // Search Result
    // -------------------------------------------------------
    public sealed class YouTubeSearchResult : YouTubeEntityBase
    {
        [JsonPropertyName("kind")] public string Kind { get; set; }
        [JsonPropertyName("etag")] public string Etag { get; set; }
        [JsonPropertyName("id")] public YouTubeResourceId Id { get; set; }
        [JsonPropertyName("snippet")] public YouTubeSearchResultSnippet Snippet { get; set; }
    }

    public sealed class YouTubeSearchResultSnippet
    {
        [JsonPropertyName("publishedAt")] public DateTime? PublishedAt { get; set; }
        [JsonPropertyName("channelId")] public string ChannelId { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("thumbnails")] public YouTubeThumbnails Thumbnails { get; set; }
        [JsonPropertyName("channelTitle")] public string ChannelTitle { get; set; }
        [JsonPropertyName("liveBroadcastContent")] public string LiveBroadcastContent { get; set; }
    }

    // -------------------------------------------------------
    // Analytics
    // -------------------------------------------------------
    public sealed class YouTubeAnalytics : YouTubeEntityBase
    {
        [JsonPropertyName("channelId")] public string ChannelId { get; set; }
        [JsonPropertyName("videoId")] public string VideoId { get; set; }
        [JsonPropertyName("date")] public DateTime? Date { get; set; }
        [JsonPropertyName("views")] public ulong? Views { get; set; }
        [JsonPropertyName("estimatedMinutesWatched")] public ulong? EstimatedMinutesWatched { get; set; }
        [JsonPropertyName("averageViewDuration")] public ulong? AverageViewDuration { get; set; }
        [JsonPropertyName("subscribersGained")] public long? SubscribersGained { get; set; }
        [JsonPropertyName("subscribersLost")] public long? SubscribersLost { get; set; }
        [JsonPropertyName("likes")] public ulong? Likes { get; set; }
        [JsonPropertyName("dislikes")] public ulong? Dislikes { get; set; }
        [JsonPropertyName("comments")] public ulong? Comments { get; set; }
        [JsonPropertyName("shares")] public ulong? Shares { get; set; }
        [JsonPropertyName("annotationClickThroughRate")] public double? AnnotationClickThroughRate { get; set; }
        [JsonPropertyName("annotationCloseRate")] public double? AnnotationCloseRate { get; set; }
        [JsonPropertyName("annotationImpressions")] public ulong? AnnotationImpressions { get; set; }
        [JsonPropertyName("annotationClickableImpressions")] public ulong? AnnotationClickableImpressions { get; set; }
        [JsonPropertyName("annotationClicks")] public ulong? AnnotationClicks { get; set; }
        [JsonPropertyName("annotationCloses")] public ulong? AnnotationCloses { get; set; }
        [JsonPropertyName("cardClickRate")] public double? CardClickRate { get; set; }
        [JsonPropertyName("cardTeaserClickRate")] public double? CardTeaserClickRate { get; set; }
        [JsonPropertyName("cardImpressions")] public ulong? CardImpressions { get; set; }
        [JsonPropertyName("cardTeaserImpressions")] public ulong? CardTeaserImpressions { get; set; }
        [JsonPropertyName("cardClicks")] public ulong? CardClicks { get; set; }
        [JsonPropertyName("cardTeaserClicks")] public ulong? CardTeaserClicks { get; set; }
        [JsonPropertyName("subscribersGainedFromCard")] public long? SubscribersGainedFromCard { get; set; }
        [JsonPropertyName("subscribersGainedFromTeaser")] public long? SubscribersGainedFromTeaser { get; set; }
        [JsonPropertyName("endScreenElementClickRate")] public double? EndScreenElementClickRate { get; set; }
        [JsonPropertyName("endScreenElementCloseRate")] public double? EndScreenElementCloseRate { get; set; }
        [JsonPropertyName("endScreenElementImpressions")] public ulong? EndScreenElementImpressions { get; set; }
        [JsonPropertyName("endScreenElementClicks")] public ulong? EndScreenElementClicks { get; set; }
        [JsonPropertyName("endScreenElementCloses")] public ulong? EndScreenElementCloses { get; set; }
        [JsonPropertyName("subscribersGainedFromEndScreen")] public long? SubscribersGainedFromEndScreen { get; set; }
    }

    // -------------------------------------------------------
    // Subscription
    // -------------------------------------------------------
    public sealed class YouTubeSubscription : YouTubeEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("snippet")] public YouTubeSubscriptionSnippet Snippet { get; set; }
        [JsonPropertyName("contentDetails")] public YouTubeSubscriptionContentDetails ContentDetails { get; set; }
        [JsonPropertyName("subscriberSnippet")] public YouTubeSubscriptionSubscriberSnippet SubscriberSnippet { get; set; }
    }

    public sealed class YouTubeSubscriptionSnippet
    {
        [JsonPropertyName("publishedAt")] public DateTime? PublishedAt { get; set; }
        [JsonPropertyName("channelTitle")] public string ChannelTitle { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("resourceId")] public YouTubeResourceId ResourceId { get; set; }
        [JsonPropertyName("channelId")] public string ChannelId { get; set; }
        [JsonPropertyName("thumbnails")] public YouTubeThumbnails Thumbnails { get; set; }
    }

    public sealed class YouTubeSubscriptionContentDetails
    {
        [JsonPropertyName("totalItemCount")] public uint? TotalItemCount { get; set; }
        [JsonPropertyName("newItemCount")] public uint? NewItemCount { get; set; }
        [JsonPropertyName("activityType")] public string ActivityType { get; set; }
    }

    public sealed class YouTubeSubscriptionSubscriberSnippet
    {
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("channelId")] public string ChannelId { get; set; }
        [JsonPropertyName("thumbnails")] public YouTubeThumbnails Thumbnails { get; set; }
    }

    // -------------------------------------------------------
    // Activity
    // -------------------------------------------------------
    public sealed class YouTubeActivity : YouTubeEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("snippet")] public YouTubeActivitySnippet Snippet { get; set; }
        [JsonPropertyName("contentDetails")] public YouTubeActivityContentDetails ContentDetails { get; set; }
    }

    public sealed class YouTubeActivitySnippet
    {
        [JsonPropertyName("publishedAt")] public DateTime? PublishedAt { get; set; }
        [JsonPropertyName("channelId")] public string ChannelId { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("thumbnails")] public YouTubeThumbnails Thumbnails { get; set; }
        [JsonPropertyName("channelTitle")] public string ChannelTitle { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("groupId")] public string GroupId { get; set; }
    }

    public sealed class YouTubeActivityContentDetails
    {
        [JsonPropertyName("upload")] public YouTubeActivityContentDetailsUpload Upload { get; set; }
        [JsonPropertyName("like")] public YouTubeActivityContentDetailsLike Like { get; set; }
        [JsonPropertyName("favorite")] public YouTubeActivityContentDetailsFavorite Favorite { get; set; }
        [JsonPropertyName("comment")] public YouTubeActivityContentDetailsComment Comment { get; set; }
        [JsonPropertyName("subscription")] public YouTubeActivityContentDetailsSubscription Subscription { get; set; }
        [JsonPropertyName("playlistItem")] public YouTubeActivityContentDetailsPlaylistItem PlaylistItem { get; set; }
        [JsonPropertyName("recommendation")] public YouTubeActivityContentDetailsRecommendation Recommendation { get; set; }
        [JsonPropertyName("social")] public YouTubeActivityContentDetailsSocial Social { get; set; }
        [JsonPropertyName("channelItem")] public YouTubeActivityContentDetailsChannelItem ChannelItem { get; set; }
        [JsonPropertyName("promotedItem")] public YouTubeActivityContentDetailsPromotedItem PromotedItem { get; set; }
    }

    public sealed class YouTubeActivityContentDetailsUpload
    {
        [JsonPropertyName("videoId")] public string VideoId { get; set; }
    }

    public sealed class YouTubeActivityContentDetailsLike
    {
        [JsonPropertyName("resourceId")] public YouTubeResourceId ResourceId { get; set; }
    }

    public sealed class YouTubeActivityContentDetailsFavorite
    {
        [JsonPropertyName("resourceId")] public YouTubeResourceId ResourceId { get; set; }
    }

    public sealed class YouTubeActivityContentDetailsComment
    {
        [JsonPropertyName("resourceId")] public YouTubeResourceId ResourceId { get; set; }
    }

    public sealed class YouTubeActivityContentDetailsSubscription
    {
        [JsonPropertyName("resourceId")] public YouTubeResourceId ResourceId { get; set; }
    }

    public sealed class YouTubeActivityContentDetailsPlaylistItem
    {
        [JsonPropertyName("resourceId")] public YouTubeResourceId ResourceId { get; set; }
        [JsonPropertyName("playlistId")] public string PlaylistId { get; set; }
        [JsonPropertyName("playlistItemId")] public string PlaylistItemId { get; set; }
    }

    public sealed class YouTubeActivityContentDetailsRecommendation
    {
        [JsonPropertyName("resourceId")] public YouTubeResourceId ResourceId { get; set; }
        [JsonPropertyName("reason")] public string Reason { get; set; }
        [JsonPropertyName("seedResourceId")] public YouTubeResourceId SeedResourceId { get; set; }
    }

    public sealed class YouTubeActivityContentDetailsSocial
    {
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("resourceId")] public YouTubeResourceId ResourceId { get; set; }
        [JsonPropertyName("author")] public string Author { get; set; }
        [JsonPropertyName("referenceUrl")] public string ReferenceUrl { get; set; }
        [JsonPropertyName("imageUrl")] public string ImageUrl { get; set; }
    }

    public sealed class YouTubeActivityContentDetailsChannelItem
    {
        [JsonPropertyName("resourceId")] public YouTubeResourceId ResourceId { get; set; }
    }

    public sealed class YouTubeActivityContentDetailsPromotedItem
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("destinationUrl")] public string DestinationUrl { get; set; }
        [JsonPropertyName("clickTrackingUrl")] public string ClickTrackingUrl { get; set; }
        [JsonPropertyName("impressionTrackingUrl")] public string ImpressionTrackingUrl { get; set; }
    }

    // -------------------------------------------------------
    // Caption
    // -------------------------------------------------------
    public sealed class YouTubeCaption : YouTubeEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("snippet")] public YouTubeCaptionSnippet Snippet { get; set; }
    }

    public sealed class YouTubeCaptionSnippet
    {
        [JsonPropertyName("videoId")] public string VideoId { get; set; }
        [JsonPropertyName("lastUpdated")] public DateTime? LastUpdated { get; set; }
        [JsonPropertyName("trackKind")] public string TrackKind { get; set; }
        [JsonPropertyName("language")] public string Language { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("audioTrackType")] public string AudioTrackType { get; set; }
        [JsonPropertyName("isCC")] public bool? IsCC { get; set; }
        [JsonPropertyName("isLarge")] public bool? IsLarge { get; set; }
        [JsonPropertyName("isEasyReader")] public bool? IsEasyReader { get; set; }
        [JsonPropertyName("isDraft")] public bool? IsDraft { get; set; }
        [JsonPropertyName("isAutoSynced")] public bool? IsAutoSynced { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; }
        [JsonPropertyName("failureReason")] public string FailureReason { get; set; }
    }

    // -------------------------------------------------------
    // Channel Section
    // -------------------------------------------------------
    public sealed class YouTubeChannelSection : YouTubeEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("snippet")] public YouTubeChannelSectionSnippet Snippet { get; set; }
        [JsonPropertyName("contentDetails")] public YouTubeChannelSectionContentDetails ContentDetails { get; set; }
        [JsonPropertyName("localizations")] public Dictionary<string, YouTubeChannelSectionLocalization> Localizations { get; set; } = new();
    }

    public sealed class YouTubeChannelSectionSnippet
    {
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("channelId")] public string ChannelId { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("position")] public uint? Position { get; set; }
        [JsonPropertyName("style")] public string Style { get; set; }
        [JsonPropertyName("defaultLanguage")] public string DefaultLanguage { get; set; }
        [JsonPropertyName("localized")] public YouTubeChannelSectionLocalization Localized { get; set; }
    }

    public sealed class YouTubeChannelSectionContentDetails
    {
        [JsonPropertyName("playlists")] public List<string> Playlists { get; set; } = new();
        [JsonPropertyName("channels")] public List<string> Channels { get; set; } = new();
    }

    public sealed class YouTubeChannelSectionLocalization
    {
        [JsonPropertyName("title")] public string Title { get; set; }
    }

    // -------------------------------------------------------
    // I18n Language
    // -------------------------------------------------------
    public sealed class YouTubeI18nLanguage : YouTubeEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("snippet")] public YouTubeI18nLanguageSnippet Snippet { get; set; }
    }

    public sealed class YouTubeI18nLanguageSnippet
    {
        [JsonPropertyName("hl")] public string Hl { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
    }

    // -------------------------------------------------------
    // I18n Region
    // -------------------------------------------------------
    public sealed class YouTubeI18nRegion : YouTubeEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("snippet")] public YouTubeI18nRegionSnippet Snippet { get; set; }
    }

    public sealed class YouTubeI18nRegionSnippet
    {
        [JsonPropertyName("gl")] public string Gl { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
    }

    // -------------------------------------------------------
    // Video Category
    // -------------------------------------------------------
    public sealed class YouTubeVideoCategory : YouTubeEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("snippet")] public YouTubeVideoCategorySnippet Snippet { get; set; }
    }

    public sealed class YouTubeVideoCategorySnippet
    {
        [JsonPropertyName("channelId")] public string ChannelId { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("assignable")] public bool? Assignable { get; set; }
    }

    // -------------------------------------------------------
    // Guide Category
    // -------------------------------------------------------
    public sealed class YouTubeGuideCategory : YouTubeEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("snippet")] public YouTubeGuideCategorySnippet Snippet { get; set; }
    }

    public sealed class YouTubeGuideCategorySnippet
    {
        [JsonPropertyName("channelId")] public string ChannelId { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("assignable")] public bool? Assignable { get; set; }
    }
}