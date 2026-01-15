using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Logger;

namespace TheTechIdea.Beep.FileManager
{
    /// <summary>
    /// CSV file writer strategy implementation
    /// </summary>
    internal class CsvFileWriter
    {
        private readonly TxtXlsCSVFileSourceHelper _helper;
        private readonly IDMLogger _logger;
        private readonly string _filePath;
        private readonly char _delimiter;

        public CsvFileWriter(TxtXlsCSVFileSourceHelper helper, IDMLogger logger, string filePath, char delimiter)
        {
            _helper = helper;
            _logger = logger;
            _filePath = filePath;
            _delimiter = delimiter;
        }

        public ErrorsInfo WriteRows(EntityStructure entity, IEnumerable<object> rows, bool append)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok };
            try
            {
                string targetPath = _filePath;
                
                // Ensure directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath));

                // If overwrite (append == false) write to temp file then replace
                if (!append)
                {
                    string temp = Path.GetTempFileName();
                    using (var sw = new StreamWriter(temp, false, System.Text.Encoding.UTF8))
                    {
                        // header
                        var header = string.Join(_delimiter.ToString(), entity.Fields.Select(f => 
                            _helper.EscapeCsvValue(f.Originalfieldname ?? f.FieldName, _delimiter)));
                        sw.WriteLine(header);
                        if (rows != null)
                        {
                            foreach (var r in rows)
                            {
                                sw.WriteLine(_helper.SerializeObjectToCsvRow(r, entity, _delimiter));
                            }
                        }
                    }
                    // Replace target (atomic where supported)
                    try
                    {
                        if (File.Exists(targetPath))
                        {
                            File.Replace(temp, targetPath, null);
                        }
                        else
                        {
                            File.Move(temp, targetPath);
                        }
                    }
                    catch
                    {
                        // fallback copy
                        File.Copy(temp, targetPath, true);
                        File.Delete(temp);
                    }
                }
                else
                {
                    // Append
                    using (var sw = new StreamWriter(targetPath, true, System.Text.Encoding.UTF8))
                    {
                        if (rows != null)
                        {
                            foreach (var r in rows)
                            {
                                sw.WriteLine(_helper.SerializeObjectToCsvRow(r, entity, _delimiter));
                            }
                        }
                    }
                }

                retval.Flag = Errors.Ok;
                retval.Message = "CSV write successful.";
            }
            catch (Exception ex)
            {
                retval.Flag = Errors.Failed;
                retval.Message = ex.Message;
                retval.Ex = ex;
                _logger?.WriteLog($"Error writing CSV: {ex.Message}");
            }
            return retval;
        }
    }
}
