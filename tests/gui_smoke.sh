#!/bin/sh
# LÖVE callback/render smoke. Screenshots are left in a temporary directory for inspection.
set -eu

project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=$(mktemp -d "${TMPDIR:-/tmp}/roeg-gui-XXXXXX")
cp "$project_dir/main.lua" "$output_dir/roeg_main.lua"
cp "$project_dir/conf.lua" "$output_dir/conf.lua"
cp -R "$project_dir/src" "$output_dir/src"

cat > "$output_dir/main.lua" <<'LUA'
local Game = require("src.game")
local Persistence = require("src.persistence")
local original_new = Game.new
local tracked
function Game.new(seed, map_id)
    -- Preserve the original Tranche 01 no-enemy input/camera smoke scenario.
    tracked = original_new(seed, "core:map/scrolling_room")
    return tracked
end

assert(love.filesystem.load("roeg_main.lua"))()
local original_load, original_update, original_draw = love.load, love.update, love.draw
local actual_keypressed=love.keypressed
local scripted=false
function love.keypressed(key,scan,isrepeat)
    if scripted then return actual_keypressed(key,scan,isrepeat) end
end
local function press(key)
    scripted=true
    local result=love.keypressed(key)
    scripted=false
    return result
end
local phase, pending = 1, false
local output_dir = assert(os.getenv("ROEG_GUI_SMOKE_OUTPUT"))
local expected = {
    {time=0, x=3, y=3},
    {time=100, x=3, y=3},
    {time=250, x=3, y=3},
    {time=1880, x=17, y=3},
}

function love.load(...)
    original_load(...)
    local before=Persistence.encode(Game.snapshot(tracked))
    press('f3');press('f3')
    assert(Persistence.encode(Game.snapshot(tracked))==before,
        'diagnostics toggle changed authoritative state')
    assert(love.roegDiagnostics()==nil,'F3 did not toggle diagnostics off')
    press("f")
    press("e")
end

function love.update(dt)
    original_update(dt)
end

function love.draw(...)
    original_draw(...)
    if phase > 4 or pending then return end
    local position = Game.player(tracked).position
    local check = expected[phase]
    assert(tracked.clock == check.time and position.x == check.x and position.y == check.y,
        ("unexpected game state in GUI phase %d: time=%d position=%d,%d expected=%d %d,%d")
            :format(phase,tracked.clock,position.x,position.y,
                check.time,check.x,check.y))
    pending = true
    love.graphics.captureScreenshot(function(image)
        local png = image:encode("png")
        local file = assert(io.open(output_dir .. "/phase" .. phase .. ".png", "wb"))
        file:write(png:getString())
        file:close()
        if phase == 1 then
            press("return")
            press("r")
            press("w")
        elseif phase == 2 then
            press("return")
            press("x")
            press("a")
        elseif phase == 3 then
            press("return")
            -- One action is accepted per completed visual beat. Drain the
            -- presentation gate between scripted key presses, as a person does.
            original_update(0.2)
            for _ = 1, 15 do
                press("d")
                original_update(0.1)
            end
            original_update(2) -- settle presentation camera only
        else
            love.event.quit()
        end
        phase = phase + 1
        pending = false
    end)
end
LUA

ROEG_GUI_SMOKE_OUTPUT="$output_dir" ROEG_GUI_AUTOSTART=1 timeout 15s love "$output_dir"
for phase in 1 2 3 4; do
    test -s "$output_dir/phase$phase.png"
done
printf 'GUI smoke passed: four screenshots in %s\n' "$output_dir"
