# Remaining Connector Refactoring Tasks

## Summary
- **Total Connectors:** 105+
- **Status:** 97/105 COMPLETE (92%)
- **Remaining:** 8 connectors need consolidation/creation

---

## Task 1: TwistDataSource (Consolidation)
**Location:** `/Connectors/Communication/TwistDataSource/`

### Current State:
- Models folder: `Models/TwistModels.cs` (348 lines)
- DataSource: Has CommandAttributes ✅

### Required Action:
1. Read: `Models/TwistModels.cs`
2. Create: `Models.cs` at root with corrected imports:
   - Add: `using System;`
   - Add: `using System.Collections.Generic;`
   - Add: `using System.Text.Json.Serialization;`
   - Add: `using TheTechIdea.Beep.DataBase;`
   - Keep namespace: `TheTechIdea.Beep.Connectors.Communication.Twist.Models`

3. Verify: Imports in `TwistDataSource.cs` file work correctly
4. Note: Delete Models/ folder after project compiles successfully

---

## Task 2: ZoomDataSource (Consolidation)
**Location:** `/Connectors/Communication/ZoomDataSource/`

### Current State:
- Models folder: `Models/ZoomModels.cs` (310 lines)
- DataSource: Has CommandAttributes ✅

### Required Action:
1. Read: `Models/ZoomModels.cs`
2. Create: `Models.cs` at root with corrected imports
3. Verify: Namespace and imports align with DataSource usage
4. Delete: Models/ folder after compilation

---

## Task 3: EcwidDataSource (Consolidation)
**Location:** `/Connectors/E-commerce/EcwidDataSource/`

### Current State:
- Models folder: `Models/EcwidModels.cs` (683 lines - largest file)
- DataSource: Has CommandAttributes ✅

### Required Action:
1. Read: `Models/EcwidModels.cs`
2. Create: `Models.cs` at root
3. Add imports for this larger file:
   - All standard imports
   - Check for any special System.* namespaces used
4. Verify: All model classes are correctly migrated
5. Delete: Models/ folder after compilation

---

## Task 4: FrontDataSource (Creation)
**Location:** `/Connectors/CustomerSupport/Front/`

### Current State:
- NO Models.cs file exists
- DataSource: Has CommandAttributes ✅

### Required Action:
1. Read: `FrontDataSource.cs` to identify model classes
2. Analyze: What entities are returned by GetEntity methods?
3. Create: `Models.cs` with:
   - Base class: `FrontEntityBase` with Attach<T> method
   - Model classes for identified entities
   - Proper namespace: `TheTechIdea.Beep.Connectors.CustomerSupport.Front.Models`
   - All necessary imports
4. Add: `[JsonPropertyName(...)]` attributes to properties
5. Update: DataSource.cs imports if needed

### Template:
```csharp
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.CustomerSupport.Front.Models
{
    public abstract class FrontEntityBase
    {
        [JsonIgnore] public IDataSource DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : FrontEntityBase 
        { 
            DataSource = ds; 
            return (T)this; 
        }
    }

    public sealed class FrontConversation : FrontEntityBase
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        // ... more properties
    }

    public sealed class FrontMessage : FrontEntityBase
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        // ... more properties
    }
}
```

---

## Task 5: KayakoDataSource (Creation)
**Location:** `/Connectors/CustomerSupport/Kayako/`

### Current State:
- NO Models.cs file exists
- DataSource: Has CommandAttributes ✅

### Required Action:
Same as FrontDataSource - follow the template above but with Kayako-specific models:
- Namespace: `TheTechIdea.Beep.Connectors.CustomerSupport.Kayako.Models`
- Base class: `KayakoEntityBase`
- Identify Kayako entities (likely: Ticket, Attachment, User, Organization, etc.)

---

## Task 6: LiveAgentDataSource (Creation)
**Location:** `/Connectors/CustomerSupport/LiveAgent/`

### Current State:
- NO Models.cs file exists
- DataSource: Has CommandAttributes ✅

### Required Action:
Same as FrontDataSource - follow the template above but with LiveAgent-specific models:
- Namespace: `TheTechIdea.Beep.Connectors.CustomerSupport.LiveAgent.Models`
- Base class: `LiveAgentEntityBase`
- Identify LiveAgent entities

---

## Consolidation Checklist (Tasks 1-3)

For each consolidation task:
- [ ] Read Models/[Name]Models.cs file completely
- [ ] Verify namespace path
- [ ] Identify all imports needed
- [ ] Create Models.cs at root level
- [ ] Add/update imports (especially System.Text.Json.Serialization, TheTechIdea.Beep.DataBase)
- [ ] Copy all class definitions
- [ ] Preserve all [JsonPropertyName] attributes
- [ ] Test: Build project to verify compilation
- [ ] Note: Don't delete Models/ folder manually - will be removed after build

---

## Creation Checklist (Tasks 4-6)

For each creation task:
- [ ] Read [Name]DataSource.cs file
- [ ] Identify all entity/model classes returned
- [ ] Identify properties/fields for each model
- [ ] Create base class with Attach<T> method
- [ ] Create sealed model classes
- [ ] Add [JsonPropertyName] attributes
- [ ] Add necessary imports
- [ ] Ensure [JsonIgnore] on DataSource property
- [ ] Test: Build project to verify compilation
- [ ] Verify DataSource.cs imports reference Models namespace

---

## Verification Steps After Completion

For all 8 remaining connectors:

1. **Check Compilation:**
   ```bash
   dotnet build "C:\Users\f_ald\source\repos\The-Tech-Idea\BeepDataSources\Connectors\[Category]\[Name]DataSource\[Name]DataSource.csproj"
   ```

2. **Verify Namespaces:**
   - Models classes should be in: `TheTechIdea.Beep.Connectors.[Category].[Name].Models`
   - DataSource imports should reference this namespace

3. **Verify CommandAttributes:**
   - All public methods should have [CommandAttribute] decoration
   - Verify in DataSource files: All methods return properly typed objects

4. **Git Status:**
   - Stage changes
   - Commit with message: "Refactor: Consolidate Models.cs for [ConnectorName]"

---

## Expected Final State

When all tasks are complete:
- ✅ 105/105 connectors following Twitter/Slack/HubSpot pattern
- ✅ All connectors have Models.cs at root level
- ✅ All connectors have proper CommandAttribute decorations
- ✅ All connectors extend WebAPIDataSource
- ✅ All connectors have proper entity mappings
- ✅ All Model classes have base class with Attach<T> method
- ✅ Unified, consistent codebase across all connectors

---

## Timeline Estimate

- Task 1-3 (Consolidation): ~30 min total
- Task 4-6 (Creation): ~60 min total
- Verification: ~15 min
- **Total: ~2 hours for all remaining work**

---

## References

- Twitter Implementation: `/Connectors/SocialMedia/Twitter/TwitterDataSource.cs`
- HubSpot Implementation: `/Connectors/CRM/HubSpotDataSource/HubSpotDataSource.cs`
- Slack Implementation: `/Connectors/Communication/SlackDataSource/SlackDataSource.cs`
- WebAPIDataSource: `/BeepDM/DataManagementEngineStandard/WebAPI/WebAPIDataSource.cs`
