
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;

using TheTechIdea.Logger;
using TheTechIdea.Util;
using TheTechIdea.Beep.WebAPI;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;
using System.Reflection;
using MongoDB.Bson;
using TheTechIdea.Beep.Helpers;
using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization;
using DataManagementModels.DriversConfigurations;
using DataManagementModels.Editor;
using System.Collections.Generic;
using System.Linq;
using SharpCompress.Common;
using System.ComponentModel;
using Microsoft.CodeAnalysis;
using System.Collections;
using System.Text.RegularExpressions;
using MongoDB.Driver.Core.Events;



namespace TheTechIdea.Beep.NOSQL
{
    [AddinAttribute(Category = DatasourceCategory.NOSQL, DatasourceType = DataSourceType.MongoDB)]
    public class MongoDBDataSource : IDataSource
    {
        public string GuidID { get; set; }
        public event EventHandler<PassedArgs> PassEvent;
        public DataSourceType DatasourceType { get; set; }
        public DatasourceCategory Category { get; set; }
        public IDataConnection Dataconnection { get; set; }
        public string DatasourceName { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public string Id { get; set; }
        public IDMLogger Logger { get; set; }
        public List<string> EntitiesNames { get; set; }
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
        public IDMEEditor DMEEditor { get; set; }
        public List<object> Records { get; set; }
        public ConnectionState ConnectionStatus { get; set; }
        public DataTable SourceEntityData { get; set; }
        public MongoClientSettings Settings { get; set; } 
        public string CurrentDatabase { get { return Dataconnection.ConnectionProp.Database; } set { Dataconnection.ConnectionProp.Database = value; } }
        public virtual string ColumnDelimiter { get; set; } = "''";
        public virtual string ParameterDelimiter { get; set; } = ":";
        #region "MongoDB Properties"
        private IMongoClient _client;
        private IMongoDatabase _database;
        private string _connectionString;
        private IClientSessionHandle _session;
        #endregion
        // MongoDBReader Reader;
        public MongoDBDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
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
            if (DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == datasourcename).Any())
            {
                Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == datasourcename).FirstOrDefault();
            }else
            {
                ConnectionDriversConfig driversConfig= DMEEditor.ConfigEditor.DataDriversClasses.FirstOrDefault(p => p.DatasourceType == databasetype);
                Dataconnection.ConnectionProp = new ConnectionProperties
                {
                    ConnectionName = datasourcename,
                    ConnectionString = driversConfig.ConnectionString,
                    DriverName = driversConfig.PackageName,
                    DriverVersion = driversConfig.version,
                    DatabaseType = DataSourceType.MongoDB,
                    Category = DatasourceCategory.NOSQL
                };
            }
           
            Dataconnection.ConnectionProp.Category = DatasourceCategory.NOSQL;
            Dataconnection.ConnectionProp.DatabaseType = DataSourceType.MongoDB;
            _connectionString = Dataconnection.ConnectionProp.ConnectionString;
            CurrentDatabase = Dataconnection.ConnectionProp.Database;
            Settings = new MongoClientSettings();
            //if (CurrentDatabase != null)
            //{
            //    if (CurrentDatabase.Length > 0)
            //    {
            //        _client = new MongoClient(_connectionString);
            //        GetEntitesList();
            //    }
            //}

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
            if (string.IsNullOrEmpty(CurrentDatabase))
            {
                var match = Regex.Match(_connectionString, @"\/(?<database>[^\/\?]+)(\?|$)");
                if (match.Success)
                {
                    CurrentDatabase = match.Groups["database"].Value;
                }
            }
        }
        public virtual Task<double> GetScalarAsync(string query)
        {
            return Task.Run(() => GetScalar(query));
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


        Type enttype = null;
        bool ObjectsCreated = false;
        string lastentityname = null;
        EntityStructure DataStruct = null;


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
        public virtual IErrorsInfo BeginTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (_session == null || !_session.IsInTransaction)
                {
                    _session = _client.StartSession();
                    _session.StartTransaction();
                    DMEEditor.AddLogMessage("Beep", "Transaction started successfully.", DateTime.Now, 0, null, Errors.Ok);
                }
                else
                {
                    DMEEditor.AddLogMessage("Beep", $"Error in Begin Transaction ", DateTime.Now, 0, null, Errors.Failed);
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
                if (_session == null)
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = "No active transaction to end.";
                    return DMEEditor.ErrorObject;
                }

                if (ErrorObject.Flag == Errors.Ok)
                {
                    _session.CommitTransaction(); // Commit if no errors
                    DMEEditor.AddLogMessage("Beep", "Transaction committed successfully.", DateTime.Now, 0, null, Errors.Ok);
                }
                else
                {
                    _session.AbortTransaction(); // Abort if errors
                    DMEEditor.AddLogMessage("Beep", "Transaction aborted due to errors.", DateTime.Now, 0, null, Errors.Failed);
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor.AddLogMessage("Beep", $"Error in End Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            finally
            {
                _session.Dispose(); // Dispose the session
                _session = null; // Reset the session variable
            }
            return DMEEditor.ErrorObject;
        }
        public virtual IErrorsInfo Commit(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (_session == null)
                {
                    DMEEditor.AddLogMessage("Beep", $"Transaction session is not started.", DateTime.Now, 0, null, Errors.Failed);
                }

                _session.CommitTransaction(); // Commit the transaction
                DMEEditor.AddLogMessage("Beep", "Transaction committed successfully.", DateTime.Now, 0, null, Errors.Ok);
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
            try
            {
                if (_client == null) // Assuming _client is the MongoClient instance
                {
                    HandleConnectionStringforMongoDB();
                    Settings = MongoClientSettings.FromConnectionString(_connectionString);
                    var pas = new PassedArgs();
                    pas.ObjectName = "MongoDB";
                    ObjectItem objectItem=new ObjectItem() {  Name= "MongoDB", obj=Settings };
                    pas.Objects.Add(objectItem);
                    PassEvent?.Invoke(this, pas);
                    //Settings.UseTls = true;

                    //Settings.SslSettings = new SslSettings
                    //{

                    //    EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 // Ensure this matches the server configuration
                    //};
                    //Settings.ClusterConfigurator = cb =>
                    //{
                    //    cb.Subscribe<CommandStartedEvent>(e =>
                    //    {
                    //        Console.WriteLine($"CommandStartedEvent: {e.CommandName} - {e.Command.ToJson()}");
                    //    });
                    //    cb.Subscribe<CommandSucceededEvent>(e =>
                    //    {
                    //        Console.WriteLine($"CommandSucceededEvent: {e.CommandName} - {e.Reply.ToJson()}");
                    //    });
                    //    cb.Subscribe<CommandFailedEvent>(e =>
                    //    {
                    //        Console.WriteLine($"CommandFailedEvent: {e.CommandName} - {e.Failure}");
                    //    });
                    //};
                    // Connection string should be stored securely or configured outside of the codebase
                    //string connectionString = Dataconnection.ConnectionProp.ConnectionString;
                    _client = new MongoClient(Settings);
                    _database = _client.GetDatabase(CurrentDatabase);
                  }
                    // Now check if the client is connected using the ping command
                    if (IsMongoDBConnected(_client))
                    {
                        ConnectionStatus = ConnectionState.Open;
                        DMEEditor.AddLogMessage("Beep", "Connection to MongoDB opened successfully.", DateTime.Now, -1, null, Errors.Ok);
                    }
                    else
                    {
                        ConnectionStatus = ConnectionState.Closed;
                        DMEEditor.AddLogMessage("Beep", "Failed to open connection to MongoDB.", DateTime.Now, -1, null, Errors.Failed);
                    }
                }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Closed;
                DMEEditor.AddLogMessage("Beep", $"Could not open MongoDB {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ConnectionStatus;
        }
        public ConnectionState Closeconnection()
        {
            try
            {
                // MongoDB driver handles connection pooling, so you typically do not need to close connections manually
                // However, you can dispose of the client if absolutely necessary
                if (_client != null)
                {
                    _client = null; // Properly dispose of the MongoClient if needed
                    ConnectionStatus = ConnectionState.Closed;
                }
                else
                {
                    DMEEditor.AddLogMessage("Beep", "MongoDB connection was not open or already closed.", DateTime.Now, -1, null, Errors.Ok);
                }
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                DMEEditor.AddLogMessage("Beep", $"Could not close MongoDB {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ConnectionStatus;
        }
        public bool CheckEntityExist(string EntityName)
        {
            ErrorsInfo erretval = new ErrorsInfo();
            erretval.Flag = Errors.Ok;
            erretval.Message = "Executed Successfully "; ;
            bool retval = false;
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }
                if (ConnectionStatus == ConnectionState.Open)
                {
                    var database = _client.GetDatabase(CurrentDatabase);  // Assume _client and CurrentDatabase are already set

                    // Check if the collection already exists
                    var filter = new BsonDocument("name", EntityName);
                    var collections = database.ListCollections(new ListCollectionsOptions { Filter = filter });
                    var exists = collections.Any();  // Check if any collections returned with that name

                    if (exists)
                    {
                        retval = true;
                    }
                    else
                    {
                        retval = false;
                        DMEEditor.AddLogMessage("Beep", "Collection already exists.", DateTime.Now, -1, null, Errors.Failed);
                    }

                }
                else
                {
                    erretval.Flag = Errors.Failed;
                    erretval.Message = "Could not open connection";
                }

                retval = true;
            }
            catch (Exception ex)
            {
                string methodName = MethodBase.GetCurrentMethod().Name;
                retval = false;
                DMEEditor.AddLogMessage("Beep", $"error in {methodName} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return retval;
        }
        public bool CreateEntityAs(EntityStructure entity)
        {
            ErrorsInfo erretval = new ErrorsInfo();
            erretval.Flag = Errors.Ok;
            erretval.Message = "Executed Successfully "; ;
            bool retval = false;
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }
                if (ConnectionStatus == ConnectionState.Open)
                {
                    var database = _client.GetDatabase(CurrentDatabase);  // Assume _client and CurrentDatabase are already set

                    // Check if the collection already exists
                    var filter = new BsonDocument("name", entity.EntityName);
                    var collections = database.ListCollections(new ListCollectionsOptions { Filter = filter });
                    var exists = collections.Any();  // Check if any collections returned with that name

                    if (!exists)
                    {
                        // Create the collection
                        database.CreateCollection(entity.EntityName);

                        // Optionally apply validation rules if the EntityStructure specifies any schema constraints
                        //if (entity.Fields != null && entity.Fields.Any())
                        //{
                        //    var validationDocument = GenerateValidationDocument(entity);
                        //    var command = new BsonDocumentCommand<BsonDocument>(
                        //        new BsonDocument { { "collMod", entity.EntityName }, { "validator", new BsonDocument { { "$jsonSchema", validationDocument } } } });

                        //    database.RunCommand(command);
                        //}
                        Entities.Add(entity);
                        EntitiesNames.Add(entity.EntityName);
                        DMEEditor.AddLogMessage("Beep", "Collection created successfully.", DateTime.Now, -1, null, Errors.Ok);
                        retval = true;
                    }
                    else
                    {
                        DMEEditor.AddLogMessage("Beep", "Collection already exists.", DateTime.Now, -1, null, Errors.Failed);
                    }

                }
                else
                {
                    erretval.Flag = Errors.Failed;
                    erretval.Message = "Could not open connection";
                }

                retval = true;
            }
            catch (Exception ex)
            {
                string methodName = MethodBase.GetCurrentMethod().Name;
                retval = false;
                DMEEditor.AddLogMessage("Beep", $"error in {methodName} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return retval;
        }
        public IErrorsInfo ExecuteSql(string sql)
        {
            ErrorsInfo retval = new ErrorsInfo();
            retval.Flag = Errors.Ok;
            retval.Message = "Executed Successfully "; ;
            try
            {

            }
            catch (Exception ex)
            {
                string methodName = MethodBase.GetCurrentMethod().Name;
                retval.Flag = Errors.Failed;
                retval.Message = ex.Message;
                DMEEditor.AddLogMessage("Beep", $"error in {methodName} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return retval;
        }
        public List<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            ErrorsInfo retval = new ErrorsInfo();
            retval.Flag = Errors.Ok;
            retval.Message = "Get Entity Successfully";
            List<ChildRelation> relations = new List<ChildRelation>();
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }
                if (ConnectionStatus == ConnectionState.Open)
                {
                    //var db = _client.GetDatabase(CurrentDatabase);
                    //var collectionNames = db.ListCollectionNames().ToList();
                    //foreach (var item in collectionNames)
                    //{
                    //    ChildRelation rel = new ChildRelation();
                    //    rel.child_table = item;
                    //    relations.Add(rel);
                    //}

                }
                else
                {
                    retval.Flag = Errors.Failed;
                    retval.Message = "Could not open connection";
                }
            }
            catch (Exception ex)
            {
                string methodName = MethodBase.GetCurrentMethod().Name;
                retval.Flag = Errors.Failed;
                retval.Message = ex.Message;
                DMEEditor.AddLogMessage("Beep", $"error in {methodName} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return relations;
        }
        public DataSet GetChildTablesListFromCustomQuery(string tablename, string customquery)
        {
            ErrorsInfo retval = new ErrorsInfo();
            retval.Flag = Errors.Ok;
            retval.Message = "Get Entity Successfully";
            DataSet dataset = new DataSet();
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }
                if (ConnectionStatus == ConnectionState.Open)
                {
                    //Get data from the custom query and put it in the dataset
                    var db = _client.GetDatabase(CurrentDatabase);
                    var collectionNames = db.ListCollectionNames().ToList();
                    //get data from the table or  collection and put in a table and add to the dataset
                    // fetch data from the colloction and put in a datatable
                    DataTable dt = new DataTable();
                    dt.TableName = tablename;
                    //get collection by name TableName
                    var collection = db.GetCollection<BsonDocument>(tablename);
                    var filter = Builders<BsonDocument>.Filter.Empty;
                    var documents = collection.Find(filter).ToList();
                    if (documents.Count > 0)
                    {
                        foreach (var item in documents[0].Elements)
                        {
                            dt.Columns.Add(item.Name);
                        }
                        foreach (var item in documents)
                        {
                            DataRow dr = dt.NewRow();
                            foreach (var item2 in item.Elements)
                            {
                                dr[item2.Name] = item2.Value;
                            }
                            dt.Rows.Add(dr);
                        }
                    }
                    dataset.Tables.Add(dt);



                }
                else
                {
                    retval.Flag = Errors.Failed;
                    retval.Message = "Could not open connection";
                }
            }
            catch (Exception ex)
            {
                string methodName = MethodBase.GetCurrentMethod().Name;
                retval.Flag = Errors.Failed;
                retval.Message = ex.Message;
                DMEEditor.AddLogMessage("Beep", $"error in {methodName} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return dataset;
        }
        public List<string> GetEntitesList()
        {

            ErrorsInfo retval = new ErrorsInfo();
            retval.Flag = Errors.Ok;
            retval.Message = "Get Entity Successfully";
            EntitiesNames = new List<string>();
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }
                if (ConnectionStatus == ConnectionState.Open)
                {
                    var db = _client.GetDatabase(CurrentDatabase);
                    var collectionNames = db.ListCollectionNames().ToList();
                    foreach (var item in collectionNames)
                    {
                        EntitiesNames.Add(item);

                    }
                    // Synchronize the Entities list to match the current collection names
                    if (Entities != null)
                    {
                        var entitiesToRemove = Entities.Where(e => !EntitiesNames.Contains(e.EntityName) && !string.IsNullOrEmpty(e.CustomBuildQuery) ).ToList();
                        foreach (var item in entitiesToRemove)
                        {
                            Entities.Remove(item);
                        }
                        var entitiesToAdd = EntitiesNames.Where(e => !Entities.Any(x => x.EntityName == e)).ToList();
                        foreach (var item in entitiesToAdd)
                        {
                            Entities.Add(GetEntityStructure(item, true));
                        }
                    }


                }
                else
                {
                    retval.Flag = Errors.Failed;
                    retval.Message = "Could not open connection";
                }
            }
            catch (Exception ex)
            {
                string methodName = MethodBase.GetCurrentMethod().Name;
                retval.Flag = Errors.Failed;
                retval.Message = ex.Message;
                DMEEditor.AddLogMessage("Beep", $"error in {methodName} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return EntitiesNames;
        }
        public async Task<object> GetEntityDataAsync(string entityname, string filterstr)
        {
            ErrorsInfo retval = new ErrorsInfo();
            retval.Flag = Errors.Ok;
            retval.Message = "Get Entity Successfully";
            object result = null;
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }
                if (ConnectionStatus == ConnectionState.Open)
                {
                    //SetObjects(entityname);
                    //Get data from the custom query and put it in the dataset
                    var db = _client.GetDatabase(CurrentDatabase);
                    var collectionNames = db.ListCollectionNames().ToList();
                    //get data from the table or  collection and put in a table and add to the dataset
                    // fetch data from the colloction and put in a datatable
                    DataTable dt = new DataTable();
                    dt.TableName = entityname;
                    //get collection by name TableName
                    var collection = db.GetCollection<BsonDocument>(entityname);
                    //use filterstr as a filter to get data from the collection like filter = Builders<BsonDocument>.Filter.Eq("name", "Jack"); 
                    // Parsing the filter string into a MongoDB filter. Assume simple equality checks for this example.
                    List<BsonDocument> documents = new List<BsonDocument>();
                    bool isFilter = false;
                    if (filterstr != null)
                    {
                            var filters = ParseFilterString(filterstr);
                            documents = collection.Find(filters).ToList();
                            isFilter = true;
                      
                    }
                    if (!isFilter)
                    {
                        documents = collection.Find(new BsonDocument()).ToList();
                    }

                    if (DataStruct == null)
                    {
                        DataStruct = CompileSchemaFromDocuments(documents, entityname);
                    }

                    // Optionally convert BSON documents to a specific object type if needed
                    // Assuming you have a method to determine the type from entityName
                    //  Type entityType = GetEntityType(EntityName);
                    enttype = GetEntityType(entityname);
                    result = ConvertBsonDocumentsToObjects(documents, enttype, DataStruct);
                    result = documents;
                }
                else
                {
                    retval.Flag = Errors.Failed;
                    retval.Message = "Could not open connection";
                }

            }
            catch (Exception ex)
            {
                string methodName = MethodBase.GetCurrentMethod().Name;
                retval.Flag = Errors.Failed;
                retval.Message = ex.Message;
                DMEEditor.AddLogMessage("Beep", $"error in {methodName} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return (Task<object>)result;
        }
        public object GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            ErrorsInfo retval = new ErrorsInfo();
            retval.Flag = Errors.Ok;
            retval.Message = "Get Entity Successfully";
            object result = null;
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }
                if (ConnectionStatus == ConnectionState.Open)
                {
                   // SetObjects(EntityName);
                    //Get data from the custom query and put it in the dataset
                    var db = _client.GetDatabase(CurrentDatabase);
                    var collection = db.GetCollection<BsonDocument>(EntityName);
                    // Build the MongoDB filter from the list of AppFilter
                    var mongoFilter = BuildMongoFilter(filter);

                    // Calculate pagination parameters
                    int skipAmount = (pageNumber - 1) * pageSize;

                    // Apply the filter and pagination to the query
                    var documents = collection.Find(mongoFilter)
                                              .Skip(skipAmount)
                                              .Limit(pageSize)
                                              .ToList();

                    // Optionally, convert BSON documents to a specific object type if necessary
                    // This conversion would depend on the expected return type and data structure
                    result = documents; // Directly return documents or convert to DTOs as needed
                }

            }
            catch (Exception ex)
            {
                string methodName = MethodBase.GetCurrentMethod().Name;
                retval.Flag = Errors.Failed;
                retval.Message = ex.Message;
                DMEEditor.AddLogMessage("Beep", $"error in {methodName} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return result;
        }
        public Task<object> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            return Task.Run(() =>
            {
                ErrorsInfo retval = new ErrorsInfo();
                retval.Flag = Errors.Ok;
                retval.Message = "Get Entity Successfully";
                object result = null;
                try
                {
                    result = GetEntity(EntityName, Filter);
                }
                catch (Exception ex)
                {
                    string methodName = MethodBase.GetCurrentMethod().Name;
                    retval.Flag = Errors.Failed;
                    retval.Message = ex.Message;
                    DMEEditor.AddLogMessage("Beep", $"error in {methodName} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                }
                return result;
            });
        }
        public object GetEntity(string EntityName, List<AppFilter> filter)
        {
            ErrorsInfo retval = new ErrorsInfo();
            retval.Flag = Errors.Ok;
            retval.Message = "Get Entity Successfully";
            object result = null;
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }
                if (ConnectionStatus == ConnectionState.Open)
                {
                    //SetObjects(EntityName);
                    //Get data from the custom query and put it in the dataset
                    var db = _client.GetDatabase(CurrentDatabase);
                    var collection = db.GetCollection<BsonDocument>(EntityName);
                    List<BsonDocument> documents = new List<BsonDocument>();
                    // Building a MongoDB filter from AppFilter list
                    bool isFilter = false;
                    if (filter!=null)
                    {
                        if (filter.Count > 0)
                        {
                            var filterString = BuildFilterString(filter);
                            var filters = ParseFilterString(filterString);
                            documents = collection.Find(filters).ToList();
                            isFilter = true;
                        }
                    }
                    if (!isFilter)
                    {
                        documents = collection.Find(new BsonDocument()).ToList();
                    }
                    if (DataStruct == null)
                    {
                        DataStruct = CompileSchemaFromDocuments(documents, EntityName);
                    }
                    
                    // Optionally convert BSON documents to a specific object type if needed
                    // Assuming you have a method to determine the type from entityName
                    //  Type entityType = GetEntityType(EntityName);
                    enttype = GetEntityType(EntityName);
                    result = ConvertBsonDocumentsToObjects(documents, enttype, DataStruct);
                }
            }
            catch (Exception ex)
            {
                string methodName = MethodBase.GetCurrentMethod().Name;
                retval.Flag = Errors.Failed;
                retval.Message = ex.Message;
                DMEEditor.AddLogMessage("Beep", $"error in {methodName} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return result;
        }
        public List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            ErrorsInfo retval = new ErrorsInfo();
            retval.Flag = Errors.Ok;
            retval.Message = "Get Entity Foreign Keys Successfully";
            List<RelationShipKeys> result = new List<RelationShipKeys>();
            try
            {
                if (entityname == null)
                {
                    entityname = "Default";
                }
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
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
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }
                if (ConnectionStatus == ConnectionState.Open)
                {

                    result= GetEntityStructure(EntityName, refresh);
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
        public EntityStructure GetEntityStructure(string EntityName, bool refresh = false)
        {
            ErrorsInfo retval = new ErrorsInfo();
            retval.Flag = Errors.Ok;
            retval.Message = "Get Entity Structure Successfully";
           
            try
            {
                if (refresh == false && Entities.Count > 0)
                {
                    DataStruct = Entities.Find(c => c.EntityName.Equals(EntityName, StringComparison.CurrentCultureIgnoreCase));
                    if (DataStruct != null)
                    {
                        return DataStruct;
                    }
                }
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }
                if (ConnectionStatus == ConnectionState.Open)
                {
                    //Get data from the custom query and put it in the dataset
                    var db = _client.GetDatabase(CurrentDatabase);
                    var collection = db.GetCollection<BsonDocument>(EntityName);
                    // Attempt to get the first document to infer the structure
                    BsonDocument firstDocument;
                    List<BsonDocument> list = new List<BsonDocument>();
                    try
                    {
                        firstDocument = collection.Find(new BsonDocument()).FirstOrDefault();
                        list = collection.Find(new BsonDocument()).ToList();
                    }
                    catch (Exception ex)
                    {
                        firstDocument = null;
                    }

                    if (firstDocument != null)
                    {
                        if (DataStruct != null) 
                        {
                            if (DataStruct.EntityName == null || DataStruct.EntityName != EntityName)
                            {
                                DataStruct = CompileSchemaFromDocuments(list, EntityName);
                            }
                        }else
                            DataStruct = CompileSchemaFromDocuments(list, EntityName);
                        ObjectsCreated = true;
                        // Optionally convert BSON documents to a specific object type if needed
                        // Assuming you have a method to determine the type from entityName
                        //  Type entityType = GetEntityType(EntityName);
                        enttype = GetEntityType(EntityName);
                        // result = GetEntityStructureFromBson(firstDocument, EntityName);
                        if (Entities.Count > 0 && !Entities.Any(p => p.EntityName == EntityName))
                        {
                            Entities.Add(DataStruct);
                        }
                        if (Entities.Count == 0 && !Entities.Any(p => p.EntityName == EntityName))
                        {
                            Entities.Add(DataStruct);
                        }
                       
                        if(EntitiesNames.Count == 0)
                        {
                            EntitiesNames = Entities.Select(p => p.EntityName).ToList();
                        }
                        if(EntitiesNames.Count>0 && !EntitiesNames.Any(p => p == EntityName))
                        {
                            EntitiesNames.Add(EntityName);
                        }
                        if (Entities.Count > 0 && DataStruct == null)
                        {
                            DataStruct = Entities.Find(c => c.EntityName.Equals(EntityName, StringComparison.CurrentCultureIgnoreCase));
                            if (DataStruct != null)
                            {
                                retval.Flag = Errors.Ok;
                                retval.Message = "documents found in the collection.";
                                return DataStruct;
                            }
                        }
                    }
                    else
                    {
                        retval.Flag = Errors.Failed;
                        retval.Message = "No documents found in the collection.";
                        DataStruct = null;
                    }
                }
            }
            catch (Exception ex)
            {
                string methodName = MethodBase.GetCurrentMethod().Name;
                retval.Flag = Errors.Failed;
                retval.Message = ex.Message;
                DataStruct = null;
                DMEEditor.AddLogMessage("Beep", $"error in {methodName} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return DataStruct;
        }
        public DataTable GetEntityDataTable(string EntityName, string filterstr)
        {
            ErrorsInfo retval = new ErrorsInfo();
            retval.Flag = Errors.Ok;
            retval.Message = "Get Entity DataTable";
            DataTable result = new DataTable();
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }
                {
                    SetObjects(EntityName);
                    var db = _client.GetDatabase(CurrentDatabase);
                    var collection = db.GetCollection<BsonDocument>(EntityName);

                    var filterDefinition = ParseFilterString(filterstr);

                    // Fetch only the first document to infer schema
                    var firstDocument = collection.Find(filterDefinition).FirstOrDefault();

                    if (firstDocument != null)
                    {
                        // Prepare the DataTable by inferring columns and types
                        foreach (var element in firstDocument.Elements)
                        {
                            var columnType = GetTypeFromBsonType(element.Value.BsonType);
                            result.Columns.Add(new DataColumn(element.Name, columnType));
                        }

                        // Now retrieve all documents based on the filter
                        var documents = collection.Find(filterDefinition).ToList();

                        foreach (var document in documents)
                        {
                            var row = result.NewRow();
                            foreach (DataColumn column in result.Columns)
                            {
                                row[column.ColumnName] = document[column.ColumnName].IsBsonNull ? DBNull.Value : Convert.ChangeType(document[column.ColumnName].ToString(), column.DataType);
                            }
                            result.Rows.Add(row);
                        }
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
        /// <summary>
        /// Sets up necessary objects and structures for database operations based on the provided entity name.
        /// </summary>
        /// <param name="Entityname">The name of the entity for which the database command objects will be set up.</param>
        /// <remarks>
        /// This method is essential for initializing and reusing database commands and structures, improving efficiency and maintainability.
        /// </remarks>
        public Type GetEntityType(string EntityName)
        {
            ErrorsInfo retval = new ErrorsInfo();
            retval.Flag = Errors.Ok;
            retval.Message = "Get Entity Type  ";
            Type result = null;
   //         SetObjects(EntityName);
            try
            {
                if(ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }
                if(ConnectionStatus == ConnectionState.Open )
                { 
                    if(EntityName== lastentityname)
                    {
                        if(enttype != null)
                        {
                            result= enttype;
                        }
                    }
                    
                }
              if(result == null)
                {

                    if(DataStruct == null)
                    {
                        lastentityname = EntityName;
                        DataStruct = GetEntityStructure(EntityName);
                    }
                    
                    DMTypeBuilder.CreateNewObject(DMEEditor, "Beep." + DatasourceName, EntityName, DataStruct.Fields);
                    result = DMTypeBuilder.myType;
                    if(result != null)
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
        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            ErrorsInfo retval = new ErrorsInfo();
            retval.Flag = Errors.Ok;
            retval.Message = "Entities Updated Successfully";
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }
                if (ConnectionStatus == ConnectionState.Open)
                {
                    var database = _client.GetDatabase(CurrentDatabase);
                    var collection = database.GetCollection<BsonDocument>(EntityName);
                    IEnumerable<BsonDocument> documents = null;

                    if (UploadData is List<BsonDocument> bsonList)
                    {
                        documents = bsonList;
                    }
                    else if (UploadData is List<object> pocoList)
                    {
                        documents = ConvertPocoListToBsonDocuments(pocoList);
                    }
                    else if (UploadData is DataTable dataTable)
                    {
                        documents = ConvertDataTableToBsonDocuments(dataTable);
                    }
                    else
                    {
                        throw new ArgumentException("Unsupported data type for UploadData.");
                    }
                    foreach (var updateDoc in (List<BsonDocument>)UploadData)
                    {
                        // Assume each document has an "_id" field used for matching
                        var filter = Builders<BsonDocument>.Filter.Eq("_id", updateDoc["_id"]);
                        var updateResult = collection.ReplaceOne(filter, updateDoc);

                        if (updateResult.IsAcknowledged && updateResult.ModifiedCount == 0)
                        {
                            retval.Flag = Errors.Failed;
                            retval.Message = "No documents updated.";
                        }

                        // Optionally, report progress
                        progress?.Report(new PassedArgs { Messege = "Updating" });
                    }
                }
            }
            catch (Exception ex)
            {
                string methodName = MethodBase.GetCurrentMethod().Name;
                retval.Flag = Errors.Failed;
                retval.Message = ex.Message;
                DMEEditor.AddLogMessage("Beep", $"error in {methodName} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return retval;
        }
        public object RunQuery(string qrystr)
        {
            ErrorsInfo retval = new ErrorsInfo();
            retval.Flag = Errors.Ok;
            retval.Message = "Entities Created Successfully";
            object result = null;
            List<BsonDocument> results = new List<BsonDocument>();
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }
                if (ConnectionStatus == ConnectionState.Open)
                {
                    var database = _client.GetDatabase(CurrentDatabase);
                    // Assume qrystr is intended for a specific collection, e.g., "collectionName|{ 'field': 'value' }"
                    var parts = qrystr.Split('|');
                    if (parts.Length != 2)
                    {
                        DMEEditor.AddLogMessage("Beep", $"Query string is not in the expected format 'collectionName|{qrystr}", DateTime.Now, -1, null, Errors.Failed);
                        return null;
                    }
                    var collectionName = parts[0].Trim();
                    var query = BsonDocument.Parse(parts[1].Trim());

                    var collection = database.GetCollection<BsonDocument>(collectionName);
                    var filter = new BsonDocumentFilterDefinition<BsonDocument>(query);
                    results = collection.Find(filter).ToList();

                    // Convert BSON documents to a more generic or user-friendly format if necessary
                    result = results.Select(doc => doc.ToDictionary()).ToList(); // Converts to List<Dictionary<string, object>>
                }

            }
            catch (Exception ex)
            {
                string methodName = MethodBase.GetCurrentMethod().Name;
                retval.Flag = Errors.Failed;
                retval.Message = ex.Message;
                DMEEditor.AddLogMessage("Beep", $"error in {methodName} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return result;
        }
        public virtual IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            ErrorsInfo retval = new ErrorsInfo();
            retval.Flag = Errors.Ok;
            retval.Message = "Entity Updated Successfully";
            BsonDocument documentToUpdate;
            try
            {

                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }
                if (ConnectionStatus == ConnectionState.Open)
                {
                    SetObjects(EntityName);
                    //Get data from the custom query and put it in the dataset
                    var db = _client.GetDatabase(CurrentDatabase);
                    var collection = db.GetCollection<BsonDocument>(EntityName);
                    // Building a MongoDB filter from AppFilter list
                    // Determine the type of UploadDataRow and convert to BsonDocument
                    if (UploadDataRow is BsonDocument)
                    {
                        documentToUpdate = (BsonDocument)UploadDataRow;
                    }
                    else if (UploadDataRow is DataRow)
                    {
                        DataRow row = (DataRow)UploadDataRow;
                        documentToUpdate = DataRowToBsonDocument(row);
                    }
                    else // Assuming UploadDataRow is a POCO
                    {
                        if(DataStruct == null)
                        {
                            DataStruct = GetEntityStructure(EntityName);
                        }
                        documentToUpdate = PocoToBsonDocument(UploadDataRow,DataStruct);
                       // documentToUpdate = UploadDataRow.ToBsonDocument();
                    }

                    // Extract the _id or another unique identifier from the document
                    var id = documentToUpdate["_id"]; // Ensure all data types set _id appropriately

                    // Define the filter for the update operation
                    var filter = Builders<BsonDocument>.Filter.Eq("_id", id);

                    // Define the update operations
                    var update = new BsonDocument("$set", documentToUpdate); // Updates all fields in the document

                    // Perform the update operation
                    var result = collection.UpdateOne(filter, update);

                    if (result.MatchedCount == 0)
                    {
                        retval.Flag = Errors.Failed;
                        retval.Message = "No documents matched the query for update.";
                    }
                    else if (result.ModifiedCount == 0)
                    {
                        retval.Flag = Errors.Failed;
                        retval.Message = "No documents were modified.";
                    }
                }
            }
            catch (Exception ex)
            {
                string methodName = MethodBase.GetCurrentMethod().Name;
                retval.Flag = Errors.Failed;
                retval.Message = ex.Message;
                DMEEditor.AddLogMessage("Beep", $"error in {methodName} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return retval;
        }
        public IErrorsInfo DeleteEntity(string EntityName, object DeletedDataRow)
        {
            ErrorsInfo retval = new ErrorsInfo();
            retval.Flag = Errors.Ok;
            retval.Message = "Entity Deleted Successfully";
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }
                if (ConnectionStatus == ConnectionState.Open)
                {
                    var database = _client.GetDatabase(CurrentDatabase);
                    var collection = database.GetCollection<BsonDocument>(EntityName);
                    var documentTodelete = BsonSerializer.Deserialize<BsonDocument>(DeletedDataRow.ToJson());
                    BsonValue idValue = GetIdentifierValue(documentTodelete);
                    var filter = Builders<BsonDocument>.Filter.Eq("_id", idValue);

                    var deleteResult = collection.DeleteOne(filter);
                    if (deleteResult.DeletedCount == 0)
                    {
                        retval.Flag = Errors.Failed;
                        retval.Message = "No documents found with the specified identifier.";
                    }
                }
            }
            catch (Exception ex)
            {
                string methodName = MethodBase.GetCurrentMethod().Name;
                retval.Flag = Errors.Failed;
                retval.Message = ex.Message;
                DMEEditor.AddLogMessage("Beep", $"error in {methodName} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return retval;
        }
        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            ErrorsInfo retval = new ErrorsInfo();
            retval.Flag = Errors.Ok;
            retval.Message = "Script Run Successfully";
            try
            {

            }
            catch (Exception ex)
            {
                string methodName = MethodBase.GetCurrentMethod().Name;
                retval.Flag = Errors.Failed;
                retval.Message = ex.Message;
                DMEEditor.AddLogMessage("Beep", $"error in {methodName} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return retval;
        }
        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            ErrorsInfo retval = new ErrorsInfo();
            retval.Flag = Errors.Ok;
            retval.Message = "Entities Created Successfully";
            try
            {

            }
            catch (Exception ex)
            {
                string methodName = MethodBase.GetCurrentMethod().Name;
                retval.Flag = Errors.Failed;
                retval.Message = ex.Message;
                DMEEditor.AddLogMessage("Beep", $"error in {methodName} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return retval;
        }
        public List<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            return null;
        }
        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            ErrorsInfo retval = new ErrorsInfo();
            retval.Flag = Errors.Ok;
            retval.Message = "Entity Created Successfully";
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }
                if (ConnectionStatus == ConnectionState.Open)

                {
                    SetObjects(EntityName);
                  
                    var collection = _database.GetCollection<BsonDocument>(EntityName);
                    var entityStructure = GetEntityStructure(EntityName) ?? new EntityStructure(EntityName);
                    BsonDocument documentToInsert = ConvertToBsonDocument(InsertedData, entityStructure);
                    UpdateEntityStructure(documentToInsert, entityStructure);
                    // Determine the type of InsertedData and convert to BsonDocument
                    //if (InsertedData is BsonDocument bsonDocument)
                    //{
                    //    documentToInsert = bsonDocument;
                    //}
                    //else if (InsertedData is DataRow dataRow)
                    //{
                    //    documentToInsert = DataRowToBsonDocument(dataRow);
                    //}
                    //else
                    //{
                    //    if(DataStruct == null)
                    //    {
                    //        DataStruct = GetEntityStructure(EntityName);
                    //    }
                    //    documentToInsert = PocoToBsonDocument(InsertedData, DataStruct);
                    //}
                    // Check if a session is active and insert accordingly
                    if (_session != null && _session.IsInTransaction)
                    {
                        collection.InsertOne(_session, documentToInsert);
                      //  DMEEditor.AddLogMessage("Beep", "Document inserted successfully within a transaction.", DateTime.Now, 0, null, Errors.Ok);
                    }
                    else
                    {
                        collection.InsertOne(documentToInsert);
                     //   DMEEditor.AddLogMessage("Beep", "Document inserted successfully without a transaction.", DateTime.Now, 0, null, Errors.Ok);
                    }
                    // Retrieve the _id value from the inserted document
                    var insertedId = documentToInsert["_id"];
                }
                else
                {
                    retval.Flag = Errors.Failed;
                    retval.Message = "Could not open connection";
                }

            }
            catch (Exception ex)
            {
                string methodName = MethodBase.GetCurrentMethod().Name;
                retval.Flag = Errors.Failed;
                retval.Message = ex.Message;
                DMEEditor.AddLogMessage("Beep", $"error in {methodName} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return retval;
        }
        private BsonDocument ConvertToBsonDocument(object data, EntityStructure structure)
        {
            if (data is BsonDocument bsonDocument)
            {
                return bsonDocument;
            }
            else if (data is DataRow dataRow)
            {
                return DataRowToBsonDocument(dataRow);
            }
            else
            {
                return PocoToBsonDocument(data, structure);
            }
        }
        private void UpdateEntityStructure(BsonDocument document, EntityStructure structure)
        {
            foreach (var element in document.Elements)
            {
                if (!structure.Fields.Any(f => f.fieldname.Equals(element.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    structure.Fields.Add(new EntityField
                    {
                        fieldname = element.Name,
                        fieldtype = element.Value.BsonType.ToString(), // Assuming a method to translate BsonType to a more general type name
                        EntityName = structure.EntityName
                    });
                }
            }
        }
        #region "Insert Bulk"
      

      
        #endregion

        #region "Parsing and Filtering"

        private BsonDocument GenerateValidationDocument(EntityStructure entity)
        {
            var schemaDoc = new BsonDocument();
            foreach (var field in entity.Fields)
            {
                schemaDoc[field.fieldname] = new BsonDocument("$type", field.fieldtype);
            }
            return new BsonDocument("$jsonSchema", new BsonDocument
            {
                { "bsonType", "object" },
                  { "required", new BsonArray(entity.Fields.Where(f => f.IsKey).Select(f => f.fieldname)) },
                  { "properties", schemaDoc }
                });
        }
        private BsonValue GetIdentifierValue(object deletedDataRow)
        {
            // Assume deletedDataRow is an ID directly or a complex object/POCO that needs parsing
            if (deletedDataRow is BsonValue bsonValue)
                return bsonValue;
            else if (deletedDataRow is string id)
                return new ObjectId(id); // Handling string as ObjectId
            else if (deletedDataRow is BsonDocument bsonDoc && bsonDoc.Contains("_id"))
                return bsonDoc["_id"];
            // Add more cases as necessary for your application
            throw new ArgumentException("Unable to extract identifier from DeletedDataRow.");
        }
        private FilterDefinition<BsonDocument> ParseFilterString(string filterstr)
        {
            var filterBuilder = Builders<BsonDocument>.Filter;
            FilterDefinition<BsonDocument> filter = filterBuilder.Empty;

            // Supports conditions combined with logical operators: AND, OR
            // Example input: "age>30 AND name=John OR status!=active"
            var logicalSegments = SplitLogicalOperators(filterstr);

            foreach (var segment in logicalSegments)
            {
                if (segment.Item1.ToUpper() == "AND")
                {
                    filter = filter & ParseSingleCondition(segment.Item2);
                }
                else if (segment.Item1.ToUpper() == "OR")
                {
                    filter = filter | ParseSingleCondition(segment.Item2);
                }
                else
                {
                    filter = ParseSingleCondition(segment.Item2); // First segment, no operator
                }
            }

            return filter;
        }
        private IEnumerable<Tuple<string, string>> SplitLogicalOperators(string input)
        {
            var operators = new[] { "AND", "OR" };
            var tokens = new List<Tuple<string, string>>();
            var currentIndex = 0;

            while (currentIndex < input.Length)
            {
                var nextOperatorIndex = -1;
                var nextOperator = string.Empty;

                // Find the next logical operator
                foreach (var op in operators)
                {
                    var tempIndex = input.IndexOf(op, currentIndex, StringComparison.OrdinalIgnoreCase);
                    if (tempIndex >= 0 && (nextOperatorIndex == -1 || tempIndex < nextOperatorIndex))
                    {
                        nextOperatorIndex = tempIndex;
                        nextOperator = op;
                    }
                }

                // No more operators, add the last segment
                if (nextOperatorIndex == -1)
                {
                    tokens.Add(new Tuple<string, string>(string.Empty, input.Substring(currentIndex).Trim()));
                    break;
                }

                // Add the current segment and operator
                tokens.Add(new Tuple<string, string>(nextOperator, input.Substring(currentIndex, nextOperatorIndex - currentIndex).Trim()));
                currentIndex = nextOperatorIndex + nextOperator.Length;
            }

            return tokens;
        }
        private FilterDefinition<BsonDocument> BuildMongoFilter(List<AppFilter> filters)
        {
            var builder = Builders<BsonDocument>.Filter;
            FilterDefinition<BsonDocument> filter = builder.Empty; // Start with an empty filter

            foreach (var appFilter in filters)
            {
                FilterDefinition<BsonDocument> newFilter = ParseFilter(appFilter);
                filter = builder.And(filter, newFilter); // Combine using logical AND
            }

            return filter;
        }
        private FilterDefinition<BsonDocument> ParseFilter(AppFilter filter)
        {
            var builder = Builders<BsonDocument>.Filter;
            // Convert filter value to the appropriate type based on valueType
            object value = Convert.ChangeType(filter.FilterValue, Type.GetType(filter.valueType));

            switch (filter.Operator)
            {
                case "=":
                    return builder.Eq(filter.FieldName, value);
                case ">":
                    return builder.Gt(filter.FieldName, value);
                case "<":
                    return builder.Lt(filter.FieldName, value);
                case "!=":
                    return builder.Ne(filter.FieldName, value);
                case "IN":
                    return builder.In(filter.FieldName, new List<object> { value }); // Needs list of values
                default:
                    throw new InvalidOperationException($"Unsupported operator {filter.Operator}");
            }
        }
        private FilterDefinition<BsonDocument> ParseSingleCondition(string condition)
        {
            var filterBuilder = Builders<BsonDocument>.Filter;

            // Handle different operators
            if (condition.Contains("!="))
            {
                var parts = condition.Split(new[] { "!=" }, StringSplitOptions.None);
                return filterBuilder.Ne(parts[0].Trim(), parts[1].Trim());
            }
            else if (condition.Contains("="))
            {
                var parts = condition.Split('=');
                return filterBuilder.Eq(parts[0].Trim(), parts[1].Trim());
            }
            else if (condition.Contains(">"))
            {
                var parts = condition.Split('>');
                return filterBuilder.Gt(parts[0].Trim(), parts[1].Trim());
            }
            else if (condition.Contains("<"))
            {
                var parts = condition.Split('<');
                return filterBuilder.Lt(parts[0].Trim(), parts[1].Trim());
            }

            return filterBuilder.Empty;
        }
        public object ConvertBsonDocumentsToObjects(List<BsonDocument> documents, Type type, EntityStructure entStructure)
        {
            Type uowGenericType = typeof(ObservableBindingList<>).MakeGenericType(type);
            var records = (IBindingListView)Activator.CreateInstance(uowGenericType);

            foreach (var document in documents)
            {
                dynamic instance = Activator.CreateInstance(type);
                foreach (var field in entStructure.Fields)
                {
                    var fieldName = field.fieldname.ToLower();
                    var f = document.Names.FirstOrDefault(p => p.Equals(fieldName, StringComparison.InvariantCultureIgnoreCase));
                    if (!string.IsNullOrEmpty(f))
                    {
                        var bsonValue = document.GetElement(f).Value;
                        if (!bsonValue.IsBsonNull)
                        {
                            try
                            {
                                string netTypeString = field.fieldtype; // Use field.fieldtype directly
                                Type netType = Type.GetType(netTypeString);

                                if (netType == typeof(string) && bsonValue.BsonType == BsonType.ObjectId)
                                {
                                    // Convert ObjectId to string
                                    var value = bsonValue.AsObjectId.ToString();
                                    type.GetProperty(field.fieldname).SetValue(instance, value);
                                }
                                else if (Type.GetTypeCode(netType) == Type.GetTypeCode(Type.GetType(field.fieldtype)))
                                {
                                    // Directly assign if types match
                                    object value;
                                    if (Type.GetType(field.fieldtype) == typeof(string))
                                    {
                                        value = bsonValue.ToString();
                                    }
                                    else
                                    {
                                        value = BsonSerializer.Deserialize(bsonValue.ToJson(), Type.GetType(field.fieldtype));
                                    }
                                    type.GetProperty(field.fieldname).SetValue(instance, value);
                                }
                                else
                                {
                                    // Handle type conversion if necessary
                                    object value;
                                    if (Type.GetType(field.fieldtype) == typeof(string))
                                    {
                                        value = bsonValue.ToString();
                                    }
                                    else
                                    {
                                        value = Convert.ChangeType(bsonValue, Type.GetType(field.fieldtype));
                                    }
                                    type.GetProperty(field.fieldname).SetValue(instance, value);
                                }
                            }
                            catch (Exception ex)
                            {
                                // Handle or log the error appropriately
                                DMEEditor.AddLogMessage("Beep", $"Error setting property {field.fieldname} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                            }
                        }
                    }
                }
                records.Add(instance);
            }

            return records;
        }
        private void UpdateDataObjectWithId(object dataObject, BsonValue idValue)
        {
            var properties = dataObject.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties)
            {
                if (property.Name.Equals("_id", StringComparison.InvariantCultureIgnoreCase) && property.CanWrite)
                {
                    if (property.PropertyType == typeof(ObjectId) && idValue.IsObjectId)
                    {
                        property.SetValue(dataObject, idValue.AsObjectId);
                    }
                    else if (property.PropertyType == typeof(string))
                    {
                        property.SetValue(dataObject, idValue.ToString());
                    }
                    break;
                }
            }
        }
        public EntityStructure CompileSchemaFromDocuments(List<BsonDocument> documents,string entityName)
        {
            EntityStructure entityStructure = new EntityStructure();
            Dictionary<string, EntityField> fieldDictionary = new Dictionary<string, EntityField>();
            // Use provided entity name or default to a generic name
            entityStructure.EntityName = entityName ?? "DefaultEntityName";
            List<EntityField> fields = new List<EntityField>();
            entityStructure.DatasourceEntityName = entityName;
            entityStructure.OriginalEntityName = entityName;
            entityStructure.DataSourceID = DatasourceName;
            int fieldIndex = 0;
            foreach (var document in documents)
            {
                foreach (var element in document.Elements)
                {
                    if (!fieldDictionary.ContainsKey(element.Name))
                    {
                        EntityField newField = new EntityField
                        {
                            fieldname = element.Name,
                            BaseColumnName = element.Name,
                            fieldtype = GetTypeFromBsonValue(element.Value),
                            IsKey = element.Name.ToLower().Contains("_id"),
                            IsIdentity = element.Name.ToLower().Contains("_id"),
                            FieldIndex = fieldIndex++
                        };
                        fieldDictionary[element.Name] = newField;
                    }
                }
            }

            entityStructure.Fields = new List<EntityField>(fieldDictionary.Values);
            return entityStructure;
        }

        public EntityStructure GetEntityStructureFromBson(BsonDocument document, string entityName = null)
        {
            EntityStructure entityData = new EntityStructure();
            try
            {
                // Use provided entity name or default to a generic name
                entityData.EntityName = entityName ?? "DefaultEntityName";
                List<EntityField> fields = new List<EntityField>();
                entityData.DatasourceEntityName = entityName;
                entityData.OriginalEntityName= entityName;
                entityData.DataSourceID = DatasourceName;
                int fieldIndex = 0;
                foreach (var element in document.Elements)
                {
                    EntityField field = new EntityField
                    {
                        fieldname = element.Name,
                        BaseColumnName = element.Name,
                        fieldtype = GetTypeFromBsonValue(element.Value), // Convert BsonType to a .NET type string
                        IsKey = element.Name.ToLower().Contains("_id"), // Assume any field with "ID" in the name is a key
                        IsIdentity = element.Name.ToLower().Contains("_id"),
                        ValueRetrievedFromParent = false,
                        EntityName = entityData.EntityName,
                        FieldIndex = fieldIndex++
                    };
                    
                    fields.Add(field);
                }

                entityData.Fields = fields;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: Could not create entity structure - " + ex.Message);
                return null;
            }

            return entityData;
        }
        private BsonDocument DataRowToBsonDocument(DataRow row)
        {
            var document = new BsonDocument();
            foreach (DataColumn column in row.Table.Columns)
            {
                document[column.ColumnName] = BsonValue.Create(row[column]);
            }
            return document;
        }
        private Type GetTypeFromBsonType(BsonType bsonType)
        {
            List<DatatypeMapping> dataTypeMappings = DataTypeFieldMappingHelper.GetMongoDBDataTypeMappings();
            DatatypeMapping mapping = null;

            switch (bsonType)
            {
                case BsonType.String:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("String", StringComparison.InvariantCultureIgnoreCase));
                    return Type.GetType(mapping?.NetDataType ?? "System.String");
                case BsonType.Int32:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("Int32", StringComparison.InvariantCultureIgnoreCase));
                    return Type.GetType(mapping?.NetDataType ?? "System.Int32");
                case BsonType.Int64:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("Int64", StringComparison.InvariantCultureIgnoreCase));
                    return Type.GetType(mapping?.NetDataType ?? "System.Int64");
                case BsonType.Boolean:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("Boolean", StringComparison.InvariantCultureIgnoreCase));
                    return Type.GetType(mapping?.NetDataType ?? "System.Boolean");
                case BsonType.DateTime:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("DateTime", StringComparison.InvariantCultureIgnoreCase));
                    return Type.GetType(mapping?.NetDataType ?? "System.DateTime");
                case BsonType.Double:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("Double", StringComparison.InvariantCultureIgnoreCase));
                    return Type.GetType(mapping?.NetDataType ?? "System.Double");
                case BsonType.Decimal128:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("Decimal128", StringComparison.InvariantCultureIgnoreCase));
                    return Type.GetType(mapping?.NetDataType ?? "System.Decimal");
                case BsonType.ObjectId:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("ObjectId", StringComparison.InvariantCultureIgnoreCase));
                    return Type.GetType(mapping?.NetDataType ?? "MongoDB.Bson.ObjectId");
                case BsonType.Binary:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("Binary", StringComparison.InvariantCultureIgnoreCase));
                    return Type.GetType(mapping?.NetDataType ?? "System.Byte[]");
                default:
                    return typeof(string); // Default to string for other less common types
            }
        }
        private BsonValue ConvertToBsonValue(object value, string mongoType)
        {
            switch (mongoType)
            {
                case "Double":
                    return BsonValue.Create(Convert.ToDouble(value));
                case "String":
                    return BsonValue.Create(value.ToString());
                case "Document":
                    return BsonDocument.Create(value); // Assuming value is a dictionary or object that can be converted to BsonDocument
                case "Array":
                    return new BsonArray((IEnumerable)value); // Assuming value is an IEnumerable
                case "Binary":
                    return BsonValue.Create((byte[])value);
                case "ObjectId":
                    return new ObjectId(value.ToString()); // Assuming value is a string representation of ObjectId
                case "Boolean":
                    return BsonValue.Create(Convert.ToBoolean(value));
                case "DateTime":
                    return BsonValue.Create(Convert.ToDateTime(value));
                case "RegularExpression":
                    return BsonValue.Create(new BsonRegularExpression(value.ToString()));
                case "Int32":
                    return BsonValue.Create(Convert.ToInt32(value));
                case "Int64":
                    return BsonValue.Create(Convert.ToInt64(value));
                case "Decimal128":
                    return BsonValue.Create(Convert.ToDecimal(value));
                default:
                    return BsonValue.Create(value.ToString()); // Default to string if type is not recognized
            }
        }
        private string GetMongoTypeFromNetType(string netType)
        {
            List<DatatypeMapping> dataTypeMappings = DataTypeFieldMappingHelper.GetMongoDBDataTypeMappings();
            DatatypeMapping mapping = null;

            switch (netType)
            {
                case "System.Double":
                    mapping = dataTypeMappings.FirstOrDefault(x => x.NetDataType.Equals("System.Double", StringComparison.InvariantCultureIgnoreCase));
                    return mapping?.DataType ?? "Double";
                case "System.String":
                    mapping = dataTypeMappings.FirstOrDefault(x => x.NetDataType.Equals("System.String", StringComparison.InvariantCultureIgnoreCase));
                    return mapping?.DataType ?? "String";
                case "System.Object":
                    return "Document"; // Could also be represented as a custom object or dictionary
                case "System.Array":
                    return "Array"; // Could also be a specific array type
                case "System.Byte[]":
                    mapping = dataTypeMappings.FirstOrDefault(x => x.NetDataType.Equals("System.Byte[]", StringComparison.InvariantCultureIgnoreCase));
                    return mapping?.DataType ?? "Binary";
                case "MongoDB.Bson.ObjectId":
                    mapping = dataTypeMappings.FirstOrDefault(x => x.NetDataType.Equals("MongoDB.Bson.ObjectId", StringComparison.InvariantCultureIgnoreCase));
                    return mapping?.DataType ?? "ObjectId";
                case "System.Boolean":
                    mapping = dataTypeMappings.FirstOrDefault(x => x.NetDataType.Equals("System.Boolean", StringComparison.InvariantCultureIgnoreCase));
                    return mapping?.DataType ?? "Boolean";
                case "System.DateTime":
                    mapping = dataTypeMappings.FirstOrDefault(x => x.NetDataType.Equals("System.DateTime", StringComparison.InvariantCultureIgnoreCase));
                    return mapping?.DataType ?? "DateTime";
                case "System.Text.RegularExpressions.Regex":
                    return "RegularExpression";
                case "System.Int32":
                    mapping = dataTypeMappings.FirstOrDefault(x => x.NetDataType.Equals("System.Int32", StringComparison.InvariantCultureIgnoreCase));
                    return mapping?.DataType ?? "Int32";
                case "System.Int64":
                    mapping = dataTypeMappings.FirstOrDefault(x => x.NetDataType.Equals("System.Int64", StringComparison.InvariantCultureIgnoreCase));
                    return mapping?.DataType ?? "Int64";
                case "System.Decimal":
                    mapping = dataTypeMappings.FirstOrDefault(x => x.NetDataType.Equals("System.Decimal", StringComparison.InvariantCultureIgnoreCase));
                    return mapping?.DataType ?? "Decimal128";

                default:
                    return "Document"; // Default to Document if the type is not recognized
            }
        }
        private string GetTypeFromBsonValue(BsonValue bsonValue)
        {

            List<DatatypeMapping> dataTypeMappings = DataTypeFieldMappingHelper.GetMongoDBDataTypeMappings();
            DatatypeMapping mapping = null;

            switch (bsonValue.BsonType)
            {
                case BsonType.Double:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("Double", StringComparison.InvariantCultureIgnoreCase));
                    return mapping?.NetDataType ?? "System.Double";
                case BsonType.String:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("String", StringComparison.InvariantCultureIgnoreCase));
                    return mapping?.NetDataType ?? "System.String";
                case BsonType.Document:
                    return "System.Object"; // Could also be represented as a custom object or dictionary
                case BsonType.Array:
                    return "System.Array"; // Could also be a specific array type
                case BsonType.Binary:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("Binary", StringComparison.InvariantCultureIgnoreCase));
                    return mapping?.NetDataType ?? "System.Byte[]";
                case BsonType.Undefined:
                    return "System.Object";
                case BsonType.ObjectId:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("ObjectId", StringComparison.InvariantCultureIgnoreCase));
                    return mapping?.NetDataType ?? "MongoDB.Bson.ObjectId";
                case BsonType.Boolean:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("Boolean", StringComparison.InvariantCultureIgnoreCase));
                    return mapping?.NetDataType ?? "System.Boolean";
                case BsonType.DateTime:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("DateTime", StringComparison.InvariantCultureIgnoreCase));
                    return mapping?.NetDataType ?? "System.DateTime";
                case BsonType.Null:
                    return "System.Object"; // Or nullable type
                case BsonType.RegularExpression:
                    return "System.Text.RegularExpressions.Regex";
                case BsonType.JavaScript:
                    return "System.String";
                case BsonType.Symbol:
                    return "System.String";
                case BsonType.JavaScriptWithScope:
                    return "System.String";
                case BsonType.Int32:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("Int32", StringComparison.InvariantCultureIgnoreCase));
                    return mapping?.NetDataType ?? "System.Int32";
                case BsonType.Timestamp:
                    return "System.DateTime"; // Or custom timestamp type
                case BsonType.Int64:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("Int64", StringComparison.InvariantCultureIgnoreCase));
                    return mapping?.NetDataType ?? "System.Int64";
                case BsonType.Decimal128:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("Decimal128", StringComparison.InvariantCultureIgnoreCase));
                    return mapping?.NetDataType ?? "System.Decimal";
                case BsonType.MinKey:
                case BsonType.MaxKey:
                    return "System.Object";
                default:
                    return "System.Object"; // Default to object if the type is not recognized
            }
        }
        private IEnumerable<BsonDocument> ConvertPocoListToBsonDocuments(List<object> pocoList)
        {
            var bsonDocuments = new List<BsonDocument>();
            foreach (var poco in pocoList)
            {
                bsonDocuments.Add(poco.ToBsonDocument());
            }
            return bsonDocuments;
        }
        private IEnumerable<BsonDocument> ConvertDataTableToBsonDocuments(DataTable dataTable)
        {
            var bsonDocuments = new List<BsonDocument>();
            foreach (DataRow row in dataTable.Rows)
            {
                var document = new BsonDocument();
                foreach (DataColumn column in dataTable.Columns)
                {
                    document[column.ColumnName] = BsonValue.Create(row[column]);
                }
                bsonDocuments.Add(document);
            }
            
            return bsonDocuments;
        }
    
     
        private string BuildFilterString(List<AppFilter> filters)
        {
            return string.Join(" AND ", filters.Select(f => $"{f.FieldName} {f.Operator} '{f.FilterValue}'"));
        }
        public bool IsMongoDBConnected(IMongoClient client)
        {
            try
            {
                //The ping command is a cheap and effective way to check connection health

                var ping = new BsonDocument("ping", 1);
                client.GetDatabase("admin").RunCommand<BsonDocument>(ping);
                return true; // Ping succeeded, connection is up
            }
            catch (Exception ex)
            {
                // Log the exception details here, could be connectivity issues or misconfiguration
                Console.WriteLine("Failed to ping MongoDB server: " + ex.Message);
                return false; // Ping failed, connection is down or unreachable
            }
        }
        private BsonDocument PocoToBsonDocument(object poco, EntityStructure entityStructure)
        {
            var bsonDocument = new BsonDocument();
            var properties = poco.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                var name = property.Name;
                var field = entityStructure.Fields.FirstOrDefault(f => f.fieldname.Equals(name, StringComparison.InvariantCultureIgnoreCase));
                try
                {
                    if (field != null)
                    {
                        var value = property.GetValue(poco);

                        // Convert the property value to BsonValue based on the field type in entityStructure
                        BsonValue bsonValue;
                        if (value == null)
                        {
                            bsonValue = BsonNull.Value;
                        }
                        else if (field.IsKey && field.fieldtype == "System.String")
                        {

                            // Handle ObjectId conversion for key fields
                            if (ObjectId.TryParse(value.ToString(), out ObjectId objectId))
                            {
                                bsonValue = objectId;
                            }
                            else
                            {
                                // Optionally handle the error or skip the conversion
                              DMEEditor.AddLogMessage("Beep", $"Error converting property '{name}' to ObjectId.", DateTime.Now, -1, null, Errors.Failed);
                                continue;
                            }
                        }
                        else
                        {
                            // Get the MongoDB type from .NET type and convert the value to BsonValue
                            var mongoType = GetMongoTypeFromNetType(field.fieldtype);
                            bsonValue = ConvertToBsonValue(value, mongoType);
                        }

                        bsonDocument.Add(name, bsonValue);
                    }
                    else
                    {
                        // Optionally log or handle the error when a field is not found in entityStructure
                       DMEEditor.AddLogMessage("Beep", $"Field '{name}' not found in entity structure.", DateTime.Now, -1, null, Errors.Failed);
                    }
                }
                catch (Exception ex)
                {
                    DMEEditor.AddLogMessage("Beep", $"Error converting property '{name}' - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                }
             
            }

            return bsonDocument;
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
                    Closeconnection();
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


        #endregion
    }
}
