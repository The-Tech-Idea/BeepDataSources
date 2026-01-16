// File: Connectors/AmazonS3/Models/AmazonS3Models.cs
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.AmazonS3.Models
{
    // -------------------------------------------------------
    // Base
    // -------------------------------------------------------
    public abstract class AmazonS3EntityBase
    {
        [JsonIgnore] public IDataSource? DataSource { get; set; }
        public T Attach<T>(IDataSource ds) where T : AmazonS3EntityBase { DataSource = ds; return (T)this; }
    }

    // -------------------------------------------------------
    // Bucket
    // -------------------------------------------------------
    public sealed class AmazonS3Bucket : AmazonS3EntityBase
    {
        [JsonPropertyName("Name")] public string? BucketName { get; set; }
        [JsonPropertyName("CreationDate")] public DateTime? CreationDate { get; set; }
        [JsonPropertyName("Region")] public string? Region { get; set; }
    }

    // -------------------------------------------------------
    // Object
    // -------------------------------------------------------
    public sealed class AmazonS3Object : AmazonS3EntityBase
    {
        [JsonPropertyName("Key")] public string? Key { get; set; }
        [JsonPropertyName("LastModified")] public DateTime? LastModified { get; set; }
        [JsonPropertyName("ETag")] public string? ETag { get; set; }
        [JsonPropertyName("Size")] public long? Size { get; set; }
        [JsonPropertyName("StorageClass")] public string? StorageClass { get; set; }
        [JsonPropertyName("Owner")] public AmazonS3Owner? Owner { get; set; }
    }

    // -------------------------------------------------------
    // Object Version
    // -------------------------------------------------------
    public sealed class AmazonS3ObjectVersion : AmazonS3EntityBase
    {
        [JsonPropertyName("Key")] public string? Key { get; set; }
        [JsonPropertyName("VersionId")] public string? VersionId { get; set; }
        [JsonPropertyName("LastModified")] public DateTime? LastModified { get; set; }
        [JsonPropertyName("ETag")] public string? ETag { get; set; }
        [JsonPropertyName("Size")] public long? Size { get; set; }
        [JsonPropertyName("StorageClass")] public string? StorageClass { get; set; }
        [JsonPropertyName("IsLatest")] public bool? IsLatest { get; set; }
        [JsonPropertyName("Owner")] public AmazonS3Owner? Owner { get; set; }
    }

    // -------------------------------------------------------
    // Owner
    // -------------------------------------------------------
    public sealed class AmazonS3Owner : AmazonS3EntityBase
    {
        [JsonPropertyName("ID")] public string? Id { get; set; }
        [JsonPropertyName("Caption")] public string? Caption { get; set; }
    }

    // -------------------------------------------------------
    // Multipart Upload
    // -------------------------------------------------------
    public sealed class AmazonS3MultipartUpload : AmazonS3EntityBase
    {
        [JsonPropertyName("Key")] public string? Key { get; set; }
        [JsonPropertyName("UploadId")] public string? UploadId { get; set; }
        [JsonPropertyName("Initiator")] public AmazonS3Owner? Initiator { get; set; }
        [JsonPropertyName("Owner")] public AmazonS3Owner? Owner { get; set; }
        [JsonPropertyName("StorageClass")] public string? StorageClass { get; set; }
        [JsonPropertyName("Initiated")] public DateTime? Initiated { get; set; }
    }

    // -------------------------------------------------------
    // Part
    // -------------------------------------------------------
    public sealed class AmazonS3Part : AmazonS3EntityBase
    {
        [JsonPropertyName("PartNumber")] public int? PartNumber { get; set; }
        [JsonPropertyName("LastModified")] public DateTime? LastModified { get; set; }
        [JsonPropertyName("ETag")] public string? ETag { get; set; }
        [JsonPropertyName("Size")] public long? Size { get; set; }
    }

    // -------------------------------------------------------
    // Bucket Policy
    // -------------------------------------------------------
    public sealed class AmazonS3BucketPolicy : AmazonS3EntityBase
    {
        [JsonPropertyName("Version")] public string? Version { get; set; }
        [JsonPropertyName("Id")] public string? Id { get; set; }
        [JsonPropertyName("Statement")] public List<AmazonS3PolicyStatement>? Statement { get; set; }
    }

    // -------------------------------------------------------
    // Policy Statement
    // -------------------------------------------------------
    public sealed class AmazonS3PolicyStatement : AmazonS3EntityBase
    {
        [JsonPropertyName("Sid")] public string? Sid { get; set; }
        [JsonPropertyName("Effect")] public string? Effect { get; set; }
        [JsonPropertyName("Principal")] public object? Principal { get; set; }
        [JsonPropertyName("Action")] public object? Action { get; set; }
        [JsonPropertyName("Resource")] public object? Resource { get; set; }
        [JsonPropertyName("Condition")] public Dictionary<string, object>? Condition { get; set; }
    }

    // -------------------------------------------------------
    // CORS Configuration
    // -------------------------------------------------------
    public sealed class AmazonS3CorsConfiguration : AmazonS3EntityBase
    {
        [JsonPropertyName("CORSRules")] public List<AmazonS3CorsRule>? CorsRules { get; set; }
    }

    // -------------------------------------------------------
    // CORS Rule
    // -------------------------------------------------------
    public sealed class AmazonS3CorsRule : AmazonS3EntityBase
    {
        [JsonPropertyName("ID")] public string? Id { get; set; }
        [JsonPropertyName("AllowedHeaders")] public List<string>? AllowedHeaders { get; set; }
        [JsonPropertyName("AllowedMethods")] public List<string>? AllowedMethods { get; set; }
        [JsonPropertyName("AllowedOrigins")] public List<string>? AllowedOrigins { get; set; }
        [JsonPropertyName("ExposeHeaders")] public List<string>? ExposeHeaders { get; set; }
        [JsonPropertyName("MaxAgeSeconds")] public int? MaxAgeSeconds { get; set; }
    }

    // -------------------------------------------------------
    // Lifecycle Configuration
    // -------------------------------------------------------
    public sealed class AmazonS3LifecycleConfiguration : AmazonS3EntityBase
    {
        [JsonPropertyName("Rules")] public List<AmazonS3LifecycleRule>? Rules { get; set; }
    }

    // -------------------------------------------------------
    // Lifecycle Rule
    // -------------------------------------------------------
    public sealed class AmazonS3LifecycleRule : AmazonS3EntityBase
    {
        [JsonPropertyName("ID")] public string? Id { get; set; }
        [JsonPropertyName("Status")] public string? Status { get; set; }
        [JsonPropertyName("Filter")] public AmazonS3LifecycleFilter? Filter { get; set; }
        [JsonPropertyName("Transitions")] public List<AmazonS3Transition>? Transitions { get; set; }
        [JsonPropertyName("Expiration")] public AmazonS3Expiration? Expiration { get; set; }
        [JsonPropertyName("NoncurrentVersionTransitions")] public List<AmazonS3NoncurrentVersionTransition>? NoncurrentVersionTransitions { get; set; }
        [JsonPropertyName("NoncurrentVersionExpiration")] public AmazonS3NoncurrentVersionExpiration? NoncurrentVersionExpiration { get; set; }
    }

    // -------------------------------------------------------
    // Lifecycle Filter
    // -------------------------------------------------------
    public sealed class AmazonS3LifecycleFilter : AmazonS3EntityBase
    {
        [JsonPropertyName("Prefix")] public string? Prefix { get; set; }
        [JsonPropertyName("Tag")] public AmazonS3Tag? Tag { get; set; }
        [JsonPropertyName("And")] public AmazonS3LifecycleAnd? And { get; set; }
    }

    // -------------------------------------------------------
    // Tag
    // -------------------------------------------------------
    public sealed class AmazonS3Tag : AmazonS3EntityBase
    {
        [JsonPropertyName("Key")] public string? Key { get; set; }
        [JsonPropertyName("Value")] public string? Value { get; set; }
    }

    // -------------------------------------------------------
    // Lifecycle And
    // -------------------------------------------------------
    public sealed class AmazonS3LifecycleAnd : AmazonS3EntityBase
    {
        [JsonPropertyName("Prefix")] public string? Prefix { get; set; }
        [JsonPropertyName("Tags")] public List<AmazonS3Tag>? Tags { get; set; }
    }

    // -------------------------------------------------------
    // Transition
    // -------------------------------------------------------
    public sealed class AmazonS3Transition : AmazonS3EntityBase
    {
        [JsonPropertyName("Days")] public int? Days { get; set; }
        [JsonPropertyName("Date")] public DateTime? Date { get; set; }
        [JsonPropertyName("StorageClass")] public string? StorageClass { get; set; }
    }

    // -------------------------------------------------------
    // Expiration
    // -------------------------------------------------------
    public sealed class AmazonS3Expiration : AmazonS3EntityBase
    {
        [JsonPropertyName("Days")] public int? Days { get; set; }
        [JsonPropertyName("Date")] public DateTime? Date { get; set; }
        [JsonPropertyName("ExpiredObjectDeleteMarker")] public bool? ExpiredObjectDeleteMarker { get; set; }
    }

    // -------------------------------------------------------
    // Noncurrent Version Transition
    // -------------------------------------------------------
    public sealed class AmazonS3NoncurrentVersionTransition : AmazonS3EntityBase
    {
        [JsonPropertyName("NoncurrentDays")] public int? NoncurrentDays { get; set; }
        [JsonPropertyName("StorageClass")] public string? StorageClass { get; set; }
    }

    // -------------------------------------------------------
    // Noncurrent Version Expiration
    // -------------------------------------------------------
    public sealed class AmazonS3NoncurrentVersionExpiration : AmazonS3EntityBase
    {
        [JsonPropertyName("NoncurrentDays")] public int? NoncurrentDays { get; set; }
    }

    // -------------------------------------------------------
    // Bucket Encryption
    // -------------------------------------------------------
    public sealed class AmazonS3BucketEncryption : AmazonS3EntityBase
    {
        [JsonPropertyName("Rules")] public List<AmazonS3EncryptionRule>? Rules { get; set; }
    }

    // -------------------------------------------------------
    // Encryption Rule
    // -------------------------------------------------------
    public sealed class AmazonS3EncryptionRule : AmazonS3EntityBase
    {
        [JsonPropertyName("ApplyServerSideEncryptionByDefault")] public AmazonS3ServerSideEncryptionByDefault? ApplyServerSideEncryptionByDefault { get; set; }
        [JsonPropertyName("BucketKeyEnabled")] public bool? BucketKeyEnabled { get; set; }
    }

    // -------------------------------------------------------
    // Server Side Encryption By Default
    // -------------------------------------------------------
    public sealed class AmazonS3ServerSideEncryptionByDefault : AmazonS3EntityBase
    {
        [JsonPropertyName("SSEAlgorithm")] public string? SSEAlgorithm { get; set; }
        [JsonPropertyName("KMSMasterKeyID")] public string? KMSMasterKeyID { get; set; }
    }

    // -------------------------------------------------------
    // Access Control List (ACL)
    // -------------------------------------------------------
    public sealed class AmazonS3AccessControlList : AmazonS3EntityBase
    {
        [JsonPropertyName("Owner")] public AmazonS3Owner? Owner { get; set; }
        [JsonPropertyName("Grants")] public List<AmazonS3Grant>? Grants { get; set; }
    }

    // -------------------------------------------------------
    // Grant
    // -------------------------------------------------------
    public sealed class AmazonS3Grant : AmazonS3EntityBase
    {
        [JsonPropertyName("Grantee")] public AmazonS3Grantee? Grantee { get; set; }
        [JsonPropertyName("Permission")] public string? Permission { get; set; }
    }

    // -------------------------------------------------------
    // Grantee
    // -------------------------------------------------------
    public sealed class AmazonS3Grantee : AmazonS3EntityBase
    {
        [JsonPropertyName("Type")] public string? Type { get; set; }
        [JsonPropertyName("ID")] public string? Id { get; set; }
        [JsonPropertyName("Caption")] public string? Caption { get; set; }
        [JsonPropertyName("URI")] public string? URI { get; set; }
        [JsonPropertyName("EmailAddress")] public string? EmailAddress { get; set; }
    }

    // -------------------------------------------------------
    // Tagging
    // -------------------------------------------------------
    public sealed class AmazonS3Tagging : AmazonS3EntityBase
    {
        [JsonPropertyName("TagSet")] public List<AmazonS3Tag>? TagSet { get; set; }
    }

    // -------------------------------------------------------
    // Object Metadata
    // -------------------------------------------------------
    public sealed class AmazonS3ObjectMetadata : AmazonS3EntityBase
    {
        [JsonPropertyName("CacheControl")] public string? CacheControl { get; set; }
        [JsonPropertyName("ContentDisposition")] public string? ContentDisposition { get; set; }
        [JsonPropertyName("ContentEncoding")] public string? ContentEncoding { get; set; }
        [JsonPropertyName("ContentLanguage")] public string? ContentLanguage { get; set; }
        [JsonPropertyName("ContentLength")] public long? ContentLength { get; set; }
        [JsonPropertyName("ContentMD5")] public string? ContentMD5 { get; set; }
        [JsonPropertyName("ContentType")] public string? ContentType { get; set; }
        [JsonPropertyName("ETag")] public string? ETag { get; set; }
        [JsonPropertyName("Expires")] public DateTime? Expires { get; set; }
        [JsonPropertyName("LastModified")] public DateTime? LastModified { get; set; }
        [JsonPropertyName("Metadata")] public Dictionary<string, string>? Metadata { get; set; }
        [JsonPropertyName("ServerSideEncryption")] public string? ServerSideEncryption { get; set; }
        [JsonPropertyName("SSEKMSKeyId")] public string? SSEKMSKeyId { get; set; }
        [JsonPropertyName("StorageClass")] public string? StorageClass { get; set; }
        [JsonPropertyName("VersionId")] public string? VersionId { get; set; }
        [JsonPropertyName("WebsiteRedirectLocation")] public string? WebsiteRedirectLocation { get; set; }
    }
}