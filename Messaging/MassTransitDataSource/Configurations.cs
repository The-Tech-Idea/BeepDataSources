using MassTransit;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MassTransitDataSourceCore
{
  

    public enum MassTransitMessageType
    {
        Command,
        Event,
        Request,
        Response
    }
    public enum MassTransitTransportType
    {
        RabbitMQ,
        AzureServiceBus,
        AmazonSQS,
        ActiveMQ,
        Kafka,
        SQLDB,
        AzureEventHb,
        AzureFunctions,
        AWSLambda,
    }
    public enum MassTransitSerializerType
    {
        Json,
        Xml,
        Binary,
        Protobuf,
        MessagePack
    }
    public enum MassTransitTransportMode
    {
        Client,
        Server
    }
}
