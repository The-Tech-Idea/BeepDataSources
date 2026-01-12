using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>
    /// Modern .NET patterns: IAsyncEnumerable streaming, CancellationToken support, ValueTask optimizations
    /// </summary>
    public partial class RDBSource : IRDBSource
    {
        #region IAsyncEnumerable Streaming

        /// <summary>
        /// Streams entity data asynchronously using IAsyncEnumerable for memory-efficient processing
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="entityName">Table or view name</param>
        /// <param name="filters">Query filters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Async enumerable stream of entities</returns>
        public virtual async IAsyncEnumerable<T> GetEntityStreamAsync<T>(
            string entityName,
            List<AppFilter>? filters = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default) where T : class, new()
        {
            SetObjects(entityName);
            ErrorObject.Flag = Errors.Ok;

            DbCommand? cmd = null;
            DbDataReader? reader = null;

            try
            {
                cmd = GetDataCommand() as DbCommand;
                if (cmd == null)
                    throw new InvalidOperationException("Database command does not support async operations");

                string query = GetQueryString(entityName, filters);
                cmd.CommandText = query;

                if (filters != null)
                {
                    foreach (var filter in filters.Where(f => !string.IsNullOrWhiteSpace(f.FilterValue)))
                    {
                        var param = cmd.CreateParameter();
                        param.ParameterName = $"{ParameterDelimiter}p_{filter.FieldName}";
                        param.Value = filter.FilterValue;
                        cmd.Parameters.Add(param);
                    }
                }

                reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var entity = new T();
                    var properties = typeof(T).GetProperties();

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var fieldName = reader.GetName(i);
                        var property = properties.FirstOrDefault(p => 
                            p.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));

                        if (property != null && property.CanWrite)
                        {
                            var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            if (value != null)
                            {
                                try
                                {
                                    var convertedValue = Convert.ChangeType(value, 
                                        Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType);
                                    property.SetValue(entity, convertedValue);
                                }
                                catch
                                {
                                    // Skip conversion errors
                                }
                            }
                        }
                    }

                    yield return entity;
                }
            }
            finally
            {
                if (reader != null)
                    await reader.DisposeAsync();
                if (cmd != null)
                    await cmd.DisposeAsync();
            }
        }

        /// <summary>
        /// Streams raw data rows asynchronously using IAsyncEnumerable
        /// </summary>
        /// <param name="query">SQL query</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Async enumerable stream of object arrays</returns>
        public virtual async IAsyncEnumerable<object[]> ExecuteQueryStreamAsync(
            string query,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            DbCommand? cmd = null;
            DbDataReader? reader = null;

            try
            {
                cmd = GetDataCommand() as DbCommand;
                if (cmd == null)
                    throw new InvalidOperationException("Database command does not support async operations");

                cmd.CommandText = query;
                reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var values = new object[reader.FieldCount];
                    reader.GetValues(values);
                    yield return values;
                }
            }
            finally
            {
                if (reader != null)
                    await reader.DisposeAsync();
                if (cmd != null)
                    await cmd.DisposeAsync();
            }
        }

        /// <summary>
        /// Streams entity data with pagination support using IAsyncEnumerable
        /// </summary>
        public virtual async IAsyncEnumerable<T> GetEntityPagedStreamAsync<T>(
            string entityName,
            List<AppFilter>? filters,
            int pageSize = 1000,
            [EnumeratorCancellation] CancellationToken cancellationToken = default) where T : class, new()
        {
            SetObjects(entityName);
            int currentPage = 1;
            bool hasMoreData = true;

            while (hasMoreData && !cancellationToken.IsCancellationRequested)
            {
                var pagedResult = await GetEntityPagedAsync<T>(entityName, filters, currentPage, pageSize, cancellationToken);
                
                if (pagedResult?.Data == null || !pagedResult.Data.Any())
                {
                    hasMoreData = false;
                    break;
                }

                foreach (var entity in pagedResult.Data)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return entity;
                }

                hasMoreData = pagedResult.HasNextPage;
                currentPage++;
            }
        }

        #endregion

        #region Enhanced Async Methods with CancellationToken

        /// <summary>
        /// Gets paged entity data asynchronously with cancellation support
        /// </summary>
        public virtual async Task<PagedResult<T>> GetEntityPagedAsync<T>(
            string entityName,
            List<AppFilter>? filters,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default) where T : class, new()
        {
            SetObjects(entityName);
            ErrorObject.Flag = Errors.Ok;

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 20;

            var result = new PagedResult<T>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Data = new List<T>()
            };

            DbCommand? cmd = null;

            try
            {
                cmd = GetDataCommand() as DbCommand;
                if (cmd == null)
                    throw new InvalidOperationException("Database command does not support async operations");

                // Get total count
                string countQuery = GetCountQuery(entityName, filters);
                cmd.CommandText = countQuery;
                AddFilterParameters(cmd, filters);

                var totalCountObj = await cmd.ExecuteScalarAsync(cancellationToken);
                result.TotalCount = Convert.ToInt32(totalCountObj);
                result.TotalPages = (int)Math.Ceiling(result.TotalCount / (double)pageSize);

                // Get paged data
                string pagedQuery = GetPagedQuery(entityName, filters, pageNumber, pageSize);
                cmd.CommandText = pagedQuery;
                cmd.Parameters.Clear();
                AddFilterParameters(cmd, filters);

                using (var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
                {
                    var entities = new List<T>();
                    var properties = typeof(T).GetProperties();

                    while (await reader.ReadAsync(cancellationToken))
                    {
                        var entity = new T();

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var fieldName = reader.GetName(i);
                            var property = properties.FirstOrDefault(p => 
                                p.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));

                            if (property != null && property.CanWrite)
                            {
                                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                if (value != null)
                                {
                                    try
                                    {
                                        var convertedValue = Convert.ChangeType(value,
                                            Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType);
                                        property.SetValue(entity, convertedValue);
                                    }
                                    catch { }
                                }
                            }
                        }

                        entities.Add(entity);
                    }

                    result.Data = entities;
                    result.HasNextPage = pageNumber < result.TotalPages;
                    result.HasPreviousPage = pageNumber > 1;
                }
            }
            catch (Exception ex)
            {
                HandleDatabaseError(ex, entityName, "GetEntityPagedAsync");
            }
            finally
            {
                if (cmd != null)
                    await cmd.DisposeAsync();
            }

            return result;
        }

        /// <summary>
        /// Executes a scalar query asynchronously with cancellation support
        /// </summary>
        public virtual async ValueTask<T?> ExecuteScalarAsync<T>(
            string query,
            CancellationToken cancellationToken = default)
        {
            DbCommand? cmd = null;

            try
            {
                cmd = GetDataCommand() as DbCommand;
                if (cmd == null)
                    throw new InvalidOperationException("Database command does not support async operations");

                cmd.CommandText = query;
                var result = await cmd.ExecuteScalarAsync(cancellationToken);

                if (result == null || result == DBNull.Value)
                    return default;

                return (T)Convert.ChangeType(result, typeof(T));
            }
            finally
            {
                if (cmd != null)
                    await cmd.DisposeAsync();
            }
        }

        /// <summary>
        /// Executes a non-query command asynchronously with cancellation support
        /// </summary>
        public virtual async ValueTask<int> ExecuteNonQueryAsync(
            string query,
            CancellationToken cancellationToken = default)
        {
            DbCommand? cmd = null;

            try
            {
                cmd = GetDataCommand() as DbCommand;
                if (cmd == null)
                    throw new InvalidOperationException("Database command does not support async operations");

                cmd.CommandText = query;
                return await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            finally
            {
                if (cmd != null)
                    await cmd.DisposeAsync();
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets query string with filters applied
        /// </summary>
        private string GetQueryString(string entityName, List<AppFilter>? filters)
        {
            string baseQuery = $"SELECT * FROM {Dataconnection.ConnectionProp.SchemaName}{entityName}";

            if (filters != null && filters.Any())
            {
                var whereClause = string.Join(" AND ", 
                    filters.Where(f => !string.IsNullOrWhiteSpace(f.FilterValue))
                           .Select(f => $"{GetFieldName(f.FieldName)} {GetOperator(f.Operator)} {ParameterDelimiter}p_{f.FieldName}"));

                if (!string.IsNullOrWhiteSpace(whereClause))
                    baseQuery += $" WHERE {whereClause}";
            }

            return baseQuery;
        }

        /// <summary>
        /// Gets count query for pagination
        /// </summary>
        private string GetCountQuery(string entityName, List<AppFilter>? filters)
        {
            string countQuery = $"SELECT COUNT(*) FROM {Dataconnection.ConnectionProp.SchemaName}{entityName}";

            if (filters != null && filters.Any())
            {
                var whereClause = string.Join(" AND ",
                    filters.Where(f => !string.IsNullOrWhiteSpace(f.FilterValue))
                           .Select(f => $"{GetFieldName(f.FieldName)} {GetOperator(f.Operator)} {ParameterDelimiter}p_{f.FieldName}"));

                if (!string.IsNullOrWhiteSpace(whereClause))
                    countQuery += $" WHERE {whereClause}";
            }

            return countQuery;
        }

        /// <summary>
        /// Gets paged query with OFFSET/FETCH or database-specific syntax
        /// </summary>
        private string GetPagedQuery(string entityName, List<AppFilter>? filters, int pageNumber, int pageSize)
        {
            string baseQuery = GetQueryString(entityName, filters);
            int offset = (pageNumber - 1) * pageSize;

            // Add ORDER BY if not present (required for paging)
            if (!baseQuery.Contains("ORDER BY", StringComparison.OrdinalIgnoreCase))
            {
                var primaryKey = DataStruct?.PrimaryKeys?.FirstOrDefault();
                if (primaryKey != null)
                {
                    baseQuery += $" ORDER BY {GetFieldName(primaryKey.fieldname)}";
                }
                else
                {
                    // Fallback to first column
                    var firstField = DataStruct?.Fields?.FirstOrDefault();
                    if (firstField != null)
                        baseQuery += $" ORDER BY {GetFieldName(firstField.fieldname)}";
                }
            }

            // Database-specific paging syntax
            return DatasourceType switch
            {
                DataSourceType.SqlServer => $"{baseQuery} OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY",
                DataSourceType.Mysql => $"{baseQuery} LIMIT {pageSize} OFFSET {offset}",
                DataSourceType.Postgre => $"{baseQuery} LIMIT {pageSize} OFFSET {offset}",
                DataSourceType.Oracle => BuildOraclePagingQuery(baseQuery, offset, pageSize),
                DataSourceType.SqlLite => $"{baseQuery} LIMIT {pageSize} OFFSET {offset}",
                _ => $"{baseQuery} OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY"
            };
        }

        /// <summary>
        /// Builds Oracle-specific paging query using ROW_NUMBER()
        /// </summary>
        private string BuildOraclePagingQuery(string baseQuery, int offset, int pageSize)
        {
            int startRow = offset + 1;
            int endRow = offset + pageSize;

            return $@"
                SELECT * FROM (
                    SELECT a.*, ROWNUM rnum FROM (
                        {baseQuery}
                    ) a WHERE ROWNUM <= {endRow}
                ) WHERE rnum >= {startRow}";
        }

        /// <summary>
        /// Gets SQL operator from filter operator enum
        /// </summary>
        private string GetOperator(string operatorType)
        {
            return operatorType?.ToUpper() switch
            {
                "EQUALS" or "=" => "=",
                "NOTEQUALS" or "!=" or "<>" => "<>",
                "GREATERTHAN" or ">" => ">",
                "LESSTHAN" or "<" => "<",
                "GREATERTHANOREQUAL" or ">=" => ">=",
                "LESSTHANOREQUAL" or "<=" => "<=",
                "LIKE" => "LIKE",
                "IN" => "IN",
                "NOTIN" => "NOT IN",
                "ISNULL" => "IS NULL",
                "ISNOTNULL" => "IS NOT NULL",
                _ => "="
            };
        }

        /// <summary>
        /// Adds filter parameters to command
        /// </summary>
        private void AddFilterParameters(DbCommand cmd, List<AppFilter>? filters)
        {
            if (filters == null) return;

            foreach (var filter in filters.Where(f => !string.IsNullOrWhiteSpace(f.FilterValue)))
            {
                var param = cmd.CreateParameter();
                param.ParameterName = $"{ParameterDelimiter}p_{filter.FieldName}";
                param.Value = string.IsNullOrWhiteSpace(filter.FilterValue) ? (object)DBNull.Value : filter.FilterValue;
                cmd.Parameters.Add(param);
            }
        }

        #endregion
    }

    #region Supporting Types

    /// <summary>
    /// Generic paged result with type safety
    /// </summary>
    public class PagedResult<T>
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
        public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();
    }

    #endregion
}
