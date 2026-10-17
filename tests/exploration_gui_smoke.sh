#!/bin/sh
# Callback-driven LÖVE render test for fog memory, map input and a real secret.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=$(mktemp -d "${TMPDIR:-/tmp}/roag-exploration-gui-XXXXXX")
cp "$project_dir/main.lua" "$output_dir/roag_main.lua"
cp "$project_dir/conf.lua" "$output_dir/conf.lua"
cp -R "$project_dir/src" "$output_dir/src"
mkdir -p "$output_dir/assets"
cp -R "$project_dir/assets/fonts" "$output_dir/assets/fonts"
cat > "$output_dir/main.lua" <<'LUA'
local Game=require("src.game")
local Content=require("src.content")
local Scheduler=require("src.scheduler")
local Camera=require('src.camera')
local camera
local camera_new=Camera.new
function Camera.new(...)
    camera=camera_new(...);return camera
end
local original_new=Game.new
local tracked
function Game.new(seed,map_id,options)
    -- This legacy fog/secret fixture freezes combat and stage pressure.
    options=options or {}
    options.stage=false
    tracked=original_new(seed,map_id,options)
    for _,entity in ipairs(tracked.entities) do
        if entity.controller=="ai" then Scheduler.remove_for_actor(tracked,entity.id) end
    end
    return tracked
end
assert(love.filesystem.load("roag_main.lua"))()
local old_load,old_draw,old_update=love.load,love.draw,love.update
local out=assert(os.getenv("ROAG_EXPLORATION_GUI_OUTPUT"))
local phase,pending=1,false
local origin_known
local hidden_source
local labels={}
local original_print=love.graphics.print
local original_printf=love.graphics.printf
love.graphics.print=function(value,...)
    labels[#labels+1]=tostring(value)
    return original_print(value,...)
end
love.graphics.printf=function(value,...)
    labels[#labels+1]=tostring(value)
    return original_printf(value,...)
end
local function contains(fragment)
    for _,label in ipairs(labels) do
        if label:find(fragment,1,true) then return true end
    end
    return false
end
local dirs={{0,-1,"w"},{1,0,"d"},{0,1,"s"},{-1,0,"a"}}
local function key(x,y) return x..","..y end
local function route(tx,ty)
    local map=tracked.generated_map
    local p=Game.player(tracked).position
    local blocked={}
    for _,e in ipairs(tracked.entities) do
        if e.id~=tracked.player_id and e.blocks_movement
            and e.controller~="door" then
            blocked[key(e.position.x,e.position.y)]=true
        end
    end
    local queue={{x=p.x,y=p.y,path={}}}
    local seen={[key(p.x,p.y)]=true}
    local head=1
    while head<=#queue do
        local current=queue[head];head=head+1
        if current.x==tx and current.y==ty then return current.path end
        for _,d in ipairs(dirs) do
            local x,y=current.x+d[1],current.y+d[2]
            if Content.walkable(map,x,y) and not seen[key(x,y)]
                and not blocked[key(x,y)] then
                seen[key(x,y)]=true
                local path={}
                for index,step in ipairs(current.path) do path[index]=step end
                path[#path+1]=d[3]
                queue[#queue+1]={x=x,y=y,path=path}
            end
        end
    end
    error("no secret approach route")
end
local function walk(path)
    local function settle()
        local ticks=0
        while love.roagPlaybackPending() do
            old_update(0.25) -- presentation time only; wait for the real gate.
            ticks=ticks+1
            assert(ticks<400,"playback did not drain")
        end
    end
    for _,button in ipairs(path) do
        settle()
        local p=Game.player(tracked).position
        local x,y=p.x,p.y
        local before=tracked.clock
        love.keypressed(button)
        settle()
        if p.x==x and p.y==y then
            assert(tracked.clock>before and tracked.last_action.kind=="open_door",
                ("uncommitted route input %s at %d,%d time %d"):format(
                    button,x,y,before))
            before=tracked.clock
            love.keypressed(button)
            settle()
            assert(tracked.clock>before,"door entry did not commit")
        end
        assert(p.x~=x or p.y~=y,"route did not move after door handling")
    end
end
function love.load(...)
    old_load(...)
    local p=Game.player(tracked).position
    assert(tracked.clock==0 and not tracked.secrets[1].revealed)
    origin_known=#tracked.explored[tracked.active_floor_id].order
    assert(not Game.is_visible(tracked,tracked.active_floor_id,p.x+9,p.y))
    for _,entity in ipairs(tracked.entities) do
        if entity.controller=="ai" and
            not Game.is_visible(tracked,tracked.active_floor_id,
                entity.position.x,entity.position.y) then
            hidden_source=entity; break
        end
    end
    assert(hidden_source)
    hidden_source.pending_attack={due=99,sequence=1,
        target_cells={{floor_id=tracked.active_floor_id,
            x=hidden_source.position.x,y=hidden_source.position.y}}}
end
function love.update() old_update(0) end
function love.draw(...)
    labels={}
    old_draw(...)
    if phase>3 or pending then return end
    local secret=tracked.secrets[1]
    assert(contains("MAP") and contains("HEALTH"))
    if phase==1 then
        local arena=tracked.generated_map.elite
        assert(tracked.clock==0 and
            not contains(("arena %d,%d"):format(arena.x,arena.y))
            and not contains("INCOMING ATTACK"))
    elseif phase==2 then
        local p=Game.player(tracked).position
        assert(p.x==secret.x and p.y==secret.y)
        assert(not secret.revealed and tracked.clock>0)
        assert(#tracked.explored[tracked.active_floor_id].order>origin_known)
        assert(Game.is_visible(tracked,tracked.active_floor_id,
            secret.wall_x,secret.wall_y))
    else
        assert(secret.revealed and secret.reward_id)
        assert(Content.walkable(tracked.generated_map,secret.wall_x,secret.wall_y))
        assert(Game.is_visible(tracked,tracked.active_floor_id,
            secret.cache_x,secret.cache_y))
        assert(contains("hidden cache found"))
    end
    pending=true
    love.graphics.captureScreenshot(function(image)
        local ok,why=pcall(function()
        local file=assert(io.open(out.."/phase"..phase..".png","wb"))
        file:write(image:encode("png"):getString()); file:close()
        if phase==1 then
            local p=Game.player(tracked).position
            -- Sample a genuinely unexplored cell inside the actual camera at
            -- its current zoom, outside the minimap. The previous hardcoded
            -- 32px offset sampled visible geometry after default zoom became 48.
            local screen_x,screen_y
            local map=Content.map_for(tracked)
            for yy=1,map.height do for xx=1,map.width do
                local sx,sy=Camera.tile_top_left(camera,xx,yy)
                sx,sy=sx+camera.tile/2,sy+camera.tile/2
                if sx>25+map.width*1.3+5 and sx<camera.viewport.x+camera.viewport.width
                    and sy>=camera.viewport.y and sy<camera.viewport.y+camera.viewport.height and
                    not Game.was_explored(tracked,tracked.active_floor_id,xx,yy)then
                    screen_x,screen_y=sx,sy;break
                end
            end;if screen_x then break end end
            assert(screen_x,'no on-screen unexplored fog sample')
            local r,g,b=image:getPixel(screen_x,screen_y)
            assert(math.abs(r-0.09)<0.03 and math.abs(g-0.13)<0.03
                and math.abs(b-0.11)<0.03,
                ('unseen tile leaked geometry at %.0f,%.0f: %.3f %.3f %.3f'):
                    format(screen_x,screen_y,r,g,b))
            local clock,rng=tracked.clock,tracked.rng.state
            love.keypressed("tab")
            assert(tracked.clock==clock and tracked.rng.state==rng)
            hidden_source.pending_attack=nil
            love.keypressed("tab")
            assert(tracked.clock==clock and tracked.rng.state==rng)
            local path=route(secret.x,secret.y)
            assert(#path>10)
            walk(path)
        elseif phase==2 then
            local dx,dy=secret.wall_x-secret.x,secret.wall_y-secret.y
            local button=dx==1 and "d" or dx==-1 and "a"
                or dy==1 and "s" or "w"
            local old_clock=tracked.clock
            love.keypressed("f")
            love.keypressed(button)
            love.keypressed("return")
            assert(tracked.clock==old_clock+100 and secret.revealed)
        else
            love.event.quit()
        end
        phase=phase+1;pending=false
        end)
        if not ok then
            io.stderr:write('Exploration screenshot assertion: '..tostring(why)..'\n')
            love.event.quit(1)
        end
    end)
end
local checked_draw=love.draw
function love.draw(...)
    local ok,why=pcall(checked_draw,...)
    if not ok then
        io.stderr:write('Exploration draw assertion: '..tostring(why)..'\n')
        love.event.quit(1)
    end
end
LUA
ROAG_EXPLORATION_GUI_OUTPUT="$output_dir" ROAG_GUI_AUTOSTART=1 timeout 20s love "$output_dir"
for phase in 1 2 3; do test -s "$output_dir/phase$phase.png"; done
printf 'Exploration GUI smoke passed: three screenshots in %s\n' "$output_dir"
