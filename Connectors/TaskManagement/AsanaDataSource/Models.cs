using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TheTechIdea.Beep.Connectors.TaskManagement.Asana.Models
{
    /// <summary>
    /// Asana API response wrapper
    /// </summary>
    public class AsanaApiResponse<T>
    {
        [JsonPropertyName("data")]
        public T Data { get; set; }

        [JsonPropertyName("next_page")]
        public AsanaPagination NextPage { get; set; }
    }

    /// <summary>
    /// Asana API response wrapper for collections
    /// </summary>
    public class AsanaCollectionResponse<T>
    {
        [JsonPropertyName("data")]
        public List<T> Data { get; set; }

        [JsonPropertyName("next_page")]
        public AsanaPagination NextPage { get; set; }
    }

    /// <summary>
    /// Pagination information for Asana API
    /// </summary>
    public class AsanaPagination
    {
        [JsonPropertyName("offset")]
        public string Offset { get; set; }

        [JsonPropertyName("path")]
        public string Path { get; set; }

        [JsonPropertyName("uri")]
        public string Uri { get; set; }
    }

    /// <summary>
    /// Asana Workspace entity
    /// </summary>
    public class AsanaWorkspace
    {
        [JsonPropertyName("gid")]
        public string Gid { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("resource_type")]
        public string ResourceType { get; set; }

        [JsonPropertyName("is_organization")]
        public bool IsOrganization { get; set; }
    }

    /// <summary>
    /// Asana Project entity
    /// </summary>
    public class AsanaProject
    {
        [JsonPropertyName("gid")]
        public string Gid { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("resource_type")]
        public string ResourceType { get; set; }

        [JsonPropertyName("archived")]
        public bool Archived { get; set; }

        [JsonPropertyName("color")]
        public string Color { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("current_status")]
        public AsanaProjectStatus CurrentStatus { get; set; }

        [JsonPropertyName("default_view")]
        public string DefaultView { get; set; }

        [JsonPropertyName("due_date")]
        public DateTime? DueDate { get; set; }

        [JsonPropertyName("due_on")]
        public DateTime? DueOn { get; set; }

        [JsonPropertyName("html_notes")]
        public string HtmlNotes { get; set; }

        [JsonPropertyName("members")]
        public List<AsanaUser> Members { get; set; }

        [JsonPropertyName("modified_at")]
        public DateTime? ModifiedAt { get; set; }

        [JsonPropertyName("notes")]
        public string Notes { get; set; }

        [JsonPropertyName("owner")]
        public AsanaUser Owner { get; set; }

        [JsonPropertyName("public")]
        public bool Public { get; set; }

        [JsonPropertyName("start_on")]
        public DateTime? StartOn { get; set; }

        [JsonPropertyName("workspace")]
        public AsanaWorkspace Workspace { get; set; }

        [JsonPropertyName("team")]
        public AsanaTeam Team { get; set; }
    }

    /// <summary>
    /// Asana Project Status
    /// </summary>
    public class AsanaProjectStatus
    {
        [JsonPropertyName("color")]
        public string Color { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("html_text")]
        public string HtmlText { get; set; }
    }

    /// <summary>
    /// Asana Task entity
    /// </summary>
    public class AsanaTask
    {
        [JsonPropertyName("gid")]
        public string Gid { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("resource_type")]
        public string ResourceType { get; set; }

        [JsonPropertyName("approval_status")]
        public string ApprovalStatus { get; set; }

        [JsonPropertyName("assignee")]
        public AsanaUser Assignee { get; set; }

        [JsonPropertyName("assignee_status")]
        public string AssigneeStatus { get; set; }

        [JsonPropertyName("completed")]
        public bool Completed { get; set; }

        [JsonPropertyName("completed_at")]
        public DateTime? CompletedAt { get; set; }

        [JsonPropertyName("completed_by")]
        public AsanaUser CompletedBy { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("dependencies")]
        public List<AsanaTask> Dependencies { get; set; }

        [JsonPropertyName("dependents")]
        public List<AsanaTask> Dependents { get; set; }

        [JsonPropertyName("due_at")]
        public DateTime? DueAt { get; set; }

        [JsonPropertyName("due_on")]
        public DateTime? DueOn { get; set; }

        [JsonPropertyName("external")]
        public AsanaExternalData External { get; set; }

        [JsonPropertyName("hearted")]
        public bool Hearted { get; set; }

        [JsonPropertyName("hearts")]
        public List<AsanaUser> Hearts { get; set; }

        [JsonPropertyName("html_notes")]
        public string HtmlNotes { get; set; }

        [JsonPropertyName("is_rendered_as_separator")]
        public bool IsRenderedAsSeparator { get; set; }

        [JsonPropertyName("liked")]
        public bool Liked { get; set; }

        [JsonPropertyName("likes")]
        public List<AsanaUser> Likes { get; set; }

        [JsonPropertyName("memberships")]
        public List<AsanaTaskMembership> Memberships { get; set; }

        [JsonPropertyName("modified_at")]
        public DateTime? ModifiedAt { get; set; }

        [JsonPropertyName("notes")]
        public string Notes { get; set; }

        [JsonPropertyName("num_hearts")]
        public int NumHearts { get; set; }

        [JsonPropertyName("num_likes")]
        public int NumLikes { get; set; }

        [JsonPropertyName("num_subtasks")]
        public int NumSubtasks { get; set; }

        [JsonPropertyName("parent")]
        public AsanaTask Parent { get; set; }

        [JsonPropertyName("permalink_url")]
        public string PermalinkUrl { get; set; }

        [JsonPropertyName("projects")]
        public List<AsanaProject> Projects { get; set; }

        [JsonPropertyName("resource_subtype")]
        public string ResourceSubtype { get; set; }

        [JsonPropertyName("start_at")]
        public DateTime? StartAt { get; set; }

        [JsonPropertyName("start_on")]
        public DateTime? StartOn { get; set; }

        [JsonPropertyName("tags")]
        public List<AsanaTag> Tags { get; set; }

        [JsonPropertyName("workspace")]
        public AsanaWorkspace Workspace { get; set; }
    }

    /// <summary>
    /// Asana Task Membership
    /// </summary>
    public class AsanaTaskMembership
    {
        [JsonPropertyName("project")]
        public AsanaProject Project { get; set; }

        [JsonPropertyName("section")]
        public AsanaSection Section { get; set; }
    }

    /// <summary>
    /// Asana User entity
    /// </summary>
    public class AsanaUser
    {
        [JsonPropertyName("gid")]
        public string Gid { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("resource_type")]
        public string ResourceType { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("photo")]
        public AsanaUserPhoto Photo { get; set; }

        [JsonPropertyName("workspaces")]
        public List<AsanaWorkspace> Workspaces { get; set; }
    }

    /// <summary>
    /// Asana User Photo
    /// </summary>
    public class AsanaUserPhoto
    {
        [JsonPropertyName("image_21x21")]
        public string Image21x21 { get; set; }

        [JsonPropertyName("image_27x27")]
        public string Image27x27 { get; set; }

        [JsonPropertyName("image_36x36")]
        public string Image36x36 { get; set; }

        [JsonPropertyName("image_60x60")]
        public string Image60x60 { get; set; }

        [JsonPropertyName("image_128x128")]
        public string Image128x128 { get; set; }
    }

    /// <summary>
    /// Asana Team entity
    /// </summary>
    public class AsanaTeam
    {
        [JsonPropertyName("gid")]
        public string Gid { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("resource_type")]
        public string ResourceType { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("html_description")]
        public string HtmlDescription { get; set; }

        [JsonPropertyName("organization")]
        public AsanaWorkspace Organization { get; set; }

        [JsonPropertyName("permalink_url")]
        public string PermalinkUrl { get; set; }
    }

    /// <summary>
    /// Asana Section entity
    /// </summary>
    public class AsanaSection
    {
        [JsonPropertyName("gid")]
        public string Gid { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("resource_type")]
        public string ResourceType { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("project")]
        public AsanaProject Project { get; set; }
    }

    /// <summary>
    /// Asana Tag entity
    /// </summary>
    public class AsanaTag
    {
        [JsonPropertyName("gid")]
        public string Gid { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("resource_type")]
        public string ResourceType { get; set; }

        [JsonPropertyName("color")]
        public string Color { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("followers")]
        public List<AsanaUser> Followers { get; set; }

        [JsonPropertyName("notes")]
        public string Notes { get; set; }

        [JsonPropertyName("permalink_url")]
        public string PermalinkUrl { get; set; }

        [JsonPropertyName("workspace")]
        public AsanaWorkspace Workspace { get; set; }
    }

    /// <summary>
    /// Asana Story (Comment/Activity) entity
    /// </summary>
    public class AsanaStory
    {
        [JsonPropertyName("gid")]
        public string Gid { get; set; }

        [JsonPropertyName("resource_type")]
        public string ResourceType { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("created_by")]
        public AsanaUser CreatedBy { get; set; }

        [JsonPropertyName("resource_subtype")]
        public string ResourceSubtype { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("html_text")]
        public string HtmlText { get; set; }

        [JsonPropertyName("is_pinned")]
        public bool IsPinned { get; set; }

        [JsonPropertyName("assignee")]
        public AsanaUser Assignee { get; set; }

        [JsonPropertyName("dependency")]
        public AsanaTask Dependency { get; set; }

        [JsonPropertyName("duplicate_of")]
        public AsanaTask DuplicateOf { get; set; }

        [JsonPropertyName("duplicated_from")]
        public AsanaTask DuplicatedFrom { get; set; }

        [JsonPropertyName("follower")]
        public AsanaUser Follower { get; set; }

        [JsonPropertyName("hearted")]
        public bool Hearted { get; set; }

        [JsonPropertyName("hearts")]
        public List<AsanaUser> Hearts { get; set; }

        [JsonPropertyName("is_editable")]
        public bool IsEditable { get; set; }

        [JsonPropertyName("is_edited")]
        public bool IsEdited { get; set; }

        [JsonPropertyName("liked")]
        public bool Liked { get; set; }

        [JsonPropertyName("likes")]
        public List<AsanaUser> Likes { get; set; }

        [JsonPropertyName("new_approval_status")]
        public string NewApprovalStatus { get; set; }

        [JsonPropertyName("new_dates")]
        public AsanaTaskDates NewDates { get; set; }

        [JsonPropertyName("new_enum_value")]
        public AsanaEnumValue NewEnumValue { get; set; }

        [JsonPropertyName("new_name")]
        public string NewName { get; set; }

        [JsonPropertyName("new_number_value")]
        public AsanaNumberValue NewNumberValue { get; set; }

        [JsonPropertyName("new_resource_subtype")]
        public string NewResourceSubtype { get; set; }

        [JsonPropertyName("new_section")]
        public AsanaSection NewSection { get; set; }

        [JsonPropertyName("new_text_value")]
        public string NewTextValue { get; set; }

        [JsonPropertyName("num_hearts")]
        public int NumHearts { get; set; }

        [JsonPropertyName("num_likes")]
        public int NumLikes { get; set; }

        [JsonPropertyName("old_approval_status")]
        public string OldApprovalStatus { get; set; }

        [JsonPropertyName("old_dates")]
        public AsanaTaskDates OldDates { get; set; }

        [JsonPropertyName("old_enum_value")]
        public AsanaEnumValue OldEnumValue { get; set; }

        [JsonPropertyName("old_name")]
        public string OldName { get; set; }

        [JsonPropertyName("old_number_value")]
        public AsanaNumberValue OldNumberValue { get; set; }

        [JsonPropertyName("old_resource_subtype")]
        public string OldResourceSubtype { get; set; }

        [JsonPropertyName("old_section")]
        public AsanaSection OldSection { get; set; }

        [JsonPropertyName("old_text_value")]
        public string OldTextValue { get; set; }

        [JsonPropertyName("source")]
        public string Source { get; set; }

        [JsonPropertyName("sticker_name")]
        public string StickerName { get; set; }

        [JsonPropertyName("target")]
        public AsanaTask Target { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }
    }

    /// <summary>
    /// Asana Task Dates
    /// </summary>
    public class AsanaTaskDates
    {
        [JsonPropertyName("due_at")]
        public DateTime? DueAt { get; set; }

        [JsonPropertyName("due_on")]
        public DateTime? DueOn { get; set; }

        [JsonPropertyName("start_on")]
        public DateTime? StartOn { get; set; }
    }

    /// <summary>
    /// Asana Enum Value
    /// </summary>
    public class AsanaEnumValue
    {
        [JsonPropertyName("gid")]
        public string Gid { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("resource_type")]
        public string ResourceType { get; set; }

        [JsonPropertyName("color")]
        public string Color { get; set; }

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }
    }

    /// <summary>
    /// Asana Number Value
    /// </summary>
    public class AsanaNumberValue
    {
        [JsonPropertyName("number")]
        public decimal Number { get; set; }
    }

    /// <summary>
    /// Asana External Data
    /// </summary>
    public class AsanaExternalData
    {
        [JsonPropertyName("gid")]
        public string Gid { get; set; }

        [JsonPropertyName("data")]
        public string Data { get; set; }
    }

    /// <summary>
    /// Asana Webhook entity
    /// </summary>
    public class AsanaWebhook
    {
        [JsonPropertyName("gid")]
        public string Gid { get; set; }

        [JsonPropertyName("resource_type")]
        public string ResourceType { get; set; }

        [JsonPropertyName("active")]
        public bool Active { get; set; }

        [JsonPropertyName("resource")]
        public AsanaWebhookResource Resource { get; set; }

        [JsonPropertyName("target")]
        public string Target { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("last_failure_at")]
        public DateTime? LastFailureAt { get; set; }

        [JsonPropertyName("last_failure_content")]
        public string LastFailureContent { get; set; }

        [JsonPropertyName("last_success_at")]
        public DateTime? LastSuccessAt { get; set; }

        [JsonPropertyName("filters")]
        public List<AsanaWebhookFilter> Filters { get; set; }
    }

    /// <summary>
    /// Asana Webhook Resource
    /// </summary>
    public class AsanaWebhookResource
    {
        [JsonPropertyName("gid")]
        public string Gid { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("resource_type")]
        public string ResourceType { get; set; }
    }

    /// <summary>
    /// Asana Webhook Filter
    /// </summary>
    public class AsanaWebhookFilter
    {
        [JsonPropertyName("resource_type")]
        public string ResourceType { get; set; }

        [JsonPropertyName("resource_subtype")]
        public string ResourceSubtype { get; set; }

        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("fields")]
        public List<string> Fields { get; set; }
    }
}