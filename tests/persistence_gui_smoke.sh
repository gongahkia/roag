#!/bin/sh
# Two separate LÖVE processes share an isolated platform save directory.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=$(mktemp -d "${TMPDIR:-/tmp}/roeg-persistence-gui-XXXXXX")
data_dir="$output_dir/data"
mkdir -p "$data_dir"
cp "$project_dir/main.lua" "$output_dir/roeg_main.lua"
cp "$project_dir/conf.lua" "$output_dir/conf.lua"
cp -R "$project_dir/src" "$output_dir/src"
cat > "$output_dir/main.lua" <<'LUA'
local Game=require('src.game')
local tracked
local old_new,old_restore,old_submit=Game.new_stage,Game.restore,Game.submit
function Game.new_stage(...)
    local state=old_new(...)
    tracked=state
    return state
end
function Game.restore(...)
    local state=old_restore(...)
    tracked=state
    return state
end
function Game.submit(state,...)
    tracked=state
    return old_submit(state,...)
end
assert(love.filesystem.load('roeg_main.lua'))()
local old_load,old_draw,old_update=love.load,love.draw,love.update
local phase=assert(os.getenv('ROEG_PERSIST_PHASE'))
local output_dir=assert(os.getenv('ROEG_PERSIST_OUTPUT'))
local captured,title_pending,continued=false,false,false
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
function love.load(...)
    old_load(...)
    if phase=='save' then
        love.keypressed('n') -- title New Run
        assert(tracked and tracked.clock==0)
        love.keypressed('space')
        assert(tracked.clock==100)
        old_update(10)
        love.keypressed('escape') -- pause without world time
        assert(tracked.clock==100)
    else
        assert(not tracked or tracked.clock==100)
        love.keypressed('n') -- replacement requires confirmation
        love.keypressed('escape') -- cancel replacement at zero world time
    end
end
function love.draw(...)
    labels={}
    old_draw(...)
    if phase=='load' and not continued then
        if title_pending then return end
        assert(has('C / Enter: Continue'))
        title_pending=true
        love.graphics.captureScreenshot(function(image)
            local file=assert(io.open(output_dir..'/title.png','wb'))
            file:write(image:encode('png'):getString());file:close()
            love.keypressed('c') -- fresh process Continue
            assert(tracked and tracked.clock==100)
            continued=true
        end)
        return
    end
    if captured then return end
    assert(tracked.clock==100 and has('World time: 100'))
    if phase=='save' then assert(has('Save & Quit')) end
    captured=true
    love.graphics.captureScreenshot(function(image)
        local file=assert(io.open(output_dir..'/'..phase..'.png','wb'))
        file:write(image:encode('png'):getString());file:close()
        if phase=='save' then
            love.keypressed('2') -- verified write then quit
        else
            love.keypressed('space')
            assert(tracked.clock==200)
            love.event.quit()
        end
    end)
end
LUA
ROEG_PERSIST_PHASE=save ROEG_PERSIST_OUTPUT="$output_dir" XDG_DATA_HOME="$data_dir" timeout 25s love "$output_dir"
ROEG_PERSIST_PHASE=load ROEG_PERSIST_OUTPUT="$output_dir" XDG_DATA_HOME="$data_dir" timeout 25s love "$output_dir"
test -s "$output_dir/save.png"
test -s "$output_dir/title.png"
test -s "$output_dir/load.png"
printf 'Persistence GUI smoke passed: two-process disk Continue in %s\n' "$output_dir"
