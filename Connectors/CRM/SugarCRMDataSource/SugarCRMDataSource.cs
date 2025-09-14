using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Http;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.WebAPI;


namespace TheTechIdea.Beep.Connectors.SugarCRM
{
    /// <summary>
    /// SugarCRM Data Source implementation using SugarCRM REST API v11
    /// </summary>
    public class SugarCRMDataSource : WebAPIDataSource
    {
        public SugarCRMDataSource(string datasourcename, IDMLogger logger, IDMEEditor dmeEditor, DataSourceType databasetype, IErrorsInfo errorObject) : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
        {
        }
    }
}
