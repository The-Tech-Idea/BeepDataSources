using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using TheTechIdea;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.WebAPI;

namespace TheTechIdea.Beep.DataBase
{
    [AddinAttribute(Category = DatasourceCategory.WEBAPI, DatasourceType = DataSourceType.Hadoop)]
    public class HadoopDataSource : IDataSource
    {
        private bool disposedValue;
        private HttpClient _httpClient;
        private string _webHdfsUrl;
        private string _basePath = "/";

        public string ColumnDelimiter { get; set; } = ",";
        public string ParameterDelimiter { get; set; } = ":";
        public string GuidID { get; set; }
        public DataSourceType DatasourceType { get; set; }
        public DatasourceCategory Category { get; set; }
        public IDataConnection Dataconnection { get; set; }
        public string DatasourceName { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public string Id { get; set; }
        public IDMLogger Logger { get; set; }
        public List<string> EntitiesNames { get; set; } = new List<string>();
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
        public IDMEEditor DMEEditor { get; set; }
        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Closed;

        public event EventHandler<PassedArgs> PassEvent;
        public event EventHandler<PassedArgs> OnLoadData;
        public event EventHandler<PassedArgs> OnLoadStructure;
        public event EventHandler<PassedArgs> OnSaveStructure;
        public event EventHandler<PassedArgs> OnCreateStructure;
        public event EventHandler<PassedArgs> OnRefreshData;
        public event EventHandler<PassedArgs> OnRefreshDataEntity;
        public event EventHandler<PassedArgs> OnSyncData;

        public HadoopDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = databasetype;
            Category = DatasourceCategory.WEBAPI;

            EntitiesNames = new List<string>();
            Entities = new List<EntityStructure>();

            Dataconnection = new WebAPIDataConnection
            {
                Logger = logger,
                ErrorObject = ErrorObject
            };

            if (DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == datasourcename).Any())
            {
                Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == datasourcename).FirstOrDefault();
            }
            else
            {
                ConnectionDriversConfig driversConfig = DMEEditor.ConfigEditor.DataDriversClasses.FirstOrDefault(p => p.DatasourceType == databasetype);
                Dataconnection.ConnectionProp = new ConnectionProperties
                {
                    ConnectionName = datasourcename,
                    ConnectionString = driversConfig?.ConnectionString ?? "",
                    DriverName = driversConfig?.PackageName ?? "",
                    DriverVersion = driversConfig?.version ?? "",
                    DatabaseType = DataSourceType.Hadoop,
                    Category = DatasourceCategory.WEBAPI
                };
            }

            Dataconnection.ConnectionProp.Category = DatasourceCategory.WEBAPI;
            Dataconnection.ConnectionProp.DatabaseType = DataSourceType.Hadoop;
            _webHdfsUrl = Dataconnection.ConnectionProp.ConnectionString ?? "http://localhost:9870";
            _basePath = Dataconnection.ConnectionProp.Database ?? "/";
        }

        public virtual IErrorsInfo BeginTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                // Hadoop/HDFS doesn't support transactions in traditional sense
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in Begin Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return ErrorObject;
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

                if (ConnectionStatus == ConnectionState.Open && _httpClient != null)
                {
                    var path = NormalizePath(EntityName);
                    var uri = $"{_webHdfsUrl}/webhdfs/v1{path}?op=GETFILESTATUS";
                    var response = _httpClient.GetAsync(uri).Result;
                    retval = response.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in CheckEntityExist: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                retval = false;
            }
            return retval;
        }

        public ConnectionState Closeconnection()
        {
            try
            {
                if (_httpClient != null)
                {
                    _httpClient.Dispose();
                    _httpClient = null;
                }
                ConnectionStatus = ConnectionState.Closed;
                DMEEditor?.AddLogMessage("Beep", "Hadoop connection closed successfully.", DateTime.Now, -1, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                DMEEditor?.AddLogMessage("Beep", $"Could not close Hadoop {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ConnectionStatus;
        }

        public virtual IErrorsInfo Commit(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                // Hadoop/HDFS doesn't support transactions
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in Commit Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
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

                    if (ConnectionStatus == ConnectionState.Open && _httpClient != null)
                    {
                        var path = NormalizePath(entity.EntityName);
                        var uri = $"{_webHdfsUrl}/webhdfs/v1{path}?op=MKDIRS";
                        var response = _httpClient.PutAsync(uri, null).Result;
                        retval = response.IsSuccessStatusCode;

                        if (retval)
                        {
                            GetEntityStructure(entity.EntityName, true);
                        }
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

        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open && _httpClient != null)
                {
                    var path = NormalizePath(EntityName);
                    var uri = $"{_webHdfsUrl}/webhdfs/v1{path}?op=DELETE&recursive=true";
                    var response = _httpClient.DeleteAsync(uri).Result;

                    if (!response.IsSuccessStatusCode)
                    {
                        ErrorObject.Flag = Errors.Failed;
                        ErrorObject.Message = $"Failed to delete {EntityName}";
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

        public virtual IErrorsInfo EndTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                // Hadoop/HDFS doesn't support transactions
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in End Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public IErrorsInfo ExecuteSql(string sql)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                // Hadoop doesn't have SQL, but we can execute HDFS commands
                // For simplicity, this is a placeholder
                DMEEditor?.AddLogMessage("Beep", "ExecuteSql not supported for Hadoop/HDFS", DateTime.Now, -1, null, Errors.Failed);
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
            // Hadoop doesn't have child tables concept
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
                            SourceDataSourceEntityName = entity.EntityName,
                           ScriptType=  DDLScriptType.CreateEntity,
                            Ddl = $"# Hadoop entity: {entity.EntityName}\n# No Ddl for HDFS - entities are directories/files"
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

                if (ConnectionStatus == ConnectionState.Open && _httpClient != null)
                {
                    var path = NormalizePath(_basePath);
                    var uri = $"{_webHdfsUrl}/webhdfs/v1{path}?op=LISTSTATUS";
                    var response = _httpClient.GetAsync(uri).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        var content = response.Content.ReadAsStringAsync().Result;
                        var fileStatus = JsonSerializer.Deserialize<WebHdfsFileStatusResponse>(content);

                        if (fileStatus?.FileStatuses?.FileStatus != null)
                        {
                            foreach (var status in fileStatus.FileStatuses.FileStatus)
                            {
                                if (status.Type == "DIRECTORY")
                                {
                                    EntitiesNames.Add($"{path}/{status.PathSuffix}");
                                }
                                else if (status.Type == "FILE")
                                {
                                    EntitiesNames.Add($"{path}/{status.PathSuffix}");
                                }
                            }
                        }

                        // Synchronize Entities list
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
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in GetEntitesList: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return EntitiesNames;
        }

        public IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            ErrorObject.Flag = Errors.Ok;
            List<object> results = new List<object>();

            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open && _httpClient != null)
                {
                    var path = NormalizePath(EntityName);
                    var uri = $"{_webHdfsUrl}/webhdfs/v1{path}?op=OPEN";
                    var response = _httpClient.GetAsync(uri).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        var content = response.Content.ReadAsStringAsync().Result;
                        
                        // Parse content based on file type
                        var lines = content.Split('\n');
                        var entityStructure = GetEntityStructure(EntityName, false);

                        foreach (var line in lines)
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                var dataDict = ParseLineToDictionary(line, entityStructure);
                                if (dataDict != null && ApplyFilters(dataDict, filter))
                                {
                                    results.Add(dataDict);
                                }
                            }
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

        public Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            return Task.Run(() => GetEntity(EntityName, Filter));
        }

        public IEnumerable<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            // Hadoop doesn't have foreign keys
            return new List<RelationShipKeys>();
        }

        public int GetEntityIdx(string entityName)
        {
            if (Entities != null && Entities.Count > 0)
            {
                return Entities.FindIndex(p => p.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase) 
                    || p.DatasourceEntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase)
                    || p.OriginalEntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
            }
            return -1;
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

                if (ConnectionStatus == ConnectionState.Open && _httpClient != null)
                {
                    var path = NormalizePath(EntityName);
                    var uri = $"{_webHdfsUrl}/webhdfs/v1{path}?op=GETFILESTATUS";
                    var response = _httpClient.GetAsync(uri).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        var content = response.Content.ReadAsStringAsync().Result;
                        var fileStatus = JsonSerializer.Deserialize<WebHdfsFileStatus>(content);

                        retval = new EntityStructure
                        {
                            EntityName = EntityName,
                            DatasourceEntityName = path,
                            OriginalEntityName = EntityName,
                            Caption = Path.GetFileName(EntityName),
                            Category = DatasourceCategory.WEBAPI,
                            DatabaseType = DataSourceType.Hadoop,
                            DataSourceID = DatasourceName,
                            Fields = new List<EntityField>()
                        };

                        // If it's a file, try to infer structure from file content
                        if (fileStatus?.FileStatus?.Type == "FILE")
                        {
                            // For text files, read first few lines to infer structure
                            var readUri = $"{_webHdfsUrl}/webhdfs/v1{path}?op=OPEN&length=1024";
                            var readResponse = _httpClient.GetAsync(readUri).Result;
                            if (readResponse.IsSuccessStatusCode)
                            {
                                var fileContent = readResponse.Content.ReadAsStringAsync().Result;
                                retval.Fields = InferFieldsFromContent(fileContent, EntityName);
                            }
                        }
                        else if (fileStatus?.FileStatus?.Type == "DIRECTORY")
                        {
                            // For directories, add metadata fields
                            retval.Fields.Add(new EntityField
                            {
                                FieldName = "Path",
                                Originalfieldname = "Path",
                                Fieldtype = "System.String",
                                EntityName = EntityName,
                                IsKey = true
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

                        // Save to config
                        DMEEditor?.ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities 
                        { 
                            datasourcename = DatasourceName, 
                            Entities = Entities 
                        });
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
                    // Use DMTypeBuilder to create dynamic type
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

        public virtual double GetScalar(string query)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                // For Hadoop, scalar could be file count or size
                if (query.ToUpper().Contains("COUNT"))
                {
                    var entities = GetEntitesList();
                    return entities.Count();
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Fail", $"Error in executing scalar query ({ex.Message})", DateTime.Now, 0, "", Errors.Failed);
            }
            return 0.0;
        }

        public virtual Task<double> GetScalarAsync(string query)
        {
            return Task.Run(() => GetScalar(query));
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

                if (ConnectionStatus == ConnectionState.Open && _httpClient != null && InsertedData != null)
                {
                    var path = NormalizePath(EntityName);
                    // First create file, then append data
                    var createUri = $"{_webHdfsUrl}/webhdfs/v1{path}?op=CREATE&overwrite=true";
                    var redirectResponse = _httpClient.PutAsync(createUri, null).Result;
                    
                    if (redirectResponse.StatusCode == System.Net.HttpStatusCode.TemporaryRedirect || 
                        redirectResponse.StatusCode == System.Net.HttpStatusCode.SeeOther)
                    {
                        var redirectUri = redirectResponse.Headers.Location?.ToString();
                        if (!string.IsNullOrEmpty(redirectUri))
                        {
                            var content = new StringContent(InsertedData.ToString(), Encoding.UTF8);
                            var finalResponse = _httpClient.PutAsync(redirectUri, content).Result;
                            
                            if (!finalResponse.IsSuccessStatusCode)
                            {
                                ErrorObject.Flag = Errors.Failed;
                                ErrorObject.Message = "Failed to write data to HDFS";
                            }
                        }
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

        public ConnectionState Openconnection()
        {
            try
            {
                if (_httpClient == null)
                {
                    _httpClient = new HttpClient();
                    _httpClient.Timeout = TimeSpan.FromMinutes(5);
                }

                // Test connection by listing root directory
                var uri = $"{_webHdfsUrl}/webhdfs/v1{_basePath}?op=LISTSTATUS";
                var response = _httpClient.GetAsync(uri).Result;

                if (response.IsSuccessStatusCode)
                {
                    ConnectionStatus = ConnectionState.Open;
                    DMEEditor?.AddLogMessage("Beep", "Connection to Hadoop opened successfully.", DateTime.Now, -1, null, Errors.Ok);
                }
                else
                {
                    ConnectionStatus = ConnectionState.Broken;
                    DMEEditor?.AddLogMessage("Beep", "Failed to open connection to Hadoop.", DateTime.Now, -1, null, Errors.Failed);
                }
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                DMEEditor?.AddLogMessage("Beep", $"Could not open Hadoop {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ConnectionStatus;
        }

        public IEnumerable<object> RunQuery(string qrystr)
        {
            List<object> results = new List<object>();
            try
            {
                // Hadoop doesn't support SQL queries, return empty
                DMEEditor?.AddLogMessage("Beep", "RunQuery not supported for Hadoop/HDFS", DateTime.Now, -1, null, Errors.Failed);
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
                // Hadoop doesn't support scripts in traditional sense
                DMEEditor?.AddLogMessage("Beep", "RunScript not supported for Hadoop/HDFS", DateTime.Now, -1, null, Errors.Failed);
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in RunScript: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                // For Hadoop, update means overwrite file
                if (UploadData is IEnumerable<object> dataList)
                {
                    var content = string.Join("\n", dataList.Select(d => d?.ToString() ?? ""));
                    InsertEntity(EntityName, content);
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

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            return InsertEntity(EntityName, UploadDataRow); // Hadoop update is same as insert/overwrite
        }

        #region "Helper Methods"
        private string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return _basePath;

            if (!path.StartsWith("/"))
            {
                path = _basePath + "/" + path;
            }

            return path.Replace("\\", "/");
        }

        private List<EntityField> InferFieldsFromContent(string content, string entityName)
        {
            var fields = new List<EntityField>();
            
            if (!string.IsNullOrEmpty(content))
            {
                var lines = content.Split('\n');
                if (lines.Length > 0)
                {
                    var firstLine = lines[0];
                    var delimiter = DetectDelimiter(firstLine);
                    var columns = firstLine.Split(new[] { delimiter }, StringSplitOptions.RemoveEmptyEntries);

                    for (int i = 0; i < columns.Length; i++)
                    {
                        fields.Add(new EntityField
                        {
                            FieldName = $"Column{i + 1}",
                            Originalfieldname = $"Column{i + 1}",
                            Fieldtype = "System.String",
                            EntityName = entityName,
                            IsKey = i == 0,
                            AllowDBNull = true
                        });
                    }
                }
            }

            if (fields.Count == 0)
            {
                fields.Add(new EntityField
                {
                    FieldName = "Content",
                    Originalfieldname = "Content",
                    Fieldtype = "System.String",
                    EntityName = entityName,
                    IsKey = false,
                    AllowDBNull = true
                });
            }

            return fields;
        }

        private char DetectDelimiter(string line)
        {
            if (line.Contains(','))
                return ',';
            if (line.Contains('\t'))
                return '\t';
            if (line.Contains('|'))
                return '|';
            return ' ';
        }

        private Dictionary<string, object> ParseLineToDictionary(string line, EntityStructure entityStructure)
        {
            var dict = new Dictionary<string, object>();
            
            if (entityStructure?.Fields != null && entityStructure.Fields.Count > 0)
            {
                var delimiter = DetectDelimiter(line);
                var values = line.Split(new[] { delimiter }, StringSplitOptions.None);

                for (int i = 0; i < Math.Min(values.Length, entityStructure.Fields.Count); i++)
                {
                    dict[entityStructure.Fields[i].FieldName] = values[i];
                }
            }
            else
            {
                dict["Content"] = line;
            }

            return dict;
        }

        private bool ApplyFilters(Dictionary<string, object> data, List<AppFilter> filters)
        {
            if (filters == null || filters.Count == 0)
                return true;

            foreach (var filter in filters)
            {
                if (!string.IsNullOrEmpty(filter.FieldName) && data.ContainsKey(filter.FieldName))
                {
                    var value = data[filter.FieldName]?.ToString() ?? "";
                    var filterValue = filter.FilterValue ?? "";

                    switch (filter.Operator.ToLower())
                    {
                        case "equals":
                        case "=":
                            if (!value.Equals(filterValue, StringComparison.OrdinalIgnoreCase))
                                return false;
                            break;
                        case "contains":
                            if (!value.Contains(filterValue, StringComparison.OrdinalIgnoreCase))
                                return false;
                            break;
                    }
                }
            }

            return true;
        }
        #endregion

        #region "WebHDFS Response Classes"
        private class WebHdfsFileStatusResponse
        {
            public WebHdfsFileStatuses FileStatuses { get; set; }
        }

        private class WebHdfsFileStatuses
        {
            public List<WebHdfsFileStatusInfo> FileStatus { get; set; }
        }

        private class WebHdfsFileStatusInfo
        {
            public string PathSuffix { get; set; }
            public string Type { get; set; }
            public long Length { get; set; }
            public long ModificationTime { get; set; }
        }

        private class WebHdfsFileStatus
        {
            public WebHdfsFileStatusInfo FileStatus { get; set; }
        }
        #endregion

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        if (_httpClient != null)
                        {
                            _httpClient.Dispose();
                            _httpClient = null;
                        }
                        ConnectionStatus = ConnectionState.Closed;
                    }
                    catch (Exception ex)
                    {
                        DMEEditor?.AddLogMessage("Beep", $"Error disposing Hadoop connection: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                    }
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
