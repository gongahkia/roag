#!/bin/sh
# Prepared three-stage route; final interactions use real LÖVE key callbacks.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=$(mktemp -d "${TMPDIR:-/tmp}/roeg-finale-gui-XXXXXX")
cp "$project_dir/main.lua" "$output_dir/roeg_main.lua"
cp "$project_dir/conf.lua" "$output_dir/conf.lua"
cp -R "$project_dir/src" "$output_dir/src"
cat > "$output_dir/main.lua" <<'LUA'
local Game=require('src.game')
local Content=require('src.content')
local Scheduler=require('src.scheduler')
local Visibility=require('src.visibility')
local tracked,first_load=true,true
local old_stage,old_finale,old_submit=Game.new_stage,Game.new_finale,Game.submit
local constructing=false
local events={}
local dirs={{0,-1,'w'},{1,0,'d'},{0,1,'s'},{-1,0,'a'}}
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
            return ({['0,-1']='s',['1,0']='a',['0,1']='w',['-1,0']='d'})[d[1]..','..d[2]]
        end
    end
    error('fixture has no cardinal approach')
end
function Game.new_stage(seed,options)
    if constructing or not first_load then
        tracked=old_stage(seed,options)
        return tracked
    end
    first_load=false
    constructing=true
    local state=old_stage(seed,options)
    for index=1,3 do
        for _,actor in ipairs(state.entities) do
            if actor.controller=='ai' then Scheduler.remove_for_actor(state,actor.id) end
        end
        local elite=assert(Game.entity(state,state.stage.elite_id))
        local player=Game.player(state)
        player.max_hp,player.hp,player.damage=300,300,100
        local key=near(state,elite)
        local d=assert(({w={0,-1},d={1,0},s={0,1},a={-1,0}})[key])
        assert(old_submit(state,{kind='basic_attack',dx=d[1],dy=d[2]}))
        local exit=assert(Game.entity(state,state.stage.exit_id))
        if index<3 then
            near(state,exit)
            assert(old_submit(state,{kind='choose_route',exit_id=exit.id,
                route_index=index==1 and 1 or 2}))
        else
            near(state,exit)
        end
    end
    constructing=false
    tracked=state
    return state
end
function Game.new_finale(seed,options)
    local state=old_finale(seed,options)
    local boss=assert(Game.entity(state,state.finale.boss_id))
    local player=Game.player(state)
    player.position.x,player.position.y=boss.position.x,boss.position.y+1
    boss.hp=33 -- prepared near phase threshold; damage still uses Game.submit.
    player.hp,player.max_hp,player.damage=150,150,4
    Visibility.recompute(state)
    return state
end
function Game.submit(state,intent)
    local ok,reason,output,preview=old_submit(state,intent)
    tracked=state
    for _,event in ipairs(output or {}) do events[#events+1]=event end
    return ok,reason,output,preview
end
assert(love.filesystem.load('roeg_main.lua'))()
local old_load,old_draw,old_update=love.load,love.draw,love.update
local output_dir=assert(os.getenv('ROEG_FINALE_GUI_OUTPUT'))
local labels,phase,pending={},1,false
local old_print,old_printf=love.graphics.print,love.graphics.printf
love.graphics.print=function(value,...) labels[#labels+1]=tostring(value);return old_print(value,...) end
love.graphics.printf=function(value,...) labels[#labels+1]=tostring(value);return old_printf(value,...) end
local function has(fragment)
    for _,label in ipairs(labels) do
        if label:find(fragment,1,true) then return true end
    end
end
local function had(kind)
    for _,event in ipairs(events) do if event.kind==kind then return true end end
end
local function settle() old_update(100) end -- presentation time only
function love.load(...)
    old_load(...)
    assert(tracked.stage.index==3 and tracked.stage.phase=='extraction')
end
function love.update(...) old_update(0) end
function love.draw(...)
    labels={};old_draw(...)
    if pending or phase>7 then return end
    if phase==1 then
        assert(tracked.stage.index==3 and has('Stage 3 extraction'))
    elseif phase==2 then
        assert(has('Enter the Final Sanctum'),table.concat(labels,' | '))
    elseif phase==3 then
        assert(tracked.finale and tracked.finale.phase=='active'
            and has('HOLLOW REGENT') and not tracked.victory)
    elseif phase==4 then
        assert(had('WindupStarted') and #Game.pending_strikes(tracked)>0
            and has('LIVE WINDUPS'))
    elseif phase==5 then
        assert(had('RegentPhaseChanged') and has('phase 2'))
    elseif phase==6 then
        assert(tracked.victory and had('RegentDefeated') and had('Victory')
            and has('VICTORY') and has('Hollow Regent defeated'))
    elseif phase==7 then
        assert(tracked.stage and tracked.stage.index==1 and not tracked.finale
            and not tracked.victory and has('Overgrown Ruins'))
    end
    pending=true
    love.graphics.captureScreenshot(function(image)
        local file=assert(io.open(output_dir..'/'..phase..'.png','wb'))
        file:write(image:encode('png'):getString());file:close()
        if phase==1 then
            local exit=Game.entity(tracked,tracked.stage.exit_id)
            local p=Game.player(tracked).position
            for _,d in ipairs(dirs) do
                if p.x+d[1]==exit.position.x and p.y+d[2]==exit.position.y then
                    love.keypressed(d[3]);break
                end
            end
        elseif phase==2 then
            love.keypressed('return');settle()
            local player=Game.player(tracked)
            player.hp,player.max_hp,player.damage=150,150,4
        elseif phase==3 then
            love.keypressed('space');settle()
        elseif phase==4 then
            love.keypressed('f');love.keypressed('w');love.keypressed('return');settle()
        elseif phase==5 then
            Game.player(tracked).damage=100 -- assisted fixture kill; real callback resolves it
            love.keypressed('f');love.keypressed('w');love.keypressed('return');settle()
        elseif phase==6 then
            love.keypressed('n');settle()
        else
            love.event.quit()
        end
        phase=phase+1;pending=false
    end)
end
LUA
ROEG_GUI_AUTOSTART=1 ROEG_FINALE_GUI_OUTPUT="$output_dir" \
  XDG_DATA_HOME="$output_dir/data" timeout 30s love "$output_dir"
for image in 1 2 3 4 5 6 7; do test -s "$output_dir/$image.png"; done
printf 'Finale GUI smoke passed: route, telegraph, phase, Victory, New Run in %s\n' "$output_dir"
