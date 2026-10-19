#!/bin/sh
# Separate LuaJIT processes write/continue every form with checked generations.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
cd "$project_dir"
if [ "$#" -eq 0 ];then
    set -- pommel_return needle_flight hollow_sigil backswing distant_bastion volatile_vial split_thrust
fi
for form do
    fixture_dir=$(mktemp -d /var/tmp/roag-boons-disk-XXXXXX)
    luajit tests/idea_boon_disk_fixture.lua write "$fixture_dir" "form_$form"
    luajit tests/idea_boon_disk_fixture.lua continue "$fixture_dir" "form_$form"
    printf 'Retained weapon form fixture: %s\n' "$fixture_dir"
done
