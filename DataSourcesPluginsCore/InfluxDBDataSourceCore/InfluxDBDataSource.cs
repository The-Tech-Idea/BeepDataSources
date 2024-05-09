using InfluxDB.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.WebAPI;
using InfluxDB.Client.Api.Domain;

namespace InfluxDBDataSourceCore
{
    [AddinAttribute(Category = DatasourceCategory.NOSQL, DatasourceType = DataSourceType.InfluxDB)]
    public class InfluxDBDataSource : IDataSource
    {

        //MH8XkA5B_dDp99-tEurjOsYTU8tuBNu7bigSGg77YfsBdMQ0bHeDyqyhiVqKOWyEIqxkqzfgDayEaJPinyCRDA==
        private bool disposedValue;
        public InfluxDBDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
        {
            this.disposedValue = false;
            // You can generate an API token from the "API Tokens Tab" in the UI
           
            
           
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = databasetype;
            Category = DatasourceCategory.NOSQL;

            Dataconnection = new WebAPIDataConnection
            {
                Logger = logger,
                ErrorObject = ErrorObject

            };
            Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == datasourcename).FirstOrDefault();
            Dataconnection.ConnectionProp.Category = DatasourceCategory.NOSQL;
            Dataconnection.ConnectionProp.DatabaseType = DataSourceType.InfluxDB;
           
            CurrentDatabase = Dataconnection.ConnectionProp.Database;
            if(Dataconnection.ConnectionProp.Url.Length > 0)
            {
                url = Dataconnection.ConnectionProp.Url;
            }
            if(Dataconnection.ConnectionProp.Port > 0)
            {
                port = Dataconnection.ConnectionProp.Port;
            }
            if(Dataconnection.ConnectionProp.KeyToken.Length > 0    )
            {
                keyToken = Dataconnection.ConnectionProp.KeyToken;
            }
            else
            {
                if (Environment.GetEnvironmentVariable("INFLUX_TOKEN") != null)
                {
                    url = Environment.GetEnvironmentVariable("INFLUX_TOKEN")!;
                }
                else
                {
                    keyToken = "MH8XkA5B_dDp99-tEurjOsYTU8tuBNu7bigSGg77YfsBdMQ0bHeDyqyhiVqKOWyEIqxkqzfgDayEaJPinyCRDA==";
                }
            }
            if(Dataconnection.ConnectionProp.SchemaName.Length > 0    )
            {
                org = Dataconnection.ConnectionProp.SchemaName;
            }
            if (CurrentDatabase != null)
            {
                if (CurrentDatabase.Length > 0)
                {
                    bucket = CurrentDatabase;
                     
                    _client = new InfluxDBClient($"{url}:{port}", keyToken);
                    GetEntitesList();
                }
            }
            GuidID=Guid.NewGuid().ToString();   
        }
        public string bucket { get; set; }
        public string org { get; set; }
        public string url { get; set; } = "http://localhost";
        public int port { get; set;         } = 8086;
        public string keyToken { get; set; }
        public string CurrentDatabase { get; set; } 
        public InfluxDBClient _client { get; set; }
        public string ColumnDelimiter { get ; set ; }
        public string ParameterDelimiter { get ; set ; }
        public string GuidID { get ; set ; }
        public DataSourceType DatasourceType { get; set; } = DataSourceType.InfluxDB;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.NOSQL;
        public IDataConnection Dataconnection { get ; set ; }
        public string DatasourceName { get ; set ; }
        public IErrorsInfo ErrorObject { get ; set ; }
        public string Id { get ; set ; }
        public IDMLogger Logger { get ; set ; }
        public List<string> EntitiesNames { get; set; } = new List<string>();
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();  
        public IDMEEditor DMEEditor { get ; set ; }
        public ConnectionState ConnectionStatus { get ; set ; }

        public event EventHandler<PassedArgs> PassEvent;

        public IErrorsInfo BeginTransaction(PassedArgs args)
        {
            throw new NotImplementedException();
        }

        public bool CheckEntityExist(string EntityName)
        {
            throw new NotImplementedException();
        }

        public ConnectionState Closeconnection()
        {
            try
            {
                _client.Dispose();
                ConnectionStatus = ConnectionState.Closed;
                Logger.WriteLog("Connection to InfluxDB closed successfully.");
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                Logger.WriteLog($"Failed to close connection: {ex.Message}");
            }
            return ConnectionStatus;
        }
        public ConnectionState Openconnection()
        {
            try
            {
                if (_client == null)
                {
                    _client = InfluxDBClientFactory.Create(url, keyToken);
                }
                // Test the connection by querying some basic data
                var health = _client.HealthAsync().Result;
                if (health.Status == HealthCheck.StatusEnum.Pass)
                {
                    ConnectionStatus = ConnectionState.Open;
                    Logger.WriteLog("Connected successfully to InfluxDB.");
                }
                else
                {
                    ConnectionStatus = ConnectionState.Broken;
                    Logger.WriteLog("Failed to connect to InfluxDB.");
                }
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                Logger.WriteLog($"Failed to open connection: {ex.Message}");
            }
            return ConnectionStatus;
        }
        public IErrorsInfo Commit(PassedArgs args)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            throw new NotImplementedException();
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo EndTransaction(PassedArgs args)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo ExecuteSql(string sql)
        {
            throw new NotImplementedException();
        }

        public List<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            throw new NotImplementedException();
        }

        public List<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            throw new NotImplementedException();
        }

        public  List<string> GetEntitesList()
        {
            List<string> entities = new List<string>();
            try
            {
                if (_client == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection(); // Ensure the database is connected
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    var query = $"import \"influxdata/influxdb/schema\"\n" +
                                $"schema.measurements(bucket: \"{bucket}\")";

                    // Async query execution
                    var fluxTables =  _client.GetQueryApi().QueryAsync(query, org).Result;

                    // Processing results
                    foreach (var fluxTable in fluxTables)
                    {
                        foreach (var fluxRecord in fluxTable.Records)
                        {
                            entities.Add(fluxRecord.GetValueByKey("_value").ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error in GetEntitesList: {ex.Message}");
                // Adjusting the connection status if there is an error
                ConnectionStatus = ConnectionState.Broken;
            }
            return entities;
        }


        public object GetEntity(string EntityName, List<AppFilter> filters)
        {
            List<Dictionary<string, object>> results = new List<Dictionary<string, object>>();
            try
            {
                if (_client == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection(); // Ensure the database is connected
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    var query = new System.Text.StringBuilder();
                    query.AppendLine($"from(bucket: \"{bucket}\")");

                    // Determine the range start from filters or default to 30 days
                    string timeFilter = FindTimeFilter(filters) ?? "-30d";
                    query.AppendLine($"  |> range(start: {timeFilter})"); // Using dynamic or default time range

                    query.AppendLine($"  |> filter(fn: (r) => r._measurement == \"{EntityName}\")");

                    // Applying other filters from AppFilter list
                    foreach (var filter in filters.Where(f => f.FieldName.ToLower() != "time"))
                    {
                        query.AppendLine($"  |> filter(fn: (r) => r[\"{filter.FieldName}\"] {ConvertOperator(filter.Operator)} {PrepareValue(filter.FilterValue, filter.valueType)})");
                    }

                    var fluxQuery = query.ToString();
                    var fluxTables =  _client.GetQueryApi().QueryAsync(fluxQuery, org).Result;

                    // Processing results
                    foreach (var fluxTable in fluxTables)
                    {
                        foreach (var fluxRecord in fluxTable.Records)
                        {
                            var recordDict = new Dictionary<string, object>();
                            foreach (var property in fluxRecord.Values)
                            {
                                recordDict[property.Key] = property.Value;
                            }
                            results.Add(recordDict);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error in GetEntity: {ex.Message}");
                ConnectionStatus = ConnectionState.Broken;
            }
            return results;
        }
        private string FindTimeFilter(List<AppFilter> filters)
        {
            // Look for a time filter in the provided list
            var timeFilter = filters.FirstOrDefault(f => f.FieldName.ToLower() == "time");
            if (timeFilter != null)
            {
                // Here you might need to parse and adjust the format of the time value if necessary
                return timeFilter.FilterValue;
            }
            return null;
        }
        private string ConvertOperator(string operatorSymbol)
        {
            // Convert commonly used SQL-like operators to Flux-compatible operators
            switch (operatorSymbol)
            {
                case "==": return "==";
                case ">": return ">";
                case "<": return "<";
                case ">=": return ">=";
                case "<=": return "<=";
                case "!=": return "!=";
                default: return "=="; // Default case could be dangerous; handle accordingly
            }
        }

        private string PrepareValue(string value, string type)
        {
            // Prepare the value based on the expected type for Flux query
            switch (type)
            {
                case "string": return $"\"{value}\"";
                case "int":
                case "float":
                case "double":
                default: return value;
            }
        }

        public object GetEntity(string EntityName, List<AppFilter> filters, int pageNumber, int pageSize)
        {
            List<Dictionary<string, object>> results = new List<Dictionary<string, object>>();
            try
            {
                if (_client == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection(); // Ensure the database is connected
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    var timeFilter = filters.FirstOrDefault(f => f.FieldName.ToLower() == "time");
                    string timeRange = timeFilter != null ? timeFilter.FilterValue : "-30d"; // Default to last 30 days if no time filter is provided

                    int offset = (pageNumber - 1) * pageSize; // Calculate the offset
                    var query = new System.Text.StringBuilder();
                    query.AppendLine($"from(bucket: \"{bucket}\")");
                    query.AppendLine($"  |> range(start: {timeRange})"); // Dynamically set time range
                    query.AppendLine($"  |> filter(fn: (r) => r._measurement == \"{EntityName}\")");

                    // Applying other filters from AppFilter list
                    foreach (var filter in filters.Where(f => f.FieldName.ToLower() != "time"))
                    {
                        query.AppendLine($"  |> filter(fn: (r) => r[\"{filter.FieldName}\"] {ConvertOperator(filter.Operator)} {PrepareValue(filter.FilterValue, filter.valueType)})");
                    }

                    query.AppendLine($"  |> limit(n: {pageSize}, offset: {offset})");

                    var fluxQuery = query.ToString();
                    var fluxTables = _client.GetQueryApi().QueryAsync(fluxQuery, org).Result;

                    // Processing results
                    foreach (var fluxTable in fluxTables)
                    {
                        foreach (var fluxRecord in fluxTable.Records)
                        {
                            var recordDict = new Dictionary<string, object>();
                            foreach (var property in fluxRecord.Values)
                            {
                                recordDict.Add(property.Key, property.Value);
                            }
                            results.Add(recordDict);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error in GetEntity with pagination and dynamic time range: {ex.Message}");
                ConnectionStatus = ConnectionState.Broken;
            }
            return results;
        }

        public Task<object> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
             return Task.Run(() => GetEntity(EntityName, Filter));
        }

        public List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            throw new NotImplementedException();
        }

        public int GetEntityIdx(string entityName)
        {
            throw new NotImplementedException();
        }

        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            throw new NotImplementedException();
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            throw new NotImplementedException();
        }

        public Type GetEntityType(string EntityName)
        {
            throw new NotImplementedException();
        }

        public double GetScalar(string query)
        {
            throw new NotImplementedException();
        }

        public Task<double> GetScalarAsync(string query)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            throw new NotImplementedException();
        }

      

        public object RunQuery(string qrystr)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            throw new NotImplementedException();
        }

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
        // ~InfluxDBDataSource()
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
