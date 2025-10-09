using System.Text.Json.Serialization;

namespace TheTechIdea.Beep.Connectors.AnyDo.Models
{
    // Any.do API Models
    public class AnyDoTask
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("priority")]
        public string Priority { get; set; }

        [JsonPropertyName("dueDate")]
        public DateTime? DueDate { get; set; }

        [JsonPropertyName("createdDate")]
        public DateTime CreatedDate { get; set; }

        [JsonPropertyName("updatedDate")]
        public DateTime UpdatedDate { get; set; }

        [JsonPropertyName("categoryId")]
        public string CategoryId { get; set; }

        [JsonPropertyName("listId")]
        public string ListId { get; set; }

        [JsonPropertyName("parentGlobalTaskId")]
        public string ParentGlobalTaskId { get; set; }

        [JsonPropertyName("note")]
        public string Note { get; set; }

        [JsonPropertyName("repeatingOptions")]
        public AnyDoRepeatingOptions RepeatingOptions { get; set; }

        [JsonPropertyName("reminders")]
        public List<AnyDoReminder> Reminders { get; set; }

        [JsonPropertyName("attachments")]
        public List<AnyDoAttachment> Attachments { get; set; }

        [JsonPropertyName("subTasks")]
        public List<AnyDoSubTask> SubTasks { get; set; }

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; }

        [JsonPropertyName("sharedWith")]
        public List<AnyDoSharedUser> SharedWith { get; set; }
    }

    public class AnyDoRepeatingOptions
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("interval")]
        public int Interval { get; set; }

        [JsonPropertyName("endDate")]
        public DateTime? EndDate { get; set; }

        [JsonPropertyName("daysOfWeek")]
        public List<int> DaysOfWeek { get; set; }
    }

    public class AnyDoReminder
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("minutesBefore")]
        public int MinutesBefore { get; set; }
    }

    public class AnyDoAttachment
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }
    }

    public class AnyDoSubTask
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("order")]
        public int Order { get; set; }
    }

    public class AnyDoSharedUser
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; }
    }

    public class AnyDoList
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("color")]
        public string Color { get; set; }

        [JsonPropertyName("icon")]
        public string Icon { get; set; }

        [JsonPropertyName("isShared")]
        public bool IsShared { get; set; }

        [JsonPropertyName("createdDate")]
        public DateTime CreatedDate { get; set; }

        [JsonPropertyName("updatedDate")]
        public DateTime UpdatedDate { get; set; }

        [JsonPropertyName("default")]
        public bool Default { get; set; }

        [JsonPropertyName("sharedWith")]
        public List<AnyDoSharedUser> SharedWith { get; set; }

        [JsonPropertyName("taskCount")]
        public int TaskCount { get; set; }
    }

    public class AnyDoCategory
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("color")]
        public string Color { get; set; }

        [JsonPropertyName("icon")]
        public string Icon { get; set; }

        [JsonPropertyName("createdDate")]
        public DateTime CreatedDate { get; set; }

        [JsonPropertyName("updatedDate")]
        public DateTime UpdatedDate { get; set; }

        [JsonPropertyName("default")]
        public bool Default { get; set; }
    }

    public class AnyDoUser
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("avatar")]
        public string Avatar { get; set; }

        [JsonPropertyName("timezone")]
        public string Timezone { get; set; }

        [JsonPropertyName("locale")]
        public string Locale { get; set; }

        [JsonPropertyName("createdDate")]
        public DateTime CreatedDate { get; set; }

        [JsonPropertyName("premium")]
        public bool Premium { get; set; }
    }

    // Response models
    public class AnyDoTasksResponse
    {
        [JsonPropertyName("tasks")]
        public List<AnyDoTask> Tasks { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("offset")]
        public int Offset { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }
    }

    public class AnyDoListsResponse
    {
        [JsonPropertyName("lists")]
        public List<AnyDoList> Lists { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }
    }

    public class AnyDoCategoriesResponse
    {
        [JsonPropertyName("categories")]
        public List<AnyDoCategory> Categories { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }
    }
}