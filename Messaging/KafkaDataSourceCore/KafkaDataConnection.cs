using System;
using System.Data;
using System.Net;
using Confluent.Kafka;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.EventStream.Kafka
{
    public class KafkaDataConnection : IDataConnection
    {
        public IConnectionProperties ConnectionProp { get; set; }
        public ConnectionDriversConfig DataSourceDriver { get; set; }
        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Closed;
        public IDMEEditor DMEEditor { get; set; }
        public int ID { get; set; }
        public IDMLogger Logger { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public IDbConnection DbConn { get; set; } // Not applicable for Kafka, kept for interface compatibility
        public ProducerConfig ProdConfig { get; private set; }
        public ConsumerConfig ConsConfig { get; private set; }
        public string GuidID { get; set; }
        public bool InMemory { get; set; }

        /// <summary>
        /// Closes the Kafka connection.
        /// </summary>
        public ConnectionState CloseConn()
        {
            try
            {
                ProdConfig = null;
                ConsConfig = null;
                ConnectionStatus = ConnectionState.Closed;
                Logger?.WriteLog("[CloseConn] Kafka connection closed successfully.");
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[CloseConn] Error closing Kafka connection: {ex.Message}");
            }
            return ConnectionStatus;
        }

        /// <summary>
        /// Opens a Kafka connection using the predefined `ConnectionProp`.
        /// </summary>
        public ConnectionState OpenConnection()
        {
            try
            {
                if (ConnectionStatus == ConnectionState.Closed)
                {
                    ProdConfig = CreateProducerConfig(ConnectionProp);
                    ConsConfig = CreateConsumerConfig(ConnectionProp);
                    ConnectionStatus = ConnectionState.Open;

                    Logger?.WriteLog("[OpenConnection] Kafka connection opened successfully.");
                }
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Closed;
                Logger?.WriteLog($"[OpenConnection] Error opening Kafka connection: {ex.Message}");
            }
            return ConnectionStatus;
        }

        /// <summary>
        /// Opens a Kafka connection using the specified parameters.
        /// </summary>
        public ConnectionState OpenConnection(DataSourceType dbtype, string host, int port, string database, string userid, string password, string parameters)
        {
            try
            {
                if (ConnectionStatus == ConnectionState.Closed)
                {
                    var connectionProps = new ConnectionProperties
                    {
                        Host = host,
                        Port = port,
                        Database = database,
                        UserID = userid,
                        Password = password
                    };

                    ProdConfig = CreateProducerConfig(connectionProps);
                    ConsConfig = CreateConsumerConfig(connectionProps);

                    ConnectionStatus = ConnectionState.Open;
                    Logger?.WriteLog("[OpenConnection] Kafka connection opened successfully with custom parameters.");
                }
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Closed;
                Logger?.WriteLog($"[OpenConnection] Error opening Kafka connection: {ex.Message}");
            }
            return ConnectionStatus;
        }

        /// <summary>
        /// Not implemented: connection via connection string.
        /// </summary>
        public ConnectionState OpenConnection(DataSourceType dbtype, string connectionstring)
        {
            throw new NotImplementedException("OpenConnection with connection string is not implemented for Kafka.");
        }

        /// <summary>
        /// Creates a Kafka producer configuration.
        /// </summary>
        private ProducerConfig CreateProducerConfig(IConnectionProperties connectionProps)
        {
            return new ProducerConfig
            {
                BootstrapServers = connectionProps.Host,
                ClientId = Dns.GetHostName(),
                SaslMechanism = SaslMechanism.Plain,
                SecurityProtocol = SecurityProtocol.SaslSsl,
                SaslUsername = connectionProps.UserID,
                SaslPassword = connectionProps.Password
            };
        }

        /// <summary>
        /// Creates a Kafka consumer configuration.
        /// </summary>
        private ConsumerConfig CreateConsumerConfig(IConnectionProperties connectionProps)
        {
            return new ConsumerConfig
            {
                BootstrapServers = connectionProps.Host,
                GroupId = connectionProps.Database,
                ClientId = Dns.GetHostName(),
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = true,
                SaslMechanism = SaslMechanism.Plain,
                SecurityProtocol = SecurityProtocol.SaslSsl,
                SaslUsername = connectionProps.UserID,
                SaslPassword = connectionProps.Password
            };
        }

        /// <summary>
        /// Kafka-specific method to replace value from the connection string. Not implemented.
        /// </summary>
        public string ReplaceValueFromConnectionString()
        {
            throw new NotImplementedException();
        }
    }
}
