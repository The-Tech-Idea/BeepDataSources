// File: Connectors/pCloud/Models/pCloudModels.cs
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.pCloud.Models
{
    // -------------------------------------------------------
    // Base
    // -------------------------------------------------------
    public abstract class pCloudModel
    {
        [JsonIgnore] public IDataSource? DataSource { get; set; }
        public T Attach<T>(IDataSource ds) where T : pCloudModel { DataSource = ds; return (T)this; }
    }

    // -------------------------------------------------------
    // File/Folder Item
    // -------------------------------------------------------
    public class pCloudItem : pCloudModel
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("path")] public string? Path { get; set; }
        [JsonPropertyName("parentfolderid")] public long? ParentFolderId { get; set; }
        [JsonPropertyName("created")] public DateTimeOffset? Created { get; set; }
        [JsonPropertyName("modified")] public DateTimeOffset? Modified { get; set; }
        [JsonPropertyName("size")] public long? Size { get; set; }
        [JsonPropertyName("hash")] public string? Hash { get; set; }
        [JsonPropertyName("thumb")] public bool? Thumb { get; set; }
        [JsonPropertyName("isfolder")] public bool? IsFolder { get; set; }
        [JsonPropertyName("isshared")] public bool? IsShared { get; set; }
        [JsonPropertyName("ismine")] public bool? IsMine { get; set; }
        [JsonPropertyName("canread")] public bool? CanRead { get; set; }
        [JsonPropertyName("canwrite")] public bool? CanWrite { get; set; }
        [JsonPropertyName("candelete")] public bool? CanDelete { get; set; }
        [JsonPropertyName("canreadcomments")] public bool? CanReadComments { get; set; }
        [JsonPropertyName("canwritecomments")] public bool? CanWriteComments { get; set; }
        [JsonPropertyName("category")] public int? Category { get; set; }
        [JsonPropertyName("contenttype")] public string? ContentType { get; set; }
        [JsonPropertyName("icon")] public string? Icon { get; set; }
        [JsonPropertyName("width")] public int? Width { get; set; }
        [JsonPropertyName("height")] public int? Height { get; set; }
        [JsonPropertyName("fileid")] public long? FileId { get; set; }
        [JsonPropertyName("folderid")] public long? FolderId { get; set; }
    }

    // -------------------------------------------------------
    // User Information
    // -------------------------------------------------------
    public class pCloudUser : pCloudModel
    {
        [JsonPropertyName("userid")] public long? UserId { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("emailverified")] public bool? EmailVerified { get; set; }
        [JsonPropertyName("registered")] public DateTimeOffset? Registered { get; set; }
        [JsonPropertyName("firstname")] public string? FirstName { get; set; }
        [JsonPropertyName("lastname")] public string? LastName { get; set; }
        [JsonPropertyName("usedquota")] public long? UsedQuota { get; set; }
        [JsonPropertyName("quota")] public long? Quota { get; set; }
        [JsonPropertyName("premium")] public bool? Premium { get; set; }
        [JsonPropertyName("premiumexpires")] public DateTimeOffset? PremiumExpires { get; set; }
        [JsonPropertyName("language")] public string? Language { get; set; }
        [JsonPropertyName("plan")] public string? Plan { get; set; }
        [JsonPropertyName("cryptosetup")] public bool? CryptoSetup { get; set; }
        [JsonPropertyName("publiclinkquota")] public long? PublicLinkQuota { get; set; }
        [JsonPropertyName("usedpublinkquota")] public long? UsedPublicLinkQuota { get; set; }
    }

    // -------------------------------------------------------
    // Share Link
    // -------------------------------------------------------
    public class pCloudShare : pCloudModel
    {
        [JsonPropertyName("link")] public string? Link { get; set; }
        [JsonPropertyName("linkid")] public long? LinkId { get; set; }
        [JsonPropertyName("code")] public string? Code { get; set; }
        [JsonPropertyName("expires")] public DateTimeOffset? Expires { get; set; }
        [JsonPropertyName("downloads")] public int? Downloads { get; set; }
        [JsonPropertyName("maxdownloads")] public int? MaxDownloads { get; set; }
        [JsonPropertyName("created")] public DateTimeOffset? Created { get; set; }
        [JsonPropertyName("modified")] public DateTimeOffset? Modified { get; set; }
        [JsonPropertyName("filename")] public string? FileName { get; set; }
        [JsonPropertyName("size")] public long? Size { get; set; }
        [JsonPropertyName("icon")] public string? Icon { get; set; }
        [JsonPropertyName("thumb")] public bool? Thumb { get; set; }
        [JsonPropertyName("metadata")] public pCloudItem? Metadata { get; set; }
    }

    // -------------------------------------------------------
    // Upload Result
    // -------------------------------------------------------
    public class pCloudUploadResult : pCloudModel
    {
        [JsonPropertyName("result")] public int? Result { get; set; }
        [JsonPropertyName("metadata")] public List<pCloudItem>? Metadata { get; set; }
        [JsonPropertyName("fileids")] public List<long>? FileIds { get; set; }
    }

    // -------------------------------------------------------
    // Folder Metadata
    // -------------------------------------------------------
    public class pCloudFolderMetadata : pCloudModel
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("folderid")] public long? FolderId { get; set; }
        [JsonPropertyName("parentfolderid")] public long? ParentFolderId { get; set; }
        [JsonPropertyName("created")] public DateTimeOffset? Created { get; set; }
        [JsonPropertyName("modified")] public DateTimeOffset? Modified { get; set; }
        [JsonPropertyName("isshared")] public bool? IsShared { get; set; }
        [JsonPropertyName("ismine")] public bool? IsMine { get; set; }
        [JsonPropertyName("canread")] public bool? CanRead { get; set; }
        [JsonPropertyName("canwrite")] public bool? CanWrite { get; set; }
        [JsonPropertyName("candelete")] public bool? CanDelete { get; set; }
        [JsonPropertyName("canreadcomments")] public bool? CanReadComments { get; set; }
        [JsonPropertyName("canwritecomments")] public bool? CanWriteComments { get; set; }
        [JsonPropertyName("contents")] public List<pCloudItem>? Contents { get; set; }
    }

    // -------------------------------------------------------
    // API Response Base
    // -------------------------------------------------------
    public class pCloudApiResponse : pCloudModel
    {
        [JsonPropertyName("result")] public int? Result { get; set; }
        [JsonPropertyName("error")] public string? Error { get; set; }
    }

    // -------------------------------------------------------
    // List Folder Response
    // -------------------------------------------------------
    public class pCloudListFolderResponse : pCloudApiResponse
    {
        [JsonPropertyName("metadata")] public pCloudFolderMetadata? Metadata { get; set; }
    }

    // -------------------------------------------------------
    // User Info Response
    // -------------------------------------------------------
    public class pCloudUserInfoResponse : pCloudApiResponse
    {
        [JsonPropertyName("userid")] public long? UserId { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("emailverified")] public bool? EmailVerified { get; set; }
        [JsonPropertyName("registered")] public DateTimeOffset? Registered { get; set; }
        [JsonPropertyName("firstname")] public string? FirstName { get; set; }
        [JsonPropertyName("lastname")] public string? LastName { get; set; }
        [JsonPropertyName("usedquota")] public long? UsedQuota { get; set; }
        [JsonPropertyName("quota")] public long? Quota { get; set; }
        [JsonPropertyName("premium")] public bool? Premium { get; set; }
        [JsonPropertyName("premiumexpires")] public DateTimeOffset? PremiumExpires { get; set; }
        [JsonPropertyName("language")] public string? Language { get; set; }
        [JsonPropertyName("plan")] public string? Plan { get; set; }
        [JsonPropertyName("cryptosetup")] public bool? CryptoSetup { get; set; }
        [JsonPropertyName("publiclinkquota")] public long? PublicLinkQuota { get; set; }
        [JsonPropertyName("usedpublinkquota")] public long? UsedPublicLinkQuota { get; set; }
    }

    // -------------------------------------------------------
    // Share Link Response
    // -------------------------------------------------------
    public class pCloudShareResponse : pCloudApiResponse
    {
        [JsonPropertyName("link")] public string? Link { get; set; }
        [JsonPropertyName("linkid")] public long? LinkId { get; set; }
        [JsonPropertyName("code")] public string? Code { get; set; }
        [JsonPropertyName("expires")] public DateTimeOffset? Expires { get; set; }
        [JsonPropertyName("downloads")] public int? Downloads { get; set; }
        [JsonPropertyName("maxdownloads")] public int? MaxDownloads { get; set; }
        [JsonPropertyName("created")] public DateTimeOffset? Created { get; set; }
        [JsonPropertyName("modified")] public DateTimeOffset? Modified { get; set; }
        [JsonPropertyName("filename")] public string? FileName { get; set; }
        [JsonPropertyName("size")] public long? Size { get; set; }
        [JsonPropertyName("icon")] public string? Icon { get; set; }
        [JsonPropertyName("thumb")] public bool? Thumb { get; set; }
        [JsonPropertyName("metadata")] public pCloudItem? Metadata { get; set; }
    }
}