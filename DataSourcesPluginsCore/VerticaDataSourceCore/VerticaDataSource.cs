using System;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>
    /// Vertica data source — inherits BeginTransaction / EndTransaction / Commit / CRUD / paging /
    /// type mapping from <see cref="RDBSource"/>.
    /// </summary>
    /// <remarks>
    /// This class did not exist, and neither did a project for it. `ConnectionHelper.CreateVerticaConfig()`
    /// in BeepDM has always declared `classHandler = "VerticaDataSource"` and connects through
    /// `Vertica.Data` — no reference to that package is needed here at compile time; the actual
    /// provider types are resolved from `ConnectionDriversConfig` by name at runtime.
    ///
    /// FK toggling is reported as an honest no-op rather than emitting SQL Server/MySQL-style
    /// `ALTER TABLE ... [NO]CHECK CONSTRAINT`: Vertica, an MPP analytical engine, does not enforce
    /// foreign keys at write time at all -- they exist only as optimizer hints for the query
    /// planner -- so there is no server-side "checking" to disable or re-enable.
    /// </remarks>
    [AddinAttribute(Category = DatasourceCategory.RDBMS, DatasourceType = DataSourceType.Vertica)]
    public class VerticaDataSource : RDBSource, IDataSource
    {
        public VerticaDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per)
            : base(datasourcename, logger, DMEEditor, databasetype, per)
        {
        }

        public override string DisableFKConstraints(EntityStructure t1)
        {
            const string message = "Vertica does not enforce foreign keys at write time (they are optimizer hints only); there is nothing to disable.";
            DMEEditor.ErrorObject.Message = message;
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            return message;
        }

        public override string EnableFKConstraints(EntityStructure t1)
        {
            const string message = "Vertica does not enforce foreign keys at write time (they are optimizer hints only); there is nothing to re-enable.";
            DMEEditor.ErrorObject.Message = message;
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            return message;
        }
    }
}
