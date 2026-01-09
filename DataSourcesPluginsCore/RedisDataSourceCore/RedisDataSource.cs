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
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DriversConfigurations;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using FreeRedis;



namespace TheTechIdea.Beep.Redis
{
    [AddinAttribute(Category = DatasourceCategory.NOSQL, DatasourceType =  DataSourceType.Redis)]
    public class RedisDataSource : IDataSource, IInMemoryDB
    {
        public string GuidID { get; set; }
        public DataSourceType DatasourceType { get ; set ; }
        public DatasourceCategory Category { get ; set ; }
        public IDataConnection Dataconnection { get ; set ; }
        public string DatasourceName { get ; set ; }
        public IErrorsInfo ErrorObject { get ; set ; }
        public string Id { get ; set ; }
        public IDMLogger Logger { get ; set ; }
        public List<string> EntitiesNames { get ; set ; }
        public List<EntityStructure> Entities { get ; set ; }
        public IDMEEditor DMEEditor { get ; set ; }
        public ConnectionState ConnectionStatus { get ; set ; }

        public event EventHandler<PassedArgs> PassEvent;
        public event EventHandler<PassedArgs> OnLoadData;
        public event EventHandler<PassedArgs> OnLoadStructure;
        public event EventHandler<PassedArgs> OnSaveStructure;
        public event EventHandler<PassedArgs> OnCreateStructure;
        public event EventHandler<PassedArgs> OnRefreshData;
        public event EventHandler<PassedArgs> OnRefreshDataEntity;
        public event EventHandler<PassedArgs> OnSyncData;

        public virtual string ColumnDelimiter { get; set; } = "''";
        public virtual string ParameterDelimiter { get; set; } = ":";
        public List<EntityStructure> InMemoryStructures { get; set; } = new List<EntityStructure>();
        public bool IsCreated { get; set; } = false;
        public bool IsLoaded { get; set; } = false;
        public bool IsSaved { get; set; } = false;
        public bool IsSynced { get; set; } = false;
        public ETLScriptHDR CreateScript { get; set; } = new ETLScriptHDR();
        public bool IsStructureCreated { get; set; } = false;

        #region "Redis Properties"
        private RedisClient _redisClient;
        private string _connectionString;
        private int _databaseNumber = 0;
        private bool _isConnected = false;
        #endregion

        #region "Constructor"
        public RedisDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = databasetype;
            Category = DatasourceCategory.NOSQL;

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
                    DatabaseType = DataSourceType.Redis,
                    Category = DatasourceCategory.NOSQL
                };
            }

            Dataconnection.ConnectionProp.Category = DatasourceCategory.NOSQL;
            Dataconnection.ConnectionProp.DatabaseType = DataSourceType.Redis;
            _connectionString = Dataconnection.ConnectionProp.ConnectionString ?? "localhost:6379";
            
            if (int.TryParse(Dataconnection.ConnectionProp.Database, out int dbNum))
            {
                _databaseNumber = dbNum;
            }
        }
        #endregion

        public virtual Task<double> GetScalarAsync(string query)
        {
            return Task.Run(() => GetScalar(query));
        }
        public virtual double GetScalar(string query)
        {
            ErrorObject.Flag = Errors.Ok;

            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open && _redisClient != null)
                {
                    // Execute Redis command as scalar
                    var result = _redisClient.Get<object>(query);
                    if (result != null && double.TryParse(result.ToString(), out double value))
                    {
                        return value;
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Fail", $"Error in executing scalar query ({ex.Message})", DateTime.Now, 0, "", Errors.Failed);
            }

            return 0.0;
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
                if (_redisClient == null)
                {
                    HandleConnectionStringForRedis();
                    _redisClient = new RedisClient(_connectionString);
                    
                    if (_databaseNumber > 0)
                    {
                        _redisClient.Select(_databaseNumber);
                    }

                    // Test connection
                    if (TestRedisConnection(_redisClient))
                    {
                        ConnectionStatus = ConnectionState.Open;
                        _isConnected = true;
                        DMEEditor?.AddLogMessage("Beep", "Connection to Redis opened successfully.", DateTime.Now, -1, null, Errors.Ok);
                    }
                    else
                    {
                        ConnectionStatus = ConnectionState.Broken;
                        _isConnected = false;
                        DMEEditor?.AddLogMessage("Beep", "Failed to open connection to Redis.", DateTime.Now, -1, null, Errors.Failed);
                    }
                }
                else if (_isConnected)
                {
                    ConnectionStatus = ConnectionState.Open;
                }
                else
                {
                    if (TestRedisConnection(_redisClient))
                    {
                        ConnectionStatus = ConnectionState.Open;
                        _isConnected = true;
                    }
                    else
                    {
                        ConnectionStatus = ConnectionState.Broken;
                    }
                }
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                _isConnected = false;
                DMEEditor?.AddLogMessage("Beep", $"Could not open Redis {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ConnectionStatus;
        }

        public ConnectionState Closeconnection()
        {
            try
            {
                if (_redisClient != null)
                {
                    _redisClient.Dispose();
                    _redisClient = null;
                    _isConnected = false;
                    ConnectionStatus = ConnectionState.Closed;
                    DMEEditor?.AddLogMessage("Beep", "Redis connection closed successfully.", DateTime.Now, -1, null, Errors.Ok);
                }
                else
                {
                    ConnectionStatus = ConnectionState.Closed;
                }
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                DMEEditor?.AddLogMessage("Beep", $"Could not close Redis {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
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

                if (ConnectionStatus == ConnectionState.Open && _redisClient != null)
                {
                    // Check if exact key exists
                    if (_redisClient.Exists(EntityName) > 0)
                    {
                        retval = true;
                    }
                    else
                    {
                        // Check if any keys match the pattern
                        var pattern = EntityName.EndsWith("*") ? EntityName : EntityName + "*";
                        var scanResult = _redisClient.Scan(0, pattern, 1);
                        retval = scanResult.items != null && scanResult.items.Length > 0;
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

                    if (ConnectionStatus == ConnectionState.Open && _redisClient != null)
                    {
                        // For Redis, creating an entity means ensuring the structure is known
                        // We can create a sample key if needed
                        GetEntityStructure(entity.EntityName, true);
                        SaveStructure();
                        retval = true;
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

                if (ConnectionStatus == ConnectionState.Open && _redisClient != null)
                {
                    // Delete all keys matching the entity pattern
                    var pattern = EntityName.EndsWith("*") ? EntityName : EntityName + "*";
                    var keys = new List<string>();
                    long scanCursor = 0;
                    do
                    {
                        var scanResult = _redisClient.Scan(scanCursor, pattern, 1000);
                        if (scanResult.items != null)
                        {
                            keys.AddRange(scanResult.items);
                        }
                        scanCursor = scanResult.cursor;
                    } while (scanCursor != 0);

                    if (keys.Count > 0)
                    {
                        _redisClient.Del(keys.ToArray());
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

        public IErrorsInfo ExecuteSql(string sql)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open && _redisClient != null)
                {
                    // Redis doesn't have SQL, but we can execute Redis commands
                    // This is a simple implementation that tries to execute as Redis command
                    var result = _redisClient.Execute(sql);
                    DMEEditor?.AddLogMessage("Beep", $"Executed Redis command: {sql}", DateTime.Now, -1, null, Errors.Ok);
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
            // Redis doesn't have child tables or foreign keys
            // Return empty list
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
                            ScriptText = $"# Redis entity: {entity.EntityName}\n# No DDL for Redis - entities are key patterns"
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

                if (ConnectionStatus == ConnectionState.Open && _redisClient != null)
                {
                    // Scan all keys and group by prefix pattern to discover entities
                    // Redis entities are typically key patterns (e.g., "user:*", "product:*")
                    var allKeys = new List<string>();
                    long cursor = 0;
                    int count = 1000;

                    do
                    {
                        var result = _redisClient.Scan(cursor, "*", count);
                        if (result != null && result.items != null)
                        {
                            allKeys.AddRange(result.items);
                        }
                        cursor = result.cursor;
                    } while (cursor != 0 && allKeys.Count < 10000); // Limit scan to prevent timeout

                    // Group keys by common prefix (entity pattern)
                    var entityPatterns = allKeys
                        .Where(k => !string.IsNullOrEmpty(k))
                        .Select(k => ExtractEntityPattern(k))
                        .Where(p => !string.IsNullOrEmpty(p))
                        .Distinct()
                        .OrderBy(p => p)
                        .ToList();

                    EntitiesNames = entityPatterns;

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
                else
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = "Could not open connection";
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

                if (ConnectionStatus == ConnectionState.Open && _redisClient != null)
                {
                    // Get entity structure first
                    var entityStructure = GetEntityStructure(EntityName, false);
                    if (entityStructure == null)
                    {
                        ErrorObject.Flag = Errors.Failed;
                        ErrorObject.Message = $"Entity '{EntityName}' not found";
                        return results;
                    }

                    // Find all keys matching the entity pattern
                    var pattern = EntityName.EndsWith("*") ? EntityName : EntityName + "*";
                    var keys = new List<string>();
                    long scanCursor = 0;
                    do
                    {
                        var scanResult = _redisClient.Scan(scanCursor, pattern, 1000);
                        if (scanResult.items != null)
                        {
                            keys.AddRange(scanResult.items);
                        }
                        scanCursor = scanResult.cursor;
                    } while (scanCursor != 0);

                    // Apply filters if provided
                    if (filter != null && filter.Count > 0)
                    {
                        keys = ApplyRedisFilters(keys, filter);
                    }

                    // Retrieve data for each key based on Redis data type
                    foreach (var key in keys)
                    {
                        var data = GetRedisDataByKey(key, entityStructure);
                        if (data != null)
                        {
                            results.Add(data);
                        }
                    }

                    // Convert to strongly typed objects if type is available
                    var entityType = GetEntityType(EntityName);
                    if (entityType != null && results.Count > 0)
                    {
                        // Use DMEEditor utilities to convert if available
                        results = ConvertToTypedList(results, entityType);
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

        public Task<object> GetEntityDataAsync(string EntityName, string filterstr)
        {
            return Task.Run(() =>
            {
                // Parse filter string into AppFilter list if needed
                List<AppFilter> filters = null;
                if (!string.IsNullOrEmpty(filterstr))
                {
                    filters = ParseFilterString(filterstr);
                }
                var result = GetEntity(EntityName, filters);
                return (object)result;
            });
        }

        public IEnumerable<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            // Redis doesn't have foreign keys
            // Return empty list
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

                if (ConnectionStatus == ConnectionState.Open && _redisClient != null)
                {
                    // Find keys matching the entity pattern
                    var pattern = EntityName.EndsWith("*") ? EntityName : EntityName + "*";
                    var allMatchingKeys = new List<string>();
                    long scanCursor = 0;
                    do
                    {
                        var scanResult = _redisClient.Scan(scanCursor, pattern, 100);
                        if (scanResult.items != null)
                        {
                            allMatchingKeys.AddRange(scanResult.items);
                        }
                        scanCursor = scanResult.cursor;
                    } while (scanCursor != 0 && allMatchingKeys.Count < 10);

                    var sampleKeys = allMatchingKeys.Take(10).ToList();

                    if (sampleKeys.Count == 0)
                    {
                        // Try exact match
                        if (_redisClient.Exists(EntityName) > 0)
                        {
                            sampleKeys.Add(EntityName);
                        }
                        else
                        {
                            ErrorObject.Message = $"Entity '{EntityName}' not found in Redis";
                            return null;
                        }
                    }

                    // Determine Redis data type and build structure
                    retval = BuildEntityStructureFromRedisKeys(EntityName, sampleKeys);

                    if (retval != null)
                    {
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
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    var entityStructure = GetEntityStructure(EntityName, false);
                    if (entityStructure != null && entityStructure.Fields != null && entityStructure.Fields.Count > 0)
                    {
                        // Use DMTypeBuilder to create dynamic type
                        DMTypeBuilder.CreateNewObject(DMEEditor, "TheTechIdea.Classes", EntityName, entityStructure.Fields);
                        retval = DMTypeBuilder.MyType;
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in GetEntityType: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return retval;
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

                if (ConnectionStatus == ConnectionState.Open && _redisClient != null && InsertedData != null)
                {
                    // Convert object to Redis data structure
                    if (InsertedData is Dictionary<string, object> dict)
                    {
                        // Insert as hash
                        string key = dict.ContainsKey("Key") ? dict["Key"].ToString() : $"{EntityName}:{Guid.NewGuid()}";
                        dict.Remove("Key");

                        var hashFields = new Dictionary<string, string>();
                        foreach (var kvp in dict)
                        {
                            hashFields[kvp.Key] = kvp.Value?.ToString() ?? "";
                        }

                        if (hashFields.Count > 0)
                        {
                            _redisClient.HMSet(key, hashFields);
                        }
                    }
                    else
                    {
                        // Insert as string
                        string key = $"{EntityName}:{Guid.NewGuid()}";
                        _redisClient.Set(key, InsertedData.ToString());
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

        public IEnumerable<object> RunQuery(string qrystr)
        {
            List<object> results = new List<object>();
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open && _redisClient != null && !string.IsNullOrEmpty(qrystr))
                {
                    // Execute Redis command and return result
                    var result = _redisClient.Execute(qrystr);
                    if (result != null)
                    {
                        results.Add(result);
                    }
                }
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
                if (dDLScripts != null && !string.IsNullOrEmpty(dDLScripts.ScriptText))
                {
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

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open && _redisClient != null && UploadDataRow != null)
                {
                    // Similar to InsertEntity
                    if (UploadDataRow is Dictionary<string, object> dict)
                    {
                        string key = dict.ContainsKey("Key") ? dict["Key"].ToString() : null;
                        if (!string.IsNullOrEmpty(key) && _redisClient.Exists(key) > 0)
                        {
                            dict.Remove("Key");
                            var hashFields = new Dictionary<string, string>();
                            foreach (var kvp in dict)
                            {
                                hashFields[kvp.Key] = kvp.Value?.ToString() ?? "";
                            }

                            if (hashFields.Count > 0)
                            {
                                _redisClient.HMSet(key, hashFields);
                            }
                        }
                        else
                        {
                            ErrorObject.Flag = Errors.Failed;
                            ErrorObject.Message = "Key not found for update";
                        }
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
        public Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            return Task.Run(() => GetEntity(EntityName, Filter));
        }

        #region "Helper Methods"
        private void HandleConnectionStringForRedis()
        {
            if (_connectionString.Contains("}"))
            {
                var replacements = new Dictionary<string, string>
                {
                    { "{Host}", Dataconnection.ConnectionProp.Host ?? "localhost" },
                    { "{Port}", Dataconnection.ConnectionProp.Port > 0 ? Dataconnection.ConnectionProp.Port.ToString() : "6379" },
                    { "{Database}", Dataconnection.ConnectionProp.Database ?? "0" }
                };

                if (!string.IsNullOrEmpty(Dataconnection.ConnectionProp.UserID) ||
                    !string.IsNullOrEmpty(Dataconnection.ConnectionProp.Password))
                {
                    replacements.Add("{Username}", Dataconnection.ConnectionProp.UserID ?? "");
                    replacements.Add("{Password}", Dataconnection.ConnectionProp.Password ?? "");
                }

                foreach (var replacement in replacements)
                {
                    if (!string.IsNullOrEmpty(replacement.Value))
                    {
                        _connectionString = Regex.Replace(_connectionString, Regex.Escape(replacement.Key), replacement.Value, RegexOptions.IgnoreCase);
                    }
                }

                _connectionString = Regex.Replace(_connectionString, @"\{Username\}:\{Password\}@", string.Empty, RegexOptions.IgnoreCase);
            }

            if (string.IsNullOrEmpty(_connectionString))
            {
                _connectionString = "localhost:6379";
            }
        }

        private bool TestRedisConnection(RedisClient client)
        {
            try
            {
                if (client != null)
                {
                    var result = client.Ping();
                    return result == "PONG";
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        private string ExtractEntityPattern(string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;

            // Extract prefix pattern (e.g., "user:123" -> "user:*")
            var parts = key.Split(':');
            if (parts.Length > 1)
            {
                return parts[0] + ":*";
            }

            // If no colon, check for other separators or use first few characters
            if (key.Length > 10)
            {
                return key.Substring(0, Math.Min(10, key.Length)) + "*";
            }

            return key;
        }

        private EntityStructure BuildEntityStructureFromRedisKeys(string entityName, List<string> sampleKeys)
        {
            EntityStructure entity = new EntityStructure
            {
                EntityName = entityName,
                DatasourceEntityName = entityName,
                OriginalEntityName = entityName,
                Caption = entityName,
                Category = DatasourceCategory.NOSQL,
                DatabaseType = DataSourceType.Redis,
                DataSourceID = DatasourceName,
                Fields = new List<EntityField>()
            };

            if (sampleKeys == null || sampleKeys.Count == 0)
                return entity;

            try
            {
                        // Check data type of first key
                        var firstKey = sampleKeys.First();
                        var dataType = _redisClient.Type(firstKey).ToString();

                // Build fields based on Redis data type
                switch (dataType.ToLower())
                {
                    case "string":
                        entity.Fields.Add(new EntityField
                        {
                            fieldname = "Value",
                            Originalfieldname = "Value",
                            fieldtype = "System.String",
                            EntityName = entityName,
                            IsKey = false,
                            AllowDBNull = true
                        });
                        entity.Fields.Add(new EntityField
                        {
                            fieldname = "Key",
                            Originalfieldname = "Key",
                            fieldtype = "System.String",
                            EntityName = entityName,
                            IsKey = true,
                            AllowDBNull = false
                        });
                        break;

                    case "hash":
                        // Get all fields from a sample hash
                        var hashFields = _redisClient.HGetAll(firstKey);
                        if (hashFields != null && hashFields.Count > 0)
                        {
                            entity.Fields.Add(new EntityField
                            {
                                fieldname = "Key",
                                Originalfieldname = "Key",
                                fieldtype = "System.String",
                                EntityName = entityName,
                                IsKey = true,
                                AllowDBNull = false
                            });

                            foreach (var hashField in hashFields.Keys)
                            {
                                var value = hashFields[hashField];
                                var inferredType = InferTypeFromValue(value);

                                entity.Fields.Add(new EntityField
                                {
                                    fieldname = hashField,
                                    Originalfieldname = hashField,
                                    fieldtype = inferredType,
                                    EntityName = entityName,
                                    IsKey = false,
                                    AllowDBNull = true
                                });
                            }
                        }
                        break;

                    case "list":
                    case "set":
                    case "zset":
                        entity.Fields.Add(new EntityField
                        {
                            fieldname = "Key",
                            Originalfieldname = "Key",
                            fieldtype = "System.String",
                            EntityName = entityName,
                            IsKey = true,
                            AllowDBNull = false
                        });
                        entity.Fields.Add(new EntityField
                        {
                            fieldname = "Items",
                            Originalfieldname = "Items",
                            fieldtype = "System.String",
                            EntityName = entityName,
                            IsKey = false,
                            AllowDBNull = true
                        });
                        break;
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error building entity structure: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return entity;
        }

        private string InferTypeFromValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "System.String";

            if (int.TryParse(value, out _))
                return "System.Int32";
            if (long.TryParse(value, out _))
                return "System.Int64";
            if (double.TryParse(value, out _))
                return "System.Double";
            if (decimal.TryParse(value, out _))
                return "System.Decimal";
            if (bool.TryParse(value, out _))
                return "System.Boolean";
            if (DateTime.TryParse(value, out _))
                return "System.DateTime";

            return "System.String";
        }

        private List<AppFilter> ParseFilterString(string filterstr)
        {
            var filters = new List<AppFilter>();
            try
            {
                if (!string.IsNullOrEmpty(filterstr))
                {
                    // Simple parsing - can be enhanced
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
                        else if (part.Contains("LIKE"))
                        {
                            var likeParts = part.Split(new[] { "LIKE" }, StringSplitOptions.RemoveEmptyEntries);
                            filter.FieldName = likeParts[0].Trim();
                            filter.FilterValue = likeParts.Length > 1 ? likeParts[1].Trim().Trim('\'') : "";
                            filter.Operator = "contains";
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

        private List<string> ApplyRedisFilters(List<string> keys, List<AppFilter> filters)
        {
            if (filters == null || filters.Count == 0)
                return keys;

            var filteredKeys = keys.ToList();

            foreach (var filter in filters)
            {
                if (string.IsNullOrEmpty(filter.FieldName) || string.IsNullOrEmpty(filter.FilterValue))
                    continue;

                switch (filter.Operator.ToLower())
                {
                    case "equals":
                    case "=":
                    case "==":
                        filteredKeys = filteredKeys.Where(k => k.Contains(filter.FilterValue, StringComparison.OrdinalIgnoreCase)).ToList();
                        break;
                    case "contains":
                        filteredKeys = filteredKeys.Where(k => k.Contains(filter.FilterValue, StringComparison.OrdinalIgnoreCase)).ToList();
                        break;
                    case "startswith":
                        filteredKeys = filteredKeys.Where(k => k.StartsWith(filter.FilterValue, StringComparison.OrdinalIgnoreCase)).ToList();
                        break;
                    case "endswith":
                        filteredKeys = filteredKeys.Where(k => k.EndsWith(filter.FilterValue, StringComparison.OrdinalIgnoreCase)).ToList();
                        break;
                }
            }

            return filteredKeys;
        }

        private object GetRedisDataByKey(string key, EntityStructure entityStructure)
        {
            try
            {
                if (_redisClient == null || string.IsNullOrEmpty(key))
                    return null;

                var dataType = _redisClient.Type(key);

                Dictionary<string, object> dataObject = new Dictionary<string, object>();
                dataObject["Key"] = key;

                switch (dataType.ToLower())
                {
                    case "string":
                        dataObject["Value"] = _redisClient.Get(key);
                        break;

                    case "hash":
                        var hashData = _redisClient.HGetAll(key);
                        foreach (var kvp in hashData)
                        {
                            dataObject[kvp.Key] = kvp.Value;
                        }
                        break;

                    case "list":
                        var listData = _redisClient.LRange(key, 0, -1);
                        dataObject["Items"] = listData != null ? string.Join(",", listData) : "";
                        break;

                    case "set":
                        var setData = _redisClient.SMembers(key);
                        dataObject["Items"] = setData != null ? string.Join(",", setData) : "";
                        break;

                    case "zset":
                    case "sortedset":
                        var zsetData = _redisClient.ZRange(key, 0, -1);
                        dataObject["Items"] = zsetData != null ? string.Join(",", zsetData) : "";
                        break;
                }

                return dataObject;
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error getting Redis data for key {key}: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return null;
            }
        }

        private List<object> ConvertToTypedList(List<object> data, Type entityType)
        {
            if (entityType == null || data == null || data.Count == 0)
                return data?.ToList() ?? new List<object>();

            try
            {
                var typedList = new List<object>();

                foreach (var item in data)
                {
                    if (item is Dictionary<string, object> dict)
                    {
                        var instance = Activator.CreateInstance(entityType);
                        foreach (var kvp in dict)
                        {
                            var prop = entityType.GetProperty(kvp.Key);
                            if (prop != null && prop.CanWrite)
                            {
                                var value = ConvertValue(kvp.Value, prop.PropertyType);
                                prop.SetValue(instance, value);
                            }
                        }
                        typedList.Add(instance);
                    }
                    else
                    {
                        typedList.Add(item);
                    }
                }

                return typedList;
            }
            catch
            {
                return data?.ToList() ?? new List<object>();
            }
        }

        private object ConvertValue(object value, Type targetType)
        {
            if (value == null)
                return null;

            try
            {
                if (targetType.IsAssignableFrom(value.GetType()))
                    return value;

                return Convert.ChangeType(value, targetType);
            }
            catch
            {
                return value?.ToString();
            }
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

        public IErrorsInfo OpenDatabaseInMemory(string databasename)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (int.TryParse(databasename, out int dbNum))
                {
                    _databaseNumber = dbNum;
                    if (_redisClient != null)
                    {
                        _redisClient.Select(_databaseNumber);
                    }
                }
                DatasourceName = databasename;
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in OpenDatabaseInMemory: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public string GetConnectionString()
        {
            return _connectionString ?? Dataconnection?.ConnectionProp?.ConnectionString ?? "";
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
                DMEEditor?.AddLogMessage("Beep", $"Could not save Redis Structure for {DatasourceName}- {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
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
                DMEEditor?.AddLogMessage("Beep", $"Could not Load Redis Structure for {DatasourceName}- {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
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

                if (ConnectionStatus == ConnectionState.Open && _redisClient != null)
                {
                    // Get all matching keys first
                    var pattern = EntityName.EndsWith("*") ? EntityName : EntityName + "*";
                    var allKeys = new List<string>();
                    long scanCursor = 0;
                    do
                    {
                        var scanResult = _redisClient.Scan(scanCursor, pattern, 1000);
                        if (scanResult.items != null)
                        {
                            allKeys.AddRange(scanResult.items);
                        }
                        scanCursor = scanResult.cursor;
                    } while (scanCursor != 0);

                    // Apply filters
                    if (filter != null && filter.Count > 0)
                    {
                        allKeys = ApplyRedisFilters(allKeys, filter);
                    }

                    int totalRecords = allKeys.Count;
                    int skipAmount = (pageNumber - 1) * pageSize;

                    // Get paginated keys
                    var paginatedKeys = allKeys.Skip(skipAmount).Take(pageSize).ToList();

                    // Get entity structure
                    var entityStructure = GetEntityStructure(EntityName, false);
                    var results = new List<object>();

                    foreach (var key in paginatedKeys)
                    {
                        var data = GetRedisDataByKey(key, entityStructure);
                        if (data != null)
                        {
                            results.Add(data);
                        }
                    }

                    // Populate PagedResult
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
                    // If no saved structure, discover from Redis
                    GetEntitesList();
                }

                SaveStructure();
                OnLoadStructure?.Invoke(this, (PassedArgs)DMEEditor?.Passedarguments);
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Could not Load Redis Structure for {DatasourceName}- {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
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
                DMEEditor?.AddLogMessage("Beep", $"Could not create Redis Structure for {DatasourceName}- {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
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
                    // Redis data is already available through GetEntity, so this is mainly for synchronization
                    IsLoaded = true;
                    OnLoadData?.Invoke(this, (PassedArgs)DMEEditor?.Passedarguments);
                }
            }
            catch (Exception ex)
            {
                IsLoaded = false;
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Could not Load Redis data for {DatasourceName}- {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public IErrorsInfo SyncData(IProgress<PassedArgs> progress, CancellationToken token)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                // Redis is already in sync, refresh structure if needed
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
                DMEEditor?.AddLogMessage("Beep", $"Could not Sync Redis data for {DatasourceName}- {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public IErrorsInfo SyncData(string entityname, IProgress<PassedArgs> progress, CancellationToken token)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                // Refresh specific entity structure
                GetEntityStructure(entityname, true);
                SaveStructure();
                OnRefreshDataEntity?.Invoke(this, (PassedArgs)DMEEditor?.Passedarguments);
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Could not Sync Redis entity {entityname}- {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
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
                DMEEditor?.AddLogMessage("Beep", $"Could not Refresh Redis data for {DatasourceName}- {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
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
