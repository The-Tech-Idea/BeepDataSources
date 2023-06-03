using DataManagementModels.DataBase;
using System.Data;
using System.Data.SQLite;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace SqliteInMemoryDBDataSourceCore
{
    [AddinAttribute(Category = DatasourceCategory.INMEMORY, DatasourceType = DataSourceType.SqlLite)]
    public class SqliteInMemoryDBDataSource : RDBSource, IInMemoryDB
    {
        private SQLiteConnection sQLiteConnection;

        public SqliteInMemoryDBDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per) : base(datasourcename, logger, pDMEEditor, databasetype, per)
        {

            Dataconnection.ConnectionProp.DatabaseType = DataSourceType.SqlLite;
            ColumnDelimiter = "[]";
            ParameterDelimiter = "$";
        }

        public int ID { get  ; set  ; }
        public string GuidID { get  ; set  ; }=Guid.NewGuid().ToString();   
        public string ViewName { get  ; set  ; }
     

        public override ConnectionState Openconnection()
        {
            if (ConnectionStatus == ConnectionState.Open)
            {
                DMEEditor.AddLogMessage("Beep", $"Connection is already open", DateTime.Now, -1, "", Errors.Ok);
                return ConnectionState.Open;
            }
            if(Dataconnection.InMemory)
            {
                OpenDatabaseInMemory(Dataconnection.ConnectionProp.Database);
                if (DMEEditor.ErrorObject.Flag == Errors.Ok)
                {
                       
                        ConnectionStatus = ConnectionState.Open;
                    return ConnectionState.Open;
                }
               
            }
            else
            {
                    ConnectionStatus = ConnectionState.Closed;
                    return ConnectionState.Closed;
                }
            return ConnectionStatus;
        }
        public override ConnectionState Closeconnection()
        {
            try
            {
                sQLiteConnection.Close();
                ConnectionStatus = ConnectionState.Closed;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Connection cannot be closed {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
            }
            return ConnectionStatus;
        }
        public IErrorsInfo OpenDatabaseInMemory(string databasename)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                sQLiteConnection = new SQLiteConnection("Data Source=:memory:");
                Dataconnection.InMemory = true;
                databasename = ":memory";
                Dataconnection.ConnectionProp.FileName = ":memory";
                Dataconnection.ConnectionProp.FilePath = ".";
                Dataconnection.ConnectionProp.ConnectionString = "Data Source=:memory:";
                Dataconnection.ConnectionProp.Database = databasename;
                Dataconnection.ConnectionProp.ConnectionName = databasename;
                RDBMSConnection.DbConn = sQLiteConnection;
                ViewName=databasename;
                
                sQLiteConnection.Open();

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Beep", $"Error in Begin Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;


        }
    }
}