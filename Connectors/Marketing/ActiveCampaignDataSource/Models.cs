// File: BeepDM/Connectors/Marketing/ActiveCampaignDataSource/Models/ActiveCampaignModels.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Marketing.ActiveCampaignDataSource.Models
{
    // Base
    public abstract class ActiveCampaignEntityBase
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("created_timestamp")] public long? CreatedTimestamp { get; set; }
        [JsonPropertyName("updated_timestamp")] public long? UpdatedTimestamp { get; set; }

        [JsonIgnore] public IDataSource? DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : ActiveCampaignEntityBase { DataSource = ds; return (T)this; }
    }

    // ---------- Contact objects ----------

    public sealed class ActiveCampaignContact : ActiveCampaignEntityBase
    {
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("firstName")] public string? FirstName { get; set; }
        [JsonPropertyName("lastName")] public string? LastName { get; set; }
        [JsonPropertyName("phone")] public string? Phone { get; set; }
        [JsonPropertyName("orgname")] public string? OrganizationName { get; set; }
        [JsonPropertyName("segmentio_id")] public string? SegmentioId { get; set; }
        [JsonPropertyName("bounced_hard")] public string? BouncedHard { get; set; }
        [JsonPropertyName("bounced_soft")] public string? BouncedSoft { get; set; }
        [JsonPropertyName("bounced_date")] public string? BouncedDate { get; set; }
        [JsonPropertyName("ip")] public string? Ip { get; set; }
        [JsonPropertyName("ua")] public string? UserAgent { get; set; }
        [JsonPropertyName("hash")] public string? Hash { get; set; }
        [JsonPropertyName("socialdata_lastcheck")] public string? SocialDataLastCheck { get; set; }
        [JsonPropertyName("email_local")] public string? EmailLocal { get; set; }
        [JsonPropertyName("email_domain")] public string? EmailDomain { get; set; }
        [JsonPropertyName("sentcnt")] public string? SentCount { get; set; }
        [JsonPropertyName("rating_tstamp")] public string? RatingTimestamp { get; set; }
        [JsonPropertyName("gravatar")] public string? Gravatar { get; set; }
        [JsonPropertyName("deleted")] public string? Deleted { get; set; }
        [JsonPropertyName("anonymized")] public string? Anonymized { get; set; }
        [JsonPropertyName("adate")] public string? ActionDate { get; set; }
        [JsonPropertyName("udate")] public string? UpdateDate { get; set; }
        [JsonPropertyName("edate")] public string? EmailDate { get; set; }
        [JsonPropertyName("scoreValues")] public List<ActiveCampaignScoreValue>? ScoreValues { get; set; } = new();
        [JsonPropertyName("links")] public ActiveCampaignContactLinks? Links { get; set; }
        [JsonPropertyName("fieldValues")] public List<ActiveCampaignFieldValue>? FieldValues { get; set; } = new();
        [JsonPropertyName("accountContacts")] public List<ActiveCampaignAccountContact>? AccountContacts { get; set; } = new();
    }

    public sealed class ActiveCampaignContactLinks
    {
        [JsonPropertyName("bounceLogs")] public string? BounceLogs { get; set; }
        [JsonPropertyName("contactAutomations")] public string? ContactAutomations { get; set; }
        [JsonPropertyName("contactData")] public string? ContactData { get; set; }
        [JsonPropertyName("contactGoals")] public string? ContactGoals { get; set; }
        [JsonPropertyName("contactLists")] public string? ContactLists { get; set; }
        [JsonPropertyName("contactLogs")] public string? ContactLogs { get; set; }
        [JsonPropertyName("contactTags")] public string? ContactTags { get; set; }
        [JsonPropertyName("contactDeals")] public string? ContactDeals { get; set; }
        [JsonPropertyName("deals")] public string? Deals { get; set; }
        [JsonPropertyName("fieldValues")] public string? FieldValues { get; set; }
        [JsonPropertyName("geoIps")] public string? GeoIps { get; set; }
        [JsonPropertyName("notes")] public string? Notes { get; set; }
        [JsonPropertyName("organization")] public string? Organization { get; set; }
        [JsonPropertyName("plusAppend")] public string? PlusAppend { get; set; }
        [JsonPropertyName("trackingLogs")] public string? TrackingLogs { get; set; }
        [JsonPropertyName("scoreValues")] public string? ScoreValues { get; set; }
    }

    public sealed class ActiveCampaignScoreValue
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("contact")] public string? Contact { get; set; }
        [JsonPropertyName("list")] public string? List { get; set; }
        [JsonPropertyName("score")] public string? Score { get; set; }
        [JsonPropertyName("cdate")] public string? CreatedDate { get; set; }
    }

    public sealed class ActiveCampaignFieldValue
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("contact")] public string? Contact { get; set; }
        [JsonPropertyName("field")] public string? Field { get; set; }
        [JsonPropertyName("value")] public string? Value { get; set; }
        [JsonPropertyName("cdate")] public string? CreatedDate { get; set; }
        [JsonPropertyName("udate")] public string? UpdatedDate { get; set; }
    }

    public sealed class ActiveCampaignAccountContact
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("account")] public string? Account { get; set; }
        [JsonPropertyName("contact")] public string? Contact { get; set; }
        [JsonPropertyName("jobtitle")] public string? JobTitle { get; set; }
        [JsonPropertyName("created_timestamp")] public long? CreatedTimestamp { get; set; }
        [JsonPropertyName("updated_timestamp")] public long? UpdatedTimestamp { get; set; }
        [JsonPropertyName("links")] public ActiveCampaignAccountContactLinks? Links { get; set; }
    }

    public sealed class ActiveCampaignAccountContactLinks
    {
        [JsonPropertyName("account")] public string? Account { get; set; }
        [JsonPropertyName("contact")] public string? Contact { get; set; }
    }

    // ---------- List objects ----------

    public sealed class ActiveCampaignList : ActiveCampaignEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("stringid")] public string? StringId { get; set; }
        [JsonPropertyName("sender_url")] public string? SenderUrl { get; set; }
        [JsonPropertyName("sender_reminder")] public string? SenderReminder { get; set; }
        [JsonPropertyName("send_last_broadcast")] public string? SendLastBroadcast { get; set; }
        [JsonPropertyName("carboncopy")] public string? CarbonCopy { get; set; }
        [JsonPropertyName("subscription_notify")] public string? SubscriptionNotify { get; set; }
        [JsonPropertyName("unsubscription_notify")] public string? UnsubscriptionNotify { get; set; }
        [JsonPropertyName("require_name")] public string? RequireName { get; set; }
        [JsonPropertyName("get_unsubscribe_reason")] public string? GetUnsubscribeReason { get; set; }
        [JsonPropertyName("to_name")] public string? ToName { get; set; }
        [JsonPropertyName("optinoptout")] public string? OptinOptout { get; set; }
        [JsonPropertyName("sender_name")] public string? SenderName { get; set; }
        [JsonPropertyName("sender_addr1")] public string? SenderAddr1 { get; set; }
        [JsonPropertyName("sender_addr2")] public string? SenderAddr2 { get; set; }
        [JsonPropertyName("sender_city")] public string? SenderCity { get; set; }
        [JsonPropertyName("sender_state")] public string? SenderState { get; set; }
        [JsonPropertyName("sender_zip")] public string? SenderZip { get; set; }
        [JsonPropertyName("sender_country")] public string? SenderCountry { get; set; }
        [JsonPropertyName("fulladdress")] public string? FullAddress { get; set; }
        [JsonPropertyName("links")] public ActiveCampaignListLinks? Links { get; set; }
    }

    public sealed class ActiveCampaignListLinks
    {
        [JsonPropertyName("contactGoalData")] public string? ContactGoalData { get; set; }
        [JsonPropertyName("automationLists")] public string? AutomationLists { get; set; }
        [JsonPropertyName("campaignLists")] public string? CampaignLists { get; set; }
        [JsonPropertyName("contactLists")] public string? ContactLists { get; set; }
        [JsonPropertyName("messageLists")] public string? MessageLists { get; set; }
        [JsonPropertyName("userLists")] public string? UserLists { get; set; }
        [JsonPropertyName("organizationLists")] public string? OrganizationLists { get; set; }
    }

    // ---------- Tag objects ----------

    public sealed class ActiveCampaignTag : ActiveCampaignEntityBase
    {
        [JsonPropertyName("tag")] public string? Tag { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("tagType")] public string? TagType { get; set; }
        [JsonPropertyName("cdate")] public string? CreatedDate { get; set; }
        [JsonPropertyName("links")] public ActiveCampaignTagLinks? Links { get; set; }
    }

    public sealed class ActiveCampaignTagLinks
    {
        [JsonPropertyName("contactTags")] public string? ContactTags { get; set; }
    }

    public sealed class ActiveCampaignContactTag
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("contact")] public string? Contact { get; set; }
        [JsonPropertyName("tag")] public string? Tag { get; set; }
        [JsonPropertyName("cdate")] public string? CreatedDate { get; set; }
        [JsonPropertyName("links")] public ActiveCampaignContactTagLinks? Links { get; set; }
    }

    public sealed class ActiveCampaignContactTagLinks
    {
        [JsonPropertyName("contact")] public string? Contact { get; set; }
        [JsonPropertyName("tag")] public string? Tag { get; set; }
    }

    // ---------- Campaign objects ----------

    public sealed class ActiveCampaignCampaign : ActiveCampaignEntityBase
    {
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("public")] public string? Public { get; set; }
        [JsonPropertyName("mail_cleanup")] public string? MailCleanup { get; set; }
        [JsonPropertyName("mail_send")] public string? MailSend { get; set; }
        [JsonPropertyName("mail_scheduler")] public string? MailScheduler { get; set; }
        [JsonPropertyName("mailer_log_file")] public string? MailerLogFile { get; set; }
        [JsonPropertyName("recurring")] public string? Recurring { get; set; }
        [JsonPropertyName("recurring_schedule")] public string? RecurringSchedule { get; set; }
        [JsonPropertyName("recurring_next_send")] public string? RecurringNextSend { get; set; }
        [JsonPropertyName("recurring_times_sent")] public string? RecurringTimesSent { get; set; }
        [JsonPropertyName("is_split")] public string? IsSplit { get; set; }
        [JsonPropertyName("split_type")] public string? SplitType { get; set; }
        [JsonPropertyName("split_content")] public string? SplitContent { get; set; }
        [JsonPropertyName("split_offset")] public string? SplitOffset { get; set; }
        [JsonPropertyName("split_offset_type")] public string? SplitOffsetType { get; set; }
        [JsonPropertyName("split_winner_message_id")] public string? SplitWinnerMessageId { get; set; }
        [JsonPropertyName("split_winner_awaiting")] public string? SplitWinnerAwaiting { get; set; }
        [JsonPropertyName("responder_offset")] public string? ResponderOffset { get; set; }
        [JsonPropertyName("responder_type")] public string? ResponderType { get; set; }
        [JsonPropertyName("responder_existing")] public string? ResponderExisting { get; set; }
        [JsonPropertyName("reminder_field")] public string? ReminderField { get; set; }
        [JsonPropertyName("reminder_format")] public string? ReminderFormat { get; set; }
        [JsonPropertyName("reminder_type")] public string? ReminderType { get; set; }
        [JsonPropertyName("reminder_offset")] public string? ReminderOffset { get; set; }
        [JsonPropertyName("reminder_offset_type")] public string? ReminderOffsetType { get; set; }
        [JsonPropertyName("reminder_offset_sign")] public string? ReminderOffsetSign { get; set; }
        [JsonPropertyName("reminder_last_cron_run")] public string? ReminderLastCronRun { get; set; }
        [JsonPropertyName("queueid")] public string? QueueId { get; set; }
        [JsonPropertyName("madmin")] public string? MAdmin { get; set; }
        [JsonPropertyName("mgroup")] public string? MGroup { get; set; }
        [JsonPropertyName("segmentid")] public string? SegmentId { get; set; }
        [JsonPropertyName("bounceid")] public string? BounceId { get; set; }
        [JsonPropertyName("realcid")] public string? RealCampaignId { get; set; }
        [JsonPropertyName("sendid")] public string? SendId { get; set; }
        [JsonPropertyName("threadid")] public string? ThreadId { get; set; }
        [JsonPropertyName("seriesid")] public string? SeriesId { get; set; }
        [JsonPropertyName("formid")] public string? FormId { get; set; }
        [JsonPropertyName("basemessageid")] public string? BaseMessageId { get; set; }
        [JsonPropertyName("basemessage_name")] public string? BaseMessageName { get; set; }
        [JsonPropertyName("open_rate")] public string? OpenRate { get; set; }
        [JsonPropertyName("click_rate")] public string? ClickRate { get; set; }
        [JsonPropertyName("bounce_rate")] public string? BounceRate { get; set; }
        [JsonPropertyName("unsubscribe_rate")] public string? UnsubscribeRate { get; set; }
        [JsonPropertyName("links")] public ActiveCampaignCampaignLinks? Links { get; set; }
    }

    public sealed class ActiveCampaignCampaignLinks
    {
        [JsonPropertyName("campaignLists")] public string? CampaignLists { get; set; }
        [JsonPropertyName("campaignMessages")] public string? CampaignMessages { get; set; }
        [JsonPropertyName("links")] public string? Links { get; set; }
        [JsonPropertyName("campaignData")] public string? CampaignData { get; set; }
        [JsonPropertyName("automation")] public string? Automation { get; set; }
        [JsonPropertyName("user")] public string? User { get; set; }
    }

    // ---------- Deal objects ----------

    public sealed class ActiveCampaignDeal : ActiveCampaignEntityBase
    {
        [JsonPropertyName("contact")] public string? Contact { get; set; }
        [JsonPropertyName("organization")] public string? Organization { get; set; }
        [JsonPropertyName("group")] public string? Group { get; set; }
        [JsonPropertyName("stage")] public string? Stage { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("currency")] public string? Currency { get; set; }
        [JsonPropertyName("value")] public string? Value { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("links")] public ActiveCampaignDealLinks? Links { get; set; }
    }

    public sealed class ActiveCampaignDealLinks
    {
        [JsonPropertyName("dealTasks")] public string? DealTasks { get; set; }
        [JsonPropertyName("contact")] public string? Contact { get; set; }
        [JsonPropertyName("contactDeals")] public string? ContactDeals { get; set; }
        [JsonPropertyName("group")] public string? Group { get; set; }
        [JsonPropertyName("nextTask")] public string? NextTask { get; set; }
        [JsonPropertyName("notes")] public string? Notes { get; set; }
        [JsonPropertyName("organization")] public string? Organization { get; set; }
        [JsonPropertyName("owner")] public string? Owner { get; set; }
        [JsonPropertyName("scoreValues")] public string? ScoreValues { get; set; }
        [JsonPropertyName("stage")] public string? Stage { get; set; }
        [JsonPropertyName("tasks")] public string? Tasks { get; set; }
    }

    public sealed class ActiveCampaignDealStage : ActiveCampaignEntityBase
    {
        [JsonPropertyName("group")] public string? Group { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("color")] public string? Color { get; set; }
        [JsonPropertyName("width")] public string? Width { get; set; }
        [JsonPropertyName("ordering")] public string? Ordering { get; set; }
        [JsonPropertyName("card_region")] public string? CardRegion { get; set; }
        [JsonPropertyName("links")] public ActiveCampaignDealStageLinks? Links { get; set; }
    }

    public sealed class ActiveCampaignDealStageLinks
    {
        [JsonPropertyName("deals")] public string? Deals { get; set; }
        [JsonPropertyName("group")] public string? Group { get; set; }
    }

    public sealed class ActiveCampaignDealGroup : ActiveCampaignEntityBase
    {
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("allgroups")] public string? AllGroups { get; set; }
        [JsonPropertyName("allusers")] public string? AllUsers { get; set; }
        [JsonPropertyName("autoassign")] public string? AutoAssign { get; set; }
        [JsonPropertyName("currency")] public string? Currency { get; set; }
        [JsonPropertyName("links")] public ActiveCampaignDealGroupLinks? Links { get; set; }
    }

    public sealed class ActiveCampaignDealGroupLinks
    {
        [JsonPropertyName("dealStages")] public string? DealStages { get; set; }
        [JsonPropertyName("deals")] public string? Deals { get; set; }
        [JsonPropertyName("users")] public string? Users { get; set; }
    }

    // ---------- Account objects ----------

    public sealed class ActiveCampaignAccount : ActiveCampaignEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("account_url")] public string? AccountUrl { get; set; }
        [JsonPropertyName("links")] public ActiveCampaignAccountLinks? Links { get; set; }
    }

    public sealed class ActiveCampaignAccountLinks
    {
        [JsonPropertyName("accountContacts")] public string? AccountContacts { get; set; }
        [JsonPropertyName("accountCustomFieldData")] public string? AccountCustomFieldData { get; set; }
        [JsonPropertyName("contactLists")] public string? ContactLists { get; set; }
        [JsonPropertyName("notes")] public string? Notes { get; set; }
        [JsonPropertyName("trackingLogs")] public string? TrackingLogs { get; set; }
    }

    // ---------- Automation objects ----------

    public sealed class ActiveCampaignAutomation : ActiveCampaignEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("entered")] public string? Entered { get; set; }
        [JsonPropertyName("exited")] public string? Exited { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("seriesid")] public string? SeriesId { get; set; }
        [JsonPropertyName("seriesname")] public string? SeriesName { get; set; }
        [JsonPropertyName("links")] public ActiveCampaignAutomationLinks? Links { get; set; }
    }

    public sealed class ActiveCampaignAutomationLinks
    {
        [JsonPropertyName("automationBlocks")] public string? AutomationBlocks { get; set; }
        [JsonPropertyName("automationContacts")] public string? AutomationContacts { get; set; }
        [JsonPropertyName("campaigns")] public string? Campaigns { get; set; }
        [JsonPropertyName("contactGoals")] public string? ContactGoals { get; set; }
        [JsonPropertyName("goals")] public string? Goals { get; set; }
        [JsonPropertyName("messages")] public string? Messages { get; set; }
    }

    // ---------- Segment objects ----------

    public sealed class ActiveCampaignSegment : ActiveCampaignEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("logic")] public string? Logic { get; set; }
        [JsonPropertyName("seriesid")] public string? SeriesId { get; set; }
        [JsonPropertyName("hidden")] public string? Hidden { get; set; }
        [JsonPropertyName("links")] public ActiveCampaignSegmentLinks? Links { get; set; }
    }

    public sealed class ActiveCampaignSegmentLinks
    {
        [JsonPropertyName("campaigns")] public string? Campaigns { get; set; }
        [JsonPropertyName("contacts")] public string? Contacts { get; set; }
        [JsonPropertyName("forms")] public string? Forms { get; set; }
        [JsonPropertyName("messages")] public string? Messages { get; set; }
    }

    // ---------- Form objects ----------

    public sealed class ActiveCampaignForm : ActiveCampaignEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("action")] public string? Action { get; set; }
        [JsonPropertyName("method")] public string? Method { get; set; }
        [JsonPropertyName("elements")] public string? Elements { get; set; }
        [JsonPropertyName("submit_text")] public string? SubmitText { get; set; }
        [JsonPropertyName("submit_value")] public string? SubmitValue { get; set; }
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("lists")] public List<string>? Lists { get; set; } = new();
        [JsonPropertyName("tags")] public List<string>? Tags { get; set; } = new();
        [JsonPropertyName("seriesid")] public string? SeriesId { get; set; }
        [JsonPropertyName("addressid")] public string? AddressId { get; set; }
        [JsonPropertyName("emailid")] public string? EmailId { get; set; }
        [JsonPropertyName("sdate")] public string? StartDate { get; set; }
        [JsonPropertyName("ldate")] public string? LastDate { get; set; }
        [JsonPropertyName("mdate")] public string? ModifiedDate { get; set; }
        [JsonPropertyName("links")] public ActiveCampaignFormLinks? Links { get; set; }
    }

    public sealed class ActiveCampaignFormLinks
    {
        [JsonPropertyName("campaigns")] public string? Campaigns { get; set; }
        [JsonPropertyName("lists")] public string? Lists { get; set; }
        [JsonPropertyName("site")] public string? Site { get; set; }
    }

    // ---------- User objects ----------

    public sealed class ActiveCampaignUser : ActiveCampaignEntityBase
    {
        [JsonPropertyName("username")] public string? Username { get; set; }
        [JsonPropertyName("firstName")] public string? FirstName { get; set; }
        [JsonPropertyName("lastName")] public string? LastName { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("phone")] public string? Phone { get; set; }
        [JsonPropertyName("signature")] public string? Signature { get; set; }
        [JsonPropertyName("links")] public ActiveCampaignUserLinks? Links { get; set; }
    }

    public sealed class ActiveCampaignUserLinks
    {
        [JsonPropertyName("lists")] public string? Lists { get; set; }
        [JsonPropertyName("campaigns")] public string? Campaigns { get; set; }
        [JsonPropertyName("forms")] public string? Forms { get; set; }
        [JsonPropertyName("segments")] public string? Segments { get; set; }
        [JsonPropertyName("automations")] public string? Automations { get; set; }
        [JsonPropertyName("templates")] public string? Templates { get; set; }
        [JsonPropertyName("dealGroups")] public string? DealGroups { get; set; }
        [JsonPropertyName("savedResponses")] public string? SavedResponses { get; set; }
        [JsonPropertyName("accounts")] public string? Accounts { get; set; }
        [JsonPropertyName("deals")] public string? Deals { get; set; }
    }

    // ---------- Webhook objects ----------

    public sealed class ActiveCampaignWebhook : ActiveCampaignEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("events")] public List<string>? Events { get; set; } = new();
        [JsonPropertyName("sources")] public List<string>? Sources { get; set; } = new();
        [JsonPropertyName("listid")] public string? ListId { get; set; }
        [JsonPropertyName("cdate")] public string? CreatedDate { get; set; }
        [JsonPropertyName("links")] public ActiveCampaignWebhookLinks? Links { get; set; }
    }

    public sealed class ActiveCampaignWebhookLinks
    {
        [JsonPropertyName("lists")] public string? Lists { get; set; }
    }

    // ---------- Field objects ----------

    public sealed class ActiveCampaignField : ActiveCampaignEntityBase
    {
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("descript")] public string? Description { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("isrequired")] public string? IsRequired { get; set; }
        [JsonPropertyName("perstag")] public string? PersonalizationTag { get; set; }
        [JsonPropertyName("defval")] public string? DefaultValue { get; set; }
        [JsonPropertyName("show_in_list")] public string? ShowInList { get; set; }
        [JsonPropertyName("rows")] public string? Rows { get; set; }
        [JsonPropertyName("cols")] public string? Columns { get; set; }
        [JsonPropertyName("visible")] public string? Visible { get; set; }
        [JsonPropertyName("service")] public string? Service { get; set; }
        [JsonPropertyName("ordernum")] public string? OrderNumber { get; set; }
        [JsonPropertyName("cdate")] public string? CreatedDate { get; set; }
        [JsonPropertyName("udate")] public string? UpdatedDate { get; set; }
        [JsonPropertyName("links")] public ActiveCampaignFieldLinks? Links { get; set; }
    }

    public sealed class ActiveCampaignFieldLinks
    {
        [JsonPropertyName("fieldRels")] public string? FieldRelations { get; set; }
        [JsonPropertyName("fieldValues")] public string? FieldValues { get; set; }
        [JsonPropertyName("lists")] public string? Lists { get; set; }
    }

    // ---------- Message objects ----------

    public sealed class ActiveCampaignMessage : ActiveCampaignEntityBase
    {
        [JsonPropertyName("subject")] public string? Subject { get; set; }
        [JsonPropertyName("fromemail")] public string? FromEmail { get; set; }
        [JsonPropertyName("fromname")] public string? FromName { get; set; }
        [JsonPropertyName("reply2")] public string? ReplyTo { get; set; }
        [JsonPropertyName("priority")] public string? Priority { get; set; }
        [JsonPropertyName("charset")] public string? Charset { get; set; }
        [JsonPropertyName("encoding")] public string? Encoding { get; set; }
        [JsonPropertyName("format")] public string? Format { get; set; }
        [JsonPropertyName("html")] public string? Html { get; set; }
        [JsonPropertyName("htmlfetch")] public string? HtmlFetch { get; set; }
        [JsonPropertyName("text")] public string? Text { get; set; }
        [JsonPropertyName("textfetch")] public string? TextFetch { get; set; }
        [JsonPropertyName("hidden")] public string? Hidden { get; set; }
        [JsonPropertyName("copy_to_master")] public string? CopyToMaster { get; set; }
        [JsonPropertyName("links")] public ActiveCampaignMessageLinks? Links { get; set; }
    }

    public sealed class ActiveCampaignMessageLinks
    {
        [JsonPropertyName("campaigns")] public string? Campaigns { get; set; }
        [JsonPropertyName("lists")] public string? Lists { get; set; }
        [JsonPropertyName("automationBlocks")] public string? AutomationBlocks { get; set; }
    }

    // ---------- Saved Response objects ----------

    public sealed class ActiveCampaignSavedResponse : ActiveCampaignEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("subject")] public string? Subject { get; set; }
        [JsonPropertyName("body")] public string? Body { get; set; }
        [JsonPropertyName("links")] public ActiveCampaignSavedResponseLinks? Links { get; set; }
    }

    public sealed class ActiveCampaignSavedResponseLinks
    {
        [JsonPropertyName("user")] public string? User { get; set; }
    }

    // ---------- Site & Tracking objects ----------

    public sealed class ActiveCampaignSite
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("account_id")] public string? AccountId { get; set; }
        [JsonPropertyName("cdate")] public string? CreatedDate { get; set; }
        [JsonPropertyName("mdate")] public string? ModifiedDate { get; set; }
        [JsonPropertyName("links")] public ActiveCampaignSiteLinks? Links { get; set; }
    }

    public sealed class ActiveCampaignSiteLinks
    {
        [JsonPropertyName("forms")] public string? Forms { get; set; }
        [JsonPropertyName("siteMessages")] public string? SiteMessages { get; set; }
        [JsonPropertyName("trackingLogs")] public string? TrackingLogs { get; set; }
    }

    public sealed class ActiveCampaignTracking
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("subscriberid")] public string? SubscriberId { get; set; }
        [JsonPropertyName("listid")] public string? ListId { get; set; }
        [JsonPropertyName("campaignid")] public string? CampaignId { get; set; }
        [JsonPropertyName("messageid")] public string? MessageId { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("seriesid")] public string? SeriesId { get; set; }
        [JsonPropertyName("responded")] public string? Responded { get; set; }
        [JsonPropertyName("unsubscribed")] public string? Unsubscribed { get; set; }
        [JsonPropertyName("bounced")] public string? Bounced { get; set; }
        [JsonPropertyName("ip")] public string? Ip { get; set; }
        [JsonPropertyName("ua")] public string? UserAgent { get; set; }
        [JsonPropertyName("hash")] public string? Hash { get; set; }
        [JsonPropertyName("tstamp")] public string? Timestamp { get; set; }
        [JsonPropertyName("links")] public ActiveCampaignTrackingLinks? Links { get; set; }
    }

    public sealed class ActiveCampaignTrackingLinks
    {
        [JsonPropertyName("subscriber")] public string? Subscriber { get; set; }
        [JsonPropertyName("list")] public string? List { get; set; }
        [JsonPropertyName("campaign")] public string? Campaign { get; set; }
        [JsonPropertyName("message")] public string? Message { get; set; }
        [JsonPropertyName("automation")] public string? Automation { get; set; }
        [JsonPropertyName("series")] public string? Series { get; set; }
    }

    // ---------- Report objects ----------

    public sealed class ActiveCampaignReport
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("cdate")] public string? CreatedDate { get; set; }
        [JsonPropertyName("mdate")] public string? ModifiedDate { get; set; }
        [JsonPropertyName("links")] public ActiveCampaignReportLinks? Links { get; set; }
    }

    public sealed class ActiveCampaignReportLinks
    {
        [JsonPropertyName("reportData")] public string? ReportData { get; set; }
    }
}