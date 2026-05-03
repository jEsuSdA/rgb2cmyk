#!/bin/bash
set -e
source "$(dirname "$0")/setup.sh"
unset LD_PRELOAD
dotnet run --project src/rgb2cmyk-gui.csproj
