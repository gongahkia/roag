#!/bin/sh
set -eu
cd "$(dirname "$0")"
project=src/Regolith.Client
for argument in "$@"; do
    case "$argument" in
        --headless|--benchmark) project=src/Regolith.Tools ;;
    esac
done
exec ./tools/dotnet.sh run --project "$project" --configuration Release -- "$@"
