using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.MassTransitDataSourceCore;
using TheTechIdea.Beep.Messaging;

namespace MassTransitDataSourceCore
{
    public class MassTransitConfiguration
    {
        public MassTransitTransportType TransportType { get; set; }
        public MassTransitSerializerType SerializerType { get; set; }
        public IDataConnection Dataconnection { get; set; }
        public IDictionary<string, StreamConfig> StreamConfigs { get; set; }
        public IDMLogger Logger { get; set; }
    }
    // Top-level class implementing IConsumer<T>
    public class GenericConsumer<T> : IConsumer<T> where T : class
    {
        // A parameterless constructor is needed for DI instantiation.
        public GenericConsumer()
        {
        }

        public async Task Consume(ConsumeContext<T> context)
        {
            // Default behavior: simply log the received message.
            Console.WriteLine($"Received message of type {typeof(T).Name}: {context.Message}");
            await Task.CompletedTask;
        }
    }
}
