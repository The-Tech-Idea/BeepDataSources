// File: Connectors/CRM/PipedriveDataSource/Models.cs
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Pipedrive.Models
{
    // Common base for Pipedrive objects
    public abstract class PipedriveEntityBase
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("add_time")] public DateTime? AddTime { get; set; }
        [JsonPropertyName("update_time")] public DateTime? UpdateTime { get; set; }
        [JsonPropertyName("active_flag")] public bool? ActiveFlag { get; set; }

        // Optional: Let POCOs know their datasource after hydration
        [JsonIgnore] public IDataSource DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : PipedriveEntityBase { DataSource = ds; return (T)this; }
    }

    // ---- Core CRM Objects ----

    public sealed class Deal : PipedriveEntityBase
    {
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("value")] public decimal? Value { get; set; }
        [JsonPropertyName("currency")] public string Currency { get; set; }
        [JsonPropertyName("stage_id")] public int? StageId { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; }
        [JsonPropertyName("probability")] public int? Probability { get; set; }
        [JsonPropertyName("lost_reason")] public string LostReason { get; set; }
        [JsonPropertyName("visible_to")] public string VisibleTo { get; set; }
        [JsonPropertyName("close_time")] public DateTime? CloseTime { get; set; }
        [JsonPropertyName("pipeline_id")] public int? PipelineId { get; set; }
        [JsonPropertyName("won_time")] public DateTime? WonTime { get; set; }
        [JsonPropertyName("first_won_time")] public DateTime? FirstWonTime { get; set; }
        [JsonPropertyName("lost_time")] public DateTime? LostTime { get; set; }
        [JsonPropertyName("products_count")] public int? ProductsCount { get; set; }
        [JsonPropertyName("files_count")] public int? FilesCount { get; set; }
        [JsonPropertyName("notes_count")] public int? NotesCount { get; set; }
        [JsonPropertyName("followers_count")] public int? FollowersCount { get; set; }
        [JsonPropertyName("email_messages_count")] public int? EmailMessagesCount { get; set; }
        [JsonPropertyName("activities_count")] public int? ActivitiesCount { get; set; }
        [JsonPropertyName("done_activities_count")] public int? DoneActivitiesCount { get; set; }
        [JsonPropertyName("undone_activities_count")] public int? UndoneActivitiesCount { get; set; }
        [JsonPropertyName("participants_count")] public int? ParticipantsCount { get; set; }
        [JsonPropertyName("expected_close_date")] public DateTime? ExpectedCloseDate { get; set; }
        [JsonPropertyName("last_incoming_mail_time")] public DateTime? LastIncomingMailTime { get; set; }
        [JsonPropertyName("last_outgoing_mail_time")] public DateTime? LastOutgoingMailTime { get; set; }
        [JsonPropertyName("org_id")] public PipedriveReference OrgId { get; set; }
        [JsonPropertyName("person_id")] public PipedriveReference PersonId { get; set; }
        [JsonPropertyName("user_id")] public PipedriveReference UserId { get; set; }
        [JsonPropertyName("creator_user_id")] public PipedriveReference CreatorUserId { get; set; }
    }

    public sealed class Person : PipedriveEntityBase
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("first_name")] public string FirstName { get; set; }
        [JsonPropertyName("last_name")] public string LastName { get; set; }
        [JsonPropertyName("email")] public List<PipedriveEmail> Email { get; set; } = new();
        [JsonPropertyName("phone")] public List<PipedrivePhone> Phone { get; set; } = new();
        [JsonPropertyName("visible_to")] public string VisibleTo { get; set; }
        [JsonPropertyName("org_id")] public PipedriveReference OrgId { get; set; }
        [JsonPropertyName("owner_id")] public PipedriveReference OwnerId { get; set; }
        [JsonPropertyName("label")] public int? Label { get; set; }
        [JsonPropertyName("open_deals_count")] public int? OpenDealsCount { get; set; }
        [JsonPropertyName("related_open_deals_count")] public int? RelatedOpenDealsCount { get; set; }
        [JsonPropertyName("closed_deals_count")] public int? ClosedDealsCount { get; set; }
        [JsonPropertyName("related_closed_deals_count")] public int? RelatedClosedDealsCount { get; set; }
        [JsonPropertyName("participant_open_deals_count")] public int? ParticipantOpenDealsCount { get; set; }
        [JsonPropertyName("participant_closed_deals_count")] public int? ParticipantClosedDealsCount { get; set; }
        [JsonPropertyName("email_messages_count")] public int? EmailMessagesCount { get; set; }
        [JsonPropertyName("activities_count")] public int? ActivitiesCount { get; set; }
        [JsonPropertyName("done_activities_count")] public int? DoneActivitiesCount { get; set; }
        [JsonPropertyName("undone_activities_count")] public int? UndoneActivitiesCount { get; set; }
        [JsonPropertyName("files_count")] public int? FilesCount { get; set; }
        [JsonPropertyName("notes_count")] public int? NotesCount { get; set; }
        [JsonPropertyName("followers_count")] public int? FollowersCount { get; set; }
        [JsonPropertyName("last_incoming_mail_time")] public DateTime? LastIncomingMailTime { get; set; }
        [JsonPropertyName("last_outgoing_mail_time")] public DateTime? LastOutgoingMailTime { get; set; }
    }

    public sealed class Organization : PipedriveEntityBase
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("address")] public string Address { get; set; }
        [JsonPropertyName("address_subpremise")] public string AddressSubpremise { get; set; }
        [JsonPropertyName("address_street_number")] public string AddressStreetNumber { get; set; }
        [JsonPropertyName("address_route")] public string AddressRoute { get; set; }
        [JsonPropertyName("address_sublocality")] public string AddressSublocality { get; set; }
        [JsonPropertyName("address_locality")] public string AddressLocality { get; set; }
        [JsonPropertyName("address_admin_area_level_1")] public string AddressAdminAreaLevel1 { get; set; }
        [JsonPropertyName("address_admin_area_level_2")] public string AddressAdminAreaLevel2 { get; set; }
        [JsonPropertyName("address_country")] public string AddressCountry { get; set; }
        [JsonPropertyName("address_postal_code")] public string AddressPostalCode { get; set; }
        [JsonPropertyName("address_formatted_address")] public string AddressFormattedAddress { get; set; }
        [JsonPropertyName("visible_to")] public string VisibleTo { get; set; }
        [JsonPropertyName("owner_id")] public PipedriveReference OwnerId { get; set; }
        [JsonPropertyName("label")] public int? Label { get; set; }
        [JsonPropertyName("open_deals_count")] public int? OpenDealsCount { get; set; }
        [JsonPropertyName("related_open_deals_count")] public int? RelatedOpenDealsCount { get; set; }
        [JsonPropertyName("closed_deals_count")] public int? ClosedDealsCount { get; set; }
        [JsonPropertyName("related_closed_deals_count")] public int? RelatedClosedDealsCount { get; set; }
        [JsonPropertyName("email_messages_count")] public int? EmailMessagesCount { get; set; }
        [JsonPropertyName("people_count")] public int? PeopleCount { get; set; }
        [JsonPropertyName("activities_count")] public int? ActivitiesCount { get; set; }
        [JsonPropertyName("done_activities_count")] public int? DoneActivitiesCount { get; set; }
        [JsonPropertyName("undone_activities_count")] public int? UndoneActivitiesCount { get; set; }
        [JsonPropertyName("files_count")] public int? FilesCount { get; set; }
        [JsonPropertyName("notes_count")] public int? NotesCount { get; set; }
        [JsonPropertyName("followers_count")] public int? FollowersCount { get; set; }
    }

    public sealed class Activity : PipedriveEntityBase
    {
        [JsonPropertyName("subject")] public string Subject { get; set; }
        [JsonPropertyName("done")] public bool? Done { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("due_date")] public DateTime? DueDate { get; set; }
        [JsonPropertyName("due_time")] public string DueTime { get; set; }
        [JsonPropertyName("duration")] public string Duration { get; set; }
        [JsonPropertyName("busy_flag")] public bool? BusyFlag { get; set; }
        [JsonPropertyName("deal_id")] public int? DealId { get; set; }
        [JsonPropertyName("person_id")] public int? PersonId { get; set; }
        [JsonPropertyName("org_id")] public int? OrgId { get; set; }
        [JsonPropertyName("user_id")] public int? UserId { get; set; }
        [JsonPropertyName("assigned_to_user_id")] public int? AssignedToUserId { get; set; }
        [JsonPropertyName("created_by_user_id")] public int? CreatedByUserId { get; set; }
        [JsonPropertyName("location")] public string Location { get; set; }
        [JsonPropertyName("org_name")] public string OrgName { get; set; }
        [JsonPropertyName("person_name")] public string PersonName { get; set; }
        [JsonPropertyName("deal_title")] public string DealTitle { get; set; }
        [JsonPropertyName("owner_name")] public string OwnerName { get; set; }
        [JsonPropertyName("person_dropbox_bcc")] public string PersonDropboxBcc { get; set; }
        [JsonPropertyName("deal_dropbox_bcc")] public string DealDropboxBcc { get; set; }
        [JsonPropertyName("assigned_to_user_email")] public string AssignedToUserEmail { get; set; }
        [JsonPropertyName("note")] public string Note { get; set; }
        [JsonPropertyName("marked_as_done_time")] public DateTime? MarkedAsDoneTime { get; set; }
    }

    public sealed class User : PipedriveEntityBase
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("default_currency")] public string DefaultCurrency { get; set; }
        [JsonPropertyName("locale")] public string Locale { get; set; }
        [JsonPropertyName("lang")] public int? Lang { get; set; }
        [JsonPropertyName("email")] public string Email { get; set; }
        [JsonPropertyName("phone")] public string Phone { get; set; }
        [JsonPropertyName("activated")] public bool? Activated { get; set; }
        [JsonPropertyName("last_login")] public DateTime? LastLogin { get; set; }
        [JsonPropertyName("created")] public DateTime? Created { get; set; }
        [JsonPropertyName("modified")] public DateTime? Modified { get; set; }
        [JsonPropertyName("has_created_company")] public bool? HasCreatedCompany { get; set; }
        [JsonPropertyName("access")] public List<PipedriveUserAccess> Access { get; set; } = new();
        [JsonPropertyName("permissions")] public PipedriveUserPermissions Permissions { get; set; }
        [JsonPropertyName("icon_url")] public string IconUrl { get; set; }
        [JsonPropertyName("is_you")] public bool? IsYou { get; set; }
        [JsonPropertyName("timezone_name")] public string TimezoneName { get; set; }
        [JsonPropertyName("timezone_offset")] public string TimezoneOffset { get; set; }
        [JsonPropertyName("role_id")] public int? RoleId { get; set; }
    }

    public sealed class Pipeline : PipedriveEntityBase
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("url_title")] public string UrlTitle { get; set; }
        [JsonPropertyName("order_nr")] public int? OrderNr { get; set; }
        [JsonPropertyName("selected")] public bool? Selected { get; set; }
        [JsonPropertyName("deals_summary")] public PipelineDealsSummary DealsSummary { get; set; }
    }

    public sealed class Stage : PipedriveEntityBase
    {
        [JsonPropertyName("order_nr")] public int? OrderNr { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("pipeline_id")] public int? PipelineId { get; set; }
        [JsonPropertyName("deal_probability")] public int? DealProbability { get; set; }
        [JsonPropertyName("rotten_flag")] public bool? RottenFlag { get; set; }
        [JsonPropertyName("rotten_days")] public int? RottenDays { get; set; }
        [JsonPropertyName("pipeline_name")] public string PipelineName { get; set; }
        [JsonPropertyName("pipeline_deal_probability")] public bool? PipelineDealProbability { get; set; }
    }

    public sealed class Product : PipedriveEntityBase
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("code")] public string Code { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("unit")] public string Unit { get; set; }
        [JsonPropertyName("tax")] public decimal? Tax { get; set; }
        [JsonPropertyName("category")] public string Category { get; set; }
        [JsonPropertyName("visible_to")] public string VisibleTo { get; set; }
        [JsonPropertyName("owner_id")] public PipedriveReference OwnerId { get; set; }
        [JsonPropertyName("files_count")] public int? FilesCount { get; set; }
        [JsonPropertyName("followers_count")] public int? FollowersCount { get; set; }
        [JsonPropertyName("prices")] public List<PipedriveProductPrice> Prices { get; set; } = new();
    }

    // ---- Supporting Classes ----

    public class PipedriveReference
    {
        [JsonPropertyName("value")] public int? Value { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("email")] public string Email { get; set; }
        [JsonPropertyName("has_pic")] public bool? HasPic { get; set; }
        [JsonPropertyName("pic_hash")] public string PicHash { get; set; }
    }

    public class PipedriveEmail
    {
        [JsonPropertyName("value")] public string Value { get; set; }
        [JsonPropertyName("primary")] public bool Primary { get; set; }
        [JsonPropertyName("label")] public string Label { get; set; }
    }

    public class PipedrivePhone
    {
        [JsonPropertyName("value")] public string Value { get; set; }
        [JsonPropertyName("primary")] public bool Primary { get; set; }
        [JsonPropertyName("label")] public string Label { get; set; }
    }

    public class PipedriveUserAccess
    {
        [JsonPropertyName("app")] public string App { get; set; }
        [JsonPropertyName("admin")] public bool Admin { get; set; }
        [JsonPropertyName("permission_set_id")] public string PermissionSetId { get; set; }
    }

    public class PipedriveUserPermissions
    {
        [JsonPropertyName("can_add_products")] public bool CanAddProducts { get; set; }
        [JsonPropertyName("can_add_custom_fields")] public bool CanAddCustomFields { get; set; }
        [JsonPropertyName("can_edit_custom_fields")] public bool CanEditCustomFields { get; set; }
        [JsonPropertyName("can_edit_deals_closed_date")] public bool CanEditDealsClosedDate { get; set; }
        [JsonPropertyName("can_edit_products")] public bool CanEditProducts { get; set; }
        [JsonPropertyName("can_export_data_from_lists")] public bool CanExportDataFromLists { get; set; }
        [JsonPropertyName("can_follow_other_users")] public bool CanFollowOtherUsers { get; set; }
        [JsonPropertyName("can_merge_deals")] public bool CanMergeDeals { get; set; }
        [JsonPropertyName("can_merge_organizations")] public bool CanMergeOrganizations { get; set; }
        [JsonPropertyName("can_merge_people")] public bool CanMergePeople { get; set; }
        [JsonPropertyName("can_modify_labels")] public bool CanModifyLabels { get; set; }
        [JsonPropertyName("can_see_company_wide_statistics")] public bool CanSeeCompanyWideStatistics { get; set; }
        [JsonPropertyName("can_see_deals_list_summary")] public bool CanSeeDealsListSummary { get; set; }
        [JsonPropertyName("can_see_hidden_items_names")] public bool CanSeeHiddenItemsNames { get; set; }
        [JsonPropertyName("can_see_other_users")] public bool CanSeeOtherUsers { get; set; }
        [JsonPropertyName("can_see_other_users_statistics")] public bool CanSeeOtherUsersStatistics { get; set; }
    }

    public class PipelineDealsSummary
    {
        [JsonPropertyName("total_count")] public int TotalCount { get; set; }
        [JsonPropertyName("total_currency_converted_value")] public decimal TotalCurrencyConvertedValue { get; set; }
        [JsonPropertyName("total_weighted_currency_converted_value")] public decimal TotalWeightedCurrencyConvertedValue { get; set; }
        [JsonPropertyName("total_currency_converted_value_formatted")] public string TotalCurrencyConvertedValueFormatted { get; set; }
        [JsonPropertyName("total_weighted_currency_converted_value_formatted")] public string TotalWeightedCurrencyConvertedValueFormatted { get; set; }
        [JsonPropertyName("per_stages")] public List<PipelineStageDeals> PerStages { get; set; } = new();
        [JsonPropertyName("per_currency")] public Dictionary<string, PipelineCurrencyDeals> PerCurrency { get; set; } = new();
    }

    public class PipelineStageDeals
    {
        [JsonPropertyName("stage_id")] public int StageId { get; set; }
        [JsonPropertyName("count")] public int Count { get; set; }
        [JsonPropertyName("currency_converted_value")] public decimal CurrencyConvertedValue { get; set; }
        [JsonPropertyName("weighted_currency_converted_value")] public decimal WeightedCurrencyConvertedValue { get; set; }
        [JsonPropertyName("currency_converted_value_formatted")] public string CurrencyConvertedValueFormatted { get; set; }
        [JsonPropertyName("weighted_currency_converted_value_formatted")] public string WeightedCurrencyConvertedValueFormatted { get; set; }
    }

    public class PipelineCurrencyDeals
    {
        [JsonPropertyName("count")] public int Count { get; set; }
        [JsonPropertyName("value")] public decimal Value { get; set; }
        [JsonPropertyName("value_formatted")] public string ValueFormatted { get; set; }
        [JsonPropertyName("weighted_value")] public decimal WeightedValue { get; set; }
        [JsonPropertyName("weighted_value_formatted")] public string WeightedValueFormatted { get; set; }
    }

    public class PipedriveProductPrice
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("currency")] public string Currency { get; set; }
        [JsonPropertyName("price")] public decimal? Price { get; set; }
        [JsonPropertyName("cost")] public decimal? Cost { get; set; }
        [JsonPropertyName("overhead_cost")] public decimal? OverheadCost { get; set; }
    }

    // Response wrapper for Pipedrive API responses
    public class PipedriveResponse<T>
    {
        [JsonPropertyName("success")] public bool Success { get; set; }
        [JsonPropertyName("data")] public List<T> Data { get; set; } = new();
        [JsonPropertyName("additional_data")] public PipedriveAdditionalData AdditionalData { get; set; }
    }

    public class PipedriveAdditionalData
    {
        [JsonPropertyName("pagination")] public PipedrivePagination Pagination { get; set; }
    }

    public class PipedrivePagination
    {
        [JsonPropertyName("start")] public int Start { get; set; }
        [JsonPropertyName("limit")] public int Limit { get; set; }
        [JsonPropertyName("more_items_in_collection")] public bool MoreItemsInCollection { get; set; }
        [JsonPropertyName("next_start")] public int? NextStart { get; set; }
    }
}