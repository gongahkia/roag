#!/bin/sh
# Prepared callback-driven render of each new biome's two concrete floors.
# Route identities and legal selection are tested headlessly; this does not
# represent physical keyboard play.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=$(mktemp -d "${TMPDIR:-/var/tmp}/roag-expansion-biomes-XXXXXX")
app="$output_dir/app"
mkdir -p "$app"
cp "$project_dir/main.lua" "$app/roag_main.lua"
cp "$project_dir/conf.lua" "$app/conf.lua"
cp -R "$project_dir/src" "$app/src"
cat > "$app/main.lua" <<'LUA'
local Game=require('src.game')
local Biomes=require('src.biomes')
local Content=require('src.content')
local Visibility=require('src.visibility')
local id=assert(os.getenv('ROAG_EXPANSION_BIOME_ID'))
local biome=assert(Biomes.get(id))
local index,history
if id==Biomes.order[5] then
    index,history=2,{'core:route/glacial_observatory'}
elseif id==Biomes.order[6] then
    index,history=3,{'core:route/fungal_hollows','core:route/fractured_archive'}
else
    index,history=3,{'core:route/sunken_aqueduct','core:route/sundered_forge'}
end
local old_new=Game.new_stage
local tracked
function Game.new_stage(seed,options)
    tracked=old_new(seed,{multifloor=true,root_seed=seed,
        stage_options={index=index,biome_id=id,route_history=history}})
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
local output=assert(os.getenv('ROAG_EXPANSION_BIOME_OUTPUT'))
local phase,pending,direction=1,false,nil
local function has(fragment)
    for _,label in ipairs(labels) do
        if label:find(fragment,1,true) then return true end
    end
end
function love.load(...)
    old_load(...)
    assert(tracked.stage.biome_id==id and tracked.generated_map.biome_id==id)
    assert(#tracked.floor_order==2)
    local visual=biome.floor_visual
    local found=false
    for _,row in ipairs(tracked.generated_map.visual_rows) do
        if row:find(visual,1,true) then found=true;break end
    end
    assert(found,'new biome has no distinctive concrete art glyph')
end
local function approach_stair()
    local stair=tracked.generated_map.placements.stairs[1]
    local player=Game.player(tracked)
    for _,d in ipairs({{1,0,'d'},{-1,0,'a'},{0,1,'s'},{0,-1,'w'}}) do
        local x,y=stair.x-d[1],stair.y-d[2]
        local free=Content.walkable(tracked.generated_map,x,y)
        for _,entity in ipairs(tracked.entities) do
            if entity.id~=player.id and entity.blocks_movement
                and entity.position.floor_id==tracked.active_floor_id
                and entity.position.x==x and entity.position.y==y then free=false end
        end
        if free then player.position.x,player.position.y=x,y
            direction=d[3];break end
    end
    assert(direction,'no legal approach to biome stair')
    tracked.meta.eligible=false
    Visibility.recompute(tracked)
end
function love.update() old_update(0) end
function love.draw(...)
    labels={};old_draw(...)
    if pending or phase>3 then return end
    local floor=phase==3 and 2 or 1
    assert(has(biome.name) and has('Floor '..floor..'/2'),
        'biome/floor presentation missing')
    pending=true
    love.graphics.captureScreenshot(function(image)
        local path=output..'/'..biome.strategy..'-floor'..phase..'.png'
        local file=assert(io.open(path,'wb'))
        file:write(image:encode('png'):getString());file:close()
        if phase==1 then
            approach_stair()
            old_update(2)
        elseif phase==2 then
            love.keypressed(direction)
            assert(tracked.active_floor_index==2 and tracked.generated_map.biome_id==id)
            old_update(1)
        else love.event.quit() end
        phase=phase+1;pending=false
    end)
end
LUA
for biome_id in core:biome/glacial_observatory core:biome/fractured_archive core:biome/sundered_forge; do
    ROAG_EXPANSION_BIOME_ID="$biome_id" ROAG_EXPANSION_BIOME_OUTPUT="$output_dir" \
        ROAG_GUI_AUTOSTART=1 XDG_DATA_HOME="$output_dir/data-${biome_id##*/}" \
        timeout 30s love "$app" > "$output_dir/${biome_id##*/}.log" 2>&1 || {
            cat "$output_dir/${biome_id##*/}.log"; exit 1;
        }
done
for strategy in observatory archive forge; do
    test -s "$output_dir/$strategy-floor1.png"
    test -s "$output_dir/$strategy-floor2.png"
    test -s "$output_dir/$strategy-floor3.png"
done
printf 'New-biome GUI smoke passed: %s\n' "$output_dir"
