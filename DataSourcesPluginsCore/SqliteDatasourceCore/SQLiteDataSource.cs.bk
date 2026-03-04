
using Microsoft.CodeAnalysis;

using System.Data;
using System.Data.SQLite;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;



namespace TheTechIdea.Beep.DataBase
{
    [AddinAttribute(Category = DatasourceCategory.RDBMS, DatasourceType = DataSourceType.SqlLite)]
    public class SQLiteDataSource : InMemoryRDBSource,  ILocalDB,  IDataSource,IDisposable

    {
        private bool disposedValue;
        private string dateformat = "yyyy-MM-dd HH:mm:ss";
        public bool CanCreateLocal { get; set; }
         SQLiteConnection sQLiteConnection;
        string dbpath;
        public bool IsCreated { get; set; } = false;
        public bool IsLoaded { get; set; } = false;
        public bool IsSaved { get; set; } = false;
        public bool IsSynced { get; set; } = false;
        public bool IsStructureLoaded { get; set; } = false;
        public bool IsStructureCreated { get; set; } = false;
        public string Extension { get; set; } = ".s3db";
        public ETLScriptHDR CreateScript { get; set; } = new ETLScriptHDR();
        public SQLiteDataSource(string pdatasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per) : base(pdatasourcename, logger, pDMEEditor, databasetype, per)
        {
            DMEEditor = pDMEEditor;
            DatasourceName = pdatasourcename;
           
            if (pdatasourcename != null)
            {
                if (Dataconnection == null)
                {
                    Dataconnection = new RDBDataConnection(DMEEditor);
                }
                Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.FirstOrDefault(p => p.ConnectionName!=null && p.ConnectionName.Equals(pdatasourcename, StringComparison.InvariantCultureIgnoreCase)); ;
                if (Dataconnection.ConnectionProp == null)
                {
                    Dataconnection.ConnectionProp = new ConnectionProperties();
                  
                }
                else
                {

                    Dataconnection.DataSourceDriver= ConnectionHelper.LinkConnection2Drivers(Dataconnection.ConnectionProp, DMEEditor.ConfigEditor);
                
                }
                
                
            }
            Dataconnection.ConnectionProp.DatabaseType = DataSourceType.SqlLite;
            ColumnDelimiter = "[]";
            ParameterDelimiter = "$";
           
            
            //dbpath = Path.Combine(DMEEditor.ConfigEditor.ExePath, "Scripts", DatasourceName);
        }
        public List<EntityStructure> InMemoryStructures { get; set; } = new List<EntityStructure>();
        public override string ColumnDelimiter { get; set; } = "[]";
        public override string ParameterDelimiter { get; set; } = "$";

        public override ConnectionState Openconnection()
        {


            CancellationTokenSource token = new CancellationTokenSource();
            var progress = new Progress<PassedArgs>(percent => { });
            InMemory = Dataconnection.ConnectionProp.IsInMemory;
            Dataconnection.InMemory = Dataconnection.ConnectionProp.IsInMemory;
            if (Dataconnection.ConnectionStatus == ConnectionState.Open) return ConnectionState.Open;
            if (Dataconnection.ConnectionProp.IsInMemory)
            {
                OpenDatabaseInMemory(Dataconnection.ConnectionProp.Database);
                 
                if (DMEEditor.ErrorObject.Flag == Errors.Ok)
                {
                    LoadStructure(progress, token.Token, false);
                    CreateStructure(progress, token.Token);
                    return ConnectionState.Open;
                }
            }
            else
            {
                base.Openconnection();

            }
            
            if (ConnectionStatus == ConnectionState.Open)
            {
                DMEEditor.AddLogMessage("Beep", $"Connection is already open", DateTime.Now, -1, "", Errors.Ok);
                return ConnectionState.Open;
            }
            return ConnectionStatus;
        }
        public override ConnectionState Closeconnection()
        {
            try
            {
               
                if(base.RDBMSConnection.DbConn.State != ConnectionState.Open)
                {
                    return ConnectionState.Closed;
                }
                SQLiteConnection.ClearAllPools();
                if (base.RDBMSConnection.DbConn != null)
                {
                    if (Dataconnection.ConnectionProp.IsInMemory)
                    {
                        SaveStructure();
                    }
                
                    base.RDBMSConnection.DbConn.Close();
                }

                DMEEditor.AddLogMessage("Success", $"Closing connection to Sqlite Database", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string errmsg = "Error Closing connection to Sqlite Database";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            //  return RDBMSConnection.DbConn.State;
            return base.ConnectionStatus;
        }
        public bool InMemory { get; set; } = false;
        #region LocalDB
        public bool CopyDB(string DestDbName, string DesPath)
        {
            try
            {
                if (!System.IO.File.Exists(Path.Combine(base.Dataconnection.ConnectionProp.FilePath, base.Dataconnection.ConnectionProp.FileName)))
                {
                    File.Copy(base.Dataconnection.ConnectionProp.ConnectionString, Path.Combine(DesPath, DestDbName));
                }
                DMEEditor.AddLogMessage("Success", "Copy Sqlite Database", DateTime.Now, 0, null, Errors.Ok);
                return true;
            }
            catch (Exception ex)
            {
                string mes = "Could not Copy Sqlite Database";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
                return false;
            }
            ;
        }
        public bool CreateDB()
        {
            try
            {
                if (!Path.HasExtension(base.Dataconnection.ConnectionProp.FileName))
                {
                    base.Dataconnection.ConnectionProp.FileName = base.Dataconnection.ConnectionProp.FileName + Extension;
                }
                if (!System.IO.File.Exists(Path.Combine(base.Dataconnection.ConnectionProp.FilePath, base.Dataconnection.ConnectionProp.FileName)))
                {
                    SQLiteConnection.CreateFile(Path.Combine(base.Dataconnection.ConnectionProp.FilePath, base.Dataconnection.ConnectionProp.FileName));
                    //sQLiteConnection = new SQLiteAsyncConnection(Path.Combine(base.Dataconnection.ConnectionProp.FilePath, base.Dataconnection.ConnectionProp.FileName), SQLiteOpenFlags.Create);
                    enablefk();
                    DMEEditor.AddLogMessage("Success", "Create Sqlite Database", DateTime.Now, 0, null, Errors.Ok);
                }
                else
                {
                    DMEEditor.AddLogMessage("Success", "Sqlite Database already exist", DateTime.Now, 0, null, Errors.Ok);
                }
                return true;
            }
            catch (Exception ex)
            {
                string mes = "Could not Create Sqlite Database";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
                return false;
            }
            ;
        }
        public bool CreateDB(bool inMemory)
        {
            return false;
        }
        public bool CreateDBDefaultDir(string filename)
        {
            try
            {
                string dirpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TheTechIdea", "Beep", "DatabaseFiles");
                if (!Directory.Exists(dirpath))
                {
                    Directory.CreateDirectory(dirpath);
                }
                string filepathandname = Path.Combine(dirpath, filename);

                return CreateDB(filepathandname);
            }
            catch (Exception ex)
            {
                string mes = "Could not Create Sqlite Database";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
                return false;
            }
            ;
        }
        public bool CreateDB(string filepathandname)
        {
            try
            {
                if (File.Exists(filepathandname))
                {
                    DMEEditor.AddLogMessage("Success", "Sqlite Database already exist", DateTime.Now, 0, null, Errors.Ok);
                }
                else
                {
                    SQLiteConnection.CreateFile(filepathandname);
                  //  enablefk();
                    DMEEditor.AddLogMessage("Success", "Create Sqlite Database", DateTime.Now, 0, null, Errors.Ok);
                }
                base.Dataconnection.ConnectionProp.ConnectionString = $"Data Source={filepathandname};Version=3;New=True;";
                base.Dataconnection.ConnectionProp.FilePath = Path.GetDirectoryName(filepathandname);
                base.Dataconnection.ConnectionProp.FileName = Path.GetFileName(filepathandname);
                if (!System.IO.File.Exists(filepathandname))
                {
                    SQLiteConnection.CreateFile(filepathandname);
                  //  enablefk();
                    DMEEditor.AddLogMessage("Success", "Create Sqlite Database", DateTime.Now, 0, null, Errors.Ok);
                }
                else
                {
                    DMEEditor.AddLogMessage("Success", "Sqlite Database already exist", DateTime.Now, 0, null, Errors.Ok);
                }
                return true;
            }
            catch (Exception ex)
            {
                string mes = "Could not Create Sqlite Database";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
                return false;
            }
            ;
        }
        public bool DeleteDB()
        {
            try
            {
                Closeconnection();
                GC.Collect();
                GC.WaitForPendingFinalizers();

                if (System.IO.File.Exists(Path.Combine(base.Dataconnection.ConnectionProp.FilePath, base.Dataconnection.ConnectionProp.FileName)))
                {
                    File.Delete(Path.Combine(base.Dataconnection.ConnectionProp.FilePath, base.Dataconnection.ConnectionProp.FileName));
                }
                DMEEditor.AddLogMessage("Success", "Deleted Sqlite Database", DateTime.Now, 0, null, Errors.Ok);
                return true;

            }
            catch (Exception ex)
            {
                string mes = "Could not Delete Sqlite Database";
                DMEEditor.AddLogMessage("Fail", ex.Message + mes, DateTime.Now, -1, mes, Errors.Failed);
                return false;
            }
            ;
        }
        #endregion

        public IErrorsInfo DropEntity(string EntityName)
        {
            try

            {

                String cmdText = $"drop table  '{EntityName}'";
                DMEEditor.ErrorObject = base.ExecuteSql(cmdText);

                if (!base.CheckEntityExist(EntityName))
                {
                    DMEEditor.AddLogMessage("Success", $"Droping Entity {EntityName}", DateTime.Now, 0, null, Errors.Ok);
                }
                else
                {

                    DMEEditor.AddLogMessage("Error", $"Droping Entity {EntityName}", DateTime.Now, 0, null, Errors.Failed);
                }

            }
            catch (Exception ex)
            {
                string errmsg = $"Error Droping Entity {EntityName}";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;

        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Closeconnection();
                    // Dispose managed resources
                }

                // Free unmanaged resources and override finalizer
                disposedValue = true;
            }
        }
        public override string DisableFKConstraints(EntityStructure t1)
        {
            try
            {
                this.ExecuteSql("PRAGMA ignore_check_constraints = 0");
                DMEEditor.ErrorObject.Message = "successfull Disabled Sqlite FK Constraints";
                DMEEditor.ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", "Diabling Sqlite FK Constraints" + ex.Message, DateTime.Now, 0, t1.EntityName, Errors.Failed);
            }
            return DMEEditor.ErrorObject.Message;
        }
        public override string EnableFKConstraints(EntityStructure t1)
        {
            try
            {
                this.ExecuteSql("PRAGMA ignore_check_constraints = 1");
                DMEEditor.ErrorObject.Message = "successfull Enabled Sqlite FK Constraints";
                DMEEditor.ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", "Enabing Sqlite FK Constraints" + ex.Message, DateTime.Now, 0, t1.EntityName, Errors.Failed);
            }
            return DMEEditor.ErrorObject.Message;
        }
        private List<FkListforSQLlite> GetSqlLiteTableKeysAsync(string tablename)
        {
            var tb = base.GetData<FkListforSQLlite>("PRAGMA foreign_key_check(" + tablename + ");");
            return tb;
        }
        private void enablefk()
        {
            // PRAGMA foreign_keys = ON;
            var ret = base.ExecuteSql("PRAGMA foreign_keys = ON;");
        }
        public virtual IErrorsInfo BeginTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Beep", $"Error in Begin Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public virtual IErrorsInfo EndTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Beep", $"Error in end Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public virtual IErrorsInfo Commit(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Beep", $"Error in Begin Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public IErrorsInfo OpenDatabaseInMemory(string databasename)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
               // Createfolder(databasename);
                base.Dataconnection.InMemory = true;
                base.Dataconnection.ConnectionProp.FileName = string.Empty;
                base.Dataconnection.ConnectionProp.ConnectionString = $@"Data Source=:memory:;Version=3;New=True;";
                base.Dataconnection.DataSourceDriver.ConnectionString= $@"Data Source=:memory:;Version=3;New=True;";
                base.Dataconnection.ConnectionProp.Database = databasename;
                base.Dataconnection.ConnectionProp.ConnectionName = databasename;
                //base.Dataconnection.ConnectionStatus = ConnectionState.Open;
                base.Dataconnection.OpenConnection();
                IsStructureLoaded = false;
                IsStructureCreated = false;
                if (Dataconnection.ConnectionStatus != ConnectionState.Open)

                {
                    ConnectionStatus = ConnectionState.Closed;
                    Dataconnection.ConnectionStatus = ConnectionState.Closed;
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.ErrorObject.Message = "Failed to open in-memory database connection";
                }
                else
                {

                    ConnectionStatus = ConnectionState.Open;
                    Dataconnection.ConnectionStatus = ConnectionState.Open;
                }
            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Beep", $"Error in Begin Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public string GetConnectionString()
        {
            return base.Dataconnection.ConnectionProp.ConnectionString;
        }
        public override IEnumerable<string> GetEntitesList()
        {

            base.GetEntitesList();
            if (Dataconnection.InMemory)
            {
                // add inmemory entities 
                foreach (var item in InMemoryStructures)
                {
                    if (!EntitiesNames.Contains(item.EntityName))
                    {
                        EntitiesNames.Add(item.EntityName);
                        Entities.Add(item);
                    }
                }
            }
            return EntitiesNames;

        }
        public override bool CreateEntityAs(EntityStructure entity)
        {
            string ds = entity.DataSourceID;
            if (!Dataconnection.InMemory)
            {
                if (EntitiesNames.Contains(entity.EntityName))
                {
                    return false;
                }
                if (Entities.Where(c => c.EntityName == entity.EntityName).Count() > 0)
                {
                    return false;
                }
            }
          
            bool retval = base.CreateEntityAs(entity);
            entity.DataSourceID = ds;
            if (retval)
            {
                entity.IsCreated=true;

            
                IsLoaded = IsCreated = IsStructureCreated = true;
            }

           
            return retval;
        }
        private EntityStructure GetEntity(EntityStructure entity)
        {
            EntityStructure ent = new EntityStructure();
            ent.DatasourceEntityName = entity.DatasourceEntityName;
            ent.DataSourceID = entity.DataSourceID; ;
            ent.DatabaseType = entity.DatabaseType;
            ent.Caption = entity.Caption;
            ent.Category = entity.Category;
            ent.Fields = entity.Fields;
            ent.PrimaryKeys = entity.PrimaryKeys;
            ent.Relations = entity.Relations;
            ent.OriginalEntityName = entity.OriginalEntityName;
            ent.GuidID = Guid.NewGuid().ToString();
            ent.ViewID = entity.ViewID;
            ent.Viewtype = entity.Viewtype;
            ent.EntityName = entity.EntityName;
            ent.OriginalEntityName = entity.OriginalEntityName;
            ent.SchemaOrOwnerOrDatabase = entity.SchemaOrOwnerOrDatabase;
            return ent;
        }
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

        #region "InMemoryDataSource Methods"
        public static string BeepDataPath { get; private set; }
        public static string InMemoryPath { get; private set; }
        public static string Filepath { get; private set; }
        public static string InMemoryStructuresfilepath { get; private set; }
        public static bool Isfoldercreated { get; private set; } = false;
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

        #endregion
      
        public virtual IErrorsInfo LoadData(IProgress<PassedArgs> progress, CancellationToken token)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                if (Isfoldercreated && IsCreated)
                {
                    List<ETLScriptDet> retscripts = DMEEditor.ETL.GetCopyDataEntityScript(this, Entities, progress, token);
                    DMEEditor.ETL.Script.ScriptDetails = retscripts;
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
        public IErrorsInfo RefreshData( IProgress<PassedArgs> progress, CancellationToken token)
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
        #region "Load Save Data Methods"
        //public virtual IErrorsInfo LoadStructure(IProgress<PassedArgs> progress, CancellationToken token, bool copydata = false)
        //{
        //    DMEEditor.ErrorObject.Flag = Errors.Ok;
        //    try
        //    {
        //        if (!IsStructureLoaded)
        //        {
        //            Entities.Clear();
        //            EntitiesNames.Clear();
        //            LoadEntities(DatasourceName);
        //            if (Entities != null && Entities.Any())
        //            {

        //                IsStructureLoaded = true;
        //                IsLoaded = true;
        //            }

        //            OnLoadStructure?.Invoke(this, (PassedArgs)DMEEditor.Passedarguments);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        IsStructureLoaded = false;
        //        IsStructureCreated = false;
        //        DMEEditor.AddLogMessage("Beep", $"Failed to load in-memory structure: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
        //    }
        //    return DMEEditor.ErrorObject;
        //}
        //public virtual IErrorsInfo SaveStructure()
        //{
        //    DMEEditor.ErrorObject.Flag = Errors.Ok;

        //    try
        //    {
        //        // Step 1: Get the latest list of table/entity names
        //        if (Entities == null || Entities.Count == 0)
        //        {
        //            DMEEditor.AddLogMessage("Beep", $"No entities found in the in-memory structure for {DatasourceName}.", DateTime.Now, 0, null, Errors.Failed);
        //            return DMEEditor.ErrorObject;
        //        }
        //        if (ConnectionStatus != ConnectionState.Open)
        //        {
        //            DMEEditor.AddLogMessage("Beep", $"Connection is not established for {DatasourceName}.", DateTime.Now, 0, null, Errors.Failed);
        //            return DMEEditor.ErrorObject;
        //        }
        //        var entityNamesFromDb = GetEntitesList();
        //        SyncEntitiesNameandEntities();
        //        // Step 2: Ensure Entities collection contains all the entities from the database

        //        SaveEntites(DatasourceName);

        //        // Step 7: Raise event
        //        OnSaveStructure?.Invoke(this, (PassedArgs)DMEEditor.Passedarguments);
        //    }
        //    catch (Exception ex)
        //    {
        //        DMEEditor.ErrorObject.Flag = Errors.Failed;
        //        DMEEditor.AddLogMessage("Beep", $"Failed to save in-memory structure for {DatasourceName}: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
        //    }

        //    return DMEEditor.ErrorObject;
        //}
        //public void SyncEntitiesNameandEntities()
        //{
        //    try
        //    {
        //        // Initialize collections if they're null
        //        if (Entities == null) Entities = new List<EntityStructure>();
        //        if (EntitiesNames == null) EntitiesNames = new List<string>();

        //        // Step 1: Create a copy of EntitiesNames to safely iterate
        //        var entityNamesToProcess = new List<string>(EntitiesNames);
        //        var entityNamesToRemove = new List<string>();

        //        // Step 2: Check entities in EntitiesNames and add missing ones to Entities
        //        foreach (string entityName in entityNamesToProcess)
        //        {
        //            if (string.IsNullOrEmpty(entityName)) continue;

        //            // Check if entity already exists in Entities collection
        //            bool entityExists = Entities.Any(e => e.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));

        //            if (!entityExists)
        //            {
        //                // Try to get entity structure
        //                EntityStructure entityStructure = GetEntityStructure(entityName);
        //                if (entityStructure != null)
        //                {
        //                    // Add to Entities if not already there
        //                    Entities.Add(entityStructure);
        //                    DMEEditor.AddLogMessage("Success", $"Added entity {entityName} to Entities collection", DateTime.Now, 0, null, Errors.Ok);
        //                }
        //                else
        //                {
        //                    // Mark for removal from EntitiesNames if structure couldn't be retrieved
        //                    entityNamesToRemove.Add(entityName);
        //                    DMEEditor.AddLogMessage("Warning", $"Could not get structure for entity {entityName}, removing from EntitiesNames", DateTime.Now, 0, null, Errors.Warning);
        //                }
        //            }
        //        }

        //        // Step 3: Remove invalid entities from EntitiesNames
        //        foreach (string entityToRemove in entityNamesToRemove)
        //        {
        //            EntitiesNames.Remove(entityToRemove);
        //        }

        //        // Step 4: Check Entities collection and ensure all are in EntitiesNames
        //        // Also create missing entities in database if needed
        //        foreach (EntityStructure entity in Entities.ToList()) // Use ToList() to avoid collection modification issues
        //        {
        //            if (entity == null || string.IsNullOrEmpty(entity.EntityName)) continue;

        //            // Check if entity name exists in EntitiesNames
        //            if (!EntitiesNames.Contains(entity.EntityName, StringComparer.OrdinalIgnoreCase))
        //            {
        //                // Try to create entity in database
        //                bool created = CreateEntityAs(entity);

        //                if (created)
        //                {
        //                    // Add to EntitiesNames if successfully created
        //                    EntitiesNames.Add(entity.EntityName);
        //                    DMEEditor.AddLogMessage("Success", $"Created entity {entity.EntityName} in database and added to EntitiesNames", DateTime.Now, 0, null, Errors.Ok);
        //                }
        //                else
        //                {
        //                    DMEEditor.AddLogMessage("Warning", $"Failed to create entity {entity.EntityName} in database", DateTime.Now, 0, null, Errors.Warning);
        //                }
        //            }
        //        }

        //        // Step 5: Sync with InMemoryStructures
        //        if (InMemoryStructures == null) InMemoryStructures = new List<EntityStructure>();
        //        InMemoryStructures = new List<EntityStructure>(Entities);

        //        // Step 6: Sync with ConnectionProp.Entities if available
        //        if (Dataconnection?.ConnectionProp?.Entities != null)
        //        {
        //            Dataconnection.ConnectionProp.Entities = InMemoryStructures;
        //        }

        //        // Mark as synced
        //        IsSynced = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        DMEEditor.AddLogMessage("Error", $"Error synchronizing entities: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
        //        IsSynced = false;
        //    }
        //}
        //public IErrorsInfo SaveEntites(string datasourcename)
        //{

        //    DMEEditor.ErrorObject.Flag = Errors.Ok;
        //    PassedArgs passedArgs = new PassedArgs { DatasourceName = datasourcename };
        //    try
        //    {
        //        IDataSource DataSource = DMEEditor.GetDataSource(datasourcename);
        //        if (DataSource != null)
        //        {
        //            DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new TheTechIdea.Beep.ConfigUtil.DatasourceEntities { datasourcename = datasourcename, Entities = this.Entities });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        DMEEditor.Logger.WriteLog($"Error in Saving Entities ({ex.Message}) ");
        //        DMEEditor.ErrorObject.Flag = Errors.Failed;
        //        DMEEditor.ErrorObject.Ex = ex;
        //    }
        //    return DMEEditor.ErrorObject;
        //}
        //public IErrorsInfo LoadEntities(string datasourcename)
        //{



        //    DMEEditor.ErrorObject.Flag = Errors.Ok;
        //    PassedArgs passedArgs = new PassedArgs { DatasourceName = datasourcename };
        //    try
        //    {
        //        IDataSource DataSource = DMEEditor.GetDataSource(datasourcename);
        //        if (DataSource != null)
        //        {
        //            var ents = DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues(datasourcename);
        //            if (ents != null)
        //            {
        //                if (ents.Entities.Count > 0)
        //                {
        //                    DataSource.Entities = ents.Entities;
        //                    DataSource.EntitiesNames = ents.Entities.Select(x => x.EntityName).ToList();
        //                }
        //            }
        //            InMemoryStructures = DataSource.Entities;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        DMEEditor.Logger.WriteLog($"Error in Loading Entities ({ex.Message}) ");
        //        DMEEditor.ErrorObject.Flag = Errors.Failed;
        //        DMEEditor.ErrorObject.Ex = ex;
        //    }
        //    return DMEEditor.ErrorObject;
        //}
        //public IErrorsInfo CreateStructure(IProgress<PassedArgs> progress, CancellationToken token)
        //{
        //    DMEEditor.ErrorObject.Flag = Errors.Ok;
        //    try
        //    {
        //        if (!IsStructureCreated)
        //        {

        //            GetEntitesList();
        //            for (int i = 0; i < Entities.Count - 1; i++)
        //            {
        //                if (!string.IsNullOrEmpty(Entities[i].DataSourceID))
        //                {
        //                    CreateEntityAs(Entities[i]);
        //                    Entities[i].IsCreated = true;
        //                }


        //            }

        //        }
        //        IsStructureCreated = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        IsStructureCreated = false;
        //        DMEEditor.AddLogMessage("Beep", $"Could not Load InMemory Structure for {DatasourceName}- {ex.Message}", System.DateTime.Now, 0, null, Errors.Failed);
        //    }
        //    return DMEEditor.ErrorObject;
        //}
        #endregion
    }

}
