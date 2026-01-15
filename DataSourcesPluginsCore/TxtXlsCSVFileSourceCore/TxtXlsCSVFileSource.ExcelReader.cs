using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Logger;

namespace TheTechIdea.Beep.FileManager
{
    /// <summary>
    /// Partial class for Excel read operations using NPOI
    /// </summary>
    public partial class TxtXlsCSVFileSource : IDataSource
    {
        /// <summary>
        /// Opens an Excel workbook (.xlsx or .xls) from the specified file path
        /// </summary>
        private IWorkbook OpenWorkbook(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"File not found: {filePath}");
                }

                string ext = Path.GetExtension(filePath).ToLower();
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    if (ext == ".xlsx")
                    {
                        return new XSSFWorkbook(fs);
                    }
                    else if (ext == ".xls")
                    {
                        return new HSSFWorkbook(fs);
                    }
                    else
                    {
                        throw new NotSupportedException($"File extension '{ext}' is not supported. Use .xlsx or .xls");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error opening workbook from {filePath}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Converts an Excel worksheet to a DataTable with automatic type inference
        /// </summary>
        private DataTable WorkbookToDataTable(IWorkbook workbook, string sheetName, int startRow, int endRow)
        {
            try
            {
                ISheet sheet = workbook.GetSheet(sheetName);
                if (sheet == null)
                {
                    throw new ArgumentException($"Sheet '{sheetName}' not found in workbook");
                }

                DataTable dt = new DataTable(sheetName);

                // Determine the actual range
                int firstRow = startRow > 0 ? startRow : sheet.FirstRowNum;
                int lastRow = endRow > 0 ? Math.Min(endRow, sheet.LastRowNum) : sheet.LastRowNum;

                if (firstRow > lastRow)
                {
                    return dt; // Empty sheet
                }

                // Get header row (first row)
                IRow headerRow = sheet.GetRow(firstRow);
                if (headerRow == null)
                {
                    return dt; // No header row
                }

                // Create columns from header
                for (int col = headerRow.FirstCellNum; col < headerRow.LastCellNum; col++)
                {
                    ICell cell = headerRow.GetCell(col);
                    string colName = cell?.StringCellValue ?? $"Column{col}";
                    dt.Columns.Add(colName, typeof(object)); // Initially all object type
                }

                // Add data rows
                for (int rowIdx = firstRow + 1; rowIdx <= lastRow; rowIdx++)
                {
                    IRow row = sheet.GetRow(rowIdx);
                    if (row == null) continue;

                    DataRow drNew = dt.NewRow();
                    for (int col = 0; col < dt.Columns.Count; col++)
                    {
                        ICell cell = row.GetCell(col);
                        drNew[col] = GetCellValue(cell);
                    }
                    dt.Rows.Add(drNew);
                }

                return dt;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error converting worksheet '{sheetName}' to DataTable: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets the value from an Excel cell with proper type handling
        /// </summary>
        private object GetCellValue(ICell cell)
        {
            if (cell == null)
            {
                return null;
            }

            try
            {
                switch (cell.CellType)
                {
                    case CellType.String:
                        return cell.StringCellValue;
                    case CellType.Numeric:
                        if (DateUtil.IsCellDateFormatted(cell))
                        {
                            return cell.DateCellValue;
                        }
                        return cell.NumericCellValue;
                    case CellType.Boolean:
                        return cell.BooleanCellValue;
                    case CellType.Formula:
                        // Try to evaluate formula
                        try
                        {
                            return cell.NumericCellValue;
                        }
                        catch
                        {
                            return cell.StringCellValue;
                        }
                    case CellType.Blank:
                        return null;
                    default:
                        return cell.ToString();
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error reading cell value: {ex.Message}");
                return cell.ToString();
            }
        }

        /// <summary>
        /// Gets all sheet names from the Excel file using NPOI
        /// </summary>
        private void GetSheetsNPOI()
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (string.IsNullOrEmpty(CombineFilePath))
                {
                    CombineFilePath = Path.Combine(FilePath, FileName);
                }

                if (!File.Exists(CombineFilePath))
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = $"File not found: {CombineFilePath}";
                    return;
                }

                Entities.Clear();
                EntitiesNames.Clear();

                using (var fs = new FileStream(CombineFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    IWorkbook workbook = null;
                    string ext = Path.GetExtension(CombineFilePath).ToLower();

                    try
                    {
                        if (ext == ".xlsx")
                        {
                            workbook = new XSSFWorkbook(fs);
                        }
                        else if (ext == ".xls")
                        {
                            workbook = new HSSFWorkbook(fs);
                        }
                        else
                        {
                            ErrorObject.Flag = Errors.Failed;
                            ErrorObject.Message = $"Unsupported file format: {ext}";
                            return;
                        }

                        // Iterate through sheets
                        for (int i = 0; i < workbook.NumberOfSheets; i++)
                        {
                            ISheet sheet = workbook.GetSheetAt(i);
                            if (sheet != null)
                            {
                                string sheetName = sheet.SheetName;
                                EntitiesNames.Add(sheetName);

                                // Read first row to get field structure
                                IRow headerRow = sheet.GetRow(sheet.FirstRowNum);
                                EntityStructure entity = new EntityStructure
                                {
                                    EntityName = sheetName,
                                    DatasourceEntityName = sheetName,
                                    OriginalEntityName = sheetName,
                                    StartRow = sheet.FirstRowNum,
                                    EndRow = sheet.LastRowNum,
                                    Fields = new List<EntityField>()
                                };

                                if (headerRow != null)
                                {
                                    for (int col = headerRow.FirstCellNum; col < headerRow.LastCellNum; col++)
                                    {
                                        ICell cell = headerRow.GetCell(col);
                                        string fieldName = cell?.StringCellValue ?? $"Column{col}";

                                        var field = new EntityField
                                        {
                                            fieldname = fieldName,
                                            Originalfieldname = fieldName,
                                            fieldtype = "System.String", // Default type; will be inferred during read
                                            IsKey = col == 0,
                                            AllowDBNull = true
                                        };
                                        entity.Fields.Add(field);
                                    }
                                }

                                Entities.Add(entity);
                            }
                        }

                        IsFileRead = true;
                    }
                    finally
                    {
                        workbook?.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error reading Excel sheets: {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
            }
        }

        /// <summary>
        /// Reads a specific range from an Excel sheet into a DataTable using NPOI
        /// </summary>
        private DataTable ReadDataTableNPOI(string sheetName, bool hasHeader, int startRow, int endRow)
        {
            DataTable dt = new DataTable(sheetName);

            try
            {
                if (string.IsNullOrEmpty(CombineFilePath))
                {
                    CombineFilePath = Path.Combine(FilePath, FileName);
                }

                if (!File.Exists(CombineFilePath))
                {
                    throw new FileNotFoundException($"File not found: {CombineFilePath}");
                }

                using (var fs = new FileStream(CombineFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    IWorkbook workbook = null;
                    string ext = Path.GetExtension(CombineFilePath).ToLower();

                    try
                    {
                        if (ext == ".xlsx")
                        {
                            workbook = new XSSFWorkbook(fs);
                        }
                        else if (ext == ".xls")
                        {
                            workbook = new HSSFWorkbook(fs);
                        }
                        else
                        {
                            throw new NotSupportedException($"File format not supported: {ext}");
                        }

                        ISheet sheet = workbook.GetSheet(sheetName);
                        if (sheet == null)
                        {
                            throw new ArgumentException($"Sheet '{sheetName}' not found");
                        }

                        // Determine row range
                        int firstDataRow = startRow >= 0 ? startRow : sheet.FirstRowNum;
                        int lastDataRow = endRow > 0 ? Math.Min(endRow, sheet.LastRowNum) : sheet.LastRowNum;

                        // Get header row
                        IRow headerRow = sheet.GetRow(firstDataRow);
                        if (headerRow == null)
                        {
                            return dt;
                        }

                        // Create columns from header
                        for (int col = headerRow.FirstCellNum; col < headerRow.LastCellNum; col++)
                        {
                            ICell cell = headerRow.GetCell(col);
                            string colName = cell?.StringCellValue ?? $"Column{col}";
                            dt.Columns.Add(colName, typeof(object));
                        }

                        // Add data rows
                        for (int rowIdx = firstDataRow + (hasHeader ? 1 : 0); rowIdx <= lastDataRow; rowIdx++)
                        {
                            IRow row = sheet.GetRow(rowIdx);
                            if (row == null) continue;

                            DataRow drNew = dt.NewRow();
                            for (int col = 0; col < dt.Columns.Count; col++)
                            {
                                ICell cell = row.GetCell(col);
                                drNew[col] = GetCellValue(cell);
                            }
                            dt.Rows.Add(drNew);
                        }
                    }
                    finally
                    {
                        workbook?.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error reading Excel data table: {ex.Message}");
                throw;
            }

            return dt;
        }

        #region "Async Excel Operations"

        /// <summary>
        /// Asynchronously opens an Excel workbook from the specified file path
        /// </summary>
        private async Task<IWorkbook> OpenWorkbookAsync(string filePath)
        {
            return await Task.Run(() => OpenWorkbook(filePath));
        }

        /// <summary>
        /// Asynchronously converts an Excel worksheet to a DataTable with automatic type inference
        /// </summary>
        private async Task<DataTable> WorkbookToDataTableAsync(IWorkbook workbook, string sheetName, int startRow, int endRow)
        {
            return await Task.Run(() => WorkbookToDataTable(workbook, sheetName, startRow, endRow));
        }

        /// <summary>
        /// Asynchronously gets all sheet names from the Excel file using NPOI
        /// </summary>
        private async Task GetSheetsNPOIAsync()
        {
            await Task.Run(() => GetSheetsNPOI());
        }

        /// <summary>
        /// Asynchronously reads a specific range from an Excel sheet into a DataTable using NPOI
        /// </summary>
        private async Task<DataTable> ReadDataTableNPOIAsync(string sheetName, bool hasHeader, int startRow, int endRow)
        {
            return await Task.Run(() => ReadDataTableNPOI(sheetName, hasHeader, startRow, endRow));
        }

        #endregion
    }
}
