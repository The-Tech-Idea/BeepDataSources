using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.DataSources.CRM.Freshsales;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Connectors.Freshsales
{
    internal static class FreshsalesHelpers
    {
        internal sealed class FreshsalesParsedResult
        {
            public List<object> Items { get; } = new();
            public FreshsalesMeta? Meta { get; set; }
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

                switch (op)
                {
                    case "like":
                    case "contains":
                        query["q"] = value;
                        break;
                    case ">":
                    case "gt":
                        query[$"filter[{key}][gt]"] = value;
                        break;
                    case "<":
                    case "lt":
                        query[$"filter[{key}][lt]"] = value;
                        break;
                    case ">=":
                    case "gte":
                        query[$"filter[{key}][gte]"] = value;
                        break;
                    case "<=":
                    case "lte":
                        query[$"filter[{key}][lte]"] = value;
                        break;
                    case "!=":
                    case "ne":
                    case "neq":
                        query[$"filter[{key}][ne]"] = value;
                        break;
                    default:
                        query[$"filter[{key}]"] = value;
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
                throw new ArgumentException($"Freshsales entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
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
                var fieldType = ResolveFieldType(property.PropertyType);

                fields.Add(new EntityField
                {
                    fieldname = jsonName,
                    fieldtype = fieldType,
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

        public static async Task<FreshsalesParsedResult> ParseResponseAsync(HttpResponseMessage response, string entityName, Type modelType)
        {
            var result = new FreshsalesParsedResult();
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

            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("meta", out var metaElement))
            {
                try
                {
                    result.Meta = JsonSerializer.Deserialize<FreshsalesMeta>(metaElement.GetRawText(), options);
                }
                catch
                {
                    // ignore deserialization issues for meta
                }
            }

            var node = ResolvePayloadNode(root, entityName);
            if (node.HasValue)
            {
                AddItems(result.Items, node.Value, modelType, options);
            }
            else
            {
                AddItems(result.Items, root, modelType, options);
            }

            return result;
        }

        private static JsonElement? ResolvePayloadNode(JsonElement root, string entityName)
        {
            if (root.ValueKind == JsonValueKind.Array)
            {
                return root;
            }

            var normalized = NormalizeEntityName(entityName);

            if (TryGetProperty(root, "data", out var data))
            {
                if (data.ValueKind == JsonValueKind.Array)
                {
                    return data;
                }

                if (data.ValueKind == JsonValueKind.Object)
                {
                    if (TryGetProperty(data, "items", out var items) && items.ValueKind == JsonValueKind.Array)
                    {
                        return items;
                    }

                    if (TryGetProperty(data, normalized, out var nested))
                    {
                        return nested;
                    }
                }
            }

            if (TryGetProperty(root, normalized, out var entityNode))
            {
                return entityNode;
            }

            return null;
        }

        private static string NormalizeEntityName(string entityName)
            => entityName.Replace('.', '_');

        private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement value)
        {
            if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out value))
            {
                return true;
            }

            value = default;
            return false;
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

        private static string ResolveFieldType(Type type)
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
