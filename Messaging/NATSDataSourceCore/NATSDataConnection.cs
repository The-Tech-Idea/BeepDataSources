using System;
using System.Data;
using NATS.Client;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.NATS;

namespace TheTechIdea.Beep.NATS
{
    public class NATSDataConnection : IDataConnection
    {
        private IConnection _connection;
        private bool _disposed = false;

        public IConnection Connection => _connection;

        public NATSDataConnection(IDMEEditor dMEEditor)
        {
            DMEEditor = dMEEditor;
        }

        public ConnectionDriversConfig DataSourceDriver { get; set; }
        public IConnectionProperties ConnectionProp { get; set; }
        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Closed;
        public IDMEEditor DMEEditor { get; set; }
        public int ID { get; set; }
        public string GuidID { get; set; }
        public IDMLogger Logger { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public bool InMemory { get; set; }

        public NATSConnectionProperties NATSProperties => ConnectionProp as NATSConnectionProperties;

        public ConnectionState CloseConn()
        {
            try
            {
                _connection?.Close();
                _connection?.Dispose();
                _connection = null;
                ConnectionStatus = ConnectionState.Closed;
                Logger?.WriteLog("[CloseConn] NATS connection closed.");
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                ErrorObject = new ErrorsInfo { Flag = Errors.Failed, Message = ex.Message, Ex = ex };
                Logger?.WriteLog($"[CloseConn] Error: {ex.Message}");
            }
            return ConnectionStatus;
        }

        public ConnectionState OpenConnection()
        {
            try
            {
                if (ConnectionProp == null)
                    throw new InvalidOperationException("Connection properties are not set.");

                var props = NATSProperties;
                if (props == null)
                    throw new InvalidOperationException("Connection properties must be of type NATSConnectionProperties.");

                var opts = ConnectionFactory.GetDefaultOptions();
                opts.Url = props.NATSUrl;
                opts.Timeout = props.ConnectionTimeoutMs;
                opts.ReconnectWait = props.ReconnectWaitMs;
                opts.MaxReconnect = props.MaxReconnectAttempts;

                if (!string.IsNullOrEmpty(props.UserID))
                    opts.User = props.UserID;
                if (!string.IsNullOrEmpty(props.Password))
                    opts.Password = props.Password;

                _connection = new ConnectionFactory().CreateConnection(opts);
                ConnectionStatus = ConnectionState.Open;
                Logger?.WriteLog("[OpenConnection] NATS connection opened successfully.");
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                ErrorObject = new ErrorsInfo { Flag = Errors.Failed, Message = ex.Message, Ex = ex };
                Logger?.WriteLog($"[OpenConnection] Error: {ex.Message}");
            }
            return ConnectionStatus;
        }

        public ConnectionState OpenConnection(DataSourceType dbtype, string connectionstring)
        {
            if (NATSProperties == null)
                ConnectionProp = new NATSConnectionProperties();
            NATSProperties.NATSUrl = connectionstring;
            return OpenConnection();
        }

        public ConnectionState OpenConnection(DataSourceType dbtype, string host, int port, string database, string userid, string password, string parameters)
        {
            if (NATSProperties == null)
                ConnectionProp = new NATSConnectionProperties();
            NATSProperties.Host = host;
            NATSProperties.Port = port;
            NATSProperties.UserID = userid;
            NATSProperties.Password = password;
            return OpenConnection();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                CloseConn();
                _disposed = true;
            }
        }
    }
}

