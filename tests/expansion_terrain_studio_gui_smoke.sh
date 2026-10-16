#!/bin/sh
# Two isolated LÖVE processes: author a mechanical tile via visible World
# Editor shortcuts, save, reopen, and preview the current room.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=$(mktemp -d "${TMPDIR:-/var/tmp}/roag-terrain-studio-XXXXXX")
app="$output_dir/app"
mkdir -p "$app" "$output_dir/data"
cp "$project_dir/main.lua" "$app/roag_main.lua"
cp "$project_dir/conf.lua" "$app/conf.lua"
cp -R "$project_dir/src" "$app/src"
mkdir -p "$app/assets"
cp -R "$project_dir/assets/fonts" "$app/assets/fonts"
cat > "$app/main.lua" <<'LUA'
assert(love.filesystem.load('roag_main.lua'))()
local old_load,old_draw=love.load,love.draw
local phase=assert(tonumber(os.getenv('ROAG_TERRAIN_STUDIO_PHASE')))
local output=assert(os.getenv('ROAG_TERRAIN_STUDIO_OUTPUT'))
local pending=false
local function state() return assert(love.roagStudioState()) end
local function replace(value)
    for _=1,120 do love.keypressed('backspace') end
    love.textinput(value);love.keypressed('return')
end
function love.load(args)
    old_load(args)
    if phase==1 then
        love.keypressed('n');love.textinput('Terrain Fixture');love.keypressed('return')
        love.keypressed('w');assert(state().mode=='world')
        love.keypressed('n');replace('9x7')
        local e=assert(state().world_editor)
        assert(#e.pack.tiles==3)
        love.keypressed('a');assert(#e.pack.tiles==9)
        for _=1,3 do love.keypressed('tab') end
        assert(e.pack.tiles[e.selected_tile].symbol=='b')
        love.keypressed('5')
        local ox,oy=e:origin()
        local px,py=ox+2.5*e.zoom,oy+2.5*e.zoom
        love.mouse.setPosition(px,py)
        love.mousepressed(px,py,1);love.mousereleased(px,py,1)
        assert(e:room().rows[3]:sub(3,3)=='b')
        love.keypressed('v')
        assert(e.message:find('VALID',1,true),e.message)
        love.keypressed('s')
        assert(not state().dirty)
    else
        love.keypressed('o');love.keypressed('return');love.keypressed('w')
        local e=assert(state().world_editor)
        assert(state().project.id=='terrain_fixture')
        assert(#e.pack.tiles==9 and e:room().rows[3]:sub(3,3)=='b')
        love.keypressed('v');assert(e.message:find('VALID',1,true),e.message)
        love.keypressed('p');assert(e.preview_turn~=0)
    end
end
function love.draw(...)
    old_draw(...)
    if pending then return end
    pending=true
    love.graphics.captureScreenshot(function(image)
        local file=assert(io.open(output..'/studio'..phase..'.tga','wb'))
        file:write(image:encode('tga'):getString());file:close()
        love.event.quit()
    end)
end
LUA
for phase in 1 2; do
    ROAG_TERRAIN_STUDIO_PHASE="$phase" ROAG_TERRAIN_STUDIO_OUTPUT="$output_dir" \
        XDG_DATA_HOME="$output_dir/data" timeout 25s love "$app" --studio \
        > "$output_dir/phase$phase.log" 2>&1 || {
            cat "$output_dir/phase$phase.log"; exit 1;
        }
    test -s "$output_dir/studio$phase.tga"
done
printf 'Terrain Studio GUI smoke passed: %s\n' "$output_dir"
