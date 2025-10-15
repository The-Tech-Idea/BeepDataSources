# Beep Data Connectors

## Overview
Beep Data Connectors provide a unified, strongly-typed framework for integrating with a wide variety of external data sources (APIs, platforms, services) in the Beep ecosystem. Each connector implements the `IDataSource` interface, leverages POCO models, and uses standardized metadata and error handling for consistency and maintainability.

## Connector Architecture
- **Base Class**: All connectors inherit from `WebAPIDataSource` (or a relevant base class).
- **Strong Typing**: POCO classes are used for all entities and responses. Avoid using `object` types.
- **CommandAttribute**: Public methods are decorated with `CommandAttribute` to provide metadata (Name, Caption, ClassName, ObjectType, PointType, misc).
- **Response Wrappers**: For paginated APIs, use generic response wrapper classes.
- **System.Text.Json**: Use `[JsonPropertyName]` attributes for property mapping.
- **Error Handling**: Implement robust exception handling and logging using base class helpers.
- **Documentation**: All public methods should have XML documentation comments.

## Connector Categories
Connectors are organized by category:
- Social Media
- Mail Services
- Communication
- Cloud Storage
- IoT
- CRM
- Marketing
- E-commerce
- Accounting
- Business Intelligence
- Content Management
- Customer Support
- Forms
- Meeting Tools
- SMS
- Task Management

Each category contains connectors for major platforms (e.g., Facebook, Gmail, Slack, GoogleDrive, AWSIoT, Salesforce, Shopify, etc.).

## How Connectors Work
1. **Initialization**: Each connector is instantiated with configuration properties (API keys, endpoints, etc.).
2. **Method Invocation**: Methods (e.g., `GetForms`, `CreatePostAsync`) are called to interact with the external API.
3. **Metadata**: Methods are decorated with `CommandAttribute` for discoverability and integration.
4. **Data Mapping**: API responses are mapped to POCO models using `System.Text.Json`.
5. **Error Handling**: Exceptions are logged and handled gracefully.
6. **Extensibility**: New methods and entities can be added following the established pattern.

## Creating a New Connector
Follow these steps to create a new connector:

### 1. Create POCO Models
- Define sealed POCO classes for all entities returned by the API.
- Use `[JsonPropertyName]` attributes for property mapping.
- Implement response wrapper classes for paginated results.

### 2. Implement the DataSource Class
- Inherit from `WebAPIDataSource`.
- Import necessary Beep framework namespaces.
- Add configuration properties for API keys, endpoints, etc.

### 3. Add Methods with CommandAttribute
- Implement strongly typed GET methods for retrieving entities.
- Implement POST/PUT async methods for creating/updating entities using `PostAsync<T>` and `PutAsync<T>`.
- Decorate all public methods with `CommandAttribute`, specifying:
  - `ObjectType`: POCO class name
  - `PointType`: EnumPointType.Function
  - `Name`: Method name
  - `Caption`: User-friendly description
  - `ClassName`: DataSource class name
  - `misc`: Return type (e.g., `IEnumerable<Entity>`)

**Example:**
```csharp
[CommandAttribute(
    ObjectType = "Form",
    PointType = EnumPointType.Function,
    Name = "GetForms",
    Caption = "Retrieve all forms",
    ClassName = "TypeformDataSource",
    misc = "ReturnType: IEnumerable<Form>"
)]
public IEnumerable<Form> GetForms() { ... }
```

### 4. Error Handling & Logging
- Use base class helpers for logging and exception handling.
- Ensure all API errors are caught and logged.

### 5. Documentation
- Add XML documentation comments to all public methods.

### 6. Test & Validate
- Build the connector project to ensure compilation.
- Test all methods for correct API integration and error handling.

### 7. Update Progress Documentation
- Mark connector status and methods in the relevant progress.md file.

## Best Practices
- Always use strong typing and POCOs.
- Decorate all public methods with `CommandAttribute`.
- Handle errors gracefully and log exceptions.
- Document all methods and classes.
- Follow architectural patterns established in existing connectors (e.g., TwitterDataSource).

## Reference Implementation
See `Connectors/SocialMedia/TwitterDataSource` for a comprehensive example of best practices and architectural consistency.

## Contributing
- Fork the repository and create a new branch for your connector.
- Follow the implementation guidelines above.
- Submit a pull request with a summary of your connector and implemented methods.

## License
This project is licensed under the MIT License.
