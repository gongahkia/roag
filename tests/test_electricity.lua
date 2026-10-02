local Content = require("src.content.legacy")
local Electricity = require("src.simulation.electricity")
local EnvironmentDamage = require("src.simulation.environment_damage")
local Liquid = require("src.simulation.liquid")
local Grid = require("src.world.grid")
local Session = require("src.simulation.session")
local World = require("src.world.world")

local WATER = "liquid.water.legacy"
local METAL_CRATE = "world_object.cover.conductive_metal_crate"
local SHOCK = "ability.electrical.discharge"

local function key(x, y)
  return Grid.key(x, y)
end

local function open_world(registry, terrain, owner)
  local cells = {}
  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do
      cells[key(x, y)] = true
    end
  end
  return World.new(registry, terrain or "cave", cells, owner)
end

local function new_session(seed)
  local session = Session.new({ seed = seed or 3901 })
  session:start_run(Content.classes[1], Content.boons[1])
  return session
end

local function prepare(session, terrain)
  local state = session.state
  state.world = open_world(session.registry, terrain or "cave", state)
  state.enemies, state.targets, state.bullets = {}, {}, {}
  state.bombs, state.flares, state.area_attacks, state.corpses = {}, {}, {}, {}
  state.electrical_effects = {}
  state.phase, state.ended = "combat", nil
  state.player.health, state.player.impact = 3, 0
  state.player.x, state.player.y = 9, 10
  return state.world
end

local function give_player_coil(session)
  local coil = session.component_factory:create("component.internal.legacy_shock_coil")
  assert(session.state.player.body:install("internal_2", coil))
  return coil
end

local function reached_snapshot(result)
  local cells = {}
  for _, cell in ipairs(result.reached_cells) do
    cells[#cells + 1] = string.format("%d:%d:%d", cell.x, cell.y, cell.distance)
  end
  return table.concat(cells, "|")
end

local function body_integrities(actor)
  local result = {}
  for _, slot in ipairs(actor.body:list_installed_slots()) do
    result[slot.component.id] = slot.component.current_integrity
  end
  return result
end

local function any_body_damage(actor, before)
  for _, slot in ipairs(actor.body:list_installed_slots()) do
    if slot.component.current_integrity < before[slot.component.id] then
      return true
    end
  end
  return false
end

return {
  {
    name = "conductivity is derived live from terrain liquid and surviving world objects",
    run = function()
      local session = new_session(3902)
      local world = prepare(session)
      assert(not world:is_conductive_at(7, 7), "dry air is nonconductive")
      assert(world:add_liquid(7, 7, WATER, 1).applied and world:is_conductive_at(7, 7))
      assert(world:set_liquid(7, 7, WATER, 2).applied and world:is_conductive_at(7, 7))
      assert(world:set_liquid(7, 7, WATER, 3).applied and world:is_conductive_at(7, 7))
      local inspected = assert(world:inspect_cell(7, 7))
      assert(inspected.conductivity.conductive and inspected.conductivity.liquid == WATER)
      assert(world:set_liquid(7, 7, WATER, 0).applied and not world:is_conductive_at(7, 7))

      local crate = assert(world:place_object(METAL_CRATE, 8, 7))
      assert(world:is_conductive_at(8, 7) and world:conductivity_at(8, 7).object == crate.id)
      assert(EnvironmentDamage.apply_to_object(world, crate, { amount = 5, cause = "explosive" }).destroyed)
      assert(not world:is_conductive_at(8, 7), "destroyed metal no longer bridges a network")
      assert(world:validate())
    end,
  },
  {
    name = "electrical BFS is cardinal deterministic and stops at dry or diagonal gaps",
    run = function()
      local session = new_session(3903)
      local world = prepare(session)
      for x = 10, 12 do
        assert(world:add_liquid(x, 10, WATER, 1).applied)
      end
      local first = Electricity.trace(world, { x = 9, y = 10 }, { max_cells = 12 })
      local second = Electricity.trace(world, { x = 9, y = 10 }, { max_cells = 12 })
      assert(first.applied and first.network_size == 3)
      assert(reached_snapshot(first) == "10:10:1|11:10:2|12:10:3")
      assert(reached_snapshot(first) == reached_snapshot(second))

      local dry_gap = prepare(session)
      assert(dry_gap:add_liquid(10, 10, WATER, 1).applied)
      assert(dry_gap:add_liquid(12, 10, WATER, 1).applied)
      assert(Electricity.trace(dry_gap, { x = 9, y = 10 }, {}).network_size == 1)

      local stone_gap = World.new(session.registry, "cave", {
        [key(10, 10)] = true,
        [key(12, 10)] = true,
      })
      assert(stone_gap:add_liquid(10, 10, WATER, 1).applied)
      assert(stone_gap:add_liquid(12, 10, WATER, 1).applied)
      assert(stone_gap:get_material(11, 10).id == "material.terrain.stone")
      assert(Electricity.trace(stone_gap, { x = 9, y = 10 }, {}).network_size == 1)

      local diagonal = prepare(session)
      assert(diagonal:add_liquid(10, 10, WATER, 1).applied)
      assert(diagonal:add_liquid(11, 11, WATER, 1).applied)
      assert(Electricity.trace(diagonal, { x = 9, y = 10 }, {}).network_size == 1)
    end,
  },
  {
    name = "conductive movable cover bridges water and force updates later topology",
    run = function()
      local session = new_session(3904)
      local world = prepare(session)
      assert(world:add_liquid(10, 10, WATER, 1).applied)
      assert(world:add_liquid(12, 10, WATER, 1).applied)
      local crate = assert(world:place_object(METAL_CRATE, 11, 10))
      assert(Electricity.trace(world, { x = 9, y = 10 }, {}).network_size == 3)
      local moved = session:apply_force(crate, { dx = 0, dy = 1, distance = 1, cause = "test" })
      assert(moved.applied and crate.x == 11 and crate.y == 11)
      assert(Electricity.trace(world, { x = 9, y = 10 }, {}).network_size == 1)
      assert(world:validate())
    end,
  },
  {
    name = "bounded network traversal never becomes radial damage",
    run = function()
      local session = new_session(3905)
      local world = prepare(session)
      for x = 10, 20 do
        assert(world:add_liquid(x, 10, WATER, 1).applied)
      end
      local bounded = Electricity.trace(world, { x = 9, y = 10 }, { max_cells = 3 })
      assert(bounded.network_size == 3 and bounded.truncated)
      assert(reached_snapshot(bounded) == "10:10:1|11:10:2|12:10:3")
      assert(not Electricity.trace(world, { x = 7, y = 7 }, {}).applied, "nearby geometry without a conductor is not an AoE")
    end,
  },
  {
    name = "shared discharge damages a reached enemy exactly once with HP and localized injury",
    run = function()
      local session = new_session(3906)
      local world = prepare(session)
      give_player_coil(session)
      for x = 10, 12 do
        assert(world:add_liquid(x, 10, WATER, 1).applied)
      end
      local enemy = session:_make_enemy("cultist", { x = 12, y = 10 })
      enemy.health = 3
      session.state.enemies = { enemy }
      local before = body_integrities(enemy)
      local result = session:activate_actor_ability(session.state.player, SHOCK, { direction = "d" })
      assert(result.applied and result.implementation == "electrical_discharge")
      assert(enemy.health == 2 and any_body_damage(enemy, before))
      assert(#result.discharge.affected_actor_ids == 1)
      assert(result.discharge.affected_actor_ids[1] ~= "actor:player")
    end,
  },
  {
    name = "players can self shock and a loop still gives every actor one hit per discharge",
    run = function()
      local session = new_session(3907)
      local world = prepare(session)
      give_player_coil(session)
      session.state.player.x, session.state.player.y = 10, 10
      for _, point in ipairs({ { 10, 10 }, { 11, 10 }, { 10, 11 }, { 11, 11 } }) do
        assert(world:add_liquid(point[1], point[2], WATER, 1).applied)
      end
      local enemy = session:_make_enemy("cultist", { x = 11, y = 11 })
      enemy.health = 3
      session.state.enemies = { enemy }
      local health = session.state.player.health
      local result = session:activate_actor_ability(session.state.player, SHOCK, { direction = "d" })
      assert(result.applied and session.state.player.health == health - 1)
      assert(session.state.player.impact == 0, "electricity is direct damage, not a stun or impact status")
      assert(enemy.health == 2, "looped BFS must not multiply electrical damage")
      assert(#result.discharge.affected_actor_ids == 2)
    end,
  },
  {
    name = "liquid flow changes later electrical connectivity without a graph cache",
    run = function()
      local session = new_session(3908)
      local world = prepare(session)
      assert(world:add_liquid(10, 10, WATER, 3).applied)
      assert(world:add_liquid(12, 10, WATER, 1).applied)
      assert(Electricity.trace(world, { x = 9, y = 10 }, {}).network_size == 1)
      assert(Liquid.tick(world).applied)
      assert(world:liquid_amount(11, 10) == 1)
      assert(Electricity.trace(world, { x = 9, y = 10 }, {}).network_size >= 3)
    end,
  },
  {
    name = "electrical death creates an ordinary identity preserving corpse",
    run = function()
      local session = new_session(3909)
      local world = prepare(session)
      give_player_coil(session)
      for x = 10, 12 do
        assert(world:add_liquid(x, 10, WATER, 1).applied)
      end
      local enemy = session:_make_enemy("cultist", { x = 12, y = 10 })
      local coil = enemy.body:get_component("internal_1")
      enemy.health = 1
      session.state.enemies = { enemy }
      local result = session:activate_actor_ability(session.state.player, SHOCK, { direction = "d" })
      assert(result.applied and #session.state.enemies == 0)
      local corpse = session.state.corpses[1]
      assert(corpse and corpse.x == 12 and corpse.y == 10)
      assert(corpse.body:get_component("internal_1") == coil)
    end,
  },
  {
    name = "broken shock coils lose their body-derived capability immediately",
    run = function()
      local session = new_session(3910)
      prepare(session)
      local player = session.state.player
      local coil = give_player_coil(session)
      assert(coil and session:actor_has_capability(player, SHOCK))
      assert(session:damage_actor_body(player, { amount = coil.current_integrity, slot_id = "internal_2", cause = "test" }).became_broken)
      local result = session:activate_actor_ability(player, SHOCK, { direction = "d" })
      assert(not session:actor_has_capability(player, SHOCK))
      assert(not result.applied and result.code == "provider_broken")
    end,
  },
  {
    name = "cultist shock coil survives corpse salvage reconstruction and player activation",
    run = function()
      local session = new_session(3911)
      prepare(session)
      local player = session.state.player
      local cultist = session:_make_enemy("cultist", { x = player.x + 1, y = player.y })
      session.state.enemies = { cultist }
      local coil = cultist.body:get_component("internal_1")
      local coil_id, integrity = coil.id, coil.current_integrity
      session:_destroy_enemy(1)
      local corpse = session.state.corpses[1]
      assert(corpse.body:get_component("internal_1") == coil)
      assert(session:salvage_corpse_component(corpse.id, "internal_1").applied)
      assert(session:_complete_stage() == "reconstruction")
      assert(session:install_inventory_component(coil_id, "internal_2").applied)
      assert(session:complete_reconstruction().next == "curse")
      session:choose_curse(Content.curses[2])
      local world = prepare(session)
      assert(session.state.player.body:get_component("internal_2") == coil)
      assert(coil.id == coil_id and coil.current_integrity == integrity)
      assert(world:add_liquid(10, 10, WATER, 1).applied)
      local result = session:activate_actor_ability(session.state.player, SHOCK, { direction = "d" })
      assert(result.applied and result.component_id == coil_id and result.wear.applied)
      assert(coil.current_integrity == integrity - 1)
      assert(session:validate_physical_ownership())
    end,
  },
  {
    name = "cultist electrical AI uses the shared network activation path without dry-gap fallback",
    run = function()
      local session = new_session(3912)
      local world = prepare(session)
      session.state.player.x, session.state.player.y = 12, 10
      for x = 10, 12 do
        assert(world:add_liquid(x, 10, WATER, 1).applied)
      end
      local cultist = session:_make_enemy("cultist", { x = 9, y = 10 })
      session.state.enemies = { cultist }
      local health = session.state.player.health
      session:_enemy_turn()
      assert(session.state.player.health == health - 1)

      local dry = prepare(session)
      session.state.player.x, session.state.player.y = 12, 10
      assert(dry:add_liquid(10, 10, WATER, 1).applied)
      assert(dry:add_liquid(12, 10, WATER, 1).applied)
      cultist = session:_make_enemy("cultist", { x = 9, y = 10 })
      session.state.enemies = { cultist }
      health = session.state.player.health
      session:_enemy_turn()
      assert(session.state.player.health == health, "enemy cannot bypass a dry nonconductive gap")
    end,
  },
  {
    name = "the normal cave progression contains deterministic water and a salvageable electrical source",
    run = function()
      local first, second = new_session(3913), new_session(3913)
      first.state.stage, second.state.stage = 2, 2
      first:start_stage()
      second:start_stage()
      assert(#first.state.world:list_liquids() > 0 and #second.state.world:list_liquids() > 0)
      assert(#first.state.enemies == first.state.settings.enemies)
      local electrical = false
      for _, enemy in ipairs(first.state.enemies) do
        if first:actor_has_capability(enemy, SHOCK) then electrical = true; break end
      end
      assert(electrical, "the authored cave tier-two pool must retain a salvageable electrical source")
      assert(reached_snapshot(Electricity.trace(first.state.world, { x = 0, y = 0 }, {}))
        == reached_snapshot(Electricity.trace(second.state.world, { x = 0, y = 0 }, {})))
      assert(first.state.world:liquid_data()[1].x == second.state.world:liquid_data()[1].x)
    end,
  },
}
