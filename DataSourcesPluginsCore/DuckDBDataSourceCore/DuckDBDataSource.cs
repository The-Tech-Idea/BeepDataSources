using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.DataBase
{
    [AddinAttribute(Category = DatasourceCategory.RDBMS, DatasourceType = DataSourceType.DuckDB)]
    public class DuckDBDataSource : IDataSource, ILocalDB
    {
        private bool disposedValue;

        public DataSourceType DatasourceType { get ; set ; }
        public DatasourceCategory Category { get ; set ; }
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
        public DuckDBDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType pDatasourceType, IErrorsInfo per) 
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = pDatasourceType;
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
        public bool CreateDB(bool inMemory)
        {
            return false;
        }

        public bool CreateDB(string filepathandname)
        {
            return false;
        }
        public DuckDBDataSource(string dbfile)
        {
            if (!string.IsNullOrEmpty(dbfile))
            {
                dBManager = new DuckDBManager(dbfile);
            }
            
                
        }
        public DuckDBDataSource(bool inMemory = true)
        {
            dBManager = new DuckDBManager(true);
        }
        public IErrorsInfo BeginTransaction(PassedArgs args)
        {
            return DMEEditor.ErrorObject;
        }

        public bool CheckEntityExist(string EntityName)
        {
            throw new NotImplementedException();
        }

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

        public IErrorsInfo Commit(PassedArgs args)
        {
            return DMEEditor.ErrorObject;
        }

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            throw new NotImplementedException();
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo EndTransaction(PassedArgs args)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo ExecuteSql(string sql)
        {
            throw new NotImplementedException();
        }

        public List<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            throw new NotImplementedException();
        }

        public List<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            throw new NotImplementedException();
        }

        public List<string> GetEntitesList()
        {
            throw new NotImplementedException();
        }

        public object GetEntity(string EntityName, List<AppFilter> filter)
        {
            throw new NotImplementedException();
        }

        public Task<object> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            throw new NotImplementedException();
        }

        public List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            throw new NotImplementedException();
        }

        public int GetEntityIdx(string entityName)
        {
            throw new NotImplementedException();
        }

        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            throw new NotImplementedException();
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            throw new NotImplementedException();
        }

        public Type GetEntityType(string EntityName)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            throw new NotImplementedException();
        }

      
        public object RunQuery(string qrystr)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            throw new NotImplementedException();
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
                    Dataconnection.ConnectionProp.FileName = Dataconnection.ConnectionProp.FileName + ".s3db";
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
        #endregion "LocalDb Methods"

    }
}
