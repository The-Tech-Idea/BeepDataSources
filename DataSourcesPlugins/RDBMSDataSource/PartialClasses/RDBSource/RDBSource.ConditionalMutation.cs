using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Helpers.RDBMSHelpers.DMLHelpers;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.DataBase
{
    public partial class RDBSource : IConditionalMutationDataSource, ICalendarGuardedMutationDataSource
    {
        /// <summary>Calendar guards are implemented only by the qualified relational mutation providers.</summary>
        public bool SupportsCalendarGuards => DatasourceType is DataSourceType.SqlServer or DataSourceType.AzureSQL or DataSourceType.Postgre or DataSourceType.SqlLite;

        private static void ValidateCalendarFields(IEnumerable<ConditionalRowGuard> guards, Func<string, string, string> fieldType)
        {
            foreach (var guard in guards)
            foreach (var name in new[] { guard.CalendarFromField, guard.CalendarThroughField })
            {
                if (name == null) continue;
                var declared = Type.GetType(fieldType(guard.EntityName, name) ?? string.Empty);
                if (declared == null || (Nullable.GetUnderlyingType(declared) ?? declared) != typeof(DateTime))
                    throw new NotSupportedException("Calendar guards require declared date/time fields without an offset.");
            }
        }

        /// <summary>One owned serializable transaction, with physical acknowledgement and no shared command state.</summary>
        public async Task<ConditionalMutationResponse> MutateConditionallyAsync(ConditionalMutationRequest request,
            CancellationToken cancellationToken = default)
        {
            var commitAttempted = false;
            var committed = false;
            var dispatched = false;
            try
            {
                ArgumentNullException.ThrowIfNull(request);
                cancellationToken.ThrowIfCancellationRequested();
                if (DatasourceType is not (DataSourceType.SqlServer or DataSourceType.AzureSQL or DataSourceType.Postgre or DataSourceType.SqlLite))
                    throw new NotSupportedException("Conditional mutations are not qualified for this provider.");
                var metadata = request.Guards.SelectMany(group => group.Alternatives).Select(guard => guard.EntityName)
                    .Append(request.EntityName).Distinct(StringComparer.OrdinalIgnoreCase).ToDictionary(name => name,
                        name => Entities?.SingleOrDefault(entity => string.Equals(entity.EntityName, name, StringComparison.OrdinalIgnoreCase))
                            ?? throw new NotSupportedException("Conditional mutations require declared cached entity metadata."),
                        StringComparer.OrdinalIgnoreCase);
                var fields = metadata.ToDictionary(pair => pair.Key, pair =>
                {
                    var declared = pair.Value.Fields?.ToArray() ?? throw new NotSupportedException("Mutation fields are not declared.");
                    if (declared.Length == 0 || declared.Any(field => string.IsNullOrWhiteSpace(field.FieldName)))
                        throw new NotSupportedException("Mutation fields must be nonempty.");
                    return declared.ToDictionary(field => field.FieldName, StringComparer.OrdinalIgnoreCase);
                }, StringComparer.OrdinalIgnoreCase);
                string Field(string entity, string field) => fields[entity].TryGetValue(field, out var found)
                    ? found.FieldName : throw new ArgumentException("Mutation predicates must use declared fields.");
                foreach (var field in request.Values.Keys.Concat(request.Match.Keys)) Field(request.EntityName, field);
                foreach (var guard in request.Guards.SelectMany(group => group.Alternatives))
                    foreach (var field in guard.Match.Keys) Field(guard.EntityName, field);
                ValidateCalendarFields(request.Guards.SelectMany(group => group.Alternatives),
                    (entity, field) => fields[entity].TryGetValue(field, out var declared) ? declared.Fieldtype : throw new ArgumentException("A calendar field is not declared."));
                var keys = fields[request.EntityName].Values.Where(field => field.IsKey).ToArray();
                if (keys.Length == 0) throw new NotSupportedException("Conditional mutations require a declared primary key.");
                var keyValues = request.Kind == ConditionalMutationKind.Insert ? request.Values : request.Match;
                if (keys.Any(key => !keyValues.TryGetValue(key.FieldName, out var value) || value == null || value == DBNull.Value))
                    throw new ArgumentException("A mutation must name the complete non-null primary key.");
                if (request.Kind == ConditionalMutationKind.Update && keys.Any(key => request.Values.ContainsKey(key.FieldName)))
                    throw new ArgumentException("A conditional update cannot change its primary key.");
                if (RDBMSConnection.DbConn is not DbConnection template || string.IsNullOrWhiteSpace(template.ConnectionString))
                    throw new NotSupportedException("A configured ADO.NET connection is required.");
                var connectionString = DMEEditor != null ? RDBMSConnection.ReplaceValueFromConnectionString() : template.ConnectionString;
                if (DatasourceType == DataSourceType.SqlLite && connectionString.Contains(":memory:", StringComparison.OrdinalIgnoreCase))
                    throw new NotSupportedException("Conditional SQLite mutations require a file or named shared-memory database.");
                var factory = DbProviderFactories.GetFactory(template) ?? throw new NotSupportedException("Provider has no connection factory.");
                var query = DatabaseDMLParameterizedQueries.GenerateConditionalMutationQuery(DatasourceType, request,
                    name => DatasourceType == DataSourceType.SqlLite ? metadata[name].EntityName : QualifyWithSchema(metadata[name].EntityName), Field);
                var finalCheck = request.Guards.SelectMany(group => group.Alternatives).Any(guard => guard.HasCalendarWindow)
                    ? DatabaseDMLParameterizedQueries.GenerateConditionalCalendarCommitQuery(DatasourceType, request,
                    name => DatasourceType == DataSourceType.SqlLite ? metadata[name].EntityName : QualifyWithSchema(metadata[name].EntityName), Field) : null;
                return await Task.Run(async () =>
                {
                    await using var connection = factory.CreateConnection() ?? throw new NotSupportedException("Provider cannot create a mutation connection.");
                    connection.ConnectionString = connectionString;
                    await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                    await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
                    await using var command = connection.CreateCommand();
                    command.Transaction = transaction;
                    command.CommandText = query.QueryText;
                    command.CommandTimeout = 10;
                    ConfigureCommand(command);
                    foreach (var pair in query.Parameters)
                    {
                        var parameter = command.CreateParameter();
                        parameter.ParameterName = (DatasourceType == DataSourceType.SqlLite ? "$" : "@") + pair.Key;
                        RdbParameters.Bind(parameter, pair.Value);
                        command.Parameters.Add(parameter);
                    }
                    dispatched = true;
                    var changed = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    if (changed == 0)
                    {
                        await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                        return new ConditionalMutationResponse(request.RequestId, ConditionalMutationStatus.NoMatch);
                    }
                    if (changed != 1)
                    {
                        await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                        throw new InvalidOperationException("The provider did not acknowledge exactly one changed row.");
                    }
                    if (finalCheck != null)
                    await using (var check = connection.CreateCommand())
                    {
                        check.Transaction = transaction; check.CommandText = finalCheck.QueryText; check.CommandTimeout = 10; ConfigureCommand(check);
                        foreach (var pair in finalCheck.Parameters)
                        {
                            var parameter = check.CreateParameter();
                            parameter.ParameterName = (DatasourceType == DataSourceType.SqlLite ? "$" : "@") + pair.Key;
                            RdbParameters.Bind(parameter, pair.Value); check.Parameters.Add(parameter);
                        }
                        if (await check.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) == null)
                        {
                            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                            return new ConditionalMutationResponse(request.RequestId, ConditionalMutationStatus.NoMatch);
                        }
                    }
                    // Cancellation before this point rolls back. Once commit starts, a failure is uncertain.
                    cancellationToken.ThrowIfCancellationRequested();
                    commitAttempted = true;
                    await transaction.CommitAsync(CancellationToken.None).ConfigureAwait(false);
                    committed = true;
                    InvalidateEntityCache(request.EntityName);
                    return new ConditionalMutationResponse(request.RequestId, ConditionalMutationStatus.Applied);
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage(nameof(RDBSource), "Conditional mutation failed: " + ex,
                    DateTime.Now, -1, request?.EntityName, Errors.Failed);
                HandleDatabaseError(ex, request?.EntityName ?? nameof(RDBSource), "conditional mutation");
                return new ConditionalMutationResponse(request?.RequestId ?? Guid.Empty,
                    committed ? ConditionalMutationStatus.Applied : commitAttempted || dispatched
                        ? ConditionalMutationStatus.Unconfirmed : ConditionalMutationStatus.NotApplied, ex);
            }
        }
    }
}
