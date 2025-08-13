

using Raven.Client.Documents;
using Raven.Client.Documents.Commands;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Session;
using Raven.Client.Exceptions;
using Raven.Client.Exceptions.Database;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using Sparrow.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DriversConfigurations;


using TheTechIdea.Beep.WebAPI;

using Raven.Embedded;


namespace TheTechIdea.Beep.NOSQL.RavenDB
{
    [AddinAttribute(Category = DatasourceCategory.NOSQL, DatasourceType =  DataSourceType.RavenDB)]
    public class RavenDBDataSource : IDataSource, IInMemoryDB
    {
        public string GuidID { get; set; }  
        public event EventHandler<PassedArgs> PassEvent;
        public event EventHandler<PassedArgs> OnLoadData;
        public event EventHandler<PassedArgs> OnLoadStructure;
        public event EventHandler<PassedArgs> OnSaveStructure;
        public event EventHandler<PassedArgs> OnCreateStructure;
        public event EventHandler<PassedArgs> OnRefreshData;
        public event EventHandler<PassedArgs> OnRefreshDataEntity;
        public event EventHandler<PassedArgs> OnSyncData;

        public BindingList<string> Databases { get ; set ; }
        public List<DatabaseCollection> RavenDatabases { get; set; }
        public DataSourceType DatasourceType { get ; set ; }
        public DatasourceCategory Category { get ; set ; }
        public IDataConnection Dataconnection { get ; set ; }
        public string DatasourceName { get ; set ; }
        public IErrorsInfo ErrorObject { get ; set ; }
        public string Id { get ; set ; }
        public IDMLogger Logger { get ; set ; }
        public List<string> EntitiesNames { get ; set ; }
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
        public IDMEEditor DMEEditor { get ; set ; }
      
        public ConnectionState ConnectionStatus { get ; set ; }
        public IDocumentSession Session { get; set; }
        public IDocumentStore Store { get; set; }
       
        public List<string> Collections { get; set; }
        public string CurrentDatabase { get; set; }
        public virtual string ColumnDelimiter { get; set; } = "''";
        public virtual string ParameterDelimiter { get; set; } = ":";
        public List<EntityStructure> InMemoryStructures { get  ; set  ; }
        public bool IsCreated { get  ; set  ; }
        public bool IsLoaded { get  ; set  ; }
        public bool IsSaved { get  ; set  ; }
        public bool IsSynced { get  ; set  ; }
        public ETLScriptHDR CreateScript { get  ; set  ; }
        public bool IsStructureCreated { get  ; set  ; }

        public virtual Task<double> GetScalarAsync(string query)
        {
            return Task.Run(() => GetScalar(query));
        }
        public virtual double GetScalar(string query)
        {
            ErrorObject.Flag = Errors.Ok;

            try
            {
                // Assuming you have a database connection and command objects.

                //using (var command = GetDataCommand())
                //{
                //    command.CommandText = query;
                //    var result = command.ExecuteScalar();

                //    // Check if the result is not null and can be converted to a double.
                //    if (result != null && double.TryParse(result.ToString(), out double value))
                //    {
                //        return value;
                //    }
                //}


                // If the query executed successfully but didn't return a valid double, you can handle it here.
                // You might want to log an error or throw an exception as needed.
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error in executing scalar query ({ex.Message})", DateTime.Now, 0, "", Errors.Failed);
            }

            // Return a default value or throw an exception if the query failed.
            return 0.0; // You can change this default value as needed.
        }
        public RavenDBDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = databasetype;
            Category = DatasourceCategory.NOSQL;
            Dataconnection = new WebAPIDataConnection
            {
                Logger = logger,
                ErrorObject = ErrorObject

            };
            Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == datasourcename).FirstOrDefault();

     
            CurrentDatabase = Dataconnection.ConnectionProp.Database;
            if (CurrentDatabase != null)
            {
                
                if (string.IsNullOrWhiteSpace(CurrentDatabase) == false)
                {
                    Store = OpenStore(Dataconnection.ConnectionProp.Url, 10, true);
                    if (Store != null)
                    {
                        
                        CurrentDatabase = Store.Database;
                        if (CurrentDatabase.Length > 0)
                        {
                            GetEntitesList();
                        }

                    }
                    else
                    {
                        ConnectionStatus = ConnectionState.Closed;
                    }

                }
                
            }
          


        }
        public virtual IErrorsInfo BeginTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Beep", $"Error in Begin Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        public virtual IErrorsInfo EndTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Beep", $"Error in end Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        public virtual IErrorsInfo Commit(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Beep", $"Error in Begin Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public int GetEntityIdx(string entityName)
        {
            if (Entities.Count > 0)
            {
                return Entities.FindIndex(p => p.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase) || p.DatasourceEntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                return -1;
            }


        }
        public ConnectionState Openconnection()
        {
            Store = OpenStore(Dataconnection.ConnectionProp.Url, 10, true);
            if (Store != null)
            {

                CurrentDatabase = Store.Database;
                if (CurrentDatabase.Length > 0)
                {
                    GetEntitesList();
                }
                ConnectionStatus = ConnectionState.Open;
            }
            else
            {
                ConnectionStatus = ConnectionState.Closed;
            }
            return ConnectionStatus;
        }

        public ConnectionState Closeconnection()
        {
            return ConnectionStatus;
        }

        public bool CheckEntityExist(string EntityName)
        {
            if (ConnectionStatus == ConnectionState.Open)
            {
                return Dataconnection.ConnectionProp.Entities.Where(o => o.EntityName.Equals(EntityName, StringComparison.OrdinalIgnoreCase)).Any();
            }
            else
                return false;

              
        }

        public bool CreateEntityAs(EntityStructure entity)
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

        public DataSet GetChildTablesListFromCustomQuery(string tablename, string customquery)
        {
            throw new NotImplementedException();
        }

        public List<string> GetEntitesList()
        {

            try
            {
                if (ConnectionStatus == ConnectionState.Open)
                {
                    if (Dataconnection.ConnectionProp.Entities.Count == 0)
                    {
                        EntitiesNames = new List<string>();
                        EntitiesNames = GetCollection().ToList();
                        foreach (string item in EntitiesNames)
                        {
                            EntityStructure ent = GetEntityStructure(item);
                            if (ent != null)
                            {
                                if (!Entities.Where(i => i.EntityName == item).Any())
                                    Entities.Add(ent);

                            }
                        }
                        DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new ConfigUtil.DatasourceEntities { datasourcename = DatasourceName, Entities = Entities });
                    }
                }
               Logger.WriteLog("Successfully Retrieve Entites list ");

            }
            catch (Exception ex)
            {
                string mes = "";
                DMEEditor.AddLogMessage(ex.Message, "Could get entities List" + mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return EntitiesNames;
        }

        public Task<object> GetEntityDataAsync(string entityname, string filterstr)
        {
            throw new NotImplementedException();
        }

        public object GetEntity(string EntityName, List<AppFilter> filter)
        {
            object entity;
            using (var session = Store.OpenSession())
            {
                entity = session.Load<object>(EntityName);
            }
            return entity;
        }

        public List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            throw new NotImplementedException();
        }

        public EntityStructure GetEntityStructure(string EntityName, bool refresh = false)
        {
            return GetEntityStructure(EntityName);
        }

        public DataTable GetEntityDataTable(string EntityName, string filterstr)
        {
            throw new NotImplementedException();
        }

        public Type GetEntityType(string EntityName)
        {
            EntityStructure x = GetEntityStructure(EntityName, false);
            DMTypeBuilder.CreateNewObject(DMEEditor, EntityName, EntityName, x.Fields);
            return DMTypeBuilder.MyType;
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            throw new NotImplementedException();
        }

        public  object RunQuery( string qrystr)
        {
            throw new NotImplementedException();
        }
        public virtual IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {


            throw new NotImplementedException();
        }
        public IErrorsInfo DeleteEntity(string EntityName, object DeletedDataRow)
        {
            throw new NotImplementedException();
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            throw new NotImplementedException();
        }
        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            throw new NotImplementedException();
        }
        public List<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                using (var session = Store.OpenSession())
                {
                    session.Store(InsertedData, EntityName);
                    session.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error in InsertEntity {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
                ErrorObject.Flag = Errors.Failed;
            }
            return ErrorObject;
        }
        public Task<object> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
           return Task.Run(() => GetEntity(EntityName, Filter));
        }
        public IErrorsInfo OpenDatabaseInMemory(string databasename)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                EmbeddedServer.Instance.StartServer();
                Store = (IDocumentStore)EmbeddedServer.Instance.GetDocumentStoreAsync("Embedded");
              
                Store.Initialize();
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error in OpenDatabaseInMemory {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
                ErrorObject.Flag = Errors.Failed;
            }
            return ErrorObject;
        }
        #region "RavenDB Client Methods"

        public EntityStructure GetEntityStructure(string DocName)
        {
            EntityStructure retval = new EntityStructure();
            try
            {

                Session = GetSession(CurrentDatabase);
                var command =new GetDocumentsCommand(1,2);
                Session.Advanced.RequestExecutor.Execute(command, Session.Advanced.Context);
                if(command.Result != null)
                {
                    var result = (BlittableJsonReaderObject)command.Result.Results[0];
                    var documentMetadata = (BlittableJsonReaderObject)result["@metadata"];

                    // Print out all the metadata properties.
                    EntityStructure entityData = new EntityStructure();

                    string sheetname;
                    sheetname = DocName;
                    entityData.EntityName = DocName;
                    entityData.DataSourceID = Dataconnection.ConnectionProp.ConnectionName;
                    entityData.SchemaOrOwnerOrDatabase = CurrentDatabase;
                    List<EntityField> Fields = new List<EntityField>();
                    int y = 0;
                    foreach (var propertyName in documentMetadata.GetPropertyNames())
                    {
                        documentMetadata.TryGet<object>(propertyName, out var metaPropValue);

                        EntityField f = new EntityField();
                        f.fieldname = propertyName;
                        f.fieldtype = "System.String";
                        f.ValueRetrievedFromParent = false;
                        f.EntityName = sheetname;
                        f.FieldIndex = y;
                        Fields.Add(f);
                        y += 1;
                    }
                }
                
                return retval;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage(ex.Message, "Could not Create Entity structure for RavenDB Entity " + DocName, DateTime.Now, -1, Dataconnection.ConnectionProp.Url, Errors.Failed);
                return null;
            }
        }
        public List<string> GetCollection()
        {

            try
            {
                var op = new GetCollectionStatisticsOperation();
                CollectionStatistics collectionStats = Store.Maintenance.Send(op);
                Collections = collectionStats.Collections.Keys.ToList();
                return Collections;

            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage(ex.Message, "Could not get Collection from Database in RavenDB " + CurrentDatabase, DateTime.Now, -1, Dataconnection.ConnectionProp.Url, Errors.Failed);
                return null;
            }
        }
        public List<DatabaseCollection> GetDatabaseNames()
        {

            try
            {
                var operation = new GetDatabaseNamesOperation(0, 25);
                string[] databaseNames = Store.Maintenance.Server.Send(operation);
                RavenDatabases = new List<DatabaseCollection>();
                foreach (string item in databaseNames)
                {
                    GetSession(item);
                    DatabaseCollection t = new DatabaseCollection();
                    t.DatabasName = item;
                    t.Collections = GetCollection();

                    foreach (string col in t.Collections)
                    {

                        Entities.Add(GetEntityStructure(col.Remove(col.Length, 1)));

                    }

                }

                return RavenDatabases;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", $"Could not Get Databases From RavenDB  {ex.Message}" + Dataconnection.ConnectionProp.ConnectionName, DateTime.Now, -1, Dataconnection.ConnectionProp.Url, Errors.Failed);
                return null;
            }
        }
       
        public IDocumentSession GetSession(string Database)
        {
            try
            {
                if (ConnectionStatus == ConnectionState.Open)
                {
                    if (string.IsNullOrWhiteSpace(Database) == false)
                    {

                        Session = Store.OpenSession(Database);


                    }
                    else
                        Session = Store.OpenSession();
                    return Session;


                };
                return null;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", "Could not Open Store Session in RavenDB " + ex.Message, DateTime.Now, -1, Dataconnection.ConnectionProp.Url, Errors.Failed);
                return null;
            }
        }
        public IDocumentSession CloseSession(string Database)
        {
            try
            {
                Session.Dispose();
                return null;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", "Could not Close Store Session in RavenDB " + ex.Message, DateTime.Now, -1, Dataconnection.ConnectionProp.Url, Errors.Failed);
                return null;
            }
        }
        public IDocumentStore OpenStore(string pUrl, int pMaxNumberOfRequestsPerSession = 10, bool pUseOptimisticConcurrency = true)
        {

            try
            {
                if (string.IsNullOrWhiteSpace(Dataconnection.ConnectionProp.CertificatePath) == false)
                {

                    Store = new DocumentStore()
                    {

                        Urls = new[] { pUrl, /*some additional nodes of this cluster*/ },
                        // Set conventions as necessary (optional)
                        Conventions =
                            {
                                MaxNumberOfRequestsPerSession = pMaxNumberOfRequestsPerSession,
                                UseOptimisticConcurrency = pUseOptimisticConcurrency
                            },
                        Certificate = new X509Certificate2(Dataconnection.ConnectionProp.CertificatePath),
                        Database = CurrentDatabase
                    }.Initialize();

                }
                else
                {
                    Store = new DocumentStore()
                    {

                        Urls = new[] { pUrl, /*some additional nodes of this cluster*/ },
                        // Set conventions as necessary (optional)
                        Conventions =
                    {
                         MaxNumberOfRequestsPerSession = pMaxNumberOfRequestsPerSession,
                         UseOptimisticConcurrency = pUseOptimisticConcurrency
                    },
                        Database = CurrentDatabase

                    }.Initialize();
                }
                ConnectionStatus = ConnectionState.Open;
                return Store;
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Closed;
                DMEEditor.AddLogMessage("Error", "Could not Open Store in RavenDB " + ex.Message, DateTime.Now, -1, Dataconnection.ConnectionProp.Url, Errors.Failed);
                return null;
            }



        }
        public async Task EnsureDatabaseExistsAsync(IDocumentStore store, string database = null, bool createDatabaseIfNotExists = true)
        {
            database = database ?? store.Database;

            if (string.IsNullOrWhiteSpace(database))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(database));

            try
            {
                await store.Maintenance.ForDatabase(database).SendAsync(new GetStatisticsOperation());
            }
            catch (DatabaseDoesNotExistException)
            {
                if (createDatabaseIfNotExists == false)
                    throw;

                try
                {
                    await store.Maintenance.Server.SendAsync(new CreateDatabaseOperation(new DatabaseRecord(database)));
                }
                catch (ConcurrencyException)
                {
                }
            }
        }
        public bool WriteDocumentToStore<T>(string pDatabase, T MyObject)
        {
            try
            {
                using (var session = Store.OpenAsyncSession(new SessionOptions
                {
                    //default is:     TransactionMode.SingleNode
                    TransactionMode = TransactionMode.ClusterWide,
                    Database = pDatabase

                }))
                {
                    //var user = new Employee
                    //{
                    //    FirstName = "John",
                    //    LastName = "Doe"
                    //};
                    session.StoreAsync(MyObject);

                    // this transaction is now conditional on this being 
                    // successfully created (so, no other users with this name)
                    // it also creates an association to the new user's id
                    //session.Advanced.ClusterTransaction
                    //    .CreateCompareExchangeValue("usernames/John", user.Id);

                    session.SaveChangesAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                string mes = "";
                DMEEditor.AddLogMessage(ex.Message, "Could not Store Document Store in RavenDB " + mes, DateTime.Now, -1, mes, Errors.Failed);
                return false;
            };
        }
        #endregion
        #region "dispose"
        private bool disposedValue;
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
        // ~RDBSource()
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

        public string GetConnectionString()
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo SaveStructure()
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo LoadStructure()
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo LoadData(Progress<PassedArgs> progress, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo SyncData(Progress<PassedArgs> progress, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo LoadStructure(Progress<PassedArgs> progress, CancellationToken token, bool copydata = false)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo CreateStructure(Progress<PassedArgs> progress, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo SyncData(string entityname, Progress<PassedArgs> progress, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo RefreshData(Progress<PassedArgs> progress, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo RefreshData(string entityname, Progress<PassedArgs> progress, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public object GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo LoadStructure(IProgress<PassedArgs> progress, CancellationToken token, bool copydata = false)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo CreateStructure(IProgress<PassedArgs> progress, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo LoadData(IProgress<PassedArgs> progress, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo SyncData(IProgress<PassedArgs> progress, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo SyncData(string entityname, IProgress<PassedArgs> progress, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo RefreshData(IProgress<PassedArgs> progress, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo RefreshData(string entityname, IProgress<PassedArgs> progress, CancellationToken token)
        {
            throw new NotImplementedException();
        }


        #endregion
    }
}
