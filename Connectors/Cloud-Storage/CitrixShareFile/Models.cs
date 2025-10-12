using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.CitrixShareFile.Models
{
    /// <summary>
    /// Base class for all Citrix ShareFile entities
    /// </summary>
    public abstract class CitrixShareFileEntityBase
    {
        /// <summary>
        /// Reference to the data source
        /// </summary>
        [JsonIgnore]
        public IDataSource? DataSource { get; set; }

        /// <summary>
        /// Attach the entity to a data source
        /// </summary>
        public T Attach<T>(IDataSource dataSource) where T : CitrixShareFileEntityBase
        {
            DataSource = dataSource;
            return (T)this;
        }
    }

    /// <summary>
    /// Represents a Citrix ShareFile item (file or folder)
    /// </summary>
    public class ShareFileItem : CitrixShareFileEntityBase
    {
        /// <summary>
        /// The unique identifier for this item
        /// </summary>
        [JsonPropertyName("Id")]
        public string? Id { get; set; }

        /// <summary>
        /// The hash of this item
        /// </summary>
        [JsonPropertyName("Hash")]
        public string? Hash { get; set; }

        /// <summary>
        /// The item type
        /// </summary>
        [JsonPropertyName("ItemType")]
        public string? ItemType { get; set; }

        /// <summary>
        /// The name of this item
        /// </summary>
        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        /// <summary>
        /// The filename of this item
        /// </summary>
        [JsonPropertyName("FileName")]
        public string? FileName { get; set; }

        /// <summary>
        /// The creator of this item
        /// </summary>
        [JsonPropertyName("Creator")]
        public ShareFileUser? Creator { get; set; }

        /// <summary>
        /// The parent item
        /// </summary>
        [JsonPropertyName("Parent")]
        public ShareFileItem? Parent { get; set; }

        /// <summary>
        /// The access controls for this item
        /// </summary>
        [JsonPropertyName("AccessControls")]
        public List<ShareFileAccessControl>? AccessControls { get; set; }

        /// <summary>
        /// Whether this item is a template
        /// </summary>
        [JsonPropertyName("IsTemplate")]
        public bool? IsTemplate { get; set; }

        /// <summary>
        /// The description of this item
        /// </summary>
        [JsonPropertyName("Description")]
        public string? Description { get; set; }

        /// <summary>
        /// The preview status of this item
        /// </summary>
        [JsonPropertyName("PreviewStatus")]
        public string? PreviewStatus { get; set; }

        /// <summary>
        /// The preview image URL
        /// </summary>
        [JsonPropertyName("PreviewImageUrl")]
        public string? PreviewImageUrl { get; set; }

        /// <summary>
        /// The stream ID
        /// </summary>
        [JsonPropertyName("StreamId")]
        public string? StreamId { get; set; }

        /// <summary>
        /// The creator first name
        /// </summary>
        [JsonPropertyName("CreatorFirstName")]
        public string? CreatorFirstName { get; set; }

        /// <summary>
        /// The creator last name
        /// </summary>
        [JsonPropertyName("CreatorLastName")]
        public string? CreatorLastName { get; set; }

        /// <summary>
        /// The expiration date
        /// </summary>
        [JsonPropertyName("ExpirationDate")]
        public DateTimeOffset? ExpirationDate { get; set; }

        /// <summary>
        /// The proppatch XML
        /// </summary>
        [JsonPropertyName("ProppatchXml")]
        public string? ProppatchXml { get; set; }

        /// <summary>
        /// The URL of this item
        /// </summary>
        [JsonPropertyName("Url")]
        public string? Url { get; set; }

        /// <summary>
        /// The odata context
        /// </summary>
        [JsonPropertyName("@odata.context")]
        public string? ODataContext { get; set; }

        /// <summary>
        /// The odata type
        /// </summary>
        [JsonPropertyName("@odata.type")]
        public string? ODataType { get; set; }

        /// <summary>
        /// The odata id
        /// </summary>
        [JsonPropertyName("@odata.id")]
        public string? ODataId { get; set; }

        /// <summary>
        /// The odata etag
        /// </summary>
        [JsonPropertyName("@odata.etag")]
        public string? ODataETag { get; set; }

        /// <summary>
        /// The file size
        /// </summary>
        [JsonPropertyName("FileSizeBytes")]
        public long? FileSizeBytes { get; set; }

        /// <summary>
        /// The creation date
        /// </summary>
        [JsonPropertyName("CreationDate")]
        public DateTimeOffset? CreationDate { get; set; }

        /// <summary>
        /// The last modified date
        /// </summary>
        [JsonPropertyName("LastModifiedDate")]
        public DateTimeOffset? LastModifiedDate { get; set; }

        /// <summary>
        /// The last accessed date
        /// </summary>
        [JsonPropertyName("LastAccessedDate")]
        public DateTimeOffset? LastAccessedDate { get; set; }

        /// <summary>
        /// Whether this item has subfolders
        /// </summary>
        [JsonPropertyName("HasSubfolders")]
        public bool? HasSubfolders { get; set; }

        /// <summary>
        /// The children count
        /// </summary>
        [JsonPropertyName("ChildrenCount")]
        public int? ChildrenCount { get; set; }

        /// <summary>
        /// The children
        /// </summary>
        [JsonPropertyName("Children")]
        public List<ShareFileItem>? Children { get; set; }

        /// <summary>
        /// The redacted by
        /// </summary>
        [JsonPropertyName("RedactedBy")]
        public ShareFileUser? RedactedBy { get; set; }

        /// <summary>
        /// The redaction policy
        /// </summary>
        [JsonPropertyName("RedactionPolicy")]
        public string? RedactionPolicy { get; set; }

        /// <summary>
        /// Whether this item is hidden
        /// </summary>
        [JsonPropertyName("IsHidden")]
        public bool? IsHidden { get; set; }

        /// <summary>
        /// The favorite status
        /// </summary>
        [JsonPropertyName("Favorite")]
        public bool? Favorite { get; set; }

        /// <summary>
        /// The tool tip text
        /// </summary>
        [JsonPropertyName("ToolTipText")]
        public string? ToolTipText { get; set; }

        /// <summary>
        /// The locked by
        /// </summary>
        [JsonPropertyName("LockedBy")]
        public ShareFileUser? LockedBy { get; set; }

        /// <summary>
        /// The lock date
        /// </summary>
        [JsonPropertyName("LockDate")]
        public DateTimeOffset? LockDate { get; set; }

        /// <summary>
        /// The lock expiration date
        /// </summary>
        [JsonPropertyName("LockExpirationDate")]
        public DateTimeOffset? LockExpirationDate { get; set; }

        /// <summary>
        /// The versions
        /// </summary>
        [JsonPropertyName("Versions")]
        public List<ShareFileItemVersion>? Versions { get; set; }

        /// <summary>
        /// The custom metadata
        /// </summary>
        [JsonPropertyName("CustomMetadata")]
        public Dictionary<string, object>? CustomMetadata { get; set; }
    }

    /// <summary>
    /// Represents a Citrix ShareFile user
    /// </summary>
    public class ShareFileUser : CitrixShareFileEntityBase
    {
        /// <summary>
        /// The unique identifier for this user
        /// </summary>
        [JsonPropertyName("Id")]
        public string? Id { get; set; }

        /// <summary>
        /// The email address
        /// </summary>
        [JsonPropertyName("Email")]
        public string? Email { get; set; }

        /// <summary>
        /// The first name
        /// </summary>
        [JsonPropertyName("FirstName")]
        public string? FirstName { get; set; }

        /// <summary>
        /// The last name
        /// </summary>
        [JsonPropertyName("LastName")]
        public string? LastName { get; set; }

        /// <summary>
        /// The full name
        /// </summary>
        [JsonPropertyName("FullName")]
        public string? FullName { get; set; }

        /// <summary>
        /// The company
        /// </summary>
        [JsonPropertyName("Company")]
        public string? Company { get; set; }

        /// <summary>
        /// The phone number
        /// </summary>
        [JsonPropertyName("Phone")]
        public string? Phone { get; set; }

        /// <summary>
        /// The account ID
        /// </summary>
        [JsonPropertyName("AccountId")]
        public string? AccountId { get; set; }

        /// <summary>
        /// Whether this user is a guest
        /// </summary>
        [JsonPropertyName("IsGuest")]
        public bool? IsGuest { get; set; }

        /// <summary>
        /// Whether this user is disabled
        /// </summary>
        [JsonPropertyName("IsDisabled")]
        public bool? IsDisabled { get; set; }

        /// <summary>
        /// Whether this user is an employee
        /// </summary>
        [JsonPropertyName("IsEmployee")]
        public bool? IsEmployee { get; set; }

        /// <summary>
        /// The creation date
        /// </summary>
        [JsonPropertyName("CreatedDate")]
        public DateTimeOffset? CreatedDate { get; set; }

        /// <summary>
        /// The last login date
        /// </summary>
        [JsonPropertyName("LastLoginDate")]
        public DateTimeOffset? LastLoginDate { get; set; }

        /// <summary>
        /// The security question
        /// </summary>
        [JsonPropertyName("SecurityQuestion")]
        public string? SecurityQuestion { get; set; }

        /// <summary>
        /// The default zone
        /// </summary>
        [JsonPropertyName("DefaultZone")]
        public ShareFileZone? DefaultZone { get; set; }

        /// <summary>
        /// The preferences
        /// </summary>
        [JsonPropertyName("Preferences")]
        public ShareFileUserPreferences? Preferences { get; set; }

        /// <summary>
        /// The odata context
        /// </summary>
        [JsonPropertyName("@odata.context")]
        public string? ODataContext { get; set; }

        /// <summary>
        /// The odata type
        /// </summary>
        [JsonPropertyName("@odata.type")]
        public string? ODataType { get; set; }

        /// <summary>
        /// The odata id
        /// </summary>
        [JsonPropertyName("@odata.id")]
        public string? ODataId { get; set; }

        /// <summary>
        /// The odata etag
        /// </summary>
        [JsonPropertyName("@odata.etag")]
        public string? ODataETag { get; set; }
    }

    /// <summary>
    /// Represents a Citrix ShareFile group
    /// </summary>
    public class ShareFileGroup : CitrixShareFileEntityBase
    {
        /// <summary>
        /// The unique identifier for this group
        /// </summary>
        [JsonPropertyName("Id")]
        public string? Id { get; set; }

        /// <summary>
        /// The name of this group
        /// </summary>
        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        /// <summary>
        /// The account ID
        /// </summary>
        [JsonPropertyName("AccountId")]
        public string? AccountId { get; set; }

        /// <summary>
        /// Whether this group is everyone
        /// </summary>
        [JsonPropertyName("IsEveryone")]
        public bool? IsEveryone { get; set; }

        /// <summary>
        /// Whether this group is shared
        /// </summary>
        [JsonPropertyName("IsShared")]
        public bool? IsShared { get; set; }

        /// <summary>
        /// The owner
        /// </summary>
        [JsonPropertyName("Owner")]
        public ShareFileUser? Owner { get; set; }

        /// <summary>
        /// The creation date
        /// </summary>
        [JsonPropertyName("CreatedDate")]
        public DateTimeOffset? CreatedDate { get; set; }

        /// <summary>
        /// The URL of this group
        /// </summary>
        [JsonPropertyName("Url")]
        public string? Url { get; set; }

        /// <summary>
        /// The odata context
        /// </summary>
        [JsonPropertyName("@odata.context")]
        public string? ODataContext { get; set; }

        /// <summary>
        /// The odata type
        /// </summary>
        [JsonPropertyName("@odata.type")]
        public string? ODataType { get; set; }

        /// <summary>
        /// The odata id
        /// </summary>
        [JsonPropertyName("@odata.id")]
        public string? ODataId { get; set; }

        /// <summary>
        /// The odata etag
        /// </summary>
        [JsonPropertyName("@odata.etag")]
        public string? ODataETag { get; set; }
    }

    /// <summary>
    /// Represents a Citrix ShareFile share
    /// </summary>
    public class ShareFileShare : CitrixShareFileEntityBase
    {
        /// <summary>
        /// The unique identifier for this share
        /// </summary>
        [JsonPropertyName("Id")]
        public string? Id { get; set; }

        /// <summary>
        /// The name of this share
        /// </summary>
        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        /// <summary>
        /// The alias of this share
        /// </summary>
        [JsonPropertyName("Alias")]
        public string? Alias { get; set; }

        /// <summary>
        /// The type of this share
        /// </summary>
        [JsonPropertyName("ShareType")]
        public string? ShareType { get; set; }

        /// <summary>
        /// The item being shared
        /// </summary>
        [JsonPropertyName("Item")]
        public ShareFileItem? Item { get; set; }

        /// <summary>
        /// The parent item
        /// </summary>
        [JsonPropertyName("Parent")]
        public ShareFileItem? Parent { get; set; }

        /// <summary>
        /// The creator of this share
        /// </summary>
        [JsonPropertyName("Creator")]
        public ShareFileUser? Creator { get; set; }

        /// <summary>
        /// The creation date
        /// </summary>
        [JsonPropertyName("CreationDate")]
        public DateTimeOffset? CreationDate { get; set; }

        /// <summary>
        /// The expiration date
        /// </summary>
        [JsonPropertyName("ExpirationDate")]
        public DateTimeOffset? ExpirationDate { get; set; }

        /// <summary>
        /// Whether this share requires login
        /// </summary>
        [JsonPropertyName("RequireLogin")]
        public bool? RequireLogin { get; set; }

        /// <summary>
        /// Whether this share requires user info
        /// </summary>
        [JsonPropertyName("RequireUserInfo")]
        public bool? RequireUserInfo { get; set; }

        /// <summary>
        /// The max downloads
        /// </summary>
        [JsonPropertyName("MaxDownloads")]
        public int? MaxDownloads { get; set; }

        /// <summary>
        /// The total downloads
        /// </summary>
        [JsonPropertyName("TotalDownloads")]
        public int? TotalDownloads { get; set; }

        /// <summary>
        /// Whether this share is view only
        /// </summary>
        [JsonPropertyName("IsViewOnly")]
        public bool? IsViewOnly { get; set; }

        /// <summary>
        /// Whether this share has subfolders
        /// </summary>
        [JsonPropertyName("HasSubfolders")]
        public bool? HasSubfolders { get; set; }

        /// <summary>
        /// The URL of this share
        /// </summary>
        [JsonPropertyName("Uri")]
        public string? Uri { get; set; }

        /// <summary>
        /// The odata context
        /// </summary>
        [JsonPropertyName("@odata.context")]
        public string? ODataContext { get; set; }

        /// <summary>
        /// The odata type
        /// </summary>
        [JsonPropertyName("@odata.type")]
        public string? ODataType { get; set; }

        /// <summary>
        /// The odata id
        /// </summary>
        [JsonPropertyName("@odata.id")]
        public string? ODataId { get; set; }

        /// <summary>
        /// The odata etag
        /// </summary>
        [JsonPropertyName("@odata.etag")]
        public string? ODataETag { get; set; }
    }

    /// <summary>
    /// Represents a Citrix ShareFile access control
    /// </summary>
    public class ShareFileAccessControl : CitrixShareFileEntityBase
    {
        /// <summary>
        /// The unique identifier for this access control
        /// </summary>
        [JsonPropertyName("Id")]
        public string? Id { get; set; }

        /// <summary>
        /// The principal
        /// </summary>
        [JsonPropertyName("Principal")]
        public ShareFilePrincipal? Principal { get; set; }

        /// <summary>
        /// Whether this access control can upload
        /// </summary>
        [JsonPropertyName("CanUpload")]
        public bool? CanUpload { get; set; }

        /// <summary>
        /// Whether this access control can download
        /// </summary>
        [JsonPropertyName("CanDownload")]
        public bool? CanDownload { get; set; }

        /// <summary>
        /// Whether this access control can view
        /// </summary>
        [JsonPropertyName("CanView")]
        public bool? CanView { get; set; }

        /// <summary>
        /// Whether this access control can delete
        /// </summary>
        [JsonPropertyName("CanDelete")]
        public bool? CanDelete { get; set; }

        /// <summary>
        /// Whether this access control can manage permissions
        /// </summary>
        [JsonPropertyName("CanManagePermissions")]
        public bool? CanManagePermissions { get; set; }

        /// <summary>
        /// The notify on upload
        /// </summary>
        [JsonPropertyName("NotifyOnUpload")]
        public bool? NotifyOnUpload { get; set; }

        /// <summary>
        /// The notify on download
        /// </summary>
        [JsonPropertyName("NotifyOnDownload")]
        public bool? NotifyOnDownload { get; set; }

        /// <summary>
        /// Whether this access control is owner
        /// </summary>
        [JsonPropertyName("IsOwner")]
        public bool? IsOwner { get; set; }

        /// <summary>
        /// The odata context
        /// </summary>
        [JsonPropertyName("@odata.context")]
        public string? ODataContext { get; set; }

        /// <summary>
        /// The odata type
        /// </summary>
        [JsonPropertyName("@odata.type")]
        public string? ODataType { get; set; }

        /// <summary>
        /// The odata id
        /// </summary>
        [JsonPropertyName("@odata.id")]
        public string? ODataId { get; set; }

        /// <summary>
        /// The odata etag
        /// </summary>
        [JsonPropertyName("@odata.etag")]
        public string? ODataETag { get; set; }
    }

    /// <summary>
    /// Represents a Citrix ShareFile principal
    /// </summary>
    public class ShareFilePrincipal : CitrixShareFileEntityBase
    {
        /// <summary>
        /// The unique identifier for this principal
        /// </summary>
        [JsonPropertyName("Id")]
        public string? Id { get; set; }

        /// <summary>
        /// The type of this principal
        /// </summary>
        [JsonPropertyName("Type")]
        public string? Type { get; set; }

        /// <summary>
        /// The name of this principal
        /// </summary>
        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        /// <summary>
        /// The email of this principal
        /// </summary>
        [JsonPropertyName("Email")]
        public string? Email { get; set; }

        /// <summary>
        /// The odata context
        /// </summary>
        [JsonPropertyName("@odata.context")]
        public string? ODataContext { get; set; }

        /// <summary>
        /// The odata type
        /// </summary>
        [JsonPropertyName("@odata.type")]
        public string? ODataType { get; set; }

        /// <summary>
        /// The odata id
        /// </summary>
        [JsonPropertyName("@odata.id")]
        public string? ODataId { get; set; }

        /// <summary>
        /// The odata etag
        /// </summary>
        [JsonPropertyName("@odata.etag")]
        public string? ODataETag { get; set; }
    }

    /// <summary>
    /// Represents a Citrix ShareFile zone
    /// </summary>
    public class ShareFileZone : CitrixShareFileEntityBase
    {
        /// <summary>
        /// The unique identifier for this zone
        /// </summary>
        [JsonPropertyName("Id")]
        public string? Id { get; set; }

        /// <summary>
        /// The name of this zone
        /// </summary>
        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        /// <summary>
        /// Whether this zone is default
        /// </summary>
        [JsonPropertyName("IsDefault")]
        public bool? IsDefault { get; set; }

        /// <summary>
        /// The odata context
        /// </summary>
        [JsonPropertyName("@odata.context")]
        public string? ODataContext { get; set; }

        /// <summary>
        /// The odata type
        /// </summary>
        [JsonPropertyName("@odata.type")]
        public string? ODataType { get; set; }

        /// <summary>
        /// The odata id
        /// </summary>
        [JsonPropertyName("@odata.id")]
        public string? ODataId { get; set; }

        /// <summary>
        /// The odata etag
        /// </summary>
        [JsonPropertyName("@odata.etag")]
        public string? ODataETag { get; set; }
    }

    /// <summary>
    /// Represents Citrix ShareFile user preferences
    /// </summary>
    public class ShareFileUserPreferences : CitrixShareFileEntityBase
    {
        /// <summary>
        /// Whether to enable notifications
        /// </summary>
        [JsonPropertyName("EnableNotifications")]
        public bool? EnableNotifications { get; set; }

        /// <summary>
        /// Whether to enable email notifications
        /// </summary>
        [JsonPropertyName("EnableEmailNotifications")]
        public bool? EnableEmailNotifications { get; set; }

        /// <summary>
        /// The time zone
        /// </summary>
        [JsonPropertyName("TimeZone")]
        public string? TimeZone { get; set; }

        /// <summary>
        /// The date format
        /// </summary>
        [JsonPropertyName("DateFormat")]
        public string? DateFormat { get; set; }

        /// <summary>
        /// The time format
        /// </summary>
        [JsonPropertyName("TimeFormat")]
        public string? TimeFormat { get; set; }

        /// <summary>
        /// The odata context
        /// </summary>
        [JsonPropertyName("@odata.context")]
        public string? ODataContext { get; set; }

        /// <summary>
        /// The odata type
        /// </summary>
        [JsonPropertyName("@odata.type")]
        public string? ODataType { get; set; }

        /// <summary>
        /// The odata id
        /// </summary>
        [JsonPropertyName("@odata.id")]
        public string? ODataId { get; set; }

        /// <summary>
        /// The odata etag
        /// </summary>
        [JsonPropertyName("@odata.etag")]
        public string? ODataETag { get; set; }
    }

    /// <summary>
    /// Represents a Citrix ShareFile item version
    /// </summary>
    public class ShareFileItemVersion : CitrixShareFileEntityBase
    {
        /// <summary>
        /// The unique identifier for this version
        /// </summary>
        [JsonPropertyName("Id")]
        public string? Id { get; set; }

        /// <summary>
        /// The display name of this version
        /// </summary>
        [JsonPropertyName("DisplayName")]
        public string? DisplayName { get; set; }

        /// <summary>
        /// The size of this version
        /// </summary>
        [JsonPropertyName("Size")]
        public long? Size { get; set; }

        /// <summary>
        /// The MD5 hash of this version
        /// </summary>
        [JsonPropertyName("Md5")]
        public string? Md5 { get; set; }

        /// <summary>
        /// The SHA256 hash of this version
        /// </summary>
        [JsonPropertyName("Sha256")]
        public string? Sha256 { get; set; }

        /// <summary>
        /// The creation date of this version
        /// </summary>
        [JsonPropertyName("CreatedDate")]
        public DateTimeOffset? CreatedDate { get; set; }

        /// <summary>
        /// The odata context
        /// </summary>
        [JsonPropertyName("@odata.context")]
        public string? ODataContext { get; set; }

        /// <summary>
        /// The odata type
        /// </summary>
        [JsonPropertyName("@odata.type")]
        public string? ODataType { get; set; }

        /// <summary>
        /// The odata id
        /// </summary>
        [JsonPropertyName("@odata.id")]
        public string? ODataId { get; set; }

        /// <summary>
        /// The odata etag
        /// </summary>
        [JsonPropertyName("@odata.etag")]
        public string? ODataETag { get; set; }
    }

    /// <summary>
    /// Represents a Citrix ShareFile connector group
    /// </summary>
    public class ShareFileConnectorGroup : CitrixShareFileEntityBase
    {
        /// <summary>
        /// The unique identifier for this connector group
        /// </summary>
        [JsonPropertyName("Id")]
        public string? Id { get; set; }

        /// <summary>
        /// The name of this connector group
        /// </summary>
        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        /// <summary>
        /// The description of this connector group
        /// </summary>
        [JsonPropertyName("Description")]
        public string? Description { get; set; }

        /// <summary>
        /// The connector type
        /// </summary>
        [JsonPropertyName("ConnectorType")]
        public string? ConnectorType { get; set; }

        /// <summary>
        /// Whether this connector group is enabled
        /// </summary>
        [JsonPropertyName("IsEnabled")]
        public bool? IsEnabled { get; set; }

        /// <summary>
        /// The account ID
        /// </summary>
        [JsonPropertyName("AccountId")]
        public string? AccountId { get; set; }

        /// <summary>
        /// The odata context
        /// </summary>
        [JsonPropertyName("@odata.context")]
        public string? ODataContext { get; set; }

        /// <summary>
        /// The odata type
        /// </summary>
        [JsonPropertyName("@odata.type")]
        public string? ODataType { get; set; }

        /// <summary>
        /// The odata id
        /// </summary>
        [JsonPropertyName("@odata.id")]
        public string? ODataId { get; set; }

        /// <summary>
        /// The odata etag
        /// </summary>
        [JsonPropertyName("@odata.etag")]
        public string? ODataETag { get; set; }
    }

    /// <summary>
    /// Represents a Citrix ShareFile account
    /// </summary>
    public class ShareFileAccount : CitrixShareFileEntityBase
    {
        /// <summary>
        /// The unique identifier for this account
        /// </summary>
        [JsonPropertyName("Id")]
        public string? Id { get; set; }

        /// <summary>
        /// The name of this account
        /// </summary>
        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        /// <summary>
        /// The company name
        /// </summary>
        [JsonPropertyName("CompanyName")]
        public string? CompanyName { get; set; }

        /// <summary>
        /// The plan type
        /// </summary>
        [JsonPropertyName("PlanType")]
        public string? PlanType { get; set; }

        /// <summary>
        /// The storage quota
        /// </summary>
        [JsonPropertyName("StorageQuota")]
        public long? StorageQuota { get; set; }

        /// <summary>
        /// The storage used
        /// </summary>
        [JsonPropertyName("StorageUsed")]
        public long? StorageUsed { get; set; }

        /// <summary>
        /// The creation date
        /// </summary>
        [JsonPropertyName("CreatedDate")]
        public DateTimeOffset? CreatedDate { get; set; }

        /// <summary>
        /// The odata context
        /// </summary>
        [JsonPropertyName("@odata.context")]
        public string? ODataContext { get; set; }

        /// <summary>
        /// The odata type
        /// </summary>
        [JsonPropertyName("@odata.type")]
        public string? ODataType { get; set; }

        /// <summary>
        /// The odata id
        /// </summary>
        [JsonPropertyName("@odata.id")]
        public string? ODataId { get; set; }

        /// <summary>
        /// The odata etag
        /// </summary>
        [JsonPropertyName("@odata.etag")]
        public string? ODataETag { get; set; }
    }

    /// <summary>
    /// Represents Citrix ShareFile search results
    /// </summary>
    public class ShareFileSearchResult : CitrixShareFileEntityBase
    {
        /// <summary>
        /// The total number of results
        /// </summary>
        [JsonPropertyName("Total")]
        public int? Total { get; set; }

        /// <summary>
        /// The results
        /// </summary>
        [JsonPropertyName("Results")]
        public List<ShareFileItem>? Results { get; set; }

        /// <summary>
        /// The odata context
        /// </summary>
        [JsonPropertyName("@odata.context")]
        public string? ODataContext { get; set; }

        /// <summary>
        /// The odata type
        /// </summary>
        [JsonPropertyName("@odata.type")]
        public string? ODataType { get; set; }

        /// <summary>
        /// The odata id
        /// </summary>
        [JsonPropertyName("@odata.id")]
        public string? ODataId { get; set; }

        /// <summary>
        /// The odata etag
        /// </summary>
        [JsonPropertyName("@odata.etag")]
        public string? ODataETag { get; set; }
    }
}