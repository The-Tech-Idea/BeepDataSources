using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Communication.Zoom.Models
{
    public abstract class ZoomEntityBase
    {
        [JsonIgnore] public IDataSource? DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : ZoomEntityBase { DataSource = ds; return (T)this; }
    }

    // Zoom Models
    public sealed class ZoomUser : ZoomEntityBase
    {
        public string? Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public int Type { get; set; }
        public string? Pmi { get; set; }
        public string? Timezone { get; set; }
        public int Verified { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastLoginTime { get; set; }
        public string? Language { get; set; }
        public string? Status { get; set; }
        public string? RoleId { get; set; }
    }

    public sealed class ZoomMeeting : ZoomEntityBase
    {
        public string? Uuid { get; set; }
        public long Id { get; set; }
        public string? HostId { get; set; }
        public string? Topic { get; set; }
        public int Type { get; set; }
        public DateTime StartTime { get; set; }
        public int Duration { get; set; }
        public string? Timezone { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? JoinUrl { get; set; }
        public string? Password { get; set; }
        public string? Agenda { get; set; }
        public List<ZoomTrackingField>? TrackingFields { get; set; }
        public ZoomRecurrence? Recurrence { get; set; }
        public ZoomSettings? Settings { get; set; }
    }

    public sealed class ZoomWebinar : ZoomEntityBase
    {
        public string? Uuid { get; set; }
        public long Id { get; set; }
        public string? HostId { get; set; }
        public string? Topic { get; set; }
        public int Type { get; set; }
        public DateTime StartTime { get; set; }
        public int Duration { get; set; }
        public string? Timezone { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? JoinUrl { get; set; }
        public string? Password { get; set; }
        public string? Agenda { get; set; }
        public ZoomSettings? Settings { get; set; }
    }

    public sealed class ZoomChannel : ZoomEntityBase
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
    }

    public sealed class ZoomChannelMessage : ZoomEntityBase
    {
        public string? MessageId { get; set; }
        public string? Sender { get; set; }
        public DateTime DateTime { get; set; }
        public string? Message { get; set; }
    }

    // Missing model classes for Zoom Map entities
    public sealed class ZoomMeetingParticipant : ZoomEntityBase
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? UserId { get; set; }
        public string? UserEmail { get; set; }
        public string? JoinTime { get; set; }
        public string? LeaveTime { get; set; }
        public string? Duration { get; set; }
        public string? AttentivenessScore { get; set; }
    }

    public sealed class ZoomMeetingRecording : ZoomEntityBase
    {
        public string? Id { get; set; }
        public string? MeetingId { get; set; }
        public string? RecordingStart { get; set; }
        public string? RecordingEnd { get; set; }
        public List<ZoomRecordingFile>? RecordingFiles { get; set; }
        public string? RecordingCount { get; set; }
        public string? TotalSize { get; set; }
    }

    public sealed class ZoomWebinarParticipant : ZoomEntityBase
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? UserId { get; set; }
        public string? UserEmail { get; set; }
        public string? JoinTime { get; set; }
        public string? LeaveTime { get; set; }
        public string? Duration { get; set; }
        public string? AttentivenessScore { get; set; }
    }

    public sealed class ZoomWebinarRecording : ZoomEntityBase
    {
        public string? Id { get; set; }
        public string? WebinarId { get; set; }
        public string? RecordingStart { get; set; }
        public string? RecordingEnd { get; set; }
        public List<ZoomRecordingFile>? RecordingFiles { get; set; }
        public string? RecordingCount { get; set; }
        public string? TotalSize { get; set; }
    }

    public sealed class ZoomGroup : ZoomEntityBase
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public int TotalMembers { get; set; }
    }

    public sealed class ZoomGroupMember : ZoomEntityBase
    {
        public string? Id { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Type { get; set; }
        public bool Pmi { get; set; }
        public string? Timezone { get; set; }
        public string? Dept { get; set; }
        public string? CreatedAt { get; set; }
        public string? LastLoginTime { get; set; }
        public string? LastClientVersion { get; set; }
        public string? Language { get; set; }
        public string? Status { get; set; }
        public string? RoleId { get; set; }
        public string? EmployeeUniqueId { get; set; }
        public string? GroupIds { get; set; }
        public string? ImGroupIds { get; set; }
        public string? SamlIdentity { get; set; }
    }

    public sealed class ZoomAccountSettings : ZoomEntityBase
    {
        public ZoomFeatureSettings? Feature { get; set; }
        public ZoomInMeetingSettings? InMeeting { get; set; }
        public ZoomEmailNotificationSettings? EmailNotification { get; set; }
        public ZoomRecordingSettings? Recording { get; set; }
        public ZoomTelephonySettings? Telephony { get; set; }
        public ZoomTspSettings? Tsp { get; set; }
        public ZoomSecuritySettings? Security { get; set; }
    }

    public sealed class ZoomUserSettings : ZoomEntityBase
    {
        public ZoomFeatureSettings? Feature { get; set; }
        public ZoomInMeetingSettings? InMeeting { get; set; }
        public ZoomEmailNotificationSettings? EmailNotification { get; set; }
        public ZoomRecordingSettings? Recording { get; set; }
        public ZoomTelephonySettings? Telephony { get; set; }
        public ZoomTspSettings? Tsp { get; set; }
        public ZoomSecuritySettings? Security { get; set; }
        public ZoomScheduleMeetingSettings? ScheduleMeeting { get; set; }
    }

    // Supporting classes for Zoom
    public sealed class ZoomTrackingField : ZoomEntityBase { public string? Field { get; set; } public string? Value { get; set; } }
    public sealed class ZoomRecurrence : ZoomEntityBase { public int Type { get; set; } public int RepeatInterval { get; set; } public string? WeeklyDays { get; set; } public int MonthlyDay { get; set; } public int MonthlyWeek { get; set; } public int MonthlyWeekDay { get; set; } public int EndTimes { get; set; } public DateTime EndDateTime { get; set; } }
    public sealed class ZoomSettings : ZoomEntityBase { public bool HostVideo { get; set; } public bool ParticipantVideo { get; set; } public bool CnMeeting { get; set; } public bool InMeeting { get; set; } public bool JoinBeforeHost { get; set; } public bool MuteUponEntry { get; set; } public bool Watermark { get; set; } public bool UsePmi { get; set; } public int ApprovalType { get; set; } public string? Audio { get; set; } public string? AutoRecording { get; set; } public bool EnforceLogin { get; set; } public string? EnforceLoginDomains { get; set; } public List<string>? AlternativeHosts { get; set; } public bool CloseRegistration { get; set; } public bool ShowShareButton { get; set; } public bool AllowMultipleDevices { get; set; } public bool RegistrantsConfirmationEmail { get; set; } public bool WaitingRoom { get; set; } public bool RequestPermissionToUnmute { get; set; } public bool RegistrantsEmailNotification { get; set; } public bool MeetingAuthentication { get; set; } public string? EncryptionType { get; set; } public bool ApprovedOrDeniedCountriesOrRegions { get; set; } public ZoomBreakoutRoom? BreakoutRoom { get; set; } }
    public sealed class ZoomBreakoutRoom : ZoomEntityBase { public bool Enable { get; set; } public List<ZoomBreakoutRoomRoom>? Rooms { get; set; } }
    public sealed class ZoomBreakoutRoomRoom : ZoomEntityBase { public string? Name { get; set; } public List<string>? Participants { get; set; } }
    public class ZoomRecordingFile
    {
        public string? Id { get; set; }
        public string? MeetingId { get; set; }
        public string? RecordingStart { get; set; }
        public string? RecordingEnd { get; set; }
        public string? FileType { get; set; }
        public string? FileSize { get; set; }
        public string? PlayUrl { get; set; }
        public string? DownloadUrl { get; set; }
        public string? Status { get; set; }
        public string? RecordingType { get; set; }
    }

    public sealed class ZoomFeatureSettings : ZoomEntityBase
    {
        public bool ZoomRooms { get; set; }
        public bool ZoomWhiteboard { get; set; }
        public bool ZoomTranslatedCaptions { get; set; }
        public bool ZoomApps { get; set; }
    }

    public sealed class ZoomInMeetingSettings : ZoomEntityBase
    {
        public bool E2eEncryption { get; set; }
        public bool Chat { get; set; }
        public bool PrivateChat { get; set; }
        public bool AutoSavingChat { get; set; }
        public bool FileTransfer { get; set; }
        public bool Feedback { get; set; }
        public bool PostMeetingFeedback { get; set; }
        public bool CoHost { get; set; }
        public bool Polling { get; set; }
        public bool AttendeeOnHold { get; set; }
        public bool Annotation { get; set; }
        public bool RemoteControl { get; set; }
        public bool NonVerbalFeedback { get; set; }
        public bool BreakoutRoom { get; set; }
        public bool RemoteSupport { get; set; }
        public bool ClosedCaptioning { get; set; }
        public bool GroupHd { get; set; }
        public bool VirtualBackground { get; set; }
        public bool FarEndCameraControl { get; set; }
        public bool ShareDualCamera { get; set; }
        public bool AttentionTracking { get; set; }
        public bool WaitingRoom { get; set; }
        public bool AllowParticipantsToRename { get; set; }
        public bool WebinarChat { get; set; }
        public bool WebinarLiveStreaming { get; set; }
        public bool WebinarQuestionAnswer { get; set; }
        public bool WebinarPolling { get; set; }
        public bool WebinarSurvey { get; set; }
    }

    public sealed class ZoomEmailNotificationSettings : ZoomEntityBase
    {
        public bool CloudRecordingAvailableReminder { get; set; }
        public bool JbhReminder { get; set; }
        public bool CancelMeetingReminder { get; set; }
        public bool AlternativeHostReminder { get; set; }
        public bool ScheduleForReminder { get; set; }
    }

    public sealed class ZoomRecordingSettings : ZoomEntityBase
    {
        public bool LocalRecording { get; set; }
        public bool CloudRecording { get; set; }
        public bool RecordSpeakerView { get; set; }
        public bool RecordGalleryView { get; set; }
        public bool RecordAudioFile { get; set; }
        public bool SaveChatText { get; set; }
        public bool ShowTimestamp { get; set; }
        public bool RecordingAudioTranscript { get; set; }
        public bool AutoRecording { get; set; }
        public bool HostPauseStopRecording { get; set; }
        public bool AutoDeleteCmr { get; set; }
        public int AutoDeleteCmrDays { get; set; }
    }

    public sealed class ZoomTelephonySettings : ZoomEntityBase
    {
        public bool ThirdPartyAudio { get; set; }
        public bool AudioConferenceInfo { get; set; }
        public bool ShowInternationalNumbersLink { get; set; }
        public bool TelephonyRegions { get; set; }
    }

    public sealed class ZoomTspSettings : ZoomEntityBase
    {
        public bool CallOut { get; set; }
        public bool CallOutCountries { get; set; }
        public bool ShowInternationalNumbersLink { get; set; }
    }

    public sealed class ZoomSecuritySettings : ZoomEntityBase
    {
        public bool AdminChangeNamePic { get; set; }
        public bool SigninWithApple { get; set; }
        public bool SigninWithGoogle { get; set; }
        public bool PasswordRequirement { get; set; }
        public bool SigninWithSso { get; set; }
        public bool MeetingPassword { get; set; }
        public bool WaitingRoom { get; set; }
        public bool WaitingRoomSettings { get; set; }
        public bool EmbedPasswordInJoinLink { get; set; }
        public bool PmiPassword { get; set; }
    }

    public sealed class ZoomScheduleMeetingSettings : ZoomEntityBase
    {
        public bool HostVideo { get; set; }
        public bool ParticipantsVideo { get; set; }
        public bool JoinBeforeHost { get; set; }
        public bool MuteUponEntry { get; set; }
        public bool Watermark { get; set; }
        public bool UsePmi { get; set; }
        public bool ApprovalType { get; set; }
        public bool Audio { get; set; }
        public bool AutoRecording { get; set; }
        public bool EnforceLogin { get; set; }
        public bool EnforceLoginDomains { get; set; }
        public bool AlternativeHosts { get; set; }
    }
}