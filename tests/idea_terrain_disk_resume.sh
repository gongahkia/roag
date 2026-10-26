#!/bin/sh
set -eu
cd "$(dirname "$0")/.."
for kind in heat push fragile; do
 fixture_dir=$(mktemp -d /var/tmp/roag-terrain-disk-XXXXXX)
 luajit tests/idea_terrain_disk_fixture.lua write "$fixture_dir" "$kind"
 luajit tests/idea_terrain_disk_fixture.lua continue "$fixture_dir" "$kind"
 printf 'Fixture artifacts: %s\n' "$fixture_dir"
done
