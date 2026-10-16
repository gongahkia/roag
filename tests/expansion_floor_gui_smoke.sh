#!/bin/sh
# Assisted callback fixture: rendered stair/HUD/fog and one real movement handoff.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=$(mktemp -d "${TMPDIR:-/tmp}/roag-floor-gui-XXXXXX")
cp "$project_dir/main.lua" "$output_dir/roag_main.lua"
cp "$project_dir/conf.lua" "$output_dir/conf.lua"
cp -R "$project_dir/src" "$output_dir/src"
mkdir -p "$output_dir/assets"
cp -R "$project_dir/assets/fonts" "$output_dir/assets/fonts"
cat > "$output_dir/main.lua" <<'LUA'
local Game=require('src.game')
local Content=require('src.content')
local Visibility=require('src.visibility')
local original_new=Game.new_stage
local tracked
function Game.new_stage(seed,options)
    options=options or {};options.multifloor=true
    tracked=original_new(seed,options)
    return tracked
end
assert(love.filesystem.load('roag_main.lua'))()
local old_load,old_update,old_draw=love.load,love.update,love.draw
local old_print=love.graphics.print
local labels={}
love.graphics.print=function(value,...)
    labels[#labels+1]=tostring(value)
    return old_print(value,...)
end
local output=assert(os.getenv('ROAG_FLOOR_GUI_OUTPUT'))
local direction
local phase,pending=1,false
local function has(fragment)
    for _,label in ipairs(labels) do
        if label:find(fragment,1,true) then return true end
    end
end
function love.load(...)
    old_load(...)
    assert(tracked.floor_order and tracked.active_floor_index==1)
    local stair=tracked.generated_map.placements.stairs[1]
    local p=Game.player(tracked)
    for _,d in ipairs({{1,0,'d'},{-1,0,'a'},{0,1,'s'},{0,-1,'w'}}) do
        local x,y=stair.x-d[1],stair.y-d[2]
        local free=Content.walkable(tracked.generated_map,x,y)
        for _,entity in ipairs(tracked.entities) do
            if entity.id~=p.id and entity.blocks_movement
                and entity.position.floor_id==tracked.active_floor_id
                and entity.position.x==x and entity.position.y==y then free=false end
        end
        if free then
            p.position.x,p.position.y=x,y
            tracked.meta.eligible=false -- prepared visual fixture
            direction=d[3]
            break
        end
    end
    assert(direction,'no stair approach')
    Visibility.recompute(tracked)
    old_update(2)
end
function love.update() old_update(0) end
function love.draw(...)
    labels={};old_draw(...)
    if phase>2 or pending then return end
    assert(has('FLOOR '..phase),'missing current-floor HUD label')
    pending=true
    love.graphics.captureScreenshot(function(image)
        local file=assert(io.open(output..'/floor'..phase..'.png','wb'))
        file:write(image:encode('png'):getString());file:close()
        if phase==1 then
            local before=tracked.clock
            love.keypressed(direction)
            assert(tracked.active_floor_index==2 and tracked.clock==before+100,
                'stair key failed to perform one movement recovery')
            old_update(1)
        else
            assert(tracked.floor_pause[tracked.floor_order[1]],
                'upper floor did not park')
            love.event.quit()
        end
        phase=phase+1;pending=false
    end)
end
LUA
ROAG_FLOOR_GUI_OUTPUT="$output_dir" ROAG_GUI_AUTOSTART=1 \
    timeout 30s love "$output_dir"
test -s "$output_dir/floor1.png"
test -s "$output_dir/floor2.png"
printf 'Connected-floor GUI smoke passed: %s\n' "$output_dir"
