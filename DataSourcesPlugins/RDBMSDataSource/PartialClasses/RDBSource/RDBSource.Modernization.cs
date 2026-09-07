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
using TheTechIdea.Beep.Helpers.RDBMSHelpers;
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

                reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken).ConfigureAwait(false);

                // One entry per column that failed conversion during this read, so the log records
                // the problem once rather than once per row.
                var reportedConversionFailures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var entity = new T();
                    var properties = typeof(T).GetProperties();

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var FieldName = reader.GetName(i);
                        var property = properties.FirstOrDefault(p => 
                            p.Name.Equals(FieldName, StringComparison.OrdinalIgnoreCase));

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
                                catch (Exception convEx)
                                {
                                    // Was an empty catch: an unconvertible column left the property
                                    // at its default and every row still came back looking valid.
                                    if (reportedConversionFailures.Add(FieldName))
                                    {
                                        DMEEditor?.AddLogMessage("Beep",
                                            $"Could not convert column '{FieldName}' of '{entityName}' to " +
                                            $"{property.PropertyType.Name}: {convEx.Message}. That property is " +
                                            "left at its default value for every row of this read.",
                                            DateTime.Now, 0, entityName, Errors.Warning);
                                    }
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
                    await reader.DisposeAsync().ConfigureAwait(false);
                if (cmd != null)
                    await cmd.DisposeAsync().ConfigureAwait(false);
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
                reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken).ConfigureAwait(false);

                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
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
                    await reader.DisposeAsync().ConfigureAwait(false);
                if (cmd != null)
                    await cmd.DisposeAsync().ConfigureAwait(false);
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
                var pagedResult = await GetEntityPagedAsync<T>(entityName, filters, currentPage, pageSize, cancellationToken).ConfigureAwait(false);
                
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

                var totalCountObj = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                result.TotalCount = Convert.ToInt32(totalCountObj);
                result.TotalPages = (int)Math.Ceiling(result.TotalCount / (double)pageSize);

                // Get paged data
                string pagedQuery = GetPagedQuery(entityName, filters, pageNumber, pageSize);
                cmd.CommandText = pagedQuery;
                cmd.Parameters.Clear();
                AddFilterParameters(cmd, filters);

                using (var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken).ConfigureAwait(false))
                {
                    var entities = new List<T>();
                    var properties = typeof(T).GetProperties();

                    // One entry per column that failed conversion on this page; see the streaming
                    // read above for why this is reported once rather than once per row.
                    var pagedConversionFailures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        var entity = new T();

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var FieldName = reader.GetName(i);
                            var property = properties.FirstOrDefault(p => 
                                p.Name.Equals(FieldName, StringComparison.OrdinalIgnoreCase));

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
                                    catch (Exception convEx)
                                    {
                                        // See the streaming read above; this was `catch { }`.
                                        if (pagedConversionFailures.Add(FieldName))
                                        {
                                            DMEEditor?.AddLogMessage("Beep",
                                                $"Could not convert column '{FieldName}' of '{entityName}' to " +
                                                $"{property.PropertyType.Name}: {convEx.Message}. That property is " +
                                                "left at its default value for every row of this page.",
                                                DateTime.Now, 0, entityName, Errors.Warning);
                                        }
                                    }
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
                    await cmd.DisposeAsync().ConfigureAwait(false);
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
                var result = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

                if (result == null || result == DBNull.Value)
                    return default;

                return (T)Convert.ChangeType(result, typeof(T));
            }
            finally
            {
                if (cmd != null)
                    await cmd.DisposeAsync().ConfigureAwait(false);
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
                return await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (cmd != null)
                    await cmd.DisposeAsync().ConfigureAwait(false);
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets query string with filters applied
        /// </summary>
        private string GetQueryString(string entityName, List<AppFilter>? filters)
        {
            string baseQuery = $"SELECT * FROM {QualifyWithSchema(entityName)}";

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
            string countQuery = $"SELECT COUNT(*) FROM {QualifyWithSchema(entityName)}";

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
        /// <remarks>
        /// Paging dialect comes from <c>RDBMSHelper.GetPagingSyntax</c>, the same source the
        /// synchronous path in <c>RDBSource.Query.cs</c> uses.
        ///
        /// This used to carry its own <c>switch (DatasourceType)</c> covering five engines and
        /// defaulting to SQL Server's OFFSET/FETCH, while the shared helper covers thirteen and
        /// defaults to LIMIT/OFFSET. The two disagreed on Oracle — OFFSET/FETCH there, a ROWNUM
        /// subquery here — and roughly nineteen engines (MariaDB, Snowflake, CockroachDB, Vertica,
        /// BigQuery, Redshift, DuckDB, Databricks, Presto, Trino, Hana, Spanner and the rest) got
        /// SQL Server syntax on the async path and LIMIT/OFFSET on the sync one. One source now.
        ///
        /// Note this settles Oracle on OFFSET/FETCH, which is 12c and later. The old ROWNUM wrapper
        /// here worked on 11g, but the synchronous path already used OFFSET/FETCH, so 11g was
        /// half-broken either way. If 11g support is needed, add it to
        /// <c>RDBMSHelper.GetPagingSyntax</c> so both paths get it.
        /// </remarks>
        private string GetPagedQuery(string entityName, List<AppFilter>? filters, int pageNumber, int pageSize)
        {
            string baseQuery = GetQueryString(entityName, filters);

            // Add ORDER BY if not present (required for paging)
            if (!baseQuery.Contains("ORDER BY", StringComparison.OrdinalIgnoreCase))
            {
                var primaryKey = DataStruct?.PrimaryKeys?.FirstOrDefault();
                if (primaryKey != null)
                {
                    baseQuery += $" ORDER BY {GetFieldName(primaryKey.FieldName)}";
                }
                else
                {
                    // Fallback to first column
                    var firstField = DataStruct?.Fields?.FirstOrDefault();
                    if (firstField != null)
                        baseQuery += $" ORDER BY {GetFieldName(firstField.FieldName)}";
                }
            }

            return $"{baseQuery} {RDBMSHelper.GetPagingSyntax(DatasourceType, pageNumber, pageSize)}";
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
