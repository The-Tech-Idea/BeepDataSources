# Messaging Data Sources

## Overview

The Messaging data sources category provides integration with message queue and event streaming platforms, enabling publish/subscribe messaging, event-driven architectures, and asynchronous communication patterns. All messaging data sources implement both `IDataSource` and `IMessageDataSource<GenericMessage, StreamConfig>` interfaces for consistency and framework integration.

## Architecture

- **Base Interfaces**: 
  - `IDataSource` - Standard data source interface
  - `IMessageDataSource<GenericMessage, StreamConfig>` - Messaging-specific interface
- **Common Models**: 
  - `GenericMessage` - Standard message wrapper with payload and metadata
  - `StreamConfig` - Stream/queue configuration
  - `GenericConsumer<T>` - Generic consumer interface
  - `GenericProducer<T>` - Generic producer interface
- **CommandAttribute**: Public methods decorated with `CommandAttribute` for framework discovery

## Standard Interface: IMessageDataSource

All messaging data sources implement `IMessageDataSource<GenericMessage, StreamConfig>` from `TheTechIdea.Beep.Messaging` namespace:

```csharp
public interface IMessageDataSource<TMessage, TConfig>
    where TMessage : class
    where TConfig : class
{
    void Initialize(TConfig config);
    Task SendMessageAsync(string streamName, TMessage message, CancellationToken cancellationToken);
    Task SubscribeAsync(string streamName, Func<TMessage, Task> onMessageReceived, CancellationToken cancellationToken);
    Task AcknowledgeMessageAsync(string streamName, TMessage message, CancellationToken cancellationToken);
    Task<TMessage> PeekMessageAsync(string streamName, CancellationToken cancellationToken);
    Task<object> GetStreamMetadataAsync(string streamName, CancellationToken cancellationToken);
    void Disconnect();
}
```

## Data Sources

### MassTransit (`MassTransitDataSource`)

**Base Interfaces**: `IDataSource`, `IMessageDataSource<GenericMessage, StreamConfig>` (to be implemented)  
**Transports Supported**: RabbitMQ, Kafka, Azure Service Bus, ActiveMQ, Amazon SQS, Azure Event Hub  
**Serialization**: JSON, XML, Binary

#### Current Status
- ✅ Implements `IDataSource`
- ⚠️ **Does NOT implement `IMessageDataSource<GenericMessage, StreamConfig>`** (inconsistency)
- ✅ Supports multiple transports via MassTransit
- ✅ Dynamic type loading for messages
- ✅ Channel-based message buffering

#### CommandAttribute Methods (To Be Added)
- `SendMessage` - Send message to stream
- `SubscribeToStream` - Subscribe to message stream
- `GetStreamMessages` - Get messages from stream
- `GetStreamMetadata` - Get stream statistics
- `AcknowledgeMessage` - Acknowledge message processing

#### Required Interface Implementation
```csharp
// Current signature - needs adjustment
public async Task SendMessageAsync(string streamName, object message, CancellationToken cancellationToken)

// Required signature
public async Task SendMessageAsync(string streamName, GenericMessage message, CancellationToken cancellationToken)

// Missing methods to implement:
- Task AcknowledgeMessageAsync(string streamName, GenericMessage message, CancellationToken cancellationToken)
- Task<GenericMessage> PeekMessageAsync(string streamName, CancellationToken cancellationToken)
- Task<object> GetStreamMetadataAsync(string streamName, CancellationToken cancellationToken)
```

#### Configuration
```csharp
var props = new ConnectionProperties
{
    Host = "rabbitmq://localhost",
    UserID = "guest",
    Password = "guest",
    Port = 5672
};

// Configure transport type
var transportType = MassTransitTransportType.RabbitMQ; // or Kafka, AzureServiceBus, etc.
var serializerType = MassTransitSerializerType.Json;
```

---

### Kafka (`KafkaDataSource`)

**Base Interfaces**: `IDataSource`, `IMessageDataSource<GenericMessage, StreamConfig>`  
**API**: Confluent.Kafka  
**Features**: Topic-based messaging, partitioning, consumer groups

#### CommandAttribute Methods
- Message sending and receiving
- Topic management
- Consumer group operations
- Metadata retrieval

#### Configuration
```csharp
var props = new ConnectionProperties
{
    Host = "localhost:9092",
    Database = "default" // Consumer group
};
```

---

### RabbitMQ (`RabbitMQDataSource`)

**Base Interfaces**: `IDataSource`, `IMessageDataSource<GenericMessage, StreamConfig>`  
**API**: RabbitMQ.Client  
**Features**: Queue-based messaging, exchanges, routing

#### CommandAttribute Methods
- Queue operations
- Exchange management
- Message routing
- Acknowledgment handling

#### Configuration
```csharp
var props = new ConnectionProperties
{
    Host = "localhost",
    UserID = "guest",
    Password = "guest",
    Port = 5672
};
```

---

## Common Patterns

### Message Structure (GenericMessage)

All messaging data sources use `GenericMessage`:

```csharp
public class GenericMessage
{
    public string EntityName { get; set; }      // Stream/queue name
    public object Payload { get; set; }         // Message data
    public Dictionary<string, string> Metadata { get; set; }  // Headers/metadata
    public DateTime Timestamp { get; set; }     // Message timestamp
    public string MessageId { get; set; }       // Unique message ID
    public int? Priority { get; set; }          // Message priority
    public ulong? DeliveryTag { get; set; }     // For acknowledgment (RabbitMQ)
}
```

### Stream Configuration (StreamConfig)

```csharp
public class StreamConfig
{
    public string EntityName { get; set; }           // Stream/queue/topic name
    public string MessageType { get; set; }          // Fully qualified type name
    public string ConsumerType { get; set; }         // Consumer group/type
    public string MessageCategory { get; set; }       // Command, Event, Request, Response
    public string ExchangeType { get; set; }         // For RabbitMQ
    public string PartitionKey { get; set; }         // For Kafka
    public string RetentionPolicy { get; set; }       // Retention settings
    public Dictionary<string, object> AdditionalOptions { get; set; }
}
```

### CommandAttribute Structure

All messaging connectors use the `CommandAttribute` pattern:

```csharp
[CommandAttribute(
    Name = "MethodName",
    Caption = "User-Friendly Description",
    Category = DatasourceCategory.MessageQueue,
    DatasourceType = DataSourceType.MessagingPlatform,
    PointType = EnumPointType.Function,
    ObjectType = "GenericMessage",
    ClassType = "MessagingDataSource",
    Showin = ShowinType.Both,
    Order = 1,
    misc = "ReturnType: Task<IErrorsInfo>"
)]
public async Task<IErrorsInfo> MethodName(...)
{
    // Implementation
}
```

## Implementation Consistency

### Required Interface Compliance

All messaging data sources **MUST** implement:

1. ✅ `IDataSource` - Standard data source operations
2. ✅ `IMessageDataSource<GenericMessage, StreamConfig>` - Messaging operations
3. ✅ Use `GenericMessage` for all message operations
4. ✅ Use `StreamConfig` for stream configuration
5. ✅ Implement all interface methods (no NotImplementedException)

### Current Status

| Data Source | IDataSource | IMessageDataSource | Status |
|-------------|------------|-------------------|--------|
| KafkaDataSource | ✅ | ✅ | Complete |
| RabbitMQDataSource | ✅ | ✅ | Complete |
| MassTransitDataSource | ✅ | ❌ | **Needs Implementation** |

## Best Practices

1. **Message Wrapping**: Always wrap messages in `GenericMessage` for consistency
2. **Error Handling**: Implement retry logic and dead letter queues
3. **Acknowledgment**: Properly acknowledge messages after processing
4. **Metadata**: Use `GenericMessage.Metadata` for headers and routing
5. **Type Safety**: Validate message types match `StreamConfig.MessageType`
6. **Resource Management**: Properly dispose consumers and producers
7. **Connection Management**: Validate connections before operations

## Configuration Examples

### MassTransit with RabbitMQ
```csharp
var config = new StreamConfig
{
    EntityName = "orders",
    MessageType = typeof(OrderMessage).AssemblyQualifiedName,
    ConsumerType = "order-processor",
    ExchangeType = "direct"
};

var dataSource = new MassTransitDataSource("RabbitMQ", logger, editor, DataSourceType.MassTransit, errors);
dataSource.TransportType = MassTransitTransportType.RabbitMQ;
dataSource.Initialize(config);
```

### MassTransit with Kafka
```csharp
var config = new StreamConfig
{
    EntityName = "events",
    MessageType = typeof(EventMessage).AssemblyQualifiedName,
    ConsumerType = "event-consumer-group",
    PartitionKey = "event-type"
};

var dataSource = new MassTransitDataSource("Kafka", logger, editor, DataSourceType.MassTransit, errors);
dataSource.TransportType = MassTransitTransportType.Kafka;
dataSource.Initialize(config);
```

## Enhancement Priorities

### High Priority
1. **Implement IMessageDataSource Interface** - MassTransitDataSource must implement the standard interface
2. **Adjust Method Signatures** - Change `object` to `GenericMessage` in SendMessageAsync
3. **Add Missing Methods** - Implement AcknowledgeMessageAsync, PeekMessageAsync, GetStreamMetadataAsync
4. **Add CommandAttribute Methods** - Enable framework discovery

### Medium Priority
5. Enhanced error handling and retry logic
6. Connection validation
7. Batch operations support

### Low Priority
8. Metrics and monitoring
9. Health checks
10. Advanced routing features

## Status

- **KafkaDataSource**: ✅ Complete and compliant
- **RabbitMQDataSource**: ✅ Complete and compliant  
- **MassTransitDataSource**: ⚠️ **Needs IMessageDataSource implementation** (see ENHANCEMENT_PLAN.md)

## Reference

- **Interface Definition**: `TheTechIdea.Beep.Messaging.IMessageDataSource<TMessage, TConfig>`
- **Message Model**: `TheTechIdea.Beep.Messaging.GenericMessage`
- **Config Model**: `TheTechIdea.Beep.Messaging.StreamConfig`
- **Enhancement Plan**: See `MassTransitDataSource/ENHANCEMENT_PLAN.md`

