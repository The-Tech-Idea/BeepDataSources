using TheTechIdea.Beep.DataBase;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor;

using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities    ;
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
            Createfolder(DatasourceName);


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
   
        public static string BeepDataPath { get; private set; }
        public static string InMemoryPath { get; private set; }
        public static string Filepath { get; private set; }
        public static string InMemoryStructuresfilepath { get; private set; }
        public static bool Isfoldercreated { get; private set; } = false;
        #endregion
        #region "InMemoryDataSource Methods"
        public virtual string GetConnectionString()
        {
            return Dataconnection.ConnectionProp.ConnectionString;
        }
        public virtual IErrorsInfo LoadData(Progress<PassedArgs> progress, CancellationToken token)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                if (Isfoldercreated && IsCreated)
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
        public virtual IErrorsInfo LoadStructure(Progress<PassedArgs> progress, CancellationToken token, bool copydata = false)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {

                if (IsLoaded == false && IsCreated == false && IsStructureCreated == false && Entities.Count == 0)
                {
                    if (Isfoldercreated)
                    {
                        ConnectionStatus = ConnectionState.Open;
                        InMemoryStructures = new List<EntityStructure>();
                        Entities = new List<EntityStructure>();
                        EntitiesNames = new List<string>();
                        if (File.Exists(InMemoryStructuresfilepath))
                        {
                            InMemoryStructures = DMEEditor.ConfigEditor.JsonLoader.DeserializeSingleObject<List<EntityStructure>>(InMemoryStructuresfilepath);
                            CreateScript = new ETLScriptHDR();
                            CreateScript.ScriptDTL.AddRange(DMEEditor.ETL.GetCreateEntityScript(this, InMemoryStructures, progress, token, true));
                            foreach (var item in CreateScript.ScriptDTL)
                            {
                                item.CopyDataScripts.AddRange(DMEEditor.ETL.GetCopyDataEntityScript(this, new List<EntityStructure>() { item.SourceEntity }, progress, token));
                            }
                        }
                        if (!IsStructureLoaded)
                        {
                            if (File.Exists(Filepath))
                            {
                                CreateScript = DMEEditor.ConfigEditor.JsonLoader.DeserializeSingleObject<ETLScriptHDR>(Filepath);
                                if (CreateScript == null)
                                {
                                    CreateScript = new ETLScriptHDR();
                                }
                                else
                                {
                                    IsStructureCreated = false;
                                }
                            }
                        }
                        //    SaveStructure();
                        OnLoadStructure?.Invoke(this, (PassedArgs)DMEEditor.Passedarguments);
                    }
                }

            }
            catch (Exception ex)
            {
                IsStructureCreated = false;
                DMEEditor.AddLogMessage("Beep", $"Could not Load InMemory Structure for {DatasourceName}- {ex.Message}", System.DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public virtual IErrorsInfo SaveStructure()
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                CancellationTokenSource token = new CancellationTokenSource();
                var progress = new Progress<PassedArgs>(percent => { });

                // remove every entity in InMemoryStructures that does not exist in Entities
                InMemoryStructures = InMemoryStructures.Where(p => Entities.Any(p2 => p2.EntityName == p.EntityName)).ToList();

                CreateScript = new ETLScriptHDR();
                CreateScript.ScriptDTL.AddRange(DMEEditor.ETL.GetCreateEntityScript(this, InMemoryStructures, progress, token.Token, true));
                foreach (var item in CreateScript.ScriptDTL)
                {
                    item.CopyDataScripts.AddRange(DMEEditor.ETL.GetCopyDataEntityScript(this, new List<EntityStructure>() { item.SourceEntity }, progress, token.Token));
                }
                DMEEditor.ConfigEditor.JsonLoader.Serialize(Filepath, CreateScript);
                DMEEditor.ConfigEditor.JsonLoader.Serialize(InMemoryStructuresfilepath, InMemoryStructures);
                OnSaveStructure?.Invoke(this, (PassedArgs)DMEEditor.Passedarguments);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Could not save InMemory Structure for {DatasourceName}- {ex.Message}", System.DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public virtual IErrorsInfo OpenDatabaseInMemory(string databasename)
        {
            return DMEEditor.ErrorObject;
        }
        public virtual IErrorsInfo SyncData(Progress<PassedArgs> progress, CancellationToken token)
        {
            OnSyncData?.Invoke(this, (PassedArgs)DMEEditor.Passedarguments);
            return DMEEditor.ErrorObject;
        }
        public IErrorsInfo SyncData(string entityname, Progress<PassedArgs> progress, CancellationToken token)
        {
            OnSyncData?.Invoke(this, (PassedArgs)DMEEditor.Passedarguments);
            return DMEEditor.ErrorObject;
        }

        public IErrorsInfo RefreshData(string entityname, Progress<PassedArgs> progress, CancellationToken token)
        {
            OnRefreshData?.Invoke(this, (PassedArgs)DMEEditor.Passedarguments);
            return DMEEditor.ErrorObject;
        }
        public IErrorsInfo RefreshData(Progress<PassedArgs> progress, CancellationToken token)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            bool isdeleted = false;
            try
            {
                if (Isfoldercreated && IsCreated)
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
            bool retval = base.CreateEntityAs(entity);
            entity.DataSourceID = ds;
            if (retval)
            {
                if (EntitiesNames.Contains(entity.EntityName) == false)
                {

                    EntitiesNames.Add(entity.EntityName);
                }
                if (Entities.Where(c => c.EntityName == entity.EntityName).Count() == 0)
                {
                    Entities.Add(entity);
                }
            }

           
                IsLoaded = true;
                IsCreated = true;
                IsStructureCreated = true;
                InMemoryStructures.Add(entity);
          

            return retval;
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
        private void Createfolder(string datasourcename)
        {
            if (!string.IsNullOrEmpty(datasourcename))
            {
                try
                {
                    if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TheTechIdea", "Beep")))
                    {
                        Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TheTechIdea", "Beep"));

                    }
                    BeepDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TheTechIdea", "Beep");
                    if (!Directory.Exists(Path.Combine(BeepDataPath, "InMemory")))
                    {
                        Directory.CreateDirectory(Path.Combine(BeepDataPath, "InMemory"));

                    }
                    InMemoryPath = Path.Combine(BeepDataPath, "InMemory");
                    if (!Directory.Exists(Path.Combine(InMemoryPath, datasourcename)))
                    {
                        Directory.CreateDirectory(Path.Combine(InMemoryPath, datasourcename));

                    }
                    Filepath = Path.Combine(InMemoryPath, datasourcename, "createscripts.json");
                    InMemoryStructuresfilepath = Path.Combine(InMemoryPath, datasourcename, "InMemoryStructures.json");
                    Isfoldercreated = true;
                }
                catch (Exception ex)
                {
                    Isfoldercreated = false;
                    DMEEditor.ErrorObject.Ex = ex;
                    DMEEditor.ErrorObject.Flag = Errors.Failed;

                    DMEEditor.AddLogMessage("Beep", $"Could not create InMemory Structure folders for {datasourcename}- {ex.Message}", System.DateTime.Now, 0, null, Errors.Failed);
                }

            }



        }
        public IErrorsInfo CreateStructure(Progress<PassedArgs> progress, CancellationToken token)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                if (!IsStructureCreated)
                {
                    DMEEditor.ETL.Script = CreateScript;
                    DMEEditor.ETL.Script.LastRunDateTime = System.DateTime.Now;
                    // Running the async method synchronously
                    var task = Task.Run(() => DMEEditor.ETL.RunCreateScript(progress, token, true, true));
                    task.Wait(token);  // Pass the cancellation token to Wait.
                    IsStructureCreated = true;
                    IsStructureCreated = true;

                }
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
