using System.Text.Json.Serialization;

namespace TheTechIdea.Beep.Connectors.TLDV.Models
{
    // tl;dv API Models
    public class TLDVMeeting
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("duration")]
        public int Duration { get; set; }

        [JsonPropertyName("video_url")]
        public string VideoUrl { get; set; }

        [JsonPropertyName("recording_url")]
        public string RecordingUrl { get; set; }

        [JsonPropertyName("transcription_url")]
        public string TranscriptionUrl { get; set; }

        [JsonPropertyName("summary_url")]
        public string SummaryUrl { get; set; }

        [JsonPropertyName("chapters_url")]
        public string ChaptersUrl { get; set; }

        [JsonPropertyName("highlights_url")]
        public string HighlightsUrl { get; set; }

        [JsonPropertyName("participants")]
        public List<TLDVParticipant> Participants { get; set; }

        [JsonPropertyName("metadata")]
        public TLDVMetadata Metadata { get; set; }
    }

    public class TLDVParticipant
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("avatar_url")]
        public string AvatarUrl { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; }
    }

    public class TLDVMetadata
    {
        [JsonPropertyName("meeting_platform")]
        public string MeetingPlatform { get; set; }

        [JsonPropertyName("meeting_url")]
        public string MeetingUrl { get; set; }

        [JsonPropertyName("start_time")]
        public DateTime StartTime { get; set; }

        [JsonPropertyName("end_time")]
        public DateTime EndTime { get; set; }

        [JsonPropertyName("language")]
        public string Language { get; set; }

        [JsonPropertyName("timezone")]
        public string Timezone { get; set; }
    }

    public class TLDVTranscription
    {
        [JsonPropertyName("meeting_id")]
        public string MeetingId { get; set; }

        [JsonPropertyName("language")]
        public string Language { get; set; }

        [JsonPropertyName("utterances")]
        public List<TLDVUtterance> Utterances { get; set; }

        [JsonPropertyName("summary")]
        public TLDVSummary Summary { get; set; }
    }

    public class TLDVUtterance
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("speaker")]
        public string Speaker { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("start")]
        public double Start { get; set; }

        [JsonPropertyName("end")]
        public double End { get; set; }

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }

        [JsonPropertyName("words")]
        public List<TLDVWord> Words { get; set; }
    }

    public class TLDVWord
    {
        [JsonPropertyName("word")]
        public string Word { get; set; }

        [JsonPropertyName("start")]
        public double Start { get; set; }

        [JsonPropertyName("end")]
        public double End { get; set; }

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }
    }

    public class TLDVSummary
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("overview")]
        public string Overview { get; set; }

        [JsonPropertyName("key_points")]
        public List<string> KeyPoints { get; set; }

        [JsonPropertyName("action_items")]
        public List<string> ActionItems { get; set; }

        [JsonPropertyName("decisions")]
        public List<string> Decisions { get; set; }

        [JsonPropertyName("sentiment")]
        public string Sentiment { get; set; }
    }

    public class TLDVChapter
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("start")]
        public double Start { get; set; }

        [JsonPropertyName("end")]
        public double End { get; set; }

        [JsonPropertyName("summary")]
        public string Summary { get; set; }

        [JsonPropertyName("highlights")]
        public List<string> Highlights { get; set; }
    }

    public class TLDVHighlight
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("start")]
        public double Start { get; set; }

        [JsonPropertyName("end")]
        public double End { get; set; }

        [JsonPropertyName("speaker")]
        public string Speaker { get; set; }

        [JsonPropertyName("importance")]
        public double Importance { get; set; }
    }

    // Response models
    public class TLDVMeetingsResponse
    {
        [JsonPropertyName("meetings")]
        public List<TLDVMeeting> Meetings { get; set; }

        [JsonPropertyName("pagination")]
        public TLDVPagination Pagination { get; set; }
    }

    public class TLDVPagination
    {
        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("total_pages")]
        public int TotalPages { get; set; }
    }
}