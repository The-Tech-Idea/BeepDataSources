using FirebirdSql.Data.FirebirdClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>
    /// Embedded Firebird data source (local .fdb file). Adds ILocalDB surface (CreateDB / DeleteDB /
    /// CopyDB / DropEntity) on top of the inherited RDBSource transaction + CRUD behaviour.
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.RDBMS, DatasourceType = DataSourceType.FireBird)]
    public class FireBirdEmbeddedDataSource : RDBSource, ILocalDB, IDataSource
    {
        public bool CanCreateLocal { get; set; } = true;
        public bool InMemory { get; set; } = false;
        public string Extension { get; set; } = ".fdb";

        public FireBirdEmbeddedDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
            : base(datasourcename, logger, pDMEEditor, databasetype, per)
        {
            Dataconnection.ConnectionProp.DatabaseType = DataSourceType.FireBird;
        }

        // (BeginTransaction / EndTransaction / Commit are inherited from RDBSource; no override needed.)

        public bool CopyDB(string DestDbName, string DesPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(DestDbName) || string.IsNullOrWhiteSpace(DesPath)) return false;
                if (!Directory.Exists(DesPath)) Directory.CreateDirectory(DesPath);
                var src = base.Dataconnection.ConnectionProp.ConnectionString;
                if (!File.Exists(src)) return false;
                File.Copy(src, Path.Combine(DesPath, DestDbName), overwrite: true);
                DMEEditor.AddLogMessage("Success", "Copied Firebird database.", DateTime.Now, 0, null, Errors.Ok);
                return true;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", "Could not copy Firebird database: " + ex.Message, DateTime.Now, -1, null, Errors.Failed);
                return false;
            }
        }

        public void CreateFBDatabase(string host, string fileName, string user, string password, int pageSize, bool forcedWrites, bool overwrite)
        {
            try
            {
                FbConnectionStringBuilder csb = new FbConnectionStringBuilder
                {
                    Database = fileName,
                    DataSource = host,
                    UserID = user,
                    Password = password,
                    ServerType = FbServerType.Embedded
                };
                base.Dataconnection.ConnectionProp.Database = fileName;
                base.Dataconnection.ConnectionProp.Host = host;
                base.Dataconnection.ConnectionProp.Port = 3050;
                base.Dataconnection.ConnectionProp.Password = password;
                base.Dataconnection.ConnectionProp.UserID = user;
                FbConnection.CreateDatabase(csb.ConnectionString, pageSize, forcedWrites, overwrite);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Could not create embedded Firebird database '{fileName}': {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
        }

        public bool CreateDB()
        {
            try
            {
                var cp = base.Dataconnection.ConnectionProp;
                if (!Path.HasExtension(cp.FileName)) cp.FileName += ".fdb";
                // Use temp dir if in-memory is requested, else the configured FilePath.
                string fullPath = ResolveFilePath();
                if (!File.Exists(fullPath))
                {
                    CreateFBDatabase("localhost", fullPath, cp.UserID ?? "SYSDBA", cp.Password ?? "masterkey", 4096, true, true);
                }
                DMEEditor.AddLogMessage("Success", "Created Firebird database.", DateTime.Now, 0, null, Errors.Ok);
                return true;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", "Could not create embedded Firebird database: " + ex.Message, DateTime.Now, -1, null, Errors.Failed);
                return false;
            }
        }

        public bool DeleteDB()
        {
            try
            {
                var path = ResolveFilePath();
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;
                File.Delete(path);
                DMEEditor.AddLogMessage("Success", "Deleted Firebird database.", DateTime.Now, 0, null, Errors.Ok);
                return true;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", "Could not delete Firebird database: " + ex.Message, DateTime.Now, -1, null, Errors.Failed);
                return false;
            }
        }

        public IErrorsInfo DropEntity(string EntityName)
        {
            try
            {
                string cmdText = $"DROP TABLE \"{EntityName}\"";
                DMEEditor.ErrorObject = base.ExecuteSql(cmdText);
                if (!base.CheckEntityExist(EntityName))
                    DMEEditor.AddLogMessage("Success", $"Dropped entity {EntityName}.", DateTime.Now, 0, null, Errors.Ok);
                else
                    DMEEditor.AddLogMessage("Fail", $"Could not drop entity {EntityName}.", DateTime.Now, 0, null, Errors.Failed);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error dropping entity {EntityName}: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        public IErrorsInfo CloseConnection()
        {
            try
            {
                if (base.RDBMSConnection?.DbConn != null && base.RDBMSConnection.DbConn.State == ConnectionState.Open)
                {
                    base.RDBMSConnection.DbConn.Close();
                }
                DMEEditor.AddLogMessage("Success", "Closed connection to Firebird database.", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", "Error closing Firebird connection: " + ex.Message, DateTime.Now, -1, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        public bool CreateDB(bool inMemory)
        {
            try
            {
                InMemory = inMemory;
                var cp = base.Dataconnection.ConnectionProp;
                if (!Path.HasExtension(cp.FileName)) cp.FileName += ".fdb";
                // In-memory mode: Firebird doesn't have a true in-memory engine like SQLite, so
                // route to a temp file. The user still gets a clean isolated DB.
                string fullPath = inMemory
                    ? Path.Combine(Path.GetTempPath(), cp.FileName)
                    : Path.Combine(string.IsNullOrEmpty(cp.FilePath) ? Directory.GetCurrentDirectory() : cp.FilePath, cp.FileName);

                if (!File.Exists(fullPath))
                {
                    CreateFBDatabase("localhost", fullPath,
                        cp.UserID ?? "SYSDBA", cp.Password ?? "masterkey", 4096, true, true);
                }
                if (inMemory) cp.FilePath = Path.GetDirectoryName(fullPath);
                DMEEditor.AddLogMessage("Success", "Created Firebird database.", DateTime.Now, 0, null, Errors.Ok);
                return true;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", "Could not create embedded Firebird database: " + ex.Message, DateTime.Now, -1, null, Errors.Failed);
                return false;
            }
        }

        public bool CreateDB(string filepathandname)
        {
            try
            {
                if (string.IsNullOrEmpty(filepathandname)) return false;
                if (!Path.HasExtension(filepathandname)) filepathandname += Extension;

                var cp = base.Dataconnection.ConnectionProp;
                if (!File.Exists(filepathandname))
                {
                    CreateFBDatabase("localhost", filepathandname,
                        cp.UserID ?? "SYSDBA", cp.Password ?? "masterkey", 4096, true, true);
                }
                cp.FilePath = Path.GetDirectoryName(filepathandname);
                cp.FileName = Path.GetFileName(filepathandname);

                DMEEditor.AddLogMessage("Success", "Created Firebird database.", DateTime.Now, 0, null, Errors.Ok);
                return true;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", "Could not create embedded Firebird database: " + ex.Message, DateTime.Now, -1, null, Errors.Failed);
                return false;
            }
        }

        /// <summary>Resolves the full .fdb file path based on InMemory + ConnectionProperties.</summary>
        private string ResolveFilePath()
        {
            var cp = base.Dataconnection.ConnectionProp;
            var name = cp.FileName ?? "FirebirdDB.fdb";
            if (!Path.HasExtension(name)) name += ".fdb";
            if (InMemory || string.IsNullOrEmpty(cp.FilePath))
                return Path.Combine(Path.GetTempPath(), name);
            return Path.Combine(cp.FilePath, name);
        }
    }
}