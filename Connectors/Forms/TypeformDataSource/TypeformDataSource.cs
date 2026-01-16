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
using TheTechIdea.Beep.Connectors.Typeform.Models;

namespace TheTechIdea.Beep.Connectors.Typeform
{
    /// <summary>
    /// Typeform data source implementation using WebAPIDataSource as base class
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Typeform)]
    public class TypeformDataSource : WebAPIDataSource
    {
        // Entity endpoints mapping for Typeform API
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // Forms
            ["forms"] = "forms",
            ["forms.get"] = "forms/{id}",
            // Responses
            ["responses"] = "forms/{form_id}/responses",
            ["responses.get"] = "responses/{id}"
        };

        // Required filters for each entity
        private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            ["forms.get"] = new[] { "id" },
            ["responses"] = new[] { "form_id" },
            ["responses.get"] = new[] { "id" }
        };

        public TypeformDataSource(
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
                throw new InvalidOperationException($"Unknown Typeform entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            // Build the full URL
            var url = $"https://api.typeform.com/v2/{endpoint}";
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
                "responses" => ParseResponses(json),
                "responses.get" => ParseResponse(json),
                _ => throw new NotSupportedException($"Entity '{EntityName}' parsing not implemented.")
            };
        }

        // Helper methods for parsing
        private IEnumerable<object> ParseForms(string json)
        {
            var response = JsonSerializer.Deserialize<TypeformFormsResponse>(json);
            return response?.Items ?? new List<TypeformForm>();
        }

        private IEnumerable<object> ParseForm(string json)
        {
            var form = JsonSerializer.Deserialize<TypeformForm>(json);
            return form != null ? new[] { form } : Array.Empty<TypeformForm>();
        }

        private IEnumerable<object> ParseResponses(string json)
        {
            var response = JsonSerializer.Deserialize<TypeformResponsesResponse>(json);
            return response?.Items ?? new List<TypeformResponse>();
        }

        private IEnumerable<object> ParseResponse(string json)
        {
            var response = JsonSerializer.Deserialize<TypeformResponse>(json);
            return response != null ? new[] { response } : Array.Empty<TypeformResponse>();
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
                throw new ArgumentException($"Typeform entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
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
            ObjectType ="TypeformForm",
            PointType = EnumPointType.Function,
           Name = "GetForms",
            Caption = "Get Typeform Forms",
            ClassName = "TypeformDataSource",
            misc = "ReturnType: IEnumerable<TypeformForm>"
        )]
        public IEnumerable<TypeformForm> GetForms()
        {
            return GetEntity("forms", null).Cast<TypeformForm>();
        }

        [CommandAttribute(
            ObjectType ="TypeformForm",
            PointType = EnumPointType.Function,
           Name = "GetForm",
            Caption = "Get Typeform Form by ID",
            ClassName = "TypeformDataSource",
            misc = "ReturnType: IEnumerable<TypeformForm>"
        )]
        public IEnumerable<TypeformForm> GetForm(string id)
        {
            return GetEntity("forms.get", new List<AppFilter> { new AppFilter { FieldName = "id", FilterValue = id } }).Cast<TypeformForm>();
        }

        [CommandAttribute(
            ObjectType ="TypeformResponse",
            PointType = EnumPointType.Function,
           Name = "GetResponses",
            Caption = "Get Typeform Responses for a Form",
            ClassName = "TypeformDataSource",
            misc = "ReturnType: IEnumerable<TypeformResponse>"
        )]
        public IEnumerable<TypeformResponse> GetResponses(string formId)
        {
            return GetEntity("responses", new List<AppFilter> { new AppFilter { FieldName = "form_id", FilterValue = formId } }).Cast<TypeformResponse>();
        }

        [CommandAttribute(
            ObjectType ="TypeformResponse",
            PointType = EnumPointType.Function,
           Name = "GetResponse",
            Caption = "Get Typeform Response by ID",
            ClassName = "TypeformDataSource",
            misc = "ReturnType: IEnumerable<TypeformResponse>"
        )]
        public IEnumerable<TypeformResponse> GetResponse(string id)
        {
            return GetEntity("responses.get", new List<AppFilter> { new AppFilter { FieldName = "id", FilterValue = id } }).Cast<TypeformResponse>();
        }

        [CommandAttribute(
            ObjectType ="TypeformForm",
            PointType = EnumPointType.Function,
           Name = "CreateForm",
            Caption = "Create Typeform Form",
            ClassName = "TypeformDataSource",
            misc = "ReturnType: IEnumerable<TypeformForm>"
        )]
        public async Task<IEnumerable<TypeformForm>> CreateFormAsync(TypeformForm form)
        {
            try
            {
                var url = "https://api.typeform.com/v2/forms";
                var response = await PostAsync(url, form);
                var json = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    var createdForm = JsonSerializer.Deserialize<TypeformForm>(json);
                    if (createdForm != null)
                    {
                        createdForm.Attach<TypeformForm>(this);
                        return new[] { createdForm };
                    }
                }
                else
                {
                    Logger?.LogError($"Failed to create form: {json}");
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating form: {ex.Message}");
            }
            return Array.Empty<TypeformForm>();
        }

        [CommandAttribute(
            ObjectType ="TypeformResponse",
            PointType = EnumPointType.Function,
           Name = "CreateResponse",
            Caption = "Create Typeform Response",
            ClassName = "TypeformDataSource",
            misc = "ReturnType: IEnumerable<TypeformResponse>"
        )]
        public async Task<IEnumerable<TypeformResponse>> CreateResponseAsync(string formId, TypeformResponse response)
        {
            try
            {
                var url = $"https://api.typeform.com/v2/forms/{formId}/responses";
                var responseResult = await PostAsync(url, response);
                var json = await responseResult.Content.ReadAsStringAsync();
                
                if (responseResult.IsSuccessStatusCode)
                {
                    var createdResponse = JsonSerializer.Deserialize<TypeformResponse>(json);
                    if (createdResponse != null)
                    {
                        createdResponse.Attach<TypeformResponse>(this);
                        return new[] { createdResponse };
                    }
                }
                else
                {
                    Logger?.LogError($"Failed to create response: {json}");
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating response: {ex.Message}");
            }
            return Array.Empty<TypeformResponse>();
        }
    }
}