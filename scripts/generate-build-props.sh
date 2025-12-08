#!/bin/bash
# Generate directory-specific Directory.Build.props files with dynamic target framework values
# based on .NET SDK version. If any files differ from what's in git, commit and push, then exit with error.
#
# RDMP project structure:
# - Libraries (multi-target): Rdmp.Core, Tests.Common, RdmpDicom/Rdmp.Dicom
# - Windows Libraries (multi-target -windows): Rdmp.UI, Plugins/Plugins.UI, RdmpDicom/Rdmp.Dicom.UI
# - Tests/Tools/Apps (single target latest): Everything else
# - Special: Rdmp.Core.Generators stays netstandard2.0 (Roslyn requirement)

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$REPO_ROOT"

# Create a temporary project to query SDK properties
TEMP_DIR=$(mktemp -d)
TEMP_PROJ="$TEMP_DIR/temp.csproj"

cat > "$TEMP_PROJ" << 'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>
EOF

# Get NETCoreAppMaximumVersion from SDK
MAX_VERSION=$(dotnet msbuild "$TEMP_PROJ" -getProperty:NETCoreAppMaximumVersion 2>/dev/null | tail -1 | tr -d ' ')

# Clean up temp project
rm -rf "$TEMP_DIR"

# Extract major version (e.g., "10.0" -> "10")
MAX_MAJOR=$(echo "$MAX_VERSION" | cut -d. -f1)

# Validate that we got a valid version number
if [ -z "$MAX_MAJOR" ] || ! [[ "$MAX_MAJOR" =~ ^[0-9]+$ ]]; then
    echo "ERROR: Failed to detect .NET SDK version. Got: '$MAX_VERSION'" >&2
    exit 1
fi

echo "Detected .NET SDK maximum version: $MAX_VERSION (major: $MAX_MAJOR)"

# Determine minimum supported major version based on SDK version
# .NET 8 LTS until Nov 2026, .NET 10 LTS until Nov 2028
# We support: current LTS (8) through current SDK version
if [ "$MAX_MAJOR" -eq 8 ] || [ "$MAX_MAJOR" -eq 9 ] || [ "$MAX_MAJOR" -eq 10 ]; then
    MIN_MAJOR=8
elif [ "$MAX_MAJOR" -eq 11 ] || [ "$MAX_MAJOR" -eq 12 ]; then
    MIN_MAJOR=10
elif [ "$MAX_MAJOR" -eq 13 ]; then
    MIN_MAJOR=11
else
    # Fallback for unknown versions
    MIN_MAJOR=$MAX_MAJOR
fi

# Build list of supported frameworks for libraries
LIB_FRAMEWORKS=""
for v in $(seq $MIN_MAJOR $MAX_MAJOR); do
    if [ -n "$LIB_FRAMEWORKS" ]; then
        LIB_FRAMEWORKS="${LIB_FRAMEWORKS};net${v}.0"
    else
        LIB_FRAMEWORKS="net${v}.0"
    fi
done

# Build list of supported frameworks for Windows libraries
WIN_FRAMEWORKS=""
for v in $(seq $MIN_MAJOR $MAX_MAJOR); do
    if [ -n "$WIN_FRAMEWORKS" ]; then
        WIN_FRAMEWORKS="${WIN_FRAMEWORKS};net${v}.0-windows"
    else
        WIN_FRAMEWORKS="net${v}.0-windows"
    fi
done

echo "Target frameworks for libraries: $LIB_FRAMEWORKS"
echo "Target frameworks for Windows libraries: $WIN_FRAMEWORKS"
echo "Target framework for tests/tools/apps: net${MAX_MAJOR}.0"

CHANGES_MADE=false
PROPS_FILES=""

# Function to generate library props file (multi-target)
generate_library_props() {
    local TARGET_DIR="$1"
    local TARGET_FILE="$TARGET_DIR/Directory.Build.props"
    local TEMP_FILE=$(mktemp)

    cat > "$TEMP_FILE" << EOF
<Project>
  <!-- Import parent props -->
  <Import Project="\$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '\$(MSBuildThisFileDirectory)../'))" />

  <!-- Library projects multi-target all non-EOL .NET versions -->
  <!-- Auto-generated based on SDK version by scripts/generate-build-props.sh -->
  <PropertyGroup>
    <TargetFrameworks>$LIB_FRAMEWORKS</TargetFrameworks>
  </PropertyGroup>
</Project>
EOF

    if ! diff -q "$TARGET_FILE" "$TEMP_FILE" > /dev/null 2>&1; then
        echo "$TARGET_FILE needs updating for current .NET SDK version"
        mv "$TEMP_FILE" "$TARGET_FILE"
        CHANGES_MADE=true
        PROPS_FILES="$PROPS_FILES $TARGET_FILE"
    else
        rm -f "$TEMP_FILE"
    fi
}

# Function to generate Windows library props file (multi-target with -windows)
generate_windows_library_props() {
    local TARGET_DIR="$1"
    local TARGET_FILE="$TARGET_DIR/Directory.Build.props"
    local TEMP_FILE=$(mktemp)

    cat > "$TEMP_FILE" << EOF
<Project>
  <!-- Import parent props -->
  <Import Project="\$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '\$(MSBuildThisFileDirectory)../'))" />

  <!-- Windows library projects multi-target all non-EOL .NET versions with -windows suffix -->
  <!-- Auto-generated based on SDK version by scripts/generate-build-props.sh -->
  <PropertyGroup>
    <TargetFrameworks>$WIN_FRAMEWORKS</TargetFrameworks>
  </PropertyGroup>
</Project>
EOF

    if ! diff -q "$TARGET_FILE" "$TEMP_FILE" > /dev/null 2>&1; then
        echo "$TARGET_FILE needs updating for current .NET SDK version"
        mv "$TEMP_FILE" "$TARGET_FILE"
        CHANGES_MADE=true
        PROPS_FILES="$PROPS_FILES $TARGET_FILE"
    else
        rm -f "$TEMP_FILE"
    fi
}

# Function to generate single-target props file for tests/tools/apps
generate_single_target_props() {
    local TARGET_DIR="$1"
    local TARGET_FILE="$TARGET_DIR/Directory.Build.props"
    local TEMP_FILE=$(mktemp)

    cat > "$TEMP_FILE" << EOF
<Project>
  <!-- Import parent props -->
  <Import Project="\$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '\$(MSBuildThisFileDirectory)../'))" />

  <!-- Non-library projects target only the latest .NET version -->
  <!-- Auto-generated based on SDK version by scripts/generate-build-props.sh -->
  <PropertyGroup>
    <TargetFramework>net${MAX_MAJOR}.0</TargetFramework>
  </PropertyGroup>
</Project>
EOF

    if ! diff -q "$TARGET_FILE" "$TEMP_FILE" > /dev/null 2>&1; then
        echo "$TARGET_FILE needs updating for current .NET SDK version"
        mv "$TEMP_FILE" "$TARGET_FILE"
        CHANGES_MADE=true
        PROPS_FILES="$PROPS_FILES $TARGET_FILE"
    else
        rm -f "$TEMP_FILE"
    fi
}

# Function to generate single-target Windows props file for tests/apps
generate_single_target_windows_props() {
    local TARGET_DIR="$1"
    local TARGET_FILE="$TARGET_DIR/Directory.Build.props"
    local TEMP_FILE=$(mktemp)

    cat > "$TEMP_FILE" << EOF
<Project>
  <!-- Import parent props -->
  <Import Project="\$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '\$(MSBuildThisFileDirectory)../'))" />

  <!-- Windows application/test projects target only the latest .NET version -->
  <!-- Auto-generated based on SDK version by scripts/generate-build-props.sh -->
  <PropertyGroup>
    <TargetFramework>net${MAX_MAJOR}.0-windows</TargetFramework>
  </PropertyGroup>
</Project>
EOF

    if ! diff -q "$TARGET_FILE" "$TEMP_FILE" > /dev/null 2>&1; then
        echo "$TARGET_FILE needs updating for current .NET SDK version"
        mv "$TEMP_FILE" "$TARGET_FILE"
        CHANGES_MADE=true
        PROPS_FILES="$PROPS_FILES $TARGET_FILE"
    else
        rm -f "$TEMP_FILE"
    fi
}

# Generate props for library projects (multi-target)
generate_library_props "Rdmp.Core"
generate_library_props "Tests.Common"

# Generate props for Windows library projects (multi-target with -windows)
generate_windows_library_props "Rdmp.UI"
generate_windows_library_props "Plugins/Plugins.UI"

# RdmpDicom library projects (multi-target now that DicomTypeTranslation 5.0.1 supports net8/9/10)
generate_library_props "RdmpDicom/Rdmp.Dicom"
generate_windows_library_props "RdmpDicom/Rdmp.Dicom.UI"

# Generate props for test projects (single target latest)
generate_single_target_props "Rdmp.Core.Tests"
generate_single_target_props "RdmpDicom/Rdmp.Dicom.Tests"

# Generate props for Windows test projects (single target latest with -windows)
generate_single_target_windows_props "Rdmp.UI.Tests"
generate_single_target_windows_props "Plugins/Plugins.UI.Tests"

# Generate props for tool projects (single target latest)
generate_single_target_props "Tools/rdmp"

# Generate props for Windows application (single target latest with -windows)
generate_single_target_windows_props "Application/ResearchDataManagementPlatform"

# Generate props for metadata/placeholder projects (single target latest)
generate_single_target_props "Plugins/Plugins"
generate_single_target_props "Plugins/Plugins.Tests"

# If changes were made and we're in CI, commit and push
if [ "$CHANGES_MADE" = true ]; then
    if [ -d .git ] && [ -n "$CI" ]; then
        git config user.name "github-actions[bot]"
        git config user.email "github-actions[bot]@users.noreply.github.com"
        find . -name 'Directory.Build.props' -not -path './Directory.Build.props' -print0 | xargs -0 git add
        git commit -m "Update Directory.Build.props files for .NET SDK version"
        git push
        echo "ERROR: Directory.Build.props files were out of date and have been updated."
        echo "The changes have been committed and pushed. Please retry the workflow."
        exit 1
    else
        echo "Updated props files locally. Please commit the changes."
        exit 0
    fi
else
    echo "All Directory.Build.props files are up to date"
    exit 0
fi
