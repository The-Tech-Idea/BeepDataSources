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
                Type columnType = GetTypeFromFieldType(field.fieldtype);
                dataTable.Columns.Add(field.fieldname, columnType);
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
        private static Type GetTypeFromFieldType(string fieldType)
        {
            return fieldType?.ToLower() switch
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
                    new EntityField { fieldname = "id", fieldtype = "long" },
                    new EntityField { fieldname = "url", fieldtype = "string" },
                    new EntityField { fieldname = "external_id", fieldtype = "string" },
                    new EntityField { fieldname = "type", fieldtype = "string" },
                    new EntityField { fieldname = "subject", fieldtype = "string" },
                    new EntityField { fieldname = "raw_subject", fieldtype = "string" },
                    new EntityField { fieldname = "description", fieldtype = "string" },
                    new EntityField { fieldname = "priority", fieldtype = "string" },
                    new EntityField { fieldname = "status", fieldtype = "string" },
                    new EntityField { fieldname = "recipient", fieldtype = "string" },
                    new EntityField { fieldname = "requester_id", fieldtype = "long" },
                    new EntityField { fieldname = "submitter_id", fieldtype = "long" },
                    new EntityField { fieldname = "assignee_id", fieldtype = "long" },
                    new EntityField { fieldname = "organization_id", fieldtype = "long" },
                    new EntityField { fieldname = "group_id", fieldtype = "long" },
                    new EntityField { fieldname = "has_incidents", fieldtype = "bool" },
                    new EntityField { fieldname = "is_public", fieldtype = "bool" },
                    new EntityField { fieldname = "due_at", fieldtype = "datetime" },
                    new EntityField { fieldname = "created_at", fieldtype = "datetime" },
                    new EntityField { fieldname = "updated_at", fieldtype = "datetime" }
                },
                "users" => new List<EntityField>
                {
                    new EntityField { fieldname = "id", fieldtype = "long" },
                    new EntityField { fieldname = "url", fieldtype = "string" },
                    new EntityField { fieldname = "name", fieldtype = "string" },
                    new EntityField { fieldname = "email", fieldtype = "string" },
                    new EntityField { fieldname = "created_at", fieldtype = "datetime" },
                    new EntityField { fieldname = "updated_at", fieldtype = "datetime" },
                    new EntityField { fieldname = "time_zone", fieldtype = "string" },
                    new EntityField { fieldname = "phone", fieldtype = "string" },
                    new EntityField { fieldname = "locale_id", fieldtype = "int" },
                    new EntityField { fieldname = "locale", fieldtype = "string" },
                    new EntityField { fieldname = "organization_id", fieldtype = "long" },
                    new EntityField { fieldname = "role", fieldtype = "string" },
                    new EntityField { fieldname = "verified", fieldtype = "bool" },
                    new EntityField { fieldname = "external_id", fieldtype = "string" },
                    new EntityField { fieldname = "alias", fieldtype = "string" },
                    new EntityField { fieldname = "active", fieldtype = "bool" },
                    new EntityField { fieldname = "shared", fieldtype = "bool" },
                    new EntityField { fieldname = "last_login_at", fieldtype = "datetime" },
                    new EntityField { fieldname = "suspended", fieldtype = "bool" }
                },
                "organizations" => new List<EntityField>
                {
                    new EntityField { fieldname = "id", fieldtype = "long" },
                    new EntityField { fieldname = "url", fieldtype = "string" },
                    new EntityField { fieldname = "external_id", fieldtype = "string" },
                    new EntityField { fieldname = "name", fieldtype = "string" },
                    new EntityField { fieldname = "created_at", fieldtype = "datetime" },
                    new EntityField { fieldname = "updated_at", fieldtype = "datetime" },
                    new EntityField { fieldname = "details", fieldtype = "string" },
                    new EntityField { fieldname = "notes", fieldtype = "string" },
                    new EntityField { fieldname = "group_id", fieldtype = "long" },
                    new EntityField { fieldname = "shared_tickets", fieldtype = "bool" },
                    new EntityField { fieldname = "shared_comments", fieldtype = "bool" }
                },
                "groups" => new List<EntityField>
                {
                    new EntityField { fieldname = "id", fieldtype = "long" },
                    new EntityField { fieldname = "url", fieldtype = "string" },
                    new EntityField { fieldname = "name", fieldtype = "string" },
                    new EntityField { fieldname = "description", fieldtype = "string" },
                    new EntityField { fieldname = "default", fieldtype = "bool" },
                    new EntityField { fieldname = "deleted", fieldtype = "bool" },
                    new EntityField { fieldname = "created_at", fieldtype = "datetime" },
                    new EntityField { fieldname = "updated_at", fieldtype = "datetime" }
                },
                "comments" => new List<EntityField>
                {
                    new EntityField { fieldname = "id", fieldtype = "long" },
                    new EntityField { fieldname = "type", fieldtype = "string" },
                    new EntityField { fieldname = "author_id", fieldtype = "long" },
                    new EntityField { fieldname = "body", fieldtype = "string" },
                    new EntityField { fieldname = "html_body", fieldtype = "string" },
                    new EntityField { fieldname = "plain_body", fieldtype = "string" },
                    new EntityField { fieldname = "public", fieldtype = "bool" },
                    new EntityField { fieldname = "audit_id", fieldtype = "long" },
                    new EntityField { fieldname = "created_at", fieldtype = "datetime" }
                },
                "macros" => new List<EntityField>
                {
                    new EntityField { fieldname = "id", fieldtype = "long" },
                    new EntityField { fieldname = "title", fieldtype = "string" },
                    new EntityField { fieldname = "active", fieldtype = "bool" },
                    new EntityField { fieldname = "updated_at", fieldtype = "datetime" },
                    new EntityField { fieldname = "created_at", fieldtype = "datetime" },
                    new EntityField { fieldname = "description", fieldtype = "string" }
                },
                "views" => new List<EntityField>
                {
                    new EntityField { fieldname = "id", fieldtype = "long" },
                    new EntityField { fieldname = "title", fieldtype = "string" },
                    new EntityField { fieldname = "active", fieldtype = "bool" },
                    new EntityField { fieldname = "updated_at", fieldtype = "datetime" },
                    new EntityField { fieldname = "created_at", fieldtype = "datetime" },
                    new EntityField { fieldname = "position", fieldtype = "int" },
                    new EntityField { fieldname = "watchable", fieldtype = "bool" }
                },
                _ => new List<EntityField>()
            };
        }
    }
}