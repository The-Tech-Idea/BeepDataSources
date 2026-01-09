using Parquet.Rows;
using Parquet.Schema;
using Parquet;
using ParquetSharp;
using Parquet.Data;
using Parquet.File;
using System.Data;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using TheTechIdea;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using System.Collections.Generic;
using TheTechIdea.Beep.FileManager;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Helpers;
using Parquet.Serialization;

namespace ParquetDataSourceCore
{
    [AddinAttribute(Category = DatasourceCategory.FILE, DatasourceType = DataSourceType.Text , FileType = "parquet")]
    public class ParquetDataSource : IDataSource
    {
        private bool disposedValue;
        string CombineFilePath = string.Empty;
        string FileName = string.Empty;
        public ParquetDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType pDatasourceType, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = pDatasourceType;
            Dataconnection = new FileConnection(DMEEditor)
            {
                Logger = logger,
                ErrorObject = ErrorObject,
            };
            Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.FileName == datasourcename).FirstOrDefault();
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            Category = DatasourceCategory.FILE;
            FileName = Dataconnection.ConnectionProp.FileName;
            CombineFilePath = Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName);
            //SetupConfig();
        }
        public DataSourceType DatasourceType { get; set; } = DataSourceType.Text;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.FILE;
        public IDataConnection Dataconnection { get  ; set  ; }
        public string DatasourceName { get  ; set  ; }
        public IErrorsInfo ErrorObject { get  ; set  ; }
        public string Id { get  ; set  ; }
        public IDMLogger Logger { get  ; set  ; }
        public List<string> EntitiesNames { get; set; } = new List<string>();
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
        public IDMEEditor DMEEditor { get  ; set  ; }
        ConnectionState pConnectionStatus;
        public ConnectionState ConnectionStatus { get { return Dataconnection.ConnectionStatus; } set { pConnectionStatus = value; } }
        public string ColumnDelimiter { get  ; set  ; }
        public string ParameterDelimiter { get  ; set  ; }
        public string GuidID { get; set; } = Guid.NewGuid().ToString();

        public event EventHandler<PassedArgs> PassEvent;

        public IErrorsInfo BeginTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            // Parquet files don't support transactions
            return ErrorObject;
        }

        public bool CheckEntityExist(string EntityName)
        {
            try
            {
                if (EntitiesNames != null && EntitiesNames.Count > 0)
                {
                    return EntitiesNames.Any(e => e.Equals(EntityName, StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    GetEntitesList();
                    return EntitiesNames.Any(e => e.Equals(EntityName, StringComparison.OrdinalIgnoreCase));
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error checking entity existence: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return false;
            }
        }

        public ConnectionState Closeconnection()
        {
            try
            {
                ConnectionStatus = Dataconnection.CloseConnection();
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                DMEEditor?.AddLogMessage("Beep", $"Error closing connection: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ConnectionStatus;
        }

        public IErrorsInfo Commit(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            // Parquet files don't support transactions
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

        public bool CreateEntityAs(EntityStructure entity)
        {
            try
            {
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
                return true;
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in CreateEntityAs: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return false;
            }
        }

        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok, Message = "Data deleted successfully." };
            try
            {
                // Parquet files are read-only in typical scenarios
                // Deletion would require rewriting the file
                retval.Flag = Errors.Failed;
                retval.Message = "Delete operation not supported for Parquet files. Files are read-only.";
                DMEEditor?.AddLogMessage("Beep", "Delete operation not supported for Parquet files", DateTime.Now, -1, null, Errors.Failed);
            }
            catch (Exception ex)
            {
                retval.Flag = Errors.Failed;
                retval.Message = $"Error deleting entity: {ex.Message}";
                DMEEditor?.AddLogMessage("Beep", $"Error deleting entity: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return retval;
        }

        public IErrorsInfo EndTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            // Parquet files don't support transactions
            return ErrorObject;
        }

        public IErrorsInfo ExecuteSql(string sql)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                // Parquet files don't support SQL
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = "SQL execution not supported for Parquet files";
                DMEEditor?.AddLogMessage("Beep", "SQL execution not supported for Parquet files", DateTime.Now, -1, null, Errors.Failed);
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
            }
            return ErrorObject;
        }

        public IEnumerable<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            // Parquet files don't have child tables
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
                            ScriptText = $"# Parquet entity: {entity.EntityName}\n# Schema defined in Parquet file"
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
            try
            {
                if (GetFileState() == ConnectionState.Open)
                {
                    if (EntitiesNames == null || EntitiesNames.Count == 0)
                    {
                        GetSheets();
                    }
                }
                return EntitiesNames ?? new List<string>();
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in GetEntitesList: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return new List<string>();
            }
        }

        public IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            List<object> results = new List<object>();
            try
            {
                if (GetFileState() != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open && File.Exists(CombineFilePath))
                {
                    var rows = ReadRows();
                    foreach (var row in rows)
                    {
                        // Apply filters if provided
                        bool matches = true;
                        if (filter != null && filter.Count > 0)
                        {
                            foreach (var f in filter)
                            {
                                if (row.TryGetValue(f.FieldName, out object value))
                                {
                                    bool fieldMatches = EvaluateFilter(value, f.Operator, f.FilterValue);
                                    if (!fieldMatches)
                                    {
                                        matches = false;
                                        break;
                                    }
                                }
                            }
                        }

                        if (matches)
                        {
                            results.Add(row);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
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
                if (GetFileState() != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open && File.Exists(CombineFilePath))
                {
                    var allRows = ReadRows().ToList();
                    
                    // Apply filters
                    if (filter != null && filter.Count > 0)
                    {
                        allRows = allRows.Where(row =>
                        {
                            foreach (var f in filter)
                            {
                                if (row.TryGetValue(f.FieldName, out object value))
                                {
                                    if (!EvaluateFilter(value, f.Operator, f.FilterValue))
                                    {
                                        return false;
                                    }
                                }
                                else
                                {
                                    return false;
                                }
                            }
                            return true;
                        }).ToList();
                    }

                    int totalRecords = allRows.Count;
                    int offset = (pageNumber - 1) * pageSize;
                    var pagedRows = allRows.Skip(offset).Take(pageSize).ToList();

                    List<object> results = new List<object>();
                    foreach (var row in pagedRows)
                    {
                        results.Add(row);
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
                DMEEditor?.AddLogMessage("Beep", $"Error in GetEntity with pagination: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return pagedResult;
        }

        public Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            return Task.Run(() => GetEntity(EntityName, Filter));
        }

        public IEnumerable<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            // Parquet files don't have foreign keys
            return new List<RelationShipKeys>();
        }

        public int GetEntityIdx(string entityName)
        {
            try
            {
                if (Entities != null && Entities.Count > 0)
                {
                    return Entities.FindIndex(e => e.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase) ||
                                                   e.OriginalEntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
                }
                return -1;
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in GetEntityIdx: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return -1;
            }
        }

        public EntityStructure GetEntityStructure(string EntityName, bool refresh = false)
        {
            EntityStructure retval = null;

            if (GetFileState() == ConnectionState.Open)
            {
                if (Entities != null)
                {
                    if (Entities.Count == 0)
                    {
                        GetSheets();

                    }
                }

                retval = Entities.Where(x => string.Equals(x.OriginalEntityName, EntityName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                if (retval == null || refresh)
                {
                    EntityStructure fndval = GetSheetEntity(EntityName);
                    retval = fndval;
                    if (retval == null)
                    {
                        Entities.Add(fndval);
                    }
                    else
                    {

                        Entities[GetEntityIdx(EntityName)] = fndval;
                    }
                }
                if (Entities.Count() == 0)
                {
                    GetSheets();
                }
                DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities { datasourcename = DatasourceName, Entities = Entities });
            }
            return retval;
        }
        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            EntityStructure retval = null;

            if (GetFileState() == ConnectionState.Open)
            {
                if (Entities != null)
                {
                    if (Entities.Count == 0)
                    {
                        var cols = GetSchemaColumns(CombineFilePath);

                    }
                }
                retval = Entities.Where(x => string.Equals(x.OriginalEntityName, fnd.EntityName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                if (retval == null || refresh)
                {
                    EntityStructure fndval = GetSheetEntity(fnd.EntityName);
                    retval = fndval;
                    if (retval == null)
                    {
                        Entities.Add(fndval);
                    }
                    else
                    {
                        Entities[GetEntityIdx(fnd.EntityName)] = fndval;
                    }
                }
                if (Entities.Count() == 0)
                {
                    GetSheets();

                }
                DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities { datasourcename = DatasourceName, Entities = Entities });
            }
            return retval;
        }

        public Type GetEntityType(string EntityName)
        {
            try
            {
                var entityStructure = GetEntityStructure(EntityName, false);
                if (entityStructure != null && entityStructure.Fields != null && entityStructure.Fields.Count > 0)
                {
                    if (DMEEditor != null)
                    {
                        string code = DMTypeBuilder.ConvertPOCOClassToEntity(DMEEditor, entityStructure, "ParquetGeneratedTypes");
                        return DMTypeBuilder.CreateTypeFromCode(DMEEditor, code, EntityName);
                    }
                }
                return typeof(object);
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in GetEntityType: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return typeof(object);
            }
        }

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok, Message = "Data inserted successfully." };
            try
            {
                // Parquet files are typically read-only
                // Insertion would require rewriting the file
                retval.Flag = Errors.Failed;
                retval.Message = "Insert operation not supported for Parquet files. Files are read-only.";
                DMEEditor?.AddLogMessage("Beep", "Insert operation not supported for Parquet files", DateTime.Now, -1, null, Errors.Failed);
            }
            catch (Exception ex)
            {
                retval.Flag = Errors.Failed;
                retval.Message = $"Error inserting entity: {ex.Message}";
                DMEEditor?.AddLogMessage("Beep", $"Error inserting entity: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return retval;
        }

        public ConnectionState Openconnection()
        {

            ConnectionStatus = Dataconnection.OpenConnection();

            if (ConnectionStatus == ConnectionState.Open)
            {
                if (DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues(FileName) == null)
                {
                    GetSheets();
                }
                else
                {
                    Entities = DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues(FileName).Entities;
                };
                CombineFilePath = Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName);
            }

            return ConnectionStatus;

        }
        public IEnumerable<object> RunQuery(string qrystr)
        {
            List<object> results = new List<object>();
            try
            {
                // Parquet files don't support SQL queries directly
                // Return empty results or parse simple queries
                DMEEditor?.AddLogMessage("Beep", "Query execution not fully supported for Parquet files", DateTime.Now, -1, null, Errors.Failed);
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
                // Parquet files don't support scripts
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = "Script execution not supported for Parquet files";
                DMEEditor?.AddLogMessage("Beep", "Script execution not supported for Parquet files", DateTime.Now, -1, null, Errors.Failed);
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
            }
            return ErrorObject;
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok, Message = "Data updated successfully." };
            try
            {
                // Parquet files are read-only
                retval.Flag = Errors.Failed;
                retval.Message = "Update operation not supported for Parquet files. Files are read-only.";
                DMEEditor?.AddLogMessage("Beep", "Update operation not supported for Parquet files", DateTime.Now, -1, null, Errors.Failed);
            }
            catch (Exception ex)
            {
                retval.Flag = Errors.Failed;
                retval.Message = $"Error updating entities: {ex.Message}";
                DMEEditor?.AddLogMessage("Beep", $"Error updating entities: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return retval;
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok, Message = "Data updated successfully." };
            try
            {
                // Parquet files are read-only
                retval.Flag = Errors.Failed;
                retval.Message = "Update operation not supported for Parquet files. Files are read-only.";
                DMEEditor?.AddLogMessage("Beep", "Update operation not supported for Parquet files", DateTime.Now, -1, null, Errors.Failed);
            }
            catch (Exception ex)
            {
                retval.Flag = Errors.Failed;
                retval.Message = $"Error updating entity: {ex.Message}";
                DMEEditor?.AddLogMessage("Beep", $"Error updating entity: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
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
        // ~ParquetDataSource()
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
        #region "Read Parquet"
        public ConnectionState GetFileState()
        {
            if (ConnectionStatus == ConnectionState.Open)
            {
                return ConnectionStatus;
            }
            else
            {
                return Openconnection();
            }

        }
        public IEnumerable<Row> ReadRows()
        {
            try
            {
                if (!File.Exists(CombineFilePath))
                {
                    yield break;
                }

                using (Stream fs = File.OpenRead(CombineFilePath))
                {
                    ParquetReader reader = ParquetReader.CreateAsync(fs).Result;
                    DataField[] dataFields = reader.Schema.GetDataFields();
                    for (int i = 0; i < reader.RowGroupCount; i++)
                    {
                        using (ParquetRowGroupReader groupReader = reader.OpenRowGroupReader(i))
                        {
                            IEnumerable<Row> rows = groupReader.ReadAsRows(dataFields);
                            foreach (Row row in rows)
                            {
                                yield return row;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error reading rows: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
        }

        public IEnumerable<T> ReadEntities<T>() where T : new()
        {
            try
            {
                if (!File.Exists(CombineFilePath))
                {
                    return new List<T>();
                }

                using (Stream fs = File.OpenRead(CombineFilePath))
                {
                    return ParquetSerializer.DeserializeAsync<T>(fs).Result;
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error reading entities: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return new List<T>();
            }
        }
        public async Task<IEnumerable<Parquet.Data.DataColumn>> GetSchemaColumns(string filepath)
        {
            List<Parquet.Data.DataColumn> ls = new List<Parquet.Data.DataColumn>(); ;
            List<EntityStructure> entities = new List<EntityStructure>();
            List<EntityField> fields = new List<EntityField>();
            Entities.Clear();
            EntitiesNames.Clear();
            using (Stream fs = File.OpenRead(filepath))
            {
                using (ParquetReader reader = await ParquetReader.CreateAsync(fs))
                {
                    for (int i = 0; i < reader.RowGroupCount; i++)
                    {
                        using (ParquetRowGroupReader rowGroupReader = reader.OpenRowGroupReader(i))
                        {
                            EntityStructure entity = new EntityStructure();
                            entity.EntityName = Path.GetFileNameWithoutExtension(filepath);
                            entity.OriginalEntityName = Path.GetFileNameWithoutExtension(filepath);
                            entity.DatasourceEntityName = Path.GetFileNameWithoutExtension(filepath);
                            entity.Fields = new List<EntityField>();
                            
                            if (!EntitiesNames.Contains(entity.EntityName))
                            {
                                EntitiesNames.Add(entity.EntityName);
                            }
                            foreach (DataField df in reader.Schema.GetDataFields())
                            {
                                EntityField field = new EntityField();
                                Parquet.Data.DataColumn columnData = await rowGroupReader.ReadColumnAsync(df);
                                ls.Add(columnData);
                                field.fieldname = df.Name;
                                field.fieldtype = df.ClrType.ToString();
                                field.BaseColumnName = df.Name;
                                entity.Fields.Add(field);
                            }
                            entities.Add(entity);
                          
                        }
                    }
                }
            }
          
           
           
            Entities.AddRange(entities);
           
           
            return ls;
        }
        public async Task<IList<T>> ReadData<T>(string filepath) where T : new()
        {
            using (Stream fs = System.IO.File.OpenRead(filepath))
            {
                return await ParquetSerializer.DeserializeAsync<T>(fs);
            }
                
        }

        public Task<double> GetScalarAsync(string query)
        {
            return Task.Run(() => GetScalar(query));
        }

        public double GetScalar(string query)
        {
            try
            {
                // Parquet files don't support scalar queries directly
                return 0.0;
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in GetScalar: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return 0.0;
            }
        }
        #endregion

        #region "Helper Methods"
        private void GetSheets()
        {
            try
            {
                EntitiesNames.Clear();
                Entities.Clear();
                var columns = GetSchemaColumns(CombineFilePath).Result;
                // Entities and EntitiesNames are populated in GetSchemaColumns
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in GetSheets: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
        }

        private EntityStructure GetSheetEntity(string EntityName)
        {
            try
            {
                if (Entities != null && Entities.Count > 0)
                {
                    return Entities.FirstOrDefault(e => e.EntityName.Equals(EntityName, StringComparison.OrdinalIgnoreCase) ||
                                                        e.OriginalEntityName.Equals(EntityName, StringComparison.OrdinalIgnoreCase));
                }
                
                // If not found, get schema from file
                var columns = GetSchemaColumns(CombineFilePath).Result;
                return Entities.FirstOrDefault(e => e.EntityName.Equals(EntityName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in GetSheetEntity: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return null;
            }
        }

        private bool EvaluateFilter(object value, string op, string filterValue)
        {
            try
            {
                if (value == null) return false;

                switch (op)
                {
                    case "==":
                    case "=":
                        return value.ToString().Equals(filterValue, StringComparison.OrdinalIgnoreCase);
                    case "!=":
                    case "<>":
                        return !value.ToString().Equals(filterValue, StringComparison.OrdinalIgnoreCase);
                    case ">":
                        if (double.TryParse(value.ToString(), out double val1) && double.TryParse(filterValue, out double val2))
                            return val1 > val2;
                        return string.Compare(value.ToString(), filterValue, StringComparison.OrdinalIgnoreCase) > 0;
                    case "<":
                        if (double.TryParse(value.ToString(), out double val3) && double.TryParse(filterValue, out double val4))
                            return val3 < val4;
                        return string.Compare(value.ToString(), filterValue, StringComparison.OrdinalIgnoreCase) < 0;
                    case ">=":
                        if (double.TryParse(value.ToString(), out double val5) && double.TryParse(filterValue, out double val6))
                            return val5 >= val6;
                        return string.Compare(value.ToString(), filterValue, StringComparison.OrdinalIgnoreCase) >= 0;
                    case "<=":
                        if (double.TryParse(value.ToString(), out double val7) && double.TryParse(filterValue, out double val8))
                            return val7 <= val8;
                        return string.Compare(value.ToString(), filterValue, StringComparison.OrdinalIgnoreCase) <= 0;
                    default:
                        return value.ToString().Contains(filterValue, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch
            {
                return false;
            }
        }
        #endregion
    }

}
