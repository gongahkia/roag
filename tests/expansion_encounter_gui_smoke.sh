#!/bin/sh
# Prepared callback-driven live rendering of a new elite telegraph and a
# regular summoner. Every invocation uses a fresh LÖVE process/save directory.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=$(mktemp -d "${TMPDIR:-/tmp}/roag-encounter-gui-XXXXXX")
cp "$project_dir/main.lua" "$output_dir/roag_main.lua"
cp "$project_dir/conf.lua" "$output_dir/conf.lua"
cp -R "$project_dir/src" "$output_dir/src"
mkdir -p "$output_dir/assets"
cp -R "$project_dir/assets/fonts" "$output_dir/assets/fonts"

cat > "$output_dir/main.lua" <<'LUA'
local Game=require("src.game")
local Biomes=require("src.biomes")
local Content=require("src.content")
local Scheduler=require("src.scheduler")
local Visibility=require("src.visibility")
local kind=assert(os.getenv("ROAG_ENCOUNTER_KIND"))
local tracked
local old_new=Game.new_stage
function Game.new_stage(seed,options)
    local id=kind=="elite" and Biomes.order[2] or Biomes.order[7]
    tracked=old_new(seed,{stage_options={index=2,biome_id=id,
        route_history={(id:gsub("^core:biome/","core:route/"))}}})
    return tracked
end
assert(love.filesystem.load("roag_main.lua"))()
local old_load,old_update,old_draw=love.load,love.update,love.draw
local old_print,old_color=love.graphics.print,love.graphics.setColor
local labels,telegraph={},0
local out=assert(os.getenv("ROAG_ENCOUNTER_OUTPUT"))
love.graphics.print=function(v,...)
    labels[#labels+1]=tostring(v)
    return old_print(v,...)
end
love.graphics.setColor=function(r,g,b,a)
    if r==0.95 and g==0.26 and b==0.31 then telegraph=telegraph+1 end
    return old_color(r,g,b,a)
end
local function near(entity)
    local p=Game.player(tracked)
    for _,d in ipairs({{1,0},{-1,0},{0,1},{0,-1}}) do
        local x,y=entity.position.x+d[1],entity.position.y+d[2]
        local open=Content.walkable(tracked.generated_map,x,y)
        for _,other in ipairs(tracked.entities) do
            if other.id~=p.id and other.blocks_movement
                and other.position.x==x and other.position.y==y then open=false end
        end
        if open then p.position.x,p.position.y=x,y;return end
    end
    error("no visible elite approach")
end
function love.load(...)
    old_load(...)
    for _,actor in ipairs(tracked.entities) do
        if actor.controller=="ai" then Scheduler.remove_for_actor(tracked,actor.id) end
    end
    local player=Game.player(tracked)
    player.hp,player.max_hp=100,100
    if kind=="elite" then
        local elite=assert(Game.entity(tracked,tracked.stage.elite_id))
        near(elite)
        elite.dormant=nil
        tracked.stage.phase="elite_active"
        Scheduler.enqueue(tracked,{kind="actor_ready",due=tracked.clock,
            actor_id=elite.id,is_player=false})
    else
        local x,y
        for _,d in ipairs({{2,0},{0,2},{-2,0},{0,-2}}) do
            local cx,cy=player.position.x+d[1],player.position.y+d[2]
            local middle_x=player.position.x+d[1]/2
            local middle_y=player.position.y+d[2]/2
            local open=Content.walkable(tracked.generated_map,cx,cy)
                and Content.walkable(tracked.generated_map,middle_x,middle_y)
            for _,actor in ipairs(tracked.entities) do
                if actor.blocks_movement and actor.position.x==cx
                    and actor.position.y==cy then open=false end
            end
            if open then x,y=cx,cy;break end
        end
        assert(x,"no regular showcase cell")
        local conjurer=Game.spawn_enemy(tracked,"core:enemy/forge_conjurer",x,y,0)
        assert(conjurer.definition_id==Biomes.get(Biomes.order[7]).enemy_ids[1])
    end
    Visibility.recompute(tracked)
    assert(Game.submit(tracked,{kind="wait"}))
    local strikes=Game.pending_strikes(tracked)
    assert(#strikes>0,"showcase has no locked attack")
    if kind=="elite" then
        assert(strikes[1].ability_id:find("core:ability/elite_",1,true))
    else
        assert(strikes[1].ability_id=="core:ability/forge_call")
    end
    old_update(10) -- scripted teleport changes the view; catch the camera up
end
local captured=false
function love.draw(...)
    labels,telegraph={},0
    old_draw(...)
    if captured then return end
    assert(telegraph>0,"encounter locked cells were not rendered")
    if kind=="elite" then
        local found=false
        for _,label in ipairs(labels) do
            if label:find("Mycelial Matriarch",1,true) then found=true end
        end
        assert(found,"biome elite health/name UI missing")
    end
    captured=true
    love.graphics.captureScreenshot(function(image)
        local file=assert(io.open(out.."/"..kind..".png","wb"))
        file:write(image:encode("png"):getString());file:close()
        love.event.quit()
    end)
end
LUA

for kind in elite regular; do
    mkdir -p "$output_dir/save-$kind"
    ROAG_GUI_AUTOSTART=1 ROAG_ENCOUNTER_KIND="$kind" \
        ROAG_ENCOUNTER_OUTPUT="$output_dir" \
        XDG_DATA_HOME="$output_dir/save-$kind" \
        timeout 20s love "$output_dir" >"$output_dir/$kind.log" 2>&1
    test -s "$output_dir/$kind.png"
done
printf 'Encounter GUI PASS: %s\n' "$output_dir"
