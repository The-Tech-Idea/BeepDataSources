# Co-Pilot Instructions: Creating Data Sources

This guide provides step-by-step instructions for creating new data source connectors following the established patterns used in the BeepDataSources project, specifically based on the Twitter connector implementation.

## 📋 Prerequisites

- Familiarity with C# and .NET development
- Understanding of REST APIs and JSON
- Knowledge of the Beep framework architecture
- Access to the target API documentation

## 🏗️ Project Structure

Each connector should follow this structure:

```
Connectors/{Category}/{ServiceName}/
├── {ServiceName}.csproj
├── {ServiceName}DataSource.cs
├── Models.cs
├── {ServiceName}Helpers.cs (optional)
└── {ServiceName}RefactoringPlan.md (optional)
```

## 🚀 Step-by-Step Implementation

### 1. Create Project Structure

1. **Create Directory Structure**:
   ```bash
   mkdir Connectors/{Category}/{ServiceName}
   cd Connectors/{Category}/{ServiceName}
   ```

2. **Create Project File** (`{ServiceName}.csproj`):
   ```xml
   <Project Sdk="Microsoft.NET.Sdk">
     <PropertyGroup>
       <TargetFramework>net9.0</TargetFramework>
       <ImplicitUsings>enable</ImplicitUsings>
       <Nullable>enable</Nullable>
       <LangVersion>12.0</LangVersion>
     </PropertyGroup>

     <ItemGroup>
       <PackageReference Include="System.Text.Json" Version="8.0.4" />
     </ItemGroup>

     <ItemGroup>
       <ProjectReference Include="..\..\..\DataManagementModelsStandard\DataManagementModels.csproj" />
       <ProjectReference Include="..\..\..\DataManagementEngineStandard\DataManagementEngine.csproj" />
     </ItemGroup>
   </Project>
   ```

### 2. Define POCO Models

Create `Models.cs` with strongly typed classes:

```csharp
using System.Text.Json.Serialization;

namespace TheTechIdea.Beep.Connectors.{ServiceName}.Models
{
    // Base class for all entities
    public abstract class {ServiceName}EntityBase : TwitterEntityBase
    {
        // Common properties if any
    }

    // Main entity classes
    public sealed class {ServiceName}Item : {ServiceName}EntityBase
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        // Add other properties based on API response
    }
}
```

**Key Points:**
- Use `sealed` classes for better performance
- Apply `[JsonPropertyName]` attributes matching API field names
- Inherit from a base class if common properties exist
- Make properties nullable where appropriate (`string?`)

### 3. Implement Data Source Class

Create `{ServiceName}DataSource.cs` following the Twitter pattern:

```csharp
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.WebAPI;
using TheTechIdea.Beep.Connectors.{ServiceName}.Models;

namespace TheTechIdea.Beep.Connectors.{ServiceName}
{
    [AddinAttribute(Category = DatasourceCategory.{Category}, DatasourceType = DataSourceType.{ServiceName})]
    public class {ServiceName}DataSource : WebAPIDataSource
    {
        // Entity endpoints mapping
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            ["items"] = "items",
            ["item_details"] = "items/{id}",
            // Add more endpoints as needed
        };

        // Required filters for each entity
        private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            ["items"] = new[] { "query" },
            ["item_details"] = new[] { "id" },
        };

        public {ServiceName}DataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor dmeEditor,
            DataSourceType databasetype,
            IErrorsInfo errorObject)
            : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
        {
            // Initialize WebAPI connection properties
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
            {
                if (Dataconnection != null)
                    Dataconnection.ConnectionProp = new WebAPIConnectionProperties();
            }

            // Register entities
            EntitiesNames = EntityEndpoints.Keys.ToList();
            Entities = EntitiesNames
                .Select(n => new EntityStructure { EntityName = n, DatasourceEntityName = n })
                .ToList();
        }

        // Override GetEntitesList to return fixed list
        public new IEnumerable<string> GetEntitesList() => EntitiesNames;

        // Override base methods for sync/async operations
        public override IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            var data = GetEntityAsync(EntityName, filter).ConfigureAwait(false).GetAwaiter().GetResult();
            return data ?? Array.Empty<object>();
        }

        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            if (!EntityEndpoints.TryGetValue(EntityName, out var endpoint))
                throw new InvalidOperationException($"Unknown {ServiceName} entity '{{EntityName}}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            var resolvedEndpoint = ResolveEndpoint(endpoint, q);

            using var resp = await GetAsync(resolvedEndpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, "data");
        }

        // Implement strongly typed methods with CommandAttributes
        [CommandAttribute(
            Name = "GetItems",
            Caption = "Get Items",
            Category = DatasourceCategory.{Category},
            DatasourceType = DataSourceType.{ServiceName},
            PointType = EnumPointType.Function,
            ObjectType = "{ServiceName}Item",
            ClassType = "{ServiceName}DataSource",
            Showin = ShowinType.Both,
            Order = 1,
            iconimage = "{servicename}.png",
            misc = "ReturnType: IEnumerable<{ServiceName}Item>"
        )]
        public async Task<IEnumerable<{ServiceName}Item>> GetItems(string query, int maxResults = 10)
        {
            var filters = new List<AppFilter>
            {
                new AppFilter { FieldName = "query", FilterValue = query, Operator = "=" },
                new AppFilter { FieldName = "max_results", FilterValue = maxResults.ToString(), Operator = "=" }
            };

            var result = await GetEntityAsync("items", filters);
            return result.Select(item => JsonSerializer.Deserialize<{ServiceName}Item>(JsonSerializer.Serialize(item)))
                        .Where(item => item != null)
                        .Cast<{ServiceName}Item>();
        }

        // Helper methods (FiltersToQuery, RequireFilters, ResolveEndpoint, etc.)
        // Copy from TwitterDataSource.cs and adapt as needed
    }
}
```

### 4. Configure Command Attributes

Each public method should have a `CommandAttribute`:

```csharp
[CommandAttribute(
    Name = "MethodName",
    Caption = "Display Caption",
    Category = DatasourceCategory.{Category},
    DatasourceType = DataSourceType.{ServiceName},
    PointType = EnumPointType.Function,
    ObjectType = "{POCOClassName}",
    ClassType = "{ServiceName}DataSource",
    Showin = ShowinType.Both,
    Order = {OrderNumber},
    iconimage = "{servicename}.png",
    misc = "ReturnType: IEnumerable<{POCOClassName}>"
)]
```

**Attribute Properties:**
- `ObjectType`: Must match the POCO class name (e.g., "TwitterTweet", not "tweets")
- `PointType`: Always `EnumPointType.Function` for data source methods
- `misc`: Include return type information

### 5. Implement Authentication

Configure authentication in the constructor or separate method:

```csharp
// For API Key authentication
if (Dataconnection?.ConnectionProp is WebAPIConnectionProperties webApiProps)
{
    webApiProps.Headers.Add("Authorization", $"Bearer {{apiKey}}");
    webApiProps.BaseUrl = "https://api.{servicename}.com/v1/";
}

// For OAuth
// Implement OAuth flow in separate method
```

### 6. Add Helper Methods

Copy and adapt helper methods from TwitterDataSource:

- `FiltersToQuery()` - Convert AppFilter list to query parameters
- `RequireFilters()` - Validate required parameters
- `ResolveEndpoint()` - Handle parameterized endpoints
- `ExtractArray()` - Parse JSON responses
- `Call{ServiceName}()` - Wrapper for API calls

### 7. Error Handling

Implement comprehensive error handling:

```csharp
try
{
    // API call
}
catch (HttpRequestException ex)
{
    Logger?.WriteLog($"API request failed: {ex.Message}");
    throw new InvalidOperationException($"Failed to retrieve data from {ServiceName}", ex);
}
catch (JsonException ex)
{
    Logger?.WriteLog($"JSON parsing failed: {ex.Message}");
    throw new InvalidOperationException($"Failed to parse {ServiceName} response", ex);
}
```

### 8. Testing and Validation

1. **Build the project**:
   ```bash
   dotnet build --no-restore
   ```

2. **Verify compilation** - Ensure no errors or warnings

3. **Test basic functionality**:
   - Check entity registration
   - Test authentication
   - Verify API calls return expected data

4. **Update project references** if needed

## 📋 Checklist

- [ ] Project structure created
- [ ] .csproj file configured
- [ ] POCO models defined with proper attributes
- [ ] DataSource class inherits from WebAPIDataSource
- [ ] Entity endpoints mapped
- [ ] Authentication configured
- [ ] CommandAttributes added with correct ObjectType
- [ ] Strongly typed methods implemented
- [ ] Helper methods copied/adapted
- [ ] Error handling implemented
- [ ] Project builds successfully
- [ ] README.md updated with new connector

## 🔧 Common Patterns

### API Response Handling
```csharp
// For single object responses
return new List<object> { deserializedObject };

// For array responses
return deserializedArray.Cast<object>();
```

### Pagination Support
```csharp
public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
{
    // Implement cursor-based or offset-based pagination
    // Return PagedResult with proper metadata
}
```

### Rate Limiting
```csharp
// Add delay between requests if needed
await Task.Delay(1000); // 1 second delay
```

## 🚨 Best Practices

1. **Strong Typing**: Always use POCO classes, never `object` or `Dictionary<string, object>`
2. **Consistent Naming**: Follow PascalCase for classes, camelCase for JSON properties
3. **Documentation**: Add XML comments to all public methods
4. **Error Handling**: Log errors and provide meaningful exception messages
5. **Testing**: Test with real API calls when possible
6. **Security**: Never hardcode API keys or secrets

## 📞 Support

For questions or issues:
1. Check existing connectors for similar patterns
2. Review the Twitter connector as the primary reference
3. Consult API documentation for endpoint specifics
4. Test incrementally - build often!

## 🎯 Example: Creating a GitHub Connector

Following these patterns, a GitHub connector would:

1. Create `Connectors/SocialMedia/GitHub/` directory
2. Define `GitHubRepo`, `GitHubIssue`, `GitHubUser` models
3. Implement `GitHubDataSource` with endpoints for repos, issues, users
4. Add methods like `GetRepositories()`, `GetIssues()`, `GetUser()`
5. Configure OAuth authentication
6. Add proper CommandAttributes with ObjectType = "GitHubRepo", etc.

This ensures consistency across all connectors in the BeepDataSources ecosystem.