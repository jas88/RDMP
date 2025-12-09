# NUnit3TestAdapter 6.0 Migration Guide

This document outlines the upgrade from NUnit3TestAdapter 5.x to 6.0.0 with Microsoft Testing Platform (MTP).

## Overview

NUnit3TestAdapter 6.0.0 is a major release that:
- Uses Microsoft Testing Platform (MTP) v2 instead of VSTest
- Drops support for .NET Core 3.1 through .NET 7
- Requires .NET 8.0 as the minimum version (.NET Framework 4.6.2 still supported)

## Changes Made

### 1. Package Version (`Directory.Packages.props`)
```xml
<PackageVersion Include="NUnit3TestAdapter" Version="6.0.0" />
```

### 2. MTP Configuration (`Directory.Build.props`)
```xml
<!-- All test projects must use MTP when global.json specifies it -->
<!-- UI tests are excluded from Linux runs via linuxtests.slnf -->
<PropertyGroup Condition="$(MSBuildProjectName.EndsWith('Tests'))">
  <EnableNUnitRunner>true</EnableNUnitRunner>
  <OutputType>Exe</OutputType>
  <TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>
</PropertyGroup>
```

**Important**: When `global.json` specifies MTP as the test runner, ALL test projects must have MTP enabled - even Windows-only projects on Linux. Use solution filters (e.g., `linuxtests.slnf`) to control which tests actually run on each platform.

### 3. SDK Configuration (`global.json`)
**Note**: We do NOT use `global.json` to set the test runner because it requires ALL test projects to use MTP with no exclusions. Windows-only test projects (UI tests) cannot run on Linux, so we rely on MSBuild properties in `Directory.Build.props` instead.

If you have a single-platform project, you can optionally add:
```json
{
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

### 4. Test Project Changes
Remove `PrivateAssets` from NUnit3TestAdapter in test `.csproj` files to allow MTP compile-time dependencies:
```xml
<!-- Before -->
<PackageReference Include="NUnit3TestAdapter">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>...</IncludeAssets>
</PackageReference>

<!-- After -->
<PackageReference Include="NUnit3TestAdapter" />
```

### 5. CI Workflow Changes
MTP uses different CLI options than VSTest:

| VSTest | MTP |
|--------|-----|
| `--nologo` | (removed) |
| `--collect:"XPlat Code Coverage"` | `-- --coverage --coverage-output-format cobertura` |
| `-- DataCollectionRunSettings...` | `-- --coverage-output <path>` |

Example:
```bash
# Before (VSTest)
dotnet test --nologo --collect:"XPlat Code Coverage" --results-directory coverage

# After (MTP)
dotnet test -- --coverage --coverage-output-format cobertura --coverage-output coverage/coverage.xml
```

## References

- [NUnit and Microsoft.Testing.Platform](https://docs.nunit.org/articles/vs-test-adapter/NUnit-And-Microsoft-Test-Platform.html)
- [NUnit3TestAdapter Release Notes](https://docs.nunit.org/articles/vs-test-adapter/AdapterV4-Release-Notes.html)
- [Microsoft Testing Platform](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-platform-intro)
