using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.DataView;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Composite;
using System.Data;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Composite
{
    [AddinAttribute(Category = DatasourceCategory.RDBMS, DatasourceType = DataSourceType.SqlLite)]
    public class CompositeLayerDataSource : IDataSource, ICompositeLayerDataSource
    {
        private bool disposedValue;

        public CompositeLayerDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per) 
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = DMEEditor;
            DatasourceType = databasetype;
            Category = DatasourceCategory.RDBMS;

            EntitiesNames = new List<string>();
            Entities = new List<EntityStructure>();
            LayerInfo = new CompositeLayer();

            Dataconnection = new WebAPIDataConnection
            {
                Logger = logger,
                ErrorObject = ErrorObject
            };

            if (DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == datasourcename).Any())
            {
                Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == datasourcename).FirstOrDefault();
            }

            // Initialize LocalDB for composite storage
            // LocalDB will be set by the caller or created here if needed
        }

        public string ColumnDelimiter { get; set; } = ",";
        public string ParameterDelimiter { get; set; } = ":";
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public DataSourceType DatasourceType { get; set; }
        public DatasourceCategory Category { get; set; }
        public IDataConnection Dataconnection { get; set; }
        public string DatasourceName { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public string Id { get; set; }
        public IDMLogger Logger { get; set; }
        public List<string> EntitiesNames { get; set; } = new List<string>();
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
        public IDMEEditor DMEEditor { get; set; }
        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Closed;

        public string DatabaseType => LocalDB?.DatasourceName ?? "Composite";

        public IDataViewDataSource DataViewSource { get; set; }
        public CompositeLayer LayerInfo { get; set; }
        public ILocalDB LocalDB { get; set; }

        public event EventHandler<PassedArgs> PassEvent;
        public event EventHandler<PassedArgs> OnLoadData;
        public event EventHandler<PassedArgs> OnLoadStructure;
        public event EventHandler<PassedArgs> OnSaveStructure;
        public event EventHandler<PassedArgs> OnCreateStructure;
        public event EventHandler<PassedArgs> OnRefreshData;
        public event EventHandler<PassedArgs> OnRefreshDataEntity;
        public event EventHandler<PassedArgs> OnSyncData;

        public IErrorsInfo BeginTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (LocalDB != null)
                {
                    ErrorObject = LocalDB.BeginTransaction(args);
                }
                else if (DataViewSource != null)
                {
                    ErrorObject = DataViewSource.BeginTransaction(args);
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in Begin Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public bool CheckEntityExist(string EntityName)
        {
            bool retval = false;
            try
            {
                if (DataViewSource != null)
                {
                    retval = DataViewSource.CheckEntityExist(EntityName);
                }
                else if (LocalDB != null)
                {
                    retval = LocalDB.CheckEntityExist(EntityName);
                }
                else
                {
                    retval = Entities.Any(e => e.EntityName.Equals(EntityName, StringComparison.OrdinalIgnoreCase));
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in CheckEntityExist: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                retval = false;
            }
            return retval;
        }

        public ConnectionState Closeconnection()
        {
            try
            {
                if (LocalDB != null)
                {
                    ConnectionStatus = LocalDB.Closeconnection();
                }
                else if (DataViewSource != null)
                {
                    ConnectionStatus = DataViewSource.Closeconnection();
                }
                else
                {
                    ConnectionStatus = ConnectionState.Closed;
                }
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                DMEEditor?.AddLogMessage("Beep", $"Could not close CompositeLayer {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ConnectionStatus;
        }

        public IErrorsInfo Commit(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (LocalDB != null)
                {
                    ErrorObject = LocalDB.Commit(args);
                }
                else if (DataViewSource != null)
                {
                    ErrorObject = DataViewSource.Commit(args);
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in Commit Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (entities != null && entities.Count > 0)
                {
                    foreach (var entity in entities)
                    {
                        AddEntitytoLayer(entity);
                    }
                    CreateLayer();
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in CreateEntities: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            bool retval = false;
            try
            {
                retval = AddEntitytoLayer(entity);
                if (retval && LocalDB != null)
                {
                    LocalDB.CreateEntityAs(entity);
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in CreateEntityAs: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                retval = false;
            }
            return retval;
        }

        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (LocalDB != null)
                {
                    ErrorObject = LocalDB.DeleteEntity(EntityName, UploadDataRow);
                }
                else if (DataViewSource != null)
                {
                    ErrorObject = DataViewSource.DeleteEntity(EntityName, UploadDataRow);
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in DeleteEntity: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public IErrorsInfo EndTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (LocalDB != null)
                {
                    ErrorObject = LocalDB.EndTransaction(args);
                }
                else if (DataViewSource != null)
                {
                    ErrorObject = DataViewSource.EndTransaction(args);
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in End Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public IErrorsInfo ExecuteSql(string sql)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (LocalDB != null)
                {
                    ErrorObject = LocalDB.ExecuteSql(sql);
                }
                else if (DataViewSource != null)
                {
                    ErrorObject = DataViewSource.ExecuteSql(sql);
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in ExecuteSql: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public IEnumerable<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            List<ChildRelation> results = new List<ChildRelation>();
            try
            {
                if (DataViewSource != null)
                {
                    results = DataViewSource.GetChildTablesList(tablename, SchemaName, Filterparamters).ToList();
                }
                else if (LocalDB != null)
                {
                    results = LocalDB.GetChildTablesList(tablename, SchemaName, Filterparamters).ToList();
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in GetChildTablesList: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return results;
        }

        public IEnumerable<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            List<ETLScriptDet> scripts = new List<ETLScriptDet>();
            try
            {
                var entitiesToScript = entities ?? LayerInfo?.Entities ?? Entities;
                if (entitiesToScript != null && entitiesToScript.Count > 0)
                {
                    foreach (var entity in entitiesToScript)
                    {
                        var script = new ETLScriptDet
                        {
                            EntityName = entity.EntityName,
                           ScriptType= "CREATE",
                            ScriptText = $"# Composite entity: {entity.EntityName}"
                        };
                        scripts.Add(script);
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in GetCreateEntityScript: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return scripts;
        }

        public IEnumerable<string> GetEntitesList()
        {
            EntitiesNames = new List<string>();
            ErrorObject.Flag = Errors.Ok;

            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                // Get entities from LayerInfo first
                if (LayerInfo?.Entities != null && LayerInfo.Entities.Count > 0)
                {
                    EntitiesNames = LayerInfo.Entities.Select(e => e.EntityName).ToList();
                    Entities = LayerInfo.Entities.ToList();
                }
                else if (DataViewSource != null)
                {
                    // Get entities from DataViewSource
                    var dataViewEntities = DataViewSource.GetEntitesList();
                    if (dataViewEntities != null)
                    {
                        EntitiesNames = dataViewEntities.ToList();
                        
                        // Get entity structures
                        foreach (var entityName in EntitiesNames)
                        {
                            var entity = DataViewSource.GetEntityStructure(entityName, false);
                            if (entity != null)
                            {
                                Entities.Add(entity);
                            }
                        }
                    }
                }
                else if (LocalDB != null)
                {
                    // Get entities from LocalDB
                    var localEntities = LocalDB.GetEntitesList();
                    if (localEntities != null)
                    {
                        EntitiesNames = localEntities.ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in GetEntitesList: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return EntitiesNames;
        }

        public IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            ErrorObject.Flag = Errors.Ok;
            List<object> results = new List<object>();

            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                // Try DataViewSource first
                if (DataViewSource != null && ConnectionStatus == ConnectionState.Open)
                {
                    var dataViewResults = DataViewSource.GetEntity(EntityName, filter);
                    if (dataViewResults != null)
                    {
                        results.AddRange(dataViewResults);
                    }
                }
                else if (LocalDB != null)
                {
                    var localResults = LocalDB.GetEntity(EntityName, filter);
                    if (localResults != null)
                    {
                        results.AddRange(localResults);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in GetEntity: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return results;
        }

        public PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            PagedResult pagedResult = new PagedResult();
            ErrorObject.Flag = Errors.Ok;

            try
            {
                if (DataViewSource != null)
                {
                    pagedResult = DataViewSource.GetEntity(EntityName, filter, pageNumber, pageSize);
                }
                else if (LocalDB != null)
                {
                    pagedResult = LocalDB.GetEntity(EntityName, filter, pageNumber, pageSize);
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in GetEntity (paged): {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return pagedResult;
        }

        public Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            return Task.Run(() => GetEntity(EntityName, Filter));
        }

        public IEnumerable<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            List<RelationShipKeys> results = new List<RelationShipKeys>();
            try
            {
                if (DataViewSource != null)
                {
                    results = DataViewSource.GetEntityforeignkeys(entityname, SchemaName).ToList();
                }
                else if (LocalDB != null)
                {
                    results = LocalDB.GetEntityforeignkeys(entityname, SchemaName).ToList();
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in GetEntityforeignkeys: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return results;
        }

        public int GetEntityIdx(string entityName)
        {
            if (Entities != null && Entities.Count > 0)
            {
                return Entities.FindIndex(p => p.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase) 
                    || p.DatasourceEntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase)
                    || p.OriginalEntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
            }
            return -1;
        }

        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            ErrorObject.Flag = Errors.Ok;
            EntityStructure retval = null;

            try
            {
                if (!refresh && Entities != null && Entities.Count > 0)
                {
                    retval = Entities.Find(c => c.EntityName.Equals(EntityName, StringComparison.OrdinalIgnoreCase));
                    if (retval != null)
                    {
                        return retval;
                    }
                }

                // Try to get from DataViewSource
                if (DataViewSource != null)
                {
                    retval = DataViewSource.GetEntityStructure(EntityName, refresh);
                }
                else if (LocalDB != null)
                {
                    retval = LocalDB.GetEntityStructure(EntityName, refresh);
                }

                if (retval != null)
                {
                    int idx = GetEntityIdx(EntityName);
                    if (idx >= 0)
                    {
                        Entities[idx] = retval;
                    }
                    else
                    {
                        Entities.Add(retval);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in GetEntityStructure: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return retval;
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            if (fnd != null && !string.IsNullOrEmpty(fnd.EntityName))
            {
                return GetEntityStructure(fnd.EntityName, refresh);
            }
            return null;
        }

        public Type GetEntityType(string EntityName)
        {
            Type retval = null;
            try
            {
                var entityStructure = GetEntityStructure(EntityName, false);
                if (entityStructure != null && entityStructure.Fields != null && entityStructure.Fields.Count > 0)
                {
                    DMTypeBuilder.CreateNewObject(DMEEditor, "TheTechIdea.Classes", EntityName, entityStructure.Fields);
                    retval = DMTypeBuilder.MyType;
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in GetEntityType: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return retval;
        }

        public double GetScalar(string query)
        {
            double retval = 0.0;
            try
            {
                if (LocalDB != null)
                {
                    retval = LocalDB.GetScalar(query);
                }
                else if (DataViewSource != null)
                {
                    retval = DataViewSource.GetScalar(query);
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Fail", $"Error in executing scalar query ({ex.Message})", DateTime.Now, 0, "", Errors.Failed);
            }
            return retval;
        }

        public Task<double> GetScalarAsync(string query)
        {
            return Task.Run(() => GetScalar(query));
        }

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (LocalDB != null)
                {
                    ErrorObject = LocalDB.InsertEntity(EntityName, InsertedData);
                }
                else if (DataViewSource != null)
                {
                    ErrorObject = DataViewSource.InsertEntity(EntityName, InsertedData);
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in InsertEntity: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public ConnectionState Openconnection()
        {
            try
            {
                // Open LocalDB connection
                if (LocalDB != null)
                {
                    ConnectionStatus = LocalDB.Openconnection();
                }
                else if (DataViewSource != null)
                {
                    ConnectionStatus = DataViewSource.Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    DMEEditor?.AddLogMessage("Beep", "CompositeLayer connection opened successfully.", DateTime.Now, -1, null, Errors.Ok);
                }
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                DMEEditor?.AddLogMessage("Beep", $"Could not open CompositeLayer {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ConnectionStatus;
        }

        public IEnumerable<object> RunQuery(string qrystr)
        {
            List<object> results = new List<object>();
            try
            {
                if (DataViewSource != null)
                {
                    results = DataViewSource.RunQuery(qrystr).ToList();
                }
                else if (LocalDB != null)
                {
                    results = LocalDB.RunQuery(qrystr).ToList();
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in RunQuery: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return results;
        }

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (LocalDB != null)
                {
                    ErrorObject = LocalDB.RunScript(dDLScripts);
                }
                else if (DataViewSource != null)
                {
                    ErrorObject = DataViewSource.RunScript(dDLScripts);
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in RunScript: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (LocalDB != null)
                {
                    ErrorObject = LocalDB.UpdateEntities(EntityName, UploadData, progress);
                }
                else if (DataViewSource != null)
                {
                    ErrorObject = DataViewSource.UpdateEntities(EntityName, UploadData, progress);
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in UpdateEntities: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (LocalDB != null)
                {
                    ErrorObject = LocalDB.UpdateEntity(EntityName, UploadDataRow);
                }
                else if (DataViewSource != null)
                {
                    ErrorObject = DataViewSource.UpdateEntity(EntityName, UploadDataRow);
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in UpdateEntity: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ErrorObject;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        if (LocalDB is IDisposable localDisposable)
                        {
                            localDisposable.Dispose();
                        }
                        if (DataViewSource is IDisposable dataViewDisposable)
                        {
                            dataViewDisposable.Dispose();
                        }
                        ConnectionStatus = ConnectionState.Closed;
                    }
                    catch (Exception ex)
                    {
                        DMEEditor?.AddLogMessage("Beep", $"Error disposing CompositeLayer connection: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                    }
                }
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~CompositeLayerDataSource()
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

        public bool AddEntitytoLayer(EntityStructure entity)
        {
            bool retval = false;
            try
            {
                if (entity != null && LayerInfo != null)
                {
                    if (LayerInfo.Entities == null)
                    {
                        LayerInfo.Entities = new List<EntityStructure>();
                    }

                    if (!LayerInfo.Entities.Any(e => e.EntityName.Equals(entity.EntityName, StringComparison.OrdinalIgnoreCase)))
                    {
                        LayerInfo.Entities.Add(entity);
                        Entities.Add(entity);
                        EntitiesNames.Add(entity.EntityName);
                        retval = true;
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in AddEntitytoLayer: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                retval = false;
            }
            return retval;
        }

        public IErrorsInfo CreateLayer()
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (LocalDB != null)
                {
                    // Create layer tables in LocalDB
                    if (LayerInfo?.Entities != null && LayerInfo.Entities.Count > 0)
                    {
                        LocalDB.CreateEntities(LayerInfo.Entities);
                    }
                }
                OnCreateStructure?.Invoke(this, (PassedArgs)DMEEditor?.Passedarguments);
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in CreateLayer: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public bool DropDatabase()
        {
            bool retval = false;
            try
            {
                if (LocalDB != null)
                {
                    retval = LocalDB.DropDatabase();
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in DropDatabase: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                retval = false;
            }
            return retval;
        }

        public IErrorsInfo DropEntity(string EntityName)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (LocalDB != null)
                {
                    ErrorObject = LocalDB.DropEntity(EntityName);
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in DropEntity: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public bool GetAllEntitiesFromDataView()
        {
            bool retval = false;
            try
            {
                if (DataViewSource != null)
                {
                    var entities = DataViewSource.GetEntitesList();
                    if (entities != null)
                    {
                        foreach (var entityName in entities)
                        {
                            var entity = DataViewSource.GetEntityStructure(entityName, false);
                            if (entity != null)
                            {
                                AddEntitytoLayer(entity);
                            }
                        }
                        retval = true;
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in GetAllEntitiesFromDataView: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                retval = false;
            }
            return retval;
        }
    }
}
