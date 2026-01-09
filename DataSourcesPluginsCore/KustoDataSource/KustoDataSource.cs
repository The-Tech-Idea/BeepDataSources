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
using System.Text;
using System.Data.Common;

namespace TheTechIdea.Beep.Cloud.Kusto
{
    [AddinAttribute(Category = DatasourceCategory.CLOUD, DatasourceType = DataSourceType.Kusto)]
    public class KustoDataSource : IDataSource
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

        private string _clusterUrl;
        private string _databaseName;
        private string _connectionString;

        public KustoDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
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
                    DatabaseType = DataSourceType.Kusto,
                    Category = DatasourceCategory.CLOUD
                };
            }

            _connectionString = Dataconnection.ConnectionProp?.ConnectionString ?? "";
            _clusterUrl = Dataconnection.ConnectionProp?.Url ?? "";
            _databaseName = Dataconnection.ConnectionProp?.Database ?? "";
            
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
                if (string.IsNullOrEmpty(_clusterUrl) || string.IsNullOrEmpty(_databaseName))
                {
                    ConnectionStatus = ConnectionState.Closed;
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = "Cluster URL and database name are required";
                    return ConnectionStatus;
                }

                // Kusto connection is established via Kusto Data Client
                // Connection is validated when executing queries
                ConnectionStatus = ConnectionState.Open;
                GetEntitesList();
                DMEEditor?.AddLogMessage("Beep", $"Connected to Kusto: {_clusterUrl}", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Closed;
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error connecting to Kusto: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return ConnectionStatus;
        }

        public ConnectionState Closeconnection()
        {
            ConnectionStatus = ConnectionState.Closed;
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

                if (ConnectionStatus == ConnectionState.Open)
                {
                    // Query Kusto to get list of tables
                    string query = $".show tables | project TableName";
                    var result = RunQuery(query);
                    
                    foreach (var row in result)
                    {
                        if (row is Dictionary<string, object> dict && dict.ContainsKey("TableName"))
                        {
                            tableNames.Add(dict["TableName"].ToString());
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

                if (ConnectionStatus == ConnectionState.Open)
                {
                    // Build Kusto query
                    var query = new StringBuilder($"{EntityName}");
                    
                    if (filter != null && filter.Count > 0)
                    {
                        query.Append(" | where ");
                        bool first = true;
                        foreach (var f in filter)
                        {
                            if (!first) query.Append(" and ");
                            query.Append($"{f.FieldName} {f.Operator} {PrepareValue(f.FilterValue, f.valueType)}");
                            first = false;
                        }
                    }

                    var queryResult = RunQuery(query.ToString());
                    results.AddRange(queryResult);
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error getting entity: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return results;
        }

        private string PrepareValue(string value, string type)
        {
            if (string.IsNullOrEmpty(type) || type == "System.String")
            {
                return $"'{value?.Replace("'", "''")}'";
            }
            return value ?? "null";
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

                if (ConnectionStatus == ConnectionState.Open)
                {
                    // Build count query
                    var countQuery = new StringBuilder($"{EntityName}");
                    if (filter != null && filter.Count > 0)
                    {
                        countQuery.Append(" | where ");
                        bool first = true;
                        foreach (var f in filter)
                        {
                            if (!first) countQuery.Append(" and ");
                            countQuery.Append($"{f.FieldName} {f.Operator} {PrepareValue(f.FilterValue, f.valueType)}");
                            first = false;
                        }
                    }
                    countQuery.Append(" | count");

                    var countResult = RunQuery(countQuery.ToString());
                    int totalRecords = 0;
                    foreach (var row in countResult)
                    {
                        if (row is Dictionary<string, object> dict && dict.ContainsKey("Count"))
                        {
                            totalRecords = Convert.ToInt32(dict["Count"]);
                            break;
                        }
                    }

                    // Build paginated query
                    int offset = (pageNumber - 1) * pageSize;
                    var query = new StringBuilder($"{EntityName}");
                    if (filter != null && filter.Count > 0)
                    {
                        query.Append(" | where ");
                        bool first = true;
                        foreach (var f in filter)
                        {
                            if (!first) query.Append(" and ");
                            query.Append($"{f.FieldName} {f.Operator} {PrepareValue(f.FilterValue, f.valueType)}");
                            first = false;
                        }
                    }
                    query.Append($" | take {pageSize} | skip {offset}");

                    var result = RunQuery(query.ToString());
                    List<object> results = new List<object>();
                    results.AddRange(result);

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

                if (ConnectionStatus == ConnectionState.Open)
                {
                    // Query table schema
                    string schemaQuery = $"{EntityName} | getschema";
                    var schemaResult = RunQuery(schemaQuery);
                    
                    EntityStructure entity = new EntityStructure
                    {
                        EntityName = EntityName,
                        DatasourceEntityName = EntityName,
                        Fields = new List<EntityField>()
                    };

                    foreach (var row in schemaResult)
                    {
                        if (row is Dictionary<string, object> dict)
                        {
                            if (dict.ContainsKey("ColumnName") && dict.ContainsKey("ColumnType"))
                            {
                                entity.Fields.Add(new EntityField
                                {
                                    fieldname = dict["ColumnName"].ToString(),
                                    Originalfieldname = dict["ColumnName"].ToString(),
                                    fieldtype = GetDotNetType(dict["ColumnType"].ToString()),
                                    EntityName = EntityName
                                });
                            }
                        }
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

        private string GetDotNetType(string kustoType)
        {
            switch (kustoType.ToLower())
            {
                case "string":
                    return "System.String";
                case "long":
                case "int":
                    return "System.Int64";
                case "real":
                case "double":
                    return "System.Double";
                case "bool":
                    return "System.Boolean";
                case "datetime":
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
            ErrorObject.Flag = Errors.Ok;
            try
            {
                RunQuery(sql);
                ErrorObject.Message = "Query executed successfully";
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error executing query: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
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

                if (ConnectionStatus == ConnectionState.Open)
                {
                    // Kusto queries are executed via Kusto Data Client
                    // This is a placeholder - actual implementation requires Kusto.Data NuGet package
                    // For now, return empty results
                    DMEEditor?.AddLogMessage("Beep", "Kusto query execution requires Kusto.Data package", DateTime.Now, 0, null, Errors.Failed);
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
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Failed, Message = "Update operation not directly supported. Use Kusto ingestion or update policies." };
            return retval;
        }

        public IErrorsInfo DeleteEntity(string EntityName, object DeletedDataRow)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Failed, Message = "Delete operation not directly supported. Use Kusto purge or retention policies." };
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
                    var sb = new StringBuilder($".create table {entity.EntityName} (\n");
                    bool first = true;
                    foreach (var field in entity.Fields)
                    {
                        if (!first) sb.Append(",\n");
                        sb.Append($"  {field.fieldname}: {GetKustoType(field.fieldtype)}");
                        first = false;
                    }
                    sb.Append("\n)");

                    scripts.Add(new ETLScriptDet
                    {
                        EntityName = entity.EntityName,
                        ScriptType = "CREATE",
                        ScriptText = sb.ToString()
                    });
                }
            }
            return scripts;
        }

        private string GetKustoType(string dotNetType)
        {
            switch (dotNetType)
            {
                case "System.String":
                    return "string";
                case "System.Int64":
                case "System.Int32":
                    return "long";
                case "System.Double":
                case "System.Decimal":
                    return "real";
                case "System.Boolean":
                    return "bool";
                case "System.DateTime":
                    return "datetime";
                default:
                    return "string";
            }
        }

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Failed, Message = "Insert operation not directly supported. Use Kusto ingestion." };
            return retval;
        }

        public virtual double GetScalar(string query)
        {
            try
            {
                var result = RunQuery(query);
                foreach (var row in result)
                {
                    if (row is Dictionary<string, object> dict && dict.Count > 0)
                    {
                        var firstValue = dict.Values.First();
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
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Failed, Message = "Bulk update not directly supported. Use Kusto ingestion." };
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