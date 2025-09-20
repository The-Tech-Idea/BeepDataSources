using System.Text.Json.Serialization;

namespace TheTechIdea.Beep.Connectors.Marketing.MailchimpDataSource.Models
{
    // List Models
    public class MailchimpList
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("contact")]
        public MailchimpContact? Contact { get; set; }

        [JsonPropertyName("permission_reminder")]
        public string? PermissionReminder { get; set; }

        [JsonPropertyName("use_archive_bar")]
        public bool? UseArchiveBar { get; set; }

        [JsonPropertyName("campaign_defaults")]
        public MailchimpCampaignDefaults? CampaignDefaults { get; set; }

        [JsonPropertyName("notify_on_subscribe")]
        public string? NotifyOnSubscribe { get; set; }

        [JsonPropertyName("notify_on_unsubscribe")]
        public string? NotifyOnUnsubscribe { get; set; }

        [JsonPropertyName("date_created")]
        public DateTime? DateCreated { get; set; }

        [JsonPropertyName("list_rating")]
        public int? ListRating { get; set; }

        [JsonPropertyName("email_type_option")]
        public bool? EmailTypeOption { get; set; }

        [JsonPropertyName("subscribe_url_short")]
        public string? SubscribeUrlShort { get; set; }

        [JsonPropertyName("subscribe_url_long")]
        public string? SubscribeUrlLong { get; set; }

        [JsonPropertyName("beamer_address")]
        public string? BeamerAddress { get; set; }

        [JsonPropertyName("visibility")]
        public string? Visibility { get; set; }

        [JsonPropertyName("double_optin")]
        public bool? DoubleOptin { get; set; }

        [JsonPropertyName("has_welcome")]
        public bool? HasWelcome { get; set; }

        [JsonPropertyName("marketing_permissions")]
        public bool? MarketingPermissions { get; set; }

        [JsonPropertyName("modules")]
        public List<string>? Modules { get; set; }

        [JsonPropertyName("stats")]
        public MailchimpListStats? Stats { get; set; }
    }

    // Campaign Models
    public class MailchimpCampaign
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("create_time")]
        public DateTime? CreateTime { get; set; }

        [JsonPropertyName("archive_url")]
        public string? ArchiveUrl { get; set; }

        [JsonPropertyName("long_archive_url")]
        public string? LongArchiveUrl { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("emails_sent")]
        public int? EmailsSent { get; set; }

        [JsonPropertyName("send_time")]
        public DateTime? SendTime { get; set; }

        [JsonPropertyName("content_type")]
        public string? ContentType { get; set; }

        [JsonPropertyName("needs_block_refresh")]
        public bool? NeedsBlockRefresh { get; set; }

        [JsonPropertyName("resendable")]
        public bool? Resendable { get; set; }

        [JsonPropertyName("recipients")]
        public MailchimpRecipients? Recipients { get; set; }

        [JsonPropertyName("settings")]
        public MailchimpCampaignSettings? Settings { get; set; }

        [JsonPropertyName("variate_settings")]
        public MailchimpVariateSettings? VariateSettings { get; set; }

        [JsonPropertyName("tracking")]
        public MailchimpTracking? Tracking { get; set; }

        [JsonPropertyName("rss_opts")]
        public MailchimpRssOptions? RssOpts { get; set; }

        [JsonPropertyName("ab_split_opts")]
        public MailchimpAbSplitOptions? AbSplitOpts { get; set; }

        [JsonPropertyName("social_card")]
        public MailchimpSocialCard? SocialCard { get; set; }

        [JsonPropertyName("report_summary")]
        public MailchimpReportSummary? ReportSummary { get; set; }

        [JsonPropertyName("delivery_status")]
        public MailchimpDeliveryStatus? DeliveryStatus { get; set; }
    }

    // Member/Contact Models
    public class MailchimpMember
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("email_address")]
        public string? EmailAddress { get; set; }

        [JsonPropertyName("unique_email_id")]
        public string? UniqueEmailId { get; set; }

        [JsonPropertyName("contact_id")]
        public string? ContactId { get; set; }

        [JsonPropertyName("full_name")]
        public string? FullName { get; set; }

        [JsonPropertyName("web_id")]
        public long? WebId { get; set; }

        [JsonPropertyName("email_type")]
        public string? EmailType { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("consents_to_one_to_one_messaging")]
        public bool? ConsentsToOneToOneMessaging { get; set; }

        [JsonPropertyName("merge_fields")]
        public Dictionary<string, object>? MergeFields { get; set; }

        [JsonPropertyName("interests")]
        public Dictionary<string, bool>? Interests { get; set; }

        [JsonPropertyName("stats")]
        public MailchimpMemberStats? Stats { get; set; }

        [JsonPropertyName("ip_signup")]
        public string? IpSignup { get; set; }

        [JsonPropertyName("timestamp_signup")]
        public DateTime? TimestampSignup { get; set; }

        [JsonPropertyName("ip_opt")]
        public string? IpOpt { get; set; }

        [JsonPropertyName("timestamp_opt")]
        public DateTime? TimestampOpt { get; set; }

        [JsonPropertyName("member_rating")]
        public int? MemberRating { get; set; }

        [JsonPropertyName("last_changed")]
        public DateTime? LastChanged { get; set; }

        [JsonPropertyName("language")]
        public string? Language { get; set; }

        [JsonPropertyName("vip")]
        public bool? Vip { get; set; }

        [JsonPropertyName("email_client")]
        public string? EmailClient { get; set; }

        [JsonPropertyName("location")]
        public MailchimpLocation? Location { get; set; }

        [JsonPropertyName("marketing_permissions")]
        public List<MailchimpMarketingPermission>? MarketingPermissions { get; set; }

        [JsonPropertyName("last_note")]
        public MailchimpNote? LastNote { get; set; }

        [JsonPropertyName("source")]
        public string? Source { get; set; }

        [JsonPropertyName("tags_count")]
        public int? TagsCount { get; set; }

        [JsonPropertyName("tags")]
        public List<MailchimpTag>? Tags { get; set; }

        [JsonPropertyName("list_id")]
        public string? ListId { get; set; }
    }

    // Template Models
    public class MailchimpTemplate
    {
        [JsonPropertyName("id")]
        public long? Id { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("drag_and_drop")]
        public bool? DragAndDrop { get; set; }

        [JsonPropertyName("responsive")]
        public bool? Responsive { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("date_created")]
        public DateTime? DateCreated { get; set; }

        [JsonPropertyName("date_edited")]
        public DateTime? DateEdited { get; set; }

        [JsonPropertyName("created_by")]
        public string? CreatedBy { get; set; }

        [JsonPropertyName("edited_by")]
        public string? EditedBy { get; set; }

        [JsonPropertyName("active")]
        public bool? Active { get; set; }

        [JsonPropertyName("folder_id")]
        public string? FolderId { get; set; }

        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }

        [JsonPropertyName("share_url")]
        public string? ShareUrl { get; set; }
    }

    // Automation Models
    public class MailchimpAutomation
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("create_time")]
        public DateTime? CreateTime { get; set; }

        [JsonPropertyName("start_time")]
        public DateTime? StartTime { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("emails_sent")]
        public int? EmailsSent { get; set; }

        [JsonPropertyName("recipients")]
        public MailchimpRecipients? Recipients { get; set; }

        [JsonPropertyName("settings")]
        public MailchimpAutomationSettings? Settings { get; set; }

        [JsonPropertyName("tracking")]
        public MailchimpTracking? Tracking { get; set; }

        [JsonPropertyName("trigger_settings")]
        public MailchimpTriggerSettings? TriggerSettings { get; set; }

        [JsonPropertyName("report_summary")]
        public MailchimpReportSummary? ReportSummary { get; set; }
    }

    // Segment Models
    public class MailchimpSegment
    {
        [JsonPropertyName("id")]
        public long? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("member_count")]
        public int? MemberCount { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("options")]
        public MailchimpSegmentOptions? Options { get; set; }

        [JsonPropertyName("list_id")]
        public string? ListId { get; set; }
    }

    // Merge Field Models
    public class MailchimpMergeField
    {
        [JsonPropertyName("merge_id")]
        public int? MergeId { get; set; }

        [JsonPropertyName("tag")]
        public string? Tag { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("required")]
        public bool? Required { get; set; }

        [JsonPropertyName("default_value")]
        public string? DefaultValue { get; set; }

        [JsonPropertyName("public")]
        public bool? Public { get; set; }

        [JsonPropertyName("display_order")]
        public int? DisplayOrder { get; set; }

        [JsonPropertyName("options")]
        public MailchimpMergeFieldOptions? Options { get; set; }

        [JsonPropertyName("help_text")]
        public string? HelpText { get; set; }

        [JsonPropertyName("list_id")]
        public string? ListId { get; set; }
    }

    // Interest Category Models
    public class MailchimpInterestCategory
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("display_order")]
        public int? DisplayOrder { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("list_id")]
        public string? ListId { get; set; }
    }

    // Interest Models
    public class MailchimpInterest
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("subscriber_count")]
        public string? SubscriberCount { get; set; }

        [JsonPropertyName("display_order")]
        public int? DisplayOrder { get; set; }

        [JsonPropertyName("category_id")]
        public string? CategoryId { get; set; }

        [JsonPropertyName("list_id")]
        public string? ListId { get; set; }
    }

    // Report Models
    public class MailchimpReport
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("campaign_title")]
        public string? CampaignTitle { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("emails_sent")]
        public int? EmailsSent { get; set; }

        [JsonPropertyName("abuse_reports")]
        public int? AbuseReports { get; set; }

        [JsonPropertyName("unsubscribed")]
        public int? Unsubscribed { get; set; }

        [JsonPropertyName("send_time")]
        public DateTime? SendTime { get; set; }

        [JsonPropertyName("bounces")]
        public MailchimpBounces? Bounces { get; set; }

        [JsonPropertyName("forwards")]
        public MailchimpForwards? Forwards { get; set; }

        [JsonPropertyName("opens")]
        public MailchimpOpens? Opens { get; set; }

        [JsonPropertyName("clicks")]
        public MailchimpClicks? Clicks { get; set; }

        [JsonPropertyName("facebook_likes")]
        public MailchimpFacebookLikes? FacebookLikes { get; set; }

        [JsonPropertyName("industry_stats")]
        public MailchimpIndustryStats? IndustryStats { get; set; }

        [JsonPropertyName("list_stats")]
        public MailchimpListStats? ListStats { get; set; }

        [JsonPropertyName("ab_split")]
        public MailchimpAbSplit? AbSplit { get; set; }

        [JsonPropertyName("timewarp")]
        public List<MailchimpTimewarp>? Timewarp { get; set; }

        [JsonPropertyName("timeseries")]
        public List<MailchimpTimeseries>? Timeseries { get; set; }

        [JsonPropertyName("share_report")]
        public MailchimpShareReport? ShareReport { get; set; }

        [JsonPropertyName("delivery_status")]
        public MailchimpDeliveryStatus? DeliveryStatus { get; set; }
    }

    // Supporting Models
    public class MailchimpContact
    {
        [JsonPropertyName("company")]
        public string? Company { get; set; }

        [JsonPropertyName("address1")]
        public string? Address1 { get; set; }

        [JsonPropertyName("address2")]
        public string? Address2 { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("zip")]
        public string? Zip { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }
    }

    public class MailchimpCampaignDefaults
    {
        [JsonPropertyName("from_name")]
        public string? FromName { get; set; }

        [JsonPropertyName("from_email")]
        public string? FromEmail { get; set; }

        [JsonPropertyName("subject")]
        public string? Subject { get; set; }

        [JsonPropertyName("language")]
        public string? Language { get; set; }
    }

    public class MailchimpListStats
    {
        [JsonPropertyName("member_count")]
        public int? MemberCount { get; set; }

        [JsonPropertyName("unsubscribe_count")]
        public int? UnsubscribeCount { get; set; }

        [JsonPropertyName("cleaned_count")]
        public int? CleanedCount { get; set; }

        [JsonPropertyName("member_count_since_send")]
        public int? MemberCountSinceSend { get; set; }

        [JsonPropertyName("unsubscribe_count_since_send")]
        public int? UnsubscribeCountSinceSend { get; set; }

        [JsonPropertyName("cleaned_count_since_send")]
        public int? CleanedCountSinceSend { get; set; }

        [JsonPropertyName("campaign_count")]
        public int? CampaignCount { get; set; }

        [JsonPropertyName("campaign_last_sent")]
        public DateTime? CampaignLastSent { get; set; }

        [JsonPropertyName("merge_field_count")]
        public int? MergeFieldCount { get; set; }

        [JsonPropertyName("avg_sub_rate")]
        public decimal? AvgSubRate { get; set; }

        [JsonPropertyName("avg_unsub_rate")]
        public decimal? AvgUnsubRate { get; set; }

        [JsonPropertyName("target_sub_rate")]
        public decimal? TargetSubRate { get; set; }

        [JsonPropertyName("open_rate")]
        public decimal? OpenRate { get; set; }

        [JsonPropertyName("click_rate")]
        public decimal? ClickRate { get; set; }

        [JsonPropertyName("last_sub_date")]
        public DateTime? LastSubDate { get; set; }

        [JsonPropertyName("last_unsub_date")]
        public DateTime? LastUnsubDate { get; set; }
    }

    public class MailchimpRecipients
    {
        [JsonPropertyName("list_id")]
        public string? ListId { get; set; }

        [JsonPropertyName("list_name")]
        public string? ListName { get; set; }

        [JsonPropertyName("segment_text")]
        public string? SegmentText { get; set; }

        [JsonPropertyName("recipient_count")]
        public int? RecipientCount { get; set; }

        [JsonPropertyName("segment_opts")]
        public MailchimpSegmentOptions? SegmentOpts { get; set; }
    }

    public class MailchimpCampaignSettings
    {
        [JsonPropertyName("subject_line")]
        public string? SubjectLine { get; set; }

        [JsonPropertyName("preview_text")]
        public string? PreviewText { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("from_name")]
        public string? FromName { get; set; }

        [JsonPropertyName("reply_to")]
        public string? ReplyTo { get; set; }

        [JsonPropertyName("use_conversation")]
        public bool? UseConversation { get; set; }

        [JsonPropertyName("to_name")]
        public string? ToName { get; set; }

        [JsonPropertyName("folder_id")]
        public string? FolderId { get; set; }

        [JsonPropertyName("authenticate")]
        public bool? Authenticate { get; set; }

        [JsonPropertyName("auto_footer")]
        public bool? AutoFooter { get; set; }

        [JsonPropertyName("inline_css")]
        public bool? InlineCss { get; set; }

        [JsonPropertyName("auto_tweet")]
        public bool? AutoTweet { get; set; }

        [JsonPropertyName("fb_comments")]
        public bool? FbComments { get; set; }

        [JsonPropertyName("timewarp")]
        public bool? Timewarp { get; set; }

        [JsonPropertyName("template_id")]
        public long? TemplateId { get; set; }

        [JsonPropertyName("drag_and_drop")]
        public bool? DragAndDrop { get; set; }
    }

    public class MailchimpVariateSettings
    {
        [JsonPropertyName("winner_criteria")]
        public string? WinnerCriteria { get; set; }

        [JsonPropertyName("wait_time")]
        public int? WaitTime { get; set; }

        [JsonPropertyName("test_size")]
        public int? TestSize { get; set; }

        [JsonPropertyName("subject_lines")]
        public List<string>? SubjectLines { get; set; }

        [JsonPropertyName("send_times")]
        public List<DateTime>? SendTimes { get; set; }

        [JsonPropertyName("from_names")]
        public List<string>? FromNames { get; set; }

        [JsonPropertyName("reply_to_addresses")]
        public List<string>? ReplyToAddresses { get; set; }
    }

    public class MailchimpTracking
    {
        [JsonPropertyName("opens")]
        public bool? Opens { get; set; }

        [JsonPropertyName("html_clicks")]
        public bool? HtmlClicks { get; set; }

        [JsonPropertyName("text_clicks")]
        public bool? TextClicks { get; set; }

        [JsonPropertyName("goal_tracking")]
        public bool? GoalTracking { get; set; }

        [JsonPropertyName("ecomm360")]
        public bool? Ecomm360 { get; set; }

        [JsonPropertyName("google_analytics")]
        public string? GoogleAnalytics { get; set; }

        [JsonPropertyName("clicktale")]
        public string? Clicktale { get; set; }

        [JsonPropertyName("salesforce")]
        public MailchimpSalesforceTracking? Salesforce { get; set; }

        [JsonPropertyName("capsule")]
        public MailchimpCapsuleTracking? Capsule { get; set; }
    }

    public class MailchimpRssOptions
    {
        [JsonPropertyName("feed_url")]
        public string? FeedUrl { get; set; }

        [JsonPropertyName("frequency")]
        public string? Frequency { get; set; }

        [JsonPropertyName("schedule")]
        public MailchimpSchedule? Schedule { get; set; }

        [JsonPropertyName("last_sent")]
        public DateTime? LastSent { get; set; }

        [JsonPropertyName("constrain_rss_img")]
        public bool? ConstrainRssImg { get; set; }
    }

    public class MailchimpAbSplitOptions
    {
        [JsonPropertyName("split_test")]
        public string? SplitTest { get; set; }

        [JsonPropertyName("pick_winner")]
        public string? PickWinner { get; set; }

        [JsonPropertyName("wait_units")]
        public string? WaitUnits { get; set; }

        [JsonPropertyName("wait_time")]
        public int? WaitTime { get; set; }

        [JsonPropertyName("split_size")]
        public int? SplitSize { get; set; }

        [JsonPropertyName("from_name_a")]
        public string? FromNameA { get; set; }

        [JsonPropertyName("from_name_b")]
        public string? FromNameB { get; set; }

        [JsonPropertyName("reply_email_a")]
        public string? ReplyEmailA { get; set; }

        [JsonPropertyName("reply_email_b")]
        public string? ReplyEmailB { get; set; }

        [JsonPropertyName("subject_a")]
        public string? SubjectA { get; set; }

        [JsonPropertyName("subject_b")]
        public string? SubjectB { get; set; }

        [JsonPropertyName("send_time_a")]
        public DateTime? SendTimeA { get; set; }

        [JsonPropertyName("send_time_b")]
        public DateTime? SendTimeB { get; set; }

        [JsonPropertyName("template_id_a")]
        public long? TemplateIdA { get; set; }

        [JsonPropertyName("template_id_b")]
        public long? TemplateIdB { get; set; }
    }

    public class MailchimpSocialCard
    {
        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }
    }

    public class MailchimpReportSummary
    {
        [JsonPropertyName("opens")]
        public int? Opens { get; set; }

        [JsonPropertyName("unique_opens")]
        public int? UniqueOpens { get; set; }

        [JsonPropertyName("open_rate")]
        public decimal? OpenRate { get; set; }

        [JsonPropertyName("clicks")]
        public int? Clicks { get; set; }

        [JsonPropertyName("subscriber_clicks")]
        public int? SubscriberClicks { get; set; }

        [JsonPropertyName("click_rate")]
        public decimal? ClickRate { get; set; }

        [JsonPropertyName("ecommerce")]
        public MailchimpEcommerceReport? Ecommerce { get; set; }
    }

    public class MailchimpDeliveryStatus
    {
        [JsonPropertyName("enabled")]
        public bool? Enabled { get; set; }

        [JsonPropertyName("can_cancel")]
        public bool? CanCancel { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("emails_sent")]
        public int? EmailsSent { get; set; }

        [JsonPropertyName("emails_canceled")]
        public int? EmailsCanceled { get; set; }
    }

    public class MailchimpMemberStats
    {
        [JsonPropertyName("avg_open_rate")]
        public decimal? AvgOpenRate { get; set; }

        [JsonPropertyName("avg_click_rate")]
        public decimal? AvgClickRate { get; set; }
    }

    public class MailchimpLocation
    {
        [JsonPropertyName("latitude")]
        public decimal? Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public decimal? Longitude { get; set; }

        [JsonPropertyName("gmtoff")]
        public int? Gmtoff { get; set; }

        [JsonPropertyName("dstoff")]
        public int? Dstoff { get; set; }

        [JsonPropertyName("country_code")]
        public string? CountryCode { get; set; }

        [JsonPropertyName("timezone")]
        public string? Timezone { get; set; }
    }

    public class MailchimpMarketingPermission
    {
        [JsonPropertyName("marketing_permission_id")]
        public string? MarketingPermissionId { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("enabled")]
        public bool? Enabled { get; set; }
    }

    public class MailchimpNote
    {
        [JsonPropertyName("note_id")]
        public int? NoteId { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("created_by")]
        public string? CreatedBy { get; set; }

        [JsonPropertyName("note")]
        public string? Note { get; set; }
    }

    public class MailchimpTag
    {
        [JsonPropertyName("id")]
        public long? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    public class MailchimpAutomationSettings
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("from_name")]
        public string? FromName { get; set; }

        [JsonPropertyName("reply_to")]
        public string? ReplyTo { get; set; }

        [JsonPropertyName("use_conversation")]
        public bool? UseConversation { get; set; }

        [JsonPropertyName("to_name")]
        public string? ToName { get; set; }

        [JsonPropertyName("authenticate")]
        public bool? Authenticate { get; set; }
    }

    public class MailchimpTriggerSettings
    {
        [JsonPropertyName("workflow_type")]
        public string? WorkflowType { get; set; }

        [JsonPropertyName("workflow_title")]
        public string? WorkflowTitle { get; set; }

        [JsonPropertyName("runtime")]
        public MailchimpRuntime? Runtime { get; set; }
    }

    public class MailchimpSegmentOptions
    {
        [JsonPropertyName("saved_segment_id")]
        public long? SavedSegmentId { get; set; }

        [JsonPropertyName("prebuilt_segment_id")]
        public string? PrebuiltSegmentId { get; set; }

        [JsonPropertyName("match")]
        public string? Match { get; set; }

        [JsonPropertyName("conditions")]
        public List<MailchimpCondition>? Conditions { get; set; }
    }

    public class MailchimpMergeFieldOptions
    {
        [JsonPropertyName("default_country")]
        public int? DefaultCountry { get; set; }

        [JsonPropertyName("phone_format")]
        public string? PhoneFormat { get; set; }

        [JsonPropertyName("date_format")]
        public string? DateFormat { get; set; }

        [JsonPropertyName("choices")]
        public List<string>? Choices { get; set; }

        [JsonPropertyName("size")]
        public int? Size { get; set; }
    }

    public class MailchimpBounces
    {
        [JsonPropertyName("hard_bounces")]
        public int? HardBounces { get; set; }

        [JsonPropertyName("soft_bounces")]
        public int? SoftBounces { get; set; }

        [JsonPropertyName("syntax_errors")]
        public int? SyntaxErrors { get; set; }
    }

    public class MailchimpForwards
    {
        [JsonPropertyName("forwards_count")]
        public int? ForwardsCount { get; set; }

        [JsonPropertyName("forwards_opens")]
        public int? ForwardsOpens { get; set; }
    }

    public class MailchimpOpens
    {
        [JsonPropertyName("opens_total")]
        public int? OpensTotal { get; set; }

        [JsonPropertyName("unique_opens")]
        public int? UniqueOpens { get; set; }

        [JsonPropertyName("open_rate")]
        public decimal? OpenRate { get; set; }

        [JsonPropertyName("last_open")]
        public DateTime? LastOpen { get; set; }
    }

    public class MailchimpClicks
    {
        [JsonPropertyName("clicks_total")]
        public int? ClicksTotal { get; set; }

        [JsonPropertyName("unique_clicks")]
        public int? UniqueClicks { get; set; }

        [JsonPropertyName("unique_subscriber_clicks")]
        public int? UniqueSubscriberClicks { get; set; }

        [JsonPropertyName("click_rate")]
        public decimal? ClickRate { get; set; }

        [JsonPropertyName("last_click")]
        public DateTime? LastClick { get; set; }
    }

    public class MailchimpFacebookLikes
    {
        [JsonPropertyName("recipient_likes")]
        public int? RecipientLikes { get; set; }

        [JsonPropertyName("unique_likes")]
        public int? UniqueLikes { get; set; }

        [JsonPropertyName("facebook_likes")]
        public int? FacebookLikes { get; set; }
    }

    public class MailchimpIndustryStats
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("open_rate")]
        public decimal? OpenRate { get; set; }

        [JsonPropertyName("click_rate")]
        public decimal? ClickRate { get; set; }

        [JsonPropertyName("bounce_rate")]
        public decimal? BounceRate { get; set; }

        [JsonPropertyName("unopen_rate")]
        public decimal? UnopenRate { get; set; }

        [JsonPropertyName("unsub_rate")]
        public decimal? UnsubRate { get; set; }

        [JsonPropertyName("abuse_rate")]
        public decimal? AbuseRate { get; set; }
    }

    public class MailchimpAbSplit
    {
        [JsonPropertyName("a")]
        public MailchimpAbSplitVariant? A { get; set; }

        [JsonPropertyName("b")]
        public MailchimpAbSplitVariant? B { get; set; }
    }

    public class MailchimpTimewarp
    {
        [JsonPropertyName("gmt_offset")]
        public int? GmtOffset { get; set; }

        [JsonPropertyName("opens")]
        public int? Opens { get; set; }

        [JsonPropertyName("last_open")]
        public DateTime? LastOpen { get; set; }

        [JsonPropertyName("unique_opens")]
        public int? UniqueOpens { get; set; }
    }

    public class MailchimpTimeseries
    {
        [JsonPropertyName("timestamp")]
        public DateTime? Timestamp { get; set; }

        [JsonPropertyName("emails_sent")]
        public int? EmailsSent { get; set; }

        [JsonPropertyName("unique_opens")]
        public int? UniqueOpens { get; set; }

        [JsonPropertyName("recipients_clicks")]
        public int? RecipientsClicks { get; set; }
    }

    public class MailchimpShareReport
    {
        [JsonPropertyName("share_url")]
        public string? ShareUrl { get; set; }

        [JsonPropertyName("share_password")]
        public string? SharePassword { get; set; }
    }

    // Additional supporting models
    public class MailchimpSalesforceTracking
    {
        [JsonPropertyName("campaign")]
        public bool? Campaign { get; set; }

        [JsonPropertyName("notes")]
        public bool? Notes { get; set; }
    }

    public class MailchimpCapsuleTracking
    {
        [JsonPropertyName("notes")]
        public bool? Notes { get; set; }
    }

    public class MailchimpSchedule
    {
        [JsonPropertyName("hour")]
        public int? Hour { get; set; }

        [JsonPropertyName("daily_send")]
        public MailchimpDailySend? DailySend { get; set; }

        [JsonPropertyName("weekly_send_day")]
        public string? WeeklySendDay { get; set; }

        [JsonPropertyName("monthly_send_date")]
        public int? MonthlySendDate { get; set; }
    }

    public class MailchimpEcommerceReport
    {
        [JsonPropertyName("total_orders")]
        public int? TotalOrders { get; set; }

        [JsonPropertyName("total_spent")]
        public decimal? TotalSpent { get; set; }

        [JsonPropertyName("total_revenue")]
        public decimal? TotalRevenue { get; set; }
    }

    public class MailchimpRuntime
    {
        [JsonPropertyName("days")]
        public List<string>? Days { get; set; }

        [JsonPropertyName("hours")]
        public MailchimpHours? Hours { get; set; }
    }

    public class MailchimpCondition
    {
        [JsonPropertyName("condition_type")]
        public string? ConditionType { get; set; }

        [JsonPropertyName("field")]
        public string? Field { get; set; }

        [JsonPropertyName("op")]
        public string? Op { get; set; }

        [JsonPropertyName("value")]
        public object? Value { get; set; }
    }

    public class MailchimpAbSplitVariant
    {
        [JsonPropertyName("clicks")]
        public int? Clicks { get; set; }

        [JsonPropertyName("opens")]
        public int? Opens { get; set; }

        [JsonPropertyName("bounces")]
        public int? Bounces { get; set; }

        [JsonPropertyName("unsubs")]
        public int? Unsubs { get; set; }

        [JsonPropertyName("winner")]
        public bool? Winner { get; set; }
    }

    public class MailchimpDailySend
    {
        [JsonPropertyName("sunday")]
        public bool? Sunday { get; set; }

        [JsonPropertyName("monday")]
        public bool? Monday { get; set; }

        [JsonPropertyName("tuesday")]
        public bool? Tuesday { get; set; }

        [JsonPropertyName("wednesday")]
        public bool? Wednesday { get; set; }

        [JsonPropertyName("thursday")]
        public bool? Thursday { get; set; }

        [JsonPropertyName("friday")]
        public bool? Friday { get; set; }

        [JsonPropertyName("saturday")]
        public bool? Saturday { get; set; }
    }

    public class MailchimpHours
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("hours")]
        public List<int>? HoursList { get; set; }
    }
}