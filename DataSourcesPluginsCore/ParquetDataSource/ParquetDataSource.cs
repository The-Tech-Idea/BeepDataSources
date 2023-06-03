using Parquet.Rows;
using Parquet.Schema;
using Parquet;
using ParquetSharp;
using Parquet.Data;
using Parquet.File;
using System.Data;
using System.Text;
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
            throw new NotImplementedException();
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

        public List<string> GetEntitesList()
        {
            throw new NotImplementedException();
        }

        public object GetEntity(string EntityName, List<AppFilter> filter)
        {
            throw new NotImplementedException();
        }

        public Task<object> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            throw new NotImplementedException();
        }

        public List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            throw new NotImplementedException();
        }

        public int GetEntityIdx(string entityName)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            throw new NotImplementedException();
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
            using ParquetReader reader = new ParquetReader();
            DataField[] dataFields = reader.Schema.GetDataFields();
            foreach (RowGroupReader groupReader in reader.RowGroupReaders)
            {
                IEnumerable<Row> rows = groupReader.ReadAsRows(dataFields);
                foreach (Row row in rows)
                {
                    yield return row;
                }
            }
        }

        public IEnumerable<T> ReadEntities<T>() where T : new()
        {
            using var reader = new ParquetReader(filepath);
            return reader.Read<T>().ToList();
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
                            entity.EntityName = rowGroupReader.owGroup.ToString();
                            entity.OriginalEntityName = rowGroupReader.owGroup.ToString();
                            entity.DatasourceEntityName = rowGroupReader.owGroup.ToString();
                            EntitiesNames.Add(entity.EntityName);
                            foreach (DataField df in reader.Schema.GetDataFields())
                            {
                                EntityField field = new EntityField();
                                Parquet.Data.DataColumn columnData = await rowGroupReader.ReadColumnAsync(df);
                                ls.Add(columnData);
                                field.fieldname = df.Name;
                                field.fieldtype = df.ClrType.ToString();
                                fields.Add(field);
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
        #endregion
    }

}
