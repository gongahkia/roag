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
local original_new = Game.new
local tracked
function Game.new(seed, map_id)
    -- Preserve the original Tranche 01 no-enemy input/camera smoke scenario.
    tracked = original_new(seed, map_id)
    return tracked
end

assert(love.filesystem.load("roeg_main.lua"))()
local original_load, original_update, original_draw = love.load, love.update, love.draw
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
    love.keypressed("f")
    love.keypressed("e")
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
        "unexpected game state in GUI phase " .. phase)
    pending = true
    love.graphics.captureScreenshot(function(image)
        local png = image:encode("png")
        local file = assert(io.open(output_dir .. "/phase" .. phase .. ".png", "wb"))
        file:write(png:getString())
        file:close()
        if phase == 1 then
            love.keypressed("return")
            love.keypressed("r")
            love.keypressed("w")
        elseif phase == 2 then
            love.keypressed("return")
            love.keypressed("x")
            love.keypressed("a")
        elseif phase == 3 then
            love.keypressed("return")
            for _ = 1, 15 do love.keypressed("d") end
            original_update(2) -- settle presentation camera only
        else
            love.event.quit()
        end
        phase = phase + 1
        pending = false
    end)
end
LUA

ROEG_GUI_SMOKE_OUTPUT="$output_dir" timeout 15s love "$output_dir"
for phase in 1 2 3 4; do
    test -s "$output_dir/phase$phase.png"
done
printf 'GUI smoke passed: four screenshots in %s\n' "$output_dir"
