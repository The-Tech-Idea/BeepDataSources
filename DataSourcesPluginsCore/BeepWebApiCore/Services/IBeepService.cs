using KocSharedLib.KocClasses;
using System.Collections.Generic;
using TheTechIdea;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.Report;
using TheTechIdea.Util;

namespace KocWebApi.Services
{
    public interface IBeepService
    {
        IDMEEditor DMEEditor { get; set; }
        IDataSource src { get; set; }

        bool AddKocConnection(string pConnectionName, DataSourceType dataSourceType);
        IDataSource OpenKocConnection();
        List<WELL_LATEST_DATA> GetWELLs(string querystr, List<ReportFilter> f = null);
        List<string> GetWellNamesList(string querystr, List<ReportFilter> f = null);
    }
}