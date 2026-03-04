using System.Data;
using System.Data.SQLite;
using TheTechIdea.Beep.Vis;

namespace TheTechIdea.Beep.DataBase
{
    public partial class SQLiteDataSource
    {
        private bool EnsureConnectionProp()
        {
            if (Dataconnection == null)
            {
                Dataconnection = new RDBDataConnection(DMEEditor);
            }

            Dataconnection.ConnectionProp ??= new ConnectionProperties();
            Dataconnection.ConnectionProp.DatabaseType = DataSourceType.SqlLite;
            Dataconnection.ConnectionProp.Category = DatasourceCategory.RDBMS;
            Dataconnection.ConnectionProp.IsDatabase = true;
            Dataconnection.ConnectionProp.IsLocal = true;
            return true;
        }

        private string ResolveDatabaseFilePath()
        {
            EnsureConnectionProp();
            var cp = Dataconnection.ConnectionProp;

            if (!string.IsNullOrWhiteSpace(cp.FilePath) && !string.IsNullOrWhiteSpace(cp.FileName))
            {
                return Path.Combine(cp.FilePath, cp.FileName);
            }

            if (!string.IsNullOrWhiteSpace(cp.ConnectionString))
            {
                var marker = "Data Source=";
                var idx = cp.ConnectionString.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    var after = cp.ConnectionString[(idx + marker.Length)..];
                    var semi = after.IndexOf(';');
                    return semi >= 0 ? after[..semi] : after;
                }
            }

            return string.Empty;
        }

        private void ApplyDatabaseFilePath(string filepathandname)
        {
            EnsureConnectionProp();
            var cp = Dataconnection.ConnectionProp;
            cp.FilePath = Path.GetDirectoryName(filepathandname);
            cp.FileName = Path.GetFileName(filepathandname);
            cp.ConnectionString = $"Data Source={filepathandname};Version=3;New=True;";
            cp.IsFile = true;
        }

        private bool EnsureDatabaseDirectory(string filePath)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (string.IsNullOrWhiteSpace(dir))
            {
                return false;
            }

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            return true;
        }

        private IErrorsInfo SetError(Errors flag, string message, Exception ex = null)
        {
            DMEEditor.ErrorObject ??= new ErrorsInfo();
            DMEEditor.ErrorObject.Flag = flag;
            DMEEditor.ErrorObject.Message = message;
            DMEEditor.ErrorObject.Ex = ex;
            ErrorObject = DMEEditor.ErrorObject;
            return ErrorObject;
        }

        public override string DisableFKConstraints(EntityStructure t1)
        {
            try
            {
                ExecuteSql("PRAGMA foreign_keys = OFF;");
                ExecuteSql("PRAGMA ignore_check_constraints = 1;");
                SetError(Errors.Ok, "Successfully disabled SQLite constraints.");
            }
            catch (Exception ex)
            {
                SetError(Errors.Failed, "Failed to disable SQLite constraints.", ex);
                DMEEditor.AddLogMessage("Beep", "Disabling SQLite constraints failed: " + ex.Message, DateTime.Now, 0, t1?.EntityName, Errors.Failed);
            }

            return DMEEditor.ErrorObject.Message;
        }

        public override string EnableFKConstraints(EntityStructure t1)
        {
            try
            {
                ExecuteSql("PRAGMA ignore_check_constraints = 0;");
                ExecuteSql("PRAGMA foreign_keys = ON;");
                SetError(Errors.Ok, "Successfully enabled SQLite constraints.");
            }
            catch (Exception ex)
            {
                SetError(Errors.Failed, "Failed to enable SQLite constraints.", ex);
                DMEEditor.AddLogMessage("Beep", "Enabling SQLite constraints failed: " + ex.Message, DateTime.Now, 0, t1?.EntityName, Errors.Failed);
            }

            return DMEEditor.ErrorObject.Message;
        }

        private List<FkListforSQLlite> GetSqlLiteTableKeysAsync(string tablename)
        {
            return base.GetData<FkListforSQLlite>($"PRAGMA foreign_key_check({tablename});");
        }

        private void enablefk()
        {
            ExecuteSql("PRAGMA foreign_keys = ON;");
        }
    }
}
