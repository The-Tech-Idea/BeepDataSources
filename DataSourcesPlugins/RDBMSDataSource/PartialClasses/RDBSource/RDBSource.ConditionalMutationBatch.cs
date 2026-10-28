using System;
using TheTechIdea.Beep.Extensions;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Helpers.RDBMSHelpers.DMLHelpers;

namespace TheTechIdea.Beep.DataBase
{
    public partial class RDBSource : IConditionalMutationBatchDataSource, IDateRangeGuardedMutationBatchDataSource, IOrdinalIgnoreCaseTextDataSource, IProjectionGuardedMutationBatchDataSource
    {
        /// <summary>Physical date-range cohorts are enforced by the qualified conditional batch providers.</summary>
        public bool SupportsDateRangeGuards => DatasourceType is DataSourceType.SqlServer or DataSourceType.AzureSQL or DataSourceType.Postgre or DataSourceType.SqlLite;
        /// <summary>Current-runtime ordinal casing is compiled identically for native reads and cohorts.</summary>
        public bool SupportsOrdinalIgnoreCaseText => DatasourceType is DataSourceType.SqlServer or DataSourceType.AzureSQL or DataSourceType.SqlLite;
        /// <summary>One isolated serializable transaction for the complete guarded batch.</summary>
        public async Task<ConditionalMutationBatchResponse> MutateBatchConditionallyAsync(ConditionalMutationBatchRequest request, CancellationToken cancellationToken = default)
            => await MutateBatchCoreAsync(request,Array.Empty<ConditionalStoredProjectionGuard>(),cancellationToken).ConfigureAwait(false);

        /// <summary>Provider families with an implemented serializable bounded projection path; qualify each deployment separately.</summary>
        public bool SupportsProjectionGuardedBatches => DatasourceType is DataSourceType.SqlServer or DataSourceType.AzureSQL or DataSourceType.Postgre or DataSourceType.SqlLite;
        /// <summary>Reuse the batch owner and check complete source projections before writes and commit.</summary>
        public async Task<ConditionalMutationBatchResponse> MutateProjectionGuardedBatchAsync(ProjectionGuardedMutationBatchRequest request,CancellationToken cancellationToken=default)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (request.CohortComparisons.Count > 0)
                throw new NotSupportedException("Complete cohort comparisons are not implemented by this provider.");
            return await MutateBatchCoreAsync(request.Batch,request.ProjectionGuards,cancellationToken).ConfigureAwait(false);
        }

        private async Task<ConditionalMutationBatchResponse> MutateBatchCoreAsync(ConditionalMutationBatchRequest? request,
            System.Collections.Generic.IReadOnlyList<ConditionalStoredProjectionGuard> projectionGuards,CancellationToken cancellationToken)
        {
            var dispatched = false; var commitAttempted = false; var committed = false; var rolledBack = false; var operationCount = 0;
            try
            {
                ArgumentNullException.ThrowIfNull(request); operationCount = request.Mutations.Count; cancellationToken.ThrowIfCancellationRequested();
                if (DatasourceType is not (DataSourceType.SqlServer or DataSourceType.AzureSQL or DataSourceType.Postgre or DataSourceType.SqlLite))
                    throw new NotSupportedException("Atomic batches are not qualified for this provider.");
                if (request.Sets.Any(set => set.TextEqualities.Count > 0) && !SupportsOrdinalIgnoreCaseText)
                    throw new NotSupportedException("Native ordinal case-insensitive text cohorts are unavailable.");
                var guards = request.Guards.Concat(request.CommitGuards).Concat(request.Mutations.SelectMany(op => op.Guards)).SelectMany(group => group.Alternatives).ToArray();
                var names = guards.Select(guard => guard.EntityName).Concat(request.Sets.Select(set => set.EntityName)).Concat(request.Mutations.Select(op => op.EntityName));
                var metadata = names.Distinct(StringComparer.OrdinalIgnoreCase).ToDictionary(name => name,
                    name => Entities?.SingleOrDefault(entity => string.Equals(entity.EntityName, name, StringComparison.OrdinalIgnoreCase))
                        ?? throw new NotSupportedException("Atomic batches require cached declared metadata."), StringComparer.OrdinalIgnoreCase);
                var fields = metadata.ToDictionary(pair => pair.Key, pair =>
                {
                    var declared = pair.Value.Fields?.ToArray() ?? throw new NotSupportedException("Batch fields are not declared.");
                    if (declared.Length == 0 || declared.Any(field => string.IsNullOrWhiteSpace(field.FieldName))) throw new NotSupportedException("Batch fields must be nonempty.");
                    return declared.ToDictionary(field => field.FieldName, StringComparer.OrdinalIgnoreCase);
                }, StringComparer.OrdinalIgnoreCase);
                string Field(string entity, string field) => fields[entity].TryGetValue(field, out var found) ? found.FieldName : throw new ArgumentException("Batch predicates must use declared fields.");
                string Table(string entity) => DatasourceType == DataSourceType.SqlLite ? metadata[entity].EntityName : QualifyWithSchema(metadata[entity].EntityName);
                ValidateCalendarFields(guards, (entity, field) => fields[entity].TryGetValue(field, out var declared)
                    ? declared.Fieldtype : throw new ArgumentException("A calendar field is not declared."));
                foreach (var set in request.Sets)
                foreach (var range in set.DateRanges)
                {
                    Field(set.EntityName, range.FieldName);
                    var declared = Type.GetType(fields[set.EntityName][range.FieldName].Fieldtype ?? string.Empty);
                    if (declared == null || (Nullable.GetUnderlyingType(declared) ?? declared) != typeof(DateTime))
                        throw new NotSupportedException("Date-range cohorts require declared DateTime fields without an offset.");
                }
                foreach (var set in request.Sets)
                foreach (var text in set.TextEqualities)
                {
                    Field(set.EntityName, text.FieldName);
                    if (Type.GetType(fields[set.EntityName][text.FieldName].Fieldtype ?? string.Empty) != typeof(string))
                        throw new NotSupportedException("Ordinal text cohorts require a declared string column.");
                }
                foreach (var op in request.Mutations)
                {
                    foreach (var name in op.Values.Keys.Concat(op.Match.Keys)) Field(op.EntityName, name);
                    var keys = fields[op.EntityName].Values.Where(field => field.IsKey).ToArray();
                    var values = op.Kind == ConditionalMutationKind.Insert ? op.Values : op.Match;
                    if (keys.Length == 0 || keys.Any(key => !values.TryGetValue(key.FieldName, out var value) || value == null || value == DBNull.Value))
                        throw new ArgumentException("Every batch operation must name its full non-null primary key.");
                    if (op.Kind == ConditionalMutationKind.Update && keys.Any(key => op.Values.ContainsKey(key.FieldName))) throw new ArgumentException("A batch cannot replace primary keys.");
                }
                var preflight = DatabaseDMLParameterizedQueries.GenerateConditionalBatchGuardQueries(DatasourceType, request, Table, Field);
                var finalCheck = DatabaseDMLParameterizedQueries.GenerateConditionalBatchGuardQueries(DatasourceType, request, Table, Field, true);
                var queries = request.Mutations.Select(op => DatabaseDMLParameterizedQueries.GenerateConditionalMutationQuery(DatasourceType, op, Table, Field)).ToArray();
                var projectionPlans=projectionGuards.Select(PrepareProjectionGuardPlan).ToArray();
                if (RDBMSConnection.DbConn is not DbConnection template || string.IsNullOrWhiteSpace(template.ConnectionString)) throw new NotSupportedException("A configured ADO.NET connection is required.");
                var connectionString = DMEEditor != null ? RDBMSConnection.ReplaceValueFromConnectionString() : template.ConnectionString;
                if (DatasourceType == DataSourceType.SqlLite && connectionString.Contains(":memory:", StringComparison.OrdinalIgnoreCase)) throw new NotSupportedException("Atomic SQLite batches require a file or named shared-memory database.");
                var factory = DbProviderFactories.GetFactory(template) ?? throw new NotSupportedException("Provider has no connection factory.");
                return await Task.Run(async () =>
                {
                    await using var connection = factory.CreateConnection() ?? throw new NotSupportedException("Provider cannot create a batch connection.");
                    connection.ConnectionString = connectionString; await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                    await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
                    DbCommand Command(AppFilterQueryDefinition query)
                    {
                        var command = connection.CreateCommand(); command.Transaction = transaction; command.CommandText = query.QueryText; command.CommandTimeout = 10; ConfigureCommand(command);
                        foreach (var pair in query.Parameters) { var parameter = command.CreateParameter(); parameter.ParameterName = (DatasourceType == DataSourceType.SqlLite ? "$" : "@") + pair.Key; RdbParameters.Bind(parameter, pair.Value); command.Parameters.Add(parameter); }
                        return command;
                    }
                    async Task<ConditionalMutationBatchResponse> NoMatch(string boundary)
                    {
                        await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false); rolledBack = true;
                        return new ConditionalMutationBatchResponse(request.RequestId, ConditionalMutationBatchStatus.NoMatch, 0, new InvalidOperationException("The observed batch did not match at " + boundary + "."));
                    }
                    try
                    {
                        foreach (var observation in preflight)
                        { await using var check = Command(observation); if (await check.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) == null) return await NoMatch("preflight").ConfigureAwait(false); }
                        foreach(var plan in projectionPlans)
                        { if(!await CheckProjectionGuardAsync(plan,connection,transaction,cancellationToken).ConfigureAwait(false))return await NoMatch("source projection").ConfigureAwait(false); }
                        foreach (var query in queries)
                        {
                            await using var command = Command(query); dispatched = true;
                            var changed = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                            if (changed == 0) return await NoMatch("operation").ConfigureAwait(false);
                            if (changed != 1) throw new InvalidOperationException("Every batch operation must acknowledge exactly one changed row.");
                        }
                        foreach (var observation in finalCheck)
                        { await using var check = Command(observation); if (await check.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) == null) return await NoMatch("commit authority").ConfigureAwait(false); }
                        foreach(var plan in projectionPlans)
                        { if(!await CheckProjectionGuardAsync(plan,connection,transaction,cancellationToken).ConfigureAwait(false))return await NoMatch("commit source projection").ConfigureAwait(false); }
                        cancellationToken.ThrowIfCancellationRequested(); commitAttempted = true;
                        await transaction.CommitAsync(CancellationToken.None).ConfigureAwait(false); committed = true;
                        foreach (var name in request.Mutations.Select(op => op.EntityName).Distinct(StringComparer.OrdinalIgnoreCase)) InvalidateEntityCache(name);
                        return new ConditionalMutationBatchResponse(request.RequestId, ConditionalMutationBatchStatus.Applied, request.Mutations.Count);
                    }
                    catch (Exception failure)
                    {
                        if (!commitAttempted && !committed && !rolledBack)
                        {
                            try { await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false); rolledBack = true; }
                            catch (Exception rollbackFailure)
                            {
                                DMEEditor?.AddLogMessage(nameof(RDBSource), "Batch rollback failed: " + rollbackFailure, DateTime.Now, -1, null, Errors.Failed);
                                throw new AggregateException(failure, rollbackFailure);
                            }
                        }
                        throw;
                    }
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage(nameof(RDBSource), "Conditional batch failed: " + ex, DateTime.Now, -1, null, Errors.Failed);
                HandleDatabaseError(ex, request?.Mutations.FirstOrDefault()?.EntityName ?? nameof(RDBSource), "conditional batch");
                var status = committed ? ConditionalMutationBatchStatus.Applied : commitAttempted ? ConditionalMutationBatchStatus.Unconfirmed :
                    rolledBack ? ConditionalMutationBatchStatus.RolledBack : dispatched ? ConditionalMutationBatchStatus.Unconfirmed : ConditionalMutationBatchStatus.NotApplied;
                return new ConditionalMutationBatchResponse(request?.RequestId ?? Guid.Empty, status, committed ? operationCount : status == ConditionalMutationBatchStatus.Unconfirmed ? null : 0, ex);
            }
        }
    }
}
