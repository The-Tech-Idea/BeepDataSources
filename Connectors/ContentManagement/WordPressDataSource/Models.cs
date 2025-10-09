using System.Text.Json.Serialization;

namespace TheTechIdea.Beep.Connectors.WordPress.Models
{
    // WordPress REST API Models
    public class WordPressPost
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("date_gmt")]
        public DateTime DateGmt { get; set; }

        [JsonPropertyName("guid")]
        public WordPressGuid Guid { get; set; }

        [JsonPropertyName("modified")]
        public DateTime Modified { get; set; }

        [JsonPropertyName("modified_gmt")]
        public DateTime ModifiedGmt { get; set; }

        [JsonPropertyName("slug")]
        public string Slug { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("link")]
        public string Link { get; set; }

        [JsonPropertyName("title")]
        public WordPressTitle Title { get; set; }

        [JsonPropertyName("content")]
        public WordPressContent Content { get; set; }

        [JsonPropertyName("excerpt")]
        public WordPressExcerpt Excerpt { get; set; }

        [JsonPropertyName("author")]
        public int Author { get; set; }

        [JsonPropertyName("featured_media")]
        public int FeaturedMedia { get; set; }

        [JsonPropertyName("comment_status")]
        public string CommentStatus { get; set; }

        [JsonPropertyName("ping_status")]
        public string PingStatus { get; set; }

        [JsonPropertyName("sticky")]
        public bool Sticky { get; set; }

        [JsonPropertyName("template")]
        public string Template { get; set; }

        [JsonPropertyName("format")]
        public string Format { get; set; }

        [JsonPropertyName("meta")]
        public Dictionary<string, object> Meta { get; set; }

        [JsonPropertyName("categories")]
        public List<int> Categories { get; set; }

        [JsonPropertyName("tags")]
        public List<int> Tags { get; set; }

        [JsonPropertyName("_links")]
        public WordPressLinks Links { get; set; }
    }

    public class WordPressGuid
    {
        [JsonPropertyName("rendered")]
        public string Rendered { get; set; }
    }

    public class WordPressTitle
    {
        [JsonPropertyName("rendered")]
        public string Rendered { get; set; }
    }

    public class WordPressContent
    {
        [JsonPropertyName("rendered")]
        public string Rendered { get; set; }

        [JsonPropertyName("protected")]
        public bool Protected { get; set; }
    }

    public class WordPressExcerpt
    {
        [JsonPropertyName("rendered")]
        public string Rendered { get; set; }

        [JsonPropertyName("protected")]
        public bool Protected { get; set; }
    }

    public class WordPressPage
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("date_gmt")]
        public DateTime DateGmt { get; set; }

        [JsonPropertyName("guid")]
        public WordPressGuid Guid { get; set; }

        [JsonPropertyName("modified")]
        public DateTime Modified { get; set; }

        [JsonPropertyName("modified_gmt")]
        public DateTime ModifiedGmt { get; set; }

        [JsonPropertyName("slug")]
        public string Slug { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("link")]
        public string Link { get; set; }

        [JsonPropertyName("title")]
        public WordPressTitle Title { get; set; }

        [JsonPropertyName("content")]
        public WordPressContent Content { get; set; }

        [JsonPropertyName("excerpt")]
        public WordPressExcerpt Excerpt { get; set; }

        [JsonPropertyName("author")]
        public int Author { get; set; }

        [JsonPropertyName("featured_media")]
        public int FeaturedMedia { get; set; }

        [JsonPropertyName("parent")]
        public int Parent { get; set; }

        [JsonPropertyName("menu_order")]
        public int MenuOrder { get; set; }

        [JsonPropertyName("comment_status")]
        public string CommentStatus { get; set; }

        [JsonPropertyName("ping_status")]
        public string PingStatus { get; set; }

        [JsonPropertyName("template")]
        public string Template { get; set; }

        [JsonPropertyName("meta")]
        public Dictionary<string, object> Meta { get; set; }

        [JsonPropertyName("_links")]
        public WordPressLinks Links { get; set; }
    }

    public class WordPressUser
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("link")]
        public string Link { get; set; }

        [JsonPropertyName("slug")]
        public string Slug { get; set; }

        [JsonPropertyName("avatar_urls")]
        public Dictionary<string, string> AvatarUrls { get; set; }

        [JsonPropertyName("meta")]
        public Dictionary<string, object> Meta { get; set; }

        [JsonPropertyName("_links")]
        public WordPressLinks Links { get; set; }
    }

    public class WordPressComment
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("post")]
        public int Post { get; set; }

        [JsonPropertyName("parent")]
        public int Parent { get; set; }

        [JsonPropertyName("author")]
        public int Author { get; set; }

        [JsonPropertyName("author_name")]
        public string AuthorName { get; set; }

        [JsonPropertyName("author_url")]
        public string AuthorUrl { get; set; }

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("date_gmt")]
        public DateTime DateGmt { get; set; }

        [JsonPropertyName("content")]
        public WordPressContent Content { get; set; }

        [JsonPropertyName("link")]
        public string Link { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("author_avatar_urls")]
        public Dictionary<string, string> AuthorAvatarUrls { get; set; }

        [JsonPropertyName("meta")]
        public Dictionary<string, object> Meta { get; set; }

        [JsonPropertyName("_links")]
        public WordPressLinks Links { get; set; }
    }

    public class WordPressCategory
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("link")]
        public string Link { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("slug")]
        public string Slug { get; set; }

        [JsonPropertyName("taxonomy")]
        public string Taxonomy { get; set; }

        [JsonPropertyName("parent")]
        public int Parent { get; set; }

        [JsonPropertyName("meta")]
        public Dictionary<string, object> Meta { get; set; }

        [JsonPropertyName("_links")]
        public WordPressLinks Links { get; set; }
    }

    public class WordPressTag
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("link")]
        public string Link { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("slug")]
        public string Slug { get; set; }

        [JsonPropertyName("taxonomy")]
        public string Taxonomy { get; set; }

        [JsonPropertyName("meta")]
        public Dictionary<string, object> Meta { get; set; }

        [JsonPropertyName("_links")]
        public WordPressLinks Links { get; set; }
    }

    public class WordPressMedia
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("date_gmt")]
        public DateTime DateGmt { get; set; }

        [JsonPropertyName("guid")]
        public WordPressGuid Guid { get; set; }

        [JsonPropertyName("modified")]
        public DateTime Modified { get; set; }

        [JsonPropertyName("modified_gmt")]
        public DateTime ModifiedGmt { get; set; }

        [JsonPropertyName("slug")]
        public string Slug { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("link")]
        public string Link { get; set; }

        [JsonPropertyName("title")]
        public WordPressTitle Title { get; set; }

        [JsonPropertyName("author")]
        public int Author { get; set; }

        [JsonPropertyName("comment_status")]
        public string CommentStatus { get; set; }

        [JsonPropertyName("ping_status")]
        public string PingStatus { get; set; }

        [JsonPropertyName("template")]
        public string Template { get; set; }

        [JsonPropertyName("meta")]
        public Dictionary<string, object> Meta { get; set; }

        [JsonPropertyName("description")]
        public WordPressContent Description { get; set; }

        [JsonPropertyName("caption")]
        public WordPressContent Caption { get; set; }

        [JsonPropertyName("alt_text")]
        public string AltText { get; set; }

        [JsonPropertyName("media_type")]
        public string MediaType { get; set; }

        [JsonPropertyName("mime_type")]
        public string MimeType { get; set; }

        [JsonPropertyName("media_details")]
        public WordPressMediaDetails MediaDetails { get; set; }

        [JsonPropertyName("post")]
        public int Post { get; set; }

        [JsonPropertyName("source_url")]
        public string SourceUrl { get; set; }

        [JsonPropertyName("_links")]
        public WordPressLinks Links { get; set; }
    }

    public class WordPressMediaDetails
    {
        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("file")]
        public string File { get; set; }

        [JsonPropertyName("sizes")]
        public Dictionary<string, WordPressMediaSize> Sizes { get; set; }
    }

    public class WordPressMediaSize
    {
        [JsonPropertyName("file")]
        public string File { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("mime_type")]
        public string MimeType { get; set; }

        [JsonPropertyName("source_url")]
        public string SourceUrl { get; set; }
    }

    public class WordPressLinks
    {
        [JsonPropertyName("self")]
        public List<WordPressLink> Self { get; set; }

        [JsonPropertyName("collection")]
        public List<WordPressLink> Collection { get; set; }

        [JsonPropertyName("about")]
        public List<WordPressLink> About { get; set; }

        [JsonPropertyName("author")]
        public List<WordPressLink> Author { get; set; }

        [JsonPropertyName("replies")]
        public List<WordPressLink> Replies { get; set; }

        [JsonPropertyName("version-history")]
        public List<WordPressLink> VersionHistory { get; set; }

        [JsonPropertyName("predecessor-version")]
        public List<WordPressLink> PredecessorVersion { get; set; }

        [JsonPropertyName("wp:attachment")]
        public List<WordPressLink> WpAttachment { get; set; }

        [JsonPropertyName("wp:term")]
        public List<WordPressLink> WpTerm { get; set; }

        [JsonPropertyName("curies")]
        public List<WordPressCuries> Curies { get; set; }
    }

    public class WordPressLink
    {
        [JsonPropertyName("href")]
        public string Href { get; set; }

        [JsonPropertyName("embeddable")]
        public bool Embeddable { get; set; }

        [JsonPropertyName("post_type")]
        public string PostType { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("templated")]
        public bool Templated { get; set; }
    }

    public class WordPressCuries
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("href")]
        public string Href { get; set; }

        [JsonPropertyName("templated")]
        public bool Templated { get; set; }
    }
}