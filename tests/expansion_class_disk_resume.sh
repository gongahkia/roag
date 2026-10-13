#!/bin/sh
# A chosen class and its authored ability survive two independent LÖVE processes.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=$(mktemp -d "${TMPDIR:-/tmp}/roeg-class-resume-XXXXXX")
cp "$project_dir/main.lua" "$output_dir/roeg_main.lua"
cp "$project_dir/conf.lua" "$output_dir/conf.lua"
cp -R "$project_dir/src" "$output_dir/src"
mkdir -p "$output_dir/data"
cat > "$output_dir/main.lua" <<'LUA'
local Game=require("src.game")
local Persistence=require("src.persistence")
local Classes=require("src.classes")
local phase=assert(os.getenv("ROEG_CLASS_RESUME_PHASE"))
local tracked
local old_new,old_restore=Game.new_stage,Game.restore
function Game.new_stage(seed,options)
    tracked=old_new(seed,options)
    return tracked
end
function Game.restore(...)
    local restored=old_restore(...)
    if phase=="load" then tracked=restored end
    return restored
end
assert(love.filesystem.load("roeg_main.lua"))()
local old_load,old_draw=love.load,love.draw
local out=assert(os.getenv("ROEG_CLASS_RESUME_OUTPUT"))
local function write(name,bytes)
    local f=assert(io.open(out.."/"..name,"wb"));f:write(bytes);f:close()
end
local function read(name)
    local f=assert(io.open(out.."/"..name,"rb"));local s=f:read("*a");f:close();return s
end
function love.load(...)
    old_load(...)
    if phase=="save" then
        love.keypressed("2")
        love.keypressed("n")
        assert(tracked and Game.player(tracked).definition_id==Classes.order[2])
        assert(tracked.floor_order and #tracked.floor_order==2)
        assert(Game.submit(tracked,{kind="wait"}))
        write("expected",Persistence.encode(Game.snapshot(tracked)))
        local control=old_restore(Game.snapshot(tracked))
        assert(Game.submit(control,{kind="wait"}))
        write("next",Persistence.encode(Game.snapshot(control)))
    end
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
            assert(tracked and Game.player(tracked).definition_id==Classes.order[2])
            write("actual",Persistence.encode(Game.snapshot(tracked)))
            assert(Game.submit(tracked,{kind="wait"}))
            write("continued",Persistence.encode(Game.snapshot(tracked)))
            love.event.quit()
        end
    end)
end
LUA
ROEG_GUI_AUTOSTART=0 ROEG_CLASS_RESUME_PHASE=save \
    ROEG_CLASS_RESUME_OUTPUT="$output_dir" XDG_DATA_HOME="$output_dir/data" \
    timeout 25s love "$output_dir" >"$output_dir/save.log" 2>&1
ROEG_CLASS_RESUME_PHASE=load \
    ROEG_CLASS_RESUME_OUTPUT="$output_dir" XDG_DATA_HOME="$output_dir/data" \
    timeout 25s love "$output_dir" >"$output_dir/load.log" 2>&1
test -s "$output_dir/save.png"
test -s "$output_dir/load.png"
cmp "$output_dir/expected" "$output_dir/actual"
cmp "$output_dir/next" "$output_dir/continued"
printf 'Class disk Continue PASS: %s\n' "$output_dir"
