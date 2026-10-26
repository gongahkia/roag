#!/bin/sh
# Callback-driven LÖVE render/input smoke for the actual chest menu and reward HUD.
set -eu

project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=$(mktemp -d "${TMPDIR:-/tmp}/roag-reward-gui-XXXXXX")
cp "$project_dir/main.lua" "$output_dir/roag_main.lua"
cp "$project_dir/conf.lua" "$output_dir/conf.lua"
cp -R "$project_dir/src" "$output_dir/src"
mkdir -p "$output_dir/assets"
cp -R "$project_dir/assets/fonts" "$output_dir/assets/fonts"

cat > "$output_dir/main.lua" <<'LUA'
local Game = require("src.game")
local BoonContent = require('src.boon_content')
local original_new = Game.new
local tracked, chest
function Game.new(seed, map_id)
    tracked = original_new(seed, "core:map/scrolling_room")
    local guard = Game.spawn_enemy(tracked,"core:enemy/mossbound_guard",4,3)
    local skitter = Game.spawn_enemy(tracked,"core:enemy/ruin_skitter",3,4)
    require("src.scheduler").remove_for_actor(tracked,guard.id)
    require("src.scheduler").remove_for_actor(tracked,skitter.id)
    guard.hp = 1
    chest = Game.spawn_chest(tracked,"core:chest/standard",5,3)
    return tracked
end
assert(love.filesystem.load("roag_main.lua"))()
local original_load, original_update, original_draw = love.load, love.update, love.draw
local output_dir = assert(os.getenv("ROAG_REWARD_GUI_OUTPUT"))
local phase, pending, labels = 1, false, {}
local old_print, old_printf = love.graphics.print, love.graphics.printf
love.graphics.print = function(value, ...)
    labels[#labels+1] = tostring(value)
    return old_print(value, ...)
end
love.graphics.printf = function(value, ...)
    labels[#labels+1] = tostring(value)
    return old_printf(value, ...)
end
local function has(fragment)
    for _, label in ipairs(labels) do
        if label:find(fragment, 1, true) then return true end
    end
    return false
end
local function settle()
    local steps=0
    while love.roagPlaybackPending() do
        original_update(0.5)
        steps=steps+1
        assert(steps<100,"reward playback failed to drain")
    end
end
function love.load(...)
    original_load(...)
    assert(tracked.clock == 0 and tracked.currency == 0)
    assert(next(Game.player(tracked).boons) == nil)
end
function love.update() original_update(0) end
function love.draw(...)
    labels = {}
    original_draw(...)
    if phase > 4 or pending then return end
    assert(has('GOLD  ') and has('LV '))
    if phase == 1 then
        -- Boon details moved out of the permanent sidebar into the I menu.
        assert(has('GOLD  0') and has('LV 1') and has('I Boons / Stats'))
    elseif phase == 2 then
        assert(has('LV 2') and has('GOLD  0') and has('Level 2:'))
        assert(tracked.progression.xp_total == 5 and #tracked.drops == 1)
    elseif phase == 3 then
        assert(has("Boon Chest") and has("Enter claim") and has("power "))
        assert(has('GOLD  6') and has('LV 2'))
        assert(tracked.currency == 6 and not chest.claimed)
    else
        assert(has('GOLD  1') and
            has(BoonContent.get(chest.offers[2].boon_id).name))
        assert(chest.claimed and tracked.currency == 1)
    end
    pending = true
    love.graphics.captureScreenshot(function(image)
        local file = assert(io.open(output_dir .. "/phase" .. phase .. ".png", "wb"))
        file:write(image:encode("png"):getString())
        file:close()
        if phase == 1 then
            love.keypressed("f"); love.keypressed("d"); love.keypressed("return")
            original_update(0.6) -- clear death playback, retain nonblocking level notice
            assert(tracked.progression.level == 2 and tracked.currency == 0)
        elseif phase == 2 then
            love.keypressed("d") -- step onto Guard drop; four currency
            assert(tracked.currency == 4)
            settle()
            love.keypressed("f"); love.keypressed("z"); love.keypressed("return")
            settle()
            love.keypressed("a"); settle()
            love.keypressed("s"); settle() -- step onto Skitter drop
            assert(tracked.currency == 6)
            love.keypressed("w"); settle()
            love.keypressed("d"); settle()
            local before = tracked.clock
            love.keypressed("d") -- bump chest; menu is a free preview
            assert(tracked.clock == before)
            love.keypressed("space") -- menu focus blocks ordinary actions
            assert(tracked.clock == before)
            love.keypressed("escape") -- cancel retains offers and currency
            assert(tracked.clock == before and tracked.currency == 6)
            love.keypressed("d")
        elseif phase == 3 then
            local offer = chest.offers[2]
            love.keypressed("2")
            love.keypressed("return")
            assert(tracked.currency == 1 and chest.claimed)
            assert(Game.player(tracked).boons[offer.boon_id][offer.rarity] == 1)
        else
            love.event.quit()
        end
        phase = phase + 1
        pending = false
    end)
end
local checked_draw=love.draw
function love.draw(...)
    local ok,why=pcall(checked_draw,...)
    if not ok then
        io.stderr:write('Reward draw assertion: '..tostring(why)..'\n')
        love.event.quit(2)
    end
end

LUA

ROAG_REWARD_GUI_OUTPUT="$output_dir" ROAG_GUI_AUTOSTART=1 timeout -k 3s 15s love "$output_dir"
for phase in 1 2 3 4; do test -s "$output_dir/phase$phase.png"; done
printf 'Reward GUI smoke passed: four screenshots in %s\n' "$output_dir"
