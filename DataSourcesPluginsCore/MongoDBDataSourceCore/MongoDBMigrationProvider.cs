using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.SchemaMigration;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.NOSQL
{
    /// <summary>
    /// Colocated Tier-1 <see cref="ISchemaMigrationProvider"/> for <see cref="DataSourceType.MongoDB"/>.
    /// Translates logical schema operations into native MongoDB driver calls (create/drop
    /// collection, create/drop index, $unset/$rename bulk updates, admin renameCollection).
    /// MongoDB is schemaless, so column add is a no-op (the collection accepts the field
    /// implicitly); alter-column and foreign keys are unsupported (no enforced schema/FKs).
    /// Constructed with the owning <see cref="MongoDBDataSource"/> so it always reflects the
    /// live connection.
    /// </summary>
    [SchemaMigrationProvider(DataSourceType.MongoDB, DatasourceCategory.NOSQL)]
    public class MongoDBMigrationProvider : ISchemaMigrationProvider
    {
        private readonly MongoDBDataSource _owner;

        public MongoDBMigrationProvider(IDataSource owner)
        {
            _owner = owner as MongoDBDataSource
                     ?? throw new ArgumentNullException(nameof(owner), "Expected a MongoDBDataSource instance.");
        }

        public DataSourceType DataSourceType => DataSourceType.MongoDB;
        public DatasourceCategory Category => DatasourceCategory.NOSQL;

        public SchemaMigrationCapabilities Capabilities { get; } = new SchemaMigrationCapabilities
        {
            SupportsCreateEntity = true,   // createCollection
            SupportsAddColumn = true,      // schemaless → no-op Ok
            SupportsDropColumn = true,     // $unset bulk update
            SupportsRenameColumn = true,   // $rename bulk update
            SupportsRenameEntity = true,   // admin renameCollection
            SupportsDropEntity = true,     // drop collection
            SupportsTruncateEntity = true, // deleteMany({})
            SupportsCreateIndex = true,    // createIndex
            SupportsDropIndex = true,      // dropIndex
            SupportsTransactionalDdl = false
            // AlterColumn / AddForeignKey / DropForeignKey: unsupported (no enforced schema/FKs)
        };

        private IMongoDatabase Database => _owner.MigrationDatabase
            ?? throw new System.InvalidOperationException("MongoDB database is not available (connection not open).");

        private IMongoCollection<BsonDocument> Collection(string entityName) => Database.GetCollection<BsonDocument>(entityName);

        public IErrorsInfo CreateEntity(EntityStructure entity)
        {
            try
            {
                Database.CreateCollection(entity.EntityName);
                return SchemaMigrationResults.Ok($"Created collection '{entity.EntityName}'.");
            }
            catch (System.Exception ex) { return SchemaMigrationResults.Fail(ex.Message, ex); }
        }

        public IErrorsInfo DropEntity(string entityName)
        {
            try
            {
                Database.RunCommand<BsonDocument>(new BsonDocument("drop", entityName));
                return SchemaMigrationResults.Ok($"Dropped collection '{entityName}'.");
            }
            catch (System.Exception ex) { return SchemaMigrationResults.Fail(ex.Message, ex); }
        }

        public IErrorsInfo TruncateEntity(string entityName)
        {
            try
            {
                Collection(entityName).DeleteMany(new BsonDocument());
                return SchemaMigrationResults.Ok($"Truncated collection '{entityName}'.");
            }
            catch (System.Exception ex) { return SchemaMigrationResults.Fail(ex.Message, ex); }
        }

        public IErrorsInfo RenameEntity(string oldName, string newName)
        {
            try
            {
                // renameCollection is an admin command and must run against the "admin" db.
                var client = _owner.MigrationClient;
                if (client == null) return SchemaMigrationResults.Fail("MongoDB client is not available.");
                var db = _owner.CurrentDatabase;
                var cmd = new BsonDocument
                {
                    { "renameCollection", $"{db}.{oldName}" },
                    { "to", $"{db}.{newName}" }
                };
                client.GetDatabase("admin").RunCommand<BsonDocument>(cmd);
                return SchemaMigrationResults.Ok($"Renamed collection '{oldName}' to '{newName}'.");
            }
            catch (System.Exception ex) { return SchemaMigrationResults.Fail(ex.Message, ex); }
        }

        // MongoDB is schemaless: adding a "column" requires no DDL — documents may carry the
        // field. We report success without mutating the collection.
        public IErrorsInfo AddColumn(string entityName, EntityField column)
            => SchemaMigrationResults.Ok($"MongoDB is schemaless; field '{column?.FieldName}' is accepted implicitly.");

        public IErrorsInfo DropColumn(string entityName, string columnName)
        {
            try
            {
                var update = Builders<BsonDocument>.Update.Unset(columnName);
                Collection(entityName).UpdateMany(new BsonDocument(), update);
                return SchemaMigrationResults.Ok($"Removed field '{columnName}' from all documents in '{entityName}'.");
            }
            catch (System.Exception ex) { return SchemaMigrationResults.Fail(ex.Message, ex); }
        }

        public IErrorsInfo RenameColumn(string entityName, string oldColumnName, string newColumnName)
        {
            try
            {
                var update = Builders<BsonDocument>.Update.Rename(oldColumnName, newColumnName);
                Collection(entityName).UpdateMany(new BsonDocument(), update);
                return SchemaMigrationResults.Ok($"Renamed field '{oldColumnName}' to '{newColumnName}' in '{entityName}'.");
            }
            catch (System.Exception ex) { return SchemaMigrationResults.Fail(ex.Message, ex); }
        }

        public IErrorsInfo CreateIndex(string entityName, string indexName, string[] columns, Dictionary<string, object> options = null)
        {
            try
            {
                if (columns == null || columns.Length == 0)
                    return SchemaMigrationResults.Fail("At least one column is required to create an index.");

                var keys = new BsonDocument(columns.Select(c => new BsonElement(c, 1)));
                var idxOptions = new CreateIndexOptions { Name = indexName };
                if (options != null && options.TryGetValue("UNIQUE", out var uniq) && uniq is bool b && b)
                    idxOptions.Unique = true;

                Collection(entityName).Indexes.CreateOne(new CreateIndexModel<BsonDocument>(keys, idxOptions));
                return SchemaMigrationResults.Ok($"Created index '{indexName}' on '{entityName}'.");
            }
            catch (System.Exception ex) { return SchemaMigrationResults.Fail(ex.Message, ex); }
        }

        public IErrorsInfo DropIndex(string entityName, string indexName)
        {
            try
            {
                Collection(entityName).Indexes.DropOne(indexName);
                return SchemaMigrationResults.Ok($"Dropped index '{indexName}' from '{entityName}'.");
            }
            catch (System.Exception ex) { return SchemaMigrationResults.Fail(ex.Message, ex); }
        }

        public IErrorsInfo AlterColumn(string entityName, string columnName, EntityField newColumn)
            => SchemaMigrationResults.Unsupported(nameof(AlterColumn), DataSourceType);
        public IErrorsInfo AddForeignKey(string entityName, string[] columnNames, string referencedEntityName, string[] referencedColumnNames, string onDeleteBehavior, string onUpdateBehavior, string constraintName)
            => SchemaMigrationResults.Unsupported(nameof(AddForeignKey), DataSourceType);
        public IErrorsInfo DropForeignKey(string entityName, string constraintName)
            => SchemaMigrationResults.Unsupported(nameof(DropForeignKey), DataSourceType);
    }
}
