local Content = require("src.content.legacy")
local EnvironmentDamage = require("src.simulation.environment_damage")
local Fire = require("src.simulation.fire")
local Grid = require("src.world.grid")
local Liquid = require("src.simulation.liquid")
local Session = require("src.simulation.session")
local World = require("src.world.world")

local WATER = "liquid.water.legacy"
local CRATE = "world_object.cover.timber_crate"

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

local function open_world(registry, terrain, owner)
  local result = {}
  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do
      result[key(x, y)] = true
    end
  end
  return World.new(registry, terrain or "cave", result, owner)
end

local function new_session(seed)
  local session = Session.new({ seed = seed or 3801 })
  session:start_run(Content.classes[1], Content.boons[1])
  return session
end

local function liquid_snapshot(world)
  local values = {}
  for _, liquid in ipairs(world:liquid_data()) do
    values[#values + 1] = string.format("%d:%d:%s:%d", liquid.x, liquid.y, liquid.liquid_id, liquid.amount)
  end
  return table.concat(values, "|")
end

return {
  {
    name = "liquid cells retain bounded semantic depth without affecting passability",
    run = function()
      local session = new_session(3802)
      local world = open_world(session.registry, "cave")
      local added = world:add_liquid(7, 7, WATER, 1)
      assert(added.applied and world:is_liquid_cell(7, 7) and world:liquid_amount(7, 7) == 1)
      local inspected = assert(world:inspect_cell(7, 7))
      assert(inspected.liquid.liquid_id == WATER and inspected.liquid.amount == 1 and inspected.liquid.max_depth == 3)
      assert(inspected.passable and not inspected.blocks_vision and not inspected.blocks_projectile)
      assert(world:add_liquid(7, 7, WATER, 3).code == "capacity_exceeded")
      assert(world:add_liquid(7, 7, "liquid.other.future", 1).code == "different_liquid")
      assert(world:remove_liquid(7, 7, 1).applied and not world:is_liquid_cell(7, 7))
      local solid = World.new(session.registry, "cave", {})
      assert(solid:set_liquid(0, 0, WATER, 1).code == "blocked_terrain")
      assert(world:validate())
    end,
  },
  {
    name = "liquid flow is cardinal synchronous conservative and settles at shallow equilibrium",
    run = function()
      local session = new_session(3803)
      local world = open_world(session.registry, "cave")
      assert(world:add_liquid(10, 10, WATER, 3).applied)
      local before = world:total_liquid_amount(WATER)
      local first = Liquid.tick(world)
      assert(first.applied and #first.transfers == 2)
      assert(world:total_liquid_amount(WATER) == before)
      assert(world:liquid_amount(10, 10) == 1)
      assert(world:liquid_amount(10, 11) == 1 and world:liquid_amount(11, 10) == 1)
      assert(world:liquid_amount(10, 12) == 0, "newly moved liquid cannot move twice in one tick")
      local settled = liquid_snapshot(world)
      assert(not Liquid.tick(world).applied and liquid_snapshot(world) == settled)
      assert(not Liquid.tick(world).applied and world:total_liquid_amount(WATER) == before)

      local full = World.new(session.registry, "cave", layout({ { 20, 20 }, { 20, 21 } }))
      assert(full:add_liquid(20, 20, WATER, 3).applied)
      assert(full:add_liquid(20, 21, WATER, 3).applied)
      assert(not Liquid.tick(full).applied and full:liquid_amount(20, 20) == 3 and full:liquid_amount(20, 21) == 3)
      assert(world:validate() and full:validate())
    end,
  },
  {
    name = "liquid cannot enter solid terrain but can flow into terrain opened on a later tick",
    run = function()
      local session = new_session(3804)
      local world = World.new(session.registry, "cave", layout({ { 10, 10 } }))
      assert(world:add_liquid(10, 10, WATER, 3).applied)
      assert(not Liquid.tick(world).applied and world:liquid_amount(11, 10) == 0)
      local breach = EnvironmentDamage.apply(world, 11, 10, { amount = 4, cause = "explosive", source = "test" })
      assert(breach.destroyed and world:liquid_amount(11, 10) == 0)
      assert(Liquid.tick(world).applied and world:liquid_amount(11, 10) == 1)
      assert(world:total_liquid_amount(WATER) == 3 and world:validate())
    end,
  },
  {
    name = "liquid extinguishes existing fire and centrally suppresses ignition and spread",
    run = function()
      local session = new_session(3805)
      local world = World.new(session.registry, "forest", layout({ { 10, 10 }, { 11, 10 } }))
      local integrity = world:get_cell(10, 10).current_integrity
      local fire = assert(Fire.ignite_terrain(world, 10, 10, { source = "test" }).fire)
      assert(world:add_liquid(10, 10, WATER, 1).applied)
      assert(Liquid.suppress_fires(world).applied and not fire.active)
      Fire.tick(world)
      assert(world:get_cell(10, 10).current_integrity == integrity)
      assert(Fire.ignite_terrain(world, 10, 10, { source = "test" }).code == "suppressed_by_liquid")

      local spread = World.new(session.registry, "forest", layout({ { 20, 20 }, { 21, 20 } }))
      assert(spread:add_liquid(21, 20, WATER, 1).applied)
      assert(Fire.ignite_terrain(spread, 20, 20, { source = "test" }).applied)
      Fire.tick(spread)
      Fire.tick(spread)
      assert(not spread:fire_at_target("terrain:" .. key(21, 20)), "water-covered fuel rejects fire spread")
      assert(spread:validate())
    end,
  },
  {
    name = "liquid phase extinguishes a burning crate moved by force without moving liquid",
    run = function()
      local session = new_session(3806)
      local state = session.state
      state.world = open_world(session.registry, "cave", state)
      state.enemies, state.targets, state.bullets, state.bombs, state.flares = {}, {}, {}, {}, {}
      local crate = assert(state.world:place_object(CRATE, 10, 10))
      local crate_id, integrity = crate.id, crate.current_integrity
      assert(state.world:add_liquid(11, 10, WATER, 1).applied)
      local fire = assert(session:ignite_world_object(crate, { source = "test" }).fire)
      local moved = session:apply_force(crate, { dx = 1, dy = 0, distance = 1, cause = "test" })
      assert(moved.applied and crate.id == crate_id and crate.x == 11 and state.world:liquid_amount(11, 10) == 1)
      assert(fire.active and state.world:fire_position(fire) == 11)
      local phase = session:_update_liquids()
      assert(phase.suppression.applied and not fire.active and crate.current_integrity == integrity)
      assert(state.world:liquid_amount(10, 10) == 0, "liquid stays at its world coordinate")
      assert(state.world:validate())
    end,
  },
  {
    name = "ordinary turns resolve liquid suppression before mature fire damage",
    run = function()
      local session = new_session(38061)
      local state = session.state
      state.world = open_world(session.registry, "forest", state)
      state.enemies, state.targets, state.bullets, state.bombs, state.flares, state.area_attacks = {}, {}, {}, {}, {}, {}
      state.settings.score = 99
      local integrity = state.world:get_cell(14, 10).current_integrity
      local fire = assert(session:ignite_terrain(14, 10, { source = "test" }).fire)
      fire.ready_tick = 1 -- mature on this turn if liquid did not suppress it first.
      assert(state.world:add_liquid(14, 10, WATER, 1).applied)
      session:turn("wait")
      assert(not fire.active and state.world:get_cell(14, 10).current_integrity == integrity)
    end,
  },
  {
    name = "generated cave pools and liquid data reproduce in stable order without perturbing floor content",
    run = function()
      local first, second = new_session(3807), new_session(3807)
      first.state.stage, second.state.stage = 2, 2
      first:start_stage()
      second:start_stage()
      assert(#first.state.world:liquid_data() > 0)
      assert(liquid_snapshot(first.state.world) == liquid_snapshot(second.state.world))
      assert(first.state.world:object_data()[1].id == second.state.world:object_data()[1].id)
      assert(first.state.world:hazard_data()[1].id == second.state.world:hazard_data()[1].id)
      assert(first.state.world:validate() and second.state.world:validate())
    end,
  },
  {
    name = "liquid serialization inspection and world validation expose only valid stored cells",
    run = function()
      local session = new_session(3808)
      local world = open_world(session.registry, "cave")
      assert(world:add_liquid(8, 9, WATER, 1).applied)
      assert(world:add_liquid(7, 9, WATER, 2).applied)
      local data = world:to_data()
      assert(data.liquid_tick == 0 and #data.liquids == 2)
      assert(data.liquids[1].x == 7 and data.liquids[2].x == 8)
      assert(world:describe_cell(7, 9):find(WATER .. "@2", 1, true))
      world.liquids[key(8, 9)].liquid_id = "liquid.missing"
      assert(not pcall(function() world:validate() end))
      world.liquids[key(8, 9)].liquid_id = WATER
      world.liquids[key(8, 9)].amount = 4
      assert(not pcall(function() world:validate() end))
    end,
  },
}
