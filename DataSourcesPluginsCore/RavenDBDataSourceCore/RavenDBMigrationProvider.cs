using System.Collections.Generic;
using System.Linq;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Operations.Indexes;
using Raven.Client.Documents.Queries;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.SchemaMigration;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.NOSQL.RavenDB
{
    /// <summary>
    /// Colocated Tier-1 <see cref="ISchemaMigrationProvider"/> for <see cref="DataSourceType.RavenDB"/>.
    /// RavenDB collections auto-create on first document insert, so CreateEntity is a no-op
    /// (delegates to the datasource's structure save). Drop/truncate empty a collection via
    /// <c>DeleteByQueryOperation</c>; indexes use RavenDB static-map indexes. Documents are
    /// schemaless JSON, so column add/rename/drop report success without structural DDL.
    /// Rename-collection, alter-column and foreign keys are unsupported.
    /// </summary>
    [SchemaMigrationProvider(DataSourceType.RavenDB, DatasourceCategory.NOSQL)]
    public class RavenDBMigrationProvider : ISchemaMigrationProvider
    {
        private readonly RavenDBDataSource _owner;

        public RavenDBMigrationProvider(IDataSource owner)
        {
            _owner = owner as RavenDBDataSource
                     ?? throw new System.ArgumentNullException(nameof(owner), "Expected a RavenDBDataSource instance.");
        }

        public DataSourceType DataSourceType => DataSourceType.RavenDB;
        public DatasourceCategory Category => DatasourceCategory.NOSQL;

        public SchemaMigrationCapabilities Capabilities { get; } = new SchemaMigrationCapabilities
        {
            SupportsCreateEntity = true,   // collection auto-creates; structure saved
            SupportsAddColumn = true,      // schemaless JSON → no-op Ok
            SupportsDropColumn = true,     // schemaless JSON → no-op Ok
            SupportsRenameColumn = true,   // schemaless JSON → no-op Ok
            SupportsDropEntity = true,     // delete all documents in collection
            SupportsTruncateEntity = true, // delete all documents in collection
            SupportsCreateIndex = true,    // static map index
            SupportsDropIndex = true,      // delete index
            SupportsTransactionalDdl = false
            // RenameEntity / AlterColumn / AddForeignKey / DropForeignKey: unsupported
        };

        private IDocumentStore Store => _owner.Store
            ?? throw new System.InvalidOperationException("RavenDB DocumentStore is not initialized.");

        public IErrorsInfo CreateEntity(EntityStructure entity)
        {
            // Collections auto-create on insert; the datasource persists the structure.
            var ok = _owner.CreateEntityAs(entity);
            return ok
                ? SchemaMigrationResults.Ok($"Collection '{entity?.EntityName}' registered (auto-created on first insert).")
                : SchemaMigrationResults.Fail($"Failed to register collection '{entity?.EntityName}'.");
        }

        public IErrorsInfo DropEntity(string entityName)
        {
            try
            {
                var op = new DeleteByQueryOperation(new IndexQuery { Query = $"from \"{entityName}\"" });
                Store.Operations.Send(op);
                return SchemaMigrationResults.Ok($"Deleted all documents in collection '{entityName}'.");
            }
            catch (System.Exception ex) { return SchemaMigrationResults.Fail(ex.Message, ex); }
        }

        public IErrorsInfo TruncateEntity(string entityName) => DropEntity(entityName);

        public IErrorsInfo CreateIndex(string entityName, string indexName, string[] columns, Dictionary<string, object> options = null)
        {
            try
            {
                if (columns == null || columns.Length == 0)
                    return SchemaMigrationResults.Fail("At least one column is required to create an index.");

                // Minimal static map index projecting the indexed fields.
                var projection = string.Join(", ", columns.Select(c => $"{c} = doc.{c}"));
                var definition = new IndexDefinition
                {
                    Name = indexName,
                    Maps =
                    {
                        $"docs.{entityName}.Select(doc => new {{ {projection} }})"
                    }
                };
                Store.Maintenance.Send(new PutIndexesOperation(definition));
                return SchemaMigrationResults.Ok($"Created index '{indexName}' on '{entityName}'.");
            }
            catch (System.Exception ex) { return SchemaMigrationResults.Fail(ex.Message, ex); }
        }

        public IErrorsInfo DropIndex(string entityName, string indexName)
        {
            try
            {
                Store.Maintenance.Send(new DeleteIndexOperation(indexName));
                return SchemaMigrationResults.Ok($"Dropped index '{indexName}'.");
            }
            catch (System.Exception ex) { return SchemaMigrationResults.Fail(ex.Message, ex); }
        }

        // RavenDB documents are schemaless JSON — field-level changes need no structural DDL.
        public IErrorsInfo AddColumn(string entityName, EntityField column)
            => SchemaMigrationResults.Ok($"RavenDB is schemaless; field '{column?.FieldName}' is accepted implicitly.");
        public IErrorsInfo DropColumn(string entityName, string columnName)
            => SchemaMigrationResults.Ok($"RavenDB is schemaless; field '{columnName}' removal requires a document patch (no DDL).");
        public IErrorsInfo RenameColumn(string entityName, string oldColumnName, string newColumnName)
            => SchemaMigrationResults.Ok($"RavenDB is schemaless; rename '{oldColumnName}'→'{newColumnName}' requires a document patch (no DDL).");

        public IErrorsInfo RenameEntity(string oldName, string newName)
            => SchemaMigrationResults.Unsupported(nameof(RenameEntity), DataSourceType);
        public IErrorsInfo AlterColumn(string entityName, string columnName, EntityField newColumn)
            => SchemaMigrationResults.Unsupported(nameof(AlterColumn), DataSourceType);
        public IErrorsInfo AddForeignKey(string entityName, string[] columnNames, string referencedEntityName, string[] referencedColumnNames, string onDeleteBehavior, string onUpdateBehavior, string constraintName)
            => SchemaMigrationResults.Unsupported(nameof(AddForeignKey), DataSourceType);
        public IErrorsInfo DropForeignKey(string entityName, string constraintName)
            => SchemaMigrationResults.Unsupported(nameof(DropForeignKey), DataSourceType);
    }
}
