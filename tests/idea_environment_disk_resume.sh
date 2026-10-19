#!/bin/sh
set -eu
cd "$(dirname "$0")/.."
for style in kenney onebit; do
 fixture_dir=$(mktemp -d /var/tmp/roag-environment-disk-XXXXXX)
 luajit tests/idea_environment_disk_fixture.lua write "$fixture_dir" "$style"
 luajit tests/idea_environment_disk_fixture.lua continue "$fixture_dir" "$style"
 printf 'Fixture artifacts: %s\n' "$fixture_dir"
done
