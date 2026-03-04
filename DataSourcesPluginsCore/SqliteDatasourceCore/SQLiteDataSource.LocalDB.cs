using System.Data.SQLite;

namespace TheTechIdea.Beep.DataBase
{
    public partial class SQLiteDataSource
    {
        public bool CopyDB(string DestDbName, string DesPath)
        {
            try
            {
                Closeconnection();

                var sourceFile = ResolveDatabaseFilePath();
                if (string.IsNullOrWhiteSpace(sourceFile) || !File.Exists(sourceFile))
                {
                    SetError(Errors.Failed, "SQLite source database file does not exist.");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(DesPath))
                {
                    SetError(Errors.Failed, "Destination path cannot be empty.");
                    return false;
                }

                if (!Directory.Exists(DesPath))
                {
                    Directory.CreateDirectory(DesPath);
                }

                var targetName = string.IsNullOrWhiteSpace(DestDbName) ? Path.GetFileName(sourceFile) : DestDbName;
                if (!Path.HasExtension(targetName))
                {
                    targetName += Extension;
                }

                File.Copy(sourceFile, Path.Combine(DesPath, targetName), true);
                SetError(Errors.Ok, "SQLite database copied successfully.");
                DMEEditor.AddLogMessage("Beep", "Copied SQLite database.", DateTime.Now, 0, null, Errors.Ok);
                return true;
            }
            catch (Exception ex)
            {
                SetError(Errors.Failed, "Could not copy SQLite database.", ex);
                DMEEditor.AddLogMessage("Beep", "Could not copy SQLite database: " + ex.Message, DateTime.Now, -1, null, Errors.Failed);
                return false;
            }
        }

        public bool CreateDB()
        {
            EnsureConnectionProp();
            var cp = Dataconnection.ConnectionProp;
            if (!Path.HasExtension(cp.FileName))
            {
                cp.FileName = cp.FileName + Extension;
            }

            var fullPath = ResolveDatabaseFilePath();
            return CreateDB(fullPath);
        }

        public bool CreateDB(bool inMemory)
        {
            EnsureConnectionProp();
            Dataconnection.ConnectionProp.IsInMemory = inMemory;
            Dataconnection.InMemory = inMemory;

            if (inMemory)
            {
                OpenDatabaseInMemory(string.IsNullOrWhiteSpace(DatasourceName) ? "InMemorySqlite" : DatasourceName);
                return DMEEditor.ErrorObject.Flag == Errors.Ok;
            }

            return CreateDB();
        }

        public bool CreateDBDefaultDir(string filename)
        {
            try
            {
                var dirpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TheTechIdea", "Beep", "DatabaseFiles");
                if (!Directory.Exists(dirpath))
                {
                    Directory.CreateDirectory(dirpath);
                }

                var filepathandname = Path.Combine(dirpath, filename);
                return CreateDB(filepathandname);
            }
            catch (Exception ex)
            {
                SetError(Errors.Failed, "Could not create SQLite database in default directory.", ex);
                return false;
            }
        }

        public bool CreateDB(string filepathandname)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filepathandname))
                {
                    SetError(Errors.Failed, "Database file path cannot be empty.");
                    return false;
                }

                if (!Path.HasExtension(filepathandname))
                {
                    filepathandname += Extension;
                }

                EnsureDatabaseDirectory(filepathandname);
                ApplyDatabaseFilePath(filepathandname);

                if (!File.Exists(filepathandname))
                {
                    SQLiteConnection.CreateFile(filepathandname);
                    DMEEditor.AddLogMessage("Beep", "Created SQLite database.", DateTime.Now, 0, null, Errors.Ok);
                }
                else
                {
                    DMEEditor.AddLogMessage("Beep", "SQLite database already exists.", DateTime.Now, 0, null, Errors.Ok);
                }

                SetError(Errors.Ok, "SQLite database is ready.");
                return true;
            }
            catch (Exception ex)
            {
                SetError(Errors.Failed, "Could not create SQLite database.", ex);
                DMEEditor.AddLogMessage("Beep", "Could not create SQLite database: " + ex.Message, DateTime.Now, -1, null, Errors.Failed);
                return false;
            }
        }

        public bool DeleteDB()
        {
            try
            {
                Closeconnection();

                var filePath = ResolveDatabaseFilePath();
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    SetError(Errors.Failed, "SQLite database file was not found for deletion.");
                    return false;
                }

                File.Delete(filePath);
                SetError(Errors.Ok, "SQLite database deleted.");
                DMEEditor.AddLogMessage("Beep", "Deleted SQLite database.", DateTime.Now, 0, null, Errors.Ok);
                return true;
            }
            catch (Exception ex)
            {
                SetError(Errors.Failed, "Could not delete SQLite database.", ex);
                DMEEditor.AddLogMessage("Beep", "Could not delete SQLite database: " + ex.Message, DateTime.Now, -1, null, Errors.Failed);
                return false;
            }
        }

        public IErrorsInfo DropEntity(string EntityName)
        {
            try
            {
                var cmdText = $"drop table '{EntityName}'";
                DMEEditor.ErrorObject = base.ExecuteSql(cmdText);

                if (!base.CheckEntityExist(EntityName))
                {
                    DMEEditor.AddLogMessage("Beep", $"Dropped entity {EntityName}", DateTime.Now, 0, null, Errors.Ok);
                }
                else
                {
                    DMEEditor.AddLogMessage("Beep", $"Could not drop entity {EntityName}", DateTime.Now, 0, null, Errors.Failed);
                }
            }
            catch (Exception ex)
            {
                SetError(Errors.Failed, $"Error dropping entity {EntityName}.", ex);
                DMEEditor.AddLogMessage("Beep", $"Error dropping entity {EntityName}: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }

            return DMEEditor.ErrorObject;
        }
    }
}
