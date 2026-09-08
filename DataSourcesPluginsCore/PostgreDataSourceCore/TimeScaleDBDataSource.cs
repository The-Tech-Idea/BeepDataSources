using System;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>
    /// TimescaleDB data source — inherits BeginTransaction / EndTransaction / Commit / InsertEntity /
    /// UpdateEntity / DeleteEntity from <see cref="RDBSource"/>. Only dialect-specific FK-toggle SQL
    /// is overridden, identical to <see cref="PostgreDataSource"/>.
    /// </summary>
    /// <remarks>
    /// This class did not exist. `ConnectionHelper.CreateTimeScaleConfig()` in BeepDM has always
    /// declared `classHandler = "TimeScaleDBDataSource"` and connects through `Npgsql` — the same
    /// ADO.NET provider PostgreSQL itself uses — because TimescaleDB literally is a PostgreSQL
    /// extension (installed into an ordinary PostgreSQL server) rather than a separate engine, so
    /// its DDL/DML dialect is PostgreSQL's own. Mirrors <see cref="PostgreDataSource"/>'s FK-toggle
    /// SQL exactly.
    /// </remarks>
    [AddinAttribute(Category = DatasourceCategory.RDBMS, DatasourceType = DataSourceType.TimeScale)]
    public class TimeScaleDBDataSource : RDBSource, IDataSource
    {
        public TimeScaleDBDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per)
            : base(datasourcename, logger, DMEEditor, databasetype, per)
        {
        }

        public override string DisableFKConstraints(EntityStructure t1)
        {
            try
            {
                this.ExecuteSql($"ALTER TABLE {t1.EntityName} DISABLE TRIGGER ALL");
                DMEEditor.ErrorObject.Message = "Successfully Disabled TimescaleDB FK Constraints";
                DMEEditor.ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", "Disabling TimescaleDB FK Constraints: " + ex.Message, DateTime.Now, 0, t1?.EntityName, Errors.Failed);
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
                DMEEditor.ErrorObject.Message = "Successfully Enabled TimescaleDB FK Constraints";
                DMEEditor.ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", "Enabling TimescaleDB FK Constraints: " + ex.Message, DateTime.Now, 0, t1?.EntityName, Errors.Failed);
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = ex.Message;
            }
            return DMEEditor.ErrorObject.Message;
        }
    }
}
