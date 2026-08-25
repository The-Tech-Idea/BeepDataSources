using System;
using System.Collections.Generic;
using System.Data;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>
    /// Oracle data source — inherits BeginTransaction / EndTransaction / Commit / CRUD from RDBSource.
    /// Only dialect-specific FK-toggle SQL and delimiter characters are overridden.
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.RDBMS, DatasourceType = DataSourceType.Oracle)]
    public class OracleDataSource : RDBSource, IDataSource
    {
        public OracleDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per)
            : base(datasourcename, logger, DMEEditor, databasetype, per)
        {
        }

        // Oracle identifiers are case-insensitive when unquoted, so single-quote is the right
        // column delimiter for our DML generator. Bind params use ":" (Oracle bind prefix).
        public override string ColumnDelimiter { get; set; } = "'";
        public override string ParameterDelimiter { get; set; } = ":";

        /// <summary>
        /// ODP.NET binds positionally by default (BindByName = false). RDBSource
        /// generates named placeholders (:p_&lt;Field&gt;) and adds parameters in field
        /// order, so positional binding silently shifts values into the wrong
        /// columns whenever a parameter is skipped. Name-based binding makes a
        /// missing parameter fail loudly (ORA-01008) instead of corrupting data.
        /// </summary>
        protected override void ConfigureCommand(IDbCommand command)
        {
            if (command is Oracle.ManagedDataAccess.Client.OracleCommand oracleCommand)
            {
                oracleCommand.BindByName = true;
            }
        }

        public override string DisableFKConstraints(EntityStructure t1)
        {
            try
            {
                this.ExecuteSql($"ALTER TABLE {t1.EntityName} DISABLE ALL TRIGGERS");
                DMEEditor.ErrorObject.Message = "Successfully Disabled Oracle FK Constraints";
                DMEEditor.ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", "Disabling Oracle FK Constraints: " + ex.Message, DateTime.Now, 0, t1?.EntityName, Errors.Failed);
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = ex.Message;
            }
            return DMEEditor.ErrorObject.Message;
        }

        public override string EnableFKConstraints(EntityStructure t1)
        {
            try
            {
                this.ExecuteSql($"ALTER TABLE {t1.EntityName} ENABLE ALL TRIGGERS");
                DMEEditor.ErrorObject.Message = "Successfully Enabled Oracle FK Constraints";
                DMEEditor.ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", "Enabling Oracle FK Constraints: " + ex.Message, DateTime.Now, 0, t1?.EntityName, Errors.Failed);
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = ex.Message;
            }
            return DMEEditor.ErrorObject.Message;
        }
    }
}