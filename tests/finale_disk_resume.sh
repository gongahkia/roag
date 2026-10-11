#!/bin/sh
# Fresh LÖVE process Save & Quit -> title Continue for a prepared final encounter.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=$(mktemp -d "${TMPDIR:-/tmp}/roeg-finale-resume-XXXXXX")
cp "$project_dir/main.lua" "$output_dir/roeg_main.lua"
cp "$project_dir/conf.lua" "$output_dir/conf.lua"
cp -R "$project_dir/src" "$output_dir/src"
cat > "$output_dir/main.lua" <<'LUA'
local Game=require('src.game')
local Scheduler=require('src.scheduler')
local Content=require('src.content')
local Visibility=require('src.visibility')
local Persistence=require('src.persistence')
local phase=assert(os.getenv('ROEG_FINALE_RESUME_PHASE'))
local out=assert(os.getenv('ROEG_FINALE_RESUME_OUTPUT'))
local old_stage,old_submit,old_restore=Game.new_stage,Game.submit,Game.restore
local tracked,building,prepared=nil,false,false
local dirs={{0,-1},{1,0},{0,1},{-1,0}}
local function near(state,object)
    local player=Game.player(state)
    local map=Content.map_for(state)
    for _,d in ipairs(dirs) do
        local x,y=object.position.x+d[1],object.position.y+d[2]
        local free=Content.walkable(map,x,y)
        for _,actor in ipairs(state.entities) do
            if actor.id~=player.id and actor.blocks_movement
                and actor.position.x==x and actor.position.y==y then free=false end
        end
        if free then
            player.position.x,player.position.y=x,y
            Visibility.recompute(state)
            return -d[1],-d[2]
        end
    end
    error('no prepared approach')
end
function Game.new_stage(seed,options)
    if phase~='save' or building or prepared then
        tracked=old_stage(seed,options);return tracked
    end
    building=true;prepared=true
    local state=old_stage(seed,options)
    for index=1,3 do
        for _,actor in ipairs(state.entities) do
            if actor.controller=='ai' then Scheduler.remove_for_actor(state,actor.id) end
        end
        local elite=assert(Game.entity(state,state.stage.elite_id))
        local player=Game.player(state)
        player.max_hp,player.hp,player.damage=300,300,100
        local dx,dy=near(state,elite)
        assert(old_submit(state,{kind='basic_attack',dx=dx,dy=dy}))
        local exit=assert(Game.entity(state,state.stage.exit_id))
        near(state,exit)
        assert(old_submit(state,{kind='choose_route',exit_id=exit.id,
            route_index=index==1 and 1 or index==2 and 2 or 1}))
    end
    building=false;tracked=state
    return state
end
function Game.restore(snapshot)
    tracked=old_restore(snapshot);return tracked
end
function Game.submit(state,...)
    tracked=state;return old_submit(state,...)
end
assert(love.filesystem.load('roeg_main.lua'))()
local old_load,old_draw,old_update=love.load,love.draw,love.update
local labels,step,pending={},1,false
local old_print,old_printf=love.graphics.print,love.graphics.printf
love.graphics.print=function(value,...) labels[#labels+1]=tostring(value);return old_print(value,...) end
love.graphics.printf=function(value,...) labels[#labels+1]=tostring(value);return old_printf(value,...) end
local function has(fragment)
    for _,label in ipairs(labels) do if label:find(fragment,1,true) then return true end end
end
local function write(path,bytes)
    local file=assert(io.open(path,'wb'));file:write(bytes);file:close()
end
local function read(path)
    local file=assert(io.open(path,'rb'));local bytes=file:read('*a');file:close();return bytes
end
function love.load(...)
    old_load(...)
    if phase=='save' then
        assert(tracked.finale and tracked.finale.phase=='active')
        local boss=Game.entity(tracked,tracked.finale.boss_id)
        local player=Game.player(tracked)
        player.damage,player.hp,player.max_hp=2,150,150
        player.position.x,player.position.y=boss.position.x,boss.position.y+1
        Visibility.recompute(tracked)
        Game.set_boon_stacks(tracked,player.id,'core:boon/temporal_echo','legendary',1)
        love.keypressed('f');love.keypressed('w');love.keypressed('return')
        assert(boss.pending_attack and #Game.pending_boon_effects(tracked)>0)
        write(out..'/expected',Persistence.encode(Game.snapshot(tracked)))
        local control=old_restore(Game.snapshot(tracked))
        assert(old_submit(control,{kind='wait'}))
        write(out..'/next',Persistence.encode(Game.snapshot(control)))
        old_update(100)
        love.keypressed('escape')
    end
end
function love.update(...) old_update(0) end
function love.draw(...)
    labels={};old_draw(...)
    if pending then return end
    if phase=='save' then
        if step>1 then return end
        assert(has('Save & Quit') and tracked.finale.phase=='active')
        pending=true
        love.graphics.captureScreenshot(function(image)
            write(out..'/save.png',image:encode('png'):getString())
            love.keypressed('2') -- production Save & Quit, then process exits
            step=2;pending=false
        end)
    elseif step==1 then
        assert(has('C / Enter: Continue'))
        pending=true
        love.graphics.captureScreenshot(function(image)
            write(out..'/title.png',image:encode('png'):getString())
            love.keypressed('c')
            assert(tracked and tracked.finale and
                Persistence.encode(Game.snapshot(tracked))==read(out..'/expected'))
            step=2;pending=false
        end)
    elseif step==2 then
        assert(has('HOLLOW REGENT') and tracked.finale.phase=='active')
        pending=true
        love.graphics.captureScreenshot(function(image)
            write(out..'/continued.png',image:encode('png'):getString())
            assert(Game.submit(tracked,{kind='wait'}))
            assert(Persistence.encode(Game.snapshot(tracked))==read(out..'/next'))
            love.event.quit()
        end)
    end
end
LUA
ROEG_GUI_AUTOSTART=1 ROEG_FINALE_RESUME_PHASE=save \
  ROEG_FINALE_RESUME_OUTPUT="$output_dir" XDG_DATA_HOME="$output_dir/data" \
  timeout 30s love "$output_dir"
ROEG_FINALE_RESUME_PHASE=load ROEG_FINALE_RESUME_OUTPUT="$output_dir" \
  XDG_DATA_HOME="$output_dir/data" timeout 30s love "$output_dir"
for file in expected next save.png title.png continued.png; do
    test -s "$output_dir/$file"
done
printf 'Finale disk resume passed: two fresh LÖVE processes in %s\n' "$output_dir"
