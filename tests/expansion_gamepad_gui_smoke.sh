#!/bin/sh
# Callback-only gamepad mapping check; no physical controller is claimed.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=$(mktemp -d "${TMPDIR:-/var/tmp}/roeg-gamepad-gui-XXXXXX")
app="$output_dir/app"
mkdir -p "$app" "$output_dir/data"
cp "$project_dir/main.lua" "$app/roeg_main.lua"
cp "$project_dir/conf.lua" "$app/conf.lua"
cp -R "$project_dir/src" "$app/src"
cat > "$app/main.lua" <<'LUA'
local Game=require('src.game')
local Content=require('src.content')
local Visibility=require('src.visibility')
local Classes=require('src.classes')
local output=assert(os.getenv('ROEG_GAMEPAD_OUTPUT'))
local tracked
local original_new=Game.new_stage
function Game.new_stage(...)
    tracked=original_new(...)
    return tracked
end
assert(love.filesystem.load('roeg_main.lua'))()
local old_load,old_draw,old_update=love.load,love.draw,love.update
local clock
local directions={{-1,0,'dpright'},{1,0,'dpleft'},
    {0,-1,'dpdown'},{0,1,'dpup'}}
function love.load(args)
    old_load(args)
    assert(not tracked)
    love.gamepadpressed(nil,'rightshoulder')
    love.gamepadpressed(nil,'y')
    assert(tracked and Game.player(tracked).definition_id==Classes.order[2])
    assert(tracked.clock==0)
    love.gamepadpressed(nil,'start')
    assert(tracked.clock==0)
    love.gamepadpressed(nil,'a')
    love.gamepadpressed(nil,'x')
    love.gamepadpressed(nil,'dpright')
    love.gamepadpressed(nil,'a')
    assert(tracked.clock>0,'gamepad aim/confirm did not submit an attack')
    clock=tracked.clock
    love.gamepadpressed(nil,'dpdown')
    assert(tracked.clock==clock,'input passed through playback gate')
    for _=1,120 do
        if not love.roegPlaybackPending() then break end
        old_update(.1)
    end
    assert(not love.roegPlaybackPending())
    local map=Content.map_for(tracked)
    local player=Game.player(tracked)
    local chest
    for _,entity in ipairs(tracked.entities) do
        if entity.controller=='chest' and entity.price>0 and
            entity.position.floor_id==tracked.active_floor_id then
            chest=entity;break
        end
    end
    assert(chest,'generated upper floor has no paid chest')
    local bump
    for _,direction in ipairs(directions) do
        local x=chest.position.x+direction[1]
        local y=chest.position.y+direction[2]
        local free=Content.walkable(map,x,y)
        for _,entity in ipairs(tracked.entities) do
            if entity.id~=player.id and entity.blocks_movement and
                entity.position.floor_id==tracked.active_floor_id and
                entity.position.x==x and entity.position.y==y then free=false end
        end
        if free then
            player.position.x,player.position.y=x,y
            bump=direction[3];break
        end
    end
    assert(bump,'fixture chest has no legal approach')
    Visibility.recompute(tracked) -- prepared UI-only chest approach
    clock=tracked.clock
    love.gamepadpressed(nil,bump)
    local menu=assert(love.roegMenuState())
    assert(menu.kind=='chest' and menu.selected==1 and menu.count==3)
    assert(tracked.clock==clock,'opening chest menu spent world time')
    love.gamepadpressed(nil,'dpdown')
    assert(love.roegMenuState().selected==2 and tracked.clock==clock)
    love.gamepadpressed(nil,'dpup')
    assert(love.roegMenuState().selected==1 and tracked.clock==clock)
end
local captured=false
function love.draw()
    old_draw()
    if captured then return end
    captured=true
    love.graphics.captureScreenshot(function(image)
        local file=assert(io.open(output..'/gamepad-menu.tga','wb'))
        file:write(image:encode('tga'):getString());file:close()
        love.gamepadpressed(nil,'b')
        assert(not love.roegMenuState() and tracked.clock==clock,
            'gamepad cancel changed the world')
        love.event.quit()
    end)
end
LUA
ROEG_GAMEPAD_OUTPUT="$output_dir" XDG_DATA_HOME="$output_dir/data" \
    timeout 25s love "$app" >"$output_dir/gamepad.log" 2>&1 || {
        cat "$output_dir/gamepad.log" >&2
        echo "Gamepad smoke artifacts: $output_dir" >&2
        exit 1
    }
test -s "$output_dir/gamepad-menu.tga"
printf 'Gamepad callback GUI PASS: %s\n' "$output_dir"
