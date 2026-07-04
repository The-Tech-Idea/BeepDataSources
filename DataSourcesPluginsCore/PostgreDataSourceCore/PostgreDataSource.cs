using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>
    /// PostgreSQL data source — inherits BeginTransaction / EndTransaction / Commit / InsertEntity /
    /// UpdateEntity / DeleteEntity from RDBSource. Only dialect-specific FK-toggle SQL is overridden.
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.RDBMS, DatasourceType = DataSourceType.Postgre)]
    public class PostgreDataSource : RDBSource, IDataSource
    {
        public PostgreDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per)
            : base(datasourcename, logger, DMEEditor, databasetype, per)
        {
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
                DMEEditor.AddLogMessage("Fail", "Disabling PostgreSQL FK Constraints: " + ex.Message, DateTime.Now, 0, t1?.EntityName, Errors.Failed);
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = ex.Message;
            }
            return DMEEditor.ErrorObject.Message;
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
                DMEEditor.AddLogMessage("Fail", "Enabling PostgreSQL FK Constraints: " + ex.Message, DateTime.Now, 0, t1?.EntityName, Errors.Failed);
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = ex.Message;
            }
            return DMEEditor.ErrorObject.Message;
        }
    }
}