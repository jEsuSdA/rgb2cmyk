#!/bin/bash
set -e
PROJECT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "$PROJECT_DIR/setup.sh"
unset LD_PRELOAD

echo "Compilando para Linux x64..."
dotnet publish src/rgb2cmyk-gui.csproj -c Release -r linux-x64 --self-contained -o publish/linux-x64

echo "Empaquetando Linux x64 como .tar.gz..."
cd publish/linux-x64 && tar -czf ../rgb2cmyk-gui-linux-x64.tar.gz . && cd "$PROJECT_DIR"

echo "Compilando para Windows x64..."
dotnet publish src/rgb2cmyk-gui.csproj -c Release -r win-x64 --self-contained -o publish/win-x64

echo "Empaquetando Windows x64 como .zip..."
cd publish/win-x64 && zip -r ../rgb2cmyk-gui-win-x64.zip . && cd "$PROJECT_DIR"

echo "Compilando para macOS ARM64..."
dotnet publish src/rgb2cmyk-gui.csproj -c Release -r osx-arm64 --self-contained -o publish/osx-arm64

echo "Empaquetando macOS .app como .zip..."
cd publish/osx-arm64 && zip -r ../rgb2cmyk-gui-macOS-arm64.zip rgb2cmyk-gui.app && cd "$PROJECT_DIR"

echo "Build completo."
ls -la publish/
