using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.MassTransitDataSourceCore
{
    public static class MassTransitServiceCollectionExtensions
    {
        public static IServiceCollection AddMassTransitTransport(
            this IServiceCollection services,
            MassTransitTransportType transportType,
            ConnectionProperties connectionProp)
        {
            services.AddMassTransit(x =>
            {
                if (transportType == MassTransitTransportType.Kafka)
                {
                    x.UsingInMemory(); // In-memory bus used in conjunction with the Kafka rider.
                    x.AddRider(rider =>
                    {
                        rider.UsingKafka((context, k) =>
                        {
                            k.Host($"{connectionProp.Host}:{connectionProp.Port}");
                        });
                    });
                }
                else if (transportType == MassTransitTransportType.RabbitMQ)
                {
                    x.UsingRabbitMq((context, cfg) =>
                    {
                        cfg.Host($"{connectionProp.Host}", "/", h =>
                        {
                            h.Username(connectionProp.UserID);
                            h.Password(connectionProp.Password);
                        });
                    });
                }
                else if (transportType == MassTransitTransportType.AzureServiceBus)
                {
                    x.UsingAzureServiceBus((context, cfg) =>
                    {
                        cfg.Host(connectionProp.ConnectionString);
                    });
                }
                else if (transportType == MassTransitTransportType.ActiveMQ)
                {
                    x.UsingActiveMq((context, cfg) =>
                    {
                        cfg.Host($"{connectionProp.Host}", connectionProp.Port, h =>
                        {
                            h.UseSsl();
                            h.Username(connectionProp.UserID);
                            h.Password(connectionProp.Password);
                        });
                    });
                }
                else if (transportType == MassTransitTransportType.AmazonSQS)
                {
                    x.UsingAmazonSqs((context, cfg) =>
                    {
                        cfg.Host($"{connectionProp.Host}", h =>
                        {
                            h.AccessKey(connectionProp.KeyToken);
                            h.SecretKey(connectionProp.ApiKey);
                        });
                    });
                }
                else if (transportType == MassTransitTransportType.AzureEventHb)
                {
                    x.UsingAzureServiceBus((context, cfg) =>
                    {
                        cfg.Host(connectionProp.ConnectionString);
                    });
                }
                // Note: The SQLDB branch has been removed because MassTransit does not provide native support for SQL Server as a transport.
            });

            return services;
        }
    }
}
