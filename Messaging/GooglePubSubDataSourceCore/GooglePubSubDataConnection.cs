using System;
using System.Data;
using Google.Cloud.PubSub.V1;
using Google.Apis.Auth.OAuth2;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.GooglePubSub
{
    public class GooglePubSubDataConnection : IDataConnection
    {
        private SubscriberClient _subscriber;
        private PublisherClient _publisher;
        private bool _disposed = false;

        public GooglePubSubDataConnection(IDMEEditor dMEEditor)
        {
            DMEEditor = dMEEditor;
        }

        public GooglePubSubDataConnection(IDMEEditor dMEEditor, IConnectionProperties properties)
        {
            DMEEditor = dMEEditor;
            ConnectionProp = properties;
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
                _subscriber?.Dispose();
                _publisher?.Dispose();
                _subscriber = null;
                _publisher = null;
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
                var props = PubSubProperties;
                if (props == null)
                {
                    ErrorObject = new ErrorsInfo { Flag = Errors.Failed, Message = "PubSub properties are not set." };
                    ConnectionStatus = ConnectionState.Broken;
                    return ConnectionStatus;
                }
                if (string.IsNullOrEmpty(props.ProjectId))
                {
                    ErrorObject = new ErrorsInfo { Flag = Errors.Failed, Message = "ProjectId is required." };
                    ConnectionStatus = ConnectionState.Broken;
                    return ConnectionStatus;
                }

                // Build a PublisherClient and SubscriberClient for the given project.
                if (props.UseEmulator && !string.IsNullOrEmpty(props.EmulatorHost))
                {
                    var builder = new ClientBuilder
                    {
                        EmulatorDetection = EmulatorDetection.EmulatorOrProduction,
                        Endpoint = props.EmulatorHost
                    };
                    _publisher = builder.BuildPublisherClientAsync().GetAwaiter().GetResult();
                    _subscriber = builder.BuildSubscriberClientAsync().GetAwaiter().GetResult();
                }
                else
                {
                    _publisher = new PublisherClientBuilder { ProjectId = props.ProjectId }.BuildAsync().GetAwaiter().GetResult();
                    _subscriber = new SubscriberClientBuilder { ProjectId = props.ProjectId }.BuildAsync().GetAwaiter().GetResult();
                }
                ConnectionStatus = ConnectionState.Open;
                Logger?.WriteLog("[OpenConnection] Google Pub/Sub connection opened.");
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
            try
            {
                if (PubSubProperties == null) ConnectionProp = new GooglePubSubConnectionProperties();
                PubSubProperties.ConnectionString = connectionstring;
                return OpenConnection();
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                ErrorObject = new ErrorsInfo { Flag = Errors.Failed, Message = ex.Message, Ex = ex };
                return ConnectionStatus;
            }
        }

        public ConnectionState OpenConnection(DataSourceType dbtype, string host, int port, string database, string userid, string password, string parameters)
        {
            try
            {
                if (PubSubProperties == null) ConnectionProp = new GooglePubSubConnectionProperties();
                PubSubProperties.ProjectId = host;
                PubSubProperties.Parameters = parameters;
                return OpenConnection();
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                ErrorObject = new ErrorsInfo { Flag = Errors.Failed, Message = ex.Message, Ex = ex };
                return ConnectionStatus;
            }
        }

        public string ReplaceValueFromConnectionString()
        {
            return ConnectionProp?.ConnectionString;
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
