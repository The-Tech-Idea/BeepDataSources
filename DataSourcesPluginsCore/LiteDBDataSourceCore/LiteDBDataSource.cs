using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using LiteDB;
using TheTechIdea;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.FileManager;
using TheTechIdea.Beep.Report;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using DataManagementModels.Editor;

namespace LiteDBDataSourceCore
{
    [AddinAttribute(Category = DatasourceCategory.NOSQL, DatasourceType = DataSourceType.LiteDB)]
    public partial class LiteDBDataSource : IDataSource, ILocalDB
    {
        private bool disposedValue;
        private LiteDatabase db;
        private string _connectionString;
        Type enttype = null;
        bool ObjectsCreated = false;
        string lastentityname = null;
        EntityStructure DataStruct = null;
        string DBfilepathandname = string.Empty;

        public LiteDBDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per ?? new ErrorsInfo();
            DMEEditor = pDMEEditor;
            DatasourceType = databasetype;
            Category = DatasourceCategory.NOSQL;
            Dataconnection = new FileConnection(DMEEditor);
        }

        public string GuidID { get; set; }
        public DataSourceType DatasourceType { get; set; } = DataSourceType.LiteDB;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.NOSQL;
        public IDataConnection Dataconnection { get; set; }
        public string DatasourceName { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public string Id { get; set; }
        public IDMLogger Logger { get; set; }
        public List<string> EntitiesNames { get; set; } = new List<string>();
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
        public IDMEEditor DMEEditor { get; set; }
        public ConnectionState ConnectionStatus { get; set; }
        public string ColumnDelimiter { get; set; }
        public string ParameterDelimiter { get; set; }
        public bool CanCreateLocal { get; set; } = true;
        public bool InMemory { get; set; } = false;
        public string Extension { get; set; } = ".db";

        public event EventHandler<PassedArgs> PassEvent;

        private bool EnsureConnectionReady(string methodName)
        {
            if (ConnectionStatus != ConnectionState.Open || db == null)
            {
                Openconnection();
            }

            if (ConnectionStatus != ConnectionState.Open || db == null)
            {
                ErrorObject ??= new ErrorsInfo();
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = "Database connection is not open.";
                DMEEditor?.AddLogMessage("Beep", $"error in {methodName} in {DatasourceName} - Database connection is not open.", DateTime.Now, -1, null, Errors.Failed);
                return false;
            }

            return true;
        }

        private void SyncEntityCaches(string entityName, EntityStructure structure = null, bool remove = false)
        {
            if (string.IsNullOrWhiteSpace(entityName))
            {
                return;
            }

            Entities ??= new List<EntityStructure>();
            EntitiesNames ??= new List<string>();

            if (remove)
            {
                Entities.RemoveAll(e => e != null && e.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
                EntitiesNames.RemoveAll(n => string.Equals(n, entityName, StringComparison.OrdinalIgnoreCase));
                return;
            }

            if (!EntitiesNames.Any(n => string.Equals(n, entityName, StringComparison.OrdinalIgnoreCase)))
            {
                EntitiesNames.Add(entityName);
            }

            if (structure != null && !Entities.Any(e => e != null && e.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase)))
            {
                Entities.Add(structure);
            }
        }

        private void SetObjects(string Entityname)
        {
            if (!ObjectsCreated || Entityname != lastentityname)
            {
                DataStruct = GetEntityStructure(Entityname, false);
                enttype = GetEntityType(Entityname);
                ObjectsCreated = true;
                lastentityname = Entityname;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (db != null)
                    {
                        db.Dispose();
                        db = null;
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
