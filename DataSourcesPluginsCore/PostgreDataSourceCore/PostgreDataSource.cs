using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.DataBase
{
    [AddinAttribute(Category = DatasourceCategory.RDBMS, DatasourceType = DataSourceType.Postgre)]
    public class PostgreDataSource : RDBSource, IDataSource
    {
        public PostgreDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per) : base(datasourcename, logger, DMEEditor, databasetype, per)
        {

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
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
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
                DMEEditor.AddLogMessage("Beep", $"Error in End Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
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
                DMEEditor.AddLogMessage("Beep", $"Error in Commit Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
            }
            return DMEEditor.ErrorObject;
        }
        public override string DisableFKConstraints(EntityStructure t1)
        {
            try
            {
                this.ExecuteSql($"ALTER TABLE {t1.EntityName} DISABLE TRIGGER ALL");
                DMEEditor.ErrorObject.Message = "Successfully Disabled PostgreSQL FK Constraints";
                DMEEditor.ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", "Disabling PostgreSQL FK Constraints: " + ex.Message, DateTime.Now, 0, t1.EntityName, Errors.Failed);
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = ex.Message;
            }
            return DMEEditor.ErrorObject.Message;
        }
        public override IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                EntityStructure t1 = GetEntityStructure(EntityName);
                string sql = $"INSERT INTO {EntityName} (";
                string sql2 = " VALUES (";
                foreach (EntityField fld in t1.Fields)
                {
                    if (fld.IsKey == false)
                    {
                        sql += fld.fieldname + ",";
                        sql2 += $"'{InsertedData.GetType().GetProperty(fld.fieldname).GetValue(InsertedData)}',";
                    }
                }
                sql = sql.Substring(0, sql.Length - 1) + ")";
                sql2 = sql2.Substring(0, sql2.Length - 1) + ")";
                sql += sql2;
                this.ExecuteSql(sql);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", "Inserting Data: " + ex.Message, DateTime.Now, 0, EntityName, Errors.Failed);
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
            }
            return DMEEditor.ErrorObject;

        }
        public override string EnableFKConstraints(EntityStructure t1)
        {
            try
            {
                this.ExecuteSql($"ALTER TABLE {t1.EntityName} ENABLE TRIGGER ALL");
                DMEEditor.ErrorObject.Message = "Successfully Enabled PostgreSQL FK Constraints";
                DMEEditor.ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", "Enabling PostgreSQL FK Constraints: " + ex.Message, DateTime.Now, 0, t1.EntityName, Errors.Failed);
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = ex.Message;
            }
            return DMEEditor.ErrorObject.Message;
        }
    
    }
}
