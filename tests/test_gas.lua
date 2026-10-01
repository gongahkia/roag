local Content = require("src.content.legacy")
local EnvironmentDamage = require("src.simulation.environment_damage")
local GasGeneration = require("src.generation.gases")
local Gas = require("src.simulation.gas")
local Grid = require("src.world.grid")
local Registry = require("src.content.registry")
local Rng = require("src.rng")
local Session = require("src.simulation.session")
local World = require("src.world.world")

local TOXIC = "gas.toxic.legacy"

local function key(x, y)
  return Grid.key(x, y)
end

local function layout(points)
  local result = {}
  for _, point in ipairs(points or {}) do
    result[key(point[1], point[2])] = true
  end
  return result
end

local function open_world(registry, owner)
  local result = {}
  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do
      result[key(x, y)] = true
    end
  end
  return World.new(registry, "cave", result, owner)
end

local function new_session(seed)
  local session = Session.new({ seed = seed or 3901 })
  session:start_run(Content.classes[1], Content.boons[1])
  return session
end

local function prepare_session(seed, points)
  local session = new_session(seed)
  local state = session.state
  state.world = points == nil and open_world(session.registry, state)
    or World.new(session.registry, "cave", layout(points), state)
  state.enemies, state.targets, state.bullets, state.bombs = {}, {}, {}, {}
  state.flares, state.torches, state.area_attacks, state.effects = {}, {}, {}, {}
  state.ammo, state.exit, state.corpses = nil, nil, {}
  state.player.x, state.player.y = 2, 2
  state.settings.score = 99
  return session
end

local function body_integrity(actor)
  local total = 0
  for _, slot in ipairs(actor.body:list_installed_slots()) do
    total = total + slot.component.current_integrity
  end
  return total
end

local function gas_snapshot(world)
  local values = {}
  for _, gas in ipairs(world:gas_data()) do
    values[#values + 1] = string.format("%d:%d:%s:%d", gas.x, gas.y, gas.gas_id, gas.concentration)
  end
  return table.concat(values, "|")
end

local function test_registry_with_second_gas()
  local gases = {}
  for _, gas in ipairs(require("content.gases.legacy")) do
    gases[#gases + 1] = gas
  end
  gases[#gases + 1] = {
    id = "gas.inert.test",
    display_name = "Test Inert Gas",
    max_concentration = 2,
    exposure_threshold = 1,
    damage = 0,
    render_style = "test",
  }
  return Registry.new({
    abilities = require("content.abilities.legacy"),
    materials = require("content.materials.legacy"),
    liquids = require("content.liquids.legacy"),
    gases = gases,
    world_objects = require("content.world_objects.legacy"),
    hazards = require("content.hazards.legacy"),
    components = require("content.components.legacy"),
    topologies = require("content.body_topologies.normal"),
    actors = require("content.actors.player_legacy"),
    enemies = require("content.enemies.legacy"),
  })
end

return {
  {
    name = "gas cells retain bounded semantic concentration and reject mixing",
    run = function()
      local registry = test_registry_with_second_gas()
      local world = open_world(registry)
      assert(world:set_gas(7, 7, TOXIC, 3).applied)
      assert(world:gas_at(7, 7).gas_id == TOXIC and world:gas_concentration(7, 7) == 3)
      assert(world:is_harmful_gas_at(7, 7))
      assert(world:add_gas(7, 7, TOXIC, 2).code == "capacity_exceeded")
      assert(world:add_gas(7, 7, "gas.inert.test", 1).code == "different_gas")
      assert(world:remove_gas(7, 7, 3).applied and not world:is_gas_cell(7, 7))
      local crate = assert(world:place_object("world_object.cover.timber_crate", 8, 8))
      assert(world:set_gas(8, 8, TOXIC, 1).applied and not crate.blocks_gas,
        "ordinary cover remains permeable to coordinate gas")
      local solid = World.new(registry, "cave", {})
      assert(solid:set_gas(0, 0, TOXIC, 1).code == "blocked_terrain")
      assert(world:validate() and solid:validate())
    end,
  },
  {
    name = "gas diffusion is cardinal synchronous conservative and settles",
    run = function()
      local session = new_session(3902)
      local world = open_world(session.registry)
      assert(world:set_gas(10, 10, TOXIC, 4).applied)
      local total = world:total_gas_amount(TOXIC)
      local first = Gas.tick(world)
      assert(first.applied and #first.transfers == 1)
      assert(first.transfers[1].to_x == 10 and first.transfers[1].to_y == 11, "north wins the fixed tie break")
      assert(world:gas_concentration(10, 10) == 3 and world:gas_concentration(10, 11) == 1)
      assert(world:gas_concentration(10, 12) == 0, "newly transferred gas cannot diffuse twice in one tick")
      assert(world:total_gas_amount(TOXIC) == total)
      while Gas.tick(world).applied do end
      local settled = gas_snapshot(world)
      assert(not Gas.tick(world).applied and gas_snapshot(world) == settled)
      assert(world:total_gas_amount(TOXIC) == total and world:validate())
    end,
  },
  {
    name = "solid terrain contains gas until later terrain destruction opens a route",
    run = function()
      local session = new_session(3903)
      local world = World.new(session.registry, "cave", layout({ { 10, 10 } }))
      assert(world:set_gas(10, 10, TOXIC, 3).applied)
      assert(not Gas.tick(world).applied and world:gas_concentration(11, 10) == 0)
      local breach = EnvironmentDamage.apply(world, 11, 10, { amount = 4, cause = "explosive", source = "test" })
      assert(breach.destroyed and world:gas_concentration(11, 10) == 0)
      assert(Gas.tick(world).applied and world:gas_concentration(11, 10) == 1)
      assert(world:total_gas_amount(TOXIC) == 3 and world:validate())
    end,
  },
  {
    name = "trace gas is harmless while toxic gas damages HP and one body component",
    run = function()
      local session = prepare_session(3904, { { 10, 10 } })
      local player = session.state.player
      player.x, player.y = 10, 10
      assert(session.state.world:set_gas(10, 10, TOXIC, 1).applied)
      local health, integrity = player.health, body_integrity(player)
      session:_update_gas()
      assert(player.health == health and body_integrity(player) == integrity)
      assert(session.state.world:set_gas(10, 10, TOXIC, 2).applied)
      session:_update_gas()
      assert(player.health == health - 1 and body_integrity(player) == integrity - 1)
    end,
  },
  {
    name = "gas exposure ends after leaving a cloud and newly diffused gas exposes immediately",
    run = function()
      local session = prepare_session(3905, { { 10, 10 }, { 11, 10 } })
      local player = session.state.player
      player.x, player.y = 10, 10
      assert(session.state.world:set_gas(10, 10, TOXIC, 3).applied)
      local health = player.health
      session:_update_gas()
      assert(player.health == health - 1 and session.state.world:gas_concentration(10, 10) == 2)
      assert(session:apply_force(player, { dx = 1, dy = 0, distance = 1, cause = "test" }).applied)
      session:_update_gas()
      assert(player.health == health - 1, "trace gas has no lingering poison effect")

      local incoming = prepare_session(39051, { { 10, 10 }, { 11, 10 } })
      local incoming_player = incoming.state.player
      incoming_player.x, incoming_player.y = 11, 10
      assert(incoming.state.world:set_gas(10, 10, TOXIC, 3).applied)
      local incoming_health = incoming_player.health
      incoming:_update_gas()
      assert(incoming.state.world:gas_concentration(11, 10) == 1)
      -- One more initial unit creates a post-diffusion concentration of two.
      assert(incoming.state.world:set_gas(10, 10, TOXIC, 4).applied)
      incoming:_update_gas()
      assert(incoming_player.health == incoming_health - 1, "post-diffusion concentration is exposed in the same gas phase")
    end,
  },
  {
    name = "gas damage kills body-bearing enemies through ordinary corpse ownership",
    run = function()
      local session = prepare_session(3906, { { 10, 10 } })
      local state = session.state
      local enemy = session:_make_enemy("bomber", { x = 10, y = 10 })
      enemy.health = 1
      local component_id = enemy.body:get_component("internal_1").id
      state.enemies = { enemy }
      assert(state.world:set_gas(10, 10, TOXIC, 2).applied)
      session:_update_gas()
      assert(#state.enemies == 0 and #state.corpses == 1)
      assert(state.corpses[1].body:get_component("internal_1").id == component_id)
      session:validate_physical_ownership()
    end,
  },
  {
    name = "force into gas waits for the gas exposure phase",
    run = function()
      local session = prepare_session(3907, { { 10, 10 }, { 11, 10 } })
      local state = session.state
      local enemy = session:_make_enemy("bomber", { x = 10, y = 10 })
      enemy.health = 2
      state.enemies = { enemy }
      assert(state.world:set_gas(11, 10, TOXIC, 3).applied)
      assert(session:apply_force(enemy, { dx = 1, dy = 0, distance = 1, cause = "test" }).applied)
      assert(enemy.health == 2 and enemy.x == 11)
      session:_update_gas()
      assert(enemy.health == 1)
    end,
  },
  {
    name = "enemy pathing avoids harmful gas when safe and falls back through it when necessary",
    run = function()
      local session = prepare_session(3908, {
        { 10, 10 }, { 11, 10 }, { 12, 10 }, { 10, 11 }, { 11, 11 }, { 12, 11 },
      })
      local world = session.state.world
      assert(world:set_gas(11, 10, TOXIC, 2).applied)
      local safe, safe_route = session:_hazard_aware_path(Grid.cell(10, 10), Grid.cell(12, 10), {})
      assert(safe_route and #safe == 5)
      for _, point in ipairs(safe) do
        assert(not (point.x == 11 and point.y == 10))
      end
      session.state.world = World.new(session.registry, "cave", layout({ { 10, 10 }, { 11, 10 }, { 12, 10 } }), session.state)
      assert(session.state.world:set_gas(11, 10, TOXIC, 2).applied)
      local fallback, fallback_safe = session:_hazard_aware_path(Grid.cell(10, 10), Grid.cell(12, 10), {})
      assert(not fallback_safe and #fallback == 3 and fallback[2].x == 11)
      assert(session.state.world:set_gas(11, 10, TOXIC, 1).applied)
      local trace, trace_safe = session:_hazard_aware_path(Grid.cell(10, 10), Grid.cell(12, 10), {})
      assert(trace_safe and #trace == 3)
    end,
  },
  {
    name = "generated gas serialization inspection and validation are deterministic",
    run = function()
      local first, second = new_session(3909), new_session(3909)
      first.state.stage, second.state.stage = 2, 2
      first:start_stage()
      second:start_stage()
      assert(#first.state.world:gas_data() > 0)
      assert(gas_snapshot(first.state.world) == gas_snapshot(second.state.world))
      assert(first.state.world:object_data()[1].id == second.state.world:object_data()[1].id)
      assert(first.state.world:hazard_data()[1].id == second.state.world:hazard_data()[1].id)

      local world = open_world(first.registry)
      assert(world:set_gas(8, 9, TOXIC, 1).applied)
      assert(world:set_gas(7, 9, TOXIC, 2).applied)
      local data = world:to_data()
      assert(#data.gases == 2 and data.gases[1].x == 7 and data.gases[2].x == 8)
      local inspected = assert(world:inspect_cell(7, 9))
      assert(inspected.gas.gas_id == TOXIC and inspected.gas.concentration == 2 and inspected.gas.harmful)
      assert(world:describe_cell(7, 9):find(TOXIC .. "@2", 1, true))
      world.gases[key(8, 9)].concentration = 5
      assert(not pcall(function() world:validate() end))
      world.gases[key(8, 9)].concentration = 1
      world.gases[key(8, 9)].gas_id = "gas.missing"
      assert(not pcall(function() world:validate() end))
    end,
  },
  {
    name = "gas generation and exposure do not consume authoritative session RNG",
    run = function()
      local first, reference = new_session(3910), new_session(3910)
      local generation_world = open_world(first.registry)
      GasGeneration.place(generation_world, "cave", Grid.cell(2, 2), first.rng:derive("gases.test"))
      assert(first.rng:int(1, 100000) == reference.rng:int(1, 100000))

      local exposed, untouched = prepare_session(3911, { { 10, 10 } }), prepare_session(3911, { { 10, 10 } })
      exposed.state.player.x, exposed.state.player.y = 10, 10
      assert(exposed.state.world:set_gas(10, 10, TOXIC, 2).applied)
      exposed:_update_gas()
      assert(exposed.rng:int(1, 100000) == untouched.rng:int(1, 100000))
    end,
  },
  {
    name = "ordinary turn processing runs fire before gas and skips dead actors",
    run = function()
      local session = prepare_session(3912, { { 10, 10 }, { 11, 10 } })
      local state, player = session.state, session.state.player
      state.world = World.new(session.registry, "forest", layout({ { 10, 10 }, { 11, 10 } }), state)
      player.x, player.y, player.health = 10, 10, 1
      state.settings.targets, state.settings.enemies = 0, 0
      state.ammo = { kind = "ammo", x = 11, y = 10 }
      local integrity = body_integrity(player)
      local fire = assert(session:ignite_terrain(10, 10, { source = "test" }).fire)
      fire.ready_tick = 1
      assert(state.world:set_gas(10, 10, TOXIC, 2).applied)
      assert(session:turn("wait") == "gameover")
      assert(body_integrity(player) == integrity - 1,
        "mature fire kills before the later gas phase; a dead actor is not exposed again")
    end,
  },
}
