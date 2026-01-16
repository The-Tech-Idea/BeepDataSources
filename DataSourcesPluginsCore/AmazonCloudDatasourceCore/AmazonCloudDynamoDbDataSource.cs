using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.WebAPI;


namespace TheTechIdea.Beep.Cloud
{
    [AddinAttribute(Category = DatasourceCategory.CLOUD, DatasourceType = DataSourceType.WebApi)]
    public class AmazonCloudDynamoDbDataSource : IDataSource
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
        
        private IAmazonDynamoDB _dynamoDbClient;
        private DynamoDBContext _context;
        private string _region = "us-east-1";
        private AmazonDynamoDBConfig _dynamoDbConfig;
        
        public AmazonCloudDynamoDbDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
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
                 _region = string.IsNullOrWhiteSpace(Dataconnection.ConnectionProp.SchemaName) ? "us-east-1" : Dataconnection.ConnectionProp.SchemaName;
                 _dynamoDbConfig = new AmazonDynamoDBConfig { RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(_region) };
                 
                 // Initialize DynamoDB client
                if (!string.IsNullOrEmpty(Dataconnection.ConnectionProp.UserID) && !string.IsNullOrEmpty(Dataconnection.ConnectionProp.Password))
                {
                    var credentials = new BasicAWSCredentials(Dataconnection.ConnectionProp.UserID, Dataconnection.ConnectionProp.Password);
                    _dynamoDbClient = new AmazonDynamoDBClient(credentials, _dynamoDbConfig);
                }
                else
                {
                    _dynamoDbClient = new AmazonDynamoDBClient(_dynamoDbConfig);
                }
                
                _context = new DynamoDBContext(_dynamoDbClient);
                ConnectionStatus = ConnectionState.Open;
                GetEntitesList();
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
                if (_dynamoDbClient == null)
                {
                     if (Dataconnection.ConnectionProp != null)
                     {
                         _region = string.IsNullOrWhiteSpace(Dataconnection.ConnectionProp.SchemaName) ? "us-east-1" : Dataconnection.ConnectionProp.SchemaName;
                         _dynamoDbConfig = new AmazonDynamoDBConfig { RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(_region) };
                         
                         if (!string.IsNullOrEmpty(Dataconnection.ConnectionProp.UserID) && !string.IsNullOrEmpty(Dataconnection.ConnectionProp.Password))
                        {
                            var credentials = new BasicAWSCredentials(Dataconnection.ConnectionProp.UserID, Dataconnection.ConnectionProp.Password);
                            _dynamoDbClient = new AmazonDynamoDBClient(credentials, _dynamoDbConfig);
                        }
                        else
                        {
                            _dynamoDbClient = new AmazonDynamoDBClient(_dynamoDbConfig);
                        }
                        _context = new DynamoDBContext(_dynamoDbClient);
                    }
                }
                
                // Test connection by listing tables
                if (_dynamoDbClient != null)
                {
                    var response = _dynamoDbClient.ListTablesAsync().Result;
                    ConnectionStatus = ConnectionState.Open;
                    DMEEditor?.AddLogMessage("Beep", "AWS DynamoDB connection opened successfully.", DateTime.Now, -1, null, Errors.Ok);
                }
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                DMEEditor?.AddLogMessage("Beep", $"Could not open AWS DynamoDB connection - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ConnectionStatus;
        }

        public ConnectionState Closeconnection()
        {
            try
            {
                if (_context != null)
                {
                    _context.Dispose();
                    _context = null;
                }
                if (_dynamoDbClient != null)
                {
                    _dynamoDbClient.Dispose();
                    _dynamoDbClient = null;
                }
                ConnectionStatus = ConnectionState.Closed;
                DMEEditor?.AddLogMessage("Beep", "AWS DynamoDB connection closed successfully.", DateTime.Now, -1, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                DMEEditor?.AddLogMessage("Beep", $"Could not close AWS DynamoDB connection - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
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

                if (ConnectionStatus == ConnectionState.Open && _dynamoDbClient != null)
                {
                    var request = new DescribeTableRequest { TableName = EntityName };
                    try
                    {
                        var response = _dynamoDbClient.DescribeTableAsync(request).Result;
                        retval = response.Table != null;
                    }
                    catch
                    {
                        retval = false;
                    }
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

                    if (ConnectionStatus == ConnectionState.Open && _dynamoDbClient != null)
                    {
                        // Create table in DynamoDB
                        var request = new CreateTableRequest
                        {
                            TableName = entity.EntityName,
                            AttributeDefinitions = new List<AttributeDefinition>(),
                            KeySchema = new List<KeySchemaElement>(),
                            BillingMode = BillingMode.PAY_PER_REQUEST
                        };

                        // Add key attributes
                        if (entity.Fields != null && entity.Fields.Any(f => f.IsKey))
                        {
                            foreach (var keyField in entity.Fields.Where(f => f.IsKey))
                            {
                                request.AttributeDefinitions.Add(new AttributeDefinition
                                {
                                    AttributeName = keyField.FieldName,
                                    AttributeType = GetDynamoDbAttributeType(keyField.Fieldtype)
                                });
                                request.KeySchema.Add(new KeySchemaElement
                                {
                                    AttributeName = keyField.FieldName,
                                    KeyType = KeyType.HASH
                                });
                            }
                        }
                        else
                        {
                            // Default: use "Id" as key
                            request.AttributeDefinitions.Add(new AttributeDefinition
                            {
                                AttributeName = "Id",
                                AttributeType = ScalarAttributeType.S
                            });
                            request.KeySchema.Add(new KeySchemaElement
                            {
                                AttributeName = "Id",
                                KeyType = KeyType.HASH
                            });
                        }

                        _dynamoDbClient.CreateTableAsync(request).Wait();
                        retval = true;
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

        private ScalarAttributeType GetDynamoDbAttributeType(string fieldType)
        {
            if (fieldType == null) return ScalarAttributeType.S;
            fieldType = fieldType.ToLower();
            
            if (fieldType.Contains("string") || fieldType.Contains("char") || fieldType.Contains("guid"))
                return ScalarAttributeType.S;
            if (fieldType.Contains("int") || fieldType.Contains("long") || fieldType.Contains("decimal") || fieldType.Contains("double") || fieldType.Contains("float") || fieldType.Contains("number"))
                return ScalarAttributeType.N;
            if (fieldType.Contains("byte[]") || fieldType.Contains("binary"))
                return ScalarAttributeType.B;
            
            return ScalarAttributeType.S; // Default to string
        }

        public IErrorsInfo ExecuteSql(string sql)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                // DynamoDB doesn't support SQL, but supports PartiQL
                // For now, just log that SQL is not supported
                DMEEditor?.AddLogMessage("Beep", "ExecuteSql not supported for DynamoDB - use DynamoDB operations or PartiQL", DateTime.Now, -1, null, Errors.Failed);
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
            // DynamoDB doesn't have child tables
            return new List<ChildRelation>();
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

                if (ConnectionStatus == ConnectionState.Open && _dynamoDbClient != null)
                {
                    var response = _dynamoDbClient.ListTablesAsync().Result;
                    EntitiesNames = response.TableNames.ToList();

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

                if (ConnectionStatus == ConnectionState.Open && _dynamoDbClient != null)
                {
                    var table = Table.LoadTable(_dynamoDbClient, EntityName);
                    
                    ScanFilter scanFilter = new ScanFilter();
                    if (filter != null && filter.Count > 0)
                    {
                        foreach (var appFilter in filter)
                        {
                            if (!string.IsNullOrEmpty(appFilter.FieldName) && !string.IsNullOrEmpty(appFilter.FilterValue))
                            {
                                scanFilter.AddCondition(appFilter.FieldName, ScanOperator.Equal, appFilter.FilterValue);
                            }
                        }
                     }
 
                     var scanResult = table.Scan(scanFilter);
                     do
                     {
                         var documents = scanResult.GetNextSetAsync().GetAwaiter().GetResult();
                         foreach (var document in documents)
                         {
                             results.Add(ConvertDocumentToDictionary(document));
                         }
                     } while (!scanResult.IsDone);
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

        private Dictionary<string, object> ConvertDocumentToDictionary(Document document)
        {
            var dict = new Dictionary<string, object>();
            foreach (var attribute in document.GetAttributeNames())
            {
                var value = document[attribute];
                if (value is DynamoDBEntry entry)
                {
                    dict[attribute] = entry.AsString();
                }
                else
                {
                    dict[attribute] = value;
                }
            }
            return dict;
        }

        public PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            PagedResult pagedResult = new PagedResult();
            ErrorObject.Flag = Errors.Ok;

            try
            {
                var allResults = GetEntity(EntityName, filter).ToList();
                int totalRecords = allResults.Count;
                int skipAmount = (pageNumber - 1) * pageSize;
                var paginatedResults = allResults.Skip(skipAmount).Take(pageSize).ToList();

                pagedResult.Data = paginatedResults;
                pagedResult.TotalRecords = totalRecords;
                pagedResult.PageNumber = pageNumber;
                pagedResult.PageSize = pageSize;
                pagedResult.TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
                pagedResult.HasNextPage = pageNumber < pagedResult.TotalPages;
                pagedResult.HasPreviousPage = pageNumber > 1;
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
            // DynamoDB doesn't have foreign keys
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

                if (ConnectionStatus == ConnectionState.Open && _dynamoDbClient != null)
                {
                    var describeRequest = new DescribeTableRequest { TableName = EntityName };
                    var describeResponse = _dynamoDbClient.DescribeTableAsync(describeRequest).Result;
                    
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

                    int fieldIndex = 0;
                    foreach (var attrDef in describeResponse.Table.AttributeDefinitions)
                    {
                        retval.Fields.Add(new EntityField
                        {
                            FieldName = attrDef.AttributeName,
                            Originalfieldname = attrDef.AttributeName,
                            Fieldtype = GetFieldTypeFromDynamoDbType(attrDef.AttributeType),
                            EntityName = EntityName,
                            IsKey = describeResponse.Table.KeySchema.Any(k => k.AttributeName == attrDef.AttributeName),
                            AllowDBNull = true,
                            FieldIndex = fieldIndex++
                        });
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

         private static string GetFieldTypeFromDynamoDbType(ScalarAttributeType attributeType)
         {
             var dynamoType = attributeType?.Value ?? attributeType?.ToString();
             return dynamoType switch
             {
                 "S" => "System.String",
                 "N" => "System.Decimal",
                 "B" => "System.Byte[]",
                 _ => "System.String"
             };
         }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            if (fnd != null && !string.IsNullOrEmpty(fnd.EntityName))
            {
                return GetEntityStructure(fnd.EntityName, refresh);
            }
            return null;
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
            // For DynamoDB, update is same as insert (upsert)
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

                if (ConnectionStatus == ConnectionState.Open && _dynamoDbClient != null)
                {
                    var table = Table.LoadTable(_dynamoDbClient, EntityName);
                    
                    // Get key from DeletedDataRow
                    if (DeletedDataRow is Dictionary<string, object> dict)
                    {
                        var key = new Primitive();
                        string keyName = dict.Keys.FirstOrDefault();
                        if (dict.ContainsKey("Id"))
                        {
                            keyName = "Id";
                        }
                        
                        if (!string.IsNullOrEmpty(keyName) && dict.ContainsKey(keyName))
                        {
                            var keyValue = dict[keyName];
                            if (keyValue is string str)
                            {
                                key = new Primitive(str);
                            }
                            else if (keyValue is int || keyValue is long || keyValue is decimal || keyValue is double)
                            {
                                key = new Primitive(keyValue.ToString(), true);
                            }
                            
                             table.DeleteItemAsync(key).GetAwaiter().GetResult();
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
                // For DynamoDB, this could parse PartiQL or use query operations
                DMEEditor?.AddLogMessage("Beep", "RunQuery for DynamoDB - use PartiQL or DynamoDB query operations", DateTime.Now, -1, null, Errors.Failed);
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in RunQuery: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return results;
        }

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                // DynamoDB doesn't support scripts in traditional sense
                DMEEditor?.AddLogMessage("Beep", "RunScript not supported for DynamoDB", DateTime.Now, -1, null, Errors.Failed);
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
                             DestinationEntityName = entity.EntityName,
                             SourceDataSourceEntityName = entity.DatasourceEntityName ?? entity.EntityName,
                             DestinationDataSourceEntityName = entity.DatasourceEntityName ?? entity.EntityName,
                             ScriptType = DDLScriptType.CreateEntity,
                             Ddl = $"# DynamoDB table: {entity.EntityName}\n# Use AWS SDK or CLI to create tables"
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

                if (ConnectionStatus == ConnectionState.Open && _dynamoDbClient != null && InsertedData != null)
                {
                    var table = Table.LoadTable(_dynamoDbClient, EntityName);
                    var document = new Document();

                    if (InsertedData is Dictionary<string, object> dict)
                    {
                        foreach (var kvp in dict)
                        {
                            document[kvp.Key] = ConvertToDynamoDbEntry(kvp.Value);
                        }
                    }
                    else
                    {
                        // Use reflection to convert object to document
                        var type = InsertedData.GetType();
                        foreach (var prop in type.GetProperties())
                        {
                            var value = prop.GetValue(InsertedData);
                            document[prop.Name] = ConvertToDynamoDbEntry(value);
                        }
                    }

                     table.PutItemAsync(document).GetAwaiter().GetResult();
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

        private DynamoDBEntry ConvertToDynamoDbEntry(object value)
        {
            if (value == null) return new Primitive((string)null);
            if (value is string str) return new Primitive(str);
            if (value is int || value is long || value is decimal || value is double || value is float)
                return new Primitive(value.ToString(), true);
            if (value is bool b) return new Primitive(b ? "1" : "0", true);
            if (value is DateTime dt) return new Primitive(dt.ToString("o"));
            if (value is byte[] bytes) return bytes;
            
            return new Primitive(value.ToString());
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

                if (ConnectionStatus == ConnectionState.Open && _dynamoDbClient != null)
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
                        DMEEditor?.AddLogMessage("Beep", $"Error disposing DynamoDB connection: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
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
