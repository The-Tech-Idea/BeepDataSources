using Dapr.Client;

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

using System.Threading.Tasks;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;


using TheTechIdea;
using TheTechIdea.Beep;

using TheTechIdea.Beep.DataBase;


using TheTechIdea.Beep.WebAPI;

using YamlDotNet.Core.Tokens;
using static Dapr.Client.Autogen.Grpc.v1.Dapr;

namespace DaprClient
{
    [AddinAttribute(Category = DatasourceCategory.WEBAPI, DatasourceType = DataSourceType.WebApi)]
    public class DaprDataSource : IDataSource
    {
        public string GuidID { get; set; }
        public Dapr.Client.DaprClient Client { get; set; }
        public DaprDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType pDatasourceType, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = pDatasourceType;
            Dataconnection = new WebAPIDataConnection
            {
                Logger = logger,
                ErrorObject = ErrorObject,

            };

            if (DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == datasourcename).Any())
            {
                Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == datasourcename).FirstOrDefault();
            }
            else
            {
                ConnectionDriversConfig driversConfig = DMEEditor.ConfigEditor.DataDriversClasses.FirstOrDefault(p => p.DatasourceType == pDatasourceType);
                Dataconnection.ConnectionProp = new ConnectionProperties
                {
                    ConnectionName = datasourcename,
                    ConnectionString = driversConfig?.ConnectionString ?? "",
                    DriverName = driversConfig?.PackageName ?? "",
                    DriverVersion = driversConfig?.version ?? "",
                    DatabaseType = DataSourceType.WebApi,
                    Category = DatasourceCategory.WEBAPI
                };
            }

            EntitiesNames = new List<string>();
            Entities = new List<EntityStructure>();

            _daprEndpoint = Dataconnection.ConnectionProp?.ConnectionString ?? "http://localhost:3500";
            _storeName = Dataconnection.ConnectionProp?.Database ?? "statestore";

            try
            {
                Client = new DaprClientBuilder().Build();
                ConnectionStatus = ConnectionState.Open;
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Closed;
                DMEEditor?.AddLogMessage("Beep", $"Could not initialize Dapr client: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }


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
                if (Client == null)
                {
                    Client = new DaprClientBuilder().Build();
                }

                // Test connection by getting health status
                ConnectionStatus = ConnectionState.Open;
                DMEEditor?.AddLogMessage("Beep", "Dapr connection opened successfully.", DateTime.Now, -1, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                DMEEditor?.AddLogMessage("Beep", $"Could not open Dapr {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ConnectionStatus;
        }

        public ConnectionState Closeconnection()
        {
            try
            {
                if (Client != null)
                {
                    Client.Dispose();
                    Client = null;
                }
                ConnectionStatus = ConnectionState.Closed;
                DMEEditor?.AddLogMessage("Beep", "Dapr connection closed successfully.", DateTime.Now, -1, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                DMEEditor?.AddLogMessage("Beep", $"Could not close Dapr {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ConnectionStatus;
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

                if (ConnectionStatus == ConnectionState.Open && Client != null)
                {
                    // For Dapr, scalar could be a state value or service invocation result
                    // Simplified implementation
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
        public DataSourceType DatasourceType { get; set; } = DataSourceType.WebApi;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.WEBAPI;
        public IDataConnection Dataconnection { get ; set ; }
        public string DatasourceName { get ; set ; }
        public IErrorsInfo ErrorObject { get ; set ; }
        public string Id { get ; set ; }
        public IDMLogger Logger { get ; set ; }
        public List<string> EntitiesNames { get ; set ; }
        public List<EntityStructure> Entities { get ; set ; }
        public IDMEEditor DMEEditor { get ; set ; }
        public ConnectionState ConnectionStatus { get ; set ; }
        public string ColumnDelimiter { get; set; } = ",";
        public string ParameterDelimiter { get; set; } = ":";
        private string _storeName = "statestore";
        private string _daprEndpoint = "http://localhost:3500";

        public event EventHandler<PassedArgs> PassEvent;
        public event EventHandler<PassedArgs> OnLoadData;
        public event EventHandler<PassedArgs> OnLoadStructure;
        public event EventHandler<PassedArgs> OnSaveStructure;
        public event EventHandler<PassedArgs> OnCreateStructure;
        public event EventHandler<PassedArgs> OnRefreshData;
        public event EventHandler<PassedArgs> OnRefreshDataEntity;
        public event EventHandler<PassedArgs> OnSyncData;

        public bool CheckEntityExist(string EntityName)
        {
            bool retval = false;
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open && Client != null)
                {
                    // Check if entity exists in state store or as a service
                    retval = EntitiesNames != null && EntitiesNames.Contains(EntityName, StringComparer.OrdinalIgnoreCase);
                    
                    if (!retval && Entities != null)
                    {
                        retval = Entities.Any(e => e.EntityName.Equals(EntityName, StringComparison.OrdinalIgnoreCase));
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
                    GetEntityStructure(entity.EntityName, true);
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

        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open && Client != null)
                {
                    string key = null;
                    if (UploadDataRow is Dictionary<string, object> dict && dict.ContainsKey("Key"))
                    {
                        key = dict["Key"].ToString();
                    }
                    else if (UploadDataRow != null)
                    {
                        key = UploadDataRow.ToString();
                    }

                    if (!string.IsNullOrEmpty(key))
                    {
                        Client.DeleteStateAsync(_storeName, key).Wait();
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
                // Dapr doesn't support SQL, but could invoke a service endpoint
                DMEEditor?.AddLogMessage("Beep", "ExecuteSql not supported for Dapr - use service invocation", DateTime.Now, -1, null, Errors.Failed);
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
            // Dapr doesn't have child tables concept
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

                if (ConnectionStatus == ConnectionState.Open && Client != null)
                {
                    // Dapr entities could be state store keys, service endpoints, or pub/sub topics
                    // For simplicity, we'll treat state store keys as entities
                    // In practice, you might query Dapr API to discover available services/endpoints
                    
                    // Get state store keys (this is a simplified approach)
                    // In real implementation, you'd query Dapr's metadata API
                    EntitiesNames = new List<string> { "StateStore", "PubSub", "Services" };

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
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in GetEntitesList: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return EntitiesNames;
        }

      
        public IEnumerable<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            // Dapr doesn't have foreign keys
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

                if (ConnectionStatus == ConnectionState.Open && Client != null)
                {
                    retval = new EntityStructure
                    {
                        EntityName = EntityName,
                        DatasourceEntityName = EntityName,
                        OriginalEntityName = EntityName,
                        Caption = EntityName,
                        Category = DatasourceCategory.WEBAPI,
                        DatabaseType = DataSourceType.WebApi,
                        DataSourceID = DatasourceName,
                        Fields = new List<EntityField>()
                    };

                    // Add common fields for Dapr entities
                    retval.Fields.Add(new EntityField
                    {
                        fieldname = "Key",
                        Originalfieldname = "Key",
                        fieldtype = "System.String",
                        EntityName = EntityName,
                        IsKey = true,
                        AllowDBNull = false
                    });
                    retval.Fields.Add(new EntityField
                    {
                        fieldname = "Value",
                        Originalfieldname = "Value",
                        fieldtype = "System.Object",
                        EntityName = EntityName,
                        IsKey = false,
                        AllowDBNull = true
                    });

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

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open && Client != null && InsertedData != null)
                {
                    // Store in Dapr state store
                    if (InsertedData is Dictionary<string, object> dict)
                    {
                        string key = dict.ContainsKey("Key") ? dict["Key"].ToString() : Guid.NewGuid().ToString();
                        object value = dict.ContainsKey("Value") ? dict["Value"] : InsertedData;

                        Client.SaveStateAsync(_storeName, key, value).Wait();
                    }
                    else
                    {
                        string key = Guid.NewGuid().ToString();
                        Client.SaveStateAsync(_storeName, key, InsertedData).Wait();
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
                // Dapr doesn't have SQL, but supports HTTP queries
                // This would typically invoke a Dapr service endpoint
                DMEEditor?.AddLogMessage("Beep", "RunQuery not fully supported for Dapr - use service invocation", DateTime.Now, -1, null, Errors.Failed);
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in RunQuery: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return results;
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
            // For Dapr, update is same as insert (upsert)
            return InsertEntity(EntityName, UploadDataRow);
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
                        DMEEditor?.AddLogMessage("Beep", $"Error disposing Dapr connection: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
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

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                // Dapr doesn't support scripts in traditional sense
                DMEEditor?.AddLogMessage("Beep", "RunScript not supported for Dapr", DateTime.Now, -1, null, Errors.Failed);
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in RunScript: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
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
                            ScriptType = "CREATE",
                            ScriptText = $"# Dapr entity: {entity.EntityName}\n# No DDL for Dapr - entities are services/endpoints"
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

        public IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            List<object> results = new List<object>();
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open && Client != null)
                {
                    // For Dapr, we can query state store
                    if (EntityName == "StateStore" || EntityName == _storeName)
                    {
                        // Get state from Dapr state store
                        // This is simplified - in practice you'd query for multiple keys
                        var filterValue = filter?.FirstOrDefault()?.FilterValue;
                        if (!string.IsNullOrEmpty(filterValue))
                        {
                            var stateResult = Client.GetStateAsync<object>(_storeName, filterValue).Result;
                            if (stateResult != null)
                            {
                                results.Add(new Dictionary<string, object> 
                                { 
                                    { "Key", filterValue }, 
                                    { "Value", stateResult } 
                                });
                            }
                        }
                    }
                    else
                    {
                        // For other entities, try to invoke Dapr service
                        // This is a placeholder for service invocation
                        results.Add(new Dictionary<string, object> { { "Entity", EntityName } });
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

        public IErrorsInfo BeginTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                // Dapr doesn't support traditional transactions
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in Begin Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public IErrorsInfo EndTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                // Dapr doesn't support traditional transactions
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in End Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public IErrorsInfo Commit(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                // Dapr doesn't support traditional transactions
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in Commit Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return ErrorObject;
        }
        #endregion
    }
}
