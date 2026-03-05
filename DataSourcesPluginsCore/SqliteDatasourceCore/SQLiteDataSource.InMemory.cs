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
                    IsCreated = false;
                    ConnectionStatus = ConnectionState.Closed;
                    Dataconnection.ConnectionStatus = ConnectionState.Closed;
                    SetError(Errors.Failed, "Failed to open in-memory database connection.");
                }
                else
                {
                    IsCreated = true;
                    ConnectionStatus = ConnectionState.Open;
                    Dataconnection.ConnectionStatus = ConnectionState.Open;
                    Createfolder(databasename);  // ensures Isfoldercreated = true for ETL script storage
                    SetError(Errors.Ok, "In-memory database connection opened.");
                }
            }
            catch (Exception ex)
            {
                IsCreated = false;
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
                    RaiseOnLoadData((PassedArgs)DMEEditor.Passedarguments);
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
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                if (Isfoldercreated && IsCreated && Entities != null && Entities.Any())
                {
                    SyncEntitiesNameandEntities();
                    DMEEditor.AddLogMessage("Beep", $"Syncing all data for {DatasourceName}.", DateTime.Now, 0, null, Errors.Ok);
                    List<ETLScriptDet> retscripts = DMEEditor.ETL.GetCopyDataEntityScript(this, Entities, progress, token);
                    DMEEditor.ETL.Script.ScriptDetails = retscripts;
                    DMEEditor.ETL.Script.LastRunDateTime = DateTime.Now;
                    DMEEditor.ETL.RunCreateScript(DMEEditor.progress, token, true);
                    IsSynced = true;
                    DMEEditor.AddLogMessage("Beep", $"Synced all data for {DatasourceName} successfully.", DateTime.Now, 0, null, Errors.Ok);
                }
            }
            catch (OperationCanceledException)
            {
                IsSynced = false;
                SetError(Errors.Failed, $"Sync of {DatasourceName} was cancelled.");
                DMEEditor.AddLogMessage("Beep", $"Sync of {DatasourceName} was cancelled.", DateTime.Now, 0, null, Errors.Failed);
            }
            catch (Exception ex)
            {
                IsSynced = false;
                SetError(Errors.Failed, $"Error syncing data for {DatasourceName}.", ex);
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
                    SetError(Errors.Failed, "Entity name cannot be null or empty.");
                    return DMEEditor.ErrorObject;
                }
                var entity = Entities?.FirstOrDefault(e => e.EntityName.Equals(entityname, StringComparison.OrdinalIgnoreCase));
                if (entity == null)
                {
                    SetError(Errors.Failed, $"Entity '{entityname}' not found for sync.");
                    return DMEEditor.ErrorObject;
                }
                if (Isfoldercreated && IsCreated)
                {
                    DMEEditor.AddLogMessage("Beep", $"Syncing entity '{entityname}' for {DatasourceName}.", DateTime.Now, 0, null, Errors.Ok);
                    List<ETLScriptDet> retscripts = DMEEditor.ETL.GetCopyDataEntityScript(this, new List<EntityStructure> { entity }, progress, token);
                    DMEEditor.ETL.Script.ScriptDetails = retscripts;
                    DMEEditor.ETL.Script.LastRunDateTime = DateTime.Now;
                    DMEEditor.ETL.RunCreateScript(DMEEditor.progress, token, true);
                    DMEEditor.AddLogMessage("Beep", $"Synced entity '{entityname}' for {DatasourceName} successfully.", DateTime.Now, 0, null, Errors.Ok);
                }
            }
            catch (OperationCanceledException)
            {
                SetError(Errors.Failed, $"Sync of entity '{entityname}' was cancelled.");
                DMEEditor.AddLogMessage("Beep", $"Sync of entity '{entityname}' was cancelled.", DateTime.Now, 0, null, Errors.Failed);
            }
            catch (Exception ex)
            {
                SetError(Errors.Failed, $"Error syncing entity '{entityname}'.", ex);
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
                    SetError(Errors.Failed, "Entity name cannot be null or empty.");
                    return DMEEditor.ErrorObject;
                }
                if (!IsCreated)
                    return DMEEditor.ErrorObject;
                var entity = InMemoryStructures?.FirstOrDefault(e => e.EntityName.Equals(entityname, StringComparison.OrdinalIgnoreCase));
                if (entity == null)
                {
                    SetError(Errors.Failed, $"Entity '{entityname}' not found in in-memory structures.");
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
                SetError(Errors.Failed, $"Refresh of entity '{entityname}' was cancelled.");
                DMEEditor.AddLogMessage("Beep", $"Refresh of entity '{entityname}' was cancelled.", DateTime.Now, 0, null, Errors.Failed);
            }
            catch (Exception ex)
            {
                SetError(Errors.Failed, $"Error refreshing entity '{entityname}'.", ex);
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
                if (Isfoldercreated && IsCreated)
                {
                    DMEEditor.AddLogMessage("Beep", $"Beginning full refresh for {DatasourceName} ({InMemoryStructures?.Count ?? 0} entities).", DateTime.Now, 0, null, Errors.Ok);
                    BeginTransaction(null);
                    try
                    {
                        foreach (var item in InMemoryStructures)
                        {
                            token.ThrowIfCancellationRequested();
                            DMEEditor.AddLogMessage("Beep", $"Clearing entity '{item.EntityName}'.", DateTime.Now, 0, null, Errors.Ok);
                            // Resolve dialect-appropriate DELETE via helper (falls back to quoted identifier).
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
                            progress?.Report(new PassedArgs { Messege = $"Cleared {item.EntityName}", EventType = "Refresh" });
                        }
                        if (isdeleted)
                        {
                            LoadData(progress, token);
                        }
                        Commit(null);
                    }
                    catch
                    {
                        EndTransaction(null);
                        throw;
                    }
                    RaiseOnLoadData((PassedArgs)DMEEditor.Passedarguments);
                    IsLoaded = true;
                    DMEEditor.AddLogMessage("Beep", $"Full refresh completed for {DatasourceName}.", DateTime.Now, 0, null, Errors.Ok);
                }
                RaiseOnRefreshData((PassedArgs)DMEEditor.Passedarguments);
            }
            catch (OperationCanceledException)
            {
                IsLoaded = false;
                SetError(Errors.Failed, $"Refresh of {DatasourceName} was cancelled.");
                DMEEditor.AddLogMessage("Beep", $"Refresh of {DatasourceName} was cancelled.", DateTime.Now, 0, null, Errors.Failed);
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
