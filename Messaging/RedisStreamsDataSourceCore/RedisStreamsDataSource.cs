using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Messaging;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.RedisStreams;

namespace TheTechIdea.Beep.RedisStreams
{
    [AddinAttribute(Category = DatasourceCategory.MessageQueue, DatasourceType = DataSourceType.Redis)]
    public class RedisStreamsDataSource : IDataSource, IDisposable, IMessageDataSource<GenericMessage, StreamConfig>
    {
        #region Properties

        private bool _disposed = false;
        private readonly Dictionary<string, string> _consumerGroups = new();

        public string ColumnDelimiter { get; set; } = ",";
        public string ParameterDelimiter { get; set; } = ":";
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public DataSourceType DatasourceType { get; set; } = DataSourceType.Redis;
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

        public RedisStreamsDataConnection RedisConnection => Dataconnection as RedisStreamsDataConnection;
        public RedisStreamsConnectionProperties RedisProperties => RedisConnection?.RedisProperties;

        #endregion

        #region Constructor

        public RedisStreamsDataSource(string datasourcename, IDMLogger logger, IDMEEditor dmeEditor, DataSourceType databasetype, IErrorsInfo errorObject)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            DMEEditor = dmeEditor;
            DatasourceType = databasetype;
            ErrorObject = errorObject ?? new ErrorsInfo();
            Category = DatasourceCategory.MessageQueue;

            Dataconnection = new RedisStreamsDataConnection(dmeEditor)
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
                    Dataconnection.ConnectionProp = new RedisStreamsConnectionProperties { ConnectionName = datasourcename };
                }
            }
        }

        #endregion

        #region IDataSource Methods

        public ConnectionState Openconnection()
        {
            try
            {
                if (RedisConnection?.OpenConnection() == ConnectionState.Open)
                {
                    ConnectionStatus = ConnectionState.Open;
                    Logger?.WriteLog("[Openconnection] Redis connection opened successfully.");
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
                Logger?.WriteLog("[Closeconnection] Redis connection closed.");
                return ConnectionStatus;
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                Logger?.WriteLog($"[Closeconnection] Error: {ex.Message}");
                return ConnectionState.Broken;
            }
        }

        private async Task EnsureConsumerGroupAsync(string streamName, string consumerGroup)
        {
            try
            {
                var key = $"{streamName}:{consumerGroup}";
                if (_consumerGroups.ContainsKey(key))
                    return;

                var db = RedisConnection.Database;
                await db.StreamCreateConsumerGroupAsync(streamName, consumerGroup, "0", createStream: true);
                _consumerGroups[key] = consumerGroup;
            }
            catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
            {
                // Consumer group already exists
                _consumerGroups[$"{streamName}:{consumerGroup}"] = consumerGroup;
            }
        }

        public bool CheckEntityExist(string EntityName)
        {
            try
            {
                var db = RedisConnection.Database;
                var info = db.StreamInfo(EntityName);
                return info != null;
            }
            catch
            {
                return false;
            }
        }

        public bool CreateEntityAs(EntityStructure entity) => true; // Streams are created automatically
        public object GetEntity(string EntityName, List<AppFilter> filter)
        {
            try
            {
                var message = PeekMessageAsync(EntityName, CancellationToken.None).Result;
                return message?.Payload;
            }
            catch { return null; }
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
                var db = RedisConnection.Database;
                var streamEntries = db.StreamRange(EntityName, "-", "+", count: pageSize);
                foreach (var entry in streamEntries)
                {
                    messages.Add(entry.Values.FirstOrDefault().Value.ToString());
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
                var message = MessageStandardsHelper.CreateStandardMessage(EntityName, InsertedData, DatasourceName ?? "RedisStreamsDataSource");
                SendMessageAsync(EntityName, message, CancellationToken.None).Wait();
                ErrorObject.Flag = Errors.Ok;
                return ErrorObject;
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                ErrorObject.Ex = ex;
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
                if (RedisConnection?.Database == null)
                    throw new InvalidOperationException("Redis connection is not open. Call OpenConnection() first.");

                message = MessageStandardsHelper.EnsureMessageStandards(message, DatasourceName ?? "RedisStreamsDataSource");
                var validation = MessageStandardsHelper.ValidateMessage(message);
                if (!validation.IsValid)
                    throw new InvalidOperationException($"Message validation failed: {string.Join("; ", validation.Errors)}");

                var db = RedisConnection.Database;
                var payload = MessageStandardsHelper.SerializePayload(message.Payload);

                var nameValueEntries = new NameValueEntry[]
                {
                    new NameValueEntry("payload", payload),
                    new NameValueEntry("messageId", message.MessageId),
                    new NameValueEntry("entityName", message.EntityName),
                    new NameValueEntry("timestamp", message.Timestamp.ToString("O"))
                };

                // Add metadata
                foreach (var kvp in message.Metadata)
                {
                    var entries = nameValueEntries.ToList();
                    entries.Add(new NameValueEntry($"meta_{kvp.Key}", kvp.Value));
                    nameValueEntries = entries.ToArray();
                }

                var streamNameToUse = RedisProperties?.StreamName ?? streamName;
                var messageId = await db.StreamAddAsync(streamNameToUse, nameValueEntries);

                // Trim stream if MaxLength is set
                if (RedisProperties?.MaxLength > 0)
                {
                    await db.StreamTrimAsync(streamNameToUse, RedisProperties.MaxLength, RedisProperties.ApproximateMaxLength);
                }

                Logger?.WriteLog($"[SendMessageAsync] Message sent to stream '{streamNameToUse}' with MessageId: {message.MessageId}");
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
                if (RedisConnection?.Database == null)
                    throw new InvalidOperationException("Redis connection is not open.");

                var db = RedisConnection.Database;
                var streamNameToUse = RedisProperties?.StreamName ?? streamName;
                var consumerGroup = RedisProperties?.ConsumerGroup ?? "default-group";
                var consumerName = RedisProperties?.ConsumerName ?? "default-consumer";

                // Ensure consumer group exists
                await EnsureConsumerGroupAsync(streamNameToUse, consumerGroup);

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var messages = await db.StreamReadGroupAsync(
                            streamNameToUse,
                            consumerGroup,
                            consumerName,
                            position: ">",
                            count: RedisProperties?.Count ?? 1,
                            flags: CommandFlags.None);

                        foreach (var message in messages)
                        {
                            try
                            {
                                var payload = message.Values.FirstOrDefault(v => v.Name == "payload").Value.ToString();
                                var messageId = message.Values.FirstOrDefault(v => v.Name == "messageId").Value.ToString();

                                var genericMessage = new GenericMessage
                                {
                                    MessageId = messageId ?? Guid.NewGuid().ToString(),
                                    EntityName = streamName,
                                    Payload = payload,
                                    Timestamp = DateTime.UtcNow
                                };

                                // Extract metadata
                                foreach (var value in message.Values)
                                {
                                    if (value.Name.StartsWith("meta_"))
                                    {
                                        genericMessage.Metadata[value.Name.Substring(5)] = value.Value.ToString();
                                    }
                                }

                                // Store message ID for acknowledgment
                                genericMessage.Metadata["RedisMessageId"] = message.Id.ToString();

                                genericMessage = MessageStandardsHelper.EnsureMessageStandards(
                                    genericMessage,
                                    DatasourceName ?? "RedisStreamsDataSource"
                                );

                                if (onMessageReceived != null)
                                    await onMessageReceived(genericMessage);

                                // Acknowledge message
                                await db.StreamAcknowledgeAsync(streamNameToUse, consumerGroup, message.Id);
                            }
                            catch (Exception ex)
                            {
                                Logger?.WriteLog($"[SubscribeAsync] Error processing message: {ex.Message}");
                            }
                        }

                        if (RedisProperties?.BlockTimeMs > 0)
                        {
                            await Task.Delay(RedisProperties.BlockTimeMs, cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger?.WriteLog($"[SubscribeAsync] Error reading messages: {ex.Message}");
                        await Task.Delay(1000, cancellationToken);
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
                if (message.Metadata?.TryGetValue("RedisMessageId", out var messageId) == true)
                {
                    var db = RedisConnection.Database;
                    var streamNameToUse = RedisProperties?.StreamName ?? streamName;
                    var consumerGroup = RedisProperties?.ConsumerGroup ?? "default-group";
                    await db.StreamAcknowledgeAsync(streamNameToUse, consumerGroup, messageId);
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
                var db = RedisConnection.Database;
                var streamNameToUse = RedisProperties?.StreamName ?? streamName;
                var entries = await db.StreamRangeAsync(streamNameToUse, "-", "+", count: 1);
                if (entries.Length == 0)
                    return null;

                var entry = entries[0];
                var payload = entry.Values.FirstOrDefault(v => v.Name == "payload").Value.ToString();
                var messageId = entry.Values.FirstOrDefault(v => v.Name == "messageId").Value.ToString();

                var genericMessage = new GenericMessage
                {
                    MessageId = messageId ?? Guid.NewGuid().ToString(),
                    EntityName = streamName,
                    Payload = payload,
                    Timestamp = DateTime.UtcNow
                };

                foreach (var value in entry.Values)
                {
                    if (value.Name.StartsWith("meta_"))
                    {
                        genericMessage.Metadata[value.Name.Substring(5)] = value.Value.ToString();
                    }
                }

                genericMessage.Metadata["RedisMessageId"] = entry.Id.ToString();

                genericMessage = MessageStandardsHelper.EnsureMessageStandards(
                    genericMessage,
                    DatasourceName ?? "RedisStreamsDataSource"
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
                var db = RedisConnection.Database;
                var streamNameToUse = RedisProperties?.StreamName ?? streamName;
                var info = await db.StreamInfoAsync(streamNameToUse);
                return new
                {
                    StreamName = streamNameToUse,
                    Length = info.Length,
                    FirstEntryId = info.FirstEntry.ToString(),
                    LastEntryId = info.LastEntry.ToString(),
                    ConsumerGroups = info.ConsumerGroupCount
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
                _consumerGroups.Clear();
                RedisConnection?.CloseConn();
                Logger?.WriteLog("[Disconnect] Redis Streams disconnected.");
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

