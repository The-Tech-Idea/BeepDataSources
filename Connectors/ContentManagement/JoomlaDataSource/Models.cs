using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TheTechIdea.Beep.Connectors.Joomla.Models
{
    /// <summary>
    /// Joomla Article (equivalent to WordPress Post)
    /// </summary>
    public class JoomlaArticle
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("alias")]
        public string Alias { get; set; }

        [JsonPropertyName("introtext")]
        public string IntroText { get; set; }

        [JsonPropertyName("fulltext")]
        public string FullText { get; set; }

        [JsonPropertyName("state")]
        public int State { get; set; } // 0 = unpublished, 1 = published, -2 = trashed

        [JsonPropertyName("catid")]
        public int CategoryId { get; set; }

        [JsonPropertyName("created")]
        public DateTime Created { get; set; }

        [JsonPropertyName("created_by")]
        public int CreatedBy { get; set; }

        [JsonPropertyName("modified")]
        public DateTime Modified { get; set; }

        [JsonPropertyName("modified_by")]
        public int ModifiedBy { get; set; }

        [JsonPropertyName("publish_up")]
        public DateTime PublishUp { get; set; }

        [JsonPropertyName("publish_down")]
        public DateTime PublishDown { get; set; }

        [JsonPropertyName("images")]
        public JoomlaArticleImages Images { get; set; }

        [JsonPropertyName("urls")]
        public JoomlaArticleUrls Urls { get; set; }

        [JsonPropertyName("metadesc")]
        public string MetaDescription { get; set; }

        [JsonPropertyName("metakey")]
        public string MetaKeywords { get; set; }

        [JsonPropertyName("hits")]
        public int Hits { get; set; }

        [JsonPropertyName("featured")]
        public bool Featured { get; set; }

        [JsonPropertyName("language")]
        public string Language { get; set; }

        [JsonPropertyName("tags")]
        public List<JoomlaTag> Tags { get; set; }
    }

    /// <summary>
    /// Joomla Article Images
    /// </summary>
    public class JoomlaArticleImages
    {
        [JsonPropertyName("image_intro")]
        public string ImageIntro { get; set; }

        [JsonPropertyName("float_intro")]
        public string FloatIntro { get; set; }

        [JsonPropertyName("image_intro_alt")]
        public string ImageIntroAlt { get; set; }

        [JsonPropertyName("image_intro_caption")]
        public string ImageIntroCaption { get; set; }

        [JsonPropertyName("image_fulltext")]
        public string ImageFulltext { get; set; }

        [JsonPropertyName("float_fulltext")]
        public string FloatFulltext { get; set; }

        [JsonPropertyName("image_fulltext_alt")]
        public string ImageFulltextAlt { get; set; }

        [JsonPropertyName("image_fulltext_caption")]
        public string ImageFulltextCaption { get; set; }
    }

    /// <summary>
    /// Joomla Article URLs
    /// </summary>
    public class JoomlaArticleUrls
    {
        [JsonPropertyName("urla")]
        public string UrlA { get; set; }

        [JsonPropertyName("urlatext")]
        public string UrlAText { get; set; }

        [JsonPropertyName("targeta")]
        public string TargetA { get; set; }

        [JsonPropertyName("urlb")]
        public string UrlB { get; set; }

        [JsonPropertyName("urlbtext")]
        public string UrlBText { get; set; }

        [JsonPropertyName("targetb")]
        public string TargetB { get; set; }

        [JsonPropertyName("urlc")]
        public string UrlC { get; set; }

        [JsonPropertyName("urlctext")]
        public string UrlCText { get; set; }

        [JsonPropertyName("targetc")]
        public string TargetC { get; set; }
    }

    /// <summary>
    /// Joomla Category
    /// </summary>
    public class JoomlaCategory
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("alias")]
        public string Alias { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("published")]
        public int Published { get; set; }

        [JsonPropertyName("parent_id")]
        public int ParentId { get; set; }

        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonPropertyName("path")]
        public string Path { get; set; }

        [JsonPropertyName("extension")]
        public string Extension { get; set; }

        [JsonPropertyName("access")]
        public int Access { get; set; }

        [JsonPropertyName("language")]
        public string Language { get; set; }

        [JsonPropertyName("metadesc")]
        public string MetaDescription { get; set; }

        [JsonPropertyName("metakey")]
        public string MetaKeywords { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, string> Metadata { get; set; }

        [JsonPropertyName("created_time")]
        public DateTime CreatedTime { get; set; }

        [JsonPropertyName("created_user_id")]
        public int CreatedUserId { get; set; }

        [JsonPropertyName("modified_time")]
        public DateTime ModifiedTime { get; set; }

        [JsonPropertyName("modified_user_id")]
        public int ModifiedUserId { get; set; }

        [JsonPropertyName("hits")]
        public int Hits { get; set; }

        [JsonPropertyName("params")]
        public Dictionary<string, string> Params { get; set; }
    }

    /// <summary>
    /// Joomla User
    /// </summary>
    public class JoomlaUser
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("block")]
        public int Block { get; set; }

        [JsonPropertyName("sendEmail")]
        public int SendEmail { get; set; }

        [JsonPropertyName("registerDate")]
        public DateTime RegisterDate { get; set; }

        [JsonPropertyName("lastvisitDate")]
        public DateTime LastVisitDate { get; set; }

        [JsonPropertyName("activation")]
        public string Activation { get; set; }

        [JsonPropertyName("params")]
        public Dictionary<string, string> Params { get; set; }

        [JsonPropertyName("lastResetTime")]
        public DateTime LastResetTime { get; set; }

        [JsonPropertyName("resetCount")]
        public int ResetCount { get; set; }

        [JsonPropertyName("groups")]
        public List<int> Groups { get; set; }
    }

    /// <summary>
    /// Joomla Tag
    /// </summary>
    public class JoomlaTag
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("alias")]
        public string Alias { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("published")]
        public int Published { get; set; }

        [JsonPropertyName("access")]
        public int Access { get; set; }

        [JsonPropertyName("language")]
        public string Language { get; set; }

        [JsonPropertyName("created_time")]
        public DateTime CreatedTime { get; set; }

        [JsonPropertyName("created_user_id")]
        public int CreatedUserId { get; set; }

        [JsonPropertyName("modified_time")]
        public DateTime ModifiedTime { get; set; }

        [JsonPropertyName("modified_user_id")]
        public int ModifiedUserId { get; set; }

        [JsonPropertyName("hits")]
        public int Hits { get; set; }

        [JsonPropertyName("params")]
        public Dictionary<string, string> Params { get; set; }
    }

    /// <summary>
    /// Joomla Media Item
    /// </summary>
    public class JoomlaMedia
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("alias")]
        public string Alias { get; set; }

        [JsonPropertyName("filename")]
        public string Filename { get; set; }

        [JsonPropertyName("path")]
        public string Path { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("extension")]
        public string Extension { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; }

        [JsonPropertyName("created_time")]
        public DateTime CreatedTime { get; set; }

        [JsonPropertyName("created_user_id")]
        public int CreatedUserId { get; set; }

        [JsonPropertyName("modified_time")]
        public DateTime ModifiedTime { get; set; }

        [JsonPropertyName("modified_user_id")]
        public int ModifiedUserId { get; set; }

        [JsonPropertyName("hits")]
        public int Hits { get; set; }

        [JsonPropertyName("params")]
        public Dictionary<string, string> Params { get; set; }
    }

    /// <summary>
    /// Joomla Menu Item
    /// </summary>
    public class JoomlaMenu
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("menutype")]
        public string MenuType { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("alias")]
        public string Alias { get; set; }

        [JsonPropertyName("path")]
        public string Path { get; set; }

        [JsonPropertyName("link")]
        public string Link { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("published")]
        public int Published { get; set; }

        [JsonPropertyName("parent_id")]
        public int ParentId { get; set; }

        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonPropertyName("component_id")]
        public int ComponentId { get; set; }

        [JsonPropertyName("ordering")]
        public int Ordering { get; set; }

        [JsonPropertyName("checked_out")]
        public int CheckedOut { get; set; }

        [JsonPropertyName("checked_out_time")]
        public DateTime CheckedOutTime { get; set; }

        [JsonPropertyName("access")]
        public int Access { get; set; }

        [JsonPropertyName("params")]
        public Dictionary<string, string> Params { get; set; }

        [JsonPropertyName("language")]
        public string Language { get; set; }

        [JsonPropertyName("created_time")]
        public DateTime CreatedTime { get; set; }

        [JsonPropertyName("created_user_id")]
        public int CreatedUserId { get; set; }

        [JsonPropertyName("modified_time")]
        public DateTime ModifiedTime { get; set; }

        [JsonPropertyName("modified_user_id")]
        public int ModifiedUserId { get; set; }

        [JsonPropertyName("hits")]
        public int Hits { get; set; }
    }
}