using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>
    /// VistaDB data source — inherits BeginTransaction / EndTransaction / Commit / CRUD from
    /// <see cref="RDBSource"/>. No dialect-specific overrides.
    /// </summary>
    /// <remarks>
    /// This class did not exist, matching <see cref="SQLCompactDataSource"/>'s situation exactly:
    /// `ConnectionHelper.CreateVistaDBConfig()` in BeepDM has always declared `classHandler =
    /// "VistaDBDataSource"`, naming a class that was never written.
    ///
    /// Unlike SQL Compact, this driver's FK-toggle SQL is deliberately left unoverridden rather than
    /// guessed: VistaDB was marketed as broadly SQL-Server-compatible, but it is a much simpler
    /// embedded engine and whether it accepts SQL Server's `ALTER TABLE ... [NO]CHECK CONSTRAINT ALL`
    /// administrative syntax specifically was not something this could be verified against --
    /// `RDBSource`'s own default (`../BeepDM/.../RDBSource.cs`'s base `DisableFKConstraints`/
    /// `EnableFKConstraints`, whatever it is) is safer than a guess in either direction here.
    ///
    /// The `VistaDB` NuGet package itself was not found published under that name during this work
    /// (queried at build time; may simply need the correct package id) and VistaDB the product has
    /// had no vendor activity in years, so — like `SQLCompactDataSource` — this class exists and
    /// registers correctly, but connecting through it depends on a provider package this repository
    /// cannot currently confirm is resolvable.
    /// </remarks>
    [AddinAttribute(Category = DatasourceCategory.RDBMS, DatasourceType = DataSourceType.VistaDB)]
    public class VistaDBDataSource : RDBSource, IDataSource
    {
        public VistaDBDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per)
            : base(datasourcename, logger, DMEEditor, databasetype, per)
        {
        }
    }
}
