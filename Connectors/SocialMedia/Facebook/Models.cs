using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.FacebookDataSource
{
    /// <summary>
    /// Base class for Facebook entities
    /// </summary>
    public abstract class FacebookEntityBase
    {
        [JsonIgnore] public IDataSource DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : FacebookEntityBase { DataSource = ds; return (T)this; }
    }

    /// <summary>
    /// Facebook Post entity
    /// </summary>
    public sealed class FacebookPost : FacebookEntityBase
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("story")]
        public string? Story { get; set; }

        [JsonPropertyName("created_time")]
        public DateTime? CreatedTime { get; set; }

        [JsonPropertyName("updated_time")]
        public DateTime? UpdatedTime { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("status_type")]
        public string? StatusType { get; set; }

        [JsonPropertyName("permalink_url")]
        public string? PermalinkUrl { get; set; }

        [JsonPropertyName("full_picture")]
        public string? FullPicture { get; set; }

        [JsonPropertyName("picture")]
        public string? Picture { get; set; }

        [JsonPropertyName("source")]
        public string? Source { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("caption")]
        public string? Caption { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("link")]
        public string? Link { get; set; }

        [JsonPropertyName("likes")]
        public FacebookLikes? Likes { get; set; }

        [JsonPropertyName("comments")]
        public FacebookComments? Comments { get; set; }

        [JsonPropertyName("shares")]
        public FacebookShares? Shares { get; set; }

        [JsonPropertyName("reactions")]
        public FacebookReactions? Reactions { get; set; }

        [JsonPropertyName("is_published")]
        public bool? IsPublished { get; set; }

        [JsonPropertyName("is_hidden")]
        public bool? IsHidden { get; set; }

        [JsonPropertyName("is_expired")]
        public bool? IsExpired { get; set; }

        [JsonPropertyName("scheduled_publish_time")]
        public DateTime? ScheduledPublishTime { get; set; }
    }

    /// <summary>
    /// Facebook Page entity
    /// </summary>
    public sealed class FacebookPage : FacebookEntityBase
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("about")]
        public string? About { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("website")]
        public string? Website { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("emails")]
        public List<string>? Emails { get; set; }

        [JsonPropertyName("location")]
        public FacebookLocation? Location { get; set; }

        [JsonPropertyName("hours")]
        public Dictionary<string, FacebookHourRange>? Hours { get; set; }

        [JsonPropertyName("parking")]
        public FacebookParking? Parking { get; set; }

        [JsonPropertyName("cover")]
        public FacebookCover? Cover { get; set; }

        [JsonPropertyName("picture")]
        public FacebookPicture? Picture { get; set; }

        [JsonPropertyName("likes")]
        public int? Likes { get; set; }

        [JsonPropertyName("followers_count")]
        public int? FollowersCount { get; set; }

        [JsonPropertyName("checkins")]
        public int? Checkins { get; set; }

        [JsonPropertyName("were_here_count")]
        public int? WereHereCount { get; set; }

        [JsonPropertyName("talking_about_count")]
        public int? TalkingAboutCount { get; set; }

        [JsonPropertyName("is_published")]
        public bool? IsPublished { get; set; }

        [JsonPropertyName("is_unclaimed")]
        public bool? IsUnclaimed { get; set; }

        [JsonPropertyName("is_permanently_closed")]
        public bool? IsPermanentlyClosed { get; set; }
    }

    /// <summary>
    /// Facebook Group entity
    /// </summary>
    public sealed class FacebookGroup : FacebookEntityBase
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("privacy")]
        public string? Privacy { get; set; }

        [JsonPropertyName("icon")]
        public string? Icon { get; set; }

        [JsonPropertyName("cover")]
        public FacebookCover? Cover { get; set; }

        [JsonPropertyName("member_count")]
        public int? MemberCount { get; set; }

        [JsonPropertyName("member_request_count")]
        public int? MemberRequestCount { get; set; }

        [JsonPropertyName("administrator")]
        public bool? Administrator { get; set; }

        [JsonPropertyName("moderator")]
        public bool? Moderator { get; set; }

        [JsonPropertyName("updated_time")]
        public DateTime? UpdatedTime { get; set; }

        [JsonPropertyName("archived")]
        public bool? Archived { get; set; }
    }

    /// <summary>
    /// Facebook Event entity
    /// </summary>
    public sealed class FacebookEvent : FacebookEntityBase
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("start_time")]
        public DateTime? StartTime { get; set; }

        [JsonPropertyName("end_time")]
        public DateTime? EndTime { get; set; }

        [JsonPropertyName("timezone")]
        public string? Timezone { get; set; }

        [JsonPropertyName("location")]
        public string? Location { get; set; }

        [JsonPropertyName("venue")]
        public FacebookVenue? Venue { get; set; }

        [JsonPropertyName("cover")]
        public FacebookCover? Cover { get; set; }

        [JsonPropertyName("picture")]
        public FacebookPicture? Picture { get; set; }

        [JsonPropertyName("attending_count")]
        public int? AttendingCount { get; set; }

        [JsonPropertyName("declined_count")]
        public int? DeclinedCount { get; set; }

        [JsonPropertyName("interested_count")]
        public int? InterestedCount { get; set; }

        [JsonPropertyName("noreply_count")]
        public int? NoreplyCount { get; set; }

        [JsonPropertyName("maybe_count")]
        public int? MaybeCount { get; set; }

        [JsonPropertyName("is_canceled")]
        public bool? IsCanceled { get; set; }

        [JsonPropertyName("ticket_uri")]
        public string? TicketUri { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }

    /// <summary>
    /// Facebook Ad entity
    /// </summary>
    public sealed class FacebookAd : FacebookEntityBase
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("creative")]
        public FacebookCreative? Creative { get; set; }

        [JsonPropertyName("created_time")]
        public DateTime? CreatedTime { get; set; }

        [JsonPropertyName("updated_time")]
        public DateTime? UpdatedTime { get; set; }

        [JsonPropertyName("campaign_id")]
        public string? CampaignId { get; set; }

        [JsonPropertyName("adset_id")]
        public string? AdsetId { get; set; }

        [JsonPropertyName("account_id")]
        public string? AccountId { get; set; }

        [JsonPropertyName("tracking_specs")]
        public List<FacebookTrackingSpec>? TrackingSpecs { get; set; }

        [JsonPropertyName("conversion_specs")]
        public List<FacebookConversionSpec>? ConversionSpecs { get; set; }

        [JsonPropertyName("bid_amount")]
        public int? BidAmount { get; set; }

        [JsonPropertyName("bid_type")]
        public string? BidType { get; set; }

        [JsonPropertyName("daily_budget")]
        public string? DailyBudget { get; set; }

        [JsonPropertyName("lifetime_budget")]
        public string? LifetimeBudget { get; set; }
    }

    /// <summary>
    /// Facebook Insights entity
    /// </summary>
    public sealed class FacebookInsight : FacebookEntityBase
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("period")]
        public string? Period { get; set; }

        [JsonPropertyName("values")]
        public List<FacebookInsightValue>? Values { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("account_id")]
        public string? AccountId { get; set; }

        [JsonPropertyName("campaign_id")]
        public string? CampaignId { get; set; }

        [JsonPropertyName("adset_id")]
        public string? AdsetId { get; set; }

        [JsonPropertyName("ad_id")]
        public string? AdId { get; set; }

        [JsonPropertyName("date_start")]
        public DateTime? DateStart { get; set; }

        [JsonPropertyName("date_stop")]
        public DateTime? DateStop { get; set; }
    }

    // Supporting classes for complex properties

    /// <summary>
    /// Facebook Likes summary
    /// </summary>
    public class FacebookLikes
    {
        [JsonPropertyName("summary")]
        public FacebookSummary? Summary { get; set; }

        [JsonPropertyName("data")]
        public List<FacebookLike>? Data { get; set; }
    }

    /// <summary>
    /// Facebook Comments summary
    /// </summary>
    public class FacebookComments
    {
        [JsonPropertyName("summary")]
        public FacebookSummary? Summary { get; set; }

        [JsonPropertyName("data")]
        public List<FacebookComment>? Data { get; set; }
    }

    /// <summary>
    /// Facebook Shares summary
    /// </summary>
    public class FacebookShares
    {
        [JsonPropertyName("count")]
        public int? Count { get; set; }
    }

    /// <summary>
    /// Facebook Reactions summary
    /// </summary>
    public class FacebookReactions
    {
        [JsonPropertyName("summary")]
        public FacebookSummary? Summary { get; set; }

        [JsonPropertyName("data")]
        public List<FacebookReaction>? Data { get; set; }
    }

    /// <summary>
    /// Facebook Summary for counts
    /// </summary>
    public class FacebookSummary
    {
        [JsonPropertyName("total_count")]
        public int? TotalCount { get; set; }

        [JsonPropertyName("can_like")]
        public bool? CanLike { get; set; }

        [JsonPropertyName("has_liked")]
        public bool? HasLiked { get; set; }
    }

    /// <summary>
    /// Facebook Like
    /// </summary>
    public class FacebookLike
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    /// <summary>
    /// Facebook Comment
    /// </summary>
    public class FacebookComment
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("created_time")]
        public DateTime? CreatedTime { get; set; }

        [JsonPropertyName("from")]
        public FacebookUser? From { get; set; }

        [JsonPropertyName("likes")]
        public FacebookLikes? Likes { get; set; }
    }

    /// <summary>
    /// Facebook Reaction
    /// </summary>
    public class FacebookReaction
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }

    /// <summary>
    /// Facebook User
    /// </summary>
    public sealed class FacebookUser : FacebookEntityBase
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }
    }

    /// <summary>
    /// Facebook Location
    /// </summary>
    public class FacebookLocation
    {
        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("latitude")]
        public double? Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double? Longitude { get; set; }

        [JsonPropertyName("street")]
        public string? Street { get; set; }

        [JsonPropertyName("zip")]
        public string? Zip { get; set; }
    }

    /// <summary>
    /// Facebook Hour Range
    /// </summary>
    public class FacebookHourRange
    {
        [JsonPropertyName("open")]
        public string? Open { get; set; }

        [JsonPropertyName("close")]
        public string? Close { get; set; }
    }

    /// <summary>
    /// Facebook Parking
    /// </summary>
    public class FacebookParking
    {
        [JsonPropertyName("lot")]
        public int? Lot { get; set; }

        [JsonPropertyName("street")]
        public int? Street { get; set; }

        [JsonPropertyName("valet")]
        public int? Valet { get; set; }
    }

    /// <summary>
    /// Facebook Cover
    /// </summary>
    public class FacebookCover
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("source")]
        public string? Source { get; set; }

        [JsonPropertyName("offset_x")]
        public int? OffsetX { get; set; }

        [JsonPropertyName("offset_y")]
        public int? OffsetY { get; set; }
    }

    /// <summary>
    /// Facebook Picture
    /// </summary>
    public class FacebookPicture
    {
        [JsonPropertyName("data")]
        public FacebookPictureData? Data { get; set; }
    }

    /// <summary>
    /// Facebook Picture Data
    /// </summary>
    public class FacebookPictureData
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("width")]
        public int? Width { get; set; }

        [JsonPropertyName("height")]
        public int? Height { get; set; }

        [JsonPropertyName("is_silhouette")]
        public bool? IsSilhouette { get; set; }
    }

    /// <summary>
    /// Facebook Venue
    /// </summary>
    public class FacebookVenue
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("location")]
        public FacebookLocation? Location { get; set; }
    }

    /// <summary>
    /// Facebook Creative
    /// </summary>
    public class FacebookCreative
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("body")]
        public string? Body { get; set; }

        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; set; }

        [JsonPropertyName("link_url")]
        public string? LinkUrl { get; set; }

        [JsonPropertyName("object_type")]
        public string? ObjectType { get; set; }
    }

    /// <summary>
    /// Facebook Tracking Spec
    /// </summary>
    public class FacebookTrackingSpec
    {
        [JsonPropertyName("action.type")]
        public List<string>? ActionType { get; set; }

        [JsonPropertyName("fb_pixel")]
        public List<string>? FbPixel { get; set; }

        [JsonPropertyName("application")]
        public List<string>? Application { get; set; }
    }

    /// <summary>
    /// Facebook Conversion Spec
    /// </summary>
    public class FacebookConversionSpec
    {
        [JsonPropertyName("action.type")]
        public List<string>? ActionType { get; set; }

        [JsonPropertyName("fb_pixel")]
        public List<string>? FbPixel { get; set; }

        [JsonPropertyName("application")]
        public List<string>? Application { get; set; }
    }

    /// <summary>
    /// Facebook Insight Value
    /// </summary>
    public class FacebookInsightValue
    {
        [JsonPropertyName("value")]
        public object? Value { get; set; }

        [JsonPropertyName("end_time")]
        public DateTime? EndTime { get; set; }
    }

    /// <summary>
    /// Facebook API response wrapper
    /// </summary>
    public class FacebookResponse<T>
    {
        [JsonPropertyName("data")]
        public List<T>? Data { get; set; }

        [JsonPropertyName("paging")]
        public FacebookPaging? Paging { get; set; }
    }

    /// <summary>
    /// Facebook paging information
    /// </summary>
    public class FacebookPaging
    {
        [JsonPropertyName("next")]
        public string? Next { get; set; }

        [JsonPropertyName("previous")]
        public string? Previous { get; set; }

        [JsonPropertyName("cursors")]
        public FacebookCursors? Cursors { get; set; }
    }

    /// <summary>
    /// Facebook cursors for pagination
    /// </summary>
    public class FacebookCursors
    {
        [JsonPropertyName("before")]
        public string? Before { get; set; }

        [JsonPropertyName("after")]
        public string? After { get; set; }
    }
}