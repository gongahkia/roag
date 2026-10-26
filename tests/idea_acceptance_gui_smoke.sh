#!/bin/sh
# Prepared old mechanics, independent expected files, actual game keys/Continue.
set -eu
cd "$(dirname "$0")/.."
group=${1:-enemy}
shift || true
if [ "$#" -eq 0 ];then
 case "$group" in
 enemy) set -- rammer wraith cantor toad cache bones alarm web oath sentinel mirror silt moth;;
 class) set -- geomancer spiritbinder tinkerer stormcaller wayfinder;;
 duelist) set -- duelist;;
 items) set -- utility cleanse_echo forge bells token fixture contract offering_font ration_cache \
  charm_well travelling_trader memorial camp route_sign;;
 terrain) set -- heat push fragile;;
 *) exit 2;;
 esac
fi
for scenario do
 case "$group" in
 enemy) fixture=$(mktemp -d /var/tmp/roag-idea-disk-XXXXXX);driver=idea_enemy_disk_fixture;;
 class) fixture=$(mktemp -d /var/tmp/roag-class-disk-XXXXXX);driver=idea_class_disk_fixture;;
 duelist) fixture=$(mktemp -d /var/tmp/roag-duelist-disk-XXXXXX);driver=idea_duelist_disk_fixture;;
 items) fixture=$(mktemp -d /var/tmp/roag-items-disk-XXXXXX);driver=idea_interaction_disk_fixture;;
 terrain) fixture=$(mktemp -d /var/tmp/roag-terrain-disk-XXXXXX);driver=idea_terrain_disk_fixture;;
 *) exit 2;;
 esac
 luajit "tests/$driver.lua" write "$fixture" "$scenario"
 output=$(mktemp -d /var/tmp/roag-idea-acceptance-gui-XXXXXX)
 mkdir -p "$output/app/tests" "$output/app/tools" "$output/data"
 cp main.lua "$output/app/roag_main.lua"
 cp conf.lua "$output/app/conf.lua"
 cp -R src assets "$output/app/"
 cp tools/art_png.lua "$output/app/tools/"
 cp tests/idea_acceptance_gui.lua "$output/app/main.lua"
 for resume in 0 1;do
  ROAG_GUI_AUTOSTART=0 ROAG_IDEA_ACCEPTANCE_DIR="$fixture" \
   ROAG_IDEA_ACCEPTANCE_OUTPUT="$output" ROAG_IDEA_ACCEPTANCE_GROUP="$group" \
   ROAG_IDEA_ACCEPTANCE_CASE="$scenario" ROAG_IDEA_ACCEPTANCE_RESUME="$resume" \
   XDG_DATA_HOME="$output/data" timeout -k 3s 40s love "$output/app" > "$output/love-$resume.log" 2>&1 || {
    cat "$output/love-$resume.log"
    printf 'Retained failed acceptance GUI: %s\n' "$output"
    exit 1
   }
  cat "$output/love-$resume.log"
 done
 printf 'Retained acceptance GUI: %s\n' "$output"
done
