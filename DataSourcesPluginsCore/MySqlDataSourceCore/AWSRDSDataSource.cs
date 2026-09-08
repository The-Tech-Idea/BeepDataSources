using System;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>
    /// AWS RDS (MySQL-compatible) data source — inherits BeginTransaction / EndTransaction / Commit /
    /// CRUD from <see cref="RDBSource"/>. Only dialect-specific FK-toggle SQL and delimiter
    /// characters are overridden, identical to <see cref="MySQLDataSource"/>.
    /// </summary>
    /// <remarks>
    /// This class did not exist. `ConnectionHelper.CreateAWSRDSConfig()` in BeepDM has always
    /// declared `classHandler = "AWSRDSDataSource"` and connects through `MySql.Data` -- this
    /// specific config is for RDS's MySQL engine (RDS itself is a managed-hosting layer over several
    /// distinct engines; a separate config/class pair would be the right home for RDS-for-PostgreSQL
    /// or RDS-for-SqlServer rather than folding those into this one). The dialect is plain MySQL.
    /// </remarks>
    [AddinAttribute(Category = DatasourceCategory.RDBMS, DatasourceType = DataSourceType.AWSRDS)]
    public class AWSRDSDataSource : RDBSource, IDataSource
    {
        public AWSRDSDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per)
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
                DMEEditor.ErrorObject.Message = "Successfully Disabled AWS RDS (MySQL) FK Constraints";
                DMEEditor.ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", "Disabling AWS RDS FK Constraints: " + ex.Message, DateTime.Now, 0, t1?.EntityName, Errors.Failed);
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
                DMEEditor.ErrorObject.Message = "Successfully Enabled AWS RDS (MySQL) FK Constraints";
                DMEEditor.ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", "Enabling AWS RDS FK Constraints: " + ex.Message, DateTime.Now, 0, t1?.EntityName, Errors.Failed);
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = ex.Message;
            }
            return DMEEditor.ErrorObject.Message;
        }
    }
}
