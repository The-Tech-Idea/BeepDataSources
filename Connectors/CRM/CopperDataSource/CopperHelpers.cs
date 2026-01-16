using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.DataSources.CRM.Copper;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Connectors.Copper
{
    internal static class CopperHelpers
    {
        internal sealed class CopperParsedResult
        {
            public List<object> Items { get; } = new();
            public CopperPagination? Pagination { get; set; }
        }

        public static Dictionary<string, string> FiltersToQuery(List<AppFilter> filters)
        {
            var query = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (filters == null || filters.Count == 0)
            {
                return query;
            }

            foreach (var filter in filters)
            {
                if (filter == null || string.IsNullOrWhiteSpace(filter.FieldName) || filter.FilterValue == null)
                {
                    continue;
                }

                var key = filter.FieldName.Trim();
                var value = filter.FilterValue.ToString() ?? string.Empty;
                var op = filter.Operator?.Trim().ToLowerInvariant();

                // Copper API uses different filter syntax for search endpoints
                switch (op)
                {
                    case "like":
                    case "contains":
                        query["q"] = value;
                        break;
                    default:
                        // For Copper, most filtering is done via search parameters
                        query[key] = value;
                        break;
                }
            }

            return query;
        }

        public static void RequireFilters(string entity, Dictionary<string, string> query, string[] required)
        {
            if (required == null || required.Length == 0)
            {
                return;
            }

            var missing = required
                .Where(r => !query.ContainsKey(r) || string.IsNullOrWhiteSpace(query[r]))
                .ToList();

            if (missing.Count > 0)
            {
                throw new ArgumentException($"Copper entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
            }
        }

        public static string ResolveEndpoint(string template, Dictionary<string, string> query)
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                return template;
            }

            foreach (var key in query.Keys.ToList())
            {
                var placeholder = "{" + key + "}";
                if (!template.Contains(placeholder, StringComparison.Ordinal))
                {
                    continue;
                }

                var value = query[key] ?? string.Empty;
                template = template.Replace(placeholder, Uri.EscapeDataString(value));
                query.Remove(key);
            }

            return template;
        }

        public static EntityStructure BuildEntityStructure(string entityName, Type modelType, string dataSourceName)
        {
            var fields = new List<EntityField>();

            foreach (var property in modelType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var jsonName = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? property.Name;
                var Fieldtype = ResolveFieldtype(property.PropertyType);

                fields.Add(new EntityField
                {
                    FieldName = jsonName,
                    Fieldtype = Fieldtype,
                    AllowDBNull = true,
                    ValueRetrievedFromParent = false,
                    IsAutoIncrement = false,
                    IsIdentity = false,
                    IsKey = string.Equals(jsonName, "id", StringComparison.OrdinalIgnoreCase),
                    IsUnique = false
                });
            }

            return new EntityStructure
            {
                EntityName = entityName,
                DatasourceEntityName = entityName,
                DataSourceID = dataSourceName,
                Fields = fields
            };
        }

        public static async Task<CopperParsedResult> ParseResponseAsync(HttpResponseMessage response, string entityName, Type modelType)
        {
            var result = new CopperParsedResult();
            if (response == null)
            {
                return result;
            }

            var payload = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(payload))
            {
                return result;
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            // Copper API responses have different structures
            if (root.ValueKind == JsonValueKind.Array)
            {
                // Direct array response
                AddItems(result.Items, root, modelType, options);
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                // Check for pagination structure
                if (root.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Array)
                {
                    AddItems(result.Items, dataElement, modelType, options);

                    if (root.TryGetProperty("pagination", out var paginationElement))
                    {
                        try
                        {
                            result.Pagination = JsonSerializer.Deserialize<CopperPagination>(paginationElement.GetRawText(), options);
                        }
                        catch
                        {
                            // ignore deserialization issues for pagination
                        }
                    }
                }
                else if (root.TryGetProperty("data", out var singleDataElement) && singleDataElement.ValueKind == JsonValueKind.Object)
                {
                    // Single object response
                    if (TryDeserialize(singleDataElement, modelType, options, out var item) && item != null)
                    {
                        result.Items.Add(item);
                    }
                }
                else
                {
                    // Try to deserialize the root object directly
                    if (TryDeserialize(root, modelType, options, out var item) && item != null)
                    {
                        result.Items.Add(item);
                    }
                }
            }

            return result;
        }

        private static void AddItems(List<object> target, JsonElement node, Type modelType, JsonSerializerOptions options)
        {
            if (node.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in node.EnumerateArray())
                {
                    if (TryDeserialize(element, modelType, options, out var item) && item != null)
                    {
                        target.Add(item);
                    }
                }
            }
            else if (node.ValueKind == JsonValueKind.Object)
            {
                if (TryDeserialize(node, modelType, options, out var item) && item != null)
                {
                    target.Add(item);
                }
            }
        }

        private static bool TryDeserialize(JsonElement element, Type modelType, JsonSerializerOptions options, out object? value)
        {
            try
            {
                value = JsonSerializer.Deserialize(element.GetRawText(), modelType, options);
                if (value != null)
                {
                    return true;
                }
            }
            catch
            {
                // ignore and fall back
            }

            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText(), options);
                if (dict != null)
                {
                    value = dict;
                    return true;
                }
            }
            catch
            {
                // ignore
            }

            value = null;
            return false;
        }

        private static string ResolveFieldtype(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            if (type == typeof(string)) return "String";
            if (type == typeof(int) || type == typeof(short)) return "Int";
            if (type == typeof(long)) return "Long";
            if (type == typeof(decimal) || type == typeof(double) || type == typeof(float)) return "Decimal";
            if (type == typeof(bool)) return "Boolean";
            if (type == typeof(DateTime)) return "DateTime";
            if (type == typeof(Guid)) return "Guid";

            if (typeof(IEnumerable<object>).IsAssignableFrom(type)) return "Array";
            if (type.IsClass) return "Object";

            return type.Name;
        }
    }
}