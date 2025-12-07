# MassTransit Data Source - Enhancement Plan

## Executive Summary

This document provides a comprehensive analysis of the MassTransit Data Source implementation and outlines an enhancement plan to improve functionality, consistency, reliability, and maintainability.

## Current Implementation Analysis

### Strengths ✅

1. **Transport Support**: Supports multiple transports (RabbitMQ, Kafka, Azure Service Bus, ActiveMQ, Amazon SQS)
2. **Dynamic Type Loading**: Flexible message type handling via reflection
3. **Stream Configuration**: Centralized stream/entity configuration management
4. **Channel-Based Storage**: Uses modern `Channel<object>` for in-memory message buffering
5. **Service Provider Integration**: Properly integrates with Microsoft.Extensions.DependencyInjection
6. **Extension Methods**: Clean extension methods for service registration

### Issues & Gaps ❌

1. **Interface Inconsistency**: Doesn't implement `IMessageDataSource<GenericMessage, StreamConfig>` (Kafka/RabbitMQ do)
2. **Incomplete Implementation**: Many methods throw `NotImplementedException`
3. **Missing CommandAttribute**: No framework discovery via CommandAttribute pattern
4. **Error Handling**: Limited error recovery and retry logic
5. **Connection Management**: No async connection methods, limited validation
6. **Type Safety**: Dynamic type loading can fail silently
7. **Resource Management**: Incomplete Dispose pattern
8. **Documentation**: Limited XML documentation
9. **Testing**: No clear testing strategy
10. **Configuration Validation**: Missing validation for connection properties

---

## Enhancement Plan

### Phase 1: Foundation & Consistency (Priority: High)

#### 1.1 Implement IMessageDataSource Interface

**Current State**: Implements only `IDataSource`  
**Target State**: Implement both `IDataSource` and `IMessageDataSource<GenericMessage, StreamConfig>`

**Note**: `IMessageDataSource<TMessage, TConfig>` already exists in `TheTechIdea.Beep.Messaging` namespace. This is the standard interface used by KafkaDataSource and RabbitMQDataSource. MassTransitDataSource should implement it for consistency.

**Interface Definition** (from `TheTechIdea.Beep.Messaging.IMessageDataSource`):
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

**Changes Required**:
```csharp
public class MassTransitDataSource : IDataSource, IMessageDataSource<GenericMessage, StreamConfig>
{
    // ✅ Already implemented:
    // - void Initialize(StreamConfig config) - EXISTS but needs to match signature
    // - void Disconnect() - EXISTS
    
    // ❌ Needs implementation/adjustment:
    // - Task SendMessageAsync(string, GenericMessage, CancellationToken) 
    //   CURRENT: SendMessageAsync(string, object, CancellationToken) - needs to accept GenericMessage
    // - Task SubscribeAsync(string, Func<GenericMessage, Task>, CancellationToken)
    //   CURRENT: SubscribeAsync(string, CancellationToken) - needs callback parameter
    // - Task AcknowledgeMessageAsync(string, GenericMessage, CancellationToken) - MISSING
    // - Task<GenericMessage> PeekMessageAsync(string, CancellationToken) - MISSING
    // - Task<object> GetStreamMetadataAsync(string, CancellationToken) - MISSING
}
```

**Benefits**:
- ✅ Consistency with KafkaDataSource and RabbitMQDataSource
- ✅ Better interface contract
- ✅ Enables polymorphic usage
- ✅ Standard messaging operations across all messaging data sources
- ✅ Framework can treat all messaging sources uniformly

**Implementation Notes**:
- The interface is already defined in `TheTechIdea.Beep.Messaging` namespace
- No need to create a new interface - use the existing standard
- MassTransitDataSource methods need signature adjustments to match interface
- GenericMessage and StreamConfig are already defined in the same namespace

---

#### 1.2 Complete NotImplementedException Methods

**Methods to Implement**:

1. **GetEntitesList()** - Return list of configured stream names
   ```csharp
   public List<string> GetEntitesList()
   {
       return StreamConfigs.Keys.ToList();
   }
   ```

2. **CheckEntityExist(string EntityName)** - Verify stream configuration exists
   ```csharp
   public bool CheckEntityExist(string EntityName)
   {
       return StreamConfigs.ContainsKey(EntityName);
   }
   ```

3. **GetEntityType(string EntityName)** - Return message type for stream
   ```csharp
   public Type GetEntityType(string EntityName)
   {
       if (!StreamConfigs.TryGetValue(EntityName, out var config))
           return null;
       return Type.GetType(config.MessageType);
   }
   ```

4. **GetEntityIdx(string entityName)** - Return index in entities list
   ```csharp
   public int GetEntityIdx(string entityName)
   {
       return EntitiesNames.IndexOf(entityName);
   }
   ```

5. **CreateEntityAs(EntityStructure entity)** - Create stream configuration from entity
   ```csharp
   public bool CreateEntityAs(EntityStructure entity)
   {
       if (entity == null || string.IsNullOrEmpty(entity.EntityName))
           return false;
       
       var config = new StreamConfig
       {
           EntityName = entity.EntityName,
           MessageType = entity.GetType().AssemblyQualifiedName
       };
       Initialize(config);
       return true;
   }
   ```

---

#### 1.3 Add CommandAttribute Methods

**Purpose**: Enable framework discovery of messaging operations

**Methods to Add**:
```csharp
[CommandAttribute(
    Name = "SendMessage",
    Caption = "Send Message to Stream",
    Category = DatasourceCategory.MessageQueue,
    DatasourceType = DataSourceType.MassTransit,
    PointType = EnumPointType.Function,
    ObjectType = "GenericMessage",
    ClassType = "MassTransitDataSource",
    Showin = ShowinType.Both,
    Order = 1,
    misc = "ReturnType: Task<IErrorsInfo>"
)]
public async Task<IErrorsInfo> SendMessage(string streamName, object message)
{
    // Implementation
}

[CommandAttribute(
    Name = "SubscribeToStream",
    Caption = "Subscribe to Message Stream",
    Category = DatasourceCategory.MessageQueue,
    DatasourceType = DataSourceType.MassTransit,
    PointType = EnumPointType.Function,
    ObjectType = "StreamConfig",
    ClassType = "MassTransitDataSource",
    Showin = ShowinType.Both,
    Order = 2,
    misc = "ReturnType: Task<IErrorsInfo>"
)]
public async Task<IErrorsInfo> SubscribeToStream(string streamName)
{
    // Implementation
}

[CommandAttribute(
    Name = "GetStreamMessages",
    Caption = "Get Messages from Stream",
    Category = DatasourceCategory.MessageQueue,
    DatasourceType = DataSourceType.MassTransit,
    PointType = EnumPointType.Function,
    ObjectType = "GenericMessage",
    ClassType = "MassTransitDataSource",
    Showin = ShowinType.Both,
    Order = 3,
    misc = "ReturnType: IEnumerable<object>"
)]
public IEnumerable<object> GetStreamMessages(string streamName, int maxCount = 100)
{
    // Implementation
}
```

---

### Phase 2: Reliability & Error Handling (Priority: High)

#### 2.1 Enhanced Connection Management

**Current Issues**:
- No connection validation before opening
- No retry logic
- Limited error recovery
- Missing async connection methods

**Enhancements**:

```csharp
public async Task<ConnectionState> OpenConnectionAsync()
{
    try
    {
        // Validate configuration
        var validationResult = ValidateConnectionConfiguration();
        if (!validationResult.IsValid)
        {
            ErrorObject = new ErrorsInfo
            {
                Flag = Errors.Failed,
                Message = string.Join("; ", validationResult.Errors)
            };
            ConnectionStatus = ConnectionState.Broken;
            return ConnectionStatus;
        }

        if (_busControl != null && _busControl.Address != null)
        {
            Logger?.WriteLog("Bus is already connected.");
            return ConnectionState.Open;
        }

        if (_services == null)
            throw new InvalidOperationException("Service provider not set.");

        _busControl = _services.GetRequiredService<IBusControl>();
        
        // Start with retry logic
        await StartBusWithRetryAsync();
        
        ConnectionStatus = ConnectionState.Open;
        Logger?.WriteLog("Bus connection opened successfully.");
        return ConnectionStatus;
    }
    catch (Exception ex)
    {
        Logger?.WriteLog($"Error opening connection: {ex.Message}");
        ErrorObject = new ErrorsInfo { Flag = Errors.Failed, Message = ex.Message, Ex = ex };
        ConnectionStatus = ConnectionState.Broken;
        return ConnectionStatus;
    }
}

private async Task StartBusWithRetryAsync(int maxRetries = 3)
{
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            await _busControl.StartAsync();
            return;
        }
        catch (Exception ex) when (i < maxRetries - 1)
        {
            Logger?.WriteLog($"Retry {i + 1}/{maxRetries}: {ex.Message}");
            await Task.Delay(1000 * (i + 1)); // Exponential backoff
        }
    }
    throw new InvalidOperationException("Failed to start bus after retries.");
}
```

---

#### 2.2 Configuration Validation

**Add Validation Method**:
```csharp
private (bool IsValid, List<string> Errors) ValidateConnectionConfiguration()
{
    var errors = new List<string>();
    
    if (Dataconnection?.ConnectionProp == null)
        errors.Add("Connection properties are not set");
    
    if (string.IsNullOrEmpty(Dataconnection?.ConnectionProp?.Host))
        errors.Add("Host is required");
    
    if (TransportType == MassTransitTransportType.RabbitMQ)
    {
        if (string.IsNullOrEmpty(Dataconnection?.ConnectionProp?.UserID))
            errors.Add("UserID is required for RabbitMQ");
        if (string.IsNullOrEmpty(Dataconnection?.ConnectionProp?.Password))
            errors.Add("Password is required for RabbitMQ");
    }
    
    if (TransportType == MassTransitTransportType.Kafka)
    {
        if (Dataconnection?.ConnectionProp?.Port <= 0)
            errors.Add("Port is required for Kafka");
    }
    
    return (errors.Count == 0, errors);
}
```

---

#### 2.3 Improved Error Handling

**Add Error Recovery**:
```csharp
private async Task<T> ExecuteWithRetryAsync<T>(
    Func<Task<T>> operation,
    int maxRetries = 3,
    TimeSpan? delay = null)
{
    delay ??= TimeSpan.FromSeconds(1);
    
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            return await operation();
        }
        catch (Exception ex) when (i < maxRetries - 1)
        {
            Logger?.WriteLog($"Operation failed (attempt {i + 1}/{maxRetries}): {ex.Message}");
            await Task.Delay(delay.Value * (i + 1));
        }
    }
    throw new InvalidOperationException("Operation failed after all retries.");
}
```

---

### Phase 3: Type Safety & Message Handling (Priority: Medium)

#### 3.1 Enhanced Type Loading

**Current Issue**: Type loading can fail silently

**Enhancement**:
```csharp
private Type LoadMessageType(string typeName)
{
    if (string.IsNullOrEmpty(typeName))
        throw new ArgumentException("Type name cannot be null or empty", nameof(typeName));
    
    var type = Type.GetType(typeName);
    if (type == null)
    {
        // Try loading from all loaded assemblies
        type = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => t.FullName == typeName || t.AssemblyQualifiedName == typeName);
    }
    
    if (type == null)
    {
        var error = $"Message type '{typeName}' not found. Ensure the assembly is loaded.";
        Logger?.WriteLog(error);
        throw new TypeLoadException(error);
    }
    
    return type;
}
```

---

#### 3.2 Message Validation

**Add Message Validation**:
```csharp
private bool ValidateMessage(object message, StreamConfig config)
{
    if (message == null)
    {
        Logger?.WriteLog("Message cannot be null");
        return false;
    }
    
    var expectedType = LoadMessageType(config.MessageType);
    if (!expectedType.IsInstanceOfType(message))
    {
        Logger?.WriteLog($"Message type {message.GetType().Name} does not match expected type {expectedType.Name}");
        return false;
    }
    
    return true;
}
```

---

#### 3.3 GenericMessage Factory

**Add Factory Method**:
```csharp
private GenericMessage CreateGenericMessage(string entityName, object payload)
{
    var message = new GenericMessage
    {
        EntityName = entityName,
        MessageId = Guid.NewGuid(),
        Timestamp = DateTimeOffset.UtcNow,
        Payload = payload is Dictionary<string, object> dict 
            ? dict 
            : payload.GetType().GetProperties()
                .ToDictionary(prop => prop.Name, prop => prop.GetValue(payload))
    };
    
    return message;
}
```

---

### Phase 4: Performance & Scalability (Priority: Medium)

#### 4.1 Channel Management Improvements

**Current Issue**: Unbounded channels can cause memory issues

**Enhancement**:
```csharp
private Channel<object> CreateChannel(StreamConfig config)
{
    var options = new BoundedChannelOptions(config.MaxChannelCapacity ?? 10000)
    {
        FullMode = BoundedChannelFullMode.Wait, // or DropOldest, DropWrite
        SingleReader = false,
        SingleWriter = false
    };
    
    return Channel.CreateBounded<object>(options);
}
```

**Add to StreamConfig**:
```csharp
public class StreamConfig
{
    // ... existing properties
    public int? MaxChannelCapacity { get; set; } = 10000;
    public BoundedChannelFullMode ChannelFullMode { get; set; } = BoundedChannelFullMode.Wait;
}
```

---

#### 4.2 Batch Operations

**Add Batch Send**:
```csharp
[CommandAttribute(
    Name = "SendBatchMessages",
    Caption = "Send Batch Messages",
    Category = DatasourceCategory.MessageQueue,
    DatasourceType = DataSourceType.MassTransit,
    PointType = EnumPointType.Function,
    ObjectType = "GenericMessage",
    ClassType = "MassTransitDataSource",
    Showin = ShowinType.Both,
    Order = 4,
    misc = "ReturnType: Task<IErrorsInfo>"
)]
public async Task<IErrorsInfo> SendBatchMessagesAsync(
    string streamName, 
    IEnumerable<object> messages, 
    CancellationToken cancellationToken = default)
{
    try
    {
        if (!StreamConfigs.TryGetValue(streamName, out var config))
        {
            ErrorObject = new ErrorsInfo { Flag = Errors.Failed, Message = $"Stream '{streamName}' not found" };
            return ErrorObject;
        }
        
        var endpointUri = new Uri($"{Dataconnection.ConnectionProp.Host}/{config.EntityName}");
        var sendEndpoint = await _busControl.GetSendEndpoint(endpointUri);
        
        var tasks = messages.Select(msg => sendEndpoint.Send(msg, cancellationToken));
        await Task.WhenAll(tasks);
        
        Logger?.WriteLog($"Sent {messages.Count()} messages to stream '{streamName}'");
        ErrorObject = new ErrorsInfo { Flag = Errors.Ok };
    }
    catch (Exception ex)
    {
        Logger?.WriteLog($"Error sending batch: {ex.Message}");
        ErrorObject = new ErrorsInfo { Flag = Errors.Failed, Message = ex.Message, Ex = ex };
    }
    
    return ErrorObject;
}
```

---

#### 4.3 Message Batching for Consumption

**Add Batch Receive**:
```csharp
public async Task<IEnumerable<object>> GetEntityBatchAsync(
    string entityName, 
    int batchSize = 100, 
    TimeSpan? timeout = null,
    CancellationToken cancellationToken = default)
{
    if (!ChannelData.TryGetValue(entityName, out var channel))
        return Enumerable.Empty<object>();
    
    var messages = new List<object>();
    timeout ??= TimeSpan.FromSeconds(5);
    var endTime = DateTime.UtcNow.Add(timeout.Value);
    
    while (messages.Count < batchSize && DateTime.UtcNow < endTime)
    {
        if (await channel.Reader.WaitToReadAsync(cancellationToken))
        {
            if (channel.Reader.TryRead(out var message))
                messages.Add(message);
        }
    }
    
    return messages;
}
```

---

### Phase 5: Monitoring & Observability (Priority: Low)

#### 5.1 Metrics & Statistics

**Add Metrics Collection**:
```csharp
public class StreamMetrics
{
    public string StreamName { get; set; }
    public long MessagesSent { get; set; }
    public long MessagesReceived { get; set; }
    public long MessagesFailed { get; set; }
    public DateTime LastMessageSent { get; set; }
    public DateTime LastMessageReceived { get; set; }
    public int ChannelCount { get; set; }
}

public Dictionary<string, StreamMetrics> StreamMetrics { get; } = new();

[CommandAttribute(
    Name = "GetStreamMetrics",
    Caption = "Get Stream Metrics",
    Category = DatasourceCategory.MessageQueue,
    DatasourceType = DataSourceType.MassTransit,
    PointType = EnumPointType.Function,
    ObjectType = "StreamMetrics",
    ClassType = "MassTransitDataSource",
    Showin = ShowinType.Both,
    Order = 5
)]
public StreamMetrics GetStreamMetrics(string streamName)
{
    return StreamMetrics.GetValueOrDefault(streamName);
}
```

---

#### 5.2 Health Checks

**Add Health Check Method**:
```csharp
[CommandAttribute(
    Name = "CheckHealth",
    Caption = "Check Connection Health",
    Category = DatasourceCategory.MessageQueue,
    DatasourceType = DataSourceType.MassTransit,
    PointType = EnumPointType.Function,
    ObjectType = "HealthStatus",
    ClassType = "MassTransitDataSource",
    Showin = ShowinType.Both,
    Order = 6
)]
public async Task<HealthStatus> CheckHealthAsync()
{
    try
    {
        if (_busControl == null)
            return new HealthStatus { IsHealthy = false, Message = "Bus not initialized" };
        
        if (ConnectionStatus != ConnectionState.Open)
            return new HealthStatus { IsHealthy = false, Message = "Connection not open" };
        
        // Try to get a send endpoint to verify connectivity
        var testEndpoint = await _busControl.GetSendEndpoint(new Uri("test"));
        
        return new HealthStatus 
        { 
            IsHealthy = true, 
            Message = "Connection healthy",
            StreamCount = StreamConfigs.Count,
            ActiveChannels = ChannelData.Count
        };
    }
    catch (Exception ex)
    {
        return new HealthStatus 
        { 
            IsHealthy = false, 
            Message = $"Health check failed: {ex.Message}" 
        };
    }
}
```

---

### Phase 6: Advanced Features (Priority: Low)

#### 6.1 Message Routing

**Add Routing Support**:
```csharp
public class RoutingConfig
{
    public string RouteKey { get; set; }
    public string Exchange { get; set; }
    public Dictionary<string, string> Headers { get; set; }
}

[CommandAttribute(
    Name = "SendWithRouting",
    Caption = "Send Message with Routing",
    Category = DatasourceCategory.MessageQueue,
    DatasourceType = DataSourceType.MassTransit,
    PointType = EnumPointType.Function,
    ObjectType = "GenericMessage",
    ClassType = "MassTransitDataSource",
    Showin = ShowinType.Both,
    Order = 7
)]
public async Task<IErrorsInfo> SendWithRoutingAsync(
    string streamName, 
    object message, 
    RoutingConfig routing,
    CancellationToken cancellationToken = default)
{
    // Implementation with routing headers
}
```

---

#### 6.2 Dead Letter Queue Support

**Add DLQ Handling**:
```csharp
public class DeadLetterConfig
{
    public string DeadLetterQueueName { get; set; }
    public int MaxRetries { get; set; } = 3;
    public TimeSpan? RetryDelay { get; set; }
}

private async Task HandleDeadLetterAsync(
    string streamName, 
    GenericMessage message, 
    Exception exception)
{
    // Move to DLQ after max retries
}
```

---

#### 6.3 Message Transformation

**Add Transformation Pipeline**:
```csharp
public interface IMessageTransformer
{
    Task<object> TransformAsync(object message, StreamConfig config);
}

[CommandAttribute(
    Name = "SendWithTransformation",
    Caption = "Send Message with Transformation",
    Category = DatasourceCategory.MessageQueue,
    DatasourceType = DataSourceType.MassTransit,
    PointType = EnumPointType.Function,
    ObjectType = "GenericMessage",
    ClassType = "MassTransitDataSource",
    Showin = ShowinType.Both,
    Order = 8
)]
public async Task<IErrorsInfo> SendWithTransformationAsync(
    string streamName,
    object message,
    IMessageTransformer transformer,
    CancellationToken cancellationToken = default)
{
    var transformed = await transformer.TransformAsync(message, StreamConfigs[streamName]);
    return await SendMessageAsync(streamName, transformed, cancellationToken);
}
```

---

### Phase 7: Code Quality & Documentation (Priority: Medium)

#### 7.1 Complete Dispose Pattern

**Enhance Dispose**:
```csharp
protected virtual void Dispose(bool disposing)
{
    if (!disposedValue)
    {
        if (disposing)
        {
            // Stop bus
            try
            {
                _busControl?.Stop();
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error stopping bus during dispose: {ex.Message}");
            }
            
            // Complete channels
            foreach (var channel in ChannelData.Values)
            {
                channel.Writer.Complete();
            }
            ChannelData.Clear();
            
            // Clear configurations
            StreamConfigs.Clear();
            
            // Dispose services if needed
            if (_services is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        
        disposedValue = true;
    }
}
```

---

#### 7.2 XML Documentation

**Add Comprehensive Documentation**:
```csharp
/// <summary>
/// Sends a message to the specified stream/queue.
/// </summary>
/// <param name="streamName">The name of the stream/queue to send the message to</param>
/// <param name="message">The message object to send. Must match the StreamConfig.MessageType</param>
/// <param name="cancellationToken">Cancellation token</param>
/// <returns>Task representing the async operation</returns>
/// <exception cref="InvalidOperationException">Thrown when bus is not connected</exception>
/// <exception cref="KeyNotFoundException">Thrown when stream configuration is not found</exception>
/// <example>
/// <code>
/// var message = new MyMessage { Id = 1, Text = "Hello" };
/// await dataSource.SendMessageAsync("MyStream", message, cancellationToken);
/// </code>
/// </example>
public async Task SendMessageAsync(string streamName, object message, CancellationToken cancellationToken)
{
    // Implementation
}
```

---

#### 7.3 Unit Test Support

**Add Testability Improvements**:
```csharp
// Make internal for testing
internal IBusControl TestBusControl
{
    get => _busControl;
    set => _busControl = value;
}

// Add test helper
public void SetTestServiceProvider(IServiceProvider provider)
{
    _services = provider;
}
```

---

## Implementation Priority

### High Priority (Immediate)
1. ✅ Implement IMessageDataSource interface
2. ✅ Complete NotImplementedException methods
3. ✅ Add CommandAttribute methods
4. ✅ Enhanced connection management with validation
5. ✅ Complete Dispose pattern

### Medium Priority (Next Sprint)
6. ⚠️ Improved error handling and retry logic
7. ⚠️ Type safety improvements
8. ⚠️ Channel management enhancements
9. ⚠️ Batch operations
10. ⚠️ XML documentation

### Low Priority (Future)
11. 📋 Metrics and monitoring
12. 📋 Health checks
13. 📋 Message routing
14. 📋 Dead letter queue support
15. 📋 Message transformation

---

## Testing Strategy

### Unit Tests
- Connection management
- Message sending/receiving
- Stream configuration
- Error handling
- Type loading

### Integration Tests
- RabbitMQ transport
- Kafka transport
- Azure Service Bus transport
- Multi-stream scenarios

### Performance Tests
- High-throughput message sending
- Channel capacity limits
- Batch operations
- Concurrent subscriptions

---

## Migration Guide

### For Existing Users

1. **Update Connection**: Use new validation methods
2. **Update Subscriptions**: Use new async methods
3. **Update Error Handling**: Use new error recovery
4. **Add CommandAttributes**: Methods now discoverable in framework

---

## Success Metrics

- ✅ All NotImplementedException methods implemented
- ✅ 100% interface coverage (IDataSource + IMessageDataSource)
- ✅ CommandAttribute methods for framework discovery
- ✅ Comprehensive error handling
- ✅ Full XML documentation
- ✅ Unit test coverage > 80%

---

## Estimated Effort

- **Phase 1**: 2-3 days
- **Phase 2**: 2-3 days
- **Phase 3**: 1-2 days
- **Phase 4**: 2-3 days
- **Phase 5**: 1-2 days
- **Phase 6**: 2-3 days
- **Phase 7**: 1-2 days

**Total**: ~12-18 days

---

## Conclusion

The MassTransit Data Source has a solid foundation but needs enhancements for production readiness, consistency with other messaging data sources, and framework integration. Following this plan will result in a robust, maintainable, and feature-complete implementation.

