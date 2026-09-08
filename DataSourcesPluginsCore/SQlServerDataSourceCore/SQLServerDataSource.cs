using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>
    /// SQL Server data source — inherits BeginTransaction / EndTransaction / Commit / CRUD from
    /// RDBSource. Only dialect-specific FK-toggle SQL and a paging helper are overridden.
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.RDBMS, DatasourceType = DataSourceType.SqlServer)]
    public class SQLServerDataSource : RDBSource, IDataSource
    {
        public SQLServerDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per)
            : base(datasourcename, logger, DMEEditor, databasetype, per)
        {
        }

        public override string DisableFKConstraints(EntityStructure t1)
        {
            try
            {
                this.ExecuteSql($"ALTER TABLE {t1.EntityName} NOCHECK CONSTRAINT ALL");
                DMEEditor.ErrorObject.Message = "Successfully Disabled SQL Server FK Constraints";
                DMEEditor.ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", "Disabling SQL Server FK Constraints: " + ex.Message, DateTime.Now, 0, t1?.EntityName, Errors.Failed);
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = ex.Message;
            }
            return DMEEditor.ErrorObject.Message;
        }

        public override string EnableFKConstraints(EntityStructure t1)
        {
            try
            {
                this.ExecuteSql($"ALTER TABLE {t1.EntityName} WITH CHECK CHECK CONSTRAINT ALL");
                DMEEditor.ErrorObject.Message = "Successfully Enabled SQL Server FK Constraints";
                DMEEditor.ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", "Enabling SQL Server FK Constraints: " + ex.Message, DateTime.Now, 0, t1?.EntityName, Errors.Failed);
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = ex.Message;
            }
            return DMEEditor.ErrorObject.Message;
        }

        // PagedQuery (SQL Server OFFSET/FETCH, driven by "Pagesize"/"pagenumber" filter entries) was
        // declared here with no caller anywhere in this class -- dead code. Paging for every RDBMS
        // driver, including SQL Server, now runs through the single dialect source RDBSource
        // delegates to (RDBMSHelper.GetPagingSyntax); duplicating that logic here risked exactly the
        // K11 defect this project fixed (two paging implementations disagreeing).
    }
}