using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Helpers.RDBMSHelpers;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>
    /// Provides high-performance bulk operations for RDBSource with batching and progress reporting
    /// </summary>
    public partial class RDBSource : IRDBSource
    {
        #region Bulk Operation Configuration

        /// <summary>
        /// Default batch size for bulk operations. Can be overridden per operation.
        /// </summary>
        public int DefaultBatchSize { get; set; } = 1000;

        /// <summary>
        /// Maximum parameters per batch to avoid exceeding database limits
        /// SQL Server: 2100, MySQL: unlimited, PostgreSQL: ~32767, Oracle: ~32K, SQLite: 999
        /// </summary>
        public int MaxParametersPerBatch { get; set; } = 2000;

        /// <summary>
        /// Enable bulk operation optimizations (multi-row INSERT, temp tables for UPDATE)
        /// </summary>
        public bool EnableBulkOptimizations { get; set; } = true;

        /// <summary>
        /// Use transactions for bulk operations to ensure atomicity
        /// </summary>
        public bool UseBulkTransactions { get; set; } = true;

        #endregion

        #region Bulk Transaction Scope

        /// <summary>
        /// Opens a transaction for a bulk batch and <b>publishes</b> it as this datasource's
        /// <see cref="ActiveTransaction"/>, so commands created through <see cref="GetDataCommand"/>
        /// carry it. Returns null when there is nothing to open.
        /// </summary>
        /// <remarks>
        /// Publishing is the whole point. The batched paths do their work through
        /// <c>InsertEntity</c>/<c>UpdateEntity</c>, which build their commands with
        /// <c>GetDataCommand()</c> — and that only attaches <see cref="ActiveTransaction"/>. A
        /// transaction held in a local variable was therefore invisible to them, so every command
        /// ran with <c>cmd.Transaction</c> unset while the connection had a pending local
        /// transaction. Providers that enforce the association reject that outright:
        ///
        ///   "ExecuteNonQuery requires the command to have a transaction when the connection
        ///    assigned to the command is in a pending local transaction."
        ///
        /// Since <c>UseBulkTransactions</c> defaults to true and Oracle takes the batched path
        /// (it is absent from <see cref="SupportsMultiRowInsert"/>), Oracle bulk insert failed out
        /// of the box. Only SQLite survived, because it does not enforce the association.
        ///
        /// Returns null when the caller already owns a transaction, so a bulk operation inside an
        /// outer <c>BeginTransaction</c>/<c>Commit</c> scope joins it instead of opening a second
        /// one — most providers throw on a nested local transaction, and the previous code also
        /// overwrote <c>cmd.Transaction</c>, silently taking the work out of the caller's scope.
        /// </remarks>
        private IDbTransaction BeginPublishedBulkTransaction()
        {
            if (!UseBulkTransactions)
                return null;

            // The caller owns one already — join it rather than nesting.
            if (ActiveTransaction != null)
                return null;

            if (RDBMSConnection?.DbConn == null || RDBMSConnection.DbConn.State != ConnectionState.Open)
                return null;

            var transaction = RDBMSConnection.DbConn.BeginTransaction();
            _activeTransaction = transaction;
            return transaction;
        }

        /// <summary>
        /// Completes a transaction opened by <see cref="BeginPublishedBulkTransaction"/> and stops
        /// publishing it. Safe to call with null.
        /// </summary>
        private void EndPublishedBulkTransaction(IDbTransaction transaction, bool commit)
        {
            if (transaction == null)
                return;

            try
            {
                if (commit)
                    transaction.Commit();
                else
                    transaction.Rollback();
            }
            finally
            {
                // Only clear it if it is still ours; never strand a caller's transaction.
                if (ReferenceEquals(_activeTransaction, transaction))
                    _activeTransaction = null;

                transaction.Dispose();
            }
        }

        /// <summary>
        /// Rolls back without letting the rollback's own failure replace the exception that caused it.
        /// </summary>
        private void RollbackPublishedBulkTransaction(IDbTransaction transaction)
        {
            if (transaction == null)
                return;

            try
            {
                EndPublishedBulkTransaction(transaction, commit: false);
            }
            catch (Exception rollbackEx)
            {
                // Rollback on a dead connection throws, and rethrowing here would replace the
                // original failure with a misleading one.
                Logger?.WriteLog($"Bulk rollback failed for {DatasourceName}: {rollbackEx.Message}");
            }
        }

        /// <summary>
        /// Resolves a column's SQL type for temp-table DDL.
        /// </summary>
        /// <remarks>
        /// The three temp-table builders emitted <c>f.Fieldtype</c> straight into the CREATE
        /// statement, but that holds a .NET type name — so the DDL read
        /// <c>CREATE TABLE #t (Name System.String, Id System.Int32)</c>, which every server rejects.
        /// With <c>EnableBulkOptimizations</c> defaulting to true, <c>BulkUpdateEntities</c>
        /// therefore failed on its first statement on SQL Server, MySQL and PostgreSQL — the three
        /// engines <see cref="SupportsTempTables"/> claims to support.
        ///
        /// <c>GenerateCreateEntityScript</c> already solved this; this routes the temp-table
        /// builders through the same two helpers rather than repeating the mapping.
        /// </remarks>
        private string ResolveDdlColumnType(EntityField field)
        {
            string dbType = field?.Fieldtype;

            if (string.IsNullOrWhiteSpace(dbType) ||
                dbType.StartsWith("System.", StringComparison.OrdinalIgnoreCase))
            {
                dbType = GetFallbackDbType(field?.Fieldtype, DatasourceType);
            }

            return NormalizeDbTypeForProvider(dbType, DatasourceType);
        }

        #endregion

        #region Bulk Insert Operations

        /// <summary>
        /// Inserts multiple entities in batches with optimized multi-row INSERT syntax
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="entityName">Table name</param>
        /// <param name="entities">Collection of entities to insert</param>
        /// <param name="progress">Progress reporting callback</param>
        /// <param name="batchSize">Override default batch size (0 = use default)</param>
        /// <returns>ErrorsInfo with operation result</returns>
        public virtual IErrorsInfo BulkInsertEntities<T>(
            string entityName, 
            IEnumerable<T> entities, 
            IProgress<PassedArgs>? progress = null,
            int batchSize = 0)
        {
            if (batchSize <= 0)
                batchSize = DefaultBatchSize;

            SetObjects(entityName);
            ErrorObject.Flag = Errors.Ok;

            if (DataStruct == null || DataStruct.Fields == null)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Entity structure not found for '{entityName}'";
                return ErrorObject;
            }

            var entitiesList = entities.ToList();
            if (!entitiesList.Any())
            {
                ErrorObject.Message = "No entities to insert";
                return ErrorObject;
            }

            int totalRows = entitiesList.Count;
            int successfulRows = 0;

            try
            {
                // Calculate optimal batch size based on parameter limits
                int fieldsCount = DataStruct.Fields.Count(f => !f.IsAutoIncrement);
                int optimalBatchSize = Math.Min(batchSize, MaxParametersPerBatch / Math.Max(fieldsCount, 1));

                if (EnableBulkOptimizations && SupportsMultiRowInsert())
                {
                    // Use optimized multi-row INSERT syntax
                    successfulRows = BulkInsertMultiRow(entityName, entitiesList, optimalBatchSize, progress);
                }
                else
                {
                    // Fallback to batched single-row INSERTs
                    successfulRows = BulkInsertBatched(entityName, entitiesList, optimalBatchSize, progress);
                }

                // Invalidate cache after successful bulk insert
                InvalidateEntityCache(entityName);

                ErrorObject.Message = $"Bulk insert completed: {successfulRows}/{totalRows} rows inserted";
                ErrorObject.Flag = successfulRows == totalRows ? Errors.Ok : Errors.Failed;
            }
            catch (Exception ex)
            {
                HandleDatabaseError(ex, entityName, "BulkInsert");
                ErrorObject.Message += $" ({successfulRows}/{totalRows} rows inserted before error)";
            }

            return ErrorObject;
        }

        /// <summary>
        /// Async version of bulk insert
        /// </summary>
        public virtual async Task<IErrorsInfo> BulkInsertEntitiesAsync<T>(
            string entityName,
            IEnumerable<T> entities,
            IProgress<PassedArgs>? progress = null,
            int batchSize = 0,
            CancellationToken cancellationToken = default)
        {
            if (batchSize <= 0)
                batchSize = DefaultBatchSize;

            SetObjects(entityName);
            ErrorObject.Flag = Errors.Ok;

            var entitiesList = entities.ToList();
            if (!entitiesList.Any())
            {
                ErrorObject.Message = "No entities to insert";
                return ErrorObject;
            }

            int totalRows = entitiesList.Count;
            int successfulRows = 0;

            try
            {
                int fieldsCount = DataStruct.Fields.Count(f => !f.IsAutoIncrement);
                int optimalBatchSize = Math.Min(batchSize, MaxParametersPerBatch / Math.Max(fieldsCount, 1));

                if (EnableBulkOptimizations && SupportsMultiRowInsert())
                {
                    successfulRows = await BulkInsertMultiRowAsync(entityName, entitiesList, optimalBatchSize, progress, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    successfulRows = await BulkInsertBatchedAsync(entityName, entitiesList, optimalBatchSize, progress, cancellationToken).ConfigureAwait(false);
                }

                InvalidateEntityCache(entityName);

                ErrorObject.Message = $"Bulk insert completed: {successfulRows}/{totalRows} rows inserted";
                ErrorObject.Flag = successfulRows == totalRows ? Errors.Ok : Errors.Failed;
            }
            catch (Exception ex)
            {
                HandleDatabaseError(ex, entityName, "BulkInsertAsync");
                ErrorObject.Message += $" ({successfulRows}/{totalRows} rows inserted before error)";
            }

            return ErrorObject;
        }

        /// <summary>
        /// Optimized multi-row INSERT (INSERT INTO table VALUES (...), (...), ...)
        /// </summary>
        private int BulkInsertMultiRow<T>(
            string entityName, 
            List<T> entities, 
            int batchSize, 
            IProgress<PassedArgs>? progress)
        {
            int successfulRows = 0;
            int totalRows = entities.Count;
            int processedRows = 0;

            for (int i = 0; i < entities.Count; i += batchSize)
            {
                var batch = entities.Skip(i).Take(batchSize).ToList();
                
                using (var cmd = GetDataCommand())
                {
                    string multiRowInsert = BuildMultiRowInsertCommand(entityName, batch, cmd);
                    cmd.CommandText = multiRowInsert;

                    // The command was created before the transaction, so attach it explicitly.
                    // A null transaction means either bulk transactions are off or the caller
                    // already owns one — in which case GetDataCommand already attached theirs and
                    // overwriting it would take this work out of their scope.
                    var transaction = BeginPublishedBulkTransaction();
                    if (transaction != null)
                        cmd.Transaction = transaction;

                    try
                    {
                        int rowsInserted = cmd.ExecuteNonQuery();
                        EndPublishedBulkTransaction(transaction, commit: true);
                        successfulRows += rowsInserted;
                    }
                    catch
                    {
                        RollbackPublishedBulkTransaction(transaction);
                        throw;
                    }
                }

                processedRows += batch.Count;
                ReportProgress(progress, entityName, processedRows, totalRows, "Bulk Insert");
            }

            return successfulRows;
        }

        /// <summary>
        /// Async multi-row INSERT
        /// </summary>
        private async Task<int> BulkInsertMultiRowAsync<T>(
            string entityName,
            List<T> entities,
            int batchSize,
            IProgress<PassedArgs>? progress,
            CancellationToken cancellationToken)
        {
            int successfulRows = 0;
            int totalRows = entities.Count;
            int processedRows = 0;

            for (int i = 0; i < entities.Count; i += batchSize)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batch = entities.Skip(i).Take(batchSize).ToList();

                var cmd = GetDataCommand() as DbCommand;
                if (cmd == null)
                    throw new InvalidOperationException("Database command does not support async operations");

                try
                {
                    string multiRowInsert = BuildMultiRowInsertCommand(entityName, batch, cmd);
                    cmd.CommandText = multiRowInsert;

                    // Same shape as the synchronous path. Synchronous Begin/Commit here rather
                    // than the *Async pair so the transaction goes through the one place that
                    // publishes it as ActiveTransaction; the work itself is still async.
                    var transaction = BeginPublishedBulkTransaction();
                    if (transaction != null)
                        cmd.Transaction = transaction as DbTransaction;

                    try
                    {
                        int rowsInserted = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                        EndPublishedBulkTransaction(transaction, commit: true);
                        successfulRows += rowsInserted;
                    }
                    catch
                    {
                        RollbackPublishedBulkTransaction(transaction);
                        throw;
                    }

                    processedRows += batch.Count;
                    ReportProgress(progress, entityName, processedRows, totalRows, "Bulk Insert Async");
                }
                finally
                {
                    await cmd.DisposeAsync().ConfigureAwait(false);
                }
            }

            return successfulRows;
        }

        /// <summary>
        /// Fallback batched single-row INSERTs
        /// </summary>
        private int BulkInsertBatched<T>(
            string entityName,
            List<T> entities,
            int batchSize,
            IProgress<PassedArgs>? progress)
        {
            int successfulRows = 0;
            int totalRows = entities.Count;
            int processedRows = 0;

            for (int i = 0; i < entities.Count; i += batchSize)
            {
                var batch = entities.Skip(i).Take(batchSize).ToList();

                // Published, so the InsertEntity calls below — which build their commands through
                // GetDataCommand — actually run inside it.
                var transaction = BeginPublishedBulkTransaction();
                try
                {
                    foreach (var entity in batch)
                    {
                        var result = InsertEntity(entityName, entity);
                        if (result.Flag == Errors.Ok)
                            successfulRows++;
                    }
                    EndPublishedBulkTransaction(transaction, commit: true);
                }
                catch
                {
                    RollbackPublishedBulkTransaction(transaction);
                    throw;
                }

                processedRows += batch.Count;
                ReportProgress(progress, entityName, processedRows, totalRows, "Bulk Insert Batched");
            }

            return successfulRows;
        }

        /// <summary>
        /// Async batched single-row INSERTs
        /// </summary>
        private async Task<int> BulkInsertBatchedAsync<T>(
            string entityName,
            List<T> entities,
            int batchSize,
            IProgress<PassedArgs>? progress,
            CancellationToken cancellationToken)
        {
            int successfulRows = 0;
            int totalRows = entities.Count;
            int processedRows = 0;

            for (int i = 0; i < entities.Count; i += batchSize)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batch = entities.Skip(i).Take(batchSize).ToList();

                // Published, so the per-row calls below — which build their commands through
                // GetDataCommand — actually run inside it. Synchronous Begin/Commit so the
                // transaction goes through the one place that publishes it; the work stays async.
                var transaction = BeginPublishedBulkTransaction();
                try
                {
                    foreach (var entity in batch)
                    {
                        var result = await InsertEntityAsync(entityName, entity).ConfigureAwait(false);
                        if (result.Flag == Errors.Ok)
                            successfulRows++;
                    }
                    EndPublishedBulkTransaction(transaction, commit: true);
                }
                catch
                {
                    RollbackPublishedBulkTransaction(transaction);
                    throw;
                }

                processedRows += batch.Count;
                ReportProgress(progress, entityName, processedRows, totalRows, "Bulk Insert Batched Async");
            }

            return successfulRows;
        }

        #endregion

        #region Bulk Update Operations

        /// <summary>
        /// Updates multiple entities in batches using temp table approach for optimal performance
        /// </summary>
        public virtual IErrorsInfo BulkUpdateEntities<T>(
            string entityName,
            IEnumerable<T> entities,
            IProgress<PassedArgs>? progress = null,
            int batchSize = 0)
        {
            if (batchSize <= 0)
                batchSize = DefaultBatchSize;

            SetObjects(entityName);
            ErrorObject.Flag = Errors.Ok;

            var entitiesList = entities.ToList();
            if (!entitiesList.Any())
            {
                ErrorObject.Message = "No entities to update";
                return ErrorObject;
            }

            int totalRows = entitiesList.Count;
            int successfulRows = 0;

            try
            {
                // Clamp to the provider's parameter budget, as the insert paths do. This was
                // missing here: BulkUpdateWithTempTable loads the temp table with a multi-row
                // INSERT over ALL fields, so the default 1000 rows x 20 columns is 20,000
                // parameters — far past SQL Server's 2,100 limit.
                int updateFieldsCount = DataStruct?.Fields?.Count ?? 1;
                int optimalBatchSize = Math.Min(batchSize, MaxParametersPerBatch / Math.Max(updateFieldsCount, 1));
                if (optimalBatchSize < 1) optimalBatchSize = 1;

                if (EnableBulkOptimizations && SupportsTempTables())
                {
                    successfulRows = BulkUpdateWithTempTable(entityName, entitiesList, optimalBatchSize, progress);
                }
                else
                {
                    successfulRows = BulkUpdateBatched(entityName, entitiesList, optimalBatchSize, progress);
                }

                InvalidateEntityCache(entityName);

                ErrorObject.Message = $"Bulk update completed: {successfulRows}/{totalRows} rows updated";
                ErrorObject.Flag = successfulRows > 0 ? Errors.Ok : Errors.Failed;
            }
            catch (Exception ex)
            {
                HandleDatabaseError(ex, entityName, "BulkUpdate");
                ErrorObject.Message += $" ({successfulRows}/{totalRows} rows updated before error)";
            }

            return ErrorObject;
        }

        /// <summary>
        /// Async bulk update
        /// </summary>
        public virtual async Task<IErrorsInfo> BulkUpdateEntitiesAsync<T>(
            string entityName,
            IEnumerable<T> entities,
            IProgress<PassedArgs>? progress = null,
            int batchSize = 0,
            CancellationToken cancellationToken = default)
        {
            if (batchSize <= 0)
                batchSize = DefaultBatchSize;

            SetObjects(entityName);
            ErrorObject.Flag = Errors.Ok;

            var entitiesList = entities.ToList();
            if (!entitiesList.Any())
            {
                ErrorObject.Message = "No entities to update";
                return ErrorObject;
            }

            int totalRows = entitiesList.Count;
            int successfulRows = 0;

            try
            {
                // See the synchronous overload: clamp to the provider's parameter budget.
                int updateFieldsCount = DataStruct?.Fields?.Count ?? 1;
                int optimalBatchSize = Math.Min(batchSize, MaxParametersPerBatch / Math.Max(updateFieldsCount, 1));
                if (optimalBatchSize < 1) optimalBatchSize = 1;

                if (EnableBulkOptimizations && SupportsTempTables())
                {
                    successfulRows = await BulkUpdateWithTempTableAsync(entityName, entitiesList, optimalBatchSize, progress, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    successfulRows = await BulkUpdateBatchedAsync(entityName, entitiesList, optimalBatchSize, progress, cancellationToken).ConfigureAwait(false);
                }

                InvalidateEntityCache(entityName);

                ErrorObject.Message = $"Bulk update completed: {successfulRows}/{totalRows} rows updated";
                ErrorObject.Flag = successfulRows > 0 ? Errors.Ok : Errors.Failed;
            }
            catch (Exception ex)
            {
                HandleDatabaseError(ex, entityName, "BulkUpdateAsync");
                ErrorObject.Message += $" ({successfulRows}/{totalRows} rows updated before error)";
            }

            return ErrorObject;
        }

        /// <summary>
        /// Optimized bulk update using temp table and MERGE/UPDATE JOIN
        /// </summary>
        private int BulkUpdateWithTempTable<T>(
            string entityName,
            List<T> entities,
            int batchSize,
            IProgress<PassedArgs>? progress)
        {
            int successfulRows = 0;
            int totalRows = entities.Count;
            int processedRows = 0;

            string tempTableName = BuildTempTableName(entityName);

            try
            {
                // Create temp table with same structure
                CreateTempTableForUpdate(tempTableName);

                // Insert data into temp table in batches
                for (int i = 0; i < entities.Count; i += batchSize)
                {
                    var batch = entities.Skip(i).Take(batchSize).ToList();
                    InsertIntoTempTable(tempTableName, batch);
                    processedRows += batch.Count;
                    ReportProgress(progress, entityName, processedRows, totalRows, "Bulk Update - Loading Temp Table");
                }

                // Execute MERGE/UPDATE statement
                string mergeQuery = BuildMergeUpdateQuery(entityName, tempTableName);
                using (var cmd = GetDataCommand())
                {
                    cmd.CommandText = mergeQuery;
                    successfulRows = cmd.ExecuteNonQuery();
                }

                ReportProgress(progress, entityName, totalRows, totalRows, "Bulk Update - Complete");
            }
            finally
            {
                // Clean up temp table
                DropTempTable(tempTableName);
            }

            return successfulRows;
        }

        /// <summary>
        /// Async bulk update with temp table
        /// </summary>
        private async Task<int> BulkUpdateWithTempTableAsync<T>(
            string entityName,
            List<T> entities,
            int batchSize,
            IProgress<PassedArgs>? progress,
            CancellationToken cancellationToken)
        {
            int successfulRows = 0;
            int totalRows = entities.Count;
            int processedRows = 0;

            string tempTableName = BuildTempTableName(entityName);

            try
            {
                await CreateTempTableForUpdateAsync(tempTableName, cancellationToken).ConfigureAwait(false);

                for (int i = 0; i < entities.Count; i += batchSize)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var batch = entities.Skip(i).Take(batchSize).ToList();
                    await InsertIntoTempTableAsync(tempTableName, batch, cancellationToken).ConfigureAwait(false);
                    processedRows += batch.Count;
                    ReportProgress(progress, entityName, processedRows, totalRows, "Bulk Update Async - Loading Temp Table");
                }

                string mergeQuery = BuildMergeUpdateQuery(entityName, tempTableName);
                var cmd = GetDataCommand() as DbCommand;
                if (cmd == null)
                    throw new InvalidOperationException("Database command does not support async operations");

                try
                {
                    cmd.CommandText = mergeQuery;
                    successfulRows = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    await cmd.DisposeAsync().ConfigureAwait(false);
                }

                ReportProgress(progress, entityName, totalRows, totalRows, "Bulk Update Async - Complete");
            }
            finally
            {
                await DropTempTableAsync(tempTableName, cancellationToken).ConfigureAwait(false);
            }

            return successfulRows;
        }

        /// <summary>
        /// Fallback batched single-row UPDATEs
        /// </summary>
        private int BulkUpdateBatched<T>(
            string entityName,
            List<T> entities,
            int batchSize,
            IProgress<PassedArgs>? progress)
        {
            int successfulRows = 0;
            int totalRows = entities.Count;
            int processedRows = 0;

            for (int i = 0; i < entities.Count; i += batchSize)
            {
                var batch = entities.Skip(i).Take(batchSize).ToList();

                var transaction = BeginPublishedBulkTransaction();
                try
                {
                    foreach (var entity in batch)
                    {
                        var result = UpdateEntity(entityName, entity);
                        if (result.Flag == Errors.Ok)
                            successfulRows++;
                    }
                    EndPublishedBulkTransaction(transaction, commit: true);
                }
                catch
                {
                    RollbackPublishedBulkTransaction(transaction);
                    throw;
                }

                processedRows += batch.Count;
                ReportProgress(progress, entityName, processedRows, totalRows, "Bulk Update Batched");
            }

            return successfulRows;
        }

        /// <summary>
        /// Async batched UPDATEs
        /// </summary>
        private async Task<int> BulkUpdateBatchedAsync<T>(
            string entityName,
            List<T> entities,
            int batchSize,
            IProgress<PassedArgs>? progress,
            CancellationToken cancellationToken)
        {
            int successfulRows = 0;
            int totalRows = entities.Count;
            int processedRows = 0;

            for (int i = 0; i < entities.Count; i += batchSize)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batch = entities.Skip(i).Take(batchSize).ToList();

                // Published, so the per-row calls below — which build their commands through
                // GetDataCommand — actually run inside it. Synchronous Begin/Commit so the
                // transaction goes through the one place that publishes it; the work stays async.
                var transaction = BeginPublishedBulkTransaction();
                try
                {
                    foreach (var entity in batch)
                    {
                        var result = await UpdateEntityAsync(entityName, entity).ConfigureAwait(false);
                        if (result.Flag == Errors.Ok)
                            successfulRows++;
                    }
                    EndPublishedBulkTransaction(transaction, commit: true);
                }
                catch
                {
                    RollbackPublishedBulkTransaction(transaction);
                    throw;
                }

                processedRows += batch.Count;
                ReportProgress(progress, entityName, processedRows, totalRows, "Bulk Update Batched Async");
            }

            return successfulRows;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Builds multi-row INSERT command (INSERT INTO table VALUES (...), (...), ...)
        /// </summary>
        /// <param name="includeAutoIncrement">
        /// True when loading a temp table for a bulk UPDATE. A real INSERT must skip identity
        /// columns because the server assigns them, but the temp table is joined on the key, so it
        /// needs the key's VALUE. The CREATE uses every field, and this used to filter identity
        /// columns out unconditionally — so when the key was auto-increment it was never populated
        /// and the MERGE's ON clause matched nothing, updating zero rows.
        /// </param>
        private string BuildMultiRowInsertCommand<T>(string entityName, List<T> batch, IDbCommand cmd,
                                                     bool includeAutoIncrement = false)
        {
            var fields = includeAutoIncrement
                ? DataStruct.Fields.ToList()
                : DataStruct.Fields.Where(f => !f.IsAutoIncrement).ToList();
            var sb = new StringBuilder();

            // INSERT INTO table (col1, col2, ...)
            sb.Append($"INSERT INTO {QualifyWithSchema(entityName)} (");
            sb.Append(string.Join(", ", fields.Select(f => GetFieldName(f.FieldName))));
            sb.Append(") VALUES ");

            // Build VALUES clauses
            var valuesClauses = new List<string>();
            int paramIndex = 0;

            foreach (var entity in batch)
            {
                var paramNames = new List<string>();

                // Reflect on the RUNTIME type of the row, not on T.
                //
                // T is `object` whenever the caller came through the ETL layer or any
                // IEnumerable<object> path, and typeof(object) has none of the entity's properties —
                // so every lookup returned null, every parameter became DBNull.Value, and the bulk
                // insert wrote a table full of NULLs and reported success. The single-row path had
                // this right all along (CreateCommandParameters uses InsertedData.GetType()).
                var rowType = entity?.GetType() ?? typeof(T);

                foreach (var field in fields)
                {
                    string paramName = $"{ParameterDelimiter}p{paramIndex++}";
                    paramNames.Add(paramName);

                    var property = FindPropertyCaseInsensitive(rowType, field.FieldName);
                    var value = property?.GetValue(entity);

                    var param = cmd.CreateParameter();
                    param.ParameterName = paramName;

                    // Type and convert the value the same way the single-row path does. Leaving
                    // DbType unset made the provider infer it from the CLR value, which silently
                    // mishandles byte[], Guid, decimal scale and DateTime bounds.
                    param.DbType = GetDbType(field.Fieldtype);
                    param.Value = value == null || value == DBNull.Value
                        ? DBNull.Value
                        : ConvertToDbTypeValue(value, field.Fieldtype);

                    cmd.Parameters.Add(param);
                }

                valuesClauses.Add($"({string.Join(", ", paramNames)})");
            }

            sb.Append(string.Join(", ", valuesClauses));
            return sb.ToString();
        }

        /// <summary>
        /// Creates temp table for bulk update
        /// </summary>
        private void CreateTempTableForUpdate(string tempTableName)
        {
            string createTableSql = DatasourceType switch
            {
                DataSourceType.SqlServer => BuildSqlServerTempTableCreate(tempTableName),
                DataSourceType.Mysql => BuildMySqlTempTableCreate(tempTableName),
                DataSourceType.Postgre => BuildPostgreSqlTempTableCreate(tempTableName),
                // CockroachDB is PostgreSQL wire- and DDL-compatible for this exact builder:
                // it accepts CREATE TEMP TABLE, and ResolveDdlColumnType already dispatches
                // on DatasourceType, so the column types it emits are Cockroach's own (K46's
                // GetFallbackDbType/NormalizeDbTypeForProvider extension), not Postgres's.
                DataSourceType.Cockroach => BuildPostgreSqlTempTableCreate(tempTableName),
                _ => throw new NotSupportedException($"Temp tables not supported for {DatasourceType}")
            };

            using (var cmd = GetDataCommand())
            {
                cmd.CommandText = createTableSql;
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Async temp table creation
        /// </summary>
        private async Task CreateTempTableForUpdateAsync(string tempTableName, CancellationToken cancellationToken)
        {
            string createTableSql = DatasourceType switch
            {
                DataSourceType.SqlServer => BuildSqlServerTempTableCreate(tempTableName),
                DataSourceType.Mysql => BuildMySqlTempTableCreate(tempTableName),
                DataSourceType.Postgre => BuildPostgreSqlTempTableCreate(tempTableName),
                // CockroachDB is PostgreSQL wire- and DDL-compatible for this exact builder:
                // it accepts CREATE TEMP TABLE, and ResolveDdlColumnType already dispatches
                // on DatasourceType, so the column types it emits are Cockroach's own (K46's
                // GetFallbackDbType/NormalizeDbTypeForProvider extension), not Postgres's.
                DataSourceType.Cockroach => BuildPostgreSqlTempTableCreate(tempTableName),
                _ => throw new NotSupportedException($"Temp tables not supported for {DatasourceType}")
            };

            var cmd = GetDataCommand() as DbCommand;
            if (cmd == null)
                throw new InvalidOperationException("Database command does not support async operations");

            try
            {
                cmd.CommandText = createTableSql;
                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                await cmd.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Inserts batch into temp table
        /// </summary>
        private void InsertIntoTempTable<T>(string tempTableName, List<T> batch)
        {
            using (var cmd = GetDataCommand())
            {
                string multiRowInsert = BuildMultiRowInsertCommand(tempTableName, batch, cmd, includeAutoIncrement: true);
                cmd.CommandText = multiRowInsert;
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Async insert into temp table
        /// </summary>
        private async Task InsertIntoTempTableAsync<T>(string tempTableName, List<T> batch, CancellationToken cancellationToken)
        {
            var cmd = GetDataCommand() as DbCommand;
            if (cmd == null)
                throw new InvalidOperationException("Database command does not support async operations");

            try
            {
                string multiRowInsert = BuildMultiRowInsertCommand(tempTableName, batch, cmd, includeAutoIncrement: true);
                cmd.CommandText = multiRowInsert;
                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                await cmd.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Builds MERGE/UPDATE query from temp table
        /// </summary>
        private string BuildMergeUpdateQuery(string targetTable, string tempTable)
        {
            var primaryKeys = DataStruct.PrimaryKeys.ToList();
            var updateFields = DataStruct.Fields.Where(f => !f.IsKey && !f.IsAutoIncrement).ToList();

            return DatasourceType switch
            {
                DataSourceType.SqlServer => BuildSqlServerMergeQuery(targetTable, tempTable, primaryKeys, updateFields),
                DataSourceType.Mysql => BuildMySqlUpdateJoinQuery(targetTable, tempTable, primaryKeys, updateFields),
                DataSourceType.Postgre => BuildPostgreSqlUpdateFromQuery(targetTable, tempTable, primaryKeys, updateFields),
                // CockroachDB accepts the same UPDATE ... SET ... FROM ... AS source WHERE ...
                // join-update syntax as PostgreSQL.
                DataSourceType.Cockroach => BuildPostgreSqlUpdateFromQuery(targetTable, tempTable, primaryKeys, updateFields),
                _ => throw new NotSupportedException($"Bulk update not supported for {DatasourceType}")
            };
        }

        /// <summary>
        /// SQL Server MERGE syntax
        /// </summary>
        private string BuildSqlServerMergeQuery(string targetTable, string tempTable, List<EntityField> primaryKeys, List<EntityField> updateFields)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"MERGE INTO {QualifyWithSchema(targetTable)} AS target");
            sb.AppendLine($"USING {tempTable} AS source");
            
            // ON clause (primary key match)
            var onConditions = primaryKeys.Select(pk => 
                $"target.{GetFieldName(pk.FieldName)} = source.{GetFieldName(pk.FieldName)}");
            sb.AppendLine($"ON ({string.Join(" AND ", onConditions)})");
            
            // WHEN MATCHED
            var setStatements = updateFields.Select(f => 
                $"{GetFieldName(f.FieldName)} = source.{GetFieldName(f.FieldName)}");
            sb.AppendLine($"WHEN MATCHED THEN UPDATE SET {string.Join(", ", setStatements)};");
            
            return sb.ToString();
        }

        /// <summary>
        /// MySQL UPDATE JOIN syntax
        /// </summary>
        private string BuildMySqlUpdateJoinQuery(string targetTable, string tempTable, List<EntityField> primaryKeys, List<EntityField> updateFields)
        {
            var sb = new StringBuilder();
            sb.Append($"UPDATE {QualifyWithSchema(targetTable)} AS target ");
            sb.Append($"INNER JOIN {tempTable} AS source ");
            
            var onConditions = primaryKeys.Select(pk => 
                $"target.{GetFieldName(pk.FieldName)} = source.{GetFieldName(pk.FieldName)}");
            sb.Append($"ON {string.Join(" AND ", onConditions)} ");
            
            var setStatements = updateFields.Select(f => 
                $"target.{GetFieldName(f.FieldName)} = source.{GetFieldName(f.FieldName)}");
            sb.Append($"SET {string.Join(", ", setStatements)}");
            
            return sb.ToString();
        }

        /// <summary>
        /// PostgreSQL UPDATE FROM syntax
        /// </summary>
        private string BuildPostgreSqlUpdateFromQuery(string targetTable, string tempTable, List<EntityField> primaryKeys, List<EntityField> updateFields)
        {
            var sb = new StringBuilder();
            sb.Append($"UPDATE {QualifyWithSchema(targetTable)} AS target SET ");
            
            var setStatements = updateFields.Select(f => 
                $"{GetFieldName(f.FieldName)} = source.{GetFieldName(f.FieldName)}");
            sb.Append(string.Join(", ", setStatements));
            
            sb.Append($" FROM {tempTable} AS source WHERE ");
            
            var whereConditions = primaryKeys.Select(pk => 
                $"target.{GetFieldName(pk.FieldName)} = source.{GetFieldName(pk.FieldName)}");
            sb.Append(string.Join(" AND ", whereConditions));
            
            return sb.ToString();
        }

        /// <summary>
        /// SQL Server temp table CREATE
        /// </summary>
        private string BuildSqlServerTempTableCreate(string tempTableName)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"CREATE TABLE {tempTableName} (");
            
            var columns = DataStruct.Fields.Select(f => 
                $"{GetFieldName(f.FieldName)} {ResolveDdlColumnType(f)}");
            sb.AppendLine(string.Join(",\n", columns));
            sb.AppendLine(")");
            
            return sb.ToString();
        }

        /// <summary>
        /// MySQL temp table CREATE
        /// </summary>
        private string BuildMySqlTempTableCreate(string tempTableName)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"CREATE TEMPORARY TABLE {tempTableName} (");
            
            var columns = DataStruct.Fields.Select(f => 
                $"{GetFieldName(f.FieldName)} {ResolveDdlColumnType(f)}");
            sb.AppendLine(string.Join(",\n", columns));
            sb.AppendLine(")");
            
            return sb.ToString();
        }

        /// <summary>
        /// PostgreSQL temp table CREATE
        /// </summary>
        private string BuildPostgreSqlTempTableCreate(string tempTableName)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"CREATE TEMP TABLE {tempTableName} (");
            
            var columns = DataStruct.Fields.Select(f => 
                $"{GetFieldName(f.FieldName)} {ResolveDdlColumnType(f)}");
            sb.AppendLine(string.Join(",\n", columns));
            sb.AppendLine(")");
            
            return sb.ToString();
        }

        /// <summary>
        /// Drop temp table
        /// </summary>
        private void DropTempTable(string tempTableName)
        {
            try
            {
                using (var cmd = GetDataCommand())
                {
                    if (cmd == null)
                    {
                        Logger?.WriteLog($"Temp table {tempTableName} was not dropped: no command could be " +
                                         $"created on {DatasourceName}. It will persist for the life of the connection.");
                        return;
                    }

                    cmd.CommandText = $"DROP TABLE IF EXISTS {tempTableName}";
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                // Cleanup failure must not replace the caller's outcome, but it must not be silent
                // either: the catch here used to be completely empty, so a temp table that could not
                // be dropped survived for the life of the connection with nothing recorded.
                Logger?.WriteLog($"Could not drop temp table {tempTableName} on {DatasourceName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Async drop temp table
        /// </summary>
        private async Task DropTempTableAsync(string tempTableName, CancellationToken cancellationToken)
        {
            try
            {
                var cmd = GetDataCommand() as DbCommand;
                if (cmd != null)
                {
                    try
                    {
                        cmd.CommandText = $"DROP TABLE IF EXISTS {tempTableName}";

                        // CancellationToken.None deliberately. This runs from a finally, and the
                        // caller's token is already cancelled on the path that matters — passing it
                        // made the drop throw immediately, so the temp table leaked on EVERY
                        // cancellation.
                        await cmd.ExecuteNonQueryAsync(CancellationToken.None).ConfigureAwait(false);
                    }
                    finally
                    {
                        await cmd.DisposeAsync().ConfigureAwait(false);
                    }
                }
                else
                {
                    Logger?.WriteLog($"Temp table {tempTableName} was not dropped: no async command could be " +
                                     $"created on {DatasourceName}. It will persist for the life of the connection.");
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Could not drop temp table {tempTableName} on {DatasourceName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Whether this engine accepts several rows in one INSERT ... VALUES clause.
        /// </summary>
        /// <remarks>
        /// Delegates to <see cref="RDBMSHelper.SupportsFeature"/>. This used to be a four-arm switch
        /// naming SQL Server, MySQL, PostgreSQL and SQLite, so MariaDB, AzureSQL, CockroachDB,
        /// DuckDB, DB2, Snowflake, Spanner, Presto/Trino and the rest all reported false and fell
        /// back to a statement per row -- correct, but far slower than the grammar they support.
        ///
        /// This is a property of the engine's grammar, so it belongs in the shared dialect table
        /// where every consumer sees the same answer, not in one base class.
        /// <see cref="BuildMultiRowInsertCommand"/> emits plain ANSI multi-row VALUES, which is
        /// exactly what that feature flag describes.
        /// </remarks>
        private bool SupportsMultiRowInsert()
            => RDBMSHelper.SupportsFeature(DatasourceType, DatabaseFeature.MultiRowInsert);

        /// <summary>
        /// Whether <b>this class</b> can build temp-table bulk-update SQL for the current engine.
        /// </summary>
        /// <remarks>
        /// Deliberately <b>not</b> delegated to the shared dialect table. Far more engines support
        /// temporary tables than are listed here; what is actually being asked is whether
        /// <see cref="CreateTempTableForUpdate"/> and <see cref="BuildMergeUpdateQuery"/> have a
        /// builder for this engine, and they have exactly three. Answering from an engine-capability
        /// table would send Oracle and the others down a path that ends in the
        /// <c>NotSupportedException</c> those two methods throw.
        ///
        /// Keep this list and those two switches in step: widening one without the other is the bug.
        /// </remarks>
        /// <summary>
        /// A temp-table name in the syntax this engine actually recognises.
        /// </summary>
        /// <remarks>
        /// This was hardcoded to a SQL-Server-only local-temp-table name -- "#TempUpdate_..." --
        /// regardless of engine. SQL Server is the ONE engine where a leading "#" means anything;
        /// on PostgreSQL and CockroachDB it is a syntax error, and on MySQL "#" starts a
        /// to-end-of-line comment, so "CREATE TEMPORARY TABLE #TempUpdate_..." silently became
        /// "CREATE TEMPORARY TABLE " with the rest of the line commented out -- a syntax error
        /// either way. Since nothing exercised this path against a real non-SqlServer engine before
        /// (SQLite masks it: this method is never reached for DataSourceType.SqlLite because
        /// SupportsTempTables() has never listed it), the bug was invisible until this class was
        /// actually driven with SupportsTempTables() reporting true for something other than
        /// SqlServer -- which is exactly what extending it to CockroachDB (above) did, and how this
        /// was found.
        /// </remarks>
        private string BuildTempTableName(string entityName)
        {
            string bare = $"TempUpdate_{entityName}_{Guid.NewGuid():N}";
            return DatasourceType switch
            {
                DataSourceType.SqlServer or DataSourceType.AzureSQL or DataSourceType.SqlCompact => "#" + bare,
                _ => bare
            };
        }

        private bool SupportsTempTables()
        {
            return DatasourceType switch
            {
                DataSourceType.SqlServer => true,
                DataSourceType.Mysql => true,
                DataSourceType.Postgre => true,
                // CockroachDB is PostgreSQL wire-compatible for the exact statements this path
                // emits (CREATE TEMP TABLE, UPDATE ... FROM ... AS source), so it reuses
                // BuildPostgreSqlTempTableCreate/BuildPostgreSqlUpdateFromQuery directly rather
                // than needing its own builder pair. Not extended further: Oracle, SQLite,
                // Hana, Firebird, Presto, Snowflake and Spanner each need dialect-specific
                // temp-table/MERGE syntax this class does not have a builder for yet.
                DataSourceType.Cockroach => true,
                _ => false
            };
        }

        /// <summary>
        /// Report progress to caller
        /// </summary>
        private void ReportProgress(IProgress<PassedArgs>? progress, string entityName, int current, int total, string operation)
        {
            if (progress != null)
            {
                var args = new PassedArgs
                {
                    ParameterInt1 = current,
                    ParameterInt2 = total,
                    ParameterString1 = entityName,
                    ParameterString2 = operation,
                    EventType = $"Progress: {current}/{total} - {operation}"
                };
                progress.Report(args);
            }
        }

        #endregion
    }
}
