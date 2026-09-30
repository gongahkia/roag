local Content = require("src.content.legacy")
local EnvironmentDamage = require("src.simulation.environment_damage")
local Gas = require("src.simulation.gas")
local Grid = require("src.world.grid")
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

local function open_world(registry, terrain, owner)
  local cells = {}
  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do
      cells[key(x, y)] = true
    end
  end
  return World.new(registry, terrain or "dungeon", cells, owner)
end

local function new_session(seed)
  local session = Session.new({ seed = seed or 4001 })
  session:start_run(Content.classes[1], Content.boons[1])
  return session
end

local function prepare(session, terrain)
  local state = session.state
  state.world = open_world(session.registry, terrain or "dungeon", state)
  state.enemies, state.targets, state.bullets = {}, {}, {}
  state.bombs, state.flares, state.area_attacks, state.corpses = {}, {}, {}, {}
  state.phase, state.ended = "combat", nil
  state.settings.score = 99
  state.player.health, state.player.impact = 3, 0
  state.player.x, state.player.y = 10, 10
  return state.world
end

local function gas_snapshot(world)
  local values = {}
  for _, gas in ipairs(world:gas_data()) do
    values[#values + 1] = string.format("%d:%d:%s:%d", gas.x, gas.y, gas.gas_id, gas.concentration)
  end
  return table.concat(values, "|")
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
    name = "gas cells retain bounded semantic concentration and reject mixing without affecting passability",
    run = function()
      local session = new_session(4002)
      local world = prepare(session)
      assert(world:add_gas(7, 7, TOXIC, 3).applied)
      assert(world:gas_concentration(7, 7) == 3 and world:is_gas_cell(7, 7))
      local inspected = assert(world:inspect_cell(7, 7))
      assert(inspected.gas.gas_id == TOXIC and inspected.gas.concentration == 3)
      assert(inspected.gas.max_concentration == 4 and inspected.gas.exposure_threshold == 2 and inspected.gas.harmful)
      assert(inspected.passable and not inspected.blocks_vision and not inspected.blocks_projectile)
      assert(world:add_gas(7, 7, TOXIC, 2).code == "capacity_exceeded")
      world.registry.gases["gas.test.other"] = {
        id = "gas.test.other", display_name = "Other Test Gas", max_concentration = 4,
        exposure_threshold = 2, damage = 1, render_style = "toxic_cloud",
      }
      assert(world:add_gas(7, 7, "gas.test.other", 1).code == "different_gas")
      world.registry.gases["gas.test.other"] = nil
      assert(world:remove_gas(7, 7, 3).applied and not world:is_gas_cell(7, 7))
      local solid = World.new(session.registry, "dungeon", {})
      assert(solid:set_gas(0, 0, TOXIC, 1).code == "blocked_geometry")
      assert(world:validate())
    end,
  },
  {
    name = "gas diffusion is cardinal synchronous conservative and uses north first with one transfer per source",
    run = function()
      local session = new_session(4003)
      local world = prepare(session)
      assert(world:add_gas(10, 10, TOXIC, 4).applied)
      local total = world:total_gas_amount(TOXIC)
      local first = Gas.tick(world)
      assert(first.applied and #first.transfers == 1)
      assert(first.transfers[1].to_x == 10 and first.transfers[1].to_y == 11)
      assert(world:gas_concentration(10, 10) == 3 and world:gas_concentration(10, 11) == 1)
      assert(world:gas_concentration(11, 10) == 0 and world:gas_concentration(10, 12) == 0,
        "one source transfer and synchronous updates prevent same-tick onward diffusion")
      assert(world:total_gas_amount(TOXIC) == total)
      Gas.tick(world)
      Gas.tick(world)
      local settled = gas_snapshot(world)
      assert(not Gas.tick(world).applied and gas_snapshot(world) == settled)
      assert(not Gas.tick(world).applied and world:total_gas_amount(TOXIC) == total)
      assert(world:validate())
    end,
  },
  {
    name = "solid terrain contains gas until a destroyed wall opens a later diffusion route",
    run = function()
      local session = new_session(4004)
      local world = World.new(session.registry, "cave", layout({ { 10, 10 } }))
      assert(world:add_gas(10, 10, TOXIC, 4).applied)
      assert(not Gas.tick(world).applied and world:gas_concentration(11, 10) == 0)
      local breach = EnvironmentDamage.apply(world, 11, 10, { amount = 4, cause = "explosive", source = "test" })
      assert(breach.destroyed and world:gas_concentration(11, 10) == 0)
      assert(Gas.tick(world).applied and world:gas_concentration(11, 10) == 1)
      assert(world:total_gas_amount(TOXIC) == 4 and world:validate())
    end,
  },
  {
    name = "trace gas is harmless while toxic concentration damages player HP and localized body",
    run = function()
      local session = new_session(4005)
      local world = prepare(session)
      assert(world:add_gas(10, 10, TOXIC, 1).applied)
      local health = session.state.player.health
      local clean = body_integrities(session.state.player)
      assert(not session:_update_gas().applied)
      assert(session.state.player.health == health and not any_body_damage(session.state.player, clean))

      assert(world:set_gas(10, 10, TOXIC, 4).applied)
      local before = body_integrities(session.state.player)
      assert(session:_update_gas().applied)
      assert(session.state.player.health == health - 1 and any_body_damage(session.state.player, before))
      session.state.player.x, session.state.player.y = 30, 30
      health = session.state.player.health
      session:_update_gas()
      assert(session.state.player.health == health, "leaving gas ends exposure without a poison status")
    end,
  },
  {
    name = "post-diffusion gas can expose an actor in a newly reached cell during the same turn",
    run = function()
      local session = new_session(4006)
      local world = prepare(session)
      session.state.player.x, session.state.player.y = 10, 11
      assert(world:add_gas(10, 10, TOXIC, 3).applied)
      assert(world:add_gas(10, 11, TOXIC, 1).applied)
      local health = session.state.player.health
      session:turn("wait")
      assert(world:gas_concentration(10, 11) == 2)
      assert(session.state.player.health == health - 1)
    end,
  },
  {
    name = "toxic gas death uses the ordinary identity preserving enemy corpse path",
    run = function()
      local session = new_session(4007)
      local world = prepare(session)
      local enemy = session:_make_enemy("cultist", { x = 12, y = 10 })
      local coil = enemy.body:get_component("internal_1")
      enemy.health = 1
      session.state.enemies = { enemy }
      assert(world:add_gas(12, 10, TOXIC, 4).applied)
      assert(session:_update_gas().applied and #session.state.enemies == 0)
      local corpse = session.state.corpses[1]
      assert(corpse and corpse.x == 12 and corpse.y == 10)
      assert(corpse.body:get_component("internal_1") == coil)
    end,
  },
  {
    name = "force into toxic gas waits for the gas exposure phase rather than triggering on entry",
    run = function()
      local session = new_session(4008)
      local world = prepare(session)
      session.state.player.x, session.state.player.y = 30, 30
      local enemy = session:_make_enemy("cultist", { x = 9, y = 10 })
      enemy.health = 2
      session.state.enemies = { enemy }
      assert(world:add_gas(10, 10, TOXIC, 4).applied)
      local forced = session:apply_force(enemy, { dx = 1, dy = 0, distance = 1, cause = "test" })
      assert(forced.applied and enemy.x == 10 and enemy.y == 10 and enemy.health == 2)
      session:_update_gas()
      assert(enemy.health == 1)
    end,
  },
  {
    name = "enemy pathing avoids harmful gas when safe and falls back through it when enclosed",
    run = function()
      local session = new_session(4009)
      local world = prepare(session)
      assert(world:add_gas(11, 10, TOXIC, 2).applied)
      local safe, avoided = session:_hazard_aware_path({ x = 10, y = 10 }, { x = 12, y = 10 }, {})
      assert(avoided and #safe > 0)
      for _, point in ipairs(safe) do
        assert(not (point.x == 11 and point.y == 10))
      end

      assert(world:set_gas(11, 10, TOXIC, 1).applied)
      local trace, trace_safe = session:_hazard_aware_path({ x = 10, y = 10 }, { x = 12, y = 10 }, {})
      assert(trace_safe and trace[2].x == 11 and trace[2].y == 10)

      local corridor = World.new(session.registry, "dungeon", layout({ { 10, 10 }, { 11, 10 }, { 12, 10 } }))
      assert(corridor:add_gas(11, 10, TOXIC, 2).applied)
      session.state.world = corridor
      local fallback, fallback_safe = session:_hazard_aware_path({ x = 10, y = 10 }, { x = 12, y = 10 }, {})
      assert(not fallback_safe and #fallback == 3 and fallback[2].x == 11)
    end,
  },
  {
    name = "generated dungeon gas is deterministic isolated and serializes in coordinate order",
    run = function()
      local first, second = new_session(4010), new_session(4010)
      first.state.stage, second.state.stage = 3, 3
      first:start_stage()
      second:start_stage()
      assert(#first.state.world:list_gases() > 0)
      assert(gas_snapshot(first.state.world) == gas_snapshot(second.state.world))
      assert(first.state.world:liquid_data()[1].x == second.state.world:liquid_data()[1].x)
      assert(first.state.world:object_data()[1].id == second.state.world:object_data()[1].id)
      local data = first.state.world:to_data()
      assert(data.gas_tick == 0 and #data.gases == #first.state.world:list_gases())
      assert(first.state.world:validate() and second.state.world:validate())
    end,
  },
  {
    name = "gas inspection and validation reject malformed or inaccessible stored world state",
    run = function()
      local session = new_session(4011)
      local world = prepare(session)
      assert(world:add_gas(8, 9, TOXIC, 1).applied)
      assert(world:add_gas(7, 9, TOXIC, 2).applied)
      local data = world:to_data()
      assert(data.gases[1].x == 7 and data.gases[2].x == 8)
      assert(world:describe_cell(7, 9):find(TOXIC .. "@2", 1, true))
      world.gases[key(8, 9)].gas_id = "gas.missing"
      assert(not pcall(function() world:validate() end))
      world.gases[key(8, 9)].gas_id = TOXIC
      world.gases[key(8, 9)].concentration = 5
      assert(not pcall(function() world:validate() end))
    end,
  },
}
