using System;
using System.Collections.Generic;
using System.Data;
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
using Google.Cloud.BigQuery.V2;
using System.Text;

namespace TheTechIdea.Beep.Cloud.GoogleBigQuery
{
    [AddinAttribute(Category = DatasourceCategory.CLOUD, DatasourceType = DataSourceType.GoogleBigQuery)]
    public class GoogleBigQueryDataSource : IDataSource
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

        private BigQueryClient _client;
        private string _projectId;
        private string _datasetId;

        public GoogleBigQueryDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
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
                    DatabaseType = DataSourceType.GoogleBigQuery,
                    Category = DatasourceCategory.CLOUD
                };
            }

            _projectId = Dataconnection.ConnectionProp?.Database ?? "";
            _datasetId = Dataconnection.ConnectionProp?.SchemaName ?? "default";
            
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

        public ConnectionState Openconnection()
        {
            try
            {
                if (string.IsNullOrEmpty(_projectId))
                {
                    ConnectionStatus = ConnectionState.Closed;
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = "Project ID is required";
                    return ConnectionStatus;
                }

                _client = BigQueryClient.Create(_projectId);
                ConnectionStatus = ConnectionState.Open;
                GetEntitesList();
                DMEEditor?.AddLogMessage("Beep", $"Connected to BigQuery: {_projectId}", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Closed;
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error connecting to BigQuery: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return ConnectionStatus;
        }

        public ConnectionState Closeconnection()
        {
            ConnectionStatus = ConnectionState.Closed;
            _client = null;
            return ConnectionStatus;
        }

        public IEnumerable<string> GetEntitesList()
        {
            List<string> tableNames = new List<string>();
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open && _client != null)
                {
                    var dataset = _client.GetDataset(_datasetId);
                    if (dataset != null)
                    {
                        var tables = dataset.ListTables().ToList();
                        foreach (var table in tables)
                        {
                            tableNames.Add(table.Reference.TableId);
                        }
                    }
                    EntitiesNames = tableNames;
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error getting entities list: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return tableNames;
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

                if (ConnectionStatus == ConnectionState.Open && _client != null)
                {
                    var query = BuildQuery(EntityName, filter);
                    var result = _client.ExecuteQuery(query, parameters: null);
                    
                    foreach (var row in result)
                    {
                        var rowDict = new Dictionary<string, object>();
                        foreach (var field in row.Schema.Fields)
                        {
                            rowDict[field.Name] = row[field.Name]?.Value;
                        }
                        results.Add(rowDict);
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error getting entity: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return results;
        }

        private string BuildQuery(string tableName, List<AppFilter> filter)
        {
            var sb = new StringBuilder($"SELECT * FROM `{_projectId}.{_datasetId}.{tableName}`");
            
            if (filter != null && filter.Count > 0)
            {
                sb.Append(" WHERE ");
                bool first = true;
                foreach (var f in filter)
                {
                    if (!first) sb.Append(" AND ");
                    sb.Append($"`{f.FieldName}` {f.Operator} {PrepareValue(f.FilterValue, f.valueType)}");
                    first = false;
                }
            }
            
            return sb.ToString();
        }

        private string PrepareValue(string value, string type)
        {
            if (string.IsNullOrEmpty(type) || type == "System.String")
            {
                return $"'{value?.Replace("'", "''")}'";
            }
            return value ?? "NULL";
        }

        public PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            PagedResult pagedResult = new PagedResult();
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open && _client != null)
                {
                    // Build count query
                    var countQuery = $"SELECT COUNT(*) as total FROM `{_projectId}.{_datasetId}.{EntityName}`";
                    if (filter != null && filter.Count > 0)
                    {
                        countQuery += " WHERE ";
                        bool first = true;
                        foreach (var f in filter)
                        {
                            if (!first) countQuery += " AND ";
                            countQuery += $"`{f.FieldName}` {f.Operator} {PrepareValue(f.FilterValue, f.valueType)}";
                            first = false;
                        }
                    }

                    var countResult = _client.ExecuteQuery(countQuery, parameters: null);
                    int totalRecords = 0;
                    foreach (var row in countResult)
                    {
                        totalRecords = Convert.ToInt32(row["total"]?.Value ?? 0);
                        break;
                    }

                    // Build paginated query
                    int offset = (pageNumber - 1) * pageSize;
                    var query = BuildQuery(EntityName, filter);
                    query += $" LIMIT {pageSize} OFFSET {offset}";

                    var result = _client.ExecuteQuery(query, parameters: null);
                    List<object> results = new List<object>();
                    foreach (var row in result)
                    {
                        var rowDict = new Dictionary<string, object>();
                        foreach (var field in row.Schema.Fields)
                        {
                            rowDict[field.Name] = row[field.Name]?.Value;
                        }
                        results.Add(rowDict);
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
                DMEEditor?.AddLogMessage("Beep", $"Error in GetEntity with pagination: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }

            return pagedResult;
        }

        public Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            return Task.Run(() => GetEntity(EntityName, Filter));
        }

        public IEnumerable<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            // BigQuery doesn't have foreign keys
            return new List<RelationShipKeys>();
        }

        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            try
            {
                if (!refresh && Entities != null && Entities.Count > 0)
                {
                    var existing = Entities.FirstOrDefault(e => e.EntityName.Equals(EntityName, StringComparison.OrdinalIgnoreCase));
                    if (existing != null)
                    {
                        return existing;
                    }
                }

                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open && _client != null)
                {
                    var table = _client.GetTable(_datasetId, EntityName);
                    var schema = table.Schema;

                    EntityStructure entity = new EntityStructure
                    {
                        EntityName = EntityName,
                        DatasourceEntityName = EntityName,
                        Fields = new List<EntityField>()
                    };

                    foreach (var field in schema.Fields)
                    {
                        entity.Fields.Add(new EntityField
                        {
                            FieldName = field.Name,
                            Originalfieldname = field.Name,
                            Fieldtype = GetDotNetType(field.Type),
                            EntityName = EntityName,
                            AllowDBNull = field.Mode == "NULLABLE"
                        });
                    }

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
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error getting entity structure: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return null;
        }

        private string GetDotNetType(BigQueryDbType type)
        {
            switch (type)
            {
                case BigQueryDbType.String:
                    return "System.String";
                case BigQueryDbType.Int64:
                    return "System.Int64";
                case BigQueryDbType.Float64:
                    return "System.Double";
                case BigQueryDbType.Bool:
                    return "System.Boolean";
                case BigQueryDbType.Timestamp:
                case BigQueryDbType.DateTime:
                    return "System.DateTime";
                case BigQueryDbType.Date:
                    return "System.DateTime";
                default:
                    return "System.String";
            }
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
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open && _client != null)
                {
                    var table = _client.GetTable(_datasetId, EntityName);
                    return table != null;
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open && _client != null)
                {
                    var schema = new TableSchemaBuilder();
                    foreach (var field in entity.Fields)
                    {
                        schema.Add(field.FieldName, GetBigQueryType(field.Fieldtype), field.AllowDBNull ? "NULLABLE" : "REQUIRED");
                    }

                    var tableRef = new TableReference
                    {
                        ProjectId = _projectId,
                        DatasetId = _datasetId,
                        TableId = entity.EntityName
                    };

                    _client.CreateTable(tableRef, schema.Build());
                    
                    if (Entities == null)
                    {
                        Entities = new List<EntityStructure>();
                    }
                    if (!Entities.Any(e => e.EntityName.Equals(entity.EntityName, StringComparison.OrdinalIgnoreCase)))
                    {
                        Entities.Add(entity);
                    }
                    
                    DMEEditor?.AddLogMessage("Beep", $"Created entity: {entity.EntityName}", DateTime.Now, 0, null, Errors.Ok);
                    return true;
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error creating entity: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return false;
        }

        private BigQueryDbType GetBigQueryType(string dotNetType)
        {
            switch (dotNetType)
            {
                case "System.String":
                    return BigQueryDbType.String;
                case "System.Int64":
                case "System.Int32":
                    return BigQueryDbType.Int64;
                case "System.Double":
                case "System.Decimal":
                    return BigQueryDbType.Float64;
                case "System.Boolean":
                    return BigQueryDbType.Bool;
                case "System.DateTime":
                    return BigQueryDbType.DateTime;
                default:
                    return BigQueryDbType.String;
            }
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

                if (ConnectionStatus == ConnectionState.Open && _client != null)
                {
                    _client.ExecuteQuery(sql, parameters: null);
                    ErrorObject.Message = "Query executed successfully";
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error executing SQL: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public IEnumerable<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            return new List<ChildRelation>();
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

                if (ConnectionStatus == ConnectionState.Open && _client != null)
                {
                    var result = _client.ExecuteQuery(qrystr, parameters: null);
                    foreach (var row in result)
                    {
                        var rowDict = new Dictionary<string, object>();
                        foreach (var field in row.Schema.Fields)
                        {
                            rowDict[field.Name] = row[field.Name]?.Value;
                        }
                        results.Add(rowDict);
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error running query: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return results;
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Failed, Message = "Update operation not directly supported. Use SQL UPDATE queries." };
            DMEEditor?.AddLogMessage("Beep", "Use ExecuteSql with UPDATE statement", DateTime.Now, 0, null, Errors.Failed);
            return retval;
        }

        public IErrorsInfo DeleteEntity(string EntityName, object DeletedDataRow)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Failed, Message = "Delete operation not directly supported. Use SQL DELETE queries." };
            DMEEditor?.AddLogMessage("Beep", "Use ExecuteSql with DELETE statement", DateTime.Now, 0, null, Errors.Failed);
            return retval;
        }

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                ExecuteSql(dDLScripts.ScriptText);
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
            }
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
                    var sb = new StringBuilder($"CREATE TABLE IF NOT EXISTS `{_projectId}.{_datasetId}.{entity.EntityName}` (\n");
                    bool first = true;
                    foreach (var field in entity.Fields)
                    {
                        if (!first) sb.Append(",\n");
                        sb.Append($"  `{field.FieldName}` {GetBigQueryTypeString(field.Fieldtype)}");
                        if (!field.AllowDBNull) sb.Append(" NOT NULL");
                        first = false;
                    }
                    sb.Append("\n)");

                    scripts.Add(new ETLScriptDet
                    {
                        EntityName = entity.EntityName,
                       ScriptType= "CREATE",
                        ScriptText = sb.ToString()
                    });
                }
            }
            return scripts;
        }

        private string GetBigQueryTypeString(string dotNetType)
        {
            switch (dotNetType)
            {
                case "System.String":
                    return "STRING";
                case "System.Int64":
                case "System.Int32":
                    return "INT64";
                case "System.Double":
                case "System.Decimal":
                    return "FLOAT64";
                case "System.Boolean":
                    return "BOOL";
                case "System.DateTime":
                    return "DATETIME";
                default:
                    return "STRING";
            }
        }

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Failed, Message = "Insert operation not directly supported. Use SQL INSERT queries." };
            DMEEditor?.AddLogMessage("Beep", "Use ExecuteSql with INSERT statement", DateTime.Now, 0, null, Errors.Failed);
            return retval;
        }

        public virtual double GetScalar(string query)
        {
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open && _client != null)
                {
                    var result = _client.ExecuteQuery(query, parameters: null);
                    foreach (var row in result)
                    {
                        var firstValue = row[0]?.Value;
                        if (firstValue != null && double.TryParse(firstValue.ToString(), out double value))
                        {
                            return value;
                        }
                    }
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

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Failed, Message = "Bulk update not directly supported. Use SQL UPDATE queries." };
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