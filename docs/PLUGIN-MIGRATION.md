# Plugin Migration Strategy

## Overview

This document describes the migration strategy for consolidating the three RDMP plugins (HicPlugin, RdmpDicom, RdmpExtensions) into a cleaner structure with just 4 projects while preserving namespaces.

## New Project Structure

```
/Plugins/
├── Plugins.csproj                 # All non-UI plugin code
├── Plugins.UI.csproj              # All UI plugin code
├── Plugins.Tests.csproj           # All non-UI tests
└── Plugins.UI.Tests.csproj        # All UI tests
```

## Namespace Preservation

The migration **preserves all original namespaces**:

- **HicPlugin Components**:
  - `SCIStorePlugin.*` - Tayside SCI Store integration
  - `DrsPlugin.*` - Laboratory system integration
  - `GoDartsPlugin.*` - Diabetes data integration
  - `HIC.Demography.*` - Demographic data handling
  - `InterfaceToJira.*` - JIRA interface
  - `JiraPlugin.*` - JIRA plugin

- **RdmpDicom Components**:
  - `Rdmp.Dicom.*` - All DICOM functionality

- **RdmpExtensions Components**:
  - `LoadModules.Extensions.*` - Automation extensions

## Migration Steps

### Phase 1: Update Project References

1. **Update Rdmp.Core.Tests** to reference the new plugin projects:
   ```xml
   <!-- Replace old plugin references -->
   <ProjectReference Include="..\Plugins\Plugins.csproj" />
   <ProjectReference Include="..\Plugins\Plugins.Tests.csproj" />
   ```

2. **Update Rdmp.UI** to reference UI plugins:
   ```xml
   <ProjectReference Include="..\Plugins\Plugins.csproj" />
   <ProjectReference Include="..\Plugins\Plugins.UI.csproj" />
   ```

3. **Update main RDMP solution** to include new projects instead of individual plugin projects.

### Phase 2: Build and Test

1. Build all 4 new plugin projects
2. Run tests to ensure all namespaces are preserved
3. Verify MEF loading works correctly with new assemblies

### Phase 3: Cleanup

Once migration is verified:
1. Remove old plugin directories:
   - `Plugins/HicPlugin/`
   - `Plugins/RdmpDicom/`
   - `Plugins/RdmpExtensions/`

2. Update CI/CD to build new projects

3. Update documentation to reflect new structure

## Benefits

1. **Cleaner Solution Structure**: 4 projects instead of 15+ individual plugin projects
2. **Preserved Namespaces**: No breaking changes for consumers of plugin APIs
3. **Simplified Dependencies**: Clear separation between UI and non-UI components
4. **Easier Maintenance**: Consolidated test structure
5. **Better CI/CD**: Simpler build process

## Implementation Notes

- The new projects use MSBuild `<Link>` to maintain virtual directory structure
- This ensures namespace preservation while consolidating code
- UI-specific dependencies are isolated to UI projects
- Test projects are separated by UI/non-UI for cross-platform compatibility

## Validation

To verify the migration works correctly:

1. All original namespaces should still be accessible
2. MEF composition should discover all plugin types
3. Tests should run successfully with the new project structure
4. UI components should load correctly in the main application

## Future Considerations

- This approach makes it easier to eventually move plugins into core if needed
- The consolidated structure is more maintainable than the distributed plugin system
- Tests can be run independently on Linux (non-UI) and Windows (UI)

[Project]: ../Documentation/CodeTutorials/Glossary.md#Project
