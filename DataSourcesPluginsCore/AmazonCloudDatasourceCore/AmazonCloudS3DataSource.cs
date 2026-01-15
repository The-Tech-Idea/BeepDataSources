using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using Amazon.S3;
using Amazon.S3.Model;
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
    public class AmazonCloudS3DataSource : IDataSource
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
        
        private IAmazonS3 _s3Client;
        private string _bucketName;
        private string _region = "us-east-1";
        private AmazonS3Config _s3Config;
        
        public AmazonCloudS3DataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
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
                _bucketName = Dataconnection.ConnectionProp.Database;
                _region = Dataconnection.ConnectionProp.Region ?? "us-east-1";
                _s3Config = new AmazonS3Config { RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(_region) };
                
                // Initialize S3 client
                if (!string.IsNullOrEmpty(Dataconnection.ConnectionProp.UserID) && !string.IsNullOrEmpty(Dataconnection.ConnectionProp.Password))
                {
                    var credentials = new BasicAWSCredentials(Dataconnection.ConnectionProp.UserID, Dataconnection.ConnectionProp.Password);
                    _s3Client = new AmazonS3Client(credentials, _s3Config);
                }
                else
                {
                    _s3Client = new AmazonS3Client(_s3Config);
                }
                
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

                if (ConnectionStatus == ConnectionState.Open && _s3Client != null)
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
                if (_s3Client == null)
                {
                    if (Dataconnection.ConnectionProp != null)
                    {
                        _region = Dataconnection.ConnectionProp.Region ?? "us-east-1";
                        _s3Config = new AmazonS3Config { RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(_region) };
                        
                        if (!string.IsNullOrEmpty(Dataconnection.ConnectionProp.UserID) && !string.IsNullOrEmpty(Dataconnection.ConnectionProp.Password))
                        {
                            var credentials = new BasicAWSCredentials(Dataconnection.ConnectionProp.UserID, Dataconnection.ConnectionProp.Password);
                            _s3Client = new AmazonS3Client(credentials, _s3Config);
                        }
                        else
                        {
                            _s3Client = new AmazonS3Client(_s3Config);
                        }
                    }
                }
                
                // Test connection by listing buckets
                if (_s3Client != null)
                {
                    var response = _s3Client.ListBucketsAsync().Result;
                    ConnectionStatus = ConnectionState.Open;
                    DMEEditor?.AddLogMessage("Beep", "AWS S3 connection opened successfully.", DateTime.Now, -1, null, Errors.Ok);
                }
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                DMEEditor?.AddLogMessage("Beep", $"Could not open AWS S3 connection - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ConnectionStatus;
        }

        public ConnectionState Closeconnection()
        {
            try
            {
                if (_s3Client != null)
                {
                    _s3Client.Dispose();
                    _s3Client = null;
                }
                ConnectionStatus = ConnectionState.Closed;
                DMEEditor?.AddLogMessage("Beep", "AWS S3 connection closed successfully.", DateTime.Now, -1, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                DMEEditor?.AddLogMessage("Beep", $"Could not close AWS S3 connection - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
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

                if (ConnectionStatus == ConnectionState.Open && _s3Client != null)
                {
                    // Check if bucket exists or if object exists
                    if (string.IsNullOrEmpty(_bucketName))
                    {
                        // Check for bucket
                        retval = _s3Client.DoesS3BucketExistAsync(EntityName).Result;
                    }
                    else
                    {
                        // Check for object in bucket
                        var request = new GetObjectMetadataRequest
                        {
                            BucketName = _bucketName,
                            Key = EntityName
                        };
                        try
                        {
                            _s3Client.GetObjectMetadataAsync(request).Wait();
                            retval = true;
                        }
                        catch
                        {
                            retval = false;
                        }
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
                    // In S3, entities are buckets or objects (files)
                    // For simplicity, we'll treat entity as a bucket
                    if (ConnectionStatus != ConnectionState.Open)
                    {
                        Openconnection();
                    }

                    if (ConnectionStatus == ConnectionState.Open && _s3Client != null)
                    {
                        var request = new PutBucketRequest { BucketName = entity.EntityName, BucketRegion = S3Region.FindValue(_region) };
                        _s3Client.PutBucketAsync(request).Wait();
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

        public IErrorsInfo ExecuteSql(string sql)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                // S3 doesn't support SQL, but could parse S3 commands
                DMEEditor?.AddLogMessage("Beep", "ExecuteSql not supported for S3 - use S3 operations", DateTime.Now, -1, null, Errors.Failed);
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
            // S3 doesn't have child tables
            return new List<ChildRelation>();
        }

        public DataSet GetChildTablesListFromCustomQuery(string tablename, string customquery)
        {
            return new DataSet();
        }

        public IDataReader GetDataReader(string querystring)
        {
            // Not applicable for S3
            return null;
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

                if (ConnectionStatus == ConnectionState.Open && _s3Client != null)
                {
                    if (string.IsNullOrEmpty(_bucketName))
                    {
                        // List buckets
                        var bucketsResponse = _s3Client.ListBucketsAsync().Result;
                        EntitiesNames = bucketsResponse.Buckets.Select(b => b.BucketName).ToList();
                    }
                    else
                    {
                        // List objects in bucket
                        var listRequest = new ListObjectsV2Request { BucketName = _bucketName };
                        var listResponse = _s3Client.ListObjectsV2Async(listRequest).Result;
                        EntitiesNames = listResponse.S3Objects.Select(o => o.Key).ToList();
                    }

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

                if (ConnectionStatus == ConnectionState.Open && _s3Client != null && !string.IsNullOrEmpty(_bucketName))
                {
                    // Get object from S3
                    var request = new GetObjectRequest
                    {
                        BucketName = _bucketName,
                        Key = EntityName
                    };

                    using (var response = _s3Client.GetObjectAsync(request).Result)
                    using (var reader = new StreamReader(response.ResponseStream))
                    {
                        string content = reader.ReadToEnd();
                        results.Add(new Dictionary<string, object>
                        {
                            { "Key", EntityName },
                            { "Bucket", _bucketName },
                            { "Content", content },
                            { "ContentType", response.Headers.ContentType },
                            { "Size", response.ContentLength },
                            { "LastModified", response.LastModified }
                        });
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

        public IEnumerable<object> RunQuery(string qrystr)
        {
            List<object> results = new List<object>();
            try
            {
                // For S3, query could be a list objects operation
                if (qrystr.ToUpper().StartsWith("LIST"))
                {
                    return GetEntitesList().Cast<object>().ToList();
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in RunQuery: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return results;
        }

        public IEnumerable<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            // S3 doesn't have foreign keys
            return new List<RelationShipKeys>();
        }

        public EntityStructure GetEntityStructure(string EntityName, bool refresh = false)
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

                if (ConnectionStatus == ConnectionState.Open && _s3Client != null)
                {
                    retval = new EntityStructure
                    {
                        EntityName = EntityName,
                        DatasourceEntityName = EntityName,
                        OriginalEntityName = EntityName,
                        Caption = EntityName,
                        Category = DatasourceCategory.CLOUD,
                        DatabaseType = DataSourceType.WebApi,
                        DataSourceID = DatasourceName,
                        Fields = new List<EntityField>()
                    };

                    if (string.IsNullOrEmpty(_bucketName))
                    {
                        // Bucket structure
                        retval.Fields.Add(new EntityField { FieldName = "BucketName", Fieldtype = "System.String", EntityName = EntityName, IsKey = true });
                        retval.Fields.Add(new EntityField { FieldName = "CreationDate", Fieldtype = "System.DateTime", EntityName = EntityName });
                    }
                    else
                    {
                        // Object structure
                        retval.Fields.Add(new EntityField { FieldName = "Key", Fieldtype = "System.String", EntityName = EntityName, IsKey = true });
                        retval.Fields.Add(new EntityField { FieldName = "Bucket", Fieldtype = "System.String", EntityName = EntityName });
                        retval.Fields.Add(new EntityField { FieldName = "Content", Fieldtype = "System.String", EntityName = EntityName });
                        retval.Fields.Add(new EntityField { FieldName = "Size", Fieldtype = "System.Int64", EntityName = EntityName });
                        retval.Fields.Add(new EntityField { FieldName = "LastModified", Fieldtype = "System.DateTime", EntityName = EntityName });
                        retval.Fields.Add(new EntityField { FieldName = "ContentType", Fieldtype = "System.String", EntityName = EntityName });
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
                            if (item is Dictionary<string, object> dict)
                            {
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
                        progress?.Report(new PassedArgs { Message = $"Updated {count} records" });
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

        public virtual IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            // For S3, update is same as insert (upload)
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

                if (ConnectionStatus == ConnectionState.Open && _s3Client != null && !string.IsNullOrEmpty(_bucketName))
                {
                    string key = EntityName;
                    if (DeletedDataRow is Dictionary<string, object> dict && dict.ContainsKey("Key"))
                    {
                        key = dict["Key"].ToString();
                    }
                    else if (DeletedDataRow != null)
                    {
                        key = DeletedDataRow.ToString();
                    }

                    var request = new DeleteObjectRequest
                    {
                        BucketName = _bucketName,
                        Key = key
                    };
                    _s3Client.DeleteObjectAsync(request).Wait();
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
                // S3 doesn't support scripts
                DMEEditor?.AddLogMessage("Beep", "RunScript not supported for S3", DateTime.Now, -1, null, Errors.Failed);
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
                            EntityName = entity.EntityName,
                           ScriptType= "CREATE",
                            ScriptText = $"# S3 bucket/object: {entity.EntityName}\n# Use AWS CLI or SDK to create buckets/upload objects"
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

                if (ConnectionStatus == ConnectionState.Open && _s3Client != null && !string.IsNullOrEmpty(_bucketName))
                {
                    byte[] content = null;
                    string contentType = "application/octet-stream";

                    if (InsertedData is Dictionary<string, object> dict)
                    {
                        if (dict.ContainsKey("Content"))
                        {
                            if (dict["Content"] is byte[] bytes)
                            {
                                content = bytes;
                            }
                            else if (dict["Content"] is string str)
                            {
                                content = Encoding.UTF8.GetBytes(str);
                            }
                        }
                        if (dict.ContainsKey("ContentType"))
                        {
                            contentType = dict["ContentType"].ToString();
                        }
                    }
                    else if (InsertedData is byte[] bytes)
                    {
                        content = bytes;
                    }
                    else if (InsertedData is string str)
                    {
                        content = Encoding.UTF8.GetBytes(str);
                    }

                    if (content != null)
                    {
                        var request = new PutObjectRequest
                        {
                            BucketName = _bucketName,
                            Key = EntityName,
                            InputStream = new MemoryStream(content),
                            ContentType = contentType
                        };
                        _s3Client.PutObjectAsync(request).Wait();
                    }
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
                        DMEEditor?.AddLogMessage("Beep", $"Error disposing S3 connection: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
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
        #endregion
    }
}
