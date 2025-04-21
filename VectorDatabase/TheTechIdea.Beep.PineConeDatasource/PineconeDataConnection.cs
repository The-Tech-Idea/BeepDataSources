using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.PineConeDatasource
{
    public class PineconeDataConnection : IDataConnection
    {
        public PineconeDataConnection(IDMEEditor dMEEditor)
        {
            DMEEditor = dMEEditor;
            ErrorObject = DMEEditor.ErrorObject;
            Logger = DMEEditor.Logger;
        }

        public IConnectionProperties ConnectionProp { get; set; }
        public ConnectionDriversConfig DataSourceDriver { get; set; }
        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Closed;
        public IDMEEditor DMEEditor { get; set; }
        public int ID { get; set; }
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public IDMLogger Logger { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public bool InMemory { get; set; }

        public ConnectionState CloseConn()
        {
            ConnectionStatus = ConnectionState.Closed;
            return ConnectionStatus;
        }

        public ConnectionState OpenConnection()
        {
            try
            {
                // Set the connection status to open since Pinecone is a REST API
                ConnectionStatus = ConnectionState.Open;
                return ConnectionStatus;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", $"Error opening Pinecone connection: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                ConnectionStatus = ConnectionState.Broken;
                return ConnectionStatus;
            }
        }

        public ConnectionState OpenConnection(DataSourceType dbtype, string host, int port, string database, string userid, string password, string parameters)
        {
            ConnectionProp = new ConnectionProperties
            {
                DatabaseType = dbtype,
                Host = host,
                Port = port,
                Database = database,
                UserID = userid,
                Password = password,
                Parameters = parameters
            };

            return OpenConnection();
        }

        public ConnectionState OpenConnection(DataSourceType dbtype, string connectionstring)
        {
            ConnectionProp = new ConnectionProperties
            {
                DatabaseType = dbtype,
                ConnectionString = connectionstring
            };

            return OpenConnection();
        }

        public string ReplaceValueFromConnectionString()
        {
            // For Pinecone API, connection string would be ApiKey and environment
            return ConnectionProp.ConnectionString;
        }
    }

}
