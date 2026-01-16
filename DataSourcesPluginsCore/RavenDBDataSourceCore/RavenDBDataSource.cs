

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
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

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
            double retval = 0.0;

            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open && Store != null)
                {
                    using (var session = Store.OpenSession(CurrentDatabase))
                    {
                        // Execute RQL query and get scalar result
                        var results = session.Advanced.RawQuery<BlittableJsonReaderObject>(query).ToList();
                        if (results != null && results.Count > 0)
                        {
                            var firstResult = results.First();
                            if (firstResult.TryGet("Value", out object value) || 
                                firstResult.TryGet("Count", out value) ||
                                firstResult.TryGet("Sum", out value))
                            {
                                if (value != null && double.TryParse(value.ToString(), out double doubleValue))
                                {
                                    retval = doubleValue;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Fail", $"Error in executing scalar query ({ex.Message})", DateTime.Now, 0, "", Errors.Failed);
            }

            return retval;
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
            try
            {
                if (Session != null)
                {
                    Session.Dispose();
                    Session = null;
                }
                
                if (Store != null)
                {
                    Store.Dispose();
                    Store = null;
                }
                
                ConnectionStatus = ConnectionState.Closed;
                DMEEditor?.AddLogMessage("Beep", "RavenDB connection closed successfully.", DateTime.Now, -1, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                DMEEditor?.AddLogMessage("Beep", $"Could not close RavenDB {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ConnectionStatus;
        }

        public bool CheckEntityExist(string EntityName)
        {
            bool retval = false;
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open && Store != null)
                {
                    // Check if collection exists
                    var collections = GetCollection();
                    retval = collections != null && collections.Contains(EntityName, StringComparer.OrdinalIgnoreCase);
                }
                else if (Entities != null && Entities.Count > 0)
                {
                    retval = Entities.Any(e => e.EntityName.Equals(EntityName, StringComparison.OrdinalIgnoreCase));
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in CheckEntityExist: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                retval = false;
            }
            return retval;
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            bool retval = false;
            try
            {
                if (entity != null && !string.IsNullOrEmpty(entity.EntityName))
                {
                    // In RavenDB, collections are created automatically when documents are stored
                    // So we just need to ensure the entity structure is saved
                    if (Entities == null)
                        Entities = new List<EntityStructure>();

                    int idx = GetEntityIdx(entity.EntityName);
                    if (idx >= 0)
                    {
                        Entities[idx] = entity;
                    }
                    else
                    {
                        Entities.Add(entity);
                    }

                    SaveStructure();
                    retval = true;
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in CreateEntityAs: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                retval = false;
            }
            return retval;
        }

        public IErrorsInfo ExecuteSql(string sql)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                // RavenDB doesn't use SQL, but supports RQL (Raven Query Language)
                if (ConnectionStatus == ConnectionState.Open && Store != null)
                {
                    using (var session = Store.OpenSession(CurrentDatabase))
                    {
                        // Execute RQL query
                        var results = session.Advanced.RawQuery<BlittableJsonReaderObject>(sql).ToList();
                        DMEEditor?.AddLogMessage("Beep", $"Executed RQL query: {sql}", DateTime.Now, -1, null, Errors.Ok);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in ExecuteSql: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public IEnumerable<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            // RavenDB doesn't have child tables or foreign keys in traditional sense
            return new List<ChildRelation>();
        }

        public DataSet GetChildTablesListFromCustomQuery(string tablename, string customquery)
        {
            DataSet ds = new DataSet();
            try
            {
                // RavenDB doesn't have child tables concept
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in GetChildTablesListFromCustomQuery: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ds;
        }

        public IEnumerable<string> GetEntitesList()
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
            return Task.Run(() =>
            {
                // Parse filter string into AppFilter list if needed
                List<AppFilter> filters = null;
                if (!string.IsNullOrEmpty(filterstr))
                {
                    filters = ParseFilterString(filterstr);
                }
                var result = GetEntity(entityname, filters);
                return (object)result;
            });
        }

        private List<AppFilter> ParseFilterString(string filterstr)
        {
            var filters = new List<AppFilter>();
            try
            {
                if (!string.IsNullOrEmpty(filterstr))
                {
                    var parts = filterstr.Split(new[] { "AND", "OR" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in parts)
                    {
                        var filter = new AppFilter();
                        if (part.Contains("="))
                        {
                            var eqParts = part.Split('=');
                            filter.FieldName = eqParts[0].Trim();
                            filter.FilterValue = eqParts.Length > 1 ? eqParts[1].Trim() : "";
                            filter.Operator = "equals";
                        }
                        if (!string.IsNullOrEmpty(filter.FieldName))
                        {
                            filters.Add(filter);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error parsing filter string: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return filters;
        }

        public IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            List<object> results = new List<object>();
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open && Store != null)
                {
                    using (var session = Store.OpenSession(CurrentDatabase))
                    {
                        // Query documents from the collection (EntityName)
                        IQueryable<BlittableJsonReaderObject> query = session.Query<BlittableJsonReaderObject>(collectionName: EntityName);
                        
                        // Apply filters if provided
                        if (filter != null && filter.Count > 0)
                        {
                            foreach (var appFilter in filter)
                            {
                                if (!string.IsNullOrEmpty(appFilter.FieldName) && !string.IsNullOrEmpty(appFilter.FilterValue))
                                {
                                    // Simple filter implementation - can be enhanced
                                    query = ApplyFilterToQuery(query, appFilter);
                                }
                            }
                        }

                        var documents = query.ToList();
                        foreach (var doc in documents)
                        {
                            results.Add(doc);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in GetEntity: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return results;
        }

        private IQueryable<BlittableJsonReaderObject> ApplyFilterToQuery(IQueryable<BlittableJsonReaderObject> query, AppFilter filter)
        {
            // This is a simplified filter - RavenDB uses its own query syntax
            // In practice, you'd convert AppFilter to RavenDB query expressions
            return query;
        }

        public IEnumerable<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            // RavenDB doesn't have foreign keys in traditional sense
            return new List<RelationShipKeys>();
        }

        public EntityStructure GetEntityStructure(string EntityName, bool refresh = false)
        {
            return GetEntityStructure(EntityName);
        }

        public DataTable GetEntityDataTable(string EntityName, string filterstr)
        {
            DataTable dt = new DataTable();
            try
            {
                List<AppFilter> filters = ParseFilterString(filterstr);
                var results = GetEntity(EntityName, filters);
                
                if (results != null && results.Any())
                {
                    // Convert results to DataTable
                    var entityStructure = GetEntityStructure(EntityName, false);
                    if (entityStructure != null && entityStructure.Fields != null)
                    {
                        foreach (var field in entityStructure.Fields)
                        {
                            dt.Columns.Add(field.FieldName, Type.GetType(field.Fieldtype) ?? typeof(string));
                        }

                        foreach (var item in results)
                        {
                            var row = dt.NewRow();
                            if (item is BlittableJsonReaderObject jsonObj)
                            {
                                foreach (DataColumn col in dt.Columns)
                                {
                                    if (jsonObj.TryGet(col.ColumnName, out object value))
                                    {
                                        row[col.ColumnName] = value ?? DBNull.Value;
                                    }
                                }
                            }
                            dt.Rows.Add(row);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in GetEntityDataTable: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return dt;
        }

        public Type GetEntityType(string EntityName)
        {
            EntityStructure x = GetEntityStructure(EntityName, false);
            DMTypeBuilder.CreateNewObject(DMEEditor, EntityName, EntityName, x.Fields);
            return DMTypeBuilder.MyType;
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (UploadData is IEnumerable<object> dataList)
                {
                    int count = 0;
                    foreach (var item in dataList)
                    {
                        UpdateEntity(EntityName, item);
                        count++;
                        progress?.Report(new PassedArgs { Messege = $"Updated {count} records" });
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in UpdateEntities: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public IEnumerable<object> RunQuery(string qrystr)
        {
            List<object> results = new List<object>();
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open && Store != null)
                {
                    using (var session = Store.OpenSession(CurrentDatabase))
                    {
                        var queryResults = session.Advanced.RawQuery<BlittableJsonReaderObject>(qrystr).ToList();
                        results.AddRange(queryResults);
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in RunQuery: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return results;
        }
        public virtual IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open && Store != null)
                {
                    using (var session = Store.OpenSession(CurrentDatabase))
                    {
                        session.Store(UploadDataRow);
                        session.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in UpdateEntity: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public IErrorsInfo DeleteEntity(string EntityName, object DeletedDataRow)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open && Store != null && DeletedDataRow != null)
                {
                    using (var session = Store.OpenSession(CurrentDatabase))
                    {
                        // Get document ID
                        string documentId = null;
                        if (DeletedDataRow is BlittableJsonReaderObject jsonObj)
                        {
                            if (jsonObj.TryGet("@metadata", out BlittableJsonReaderObject metadata))
                            {
                                metadata.TryGet("@id", out documentId);
                            }
                        }
                        else if (DeletedDataRow.GetType().GetProperty("Id") != null)
                        {
                            documentId = DeletedDataRow.GetType().GetProperty("Id")?.GetValue(DeletedDataRow)?.ToString();
                        }

                        if (!string.IsNullOrEmpty(documentId))
                        {
                            session.Delete(documentId);
                            session.SaveChanges();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in DeleteEntity: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            if (fnd != null && !string.IsNullOrEmpty(fnd.EntityName))
            {
                return GetEntityStructure(fnd.EntityName, refresh);
            }
            return null;
        }

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (dDLScripts != null && !string.IsNullOrEmpty(dDLScripts.Ddl))
                {
                    ExecuteSql(dDLScripts.Ddl);
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
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (entities != null && entities.Count > 0)
                {
                    foreach (var entity in entities)
                    {
                        CreateEntityAs(entity);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in CreateEntities: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ErrorObject;
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
                            SourceDataSourceEntityName = entity.EntityName,
                           ScriptType= DDLScriptType.CreateEntity,
                            Ddl = $"# RavenDB collection: {entity.EntityName}\n# Collections are created automatically when documents are stored"
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
        public Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
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
            EntityStructure retval = null;
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open && Store != null)
                {
                    using (var session = Store.OpenSession(CurrentDatabase))
                    {
                        // Get a sample document from the collection
                        var sampleDoc = session.Query<BlittableJsonReaderObject>(collectionName: DocName).FirstOrDefault();
                        
                        if (sampleDoc != null)
                        {
                            retval = new EntityStructure
                            {
                                EntityName = DocName,
                                DatasourceEntityName = DocName,
                                OriginalEntityName = DocName,
                                Caption = DocName,
                                Category = DatasourceCategory.NOSQL.ToString(),
                                DatabaseType = DataSourceType.RavenDB,
                                DataSourceID = DatasourceName,
                                SchemaOrOwnerOrDatabase = CurrentDatabase,
                                Fields = new List<EntityField>()
                            };

                            // Extract fields from sample document
                            int fieldIndex = 0;
                            foreach (var propertyName in sampleDoc.GetPropertyNames())
                            {
                                if (propertyName != "@metadata")
                                {
                                    sampleDoc.TryGet(propertyName, out object propValue);
                                    var fieldType = InferTypeFromValue(propValue);

                                    retval.Fields.Add(new EntityField
                                    {
                                        FieldName = propertyName,
                                        Originalfieldname = propertyName,
                                        Fieldtype = fieldType,
                                        ValueRetrievedFromParent = false,
                                        EntityName = DocName,
                                        FieldIndex = fieldIndex++,
                                        IsKey = propertyName == "Id" || propertyName == "id",
                                        AllowDBNull = true
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage(ex.Message, "Could not Create Entity structure for RavenDB Entity " + DocName, DateTime.Now, -1, Dataconnection.ConnectionProp?.Url ?? "", Errors.Failed);
                retval = null;
            }
            return retval;
        }

        private string InferTypeFromValue(object value)
        {
            if (value == null)
                return "System.String";

            var type = value.GetType();
            if (type == typeof(int))
                return "System.Int32";
            if (type == typeof(long))
                return "System.Int64";
            if (type == typeof(double))
                return "System.Double";
            if (type == typeof(decimal))
                return "System.Decimal";
            if (type == typeof(bool))
                return "System.Boolean";
            if (type == typeof(DateTime))
                return "System.DateTime";
            if (type == typeof(string))
                return "System.String";

            return "System.Object";
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
                    try
                    {
                        Closeconnection();
                    }
                    catch (Exception ex)
                    {
                        DMEEditor?.AddLogMessage("Beep", $"Error disposing RavenDB connection: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                    }
                }
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
            return Dataconnection?.ConnectionProp?.ConnectionString ?? Dataconnection?.ConnectionProp?.Url ?? "";
        }

        public IErrorsInfo SaveStructure()
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (Entities != null && Entities.Count > 0)
                {
                    InMemoryStructures = Entities;
                    DMEEditor?.ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities 
                    { 
                        datasourcename = DatasourceName, 
                        Entities = Entities 
                    });
                    IsSaved = true;
                    OnSaveStructure?.Invoke(this, (PassedArgs)DMEEditor?.Passedarguments);
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Could not save RavenDB Structure for {DatasourceName}- {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public IErrorsInfo LoadStructure()
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                var entitiesData = DMEEditor?.ConfigEditor.LoadDataSourceEntitiesValues(DatasourceName);
                if (entitiesData != null && entitiesData.Entities != null)
                {
                    Entities = entitiesData.Entities;
                    EntitiesNames = Entities.Select(e => e.EntityName).ToList();
                    InMemoryStructures = Entities;
                    OnLoadStructure?.Invoke(this, (PassedArgs)DMEEditor?.Passedarguments);
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Could not Load RavenDB Structure for {DatasourceName}- {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public IErrorsInfo LoadData(Progress<PassedArgs> progress, CancellationToken token)
        {
            return LoadData((IProgress<PassedArgs>)progress, token);
        }

        public IErrorsInfo SyncData(Progress<PassedArgs> progress, CancellationToken token)
        {
            return SyncData((IProgress<PassedArgs>)progress, token);
        }

        public IErrorsInfo LoadStructure(Progress<PassedArgs> progress, CancellationToken token, bool copydata = false)
        {
            return LoadStructure((IProgress<PassedArgs>)progress, token, copydata);
        }

        public IErrorsInfo CreateStructure(Progress<PassedArgs> progress, CancellationToken token)
        {
            return CreateStructure((IProgress<PassedArgs>)progress, token);
        }

        public IErrorsInfo SyncData(string entityname, Progress<PassedArgs> progress, CancellationToken token)
        {
            return SyncData(entityname, (IProgress<PassedArgs>)progress, token);
        }

        public IErrorsInfo RefreshData(Progress<PassedArgs> progress, CancellationToken token)
        {
            return RefreshData((IProgress<PassedArgs>)progress, token);
        }

        public IErrorsInfo RefreshData(string entityname, Progress<PassedArgs> progress, CancellationToken token)
        {
            return RefreshData(entityname, (IProgress<PassedArgs>)progress, token);
        }

        public PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            PagedResult pagedResult = new PagedResult();
            ErrorObject.Flag = Errors.Ok;

            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open && Store != null)
                {
                    using (var session = Store.OpenSession(CurrentDatabase))
                    {
                        IQueryable<BlittableJsonReaderObject> query = session.Query<BlittableJsonReaderObject>(collectionName: EntityName);
                        
                        if (filter != null && filter.Count > 0)
                        {
                            foreach (var appFilter in filter)
                            {
                                query = ApplyFilterToQuery(query, appFilter);
                            }
                        }

                        // Get total count
                        var totalCount = query.Count();

                        // Get paginated results
                        int skipAmount = (pageNumber - 1) * pageSize;
                        var documents = query.Skip(skipAmount).Take(pageSize).ToList();

                        pagedResult.Data = documents;
                        pagedResult.TotalRecords = totalCount;
                        pagedResult.PageNumber = pageNumber;
                        pagedResult.PageSize = pageSize;
                        pagedResult.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                        pagedResult.HasNextPage = pageNumber < pagedResult.TotalPages;
                        pagedResult.HasPreviousPage = pageNumber > 1;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in GetEntity (paged): {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return pagedResult;
        }

        public IErrorsInfo LoadStructure(IProgress<PassedArgs> progress, CancellationToken token, bool copydata = false)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                ConnectionStatus = ConnectionState.Open;
                Entities = new List<EntityStructure>();
                EntitiesNames = new List<string>();

                var entitiesData = DMEEditor?.ConfigEditor.LoadDataSourceEntitiesValues(DatasourceName);
                if (entitiesData != null)
                {
                    Entities = entitiesData.Entities ?? new List<EntityStructure>();
                    EntitiesNames = Entities.Select(e => e.EntityName).ToList();
                    InMemoryStructures = Entities;
                }
                else
                {
                    GetEntitesList();
                }

                SaveStructure();
                OnLoadStructure?.Invoke(this, (PassedArgs)DMEEditor?.Passedarguments);
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Could not Load RavenDB Structure for {DatasourceName}- {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public IErrorsInfo CreateStructure(IProgress<PassedArgs> progress, CancellationToken token)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (!IsStructureCreated)
                {
                    GetEntitesList();
                    SaveStructure();
                    IsStructureCreated = true;
                    OnCreateStructure?.Invoke(this, (PassedArgs)DMEEditor?.Passedarguments);
                }
            }
            catch (Exception ex)
            {
                IsStructureCreated = false;
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Could not create RavenDB Structure for {DatasourceName}- {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public IErrorsInfo LoadData(IProgress<PassedArgs> progress, CancellationToken token)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (IsStructureCreated)
                {
                    // RavenDB data is already available through GetEntity
                    IsLoaded = true;
                    OnLoadData?.Invoke(this, (PassedArgs)DMEEditor?.Passedarguments);
                }
            }
            catch (Exception ex)
            {
                IsLoaded = false;
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Could not Load RavenDB data for {DatasourceName}- {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public IErrorsInfo SyncData(IProgress<PassedArgs> progress, CancellationToken token)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                GetEntitesList();
                SaveStructure();
                IsSynced = true;
                OnSyncData?.Invoke(this, (PassedArgs)DMEEditor?.Passedarguments);
            }
            catch (Exception ex)
            {
                IsSynced = false;
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Could not Sync RavenDB data for {DatasourceName}- {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public IErrorsInfo SyncData(string entityname, IProgress<PassedArgs> progress, CancellationToken token)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                GetEntityStructure(entityname, true);
                SaveStructure();
                OnRefreshDataEntity?.Invoke(this, (PassedArgs)DMEEditor?.Passedarguments);
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Could not Sync RavenDB entity {entityname}- {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public IErrorsInfo RefreshData(IProgress<PassedArgs> progress, CancellationToken token)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                GetEntitesList();
                SaveStructure();
                OnRefreshData?.Invoke(this, (PassedArgs)DMEEditor?.Passedarguments);
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Could not Refresh RavenDB data for {DatasourceName}- {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public IErrorsInfo RefreshData(string entityname, IProgress<PassedArgs> progress, CancellationToken token)
        {
            return SyncData(entityname, progress, token);
        }


        #endregion
    }
}
