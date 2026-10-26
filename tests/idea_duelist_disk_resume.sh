#!/bin/sh
set -eu
cd "$(dirname "$0")/.."
fixture_dir=$(mktemp -d /var/tmp/roag-duelist-disk-XXXXXX)
luajit tests/idea_duelist_disk_fixture.lua write "$fixture_dir"
luajit tests/idea_duelist_disk_fixture.lua continue "$fixture_dir"
printf 'Fixture artifacts: %s\n' "$fixture_dir"
