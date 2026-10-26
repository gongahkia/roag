#!/bin/sh
# One isolated real game process per form, using the shared geometry GUI check.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
if [ "$#" -eq 0 ];then
    set -- pommel_return needle_flight hollow_sigil backswing distant_bastion volatile_vial split_thrust
fi
for form do
    ROAG_PATTERN_CASE="$form" sh "$project_dir/tests/idea_pattern_gui_smoke.sh"
done
