namespace TheTechIdea.Beep.TwitterDataSource.Entities
{
    /// <summary>
    /// Represents the metrics for a Twitter User
    /// </summary>
    public class UserMetrics
    {
        /// <summary>
        /// The number of followers the User has
        /// </summary>
        public int FollowersCount { get; set; }

        /// <summary>
        /// The number of users the User is following
        /// </summary>
        public int FollowingCount { get; set; }

        /// <summary>
        /// The number of Tweets the User has posted
        /// </summary>
        public int TweetCount { get; set; }

        /// <summary>
        /// The number of lists the User is a member of
        /// </summary>
        public int ListedCount { get; set; }
    }
}
