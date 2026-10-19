#!/bin/sh
set -eu
cd "$(dirname "$0")/.."
export DOTNET_NOLOGO=1
if command -v dotnet >/dev/null 2>&1; then
    exec dotnet "$@"
fi
local_sdk="$HOME/.local/share/regolith/dotnet/dotnet"
if [ -x "$local_sdk" ]; then
    exec "$local_sdk" "$@"
fi
echo 'Regolith needs the .NET 10 SDK. Install it from https://dotnet.microsoft.com/download/dotnet/10.0 or run ./tools/setup-dotnet.sh.' >&2
exit 1
