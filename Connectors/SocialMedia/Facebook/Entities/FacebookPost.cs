using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.FacebookDataSource.Entities
{
    /// <summary>
    /// Represents a Facebook post
    /// </summary>
    public class FacebookPost
    {
        /// <summary>
        /// The post ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The post message content
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// The post creation time
        /// </summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// The user who created the post
        /// </summary>
        public FacebookUser From { get; set; }

        /// <summary>
        /// The post type (status, photo, video, link, etc.)
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The post status type
        /// </summary>
        public string StatusType { get; set; }

        /// <summary>
        /// The post link URL
        /// </summary>
        public string Link { get; set; }

        /// <summary>
        /// The post name/title
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The post caption
        /// </summary>
        public string Caption { get; set; }

        /// <summary>
        /// The post description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The post picture URL
        /// </summary>
        public string Picture { get; set; }

        /// <summary>
        /// The post full picture URL
        /// </summary>
        public string FullPicture { get; set; }

        /// <summary>
        /// The post source URL
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// The post icon URL
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// The post privacy settings
        /// </summary>
        public FacebookPrivacy Privacy { get; set; }

        /// <summary>
        /// The post updated time
        /// </summary>
        public DateTime UpdatedTime { get; set; }

        /// <summary>
        /// Whether the post is published
        /// </summary>
        public bool IsPublished { get; set; }

        /// <summary>
        /// Whether the post is hidden
        /// </summary>
        public bool IsHidden { get; set; }

        /// <summary>
        /// Whether the post is expired
        /// </summary>
        public bool IsExpired { get; set; }

        /// <summary>
        /// The post permalink URL
        /// </summary>
        public string PermalinkUrl { get; set; }

        /// <summary>
        /// The post story
        /// </summary>
        public string Story { get; set; }

        /// <summary>
        /// The post story tags
        /// </summary>
        public List<FacebookStoryTag> StoryTags { get; set; } = new();

        /// <summary>
        /// The post with tags
        /// </summary>
        public List<FacebookWithTag> WithTags { get; set; } = new();

        /// <summary>
        /// The post message tags
        /// </summary>
        public List<FacebookMessageTag> MessageTags { get; set; } = new();

        /// <summary>
        /// The post attachments
        /// </summary>
        public List<FacebookAttachment> Attachments { get; set; } = new();

        /// <summary>
        /// The post properties
        /// </summary>
        public List<FacebookProperty> Properties { get; set; } = new();

        /// <summary>
        /// The post insights/metrics
        /// </summary>
        public FacebookInsights Insights { get; set; }

        /// <summary>
        /// The post comments
        /// </summary>
        public List<FacebookComment> Comments { get; set; } = new();

        /// <summary>
        /// The post reactions/likes
        /// </summary>
        public FacebookReactions Reactions { get; set; }

        /// <summary>
        /// The post shares
        /// </summary>
        public FacebookShares Shares { get; set; }

        /// <summary>
        /// Custom fields for extensibility
        /// </summary>
        public Dictionary<string, object> CustomFields { get; set; } = new();
    }

    /// <summary>
    /// Represents Facebook privacy settings
    /// </summary>
    public class FacebookPrivacy
    {
        /// <summary>
        /// The privacy value
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// The privacy description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The privacy friends list (if applicable)
        /// </summary>
        public string Friends { get; set; }

        /// <summary>
        /// Whether the privacy allows certain actions
        /// </summary>
        public string Allow { get; set; }

        /// <summary>
        /// The privacy deny list
        /// </summary>
        public string Deny { get; set; }
    }

    /// <summary>
    /// Represents a story tag in a Facebook post
    /// </summary>
    public class FacebookStoryTag
    {
        /// <summary>
        /// The tag ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The tag name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The tag type
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The tag offset
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// The tag length
        /// </summary>
        public int Length { get; set; }
    }

    /// <summary>
    /// Represents a with tag in a Facebook post
    /// </summary>
    public class FacebookWithTag
    {
        /// <summary>
        /// The tag ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The tag name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The tag type
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The tag offset
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// The tag length
        /// </summary>
        public int Length { get; set; }
    }

    /// <summary>
    /// Represents a message tag in a Facebook post
    /// </summary>
    public class FacebookMessageTag
    {
        /// <summary>
        /// The tag ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The tag name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The tag type
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The tag offset
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// The tag length
        /// </summary>
        public int Length { get; set; }
    }

    /// <summary>
    /// Represents an attachment in a Facebook post
    /// </summary>
    public class FacebookAttachment
    {
        /// <summary>
        /// The attachment title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The attachment description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The attachment URL
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// The attachment type
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The attachment media
        /// </summary>
        public FacebookMedia Media { get; set; }

        /// <summary>
        /// The attachment target
        /// </summary>
        public FacebookTarget Target { get; set; }

        /// <summary>
        /// The attachment subattachments
        /// </summary>
        public List<FacebookSubAttachment> SubAttachments { get; set; } = new();
    }

    /// <summary>
    /// Represents media in a Facebook attachment
    /// </summary>
    public class FacebookMedia
    {
        /// <summary>
        /// The media image
        /// </summary>
        public FacebookImage Image { get; set; }
    }

    /// <summary>
    /// Represents an image in Facebook media
    /// </summary>
    public class FacebookImage
    {
        /// <summary>
        /// The image height
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// The image width
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// The image source URL
        /// </summary>
        public string Src { get; set; }
    }

    /// <summary>
    /// Represents a target in a Facebook attachment
    /// </summary>
    public class FacebookTarget
    {
        /// <summary>
        /// The target ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The target URL
        /// </summary>
        public string Url { get; set; }
    }

    /// <summary>
    /// Represents a subattachment in a Facebook attachment
    /// </summary>
    public class FacebookSubAttachment
    {
        /// <summary>
        /// The subattachment title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The subattachment description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The subattachment URL
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// The subattachment type
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The subattachment media
        /// </summary>
        public FacebookMedia Media { get; set; }

        /// <summary>
        /// The subattachment target
        /// </summary>
        public FacebookTarget Target { get; set; }
    }

    /// <summary>
    /// Represents a property in a Facebook post
    /// </summary>
    public class FacebookProperty
    {
        /// <summary>
        /// The property name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The property text
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// The property href
        /// </summary>
        public string Href { get; set; }
    }
}
