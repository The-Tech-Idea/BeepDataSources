using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LiteDB;
using TheTechIdea;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.FileManager;
using TheTechIdea.Beep.Report;
using TheTechIdea.Logger;
using TheTechIdea.Util;


namespace LiteDBDataSourceCore
{
    public class LiteDBDataSource : IDataSource,ILocalDB
    {
        private bool disposedValue;
        private LiteDatabase _db;
        private string _connectionString;
        public LiteDBDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = databasetype;
            Category = DatasourceCategory.NOSQL;
            Dataconnection = new FileConnection(DMEEditor);
            InitDataConnection();

            Openconnection(); // Attempt to open connection on initialization
        }
        public string GuidID { get  ; set  ; }
        public DataSourceType DatasourceType { get; set; } = DataSourceType.LiteDB;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.NOSQL;
        public IDataConnection Dataconnection { get  ; set  ; }
        public string DatasourceName { get  ; set  ; }
        public IErrorsInfo ErrorObject { get  ; set  ; }
        public string Id { get  ; set  ; }
        public IDMLogger Logger { get  ; set  ; }
        public List<string> EntitiesNames { get  ; set  ; }
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
        public IDMEEditor DMEEditor { get  ; set  ; }
        public ConnectionState ConnectionStatus { get  ; set  ; }
        public string ColumnDelimiter { get  ; set  ; }
        public string ParameterDelimiter { get  ; set  ; }
        public bool CanCreateLocal { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool InMemory { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public event EventHandler<PassedArgs> PassEvent;

        public IErrorsInfo BeginTransaction(PassedArgs args)
        {
            throw new NotImplementedException();
        }

        public bool CheckEntityExist(string EntityName)
        {
            ErrorsInfo erretval = new ErrorsInfo();
            erretval.Flag = Errors.Ok;
            erretval.Message = "Executed Successfully";
            bool retval = false;

            try
            {
                if (_db == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection(); // Ensure the database is connected
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    var collection = _db.GetCollection<BsonDocument>(EntityName);
                    long count = collection.Count();

                    if (count > 0)
                    {
                        retval = true; // Collection exists and has documents
                    }
                    else
                    {
                        retval = false; // Collection does not exist or is empty
                        DMEEditor.AddLogMessage("Beep", "Collection does not exist.", DateTime.Now, -1, null, Errors.Failed);
                    }
                }
                else
                {
                    erretval.Flag = Errors.Failed;
                    erretval.Message = "Could not open connection";
                    retval = false; // Failed to open connection
                }
            }
            catch (Exception ex)
            {
                string methodName = MethodBase.GetCurrentMethod().Name;
                retval = false;
                erretval.Flag = Errors.Failed;
                erretval.Message = $"Error checking existence of the entity {EntityName}: {ex.Message}";
                DMEEditor.AddLogMessage("Beep", $"Error in {methodName} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return retval;
        }

        public ConnectionState Closeconnection()
        {
            if (_db != null)
            {
                _db.Dispose();
                _db = null;
                ConnectionStatus = ConnectionState.Closed;
            }
            return ConnectionStatus;
        }

        public IErrorsInfo Commit(PassedArgs args)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok, Message = "All entities processed successfully." };
            try
            {
                if (_db == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();  // Ensure the database is connected
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    foreach (var entity in entities)
                    {
                        var collection = _db.GetCollection<BsonDocument>(entity.EntityName);
                        long count = collection.Count();

                        if (count == 0) // If the collection is empty, initialize it
                        {
                            // Optionally initialize with a default document if required
                            if (entity.Fields != null && entity.Fields.Count > 0)
                            {
                                var doc = new BsonDocument();
                                foreach (var field in entity.Fields)
                                {
                                    doc[field.fieldname] = new BsonValue((object)null);  // Set default null or another default value
                                }
                                collection.Insert(doc);  // Insert the initial document to create the collection
                            }

                            // Add indexes specified in the EntityStructure
                            foreach (var field in entity.Fields)
                            {
                                if (field.IsKey)  // Assuming 'IsKey' implies an index should be created
                                {
                                    collection.EnsureIndex(field.fieldname);
                                }
                            }
                        }
                        else
                        {
                            retval.Message += $" Collection {entity.EntityName} already initialized; ";
                        }
                    }
                }
                else
                {
                    retval.Flag = Errors.Failed;
                    retval.Message = "Database connection is not open.";
                }
            }
            catch (Exception ex)
            {
                retval.Flag = Errors.Failed;
                retval.Message = $"Error creating entities: {ex.Message}";
            }

            return retval;
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            ErrorsInfo retval = new ErrorsInfo();
            retval.Flag = Errors.Ok;
            retval.Message = "Executed Successfully "; ;
            bool success = false;
            try
            {
                if (_db == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();  // Ensure the database is connected
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    // Check if the collection already exists by trying to get it and seeing if any records exist
                    var collection = _db.GetCollection<BsonDocument>(entity.EntityName);
                    var exists = collection.Count() > 0;

                    if (!exists)
                    {
                        // Optionally, you might want to initialize the collection with an index or a first document
                        // For example, initializing with a document
                        if (entity.Fields != null && entity.Fields.Count > 0)
                        {
                            var doc = new BsonDocument();
                            foreach (var field in entity.Fields)
                            {
                                doc[field.fieldname] = new BsonValue((object)null);  // Set default null or another default value
                            }
                            collection.Insert(doc);  // Insert the initial document to create the collection
                        }

                        // Add indexes specified in the EntityStructure
                        foreach (var field in entity.Fields)
                        {
                            if (field.IsKey)  // Assuming 'IsKey' implies an index should be created
                            {
                                collection.EnsureIndex(field.fieldname);
                            }
                        }
                        success = true;
                    }
                    else
                    {
                        retval.Flag = Errors.Failed;
                        retval.Message = "Collection already exists.";
                        DMEEditor.AddLogMessage("Beep", "Collection already exists.", DateTime.Now, -1, null, Errors.Failed);
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
                success = false;
                DMEEditor.AddLogMessage("Beep", $"error in {methodName} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return success;
        }

        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            ErrorsInfo retval = new ErrorsInfo();
            try
            {
                if (_db == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection(); // Ensure the database is connected
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    var collection = _db.GetCollection<BsonDocument>(EntityName);
                    BsonValue idValue = GetIdentifierValue(UploadDataRow); // Extract ID from DataRow

                    var result = collection.Delete(idValue); // Delete the document by its ID
                    if (result)
                    {
                        retval.Flag = Errors.Ok;
                        retval.Message = "Entity deleted successfully.";
                    }
                    else
                    {
                        retval.Flag = Errors.Failed;
                        retval.Message = "No document found with the specified identifier.";
                    }
                }
                else
                {
                    retval.Flag = Errors.Failed;
                    retval.Message = "Database connection is not open.";
                }
            }
            catch (Exception ex)
            {
                retval.Flag = Errors.Failed;
                retval.Message = $"Error deleting entity: {ex.Message}";
            }

            return retval;
        }

        public IErrorsInfo EndTransaction(PassedArgs args)
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
                    
                    var collectionNames = _db.GetCollectionNames().ToList();
                    foreach (var item in collectionNames)
                    {
                        EntitiesNames.Add(item);

                    }
                    // Synchronize the Entities list to match the current collection names
                    if (Entities != null)
                    {
                        var entitiesToRemove = Entities.Where(e => !EntitiesNames.Contains(e.EntityName)).ToList();
                        foreach (var item in entitiesToRemove)
                        {
                            Entities.Remove(item);
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
                    var collection = _db.GetCollection<BsonDocument>(EntityName);

                    // Build the LiteDB BsonExpression from the list of AppFilter
                    var bsonExpression = BuildLiteDBExpression(filter);

                    // Execute the query
                    var documents = collection.Find(bsonExpression);

                    // Optionally, convert BSON documents to a specific object type if necessary
                    // This conversion would depend on the expected return type and data structure
                    result = documents.ToList(); // Convert to List to ensure the query executes

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
        public object GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok, Message = "Get Entity Successfully" };
            object result = null;
            try
            {
                if (_db == null)
                {
                    Openconnection();  // Ensure the database connection is open
                }

                var collection = _db.GetCollection<BsonDocument>(EntityName);

                // Convert AppFilters to LiteDB BsonExpression
                var bsonExpression = BuildLiteDBExpression(filter);

                // Calculate pagination parameters
                int skipAmount = (pageNumber - 1) * pageSize;

                // Apply the filter and pagination to the query
                // Here we directly use skip and limit parameters in the Find method
                var results = collection.Find(bsonExpression, skipAmount, pageSize);

                result = results.ToList();  // Convert to List to realize the query and gather results
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

        public List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            throw new NotImplementedException();
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

        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            ErrorsInfo retval = new ErrorsInfo();
            retval.Flag = Errors.Ok;
            retval.Message = "Get Entity Structure Successfully";
            EntityStructure result = new EntityStructure();

            try
            {
                // Check if entity structure is already loaded and refresh is not requested
                if (!refresh && Entities != null && Entities.Count > 0)
                {
                    result = Entities.Find(c => c.EntityName.Equals(EntityName, StringComparison.OrdinalIgnoreCase));
                    if (result != null)
                    {
                        return result;
                    }
                }

                // Ensure the connection is open
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                // Retrieve the structure from the database
                if (ConnectionStatus == ConnectionState.Open)
                {
                    var collection = _db.GetCollection<BsonDocument>(EntityName);
                    var firstDocument = collection.Query().First(); // Get the first document to infer the structure

                    if (firstDocument != null)
                    {
                        result.Fields = new List<EntityField>();
                        foreach (var key in firstDocument.Keys)
                        {
                            result.Fields.Add(new EntityField
                            {
                                fieldname = key,
                                fieldtype = GetDotNetTypeFromBsonType(firstDocument[key].Type)
                            });
                        }
                        result.IsLoaded = true;
                    }
                    else
                    {
                        DMEEditor.AddLogMessage("Beep", "No documents found in the collection, unable to infer structure.", DateTime.Now, -1, null, Errors.Failed);
                    }
                }
            }
            catch (Exception ex)
            {
                string methodName = MethodBase.GetCurrentMethod().Name;
                retval.Flag = Errors.Failed;
                retval.Message = ex.Message;
                DMEEditor.AddLogMessage("Beep", $"error in {methodName} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return null;
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

                  return GetEntityStructure(EntityName,refresh);
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
            try
            {
                EntityStructure x = GetEntityStructure(EntityName,false);
                DMTypeBuilder.CreateNewObject(DMEEditor, "Beep." + DatasourceName, EntityName, x.Fields);
                result = DMTypeBuilder.myType;
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
        public ConnectionState Openconnection()
        {
            try
            {
                _db = new LiteDatabase(_connectionString);
                ConnectionStatus = ConnectionState.Open;
            }
            catch (Exception ex)
            {
               DMEEditor.AddLogMessage("Beep",$"Failed to open LiteDB connection: " + ex.Message,DateTime.Now,-1,null, Errors.Failed);
                ConnectionStatus = ConnectionState.Closed;
            }
            return ConnectionStatus;
        }
        public object RunQuery(string qrystr)
        {
            ErrorsInfo retval = new ErrorsInfo();
            object result = null;
            try
            {
                if (_db == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection(); // Ensure the database is connected
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    // Assuming you know which collection to query against, or it's part of the qrystr
                    var collection = _db.GetCollection<BsonDocument>("DefaultCollection");

                    // If qrystr is expected to be a direct BSON expression
                    var expression = BsonExpression.Create(qrystr);
                    var docs = collection.Find(expression);
                    result = docs.ToList(); // Materialize query results to a list
                }
                else
                {
                    throw new InvalidOperationException("Database connection is not open.");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error running query: {ex.Message}", ex);
            }

            return result;
        }
        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            throw new NotImplementedException();
        }
        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok, Message = "Batch update initiated." };
            int count = 0;
            int successCount = 0;

            try
            {
                if (_db == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection(); // Ensure the database is connected
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    var collection = _db.GetCollection<BsonDocument>(EntityName);

                    // Assuming UploadData is an IEnumerable of data rows or POCOs
                    IEnumerable<object> items = UploadData as IEnumerable<object>;
                    if (items == null)
                    {
                        DMEEditor.AddLogMessage("Beep", $"UploadData must be an IEnumerable type.", DateTime.Now, -1, null, Errors.Failed);
                        return new ErrorsInfo { Flag = Errors.Failed, Message = "UploadData must be an IEnumerable type." };
                    }

                    foreach (var item in items)
                    {
                        BsonDocument docToUpdate = ConvertToBsonDocument(item);
                        if (!docToUpdate.ContainsKey("_id"))
                        {
                            DMEEditor.AddLogMessage("Beep", $"Each document must contain an '_id' field for updates.", DateTime.Now, -1, null, Errors.Failed);
                        }
                        else
                        {

                            var id = docToUpdate["_id"];
                            bool result = collection.Update(id, docToUpdate);

                            if (result)
                            {
                                successCount++;
                            }
                        }


                        count++;
                        progress?.Report(new PassedArgs { Messege = $"Updating {count} of {items}", ParameterInt1 = count });
                    }

                    retval.Message = $"{successCount} out of {count} entities updated successfully.";
                }
                else
                {
                    retval.Flag = Errors.Failed;
                    retval.Message = "Database connection is not open.";
                }
            }
            catch (Exception ex)
            {
                string methodName = MethodBase.GetCurrentMethod().Name;
                retval.Flag = Errors.Failed;
                retval.Message = $"Error during batch update: {ex.Message}";
                DMEEditor.AddLogMessage("Beep", $"Error in {methodName} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return retval;
        }
        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok, Message = "Data inserted successfully." };

            try
            {
                // Ensure database connection is open
                if (_db == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    var collection = _db.GetCollection<BsonDocument>(EntityName);

                    // Determine the type of InsertedData and convert it to BsonDocument if necessary
                    BsonDocument docToInsert = ConvertToBsonDocument(InsertedData);

                    collection.Insert(docToInsert);
                    retval.Flag = Errors.Ok;
                    retval.Message = "Data inserted successfully.";
                }
                else
                {
                    retval.Flag = Errors.Failed;
                    retval.Message = "Database connection is not open.";
                }
            }
            catch (Exception ex)
            {
                string methodName = MethodBase.GetCurrentMethod().Name;
                retval.Flag = Errors.Failed;
                retval.Message = "Error inserting data: " + ex.Message;
                DMEEditor.AddLogMessage("Beep", $"error in {methodName} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return retval;
        }
        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            ErrorsInfo retval = new ErrorsInfo();
            try
            {
                if (_db == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection(); // Ensure the database is connected
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    var collection = _db.GetCollection<BsonDocument>(EntityName);
                    BsonDocument docToUpdate = ConvertToBsonDocument(UploadDataRow);

                    // Assuming an "_id" field is used as a unique identifier
                    if (!docToUpdate.ContainsKey("_id"))
                    {
                        DMEEditor.AddLogMessage("Beep", "Document must contain an '_id' field for updates.", DateTime.Now, -1, null, Errors.Failed);
                    }

                    var id = docToUpdate["_id"];
                    var result = collection.Update(docToUpdate);

                    if (result)
                    {
                        retval.Flag = Errors.Ok;
                        retval.Message = "Document updated successfully.";
                    }
                    else
                    {
                        retval.Flag = Errors.Failed;
                        retval.Message = "No document found with the specified ID.";
                    }
                }
                else
                {
                    retval.Flag = Errors.Failed;
                    retval.Message = "Database connection is not open.";
                }
            }
            catch (Exception ex)
            {
                string methodName = MethodBase.GetCurrentMethod().Name;
                retval.Flag = Errors.Failed;
                retval.Message = $"Error updating document: {ex.Message}";
                DMEEditor.AddLogMessage("Beep", $"error in {methodName} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return retval;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_db != null)
                    {
                        _db.Dispose();
                        _db = null;
                    }
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~LiteDBDataSource()
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

        public Task<double> GetScalarAsync(string query)
        {
            throw new NotImplementedException();
        }

        public double GetScalar(string query)
        {
            throw new NotImplementedException();
        }
        private BsonDocument ToBsonDocument(object data)
        {
            if (data is BsonDocument doc)
                return doc;

            // Check if the data is already a BsonDocument
            if (data is BsonDocument bson)
                return bson;

            // Use LiteDB's BsonMapper to convert POCO to BsonDocument
            var mapper = new BsonMapper();
            return mapper.ToDocument(data);
        }
        private BsonExpression BuildLiteDBExpression(List<AppFilter> filters)
        {
            // Start with a base expression that selects all documents
            string expression = "";

            foreach (var filter in filters)
            {
                if (!string.IsNullOrEmpty(expression))
                    expression += " and ";

                switch (filter.Operator)
                {
                    case "==":
                        expression += $"$.{filter.FieldName} = {BsonExpressionValue(filter.FilterValue)}";
                        break;
                    case ">":
                        expression += $"$.{filter.FieldName} > {BsonExpressionValue(filter.FilterValue)}";
                        break;
                    case "<":
                        expression += $"$.{filter.FieldName} < {BsonExpressionValue(filter.FilterValue)}";
                        break;
                        // Add more cases as necessary for your application
                }
            }

            return string.IsNullOrEmpty(expression) ? BsonExpression.Create("$") : BsonExpression.Create(expression);
        }

        private string BsonExpressionValue(string value)
        {
            // Properly format the value based on its type, assuming it's a string here
            return $"\"{value}\""; // Quotes are necessary for string values in expressions
        }
        private EntityStructure GetEntityStructureFromBson(BsonDocument document, string entityName)
        {
            EntityStructure structure = new EntityStructure { EntityName = entityName };
            structure.Fields = new List<EntityField>();

            // Iterate over each key-value pair in the BsonDocument
            foreach (var element in document)
            {
                string fieldType = GetDotNetTypeFromBsonType(element.Value.Type);
                structure.Fields.Add(new EntityField
                {
                    fieldname = element.Key,
                    fieldtype = fieldType
                });
            }

            return structure;
        }

        private string GetDotNetTypeFromBsonType(BsonType bsonType)
        {
            switch (bsonType)
            {
                case BsonType.Int32:
                    return "System.Int32";
                case BsonType.Double:
                    return "System.Double";
                case BsonType.String:
                    return "System.String";
                case BsonType.Document:
                    return "System.Collections.Generic.Dictionary<string, object>";
                case BsonType.Array:
                    return "System.Collections.Generic.List<object>";
                case BsonType.Boolean:
                    return "System.Boolean";
                case BsonType.DateTime:
                    return "System.DateTime";
                case BsonType.ObjectId:
                    return "LiteDB.ObjectId"; // Note: LiteDB's ObjectId type
                default:
                    return "System.Object"; // Fallback type
            }
        }
        private BsonValue GetIdentifierValue(object data)
        {
            if (data is BsonDocument bson)
            {
                if (bson.ContainsKey("_id"))
                    return bson["_id"];
            }
            else if (data is DataRow row)
            {
                if (row.Table.Columns.Contains("_id"))
                    return new BsonValue(row["_id"]);
            }
            else
            {
                // Assuming data is a POCO
                var property = data.GetType().GetProperty("_id");
                if (property != null)
                    return new BsonValue(property.GetValue(data));
            }

            throw new ArgumentException("Data does not contain an identifiable '_id' property.");
        }
        private BsonDocument ConvertToBsonDocument(object data)
        {
            if (data is BsonDocument bson)
            {
                return bson;
            }
            else if (data is DataRow dataRow)
            {
                // Convert DataRow to BsonDocument
                var doc = new BsonDocument();
                foreach (DataColumn column in dataRow.Table.Columns)
                {
                    doc[column.ColumnName] = new BsonValue(dataRow[column]);
                }
                return doc;
            }
            else
            {
                // Assuming data is a POCO, serialize to BsonDocument
                return BsonMapper.Global.ToDocument(data.GetType(), data);
            }
        }
        private void InitDataConnection()
        {
            if(Dataconnection == null)
            {
                Dataconnection = new FileConnection(DMEEditor);
            }
            if(DatasourceName != null && DMEEditor.ConfigEditor.DataConnections.Count > 0)
            {
                Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == DatasourceName).FirstOrDefault();
            }else
            {
                Dataconnection.ConnectionProp = new ConnectionProperties();
            }
            if (_connectionString == null && Dataconnection.ConnectionProp != null)
            {
                _connectionString = Dataconnection.ConnectionProp.ConnectionString;
            }else
            {
                _connectionString = "";
            }
            if(Dataconnection.ConnectionProp==null)
            {
                Dataconnection.ConnectionProp = new ConnectionProperties();
            }
            Dataconnection.ConnectionProp.IsLocal= true;

        }
        #region "LocalDB"
        public bool CreateDB()
        {
            InitDataConnection();
            return Openconnection() == ConnectionState.Open;
        }

        public bool CreateDB(bool inMemory)
        {
            InitDataConnection();
            Dataconnection.ConnectionProp.ConnectionString = Path.Combine(DMEEditor.ConfigEditor.Config.DataFilePath,"LiteDb.db");
            _connectionString = Dataconnection.ConnectionProp.ConnectionString;

            return Openconnection() == ConnectionState.Open;
        }

        public bool CreateDB(string filepathandname)
        {
            if(string.IsNullOrEmpty(filepathandname))
            {
                return false;
            }
            InitDataConnection();
            if(filepathandname != null)
            {
                Dataconnection.ConnectionProp.ConnectionString = filepathandname;
                _connectionString = Dataconnection.ConnectionProp.ConnectionString;
            }
            else
            {
                Dataconnection.ConnectionProp.ConnectionString = Path.Combine(DMEEditor.ConfigEditor.Config.DataFilePath, "LiteDb.db");
                _connectionString = Dataconnection.ConnectionProp.ConnectionString;
            }

            
            return Openconnection() == ConnectionState.Open;
        }

        public bool DeleteDB()
        {
            if(_db != null)
            {
                _db.Dispose();
                _db = null;
            }
            if (File.Exists(_connectionString))
            {
                File.Delete(_connectionString);
            }
            return true;
        }

        public IErrorsInfo DropEntity(string EntityName)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok, Message = "Data inserted successfully." };

            try
            {
                // Ensure database connection is open
                if (_db == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    DeleteEntity(EntityName, null);
                }
                else
                {
                    retval.Flag = Errors.Failed;
                    retval.Message = "Database connection is not open.";
                }
            }
            catch (Exception ex)
            {
                string methodName = MethodBase.GetCurrentMethod().Name;
                retval.Flag = Errors.Failed;
                retval.Message = "Error inserting data: " + ex.Message;
                DMEEditor.AddLogMessage("Beep", $"error in {methodName} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return retval;
           
        }

        public bool CopyDB(string DestDbName, string DesPath)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok, Message = "Get Entity Successfully" };
            bool result = false;
            try
            {
                if (_db == null)
                {
                    Openconnection();  // Ensure the database connection is open
                }
                // copy file db to destpath/destdbname
                // 
                if (ConnectionStatus == ConnectionState.Open)
                {
                    if (File.Exists(_connectionString))
                    {
                        File.Copy(_connectionString, Path.Combine(DesPath, DestDbName));
                        result = true;
                    }
                    else
                    {
                        result = false;
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
                return false;
            }

            return result;

        }
        #endregion

    }
}
