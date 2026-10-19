#!/bin/sh
set -eu
cd "$(dirname "$0")/.."
if [ "$#" -eq 0 ]; then set -- 1 2 3 4 5 6 7 8; fi
for face do
    printf 'RUN real dice face %s\n' "$face"
    ROAG_IDEA_BOON_CASE=dice ROAG_DICE_FACE="$face" TMPDIR=/var/tmp \
        sh tests/idea_boon_gui_smoke.sh
done
