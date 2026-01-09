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
using DataManagementModels.DriversConfigurations;
using DataManagementModels.Editor;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.Helpers;
using System.ComponentModel;
using TheTechIdea.Beep.Vis;

namespace LiteDBDataSourceCore
{
    [AddinAttribute(Category = DatasourceCategory.NOSQL, DatasourceType = DataSourceType.LiteDB)]
    public class LiteDBDataSource : IDataSource,ILocalDB
    {
        private bool disposedValue;
        private LiteDatabase db;
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
            

            //Openconnection(); // Attempt to open connection on initialization
        }
        public string GuidID { get  ; set  ; }
        public DataSourceType DatasourceType { get; set; } = DataSourceType.LiteDB;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.NOSQL;
        public IDataConnection Dataconnection { get  ; set  ; }
        public string DatasourceName { get  ; set  ; }
        public IErrorsInfo ErrorObject { get  ; set  ; }
        public string Id { get  ; set  ; }
        public IDMLogger Logger { get  ; set  ; }
        public List<string> EntitiesNames { get  ; set  ; }=new List<string>();
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
        public IDMEEditor DMEEditor { get  ; set  ; }
        public ConnectionState ConnectionStatus { get  ; set  ; }
        public string ColumnDelimiter { get  ; set  ; }
        public string ParameterDelimiter { get  ; set  ; }
        public bool CanCreateLocal { get; set; } = true;
        public bool InMemory { get; set; } = false;

        public event EventHandler<PassedArgs> PassEvent;
        Type enttype = null;
        bool ObjectsCreated = false;
        string lastentityname = null;
        EntityStructure DataStruct = null;
        string DBfilepathandname=string.Empty;
        #region "Misc"
        public IEnumerable<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            // LiteDB doesn't have child tables
            return new List<ChildRelation>();
        }

        public IEnumerable<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            List<ETLScriptDet> scripts = new List<ETLScriptDet>();
            try
            {
                var entitiesToScript = entities ?? Entities;
                if (entitiesToScript != null && entitiesToScript.Count > 0)
                {
                    foreach (var entity in entitiesToScript)
                    {
                        var script = new ETLScriptDet
                        {
                            EntityName = entity.EntityName,
                            ScriptType = "CREATE",
                            ScriptText = $"# LiteDB collection: {entity.EntityName}\n# Collections are created automatically when first document is inserted"
                        };
                        scripts.Add(script);
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in GetCreateEntityScript: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return scripts;
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
        public IEnumerable<string> GetEntitesList()
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
                    using (var db = new LiteDatabase(_connectionString))
                    {
                        var collectionNames = db.GetCollectionNames().ToList();
                        foreach (var item in collectionNames)
                        {
                            EntitiesNames.Add(item);

                        }

                        // Synchronize the Entities list to match the current collection names
                       
                    }
                    if (Entities == null)
                    {
                        Entities = new List<EntityStructure>();
                    }
                   
                   
                    if (EntitiesNames == null)
                    {
                        EntitiesNames = new List<string>();
                    }
                   
                
                    if (Entities != null)
                    {
                        var entitiesToRemove = Entities.Where(e => !EntitiesNames.Contains(e.EntityName) && !string.IsNullOrEmpty(e.CustomBuildQuery)).ToList();
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

        public IEnumerable<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            // LiteDB doesn't have foreign keys
            return new List<RelationShipKeys>();
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
                    using (var db = new LiteDatabase(_connectionString))
                    {
                        var collection = db.GetCollection<BsonDocument>(EntityName);
                        var firstDocument = collection.Query().First(); // Get the first document to infer the structure
                        var list = collection.FindAll().ToList(); // Get all documents to infer the structure
                        if (firstDocument != null)
                        {
                            if (DataStruct != null)
                            {
                                if (DataStruct.EntityName == null || DataStruct.EntityName != EntityName)
                                {
                                    DataStruct = CompileSchemaFromDocuments(list, EntityName);
                                }
                            }
                            else
                                DataStruct = CompileSchemaFromDocuments(list, EntityName);


                            ObjectsCreated = true;
                            // Optionally convert BSON documents to a specific object type if needed
                            // Assuming you have a method to determine the type from entityName
                            //  Type entityType = GetEntityType(EntityName);
                            enttype = GetEntityType(EntityName);
                        }
                    }
                    // result = GetEntityStructureFromBson(firstDocument, EntityName);
                    if (Entities == null)
                    {
                        Entities = new List<EntityStructure>();
                    }
                    if (Entities.Count > 0 && !Entities.Any(p => p.EntityName == EntityName))
                    {
                        Entities.Add(DataStruct);
                    }
                    if (Entities.Count == 0 && !Entities.Any(p => p.EntityName == EntityName))
                    {
                        Entities.Add(DataStruct);
                    }
                    if (EntitiesNames == null)
                    {
                        EntitiesNames = new List<string>();
                    }
                    if (EntitiesNames.Count == 0)
                    {
                        EntitiesNames = Entities.Select(p => p.EntityName).ToList();
                    }
                    if (EntitiesNames.Count > 0 && !EntitiesNames.Any(p => p == EntityName))
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
                result = DataStruct;
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
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }
                if (ConnectionStatus == ConnectionState.Open)
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

                    DMTypeBuilder.CreateNewObject(DMEEditor, "Beep." + DatasourceName, EntityName, DataStruct.Fields);
                    result = DMTypeBuilder.myType;
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
            ErrorsInfo erretval = new ErrorsInfo();
            erretval.Flag = Errors.Ok;
            erretval.Message = "Executed Successfully";
            bool retval = false;

            try
            {
                if (db == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection(); // Ensure the database is connected
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    using (var db = new LiteDatabase(_connectionString))
                    {
                        var collection = db.GetCollection<BsonDocument>(EntityName);
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

        #endregion
        #region "DDL"
        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (dDLScripts != null && !string.IsNullOrEmpty(dDLScripts.ScriptText))
                {
                    // Execute script as SQL command
                    ExecuteSql(dDLScripts.ScriptText);
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in RunScript: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok, Message = "All entities processed successfully." };
            try
            {
                if (db == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();  // Ensure the database is connected
                }
                if (Entities == null)
                {
                    Entities = new List<EntityStructure>();
                }


                if (EntitiesNames == null)
                {
                    EntitiesNames = new List<string>();
                }
                if (ConnectionStatus == ConnectionState.Open)
                {
                    foreach (var item in entities)
                    {
                        CreateEntityAs(item);
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
                if (db == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();  // Ensure the database is connected
                }
                if (Entities == null)
                {
                    Entities = new List<EntityStructure>();
                }


                if (EntitiesNames == null)
                {
                    EntitiesNames = new List<string>();
                }
                if (ConnectionStatus == ConnectionState.Open)
                {
                    using (var db = new LiteDatabase(_connectionString))
                    {
                        // Check if the collection already exists by trying to get it and seeing if any records exist
                        var collection = db.GetCollection<BsonDocument>(entity.EntityName);
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
                                    if(field.fieldname != "_id") {
                                        doc[field.fieldname] = new BsonValue((object)null);  // Set default null or another default value
                                    }else
                                        {
                                        doc[field.fieldname] = new BsonValue(Guid.NewGuid().ToString());}
                                    
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
                            if(Entities==null)
                            {
                                Entities = new List<EntityStructure>();
                            }
                            if(EntitiesNames==null)
                            {
                                EntitiesNames = new List<string>();
                            }
                            if(Entities.Count > 0)
                            {
                                if (!Entities.Any(p => p.EntityName == entity.EntityName))
                                {
                                    Entities.Add(entity);
                                }
                              
                            }
                            else
                            {
                                Entities.Add(entity);
                            }
                            if(EntitiesNames.Count > 0)
                            {
                                if (!EntitiesNames.Any(p => p == entity.EntityName))
                                {
                                    EntitiesNames.Add(entity.EntityName);
                                }
                            }
                            else
                            {
                                EntitiesNames.Add(entity.EntityName);
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



        #endregion
        #region "Get Metadata"
        #endregion
        public IErrorsInfo BeginTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                // LiteDB uses file-based transactions
                // Transactions are handled automatically on dispose or commit
                if (db == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in Begin Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return ErrorObject;
        }
        public IErrorsInfo Commit(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                // LiteDB commits automatically when operations complete
                // Explicit commit may not be necessary, but we can ensure consistency
                if (db != null)
                {
                    db.Checkpoint();
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in Commit Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return ErrorObject;
        }
        public IErrorsInfo EndTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                // LiteDB transactions end automatically
                // Ensure checkpoint is called if needed
                if (db != null)
                {
                    db.Checkpoint();
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in End Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return ErrorObject;
        }
        public ConnectionState Openconnection()
        {
            try
            {
                InitDataConnection();
                if (_connectionString == null)
                {
                    throw new Exception("Connection string is empty");
                }
               
                if (File.Exists(DBfilepathandname))
                {
                    DMEEditor.AddLogMessage("Success", "lITEdb Database already exist", DateTime.Now, 0, null, Errors.Ok);
                }
                else
                {
                    
                    DMEEditor.AddLogMessage("Success", "Create lITEdb Database", DateTime.Now, 0, null, Errors.Ok);
                }
                using (var db = new LiteDatabase(_connectionString))
                {
                  
                }

                ConnectionStatus = ConnectionState.Open;
            }
            catch (Exception ex)
            {
               DMEEditor.AddLogMessage("Beep",$"Failed to open LiteDB connection: " + ex.Message,DateTime.Now,-1,null, Errors.Failed);
                ConnectionStatus = ConnectionState.Closed;
            }
            return ConnectionStatus;
        }
        public ConnectionState Closeconnection()
        {
            if (db != null)
            {
                db.Dispose();
                db = null;
                ConnectionStatus = ConnectionState.Closed;
            }
            return ConnectionStatus;
        }
        #region "Query"
        public Task<double> GetScalarAsync(string query)
        {
            return  Task.Run(() => GetScalar(query));
        }

        public double GetScalar(string query)
        {
            try
            {
                using (var db = new LiteDatabase(_connectionString))
                {
                    var result = db.Execute(query);
                    return Convert.ToDouble(result);
                }
             
                   
              
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error in executing scalar query ({ex.Message})", DateTime.Now, 0, "", Errors.Failed);
                return 0.0;
            }
        }

        public IErrorsInfo ExecuteSql(string sql)
        {
            var retval = new ErrorsInfo { Flag = Errors.Ok, Message = "Executed Successfully" };
            try
            {
                using (var db = new LiteDatabase(_connectionString))
                {
                    db.Execute(sql);
                }
               
              
            }
            catch (Exception ex)
            {
                var methodName = MethodBase.GetCurrentMethod().Name;
                retval.Flag = Errors.Failed;
                retval.Message = ex.Message;
                DMEEditor.AddLogMessage("Beep", $"error in {methodName} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return retval;
        }


        public IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            ErrorsInfo retval = new ErrorsInfo();
            retval.Flag = Errors.Ok;
            retval.Message = "Get Entity Successfully";
            List<object> results = new List<object>();
            IEnumerable<BsonDocument> documents;
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }
                if (ConnectionStatus == ConnectionState.Open)
                {
                    using (var db = new LiteDatabase(_connectionString))
                    {
                        var collection = db.GetCollection<BsonDocument>(EntityName);
                        SetObjects(EntityName);

                        if (filter != null && filter.Count > 0)
                        {
                            var bsonExpression = BuildLiteDBExpression(filter);
                            documents = collection.Find(bsonExpression);
                        }
                        else
                        {
                            documents = collection.Count() > 0 ? collection.FindAll() : new List<BsonDocument>();
                        }

                        List<BsonDocument> ls = documents.ToList();
                        var converted = ConvertBsonDocumentsToObjects(ls, enttype, DataStruct);
                        
                        // Convert IBindingListView to IEnumerable<object>
                        if (converted is System.Collections.IEnumerable enumerable)
                        {
                            foreach (var item in enumerable)
                            {
                                results.Add(item);
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                string methodName = MethodBase.GetCurrentMethod().Name;
                retval.Flag = Errors.Failed;
                retval.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"error in {methodName} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return results;
        }
        public PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            PagedResult pagedResult = new PagedResult();
            ErrorObject.Flag = Errors.Ok;

            try
            {
                if (db == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                using (var db = new LiteDatabase(_connectionString))
                {
                    var collection = db.GetCollection<BsonDocument>(EntityName);
                    SetObjects(EntityName);

                    // Get total count
                    var bsonExpression = BuildLiteDBExpression(filter);
                    int totalRecords = (int)collection.Count(bsonExpression);

                    // Calculate pagination parameters
                    int skipAmount = (pageNumber - 1) * pageSize;

                    // Get paginated results
                    var documents = collection.Find(bsonExpression, skipAmount, pageSize);
                    List<BsonDocument> result = documents.ToList();

                    var converted = ConvertBsonDocumentsToObjects(result, enttype, DataStruct);
                    List<object> results = new List<object>();

                    // Convert IBindingListView to List<object>
                    if (converted is System.Collections.IEnumerable enumerable)
                    {
                        foreach (var item in enumerable)
                        {
                            results.Add(item);
                        }
                    }

                    pagedResult.Data = results;
                    pagedResult.TotalRecords = totalRecords;
                    pagedResult.PageNumber = pageNumber;
                    pagedResult.PageSize = pageSize;
                    pagedResult.TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
                    pagedResult.HasNextPage = pageNumber < pagedResult.TotalPages;
                    pagedResult.HasPreviousPage = pageNumber > 1;
                }
            }
            catch (Exception ex)
            {
                string methodName = MethodBase.GetCurrentMethod().Name;
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"error in {methodName} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return pagedResult;
        }
        public Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            return Task.Run(() => GetEntity(EntityName, Filter));
        }

        public IEnumerable<object> RunQuery(string qrystr)
        {
            ErrorsInfo retval = new ErrorsInfo();
            object result = null;
            try
            {
                if (db == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection(); // Ensure the database is connected
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    using (var db = new LiteDatabase(_connectionString))
                    {
                        // Assuming you know which collection to query against, or it's part of the qrystr
                        var collection = db.GetCollection<BsonDocument>("DefaultCollection");

                        // If qrystr is expected to be a direct BSON expression
                        var expression = BsonExpression.Create(qrystr);
                        var docs = collection.Find(expression);
                      //  result = docs.ToList(); // Materialize query results to a list
                        List<BsonDocument> ls = docs.ToList();  // Convert to List to realize the query and gather results
                        result = ConvertBsonDocumentsToObjects(ls, enttype, DataStruct);
                    }
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
        #endregion
        #region "CRUD"
        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            ErrorsInfo retval = new ErrorsInfo();
            try
            {
                if (db == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection(); // Ensure the database is connected
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    using (var db = new LiteDatabase(_connectionString))
                    {
                        var collection = db.GetCollection<BsonDocument>(EntityName);
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
        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok, Message = "Batch update initiated." };
            int count = 0;
            int successCount = 0;
            IEnumerable<object> items;
            try
            {
                if (db == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection(); // Ensure the database is connected
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    using (var db = new LiteDatabase(_connectionString))
                    {
                        var collection = db.GetCollection<BsonDocument>(EntityName);
                        items = UploadData as IEnumerable<object>;

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
                

                    // Assuming UploadData is an IEnumerable of data rows or POCOs
               
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

        public ILiteCollection<T> GetLiteCollection<T>(string EntityName)
        {
            ILiteCollection<T> retval = null;
            try
            {
                if (db == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection(); // Ensure the database is connected
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    using (var db = new LiteDatabase(_connectionString))
                    {
                        retval = db.GetCollection<T>(EntityName);
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error getting LiteCollection: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return retval;
        }
        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok, Message = "Data inserted successfully." };
            EntityStructure entity=null;
            try
            {
                // Ensure database connection is open
                if (db == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    using (var db = new LiteDatabase(_connectionString))
                    {
                        // Get Collection based on entityType or DataStruct or EntityName
                        // user GetLiteCollection method to get the collection
                        // Example usage where 'entType' is resolved at runtime, perhaps from some metadata or user input
                        SetObjects(EntityName);

                        var collection = db.GetCollection<BsonDocument>(EntityName);
                        if(collection.Count()==0)
                        {
                 //           collection.Name= EntityName;
                            collection.EnsureIndex("_id", true);
                        }
                        // Determine the type of InsertedData and convert it to BsonDocument if necessary
                        BsonDocument docToInsert = ConvertToBsonDocument(InsertedData);

                        collection.Insert(docToInsert);
                      
                      
                        retval.Flag = Errors.Ok;
                        retval.Message = "Data inserted successfully.";
                    }
                    if (Entities == null)
                    {
                        Entities = new List<EntityStructure>();
                    }
                    if (EntitiesNames == null)
                    {
                        EntitiesNames = new List<string>();
                    }
                    if (!Entities.Any(p => p.EntityName == EntityName))
                    {
                       entity = GetEntityStructure(EntityName, false);
                    }
                    if (!EntitiesNames.Any(p => p == entity.EntityName))
                    {
                        EntitiesNames.Add(entity.EntityName);
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
                if (db == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection(); // Ensure the database is connected
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    using (var db = new LiteDatabase(_connectionString))
                    {
                        var collection = db.GetCollection<BsonDocument>(EntityName);
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

        #endregion
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

        #region "Helpers"
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
            if(filters == null)
            {
                return BsonExpression.Create("$");
            }
            if (filters.Count == 0)
            {
                return BsonExpression.Create("$");
            }
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
        private EntityStructure CompileSchemaFromDocuments(List<BsonDocument> documents, string entityName)
        {
            EntityStructure entityStructure = new EntityStructure
            {
                EntityName = entityName ?? "DefaultEntityName",
                DatasourceEntityName = entityName,
                OriginalEntityName = entityName,
                DataSourceID = DatasourceName
            };

            Dictionary<string, EntityField> fieldDictionary = new Dictionary<string, EntityField>();
            int fieldIndex = 0;

            foreach (var document in documents)
            {
                foreach (var element in document)
                {
                    if (!fieldDictionary.ContainsKey(element.Key))
                    {
                        EntityField newField = new EntityField
                        {
                            fieldname = element.Key,
                            BaseColumnName = element.Key,
                            fieldtype = GetDotNetTypeStringFromBsonType(element.Value.Type),
                            IsKey = element.Key.Equals("_id", StringComparison.OrdinalIgnoreCase),
                            IsIdentity = element.Key.Equals("_id", StringComparison.OrdinalIgnoreCase),
                            FieldIndex = fieldIndex++
                        };
                        fieldDictionary[element.Key] = newField;
                    }
                }
            }

            entityStructure.Fields = new List<EntityField>(fieldDictionary.Values);
            return entityStructure;
        }
        private string GetDotNetTypeStringFromBsonType(BsonType bsonType)
        {
            List<DatatypeMapping> dataTypeMappings = DataTypeFieldMappingHelper.GetLiteDBDataTypesMapping();
            DatatypeMapping mapping = null;

            switch (bsonType)
            {
                case BsonType.Double:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("Double", StringComparison.InvariantCultureIgnoreCase));
                    return mapping?.NetDataType ?? "System.Double";
                case BsonType.String:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("String", StringComparison.InvariantCultureIgnoreCase));
                    return mapping?.NetDataType ?? "System.String";
                case BsonType.Document:
                    return "System.Object";
                case BsonType.Array:
                    return "System.Collections.Generic.List<object>";
                case BsonType.Binary:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("Binary", StringComparison.InvariantCultureIgnoreCase));
                    return mapping?.NetDataType ?? "System.Byte[]";
                case BsonType.ObjectId:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("ObjectId", StringComparison.InvariantCultureIgnoreCase));
                    return mapping?.NetDataType ?? "LiteDB.ObjectId";
                case BsonType.Boolean:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("Boolean", StringComparison.InvariantCultureIgnoreCase));
                    return mapping?.NetDataType ?? "System.Boolean";
                case BsonType.DateTime:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("DateTime", StringComparison.InvariantCultureIgnoreCase));
                    return mapping?.NetDataType ?? "System.DateTime";
                case BsonType.Null:
                    return mapping?.NetDataType ?? "System.String";
                case BsonType.Int32:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("Int32", StringComparison.InvariantCultureIgnoreCase));
                    return mapping?.NetDataType ?? "System.Int32";
                case BsonType.Int64:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("Int64", StringComparison.InvariantCultureIgnoreCase));
                    return mapping?.NetDataType ?? "System.Int64";
                case BsonType.Decimal:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("Decimal", StringComparison.InvariantCultureIgnoreCase));
                    return mapping?.NetDataType ?? "System.Decimal";
                default:
                    return "System.Object";
            }
        }

        private Type GetDotNetTypeFromBsonType(BsonType bsonType)
        {
            List<DatatypeMapping> dataTypeMappings = DataTypeFieldMappingHelper.GetLiteDBDataTypesMapping();
            DatatypeMapping mapping = null;

            switch (bsonType)
            {
                case BsonType.Double:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("Double", StringComparison.InvariantCultureIgnoreCase));
                    return Type.GetType(mapping?.NetDataType ?? "System.Double");
                case BsonType.String:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("String", StringComparison.InvariantCultureIgnoreCase));
                    return Type.GetType(mapping?.NetDataType ?? "System.String");
                case BsonType.Document:
                    return typeof(object);
                case BsonType.Array:
                    return typeof(Array);
                case BsonType.Binary:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("Binary", StringComparison.InvariantCultureIgnoreCase));
                    return Type.GetType(mapping?.NetDataType ?? "System.Byte[]");
              
                case BsonType.ObjectId:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("ObjectId", StringComparison.InvariantCultureIgnoreCase));
                    return Type.GetType(mapping?.NetDataType ?? "LiteDB.ObjectId");
                case BsonType.Boolean:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("Boolean", StringComparison.InvariantCultureIgnoreCase));
                    return Type.GetType(mapping?.NetDataType ?? "System.Boolean");
                case BsonType.DateTime:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("DateTime", StringComparison.InvariantCultureIgnoreCase));
                    return Type.GetType(mapping?.NetDataType ?? "System.DateTime");
                case BsonType.Null:
                    return typeof(object);
                case BsonType.Int32:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("Int32", StringComparison.InvariantCultureIgnoreCase));
                    return Type.GetType(mapping?.NetDataType ?? "System.Int32");
                case BsonType.Int64:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("Int64", StringComparison.InvariantCultureIgnoreCase));
                    return Type.GetType(mapping?.NetDataType ?? "System.Int64");
                case BsonType.Decimal:
                    mapping = dataTypeMappings.FirstOrDefault(x => x.DataType.Equals("Decimal", StringComparison.InvariantCultureIgnoreCase));
                    return Type.GetType(mapping?.NetDataType ?? "System.Decimal");
                default:
                    return typeof(object);
            }
        }
        private object ConvertBsonValueToNetType(BsonValue bsonValue, Type targetType)
        {
            if (bsonValue.IsNull)
                return null;

            switch (bsonValue.Type)
            {
                case BsonType.Int32:
                    return bsonValue.AsInt32;
                case BsonType.Int64:
                    return bsonValue.AsInt64;
                case BsonType.Double:
                    return bsonValue.AsDouble;
                case BsonType.String:
                    return bsonValue.AsString;
                case BsonType.Document:
                    return bsonValue.AsDocument;
                case BsonType.Array:
                    return bsonValue.AsArray.Select(a => ConvertBsonValueToNetType(a, typeof(object))).ToList();
                case BsonType.Binary:
                    return bsonValue.AsBinary;
                case BsonType.ObjectId:
                    return bsonValue.AsObjectId.ToString(); // Convert ObjectId to string
                case BsonType.Guid:
                    return bsonValue.AsGuid;
                case BsonType.Boolean:
                    return bsonValue.AsBoolean;
                case BsonType.DateTime:
                    return bsonValue.AsDateTime;
                default:
                    return bsonValue;
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
            EntityStructure entStructure = DataStruct;
            var doc = new BsonDocument();
            if(data is null)
            {
                return doc;
            }
            if(data is BsonDocument)
            {
                return (BsonDocument)data;
            }
            if (data is DataRow dataRow)
            {
                // Convert DataRow to BsonDocument using EntityStructure for schema guidance
                foreach (var field in entStructure.Fields)
                {
                    var fieldName = field.fieldname;
                    var value = dataRow.Table.Columns.Contains(fieldName) ? dataRow[fieldName] : DBNull.Value;

                    // Convert value to the appropriate BsonValue type
                    doc[fieldName] = ConvertToBsonValue(field.fieldtype, value);
                }
            }
            else
            {
                // Assuming data is a POCO, manually serialize to BsonDocument using EntityStructure for schema guidance
                foreach (var field in entStructure.Fields)
                {
                    var prop = data.GetType().GetProperty(field.fieldname);
                    if (prop != null)
                    {
                        var value = prop.GetValue(data);
                        doc[field.fieldname] = ConvertToBsonValue(field.fieldtype, value);
                    }
                    else
                    {
                        doc[field.fieldname] = BsonValue.Null;
                    }
                }
            }

            return doc;
        }
        private BsonValue ConvertToBsonValue(string fieldType, object value)
        {
            if (value == null)
            {
                return BsonValue.Null;
            }

            switch (fieldType)
            {
                case "System.Int32":
                    return new BsonValue(Convert.ToInt32(value));
                case "System.Int64":
                    return new BsonValue(Convert.ToInt64(value));
                case "System.Double":
                    return new BsonValue(Convert.ToDouble(value));
                case "System.Decimal":
                    return new BsonValue(Convert.ToDecimal(value));
                case "System.String":
                    return new BsonValue(SanitizeString(value.ToString()));
                case "System.Boolean":
                    return new BsonValue(Convert.ToBoolean(value));
                case "System.DateTime":
                    return new BsonValue(Convert.ToDateTime(value));
                case "System.Guid":
                    return new BsonValue((Guid)value);
                case "System.Byte[]":
                    return new BsonValue((byte[])value);
                default:
                    return new BsonValue(SanitizeString(value.ToString()));
            }
        }

        private string SanitizeString(string value)
        {
            // Remove unwanted double quotes
            return value.Replace("\"", "");
        }
       
        private BsonValue ConvertToBsonValue(object value, Type type)
        {
            // Handle conversion based on type
            if (type == typeof(DateTime))
                return new BsonValue(Convert.ToDateTime(value));
            if (type == typeof(int))
                return new BsonValue(Convert.ToInt32(value));
            if (type == typeof(double))
                return new BsonValue(Convert.ToDouble(value));
            if (type == typeof(bool))
                return new BsonValue(Convert.ToBoolean(value));
            if (type == typeof(string))
                return new BsonValue(value.ToString());

            return new BsonValue(value); // As a fallback
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
                    var f = document.Keys.FirstOrDefault(p => p.Equals(fieldName, StringComparison.InvariantCultureIgnoreCase));
                    if (!string.IsNullOrEmpty(f))
                    {
                        var bsonValue = document[f];
                        if (!bsonValue.IsNull)
                        {
                            try
                            {
                                string netTypeString = field.fieldtype; // Use field.fieldtype directly
                                Type netType = Type.GetType(netTypeString);

                                if (netType == typeof(string) && bsonValue.IsObjectId)
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
                                       // value = bsonValue.ToString();
                                        value= RemoveQuotes(bsonValue.ToString());
                                    }
                                    else
                                    {
                                        value = ConvertBsonValueToNetType(bsonValue, Type.GetType(field.fieldtype));
                                    }
                                    type.GetProperty(field.fieldname).SetValue(instance, value);
                                }
                                else
                                {
                                    // Handle type conversion if necessary
                                    object value;
                                    if (Type.GetType(field.fieldtype) == typeof(string))
                                    {
                                        value = RemoveQuotes(bsonValue.ToString());
                                    }
                                    else
                                    {
                                        value = Convert.ChangeType(ConvertBsonValueToNetType(bsonValue, Type.GetType(field.fieldtype)), Type.GetType(field.fieldtype));
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
        private string RemoveQuotes(string value)
        {
            if (value.StartsWith("\"") && value.EndsWith("\""))
            {
                return value.Substring(1, value.Length - 2);
            }
            return value;
        }
        #endregion

        #region "LocalDB"
        private void InitDataConnection()
        {


            if (DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == DatasourceName).Any())
            {
                Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == DatasourceName).FirstOrDefault();
            }
            else
            {
                ConnectionDriversConfig driversConfig = DMEEditor.ConfigEditor.DataDriversClasses.FirstOrDefault(p => p.DatasourceType == DataSourceType.LiteDB);
                if (driversConfig != null)
                {
                    Dataconnection.ConnectionProp = new ConnectionProperties
                    {
                        ConnectionName = DatasourceName,
                        ConnectionString = driversConfig.ConnectionString,
                        DriverName = driversConfig.PackageName,
                        DriverVersion = driversConfig.version,
                        DatabaseType = DataSourceType.LiteDB,
                        Category = DatasourceCategory.NOSQL
                    };

                }

            }
            if (string.IsNullOrEmpty(_connectionString) && string.IsNullOrEmpty(DBfilepathandname))
            {
                DBfilepathandname = Path.Combine(DMEEditor.ConfigEditor.Config.DataFilePath, $"{DatasourceName}.db");

            }
            if (!string.IsNullOrEmpty(DBfilepathandname))
            {
                Dataconnection.ConnectionProp.ConnectionString = $"{DBfilepathandname}";
                Dataconnection.ConnectionProp.FilePath = Path.GetDirectoryName(DBfilepathandname);
                Dataconnection.ConnectionProp.FileName = Path.GetFileName(DBfilepathandname);

            }

            Dataconnection.ConnectionProp.Category = DatasourceCategory.NOSQL;
            Dataconnection.ConnectionProp.DatabaseType = DataSourceType.LiteDB;
            _connectionString = Dataconnection.ConnectionProp.ConnectionString;

            // HandleConnectionStringforMongoDB();
            Dataconnection.ConnectionProp.IsLocal = true;
        }
        public bool CreateDB()
        {
            DBfilepathandname = string.Empty;
            InitDataConnection();
            return Openconnection() == ConnectionState.Open;
        }

        public bool CreateDB(bool inMemory)
        {
            InitDataConnection();
            Dataconnection.ConnectionProp.ConnectionString = Path.Combine(DMEEditor.ConfigEditor.Config.DataFilePath, $"{DatasourceName}.db");
            _connectionString = Dataconnection.ConnectionProp.ConnectionString;

            return Openconnection() == ConnectionState.Open;
        }

        public bool CreateDB(string filepathandname)
        {
            if(string.IsNullOrEmpty(filepathandname))
            {
                return false;
            }
            DBfilepathandname = filepathandname;
            InitDataConnection();
            return Openconnection() == ConnectionState.Open;
        }

        public bool DeleteDB()
        {
            if(db != null)
            {
                db.Dispose();
                db = null;
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
                if (db == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    using (var db = new LiteDatabase(_connectionString))
                    {
                        // Drop the collection
                        bool dropped = db.DropCollection(EntityName);

                        if (!dropped)
                        {
                            // If DropCollection returns false, the collection did not exist.
                            retval.Flag = Errors.Failed;
                            retval.Message = "Collection does not exist or could not be dropped.";
                            DMEEditor.AddLogMessage("Beep", "Collection does not exist or could not be dropped.", DateTime.Now, -1, null, Errors.Failed);
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
                if (db == null)
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
            //if (string.IsNullOrEmpty(CurrentDatabase))
            //{
            //    var match = Regex.Match(_connectionString, @"\/(?<database>[^\/\?]+)(\?|$)");
            //    if (match.Success)
            //    {
            //        CurrentDatabase = match.Groups["database"].Value;
            //    }
            //}
        }
        #endregion

    }
}
