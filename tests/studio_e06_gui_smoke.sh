#!/bin/sh
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output=$(mktemp -d "${TMPDIR:-/var/tmp}/roeg-studio-e06-XXXXXX")
app="$output/app"
mkdir -p "$app/assets/studio/sources" "$app/assets/studio/atlas" "$output/data"
cp "$project_dir/main.lua" "$app/roeg_main.lua"
cp "$project_dir/conf.lua" "$app/conf.lua"
cp -R "$project_dir/src" "$app/src"
cp -R "$project_dir/examples" "$app/examples"
cp -R "$project_dir/tools" "$app/tools"
cp "$project_dir/tests/e06_game_wrapper.lua" "$app/main.lua"
(cd "$project_dir" && luajit tests/e06_fixture.lua "$app") >"$output/fixture.log" 2>&1
cp "$project_dir/tests/e06_studio_wrapper.lua" "$app/main.lua"
ROEG_E06_STUDIO_MODE=create ROEG_E06_OUTPUT="$output" \
    XDG_DATA_HOME="$output/data" TMPDIR="${TMPDIR:-/var/tmp}" \
    timeout 35s love "$app" --studio >"$output/studio-create.log" 2>&1
ROEG_E06_STUDIO_MODE=reopen ROEG_E06_OUTPUT="$output" \
    XDG_DATA_HOME="$output/data" TMPDIR="${TMPDIR:-/var/tmp}" \
    timeout 35s love "$app" --studio >"$output/studio-reopen.log" 2>&1
test -s "$output/create-3.tga"
test -s "$output/reopen-1.tga"
# The compiler needs LÖVE's 11.5 image decoder; the candidate remains inactive.
cp "$app/roeg_main.lua" "$app/main.lua"
XDG_DATA_HOME="$output/data" TMPDIR="${TMPDIR:-/var/tmp}" \
    timeout 35s love "$app" --atlas-build >"$output/build.log" 2>&1
save_dir="$output/data/love/roeg"
if [ ! -d "$save_dir/studio/atlas-build" ]; then
    save_dir=$(find "$output/data" -type d -path '*/studio/atlas-build' -print -quit)
    save_dir=${save_dir%/studio/atlas-build}
fi
(cd "$app" && luajit tools/studio_atlas_publish.lua --save-dir "$save_dir" \
    --project e06_visuals --apply) >"$output/publish.log" 2>&1
cp "$project_dir/tests/e06_game_wrapper.lua" "$app/main.lua"
ROEG_GUI_AUTOSTART=1 ROEG_E06_APP="$app" ROEG_E06_OUTPUT="$output" \
    XDG_DATA_HOME="$output/data" TMPDIR="${TMPDIR:-/var/tmp}" \
    timeout 35s love "$app" >"$output/game.log" 2>&1
test -s "$output/frame-5.tga"
printf 'E06 GUI PASS %s\n' "$output"
