using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using TheTechIdea;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.WebAPI;
using DataManagementModels.DriversConfigurations;
using DataManagementModels.Editor;
using System.Text.RegularExpressions;
using Supabase.Storage;
using System.Reflection;

using TheTechIdea.Beep.Helpers;
using static Supabase.Postgrest.Constants;
using System.ComponentModel;


namespace SupabaseDataSourceCore
{
    [AddinAttribute(Category = DatasourceCategory.WEBAPI, DatasourceType = DataSourceType.Supabase)]
    public class SupabaseDataSource : IDataSource
    {
        private bool disposedValue;
        public string CurrentDatabase { get { return Dataconnection.ConnectionProp.Database; } set { Dataconnection.ConnectionProp.Database = value; } }
        public string ColumnDelimiter { get  ; set  ; }
        public string ParameterDelimiter { get  ; set  ; }
        public string GuidID { get  ; set  ; }
        public DataSourceType DatasourceType { get  ; set  ; }= DataSourceType.Supabase;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.WEBAPI;
        public IDataConnection Dataconnection { get  ; set  ; }
        public string DatasourceName { get  ; set  ; }
        public IErrorsInfo ErrorObject { get  ; set  ; }
        public string Id { get  ; set  ; }
        public IDMLogger Logger { get  ; set  ; }
        public List<string> EntitiesNames { get  ; set  ; }=new List<string>();
        public List<EntityStructure> Entities { get  ; set  ; }=new List<EntityStructure>();    
        public IDMEEditor DMEEditor { get  ; set  ; }
        public ConnectionState ConnectionStatus { get  ; set  ; }

        public event EventHandler<PassedArgs> PassEvent;

        string _connectionString;
        Supabase.Client client;
        Supabase.SupabaseOptions options;
        public List<Bucket> Buckets { get; set; }=new List<Bucket>();
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
        public SupabaseDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = databasetype;
            Category = DatasourceCategory.WEBAPI;

            Dataconnection = new WebAPIDataConnection
            {
                Logger = logger,
                ErrorObject = ErrorObject

            };
            if (DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == datasourcename).Any())
            {
                Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == datasourcename).FirstOrDefault();
                GuidID = Dataconnection.ConnectionProp.GuidID;
            }
            else
            {
                ConnectionDriversConfig driversConfig = DMEEditor.ConfigEditor.DataDriversClasses.FirstOrDefault(p => p.DatasourceType == databasetype);
                Dataconnection.ConnectionProp = new ConnectionProperties
                {
                    ConnectionName = datasourcename,
                    ConnectionString = driversConfig.ConnectionString,
                    DriverName = driversConfig.PackageName,
                    DriverVersion = driversConfig.version,
                    DatabaseType = DataSourceType.Supabase,
                    Category = DatasourceCategory.WEBAPI
                };
                GuidID = Guid.NewGuid().ToString();
            }

            Dataconnection.ConnectionProp.Category = DatasourceCategory.WEBAPI;
            Dataconnection.ConnectionProp.DatabaseType = DataSourceType.Supabase;
            _connectionString = Dataconnection.ConnectionProp.ConnectionString;
            CurrentDatabase = Dataconnection.ConnectionProp.Database;
           // Settings = new MongoClientSettings();
            //if (CurrentDatabase != null)
            //{
            //    if (CurrentDatabase.Length > 0)
            //    {
            //        _client = new MongoClient(_connectionString);
            //        GetEntitesList();
            //    }
            //}

        }


        #region "Data Manipulation"
        public IErrorsInfo BeginTransaction(PassedArgs args)
        {
            throw new NotImplementedException();
        }
        public IErrorsInfo Commit(PassedArgs args)
        {
            throw new NotImplementedException();
        }
        public IErrorsInfo EndTransaction(PassedArgs args)
        {
            throw new NotImplementedException();
        }
        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            throw new NotImplementedException();
        }
        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            ErrorsInfo retval = new ErrorsInfo();
            retval.Flag = Errors.Ok;
            retval.Message = "Data inserted successfully.";

            try
            {
                // Ensure the connection is open
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    // Get the entity structure for the given entity name
                    var entityStructure = GetEntityStructure(EntityName, false);
                    if (entityStructure == null)
                    {
                        retval.Flag = Errors.Failed;
                        retval.Message = $"Entity structure for {EntityName} not found.";
                        return retval;
                    }

                    // Create the dynamic type from the entity structure
                    Type dynamicType = DMTypeBuilder.CreateTypeFromCode(DMEEditor, DMTypeBuilder.ConvertPOCOClassToEntity(DMEEditor, entityStructure, "SupabaseGeneratedTypes"), entityStructure.EntityName);

                    // Create an instance of the dynamic type and populate its properties with InsertedData
                    var dynamicInstance = Activator.CreateInstance(dynamicType);
                    foreach (var field in entityStructure.Fields)
                    {
                        var property = dynamicType.GetProperty(field.fieldname);
                        if (property != null)
                        {
                            var value = InsertedData.GetType().GetProperty(field.fieldname)?.GetValue(InsertedData);
                            property.SetValue(dynamicInstance, value);
                        }
                    }

                    // Use reflection to call the generic method
                    var fromMethod = client.GetType().GetMethod("From").MakeGenericMethod(dynamicType);
                    var table = fromMethod.Invoke(client, new object[] { EntityName });
                    var insertMethod = table.GetType().GetMethod("Insert");

                    var responseTask = (Task)insertMethod.Invoke(table, new object[] { dynamicInstance });
                    responseTask.Wait();

                    var response = responseTask.GetType().GetProperty("Result").GetValue(responseTask) as Supabase.Postgrest.Responses.BaseResponse;
                    if (response.ResponseMessage.StatusCode != System.Net.HttpStatusCode.Created)
                    {
                        retval.Flag = Errors.Failed;
                        retval.Message = $"Error inserting data: {response.ResponseMessage.ReasonPhrase}";
                        DMEEditor.AddLogMessage("Beep", $"Error inserting data: {response.ResponseMessage.ReasonPhrase}", DateTime.Now, -1, null, Errors.Failed);
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
                retval.Message = $"Error in {methodName}: {ex.Message}";
                DMEEditor.AddLogMessage("Beep", $"Error in {methodName} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return retval;
        }



        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            throw new NotImplementedException();
        }
        #endregion "Data Manipulation"
        #region "Data Definition"
        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            throw new NotImplementedException();
        }
        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            try
            {
                foreach (var item in entities)
                {
                    CreateEntityAs(item);
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error in CreateEntities: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return new ErrorsInfo { Flag = Errors.Ok, Message = "Executed Successfully" };
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            ErrorsInfo erretval = new ErrorsInfo();
            erretval.Flag = Errors.Ok;
            erretval.Message = "Executed Successfully"; ;
            bool retval = false;

            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    // Check if the table already exists
                    var existingTables = GetTablesFromSupabase();
                    if (!existingTables.Contains(entity.EntityName))
                    {
                        // Build the create table SQL command
                        StringBuilder createTableCommand = new StringBuilder();
                        createTableCommand.AppendLine($"CREATE TABLE {entity.EntityName} (");

                        foreach (var field in entity.Fields)
                        {
                            string supabaseType = GetSupabaseDataType(field.fieldtype);
                            createTableCommand.AppendLine($"{field.fieldname} {supabaseType},");
                        }

                        // Remove the last comma and add closing parenthesis
                        createTableCommand.Length--; // Remove last comma
                        createTableCommand.AppendLine(");");

                        // Execute the command
                        var response = client.Rpc("sql", new Dictionary<string, object>
                {
                    { "query", createTableCommand.ToString() }
                }).Result;

                        if (response.ResponseMessage.IsSuccessStatusCode)
                        {
                            // Add the entity to Entities and EntitiesNames
                            if (Entities == null)
                            {
                                Entities = new List<EntityStructure>();
                            }
                            if (Entities.Count > 0 && !Entities.Any(p => p.EntityName == entity.EntityName))
                            {
                                Entities.Add(entity);
                            }
                            else
                            {
                                Entities.Add(entity);
                            }

                            if (EntitiesNames == null)
                            {
                                EntitiesNames = new List<string>();
                            }
                            if (EntitiesNames.Count == 0)
                            {
                                EntitiesNames.Add(entity.EntityName);
                            }
                            if (EntitiesNames.Count > 0 && !EntitiesNames.Contains(entity.EntityName))
                            {
                                EntitiesNames.Add(entity.EntityName);
                            }

                            DMEEditor.AddLogMessage("Beep", "Table created successfully.", DateTime.Now, -1, null, Errors.Ok);
                            retval = true;
                        }
                        else
                        {
                            DMEEditor.AddLogMessage("Beep", $"Error creating table: {response.ResponseMessage.ReasonPhrase}", DateTime.Now, -1, null, Errors.Failed);
                        }
                    }
                    else
                    {
                        DMEEditor.AddLogMessage("Beep", "Table already exists.", DateTime.Now, -1, null, Errors.Failed);
                    }
                }
                else
                {
                    erretval.Flag = Errors.Failed;
                    erretval.Message = "Could not open connection";
                }
            }
            catch (Exception ex)
            {
                string methodName = MethodBase.GetCurrentMethod().Name;
                retval = false;
                DMEEditor.AddLogMessage("Beep", $"Error in {methodName} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return retval;
        }

        #endregion "Data Definition"
        #region "Data Retrieval"
        public object RunQuery(string qrystr)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok, Message = "Executed Successfully " };
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    var response = client.Rpc("exec_sql", new Dictionary<string, object> { { "sql", qrystr } }).Result;

                    if (response.ResponseMessage.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var json = response.Content.ToString();
                        var result = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
                        return result;
                    }
                    else
                    {
                        retval.Flag = Errors.Failed;
                        retval.Message = $"Error executing query: {response.ResponseMessage.ReasonPhrase}";
                        DMEEditor.AddLogMessage("Beep", retval.Message, DateTime.Now, -1, null, Errors.Failed);
                    }
                }
                else
                {
                    retval.Flag = Errors.Failed;
                    retval.Message = "Could not open connection";
                    DMEEditor.AddLogMessage("Beep", retval.Message, DateTime.Now, -1, null, Errors.Failed);
                }
            }
            catch (Exception ex)
            {
                string methodName = MethodBase.GetCurrentMethod().Name;
                retval.Flag = Errors.Failed;
                retval.Message = $"Error in {methodName} in {DatasourceName} - {ex.Message}";
                DMEEditor.AddLogMessage("Beep", retval.Message, DateTime.Now, -1, null, Errors.Failed);
            }

            return retval;
        }

        public IErrorsInfo ExecuteSql(string sql)
        {
            ErrorsInfo retval = new ErrorsInfo();
            retval.Flag = Errors.Ok;
            retval.Message = "Executed Successfully"; ;
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    var response = client.Rpc("execute_sql", new Dictionary<string, string> { { "sql", sql } }).Result;
                    if (response.ResponseMessage.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        retval.Flag = Errors.Ok;
                        retval.Message = "SQL executed successfully.";
                        DMEEditor.AddLogMessage("Beep", "SQL executed successfully.", DateTime.Now, -1, null, Errors.Ok);
                    }
                    else
                    {
                        retval.Flag = Errors.Failed;
                        retval.Message = $"Error executing SQL: {response.ResponseMessage.ReasonPhrase}";
                        DMEEditor.AddLogMessage("Beep", $"Error executing SQL: {response.ResponseMessage.ReasonPhrase}", DateTime.Now, -1, null, Errors.Failed);
                    }
                }
                else
                {
                    retval.Flag = Errors.Failed;
                    retval.Message = "Could not open connection";
                    DMEEditor.AddLogMessage("Beep", "Could not open connection", DateTime.Now, -1, null, Errors.Failed);
                }
            }
            catch (Exception ex)
            {
                string methodName = MethodBase.GetCurrentMethod().Name;
                retval.Flag = Errors.Failed;
                retval.Message = ex.Message;
                DMEEditor.AddLogMessage("Beep", $"Error in {methodName} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return retval;
        }


        public List<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            List<ChildRelation> childRelations = new List<ChildRelation>();

            try
            {
                // Ensure the connection is open
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                // Query to get foreign key relationships
                string query = $@"
            SELECT 
                tc.constraint_name, 
                kcu.column_name AS child_column, 
                ccu.table_name AS parent_table,
                ccu.column_name AS parent_column
            FROM 
                information_schema.table_constraints AS tc 
                JOIN information_schema.key_column_usage AS kcu
                  ON tc.constraint_name = kcu.constraint_name
                JOIN information_schema.constraint_column_usage AS ccu
                  ON ccu.constraint_name = tc.constraint_name
            WHERE 
                tc.constraint_type = 'FOREIGN KEY' AND 
                kcu.table_name = '{tablename}'";

                var responseTask = client.Rpc("sql", new { query = query });
                responseTask.Wait();

                var response = responseTask.Result;
                if (response.ResponseMessage.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var json = response.Content;
                    var relations = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);

                    foreach (var relation in relations)
                    {
                        ChildRelation childRelation = new ChildRelation
                        {
                            child_table = tablename,
                            child_column = relation["child_column"].ToString(),
                            parent_table = relation["parent_table"].ToString(),
                            parent_column = relation["parent_column"].ToString(),
                            Constraint_Name = relation["constraint_name"].ToString(),
                            RalationName = $"{tablename}_{relation["parent_table"]}_{relation["constraint_name"]}"
                        };

                        childRelations.Add(childRelation);
                    }
                }
                else
                {
                    DMEEditor.AddLogMessage("Beep", $"Error retrieving child tables for {tablename}: {response.ResponseMessage.ReasonPhrase}", DateTime.Now, -1, null, Errors.Failed);
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error retrieving child tables for {tablename}: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return childRelations;
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
                    var response = client.Rpc("pg_tables", new Dictionary<string, string>()).Result;
                    
                    if (response.ResponseMessage.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var json = response.Content.ToString();
                        var tables = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
                        EntitiesNames = tables.Select(table => table["tablename"].ToString()).ToList();
                    }
                    else
                    {
                        DMEEditor.AddLogMessage("Beep", $"Error in getting entities {response.ResponseMessage.ReasonPhrase}", DateTime.Now, -1, null, Errors.Failed);
                        return EntitiesNames;
                    }
                    
                    // Synchronize the Entities list to match the current collection names
                    if (Entities != null)
                    {
                        var entitiesToRemove = Entities.Where(e => !EntitiesNames.Contains(e.EntityName)).ToList();
                        foreach (var item in entitiesToRemove)
                        {
                            Entities.Remove(item);
                        }
                        var entitiesToAdd = EntitiesNames.Where(e => !Entities.Any(x => x.EntityName == e)).ToList();
                        foreach (var item in entitiesToAdd)
                        {
                            GetEntityStructure(item, true);
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
            try
            {
                // Ensure the connection is open
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                // Retrieve the entity structure
                EntityStructure entityStructure = GetEntityStructure(EntityName, refresh: false);

                // Generate the dynamic type from the entity structure
                string classNamespace = "TheTechIdea.Classes";
                string code = DMTypeBuilder.ConvertPOCOClassToEntity(DMEEditor, entityStructure, classNamespace);
                Type dynamicType = DMTypeBuilder.CreateTypeFromCode(DMEEditor, code, $"{EntityName}");

                // Fetch data from Supabase
                // Use reflection to call the generic From<T> method
                var method = client.GetType().GetMethod("From").MakeGenericMethod(dynamicType);
                var table = method.Invoke(client, new object[] { EntityName });
                var getMethod = table.GetType().GetMethod("Get");
                var responseTask = (Task)getMethod.Invoke(table, null);
                responseTask.Wait();

                var response = ((dynamic)responseTask).Result;

                if (response.ResponseMessage.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var json = response.Content.ToString();
                    var records = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);

                    // Convert the records to the dynamically generated type
                    var dynamicRecords = ConvertToDynamicType(records, dynamicType, entityStructure);
                    return dynamicRecords;
                }
                else
                {
                    DMEEditor.AddLogMessage("Beep", $"Error retrieving data for {EntityName}: {response.ResponseMessage.ReasonPhrase}", DateTime.Now, -1, null, Errors.Failed);
                    return null;
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error in GetEntity for {EntityName}: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return null;
            }
        }



        public object GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            try
            {
                // Ensure the connection is open
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                // Retrieve the entity structure
                EntityStructure entityStructure = GetEntityStructure(EntityName, refresh: false);

                // Generate the dynamic type from the entity structure
                string classNamespace = "TheTechIdea.Classes";
                string code = DMTypeBuilder.ConvertPOCOClassToEntity(DMEEditor, entityStructure, classNamespace);
                Type dynamicType = DMTypeBuilder.CreateTypeFromCode(DMEEditor, code, $"{classNamespace}.{EntityName}");

                // Use reflection to call the generic From<T> method
                var method = client.GetType().GetMethod("From").MakeGenericMethod(dynamicType);
                var table = method.Invoke(client, new object[] { EntityName });

                // Apply filters and pagination
                var query = table.GetType().GetMethod("Select").Invoke(table, new object[] { "*" });

                if (filter != null && filter.Count > 0)
                {
                    foreach (var f in filter)
                    {
                        var filterString = $"{f.FieldName} {f.Operator} {f.FilterValue}";
                        query = query.GetType().GetMethod("Filter").Invoke(query, new object[] { filterString });
                    }
                }

                query = query.GetType().GetMethod("Range").Invoke(query, new object[] { (pageNumber - 1) * pageSize, pageNumber * pageSize - 1 });
                var responseTask = (Task)query.GetType().GetMethod("Get").Invoke(query, null);
                responseTask.Wait();

                var response = ((dynamic)responseTask).Result;

                if (response.ResponseMessage.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var json = response.Content.ReadAsStringAsync().Result;
                    var records = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);

                    // Convert the records to the dynamically generated type
                    var dynamicRecords = ConvertToDynamicType(records, dynamicType, entityStructure);
                    return dynamicRecords;
                }
                else
                {
                    DMEEditor.AddLogMessage("Beep", $"Error retrieving data for {EntityName}: {response.ResponseMessage.ReasonPhrase}", DateTime.Now, -1, null, Errors.Failed);
                    return null;
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error in GetEntity for {EntityName}: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return null;
            }
        }

        public Task<object> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
           return Task.Run(() => GetEntity(EntityName, Filter));
        }

        public List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            List<RelationShipKeys> foreignKeys = new List<RelationShipKeys>();

            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    var response = client.Rpc("pg_get_foreign_keys", new Dictionary<string, object> { { "tablename", entityname } }).Result;

                    if (response.ResponseMessage.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var json = response.Content.ToString();
                        var keys = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);

                        foreach (var key in keys)
                        {
                            RelationShipKeys relationshipKey = new RelationShipKeys
                            {
                                RalationName = key["constraint_name"].ToString(),
                                RelatedEntityID = key["foreign_table_name"].ToString(),
                                RelatedEntityColumnID = key["foreign_column_name"].ToString(),
                                EntityColumnID = key["column_name"].ToString(),
                                EntityColumnSequenceID = int.Parse(key["ordinal_position"].ToString()),
                                RelatedColumnSequenceID = int.Parse(key["foreign_column_position"].ToString())
                            };

                            foreignKeys.Add(relationshipKey);
                        }
                    }
                    else
                    {
                        DMEEditor.AddLogMessage("Beep", $"Error retrieving foreign keys for {entityname}: {response.ResponseMessage.ReasonPhrase}", DateTime.Now, -1, null, Errors.Failed);
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error retrieving foreign keys for {entityname}: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return foreignKeys;
        }


        public int GetEntityIdx(string entityName)
        {
            try
            {
                if (Entities == null || Entities.Count == 0)
                {
                    GetEntitesList(); // Ensure the Entities list is populated
                }

                for (int i = 0; i < Entities.Count; i++)
                {
                    if (Entities[i].EntityName.Equals(entityName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return i;
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error in GetEntityIdx for {entityName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return -1; // Return -1 if entity is not found
        }


        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
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
                  
                        if (DataStruct != null)
                        {
                            if (DataStruct.EntityName == null || DataStruct.EntityName != EntityName)
                            {
                                DataStruct = GetEntityFromSupabase( EntityName);
                            }
                        }
                        else
                            DataStruct = GetEntityFromSupabase( EntityName);
                        ObjectsCreated = true;
                        // Optionally convert BSON documents to a specific object type if needed
                        // Assuming you have a method to determine the type from entityName
                        //  Type entityType = GetEntityType(EntityName);
                        enttype = GetEntityType(EntityName);
                        // result = GetEntityStructureFromBson(firstDocument, EntityName);
                        if (Entities == null)
                        {
                            Entities = new List<EntityStructure>();
                        }
                        if (EntitiesNames == null)
                        {
                            EntitiesNames = new List<string>();
                        }
                        if (Entities.Count > 0 && !Entities.Any(p => p.EntityName == EntityName))
                        {
                            Entities.Add(DataStruct);
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
                        DataStruct = GetEntityStructure(EntityName,false);
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

        public double GetScalar(string query)
        {
            double result = 0;

            try
            {
                // Ensure the connection is open
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                // Execute the query and get the result
                var responseTask = client.Rpc("sql", new { query = query });
                responseTask.Wait();

                var response = responseTask.Result;
                if (response.ResponseMessage.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var json = response.Content;
                    var scalarResult = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

                    if (scalarResult != null && scalarResult.ContainsKey("value"))
                    {
                        result = Convert.ToDouble(scalarResult["value"]);
                    }
                }
                else
                {
                    DMEEditor.AddLogMessage("Beep", $"Error executing scalar query: {response.ResponseMessage.ReasonPhrase}", DateTime.Now, -1, null, Errors.Failed);
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error in GetScalar: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return result;
        }

        public async Task<double> GetScalarAsync(string query)
        {
            double result = 0;

            try
            {
                // Ensure the connection is open
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                // Execute the query and get the result
                var response = await client.Rpc("sql", new { query = query });

                if (response.ResponseMessage.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var json =  response.Content;
                    var scalarResult = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

                    if (scalarResult != null && scalarResult.ContainsKey("value"))
                    {
                        result = Convert.ToDouble(scalarResult["value"]);
                    }
                }
                else
                {
                    DMEEditor.AddLogMessage("Beep", $"Error executing scalar query: {response.ResponseMessage.ReasonPhrase}", DateTime.Now, -1, null, Errors.Failed);
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error in GetScalarAsync: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return result;
        }


        public bool CheckEntityExist(string EntityName)
        {
            ErrorsInfo erretval = new ErrorsInfo();
            erretval.Flag = Errors.Ok;
            erretval.Message = "Executed Successfully"; ;
            bool retval = false;

            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    // Check if the table already exists
                    var existingTables = GetTablesFromSupabase();
                    if (existingTables.Contains(EntityName))
                    {
                        retval = true;
                    }
                    else
                    {
                        retval = false;
                        DMEEditor.AddLogMessage("Beep", "Table does not exist.", DateTime.Now, -1, null, Errors.Failed);
                    }
                }
                else
                {
                    erretval.Flag = Errors.Failed;
                    erretval.Message = "Could not open connection";
                }
            }
            catch (Exception ex)
            {
                string methodName = MethodBase.GetCurrentMethod().Name;
                retval = false;
                DMEEditor.AddLogMessage("Beep", $"Error in {methodName} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return retval;
        }

        #endregion "Data Retrieval"
        #region "Connection Management"
        public void HandleConnectionString()
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
        public  ConnectionState Openconnection()
        {
            try
            {
                options = new Supabase.SupabaseOptions
                {
                    AutoConnectRealtime = true
                };
                client = new Supabase.Client(Dataconnection.ConnectionProp.Url, Dataconnection.ConnectionProp.KeyToken, options);
                var r = Task.Run(() => client.InitializeAsync());
                r.Wait();
                ConnectionStatus = ConnectionState.Open;
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Closed;
                DMEEditor.AddLogMessage("Error", $"Error in opening connection {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
            }
           
            return ConnectionStatus;
        }
        public ConnectionState Closeconnection()
        {
            throw new NotImplementedException();
        }
        #endregion "Connection Management"
        #region "Supporting Methods From Supabase"
        private EntityStructure GetEntityFromSupabase(string EntityName)
        {
            EntityStructure entityStructure = new EntityStructure
            {
                EntityName = EntityName,
                Fields = new List<EntityField>()
            };

            try
            {
                var response = client.Rpc($"pg_table_def", new Dictionary<string, string> { { "tablename", EntityName } }).Result;
                if (response.ResponseMessage.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var json = response.Content.ToString();
                    var fields = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);

                    foreach (var field in fields)
                    {
                        EntityField entityField = new EntityField
                        {
                            fieldname = field["column_name"].ToString(),
                            fieldtype = field["data_type"].ToString()
                        };
                        entityStructure.Fields.Add(entityField);
                    }
                }
                else
                {
                    DMEEditor.AddLogMessage("Beep", $"Error retrieving entity structure for {EntityName}: {response.ResponseMessage.ReasonPhrase}", DateTime.Now, -1, null, Errors.Failed);

                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error retrieving entity structure for {EntityName}: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return entityStructure;
        }
        private List<string> GetTablesFromSupabase()
        {
            List<string> tables = new List<string>();
            try
            {
                var parameters = new Dictionary<string, object>();
                var response = client.Rpc("pg_tables", parameters).Result;
                if (response.ResponseMessage.IsSuccessStatusCode)
                {
                    var json = response.Content;
                    var tableList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);

                    foreach (var table in tableList)
                    {
                        tables.Add(table["tablename"].ToString());
                    }
                }
                else
                {
                    DMEEditor.AddLogMessage("Beep", $"Error retrieving tables: {response.ResponseMessage.ReasonPhrase}", DateTime.Now, -1, null, Errors.Failed);
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error retrieving tables: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return tables;
        }

        public void GetBuckets()
        {
            throw new NotImplementedException();
        }
        public void GetBucket(string bucketname)
        {
            throw new NotImplementedException();
        }
        public void GetBucketFiles(string bucketname)
        {
            throw new NotImplementedException();
        }
        public void GetBucketFile(string bucketname, string filename)
        {
            throw new NotImplementedException();
        }
        public void GetBucketFile(string bucketname, string filename, string path)
        {
            throw new NotImplementedException();
        }
        public void GetBucketFile(string bucketname, string filename, string path, string filename2)
        {
            throw new NotImplementedException();
        }
        public void GetBucketFile(string bucketname, string filename, string path, string filename2, string path2)
        {
            throw new NotImplementedException();
        }
        public void GetBucketFile(string bucketname, string filename, string path, string filename2, string path2, string filename3)
        {
            throw new NotImplementedException();
        }
        public void GetBucketFile(string bucketname, string filename, string path, string filename2, string path2, string filename3, string path3)
        {
            throw new NotImplementedException();
        }
        public void GetBucketFile(string bucketname, string filename, string path, string filename2, string path2, string filename3, string path3, string filename4)
        {
            throw new NotImplementedException();
        }
        public void GetBucketFile(string bucketname, string filename, string path, string filename2, string path2, string filename3, string path3, string filename4, string path4)
        {
            throw new NotImplementedException();
        }
        public void GetBucketFile(string bucketname, string filename, string path, string filename2, string path2, string filename3, string path3, string filename4, string path4, string filename5)
        {
            throw new NotImplementedException();
        }
        public void GetBucketFile(string bucketname, string filename, string path, string filename2, string path2, string filename3, string path3, string filename4, string path4, string filename5, string path5)
        {
            throw new NotImplementedException();
        }
        public void GetBucketFile(string bucketname, string filename, string path, string filename2, string path2, string filename3, string path3, string filename4, string path4, string filename5, string path5, string filename6)
        {
            throw new NotImplementedException();
        }
        public void GetBucketFile(string bucketname, string filename, string path, string filename2, string path2, string filename3, string path3, string filename4, string path4, string filename5, string path5, string filename6, string path6)
        {
            throw new NotImplementedException();
        }
        public static string GetSupabaseDataType(string netDataType)
        {
            var mapping = DataTypeFieldMappingHelper.GetSupabaseDataTypeMappings().FirstOrDefault(m => m.NetDataType == netDataType);
            return mapping != null ? mapping.DataType : "text"; // Default to "text" if no match found
        }
        private object ConvertToDynamicType(List<Dictionary<string, object>> records, Type dynamicType, EntityStructure entityStructure)
        {
            Type uowGenericType = typeof(ObservableBindingList<>).MakeGenericType(dynamicType);
            var dynamicRecords = (IBindingListView)Activator.CreateInstance(uowGenericType);

            foreach (var record in records)
            {
                dynamic instance = Activator.CreateInstance(dynamicType);

                foreach (var field in entityStructure.Fields)
                {
                    string fieldName = field.fieldname;
                    if (record.ContainsKey(fieldName))
                    {
                        object value = record[fieldName];

                        // Handle ObjectId separately
                        if (field.fieldtype == "System.String" && value != null && value.GetType().Name == "ObjectId")
                        {
                            value = value.ToString();
                        }

                        // Handle type conversion if necessary
                        Type netType = Type.GetType(field.fieldtype);
                        if (netType != null)
                        {
                            if (netType == typeof(string))
                            {
                                value = value?.ToString();
                            }
                            else if (value != null)
                            {
                                value = Convert.ChangeType(value, netType);
                            }
                        }

                        dynamicType.GetProperty(fieldName)?.SetValue(instance, value);
                    }
                }

                dynamicRecords.Add(instance);
            }

            return dynamicRecords;
        }
        #endregion "Supporting Methods From Supabase"
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
        // ~SupabaseDataSource()
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
    }
}
