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
        private bool disposedValue;
        private bool _transactionStarted;

        public bool CanCreateLocal { get; set; }
        public bool InMemory { get; set; } = false;
        public bool IsCreated { get; set; } = false;
        public bool IsLoaded { get; set; } = false;
        public bool IsSaved { get; set; } = false;
        public bool IsSynced { get; set; } = false;
        public bool IsStructureLoaded { get; set; } = false;
        public bool IsStructureCreated { get; set; } = false;
        public string Extension { get; set; } = ".s3db";
        public ETLScriptHDR CreateScript { get; set; } = new ETLScriptHDR();
        public List<EntityStructure> InMemoryStructures { get; set; } = new List<EntityStructure>();

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

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposedValue)
            {
                return;
            }

            if (disposing)
            {
                Closeconnection();
            }

            disposedValue = true;
        }
    }
}
