#!/bin/sh
# Real LÖVE rendering and two fresh processes; never touches a player's save.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output=${1:-$(mktemp -d "${TMPDIR:-/tmp}/roag-classic-gui-XXXXXX")}
mkdir -p "$output/app/src" "$output/app/assets" "$output/data"
output=$(CDPATH= cd "$output" && pwd)
cp "$project_dir/main.lua" "$output/app/roag_main.lua"
cp "$project_dir/conf.lua" "$output/app/conf.lua"
cp "$project_dir"/src/*.lua "$output/app/src/"
cp "$project_dir/tests/classic_restore_gui.lua" "$output/app/main.lua"
cp "$project_dir/tests/classic_catalog_pixels.lua" "$output/app/classic_catalog_pixels.lua"
cp -R "$project_dir/assets/fonts" "$output/app/assets/"
(
    cd "$project_dir"
    luajit - <<'LUA'
local data=require('src.classic_sprite_data')
local paths={};for path in pairs(data.sources)do
    assert(path:match('^assets/') and not path:find('..',1,true) and not path:find('kenney',1,true))
    paths[#paths+1]=path
end
table.sort(paths);for _,path in ipairs(paths)do print(path)end
LUA
) > "$output/sources.txt"
while IFS= read -r path; do
    mkdir -p "$(dirname "$output/app/$path")"
    cp "$project_dir/$path" "$output/app/$path"
done < "$output/sources.txt"
for mode in save load; do
    ROAG_CLASSIC_GUI_OUTPUT="$output" ROAG_CLASSIC_GUI_MODE="$mode" \
    ROAG_GUI_AUTOSTART=0 XDG_DATA_HOME="$output/data" \
    timeout -k 3s 60s love "$output/app" > "$output/$mode.log" 2>&1 || {
        cat "$output/$mode.log";exit 1;
    }
    test -s "$output/$mode-result.txt"
    printf 'PASS classic GUI %s\n' "$mode"
done
test -s "$output/save-aim.png"
test -s "$output/save-boons.png"
test -s "$output/load-continued.png"
cmp "$output/continued.snapshot" "$output/expected-next.snapshot"
printf 'Classic LÖVE catalog GUI evidence: %s\n' "$output"
