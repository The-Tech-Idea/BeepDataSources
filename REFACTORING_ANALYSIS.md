# Connector Refactoring Analysis & Implementation Plan

## Overview
This document outlines the current state of connector implementations and the refactoring needed to standardize them to the Twitter/Slack DataSource pattern (with Models classes and CommandAttribute decorations).

## Pattern Reference: Twitter DataSource

### Key Characteristics:
1. **Extends WebAPIDataSource** - Inherits from WebAPIDataSource base class
2. **Models Namespace** - Uses dedicated `TheTechIdea.Beep.Connectors.[Service].Models` namespace
3. **Models.cs File** - Single file with all model classes
4. **CommandAttribute Methods** - Extra methods decorated with [CommandAttribute(...)] that wrap standard IDataSource methods
5. **Entity Mapping** - Static dictionaries mapping logical entity names to API endpoints
6. **Helper Methods** - FiltersToQuery, RequireFilters, ResolveEndpoint, etc.
7. **Fixed Entities List** - Pre-defined list of known entities rather than dynamic discovery

### CommandAttribute Structure:
```csharp
[CommandAttribute(
    Name = "GetTweets",
    Caption = "Search Tweets",
    Category = DatasourceCategory.Connector,
    DatasourceType = DataSourceType.Twitter,
    PointType = EnumPointType.Function,
    ObjectType = "TwitterTweet",
    ClassType = "TwitterDataSource",
    Showin = ShowinType.Both,
    Order = 1,
    iconimage = "twitter.png",
    misc = "ReturnType: IEnumerable<TwitterTweet>"
)]
public async Task<IEnumerable<TwitterTweet>> GetTweets(...)
```

---

## Connector Status Summary

### ✅ ALREADY FOLLOWING PATTERN:
1. **Twitter** - Complete implementation (Models.cs, CommandAttributes)
2. **HubSpot** - Complete implementation (Models.cs, CommandAttributes)
3. **Salesforce** - Has CommandAttributes  
4. **Pipedrive** - Has CommandAttributes
5. **Slack** - Has Models.cs and CommandAttributes
6. **Shopify** - Has CommandAttributes and Models.cs
7. **Copper** - Has CommandAttributes

### ✅ STATUS UPDATE - NEARLY COMPLETE!

After comprehensive analysis of all 100+ connectors across all categories:

**COMPLETE (97 connectors):** All have both CommandAttributes and Models.cs
- All CRM connectors (10)
- Most Communication (7 of 11)
- Most E-commerce (9 of 10)
- All Marketing (11)
- All Accounting (7)
- Most CustomerSupport (4 of 7)
- All Cloud-Storage (10)
- All other categories

### ❌ NEED REFACTORING - ONLY 8 CONNECTORS:

The following connectors have Models in Models/ folder (not Models.cs root):

#### Communication (4 connectors):
- **GoogleChatDataSource** - Has Models/GoogleChatModels.cs
- **MicrosoftTeamsDataSource** - Has Models/MicrosoftTeamsModels.cs  
- **TwistDataSource** - Has Models/TwistModels.cs
- **ZoomDataSource** - Has Models/ZoomModels.cs

#### E-commerce (1 connector):
- **EcwidDataSource** - Has Models/EcwidModels.cs

#### CustomerSupport (3 connectors):
- **Front** - Needs Models file creation
- **Kayako** - Needs Models file creation
- **LiveAgent** - Needs Models file creation

---

## Detailed Refactoring Tasks (8 connectors)

### Task 1: GoogleChatDataSource
**Location:** Connectors/Communication/GoogleChatDataSource/
**Current State:** Models/GoogleChatModels.cs exists
**Action:** Merge Models/GoogleChatModels.cs → Models.cs at root
**Status:** Ready for consolidation

### Task 2: MicrosoftTeamsDataSource
**Location:** Connectors/Communication/MicrosoftTeamsDataSource/
**Current State:** Models/MicrosoftTeamsModels.cs exists
**Action:** Merge into Models.cs at root
**Status:** Ready for consolidation

### Task 3: TwistDataSource  
**Location:** Connectors/Communication/TwistDataSource/
**Current State:** Models/TwistModels.cs exists
**Action:** Merge into Models.cs at root
**Status:** Ready for consolidation

### Task 4: ZoomDataSource
**Location:** Connectors/Communication/ZoomDataSource/
**Current State:** Models/ZoomModels.cs exists
**Action:** Merge into Models.cs at root
**Status:** Ready for consolidation

### Task 5: EcwidDataSource
**Location:** Connectors/E-commerce/EcwidDataSource/
**Current State:** Models/EcwidModels.cs exists
**Action:** Merge into Models.cs at root
**Status:** Ready for consolidation

### Task 6-8: Front, Kayako, LiveAgent
**Location:** Connectors/CustomerSupport/[Name]/
**Current State:** No Models files
**Action:** Create Models.cs files with appropriate model classes
**Status:** Requires analysis of respective DataSource files to extract/create models

---

## Implementation Status - NEARLY COMPLETE (92%)

### Completed Work ✅

All 97 connectors in the following categories are COMPLETE:
- **CRM (10/10)** - All complete with CommandAttributes and Models
- **Marketing (11/11)** - All complete
- **Accounting (7/7)** - All complete  
- **Cloud Storage (10/10)** - All complete
- **Communication (7/11)** - Most complete, 4 need consolidation
- **E-commerce (9/10)** - Most complete, 1 needs consolidation
- **CustomerSupport (4/7)** - Most complete, 3 need creation
- **SocialMedia** - All complete
- **BusinessIntelligence, MailServices, ContentManagement, Forms, IoT, MeetingTools, SMS, TaskManagement** - All complete

### Remaining Work (8 connectors)

#### Phase 1: Consolidation Tasks (5 connectors)
Models/ folder → Models.cs at root level

1. **GoogleChatDataSource** ✅ DONE - Models.cs created
2. **MicrosoftTeamsDataSource** ✅ DONE - Models.cs created  
3. **TwistDataSource** - Ready for consolidation (348 lines)
4. **ZoomDataSource** - Ready for consolidation (310 lines)
5. **EcwidDataSource** - Ready for consolidation (683 lines)

#### Phase 2: Creation Tasks (3 connectors)
Models.cs files need creation

6. **Front** (CustomerSupport) - Analyze DataSource.cs and extract models
7. **Kayako** (CustomerSupport) - Analyze DataSource.cs and extract models
8. **LiveAgent** (CustomerSupport) - Analyze DataSource.cs and extract models

### Actions per Remaining Connector:

**For Consolidation Connectors (5):**
1. Read Models/[Name]Models.cs file
2. Create new Models.cs at root with updated namespace and imports
3. Delete Models/ folder when project is rebuilt
4. Verify imports in DataSource.cs file use new namespace path

**For Creation Connectors (3):**
1. Analyze DataSource.cs to identify data model classes used
2. Create Models.cs file with appropriate model classes
3. Add missing JsonPropertyName attributes
4. Ensure base class with Attach<T> method is included
5. Update DataSource.cs imports to reference Models namespace

---

## Refactoring Checklist

For each connector, apply these steps:

### Step 1: Verify Models.cs Structure
```csharp
namespace TheTechIdea.Beep.Connectors.[Service].Models
{
    public abstract class [Service]EntityBase
    {
        [JsonIgnore] public IDataSource DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : [Service]EntityBase { ... }
    }

    public sealed class [Service][Entity] : [Service]EntityBase
    {
        [JsonPropertyName("field_name")]
        public string FieldName { get; set; }
        // ... more properties
    }
}
```

### Step 2: Update DataSource Class
- Import: `using TheTechIdea.Beep.Connectors.[Service].Models;`
- Create fixed entity list
- Create entity to endpoint mapping
- Override GetEntity/GetEntityAsync to use mapping

### Step 3: Add CommandAttribute Methods
For each primary entity/operation:
```csharp
[CommandAttribute(
    Name = "[OperationName]",
    Caption = "[Display Name]",
    Category = DatasourceCategory.Connector,
    DatasourceType = DataSourceType.[Service],
    PointType = EnumPointType.Function,
    ObjectType = "[ModelClass]",
    ClassType = "[DataSourceClass]",
    Showin = ShowinType.Both,
    Order = [number],
    iconimage = "[icon].png",
    misc = "ReturnType: [ReturnType]"
)]
public async Task<[ReturnType]> [OperationName]([Parameters])
{
    // Implementation wrapping GetEntityAsync or standard IDataSource methods
}
```

### Step 4: Add Helper Methods
```csharp
private static Dictionary<string, string> FiltersToQuery(List<AppFilter> filters)
{
    var q = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    if (filters == null) return q;
    foreach (var f in filters)
    {
        if (f == null || string.IsNullOrWhiteSpace(f.FieldName)) continue;
        q[f.FieldName.Trim()] = f.FilterValue?.ToString() ?? string.Empty;
    }
    return q;
}

private static void RequireFilters(string entity, Dictionary<string, string> q, string[] required)
{
    if (required == null || required.Length == 0) return;
    var missing = required.Where(r => !q.ContainsKey(r) || string.IsNullOrWhiteSpace(q[r])).ToList();
    if (missing.Count > 0)
        throw new ArgumentException($"Entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
}
```

### Step 5: Test and Verify
- Ensure Models deserialize correctly from JSON
- Test GetEntity methods with both filters and no filters
- Test CommandAttribute methods return correctly typed data
- Verify error handling

---

## Files to Update

### csproj Changes
Ensure all connectors have:
```xml
<ItemGroup>
    <PackageReference Include="TheTechIdea.Beep.DataManagementEngine" Version="2.0.61" />
    <PackageReference Include="TheTechIdea.Beep.DataManagementModels" Version="2.0.118" />
    <PackageReference Include="System.Text.Json" Version="10.0.2" />
</ItemGroup>
```

### Namespace Structure
```
Connectors/
├── [Category]/
│   └── [Service]DataSource/
│       ├── [Service]DataSource.cs          (Main class, extends WebAPIDataSource)
│       ├── Models.cs                        (All model classes)
│       └── [Service]DataSource.csproj      (Project file)
```

---

## Implementation Order (Recommended)

1. **Start with CRM (Dynamics365, Freshsales, Insightly, Nutshell, Zoho)** - Most critical business domain
2. **Then Communication (Discord, Teams, RocketChat, etc.)** - Well-defined APIs
3. **Then E-commerce (WooCommerce, Magento, etc.)** - Clear entity models
4. **Then Marketing (Mailchimp, Klaviyo, etc.)** - Similar patterns
5. **Then Accounting** - Standardized models
6. **Then Cloud Storage & Other Services** - Final batch

---

## Benefits of This Refactoring

1. **Consistency** - All connectors follow the same pattern
2. **Discoverability** - CommandAttribute methods show up in UI
3. **Type Safety** - Strong typing with models instead of dynamic objects
4. **Maintainability** - Clear separation of concerns (Models vs DataSource logic)
5. **Reusability** - Models can be used in other parts of the application
6. **Documentation** - CommandAttribute provides built-in documentation
7. **Testing** - Easier to write unit tests with typed models

---

## References

- Twitter Implementation: `/Connectors/SocialMedia/Twitter/`
- HubSpot Implementation: `/Connectors/CRM/HubSpotDataSource/`
- Slack Implementation: `/Connectors/Communication/SlackDataSource/`
- WebAPI Base Class: `/BeepDM/DataManagementEngineStandard/WebAPI/WebAPIDataSource.cs`
