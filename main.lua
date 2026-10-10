local Camera = require("src.camera")
local Content = require("src.content")
local Game = require("src.game")

local TILE = 32
local VIEWPORT = { x = 16, y = 16, width = 704, height = 608 }
local PANEL_X = 742

local state
local camera
local camera_state
local targeting -- nil, or a presentation-only { kind, dx, dy }.
local notice = "Ready"

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
    state = Game.new(1, "core:map/scrolling_room")
    reset_camera()
    love.graphics.setBackgroundColor(0.055, 0.075, 0.075)
end

function love.update(dt)
    if camera_state ~= state then reset_camera() end
    local position = Game.player(state).position
    Camera.update(camera, dt, position.x, position.y)
end

local function submit(intent)
    local committed, reason = Game.submit(state, intent)
    if committed then
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
    draw_preview()
    local position = Game.player(state).position
    local screen_x, screen_y = Camera.tile_top_left(camera, position.x, position.y)
    love.graphics.setColor(0.95, 0.78, 0.30)
    love.graphics.circle("fill", screen_x + TILE / 2 - 1, screen_y + TILE / 2 - 1, 11)
    love.graphics.setScissor()
end

local function draw_panel()
    local position = Game.player(state).position
    love.graphics.setColor(0.93, 0.95, 0.88)
    love.graphics.print("ROEG — Tranche 01", PANEL_X, 26)
    love.graphics.print(("Position: %d, %d"):format(position.x, position.y), PANEL_X, 66)
    love.graphics.print(("World time: %d"):format(state.clock), PANEL_X, 90)
    local action = state.last_action
    love.graphics.print("Last: " .. (action and labels[action.kind] or "none"), PANEL_X, 114)
    love.graphics.print("Cost: " .. (action and action.cost or "—"), PANEL_X, 138)
    love.graphics.printf(notice, PANEL_X, 174, 260)

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
    love.graphics.print("CONTROLS", PANEL_X, 383)
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
