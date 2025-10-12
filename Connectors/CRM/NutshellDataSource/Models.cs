// File: Connectors/CRM/NutshellDataSource/Models.cs
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Nutshell.Models
{
    // Common base for Nutshell CRM entities
    public abstract class NutshellEntityBase
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("createdTime")] public DateTime? CreatedTime { get; set; }
        [JsonPropertyName("modifiedTime")] public DateTime? ModifiedTime { get; set; }

        // Optional: Let POCOs know their datasource after hydration
        [JsonIgnore] public IDataSource? DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : NutshellEntityBase { DataSource = ds; return (T)this; }
    }

    // ---- Core CRM Entities ----

    public sealed class Contact : NutshellEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("firstName")] public string? FirstName { get; set; }
        [JsonPropertyName("lastName")] public string? LastName { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("phone")] public string? Phone { get; set; }
        [JsonPropertyName("jobTitle")] public string? JobTitle { get; set; }
        [JsonPropertyName("account")] public string? Account { get; set; }
    }

    public sealed class Account : NutshellEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("industry")] public string? Industry { get; set; }
        [JsonPropertyName("phone")] public string? Phone { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("website")] public string? Website { get; set; }
    }

    public sealed class Lead : NutshellEntityBase
    {
        [JsonPropertyName("firstName")] public string? FirstName { get; set; }
        [JsonPropertyName("lastName")] public string? LastName { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("company")] public string? Company { get; set; }
        [JsonPropertyName("jobTitle")] public string? JobTitle { get; set; }
        [JsonPropertyName("phone")] public string? Phone { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
    }

    public sealed class Opportunity : NutshellEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("account")] public string? Account { get; set; }
        [JsonPropertyName("contact")] public string? Contact { get; set; }
        [JsonPropertyName("value")] public decimal? Value { get; set; }
        [JsonPropertyName("currency")] public string? Currency { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
    }

    // Response wrapper for Nutshell API responses
    public class NutshellResponse<T>
    {
        [JsonPropertyName("result")] public List<T> Result { get; set; } = new();
        [JsonPropertyName("error")] public string? Error { get; set; }
    }
}