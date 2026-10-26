#!/bin/sh
# Callback-driven Ranger transform and purchased-catalogue presentation fixture.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=$(mktemp -d "${TMPDIR:-/tmp}/roag-m6-gui-XXXXXX")
cp "$project_dir/main.lua" "$output_dir/roag_main.lua"
cp "$project_dir/conf.lua" "$output_dir/conf.lua"
cp -R "$project_dir/src" "$output_dir/src"
mkdir -p "$output_dir/assets"
cp -R "$project_dir/assets/fonts" "$output_dir/assets/fonts"
cat > "$output_dir/main.lua" <<'LUA'
local Game=require("src.game")
local Scheduler=require("src.scheduler")
local Playback=require("src.playback")
local Classes=require("src.classes")
local state,first,branch,events,queue
function Game.new_stage(seed)
    state=Game.new(seed,"core:map/test_room",{class_id=Classes.order[2]})
    first=Game.spawn_enemy(state,"core:enemy/mossbound_guard",3,2)
    branch=Game.spawn_enemy(state,"core:enemy/mossbound_guard",3,3)
    for _,enemy in ipairs({first,branch}) do
        Scheduler.remove_for_actor(state,enemy.id)
        enemy.hp=30;enemy.max_hp=30
    end
    return state
end
local submit=Game.submit
function Game.submit(...)
    local ok,reason,output,preview=submit(...)
    events=output
    return ok,reason,output,preview
end
local add=Playback.add
function Playback.add(playback,...)
    queue=playback
    return add(playback,...)
end
assert(love.filesystem.load("roag_main.lua"))()
local old_load,old_update,old_draw=love.load,love.update,love.draw
local out=assert(os.getenv("ROAG_M6_GUI_OUTPUT"))
local phase,pending=1,false
local function has(kind,boon_id)
    for _,event in ipairs(events or {}) do
        if event.kind==kind and (not boon_id or event.boon_id==boon_id) then
            return true
        end
    end
    return false
end
function love.load(...)
    old_load(...)
    Game.set_boon_stacks(state,state.player_id,"core:boon/splitflight","rare",1)
    Game.set_boon_stacks(state,state.player_id,"core:boon/rhythm_of_steel","common",1)
    assert(Game.player(state).definition_id==Classes.order[2] and state.clock==0)
end
function love.update() old_update(0) end
function love.draw(...)
    old_draw(...)
    if pending or phase>4 then return end
    pending=true
    love.graphics.captureScreenshot(function(image)
        local file=assert(io.open(out.."/phase"..phase..".png","wb"))
        file:write(image:encode("png"):getString());file:close()
        if phase==1 then
            love.keypressed("f");love.keypressed("d");love.keypressed("return")
            assert(first.hp<30 and branch.hp<30 and state.clock>0)
            assert(Playback.busy(queue))
            local clock=state.clock
            love.keypressed("space")
            assert(state.clock==clock,"playback accepted hidden wait")
        elseif phase==2 then
            old_update(10)
            love.keypressed("s")
            assert(has("BoonActivated","core:boon/rhythm_of_steel"))
            assert(Game.player(state).position.y==3)
        elseif phase==3 then
            old_update(10)
            local before=branch.hp
            love.keypressed("f");love.keypressed("d");love.keypressed("return")
            assert(has("BoonActivated","core:boon/rhythm_of_steel")
                and branch.hp<before-4,"primed Ranger shot did not apply bonus")
        else love.event.quit() end
        phase=phase+1;pending=false
    end)
end
LUA
ROAG_M6_GUI_OUTPUT="$output_dir" ROAG_GUI_AUTOSTART=1 \
    timeout 20s love "$output_dir" >"$output_dir/love.log" 2>&1
for phase in 1 2 3 4; do test -s "$output_dir/phase$phase.png"; done
printf 'M6 boon GUI PASS: Ranger branch/rhythm/input gate in %s\n' "$output_dir"
