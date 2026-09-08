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
using TheTechIdea.Beep.DriversConfigurations;

namespace TheTechIdea.Beep.DataBase
{
    [AddinAttribute(Category = DatasourceCategory.RDBMS, DatasourceType = DataSourceType.Hana)]
    public class HanaDataSource : RDBSource, IDataSource
    {
        public HanaDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per) 
            : base(datasourcename, logger, DMEEditor, databasetype, per)
        {
            ColumnDelimiter = "'";
            ParameterDelimiter = "?";
        }

        public override string ColumnDelimiter { get; set; } = "'";
        public override string ParameterDelimiter { get; set; } = "?";

        // BeginTransaction / EndTransaction / Commit are intentionally NOT declared here.
        // SAP HANA transactions are handled by RDBSource base class -- that was always the
        // stated intent (see the comments these replaced), but the three methods previously
        // written here as `public virtual` (not `override`) HID RDBSource's real ADO.NET
        // transaction plumbing instead of deferring to it. Because this class re-declares
        // IDataSource in its own inheritance list, C# rebinds the IDataSource interface slot
        // to the most-derived member matching the signature -- the hiding stub, not the base
        // override -- so every caller holding this datasource as IDataSource (the normal way
        // the engine consumes a plugin) got a no-op that always reported Ok: BeginTransaction
        // never opened a real transaction, Commit had nothing to commit, and EndTransaction
        // (rollback) had nothing to roll back -- so writes made "inside" a transaction were
        // never atomic and a rollback never undid them. Removing the hiding methods restores
        // RDBSource's real, tested BeginTransaction/Commit/EndTransaction to the IDataSource slot.

        public override string DisableFKConstraints(EntityStructure t1)
        {
            try
            {
                // SAP HANA: Disable referential integrity checks
                this.ExecuteSql($"ALTER TABLE {t1.EntityName} DISABLE REFERENTIAL INTEGRITY");
                DMEEditor.ErrorObject.Message = "Successfully Disabled SAP HANA FK Constraints";
                DMEEditor.ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", "Disabling SAP HANA FK Constraints: " + ex.Message, DateTime.Now, 0, t1.EntityName, Errors.Failed);
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = ex.Message;
            }
            return DMEEditor.ErrorObject.Message;
        }

        public override string EnableFKConstraints(EntityStructure t1)
        {
            try
            {
                // SAP HANA: Enable referential integrity checks
                this.ExecuteSql($"ALTER TABLE {t1.EntityName} ENABLE REFERENTIAL INTEGRITY");
                DMEEditor.ErrorObject.Message = "Successfully Enabled SAP HANA FK Constraints";
                DMEEditor.ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", "Enabling SAP HANA FK Constraints: " + ex.Message, DateTime.Now, 0, t1.EntityName, Errors.Failed);
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = ex.Message;
            }
            return DMEEditor.ErrorObject.Message;
        }
    }
}