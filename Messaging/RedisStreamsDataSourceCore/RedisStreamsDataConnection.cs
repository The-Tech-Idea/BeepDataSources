using System;
using System.Data;
using StackExchange.Redis;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.RedisStreams;

namespace TheTechIdea.Beep.RedisStreams
{
    public class RedisStreamsDataConnection : IDataConnection
    {
        private IConnectionMultiplexer _connection;
        private bool _disposed = false;

        public IConnectionMultiplexer Connection => _connection;
        public IDatabase Database => _connection?.GetDatabase();

        public RedisStreamsDataConnection(IDMEEditor dMEEditor)
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
        public bool InMemory { get; set; } = true;

        public RedisStreamsConnectionProperties RedisProperties => ConnectionProp as RedisStreamsConnectionProperties;

        public ConnectionState CloseConn()
        {
            try
            {
                _connection?.Close();
                _connection?.Dispose();
                _connection = null;
                ConnectionStatus = ConnectionState.Closed;
                Logger?.WriteLog("[CloseConn] Redis connection closed.");
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

                var props = RedisProperties;
                if (props == null)
                    throw new InvalidOperationException("Connection properties must be of type RedisStreamsConnectionProperties.");

                var configuration = props.RedisConnectionString;
                if (string.IsNullOrEmpty(configuration))
                    throw new InvalidOperationException("RedisConnectionString is required.");

                var options = ConfigurationOptions.Parse(configuration);
                if (!string.IsNullOrEmpty(props.Password))
                    options.Password = props.Password;

                _connection = ConnectionMultiplexer.Connect(options);
                ConnectionStatus = ConnectionState.Open;
                Logger?.WriteLog("[OpenConnection] Redis connection opened successfully.");
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
            if (RedisProperties == null)
                ConnectionProp = new RedisStreamsConnectionProperties();
            RedisProperties.RedisConnectionString = connectionstring;
            return OpenConnection();
        }

        public ConnectionState OpenConnection(DataSourceType dbtype, string host, int port, string database, string userid, string password, string parameters)
        {
            if (RedisProperties == null)
                ConnectionProp = new RedisStreamsConnectionProperties();
            RedisProperties.Host = host;
            RedisProperties.Port = port;
            RedisProperties.Password = password;
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

