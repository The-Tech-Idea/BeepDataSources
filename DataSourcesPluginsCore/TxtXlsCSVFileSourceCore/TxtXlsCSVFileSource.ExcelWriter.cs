using System;
using System.Collections.Generic;
using System.Data;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Logger;

namespace TheTechIdea.Beep.FileManager
{
    /// <summary>
    /// Excel file writer strategy implementation
    /// </summary>
    internal class ExcelFileWriter
    {
        private readonly IDMLogger _logger;
        private readonly string _filePath;
        private readonly string _extension;

        public ExcelFileWriter(IDMLogger logger, string filePath, string extension)
        {
            _logger = logger;
            _filePath = filePath;
            _extension = extension.Replace(".", "").ToLower();
        }

        public ErrorsInfo WriteRows(EntityStructure entity, IEnumerable<object> rows, bool append)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok };
            try
            {
                if (_extension != "xlsx" && _extension != "xls")
                {
                    retval.Flag = Errors.Failed;
                    retval.Message = "Excel write only supported for .xlsx/.xls";
                    return retval;
                }

                string targetPath = _filePath;
                IWorkbook workbook = null;
                ISheet sheet = null;
                bool isXlsx = _extension == "xlsx";

                if (append && System.IO.File.Exists(targetPath))
                {
                    using (var fsr = new System.IO.FileStream(targetPath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                    {
                        workbook = isXlsx ? (IWorkbook)new XSSFWorkbook(fsr) : new HSSFWorkbook(fsr);
                    }
                }
                else
                {
                    workbook = isXlsx ? (IWorkbook)new XSSFWorkbook() : new HSSFWorkbook();
                }

                string sheetName = entity.EntityName ?? entity.OriginalEntityName ?? "Sheet1";
                sheet = workbook.GetSheet(sheetName) ?? workbook.CreateSheet(sheetName);

                int startRow = sheet.LastRowNum;
                if (!append || sheet.PhysicalNumberOfRows == 0)
                {
                    // write header
                    var headerRow = sheet.CreateRow(0);
                    for (int c = 0; c < entity.Fields.Count; c++)
                    {
                        var cell = headerRow.CreateCell(c);
                        cell.SetCellValue(entity.Fields[c].Originalfieldname ?? entity.Fields[c].fieldname);
                    }
                    startRow = 1;
                }
                else
                {
                    startRow = sheet.LastRowNum + 1;
                }

                if (rows != null)
                {
                    int rindex = startRow;
                    foreach (var r in rows)
                    {
                        IRow row = sheet.CreateRow(rindex++);
                        for (int c = 0; c < entity.Fields.Count; c++)
                        {
                            var cell = row.CreateCell(c);
                            string sval = string.Empty;
                            try
                            {
                                var fld = entity.Fields[c];
                                object val = null;
                                if (r is DataRow dr)
                                {
                                    if (dr.Table.Columns.Contains(fld.Originalfieldname)) val = dr[fld.Originalfieldname];
                                    else if (dr.Table.Columns.Contains(fld.fieldname)) val = dr[fld.fieldname];
                                }
                                else if (r is IDictionary<string, object> dict)
                                {
                                    if (dict.ContainsKey(fld.fieldname)) val = dict[fld.fieldname];
                                    else if (dict.ContainsKey(fld.Originalfieldname)) val = dict[fld.Originalfieldname];
                                }
                                else
                                {
                                    var pi = r.GetType().GetProperty(fld.fieldname) ?? r.GetType().GetProperty(fld.Originalfieldname);
                                    if (pi != null) val = pi.GetValue(r);
                                }
                                if (val != null)
                                {
                                    if (val is DateTime dtv)
                                    {
                                        cell.SetCellValue(dtv);
                                    }
                                    else if (double.TryParse(val.ToString(), out double d))
                                    {
                                        cell.SetCellValue(d);
                                    }
                                    else if (bool.TryParse(val.ToString(), out bool bv))
                                    {
                                        cell.SetCellValue(bv);
                                    }
                                    else
                                    {
                                        sval = val.ToString();
                                        cell.SetCellValue(sval);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger?.WriteLog($"Error writing Excel cell value: {ex.Message}");
                            }
                        }
                    }
                }

                // save workbook
                using (var fs = new System.IO.FileStream(targetPath, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                {
                    workbook.Write(fs);
                }

                retval.Flag = Errors.Ok;
                retval.Message = "Excel write successful.";
            }
            catch (Exception ex)
            {
                retval.Flag = Errors.Failed;
                retval.Message = ex.Message;
                retval.Ex = ex;
                _logger?.WriteLog($"Error writing Excel: {ex.Message}");
            }
            return retval;
        }
    }
}
