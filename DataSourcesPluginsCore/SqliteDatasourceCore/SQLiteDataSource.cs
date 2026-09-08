using System.Data;
using System.Data.SQLite;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;

namespace TheTechIdea.Beep.DataBase
{
    [AddinAttribute(Category = DatasourceCategory.RDBMS, DatasourceType = DataSourceType.SqlLite)]
    public partial class SQLiteDataSource : InMemoryRDBSource, ILocalDB, IDataSource, IDisposable
    {
        // BeginTransaction / Commit / EndTransaction are intentionally NOT declared in this class.
        // SQLiteDataSource.Transactions.cs used to reimplement them with a private _transactionStarted
        // flag and literal "BEGIN TRANSACTION;" / "COMMIT;" / "ROLLBACK;" SQL text, entirely disconnected
        // from RDBSource's real _activeTransaction (the ADO.NET IDbTransaction that GetDataCommand
        // attaches to every command). Two problems: (1) because this class re-declares IDataSource,
        // those methods -- declared `public virtual`, not `override` -- hid RDBSource's real transaction
        // handling from every caller holding this datasource as IDataSource, the normal way the engine
        // consumes a plugin; and (2) RDBSource.ActiveTransaction stayed null throughout, so bulk
        // operations checking it to avoid nesting a transaction (K8) could not see the raw-SQL
        // transaction was open, risking a second, real ADO.NET transaction starting on top of it.
        // Removed; RDBSource's tested implementation now runs for SQLite too.

        public bool CanCreateLocal { get; set; }
        public bool InMemory { get; set; } = false;
        public string Extension { get; set; } = ".s3db";

        // IsCreated / IsLoaded / IsSaved / IsSynced / IsStructureLoaded / IsStructureCreated /
        // CreateScript / InMemoryStructures are NOT re-declared here. They used to be, as plain
        // auto-properties with the same names as InMemoryRDBSource's -- which gave this class a
        // SEPARATE backing field for each, entirely disconnected from the base class's copy. Since
        // SQLiteDataSource does not itself re-list IInMemoryDB (only InMemoryRDBSource does), any
        // caller holding this datasource as IInMemoryDB read and wrote the BASE class's copy, while
        // this class's own methods (OpenDatabaseInMemory, LoadData, ...) read and wrote the
        // SHADOWED copy declared here -- so `OpenDatabaseInMemory` setting `IsCreated = true` was
        // invisible to anything checking `IsCreated` through the base. Inheriting InMemoryRDBSource's
        // single copy of each removes the desync.

        public override string ColumnDelimiter { get; set; } = "[]";
        public override string ParameterDelimiter { get; set; } = "$";

        public SQLiteDataSource(string pdatasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
            : base(pdatasourcename, logger, pDMEEditor, databasetype, per)
        {
            DMEEditor = pDMEEditor;
            DatasourceName = pdatasourcename;

            if (!string.IsNullOrEmpty(pdatasourcename) && DMEEditor?.ConfigEditor != null)
            {
                Dataconnection ??= new RDBDataConnection(DMEEditor);
                Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections
                    .FirstOrDefault(p => p.ConnectionName != null &&
                                         p.ConnectionName.Equals(pdatasourcename, StringComparison.InvariantCultureIgnoreCase))
                    ?? new ConnectionProperties();

                Dataconnection.DataSourceDriver = ConnectionHelper.LinkConnection2Drivers(Dataconnection.ConnectionProp, DMEEditor.ConfigEditor);
            }

            // Defaults — EnsureConnectionProp may override during Open. Don't set IsLocal here; that
            // belongs to EnsureConnectionProp which is the single source of truth for connection-prop defaults.
        }

        // Dispose() / Dispose(bool) are NOT re-declared here either. This class used to declare its
        // own complete, non-overriding Dispose pattern -- `public void Dispose()` hiding
        // RDBSource.Dispose(), and `protected virtual void Dispose(bool disposing)` hiding
        // InMemoryRDBSource.Dispose(bool) -- that called Closeconnection() and nothing else, with no
        // `override` and no `base.Dispose(disposing)` call. Since `using var ds = new
        // SQLiteDataSource(...)` calls Dispose() on the compile-time SQLiteDataSource type directly,
        // every normal disposal ran this shadow instead of RDBSource's real Dispose(bool) chain --
        // meaning the K22 rework (rolling back a pending transaction, disposing the cached command,
        // clearing the entity-structure cache, and Dispose()-ing rather than merely Close()-ing the
        // provider connection) never ran for SQLite. Closeconnection() alone leaks exactly what K12
        // was about: the native handles Dispose() releases and Close() does not.
        // Inheriting RDBSource.Dispose() runs that whole chain, doing everything the old override did
        // and more.
    }
}
