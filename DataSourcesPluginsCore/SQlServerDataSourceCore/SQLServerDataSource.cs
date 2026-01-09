using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.DataBase
{
    [AddinAttribute(Category = DatasourceCategory.RDBMS, DatasourceType =  DataSourceType.SqlServer)]
    public class SQLServerDataSource : RDBSource, IDataSource
    {
        
        public SQLServerDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per) : base(datasourcename, logger, DMEEditor, databasetype, per)
        {
            //DateTime.MinValue =Convert.ToDateTime("01/01/1900 12:00:00 AM");
            //DateTime.MaxValue = Convert.ToDateTime("01/01/1900 12:00:00 AM");
        }
        public virtual IErrorsInfo BeginTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Beep", $"Error in Begin Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        public virtual IErrorsInfo EndTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Beep", $"Error in end Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        public virtual IErrorsInfo Commit(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Beep", $"Error in Commit Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public override string DisableFKConstraints(EntityStructure t1)
        {
            try
            {
                this.ExecuteSql($"ALTER TABLE {t1.EntityName} NOCHECK CONSTRAINT ALL");
                DMEEditor.ErrorObject.Message = "Successfully Disabled SQL Server FK Constraints";
                DMEEditor.ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", "Disabling SQL Server FK Constraints: " + ex.Message, DateTime.Now, 0, t1.EntityName, Errors.Failed);
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = ex.Message;
            }
            return DMEEditor.ErrorObject.Message;
        }
        public override string EnableFKConstraints(EntityStructure t1)
        {
            try
            {
                this.ExecuteSql($"ALTER TABLE {t1.EntityName} WITH CHECK CHECK CONSTRAINT ALL");
                DMEEditor.ErrorObject.Message = "Successfully Enabled SQL Server FK Constraints";
                DMEEditor.ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", "Enabling SQL Server FK Constraints: " + ex.Message, DateTime.Now, 0, t1.EntityName, Errors.Failed);
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = ex.Message;
            }
            return DMEEditor.ErrorObject.Message;
        }
        private string PagedQuery(string originalquery, List<AppFilter> Filter)
        {
            if (Filter == null)
            {
                return originalquery;
            }

            AppFilter pagesizefilter = Filter.Where(o => o.FieldName.Equals("Pagesize", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            AppFilter pagenumberfilter = Filter.Where(o => o.FieldName.Equals("pagenumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            
            if (pagesizefilter == null || pagenumberfilter == null)
            {
                return originalquery;
            }

            int pagesize = Convert.ToInt32(pagesizefilter.FilterValue);
            int pagenumber = Convert.ToInt32(pagenumberfilter.FilterValue);
            
            if (pagesize <= 0 || pagenumber <= 0)
            {
                return originalquery;
            }

            // SQL Server 2012+ OFFSET/FETCH syntax (preferred)
            // If the query already has ORDER BY, use it; otherwise add a default ORDER BY
            string orderByClause = originalquery.ToUpper().Contains("ORDER BY") ? "" : " ORDER BY (SELECT NULL)";
            int offset = (pagenumber - 1) * pagesize;
            
            string pagedquery = $"{originalquery}{orderByClause} OFFSET {offset} ROWS FETCH NEXT {pagesize} ROWS ONLY";
            
            return pagedquery;
        }

      
     
    }
}
