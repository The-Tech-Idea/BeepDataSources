using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.WebAPI;
using TheTechIdea.Beep.Connectors.Jotform.Models;

namespace TheTechIdea.Beep.Connectors.Jotform
{
    /// <summary>
    /// Jotform data source implementation using WebAPIDataSource as base class
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Jotform)]
    public class JotformDataSource : WebAPIDataSource
    {
        // Entity endpoints mapping for Jotform API
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // Forms
            ["forms"] = "user/forms",
            ["forms.get"] = "form/{id}",
            // Submissions
            ["submissions"] = "user/submissions",
            ["submissions.get"] = "submission/{id}",
            ["forms.submissions"] = "form/{form_id}/submissions"
        };

        // Required filters for each entity
        private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            ["forms.get"] = new[] { "id" },
            ["submissions.get"] = new[] { "id" },
            ["forms.submissions"] = new[] { "form_id" }
        };

        public JotformDataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor dmeEditor,
            DataSourceType databasetype,
            IErrorsInfo errorObject)
            : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
        {
            // Ensure WebAPI connection props exist
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
            {
                if (Dataconnection != null)
                    Dataconnection.ConnectionProp = new WebAPIConnectionProperties();
            }

            // Register entities
            EntitiesNames = EntityEndpoints.Keys.ToList();
            Entities = EntitiesNames
                .Select(n => new EntityStructure { EntityName = n, DatasourceEntityName = n })
                .ToList();
        }

        // Return the fixed list
        public new IEnumerable<string> GetEntitesList() => EntitiesNames;

        // -------------------- Overrides (same signatures) --------------------

        // Sync
        public override IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            var data = GetEntityAsync(EntityName, filter).ConfigureAwait(false).GetAwaiter().GetResult();
            return data ?? Array.Empty<object>();
        }

        // Async
        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            if (!EntityEndpoints.TryGetValue(EntityName, out var endpoint))
                throw new InvalidOperationException($"Unknown Jotform entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            // Build the full URL
            var url = $"https://api.jotform.com/v1/{endpoint}";
            url = ReplacePlaceholders(url, q);

            // Add query parameters
            var queryParams = BuildQueryParameters(q);
            if (!string.IsNullOrEmpty(queryParams))
                url += "?" + queryParams;

            // Make the request
            var response = await GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            // Parse based on entity
            return EntityName switch
            {
                "forms" => ParseForms(json),
                "forms.get" => ParseForm(json),
                "submissions" => ParseSubmissions(json),
                "submissions.get" => ParseSubmission(json),
                "forms.submissions" => ParseSubmissions(json),
                _ => throw new NotSupportedException($"Entity '{EntityName}' parsing not implemented.")
            };
        }

        // Helper methods for parsing
        private IEnumerable<object> ParseForms(string json)
        {
            var response = JsonSerializer.Deserialize<JotformFormsResponse>(json);
            return response?.Content ?? new List<JotformForm>();
        }

        private IEnumerable<object> ParseForm(string json)
        {
            var response = JsonSerializer.Deserialize<JotformFormResponse>(json);
            return response?.Content != null ? new[] { response.Content } : Array.Empty<JotformFormDetail>();
        }

        private IEnumerable<object> ParseSubmissions(string json)
        {
            var response = JsonSerializer.Deserialize<JotformSubmissionsResponse>(json);
            return response?.Content ?? new List<JotformSubmission>();
        }

        private IEnumerable<object> ParseSubmission(string json)
        {
            var response = JsonSerializer.Deserialize<JotformSubmissionResponse>(json);
            return response?.Content != null ? new[] { response.Content } : Array.Empty<JotformSubmission>();
        }

        // Helper methods
        private static Dictionary<string, string> FiltersToQuery(List<AppFilter> filters)
        {
            var q = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (filters == null) return q;
            foreach (var f in filters)
            {
                if (f == null || string.IsNullOrWhiteSpace(f.FieldName)) continue;
                q[f.FieldName.Trim()] = f.FilterValue?.ToString() ?? string.Empty;
            }
            return q;
        }

        private static void RequireFilters(string entity, Dictionary<string, string> q, string[] required)
        {
            if (required == null || required.Length == 0) return;
            var missing = required.Where(r => !q.ContainsKey(r) || string.IsNullOrWhiteSpace(q[r])).ToList();
            if (missing.Count > 0)
                throw new ArgumentException($"Jotform entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ReplacePlaceholders(string url, Dictionary<string, string> q)
        {
            // Substitute {id} and {form_id} from filters if present
            if (url.Contains("{id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("id", out var id) || string.IsNullOrWhiteSpace(id))
                    throw new ArgumentException("Missing required 'id' filter for this endpoint.");
                url = url.Replace("{id}", Uri.EscapeDataString(id));
            }
            if (url.Contains("{form_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("form_id", out var formId) || string.IsNullOrWhiteSpace(formId))
                    throw new ArgumentException("Missing required 'form_id' filter for this endpoint.");
                url = url.Replace("{form_id}", Uri.EscapeDataString(formId));
            }
            return url;
        }

        private static string BuildQueryParameters(Dictionary<string, string> q)
        {
            var query = new List<string>();
            foreach (var kvp in q)
            {
                if (!string.IsNullOrWhiteSpace(kvp.Value) && !kvp.Key.Contains("{") && !kvp.Key.Contains("}"))
                    query.Add($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}");
            }
            return string.Join("&", query);
        }

        // -------------------- CommandAttribute Methods --------------------

        [CommandAttribute(
            ObjectType = "JotformForm",
            PointType = EnumPointType.Function,
            Name = "GetForms",
            Caption = "Get Jotform Forms",
            ClassName = "JotformDataSource",
            misc = "ReturnType: IEnumerable<JotformForm>"
        )]
        public IEnumerable<JotformForm> GetForms()
        {
            return GetEntity<JotformForm>("forms");
        }

        [CommandAttribute(
            ObjectType = "JotformForm",
            PointType = EnumPointType.Function,
            Name = "GetForm",
            Caption = "Get Jotform Form by ID",
            ClassName = "JotformDataSource",
            misc = "ReturnType: IEnumerable<JotformForm>"
        )]
        public IEnumerable<JotformForm> GetForm(string id)
        {
            return GetEntity<JotformForm>("forms.get", new List<AppFilter> { new AppFilter { FieldName = "id", FilterValue = id } });
        }

        [CommandAttribute(
            ObjectType = "JotformSubmission",
            PointType = EnumPointType.Function,
            Name = "GetSubmissions",
            Caption = "Get Jotform Submissions",
            ClassName = "JotformDataSource",
            misc = "ReturnType: IEnumerable<JotformSubmission>"
        )]
        public IEnumerable<JotformSubmission> GetSubmissions()
        {
            return GetEntity<JotformSubmission>("submissions");
        }

        [CommandAttribute(
            ObjectType = "JotformSubmission",
            PointType = EnumPointType.Function,
            Name = "GetSubmission",
            Caption = "Get Jotform Submission by ID",
            ClassName = "JotformDataSource",
            misc = "ReturnType: IEnumerable<JotformSubmission>"
        )]
        public IEnumerable<JotformSubmission> GetSubmission(string id)
        {
            return GetEntity<JotformSubmission>("submissions.get", new List<AppFilter> { new AppFilter { FieldName = "id", FilterValue = id } });
        }

        [CommandAttribute(
            ObjectType = "JotformSubmission",
            PointType = EnumPointType.Function,
            Name = "GetFormSubmissions",
            Caption = "Get Jotform Submissions for a Form",
            ClassName = "JotformDataSource",
            misc = "ReturnType: IEnumerable<JotformSubmission>"
        )]
        public IEnumerable<JotformSubmission> GetFormSubmissions(string formId)
        {
            return GetEntity<JotformSubmission>("forms.submissions", new List<AppFilter> { new AppFilter { FieldName = "form_id", FilterValue = formId } });
        }
    }
}