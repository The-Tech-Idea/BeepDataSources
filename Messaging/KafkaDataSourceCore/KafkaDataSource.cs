using Confluent.Kafka;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO.Pipelines;

using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.EventStream.Kafka;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Messaging;
using System.Text.Json;

namespace TheTechIdea.Beep.EventStream
{
    [AddinAttribute(Category = DatasourceCategory.QUEUE, DatasourceType = DataSourceType.Kafka)]
    public class KafkaDataSource : IDataSource, IDisposable, IMessageDataSource<GenericMessage, StreamConfig>
    { // Properties
        public string GuidID { get; set; }
        public KafkaDataConnection kdataconnection { get; set; }
        public bool StopConsume { get; set; } = true;

        private readonly Dictionary<string, IConsumer<Ignore, string>> Consumers = new();
        private readonly Dictionary<string, IProducer<Null, string>> Producers = new();

   
        public KafkaDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            Category = DatasourceCategory.STREAM;
            DatasourceType = DataSourceType.Kafka;
            DMEEditor = pDMEEditor;
            Dataconnection = new KafkaDataConnection
            {
                Logger = logger,
                ErrorObject = ErrorObject,
                DMEEditor = pDMEEditor

            };


            if (DMEEditor.DataSources.Where(o => o.DatasourceName == datasourcename).Any())
            {
                Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(o => o.ConnectionName == datasourcename).FirstOrDefault();
            }
            kdataconnection = (KafkaDataConnection)Dataconnection;

        }
      
        public virtual string ColumnDelimiter { get; set; } = "''";
        public virtual string ParameterDelimiter { get; set; } = ":";
        public event EventHandler<PassedArgs> PassEvent;
        public DataSourceType DatasourceType { get; set; }
        public DatasourceCategory Category { get; set; }
        public IDataConnection Dataconnection { get; set; }
        public string DatasourceName { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public string Id { get; set; }
        public IDMLogger Logger { get; set; }
        public List<string> EntitiesNames { get; set; }
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
        public IDMEEditor DMEEditor { get; set; }

        public ConnectionState ConnectionStatus { get { return Dataconnection.ConnectionStatus; } set { } }

        #region IDataSource Methods
        #region "IDataSource Interface Implementations"

        // Check if an entity (topic) exists in Kafka
        public bool CheckEntityExist(string EntityName)
        {
            try
            {
                using (var adminClient = new AdminClientBuilder(kdataconnection.ConsConfig).Build())
                {
                    var metadata = adminClient.GetMetadata(EntityName, TimeSpan.FromSeconds(5));
                    return metadata.Topics.Any(topic => topic.Topic == EntityName);
                }
            }
            catch (KafkaException ex)
            {
                Logger?.WriteLog($"[CheckEntityExist] Error checking entity '{EntityName}': {ex.Message}");
                return false;
            }
        }

        // Close the Kafka connection
        public ConnectionState Closeconnection()
        {
            try
            {
                // Disconnect all producers and consumers
                Disconnect();
                Logger?.WriteLog("[Closeconnection] Successfully closed Kafka connection.");
                return ConnectionState.Closed;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[Closeconnection] Error closing Kafka connection: {ex.Message}");
                return ConnectionState.Broken;
            }
        }

        // Create an entity (topic) in Kafka if it does not already exist
        public bool CreateEntityAs(EntityStructure entity)
        {
            try
            {
                if (!CheckEntityExist(entity.EntityName))
                {
                    Initialize(new StreamConfig { EntityName = entity.EntityName });
                    Logger?.WriteLog($"[CreateEntityAs] Successfully created entity '{entity.EntityName}'.");
                    return true;
                }
                else
                {
                    Logger?.WriteLog($"[CreateEntityAs] Entity '{entity.EntityName}' already exists.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[CreateEntityAs] Error creating entity '{entity.EntityName}': {ex.Message}");
                return false;
            }
        }

        // Open the Kafka connection
        public ConnectionState Openconnection()
        {
            try
            {
                if (kdataconnection.OpenConnection() == ConnectionState.Open)
                {
                    Logger?.WriteLog("[Openconnection] Successfully opened Kafka connection.");
                    return ConnectionState.Open;
                }
                else
                {
                    Logger?.WriteLog("[Openconnection] Kafka connection is already open or failed to open.");
                    return ConnectionState.Broken;
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[Openconnection] Error opening Kafka connection: {ex.Message}");
                return ConnectionState.Broken;
            }
        }

        #endregion

        public IErrorsInfo BeginTransaction(PassedArgs args)
        {
            var errors = new ErrorsInfo { Flag = Errors.Warning };
            Logger?.WriteLog("[BeginTransaction] Kafka does not support transactions.");
            return errors;
        }

        public IErrorsInfo Commit(PassedArgs args)
        {
            var errors = new ErrorsInfo { Flag = Errors.Warning };
            Logger?.WriteLog("[Commit] Kafka does not support transactions.");
            return errors;
        }

        public IErrorsInfo EndTransaction(PassedArgs args)
        {
            var errors = new ErrorsInfo { Flag = Errors.Warning };
            Logger?.WriteLog("[EndTransaction] Kafka does not support transactions.");
            return errors;
        }

        public IErrorsInfo ExecuteSql(string sql)
        {
            var errors = new ErrorsInfo { Flag = Errors.Warning };
            Logger?.WriteLog("[ExecuteSql] Kafka is not a SQL-based system.");
            return errors;
        }

        public List<ChildRelation> GetChildTablesList(string tablename, string schemaName, string filterParameters)
        {
            var errors = new ErrorsInfo { Flag = Errors.Warning };
            Logger?.WriteLog("[GetChildTablesList] Kafka does not support relational schemas.");
            return new List<ChildRelation>();
        }

        public List<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            var errors = new ErrorsInfo { Flag = Errors.Warning };
            Logger?.WriteLog("[GetCreateEntityScript] Kafka does not use entity creation scripts.");
            return new List<ETLScriptDet>();
        }

        public List<string> GetEntitesList()
        {
            // Fetch the list of available topics in Kafka.
            try
            {
                using var adminClient = new AdminClientBuilder(kdataconnection.ProdConfig).Build();
                var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
                return metadata.Topics.Select(t => t.Topic).ToList();
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[GetEntitiesList] Error fetching topics: {ex.Message}");
                return new List<string>();
            }
        }

        public List<RelationShipKeys> GetEntityforeignkeys(string entityName, string schemaName)
        {
            Logger?.WriteLog("[GetEntityforeignkeys] Kafka does not support foreign keys.");
            return new List<RelationShipKeys>();
        }

        public int GetEntityIdx(string entityName)
        {
            var entities = GetEntitesList();
            return entities.IndexOf(entityName);
        }

        public EntityStructure GetEntityStructure(string entityName, bool refresh)
        {
            Logger?.WriteLog("[GetEntityStructure] Kafka topics do not have a predefined structure.");
            return new EntityStructure
            {
                EntityName = entityName,
                DatasourceEntityName = entityName,
                Fields = new List<EntityField>() // No predefined fields for Kafka topics.
            };
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            return GetEntityStructure(fnd.EntityName, refresh);
        }

        public Type GetEntityType(string entityName)
        {
            Logger?.WriteLog("[GetEntityType] Kafka topics do not map to a specific entity type.");
            return typeof(string); // Messages are typically string-based in Kafka.
        }

        public double GetScalar(string query)
        {
            Logger?.WriteLog("[GetScalar] Kafka does not support scalar queries.");
            return 0.0;
        }

        public async Task<double> GetScalarAsync(string query)
        {
            Logger?.WriteLog("[GetScalarAsync] Kafka does not support scalar queries.");
            return await Task.FromResult(0.0);
        }

        public object RunQuery(string qrystr)
        {
            Logger?.WriteLog("[RunQuery] Kafka does not support SQL queries.");
            return null;
        }

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            var errors = new ErrorsInfo { Flag = Errors.Warning };
            Logger?.WriteLog("[RunScript] Kafka does not support script execution.");
            return errors;
        }
        #endregion

        #region "Pull/Push Messages in IDataSource"

        // Update a single entity by pushing a message to Kafka
        public virtual IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            var errors = new ErrorsInfo();
            try
            {
                var genericMessage = new GenericMessage
                {
                    EntityName = EntityName,
                    Payload = UploadDataRow,
                    Timestamp = DateTime.UtcNow
                };

                // Use the IMessageDataSource SendMessageAsync method
                SendMessageAsync(EntityName, genericMessage, CancellationToken.None).Wait();
                Logger?.WriteLog($"[UpdateEntity] Successfully updated entity '{EntityName}'.");
            }
            catch (Exception ex)
            {
                errors.Flag = Errors.Failed;
                errors.Message = ex.Message;
                Logger?.WriteLog($"[UpdateEntity] Error updating entity '{EntityName}': {ex.Message}");
            }
            return errors;
        }

        // Delete an entity by pushing a message to Kafka with a delete marker
        public IErrorsInfo DeleteEntity(string EntityName, object DeletedDataRow)
        {
            var errors = new ErrorsInfo();
            try
            {
                var genericMessage = new GenericMessage
                {
                    EntityName = EntityName,
                    Payload = DeletedDataRow,
                    Metadata = new Dictionary<string, string> { { "Operation", "Delete" } },
                    Timestamp = DateTime.UtcNow
                };

                // Use the IMessageDataSource SendMessageAsync method
                SendMessageAsync(EntityName, genericMessage, CancellationToken.None).Wait();
                Logger?.WriteLog($"[DeleteEntity] Successfully marked entity '{EntityName}' for deletion.");
            }
            catch (Exception ex)
            {
                errors.Flag = Errors.Failed;
                errors.Message = ex.Message;
                Logger?.WriteLog($"[DeleteEntity] Error deleting entity '{EntityName}': {ex.Message}");
            }
            return errors;
        }

        // Update multiple entities using IMessageDataSource
        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            var errors = new ErrorsInfo();
            try
            {
                if (UploadData is IEnumerable<object> dataList)
                {
                    foreach (var data in dataList)
                    {
                        var genericMessage = new GenericMessage
                        {
                            EntityName = EntityName,
                            Payload = data,
                            Timestamp = DateTime.UtcNow
                        };

                        // Use the IMessageDataSource SendMessageAsync method
                        SendMessageAsync(EntityName, genericMessage, CancellationToken.None).Wait();

                        // Report progress if applicable
                        progress?.Report(new PassedArgs
                        {
                            Messege = $"Updated entity '{EntityName}' with data: {data}"
                        });
                    }
                    Logger?.WriteLog($"[UpdateEntities] Successfully updated multiple entities for '{EntityName}'.");
                }
                else
                {
                    throw new ArgumentException("UploadData must be an IEnumerable of objects.");
                }
            }
            catch (Exception ex)
            {
                errors.Flag = Errors.Failed;
                errors.Message = ex.Message;
                Logger?.WriteLog($"[UpdateEntities] Error updating entities for '{EntityName}': {ex.Message}");
            }
            return errors;
        }

        // Create new entities (topics in Kafka)
        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            var errors = new ErrorsInfo();
            try
            {
                foreach (var entity in entities)
                {
                    // Use the IMessageDataSource Initialize method to create the topic
                    var config = new StreamConfig { EntityName = entity.EntityName };
                    Initialize(config);
                    Logger?.WriteLog($"[CreateEntities] Successfully created entity '{entity.EntityName}'.");
                }
            }
            catch (Exception ex)
            {
                errors.Flag = Errors.Failed;
                errors.Message = ex.Message;
                Logger?.WriteLog($"[CreateEntities] Error creating entities: {ex.Message}");
            }
            return errors;
        }

        // Insert a single entity using IMessageDataSource
        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            var errors = new ErrorsInfo();
            try
            {
                var genericMessage = new GenericMessage
                {
                    EntityName = EntityName,
                    Payload = InsertedData,
                    Timestamp = DateTime.UtcNow
                };

                // Use the IMessageDataSource SendMessageAsync method
                SendMessageAsync(EntityName, genericMessage, CancellationToken.None).Wait();
                Logger?.WriteLog($"[InsertEntity] Successfully inserted entity '{EntityName}'.");
            }
            catch (Exception ex)
            {
                errors.Flag = Errors.Failed;
                errors.Message = ex.Message;
                Logger?.WriteLog($"[InsertEntity] Error inserting entity '{EntityName}': {ex.Message}");
            }
            return errors;
        }

        // Fetch data asynchronously from a Kafka topic
        public async Task<object> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            try
            {
                // Use IMessageDataSource PeekMessageAsync to retrieve the latest message
                var message = await PeekMessageAsync(EntityName, CancellationToken.None);
                Logger?.WriteLog($"[GetEntityAsync] Retrieved message for entity '{EntityName}': {message.Payload}");
                return message.Payload;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[GetEntityAsync] Error fetching entity '{EntityName}': {ex.Message}");
                throw;
            }
        }
        public object GetEntity(string EntityName, List<AppFilter> filter)
        {
            try
            {
                if (!Consumers.TryGetValue(EntityName, out var consumer))
                {
                    // Create a new consumer if it doesn't exist
                    consumer = new ConsumerBuilder<Ignore, string>(kdataconnection.ConsConfig).Build();
                    consumer.Subscribe(EntityName);
                    Consumers[EntityName] = consumer;
                }

                var results = new List<GenericMessage>();
                var consumeResult = consumer.Consume(TimeSpan.FromSeconds(5)); // Timeout to avoid indefinite blocking

                while (consumeResult != null)
                {
                    // Apply filters (if any)
                    if (ApplyFilters(consumeResult.Message.Value, filter))
                    {
                        results.Add(new GenericMessage
                        {
                            EntityName = EntityName,
                            Payload = consumeResult.Message.Value,
                            Timestamp = consumeResult.Message.Timestamp.UtcDateTime
                        });
                    }

                    // Consume the next message
                    consumeResult = consumer.Consume(TimeSpan.FromSeconds(5));
                }

                Logger?.WriteLog($"[GetEntity] Retrieved messages from topic '{EntityName}'.");
                return results;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[GetEntity] Error retrieving messages from topic '{EntityName}': {ex.Message}");
                throw;
            }
        }
        public object GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            try
            {
                if (!Consumers.TryGetValue(EntityName, out var consumer))
                {
                    // Create a new consumer if it doesn't exist
                    consumer = new ConsumerBuilder<Ignore, string>(kdataconnection.ConsConfig).Build();
                    consumer.Subscribe(EntityName);
                    Consumers[EntityName] = consumer;
                }

                var results = new List<GenericMessage>();
                var consumeResult = consumer.Consume(TimeSpan.FromSeconds(5)); // Timeout to avoid indefinite blocking
                int skipCount = (pageNumber - 1) * pageSize;
                int messageCount = 0;

                while (consumeResult != null && messageCount < skipCount + pageSize)
                {
                    // Apply filters (if any)
                    if (ApplyFilters(consumeResult.Message.Value, filter))
                    {
                        if (messageCount >= skipCount)
                        {
                            results.Add(new GenericMessage
                            {
                                EntityName = EntityName,
                                Payload = consumeResult.Message.Value,
                                Timestamp = consumeResult.Message.Timestamp.UtcDateTime
                            });
                        }
                        messageCount++;
                    }

                    // Consume the next message
                    consumeResult = consumer.Consume(TimeSpan.FromSeconds(5));
                }

                Logger?.WriteLog($"[GetEntity (Paged)] Retrieved page {pageNumber} of messages from topic '{EntityName}'.");
                return results;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[GetEntity (Paged)] Error retrieving messages from topic '{EntityName}': {ex.Message}");
                throw;
            }
        }
        private bool ApplyFilters(string messageValue, List<AppFilter> filters)
        {
            if (filters == null || filters.Count == 0)
                return true; // No filters, include all messages

            foreach (var filter in filters)
            {
                // Example: Check if message contains the filter value
                if (!messageValue.Contains(filter.FilterValue, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true; // Message satisfies all filters
        }

        #endregion "Pull/Push Messages in IDataSource"
        #region "dispose"
        private bool disposedValue;
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
        // ~RDBSource()
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
        #region "Kafka Methods"
        public Pipe pipe { get; set; } = new Pipe();
        public PipeReader reader { get; set; }
        public PipeWriter writer { get; set; }
        CancellationTokenSource cts = new CancellationTokenSource();
        public void handler(DeliveryReport<Null, string> deliveryReport)
        {
            DMEEditor.AddLogMessage("Kafka Producer", $"{deliveryReport.Status} {deliveryReport.Message}", deliveryReport.Timestamp.UtcDateTime, 0, deliveryReport.Value, Errors.Ok);
        }
        private void ProduceTopic(string topic, List<string> Values)
        {
            if (kdataconnection.OpenConnection() == ConnectionState.Open)
            {

            }
            using (var p = new ProducerBuilder<Null, string>(kdataconnection.ProdConfig).Build())
            {
                for (int i = 0; Values.Count >= i; ++i)
                {
                    p.Produce(topic, new Message<Null, string> { Value = Values[i] }, handler);
                }
                // wait for up to 10 seconds for any inflight messages to be delivered.
                //p.Flush(TimeSpan.FromSeconds(10));
            }
        }
        private Task ConsumeTopic(string topic)
        {

            using (var c = new ConsumerBuilder<Ignore, string>(kdataconnection.ConsConfig).Build())
            {
                c.Subscribe(topic);

                CancellationTokenSource cts = new CancellationTokenSource();

                Console.CancelKeyPress += (_, e) => {
                    e.Cancel = true; // prevent the process from terminating.
                    cts.Cancel();
                };

                try
                {
                    while (!cts.IsCancellationRequested)
                    {
                        try
                        {
                            var cr = c.Consume(cts.Token);
                            if (string.IsNullOrEmpty(cr.Message.Value))
                            {
                                cts.Cancel();
                            }

                            DMEEditor.AddLogMessage("Kafka Consumer", $"Consumed message '{cr.Message.Value}' at: '{cr.TopicPartitionOffset}'.", cr.Message.Timestamp.UtcDateTime, 0, cr.Message.Value, Errors.Ok);
                            //  Console.WriteLine($"Consumed message '{cr.Value}' at: '{cr.TopicPartitionOffset}'.");
                        }
                        catch (ConsumeException e)
                        {
                            DMEEditor.AddLogMessage("Kafka Consumer", $"Error occured: {e.Error.Reason}", DateTime.Now, 0, e.Error.Reason, Errors.Failed);
                            //Console.WriteLine($"Error occured: {e.Error.Reason}");
                        }
                    }
                }
                catch (OperationCanceledException canc)
                {
                    // Close and Release all the resources held by this consumer
                    DMEEditor.AddLogMessage("Kafka Consumer", $"Cancelation occured: {canc.Message}", DateTime.Now, 0, canc.Message, Errors.Ok);
                    c.Close();
                }
            }
            return Task.CompletedTask;
        }
        public Task StopAsync()
        {
            if (cts != null)
            {
                cts.Cancel();
            }

            return Task.CompletedTask;
        }
        #endregion
        #region IMessageDataSource Methods
        private readonly ConsumerConfig _consumerConfig;
        public void Initialize(StreamConfig config)
        {
            try
            {
                Logger?.WriteLog($"[Initialize] KafkaDataSource initialized with config: {JsonSerializer.Serialize(config)}");
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[Initialize] Error initializing KafkaDataSource: {ex.Message}");
                throw;
            }
        }

        public async Task SendMessageAsync(string streamName, GenericMessage message, CancellationToken cancellationToken)
        {
            try
            {
                if (!Producers.TryGetValue(streamName, out var producer))
                {
                    producer = new ProducerBuilder<Null, string>(kdataconnection.ProdConfig).Build();
                    Producers[streamName] = producer;
                }

                var payload = message.SerializePayload();
                var deliveryResult = await producer.ProduceAsync(
                    streamName,
                    new Message<Null, string> { Value = payload },
                    cancellationToken
                );

                Logger?.WriteLog($"[SendMessageAsync] Message sent to topic '{streamName}': {payload}");
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[SendMessageAsync] Error sending message to topic '{streamName}': {ex.Message}");
                throw;
            }
        }

        public async Task SubscribeAsync(string streamName, Func<GenericMessage, Task> onMessageReceived, CancellationToken cancellationToken)
        {
            try
            {
                if (!Consumers.TryGetValue(streamName, out var consumer))
                {
                    var consumerConfig = kdataconnection.ConsConfig;
                    consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
                    consumer.Subscribe(streamName);
                    Consumers[streamName] = consumer;
                }

                await Task.Run(() =>
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var consumeResult = consumer.Consume(cancellationToken);
                        var genericMessage = new GenericMessage
                        {
                            EntityName = streamName,
                            Payload = consumeResult.Message.Value,
                            Timestamp = consumeResult.Message.Timestamp.UtcDateTime
                        };

                        onMessageReceived?.Invoke(genericMessage).Wait(cancellationToken);

                        Logger?.WriteLog($"[SubscribeAsync] Message consumed from topic '{streamName}': {consumeResult.Message.Value}");
                    }
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[SubscribeAsync] Error subscribing to topic '{streamName}': {ex.Message}");
                throw;
            }
        }

        public async Task AcknowledgeMessageAsync(string streamName, GenericMessage message, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            Logger?.WriteLog($"[AcknowledgeMessageAsync] Kafka does not require explicit message acknowledgment.");
        }

        public async Task<GenericMessage> PeekMessageAsync(string streamName, CancellationToken cancellationToken)
        {
            try
            {
                if (!Consumers.TryGetValue(streamName, out var consumer))
                {
                    throw new Exception($"Consumer not found for topic '{streamName}'.");
                }

                var consumeResult = consumer.Consume(TimeSpan.FromSeconds(5));
                if (consumeResult == null)
                {
                    return null;
                }

                return new GenericMessage
                {
                    EntityName = streamName,
                    Payload = consumeResult.Message.Value,
                    Timestamp = consumeResult.Message.Timestamp.UtcDateTime
                };
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[PeekMessageAsync] Error peeking message from topic '{streamName}': {ex.Message}");
                throw;
            }
        }

        public async Task<object> GetStreamMetadataAsync(string streamName, CancellationToken cancellationToken)
        {
            using var adminClient = new AdminClientBuilder(kdataconnection.ProdConfig).Build();
            var metadata = adminClient.GetMetadata(streamName, TimeSpan.FromSeconds(10));
            return new
            {
                Topic = metadata.Topics.First().Topic,
                Partitions = metadata.Topics.First().Partitions.Count
            };
        }
        public void GetMetadata(string topicName)
        {
            using (var adminClient = new AdminClientBuilder(_consumerConfig).Build())
            {
                try
                {
                    var metadata = adminClient.GetMetadata(topicName, TimeSpan.FromSeconds(10));

                    Console.WriteLine($"Metadata for Topic: {topicName}");
                    Console.WriteLine($"- Partitions: {metadata.Topics.First().Partitions.Count}");

                    foreach (var partition in metadata.Topics.First().Partitions)
                    {
                        Console.WriteLine($"  Partition ID: {partition.PartitionId}, Leader: {partition.Leader}, Replicas: {string.Join(",", partition.Replicas)}");
                    }
                }
                catch (KafkaException ex)
                {
                    Console.WriteLine($"Error fetching metadata: {ex.Message}");
                }
            }
        }
        public void Disconnect()
        {
            foreach (var producer in Producers.Values)
            {
                producer.Flush();
                producer.Dispose();
            }

            foreach (var consumer in Consumers.Values)
            {
                consumer.Close();
                consumer.Dispose();
            }

            Producers.Clear();
            Consumers.Clear();
            Logger?.WriteLog("[Disconnect] Disconnected all Kafka producers and consumers.");
        }

        #endregion

    }
}
