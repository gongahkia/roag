#!/bin/sh
set -eu
cd "$(dirname "$0")"
project=src/Ithica.Client
for argument in "$@"; do
    case "$argument" in
        --headless|--story-check|--benchmark|--validate-seeds|--generation-report|--campaign-benchmark) project=src/Ithica.Tools ;;
        --preview) exec ./tools/dotnet.sh run --project src/Ithica.Client --configuration Release -- "$@" ;;
    esac
done
exec ./tools/dotnet.sh run --project "$project" --configuration Release -- "$@"
