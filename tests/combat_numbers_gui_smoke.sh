#!/bin/sh
# Render a resolved player hit, then confirm its number rises beside the enemy.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=$(mktemp -d "${TMPDIR:-/tmp}/roag-damage-gui-XXXXXX")
cp "$project_dir/main.lua" "$output_dir/roag_main.lua"
cp "$project_dir/conf.lua" "$output_dir/conf.lua"
cp -R "$project_dir/src" "$output_dir/src"
mkdir -p "$output_dir/assets"
cp -R "$project_dir/assets/fonts" "$output_dir/assets/fonts"
cat > "$output_dir/main.lua" <<'LUA'
local Game=require('src.game')
local Playback=require('src.playback')
local old_new,old_submit,old_add=Game.new,Game.submit,Playback.add
local tracked,playback,loss
function Game.new(seed)
    tracked=old_new(seed,'core:map/scrolling_room')
    local enemy=Game.spawn_enemy(tracked,'core:enemy/mossbound_guard',4,3)
    require('src.scheduler').remove_for_actor(tracked,enemy.id)
    return tracked
end
function Game.submit(...)
    local ok,reason,events,preview=old_submit(...)
    for _,event in ipairs(events or {}) do
        if event.kind=='DamageTaken' and event.source_id==tracked.player_id then
            loss=event.actual_loss
        end
    end
    return ok,reason,events,preview
end
function Playback.add(queue,...)
    playback=queue
    return old_add(queue,...)
end
assert(love.filesystem.load('roag_main.lua'))()
local old_load,old_draw,old_update=love.load,love.draw,love.update
local old_print=love.graphics.print
local output=assert(os.getenv('ROAG_DAMAGE_GUI_OUTPUT'))
local phase,pending,y_first=1,false,nil
local rendered_y
love.graphics.print=function(value,x,y,...)
    if loss and tostring(value)==tostring(loss) and type(x)=='number'
        and x<720 and type(y)=='number' then
        rendered_y=rendered_y and math.min(rendered_y,y) or y
    end
    return old_print(value,x,y,...)
end
function love.load(...)
    old_load(...)
    love.keypressed('f');love.keypressed('d');love.keypressed('return')
    assert(loss and loss>0,'player attack did not damage enemy')
    for _=1,100 do
        if Playback.current(playback) and
            Playback.current(playback).kind=='DamageTaken' then break end
        old_update(.01)
    end
    assert(Playback.current(playback).kind=='DamageTaken',
        'damage beat not reached')
    old_update(0)
end
function love.update() old_update(0) end
function love.draw(...)
    rendered_y=nil
    old_draw(...)
    assert(rendered_y,'resolved player damage number was not drawn')
    if phase==2 then assert(rendered_y<y_first,'damage number did not rise') end
    if pending then return end
    pending=true
    love.graphics.captureScreenshot(function(image)
        local file=assert(io.open(output..'/phase'..phase..'.png','wb'))
        file:write(image:encode('png'):getString());file:close()
        if phase==1 then
            y_first=rendered_y
            old_update(.25)
        else love.event.quit() end
        phase=phase+1;pending=false
    end)
end
LUA
ROAG_DAMAGE_GUI_OUTPUT="$output_dir" ROAG_GUI_AUTOSTART=1 timeout 15s love "$output_dir"
test -s "$output_dir/phase1.png"
test -s "$output_dir/phase2.png"
printf 'Damage GUI smoke passed: two screenshots in %s\n' "$output_dir"
