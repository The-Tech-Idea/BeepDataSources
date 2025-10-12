// File: Connectors/iCloud/Models/iCloudModels.cs
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.iCloud.Models
{
    // -------------------------------------------------------
    // Base
    // -------------------------------------------------------
    public abstract class iCloudEntityBase
    {
        [JsonIgnore] public IDataSource? DataSource { get; set; }
        public T Attach<T>(IDataSource ds) where T : iCloudEntityBase { DataSource = ds; return (T)this; }
    }

    // -------------------------------------------------------
    // File
    // -------------------------------------------------------
    public sealed class iCloudFile : iCloudEntityBase
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("size")] public long? Size { get; set; }
        [JsonPropertyName("dateCreated")] public DateTime? DateCreated { get; set; }
        [JsonPropertyName("dateModified")] public DateTime? DateModified { get; set; }
        [JsonPropertyName("etag")] public string? ETag { get; set; }
        [JsonPropertyName("extension")] public string? Extension { get; set; }
    }

    // -------------------------------------------------------
    // Folder
    // -------------------------------------------------------
    public sealed class iCloudFolder : iCloudEntityBase
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("dateCreated")] public DateTime? DateCreated { get; set; }
        [JsonPropertyName("dateModified")] public DateTime? DateModified { get; set; }
        [JsonPropertyName("childCount")] public int? ChildCount { get; set; }
        [JsonPropertyName("parentId")] public string? ParentId { get; set; }
    }

    // -------------------------------------------------------
    // Share
    // -------------------------------------------------------
    public sealed class iCloudShare : iCloudEntityBase
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("owner")] public string? Owner { get; set; }
        [JsonPropertyName("permissions")] public string? Permissions { get; set; }
        [JsonPropertyName("dateShared")] public DateTime? DateShared { get; set; }
        [JsonPropertyName("expirationDate")] public DateTime? ExpirationDate { get; set; }
    }

    // -------------------------------------------------------
    // Device
    // -------------------------------------------------------
    public sealed class iCloudDevice : iCloudEntityBase
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("model")] public string? Model { get; set; }
        [JsonPropertyName("osVersion")] public string? OsVersion { get; set; }
        [JsonPropertyName("lastSeen")] public DateTime? LastSeen { get; set; }
        [JsonPropertyName("isActive")] public bool? IsActive { get; set; }
        [JsonPropertyName("deviceClass")] public string? DeviceClass { get; set; }
    }
}