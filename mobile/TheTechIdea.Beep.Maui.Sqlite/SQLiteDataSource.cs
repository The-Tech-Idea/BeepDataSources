using DataManagementModels.DataBase;
using Microsoft.VisualBasic;
using SQLite;
using System.Data;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Maui.DataSource.Sqlite
{
    [AddinAttribute(Category = DatasourceCategory.RDBMS, DatasourceType = DataSourceType.SqlLite)]
    public class SQLiteDataSource : RDBSource, ILocalDB,IInMemoryDB

    {
        private string dateformat = "yyyy-MM-dd HH:mm:ss";
        public bool CanCreateLocal { get ; set; }
        SQLiteAsyncConnection sQLiteConnection;
        string dbpath;
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
                Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.FirstOrDefault(p => p.ConnectionName.Equals(pdatasourcename, StringComparison.InvariantCultureIgnoreCase)); ;
                if (Dataconnection.ConnectionProp==null)
                {
                    Dataconnection.ConnectionProp = new ConnectionProperties();
                }
              
            }
            Dataconnection.ConnectionProp.DatabaseType = DataSourceType.SqlLite;
            ColumnDelimiter = "[]";
            ParameterDelimiter = "$";
            dbpath =Path.Combine(DMEEditor.ConfigEditor.ExePath , "Scripts" , DatasourceName);
        }
        public List<EntityStructure> InMemoryStructures { get; set; } = new List<EntityStructure>();
        public override string ColumnDelimiter { get; set; } = "[]";
        public override string ParameterDelimiter { get; set; } = "$";
        public override ConnectionState Openconnection()
        {
            
         
           
            ETLScriptHDR scriptHDR = new ETLScriptHDR();
            scriptHDR.ScriptDTL = new List<ETLScriptDet>();
            CancellationTokenSource token = new CancellationTokenSource();
            InMemory = Dataconnection.ConnectionProp.IsInMemory;
            Dataconnection.InMemory = Dataconnection.ConnectionProp.IsInMemory;
            if (ConnectionStatus == ConnectionState.Open)
            {
                DMEEditor.AddLogMessage("Beep", $"Connection is already open", DateTime.Now, -1, "", Errors.Ok);
                return ConnectionState.Open;
            }
            if (Dataconnection.ConnectionProp.IsInMemory)
            {
                 OpenDatabaseInMemory(Dataconnection.ConnectionProp.Database);
                 base.Openconnection();
                    if (DMEEditor.ErrorObject.Flag == Errors.Ok)
                    {
                        LoadStructure();
                        return ConnectionState.Open;
                    }
            }else
                {
                base.Openconnection();
               
                }
            return ConnectionStatus;
        }
        public bool InMemory { get; set; } = false;
        public  bool CopyDB( string DestDbName, string DesPath)
        {
            try
            {
                if (!System.IO.File.Exists(Path.Combine(base.Dataconnection.ConnectionProp.FilePath, base.Dataconnection.ConnectionProp.FileName)))
                {
                    File.Copy(base.Dataconnection.ConnectionProp.ConnectionString, Path.Combine(DesPath,DestDbName));
                }
                DMEEditor.AddLogMessage("Success", "Copy Sqlite Database", DateTime.Now, 0, null, Errors.Ok);
                return true;
            }
            catch (Exception ex)
            {
                string mes = "Could not Copy Sqlite Database";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
                return false;
            };
        }
        public  bool CreateDB()
        {
            try
            {
                if (!Path.HasExtension(base.Dataconnection.ConnectionProp.FileName) )
                {
                    base.Dataconnection.ConnectionProp.FileName = base.Dataconnection.ConnectionProp.FileName + ".s3db";
                }
                if (!System.IO.File.Exists(Path.Combine(base.Dataconnection.ConnectionProp.FilePath, base.Dataconnection.ConnectionProp.FileName)))
                {
                    sQLiteConnection= new SQLiteAsyncConnection(Path.Combine(base.Dataconnection.ConnectionProp.FilePath, base.Dataconnection.ConnectionProp.FileName), SQLiteOpenFlags.Create);
                    //SQLiteConnection.CreateFile( Path.Combine(base.Dataconnection.ConnectionProp.FilePath, base.Dataconnection.ConnectionProp.FileName ));
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
            };
        }
        public bool CreateDB(bool inMemory)
        {
            return false;
        }
        public bool CreateDB(string filepathandname)
        {
            return false;
        }
        public  bool DeleteDB()
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
                DMEEditor.AddLogMessage("Fail",ex.Message+ mes, DateTime.Now, -1, mes, Errors.Failed);
                return false;
            };
        }
        public  IErrorsInfo DropEntity(string EntityName)
        {  try

            {

                String cmdText = $"drop table  '{EntityName}'";
                DMEEditor.ErrorObject=base.ExecuteSql(cmdText);
              
                if (!base.CheckEntityExist(EntityName))
                {
                    DMEEditor.AddLogMessage("Success", $"Droping Entity {EntityName}", DateTime.Now, 0, null, Errors.Ok);
                }else
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
        public override ConnectionState Closeconnection()
        {
            try
            {
                sQLiteConnection.CloseAsync();
                if (base.RDBMSConnection.DbConn != null)
                {
                    base.RDBMSConnection.DbConn.Close();
                }
                SaveStructure();
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
        public override void Dispose()
        {
            Closeconnection();
            base.Dispose();
        }
        public override string DisableFKConstraints( EntityStructure t1)
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
        private  List<FkListforSQLlite> GetSqlLiteTableKeysAsync(string tablename)
        {
            var tb =  base.GetData<FkListforSQLlite>("PRAGMA foreign_key_check(" + tablename + ");");
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
                base.Dataconnection.InMemory = true;
                base.Dataconnection.ConnectionProp.FileName = string.Empty;
                base.Dataconnection.ConnectionProp.ConnectionString=$@"Data Source=:memory:;Version=3;New=True;";
                base.Dataconnection.ConnectionProp.Database = databasename;
                base.Dataconnection.ConnectionProp.ConnectionName = databasename;
                //base.Dataconnection.ConnectionStatus = ConnectionState.Open;
              
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
        public override bool CreateEntityAs(EntityStructure entity)
        {
            string ds = entity.DataSourceID;
            bool retval = base.CreateEntityAs(entity);
            entity.DataSourceID= ds;
         
            InMemoryStructures.Add(GetEntity(entity));
            return retval;
        }
        private EntityStructure GetEntity(EntityStructure entity)
        {
            EntityStructure ent = new EntityStructure();
            ent.DatasourceEntityName = entity.DatasourceEntityName;
            ent.DataSourceID=entity.DataSourceID; ;
            ent.DatabaseType = entity.DatabaseType;
            ent.Caption=entity.Caption;
            ent.Category = entity.Category;
            ent.Fields = entity.Fields;
            ent.PrimaryKeys= entity.PrimaryKeys;
            ent.Relations= entity.Relations;
            ent.OriginalEntityName = entity.OriginalEntityName;
            ent.GuidID = Guid.NewGuid().ToString();
            ent.ViewID= entity.ViewID;
            ent.Viewtype = entity.Viewtype;
            ent.EntityName= entity.EntityName;
            ent.OriginalEntityName= entity.OriginalEntityName;
            ent.SchemaOrOwnerOrDatabase=entity.SchemaOrOwnerOrDatabase;
            return ent;
        }
        public IErrorsInfo LoadStructure()
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                string filepath = Path.Combine(dbpath, "createscripts.json");
                string InMemoryStructuresfilepath = Path.Combine(dbpath, "InMemoryStructures.json");
                ConnectionStatus = ConnectionState.Open;
                InMemoryStructures = new List<EntityStructure>();
                Entities = new List<EntityStructure>();
                EntitiesNames = new List<string>();
                CancellationTokenSource token = new CancellationTokenSource();
                if (File.Exists(InMemoryStructuresfilepath))
                {
                    InMemoryStructures = DMEEditor.ConfigEditor.JsonLoader.DeserializeObject<EntityStructure>(InMemoryStructuresfilepath);
                }
                if (File.Exists(filepath))
                {
                    var hdr = DMEEditor.ConfigEditor.JsonLoader.DeserializeSingleObject<ETLScriptHDR>(filepath);
                    DMEEditor.ETL.Script = hdr;
                    DMEEditor.ETL.Script.LastRunDateTime = DateTime.Now;
                    DMEEditor.ETL.RunCreateScript(DMEEditor.progress, token.Token);

                }
              
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Could not Load InMemory Structure for {DatasourceName}- {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public IErrorsInfo SaveStructure()
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                if (InMemoryStructures.Count > 0)
                {
                   
                    Directory.CreateDirectory(dbpath);

                    string filepath = Path.Combine(dbpath,"createscripts.json");
                    string InMemoryStructuresfilepath = Path.Combine(dbpath, "InMemoryStructures.json");
                    ETLScriptHDR scriptHDR = new ETLScriptHDR();
                    scriptHDR.ScriptDTL = new List<ETLScriptDet>();
                    CancellationTokenSource token = new CancellationTokenSource();
                    scriptHDR.scriptName = Dataconnection.ConnectionProp.Database;
                    scriptHDR.scriptStatus = "SAVED";
                    scriptHDR.ScriptDTL.AddRange(DMEEditor.ETL.GetCreateEntityScript(this, InMemoryStructures, DMEEditor.progress, token.Token));
                    scriptHDR.ScriptDTL.AddRange(DMEEditor.ETL.GetCopyDataEntityScript(this, InMemoryStructures, DMEEditor.progress, token.Token));
                    DMEEditor.ConfigEditor.JsonLoader.Serialize(filepath, scriptHDR);
                    DMEEditor.ConfigEditor.JsonLoader.Serialize(InMemoryStructuresfilepath, InMemoryStructures);
                }

            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Could not save InMemory Structure for {DatasourceName}- {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
    }
   
}
