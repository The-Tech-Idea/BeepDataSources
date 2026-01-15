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
using HDF.PInvoke;
using System.Runtime.InteropServices;
using System.Text;

namespace TheTechIdea.Beep.FileManager
{
    [AddinAttribute(Category = DatasourceCategory.FILE, DatasourceType = DataSourceType.Hdf5, FileType = "h5,hdf5")]
    public class Hdf5DataSource : IDataSource
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
        private long _fileId = -1;

        public Hdf5DataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
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
                    _fileId = H5F.open(_filePath, H5F.ACC_RDONLY);
                    if (_fileId >= 0)
                    {
                        ConnectionStatus = ConnectionState.Open;
                        GetEntitesList();
                        DMEEditor?.AddLogMessage("Beep", $"Opened HDF5 file: {_filePath}", DateTime.Now, 0, null, Errors.Ok);
                    }
                    else
                    {
                        ConnectionStatus = ConnectionState.Closed;
                        ErrorObject.Flag = Errors.Failed;
                        ErrorObject.Message = "Failed to open HDF5 file";
                    }
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
                DMEEditor?.AddLogMessage("Beep", $"Error opening HDF5 file: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return ConnectionStatus;
        }

        public ConnectionState Closeconnection()
        {
            try
            {
                if (_fileId >= 0)
                {
                    H5F.close(_fileId);
                    _fileId = -1;
                }
                ConnectionStatus = ConnectionState.Closed;
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error closing HDF5 file: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return ConnectionStatus;
        }

        public IEnumerable<string> GetEntitesList()
        {
            List<string> datasetNames = new List<string>();
            try
            {
                if (ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (ConnectionStatus == ConnectionState.Open && _fileId >= 0)
                {
                    // Traverse HDF5 file structure to find datasets
                    TraverseHDF5Group(_fileId, "/", datasetNames);
                    EntitiesNames = datasetNames;
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error getting entities list: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return datasetNames;
        }

        private void TraverseHDF5Group(long groupId, string currentPath, List<string> datasetNames)
        {
            try
            {
                var info = new H5O.info_t();
                H5O.get_info(groupId, ref info);

                // Iterate through objects in the group
                var idxType = H5.index_t.NAME;
                var order = H5.iter_order_t.INC;
                ulong idx = 0;
                H5.iterate(groupId, idxType, order, ref idx, (long loc_id, IntPtr name, ref H5L.info_t info) =>
                {
                    string objectName = Marshal.PtrToStringAnsi(name);
                    string fullPath = currentPath == "/" ? $"/{objectName}" : $"{currentPath}/{objectName}";

                    var objId = H5O.open(loc_id, objectName);
                    if (objId >= 0)
                    {
                        var objInfo = new H5O.info_t();
                        H5O.get_info(objId, ref objInfo);

                        if (objInfo.type == H5O.type_t.DATASET)
                        {
                            datasetNames.Add(fullPath);
                        }
                        else if (objInfo.type == H5O.type_t.GROUP)
                        {
                            TraverseHDF5Group(objId, fullPath, datasetNames);
                        }

                        H5O.close(objId);
                    }
                    return 0;
                }, IntPtr.Zero);
            }
            catch
            {
                // Continue if traversal fails
            }
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

                if (ConnectionStatus == ConnectionState.Open && _fileId >= 0)
                {
                    var datasetId = H5D.open(_fileId, EntityName);
                    if (datasetId >= 0)
                    {
                        // Read dataset data
                        var spaceId = H5D.get_space(datasetId);
                        var typeId = H5D.get_type(datasetId);

                        // Get dataset dimensions
                        var dims = new long[2];
                        H5S.get_simple_extent_dims(spaceId, dims, null);

                        // Read data (simplified - actual implementation depends on data type)
                        // This is a placeholder - HDF5 data reading is complex
                        results.Add(new { Path = EntityName, Type = "HDF5 Dataset" });
                        
                        H5D.close(datasetId);
                    }
                }
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

                if (ConnectionStatus == ConnectionState.Open && _fileId >= 0)
                {
                    EntityStructure entity = new EntityStructure
                    {
                        EntityName = EntityName,
                        DatasourceEntityName = EntityName,
                        Fields = new List<EntityField>
                        {
                            new EntityField { FieldName = "Path", Fieldtype = "System.String", EntityName = EntityName },
                            new EntityField { FieldName = "DataType", Fieldtype = "System.String", EntityName = EntityName },
                            new EntityField { FieldName = "Dimensions", Fieldtype = "System.String", EntityName = EntityName }
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
            ErrorObject.Message = "SQL execution not supported for HDF5 files";
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
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Failed, Message = "Update operation not supported for HDF5 files. Files are read-only." };
            return retval;
        }

        public IErrorsInfo DeleteEntity(string EntityName, object DeletedDataRow)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Failed, Message = "Delete operation not supported for HDF5 files. Files are read-only." };
            return retval;
        }

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = "Script execution not supported for HDF5 files";
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
                        SourceDataSourceEntityName = entity.EntityName,
                       ScriptType=   DDLScriptType.CreateEntity,
                        Ddl = $"# HDF5 dataset: {entity.EntityName}"
                    });
                }
            }
            return scripts;
        }

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Failed, Message = "Insert operation not supported for HDF5 files. Files are read-only." };
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

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Failed, Message = "Bulk update not supported for HDF5 files" };
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