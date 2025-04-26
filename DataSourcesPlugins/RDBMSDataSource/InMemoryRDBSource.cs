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
                    DMEEditor.ETL.Script.ScriptDTL = retscripts;
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
                        IsLoaded = true;
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
            return DMEEditor.ErrorObject;
        }
        public virtual IErrorsInfo SyncData(IProgress<PassedArgs> progress, CancellationToken token)
        {
            OnSyncData?.Invoke(this, (PassedArgs)DMEEditor.Passedarguments);
            return DMEEditor.ErrorObject;
        }
        public IErrorsInfo SyncData(string entityname, IProgress<PassedArgs> progress, CancellationToken token)
        {
            OnSyncData?.Invoke(this, (PassedArgs)DMEEditor.Passedarguments);
            return DMEEditor.ErrorObject;
        }

        public IErrorsInfo RefreshData(string entityname, IProgress<PassedArgs> progress, CancellationToken token)
        {
            OnRefreshData?.Invoke(this, (PassedArgs)DMEEditor.Passedarguments);
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
                    // delete all data in the  sqllite InMemory database
                    foreach (var item in InMemoryStructures)
                    {
                        // run sql to delete data from entity 
                        string sql = $"delete from {item.EntityName}";
                        DMEEditor.ErrorObject = ExecuteSql(sql);
                        if (DMEEditor.ErrorObject.Flag == Errors.Ok)
                        {
                            isdeleted = true;
                            DMEEditor.AddLogMessage("Beep", $"Deleted data from {item.EntityName}", System.DateTime.Now, 0, null, Errors.Ok);
                        }
                        else
                        {
                            DMEEditor.AddLogMessage("Beep", $"Could not delete data from {item.EntityName}", System.DateTime.Now, 0, null, Errors.Failed);
                        }
                    }
                    if (isdeleted)
                    {
                        LoadData(progress, token);
                    }
                    OnLoadData?.Invoke(this, (PassedArgs)DMEEditor.Passedarguments);
                    IsLoaded = true;
                }
                OnRefreshData?.Invoke(this, (PassedArgs)DMEEditor.Passedarguments);
            }
            catch (Exception ex)
            {
                IsLoaded = false;
                DMEEditor.AddLogMessage("Beep", $"Could not refresh InMemory data for {DatasourceName}- {ex.Message}", System.DateTime.Now, 0, null, Errors.Failed);
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
        #endregion
        #region "Overriden Methods"
        public override bool CreateEntityAs(EntityStructure entity)
        {
            string ds = entity.DataSourceID;
            if (EntitiesNames.Contains(entity.EntityName))
            {
                return false;
            }
            if (Entities.Where(c => c.EntityName == entity.EntityName).Count() > 0)
            {
                return false;
            }
            bool created = base.CreateEntityAs(entity);
            entity.DataSourceID = ds;
            if (created)
            {
                Entities.Add(entity);
                EntitiesNames.Add(entity.EntityName);
                InMemoryStructures.Add(entity);

                if (Dataconnection?.ConnectionProp?.Entities != null)
                {
                    Dataconnection.ConnectionProp.Entities = InMemoryStructures;
                }

                IsLoaded = IsCreated = IsStructureCreated = true;
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
                    
                    GetEntitesList();
                    for (int i = 0; i < Entities.Count-1; i++)
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
