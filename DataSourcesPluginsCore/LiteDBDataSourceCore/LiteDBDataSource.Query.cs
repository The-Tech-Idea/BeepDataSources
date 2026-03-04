using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using DataManagementModels.Editor;

namespace LiteDBDataSourceCore
{
    public partial class LiteDBDataSource
    {
        public Task<double> GetScalarAsync(string query)
        {
            return Task.Run(() => GetScalar(query));
        }

        public double GetScalar(string query)
        {
            try
            {
                if (!EnsureConnectionReady(nameof(GetScalar)))
                {
                    return 0.0;
                }

                using (var session = new LiteDatabase(_connectionString))
                {
                    var result = session.Execute(query);
                    return Convert.ToDouble(result);
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error in executing scalar query ({ex.Message})", DateTime.Now, 0, "", Errors.Failed);
                return 0.0;
            }
        }

        public IErrorsInfo ExecuteSql(string sql)
        {
            var retval = new ErrorsInfo { Flag = Errors.Ok, Message = "Executed Successfully" };
            try
            {
                if (!EnsureConnectionReady(nameof(ExecuteSql)))
                {
                    retval.Flag = Errors.Failed;
                    retval.Message = "Database connection is not open.";
                    return retval;
                }

                using (var session = new LiteDatabase(_connectionString))
                {
                    session.Execute(sql);
                }
            }
            catch (Exception ex)
            {
                retval.Flag = Errors.Failed;
                retval.Message = ex.Message;
                DMEEditor.AddLogMessage("Beep", $"error in {nameof(ExecuteSql)} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return retval;
        }

        public IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            var results = new List<object>();
            try
            {
                if (!EnsureConnectionReady(nameof(GetEntity)))
                {
                    return results;
                }

                using (var session = new LiteDatabase(_connectionString))
                {
                    var collection = session.GetCollection<BsonDocument>(EntityName);
                    SetObjects(EntityName);

                    IEnumerable<BsonDocument> documents;
                    if (filter != null && filter.Count > 0)
                    {
                        var bsonExpression = BuildLiteDBExpression(filter);
                        documents = collection.Find(bsonExpression);
                    }
                    else
                    {
                        documents = collection.Count() > 0 ? collection.FindAll() : new List<BsonDocument>();
                    }

                    List<BsonDocument> ls = documents.ToList();
                    if (ls.Count == 0)
                    {
                        return results;
                    }

                    if (enttype == null || DataStruct == null || DataStruct.Fields == null || DataStruct.Fields.Count == 0)
                    {
                        results.AddRange(ls.Cast<object>());
                        return results;
                    }

                    var converted = ConvertBsonDocumentsToObjects(ls, enttype, DataStruct);
                    if (converted is System.Collections.IEnumerable enumerable)
                    {
                        foreach (var item in enumerable)
                        {
                            results.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"error in {nameof(GetEntity)} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return results;
        }

        public PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            PagedResult pagedResult = new PagedResult();
            ErrorObject.Flag = Errors.Ok;

            try
            {
                if (!EnsureConnectionReady(nameof(GetEntity)))
                {
                    return pagedResult;
                }

                using (var session = new LiteDatabase(_connectionString))
                {
                    var collection = session.GetCollection<BsonDocument>(EntityName);
                    SetObjects(EntityName);

                    // Get total count
                    var bsonExpression = BuildLiteDBExpression(filter);
                    int totalRecords = (int)collection.Count(bsonExpression);

                    // Calculate pagination parameters
                    int skipAmount = (pageNumber - 1) * pageSize;

                    // Get paginated results
                    var documents = collection.Find(bsonExpression, skipAmount, pageSize);
                    List<BsonDocument> result = documents.ToList();

                    if (enttype == null || DataStruct == null || DataStruct.Fields == null || DataStruct.Fields.Count == 0)
                    {
                        pagedResult.Data = result.Cast<object>().ToList();
                        pagedResult.TotalRecords = totalRecords;
                        pagedResult.PageNumber = pageNumber;
                        pagedResult.PageSize = pageSize;
                        pagedResult.TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
                        pagedResult.HasNextPage = pageNumber < pagedResult.TotalPages;
                        pagedResult.HasPreviousPage = pageNumber > 1;
                        return pagedResult;
                    }

                    var converted = ConvertBsonDocumentsToObjects(result, enttype, DataStruct);
                    List<object> results = new List<object>();

                    // Convert IBindingListView to List<object>
                    if (converted is System.Collections.IEnumerable enumerable)
                    {
                        foreach (var item in enumerable)
                        {
                            results.Add(item);
                        }
                    }

                    pagedResult.Data = results;
                    pagedResult.TotalRecords = totalRecords;
                    pagedResult.PageNumber = pageNumber;
                    pagedResult.PageSize = pageSize;
                    pagedResult.TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
                    pagedResult.HasNextPage = pageNumber < pagedResult.TotalPages;
                    pagedResult.HasPreviousPage = pageNumber > 1;
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"error in {nameof(GetEntity)} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return pagedResult;
        }

        public Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            return Task.Run(() => GetEntity(EntityName, Filter));
        }

        public IEnumerable<object> RunQuery(string qrystr)
        {
            var result = new List<object>();
            try
            {
                if (!EnsureConnectionReady(nameof(RunQuery)))
                {
                    return result;
                }

                if (string.IsNullOrWhiteSpace(qrystr))
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = "Query string cannot be empty.";
                    return result;
                }

                using (var session = new LiteDatabase(_connectionString))
                {
                    var collection = session.GetCollection<BsonDocument>("DefaultCollection");

                    var expression = BsonExpression.Create(qrystr);
                    var docs = collection.Find(expression).ToList();
                    if (docs.Count == 0)
                    {
                        return result;
                    }

                    if (enttype == null || DataStruct == null || DataStruct.Fields == null || DataStruct.Fields.Count == 0)
                    {
                        result.AddRange(docs.Cast<object>());
                        return result;
                    }

                    var converted = ConvertBsonDocumentsToObjects(docs, enttype, DataStruct);
                    if (converted is System.Collections.IEnumerable enumerable)
                    {
                        foreach (var item in enumerable)
                        {
                            result.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorObject ??= new ErrorsInfo();
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error running query in {DatasourceName}: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return result;
        }

        private BsonExpression BuildLiteDBExpression(List<AppFilter> filters)
        {
            if (filters == null || filters.Count == 0)
            {
                return BsonExpression.Create("$");
            }

            var clauses = new List<string>();
            foreach (var filter in filters)
            {
                if (filter == null || string.IsNullOrWhiteSpace(filter.FieldName))
                {
                    continue;
                }

                string field = $"$.{filter.FieldName}";
                string op = (filter.Operator ?? "=").Trim().ToLowerInvariant();
                string value = BsonExpressionValue(filter.FilterValue, filter.valueType, filter.FieldType);
                string value2 = BsonExpressionValue(filter.FilterValue1, filter.valueType, filter.FieldType);

                switch (op)
                {
                    case "=":
                    case "==":
                    case "eq":
                        clauses.Add($"{field} = {value}");
                        break;
                    case "!=":
                    case "<>":
                    case "ne":
                        clauses.Add($"{field} != {value}");
                        break;
                    case ">":
                    case "gt":
                        clauses.Add($"{field} > {value}");
                        break;
                    case ">=":
                    case "ge":
                        clauses.Add($"{field} >= {value}");
                        break;
                    case "<":
                    case "lt":
                        clauses.Add($"{field} < {value}");
                        break;
                    case "<=":
                    case "le":
                        clauses.Add($"{field} <= {value}");
                        break;
                    case "contains":
                    case "like":
                        clauses.Add($"{field} like \"%{EscapeExpressionText(filter.FilterValue)}%\"");
                        break;
                    case "startswith":
                        clauses.Add($"{field} like \"{EscapeExpressionText(filter.FilterValue)}%\"");
                        break;
                    case "endswith":
                        clauses.Add($"{field} like \"%{EscapeExpressionText(filter.FilterValue)}\"");
                        break;
                    case "between":
                        if (!string.IsNullOrWhiteSpace(filter.FilterValue1))
                        {
                            clauses.Add($"({field} >= {value} and {field} <= {value2})");
                        }
                        break;
                    case "isnull":
                        clauses.Add($"{field} = null");
                        break;
                    case "isnotnull":
                        clauses.Add($"{field} != null");
                        break;
                }
            }

            if (clauses.Count == 0)
            {
                return BsonExpression.Create("$");
            }

            return BsonExpression.Create(string.Join(" and ", clauses));
        }

        private string BsonExpressionValue(string value, string valueType = null, Type fieldType = null)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "null";
            }

            if (fieldType == typeof(bool) || string.Equals(valueType, "System.Boolean", StringComparison.OrdinalIgnoreCase))
            {
                return bool.TryParse(value, out var boolResult)
                    ? (boolResult ? "true" : "false")
                    : "false";
            }

            if (fieldType == typeof(DateTime) || string.Equals(valueType, "System.DateTime", StringComparison.OrdinalIgnoreCase))
            {
                if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
                {
                    return $"\"{dt:O}\"";
                }
                return $"\"{EscapeExpressionText(value)}\"";
            }

            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
            {
                return value;
            }

            return $"\"{EscapeExpressionText(value)}\"";
        }

        private static string EscapeExpressionText(string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : value.Replace("\"", "\\\"");
        }
    }
}
