#!/bin/sh
set -eu
cd "$(dirname "$0")/.."
for scenario in rammer wraith cantor toad cache bones alarm web oath sentinel mirror silt; do
    fixture_dir=$(mktemp -d /var/tmp/roag-idea-disk-XXXXXX)
    luajit tests/idea_enemy_disk_fixture.lua write "$fixture_dir" "$scenario"
    luajit tests/idea_enemy_disk_fixture.lua continue "$fixture_dir" "$scenario"
    if [ "$scenario" = web ] || [ "$scenario" = mirror ]; then
        luajit tests/idea_enemy_disk_fixture.lua cross "$fixture_dir" "$scenario"
    fi
    printf 'Fixture artifacts: %s\n' "$fixture_dir"
done
