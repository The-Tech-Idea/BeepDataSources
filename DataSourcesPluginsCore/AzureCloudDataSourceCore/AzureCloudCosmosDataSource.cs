
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Text;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Newtonsoft.Json;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.WebAPI;



namespace TheTechIdea.Beep.Cloud
{
    [AddinAttribute(Category = DatasourceCategory.CLOUD, DatasourceType = DataSourceType.WebApi)]
    public class AzureCloudCosmosDataSource : IDataSource
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
        public virtual string ColumnDelimiter { get; set; } = "''";
        public virtual string ParameterDelimiter { get; set; } = ":";
        
        private CosmosClient _cosmosClient;
        private Database _database;
        private string _endpoint;
        private string _accountKey;
        private string _databaseName;
        
        public AzureCloudCosmosDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = databasetype;
            Category = DatasourceCategory.CLOUD;
            EntitiesNames = new List<string>();
            Entities = new List<EntityStructure>();
            
            Dataconnection = new WebAPIDataConnection
            {
                Logger = logger,
                ErrorObject = ErrorObject
            };
            
            Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == datasourcename).FirstOrDefault();
            
            if (Dataconnection.ConnectionProp != null)
            {
                _endpoint = Dataconnection.ConnectionProp.Url;
                _accountKey = Dataconnection.ConnectionProp.Password;
                _databaseName = Dataconnection.ConnectionProp.Database;
                
                try
                {
                    _cosmosClient = new CosmosClient(_endpoint, _accountKey);
                    if (!string.IsNullOrEmpty(_databaseName))
                    {
                        _database = _cosmosClient.GetDatabase(_databaseName);
                        ConnectionStatus = ConnectionState.Open;
                        GetEntitesList();
                    }
                }
                catch (Exception ex)
                {
                    ConnectionStatus = ConnectionState.Closed;
                    DMEEditor?.AddLogMessage("Beep", $"Could not initialize Azure Cosmos DB client: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                }
            }
            else
            {
                ConnectionStatus = ConnectionState.Closed;
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
         public virtual Task<double> GetScalarAsync(string query)
         {
             return Task.Run(() => GetScalar(query));
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
                if (_cosmosClient == null && Dataconnection.ConnectionProp != null)
                {
                    _endpoint = Dataconnection.ConnectionProp.Url;
                    _accountKey = Dataconnection.ConnectionProp.Password;
                    _databaseName = Dataconnection.ConnectionProp.Database;
                    _cosmosClient = new CosmosClient(_endpoint, _accountKey);
                    
                    if (!string.IsNullOrEmpty(_databaseName))
                    {
                        _database = _cosmosClient.GetDatabase(_databaseName);
                    }
                }
                
                if (_cosmosClient != null)
                {
                    // Test connection
                    var response = _cosmosClient.ReadAccountAsync().Result;
                    ConnectionStatus = ConnectionState.Open;
                    DMEEditor?.AddLogMessage("Beep", "Azure Cosmos DB connection opened successfully.", DateTime.Now, -1, null, Errors.Ok);
                }
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                DMEEditor?.AddLogMessage("Beep", $"Could not open Azure Cosmos DB connection - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ConnectionStatus;
        }

        public ConnectionState Closeconnection()
        {
            try
            {
                if (_cosmosClient != null)
                {
                    _cosmosClient.Dispose();
                    _cosmosClient = null;
                    _database = null;
                }
                ConnectionStatus = ConnectionState.Closed;
                DMEEditor?.AddLogMessage("Beep", "Azure Cosmos DB connection closed successfully.", DateTime.Now, -1, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                DMEEditor?.AddLogMessage("Beep", $"Could not close Azure Cosmos DB connection - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
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

                if (ConnectionStatus == ConnectionState.Open && _database != null)
                {
                    var container = _database.GetContainer(EntityName);
                    var response = container.ReadContainerAsync().Result;
                    retval = response != null;
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
                    if (ConnectionStatus != ConnectionState.Open)
                    {
                        Openconnection();
                    }

                    if (ConnectionStatus == ConnectionState.Open && _database != null)
                    {
                        // Create container in Cosmos DB
                        var containerProperties = new ContainerProperties(entity.EntityName, "/id");
                        var containerResponse = _database.CreateContainerIfNotExistsAsync(containerProperties).Result;
                        retval = containerResponse != null;
                    }
                    
                    // Add to Entities list
                    if (Entities == null) Entities = new List<EntityStructure>();
                    int idx = GetEntityIdx(entity.EntityName);
                    if (idx >= 0)
                    {
                        Entities[idx] = entity;
                    }
                    else
                    {
                        Entities.Add(entity);
                    }
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
                // Cosmos DB supports SQL queries
                var results = RunQuery(sql);
                DMEEditor?.AddLogMessage("Beep", $"Executed SQL query: {sql}", DateTime.Now, -1, null, Errors.Ok);
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
            // Cosmos DB doesn't have child tables
            return new List<ChildRelation>();
        }

        public DataSet GetChildTablesListFromCustomQuery(string tablename, string customquery)
        {
            return new DataSet();
        }

        public IEnumerable<string> GetEntitesList()
        {
            EntitiesNames = new List<string>();
            ErrorObject.Flag = Errors.Ok;

            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open && _database != null)
                {
                    var iterator = _database.GetContainerQueryIterator<ContainerProperties>();
                    var containers = new List<ContainerProperties>();
                    
                    while (iterator.HasMoreResults)
                    {
                        var feedResponse = iterator.ReadNextAsync().Result;
                        containers.AddRange(feedResponse);
                    }
                    
                    EntitiesNames = containers.Select(c => c.Id).ToList();

                    // Sync Entities list
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
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in GetEntitesList: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return EntitiesNames;
        }

        public Task<object> GetEntityDataAsync(string entityname, string filterstr)
        {
            return Task.Run(() =>
            {
                List<AppFilter> filters = ParseFilterString(filterstr);
                var result = GetEntity(entityname, filters);
                return (object)result;
            });
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

                if (ConnectionStatus == ConnectionState.Open && _database != null)
                {
                    var container = _database.GetContainer(EntityName);
                    
                    QueryDefinition queryDefinition;
                    if (filter != null && filter.Count > 0)
                    {
                        var whereClause = BuildWhereClause(filter);
                        queryDefinition = new QueryDefinition($"SELECT * FROM c {whereClause}");
                    }
                    else
                    {
                        queryDefinition = new QueryDefinition("SELECT * FROM c");
                    }

                    var iterator = container.GetItemQueryIterator<dynamic>(queryDefinition);
                    while (iterator.HasMoreResults)
                    {
                        var response = iterator.ReadNextAsync().Result;
                        foreach (var item in response)
                        {
                            results.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in GetEntity: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return results;
        }

        private string BuildWhereClause(List<AppFilter> filters)
        {
            if (filters == null || filters.Count == 0)
                return "";

            var conditions = new List<string>();
            foreach (var filter in filters)
            {
                if (!string.IsNullOrEmpty(filter.FieldName) && !string.IsNullOrEmpty(filter.FilterValue))
                {
                    conditions.Add($"c.{filter.FieldName} = '{filter.FilterValue}'");
                }
            }

            return conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";
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

                if (ConnectionStatus == ConnectionState.Open && _database != null)
                {
                    var container = _database.GetContainer(EntityName);
                    
                    QueryDefinition queryDefinition;
                    if (filter != null && filter.Count > 0)
                    {
                        var whereClause = BuildWhereClause(filter);
                        queryDefinition = new QueryDefinition($"SELECT * FROM c {whereClause}");
                    }
                    else
                    {
                        queryDefinition = new QueryDefinition("SELECT * FROM c");
                    }

                    // Get total count
                    var countQuery = new QueryDefinition("SELECT VALUE COUNT(1) FROM c");
                    var countIterator = container.GetItemQueryIterator<int>(countQuery);
                    var countResponse = countIterator.ReadNextAsync().Result;
                    int totalRecords = countResponse.FirstOrDefault();

                    // Get paginated results
                    int skipAmount = (pageNumber - 1) * pageSize;
                    queryDefinition = new QueryDefinition($"SELECT * FROM c {BuildWhereClause(filter)} OFFSET {skipAmount} LIMIT {pageSize}");
                    var iterator = container.GetItemQueryIterator<dynamic>(queryDefinition);
                    var results = new List<object>();
                    
                    while (iterator.HasMoreResults)
                    {
                        var response = iterator.ReadNextAsync().Result;
                        foreach (var item in response)
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
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in GetEntity (paged): {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return pagedResult;
        }

        public IEnumerable<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            // Cosmos DB doesn't have foreign keys
            return new List<RelationShipKeys>();
        }

        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            ErrorObject.Flag = Errors.Ok;
            EntityStructure retval = null;

            try
            {
                if (!refresh && Entities != null && Entities.Count > 0)
                {
                    retval = Entities.Find(c => c.EntityName.Equals(EntityName, StringComparison.OrdinalIgnoreCase));
                    if (retval != null)
                    {
                        return retval;
                    }
                }

                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open && _database != null)
                {
                    var container = _database.GetContainer(EntityName);
                    
                    // Get a sample document to infer structure
                    var query = container.GetItemQueryIterator<dynamic>(new QueryDefinition("SELECT * FROM c LIMIT 1"));
                    var response = query.ReadNextAsync().Result;
                    
                    retval = new EntityStructure
                    {
                        EntityName = EntityName,
                        DatasourceEntityName = EntityName,
                         OriginalEntityName = EntityName,
                         Caption = EntityName,
                         Category = DatasourceCategory.CLOUD.ToString(),
                         DatabaseType = DataSourceType.WebApi,
                         DataSourceID = DatasourceName,
                         Fields = new List<EntityField>()
                     };

                    if (response.Any())
                    {
                        var sampleDoc = response.First();
                        var jsonDoc = JsonConvert.SerializeObject(sampleDoc);
                        var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonDoc);
                        
                        int fieldIndex = 0;
                        foreach (var kvp in dict)
                        {
                            retval.Fields.Add(new EntityField
                            {
                                FieldName = kvp.Key,
                                Originalfieldname = kvp.Key,
                                Fieldtype = GetFieldTypeFromValue(kvp.Value),
                                EntityName = EntityName,
                                IsKey = kvp.Key == "id",
                                AllowDBNull = true,
                                FieldIndex = fieldIndex++
                            });
                        }
                    }
                    else
                    {
                        // Default fields if no documents exist
                        retval.Fields.Add(new EntityField { FieldName = "id", Fieldtype = "System.String", EntityName = EntityName, IsKey = true });
                    }

                    // Add or update in Entities list
                    int idx = GetEntityIdx(EntityName);
                    if (idx >= 0)
                    {
                        Entities[idx] = retval;
                    }
                    else
                    {
                        Entities.Add(retval);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in GetEntityStructure: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return retval;
        }

        private string GetFieldTypeFromValue(object value)
        {
            if (value == null) return "System.String";
            var type = value.GetType();
            if (type == typeof(int)) return "System.Int32";
            if (type == typeof(long)) return "System.Int64";
            if (type == typeof(double)) return "System.Double";
            if (type == typeof(decimal)) return "System.Decimal";
            if (type == typeof(bool)) return "System.Boolean";
            if (type == typeof(DateTime)) return "System.DateTime";
            if (type == typeof(string)) return "System.String";
            return "System.Object";
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            if (fnd != null && !string.IsNullOrEmpty(fnd.EntityName))
            {
                return GetEntityStructure(fnd.EntityName, refresh);
            }
            return null;
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
                    var entityStructure = GetEntityStructure(EntityName, false);
                    if (entityStructure != null && entityStructure.Fields != null)
                    {
                        foreach (var field in entityStructure.Fields)
                        {
                            dt.Columns.Add(field.FieldName, Type.GetType(field.Fieldtype) ?? typeof(string));
                        }

                        foreach (var item in results)
                        {
                            var jsonItem = JsonConvert.SerializeObject(item);
                            var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonItem);
                            var row = dt.NewRow();
                            foreach (DataColumn col in dt.Columns)
                            {
                                if (dict.ContainsKey(col.ColumnName))
                                {
                                    row[col.ColumnName] = dict[col.ColumnName] ?? DBNull.Value;
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
            Type retval = null;
            try
            {
                var entityStructure = GetEntityStructure(EntityName, false);
                if (entityStructure != null && entityStructure.Fields != null && entityStructure.Fields.Count > 0)
                {
                    DMTypeBuilder.CreateNewObject(DMEEditor, "TheTechIdea.Classes", EntityName, entityStructure.Fields);
                    retval = DMTypeBuilder.MyType;
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in GetEntityType: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return retval;
        }

        public virtual IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            // For Cosmos DB, update is same as insert (upsert)
            return InsertEntity(EntityName, UploadDataRow);
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

                if (ConnectionStatus == ConnectionState.Open && _database != null)
                {
                    var container = _database.GetContainer(EntityName);
                    
                    string itemId = null;
                    string partitionKey = "/id";
                    
                    if (DeletedDataRow is Dictionary<string, object> dict)
                    {
                        if (dict.ContainsKey("id"))
                            itemId = dict["id"].ToString();
                        else if (dict.ContainsKey("Id"))
                            itemId = dict["Id"].ToString();
                    }
                    else
                    {
                        var jsonItem = JsonConvert.SerializeObject(DeletedDataRow);
                        var dict2 = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonItem);
                        if (dict2.ContainsKey("id"))
                            itemId = dict2["id"].ToString();
                        else if (dict2.ContainsKey("Id"))
                            itemId = dict2["Id"].ToString();
                    }

                    if (!string.IsNullOrEmpty(itemId))
                    {
                        container.DeleteItemAsync<dynamic>(itemId, new PartitionKey(itemId)).Wait();
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

                if (ConnectionStatus == ConnectionState.Open && _database != null && !string.IsNullOrEmpty(qrystr))
                {
                    // Parse query to extract container name if needed
                    // For now, assume query format: "SELECT * FROM ContainerName WHERE ..."
                    var containerName = ExtractContainerNameFromQuery(qrystr);
                    if (!string.IsNullOrEmpty(containerName))
                    {
                        var container = _database.GetContainer(containerName);
                        var queryDefinition = new QueryDefinition(qrystr);
                        var iterator = container.GetItemQueryIterator<dynamic>(queryDefinition);
                        
                        while (iterator.HasMoreResults)
                        {
                            var response = iterator.ReadNextAsync().Result;
                            foreach (var item in response)
                            {
                                results.Add(item);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in RunQuery: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return results;
        }

        private string ExtractContainerNameFromQuery(string query)
        {
            // Simple extraction - look for FROM clause
            var upperQuery = query.ToUpper();
            if (upperQuery.Contains("FROM"))
            {
                var fromIndex = upperQuery.IndexOf("FROM");
                var afterFrom = query.Substring(fromIndex + 4).Trim();
                var parts = afterFrom.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                {
                    return parts[0].Trim();
                }
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
                            SourceEntityName = entity.EntityName,
                            DestinationDataSourceName = DatasourceName,
                            DestinationDataSourceEntityName = entity.EntityName,
                            ScriptType = DDLScriptType.CreateEntity,
                            Ddl = $"# Cosmos DB container: {entity.EntityName}\n# Use Azure SDK or Portal to create containers"
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
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open && _database != null && InsertedData != null)
                {
                    var container = _database.GetContainer(EntityName);
                    
                    // Ensure item has an id field
                    var jsonItem = JsonConvert.SerializeObject(InsertedData);
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonItem);
                    
                    if (!dict.ContainsKey("id") && !dict.ContainsKey("Id"))
                    {
                        dict["id"] = Guid.NewGuid().ToString();
                    }
                    
                    jsonItem = JsonConvert.SerializeObject(dict);
                    var item = JsonConvert.DeserializeObject<dynamic>(jsonItem);
                    
                    string itemId = dict.ContainsKey("id") ? dict["id"].ToString() : dict["Id"].ToString();
                    container.UpsertItemAsync(item, new PartitionKey(itemId)).Wait();
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in InsertEntity: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            return Task.Run(() => GetEntity(EntityName, Filter));
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

                if (ConnectionStatus == ConnectionState.Open && _database != null)
                {
                    if (query.ToUpper().Contains("COUNT"))
                    {
                        var entities = GetEntitesList();
                        return entities.Count();
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Fail", $"Error in executing scalar query ({ex.Message})", DateTime.Now, 0, "", Errors.Failed);
            }

            return retval;
        }
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
                        DMEEditor?.AddLogMessage("Beep", $"Error disposing Cosmos DB connection: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
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
        #endregion
    }
}
