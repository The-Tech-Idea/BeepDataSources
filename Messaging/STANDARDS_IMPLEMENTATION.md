# Messaging Standards Implementation Summary

## Overview

All messaging data sources have been updated to implement and follow the messaging standards defined in `DataManagementModelsStandard/Messaging/MESSAGE_STANDARDS.md`.

## Implementation Status

### ✅ KafkaDataSource
- **Status**: Fully compliant
- **Interface**: Implements `IMessageDataSource<GenericMessage, StreamConfig>`
- **Standards Applied**:
  - Message validation before sending
  - Standard metadata enforcement
  - Standard JSON serialization
  - Error handling with standard metadata
  - Header extraction on message consumption
  - Standards compliance enforcement

### ✅ RabbitMQDataSource
- **Status**: Fully compliant
- **Interface**: Implements `IMessageDataSource<GenericMessage, StreamConfig>`
- **Standards Applied**:
  - Message validation before sending
  - Standard metadata enforcement
  - Standard JSON serialization
  - Error handling with standard metadata
  - Routing key support from metadata
  - Priority handling
  - Header extraction on message consumption
  - Standards compliance enforcement

### ✅ MassTransitDataSource
- **Status**: Fully compliant
- **Interface**: Now implements `IMessageDataSource<GenericMessage, StreamConfig>` (previously missing)
- **Standards Applied**:
  - Message validation before sending
  - Standard metadata enforcement
  - Standard JSON serialization
  - Error handling with standard metadata
  - All required interface methods implemented:
    - `Initialize(StreamConfig)`
    - `SendMessageAsync(string, GenericMessage, CancellationToken)`
    - `SubscribeAsync(string, Func<GenericMessage, Task>, CancellationToken)`
    - `AcknowledgeMessageAsync(string, GenericMessage, CancellationToken)` ✨ NEW
    - `PeekMessageAsync(string, CancellationToken)` ✨ NEW
    - `GetStreamMetadataAsync(string, CancellationToken)` ✨ NEW
    - `Disconnect()`

## Key Changes

### 1. MessageStandardsHelper Class
Created a comprehensive helper class (`TheTechIdea.Beep.Messaging.MessageStandardsHelper`) with:
- `CreateStandardMessage()` - Creates messages with all required metadata
- `CreateCorrelatedMessage()` - Creates correlated messages for request/response
- `ValidateMessage()` - Validates messages against standards
- `EnsureMessageStandards()` - Ensures messages have all required metadata
- `SerializePayload()` / `DeserializePayload()` - Standard JSON serialization
- `GetPartitionKey()` - Gets partition key following priority rules
- `SetErrorMessage()` / `ClearErrorMessage()` - Error metadata management
- `IsVersionCompatible()` - Version compatibility checking

### 2. Enhanced GenericMessage
Added helper properties for standard metadata:
- `MessageType`, `MessageVersion`, `CorrelationId`, `CausationId`
- `Source`, `ContentType`, `Encoding`
- `RetryCount`, `ErrorCode`, `ErrorMessage`
- `PartitionKey`, `RoutingKey`, `TenantId`, `UserId`, `TraceId`
- `IsValid()`, `GetValidationErrors()` - Validation methods

### 3. SendMessageAsync Updates
All three data sources now:
1. **Validate** messages using `MessageStandardsHelper.ValidateMessage()`
2. **Ensure standards** using `MessageStandardsHelper.EnsureMessageStandards()`
3. **Use standard serialization** via `MessageStandardsHelper.SerializePayload()`
4. **Handle errors** using `MessageStandardsHelper.SetErrorMessage()`
5. **Log with MessageId** for traceability

### 4. SubscribeAsync Updates
All three data sources now:
1. **Extract metadata** from transport-specific headers
2. **Create GenericMessage** with all standard properties
3. **Ensure standards compliance** before invoking callbacks
4. **Preserve correlation IDs** and other metadata

### 5. MassTransitDataSource Specific
- **Added missing interface methods**:
  - `AcknowledgeMessageAsync()` - For interface compliance (MassTransit handles automatically)
  - `PeekMessageAsync()` - Reads from channel without removing (limited peek support)
  - `GetStreamMetadataAsync()` - Returns stream configuration and status
- **Updated SendMessageAsync** to accept `GenericMessage` instead of `object`
- **Updated SubscribeAsync** to match interface signature with callback

## Standards Compliance

### Required Metadata
All messages now automatically include:
- ✅ `MessageId` (GUID)
- ✅ `EntityName` (stream/queue name)
- ✅ `Timestamp` (UTC)
- ✅ `MessageType` (fully qualified type name)
- ✅ `MessageVersion` (semantic version, default: "1.0.0")
- ✅ `Source` (application/service name)
- ✅ `ContentType` (default: "application/json")
- ✅ `Encoding` (default: "utf-8")

### Validation
- Messages are validated before sending
- Validation errors are logged and exceptions thrown
- Invalid messages are rejected with clear error messages

### Error Handling
- Errors are captured in message metadata
- `ErrorCode`, `ErrorMessage`, `RetryCount` are set automatically
- Error timestamps are recorded
- Stack traces are preserved (when available)

### Serialization
- Consistent JSON serialization across all data sources
- CamelCase property naming
- Null values ignored
- Case-insensitive deserialization

## Usage Examples

### Creating a Standard Message

```csharp
// Using helper
var message = MessageStandardsHelper.CreateStandardMessage(
    entityName: "orders.created.v1",
    payload: orderData,
    source: "OrderService",
    messageVersion: "1.0.0",
    priority: 100,
    correlationId: correlationId
);

// Or manually
var message = new GenericMessage
{
    MessageId = Guid.NewGuid().ToString(),
    EntityName = "orders.created.v1",
    Payload = orderData,
    Timestamp = DateTime.UtcNow
};
message.MessageType = typeof(OrderCreatedEvent).AssemblyQualifiedName;
message.MessageVersion = "1.0.0";
message.Source = "OrderService";
message.ContentType = "application/json";
```

### Sending a Message

```csharp
// All data sources now validate and ensure standards automatically
await dataSource.SendMessageAsync("orders.created.v1", message, cancellationToken);
```

### Subscribing to Messages

```csharp
await dataSource.SubscribeAsync("orders.created.v1", async (message) =>
{
    // Message is guaranteed to have all standard metadata
    if (!message.IsValid())
    {
        var errors = message.GetValidationErrors();
        // Handle validation errors
        return;
    }

    // Process message
    await ProcessMessageAsync(message);

    // Acknowledge
    await dataSource.AcknowledgeMessageAsync("orders.created.v1", message, cancellationToken);
}, cancellationToken);
```

## Testing Recommendations

1. **Message Validation**: Test with missing required metadata
2. **Error Handling**: Test with invalid payloads
3. **Correlation**: Test request/response correlation
4. **Version Compatibility**: Test with different message versions
5. **Serialization**: Test with various payload types
6. **Metadata Preservation**: Verify metadata is preserved across transport

## Migration Notes

### Breaking Changes
- **MassTransitDataSource.SendMessageAsync**: Now requires `GenericMessage` instead of `object`
- **MassTransitDataSource.SubscribeAsync**: Now requires callback parameter

### Backward Compatibility
- Existing code using `GenericMessage` will continue to work
- Messages without standard metadata will be automatically enhanced
- Validation errors will be thrown for truly invalid messages

## Next Steps

1. ✅ All data sources implement standards
2. ✅ MessageStandardsHelper created
3. ✅ GenericMessage enhanced
4. ⏭️ Add unit tests for standards compliance
5. ⏭️ Add integration tests for cross-data-source compatibility
6. ⏭️ Document migration guide for existing code
7. ⏭️ Add performance benchmarks

## References

- **Standards Document**: `DataManagementModelsStandard/Messaging/MESSAGE_STANDARDS.md`
- **Quick Reference**: `DataManagementModelsStandard/Messaging/QUICK_REFERENCE.md`
- **Helper Class**: `DataManagementModelsStandard/Messaging/MessageStandardsHelper.cs`
- **Enhanced Message**: `DataManagementModelsStandard/Messaging/GenericMessage.cs`

