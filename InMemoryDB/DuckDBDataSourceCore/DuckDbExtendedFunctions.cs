using DuckDB.NET.Data;
using DuckDB.NET.Native;
using System.Data;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using static DuckDB.NET.Native.NativeMethods;

namespace DuckDBDataSourceCore
{
    
    public static class DuckDbExtendedFunctions
    {
        public static void CreateSequence(this DuckDBDataSource DuckDB, string sequenceName, int start = 1, int increment = 1, int minValue = 1, int maxValue = int.MaxValue, bool cycle = false)
        {
            string sql = $"CREATE SEQUENCE {sequenceName} START {start} INCREMENT {increment} MINVALUE {minValue} MAXVALUE {maxValue} {(cycle ? "CYCLE" : "NO CYCLE")};";

         
                using (var command = new DuckDBCommand(sql,DuckDB.DuckConn))
                {
                    command.ExecuteNonQuery();
                }
           
        }
        public static void CreateOrReplaceView(this DuckDBDataSource DuckDB, string viewName, string viewQuerySql)
        {
            // Define the SQL statement to create or replace the view
            string sql = $"CREATE OR REPLACE VIEW {viewName} AS {viewQuerySql};";

           
                using (var command = new DuckDBCommand(sql,DuckDB.DuckConn))
                {
                    // Execute the command to create or replace the view
                    command.ExecuteNonQuery();
                }
           
        }
        //filePattern could be something like /data/myfiles_*.csv, where * is a 
        public static void ImportFromMultipleCsvFiles(this DuckDBDataSource DuckDB, string tableName, string filePattern,bool union_by_name=true)
        {
            DataTable dataTable = new DataTable();
            string query = $"CREATE TABLE {tableName} AS FROM  read_csv_auto('{filePattern}',union_by_name={union_by_name});";

            using (var command = new DuckDBCommand(query, DuckDB.DuckConn))
            {
                //connection.Open();
                command.ExecuteNonQuery();
            }
        }
        //Here, filePattern could be /data/myparquetfiles_*.parquet
        public static void ImportFromMultipleParquetFiles(this DuckDBDataSource DuckDB, string tableName, string filePattern)
        {
            DataTable dataTable = new DataTable();
            string query = $"CREATE TABLE {tableName} AS FROM parquet_scan('{filePattern}');";

            using (var command = new DuckDBCommand(query, DuckDB.DuckConn))
            {
                //connection.Open();
                command.ExecuteNonQuery();
            }
        }
        public static void ImportCSV(this DuckDBDataSource DuckDB, string filePath, string tableName, bool createTable = true)
        {
            string sql;

            if (createTable)
            {
                // Assuming AUTO_DETECT and creating a new table
                sql = $"CREATE TABLE {tableName} AS FROM read_csv('{filePath}');";
            }
            else
            {
                // Importing into an existing table
                sql = $"COPY {tableName} FROM '{filePath}' (FORMAT csv, HEADER true);";
            }

          
                using (var command = new DuckDBCommand(sql,DuckDB.DuckConn))
                {
                    //connection.Open();
                    command.ExecuteNonQuery();
                }
          
        }
        public static void ExportToCSV(this DuckDBDataSource DuckDB, string tableName, string filePath)
        {
            // This SQL command exports the entire table or the result of a query to a Parquet file
            string sql = $"COPY (SELECT * FROM {tableName}) TO '{filePath}' (FORMAT 'csv');";


            //connection.Open();
            using (var command = new DuckDBCommand(sql, DuckDB.DuckConn))
            {
                command.ExecuteNonQuery();
            }

        }
        public static void ImportParquetIntoExistingTable(this DuckDBDataSource DuckDB, string filePath, string tableName)
        {
            // Ensure the table exists and its schema matches the Parquet file schema
            string sql = $"INSERT INTO {tableName} SELECT * FROM read_parquet('{filePath}');";

          
                using (var command = new DuckDBCommand(sql,DuckDB.DuckConn))
                {
                    //connection.Open();
                    command.ExecuteNonQuery();
                }
           
        }
        public static void ImportParquet(this DuckDBDataSource DuckDB, string filePath, string tableName)
        {
            // This SQL command assumes DuckDB will create the table based on the Parquet file schema
            string sql = $"CREATE TABLE {tableName} AS SELECT * FROM read_parquet('{filePath}');";
    using (var command = new DuckDBCommand(sql,DuckDB.DuckConn))
                {
                    //connection.Open();
                    command.ExecuteNonQuery();
                }
           
        }
        public static void ImportJson(this DuckDBDataSource DuckDB, string filePath, string tableName,string parameters="")
        {
            // This SQL command assumes DuckDB will create the table based on the JSON file schema
            string sql = $"CREATE TABLE {tableName} AS SELECT * FROM '{filePath}'  (FORMAT JSON,AUTO_DETECT TRUE);";

           
                using (var command = new DuckDBCommand(sql,DuckDB.DuckConn))
                {
                    //connection.Open();
                    command.ExecuteNonQuery();
                }
          
        }
        public static void ExportJson(this DuckDBDataSource DuckDB, string filePath, string tableName)
        {
            // This SQL command assumes DuckDB will create the table based on the JSON file schema
            string sql = $"CREATE TABLE {tableName} AS SELECT * FROM read_json('{filePath}',auto_detect=true);";

         
                using (var command = new DuckDBCommand(sql,DuckDB.DuckConn))
                {
                    //connection.Open();
                    command.ExecuteNonQuery();
                }
            
        }
        public static void ImportJsonIntoExistingTable(this DuckDBDataSource DuckDB, string filePath, string tableName)
        {
            // Ensure the table exists and its schema matches the JSON structure
            string sql = $"INSERT INTO {tableName} SELECT * FROM read_json('{filePath}');";

           
                using (var command = new DuckDBCommand(sql,DuckDB.DuckConn))
                {
                    //connection.Open();
                    command.ExecuteNonQuery();
                }
           
        }
        public static void ExportToParquet(this DuckDBDataSource DuckDB, string tableName, string filePath)
        {
            // This SQL command exports the entire table or the result of a query to a Parquet file
            string sql = $"COPY (SELECT * FROM {tableName}) TO '{filePath}' (FORMAT 'parquet');";

         
                //connection.Open();
                using (var command = new DuckDBCommand(sql,DuckDB.DuckConn))
                {
                    command.ExecuteNonQuery();
                }
           
        }
        public static void ExportQueryResultToParquet(this DuckDBDataSource DuckDB, string query, string filePath)
        {
            // This SQL command exports the result of a query to a Parquet file
            string sql = $"COPY ({query}) TO '{filePath}' (FORMAT 'parquet');";

           
                //connection.Open();
                using (var command = new DuckDBCommand(sql,DuckDB.DuckConn))
                {
                    command.ExecuteNonQuery();
                }
           
        }
        public static DataTable ExecuteParameterizedQuery(this DuckDBDataSource DuckDB, string sql, Dictionary<string, object> parameters)
        {
            DataTable dataTable = new DataTable();

                using (var command = new DuckDBCommand(sql,DuckDB.DuckConn))
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.Add(new DuckDBParameter() {  ParameterName = param.Key, Value = param.Value });
                    }

                    using (var reader = command.ExecuteReader())
                    {
                        dataTable.Load(reader);
                    }
                }
           

            return dataTable;
        }
        public static void ExportToPartitionedParquet(this DuckDBDataSource DuckDB, string tableName, string directoryPath, string partitionColumn)
        {
            // This SQL command exports the table into partitioned Parquet files in the specified directory
            string sql = $"COPY (SELECT * FROM {tableName}) TO '{directoryPath}' (FORMAT 'parquet', PARTITION_BY '{partitionColumn}');";

         
                //connection.Open();
                using (var command = new DuckDBCommand(sql,DuckDB.DuckConn))
                {
                    command.ExecuteNonQuery();
                }
           
        }
        public static void ExportQueryToPartitionedParquet(this DuckDBDataSource DuckDB, string query, string directoryPath, List<string> partitionColumns)
        {
            string partitionByClause = string.Join(", ", partitionColumns.Select(col => $"'{col}'"));

            // This SQL command exports the query result into partitioned Parquet files in the specified directory
            string sql = $"COPY ({query}) TO '{directoryPath}' (FORMAT 'parquet', PARTITION_BY ({partitionByClause}));";

          
                //connection.Open();
                using (var command = new DuckDBCommand(sql,DuckDB.DuckConn))
                {
                    command.ExecuteNonQuery();
                }
            
        }
        public static int ExecuteNonQuery(this DuckDBDataSource DuckDB, string sql)
        {
          
                using (var command = new DuckDBCommand(sql,DuckDB.DuckConn))
                {
                    //connection.Open();
                    return command.ExecuteNonQuery();
                }
            
        }
        public static object GetScalarValue(this DuckDBDataSource DuckDB, string sql)
        {
           
                using (var command = new DuckDBCommand(sql,DuckDB.DuckConn))
                {
                    //connection.Open();
                    return command.ExecuteScalar();
                }
           
        }
        public static bool TableExists(this DuckDBDataSource DuckDB, string tableName)
        {
            var sql = $"SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = '{tableName}');";
            object result = GetScalarValue(DuckDB, sql);
            return Convert.ToBoolean(result);
        }
        public static void DropTable(this DuckDBDataSource DuckDB, string tableName)
        {
            if (TableExists(DuckDB, tableName))
            {
                ExecuteNonQuery(DuckDB, $"DROP TABLE {tableName};");
            }
        }
        public static void PrintQueryResults(this DuckDBDataSource DuckDB, DuckDBResult queryResult)
        {
            long columnCount = (long)Query.DuckDBColumnCount(ref queryResult);
            for (var index = 0; index < columnCount; index++)
            {
                var columnName = Query.DuckDBColumnName(ref queryResult, index).ToManagedString(false);
                Console.Write($"{columnName} ");
            }

            Console.WriteLine();

            var rowCount = Query.DuckDBRowCount(ref queryResult);
            for (long row = 0; row < rowCount; row++)
            {
                for (long column = 0; column < columnCount; column++)
                {
                    var val = Types.DuckDBValueInt32(ref queryResult, column, row);
                    Console.Write(val);
                    Console.Write(" ");
                }

                Console.WriteLine();
            }
        }
        #region "Data Reading Methods"
        public static DataTable ParquetMetaData(this DuckDBDataSource DuckDB, string filePath)
        {
            // This SQL command assumes DuckDB will create the table based on the Parquet file schema
            string sql = $"SELECT *  FROM parquet_metadata('{filePath}');";
            DataTable dataTable = new DataTable();

            using (var command = new DuckDBCommand(sql, DuckDB.DuckConn))
            {
                //connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    dataTable.Load(reader);
                }
            }

            return dataTable;
        }
        public static DataTable ReadTextFile(this DuckDBDataSource DuckDB, string filePath)
        {
            // This SQL command assumes DuckDB will create the table based on the Parquet file schema
            string sql = $"SELECT size, parse_path(filename), content  FROM read_text('{filePath}');";
            DataTable dataTable = new DataTable();

            using (var command = new DuckDBCommand(sql, DuckDB.DuckConn))
            {
                //connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    dataTable.Load(reader);
                }
            }

            return dataTable;
        }
        public static DataTable ReadParquetFile(this DuckDBDataSource DuckDB, string filepath, bool binaryAsString = false, bool filename = false, bool fileRowNumber = false, bool hivePartitioning = false, bool unionByName = false)
        {

           DuckDBConnection DuckConn = DuckDB.DuckConn;
            using (var cmd = new DuckDBCommand($"SELECT * FROM read_parquet('{filepath}', (binary_as_string={binaryAsString.ToString().ToLower()}, filename={filename.ToString().ToLower()}, file_row_number={fileRowNumber.ToString().ToLower()}, hive_partitioning={hivePartitioning.ToString().ToLower()}, union_by_name={unionByName.ToString().ToLower()}));", DuckConn))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    DataTable dt = new DataTable();
                    dt.Load(reader);
                    return dt;
                }
            }
        }
        public static DataTable ReadMultipleCSVFiles(this DuckDBDataSource DuckDB, List<string> filePaths, bool union_by_name = false, bool filename = false)
        {
            string files = string.Join(", ", filePaths.Select(x => $"'{x}'"));
            string sql = $"SELECT * FROM read_csv_auto([{files}]";
            DuckDBConnection DuckConn = DuckDB.DuckConn;

            if (union_by_name)
            {
                sql += ", union_by_name=True";
            }

            if (filename)
            {
                sql += ", filename=True";
            }

            sql += ");";

            using (var cmd = new DuckDBCommand(sql, DuckConn))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    DataTable dt = new DataTable();
                    dt.Load(reader);
                    return dt;
                }
            }
        }
        public static DataTable JSONLoad(this DuckDBDataSource DuckDB, string filepath, uint maximum_object_size = 16777216, string format = "array", bool ignore_errors = false,
             string compression = "auto", string columns = null, string records = "records", bool auto_detect = false,
             ulong sample_size = 20480, long maximum_depth = -1, string dateformat = "iso", string timestampformat = "iso",
             bool filename = false, bool hive_partitioning = false, bool union_by_name = false)
        {
            DuckDBConnection DuckConn = DuckDB.DuckConn;
            string sql = $"SELECT * FROM json_read('{filepath}', FORMAT='{format}', COMPRESSION='{compression}', RECORDS='{records}'";

            sql += $", MAXIMUM_OBJECT_SIZE={maximum_object_size}, IGNORE_ERRORS={ignore_errors.ToString().ToUpper()}, AUTO_DETECT={auto_detect.ToString().ToUpper()}, SAMPLE_SIZE={sample_size}";
            sql += $", MAXIMUM_DEPTH={maximum_depth}, DATEFORMAT='{dateformat}', TIMESTAMPFORMAT='{timestampformat}', FILENAME={filename.ToString().ToUpper()}";
            sql += $", HIVE_PARTITIONING={hive_partitioning.ToString().ToUpper()}, UNION_BY_NAME={union_by_name.ToString().ToUpper()}";

            if (columns != null)
            {
                sql += $", COLUMNS='{columns}'";
            }

            sql += ");";

            using (var cmd = new DuckDBCommand(sql, DuckConn))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    DataTable dt = new DataTable();
                    dt.Load(reader);
                    return dt;
                }
            }
        }
        public static DataTable CSVLoad(this DuckDBDataSource DuckDB, string filepath, bool all_varchar = false, bool auto_detect = true, string columns = null,
            compressiontype compression = compressiontype.auto, string dateformat = null, char decimal_separator = '.', char delim = ',',
            char escape = '"', bool filename = false, string[] force_not_null = null, bool header = false, bool hive_partitioning = false,
            bool ignore_errors = false, long max_line_size = 2097152, string[] names = null, string new_line = null,
            bool normalize_names = false, string nullstr = null, bool parallel = false, char quote = '"', long sample_size = 20480,
            long skip = 0, string timestampformat = null, string[] types = null, bool union_by_name = false)
        {

            DuckDBConnection DuckConn = DuckDB.DuckConn;
            string sql = $"SELECT * FROM read_csv_auto('{filepath}', HEADER={header.ToString().ToUpper()}, DELIM='{delim}', ESCAPE='{escape}', QUOTE='{quote}'";

            sql += $", ALL_VARCHAR={all_varchar.ToString().ToUpper()}, AUTO_DETECT={auto_detect.ToString().ToUpper()}, COMPRESSION='{compression.ToString().ToUpper()}'";
            sql += $", DATEFORMAT='{dateformat}', DECIMAL='{decimal_separator}', FILENAME={filename.ToString().ToUpper()}, HEADER={header.ToString().ToUpper()}, HIVE_PARTITIONING={hive_partitioning.ToString().ToUpper()}";
            sql += $", IGNORE_ERRORS={ignore_errors.ToString().ToUpper()}, MAX_LINE_SIZE={max_line_size}, NEW_LINE='{new_line}', NORMALIZE_NAMES={normalize_names.ToString().ToUpper()}, NULLSTR='{nullstr}'";
            sql += $", PARALLEL={parallel.ToString().ToUpper()}, SAMPLE_SIZE={sample_size}, SKIP={skip}, TIMESTAMPFORMAT='{timestampformat}', UNION_BY_NAME={union_by_name.ToString().ToUpper()}";

            if (force_not_null != null)
            {
                sql += $", FORCE_NOT_NULL=ARRAY['{string.Join("','", force_not_null)}']";
            }

            if (names != null)
            {
                sql += $", NAMES=ARRAY['{string.Join("','", names)}']";
            }

            if (types != null)
            {
                sql += $", TYPES=ARRAY['{string.Join("','", types)}']";
            }

            sql += ");";

            using (var cmd = new DuckDBCommand(sql, DuckConn))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    DataTable dt = new DataTable();
                    dt.Load(reader);
                    return dt;
                }
            }
        }
        #endregion "Data Import Methods"
    }
}
