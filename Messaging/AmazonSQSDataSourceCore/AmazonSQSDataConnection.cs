using System;
using System.Data;
using Amazon.SQS;
using Amazon.SQS.Model;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.AmazonSQS;

namespace TheTechIdea.Beep.AmazonSQS
{
    public class AmazonSQSDataConnection : IDataConnection
    {
        private AmazonSQSClient _client;
        private bool _disposed = false;

        public AmazonSQSClient Client => _client;

        public AmazonSQSDataConnection(IDMEEditor dMEEditor)
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

        public AmazonSQSConnectionProperties SQSProperties => ConnectionProp as AmazonSQSConnectionProperties;

        public ConnectionState CloseConn()
        {
            try
            {
                _client?.Dispose();
                _client = null;
                ConnectionStatus = ConnectionState.Closed;
                Logger?.WriteLog("[CloseConn] Amazon SQS connection closed.");
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

                var props = SQSProperties;
                if (props == null)
                    throw new InvalidOperationException("Connection properties must be of type AmazonSQSConnectionProperties.");

                var config = new AmazonSQSConfig
                {
                    RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(props.Region ?? "us-east-1")
                };

                _client = new AmazonSQSClient(props.AccessKey, props.SecretKey, config);
                
                // Test connection by getting queue URL
                if (!string.IsNullOrEmpty(props.QueueUrl))
                {
                    var getQueueAttributesRequest = new GetQueueAttributesRequest
                    {
                        QueueUrl = props.QueueUrl,
                        AttributeNames = new System.Collections.Generic.List<string> { "QueueArn" }
                    };
                    _client.GetQueueAttributesAsync(getQueueAttributesRequest).Wait();
                }

                ConnectionStatus = ConnectionState.Open;
                Logger?.WriteLog("[OpenConnection] Amazon SQS connection opened successfully.");
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
            if (SQSProperties == null)
                ConnectionProp = new AmazonSQSConnectionProperties();
            SQSProperties.ConnectionString = connectionstring;
            return OpenConnection();
        }

        public ConnectionState OpenConnection(DataSourceType dbtype, string host, int port, string database, string userid, string password, string parameters)
        {
            if (SQSProperties == null)
                ConnectionProp = new AmazonSQSConnectionProperties();
            SQSProperties.AccessKey = userid;
            SQSProperties.SecretKey = password;
            SQSProperties.Region = host ?? "us-east-1";
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

