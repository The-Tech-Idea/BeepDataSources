﻿using Microsoft.CodeAnalysis;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Messaging;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;

namespace RabbitMQDataSourceCore
{
    [AddinAttribute(Category = DatasourceCategory.MessageQueue, DatasourceType = DataSourceType.RabbitMQ)]

    public class RabbitMQDataSource : IDataSource,IDisposable, IMessageDataSource<GenericMessage, StreamConfig>
    {
        #region "IDataSource Properties"
        // Private fields
        private IConnection _connection;
      
        private bool disposedValue;
        private bool _isInitialized;

        public string ColumnDelimiter { get  ; set  ; }
        public string ParameterDelimiter { get  ; set  ; }
        public string GuidID { get  ; set  ; }
        public DataSourceType DatasourceType { get; set; } = DataSourceType.RabbitMQ;
        public DatasourceCategory Category { get  ; set  ; }= DatasourceCategory.QUEUE;
        public IDataConnection Dataconnection { get  ; set  ; }  // use for RabbitMQ connection properties
        public string DatasourceName { get  ; set  ; }
        public IErrorsInfo ErrorObject { get  ; set  ; }
        public string Id { get  ; set  ; }
        public IDMLogger Logger { get  ; set  ; }
        public List<string> EntitiesNames { get  ; set  ; } // Store all Queues name just like a table name in rdbms
        public List<EntityStructure> Entities { get  ; set  ; } // Store Data structure of the Queue just like a table columns in rdbms
        public IDMEEditor DMEEditor { get  ; set  ; }
        public ConnectionState ConnectionStatus { get  ; set  ; }

        public event EventHandler<PassedArgs> PassEvent;
        #endregion "IDataSource Properties"
        #region "Constructors"
        public RabbitMQDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per)
        {
            ColumnDelimiter = "''";
            ParameterDelimiter = ":";
            Entities = new List<EntityStructure>();
            EntitiesNames = new List<string>();
            ErrorObject = new ErrorsInfo();
            DatasourceName = datasourcename;
            Logger = logger;
            this.DMEEditor = DMEEditor;
            DatasourceType = databasetype;
            ErrorObject = per;
            Dataconnection = new RabbitMQDataConnection(DMEEditor)
            {
                Logger = logger,
                ErrorObject = per
            };
        }
        #endregion "Constructors"
        #region "IDataSource Methods"
        #region "Pull from Queue"

        // Pull data from Queue using EntityName and filter, where EntityName is the Queue name
        public object GetEntity(string EntityName, List<AppFilter> filter)
        {
            try
            {
                // Use the PeekMessageAsync method to fetch a message without acknowledging
                var peekTask = PeekMessageAsync(EntityName, CancellationToken.None);
                peekTask.Wait(); // Blocking call since this is a synchronous method

                var message = peekTask.Result;
                if (message == null)
                {
                    Logger?.WriteLog($"[GetEntity] No message found in queue '{EntityName}'.");
                }
                return message;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[GetEntity] Error: {ex.Message}");
                throw;
            }
        }

        // Asynchronous method to pull data from Queue using EntityName and filter
        public async Task<object> GetEntityAsync(string EntityName, List<AppFilter> filter)
        {
            try
            {
                // Use PeekMessageAsync to fetch a message without acknowledging
                var message = await PeekMessageAsync(EntityName, CancellationToken.None);
                if (message == null)
                {
                    Logger?.WriteLog($"[GetEntityAsync] No message found in queue '{EntityName}'.");
                }
                return message;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[GetEntityAsync] Error: {ex.Message}");
                throw;
            }
        }

        // Pull data from Queue with pagination support
        public object GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            try
            {
                if (!Channels.TryGetValue(EntityName, out var channel))
                {
                    throw new Exception($"Channel not found for queue '{EntityName}'.");
                }

                // Collect messages up to the page size
                var messages = new List<GenericMessage>();
                for (int i = 0; i < pageSize; i++)
                {
                    // Use PeekMessageAsync to get the message
                    var peekTask = PeekMessageAsync(EntityName, CancellationToken.None);
                    peekTask.Wait(); // Blocking call to fetch each message

                    var message = peekTask.Result as GenericMessage;
                    if (message != null)
                    {
                        messages.Add(message);
                    }
                    else
                    {
                        // Break if no more messages
                        break;
                    }
                }

                // Return the requested page
                var pagedMessages = messages
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return pagedMessages;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[GetEntity (with pagination)] Error: {ex.Message}");
                throw;
            }
        }

        #endregion "Pull from Queue"
        #region "Push to Queue"
        public async Task<IErrorsInfo> InsertEntityAsync(string EntityName, object InsertedData)
        {
            var errors = new ErrorsInfo();
            try
            {
                // Serialize the InsertedData into JSON format
                var messagePayload = InsertedData is string
                    ? InsertedData.ToString()
                    : JsonSerializer.Serialize(InsertedData);

                // Create a GenericMessage object to represent the payload
                var message = new GenericMessage
                {
                    EntityName = EntityName,
                    Payload = messagePayload,
                    Timestamp = DateTime.UtcNow
                };

                // Call SendMessageAsync
                await SendMessageAsync(EntityName, message, CancellationToken.None);

                Logger?.WriteLog($"[InsertEntityAsync] Successfully inserted data into queue '{EntityName}'.");
            }
            catch (Exception ex)
            {
                errors.Flag = Errors.Failed;
                errors.Message = ex.Message;
                Logger?.WriteLog($"[InsertEntityAsync] Error while inserting data into queue '{EntityName}': {ex.Message}");
            }

            return errors;
        }

        /// <summary>
        /// Push data to the Queue using EntityName and InsertedData.
        /// </summary>
        /// <param name="EntityName">The name of the queue.</param>
        /// <param name="InsertedData">The data to insert (message payload).</param>
        /// <returns>An IErrorsInfo object indicating the result of the operation.</returns>
        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            return InsertEntityAsync(EntityName, InsertedData).GetAwaiter().GetResult();
        }

        #endregion "Push to Queue"
        #region "open/close connection"
        public ConnectionState Openconnection()
        {
            ConnectionState retval = ConnectionState.Closed;
            try
            {
                Dataconnection.OpenConnection();
                retval = ConnectionState.Open;
            }
            catch (Exception ex)
            {
               DMEEditor.AddLogMessage("Beep", $"Error in Opening Connection {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
                retval = ConnectionState.Broken;
            }
            return retval;
        }
        public ConnectionState Closeconnection()
        {
            throw new NotImplementedException();
        }
        #endregion "open/close connection"
        #region "Add/Remove Queue"
        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            throw new NotImplementedException();
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            throw new NotImplementedException();
        }
        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            throw new NotImplementedException();
        }
        #endregion "Add/Remove Queue"
        #region "Updated Functions"
        // Begin a transaction (RabbitMQ does not natively support transactions across queues, so this could be a placeholder or custom logic)
        public IErrorsInfo BeginTransaction(PassedArgs args)
        {
            var errors = new ErrorsInfo
            {
                Flag = Errors.Warning,
                Message = "Transactions are not supported by RabbitMQ."
            };
            Logger?.WriteLog("[BeginTransaction] Transactions are not supported by RabbitMQ.");
            return errors;
        }
        // Check if an entity (queue) exists
        public bool CheckEntityExist(string EntityName)
        {
            try
            {
                if (!Channels.ContainsKey(EntityName))
                {
                    // Check by declaring the queue passively
                    if (Dataconnection is RabbitMQDataConnection rabbitConn)
                    {
                        var channel = rabbitConn.Connection.CreateChannelAsync().Result;
                        channel.QueueDeclarePassiveAsync(queue: EntityName);
                        channel.CloseAsync();
                        return true;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[CheckEntityExist] Queue '{EntityName}' does not exist. Error: {ex.Message}");
                return false;
            }
        }
        // Commit a transaction (RabbitMQ does not natively support transactions across queues)
        public IErrorsInfo Commit(PassedArgs args)
        {
            return new ErrorsInfo
            {
                Flag = Errors.Warning,
                Message = "Transactions are not supported by RabbitMQ."
            };
        }
        // End a transaction (RabbitMQ does not natively support transactions across queues)
        public IErrorsInfo EndTransaction(PassedArgs args)
        {
            return new ErrorsInfo
            {
                Flag = Errors.Warning,
                Message = "Transactions are not supported by RabbitMQ."
            };
        }
        // Execute a raw SQL query (not applicable to RabbitMQ; placeholder for compatibility with other data sources)
        public IErrorsInfo ExecuteSql(string sql)
        {
            return new ErrorsInfo
            {
                Flag = Errors.Warning,
                Message = "SQL execution is not supported by RabbitMQ."
            };
        }
        // Retrieve a list of child tables (not applicable to RabbitMQ; placeholder)
        public List<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            Logger?.WriteLog("[GetChildTablesList] Child relations are not supported by RabbitMQ.");
            return new List<ChildRelation>();
        }
        // Generate create entity script (could return a sample RabbitMQ queue declaration command)
        public List<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            var scripts = new List<ETLScriptDet>();
            if (entities != null)
            {
                //foreach (var entity in entities)
                //{
                //    var script = new ETLScriptDet
                //    {
                //          = $"QueueDeclare({entity.EntityName}, durable: false, exclusive: false, autoDelete: false, arguments: null);"
                //    };
                //    scripts.Add(script);
                //}
            }
            return scripts;
        }
        // Retrieve a list of all entities (queues)
        public List<string> GetEntitesList()
        {
            return EntitiesNames ?? new List<string>();
        }
        // Retrieve foreign keys for an entity (not applicable to RabbitMQ; placeholder)
        public List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            Logger?.WriteLog("[GetEntityforeignkeys] Foreign keys are not supported by RabbitMQ.");
            return new List<RelationShipKeys>();
        }
        // Get the index of an entity in the list
        public int GetEntityIdx(string entityName)
        {
            return EntitiesNames?.IndexOf(entityName) ?? -1;
        }
        // Retrieve the structure of an entity (queue metadata)
        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            try
            {
                if (Channels.TryGetValue(EntityName, out var channel))
                {
                    var result = channel.QueueDeclarePassiveAsync(queue: EntityName).Result;
                    return new EntityStructure
                    {
                        EntityName = EntityName,
                         EndRow = (int)result.MessageCount
                    };
                }
                throw new Exception($"Channel for queue '{EntityName}' not found.");
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[GetEntityStructure] Error: {ex.Message}");
                return null;
            }
        }
        // Retrieve the structure of an entity based on another structure
        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            return GetEntityStructure(fnd.EntityName, refresh);
        }
        // Retrieve the type of an entity
        public Type GetEntityType(string EntityName)
        {
            return typeof(GenericMessage); // All queues handle GenericMessage type
        }
        // Retrieve a scalar value (not applicable to RabbitMQ; placeholder)
        public double GetScalar(string query)
        {
            Logger?.WriteLog("[GetScalar] Scalar queries are not supported by RabbitMQ.");
            return 0.0;
        }
        // Asynchronously retrieve a scalar value (not applicable to RabbitMQ; placeholder)
        public Task<double> GetScalarAsync(string query)
        {
            Logger?.WriteLog("[GetScalarAsync] Scalar queries are not supported by RabbitMQ.");
            return Task.FromResult(0.0);
        }
        // Run a query (not applicable to RabbitMQ; placeholder)
        public object RunQuery(string qrystr)
        {
            Logger?.WriteLog("[RunQuery] Queries are not supported by RabbitMQ.");
            return null;
        }
        // Run a script (e.g., script for creating queues or exchanges)
        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            var errors = new ErrorsInfo();
            try
            {
                // Execute the script (assumes the script is in RabbitMQ C# API format)
            //    Logger?.WriteLog($"[RunScript] Executing script: {dDLScripts.Script}");
                return errors;
            }
            catch (Exception ex)
            {
                errors.Flag = Errors.Failed;
                errors.Message = ex.Message;
                Logger?.WriteLog($"[RunScript] Error: {ex.Message}");
                return errors;
            }
        }
        // Update multiple entities (queues)
        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            return InsertEntity(EntityName, UploadData);
        }
        // Update a single entity (queue)
        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            return InsertEntity(EntityName, UploadDataRow);
        }
        #endregion

        #endregion "IDataSource Methods"
        #region "Dispose"
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
        // ~RabbitMQDataSource()
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
        #endregion "Dispose"
        #region "RabbitMQDataSource Methods"

        // Stores channels keyed by the queue name (or entity name).
        // You can store more than just queue-based channels if desired.
        public Dictionary<string, IChannel> Channels { get; set; } = new Dictionary<string, IChannel>();

        // -------------------------------------------------
        // Basic RabbitMQ queue operations
        // -------------------------------------------------

        /// <summary>
        /// Creates an exchange (placeholder).
        /// </summary>
        public IErrorsInfo CreateExchange(string exchangeName)
        {
            // For a one-off exchange creation, you can open/close a channel on the fly,
            // or store it in a dictionary if you want to reuse it.
            throw new NotImplementedException();
        }

        public IErrorsInfo DeleteExchange(string exchangeName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Specialized method outside of Openconnection()—just calls Openconnection().
        /// </summary>
        public IErrorsInfo ConnectToRabbitMQ()
        {
            Openconnection();
            return ErrorObject;
        }

        /// <summary>
        /// Disconnect by calling Closeconnection().
        /// </summary>
        public IErrorsInfo DisconnectFromRabbitMQ()
        {
            Closeconnection();
            return ErrorObject;
        }

        /// <summary>
        /// Creates (declares) a queue asynchronously and
        /// stores the IChannel in the `Channels` dictionary so we can reuse it later.
        /// </summary>
        public async Task<IErrorsInfo> CreateQueueAsync(string queueName)
        {
            var errors = new ErrorsInfo();
            try
            {
                if (Dataconnection is RabbitMQDataConnection rabbitConn)
                {
                    // Ensure we have an open connection
                    if (rabbitConn.Connection == null || !rabbitConn.Connection.IsOpen)
                        rabbitConn.OpenConnection();

                    // Create (or reuse) the channel for this queue
                    if (!Channels.TryGetValue(queueName, out var channel))
                    {
                        channel = await rabbitConn.Connection.CreateChannelAsync();
                        Channels[queueName] = channel;
                    }

                    // Declare the queue asynchronously (idempotent)
                    await channel.QueueDeclareAsync(
                        queue: queueName,
                        durable: false,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null
                    );
                }
                else
                {
                    errors.Flag = Errors.Failed;
                    errors.Message = "Dataconnection is not a RabbitMQDataConnection.";
                }
            }
            catch (Exception ex)
            {
                errors.Flag = Errors.Failed;
                errors.Message = ex.Message;
                Logger?.WriteLog($"[CreateQueueAsync] Error: {ex.Message}");
            }
            return errors;
        }

        /// <summary>
        /// Example of a ephemeral approach to deleting a queue: 
        /// open a fresh channel, perform the delete, then close it.
        /// If you prefer, you can also check `Channels` and reuse an existing channel.
        /// </summary>
        public async Task<IErrorsInfo> DeleteQueueAsync(string queueName)
        {
            var errors = new ErrorsInfo();
            try
            {
                if (Dataconnection is RabbitMQDataConnection rabbitConn)
                {
                    if (rabbitConn.Connection == null || !rabbitConn.Connection.IsOpen)
                        rabbitConn.OpenConnection();

                    // For a single operation, we create/dispose of a channel
                    // If you want to reuse a channel, use Channels[queueName].
                    using var channel = await rabbitConn.Connection.CreateChannelAsync();
                    await channel.QueueDeleteAsync(queue: queueName);
                }
                else
                {
                    errors.Flag = Errors.Failed;
                    errors.Message = "Dataconnection is not a RabbitMQDataConnection.";
                }
            }
            catch (Exception ex)
            {
                errors.Flag = Errors.Failed;
                errors.Message = ex.Message;
                Logger?.WriteLog($"[DeleteQueueAsync] Error: {ex.Message}");
            }
            return errors;
        }

        /// <summary>
        /// Same idea: ephemeral approach to purge a queue.
        /// You could reuse a stored channel if desired.
        /// </summary>
        public async Task<IErrorsInfo> PurgeQueueAsync(string queueName)
        {
            var errors = new ErrorsInfo();
            try
            {
                if (Dataconnection is RabbitMQDataConnection rabbitConn)
                {
                    if (rabbitConn.Connection == null || !rabbitConn.Connection.IsOpen)
                        rabbitConn.OpenConnection();

                    using var channel = await rabbitConn.Connection.CreateChannelAsync();
                    await channel.QueuePurgeAsync(queue: queueName);
                }
                else
                {
                    errors.Flag = Errors.Failed;
                    errors.Message = "Dataconnection is not a RabbitMQDataConnection.";
                }
            }
            catch (Exception ex)
            {
                errors.Flag = Errors.Failed;
                errors.Message = ex.Message;
                Logger?.WriteLog($"[PurgeQueueAsync] Error: {ex.Message}");
            }
            return errors;
        }

        // -------------------------------------------------
        // Publish / Send
        // -------------------------------------------------

        /// <summary>
        /// Publish a message asynchronously.
        /// Reuses (or creates) a channel stored in `Channels` for the given queue.
        /// </summary>
        public async Task<IErrorsInfo> SendMessageAsync(string queueName, string message)
        {
            var errors = new ErrorsInfo();
            try
            {
                if (Dataconnection is RabbitMQDataConnection rabbitConn)
                {
                    if (rabbitConn.Connection == null || !rabbitConn.Connection.IsOpen)
                        rabbitConn.OpenConnection();

                    // Reuse or create a channel for this queue
                    if (!Channels.TryGetValue(queueName, out var channel))
                    {
                        channel = await rabbitConn.Connection.CreateChannelAsync();
                        Channels[queueName] = channel;

                        // Optionally declare the queue if not declared yet
                        await channel.QueueDeclareAsync(
                            queue: queueName,
                            durable: false,
                            exclusive: false,
                            autoDelete: false,
                            arguments: null
                        );
                    }

                    // Create BasicProperties instance (implementing IReadOnlyBasicProperties and IAmqpHeader)
                    var basicProperties = new BasicProperties
                    {
                        ContentType = "text/plain",
                        DeliveryMode = DeliveryModes.Persistent // Persistent delivery mode
                    };

                    // Convert the message to ReadOnlyMemory<byte>
                    var body = Encoding.UTF8.GetBytes(message ?? string.Empty).AsMemory();

                    // Publish the message asynchronously
                    await channel.BasicPublishAsync<BasicProperties>(
                        exchange: "", // Default exchange
                        routingKey: queueName,
                        mandatory: false, // Not mandatory
                        basicProperties: basicProperties,
                        body: body
                    );

                    Logger?.WriteLog($"[SendMessageAsync] Sent to queue '{queueName}': {message}");
                }
                else
                {
                    errors.Flag = Errors.Failed;
                    errors.Message = "Dataconnection is not a RabbitMQDataConnection.";
                }
            }
            catch (Exception ex)
            {
                errors.Flag = Errors.Failed;
                errors.Message = ex.Message;
                Logger?.WriteLog($"[SendMessageAsync] Error: {ex.Message}");
            }
            return errors;
        }

        // (Optional) Overloads that handle delay, priority, etc., can do the same approach:
        public Task<IErrorsInfo> SendMessageAsync(string queueName, string message, int delay)
            => SendMessageAsync(queueName, message);

        public Task<IErrorsInfo> SendMessageAsync(string queueName, string message, int delay, int priority)
            => SendMessageAsync(queueName, message);

        public Task<IErrorsInfo> SendMessageAsync(string queueName, string message, int delay, int priority, string messageID)
            => SendMessageAsync(queueName, message);

        public Task<IErrorsInfo> SendMessageAsync(string queueName, string message, int delay, int priority, string messageID, string groupID)
            => SendMessageAsync(queueName, message);

        public Task<IErrorsInfo> SendMessageAsync(string queueName, string message, int delay, int priority, string messageID, string groupID, string deduplicationID)
            => SendMessageAsync(queueName, message);

        public Task<IErrorsInfo> SendMessageAsync(string queueName, string message, int delay, int priority, string messageID, string groupID, string deduplicationID, string tag)
            => SendMessageAsync(queueName, message);

        // -------------------------------------------------
        // Consume / Receive
        // -------------------------------------------------

        /// <summary>
        /// Demonstrates a persistent consumer approach. 
        /// Reuses (or creates) a channel in the dictionary, sets up an EventingBasicConsumer, 
        /// and keeps it open so messages can arrive asynchronously.
        /// </summary>
        public async Task<IErrorsInfo> ReceiveMessageAsync(string queueName)
        {
            var errors = new ErrorsInfo();
            try
            {
                if (Dataconnection is RabbitMQDataConnection rabbitConn)
                {
                    if (rabbitConn.Connection == null || !rabbitConn.Connection.IsOpen)
                        rabbitConn.OpenConnection();

                    // Reuse or create a channel
                    if (!Channels.TryGetValue(queueName, out var channel))
                    {
                        channel = await rabbitConn.Connection.CreateChannelAsync();
                        Channels[queueName] = channel;

                        await channel.QueueDeclareAsync(
                            queue: queueName,
                            durable: false,
                            exclusive: false,
                            autoDelete: false,
                            arguments: null
                        );
                    }

                    // Create an async consumer
                    var consumer = new AsyncEventingBasicConsumer(channel);

                    consumer.ReceivedAsync += async (model, ea) =>
                    {
                        var body = ea.Body.ToArray();
                        var msg = Encoding.UTF8.GetString(body);
                        Logger?.WriteLog($"[ReceiveMessageAsync] From '{queueName}': {msg}");

                        // Manually acknowledge
                        channel.BasicAckAsync(ea.DeliveryTag, multiple: false);

                        // If you want to do further async processing, you can do it here.
                        await Task.Yield();
                    };

                    // Start consuming, autoAck = false for manual ack
                    _ = channel.BasicConsumeAsync(
                        queue: queueName,
                        autoAck: false,
                        consumer: consumer
                    );

                    // This consumer remains active in the background until you close the channel or connection.
                }
                else
                {
                    errors.Flag = Errors.Failed;
                    errors.Message = "Dataconnection is not a RabbitMQDataConnection.";
                }
            }
            catch (Exception ex)
            {
                errors.Flag = Errors.Failed;
                errors.Message = ex.Message;
                Logger?.WriteLog($"[ReceiveMessageAsync] Error: {ex.Message}");
            }
            return errors;
        }

        // Overloads for filters, maxMessages, waitTime, etc. are placeholders.
        // They can likewise create or reuse a channel, and do partial-consuming or BasicGet, etc.
        public Task<IErrorsInfo> ReceiveMessageAsync(string queueName, string filter)
            => ReceiveMessageAsync(queueName);

        public Task<IErrorsInfo> ReceiveMessageAsync(string queueName, string filter, int maxMessages)
            => ReceiveMessageAsync(queueName);

        public Task<IErrorsInfo> ReceiveMessageAsync(string queueName, string filter, int maxMessages, int waitTime)
            => ReceiveMessageAsync(queueName);

        #endregion // RabbitMQDataSource Methods
        #region "IMessageDataSource Methods"

        // Initialize the RabbitMQ connection and configure the stream (queue)
        public void Initialize(StreamConfig config)
        {
            try
            {
                var createQueueTask = CreateQueueAsync(config.EntityName);
                createQueueTask.Wait(); // Ensure queue creation is completed
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[Initialize] Error while initializing stream '{config.EntityName}': {ex.Message}");
                throw;
            }
        }

        // Send a message to the specified stream
        public async Task SendMessageAsync(string streamName, GenericMessage message, CancellationToken cancellationToken)
        {
            try
            {
                if (Dataconnection is RabbitMQDataConnection rabbitConn)
                {
                    // Ensure queue existence
                    var result = await CreateQueueAsync(streamName);
                    if (result.Flag == Errors.Failed)
                        throw new Exception(result.Message);

                    // Prepare the message properties (use Amqp091.BasicProperties instead of IBasicProperties)
                    var properties = new BasicProperties
                    {
                        ContentType = "application/json",
                        DeliveryMode =(message.Priority.HasValue ? DeliveryModes.Persistent : DeliveryModes.Transient),
                        MessageId = message.MessageId,
                        Timestamp = new AmqpTimestamp(new DateTimeOffset(message.Timestamp).ToUnixTimeSeconds()),
                        Headers = ToRabbitMqHeaders(message)
                    };

                    var body = Encoding.UTF8.GetBytes(message.SerializePayload());

                    if (Channels.TryGetValue(streamName, out var channel))
                    {
                        // Publish the message using the correct type for properties
                        await channel.BasicPublishAsync(
                            exchange: "",
                            routingKey: streamName,
                            mandatory: false,
                            basicProperties: properties,
                            body: body.AsMemory(),
                            cancellationToken: cancellationToken
                        );

                        Logger?.WriteLog($"[SendMessageAsync] Sent to stream '{streamName}': {message.SerializePayload()}");
                    }
                    else
                    {
                        throw new Exception($"Channel not found for stream '{streamName}'.");
                    }
                }
                else
                {
                    throw new Exception("Dataconnection is not a RabbitMQDataConnection.");
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[SendMessageAsync] Error while sending message to stream '{streamName}': {ex.Message}");
                throw;
            }
        }

        // Subscribe to a stream and execute a callback when messages are received
        public async Task SubscribeAsync(string streamName, Func<GenericMessage, Task> onMessageReceived, CancellationToken cancellationToken)
        {
            try
            {
                var createQueueResult = await CreateQueueAsync(streamName);
                if (createQueueResult.Flag == Errors.Failed)
                {
                    throw new Exception(createQueueResult.Message);
                }

                if (Channels.TryGetValue(streamName, out var channel))
                {
                    var consumer = new AsyncEventingBasicConsumer(channel);
                    consumer.ReceivedAsync += async (model, ea) =>
                    {
                        var body = ea.Body.ToArray();
                        var messageBody = Encoding.UTF8.GetString(body);

                        var genericMessage = new GenericMessage
                        {
                            EntityName = streamName,
                            Payload = messageBody,
                            Metadata = FromRabbitMqHeaders(ea.BasicProperties.Headers),
                            Timestamp = DateTime.UtcNow,
                            MessageId = ea.BasicProperties.MessageId,
                            DeliveryTag = ea.DeliveryTag
                        };

                        await onMessageReceived(genericMessage);

                        // Acknowledge the message
                        await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                    };

                    await channel.BasicConsumeAsync(
                        queue: streamName,
                        autoAck: false,
                        consumer: consumer
                    );

                    Logger?.WriteLog($"[SubscribeAsync] Subscribed to stream '{streamName}'.");
                }
                else
                {
                    throw new Exception($"Channel not found for stream '{streamName}'.");
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[SubscribeAsync] Error while subscribing to stream '{streamName}': {ex.Message}");
                throw;
            }
        }

        // Acknowledge a specific message as successfully processed
        public async Task AcknowledgeMessageAsync(string streamName, GenericMessage message, CancellationToken cancellationToken)
        {
            try
            {
                if (Channels.TryGetValue(streamName, out var channel))
                {
                    if (message.DeliveryTag.HasValue)
                    {
                        await channel.BasicAckAsync(message.DeliveryTag.Value, multiple: false);
                        Logger?.WriteLog($"[AcknowledgeMessageAsync] Acknowledged message in stream '{streamName}'.");
                    }
                    else
                    {
                        throw new InvalidOperationException("DeliveryTag is required to acknowledge a message.");
                    }
                }
                else
                {
                    throw new Exception($"Channel not found for stream '{streamName}'.");
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[AcknowledgeMessageAsync] Error: {ex.Message}");
                throw;
            }
        }


        // Peek a single message from the stream without acknowledging
        public async Task<GenericMessage> PeekMessageAsync(string streamName, CancellationToken cancellationToken)
        {
            try
            {
                var createQueueResult = await CreateQueueAsync(streamName);
                if (createQueueResult.Flag == Errors.Failed)
                {
                    throw new Exception(createQueueResult.Message);
                }

                if (Channels.TryGetValue(streamName, out var channel))
                {
                    var result = await channel.BasicGetAsync(queue: streamName, autoAck: false);
                    if (result != null)
                    {
                        return new GenericMessage
                        {
                            EntityName = streamName,
                            Payload = Encoding.UTF8.GetString(result.Body.ToArray()),
                            Metadata = FromRabbitMqHeaders(result.BasicProperties.Headers),
                            Timestamp = DateTime.UtcNow,
                            MessageId = result.BasicProperties.MessageId
                        };
                    }
                }
                else
                {
                    throw new Exception($"Channel not found for stream '{streamName}'.");
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[PeekMessageAsync] Error: {ex.Message}");
                throw;
            }
        }

        // Retrieve metadata about the stream (e.g., message count, consumer count)
        public async Task<object> GetStreamMetadataAsync(string streamName, CancellationToken cancellationToken)
        {
            try
            {
                var createQueueResult = await CreateQueueAsync(streamName);
                if (createQueueResult.Flag == Errors.Failed)
                {
                    throw new Exception(createQueueResult.Message);
                }

                if (Channels.TryGetValue(streamName, out var channel))
                {
                    var result =await  channel.QueueDeclarePassiveAsync(queue: streamName);
                    return new
                    {
                        MessageCount = result.MessageCount,
                        ConsumerCount = result.ConsumerCount
                    };
                }
                else
                {
                    throw new Exception($"Channel not found for stream '{streamName}'.");
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[GetStreamMetadataAsync] Error: {ex.Message}");
                throw;
            }
        }

        // Disconnect all channels and clean up resources
        public void Disconnect()
        {
            try
            {
                DisconnectFromRabbitMQ();
                Logger?.WriteLog("[Disconnect] All channels and connections closed.");
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[Disconnect] Error: {ex.Message}");
                throw;
            }
        }

        // Convert GenericMessage Metadata to RabbitMQ Headers
        public IDictionary<string, object> ToRabbitMqHeaders(GenericMessage message)
        {
            var headers = new Dictionary<string, object>();
            foreach (var item in message.Metadata)
            {
                headers[item.Key] = item.Value;
            }
            return headers;
        }

        // Convert RabbitMQ Headers to GenericMessage Metadata
        public Dictionary<string, string> FromRabbitMqHeaders(IDictionary<string, object> headers)
        {
            var metadata = new Dictionary<string, string>();
            foreach (var header in headers)
            {
                metadata[header.Key] = header.Value?.ToString();
            }
            return metadata;
        }

        // Create IBasicProperties with Headers from GenericMessage
        public IBasicProperties ToRabbitMQHeaders(GenericMessage message)
        {
            IBasicProperties basicProperties = new BasicProperties
            {
                MessageId = message.MessageId,
                Timestamp = new AmqpTimestamp(new DateTimeOffset(message.Timestamp).ToUnixTimeSeconds()),
                Priority = (byte?)message.Priority ?? 0,
                ContentType = "application/json",
                Headers = ToRabbitMqHeaders(message)
            };
            return basicProperties;
        }

        #endregion "IMessageDataSource Methods"



    }
}
