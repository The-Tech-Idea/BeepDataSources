// File: Connectors/Dropbox/Models/DropboxModels.cs
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Dropbox.Models
{
    // -------------------------------------------------------
    // Base
    // -------------------------------------------------------
    public abstract class DropboxEntityBase
    {
        [JsonIgnore] public IDataSource? DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : DropboxEntityBase { DataSource = ds; return (T)this; }
    }

    // -------------------------------------------------------
    // File/Folder Metadata
    // -------------------------------------------------------
    public sealed class DropboxMetadata : DropboxEntityBase
    {
        [JsonPropertyName(".tag")] public string? Tag { get; set; } // "file" or "folder"
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("path_lower")] public string? PathLower { get; set; }
        [JsonPropertyName("path_display")] public string? PathDisplay { get; set; }
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("client_modified")] public DateTimeOffset? ClientModified { get; set; }
        [JsonPropertyName("server_modified")] public DateTimeOffset? ServerModified { get; set; }
        [JsonPropertyName("rev")] public string? Rev { get; set; }
        [JsonPropertyName("size")] public long? Size { get; set; }
        [JsonPropertyName("content_hash")] public string? ContentHash { get; set; }
    }

    // -------------------------------------------------------
    // File Metadata
    // -------------------------------------------------------
    public sealed class DropboxFileMetadata : DropboxEntityBase
    {
        [JsonPropertyName(".tag")] public string? Tag { get; set; } // "file"
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("path_lower")] public string? PathLower { get; set; }
        [JsonPropertyName("path_display")] public string? PathDisplay { get; set; }
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("client_modified")] public DateTimeOffset? ClientModified { get; set; }
        [JsonPropertyName("server_modified")] public DateTimeOffset? ServerModified { get; set; }
        [JsonPropertyName("rev")] public string? Rev { get; set; }
        [JsonPropertyName("size")] public long? Size { get; set; }
        [JsonPropertyName("content_hash")] public string? ContentHash { get; set; }
        [JsonPropertyName("is_downloadable")] public bool? IsDownloadable { get; set; }
        [JsonPropertyName("has_explicit_shared_members")] public bool? HasExplicitSharedMembers { get; set; }
        [JsonPropertyName("sharing_info")] public DropboxSharingInfo? SharingInfo { get; set; }
        [JsonPropertyName("property_groups")] public List<DropboxPropertyGroup>? PropertyGroups { get; set; } = new();
        [JsonPropertyName("has_thumbnail")] public bool? HasThumbnail { get; set; }
        [JsonPropertyName("thumbnail")] public string? Thumbnail { get; set; }
    }

    // -------------------------------------------------------
    // Folder Metadata
    // -------------------------------------------------------
    public sealed class DropboxFolderMetadata : DropboxEntityBase
    {
        [JsonPropertyName(".tag")] public string? Tag { get; set; } // "folder"
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("path_lower")] public string? PathLower { get; set; }
        [JsonPropertyName("path_display")] public string? PathDisplay { get; set; }
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("client_modified")] public DateTimeOffset? ClientModified { get; set; }
        [JsonPropertyName("server_modified")] public DateTimeOffset? ServerModified { get; set; }
        [JsonPropertyName("rev")] public string? Rev { get; set; }
        [JsonPropertyName("size")] public long? Size { get; set; }
        [JsonPropertyName("content_hash")] public string? ContentHash { get; set; }
        [JsonPropertyName("shared_folder_id")] public string? SharedFolderId { get; set; }
        [JsonPropertyName("sharing_info")] public DropboxSharingInfo? SharingInfo { get; set; }
        [JsonPropertyName("property_groups")] public List<DropboxPropertyGroup>? PropertyGroups { get; set; } = new();
    }

    // -------------------------------------------------------
    // Sharing Info
    // -------------------------------------------------------
    public sealed class DropboxSharingInfo : DropboxEntityBase
    {
        [JsonPropertyName("read_only")] public bool? ReadOnly { get; set; }
        [JsonPropertyName("parent_shared_folder_id")] public string? ParentSharedFolderId { get; set; }
        [JsonPropertyName("modified_by")] public string? ModifiedBy { get; set; }
        [JsonPropertyName("shared_folder_id")] public string? SharedFolderId { get; set; }
        [JsonPropertyName("traverse_only")] public bool? TraverseOnly { get; set; }
        [JsonPropertyName("no_access")] public bool? NoAccess { get; set; }
    }

    // -------------------------------------------------------
    // Property Group
    // -------------------------------------------------------
    public sealed class DropboxPropertyGroup : DropboxEntityBase
    {
        [JsonPropertyName("template_id")] public string? TemplateId { get; set; }
        [JsonPropertyName("fields")] public List<DropboxPropertyField>? Fields { get; set; } = new();
    }

    public sealed class DropboxPropertyField : DropboxEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("value")] public string? Value { get; set; }
    }

    // -------------------------------------------------------
    // Shared Link
    // -------------------------------------------------------
    public sealed class DropboxSharedLink : DropboxEntityBase
    {
        [JsonPropertyName(".tag")] public string? Tag { get; set; }
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("link_permissions")] public DropboxLinkPermissions? LinkPermissions { get; set; }
        [JsonPropertyName("client_modified")] public DateTimeOffset? ClientModified { get; set; }
        [JsonPropertyName("server_modified")] public DateTimeOffset? ServerModified { get; set; }
        [JsonPropertyName("rev")] public string? Rev { get; set; }
        [JsonPropertyName("size")] public long? Size { get; set; }
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("expires")] public DateTimeOffset? Expires { get; set; }
        [JsonPropertyName("path_lower")] public string? PathLower { get; set; }
        [JsonPropertyName("team_member_info")] public DropboxTeamMemberInfo? TeamMemberInfo { get; set; }
        [JsonPropertyName("content_owner_team_info")] public DropboxTeamInfo? ContentOwnerTeamInfo { get; set; }
    }

    public sealed class DropboxLinkPermissions : DropboxEntityBase
    {
        [JsonPropertyName("can_revoke")] public bool? CanRevoke { get; set; }
        [JsonPropertyName("revoke_failure_reason")] public DropboxRevokeFailureReason? RevokeFailureReason { get; set; }
        [JsonPropertyName("visibility")] public DropboxVisibility? Visibility { get; set; }
        [JsonPropertyName("password_protected")] public bool? PasswordProtected { get; set; }
        [JsonPropertyName("link_access_level")] public DropboxLinkAccessLevel? LinkAccessLevel { get; set; }
        [JsonPropertyName("effective_audience")] public DropboxEffectiveAudience? EffectiveAudience { get; set; }
        [JsonPropertyName("effective_expiry")] public DateTimeOffset? EffectiveExpiry { get; set; }
        [JsonPropertyName("allow_download")] public bool? AllowDownload { get; set; }
        [JsonPropertyName("require_password")] public bool? RequirePassword { get; set; }
        [JsonPropertyName("allow_comments")] public bool? AllowComments { get; set; }
    }

    // -------------------------------------------------------
    // Account Info
    // -------------------------------------------------------
    public sealed class DropboxAccountInfo : DropboxEntityBase
    {
        [JsonPropertyName("account_id")] public string? AccountId { get; set; }
        [JsonPropertyName("name")] public DropboxName? Name { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("email_verified")] public bool? EmailVerified { get; set; }
        [JsonPropertyName("profile_photo_url")] public string? ProfilePhotoUrl { get; set; }
        [JsonPropertyName("disabled")] public bool? Disabled { get; set; }
        [JsonPropertyName("country")] public string? Country { get; set; }
        [JsonPropertyName("locale")] public string? Locale { get; set; }
        [JsonPropertyName("referral_link")] public string? ReferralLink { get; set; }
        [JsonPropertyName("is_paired")] public bool? IsPaired { get; set; }
        [JsonPropertyName("account_type")] public DropboxAccountType? AccountType { get; set; }
        [JsonPropertyName("root_info")] public DropboxRootInfo? RootInfo { get; set; }
    }

    public sealed class DropboxName : DropboxEntityBase
    {
        [JsonPropertyName("given_name")] public string? GivenName { get; set; }
        [JsonPropertyName("surname")] public string? Surname { get; set; }
        [JsonPropertyName("familiar_name")] public string? FamiliarName { get; set; }
        [JsonPropertyName("display_name")] public string? Caption { get; set; }
        [JsonPropertyName("abbreviated_name")] public string? AbbreviatedName { get; set; }
    }

    // -------------------------------------------------------
    // Space Usage
    // -------------------------------------------------------
    public sealed class DropboxSpaceUsage : DropboxEntityBase
    {
        [JsonPropertyName("used")] public long? Used { get; set; }
        [JsonPropertyName("allocation")] public DropboxSpaceAllocation? Allocation { get; set; }
    }

    public sealed class DropboxSpaceAllocation : DropboxEntityBase
    {
        [JsonPropertyName(".tag")] public string? Tag { get; set; }
        [JsonPropertyName("allocated")] public long? Allocated { get; set; }
    }

    // -------------------------------------------------------
    // Team Info
    // -------------------------------------------------------
    public sealed class DropboxTeamInfo : DropboxEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("team_id")] public string? TeamId { get; set; }
    }

    public sealed class DropboxTeamMemberInfo : DropboxEntityBase
    {
        [JsonPropertyName("team_info")] public DropboxTeamInfo? TeamInfo { get; set; }
        [JsonPropertyName("display_name")] public string? Caption { get; set; }
        [JsonPropertyName("member_id")] public string? MemberId { get; set; }
    }

    // -------------------------------------------------------
    // Enums and Supporting Types
    // -------------------------------------------------------
    public enum DropboxVisibility
    {
        Public,
        TeamOnly,
        Password,
        TeamAndPassword,
        SharedFolderOnly
    }

    public enum DropboxLinkAccessLevel
    {
        Viewer,
        Editor,
        Max,
        Default
    }

    public enum DropboxEffectiveAudience
    {
        Public,
        Team,
        NoOne
    }

    public enum DropboxAccountType
    {
        Basic,
        Pro,
        Business
    }

    public sealed class DropboxRootInfo : DropboxEntityBase
    {
        [JsonPropertyName(".tag")] public string? Tag { get; set; }
        [JsonPropertyName("root_namespace_id")] public string? RootNamespaceId { get; set; }
        [JsonPropertyName("home_namespace_id")] public string? HomeNamespaceId { get; set; }
    }

    public sealed class DropboxRevokeFailureReason : DropboxEntityBase
    {
        [JsonPropertyName(".tag")] public string? Tag { get; set; }
    }

    // -------------------------------------------------------
    // List Folder Response
    // -------------------------------------------------------
    public sealed class DropboxListFolderResponse : DropboxEntityBase
    {
        [JsonPropertyName("entries")] public List<DropboxMetadata>? Entries { get; set; } = new();
        [JsonPropertyName("cursor")] public string? Cursor { get; set; }
        [JsonPropertyName("has_more")] public bool? HasMore { get; set; }
    }

    // -------------------------------------------------------
    // Team Member
    // -------------------------------------------------------
    public sealed class DropboxTeamMember : DropboxEntityBase
    {
        [JsonPropertyName("profile")] public DropboxAccountInfo? Profile { get; set; }
        [JsonPropertyName("role")] public DropboxTeamMemberRole? Role { get; set; }
    }

    public sealed class DropboxTeamMemberRole : DropboxEntityBase
    {
        [JsonPropertyName(".tag")] public string? Tag { get; set; }
    }
}