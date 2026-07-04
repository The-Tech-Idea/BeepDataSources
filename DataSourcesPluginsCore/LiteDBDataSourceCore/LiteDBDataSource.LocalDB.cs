using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using LiteDB;

namespace LiteDBDataSourceCore
{
    public partial class LiteDBDataSource
    {
        // ── ILocalDB (file-lifecycle parity with SQLite) ──
        public bool CanCreateLocal { get; set; } = true;
        public bool InMemory { get; set; } = false;
        public string Extension { get; set; } = ".ldb";

        public bool CreateDB()
        {
            try
            {
                var p = Dataconnection?.ConnectionProp;
                if (p == null) return false;
                if (string.IsNullOrEmpty(p.FileName) && string.IsNullOrEmpty(p.FilePath)) return false;
                var path = ResolveLocalDbPath();
                if (string.IsNullOrEmpty(path)) return false;
                return CreateDB(path);
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"LiteDB CreateDB error: {ex.Message}");
                return false;
            }
        }

        public bool CreateDB(bool inMemory)
        {
            InMemory = inMemory;
            if (inMemory)
            {
                Dataconnection!.ConnectionProp.IsInMemory = true;
                var tmp = Path.Combine(Path.GetTempPath(), "beep-litedb-" + Guid.NewGuid().ToString("N"));
                try { Directory.CreateDirectory(tmp); } catch { }
                Dataconnection.ConnectionProp.FilePath = tmp;
                Dataconnection.ConnectionProp.FileName = "memory.ldb";
                DatabasePath = Path.Combine(tmp, "memory.ldb");
                return Openconnection() == System.Data.ConnectionState.Open;
            }
            Dataconnection!.ConnectionProp.IsInMemory = false;
            return CreateDB();
        }

        public bool CreateDB(string filepathandname)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filepathandname)) return false;
                var dir = Path.GetDirectoryName(filepathandname);
                var name = Path.GetFileName(filepathandname);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                Closeconnection();
                Dataconnection!.ConnectionProp.FilePath = dir ?? string.Empty;
                Dataconnection.ConnectionProp.FileName = name;
                DatabasePath = filepathandname;
                return Openconnection() == System.Data.ConnectionState.Open;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"LiteDB CreateDB('{filepathandname}') error: {ex.Message}");
                return false;
            }
        }

        public bool DeleteDB()
        {
            try
            {
                Closeconnection();
                var path = ResolveLocalDbPath();
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;
                File.Delete(path);
                return true;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"LiteDB DeleteDB error: {ex.Message}");
                return false;
            }
        }

        public bool CopyDB(string destDbName, string destPath)
        {
            try
            {
                Closeconnection();
                var src = ResolveLocalDbPath();
                if (string.IsNullOrEmpty(src) || !File.Exists(src)) return false;
                if (string.IsNullOrWhiteSpace(destPath)) return false;
                if (!Directory.Exists(destPath)) Directory.CreateDirectory(destPath);
                var targetName = string.IsNullOrWhiteSpace(destDbName) ? Path.GetFileName(src) : destDbName;
                if (!Path.HasExtension(targetName)) targetName += Extension;
                File.Copy(src, Path.Combine(destPath, targetName), overwrite: true);
                return true;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"LiteDB CopyDB error: {ex.Message}");
                return false;
            }
        }

        public IErrorsInfo DropEntity(string EntityName)
            => DeleteEntity(EntityName, null);

        private string ResolveLocalDbPath()
        {
            var p = Dataconnection?.ConnectionProp;
            if (p == null) return null;
            if (!string.IsNullOrEmpty(p.FileName))
                return string.IsNullOrEmpty(p.FilePath) ? p.FileName : Path.Combine(p.FilePath, p.FileName);
            return p.FilePath;
        }
    }
}