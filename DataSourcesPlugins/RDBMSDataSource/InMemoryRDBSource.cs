using DataManagementModels.DataBase;
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
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;

using TheTechIdea.Logger;
using TheTechIdea.Util;


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
        public ETLScriptHDR CreateScript { get; set; } = new ETLScriptHDR();
        public List<EntityStructure> InMemoryStructures { get; set; } = new List<EntityStructure>();
        public string ColumnDelimiter { get; set; }
        public string ParameterDelimiter { get; set; }
        public string GuidID { get; set; }
        public DataSourceType DatasourceType { get; set; } = DataSourceType.NONE;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.INMEMORY;
        public IDataConnection Dataconnection { get; set; }
        public string DatasourceName { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public string Id { get; set; }
        public IDMLogger Logger { get; set; }
        public List<string> EntitiesNames { get; set; } = new List<string>();
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
        public IDMEEditor DMEEditor { get; set; }
        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Closed;
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
                    DMEEditor.ETL.Script = CreateScript;
                    DMEEditor.ETL.Script.LastRunDateTime = System.DateTime.Now;
                    DMEEditor.ETL.RunImportScript(DMEEditor.progress, token);
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
                if (Isfoldercreated)
                {
                    ConnectionStatus = ConnectionState.Open;
                    InMemoryStructures = new List<EntityStructure>();
                    Entities = new List<EntityStructure>();
                    EntitiesNames = new List<string>();

                    if (File.Exists(InMemoryStructuresfilepath))
                    {
                        InMemoryStructures = DMEEditor.ConfigEditor.JsonLoader.DeserializeObject<EntityStructure>(InMemoryStructuresfilepath);
                    }
                    if (File.Exists(Filepath))
                    {
                        CreateScript = DMEEditor.ConfigEditor.JsonLoader.DeserializeSingleObject<ETLScriptHDR>(Filepath);
                        DMEEditor.ETL.Script = CreateScript;
                        DMEEditor.ETL.Script.LastRunDateTime = System.DateTime.Now;
                        DMEEditor.ErrorObject=DMEEditor.ETL.RunCreateScript(progress, token, copydata).Result;

                    }
                    OnLoadStructure?.Invoke(this, (PassedArgs)DMEEditor.Passedarguments);
                }
            }
            catch (Exception ex)
            {
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
               

                    CreateScript = new ETLScriptHDR();
                    CreateScript.ScriptDTL.AddRange(DMEEditor.ETL.GetCreateEntityScript(this, Entities, progress, token.Token, true));
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
        #endregion
        #region "InMemoryDataSource Events"
        public event EventHandler<PassedArgs> OnLoadData;
        public event EventHandler<PassedArgs> OnLoadStructure;
        public event EventHandler<PassedArgs> OnSaveStructure;
        public event EventHandler<PassedArgs> OnSyncData;
        public event EventHandler<PassedArgs> PassEvent;
        #endregion
        #region "Overriden Methods"
        public override bool CreateEntityAs(EntityStructure entity)
        {
            string ds = entity.DataSourceID;
            bool retval = base.CreateEntityAs(entity);
            entity.DataSourceID = ds;

            InMemoryStructures.Add(base.GetEntityStructure(entity.EntityName));
            return retval;
        }
        public override List<string> GetEntitesList()
        {
            if (Entities.Count == 0)
            {
                CancellationTokenSource token = new CancellationTokenSource();
                Progress<PassedArgs> progress = new Progress<PassedArgs>();
                LoadStructure(progress, token.Token, false);
                if(DMEEditor.ErrorObject.Flag==Errors.Failed)
                {
                    DMEEditor.AddLogMessage("Beep", $"Could not Load InMemory Structure for {DatasourceName}", System.DateTime.Now, 0, null, Errors.Failed);
                }
            }
            EntitiesNames=new List<string>();
            foreach (EntityStructure ent in Entities)
            {
                EntitiesNames.Add(ent.EntityName);
            }
            return EntitiesNames;
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


                    Task.Run(() =>
                    {
                        DMEEditor.ETL.RunCreateScript(progress, token, true, true);
                    }).Wait();
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
