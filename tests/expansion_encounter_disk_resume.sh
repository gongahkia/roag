#!/bin/sh
# Fresh LÖVE processes save and continue a new biome elite's locked attack.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=$(mktemp -d "${TMPDIR:-/tmp}/roag-encounter-resume-XXXXXX")
cp "$project_dir/main.lua" "$output_dir/roag_main.lua"
cp "$project_dir/conf.lua" "$output_dir/conf.lua"
cp -R "$project_dir/src" "$output_dir/src"
mkdir -p "$output_dir/data"
cat > "$output_dir/main.lua" <<'LUA'
local Game=require("src.game")
local Biomes=require("src.biomes")
local Content=require("src.content")
local Scheduler=require("src.scheduler")
local Visibility=require("src.visibility")
local Persistence=require("src.persistence")
local tracked
local old_new,old_restore=Game.new_stage,Game.restore
function Game.new_stage(seed,options)
    tracked=old_new(seed,{stage_options={index=2,biome_id=Biomes.order[2],
        route_history={"core:route/fungal_hollows"}}})
    return tracked
end
function Game.restore(...)
    tracked=old_restore(...)
    return tracked
end
assert(love.filesystem.load("roag_main.lua"))()
local old_load,old_draw=love.load,love.draw
local phase=assert(os.getenv("ROAG_ENCOUNTER_RESUME_PHASE"))
local out=assert(os.getenv("ROAG_ENCOUNTER_RESUME_OUTPUT"))
local function write(name,bytes)
    local f=assert(io.open(out.."/"..name,"wb"));f:write(bytes);f:close()
end
local function read(name)
    local f=assert(io.open(out.."/"..name,"rb"));local s=f:read("*a");f:close();return s
end
function love.load(...)
    old_load(...)
    if phase~="save" then return end
    local elite=Game.entity(tracked,tracked.stage.elite_id)
    for _,actor in ipairs(tracked.entities) do
        if actor.controller=="ai" then Scheduler.remove_for_actor(tracked,actor.id) end
    end
    local player=Game.player(tracked)
    for _,d in ipairs({{1,0},{-1,0},{0,1},{0,-1}}) do
        local x,y=elite.position.x+d[1],elite.position.y+d[2]
        if Content.walkable(tracked.generated_map,x,y) then
            player.position.x,player.position.y=x,y;break
        end
    end
    player.hp,player.max_hp=100,100
    elite.dormant=nil;tracked.stage.phase="elite_active"
    Scheduler.enqueue(tracked,{kind="actor_ready",due=0,
        actor_id=elite.id,is_player=false})
    Visibility.recompute(tracked)
    assert(Game.submit(tracked,{kind="wait"}))
    assert(elite.pending_attack and
        elite.pending_attack.ability_id:find("core:ability/elite_",1,true))
    write("expected",Persistence.encode(Game.snapshot(tracked)))
    local control=old_restore(Game.snapshot(tracked))
    assert(Game.submit(control,{kind="wait"}))
    write("next",Persistence.encode(Game.snapshot(control)))
end
local done=false
function love.draw(...)
    old_draw(...)
    if done then return end
    done=true
    love.graphics.captureScreenshot(function(image)
        write(phase..".png",image:encode("png"):getString())
        if phase=="save" then
            love.keypressed("escape")
            love.keypressed("2")
        else
            love.keypressed("c")
            assert(tracked and Persistence.encode(Game.snapshot(tracked))==read("expected"),
                "fresh-process elite windup differs")
            assert(Game.submit(tracked,{kind="wait"}))
            assert(Persistence.encode(Game.snapshot(tracked))==read("next"),
                "fresh-process elite impact differs")
            love.event.quit()
        end
    end)
end
LUA
ROAG_GUI_AUTOSTART=1 ROAG_ENCOUNTER_RESUME_PHASE=save \
    ROAG_ENCOUNTER_RESUME_OUTPUT="$output_dir" XDG_DATA_HOME="$output_dir/data" \
    timeout 25s love "$output_dir" >"$output_dir/save.log" 2>&1
ROAG_ENCOUNTER_RESUME_PHASE=load \
    ROAG_ENCOUNTER_RESUME_OUTPUT="$output_dir" XDG_DATA_HOME="$output_dir/data" \
    timeout 25s love "$output_dir" >"$output_dir/load.log" 2>&1
test -s "$output_dir/save.png"
test -s "$output_dir/load.png"
printf 'Encounter disk Continue PASS: %s\n' "$output_dir"
