#!/bin/sh
# Publish from Studio callbacks in a copied checkout and isolated LÖVE identity.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output=$(mktemp -d "${TMPDIR:-/tmp}/roag-studio-gui-publish-XXXXXX")
app="$output/app"
mkdir -p "$app" "$output/data"
cp "$project_dir/main.lua" "$app/roag_main.lua"
cp "$project_dir/conf.lua" "$app/conf.lua"
cp -R "$project_dir/src" "$app/src"
mkdir -p "$app/assets"
cp -R "$project_dir/assets/fonts" "$app/assets/fonts"
cp -R "$project_dir/examples" "$app/examples"
cp "$project_dir/tests/studio_gui_publish_wrapper.lua" "$app/main.lua"
ROAG_GUI_PUBLISH_OUTPUT="$output" XDG_DATA_HOME="$output/data" \
    timeout 40s love "$app" --studio >"$output/studio.log" 2>&1
for phase in 1 2 3 4 5; do test -s "$output/phase$phase.tga"; done
test -s "$app/assets/studio/manifest.txt"
printf 'Studio GUI publish passed: %s\n' "$output"
