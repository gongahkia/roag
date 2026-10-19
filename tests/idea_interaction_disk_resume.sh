#!/bin/sh
set -eu
cd "$(dirname "$0")/.."
if [ "$#" -eq 0 ]; then set -- utility cleanse_echo forge bells token fixture contract; fi
for scenario do
 fixture_dir=$(mktemp -d /var/tmp/roag-items-disk-XXXXXX)
 luajit tests/idea_interaction_disk_fixture.lua write "$fixture_dir" "$scenario"
 luajit tests/idea_interaction_disk_fixture.lua continue "$fixture_dir" "$scenario"
 printf 'Fixture artifacts: %s\n' "$fixture_dir"
done
