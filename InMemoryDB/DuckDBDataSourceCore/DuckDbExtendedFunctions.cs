using DuckDB.NET.Data;
using DuckDB.NET.Native;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Data;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Helpers.FileandFolderHelpers;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using static DuckDB.NET.Native.NativeMethods;

namespace DuckDBDataSourceCore
{
    
    public partial class DuckDBDataSource
    {
        [CommandAttribute(
            Name = "CreateSequence",
            Caption = "Create Sequence",
            Category = DatasourceCategory.INMEMORY,
            DatasourceType = DataSourceType.DuckDB,
            PointType = EnumPointType.Function,
            ObjectType = "Sequence",
            ClassType = "DuckDbExtendedFunctions",
            Showin = ShowinType.Both,
            Order = 1,
            iconimage = "sequence.png",
            misc = "ReturnType: void"
        )]
        public void CreateSequence(string sequenceName, int start = 1, int increment = 1, int minValue = 1, int maxValue = int.MaxValue, bool cycle = false)
        {
            string sql = $"CREATE SEQUENCE {sequenceName} START {start} INCREMENT {increment} MINVALUE {minValue} MAXVALUE {maxValue} {(cycle ? "CYCLE" : "NO CYCLE")};";

         
                using (var command = new DuckDBCommand(sql,DuckConn))
                {
                    command.ExecuteNonQuery();
                }
           
        }
        [CommandAttribute(
            Name = "CreateOrReplaceView",
            Caption = "Create Or Replace View",
            Category = DatasourceCategory.INMEMORY,
            DatasourceType = DataSourceType.DuckDB,
            PointType = EnumPointType.Function,
            ObjectType = "View",
            ClassType = "DuckDbExtendedFunctions",
            Showin = ShowinType.Both,
            Order = 2,
            iconimage = "view.png",
            misc = "ReturnType: void"
        )]
        public void CreateOrReplaceView(string viewName, string viewQuerySql)
        {
            // Define the SQL statement to create or replace the view
            string sql = $"CREATE OR REPLACE VIEW {viewName} AS {viewQuerySql};";

           
                using (var command = new DuckDBCommand(sql,DuckConn))
                {
                    // Execute the command to create or replace the view
                    command.ExecuteNonQuery();
                }
           
        }
        //filePattern could be something like /data/myfiles_*.csv, where * is a 
        [CommandAttribute(
            Name = "ImportFromMultipleCsvFiles",
            Caption = "Import Multiple CSVs",
            Category = DatasourceCategory.INMEMORY,
            DatasourceType = DataSourceType.DuckDB,
            PointType = EnumPointType.Function,
            ObjectType = "Table",
            ClassType = "DuckDbExtendedFunctions",
            Showin = ShowinType.Both,
            Order = 3,
            iconimage = "import.png",
            misc = "ReturnType: void"
        )]
        public void ImportFromMultipleCsvFiles(string tableName, string filePattern,bool union_by_name=true)
        {
            DataTable dataTable = new DataTable();
            string query = $"CREATE TABLE {tableName} AS FROM  read_csv_auto('{filePattern}',union_by_name={union_by_name});";

            using (var command = new DuckDBCommand(query, DuckConn))
            {
                //connection.Open();
                command.ExecuteNonQuery();
            }
        }
        //Here, filePattern could be /data/myparquetfiles_*.parquet
        [CommandAttribute(
            Name = "ImportFromMultipleParquetFiles",
            Caption = "Import Multiple Parquets",
            Category = DatasourceCategory.INMEMORY,
            DatasourceType = DataSourceType.DuckDB,
            PointType = EnumPointType.Function,
            ObjectType = "Table",
            ClassType = "DuckDbExtendedFunctions",
            Showin = ShowinType.Both,
            Order = 4,
            iconimage = "import.png",
            misc = "ReturnType: void"
        )]
        public void ImportFromMultipleParquetFiles(string tableName, string filePattern)
        {
            DataTable dataTable = new DataTable();
            string query = $"CREATE TABLE {tableName} AS FROM parquet_scan('{filePattern}');";

            using (var command = new DuckDBCommand(query, DuckConn))
            {
                //connection.Open();
                command.ExecuteNonQuery();
            }
        }
        [CommandAttribute(
            Name = "ImportCSV",
            Caption = "Import CSV",
            Category = DatasourceCategory.INMEMORY,
            DatasourceType = DataSourceType.DuckDB,
            PointType = EnumPointType.Function,
            ObjectType = "Table",
            ClassType = "DuckDbExtendedFunctions",
            Showin = ShowinType.Both,
            Order = 5,
            iconimage = "import_csv.png",
            misc = "ReturnType: void"
        )]
        public void ImportCSV(string filePath, string tableName, bool createTable = true)
        {
            string sql;
            IDMEEditor editor = DMEEditor;
            try
            {
                // Check if the table already exists
                if (TableExists(tableName) && !createTable)
                {
                    // If the table exists and we are not creating a new one, we can just import data
                    sql = $"COPY {tableName} FROM '{filePath}' (FORMAT csv, HEADER true);";
                }
                else
                {
                    // If the table does not exist or we want to create a new one
                    sql = $"CREATE TABLE {tableName} AS SELECT * FROM read_csv_auto('{filePath}');";
                }
                using (var command = new DuckDBCommand(sql, DuckConn))
                {
                    //connection.Open();
                    command.ExecuteNonQuery();
                }
                FileHelper.CreateFileDataConnection( filePath);
                // Create EntityStructure object 
                // and set its properties

                EntityStructure entityStructure=GetEntityStructure(tableName);
               
                SaveStructure();
             //   FileConnectionHelper.LoadFile(filePath);
            }
            catch (Exception ex)
            {

            }
           

          
        }
        [CommandAttribute(
            Name = "ExportToCSV",
            Caption = "Export To CSV",
            Category = DatasourceCategory.INMEMORY,
            DatasourceType = DataSourceType.DuckDB,
            PointType = EnumPointType.Function,
            ObjectType = "File",
            ClassType = "DuckDbExtendedFunctions",
            Showin = ShowinType.Both,
            Order = 6,
            iconimage = "export_csv.png",
            misc = "ReturnType: void"
        )]
        public void ExportToCSV(string tableName, string filePath)
        {
            // This SQL command exports the entire table or the result of a query to a Parquet file
            string sql = $"COPY (SELECT * FROM {tableName}) TO '{filePath}' (FORMAT 'csv');";


            //connection.Open();
            using (var command = new DuckDBCommand(sql, DuckConn))
            {
                command.ExecuteNonQuery();
            }

        }
        [CommandAttribute(
            Name = "ImportParquetIntoExistingTable",
            Caption = "Import Parquet To Table",
            Category = DatasourceCategory.INMEMORY,
            DatasourceType = DataSourceType.DuckDB,
            PointType = EnumPointType.Function,
            ObjectType = "Table",
            ClassType = "DuckDbExtendedFunctions",
            Showin = ShowinType.Both,
            Order = 7,
            iconimage = "import_parquet.png",
            misc = "ReturnType: void"
        )]
        public void ImportParquetIntoExistingTable(string filePath, string tableName)
        {
            // Ensure the table exists and its schema matches the Parquet file schema
            string sql = $"INSERT INTO {tableName} SELECT * FROM read_parquet('{filePath}');";

          
                using (var command = new DuckDBCommand(sql,DuckConn))
                {
                    //connection.Open();
                    command.ExecuteNonQuery();
                }
           
        }
        [CommandAttribute(
            Name = "ImportParquet",
            Caption = "Import Parquet",
            Category = DatasourceCategory.INMEMORY,
            DatasourceType = DataSourceType.DuckDB,
            PointType = EnumPointType.Function,
            ObjectType = "Table",
            ClassType = "DuckDbExtendedFunctions",
            Showin = ShowinType.Both,
            Order = 8,
            iconimage = "import_parquet.png",
            misc = "ReturnType: void"
        )]
        public void ImportParquet(string filePath, string tableName)
        {
            // This SQL command assumes DuckDB will create the table based on the Parquet file schema
            string sql = $"CREATE TABLE {tableName} AS SELECT * FROM read_parquet('{filePath}');";
    using (var command = new DuckDBCommand(sql,DuckConn))
                {
                    //connection.Open();
                    command.ExecuteNonQuery();
                }
           
        }
        [CommandAttribute(
            Name = "ImportJson",
            Caption = "Import JSON",
            Category = DatasourceCategory.INMEMORY,
            DatasourceType = DataSourceType.DuckDB,
            PointType = EnumPointType.Function,
            ObjectType = "Table",
            ClassType = "DuckDbExtendedFunctions",
            Showin = ShowinType.Both,
            Order = 9,
            iconimage = "import_json.png",
            misc = "ReturnType: void"
        )]
        public void ImportJson(string filePath, string tableName,string parameters="")
        {
            // This SQL command assumes DuckDB will create the table based on the JSON file schema
            string sql = $"CREATE TABLE {tableName} AS SELECT * FROM '{filePath}'  (FORMAT JSON,AUTO_DETECT TRUE);";

           
                using (var command = new DuckDBCommand(sql,DuckConn))
                {
                    //connection.Open();
                    command.ExecuteNonQuery();
                }
          
        }
        [CommandAttribute(
            Name = "ExportJson",
            Caption = "Export JSON",
            Category = DatasourceCategory.INMEMORY,
            DatasourceType = DataSourceType.DuckDB,
            PointType = EnumPointType.Function,
            ObjectType = "Table",
            ClassType = "DuckDbExtendedFunctions",
            Showin = ShowinType.Both,
            Order = 10,
            iconimage = "export_json.png",
            misc = "ReturnType: void"
        )]
        public void ExportJson(string filePath, string tableName)
        {
            // This SQL command assumes DuckDB will create the table based on the JSON file schema
            string sql = $"CREATE TABLE {tableName} AS SELECT * FROM read_json('{filePath}',auto_detect=true);";

         
                using (var command = new DuckDBCommand(sql,DuckConn))
                {
                    //connection.Open();
                    command.ExecuteNonQuery();
                }
            
        }
        [CommandAttribute(
            Name = "ImportJsonIntoExistingTable",
            Caption = "Import JSON To Table",
            Category = DatasourceCategory.INMEMORY,
            DatasourceType = DataSourceType.DuckDB,
            PointType = EnumPointType.Function,
            ObjectType = "Table",
            ClassType = "DuckDbExtendedFunctions",
            Showin = ShowinType.Both,
            Order = 11,
            iconimage = "import_json.png",
            misc = "ReturnType: void"
        )]
        public void ImportJsonIntoExistingTable(string filePath, string tableName)
        {
            // Ensure the table exists and its schema matches the JSON structure
            string sql = $"INSERT INTO {tableName} SELECT * FROM read_json('{filePath}');";

           
                using (var command = new DuckDBCommand(sql,DuckConn))
                {
                    //connection.Open();
                    command.ExecuteNonQuery();
                }
           
        }
        [CommandAttribute(
            Name = "ExportToParquet",
            Caption = "Export To Parquet",
            Category = DatasourceCategory.INMEMORY,
            DatasourceType = DataSourceType.DuckDB,
            PointType = EnumPointType.Function,
            ObjectType = "Table",
            ClassType = "DuckDbExtendedFunctions",
            Showin = ShowinType.Both,
            Order = 12,
            iconimage = "export_parquet.png",
            misc = "ReturnType: void"
        )]
        public void ExportToParquet(string tableName, string filePath)
        {
            // This SQL command exports the entire table or the result of a query to a Parquet file
            string sql = $"COPY (SELECT * FROM {tableName}) TO '{filePath}' (FORMAT 'parquet');";

         
                //connection.Open();
                using (var command = new DuckDBCommand(sql,DuckConn))
                {
                    command.ExecuteNonQuery();
                }
           
        }
        [CommandAttribute(
            Name = "ExportQueryResultToParquet",
            Caption = "Export Query To Parquet",
            Category = DatasourceCategory.INMEMORY,
            DatasourceType = DataSourceType.DuckDB,
            PointType = EnumPointType.Function,
            ObjectType = "Query",
            ClassType = "DuckDbExtendedFunctions",
            Showin = ShowinType.Both,
            Order = 13,
            iconimage = "export_parquet.png",
            misc = "ReturnType: void"
        )]
        public void ExportQueryResultToParquet(string query, string filePath)
        {
            // This SQL command exports the result of a query to a Parquet file
            string sql = $"COPY ({query}) TO '{filePath}' (FORMAT 'parquet');";

           
                //connection.Open();
                using (var command = new DuckDBCommand(sql,DuckConn))
                {
                    command.ExecuteNonQuery();
                }
           
        }
        [CommandAttribute(
            Name = "ExecuteParameterizedQuery",
            Caption = "Execute Parameterized Query",
            Category = DatasourceCategory.INMEMORY,
            DatasourceType = DataSourceType.DuckDB,
            PointType = EnumPointType.Function,
            ObjectType = "Query",
            ClassType = "DuckDbExtendedFunctions",
            Showin = ShowinType.Both,
            Order = 14,
            iconimage = "query.png",
            misc = "ReturnType: DataTable"
        )]
        public DataTable ExecuteParameterizedQuery( string sql, Dictionary<string, object> parameters)
        {
            DataTable dataTable = new DataTable();

                using (var command = new DuckDBCommand(sql,DuckConn))
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
        [CommandAttribute(
            Name = "ExportToPartitionedParquet",
            Caption = "Export To Partitioned Parquet",
            Category = DatasourceCategory.INMEMORY,
            DatasourceType = DataSourceType.DuckDB,
            PointType = EnumPointType.Function,
            ObjectType = "Table",
            ClassType = "DuckDbExtendedFunctions",
            Showin = ShowinType.Both,
            Order = 15,
            iconimage = "export_partition.png",
            misc = "ReturnType: void"
        )]
        public void ExportToPartitionedParquet(string tableName, string directoryPath, string partitionColumn)
        {
            // This SQL command exports the table into partitioned Parquet files in the specified directory
            string sql = $"COPY (SELECT * FROM {tableName}) TO '{directoryPath}' (FORMAT 'parquet', PARTITION_BY '{partitionColumn}');";

         
                //connection.Open();
                using (var command = new DuckDBCommand(sql,DuckConn))
                {
                    command.ExecuteNonQuery();
                }
           
        }
        [CommandAttribute(
            Name = "ExportQueryToPartitionedParquet",
            Caption = "Export Query To Partitioned Parquet",
            Category = DatasourceCategory.INMEMORY,
            DatasourceType = DataSourceType.DuckDB,
            PointType = EnumPointType.Function,
            ObjectType = "Query",
            ClassType = "DuckDbExtendedFunctions",
            Showin = ShowinType.Both,
            Order = 16,
            iconimage = "export_partition.png",
            misc = "ReturnType: void"
        )]
        public void ExportQueryToPartitionedParquet(string query, string directoryPath, List<string> partitionColumns)
        {
            string partitionByClause = string.Join(", ", partitionColumns.Select(col => $"'{col}'"));

            // This SQL command exports the query result into partitioned Parquet files in the specified directory
            string sql = $"COPY ({query}) TO '{directoryPath}' (FORMAT 'parquet', PARTITION_BY ({partitionByClause}));";

          
                //connection.Open();
                using (var command = new DuckDBCommand(sql,DuckConn))
                {
                    command.ExecuteNonQuery();
                }
            
        }
        [CommandAttribute(
            Name = "ExecuteNonQuery",
            Caption = "Execute Non Query",
            Category = DatasourceCategory.INMEMORY,
            DatasourceType = DataSourceType.DuckDB,
            PointType = EnumPointType.Function,
            ObjectType = "Query",
            ClassType = "DuckDbExtendedFunctions",
            Showin = ShowinType.Both,
            Order = 17,
            iconimage = "execute.png",
            misc = "ReturnType: int"
        )]
        public int ExecuteNonQuery( string sql)
        {
          
                using (var command = new DuckDBCommand(sql,DuckConn))
                {
                    //connection.Open();
                    return command.ExecuteNonQuery();
                }
            
        }
        [CommandAttribute(
            Name = "GetScalarValue",
            Caption = "Get Scalar Value",
            Category = DatasourceCategory.INMEMORY,
            DatasourceType = DataSourceType.DuckDB,
            PointType = EnumPointType.Function,
            ObjectType = "Query",
            ClassType = "DuckDbExtendedFunctions",
            Showin = ShowinType.Both,
            Order = 18,
            iconimage = "scalar.png",
            misc = "ReturnType: object"
        )]
        public object GetScalarValue( string sql)
        {
           
                using (var command = new DuckDBCommand(sql,DuckConn))
                {
                    //connection.Open();
                    return command.ExecuteScalar();
                }
           
        }
        [CommandAttribute(
            Name = "TableExists",
            Caption = "Table Exists",
            Category = DatasourceCategory.INMEMORY,
            DatasourceType = DataSourceType.DuckDB,
            PointType = EnumPointType.Function,
            ObjectType = "Table",
            ClassType = "DuckDbExtendedFunctions",
            Showin = ShowinType.Both,
            Order = 19,
            iconimage = "table_exists.png",
            misc = "ReturnType: bool"
        )]
        public bool TableExists( string tableName)
        {
            var sql = $"SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = '{tableName}');";
            object result = GetScalarValue(sql);
            return Convert.ToBoolean(result);
        }
        [CommandAttribute(
            Name = "DropTable",
            Caption = "Drop Table",
            Category = DatasourceCategory.INMEMORY,
            DatasourceType = DataSourceType.DuckDB,
            PointType = EnumPointType.Function,
            ObjectType = "Table",
            ClassType = "DuckDbExtendedFunctions",
            Showin = ShowinType.Both,
            Order = 20,
            iconimage = "drop_table.png",
            misc = "ReturnType: void"
        )]
        public void DropTable(string tableName)
        {
            if (TableExists(tableName))
            {
                ExecuteNonQuery($"DROP TABLE {tableName};");
            }
        }
        [CommandAttribute(
            Name = "PrintQueryResults",
            Caption = "Print Query Results",
            Category = DatasourceCategory.INMEMORY,
            DatasourceType = DataSourceType.DuckDB,
            PointType = EnumPointType.Function,
            ObjectType = "Query",
            ClassType = "DuckDbExtendedFunctions",
            Showin = ShowinType.Both,
            Order = 21,
            iconimage = "print.png",
            misc = "ReturnType: void"
        )]
        public void PrintQueryResults(DuckDBResult queryResult)
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
        [CommandAttribute(
            Name = "ParquetMetaData",
            Caption = "Parquet Meta Data",
            Category = DatasourceCategory.INMEMORY,
            DatasourceType = DataSourceType.DuckDB,
            PointType = EnumPointType.Function,
            ObjectType = "Table",
            ClassType = "DuckDbExtendedFunctions",
            Showin = ShowinType.Both,
            Order = 22,
            iconimage = "parquet_meta.png",
            misc = "ReturnType: DataTable"
        )]
        public DataTable ParquetMetaData( string filePath)
        {
            // This SQL command assumes DuckDB will create the table based on the Parquet file schema
            string sql = $"SELECT *  FROM parquet_metadata('{filePath}');";
            DataTable dataTable = new DataTable();

            using (var command = new DuckDBCommand(sql, DuckConn))
            {
                //connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    dataTable.Load(reader);
                }
            }

            return dataTable;
        }
        [CommandAttribute(
            Name = "ReadTextFile",
            Caption = "Read Text File",
            Category = DatasourceCategory.INMEMORY,
            DatasourceType = DataSourceType.DuckDB,
            PointType = EnumPointType.Function,
            ObjectType = "Table",
            ClassType = "DuckDbExtendedFunctions",
            Showin = ShowinType.Both,
            Order = 23,
            iconimage = "text_file.png",
            misc = "ReturnType: DataTable"
        )]
        public DataTable ReadTextFile( string filePath)
        {
            // This SQL command assumes DuckDB will create the table based on the Parquet file schema
            string sql = $"SELECT size, parse_path(filename), content  FROM read_text('{filePath}');";
            DataTable dataTable = new DataTable();

            using (var command = new DuckDBCommand(sql, DuckConn))
            {
                //connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    dataTable.Load(reader);
                }
            }

            return dataTable;
        }
        [CommandAttribute(
            Name = "ReadParquetFile",
            Caption = "Read Parquet File",
            Category = DatasourceCategory.INMEMORY,
            DatasourceType = DataSourceType.DuckDB,
            PointType = EnumPointType.Function,
            ObjectType = "Table",
            ClassType = "DuckDbExtendedFunctions",
            Showin = ShowinType.Both,
            Order = 24,
            iconimage = "parquet_file.png",
            misc = "ReturnType: DataTable"
        )]
        public DataTable ReadParquetFile( string filepath, bool binaryAsString = false, bool filename = false, bool fileRowNumber = false, bool hivePartitioning = false, bool unionByName = false)
        {


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
        [CommandAttribute(
            Name = "ReadMultipleCSVFiles",
            Caption = "Read Multiple CSV Files",
            Category = DatasourceCategory.INMEMORY,
            DatasourceType = DataSourceType.DuckDB,
            PointType = EnumPointType.Function,
            ObjectType = "Table",
            ClassType = "DuckDbExtendedFunctions",
            Showin = ShowinType.Both,
            Order = 25,
            iconimage = "csv_file.png",
            misc = "ReturnType: DataTable"
        )]
        public DataTable ReadMultipleCSVFiles( List<string> filePaths, bool union_by_name = false, bool filename = false)
        {
            string files = string.Join(", ", filePaths.Select(x => $"'{x}'"));
            string sql = $"SELECT * FROM read_csv_auto([{files}]";


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
        [CommandAttribute(
            Name = "JSONLoad",
            Caption = "JSON Load",
            Category = DatasourceCategory.INMEMORY,
            DatasourceType = DataSourceType.DuckDB,
            PointType = EnumPointType.Function,
            ObjectType = "Table",
            ClassType = "DuckDbExtendedFunctions",
            Showin = ShowinType.Both,
            Order = 26,
            iconimage = "json_load.png",
            misc = "ReturnType: DataTable"
        )]
        public DataTable JSONLoad( string filepath, uint maximum_object_size = 16777216, string format = "array", bool ignore_errors = false,
             string compression = "auto", string columns = null, string records = "records", bool auto_detect = false,
             ulong sample_size = 20480, long maximum_depth = -1, string dateformat = "iso", string timestampformat = "iso",
             bool filename = false, bool hive_partitioning = false, bool union_by_name = false)
        {

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
        [CommandAttribute(
            Name = "CSVLoad",
            Caption = "CSV Load",
            Category = DatasourceCategory.INMEMORY,
            DatasourceType = DataSourceType.DuckDB,
            PointType = EnumPointType.Function,
            ObjectType = "Table",
            ClassType = "DuckDbExtendedFunctions",
            Showin = ShowinType.Both,
            Order = 27,
            iconimage = "csv_load.png",
            misc = "ReturnType: DataTable"
        )]
        public DataTable CSVLoad( string filepath, bool all_varchar = false, bool auto_detect = true, string columns = null,
            compressiontype compression = compressiontype.auto, string dateformat = null, char decimal_separator = '.', char delim = ',',
            char escape = '"', bool filename = false, string[] force_not_null = null, bool header = false, bool hive_partitioning = false,
            bool ignore_errors = false, long max_line_size = 2097152, string[] names = null, string new_line = null,
            bool normalize_names = false, string nullstr = null, bool parallel = false, char quote = '"', long sample_size = 20480,
            long skip = 0, string timestampformat = null, string[] types = null, bool union_by_name = false)
        {


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
        #region "Additional DuckDB Functions"
        
        [CommandAttribute(
            Name = "InstallExtension",
            Caption = "Install Extension",
            Category = DatasourceCategory.INMEMORY,
            DatasourceType = DataSourceType.DuckDB,
            PointType = EnumPointType.Function,
            ObjectType = "Extension",
            ClassType = "DuckDbExtendedFunctions",
            Showin = ShowinType.Both,
            Order = 28,
            iconimage = "extension_install.png",
            misc = "ReturnType: void"
        )]
        public void InstallExtension(string extensionName)
        {
            ExecuteNonQuery($"INSTALL {extensionName};");
        }

        [CommandAttribute(
            Name = "LoadExtension",
            Caption = "Load Extension",
            Category = DatasourceCategory.INMEMORY,
            DatasourceType = DataSourceType.DuckDB,
            PointType = EnumPointType.Function,
            ObjectType = "Extension",
            ClassType = "DuckDbExtendedFunctions",
            Showin = ShowinType.Both,
            Order = 29,
            iconimage = "extension_load.png",
            misc = "ReturnType: void"
        )]
        public void LoadExtension(string extensionName)
        {
            ExecuteNonQuery($"LOAD {extensionName};");
        }

        [CommandAttribute(
            Name = "AttachDatabase",
            Caption = "Attach Database",
            Category = DatasourceCategory.INMEMORY,
            DatasourceType = DataSourceType.DuckDB,
            PointType = EnumPointType.Function,
            ObjectType = "Database",
            ClassType = "DuckDbExtendedFunctions",
            Showin = ShowinType.Both,
            Order = 30,
            iconimage = "database_attach.png",
            misc = "ReturnType: void"
        )]
        public void AttachDatabase(string dbPath, string alias, string type = "")
        {
            string sql = $"ATTACH '{dbPath}' AS {alias}";
            if (!string.IsNullOrEmpty(type))
            {
                sql += $" (TYPE {type})";
            }
            ExecuteNonQuery(sql + ";");
        }

        [CommandAttribute(
            Name = "DetachDatabase",
            Caption = "Detach Database",
            Category = DatasourceCategory.INMEMORY,
            DatasourceType = DataSourceType.DuckDB,
            PointType = EnumPointType.Function,
            ObjectType = "Database",
            ClassType = "DuckDbExtendedFunctions",
            Showin = ShowinType.Both,
            Order = 31,
            iconimage = "database_detach.png",
            misc = "ReturnType: void"
        )]
        public void DetachDatabase(string alias)
        {
            ExecuteNonQuery($"DETACH {alias};");
        }

        [CommandAttribute(
            Name = "ExportDatabase",
            Caption = "Export Database",
            Category = DatasourceCategory.INMEMORY,
            DatasourceType = DataSourceType.DuckDB,
            PointType = EnumPointType.Function,
            ObjectType = "Database",
            ClassType = "DuckDbExtendedFunctions",
            Showin = ShowinType.Both,
            Order = 32,
            iconimage = "database_export.png",
            misc = "ReturnType: void"
        )]
        public void ExportDatabase(string directoryPath)
        {
            ExecuteNonQuery($"EXPORT DATABASE '{directoryPath}';");
        }

        [CommandAttribute(
            Name = "ImportDatabase",
            Caption = "Import Database",
            Category = DatasourceCategory.INMEMORY,
            DatasourceType = DataSourceType.DuckDB,
            PointType = EnumPointType.Function,
            ObjectType = "Database",
            ClassType = "DuckDbExtendedFunctions",
            Showin = ShowinType.Both,
            Order = 33,
            iconimage = "database_import.png",
            misc = "ReturnType: void"
        )]
        public void ImportDatabase(string directoryPath)
        {
            ExecuteNonQuery($"IMPORT DATABASE '{directoryPath}';");
        }

        [CommandAttribute(
            Name = "SniffCSV",
            Caption = "Sniff CSV",
            Category = DatasourceCategory.INMEMORY,
            DatasourceType = DataSourceType.DuckDB,
            PointType = EnumPointType.Function,
            ObjectType = "Table",
            ClassType = "DuckDbExtendedFunctions",
            Showin = ShowinType.Both,
            Order = 34,
            iconimage = "csv_sniff.png",
            misc = "ReturnType: DataTable"
        )]
        public DataTable SniffCSV(string filepath)
        {
            using (var cmd = new DuckDBCommand($"SELECT * FROM sniff_csv('{filepath}');", DuckConn))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    DataTable dt = new DataTable();
                    dt.Load(reader);
                    return dt;
                }
            }
        }

        [CommandAttribute(
            Name = "SummarizeTable",
            Caption = "Summarize Table",
            Category = DatasourceCategory.INMEMORY,
            DatasourceType = DataSourceType.DuckDB,
            PointType = EnumPointType.Function,
            ObjectType = "Table",
            ClassType = "DuckDbExtendedFunctions",
            Showin = ShowinType.Both,
            Order = 35,
            iconimage = "table_summarize.png",
            misc = "ReturnType: DataTable"
        )]
        public DataTable SummarizeTable(string tableName)
        {
            using (var cmd = new DuckDBCommand($"SUMMARIZE {tableName};", DuckConn))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    DataTable dt = new DataTable();
                    dt.Load(reader);
                    return dt;
                }
            }
        }

        [CommandAttribute(
            Name = "SetMemoryLimit",
            Caption = "Set Memory Limit",
            Category = DatasourceCategory.INMEMORY,
            DatasourceType = DataSourceType.DuckDB,
            PointType = EnumPointType.Function,
            ObjectType = "Memory",
            ClassType = "DuckDbExtendedFunctions",
            Showin = ShowinType.Both,
            Order = 36,
            iconimage = "memory_limit.png",
            misc = "ReturnType: void"
        )]
        public void SetMemoryLimit(string limit)
        {
            // e.g. limit = '1GB'
            ExecuteNonQuery($"PRAGMA memory_limit='{limit}';");
        }

        #endregion "Additional DuckDB Functions"

        #endregion "Data Import Methods"
    }
}

