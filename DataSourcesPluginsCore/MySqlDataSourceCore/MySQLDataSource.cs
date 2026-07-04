using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>
    /// MySQL / MariaDB data source — inherits BeginTransaction / EndTransaction / Commit / CRUD from RDBSource.
    /// Only dialect-specific FK-toggle SQL and delimiter characters are overridden.
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.RDBMS, DatasourceType = DataSourceType.Mysql)]
    public class MySQLDataSource : RDBSource, IDataSource
    {
        public MySQLDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per)
            : base(datasourcename, logger, DMEEditor, databasetype, per)
        {
        }

        public override string ColumnDelimiter { get; set; } = "'";
        public override string ParameterDelimiter { get; set; } = "@";

        public override string DisableFKConstraints(EntityStructure t1)
        {
            try
            {
                this.ExecuteSql("SET FOREIGN_KEY_CHECKS=0;");
                DMEEditor.ErrorObject.Message = "Successfully Disabled MySQL FK Constraints";
                DMEEditor.ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", "Disabling MySQL FK Constraints: " + ex.Message, DateTime.Now, 0, t1?.EntityName, Errors.Failed);
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = ex.Message;
            }
            return DMEEditor.ErrorObject.Message;
        }

        public override string EnableFKConstraints(EntityStructure t1)
        {
            try
            {
                this.ExecuteSql("SET FOREIGN_KEY_CHECKS=1;");
                DMEEditor.ErrorObject.Message = "Successfully Enabled MySQL FK Constraints";
                DMEEditor.ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", "Enabling MySQL FK Constraints: " + ex.Message, DateTime.Now, 0, t1?.EntityName, Errors.Failed);
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = ex.Message;
            }
            return DMEEditor.ErrorObject.Message;
        }
    }
}