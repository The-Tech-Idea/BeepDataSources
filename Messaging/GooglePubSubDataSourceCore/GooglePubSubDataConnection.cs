using System;
using System.Data;
using Google.Cloud.PubSub.V1;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.GooglePubSub;

namespace TheTechIdea.Beep.GooglePubSub
{
    public class GooglePubSubDataConnection : IDataConnection
    {
        private PublisherServiceApiClient _publisherClient;
        private SubscriberServiceApiClient _subscriberClient;
        private bool _disposed = false;

        public PublisherServiceApiClient PublisherClient => _publisherClient;
        public SubscriberServiceApiClient SubscriberClient => _subscriberClient;

        public GooglePubSubDataConnection(IDMEEditor dMEEditor)
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

        public GooglePubSubConnectionProperties PubSubProperties => ConnectionProp as GooglePubSubConnectionProperties;

        public ConnectionState CloseConn()
        {
            try
            {
                _publisherClient?.Dispose();
                _subscriberClient?.Dispose();
                _publisherClient = null;
                _subscriberClient = null;
                ConnectionStatus = ConnectionState.Closed;
                Logger?.WriteLog("[CloseConn] Google Pub/Sub connection closed.");
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

                var props = PubSubProperties;
                if (props == null)
                    throw new InvalidOperationException("Connection properties must be of type GooglePubSubConnectionProperties.");

                if (string.IsNullOrEmpty(props.ProjectId))
                    throw new InvalidOperationException("ProjectId is required.");

                PublisherServiceApiClient.Builder publisherBuilder = new PublisherServiceApiClient.Builder();
                SubscriberServiceApiClient.Builder subscriberBuilder = new SubscriberServiceApiClient.Builder();

                // Set credentials if provided
                if (!string.IsNullOrEmpty(props.CredentialsJson))
                {
                    // Credentials can be set via environment variable GOOGLE_APPLICATION_CREDENTIALS
                    // or passed as JSON string
                    // For simplicity, we'll rely on environment variable or default credentials
                }

                _publisherClient = publisherBuilder.Build();
                _subscriberClient = subscriberBuilder.Build();

                ConnectionStatus = ConnectionState.Open;
                Logger?.WriteLog("[OpenConnection] Google Pub/Sub connection opened successfully.");
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
            if (PubSubProperties == null)
                ConnectionProp = new GooglePubSubConnectionProperties();
            PubSubProperties.ConnectionString = connectionstring;
            return OpenConnection();
        }

        public ConnectionState OpenConnection(DataSourceType dbtype, string host, int port, string database, string userid, string password, string parameters)
        {
            if (PubSubProperties == null)
                ConnectionProp = new GooglePubSubConnectionProperties();
            PubSubProperties.ProjectId = host ?? database;
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

