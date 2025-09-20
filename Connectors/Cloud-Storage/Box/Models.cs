using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TheTechIdea.Beep.Connectors.Box
{
    /// <summary>
    /// Base class for all Box models
    /// </summary>
    public abstract class BoxModel
    {
        /// <summary>
        /// Reference to the data source
        /// </summary>
        [JsonIgnore]
        public BoxDataSource? DataSource { get; set; }
    }

    /// <summary>
    /// Represents a Box item (file or folder)
    /// </summary>
    public class BoxItem : BoxModel
    {
        /// <summary>
        /// The unique identifier for this item
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// The type of the item (file or folder)
        /// </summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>
        /// The name of the item
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// The description of the item
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// The size of the item in bytes
        /// </summary>
        [JsonPropertyName("size")]
        public long? Size { get; set; }

        /// <summary>
        /// The time the item was created
        /// </summary>
        [JsonPropertyName("created_at")]
        public DateTimeOffset? CreatedAt { get; set; }

        /// <summary>
        /// The time the item was last modified
        /// </summary>
        [JsonPropertyName("modified_at")]
        public DateTimeOffset? ModifiedAt { get; set; }

        /// <summary>
        /// The time the item was trashed
        /// </summary>
        [JsonPropertyName("trashed_at")]
        public DateTimeOffset? TrashedAt { get; set; }

        /// <summary>
        /// The time the item was purged
        /// </summary>
        [JsonPropertyName("purged_at")]
        public DateTimeOffset? PurgedAt { get; set; }

        /// <summary>
        /// The time the item was last accessed
        /// </summary>
        [JsonPropertyName("content_created_at")]
        public DateTimeOffset? ContentCreatedAt { get; set; }

        /// <summary>
        /// The time the item content was last modified
        /// </summary>
        [JsonPropertyName("content_modified_at")]
        public DateTimeOffset? ContentModifiedAt { get; set; }

        /// <summary>
        /// The user who created this item
        /// </summary>
        [JsonPropertyName("created_by")]
        public BoxUser? CreatedBy { get; set; }

        /// <summary>
        /// The user who last modified this item
        /// </summary>
        [JsonPropertyName("modified_by")]
        public BoxUser? ModifiedBy { get; set; }

        /// <summary>
        /// The user who owns this item
        /// </summary>
        [JsonPropertyName("owned_by")]
        public BoxUser? OwnedBy { get; set; }

        /// <summary>
        /// The shared link for this item
        /// </summary>
        [JsonPropertyName("shared_link")]
        public BoxSharedLink? SharedLink { get; set; }

        /// <summary>
        /// The parent folder of this item
        /// </summary>
        [JsonPropertyName("parent")]
        public BoxItem? Parent { get; set; }

        /// <summary>
        /// The item status
        /// </summary>
        [JsonPropertyName("item_status")]
        public string? ItemStatus { get; set; }

        /// <summary>
        /// The sequence ID of the item
        /// </summary>
        [JsonPropertyName("sequence_id")]
        public string? SequenceId { get; set; }

        /// <summary>
        /// The etag of the item
        /// </summary>
        [JsonPropertyName("etag")]
        public string? Etag { get; set; }

        /// <summary>
        /// The SHA1 hash of the item
        /// </summary>
        [JsonPropertyName("sha1")]
        public string? Sha1 { get; set; }

        /// <summary>
        /// The file version of the item
        /// </summary>
        [JsonPropertyName("file_version")]
        public BoxFileVersion? FileVersion { get; set; }

        /// <summary>
        /// The collection of items in this folder (for folders only)
        /// </summary>
        [JsonPropertyName("item_collection")]
        public BoxItemCollection? ItemCollection { get; set; }

        /// <summary>
        /// The path collection for this item
        /// </summary>
        [JsonPropertyName("path_collection")]
        public BoxPathCollection? PathCollection { get; set; }

        /// <summary>
        /// The permissions for this item
        /// </summary>
        [JsonPropertyName("permissions")]
        public BoxPermissions? Permissions { get; set; }

        /// <summary>
        /// The tags for this item
        /// </summary>
        [JsonPropertyName("tags")]
        public List<string>? Tags { get; set; }

        /// <summary>
        /// The lock information for this item
        /// </summary>
        [JsonPropertyName("lock")]
        public BoxLock? Lock { get; set; }

        /// <summary>
        /// The extension of the item (for files)
        /// </summary>
        [JsonPropertyName("extension")]
        public string? Extension { get; set; }

        /// <summary>
        /// The watermark info for this item
        /// </summary>
        [JsonPropertyName("watermark_info")]
        public BoxWatermarkInfo? WatermarkInfo { get; set; }

        /// <summary>
        /// The metadata for this item
        /// </summary>
        [JsonPropertyName("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// The representations for this item
        /// </summary>
        [JsonPropertyName("representations")]
        public BoxRepresentationCollection? Representations { get; set; }

        /// <summary>
        /// The classification for this item
        /// </summary>
        [JsonPropertyName("classification")]
        public BoxClassification? Classification { get; set; }

        /// <summary>
        /// Whether this item is externally owned
        /// </summary>
        [JsonPropertyName("is_externally_owned")]
        public bool? IsExternallyOwned { get; set; }

        /// <summary>
        /// The expiration timestamp for this item
        /// </summary>
        [JsonPropertyName("expires_at")]
        public DateTimeOffset? ExpiresAt { get; set; }

        /// <summary>
        /// The allowed invitee roles for this item
        /// </summary>
        [JsonPropertyName("allowed_invitee_roles")]
        public List<string>? AllowedInviteeRoles { get; set; }

        /// <summary>
        /// Whether this item is package
        /// </summary>
        [JsonPropertyName("is_package")]
        public bool? IsPackage { get; set; }
    }

    /// <summary>
    /// Represents a Box user
    /// </summary>
    public class BoxUser : BoxModel
    {
        /// <summary>
        /// The unique identifier for this user
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// The type of this object
        /// </summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>
        /// The name of this user
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// The login of this user
        /// </summary>
        [JsonPropertyName("login")]
        public string? Login { get; set; }

        /// <summary>
        /// The time this user was created
        /// </summary>
        [JsonPropertyName("created_at")]
        public DateTimeOffset? CreatedAt { get; set; }

        /// <summary>
        /// The time this user was last modified
        /// </summary>
        [JsonPropertyName("modified_at")]
        public DateTimeOffset? ModifiedAt { get; set; }

        /// <summary>
        /// The language of this user
        /// </summary>
        [JsonPropertyName("language")]
        public string? Language { get; set; }

        /// <summary>
        /// The timezone of this user
        /// </summary>
        [JsonPropertyName("timezone")]
        public string? Timezone { get; set; }

        /// <summary>
        /// The space amount for this user
        /// </summary>
        [JsonPropertyName("space_amount")]
        public long? SpaceAmount { get; set; }

        /// <summary>
        /// The space used by this user
        /// </summary>
        [JsonPropertyName("space_used")]
        public long? SpaceUsed { get; set; }

        /// <summary>
        /// The max upload size for this user
        /// </summary>
        [JsonPropertyName("max_upload_size")]
        public long? MaxUploadSize { get; set; }

        /// <summary>
        /// The status of this user
        /// </summary>
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        /// <summary>
        /// The job title of this user
        /// </summary>
        [JsonPropertyName("job_title")]
        public string? JobTitle { get; set; }

        /// <summary>
        /// The phone number of this user
        /// </summary>
        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        /// <summary>
        /// The address of this user
        /// </summary>
        [JsonPropertyName("address")]
        public string? Address { get; set; }

        /// <summary>
        /// The avatar URL of this user
        /// </summary>
        [JsonPropertyName("avatar_url")]
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// The role of this user
        /// </summary>
        [JsonPropertyName("role")]
        public string? Role { get; set; }

        /// <summary>
        /// The tracking codes for this user
        /// </summary>
        [JsonPropertyName("tracking_codes")]
        public List<BoxTrackingCode>? TrackingCodes { get; set; }

        /// <summary>
        /// Whether this user can see managed users
        /// </summary>
        [JsonPropertyName("can_see_managed_users")]
        public bool? CanSeeManagedUsers { get; set; }

        /// <summary>
        /// The timezone of this user
        /// </summary>
        [JsonPropertyName("is_sync_enabled")]
        public bool? IsSyncEnabled { get; set; }

        /// <summary>
        /// Whether this user is external collab restricted
        /// </summary>
        [JsonPropertyName("is_external_collab_restricted")]
        public bool? IsExternalCollabRestricted { get; set; }

        /// <summary>
        /// Whether this user is platform access only
        /// </summary>
        [JsonPropertyName("is_platform_access_only")]
        public bool? IsPlatformAccessOnly { get; set; }

        /// <summary>
        /// The external app user ID
        /// </summary>
        [JsonPropertyName("external_app_user_id")]
        public string? ExternalAppUserId { get; set; }

        /// <summary>
        /// The hostname for this user
        /// </summary>
        [JsonPropertyName("hostname")]
        public string? Hostname { get; set; }

        /// <summary>
        /// The notification email for this user
        /// </summary>
        [JsonPropertyName("notification_email")]
        public BoxEmailAlias? NotificationEmail { get; set; }

        /// <summary>
        /// Whether this user is exempt from device limits
        /// </summary>
        [JsonPropertyName("is_exempt_from_device_limits")]
        public bool? IsExemptFromDeviceLimits { get; set; }

        /// <summary>
        /// Whether this user is exempt from login verification
        /// </summary>
        [JsonPropertyName("is_exempt_from_login_verification")]
        public bool? IsExemptFromLoginVerification { get; set; }

        /// <summary>
        /// The enterprise for this user
        /// </summary>
        [JsonPropertyName("enterprise")]
        public BoxEnterprise? Enterprise { get; set; }

        /// <summary>
        /// The my tags for this user
        /// </summary>
        [JsonPropertyName("my_tags")]
        public List<string>? MyTags { get; set; }
    }

    /// <summary>
    /// Represents a Box group
    /// </summary>
    public class BoxGroup : BoxModel
    {
        /// <summary>
        /// The unique identifier for this group
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// The type of this object
        /// </summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>
        /// The name of this group
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// The time this group was created
        /// </summary>
        [JsonPropertyName("created_at")]
        public DateTimeOffset? CreatedAt { get; set; }

        /// <summary>
        /// The time this group was last modified
        /// </summary>
        [JsonPropertyName("modified_at")]
        public DateTimeOffset? ModifiedAt { get; set; }

        /// <summary>
        /// The provenance of this group
        /// </summary>
        [JsonPropertyName("provenance")]
        public string? Provenance { get; set; }

        /// <summary>
        /// The external sync identifier for this group
        /// </summary>
        [JsonPropertyName("external_sync_identifier")]
        public string? ExternalSyncIdentifier { get; set; }

        /// <summary>
        /// The description of this group
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// The invitability level of this group
        /// </summary>
        [JsonPropertyName("invitability_level")]
        public string? InvitabilityLevel { get; set; }

        /// <summary>
        /// The member viewability level of this group
        /// </summary>
        [JsonPropertyName("member_viewability_level")]
        public string? MemberViewabilityLevel { get; set; }
    }

    /// <summary>
    /// Represents a Box shared link
    /// </summary>
    public class BoxSharedLink : BoxModel
    {
        /// <summary>
        /// The URL of the shared link
        /// </summary>
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        /// <summary>
        /// The vanity URL of the shared link
        /// </summary>
        [JsonPropertyName("vanity_url")]
        public string? VanityUrl { get; set; }

        /// <summary>
        /// The vanity name of the shared link
        /// </summary>
        [JsonPropertyName("vanity_name")]
        public string? VanityName { get; set; }

        /// <summary>
        /// The effective access level of the shared link
        /// </summary>
        [JsonPropertyName("effective_access")]
        public string? EffectiveAccess { get; set; }

        /// <summary>
        /// The effective permission level of the shared link
        /// </summary>
        [JsonPropertyName("effective_permission")]
        public string? EffectivePermission { get; set; }

        /// <summary>
        /// The time the shared link was created
        /// </summary>
        [JsonPropertyName("created_at")]
        public DateTimeOffset? CreatedAt { get; set; }

        /// <summary>
        /// The time the shared link was last modified
        /// </summary>
        [JsonPropertyName("modified_at")]
        public DateTimeOffset? ModifiedAt { get; set; }

        /// <summary>
        /// The access level of the shared link
        /// </summary>
        [JsonPropertyName("access")]
        public string? Access { get; set; }

        /// <summary>
        /// The password of the shared link
        /// </summary>
        [JsonPropertyName("password")]
        public string? Password { get; set; }

        /// <summary>
        /// The expiration date of the shared link
        /// </summary>
        [JsonPropertyName("unshared_at")]
        public DateTimeOffset? UnsharedAt { get; set; }

        /// <summary>
        /// Whether the shared link is password enabled
        /// </summary>
        [JsonPropertyName("is_password_enabled")]
        public bool? IsPasswordEnabled { get; set; }

        /// <summary>
        /// The permissions of the shared link
        /// </summary>
        [JsonPropertyName("permissions")]
        public BoxSharedLinkPermissions? Permissions { get; set; }

        /// <summary>
        /// The download URL of the shared link
        /// </summary>
        [JsonPropertyName("download_url")]
        public string? DownloadUrl { get; set; }

        /// <summary>
        /// The preview URL of the shared link
        /// </summary>
        [JsonPropertyName("preview_url")]
        public string? PreviewUrl { get; set; }
    }

    /// <summary>
    /// Represents Box shared link permissions
    /// </summary>
    public class BoxSharedLinkPermissions : BoxModel
    {
        /// <summary>
        /// Whether download is allowed
        /// </summary>
        [JsonPropertyName("can_download")]
        public bool? CanDownload { get; set; }

        /// <summary>
        /// Whether preview is allowed
        /// </summary>
        [JsonPropertyName("can_preview")]
        public bool? CanPreview { get; set; }

        /// <summary>
        /// Whether edit is allowed
        /// </summary>
        [JsonPropertyName("can_edit")]
        public bool? CanEdit { get; set; }
    }

    /// <summary>
    /// Represents a Box file version
    /// </summary>
    public class BoxFileVersion : BoxModel
    {
        /// <summary>
        /// The unique identifier for this file version
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// The type of this object
        /// </summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>
        /// The SHA1 hash of this file version
        /// </summary>
        [JsonPropertyName("sha1")]
        public string? Sha1 { get; set; }

        /// <summary>
        /// The name of this file version
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// The size of this file version
        /// </summary>
        [JsonPropertyName("size")]
        public long? Size { get; set; }

        /// <summary>
        /// The time this file version was created
        /// </summary>
        [JsonPropertyName("created_at")]
        public DateTimeOffset? CreatedAt { get; set; }

        /// <summary>
        /// The time this file version was last modified
        /// </summary>
        [JsonPropertyName("modified_at")]
        public DateTimeOffset? ModifiedAt { get; set; }

        /// <summary>
        /// The user who created this file version
        /// </summary>
        [JsonPropertyName("modified_by")]
        public BoxUser? ModifiedBy { get; set; }

        /// <summary>
        /// The trashed timestamp of this file version
        /// </summary>
        [JsonPropertyName("trashed_at")]
        public DateTimeOffset? TrashedAt { get; set; }

        /// <summary>
        /// The purged timestamp of this file version
        /// </summary>
        [JsonPropertyName("purged_at")]
        public DateTimeOffset? PurgedAt { get; set; }

        /// <summary>
        /// The uploader display name
        /// </summary>
        [JsonPropertyName("uploader_display_name")]
        public string? UploaderDisplayName { get; set; }

        /// <summary>
        /// The version number
        /// </summary>
        [JsonPropertyName("version_number")]
        public string? VersionNumber { get; set; }
    }

    /// <summary>
    /// Represents a Box item collection
    /// </summary>
    public class BoxItemCollection : BoxModel
    {
        /// <summary>
        /// The total count of items
        /// </summary>
        [JsonPropertyName("total_count")]
        public int? TotalCount { get; set; }

        /// <summary>
        /// The entries in this collection
        /// </summary>
        [JsonPropertyName("entries")]
        public List<BoxItem>? Entries { get; set; }

        /// <summary>
        /// The offset of this collection
        /// </summary>
        [JsonPropertyName("offset")]
        public int? Offset { get; set; }

        /// <summary>
        /// The limit of this collection
        /// </summary>
        [JsonPropertyName("limit")]
        public int? Limit { get; set; }

        /// <summary>
        /// The order of this collection
        /// </summary>
        [JsonPropertyName("order")]
        public List<BoxOrder>? Order { get; set; }
    }

    /// <summary>
    /// Represents a Box path collection
    /// </summary>
    public class BoxPathCollection : BoxModel
    {
        /// <summary>
        /// The total count of path items
        /// </summary>
        [JsonPropertyName("total_count")]
        public int? TotalCount { get; set; }

        /// <summary>
        /// The entries in this path collection
        /// </summary>
        [JsonPropertyName("entries")]
        public List<BoxItem>? Entries { get; set; }
    }

    /// <summary>
    /// Represents Box permissions
    /// </summary>
    public class BoxPermissions : BoxModel
    {
        /// <summary>
        /// Whether the user can delete this item
        /// </summary>
        [JsonPropertyName("can_delete")]
        public bool? CanDelete { get; set; }

        /// <summary>
        /// Whether the user can download this item
        /// </summary>
        [JsonPropertyName("can_download")]
        public bool? CanDownload { get; set; }

        /// <summary>
        /// Whether the user can invite collaborator
        /// </summary>
        [JsonPropertyName("can_invite_collaborator")]
        public bool? CanInviteCollaborator { get; set; }

        /// <summary>
        /// Whether the user can rename this item
        /// </summary>
        [JsonPropertyName("can_rename")]
        public bool? CanRename { get; set; }

        /// <summary>
        /// Whether the user can set share link
        /// </summary>
        [JsonPropertyName("can_set_share_link")]
        public bool? CanSetShareLink { get; set; }

        /// <summary>
        /// Whether the user can share this item
        /// </summary>
        [JsonPropertyName("can_share")]
        public bool? CanShare { get; set; }

        /// <summary>
        /// Whether the user can upload to this folder
        /// </summary>
        [JsonPropertyName("can_upload")]
        public bool? CanUpload { get; set; }

        /// <summary>
        /// Whether the user can preview this item
        /// </summary>
        [JsonPropertyName("can_preview")]
        public bool? CanPreview { get; set; }

        /// <summary>
        /// Whether the user can comment on this item
        /// </summary>
        [JsonPropertyName("can_comment")]
        public bool? CanComment { get; set; }

        /// <summary>
        /// Whether the user can view annotations
        /// </summary>
        [JsonPropertyName("can_view_annotations")]
        public bool? CanViewAnnotations { get; set; }
    }

    /// <summary>
    /// Represents a Box lock
    /// </summary>
    public class BoxLock : BoxModel
    {
        /// <summary>
        /// The unique identifier for this lock
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// The type of this object
        /// </summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>
        /// The time this lock was created
        /// </summary>
        [JsonPropertyName("created_at")]
        public DateTimeOffset? CreatedAt { get; set; }

        /// <summary>
        /// The time this lock was last modified
        /// </summary>
        [JsonPropertyName("modified_at")]
        public DateTimeOffset? ModifiedAt { get; set; }

        /// <summary>
        /// The user who created this lock
        /// </summary>
        [JsonPropertyName("created_by")]
        public BoxUser? CreatedBy { get; set; }

        /// <summary>
        /// The expiration timestamp of this lock
        /// </summary>
        [JsonPropertyName("expires_at")]
        public DateTimeOffset? ExpiresAt { get; set; }

        /// <summary>
        /// Whether this lock is download prevented
        /// </summary>
        [JsonPropertyName("is_download_prevented")]
        public bool? IsDownloadPrevented { get; set; }

        /// <summary>
        /// The app type of this lock
        /// </summary>
        [JsonPropertyName("app_type")]
        public string? AppType { get; set; }
    }

    /// <summary>
    /// Represents Box watermark info
    /// </summary>
    public class BoxWatermarkInfo : BoxModel
    {
        /// <summary>
        /// Whether watermarking is enabled
        /// </summary>
        [JsonPropertyName("is_watermarked")]
        public bool? IsWatermarked { get; set; }
    }

    /// <summary>
    /// Represents Box representation collection
    /// </summary>
    public class BoxRepresentationCollection : BoxModel
    {
        /// <summary>
        /// The entries in this representation collection
        /// </summary>
        [JsonPropertyName("entries")]
        public List<BoxRepresentation>? Entries { get; set; }
    }

    /// <summary>
    /// Represents a Box representation
    /// </summary>
    public class BoxRepresentation : BoxModel
    {
        /// <summary>
        /// The content of this representation
        /// </summary>
        [JsonPropertyName("content")]
        public BoxRepresentationContent? Content { get; set; }

        /// <summary>
        /// The info of this representation
        /// </summary>
        [JsonPropertyName("info")]
        public BoxRepresentationInfo? Info { get; set; }

        /// <summary>
        /// The properties of this representation
        /// </summary>
        [JsonPropertyName("properties")]
        public Dictionary<string, object>? Properties { get; set; }

        /// <summary>
        /// The status of this representation
        /// </summary>
        [JsonPropertyName("status")]
        public BoxRepresentationStatus? Status { get; set; }

        /// <summary>
        /// The type of this representation
        /// </summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }

    /// <summary>
    /// Represents Box representation content
    /// </summary>
    public class BoxRepresentationContent : BoxModel
    {
        /// <summary>
        /// The URL of this representation content
        /// </summary>
        [JsonPropertyName("url_template")]
        public string? UrlTemplate { get; set; }
    }

    /// <summary>
    /// Represents Box representation info
    /// </summary>
    public class BoxRepresentationInfo : BoxModel
    {
        /// <summary>
        /// The URL of this representation info
        /// </summary>
        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }

    /// <summary>
    /// Represents Box representation status
    /// </summary>
    public class BoxRepresentationStatus : BoxModel
    {
        /// <summary>
        /// The state of this representation
        /// </summary>
        [JsonPropertyName("state")]
        public string? State { get; set; }
    }

    /// <summary>
    /// Represents Box classification
    /// </summary>
    public class BoxClassification : BoxModel
    {
        /// <summary>
        /// The color of this classification
        /// </summary>
        [JsonPropertyName("color")]
        public string? Color { get; set; }

        /// <summary>
        /// The definition of this classification
        /// </summary>
        [JsonPropertyName("definition")]
        public string? Definition { get; set; }

        /// <summary>
        /// The name of this classification
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    /// <summary>
    /// Represents a Box tracking code
    /// </summary>
    public class BoxTrackingCode : BoxModel
    {
        /// <summary>
        /// The type of this tracking code
        /// </summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>
        /// The name of this tracking code
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// The value of this tracking code
        /// </summary>
        [JsonPropertyName("value")]
        public string? Value { get; set; }
    }

    /// <summary>
    /// Represents a Box email alias
    /// </summary>
    public class BoxEmailAlias : BoxModel
    {
        /// <summary>
        /// The email address
        /// </summary>
        [JsonPropertyName("email")]
        public string? Email { get; set; }

        /// <summary>
        /// Whether this is confirmed
        /// </summary>
        [JsonPropertyName("is_confirmed")]
        public bool? IsConfirmed { get; set; }
    }

    /// <summary>
    /// Represents a Box enterprise
    /// </summary>
    public class BoxEnterprise : BoxModel
    {
        /// <summary>
        /// The unique identifier for this enterprise
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// The type of this object
        /// </summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>
        /// The name of this enterprise
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    /// <summary>
    /// Represents a Box order
    /// </summary>
    public class BoxOrder : BoxModel
    {
        /// <summary>
        /// The by field for this order
        /// </summary>
        [JsonPropertyName("by")]
        public string? By { get; set; }

        /// <summary>
        /// The direction of this order
        /// </summary>
        [JsonPropertyName("direction")]
        public string? Direction { get; set; }
    }

    /// <summary>
    /// Represents a Box webhook
    /// </summary>
    public class BoxWebhook : BoxModel
    {
        /// <summary>
        /// The unique identifier for this webhook
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// The type of this object
        /// </summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>
        /// The target of this webhook
        /// </summary>
        [JsonPropertyName("target")]
        public BoxWebhookTarget? Target { get; set; }

        /// <summary>
        /// The address of this webhook
        /// </summary>
        [JsonPropertyName("address")]
        public string? Address { get; set; }

        /// <summary>
        /// The triggers of this webhook
        /// </summary>
        [JsonPropertyName("triggers")]
        public List<string>? Triggers { get; set; }

        /// <summary>
        /// The time this webhook was created
        /// </summary>
        [JsonPropertyName("created_at")]
        public DateTimeOffset? CreatedAt { get; set; }

        /// <summary>
        /// The time this webhook was last modified
        /// </summary>
        [JsonPropertyName("modified_at")]
        public DateTimeOffset? ModifiedAt { get; set; }

        /// <summary>
        /// The user who created this webhook
        /// </summary>
        [JsonPropertyName("created_by")]
        public BoxUser? CreatedBy { get; set; }
    }

    /// <summary>
    /// Represents a Box webhook target
    /// </summary>
    public class BoxWebhookTarget : BoxModel
    {
        /// <summary>
        /// The unique identifier for this target
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// The type of this target
        /// </summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }

    /// <summary>
    /// Represents Box search results
    /// </summary>
    public class BoxSearchResult : BoxModel
    {
        /// <summary>
        /// The total count of search results
        /// </summary>
        [JsonPropertyName("total_count")]
        public int? TotalCount { get; set; }

        /// <summary>
        /// <summary>
        /// The offset of search results
        /// </summary>
        [JsonPropertyName("offset")]
        public int? Offset { get; set; }

        /// <summary>
        /// The limit of search results
        ///</summary>
        [JsonPropertyName("limit")]
        public int? Limit { get; set; }

        /// <summary>
        /// The entries in search results
        /// </summary>
        [JsonPropertyName("entries")]
        public List<BoxItem>? Entries { get; set; }

        /// <summary>
        /// The type of search results
        /// </summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }
}