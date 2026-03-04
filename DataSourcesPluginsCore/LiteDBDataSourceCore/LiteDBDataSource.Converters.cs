using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using LiteDB;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Util;

namespace LiteDBDataSourceCore
{
    public partial class LiteDBDataSource
    {
        private BsonDocument ToBsonDocument(object data)
        {
            if (data is BsonDocument doc)
                return doc;

            // Check if the data is already a BsonDocument
            if (data is BsonDocument bson)
                return bson;

            // Use LiteDB's BsonMapper to convert POCO to BsonDocument
            var mapper = new BsonMapper();
            return mapper.ToDocument(data);
        }

        private EntityStructure CompileSchemaFromDocuments(List<BsonDocument> documents, string entityName)
        {
            EntityStructure entityStructure = new EntityStructure
            {
                EntityName = entityName ?? "DefaultEntityName",
                DatasourceEntityName = entityName,
                OriginalEntityName = entityName,
                DataSourceID = DatasourceName
            };

            Dictionary<string, EntityField> fieldDictionary = new Dictionary<string, EntityField>();
            int fieldIndex = 0;

            foreach (var document in documents)
            {
                foreach (var element in document)
                {
                    if (!fieldDictionary.ContainsKey(element.Key))
                    {
                        EntityField newField = new EntityField
                        {
                            fieldname = element.Key,
                            BaseColumnName = element.Key,
                            fieldtype = GetDotNetTypeStringFromBsonType(element.Value.Type),
                            IsKey = element.Key.Equals("_id", StringComparison.OrdinalIgnoreCase),
                            IsIdentity = element.Key.Equals("_id", StringComparison.OrdinalIgnoreCase),
                            FieldIndex = fieldIndex++
                        };
                        fieldDictionary[element.Key] = newField;
                    }
                }
            }

            entityStructure.Fields = new List<EntityField>(fieldDictionary.Values);
            return entityStructure;
        }

        private string GetDotNetTypeStringFromBsonType(BsonType bsonType)
        {
            List<DatatypeMapping> dataTypeMappings = DataTypeFieldMappingHelper.GetLiteDBDataTypesMapping();
            DatatypeMapping mapping = null;

            switch (bsonType)
            {
                case BsonType.Double:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("Double", StringComparison.InvariantCultureIgnoreCase));
                    return mapping?.NetDataType ?? "System.Double";
                case BsonType.String:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("String", StringComparison.InvariantCultureIgnoreCase));
                    return mapping?.NetDataType ?? "System.String";
                case BsonType.Document:
                    return "System.Object";
                case BsonType.Array:
                    return "System.Collections.Generic.List<object>";
                case BsonType.Binary:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("Binary", StringComparison.InvariantCultureIgnoreCase));
                    return mapping?.NetDataType ?? "System.Byte[]";
                case BsonType.ObjectId:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("ObjectId", StringComparison.InvariantCultureIgnoreCase));
                    return mapping?.NetDataType ?? "LiteDB.ObjectId";
                case BsonType.Boolean:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("Boolean", StringComparison.InvariantCultureIgnoreCase));
                    return mapping?.NetDataType ?? "System.Boolean";
                case BsonType.DateTime:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("DateTime", StringComparison.InvariantCultureIgnoreCase));
                    return mapping?.NetDataType ?? "System.DateTime";
                case BsonType.Null:
                    return mapping?.NetDataType ?? "System.String";
                case BsonType.Int32:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("Int32", StringComparison.InvariantCultureIgnoreCase));
                    return mapping?.NetDataType ?? "System.Int32";
                case BsonType.Int64:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("Int64", StringComparison.InvariantCultureIgnoreCase));
                    return mapping?.NetDataType ?? "System.Int64";
                case BsonType.Decimal:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("Decimal", StringComparison.InvariantCultureIgnoreCase));
                    return mapping?.NetDataType ?? "System.Decimal";
                default:
                    return "System.Object";
            }
        }

        private Type GetDotNetTypeFromBsonType(BsonType bsonType)
        {
            List<DatatypeMapping> dataTypeMappings = DataTypeFieldMappingHelper.GetLiteDBDataTypesMapping();
            DatatypeMapping mapping = null;

            switch (bsonType)
            {
                case BsonType.Double:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("Double", StringComparison.InvariantCultureIgnoreCase));
                    return Type.GetType(mapping?.NetDataType ?? "System.Double");
                case BsonType.String:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("String", StringComparison.InvariantCultureIgnoreCase));
                    return Type.GetType(mapping?.NetDataType ?? "System.String");
                case BsonType.Document:
                    return typeof(object);
                case BsonType.Array:
                    return typeof(Array);
                case BsonType.Binary:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("Binary", StringComparison.InvariantCultureIgnoreCase));
                    return Type.GetType(mapping?.NetDataType ?? "System.Byte[]");
                case BsonType.ObjectId:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("ObjectId", StringComparison.InvariantCultureIgnoreCase));
                    return Type.GetType(mapping?.NetDataType ?? "LiteDB.ObjectId");
                case BsonType.Boolean:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("Boolean", StringComparison.InvariantCultureIgnoreCase));
                    return Type.GetType(mapping?.NetDataType ?? "System.Boolean");
                case BsonType.DateTime:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("DateTime", StringComparison.InvariantCultureIgnoreCase));
                    return Type.GetType(mapping?.NetDataType ?? "System.DateTime");
                case BsonType.Null:
                    return typeof(object);
                case BsonType.Int32:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("Int32", StringComparison.InvariantCultureIgnoreCase));
                    return Type.GetType(mapping?.NetDataType ?? "System.Int32");
                case BsonType.Int64:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("Int64", StringComparison.InvariantCultureIgnoreCase));
                    return Type.GetType(mapping?.NetDataType ?? "System.Int64");
                case BsonType.Decimal:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("Decimal", StringComparison.InvariantCultureIgnoreCase));
                    return Type.GetType(mapping?.NetDataType ?? "System.Decimal");
                default:
                    return typeof(object);
            }
        }

        private object ConvertBsonValueToNetType(BsonValue bsonValue, Type targetType)
        {
            if (bsonValue.IsNull)
                return null;

            switch (bsonValue.Type)
            {
                case BsonType.Int32:
                    return bsonValue.AsInt32;
                case BsonType.Int64:
                    return bsonValue.AsInt64;
                case BsonType.Double:
                    return bsonValue.AsDouble;
                case BsonType.String:
                    return bsonValue.AsString;
                case BsonType.Document:
                    return bsonValue.AsDocument;
                case BsonType.Array:
                    return bsonValue.AsArray.Select(a => ConvertBsonValueToNetType(a, typeof(object))).ToList();
                case BsonType.Binary:
                    return bsonValue.AsBinary;
                case BsonType.ObjectId:
                    return bsonValue.AsObjectId.ToString(); // Convert ObjectId to string
                case BsonType.Guid:
                    return bsonValue.AsGuid;
                case BsonType.Boolean:
                    return bsonValue.AsBoolean;
                case BsonType.DateTime:
                    return bsonValue.AsDateTime;
                default:
                    return bsonValue;
            }
        }

        private BsonValue GetIdentifierValue(object data)
        {
            if (data is BsonDocument bson)
            {
                if (bson.ContainsKey("_id"))
                    return bson["_id"];
            }
            else if (data is DataRow row)
            {
                if (row.Table.Columns.Contains("_id"))
                    return new BsonValue(row["_id"]);
            }
            else
            {
                // Assuming data is a POCO
                var property = data.GetType().GetProperty("_id");
                if (property != null)
                    return new BsonValue(property.GetValue(data));
            }

            throw new ArgumentException("Data does not contain an identifiable '_id' property.");
        }

        private BsonDocument ConvertToBsonDocument(object data)
        {
            EntityStructure entStructure = DataStruct;
            var doc = new BsonDocument();
            if (data is null)
            {
                return doc;
            }
            if (data is BsonDocument)
            {
                return (BsonDocument)data;
            }
            if (data is DataRow dataRow)
            {
                if (entStructure?.Fields == null || entStructure.Fields.Count == 0)
                {
                    foreach (DataColumn column in dataRow.Table.Columns)
                    {
                        doc[column.ColumnName] = dataRow[column.ColumnName] == DBNull.Value
                            ? BsonValue.Null
                            : new BsonValue(dataRow[column.ColumnName]);
                    }
                    return doc;
                }
                // Convert DataRow to BsonDocument using EntityStructure for schema guidance
                foreach (var field in entStructure.Fields)
                {
                    var fieldName = field.fieldname;
                    var value = dataRow.Table.Columns.Contains(fieldName) ? dataRow[fieldName] : DBNull.Value;

                    // Convert value to the appropriate BsonValue type
                    doc[fieldName] = ConvertToBsonValue(field.fieldtype, value);
                }
            }
            else
            {
                if (entStructure?.Fields == null || entStructure.Fields.Count == 0)
                {
                    return BsonMapper.Global.ToDocument(data);
                }
                // Assuming data is a POCO, manually serialize to BsonDocument using EntityStructure for schema guidance
                foreach (var field in entStructure.Fields)
                {
                    var prop = data.GetType().GetProperty(field.fieldname);
                    if (prop != null)
                    {
                        var value = prop.GetValue(data);
                        doc[field.fieldname] = ConvertToBsonValue(field.fieldtype, value);
                    }
                    else
                    {
                        doc[field.fieldname] = BsonValue.Null;
                    }
                }
            }

            return doc;
        }

        private BsonValue ConvertToBsonValue(string fieldType, object value)
        {
            if (value == null || value == DBNull.Value)
            {
                return BsonValue.Null;
            }

            switch (fieldType)
            {
                case "System.Int32":
                    return new BsonValue(Convert.ToInt32(value));
                case "System.Int64":
                    return new BsonValue(Convert.ToInt64(value));
                case "System.Double":
                    return new BsonValue(Convert.ToDouble(value));
                case "System.Decimal":
                    return new BsonValue(Convert.ToDecimal(value));
                case "System.String":
                    return new BsonValue(SanitizeString(value.ToString()));
                case "System.Boolean":
                    return new BsonValue(Convert.ToBoolean(value));
                case "System.DateTime":
                    return new BsonValue(Convert.ToDateTime(value));
                case "System.Guid":
                    return new BsonValue((Guid)value);
                case "System.Byte[]":
                    return new BsonValue((byte[])value);
                default:
                    return new BsonValue(SanitizeString(value.ToString()));
            }
        }

        private string SanitizeString(string value)
        {
            // Remove unwanted double quotes
            return value.Replace("\"", "");
        }

        private BsonValue ConvertToBsonValue(object value, Type type)
        {
            // Handle conversion based on type
            if (type == typeof(DateTime))
                return new BsonValue(Convert.ToDateTime(value));
            if (type == typeof(int))
                return new BsonValue(Convert.ToInt32(value));
            if (type == typeof(double))
                return new BsonValue(Convert.ToDouble(value));
            if (type == typeof(bool))
                return new BsonValue(Convert.ToBoolean(value));
            if (type == typeof(string))
                return new BsonValue(value.ToString());

            return new BsonValue(value); // As a fallback
        }

        public object ConvertBsonDocumentsToObjects(List<BsonDocument> documents, Type type, EntityStructure entStructure)
        {
            if (documents == null)
            {
                return new List<object>();
            }
            if (type == null)
            {
                return documents.Cast<object>().ToList();
            }
            if (entStructure?.Fields == null || entStructure.Fields.Count == 0)
            {
                return documents.Cast<object>().ToList();
            }

            Type uowGenericType = typeof(ObservableBindingList<>).MakeGenericType(type);
            var records = (IBindingListView)Activator.CreateInstance(uowGenericType);

            foreach (var document in documents)
            {
                dynamic instance = Activator.CreateInstance(type);
                foreach (var field in entStructure.Fields)
                {
                    if (field == null || string.IsNullOrWhiteSpace(field.fieldname))
                    {
                        continue;
                    }
                    var fieldName = field.fieldname.ToLower();
                    var f = document.Keys.FirstOrDefault(p => p.Equals(fieldName, StringComparison.InvariantCultureIgnoreCase));
                    if (!string.IsNullOrEmpty(f))
                    {
                        var bsonValue = document[f];
                        if (!bsonValue.IsNull)
                        {
                            try
                            {
                                string netTypeString = field.fieldtype; // Use field.fieldtype directly
                                Type netType = Type.GetType(netTypeString) ?? typeof(string);
                                var propInfo = type.GetProperty(field.fieldname);
                                if (propInfo == null || !propInfo.CanWrite)
                                {
                                    continue;
                                }

                                if (netType == typeof(string) && bsonValue.IsObjectId)
                                {
                                    // Convert ObjectId to string
                                    var value = bsonValue.AsObjectId.ToString();
                                    propInfo.SetValue(instance, value);
                                }
                                else if (Type.GetTypeCode(netType) == Type.GetTypeCode(Type.GetType(field.fieldtype) ?? netType))
                                {
                                    // Directly assign if types match
                                    object value;
                                    if ((Type.GetType(field.fieldtype) ?? netType) == typeof(string))
                                    {
                                        value = RemoveQuotes(bsonValue.ToString());
                                    }
                                    else
                                    {
                                        value = ConvertBsonValueToNetType(bsonValue, Type.GetType(field.fieldtype) ?? netType);
                                    }
                                    propInfo.SetValue(instance, value);
                                }
                                else
                                {
                                    // Handle type conversion if necessary
                                    object value;
                                    if ((Type.GetType(field.fieldtype) ?? netType) == typeof(string))
                                    {
                                        value = RemoveQuotes(bsonValue.ToString());
                                    }
                                    else
                                    {
                                        var targetType = Type.GetType(field.fieldtype) ?? netType;
                                        value = Convert.ChangeType(ConvertBsonValueToNetType(bsonValue, targetType), targetType);
                                    }
                                    propInfo.SetValue(instance, value);
                                }
                            }
                            catch (Exception ex)
                            {
                                // Handle or log the error appropriately
                                DMEEditor.AddLogMessage("Beep", $"Error setting property {field.fieldname} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                            }
                        }
                    }
                }
                records.Add(instance);
            }

            return records;
        }

        private string RemoveQuotes(string value)
        {
            if (value.StartsWith("\"") && value.EndsWith("\""))
            {
                return value.Substring(1, value.Length - 2);
            }
            return value;
        }
    }
}
