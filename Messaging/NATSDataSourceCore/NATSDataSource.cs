using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NATS.Client;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Messaging;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.NATS;

namespace TheTechIdea.Beep.NATS
{
    [AddinAttribute(Category = DatasourceCategory.MessageQueue, DatasourceType = DataSourceType.NATS)]
    public class NATSDataSource : IDataSource, IDisposable, IMessageDataSource<GenericMessage, StreamConfig>
    {
        #region Properties

        private bool _disposed = false;
        private readonly Dictionary<string, IAsyncSubscription> _subscriptions = new();

        public string ColumnDelimiter { get; set; } = ",";
        public string ParameterDelimiter { get; set; } = ":";
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public DataSourceType DatasourceType { get; set; } = DataSourceType.NATS;
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

        public NATSDataConnection NATSConnection => Dataconnection as NATSDataConnection;
        public NATSConnectionProperties NATSProperties => NATSConnection?.NATSProperties;

        #endregion

        #region Constructor

        public NATSDataSource(string datasourcename, IDMLogger logger, IDMEEditor dmeEditor, DataSourceType databasetype, IErrorsInfo errorObject)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            DMEEditor = dmeEditor;
            DatasourceType = databasetype;
            ErrorObject = errorObject ?? new ErrorsInfo();
            Category = DatasourceCategory.MessageQueue;

            Dataconnection = new NATSDataConnection(dmeEditor)
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
                    Dataconnection.ConnectionProp = new NATSConnectionProperties { ConnectionName = datasourcename };
                }
            }
        }

        #endregion

        #region IDataSource Methods

        public ConnectionState Openconnection()
        {
            try
            {
                if (NATSConnection?.OpenConnection() == ConnectionState.Open)
                {
                    ConnectionStatus = ConnectionState.Open;
                    Logger?.WriteLog("[Openconnection] NATS connection opened successfully.");
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
                Logger?.WriteLog("[Closeconnection] NATS connection closed.");
                return ConnectionStatus;
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                Logger?.WriteLog($"[Closeconnection] Error: {ex.Message}");
                return ConnectionState.Broken;
            }
        }

        public bool CheckEntityExist(string EntityName) => true; // NATS subjects don't need to exist
        public bool CreateEntityAs(EntityStructure entity) => true;
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
        public object GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize) => new List<object>();
        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            try
            {
                var message = MessageStandardsHelper.CreateStandardMessage(EntityName, InsertedData, DatasourceName ?? "NATSDataSource");
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
                if (NATSConnection?.Connection == null || NATSConnection.Connection.IsClosed())
                    throw new InvalidOperationException("NATS connection is not open. Call OpenConnection() first.");

                message = MessageStandardsHelper.EnsureMessageStandards(message, DatasourceName ?? "NATSDataSource");
                var validation = MessageStandardsHelper.ValidateMessage(message);
                if (!validation.IsValid)
                    throw new InvalidOperationException($"Message validation failed: {string.Join("; ", validation.Errors)}");

                var payload = MessageStandardsHelper.SerializePayload(message.Payload);
                var data = System.Text.Encoding.UTF8.GetBytes(payload);

                // Use subject from streamName or properties
                var subject = streamName;
                if (NATSProperties != null && !string.IsNullOrEmpty(NATSProperties.Subject))
                    subject = NATSProperties.Subject;

                NATSConnection.Connection.Publish(subject, data);
                Logger?.WriteLog($"[SendMessageAsync] Message sent to subject '{subject}' with MessageId: {message.MessageId}");
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
                if (NATSConnection?.Connection == null || NATSConnection.Connection.IsClosed())
                    throw new InvalidOperationException("NATS connection is not open.");

                var subject = streamName;
                if (NATSProperties != null && !string.IsNullOrEmpty(NATSProperties.Subject))
                    subject = NATSProperties.Subject;

                var subscription = NATSConnection.Connection.SubscribeAsync(subject, (sender, args) =>
                {
                    try
                    {
                        var body = System.Text.Encoding.UTF8.GetString(args.Message.Data);
                        var genericMessage = new GenericMessage
                        {
                            MessageId = Guid.NewGuid().ToString(),
                            EntityName = streamName,
                            Payload = body,
                            Timestamp = DateTime.UtcNow
                        };

                        // Extract metadata from headers if available
                        if (args.Message.Header != null && args.Message.Header.Count > 0)
                        {
                            foreach (var header in args.Message.Header)
                            {
                                genericMessage.Metadata[header.Key] = header.Value;
                            }
                        }

                        genericMessage = MessageStandardsHelper.EnsureMessageStandards(
                            genericMessage,
                            DatasourceName ?? "NATSDataSource"
                        );

                        onMessageReceived?.Invoke(genericMessage).Wait(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        Logger?.WriteLog($"[SubscribeAsync] Error processing message: {ex.Message}");
                    }
                });

                _subscriptions[streamName] = subscription;
                Logger?.WriteLog($"[SubscribeAsync] Subscribed to subject '{subject}'");
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[SubscribeAsync] Error: {ex.Message}");
                throw;
            }
        }

        public async Task AcknowledgeMessageAsync(string streamName, GenericMessage message, CancellationToken cancellationToken)
        {
            // NATS doesn't require explicit acknowledgment for regular messages
            await Task.CompletedTask;
            Logger?.WriteLog($"[AcknowledgeMessageAsync] Message acknowledged for '{streamName}'");
        }

        public async Task<GenericMessage> PeekMessageAsync(string streamName, CancellationToken cancellationToken)
        {
            // NATS doesn't support true peek - would need to use request-reply pattern
            // For now, return null
            await Task.CompletedTask;
            Logger?.WriteLog("[PeekMessageAsync] NATS doesn't support peek operations. Use request-reply pattern.");
            return null;
        }

        public async Task<object> GetStreamMetadataAsync(string streamName, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            return new
            {
                Subject = streamName,
                IsConnected = NATSConnection?.Connection != null && !NATSConnection.Connection.IsClosed(),
                UseJetStream = NATSProperties?.UseJetStream ?? false
            };
        }

        public void Disconnect()
        {
            try
            {
                foreach (var subscription in _subscriptions.Values)
                {
                    subscription?.Unsubscribe();
                }
                _subscriptions.Clear();
                NATSConnection?.CloseConn();
                Logger?.WriteLog("[Disconnect] NATS disconnected.");
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

