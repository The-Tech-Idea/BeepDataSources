
using FirebirdSql.Data.FirebirdClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TheTechIdea;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Workflow;

using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace  TheTechIdea.Beep.DataBase
{
    [AddinAttribute(Category = DatasourceCategory.RDBMS, DatasourceType =  DataSourceType.FireBird)]
    public class FireBirdDataSource : RDBSource
    {
        public FireBirdDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per) : base(datasourcename, logger, pDMEEditor, databasetype, per)
        {

        }
      

    }
}
