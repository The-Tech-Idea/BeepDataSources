using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TheTechIdea.Beep.Connectors.Drupal.Models
{
    /// <summary>
    /// Drupal Node (equivalent to WordPress Post/Article)
    /// </summary>
    public class DrupalNode
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("attributes")]
        public DrupalNodeAttributes Attributes { get; set; }

        [JsonPropertyName("relationships")]
        public DrupalNodeRelationships Relationships { get; set; }
    }

    /// <summary>
    /// Drupal Node Attributes
    /// </summary>
    public class DrupalNodeAttributes
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("body")]
        public DrupalBody Body { get; set; }

        [JsonPropertyName("status")]
        public bool Status { get; set; }

        [JsonPropertyName("promote")]
        public bool Promote { get; set; }

        [JsonPropertyName("sticky")]
        public bool Sticky { get; set; }

        [JsonPropertyName("created")]
        public DateTime Created { get; set; }

        [JsonPropertyName("changed")]
        public DateTime Changed { get; set; }

        [JsonPropertyName("author")]
        public string Author { get; set; }

        [JsonPropertyName("path")]
        public DrupalPath Path { get; set; }

        [JsonPropertyName("field_tags")]
        public List<DrupalTagReference> FieldTags { get; set; }

        [JsonPropertyName("field_image")]
        public DrupalMediaReference FieldImage { get; set; }
    }

    /// <summary>
    /// Drupal Node Relationships
    /// </summary>
    public class DrupalNodeRelationships
    {
        [JsonPropertyName("node_type")]
        public DrupalRelationship NodeType { get; set; }

        [JsonPropertyName("uid")]
        public DrupalRelationship Uid { get; set; }

        [JsonPropertyName("field_tags")]
        public DrupalRelationship FieldTags { get; set; }

        [JsonPropertyName("field_image")]
        public DrupalRelationship FieldImage { get; set; }
    }

    /// <summary>
    /// Drupal Body Field
    /// </summary>
    public class DrupalBody
    {
        [JsonPropertyName("value")]
        public string Value { get; set; }

        [JsonPropertyName("format")]
        public string Format { get; set; }

        [JsonPropertyName("summary")]
        public string Summary { get; set; }
    }

    /// <summary>
    /// Drupal Path Field
    /// </summary>
    public class DrupalPath
    {
        [JsonPropertyName("alias")]
        public string Alias { get; set; }

        [JsonPropertyName("pid")]
        public int Pid { get; set; }

        [JsonPropertyName("langcode")]
        public string Langcode { get; set; }
    }

    /// <summary>
    /// Drupal Taxonomy Term (Category/Tag)
    /// </summary>
    public class DrupalTaxonomyTerm
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("attributes")]
        public DrupalTaxonomyTermAttributes Attributes { get; set; }

        [JsonPropertyName("relationships")]
        public DrupalTaxonomyTermRelationships Relationships { get; set; }
    }

    /// <summary>
    /// Drupal Taxonomy Term Attributes
    /// </summary>
    public class DrupalTaxonomyTermAttributes
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public DrupalBody Description { get; set; }

        [JsonPropertyName("weight")]
        public int Weight { get; set; }

        [JsonPropertyName("changed")]
        public DateTime Changed { get; set; }

        [JsonPropertyName("path")]
        public DrupalPath Path { get; set; }
    }

    /// <summary>
    /// Drupal Taxonomy Term Relationships
    /// </summary>
    public class DrupalTaxonomyTermRelationships
    {
        [JsonPropertyName("vid")]
        public DrupalRelationship Vid { get; set; }

        [JsonPropertyName("parent")]
        public DrupalRelationship Parent { get; set; }
    }

    /// <summary>
    /// Drupal User
    /// </summary>
    public class DrupalUser
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("attributes")]
        public DrupalUserAttributes Attributes { get; set; }

        [JsonPropertyName("relationships")]
        public DrupalUserRelationships Relationships { get; set; }
    }

    /// <summary>
    /// Drupal User Attributes
    /// </summary>
    public class DrupalUserAttributes
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("mail")]
        public string Mail { get; set; }

        [JsonPropertyName("timezone")]
        public string Timezone { get; set; }

        [JsonPropertyName("status")]
        public bool Status { get; set; }

        [JsonPropertyName("created")]
        public DateTime Created { get; set; }

        [JsonPropertyName("changed")]
        public DateTime Changed { get; set; }

        [JsonPropertyName("access")]
        public DateTime Access { get; set; }

        [JsonPropertyName("login")]
        public DateTime Login { get; set; }
    }

    /// <summary>
    /// Drupal User Relationships
    /// </summary>
    public class DrupalUserRelationships
    {
        [JsonPropertyName("roles")]
        public DrupalRelationship Roles { get; set; }
    }

    /// <summary>
    /// Drupal Media Entity
    /// </summary>
    public class DrupalMedia
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("attributes")]
        public DrupalMediaAttributes Attributes { get; set; }

        [JsonPropertyName("relationships")]
        public DrupalMediaRelationships Relationships { get; set; }
    }

    /// <summary>
    /// Drupal Media Attributes
    /// </summary>
    public class DrupalMediaAttributes
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("created")]
        public DateTime Created { get; set; }

        [JsonPropertyName("changed")]
        public DateTime Changed { get; set; }

        [JsonPropertyName("status")]
        public bool Status { get; set; }

        [JsonPropertyName("field_media_image")]
        public DrupalMediaImage FieldMediaImage { get; set; }
    }

    /// <summary>
    /// Drupal Media Relationships
    /// </summary>
    public class DrupalMediaRelationships
    {
        [JsonPropertyName("bundle")]
        public DrupalRelationship Bundle { get; set; }

        [JsonPropertyName("uid")]
        public DrupalRelationship Uid { get; set; }

        [JsonPropertyName("field_media_image")]
        public DrupalRelationship FieldMediaImage { get; set; }
    }

    /// <summary>
    /// Drupal Media Image Field
    /// </summary>
    public class DrupalMediaImage
    {
        [JsonPropertyName("target_id")]
        public int TargetId { get; set; }

        [JsonPropertyName("alt")]
        public string Alt { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }
    }

    /// <summary>
    /// Drupal File Entity
    /// </summary>
    public class DrupalFile
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("attributes")]
        public DrupalFileAttributes Attributes { get; set; }

        [JsonPropertyName("relationships")]
        public DrupalFileRelationships Relationships { get; set; }
    }

    /// <summary>
    /// Drupal File Attributes
    /// </summary>
    public class DrupalFileAttributes
    {
        [JsonPropertyName("filename")]
        public string Filename { get; set; }

        [JsonPropertyName("uri")]
        public DrupalFileUri Uri { get; set; }

        [JsonPropertyName("filemime")]
        public string Filemime { get; set; }

        [JsonPropertyName("filesize")]
        public long Filesize { get; set; }

        [JsonPropertyName("status")]
        public bool Status { get; set; }

        [JsonPropertyName("created")]
        public DateTime Created { get; set; }

        [JsonPropertyName("changed")]
        public DateTime Changed { get; set; }
    }

    /// <summary>
    /// Drupal File Relationships
    /// </summary>
    public class DrupalFileRelationships
    {
        [JsonPropertyName("uid")]
        public DrupalRelationship Uid { get; set; }
    }

    /// <summary>
    /// Drupal File URI
    /// </summary>
    public class DrupalFileUri
    {
        [JsonPropertyName("value")]
        public string Value { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }
    }

    /// <summary>
    /// Drupal Relationship Reference
    /// </summary>
    public class DrupalRelationship
    {
        [JsonPropertyName("data")]
        public object Data { get; set; }
    }

    /// <summary>
    /// Drupal Tag Reference (for field_tags)
    /// </summary>
    public class DrupalTagReference
    {
        [JsonPropertyName("target_id")]
        public int TargetId { get; set; }

        [JsonPropertyName("target_type")]
        public string TargetType { get; set; }

        [JsonPropertyName("target_uuid")]
        public string TargetUuid { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }
    }

    /// <summary>
    /// Drupal Media Reference (for field_image)
    /// </summary>
    public class DrupalMediaReference
    {
        [JsonPropertyName("target_id")]
        public int TargetId { get; set; }

        [JsonPropertyName("alt")]
        public string Alt { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }
    }

    /// <summary>
    /// Drupal JSON:API Response Wrapper
    /// </summary>
    public class DrupalApiResponse<T>
    {
        [JsonPropertyName("data")]
        public T Data { get; set; }

        [JsonPropertyName("included")]
        public List<object> Included { get; set; }

        [JsonPropertyName("links")]
        public Dictionary<string, DrupalLink> Links { get; set; }

        [JsonPropertyName("meta")]
        public DrupalMeta Meta { get; set; }
    }

    /// <summary>
    /// Drupal Link
    /// </summary>
    public class DrupalLink
    {
        [JsonPropertyName("href")]
        public string Href { get; set; }

        [JsonPropertyName("meta")]
        public Dictionary<string, object> Meta { get; set; }
    }

    /// <summary>
    /// Drupal Meta Information
    /// </summary>
    public class DrupalMeta
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }
    }
}