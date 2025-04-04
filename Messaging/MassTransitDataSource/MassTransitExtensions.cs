using Confluent.Kafka;
using MassTransit;
using MassTransit.KafkaIntegration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.MassTransitDataSourceCore;




namespace MassTransitDataSourceCore
{

        public static class MassTransitExtensions
        {
        public static IServiceCollection AddMassTransitServices(
             this IServiceCollection services,
             MassTransitConfiguration config,
             IServiceProvider serviceProvider)
        {
            // 1) Register consumers dynamically for each stream.
            services.AddMassTransit(x =>
            {
                foreach (var streamConfig in config.StreamConfigs.Values)
                {
                    // Expecting MessageType to be a fully qualified type name in streamConfig.MessageType
                    var messageType = Type.GetType(streamConfig.MessageType);
                    if (messageType == null)
                        throw new Exception($"Could not load type: {streamConfig.MessageType}");

                    // Construct GenericConsumer<T> where T is the message type.
                    var consumerType = typeof(GenericConsumer<>).MakeGenericType(messageType);
                    // Register the consumer type so MassTransit knows about it in the container.
                    x.AddConsumer(consumerType);
                }

                // 2) Configure the bus based on the selected transport.
                switch (config.TransportType)
                {
                    case MassTransitTransportType.RabbitMQ:
                        x.UsingRabbitMq((context, cfg) =>
                        {
                            cfg.Host(config.Dataconnection.ConnectionProp.Host, h =>
                            {
                                h.Username(config.Dataconnection.ConnectionProp.UserID);
                                h.Password(config.Dataconnection.ConnectionProp.Password);
                            });

                            // Create a receive endpoint for each stream.
                            foreach (var streamConfig in config.StreamConfigs.Values)
                            {
                                var messageType = Type.GetType(streamConfig.MessageType);
                                if (messageType == null)
                                    throw new Exception($"Could not load type: {streamConfig.MessageType}");
                                var consumerType = typeof(GenericConsumer<>).MakeGenericType(messageType);

                                cfg.ReceiveEndpoint(streamConfig.EntityName, ep =>
                                {
                                    // 3) Use ep.Consumer(Type consumerType, Func<Type, object> consumerFactory)
                                    ep.Consumer(consumerType, type =>
                                    {
                                        // The simplest approach is to get the consumer from the provided serviceProvider:
                                        return serviceProvider.GetService(type);
                                    });
                                });
                            }

                            ConfigureSerialization(cfg, config);
                        });
                        break;

                    case MassTransitTransportType.Kafka:
                        x.AddRider(rider =>
                        {
                            rider.UsingKafka((context, kafkaCfg) =>
                            {
                                kafkaCfg.Host(config.Dataconnection.ConnectionProp.Host);
                                foreach (var streamConfig in config.StreamConfigs.Values)
                                {
                                    var messageType = Type.GetType(streamConfig.MessageType);
                                    if (messageType == null)
                                        throw new Exception($"Could not load type: {streamConfig.MessageType}");
                                    var consumerType = typeof(GenericConsumer<>).MakeGenericType(messageType);

                                    // 4) Kafka requires TKey/TValue. 
                                    // If you don't care about the key, you can use Ignore or Null.
                                    // We'll assume "Ignore" from Confluent.Kafka here.
                                    kafkaCfg.TopicEndpoint<Ignore, object>(
                                        streamConfig.EntityName,
                                        streamConfig.ConsumerType, // the consumer group name
                                        ep =>
                                        {
                                            // Now for the consumer factory:
                                            ep.Consumer(consumerType, type =>
                                            {
                                                return serviceProvider.GetService(type);
                                            });
                                        });
                                }
                            });
                        });

                        // If you're combining Kafka with an in-memory transport:
                        x.UsingInMemory();
                        break;

                        // ... handle other transports similarly, 
                        // using ep.Consumer(consumerType, type => serviceProvider.GetService(type))
                        // or a similar factory approach
                }
            });

           
            return services;
        }

        private static void ConfigureSerialization(IBusFactoryConfigurator configurator, MassTransitConfiguration config)
        {
            switch (config.SerializerType)
            {
                case MassTransitSerializerType.Json:
                    configurator.UseJsonSerializer(); // JSON is the default serializer.
                    break;
                case MassTransitSerializerType.Xml:
                    configurator.UseXmlSerializer(); // XML serialization.
                    break;
                case MassTransitSerializerType.Binary:
                    configurator.UseRawJsonSerializer(); // Using raw JSON as a binary alternative.
                    break;
                default:
                    config.Logger?.WriteLog($"Serialization type {config.SerializerType} not implemented. Defaulting to JSON.");
                    configurator.UseJsonSerializer();
                    break;
            }
        }
    }
}
