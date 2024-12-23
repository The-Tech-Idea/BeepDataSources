using MassTransit;
using MassTransit.KafkaIntegration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Data;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Messaging;
using TheTechIdea.Beep.Utilities;

namespace MassTransitDataSourceCore
{
    public class MassTransitDataConnection : IDataConnection
    {
        private IBusControl _busControl;
        private readonly MassTransitTransportType _transportType;
        private readonly MassTransitSerializerType _serializerType;
        public IServiceCollection Services { get; set; }
        public MassTransitDataConnection(IServiceCollection services,IDMEEditor dMEEditor, MassTransitTransportType transportType, MassTransitSerializerType serializerType)
        {
            DMEEditor = dMEEditor ?? throw new ArgumentNullException(nameof(dMEEditor));
            _transportType = transportType;
            _serializerType = serializerType;
            Services = services;
        }

        public IConnectionProperties ConnectionProp { get; set; }
        public ConnectionDriversConfig DataSourceDriver { get; set; }
        public ConnectionState ConnectionStatus { get;  set; } = ConnectionState.Closed;
        public IDMEEditor DMEEditor { get; set; }
        public int ID { get; set; }
        public string GuidID { get; set; }
        public IDMLogger Logger { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public bool InMemory { get; set; }

        public ConnectionState OpenConnection()
        {
            try
            {
                _busControl = ConfigureTransport();
                _busControl.Start();
                ConnectionStatus = ConnectionState.Open;
                Logger?.WriteLog("[OpenConnection] Connection established successfully.");
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Closed;
                Logger?.WriteLog($"[OpenConnection] Failed to open connection: {ex.Message}");
                throw;
            }

            return ConnectionStatus;
        }

        public ConnectionState CloseConn()
        {
            try
            {
                _busControl?.Stop();
                ConnectionStatus = ConnectionState.Closed;
                Logger?.WriteLog("[CloseConn] Connection closed successfully.");
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"[CloseConn] Failed to close connection: {ex.Message}");
                throw;
            }

            return ConnectionStatus;
        }

        public ConnectionState OpenConnection(DataSourceType dbtype, string host, int port, string database, string userid, string password, string parameters)
        {
            ConnectionProp = new ConnectionProperties
            {
                Host = host,
                Database = database,
                Port = port,
                UserID = userid,
                Password = password,
                Parameters = parameters
            };
            return OpenConnection();
        }

        public ConnectionState OpenConnection(DataSourceType dbtype, string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            ConnectionProp = ParseConnectionString(connectionString);
            return OpenConnection();
        }

        public string ReplaceValueFromConnectionString()
        {
            return ConnectionProp?.ConnectionString ?? string.Empty;
        }

        private IBusControl ConfigureTransport()
        {
            Services.AddMassTransit(x =>
            {

                if (_transportType == MassTransitTransportType.Kafka)
                {
                    x.UsingInMemory();
                    x.AddRider(rider =>
                    {
                        rider.UsingKafka((context, k) =>
                        {
                            k.Host($"{ConnectionProp.Host}:{ConnectionProp.Port}");
                        });
                    });

                }
                if (_transportType == MassTransitTransportType.RabbitMQ)
                {
                   
                        x.UsingRabbitMq((context, cfg) =>
                        {
                            cfg.Host($"{ConnectionProp.Host}", "/", h =>
                            {
                                h.Username($"{ConnectionProp.UserID}");
                                h.Password($"{ConnectionProp.Password}");
                            });
                        });
                   
                }
                if (_transportType == MassTransitTransportType.AzureServiceBus)
                {
                    x.UsingAzureServiceBus((context, cfg) =>
                    {
                        cfg.Host(ConnectionProp.ConnectionString);
                    });
                }
                if (_transportType == MassTransitTransportType.ActiveMQ)
                {
                    x.UsingActiveMq((context, cfg) =>
                    {
                        cfg.Host($"{ConnectionProp.Host}", ConnectionProp.Port
                    ,h =>
                        {
                            h.UseSsl();

                            h.Username($"{ConnectionProp.UserID}");
                            h.Password($"{ConnectionProp.Password}");
                        });
                    });
                }
                if (_transportType == MassTransitTransportType.AmazonSQS)
                {
                    x.UsingAmazonSqs((context, cfg) =>
                    {
                        cfg.Host($"{ConnectionProp.Host}", h =>
                        {
                            h.AccessKey($"{ConnectionProp.KeyToken}");
                            h.SecretKey($"{ConnectionProp.ApiKey}");
                        });
                    });
                }
                if(_transportType == MassTransitTransportType.SQLDB)
                {
                    x.UsingSqlServer((context, cfg) =>
                    {
                        cfg.Host(ConnectionProp.Host, h =>
                        {
                            h.Database(ConnectionProp.Database);
                            h.Username(ConnectionProp.UserID);
                            h.Password(ConnectionProp.Password);
                        });
                    });
                }
                if(_transportType == MassTransitTransportType.AzureEventHb)
                {
                    x.UsingAzureServiceBus((context, cfg) =>
                    {
                        cfg.Host(ConnectionProp.ConnectionString);
                    });
                }
            });

        }

        private IBusControl ConfigureKafkaTransport()
        {
            var services = new ServiceCollection();

            services.AddMassTransit(x =>
            {
                x.AddRider(rider =>
                {
                    rider.AddConsumer<MyConsumer>();

                    rider.UsingKafka((context, kafka) =>
                    {
                        kafka.Host(ConnectionProp.Host);

                        kafka.TopicEndpoint<string>("example-topic", "example-group", e =>
                        {
                            e.ConfigureConsumer<MyConsumer>(context);
                        });
                    });
                });
            });

            var provider = services.BuildServiceProvider();
            return provider.GetRequiredService<IBusControl>();
        }
        private ConnectionProperties ParseConnectionString(string connectionString)
        {
            var properties = new ConnectionProperties();
            // Implement connection string parsing logic here
            return properties;
        }
        private class MyConsumer : IConsumer<string>
        {
            public Task Consume(ConsumeContext<string> context)
            {
                // Add consumer logic here
                return Task.CompletedTask;
            }
        }
    }
}
