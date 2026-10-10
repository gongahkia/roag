local Camera = require("src.camera")
local BoonContent = require("src.boon_content")
local Content = require("src.content")
local Game = require("src.game")
local Playback = require("src.playback")

local TILE = 32
local VIEWPORT = { x = 16, y = 16, width = 704, height = 608 }
local PANEL_X = 742

local state
local camera
local camera_state
local targeting -- nil, or a presentation-only { kind, dx, dy }.
local notice = "Ready"
local playback = Playback.new()
local showcase_index = 0
local showcase = {
    {name="Lightning", boons={{"storm_conductor","rare",2},
        {"static_footsteps","rare",1}}},
    {name="Cascade", boons={{"storm_conductor","legendary",2},
        {"detonation_bloom","rare",2}, {"resonant_wounds","rare",1},
        {"temporal_echo","uncommon",1}}},
    {name="Transformation", boons={{"crescent_reach","rare",1},
        {"thorn_mirror","rare",1}, {"turncoat_spark","rare",1}}},
}

local function apply_showcase()
    local player = Game.player(state)
    for _, id in ipairs(BoonContent.order) do
        for _, rarity in ipairs(BoonContent.rarities) do
            Game.set_boon_stacks(state, player.id, id, rarity, 0)
        end
    end
    local selected = showcase[showcase_index]
    if selected then
        for _, entry in ipairs(selected.boons) do
            Game.set_boon_stacks(state, player.id, "core:boon/" .. entry[1], entry[2], entry[3])
        end
    end
end

local cardinal = {
    w = { 0, -1 }, up = { 0, -1 }, kp8 = { 0, -1 },
    s = { 0, 1 }, down = { 0, 1 }, kp2 = { 0, 1 },
    a = { -1, 0 }, left = { -1, 0 }, kp4 = { -1, 0 },
    d = { 1, 0 }, right = { 1, 0 }, kp6 = { 1, 0 },
}
local diagonal = {
    q = { -1, -1 }, kp7 = { -1, -1 },
    e = { 1, -1 }, kp9 = { 1, -1 },
    z = { -1, 1 }, kp1 = { -1, 1 },
    c = { 1, 1 }, kp3 = { 1, 1 },
}
local labels = {
    move = "Move", wait = "Wait", basic_attack = "Sword Strike",
    sweeping_slash = "Sweeping Slash", dash = "Dash",
}

local function reset_camera()
    local map = Content.get_map(state.map_id)
    local position = Game.player(state).position
    camera = Camera.new(map, VIEWPORT, TILE, position.x, position.y)
    camera_state = state
end

function love.load()
    state = Game.new(1, "core:map/scrolling_room", {demo_enemies=true})
    apply_showcase()
    reset_camera()
    love.graphics.setBackgroundColor(0.055, 0.075, 0.075)
end

function love.update(dt)
    if camera_state ~= state then reset_camera() end
    local position = Game.player(state).position
    Camera.update(camera, dt, position.x, position.y)
    Playback.update(playback, dt)
end

local function submit(intent)
    local committed, reason, events = Game.submit(state, intent)
    if committed then
        Playback.add(playback, events, state.player_id)
        local action = state.last_action
        if action.kind == "dash" then
            notice = ("Dash: %d/2 tiles, +%d time"):format(action.distance, action.cost)
        else
            notice = ("%s: +%d time"):format(labels[action.kind], action.cost)
        end
    else
        notice = (reason or "Invalid action") .. " — no time"
    end
    return committed
end

function love.keypressed(key, _, isrepeat)
    if isrepeat then return end
    if key == "n" then
        state = Game.new(1, "core:map/scrolling_room", {demo_enemies=true})
        apply_showcase()
        playback, targeting, notice = Playback.new(), nil, "New test run"
        reset_camera()
        return
    end
    if Playback.busy(playback) or state.game_over then return end
    if key == "b" and not targeting then
        showcase_index = (showcase_index + 1) % (#showcase + 1)
        apply_showcase()
        notice = "DEV loadout: " .. (showcase[showcase_index] and
            showcase[showcase_index].name or "None") .. " — no time"
        return
    end
    if targeting then
        if key == "escape" then
            targeting = nil
            notice = "Targeting cancelled — no time"
            return
        end
        local direction = cardinal[key] or diagonal[key]
        if direction then
            targeting.dx, targeting.dy = direction[1], direction[2]
            local preview = Game.preview(state, targeting.kind, targeting.dx, targeting.dy)
            notice = preview.valid and "Preview ready; Enter to commit" or
                ((preview.reason or "Invalid target") .. " — no time")
            return
        end
        if key == "return" or key == "kpenter" then
            if targeting.dx == nil then
                notice = "Choose a direction first — no time"
                return
            end
            local committed = submit({
                kind = targeting.kind, dx = targeting.dx, dy = targeting.dy,
            })
            if committed then targeting = nil end
        end
        return
    end

    if key == "f" then
        targeting = { kind = "basic_attack" }
        notice = "Sword Strike: choose direction, then Enter"
    elseif key == "r" then
        targeting = { kind = "sweeping_slash" }
        notice = "Sweeping Slash: choose direction, then Enter"
    elseif key == "x" then
        targeting = { kind = "dash" }
        notice = "Dash: choose cardinal direction, then Enter"
    elseif key == "space" or key == "." or key == "kp5" then
        submit({ kind = "wait" })
    elseif cardinal[key] then
        local direction = cardinal[key]
        submit({ kind = "move", dx = direction[1], dy = direction[2] })
    end
end

local function draw_cells(cells, red, green, blue, alpha)
    love.graphics.setColor(red, green, blue, alpha)
    for _, cell in ipairs(cells) do
        local screen_x, screen_y = Camera.tile_top_left(camera, cell.x, cell.y)
        love.graphics.rectangle("fill", screen_x + 2, screen_y + 2, TILE - 4, TILE - 4)
        love.graphics.setColor(red, green, blue, math.min(1, alpha + 0.3))
        love.graphics.rectangle("line", screen_x + 2, screen_y + 2, TILE - 4, TILE - 4)
        love.graphics.setColor(red, green, blue, alpha)
    end
end

local function draw_enemies()
    for _, enemy in ipairs(state.entities) do
        if enemy.controller == "ai" then
            local x, y = Camera.tile_top_left(camera, enemy.position.x, enemy.position.y)
            if enemy.ai_id == "guard" then
                love.graphics.setColor(0.42, 0.73, 0.43)
                love.graphics.rectangle("fill", x + 5, y + 5, TILE - 12, TILE - 13)
            elseif enemy.ai_id == "spitter" then
                love.graphics.setColor(0.70, 0.40, 0.80)
                love.graphics.polygon("fill", x + 15, y + 3, x + 28, y + 15,
                    x + 15, y + 27, x + 2, y + 15)
            else
                love.graphics.setColor(0.95, 0.49, 0.27)
                love.graphics.polygon("fill", x + 15, y + 3, x + 28, y + 26,
                    x + 2, y + 26)
            end
            love.graphics.setColor(0.16, 0.19, 0.16)
            love.graphics.rectangle("fill", x + 3, y + 28, TILE - 6, 3)
            love.graphics.setColor(0.94, 0.28, 0.29)
            love.graphics.rectangle("fill", x + 3, y + 28,
                (TILE - 6) * enemy.hp / enemy.max_hp, 3)
        end
    end
end

local function draw_history_event(event)
    if not event then return end
    if event.kind == "WindupStarted" then
        draw_cells(event.target_cells, 1, 0.64, 0.18, 0.5)
    elseif event.kind == "AttackPerformed" then
        draw_cells(event.target_cells, 1, 0.22, 0.20, 0.7)
    elseif event.kind == "WindupCancelled" then
        draw_cells(event.target_cells, 0.6, 0.6, 0.6, 0.4)
    elseif event.kind == "EffectApplied" or event.kind == "DelayedEffectResolved" then
        draw_cells(event.target_cells or {}, 0.36, 0.78, 1.0, 0.57)
    elseif event.x and event.y then
        draw_cells({{x=event.x,y=event.y}}, 1, 0.20, 0.22, 0.5)
    end
    if event.from_x and event.from_y then
        local x, y = Camera.tile_top_left(camera, event.from_x, event.from_y)
        love.graphics.setColor(1, 0.92, 0.52, 0.85)
        love.graphics.circle("line", x + TILE / 2, y + TILE / 2, 13)
    end
end

local function draw_preview()
    if not targeting or targeting.dx == nil then return end
    local preview = Game.preview(state, targeting.kind, targeting.dx, targeting.dy)
    for _, cell in ipairs(preview.cells) do
        if targeting.kind == "dash" then
            if cell.status == "open" then
                love.graphics.setColor(0.22, 0.85, 0.55, 0.55)
            elseif cell.status == "unreachable" then
                love.graphics.setColor(0.55, 0.55, 0.55, 0.45)
            else
                love.graphics.setColor(1.0, 0.27, 0.28, 0.65)
            end
        elseif targeting.kind == "sweeping_slash" then
            love.graphics.setColor(1.0, 0.56, 0.17, 0.65)
        else
            love.graphics.setColor(1.0, 0.86, 0.30, 0.65)
        end
        local screen_x, screen_y = Camera.tile_top_left(camera, cell.x, cell.y)
        love.graphics.rectangle("fill", screen_x + 2, screen_y + 2, TILE - 4, TILE - 4)
        love.graphics.setColor(1, 1, 1, 0.9)
        love.graphics.rectangle("line", screen_x + 2, screen_y + 2, TILE - 4, TILE - 4)
    end
end

local function draw_world()
    local map = Content.get_map(state.map_id)
    love.graphics.setScissor(VIEWPORT.x, VIEWPORT.y, VIEWPORT.width, VIEWPORT.height)
    love.graphics.setColor(0.09, 0.13, 0.11)
    love.graphics.rectangle("fill", VIEWPORT.x, VIEWPORT.y, VIEWPORT.width, VIEWPORT.height)
    for y = 1, map.height do
        for x = 1, map.width do
            local screen_x, screen_y = Camera.tile_top_left(camera, x, y)
            local wall = map.rows[y]:sub(x, x) == "#"
            if wall then
                love.graphics.setColor(0.30, 0.39, 0.34)
            else
                love.graphics.setColor(0.14, 0.20, 0.17)
            end
            love.graphics.rectangle("fill", screen_x, screen_y, TILE - 2, TILE - 2)
        end
    end
    if not Playback.busy(playback) then
        for _, strike in ipairs(Game.pending_strikes(state)) do
            draw_cells(strike.target_cells, 0.95, 0.26, 0.31, 0.38)
        end
    end
    draw_preview()
    draw_enemies()
    local position = Game.player(state).position
    local screen_x, screen_y = Camera.tile_top_left(camera, position.x, position.y)
    love.graphics.setColor(0.95, 0.78, 0.30)
    love.graphics.circle("fill", screen_x + TILE / 2 - 1, screen_y + TILE / 2 - 1, 11)
    draw_history_event(Playback.current(playback))
    if state.game_over and not Playback.busy(playback) then
        love.graphics.setColor(0, 0, 0, 0.65)
        love.graphics.rectangle("fill", VIEWPORT.x, VIEWPORT.y, VIEWPORT.width, VIEWPORT.height)
        love.graphics.setColor(1, 0.36, 0.33)
        love.graphics.printf("GAME OVER — press N to restart", VIEWPORT.x,
            VIEWPORT.y + VIEWPORT.height / 2 - 10, VIEWPORT.width, "center")
    end
    love.graphics.setScissor()
end

local function draw_panel()
    local position = Game.player(state).position
    love.graphics.setColor(0.93, 0.95, 0.88)
    love.graphics.print("ROEG — Tranche 03", PANEL_X, 26)
    local player = Game.player(state)
    love.graphics.print(("HP: %d / %d"):format(player.hp, player.max_hp), PANEL_X, 58)
    love.graphics.print(("Position: %d, %d"):format(position.x, position.y), PANEL_X, 82)
    love.graphics.print(("World time: %d"):format(state.clock), PANEL_X, 106)
    local action = state.last_action
    love.graphics.print("Last: " .. (action and labels[action.kind] or "none"), PANEL_X, 130)
    love.graphics.print("Cost: " .. (action and action.cost or "—"), PANEL_X, 154)
    love.graphics.printf(notice, PANEL_X, 181, 260)

    if not targeting then
        love.graphics.setColor(0.66, 0.86, 1)
        love.graphics.print("BOONS (B: developer loadout)", PANEL_X, 213)
        local line = 0
        for _, id in ipairs(BoonContent.order) do
            local stacks = player.boons and player.boons[id]
            if stacks then
                local counts = {}
                local short = {common="C", uncommon="U", rare="R", legendary="L"}
                for _, rarity in ipairs(BoonContent.rarities) do
                    if (stacks[rarity] or 0) > 0 then
                        counts[#counts + 1] = short[rarity] .. stacks[rarity]
                    end
                end
                if #counts > 0 then
                    local label = BoonContent.get(id).name .. " " .. table.concat(counts, " ")
                    love.graphics.print(label, PANEL_X, 230 + line * 12)
                    line = line + 1
                end
            end
        end
        if line == 0 then love.graphics.print("None equipped", PANEL_X, 230) end
    end

    local history = Playback.current(playback)
    if history then
        love.graphics.setColor(1, 0.67, 0.28)
        local playback_label = history.kind
        if history.boon_id then
            playback_label = history.kind .. ": " .. BoonContent.get(history.boon_id).name
        end
        love.graphics.printf("PLAYBACK: " .. playback_label, PANEL_X, 325, 265)
        love.graphics.print("Past event; input paused", PANEL_X, 349)
    else
        local strikes = Game.pending_strikes(state)
        if #strikes > 0 then
            love.graphics.setColor(1, 0.51, 0.43)
            love.graphics.print(("LIVE WINDUPS: %d"):format(#strikes), PANEL_X, 325)
            love.graphics.print(("Next impact in %d time"):format(strikes[1].due - state.clock), PANEL_X, 349)
        else
            local effects = Game.pending_boon_effects(state)
            if #effects > 0 then
                love.graphics.setColor(0.54, 0.85, 1)
                love.graphics.print(("LIVE ECHOES: %d"):format(#effects), PANEL_X, 325)
                love.graphics.print(("Next in %d time"):format(effects[1].due - state.clock), PANEL_X, 349)
            end
        end
    end

    if targeting then
        love.graphics.setColor(1, 0.72, 0.32)
        love.graphics.print("AIMING: " .. labels[targeting.kind], PANEL_X, 224)
        if targeting.dx ~= nil then
            local preview = Game.preview(state, targeting.kind, targeting.dx, targeting.dy)
            love.graphics.print(("Direction: %d, %d"):format(targeting.dx, targeting.dy), PANEL_X, 249)
            if targeting.kind == "dash" then
                local outcome = ("Path: %d/2 open"):format(preview.distance or 0)
                if preview.cells[1] and preview.cells[1].status ~= "open" then
                    outcome = outcome .. " (first " .. preview.cells[1].status .. ")"
                elseif preview.cells[2] and preview.cells[2].status ~= "open" then
                    outcome = outcome .. " (second " .. preview.cells[2].status .. ")"
                end
                love.graphics.print(outcome, PANEL_X, 274)
            else
                love.graphics.print(("Cells: %d"):format(#preview.cells), PANEL_X, 274)
            end
        else
            love.graphics.print("Select a direction", PANEL_X, 249)
        end
        love.graphics.print("Enter: commit  Esc: cancel", PANEL_X, 305)
    end

    love.graphics.setColor(0.80, 0.86, 0.80)
    love.graphics.print("CONTROLS (N reset, B boons)", PANEL_X, 383)
    love.graphics.print("Move: WASD / arrows", PANEL_X, 409)
    love.graphics.print("Wait: Space or .", PANEL_X, 433)
    love.graphics.print("F: Sword Strike", PANEL_X, 465)
    love.graphics.print("R: Sweeping Slash", PANEL_X, 489)
    love.graphics.print("X: Dash", PANEL_X, 513)
    love.graphics.print("Aim: WASD/arrows", PANEL_X, 545)
    love.graphics.print("Diagonals: Q E Z C", PANEL_X, 569)
    love.graphics.print("Enter confirm / Esc cancel", PANEL_X, 593)
end

function love.draw()
    if camera_state ~= state then reset_camera() end
    draw_world()
    draw_panel()
end
