using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TheTechIdea.Beep.Connectors.Contentful.Models
{
    /// <summary>
    /// Contentful Entry (equivalent to a content item)
    /// </summary>
    public class ContentfulEntry
    {
        [JsonPropertyName("sys")]
        public ContentfulSys Sys { get; set; }

        [JsonPropertyName("fields")]
        public Dictionary<string, object> Fields { get; set; }

        [JsonPropertyName("metadata")]
        public ContentfulMetadata Metadata { get; set; }
    }

    /// <summary>
    /// Contentful System Properties
    /// </summary>
    public class ContentfulSys
    {
        [JsonPropertyName("space")]
        public ContentfulLink Space { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("environment")]
        public ContentfulLink Environment { get; set; }

        [JsonPropertyName("publishedVersion")]
        public int PublishedVersion { get; set; }

        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("contentType")]
        public ContentfulLink ContentType { get; set; }
    }

    /// <summary>
    /// Contentful Link
    /// </summary>
    public class ContentfulLink
    {
        [JsonPropertyName("sys")]
        public ContentfulLinkSys Sys { get; set; }
    }

    /// <summary>
    /// Contentful Link System Properties
    /// </summary>
    public class ContentfulLinkSys
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("linkType")]
        public string LinkType { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }
    }

    /// <summary>
    /// Contentful Metadata
    /// </summary>
    public class ContentfulMetadata
    {
        [JsonPropertyName("tags")]
        public List<ContentfulLink> Tags { get; set; }
    }

    /// <summary>
    /// Contentful Content Type
    /// </summary>
    public class ContentfulContentType
    {
        [JsonPropertyName("sys")]
        public ContentfulSys Sys { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("displayField")]
        public string DisplayField { get; set; }

        [JsonPropertyName("fields")]
        public List<ContentfulField> Fields { get; set; }
    }

    /// <summary>
    /// Contentful Field Definition
    /// </summary>
    public class ContentfulField
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("localized")]
        public bool Localized { get; set; }

        [JsonPropertyName("required")]
        public bool Required { get; set; }

        [JsonPropertyName("validations")]
        public List<object> Validations { get; set; }

        [JsonPropertyName("disabled")]
        public bool Disabled { get; set; }

        [JsonPropertyName("omitted")]
        public bool Omitted { get; set; }

        [JsonPropertyName("linkType")]
        public string LinkType { get; set; }

        [JsonPropertyName("items")]
        public ContentfulFieldItems Items { get; set; }
    }

    /// <summary>
    /// Contentful Field Items (for arrays)
    /// </summary>
    public class ContentfulFieldItems
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("validations")]
        public List<object> Validations { get; set; }

        [JsonPropertyName("linkType")]
        public string LinkType { get; set; }
    }

    /// <summary>
    /// Contentful Asset (media file)
    /// </summary>
    public class ContentfulAsset
    {
        [JsonPropertyName("sys")]
        public ContentfulSys Sys { get; set; }

        [JsonPropertyName("fields")]
        public ContentfulAssetFields Fields { get; set; }

        [JsonPropertyName("metadata")]
        public ContentfulMetadata Metadata { get; set; }
    }

    /// <summary>
    /// Contentful Asset Fields
    /// </summary>
    public class ContentfulAssetFields
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("file")]
        public ContentfulFile File { get; set; }
    }

    /// <summary>
    /// Contentful File
    /// </summary>
    public class ContentfulFile
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("details")]
        public ContentfulFileDetails Details { get; set; }

        [JsonPropertyName("fileName")]
        public string FileName { get; set; }

        [JsonPropertyName("contentType")]
        public string ContentType { get; set; }
    }

    /// <summary>
    /// Contentful File Details
    /// </summary>
    public class ContentfulFileDetails
    {
        [JsonPropertyName("image")]
        public ContentfulImageDetails Image { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }
    }

    /// <summary>
    /// Contentful Image Details
    /// </summary>
    public class ContentfulImageDetails
    {
        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }
    }

    /// <summary>
    /// Contentful Space
    /// </summary>
    public class ContentfulSpace
    {
        [JsonPropertyName("sys")]
        public ContentfulSys Sys { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    /// <summary>
    /// Contentful Environment
    /// </summary>
    public class ContentfulEnvironment
    {
        [JsonPropertyName("sys")]
        public ContentfulSys Sys { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    /// <summary>
    /// Contentful Locale
    /// </summary>
    public class ContentfulLocale
    {
        [JsonPropertyName("sys")]
        public ContentfulSys Sys { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("default")]
        public bool Default { get; set; }

        [JsonPropertyName("fallbackCode")]
        public string FallbackCode { get; set; }

        [JsonPropertyName("contentManagementApi")]
        public bool ContentManagementApi { get; set; }

        [JsonPropertyName("contentDeliveryApi")]
        public bool ContentDeliveryApi { get; set; }

        [JsonPropertyName("optional")]
        public bool Optional { get; set; }
    }

    /// <summary>
    /// Contentful API Response Wrapper
    /// </summary>
    public class ContentfulApiResponse<T>
    {
        [JsonPropertyName("sys")]
        public ContentfulResponseSys Sys { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("skip")]
        public int Skip { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("items")]
        public List<T> Items { get; set; }
    }

    /// <summary>
    /// Contentful Response System Properties
    /// </summary>
    public class ContentfulResponseSys
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }
    }

    /// <summary>
    /// Contentful Single Item Response
    /// </summary>
    public class ContentfulSingleResponse<T>
    {
        [JsonPropertyName("sys")]
        public ContentfulSys Sys { get; set; }

        [JsonPropertyName("fields")]
        public T Fields { get; set; }

        [JsonPropertyName("metadata")]
        public ContentfulMetadata Metadata { get; set; }
    }
}