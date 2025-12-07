# IoT Connectors

## Overview

The IoT connectors category provides integration with Internet of Things platforms, enabling device management, data ingestion, device communication, and IoT analytics. All connectors inherit from `WebAPIDataSource` and use `CommandAttribute` to expose platform-specific functionality to the Beep framework.

## Architecture

- **Base Class**: All connectors inherit from `WebAPIDataSource`
- **Authentication**: Primarily OAuth 2.0 or API Keys
- **Models**: Strongly-typed POCO classes for devices, events, data streams, etc.
- **CommandAttribute**: Public methods decorated with `CommandAttribute` for framework discovery

## Connectors

### AWS IoT (`AWSIoTDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://{region}.iot.{region}.amazonaws.com`  
**Authentication**: AWS Access Key/Secret

#### CommandAttribute Methods
- Device management
- Thing operations
- Shadow operations
- Rule management
- Certificate management

#### Configuration
```csharp
var props = new WebAPIConnectionProperties
{
    Url = "https://iot.us-east-1.amazonaws.com",
    AuthType = AuthTypeEnum.Basic,
    UserID = "your_access_key",
    Password = "your_secret_key"
};
```

---

### Azure IoT Hub (`AzureIoTHubDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://{hub}.azure-devices.net`  
**Authentication**: Shared Access Signature or OAuth 2.0

#### CommandAttribute Methods
- Device management
- Twin operations
- Message operations
- Job management
- Query operations

---

### Particle (`ParticleDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://api.particle.io/v1`  
**Authentication**: Access Token

#### CommandAttribute Methods
- Device management
- Event publishing
- Variable reading
- Function calling
- Product operations

---

## Common Patterns

### CommandAttribute Structure

All IoT connectors use the `CommandAttribute` pattern:

```csharp
[CommandAttribute(
    Category = DatasourceCategory.Connector,
    DatasourceType = DataSourceType.IoTPlatform,
    PointType = EnumPointType.Function,
    ObjectType = "EntityName",
    ClassName = "PlatformDataSource",
    Showin = ShowinType.Both,
    misc = "IEnumerable<EntityType>"
)]
public async Task<IEnumerable<EntityType>> GetDevices(AppFilter filter)
{
    // Implementation
}
```

### Entity Mapping

IoT connectors typically support:
- **Devices** - Device registration, management, and status
- **Events** - Event publishing and subscription
- **Data Streams** - Telemetry data ingestion
- **Shadows/Twins** - Device state management
- **Rules** - Rule engine and automation

## Status

All IoT connectors are **✅ Completed** and ready for use.

