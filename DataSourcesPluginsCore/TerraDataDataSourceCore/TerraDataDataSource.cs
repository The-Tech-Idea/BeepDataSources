using System;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>
    /// Teradata data source — inherits BeginTransaction / EndTransaction / Commit / CRUD / paging /
    /// type mapping from <see cref="RDBSource"/>.
    /// </summary>
    /// <remarks>
    /// This class did not exist, and neither did a project for it. `ConnectionHelper.CreateTerraDataConfig()`
    /// in BeepDM has always declared `classHandler = "TerraDataDataSource"` and connects through
    /// `Teradata.Client.Provider` — no reference to that package is needed here at compile time; the
    /// actual provider types are resolved from `ConnectionDriversConfig` by name at runtime.
    ///
    /// FK toggling is reported as an honest no-op rather than emitting SQL Server/MySQL-style
    /// `ALTER TABLE ... [NO]CHECK CONSTRAINT`: Teradata's MPP architecture makes hard, enforced
    /// referential integrity checks expensive at scale, so REFERENCES constraints are conventionally
    /// declared `WITH NO CHECK OPTION` (soft/unenforced, informational for the optimizer) rather than
    /// enforced the way this base class's other, enforcing engines are -- there is no per-table
    /// "checking" toggle to turn off and back on the way SQL Server's or MySQL's is.
    /// </remarks>
    [AddinAttribute(Category = DatasourceCategory.RDBMS, DatasourceType = DataSourceType.TerraData)]
    public class TerraDataDataSource : RDBSource, IDataSource
    {
        public TerraDataDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per)
            : base(datasourcename, logger, DMEEditor, databasetype, per)
        {
        }

        public override string DisableFKConstraints(EntityStructure t1)
        {
            const string message = "Teradata foreign keys are conventionally declared WITH NO CHECK OPTION (unenforced); there is no server-side checking to disable.";
            DMEEditor.ErrorObject.Message = message;
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            return message;
        }

        public override string EnableFKConstraints(EntityStructure t1)
        {
            const string message = "Teradata foreign keys are conventionally declared WITH NO CHECK OPTION (unenforced); there is no server-side checking to re-enable.";
            DMEEditor.ErrorObject.Message = message;
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            return message;
        }
    }
}
