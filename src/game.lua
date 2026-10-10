local Content = require("src.content")
local RNG = require("src.rng")
local Scheduler = require("src.scheduler")
local Targeting = require("src.targeting")

local Game = {}
local SCHEMA = "roeg-run/0"
local COSTS = {
    move = 100,
    wait = 100,
    basic_attack = 100,
    sweeping_slash = 150,
    dash = 130,
}

local function copy(value)
    if type(value) ~= "table" then return value end
    local result = {}
    for key, child in pairs(value) do result[key] = copy(child) end
    return result
end

local function emit(state, output, kind, fields)
    local event = {
        id = state.next_event_id,
        kind = kind,
        time = state.clock,
        source_id = state.player_id,
        action_id = state.next_action_id,
    }
    state.next_event_id = state.next_event_id + 1
    for key, value in pairs(fields or {}) do event[key] = value end
    output[#output + 1] = event
    return event
end

function Game.new(seed, map_id)
    Content.validate_all()
    local map = assert(Content.get_map(map_id or "core:map/test_room"), "unknown map content ID")
    local state = {
        schema = SCHEMA,
        content_version = Content.version,
        map_id = map.id,
        active_floor_id = map.floor_id,
        clock = 0,
        player_id = 1,
        next_entity_id = 2,
        next_schedule_id = 1,
        next_event_id = 1,
        next_action_id = 1,
        rng = RNG.new(seed or 1),
        entities = {{
            id = 1,
            definition_id = "core:actor/adventurer",
            controller = "player",
            position = { floor_id = map.floor_id, x = map.start_x, y = map.start_y },
            blocks_movement = true,
        }},
        schedule = {},
        last_action = nil,
    }
    Scheduler.enqueue(state, { kind = "actor_ready", due = 0, actor_id = 1, is_player = true })
    return state
end

function Game.player(state)
    for _, entity in ipairs(state.entities) do
        if entity.id == state.player_id then return entity end
    end
    error("player entity missing")
end

local function blocker_at(state, floor_id, x, y)
    for _, entity in ipairs(state.entities) do
        local position = entity.position
        if entity.id ~= state.player_id and entity.blocks_movement and position
            and position.floor_id == floor_id and position.x == x and position.y == y then
            return true
        end
    end
    return false
end

function Game.preview(state, kind, dx, dy)
    if kind == "cancel" then return { valid = false, reason = "cancelled", cells = {} } end
    if not COSTS[kind] then return { valid = false, reason = "unknown action", cells = {} } end
    if kind == "wait" then return { valid = true, cells = {}, cost = COSTS.wait } end
    if not Targeting.direction_index(dx, dy) then
        return { valid = false, reason = "invalid direction", cells = {} }
    end
    if kind == "move" or kind == "dash" then
        if not Targeting.is_cardinal(dx, dy) then
            return { valid = false, reason = "movement is cardinal", cells = {} }
        end
        local position = Game.player(state).position
        local map = Content.get_map(state.map_id)
        local cells, distance, blocked = {}, 0, false
        local count = kind == "dash" and 2 or 1
        local reason
        for step = 1, count do
            local x, y = position.x + dx * step, position.y + dy * step
            local cell = { x = x, y = y }
            if blocked then
                cell.status = "unreachable"
            elseif not Content.walkable(map, x, y) then
                cell.status = "wall"
                blocked, reason = true, "wall"
            elseif blocker_at(state, position.floor_id, x, y) then
                cell.status = "occupied"
                blocked, reason = true, "occupied"
            else
                cell.status = "open"
                distance = distance + 1
            end
            cells[#cells + 1] = cell
        end
        return { valid = distance > 0, reason = distance == 0 and reason or nil,
            cost = COSTS[kind], cells = cells, distance = distance }
    end
    local position = Game.player(state).position
    return { valid = true, cost = COSTS[kind],
        cells = Targeting.cells(kind, position.x, position.y, dx, dy) }
end

local function advance_to_player(state, output)
    while true do
        local next_item = assert(Scheduler.peek(state), "scheduler has no ready actor")
        state.clock = next_item.due
        if next_item.kind == "actor_ready" then
            assert(next_item.actor_id == state.player_id, "enemy turns are outside Tranche 00")
            return
        end
        Scheduler.pop(state)
        emit(state, output, "ScheduledEffectResolved", { effect_id = next_item.effect_id })
    end
end

function Game.submit(state, intent)
    local ready = Scheduler.peek(state)
    assert(ready and ready.kind == "actor_ready" and ready.actor_id == state.player_id
        and ready.due == state.clock, "player is not ready")
    if type(intent) ~= "table" then return false, "invalid intent", {} end
    local preview = Game.preview(state, intent.kind, intent.dx, intent.dy)
    if not preview.valid then return false, preview.reason, {} end

    Scheduler.pop(state)
    local output = {}
    local player = Game.player(state)
    local position = player.position
    local action = { kind = intent.kind, time = state.clock, cost = preview.cost }
    if intent.kind == "move" or intent.kind == "dash" then
        local from_x, from_y = position.x, position.y
        position.x = from_x + intent.dx * preview.distance
        position.y = from_y + intent.dy * preview.distance
        action.dx, action.dy = intent.dx, intent.dy
        if intent.kind == "dash" then action.distance = preview.distance end
        local fields = {
            floor_id = position.floor_id, from_x = from_x, from_y = from_y,
            x = position.x, y = position.y,
        }
        if intent.kind == "dash" then
            fields.ability_id = "core:ability/dash"
            fields.path = {}
            for index = 1, preview.distance do
                fields.path[index] = { floor_id = position.floor_id,
                    x = preview.cells[index].x, y = preview.cells[index].y }
            end
        end
        emit(state, output, "Moved", fields)
    elseif intent.kind == "basic_attack" or intent.kind == "sweeping_slash" then
        action.dx, action.dy = intent.dx, intent.dy
        local target_cells = {}
        for index, cell in ipairs(preview.cells) do
            target_cells[index] = { floor_id = position.floor_id, x = cell.x, y = cell.y }
        end
        local fields = {
            floor_id = position.floor_id, from_x = position.x, from_y = position.y,
            ability_id = intent.kind == "basic_attack" and "core:ability/basic_attack"
                or "core:ability/sweeping_slash",
            target_cells = target_cells,
        }
        if intent.kind == "basic_attack" then
            fields.target_x, fields.target_y = target_cells[1].x, target_cells[1].y
        end
        emit(state, output, "AttackPerformed", fields)
    else
        emit(state, output, "Waited", { floor_id = position.floor_id, x = position.x, y = position.y })
    end
    state.last_action = action
    state.next_action_id = state.next_action_id + 1
    Scheduler.enqueue(state, {
        kind = "actor_ready", due = state.clock + preview.cost,
        actor_id = state.player_id, is_player = true,
    })
    advance_to_player(state, output)
    return true, nil, output
end

function Game.snapshot(state)
    -- Snapshots contain only plain data. The map definition is referenced by stable ID.
    return copy(state)
end

function Game.restore(snapshot)
    assert(type(snapshot) == "table" and snapshot.schema == SCHEMA, "incompatible run schema")
    assert(snapshot.content_version == Content.version, "incompatible content version")
    Content.validate_all()
    local map = assert(Content.get_map(snapshot.map_id), "unknown map content ID")
    assert(snapshot.active_floor_id == map.floor_id, "incompatible floor ID")
    assert(type(snapshot.clock) == "number" and snapshot.clock % 1 == 0 and snapshot.clock >= 0,
        "invalid world clock")
    assert(type(snapshot.entities) == "table" and type(snapshot.schedule) == "table", "invalid run state")
    assert(type(snapshot.rng) == "table", "missing RNG state")
    local state = copy(snapshot)
    RNG.new(state.rng.state)
    local player = Game.player(state)
    assert(player.position.floor_id == map.floor_id, "player on wrong floor")
    assert(Content.walkable(map, player.position.x, player.position.y), "player outside walkable map")
    Scheduler.sort(state)
    local ready = Scheduler.peek(state)
    assert(ready and ready.kind == "actor_ready" and ready.actor_id == state.player_id
        and ready.due == state.clock, "snapshot is not at a player decision boundary")
    return state
end

return Game
