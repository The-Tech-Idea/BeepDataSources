using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Extensions;
using TheTechIdea.Beep.Helpers.RDBMSHelpers.DMLHelpers;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.DataBase
{
    public partial class RDBSource : IBoundedComparedRelatedPagedDataSource, IBoundedGroupedRelatedPagedDataSource, IBoundedSnapshotPagedDataSource
    {
        /// <summary>A dedicated read connection owns one coherent count/page snapshot.
        /// The awaited worker also covers synchronous ADO.NET drivers; cancellation never detaches it.</summary>
        public async Task<BoundedPageResponse> ReadPageAsync(string entityName, BoundedPageRequest request,
            CancellationToken cancellationToken = default)
            => (await ReadBoundedPagesCoreAsync(entityName,request,null,cancellationToken).ConfigureAwait(false)).Page;

        /// <summary>All bounded batches share one native transaction and one count. No total-row cap is imposed.</summary>
        public async Task<SnapshotReadCompletion> VisitSnapshotPagesAsync(string entityName, BoundedPageRequest firstPage,
            Action<BoundedPageResponse> consume, CancellationToken cancellationToken=default)
        {
            ArgumentNullException.ThrowIfNull(firstPage);ArgumentNullException.ThrowIfNull(consume);
            if(firstPage.PageNumber!=1)throw new ArgumentException("A complete traversal must begin at page one.");
            return (await ReadBoundedPagesCoreAsync(entityName,firstPage,consume,cancellationToken).ConfigureAwait(false)).Completion
                ?? throw new InvalidOperationException("Snapshot traversal was not acknowledged.");
        }

        private async Task<(BoundedPageResponse Page,SnapshotReadCompletion? Completion)> ReadBoundedPagesCoreAsync(
            string entityName,BoundedPageRequest request,Action<BoundedPageResponse>? consume,CancellationToken cancellationToken)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(request);
                cancellationToken.ThrowIfCancellationRequested();
                var isolation = DatasourceType == DataSourceType.Postgre ? IsolationLevel.RepeatableRead :
                    DatasourceType is DataSourceType.SqlLite or DataSourceType.SqlServer or DataSourceType.AzureSQL
                        ? IsolationLevel.Serializable : throw new NotSupportedException("Strict paging is not qualified for this provider.");
                var entity = Entities?.SingleOrDefault(item => string.Equals(item.EntityName, entityName, StringComparison.OrdinalIgnoreCase))
                    ?? throw new NotSupportedException("Strict paging requires cached declared entity metadata.");
                var fields = entity.Fields?.ToArray() ?? throw new NotSupportedException("Page fields are not declared.");
                var names = fields.Select(field => field.FieldName).ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (fields.Length == 0 || names.Count != fields.Length || names.Any(string.IsNullOrWhiteSpace))
                    throw new NotSupportedException("Page fields must be unique and nonempty.");
                if (request.Order.Count == 0 || request.Order.Select(order => order.FieldName).Distinct(StringComparer.OrdinalIgnoreCase).Count() != request.Order.Count ||
                    request.Order.Any(order => !names.Contains(order.FieldName)) ||
                    !fields.Any(field => field.IsKey) ||
                    fields.Where(field => field.IsKey).Any(key => !request.Order.Any(order => string.Equals(order.FieldName, key.FieldName, StringComparison.OrdinalIgnoreCase))))
                    throw new ArgumentException("Page order must use declared fields and the complete primary key.");
                if (request.CopyFilters().Concat(request.CopyAnyFilters()).Any(filter => !names.Contains(filter.FieldName)))
                    throw new ArgumentException("Page filters must use declared field names.");
                if (RDBMSConnection.DbConn is not DbConnection template || string.IsNullOrWhiteSpace(template.ConnectionString))
                    throw new NotSupportedException("A configured ADO.NET connection is required for strict paging.");
                var connectionString = DMEEditor != null ? RDBMSConnection.ReplaceValueFromConnectionString() : template.ConnectionString;
                if (DatasourceType == DataSourceType.SqlLite && connectionString.Contains(":memory:", StringComparison.OrdinalIgnoreCase))
                    throw new NotSupportedException("Strict SQLite paging requires a file or a named shared-memory database.");
                var factory = DbProviderFactories.GetFactory(template) ?? throw new NotSupportedException("Provider has no connection factory.");
                var qualifiedEntity = DatasourceType == DataSourceType.SqlLite ? entity.EntityName : QualifyWithSchema(entity.EntityName);
                var quotedEntity = DataSourceAppFilterExtensions.QuoteQueryIdentifier(qualifiedEntity, DatasourceType);
                var physicalNames = fields.ToDictionary(field => field.FieldName, field => field.FieldName, StringComparer.OrdinalIgnoreCase);
                var requiredFilters = request.CopyFilters();
                var anyFilters = request.CopyAnyFilters();
                foreach (var filter in requiredFilters.Concat(anyFilters)) filter.FieldName = physicalNames[filter.FieldName];
                // This query-only copy resolves metadata casing; the response retains the original request ID.
                var queryRequest = new BoundedPageRequest(request.PageNumber, request.PageSize, request.MaxPayloadBytes,
                    request.Order.Select(order => new PageOrder(physicalNames[order.FieldName], order.Descending)), requiredFilters, anyFilters, request.AnyRelatedFilters, request.RequiredRelatedGroups);
                var query = this.BuildBoundedSelectQueryDefinition(quotedEntity, queryRequest, DatabaseDMLUtilities.GetBoundedPageProjection(DatasourceType, fields, request.MaxPayloadBytes), entity,
                    name => DataSourceAppFilterExtensions.QuoteQueryIdentifier(DatasourceType == DataSourceType.SqlLite ? name : QualifyWithSchema(name), DatasourceType));
                var orderSql = string.Join(", ", request.Order.Select(order =>
                {
                    var quoted = DataSourceAppFilterExtensions.QuoteQueryIdentifier(physicalNames[order.FieldName], DatasourceType);
                    var declared = Type.GetType(fields.Single(field => string.Equals(field.FieldName, order.FieldName, StringComparison.OrdinalIgnoreCase)).Fieldtype ?? string.Empty, false);
                    var expression = declared != null && (Nullable.GetUnderlyingType(declared) ?? declared) == typeof(DateTime)
                        ? DataSourceAppFilterExtensions.DateTimeComparisonExpression(quoted, DatasourceType) : quoted;
                    return expression + (order.Descending ? " DESC" : " ASC");
                }));
                var countSql = DatabaseDMLUtilities.GetBoundedRecordCountQuery(DatasourceType, quotedEntity, query.WhereClause);
                // Task.Run owns the entire lifetime; it is awaited without WaitAsync or early cancellation return.
                return await Task.Run(async () =>
                {
                    await using var connection = factory.CreateConnection() ?? throw new NotSupportedException("Provider cannot create an independent read connection.");
                    connection.ConnectionString = connectionString;
                    await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                    await using var transaction = await connection.BeginTransactionAsync(isolation, cancellationToken).ConfigureAwait(false);
                    DbCommand Command(string sql)
                    {
                        var command = connection.CreateCommand();
                        command.Transaction = transaction;
                        command.CommandText = sql;
                        ConfigureCommand(command);
                        foreach (var pair in query.Parameters)
                        {
                            var parameter = command.CreateParameter();
                            parameter.ParameterName = this.GetParameterPrefix() + pair.Key;
                            RdbParameters.Bind(parameter, pair.Value);
                            command.Parameters.Add(parameter);
                        }
                        return command;
                    }
                    long total;
                    await using (var count = Command(countSql))
                    {
                        var value = await count.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                        if (value == null || value == DBNull.Value) throw new InvalidOperationException("Provider returned no count.");
                        total = Convert.ToInt64(value, CultureInfo.InvariantCulture);
                        if (total < 0) throw new InvalidOperationException("Provider returned a negative count.");
                    }
                    var current=request;long pages=0,visited=0;BoundedPageResponse response=null!;
                    do
                    {
                    var pageSql=query.QueryText+" ORDER BY "+orderSql+" "+
                        DatabaseDMLUtilities.GetBoundedPagingSyntax(DatasourceType,current.Offset,current.PageSize);
                    using var payload = new PagePayloadStream(request.MaxPayloadBytes);
                    using (var writer = new Utf8JsonWriter(payload))
                    {
                        writer.WriteStartArray();
                        var rows = 0;
                        await using var command = Command(pageSql);
                        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken).ConfigureAwait(false);
                        if (reader.FieldCount != fields.Length + 1) throw new InvalidOperationException("Provider page schema differs from declared fields.");
                        var types = new Type[reader.FieldCount];
                        for (var column = 1; column < reader.FieldCount; column++)
                        {
                            var field = fields.SingleOrDefault(field => string.Equals(field.FieldName, reader.GetName(column), StringComparison.OrdinalIgnoreCase))
                                ?? throw new InvalidOperationException("Provider returned an undeclared column.");
                            types[column] = Type.GetType(field.Fieldtype ?? "") ?? throw new NotSupportedException("Page field type is not qualified.");
                        }
                        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            if (reader.GetInt32(0) != 0) throw new InvalidOperationException("A provider cell exceeds the page byte bound.");
                            if (++rows > request.PageSize) throw new InvalidOperationException("Provider exceeded the page row bound.");
                            writer.WriteStartObject();
                            for (var column = 1; column < reader.FieldCount; column++)
                            {
                                writer.WritePropertyName(reader.GetName(column));
                                var value = ReadBoundedScalar(reader, column, types[column], request.MaxPayloadBytes);
                                // Metadata often declares the underlying scalar type for nullable columns.
                                // Preserve a physical database NULL without asking a non-nullable converter to box it.
                                if (value is null) writer.WriteNullValue();
                                else JsonSerializer.Serialize(writer, value, types[column]);
                                writer.Flush(); // Enforce encoded byte bounds at each field, not after an unbounded rowset.
                            }
                            writer.WriteEndObject();
                        }
                        var expected = current.Offset >= total ? 0 : (int)Math.Min(current.PageSize, total - current.Offset);
                        if (rows != expected) throw new InvalidOperationException("Count and page do not form a coherent snapshot.");
                        writer.WriteEndArray();
                        writer.Flush();
                    }
                    response=new BoundedPageResponse(current.RequestId,current.PageNumber,current.PageSize,total,payload.ToArray());
                    consume?.Invoke(response);
                    pages=checked(pages+1);visited=checked(visited+(current.Offset>=total?0:Math.Min(current.PageSize,total-current.Offset)));
                    cancellationToken.ThrowIfCancellationRequested();
                    if(consume is null || current.Offset>=total || total-current.Offset<=current.PageSize)break;
                    current=current.ForPage(checked(current.PageNumber+1));
                    } while(true);
                    await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                    return (response,consume is null ? null : new SnapshotReadCompletion(request.RequestId,total,visited,pages));
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                HandleDatabaseError(ex, entityName, "read bounded page");
                throw;
            }
        }

        private object? ReadBoundedScalar(DbDataReader reader, int column, Type declaredType, int byteBound)
        {
            if (reader.IsDBNull(column)) return null;
            var type = Nullable.GetUnderlyingType(declaredType) ?? declaredType;
            // The SQL projection has already excluded oversized cells. Avoid GetChars/GetBytes
            // streaming metadata: SQLite expression columns have no origin table, and some drivers
            // require one for their streaming API. Materialize only the admitted bounded scalar.
            if (type == typeof(string))
            {
                var text = reader.GetString(column);
                if (System.Text.Encoding.UTF8.GetByteCount(text) > byteBound)
                    throw new InvalidOperationException("String field exceeds its UTF-8 byte bound.");
                return text;
            }
            if (type == typeof(byte[]))
            {
                var bytes = reader.GetFieldValue<byte[]>(column);
                if (bytes.Length > byteBound / 4L * 3L)
                    throw new InvalidOperationException("Binary field exceeds its base64 page bound.");
                return bytes;
            }
            if (type == typeof(decimal)) return reader.GetDecimal(column);
            if (type == typeof(DateTime))
            {
                // SQLite text dates retain their explicit marker in both normal and bounded reads.
                if (DatasourceType == DataSourceType.SqlLite)
                    return DateTime.Parse(reader.GetString(column), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                return reader.GetDateTime(column);
            }
            if (type == typeof(DateTimeOffset)) return reader.GetFieldValue<DateTimeOffset>(column);
            if (type == typeof(Guid)) return reader.GetGuid(column);
            if (type == typeof(bool)) return reader.GetBoolean(column);
            if (type == typeof(TimeSpan)) return reader.GetFieldValue<TimeSpan>(column);
            if (type == typeof(char)) return reader.GetChar(column);
            if (type.IsEnum) return Enum.ToObject(type, reader.GetInt64(column));
            if (type.IsPrimitive)
                return Convert.ChangeType(reader.GetValue(column), type, CultureInfo.InvariantCulture);
            throw new NotSupportedException("Only scalar fields can be paged.");
        }

        private sealed class PagePayloadStream : MemoryStream
        {
            private readonly int _limit;
            public PagePayloadStream(int limit) : base(new byte[limit], 0, limit, true, true)
            { _limit = limit; SetLength(0); }
            public override void Write(byte[] buffer, int offset, int count)
            { Check(count); base.Write(buffer, offset, count); }
            public override void Write(ReadOnlySpan<byte> buffer)
            { Check(buffer.Length); base.Write(buffer); }
            private void Check(int count)
            { if (Position + count > _limit) throw new InvalidOperationException("Encoded page exceeds its byte bound."); }
        }
    }
}
