using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.FacebookDataSource.Entities
{
    /// <summary>
    /// Represents a Facebook comment
    /// </summary>
    public class FacebookComment
    {
        /// <summary>
        /// The comment ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The comment message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// The comment creation time
        /// </summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// The user who created the comment
        /// </summary>
        public FacebookUser From { get; set; }

        /// <summary>
        /// The comment likes count
        /// </summary>
        public int? LikeCount { get; set; }

        /// <summary>
        /// Whether the comment can be liked
        /// </summary>
        public bool? CanLike { get; set; }

        /// <summary>
        /// Whether the comment can be commented on
        /// </summary>
        public bool? CanComment { get; set; }

        /// <summary>
        /// Whether the comment can be hidden
        /// </summary>
        public bool? CanHide { get; set; }

        /// <summary>
        /// Whether the comment can be removed
        /// </summary>
        public bool? CanRemove { get; set; }

        /// <summary>
        /// Whether the comment can be replied to
        /// </summary>
        public bool? CanReply { get; set; }

        /// <summary>
        /// Whether the comment is hidden
        /// </summary>
        public bool? IsHidden { get; set; }

        /// <summary>
        /// Whether the comment is private
        /// </summary>
        public bool? IsPrivate { get; set; }

        /// <summary>
        /// The comment permalink URL
        /// </summary>
        public string PermalinkUrl { get; set; }

        /// <summary>
        /// The comment attachment
        /// </summary>
        public FacebookAttachment Attachment { get; set; }

        /// <summary>
        /// The comment parent ID (for replies)
        /// </summary>
        public string ParentId { get; set; }

        /// <summary>
        /// The comment object ID (post/page ID)
        /// </summary>
        public string ObjectId { get; set; }

        /// <summary>
        /// The comment user likes status
        /// </summary>
        public bool? UserLikes { get; set; }

        /// <summary>
        /// The comment message tags
        /// </summary>
        public List<FacebookMessageTag> MessageTags { get; set; } = new();

        /// <summary>
        /// The comment replies
        /// </summary>
        public List<FacebookComment> Comments { get; set; } = new();

        /// <summary>
        /// Custom fields for extensibility
        /// </summary>
        public Dictionary<string, object> CustomFields { get; set; } = new();
    }

    /// <summary>
    /// Represents Facebook insights/metrics
    /// </summary>
    public class FacebookInsights
    {
        /// <summary>
        /// The insights data
        /// </summary>
        public List<FacebookInsightData> Data { get; set; } = new();

        /// <summary>
        /// The insights paging information
        /// </summary>
        public FacebookPaging Paging { get; set; }
    }

    /// <summary>
    /// Represents Facebook insight data
    /// </summary>
    public class FacebookInsightData
    {
        /// <summary>
        /// The insight name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The insight period
        /// </summary>
        public string Period { get; set; }

        /// <summary>
        /// The insight values
        /// </summary>
        public List<FacebookInsightValue> Values { get; set; } = new();

        /// <summary>
        /// The insight title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The insight description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The insight ID
        /// </summary>
        public string Id { get; set; }
    }

    /// <summary>
    /// Represents Facebook insight value
    /// </summary>
    public class FacebookInsightValue
    {
        /// <summary>
        /// The value
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// The end time
        /// </summary>
        public DateTime EndTime { get; set; }
    }

    /// <summary>
    /// Represents Facebook paging information
    /// </summary>
    public class FacebookPaging
    {
        /// <summary>
        /// The cursors
        /// </summary>
        public FacebookCursors Cursors { get; set; }

        /// <summary>
        /// The previous page URL
        /// </summary>
        public string Previous { get; set; }

        /// <summary>
        /// The next page URL
        /// </summary>
        public string Next { get; set; }
    }

    /// <summary>
    /// Represents Facebook cursors
    /// </summary>
    public class FacebookCursors
    {
        /// <summary>
        /// The before cursor
        /// </summary>
        public string Before { get; set; }

        /// <summary>
        /// The after cursor
        /// </summary>
        public string After { get; set; }
    }

    /// <summary>
    /// Represents Facebook reactions
    /// </summary>
    public class FacebookReactions
    {
        /// <summary>
        /// The reactions data
        /// </summary>
        public List<FacebookReaction> Data { get; set; } = new();

        /// <summary>
        /// The reactions summary
        /// </summary>
        public FacebookReactionsSummary Summary { get; set; }

        /// <summary>
        /// The reactions paging information
        /// </summary>
        public FacebookPaging Paging { get; set; }
    }

    /// <summary>
    /// Represents a Facebook reaction
    /// </summary>
    public class FacebookReaction
    {
        /// <summary>
        /// The reaction ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The reaction name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The reaction type
        /// </summary>
        public string Type { get; set; }
    }

    /// <summary>
    /// Represents Facebook reactions summary
    /// </summary>
    public class FacebookReactionsSummary
    {
        /// <summary>
        /// The total count
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// The viewer reaction
        /// </summary>
        public string ViewerReaction { get; set; }
    }

    /// <summary>
    /// Represents Facebook shares
    /// </summary>
    public class FacebookShares
    {
        /// <summary>
        /// The shares count
        /// </summary>
        public int Count { get; set; }
    }

    /// <summary>
    /// Represents Facebook event
    /// </summary>
    public class FacebookEvent
    {
        /// <summary>
        /// The event ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The event name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The event description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The event start time
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// The event end time
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// The event timezone
        /// </summary>
        public string Timezone { get; set; }

        /// <summary>
        /// The event location
        /// </summary>
        public FacebookLocation Location { get; set; }

        /// <summary>
        /// The event venue
        /// </summary>
        public FacebookVenue Venue { get; set; }

        /// <summary>
        /// The event cover photo
        /// </summary>
        public FacebookCover Cover { get; set; }

        /// <summary>
        /// The event ticket URI
        /// </summary>
        public string TicketUri { get; set; }

        /// <summary>
        /// The event type
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The event category
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// The event owner
        /// </summary>
        public FacebookUser Owner { get; set; }

        /// <summary>
        /// The event parent group
        /// </summary>
        public FacebookGroup ParentGroup { get; set; }

        /// <summary>
        /// The event privacy
        /// </summary>
        public string Privacy { get; set; }

        /// <summary>
        /// The event updated time
        /// </summary>
        public DateTime UpdatedTime { get; set; }

        /// <summary>
        /// The event attending count
        /// </summary>
        public int? AttendingCount { get; set; }

        /// <summary>
        /// The event declined count
        /// </summary>
        public int? DeclinedCount { get; set; }

        /// <summary>
        /// The event interested count
        /// </summary>
        public int? InterestedCount { get; set; }

        /// <summary>
        /// The event maybe count
        /// </summary>
        public int? MaybeCount { get; set; }

        /// <summary>
        /// The event noreply count
        /// </summary>
        public int? NoreplyCount { get; set; }

        /// <summary>
        /// Whether the event can guests invite
        /// </summary>
        public bool? CanGuestsInvite { get; set; }

        /// <summary>
        /// Whether the event guest list enabled
        /// </summary>
        public bool? GuestListEnabled { get; set; }

        /// <summary>
        /// Whether the event is canceled
        /// </summary>
        public bool? IsCanceled { get; set; }

        /// <summary>
        /// Whether the event is draft
        /// </summary>
        public bool? IsDraft { get; set; }

        /// <summary>
        /// Whether the event is page owned
        /// </summary>
        public bool? IsPageOwned { get; set; }

        /// <summary>
        /// The event online event format
        /// </summary>
        public string OnlineEventFormat { get; set; }

        /// <summary>
        /// The event online event third party url
        /// </summary>
        public string OnlineEventThirdPartyUrl { get; set; }

        /// <summary>
        /// Custom fields for extensibility
        /// </summary>
        public Dictionary<string, object> CustomFields { get; set; } = new();
    }

    /// <summary>
    /// Represents Facebook venue
    /// </summary>
    public class FacebookVenue
    {
        /// <summary>
        /// The venue ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The venue name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The venue location
        /// </summary>
        public FacebookLocation Location { get; set; }

        /// <summary>
        /// The venue about
        /// </summary>
        public string About { get; set; }
    }

    /// <summary>
    /// Represents Facebook group
    /// </summary>
    public class FacebookGroup
    {
        /// <summary>
        /// The group ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The group name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The group description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The group owner
        /// </summary>
        public FacebookUser Owner { get; set; }

        /// <summary>
        /// The group privacy
        /// </summary>
        public string Privacy { get; set; }

        /// <summary>
        /// The group icon
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// The group cover
        /// </summary>
        public FacebookCover Cover { get; set; }

        /// <summary>
        /// The group email
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// The group website
        /// </summary>
        public string Website { get; set; }

        /// <summary>
        /// The group member count
        /// </summary>
        public long? MemberCount { get; set; }

        /// <summary>
        /// The group member request count
        /// </summary>
        public long? MemberRequestCount { get; set; }

        /// <summary>
        /// The group updated time
        /// </summary>
        public DateTime UpdatedTime { get; set; }

        /// <summary>
        /// The group archived status
        /// </summary>
        public bool? Archived { get; set; }

        /// <summary>
        /// Custom fields for extensibility
        /// </summary>
        public Dictionary<string, object> CustomFields { get; set; } = new();
    }

    /// <summary>
    /// Represents Facebook photo
    /// </summary>
    public class FacebookPhoto
    {
        /// <summary>
        /// The photo ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The photo name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The photo source URL
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// The photo height
        /// </summary>
        public int? Height { get; set; }

        /// <summary>
        /// The photo width
        /// </summary>
        public int? Width { get; set; }

        /// <summary>
        /// The photo images
        /// </summary>
        public List<FacebookImage> Images { get; set; } = new();

        /// <summary>
        /// The photo link
        /// </summary>
        public string Link { get; set; }

        /// <summary>
        /// The photo picture URL
        /// </summary>
        public string Picture { get; set; }

        /// <summary>
        /// The photo created time
        /// </summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// The photo updated time
        /// </summary>
        public DateTime UpdatedTime { get; set; }

        /// <summary>
        /// The photo from user
        /// </summary>
        public FacebookUser From { get; set; }

        /// <summary>
        /// The photo album
        /// </summary>
        public FacebookAlbum Album { get; set; }

        /// <summary>
        /// The photo backdated time
        /// </summary>
        public DateTime? BackdatedTime { get; set; }

        /// <summary>
        /// The photo backdated time granularity
        /// </summary>
        public string BackdatedTimeGranularity { get; set; }

        /// <summary>
        /// The photo can delete
        /// </summary>
        public bool? CanDelete { get; set; }

        /// <summary>
        /// The photo can tag
        /// </summary>
        public bool? CanTag { get; set; }

        /// <summary>
        /// The photo event
        /// </summary>
        public FacebookEvent Event { get; set; }

        /// <summary>
        /// The photo icon
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// The photo page story ID
        /// </summary>
        public string PageStoryId { get; set; }

        /// <summary>
        /// The photo place
        /// </summary>
        public FacebookPlace Place { get; set; }

        /// <summary>
        /// The photo target
        /// </summary>
        public FacebookTarget Target { get; set; }

        /// <summary>
        /// The photo webp images
        /// </summary>
        public List<FacebookImage> WebpImages { get; set; } = new();

        /// <summary>
        /// Custom fields for extensibility
        /// </summary>
        public Dictionary<string, object> CustomFields { get; set; } = new();
    }

    /// <summary>
    /// Represents Facebook album
    /// </summary>
    public class FacebookAlbum
    {
        /// <summary>
        /// The album ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The album name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The album description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The album location
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// The album link
        /// </summary>
        public string Link { get; set; }

        /// <summary>
        /// The album cover photo
        /// </summary>
        public string CoverPhoto { get; set; }

        /// <summary>
        /// The album privacy
        /// </summary>
        public string Privacy { get; set; }

        /// <summary>
        /// The album count
        /// </summary>
        public int? Count { get; set; }

        /// <summary>
        /// The album type
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The album created time
        /// </summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// The album updated time
        /// </summary>
        public DateTime UpdatedTime { get; set; }

        /// <summary>
        /// The album from user
        /// </summary>
        public FacebookUser From { get; set; }

        /// <summary>
        /// The album place
        /// </summary>
        public FacebookPlace Place { get; set; }

        /// <summary>
        /// The album can upload
        /// </summary>
        public bool? CanUpload { get; set; }

        /// <summary>
        /// Custom fields for extensibility
        /// </summary>
        public Dictionary<string, object> CustomFields { get; set; } = new();
    }

    /// <summary>
    /// Represents Facebook place
    /// </summary>
    public class FacebookPlace
    {
        /// <summary>
        /// The place ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The place name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The place location
        /// </summary>
        public FacebookLocation Location { get; set; }

        /// <summary>
        /// The place overall rating
        /// </summary>
        public double? OverallRating { get; set; }
    }
}
