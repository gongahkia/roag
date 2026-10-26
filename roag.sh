#!/bin/sh
set -eu
cd "$(dirname "$0")"
exec ./tools/dotnet.sh run --project src/Roag.Client --configuration Release -- "$@"
