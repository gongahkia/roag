#!/bin/sh
# Compatibility/regression preview for the original expedition prototype.
set -eu
cd "$(dirname "$0")"
exec ./tools/dotnet.sh run --project src/Ithica.Client --configuration Release -- --preview "$@"
