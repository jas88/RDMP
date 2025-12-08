# NUnit3TestAdapter 6.0 Migration Guide

This document outlines the steps required to upgrade from NUnit3TestAdapter 5.x to 6.0.0, which introduces breaking changes due to the adoption of Microsoft Testing Platform (MTP) v2.

## Overview

NUnit3TestAdapter 6.0.0 is a major release that:
- Switches from VSTest to Microsoft Testing Platform (MTP) v2
- Drops support for .NET Core 3.1 through .NET 7
- Requires .NET 8.0 as the minimum version (.NET Framework 4.6.2 still supported)

## Prerequisites

Before upgrading, ensure:
- All test projects target .NET 8.0 or higher (RDMP currently targets net10.0 ✓)
- Visual Studio 17.12 or later for full MTP protocol support
- .NET SDK 8.0 or later

## Migration Steps

### Step 1: Update Directory.Packages.props

Update the NUnit3TestAdapter version:

```xml
<PackageVersion Include="NUnit3TestAdapter" Version="6.0.0" />
```

### Step 2: Enable NUnit Runner (Optional but Recommended)

Add to `Directory.Build.props` in the repository root or each test project:

```xml
<PropertyGroup>
  <EnableNUnitRunner>true</EnableNUnitRunner>
  <OutputType>Exe</OutputType>
</PropertyGroup>
```

**Properties explained:**
- `EnableNUnitRunner` - Enables the Microsoft Testing Platform integration
- `OutputType>Exe` - Required for MTP; the test assembly becomes its own test runner

### Step 3: Enable dotnet test Support

For compatibility with `dotnet test`, add:

```xml
<PropertyGroup>
  <TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>
</PropertyGroup>
```

### Step 4: .NET 10 SDK Configuration

If using .NET 10 SDK, create or update `global.json` in the repository root:

```json
{
  "sdk": {
    "version": "10.0.100"
  },
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

This opts into the MTP-based `dotnet test` implementation required for .NET 10.

### Step 5: Update CI Workflows

The existing `dotnet test` commands should continue to work, but verify that:
- Test discovery works correctly
- Code coverage collection still functions
- Test filters (`--filter`) behave as expected

## RDMP-Specific Considerations

### Test Projects Affected

| Project | Target Framework | Notes |
|---------|-----------------|-------|
| Rdmp.Core.Tests | net10.0 | Main test suite |
| Rdmp.UI.Tests | net10.0-windows | Windows-only UI tests |
| Plugins.Tests | net10.0 | Plugin tests |
| Plugins.UI.Tests | net10.0-windows | Plugin UI tests |
| Rdmp.Dicom.Tests | net10.0 | DICOM functionality tests |

### Current Package Versions

```xml
<PackageVersion Include="NUnit" Version="4.4.0" />
<PackageVersion Include="NUnit3TestAdapter" Version="5.2.0" />  <!-- Current -->
<PackageVersion Include="Microsoft.NET.Test.Sdk" Version="18.0.1" />
```

### Recommended Final Configuration

After migration, `Directory.Build.props` for test projects should include:

```xml
<PropertyGroup>
  <EnableNUnitRunner>true</EnableNUnitRunner>
  <OutputType>Exe</OutputType>
  <TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>
</PropertyGroup>
```

## Potential Issues

### 1. Test Discovery Failures
If tests aren't discovered, ensure `EnableNUnitRunner` and `OutputType` are set correctly.

### 2. Code Coverage
Verify that `coverlet.collector` still works with MTP. May need updates.

### 3. Parallel Test Execution
MTP has different parallelization behavior. Test thoroughly.

### 4. Test Filters
Some filter syntax may differ. Verify CI filter expressions.

## Rollback Plan

If issues occur, revert to the stable configuration:

```xml
<PackageVersion Include="NUnit3TestAdapter" Version="5.2.0" />
```

And remove any MTP-specific properties added during migration.

## References

- [NUnit and Microsoft.Testing.Platform](https://docs.nunit.org/articles/vs-test-adapter/NUnit-And-Microsoft-Test-Platform.html)
- [NUnit3TestAdapter Release Notes](https://docs.nunit.org/articles/vs-test-adapter/AdapterV4-Release-Notes.html)
- [Microsoft Testing Platform v1 to v2 Migration](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-migration-from-v1-to-v2)
- [VSTest vs Microsoft.Testing.Platform](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-platform-vs-vstest)
- [NUnit Runner Introduction](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-nunit-runner-intro)

## Migration Checklist

- [ ] Update NUnit3TestAdapter to 6.0.0 in Directory.Packages.props
- [ ] Add EnableNUnitRunner and OutputType properties
- [ ] Add TestingPlatformDotnetTestSupport property
- [ ] Update global.json for .NET 10 SDK (if applicable)
- [ ] Run tests locally on Windows
- [ ] Run tests locally on Linux
- [ ] Verify CI passes on all platforms
- [ ] Verify code coverage still works
- [ ] Verify Test Explorer integration in Visual Studio
