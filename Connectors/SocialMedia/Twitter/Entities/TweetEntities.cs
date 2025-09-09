using System.Collections.Generic;

namespace TheTechIdea.Beep.TwitterDataSource.Entities
{
    /// <summary>
    /// Represents the entities in a Tweet
    /// </summary>
    public class TweetEntities
    {
        /// <summary>
        /// The hashtags in the Tweet
        /// </summary>
        public List<Hashtag> Hashtags { get; set; } = new List<Hashtag>();

        /// <summary>
        /// The mentions in the Tweet
        /// </summary>
        public List<Mention> Mentions { get; set; } = new List<Mention>();

        /// <summary>
        /// The URLs in the Tweet
        /// </summary>
        public List<Url> Urls { get; set; } = new List<Url>();

        /// <summary>
        /// The cashtags in the Tweet
        /// </summary>
        public List<Cashtag> Cashtags { get; set; } = new List<Cashtag>();
    }

    /// <summary>
    /// Represents a hashtag entity
    /// </summary>
    public class Hashtag
    {
        /// <summary>
        /// The start position of the hashtag in the Tweet text
        /// </summary>
        public int Start { get; set; }

        /// <summary>
        /// The end position of the hashtag in the Tweet text
        /// </summary>
        public int End { get; set; }

        /// <summary>
        /// The text of the hashtag
        /// </summary>
        public string Tag { get; set; }
    }

    /// <summary>
    /// Represents a mention entity
    /// </summary>
    public class Mention
    {
        /// <summary>
        /// The start position of the mention in the Tweet text
        /// </summary>
        public int Start { get; set; }

        /// <summary>
        /// The end position of the mention in the Tweet text
        /// </summary>
        public int End { get; set; }

        /// <summary>
        /// The username of the mentioned user
        /// </summary>
        public string Username { get; set; }
    }

    /// <summary>
    /// Represents a URL entity
    /// </summary>
    public class Url
    {
        /// <summary>
        /// The start position of the URL in the Tweet text
        /// </summary>
        public int Start { get; set; }

        /// <summary>
        /// The end position of the URL in the Tweet text
        /// </summary>
        public int End { get; set; }

        /// <summary>
        /// The URL
        /// </summary>
        public string UrlValue { get; set; }

        /// <summary>
        /// The expanded URL
        /// </summary>
        public string ExpandedUrl { get; set; }

        /// <summary>
        /// The display URL
        /// </summary>
        public string DisplayUrl { get; set; }
    }

    /// <summary>
    /// Represents a cashtag entity
    /// </summary>
    public class Cashtag
    {
        /// <summary>
        /// The start position of the cashtag in the Tweet text
        /// </summary>
        public int Start { get; set; }

        /// <summary>
        /// The end position of the cashtag in the Tweet text
        /// </summary>
        public int End { get; set; }

        /// <summary>
        /// The text of the cashtag
        /// </summary>
        public string Tag { get; set; }
    }
}
