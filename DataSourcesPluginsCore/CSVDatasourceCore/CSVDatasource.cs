using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
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
using System.Text.RegularExpressions;
using System.Globalization;
using System.ComponentModel;

namespace TheTechIdea.Beep.FileManager
{
    [AddinAttribute(Category = DatasourceCategory.FILE, DatasourceType = DataSourceType.CSV, FileType = "csv")]
    public class CSVDatasource : IDataSource
    {
        public string GuidID { get; set; }
        public event EventHandler<PassedArgs> PassEvent;
        public string Id { get; set; }
        public string DatasourceName { get; set; }
        public DataSourceType DatasourceType { get; set; } = DataSourceType.CSV;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.FILE;
        public ConnectionState ConnectionStatus { get; set; }
        public List<string> EntitiesNames { get; set; } = new List<string>();
        public IDMEEditor DMEEditor { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public IDMLogger Logger { get; set; }
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
        public bool HeaderExist { get; set; } = true;
        public IDataConnection Dataconnection { get; set; }
        public virtual string ColumnDelimiter { get; set; } = ",";
        public virtual string ParameterDelimiter { get; set; } = ":";

        private string _filePath;
        private string _fileName;
        private string _fullPath;
        private char _delimiter = ',';
        private DataTable _fileData;
        private bool _isFileRead = false;

        EntityStructure DataStruct = null;
        Type enttype = null;
        bool ObjectsCreated = false;
        string lastentityname = null;

        private void SetObjects(string Entityname)
        {
            if (!ObjectsCreated || Entityname != lastentityname)
            {
                DataStruct = GetEntityStructure(Entityname, false);
                enttype = GetEntityType(Entityname);
                ObjectsCreated = true;
                lastentityname = Entityname;
            }
        }

        public CSVDatasource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType pDatasourceType, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = pDatasourceType;
            Category = DatasourceCategory.FILE;

            Dataconnection = new FileConnection(DMEEditor)
            {
                Logger = logger,
                ErrorObject = ErrorObject
            };

            Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.FileName == datasourcename).FirstOrDefault();

            if (Dataconnection.ConnectionProp != null)
            {
                _fileName = Dataconnection.ConnectionProp.FileName;
                _filePath = Dataconnection.ConnectionProp.FilePath;
                _fullPath = Path.Combine(_filePath, _fileName);
                if (!string.IsNullOrEmpty(Dataconnection.ConnectionProp.Delimiter))
                {
                    _delimiter = Dataconnection.ConnectionProp.Delimiter[0];
                }
            }

            GuidID = Guid.NewGuid().ToString();
        }

        public int GetEntityIdx(string entityName)
        {
            if (Entities != null && Entities.Count > 0)
            {
                return Entities.FindIndex(p => p.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase) ||
                                              (p.DatasourceEntityName != null && p.DatasourceEntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase)));
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
                if (File.Exists(_fullPath))
                {
                    ConnectionStatus = ConnectionState.Open;
                    _isFileRead = false;
                    GetEntitesList();
                }
                else
                {
                    ConnectionStatus = ConnectionState.Closed;
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = $"File not found: {_fullPath}";
                }
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Closed;
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error opening CSV file: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return ConnectionStatus;
        }

        public ConnectionState Closeconnection()
        {
            ConnectionStatus = ConnectionState.Closed;
            _fileData = null;
            _isFileRead = false;
            return ConnectionStatus;
        }

        public IEnumerable<string> GetEntitesList()
        {
            try
            {
                if (ConnectionStatus == ConnectionState.Open && !_isFileRead)
                {
                    if (File.Exists(_fullPath))
                    {
                        string entityName = Path.GetFileNameWithoutExtension(_fileName);
                        EntitiesNames.Clear();
                        EntitiesNames.Add(entityName);

                        if (!Entities.Any(e => e.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase)))
                        {
                            GetEntityStructure(entityName, true);
                        }

                        _isFileRead = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error retrieving entities list: {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
            }

            return EntitiesNames;
        }

        private DataTable ReadCSVFile(string entityName, int fromLine = 0, int toLine = int.MaxValue)
        {
            DataTable dt = new DataTable(entityName);
            try
            {
                if (!File.Exists(_fullPath))
                {
                    return dt;
                }

                using (var reader = new StreamReader(_fullPath))
                {
                    string line;
                    int lineNumber = 0;
                    bool firstLine = true;

                    while ((line = reader.ReadLine()) != null && lineNumber < toLine)
                    {
                        if (lineNumber >= fromLine)
                        {
                            var values = ParseCSVLine(line);

                            if (firstLine && HeaderExist)
                            {
                                foreach (var header in values)
                                {
                                    string cleanHeader = CleanColumnName(header ?? $"Column{dt.Columns.Count + 1}");
                                    dt.Columns.Add(cleanHeader, typeof(string));
                                }
                                firstLine = false;
                            }
                            else if (firstLine && !HeaderExist)
                            {
                                // Create default column names
                                for (int i = 0; i < values.Length; i++)
                                {
                                    dt.Columns.Add($"Column{i + 1}", typeof(string));
                                }
                                dt.Rows.Add(values);
                                firstLine = false;
                            }
                            else
                            {
                                if (values.Length == dt.Columns.Count)
                                {
                                    dt.Rows.Add(values);
                                }
                                else if (values.Length < dt.Columns.Count)
                                {
                                    // Pad with empty values
                                    var paddedValues = new object[dt.Columns.Count];
                                    Array.Copy(values, paddedValues, values.Length);
                                    dt.Rows.Add(paddedValues);
                                }
                            }
                        }
                        lineNumber++;
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error reading CSV file: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }

            return dt;
        }

        private string[] ParseCSVLine(string line)
        {
            List<string> values = new List<string>();
            bool inQuotes = false;
            StringBuilder currentValue = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        currentValue.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == _delimiter && !inQuotes)
                {
                    values.Add(currentValue.ToString());
                    currentValue.Clear();
                }
                else
                {
                    currentValue.Append(c);
                }
            }

            values.Add(currentValue.ToString());
            return values.ToArray();
        }

        private string CleanColumnName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "Column1";

            name = Regex.Replace(name, @"[^\w]", "_");
            if (char.IsDigit(name[0]))
                name = "_" + name;

            return name;
        }

        public IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    SetObjects(EntityName);
                    var entity = GetEntityStructure(EntityName, false);
                    int fromLine = entity?.StartRow ?? 0;
                    int toLine = entity?.EndRow ?? int.MaxValue;

                    if (filter != null && filter.Count > 0)
                    {
                        var fromLineFilter = filter.FirstOrDefault(f => f.FieldName.Equals("FromLine", StringComparison.OrdinalIgnoreCase));
                        if (fromLineFilter != null && int.TryParse(fromLineFilter.FilterValue, out int from))
                        {
                            fromLine = from;
                        }
                        var toLineFilter = filter.FirstOrDefault(f => f.FieldName.Equals("ToLine", StringComparison.OrdinalIgnoreCase));
                        if (toLineFilter != null && int.TryParse(toLineFilter.FilterValue, out int to))
                        {
                            toLine = to;
                        }
                    }

                    _fileData = ReadCSVFile(EntityName, fromLine, toLine);
                    
                    // Apply filters
                    if (filter != null && filter.Count > 0)
                    {
                        var dataFilters = filter.Where(f => !f.FieldName.Equals("FromLine", StringComparison.OrdinalIgnoreCase) &&
                                                           !f.FieldName.Equals("ToLine", StringComparison.OrdinalIgnoreCase));
                        if (dataFilters.Any())
                        {
                            string filterExpression = BuildFilterExpression(dataFilters);
                            if (!string.IsNullOrEmpty(filterExpression))
                            {
                                _fileData = _fileData.Select(filterExpression).CopyToDataTable();
                            }
                        }
                    }

                    // Convert to objects
                    enttype = GetEntityType(EntityName);
                    Type uowGenericType = typeof(ObservableBindingList<>).MakeGenericType(enttype);
                    object[] constructorArgs = new object[] { _fileData };
                    object uowInstance = Activator.CreateInstance(uowGenericType, constructorArgs);
                    return uowInstance as IEnumerable<object> ?? Enumerable.Empty<object>();
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                Logger?.WriteLog($"Error getting entity: {ex.Message}");
            }

            return Enumerable.Empty<object>();
        }

        private string BuildFilterExpression(IEnumerable<AppFilter> filters)
        {
            StringBuilder sb = new StringBuilder();
            bool first = true;

            foreach (var filter in filters)
            {
                if (string.IsNullOrEmpty(filter.FieldName) || string.IsNullOrEmpty(filter.Operator))
                    continue;

                if (!first)
                    sb.Append(" AND ");

                string fieldName = $"[{filter.FieldName}]";
                string op = ConvertOperator(filter.Operator);
                string value = PrepareValue(filter.FilterValue, filter.valueType);

                sb.Append($"{fieldName} {op} {value}");
                first = false;
            }

            return sb.ToString();
        }

        private string ConvertOperator(string op)
        {
            switch (op.ToUpper())
            {
                case "=": return "=";
                case "!=": return "<>";
                case ">": return ">";
                case ">=": return ">=";
                case "<": return "<";
                case "<=": return "<=";
                case "LIKE": return "LIKE";
                default: return "=";
            }
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
                    _fileData = ReadCSVFile(EntityName, 0, 100); // Read first 100 rows for schema
                    
                    EntityStructure entity = new EntityStructure
                    {
                        EntityName = EntityName,
                        DatasourceEntityName = EntityName,
                        OriginalEntityName = EntityName,
                        Fields = new List<EntityField>(),
                        StartRow = HeaderExist ? 1 : 0,
                        EndRow = int.MaxValue
                    };

                    if (_fileData != null && _fileData.Columns.Count > 0)
                    {
                        foreach (DataColumn col in _fileData.Columns)
                        {
                            var field = new EntityField
                            {
                                fieldname = CleanColumnName(col.ColumnName),
                                Originalfieldname = col.ColumnName,
                                fieldtype = InferFieldType(_fileData, col.ColumnName),
                                EntityName = EntityName,
                                AllowDBNull = true,
                                IsKey = false
                            };
                            entity.Fields.Add(field);
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

        private string InferFieldType(DataTable dt, string columnName)
        {
            if (dt == null || dt.Rows.Count == 0)
                return "System.String";

            // Sample rows to determine type
            for (int i = 0; i < Math.Min(100, dt.Rows.Count); i++)
            {
                var value = dt.Rows[i][columnName];
                if (value != null && value != DBNull.Value)
                {
                    string valStr = value.ToString();
                    if (!string.IsNullOrWhiteSpace(valStr))
                    {
                        if (long.TryParse(valStr, out _))
                            return "System.Int64";
                        if (double.TryParse(valStr, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                            return "System.Double";
                        if (DateTime.TryParse(valStr, out _))
                            return "System.DateTime";
                        if (bool.TryParse(valStr, out _))
                            return "System.Boolean";
                    }
                }
            }

            return "System.String";
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
            return EntitiesNames.Contains(EntityName, StringComparer.OrdinalIgnoreCase) ||
                   Entities.Any(e => e.EntityName.Equals(EntityName, StringComparison.OrdinalIgnoreCase));
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
                if (EntitiesNames == null)
                {
                    EntitiesNames = new List<string>();
                }
                if (!EntitiesNames.Contains(entity.EntityName))
                {
                    EntitiesNames.Add(entity.EntityName);
                }
                return true;
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error creating entity: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return false;
            }
        }

        public IErrorsInfo ExecuteSql(string sql)
        {
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = "SQL execution not supported for CSV files";
            DMEEditor?.AddLogMessage("Beep", "SQL execution not supported for CSV files", DateTime.Now, 0, null, Errors.Failed);
            return ErrorObject;
        }

        public IEnumerable<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            return new List<ChildRelation>();
        }

        public IEnumerable<object> RunQuery(string qrystr)
        {
            // CSV files don't support queries directly
            DMEEditor?.AddLogMessage("Beep", "Query execution not fully supported for CSV files", DateTime.Now, 0, null, Errors.Failed);
            return Enumerable.Empty<object>();
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Failed, Message = "Update operation not supported for CSV files. Files are read-only." };
            DMEEditor?.AddLogMessage("Beep", "Update operation not supported for CSV files", DateTime.Now, 0, null, Errors.Failed);
            return retval;
        }

        public IErrorsInfo DeleteEntity(string EntityName, object DeletedDataRow)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Failed, Message = "Delete operation not supported for CSV files. Files are read-only." };
            DMEEditor?.AddLogMessage("Beep", "Delete operation not supported for CSV files", DateTime.Now, 0, null, Errors.Failed);
            return retval;
        }

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = "Script execution not supported for CSV files";
            DMEEditor?.AddLogMessage("Beep", "Script execution not supported for CSV files", DateTime.Now, 0, null, Errors.Failed);
            return ErrorObject;
        }

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                foreach (var entity in entities)
                {
                    CreateEntityAs(entity);
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
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
                        scripts.Add(new ETLScriptDet
                        {
                            EntityName = entity.EntityName,
                            ScriptType = "CREATE",
                            ScriptText = $"# CSV entity: {entity.EntityName}\n# Schema defined in file"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in GetCreateEntityScript: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return scripts;
        }

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Failed, Message = "Insert operation not supported for CSV files. Files are read-only." };
            DMEEditor?.AddLogMessage("Beep", "Insert operation not supported for CSV files", DateTime.Now, 0, null, Errors.Failed);
            return retval;
        }

        public virtual double GetScalar(string query)
        {
            try
            {
                // For CSV, scalar queries are limited
                return 0.0;
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Fail", $"Error in executing scalar query ({ex.Message})", DateTime.Now, 0, "", Errors.Failed);
                return 0.0;
            }
        }

        public virtual Task<double> GetScalarAsync(string query)
        {
            return Task.Run(() => GetScalar(query));
        }

        #region "Dispose"
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
        #endregion
    }
}