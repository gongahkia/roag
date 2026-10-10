#!/bin/sh
# LÖVE smoke for locked windup playback. Saves screenshots for visual inspection.
set -eu

project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=$(mktemp -d "${TMPDIR:-/tmp}/roeg-enemy-gui-XXXXXX")
cp "$project_dir/main.lua" "$output_dir/roeg_main.lua"
cp "$project_dir/conf.lua" "$output_dir/conf.lua"
cp -R "$project_dir/src" "$output_dir/src"

cat > "$output_dir/main.lua" <<'LUA'
local Game = require("src.game")
local Playback = require("src.playback")
local original_new, original_submit, original_add = Game.new, Game.submit, Playback.add
local tracked, playback, events
local run_number = 0
function Game.new(...)
    run_number = run_number + 1
    tracked = original_new(...)
    if run_number == 2 then
        for _, entity in ipairs(tracked.entities) do
            if entity.ai_id == "spitter" then entity.speed = 60 end
        end
    end
    return tracked
end
function Game.submit(...)
    local ok, reason, output = original_submit(...)
    events = output
    return ok, reason, output
end
function Playback.add(queue, output, player_id)
    playback = queue
    return original_add(queue, output, player_id)
end

assert(love.filesystem.load("roeg_main.lua"))()
local original_load, original_update, original_draw = love.load, love.update, love.draw
local phase, pending = 1, false
local output_dir = assert(os.getenv("ROEG_ENEMY_GUI_OUTPUT"))
local expected = {"WindupStarted", "AttackPerformed", "DamageTaken", false, false}

function love.load(...)
    original_load(...)
    local enemy_count, chest_count = 0, 0
    for _, entity in ipairs(tracked.entities) do
        if entity.controller == "ai" then enemy_count = enemy_count + 1 end
        if entity.controller == "chest" then chest_count = chest_count + 1 end
    end
    assert(enemy_count == 3 and chest_count == 1)
    original_update(10)
    assert(tracked.clock == 0) -- idle presentation cannot advance AI
    love.keypressed("space")
    assert(tracked.clock == 100 and Game.player(tracked).hp == 22)
    local windup, impact
    for index, event in ipairs(events) do
        if event.kind == "WindupStarted" and not windup then windup = index end
        if event.kind == "AttackPerformed" and event.source_id ~= tracked.player_id
            and not impact then impact = index end
    end
    assert(windup and impact and windup < impact)
end

function love.update()
    original_update(0) -- controlled presentation stepping below
end

function love.draw(...)
    original_draw(...)
    if phase > 5 or pending then return end
    local current = Playback.current(playback)
    assert(tracked.clock == 100 and (current and current.kind or false) == expected[phase],
        "wrong playback order or simulation time in phase " .. phase)
    if phase == 5 then
        local strikes = Game.pending_strikes(tracked)
        assert(#strikes == 1 and strikes[1].due == 150)
    end
    pending = true
    love.graphics.captureScreenshot(function(image)
        local png = image:encode("png")
        local file = assert(io.open(output_dir .. "/phase" .. phase .. ".png", "wb"))
        file:write(png:getString())
        file:close()
        if phase == 1 then
            love.keypressed("space") -- playback gates input; no extra turn
            assert(tracked.clock == 100)
            original_update(0.56)
        elseif phase == 2 then
            original_update(0.27)
        elseif phase == 3 then
            original_update(1)
        elseif phase == 4 then
            love.keypressed("n")
            love.keypressed("space")
            original_update(1) -- clear windup history; leave live future strike visible
        else
            love.event.quit()
        end
        phase = phase + 1
        pending = false
    end)
end
LUA

ROEG_ENEMY_GUI_OUTPUT="$output_dir" timeout 15s love "$output_dir"
for phase in 1 2 3 4 5; do
    test -s "$output_dir/phase$phase.png"
done
printf 'Enemy GUI smoke passed: five screenshots in %s\n' "$output_dir"
