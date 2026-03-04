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
        private readonly string dateformat = "yyyy-MM-dd HH:mm:ss";
        private bool _transactionStarted;
        private SQLiteConnection sQLiteConnection;
        private string dbpath;

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

            if (!string.IsNullOrEmpty(pdatasourcename))
            {
                Dataconnection ??= new RDBDataConnection(DMEEditor);
                Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections
                    .FirstOrDefault(p => p.ConnectionName != null &&
                                         p.ConnectionName.Equals(pdatasourcename, StringComparison.InvariantCultureIgnoreCase))
                    ?? new ConnectionProperties();

                Dataconnection.DataSourceDriver = ConnectionHelper.LinkConnection2Drivers(Dataconnection.ConnectionProp, DMEEditor.ConfigEditor);
            }

            Dataconnection.ConnectionProp.DatabaseType = DataSourceType.SqlLite;
            Dataconnection.ConnectionProp.Category = DatasourceCategory.RDBMS;
            Dataconnection.ConnectionProp.IsLocal = true;
            Dataconnection.ConnectionProp.IsDatabase = true;
            Dataconnection.ConnectionProp.IsFile = true;

            ColumnDelimiter = "[]";
            ParameterDelimiter = "$";
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
