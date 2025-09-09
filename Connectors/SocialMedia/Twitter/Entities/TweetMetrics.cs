namespace TheTechIdea.Beep.TwitterDataSource.Entities
{
    /// <summary>
    /// Represents the metrics for a Tweet
    /// </summary>
    public class TweetMetrics
    {
        /// <summary>
        /// The number of times the Tweet has been retweeted
        /// </summary>
        public int RetweetCount { get; set; }

        /// <summary>
        /// The number of times the Tweet has been liked
        /// </summary>
        public int LikeCount { get; set; }

        /// <summary>
        /// The number of times the Tweet has been replied to
        /// </summary>
        public int ReplyCount { get; set; }

        /// <summary>
        /// The number of times the Tweet has been quoted
        /// </summary>
        public int QuoteCount { get; set; }

        /// <summary>
        /// The number of times the Tweet has been viewed
        /// </summary>
        public int ImpressionCount { get; set; }
    }
}
