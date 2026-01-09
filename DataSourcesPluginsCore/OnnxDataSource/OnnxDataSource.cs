using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
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
using TheTechIdea.Beep.FileManager;
using Microsoft.ML.OnnxRuntime;
using System.Text;

namespace TheTechIdea.Beep.FileManager
{
    [AddinAttribute(Category = DatasourceCategory.FILE, DatasourceType = DataSourceType.ONNX, FileType = "onnx")]
    public class OnnxDataSource : IDataSource
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

        private string _filePath;
        private InferenceSession _session;
        private string _modelName;

        public OnnxDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = databasetype;
            Category = DatasourceCategory.FILE;

            Dataconnection = new FileConnection(DMEEditor)
            {
                Logger = logger,
                ErrorObject = ErrorObject
            };

            Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.FileName == datasourcename).FirstOrDefault();

            if (Dataconnection.ConnectionProp != null)
            {
                _filePath = Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName);
                _modelName = Path.GetFileNameWithoutExtension(Dataconnection.ConnectionProp.FileName);
            }

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
                if (File.Exists(_filePath))
                {
                    _session = new InferenceSession(_filePath);
                    ConnectionStatus = ConnectionState.Open;
                    GetEntitesList();
                    DMEEditor?.AddLogMessage("Beep", $"Loaded ONNX model: {_filePath}", DateTime.Now, 0, null, Errors.Ok);
                }
                else
                {
                    ConnectionStatus = ConnectionState.Closed;
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = $"File not found: {_filePath}";
                }
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Closed;
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error loading ONNX model: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return ConnectionStatus;
        }

        public ConnectionState Closeconnection()
        {
            try
            {
                if (_session != null)
                {
                    _session.Dispose();
                    _session = null;
                }
                ConnectionStatus = ConnectionState.Closed;
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error closing ONNX model: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return ConnectionStatus;
        }

        public IEnumerable<string> GetEntitesList()
        {
            List<string> inputNames = new List<string>();
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open && _session != null)
                {
                    // Get model inputs (entities are represented as model inputs/outputs)
                    foreach (var input in _session.InputMetadata)
                    {
                        inputNames.Add($"Input_{input.Key}");
                    }

                    // Also add outputs
                    foreach (var output in _session.OutputMetadata)
                    {
                        inputNames.Add($"Output_{output.Key}");
                    }

                    // Add model name as main entity
                    if (!string.IsNullOrEmpty(_modelName) && !inputNames.Contains(_modelName))
                    {
                        inputNames.Insert(0, _modelName);
                    }

                    EntitiesNames = inputNames;
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error getting entities list: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return inputNames;
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

                if (ConnectionStatus == ConnectionState.Open && _session != null)
                {
                    // For ONNX models, "GetEntity" means running inference
                    // EntityName could be an input name or the model name
                    // Filters represent input values for inference

                    if (EntityName == _modelName || EntityName.StartsWith("Input_") || EntityName.StartsWith("Output_"))
                    {
                        // Build input tensor from filters
                        var inputContainer = new List<NamedOnnxValue>();
                        
                        if (filter != null && filter.Count > 0)
                        {
                            foreach (var f in filter)
                            {
                                // Find matching input metadata
                                var inputMeta = _session.InputMetadata.FirstOrDefault(m => m.Key.Equals(f.FieldName, StringComparison.OrdinalIgnoreCase));
                                if (inputMeta.Key != null)
                                {
                                    var value = ConvertFilterValue(f.FilterValue, f.valueType, inputMeta.Value.ElementType);
                                    var tensor = CreateTensor(value, inputMeta.Value);
                                    inputContainer.Add(NamedOnnxValue.CreateFromTensor(inputMeta.Key, tensor));
                                }
                            }
                        }
                        else
                        {
                            // Use default values or return model metadata
                            foreach (var input in _session.InputMetadata)
                            {
                                var defaultValue = CreateDefaultTensor(input.Value);
                                inputContainer.Add(NamedOnnxValue.CreateFromTensor(input.Key, defaultValue));
                            }
                        }

                        // Run inference
                        using (var resultsDisposable = _session.Run(inputContainer))
                        {
                            var resultDict = new Dictionary<string, object>();
                            foreach (var result in resultsDisposable)
                            {
                                resultDict[result.Name] = ConvertTensorToObject(result.Value);
                            }
                            results.Add(resultDict);
                        }
                    }
                    else
                    {
                        // Return model metadata
                        results.Add(new { ModelName = _modelName, Type = "ONNX Model" });
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error getting entity (running inference): {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return results;
        }

        private object ConvertFilterValue(string value, string type, TensorElementType elementType)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            switch (elementType)
            {
                case TensorElementType.Float:
                    return float.Parse(value);
                case TensorElementType.Double:
                    return double.Parse(value);
                case TensorElementType.Int32:
                    return int.Parse(value);
                case TensorElementType.Int64:
                    return long.Parse(value);
                case TensorElementType.String:
                    return value;
                case TensorElementType.Bool:
                    return bool.Parse(value);
                default:
                    return value;
            }
        }

        private Tensor<float> CreateDefaultTensor(NodeMetadata metadata)
        {
            // Create a default tensor with zeros or ones based on shape
            var shape = metadata.Dimensions.Select(d => d > 0 ? (int)d : 1).ToArray();
            var totalElements = shape.Aggregate(1, (a, b) => a * b);
            var data = new float[totalElements];
            return new DenseTensor<float>(data, shape);
        }

        private Tensor<float> CreateTensor(object value, NodeMetadata metadata)
        {
            var shape = metadata.Dimensions.Select(d => d > 0 ? (int)d : 1).ToArray();
            
            if (value is float floatVal)
            {
                var totalElements = shape.Aggregate(1, (a, b) => a * b);
                var data = new float[totalElements];
                for (int i = 0; i < totalElements; i++)
                {
                    data[i] = floatVal;
                }
                return new DenseTensor<float>(data, shape);
            }
            else if (value is float[] arrayVal)
            {
                return new DenseTensor<float>(arrayVal, shape);
            }
            
            // Default: create zero tensor
            var defaultTotal = shape.Aggregate(1, (a, b) => a * b);
            var defaultData = new float[defaultTotal];
            return new DenseTensor<float>(defaultData, shape);
        }

        private object ConvertTensorToObject(DisposableNamedOnnxValue value)
        {
            if (value.Value is Tensor<float> floatTensor)
            {
                return floatTensor.ToArray();
            }
            else if (value.Value is Tensor<double> doubleTensor)
            {
                return doubleTensor.ToArray();
            }
            else if (value.Value is Tensor<int> intTensor)
            {
                return intTensor.ToArray();
            }
            else if (value.Value is Tensor<long> longTensor)
            {
                return longTensor.ToArray();
            }
            else if (value.Value is Tensor<string> stringTensor)
            {
                return stringTensor.ToArray();
            }
            else if (value.Value is Tensor<bool> boolTensor)
            {
                return boolTensor.ToArray();
            }
            
            return value.Value?.ToString() ?? "";
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

                if (ConnectionStatus == ConnectionState.Open && _session != null)
                {
                    EntityStructure entity = new EntityStructure
                    {
                        EntityName = EntityName,
                        DatasourceEntityName = EntityName,
                        Fields = new List<EntityField>()
                    };

                    // If EntityName is the model name, include all inputs and outputs
                    if (EntityName == _modelName)
                    {
                        // Add input fields
                        foreach (var input in _session.InputMetadata)
                        {
                            entity.Fields.Add(new EntityField
                            {
                                fieldname = input.Key,
                                Originalfieldname = input.Key,
                                fieldtype = GetDotNetType(input.Value.ElementType),
                                EntityName = EntityName,
                                FieldIndex = entity.Fields.Count
                            });
                        }

                        // Add output fields
                        foreach (var output in _session.OutputMetadata)
                        {
                            entity.Fields.Add(new EntityField
                            {
                                fieldname = $"Output_{output.Key}",
                                Originalfieldname = output.Key,
                                fieldtype = GetDotNetType(output.Value.ElementType),
                                EntityName = EntityName,
                                FieldIndex = entity.Fields.Count
                            });
                        }
                    }
                    else if (EntityName.StartsWith("Input_"))
                    {
                        // Specific input
                        var inputName = EntityName.Replace("Input_", "");
                        if (_session.InputMetadata.ContainsKey(inputName))
                        {
                            var input = _session.InputMetadata[inputName];
                            entity.Fields.Add(new EntityField
                            {
                                fieldname = inputName,
                                Originalfieldname = inputName,
                                fieldtype = GetDotNetType(input.ElementType),
                                EntityName = EntityName
                            });
                        }
                    }
                    else if (EntityName.StartsWith("Output_"))
                    {
                        // Specific output
                        var outputName = EntityName.Replace("Output_", "");
                        if (_session.OutputMetadata.ContainsKey(outputName))
                        {
                            var output = _session.OutputMetadata[outputName];
                            entity.Fields.Add(new EntityField
                            {
                                fieldname = outputName,
                                Originalfieldname = outputName,
                                fieldtype = GetDotNetType(output.ElementType),
                                EntityName = EntityName
                            });
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

        private string GetDotNetType(TensorElementType elementType)
        {
            switch (elementType)
            {
                case TensorElementType.Float:
                    return "System.Single";
                case TensorElementType.Double:
                    return "System.Double";
                case TensorElementType.Int32:
                    return "System.Int32";
                case TensorElementType.Int64:
                    return "System.Int64";
                case TensorElementType.String:
                    return "System.String";
                case TensorElementType.Bool:
                    return "System.Boolean";
                default:
                    return "System.Object";
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
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = "SQL execution not supported for ONNX models";
            DMEEditor?.AddLogMessage("Beep", "SQL execution not supported for ONNX models", DateTime.Now, 0, null, Errors.Failed);
            return ErrorObject;
        }

        public IEnumerable<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            return new List<ChildRelation>();
        }

        public IEnumerable<object> RunQuery(string qrystr)
        {
            // ONNX models don't support queries in traditional sense
            // Could parse query as JSON input specification
            DMEEditor?.AddLogMessage("Beep", "Query execution not fully supported for ONNX models. Use GetEntity with filters for inference.", DateTime.Now, 0, null, Errors.Failed);
            return Enumerable.Empty<object>();
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Failed, Message = "Update operation not supported for ONNX models. Models are read-only." };
            DMEEditor?.AddLogMessage("Beep", "Update operation not supported for ONNX models", DateTime.Now, 0, null, Errors.Failed);
            return retval;
        }

        public IErrorsInfo DeleteEntity(string EntityName, object DeletedDataRow)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Failed, Message = "Delete operation not supported for ONNX models. Models are read-only." };
            DMEEditor?.AddLogMessage("Beep", "Delete operation not supported for ONNX models", DateTime.Now, 0, null, Errors.Failed);
            return retval;
        }

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = "Script execution not supported for ONNX models";
            DMEEditor?.AddLogMessage("Beep", "Script execution not supported for ONNX models", DateTime.Now, 0, null, Errors.Failed);
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
                        ScriptText = $"# ONNX model entity: {entity.EntityName}\n# Model inputs/outputs defined in ONNX file"
                    });
                }
            }
            return scripts;
        }

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Failed, Message = "Insert operation not supported for ONNX models. Models are read-only." };
            DMEEditor?.AddLogMessage("Beep", "Insert operation not supported for ONNX models", DateTime.Now, 0, null, Errors.Failed);
            return retval;
        }

        public virtual double GetScalar(string query)
        {
            try
            {
                // For ONNX, scalar could mean a single output value
                // This is a placeholder implementation
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

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Failed, Message = "Bulk update not supported for ONNX models" };
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