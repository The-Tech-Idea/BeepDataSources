using TheTechIdea.Beep.DataBase;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor.ETL;
using TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.Core;



namespace TheTechIdea.Beep
{
    public class InMemoryRDBSource : RDBSource, IInMemoryDB, IDisposable
    {
        #region "InMemoryDataSource Constructors"

        public InMemoryRDBSource(string pdatasourcename, IDMLogger plogger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per) : base(pdatasourcename, plogger, pDMEEditor, databasetype, per)
        {
            DMEEditor = pDMEEditor;
            Logger = plogger;
            ErrorObject = per;
            DatasourceName = pdatasourcename;
            DatasourceType = databasetype;
           
        }

        #endregion
        #region "InMemoryDataSource Properties"
        public bool IsCreated { get; set; } = false;
        public bool IsLoaded { get; set; } = false;
        public bool IsSaved { get; set; } = false;
        public bool IsSynced { get; set; } = false;
        public bool IsStructureCreated { get; set; } = false;
        public bool IsStructureLoaded { get; set; } = false;
        public ETLScriptHDR CreateScript { get; set; } = new ETLScriptHDR();
        public List<EntityStructure> InMemoryStructures { get; set; } = new List<EntityStructure>();
 
        #endregion
        #region "InMemoryDataSource Methods"
        public virtual string GetConnectionString()
        {
            return Dataconnection.ConnectionProp.ConnectionString;
        }
        public virtual IErrorsInfo LoadData(IProgress<PassedArgs> progress, CancellationToken token)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                if ( IsCreated)
                {
                    List<ETLScriptDet> retscripts = DMEEditor.ETL.GetCopyDataEntityScript(this, Entities, progress, token);
                    DMEEditor.ETL.Script.ScriptDetails=retscripts;
                    DMEEditor.ETL.Script.LastRunDateTime = System.DateTime.Now;
                    DMEEditor.ETL.RunCreateScript(DMEEditor.progress, token, true);
                    OnLoadData?.Invoke(this, (PassedArgs)DMEEditor.Passedarguments);
                    IsLoaded = true;
                }

            }
            catch (Exception ex)
            {
                IsLoaded = false;
                DMEEditor.AddLogMessage("Beep", $"Could not Load InMemory data for {DatasourceName}- {ex.Message}", System.DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public virtual IErrorsInfo LoadStructure(IProgress<PassedArgs> progress, CancellationToken token, bool copydata = false)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                if (!IsStructureLoaded)
                {
                    Entities.Clear();
                    EntitiesNames.Clear();
                    LoadEntities(DatasourceName);
                    if (Entities != null  && Entities.Any())
                    {
                        IsStructureLoaded = true;
                    }

                    OnLoadStructure?.Invoke(this, (PassedArgs)DMEEditor.Passedarguments);
                }
            }
            catch (Exception ex)
            {
                IsStructureLoaded = false;
                IsStructureCreated = false;
                DMEEditor.AddLogMessage("Beep", $"Failed to load in-memory structure: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public virtual IErrorsInfo SaveStructure()
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;

            try
            {
                // Step 1: Get the latest list of table/entity names
                if(Entities == null || Entities.Count == 0)
                {
                    DMEEditor.AddLogMessage("Beep", $"No entities found in the in-memory structure for {DatasourceName}.", DateTime.Now, 0, null, Errors.Failed);
                    return DMEEditor.ErrorObject;
                }
                if(ConnectionStatus!= ConnectionState.Open)
                {
                    DMEEditor.AddLogMessage("Beep", $"Connection is not established for {DatasourceName}.", DateTime.Now, 0, null, Errors.Failed);
                    return DMEEditor.ErrorObject;
                }
                var entityNamesFromDb = GetEntitesList();
                SyncEntitiesNameandEntities();
                // Step 2: Ensure Entities collection contains all the entities from the database

                SaveEntites(DatasourceName);

                // Step 7: Raise event
                OnSaveStructure?.Invoke(this, (PassedArgs)DMEEditor.Passedarguments);
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.AddLogMessage("Beep", $"Failed to save in-memory structure for {DatasourceName}: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }

            return DMEEditor.ErrorObject;
        }
        public void SyncEntitiesNameandEntities()
        {
            try
            {
                // Initialize collections if they're null
                if (Entities == null) Entities = new List<EntityStructure>();
                if (EntitiesNames == null) EntitiesNames = new List<string>();

                // Step 1: Create a copy of EntitiesNames to safely iterate
                var entityNamesToProcess = new List<string>(EntitiesNames);
                var entityNamesToRemove = new List<string>();

                // Step 2: Check entities in EntitiesNames and add missing ones to Entities
                foreach (string entityName in entityNamesToProcess)
                {
                    if (string.IsNullOrEmpty(entityName)) continue;

                    // Check if entity already exists in Entities collection
                    bool entityExists = Entities.Any(e => e.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));

                    if (!entityExists)
                    {
                        // Try to get entity structure
                        EntityStructure entityStructure = GetEntityStructure(entityName);
                        if (entityStructure != null)
                        {
                            // Add to Entities if not already there
                            Entities.Add(entityStructure);
                            DMEEditor.AddLogMessage("Success", $"Added entity {entityName} to Entities collection", DateTime.Now, 0, null, Errors.Ok);
                        }
                        else
                        {
                            // Mark for removal from EntitiesNames if structure couldn't be retrieved
                            entityNamesToRemove.Add(entityName);
                            DMEEditor.AddLogMessage("Warning", $"Could not get structure for entity {entityName}, removing from EntitiesNames", DateTime.Now, 0, null, Errors.Warning);
                        }
                    }
                }

                // Step 3: Remove invalid entities from EntitiesNames
                foreach (string entityToRemove in entityNamesToRemove)
                {
                    EntitiesNames.Remove(entityToRemove);
                }

                // Step 4: Check Entities collection and ensure all are in EntitiesNames
                // Also create missing entities in database if needed
                foreach (EntityStructure entity in Entities.ToList()) // Use ToList() to avoid collection modification issues
                {
                    if (entity == null || string.IsNullOrEmpty(entity.EntityName)) continue;

                    // Check if entity name exists in EntitiesNames
                    if (!EntitiesNames.Contains(entity.EntityName, StringComparer.OrdinalIgnoreCase))
                    {
                        // Try to create entity in database
                        bool created = CreateEntityAs(entity);

                        if (created)
                        {
                            // Add to EntitiesNames if successfully created
                            EntitiesNames.Add(entity.EntityName);
                            DMEEditor.AddLogMessage("Success", $"Created entity {entity.EntityName} in database and added to EntitiesNames", DateTime.Now, 0, null, Errors.Ok);
                        }
                        else
                        {
                            DMEEditor.AddLogMessage("Warning", $"Failed to create entity {entity.EntityName} in database", DateTime.Now, 0, null, Errors.Warning);
                        }
                    }
                }

                // Step 5: Sync with InMemoryStructures
                if (InMemoryStructures == null) InMemoryStructures = new List<EntityStructure>();
                InMemoryStructures = new List<EntityStructure>(Entities);

                // Step 6: Sync with ConnectionProp.Entities if available
                if (Dataconnection?.ConnectionProp?.Entities != null)
                {
                    Dataconnection.ConnectionProp.Entities = InMemoryStructures;
                }

                // Mark as synced
                IsSynced = true;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", $"Error synchronizing entities: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                IsSynced = false;
            }
        }


        public virtual IErrorsInfo OpenDatabaseInMemory(string databasename)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                if (string.IsNullOrWhiteSpace(databasename))
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.AddLogMessage("Beep", $"Database name cannot be null or empty.", DateTime.Now, 0, null, Errors.Failed);
                    return DMEEditor.ErrorObject;
                }
                DatasourceName = databasename;
                if (ConnectionStatus != System.Data.ConnectionState.Open)
                {
                    Openconnection();
                }
                if (ConnectionStatus == System.Data.ConnectionState.Open)
                {
                    if (InMemoryStructures == null)
                        InMemoryStructures = new List<EntityStructure>();
                    IsCreated = true;
                    DMEEditor.AddLogMessage("Beep", $"In-memory database '{databasename}' opened successfully.", DateTime.Now, 0, null, Errors.Ok);
                }
                else
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.AddLogMessage("Beep", $"Failed to open in-memory database '{databasename}'.", DateTime.Now, 0, null, Errors.Failed);
                }
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.AddLogMessage("Beep", $"Error opening in-memory database '{databasename}': {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public virtual IErrorsInfo SyncData(IProgress<PassedArgs> progress, CancellationToken token)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                if (IsCreated && Entities != null && Entities.Any())
                {
                    SyncEntitiesNameandEntities();
                    List<ETLScriptDet> retscripts = DMEEditor.ETL.GetCopyDataEntityScript(this, Entities, progress, token);
                    DMEEditor.ETL.Script.ScriptDetails = retscripts;
                    DMEEditor.ETL.Script.LastRunDateTime = DateTime.Now;
                    DMEEditor.ETL.RunCreateScript(DMEEditor.progress, token, true);
                    IsSynced = true;
                    DMEEditor.AddLogMessage("Beep", $"Synced all data for {DatasourceName}.", DateTime.Now, 0, null, Errors.Ok);
                }
            }
            catch (OperationCanceledException)
            {
                IsSynced = false;
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.AddLogMessage("Beep", $"Sync of {DatasourceName} was cancelled.", DateTime.Now, 0, null, Errors.Failed);
            }
            catch (Exception ex)
            {
                IsSynced = false;
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.AddLogMessage("Beep", $"Error syncing data for {DatasourceName}: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            RaiseOnSyncData((PassedArgs)DMEEditor.Passedarguments);
            return DMEEditor.ErrorObject;
        }
        public IErrorsInfo SyncData(string entityname, IProgress<PassedArgs> progress, CancellationToken token)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                if (string.IsNullOrWhiteSpace(entityname))
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.AddLogMessage("Beep", $"Entity name cannot be null or empty.", DateTime.Now, 0, null, Errors.Failed);
                    return DMEEditor.ErrorObject;
                }
                var entity = Entities?.FirstOrDefault(e => e.EntityName.Equals(entityname, StringComparison.OrdinalIgnoreCase));
                if (entity == null)
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.AddLogMessage("Beep", $"Entity '{entityname}' not found for sync.", DateTime.Now, 0, null, Errors.Failed);
                    return DMEEditor.ErrorObject;
                }
                if (IsCreated)
                {
                    List<ETLScriptDet> retscripts = DMEEditor.ETL.GetCopyDataEntityScript(this, new List<EntityStructure> { entity }, progress, token);
                    DMEEditor.ETL.Script.ScriptDetails = retscripts;
                    DMEEditor.ETL.Script.LastRunDateTime = DateTime.Now;
                    DMEEditor.ETL.RunCreateScript(DMEEditor.progress, token, true);
                    DMEEditor.AddLogMessage("Beep", $"Synced entity '{entityname}' for {DatasourceName}.", DateTime.Now, 0, null, Errors.Ok);
                }
            }
            catch (OperationCanceledException)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.AddLogMessage("Beep", $"Sync of entity '{entityname}' was cancelled.", DateTime.Now, 0, null, Errors.Failed);
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.AddLogMessage("Beep", $"Error syncing entity '{entityname}' for {DatasourceName}: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            RaiseOnSyncData((PassedArgs)DMEEditor.Passedarguments);
            return DMEEditor.ErrorObject;
        }

        public IErrorsInfo RefreshData(string entityname, IProgress<PassedArgs> progress, CancellationToken token)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                if (string.IsNullOrWhiteSpace(entityname))
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.AddLogMessage("Beep", $"Entity name cannot be null or empty.", DateTime.Now, 0, null, Errors.Failed);
                    return DMEEditor.ErrorObject;
                }
                if (!IsCreated)
                    return DMEEditor.ErrorObject;
                var entity = InMemoryStructures?.FirstOrDefault(e => e.EntityName.Equals(entityname, StringComparison.OrdinalIgnoreCase));
                if (entity == null)
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.AddLogMessage("Beep", $"Entity '{entityname}' not found in in-memory structures.", DateTime.Now, 0, null, Errors.Failed);
                    return DMEEditor.ErrorObject;
                }
                token.ThrowIfCancellationRequested();
                DMEEditor.AddLogMessage("Beep", $"Refreshing entity '{entityname}' — clearing data.", DateTime.Now, 0, null, Errors.Ok);
                string sql = GetDeleteAllSql(entity.EntityName);
                DMEEditor.ErrorObject = ExecuteSql(sql);
                if (DMEEditor.ErrorObject.Flag == Errors.Ok)
                {
                    DMEEditor.AddLogMessage("Beep", $"Deleted data from '{entityname}', reloading.", DateTime.Now, 0, null, Errors.Ok);
                    List<ETLScriptDet> retscripts = DMEEditor.ETL.GetCopyDataEntityScript(this, new List<EntityStructure> { entity }, progress, token);
                    DMEEditor.ETL.Script.ScriptDetails = retscripts;
                    DMEEditor.ETL.Script.LastRunDateTime = DateTime.Now;
                    DMEEditor.ETL.RunCreateScript(DMEEditor.progress, token, true);
                    DMEEditor.AddLogMessage("Beep", $"Refreshed entity '{entityname}' successfully.", DateTime.Now, 0, null, Errors.Ok);
                }
                else
                {
                    DMEEditor.AddLogMessage("Beep", $"Could not delete data from '{entityname}' during refresh.", DateTime.Now, 0, null, Errors.Failed);
                }
                RaiseOnRefreshDataEntity((PassedArgs)DMEEditor.Passedarguments);
            }
            catch (OperationCanceledException)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.AddLogMessage("Beep", $"Refresh of entity '{entityname}' was cancelled.", DateTime.Now, 0, null, Errors.Failed);
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.AddLogMessage("Beep", $"Error refreshing entity '{entityname}': {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public IErrorsInfo RefreshData(IProgress<PassedArgs> progress, CancellationToken token)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            bool isdeleted = false;
            try
            {
                if (IsCreated)
                {
                    foreach (var item in InMemoryStructures)
                    {
                        token.ThrowIfCancellationRequested();
                        DMEEditor.AddLogMessage("Beep", $"Refreshing entity '{item.EntityName}' — clearing data.", DateTime.Now, 0, null, Errors.Ok);
                        string sql = GetDeleteAllSql(item.EntityName);
                        DMEEditor.ErrorObject = ExecuteSql(sql);
                        if (DMEEditor.ErrorObject.Flag == Errors.Ok)
                        {
                            isdeleted = true;
                            DMEEditor.AddLogMessage("Beep", $"Deleted data from {item.EntityName}", DateTime.Now, 0, null, Errors.Ok);
                        }
                        else
                        {
                            DMEEditor.AddLogMessage("Beep", $"Could not delete data from {item.EntityName}", DateTime.Now, 0, null, Errors.Failed);
                        }
                    }
                    if (isdeleted)
                    {
                        LoadData(progress, token);
                    }
                    RaiseOnLoadData((PassedArgs)DMEEditor.Passedarguments);
                    IsLoaded = true;
                }
                RaiseOnRefreshData((PassedArgs)DMEEditor.Passedarguments);
            }
            catch (OperationCanceledException)
            {
                IsLoaded = false;
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.AddLogMessage("Beep", $"Refresh of {DatasourceName} was cancelled.", DateTime.Now, 0, null, Errors.Failed);
            }
            catch (Exception ex)
            {
                IsLoaded = false;
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.AddLogMessage("Beep", $"Could not refresh InMemory data for {DatasourceName}- {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        #endregion
        #region "InMemoryDataSource Events"
        public event EventHandler<PassedArgs> OnLoadData;
        public event EventHandler<PassedArgs> OnLoadStructure;
        public event EventHandler<PassedArgs> OnSaveStructure;
        public event EventHandler<PassedArgs> OnSyncData;
        public event EventHandler<PassedArgs> PassEvent;
        public event EventHandler<PassedArgs> OnCreateStructure;
        public event EventHandler<PassedArgs> OnRefreshData;
        public event EventHandler<PassedArgs> OnRefreshDataEntity;

        /// <summary>Raises the <see cref="OnLoadData"/> event. Derived classes can override to intercept.</summary>
        protected virtual void RaiseOnLoadData(PassedArgs args) => OnLoadData?.Invoke(this, args);

        /// <summary>Raises the <see cref="OnSyncData"/> event. Derived classes can override to intercept.</summary>
        protected virtual void RaiseOnSyncData(PassedArgs args) => OnSyncData?.Invoke(this, args);

        /// <summary>Raises the <see cref="OnRefreshData"/> event. Derived classes can override to intercept.</summary>
        protected virtual void RaiseOnRefreshData(PassedArgs args) => OnRefreshData?.Invoke(this, args);

        /// <summary>Raises the <see cref="OnRefreshDataEntity"/> event. Derived classes can override to intercept.</summary>
        protected virtual void RaiseOnRefreshDataEntity(PassedArgs args) => OnRefreshDataEntity?.Invoke(this, args);

        /// <summary>Raises the <see cref="OnCreateStructure"/> event. Derived classes can override to intercept.</summary>
        protected virtual void RaiseOnCreateStructure(PassedArgs args) => OnCreateStructure?.Invoke(this, args);

        /// <summary>
        /// Returns a provider-appropriate DELETE-all statement for the given table.
        /// Resolves via <see cref="GeneralDataSourceHelper"/> (uses the same dialect as the current
        /// <see cref="DatasourceType"/>), falling back to quoted-identifier SQL when the helper
        /// cannot produce a statement.
        /// </summary>
        protected virtual string GetDeleteAllSql(string tableName)
        {
            try
            {
                var helper = new GeneralDataSourceHelper(DatasourceType, DMEEditor);
                var (sql, success, _) = helper.GenerateTruncateTableSql(tableName);
                if (success && !string.IsNullOrWhiteSpace(sql))
                    return sql;
            }
            catch
            {
                // fall through to safe quoted-identifier fallback
            }
            // Fallback: provider-appropriate quoted identifier DELETE
            switch (DatasourceType)
            {
                case DataSourceType.SqlServer:
                    return $"DELETE FROM [{tableName}]";
                case DataSourceType.Mysql:
                    return $"DELETE FROM `{tableName}`";
                default:
                    // SQLite, PostgreSQL, Oracle, FireBird, DB2 and most ANSI-SQL providers.
                    return $"DELETE FROM \"{tableName}\"";
            }
        }
        #endregion
        #region "Overriden Methods"
        public override bool CreateEntityAs(EntityStructure entity)
        {
            string ds = entity.DataSourceID;
            entity.EntityName = entity.EntityName.Trim().ToUpper();
            entity.DatasourceEntityName = entity.EntityName.Trim().ToUpper();
            if (EntitiesNames.Contains(entity.EntityName))
            {
                return false;
            }
            if (Entities.Where(c => c.EntityName == entity.EntityName).Count() > 0)
            {
                return false;
            }
            if (CheckEntityExist(entity.EntityName))
            {
                return false;
            }
            bool created = base.CreateEntityAs(entity);
            entity.IsCreated = created;
            entity.DataSourceID = ds;
            if (created)
            {
               
                SyncEntitiesNameandEntities();

            }

            return created;

        }
       
        #endregion
        #region "Dispose"
        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    SaveStructure();
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~InMemoryDataSource()
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


        #endregion
        #region "Load Save Data Methods"
        public  IErrorsInfo SaveEntites( string datasourcename)
        {

            DMEEditor.ErrorObject.Flag = Errors.Ok;
            PassedArgs passedArgs = new PassedArgs { DatasourceName = datasourcename     };
            try
            {
                IDataSource DataSource = DMEEditor.GetDataSource(datasourcename);
                if (DataSource != null)
                {
                    DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new TheTechIdea.Beep.ConfigUtil.DatasourceEntities { datasourcename = datasourcename, Entities = this.Entities });
                }
            }
            catch (Exception ex)
            {
                DMEEditor.Logger.WriteLog($"Error in Saving Entities ({ex.Message}) ");
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Ex = ex;
            }
            return DMEEditor.ErrorObject;
        }
        public  IErrorsInfo LoadEntities( string datasourcename)
        {
           
          
           
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            PassedArgs passedArgs = new PassedArgs { DatasourceName = datasourcename };
            try
            {
                IDataSource DataSource = DMEEditor.GetDataSource(datasourcename);
                if (DataSource != null)
                {
                    var ents = DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues(datasourcename);
                    if (ents != null)
                    {
                        if (ents.Entities.Count > 0)
                        {
                            // remove duplicates
                            ents.Entities = ents.Entities.GroupBy(x => x.EntityName).Select(g => g.First()).ToList();
                            DataSource.Entities = ents.Entities;
                            DataSource.EntitiesNames = ents.Entities.Select(x => x.EntityName).ToList();
                        }
                    }
                    InMemoryStructures = DataSource.Entities;
                }
            }
            catch (Exception ex)
            {
                DMEEditor.Logger.WriteLog($"Error in Loading Entities ({ex.Message}) ");
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Ex = ex;
            }
            return DMEEditor.ErrorObject;
        }
        public IErrorsInfo CreateStructure(IProgress<PassedArgs> progress, CancellationToken token)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                if (!IsStructureCreated)
                {
                    
           //         GetEntitesList();
                    for (int i = 0; i <= Entities.Count-1; i++)
                    {
                    //    if (!string.IsNullOrEmpty(Entities[i].DataSourceID))
                    //    {
                            CreateEntityAs(Entities[i]);
                            Entities[i].IsCreated = true;
                  //      }
                       

                    }
                  
                }
                IsStructureCreated = true;
            }
            catch (Exception ex)
            {
                IsStructureCreated = false;
                DMEEditor.AddLogMessage("Beep", $"Could not Load InMemory Structure for {DatasourceName}- {ex.Message}", System.DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        #endregion
    }
}
