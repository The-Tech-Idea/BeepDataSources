using System;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>
    /// Azure SQL Database data source — inherits BeginTransaction / EndTransaction / Commit / CRUD /
    /// paging / type mapping from <see cref="RDBSource"/>. Only dialect-specific FK-toggle SQL is
    /// overridden, identical to <see cref="SQLServerDataSource"/>.
    /// </summary>
    /// <remarks>
    /// This class did not exist. `ConnectionHelper.CreateAzureSQLConfig()` in BeepDM has always
    /// declared `classHandler = "AzureSQLDataSource"` and every dialect decision this base class
    /// makes already treats <see cref="DataSourceType.AzureSQL"/> as a first-class case alongside
    /// <see cref="DataSourceType.SqlServer"/> — `QuoteIdentifier`'s bracket-quoting switch,
    /// `NormalizeDbTypeForProvider`, `SupportsFeature(..., DatabaseFeature.MultiRowInsert)`, and
    /// `RDBMSHelper.GetPagingSyntax` all already branch on it — yet no concrete class existed to
    /// register it. Azure SQL Database speaks the same T-SQL dialect as SQL Server (it is the same
    /// engine, managed), so this mirrors <see cref="SQLServerDataSource"/>'s FK-toggle SQL exactly.
    /// </remarks>
    [AddinAttribute(Category = DatasourceCategory.RDBMS, DatasourceType = DataSourceType.AzureSQL)]
    public class AzureSQLDataSource : RDBSource, IDataSource
    {
        public AzureSQLDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per)
            : base(datasourcename, logger, DMEEditor, databasetype, per)
        {
        }

        public override string DisableFKConstraints(EntityStructure t1)
        {
            try
            {
                this.ExecuteSql($"ALTER TABLE {t1.EntityName} NOCHECK CONSTRAINT ALL");
                DMEEditor.ErrorObject.Message = "Successfully Disabled Azure SQL FK Constraints";
                DMEEditor.ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", "Disabling Azure SQL FK Constraints: " + ex.Message, DateTime.Now, 0, t1?.EntityName, Errors.Failed);
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
                DMEEditor.ErrorObject.Message = "Successfully Enabled Azure SQL FK Constraints";
                DMEEditor.ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", "Enabling Azure SQL FK Constraints: " + ex.Message, DateTime.Now, 0, t1?.EntityName, Errors.Failed);
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = ex.Message;
            }
            return DMEEditor.ErrorObject.Message;
        }
    }
}
