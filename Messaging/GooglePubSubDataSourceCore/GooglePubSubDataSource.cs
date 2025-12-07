using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Messaging;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.GooglePubSub;

namespace TheTechIdea.Beep.GooglePubSub
{
    [AddinAttribute(Category = DatasourceCategory.MessageQueue, DatasourceType = DataSourceType.GooglePubSub)]
    public class GooglePubSubDataSource : IDataSource, IDisposable, IMessageDataSource<GenericMessage, StreamConfig>
    {
        #region Properties

        private bool _disposed = false;
        private readonly Dictionary<string, PublisherClient> _publishers = new();
        private readonly Dictionary<string, SubscriberClient> _subscribers = new();

        public string ColumnDelimiter { get; set; } = ",";
        public string ParameterDelimiter { get; set; } = ":";
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public DataSourceType DatasourceType { get; set; } = DataSourceType.GooglePubSub;
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

        public GooglePubSubDataConnection PubSubConnection => Dataconnection as GooglePubSubDataConnection;
        public GooglePubSubConnectionProperties PubSubProperties => PubSubConnection?.PubSubProperties;

        #endregion

        #region Constructor

        public GooglePubSubDataSource(string datasourcename, IDMLogger logger, IDMEEditor dmeEditor, DataSourceType databasetype, IErrorsInfo errorObject)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            DMEEditor = dmeEditor;
            DatasourceType = databasetype;
            ErrorObject = errorObject ?? new ErrorsInfo();
            Category = DatasourceCategory.MessageQueue;

            Dataconnection = new GooglePubSubDataConnection(dmeEditor)
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
                    Dataconnection.ConnectionProp = new GooglePubSubConnectionProperties { ConnectionName = datasourcename };
                }
            }
        }

        #endregion

        #region IDataSource Methods

        public ConnectionState Openconnection()
        {
            try
            {
                if (PubSubConnection?.OpenConnection() == ConnectionState.Open)
                {
                    ConnectionStatus = ConnectionState.Open;
                    Logger?.WriteLog("[Openconnection] Google Pub/Sub connection opened successfully.");
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
                Logger?.WriteLog("[Closeconnection] Google Pub/Sub connection closed.");
                return ConnectionStatus;
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                Logger?.WriteLog($"[Closeconnection] Error: {ex.Message}");
                return ConnectionState.Broken;
            }
        }

        private TopicName GetTopicName(string topicName)
        {
            var props = PubSubProperties;
            if (props == null || string.IsNullOrEmpty(props.ProjectId))
                throw new InvalidOperationException("ProjectId is required in connection properties.");
            return new TopicName(props.ProjectId, topicName);
        }

        private SubscriptionName GetSubscriptionName(string subscriptionName)
        {
            var props = PubSubProperties;
            if (props == null || string.IsNullOrEmpty(props.ProjectId))
                throw new InvalidOperationException("ProjectId is required in connection properties.");
            return new SubscriptionName(props.ProjectId, subscriptionName);
        }

        public bool CheckEntityExist(string EntityName)
        {
            try
            {
                var topicName = GetTopicName(EntityName);
                var topic = PubSubConnection.PublisherClient.GetTopic(topicName);
                return topic != null;
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
                var topicName = GetTopicName(entity.EntityName);
                PubSubConnection.PublisherClient.CreateTopic(topicName);
                Logger?.WriteLog($"[CreateEntityAs] Topic '{entity.EntityName}' created.");
                return true;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[CreateEntityAs] Error: {ex.Message}");
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
                var subscriptionName = GetSubscriptionName(PubSubProperties?.SubscriptionName ?? EntityName);
                var request = new PullRequest
                {
                    SubscriptionAsSubscriptionName = subscriptionName,
                    MaxMessages = Math.Min(pageSize, 100)
                };
                var response = PubSubConnection.SubscriberClient.Pull(request);
                foreach (var msg in response.ReceivedMessages)
                {
                    messages.Add(msg.Message.Data.ToStringUtf8());
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
                var message = MessageStandardsHelper.CreateStandardMessage(EntityName, InsertedData, DatasourceName ?? "GooglePubSubDataSource");
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
                if (PubSubConnection?.PublisherClient == null)
                    throw new InvalidOperationException("Pub/Sub client is not connected. Call OpenConnection() first.");

                message = MessageStandardsHelper.EnsureMessageStandards(message, DatasourceName ?? "GooglePubSubDataSource");
                var validation = MessageStandardsHelper.ValidateMessage(message);
                if (!validation.IsValid)
                    throw new InvalidOperationException($"Message validation failed: {string.Join("; ", validation.Errors)}");

                var topicName = GetTopicName(streamName);

                // Get or create publisher
                if (!_publishers.TryGetValue(streamName, out var publisher))
                {
                    publisher = await PublisherClient.CreateAsync(topicName);
                    _publishers[streamName] = publisher;
                }

                var payload = MessageStandardsHelper.SerializePayload(message.Payload);
                var pubsubMessage = new PubsubMessage
                {
                    Data = ByteString.CopyFromUtf8(payload),
                    MessageId = message.MessageId
                };

                // Add metadata as attributes
                foreach (var kvp in message.Metadata)
                {
                    pubsubMessage.Attributes[kvp.Key] = kvp.Value;
                }

                var messageId = await publisher.PublishAsync(pubsubMessage);
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
                if (PubSubConnection?.SubscriberClient == null)
                    throw new InvalidOperationException("Pub/Sub client is not connected.");

                var subscriptionName = GetSubscriptionName(PubSubProperties?.SubscriptionName ?? streamName);
                var topicName = GetTopicName(streamName);

                // Ensure subscription exists
                try
                {
                    PubSubConnection.SubscriberClient.GetSubscription(subscriptionName);
                }
                catch
                {
                    // Create subscription
                    var subscription = new Subscription
                    {
                        SubscriptionName = subscriptionName,
                        TopicAsTopicName = topicName,
                        AckDeadlineSeconds = PubSubProperties?.AckDeadlineSeconds ?? 10
                    };
                    PubSubConnection.SubscriberClient.CreateSubscription(subscription);
                }

                // Get or create subscriber
                if (!_subscribers.TryGetValue(streamName, out var subscriber))
                {
                    var settings = new SubscriberClient.ClientCreationSettings();
                    subscriber = await SubscriberClient.CreateAsync(subscriptionName, settings);
                    _subscribers[streamName] = subscriber;
                }

                await subscriber.StartAsync(async (msg, ct) =>
                {
                    try
                    {
                        var body = msg.Data.ToStringUtf8();
                        var genericMessage = new GenericMessage
                        {
                            MessageId = msg.MessageId ?? Guid.NewGuid().ToString(),
                            EntityName = streamName,
                            Payload = body,
                            Timestamp = DateTime.UtcNow
                        };

                        // Extract metadata from attributes
                        foreach (var attr in msg.Attributes)
                        {
                            genericMessage.Metadata[attr.Key] = attr.Value;
                        }

                        // Store acknowledgment ID
                        genericMessage.Metadata["AckId"] = msg.AckId;

                        genericMessage = MessageStandardsHelper.EnsureMessageStandards(
                            genericMessage,
                            DatasourceName ?? "GooglePubSubDataSource"
                        );

                        if (onMessageReceived != null)
                            await onMessageReceived(genericMessage);

                        return SubscriberClient.Reply.Ack;
                    }
                    catch (Exception ex)
                    {
                        Logger?.WriteLog($"[SubscribeAsync] Error processing message: {ex.Message}");
                        return SubscriberClient.Reply.Nack;
                    }
                });
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[SubscribeAsync] Error: {ex.Message}");
                throw;
            }
        }

        public async Task AcknowledgeMessageAsync(string streamName, GenericMessage message, CancellationToken cancellationToken)
        {
            // Acknowledgment is handled automatically in SubscribeAsync
            await Task.CompletedTask;
            Logger?.WriteLog($"[AcknowledgeMessageAsync] Message acknowledged for '{streamName}'");
        }

        public async Task<GenericMessage> PeekMessageAsync(string streamName, CancellationToken cancellationToken)
        {
            try
            {
                var subscriptionName = GetSubscriptionName(PubSubProperties?.SubscriptionName ?? streamName);
                var request = new PullRequest
                {
                    SubscriptionAsSubscriptionName = subscriptionName,
                    MaxMessages = 1,
                    ReturnImmediately = true
                };

                var response = await PubSubConnection.SubscriberClient.PullAsync(request, cancellationToken);
                if (response.ReceivedMessages.Count == 0)
                    return null;

                var msg = response.ReceivedMessages[0];
                var body = msg.Message.Data.ToStringUtf8();
                var genericMessage = new GenericMessage
                {
                    MessageId = msg.Message.MessageId ?? Guid.NewGuid().ToString(),
                    EntityName = streamName,
                    Payload = body,
                    Timestamp = DateTime.UtcNow
                };

                foreach (var attr in msg.Message.Attributes)
                {
                    genericMessage.Metadata[attr.Key] = attr.Value;
                }

                genericMessage.Metadata["AckId"] = msg.AckId;

                genericMessage = MessageStandardsHelper.EnsureMessageStandards(
                    genericMessage,
                    DatasourceName ?? "GooglePubSubDataSource"
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
                var topicName = GetTopicName(streamName);
                var topic = await PubSubConnection.PublisherClient.GetTopicAsync(topicName, cancellationToken);
                return new
                {
                    TopicName = topic.TopicName.TopicId,
                    ProjectId = topic.TopicName.ProjectId,
                    FullName = topic.Name,
                    Labels = topic.Labels
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
                foreach (var publisher in _publishers.Values)
                {
                    publisher?.ShutdownAsync(TimeSpan.FromSeconds(5)).Wait();
                }
                _publishers.Clear();

                foreach (var subscriber in _subscribers.Values)
                {
                    subscriber?.StopAsync(TimeSpan.FromSeconds(5)).Wait();
                }
                _subscribers.Clear();

                PubSubConnection?.CloseConn();
                Logger?.WriteLog("[Disconnect] Google Pub/Sub disconnected.");
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

