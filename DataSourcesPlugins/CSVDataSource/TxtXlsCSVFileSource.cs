
using TheTechIdea.Logger;
using System.Data;
using TheTechIdea.Util;

using System.IO;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Vis;
using ExcelDataReader;
using System.Text;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.ConfigUtil;
using System.Xml;

namespace TheTechIdea.Beep.FileManager
{
    [AddinAttribute(Category = DatasourceCategory.FILE, DatasourceType = DataSourceType.CSV|DataSourceType.Xls,FileType = "xls,xlsx") ]
    public class TxtXlsCSVFileSource : IDataSource
    {
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
            FileName = Dataconnection.ConnectionProp.FileName;
            FilePath = Dataconnection.ConnectionProp.FilePath;
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
                } else
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
        public ConnectionState Closeconnection()
        {
           return ConnectionStatus = ConnectionState.Closed;
        }
        public List<string> GetEntitesList()
        {
            ErrorObject.Flag = Errors.Ok;

            try
            {
              
               
                if (GetFileState() == ConnectionState.Open)
                {
                    GetSheets();
                    //if (Entities.Count > 0)
                    //{
                    //    List<string> ename = Entities.Select(p => p.EntityName.ToUpper()).ToList();
                    //    List<string> diffnames = ename.Except(EntitiesNames.Select(o=>o.ToUpper())).ToList();
                    //    if (diffnames.Count > 0)
                    //    {
                    //        foreach (string item in diffnames)
                    //        {
                    //            Entities.Add(GetEntityStructure(item, true));
                    //            //int idx = Entities.FindIndex(p => p.EntityName.Equals(item, StringComparison.OrdinalIgnoreCase) || p.DatasourceEntityName.Equals(item, StringComparison.OrdinalIgnoreCase));
                    //            //Entities[idx].Created = false;
                                
                    //        }
                    //    }
                    //}
                }
               
               
                

                //  DMEEditor.ConfigEditor.DataConnections.Where(c => c.FileName == DatasourceName).FirstOrDefault().Entities =entlist ;
            //    Logger.WriteLog("Successfully Retrieve Entites list ");

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

                    ent = Entities[Entities.FindIndex(x => x.EntityName == EntityName)];
                    string filenamenoext = EntityName;
                    DMTypeBuilder.CreateNewObject(DMEEditor, EntityName, EntityName, Entities.Where(x => x.EntityName == EntityName).FirstOrDefault().Fields);
                }
             
                return DMTypeBuilder.myType;
            }
            return null;
        }
        public  object GetEntity(string EntityName, List<AppFilter> filter)
        {
            ErrorObject.Flag = Errors.Ok;
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
                    int idx = -1;
                    if(Entities.Count > 0)
                    {
                        idx = Entities.FindIndex(p => p.EntityName.Equals(EntityName, StringComparison.InvariantCultureIgnoreCase));
                     
                    }
                   
                    if (idx > -1)
                    {
                        entity = Entities[idx];
                        dt = ReadDataTable(EntityName, HeaderExist, fromline, toline);
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
                return dt;
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
                foreach (DataRow dr in dt.Rows)
                {
                    try
                    {
                        DataRow r = newdt.NewRow();
                        foreach (var item in ent.Fields)
                        {
                            if (dr[item.fieldname] != DBNull.Value )
                            {
                                string st = dr[item.fieldname].ToString().Trim();
                                if(!string.IsNullOrEmpty(st) && !string.IsNullOrWhiteSpace(st))
                                {
                                    r[item.fieldname] = Convert.ChangeType(dr[item.fieldname], ToConvert(Type.GetType(item.fieldtype)));
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
        public List<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            return null;
        }
        public DataSet GetChildTablesListFromCustomQuery(string tablename, string customquery)
        {
            throw new NotImplementedException();
        }
        public List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            throw new NotImplementedException();
        }
        public IErrorsInfo ExecuteSql(string sql)
        {
            throw new NotImplementedException();
        }
        public bool CreateEntityAs(EntityStructure entity)
        {
            throw new NotImplementedException();
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
                        GetSheets();
                      
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
        public  object RunQuery( string qrystr)
        {
            throw new NotImplementedException();
        }
        public virtual IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {


            throw new NotImplementedException();
        }
        public IErrorsInfo DeleteEntity(string EntityName, object DeletedDataRow)
        {
            throw new NotImplementedException();
        }  
        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            throw new NotImplementedException();
        }
        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            throw new NotImplementedException();
        }
        public List<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            throw new NotImplementedException();
        }
        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            throw new NotImplementedException();
        }
        public Task<object> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            return (Task<object>)GetEntity(EntityName, Filter);
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
            DataSet ds = new DataSet();
            using (var stream = File.Open(Path.Combine(FilePath, FileName), FileMode.Open, FileAccess.Read))
            {
                switch (Dataconnection.ConnectionProp.Ext.Replace(".","").ToLower())
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


                // 2. Use the AsDataSet extension method
                ds = reader.AsDataSet(ExcelDataSetConfig);
                // The result of each spreadsheet is in result.Tables

                stream.Close();
            }
            return ds;
        }
        private void GetSheets()
        {
            DataSet ds;
           
            if (GetFileState() == ConnectionState.Open)
            {
                try
                {
                    string sheetname;
                    ds = GetExcelDataSet();
                    EntitiesNames = new List<string>();
                   
                    for (int i = 0; i < ds.Tables.Count; i++)
                    {
                        DataTable tb=ds.Tables[i];
                        EntitiesNames.Add(tb.TableName);
                        EntityStructure ent = null;

                        int idx = Entities.FindIndex(p => p.EntityName.Equals(tb.TableName, StringComparison.InvariantCultureIgnoreCase));
                        if (idx > -1)
                        {
                            ent = Entities[idx];
                        }
                        if (ent == null)
                        {
                            EntityStructure entityData = new EntityStructure();
                            entityData = new EntityStructure();
                            sheetname = tb.TableName;
                            entityData.Viewtype = ViewType.File;
                            entityData.DatabaseType = DataSourceType.Text;
                            entityData.DataSourceID = FileName;
                            entityData.DatasourceEntityName = tb.TableName;
                            entityData.Caption = tb.TableName;
                            entityData.EntityName = sheetname;
                            entityData.Id = i;
                            i++;
                            entityData.OriginalEntityName = sheetname;
                            Entities.Add(entityData);
                            entityData.Drawn = true;
                        }
                        else
                        {
                            ent.Fields = new List<EntityField>();
                            DataTable tbdata = GetDataTableforSheet(tb.TableName, ent.StartRow);
                            ent.Fields.AddRange(GetFieldsbyTableScan( tbdata,tbdata.TableName, tbdata.Columns));
                            ent.Drawn = true;
                        }

                    }
                  
                    for (int y = 0; y < Entities.Count(); y++)
                    {
                        
                    }
                }
                catch (Exception ex)
                {
                    DMEEditor.AddLogMessage("Fail", $"Error in getting File format {ex.Message}", DateTime.Now, 0, FileName, Errors.Failed);

                }
            }
        }
        private void Getfields()
        {
            DataSet ds;
            if (Entities == null)
            {
                Entities = new List<EntityStructure>();
            }
            if (GetFileState() == ConnectionState.Open)
            {
                try
                {
                    ds = GetExcelDataSet();

                    int i = 0;
                    foreach (DataTable tb in ds.Tables)
                    {
                        EntityStructure ent = null;
                        if (!Entities.Where(p => p.OriginalEntityName.Equals(tb.TableName, StringComparison.OrdinalIgnoreCase)).Any())
                        {
                            string sheetname;
                            int idx = Entities.FindIndex(p => p.EntityName.Equals(tb.TableName, StringComparison.InvariantCultureIgnoreCase));
                            if (idx > -1)
                            {
                               ent = Entities[idx];
                            }
                           if(ent == null)
                           {
                                EntityStructure entityData = new EntityStructure();
                                entityData = new EntityStructure();
                                sheetname = tb.TableName;
                                entityData.Viewtype = ViewType.File;
                                entityData.DatabaseType = DataSourceType.Text;
                                entityData.DataSourceID = FileName;
                                entityData.DatasourceEntityName = tb.TableName;
                                entityData.Caption = tb.TableName;
                                entityData.EntityName = sheetname;
                                entityData.Id = i;
                             
                                i++;
                                entityData.OriginalEntityName = sheetname;
                                DataTable tbdata = GetDataTableforSheet(tb.TableName, ent.StartRow);
                                entityData.Fields = new List<EntityField>();
                                entityData.Fields.AddRange(GetFieldsbyTableScan(tbdata,tb.TableName, tb.Columns));
                                Entities.Add(entityData);
                            }
                            else
                            {

                            }
                       

                        }
                          
                    }

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
                        int idx = Entities.FindIndex(p => p.EntityName.Equals(EntityName, StringComparison.InvariantCultureIgnoreCase));
                        if (idx > -1)
                            entityData = Entities[idx];
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
        public DataTable ReadDataTable(int sheetno = 0, bool HeaderExist = true, int fromline = 0, int toline = 100)
        {
            if (GetFileState() == ConnectionState.Open)
            {
                DataTable dataRows = new DataTable();
                FileData = GetDataTableforSheet(sheetno,fromline);
                dataRows = FileData;
                toline = dataRows.Rows.Count;
                List<EntityField> flds = GetSheetColumns(FileData.TableName);
                string classpath = DMEEditor.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.ProjectClass).Select(x => x.FolderPath).FirstOrDefault();
                // DMEEditor.classCreator.CreateClass(FileData.Tables[sheetno].TableName, flds, classpath);
                //GetTypeForSheetsFile(dataRows.TableName);
                return dataRows;
            }
            else
            {
                return null;
            }

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
        //        string classpath = DMEEditor.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.ProjectClass).Select(x => x.FolderPath).FirstOrDefault();
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

                            if (ls.Tables[i].TableName == sheetname)
                            {
                                retval = i;

                                found = "Found";
                            }
                            else
                            {
                                if (i == ls.Tables.Count - 1)
                                {
                                    found = "ExitandNotFound";
                                }
                                else
                                {
                                    i += 1;
                                }
                            }
                        }


                    }
                }

            }
            return retval;

        }
        public DataTable ReadDataTable(string sheetname, bool HeaderExist = true, int fromline = 0, int toline = 10000)
        {

            int idx = -1;
            if (Entities.Count > 0)
            {
                idx = Entities.FindIndex(p => p.EntityName.Equals(sheetname, StringComparison.InvariantCultureIgnoreCase));


            }

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
                string entspace = Regex.Replace(field.ColumnName, @"\s+", "_");
                f.fieldname = field.ColumnName;
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
                            if (r[f.fieldname] != DBNull.Value)
                            {
                                valstring = r[f.fieldname].ToString();
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
                                if (r[f.fieldname] != DBNull.Value)
                                {
                                    if (!string.IsNullOrEmpty(r[f.fieldname].ToString()))
                                    {
                                        if (r[f.fieldname].ToString().Length > f.Size1)
                                        {
                                            f.Size1 = r[f.fieldname].ToString().Length;
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
                                if (r[f.fieldname] != DBNull.Value)
                                {
                                    if (!string.IsNullOrEmpty(r[f.fieldname].ToString()))
                                    {
                                        valstring = r[f.fieldname].ToString();
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
        #endregion

    }
}
