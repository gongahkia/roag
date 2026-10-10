local AI = require("src.ai")
local Content = require("src.content")
local RNG = require("src.rng")
local Scheduler = require("src.scheduler")
local Targeting = require("src.targeting")

local Game = {}
local SCHEMA = "roeg-run/2"
local COSTS = {move=100, wait=100, basic_attack=100, sweeping_slash=150, dash=130}

local function copy(value)
    if type(value) ~= "table" then return value end
    local result = {}
    for key, child in pairs(value) do result[key] = copy(child) end
    return result
end

local function emit(state, output, kind, fields, source_id, action_id)
    local event = {
        id = state.next_event_id, kind = kind, time = state.clock,
        source_id = source_id, action_id = action_id, chain_id = action_id,
    }
    state.next_event_id = state.next_event_id + 1
    for key, value in pairs(fields or {}) do event[key] = copy(value) end
    output[#output + 1] = event
    return event
end

function Game.effective_cost(base, speed)
    assert(type(base) == "number" and base % 1 == 0 and base > 0 and base <= 1000000,
        "invalid base cost")
    assert(type(speed) == "number" and speed % 1 == 0 and speed > 0 and speed <= 1000000,
        "invalid action speed")
    return math.max(1, math.floor((base * 200 + speed) / (speed * 2)))
end

function Game.entity(state, id)
    for _, entity in ipairs(state.entities) do
        if entity.id == id then return entity end
    end
    return nil
end

function Game.player(state)
    return assert(Game.entity(state, state.player_id), "player entity missing")
end

local function occupant(state, floor_id, x, y, excluded_id, combat_only)
    for _, entity in ipairs(state.entities) do
        local position = entity.position
        if entity.id ~= excluded_id and position and position.floor_id == floor_id
            and position.x == x and position.y == y
            and (not combat_only or (entity.hp ~= nil and entity.hp > 0))
            and (combat_only or entity.blocks_movement) then
            return entity
        end
    end
    return nil
end

function Game.spawn_enemy(state, definition_id, x, y, due)
    local definition = assert(Content.get_enemy(definition_id), "unknown enemy definition")
    local map = assert(Content.get_map(state.map_id), "unknown map")
    assert(Content.walkable(map, x, y), "enemy spawn is blocked")
    assert(not occupant(state, map.floor_id, x, y, nil, false), "enemy spawn is occupied")
    local id = state.next_entity_id
    state.next_entity_id = id + 1
    local enemy = {
        id = id, definition_id = definition_id, controller = "ai", ai_id = definition.ai,
        ai_memory = {last_dx=0, last_dy=0}, faction = "enemy",
        position = {floor_id=map.floor_id, x=x, y=y}, blocks_movement = true,
        hp = definition.hp, max_hp = definition.hp, damage = definition.damage,
        defense = definition.defense, speed = definition.speed, stunned_until = 0,
    }
    state.entities[#state.entities + 1] = enemy
    Scheduler.enqueue(state, {kind="actor_ready", due=due or state.clock,
        actor_id=id, is_player=false})
    return enemy
end

function Game.new(seed, map_id, options)
    Content.validate_all()
    local map = assert(Content.get_map(map_id or "core:map/test_room"), "unknown map content ID")
    local definition = Content.get_adventurer()
    local state = {
        schema=SCHEMA, content_version=Content.version, map_id=map.id,
        active_floor_id=map.floor_id, clock=0, player_id=1, next_entity_id=2,
        next_schedule_id=1, next_event_id=1, next_action_id=1,
        rng=RNG.new(seed or 1), entities={{
            id=1, definition_id="core:actor/adventurer", controller="player", faction="player",
            position={floor_id=map.floor_id, x=map.start_x, y=map.start_y},
            blocks_movement=true, hp=definition.hp, max_hp=definition.hp,
            damage=definition.damage, slash_damage=definition.slash_damage,
            defense=definition.defense, speed=definition.speed,
        }},
        schedule={}, last_action=nil, game_over=false,
    }
    Scheduler.enqueue(state, {kind="actor_ready", due=0, actor_id=1, is_player=true})
    if options and options.demo_enemies then
        for _, spawn in ipairs(Content.demo_spawns(map.id)) do
            Game.spawn_enemy(state, spawn.definition_id, spawn.x, spawn.y)
        end
    end
    return state
end

function Game.preview(state, kind, dx, dy)
    if kind == "cancel" then return {valid=false, reason="cancelled", cells={}} end
    if not COSTS[kind] then return {valid=false, reason="unknown action", cells={}} end
    local player = Game.player(state)
    local cost = Game.effective_cost(COSTS[kind], player.speed)
    if kind == "wait" then return {valid=true, cells={}, cost=cost} end
    if not Targeting.direction_index(dx, dy) then
        return {valid=false, reason="invalid direction", cells={}}
    end
    if kind == "move" or kind == "dash" then
        if not Targeting.is_cardinal(dx, dy) then
            return {valid=false, reason="movement is cardinal", cells={}}
        end
        local position = player.position
        local map = Content.get_map(state.map_id)
        local cells, distance, blocked = {}, 0, false
        local count = kind == "dash" and 2 or 1
        local reason, bump
        for step = 1, count do
            local x, y = position.x + dx * step, position.y + dy * step
            local cell = {x=x, y=y}
            if blocked then
                cell.status = "unreachable"
            elseif not Content.walkable(map, x, y) then
                cell.status = "wall"
                blocked, reason = true, "wall"
            else
                local entity = occupant(state, position.floor_id, x, y, player.id, false)
                if entity then
                    if kind == "move" and entity.controller == "ai" and entity.faction ~= player.faction
                        and entity.hp and entity.hp > 0 then
                        cell.status, bump = "enemy", true
                    else
                        cell.status, reason = "occupied", "occupied"
                    end
                    blocked = true
                else
                    cell.status = "open"
                    distance = distance + 1
                end
            end
            cells[#cells + 1] = cell
        end
        return {valid=distance > 0 or bump == true,
            reason=(distance == 0 and not bump) and reason or nil,
            cost=cost, cells=cells, distance=distance, bump=bump}
    end
    local position = player.position
    return {valid=true, cost=cost,
        cells=Targeting.cells(kind, position.x, position.y, dx, dy)}
end

local function cancel_windup(state, target, output, reason, source_id, action_id)
    local pending = target.pending_attack
    if not pending then return false end
    Scheduler.remove_for_actor(state, target.id, "impact")
    target.pending_attack = nil
    emit(state, output, "WindupCancelled", {
        target_id=target.id, windup_id=pending.schedule_id, reason=reason,
        target_cells=pending.target_cells,
    }, source_id, action_id)
    return true
end

local function remove_dead(state, target, output, source_id, action_id)
    if target.id == state.player_id then
        state.game_over = true
        Scheduler.remove_for_actor(state, target.id)
        return
    end
    cancel_windup(state, target, output, "death", source_id, action_id)
    Scheduler.remove_for_actor(state, target.id)
    for index, entity in ipairs(state.entities) do
        if entity.id == target.id then table.remove(state.entities, index); break end
    end
end

local function damage_batch(state, output, source, action_id, cells, raw_damage, ranged)
    local outcomes = {}
    local map = Content.get_map(state.map_id)
    for _, cell in ipairs(cells) do
        if ranged and not Content.projectile_passable(map, cell.x, cell.y) then break end
        local target = occupant(state, cell.floor_id, cell.x, cell.y, source.id, true)
        if target then
            local after_defense = math.max(0, raw_damage - (target.defense or 0))
            local loss = math.min(target.hp, after_defense)
            outcomes[#outcomes + 1] = {
                target_id=target.id, target_definition_id=target.definition_id,
                floor_id=cell.floor_id, x=cell.x, y=cell.y,
                attempted_damage=raw_damage, after_defense=after_defense,
                hp_before=target.hp, hp_after=target.hp - loss, actual_loss=loss,
            }
            if ranged then break end
        end
    end
    -- Commit the entire batch before exposing any dependent damage/death record.
    for _, outcome in ipairs(outcomes) do
        Game.entity(state, outcome.target_id).hp = outcome.hp_after
    end
    if #outcomes > 0 then
        emit(state, output, "DamageBatchResolved", {outcomes=outcomes}, source.id, action_id)
    end
    for _, outcome in ipairs(outcomes) do
        emit(state, output, "DamageAttempted", outcome, source.id, action_id)
        if outcome.actual_loss > 0 then
            emit(state, output, "DamageTaken", outcome, source.id, action_id)
        else
            emit(state, output, "DamageBlocked", outcome, source.id, action_id)
        end
        if outcome.hp_after == 0 then
            emit(state, output, "Died", outcome, source.id, action_id)
            remove_dead(state, Game.entity(state, outcome.target_id), output, source.id, action_id)
        end
    end
end

local function attack(state, output, source, action_id, ability_id, cells, raw_damage, ranged, extra)
    local position = source.position
    local fields = {
        floor_id=position.floor_id, from_x=position.x, from_y=position.y,
        ability_id=ability_id, target_cells=cells,
    }
    if #cells == 1 and ability_id == "core:ability/basic_attack" then
        fields.target_x, fields.target_y = cells[1].x, cells[1].y
    end
    if extra then for key, value in pairs(extra) do fields[key] = value end end
    emit(state, output, "AttackPerformed", fields, source.id, action_id)
    damage_batch(state, output, source, action_id, cells, raw_damage, ranged)
end

local function enemy_turn(state, item, output)
    local enemy = Game.entity(state, item.actor_id)
    if not enemy or enemy.hp <= 0 then return end
    if enemy.pending_attack then return end -- no overlapping attacks
    if (enemy.stunned_until or 0) > state.clock then
        Scheduler.enqueue(state, {kind="actor_ready", due=enemy.stunned_until,
            actor_id=enemy.id, is_player=false})
        return
    end
    local definition = Content.get_enemy(enemy.definition_id)
    local intent = AI.choose(state, enemy) -- read-only decision
    local action_id = state.next_action_id
    state.next_action_id = action_id + 1
    local position = enemy.position
    if intent.kind == "windup" then
        local due = state.clock + Game.effective_cost(definition.windup, enemy.speed)
        local scheduled = Scheduler.enqueue(state, {kind="impact", due=due,
            actor_id=enemy.id, attack_id=action_id})
        local cells = {}
        for index, cell in ipairs(intent.cells) do
            cells[index] = {floor_id=position.floor_id, x=cell.x, y=cell.y}
        end
        enemy.pending_attack = {
            schedule_id=scheduled.id, sequence=scheduled.seq, action_id=action_id,
            source_id=enemy.id, floor_id=position.floor_id,
            source_x=position.x, source_y=position.y,
            ability_id=intent.ability_id, target_cells=cells, ranged=intent.ranged,
            damage=enemy.damage, due=due,
            committed_time=state.clock,
        }
        emit(state, output, "WindupStarted", {
            windup_id=scheduled.id, due=due, ability_id=intent.ability_id,
            floor_id=position.floor_id, from_x=position.x, from_y=position.y,
            target_cells=cells,
        }, enemy.id, action_id)
        return
    end
    if intent.kind == "move" then
        local x, y = position.x + intent.dx, position.y + intent.dy
        local map = Content.get_map(state.map_id)
        if Content.walkable(map, x, y)
            and not occupant(state, position.floor_id, x, y, enemy.id, false) then
            local from_x, from_y = position.x, position.y
            position.x, position.y = x, y
            enemy.ai_memory.last_dx, enemy.ai_memory.last_dy = intent.dx, intent.dy
            emit(state, output, "Moved", {
                floor_id=position.floor_id, from_x=from_x, from_y=from_y, x=x, y=y,
            }, enemy.id, action_id)
        else
            emit(state, output, "Waited", {floor_id=position.floor_id,
                x=position.x, y=position.y}, enemy.id, action_id)
        end
    else
        emit(state, output, "Waited", {floor_id=position.floor_id,
            x=position.x, y=position.y}, enemy.id, action_id)
    end
    Scheduler.enqueue(state, {kind="actor_ready",
        due=state.clock + Game.effective_cost(definition.move_cost, enemy.speed),
        actor_id=enemy.id, is_player=false})
end

local function resolve_impact(state, item, output)
    local enemy = Game.entity(state, item.actor_id)
    if not enemy or enemy.hp <= 0 then return end
    local pending = enemy.pending_attack
    if not pending or pending.schedule_id ~= item.id or pending.action_id ~= item.attack_id then return end
    enemy.pending_attack = nil
    attack(state, output, enemy, pending.action_id, pending.ability_id,
        pending.target_cells, pending.damage, pending.ranged,
        {windup_id=pending.schedule_id, committed_time=pending.committed_time})
    Scheduler.enqueue(state, {kind="actor_ready", actor_id=enemy.id,
        due=state.clock + Game.effective_cost(Content.get_enemy(enemy.definition_id).recovery, enemy.speed),
        is_player=false})
end

local function advance_to_player(state, output)
    local processed = 0
    while not state.game_over do
        processed = processed + 1
        assert(processed <= 100000, "scheduler failed to reach player decision")
        local item = assert(Scheduler.peek(state), "scheduler has no ready actor")
        state.clock = item.due
        if item.kind == "actor_ready" and item.actor_id == state.player_id then return end
        Scheduler.pop(state)
        if item.kind == "actor_ready" then
            enemy_turn(state, item, output)
        elseif item.kind == "impact" then
            resolve_impact(state, item, output)
        else
            emit(state, output, "ScheduledEffectResolved", {effect_id=item.effect_id},
                item.actor_id, item.attack_id)
        end
    end
end

function Game.submit(state, intent)
    if state.game_over then return false, "game over", {} end
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
    local action_id = state.next_action_id
    state.next_action_id = action_id + 1
    local kind = preview.bump and "basic_attack" or intent.kind
    local action = {kind=kind, time=state.clock, cost=preview.cost}
    if preview.bump then action.bump = true end
    if kind == "move" or kind == "dash" then
        local from_x, from_y = position.x, position.y
        position.x = from_x + intent.dx * preview.distance
        position.y = from_y + intent.dy * preview.distance
        action.dx, action.dy = intent.dx, intent.dy
        if kind == "dash" then action.distance = preview.distance end
        local fields = {floor_id=position.floor_id, from_x=from_x, from_y=from_y,
            x=position.x, y=position.y}
        if kind == "dash" then
            fields.ability_id = "core:ability/dash"
            fields.path = {}
            for index = 1, preview.distance do
                fields.path[index] = {floor_id=position.floor_id,
                    x=preview.cells[index].x, y=preview.cells[index].y}
            end
        end
        emit(state, output, "Moved", fields, player.id, action_id)
    elseif kind == "basic_attack" or kind == "sweeping_slash" then
        action.dx, action.dy = intent.dx, intent.dy
        local cells = {}
        local target = preview.bump and Targeting.cells("basic_attack",
            position.x, position.y, intent.dx, intent.dy) or preview.cells
        for index, cell in ipairs(target) do
            cells[index] = {floor_id=position.floor_id, x=cell.x, y=cell.y}
        end
        attack(state, output, player, action_id,
            kind == "basic_attack" and "core:ability/basic_attack" or "core:ability/sweeping_slash",
            cells, kind == "basic_attack" and player.damage or player.slash_damage,
            false, preview.bump and {bump=true} or nil)
    else
        emit(state, output, "Waited", {floor_id=position.floor_id,
            x=position.x, y=position.y}, player.id, action_id)
    end
    state.last_action = action
    Scheduler.enqueue(state, {kind="actor_ready", due=state.clock + preview.cost,
        actor_id=player.id, is_player=true})
    advance_to_player(state, output)
    return true, nil, output
end

function Game.stun(state, target_id, duration, source_id, action_id)
    assert(type(duration) == "number" and duration % 1 == 0 and duration > 0,
        "stun duration must be positive integer time")
    local target = Game.entity(state, target_id)
    if not target or target.controller ~= "ai" or target.hp <= 0 then
        return false, "invalid stun target", {}
    end
    local output = {}
    cancel_windup(state, target, output, "stun", source_id, action_id)
    target.stunned_until = math.max(target.stunned_until or 0, state.clock + duration)
    Scheduler.remove_for_actor(state, target.id, "actor_ready")
    Scheduler.enqueue(state, {kind="actor_ready", due=target.stunned_until,
        actor_id=target.id, is_player=false})
    emit(state, output, "Stunned", {target_id=target.id,
        until_time=target.stunned_until}, source_id, action_id)
    return true, nil, output
end

function Game.displace(state, target_id, x, y, source_id, action_id)
    local target = Game.entity(state, target_id)
    if not target or target.controller ~= "ai" or target.hp <= 0 then
        return false, "invalid displacement target", {}
    end
    if target.position.x == x and target.position.y == y then
        return false, "same cell", {}
    end
    local map = Content.get_map(state.map_id)
    if not Content.walkable(map, x, y) then return false, "wall", {} end
    if occupant(state, target.position.floor_id, x, y, target.id, false) then
        return false, "occupied", {}
    end
    local output = {}
    local interrupted = cancel_windup(state, target, output, "displacement", source_id, action_id)
    local from_x, from_y = target.position.x, target.position.y
    target.position.x, target.position.y = x, y
    emit(state, output, "Displaced", {target_id=target.id,
        floor_id=target.position.floor_id, from_x=from_x, from_y=from_y,
        x=x, y=y}, source_id, action_id)
    if interrupted then
        local definition = Content.get_enemy(target.definition_id)
        Scheduler.enqueue(state, {kind="actor_ready", actor_id=target.id, is_player=false,
            due=state.clock + Game.effective_cost(definition.recovery, target.speed)})
    end
    return true, nil, output
end

function Game.pending_strikes(state)
    local result = {}
    for _, entity in ipairs(state.entities) do
        if entity.pending_attack then result[#result + 1] = copy(entity.pending_attack) end
    end
    table.sort(result, function(a, b)
        if a.due ~= b.due then return a.due < b.due end
        return a.sequence < b.sequence
    end)
    return result
end

function Game.snapshot(state)
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
    local occupied = {}
    for _, entity in ipairs(state.entities) do
        if entity.blocks_movement then
            assert(Content.walkable(map, entity.position.x, entity.position.y), "entity outside walkable map")
            local key = entity.position.x .. "," .. entity.position.y
            assert(not occupied[key], "overlapping blocking entities")
            occupied[key] = true
        end
        if entity.pending_attack then
            local found = false
            for _, item in ipairs(state.schedule) do
                if item.id == entity.pending_attack.schedule_id and item.kind == "impact"
                    and item.actor_id == entity.id
                    and item.attack_id == entity.pending_attack.action_id
                    and item.seq == entity.pending_attack.sequence
                    and item.due == entity.pending_attack.due then found = true; break end
            end
            assert(found, "pending windup has no matching impact")
        end
    end
    Scheduler.sort(state)
    if not state.game_over then
        local ready = Scheduler.peek(state)
        assert(ready and ready.kind == "actor_ready" and ready.actor_id == state.player_id
            and ready.due == state.clock, "snapshot is not at a player decision boundary")
    end
    return state
end

return Game
