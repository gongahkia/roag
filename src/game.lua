local AI = require("src.ai")
local Boons = require("src.boons")
local BoonContent = require("src.boon_content")
local Content = require("src.content")
local RNG = require("src.rng")
local Rewards = require("src.rewards")
local Scheduler = require("src.scheduler")
local Targeting = require("src.targeting")

local Game = {}
local SCHEMA = "roeg-run/4"
local COSTS = {move=100, wait=100, basic_attack=100, sweeping_slash=150,
    dash=130, claim_chest=100}
local contexts = setmetatable({}, {__mode="k"}) -- resolver context never enters snapshots

local function copy(value)
    if type(value) ~= "table" then return value end
    local result = {}
    for key, child in pairs(value) do result[key] = copy(child) end
    return result
end

local function emit(state, output, kind, fields, source_id, action_id)
    local context = contexts[output]
    local cause = context and context.cause
    local source
    for _, actor in ipairs(state.entities) do
        if actor.id == source_id then source = actor; break end
    end
    local event = {
        id = state.next_event_id, kind = kind, time = state.clock,
        source_id = source_id, action_id = action_id,
        root_action_id = cause and cause.root_action_id or action_id,
        chain_id = cause and cause.chain_id or action_id,
        parent_id = cause and cause.parent_id or nil,
        ancestry = cause and copy(cause.ancestry) or {},
        proc_coefficient = cause and cause.proc_coefficient or 1,
        source_faction = source and source.faction or (cause and cause.source_faction),
        source_position = source and copy(source.position) or (cause and copy(cause.source_position)),
        source_tags = source and {source.definition_id, source.faction, source.ai_id} or nil,
    }
    state.next_event_id = state.next_event_id + 1
    for key, value in pairs(fields or {}) do event[key] = copy(value) end
    output[#output + 1] = event
    if context then context.events[#context.events + 1] = event end
    return event
end

function Game.effective_cost(base, speed)
    assert(type(base) == "number" and base % 1 == 0 and base > 0 and base <= 1000000,
        "invalid base cost")
    assert(type(speed) == "number" and speed % 1 == 0 and speed > 0
        and speed <= 4503599627370496,
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
        ai_memory = {last_dx=0, last_dy=0}, faction = "enemy", boons={},
        position = {floor_id=map.floor_id, x=x, y=y}, blocks_movement = true,
        hp = definition.hp, max_hp = definition.hp, damage = definition.damage,
        defense = definition.defense, speed = definition.speed, stunned_until = 0,
    }
    state.entities[#state.entities + 1] = enemy
    Scheduler.enqueue(state, {kind="actor_ready", due=due or state.clock,
        actor_id=id, is_player=false})
    return enemy
end

function Game.spawn_chest(state, definition_id, x, y)
    local definition = assert(Content.get_chest(definition_id), "unknown chest definition")
    local map = assert(Content.get_map(state.map_id), "unknown map")
    assert(Content.walkable(map, x, y), "chest spawn is blocked")
    assert(not occupant(state, map.floor_id, x, y, nil, false), "chest spawn is occupied")
    local chest = {
        id=state.next_entity_id, definition_id=definition_id, controller="chest",
        faction="neutral", position={floor_id=map.floor_id, x=x, y=y},
        blocks_movement=true, claimed=false, price=definition.price,
        offers=Rewards.make_offers(state, definition),
    }
    state.next_entity_id = chest.id + 1
    state.entities[#state.entities + 1] = chest
    return chest
end

function Game.new(seed, map_id, options)
    Content.validate_all()
    BoonContent.validate()
    local map = assert(Content.get_map(map_id or "core:map/test_room"), "unknown map content ID")
    local definition = Content.get_adventurer()
    local state = {
        schema=SCHEMA, content_version=Content.version,
        boon_content_version=BoonContent.version, map_id=map.id,
        active_floor_id=map.floor_id, clock=0, player_id=1, next_entity_id=2,
        next_schedule_id=1, next_event_id=1, next_action_id=1, next_activation_id=1,
        chain_history={}, delayed_effects={},
        progression={level=1, xp_total=0}, currency=0,
        next_drop_id=1, drops={}, rewarded_kills={},
        rng=RNG.new(seed or 1), entities={{
            id=1, definition_id="core:actor/adventurer", controller="player", faction="player",
            position={floor_id=map.floor_id, x=map.start_x, y=map.start_y},
            blocks_movement=true, hp=definition.hp, max_hp=definition.hp,
            damage=definition.damage, slash_damage=definition.slash_damage,
            defense=definition.defense, speed=definition.speed, boons={},
            crit_chance_ppm=definition.crit_chance_ppm,
            crit_damage_percent=definition.crit_damage_percent,
        }},
        schedule={}, last_action=nil, game_over=false,
    }
    Scheduler.enqueue(state, {kind="actor_ready", due=0, actor_id=1, is_player=true})
    if options and options.demo_enemies then
        for _, spawn in ipairs(Content.demo_spawns(map.id)) do
            Game.spawn_enemy(state, spawn.definition_id, spawn.x, spawn.y)
        end
    end
    if options and options.demo_chests then
        for _, spawn in ipairs(Content.demo_chests(map.id)) do
            Game.spawn_chest(state, spawn.definition_id, spawn.x, spawn.y)
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
        local reason, bump, chest_id
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
                    if kind == "move" and entity.controller == "chest" and not entity.claimed then
                        cell.status, reason, chest_id = "chest", "chest", entity.id
                    elseif kind == "move" and entity.controller == "ai" and entity.faction ~= player.faction
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
            cost=cost, cells=cells, distance=distance, bump=bump, chest_id=chest_id}
    end
    local position = player.position
    local cells = Targeting.cells(kind, position.x, position.y, dx, dy)
    if kind == "sweeping_slash" then
        cells = Boons.ability(state, player, "core:ability/sweeping_slash",
            cells, player.slash_damage, dx, dy)
    end
    return {valid=true, cost=cost, cells=cells}
end

function Game.set_boon_stacks(state, owner_id, boon_id, rarity, count)
    local owner = assert(Game.entity(state, owner_id), "unknown boon owner")
    Boons.set_stacks(owner, boon_id, rarity, count)
end

function Game.chest_options(state, chest_id)
    local chest = Game.entity(state, chest_id)
    if not chest or chest.controller ~= "chest" then
        return nil, "unknown chest"
    end
    return Rewards.chest_options(state, chest)
end

function Game.next_level_xp(state)
    return Rewards.next_threshold(state.progression.level)
end

function Game.effective_ability(state, kind, dx, dy)
    assert(kind == "basic_attack" or kind == "sweeping_slash", "unknown ability")
    local owner = Game.player(state)
    local base = Targeting.cells(kind, owner.position.x, owner.position.y, dx, dy)
    assert(base, "invalid ability direction")
    return Boons.ability(state, owner,
        kind == "basic_attack" and "core:ability/basic_attack" or "core:ability/sweeping_slash",
        base, kind == "basic_attack" and owner.damage or owner.slash_damage, dx, dy)
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

local function damage_batch(state, output, source, action_id, cells, raw_damage, ranged,
    damage_type, critical)
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
                target_faction=target.faction, target_tags={target.definition_id, target.faction},
                floor_id=cell.floor_id, x=cell.x, y=cell.y,
                damage_type=damage_type or "physical",
                critical=critical or false,
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
            emit(state, output, "EntityKilled", outcome, source.id, action_id)
            remove_dead(state, Game.entity(state, outcome.target_id), output, source.id, action_id)
        end
    end
    for _, outcome in ipairs(outcomes) do
        emit(state, output, "HitConfirmed", outcome, source.id, action_id)
    end
end

local function effect_cause(activation, effect)
    return {
        chain_id=activation.chain_id, root_action_id=activation.root_action_id,
        parent_id=activation.id, ancestry=activation.ancestry,
        proc_coefficient=effect.proc_coefficient or 1,
        source_faction=activation.source_faction,
        source_position=activation.source_position,
    }
end

local function effect_cells(state, activation, effect)
    local event = activation.event
    if effect.kind == "echo_impact" then return event.target_cells or {} end
    if effect.kind == "damage_source" then
        local target = Game.entity(state, event.source_id)
        if not target or target.hp <= 0 then return {} end
        return {{floor_id=target.position.floor_id, x=target.position.x, y=target.position.y}}
    end
    local x, y = event.x or event.from_x, event.y or event.from_y
    local floor_id = event.floor_id or (event.source_position and event.source_position.floor_id)
    if not x or not y then
        x, y = event.source_position.x, event.source_position.y
    end
    if effect.kind == "damage_nearest" then
        local owner = Game.entity(state, activation.owner_id)
        if not owner then return {} end
        local best, best_distance
        for _, target in ipairs(state.entities) do
            if target.id ~= owner.id and target.id ~= event.target_id
                and target.hp and target.hp > 0 and target.faction ~= owner.faction
                and target.position.floor_id == floor_id then
                local d = math.abs(target.position.x - x) + math.abs(target.position.y - y)
                if d <= effect.radius and (not best or d < best_distance
                    or (d == best_distance and target.id < best.id)) then
                    best, best_distance = target, d
                end
            end
        end
        if not best then return {} end
        return {{floor_id=floor_id, x=best.position.x, y=best.position.y}}
    end
    if effect.kind == "area" then
        local cells = {}
        for dy = -effect.radius, effect.radius do
            for dx = -effect.radius, effect.radius do
                if math.max(math.abs(dx), math.abs(dy)) <= effect.radius then
                    cells[#cells + 1] = {floor_id=floor_id, x=x + dx, y=y + dy}
                end
            end
        end
        return cells
    end
    return {}
end

local function execute_effect(state, output, activation, effect)
    local context = assert(contexts[output], "missing boon resolution context")
    local owner = Game.entity(state, activation.owner_id)
    local source = owner or {id=activation.owner_id, position=activation.source_position}
    context.cause = effect_cause(activation, effect)
    if effect.kind == "echo" then
        local due = state.clock + effect.delay
        local scheduled = Scheduler.enqueue(state, {kind="effect", due=due,
            actor_id=source.id, attack_id=activation.root_action_id})
        scheduled.effect_id = scheduled.id
        state.delayed_effects[scheduled.id] = {
            activation=copy(activation),
            effect={kind="echo_impact", damage_type=effect.damage_type,
                proc_coefficient=effect.proc_coefficient},
        }
        emit(state, output, "DelayedEffectScheduled", {
            boon_id=activation.boon_id, activation_id=activation.id,
            due=due, effect_id=scheduled.id, target_cells=activation.event.target_cells,
        }, source.id, activation.root_action_id)
    else
        local cells = effect_cells(state, activation, effect)
        emit(state, output, "EffectApplied", {
            boon_id=activation.boon_id, activation_id=activation.id,
            damage_type=effect.damage_type, target_cells=cells,
        }, source.id, activation.root_action_id)
        if #cells > 0 then
            damage_batch(state, output, source, activation.root_action_id, cells,
                activation.power, false, effect.damage_type)
        end
    end
    context.cause = nil
end

local function drain_effects(state, output)
    local context = assert(contexts[output], "missing boon resolution context")
    local steps = 0
    while context.event_head <= #context.events or context.effect_head <= #context.effects do
        steps = steps + 1
        assert(steps <= 1000000, "boon effect resolution exceeded diagnostic limit")
        if context.event_head <= #context.events then
            local event = context.events[context.event_head]
            context.event_head = context.event_head + 1
            Boons.on_event(state, event,
                function(activation, effect)
                    context.effects[#context.effects + 1] = {activation=activation, effect=effect}
                end,
                function(activation)
                    context.cause = effect_cause(activation, {})
                    emit(state, output, "BoonActivated", {
                        boon_id=activation.boon_id, activation_id=activation.id,
                        trigger_event_id=event.id, power=activation.power,
                        x=event.x, y=event.y, floor_id=event.floor_id,
                        pair_target_id=activation.pair_target_id,
                    }, activation.owner_id, activation.root_action_id)
                    context.cause = nil
                end)
            context.cause = {
                chain_id=event.chain_id, root_action_id=event.root_action_id,
                parent_id=event.id, ancestry=event.ancestry,
                proc_coefficient=event.proc_coefficient,
                source_faction=event.source_faction,
                source_position=event.source_position,
            }
            Rewards.on_event(state, event, function(kind, fields, source_id)
                emit(state, output, kind, fields, source_id, event.action_id)
            end)
            context.cause = nil
        else
            local item = context.effects[context.effect_head]
            context.effect_head = context.effect_head + 1
            execute_effect(state, output, item.activation, item.effect)
        end
    end
end

local function attack(state, output, source, action_id, ability_id, cells, raw_damage, ranged, extra)
    local context = contexts[output]
    local previous_cause = context and context.cause
    if context then
        context.cause = {chain_id=action_id, root_action_id=action_id, ancestry={},
            proc_coefficient=Content.ability_proc_coefficient(ability_id)}
    end
    local position = source.position
    local critical = false
    if source.controller == "player" and
        (ability_id == "core:ability/basic_attack"
            or ability_id == "core:ability/sweeping_slash")
        and (source.crit_chance_ppm or 0) > 0 then
        critical = RNG.next_int(state.rng, 1000000) <= source.crit_chance_ppm
        if critical then
            raw_damage = math.floor(raw_damage * source.crit_damage_percent / 100 + 0.5)
        end
    end
    local fields = {
        floor_id=position.floor_id, from_x=position.x, from_y=position.y,
        ability_id=ability_id, target_cells=cells, raw_damage=raw_damage,
    }
    if critical then fields.critical = true end
    if #cells == 1 and ability_id == "core:ability/basic_attack" then
        fields.target_x, fields.target_y = cells[1].x, cells[1].y
    end
    if extra then for key, value in pairs(extra) do fields[key] = value end end
    emit(state, output, "AttackPerformed", fields, source.id, action_id)
    damage_batch(state, output, source, action_id, cells, raw_damage, ranged,
        nil, critical)
    if Boons.active(state) then
        emit(state, output, "AbilityUsed", fields, source.id, action_id)
    end
    if context then context.cause = previous_cause end
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
            emit(state, output, "TileEntered", {
                floor_id=position.floor_id, from_x=from_x, from_y=from_y,
                x=x, y=y}, enemy.id, action_id)
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
            local payload = state.delayed_effects[item.id]
            if payload then
                state.delayed_effects[item.id] = nil
                local context = contexts[output]
                context.cause = effect_cause(payload.activation, payload.effect)
                emit(state, output, "DelayedEffectResolved", {
                    effect_id=item.id, boon_id=payload.activation.boon_id,
                    activation_id=payload.activation.id,
                    target_cells=payload.activation.event.target_cells,
                }, payload.activation.owner_id, payload.activation.root_action_id)
                context.cause = nil
                execute_effect(state, output, payload.activation, payload.effect)
            else
                emit(state, output, "ScheduledEffectResolved", {effect_id=item.effect_id},
                    item.actor_id, item.attack_id)
            end
        end
        drain_effects(state, output)
    end
end

function Game.submit(state, intent)
    if state.game_over then return false, "game over", {} end
    local ready = Scheduler.peek(state)
    assert(ready and ready.kind == "actor_ready" and ready.actor_id == state.player_id
        and ready.due == state.clock, "player is not ready")
    if type(intent) ~= "table" then return false, "invalid intent", {} end
    local preview, chest
    if intent.kind == "claim_chest" then
        chest = Game.entity(state, intent.chest_id)
        if not chest or chest.controller ~= "chest" then
            return false, "unknown chest", {}
        end
        local valid, reason = Rewards.can_claim(state, chest, intent.offer_index)
        if not valid then return false, reason, {} end
        preview = {valid=true, cost=Game.effective_cost(COSTS.claim_chest,
            Game.player(state).speed)}
    else
        preview = Game.preview(state, intent.kind, intent.dx, intent.dy)
        if not preview.valid then return false, preview.reason, {}, preview end
    end

    Scheduler.pop(state)
    local output = {}
    contexts[output] = {events={}, event_head=1, effects={}, effect_head=1}
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
        for step = 1, preview.distance do
            local x, y = preview.cells[step].x, preview.cells[step].y
            local from_step_x = from_x + intent.dx * (step - 1)
            local from_step_y = from_y + intent.dy * (step - 1)
            emit(state, output, "TileEntered", {
                floor_id=position.floor_id, from_x=from_step_x, from_y=from_step_y,
                x=x, y=y, source_position={floor_id=position.floor_id, x=x, y=y},
            }, player.id, action_id)
            Rewards.pickup_at(state, player, x, y, function(event_kind, reward_fields, source_id)
                emit(state, output, event_kind, reward_fields, source_id, action_id)
            end)
        end
    elseif kind == "basic_attack" or kind == "sweeping_slash" then
        action.dx, action.dy = intent.dx, intent.dy
        local cells = {}
        local target, damage = Game.effective_ability(state, kind, intent.dx, intent.dy)
        for index, cell in ipairs(target) do
            cells[index] = {floor_id=position.floor_id, x=cell.x, y=cell.y}
        end
        local ability_id = kind == "basic_attack" and "core:ability/basic_attack"
            or "core:ability/sweeping_slash"
        attack(state, output, player, action_id, ability_id,
            cells, damage,
            false, preview.bump and {bump=true} or nil)
    elseif kind == "claim_chest" then
        local offer = Rewards.claim(state, chest, intent.offer_index,
            function(event_kind, reward_fields, source_id)
                emit(state, output, event_kind, reward_fields, source_id, action_id)
            end)
        action.chest_id, action.boon_id, action.rarity = chest.id,
            offer.boon_id, offer.rarity
    else
        emit(state, output, "Waited", {floor_id=position.floor_id,
            x=position.x, y=position.y}, player.id, action_id)
    end
    state.last_action = action
    drain_effects(state, output)
    Scheduler.enqueue(state, {kind="actor_ready", due=state.clock + preview.cost,
        actor_id=player.id, is_player=true})
    advance_to_player(state, output)
    Boons.prune_histories(state)
    contexts[output] = nil
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

function Game.pending_boon_effects(state)
    local result = {}
    for _, item in ipairs(state.schedule) do
        local payload = state.delayed_effects[item.id]
        if payload then
            result[#result + 1] = {
                id=item.id, due=item.due, boon_id=payload.activation.boon_id,
                target_cells=copy(payload.activation.event.target_cells),
            }
        end
    end
    return result
end

function Game.snapshot(state)
    return copy(state)
end

function Game.restore(snapshot)
    assert(type(snapshot) == "table" and snapshot.schema == SCHEMA, "incompatible run schema")
    assert(snapshot.content_version == Content.version, "incompatible content version")
    assert(snapshot.boon_content_version == BoonContent.version,
        "incompatible boon content version")
    Content.validate_all()
    BoonContent.validate()
    local map = assert(Content.get_map(snapshot.map_id), "unknown map content ID")
    assert(snapshot.active_floor_id == map.floor_id, "incompatible floor ID")
    assert(type(snapshot.clock) == "number" and snapshot.clock % 1 == 0 and snapshot.clock >= 0,
        "invalid world clock")
    assert(type(snapshot.entities) == "table" and type(snapshot.schedule) == "table", "invalid run state")
    assert(type(snapshot.rng) == "table", "missing RNG state")
    assert(type(snapshot.chain_history) == "table" and
        type(snapshot.delayed_effects) == "table", "missing boon state")
    assert(type(snapshot.progression) == "table" and
        type(snapshot.progression.level) == "number" and
        snapshot.progression.level % 1 == 0 and snapshot.progression.level >= 1
        and type(snapshot.progression.xp_total) == "number"
        and snapshot.progression.xp_total % 1 == 0
        and snapshot.progression.xp_total >= 0
        and snapshot.progression.xp_total < Rewards.next_threshold(snapshot.progression.level),
        "invalid progression state")
    assert(type(snapshot.currency) == "number" and snapshot.currency % 1 == 0
        and snapshot.currency >= 0 and type(snapshot.drops) == "table"
        and type(snapshot.rewarded_kills) == "table"
        and type(snapshot.next_drop_id) == "number", "invalid reward state")
    local state = copy(snapshot)
    RNG.new(state.rng.state)
    local player = Game.player(state)
    assert(player.position.floor_id == map.floor_id, "player on wrong floor")
    assert(Content.walkable(map, player.position.x, player.position.y), "player outside walkable map")
    assert(type(player.crit_chance_ppm) == "number" and player.crit_chance_ppm % 1 == 0
        and player.crit_chance_ppm >= 0 and
        type(player.crit_damage_percent) == "number"
        and player.crit_damage_percent % 1 == 0 and player.crit_damage_percent >= 100,
        "invalid player crit stats")
    assert(type(player.hp) == "number" and player.hp >= 0 and player.hp <= player.max_hp,
        "invalid player health")
    local occupied = {}
    for _, entity in ipairs(state.entities) do
        Boons.validate_inventory(entity)
        if entity.controller == "chest" then Rewards.validate_chest(entity) end
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
    local drop_ids = {}
    for _, drop in ipairs(state.drops) do
        assert(type(drop.id) == "number" and drop.id % 1 == 0
            and drop.id >= 1 and drop.id < state.next_drop_id
            and not drop_ids[drop.id] and drop.kind == "currency"
            and type(drop.amount) == "number" and drop.amount % 1 == 0
            and drop.amount > 0 and type(drop.collected) == "boolean"
            and drop.position.floor_id == map.floor_id
            and Content.walkable(map, drop.position.x, drop.position.y),
            "invalid currency drop")
        drop_ids[drop.id] = true
    end
    for id, payload in pairs(state.delayed_effects) do
        local found = false
        for _, item in ipairs(state.schedule) do
            if item.id == id and item.kind == "effect" and item.effect_id == id then
                found = true; break
            end
        end
        assert(found and type(payload.activation) == "table"
            and type(payload.activation.ancestry) == "table", "invalid delayed boon effect")
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
