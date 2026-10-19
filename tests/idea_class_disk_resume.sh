#!/bin/sh
set -eu
cd "$(dirname "$0")/.."
for class in "${@:-geomancer}"; do
 fixture_dir=$(mktemp -d /var/tmp/roag-class-disk-XXXXXX)
 luajit tests/idea_class_disk_fixture.lua write "$fixture_dir" "$class"
 luajit tests/idea_class_disk_fixture.lua continue "$fixture_dir" "$class"
 printf 'Fixture artifacts: %s\n' "$fixture_dir"
done
