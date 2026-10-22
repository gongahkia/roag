#!/bin/sh
set -eu
cd "$(dirname "$0")"
exec ./tools/dotnet.sh run --project src/Vagabonds.Client --configuration Release -- "$@"
