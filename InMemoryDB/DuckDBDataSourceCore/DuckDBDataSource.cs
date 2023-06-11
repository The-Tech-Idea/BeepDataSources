using DataManagementModels.DataBase;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using TheTechIdea;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using System;
using System.Data.Odbc;

namespace DuckDBDataSourceCore
{
    [AddinAttribute(Category = DatasourceCategory.INMEMORY, DatasourceType = DataSourceType.DuckDB)]
    public class DuckDBDataSource : RDBSource, IInMemoryDB
    {
        private bool disposedValue;
        string dbpath;
        public OdbcConnection connection { get; set; }
        public DuckDBDataSource(string pdatasourcename, IDMLogger plogger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per) : base(pdatasourcename, plogger, pDMEEditor, databasetype, per)
        {
            Dataconnection.ConnectionProp.DatabaseType = DataSourceType.DuckDB;
            Dataconnection.ConnectionProp.IsInMemory=true;
            ColumnDelimiter = "[]";
            ParameterDelimiter = "$";
            DMEEditor = pDMEEditor;
            DatasourceName=pdatasourcename;
        }
        public List<EntityStructure> InMemoryStructures { get; set; } = new List<EntityStructure>();
        public  ConnectionState Openconnection()
        {
            dbpath = DMEEditor.ConfigEditor.ExePath + "\\Scripts\\" + DatasourceName;
            ETLScriptHDR scriptHDR = new ETLScriptHDR();
            scriptHDR.ScriptDTL = new List<ETLScriptDet>();
            CancellationTokenSource token = new CancellationTokenSource();
            
            Dataconnection.InMemory = Dataconnection.ConnectionProp.IsInMemory;
            if (ConnectionStatus == ConnectionState.Open)
            {
                DMEEditor.AddLogMessage("Beep", $"Connection is already open", DateTime.Now, -1, "", Errors.Ok);
                return ConnectionState.Open;
            }
            OpenDatabaseInMemory(Dataconnection.ConnectionProp.Database);
              
                if (DMEEditor.ErrorObject.Flag == Errors.Ok)
                {
                    LoadStructure();
                    return ConnectionState.Open;
                }
       
            return ConnectionStatus;
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }
        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~DuckDBDataSource()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        public IErrorsInfo OpenDatabaseInMemory(string databasename)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                Dataconnection.InMemory = true;
                Dataconnection.ConnectionProp.FileName = string.Empty;
                Dataconnection.ConnectionProp.ConnectionString = "Driver=DuckDB;Database=:memory:";
                Dataconnection.ConnectionProp.Database = databasename;
                Dataconnection.ConnectionProp.ConnectionName = databasename;
               // connection = new OdbcConnection("Driver={DuckDB};"+$"Database={databasename};");
               
                base.Dataconnection.ConnectionStatus = ConnectionState.Open;

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Beep", $"Error in Begin Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public string GetConnectionString()
        {
            return Dataconnection.ConnectionProp.ConnectionString;
        }
        public override bool CreateEntityAs(EntityStructure entity)
        {
            string ds = entity.DataSourceID;
            bool retval = base.CreateEntityAs(entity);
            entity.DataSourceID = ds;

            InMemoryStructures.Add(GetEntity(entity));
            return retval;
        }
        public override ConnectionState Closeconnection()
        {
            try
            {
              
                SaveStructure();
                DMEEditor.AddLogMessage("Success", $"Closing connection to Sqlite Database", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string errmsg = "Error Closing connection to Sqlite Database";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            //  return RDBMSConnection.DbConn.State;
            return base.ConnectionStatus;
        }
        private EntityStructure GetEntity(EntityStructure entity)
        {
            EntityStructure ent = new EntityStructure();
            ent.DatasourceEntityName = entity.DatasourceEntityName;
            ent.DataSourceID = entity.DataSourceID; ;
            ent.DatabaseType = entity.DatabaseType;
            ent.Caption = entity.Caption;
            ent.Category = entity.Category;
            ent.Fields = entity.Fields;
            ent.PrimaryKeys = entity.PrimaryKeys;
            ent.Relations = entity.Relations;
            ent.OriginalEntityName = entity.OriginalEntityName;
            ent.GuidID = Guid.NewGuid().ToString();
            ent.ViewID = entity.ViewID;
            ent.Viewtype = entity.Viewtype;
            ent.EntityName = entity.EntityName;
            ent.OriginalEntityName = entity.OriginalEntityName;
            ent.SchemaOrOwnerOrDatabase = entity.SchemaOrOwnerOrDatabase;
            return ent;
        }
        public IErrorsInfo LoadStructure()
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                string filepath = Path.Combine(dbpath, "createscripts.json");
                string InMemoryStructuresfilepath = Path.Combine(dbpath, "InMemoryStructures.json");
                ConnectionStatus = ConnectionState.Open;
                InMemoryStructures = new List<EntityStructure>();
                Entities = new List<EntityStructure>();
                EntitiesNames = new List<string>();
                CancellationTokenSource token = new CancellationTokenSource();
                if (File.Exists(InMemoryStructuresfilepath))
                {
                    InMemoryStructures = DMEEditor.ConfigEditor.JsonLoader.DeserializeObject<EntityStructure>(InMemoryStructuresfilepath);
                }
                if (File.Exists(filepath))
                {
                    var hdr = DMEEditor.ConfigEditor.JsonLoader.DeserializeSingleObject<ETLScriptHDR>(filepath);
                    DMEEditor.ETL.Script = hdr;
                    DMEEditor.ETL.Script.LastRunDateTime = DateTime.Now;
                    DMEEditor.ETL.RunCreateScript(DMEEditor.progress, token.Token);

                }

            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Could not Load InMemory Structure for {DatasourceName}- {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public IErrorsInfo SaveStructure()
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                if (InMemoryStructures.Count > 0)
                {

                    Directory.CreateDirectory(dbpath);

                    string filepath = Path.Combine(dbpath, "createscripts.json");
                    string InMemoryStructuresfilepath = Path.Combine(dbpath, "InMemoryStructures.json");
                    ETLScriptHDR scriptHDR = new ETLScriptHDR();
                    scriptHDR.ScriptDTL = new List<ETLScriptDet>();
                    CancellationTokenSource token = new CancellationTokenSource();
                    scriptHDR.scriptName = Dataconnection.ConnectionProp.Database;
                    scriptHDR.scriptStatus = "SAVED";
                    scriptHDR.ScriptDTL.AddRange(DMEEditor.ETL.GetCreateEntityScript(this, InMemoryStructures, DMEEditor.progress, token.Token));
                    scriptHDR.ScriptDTL.AddRange(DMEEditor.ETL.GetCopyDataEntityScript(this, InMemoryStructures, DMEEditor.progress, token.Token));
                    DMEEditor.ConfigEditor.JsonLoader.Serialize(filepath, scriptHDR);
                    DMEEditor.ConfigEditor.JsonLoader.Serialize(InMemoryStructuresfilepath, InMemoryStructures);
                }

            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Could not save InMemory Structure for {DatasourceName}- {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
    }
}
