# Updated Plugin Migration Strategy

## Overview

This document describes the updated migration strategy for consolidating the RDMP plugins while preserving namespaces and respecting that RdmpDicom is a separate published NuGet package.

## Project Structure

```
/Plugins/
├── Plugins.csproj                 # HicPlugin + RdmpExtensions non-UI components
├── Plugins.UI.csproj              # HicPlugin + RdmpExtensions UI components
├── Plugins.Tests.csproj           # All non-UI tests (HicPlugin + RdmpExtensions)
└── Plugins.UI.Tests.csproj        # All UI tests (HicPlugin + RdmpExtensions)

/RdmpDicom/ (SEPARATE - Published as NuGet package)
├── Rdmp.Dicom.csproj              # Stays separate
├── Rdmp.Dicom.UI.csproj           # Stays separate
├── Rdmp.Dicom.Tests.csproj        # Stays separate
└── Rdmp.Dicom.sln                 # Separate solution
```

## What to Migrate

### HicPlugin Components (TO BE MIGRATED)
- **SCIStorePlugin** - Tayside SCI Store integration
  - `SCIStorePlugin.*` namespaces
- **DrsPlugin** - Laboratory system integration
  - `DrsPlugin.*` namespaces
- **GoDartsPlugin** - Diabetes data integration
  - `GoDartsPlugin.*` namespaces
- **HIC.Demography** - Demographic data handling
  - `HIC.Demography.*` namespaces
- **InterfaceToJira** - JIRA interface
  - `InterfaceToJira.*` namespaces
- **JiraPlugin** - JIRA plugin
  - `JiraPlugin.*` namespaces

### RdmpExtensions Components (TO BE MIGRATED)
- **LoadModules.Extensions.AutomationPlugins**
  - `LoadModules.Extensions.AutomationPlugins.*` namespaces

### RdmpDicom Components (STAY SEPARATE)
- **Rdmp.Dicom** - Core DICOM functionality
  - `Rdmp.Dicom.*` namespaces
- **Rdmp.Dicom.UI** - UI components
  - `Rdmp.Dicom.UI.*` namespaces

## Migration Steps

### Phase 1: Update Project Files
1. **Create consolidated HicPlugin + RdmpExtensions projects**
2. **Exclude RdmpDicom from consolidation**
3. **Keep RdmpDicom as separate external dependency**

### Phase 2: Update References
1. **Update Rdmp.Core.Tests** to reference:
   ```xml
   <ProjectReference Include="..\Plugins\Plugins.csproj" />
   <ProjectReference Include="..\Plugins\Plugins.Tests.csproj" />
   <!-- Keep RdmpDicom as external if needed for tests -->
   ```

2. **Update main RDMP solution** to include:
   - New consolidated plugin projects
   - Remove old HicPlugin project references
   - Keep RdmpDicom as external dependency

### Phase 3: Handle Dependencies
1. **SCIStorePlugin WCF service references** need special handling
2. **Consider modernizing WCF to gRPC/REST if possible**
3. **Or include System.ServiceModel.Primitives package**

### Phase 4: Testing
1. Run tests to ensure namespaces are preserved
2. Verify MEF loading works
3. Confirm no breaking changes for consumers

## Benefits

1. **Cleaner structure**: 3 main projects instead of 15+
2. **Preserves RdmpDicom independence**: Remains separate NuGet package
3. **Namespace preservation**: No breaking changes
4. **Simplified dependencies**: Clear separation of concerns

## Notes

- RdmpDicom should NOT be included in the consolidated plugin projects
- RdmpDicom remains a separate product with its own versioning and release cycle
- HicPlugin and RdmpExtensions are internal RDMP plugins suitable for consolidation
- Tests that require RdmpDicom should reference it as an external package