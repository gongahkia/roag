#!/bin/sh
set -eu
cd "$(dirname "$0")/.."
export DOTNET_NOLOGO=1
if command -v dotnet >/dev/null 2>&1; then
    exec dotnet "$@"
fi
for ithica_sdk in "$HOME/.local/share/ithica/dotnet/dotnet" "$HOME/.local/share/regolith/dotnet/dotnet"; do
    if [ -x "$ithica_sdk" ]; then
        exec "$ithica_sdk" "$@"
    fi
done
echo 'Ithica needs the .NET 10 SDK. Install it from https://dotnet.microsoft.com/download/dotnet/10.0 or run ./tools/setup-dotnet.sh.' >&2
exit 1
