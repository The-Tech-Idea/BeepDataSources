using System;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Helpers.RDBMSHelpers;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.DataBase.Helpers
{
    internal static class PaginationHelper
    {
        public static string ApplyPaging(string filteredQuery, DataSourceType dbType, int pageNumber, int pageSize)
        {
            return filteredQuery + " " + RDBMSHelper.GetPagingSyntax(dbType, pageNumber, pageSize);
        }
    }
}
