using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.TwitterDataSource.Entities
{
    /// <summary>
    /// Represents a Twitter Tweet
    /// </summary>
    public class Tweet
    {
        /// <summary>
        /// The unique identifier of the Tweet
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The text content of the Tweet
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// The date and time when the Tweet was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// The author of the Tweet
        /// </summary>
        public TwitterUser Author { get; set; }

        /// <summary>
        /// The metrics for the Tweet
        /// </summary>
        public TweetMetrics Metrics { get; set; }

        /// <summary>
        /// The Tweets referenced in this Tweet
        /// </summary>
        public List<Tweet> ReferencedTweets { get; set; } = new List<Tweet>();

        /// <summary>
        /// The media attached to the Tweet
        /// </summary>
        public List<Media> Media { get; set; } = new List<Media>();

        /// <summary>
        /// The entities in the Tweet (hashtags, mentions, URLs, etc.)
        /// </summary>
        public TweetEntities Entities { get; set; }

        /// <summary>
        /// The context annotations for the Tweet
        /// </summary>
        public List<ContextAnnotation> ContextAnnotations { get; set; } = new List<ContextAnnotation>();

        /// <summary>
        /// Whether the Tweet is a retweet
        /// </summary>
        public bool IsRetweet { get; set; }

        /// <summary>
        /// Whether the Tweet is a reply
        /// </summary>
        public bool IsReply { get; set; }

        /// <summary>
        /// Whether the Tweet is a quote
        /// </summary>
        public bool IsQuote { get; set; }

        /// <summary>
        /// The language of the Tweet
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        /// The source application used to post the Tweet
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Whether the Tweet is sensitive content
        /// </summary>
        public bool PossiblySensitive { get; set; }
    }
}
