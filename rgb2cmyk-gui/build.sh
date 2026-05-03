#!/bin/bash
set -e
SELF="$(dirname "$0")"
source "$SELF/setup.sh"
unset LD_PRELOAD

echo "Compilando para Linux x64..."
dotnet publish src/rgb2cmyk-gui.csproj -c Release -r linux-x64 --self-contained -o publish/linux-x64

echo "Compilando para Windows x64..."
dotnet publish src/rgb2cmyk-gui.csproj -c Release -r win-x64 --self-contained -o publish/win-x64

echo "Compilando para macOS ARM64..."
dotnet publish src/rgb2cmyk-gui.csproj -c Release -r osx-arm64 --self-contained -o publish/osx-arm64

echo "Empaquetando macOS .app como .zip..."
cd publish/osx-arm64 && zip -r ../rgb2cmyk-gui-macOS-arm64.zip rgb2cmyk-gui.app && cd "$SELF"

echo "Build completo."
ls -la publish/
