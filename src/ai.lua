local Content = require("src.content")

local AI = {}
local directions = {{0,-1},{1,0},{0,1},{-1,0}} -- stable N, E, S, W

local function occupied(state, actor_id, x, y)
    for _, entity in ipairs(state.entities) do
        local position = entity.position
        if entity.id ~= actor_id and entity.blocks_movement and position
            and position.floor_id == state.active_floor_id and position.x == x and position.y == y then
            return true
        end
    end
    return false
end

local function free(state, actor_id, x, y)
    return Content.walkable(Content.get_map(state.map_id), x, y)
        and not occupied(state, actor_id, x, y)
end

local function lane(state, enemy, player, minimum, maximum)
    local a, b = enemy.position, player.position
    local dx, dy = b.x - a.x, b.y - a.y
    if dx ~= 0 and dy ~= 0 then return nil end
    local distance = math.abs(dx) + math.abs(dy)
    if distance < minimum or distance > maximum then return nil end
    local step_x = dx == 0 and 0 or dx / math.abs(dx)
    local step_y = dy == 0 and 0 or dy / math.abs(dy)
    local map = Content.get_map(state.map_id)
    local cells = {}
    for step = 1, distance do
        local x, y = a.x + step_x * step, a.y + step_y * step
        if not Content.projectile_passable(map, x, y) then return nil end
        cells[#cells + 1] = {x=x, y=y}
    end
    return cells
end

local function goal_key(map, x, y)
    return (y - 1) * map.width + x
end

local function next_step(state, actor, goals)
    local map = Content.get_map(state.map_id)
    local start = actor.position
    local queue = {{x=start.x, y=start.y}}
    local seen = {[goal_key(map, start.x, start.y)] = true}
    local head = 1
    while head <= #queue do
        local current = queue[head]
        head = head + 1
        for _, direction in ipairs(directions) do
            local x, y = current.x + direction[1], current.y + direction[2]
            local key = goal_key(map, x, y)
            if not seen[key] and free(state, actor.id, x, y) then
                seen[key] = true
                local first_dx = current.first_dx or direction[1]
                local first_dy = current.first_dy or direction[2]
                if goals[key] then return {kind="move", dx=first_dx, dy=first_dy} end
                queue[#queue + 1] = {x=x, y=y, first_dx=first_dx, first_dy=first_dy}
            end
        end
    end
    return nil
end

local function adjacent_goals(state, enemy, player, diagonal)
    local map = Content.get_map(state.map_id)
    local goals = {}
    local offsets = diagonal and {{-1,-1},{1,-1},{1,1},{-1,1}} or directions
    for _, offset in ipairs(offsets) do
        local x, y = player.position.x + offset[1], player.position.y + offset[2]
        if free(state, enemy.id, x, y) then goals[goal_key(map, x, y)] = true end
    end
    return goals
end

local function lane_goals(state, enemy, player, definition)
    local map = Content.get_map(state.map_id)
    local goals = {}
    for _, direction in ipairs(directions) do
        for distance = definition.min_range, definition.range do
            local x = player.position.x + direction[1] * distance
            local y = player.position.y + direction[2] * distance
            if free(state, enemy.id, x, y) then
                local mock = {position={x=x,y=y}}
                if lane(state, mock, player, definition.min_range, definition.range) then
                    goals[goal_key(map, x, y)] = true
                end
            end
        end
    end
    return goals
end

function AI.choose(state, enemy)
    local player = state.entities[1]
    for _, entity in ipairs(state.entities) do
        if entity.id == state.player_id then player = entity; break end
    end
    local definition = assert(Content.get_enemy(enemy.definition_id), "unknown AI content")
    local dx = player.position.x - enemy.position.x
    local dy = player.position.y - enemy.position.y
    local distance = math.abs(dx) + math.abs(dy)

    if definition.ai == "guard" then
        if distance == 1 then
            return {kind="windup", ability_id="core:ability/guard_strike",
                cells={{x=player.position.x,y=player.position.y}}, ranged=false}
        end
        return next_step(state, enemy, adjacent_goals(state, enemy, player, false)) or {kind="wait"}
    end
    if definition.ai == "spitter" then
        local cells = lane(state, enemy, player, definition.min_range, definition.range)
        if cells then
            return {kind="windup", ability_id="core:ability/thorn_lane",
                cells=cells, ranged=true}
        end
        return next_step(state, enemy, lane_goals(state, enemy, player, definition)) or {kind="wait"}
    end
    if definition.ai == "skitter" then
        if math.abs(dx) == 1 and math.abs(dy) == 1 then
            return {kind="windup", ability_id="core:ability/skitter_stab",
                cells={{x=player.position.x,y=player.position.y}}, ranged=false}
        end
        local flank = next_step(state, enemy, adjacent_goals(state, enemy, player, true))
        if flank then return flank end
        if math.max(math.abs(dx), math.abs(dy)) == 1 then
            return {kind="windup", ability_id="core:ability/skitter_stab",
                cells={{x=player.position.x,y=player.position.y}}, ranged=false}
        end
        return next_step(state, enemy, adjacent_goals(state, enemy, player, false)) or {kind="wait"}
    end
    error("unknown AI behavior")
end

return AI
