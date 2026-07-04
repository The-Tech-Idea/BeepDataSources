using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
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
    /// <summary>
    /// CockroachDB data source — inherits BeginTransaction / EndTransaction / Commit / CRUD from
    /// RDBSource. Only dialect-specific FK-toggle SQL is overridden (CockroachDB inherits
    /// PostgreSQL's trigger-based DDL).
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.RDBMS, DatasourceType = DataSourceType.Cockroach)]
    public class CockRoachDataSource : RDBSource, IDataSource
    {
        public CockRoachDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per)
            : base(datasourcename, logger, DMEEditor, databasetype, per)
        {
        }

        public override string DisableFKConstraints(EntityStructure t1)
        {
            try
            {
                this.ExecuteSql($"ALTER TABLE {t1.EntityName} DISABLE TRIGGER ALL");
                DMEEditor.ErrorObject.Message = "Successfully Disabled CockroachDB FK Constraints";
                DMEEditor.ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", "Disabling CockroachDB FK Constraints: " + ex.Message, DateTime.Now, 0, t1?.EntityName, Errors.Failed);
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
                DMEEditor.ErrorObject.Message = "Successfully Enabled CockroachDB FK Constraints";
                DMEEditor.ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", "Enabling CockroachDB FK Constraints: " + ex.Message, DateTime.Now, 0, t1?.EntityName, Errors.Failed);
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = ex.Message;
            }
            return DMEEditor.ErrorObject.Message;
        }
    }
}