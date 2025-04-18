﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;

using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;


using TheTechIdea.Beep.Report;

using TheTechIdea.Beep.Vis;

namespace TheTechIdea.Beep.FileManager
{
    [AddinAttribute(Category = DatasourceCategory.FILE,DatasourceType =  DataSourceType.Xls, FileType = "xls,xlsx")]
    public class NPOIExcelDataSource : IDataSource
    {
        public string GuidID { get; set; }
        IWorkbook book;
        public NPOIExcelDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType pDatasourceType, IErrorsInfo per)
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


        }
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
        public ConnectionState Openconnection()
        {
            throw new NotImplementedException();
        }

        public ConnectionState Closeconnection()
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
        public DataSourceType DatasourceType { get; set; } = DataSourceType.Xls;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.FILE;
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
        public virtual string ColumnDelimiter { get; set; } = "''";
        public virtual string ParameterDelimiter { get; set; } = ":";
        IDMLogger IDataSource.Logger { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
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
        public bool CheckEntityExist(string EntityName)
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

        public Task<object> GetEntityDataAsync(string EntityName, string filterstr)
        {
            throw new NotImplementedException();
        }

        public List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
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

         public  object RunQuery( string qrystr)
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
        public Task<object> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            throw new NotImplementedException();
        }
        #region "Reader NPOI"
        public IErrorsInfo ReadWorkbook(string path)
        {
           

            try
            {
                FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                // Try to read workbook as XLSX:
                try
                {
                    book = new XSSFWorkbook(fs);
                }
                catch
                {
                    book = null;
                }

                // If reading fails, try to read workbook as XLS:
                if (book == null)
                {
                    book = new HSSFWorkbook(fs);
                }
                DMEEditor.AddLogMessage("Success", "Opened Workbook", DateTime.Now, 0, path, Errors.Ok);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", "Could not Open Workbook", DateTime.Now, 0, path, Errors.Failed);
               
            }
            return DMEEditor.ErrorObject;
        }
        public ISheet CreateWorkSheet(string sheetname)
        {
            return book.CreateSheet(sheetname);
        }
        public IErrorsInfo SaveWorkBook(string path)
        {
            try
            {
                using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                //    book.Write(stream);
                }
                DMEEditor.AddLogMessage("Success", "Saved Workbook", DateTime.Now, 0, path, Errors.Ok);
            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", "Could not save Workbook", DateTime.Now, 0, path, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            throw new NotImplementedException();
        }
        //------------【Function: read data from Excel file to table control】------------    
        //filePath is the path name of the Excel file
        //datagGridView to display data table control
        //------------------------------------------------
        //public  DataTable ReadFromExcel(string filePath)
        //{
        //    bool result = true;
        //    DataTable tb = null;
        //    FileStream fs = null;//Create a new file stream
        //    HSSFWorkbook workbook = null;//Create a new Excel file
        //    ISheet sheet = null;//Create a worksheet for Excel

        //    //Define the number of rows and columns
        //    int rowCount = 0;//Record the number of rows in Excel
        //    int colCount = 0;//Record the number of columns in Excel

        //    //Determine whether the file exists
        //    if (!File.Exists(filePath))
        //    {

        //        return null;
        //    }
        //    //Create a worksheet pointing to the file
        //    try
        //    {
        //        fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        //        workbook = new HSSFWorkbook(fs);//.xls
        //        if (fs != null)
        //        {
        //            fs.Close();
        //            fs.Dispose();
        //            fs = null;
        //        }
        //        sheet = workbook.GetSheetAt(0);
        //        if (sheet == null)
        //        {
        //            result = false;
        //            return null;
        //        }
        //        rowCount = sheet.LastRowNum;
        //        colCount = sheet.GetRow(0).LastCellNum;
        //        tb.Rows.Clear();
        //        tb.Columns.Clear();
        //        for (int j = 0; j < colCount; j++) //Column loop
        //        {
        //            ICell cell = sheet.GetRow(0).GetCell(j);//Get column
        //          //  tb.Columns.Add(j.ToString() + cell.ToString(), );
        //        }
        //        for (int i = 1; i < rowCount; i++) //row loop
        //        {
        //            IRow row = sheet.GetRow(i); //Get i row
        //            int index = i;// dataGridView.Rows.Add();
        //            colCount = row.LastCellNum;
        //            for (int j = 0; j < colCount; j++) //Column loop
        //            {
        //                ICell cell = row.GetCell(j);//Get j column
        //            //    tb.Rows[index].Field[j].Value = cell.ToString();
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {

        //        result = false;
        //        return null;
        //    }
        //    return tb;
        //}
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

        public object GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
