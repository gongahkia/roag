#!/bin/sh
# Callback-driven LÖVE smoke for the developer loadout and authoritative boon playback.
set -eu

project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=$(mktemp -d "${TMPDIR:-/tmp}/roeg-boon-gui-XXXXXX")
cp "$project_dir/main.lua" "$output_dir/roeg_main.lua"
cp "$project_dir/conf.lua" "$output_dir/conf.lua"
cp -R "$project_dir/src" "$output_dir/src"

cat > "$output_dir/main.lua" <<'LUA'
local Game = require("src.game")
local Playback = require("src.playback")
local Scheduler = require("src.scheduler")
local original_new, original_submit = Game.new, Game.submit
local tracked, events, playback
function Game.new(...)
    tracked = original_new(...)
    local first = Game.spawn_enemy(tracked, "core:enemy/ruin_skitter", 4, 3)
    local second = Game.spawn_enemy(tracked, "core:enemy/thornspitter", 5, 3)
    Scheduler.remove_for_actor(tracked, first.id)
    Scheduler.remove_for_actor(tracked, second.id)
    return tracked
end
function Game.submit(...)
    local ok, reason, output = original_submit(...)
    events = output
    return ok, reason, output
end
local original_add = Playback.add
function Playback.add(queue, output, player_id)
    playback = queue
    return original_add(queue, output, player_id)
end

assert(love.filesystem.load("roeg_main.lua"))()
local original_load, original_update, original_draw = love.load, love.update, love.draw
local output_dir = assert(os.getenv("ROEG_BOON_GUI_OUTPUT"))
local phase, pending = 1, false
local function save()
    pending = true
    love.graphics.captureScreenshot(function(image)
        local file = assert(io.open(output_dir .. "/phase" .. phase .. ".png", "wb"))
        file:write(image:encode("png"):getString())
        file:close()
        if phase == 1 then
            love.keypressed("b"); love.keypressed("b") -- developer Cascade preset
            assert(tracked.clock == 0)
        elseif phase == 2 then
            love.keypressed("f"); love.keypressed("d"); love.keypressed("return")
            assert(tracked.clock == 100 and events)
            local activated, applied = false, false
            for _, event in ipairs(events) do
                if event.kind == "BoonActivated" then activated = true end
                if event.kind == "EffectApplied" then applied = true end
            end
            assert(activated and applied)
            for _ = 1, 80 do
                local current = Playback.current(playback)
                if current and current.kind == "BoonActivated" then break end
                original_update(0.3)
            end
            assert(Playback.current(playback).kind == "BoonActivated")
        else
            love.keypressed("space") -- playback gates authoritative input
            original_update(0.1)
            assert(tracked.clock == 100)
            love.event.quit()
        end
        phase = phase + 1
        pending = false
    end)
end

function love.load(...)
    original_load(...)
    assert(tracked.clock == 0 and next(Game.player(tracked).boons) == nil)
end
function love.update() original_update(0) end
function love.draw(...)
    original_draw(...)
    if phase > 3 or pending then return end
    if phase == 2 then
        assert(tracked.clock == 0 and Game.player(tracked).boons["core:boon/storm_conductor"])
    elseif phase == 3 then
        assert(tracked.clock == 100 and Playback.current(playback).kind == "BoonActivated")
    end
    save()
end
LUA

ROEG_BOON_GUI_OUTPUT="$output_dir" timeout 15s love "$output_dir"
for phase in 1 2 3; do test -s "$output_dir/phase$phase.png"; done
printf 'Boon GUI smoke passed: three screenshots in %s\n' "$output_dir"
