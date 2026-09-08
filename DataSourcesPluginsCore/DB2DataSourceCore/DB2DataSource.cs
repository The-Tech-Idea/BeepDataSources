using System;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>
    /// IBM Db2 (LUW) data source — inherits BeginTransaction / EndTransaction / Commit / CRUD /
    /// paging / type mapping from <see cref="RDBSource"/>. Only dialect-specific FK-toggle SQL is
    /// overridden.
    /// </summary>
    /// <remarks>
    /// This class did not exist, and neither did a project for it. `ConnectionHelper.CreateDB2Config()`
    /// in BeepDM has always declared `classHandler = "DB2DataSource"` and connects through
    /// `IBM.Data.DB2` — no reference to that package is needed here at compile time, matching
    /// <c>FireBirdDataSource</c>'s pattern: <c>RDBDataConnection</c> resolves the actual provider
    /// types from <c>ConnectionDriversConfig</c> by name at runtime.
    ///
    /// Db2's own referential integrity toggle is <c>SET INTEGRITY FOR ... OFF</c> /
    /// <c>... IMMEDIATE CHECKED</c> — not the SQL Server/MySQL/PostgreSQL
    /// <c>ALTER TABLE ... [NO]CHECK CONSTRAINT</c> family this base class's other drivers use, and
    /// it operates on the whole table's integrity state (all constraints at once), which
    /// <c>SET INTEGRITY ... IMMEDIATE CHECKED</c> restores. Targets Db2 for Linux/Unix/Windows
    /// specifically; Db2 for i and Db2 for z/OS have their own administrative differences not
    /// accounted for here.
    /// </remarks>
    [AddinAttribute(Category = DatasourceCategory.RDBMS, DatasourceType = DataSourceType.DB2)]
    public class DB2DataSource : RDBSource, IDataSource
    {
        public DB2DataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per)
            : base(datasourcename, logger, DMEEditor, databasetype, per)
        {
        }

        public override string DisableFKConstraints(EntityStructure t1)
        {
            try
            {
                this.ExecuteSql($"SET INTEGRITY FOR {t1.EntityName} OFF");
                DMEEditor.ErrorObject.Message = "Successfully Disabled Db2 FK Constraints";
                DMEEditor.ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", "Disabling Db2 FK Constraints: " + ex.Message, DateTime.Now, 0, t1?.EntityName, Errors.Failed);
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = ex.Message;
            }
            return DMEEditor.ErrorObject.Message;
        }

        public override string EnableFKConstraints(EntityStructure t1)
        {
            try
            {
                this.ExecuteSql($"SET INTEGRITY FOR {t1.EntityName} IMMEDIATE CHECKED");
                DMEEditor.ErrorObject.Message = "Successfully Enabled Db2 FK Constraints";
                DMEEditor.ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", "Enabling Db2 FK Constraints: " + ex.Message, DateTime.Now, 0, t1?.EntityName, Errors.Failed);
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = ex.Message;
            }
            return DMEEditor.ErrorObject.Message;
        }
    }
}
