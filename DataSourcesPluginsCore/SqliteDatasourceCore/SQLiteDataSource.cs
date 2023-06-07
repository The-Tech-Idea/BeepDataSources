using DataManagementModels.DataBase;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.DataBase
{
    [AddinAttribute(Category = DatasourceCategory.RDBMS, DatasourceType = DataSourceType.SqlLite)]
    public class SQLiteDataSource : RDBSource, ILocalDB,IInMemoryDB

    {
        private string dateformat = "yyyy-MM-dd HH:mm:ss";
        public bool CanCreateLocal { get ; set; }
        SQLiteConnection sQLiteConnection;
        public SQLiteDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per) : base(datasourcename, logger, DMEEditor, databasetype, per)
        {

            Dataconnection.ConnectionProp.DatabaseType = DataSourceType.SqlLite;
            ColumnDelimiter = "[]";
            ParameterDelimiter = "$";
        }
        public override string ColumnDelimiter { get; set; } = "[]";
        public override string ParameterDelimiter { get; set; } = "$";
        public override ConnectionState Openconnection()
        {
            ETLScriptHDR scriptHDR = new ETLScriptHDR();
            scriptHDR.ScriptDTL = new List<ETLScriptDet>();
            CancellationTokenSource token = new CancellationTokenSource();

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

                        ConnectionStatus = ConnectionState.Open;
                        return ConnectionState.Open;
                    }

               

            }else
                {
                base.Openconnection();
               
                }
         
            if(InMemory && ConnectionStatus== ConnectionState.Open)
            {
                //var ret = DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues(Dataconnection.ConnectionProp.Database);
                //if (ret != null)
                //{
                //    Entities = ret.Entities;
                //}
                //else
                //{
                    Entities = new List<EntityStructure>();
                //}
                if (Entities != null)
                {
                    if (Entities.Count > 0)
                    {
                       string filepath = DMEEditor.ConfigEditor.ConfigPath + "\\Scripts\\" + Dataconnection.ConnectionProp.Database + ".json";
                       if (File.Exists(filepath))
                       {
                            scriptHDR= DMEEditor.ConfigEditor.JsonLoader.DeserializeSingleObject<ETLScriptHDR>(filepath); ;
                       }
                       else
                       {
                            scriptHDR.scriptName = Dataconnection.ConnectionProp.Database;
                            scriptHDR.scriptStatus = "New"; 
                            scriptHDR.ScriptDTL.AddRange(DMEEditor.ETL.GetCreateEntityScript(this, Entities, DMEEditor.progress, token.Token));
                            scriptHDR.ScriptDTL.AddRange(DMEEditor.ETL.GetCopyDataEntityScript(this, Entities, DMEEditor.progress, token.Token));
                        }
                      
                        DMEEditor.ETL.Script = scriptHDR;
                        DMEEditor.ETL.Script.LastRunDateTime = DateTime.Now;
                        DMEEditor.ETL.RunCreateScript(DMEEditor.progress, token.Token);
                    }
                }
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
                    SQLiteConnection.CreateFile( Path.Combine(base.Dataconnection.ConnectionProp.FilePath, base.Dataconnection.ConnectionProp.FileName ));
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
                SQLiteConnection.ClearAllPools();
                if (base.RDBMSConnection.DbConn != null)
                {
                    base.RDBMSConnection.DbConn.Close();
                }
                if (DMEEditor.ETL.Script.ScriptDTL.Count > 0)
                {
                    string filepath = DMEEditor.ConfigEditor.ConfigPath + "\\Scripts\\" + DatasourceName + ".json";
                    DMEEditor.ConfigEditor.JsonLoader.Serialize(filepath, DMEEditor.ETL.Script); ;
                }
                DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new ConfigUtil.DatasourceEntities() { datasourcename=DatasourceName, Entities=Entities, GuidID=Dataconnection.GuidID, ID=Dataconnection.ID});

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
    }
   
}
