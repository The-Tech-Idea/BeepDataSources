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
using System.Security.Cryptography;
using System.Reflection;
using InfluxDB.Client.Core.Flux.Domain;
using InfluxDB.Client.Writes;

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
            return BucketExists(EntityName);
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
        private bool BucketExists(string bucketName)
        {
            Bucket buckets = _client.GetBucketsApi().FindBucketByNameAsync(bucketName).Result;
            return buckets != null;
        }

        private void CreateBucket(string bucketName)
        {
            var bucketApi = _client.GetBucketsApi();
            var bucket = new Bucket(name: bucketName, orgID: org, retentionRules: new List<BucketRetentionRules> { new BucketRetentionRules { EverySeconds = 0 } });
            bucketApi.CreateBucketAsync(bucket);
        }
        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            foreach (EntityStructure entity in entities)
            {
                CreateEntityAs(entity);
            }
            return DMEEditor.ErrorObject;
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            try
            {
                if (_client == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection(); // Ensure the database is connected
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    // Since InfluxDB doesn't require explicit creation of measurements (entities),
                    // you might want to set up a template or ensure that a setup task or CQ is in place,
                    // or simply validate that the intended structure aligns with what's expected in queries.

                    // Example: Log or prepare for future validation tasks
                     DMEEditor.AddLogMessage("Beep",$"Preparation for '{entity.EntityName}' is complete. Ready to accept data.", DateTime.Now,-1,null, Errors.Failed);

                    // Optionally, if you're using InfluxDB 2.0, you might want to create a bucket if it doesn't exist.
                    // This could be the place where you handle such initial setup.

                    // Example check to create a bucket
                    if (!BucketExists(entity.EntityName))
                    {
                        CreateBucket(entity.EntityName);
                    }

                    return true; // Indicates the "entity" is ready to be used.
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error creating entity structure for '{entity.EntityName}': {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return false;
            }

            return false;
        }

        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok, Message = "Data deleted successfully." };

            try
            {
                if (_client == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection(); // Ensure the database is connected
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    // Assuming UploadDataRow contains a dictionary or similar structure with keys and values
                    var dataRow = UploadDataRow as Dictionary<string, object>;
                    if (dataRow == null)
                    {
                        throw new ArgumentException("UploadDataRow must be a dictionary with field and tag values.");
                    }

                    // Construct the Flux query for deletion
                    string fluxQuery = $"from(bucket: \"{bucket}\")\n" +
                                       $"|> range(start: {dataRow["startTime"]}, stop: {dataRow["endTime"]})\n" +
                                       $"|> filter(fn: (r) => r._measurement == \"{EntityName}\")\n";

                    foreach (var item in dataRow)
                    {
                        if (item.Key != "startTime" && item.Key != "endTime")
                        {
                            fluxQuery += $"|> filter(fn: (r) => r[\"{item.Key}\"] == \"{item.Value}\")\n";
                        }
                    }

                    fluxQuery += "|> delete()";

                    // Execute the deletion
                    _client.GetQueryApi().QueryAsync(fluxQuery, org).Wait(); // This assumes the client supports a delete operation in this manner, adjust based on actual SDK capabilities
                }
            }
            catch (Exception ex)
            {
                retval.Flag = Errors.Failed;
                retval.Message = $"Failed to delete entity: {ex.Message}";
            }

            return retval;
        }

        public IErrorsInfo EndTransaction(PassedArgs args)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo ExecuteSql(string sql)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok, Message = "Query executed successfully." };

            try
            {
                if (_client == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection(); // Ensure the database is connected
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    // Executing the Flux query
                    var queryApi = _client.GetQueryApi();
                    List<FluxTable> result = queryApi.QueryAsync(sql, org).Result;

                    // Optionally process the results or log them
                    foreach (var table in result)
                    {
                        foreach (var record in table.Records)
                        {
                            // Process each record as needed
                            DMEEditor.AddLogMessage("Beep", $"Data: {record.Values}", DateTime.Now, -1, null, Errors.Failed);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                retval.Flag = Errors.Failed;
                retval.Message = $"Error executing query: {ex.Message}";
                DMEEditor.AddLogMessage("Beep", $"Error in ExecuteQuery: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return retval;
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
                DMEEditor.AddLogMessage("Beep", $"Error in GetEntity: {ex.Message}",DateTime.Now, -1, null, Errors.Failed);
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
            if (Entities.Count > 0)
            {
                return Entities.FindIndex(p => p.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase) || p.DatasourceEntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                return -1;
            }
        }

        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            EntityStructure structure = new EntityStructure();
            try
            {
                if (refresh || !Entities.Any(e => e.EntityName == EntityName))
                {
                    if (_client == null || ConnectionStatus != ConnectionState.Open)
                    {
                        Openconnection();
                    }

                    if (ConnectionStatus == ConnectionState.Open)
                    {
                        // Flux query to retrieve field and tag keys separately
                        string fluxQueryFields = $@"
                    from(bucket: ""{bucket}"")
                    |> range(start: -1d)  // Limit range to recent data
                    |> filter(fn: (r) => r._measurement == ""{EntityName}"")
                    |> keys()
                    |> keep(columns: [""_value""])
                    |> distinct()
                ";

                        string fluxQueryTags = $@"
                    from(bucket: ""{bucket}"")
                    |> range(start: -1d)
                    |> filter(fn: (r) => r._measurement == ""{EntityName}"")
                    |> keys()
                    |> keep(columns: [""_field""])
                    |> distinct()
                ";

                        // Execute the queries asynchronously
                        var fieldsTask = _client.GetQueryApi().QueryAsync(fluxQueryFields, org);
                        var tagsTask = _client.GetQueryApi().QueryAsync(fluxQueryTags, org);

                        var fieldsResult =  fieldsTask.Result;
                        var tagsResult =  tagsTask.Result;

                        structure.EntityName = EntityName;
                        structure.Fields = new List<EntityField>();

                        // Process fields
                        foreach (var record in fieldsResult.SelectMany(table => table.Records))
                        {
                            structure.Fields.Add(new EntityField
                            {
                                fieldname = record.GetValueByKey("_value").ToString(),
                                fieldtype = "field"
                            });
                        }

                        // Process tags
                        foreach (var record in tagsResult.SelectMany(table => table.Records))
                        {
                            structure.Fields.Add(new EntityField
                            {
                                fieldname = record.GetValueByKey("_field").ToString(),
                                fieldtype = "tag"
                            });
                        }

                        // Cache the structure if needed
                        if (!Entities.Any(e => e.EntityName == EntityName))
                        {
                            Entities.Add(structure);
                        }
                    }
                }
                else
                {
                    structure = Entities.First(e => e.EntityName == EntityName);
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error in GetEntityStructure: {ex.Message}");
                ConnectionStatus = ConnectionState.Broken;
            }
            return structure;
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            string EntityName = fnd.EntityName;
            return GetEntityStructure(EntityName, refresh);
        }

        public Type GetEntityType(string EntityName)
        {
            ErrorsInfo retval = new ErrorsInfo();
            retval.Flag = Errors.Ok;
            retval.Message = "Get Entity Type  ";
            Type result = null;
            try
            {
                EntityStructure x = GetEntityStructure(EntityName,false);
                DMTypeBuilder.CreateNewObject(DMEEditor, "Beep." + DatasourceName, EntityName, x.Fields);
                result = DMTypeBuilder.myType;
            }
            catch (Exception ex)
            {
                string methodName = MethodBase.GetCurrentMethod().Name;
                retval.Flag = Errors.Failed;
                retval.Message = ex.Message;
                result = null;
                DMEEditor.AddLogMessage("Beep", $"error in {methodName} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return result;
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
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok, Message = "Data inserted successfully." };
            string methodName = MethodBase.GetCurrentMethod().Name;
            try
            {
               
                if (_client == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection(); // Ensure the database is connected
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    var writeApi = _client.GetWriteApi();

                    // Assuming InsertedData is a dictionary for simplicity. You'll need to adjust this based on the actual data structure.
                    if (InsertedData is Dictionary<string, object> data)
                    {
                        var point = PointData
                                    .Measurement(EntityName)
                                    .Timestamp(DateTime.UtcNow, WritePrecision.Ns);

                        foreach (var item in data)
                        {
                            if (item.Value is double || item.Value is int || item.Value is float)
                            {
                                point = point.Field(item.Key, Convert.ToDouble(item.Value));
                            }
                            else if (item.Value is string)
                            {
                                point = point.Tag(item.Key, (string)item.Value);
                            }
                            else
                            {
                                // Handle other data types or throw an exception if the type is not supported
                                
                                DMEEditor.AddLogMessage("Beep", $"error in {methodName} Unsupported data type for InfluxDB field or tag.\"", DateTime.Now, -1, null, Errors.Failed);
                            }
                        }

                        writeApi.WritePoint(point,bucket, org);
                    }
                    else
                    {
                        DMEEditor.AddLogMessage("Beep", $"error in {methodName} InsertedData must be a Dictionary<string, object>.", DateTime.Now, -1, null, Errors.Failed);
                        
                    }
                }
            }
            catch (Exception ex)
            {
                retval.Flag = Errors.Failed;
                retval.Message = $"Error inserting data into {EntityName}: {ex.Message}";
                DMEEditor.AddLogMessage("Beep", $"error in {methodName} InsertedData must be a Dictionary<string, object>. {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return retval;
        }

      

        public object RunQuery(string qrystr)
        {
            List<Dictionary<string, object>> results = new List<Dictionary<string, object>>();
            string methodName = MethodBase.GetCurrentMethod().Name;
            try
            {
                if (_client == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection(); // Ensure the database is connected
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    var queryApi = _client.GetQueryApi();
                    var tables =  queryApi.QueryAsync(qrystr, org).Result;

                    // Processing the results
                    foreach (var table in tables)
                    {
                        foreach (var record in table.Records)
                        {
                            var resultRow = new Dictionary<string, object>();
                            foreach (var property in record.Values)
                            {
                                resultRow[property.Key] = property.Value;
                            }
                            results.Add(resultRow);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handling exceptions such as connection issues, query syntax errors, etc.
                
                DMEEditor.AddLogMessage("Beep", $"Error Running Query in {methodName} : {ex.Message}.", DateTime.Now, -1, null, Errors.Failed);
                return null;
            }

            return results;
        }

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok, Message = "Data batch inserted successfully." };
            string methodName = MethodBase.GetCurrentMethod().Name;
            try
            {
                if (_client == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection(); // Ensure the database is connected
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    var writeApi = _client.GetWriteApi();

                    // Assuming UploadData is a list of dictionaries
                    if (UploadData is List<Dictionary<string, object>> dataList)
                    {
                        List<PointData> points = new List<PointData>();

                        foreach (var dataRow in dataList)
                        {
                            if (!dataRow.ContainsKey("Timestamp") || !dataRow.ContainsKey("Tags") || !dataRow.ContainsKey("Fields"))
                            {
                                DMEEditor.AddLogMessage("Beep", $"error in {methodName} Each data row must include 'Timestamp', 'Tags', and 'Fields' ", DateTime.Now, -1, null, Errors.Failed);
                                
                            }

                            var timestamp = (DateTime)dataRow["Timestamp"];
                            var tags = (Dictionary<string, string>)dataRow["Tags"];
                            var fields = (Dictionary<string, object>)dataRow["Fields"];

                            var point = PointData
                                        .Measurement(EntityName)
                                        .Timestamp(timestamp, WritePrecision.Ns);

                            // Adding tags
                            foreach (var tag in tags)
                            {
                                point = point.Tag(tag.Key, tag.Value);
                            }

                            // Adding fields
                            foreach (var field in fields)
                            {
                                point = point.Field(field.Key, field.Value);
                            }

                            points.Add(point);
                        }

                        // Writing all points in a batch
                        writeApi.WritePoints(points, bucket, org);

                        // Optionally update progress
                        progress?.Report(new PassedArgs { Messege = "Batch insert completed successfully.",ParameterString1 = "Completed" });
                    }
                    else
                    {
                        DMEEditor.AddLogMessage("Beep", $"error in {methodName} UploadData must be a List<Dictionary<string, object>>. ", DateTime.Now, -1, null, Errors.Failed);
                        
                    }
                }
            }
            catch (Exception ex)
            {
                retval.Flag = Errors.Failed;
                retval.Message = $"Error in batch insertion: {ex.Message}";
                progress?.Report(new PassedArgs { Messege = $"Error: {ex.Message}", ParameterString1 = "Failed" });
                DMEEditor.AddLogMessage("Beep", $"error in {methodName} Error in batch insertion. {ex.Message} ", DateTime.Now, -1, null, Errors.Failed);
            }

            return retval;
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok, Message = "Data updated successfully." };
            string methodName = MethodBase.GetCurrentMethod().Name;
            try
            {
                if (_client == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection(); // Ensure the database is connected
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    var writeApi = _client.GetWriteApi();

                    // Check if UploadDataRow is in an expected format, for example, Dictionary
                    if (UploadDataRow is Dictionary<string, object> dataRow)
                    {
                        if (!dataRow.ContainsKey("Timestamp") || !dataRow.ContainsKey("Tags") || !dataRow.ContainsKey("Fields"))
                        {
                            DMEEditor.AddLogMessage("Beep", $"error in {methodName} UploadDataRow must include 'Timestamp', 'Tags', and 'Fields'", DateTime.Now, -1, null, Errors.Failed);
                            
                        }

                        var timestamp = (DateTime)dataRow["Timestamp"];
                        var tags = (Dictionary<string, string>)dataRow["Tags"];
                        var fields = (Dictionary<string, object>)dataRow["Fields"];

                        var point = PointData
                                    .Measurement(EntityName)
                                    .Timestamp(timestamp, WritePrecision.Ns);

                        // Adding tags
                        foreach (var tag in tags)
                        {
                            point = point.Tag(tag.Key, tag.Value);
                        }

                        // Adding fields
                        foreach (var field in fields)
                        {
                            point = point.Field(field.Key, field.Value);
                        }

                        // Writing the point to InfluxDB, which will overwrite the existing point with the same timestamp and tags
                        writeApi.WritePoint(point,bucket, org );
                    }
                    else
                    {
                        DMEEditor.AddLogMessage("Beep", $"error in {methodName} InsertedData must be a Dictionary<string, object>.", DateTime.Now, -1, null, Errors.Failed);
                    }
                }
            }
            catch (Exception ex)
            {
                retval.Flag = Errors.Failed;
                retval.Message = $"Error updating entity in InfluxDB: {ex.Message}";
                DMEEditor.AddLogMessage("Beep", $"Error updating entity in InfluxDB: {ex.Message}.", DateTime.Now, -1, null, Errors.Failed);
            }

            return retval;
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
