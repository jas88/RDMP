#!/bin/bash
set -euo pipefail

# Determine version from SharedAssemblyInfo.cs
RDMP_VERSION=$(perl -ne 'print "$1" if /AssemblyInformationalVersion\("([0-9a-z.-]+)"\)/' SharedAssemblyInfo.cs)
echo "Building RDMP version: $RDMP_VERSION"
echo "rdmpversion=$RDMP_VERSION" >> "$GITHUB_OUTPUT"

# Bundle source code
echo "Bundling source code..."
mkdir -p Tools/BundleUpSourceIntoZip/output
rm -f Tools/BundleUpSourceIntoZip/output/SourceCodeForSelfAwareness.zip
find . -type f \( -name '*.cs' -o -name '*.xml' \) | rev | sort -t'/' -k1,1 -u | rev > srcbits.txt
7z a -mx=9 -mmt Tools/BundleUpSourceIntoZip/output/SourceCodeForSelfAwareness.zip @srcbits.txt

# Package applications
echo "Publishing applications..."
dotnet publish Application/ResearchDataManagementPlatform/ResearchDataManagementPlatform.csproj \
  -r win-x64 --self-contained -c Release -o PublishWinForms \
  -p:GenerateDocumentationFile=false -p:PublishSingleFile=true \
  -p:PublishReadyToRun=true -p:IncludeNativeLibrariesForSelfExtract=true \
  --verbosity minimal --nologo

dotnet publish Tools/rdmp/rdmp.csproj \
  -r win-x64 --self-contained -c Release -o PublishWindows \
  -p:GenerateDocumentationFile=false -p:PublishSingleFile=true \
  -p:PublishReadyToRun=true -p:IncludeNativeLibrariesForSelfExtract=true \
  --verbosity minimal --nologo

dotnet publish Tools/rdmp/rdmp.csproj \
  -r linux-x64 --self-contained -c Release -o PublishLinux \
  -p:GenerateDocumentationFile=false -p:PublishSingleFile=true \
  -p:PublishReadyToRun=true --verbosity minimal --nologo

# Bundle WinForms dependencies
echo "Bundling WinForms dependencies..."
BINDIR=Application/ResearchDataManagementPlatform/bin/Release/net9.0-windows/win-x64
cp -rt ./PublishWinForms \
  $BINDIR/runtimes \
  $BINDIR/x64 \
  $BINDIR/D3DCompiler_47_cor3.dll \
  $BINDIR/PenImc_cor3.dll \
  $BINDIR/PresentationNative_cor3.dll \
  $BINDIR/vcruntime140_cor3.dll \
  $BINDIR/WebView2Loader.dll \
  $BINDIR/wpfgfx_cor3.dll

# Install plugins
echo "Installing plugins..."
for plugin in \
  https://api.github.com/repos/SMI/RdmpDicom/releases/latest \
  https://api.github.com/repos/HICServices/HicPlugin/releases/latest \
  https://api.github.com/repos/HICServices/RdmpExtensions/releases/latest
do
  PluginName="$(cut -d/ -f6 <<< "$plugin")"
  NAME="$(curl -s "$plugin" | grep "browser_download_url.*$PluginName.*rdmp" | cut -d : -f 2,3 | cut -d '"' -f 2)"
  curl -OL "$NAME"
done

# Use hardlinks instead of copying to save space
for platform in PublishWindows PublishLinux PublishWinForms
do
  for rdmp in *.rdmp; do
    ln "$rdmp" "$platform/$rdmp"
  done
done
rm *.rdmp

# Sign executables (disabled for fork)
# if [ -n "${AZURE_KEY_VAULT_URI:-}" ]; then
#   echo "Signing executables..."
#   dotnet tool install --global AzureSignTool
#   AzureSignTool sign \
#     -kvu "$AZURE_KEY_VAULT_URI" \
#     -kvi "$AZURE_CLIENT_ID" \
#     -kvt "$AZURE_TENANT_ID" \
#     -kvs "$AZURE_CLIENT_SECRET" \
#     -kvc "$AZURE_CERT_NAME" \
#     -tr http://timestamp.digicert.com \
#     -v PublishWindows/rdmp.exe
#   AzureSignTool sign \
#     -kvu "$AZURE_KEY_VAULT_URI" \
#     -kvi "$AZURE_CLIENT_ID" \
#     -kvt "$AZURE_TENANT_ID" \
#     -kvs "$AZURE_CLIENT_SECRET" \
#     -kvc "$AZURE_CERT_NAME" \
#     -tr http://timestamp.digicert.com \
#     -v PublishWinForms/ResearchDataManagementPlatform.exe
# fi

# Create distribution archives
echo "Creating distribution archives..."
mkdir -p dist
chmod +x PublishLinux/rdmp

# Set compression level based on build type
if [[ "$GITHUB_REF" == refs/tags/v* ]]; then
  echo "Tagged release build - using maximum compression"
  COMPRESSION_LEVEL=9
  PIXZ_OPTS=""
else
  echo "Development build - using fast compression"
  COMPRESSION_LEVEL=1
  PIXZ_OPTS="-1"
fi

# Create archives with multithreaded compression
(cd PublishWindows && 7z a -mx=$COMPRESSION_LEVEL -mmt "../dist/rdmp-${RDMP_VERSION}-cli-win-x64.zip" .)
(cd PublishLinux && 7z a -mx=0 -mmt "../dist/rdmp-${RDMP_VERSION}-cli-linux-x64.zip" .)
(cd PublishWinForms && 7z a -mx=$COMPRESSION_LEVEL -mmt "../dist/rdmp-${RDMP_VERSION}-client.zip" .)

# Create tar.xz for Linux
mv PublishLinux "rdmp-${RDMP_VERSION}-cli-linux"
(cd "rdmp-${RDMP_VERSION}-cli-linux" && tar -cf - .) | pixz $PIXZ_OPTS > "dist/rdmp-${RDMP_VERSION}-cli-linux-x64.tar.xz"

# Build NuGet packages (tagged builds only)
if [[ "$GITHUB_REF" == refs/tags/v* ]]; then
  echo "Building NuGet packages..."
  for i in Rdmp.Core/Rdmp.Core.csproj Rdmp.UI/Rdmp.UI.csproj Tests.Common/Tests.Common.csproj
  do
    dotnet pack "$i" -c Release --include-symbols --nologo -o . -v:m -p:Version="$RDMP_VERSION"
  done

  # Calculate checksums
  echo "Calculating SHA256SUMS..."
  cd dist
  sha256sum * 2>/dev/null | grep -v 'SHA256SUMS' > SHA256SUMS || true
  cd ..
fi

echo "Build complete!"
