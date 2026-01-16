using System;
using System.Data;
using RabbitMQ.Client;                  // <-- Make sure to add this
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;

namespace RabbitMQDataSourceCore
{
    public class RabbitMQDataConnection : IDataConnection
    {
        // Optionally store the actual RabbitMQ connection
        private IConnection _connection;

        // If you need to expose the connection publicly:
        public IConnection Connection => _connection;

        public RabbitMQDataConnection(IDMEEditor dMEEditor)
        {
            DMEEditor = dMEEditor;
        }

        // This is the "driver" info, presumably loaded from config files
        public ConnectionDriversConfig DataSourceDriver { get; set; }

        // This holds host, port, user, password, etc.
        public IConnectionProperties ConnectionProp { get; set; }

        public ConnectionState ConnectionStatus { get; set; }
        public IDMEEditor DMEEditor { get; set; }
        public int ID { get; set; }
        public string GuidID { get; set; }
        public IDMLogger Logger { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public bool InMemory { get; set; }

        /// <summary>
        /// Closes the RabbitMQ connection, if open.
        /// </summary>
        public ConnectionState CloseConn()
        {
            try
            {
                if (_connection != null && _connection.IsOpen)
                {
                    _connection.CloseAsync();
                    _connection.Dispose();
                    _connection = null;
                    ConnectionStatus = ConnectionState.Closed;
                    Logger?.WriteLog("[CloseConn] RabbitMQ connection closed.");
                }
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                Logger?.WriteLog($"[CloseConn] Error closing connection: {ex.Message}");
            }
            return ConnectionStatus;
        }

        /// <summary>
        /// Opens a RabbitMQ connection using settings in ConnectionProp.
        /// </summary>
        public ConnectionState OpenConnection()
        {
            try
            {
                // Example: If your IConnectionProperties has fields for Host, Port, User, Password, etc.
                var factory = new ConnectionFactory
                {
                    HostName = ConnectionProp?.Host,     // e.g. "localhost"
                    Port = ConnectionProp?.Port ?? 5672, // default port
                    UserName = ConnectionProp?.UserID,   // e.g. "guest"
                    Password = ConnectionProp?.Password, // e.g. "guest"
                    // If you have VirtualHost or other properties, set them here
                    // VirtualHost = ConnectionProp?.Database, etc.
                };

                _connection = factory.CreateConnectionAsync().Result;
                ConnectionStatus = ConnectionState.Open;
                Logger?.WriteLog("[OpenConnection] RabbitMQ connection opened.");
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                Logger?.WriteLog($"[OpenConnection] Error opening connection: {ex.Message}");
            }
            return ConnectionStatus;
        }

        /// <summary>
        /// Opens a connection with more explicit arguments 
        /// (DataSourceType dbtype, host, port, database, user, password, parameters).
        /// </summary>
        public  ConnectionState OpenConnection(
            DataSourceType dbtype,
            string host,
            int port,
            string database,
            string userid,
            string password,
            string parameters)
        {
            // You can store these arguments in ConnectionProp
            // or directly create a ConnectionFactory here:
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = host,
                    Port = port,
                    UserName = userid,
                    Password = password,
                    // For RabbitMQ, "database" might be mapped to VirtualHost
                    VirtualHost = string.IsNullOrWhiteSpace(database) ? "/" : database,
                    // "parameters" could be used for custom config
                };

                _connection =  factory.CreateConnectionAsync().Result;
                ConnectionStatus = ConnectionState.Open;
                Logger?.WriteLog("[OpenConnection overload] RabbitMQ connection opened.");
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                Logger?.WriteLog($"[OpenConnection overload] Error: {ex.Message}");
            }
            return ConnectionStatus;
        }

        /// <summary>
        /// Opens a connection if you only have a single connection string 
        /// (less common for RabbitMQ, but you could parse it).
        /// </summary>
        public ConnectionState OpenConnection(DataSourceType dbtype, string connectionstring)
        {
            try
            {
                // If the connection string is something like "amqp://user:pass@host:port/vhost"
                // You can parse it or let ConnectionFactory handle it:
                var factory = new ConnectionFactory
                {
                    Uri = new Uri(connectionstring)
                };

                _connection = factory.CreateConnectionAsync().Result;
                ConnectionStatus = ConnectionState.Open;
                Logger?.WriteLog("[OpenConnection connectionString] Connection opened via URI.");
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                Logger?.WriteLog($"[OpenConnection connectionString] Error: {ex.Message}");
            }
            return ConnectionStatus;
        }

        /// <summary>
        /// If you need to modify the connection string with placeholders or something else,
        /// implement that logic here.
        /// </summary>
        public string ReplaceValueFromConnectionString()
        {
            // For RabbitMQ, not typically needed. You can return the original or processed value.
            throw new NotImplementedException();
        }
    }
}
