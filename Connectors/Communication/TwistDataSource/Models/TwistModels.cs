using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Connectors.Communication.Twist.Models
{
    public class TwistWorkspace
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public string? Desc { get; set; }
        public string? Color { get; set; }
        public DateTime? CreatedTs { get; set; }
        public TwistUser? Creator { get; set; }
        public List<TwistUser>? Users { get; set; } = new();
        public List<TwistChannel>? Channels { get; set; } = new();
        public List<TwistGroup>? Groups { get; set; } = new();
    }

    public class TwistWorkspaceUser
    {
        public int? WorkspaceId { get; set; }
        public TwistUser? User { get; set; }
        public string? Role { get; set; }
        public DateTime? JoinedTs { get; set; }
    }

    public class TwistWorkspaceGroup
    {
        public int? WorkspaceId { get; set; }
        public TwistGroup? Group { get; set; }
        public DateTime? AddedTs { get; set; }
    }

    public class TwistChannel
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public string? Desc { get; set; }
        public TwistUser? Creator { get; set; }
        public DateTime? CreatedTs { get; set; }
        public List<TwistUser>? Users { get; set; } = new();
    }

    public class TwistChannelUser
    {
        public int? ChannelId { get; set; }
        public TwistUser? User { get; set; }
        public string? Role { get; set; }
        public DateTime? JoinedTs { get; set; }
    }

    public class TwistThread
    {
        public int? Id { get; set; }
        public string? Title { get; set; }
        public TwistUser? Creator { get; set; }
        public DateTime? CreatedTs { get; set; }
        public DateTime? LastTs { get; set; }
        public int? PostsCount { get; set; }
        public int? ParticipantsCount { get; set; }
        public bool? IsPrivate { get; set; }
        public bool? IsArchived { get; set; }
    }

    public class TwistMessage
    {
        public int? Id { get; set; }
        public string? Content { get; set; }
        public TwistUser? Sender { get; set; }
        public DateTime? SentTs { get; set; }
        public List<TwistAttachment>? Attachments { get; set; } = new();
        public List<TwistReaction>? Reactions { get; set; } = new();
        public bool? IsEdited { get; set; }
        public DateTime? EditedTs { get; set; }
    }

    public class TwistMessageReaction
    {
        public int? MessageId { get; set; }
        public string? Emoji { get; set; }
        public TwistUser? User { get; set; }
        public DateTime? ReactedTs { get; set; }
    }

    public class TwistComment
    {
        public int? Id { get; set; }
        public string? Content { get; set; }
        public TwistUser? Author { get; set; }
        public DateTime? CreatedTs { get; set; }
        public int? ThreadId { get; set; }
        public int? MessageId { get; set; }
        public bool? IsEdited { get; set; }
        public DateTime? EditedTs { get; set; }
    }

    public class TwistUser
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Timezone { get; set; }
        public bool? IsBot { get; set; }
        public TwistUserProfile? Profile { get; set; }
    }

    public class TwistUserWorkspace
    {
        public int? UserId { get; set; }
        public TwistWorkspace? Workspace { get; set; }
        public string? Role { get; set; }
        public DateTime? JoinedTs { get; set; }
    }

    public class TwistGroup
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public string? Desc { get; set; }
        public TwistUser? Creator { get; set; }
        public DateTime? CreatedTs { get; set; }
        public List<TwistUser>? Users { get; set; } = new();
    }

    public class TwistGroupUser
    {
        public int? GroupId { get; set; }
        public TwistUser? User { get; set; }
        public string? Role { get; set; }
        public DateTime? JoinedTs { get; set; }
    }

    public class TwistIntegration
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Description { get; set; }
        public TwistUser? Creator { get; set; }
        public DateTime? CreatedTs { get; set; }
        public bool? IsActive { get; set; }
        public string? Config { get; set; }
    }

    public class TwistAttachment
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public long? Size { get; set; }
        public string? Url { get; set; }
        public TwistUser? Uploader { get; set; }
        public DateTime? UploadedTs { get; set; }
        public int? MessageId { get; set; }
    }

    public class TwistNotification
    {
        public int? Id { get; set; }
        public string? Type { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public TwistUser? Recipient { get; set; }
        public DateTime? CreatedTs { get; set; }
        public bool? IsRead { get; set; }
        public string? ActionUrl { get; set; }
    }

    // Supporting classes for Twist
    public class TwistUserProfile
    {
        public string? Name { get; set; }
        public string? Bio { get; set; }
        public string? Image { get; set; }
    }

    public class TwistReaction
    {
        public string? Emoji { get; set; }
        public TwistUser? User { get; set; }
        public DateTime? ReactedTs { get; set; }
    }
}