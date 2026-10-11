#!/bin/sh
# Prepared callback-driven screenshots of all three genuine biome renderers,
# their real locked enemy telegraphs, and the route UI. No real keyboard play.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=$(mktemp -d "${TMPDIR:-/tmp}/roeg-biome-gui-XXXXXX")
cp "$project_dir/main.lua" "$output_dir/roeg_main.lua"
cp "$project_dir/conf.lua" "$output_dir/conf.lua"
cp -R "$project_dir/src" "$output_dir/src"

cat > "$output_dir/main.lua" <<'LUA'
local Game=require("src.game")
local Biomes=require("src.biomes")
local Content=require("src.content")
local Scheduler=require("src.scheduler")
local Visibility=require("src.visibility")
local id=assert(os.getenv("ROEG_BIOME_GUI_ID"))
local b=assert(Biomes.get(id))
local index=id==Biomes.order[4] and 3 or 2
local history=index==3 and {"core:route/fungal_hollows","core:route/ashen_bastion"}
    or {(id:gsub("^core:biome/","core:route/"))}
local tracked
local original_new=Game.new_stage
function Game.new_stage(seed,options)
    tracked=original_new(seed,{root_seed=seed,stage_options={index=index,
        biome_id=id,route_history=history}})
    return tracked
end
assert(love.filesystem.load("roeg_main.lua"))()
local old_load,old_update,old_draw=love.load,love.update,love.draw
local old_print,old_printf,old_color=love.graphics.print,
    love.graphics.printf,love.graphics.setColor
local labels,scenery,telegraph={},0,0
local phase,pending=1,false
local out=assert(os.getenv("ROEG_BIOME_GUI_OUTPUT"))
love.graphics.print=function(v,...)
    labels[#labels+1]=tostring(v);return old_print(v,...)
end
love.graphics.printf=function(v,...)
    labels[#labels+1]=tostring(v);return old_printf(v,...)
end
love.graphics.setColor=function(r,g,bl,a)
    if id==Biomes.order[2] and r==0.17 and g==0.31 and bl==0.31
        or id==Biomes.order[3] and r==0.20 and g==0.36 and bl==0.43
        or id==Biomes.order[4] and r==0.36 and g==0.27 and bl==0.24 then
        scenery=scenery+1
    end
    if r==0.95 and g==0.26 and bl==0.31 then telegraph=telegraph+1 end
    return old_color(r,g,bl,a)
end
local function has(fragment)
    for _,v in ipairs(labels) do if v:find(fragment,1,true) then return true end end
end
local function approach(entity)
    local p=Game.player(tracked)
    for _,d in ipairs({{0,-1},{1,0},{0,1},{-1,0}}) do
        local x,y=entity.position.x+d[1],entity.position.y+d[2]
        local open=Content.walkable(tracked.generated_map,x,y)
        for _,other in ipairs(tracked.entities) do
            if other.id~=p.id and other.blocks_movement and
                other.position.x==x and other.position.y==y then open=false end
        end
        if open then
            p.position.x,p.position.y=x,y;Visibility.recompute(tracked)
            return -d[1],-d[2]
        end
    end
    error("no fixture approach")
end
function love.load(...)
    old_load(...)
    assert(tracked.stage.biome_id==id and tracked.generated_map.biome_id==id)
    assert(#tracked.generated_map.rooms>=8)
end
function love.update() old_update(0) end
function love.draw(...)
    labels,scenery,telegraph={},0,0
    old_draw(...)
    if phase>3 or pending then return end
    assert(has(b.name) and scenery>0,"biome identity not rendered")
    if phase==2 then
        local strikes=Game.pending_strikes(tracked)
        assert(#strikes>=1 and strikes[1].ability_id==
            (id==Biomes.order[2] and "core:ability/spore_burst" or
            id==Biomes.order[3] and "core:ability/canal_thrust" or
            "core:ability/bell_cross"))
        assert(telegraph>0,"locked cells not rendered")
    elseif phase==3 then
        assert(index==3 and has("Enter the Final Sanctum")
            or index==2 and has("Choose a route") and has("Ashen Bastion"))
    end
    pending=true
    love.graphics.captureScreenshot(function(image)
        local file=assert(io.open(out.."/"..b.strategy.."-"..phase..".png","wb"))
        file:write(image:encode("png"):getString());file:close()
        if phase==1 then
            local player=Game.player(tracked)
            for _,e in ipairs(tracked.entities) do
                if e.controller=="ai" then Scheduler.remove_for_actor(tracked,e.id) end
            end
            local room
            for _,candidate in ipairs(tracked.generated_map.rooms) do
                if math.max(candidate.width,candidate.height)>=15
                    and candidate.role~="elite" then
                    room=candidate;break
                end
            end
            assert(room,"no medium biome room")
            player.position.x,player.position.y=room.center.x,room.center.y
            local ex,ey
            for _,d in ipairs({{1,0},{0,1},{-1,0},{0,-1}}) do
                local x,y=player.position.x+d[1]*2,player.position.y+d[2]*2
                local middle_x,middle_y=player.position.x+d[1],
                    player.position.y+d[2]
                local occupied=false
                for _,other in ipairs(tracked.entities) do
                    if other.blocks_movement and other.position.x==x
                        and other.position.y==y then occupied=true end
                end
                if not occupied and Content.walkable(tracked.generated_map,x,y)
                    and Content.projectile_passable(tracked.generated_map,middle_x,middle_y,
                        tracked) then ex,ey=x,y;break end
            end
            assert(ex,"no medium-room signature enemy lane")
            Visibility.recompute(tracked)
            old_update(10)
            local enemy=Game.spawn_enemy(tracked,b.enemy_ids[1],ex,ey)
            enemy.speed=35 -- prepared long windup for a visible locked warning
            player.hp,player.max_hp=100,100
            assert(Game.submit(tracked,{kind="wait"}))
            assert(enemy.pending_attack)
        elseif phase==2 then
            -- Prepared elite death, then the real cardinal bump opens the menu.
            for _,e in ipairs(tracked.entities) do
                if e.controller=="ai" then Scheduler.remove_for_actor(tracked,e.id) end
            end
            local elite=assert(Game.entity(tracked,tracked.stage.elite_id))
            local dx,dy=approach(elite)
            Game.player(tracked).damage=100
            assert(Game.submit(tracked,{kind="basic_attack",dx=dx,dy=dy}))
            local exit=assert(Game.entity(tracked,tracked.stage.exit_id))
            dx,dy=approach(exit)
            old_update(10)
            local clock=tracked.clock
            local key=dx==1 and "d" or dx==-1 and "a"
                or dy==1 and "s" or "w"
            love.keypressed(key)
            assert(tracked.clock==clock,"route menu spent time")
        else love.event.quit() end
        phase=phase+1;pending=false
    end)
end
LUA

for name in fungal_hollows sunken_aqueduct ashen_bastion; do
    ROEG_BIOME_GUI_ID="core:biome/$name" ROEG_BIOME_GUI_OUTPUT="$output_dir" \
      ROEG_GUI_AUTOSTART=1 timeout 20s love "$output_dir"
done
for strategy in warrens canals courtyards; do
    for frame in 1 2 3; do test -s "$output_dir/$strategy-$frame.png"; done
done
printf 'Biome GUI smoke passed: nine frames in %s\n' "$output_dir"
