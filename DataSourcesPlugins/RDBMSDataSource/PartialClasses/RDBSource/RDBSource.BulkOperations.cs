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

            var entitiesList = entities.ToList();
            if (!entitiesList.Any())
            {
                ErrorObject.Message = "No entities to insert";
                return ErrorObject;
            }

            int totalRows = entitiesList.Count;
            int processedRows = 0;
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
            int processedRows = 0;
            int successfulRows = 0;

            try
            {
                int fieldsCount = DataStruct.Fields.Count(f => !f.IsAutoIncrement);
                int optimalBatchSize = Math.Min(batchSize, MaxParametersPerBatch / Math.Max(fieldsCount, 1));

                if (EnableBulkOptimizations && SupportsMultiRowInsert())
                {
                    successfulRows = await BulkInsertMultiRowAsync(entityName, entitiesList, optimalBatchSize, progress, cancellationToken);
                }
                else
                {
                    successfulRows = await BulkInsertBatchedAsync(entityName, entitiesList, optimalBatchSize, progress, cancellationToken);
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

                    if (UseBulkTransactions && Dataconnection.ConnectionProp.Database != null)
                    {
                        using (var transaction = RDBMSConnection.DbConn.BeginTransaction())
                        {
                            cmd.Transaction = transaction;
                            try
                            {
                                int rowsInserted = cmd.ExecuteNonQuery();
                                transaction.Commit();
                                successfulRows += rowsInserted;
                            }
                            catch
                            {
                                transaction.Rollback();
                                throw;
                            }
                        }
                    }
                    else
                    {
                        successfulRows += cmd.ExecuteNonQuery();
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

                    if (UseBulkTransactions && Dataconnection.ConnectionProp.Database != null)
                    {
                        var transaction = await (RDBMSConnection.DbConn as DbConnection)!.BeginTransactionAsync(cancellationToken);
                        cmd.Transaction = transaction;
                        try
                        {
                            int rowsInserted = await cmd.ExecuteNonQueryAsync(cancellationToken);
                            await transaction.CommitAsync(cancellationToken);
                            successfulRows += rowsInserted;
                        }
                        catch
                        {
                            await transaction.RollbackAsync(cancellationToken);
                            throw;
                        }
                        finally
                        {
                            await transaction.DisposeAsync();
                        }
                    }
                    else
                    {
                        successfulRows += await cmd.ExecuteNonQueryAsync(cancellationToken);
                    }

                    processedRows += batch.Count;
                    ReportProgress(progress, entityName, processedRows, totalRows, "Bulk Insert Async");
                }
                finally
                {
                    await cmd.DisposeAsync();
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

                if (UseBulkTransactions && Dataconnection.ConnectionProp.Database != null)
                {
                    using (var transaction = RDBMSConnection.DbConn.BeginTransaction())
                    {
                        try
                        {
                            foreach (var entity in batch)
                            {
                                var result = InsertEntity(entityName, entity);
                                if (result.Flag == Errors.Ok)
                                    successfulRows++;
                            }
                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
                else
                {
                    foreach (var entity in batch)
                    {
                        var result = InsertEntity(entityName, entity);
                        if (result.Flag == Errors.Ok)
                            successfulRows++;
                    }
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

                if (UseBulkTransactions && Dataconnection.ConnectionProp.Database != null)
                {
                    var transaction = await (RDBMSConnection.DbConn as DbConnection)!.BeginTransactionAsync(cancellationToken);
                    try
                    {
                        foreach (var entity in batch)
                        {
                            var result = await InsertEntityAsync(entityName, entity);
                            if (result.Flag == Errors.Ok)
                                successfulRows++;
                        }
                        await transaction.CommitAsync(cancellationToken);
                    }
                    catch
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        throw;
                    }
                    finally
                    {
                        await transaction.DisposeAsync();
                    }
                }
                else
                {
                    foreach (var entity in batch)
                    {
                        var result = await InsertEntityAsync(entityName, entity);
                        if (result.Flag == Errors.Ok)
                            successfulRows++;
                    }
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
                if (EnableBulkOptimizations && SupportsTempTables())
                {
                    successfulRows = BulkUpdateWithTempTable(entityName, entitiesList, batchSize, progress);
                }
                else
                {
                    successfulRows = BulkUpdateBatched(entityName, entitiesList, batchSize, progress);
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
                if (EnableBulkOptimizations && SupportsTempTables())
                {
                    successfulRows = await BulkUpdateWithTempTableAsync(entityName, entitiesList, batchSize, progress, cancellationToken);
                }
                else
                {
                    successfulRows = await BulkUpdateBatchedAsync(entityName, entitiesList, batchSize, progress, cancellationToken);
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

            string tempTableName = $"#TempUpdate_{entityName}_{Guid.NewGuid():N}";

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

            string tempTableName = $"#TempUpdate_{entityName}_{Guid.NewGuid():N}";

            try
            {
                await CreateTempTableForUpdateAsync(tempTableName, cancellationToken);

                for (int i = 0; i < entities.Count; i += batchSize)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var batch = entities.Skip(i).Take(batchSize).ToList();
                    await InsertIntoTempTableAsync(tempTableName, batch, cancellationToken);
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
                    successfulRows = await cmd.ExecuteNonQueryAsync(cancellationToken);
                }
                finally
                {
                    await cmd.DisposeAsync();
                }

                ReportProgress(progress, entityName, totalRows, totalRows, "Bulk Update Async - Complete");
            }
            finally
            {
                await DropTempTableAsync(tempTableName, cancellationToken);
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

                if (UseBulkTransactions && Dataconnection.ConnectionProp.Database != null)
                {
                    using (var transaction = RDBMSConnection.DbConn.BeginTransaction())
                    {
                        try
                        {
                            foreach (var entity in batch)
                            {
                                var result = UpdateEntity(entityName, entity);
                                if (result.Flag == Errors.Ok)
                                    successfulRows++;
                            }
                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
                else
                {
                    foreach (var entity in batch)
                    {
                        var result = UpdateEntity(entityName, entity);
                        if (result.Flag == Errors.Ok)
                            successfulRows++;
                    }
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

                if (UseBulkTransactions && Dataconnection.ConnectionProp.Database != null)
                {
                    var transaction = await (RDBMSConnection.DbConn as DbConnection)!.BeginTransactionAsync(cancellationToken);
                    try
                    {
                        foreach (var entity in batch)
                        {
                            var result = await UpdateEntityAsync(entityName, entity);
                            if (result.Flag == Errors.Ok)
                                successfulRows++;
                        }
                        await transaction.CommitAsync(cancellationToken);
                    }
                    catch
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        throw;
                    }
                    finally
                    {
                        await transaction.DisposeAsync();
                    }
                }
                else
                {
                    foreach (var entity in batch)
                    {
                        var result = await UpdateEntityAsync(entityName, entity);
                        if (result.Flag == Errors.Ok)
                            successfulRows++;
                    }
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
        private string BuildMultiRowInsertCommand<T>(string entityName, List<T> batch, IDbCommand cmd)
        {
            var fields = DataStruct.Fields.Where(f => !f.IsAutoIncrement).ToList();
            var sb = new StringBuilder();

            // INSERT INTO table (col1, col2, ...)
            sb.Append($"INSERT INTO {Dataconnection.ConnectionProp.SchemaName}{entityName} (");
            sb.Append(string.Join(", ", fields.Select(f => GetFieldName(f.FieldName))));
            sb.Append(") VALUES ");

            // Build VALUES clauses
            var valuesClauses = new List<string>();
            int paramIndex = 0;

            foreach (var entity in batch)
            {
                var paramNames = new List<string>();
                
                foreach (var field in fields)
                {
                    string paramName = $"{ParameterDelimiter}p{paramIndex++}";
                    paramNames.Add(paramName);

                    var property = typeof(T).GetProperty(field.FieldName)
                        ?? typeof(T).GetProperty(field.FieldName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    var value = property?.GetValue(entity);
                    
                    var param = cmd.CreateParameter();
                    param.ParameterName = paramName;
                    param.Value = value ?? DBNull.Value;
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
                _ => throw new NotSupportedException($"Temp tables not supported for {DatasourceType}")
            };

            var cmd = GetDataCommand() as DbCommand;
            if (cmd == null)
                throw new InvalidOperationException("Database command does not support async operations");

            try
            {
                cmd.CommandText = createTableSql;
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            finally
            {
                await cmd.DisposeAsync();
            }
        }

        /// <summary>
        /// Inserts batch into temp table
        /// </summary>
        private void InsertIntoTempTable<T>(string tempTableName, List<T> batch)
        {
            using (var cmd = GetDataCommand())
            {
                string multiRowInsert = BuildMultiRowInsertCommand(tempTableName, batch, cmd);
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
                string multiRowInsert = BuildMultiRowInsertCommand(tempTableName, batch, cmd);
                cmd.CommandText = multiRowInsert;
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            finally
            {
                await cmd.DisposeAsync();
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
                _ => throw new NotSupportedException($"Bulk update not supported for {DatasourceType}")
            };
        }

        /// <summary>
        /// SQL Server MERGE syntax
        /// </summary>
        private string BuildSqlServerMergeQuery(string targetTable, string tempTable, List<EntityField> primaryKeys, List<EntityField> updateFields)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"MERGE INTO {Dataconnection.ConnectionProp.SchemaName}{targetTable} AS target");
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
            sb.Append($"UPDATE {Dataconnection.ConnectionProp.SchemaName}{targetTable} AS target ");
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
            sb.Append($"UPDATE {Dataconnection.ConnectionProp.SchemaName}{targetTable} AS target SET ");
            
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
                $"{GetFieldName(f.FieldName)} {f.Fieldtype}");
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
                $"{GetFieldName(f.FieldName)} {f.Fieldtype}");
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
                $"{GetFieldName(f.FieldName)} {f.Fieldtype}");
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
                    cmd.CommandText = $"DROP TABLE {tempTableName}";
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                // Ignore errors on cleanup
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
                        cmd.CommandText = $"DROP TABLE {tempTableName}";
                        await cmd.ExecuteNonQueryAsync(cancellationToken);
                    }
                    finally
                    {
                        await cmd.DisposeAsync();
                    }
                }
            }
            catch
            {
                // Ignore errors on cleanup
            }
        }

        /// <summary>
        /// Check if database supports multi-row INSERT
        /// </summary>
        private bool SupportsMultiRowInsert()
        {
            return DatasourceType switch
            {
                DataSourceType.SqlServer => true,
                DataSourceType.Mysql => true,
                DataSourceType.Postgre => true,
                DataSourceType.SqlLite => true,
                _ => false
            };
        }

        /// <summary>
        /// Check if database supports temp tables for bulk operations
        /// </summary>
        private bool SupportsTempTables()
        {
            return DatasourceType switch
            {
                DataSourceType.SqlServer => true,
                DataSourceType.Mysql => true,
                DataSourceType.Postgre => true,
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
