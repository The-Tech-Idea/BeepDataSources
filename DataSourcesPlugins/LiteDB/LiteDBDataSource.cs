using LiteDB;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.Datasources
{
    [AddinAttribute(Category = DatasourceCategory.NOSQL, DatasourceType = DataSourceType.LiteDB)]
    public class LiteDBDataSource : IDataSource,ILocalDB
    {

        LiteDatabase db;
        public LiteDBDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = databasetype;
            Category = DatasourceCategory.NOSQL;

            Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == datasourcename).FirstOrDefault();
            if(Dataconnection.ConnectionProp != null)
            {
                Dataconnection.ConnectionProp.Category = DatasourceCategory.NOSQL;
                Dataconnection.ConnectionProp.DatabaseType = DataSourceType.LiteDB;
            }
            else
            {

            }
          
        }
        public DataSourceType DatasourceType { get ; set ; }
        public DatasourceCategory Category { get ; set ; }
        public IDataConnection Dataconnection { get ; set ; }
        public string DatasourceName { get ; set ; }
        public IErrorsInfo ErrorObject { get ; set ; }
        public string Id { get ; set ; }
        public IDMLogger Logger { get ; set ; }
        public List<string> EntitiesNames { get ; set ; }=new List<string>();
        public List<EntityStructure> Entities { get ; set ; }=new List<EntityStructure>();
        public IDMEEditor DMEEditor { get ; set ; }
        public ConnectionState ConnectionStatus { get ; set ; }
        public string ColumnDelimiter { get ; set ; }
        public string ParameterDelimiter { get ; set ; }
        public bool CanCreateLocal { get ; set ; }
        string CombineFilePath = "";
        public event EventHandler<PassedArgs> PassEvent;
        #region "Local DB Interface"
        public bool CopyDB(string DestDbName, string DesPath)
        {
            try
            {
                if (!System.IO.File.Exists(Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName)))
                {
                    File.Copy(Dataconnection.ConnectionProp.ConnectionString, Path.Combine(DesPath, DestDbName));
                }
                DMEEditor.AddLogMessage("Success", "Copy LiteDB Database", DateTime.Now, 0, null, Errors.Ok);
                return true;
            }
            catch (Exception ex)
            {
                string mes = "Could not Copy LiteDB Database";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
                return false;
            };
        }
        public bool CreateDB()
        {
            try
            {
                if (!Path.HasExtension(Dataconnection.ConnectionProp.FileName))
                {
                    Dataconnection.ConnectionProp.FileName = Dataconnection.ConnectionProp.FileName + ".ldb";
                }
                if (!System.IO.File.Exists(Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName)))
                {
                    db=new LiteDatabase(Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName));
                    DMEEditor.AddLogMessage("Success", "Create LiteDB Database", DateTime.Now, 0, null, Errors.Ok);
                }
                else
                {
                    DMEEditor.AddLogMessage("Success", "LiteDB Database already exist", DateTime.Now, 0, null, Errors.Ok);
                }
                return true;
            }
            catch (Exception ex)
            {
                string mes = "Could not Create Sqlite Database";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
                return false;
            };
        }
        public bool DeleteDB()
        {
            try
            {
                Closeconnection();
                GC.Collect();
                GC.WaitForPendingFinalizers();

                if (System.IO.File.Exists(Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName)))
                {
                    File.Delete(Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName));
                }
                DMEEditor.AddLogMessage("Success", "Deleted Sqlite Database", DateTime.Now, 0, null, Errors.Ok);
                return true;

            }
            catch (Exception ex)
            {
                string mes = "Could not Delete Sqlite Database";
                DMEEditor.AddLogMessage("Fail", ex.Message + mes, DateTime.Now, -1, mes, Errors.Failed);
                return false;
            };
        }
        public IErrorsInfo DropEntity(string EntityName)
        {
            try

            {

                String cmdText = $"drop table  '{EntityName}'";
                DMEEditor.ErrorObject = ExecuteSql(cmdText);

                if (CheckEntityExist(EntityName))
                {
                    DMEEditor.AddLogMessage("Success", $"Droping Entity {EntityName}", DateTime.Now, 0, null, Errors.Ok);
                }
                else
                {

                    DMEEditor.AddLogMessage("Error", $"Droping Entity {EntityName}", DateTime.Now, 0, null, Errors.Failed);
                }

            }
            catch (Exception ex)
            {
                string errmsg = $"Error Droping Entity {EntityName}";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;

        }
        public  ConnectionState Closeconnection()
        {
            try
            {
              
                if ( Dataconnection != null)
                {
                    Dataconnection.CloseConn();
                }


                DMEEditor.AddLogMessage("Success", $"Closing connection to Sqlite Database", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string errmsg = "Error Closing connection to Sqlite Database";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            //  return RDBMSConnection.DbConn.State;
            return Dataconnection.ConnectionStatus;
        }

        #endregion

        public bool CreateEntityAs(EntityStructure entity)
        {
            throw new NotImplementedException();
        }
        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            throw new NotImplementedException();
        }
        public bool CheckEntityExist(string EntityName)
        {
            throw new NotImplementedException();
        }
        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            throw new NotImplementedException();
        }
        public void Dispose()
        {
            throw new NotImplementedException();
        }
        public IErrorsInfo ExecuteSql(string sql)
        {
            throw new NotImplementedException();
        }

        public List<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            throw new NotImplementedException();
        }

        public List<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            throw new NotImplementedException();
        }

        public List<string> GetEntitesList()
        {
            throw new NotImplementedException();
        }

        public object GetEntity(string EntityName, List<AppFilter> filter)
        {
            throw new NotImplementedException();
        }

        public Task<object> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            throw new NotImplementedException();
        }

        public List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            throw new NotImplementedException();
        }

        public int GetEntityIdx(string entityName)
        {
            throw new NotImplementedException();
        }

        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            try
            {
                DMEEditor.ErrorObject.Ex = null;
                DMEEditor.ErrorObject.Flag = Errors.Ok;
                DMEEditor.ErrorObject.Message = "";
                EntityStructure entstrc = null;
                var col = db.GetCollection(EntityName);
                if (Entities.Count > 0)
                {
                    int idx = Entities.FindIndex(p => p.EntityName.Equals(EntityName, StringComparison.CurrentCultureIgnoreCase));
                    if (idx > -1)
                    {
                        entstrc = Entities[idx];
                    }
                    else
                    {
                        DMEEditor.ErrorObject.Flag = Errors.Failed;
                        DMEEditor.ErrorObject.Message = $"Entity {EntityName} not found";
                        return null;
                    }
                }
                return entstrc;


            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Ex = ex;
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = ex.Message;
                return null;
            }
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            try
            {
                DMEEditor.ErrorObject.Ex = null;
                DMEEditor.ErrorObject.Flag = Errors.Ok;
                DMEEditor.ErrorObject.Message = "";
                EntityStructure entstrc = null;
                var col = db.GetCollection(fnd.EntityName);
                if (Entities.Count > 0)
                {
                    int idx = Entities.FindIndex(p => p.EntityName.Equals(fnd.EntityName, StringComparison.CurrentCultureIgnoreCase));
                    if (idx > -1)
                    {
                        entstrc = Entities[idx];
                    }
                    else
                    {
                        DMEEditor.ErrorObject.Flag = Errors.Failed;
                        DMEEditor.ErrorObject.Message = $"Entity {fnd.EntityName} not found";
                        return null;
                    }
                }
                return entstrc;


            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Ex = ex;
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = ex.Message;
                return null;
            }
        }

        public Type GetEntityType(string EntityName)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            try
            {
                DMEEditor.ErrorObject.Ex = null;
                DMEEditor.ErrorObject.Flag = Errors.Ok;
                DMEEditor.ErrorObject.Message = "";
                EntityStructure entstrc = null;
                var col = db.GetCollection(EntityName);
                if (Entities.Count > 0)
                {
                    int idx = Entities.FindIndex(p => p.EntityName.Equals(EntityName, StringComparison.CurrentCultureIgnoreCase));
                    if (idx > -1)
                    {
                        entstrc = Entities[idx];
                    }
                    else
                    {
                        List<EntityField> fields = DMEEditor.Utilfunction.GetFieldFromGeneratedObject(InsertedData);
                        entstrc = new EntityStructure();
                        entstrc.EntityName = EntityName;
                        entstrc.Fields = fields;
                        Entities.Add(entstrc);
                        EntitiesNames.Add(EntityName);
                    }
                }
                col.Insert(ConvertObjectToBsonDoc(InsertedData));
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Ex = ex;
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = ex.Message;
            }
            return DMEEditor.ErrorObject;

         
        }

        public ConnectionState Openconnection()
        {
            ConnectionStatus = Dataconnection.OpenConnection();
            try
            {
                CreateDB();

                 // force open database
                 var uv = db.UserVersion;
            }
            catch (Exception ex)
            {
                db?.Dispose();
                db = null;


               
            }

            if (ConnectionStatus == ConnectionState.Open)
            {
                if (DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues(Dataconnection.ConnectionProp.FileName) == null)
                {
                    GetEntityStructures();
                }
                else
                {
                    Entities = DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues(Dataconnection.ConnectionProp.FileName).Entities;
                };
                CombineFilePath = Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName);
            }

            return ConnectionStatus;
        }

        public object RunQuery(string qrystr)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            try
            {
                DMEEditor.ErrorObject.Ex = null;
                DMEEditor.ErrorObject.Flag = Errors.Ok;
                DMEEditor.ErrorObject.Message = "";
                EntityStructure entstrc = null;
                var col = db.GetCollection(EntityName);
                if (Entities.Count > 0)
                {
                    int idx = Entities.FindIndex(p => p.EntityName.Equals(EntityName, StringComparison.CurrentCultureIgnoreCase));
                    if (idx > -1)
                    {
                        entstrc = Entities[idx];
                    }
                    else
                    {
                        List<EntityField> fields= DMEEditor.Utilfunction.GetFieldFromGeneratedObject(UploadDataRow);
                        entstrc = new EntityStructure();
                        entstrc.EntityName=EntityName;
                        entstrc.Fields=fields;
                        Entities.Add(entstrc);
                        EntitiesNames.Add(EntityName);
                    }
                }
                col.Update(ConvertObjectToBsonDoc(UploadDataRow));
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Ex = ex;
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = ex.Message;
            }
            return DMEEditor.ErrorObject;
        }
        #region "Support Methods"
        private async Task<LiteDatabase> AsyncConnect(ConnectionString connectionString)
        {
            return await Task.Run(() =>
            {
                return new LiteDatabase(connectionString);
            });
        }
        private void GetEntityStructures()
        {
            if (db != null)
            {
                IEnumerable<string> ls=db.GetCollectionNames();
                if(Entities.Count <= 0)
                {
                    foreach (string colname in ls)
                    {
                        var col = db.GetCollection(colname);
                        if (col != null)
                        {
                            var t = col.Query().Limit(1).FirstOrDefault();
                            if (t != null)
                            {
                                

                            }
                        }
                    }
                }
               
              
            }
        }
        private BsonDocument ConvertObjectToBsonDoc(object obj)
        {
            var bs=new BsonDocument();
            PropertyInfo[] properties=obj.GetType().GetProperties();    
            foreach (PropertyInfo pi in properties)
            {
                PropertyInfo SrcPropAInfo = obj.GetType().GetProperty(pi.Name);
                dynamic result = SrcPropAInfo.GetValue(obj);
                bs[pi.Name]=result;
            }
            bs["_id"] = ObjectId.NewObjectId();
            return bs;
        }
        private object ConvertBsonDocTOObject(BsonDocument obj)
        {
            var bs = new BsonDocument();
            PropertyInfo[] properties = obj.GetType().GetProperties();
            foreach (PropertyInfo pi in properties)
            {
                PropertyInfo SrcPropAInfo = obj.GetType().GetProperty(pi.Name);
                dynamic result = SrcPropAInfo.GetValue(obj);
                bs[pi.Name] = result;
            }
            bs["_id"] = ObjectId.NewObjectId();
            return bs;
        }
        #endregion
    }
}
