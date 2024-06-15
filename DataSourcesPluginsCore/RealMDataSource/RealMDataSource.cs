using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using MongoDB.Bson;
using Realms;
using Realms.Sync;
using System.Data;
using System.Reflection;
using System.Text;
using TheTechIdea.Beep.Connections;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using System.Linq.Expressions;
using System.ComponentModel;
using DataManagementModels.Editor;
using DataManagementModels.DataBase;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.DataSource
{
    [AddinAttribute(Category = DatasourceCategory.CLOUD, DatasourceType = DataSourceType.RealIM)]
    public class RealMDataSource : IDataSource,ILocalDB,IInMemoryDB
    {
        public string DbPath { get; set; }
        public string dbname { get; set; }
        
       
        public RealMDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = databasetype;
            Category = DatasourceCategory.RDBMS;
            Dataconnection = new DefaulDataConnection();
            Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == datasourcename).FirstOrDefault();
            ConnectionStatus = System.Data.ConnectionState.Closed;
            if (Dataconnection.ConnectionProp == null)
            {
                Dataconnection.ConnectionProp = new ConnectionProperties();
                Dataconnection.DataSourceDriver = DMEEditor.ConfigEditor.DataDriversClasses.Find(p => p.classHandler == "SQLiteMauiDataSource");
            }
            dbname = "MyData.db";
            Appid= Dataconnection.ConnectionProp.GuidID;
            AppConfiguration = new AppConfiguration(Appid)
            {
                SyncTimeoutOptions = new SyncTimeoutOptions()
                {
                    ConnectTimeout = TimeSpan.FromMinutes(2),
                    ConnectionLingerTime = TimeSpan.FromSeconds(30),
                    PingKeepAlivePeriod = TimeSpan.FromMinutes(1),
                    PongKeepAliveTimeout = TimeSpan.FromMinutes(1),
                    FastReconnectLimit = TimeSpan.FromMinutes(1),
                },
            };


        }
        Type enttype = null;
        bool ObjectsCreated = false;
        string lastentityname = null;
        EntityStructure DataStruct = null;
        Transaction transaction;
        #region "Properties"
        public string GuidID { get; set; }
        public DataSourceType DatasourceType { get; set; } = DataSourceType.RealIM;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.NOSQL;
        public IDataConnection Dataconnection { get; set; }
        public string DatasourceName { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public string Id { get; set; }
        public IDMLogger Logger { get; set; }
        public List<string> EntitiesNames { get; set; } = new List<string>();
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
        public IDMEEditor DMEEditor { get; set; }
        public System.Data.ConnectionState ConnectionStatus { get; set; } = System.Data.ConnectionState.Closed;
        public string ColumnDelimiter { get; set; }
        public string ParameterDelimiter { get; set; }
        #endregion "Properties"
        #region "Events"
        public event EventHandler<PassedArgs> PassEvent;
        public event EventHandler<PassedArgs> OnLoadData;
        public event EventHandler<PassedArgs> OnLoadStructure;
        public event EventHandler<PassedArgs> OnSaveStructure;
        public event EventHandler<PassedArgs> OnCreateStructure;
        public event EventHandler<PassedArgs> OnRefreshData;
        public event EventHandler<PassedArgs> OnRefreshDataEntity;
        public event EventHandler<PassedArgs> OnSyncData;
        #endregion "Events"
        #region "RealM Properties"
        public Realm RealMInstance { get; set; }
        public Realms.Sync.App App { get; set; }
        public Realms.Sync.AppConfiguration AppConfiguration { get; set; }
        public Realms.Sync.User User { get; set; }
        public List<RealmObject> RealMObjects { get; set; } = new List<RealmObject>();
        public List<Type> RealTypeMObjects { get; set; } = new List<Type>();
        public bool IsConnected { get; set; } = false;
        #endregion "RealM Properties"
        #region "INMemory Properties"
        public bool IsCreated { get; set; }
        public bool IsLoaded { get; set; }
        public bool IsSaved { get; set; }
        public bool IsSynced { get; set; }
        public ETLScriptHDR CreateScript { get; set; }
        public bool IsStructureCreated { get; set; } = false;
        public List<EntityStructure> InMemoryStructures { get; set; } = new List<EntityStructure>();
        #endregion "INMemory Properties"
        #region "LocalDb Properties"
        public bool CanCreateLocal { get; set; } = true;
        public bool InMemory { get; set; } = true;
        #endregion "LocalDb Properties"
        #region "CRUD"
        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            IErrorsInfo errorsInfo = DMEEditor.ErrorObject;
            EntityStructure ent = Entities.FirstOrDefault(p => p.EntityName == EntityName);

            try
            {
                if (InsertedData == null)
                {
                    errorsInfo.Flag = Errors.Failed;
                    errorsInfo.Message = "InsertedData is null";
                    return errorsInfo;
                }
                if (Entities.Count == 0 || !Entities.Any(p => p.EntityName == EntityName))
                {
                    DataStruct = GetEntityStructureFromPoco(InsertedData);
                    Entities.Add(DataStruct);
                    if (EntitiesNames.Count == 0 || !EntitiesNames.Contains(EntityName))
                    {
                        EntitiesNames.Add(EntityName);
                    }

                    enttype = GetEntityType(EntityName);

                    SetObjects(EntityName);
                }


                Type realmObjectType = CompileRealmClass(GenerateRealmClass(enttype, ent), EntityName);

                var realmObject = Activator.CreateInstance(realmObjectType);
                CopyProperties(InsertedData, realmObject);

                dynamic existingObject = null;
                var idProperty = enttype.GetProperty(ent.PrimaryKeys.FirstOrDefault().fieldname);
                if (idProperty != null)
                {
                    var idValue = idProperty.GetValue(InsertedData);
                    var method = typeof(Realm).GetMethod("Find");
                    var generic = method.MakeGenericMethod(realmObjectType);
                    existingObject = generic.Invoke(RealMInstance, new[] { idValue });
                }

                if (existingObject != null)
                {
                    errorsInfo.Flag = Errors.Failed;
                    errorsInfo.Message = "Record already exists";
                    return errorsInfo;
                }

                RealMInstance.Write(() =>
                {
                    RealMInstance.Add((RealmObject)realmObject);
                });

                errorsInfo.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {
                errorsInfo.Flag = Errors.Failed;
                errorsInfo.Ex = ex;
            }

            return errorsInfo;
        }
        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            throw new NotImplementedException();
        }
        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {

            IErrorsInfo errorsInfo = new ErrorsInfo();
            EntityStructure ent = Entities.FirstOrDefault(p => p.EntityName == EntityName);
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
        #endregion "CRUD"
        #region "Query" 

        public object RunQuery(string qrystr)
        {
            try
            {
                // Assuming qrystr is in the format "SELECT * FROM EntityName WHERE Condition"
                var queryParts = qrystr.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (queryParts.Length < 4 || !queryParts[0].Equals("SELECT", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("Invalid query format.");
                }

                string entityName = queryParts[3];
                Type entityType = GetEntityTypeByName(entityName);

                if (entityType == null)
                {
                    throw new ArgumentException($"Entity type {entityName} not found.");
                }

                using (var realm = Realm.GetInstance())
                {
                    // Use reflection to call the generic All<T> method
                    var method = typeof(Realm).GetMethod("All", Type.EmptyTypes);
                    var genericMethod = method.MakeGenericMethod(entityType);
                    var result = genericMethod.Invoke(realm, null);

                    // Apply any filtering or other query logic based on qrystr...
                    // Assuming the query does not have a WHERE clause, return the results
                    return result;
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Exception", ex.Message, DateTime.Now, 0, "", Errors.Failed);
                return null;
            }
        }

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
        public object GetEntity(string EntityName, List<AppFilter> filter)
        {
            try
            {
                // Retrieve the entity type by name
                if (DataStruct == null)
                {
                    DataStruct = GetEntityStructure(EntityName, false);
                }
                if (DataStruct.EntityName != EntityName)
                {
                    DataStruct = GetEntityStructure(EntityName, false);
                }
                if (enttype == null)
                {
                    enttype = GetEntityTypeByName(EntityName);

                }
                if (DataStruct == null || enttype == null)
                {
                    DMEEditor.AddLogMessage("Beep", $"Error not able to Get Entity Structure or Type {EntityName}", DateTime.Now, 0, "", Errors.Failed);
                    return null;
                }
                using (var realm = Realm.GetInstance())
                {
                    // Use reflection to call the generic All<T> method
                    var method = typeof(Realm).GetMethod("All", Type.EmptyTypes);
                    var genericMethod = method.MakeGenericMethod(enttype);
                    var query = genericMethod.Invoke(realm, null);

                    // Cast to IQueryable for filtering
                    var queryable = (IQueryable)query;

                    // Apply filters
                    foreach (var fil in filter)
                    {
                        queryable = ApplyFilter(queryable, fil);
                    }

                    // Execute the query and return the results
                    var listMethod = typeof(Enumerable).GetMethod("ToList").MakeGenericMethod(enttype);
                    var resultList = listMethod.Invoke(null, new object[] { queryable });
                    var result = ConvertQueryResultToObservableList((IQueryable)resultList, enttype, DataStruct);
                    return result;
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Exception", ex.Message, DateTime.Now, 0, "", Errors.Failed);
                return null;
            }
        }

        public Task<object> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            return Task.Run(() => GetEntity(EntityName, Filter));
        }

        public List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            throw new NotImplementedException();
        }

        #endregion "Query"
        #region "DDL"
        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            EntityStructure entityStructure = new EntityStructure
            {
                EntityName = EntityName,
                Fields = new List<EntityField>()
            };

            try
            {
                Type realmObjectType = AppDomain.CurrentDomain.GetAssemblies()
                                          .SelectMany(assembly => assembly.GetTypes())
                                          .FirstOrDefault(type => type.Name == EntityName && type.IsSubclassOf(typeof(RealmObject)));

                if (realmObjectType != null)
                {
                    foreach (var property in realmObjectType.GetProperties())
                    {
                        EntityField field = new EntityField
                        {
                            fieldname = property.Name,
                            fieldtype = property.PropertyType.Name,
                            IsKey = property.GetCustomAttributes(typeof(PrimaryKeyAttribute), false).Any(),
                            IsUnique = property.GetCustomAttributes(typeof(IndexedAttribute), false).Any(),
                            AllowDBNull = !property.PropertyType.IsValueType || (Nullable.GetUnderlyingType(property.PropertyType) != null)
                        };

                        entityStructure.Fields.Add(field);

                        if (field.IsKey)
                        {
                            entityStructure.PrimaryKeys.Add(field);
                        }
                    }
                }
                else
                {
                    DMEEditor.AddLogMessage("Error", $"Entity type {EntityName} not found in the current domain", DateTime.Now, 0, "", Errors.Failed);
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Exception", ex.Message, DateTime.Now, 0, "", Errors.Failed);
            }

            return entityStructure;
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            ErrorsInfo retval = new ErrorsInfo();
            retval.Flag = Errors.Ok;
            retval.Message = "Get Entity Structure Successfully";
            EntityStructure result = fnd;
            string EntityName = fnd.EntityName;
            try
            {
                if (refresh == false && Entities.Count > 0)
                {
                    result = Entities.Find(c => c.EntityName.Equals(EntityName, StringComparison.CurrentCultureIgnoreCase));
                    if (result != null)
                    {
                        return result;
                    }
                }
                if (ConnectionStatus != System.Data.ConnectionState.Open)
                {
                    Openconnection();
                }
                if (ConnectionStatus == System.Data.ConnectionState.Open)
                {

                    result = GetEntityStructure(EntityName, refresh);
                }

            }
            catch (Exception ex)
            {
                string methodName = MethodBase.GetCurrentMethod().Name;
                retval.Flag = Errors.Failed;
                retval.Message = ex.Message;
                result = null;
                DMEEditor.AddLogMessage("Beep", $"error in {methodName} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return result;
        }

        public Type GetEntityType(string EntityName)
        {
            ErrorsInfo retval = new ErrorsInfo();
            retval.Flag = Errors.Ok;
            retval.Message = "Get Entity Type  ";

            Type result = null;
            //         SetObjects(EntityName);
            try
            {
                if (ConnectionStatus != System.Data.ConnectionState.Open)
                {
                    Openconnection();
                }
                if (ConnectionStatus == System.Data.ConnectionState.Open)
                {
                    if (EntityName == lastentityname)
                    {
                        if (enttype != null)
                        {
                            result = enttype;
                        }
                    }

                }
                if (result == null)
                {

                    if (DataStruct == null)
                    {
                        lastentityname = EntityName;
                        DataStruct = GetEntityStructure(EntityName, false);
                    }
                    Type realmObjectType = CompileRealmClass(GenerateRealmClass(enttype, DataStruct), EntityName);
                    // DMTypeBuilder.CreateTypeFromCode(DMEEditor,  EntityName, EntityName);
                    result = realmObjectType;
                    if (result != null)
                    {

                        enttype = result;
                    }
                }
            }
            catch (Exception ex)
            {
                string methodName = MethodBase.GetCurrentMethod().Name;
                retval.Flag = Errors.Failed;
                retval.Message = ex.Message;
                result = null;
                DMEEditor.AddLogMessage("Beep", $"error in {methodName} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return result;
        }

        public bool CheckEntityExist(string EntityName)
        {
            return EntitiesNames.Contains(EntityName);
        }
        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {

                for (int i = 0; i < entities.Count; i++)
                {
                    CreateEntityAs(entities[i]);


                }
            }
            catch (Exception ex)
            {
                ConnectionStatus = System.Data.ConnectionState.Broken;
                DMEEditor.AddLogMessage("Beep", $"Error in Opening RealM : {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return ErrorObject;
        }
        public bool CreateEntityAs(EntityStructure entity)
        {
            try
            {
                // Generate the Realm class code from the EntityStructure
                DataStruct = entity;
                enttype = CompileRealmClass(GenerateRealmClass(enttype, DataStruct), entity.EntityName);
                string classCode = GenerateRealmClass(enttype, entity);

                // Compile the generated class code
                Type realmObjectType = CompileRealmClass(classCode, $"{entity.EntityName}");

                // Create an instance of the generated Realm class
                var realmObject = Activator.CreateInstance(realmObjectType);

                // Add the new object to the Realm database
                using (var realm = Realm.GetInstance())
                {
                    realm.Write(() =>
                    {
                        realm.Add((RealmObject)realmObject);
                    });
                }

                return true;
            }
            catch (Exception ex)
            {
                // Log the error
                DMEEditor.AddLogMessage("Exception", ex.Message, DateTime.Now, 0, "", Errors.Failed);
                return false;
            }
        }
        #endregion "DDL"
        #region "DML"
        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
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
                        EntityStructure ent = GetEntityStructureFromRealmObject(obj);
                        EntitiesNames.Add(item.Name);
                    }

                }
            }
            return EntitiesNames;
        }


        public int GetEntityIdx(string entityName)
        {
            throw new NotImplementedException();
        }
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

                if (transaction == null)
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
        #endregion "DML"
        private void SetObjects(string Entityname)
        {
            if (!ObjectsCreated || Entityname != lastentityname)
            {
                if (DataStruct.EntityName != Entityname)
                {
                    DataStruct = GetEntityStructure(Entityname, false);
                }

                enttype = GetEntityType(Entityname);
                ObjectsCreated = true;
                lastentityname = Entityname;
            }
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
        public void Dispose()
        {
            throw new NotImplementedException();
        }
        #region "RealM Functions"
        FlexibleSyncConfiguration syncConfiguration;
        public string Appid { get; set; }
        public async Task<IErrorsInfo> CreateFlexSyncRealm()
        {
            try
            {
                if (User == null || User.State != UserState.LoggedIn)
                {
                    throw new InvalidOperationException("User is not logged in.");
                }

                // Create a dictionary to store generated Realm object types
                var realmObjectTypes = new Dictionary<string, Type>();

                foreach (var entity in Entities)
                {
                    // Generate and compile the Realm object class for each entity
                    Type realmObjectType = CompileRealmClass(GenerateRealmClassFromEntityStructure(entity), entity.EntityName);
                    realmObjectTypes.Add(entity.EntityName, realmObjectType);
                }

                var config = new FlexibleSyncConfiguration(User)
                {
                    PopulateInitialSubscriptions = (realm) =>
                    {
                        foreach (var entity in Entities)
                        {
                            Type realmObjectType = realmObjectTypes[entity.EntityName];

                            // Use reflection to call the generic All<T> method
                            var method = typeof(Realm).GetMethod("All", Type.EmptyTypes);
                            var genericMethod = method.MakeGenericMethod(realmObjectType);
                            var allObjects = genericMethod.Invoke(realm, null);

                            var subscriptionsAddMethod = typeof(SubscriptionSet).GetMethod("Add", new Type[] { allObjects.GetType(), typeof(SubscriptionOptions) });
                            subscriptionsAddMethod.Invoke(realm.Subscriptions, new object[] { allObjects, new SubscriptionOptions { Name = $"all{entity.EntityName}" } });
                        }
                    }
                };

                // Create the Realm instance with the configuration
                RealMInstance = await Realm.GetInstanceAsync(config);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Exception", ex.Message, DateTime.Now, 0, "", Errors.Failed);
                return DMEEditor.ErrorObject;
            }

            return DMEEditor.ErrorObject;
        }
        public async Task<IErrorsInfo> CreateAsync()
        {
            try
            {
                if(Appid==null)
                {
                    Appid = Dataconnection.ConnectionProp.GuidID;
                }
                if(Appid==null)
                {
                    Appid =Guid.NewGuid().ToString();
                }
                AppConfiguration=new AppConfiguration(Appid);
                
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
                if (App == null)
                {
                    CreateAsync();
                }
                Dataconnection.ConnectionProp.UserID = username;
                Dataconnection.ConnectionProp.Password = password;
                User = await App.LogInAsync(Credentials.EmailPassword(username, password));
                IsConnected = true;
            }
            catch (Exception ex)
            {
                IsConnected = false;
                DMEEditor.AddLogMessage("Beep", $"Could not login in Realm Database {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public async Task<IErrorsInfo> LoginAsync()
        {
            try
            {
                // Login with existing user
                if (App == null)
                {
                    CreateAsync();
                }
               
                User = await App.LogInAsync(Credentials.Anonymous());
                IsConnected = true;
            }
            catch (Exception ex)
            {
                IsConnected = false;
                DMEEditor.AddLogMessage("Beep", $"Could not login in Realm Database {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public async Task<IErrorsInfo> LoginAsync(string apiKey)
        {
            try
            {
                if (string.IsNullOrEmpty(apiKey))
                {
                    apiKey = Dataconnection.ConnectionProp.ApiKey;
                }else
                {
                    Dataconnection.ConnectionProp.ApiKey = apiKey;
                }
                // Login with existing user
                if (App == null)
                {
                    CreateAsync();
                }

                User = await App.LogInAsync(Credentials.ApiKey(apiKey));
                IsConnected = true;
            }
            catch (Exception ex)
            {
                IsConnected = false;
                DMEEditor.AddLogMessage("Beep", $"Could not login in Realm Database {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public async Task<IErrorsInfo> LoginAsyncUsingGoogle(string googleAuthCode)
        {
            try
            {
                if (string.IsNullOrEmpty(googleAuthCode))
                {
                    googleAuthCode = Dataconnection.ConnectionProp.ApiKey;
                }
                else
                {
                    Dataconnection.ConnectionProp.ApiKey = googleAuthCode;
                }
                // Login with existing user
                if (App == null)
                {
                    CreateAsync();
                }

                User = await App.LogInAsync(Credentials.Google(googleAuthCode, GoogleCredentialType.AuthCode));

                IsConnected = true;
            }
            catch (Exception ex)
            {
                IsConnected = false;
                DMEEditor.AddLogMessage("Beep", $"Could not login in Realm Database {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public async Task<IErrorsInfo> LoginAsyncUsingApple(string appleToken)
        {
            try
            {
                if (string.IsNullOrEmpty(appleToken))
                {
                    appleToken = Dataconnection.ConnectionProp.KeyToken;
                }
                else
                {
                    Dataconnection.ConnectionProp.KeyToken = appleToken;
                }
                // Login with existing user
                if (App == null)
                {
                    CreateAsync();
                }

                User = await App.LogInAsync(Credentials.Apple(appleToken));

                IsConnected = true;
            }
            catch (Exception ex)
            {
                IsConnected = false;
                DMEEditor.AddLogMessage("Beep", $"Could not login in Realm Database {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        // Returns a valid user access token to authenticate requests
        public async Task<string> GetValidAccessToken(User user)
        {
            // An already logged in user's access token might be stale. To
            // guarantee that the token is valid, refresh it.
            await user.RefreshCustomDataAsync();
            return user.AccessToken;
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
        #endregion
        #region "RealM Util"
     
        public async Task<IErrorsInfo> CreateFlexSyncRealm(EntityStructure entityStructure)
        {
            try
            {
                if (User == null || User.State != UserState.LoggedIn)
                {
                    throw new InvalidOperationException("User is not logged in.");
                }

                // Generate and compile the Realm object class
                Type realmObjectType = CompileRealmClass(GenerateRealmClassFromEntityStructure(entityStructure),entityStructure.EntityName);
                var config = new FlexibleSyncConfiguration(User)
                {
                    PopulateInitialSubscriptions = (realm) =>
                    {
                        // Use reflection to call the generic All<T> method
                        var method = typeof(Realm).GetMethod("All", Type.EmptyTypes);
                        var genericMethod = method.MakeGenericMethod(realmObjectType);
                        var allObjects = genericMethod.Invoke(realm, null);
                        var subscriptionsAddMethod = typeof(Realm).GetMethod("Add", new Type[] { allObjects.GetType(), typeof(SubscriptionOptions) });
                        subscriptionsAddMethod.Invoke(realm.Subscriptions, new object[] { allObjects, new SubscriptionOptions { Name = "allObjects" } });
                    }
                };

                // Create the Realm instance with the configuration
                var realm = await Realm.GetInstanceAsync(config);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Exception", ex.Message, DateTime.Now, 0, "", Errors.Failed);
                return DMEEditor.ErrorObject;
            }

            return DMEEditor.ErrorObject;
        }
        private string GenerateRealmClassFromEntityStructure(EntityStructure structure)
        {
            var classBuilder = new StringBuilder();

            classBuilder.AppendLine("using Realms;");
            classBuilder.AppendLine();
            classBuilder.AppendLine($"public class {structure.EntityName}Realm : RealmObject");
            classBuilder.AppendLine("{");

            foreach (var field in structure.Fields)
            {
                string propertyType = field.fieldtype;
                string propertyName = field.fieldname;

                if (field.IsKey)
                {
                    classBuilder.AppendLine("    [PrimaryKey]");
                    classBuilder.AppendLine("    [MapTo(\"_id\")]");
                    classBuilder.AppendLine("    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();");
                }
                else
                {
                    classBuilder.AppendLine($"    public {propertyType} {propertyName} {{ get; set; }}");
                }
                classBuilder.AppendLine();
            }

            classBuilder.AppendLine("}");

            return classBuilder.ToString();
        }
        private Type GetEntityTypeByName(string entityName)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase));
        }
        private void CopyProperties(object source, object target)
        {
            var sourceProperties = source.GetType().GetProperties();
            var targetProperties = target.GetType().GetProperties();

            foreach (var property in sourceProperties)
            {
                var targetProperty = targetProperties.FirstOrDefault(p => p.Name == property.Name);
                if (targetProperty != null && targetProperty.CanWrite)
                {
                    targetProperty.SetValue(target, property.GetValue(source));
                }
            }
        }
        private string GetRealmType(Type propertyType)
        {
            // Map common types to Realm-supported types
            if (propertyType == typeof(int) || propertyType == typeof(long) || propertyType == typeof(short))
            {
                return "long";
            }
            if (propertyType == typeof(float) || propertyType == typeof(double) || propertyType == typeof(decimal))
            {
                return "double";
            }
            if (propertyType == typeof(bool))
            {
                return "bool";
            }
            if (propertyType == typeof(string))
            {
                return "string";
            }
            if (propertyType == typeof(DateTime))
            {
                return "DateTimeOffset";
            }
            if (propertyType == typeof(ObjectId))
            {
                return "ObjectId";
            }

            // Handle nullable types
            if (Nullable.GetUnderlyingType(propertyType) != null)
            {
                return GetRealmType(Nullable.GetUnderlyingType(propertyType)) + "?";
            }

            return propertyType.Name;
        }
        public string GenerateRealmClass(Type pocoType,EntityStructure structure)
        {
            var classBuilder = new StringBuilder();

            // Start class definition
            classBuilder.AppendLine("using Realms;");
            classBuilder.AppendLine();
            classBuilder.AppendLine($"public class {structure.EntityName} : IRealmObject");
            classBuilder.AppendLine("{");

            // Add properties
            foreach (var property in pocoType.GetProperties())
            {
                string propertyType = GetRealmType(property.PropertyType);
                string propertyName = property.Name;

                // Add PrimaryKey attribute if it is Id
                if (propertyName.Equals("Id", StringComparison.OrdinalIgnoreCase))
                {
                    classBuilder.AppendLine("    [PrimaryKey]");
                    classBuilder.AppendLine("    [MapTo(\"_id\")]");
                    classBuilder.AppendLine("    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();");
                }
                else
                {
                    classBuilder.AppendLine($"    public {propertyType} {propertyName} {{ get; set; }}");
                }
                classBuilder.AppendLine();
            }


            // End class definition
            classBuilder.AppendLine("}");

            return classBuilder.ToString();
        }
        public string GenerateRealmClass(Type pocoType)
        {
            var classBuilder = new StringBuilder();

            // Start class definition
            classBuilder.AppendLine("using Realms;");
            classBuilder.AppendLine();
            classBuilder.AppendLine($"public class {pocoType.Name} : IRealmObject");
            classBuilder.AppendLine("{");

            // Add properties
            // Add properties
            foreach (var property in pocoType.GetProperties())
            {
                string propertyType = GetRealmType(property.PropertyType);
                string propertyName = property.Name;

                // Add PrimaryKey attribute if it is Id
                if (propertyName.Equals("Id", StringComparison.OrdinalIgnoreCase))
                {
                    classBuilder.AppendLine("    [PrimaryKey]");
                    classBuilder.AppendLine("    [MapTo(\"_id\")]");
                    classBuilder.AppendLine("    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();");
                }
                else
                {
                    classBuilder.AppendLine($"    public {propertyType} {propertyName} {{ get; set; }}");
                }
                classBuilder.AppendLine();
            }

            // End class definition
            classBuilder.AppendLine("}");

            return classBuilder.ToString();
        }
        public EntityStructure GetEntityStructureFromPoco(object poco)
        {
            EntityStructure entity = new EntityStructure
            {
                EntityName = poco.GetType().Name,
                Fields = new List<EntityField>(),
                PrimaryKeys = new List<EntityField>()
            };

            Type tp = poco.GetType();
            var properties = tp.GetProperties();

            foreach (var property in properties)
            {
                EntityField field = new EntityField
                {
                    fieldname = property.Name,
                    fieldtype = GetRealmType(property.PropertyType),
                    IsKey = property.GetCustomAttributes(typeof(PrimaryKeyAttribute), false).Any(),
                    Originalfieldname = property.GetCustomAttributes<MapToAttribute>(false).FirstOrDefault()?.Mapping ?? property.Name,
                    AllowDBNull = !property.PropertyType.IsValueType || Nullable.GetUnderlyingType(property.PropertyType) != null
                };

                if (field.IsKey)
                {
                    entity.PrimaryKeys.Add(field);
                }

                entity.Fields.Add(field);
            }

            return entity;
        }
        public EntityStructure GetEntityStructureFromRealmObject(RealmObject realmObject)
        {
            EntityStructure entity = new EntityStructure
            {
                EntityName = realmObject.GetType().Name,
                Fields = new List<EntityField>(),
                PrimaryKeys = new List<EntityField>()
            };

            Type tp = realmObject.GetType();
            var properties = tp.GetProperties();

            foreach (var property in properties)
            {
                EntityField field = new EntityField
                {
                    fieldname = property.Name,
                    fieldtype = GetRealmType(property.PropertyType),
                    IsKey = property.GetCustomAttributes(typeof(PrimaryKeyAttribute), false).Any(),
                    Originalfieldname = property.GetCustomAttributes<MapToAttribute>(false).FirstOrDefault()?.Mapping ?? property.Name,
                    AllowDBNull = !property.PropertyType.IsValueType || Nullable.GetUnderlyingType(property.PropertyType) != null
                };

                if (field.IsKey)
                {
                    entity.PrimaryKeys.Add(field);
                }

                entity.Fields.Add(field);
            }

            return entity;
        }
        public object GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            throw new NotImplementedException();
        }
        private Type CompileRealmClass(string classCode, string className)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(classCode);
            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .Select(a => MetadataReference.CreateFromFile(a.Location))
                .Cast<MetadataReference>()
                .ToList();

            var compilation = CSharpCompilation.Create("GeneratedRealmClasses")
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddReferences(references)
                .AddSyntaxTrees(syntaxTree);

            using (var ms = new System.IO.MemoryStream())
            {
                var result = compilation.Emit(ms);
                if (!result.Success)
                {
                    var failures = result.Diagnostics.Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);
                    foreach (var diagnostic in failures)
                    {
                        Console.Error.WriteLine($"{diagnostic.Id}: {diagnostic.GetMessage()}");
                    }
                    throw new InvalidOperationException("Failed to compile the Realm class.");
                }
                else
                {
                    ms.Seek(0, System.IO.SeekOrigin.Begin);
                    var assembly = Assembly.Load(ms.ToArray());
                    return assembly.GetType(className);
                }
            }
        }
        private IQueryable ApplyFilter(IQueryable query, AppFilter filter)
        {
            // This method should build and apply a filter expression based on the AppFilter
            // For simplicity, we'll assume AppFilter has a Field, Operator, and Value properties
            // This example handles basic equality, greater than, and less than operators

            var parameter = Expression.Parameter(query.ElementType, "x");
            var member = Expression.Property(parameter, filter.FieldName);
            var constant = Expression.Constant(Convert.ChangeType(filter.FilterValue, member.Type));
            Expression body = null;

            switch (filter.Operator)
            {
                case "==":
                    body = Expression.Equal(member, constant);
                    break;
                case ">":
                    body = Expression.GreaterThan(member, constant);
                    break;
                case "<":
                    body = Expression.LessThan(member, constant);
                    break;
                    // Add more cases as needed
            }

            var predicate = Expression.Lambda(body, parameter);
            var methodCall = Expression.Call(
                typeof(Queryable),
                "Where",
                new Type[] { query.ElementType },
                query.Expression,
                Expression.Quote(predicate)
            );

            return query.Provider.CreateQuery(methodCall);
        }
        public object ConvertQueryResultToObservableList(IQueryable queryable, Type type, EntityStructure entStructure)
        {
            Type uowGenericType = typeof(ObservableBindingList<>).MakeGenericType(type);
            var records = (IBindingListView)Activator.CreateInstance(uowGenericType);

            foreach (var item in queryable)
            {
                dynamic instance = Activator.CreateInstance(type);
                foreach (var field in entStructure.Fields)
                {
                    var propertyName = field.fieldname;
                    var propertyInfo = item.GetType().GetProperty(propertyName);
                    if (propertyInfo != null)
                    {
                        var value = propertyInfo.GetValue(item);
                        try
                        {
                            string netTypeString = field.fieldtype;
                            Type netType = Type.GetType(netTypeString);

                            if (netType == typeof(string) && value is ObjectId)
                            {
                                type.GetProperty(field.fieldname).SetValue(instance, value.ToString());
                            }
                            else if (Type.GetTypeCode(netType) == Type.GetTypeCode(value.GetType()))
                            {
                                object convertedValue = Convert.ChangeType(value, netType);
                                type.GetProperty(field.fieldname).SetValue(instance, convertedValue);
                            }
                            else
                            {
                                object convertedValue = Convert.ChangeType(value, netType);
                                type.GetProperty(field.fieldname).SetValue(instance, convertedValue);
                            }
                        }
                        catch (Exception ex)
                        {
                            DMEEditor.AddLogMessage("Beep", $"Error setting property {field.fieldname} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                        }
                    }
                }
                records.Add(instance);
            }

            return records;
        }
        public object ConvertRealmObjectsToObservableList(List<RealmObject> realmObjects, Type type, EntityStructure entStructure)
        {
            Type uowGenericType = typeof(ObservableBindingList<>).MakeGenericType(type);
            var records = (IBindingListView)Activator.CreateInstance(uowGenericType);

            foreach (var realmObject in realmObjects)
            {
                dynamic instance = Activator.CreateInstance(type);
                foreach (var field in entStructure.Fields)
                {
                    var propertyName = field.fieldname;
                    var propertyInfo = realmObject.GetType().GetProperty(propertyName);
                    if (propertyInfo != null)
                    {
                        var value = propertyInfo.GetValue(realmObject);
                        try
                        {
                            string netTypeString = field.fieldtype;
                            Type netType = Type.GetType(netTypeString);

                            if (netType == typeof(string) && value is ObjectId)
                            {
                                type.GetProperty(field.fieldname).SetValue(instance, value.ToString());
                            }
                            else if (Type.GetTypeCode(netType) == Type.GetTypeCode(value.GetType()))
                            {
                                object convertedValue = Convert.ChangeType(value, netType);
                                type.GetProperty(field.fieldname).SetValue(instance, convertedValue);
                            }
                            else
                            {
                                object convertedValue = Convert.ChangeType(value, netType);
                                type.GetProperty(field.fieldname).SetValue(instance, convertedValue);
                            }
                        }
                        catch (Exception ex)
                        {
                            DMEEditor.AddLogMessage("Beep", $"Error setting property {field.fieldname} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                        }
                    }
                }
                records.Add(instance);
            }

            return records;
        }
        private object ConvertBsonValueToNetType(BsonValue bsonValue, Type targetType)
        {
            if (bsonValue.IsString) return bsonValue.AsString;
            if (bsonValue.IsInt32) return bsonValue.AsInt32;
            if (bsonValue.IsInt64) return bsonValue.AsInt64;
            if (bsonValue.IsBoolean) return bsonValue.AsBoolean;
            if (bsonValue.IsDouble) return bsonValue.AsDouble;
            if (bsonValue.IsDateTime) return bsonValue.ToUniversalTime();

            throw new InvalidOperationException($"Unsupported BsonValue type: {bsonValue.BsonType}");
        }
        private string RemoveQuotes(string value)
        {
            if (value.StartsWith("\"") && value.EndsWith("\""))
            {
                value = value.Substring(1, value.Length - 2);
            }
            return value;
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
        private string GetCSharpType(string fieldType)
        {
            switch (fieldType.ToLower())
            {
                case "int":
                case "integer":
                case "int32":
                    return "int";
                case "long":
                case "int64":
                    return "long";
                case "short":
                case "int16":
                    return "short";
                case "float":
                    return "float";
                case "double":
                    return "double";
                case "decimal":
                    return "decimal";
                case "bool":
                case "boolean":
                    return "bool";
                case "string":
                    return "string";
                case "datetime":
                case "datetimeoffset":
                    return "DateTimeOffset";
                case "objectid":
                    return "ObjectId";
                default:
                    if (fieldType.EndsWith("?"))
                    {
                        return GetCSharpType(fieldType.TrimEnd('?')) + "?";
                    }
                    return fieldType;
            }
        }
        #endregion "RealM Util"
        #region "LocalDB Methods"
        RealmConfiguration realmConfiguration;
        public bool CreateDB()
        {
            bool retval=false;
            try
            {
                IsCreated = true;
                InMemory = false;
                realmConfiguration = new RealmConfiguration("my.realm");
                 RealMInstance = Realm.GetInstance(realmConfiguration);
                retval=true;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error in Opening RealM : {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
                retval = false;
            }
            return retval;
            
        }

        public bool CreateDB(bool inMemory)
        {
            throw new NotImplementedException();
        }

        public bool CreateDB(string filepathandname)
        {
            throw new NotImplementedException();
        }

        public bool DeleteDB()
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo DropEntity(string EntityName)
        {
            throw new NotImplementedException();
        }

        public bool CopyDB(string DestDbName, string DesPath)
        {
            throw new NotImplementedException();
        }

        #endregion "LocalDB Methods"
        #region "InMemory Functions"
        InMemoryConfiguration MemoryConfiguration;
        string InMemoryIdentity;
        public IErrorsInfo OpenDatabaseInMemory(string databasename)
        {
            try
            {
                InMemory=true;
                InMemoryIdentity=databasename;
                MemoryConfiguration = new InMemoryConfiguration(InMemoryIdentity);
                RealMInstance = Realm.GetInstance(MemoryConfiguration);

            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error in Opening RealM : {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        public string GetConnectionString()
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo SaveStructure()
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

        public IErrorsInfo LoadData(Progress<PassedArgs> progress, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo SyncData(Progress<PassedArgs> progress, CancellationToken token)
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
        #endregion "InMemory Functions"
    }




}
