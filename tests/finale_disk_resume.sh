#!/bin/sh
# Fresh LÖVE process Save & Quit -> title Continue for a prepared final encounter.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=$(mktemp -d "${TMPDIR:-/tmp}/roag-finale-resume-XXXXXX")
cp "$project_dir/main.lua" "$output_dir/roag_main.lua"
cp "$project_dir/conf.lua" "$output_dir/conf.lua"
cp -R "$project_dir/src" "$output_dir/src"
mkdir -p "$output_dir/assets"
cp -R "$project_dir/assets/fonts" "$output_dir/assets/fonts"
cat > "$output_dir/main.lua" <<'LUA'
local Game=require('src.game')
local Scheduler=require('src.scheduler')
local Content=require('src.content')
local Visibility=require('src.visibility')
local Persistence=require('src.persistence')
local phase=assert(os.getenv('ROAG_FINALE_RESUME_PHASE'))
local out=assert(os.getenv('ROAG_FINALE_RESUME_OUTPUT'))
local old_stage,old_submit,old_restore=Game.new_stage,Game.submit,Game.restore
local tracked,building,prepared=nil,false,false
local last_submit
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
    tracked=state
    local before_damage=Game.player(state).damage
    local ok,reason,events,preview=old_submit(state,...)
    last_submit={intent=(...),ok=ok,reason=reason,events=events,
        before_damage=before_damage}
    return ok,reason,events,preview
end
assert(love.filesystem.load('roag_main.lua'))()
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
    elseif phase=='terminal' then
        assert(has('Continue unavailable') and has('RUN COMPLETE'))
        pending=true
        love.graphics.captureScreenshot(function(image)
            write(out..'/terminal.png',image:encode('png'):getString())
            love.keypressed('c')
            assert(not tracked)
            love.event.quit()
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
            local ok,reason=xpcall(function()
                write(out..'/continued.png',image:encode('png'):getString())
                love.keypressed('space')
                assert(Persistence.encode(Game.snapshot(tracked))==read(out..'/next'),
                    'continued action diverged')
                old_update(100)
                local live=tracked
                Game.player(live).damage=100 -- prepared terminal check
                love.keypressed('f');love.keypressed('w');love.keypressed('return')
                local boss=Game.entity(live,live.finale.boss_id)
                assert(live.victory and live.game_over,
                    ('victory callback failed: boss=%s hp=%s at=%s,%s player=%s,%s dmg=%s phase=%s last=%s submitted=%s/%s'):format(
                        tostring(boss and boss.id),tostring(boss and boss.hp),
                        tostring(boss and boss.position.x),tostring(boss and boss.position.y),
                        tostring(Game.player(live).position.x),tostring(Game.player(live).position.y),
                        tostring(Game.player(live).damage),live.finale.phase,
                        tostring(live.last_action and live.last_action.kind),
                        tostring(last_submit and last_submit.intent.kind),
                        tostring(last_submit and last_submit.reason)
                        ..' before_damage='..tostring(last_submit and last_submit.before_damage)))
                assert(Persistence.new(require('src.love_storage')):inspect().status=='terminal',
                    'terminal tombstone missing')
            end,debug.traceback)
            if not ok then write(out..'/load-error.txt',reason) end
            love.event.quit(ok and 0 or 1)
        end)
    end
end
LUA
ROAG_GUI_AUTOSTART=1 ROAG_FINALE_RESUME_PHASE=save \
  ROAG_FINALE_RESUME_OUTPUT="$output_dir" XDG_DATA_HOME="$output_dir/data" \
  timeout 30s love "$output_dir"
ROAG_FINALE_RESUME_PHASE=load ROAG_FINALE_RESUME_OUTPUT="$output_dir" \
  XDG_DATA_HOME="$output_dir/data" timeout 30s love "$output_dir"
ROAG_FINALE_RESUME_PHASE=terminal ROAG_FINALE_RESUME_OUTPUT="$output_dir" \
  XDG_DATA_HOME="$output_dir/data" timeout 30s love "$output_dir"
for file in expected next save.png title.png continued.png terminal.png; do
    test -s "$output_dir/$file"
done
printf 'Finale disk resume and terminal tombstone passed: three fresh LÖVE processes in %s\n' "$output_dir"
