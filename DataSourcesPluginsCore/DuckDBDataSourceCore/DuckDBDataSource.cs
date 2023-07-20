using DataManagementModels.DataBase;
using System.Data;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.DataBase
{
    [AddinAttribute(Category = DatasourceCategory.RDBMS, DatasourceType = DataSourceType.DuckDB)]
    public class DuckDBDataSource : RDBSource, ILocalDB, IInMemoryDB
    {
        private bool disposedValue;

        public DataSourceType DatasourceType { get; set; } = DataSourceType.DuckDB;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.INMEMORY;
        public IDataConnection Dataconnection { get ; set ; }
        public string DatasourceName { get ; set ; }
        public IErrorsInfo ErrorObject { get ; set ; }
        public string Id { get ; set ; }
        public IDMLogger Logger { get ; set ; }
        public List<string> EntitiesNames { get ; set ; }
        public List<EntityStructure> Entities { get ; set ; }
        public IDMEEditor DMEEditor { get ; set ; }
        public ConnectionState ConnectionStatus { get ; set ; }
       

        public event EventHandler<PassedArgs> PassEvent;

        private DuckDBManager dBManager;
        public DuckDBDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType pdatabasetype, IErrorsInfo per) : base(datasourcename, logger, pDMEEditor, pdatabasetype, per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = pdatabasetype;
            Dataconnection = new FileManager.FileConnection(DMEEditor)
            {
                Logger = logger,
                ErrorObject = ErrorObject,
            };
            Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.FileName == datasourcename).FirstOrDefault();
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            Category = DatasourceCategory.FILE;
           // FileName = Dataconnection.ConnectionProp.FileName;
          //  FilePath = Dataconnection.ConnectionProp.FilePath;

        }
        private string _ParameterDelimiter = ":";
        private string _ColumnDelimiter = "''";
        private string CombineFilePath=> Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName);
        public  string ParameterDelimiter { get => _ParameterDelimiter; set => _ParameterDelimiter = value; }
        public  string ColumnDelimiter { get => _ColumnDelimiter; set => _ColumnDelimiter = value; }
        public bool CanCreateLocal { get; set; }
        public bool InMemory { get; set; }
        public List<EntityStructure> InMemoryStructures { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ConnectionState Openconnection()
        {
            ConnectionStatus = Dataconnection.OpenConnection();

            if (ConnectionStatus == ConnectionState.Open)
            {
                if (DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues(Dataconnection.ConnectionProp.FileName) == null)
                {
                    // GetSheets();
                }
                else
                {
                    Entities = DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues(Dataconnection.ConnectionProp.FileName).Entities;
                };
                // CombineFilePath = Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName);
            }

            return ConnectionStatus;

        }
        public ConnectionState Closeconnection()
        {
            return ConnectionStatus = ConnectionState.Closed;
        }
        public bool CreateDB(bool inMemory)
        {
            return false;
        }

        public bool CreateDB(string filepathandname)
        {
            return false;
        }
      
        #region "Other Methods"
        public ConnectionState GetFileState()
        {
            if (ConnectionStatus == ConnectionState.Open)
            {
                return ConnectionStatus;
            }
            else
            {
                return Openconnection();
            }

        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~DuckDBDataSource()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion "Other Methods"
        #region "LocalDb Methods"

        public bool CreateDB()
        {
            try
            {
                if (!Path.HasExtension(Dataconnection.ConnectionProp.FileName))
                {
                    Dataconnection.ConnectionProp.FileName = Dataconnection.ConnectionProp.FileName + ".db";
                }
                if (!System.IO.File.Exists(Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName)))
                {
                    dBManager.CreateDatabase(Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName));
                   
                    DMEEditor.AddLogMessage("Success", "Create DuckDB Database", DateTime.Now, 0, null, Errors.Ok);
                }
                else
                {
                    DMEEditor.AddLogMessage("Success", "DuckDB Database already exist", DateTime.Now, 0, null, Errors.Ok);
                }
                return true;
            }
            catch (Exception ex)
            {
                string mes = "Could not Create Sqlite Database";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
                return false;
            };
        }

        public bool DeleteDB()
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo DropEntity(string EntityName)
        {
            throw new NotImplementedException();
        }

        public bool CopyDB(string DestDbName, string DesPath)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo OpenDatabaseInMemory(string databasename)
        {
            throw new NotImplementedException();
        }

        public string GetConnectionString()
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo SaveStructure()
        {
            throw new NotImplementedException();
        }
        #endregion "LocalDb Methods"

    }
}
