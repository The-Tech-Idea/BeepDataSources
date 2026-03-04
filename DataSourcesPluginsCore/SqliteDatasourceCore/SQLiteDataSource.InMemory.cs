using System.Data;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.DataBase
{
    public partial class SQLiteDataSource
    {
        public static string BeepDataPath { get; private set; }
        public static string InMemoryPath { get; private set; }
        public static string Filepath { get; private set; }
        public static string InMemoryStructuresfilepath { get; private set; }
        public static bool Isfoldercreated { get; private set; } = false;

        public IErrorsInfo OpenDatabaseInMemory(string databasename)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                base.Dataconnection.InMemory = true;
                base.Dataconnection.ConnectionProp.IsInMemory = true;
                base.Dataconnection.ConnectionProp.IsFile = false;
                base.Dataconnection.ConnectionProp.FileName = string.Empty;
                base.Dataconnection.ConnectionProp.ConnectionString = @"Data Source=:memory:;Version=3;New=True;";
                base.Dataconnection.DataSourceDriver.ConnectionString = @"Data Source=:memory:;Version=3;New=True;";
                base.Dataconnection.ConnectionProp.Database = databasename;
                base.Dataconnection.ConnectionProp.ConnectionName = databasename;
                base.Dataconnection.OpenConnection();

                IsStructureLoaded = false;
                IsStructureCreated = false;
                if (Dataconnection.ConnectionStatus != ConnectionState.Open)
                {
                    ConnectionStatus = ConnectionState.Closed;
                    Dataconnection.ConnectionStatus = ConnectionState.Closed;
                    SetError(Errors.Failed, "Failed to open in-memory database connection.");
                }
                else
                {
                    ConnectionStatus = ConnectionState.Open;
                    Dataconnection.ConnectionStatus = ConnectionState.Open;
                    SetError(Errors.Ok, "In-memory database connection opened.");
                }
            }
            catch (Exception ex)
            {
                SetError(Errors.Failed, "Error opening in-memory database.", ex);
                DMEEditor.AddLogMessage("Beep", $"Error opening in-memory database: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        public string GetConnectionString()
        {
            return base.Dataconnection.ConnectionProp.ConnectionString;
        }

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
                    DMEEditor.AddLogMessage("Beep", $"Could not create InMemory Structure folders for {datasourcename}- {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                }
            }
        }

        public virtual IErrorsInfo LoadData(IProgress<PassedArgs> progress, CancellationToken token)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                if (Isfoldercreated && IsCreated)
                {
                    List<ETLScriptDet> retscripts = DMEEditor.ETL.GetCopyDataEntityScript(this, Entities, progress, token);
                    DMEEditor.ETL.Script.ScriptDetails = retscripts;
                    DMEEditor.ETL.Script.LastRunDateTime = DateTime.Now;
                    DMEEditor.ETL.RunCreateScript(DMEEditor.progress, token, true);
                    OnLoadData?.Invoke(this, (PassedArgs)DMEEditor.Passedarguments);
                    IsLoaded = true;
                }
            }
            catch (Exception ex)
            {
                IsLoaded = false;
                SetError(Errors.Failed, $"Could not load in-memory data for {DatasourceName}.", ex);
                DMEEditor.AddLogMessage("Beep", $"Could not load in-memory data for {DatasourceName}- {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
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
                if (Isfoldercreated && IsCreated)
                {
                    foreach (var item in InMemoryStructures)
                    {
                        string sql = $"delete from {item.EntityName}";
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
                    OnLoadData?.Invoke(this, (PassedArgs)DMEEditor.Passedarguments);
                    IsLoaded = true;
                }
                OnRefreshData?.Invoke(this, (PassedArgs)DMEEditor.Passedarguments);
            }
            catch (Exception ex)
            {
                IsLoaded = false;
                SetError(Errors.Failed, $"Could not refresh in-memory data for {DatasourceName}.", ex);
                DMEEditor.AddLogMessage("Beep", $"Could not refresh in-memory data for {DatasourceName}- {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
    }
}
