#!/bin/sh
# Callback-driven GUI check for Burning, Longblade geometry and the playback input gate.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=$(mktemp -d "${TMPDIR:-/tmp}/roag-build-gui-XXXXXX")
cp "$project_dir/main.lua" "$output_dir/roag_main.lua"
cp "$project_dir/conf.lua" "$output_dir/conf.lua"
cp -R "$project_dir/src" "$output_dir/src"
mkdir -p "$output_dir/assets"
cp -R "$project_dir/assets/fonts" "$output_dir/assets/fonts"
cat > "$output_dir/main.lua" <<'LUA'
local Game=require('src.game')
local Scheduler=require('src.scheduler')
local Playback=require('src.playback')
local state,near,far,events,queue
function Game.new_stage(seed)
    state=Game.new(seed,'core:map/test_room')
    near=Game.spawn_enemy(state,'core:enemy/mossbound_guard',3,2)
    far=Game.spawn_enemy(state,'core:enemy/mossbound_guard',4,2)
    Scheduler.remove_for_actor(state,near.id)
    Scheduler.remove_for_actor(state,far.id)
    return state
end
local original_submit=Game.submit
function Game.submit(...)
    local ok,reason,output=original_submit(...)
    events=output
    return ok,reason,output
end
local original_add=Playback.add
function Playback.add(p,...)
    queue=p
    return original_add(p,...)
end
assert(love.filesystem.load('roag_main.lua'))()
local old_load,old_update,old_draw=love.load,love.update,love.draw
local out=assert(os.getenv('ROAG_BUILD_GUI_OUTPUT'))
local phase,pending=1,false
local function has(kind,target_id)
    for _,event in ipairs(events or {}) do
        if event.kind==kind and (not target_id or event.target_id==target_id) then return true end
    end
    return false
end
function love.load(...)
    old_load(...)
    assert(state.clock==0 and not next(Game.player(state).boons))
end
function love.update() old_update(0) end
local function save()
    pending=true
    love.graphics.captureScreenshot(function(image)
        local file=assert(io.open(out..'/phase'..phase..'.png','wb'))
        file:write(image:encode('png'):getString());file:close()
        if phase==1 then
            for _=1,4 do love.keypressed('b') end -- Firestorm developer build
            assert(Game.player(state).boons['core:boon/emberbrand'])
            assert(state.clock==0)
        elseif phase==2 then
            love.keypressed('f');love.keypressed('d');love.keypressed('return')
            assert(state.clock==100 and has('StatusApplied',near.id))
            assert(Playback.busy(queue))
            love.keypressed('space') -- busy playback does not queue a hidden action
            assert(state.clock==100)
            old_update(10)
            assert(not Playback.busy(queue))
            love.keypressed('b') -- Blade and Lunge developer build
            assert(Game.player(state).boons['core:boon/longblade_geometry'])
            assert(state.clock==100)
        elseif phase==3 then
            local before=far.hp
            love.keypressed('f');love.keypressed('d');love.keypressed('return')
            assert(state.clock==200 and far.hp<before)
            assert(has('HitConfirmed',far.id))
            local attack
            for _,event in ipairs(events) do if event.kind=='AttackPerformed' then attack=event;break end end
            assert(attack and #attack.target_cells==2 and attack.target_cells[2].x==4)
        else
            love.event.quit()
        end
        phase=phase+1;pending=false
    end)
end
function love.draw(...)
    old_draw(...)
    if phase<=4 and not pending then save() end
end
LUA
ROAG_BUILD_GUI_OUTPUT="$output_dir" ROAG_GUI_AUTOSTART=1 timeout 15s love "$output_dir"
for phase in 1 2 3 4; do test -s "$output_dir/phase$phase.png"; done
printf 'Build GUI smoke passed: Burning, Longblade and input gate in %s\n' "$output_dir"
