using DataManagementModels.DataBase;
using DuckDB.NET.Data;
using DuckDB.NET;
using System.Data;
using System.Numerics;
using TheTechIdea;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using static DuckDB.NET.NativeMethods;
using System.Reflection;
using System.Text;
using TheTechIdea.Beep.FileManager;
using System.Xml;

namespace DuckDBDataSourceCore
{
    [AddinAttribute(Category = DatasourceCategory.INMEMORY, DatasourceType = DataSourceType.DuckDB)]
    public class DuckDBDataSource : RDBSource, IDataSource, IInMemoryDB
    {
        private bool disposedValue;
        string dbpath;

        public event EventHandler<PassedArgs> PassEvent;
        public DuckDBConnection DuckConn { get; set; }
      
        public DuckDBDataSource(string pdatasourcename, IDMLogger plogger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per) : base(pdatasourcename, plogger, pDMEEditor, databasetype, per)
        {
            if(per == null)
            {
                per = DMEEditor.ErrorObject;
            }
            if (pdatasourcename != null)
            {
                if (Dataconnection == null)
                {
                    Dataconnection = new RDBDataConnection(DMEEditor);
                }
                Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.FirstOrDefault(p => p.ConnectionName.Equals(pdatasourcename, StringComparison.InvariantCultureIgnoreCase)); ;
            }
            Dataconnection.ConnectionProp.DatabaseType = DataSourceType.DuckDB;
         //   Dataconnection.ConnectionProp.IsInMemory=true;
            ColumnDelimiter = "[]";
            ParameterDelimiter = "$";
            DMEEditor = pDMEEditor;
            DatasourceName=pdatasourcename;
            dbpath = Path.Combine(DMEEditor.ConfigEditor.Config.DataFilePath, DatasourceName);
        }
        public List<EntityStructure> InMemoryStructures { get; set; } = new List<EntityStructure>();
    
        
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
        // ~DuckDBDataSource()
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
        public IErrorsInfo OpenDatabaseInMemory(string databasename)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                Dataconnection.InMemory = true;
                Dataconnection.ConnectionProp.FileName = string.Empty;
                Dataconnection.ConnectionProp.ConnectionString = "DataSource=:memory:?cache=shared";
                Dataconnection.ConnectionProp.Database = databasename;
                Dataconnection.ConnectionProp.ConnectionName = databasename;
                Dataconnection.ConnectionProp.SchemaName = "main";
                // connection = new OdbcConnection("Driver={DuckDB};"+$"Database={databasename};");
                DuckConn= new DuckDBConnection("DataSource=:memory:?cache=shared");
                DuckConn.Open();
                if (DuckConn.State== ConnectionState.Open)
                {
                    Dataconnection.ConnectionStatus = ConnectionState.Open;
                }else
                    Dataconnection.ConnectionStatus = ConnectionState.Closed;


            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Beep", $"Error in Begin Transaction {ex.Message} ", System.DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public string GetConnectionString()
        {
            return Dataconnection.ConnectionProp.ConnectionString;
        }
        public override bool CreateEntityAs(EntityStructure entity)
        {
            string ds = entity.DataSourceID;
            bool retval =  base.CreateEntityAs(entity);
            entity.DataSourceID = ds;

            InMemoryStructures.Add(GetEntity(entity));
            return retval;
        }
        private EntityStructure GetEntity(EntityStructure entity)
        {
            EntityStructure ent = new EntityStructure();
            ent.DatasourceEntityName = entity.DatasourceEntityName;
            ent.DataSourceID = entity.DataSourceID; ;
            ent.DatabaseType = entity.DatabaseType;
            ent.Caption = entity.Caption;
            ent.Category = entity.Category;
            ent.Fields = entity.Fields;
            ent.PrimaryKeys = entity.PrimaryKeys;
            ent.Relations = entity.Relations;
            ent.OriginalEntityName = entity.OriginalEntityName;
            ent.GuidID = Guid.NewGuid().ToString();
            ent.ViewID = entity.ViewID;
            ent.Viewtype = entity.Viewtype;
            ent.EntityName = entity.EntityName;
            ent.OriginalEntityName = entity.OriginalEntityName;
            ent.SchemaOrOwnerOrDatabase = entity.SchemaOrOwnerOrDatabase;
            return ent;
        }
        public IErrorsInfo LoadStructure()
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                string filepath = Path.Combine(dbpath, "createscripts.json");
                string InMemoryStructuresfilepath = Path.Combine(dbpath, "InMemoryStructures.json");
                ConnectionStatus = ConnectionState.Open;
                InMemoryStructures = new List<EntityStructure>();
                Entities = new List<EntityStructure>();
                EntitiesNames = new List<string>();
                CancellationTokenSource token = new CancellationTokenSource();
                if (File.Exists(InMemoryStructuresfilepath))
                {
                    InMemoryStructures = DMEEditor.ConfigEditor.JsonLoader.DeserializeObject<EntityStructure>(InMemoryStructuresfilepath);
                }
                if (File.Exists(filepath))
                {
                    var hdr = DMEEditor.ConfigEditor.JsonLoader.DeserializeSingleObject<ETLScriptHDR>(filepath);
                    DMEEditor.ETL.Script = hdr;
                    DMEEditor.ETL.Script.LastRunDateTime = System.DateTime.Now;
                    DMEEditor.ETL.RunCreateScript(DMEEditor.progress, token.Token);

                }

            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Could not Load InMemory Structure for {DatasourceName}- {ex.Message}", System.DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public IErrorsInfo SaveStructure()
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                if (InMemoryStructures.Count > 0)
                {

                    Directory.CreateDirectory(dbpath);

                    string filepath = Path.Combine(dbpath, "createscripts.json");
                    string InMemoryStructuresfilepath = Path.Combine(dbpath, "InMemoryStructures.json");
                    ETLScriptHDR scriptHDR = new ETLScriptHDR();
                    scriptHDR.ScriptDTL = new List<ETLScriptDet>();
                    CancellationTokenSource token = new CancellationTokenSource();
                    scriptHDR.scriptName = Dataconnection.ConnectionProp.Database;
                    scriptHDR.scriptStatus = "SAVED";
                    scriptHDR.ScriptDTL.AddRange(DMEEditor.ETL.GetCreateEntityScript(this, InMemoryStructures, DMEEditor.progress, token.Token));
                    scriptHDR.ScriptDTL.AddRange(DMEEditor.ETL.GetCopyDataEntityScript(this, InMemoryStructures, DMEEditor.progress, token.Token));
                    DMEEditor.ConfigEditor.JsonLoader.Serialize(filepath, scriptHDR);
                    DMEEditor.ConfigEditor.JsonLoader.Serialize(InMemoryStructuresfilepath, InMemoryStructures);
                }

            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Could not save InMemory Structure for {DatasourceName}- {ex.Message}", System.DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        private static void PrintQueryResults(DuckDBResult queryResult)
        {
            var columnCount = Query.DuckDBColumnCount(queryResult);
            for (var index = 0; index < columnCount; index++)
            {
                var columnName = Query.DuckDBColumnName(queryResult, index).ToManagedString(false);
                Console.Write($"{columnName} ");
            }

            Console.WriteLine();

            var rowCount = Query.DuckDBRowCount(queryResult);
            for (long row = 0; row < rowCount; row++)
            {
                for (long column = 0; column < columnCount; column++)
                {
                    var val = Types.DuckDBValueInt32(queryResult, column, row);
                    Console.Write(val);
                    Console.Write(" ");
                }

                Console.WriteLine();
            }
        }
        #region "IDataSource Properties"


        #endregion "IDataSource Properties"
        #region "IDataSource Methods"
        public override ConnectionState Openconnection()
        {
            ETLScriptHDR scriptHDR = new ETLScriptHDR();
            scriptHDR.ScriptDTL = new List<ETLScriptDet>();
            CancellationTokenSource token = new CancellationTokenSource();

            Dataconnection.InMemory = Dataconnection.ConnectionProp.IsInMemory;
            if (ConnectionStatus == ConnectionState.Open)
            {
                DMEEditor.AddLogMessage("Beep", $"Connection is already open", System.DateTime.Now, -1, "", Errors.Ok);
                return ConnectionState.Open;
            }
            if (Dataconnection.ConnectionProp.IsInMemory)
            {
                OpenDatabaseInMemory(Dataconnection.ConnectionProp.Database);
                base.Openconnection();
                if (DMEEditor.ErrorObject.Flag == Errors.Ok)
                {
                    LoadStructure();
                    return ConnectionState.Open;
                }
            }
            else
            {
                base.Openconnection();

            }

            return ConnectionStatus;
        }
        public override ConnectionState Closeconnection()
        {
            try
            {
                SaveStructure();
                DuckConn.Close();
                Dataconnection.ConnectionStatus = ConnectionState.Closed;
                DMEEditor.AddLogMessage("Success", $"Closing connection to Sqlite Database", System.DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string errmsg = "Error Closing connection to Sqlite Database";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", System.DateTime.Now, 0, null, Errors.Failed);
            }
            //  return RDBMSConnection.DbConn.State;
            return DuckConn.State;

        }
        public override List<string> GetEntitesList()
        {
            if(Entities.Count == 0)
            {
                LoadStructure();
               
            }
            return EntitiesNames;
        }

        #endregion "IDataSource Methods"
        #region "DuckDB Methods"
        public  string CreateSql(string tableName, List<EntityField> fields)
        {
            StringBuilder sql = new StringBuilder();
            sql.Append($"CREATE TABLE {tableName} (");

            for (int i = 0; i < fields.Count; i++)
            {
                EntityField field = fields[i];
                string columnName = field.fieldname;
                string columnType = Convert(field.fieldtype);

                sql.Append($"{columnName} {columnType}");

                if (i < fields.Count - 1)
                    sql.Append(", ");
            }

            sql.Append(");");
            return sql.ToString();
        }
        public  string CreateSql<T>()
        {
            StringBuilder sql = new StringBuilder();
            sql.Append($"CREATE TABLE {typeof(T).Name} (");

            PropertyInfo[] properties = typeof(T).GetProperties();
            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo property = properties[i];
                string columnName = property.Name;
                string columnType = Convert(property.PropertyType);

                sql.Append($"{columnName} {columnType}");

                if (i < properties.Length - 1)
                    sql.Append(", ");
            }

            sql.Append(");");
            return sql.ToString();
        }
        public static string Convert(Type netType)
        {
            if (netType == typeof(bool))
                return "BOOLEAN";
            else if (netType == typeof(sbyte))
                return "TINYINT";
            else if (netType == typeof(byte))
                return "UTINYINT";
            else if (netType == typeof(short))
                return "SMALLINT";
            else if (netType == typeof(ushort))
                return "USMALLINT";
            else if (netType == typeof(int))
                return "INTEGER";
            else if (netType == typeof(uint))
                return "UINTEGER";
            else if (netType == typeof(long))
                return "BIGINT";
            else if (netType == typeof(ulong))
                return "UBIGINT";
            else if (netType == typeof(decimal))
                return "DECIMAL";
            else if (netType == typeof(float))
                return "REAL";
            else if (netType == typeof(double))
                return "DOUBLE";
            else if (netType == typeof(System.DateTime))
                return "TIMESTAMP";
            else if (netType == typeof(string))
                return "VARCHAR";
            else if (netType == typeof(char))
                return "VARCHAR";
            else if (netType == typeof(byte[]))
                return "BLOB";
            else if (netType == typeof(XmlDocument) || netType == typeof(XmlElement) || netType == typeof(XmlNode))
                return "VARCHAR";
            else
                throw new ArgumentException("Unsupported .NET data type: " + netType);
        }
        public DuckDBType ConvertT(string netTypeName)
        {
            Type netType = Type.GetType(netTypeName);

            if (netType == null)
                throw new ArgumentException("Invalid or unknown .NET type name: " + netTypeName);

            if (netType == typeof(bool))
                return DuckDBType.DuckdbTypeBoolean;
            else if (netType == typeof(sbyte))
                return DuckDBType.DuckdbTypeTinyInt;
            else if (netType == typeof(short))
                return DuckDBType.DuckdbTypeSmallInt;
            else if (netType == typeof(int))
                return DuckDBType.DuckdbTypeInteger;
            else if (netType == typeof(long))
                return DuckDBType.DuckdbTypeBigInt;
            else if (netType == typeof(decimal))
                return DuckDBType.DuckdbTypeDecimal;
            else if (netType == typeof(float))
                return DuckDBType.DuckdbTypeFloat;
            else if (netType == typeof(double))
                return DuckDBType.DuckdbTypeDouble;
            else if (netType == typeof(System.DateTime))
                return DuckDBType.DuckdbTypeTimestamp; // or DUCKDB_TYPE_DATE, based on precision needs
            else if (netType == typeof(string))
                return DuckDBType.DuckdbTypeVarchar; // or DUCKDB_TYPE_VARCHAR, based on needs
            else if (netType == typeof(byte[]))
                return DuckDBType.DuckdbTypeBlob;
            // Add more cases if you have more data types
            else
                throw new ArgumentException("Unsupported .NET data type: " + netTypeName);
        }
        public static string Convert(string netTypeName)
        {
            Type netType = Type.GetType(netTypeName);

            if (netType == null)
                throw new ArgumentException("Unrecognized .NET data type: " + netTypeName);

            if (netType == typeof(bool))
                return "BOOLEAN";
            else if (netType == typeof(sbyte))
                return "TINYINT";
            else if (netType == typeof(byte))
                return "UTINYINT";
            else if (netType == typeof(short))
                return "SMALLINT";
            else if (netType == typeof(ushort))
                return "USMALLINT";
            else if (netType == typeof(int))
                return "INTEGER";
            else if (netType == typeof(uint))
                return "UINTEGER";
            else if (netType == typeof(long))
                return "BIGINT";
            else if (netType == typeof(ulong))
                return "UBIGINT";
            else if (netType == typeof(decimal))
                return "DECIMAL";
            else if (netType == typeof(float))
                return "REAL";
            else if (netType == typeof(double))
                return "DOUBLE";
            else if (netType == typeof(System.DateTime))
                return "TIMESTAMP";
            else if (netType == typeof(string))
                return "VARCHAR";
            else if (netType == typeof(char))
                return "VARCHAR";
            else if (netType == typeof(byte[]))
                return "BLOB";
            else if (netType == typeof(XmlDocument) || netType == typeof(XmlElement) || netType == typeof(XmlNode))
                return "VARCHAR";
            else
                throw new ArgumentException("Unsupported .NET data type: " + netTypeName);
        }

        #endregion
        #region "Insert or Update or Delete Objects"
        EntityStructure DataStruct = null;
        IDbCommand command = null;
        Type enttype = null;
        bool ObjectsCreated = false;
        string lastentityname = null;
        #endregion
        private void SetObjects(string Entityname)
        {
            if (!ObjectsCreated || Entityname != lastentityname)
            {
                DataStruct = GetEntityStructure(Entityname, true);
                command = RDBMSConnection.DbConn.CreateCommand();
                enttype = GetEntityType(Entityname);
                ObjectsCreated = true;
                lastentityname = Entityname;
            }
        }
        //public  Type GetFieldType(int ordinal)
        //{
        //    return NativeMethods.Query.DuckDBColumnType(currentResult, ordinal) switch
        //    {
        //        DuckDBType.DuckdbTypeInvalid => throw new DuckDBException("Invalid type"),
        //        DuckDBType.DuckdbTypeBoolean => typeof(bool),
        //        DuckDBType.DuckdbTypeTinyInt => typeof(sbyte),
        //        DuckDBType.DuckdbTypeSmallInt => typeof(short),
        //        DuckDBType.DuckdbTypeInteger => typeof(int),
        //        DuckDBType.DuckdbTypeBigInt => typeof(long),
        //        DuckDBType.DuckdbTypeUnsignedTinyInt => typeof(byte),
        //        DuckDBType.DuckdbTypeUnsignedSmallInt => typeof(ushort),
        //        DuckDBType.DuckdbTypeUnsignedInteger => typeof(uint),
        //        DuckDBType.DuckdbTypeUnsignedBigInt => typeof(ulong),
        //        DuckDBType.DuckdbTypeFloat => typeof(float),
        //        DuckDBType.DuckdbTypeDouble => typeof(double),
        //        DuckDBType.DuckdbTypeTimestamp => typeof(DateTime),
        //        DuckDBType.DuckdbTypeInterval => typeof(DuckDBInterval),
        //        DuckDBType.DuckdbTypeDate => typeof(DuckDBDateOnly),
        //        DuckDBType.DuckdbTypeTime => typeof(DuckDBTimeOnly),
        //        DuckDBType.DuckdbTypeHugeInt => typeof(BigInteger),
        //        DuckDBType.DuckdbTypeVarchar => typeof(string),
        //        DuckDBType.DuckdbTypeDecimal => typeof(decimal),
        //        DuckDBType.DuckdbTypeBlob => typeof(Stream),
        //        var type => throw new ArgumentException($"Unrecognised type {type} ({(int)type}) in column {ordinal + 1}")
        //    };
        //}
    }
}
