using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Egnyte.Models
{
    /// <summary>
    /// Base class for all Egnyte entities
    /// </summary>
    public abstract class EgnyteEntityBase
    {
        /// <summary>
        /// Reference to the data source
        /// </summary>
        [JsonIgnore]
        public IDataSource? DataSource { get; set; }

        /// <summary>
        /// Attach the entity to a data source
        /// </summary>
        public T Attach<T>(IDataSource dataSource) where T : EgnyteEntityBase
        {
            DataSource = dataSource;
            return (T)this;
        }
    }

    /// <summary>
    /// Represents an Egnyte file or folder item
    /// </summary>
    public sealed class EgnyteItem : EgnyteEntityBase
    {
        /// <summary>
        /// The checksum of the item
        /// </summary>
        [JsonPropertyName("checksum")]
        public string? Checksum { get; set; }

        /// <summary>
        /// The size of the item in bytes
        /// </summary>
        [JsonPropertyName("size")]
        public string? Size { get; set; }

        /// <summary>
        /// The path of the item
        /// </summary>
        [JsonPropertyName("path")]
        public string? Path { get; set; }

        /// <summary>
        /// The name of the item
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Whether the item is a folder
        /// </summary>
        [JsonPropertyName("is_folder")]
        public bool? IsFolder { get; set; }

        /// <summary>
        /// The last modified date of the item
        /// </summary>
        [JsonPropertyName("last_modified")]
        public string? LastModified { get; set; }

        /// <summary>
        /// The uploaded date of the item
        /// </summary>
        [JsonPropertyName("uploaded")]
        public string? Uploaded { get; set; }

        /// <summary>
        /// The number of versions of the item
        /// </summary>
        [JsonPropertyName("num_versions")]
        public int? NumVersions { get; set; }

        /// <summary>
        /// The entry ID of the item
        /// </summary>
        [JsonPropertyName("entry_id")]
        public string? EntryId { get; set; }

        /// <summary>
        /// The group ID of the item
        /// </summary>
        [JsonPropertyName("group_id")]
        public string? GroupId { get; set; }

        /// <summary>
        /// The custom metadata of the item
        /// </summary>
        [JsonPropertyName("custom_metadata")]
        public Dictionary<string, object>? CustomMetadata { get; set; }

        /// <summary>
        /// The locked status of the item
        /// </summary>
        [JsonPropertyName("locked")]
        public bool? Locked { get; set; }

        /// <summary>
        /// The upload ID of the item
        /// </summary>
        [JsonPropertyName("upload_id")]
        public string? UploadId { get; set; }

        /// <summary>
        /// The parent folder of the item
        /// </summary>
        [JsonPropertyName("parent_id")]
        public string? ParentId { get; set; }

        /// <summary>
        /// The folder ID of the item
        /// </summary>
        [JsonPropertyName("folder_id")]
        public string? FolderId { get; set; }

        /// <summary>
        /// The count of items in the folder (for folders only)
        /// </summary>
        [JsonPropertyName("count")]
        public int? Count { get; set; }

        /// <summary>
        /// The offset for pagination
        /// </summary>
        [JsonPropertyName("offset")]
        public int? Offset { get; set; }

        /// <summary>
        /// The path ID of the item
        /// </summary>
        [JsonPropertyName("path_id")]
        public string? PathId { get; set; }

        /// <summary>
        /// The total count of items
        /// </summary>
        [JsonPropertyName("total_count")]
        public int? TotalCount { get; set; }

        /// <summary>
        /// The folders in the response
        /// </summary>
        [JsonPropertyName("folders")]
        public List<EgnyteItem>? Folders { get; set; }

        /// <summary>
        /// The files in the response
        /// </summary>
        [JsonPropertyName("files")]
        public List<EgnyteItem>? Files { get; set; }
    }

    /// <summary>
    /// Represents an Egnyte folder
    /// </summary>
    public sealed class EgnyteFolder : EgnyteEntityBase
    {
        /// <summary>
        /// The checksum of the folder
        /// </summary>
        [JsonPropertyName("checksum")]
        public string? Checksum { get; set; }

        /// <summary>
        /// The size of the folder in bytes
        /// </summary>
        [JsonPropertyName("size")]
        public string? Size { get; set; }

        /// <summary>
        /// The path of the folder
        /// </summary>
        [JsonPropertyName("path")]
        public string? Path { get; set; }

        /// <summary>
        /// The name of the folder
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Whether the item is a folder (should be true)
        /// </summary>
        [JsonPropertyName("is_folder")]
        public bool? IsFolder { get; set; } = true;

        /// <summary>
        /// The last modified date of the folder
        /// </summary>
        [JsonPropertyName("last_modified")]
        public string? LastModified { get; set; }

        /// <summary>
        /// The uploaded date of the folder
        /// </summary>
        [JsonPropertyName("uploaded")]
        public string? Uploaded { get; set; }

        /// <summary>
        /// The entry ID of the folder
        /// </summary>
        [JsonPropertyName("entry_id")]
        public string? EntryId { get; set; }

        /// <summary>
        /// The group ID of the folder
        /// </summary>
        [JsonPropertyName("group_id")]
        public string? GroupId { get; set; }

        /// <summary>
        /// The custom metadata of the folder
        /// </summary>
        [JsonPropertyName("custom_metadata")]
        public Dictionary<string, object>? CustomMetadata { get; set; }

        /// <summary>
        /// The locked status of the folder
        /// </summary>
        [JsonPropertyName("locked")]
        public bool? Locked { get; set; }

        /// <summary>
        /// The parent folder of the folder
        /// </summary>
        [JsonPropertyName("parent_id")]
        public string? ParentId { get; set; }

        /// <summary>
        /// The folder ID of the folder
        /// </summary>
        [JsonPropertyName("folder_id")]
        public string? FolderId { get; set; }

        /// <summary>
        /// The count of items in the folder
        /// </summary>
        [JsonPropertyName("count")]
        public int? Count { get; set; }

        /// <summary>
        /// The offset for pagination
        /// </summary>
        [JsonPropertyName("offset")]
        public int? Offset { get; set; }

        /// <summary>
        /// The path ID of the folder
        /// </summary>
        [JsonPropertyName("path_id")]
        public string? PathId { get; set; }

        /// <summary>
        /// The total count of items
        /// </summary>
        [JsonPropertyName("total_count")]
        public int? TotalCount { get; set; }

        /// <summary>
        /// The folders in the response
        /// </summary>
        [JsonPropertyName("folders")]
        public List<EgnyteItem>? Folders { get; set; }

        /// <summary>
        /// The files in the response
        /// </summary>
        [JsonPropertyName("files")]
        public List<EgnyteItem>? Files { get; set; }
    }

    /// <summary>
    /// Represents an Egnyte file
    /// </summary>
    public sealed class EgnyteFile : EgnyteEntityBase
    {
        /// <summary>
        /// The checksum of the file
        /// </summary>
        [JsonPropertyName("checksum")]
        public string? Checksum { get; set; }

        /// <summary>
        /// The size of the file in bytes
        /// </summary>
        [JsonPropertyName("size")]
        public string? Size { get; set; }

        /// <summary>
        /// The path of the file
        /// </summary>
        [JsonPropertyName("path")]
        public string? Path { get; set; }

        /// <summary>
        /// The name of the file
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Whether the item is a folder (should be false)
        /// </summary>
        [JsonPropertyName("is_folder")]
        public bool? IsFolder { get; set; } = false;

        /// <summary>
        /// The last modified date of the file
        /// </summary>
        [JsonPropertyName("last_modified")]
        public string? LastModified { get; set; }

        /// <summary>
        /// The uploaded date of the file
        /// </summary>
        [JsonPropertyName("uploaded")]
        public string? Uploaded { get; set; }

        /// <summary>
        /// The number of versions of the file
        /// </summary>
        [JsonPropertyName("num_versions")]
        public int? NumVersions { get; set; }

        /// <summary>
        /// The entry ID of the file
        /// </summary>
        [JsonPropertyName("entry_id")]
        public string? EntryId { get; set; }

        /// <summary>
        /// The group ID of the file
        /// </summary>
        [JsonPropertyName("group_id")]
        public string? GroupId { get; set; }

        /// <summary>
        /// The custom metadata of the file
        /// </summary>
        [JsonPropertyName("custom_metadata")]
        public Dictionary<string, object>? CustomMetadata { get; set; }

        /// <summary>
        /// The locked status of the file
        /// </summary>
        [JsonPropertyName("locked")]
        public bool? Locked { get; set; }

        /// <summary>
        /// The upload ID of the file
        /// </summary>
        [JsonPropertyName("upload_id")]
        public string? UploadId { get; set; }

        /// <summary>
        /// The parent folder of the file
        /// </summary>
        [JsonPropertyName("parent_id")]
        public string? ParentId { get; set; }

        /// <summary>
        /// The folder ID of the file
        /// </summary>
        [JsonPropertyName("folder_id")]
        public string? FolderId { get; set; }

        /// <summary>
        /// The path ID of the file
        /// </summary>
        [JsonPropertyName("path_id")]
        public string? PathId { get; set; }
    }

    /// <summary>
    /// Represents an Egnyte user
    /// </summary>
    public sealed class EgnyteUser : EgnyteEntityBase
    {
        /// <summary>
        /// The username
        /// </summary>
        [JsonPropertyName("username")]
        public string? Username { get; set; }

        /// <summary>
        /// The user ID
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// The email address
        /// </summary>
        [JsonPropertyName("email")]
        public string? Email { get; set; }

        /// <summary>
        /// The first name
        /// </summary>
        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }

        /// <summary>
        /// The last name
        /// </summary>
        [JsonPropertyName("last_name")]
        public string? LastName { get; set; }

        /// <summary>
        /// The user type
        /// </summary>
        [JsonPropertyName("user_type")]
        public string? UserType { get; set; }

        /// <summary>
        /// Whether the user is active
        /// </summary>
        [JsonPropertyName("active")]
        public bool? Active { get; set; }

        /// <summary>
        /// The user principal name
        /// </summary>
        [JsonPropertyName("user_principal_name")]
        public string? UserPrincipalName { get; set; }

        /// <summary>
        /// The external ID
        /// </summary>
        [JsonPropertyName("external_id")]
        public string? ExternalId { get; set; }

        /// <summary>
        /// The role
        /// </summary>
        [JsonPropertyName("role")]
        public string? Role { get; set; }

        /// <summary>
        /// The IDP user ID
        /// </summary>
        [JsonPropertyName("idp_user_id")]
        public string? IdpUserId { get; set; }

        /// <summary>
        /// The manager username
        /// </summary>
        [JsonPropertyName("manager_username")]
        public string? ManagerUsername { get; set; }

        /// <summary>
        /// The employee number
        /// </summary>
        [JsonPropertyName("employee_number")]
        public string? EmployeeNumber { get; set; }

        /// <summary>
        /// The department
        /// </summary>
        [JsonPropertyName("department")]
        public string? Department { get; set; }

        /// <summary>
        /// The business unit
        /// </summary>
        [JsonPropertyName("business_unit")]
        public string? BusinessUnit { get; set; }

        /// <summary>
        /// The title
        /// </summary>
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        /// <summary>
        /// The cost center
        /// </summary>
        [JsonPropertyName("cost_center")]
        public string? CostCenter { get; set; }

        /// <summary>
        /// The hire date
        /// </summary>
        [JsonPropertyName("hire_date")]
        public string? HireDate { get; set; }

        /// <summary>
        /// The termination date
        /// </summary>
        [JsonPropertyName("termination_date")]
        public string? TerminationDate { get; set; }

        /// <summary>
        /// The phone number
        /// </summary>
        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        /// <summary>
        /// The mobile phone number
        /// </summary>
        [JsonPropertyName("mobile_phone")]
        public string? MobilePhone { get; set; }

        /// <summary>
        /// The fax number
        /// </summary>
        [JsonPropertyName("fax")]
        public string? Fax { get; set; }

        /// <summary>
        /// The address
        /// </summary>
        [JsonPropertyName("address")]
        public string? Address { get; set; }

        /// <summary>
        /// The city
        /// </summary>
        [JsonPropertyName("city")]
        public string? City { get; set; }

        /// <summary>
        /// The state
        /// </summary>
        [JsonPropertyName("state")]
        public string? State { get; set; }

        /// <summary>
        /// The zip code
        /// </summary>
        [JsonPropertyName("zip_code")]
        public string? ZipCode { get; set; }

        /// <summary>
        /// The country
        /// </summary>
        [JsonPropertyName("country")]
        public string? Country { get; set; }

        /// <summary>
        /// The groups the user belongs to
        /// </summary>
        [JsonPropertyName("groups")]
        public List<EgnyteGroup>? Groups { get; set; }
    }

    /// <summary>
    /// Represents an Egnyte group
    /// </summary>
    public sealed class EgnyteGroup : EgnyteEntityBase
    {
        /// <summary>
        /// The group ID
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// The group name
        /// </summary>
        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }

        /// <summary>
        /// The group description
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// The group type
        /// </summary>
        [JsonPropertyName("group_type")]
        public string? GroupType { get; set; }

        /// <summary>
        /// The members of the group
        /// </summary>
        [JsonPropertyName("members")]
        public List<EgnyteUser>? Members { get; set; }

        /// <summary>
        /// The member count
        /// </summary>
        [JsonPropertyName("member_count")]
        public int? MemberCount { get; set; }

        /// <summary>
        /// The parent group ID
        /// </summary>
        [JsonPropertyName("parent_id")]
        public string? ParentId { get; set; }

        /// <summary>
        /// The external ID
        /// </summary>
        [JsonPropertyName("external_id")]
        public string? ExternalId { get; set; }

        /// <summary>
        /// Whether the group is synced
        /// </summary>
        [JsonPropertyName("synced")]
        public bool? Synced { get; set; }
    }

    /// <summary>
    /// Represents an Egnyte link
    /// </summary>
    public sealed class EgnyteLink : EgnyteEntityBase
    {
        /// <summary>
        /// The link ID
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// The link URL
        /// </summary>
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        /// <summary>
        /// The link type
        /// </summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>
        /// The accessibility of the link
        /// </summary>
        [JsonPropertyName("accessibility")]
        public string? Accessibility { get; set; }

        /// <summary>
        /// Whether the link requires authentication
        /// </summary>
        [JsonPropertyName("require_auth")]
        public bool? RequireAuth { get; set; }

        /// <summary>
        /// The creation date of the link
        /// </summary>
        [JsonPropertyName("creation_date")]
        public string? CreationDate { get; set; }

        /// <summary>
        /// The creator username
        /// </summary>
        [JsonPropertyName("creator_username")]
        public string? CreatorUsername { get; set; }

        /// <summary>
        /// The last accessed date
        /// </summary>
        [JsonPropertyName("last_accessed")]
        public string? LastAccessed { get; set; }

        /// <summary>
        /// The number of clicks
        /// </summary>
        [JsonPropertyName("clicks")]
        public int? Clicks { get; set; }

        /// <summary>
        /// The path of the linked item
        /// </summary>
        [JsonPropertyName("path")]
        public string? Path { get; set; }

        /// <summary>
        /// The expiry date
        /// </summary>
        [JsonPropertyName("expiry_date")]
        public string? ExpiryDate { get; set; }

        /// <summary>
        /// The expiry clicks
        /// </summary>
        [JsonPropertyName("expiry_clicks")]
        public int? ExpiryClicks { get; set; }

        /// <summary>
        /// Whether the link is active
        /// </summary>
        [JsonPropertyName("active")]
        public bool? Active { get; set; }

        /// <summary>
        /// The send emails
        /// </summary>
        [JsonPropertyName("send_emails")]
        public List<string>? SendEmails { get; set; }

        /// <summary>
        /// The notify on download
        /// </summary>
        [JsonPropertyName("notify_on_download")]
        public bool? NotifyOnDownload { get; set; }

        /// <summary>
        /// The recipients
        /// </summary>
        [JsonPropertyName("recipients")]
        public List<EgnyteLinkRecipient>? Recipients { get; set; }
    }

    /// <summary>
    /// Represents an Egnyte link recipient
    /// </summary>
    public sealed class EgnyteLinkRecipient : EgnyteEntityBase
    {
        /// <summary>
        /// The recipient email
        /// </summary>
        [JsonPropertyName("email")]
        public string? Email { get; set; }

        /// <summary>
        /// The recipient name
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Whether the email was sent
        /// </summary>
        [JsonPropertyName("sent")]
        public bool? Sent { get; set; }
    }

    /// <summary>
    /// Represents Egnyte permissions
    /// </summary>
    public sealed class EgnytePermissions : EgnyteEntityBase
    {
        /// <summary>
        /// The user permissions
        /// </summary>
        [JsonPropertyName("users")]
        public Dictionary<string, EgnytePermission>? Users { get; set; }

        /// <summary>
        /// The group permissions
        /// </summary>
        [JsonPropertyName("groups")]
        public Dictionary<string, EgnytePermission>? Groups { get; set; }

        /// <summary>
        /// The path
        /// </summary>
        [JsonPropertyName("path")]
        public string? Path { get; set; }

        /// <summary>
        /// Whether inheritance is broken
        /// </summary>
        [JsonPropertyName("inheritance_disabled")]
        public bool? InheritanceDisabled { get; set; }
    }

    /// <summary>
    /// Represents an Egnyte permission
    /// </summary>
    public sealed class EgnytePermission : EgnyteEntityBase
    {
        /// <summary>
        /// The permission level
        /// </summary>
        [JsonPropertyName("permission")]
        public string? Permission { get; set; }

        /// <summary>
        /// The subject type
        /// </summary>
        [JsonPropertyName("subject_type")]
        public string? SubjectType { get; set; }
    }

    /// <summary>
    /// Represents Egnyte search results
    /// </summary>
    public sealed class EgnyteSearchResult : EgnyteEntityBase
    {
        /// <summary>
        /// The total count of search results
        /// </summary>
        [JsonPropertyName("total_count")]
        public int? TotalCount { get; set; }

        /// <summary>
        /// The offset of search results
        /// </summary>
        [JsonPropertyName("offset")]
        public int? Offset { get; set; }

        /// <summary>
        /// The count of search results
        /// </summary>
        [JsonPropertyName("count")]
        public int? Count { get; set; }

        /// <summary>
        /// The search results
        /// </summary>
        [JsonPropertyName("results")]
        public List<EgnyteItem>? Results { get; set; }

        /// <summary>
        /// The facets
        /// </summary>
        [JsonPropertyName("facets")]
        public Dictionary<string, List<EgnyteFacet>>? Facets { get; set; }
    }

    /// <summary>
    /// Represents an Egnyte facet
    /// </summary>
    public sealed class EgnyteFacet : EgnyteEntityBase
    {
        /// <summary>
        /// The facet key
        /// </summary>
        [JsonPropertyName("key")]
        public string? Key { get; set; }

        /// <summary>
        /// The facet label
        /// </summary>
        [JsonPropertyName("label")]
        public string? Label { get; set; }

        /// <summary>
        /// The facet count
        /// </summary>
        [JsonPropertyName("count")]
        public int? Count { get; set; }
    }

    /// <summary>
    /// Represents Egnyte audit events
    /// </summary>
    public sealed class EgnyteAuditEvent : EgnyteEntityBase
    {
        /// <summary>
        /// The event ID
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// The timestamp of the event
        /// </summary>
        [JsonPropertyName("timestamp")]
        public string? Timestamp { get; set; }

        /// <summary>
        /// The actor username
        /// </summary>
        [JsonPropertyName("actor")]
        public string? Actor { get; set; }

        /// <summary>
        /// The action performed
        /// </summary>
        [JsonPropertyName("action")]
        public string? Action { get; set; }

        /// <summary>
        /// The target path
        /// </summary>
        [JsonPropertyName("target")]
        public string? Target { get; set; }

        /// <summary>
        /// The description of the event
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// The IP address
        /// </summary>
        [JsonPropertyName("ip_address")]
        public string? IpAddress { get; set; }

        /// <summary>
        /// The user agent
        /// </summary>
        [JsonPropertyName("user_agent")]
        public string? UserAgent { get; set; }

        /// <summary>
        /// The event data
        /// </summary>
        [JsonPropertyName("data")]
        public Dictionary<string, object>? Data { get; set; }
    }

    /// <summary>
    /// Represents Egnyte file versions
    /// </summary>
    public sealed class EgnyteFileVersion : EgnyteEntityBase
    {
        /// <summary>
        /// The version ID
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// The checksum of the version
        /// </summary>
        [JsonPropertyName("checksum")]
        public string? Checksum { get; set; }

        /// <summary>
        /// The size of the version
        /// </summary>
        [JsonPropertyName("size")]
        public string? Size { get; set; }

        /// <summary>
        /// The last modified date of the version
        /// </summary>
        [JsonPropertyName("last_modified")]
        public string? LastModified { get; set; }

        /// <summary>
        /// The uploaded date of the version
        /// </summary>
        [JsonPropertyName("uploaded")]
        public string? Uploaded { get; set; }

        /// <summary>
        /// The uploader username
        /// </summary>
        [JsonPropertyName("uploader")]
        public string? Uploader { get; set; }

        /// <summary>
        /// The version number
        /// </summary>
        [JsonPropertyName("version")]
        public int? Version { get; set; }

        /// <summary>
        /// The snapshot date
        /// </summary>
        [JsonPropertyName("snapshot_date")]
        public string? SnapshotDate { get; set; }
    }

    /// <summary>
    /// Represents Egnyte user info
    /// </summary>
    public sealed class EgnyteUserInfo : EgnyteEntityBase
    {
        /// <summary>
        /// The username
        /// </summary>
        [JsonPropertyName("username")]
        public string? Username { get; set; }

        /// <summary>
        /// The user ID
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// The email address
        /// </summary>
        [JsonPropertyName("email")]
        public string? Email { get; set; }

        /// <summary>
        /// The first name
        /// </summary>
        [JsonPropertyName("firstName")]
        public string? FirstName { get; set; }

        /// <summary>
        /// The last name
        /// </summary>
        [JsonPropertyName("lastName")]
        public string? LastName { get; set; }

        /// <summary>
        /// The user type
        /// </summary>
        [JsonPropertyName("userType")]
        public string? UserType { get; set; }

        /// <summary>
        /// Whether the user is active
        /// </summary>
        [JsonPropertyName("active")]
        public bool? Active { get; set; }

        /// <summary>
        /// The domain username
        /// </summary>
        [JsonPropertyName("domainUsername")]
        public string? DomainUsername { get; set; }

        /// <summary>
        /// The role
        /// </summary>
        [JsonPropertyName("role")]
        public string? Role { get; set; }

        /// <summary>
        /// The groups
        /// </summary>
        [JsonPropertyName("groups")]
        public List<string>? Groups { get; set; }
    }
}