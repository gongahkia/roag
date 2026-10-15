#!/bin/sh
# Prepared, callback-driven visual coverage of the real stage resolver and UI.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=$(mktemp -d "${TMPDIR:-/tmp}/roag-stage-gui-XXXXXX")
cp "$project_dir/main.lua" "$output_dir/roag_main.lua"
cp "$project_dir/conf.lua" "$output_dir/conf.lua"
cp -R "$project_dir/src" "$output_dir/src"

cat > "$output_dir/main.lua" <<'LUA'
local Game=require("src.game")
local Scheduler=require("src.scheduler")
local Content=require("src.content")
local Visibility=require("src.visibility")
local tracked
local old_new,old_submit=Game.new,Game.submit
function Game.new(seed,map_id,options)
    local state=old_new(seed,map_id,options)
    tracked=state
    -- Preserve the real elite/pressure/pursuer; freeze ordinary crowd during this
    -- prepared UI sequence so screenshots do not depend on prolonged survival.
    for _,e in ipairs(state.entities) do
        if e.controller=="ai" and e.ai_id~="warden" then
            Scheduler.remove_for_actor(state,e.id)
        end
    end
    return state
end
function Game.submit(state,intent)
    local a,b,c,d=old_submit(state,intent)
    tracked=state
    return a,b,c,d
end
assert(love.filesystem.load("roag_main.lua"))()
local old_load,old_update,old_draw=love.load,love.update,love.draw
local output_dir=assert(os.getenv("ROAG_STAGE_GUI_OUTPUT"))
local phase,pending=1,false
local labels={}
local old_print,old_printf=love.graphics.print,love.graphics.printf
love.graphics.print=function(value,...)
    labels[#labels+1]=tostring(value)
    return old_print(value,...)
end
love.graphics.printf=function(value,...)
    labels[#labels+1]=tostring(value)
    return old_printf(value,...)
end
local function has(fragment)
    for _,label in ipairs(labels) do
        if label:find(fragment,1,true) then return true end
    end
end
local dirs={{0,-1,"w"},{1,0,"d"},{0,1,"s"},{-1,0,"a"}}
local function near(entity)
    local p=Game.player(tracked)
    for _,d in ipairs(dirs) do
        local x,y=entity.position.x+d[1],entity.position.y+d[2]
        local free=Content.walkable(tracked.generated_map,x,y)
        for _,other in ipairs(tracked.entities) do
            if other.id~=p.id and other.blocks_movement
                and other.position.x==x and other.position.y==y then free=false end
        end
        if free then
            p.position.x,p.position.y=x,y
            Visibility.recompute(tracked)
            for _,aim in ipairs(dirs) do
                if aim[1]==-d[1] and aim[2]==-d[2] then return aim[3] end
            end
        end
    end
    error("no approach")
end
local function clear_playback() old_update(100) end
local function strike(entity)
    local direction=near(entity)
    clear_playback()
    love.keypressed("f");love.keypressed(direction);love.keypressed("return")
end
function love.load(...)
    old_load(...)
    assert(tracked.stage and tracked.stage.phase=="exploring")
end
function love.update() old_update(0) end
function love.draw(...)
    labels={}
    old_draw(...)
    if phase>6 or pending then return end
    if phase==1 then
        assert(has("Stage 1 exploring") and tracked.stage.elite_id)
    elseif phase==2 then
        assert(tracked.stage.phase=="elite_active" and
            Game.entity(tracked,tracked.stage.elite_id).phase_two)
        assert(has("THORN WARDEN") and has("phase 2"))
    elseif phase==3 then
        assert(tracked.stage.phase=="extraction" and
            Game.entity(tracked,tracked.stage.elite_reward_id).price==0)
        assert(Game.entity(tracked,tracked.stage.exit_id))
        assert(has("Extract:"))
    elseif phase==4 then
        assert(tracked.stage.phase=="pursuit" and tracked.stage.pursuer_id)
        assert(has("PURSUER ACTIVE"))
    elseif phase==5 then
        assert(has("Choose a route") and has("Fungal Hollows")
            and has("Sunken Aqueduct"))
    elseif phase==6 then
        assert(tracked.stage.index==2 and tracked.stage.phase=="exploring")
        assert(has("Stage 2 exploring") and tracked.generated_map.seed~=1
            and tracked.stage.biome_id=="core:biome/fungal_hollows"
            and has("Fungal Hollows"))
    end
    pending=true
    love.graphics.captureScreenshot(function(image)
        local file=assert(io.open(output_dir.."/phase"..phase..".png","wb"))
        file:write(image:encode("png"):getString());file:close()
        if phase==1 then
            local elite=Game.entity(tracked,tracked.stage.elite_id)
            near(elite)
            Game.player(tracked).hp=500
            Game.player(tracked).max_hp=500
            love.keypressed("space") -- commits engagement and a real elite windup.
            assert(tracked.stage.phase=="elite_active")
            elite.hp=24
            Game.player(tracked).damage=4
            strike(elite) -- crosses the second-phase threshold.
            assert(elite.phase_two)
        elseif phase==2 then
            local elite=Game.entity(tracked,tracked.stage.elite_id)
            elite.hp=1
            Game.player(tracked).damage=100
            strike(elite)
            assert(tracked.stage.phase=="extraction")
        elseif phase==3 then
            while tracked.clock<tracked.stage.deadline do
                clear_playback();love.keypressed("space")
            end
            assert(tracked.stage.phase=="pursuit")
        elseif phase==4 then
            local exit=Game.entity(tracked,tracked.stage.exit_id)
            local direction=near(exit)
            clear_playback()
            local clock=tracked.clock
            love.keypressed(direction)
            assert(tracked.clock==clock) -- route menu is presentation-only.
        elseif phase==5 then
            love.keypressed("1");love.keypressed("return")
            assert(tracked.stage.index==2)
        else
            love.event.quit()
        end
        phase=phase+1;pending=false
    end)
end
LUA

ROAG_STAGE_GUI_OUTPUT="$output_dir" ROAG_GUI_AUTOSTART=1 timeout 20s love "$output_dir"
for phase in 1 2 3 4 5 6; do test -s "$output_dir/phase$phase.png"; done
printf 'Stage GUI smoke passed: six screenshots in %s\n' "$output_dir"
