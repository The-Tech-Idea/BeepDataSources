using System.Collections.Generic;

namespace TheTechIdea.Beep.TwitterDataSource.Entities
{
    /// <summary>
    /// Represents media attached to a Tweet
    /// </summary>
    public class Media
    {
        /// <summary>
        /// The unique identifier of the Media
        /// </summary>
        public string MediaKey { get; set; }

        /// <summary>
        /// The type of media (photo, video, animated_gif)
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The URL of the media
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// The preview image URL for video and GIF media
        /// </summary>
        public string PreviewImageUrl { get; set; }

        /// <summary>
        /// The duration of the video or GIF in milliseconds
        /// </summary>
        public int? DurationMs { get; set; }

        /// <summary>
        /// The height of the media in pixels
        /// </summary>
        public int? Height { get; set; }

        /// <summary>
        /// The width of the media in pixels
        /// </summary>
        public int? Width { get; set; }

        /// <summary>
        /// The alt text for the media
        /// </summary>
        public string AltText { get; set; }

        /// <summary>
        /// The variants for video media
        /// </summary>
        public List<MediaVariant> Variants { get; set; } = new List<MediaVariant>();
    }

    /// <summary>
    /// Represents a variant of media (for videos)
    /// </summary>
    public class MediaVariant
    {
        /// <summary>
        /// The bitrate of the variant
        /// </summary>
        public int? Bitrate { get; set; }

        /// <summary>
        /// The content type of the variant
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// The URL of the variant
        /// </summary>
        public string Url { get; set; }
    }
}
