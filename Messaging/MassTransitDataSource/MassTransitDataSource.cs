using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.KafkaIntegration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
using static MassTransit.MessageHeaders;

namespace MassTransitDataSourceCore
{
    [AddinAttribute(Category = DatasourceCategory.MessageQueue, DatasourceType = DataSourceType.MassTransit)]
    public class MassTransitDataSource : IDataSource
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

        private IBusControl BusControl { get; set; }

        public MassTransitTransportType TransportType { get; set; } = MassTransitTransportType.RabbitMQ;
        public MassTransitSerializerType SerializerType { get; set; } = MassTransitSerializerType.Json;
        public MassTransitTransportMode TransportMode { get; set; } = MassTransitTransportMode.Client;

        public Dictionary<string, List<GenericMessage>> QueueData { get; set; } = new Dictionary<string, List<GenericMessage>>();

        public event EventHandler<PassedArgs> PassEvent;
        private IServiceCollection Services { get; set; } = new ServiceCollection();
        private IHost Host { get; set; }
        #endregion

        #region Constructor
        public MassTransitDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            this.DMEEditor = DMEEditor;
            DatasourceType = databasetype;
            ErrorObject = per;
            Dataconnection = new MassTransitDataConnection(DMEEditor)
            {
                Logger = logger,
                ErrorObject = per
            };
        }
        #endregion
        #region IMessageDataSource Methods
        public void Initialize(StreamConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (!StreamConfigs.ContainsKey(config.EntityName))
                StreamConfigs[config.EntityName] = config;

            Logger?.WriteLog($"Stream '{config.EntityName}' initialized.");
        }

        public async Task SendMessageAsync(string streamName, GenericMessage message, CancellationToken cancellationToken)
        {
            if (_busControl == null || ConnectionStatus != ConnectionState.Open)
                throw new InvalidOperationException("Bus is not connected. Call Openconnection() before sending messages.");

            if (!StreamConfigs.ContainsKey(streamName))
                throw new KeyNotFoundException($"Stream configuration for '{streamName}' not found.");

            var config = StreamConfigs[streamName];
            var sendEndpoint = await _busControl.GetSendEndpoint(new Uri($"{Dataconnection.ConnectionProp.Host}/{config.EntityName}"));

            await sendEndpoint.Send(message, cancellationToken);
            Logger?.WriteLog($"Message sent to stream '{streamName}'.");
        }

        public async Task SubscribeAsync(string streamName, Func<GenericMessage, Task> onMessageReceived, CancellationToken cancellationToken)
        {
            if (_busControl == null || ConnectionStatus != ConnectionState.Open)
                throw new InvalidOperationException("Bus is not connected. Call Openconnection() before subscribing to streams.");

            if (!StreamConfigs.ContainsKey(streamName))
                throw new KeyNotFoundException($"Stream configuration for '{streamName}' not found.");

            var config = StreamConfigs[streamName];
            _services.AddMassTransit(cfg =>
            {
                cfg.AddConsumer<GenericConsumer>();
                cfg.UsingRabbitMq((context, rabbitCfg) =>
                {
                    rabbitCfg.Host(Dataconnection.ConnectionProp.Host, h =>
                    {
                        h.Username(Dataconnection.ConnectionProp.UserID);
                        h.Password(Dataconnection.ConnectionProp.Password);
                    });

                    rabbitCfg.ReceiveEndpoint(config.EntityName, ep =>
                    {
                        ep.ConfigureConsumer<GenericConsumer>(context);
                        ep.Handler<GenericMessage>(async context =>
                        {
                            await onMessageReceived(context.Message);
                        });
                    });
                });
            });

            Logger?.WriteLog($"Subscribed to stream '{streamName}'.");
        }

        public void Disconnect()
        {
            Closeconnection();
        }
        #endregion
        #region Connection Methods
        public ConnectionState Openconnection()
        {
            try
            {
                if (BusControl != null)
                {
                    Logger?.WriteLog("Bus is already connected.");
                    return ConnectionState.Open;
                }

                if (Dataconnection?.ConnectionProp == null)
                {
                    throw new InvalidOperationException("Connection properties are not initialized.");
                }

                // Configure services for MassTransit
                ConfigureServices(Services);

                // Build the service provider and resolve the bus
                var serviceProvider = Services.BuildServiceProvider();
                BusControl = serviceProvider.GetRequiredService<IBusControl>();

                // Start the bus
                BusControl.Start();

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


        private void ConfigureSerialization(IBusFactoryConfigurator configurator)
        {
            switch (SerializerType)
            {
                case MassTransitSerializerType.Json:
                    configurator.UseJsonSerializer(); // JSON is the default in MassTransit
                    break;

                case MassTransitSerializerType.Xml:
                    configurator.UseXmlSerializer(); // XML serialization
                    break;

                case MassTransitSerializerType.Binary:
                    configurator.UseRawJsonSerializer(); // MassTransit does not directly support Binary; use Raw JSON
                    break;

                default:
                    Logger?.WriteLog($"Serialization type {SerializerType} not implemented. Defaulting to JSON.");
                    configurator.UseJsonSerializer();
                    break;
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddMassTransit(config =>
            {
                switch (TransportType)
                {
                    case MassTransitTransportType.RabbitMQ:
                        ConfigureRabbitMqBus(config);
                        break;
                    case MassTransitTransportType.AzureServiceBus:
                        ConfigureAzureServiceBus(config);
                        break;
                    case MassTransitTransportType.AmazonSQS:
                        ConfigureAmazonSqsBus(config);
                        break;
                    case MassTransitTransportType.ActiveMQ:
                        ConfigureActiveMqBus(config);
                        break;
                    case MassTransitTransportType.Kafka:
                        ConfigureKafkaBus(config);
                        break;
                    case MassTransitTransportType.SQLDB:
                        ConfigureSqlDbBus(config);
                        break;
                    case MassTransitTransportType.AzureEventHb:
                        ConfigureAzureEventHubBus(config);
                        break;
                    case MassTransitTransportType.AzureFunctions:
                        ConfigureAzureFunctionsBus(config);
                        break;
                    case MassTransitTransportType.AWSLambda:
                        ConfigureAwsLambdaBus(config);
                        break;
                    default:
                        throw new NotSupportedException($"Transport type {TransportType} is not supported.");
                }
            });

            services.AddMassTransitHostedService();
        }

        private void ConfigureRabbitMqBus(IBusRegistrationConfigurator config)
        {
            config.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(Dataconnection.ConnectionProp.Host, h =>
                {
                    h.Username(Dataconnection.ConnectionProp.UserID);
                    h.Password(Dataconnection.ConnectionProp.Password);
                });

                foreach (var streamConfig in StreamConfigs.Values)
                {
                    cfg.ReceiveEndpoint(streamConfig.EntityName, ep =>
                    {
                        ep.ConfigureConsumer<GenericConsumer>(context);
                    });
                }
            });
        }

        private void ConfigureAzureServiceBus(IBusRegistrationConfigurator config)
        {
            config.UsingAzureServiceBus((context, cfg) =>
            {
                cfg.Host(Dataconnection.ConnectionProp.ConnectionString);

                foreach (var streamConfig in StreamConfigs.Values)
                {
                    cfg.ReceiveEndpoint(streamConfig.EntityName, ep =>
                    {
                        ep.ConfigureConsumer<GenericConsumer>(context);
                    });
                }
            });
        }

        private void ConfigureAmazonSqsBus(IBusRegistrationConfigurator config)
        {
            config.UsingAmazonSqs((context, cfg) =>
            {
                cfg.Host(Dataconnection.ConnectionProp.Host, h =>
                {
                    h.AccessKey(Dataconnection.ConnectionProp.UserID);
                    h.SecretKey(Dataconnection.ConnectionProp.Password);
                });

                foreach (var streamConfig in StreamConfigs.Values)
                {
                    cfg.ReceiveEndpoint(streamConfig.EntityName, ep =>
                    {
                        ep.ConfigureConsumer<GenericConsumer>(context);
                    });
                }
            });
        }

        private void ConfigureActiveMqBus(IBusRegistrationConfigurator config)
        {
            config.UsingActiveMq((context, cfg) =>
            {
                cfg.Host(new Uri(Dataconnection.ConnectionProp.Host), h =>
                {
                    h.Username(Dataconnection.ConnectionProp.UserID);
                    h.Password(Dataconnection.ConnectionProp.Password);
                });

                foreach (var streamConfig in StreamConfigs.Values)
                {
                    cfg.ReceiveEndpoint(streamConfig.EntityName, ep =>
                    {
                        ep.ConfigureConsumer<GenericConsumer>(context);
                    });
                }
            });
        }

        private void ConfigureKafkaBus(IBusRegistrationConfigurator config)
        {
            config.AddRider(rider =>
            {
                rider.UsingKafka((context, kafkaCfg) =>
                {
                    kafkaCfg.Host(Dataconnection.ConnectionProp.Host);
                    foreach (var streamConfig in StreamConfigs.Values)
                    {
                        kafkaCfg.TopicEndpoint<KafkaMessage>(
                            streamConfig.EntityName, streamConfig.ConsumerType, ep =>
                            {
                                ep.ConfigureConsumer<KafkaMessageConsumer>(context);
                            });
                    }
                });
            });
            config.UsingInMemory();
        }

        private void ConfigureSqlDbBus(IBusRegistrationConfigurator config)
        {
            config.AddSqlMessageScheduler();
            //config.UsingInMemory((context, cfg) =>
            //{
            //    cfg.UseInMemoryOutbox();
            //    cfg.TransportConcurrencyLimit = 1;

            //    foreach (var streamConfig in StreamConfigs.Values)
            //    {
            //        cfg.ReceiveEndpoint(streamConfig.EntityName, ep =>
            //        {
            //            ep.ConfigureConsumer<GenericConsumer>(context);
            //        });
            //    }
            //});
        }

        private void ConfigureAzureEventHubBus(IBusRegistrationConfigurator config)
        {
            //config.AddRider(rider =>
            //{
            //    rider.UsingAzureEventHub((context, eventHubCfg) =>
            //    {
            //        eventHubCfg.Host(Dataconnection.ConnectionProp.ConnectionString);

            //        foreach (var streamConfig in StreamConfigs.Values)
            //        {
            //            eventHubCfg.EventHubEndpoint<KafkaMessage>(
            //                streamConfig.EntityName, ep =>
            //                {
            //                    ep.ConfigureConsumer<KafkaMessageConsumer>(context);
            //                });
            //        }
            //    });
            //});
            //config.UsingInMemory((context, cfg) =>
            //{
               
            //});
        }

        private void ConfigureAzureFunctionsBus(IBusRegistrationConfigurator config)
        {
            config.UsingAzureServiceBus((context, cfg) =>
            {
                cfg.Host(Dataconnection.ConnectionProp.ConnectionString);

                foreach (var streamConfig in StreamConfigs.Values)
                {
                    cfg.ReceiveEndpoint(streamConfig.EntityName, ep =>
                    {
                        ep.ConfigureConsumer<GenericConsumer>(context);
                    });
                }
            });
        }

        private void ConfigureAwsLambdaBus(IBusRegistrationConfigurator config)
        {
            config.UsingAmazonSqs((context, cfg) =>
            {
                cfg.Host(Dataconnection.ConnectionProp.Host, h =>
                {
                    h.AccessKey(Dataconnection.ConnectionProp.UserID);
                    h.SecretKey(Dataconnection.ConnectionProp.Password);
                });

                foreach (var streamConfig in StreamConfigs.Values)
                {
                    cfg.ReceiveEndpoint(streamConfig.EntityName, ep =>
                    {
                        ep.ConfigureConsumer<GenericConsumer>(context);
                    });
                }
            });
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
        public object GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            throw new NotImplementedException();
        }

        public Task<object> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            throw new NotImplementedException();
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
                            new EntityField { fieldname = "MessageId", fieldtype = "System.Guid" },
                            new EntityField { fieldname = "Data", fieldtype = "System.String" }
                        }
                    };
                    Entities.Add(entity);
                }
            }

            return Entities.FirstOrDefault(e => e.EntityName == EntityName);
        }

        public object GetEntity(string EntityName, List<AppFilter> filter)
        {
            if (!QueueData.ContainsKey(EntityName))
            {
                QueueData[EntityName] = new List<GenericMessage>();
            }

            return new ObservableBindingList<GenericMessage>(QueueData[EntityName]);
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
                    throw new InvalidOperationException("Bus is not connected. Call Openconnection() before producing messages.");
                }

                if (!StreamConfigs.TryGetValue(EntityName, out StreamConfig config))
                {
                    throw new KeyNotFoundException($"Stream configuration for entity '{EntityName}' not found.");
                }

                var message = new GenericMessage
                {
                    EntityName = EntityName,
                    Data = InsertedData as Dictionary<string, object>
                           ?? InsertedData.GetType()
                                           .GetProperties()
                                           .ToDictionary(
                                               prop => prop.Name,
                                               prop => prop.GetValue(InsertedData))
                };

                var sendEndpoint = BusControl.GetSendEndpoint(new Uri($"{Dataconnection.ConnectionProp.Host}/{config.EntityName}")).Result;
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
