using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlServerCe;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.DataBase
{
    [AddinAttribute(Category = DatasourceCategory.RDBMS, DatasourceType =  DataSourceType.SqlCompact)]
    public class SQLCompactDataSource : RDBSource, ILocalDB
    {
        public bool CanCreateLocal { get ; set; }
        public SQLCompactDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per) : base(datasourcename, logger, DMEEditor, databasetype, per)
        {
            Dataconnection.ConnectionProp.DatabaseType = DataSourceType.SqlCompact;
        }
        public   bool CopyDB(string DestDbName, string DesPath)
        {
            try
            {
                if (!System.IO.File.Exists(base.Dataconnection.ConnectionProp.ConnectionString))
                {
                    File.Copy(base.Dataconnection.ConnectionProp.ConnectionString, Path.Combine(DesPath, DestDbName));
                }
                DMEEditor.AddLogMessage("Success", "Copy SQLCompact Database", DateTime.Now, 0, null, Errors.Ok);
                return true;
            }
            catch (Exception ex)
            {
                string mes = "Could not Copy SQLCompact Database";
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
                    base.Dataconnection.ConnectionProp.FileName = base.Dataconnection.ConnectionProp.FileName + ".sdf";
                }
                if (!System.IO.File.Exists(Path.Combine(base.Dataconnection.ConnectionProp.FilePath, base.Dataconnection.ConnectionProp.FileName)))
                {
                  
                    SqlCeEngine en = new SqlCeEngine("DataSource='" + Path.Combine(base.Dataconnection.ConnectionProp.FilePath, base.Dataconnection.ConnectionProp.FileName) + "'");
                    en.CreateDatabase();
                    DMEEditor.AddLogMessage("Success", "Create SQLCompact Database", DateTime.Now, 0, null, Errors.Ok);
                }
                else
                {
                    DMEEditor.AddLogMessage("Success", "SQLCompact Database already exist", DateTime.Now, 0, null, Errors.Ok);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public  bool DeleteDB()
        {
            try
            {
                if (Closeconnection()== ConnectionState.Closed)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    if (System.IO.File.Exists(Path.Combine(base.Dataconnection.ConnectionProp.FilePath, base.Dataconnection.ConnectionProp.FileName)))
                    {
                        File.Delete(Path.Combine(base.Dataconnection.ConnectionProp.FilePath, base.Dataconnection.ConnectionProp.FileName));
                    }
                    DMEEditor.AddLogMessage("Success", "Deleted SQLCompact Database", DateTime.Now, 0, null, Errors.Ok);
                    return true;
                }
                else
                {
                    string mes = "Could not Delete SQLCompact Database";
                    DMEEditor.AddLogMessage("Fail", mes, DateTime.Now, -1, mes, Errors.Failed);
                    return false;
                }
            }
            catch (Exception ex)
            {
                string mes = "Could not Delete SQLCompact Database";
                DMEEditor.AddLogMessage("Fail",ex.Message+ mes, DateTime.Now, -1, mes, Errors.Failed);
                return false;
            };
        }
        public  IErrorsInfo DropEntity(string EntityName)
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
        public override ConnectionState Closeconnection()
        {
            try

            {
                if (base.RDBMSConnection.DbConn!=null)
                {
                    base.RDBMSConnection.DbConn.Close();
                    base.RDBMSConnection.DbConn.Dispose();
                }
               
                DMEEditor.AddLogMessage("Success", $"Closing connection to SQL Compact Database", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string errmsg = "Error Closing connection to SQL Compact Database";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return   base.RDBMSConnection.DbConn.State;
        }
        public override string DisableFKConstraints( EntityStructure t1)
        {
            try
            {
                if (t1 != null)
                {
                    this.ExecuteSql($"ALTER TABLE {t1.EntityName} NOCHECK CONSTRAINT ALL");
                    DMEEditor.ErrorObject.Message = "successfull Disabled SQlCompact FK Constraints";
                    DMEEditor.ErrorObject.Flag = Errors.Ok;

                }
             
            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", "Diabling SQlCompact FK Constraints" + ex.Message, DateTime.Now, 0, t1.EntityName, Errors.Failed);
            }
            return DMEEditor.ErrorObject.Message;
        }

        public override string EnableFKConstraints( EntityStructure t1)
        {
            try
            {
                this.ExecuteSql($"ALTER TABLE {t1.EntityName} WITH CHECK CHECK CONSTRAINT all");
                DMEEditor.ErrorObject.Message = "successfull Enabled SQlCompact FK Constraints";
                DMEEditor.ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", "Enabing SQlCompact FK Constraints" + ex.Message, DateTime.Now, 0, t1.EntityName, Errors.Failed);
            }
            return DMEEditor.ErrorObject.Message;
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

    }
}
