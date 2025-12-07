using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Messaging;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.AmazonSQS;

namespace TheTechIdea.Beep.AmazonSQS
{
    [AddinAttribute(Category = DatasourceCategory.MessageQueue, DatasourceType = DataSourceType.AmazonSQS)]
    public class AmazonSQSDataSource : IDataSource, IDisposable, IMessageDataSource<GenericMessage, StreamConfig>
    {
        #region Properties

        private bool _disposed = false;
        private readonly Dictionary<string, string> _queueUrls = new();

        public string ColumnDelimiter { get; set; } = ",";
        public string ParameterDelimiter { get; set; } = ":";
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public DataSourceType DatasourceType { get; set; } = DataSourceType.AmazonSQS;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.MessageQueue;
        public IDataConnection Dataconnection { get; set; }
        public string DatasourceName { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public string Id { get; set; }
        public IDMLogger Logger { get; set; }
        public List<string> EntitiesNames { get; set; } = new List<string>();
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
        public IDMEEditor DMEEditor { get; set; }
        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Closed;
        public event EventHandler<PassedArgs> PassEvent;

        public AmazonSQSDataConnection SQSConnection => Dataconnection as AmazonSQSDataConnection;
        public AmazonSQSConnectionProperties SQSProperties => SQSConnection?.SQSProperties;

        #endregion

        #region Constructor

        public AmazonSQSDataSource(string datasourcename, IDMLogger logger, IDMEEditor dmeEditor, DataSourceType databasetype, IErrorsInfo errorObject)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            DMEEditor = dmeEditor;
            DatasourceType = databasetype;
            ErrorObject = errorObject ?? new ErrorsInfo();
            Category = DatasourceCategory.MessageQueue;

            Dataconnection = new AmazonSQSDataConnection(dmeEditor)
            {
                Logger = logger,
                ErrorObject = ErrorObject,
                DMEEditor = dmeEditor
            };

            if (dmeEditor?.ConfigEditor?.DataConnections != null)
            {
                var connection = dmeEditor.ConfigEditor.DataConnections
                    .FirstOrDefault(c => c.ConnectionName.Equals(datasourcename, StringComparison.InvariantCultureIgnoreCase));
                if (connection != null)
                {
                    Dataconnection.ConnectionProp = connection;
                }
                else
                {
                    Dataconnection.ConnectionProp = new AmazonSQSConnectionProperties { ConnectionName = datasourcename };
                }
            }
        }

        #endregion

        #region IDataSource Methods

        public ConnectionState Openconnection()
        {
            try
            {
                if (SQSConnection?.OpenConnection() == ConnectionState.Open)
                {
                    ConnectionStatus = ConnectionState.Open;
                    Logger?.WriteLog("[Openconnection] Amazon SQS connection opened successfully.");
                    return ConnectionState.Open;
                }
                ConnectionStatus = ConnectionState.Broken;
                return ConnectionState.Broken;
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                ErrorObject = new ErrorsInfo { Flag = Errors.Failed, Message = ex.Message, Ex = ex };
                Logger?.WriteLog($"[Openconnection] Error: {ex.Message}");
                return ConnectionState.Broken;
            }
        }

        public ConnectionState Closeconnection()
        {
            try
            {
                Disconnect();
                ConnectionStatus = ConnectionState.Closed;
                Logger?.WriteLog("[Closeconnection] Amazon SQS connection closed.");
                return ConnectionStatus;
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                Logger?.WriteLog($"[Closeconnection] Error: {ex.Message}");
                return ConnectionState.Broken;
            }
        }

        private async Task<string> GetQueueUrlAsync(string queueName)
        {
            if (_queueUrls.TryGetValue(queueName, out var url))
                return url;

            try
            {
                var request = new GetQueueUrlRequest { QueueName = queueName };
                var response = await SQSConnection.Client.GetQueueUrlAsync(request);
                _queueUrls[queueName] = response.QueueUrl;
                return response.QueueUrl;
            }
            catch (QueueDoesNotExistException)
            {
                // Try to create queue
                var createRequest = new CreateQueueRequest
                {
                    QueueName = queueName,
                    Attributes = new Dictionary<string, string>
                    {
                        { "VisibilityTimeout", SQSProperties?.VisibilityTimeout.ToString() ?? "30" },
                        { "MessageRetentionPeriod", SQSProperties?.MessageRetentionPeriod.ToString() ?? "345600" }
                    }
                };
                if (SQSProperties?.UseFIFO == true)
                {
                    createRequest.Attributes.Add("FifoQueue", "true");
                    createRequest.Attributes.Add("ContentBasedDeduplication", "true");
                }
                var createResponse = await SQSConnection.Client.CreateQueueAsync(createRequest);
                _queueUrls[queueName] = createResponse.QueueUrl;
                return createResponse.QueueUrl;
            }
        }

        public bool CheckEntityExist(string EntityName)
        {
            try
            {
                var url = GetQueueUrlAsync(EntityName).Result;
                return !string.IsNullOrEmpty(url);
            }
            catch
            {
                return false;
            }
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            try
            {
                var url = GetQueueUrlAsync(entity.EntityName).Result;
                return !string.IsNullOrEmpty(url);
            }
            catch
            {
                return false;
            }
        }

        public object GetEntity(string EntityName, List<AppFilter> filter)
        {
            try
            {
                var message = PeekMessageAsync(EntityName, CancellationToken.None).Result;
                return message?.Payload;
            }
            catch
            {
                return null;
            }
        }

        public Task<object> GetEntityAsync(string EntityName, List<AppFilter> filter)
        {
            return Task.Run(async () =>
            {
                var message = await PeekMessageAsync(EntityName, CancellationToken.None);
                return message?.Payload;
            });
        }

        public object GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            var messages = new List<object>();
            try
            {
                var url = GetQueueUrlAsync(EntityName).Result;
                var request = new ReceiveMessageRequest
                {
                    QueueUrl = url,
                    MaxNumberOfMessages = Math.Min(pageSize, 10),
                    WaitTimeSeconds = SQSProperties?.ReceiveMessageWaitTimeSeconds ?? 0
                };
                var response = SQSConnection.Client.ReceiveMessageAsync(request).Result;
                foreach (var msg in response.Messages)
                {
                    messages.Add(msg.Body);
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[GetEntity] Error: {ex.Message}");
            }
            return messages;
        }

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            try
            {
                var message = MessageStandardsHelper.CreateStandardMessage(EntityName, InsertedData, DatasourceName ?? "AmazonSQSDataSource");
                SendMessageAsync(EntityName, message, CancellationToken.None).Wait();
                ErrorObject.Flag = Errors.Ok;
                return ErrorObject;
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                ErrorObject.Ex = ex;
                Logger?.WriteLog($"[InsertEntity] Error: {ex.Message}");
                return ErrorObject;
            }
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow) => InsertEntity(EntityName, UploadDataRow);
        public IErrorsInfo DeleteEntity(string EntityName, object DeletedDataRow) => new ErrorsInfo { Flag = Errors.Ok };
        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            if (UploadData is IEnumerable<object> items)
            {
                int count = 0;
                foreach (var item in items)
                {
                    InsertEntity(EntityName, item);
                    progress?.Report(new PassedArgs { ParameterInt1 = ++count });
                }
            }
            return ErrorObject;
        }

        // IDataSource methods that don't apply
        public IErrorsInfo BeginTransaction(PassedArgs args) => new ErrorsInfo { Flag = Errors.Warning };
        public IErrorsInfo Commit(PassedArgs args) => new ErrorsInfo { Flag = Errors.Warning };
        public IErrorsInfo EndTransaction(PassedArgs args) => new ErrorsInfo { Flag = Errors.Warning };
        public IErrorsInfo ExecuteSql(string sql) => new ErrorsInfo { Flag = Errors.Warning };
        public List<ChildRelation> GetChildTablesList(string tablename, string schemaName, string filterParameters) => new List<ChildRelation>();
        public List<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null) => new List<ETLScriptDet>();
        public IErrorsInfo RunScript(ETLScriptDet dDLScripts) => new ErrorsInfo { Flag = Errors.Warning };
        public IEnumerable<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName) => new List<RelationShipKeys>();
        public EntityStructure GetEntityStructure(string EntityName, bool refresh) => new EntityStructure { EntityName = EntityName };
        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false) => GetEntityStructure(fnd?.EntityName, refresh);
        public Type GetEntityType(string EntityName) => typeof(GenericMessage);
        public IEnumerable<string> GetEntitesList() => EntitiesNames;
        public int GetEntityIdx(string entityName) => EntitiesNames.IndexOf(entityName);
        public IErrorsInfo CreateEntities(List<EntityStructure> entities) => new ErrorsInfo { Flag = Errors.Ok };

        #endregion

        #region IMessageDataSource Implementation

        public void Initialize(StreamConfig config)
        {
            if (config == null || string.IsNullOrEmpty(config.EntityName))
                throw new ArgumentException("EntityName is required in StreamConfig");
            if (!EntitiesNames.Contains(config.EntityName))
                EntitiesNames.Add(config.EntityName);
            Logger?.WriteLog($"[Initialize] Stream '{config.EntityName}' initialized.");
        }

        public async Task SendMessageAsync(string streamName, GenericMessage message, CancellationToken cancellationToken)
        {
            try
            {
                if (SQSConnection?.Client == null)
                    throw new InvalidOperationException("SQS client is not connected. Call OpenConnection() first.");

                message = MessageStandardsHelper.EnsureMessageStandards(message, DatasourceName ?? "AmazonSQSDataSource");
                var validation = MessageStandardsHelper.ValidateMessage(message);
                if (!validation.IsValid)
                    throw new InvalidOperationException($"Message validation failed: {string.Join("; ", validation.Errors)}");

                var queueUrl = await GetQueueUrlAsync(streamName);
                var payload = MessageStandardsHelper.SerializePayload(message.Payload);
                
                var request = new SendMessageRequest
                {
                    QueueUrl = queueUrl,
                    MessageBody = payload,
                    MessageAttributes = new Dictionary<string, MessageAttributeValue>()
                };

                // Add metadata as message attributes
                foreach (var kvp in message.Metadata)
                {
                    request.MessageAttributes[kvp.Key] = new MessageAttributeValue
                    {
                        StringValue = kvp.Value,
                        DataType = "String"
                    };
                }

                // Set delay if in metadata
                if (message.Metadata?.TryGetValue("DelaySeconds", out var delayStr) == true &&
                    int.TryParse(delayStr, out var delay))
                {
                    request.DelaySeconds = Math.Min(delay, 900);
                }
                else if (SQSProperties?.DelaySeconds > 0)
                {
                    request.DelaySeconds = SQSProperties.DelaySeconds;
                }

                // FIFO queue requires MessageGroupId and MessageDeduplicationId
                if (SQSProperties?.UseFIFO == true)
                {
                    request.MessageGroupId = message.CorrelationId ?? message.MessageId;
                    request.MessageDeduplicationId = message.MessageId;
                }

                await SQSConnection.Client.SendMessageAsync(request, cancellationToken);
                Logger?.WriteLog($"[SendMessageAsync] Message sent to '{streamName}' with MessageId: {message.MessageId}");
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[SendMessageAsync] Error: {ex.Message}");
                MessageStandardsHelper.SetErrorMessage(message, ex);
                throw;
            }
        }

        public async Task SubscribeAsync(string streamName, Func<GenericMessage, Task> onMessageReceived, CancellationToken cancellationToken)
        {
            try
            {
                if (SQSConnection?.Client == null)
                    throw new InvalidOperationException("SQS client is not connected.");

                var queueUrl = await GetQueueUrlAsync(streamName);

                while (!cancellationToken.IsCancellationRequested)
                {
                    var request = new ReceiveMessageRequest
                    {
                        QueueUrl = queueUrl,
                        MaxNumberOfMessages = SQSProperties?.MaxNumberOfMessages ?? 1,
                        WaitTimeSeconds = SQSProperties?.ReceiveMessageWaitTimeSeconds ?? 0,
                        MessageAttributeNames = new List<string> { "All" }
                    };

                    var response = await SQSConnection.Client.ReceiveMessageAsync(request, cancellationToken);

                    foreach (var sqsMessage in response.Messages)
                    {
                        try
                        {
                            var genericMessage = new GenericMessage
                            {
                                MessageId = sqsMessage.MessageId ?? Guid.NewGuid().ToString(),
                                EntityName = streamName,
                                Payload = sqsMessage.Body,
                                Timestamp = DateTime.UtcNow
                            };

                            // Extract metadata from message attributes
                            foreach (var attr in sqsMessage.MessageAttributes)
                            {
                                genericMessage.Metadata[attr.Key] = attr.Value.StringValue;
                            }

                            // Store receipt handle for acknowledgment
                            genericMessage.Metadata["ReceiptHandle"] = sqsMessage.ReceiptHandle;

                            genericMessage = MessageStandardsHelper.EnsureMessageStandards(
                                genericMessage,
                                DatasourceName ?? "AmazonSQSDataSource"
                            );

                            if (onMessageReceived != null)
                                await onMessageReceived(genericMessage);

                            // Delete message (acknowledge)
                            await SQSConnection.Client.DeleteMessageAsync(new DeleteMessageRequest
                            {
                                QueueUrl = queueUrl,
                                ReceiptHandle = sqsMessage.ReceiptHandle
                            }, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            Logger?.WriteLog($"[SubscribeAsync] Error processing message: {ex.Message}");
                            // Message will become visible again after visibility timeout
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[SubscribeAsync] Error: {ex.Message}");
                throw;
            }
        }

        public async Task AcknowledgeMessageAsync(string streamName, GenericMessage message, CancellationToken cancellationToken)
        {
            try
            {
                if (message.Metadata?.TryGetValue("ReceiptHandle", out var receiptHandle) == true)
                {
                    var queueUrl = await GetQueueUrlAsync(streamName);
                    await SQSConnection.Client.DeleteMessageAsync(new DeleteMessageRequest
                    {
                        QueueUrl = queueUrl,
                        ReceiptHandle = receiptHandle
                    }, cancellationToken);
                    Logger?.WriteLog($"[AcknowledgeMessageAsync] Message acknowledged for '{streamName}'");
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[AcknowledgeMessageAsync] Error: {ex.Message}");
                throw;
            }
        }

        public async Task<GenericMessage> PeekMessageAsync(string streamName, CancellationToken cancellationToken)
        {
            try
            {
                var queueUrl = await GetQueueUrlAsync(streamName);
                var request = new ReceiveMessageRequest
                {
                    QueueUrl = queueUrl,
                    MaxNumberOfMessages = 1,
                    WaitTimeSeconds = 0, // Short polling for peek
                    MessageAttributeNames = new List<string> { "All" }
                };

                var response = await SQSConnection.Client.ReceiveMessageAsync(request, cancellationToken);
                if (response.Messages.Count == 0)
                    return null;

                var sqsMessage = response.Messages[0];
                var genericMessage = new GenericMessage
                {
                    MessageId = sqsMessage.MessageId ?? Guid.NewGuid().ToString(),
                    EntityName = streamName,
                    Payload = sqsMessage.Body,
                    Timestamp = DateTime.UtcNow
                };

                foreach (var attr in sqsMessage.MessageAttributes)
                {
                    genericMessage.Metadata[attr.Key] = attr.Value.StringValue;
                }

                genericMessage.Metadata["ReceiptHandle"] = sqsMessage.ReceiptHandle;

                genericMessage = MessageStandardsHelper.EnsureMessageStandards(
                    genericMessage,
                    DatasourceName ?? "AmazonSQSDataSource"
                );

                return genericMessage;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[PeekMessageAsync] Error: {ex.Message}");
                throw;
            }
        }

        public async Task<object> GetStreamMetadataAsync(string streamName, CancellationToken cancellationToken)
        {
            try
            {
                var queueUrl = await GetQueueUrlAsync(streamName);
                var request = new GetQueueAttributesRequest
                {
                    QueueUrl = queueUrl,
                    AttributeNames = new List<string> { "All" }
                };

                var response = await SQSConnection.Client.GetQueueAttributesAsync(request, cancellationToken);
                return new
                {
                    QueueUrl = queueUrl,
                    QueueName = streamName,
                    ApproximateNumberOfMessages = response.Attributes.ContainsKey("ApproximateNumberOfMessages") 
                        ? int.Parse(response.Attributes["ApproximateNumberOfMessages"]) : 0,
                    ApproximateNumberOfMessagesNotVisible = response.Attributes.ContainsKey("ApproximateNumberOfMessagesNotVisible")
                        ? int.Parse(response.Attributes["ApproximateNumberOfMessagesNotVisible"]) : 0,
                    VisibilityTimeout = response.Attributes.ContainsKey("VisibilityTimeout")
                        ? int.Parse(response.Attributes["VisibilityTimeout"]) : 0,
                    CreatedTimestamp = response.Attributes.ContainsKey("CreatedTimestamp")
                        ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(response.Attributes["CreatedTimestamp"])).DateTime : (DateTime?)null
                };
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[GetStreamMetadataAsync] Error: {ex.Message}");
                throw;
            }
        }

        public void Disconnect()
        {
            try
            {
                _queueUrls.Clear();
                SQSConnection?.CloseConn();
                Logger?.WriteLog("[Disconnect] Amazon SQS disconnected.");
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[Disconnect] Error: {ex.Message}");
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (!_disposed)
            {
                Disconnect();
                _disposed = true;
            }
        }

        #endregion
    }
}

