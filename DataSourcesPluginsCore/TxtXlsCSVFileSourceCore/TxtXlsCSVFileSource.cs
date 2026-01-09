using System.Data;
using System.Linq;
using System.Collections.Generic;

using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Vis;
using ExcelDataReader;
using System.Text;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.FileManager
{
    [AddinAttribute(Category = DatasourceCategory.FILE, DatasourceType = DataSourceType.CSV|DataSourceType.Xls,FileType = "xls,xlsx") ]
    public class TxtXlsCSVFileSource : IDataSource

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
        ExcelReaderConfiguration ReaderConfig;
        ExcelDataSetConfiguration ExcelDataSetConfig;
        public DataTable FileData { get; set; }
        IExcelDataReader reader;
        bool IsFileRead = false;
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
           
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            SetupConfig();
        }
        public virtual IErrorsInfo BeginTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Beep", $"Error in Begin Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        public virtual IErrorsInfo EndTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Beep", $"Error in end Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        public virtual IErrorsInfo Commit(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Beep", $"Error in Begin Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
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
                
                ent=Entities[Entities.FindIndex(x => x.EntityName == EntityName)];
            }

            return ent;
        }
        public Type GetEntityType(string EntityName)
        {
           

            if (GetFileState() == ConnectionState.Open)
            {
                EntityStructure ent = null;
                if (Entities != null)
                {
                    if (Entities.Count() == 0)
                    {
                        GetEntitesList();
                    }

                    ent =Entities[GetEntityIdx(EntityName)];
                  
                    DMTypeBuilder.CreateNewObject(DMEEditor, "TheTechIdea.Classes", EntityName, ent.Fields);
                }
             
                return DMTypeBuilder.MyType;
            }
            return null;
        }
        public  IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            ErrorObject.Flag = Errors.Ok;
            object data=null;
            try
            {

                DataTable dt=null;
                string qrystr="";
                EntityStructure entity=GetEntityStructure(EntityName);

                int fromline = entity.StartRow;
                int toline = entity.EndRow;
                if (GetFileState() == ConnectionState.Open)
                {
                    if (Entities != null)
                    {
                        if (Entities.Count() == 0)
                        {
                            GetEntitesList();
                        }
                       
                    }

                    if (filter != null)
                    {
                        if(filter.Count > 0)
                        {
                            AppFilter fromlinefilter = filter.FirstOrDefault(p => p.FieldName.Equals("FromLine", StringComparison.InvariantCultureIgnoreCase));
                            if (fromlinefilter != null)
                            {
                                fromline =Convert.ToInt32(fromlinefilter.FilterValue);
                            }
                            AppFilter Tolinefilter = filter.FirstOrDefault(p => p.FieldName.Equals("ToLine", StringComparison.InvariantCultureIgnoreCase));
                            if (fromlinefilter != null)
                            {
                                 toline = Convert.ToInt32(fromlinefilter.FilterValue);
                            }
                        }
                    }
                    int idx = GetEntityIdx(EntityName);
                    //if(Entities.Count > 0)
                    //{
                    //    idx = Entities.FindIndex(p => p.EntityName.Equals(EntityName, StringComparison.InvariantCultureIgnoreCase));
                     
                    //}
                    //if (idx == -1)
                    //{
                    //    idx = Entities.FindIndex(p => p.OriginalEntityName.Equals(EntityName, StringComparison.InvariantCultureIgnoreCase));

                    //}
                    //if (idx == -1)
                    //{
                    //    idx = Entities.FindIndex(p => p.DatasourceEntityName.Equals(EntityName, StringComparison.InvariantCultureIgnoreCase));

                    //}
                    
                    if (idx > -1)
                    {
                        entity = Entities[idx];
                        dt = ReadDataTable(entity.OriginalEntityName, HeaderExist, fromline, toline);
                        SyncFieldTypes(ref dt, EntityName);
                        if (filter != null)
                        {
                            if (filter.Where(p => !string.IsNullOrEmpty(p.FilterValue) && !string.IsNullOrWhiteSpace(p.FilterValue) && !string.IsNullOrEmpty(p.Operator) && !string.IsNullOrWhiteSpace(p.Operator)).Any())
                            {

                                foreach (AppFilter item in filter.Where(p => !string.IsNullOrEmpty(p.FilterValue) && !string.IsNullOrWhiteSpace(p.FilterValue) && !string.IsNullOrEmpty(p.Operator) && !string.IsNullOrWhiteSpace(p.Operator) && !p.FieldName.Equals("ToLine", StringComparison.InvariantCultureIgnoreCase) && !p.FieldName.Equals("FromLine", StringComparison.InvariantCultureIgnoreCase)))
                                {
                                    if (!string.IsNullOrEmpty(item.FilterValue) && !string.IsNullOrWhiteSpace(item.FilterValue))
                                    {
                                        //  EntityField f = ent.Fields.Where(i => i.fieldname == item.FieldName).FirstOrDefault();
                                        if (item.Operator.ToLower() == "between")
                                        {
                                            if (item.valueType == "System.DateTime")
                                            {
                                                qrystr += "[" + item.FieldName + "] " + item.Operator + " '" + DateTime.Parse(item.FilterValue) + "' and  '" + DateTime.Parse(item.FilterValue1) + "'" + Environment.NewLine;
                                            }
                                            else
                                            {
                                                qrystr += "[" + item.FieldName + "] " + item.Operator + " " + item.FilterValue + " and  " + item.FilterValue1 + " " + Environment.NewLine;
                                            }
                                        }
                                        else
                                        {
                                            if (item.valueType == "System.String")
                                            {
                                                qrystr += "[" + item.FieldName + "] " + item.Operator + " '" + item.FilterValue + "' " + Environment.NewLine;
                                            }
                                            else
                                            {
                                                qrystr += "[" + item.FieldName + "] " + item.Operator + " " + item.FilterValue + " " + Environment.NewLine;
                                            }

                                        }
                                    }
                                }
                            }
                            if (!string.IsNullOrEmpty(qrystr))
                            {
                                dt = dt.Select(qrystr).CopyToDataTable();
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
        public  TypeCode ToConvert( Type dest)
        {
            TypeCode retval = TypeCode.String;
           switch (dest.ToString())
            {
                case "System.String":
                    retval = TypeCode.String;
                    break;
                case "System.Decimal":
                    retval = TypeCode.Decimal;
                    break;
                case "System.DateTime":
                    retval = TypeCode.DateTime;
                    break;
                case "System.Char":
                    retval = TypeCode.Char;
                    break;
                case "System.Boolean":
                    retval = TypeCode.Boolean;
                    break;
                case "System.DBNull":
                    retval = TypeCode.DBNull;
                    break;
                case "System.Byte":
                    retval = TypeCode.Byte;
                    break;
                case "System.Int16":
                    retval = TypeCode.Int16;
                    break;
                case "System.Double":
                    retval = TypeCode.Double;
                    break;
                case "System.Int32":
                    retval = TypeCode.Int32;
                    break;
                case "System.Int64":
                    retval = TypeCode.Int64;
                    break;
                case "System.Single":
                    retval = TypeCode.Single;
                    break;
                case "System.Object":
                    retval = TypeCode.String;

                    break;
                   

            }
            return retval;
        }
        private void SyncFieldTypes(ref DataTable dt, string EntityName)
        {
            EntityStructure ent = GetEntityStructure(EntityName);
            DataTable newdt = new DataTable(EntityName);
            if (ent != null)
            {
                foreach (var item in ent.Fields)
                {
                    DataColumn cl = new DataColumn(item.fieldname, Type.GetType(item.fieldtype));
                    newdt.Columns.Add(cl);
                    //dt.Columns[item.fieldname].DataType = Type.GetType(item.fieldtype);
                }
                if (dt != null)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        try
                        {
                            DataRow r = newdt.NewRow();
                            foreach (var item in ent.Fields)
                            {
                                if (dr[item.Originalfieldname] != DBNull.Value)
                                {
                                    string st = dr[item.Originalfieldname].ToString().Trim();
                                    if (!string.IsNullOrEmpty(st) && !string.IsNullOrWhiteSpace(st))
                                    {

                                        r[item.fieldname] = Convert.ChangeType(dr[item.Originalfieldname], ToConvert(Type.GetType(item.fieldtype)));
                                    }

                                }


                            }
                            try
                            {
                                newdt.Rows.Add(r);
                            }
                            catch (Exception aa)
                            {


                            }

                        }
                        catch (Exception ex)
                        {

                            // throw;
                        }

                    }
                }
               
            }
            dt = newdt;
        }
        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            ErrorObject.Flag = Errors.Ok;

            try
            {

                Logger.WriteLog("Successfully Retrieve Entites list ");

            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Unsuccessfully Retrieve Entites list {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
            }

            return ErrorObject;
        }
        public IEnumerable<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            return null;
        }
        public DataSet GetChildTablesListFromCustomQuery(string tablename, string customquery)
        {
            // CSV/Excel files don't have child tables
            return new DataSet();
        }
        public IEnumerable<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            // CSV/Excel files don't have foreign keys
            return new List<RelationShipKeys>();
        }
        public IErrorsInfo ExecuteSql(string sql)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                // CSV/Excel files don't support SQL
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = "SQL execution not supported for CSV/Excel files";
                DMEEditor?.AddLogMessage("Beep", "SQL execution not supported for CSV/Excel files", DateTime.Now, -1, null, Errors.Failed);
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
            }
            return ErrorObject;
        }
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
                return true;
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in CreateEntityAs: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
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
        public EntityStructure GetEntityStructure(string EntityName,bool refresh=false )
        {
            EntityStructure retval = null;
            
            if (GetFileState() == ConnectionState.Open)
            {
                 if(Entities!= null)
                {
                    if (Entities.Count == 0) 
                    {
                        IsFileRead = false;
                        GetSheets();
                      
                    }
                }
                int idx = GetEntityIdx(EntityName);
                retval = Entities[idx];
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
                        IsFileRead = false;
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
                        IsFileRead = false;
                        GetSheets();
                      
                    }
                }
                int idx = GetEntityIdx(fnd.EntityName);
                retval = Entities[idx];
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
                        IsFileRead=false;
                         GetSheets();
                       
                    }
                DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities { datasourcename = DatasourceName, Entities = Entities });
            }
            return retval;
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
                retval.Flag = Errors.Failed;
                retval.Message = "Update operation not supported for CSV/Excel files. Files are read-only.";
                DMEEditor?.AddLogMessage("Beep", "Update operation not supported for CSV/Excel files", DateTime.Now, -1, null, Errors.Failed);
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
                            scriptType = DDLScriptType.CreateEntity,
                          
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
        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok, Message = "Data inserted successfully." };
            try
            {
                // CSV/Excel files are typically read-only in this implementation
                // Insert would require rewriting the file
                retval.Flag = Errors.Failed;
                retval.Message = "Insert operation not supported for CSV/Excel files. Files are read-only.";
                DMEEditor?.AddLogMessage("Beep", "Insert operation not supported for CSV/Excel files", DateTime.Now, -1, null, Errors.Failed);
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
        private void SetupConfig()
        {
            ReaderConfig = new ExcelReaderConfiguration()
            {
                // Gets or sets the encoding to use when the input XLS lacks a CodePage
                // record, or when the input CSV lacks a BOM and does not parse as UTF8. 
                // Default: cp1252 (XLS BIFF2-5 and CSV only)
                FallbackEncoding = Encoding.GetEncoding(1252),

                //// Gets or sets the password used to open password protected workbooks.
                //Password = "password",

                // Gets or sets an array of CSV separator candidates. The reader 
                // autodetects which best fits the input data. Default: , ; TAB | # 
                // (CSV only)
                AutodetectSeparators = new char[] { ',', ';', '\t', '|', '#' },

                // Gets or sets a value indicating whether to leave the stream open after
                // the IExcelDataReader object is disposed. Default: false
                LeaveOpen = false,

                // Gets or sets a value indicating the number of rows to analyze for
                // encoding, separator and field count in a CSV. When set, this option
                // causes the IExcelDataReader.RowCount property to throw an exception.
                // Default: 0 - analyzes the entire file (CSV only, has no effect on other
                // formats)
                AnalyzeInitialCsvRows = 0,
            };
            ExcelDataSetConfig = new ExcelDataSetConfiguration()
            {
                // Gets or sets a value indicating whether to set the DataColumn.DataType 
                // property in a second pass.
                UseColumnDataType = true,

                // Gets or sets a callback to determine whether to include the current sheet
                // in the DataSet. Called once per sheet before ConfigureDataTable.
                FilterSheet = (tableReader, sheetIndex) => true,

                // Gets or sets a callback to obtain configuration options for a DataTable. 
                ConfigureDataTable = (tableReader) => new ExcelDataTableConfiguration()
                {
                    // Gets or sets a value indicating the prefix of generated column names.
                    //EmptyColumnNamePrefix = "Column",

                    // Gets or sets a value indicating whether to use a row from the 
                    // data as column names.
                    UseHeaderRow = true,


                    // Gets or sets a callback to determine which row is the header row. 
                    // Only called when UseHeaderRow = true.
                    ReadHeaderRow = (rowReader) =>
                    {
                        // F.ex skip the first row and use the 2nd row as column headers:
                        // rowReader.Read();

                    },

                    // Gets or sets a callback to determine whether to include the 
                    // current row in the DataTable.
                    FilterRow = (rowReader) =>
                    {
                        //return true;
                        var hasData = false;
                        for (var u = 0; u < rowReader.FieldCount; u++)
                        {
                            if (rowReader[u] == null || string.IsNullOrEmpty(rowReader[u].ToString()))
                            {
                                continue;
                            }
                            else
                            {
                                hasData = true;
                                break;
                            }


                        }

                        return hasData;
                    },

                    // Gets or sets a callback to determine whether to include the specific
                    // column in the DataTable. Called once per column after reading the 
                    // headers.
                    FilterColumn = (rowReader, columnIndex) =>
                    {
                        return true;
                    }
                }
            };

        }
        private ExcelReaderConfiguration GetReaderConfiguration()
        {
            ExcelReaderConfiguration ReaderConfig = new ExcelReaderConfiguration()
            {
                // Gets or sets the encoding to use when the input XLS lacks a CodePage
                // record, or when the input CSV lacks a BOM and does not parse as UTF8. 
                // Default: cp1252 (XLS BIFF2-5 and CSV only)
                FallbackEncoding = Encoding.GetEncoding(1252),

                //// Gets or sets the password used to open password protected workbooks.
                //Password = "password",

                // Gets or sets an array of CSV separator candidates. The reader 
                // autodetects which best fits the input data. Default: , ; TAB | # 
                // (CSV only)
                AutodetectSeparators = new char[] { ',', ';', '\t', '|', '#' },

                // Gets or sets a value indicating whether to leave the stream open after
                // the IExcelDataReader object is disposed. Default: false
                LeaveOpen = false,

                // Gets or sets a value indicating the number of rows to analyze for
                // encoding, separator and field count in a CSV. When set, this option
                // causes the IExcelDataReader.RowCount property to throw an exception.
                // Default: 0 - analyzes the entire file (CSV only, has no effect on other
                // formats)
                AnalyzeInitialCsvRows = 0,
            };
          
            return ReaderConfig;

        }
        private ExcelDataSetConfiguration GetDataSetConfiguration(int sheetidx,int startrow)
        {

            ExcelDataSetConfiguration  ExcelDataSetConfig = new ExcelDataSetConfiguration()
            {
                // Gets or sets a value indicating whether to set the DataColumn.DataType 
                // property in a second pass.
                UseColumnDataType = true,

                // Gets or sets a callback to determine whether to include the current sheet
                // in the DataSet. Called once per sheet before ConfigureDataTable.
                FilterSheet = (tableReader, sheetIndex) =>
                {
                   return sheetidx == sheetIndex;
                },

                // Gets or sets a callback to obtain configuration options for a DataTable. 
                ConfigureDataTable = (tableReader) => new ExcelDataTableConfiguration()
                {
                    // Gets or sets a value indicating the prefix of generated column names.
                    //EmptyColumnNamePrefix = "Column",

                    // Gets or sets a value indicating whether to use a row from the 
                    // data as column names.
                    UseHeaderRow = true,


                    // Gets or sets a callback to determine which row is the header row. 
                    // Only called when UseHeaderRow = true.
                    ReadHeaderRow = (rowReader) =>
                    {
                        // F.ex skip the first row and use the 2nd row as column headers:
                        for (int i = 0; i < startrow; i++)
                        {
                            rowReader.Read();
                        }
                      
                    },

                    // Gets or sets a callback to determine whether to include the 
                    // current row in the DataTable.
                    FilterRow = (rowReader) =>
                    {
                        //return true;
                        var hasData = false;
                        for (var u = 0; u < rowReader.FieldCount; u++)
                        {
                            if (rowReader[u] == null || string.IsNullOrEmpty(rowReader[u].ToString()))
                            {
                                continue;
                            }
                            else
                            {
                                hasData = true;
                                break;
                            }


                        }

                        return hasData;
                    },

                    // Gets or sets a callback to determine whether to include the specific
                    // column in the DataTable. Called once per column after reading the 
                    // headers.
                    FilterColumn = (rowReader, columnIndex) =>
                    {
                        return true;
                    }
                }
            };
            return ExcelDataSetConfig;

        }
        private ExcelDataSetConfiguration GetDataSetConfiguration(string sheetname, int startrow)
        {

            ExcelDataSetConfiguration ExcelDataSetConfig = new ExcelDataSetConfiguration()
            {
                // Gets or sets a value indicating whether to set the DataColumn.DataType 
                // property in a second pass.
                UseColumnDataType = true,

                // Gets or sets a callback to determine whether to include the current sheet
                // in the DataSet. Called once per sheet before ConfigureDataTable.
                FilterSheet = (tableReader, sheetIndex) =>
                {
                    return tableReader.Name.Equals(sheetname,StringComparison.InvariantCultureIgnoreCase);
                },

                // Gets or sets a callback to obtain configuration options for a DataTable. 
                ConfigureDataTable = (tableReader) => new ExcelDataTableConfiguration()
                {
                    // Gets or sets a value indicating the prefix of generated column names.
                    //EmptyColumnNamePrefix = "Column",

                    // Gets or sets a value indicating whether to use a row from the 
                    // data as column names.
                    UseHeaderRow = true,


                    // Gets or sets a callback to determine which row is the header row. 
                    // Only called when UseHeaderRow = true.
                    ReadHeaderRow = (rowReader) =>
                    {
                        // F.ex skip the first row and use the 2nd row as column headers:
                        for (int i = 0; i < startrow; i++)
                        {
                            rowReader.Read();
                        }

                    },

                    // Gets or sets a callback to determine whether to include the 
                    // current row in the DataTable.
                    FilterRow = (rowReader) =>
                    {
                        //return true;
                        var hasData = false;
                        for (var u = 0; u < rowReader.FieldCount; u++)
                        {
                            if (rowReader[u] == null || string.IsNullOrEmpty(rowReader[u].ToString()))
                            {
                                continue;
                            }
                            else
                            {
                                hasData = true;
                                break;
                            }


                        }

                        return hasData;
                    },

                    // Gets or sets a callback to determine whether to include the specific
                    // column in the DataTable. Called once per column after reading the 
                    // headers.
                    FilterColumn = (rowReader, columnIndex) =>
                    {
                        return true;
                    }
                }
            };
            return ExcelDataSetConfig;

        }
        private DataTable GetDataTableforSheet(int sheetidx, int startrow)
        {
            DataSet ds = new DataSet();
            using (var stream = File.Open(Path.Combine(FilePath, FileName), FileMode.Open, FileAccess.Read))
            {
                switch (Dataconnection.ConnectionProp.Ext.Replace(".", "").ToLower())
                {
                    case "csv":
                        reader = ExcelReaderFactory.CreateCsvReader(stream, GetReaderConfiguration());
                        break;
                    case "xls":
                        reader = ExcelReaderFactory.CreateBinaryReader(stream, GetReaderConfiguration());
                        break;
                    case "xlsx":
                        reader = ExcelReaderFactory.CreateOpenXmlReader(stream, GetReaderConfiguration());
                        break;
                    default:
                        throw new Exception("ExcelDataReaderFactory() - unknown/unsupported file extension");
                        // break;
                }


                // 2. Use the AsDataSet extension method

                ds = reader.AsDataSet(GetDataSetConfiguration(sheetidx, startrow));
                // The result of each spreadsheet is in result.Tables

                stream.Close();
            }
            return ds.Tables[sheetidx];
        }
        private DataTable GetDataTableforSheet(string sheetname, int startrow)
        {
            DataSet ds = new DataSet();
            using (var stream = File.Open(Path.Combine(FilePath, FileName), FileMode.Open, FileAccess.Read))
            {
                switch (Dataconnection.ConnectionProp.Ext.Replace(".", "").ToLower())
                {
                    case "csv":
                        reader = ExcelReaderFactory.CreateCsvReader(stream, GetReaderConfiguration());
                        break;
                    case "xls":
                        reader = ExcelReaderFactory.CreateBinaryReader(stream, GetReaderConfiguration());
                        break;
                    case "xlsx":
                        reader = ExcelReaderFactory.CreateOpenXmlReader(stream, GetReaderConfiguration());
                        break;
                    default:
                        throw new Exception("ExcelDataReaderFactory() - unknown/unsupported file extension");
                        // break;
                }


                // 2. Use the AsDataSet extension method
             
                ds = reader.AsDataSet(GetDataSetConfiguration(sheetname, startrow));
                // The result of each spreadsheet is in result.Tables

                stream.Close();
            }
            return ds.Tables[sheetname];
        }
        private DataSet GetExcelDataSet()   
        {
            FilePath = Dataconnection.ConnectionProp.FilePath;
            string filpath = Path.Combine(FilePath, FileName);
            if (string.IsNullOrEmpty(Dataconnection.ConnectionProp.Ext))
            {
                Dataconnection.ConnectionProp.Ext= Path.GetExtension(FileName);
            }
            DataSet ds = new DataSet();
            if (File.Exists(filpath))
            {
                using (var stream = File.Open(Path.Combine(FilePath, FileName), FileMode.Open, FileAccess.Read))
                {
                    switch (Dataconnection.ConnectionProp.Ext.Replace(".", "").ToLower())
                    {
                        case "csv":
                            reader = ExcelReaderFactory.CreateCsvReader(stream, ReaderConfig);
                            break;
                        case "xls":
                            reader = ExcelReaderFactory.CreateBinaryReader(stream, ReaderConfig);
                            break;
                        case "xlsx":
                            reader = ExcelReaderFactory.CreateOpenXmlReader(stream, ReaderConfig);
                            break;
                        default:
                            throw new Exception("ExcelDataReaderFactory() - unknown/unsupported file extension");
                            // break;
                    }


                    // 2. Use the c extension method
                    ds = reader.AsDataSet(ExcelDataSetConfig);
                    // The result of each spreadsheet is in result.Tables

                    stream.Close();
                }
            }
         
            return ds;
        }
       
        private void GetSheets()
        {
            DataSet ds;
           
            if (GetFileState() == ConnectionState.Open && !IsFileRead)
            {
                try
                {
                    string sheetname;
                    ds = GetExcelDataSet();
                    if(ds.Tables.Count==0)
                    {
                        DMEEditor.AddLogMessage("Fail", $"Error in getting File format {FileName}  or missing file", DateTime.Now, 0, FileName, Errors.Failed);
                        return;
                    }
                    EntitiesNames = new List<string>();
                    Entities = new List<EntityStructure>();
                    for (int i = 0; i <= ds.Tables.Count-1; i++)
                    {
                        DataTable tb=ds.Tables[i];
                      
                            EntityStructure entityData = new EntityStructure();
                            entityData = new EntityStructure();
                            string filename = Path.GetFileNameWithoutExtension(DatasourceName);
                            filename = Regex.Replace(filename, @"[\s-]+", "_");
                            if (tb.TableName.StartsWith("Sheet"))
                            {
                                sheetname = filename+i;
                            }else
                                sheetname=tb.TableName;
                            entityData.Viewtype = ViewType.File;
                            entityData.DatabaseType = DataSourceType.Text;
                            entityData.DataSourceID = FileName;
                            entityData.DatasourceEntityName = sheetname;
                            entityData.Caption = sheetname;
                            entityData.EntityName = sheetname;
                            entityData.Id = i;
                            i++;
                            entityData.OriginalEntityName = sheetname;
                            entityData.Drawn = true;
                            EntitiesNames.Add(sheetname);
                            entityData.Fields = new List<EntityField>();
                            entityData.Fields.AddRange(GetFieldsbyTableScan( tb,tb.TableName, tb.Columns));
                            entityData.Drawn = true;
                        
                        Entities.Add(entityData);
                    }

                    IsFileRead = true;
                }
                catch (Exception ex)
                {
                    DMEEditor.AddLogMessage("Fail", $"Error in getting File format {ex.Message}", DateTime.Now, 0, FileName, Errors.Failed);

                }
            }
        }
        private EntityStructure GetSheetEntity(string EntityName)
        {
            EntityStructure entityData = new EntityStructure();
            if (GetFileState() == ConnectionState.Open)
            {
                try
                {
                    GetSheets();
                    if (Entities != null)
                    {
                      entityData = Entities[GetEntityIdx(EntityName)];
                    }
                }
                catch (Exception ex)
                {
                    entityData = null;
                    DMEEditor.AddLogMessage("Fail", $"Error in getting Entity from File  {ex.Message}", DateTime.Now, 0, FileName, Errors.Failed);

                }
            }

            return entityData;


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
            int retval = 0;
            if (ls.Tables.Count == 1)
            {
                retval = 0;
            }
            else
            {
                if (ls.Tables.Count == 0)
                {
                    retval = -1;
                }
                else
                {
                    if (ls.Tables.Count > 1)
                    {
                        int i = 0;
                        string found = "NotFound";
                        while (found == "Found" || found == "ExitandNotFound")
                        {

                            if (ls.Tables[i].TableName.Equals(sheetname,StringComparison.InvariantCultureIgnoreCase))
                            {
                                retval = i;
                                return retval;
                            }
                            i += 1;
                        }


                    }
                }

            }
            return retval;

        }
        public DataTable ReadDataTable(int sheetno = 0, bool HeaderExist = true, int fromline = 0, int toline = 100)
        {
            if (sheetno > -1)
            {
                FileData = GetDataTableforSheet(sheetno, fromline);
            }
            return FileData;

        }
        public DataTable ReadDataTable(string sheetname, bool HeaderExist = true, int fromline = 0, int toline = 10000)
        {

            int idx = GetEntityIdx(sheetname);
          
            if (idx > -1)
            {
                FileData = GetDataTableforSheet(sheetname, fromline);
            }
            return FileData ;
        }
        private EntityStructure GetEntityDataType(int sheetno)
        {

            return Entities[sheetno];
        }
      
        // Private Excel Data Reader Methods
        //-------------------------------------
        public IExcelDataReader getExcelReader()
        {
            return ExcelReaderFactory.CreateReader(System.IO.File.OpenRead(FilePath), ReaderConfig);
        }
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

                //return null;
                //var workbook = FileData;
                //var sheets = from DataTable sheet in workbook.Tables.Cast<DataTable>() select sheet.TableName;
                //return sheets;

            }
            return entlist;

        }
        public IEnumerable<DataRow> getData(string sheet, bool firstRowIsColumnNames = false)
        {
            //var reader = this.getExcelReader();
            //reader.AsDataSet(ExcelDataSetConfig);
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
                f.fieldname = entspace;
                f.Originalfieldname = field.ColumnName;
                f.fieldtype = field.DataType.ToString();
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
                                    if (f.fieldtype != "System.String")
                                    {
                                        if (decimal.TryParse(valstring, out dval))
                                        {
                                            f.fieldtype = "System.Decimal";
                                            f.Checked = true;
                                        }
                                        else
                                        if (double.TryParse(valstring, out dblval))
                                        {
                                            f.fieldtype = "System.Double";
                                            f.Checked = true;
                                        }
                                        else
                                        if (long.TryParse(valstring, out longval))
                                        {
                                            f.fieldtype = "System.Long";
                                            f.Checked = true;
                                        }
                                        else
                                        if (float.TryParse(valstring, out floatval))
                                        {
                                            f.fieldtype = "System.Float";
                                            f.Checked = true;
                                        }
                                        else
                                        if (int.TryParse(valstring, out intval))
                                        {
                                            f.fieldtype = "System.Int32";
                                            f.Checked = true;
                                        }
                                        else
                                        if (DateTime.TryParse(valstring, out dateval))
                                        {
                                            f.fieldtype = "System.DateTime";
                                            f.Checked = true;
                                        }
                                        else
                                        if (bool.TryParse(valstring, out boolval))
                                        {
                                            f.fieldtype = "System.Bool";
                                            f.Checked = true;
                                        }
                                        else
                                        if (short.TryParse(valstring, out shortval))
                                        {
                                            f.fieldtype = "System.Short";
                                            f.Checked = true;
                                        }
                                        else
                                            f.fieldtype = "System.String";
                                    }
                                    else
                                        f.fieldtype = "System.String";

                                }
                            }
                        }
                        catch (Exception Fieldex)
                        {

                        }
                        try
                        {
                            if (f.fieldtype.Equals("System.String", StringComparison.OrdinalIgnoreCase))
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
                           
                        }
                        try
                        {
                            if (f.fieldtype.Equals("System.Decimal", StringComparison.OrdinalIgnoreCase))
                            {
                                if (r[f.Originalfieldname] != DBNull.Value)
                                {
                                    if (!string.IsNullOrEmpty(r[f.Originalfieldname].ToString()))
                                    {
                                        valstring = r[f.Originalfieldname].ToString();
                                        if (decimal.TryParse(valstring, out dval))
                                        {

                                            f.fieldtype = "System.Decimal";
                                            f.Size1 = GetDecimalPrecision(dval);
                                            f.Size2 = GetDecimalScale(dval);
                                        }
                                    }
                                }

                            }
                        }
                        catch (Exception decimalsizeex)
                        {
                        }
                      
                    }
                }
                catch (Exception rowex)
                {

                }
                
            }
            // Check for string size
            foreach (EntityField fld in flds)
            {
                if (fld.fieldtype.Equals("System.string", StringComparison.OrdinalIgnoreCase))
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
                    if (fld.fieldtype.Equals("System.string", StringComparison.OrdinalIgnoreCase))
                    {
                        if (r[fld.fieldname] != DBNull.Value)
                        {
                            if (!string.IsNullOrEmpty(r[fld.fieldname].ToString()))
                            {
                                if (r[fld.fieldname].ToString().Length > fld.Size1)
                                {
                                    fld.Size1 = r[fld.fieldname].ToString().Length;
                                }
                           
                            }
                        }
                        
                    }
                    decimal dval;
               
                    string valstring;
                   
                    if (fld.fieldtype.Equals("System.Decimal", StringComparison.OrdinalIgnoreCase))
                    {
                        if (r[fld.fieldname] != DBNull.Value)
                        {
                            if (!string.IsNullOrEmpty(r[fld.fieldname].ToString()))
                            {
                                valstring = r[fld.fieldname].ToString();
                                if (decimal.TryParse(valstring, out dval))
                                {
                                    
                                    fld.fieldtype = "System.Decimal";
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
                if (fld.fieldtype.Equals("System.string", StringComparison.OrdinalIgnoreCase))
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
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
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
                DMEEditor?.AddLogMessage("Beep", $"Error in GetEntity with pagination: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return pagedResult;
        }
        #endregion

    }
}
