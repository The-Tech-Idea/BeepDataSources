using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.ZendeskDataSource
{
    /// <summary>
    /// Helper class for parsing Zendesk API responses
    /// </summary>
    public static class ZendeskHelpers
    {
        /// <summary>
        /// Parses entity data from JSON response
        /// </summary>
        public static DataTable ParseEntityData(string jsonResponse, string entityName)
        {
            var dataTable = new DataTable(entityName);

            try
            {
                using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                JsonElement root = doc.RootElement;

                // Handle different Zendesk API response structures
                if (root.TryGetProperty(entityName, out JsonElement entityArray))
                {
                    // Response has entity array (e.g., {"tickets": [...]})
                    if (entityArray.ValueKind == JsonValueKind.Array)
                    {
                        ParseDataArray(dataTable, entityArray, entityName);
                    }
                    else
                    {
                        // Single entity object
                        ParseSingleObject(dataTable, entityArray, entityName);
                    }
                }
                else if (root.ValueKind == JsonValueKind.Array)
                {
                    // Direct array response
                    ParseDataArray(dataTable, root, entityName);
                }
                else if (root.ValueKind == JsonValueKind.Object)
                {
                    // Single object response
                    ParseSingleObject(dataTable, root, entityName);
                }
                else
                {
                    throw new JsonException("Unexpected JSON structure in Zendesk API response");
                }
            }
            catch (JsonException ex)
            {
                throw new Exception($"Failed to parse Zendesk API response: {ex.Message}", ex);
            }

            return dataTable;
        }

        /// <summary>
        /// Parses array of data objects
        /// </summary>
        private static void ParseDataArray(DataTable dataTable, JsonElement dataArray, string entityName)
        {
            if (dataArray.GetArrayLength() == 0)
            {
                // Create columns from entity metadata
                CreateColumnsFromEntity(dataTable, entityName);
                return;
            }

            foreach (JsonElement item in dataArray.EnumerateArray())
            {
                if (dataTable.Columns.Count == 0)
                {
                    CreateColumnsFromJson(dataTable, item);
                }

                DataRow row = dataTable.NewRow();
                PopulateRowFromJson(row, item);
                dataTable.Rows.Add(row);
            }
        }

        /// <summary>
        /// Parses single object response
        /// </summary>
        private static void ParseSingleObject(DataTable dataTable, JsonElement dataObject, string entityName)
        {
            CreateColumnsFromJson(dataTable, dataObject);

            DataRow row = dataTable.NewRow();
            PopulateRowFromJson(row, dataObject);
            dataTable.Rows.Add(row);
        }

        /// <summary>
        /// Creates DataTable columns from JSON object properties
        /// </summary>
        private static void CreateColumnsFromJson(DataTable dataTable, JsonElement jsonElement)
        {
            foreach (JsonProperty property in jsonElement.EnumerateObject())
            {
                string columnName = property.Name;
                Type columnType = GetColumnType(property.Value);

                if (!dataTable.Columns.Contains(columnName))
                {
                    dataTable.Columns.Add(columnName, columnType);
                }
            }
        }

        /// <summary>
        /// Creates DataTable columns from entity metadata
        /// </summary>
        private static void CreateColumnsFromEntity(DataTable dataTable, string entityName)
        {
            var fields = GetEntityFields(entityName);

            foreach (var field in fields)
            {
                Type columnType = GetTypeFromFieldtype(field.Fieldtype);
                dataTable.Columns.Add(field.FieldName, columnType);
            }
        }

        /// <summary>
        /// Populates a DataRow from JSON object
        /// </summary>
        private static void PopulateRowFromJson(DataRow row, JsonElement jsonElement)
        {
            foreach (JsonProperty property in jsonElement.EnumerateObject())
            {
                string columnName = property.Name;

                if (row.Table.Columns.Contains(columnName))
                {
                    object value = ParseJsonValue(property.Value);
                    row[columnName] = value ?? DBNull.Value;
                }
            }
        }

        /// <summary>
        /// Parses JSON value to appropriate .NET type
        /// </summary>
        private static object ParseJsonValue(JsonElement value)
        {
            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number => value.TryGetInt32(out int intValue) ? intValue :
                                       value.TryGetInt64(out long longValue) ? longValue :
                                       value.TryGetDouble(out double doubleValue) ? doubleValue : value.GetDecimal(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                JsonValueKind.Object => value.GetRawText(), // Store complex objects as JSON string
                JsonValueKind.Array => value.GetRawText(),  // Store arrays as JSON string
                _ => value.GetRawText()
            };
        }

        /// <summary>
        /// Gets .NET type from JSON element
        /// </summary>
        private static Type GetColumnType(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => typeof(string),
                JsonValueKind.Number => element.TryGetInt32(out _) ? typeof(int) :
                                       element.TryGetInt64(out _) ? typeof(long) : typeof(double),
                JsonValueKind.True or JsonValueKind.False => typeof(bool),
                JsonValueKind.Object or JsonValueKind.Array => typeof(string), // Store as JSON string
                _ => typeof(string)
            };
        }

        /// <summary>
        /// Gets .NET type from field type string
        /// </summary>
        private static Type GetTypeFromFieldtype(string Fieldtype)
        {
            return Fieldtype?.ToLower() switch
            {
                "string" => typeof(string),
                "int" or "integer" => typeof(int),
                "long" => typeof(long),
                "double" or "decimal" => typeof(double),
                "bool" or "boolean" => typeof(bool),
                "datetime" => typeof(DateTime),
                _ => typeof(string)
            };
        }

        /// <summary>
        /// Gets entity fields for a given entity
        /// </summary>
        private static List<EntityField> GetEntityFields(string entityName)
        {
            return entityName switch
            {
                "tickets" => new List<EntityField>
                {
                    new EntityField { FieldName = "id", Fieldtype ="long" },
                    new EntityField { FieldName = "url", Fieldtype ="string" },
                    new EntityField { FieldName = "external_id", Fieldtype ="string" },
                    new EntityField { FieldName = "type", Fieldtype ="string" },
                    new EntityField { FieldName = "subject", Fieldtype ="string" },
                    new EntityField { FieldName = "raw_subject", Fieldtype ="string" },
                    new EntityField { FieldName = "description", Fieldtype ="string" },
                    new EntityField { FieldName = "priority", Fieldtype ="string" },
                    new EntityField { FieldName = "status", Fieldtype ="string" },
                    new EntityField { FieldName = "recipient", Fieldtype ="string" },
                    new EntityField { FieldName = "requester_id", Fieldtype ="long" },
                    new EntityField { FieldName = "submitter_id", Fieldtype ="long" },
                    new EntityField { FieldName = "assignee_id", Fieldtype ="long" },
                    new EntityField { FieldName = "organization_id", Fieldtype ="long" },
                    new EntityField { FieldName = "group_id", Fieldtype ="long" },
                    new EntityField { FieldName = "has_incidents", Fieldtype ="bool" },
                    new EntityField { FieldName = "is_public", Fieldtype ="bool" },
                    new EntityField { FieldName = "due_at", Fieldtype ="datetime" },
                    new EntityField { FieldName = "created_at", Fieldtype ="datetime" },
                    new EntityField { FieldName = "updated_at", Fieldtype ="datetime" }
                },
                "users" => new List<EntityField>
                {
                    new EntityField { FieldName = "id", Fieldtype ="long" },
                    new EntityField { FieldName = "url", Fieldtype ="string" },
                    new EntityField { FieldName = "name", Fieldtype ="string" },
                    new EntityField { FieldName = "email", Fieldtype ="string" },
                    new EntityField { FieldName = "created_at", Fieldtype ="datetime" },
                    new EntityField { FieldName = "updated_at", Fieldtype ="datetime" },
                    new EntityField { FieldName = "time_zone", Fieldtype ="string" },
                    new EntityField { FieldName = "phone", Fieldtype ="string" },
                    new EntityField { FieldName = "locale_id", Fieldtype ="int" },
                    new EntityField { FieldName = "locale", Fieldtype ="string" },
                    new EntityField { FieldName = "organization_id", Fieldtype ="long" },
                    new EntityField { FieldName = "role", Fieldtype ="string" },
                    new EntityField { FieldName = "verified", Fieldtype ="bool" },
                    new EntityField { FieldName = "external_id", Fieldtype ="string" },
                    new EntityField { FieldName = "alias", Fieldtype ="string" },
                    new EntityField { FieldName = "active", Fieldtype ="bool" },
                    new EntityField { FieldName = "shared", Fieldtype ="bool" },
                    new EntityField { FieldName = "last_login_at", Fieldtype ="datetime" },
                    new EntityField { FieldName = "suspended", Fieldtype ="bool" }
                },
                "organizations" => new List<EntityField>
                {
                    new EntityField { FieldName = "id", Fieldtype ="long" },
                    new EntityField { FieldName = "url", Fieldtype ="string" },
                    new EntityField { FieldName = "external_id", Fieldtype ="string" },
                    new EntityField { FieldName = "name", Fieldtype ="string" },
                    new EntityField { FieldName = "created_at", Fieldtype ="datetime" },
                    new EntityField { FieldName = "updated_at", Fieldtype ="datetime" },
                    new EntityField { FieldName = "details", Fieldtype ="string" },
                    new EntityField { FieldName = "notes", Fieldtype ="string" },
                    new EntityField { FieldName = "group_id", Fieldtype ="long" },
                    new EntityField { FieldName = "shared_tickets", Fieldtype ="bool" },
                    new EntityField { FieldName = "shared_comments", Fieldtype ="bool" }
                },
                "groups" => new List<EntityField>
                {
                    new EntityField { FieldName = "id", Fieldtype ="long" },
                    new EntityField { FieldName = "url", Fieldtype ="string" },
                    new EntityField { FieldName = "name", Fieldtype ="string" },
                    new EntityField { FieldName = "description", Fieldtype ="string" },
                    new EntityField { FieldName = "default", Fieldtype ="bool" },
                    new EntityField { FieldName = "deleted", Fieldtype ="bool" },
                    new EntityField { FieldName = "created_at", Fieldtype ="datetime" },
                    new EntityField { FieldName = "updated_at", Fieldtype ="datetime" }
                },
                "comments" => new List<EntityField>
                {
                    new EntityField { FieldName = "id", Fieldtype ="long" },
                    new EntityField { FieldName = "type", Fieldtype ="string" },
                    new EntityField { FieldName = "author_id", Fieldtype ="long" },
                    new EntityField { FieldName = "body", Fieldtype ="string" },
                    new EntityField { FieldName = "html_body", Fieldtype ="string" },
                    new EntityField { FieldName = "plain_body", Fieldtype ="string" },
                    new EntityField { FieldName = "public", Fieldtype ="bool" },
                    new EntityField { FieldName = "audit_id", Fieldtype ="long" },
                    new EntityField { FieldName = "created_at", Fieldtype ="datetime" }
                },
                "macros" => new List<EntityField>
                {
                    new EntityField { FieldName = "id", Fieldtype ="long" },
                    new EntityField { FieldName = "title", Fieldtype ="string" },
                    new EntityField { FieldName = "active", Fieldtype ="bool" },
                    new EntityField { FieldName = "updated_at", Fieldtype ="datetime" },
                    new EntityField { FieldName = "created_at", Fieldtype ="datetime" },
                    new EntityField { FieldName = "description", Fieldtype ="string" }
                },
                "views" => new List<EntityField>
                {
                    new EntityField { FieldName = "id", Fieldtype ="long" },
                    new EntityField { FieldName = "title", Fieldtype ="string" },
                    new EntityField { FieldName = "active", Fieldtype ="bool" },
                    new EntityField { FieldName = "updated_at", Fieldtype ="datetime" },
                    new EntityField { FieldName = "created_at", Fieldtype ="datetime" },
                    new EntityField { FieldName = "position", Fieldtype ="int" },
                    new EntityField { FieldName = "watchable", Fieldtype ="bool" }
                },
                _ => new List<EntityField>()
            };
        }
    }
}