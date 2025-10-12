using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.OneDrive.Models
{
    /// <summary>
    /// Base class for all OneDrive models
    /// </summary>
    public abstract class OneDriveEntityBase
    {
        /// <summary>
        /// Reference to the data source
        /// </summary>
        [JsonIgnore]
        public IDataSource? DataSource { get; private set; }

        public T Attach<T>(IDataSource ds) where T : OneDriveEntityBase 
        { 
            DataSource = ds; 
            return (T)this; 
        }
    }

    /// <summary>
    /// Represents a OneDrive drive
    /// </summary>
    public sealed class Drive : OneDriveEntityBase
    {
        /// <summary>
        /// The unique identifier of the drive
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// The drive resource type
        /// </summary>
        [JsonPropertyName("@odata.type")]
        public string? ODataType { get; set; }

        /// <summary>
        /// The drive resource context
        /// </summary>
        [JsonPropertyName("@odata.context")]
        public string? ODataContext { get; set; }

        /// <summary>
        /// Date and time the drive was created
        /// </summary>
        [JsonPropertyName("createdDateTime")]
        public DateTimeOffset? CreatedDateTime { get; set; }

        /// <summary>
        /// Description of the drive
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// The drive type (personal, business, documentLibrary)
        /// </summary>
        [JsonPropertyName("driveType")]
        public string? DriveType { get; set; }

        /// <summary>
        /// Date and time the drive was last modified
        /// </summary>
        [JsonPropertyName("lastModifiedDateTime")]
        public DateTimeOffset? LastModifiedDateTime { get; set; }

        /// <summary>
        /// The name of the drive
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Owner of the drive
        /// </summary>
        [JsonPropertyName("owner")]
        public IdentitySet? Owner { get; set; }

        /// <summary>
        /// Information about the drive's storage quota
        /// </summary>
        [JsonPropertyName("quota")]
        public Quota? Quota { get; set; }

        /// <summary>
        /// SharePoint IDs
        /// </summary>
        [JsonPropertyName("sharePointIds")]
        public SharePointIds? SharePointIds { get; set; }

        /// <summary>
        /// System facet
        /// </summary>
        [JsonPropertyName("system")]
        public SystemFacet? System { get; set; }

        /// <summary>
        /// Web URL for the drive
        /// </summary>
        [JsonPropertyName("webUrl")]
        public string? WebUrl { get; set; }
    }

    /// <summary>
    /// Represents a drive item (file or folder)
    /// </summary>
    public sealed class DriveItem : OneDriveEntityBase
    {
        /// <summary>
        /// The unique identifier of the item
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// The item resource type
        /// </summary>
        [JsonPropertyName("@odata.type")]
        public string? ODataType { get; set; }

        /// <summary>
        /// The item resource context
        /// </summary>
        [JsonPropertyName("@odata.context")]
        public string? ODataContext { get; set; }

        /// <summary>
        /// Audio metadata if the item is an audio file
        /// </summary>
        [JsonPropertyName("audio")]
        public Audio? Audio { get; set; }

        /// <summary>
        /// Content stream URL for downloading the file
        /// </summary>
        [JsonPropertyName("@microsoft.graph.downloadUrl")]
        public string? DownloadUrl { get; set; }

        /// <summary>
        /// Camera metadata if the item is a photo
        /// </summary>
        [JsonPropertyName("camera")]
        public Camera? Camera { get; set; }

        /// <summary>
        /// Content of the item
        /// </summary>
        [JsonPropertyName("content")]
        public Stream? Content { get; set; }

        /// <summary>
        /// Date and time the item was created
        /// </summary>
        [JsonPropertyName("createdDateTime")]
        public DateTimeOffset? CreatedDateTime { get; set; }

        /// <summary>
        /// ETag for the item
        /// </summary>
        [JsonPropertyName("eTag")]
        public string? ETag { get; set; }

        /// <summary>
        /// File metadata if the item is a file
        /// </summary>
        [JsonPropertyName("file")]
        public FileFacet? File { get; set; }

        /// <summary>
        /// File system info
        /// </summary>
        [JsonPropertyName("fileSystemInfo")]
        public FileSystemInfo? FileSystemInfo { get; set; }

        /// <summary>
        /// Folder metadata if the item is a folder
        /// </summary>
        [JsonPropertyName("folder")]
        public FolderFacet? Folder { get; set; }

        /// <summary>
        /// Image metadata if the item is an image
        /// </summary>
        [JsonPropertyName("image")]
        public Image? Image { get; set; }

        /// <summary>
        /// Date and time the item was last modified
        /// </summary>
        [JsonPropertyName("lastModifiedDateTime")]
        public DateTimeOffset? LastModifiedDateTime { get; set; }

        /// <summary>
        /// Location metadata if available
        /// </summary>
        [JsonPropertyName("location")]
        public GeoCoordinates? Location { get; set; }

        /// <summary>
        /// The name of the item
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Package metadata if the item is a package
        /// </summary>
        [JsonPropertyName("package")]
        public Package? Package { get; set; }

        /// <summary>
        /// Parent reference information
        /// </summary>
        [JsonPropertyName("parentReference")]
        public ItemReference? ParentReference { get; set; }

        /// <summary>
        /// Photo metadata if the item is a photo
        /// </summary>
        [JsonPropertyName("photo")]
        public Photo? Photo { get; set; }

        /// <summary>
        /// Remote item info if this is a remote item
        /// </summary>
        [JsonPropertyName("remoteItem")]
        public RemoteItem? RemoteItem { get; set; }

        /// <summary>
        /// Root item info if this is the root
        /// </summary>
        [JsonPropertyName("root")]
        public Root? Root { get; set; }

        /// <summary>
        /// Search result metadata
        /// </summary>
        [JsonPropertyName("searchResult")]
        public SearchResult? SearchResult { get; set; }

        /// <summary>
        /// Shared metadata
        /// </summary>
        [JsonPropertyName("shared")]
        public Shared? Shared { get; set; }

        /// <summary>
        /// SharePoint IDs
        /// </summary>
        [JsonPropertyName("sharepointIds")]
        public SharePointIds? SharepointIds { get; set; }

        /// <summary>
        /// Size of the item in bytes
        /// </summary>
        [JsonPropertyName("size")]
        public long? Size { get; set; }

        /// <summary>
        /// Special folder metadata
        /// </summary>
        [JsonPropertyName("specialFolder")]
        public SpecialFolder? SpecialFolder { get; set; }

        /// <summary>
        /// Video metadata if the item is a video
        /// </summary>
        [JsonPropertyName("video")]
        public Video? Video { get; set; }

        /// <summary>
        /// Web URL for the item
        /// </summary>
        [JsonPropertyName("webUrl")]
        public string? WebUrl { get; set; }

        /// <summary>
        /// Children of this item (for folders)
        /// </summary>
        [JsonPropertyName("children")]
        public List<DriveItem>? Children { get; set; }

        /// <summary>
        /// Permissions for this item
        /// </summary>
        [JsonPropertyName("permissions")]
        public List<Permission>? Permissions { get; set; }

        /// <summary>
        /// Thumbnails for this item
        /// </summary>
        [JsonPropertyName("thumbnails")]
        public List<ThumbnailSet>? Thumbnails { get; set; }

        /// <summary>
        /// Versions of this item
        /// </summary>
        [JsonPropertyName("versions")]
        public List<DriveItemVersion>? Versions { get; set; }
    }

    /// <summary>
    /// Represents a user identity
    /// </summary>
    public sealed class User : OneDriveEntityBase
    {
        /// <summary>
        /// The unique identifier of the user
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// The user's display name
        /// </summary>
        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        /// <summary>
        /// The user's email address
        /// </summary>
        [JsonPropertyName("mail")]
        public string? Mail { get; set; }

        /// <summary>
        /// The user's principal name
        /// </summary>
        [JsonPropertyName("userPrincipalName")]
        public string? UserPrincipalName { get; set; }
    }

    /// <summary>
    /// Represents an identity set
    /// </summary>
    public sealed class IdentitySet : OneDriveEntityBase
    {
        /// <summary>
        /// User identity
        /// </summary>
        [JsonPropertyName("user")]
        public User? User { get; set; }

        /// <summary>
        /// Application identity
        /// </summary>
        [JsonPropertyName("application")]
        public Identity? Application { get; set; }

        /// <summary>
        /// Device identity
        /// </summary>
        [JsonPropertyName("device")]
        public Identity? Device { get; set; }
    }

    /// <summary>
    /// Represents a generic identity
    /// </summary>
    public sealed class Identity : OneDriveEntityBase
    {
        /// <summary>
        /// The unique identifier
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// The display name
        /// </summary>
        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }
    }

    /// <summary>
    /// Represents drive quota information
    /// </summary>
    public sealed class Quota : OneDriveEntityBase
    {
        /// <summary>
        /// Total storage space
        /// </summary>
        [JsonPropertyName("total")]
        public long? Total { get; set; }

        /// <summary>
        /// Used storage space
        /// </summary>
        [JsonPropertyName("used")]
        public long? Used { get; set; }

        /// <summary>
        /// Remaining storage space
        /// </summary>
        [JsonPropertyName("remaining")]
        public long? Remaining { get; set; }

        /// <summary>
        /// Deleted storage space
        /// </summary>
        [JsonPropertyName("deleted")]
        public long? Deleted { get; set; }

        /// <summary>
        /// Storage state
        /// </summary>
        [JsonPropertyName("state")]
        public string? State { get; set; }
    }

    /// <summary>
    /// Represents SharePoint IDs
    /// </summary>
    public sealed class SharePointIds : OneDriveEntityBase
    {
        /// <summary>
        /// List ID
        /// </summary>
        [JsonPropertyName("listId")]
        public string? ListId { get; set; }

        /// <summary>
        /// List item ID
        /// </summary>
        [JsonPropertyName("listItemId")]
        public string? ListItemId { get; set; }

        /// <summary>
        /// List item unique ID
        /// </summary>
        [JsonPropertyName("listItemUniqueId")]
        public string? ListItemUniqueId { get; set; }

        /// <summary>
        /// Site ID
        /// </summary>
        [JsonPropertyName("siteId")]
        public string? SiteId { get; set; }

        /// <summary>
        /// Site URL
        /// </summary>
        [JsonPropertyName("siteUrl")]
        public string? SiteUrl { get; set; }

        /// <summary>
        /// Tenant ID
        /// </summary>
        [JsonPropertyName("tenantId")]
        public string? TenantId { get; set; }

        /// <summary>
        /// Web ID
        /// </summary>
        [JsonPropertyName("webId")]
        public string? WebId { get; set; }
    }

    /// <summary>
    /// Represents system facet
    /// </summary>
    public sealed class SystemFacet : OneDriveEntityBase
    {
        /// <summary>
        /// System facet type
        /// </summary>
        [JsonPropertyName("@odata.type")]
        public string? ODataType { get; set; }
    }

    /// <summary>
    /// Represents audio metadata
    /// </summary>
    public sealed class Audio : OneDriveEntityBase
    {
        /// <summary>
        /// Album name
        /// </summary>
        [JsonPropertyName("album")]
        public string? Album { get; set; }

        /// <summary>
        /// Album artist
        /// </summary>
        [JsonPropertyName("albumArtist")]
        public string? AlbumArtist { get; set; }

        /// <summary>
        /// Artist name
        /// </summary>
        [JsonPropertyName("artist")]
        public string? Artist { get; set; }

        /// <summary>
        /// Bitrate
        /// </summary>
        [JsonPropertyName("bitrate")]
        public long? Bitrate { get; set; }

        /// <summary>
        /// Composers
        /// </summary>
        [JsonPropertyName("composers")]
        public string? Composers { get; set; }

        /// <summary>
        /// Copyright
        /// </summary>
        [JsonPropertyName("copyright")]
        public string? Copyright { get; set; }

        /// <summary>
        /// Disc number
        /// </summary>
        [JsonPropertyName("disc")]
        public int? Disc { get; set; }

        /// <summary>
        /// Disc count
        /// </summary>
        [JsonPropertyName("discCount")]
        public int? DiscCount { get; set; }

        /// <summary>
        /// Duration in milliseconds
        /// </summary>
        [JsonPropertyName("duration")]
        public long? Duration { get; set; }

        /// <summary>
        /// Genre
        /// </summary>
        [JsonPropertyName("genre")]
        public string? Genre { get; set; }

        /// <summary>
        /// Has DRM
        /// </summary>
        [JsonPropertyName("hasDrm")]
        public bool? HasDrm { get; set; }

        /// <summary>
        /// Is variable bitrate
        /// </summary>
        [JsonPropertyName("isVariableBitrate")]
        public bool? IsVariableBitrate { get; set; }

        /// <summary>
        /// Title
        /// </summary>
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        /// <summary>
        /// Track number
        /// </summary>
        [JsonPropertyName("track")]
        public int? Track { get; set; }

        /// <summary>
        /// Track count
        /// </summary>
        [JsonPropertyName("trackCount")]
        public int? TrackCount { get; set; }

        /// <summary>
        /// Year
        /// </summary>
        [JsonPropertyName("year")]
        public int? Year { get; set; }
    }

    /// <summary>
    /// Represents camera metadata
    /// </summary>
    public sealed class Camera : OneDriveEntityBase
    {
        /// <summary>
        /// Camera manufacturer
        /// </summary>
        [JsonPropertyName("manufacturer")]
        public string? Manufacturer { get; set; }

        /// <summary>
        /// Camera model
        /// </summary>
        [JsonPropertyName("model")]
        public string? Model { get; set; }

        /// <summary>
        /// Date taken
        /// </summary>
        [JsonPropertyName("dateTaken")]
        public DateTimeOffset? DateTaken { get; set; }

        /// <summary>
        /// Focal length
        /// </summary>
        [JsonPropertyName("focalLength")]
        public double? FocalLength { get; set; }

        /// <summary>
        /// ISO value
        /// </summary>
        [JsonPropertyName("iso")]
        public int? Iso { get; set; }
    }

    /// <summary>
    /// Represents file facet
    /// </summary>
    public sealed class FileFacet : OneDriveEntityBase
    {
        /// <summary>
        /// MIME type
        /// </summary>
        [JsonPropertyName("mimeType")]
        public string? MimeType { get; set; }

        /// <summary>
        /// Hashes
        /// </summary>
        [JsonPropertyName("hashes")]
        public Hashes? Hashes { get; set; }
    }

    /// <summary>
    /// Represents file system info
    /// </summary>
    public sealed class FileSystemInfo : OneDriveEntityBase
    {
        /// <summary>
        /// Created date time
        /// </summary>
        [JsonPropertyName("createdDateTime")]
        public DateTimeOffset? CreatedDateTime { get; set; }

        /// <summary>
        /// Last modified date time
        /// </summary>
        [JsonPropertyName("lastModifiedDateTime")]
        public DateTimeOffset? LastModifiedDateTime { get; set; }
    }

    /// <summary>
    /// Represents folder facet
    /// </summary>
    public sealed class FolderFacet : OneDriveEntityBase
    {
        /// <summary>
        /// Child count
        /// </summary>
        [JsonPropertyName("childCount")]
        public int? ChildCount { get; set; }

        /// <summary>
        /// View information
        /// </summary>
        [JsonPropertyName("view")]
        public FolderView? View { get; set; }
    }

    /// <summary>
    /// Represents folder view
    /// </summary>
    public sealed class FolderView : OneDriveEntityBase
    {
        /// <summary>
        /// Sort by
        /// </summary>
        [JsonPropertyName("sortBy")]
        public string? SortBy { get; set; }

        /// <summary>
        /// Sort order
        /// </summary>
        [JsonPropertyName("sortOrder")]
        public string? SortOrder { get; set; }

        /// <summary>
        /// View type
        /// </summary>
        [JsonPropertyName("viewType")]
        public string? ViewType { get; set; }
    }

    /// <summary>
    /// Represents image metadata
    /// </summary>
    public sealed class Image : OneDriveEntityBase
    {
        /// <summary>
        /// Height
        /// </summary>
        [JsonPropertyName("height")]
        public int? Height { get; set; }

        /// <summary>
        /// Width
        /// </summary>
        [JsonPropertyName("width")]
        public int? Width { get; set; }
    }

    /// <summary>
    /// Represents geo coordinates
    /// </summary>
    public sealed class GeoCoordinates : OneDriveEntityBase
    {
        /// <summary>
        /// Altitude
        /// </summary>
        [JsonPropertyName("altitude")]
        public double? Altitude { get; set; }

        /// <summary>
        /// Latitude
        /// </summary>
        [JsonPropertyName("latitude")]
        public double? Latitude { get; set; }

        /// <summary>
        /// Longitude
        /// </summary>
        [JsonPropertyName("longitude")]
        public double? Longitude { get; set; }
    }

    /// <summary>
    /// Represents package metadata
    /// </summary>
    public sealed class Package : OneDriveEntityBase
    {
        /// <summary>
        /// Package type
        /// </summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }

    /// <summary>
    /// Represents item reference
    /// </summary>
    public sealed class ItemReference : OneDriveEntityBase
    {
        /// <summary>
        /// Drive ID
        /// </summary>
        [JsonPropertyName("driveId")]
        public string? DriveId { get; set; }

        /// <summary>
        /// Drive type
        /// </summary>
        [JsonPropertyName("driveType")]
        public string? DriveType { get; set; }

        /// <summary>
        /// Item ID
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// Name
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Path
        /// </summary>
        [JsonPropertyName("path")]
        public string? Path { get; set; }

        /// <summary>
        /// Share ID
        /// </summary>
        [JsonPropertyName("shareId")]
        public string? ShareId { get; set; }

        /// <summary>
        /// SharePoint IDs
        /// </summary>
        [JsonPropertyName("sharepointIds")]
        public SharePointIds? SharepointIds { get; set; }

        /// <summary>
        /// Site ID
        /// </summary>
        [JsonPropertyName("siteId")]
        public string? SiteId { get; set; }
    }

    /// <summary>
    /// Represents photo metadata
    /// </summary>
    public sealed class Photo : OneDriveEntityBase
    {
        /// <summary>
        /// Camera make
        /// </summary>
        [JsonPropertyName("cameraMake")]
        public string? CameraMake { get; set; }

        /// <summary>
        /// Camera model
        /// </summary>
        [JsonPropertyName("cameraModel")]
        public string? CameraModel { get; set; }

        /// <summary>
        /// Exposure denominator
        /// </summary>
        [JsonPropertyName("exposureDenominator")]
        public double? ExposureDenominator { get; set; }

        /// <summary>
        /// Exposure numerator
        /// </summary>
        [JsonPropertyName("exposureNumerator")]
        public double? ExposureNumerator { get; set; }

        /// <summary>
        /// Focal length
        /// </summary>
        [JsonPropertyName("focalLength")]
        public double? FocalLength { get; set; }

        /// <summary>
        /// F-stop
        /// </summary>
        [JsonPropertyName("fNumber")]
        public double? FNumber { get; set; }

        /// <summary>
        /// ISO
        /// </summary>
        [JsonPropertyName("iso")]
        public int? Iso { get; set; }

        /// <summary>
        /// Orientation
        /// </summary>
        [JsonPropertyName("orientation")]
        public int? Orientation { get; set; }

        /// <summary>
        /// Taken date time
        /// </summary>
        [JsonPropertyName("takenDateTime")]
        public DateTimeOffset? TakenDateTime { get; set; }
    }

    /// <summary>
    /// Represents remote item
    /// </summary>
    public sealed class RemoteItem : OneDriveEntityBase
    {
        /// <summary>
        /// Created by
        /// </summary>
        [JsonPropertyName("createdBy")]
        public IdentitySet? CreatedBy { get; set; }

        /// <summary>
        /// Created date time
        /// </summary>
        [JsonPropertyName("createdDateTime")]
        public DateTimeOffset? CreatedDateTime { get; set; }

        /// <summary>
        /// File info
        /// </summary>
        [JsonPropertyName("file")]
        public FileFacet? File { get; set; }

        /// <summary>
        /// File system info
        /// </summary>
        [JsonPropertyName("fileSystemInfo")]
        public FileSystemInfo? FileSystemInfo { get; set; }

        /// <summary>
        /// Folder info
        /// </summary>
        [JsonPropertyName("folder")]
        public FolderFacet? Folder { get; set; }

        /// <summary>
        /// ID
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// Last modified by
        /// </summary>
        [JsonPropertyName("lastModifiedBy")]
        public IdentitySet? LastModifiedBy { get; set; }

        /// <summary>
        /// Last modified date time
        /// </summary>
        [JsonPropertyName("lastModifiedDateTime")]
        public DateTimeOffset? LastModifiedDateTime { get; set; }

        /// <summary>
        /// Name
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Package info
        /// </summary>
        [JsonPropertyName("package")]
        public Package? Package { get; set; }

        /// <summary>
        /// Parent reference
        /// </summary>
        [JsonPropertyName("parentReference")]
        public ItemReference? ParentReference { get; set; }

        /// <summary>
        /// Shared info
        /// </summary>
        [JsonPropertyName("shared")]
        public Shared? Shared { get; set; }

        /// <summary>
        /// SharePoint IDs
        /// </summary>
        [JsonPropertyName("sharepointIds")]
        public SharePointIds? SharepointIds { get; set; }

        /// <summary>
        /// Size
        /// </summary>
        [JsonPropertyName("size")]
        public long? Size { get; set; }

        /// <summary>
        /// Special folder info
        /// </summary>
        [JsonPropertyName("specialFolder")]
        public SpecialFolder? SpecialFolder { get; set; }

        /// <summary>
        /// Video info
        /// </summary>
        [JsonPropertyName("video")]
        public Video? Video { get; set; }

        /// <summary>
        /// Web URL
        /// </summary>
        [JsonPropertyName("webUrl")]
        public string? WebUrl { get; set; }
    }

    /// <summary>
    /// Represents root item
    /// </summary>
    public sealed class Root : OneDriveEntityBase
    {
        /// <summary>
        /// Root type
        /// </summary>
        [JsonPropertyName("@odata.type")]
        public string? ODataType { get; set; }
    }

    /// <summary>
    /// Represents search result
    /// </summary>
    public sealed class SearchResult : OneDriveEntityBase
    {
        /// <summary>
        /// On click telemetry URL
        /// </summary>
        [JsonPropertyName("onClickTelemetryUrl")]
        public string? OnClickTelemetryUrl { get; set; }
    }

    /// <summary>
    /// Represents shared metadata
    /// </summary>
    public sealed class Shared : OneDriveEntityBase
    {
        /// <summary>
        /// Owner
        /// </summary>
        [JsonPropertyName("owner")]
        public IdentitySet? Owner { get; set; }

        /// <summary>
        /// Scope
        /// </summary>
        [JsonPropertyName("scope")]
        public string? Scope { get; set; }

        /// <summary>
        /// Shared by
        /// </summary>
        [JsonPropertyName("sharedBy")]
        public IdentitySet? SharedBy { get; set; }

        /// <summary>
        /// Shared date time
        /// </summary>
        [JsonPropertyName("sharedDateTime")]
        public DateTimeOffset? SharedDateTime { get; set; }
    }

    /// <summary>
    /// Represents special folder
    /// </summary>
    public sealed class SpecialFolder : OneDriveEntityBase
    {
        /// <summary>
        /// Special folder name
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    /// <summary>
    /// Represents video metadata
    /// </summary>
    public sealed class Video : OneDriveEntityBase
    {
        /// <summary>
        /// Audio bits per sample
        /// </summary>
        [JsonPropertyName("audioBitsPerSample")]
        public int? AudioBitsPerSample { get; set; }

        /// <summary>
        /// Audio channels
        /// </summary>
        [JsonPropertyName("audioChannels")]
        public int? AudioChannels { get; set; }

        /// <summary>
        /// Audio format
        /// </summary>
        [JsonPropertyName("audioFormat")]
        public string? AudioFormat { get; set; }

        /// <summary>
        /// Audio samples per second
        /// </summary>
        [JsonPropertyName("audioSamplesPerSecond")]
        public int? AudioSamplesPerSecond { get; set; }

        /// <summary>
        /// Bitrate
        /// </summary>
        [JsonPropertyName("bitrate")]
        public int? Bitrate { get; set; }

        /// <summary>
        /// Duration
        /// </summary>
        [JsonPropertyName("duration")]
        public long? Duration { get; set; }

        /// <summary>
        /// Four character code
        /// </summary>
        [JsonPropertyName("fourCC")]
        public string? FourCC { get; set; }

        /// <summary>
        /// Frame rate
        /// </summary>
        [JsonPropertyName("frameRate")]
        public double? FrameRate { get; set; }

        /// <summary>
        /// Height
        /// </summary>
        [JsonPropertyName("height")]
        public int? Height { get; set; }

        /// <summary>
        /// Width
        /// </summary>
        [JsonPropertyName("width")]
        public int? Width { get; set; }
    }

    /// <summary>
    /// Represents permission
    /// </summary>
    public sealed class Permission : OneDriveEntityBase
    {
        /// <summary>
        /// Granted to
        /// </summary>
        [JsonPropertyName("grantedTo")]
        public IdentitySet? GrantedTo { get; set; }

        /// <summary>
        /// Granted to identities
        /// </summary>
        [JsonPropertyName("grantedToIdentities")]
        public List<IdentitySet>? GrantedToIdentities { get; set; }

        /// <summary>
        /// Has password
        /// </summary>
        [JsonPropertyName("hasPassword")]
        public bool? HasPassword { get; set; }

        /// <summary>
        /// ID
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// Invitation
        /// </summary>
        [JsonPropertyName("invitation")]
        public SharingInvitation? Invitation { get; set; }

        /// <summary>
        /// Inherited from
        /// </summary>
        [JsonPropertyName("inheritedFrom")]
        public ItemReference? InheritedFrom { get; set; }

        /// <summary>
        /// Link
        /// </summary>
        [JsonPropertyName("link")]
        public SharingLink? Link { get; set; }

        /// <summary>
        /// Roles
        /// </summary>
        [JsonPropertyName("roles")]
        public List<string>? Roles { get; set; }

        /// <summary>
        /// Share ID
        /// </summary>
        [JsonPropertyName("shareId")]
        public string? ShareId { get; set; }
    }

    /// <summary>
    /// Represents sharing invitation
    /// </summary>
    public sealed class SharingInvitation : OneDriveEntityBase
    {
        /// <summary>
        /// Email
        /// </summary>
        [JsonPropertyName("email")]
        public string? Email { get; set; }

        /// <summary>
        /// Invited by
        /// </summary>
        [JsonPropertyName("invitedBy")]
        public IdentitySet? InvitedBy { get; set; }

        /// <summary>
        /// Sign in required
        /// </summary>
        [JsonPropertyName("signInRequired")]
        public bool? SignInRequired { get; set; }
    }

    /// <summary>
    /// Represents sharing link
    /// </summary>
    public sealed class SharingLink : OneDriveEntityBase
    {
        /// <summary>
        /// Application
        /// </summary>
        [JsonPropertyName("application")]
        public Identity? Application { get; set; }

        /// <summary>
        /// Prevents download
        /// </summary>
        [JsonPropertyName("preventsDownload")]
        public bool? PreventsDownload { get; set; }

        /// <summary>
        /// Scope
        /// </summary>
        [JsonPropertyName("scope")]
        public string? Scope { get; set; }

        /// <summary>
        /// Type
        /// </summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>
        /// Web HTML
        /// </summary>
        [JsonPropertyName("webHtml")]
        public string? WebHtml { get; set; }

        /// <summary>
        /// Web URL
        /// </summary>
        [JsonPropertyName("webUrl")]
        public string? WebUrl { get; set; }
    }

    /// <summary>
    /// Represents thumbnail set
    /// </summary>
    public sealed class ThumbnailSet : OneDriveEntityBase
    {
        /// <summary>
        /// ID
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// Large thumbnail
        /// </summary>
        [JsonPropertyName("large")]
        public Thumbnail? Large { get; set; }

        /// <summary>
        /// Medium thumbnail
        /// </summary>
        [JsonPropertyName("medium")]
        public Thumbnail? Medium { get; set; }

        /// <summary>
        /// Small thumbnail
        /// </summary>
        [JsonPropertyName("small")]
        public Thumbnail? Small { get; set; }

        /// <summary>
        /// Source thumbnail
        /// </summary>
        [JsonPropertyName("source")]
        public Thumbnail? Source { get; set; }
    }

    /// <summary>
    /// Represents thumbnail
    /// </summary>
    public sealed class Thumbnail : OneDriveEntityBase
    {
        /// <summary>
        /// Content stream
        /// </summary>
        [JsonPropertyName("content")]
        public Stream? Content { get; set; }

        /// <summary>
        /// Height
        /// </summary>
        [JsonPropertyName("height")]
        public int? Height { get; set; }

        /// <summary>
        /// Source item ID
        /// </summary>
        [JsonPropertyName("sourceItemId")]
        public string? SourceItemId { get; set; }

        /// <summary>
        /// URL
        /// </summary>
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        /// <summary>
        /// Width
        /// </summary>
        [JsonPropertyName("width")]
        public int? Width { get; set; }
    }

    /// <summary>
    /// Represents drive item version
    /// </summary>
    public sealed class DriveItemVersion : OneDriveEntityBase
    {
        /// <summary>
        /// ID
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// Last modified by
        /// </summary>
        [JsonPropertyName("lastModifiedBy")]
        public IdentitySet? LastModifiedBy { get; set; }

        /// <summary>
        /// Last modified date time
        /// </summary>
        [JsonPropertyName("lastModifiedDateTime")]
        public DateTimeOffset? LastModifiedDateTime { get; set; }

        /// <summary>
        /// Publication
        /// </summary>
        [JsonPropertyName("publication")]
        public PublicationFacet? Publication { get; set; }

        /// <summary>
        /// Size
        /// </summary>
        [JsonPropertyName("size")]
        public long? Size { get; set; }
    }

    /// <summary>
    /// Represents publication facet
    /// </summary>
    public sealed class PublicationFacet : OneDriveEntityBase
    {
        /// <summary>
        /// Level
        /// </summary>
        [JsonPropertyName("level")]
        public string? Level { get; set; }

        /// <summary>
        /// Version ID
        /// </summary>
        [JsonPropertyName("versionId")]
        public string? VersionId { get; set; }
    }

    /// <summary>
    /// Represents hashes
    /// </summary>
    public sealed class Hashes : OneDriveEntityBase
    {
        /// <summary>
        /// CRC32 hash
        /// </summary>
        [JsonPropertyName("crc32Hash")]
        public string? Crc32Hash { get; set; }

        /// <summary>
        /// Quick XOR hash
        /// </summary>
        [JsonPropertyName("quickXorHash")]
        public string? QuickXorHash { get; set; }

        /// <summary>
        /// SHA1 hash
        /// </summary>
        [JsonPropertyName("sha1Hash")]
        public string? Sha1Hash { get; set; }

        /// <summary>
        /// SHA256 hash
        /// </summary>
        [JsonPropertyName("sha256Hash")]
        public string? Sha256Hash { get; set; }
    }

    /// <summary>
    /// Represents a stream (placeholder for content streams)
    /// </summary>
    public sealed class Stream : OneDriveEntityBase
    {
        // Placeholder for stream content - would be implemented based on actual usage
    }
}