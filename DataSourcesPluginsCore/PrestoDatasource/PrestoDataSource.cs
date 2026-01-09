using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.WebAPI;
using System.Text;
using System.Data.Common;

namespace TheTechIdea.Beep.Cloud.Presto
{
    [AddinAttribute(Category = DatasourceCategory.CLOUD, DatasourceType = DataSourceType.Presto)]
    public class PrestoDataSource : RDBSource, IDataSource
    {
        public PrestoDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per) 
            : base(datasourcename, logger, DMEEditor, databasetype, per)
        {
            ColumnDelimiter = "'";
            ParameterDelimiter = "?";
        }

        public override string ColumnDelimiter { get; set; } = "'";
        public override string ParameterDelimiter { get; set; } = "?";

        public virtual IErrorsInfo BeginTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                // Presto transactions are handled by RDBSource base class
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error in Begin Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
            }
            return DMEEditor.ErrorObject;
        }

        public virtual IErrorsInfo EndTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                // Presto transactions are handled by RDBSource base class
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error in End Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
            }
            return DMEEditor.ErrorObject;
        }

        public virtual IErrorsInfo Commit(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                // Presto transactions are handled by RDBSource base class
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error in Commit Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
            }
            return DMEEditor.ErrorObject;
        }

        public override string DisableFKConstraints(EntityStructure t1)
        {
            try
            {
                // Presto doesn't support disabling FK constraints like traditional SQL
                // Presto is primarily a query engine, not a transactional database
                DMEEditor.ErrorObject.Message = "Presto does not support disabling FK constraints";
                DMEEditor.ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", "Disabling Presto FK Constraints: " + ex.Message, DateTime.Now, 0, t1.EntityName, Errors.Failed);
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = ex.Message;
            }
            return DMEEditor.ErrorObject.Message;
        }

        public override string EnableFKConstraints(EntityStructure t1)
        {
            try
            {
                // Presto doesn't support enabling FK constraints like traditional SQL
                // Presto is primarily a query engine, not a transactional database
                DMEEditor.ErrorObject.Message = "Presto does not support enabling FK constraints";
                DMEEditor.ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", "Enabling Presto FK Constraints: " + ex.Message, DateTime.Now, 0, t1.EntityName, Errors.Failed);
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = ex.Message;
            }
            return DMEEditor.ErrorObject.Message;
        }
    }
}