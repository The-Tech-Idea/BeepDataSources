using MassTransit;
using MassTransit.KafkaIntegration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Data;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Messaging;
using TheTechIdea.Beep.Utilities;


namespace TheTechIdea.Beep.MassTransitDataSourceCore
{
    public class MassTransitDataConnection : IDataConnection
    {
        private IBusControl _busControl;
        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Closed;
        public IDictionary<string, StreamConfig> StreamConfigs { get; } = new Dictionary<string, StreamConfig>();
        public IDMLogger Logger { get; set; }
        public IDataConnection Dataconnection { get; set; } // Assuming this holds connection properties

      
        private readonly MassTransitTransportType _transportType;
        private readonly MassTransitSerializerType _serializerType;
        public IServiceCollection Services { get; set; }
        public MassTransitDataConnection(IDMEEditor dMEEditor, MassTransitTransportType transportType, MassTransitSerializerType serializerType)
        {
            DMEEditor = dMEEditor ?? throw new ArgumentNullException(nameof(dMEEditor));
            _transportType = transportType;
            _serializerType = serializerType;
          
        }
        public MassTransitDataConnection(IDMEEditor dMEEditor)
        { 
            DMEEditor = dMEEditor ?? throw new ArgumentNullException(nameof(dMEEditor));
           

        }
        public IConnectionProperties ConnectionProp { get; set; }
        public ConnectionDriversConfig DataSourceDriver { get; set; }
       
        public IDMEEditor DMEEditor { get; set; }
        public int ID { get; set; }
        public string GuidID { get; set; }
     
        public IErrorsInfo ErrorObject { get; set; }
        public bool InMemory { get; set; }

        public ConnectionState OpenConnection()
        {
            try
            {
               
                _busControl.Start();
                ConnectionStatus = ConnectionState.Open;
                Logger?.WriteLog("[OpenConnection] Connection established successfully.");
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Closed;
                Logger?.WriteLog($"[OpenConnection] Failed to open connection: {ex.Message}");
                throw;
            }

            return ConnectionStatus;
        }

        public ConnectionState CloseConn()
        {
            try
            {
                _busControl?.Stop();
                ConnectionStatus = ConnectionState.Closed;
                Logger?.WriteLog("[CloseConn] Connection closed successfully.");
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[CloseConn] Failed to close connection: {ex.Message}");
                throw;
            }

            return ConnectionStatus;
        }

        public ConnectionState OpenConnection(DataSourceType dbtype, string host, int port, string database, string userid, string password, string parameters)
        {
            ConnectionProp = new ConnectionProperties
            {
                Host = host,
                Database = database,
                Port = port,
                UserID = userid,
                Password = password,
                Parameters = parameters
            };
            return OpenConnection();
        }

        public ConnectionState OpenConnection(DataSourceType dbtype, string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            ConnectionProp = ParseConnectionString(connectionString);
            return OpenConnection();
        }

        public string ReplaceValueFromConnectionString()
        {
            return ConnectionProp?.ConnectionString ?? string.Empty;
        }

    
        private ConnectionProperties ParseConnectionString(string connectionString)
        {
            var properties = new ConnectionProperties();
            // Implement connection string parsing logic here
            return properties;
        }
        // Initializes a stream configuration.
        public void Initialize(StreamConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (!StreamConfigs.ContainsKey(config.EntityName))
                StreamConfigs[config.EntityName] = config;

            Logger?.WriteLog($"Stream '{config.EntityName}' initialized.");
        }

        // Sends a message to the specified stream.
        public async Task SendMessageAsync(string streamName, GenericMessage message, CancellationToken cancellationToken)
        {
            if (_busControl == null || ConnectionStatus != ConnectionState.Open)
                throw new InvalidOperationException("Bus is not connected. Call OpenConnection() before sending messages.");

            if (!StreamConfigs.ContainsKey(streamName))
                throw new KeyNotFoundException($"Stream configuration for '{streamName}' not found.");

            var config = StreamConfigs[streamName];
            // Build the endpoint URI. Adjust the URI format as required.
            var endpointUri = new Uri($"{Dataconnection.ConnectionProp.Host}/{config.EntityName}");
            var sendEndpoint = await _busControl.GetSendEndpoint(endpointUri);
            await sendEndpoint.Send(message, cancellationToken);
            Logger?.WriteLog($"Message sent to stream '{streamName}'.");
        }

        // Subscribes to a stream by dynamically creating a receive endpoint.
        public async Task SubscribeAsync(string streamName, Func<GenericMessage, Task> onMessageReceived, CancellationToken cancellationToken)
        {
            if (_busControl == null || ConnectionStatus != ConnectionState.Open)
                throw new InvalidOperationException("Bus is not connected. Call OpenConnection() before subscribing to streams.");

            if (!StreamConfigs.ContainsKey(streamName))
                throw new KeyNotFoundException($"Stream configuration for '{streamName}' not found.");

            var config = StreamConfigs[streamName];

            // Dynamically create a receive endpoint. This works for RabbitMQ (and other transports)
            // without requiring you to rebuild the entire bus.
            var handle = _busControl.ConnectReceiveEndpoint(config.EntityName, ep =>
            {
                // Here we use MassTransit's handler registration for GenericMessage.
                // This inline handler delegates the received message to your callback.
                ep.Handler<GenericMessage>(async context =>
                {
                    await onMessageReceived(context.Message);
                });
            });

            // Wait until the endpoint is ready.
            await handle.Ready;
            Logger?.WriteLog($"Subscribed to stream '{streamName}'.");
        }
    }
}
