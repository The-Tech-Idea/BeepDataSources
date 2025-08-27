# CRM Data Sources Implementation Instructions

## Overview

This document provides detailed instructions for implementing and using the individual CRM data source projects in the Beep Data Connectors framework.

## Project Structure

All CRM data sources are organized within the CRM folder with the following structure:

```
Connectors/
└── CRM/
    ├── README.md
    ├── Instructions.md (this file)
    ├── SalesforceDataSource/
    │   ├── SalesforceDataSource.csproj
    │   └── SalesforceDataSource.cs
    ├── HubSpotDataSource/
    │   ├── HubSpotDataSource.csproj
    │   └── HubSpotDataSource.cs
    ├── Dynamics365DataSource/
    │   ├── Dynamics365DataSource.csproj
    │   └── Dynamics365DataSource.cs
    └── ... (other CRM data sources)
```

Each CRM data source is implemented as a separate .NET project with embedded driver logic.

## Implementation Pattern

Each CRM data source follows this consistent pattern:

### 1. Configuration Classes
- **Config Class**: Holds connection parameters and authentication details
- **Entity Class**: Defines CRM entity metadata and field mappings
- **API Response Classes**: Handle CRM-specific API response formats

### 2. IDataSource Implementation
- **Constructor**: Initializes with datasource name, DME editor, connection, and error handler
- **Connection Management**: `ConnectAsync()`, `DisconnectAsync()`
- **Entity Discovery**: `GetEntitiesNamesAsync()`, `GetEntityStructuresAsync()`
- **CRUD Operations**: `GetEntityAsync()`, `InsertEntityAsync()`, `UpdateEntityAsync()`, `DeleteEntityAsync()`

### 3. Authentication Handling
Each CRM implements its specific authentication method:
- **OAuth 2.0**: Salesforce, Dynamics 365, Zoho, SugarCRM
- **API Key**: HubSpot, Freshsales, Insightly, Copper
- **Username/API Key**: Nutshell
- **API Token**: Pipedrive

## Adding a New CRM Data Source

To add a new CRM data source:

1. **Create Project Structure**
   ```bash
   mkdir NewCRMDataSource
   cd NewCRMDataSource
   ```

2. **Create .csproj File**
   ```xml
   <Project Sdk="Microsoft.NET.Sdk">
     <PropertyGroup>
       <TargetFramework>net9.0</TargetFramework>
       <ImplicitUsings>enable</ImplicitUsings>
       <Nullable>enable</Nullable>
       <PackageId>TheTechIdea.Beep.Connectors.NewCRMDataSource</PackageId>
       <Version>1.0.0</Version>
       <Authors>The Tech Idea</Authors>
       <Description>NewCRM Data Source for Beep Data Connectors</Description>
     </PropertyGroup>

     <ItemGroup>
       <ProjectReference Include="..\..\..\BeepDM\DataManagementEngineStandard\DataManagementEngine.csproj" />
       <ProjectReference Include="..\..\..\BeepDM\DataManagementModelsStandard\DataManagementModels.csproj" />
       <ProjectReference Include="..\..\..\BeepDM\DMLoggerStandard\DMLogger.csproj" />
     </ItemGroup>

     <ItemGroup>
       <!-- HTTP Client Extensions -->
       <PackageReference Include="Microsoft.Extensions.Http" Version="8.0.0" />
       <PackageReference Include="Microsoft.Extensions.Http.Polly" Version="8.0.0" />
       <!-- JSON Handling -->
       <PackageReference Include="System.Text.Json" Version="8.0.0" />
       <!-- Add CRM-specific packages here -->
     </ItemGroup>
   </Project>
   ```

3. **Implement the Data Source Class**
   ```csharp
   using System;
   using System.Collections.Generic;
   using System.Net.Http;
   using System.Threading.Tasks;
   using TheTechIdea.Beep.DataBase;
   using TheTechIdea.Beep.Editor;
   using TheTechIdea.Beep.Logger;
   using TheTechIdea.Beep.Utilities;
   using TheTechIdea.Logger;
   using TheTechIdea.Util;

   namespace TheTechIdea.Beep.Connectors.NewCRMDataSource
   {
       public class NewCRMDataSource : IDataSource
       {
           // Configuration classes
           public class NewCRMConfig { /* config properties */ }
           public class NewCRMEntity { /* entity metadata */ }

           // Private fields
           private readonly NewCRMConfig _config;
           private HttpClient? _httpClient;
           private readonly IDMEEditor _dmeEditor;
           private readonly IErrorsInfo _errorsInfo;
           private readonly IDMLogger _logger;
           private readonly IUtil _util;
           private ConnectionState _connectionState = ConnectionState.Closed;

           // Constructor
           public NewCRMDataSource(string datasourcename, IDMEEditor dmeEditor,
                                 IDataConnection cn, IErrorsInfo per) { /* implementation */ }

           // IDataSource implementation
           public string DatasourceName { get; set; }
           public string DatasourceType { get; set; } = "NewCRM";
           public DatasourceCategory Category { get; set; } = DatasourceCategory.CRM;
           // ... other interface properties

           // Core methods
           public async Task<bool> ConnectAsync() { /* implementation */ }
           public async Task<bool> DisconnectAsync() { /* implementation */ }
           public async Task<List<string>> GetEntitiesNamesAsync() { /* implementation */ }
           public async Task<List<EntityStructure>> GetEntityStructuresAsync(bool refresh = false) { /* implementation */ }
           public async Task<object?> GetEntityAsync(string entityName, List<AppFilter>? filter = null) { /* implementation */ }
           public async Task<bool> InsertEntityAsync(string entityName, object entityData) { /* implementation */ }
           public async Task<bool> UpdateEntityAsync(string entityName, object entityData, string entityId) { /* implementation */ }
           public async Task<bool> DeleteEntityAsync(string entityName, string entityId) { /* implementation */ }

           // Standard interface methods
           public bool CreateEntityAsAsync(string entityname, object entitydata) { /* implementation */ }
           public bool UpdateEntity(string entityname, object entitydata, string entityid) { /* implementation */ }
           public bool DeleteEntity(string entityname, string entityid) { /* implementation */ }
           public object GetEntity(string entityname, List<AppFilter> filter) { /* implementation */ }
           public List<EntityStructure> GetEntityStructures(bool refresh = false) { /* implementation */ }
           public List<string> GetEntitesList() { /* implementation */ }
           public bool Openconnection() { /* implementation */ }
           public bool Closeconnection() { /* implementation */ }
           public bool CreateEntityAs(string entityname, object entitydata) { /* implementation */ }
           public object RunQuery(string qrystr) { /* implementation */ }
           public object RunScript(ETLScriptDet dDLScripts) { /* implementation */ }
           public void Dispose() { /* implementation */ }
       }
   }
   ```

## Connection String Parsing

Each data source must parse its connection string in the constructor:

```csharp
private void ParseConnectionString()
{
    if (string.IsNullOrEmpty(_connectionString))
        return;

    // Parse connection string format: Key1=value1;Key2=value2
    var parts = _connectionString.Split(';');
    foreach (var part in parts)
    {
        var keyValue = part.Split('=');
        if (keyValue.Length == 2)
        {
            var key = keyValue[0].Trim();
            var value = keyValue[1].Trim();

            switch (key.ToLower())
            {
                case "key1":
                    _config.Property1 = value;
                    break;
                case "key2":
                    _config.Property2 = value;
                    break;
            }
        }
    }
}
```

## Entity Metadata

Define common entities for each CRM:

```csharp
private async Task<List<NewCRMEntity>> GetNewCRMEntitiesAsync()
{
    if (_entityCache.Any())
        return _entityCache.Values.ToList();

    var entities = new List<NewCRMEntity>
    {
        new NewCRMEntity
        {
            EntityName = "Contacts",
            DisplayName = "Contacts",
            Fields = new Dictionary<string, string>
            {
                ["id"] = "String",
                ["first_name"] = "String",
                ["last_name"] = "String",
                ["email"] = "String"
            }
        },
        // Add more entities...
    };

    foreach (var entity in entities)
    {
        _entityCache[entity.EntityName] = entity;
    }

    return entities;
}
```

## Error Handling

Implement consistent error handling:

```csharp
try
{
    // Operation code
}
catch (Exception ex)
{
    _errorsInfo.AddError("CRMName", $"Operation failed: {ex.Message}", ex);
    _logger.WriteLog($"CRMName error: {ex.Message}");
    return false;
}
```

## Testing

Test each data source implementation:

1. **Connection Test**: Verify authentication works
2. **Entity Discovery**: Ensure entities are discovered correctly
3. **CRUD Operations**: Test Create, Read, Update, Delete operations
4. **Error Scenarios**: Test error handling for invalid credentials, network issues, etc.

## Integration with Beep Framework

To integrate with the Beep framework:

1. **Add Project Reference**: Add the CRM data source project to your main application
2. **Register Data Source**: Register the data source in the framework's data source factory
3. **Configure Connection**: Set up connection strings in your application configuration
4. **Use in Code**: Instantiate and use the data source like any other IDataSource

## Best Practices

1. **Async/Await**: Use async/await for all I/O operations
2. **Error Handling**: Implement comprehensive error handling and logging
3. **Connection Management**: Properly manage HTTP client connections
4. **Caching**: Cache entity metadata to avoid repeated API calls
5. **Rate Limiting**: Implement rate limiting for CRM APIs that require it
6. **Authentication Refresh**: Handle token refresh for OAuth-based CRMs
7. **Documentation**: Document connection string formats and configuration options

## Common Issues and Solutions

### Authentication Issues
- Verify connection string parameters
- Check API credentials and permissions
- Ensure correct data center/region for multi-region CRMs

### API Rate Limiting
- Implement retry logic with exponential backoff
- Cache frequently accessed data
- Use bulk operations when available

### Data Type Mapping
- Map CRM data types to .NET types consistently
- Handle nullable fields appropriately
- Convert date/time formats correctly

### Network Issues
- Implement timeout handling
- Add retry logic for transient failures
- Handle proxy and firewall configurations

## Performance Optimization

1. **Connection Pooling**: Reuse HTTP client instances
2. **Caching**: Cache entity metadata and frequently accessed data
3. **Batch Operations**: Use bulk operations when available
4. **Pagination**: Handle large result sets with pagination
5. **Compression**: Enable HTTP compression for large payloads

## Security Considerations

1. **Credential Storage**: Store credentials securely (not in plain text)
2. **Token Management**: Handle access tokens securely
3. **HTTPS Only**: Always use HTTPS for API communications
4. **Input Validation**: Validate all input parameters
5. **Error Information**: Don't expose sensitive information in error messages

## Maintenance

1. **API Updates**: Monitor CRM API changes and update implementations
2. **Dependency Updates**: Keep NuGet packages updated
3. **Testing**: Maintain comprehensive test coverage
4. **Documentation**: Keep documentation current with implementation changes

## Support and Resources

- **CRM API Documentation**: Refer to each CRM's official API documentation
- **Beep Framework Documentation**: Framework-specific integration guides
- **Community Support**: Check for community forums and issues
- **Logging**: Use comprehensive logging for troubleshooting
- **Authentication**: OAuth 2.0
- **Project**: `../SugarCRMDataSource/SugarCRMDataSource.csproj`
- **Dependencies**:
  - Microsoft.Extensions.Http
  - Microsoft.Extensions.Http.Polly
  - System.Text.Json

### 6. FreshsalesDataSource
- **CRM**: Freshsales
- **API**: Freshsales REST API
- **Authentication**: API Key
- **Project**: `../FreshsalesDataSource/FreshsalesDataSource.csproj`
- **Dependencies**:
  - Microsoft.Extensions.Http
  - Microsoft.Extensions.Http.Polly
  - System.Text.Json

### 7. InsightlyDataSource
- **CRM**: Insightly
- **API**: Insightly REST API
- **Authentication**: API Key (Basic Auth)
- **Project**: `../InsightlyDataSource/InsightlyDataSource.csproj`
- **Dependencies**:
  - Microsoft.Extensions.Http
  - Microsoft.Extensions.Http.Polly
  - System.Text.Json

### 8. CopperDataSource
- **CRM**: Copper
- **API**: Copper REST API
- **Authentication**: API Key
- **Project**: `../CopperDataSource/CopperDataSource.csproj`
- **Dependencies**:
  - Microsoft.Extensions.Http
  - Microsoft.Extensions.Http.Polly
  - System.Text.Json

### 9. NutshellDataSource
- **CRM**: Nutshell
- **API**: Nutshell JSON-RPC API
- **Authentication**: Username/API Key
- **Project**: `../NutshellDataSource/NutshellDataSource.csproj`
- **Dependencies**:
  - Microsoft.Extensions.Http
  - Microsoft.Extensions.Http.Polly
  - System.Text.Json

### 10. PipedriveDataSource
- **CRM**: Pipedrive
- **API**: Pipedrive REST API
- **Authentication**: API Token
- **Project**: `../PipedriveDataSource/PipedriveDataSource.csproj`
- **Dependencies**:
  - Microsoft.Extensions.Http
  - Microsoft.Extensions.Http.Polly
  - System.Text.Json

## Common Features

All CRM data sources implement the `IDataSource` interface and provide:

- **Connection Management**: Connect/Disconnect with proper authentication
- **Entity Discovery**: Get available entities (tables/objects) from the CRM
- **CRUD Operations**: Create, Read, Update, Delete operations
- **Filtering**: Support for basic filtering on entity queries
- **Error Handling**: Comprehensive error handling and logging
- **Async Support**: All operations support async/await patterns

## Usage Example

```csharp
// Create data source instance
var dataSource = new SalesforceDataSource("MySalesforce", dmeEditor, connection, errors);

// Connect to CRM
await dataSource.ConnectAsync();

// Get available entities
var entities = await dataSource.GetEntitiesNamesAsync();

// Get data from specific entity
var contacts = await dataSource.GetEntityAsync("Contact");

// Insert new record
var newContact = new { FirstName = "John", LastName = "Doe", Email = "john@example.com" };
await dataSource.InsertEntityAsync("Contact", newContact);

// Disconnect
await dataSource.DisconnectAsync();
```

## Connection String Formats

Each CRM requires specific connection string parameters:

### Salesforce
```
Username=username;Password=password;SecurityToken=token;InstanceUrl=https://yourinstance.salesforce.com
```

### HubSpot
```
ApiKey=your_api_key;BaseUrl=https://api.hubapi.com
```

### Dynamics 365
```
TenantId=tenant_id;ClientId=client_id;ClientSecret=client_secret;OrganizationUrl=https://yourorg.crm.dynamics.com
```

### Zoho
```
ClientId=client_id;ClientSecret=client_secret;RefreshToken=refresh_token;DataCenter=us
```

### SugarCRM
```
BaseUrl=https://yourinstance.sugarcrm.com;Username=username;Password=password;ClientId=client_id;ClientSecret=client_secret
```

### Freshsales
```
Domain=yourcompany;ApiKey=api_key
```

### Insightly
```
ApiKey=api_key;BaseUrl=https://api.insightly.com/v3.1
```

### Copper
```
Email=email;ApiKey=api_key;BaseUrl=https://api.copper.com/developer_api/v1
```

### Nutshell
```
Username=username;ApiKey=api_key;BaseUrl=https://app.nutshell.com/api/v1/json
```

### Pipedrive
```
ApiToken=api_token;BaseUrl=https://api.pipedrive.com/v1
```

## Architecture Notes

- **Embedded Drivers**: Each data source contains its own CRM-specific driver logic
- **No External Dependencies**: Driver logic is self-contained within each data source
- **Consistent Interface**: All data sources implement the same `IDataSource` interface
- **Framework Integration**: Built for the Beep Data Connectors framework
- **Async/Await**: All operations are asynchronous for better performance

## Building and Deployment

1. Each CRM data source is a separate .NET project
2. Add project references to the projects you need
3. Restore NuGet packages: `dotnet restore`
4. Build: `dotnet build`
5. The assemblies can be deployed independently

## Support

For issues specific to a particular CRM integration, refer to:
- The CRM's official API documentation
- The data source implementation comments
- Beep framework documentation</content>
<parameter name="filePath">c:\Users\f_ald\source\repos\The-Tech-Idea\BeepDataSources\Connectors\CRM\README.md
