#!/bin/bash
# Generate directory-specific Directory.Build.props files with dynamic target framework values
# based on .NET SDK version. All projects target only the latest .NET version.
# If any files differ from what's in git, commit and push, then exit with error.
#
# The target framework is determined by the installed SDK (via NETCoreAppMaximumVersion).
# To upgrade (e.g. net10→net11), update global.json to the new SDK version and re-run
# this script — it will regenerate the props files automatically.
#
# RDMP project structure:
# - All projects target only the latest .NET version (single target)
# - Windows projects use -windows TFM suffix
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
echo "Target framework: net${MAX_MAJOR}.0"

CHANGES_MADE=false
PROPS_FILES=""

# Function to generate single-target props file
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

# Function to generate single-target Windows props file
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

# All projects target only the latest .NET version
generate_single_target_props "Rdmp.Core"
generate_single_target_props "Tests.Common"
generate_single_target_props "RdmpDicom/Rdmp.Dicom"
generate_single_target_props "externals"

generate_single_target_windows_props "Rdmp.UI"
generate_single_target_windows_props "Plugins/Plugins.UI"
generate_single_target_windows_props "RdmpDicom/Rdmp.Dicom.UI"

# Test projects
generate_single_target_props "Rdmp.Core.Tests"
generate_single_target_props "RdmpDicom/Rdmp.Dicom.Tests"

generate_single_target_windows_props "Rdmp.UI.Tests"
generate_single_target_windows_props "Plugins/Plugins.UI.Tests"

# Tools
generate_single_target_props "Tools/rdmp"

# Windows application
generate_single_target_windows_props "Application/ResearchDataManagementPlatform"

# Metadata/placeholder projects
generate_single_target_props "Plugins/Plugins"
generate_single_target_props "Plugins/Plugins.Tests"

# If changes were made and we're in CI, commit and push
if [ "$CHANGES_MADE" = true ]; then
    if [ -d .git ] && [ -n "$CI" ]; then
        git config user.name "github-actions[bot]"
        git config user.email "github-actions[bot]@users.noreply.github.com"
        find . -name 'Directory.Build.props' -not -path './Directory.Build.props' -not -path './externals/npoi/*' -not -path './build-standards/*' -print0 | xargs -0 git add
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
