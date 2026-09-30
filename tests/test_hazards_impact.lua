local Content = require("src.content.legacy")
local Grid = require("src.world.grid")
local Session = require("src.simulation.session")
local World = require("src.world.world")

local SPIKES = "hazard.legacy.spike_field"
local BARRICADE = "world_object.cover.masonry_barricade"

local function key(x, y)
  return Grid.key(x, y)
end

local function open_layout(points)
  local result = {}
  for _, point in ipairs(points or {}) do
    result[key(point[1], point[2])] = true
  end
  return result
end

local function open_world(registry, owner, closed)
  local result, closed_cells = {}, {}
  for _, point in ipairs(closed or {}) do
    closed_cells[key(point[1], point[2])] = true
  end
  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do
      if not closed_cells[key(x, y)] then
        result[key(x, y)] = true
      end
    end
  end
  return World.new(registry, "forest", result, owner)
end

local function new_session(seed)
  local session = Session.new({ seed = seed or 3601 })
  session:start_run(Content.classes[1], Content.boons[1])
  return session
end

local function prepare_open_session(seed, closed)
  local session = new_session(seed)
  local state = session.state
  state.world = open_world(session.registry, state, closed)
  state.enemies, state.targets, state.bullets, state.bombs = {}, {}, {}, {}
  state.flares, state.torches, state.area_attacks, state.effects = {}, {}, {}, {}
  state.ammo, state.exit, state.corpses = nil, nil, {}
  state.player.x, state.player.y = 2, 2
  return session
end

local function body_integrity(actor)
  local total = 0
  for _, slot in ipairs(actor.body:list_installed_slots()) do
    total = total + slot.component.current_integrity
  end
  return total
end

local function hazard_snapshot(session)
  local values = {}
  for _, hazard in ipairs(session.state.world:list_hazards(true)) do
    values[#values + 1] = table.concat({ hazard.id, hazard.definition_id, hazard.x, hazard.y, tostring(hazard.active) }, ":")
  end
  return table.concat(values, "|")
end

return {
  {
    name = "hazard instances are deterministic inspectable world state with plain data",
    run = function()
      local owner = { next_world_object_sequence = 1, next_hazard_sequence = 1 }
      local session = new_session(3602)
      local world = open_world(session.registry, owner)
      local hazard = assert(world:place_hazard(SPIKES, 6, 6))
      assert(hazard.id == "hazard:000001" and hazard.active)
      assert(world:is_passable(6, 6) and world:is_hazardous(6, 6))
      local inspected = assert(world:inspect_cell(6, 6))
      assert(inspected.hazards[1].id == hazard.id and inspected.hazards[1].effect.amount == 1)
      assert(world:describe_cell(6, 6):find(hazard.id, 1, true))
      local data = world:to_data().hazards[1]
      assert(data.id == hazard.id and data.definition_id == SPIKES and data.x == 6 and data.active)
      local _, duplicate = world:place_hazard(SPIKES, 6, 6)
      assert(duplicate.code == "occupied_hazard")
      assert(world:validate())

      local first, second = new_session(3603), new_session(3603)
      assert(hazard_snapshot(first) == hazard_snapshot(second))
    end,
  },
  {
    name = "voluntary entry onto a passable spike field damages health and one body component once",
    run = function()
      local session = prepare_open_session(3604)
      local player = session.state.player
      player.x, player.y = 10, 10
      assert(session.state.world:place_hazard(SPIKES, 10, 11))
      local health, integrity = player.health, body_integrity(player)
      local entered = session:_move_player("w")
      assert(entered.applied and player.x == 10 and player.y == 11)
      assert(player.health == health - 1 and body_integrity(player) == integrity - 1)
      assert(entered.hazard.applied and entered.hazard.results[1].hazard_definition_id == SPIKES)

      session:turn("e")
      assert(player.health == health - 1 and body_integrity(player) == integrity - 1)
    end,
  },
  {
    name = "force triggers hazards on every entered cell including intermediate cells",
    run = function()
      local session = prepare_open_session(3605)
      local state = session.state
      local enemy = session:_make_enemy("bomber", { x = 10, y = 10 })
      enemy.health = 3
      state.enemies = { enemy }
      assert(state.world:place_hazard(SPIKES, 11, 10))
      local health, integrity = enemy.health, body_integrity(enemy)
      local result = session:apply_force(enemy, { dx = 1, dy = 0, distance = 2, cause = "test_force" })
      assert(result.applied and result.moved_distance == 2 and result.final_x == 12)
      assert(enemy.health == health - 1 and body_integrity(enemy) == integrity - 1)
      assert(result.code == "applied" and not result.blocked and result.remaining_distance == 0)
    end,
  },
  {
    name = "hazard death from force uses normal corpse ownership at the entered hazard cell",
    run = function()
      local session = prepare_open_session(3606)
      local state = session.state
      local enemy = session:_make_enemy("bomber", { x = 10, y = 10 })
      enemy.health = 1
      local physical_id = enemy.body:get_component("internal_1").id
      state.enemies = { enemy }
      assert(state.world:place_hazard(SPIKES, 11, 10))
      local result = session:apply_force(enemy, { dx = 1, dy = 0, distance = 2, cause = "explosive", source_actor_id = "actor.test" })
      assert(result.code == "target_destroyed" and result.moved_distance == 1 and result.final_x == 11)
      assert(#state.enemies == 0 and #state.corpses == 1)
      local corpse = state.corpses[1]
      assert(corpse.x == 11 and corpse.y == 10)
      assert(corpse.body:get_component("internal_1").id == physical_id)
      session:validate_physical_ownership()
    end,
  },
  {
    name = "bomb force can complete combat to hazard injury death and physical corpse salvage state",
    run = function()
      local session = prepare_open_session(3613)
      local state = session.state
      state.player.x, state.player.y = 2, 2
      local enemy = session:_make_enemy("bomber", { x = 11, y = 10 })
      enemy.health = 3 -- survives the direct blast, then dies on spike entry.
      local physical_id = enemy.body:get_component("internal_1").id
      state.enemies = { enemy }
      assert(state.world:place_hazard(SPIKES, 12, 10))
      state.bombs = { { kind = "bomb", x = 10, y = 10, radius = 1, fuse = 1, source_actor_id = "actor.player.legacy" } }
      session:_update_bombs()
      assert(#state.enemies == 0 and #state.corpses == 1)
      local corpse = state.corpses[1]
      assert(corpse.x == 12 and corpse.y == 10)
      assert(corpse.body:get_component("internal_1").id == physical_id)
      session:validate_physical_ownership()
    end,
  },
  {
    name = "blocked force exposes remaining distance and causes localized wall impact damage",
    run = function()
      local session = prepare_open_session(3607, { { 8, 5 } })
      local state = session.state
      local enemy = session:_make_enemy("bomber", { x = 7, y = 5 })
      enemy.health = 4
      state.enemies = { enemy }
      local health, integrity = enemy.health, body_integrity(enemy)
      local result = session:apply_force(enemy, { dx = 1, dy = 0, distance = 2, cause = "explosive" })
      assert(not result.applied and result.blocked and result.blocker_code == "blocked_world")
      assert(result.remaining_distance == 2 and result.final_x == 7 and result.final_y == 5)
      assert(result.impact.applied and result.impact.severity == 2)
      assert(enemy.health == health - 2 and body_integrity(enemy) == integrity - 2)
      assert(enemy.x == 7 and enemy.y == 5)
    end,
  },
  {
    name = "partial force impacts by unspent distance while completed force has no collision damage",
    run = function()
      local session = prepare_open_session(3608, { { 8, 5 } })
      local state = session.state
      local enemy = session:_make_enemy("bomber", { x = 5, y = 5 })
      enemy.health = 4
      state.enemies = { enemy }
      local health = enemy.health
      local partial = session:apply_force(enemy, { dx = 1, dy = 0, distance = 3, cause = "test" })
      assert(partial.moved_distance == 2 and partial.remaining_distance == 1 and partial.impact.severity == 1)
      assert(enemy.x == 7 and enemy.health == health - 1)

      local clear = prepare_open_session(3609)
      local clear_enemy = clear:_make_enemy("bomber", { x = 5, y = 5 })
      clear_enemy.health = 4
      clear.state.enemies = { clear_enemy }
      local clear_health = clear_enemy.health
      local completed = clear:apply_force(clear_enemy, { dx = 1, dy = 0, distance = 2, cause = "test" })
      assert(completed.applied and not completed.blocked and completed.remaining_distance == 0)
      assert(completed.impact.code == "no_impact" and clear_enemy.health == clear_health)
    end,
  },
  {
    name = "fixed cover blocks force and produces impact while crawling actors remain externally movable",
    run = function()
      local session = prepare_open_session(3610)
      local state, player = session.state, session.state.player
      local enemy = session:_make_enemy("bomber", { x = 9, y = 10 })
      enemy.health = 3
      state.enemies = { enemy }
      assert(state.world:place_object(BARRICADE, 10, 10))
      local cover = session:apply_force(enemy, { dx = 1, dy = 0, distance = 1, cause = "test" })
      assert(cover.blocked and cover.blocker_code == "blocked_world" and cover.impact.applied)
      assert(enemy.x == 9 and enemy.health == 2)

      player.x, player.y = 5, 5
      assert(session:damage_actor_body(player, { amount = 3, slot_id = "left_leg", cause = "test" }).became_broken)
      assert(session:damage_actor_body(player, { amount = 3, slot_id = "right_leg", cause = "test" }).became_broken)
      assert(session:locomotion_state(player).state == "CRAWLING")
      local health, integrity = player.health, body_integrity(player)
      local crawl = session:apply_force(player, { dx = -1, dy = 0, distance = 1, cause = "test" })
      assert(crawl.applied and player.x == 4 and player.y == 5 and not crawl.impact.applied)
      local wall = session:apply_force(player, { dx = -1, dy = 0, distance = 5, cause = "test" })
      assert(wall.blocked and wall.impact.applied and player.health < health and body_integrity(player) < integrity)
      assert(session:locomotion_state(player).state == "CRAWLING")
    end,
  },
  {
    name = "impact target selection and force outcome reproduce under the same seed",
    run = function()
      local function resolve(seed)
        local session = prepare_open_session(seed, { { 8, 5 } })
        local enemy = session:_make_enemy("bomber", { x = 7, y = 5 })
        enemy.health = 4
        session.state.enemies = { enemy }
        return session:apply_force(enemy, { dx = 1, dy = 0, distance = 2, cause = "test" })
      end
      local first, second = resolve(3611), resolve(3611)
      assert(first.final_x == second.final_x and first.remaining_distance == second.remaining_distance)
      assert(first.impact.damage.body_damage.slot_id == second.impact.damage.body_damage.slot_id)
      assert(first.impact.damage.body_damage.component_id == second.impact.damage.body_damage.component_id)
    end,
  },
  {
    name = "enemy hazard-aware paths prefer a safe route and fall back only when necessary",
    run = function()
      local session = new_session(3612)
      local state = session.state
      state.world = World.new(session.registry, "forest", open_layout({
        { 10, 10 }, { 11, 10 }, { 12, 10 }, { 10, 11 }, { 11, 11 }, { 12, 11 },
      }), state)
      assert(state.world:place_hazard(SPIKES, 11, 10))
      local route, safe = session:_hazard_aware_path(Grid.cell(10, 10), Grid.cell(12, 10), {})
      assert(safe and #route == 5)
      for _, point in ipairs(route) do
        assert(not (point.x == 11 and point.y == 10))
      end

      state.world = World.new(session.registry, "forest", open_layout({ { 10, 10 }, { 11, 10 }, { 12, 10 } }), state)
      assert(state.world:place_hazard(SPIKES, 11, 10))
      local fallback, fallback_safe = session:_hazard_aware_path(Grid.cell(10, 10), Grid.cell(12, 10), {})
      assert(not fallback_safe and #fallback == 3 and fallback[2].x == 11)
    end,
  },
}
