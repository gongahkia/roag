#!/bin/sh
# Assisted callback fixture: a generated bramble is rendered and causes canonical
# contact damage through an ordinary key action. It is not a physical playtest.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=$(mktemp -d "${TMPDIR:-/tmp}/roeg-terrain-gui-XXXXXX")
cp "$project_dir/main.lua" "$output_dir/roeg_main.lua"
cp "$project_dir/conf.lua" "$output_dir/conf.lua"
cp -R "$project_dir/src" "$output_dir/src"
cat > "$output_dir/main.lua" <<'LUA'
local Game=require('src.game')
local Content=require('src.content')
local Visibility=require('src.visibility')
local original_new=Game.new_stage
local original_submit=Game.submit
local tracked,last_events
function Game.new_stage(seed,options)
    options=options or {};options.multifloor=true
    tracked=original_new(seed,options)
    return tracked
end
function Game.submit(state,intent)
    local committed,reason,events,preview=original_submit(state,intent)
    last_events=events
    return committed,reason,events,preview
end
assert(love.filesystem.load('roeg_main.lua'))()
local old_load,old_update,old_draw=love.load,love.update,love.draw
local output=assert(os.getenv('ROEG_TERRAIN_GUI_OUTPUT'))
local phase,pending,direction,initial_hp=1,false,nil,nil
local first_bytes
local function capture(image,path)
    local bytes=image:encode('png'):getString()
    local file=assert(io.open(output..'/'..path,'wb'))
    file:write(bytes);file:close()
    return bytes
end
function love.load(...)
    old_load(...)
    local map=tracked.generated_map
    local player=Game.player(tracked)
    local hazard
    for _,p in ipairs(map.terrain_placements) do
        if p.symbol=='b' then hazard=p end
    end
    assert(hazard and map.rows[hazard.y]:sub(hazard.x,hazard.x)=='b')
    for _,d in ipairs({{1,0,'d'},{-1,0,'a'},{0,1,'s'},{0,-1,'w'}}) do
        local x,y=hazard.x-d[1],hazard.y-d[2]
        local free=Content.walkable(map,x,y)
        for _,entity in ipairs(tracked.entities) do
            if entity.id~=player.id and entity.blocks_movement
                and entity.position.floor_id==tracked.active_floor_id
                and entity.position.x==x and entity.position.y==y then free=false end
        end
        if free then
            player.position.x,player.position.y=x,y
            direction=d[3];break
        end
    end
    assert(direction,'bramble has no safe approach')
    tracked.meta.eligible=false
    initial_hp=player.hp
    Visibility.recompute(tracked)
    old_update(2)
end
function love.update() old_update(0) end
function love.draw(...)
    old_draw(...)
    if pending or phase>2 then return end
    pending=true
    love.graphics.captureScreenshot(function(image)
        local bytes=capture(image,'terrain'..phase..'.png')
        if phase==1 then
            first_bytes=bytes
            love.keypressed(direction)
            local hit=false
            for _,event in ipairs(last_events or {}) do
                if event.kind=='DamageTaken' and event.damage_type=='bramble'
                    and event.target_id==tracked.player_id and event.actual_loss==1 then
                    hit=true
                end
            end
            assert(hit and Game.player(tracked).hp<=initial_hp-1,
                'bramble key action did not apply canonical contact damage')
            old_update(1)
        else
            assert(bytes~=first_bytes,'rendered board did not change after movement')
            love.event.quit()
        end
        phase=phase+1;pending=false
    end)
end
LUA
ROEG_TERRAIN_GUI_OUTPUT="$output_dir" ROEG_GUI_AUTOSTART=1 \
    timeout 30s love "$output_dir"
test -s "$output_dir/terrain1.png"
test -s "$output_dir/terrain2.png"
printf 'Terrain GUI smoke passed: %s\n' "$output_dir"
