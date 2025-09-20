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
    public abstract class AmazonS3Model
    {
        [JsonIgnore] public IDataSource? DataSource { get; set; }
        public T Attach<T>(IDataSource ds) where T : AmazonS3Model { DataSource = ds; return (T)this; }
    }

    // -------------------------------------------------------
    // Bucket
    // -------------------------------------------------------
    public class S3Bucket : AmazonS3Model
    {
        [JsonPropertyName("Name")] public string? BucketName { get; set; }
        [JsonPropertyName("CreationDate")] public DateTime? CreationDate { get; set; }
        [JsonPropertyName("Region")] public string? Region { get; set; }
    }

    // -------------------------------------------------------
    // Object
    // -------------------------------------------------------
    public class S3Object : AmazonS3Model
    {
        [JsonPropertyName("Key")] public string? Key { get; set; }
        [JsonPropertyName("LastModified")] public DateTime? LastModified { get; set; }
        [JsonPropertyName("ETag")] public string? ETag { get; set; }
        [JsonPropertyName("Size")] public long? Size { get; set; }
        [JsonPropertyName("StorageClass")] public string? StorageClass { get; set; }
        [JsonPropertyName("Owner")] public S3Owner? Owner { get; set; }
    }

    // -------------------------------------------------------
    // Object Version
    // -------------------------------------------------------
    public class S3ObjectVersion : AmazonS3Model
    {
        [JsonPropertyName("Key")] public string? Key { get; set; }
        [JsonPropertyName("VersionId")] public string? VersionId { get; set; }
        [JsonPropertyName("LastModified")] public DateTime? LastModified { get; set; }
        [JsonPropertyName("ETag")] public string? ETag { get; set; }
        [JsonPropertyName("Size")] public long? Size { get; set; }
        [JsonPropertyName("StorageClass")] public string? StorageClass { get; set; }
        [JsonPropertyName("IsLatest")] public bool? IsLatest { get; set; }
        [JsonPropertyName("Owner")] public S3Owner? Owner { get; set; }
    }

    // -------------------------------------------------------
    // Owner
    // -------------------------------------------------------
    public class S3Owner : AmazonS3Model
    {
        [JsonPropertyName("ID")] public string? Id { get; set; }
        [JsonPropertyName("DisplayName")] public string? DisplayName { get; set; }
    }

    // -------------------------------------------------------
    // Multipart Upload
    // -------------------------------------------------------
    public class S3MultipartUpload : AmazonS3Model
    {
        [JsonPropertyName("Key")] public string? Key { get; set; }
        [JsonPropertyName("UploadId")] public string? UploadId { get; set; }
        [JsonPropertyName("Initiator")] public S3Owner? Initiator { get; set; }
        [JsonPropertyName("Owner")] public S3Owner? Owner { get; set; }
        [JsonPropertyName("StorageClass")] public string? StorageClass { get; set; }
        [JsonPropertyName("Initiated")] public DateTime? Initiated { get; set; }
    }

    // -------------------------------------------------------
    // Part
    // -------------------------------------------------------
    public class S3Part : AmazonS3Model
    {
        [JsonPropertyName("PartNumber")] public int? PartNumber { get; set; }
        [JsonPropertyName("LastModified")] public DateTime? LastModified { get; set; }
        [JsonPropertyName("ETag")] public string? ETag { get; set; }
        [JsonPropertyName("Size")] public long? Size { get; set; }
    }

    // -------------------------------------------------------
    // Bucket Policy
    // -------------------------------------------------------
    public class S3BucketPolicy : AmazonS3Model
    {
        [JsonPropertyName("Version")] public string? Version { get; set; }
        [JsonPropertyName("Id")] public string? Id { get; set; }
        [JsonPropertyName("Statement")] public List<S3PolicyStatement>? Statement { get; set; }
    }

    // -------------------------------------------------------
    // Policy Statement
    // -------------------------------------------------------
    public class S3PolicyStatement : AmazonS3Model
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
    public class S3CorsConfiguration : AmazonS3Model
    {
        [JsonPropertyName("CORSRules")] public List<S3CorsRule>? CorsRules { get; set; }
    }

    // -------------------------------------------------------
    // CORS Rule
    // -------------------------------------------------------
    public class S3CorsRule : AmazonS3Model
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
    public class S3LifecycleConfiguration : AmazonS3Model
    {
        [JsonPropertyName("Rules")] public List<S3LifecycleRule>? Rules { get; set; }
    }

    // -------------------------------------------------------
    // Lifecycle Rule
    // -------------------------------------------------------
    public class S3LifecycleRule : AmazonS3Model
    {
        [JsonPropertyName("ID")] public string? Id { get; set; }
        [JsonPropertyName("Status")] public string? Status { get; set; }
        [JsonPropertyName("Filter")] public S3LifecycleFilter? Filter { get; set; }
        [JsonPropertyName("Transitions")] public List<S3Transition>? Transitions { get; set; }
        [JsonPropertyName("Expiration")] public S3Expiration? Expiration { get; set; }
        [JsonPropertyName("NoncurrentVersionTransitions")] public List<S3NoncurrentVersionTransition>? NoncurrentVersionTransitions { get; set; }
        [JsonPropertyName("NoncurrentVersionExpiration")] public S3NoncurrentVersionExpiration? NoncurrentVersionExpiration { get; set; }
    }

    // -------------------------------------------------------
    // Lifecycle Filter
    // -------------------------------------------------------
    public class S3LifecycleFilter : AmazonS3Model
    {
        [JsonPropertyName("Prefix")] public string? Prefix { get; set; }
        [JsonPropertyName("Tag")] public S3Tag? Tag { get; set; }
        [JsonPropertyName("And")] public S3LifecycleAnd? And { get; set; }
    }

    // -------------------------------------------------------
    // Tag
    // -------------------------------------------------------
    public class S3Tag : AmazonS3Model
    {
        [JsonPropertyName("Key")] public string? Key { get; set; }
        [JsonPropertyName("Value")] public string? Value { get; set; }
    }

    // -------------------------------------------------------
    // Lifecycle And
    // -------------------------------------------------------
    public class S3LifecycleAnd : AmazonS3Model
    {
        [JsonPropertyName("Prefix")] public string? Prefix { get; set; }
        [JsonPropertyName("Tags")] public List<S3Tag>? Tags { get; set; }
    }

    // -------------------------------------------------------
    // Transition
    // -------------------------------------------------------
    public class S3Transition : AmazonS3Model
    {
        [JsonPropertyName("Days")] public int? Days { get; set; }
        [JsonPropertyName("Date")] public DateTime? Date { get; set; }
        [JsonPropertyName("StorageClass")] public string? StorageClass { get; set; }
    }

    // -------------------------------------------------------
    // Expiration
    // -------------------------------------------------------
    public class S3Expiration : AmazonS3Model
    {
        [JsonPropertyName("Days")] public int? Days { get; set; }
        [JsonPropertyName("Date")] public DateTime? Date { get; set; }
        [JsonPropertyName("ExpiredObjectDeleteMarker")] public bool? ExpiredObjectDeleteMarker { get; set; }
    }

    // -------------------------------------------------------
    // Noncurrent Version Transition
    // -------------------------------------------------------
    public class S3NoncurrentVersionTransition : AmazonS3Model
    {
        [JsonPropertyName("NoncurrentDays")] public int? NoncurrentDays { get; set; }
        [JsonPropertyName("StorageClass")] public string? StorageClass { get; set; }
    }

    // -------------------------------------------------------
    // Noncurrent Version Expiration
    // -------------------------------------------------------
    public class S3NoncurrentVersionExpiration : AmazonS3Model
    {
        [JsonPropertyName("NoncurrentDays")] public int? NoncurrentDays { get; set; }
    }

    // -------------------------------------------------------
    // Bucket Encryption
    // -------------------------------------------------------
    public class S3BucketEncryption : AmazonS3Model
    {
        [JsonPropertyName("Rules")] public List<S3EncryptionRule>? Rules { get; set; }
    }

    // -------------------------------------------------------
    // Encryption Rule
    // -------------------------------------------------------
    public class S3EncryptionRule : AmazonS3Model
    {
        [JsonPropertyName("ApplyServerSideEncryptionByDefault")] public S3ServerSideEncryptionByDefault? ApplyServerSideEncryptionByDefault { get; set; }
        [JsonPropertyName("BucketKeyEnabled")] public bool? BucketKeyEnabled { get; set; }
    }

    // -------------------------------------------------------
    // Server Side Encryption By Default
    // -------------------------------------------------------
    public class S3ServerSideEncryptionByDefault : AmazonS3Model
    {
        [JsonPropertyName("SSEAlgorithm")] public string? SSEAlgorithm { get; set; }
        [JsonPropertyName("KMSMasterKeyID")] public string? KMSMasterKeyID { get; set; }
    }

    // -------------------------------------------------------
    // Access Control List (ACL)
    // -------------------------------------------------------
    public class S3AccessControlList : AmazonS3Model
    {
        [JsonPropertyName("Owner")] public S3Owner? Owner { get; set; }
        [JsonPropertyName("Grants")] public List<S3Grant>? Grants { get; set; }
    }

    // -------------------------------------------------------
    // Grant
    // -------------------------------------------------------
    public class S3Grant : AmazonS3Model
    {
        [JsonPropertyName("Grantee")] public S3Grantee? Grantee { get; set; }
        [JsonPropertyName("Permission")] public string? Permission { get; set; }
    }

    // -------------------------------------------------------
    // Grantee
    // -------------------------------------------------------
    public class S3Grantee : AmazonS3Model
    {
        [JsonPropertyName("Type")] public string? Type { get; set; }
        [JsonPropertyName("ID")] public string? Id { get; set; }
        [JsonPropertyName("DisplayName")] public string? DisplayName { get; set; }
        [JsonPropertyName("URI")] public string? URI { get; set; }
        [JsonPropertyName("EmailAddress")] public string? EmailAddress { get; set; }
    }

    // -------------------------------------------------------
    // Tagging
    // -------------------------------------------------------
    public class S3Tagging : AmazonS3Model
    {
        [JsonPropertyName("TagSet")] public List<S3Tag>? TagSet { get; set; }
    }

    // -------------------------------------------------------
    // Object Metadata
    // -------------------------------------------------------
    public class S3ObjectMetadata : AmazonS3Model
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