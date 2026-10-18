#!/bin/sh
# Prepared combat fixtures, real renderer, isolated fresh processes. No physical
# input claim. Run under xvfb-run on Linux to avoid opening a desktop window.
set -eu
project=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output=${1:-$(mktemp -d "${TMPDIR:-/var/tmp}/roag-squad-gui-XXXXXX")}
mkdir -p "$output/app" "$output/data"
cp "$project/main.lua" "$output/app/roag_main.lua"
cp "$project/conf.lua" "$output/app/conf.lua"
cp -R "$project/src" "$project/assets" "$output/app/"
cat > "$output/app/main.lua" <<'LUA'
local G=require('src.game')
local S=require('src.enemy_squads')
local P=require('src.persistence')
local V=require('src.visibility')
local Scheduler=require('src.scheduler')
local kind=assert(os.getenv('ROAG_SQUAD_KIND'))
local pack=assert(os.getenv('ROAG_SPRITE_PACK'))
local out=assert(os.getenv('ROAG_SQUAD_OUTPUT'))
local phase=os.getenv('ROAG_SQUAD_PHASE') or 'render'
local state,leader,seen,cue_seen
local native_new_stage=G.new_stage
local function read(path)
    local f=assert(io.open(path,'rb'));local bytes=f:read('*a');f:close();return bytes
end
local function write(path,bytes)
    local f=assert(io.open(path,'wb'));f:write(bytes);f:close()
end
local function check(fn)
    local ok,why=xpcall(fn,debug.traceback)
    if not ok then
        write(out..'/failure-'..kind..'-'..pack..'-'..phase..'.txt',tostring(why))
        io.stderr:write(tostring(why)..'\n');love.event.quit(1)
    end
    return ok
end
function G.new_stage()
    if phase=='read' then state=G.restore(P.decode(read(out..'/pending.save')));return state end
    if phase=='generated' then
        state=native_new_stage(2,{stage_options={index=2,
            biome_id='core:biome/fractured_archive',route_history={'core:route/fractured_archive'}}})
        for _,e in ipairs(state.entities) do
            if e.definition_id=='core:enemy/ward_weaver' then leader=e end
        end
        assert(leader,'natural generated room did not contain reviewed wizard group')
        -- Prepared camera approach only. All squad placement/geometry is native;
        -- the following channel runs through genuine scheduled public actions.
        state.meta.eligible=false
        local p=G.player(state)
        p.position.x,p.position.y=leader.position.x,leader.position.y-1
        V.recompute(state)
        assert(G.submit(state,{kind='wait'}));assert(G.submit(state,{kind='wait'}))
        assert(leader.ai_memory.ward_ready)
        return state
    end
    state=G.new(37)
    state.meta.eligible=false
    local p=G.player(state);p.position.x,p.position.y=9,6
    p.hp,p.max_hp=100,100
    local roster=S.groups[kind]
    local points={{9,9},{8,8},{10,8},{7,8},{11,8},{9,8}}
    local count=kind=='captain_and_followers' and 6 or roster[3]+1
    for i=1,count do
        local role=i==1 and roster[1] or roster[2]
        local e=G.spawn_enemy(state,'core:enemy/'..role,points[i][1],points[i][2],10000)
        leader=leader or e
        e.squad_id='test:squad';e.squad_kind=kind;e.squad_leader_id=leader.id
        e.squad_home={floor_id=state.active_floor_id,x=9,y=9}
        if role=='ward_weaver' then e.ai_memory.ward_ready=true end
        if role=='shield_bearer' then e.ai_memory.shield_dx,e.ai_memory.shield_dy=0,-1 end
        if role=='rushwing' then e.hp=1 end
        if role=='snare_scribe' then
            state.enemy_snares={{source_id=e.id,floor_id=state.active_floor_id,x=8,y=6}}
        end
    end
    -- Commit a genuine support/artillery windup without changing scheduler rules.
    if kind=='healer_rushwings' or kind=='snares_and_siege' then
        Scheduler.remove_for_actor(state,leader.id)
        Scheduler.enqueue(state,{kind='actor_ready',due=0,actor_id=leader.id,is_player=false})
        assert(G.submit(state,{kind='wait'}))
        assert(leader.pending_attack and leader.pending_attack.support)
    end
    V.recompute(state)
    return state
end
assert(love.filesystem.load('roag_main.lua'))()
local old_load,old_update,old_draw=love.load,love.update,love.draw
local draw=love.graphics.draw
love.graphics.draw=function(image,quad,...)
    if quad and type(quad)=='userdata' and quad.typeOf and quad:typeOf('Quad') then
        local x,y,w,h=quad:getViewport()
        local iw,ih=image:getDimensions()
        if iw==784 and ih==352 and w==16 and h==16 then
            cue_seen[math.floor(y/16)*49+math.floor(x/16)]=true
        end
        if (pack=='kenney' and iw==203 and ih==186) or
            (pack=='onebit' and iw==784 and ih==352) then
            local stride,columns=pack=='kenney' and 17 or 16,pack=='kenney' and 12 or 49
            if w==16 and h==16 then seen[math.floor(y/stride)*columns+math.floor(x/stride)]=true end
        end
    end
    return draw(image,quad,...)
end
local baseline,frames,captured
function love.load(...)
    local args={...}
    check(function()
        old_load(unpack(args))
        old_update(10)
        baseline=P.encode(G.snapshot(state))
        for _=1,2 do love.keypressed('f5');old_update(0) end
        assert(P.encode(G.snapshot(state))==baseline,'F5 changed squad state')
        if phase=='write' then
            write(out..'/pending.save',baseline)
            local next_state=G.restore(G.snapshot(state))
            local _,_,events=G.submit(next_state,{kind='wait'})
            write(out..'/next.save',P.encode({state=G.snapshot(next_state),events=events}))
        elseif phase=='read' then
            assert(baseline==read(out..'/pending.save'),'fresh-process save changed')
            local next_state=G.restore(G.snapshot(state))
            local _,_,events=G.submit(next_state,{kind='wait'})
            assert(P.encode({state=G.snapshot(next_state),events=events})==read(out..'/next.save'),
                'fresh-process scheduled continuation changed')
        end
    end)
end
function love.update(dt)
    check(function()old_update(dt);frames=(frames or 0)+1 end)
end
function love.draw(...)
    if captured then return end
    local args={...}
    check(function()
        seen={};cue_seen={};old_draw(unpack(args))
        if (frames or 0)<3 then return end
        assert(P.encode(G.snapshot(state))==baseline,'render mutated run')
        local roster=S.groups[kind]
        if pack~='classic' then
            local assignments=pack=='kenney' and S.tiny or S.onebit
            assert(seen[assignments[roster[1]]] and seen[assignments[roster[2]]],
                'actual renderer did not draw both approved native actor cells')
            local cues={warded_bodyguards=636,healer_rushwings=535,snares_and_siege=715,
                shield_and_archer=239,captain_and_followers=527}
            assert(cue_seen[cues[kind]],'approved shared cue not drawn')
        end
        captured=true
        love.graphics.captureScreenshot(function(image)
            check(function()
                write(out..'/'..kind..'-'..pack..'-'..phase..'.png',image:encode('png'):getString())
                write(out..'/'..kind..'-'..pack..'-'..phase..'.ok','approved native cells; snapshot unchanged\n')
                love.event.quit()
            end)
        end)
    end)
end
LUA
for pack in kenney onebit classic; do
    for kind in warded_bodyguards healer_rushwings snares_and_siege shield_and_archer captain_and_followers; do
        ROAG_GUI_AUTOSTART=1 ROAG_SPRITE_PACK="$pack" ROAG_SQUAD_KIND="$kind" \
            ROAG_SQUAD_OUTPUT="$output" XDG_DATA_HOME="$output/data/$pack-$kind" \
            timeout 25s love "$output/app" > "$output/$kind-$pack.log" 2>&1
        test -s "$output/$kind-$pack-render.ok"
    done
done
for phase in write read; do
    ROAG_GUI_AUTOSTART=1 ROAG_SPRITE_PACK=onebit ROAG_SQUAD_KIND=healer_rushwings \
        ROAG_SQUAD_PHASE="$phase" ROAG_SQUAD_OUTPUT="$output" XDG_DATA_HOME="$output/data/$phase" \
        timeout 25s love "$output/app" > "$output/persistence-$phase.log" 2>&1
    test -s "$output/healer_rushwings-onebit-$phase.ok"
done
for pack in kenney onebit; do
    ROAG_GUI_AUTOSTART=1 ROAG_SPRITE_PACK="$pack" ROAG_SQUAD_KIND=warded_bodyguards \
        ROAG_SQUAD_PHASE=generated ROAG_SQUAD_OUTPUT="$output" XDG_DATA_HOME="$output/data/generated-$pack" \
        timeout 25s love "$output/app" > "$output/generated-$pack.log" 2>&1
    test -s "$output/warded_bodyguards-$pack-generated.ok"
done
printf 'Squad GUI PASS: 15 pack/scenario + 2 disk + 2 natural generated-room processes; %s\n' "$output"
