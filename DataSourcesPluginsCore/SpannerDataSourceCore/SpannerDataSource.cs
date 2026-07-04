using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.Cloud.Spanner
{
    /// <summary>
    /// Google Cloud Spanner data source — inherits BeginTransaction / EndTransaction / Commit /
    /// CRUD from RDBSource. FK toggle is a no-op (Spanner enforces referential integrity at the
    /// database level and does not expose a way to disable it per-table).
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.RDBMS, DatasourceType = DataSourceType.Spanner)]
    public class SpannerDataSource : RDBSource, IDataSource
    {
        public SpannerDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per)
            : base(datasourcename, logger, DMEEditor, databasetype, per)
        {
            Category = DatasourceCategory.RDBMS;
        }

        public override string ColumnDelimiter { get; set; } = "'";
        public override string ParameterDelimiter { get; set; } = "@";

        public override string DisableFKConstraints(EntityStructure t1)
        {
            // Google Cloud Spanner does not support disabling FK constraints — it enforces referential
            // integrity at the database level. We surface this as a no-op success so callers can
            // call toggle FKs uniformly across RDBMS engines.
            DMEEditor.ErrorObject.Message = "Google Cloud Spanner does not support disabling FK constraints";
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            return DMEEditor.ErrorObject.Message;
        }

        public override string EnableFKConstraints(EntityStructure t1)
        {
            DMEEditor.ErrorObject.Message = "Google Cloud Spanner does not support enabling FK constraints";
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            return DMEEditor.ErrorObject.Message;
        }
    }
}