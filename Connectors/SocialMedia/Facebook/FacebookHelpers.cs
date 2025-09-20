using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.FacebookDataSource
{
    /// <summary>
    /// Helper class for parsing Facebook API responses
    /// </summary>
    public static class FacebookHelpers
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

                // Handle different response structures
                if (root.TryGetProperty("data", out JsonElement dataElement))
                {
                    // Response has data array
                    if (dataElement.ValueKind == JsonValueKind.Array)
                    {
                        ParseDataArray(dataTable, dataElement, entityName);
                    }
                    else
                    {
                        // Single object response
                        ParseSingleObject(dataTable, dataElement, entityName);
                    }
                }
                else
                {
                    // Direct object response
                    ParseSingleObject(dataTable, root, entityName);
                }
            }
            catch (JsonException ex)
            {
                throw new Exception($"Failed to parse Facebook API response: {ex.Message}", ex);
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
                "posts" => new List<EntityField>
                {
                    new EntityField { fieldname = "id", fieldtype = "string" },
                    new EntityField { fieldname = "message", fieldtype = "string" },
                    new EntityField { fieldname = "created_time", fieldtype = "datetime" },
                    new EntityField { fieldname = "updated_time", fieldtype = "datetime" },
                    new EntityField { fieldname = "likes_count", fieldtype = "int" },
                    new EntityField { fieldname = "comments_count", fieldtype = "int" }
                },
                "pages" => new List<EntityField>
                {
                    new EntityField { fieldname = "id", fieldtype = "string" },
                    new EntityField { fieldname = "name", fieldtype = "string" },
                    new EntityField { fieldname = "category", fieldtype = "string" },
                    new EntityField { fieldname = "description", fieldtype = "string" },
                    new EntityField { fieldname = "website", fieldtype = "string" },
                    new EntityField { fieldname = "phone", fieldtype = "string" }
                },
                "groups" => new List<EntityField>
                {
                    new EntityField { fieldname = "id", fieldtype = "string" },
                    new EntityField { fieldname = "name", fieldtype = "string" },
                    new EntityField { fieldname = "description", fieldtype = "string" },
                    new EntityField { fieldname = "privacy", fieldtype = "string" },
                    new EntityField { fieldname = "updated_time", fieldtype = "datetime" }
                },
                "events" => new List<EntityField>
                {
                    new EntityField { fieldname = "id", fieldtype = "string" },
                    new EntityField { fieldname = "name", fieldtype = "string" },
                    new EntityField { fieldname = "description", fieldtype = "string" },
                    new EntityField { fieldname = "start_time", fieldtype = "datetime" },
                    new EntityField { fieldname = "end_time", fieldtype = "datetime" },
                    new EntityField { fieldname = "attending_count", fieldtype = "int" },
                    new EntityField { fieldname = "interested_count", fieldtype = "int" }
                },
                "ads" => new List<EntityField>
                {
                    new EntityField { fieldname = "id", fieldtype = "string" },
                    new EntityField { fieldname = "name", fieldtype = "string" },
                    new EntityField { fieldname = "status", fieldtype = "string" },
                    new EntityField { fieldname = "created_time", fieldtype = "datetime" },
                    new EntityField { fieldname = "updated_time", fieldtype = "datetime" }
                },
                "insights" => new List<EntityField>
                {
                    new EntityField { fieldname = "id", fieldtype = "string" },
                    new EntityField { fieldname = "name", fieldtype = "string" },
                    new EntityField { fieldname = "period", fieldtype = "string" },
                    new EntityField { fieldname = "values", fieldtype = "string" },
                    new EntityField { fieldname = "title", fieldtype = "string" },
                    new EntityField { fieldname = "description", fieldtype = "string" }
                },
                _ => new List<EntityField>()
            };
        }
    }
}