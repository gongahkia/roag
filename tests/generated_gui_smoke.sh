#!/bin/sh
# Callback-driven LÖVE smoke of the default generated floor and continuous traversal.
set -eu

project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=$(mktemp -d "${TMPDIR:-/tmp}/roag-generated-gui-XXXXXX")
cp "$project_dir/main.lua" "$output_dir/roag_main.lua"
cp "$project_dir/conf.lua" "$output_dir/conf.lua"
cp -R "$project_dir/src" "$output_dir/src"
mkdir -p "$output_dir/assets"
cp -R "$project_dir/assets/fonts" "$output_dir/assets/fonts"

cat > "$output_dir/main.lua" <<'LUA'
local Game=require("src.game")
local Generation=require("src.generation")
local Content=require("src.content")
local Scheduler=require("src.scheduler")
local original_new=Game.new
local tracked,initial_rows
function Game.new(seed,map_id,options)
    -- This legacy camera fixture holds combat stationary while traversing the map.
    -- The new stage encounter has its own GUI harness.
    options=options or {}
    options.stage=false
    tracked=original_new(seed,map_id,options)
    -- Keep real rendered combatants stationary during this long camera route.
    for _,entity in ipairs(tracked.entities) do
        if entity.controller=="ai" then Scheduler.remove_for_actor(tracked,entity.id) end
    end
    return tracked
end
assert(love.filesystem.load("roag_main.lua"))()
local original_load,original_update,original_draw=love.load,love.update,love.draw
local output_dir=assert(os.getenv("ROAG_GENERATED_GUI_OUTPUT"))
local phase,pending=1,false
local labels,elite_rings={},0
local old_print,old_printf,old_circle=love.graphics.print,love.graphics.printf,love.graphics.circle
love.graphics.print=function(value,...)
    labels[#labels+1]=tostring(value)
    return old_print(value,...)
end
love.graphics.printf=function(value,...)
    labels[#labels+1]=tostring(value)
    return old_printf(value,...)
end
love.graphics.circle=function(mode,x,y,radius,...)
    if mode=="line" and radius==12 then elite_rings=elite_rings+1 end
    return old_circle(mode,x,y,radius,...)
end
local function has(fragment)
    for _,s in ipairs(labels) do if s:find(fragment,1,true) then return true end end
    return false
end
local dirs={{0,-1,"w"},{1,0,"d"},{0,1,"s"},{-1,0,"a"}}
local function key(x,y) return x..","..y end
local function route(predicate)
    local map=tracked.generated_map
    local p=Game.player(tracked).position
    local blocked={}
    for _,e in ipairs(tracked.entities) do
        if e.id~=tracked.player_id and e.blocks_movement then
            blocked[key(e.position.x,e.position.y)]=true
        end
    end
    local seen={[key(p.x,p.y)]=true}
    local queue={{x=p.x,y=p.y,path={}}}
    local head=1
    while head<=#queue do
        local current=queue[head];head=head+1
        if predicate(current.x,current.y) then return current.path end
        for _,d in ipairs(dirs) do
            local x,y=current.x+d[1],current.y+d[2]
            local k=key(x,y)
            if not seen[k] and Content.walkable(map,x,y) and not blocked[k] then
                seen[k]=true
                local path={}
                for i,v in ipairs(current.path) do path[i]=v end
                path[#path+1]=d[3]
                queue[#queue+1]={x=x,y=y,path=path}
            end
        end
    end
    error("no rendered traversal path")
end
local function walk(path)
    local old=tracked.clock
    for _,button in ipairs(path) do
        love.keypressed(button)
        local steps=0
        while love.roagPlaybackPending() do
            original_update(0.5)
            steps=steps+1
            assert(steps<100,"generated traversal playback did not drain")
        end
    end
    assert(tracked.clock==old+#path*100)
    local before=tracked.clock
    original_update(2) -- camera only
    assert(tracked.clock==before)
end
function love.load(...)
    original_load(...)
    assert(tracked.map_id==Generation.map_id and tracked.run_seed==1)
    assert(Generation.validate(tracked.generated_map))
    initial_rows=tracked.generated_map.rows
end
function love.update() original_update(0) end
function love.draw(...)
    labels,elite_rings={},0
    original_draw(...)
    if phase>3 or pending then return end
    assert(has("OVERGROWN RUINS") and has("HEALTH"))
    if phase==1 then
        assert(tracked.clock==0 and #tracked.generated_map.rooms>=8)
    elseif phase==2 then
        assert(tracked.clock>0 and has("HEALTH"))
        local p=Game.player(tracked).position
        local visible_chest,visible_enemy=false,false
        for _,e in ipairs(tracked.entities) do
            local d=math.abs(e.position.x-p.x)+math.abs(e.position.y-p.y)
            if e.controller=="chest" and d<=6 then visible_chest=true end
            if e.controller=="ai" and d<=9 then visible_enemy=true end
        end
        assert(visible_chest and visible_enemy)
    else
        local elite=tracked.generated_map.elite
        local p=Game.player(tracked).position
        assert(p.x==elite.x and p.y==elite.y and elite_rings>=1)
    end
    pending=true
    love.graphics.captureScreenshot(function(image)
        local file=assert(io.open(output_dir.."/phase"..phase..".png","wb"))
        file:write(image:encode("png"):getString()); file:close()
        if phase==1 then
            local chest=tracked.generated_map.placements.chests[1]
            local path=route(function(x,y)
                return math.abs(x-chest.x)+math.abs(y-chest.y)==1
            end)
            assert(#path>12) -- leaves the start room through a corridor
            walk(path)
        elseif phase==2 then
            local elite=tracked.generated_map.elite
            local path=route(function(x,y) return x==elite.x and y==elite.y end)
            assert(#path>40)
            walk(path)
        else
            love.keypressed("n")
            assert(tracked.run_seed==1)
            for y,row in ipairs(initial_rows) do
                assert(tracked.generated_map.rows[y]==row)
            end
            love.keypressed("m")
            assert(tracked.run_seed==2 and tracked.generated_map.rows[1]==initial_rows[1])
            local different=false
            for y,row in ipairs(initial_rows) do
                if tracked.generated_map.rows[y]~=row then different=true;break end
            end
            assert(different)
            love.event.quit()
        end
        phase=phase+1; pending=false
    end)
end
LUA

ROAG_GENERATED_GUI_OUTPUT="$output_dir" ROAG_GUI_AUTOSTART=1 timeout 20s love "$output_dir"
for phase in 1 2 3; do test -s "$output_dir/phase$phase.png"; done
printf 'Generated GUI smoke passed: three screenshots in %s\n' "$output_dir"
