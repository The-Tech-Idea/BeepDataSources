using System;
using System.Collections.Generic;
using System.Linq;
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
using TheTechIdea.Beep.WebAPI;
using FirebaseStorage;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;

namespace TheTechIdea.Beep.Cloud.Firebase
{
    [AddinAttribute(Category = DatasourceCategory.CLOUD, DatasourceType = DataSourceType.Firebase)]
    public class FireBaseDataSource : IDataSource
    {
        public string GuidID { get; set; }
        public event EventHandler<PassedArgs> PassEvent;
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
        public ConnectionState ConnectionStatus { get; set; }
        public virtual string ColumnDelimiter { get; set; } = "''";
        public virtual string ParameterDelimiter { get; set; } = ":";

        private FirebaseStorageClient _firebaseStorage;
        private string _bucketName;
        private string _apiKey;

        public FireBaseDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = databasetype;
            Category = DatasourceCategory.CLOUD;

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
                    DatabaseType = DataSourceType.Firebase,
                    Category = DatasourceCategory.CLOUD
                };
            }

            _bucketName = Dataconnection.ConnectionProp?.Database ?? "";
            _apiKey = Dataconnection.ConnectionProp?.KeyToken ?? "";
            
            GuidID = Guid.NewGuid().ToString();
        }

        public int GetEntityIdx(string entityName)
        {
            if (Entities != null && Entities.Count > 0)
            {
                return Entities.FindIndex(p => p.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
            }
            return -1;
        }

        public ConnectionState Openconnection()
        {
            try
            {
                if (string.IsNullOrEmpty(_bucketName) || string.IsNullOrEmpty(_apiKey))
                {
                    ConnectionStatus = ConnectionState.Closed;
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = "Bucket name and API key are required";
                    return ConnectionStatus;
                }

                _firebaseStorage = new FirebaseStorageClient(_bucketName, _apiKey);
                ConnectionStatus = ConnectionState.Open;
                GetEntitesList();
                DMEEditor?.AddLogMessage("Beep", $"Connected to Firebase Storage: {_bucketName}", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Closed;
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error connecting to Firebase: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return ConnectionStatus;
        }

        public ConnectionState Closeconnection()
        {
            ConnectionStatus = ConnectionState.Closed;
            _firebaseStorage = null;
            return ConnectionStatus;
        }

        public IEnumerable<string> GetEntitesList()
        {
            List<string> entityNames = new List<string>();
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                // Firebase Storage doesn't have a direct way to list all files
                // Entities are represented as folders or file paths
                // This is a simplified implementation
                entityNames.Add("files"); // Default entity
                EntitiesNames = entityNames;
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error getting entities list: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return entityNames;
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

                // For Firebase Storage, entities are file paths
                // This is a simplified implementation
                // In a real scenario, you'd list files from a specific path
                results.Add(new { Name = EntityName, Type = "file" });
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error getting entity: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return results;
        }

        public PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            PagedResult pagedResult = new PagedResult();
            try
            {
                var allData = GetEntity(EntityName, filter).ToList();
                int totalRecords = allData.Count;
                int offset = (pageNumber - 1) * pageSize;
                var pagedData = allData.Skip(offset).Take(pageSize).ToList();

                pagedResult.Data = pagedData;
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
            }
            return pagedResult;
        }

        public Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            return Task.Run(() => GetEntity(EntityName, Filter));
        }

        public IEnumerable<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            return new List<RelationShipKeys>();
        }

        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            try
            {
                var entity = new EntityStructure
                {
                    EntityName = EntityName,
                    DatasourceEntityName = EntityName,
                    Fields = new List<EntityField>
                    {
                        new EntityField { fieldname = "Name", fieldtype = "System.String", EntityName = EntityName },
                        new EntityField { fieldname = "Path", fieldtype = "System.String", EntityName = EntityName },
                        new EntityField { fieldname = "Size", fieldtype = "System.Int64", EntityName = EntityName },
                        new EntityField { fieldname = "ContentType", fieldtype = "System.String", EntityName = EntityName }
                    }
                };

                if (Entities == null)
                {
                    Entities = new List<EntityStructure>();
                }

                var idx = GetEntityIdx(EntityName);
                if (idx >= 0)
                {
                    Entities[idx] = entity;
                }
                else
                {
                    Entities.Add(entity);
                }

                return entity;
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error getting entity structure: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return null;
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            return GetEntityStructure(fnd?.EntityName ?? "", refresh);
        }

        public Type GetEntityType(string EntityName)
        {
            try
            {
                var entity = GetEntityStructure(EntityName, false);
                if (entity != null)
                {
                    return DMTypeBuilder.CreateTypeFromEntityStructure(entity, DMEEditor);
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error getting entity type: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return typeof(object);
        }

        public bool CheckEntityExist(string EntityName)
        {
            return EntitiesNames.Contains(EntityName, StringComparer.OrdinalIgnoreCase);
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            try
            {
                if (Entities == null)
                {
                    Entities = new List<EntityStructure>();
                }
                if (!Entities.Any(e => e.EntityName.Equals(entity.EntityName, StringComparison.OrdinalIgnoreCase)))
                {
                    Entities.Add(entity);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public IErrorsInfo ExecuteSql(string sql)
        {
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = "SQL execution not supported for Firebase Storage";
            return ErrorObject;
        }

        public IEnumerable<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            return new List<ChildRelation>();
        }

        public IEnumerable<object> RunQuery(string qrystr)
        {
            return Enumerable.Empty<object>();
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok };
            // Implementation would upload file to Firebase Storage
            return retval;
        }

        public IErrorsInfo DeleteEntity(string EntityName, object DeletedDataRow)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok };
            // Implementation would delete file from Firebase Storage
            return retval;
        }

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = "Script execution not supported for Firebase Storage";
            return ErrorObject;
        }

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            ErrorObject.Flag = Errors.Ok;
            foreach (var entity in entities)
            {
                CreateEntityAs(entity);
            }
            return ErrorObject;
        }

        public IEnumerable<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            List<ETLScriptDet> scripts = new List<ETLScriptDet>();
            var entitiesToScript = entities ?? Entities;
            if (entitiesToScript != null)
            {
                foreach (var entity in entitiesToScript)
                {
                    scripts.Add(new ETLScriptDet
                    {
                        EntityName = entity.EntityName,
                        ScriptType = "CREATE",
                        ScriptText = $"# Firebase Storage entity: {entity.EntityName}"
                    });
                }
            }
            return scripts;
        }

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok };
            // Implementation would upload file to Firebase Storage
            return retval;
        }

        public virtual double GetScalar(string query)
        {
            return 0.0;
        }

        public virtual Task<double> GetScalarAsync(string query)
        {
            return Task.Run(() => GetScalar(query));
        }

        public virtual IErrorsInfo BeginTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            return ErrorObject;
        }

        public virtual IErrorsInfo EndTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            return ErrorObject;
        }

        public virtual IErrorsInfo Commit(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            return ErrorObject;
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok };
            return retval;
        }

        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Closeconnection();
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