#!/bin/sh
# Current native 1-Bit pixels, real callbacks and separate-process Continue.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output=${1:-$(mktemp -d "${TMPDIR:-/var/tmp}/roag-juice-gui-XXXXXX")}
mkdir -p "$output"
output=$(CDPATH= cd "$output" && pwd)
mkdir -p "$output/app/tools" "$output/data"
cp "$project_dir/main.lua" "$output/app/roag_main.lua"
cp "$project_dir/tests/visual_juice_gui.lua" "$output/app/main.lua"
cp "$project_dir/conf.lua" "$output/app/conf.lua"
cp -R "$project_dir/src" "$project_dir/assets" "$output/app/"
cp "$project_dir/tools/art_png.lua" "$output/app/tools/"
save_root=$(mktemp -d "$output/data/run-XXXXXX")
for mode in flow resume enemy impact blocked discovery; do
    autostart=0
    data="$save_root/flow"
    case "$mode" in
        enemy|impact|blocked|discovery) autostart=1; data="$save_root/$mode" ;;
    esac
    mkdir -p "$data"
    if ! ROAG_JUICE_OUTPUT="$output" ROAG_JUICE_MODE="$mode" \
        ROAG_GUI_AUTOSTART="$autostart" XDG_DATA_HOME="$data" \
        timeout -k 3s 45s love "$output/app" > "$output/$mode.log" 2>&1; then
        cat "$output/$mode.log"
        exit 1
    fi
    cat "$output/$mode.log"
    if ! grep -q "^PASS visual juice $mode$" "$output/$mode.log"; then
        printf 'Missing successful completion for %s\n' "$mode"
        exit 1
    fi
done
printf 'Visual juice native frames, exact continuation and refresh PASS: %s\n' "$output"
