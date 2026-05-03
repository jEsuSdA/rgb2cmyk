#!/bin/bash
set -e

SELF="${BASH_SOURCE[0]:-$0}"
PROJECT_DIR="$(cd "$(dirname "$SELF")" && pwd)"
DOTNET_DIR="$PROJECT_DIR/.dotnet"
DOTNET_BIN="$DOTNET_DIR/dotnet"

export DOTNET_ROOT="$DOTNET_DIR"
export PATH="$DOTNET_DIR:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export NUGET_PACKAGES="$PROJECT_DIR/.nuget"

if [ ! -f "$DOTNET_BIN" ]; then
    echo "Descargando .NET 8 SDK en $DOTNET_DIR ..."
    curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin \
        --channel 8.0 \
        --install-dir "$DOTNET_DIR"
    echo "SDK instalado: $($DOTNET_BIN --version)"
fi

echo "Entorno listo. dotnet --version: $($DOTNET_BIN --version)"
echo "Para compilar: ./build.sh"
echo "Para ejecutar:  ./run.sh"
