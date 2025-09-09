using System;

namespace TheTechIdea.Beep.TwitterDataSource.Entities
{
    /// <summary>
    /// Represents a Twitter User
    /// </summary>
    public class TwitterUser
    {
        /// <summary>
        /// The unique identifier of the User
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The username of the User
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// The name of the User
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The description of the User
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The date and time when the User account was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// The metrics for the User
        /// </summary>
        public UserMetrics Metrics { get; set; }

        /// <summary>
        /// Whether the User is verified
        /// </summary>
        public bool Verified { get; set; }

        /// <summary>
        /// Whether the User is protected
        /// </summary>
        public bool Protected { get; set; }

        /// <summary>
        /// The location of the User
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// The URL of the User's profile
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// The profile image URL of the User
        /// </summary>
        public string ProfileImageUrl { get; set; }

        /// <summary>
        /// The profile banner URL of the User
        /// </summary>
        public string ProfileBannerUrl { get; set; }

        /// <summary>
        /// The pinned Tweet ID of the User
        /// </summary>
        public string PinnedTweetId { get; set; }
    }
}
