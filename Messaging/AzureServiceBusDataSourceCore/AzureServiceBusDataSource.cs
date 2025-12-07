using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Messaging;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.AzureServiceBus;

namespace TheTechIdea.Beep.AzureServiceBus
{
    [AddinAttribute(Category = DatasourceCategory.MessageQueue, DatasourceType = DataSourceType.AzureServiceBus)]
    public class AzureServiceBusDataSource : IDataSource, IDisposable, IMessageDataSource<GenericMessage, StreamConfig>
    {
        #region Properties

        private readonly Dictionary<string, ServiceBusSender> _senders = new();
        private readonly Dictionary<string, ServiceBusReceiver> _receivers = new();
        private readonly Dictionary<string, ServiceBusProcessor> _processors = new();
        private bool _disposed = false;

        public string ColumnDelimiter { get; set; } = ",";
        public string ParameterDelimiter { get; set; } = ":";
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public DataSourceType DatasourceType { get; set; } = DataSourceType.AzureServiceBus;
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

        public AzureServiceBusDataConnection ServiceBusConnection => Dataconnection as AzureServiceBusDataConnection;
        public AzureServiceBusConnectionProperties ServiceBusProperties => ServiceBusConnection?.ServiceBusProperties;

        #endregion

        #region Constructor

        public AzureServiceBusDataSource(string datasourcename, IDMLogger logger, IDMEEditor dmeEditor, DataSourceType databasetype, IErrorsInfo errorObject)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            DMEEditor = dmeEditor;
            DatasourceType = databasetype;
            ErrorObject = errorObject ?? new ErrorsInfo();
            Category = DatasourceCategory.MessageQueue;

            Dataconnection = new AzureServiceBusDataConnection(dmeEditor)
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
                    // Create default connection properties
                    Dataconnection.ConnectionProp = new AzureServiceBusConnectionProperties
                    {
                        ConnectionName = datasourcename
                    };
                }
            }
        }

        #endregion

        #region IDataSource Methods

        public ConnectionState Openconnection()
        {
            try
            {
                if (ServiceBusConnection?.OpenConnection() == ConnectionState.Open)
                {
                    ConnectionStatus = ConnectionState.Open;
                    Logger?.WriteLog("[Openconnection] Azure Service Bus connection opened successfully.");
                    return ConnectionState.Open;
                }
                else
                {
                    ConnectionStatus = ConnectionState.Broken;
                    Logger?.WriteLog("[Openconnection] Failed to open Azure Service Bus connection.");
                    return ConnectionState.Broken;
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
                Logger?.WriteLog("[Closeconnection] Azure Service Bus connection closed.");
                return ConnectionStatus;
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                Logger?.WriteLog($"[Closeconnection] Error: {ex.Message}");
                return ConnectionState.Broken;
            }
        }

        public bool CheckEntityExist(string EntityName)
        {
            // Azure Service Bus doesn't have a direct way to check if queue/topic exists
            // We'll try to create a sender/receiver and catch exceptions
            try
            {
                if (ServiceBusConnection?.Client == null)
                    return false;

                // Try to get a sender (this will fail if entity doesn't exist)
                var sender = ServiceBusConnection.Client.CreateSender(EntityName);
                sender.DisposeAsync().AsTask().Wait();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            // Azure Service Bus entities must be created via Azure Portal, PowerShell, or Management API
            // This method just validates the entity name
            Logger?.WriteLog($"[CreateEntityAs] Azure Service Bus entities must be created via Azure Portal or Management API. Entity: {entity?.EntityName}");
            return true;
        }

        public object GetEntity(string EntityName, List<AppFilter> filter)
        {
            try
            {
                var message = PeekMessageAsync(EntityName, CancellationToken.None).Result;
                return message?.Payload;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[GetEntity] Error: {ex.Message}");
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
            // Azure Service Bus doesn't support paging in the traditional sense
            // Return a list of peeked messages
            var messages = new List<object>();
            try
            {
                for (int i = 0; i < pageSize; i++)
                {
                    var message = PeekMessageAsync(EntityName, CancellationToken.None).Result;
                    if (message == null) break;
                    messages.Add(message.Payload);
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
                var message = MessageStandardsHelper.CreateStandardMessage(
                    EntityName,
                    InsertedData,
                    DatasourceName ?? "AzureServiceBusDataSource"
                );

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

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            // Azure Service Bus doesn't support updates - send as new message
            return InsertEntity(EntityName, UploadDataRow);
        }

        public IErrorsInfo DeleteEntity(string EntityName, object DeletedDataRow)
        {
            // Azure Service Bus doesn't support deletes - this would be handled by message consumption
            ErrorObject.Flag = Errors.Ok;
            Logger?.WriteLog("[DeleteEntity] Azure Service Bus handles deletion through message consumption.");
            return ErrorObject;
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            try
            {
                if (UploadData is IEnumerable<object> items)
                {
                    int count = 0;
                    foreach (var item in items)
                    {
                        InsertEntity(EntityName, item);
                        count++;
                        progress?.Report(new PassedArgs { ParameterInt1 = count });
                    }
                }
                ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
            }
            return ErrorObject;
        }

        // IDataSource methods that don't apply to messaging
        public IErrorsInfo BeginTransaction(PassedArgs args) => new ErrorsInfo { Flag = Errors.Warning, Message = "Not supported" };
        public IErrorsInfo Commit(PassedArgs args) => new ErrorsInfo { Flag = Errors.Warning, Message = "Not supported" };
        public IErrorsInfo EndTransaction(PassedArgs args) => new ErrorsInfo { Flag = Errors.Warning, Message = "Not supported" };
        public IErrorsInfo ExecuteSql(string sql) => new ErrorsInfo { Flag = Errors.Warning, Message = "Not supported" };
        public List<ChildRelation> GetChildTablesList(string tablename, string schemaName, string filterParameters) => new List<ChildRelation>();
        public List<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null) => new List<ETLScriptDet>();
        public IErrorsInfo RunScript(ETLScriptDet dDLScripts) => new ErrorsInfo { Flag = Errors.Warning, Message = "Not supported" };
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
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (string.IsNullOrEmpty(config.EntityName))
                throw new ArgumentException("EntityName is required in StreamConfig", nameof(config));

            if (!EntitiesNames.Contains(config.EntityName))
            {
                EntitiesNames.Add(config.EntityName);
            }

            Logger?.WriteLog($"[Initialize] Stream '{config.EntityName}' initialized.");
        }

        public async Task SendMessageAsync(string streamName, GenericMessage message, CancellationToken cancellationToken)
        {
            try
            {
                if (ServiceBusConnection?.Client == null)
                    throw new InvalidOperationException("Service Bus client is not connected. Call OpenConnection() first.");

                // Ensure message follows standards
                message = MessageStandardsHelper.EnsureMessageStandards(message, DatasourceName ?? "AzureServiceBusDataSource");

                // Validate message
                var validation = MessageStandardsHelper.ValidateMessage(message);
                if (!validation.IsValid)
                {
                    var errorMsg = $"Message validation failed: {string.Join("; ", validation.Errors)}";
                    Logger?.WriteLog($"[SendMessageAsync] {errorMsg}");
                    throw new InvalidOperationException(errorMsg);
                }

                // Get or create sender
                if (!_senders.TryGetValue(streamName, out var sender))
                {
                    sender = ServiceBusConnection.Client.CreateSender(streamName);
                    _senders[streamName] = sender;
                }

                // Use standard serialization
                var payload = MessageStandardsHelper.SerializePayload(message.Payload);
                var serviceBusMessage = new ServiceBusMessage(payload)
                {
                    MessageId = message.MessageId,
                    ContentType = message.ContentType ?? "application/json",
                    Subject = message.EntityName
                };

                // Add metadata as application properties
                foreach (var kvp in message.Metadata)
                {
                    serviceBusMessage.ApplicationProperties[kvp.Key] = kvp.Value;
                }

                // Set priority if available
                if (message.Priority.HasValue)
                {
                    // Azure Service Bus uses a different priority system, map if needed
                }

                // Set scheduled time if in metadata
                if (message.Metadata?.TryGetValue("ScheduledAt", out var scheduledAt) == true &&
                    DateTime.TryParse(scheduledAt, out var scheduledTime))
                {
                    serviceBusMessage.ScheduledEnqueueTime = scheduledTime;
                }

                await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
                Logger?.WriteLog($"[SendMessageAsync] Message sent to '{streamName}' with MessageId: {message.MessageId}");
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[SendMessageAsync] Error sending message to '{streamName}': {ex.Message}");
                MessageStandardsHelper.SetErrorMessage(message, ex);
                throw;
            }
        }

        public async Task SubscribeAsync(string streamName, Func<GenericMessage, Task> onMessageReceived, CancellationToken cancellationToken)
        {
            try
            {
                if (ServiceBusConnection?.Client == null)
                    throw new InvalidOperationException("Service Bus client is not connected. Call OpenConnection() first.");

                var props = ServiceBusProperties;
                var options = new ServiceBusProcessorOptions
                {
                    MaxConcurrentCalls = props?.MaxConcurrentCalls ?? 1,
                    PrefetchCount = props?.PrefetchCount ?? 0,
                    AutoCompleteMessages = false // We'll manually acknowledge
                };

                ServiceBusProcessor processor;
                if (props?.UseSessions == true)
                {
                    // Use session processor
                    processor = ServiceBusConnection.Client.CreateSessionProcessor(
                        streamName,
                        props.SubscriptionName,
                        options);
                }
                else
                {
                    // Use regular processor
                    if (!string.IsNullOrEmpty(props?.SubscriptionName))
                    {
                        // Topic subscription
                        processor = ServiceBusConnection.Client.CreateProcessor(
                            streamName,
                            props.SubscriptionName,
                            options);
                    }
                    else
                    {
                        // Queue
                        processor = ServiceBusConnection.Client.CreateProcessor(streamName, options);
                    }
                }

                processor.ProcessMessageAsync += async args =>
                {
                    try
                    {
                        var body = args.Message.Body.ToString();
                        var genericMessage = new GenericMessage
                        {
                            MessageId = args.Message.MessageId ?? Guid.NewGuid().ToString(),
                            EntityName = streamName,
                            Payload = body,
                            Timestamp = args.Message.EnqueuedTime.UtcDateTime,
                            DeliveryTag = (ulong?)args.Message.DeliveryCount
                        };

                        // Extract metadata from application properties
                        foreach (var prop in args.Message.ApplicationProperties)
                        {
                            genericMessage.Metadata[prop.Key] = prop.Value?.ToString();
                        }

                        // Set standard metadata
                        if (args.Message.ContentType != null)
                            genericMessage.ContentType = args.Message.ContentType;
                        if (args.Message.Subject != null)
                            genericMessage.EntityName = args.Message.Subject;

                        // Ensure standards compliance
                        genericMessage = MessageStandardsHelper.EnsureMessageStandards(
                            genericMessage,
                            DatasourceName ?? "AzureServiceBusDataSource"
                        );

                        // Invoke callback
                        if (onMessageReceived != null)
                        {
                            await onMessageReceived(genericMessage);
                        }

                        // Acknowledge message
                        await args.CompleteMessageAsync(args.Message);
                    }
                    catch (Exception ex)
                    {
                        Logger?.WriteLog($"[SubscribeAsync] Error processing message: {ex.Message}");
                        // Abandon message for retry or send to dead-letter queue
                        await args.AbandonMessageAsync(args.Message);
                        throw;
                    }
                };

                processor.ProcessErrorAsync += args =>
                {
                    Logger?.WriteLog($"[SubscribeAsync] Error: {args.Exception.Message}");
                    return Task.CompletedTask;
                };

                _processors[streamName] = processor;
                await processor.StartProcessingAsync(cancellationToken);
                Logger?.WriteLog($"[SubscribeAsync] Subscribed to '{streamName}'");
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[SubscribeAsync] Error subscribing to '{streamName}': {ex.Message}");
                throw;
            }
        }

        public async Task AcknowledgeMessageAsync(string streamName, GenericMessage message, CancellationToken cancellationToken)
        {
            // Acknowledgment is handled automatically in SubscribeAsync via CompleteMessageAsync
            // This method is provided for interface compliance
            await Task.CompletedTask;
            Logger?.WriteLog($"[AcknowledgeMessageAsync] Message acknowledged for '{streamName}' with MessageId: {message.MessageId}");
        }

        public async Task<GenericMessage> PeekMessageAsync(string streamName, CancellationToken cancellationToken)
        {
            try
            {
                if (ServiceBusConnection?.Client == null)
                    throw new InvalidOperationException("Service Bus client is not connected.");

                // Get or create receiver
                if (!_receivers.TryGetValue(streamName, out var receiver))
                {
                    var props = ServiceBusProperties;
                    if (!string.IsNullOrEmpty(props?.SubscriptionName))
                    {
                        // Topic subscription
                        receiver = ServiceBusConnection.Client.CreateReceiver(streamName, props.SubscriptionName);
                    }
                    else
                    {
                        // Queue
                        receiver = ServiceBusConnection.Client.CreateReceiver(streamName);
                    }
                    _receivers[streamName] = receiver;
                }

                // Peek message (doesn't remove from queue)
                var message = await receiver.PeekMessageAsync(cancellationToken: cancellationToken);
                if (message == null)
                    return null;

                var body = message.Body.ToString();
                var genericMessage = new GenericMessage
                {
                    MessageId = message.MessageId ?? Guid.NewGuid().ToString(),
                    EntityName = streamName,
                    Payload = body,
                    Timestamp = message.EnqueuedTime.UtcDateTime
                };

                // Extract metadata
                foreach (var prop in message.ApplicationProperties)
                {
                    genericMessage.Metadata[prop.Key] = prop.Value?.ToString();
                }

                // Ensure standards compliance
                genericMessage = MessageStandardsHelper.EnsureMessageStandards(
                    genericMessage,
                    DatasourceName ?? "AzureServiceBusDataSource"
                );

                return genericMessage;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[PeekMessageAsync] Error peeking message from '{streamName}': {ex.Message}");
                throw;
            }
        }

        public async Task<object> GetStreamMetadataAsync(string streamName, CancellationToken cancellationToken)
        {
            try
            {
                // Azure Service Bus doesn't provide direct metadata API in the client library
                // Return basic information
                return new
                {
                    StreamName = streamName,
                    IsInitialized = EntitiesNames.Contains(streamName),
                    HasSender = _senders.ContainsKey(streamName),
                    HasReceiver = _receivers.ContainsKey(streamName),
                    HasProcessor = _processors.ContainsKey(streamName),
                    ConnectionStatus = ConnectionStatus.ToString()
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
                // Dispose all senders
                foreach (var sender in _senders.Values)
                {
                    sender?.DisposeAsync().AsTask().Wait();
                }
                _senders.Clear();

                // Dispose all receivers
                foreach (var receiver in _receivers.Values)
                {
                    receiver?.DisposeAsync().AsTask().Wait();
                }
                _receivers.Clear();

                // Stop and dispose all processors
                foreach (var processor in _processors.Values)
                {
                    processor?.StopProcessingAsync().Wait();
                    processor?.DisposeAsync().AsTask().Wait();
                }
                _processors.Clear();

                // Close connection
                ServiceBusConnection?.CloseConn();

                Logger?.WriteLog("[Disconnect] Azure Service Bus disconnected.");
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

