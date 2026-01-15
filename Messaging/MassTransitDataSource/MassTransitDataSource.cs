
using System;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Channels;
using Azure;
using MassTransit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Messaging;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Roslyn;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;



namespace TheTechIdea.Beep.MassTransitDataSourceCore
{
    [AddinAttribute(Category = DatasourceCategory.MessageQueue, DatasourceType = DataSourceType.MassTransit)]
    public class MassTransitDataSource : IDataSource, IMessageDataSource<GenericMessage, StreamConfig>
    {
        private bool disposedValue;
        #region Properties
        public string ColumnDelimiter { get; set; } = ",";
        public string ParameterDelimiter { get; set; } = ":";
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public DataSourceType DatasourceType { get; set; } = DataSourceType.MassTransit;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.MessageQueue;
        public IDataConnection Dataconnection { get; set; }
        public string DatasourceName { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public string Id { get; set; }
        public IDMLogger Logger { get; set; }
        public List<string> EntitiesNames { get; set; } = new List<string>();
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
        public IDMEEditor DMEEditor { get; set; }
        public ConnectionState ConnectionStatus { get; set; }

        public Dictionary<string, StreamConfig> StreamConfigs { get; set; } = new Dictionary<string, StreamConfig>();
        private IBusControl _busControl;
        public IBusControl BusControl
        {
            get => _busControl;
            set
            {
                if (_busControl != null)
                {
                    _busControl.Stop();
                }
                _busControl = value;
            }
        }

        public MassTransitTransportType TransportType { get; set; } = MassTransitTransportType.RabbitMQ;
        public MassTransitSerializerType SerializerType { get; set; } = MassTransitSerializerType.Json;
        public MassTransitTransportMode TransportMode { get; set; } = MassTransitTransportMode.Client;
        // Old approach
        // public Dictionary<string, List<object>> QueueData { get; set; } 
        //     = new Dictionary<string, List<object>>();

        public Dictionary<string, Channel<object>> ChannelData { get; }
            = new Dictionary<string, Channel<object>>();

       // public Dictionary<string, List<object>> QueueData { get; set; } = new Dictionary<string, List<object>>();

        public event EventHandler<PassedArgs> PassEvent;
        private IServiceProvider _services;
        public IServiceProvider Services
        {
            get => _services;
            set
            {
                _services = value;
               
            }
        }
     
        #endregion

        #region Constructor
        public MassTransitDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            this.DMEEditor = DMEEditor;
            DatasourceType = databasetype;
            ErrorObject = per;
            Dataconnection = new MassTransitDataConnection(DMEEditor)        // or your desired serializer type
            {
                Logger = logger,
                ErrorObject = per
            };
        }
        #endregion
        #region IMessageDataSource Methods

        /// <summary>
        /// Initializes a new stream config, stored under StreamConfigs[EntityName].
        /// </summary>
        public void Initialize(StreamConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (!StreamConfigs.ContainsKey(config.EntityName))
                StreamConfigs[config.EntityName] = config;

            // Create an unbounded channel if we don't want to limit capacity:
            if (!ChannelData.ContainsKey(config.EntityName))
            {
                ChannelData[config.EntityName] = Channel.CreateUnbounded<object>();
            }

            Logger?.WriteLog($"Stream '{config.EntityName}' initialized with MessageType '{config.MessageType}'.");
        }


        /// <summary>
        /// Sends a message to the specified stream following messaging standards.
        /// </summary>
        public async Task SendMessageAsync(string streamName, GenericMessage message, CancellationToken cancellationToken)
        {
            try
            {
                if (_busControl == null || ConnectionStatus != ConnectionState.Open)
                    throw new InvalidOperationException("Bus is not connected. Call OpenConnection() before sending messages.");

                if (!StreamConfigs.ContainsKey(streamName))
                    throw new KeyNotFoundException($"Stream configuration for '{streamName}' not found.");

                // Ensure message follows standards
                message = MessageStandardsHelper.EnsureMessageStandards(message, DatasourceName ?? "MassTransitDataSource");
                
                // Validate message
                var validation = MessageStandardsHelper.ValidateMessage(message);
                if (!validation.IsValid)
                {
                    var errorMsg = $"Message validation failed: {string.Join("; ", validation.Errors)}";
                    Logger?.WriteLog($"[SendMessageAsync] {errorMsg}");
                    throw new InvalidOperationException(errorMsg);
                }

                var config = StreamConfigs[streamName];
                
                // Build the endpoint URI. The format depends on your transport.
                var endpointUri = new Uri($"{Dataconnection.ConnectionProp.Host}/{config.EntityName}");
                var sendEndpoint = await _busControl.GetSendEndpoint(endpointUri);

                // Deserialize payload to the expected type if needed
                object payloadToSend = message.Payload;
                if (message.Payload is string payloadString && !string.IsNullOrEmpty(config.MessageType))
                {
                    var messageType = MessageStandardsHelper.ResolveMessageType(config.MessageType);
                    if (messageType != null && messageType != typeof(object))
                    {
                        payloadToSend = MessageStandardsHelper.DeserializePayload(payloadString, messageType);
                    }
                }

                // Send the message payload
                await sendEndpoint.Send(payloadToSend, cancellationToken);
                Logger?.WriteLog($"[SendMessageAsync] Message sent to stream '{streamName}' with MessageId: {message.MessageId}");
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[SendMessageAsync] Error sending message to stream '{streamName}': {ex.Message}");
                MessageStandardsHelper.SetErrorMessage(message, ex);
                throw;
            }
        }

        /// <summary>
        /// Subscribes to messages for a specific stream (entity) with a callback handler.
        /// </summary>
        public async Task SubscribeAsync(string streamName, Func<GenericMessage, Task> onMessageReceived, CancellationToken cancellationToken)
        {
            if (_busControl == null || ConnectionStatus != ConnectionState.Open)
                throw new InvalidOperationException("Bus is not connected. Call OpenConnection() first.");

            if (!StreamConfigs.TryGetValue(streamName, out var config))
                throw new KeyNotFoundException($"Stream config for '{streamName}' not found.");

            var messageType = MessageStandardsHelper.ResolveMessageType(config.MessageType);
            if (messageType == null)
                throw new InvalidOperationException($"Cannot load message type: {config.MessageType}");

            // Ensure channel for this stream
            if (!ChannelData.ContainsKey(streamName))
            {
                ChannelData[streamName] = Channel.CreateUnbounded<object>();
            }
            var channel = ChannelData[streamName];

            // Start background task to process messages from channel
            _ = Task.Run(async () =>
            {
                await foreach (var consumedMessage in channel.Reader.ReadAllAsync(cancellationToken))
                {
                    try
                    {
                        // Create GenericMessage from consumed message
                        var genericMessage = new GenericMessage
                        {
                            MessageId = Guid.NewGuid().ToString(),
                            EntityName = streamName,
                            Payload = consumedMessage,
                            Timestamp = DateTime.UtcNow
                        };

                        // Set metadata from config
                        genericMessage.MessageType = config.MessageType;
                        genericMessage.MessageVersion = "1.0.0"; // Default, can be overridden
                        genericMessage.Source = DatasourceName ?? "MassTransitDataSource";
                        genericMessage.ContentType = "application/json";

                        // Ensure standards compliance
                        genericMessage = MessageStandardsHelper.EnsureMessageStandards(genericMessage, DatasourceName ?? "MassTransitDataSource");

                        // Invoke callback
                        if (onMessageReceived != null)
                        {
                            await onMessageReceived(genericMessage);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger?.WriteLog($"[SubscribeAsync] Error processing message from stream '{streamName}': {ex.Message}");
                    }
                }
            }, cancellationToken);

            var handle = _busControl.ConnectReceiveEndpoint(config.EntityName, ep =>
            {
                // Example: dynamic reflection or a typed consumer
                var handlerMethod = ep.GetType()
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(m => m.Name == "Handler"
                                         && m.IsGenericMethod
                                         && m.GetParameters().Length == 1);

                if (handlerMethod == null)
                    throw new InvalidOperationException("Could not find ep.Handler<T> extension method.");

                var genericHandler = handlerMethod.MakeGenericMethod(messageType);

                // Build a delegate that writes the consumed message to the channel
                var callbackDelegate = BuildCallbackDelegate(messageType, channel.Writer);

                genericHandler.Invoke(null, new object[] { ep, callbackDelegate });
            });

            await handle.Ready;
            Logger?.WriteLog($"[SubscribeAsync] Subscribed to stream '{streamName}' for message type '{config.MessageType}'.");
        }

        private Delegate BuildCallbackDelegate(Type messageType, ChannelWriter<object> writer)
        {
            // We'll build a delegate of type: Func<ConsumeContext<T>, Task>.
            //
            // The logic is:
            // {
            //     return writer.WriteAsync((object)ctx.Message, CancellationToken.None).AsTask();
            // }

            // 1) Build the parameter: (ConsumeContext<T> ctx)
            var consumeContextType = typeof(ConsumeContext<>).MakeGenericType(messageType);
            var ctxParam = Expression.Parameter(consumeContextType, "ctx");

            // 2) ctx.Message -> the incoming typed message
            var msgProp = consumeContextType.GetProperty("Message");
            var msgAccess = Expression.Property(ctxParam, msgProp!);

            // 3) writer.WriteAsync((object)ctx.Message, CancellationToken.None)
            var writeAsyncMethod = typeof(ChannelWriter<object>)
                .GetMethod("WriteAsync", new[] { typeof(object), typeof(CancellationToken) });

            var writeAsyncCall = Expression.Call(
                Expression.Constant(writer),
                writeAsyncMethod!,
                Expression.Convert(msgAccess, typeof(object)),
                Expression.Constant(CancellationToken.None)
            );

            // 4) .AsTask() to return Task instead of ValueTask
            var asTaskMethod = typeof(ValueTask).GetMethod("AsTask", Type.EmptyTypes);
            var asTaskCall = Expression.Call(writeAsyncCall, asTaskMethod!);

            // 5) Build the final expression block that returns asTaskCall
            var block = Expression.Block(asTaskCall);

            // 6) Create a delegate: Func<ConsumeContext<T>, Task>
            var delegateType = typeof(Func<,>).MakeGenericType(consumeContextType, typeof(Task));
            return Expression.Lambda(delegateType, block, ctxParam).Compile();
        }



        /// <summary>
        /// Disconnect from the transport (stop the bus).
        /// </summary>
        public void Disconnect()
        {
            Closeconnection();
        }

        /// <summary>
        /// Acknowledges that a message has been successfully processed.
        /// Note: MassTransit handles acknowledgments automatically, but this method is provided for interface compliance.
        /// </summary>
        public async Task AcknowledgeMessageAsync(string streamName, GenericMessage message, CancellationToken cancellationToken)
        {
            // MassTransit handles acknowledgments automatically through its consumer pipeline
            // This method is provided for interface compliance
            await Task.CompletedTask;
            Logger?.WriteLog($"[AcknowledgeMessageAsync] Message acknowledged for stream '{streamName}' with MessageId: {message.MessageId}");
        }

        /// <summary>
        /// Retrieves a message without committing its acknowledgment (peek functionality).
        /// Note: MassTransit doesn't support true peek operations, but we can read from the channel if available.
        /// </summary>
        public async Task<GenericMessage> PeekMessageAsync(string streamName, CancellationToken cancellationToken)
        {
            try
            {
                if (!StreamConfigs.TryGetValue(streamName, out var config))
                    throw new KeyNotFoundException($"Stream config for '{streamName}' not found.");

                if (!ChannelData.TryGetValue(streamName, out var channel))
                    return null;

                // Try to read from channel without removing (peek)
                // Note: Channel doesn't support true peek, so we read and write back
                if (await channel.Reader.WaitToReadAsync(cancellationToken))
                {
                    if (channel.Reader.TryRead(out var consumedMessage))
                    {
                        // Create GenericMessage
                        var genericMessage = new GenericMessage
                        {
                            MessageId = Guid.NewGuid().ToString(),
                            EntityName = streamName,
                            Payload = consumedMessage,
                            Timestamp = DateTime.UtcNow
                        };

                        // Set metadata from config
                        genericMessage.MessageType = config.MessageType;
                        genericMessage.MessageVersion = "1.0.0";
                        genericMessage.Source = DatasourceName ?? "MassTransitDataSource";
                        genericMessage.ContentType = "application/json";

                        // Ensure standards compliance
                        genericMessage = MessageStandardsHelper.EnsureMessageStandards(genericMessage, DatasourceName ?? "MassTransitDataSource");

                        // Write back to channel (since we can't truly peek)
                        await channel.Writer.WriteAsync(consumedMessage, cancellationToken);

                        return genericMessage;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[PeekMessageAsync] Error peeking message from stream '{streamName}': {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves metadata about a stream (e.g., queue depth, message count).
        /// </summary>
        public async Task<object> GetStreamMetadataAsync(string streamName, CancellationToken cancellationToken)
        {
            try
            {
                if (!StreamConfigs.TryGetValue(streamName, out var config))
                    throw new KeyNotFoundException($"Stream config for '{streamName}' not found.");

                var metadata = new
                {
                    StreamName = streamName,
                    EntityName = config.EntityName,
                    MessageType = config.MessageType,
                    MessageCategory = config.MessageCategory,
                    ConsumerType = config.ConsumerType,
                    ChannelCapacity = ChannelData.TryGetValue(streamName, out var channel) 
                        ? (channel.Reader.CanCount ? channel.Reader.Count : -1) 
                        : 0,
                    IsInitialized = StreamConfigs.ContainsKey(streamName),
                    TransportType = TransportType.ToString(),
                    SerializerType = SerializerType.ToString()
                };

                return metadata;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[GetStreamMetadataAsync] Error getting metadata for stream '{streamName}': {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Helper: Build the ep.Handler<T> callback

       

        #endregion


        #region Connection Methods
        public ConnectionState Openconnection()
        {
            try
            {
                if (BusControl != null)
                {
                    // Bus is already set and presumably started
                    Logger?.WriteLog("Bus is already connected.");
                    return ConnectionState.Open;
                }

                if (_services == null)
                    throw new InvalidOperationException("A built service provider was not set in the data source.");

                // Retrieve the bus from the ALREADY built provider
                _busControl = _services.GetRequiredService<IBusControl>();

                // Start the bus
                _busControl.Start();

                ConnectionStatus = ConnectionState.Open;
                Logger?.WriteLog("Bus connection opened successfully.");
                return ConnectionStatus;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error opening connection: {ex.Message}");
                ConnectionStatus = ConnectionState.Broken;
                return ConnectionStatus;
            }
        }
        public ConnectionState Closeconnection()
        {
            try
            {
                if (BusControl != null)
                {
                    BusControl.Stop();
                    BusControl = null;
                    Logger?.WriteLog("Bus connection closed successfully.");
                }

                ConnectionStatus = ConnectionState.Closed;
                return ConnectionStatus;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error closing connection: {ex.Message}");
                return ConnectionStatus;
            }
        }
        #endregion

        #region Entity and Queue Management
        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            throw new NotImplementedException();
        }

        public object GetEntity(string entityName, List<AppFilter> filters, int pageNumber, int pageSize)
        {
           
            // 4) Apply paging (assuming pageNumber is 1-based).
            int skipCount = (pageNumber - 1) * pageSize;
            var drainedMessages=GetEntity(entityName, filters) as List<object>;
            var pagedMessages = drainedMessages
                .Skip(skipCount)
                .Take(pageSize)
                .ToList();

            // 5) Return results in an ObservableBindingList for data binding (if desired).
            return  pagedMessages;
        }
        public Task<object> GetEntityAsync(string entityName, List<AppFilter> filters)
        {
            // Reuse the synchronous method and wrap the result in a completed Task.
            object result = GetEntity(entityName, filters);
            return Task.FromResult(result);
        }

        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            if (refresh || !Entities.Any(e => e.EntityName == EntityName))
            {
                var streamConfig = StreamConfigs.GetValueOrDefault(EntityName);
                if (streamConfig != null)
                {
                    var entity = new EntityStructure
                    {
                        EntityName = EntityName,
                        Fields = new List<EntityField>
                        {
                            new EntityField { FieldName = "MessageId", Fieldtype = "System.Guid" },
                            new EntityField { FieldName = "Data", Fieldtype = "System.String" }
                        }
                    };
                    Entities.Add(entity);
                }
            }

            return Entities.FirstOrDefault(e => e.EntityName == EntityName);
        }
        private List<object> ApplyFilters(List<object> messages, List<AppFilter> filters)
        {
            if (filters == null || filters.Count == 0)
                return messages;

            return messages.Where(m =>
            {
                foreach (var filter in filters)
                {
                    var prop = m.GetType().GetProperty(filter.FieldName);
                    if (prop == null) return false;

                    var msgValue = prop.GetValue(m)?.ToString();
                    var filterValue = filter.FilterValue1?.ToString();
                    if (!string.Equals(msgValue, filterValue, StringComparison.OrdinalIgnoreCase))
                        return false;
                }
                return true;
            }).ToList();
        }
        public object GetEntity(string entityName, List<AppFilter> filters)
        {
            
            // 1) Ensure there's a channel for this entity
            if (!ChannelData.ContainsKey(entityName))
            {
                ChannelData[entityName] = Channel.CreateUnbounded<object>();
            }

            var channel = ChannelData[entityName];

            // 2) Drain all currently available messages (non-blocking) into a list
            var drainedMessages = new List<object>();
            while (channel.Reader.TryRead(out var msg))
            {
                drainedMessages.Add(msg);
            }
            // Get StreamConfig for the entity
         
            // 3) Filter the messages using reflection-based logic (if filters were provided)
            if (filters != null && filters.Any())
            {
                drainedMessages = drainedMessages.Where(m =>
                {
                    // The message passes only if it meets *all* filters
                    return filters.All(filter =>
                    {
                        // Retrieve the property from GenericMessage (e.g. "EntityName", "MessageId", "Payload", etc.)
                        var propertyInfo = m.GetType().GetProperty(filter.FieldName);
                        if (propertyInfo == null)
                        {
                            // If this property doesn't exist on GenericMessage, exclude it
                            return false;
                        }

                        // Compare the string value to filterValue (case-insensitive)
                        var messageValue = propertyInfo.GetValue(m)?.ToString();
                        var filterValue = filter.FilterValue1?.ToString();

                        return !string.IsNullOrEmpty(messageValue)
                            && messageValue.Equals(filterValue, StringComparison.OrdinalIgnoreCase);
                    });
                }).ToList();
            }
            if (!StreamConfigs.TryGetValue(entityName, out var config))
            {
                Logger?.WriteLog($"Stream configuration for entity '{entityName}' not found.");
                return null;
            }
            // Get the message type
            if (config.MessageType == null)
            {
                // Attemp to GenerateType from any object in Payload in GenericMessage
                var firstMessage = drainedMessages.FirstOrDefault();
                if (firstMessage != null && firstMessage != null)
                {
                    var payloadType = firstMessage.GetType();
                    if (payloadType == null)
                    {
                        payloadType= DMTypeBuilder.CreateDynamicTypeFromObject(firstMessage);
                    }
                    config.MessageType = payloadType.AssemblyQualifiedName;
                }

            }
            var messageType = Type.GetType(config.MessageType);
            if (messageType == null)
            {
                Logger?.WriteLog($"Message type '{config.MessageType}' not found.");
                return null;
            }
            // Check this type is exist in entities and entitiesnames
            if (!EntitiesNames.Contains(entityName))
            {
                EntitiesNames.Add(entityName);
            }
            if (!Entities.Any(e => e.EntityName == entityName))
            {
                var entity = new EntityStructure();
                entity.EntityName = entityName;
                entity.Fields = new List<EntityField>();

                // Add fields based on the message type
                foreach (var prop in messageType.GetProperties())
                {
                    var field = new EntityField
                    {
                        FieldName = prop.Name,
                        Fieldtype = prop.PropertyType.FullName
                    };
                    entity.Fields.Add(field);
                }

                Entities.Add(entity);
            }

            // 5) Return as an ObservableBindingList<GenericMessage> (helpful for data binding)
            return drainedMessages;
        }
        private IEnumerable<object> FilterMessages(IEnumerable<object> messages, List<AppFilter> filters)
        {
            if (filters == null || !filters.Any())
                return messages; // no filtering

            return messages.Where(m =>
            {
                foreach (var filter in filters)
                {
                    var property = m.GetType().GetProperty(filter.FieldName);
                    if (property == null) return false;

                    var messageValue = property.GetValue(m)?.ToString();
                    var filterValue = filter.FilterValue1?.ToString();

                    if (string.IsNullOrEmpty(messageValue) ||
                        !messageValue.Equals(filterValue, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
                return true;
            });
        }
        private Delegate BuildCallbackDelegate(Type messageType, List<object> localList)
        {
            var ctxType = typeof(ConsumeContext<>).MakeGenericType(messageType);
            var ctxParam = Expression.Parameter(ctxType, "ctx");

            // ctx.Message
            var msgProp = ctxType.GetProperty("Message");
            var msgAccess = Expression.Property(ctxParam, msgProp!);

            // localList.Add( (object)ctx.Message );
            var addMethod = typeof(List<object>).GetMethod("Add", new[] { typeof(object) });
            var addCall = Expression.Call(
                Expression.Constant(localList),
                addMethod!,
                Expression.Convert(msgAccess, typeof(object))
            );

            // return Task.CompletedTask
            var completedTaskProp = typeof(Task).GetProperty(nameof(Task.CompletedTask));
            var completedTaskExpr = Expression.Property(null, completedTaskProp!);

            // block: { localList.Add(...); return Task.CompletedTask; }
            var block = Expression.Block(addCall, completedTaskExpr);

            // Build a Func<ConsumeContext<T>, Task>
            var delegateType = typeof(Func<,>).MakeGenericType(ctxType, typeof(Task));
            return Expression.Lambda(delegateType, block, ctxParam).Compile();
        }
        public bool CreateEntityAs(EntityStructure entity)
        {
            throw new NotImplementedException();
        }

        public Type GetEntityType(string EntityName)
        {
            throw new NotImplementedException();
        }

        public bool CheckEntityExist(string EntityName)
        {
            throw new NotImplementedException();
        }

        public int GetEntityIdx(string entityName)
        {
            throw new NotImplementedException();
        }
        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            ErrorObject = new ErrorsInfo { Flag = Errors.Ok };

            try
            {
                if (BusControl == null)
                {
                    throw new InvalidOperationException("Bus is not connected. Call OpenConnection() before producing messages.");
                }

                if (!StreamConfigs.TryGetValue(EntityName, out StreamConfig config))
                {
                    throw new KeyNotFoundException($"Stream configuration for entity '{EntityName}' not found.");
                }

                // Create a new GenericMessage using the Payload property instead of Data.
                var message = new GenericMessage
                {
                    EntityName = EntityName,
                    Payload = InsertedData as Dictionary<string, object>
                              ?? InsertedData.GetType().GetProperties()
                                  .ToDictionary(prop => prop.Name, prop => prop.GetValue(InsertedData))
                };

                var sendEndpoint = BusControl
                    .GetSendEndpoint(new Uri($"{Dataconnection.ConnectionProp.Host}/{config.EntityName}"))
                    .Result;
                sendEndpoint.Send(message).Wait();

                Logger?.WriteLog($"Message successfully sent to queue '{EntityName}'.");
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error producing message to queue '{EntityName}': {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
            }

            return ErrorObject;
        }


        public bool TryGetStreamConfig(string name, out StreamConfig config)
        {
            return StreamConfigs.TryGetValue(name, out config);
        }

        public List<string> GetEntitesList()
        {
            throw new NotImplementedException();
        }
        #region Not Implemented Methods
        public object RunQuery(string qrystr)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo ExecuteSql(string sql)
        {
            throw new NotImplementedException();
        }

      

        public List<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            throw new NotImplementedException();
        }

        public List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            throw new NotImplementedException();
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            throw new NotImplementedException();
        }

        public List<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            throw new NotImplementedException();
        }

      

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo BeginTransaction(PassedArgs args)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo EndTransaction(PassedArgs args)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo Commit(PassedArgs args)
        {
            throw new NotImplementedException();
        }
      

        public Task<double> GetScalarAsync(string query)
        {
            throw new NotImplementedException();
        }

        public double GetScalar(string query)
        {
            throw new NotImplementedException();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~MassTransitDataSource()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion

    

        #endregion
    }
}
