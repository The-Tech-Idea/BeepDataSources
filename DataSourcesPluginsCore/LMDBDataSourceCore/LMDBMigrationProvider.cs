using System.Collections.Generic;
using TheTechIdea.Beep;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.SchemaMigration;
using TheTechIdea.Beep.Utilities;
using LightningDB;

namespace LMDBDataSourceCore
{
    /// <summary>
    /// Colocated Tier-1 <see cref="ISchemaMigrationProvider"/> for <see cref="DataSourceType.LMDB"/>.
    /// LMDB named databases = entity. Capabilities: create named db, drop named db, truncate
    /// named db, rename named db (drop + create + iterate-copy). LMDB supports transactions
    /// for DDL on the DB level via its own MVCC semantics (single-writer per env).
    /// </summary>
    [SchemaMigrationProvider(DataSourceType.LMDB, DatasourceCategory.KVStore)]
    public class LMDBMigrationProvider : ISchemaMigrationProvider
    {
        private readonly LMDBDataSource _owner;

        public LMDBMigrationProvider(IDataSource owner)
        {
            _owner = owner as LMDBDataSource
                     ?? throw new System.ArgumentNullException(nameof(owner), "Expected a LMDBDataSource instance.");
        }

        public DataSourceType DataSourceType => DataSourceType.LMDB;
        public DatasourceCategory Category => DatasourceCategory.KVStore;

        public SchemaMigrationCapabilities Capabilities { get; } = new SchemaMigrationCapabilities
        {
            SupportsCreateEntity = true,           // delegates to datasource (real: tx.OpenDatabase with Create flag)
            SupportsDropEntity = true,             // delegates to datasource (real tx.DropDatabase)
            SupportsAddColumn = true,              // schemaless; no-op Ok
            SupportsTruncateEntity = true,         // real: tx.TruncateDatabase
            SupportsRenameEntity = true,           // real: tx.DropDatabase + OpenDatabase + iterate-copy
            SupportsCreateIndex = false,           // LMDB has no built-in secondary index
            SupportsDropIndex = false,
            SupportsTransactionalDdl = true        // DDL happens within write transactions
        };

        public IErrorsInfo CreateEntity(EntityStructure entity)
        {
            var ok = _owner.CreateEntityAs(entity);
            return ok
                ? SchemaMigrationResults.Ok($"Created LMDB named database '{entity?.EntityName}'.")
                : SchemaMigrationResults.Fail(_owner.ErrorObject?.Message ?? $"Failed to create LMDB named database '{entity?.EntityName}'.");
        }

        public IErrorsInfo DropEntity(string entityName)
        {
            var r = _owner.DeleteEntity(entityName, null);
            return r != null && r.Flag == Errors.Ok
                ? SchemaMigrationResults.Ok($"Dropped LMDB named database '{entityName}'.")
                : SchemaMigrationResults.Fail(r?.Message ?? $"Failed to drop LMDB named database '{entityName}'.");
        }

        public IErrorsInfo AddColumn(string entityName, EntityField column)
            => SchemaMigrationResults.Ok($"LMDB is schemaless; field '{column?.FieldName}' is accepted as a document field.");

        public IErrorsInfo TruncateEntity(string entityName)
        {
            try
            {
                _owner.EnsureMigrationConnected();
                var env = _owner.MigrationEnv;
                if (env == null) return SchemaMigrationResults.Fail("LMDB is not open.");
                if (!_owner.CheckEntityExist(entityName))
                    return SchemaMigrationResults.Fail($"LMDB named database '{entityName}' not found.");

                using var tx = env.BeginTransaction(TransactionBeginFlags.None);
                using (var db = tx.OpenDatabase(entityName, new DatabaseConfiguration { Flags = DatabaseOpenFlags.None }, closeOnDispose: true))
                {
                    var rc = tx.TruncateDatabase(db);
                    if (rc != MDBResultCode.Success)
                    {
                        tx.Abort();
                        return SchemaMigrationResults.Fail($"LMDB TruncateDatabase returned {rc}.");
                    }
                }
                tx.Commit();
                return SchemaMigrationResults.Ok($"Truncated LMDB named database '{entityName}'.");
            }
            catch (System.Exception ex)
            {
                return SchemaMigrationResults.Fail($"TruncateEntity('{entityName}') failed: {ex.Message}");
            }
        }

        public IErrorsInfo RenameEntity(string oldName, string newName)
        {
            try
            {
                _owner.EnsureMigrationConnected();
                var env = _owner.MigrationEnv;
                if (env == null) return SchemaMigrationResults.Fail("LMDB is not open.");
                if (!_owner.CheckEntityExist(oldName))
                    return SchemaMigrationResults.Fail($"LMDB named database '{oldName}' not found.");
                if (_owner.CheckEntityExist(newName))
                    return SchemaMigrationResults.Fail($"LMDB named database '{newName}' already exists.");

                int copied = 0;
                using (var srcTx = env.BeginTransaction(TransactionBeginFlags.ReadOnly))
                using (var srcDb = srcTx.OpenDatabase(oldName, new DatabaseConfiguration { Flags = DatabaseOpenFlags.None }, closeOnDispose: true))
                using (var srcCursor = srcTx.CreateCursor(srcDb))
                {
                    var (rc, keyBytes, valueBytes) = srcCursor.First();
                    while (rc == MDBResultCode.Success)
                    {
                        var keyArr0 = keyBytes.CopyToNewArray();
                        if (keyArr0.Length == 0) { (rc, keyBytes, valueBytes) = srcCursor.Next(); continue; }
                        using var dstTx = env.BeginTransaction(TransactionBeginFlags.None);
                        using (var dstDb = dstTx.OpenDatabase(newName, new DatabaseConfiguration { Flags = DatabaseOpenFlags.Create }, closeOnDispose: true))
                        {
                            var keyArr = keyBytes.CopyToNewArray();
                            var valArr = valueBytes.CopyToNewArray();
                            var putRc = dstTx.Put(dstDb, keyArr, valArr, PutOptions.None);
                            if (putRc == MDBResultCode.Success) { dstTx.Commit(); copied++; }
                            else { dstTx.Abort(); }
                        }
                        (rc, keyBytes, valueBytes) = srcCursor.Next();
                    }
                }
                // Drop the source database.
                using (var dropTx = env.BeginTransaction(TransactionBeginFlags.None))
                using (var dropDb = dropTx.OpenDatabase(oldName, new DatabaseConfiguration { Flags = DatabaseOpenFlags.None }, closeOnDispose: true))
                {
                    dropTx.DropDatabase(dropDb);
                    dropTx.Commit();
                }
                _owner.EntitiesNames.Remove(oldName);
                if (!_owner.EntitiesNames.Contains(newName, System.StringComparer.OrdinalIgnoreCase))
                    _owner.EntitiesNames.Add(newName);
                return SchemaMigrationResults.Ok($"Renamed LMDB named database '{oldName}' → '{newName}' ({copied} entries copied).");
            }
            catch (System.Exception ex)
            {
                return SchemaMigrationResults.Fail($"RenameEntity('{oldName}' → '{newName}') failed: {ex.Message}");
            }
        }

        public IErrorsInfo CreateIndex(string entityName, string indexName, string[] columns, Dictionary<string, object> options = null)
            => SchemaMigrationResults.Unsupported(nameof(CreateIndex), DataSourceType);

        public IErrorsInfo DropIndex(string entityName, string indexName)
            => SchemaMigrationResults.Unsupported(nameof(DropIndex), DataSourceType);

        public IErrorsInfo AlterColumn(string entityName, string columnName, EntityField newColumn)
            => SchemaMigrationResults.Unsupported(nameof(AlterColumn), DataSourceType);
        public IErrorsInfo DropColumn(string entityName, string columnName)
            => SchemaMigrationResults.Unsupported(nameof(DropColumn), DataSourceType);
        public IErrorsInfo RenameColumn(string entityName, string oldColumnName, string newColumnName)
            => SchemaMigrationResults.Unsupported(nameof(RenameColumn), DataSourceType);
        public IErrorsInfo AddForeignKey(string entityName, string[] columnNames, string referencedEntityName, string[] referencedColumnNames, string onDeleteBehavior, string onUpdateBehavior, string constraintName)
            => SchemaMigrationResults.Unsupported(nameof(AddForeignKey), DataSourceType);
        public IErrorsInfo DropForeignKey(string entityName, string constraintName)
            => SchemaMigrationResults.Unsupported(nameof(DropForeignKey), DataSourceType);
    }
}