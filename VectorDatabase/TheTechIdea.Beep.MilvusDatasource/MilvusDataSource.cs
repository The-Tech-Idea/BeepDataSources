using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Connections;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;

namespace TheTechIdea.Beep.MilvusDatasource
{
    [AddinAttribute(Category = DatasourceCategory.VectorDB, DatasourceType = DataSourceType.Milvus)]
    public class MilvusDataSource : IDataSource
    { 
        #region IDataSource Constructor

        // Constructor with standard signature for IDataSource implementations
        public MilvusDataSource(string pdatasourcename, IDMLogger plogger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
        {
            DMEEditor = pDMEEditor;
            Logger = plogger;
            ErrorObject = per ?? DMEEditor.ErrorObject;
            DatasourceName = pdatasourcename;
            DatasourceType = DataSourceType.Qdrant;
            Category = DatasourceCategory.VectorDB;

            if (pdatasourcename != null)
            {
                // Initialize data connection with connection properties
                Dataconnection = new DefaulDataConnection
                {
                    DMEEditor = DMEEditor,
                    ConnectionProp = DMEEditor.ConfigEditor.DataConnections.FirstOrDefault(
                        p => p.ConnectionName.Equals(pdatasourcename, StringComparison.InvariantCultureIgnoreCase))
                };

                if (Dataconnection.ConnectionProp == null)
                {
                    Dataconnection.ConnectionProp = new ConnectionProperties();
                }
            }

            ColumnDelimiter = "[]";
            ParameterDelimiter = "$";
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
        }
        #endregion
        #region IDataSource Properties
        private bool disposedValue;

        public string ColumnDelimiter { get; set; }
        public string ParameterDelimiter { get; set; }
        public string GuidID { get; set; }
        public DataSourceType DatasourceType { get; set; }
        public DatasourceCategory Category { get; set; }
        public IDataConnection Dataconnection { get; set; }
        public string DatasourceName { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public string Id { get; set; }
        public IDMLogger Logger { get; set; }
        public List<string> EntitiesNames { get; set; }
        public List<EntityStructure> Entities { get; set; }
        public IDMEEditor DMEEditor { get; set; }
        public ConnectionState ConnectionStatus { get; set; }

        #endregion
        #region IDataSource Methods


        public event EventHandler<PassedArgs> PassEvent;

        public IErrorsInfo BeginTransaction(PassedArgs args)
        {
            // Initialize the error object
            if (ErrorObject == null)
            {
                ErrorObject = new ErrorsInfo();
                ErrorObject.Flag = Errors.Ok;
            }
            try
            {
                
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                ErrorObject.Message = ex.Message;
                DMEEditor.AddLogMessage("Beep", "Error in BeginTransaction { ex.Message}", DateTime.Now,-1, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public bool CheckEntityExist(string EntityName)
        {
            throw new NotImplementedException();
        }

        public ConnectionState Closeconnection()
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo Commit(PassedArgs args)
        {
            // Initialize the error object
            if (ErrorObject == null)
            {
                ErrorObject = new ErrorsInfo();
                ErrorObject.Flag = Errors.Ok;
            }
            try
            {

            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                ErrorObject.Message = ex.Message;
                DMEEditor.AddLogMessage("Beep", "Error in BeginTransaction { ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            // Initialize the error object
            if (ErrorObject == null)
            {
                ErrorObject = new ErrorsInfo();
                ErrorObject.Flag = Errors.Ok;
            }
            try
            {

            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                ErrorObject.Message = ex.Message;
                DMEEditor.AddLogMessage("Beep", "Error in BeginTransaction { ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            // Initialize the error object
            if (ErrorObject == null)
            {
                ErrorObject = new ErrorsInfo();
                ErrorObject.Flag = Errors.Ok;
            }
            try
            {

            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                ErrorObject.Message = ex.Message;
                DMEEditor.AddLogMessage("Beep", "Error in BeginTransaction { ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public IErrorsInfo EndTransaction(PassedArgs args)
        {
            // Initialize the error object
            if (ErrorObject == null)
            {
                ErrorObject = new ErrorsInfo();
                ErrorObject.Flag = Errors.Ok;
            }
            try
            {

            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                ErrorObject.Message = ex.Message;
                DMEEditor.AddLogMessage("Beep", "Error in BeginTransaction { ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public IErrorsInfo ExecuteSql(string sql)
        {
            // Initialize the error object
            if (ErrorObject == null)
            {
                ErrorObject = new ErrorsInfo();
                ErrorObject.Flag = Errors.Ok;
            }
            try
            {

            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                ErrorObject.Message = ex.Message;
                DMEEditor.AddLogMessage("Beep", "Error in BeginTransaction { ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ErrorObject;
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

        public object GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
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

        public double GetScalar(string query)
        {
            throw new NotImplementedException();
        }

        public Task<double> GetScalarAsync(string query)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            // Initialize the error object
            if (ErrorObject == null)
            {
                ErrorObject = new ErrorsInfo();
                ErrorObject.Flag = Errors.Ok;
            }
            try
            {

            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                ErrorObject.Message = ex.Message;
                DMEEditor.AddLogMessage("Beep", "Error in BeginTransaction { ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public ConnectionState Openconnection()
        {
            throw new NotImplementedException();
        }

        public object RunQuery(string qrystr)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            // Initialize the error object
            if (ErrorObject == null)
            {
                ErrorObject = new ErrorsInfo();
                ErrorObject.Flag = Errors.Ok;
            }
            try
            {

            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                ErrorObject.Message = ex.Message;
                DMEEditor.AddLogMessage("Beep", "Error in BeginTransaction { ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            // Initialize the error object
            if (ErrorObject == null)
            {
                ErrorObject = new ErrorsInfo();
                ErrorObject.Flag = Errors.Ok;
            }
            try
            {

            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                ErrorObject.Message = ex.Message;
                DMEEditor.AddLogMessage("Beep", "Error in BeginTransaction { ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            // Initialize the error object
            if (ErrorObject == null)
            {
                ErrorObject = new ErrorsInfo();
                ErrorObject.Flag = Errors.Ok;
            }
            try
            {

            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                ErrorObject.Message = ex.Message;
                DMEEditor.AddLogMessage("Beep", "Error in BeginTransaction { ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ErrorObject;
        }

        #endregion
        #region IDataSource Events
        #endregion

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
        // ~MilvusDataSource()
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
    }
}
