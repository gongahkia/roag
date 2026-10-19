#!/bin/sh
set -eu
cd "$(dirname "$0")/.."
if [ "$#" -eq 0 ]; then set -- supply legacy vow dice handoff patterns; fi
for scenario do
    fixture_dir=$(mktemp -d /var/tmp/roag-boons-disk-XXXXXX)
    luajit tests/idea_boon_disk_fixture.lua write "$fixture_dir" "$scenario"
    luajit tests/idea_boon_disk_fixture.lua continue "$fixture_dir" "$scenario"
    printf 'Retained boon fixture: %s\n' "$fixture_dir"
done
