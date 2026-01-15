using System.Data;
using System.Linq;
using System.Collections.Generic;

using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Vis;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.FileManager
{
    /// <summary>
    /// Enumeration for supported file types
    /// </summary>
    internal enum FileType
    {
        Csv,
        Xlsx,
        Xls,
        Unknown
    }

    [AddinAttribute(Category = DatasourceCategory.FILE, DatasourceType = DataSourceType.CSV|DataSourceType.Xls,FileType = "xls,xlsx") ]
    public partial class TxtXlsCSVFileSource : IDataSource

    {
        public string GuidID { get; set; } 
        public event EventHandler<PassedArgs> PassEvent;
        public string Id { get; set; }
        public string DatasourceName { get; set; }
        public DataSourceType DatasourceType { get; set; } = DataSourceType.Xls;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.FILE;
        ConnectionState pConnectionStatus;
        public ConnectionState ConnectionStatus { get { return Dataconnection.ConnectionStatus; }  set { pConnectionStatus = value; }  }
        public List<string> EntitiesNames { get; set; } = new List<string>();
        public IDMEEditor DMEEditor { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public IDMLogger Logger { get; set; }
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>(); 
        public bool HeaderExist { get; set; }
        public IDataConnection Dataconnection { get; set ; }
        public virtual string ColumnDelimiter { get; set; } = "''";
        public virtual string ParameterDelimiter { get; set; } = ":";
        string FileName;
        string FilePath;
        string CombineFilePath;
        char Delimiter;
        public DataTable FileData { get; set; }
        bool IsFileRead = false;
        private TxtXlsCSVFileSourceHelper _helper;

        #region "Shared Utility Methods"

        /// <summary>
        /// Detects the file type based on file extension
        /// </summary>
        private FileType GetFileType(string filePath = null)
        {
            string path = filePath ?? CombineFilePath ?? FileName;
            string ext = (Dataconnection?.ConnectionProp?.Ext ?? Path.GetExtension(path))
                .Replace(".", "").ToLower();
            
            return ext switch
            {
                "csv" => FileType.Csv,
                "xlsx" => FileType.Xlsx,
                "xls" => FileType.Xls,
                _ => FileType.Unknown
            };
        }

        /// <summary>
        /// Ensures entities are loaded and validates entity exists
        /// </summary>
        private void EnsureEntitiesLoaded(string entityName = null)
        {
            try
            {
                if (Entities == null || Entities.Count == 0)
                {
                    IsFileRead = false;
                    GetSheets();
                }
                
                if (Entities.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"No entities found in {Path.GetFileName(CombineFilePath ?? FileName)}. " +
                        $"File may be empty or connection not opened.");
                }
                
                if (!string.IsNullOrEmpty(entityName))
                {
                    int idx = GetEntityIdx(entityName);
                    if (idx < 0)
                        throw new ArgumentException($"Entity '{entityName}' not found. Available entities: {string.Join(", ", EntitiesNames)}");
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error ensuring entities loaded: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Extracts field value from various source types (DataRow, Dictionary, POCO)
        /// </summary>
        private object ExtractFieldValue(object source, EntityField field)
        {
            if (source == null || field == null) return null;
            
            try
            {
                // Try DataRow first
                if (source is DataRow dr)
                {
                    string colName = null;
                    if (dr.Table.Columns.Contains(field.Originalfieldname))
                        colName = field.Originalfieldname;
                    else if (dr.Table.Columns.Contains(field.FieldName))
                        colName = field.FieldName;
                    
                    return colName != null ? dr[colName] : null;
                }
                
                // Try IDictionary
                if (source is IDictionary<string, object> dict)
                {
                    if (dict.ContainsKey(field.FieldName))
                        return dict[field.FieldName];
                    if (dict.ContainsKey(field.Originalfieldname))
                        return dict[field.Originalfieldname];
                    return null;
                }
                
                // Try POCO via reflection
                var prop = source.GetType().GetProperty(field.FieldName) 
                    ?? source.GetType().GetProperty(field.Originalfieldname);
                return prop?.GetValue(source);
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error extracting field value '{field.FieldName}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Extracts filter parameters for row filtering (FromLine, ToLine) from filter list
        /// </summary>
        private (int fromLine, int toLine) ExtractLineFilterParameters(List<AppFilter> filters, int defaultFromLine, int defaultToLine)
        {
            int fromLine = defaultFromLine;
            int toLine = defaultToLine;

            if (filters == null || filters.Count == 0)
                return (fromLine, toLine);

            try
            {
                var fromLineFilter = filters.FirstOrDefault(p => p.FieldName.Equals("FromLine", StringComparison.InvariantCultureIgnoreCase));
                if (fromLineFilter != null && int.TryParse(fromLineFilter.FilterValue, out int from))
                    fromLine = from;

                var toLineFilter = filters.FirstOrDefault(p => p.FieldName.Equals("ToLine", StringComparison.InvariantCultureIgnoreCase));
                if (toLineFilter != null && int.TryParse(toLineFilter.FilterValue, out int to))
                    toLine = to;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error extracting line filter parameters: {ex.Message}");
            }

            return (fromLine, toLine);
        }

        /// <summary>
        /// Builds a DataTable filter query string from AppFilter list
        /// </summary>
        private string BuildDataTableFilterQuery(List<AppFilter> filters)
        {
            if (filters == null || filters.Count == 0)
                return "";

            var sb = new StringBuilder();
            
            try
            {
                var dataFilters = filters.Where(p => 
                    !string.IsNullOrEmpty(p.FilterValue) && 
                    !string.IsNullOrWhiteSpace(p.FilterValue) && 
                    !string.IsNullOrEmpty(p.Operator) && 
                    !string.IsNullOrWhiteSpace(p.Operator) && 
                    !p.FieldName.Equals("ToLine", StringComparison.InvariantCultureIgnoreCase) && 
                    !p.FieldName.Equals("FromLine", StringComparison.InvariantCultureIgnoreCase)).ToList();

                foreach (var filter in dataFilters)
                {
                    if (filter.Operator.Equals("between", StringComparison.OrdinalIgnoreCase))
                    {
                        if (filter.valueType == "System.DateTime")
                        {
                            sb.AppendLine($"[{filter.FieldName}] {filter.Operator} '{DateTime.Parse(filter.FilterValue)}' and '{DateTime.Parse(filter.FilterValue1)}'");
                        }
                        else
                        {
                            sb.AppendLine($"[{filter.FieldName}] {filter.Operator} {filter.FilterValue} and {filter.FilterValue1}");
                        }
                    }
                    else
                    {
                        if (filter.valueType == "System.String")
                        {
                            sb.AppendLine($"[{filter.FieldName}] {filter.Operator} '{filter.FilterValue}'");
                        }
                        else
                        {
                            sb.AppendLine($"[{filter.FieldName}] {filter.Operator} {filter.FilterValue}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error building filter query: {ex.Message}");
            }

            return sb.ToString();
        }

        #endregion
        #region "Insert or Update or Delete Objects"
        EntityStructure DataStruct = null;
        IDbCommand command = null;
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
                // Assuming you have a database connection and command objects.

                //using (var command = GetDataCommand())
                //{
                //    command.CommandText = query;
                //    var result = command.ExecuteScalar();

                //    // Check if the result is not null and can be converted to a double.
                //    if (result != null && double.TryParse(result.ToString(), out double value))
                //    {
                //        return value;
                //    }
                //}


                // If the query executed successfully but didn't return a valid double, you can handle it here.
                // You might want to log an error or throw an exception as needed.
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error in executing scalar query ({ex.Message})", DateTime.Now, 0, "", Errors.Failed);
                try { ErrorObject.Flag = Errors.Failed; ErrorObject.Ex = ex; } catch { }
                Logger?.WriteLog($"Error in GetScalar: {ex.Message}");
            }

            // Return a default value or throw an exception if the query failed.
            return 0.0; // You can change this default value as needed.
        }
        public TxtXlsCSVFileSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType pDatasourceType ,  IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = pDatasourceType;
            _helper = new TxtXlsCSVFileSourceHelper(logger);
            Dataconnection = new FileConnection(DMEEditor)
            {
                Logger = logger,
                ErrorObject = ErrorObject,
            };
            Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.FileName == datasourcename).FirstOrDefault();
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            Category = DatasourceCategory.FILE;
            if (Dataconnection.ConnectionProp != null)
            {
                FileName = Dataconnection.ConnectionProp.FileName;
                FilePath = Dataconnection.ConnectionProp.FilePath;
               
            }
        }

        /// <summary>
        /// Helper method to handle transaction operations with error logging
        /// </summary>
        private IErrorsInfo HandleTransaction(string operationName)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                // Transaction operations not yet implemented for file-based datasource
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error in {operationName}: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                Logger?.WriteLog($"Error in {operationName}: {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
            }
            return DMEEditor.ErrorObject;
        }

        public virtual IErrorsInfo BeginTransaction(PassedArgs args) => HandleTransaction("BeginTransaction");

        public virtual IErrorsInfo EndTransaction(PassedArgs args) => HandleTransaction("EndTransaction");

        public virtual IErrorsInfo Commit(PassedArgs args) => HandleTransaction("Commit");

        public int GetEntityIdx(string entityName)
        {
            int i = -1;
            if (Entities.Count > 0)
            {
                i = Entities.FindIndex(p => p.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
                if (i < 0)
                {
                    i=Entities.FindIndex(p => p.DatasourceEntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
                }
                if (i < 0)
                {
                    i = Entities.FindIndex(p => p.OriginalEntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
                }
                return i;
            }
            else
            {
                return -1;
            }


        }
        public ConnectionState Openconnection()
        {
            ConnectionStatus = Dataconnection.OpenConnection();
             FilePath = Dataconnection.ConnectionProp.FilePath;
            if (ConnectionStatus == ConnectionState.Open)
            {
                GetSheets();
                CombineFilePath = Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName);
            }

            return ConnectionStatus;
          
        }
        public ConnectionState Closeconnection()
        {
           return ConnectionStatus = ConnectionState.Closed;
        }
        public IEnumerable<string> GetEntitesList()
        {
            ErrorObject.Flag = Errors.Ok;

            try
            {


                if (GetFileState() == ConnectionState.Open && !IsFileRead)
                {
                    GetSheets();
                }

            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Unsuccessfully Retrieve Entites list {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
            }

            return EntitiesNames;



        }
        public EntityStructure GetEntityDataType(string EntityName)
        {
            EntityStructure ent = null;
            if (Entities != null)
            {
                if (Entities.Count() == 0)
                {
                    GetEntitesList();
                }
                int idx = GetEntityIdx(EntityName);
                if (idx >= 0 && idx < Entities.Count)
                {
                    ent = Entities[idx];
                }
            }

            return ent;
        }
        public Type GetEntityType(string EntityName)
        {
            if (GetFileState() == ConnectionState.Open)
            {
                // Use consolidated entity initialization check
                EnsureEntitiesLoaded();

                int idx = GetEntityIdx(EntityName);
                if (idx >= 0 && idx < Entities.Count)
                {
                    EntityStructure ent = Entities[idx];
                    DMTypeBuilder.CreateNewObject(DMEEditor, "TheTechIdea.Classes", EntityName, ent.Fields);
                }
             
                return DMTypeBuilder.MyType;
            }
            return null;
        }
        public  IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            ErrorObject.Flag = Errors.Ok;
            object data = null;
            try
            {
                DataTable dt = null;
                EntityStructure entity = GetEntityStructure(EntityName);

                // Use consolidated entity initialization check
                EnsureEntitiesLoaded();

                // Extract line filter parameters (FromLine, ToLine) using helper method
                var (fromLine, toLine) = ExtractLineFilterParameters(filter, entity.StartRow, entity.EndRow);

                if (GetFileState() == ConnectionState.Open)
                {
                    int idx = GetEntityIdx(EntityName);
                    if (idx > -1)
                    {
                        entity = Entities[idx];
                        dt = ReadDataTable(entity.OriginalEntityName, HeaderExist, fromLine, toLine);
                        SyncFieldTypes(ref dt, EntityName);

                        // Apply filters using consolidated method
                        if (filter != null && filter.Count > 0)
                        {
                            string filterQuery = BuildDataTableFilterQuery(filter);
                            if (!string.IsNullOrEmpty(filterQuery))
                            {
                                dt = dt.Select(filterQuery).CopyToDataTable();
                            }
                        }
                    }
                  
                }
               
                enttype = GetEntityType(EntityName);
                Type uowGenericType = typeof(ObservableBindingList<>).MakeGenericType(enttype);
                // Prepare the arguments for the constructor
                object[] constructorArgs = new object[] { dt };

                // Create an instance of UnitOfWork<T> with the specific constructor
                // Dynamically handle the instance since we can't cast to a specific IUnitofWork<T> at compile time
                object uowInstance = Activator.CreateInstance(uowGenericType, constructorArgs);

                // Convert to IEnumerable<object> - ObservableBindingList<T> implements IEnumerable<T>
                if (uowInstance is System.Collections.IEnumerable)
                {
                    return ((System.Collections.IEnumerable)uowInstance).Cast<object>();
                }
                return Enumerable.Empty<object>();
            }
            catch (Exception ex)
            {
                
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                Logger.WriteLog($"Error in getting File Data ({ex.Message}) ");
                return null;
            }
           // return Records;
        }

        #region "Async Methods"

        /// <summary>
        /// Asynchronously reads a data table from a named entity
        /// </summary>
        public async Task<DataTable> ReadDataTableAsync(string sheetName, bool HeaderExist = true, int fromline = 0, int toline = 10000)
        {
            try
            {
                int idx = GetEntityIdx(sheetName);
                if (idx < 0)
                {
                    throw new ArgumentException($"Sheet '{sheetName}' not found");
                }

                FileData = await (GetFileType() switch
                {
                    FileType.Csv => ReadDataTableCsvAsync(sheetName, HeaderExist, fromline, toline),
                    FileType.Xlsx or FileType.Xls => ReadDataTableNPOIAsync(sheetName, HeaderExist, fromline, toline),
                    _ => throw new NotSupportedException($"File format '{Path.GetExtension(FileName)}' is not supported")
                });

                return FileData;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error reading data table for '{sheetName}': {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                throw;
            }
        }

        /// <summary>
        /// Asynchronously gets entity structures for all sheets/CSV files
        /// </summary>
        public async Task<List<EntityStructure>> GetEntityStructuresAsync(bool refresh = false)
        {
            return await Task.Run(() => GetEntityStructures(refresh));
        }

        #endregion

        private void SyncFieldTypes(ref DataTable dt, string EntityName)
        {
            EntityStructure ent = GetEntityStructure(EntityName);
            DataTable newdt = new DataTable(EntityName);
            
            if (ent == null || dt == null)
                return;

            // Create new DataTable with properly typed columns
            foreach (var field in ent.Fields)
            {
                Type pFieldtype = Type.GetType(field.Fieldtype) ?? typeof(string);
                newdt.Columns.Add(new DataColumn(field.FieldName, pFieldtype));
            }

            // Convert and copy rows with type conversion
            foreach (DataRow sourceRow in dt.Rows)
            {
                try
                {
                    DataRow targetRow = newdt.NewRow();
                    
                    foreach (var field in ent.Fields)
                    {
                        try
                        {
                            object value = ExtractFieldValue(sourceRow, field);
                            
                            if (value != DBNull.Value && value != null)
                            {
                                string stringValue = value.ToString().Trim();
                                if (!string.IsNullOrEmpty(stringValue) && !string.IsNullOrWhiteSpace(stringValue))
                                {
                                    // Use Convert.ChangeType with helper for type-safe conversion
                                    Type targetType = Type.GetType(field.Fieldtype) ?? typeof(string);
                                    targetRow[field.FieldName] = Convert.ChangeType(stringValue, targetType);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger?.WriteLog($"Error converting field {field.FieldName}: {ex.Message}");
                        }
                    }
                    
                    newdt.Rows.Add(targetRow);
                }
                catch (Exception ex)
                {
                    Logger?.WriteLog($"Error converting row for entity {ent.EntityName}: {ex.Message}");
                }
            }

            dt = newdt;
        }
        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            ErrorObject.Flag = Errors.Ok;

            try
            {
                var entity = GetEntityStructure(EntityName);
                if (entity == null)
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = "Entity not found.";
                    return ErrorObject;
                }
                if (Dataconnection?.ConnectionProp == null)
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = "Connection properties not set.";
                    return ErrorObject;
                }
                string ext = (Dataconnection.ConnectionProp.Ext ?? Path.GetExtension(FileName)).Replace(".", "").ToLower();
                if (ext != "csv")
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = "UpdateEntities only implemented for .csv files.";
                    return ErrorObject;
                }

                // Determine if UploadData is a DataTable or IEnumerable<object>
                IEnumerable<object> rows = null;
                if (UploadData is DataTable dt)
                {
                    rows = dt.Rows.Cast<object>();
                }
                else if (UploadData is IEnumerable<object> ie)
                {
                    rows = ie;
                }
                else
                {
                    // single row -> overwrite with single row
                    rows = new List<object> { UploadData };
                }

                // Overwrite whole file with provided rows
                var writeRes = WriteRowsToCsv(entity, rows, false);
                if (writeRes.Flag == Errors.Ok)
                {
                    ErrorObject.Flag = Errors.Ok;
                    ErrorObject.Message = "File updated successfully.";
                }
                else
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = writeRes.Message;
                    ErrorObject.Ex = writeRes.Ex;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Unsuccessfully Update Entities {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
            }

            return ErrorObject;
        }

        private IErrorsInfo WriteRowsToCsv(EntityStructure entity, IEnumerable<object> rows, bool append)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok };
            try
            {
                if (Dataconnection?.ConnectionProp == null)
                {
                    retval.Flag = Errors.Failed;
                    retval.Message = "Connection properties not set.";
                    return retval;
                }
                
                string targetPath = Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName);
                char delim = Delimiter != '\0' ? Delimiter : (Dataconnection.ConnectionProp.Delimiter != '\0' ? Dataconnection.ConnectionProp.Delimiter : ',');

                var writer = new CsvFileWriter(_helper, Logger, targetPath, delim);
                return writer.WriteRows(entity, rows, append);
            }
            catch (Exception ex)
            {
                retval.Flag = Errors.Failed;
                retval.Message = ex.Message;
                retval.Ex = ex;
                Logger?.WriteLog($"Error writing CSV: {ex.Message}");
                return retval;
            }
        }

        private IErrorsInfo WriteRowsToExcel(EntityStructure entity, IEnumerable<object> rows, bool append)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok };
            try
            {
                if (Dataconnection?.ConnectionProp == null)
                {
                    retval.Flag = Errors.Failed;
                    retval.Message = "Connection properties not set.";
                    return retval;
                }
                
                string targetPath = Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName);
                string ext = Dataconnection.ConnectionProp.Ext ?? Path.GetExtension(FileName);

                var writer = new ExcelFileWriter(Logger, targetPath, ext);
                return writer.WriteRows(entity, rows, append);
            }
            catch (Exception ex)
            {
                retval.Flag = Errors.Failed;
                retval.Message = ex.Message;
                retval.Ex = ex;
                Logger?.WriteLog($"Error writing Excel: {ex.Message}");
                return retval;
            }
        }

        #region "Async Write Methods"

        /// <summary>
        /// Asynchronously writes rows to a CSV file with optional append
        /// </summary>
        public async Task<IErrorsInfo> WriteRowsToCsvAsync(EntityStructure entity, IEnumerable<object> rows, bool append)
            => await AsyncWrapper.WrapAsync(() => WriteRowsToCsv(entity, rows, append));

        /// <summary>
        /// Asynchronously writes rows to an Excel file with optional append
        /// </summary>
        public async Task<IErrorsInfo> WriteRowsToExcelAsync(EntityStructure entity, IEnumerable<object> rows, bool append)
            => await AsyncWrapper.WrapAsync(() => WriteRowsToExcel(entity, rows, append));

        /// <summary>
        /// Asynchronously inserts an entity
        /// </summary>
        public async Task<IErrorsInfo> InsertEntityAsync(string EntityName, object InsertedData)
            => await AsyncWrapper.WrapAsync(() => InsertEntity(EntityName, InsertedData));

        /// <summary>
        /// Asynchronously updates entities with optional progress reporting
        /// </summary>
        public async Task<IErrorsInfo> UpdateEntitiesAsync(string EntityName, object UploadData, IProgress<PassedArgs> progress)
            => await AsyncWrapper.WrapAsync(() => UpdateEntities(EntityName, UploadData, progress));

        #endregion

        #region "Unsupported Operations (File-based datasource)"

        public IEnumerable<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters) => null;

        public DataSet GetChildTablesListFromCustomQuery(string tablename, string customquery) => new DataSet();

        public IEnumerable<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName) 
            => new List<RelationShipKeys>();

        public IErrorsInfo ExecuteSql(string sql)
        {
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = "SQL execution not supported for CSV/Excel files";
            DMEEditor?.AddLogMessage("Beep", ErrorObject.Message, DateTime.Now, -1, null, Errors.Failed);
            return ErrorObject;
        }

        #endregion

        public bool CreateEntityAs(EntityStructure entity)
        {
            try
            {
                // CSV/Excel files don't support creating entities
                // Entities are sheets/worksheets in the file
                if (Entities == null)
                {
                    Entities = new List<EntityStructure>();
                }
                if (!Entities.Any(p => p.EntityName == entity.EntityName))
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
                // Create an empty file with header depending on extension
                try
                {
                    if (Dataconnection?.ConnectionProp != null)
                    {
                        string ext = (Dataconnection.ConnectionProp.Ext ?? Path.GetExtension(FileName)).Replace(".", "").ToLower();
                        string targetPath = Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName);
                        Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                        if (ext == "csv")
                        {
                            char delim = Delimiter != '\0' ? Delimiter : (Dataconnection.ConnectionProp.Delimiter != '\0' ? Dataconnection.ConnectionProp.Delimiter : ',');
                            using (var sw = new StreamWriter(targetPath, false, Encoding.UTF8))
                            {
                                var header = string.Join(delim.ToString(), entity.Fields.Select(f => _helper.EscapeCsvValue(f.Originalfieldname ?? f.FieldName, delim)));
                                sw.WriteLine(header);
                            }
                        }
                        else if (ext == "xlsx" || ext == "xls")
                        {
                            // create workbook and header row (xlsx preferred)
                            try
                            {
                                IWorkbook workbook = ext == "xlsx" ? (IWorkbook)new XSSFWorkbook() : new HSSFWorkbook();
                                ISheet sheet = workbook.CreateSheet(entity.EntityName ?? "Sheet1");
                                IRow headerRow = sheet.CreateRow(0);
                                int ci = 0;
                                foreach (var f in entity.Fields)
                                {
                                    var cell = headerRow.CreateCell(ci++);
                                    cell.SetCellValue(f.Originalfieldname ?? f.FieldName);
                                }
                                using (var fs = new FileStream(targetPath, FileMode.Create, FileAccess.Write))
                                {
                                    workbook.Write(fs);
                                }
                            }
                            catch (Exception ex)
                            {
                                DMEEditor?.AddLogMessage("Beep", $"Error creating Excel entity file: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                                try { ErrorObject.Flag = Errors.Failed; ErrorObject.Ex = ex; ErrorObject.Message = ex.Message; } catch { }
                                Logger?.WriteLog($"Error creating Excel entity file: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    DMEEditor?.AddLogMessage("Beep", $"Error creating entity file: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                    try { ErrorObject.Flag = Errors.Failed; ErrorObject.Ex = ex; ErrorObject.Message = ex.Message; } catch { }
                    Logger?.WriteLog($"Error creating entity file: {ex.Message}");
                }
                return true;
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in CreateEntityAs: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                try { ErrorObject.Flag = Errors.Failed; ErrorObject.Ex = ex; ErrorObject.Message = ex.Message; } catch { }
                Logger?.WriteLog($"Error in CreateEntityAs: {ex.Message}");
                return false;
            }
        }
        public bool CheckEntityExist(string EntityName)
        {
            bool retval=false;
            if(GetFileState()== ConnectionState.Open)
            {
                if (Entities != null)
                {
                    if (Entities.Where(x => string.Equals(x.EntityName, EntityName, StringComparison.OrdinalIgnoreCase)).Count() > 0)
                    {
                        retval = true;
                    }
                    else
                        retval = false;

                }

            }

            return retval;
        }
        /// <summary>
        /// Gets entity structure by name with optional refresh
        /// Handles both string and EntityStructure input for compatibility
        /// </summary>
        public EntityStructure GetEntityStructure(string entityName, bool refresh = false)
        {
            EntityStructure result = null;

            try
            {
                // Ensure entities are loaded
                EnsureEntitiesLoaded(entityName);

                if (GetFileState() != ConnectionState.Open)
                    return null;

                // Try to get from cache first
                int idx = GetEntityIdx(entityName);
                if (idx >= 0 && idx < Entities.Count && !refresh)
                {
                    result = Entities[idx];
                }
                else
                {
                    // Refresh from source or load new
                    GetSheets();
                    idx = GetEntityIdx(entityName);
                    if (idx >= 0 && idx < Entities.Count)
                    {
                        result = Entities[idx];
                    }
                }

                // Save configuration
                if (result != null)
                {
                    DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(
                        new DatasourceEntities 
                        { 
                            datasourcename = DatasourceName, 
                            Entities = Entities 
                        });
                }

                return result;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error getting entity structure for '{entityName}': {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                throw;
            }
        }

        /// <summary>
        /// Overload for EntityStructure parameter - delegates to string version
        /// </summary>
        public EntityStructure GetEntityStructure(EntityStructure entity, bool refresh = false)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
                
            return GetEntityStructure(entity.EntityName, refresh);
        }
        public IEnumerable<object> RunQuery(string qrystr)
        {
            List<object> results = new List<object>();
            try
            {
                // CSV/Excel files don't support SQL queries directly
                // Could parse simple queries if needed
                DMEEditor?.AddLogMessage("Beep", "Query execution not fully supported for CSV/Excel files", DateTime.Now, -1, null, Errors.Failed);
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in RunQuery: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                try { ErrorObject.Flag = Errors.Failed; ErrorObject.Ex = ex; ErrorObject.Message = ex.Message; } catch { }
                Logger?.WriteLog($"Error in RunQuery: {ex.Message}");
            }
            return results;
        }
        public virtual IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok, Message = "Data updated successfully." };
            try
            {
                // CSV/Excel files are typically read-only in this implementation
                // Update would require rewriting the file
                // Implement point-update by rewriting full file: not implemented here.
                retval.Flag = Errors.Failed;
                retval.Message = "Update operation not supported in this method; use UpdateEntities to overwrite file.";
                DMEEditor?.AddLogMessage("Beep", "Update operation not supported for CSV/Excel files via UpdateEntity; use UpdateEntities.", DateTime.Now, -1, null, Errors.Failed);
            }
            catch (Exception ex)
            {
                retval.Flag = Errors.Failed;
                retval.Message = $"Error updating entity: {ex.Message}";
                DMEEditor?.AddLogMessage("Beep", $"Error updating entity: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return retval;
        }
        public IErrorsInfo DeleteEntity(string EntityName, object DeletedDataRow)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok, Message = "Data deleted successfully." };
            try
            {
                // CSV/Excel files are typically read-only in this implementation
                // Delete would require rewriting the file
                retval.Flag = Errors.Failed;
                retval.Message = "Delete operation not supported for CSV/Excel files. Files are read-only.";
                DMEEditor?.AddLogMessage("Beep", "Delete operation not supported for CSV/Excel files", DateTime.Now, -1, null, Errors.Failed);
            }
            catch (Exception ex)
            {
                retval.Flag = Errors.Failed;
                retval.Message = $"Error deleting entity: {ex.Message}";
                DMEEditor?.AddLogMessage("Beep", $"Error deleting entity: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return retval;
        }  
        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                // CSV/Excel files don't support scripts
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = "Script execution not supported for CSV/Excel files";
                DMEEditor?.AddLogMessage("Beep", "Script execution not supported for CSV/Excel files", DateTime.Now, -1, null, Errors.Failed);
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
                            SourceEntity = entity,
                           ScriptType= DDLScriptType.CreateEntity,
                          
                        };
                        scripts.Add(script);
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in GetCreateEntityScript: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                try { ErrorObject.Flag = Errors.Failed; ErrorObject.Ex = ex; ErrorObject.Message = ex.Message; } catch { }
                Logger?.WriteLog($"Error in GetCreateEntityScript: {ex.Message}");
            }
            return scripts;
        }
        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok, Message = "Data inserted successfully." };
            try
            {
                var entity = GetEntityStructure(EntityName);
                if (entity == null)
                {
                    retval.Flag = Errors.Failed;
                    retval.Message = "Entity not found.";
                    return retval;
                }
                if (Dataconnection?.ConnectionProp == null)
                {
                    retval.Flag = Errors.Failed;
                    retval.Message = "Connection properties not set.";
                    return retval;
                }
                string ext = (Dataconnection.ConnectionProp.Ext ?? Path.GetExtension(FileName)).Replace(".", "").ToLower();
                if (ext != "csv")
                {
                    retval.Flag = Errors.Failed;
                    retval.Message = "Insert supported only for .csv files.";
                    return retval;
                }
                // append single row
                retval = (ErrorsInfo)WriteRowsToCsv(entity, new List<object> { InsertedData }, true);
                if (retval.Flag == Errors.Ok)
                {
                    DMEEditor?.AddLogMessage("Beep", $"Row appended to {EntityName}", DateTime.Now, -1, null, Errors.Ok);
                }
            }
            catch (Exception ex)
            {
                retval.Flag = Errors.Failed;
                retval.Message = $"Error inserting entity: {ex.Message}";
                DMEEditor?.AddLogMessage("Beep", $"Error inserting entity: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return retval;
        }
        public Task<IEnumerable< object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            return Task.FromResult(GetEntity(EntityName, Filter));
        }
        #region "Excel and CSV Reader"
        
        public ConnectionState GetFileState()
        {
            if (ConnectionStatus == ConnectionState.Open)
            {
                return ConnectionStatus;
            }else
            {
                return Openconnection();
            }

        }
        public List<EntityStructure> GetEntityStructures(bool refresh = false)
        {
            List<EntityStructure> retval = new List<EntityStructure>();
            Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.FileName == FileName).FirstOrDefault();
           // Openconnection();

            if (GetFileState() == ConnectionState.Open)
            {
                CombineFilePath = Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName);
                if (File.Exists(CombineFilePath))
                {

                    if ((Entities == null) || (Entities.Count == 0) || (refresh))
                    {
                        Entities = new List<EntityStructure>();
                        IsFileRead = false;
                        GetSheets();
                        Dataconnection.ConnectionProp.Delimiter = Delimiter;
                       

                    }
                    else
                    {
                        if(Entities.Count == 0)
                        {
                            if (Entities != null)
                            {
                                if (Entities.Count == 0)
                                {
                                    IsFileRead = false;
                                    GetSheets();

                                }
                            }
                        }
                        DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities { datasourcename = FileName, Entities = Entities });
                        //  ConnProp.Entities = Entities;
                        DMEEditor.ConfigEditor.SaveDataconnectionsValues();

                    }


                    retval = Entities;
                }
                else
                    retval = Entities;

            }
          

            return retval;

        }

        /* DEPRECATED: ExcelDataReader support removed. These methods are kept for reference only.
           They are NO LONGER FUNCTIONAL and should not be used.
           Use GetSheetsNPOI/GetSheetsCsv and ReadDataTableNPOI/ReadDataTableCsv instead.

        private ExcelReaderConfiguration GetReaderConfiguration()
        {
            // DEPRECATED - ExcelDataReader no longer supported
            throw new NotSupportedException("ExcelDataReader has been removed. Use NPOI reader instead.");
        }

        private ExcelDataSetConfiguration GetDataSetConfiguration(int sheetidx,int startrow)
        {
            // DEPRECATED - ExcelDataReader no longer supported
            throw new NotSupportedException("ExcelDataReader has been removed. Use NPOI reader instead.");
        }

        private ExcelDataSetConfiguration GetDataSetConfiguration(string sheetname, int startrow)
        {
            // DEPRECATED - ExcelDataReader no longer supported
            throw new NotSupportedException("ExcelDataReader has been removed. Use NPOI reader instead.");
        }

        private DataTable GetDataTableforSheet(int sheetidx, int startrow)
        {
            // DEPRECATED - ExcelDataReader no longer supported
            throw new NotSupportedException("ExcelDataReader has been removed. Use ReadDataTable with sheet name instead.");
        }

        private DataTable GetDataTableforSheet(string sheetname, int startrow)
        {
            // DEPRECATED - ExcelDataReader no longer supported
            throw new NotSupportedException("ExcelDataReader has been removed. Use ReadDataTable with sheet name instead.");
        }

        private DataSet GetExcelDataSet()   
        {
            // DEPRECATED - ExcelDataReader no longer supported
            throw new NotSupportedException("ExcelDataReader has been removed. Use GetEntityStructures instead.");
        }

        public IExcelDataReader getExcelReader()
        {
            // DEPRECATED - ExcelDataReader no longer supported
            throw new NotSupportedException("ExcelDataReader has been removed. Use NPOI reader instead.");
        }

        END DEPRECATED SECTION */
       
        private void GetSheets()
        {
            if (GetFileState() == ConnectionState.Open && !IsFileRead)
            {
                try
                {
                    // Dispatch to appropriate reader based on file type
                    switch (GetFileType())
                    {
                        case FileType.Csv:
                            GetSheetsCsv();
                            break;
                        case FileType.Xlsx:
                        case FileType.Xls:
                            GetSheetsNPOI();
                            break;
                        default:
                            ErrorObject.Flag = Errors.Failed;
                            ErrorObject.Message = $"Unsupported file format: {Path.GetExtension(FileName)}";
                            DMEEditor.AddLogMessage("Fail", $"Error in getting File format {FileName}: unsupported format", DateTime.Now, 0, FileName, Errors.Failed);
                            Logger?.WriteLog($"Error in GetSheets: Unsupported file format");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    DMEEditor.AddLogMessage("Fail", $"Error in getting File format {ex.Message}", DateTime.Now, 0, FileName, Errors.Failed);
                    try { ErrorObject.Flag = Errors.Failed; ErrorObject.Ex = ex; ErrorObject.Message = ex.Message; } catch { }
                    Logger?.WriteLog($"Error in GetSheets: {ex.Message}");
                }
            }
        }

        private List<EntityField> GetSheetColumns(string psheetname)
        {
            return GetEntityDataType(psheetname).Fields.Where(x => x.EntityName == psheetname).ToList();
        }
        private void GetTypeForSheetsFile(string pSheetname)
        {
            List<EntityField> flds = GetSheetColumns(pSheetname);
            DMTypeBuilder.CreateNewObject(DMEEditor, pSheetname, pSheetname, flds);

        }
       
        //public DataTable ReadDataTable(string sheetname, bool HeaderExist = true, int fromline = 0, int toline = 100)
        //{
        //    if (GetFileState() == ConnectionState.Open)
        //    {
        //        DataTable dataRows = new DataTable();
        //        FileData = GetDataTableforSheet(sheetname, fromline);
        //        dataRows = FileData;
        //        toline = dataRows.Rows.Count;
        //        List<EntityField> flds = GetSheetColumns(FileData.TableName);
        //        // DMEEditor.classCreator.CreateClass(FileData.Tables[sheetno].TableName, flds, classpath);
        //        //GetTypeForSheetsFile(dataRows.TableName);
        //        return dataRows;
        //    }
        //    else
        //    {
        //        return null;
        //    }

        //}
        public int GetSheetNumber(DataSet ls, string sheetname)
        {
            if (ls == null || ls.Tables == null || ls.Tables.Count == 0) return -1;
            for (int i = 0; i < ls.Tables.Count; i++)
            {
                try
                {
                    if (ls.Tables[i].TableName.Equals(sheetname, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return i;
                    }
                }
                catch
                {
                    // ignore malformed table names
                }
            }
            return -1;

        }
        public DataTable ReadDataTable(int sheetno = 0, bool HeaderExist = true, int fromline = 0, int toline = 100)
        {
            if (sheetno > -1 && sheetno < Entities.Count)
            {
                EntityStructure entity = Entities[sheetno];
                return ReadDataTable(entity.OriginalEntityName, HeaderExist, fromline, toline);
            }
            return FileData;
        }
        public DataTable ReadDataTable(string sheetname, bool HeaderExist = true, int fromline = 0, int toline = 10000)
        {
            try
            {
                int idx = GetEntityIdx(sheetname);
                if (idx < 0)
                {
                    throw new ArgumentException($"Sheet '{sheetname}' not found");
                }

                FileData = GetFileType() switch
                {
                    FileType.Csv => ReadDataTableCsv(sheetname, HeaderExist, fromline, toline),
                    FileType.Xlsx or FileType.Xls => ReadDataTableNPOI(sheetname, HeaderExist, fromline, toline),
                    _ => throw new NotSupportedException($"File format '{Path.GetExtension(FileName)}' is not supported")
                };

                return FileData;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error reading data table for '{sheetname}': {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                throw;
            }
        }
        private EntityStructure GetEntityDataType(int sheetno)
        {

            return Entities[sheetno];
        }
      
        // DEPRECATED: ExcelDataReader methods removed - use NPOI reader instead
        // Kept for reference but not functional
        public IEnumerable<string> getWorksheetNames()
        {
            List<string> entlist = new List<string>();
            if (GetFileState() == ConnectionState.Open)
            {

                if (Entities.Count == 0)
                {
                    Entities = GetEntityStructures(true);

                }
                foreach (EntityStructure item in Entities)
                {

                    entlist.Add(item.EntityName);
                }

            }
            return entlist;

        }
        public IEnumerable<DataRow> getData(string sheet, bool firstRowIsColumnNames = false)
        {
            if (GetFileState() == ConnectionState.Open)
            {
                    FileData = ReadDataTable(sheet,true);
                
              //  var workSheet = FileData;
                var rows = from DataRow a in FileData.Rows select a;
                return rows;
            }
            else
            {
                return null;
            }

        }
        private List<EntityField> GetFieldsbyTableScan(DataTable tbdata,string sheetname, DataColumnCollection datac)
        {
            var rows = from DataRow a in tbdata.Rows select a;
            IEnumerable<DataRow> tb =rows;
            List<EntityField> flds = new List<EntityField>();
            int y = 0;
            string valstring;
            decimal dval;
            double dblval;
            long longval;
            bool boolval;
            int intval;
            short shortval;
            float floatval;
          
            DateTime dateval = DateTime.Now;
            // setup Fields for Entity
            foreach (DataColumn field in datac)
            {
                EntityField f = new EntityField();
                string entspace = Regex.Replace(field.ColumnName, @"[\s-]+", "_");
                if (entspace.Equals(sheetname, StringComparison.InvariantCultureIgnoreCase))
                {
                    entspace = "_" + entspace;
                }
                f.FieldName = entspace;
                f.Originalfieldname = field.ColumnName;
                f.Fieldtype = field.DataType.ToString();
                f.ValueRetrievedFromParent = false;
                f.EntityName = sheetname;
                f.FieldIndex = y;
                f.Checked = false;
                f.AllowDBNull = true;
                f.IsAutoIncrement = false;
                f.IsCheck = false;
                f.IsKey = false;
                f.IsUnique = false;
                y++;
                flds.Add(f);
            }
            // Scan all rows in Table for types
            foreach (DataRow r in tb)
            {
                try
                {
                    // Scan fields in row for Types
                    foreach (EntityField f in flds)
                    {
                        try
                        {
                            if (r[f.Originalfieldname] != DBNull.Value)
                            {
                                valstring = r[f.Originalfieldname].ToString();
                                dateval = DateTime.Now;

                                if (!string.IsNullOrEmpty(valstring) && !string.IsNullOrWhiteSpace(valstring))
                                {
                                    if (f.Fieldtype != "System.String")
                                    {
                                        if (decimal.TryParse(valstring, out dval))
                                        {
                                            f.Fieldtype = "System.Decimal";
                                            f.Checked = true;
                                        }
                                        else
                                        if (double.TryParse(valstring, out dblval))
                                        {
                                            f.Fieldtype = "System.Double";
                                            f.Checked = true;
                                        }
                                        else
                                        if (long.TryParse(valstring, out longval))
                                        {
                                            f.Fieldtype = "System.Long";
                                            f.Checked = true;
                                        }
                                        else
                                        if (float.TryParse(valstring, out floatval))
                                        {
                                            f.Fieldtype = "System.Float";
                                            f.Checked = true;
                                        }
                                        else
                                        if (int.TryParse(valstring, out intval))
                                        {
                                            f.Fieldtype = "System.Int32";
                                            f.Checked = true;
                                        }
                                        else
                                        if (DateTime.TryParse(valstring, out dateval))
                                        {
                                            f.Fieldtype = "System.DateTime";
                                            f.Checked = true;
                                        }
                                        else
                                        if (bool.TryParse(valstring, out boolval))
                                        {
                                            f.Fieldtype = "System.Bool";
                                            f.Checked = true;
                                        }
                                        else
                                        if (short.TryParse(valstring, out shortval))
                                        {
                                            f.Fieldtype = "System.Short";
                                            f.Checked = true;
                                        }
                                        else
                                            f.Fieldtype = "System.String";
                                    }
                                    else
                                        f.Fieldtype = "System.String";

                                }
                            }
                        }
                        catch (Exception Fieldex)
                        {
                            Logger?.WriteLog($"Field type detection error for field {f.FieldName}: {Fieldex.Message}");
                            try { ErrorObject.Flag = Errors.Failed; ErrorObject.Ex = Fieldex; ErrorObject.Message = Fieldex.Message; } catch { }
                        }
                        try
                        {
                            if (f.Fieldtype.Equals("System.String", StringComparison.OrdinalIgnoreCase))
                            {
                                if (r[f.Originalfieldname] != DBNull.Value)
                                {
                                    if (!string.IsNullOrEmpty(r[f.Originalfieldname].ToString()))
                                    {
                                        if (r[f.Originalfieldname].ToString().Length > f.Size1)
                                        {
                                            f.Size1 = r[f.Originalfieldname].ToString().Length;
                                        }

                                    }
                                }

                            }
                        }
                        catch (Exception stringsizeex)
                        {
                            Logger?.WriteLog($"String size detection error for field {f.FieldName}: {stringsizeex.Message}");
                            try { ErrorObject.Flag = Errors.Failed; ErrorObject.Ex = stringsizeex; ErrorObject.Message = stringsizeex.Message; } catch { }
                        }
                        try
                        {
                            if (f.Fieldtype.Equals("System.Decimal", StringComparison.OrdinalIgnoreCase))
                            {
                                if (r[f.Originalfieldname] != DBNull.Value)
                                {
                                    if (!string.IsNullOrEmpty(r[f.Originalfieldname].ToString()))
                                    {
                                        valstring = r[f.Originalfieldname].ToString();
                                        if (decimal.TryParse(valstring, out dval))
                                        {

                                            f.Fieldtype = "System.Decimal";
                                            f.Size1 = GetDecimalPrecision(dval);
                                            f.Size2 = GetDecimalScale(dval);
                                        }
                                    }
                                }

                            }
                        }
                        catch (Exception decimalsizeex)
                        {
                            Logger?.WriteLog($"Decimal size detection error for field {f.FieldName}: {decimalsizeex.Message}");
                            try { ErrorObject.Flag = Errors.Failed; ErrorObject.Ex = decimalsizeex; ErrorObject.Message = decimalsizeex.Message; } catch { }
                        }
                      
                    }
                }
                catch (Exception rowex)
                {
                    Logger?.WriteLog($"Error scanning table rows for size/type detection: {rowex.Message}");
                    try { ErrorObject.Flag = Errors.Failed; ErrorObject.Ex = rowex; ErrorObject.Message = rowex.Message; } catch { }
                }
                
            }
            // Check for string size
            foreach (EntityField fld in flds)
            {
                if (fld.Fieldtype.Equals("System.string", StringComparison.OrdinalIgnoreCase))
                {
                    if (fld.Size1 == 0)
                    {
                        fld.Size1 = 150;
                    }

                }
            }
            return flds;
        }
        private List<EntityField> GetStringSizeFromTable(List<EntityField> entityFields ,DataTable tb)
        {

            foreach (DataRow r in tb.Rows)
            {
                foreach (EntityField fld in entityFields)
                {
                    if (fld.Fieldtype.Equals("System.string", StringComparison.OrdinalIgnoreCase))
                    {
                        if (r[fld.FieldName] != DBNull.Value)
                        {
                            if (!string.IsNullOrEmpty(r[fld.FieldName].ToString()))
                            {
                                if (r[fld.FieldName].ToString().Length > fld.Size1)
                                {
                                    fld.Size1 = r[fld.FieldName].ToString().Length;
                                }
                           
                            }
                        }
                        
                    }
                    decimal dval;
               
                    string valstring;
                   
                    if (fld.Fieldtype.Equals("System.Decimal", StringComparison.OrdinalIgnoreCase))
                    {
                        if (r[fld.FieldName] != DBNull.Value)
                        {
                            if (!string.IsNullOrEmpty(r[fld.FieldName].ToString()))
                            {
                                valstring = r[fld.FieldName].ToString();
                                if (decimal.TryParse(valstring, out dval))
                                {
                                    
                                    fld.Fieldtype = "System.Decimal";
                                    fld.Size1 = GetDecimalPrecision(dval);
                                    fld.Size2= GetDecimalScale(dval);

                                }
                               
                                   
                                

                            }
                        }

                    }
                  
                  
                }
            }
            foreach (EntityField fld in entityFields)
            {
                if (fld.Fieldtype.Equals("System.string", StringComparison.OrdinalIgnoreCase))
                {
                  if (fld.Size1==0)
                  {
                        fld.Size1 = 150;
                  }

                }
            }
            return entityFields;
        }
        public static int GetDecimalScale( decimal value)
        {
            if (value == 0)
                return 0;
            int[] bits = decimal.GetBits(value);
            return (int)((bits[3] >> 16) & 0x7F);
        }
        public static int GetDecimalPrecision( decimal value)
        {
            if (value == 0)
                return 0;
            int[] bits = decimal.GetBits(value);
            //We will use false for the sign (false =  positive), because we don't care about it.
            //We will use 0 for the last argument instead of bits[3] to eliminate the fraction point.
            decimal d = new Decimal(bits[0], bits[1], bits[2], false, 0);
            return (int)Math.Floor(Math.Log10((double)d)) + 1;
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
                    try
                    {
                       
                    }
                    catch (Exception ex)
                    {
                        Logger?.WriteLog($"Error disposing reader container: {ex.Message}");
                    }
                    try
                    {
                        FileData = null;
                        // do not clear Entities here; keep in memory unless explicit refresh requested
                    }
                    catch (Exception ex)
                    {
                        Logger?.WriteLog($"Error clearing FileData: {ex.Message}");
                    }
                }

                // no unmanaged resources at this time
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
                // Get all data first
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
                ErrorObject.Ex = ex;
                DMEEditor?.AddLogMessage("Beep", $"Error in GetEntity with pagination: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                Logger?.WriteLog($"Error in GetEntity pagination: {ex.Message}");
            }

            return pagedResult;
        }
        #endregion

    }
}
