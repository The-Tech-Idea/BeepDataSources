using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>
    /// Firebird data source (networked) — inherits BeginTransaction / EndTransaction / Commit /
    /// CRUD from RDBSource. No dialect-specific overrides; the base class handles Firebird's
    /// ADO.NET connection model.
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.RDBMS, DatasourceType = DataSourceType.FireBird)]
    public class FireBirdDataSource : RDBSource, IDataSource
    {
        public FireBirdDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
            : base(datasourcename, logger, pDMEEditor, databasetype, per)
        {
        }
    }
}