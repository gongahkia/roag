#!/bin/sh
# New plain-data boon prime survives real Save & Quit / Continue LÖVE processes.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=$(mktemp -d "${TMPDIR:-/tmp}/roag-m6-resume-XXXXXX")
cp "$project_dir/main.lua" "$output_dir/roag_main.lua"
cp "$project_dir/conf.lua" "$output_dir/conf.lua"
cp -R "$project_dir/src" "$output_dir/src"
mkdir -p "$output_dir/data"
cat > "$output_dir/main.lua" <<'LUA'
local Game=require("src.game")
local Persistence=require("src.persistence")
local phase=assert(os.getenv("ROAG_M6_RESUME_PHASE"))
local out=assert(os.getenv("ROAG_M6_RESUME_OUTPUT"))
local tracked
local old_new,old_restore=Game.new_stage,Game.restore
function Game.new_stage(seed,options)
    local created=old_new(seed,options)
    if phase=="save" then tracked=created end
    return created
end
function Game.restore(...)
    local restored=old_restore(...)
    if phase=="load" then tracked=restored end
    return restored
end
assert(love.filesystem.load("roag_main.lua"))()
local old_load,old_draw=love.load,love.draw
local function write(name,bytes)
    local f=assert(io.open(out.."/"..name,"wb"));f:write(bytes);f:close()
end
local function read(name)
    local f=assert(io.open(out.."/"..name,"rb"));local s=f:read("*a");f:close();return s
end
function love.load(...)
    old_load(...)
    if phase~="save" then return end
    love.keypressed("n")
    assert(tracked and tracked.floor_order)
    Game.set_boon_stacks(tracked,tracked.player_id,
        "core:boon/rhythm_of_steel","common",1)
    local committed=false
    for _,d in ipairs({{1,0},{0,1},{-1,0},{0,-1}}) do
        local p=Game.preview(tracked,"move",d[1],d[2])
        if p.valid and p.distance==1 then
            assert(Game.submit(tracked,{kind="move",dx=d[1],dy=d[2]}))
            committed=true;break
        end
    end
    assert(committed and Game.player(tracked).boon_memory
        and Game.player(tracked).boon_memory["core:boon/rhythm_of_steel"].charges==1)
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
            assert(tracked and Game.player(tracked).boon_memory)
            write("actual",Persistence.encode(Game.snapshot(tracked)))
            assert(Game.submit(tracked,{kind="wait"}))
            write("continued",Persistence.encode(Game.snapshot(tracked)))
            love.event.quit()
        end
    end)
end
LUA
ROAG_M6_RESUME_PHASE=save ROAG_M6_RESUME_OUTPUT="$output_dir" \
    XDG_DATA_HOME="$output_dir/data" timeout 25s love "$output_dir" \
    >"$output_dir/save.log" 2>&1
ROAG_M6_RESUME_PHASE=load ROAG_M6_RESUME_OUTPUT="$output_dir" \
    XDG_DATA_HOME="$output_dir/data" timeout 25s love "$output_dir" \
    >"$output_dir/load.log" 2>&1
test -s "$output_dir/save.png"
test -s "$output_dir/load.png"
cmp "$output_dir/expected" "$output_dir/actual"
cmp "$output_dir/next" "$output_dir/continued"
printf 'M6 boon disk Continue PASS: %s\n' "$output_dir"
