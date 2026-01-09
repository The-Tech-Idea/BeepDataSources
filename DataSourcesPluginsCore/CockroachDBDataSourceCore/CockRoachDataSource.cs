using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using TheTechIdea;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Vis;

namespace TheTechIdea.Beep.DataBase
{
    [AddinAttribute(Category = DatasourceCategory.RDBMS, DatasourceType = DataSourceType.CockroachDB)]
    public class CockRoachDataSource : RDBSource, IDataSource
    {
        public CockRoachDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per) : base(datasourcename, logger, DMEEditor, databasetype, per)
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
                DMEEditor.AddLogMessage("Beep", $"Error in Commit Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        public override string DisableFKConstraints(EntityStructure t1)
        {
            try
            {
                // CockroachDB uses PostgreSQL-compatible syntax
                this.ExecuteSql($"ALTER TABLE {t1.EntityName} DISABLE TRIGGER ALL");
                DMEEditor.ErrorObject.Message = "Successfully Disabled CockroachDB FK Constraints";
                DMEEditor.ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", "Disabling CockroachDB FK Constraints" + ex.Message, DateTime.Now, 0, t1.EntityName, Errors.Failed);
            }
            return DMEEditor.ErrorObject.Message;
        }

        public override string EnableFKConstraints(EntityStructure t1)
        {
            try
            {
                // CockroachDB uses PostgreSQL-compatible syntax
                this.ExecuteSql($"ALTER TABLE {t1.EntityName} ENABLE TRIGGER ALL");
                DMEEditor.ErrorObject.Message = "Successfully Enabled CockroachDB FK Constraints";
                DMEEditor.ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", "Enabling CockroachDB FK Constraints" + ex.Message, DateTime.Now, 0, t1.EntityName, Errors.Failed);
            }
            return DMEEditor.ErrorObject.Message;
        }


    }
}
