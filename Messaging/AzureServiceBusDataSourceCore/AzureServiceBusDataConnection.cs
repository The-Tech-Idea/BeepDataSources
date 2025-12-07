using System;
using System.Data;
using Azure.Messaging.ServiceBus;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.AzureServiceBus;

namespace TheTechIdea.Beep.AzureServiceBus
{
    public class AzureServiceBusDataConnection : IDataConnection
    {
        private ServiceBusClient _client;
        private bool _disposed = false;

        public ServiceBusClient Client => _client;

        public AzureServiceBusDataConnection(IDMEEditor dMEEditor)
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

        public AzureServiceBusConnectionProperties ServiceBusProperties => ConnectionProp as AzureServiceBusConnectionProperties;

        /// <summary>
        /// Closes the Azure Service Bus connection.
        /// </summary>
        public ConnectionState CloseConn()
        {
            try
            {
                if (_client != null && !_client.IsClosed)
                {
                    _client.DisposeAsync().AsTask().Wait();
                    _client = null;
                    ConnectionStatus = ConnectionState.Closed;
                    Logger?.WriteLog("[CloseConn] Azure Service Bus connection closed.");
                }
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                ErrorObject = new ErrorsInfo
                {
                    Flag = Errors.Failed,
                    Message = ex.Message,
                    Ex = ex
                };
                Logger?.WriteLog($"[CloseConn] Error closing connection: {ex.Message}");
            }
            return ConnectionStatus;
        }

        /// <summary>
        /// Opens an Azure Service Bus connection using settings in ConnectionProp.
        /// </summary>
        public ConnectionState OpenConnection()
        {
            try
            {
                if (ConnectionProp == null)
                {
                    throw new InvalidOperationException("Connection properties are not set.");
                }

                var props = ServiceBusProperties;
                if (props == null)
                {
                    throw new InvalidOperationException("Connection properties must be of type AzureServiceBusConnectionProperties.");
                }

                // Use connection string if provided, otherwise construct from properties
                string connectionString = props.ServiceBusConnectionString;
                if (string.IsNullOrEmpty(connectionString) && !string.IsNullOrEmpty(props.FullyQualifiedNamespace))
                {
                    // Could construct connection string from individual properties if needed
                    throw new InvalidOperationException("ServiceBusConnectionString or FullyQualifiedNamespace with credentials must be provided.");
                }

                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("ServiceBusConnectionString is required.");
                }

                var clientOptions = new ServiceBusClientOptions
                {
                    EnableCrossEntityTransactions = false
                };

                _client = new ServiceBusClient(connectionString, clientOptions);
                ConnectionStatus = ConnectionState.Open;
                Logger?.WriteLog("[OpenConnection] Azure Service Bus connection opened successfully.");
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                ErrorObject = new ErrorsInfo
                {
                    Flag = Errors.Failed,
                    Message = ex.Message,
                    Ex = ex
                };
                Logger?.WriteLog($"[OpenConnection] Error opening connection: {ex.Message}");
            }
            return ConnectionStatus;
        }

        /// <summary>
        /// Opens connection with explicit parameters.
        /// </summary>
        public ConnectionState OpenConnection(DataSourceType dbtype, string connectionstring)
        {
            try
            {
                if (ServiceBusProperties == null)
                {
                    ConnectionProp = new AzureServiceBusConnectionProperties();
                }
                ServiceBusProperties.ServiceBusConnectionString = connectionstring;
                return OpenConnection();
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                ErrorObject = new ErrorsInfo
                {
                    Flag = Errors.Failed,
                    Message = ex.Message,
                    Ex = ex
                };
                Logger?.WriteLog($"[OpenConnection] Error: {ex.Message}");
                return ConnectionStatus;
            }
        }

        /// <summary>
        /// Opens connection with individual parameters.
        /// </summary>
        public ConnectionState OpenConnection(DataSourceType dbtype, string host, int port, string database, string userid, string password, string parameters)
        {
            try
            {
                if (ServiceBusProperties == null)
                {
                    ConnectionProp = new AzureServiceBusConnectionProperties();
                }
                ServiceBusProperties.Host = host;
                ServiceBusProperties.FullyQualifiedNamespace = host;
                ServiceBusProperties.UserID = userid;
                ServiceBusProperties.Password = password;
                ServiceBusProperties.Parameters = parameters;
                return OpenConnection();
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                ErrorObject = new ErrorsInfo
                {
                    Flag = Errors.Failed,
                    Message = ex.Message,
                    Ex = ex
                };
                Logger?.WriteLog($"[OpenConnection] Error: {ex.Message}");
                return ConnectionStatus;
            }
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

