#!/bin/sh
# Each case runs real callbacks in its own isolated LÖVE process.
set -eu
cd "$(dirname "$0")/.."
if [ "$#" -eq 0 ];then
 set -- doorward_lamp secret_apple spoils_aegis mist_lullaby rime_disk vent_mantle \
 patient_rebuke beacon_afterglow sentry_quartermaster spirit_shelter grounding_rod mirror_dividend
fi
for slug do ROAG_SUPPORT_BOON_CASE="$slug" sh tests/idea_cadence_boon_gui_smoke.sh;done
