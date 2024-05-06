
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

using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization;

using System;
using System.Collections.Generic;
using System.Linq;



namespace TheTechIdea.Beep.NOSQL
{
    [AddinAttribute(Category = DatasourceCategory.NOSQL, DatasourceType =  DataSourceType.MongoDB)]
    public class MongoDBDataSource : IDataSource
    {
        public string GuidID { get; set; }
        public event EventHandler<PassedArgs> PassEvent;
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
        public List<object> Records { get ; set ; }
        public ConnectionState ConnectionStatus { get ; set ; }
        public DataTable SourceEntityData { get ; set ; }
        public string CurrentDatabase { get; set; }
        public virtual string ColumnDelimiter { get; set; } = "''";
        public virtual string ParameterDelimiter { get; set; } = ":";
        #region "MongoDB Properties"
        private IMongoClient _client;
        private IMongoDatabase _database;
        private string _connectionString;
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
            Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == datasourcename).FirstOrDefault();
            Dataconnection.ConnectionProp.Category = DatasourceCategory.NOSQL;
            Dataconnection.ConnectionProp.DatabaseType = DataSourceType.MongoDB;
            _connectionString = Dataconnection.ConnectionProp.ConnectionString;
            CurrentDatabase = Dataconnection.ConnectionProp.Database;
            if (CurrentDatabase != null)
            {
                if (CurrentDatabase.Length > 0)
                {
                    _client = new MongoClient(_connectionString);
                    GetEntitesList();
                }
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
            try
            {
                ConnectionStatus = ConnectionState.Open;
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Closed;
                DMEEditor.AddLogMessage("Beep", $"Could not open MonogoDB {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ConnectionStatus;
        }

        public ConnectionState Closeconnection()
        {
            try
            {
                ConnectionStatus = ConnectionState.Closed;
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                DMEEditor.AddLogMessage("Beep", $"Could not close MonogoDB {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ConnectionStatus;
        }

      

        public bool CheckEntityExist(string EntityName)
        {
            bool retval = false;
            try
            {
                retval =true;
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
            bool retval = false;
            try
            {
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
                    var filter = ParseFilterString(filterstr);

                    var documents = await collection.Find(filter).ToListAsync();




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

                    //Get data from the custom query and put it in the dataset
                    var db = _client.GetDatabase(CurrentDatabase);
                    var collection = db.GetCollection<BsonDocument>(EntityName);
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

                    //Get data from the custom query and put it in the dataset
                    var db = _client.GetDatabase(CurrentDatabase);
                    var collection = db.GetCollection<BsonDocument>(EntityName);
                    // Building a MongoDB filter from AppFilter list
                    var filterString = BuildFilterString(filter);
                    var filters = ParseFilterString(filterString);

                    // Perform the query
                    var documents = collection.Find(filters).ToList();

                    // Optionally convert BSON documents to a specific object type if needed
                    // Assuming you have a method to determine the type from entityName
                    Type entityType = GetEntityType(EntityName);
                    result = ConvertBsonDocumentsToObjects(documents, entityType, GetEntityStructureFromBson(documents.FirstOrDefault(), EntityName));
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
                    var firstDocument = collection.Find(new BsonDocument()).FirstOrDefault();
                    if (firstDocument != null)
                    {
                        result = GetEntityStructureFromBson(firstDocument, EntityName);
                        result.IsLoaded = true;
                    }
                    else
                    {
                        retval.Flag = Errors.Failed;
                        retval.Message = "No documents found in the collection.";
                        result = null;
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
        public EntityStructure GetEntityStructure(string EntityName, bool refresh = false)
        {
            ErrorsInfo retval = new ErrorsInfo();
            retval.Flag = Errors.Ok;
            retval.Message = "Get Entity Structure Successfully";
            EntityStructure result = new EntityStructure();
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
                    var collection = db.GetCollection<BsonDocument>(EntityName);
                    // Attempt to get the first document to infer the structure
                    var firstDocument = collection.Find(new BsonDocument()).FirstOrDefault();
                    if (firstDocument != null)
                    {
                        result = GetEntityStructureFromBson(firstDocument, EntityName);
                        result.IsLoaded = true;
                    }
                    else
                    {
                        retval.Flag = Errors.Failed;
                        retval.Message = "No documents found in the collection.";
                        result = null;
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

        public DataTable GetEntityDataTable(string EntityName, string filterstr)
        {
            ErrorsInfo retval = new ErrorsInfo();
            retval.Flag = Errors.Ok;
            retval.Message = "Get Entity DataTable";
            DataTable result = new DataTable();
            try
            {

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
            Type result=null;
            try
            {
                EntityStructure x = GetEntityStructure(EntityName);
                DMTypeBuilder.CreateNewObject(DMEEditor, "Beep." + DatasourceName, EntityName, x.Fields);
                result= DMTypeBuilder.myType;
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

        public  object RunQuery( string qrystr)
        {
            ErrorsInfo retval = new ErrorsInfo();
            retval.Flag = Errors.Ok;
            retval.Message = "Entities Created Successfully";
            object result = null;
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
            return result;
        }
        public virtual IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            ErrorsInfo retval = new ErrorsInfo();
            retval.Flag = Errors.Ok;
            retval.Message = "Entity Updated Successfully";
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
        public IErrorsInfo DeleteEntity(string EntityName, object DeletedDataRow)
        {
            ErrorsInfo retval = new ErrorsInfo();
            retval.Flag = Errors.Ok;
            retval.Message = "Entity Deleted Successfully";
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
            throw new NotImplementedException();
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
                    var collection = _database.GetCollection<BsonDocument>(EntityName);
                    collection.InsertOne((BsonDocument)InsertedData);
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
        #region "Parsing and Filtering"

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
    public List<object> ConvertBsonDocumentsToObjects(List<BsonDocument> documents, Type type, EntityStructure entStructure)
    {
        List<object> records = new List<object>();
        Dictionary<string, PropertyInfo> properties = new Dictionary<string, PropertyInfo>();

        // Cache property info and ensure the property exists in the target type
        foreach (var field in entStructure.Fields)
        {
            PropertyInfo propInfo = type.GetProperty(field.fieldname, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (propInfo != null)
            {
                properties.Add(field.fieldname.ToLower(), propInfo);
            }
            else
            {
                // Optionally log or handle the error when a property is not found
                throw new InvalidOperationException($"Property {field.fieldname} not found on type {type.Name}.");
            }
        }

        foreach (var document in documents)
        {
            dynamic instance = Activator.CreateInstance(type);
            foreach (var kvp in properties)
            {
                var fieldName = kvp.Key;
                var propInfo = kvp.Value;

                if (document.Contains(fieldName))
                {
                    var bsonValue = document[fieldName];
                    if (!bsonValue.IsBsonNull)
                    {
                        try
                        {
                            // Use BsonSerializer if possible for better compatibility with complex types and arrays
                            object value = BsonSerializer.Deserialize(bsonValue.ToJson(), propInfo.PropertyType);
                            propInfo.SetValue(instance, value, null);
                        }
                        catch (Exception ex)
                        {
                            // Handle or log the error appropriately
                            throw new InvalidOperationException($"Error converting field {fieldName} to type {propInfo.PropertyType.Name}: {ex.Message}", ex);
                        }
                    }
                }
            }
            records.Add(instance);
        }

        return records;
    }
    public EntityStructure GetEntityStructureFromBson(BsonDocument document, string entityName = null)
        {
            EntityStructure entityData = new EntityStructure();
            try
            {
                // Use provided entity name or default to a generic name
                entityData.EntityName = entityName ?? "DefaultEntityName";
                List<EntityField> fields = new List<EntityField>();

                int fieldIndex = 0;
                foreach (var element in document.Elements)
                {
                    EntityField field = new EntityField
                    {
                        fieldname = element.Name,
                        fieldtype = GetTypeFromBsonValue(element.Value), // Convert BsonType to a .NET type string
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
    private string GetTypeFromBsonValue(BsonValue value)
    {
        switch (value.BsonType)
        {
            case BsonType.String:
                return "System.String";
            case BsonType.Int32:
                return "System.Int32";
            case BsonType.Int64:
                return "System.Int64";
            case BsonType.Boolean:
                return "System.Boolean";
            case BsonType.DateTime:
                return "System.DateTime";
            case BsonType.Double:
                return "System.Double";
            case BsonType.Decimal128:
                return "System.Decimal";
            case BsonType.ObjectId:
                return "MongoDB.Bson.ObjectId";  // Uses MongoDB's ObjectId type
            case BsonType.Binary:
                return "System.Byte[]";
            case BsonType.Array:
                return "System.Collections.Generic.List<System.Object>"; // Simplified, actual implementation may need specific list types
            case BsonType.Document:
                return "MongoDB.Bson.BsonDocument";
            case BsonType.Null:
                return "System.Object"; // Null has no direct type, handled as Object
            case BsonType.RegularExpression:
                return "System.Text.RegularExpressions.Regex";
            case BsonType.JavaScript:
                return "System.String"; // JavaScript code as string
            case BsonType.Symbol:
                return "System.String"; // Symbols are deprecated and can be handled as strings
            case BsonType.JavaScriptWithScope:
                return "System.String"; // JavaScript code with scope as string
            case BsonType.Timestamp:
                return "System.DateTime"; // Timestamps can be treated as DateTime
            case BsonType.Undefined:
                return "System.Object"; // Undefined is generally not used; handle as Object
            case BsonType.MinKey:
                return "MongoDB.Bson.BsonMinKey"; // Specific BSON type
            case BsonType.MaxKey:
                return "MongoDB.Bson.BsonMaxKey"; // Specific BSON type
            default:
                return "System.Object"; // Catch-all for any types not explicitly handled
        }
    }
    private string BuildFilterString(List<AppFilter> filters)
        {
            return string.Join(" AND ", filters.Select(f => $"{f.FieldName} {f.Operator} '{f.FilterValue}'"));
        }
        private void SetObjects(string Entityname)
        {
            if (!ObjectsCreated || Entityname != lastentityname)
            {
                DataStruct = GetEntityStructure(Entityname, true);
             
                enttype = GetEntityType(Entityname);
                ObjectsCreated = true;
                lastentityname = Entityname;
            }
        }
        Type enttype = null;
        bool ObjectsCreated = false;
        string lastentityname = null;
        EntityStructure DataStruct = null;
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
