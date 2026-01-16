// File: Connectors/MediaFire/Models/MediaFireModels.cs
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.MediaFire.Models
{
    // -------------------------------------------------------
    // Base
    // -------------------------------------------------------
    public abstract class MediaFireEntityBase
    {
        [JsonIgnore] public IDataSource? DataSource { get; set; }
        public T Attach<T>(IDataSource ds) where T : MediaFireEntityBase { DataSource = ds; return (T)this; }
    }

    // -------------------------------------------------------
    // File/Folder Item
    // -------------------------------------------------------
    public sealed class MediaFireItem : MediaFireEntityBase
    {
        [JsonPropertyName("quickkey")] public string? QuickKey { get; set; }
        [JsonPropertyName("filename")] public string? FileName { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("size")] public long? Size { get; set; }
        [JsonPropertyName("privacy")] public string? Privacy { get; set; }
        [JsonPropertyName("created")] public DateTimeOffset? Created { get; set; }
        [JsonPropertyName("downloads")] public int? Downloads { get; set; }
        [JsonPropertyName("views")] public int? Views { get; set; }
        [JsonPropertyName("hash")] public string? Hash { get; set; }
        [JsonPropertyName("filetype")] public string? FileType { get; set; }
        [JsonPropertyName("mimetype")] public string? MimeType { get; set; }
        [JsonPropertyName("flag")] public string? Flag { get; set; }
        [JsonPropertyName("parent_folderkey")] public string? ParentFolderKey { get; set; }
        [JsonPropertyName("folderkey")] public string? FolderKey { get; set; }
        [JsonPropertyName("foldername")] public string? FolderName { get; set; }
        [JsonPropertyName("permissions")] public MediaFirePermissions? Permissions { get; set; }
    }

    // -------------------------------------------------------
    // Permissions
    // -------------------------------------------------------
    public sealed class MediaFirePermissions : MediaFireEntityBase
    {
        [JsonPropertyName("edit")] public bool? Edit { get; set; }
        [JsonPropertyName("delete")] public bool? Delete { get; set; }
        [JsonPropertyName("read")] public bool? Read { get; set; }
        [JsonPropertyName("value")] public int? Value { get; set; }
    }

    // -------------------------------------------------------
    // Folder Content
    // -------------------------------------------------------
    public sealed class MediaFireFolderContent : MediaFireEntityBase
    {
        [JsonPropertyName("folderkey")] public string? FolderKey { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("total_folders")] public int? TotalFolders { get; set; }
        [JsonPropertyName("total_files")] public int? TotalFiles { get; set; }
        [JsonPropertyName("revision")] public long? Revision { get; set; }
        [JsonPropertyName("epoch")] public long? Epoch { get; set; }
        [JsonPropertyName("created")] public DateTimeOffset? Created { get; set; }
        [JsonPropertyName("permissions")] public MediaFirePermissions? Permissions { get; set; }
        [JsonPropertyName("folders")] public List<MediaFireItem>? Folders { get; set; }
        [JsonPropertyName("files")] public List<MediaFireItem>? Files { get; set; }
    }

    // -------------------------------------------------------
    // User Information
    // -------------------------------------------------------
    public sealed class MediaFireUser : MediaFireEntityBase
    {
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("first_name")] public string? FirstName { get; set; }
        [JsonPropertyName("last_name")] public string? LastName { get; set; }
        [JsonPropertyName("display_name")] public string? Caption { get; set; }
        [JsonPropertyName("gender")] public string? Gender { get; set; }
        [JsonPropertyName("birthday")] public DateTimeOffset? Birthday { get; set; }
        [JsonPropertyName("premium")] public bool? Premium { get; set; }
        [JsonPropertyName("bandwidth")] public long? Bandwidth { get; set; }
        [JsonPropertyName("storage_limit")] public long? StorageLimit { get; set; }
        [JsonPropertyName("storage_used")] public long? StorageUsed { get; set; }
        [JsonPropertyName("valid_forever")] public bool? ValidForever { get; set; }
        [JsonPropertyName("reset_date")] public DateTimeOffset? ResetDate { get; set; }
    }

    // -------------------------------------------------------
    // Share Link
    // -------------------------------------------------------
    public sealed class MediaFireShare : MediaFireEntityBase
    {
        [JsonPropertyName("quickkey")] public string? QuickKey { get; set; }
        [JsonPropertyName("link")] public string? Link { get; set; }
        [JsonPropertyName("one_time_download")] public bool? OneTimeDownload { get; set; }
        [JsonPropertyName("direct_download")] public bool? DirectDownload { get; set; }
        [JsonPropertyName("password_protected")] public bool? PasswordProtected { get; set; }
        [JsonPropertyName("email_required")] public bool? EmailRequired { get; set; }
        [JsonPropertyName("created")] public DateTimeOffset? Created { get; set; }
        [JsonPropertyName("downloads")] public int? Downloads { get; set; }
        [JsonPropertyName("views")] public int? Views { get; set; }
    }

    // -------------------------------------------------------
    // Upload Result
    // -------------------------------------------------------
    public sealed class MediaFireUploadResult : MediaFireEntityBase
    {
        [JsonPropertyName("quickkey")] public string? QuickKey { get; set; }
        [JsonPropertyName("filename")] public string? FileName { get; set; }
        [JsonPropertyName("size")] public long? Size { get; set; }
        [JsonPropertyName("hash")] public string? Hash { get; set; }
        [JsonPropertyName("created")] public DateTimeOffset? Created { get; set; }
        [JsonPropertyName("revision")] public long? Revision { get; set; }
    }

    // -------------------------------------------------------
    // API Response Base
    // -------------------------------------------------------
    public class MediaFireApiResponse : MediaFireEntityBase
    {
        [JsonPropertyName("response")] public MediaFireResponse? Response { get; set; }
    }

    // -------------------------------------------------------
    // Response Wrapper
    // -------------------------------------------------------
    public sealed class MediaFireResponse : MediaFireEntityBase
    {
        [JsonPropertyName("action")] public string? Action { get; set; }
        [JsonPropertyName("result")] public string? Result { get; set; }
        [JsonPropertyName("message")] public string? Message { get; set; }
        [JsonPropertyName("current_api_version")] public string? CurrentApiVersion { get; set; }
        [JsonPropertyName("new_key")] public string? NewKey { get; set; }
        [JsonPropertyName("session_token")] public string? SessionToken { get; set; }
        [JsonPropertyName("secret_key")] public string? SecretKey { get; set; }
        [JsonPropertyName("time")] public long? Time { get; set; }
        [JsonPropertyName("device_revision")] public long? DeviceRevision { get; set; }
    }

    // -------------------------------------------------------
    // Folder Response
    // -------------------------------------------------------
    public sealed class MediaFireFolderResponse : MediaFireApiResponse
    {
        [JsonPropertyName("folder_content")] public MediaFireFolderContent? FolderContent { get; set; }
    }

    // -------------------------------------------------------
    // File Response
    // -------------------------------------------------------
    public sealed class MediaFireFileResponse : MediaFireApiResponse
    {
        [JsonPropertyName("file_info")] public MediaFireItem? FileInfo { get; set; }
    }

    // -------------------------------------------------------
    // User Info Response
    // -------------------------------------------------------
    public sealed class MediaFireUserInfoResponse : MediaFireApiResponse
    {
        [JsonPropertyName("user_info")] public MediaFireUser? UserInfo { get; set; }
    }

    // -------------------------------------------------------
    // Upload Response
    // -------------------------------------------------------
    public sealed class MediaFireUploadResponse : MediaFireApiResponse
    {
        [JsonPropertyName("doupload")] public MediaFireUploadResult? DoUpload { get; set; }
    }

    // -------------------------------------------------------
    // Share Response
    // -------------------------------------------------------
    public sealed class MediaFireShareResponse : MediaFireApiResponse
    {
        [JsonPropertyName("share_link")] public MediaFireShare? ShareLink { get; set; }
    }
}