#!/bin/bash
# Auto-patch RdmpDicom .csproj files to use local Rdmp.Core and FAnsiSql.Legacy
# Usage: ./scripts/patch-rdmpdicom-references.sh

set -e

RDMPDICOM_DIR="Plugins/RdmpDicom"
RDMP_CORE_PATH="../../../Rdmp.Core/Rdmp.Core.csproj"

echo "Patching RdmpDicom project references..."

# Find all .csproj files in RdmpDicom
find "$RDMPDICOM_DIR" -name "*.csproj" | while read -r csproj; do
    echo "Processing: $csproj"

    # Create backup
    cp "$csproj" "$csproj.bak"

    # 1. Replace old FAnsi package names with FAnsiSql.Legacy (remove version to use central)
    perl -i -pe '
        # Replace FAnsi package references with FAnsiSql.Legacy
        s/<PackageReference\s+Include="FAnsi"[^>]*\/>/<PackageReference Include="FAnsiSql.Legacy" \/>/g;
        s/<PackageReference\s+Include="FAnsi"[^>]*>/<PackageReference Include="FAnsiSql.Legacy">/g;

        # Remove explicit version from FAnsiSql.Legacy to use central version
        s/<PackageReference\s+Include="FAnsiSql\.Legacy"\s+Version="[^"]*"\s*\/>/<PackageReference Include="FAnsiSql.Legacy" \/>/g;
    ' "$csproj"

    # 2. Replace Rdmp.Core PackageReference with ProjectReference
    if grep -q 'PackageReference.*Rdmp\.Core' "$csproj"; then
        echo "  → Converting Rdmp.Core PackageReference to ProjectReference"

        # Calculate relative path based on project location
        PROJECT_DIR=$(dirname "$csproj")
        REL_PATH=$(realpath --relative-to="$PROJECT_DIR" "Rdmp.Core/Rdmp.Core.csproj")

        perl -i -pe "
            # Replace Rdmp.Core PackageReference with ProjectReference
            s|<PackageReference\s+Include=\"Rdmp\.Core\"[^/]*/?>|<ProjectReference Include=\"$REL_PATH\" />|g;
        " "$csproj"
    fi

    # 3. Ensure no explicit FAnsiSql.Legacy versions (use central management)
    if grep -q 'FAnsiSql\.Legacy.*Version=' "$csproj"; then
        echo "  → Removing explicit FAnsiSql.Legacy version (using central management)"
        perl -i -pe '
            s/(<PackageReference\s+Include="FAnsiSql\.Legacy")\s+Version="[^"]*"/\1/g;
        ' "$csproj"
    fi

    echo "  ✓ Patched successfully"
done

echo ""
echo "✓ All RdmpDicom projects patched!"
echo ""
echo "Summary of changes:"
echo "  • FAnsi → FAnsiSql.Legacy (using central version 3.3.1)"
echo "  • Rdmp.Core PackageReference → ProjectReference to local project"
echo ""
echo "Backups saved as .csproj.bak files"
