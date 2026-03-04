using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DataManagementModels.DriversConfigurations;
using DataManagementModels.Editor;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;

namespace LiteDBDataSourceCore
{
    public partial class LiteDBDataSource
    {
        private void InitDataConnection()
        {
            if (DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == DatasourceName).Any())
            {
                Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == DatasourceName).FirstOrDefault();
            }
            else
            {
                ConnectionDriversConfig driversConfig = DMEEditor.ConfigEditor.DataDriversClasses.FirstOrDefault(p => p.DatasourceType == DataSourceType.LiteDB);
                if (driversConfig != null)
                {
                    Dataconnection.ConnectionProp = new ConnectionProperties
                    {
                        ConnectionName = DatasourceName,
                        ConnectionString = driversConfig.ConnectionString,
                        DriverName = driversConfig.PackageName,
                        DriverVersion = driversConfig.version,
                        DatabaseType = DataSourceType.LiteDB,
                        Category = DatasourceCategory.NOSQL
                    };
                }
            }
            if (string.IsNullOrEmpty(_connectionString) && string.IsNullOrEmpty(DBfilepathandname))
            {
                DBfilepathandname = Path.Combine(DMEEditor.ConfigEditor.Config.DataFilePath, $"{DatasourceName}{Extension}");
            }
            if (!string.IsNullOrEmpty(DBfilepathandname))
            {
                Dataconnection.ConnectionProp.ConnectionString = $"{DBfilepathandname}";
                Dataconnection.ConnectionProp.FilePath = Path.GetDirectoryName(DBfilepathandname);
                Dataconnection.ConnectionProp.FileName = Path.GetFileName(DBfilepathandname);
            }

            Dataconnection.ConnectionProp.Category = DatasourceCategory.NOSQL;
            Dataconnection.ConnectionProp.DatabaseType = DataSourceType.LiteDB;
            _connectionString = Dataconnection.ConnectionProp.ConnectionString;

            // HandleConnectionStringforMongoDB();
            Dataconnection.ConnectionProp.IsLocal = true;
        }

        public bool CreateDB()
        {
            DBfilepathandname = string.Empty;
            InitDataConnection();
            return Openconnection() == ConnectionState.Open;
        }

        public bool CreateDB(bool inMemory)
        {
            InitDataConnection();
            InMemory = inMemory;
            Dataconnection.ConnectionProp.ConnectionString = inMemory
                ? ":memory:"
                : Path.Combine(DMEEditor.ConfigEditor.Config.DataFilePath, $"{DatasourceName}{Extension}");
            _connectionString = Dataconnection.ConnectionProp.ConnectionString;

            return Openconnection() == ConnectionState.Open;
        }

        public bool CreateDB(string filepathandname)
        {
            if (string.IsNullOrEmpty(filepathandname))
            {
                return false;
            }
            DBfilepathandname = filepathandname;
            InitDataConnection();
            return Openconnection() == ConnectionState.Open;
        }

        public bool DeleteDB()
        {
            try
            {
                Closeconnection();
                if (string.IsNullOrWhiteSpace(_connectionString))
                {
                    return false;
                }

                if (File.Exists(_connectionString))
                {
                    File.Delete(_connectionString);
                }
                return true;
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error deleting LiteDB file in {DatasourceName}: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return false;
            }
        }

        public IErrorsInfo DropEntity(string EntityName)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok, Message = "Collection dropped successfully." };

            try
            {
                if (!EnsureConnectionReady(nameof(DropEntity)))
                {
                    retval.Flag = Errors.Failed;
                    retval.Message = "Database connection is not open.";
                    return retval;
                }

                using (var session = new LiteDatabase(_connectionString))
                {
                    bool dropped = session.DropCollection(EntityName);
                    if (!dropped)
                    {
                        retval.Flag = Errors.Failed;
                        retval.Message = "Collection does not exist or could not be dropped.";
                        DMEEditor.AddLogMessage("Beep", "Collection does not exist or could not be dropped.", DateTime.Now, -1, null, Errors.Failed);
                    }
                    else
                    {
                        SyncEntityCaches(EntityName, remove: true);
                    }
                }
            }
            catch (Exception ex)
            {
                retval.Flag = Errors.Failed;
                retval.Message = "Error dropping collection: " + ex.Message;
                DMEEditor.AddLogMessage("Beep", $"error in {nameof(DropEntity)} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return retval;
        }

        public bool CopyDB(string DestDbName, string DesPath)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok, Message = "Get Entity Successfully" };
            bool result = false;
            try
            {
                if (string.IsNullOrWhiteSpace(_connectionString))
                {
                    return false;
                }

                Closeconnection();

                if (!Directory.Exists(DesPath))
                {
                    Directory.CreateDirectory(DesPath);
                }

                if (File.Exists(_connectionString))
                {
                    string targetName = string.IsNullOrWhiteSpace(DestDbName)
                        ? Path.GetFileName(_connectionString)
                        : DestDbName;
                    if (Path.GetExtension(targetName) == string.Empty)
                    {
                        targetName = $"{targetName}{Extension}";
                    }

                    string destinationFile = Path.Combine(DesPath, targetName);
                    File.Copy(_connectionString, destinationFile, true);
                    result = true;
                }
                else
                {
                    result = false;
                }
            }
            catch (Exception ex)
            {
                retval.Flag = Errors.Failed;
                retval.Message = ex.Message;
                DMEEditor.AddLogMessage("Beep", $"error in {nameof(CopyDB)} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return false;
            }

            return result;
        }

        public void HandleConnectionStringforMongoDB()
        {
            if (_connectionString.Contains("}"))
            {
                // Create a dictionary to map placeholders to their respective values
                var replacements = new Dictionary<string, string>
                {
                    { "{Host}", Dataconnection.ConnectionProp.Host },
                    { "{Port}", Dataconnection.ConnectionProp.Port.ToString() },
                    { "{Database}", Dataconnection.ConnectionProp.Database }
                };

                // Optionally add Username and Password to the replacements dictionary
                if (!string.IsNullOrEmpty(Dataconnection.ConnectionProp.UserID) ||
                    !string.IsNullOrEmpty(Dataconnection.ConnectionProp.Password))
                {
                    replacements.Add("{Username}", Dataconnection.ConnectionProp.UserID);
                    replacements.Add("{Password}", Dataconnection.ConnectionProp.Password);
                }

                // Use a regular expression to replace placeholders, ignoring case
                foreach (var replacement in replacements)
                {
                    if (!string.IsNullOrEmpty(replacement.Value))
                    {
                        _connectionString = Regex.Replace(_connectionString, Regex.Escape(replacement.Key), replacement.Value, RegexOptions.IgnoreCase);
                    }
                }

                // Remove any remaining username and password placeholders if they were not replaced
                _connectionString = Regex.Replace(_connectionString, @"\{Username\}:\{Password\}@", string.Empty, RegexOptions.IgnoreCase);
                _connectionString = Regex.Replace(_connectionString, @"\{Username\}:\{Password\}", string.Empty, RegexOptions.IgnoreCase);
            }

            // get database name from connection string if CurrentDatabase is not set
            //if (string.IsNullOrEmpty(CurrentDatabase))
            //{
            //    var match = Regex.Match(_connectionString, @"\/(?<database>[^\/\?]+)(\?|$)");
            //    if (match.Success)
            //    {
            //        CurrentDatabase = match.Groups["database"].Value;
            //    }
            //}
        }
    }
}
