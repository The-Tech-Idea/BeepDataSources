using Realms;
using Realms.Sync;
using System.Data;
using System.Reflection;
using TheTechIdea.Beep.Connections;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.DataSource
{
    public class RealMDataSource : IDataSource
    {
        public RealMDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = databasetype;
            Category = DatasourceCategory.NOSQL;
            Dataconnection = new DefaulDataConnection();


           // Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == datasourcename).FirstOrDefault();

        }
        public Realm RealMInstance { get; set; } 
        public Realms.Sync.App App { get; set; }
        public Realms.Sync.AppConfiguration AppConfiguration { get; set; }
        public Realms.Sync.User User { get; set; }
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
        public bool IsConnected { get; set; } = false;
        public async Task<IErrorsInfo> CreateAsync()
        {
            try
            {
                  if (App == null)
                    {
                        App.Create(AppConfiguration);
                    }
             

            }
            catch (Exception ex)
            {
                IsConnected = false;
                DMEEditor.AddLogMessage("Beep", $"Could not login in Realm Database {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public async Task<IErrorsInfo> LoginAsync(string username, string password)
        {
            try
            {
                // Login with existing user
                if(App == null)
                {
                    CreateAsync();
                }
                Dataconnection.ConnectionProp.UserID = username;
                Dataconnection.ConnectionProp.Password = password;
                User = await App.LogInAsync(Credentials.EmailPassword(username, password));
                IsConnected=true;
            }
            catch (Exception ex)
            {
                IsConnected = false;
                DMEEditor.AddLogMessage("Beep", $"Could not login in Realm Database {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public async Task<IErrorsInfo> LogoutAsync()
        {
            try
            {
                // Login with existing user
                await User.LogOutAsync();

                IsConnected = false;
            }
            catch (Exception ex)
            {
                IsConnected = false;
                DMEEditor.AddLogMessage("Beep", $"Could not logout in Realm Database {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public async Task<IErrorsInfo> DataAsync<T>() where T : RealmObject
        {
            try
            {
                if (IsConnected)
                {
                    var config = new FlexibleSyncConfiguration(App.CurrentUser)
                    {
                        PopulateInitialSubscriptions = (realm) =>
                        {

                         //   var myItems = realm.All<T>().Where(n => n.OwnerId == Dataconnection.ConnectionProp.UserID);
                         //   realm.Subscriptions.Add(myItems);
                        }
                    };

                    // The process will complete when all the user's items have been downloaded.
                    var realm = await Realm.GetInstanceAsync(config);

                }

            }
            catch (Exception ex)
            {
                IsConnected = false;
                DMEEditor.AddLogMessage("Beep", $"Could not login in Realm Database {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        Transaction transaction;
        public List<RealmObject> RealMObjects { get; set; } = new List<RealmObject>();
        public List<Type> RealTypeMObjects { get; set; } = new List<Type>();
        public string GuidID { get; set; }

        public DataSourceType DatasourceType { get; set; } = DataSourceType.RealIM;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.NOSQL;   
        public IDataConnection Dataconnection { get; set; }
        public string DatasourceName { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public string Id { get; set; }
        public IDMLogger Logger { get; set; }
        public List<string> EntitiesNames { get; set; }=new List<string>();
        public List<EntityStructure> Entities { get; set; }=new List<EntityStructure>();
        public IDMEEditor DMEEditor { get; set; }
        public System.Data.ConnectionState ConnectionStatus { get; set; } = System.Data.ConnectionState.Closed;
        public string ColumnDelimiter { get; set; }
        public string ParameterDelimiter { get; set; }

        public event EventHandler<PassedArgs> PassEvent;
        public virtual IErrorsInfo BeginTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (transaction != null)
                {
                    if (transaction.State == TransactionState.Running)
                    {
                        return DMEEditor.ErrorObject;
                    }
                }
               
                if(transaction == null)
                {
                    transaction = RealMInstance.BeginWrite();
                }
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
                if (transaction != null)
                {
                    if (transaction.State == TransactionState.Running)
                    {
                       transaction.Commit();
                    }
                }

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
                EndTransaction(args);
            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Beep", $"Error in Begin Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public bool CheckEntityExist(string EntityName)
        {
            return EntitiesNames.Contains(EntityName);
        }
        public void GetAllReamlMObjects()
        {
            RealTypeMObjects = AppDomain.CurrentDomain.GetAssemblies()
                           .SelectMany(assembly => assembly.GetTypes())
                           .Where(type =>
                           {
                               return type.IsSubclassOf(typeof(RealmObject));
                           }).ToList();
        }
        public System.Data.ConnectionState Closeconnection()
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                
                RealMInstance.Dispose();
                ConnectionStatus = System.Data.ConnectionState.Open;
            }
            catch (Exception ex)
            {
                ConnectionStatus = System.Data.ConnectionState.Broken;
                DMEEditor.AddLogMessage("Beep", $"Error in Opening RealM : {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return ConnectionStatus;
          
        }

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            throw new NotImplementedException();
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            IErrorsInfo errorsInfo = DMEEditor.ErrorObject;
            try
            {
                if (UploadDataRow is RealmObject realmObject)
                {
                    //var realm = Realm.GetInstance(); // Get the default Realm instance

                    RealMInstance.Write(() =>
                    {
                        RealMInstance.Remove(realmObject);
                    });

                    errorsInfo.Flag = Errors.Ok; // Assuming Errors is an enum and Success is one of its members
                }
                else
                {
                    errorsInfo.Flag = Errors.Failed; // Assuming Errors is an enum and Failed is one of its members
                    errorsInfo.Message = "UploadDataRow is not a RealmObject";
                }
            }
            catch (Exception ex)
            {
                errorsInfo.Flag = Errors.Failed;
                errorsInfo.Ex = ex;
            }

            return errorsInfo;
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
            //var realmObjectTypes = AppDomain.CurrentDomain.GetAssemblies()
            //               .SelectMany(assembly => assembly.GetTypes())
            //               .Where(type => type.IsSubclassOf(typeof(RealmObject)))
            //               .Select(type => type.Name)
            //               .ToList();
            //RealTypeMObjects = AppDomain.CurrentDomain.GetAssemblies()
            //               .SelectMany(assembly => assembly.GetTypes())
            //               .Where(type =>
            //               {
            //                   return type.IsSubclassOf(typeof(RealmObject));
            //               }).ToList();
            //EntitiesNames.Clear();
            //EntitiesNames.AddRange(realmObjectTypes);
            //Entities.Clear();
            GetAllReamlMObjects();
            if (RealTypeMObjects.Count != EntitiesNames.Count)
            {
                
                foreach (var item in RealTypeMObjects)
                {
                    if (!EntitiesNames.Contains(item.Name))
                    {
                        RealmObject obj = (RealmObject)Activator.CreateInstance(item);
                        EntityStructure ent=GetEntityStructureFromRealmObject(obj);
                        EntitiesNames.Add(item.Name);
                    }
                    
                }
            }
            return EntitiesNames;
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
            throw new NotImplementedException();
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            throw new NotImplementedException();
        }

        public Type GetEntityType(string EntityName)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            IErrorsInfo errorsInfo = DMEEditor.ErrorObject;
            EntityStructure ent = Entities.FirstOrDefault(p => p.EntityName == EntityName);
            try
            {
                if (InsertedData is RealmObject realmObject)
                {
                    dynamic existingObject = null;

                    var idProperty = InsertedData.GetType().GetProperty(ent.PrimaryKeys.FirstOrDefault().fieldname);
                    if (idProperty != null)
                    {
                        var idValue = idProperty.GetValue(InsertedData);
                        var method = typeof(Realm).GetMethod("Find");
                        var generic = method.MakeGenericMethod(InsertedData.GetType());
                        existingObject = generic.Invoke(RealMInstance, new[] { idValue });
                    }

                   
                    if (idProperty != null)
                    {
                        var primaryKey = idProperty.GetValue(realmObject);
                        var idValue = idProperty.GetValue(InsertedData);
                        var method = typeof(Realm).GetMethod("Find");
                        var generic = method.MakeGenericMethod(InsertedData.GetType());
                        existingObject = generic.Invoke(RealMInstance, new[] { idValue });

                        if (existingObject != null)
                        {
                            errorsInfo.Flag = Errors.Failed;
                            errorsInfo.Message = "Record already exists";
                            return errorsInfo;
                        }
                    }

                    RealMInstance.Write(() =>
                    {
                        RealMInstance.Add(realmObject);
                    });

                    errorsInfo.Flag = Errors.Ok; // Assuming Errors is an enum and Success is one of its members
                }
                else
                {
                    errorsInfo.Flag = Errors.Failed; // Assuming Errors is an enum and Failed is one of its members
                    errorsInfo.Message = "InsertedData is not a RealmObject";
                }
            }
            catch (Exception ex)
            {
                errorsInfo.Flag = Errors.Failed;
                errorsInfo.Ex = ex;
            }

            return errorsInfo;
        }

        public System.Data.ConnectionState Openconnection()
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                 RealMInstance = Realm.GetInstance(); // Get a reference to the Realm instance
                ConnectionStatus = System.Data.ConnectionState.Open;
            }
            catch (Exception ex)
            {
                ConnectionStatus = System.Data.ConnectionState.Broken;
                DMEEditor.AddLogMessage("Beep", $"Error in Opening RealM : {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
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

            IErrorsInfo errorsInfo = new ErrorsInfo();
            EntityStructure ent=Entities.FirstOrDefault(p=>p.EntityName==EntityName);
            try
            {
                if (UploadDataRow is RealmObject uploadRealmObject)
                {
                   

                    RealMInstance.Write(() =>
                    {
                        dynamic existingObject = null;

                        var idProperty = uploadRealmObject.GetType().GetProperty(ent.PrimaryKeys.FirstOrDefault().fieldname);
                        if (idProperty != null)
                        {
                            var idValue = idProperty.GetValue(uploadRealmObject);
                            var method = typeof(Realm).GetMethod("Find");
                            var generic = method.MakeGenericMethod(uploadRealmObject.GetType());
                            existingObject = generic.Invoke(RealMInstance, new[] { idValue });
                        }


                        if (existingObject != null)
                        {
                            // Here you would set each property you wish to update.
                            // For example, if your RealmObject has a field named "Name":
                            foreach (var property in existingObject.GetType().GetProperties())
                            {
                                if (property.CanWrite && property.Name != ent.PrimaryKeys.FirstOrDefault().fieldname)  // Assuming "Id" is the primary key and should not be changed
                                {
                                    var newValue = property.GetValue(uploadRealmObject);
                                    property.SetValue(existingObject, newValue);
                                }
                            }

                            // Repeat this for all fields you wish to update.

                            // Optionally, if you want to update all properties, you can loop through properties using reflection.
                            foreach (var property in existingObject.GetType().GetProperties())
                            {
                                if (property.CanWrite && property.Name != ent.PrimaryKeys.FirstOrDefault().fieldname) // Assuming "Id" is the primary key and should not be changed.
                                {
                                    var newValue = property.GetValue(uploadRealmObject);
                                    property.SetValue(existingObject, newValue);
                                }
                            }
                        }
                        else
                        {
                            // Handle the case where the object does not exist in the database.
                            errorsInfo.Flag = Errors.Failed;
                            errorsInfo.Ex = new Exception("Entity not found");
                        }
                    });

                    errorsInfo.Flag = Errors.Ok;
                }
                else
                {
                    errorsInfo.Flag = Errors.Failed;
                    errorsInfo.Ex = new Exception("UploadDataRow is not a RealmObject");
                }
            }
            catch (Exception ex)
            {
                errorsInfo.Flag = Errors.Failed;
                errorsInfo.Ex = ex;
            }

            return errorsInfo;
        }


        #region "RealM "
        public EntityStructure GetEntityStructureFromRealmObject(RealmObject realmObject)
        {
            EntityStructure entity = new EntityStructure();
            Type tp = realmObject.GetType();

            if (entity.Fields.Count == 0)
            {
                var properties = tp.GetProperties();

                foreach (var property in properties)
                {
                    EntityField x = new EntityField();
                    try
                    {
                        x.fieldname = property.Name;
                        x.fieldtype = property.PropertyType.ToString();

                        // Check for primary key
                        x.IsKey = property.GetCustomAttributes(typeof(PrimaryKeyAttribute), false).Any();

                        // Check for indexed field
                        //x.is = property.GetCustomAttributes(typeof(IndexedAttribute), false).Any();

                        // Check for custom mapped name
                        if (property.GetCustomAttributes<MapToAttribute>(false).Any())
                        {
                          x.Originalfieldname=  property.GetCustomAttributes<MapToAttribute>(false).FirstOrDefault()?.Mapping ?? property.Name;
                        }

                        // Check for required field
                        x.AllowDBNull = property.GetCustomAttributes(typeof(RequiredAttribute), false).Any();

                     
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog("Error in Creating Field Type");
                        ErrorObject.Flag = Errors.Failed;
                        ErrorObject.Ex = ex;
                    }

                    if (x.IsKey)
                    {
                        entity.PrimaryKeys.Add(x);
                    }

                    entity.Fields.Add(x);
                }
            }

            return entity;
        }

        #endregion "Real M"

    }
}
