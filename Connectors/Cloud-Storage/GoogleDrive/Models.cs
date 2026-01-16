// File: Connectors/GoogleDrive/Models/GoogleDriveModels.cs
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.GoogleDrive.Models
{
    // -------------------------------------------------------
    // Base
    // -------------------------------------------------------
    public abstract class GoogleDriveEntityBase
    {
        [JsonIgnore] public IDataSource DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : GoogleDriveEntityBase { DataSource = ds; return (T)this; }
    }

    // -------------------------------------------------------
    // File
    // -------------------------------------------------------
    public sealed class GoogleDriveFile : GoogleDriveEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("mimeType")] public string MimeType { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("parents")] public List<string> Parents { get; set; } = new();
        [JsonPropertyName("webContentLink")] public string WebContentLink { get; set; }
        [JsonPropertyName("webViewLink")] public string WebViewLink { get; set; }
        [JsonPropertyName("iconLink")] public string IconLink { get; set; }
        [JsonPropertyName("thumbnailLink")] public string ThumbnailLink { get; set; }
        [JsonPropertyName("createdTime")] public DateTimeOffset? CreatedTime { get; set; }
        [JsonPropertyName("modifiedTime")] public DateTimeOffset? ModifiedTime { get; set; }
        [JsonPropertyName("viewedByMeTime")] public DateTimeOffset? ViewedByMeTime { get; set; }
        [JsonPropertyName("modifiedByMeTime")] public DateTimeOffset? ModifiedByMeTime { get; set; }
        [JsonPropertyName("sharedWithMeTime")] public DateTimeOffset? SharedWithMeTime { get; set; }
        [JsonPropertyName("sharingUser")] public GoogleDriveUser SharingUser { get; set; }
        [JsonPropertyName("owners")] public List<GoogleDriveUser> Owners { get; set; } = new();
        [JsonPropertyName("lastModifyingUser")] public GoogleDriveUser LastModifyingUser { get; set; }
        [JsonPropertyName("shared")] public bool? Shared { get; set; }
        [JsonPropertyName("ownedByMe")] public bool? OwnedByMe { get; set; }
        [JsonPropertyName("viewersCanCopyContent")] public bool? ViewersCanCopyContent { get; set; }
        [JsonPropertyName("copyRequiresWriterPermission")] public bool? CopyRequiresWriterPermission { get; set; }
        [JsonPropertyName("writersCanShare")] public bool? WritersCanShare { get; set; }
        [JsonPropertyName("permissions")] public List<GoogleDrivePermission> Permissions { get; set; } = new();
        [JsonPropertyName("permissionIds")] public List<string> PermissionIds { get; set; } = new();
        [JsonPropertyName("hasThumbnail")] public bool? HasThumbnail { get; set; }
        [JsonPropertyName("thumbnailVersion")] public string ThumbnailVersion { get; set; }
        [JsonPropertyName("modifiedByMe")] public bool? ModifiedByMe { get; set; }
        [JsonPropertyName("viewedByMe")] public bool? ViewedByMe { get; set; }
        [JsonPropertyName("starred")] public bool? Starred { get; set; }
        [JsonPropertyName("trashed")] public bool? Trashed { get; set; }
        [JsonPropertyName("explicitlyTrashed")] public bool? ExplicitlyTrashed { get; set; }
        [JsonPropertyName("isAppAuthorized")] public bool? IsAppAuthorized { get; set; }
        [JsonPropertyName("teamDriveId")] public string TeamDriveId { get; set; }
        [JsonPropertyName("driveId")] public string DriveId { get; set; }
        [JsonPropertyName("hasAugmentedPermissions")] public bool? HasAugmentedPermissions { get; set; }
        [JsonPropertyName("trashingUser")] public GoogleDriveUser TrashingUser { get; set; }
        [JsonPropertyName("trashedTime")] public DateTimeOffset? TrashedTime { get; set; }
        [JsonPropertyName("version")] public string Version { get; set; }
        [JsonPropertyName("size")] public string Size { get; set; }
        [JsonPropertyName("quotaBytesUsed")] public string QuotaBytesUsed { get; set; }
        [JsonPropertyName("headRevisionId")] public string HeadRevisionId { get; set; }
        [JsonPropertyName("contentHints")] public GoogleDriveContentHints ContentHints { get; set; }
        [JsonPropertyName("imageMediaMetadata")] public GoogleDriveImageMediaMetadata ImageMediaMetadata { get; set; }
        [JsonPropertyName("videoMediaMetadata")] public GoogleDriveVideoMediaMetadata VideoMediaMetadata { get; set; }
        [JsonPropertyName("capabilities")] public GoogleDriveCapabilities Capabilities { get; set; }
        [JsonPropertyName("hasChildren")] public bool? HasChildren { get; set; }
        [JsonPropertyName("spaces")] public List<string> Spaces { get; set; } = new();
        [JsonPropertyName("folderColorRgb")] public string FolderColorRgb { get; set; }
        [JsonPropertyName("originalFilename")] public string OriginalFilename { get; set; }
        [JsonPropertyName("fullFileExtension")] public string FullFileExtension { get; set; }
        [JsonPropertyName("fileExtension")] public string FileExtension { get; set; }
        [JsonPropertyName("md5Checksum")] public string Md5Checksum { get; set; }
        [JsonPropertyName("sha1Checksum")] public string Sha1Checksum { get; set; }
        [JsonPropertyName("sha256Checksum")] public string Sha256Checksum { get; set; }
        [JsonPropertyName("properties")] public Dictionary<string, string> Properties { get; set; } = new();
        [JsonPropertyName("appProperties")] public Dictionary<string, string> AppProperties { get; set; } = new();
        [JsonPropertyName("exportLinks")] public Dictionary<string, string> ExportLinks { get; set; } = new();
        [JsonPropertyName("shortcutDetails")] public GoogleDriveShortcutDetails ShortcutDetails { get; set; }
        [JsonPropertyName("contentRestrictions")] public List<GoogleDriveContentRestriction> ContentRestrictions { get; set; } = new();
        [JsonPropertyName("resourceKey")] public string ResourceKey { get; set; }
        [JsonPropertyName("linkShareMetadata")] public GoogleDriveLinkShareMetadata LinkShareMetadata { get; set; }
        [JsonPropertyName("labelInfo")] public GoogleDriveLabelInfo LabelInfo { get; set; }
    }

    // -------------------------------------------------------
    // User
    // -------------------------------------------------------
    public sealed class GoogleDriveUser : GoogleDriveEntityBase
    {
        [JsonPropertyName("kind")] public string Kind { get; set; }
        [JsonPropertyName("Caption")] public string Caption { get; set; }
        [JsonPropertyName("photoLink")] public string PhotoLink { get; set; }
        [JsonPropertyName("me")] public bool? Me { get; set; }
        [JsonPropertyName("permissionId")] public string PermissionId { get; set; }
        [JsonPropertyName("emailAddress")] public string EmailAddress { get; set; }
    }

    // -------------------------------------------------------
    // Permission
    // -------------------------------------------------------
    public sealed class GoogleDrivePermission : GoogleDriveEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; } // user, group, domain, anyone
        [JsonPropertyName("emailAddress")] public string EmailAddress { get; set; }
        [JsonPropertyName("domain")] public string Domain { get; set; }
        [JsonPropertyName("role")] public string Role { get; set; } // owner, organizer, fileOrganizer, writer, commenter, reader
        [JsonPropertyName("view")] public string View { get; set; }
        [JsonPropertyName("allowFileDiscovery")] public bool? AllowFileDiscovery { get; set; }
        [JsonPropertyName("Caption")] public string Caption { get; set; }
        [JsonPropertyName("photoLink")] public string PhotoLink { get; set; }
        [JsonPropertyName("expirationTime")] public DateTimeOffset? ExpirationTime { get; set; }
        [JsonPropertyName("teamDrivePermissionDetails")] public List<GoogleDriveTeamDrivePermissionDetail> TeamDrivePermissionDetails { get; set; } = new();
        [JsonPropertyName("permissionDetails")] public List<GoogleDrivePermissionDetail> PermissionDetails { get; set; } = new();
        [JsonPropertyName("deleted")] public bool? Deleted { get; set; }
        [JsonPropertyName("pendingOwner")] public bool? PendingOwner { get; set; }
    }

    // -------------------------------------------------------
    // Revision
    // -------------------------------------------------------
    public sealed class GoogleDriveRevision : GoogleDriveEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("mimeType")] public string MimeType { get; set; }
        [JsonPropertyName("modifiedTime")] public DateTimeOffset? ModifiedTime { get; set; }
        [JsonPropertyName("keepForever")] public bool? KeepForever { get; set; }
        [JsonPropertyName("published")] public bool? Published { get; set; }
        [JsonPropertyName("publishAuto")] public bool? PublishAuto { get; set; }
        [JsonPropertyName("publishedOutsideDomain")] public bool? PublishedOutsideDomain { get; set; }
        [JsonPropertyName("lastModifyingUser")] public GoogleDriveUser LastModifyingUser { get; set; }
        [JsonPropertyName("originalFilename")] public string OriginalFilename { get; set; }
        [JsonPropertyName("md5Checksum")] public string Md5Checksum { get; set; }
        [JsonPropertyName("size")] public string Size { get; set; }
        [JsonPropertyName("exportLinks")] public Dictionary<string, string> ExportLinks { get; set; } = new();
    }

    // -------------------------------------------------------
    // Comment
    // -------------------------------------------------------
    public sealed class GoogleDriveComment : GoogleDriveEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("content")] public string Content { get; set; }
        [JsonPropertyName("modifiedTime")] public DateTimeOffset? ModifiedTime { get; set; }
        [JsonPropertyName("createdTime")] public DateTimeOffset? CreatedTime { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; }
        [JsonPropertyName("author")] public GoogleDriveUser Author { get; set; }
        [JsonPropertyName("replies")] public List<GoogleDriveReply> Replies { get; set; } = new();
        [JsonPropertyName("anchor")] public string Anchor { get; set; }
        [JsonPropertyName("resolved")] public bool? Resolved { get; set; }
        [JsonPropertyName("quotedFileContent")] public GoogleDriveQuotedFileContent QuotedFileContent { get; set; }
    }

    // -------------------------------------------------------
    // Change
    // -------------------------------------------------------
    public sealed class GoogleDriveChange : GoogleDriveEntityBase
    {
        [JsonPropertyName("kind")] public string Kind { get; set; }
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("fileId")] public string FileId { get; set; }
        [JsonPropertyName("time")] public DateTimeOffset? Time { get; set; }
        [JsonPropertyName("removed")] public bool? Removed { get; set; }
        [JsonPropertyName("file")] public GoogleDriveFile File { get; set; }
    }

    // -------------------------------------------------------
    // Supporting Models
    // -------------------------------------------------------
    public sealed class GoogleDriveContentHints
    {
        [JsonPropertyName("thumbnail")] public GoogleDriveThumbnail Thumbnail { get; set; }
        [JsonPropertyName("indexableText")] public string IndexableText { get; set; }
    }

    public sealed class GoogleDriveThumbnail
    {
        [JsonPropertyName("image")] public string Image { get; set; }
        [JsonPropertyName("mimeType")] public string MimeType { get; set; }
    }

    public sealed class GoogleDriveImageMediaMetadata
    {
        [JsonPropertyName("width")] public int? Width { get; set; }
        [JsonPropertyName("height")] public int? Height { get; set; }
        [JsonPropertyName("rotation")] public int? Rotation { get; set; }
        [JsonPropertyName("location")] public GoogleDriveLocation Location { get; set; }
        [JsonPropertyName("time")] public string Time { get; set; }
        [JsonPropertyName("cameraMake")] public string CameraMake { get; set; }
        [JsonPropertyName("cameraModel")] public string CameraModel { get; set; }
        [JsonPropertyName("exposureTime")] public float? ExposureTime { get; set; }
        [JsonPropertyName("aperture")] public float? Aperture { get; set; }
        [JsonPropertyName("flashUsed")] public bool? FlashUsed { get; set; }
        [JsonPropertyName("focalLength")] public float? FocalLength { get; set; }
        [JsonPropertyName("isoSpeed")] public int? IsoSpeed { get; set; }
        [JsonPropertyName("meteringMode")] public string MeteringMode { get; set; }
        [JsonPropertyName("sensor")] public string Sensor { get; set; }
        [JsonPropertyName("exposureMode")] public string ExposureMode { get; set; }
        [JsonPropertyName("colorSpace")] public string ColorSpace { get; set; }
        [JsonPropertyName("whiteBalance")] public string WhiteBalance { get; set; }
        [JsonPropertyName("exposureBias")] public float? ExposureBias { get; set; }
        [JsonPropertyName("maxApertureValue")] public float? MaxApertureValue { get; set; }
        [JsonPropertyName("subjectDistance")] public int? SubjectDistance { get; set; }
        [JsonPropertyName("lens")] public string Lens { get; set; }
    }

    public sealed class GoogleDriveLocation
    {
        [JsonPropertyName("latitude")] public double? Latitude { get; set; }
        [JsonPropertyName("longitude")] public double? Longitude { get; set; }
        [JsonPropertyName("altitude")] public double? Altitude { get; set; }
    }

    public sealed class GoogleDriveVideoMediaMetadata
    {
        [JsonPropertyName("width")] public int? Width { get; set; }
        [JsonPropertyName("height")] public int? Height { get; set; }
        [JsonPropertyName("durationMillis")] public string DurationMillis { get; set; }
    }

    public sealed class GoogleDriveCapabilities
    {
        [JsonPropertyName("canChangeViewersCanCopyContent")] public bool? CanChangeViewersCanCopyContent { get; set; }
        [JsonPropertyName("canChangeCopyRequiresWriterPermission")] public bool? CanChangeCopyRequiresWriterPermission { get; set; }
        [JsonPropertyName("canChangeViewersCanCopyContent")] public bool? CanChangeWritersCanShare { get; set; }
        [JsonPropertyName("canComment")] public bool? CanComment { get; set; }
        [JsonPropertyName("canCopy")] public bool? CanCopy { get; set; }
        [JsonPropertyName("canDelete")] public bool? CanDelete { get; set; }
        [JsonPropertyName("canDownload")] public bool? CanDownload { get; set; }
        [JsonPropertyName("canEdit")] public bool? CanEdit { get; set; }
        [JsonPropertyName("canListChildren")] public bool? CanListChildren { get; set; }
        [JsonPropertyName("canModifyContent")] public bool? CanModifyContent { get; set; }
        [JsonPropertyName("canMoveChildrenOutOfTeamDrive")] public bool? CanMoveChildrenOutOfTeamDrive { get; set; }
        [JsonPropertyName("canMoveChildrenWithinTeamDrive")] public bool? CanMoveChildrenWithinTeamDrive { get; set; }
        [JsonPropertyName("canMoveItemIntoTeamDrive")] public bool? CanMoveItemIntoTeamDrive { get; set; }
        [JsonPropertyName("canMoveItemOutOfTeamDrive")] public bool? CanMoveItemOutOfTeamDrive { get; set; }
        [JsonPropertyName("canMoveItemWithinTeamDrive")] public bool? CanMoveItemWithinTeamDrive { get; set; }
        [JsonPropertyName("canMoveTeamDriveItem")] public bool? CanMoveTeamDriveItem { get; set; }
        [JsonPropertyName("canReadRevisions")] public bool? CanReadRevisions { get; set; }
        [JsonPropertyName("canReadTeamDrive")] public bool? CanReadTeamDrive { get; set; }
        [JsonPropertyName("canRemoveChildren")] public bool? CanRemoveChildren { get; set; }
        [JsonPropertyName("canRename")] public bool? CanRename { get; set; }
        [JsonPropertyName("canShare")] public bool? CanShare { get; set; }
        [JsonPropertyName("canTrash")] public bool? CanTrash { get; set; }
        [JsonPropertyName("canUntrash")] public bool? CanUntrash { get; set; }
    }

    public sealed class GoogleDriveShortcutDetails
    {
        [JsonPropertyName("targetId")] public string TargetId { get; set; }
        [JsonPropertyName("targetMimeType")] public string TargetMimeType { get; set; }
        [JsonPropertyName("targetResourceKey")] public string TargetResourceKey { get; set; }
    }

    public sealed class GoogleDriveContentRestriction
    {
        [JsonPropertyName("readOnly")] public bool? ReadOnly { get; set; }
        [JsonPropertyName("reason")] public string Reason { get; set; }
        [JsonPropertyName("restrictingUser")] public GoogleDriveUser RestrictingUser { get; set; }
        [JsonPropertyName("restrictionTime")] public DateTimeOffset? RestrictionTime { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; }
    }

    public sealed class GoogleDriveLinkShareMetadata
    {
        [JsonPropertyName("securityUpdateEligible")] public bool? SecurityUpdateEligible { get; set; }
        [JsonPropertyName("securityUpdateEnabled")] public bool? SecurityUpdateEnabled { get; set; }
    }

    public sealed class GoogleDriveLabelInfo
    {
        [JsonPropertyName("labels")] public List<GoogleDriveLabel> Labels { get; set; } = new();
    }

    public sealed class GoogleDriveLabel
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("revisionId")] public string RevisionId { get; set; }
        [JsonPropertyName("kind")] public string Kind { get; set; }
        [JsonPropertyName("fields")] public Dictionary<string, GoogleDriveLabelField> Fields { get; set; } = new();
    }

    public sealed class GoogleDriveLabelField
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("kind")] public string Kind { get; set; }
        [JsonPropertyName("valueType")] public string ValueType { get; set; }
        [JsonPropertyName("dateString")] public List<DateTimeOffset?> DateString { get; set; } = new();
        [JsonPropertyName("integer")] public List<string> Integer { get; set; } = new();
        [JsonPropertyName("selection")] public List<string> Selection { get; set; } = new();
        [JsonPropertyName("text")] public List<string> Text { get; set; } = new();
        [JsonPropertyName("user")] public List<GoogleDriveUser> User { get; set; } = new();
    }

    public sealed class GoogleDriveTeamDrivePermissionDetail
    {
        [JsonPropertyName("teamDrivePermissionType")] public string TeamDrivePermissionType { get; set; }
        [JsonPropertyName("role")] public string Role { get; set; }
        [JsonPropertyName("additionalRoles")] public List<string> AdditionalRoles { get; set; } = new();
        [JsonPropertyName("inheritedFrom")] public string InheritedFrom { get; set; }
        [JsonPropertyName("inherited")] public bool? Inherited { get; set; }
    }

    public sealed class GoogleDrivePermissionDetail
    {
        [JsonPropertyName("permissionType")] public string PermissionType { get; set; }
        [JsonPropertyName("role")] public string Role { get; set; }
        [JsonPropertyName("additionalRoles")] public List<string> AdditionalRoles { get; set; } = new();
        [JsonPropertyName("inheritedFrom")] public string InheritedFrom { get; set; }
        [JsonPropertyName("inherited")] public bool? Inherited { get; set; }
    }

    public sealed class GoogleDriveReply
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("content")] public string Content { get; set; }
        [JsonPropertyName("modifiedTime")] public DateTimeOffset? ModifiedTime { get; set; }
        [JsonPropertyName("createdTime")] public DateTimeOffset? CreatedTime { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; }
        [JsonPropertyName("author")] public GoogleDriveUser Author { get; set; }
        [JsonPropertyName("action")] public string Action { get; set; }
    }

    public sealed class GoogleDriveQuotedFileContent
    {
        [JsonPropertyName("mimeType")] public string MimeType { get; set; }
        [JsonPropertyName("value")] public string Value { get; set; }
    }
}