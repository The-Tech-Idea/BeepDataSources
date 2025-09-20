using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Connectors.Communication.MicrosoftTeams.Models
{
    public class TeamsChannel
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public bool IsFavoriteByDefault { get; set; }
        public string? Email { get; set; }
        public string? WebUrl { get; set; }
        public TeamsChannelMembershipType MembershipType { get; set; }
    }

    public class TeamsMessage
    {
        public string? Id { get; set; }
        public string? ReplyToId { get; set; }
        public string? Etag { get; set; }
        public string? MessageType { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public DateTime LastModifiedDateTime { get; set; }
        public DateTime LastEditedDateTime { get; set; }
        public DateTime DeletedDateTime { get; set; }
        public string? Subject { get; set; }
        public string? Summary { get; set; }
        public string? Importance { get; set; }
        public string? Locale { get; set; }
        public string? WebUrl { get; set; }
        public TeamsFrom? From { get; set; }
        public TeamsBody? Body { get; set; }
        public TeamsChannelIdentity? ChannelIdentity { get; set; }
        public TeamsOnBehalfOf? OnBehalfOf { get; set; }
        public string? PolicyViolation { get; set; }
        public string? EventDetail { get; set; }
        public List<TeamsAttachment>? Attachments { get; set; }
        public List<TeamsMention>? Mentions { get; set; }
        public List<TeamsReaction>? Reactions { get; set; }
    }

    public class TeamsUser
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public string? UserIdentityType { get; set; }
    }

    // Supporting classes for Microsoft Teams
    public enum TeamsChannelMembershipType { Standard, Private, Shared, UnknownFutureValue }
    public class TeamsFrom { public TeamsUser? User { get; set; } public TeamsApplication? Application { get; set; } public TeamsDevice? Device { get; set; } }
    public class TeamsApplication { public string? Id { get; set; } public string? DisplayName { get; set; } public string? ApplicationIdentityType { get; set; } }
    public class TeamsDevice { public string? Id { get; set; } public string? DisplayName { get; set; } }
    public class TeamsBody { public string? ContentType { get; set; } public string? Content { get; set; } }
    public class TeamsChannelIdentity { public string? ChannelId { get; set; } public string? TeamId { get; set; } }
    public class TeamsOnBehalfOf { public TeamsUser? User { get; set; } public TeamsApplication? Application { get; set; } public TeamsDevice? Device { get; set; } }
    public class TeamsAttachment { public string? Id { get; set; } public string? ContentType { get; set; } public string? ContentUrl { get; set; } public string? Content { get; set; } public string? Name { get; set; } public string? ThumbnailUrl { get; set; } }
    public class TeamsMention { public int Id { get; set; } public string? MentionText { get; set; } public TeamsUser? Mentioned { get; set; } }
    public class TeamsReaction { public string? ReactionType { get; set; } public DateTime CreatedDateTime { get; set; } public TeamsUser? User { get; set; } }

    // Missing model classes for Microsoft Teams Map entities
    public class TeamsTeam
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public bool IsArchived { get; set; }
        public string? WebUrl { get; set; }
        public TeamsTeamVisibility Visibility { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public TeamsUser? CreatedBy { get; set; }
    }

    public class TeamsChannelTab
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public string? WebUrl { get; set; }
        public TeamsTabConfiguration? Configuration { get; set; }
        public List<string>? TeamsAppId { get; set; }
        public int SortOrderIndex { get; set; }
        public string? MessageId { get; set; }
    }

    public class TeamsTeamMember
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public string? UserId { get; set; }
        public string? Email { get; set; }
        public TeamsTeamMemberRole Roles { get; set; }
        public bool IsOwner { get; set; }
        public DateTime JoinedDateTime { get; set; }
    }

    public class TeamsTeamApp
    {
        public string? Id { get; set; }
        public TeamsApp? TeamsApp { get; set; }
        public TeamsAppDefinition? TeamsAppDefinition { get; set; }
    }

    public class TeamsChat
    {
        public string? Id { get; set; }
        public string? Topic { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public DateTime LastUpdatedDateTime { get; set; }
        public TeamsChatType ChatType { get; set; }
        public string? WebUrl { get; set; }
        public TeamsUser? CreatedBy { get; set; }
    }

    public class TeamsChatMember
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public string? UserId { get; set; }
        public string? Email { get; set; }
        public bool IsOwner { get; set; }
        public DateTime JoinedDateTime { get; set; }
        public TeamsChatMemberRole Roles { get; set; }
    }

    public class TeamsMe
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public string? UserPrincipalName { get; set; }
        public string? Mail { get; set; }
        public string? MobilePhone { get; set; }
        public string? OfficeLocation { get; set; }
        public string? PreferredLanguage { get; set; }
    }

    public class TeamsJoinedTeam
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public bool IsArchived { get; set; }
        public string? WebUrl { get; set; }
        public TeamsTeamVisibility Visibility { get; set; }
    }

    public class TeamsMeChat
    {
        public string? Id { get; set; }
        public string? Topic { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public DateTime LastUpdatedDateTime { get; set; }
        public TeamsChatType ChatType { get; set; }
        public string? WebUrl { get; set; }
    }

    public class TeamsApp
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public string? DistributionMethod { get; set; }
        public string? ExternalId { get; set; }
        public TeamsAppPublishingState PublishingState { get; set; }
        public bool IsGloballyEnabled { get; set; }
    }

    // Additional supporting classes
    public enum TeamsTeamVisibility { Private, Public, HiddenMembership, UnknownFutureValue }
    public enum TeamsTeamMemberRole { Owner, Member, Guest, UnknownFutureValue }
    public enum TeamsChatType { Group, OneOnOne, Meeting, UnknownFutureValue }
    public enum TeamsChatMemberRole { Owner, Member, Guest, UnknownFutureValue }
    public enum TeamsAppPublishingState { Submitted, Rejected, Published, UnknownFutureValue }

    public class TeamsTabConfiguration
    {
        public string? EntityId { get; set; }
        public string? ContentUrl { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? RemoveUrl { get; set; }
    }

    public class TeamsAppDefinition
    {
        public string? TeamsAppId { get; set; }
        public string? DisplayName { get; set; }
        public string? Version { get; set; }
        public TeamsAppPublishingState PublishingState { get; set; }
    }
}