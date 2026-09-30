local Content = require("src.content.legacy")
local Fire = require("src.simulation.fire")
local Grid = require("src.world.grid")
local Session = require("src.simulation.session")
local World = require("src.world.world")

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

local function open_world(registry, owner, terrain)
  local result = {}
  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do
      result[key(x, y)] = true
    end
  end
  return World.new(registry, terrain or "cave", result, owner)
end

local function new_session(seed)
  local session = Session.new({ seed = seed or 3701 })
  session:start_run(Content.classes[1], Content.boons[1])
  return session
end

local function prepare_session(seed, terrain, points)
  local session = new_session(seed)
  local state = session.state
  state.world = points == nil and open_world(session.registry, state, terrain or "forest")
    or World.new(session.registry, terrain or "forest", layout(points), state)
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

local function fire_snapshot(world)
  local values = {}
  for _, fire in ipairs(world:list_fires(true)) do
    values[#values + 1] = table.concat({
      fire.id, fire.target_key, fire.age, fire.ready_tick, tostring(fire.active),
      tostring(fire.provenance.ignited_by_fire_id),
    }, ":")
  end
  return table.concat(values, "|")
end

return {
  {
    name = "fire ignition is material-gated deterministic and cannot duplicate a physical target",
    run = function()
      local session = new_session(3702)
      local owner = { next_world_object_sequence = 1, next_hazard_sequence = 1, next_fire_sequence = 1 }
      local forest = World.new(session.registry, "forest", {}, owner)
      local first = Fire.ignite_terrain(forest, 6, 6, { source = "test" })
      assert(first.applied and first.fire.id == "fire:000001")
      local duplicate = Fire.ignite_terrain(forest, 6, 6, { source = "test" })
      assert(not duplicate.applied and duplicate.code == "already_burning")
      local cave = World.new(session.registry, "cave", {}, owner)
      local stone = Fire.ignite_terrain(cave, 6, 6, { source = "test" })
      assert(not stone.applied and stone.code == "not_flammable")

      local other_owner = { next_world_object_sequence = 1, next_hazard_sequence = 1, next_fire_sequence = 1 }
      local other = World.new(session.registry, "forest", {}, other_owner)
      assert(Fire.ignite_terrain(other, 6, 6, { source = "test" }).fire.id == first.fire.id)
    end,
  },
  {
    name = "burning terrain waits one tick then consumes material through ordinary terrain integrity",
    run = function()
      local session = new_session(3703)
      local world = World.new(session.registry, "forest", {}, { next_fire_sequence = 1 })
      local material = session.registry:get_material("material.terrain.brush")
      local fire = assert(Fire.ignite_terrain(world, 7, 7, { source = "test" }).fire)
      assert(world:get_cell(7, 7).current_integrity == material.max_integrity)
      local first_tick = Fire.tick(world)
      assert(#first_tick.burns == 0 and world:get_cell(7, 7).current_integrity == material.max_integrity)
      local second_tick = Fire.tick(world)
      assert(#second_tick.burns == 1 and world:get_cell(7, 7).current_integrity == material.max_integrity - material.burn_rate)
      assert(session.registry:get_material("material.terrain.brush").max_integrity == material.max_integrity)
      Fire.tick(world)
      assert(world:get_cell(7, 7).destroyed and world:is_passable(7, 7) and not world:blocks_vision(7, 7))
      assert(not fire.active and not world:fire_at_target("terrain:" .. key(7, 7)))
      assert(world:validate())
    end,
  },
  {
    name = "burning timber cover uses object integrity and extinguishes when destroyed",
    run = function()
      local session = new_session(3704)
      local world = open_world(session.registry, { next_world_object_sequence = 1, next_fire_sequence = 1 }, "cave")
      local crate = assert(world:place_object(CRATE, 8, 8))
      local fire = assert(Fire.ignite_object(world, crate, { source = "test" }).fire)
      Fire.tick(world)
      assert(crate.current_integrity == 4)
      Fire.tick(world)
      assert(crate.current_integrity == 3 and fire.active)
      Fire.tick(world)
      Fire.tick(world)
      Fire.tick(world)
      assert(crate.destroyed and world:is_passable(8, 8) and not fire.active)
      assert(world:validate())
    end,
  },
  {
    name = "fire spreads cardinally without same-tick cascade and respects nonflammable barriers",
    run = function()
      local session = new_session(3705)
      local world = World.new(session.registry, "forest", {}, { next_fire_sequence = 1 })
      local source = assert(Fire.ignite_terrain(world, 10, 10, { source = "test" }).fire)
      Fire.tick(world) -- ignition wait
      assert(not world:fire_at_target("terrain:" .. key(11, 10)))
      Fire.tick(world) -- source burns and creates cardinal neighbours
      local east_key = "terrain:" .. key(11, 10)
      local east = assert(world:fire_at_target(east_key))
      assert(east.provenance.ignited_by_fire_id == source.id and east.age == 0)
      local ordered = world:list_fires()
      assert(ordered[2].target_key == "terrain:" .. key(10, 11))
      assert(ordered[3].target_key == "terrain:" .. key(11, 10))
      assert(ordered[4].target_key == "terrain:" .. key(10, 9))
      assert(ordered[5].target_key == "terrain:" .. key(9, 10))
      assert(not world:fire_at_target("terrain:" .. key(12, 10)))
      Fire.tick(world) -- east burns; only then may it spread further
      assert(world:fire_at_target("terrain:" .. key(12, 10)))

      local cave = open_world(session.registry, { next_world_object_sequence = 1, next_fire_sequence = 1 }, "cave")
      local crate = assert(cave:place_object(CRATE, 10, 10))
      assert(Fire.ignite_object(cave, crate, { source = "test" }).applied)
      Fire.tick(cave)
      Fire.tick(cave)
      assert(#cave:list_fires() == 1, "stone/air neighbours cannot ignite")

      local barrier = World.new(session.registry, "forest", {}, { next_fire_sequence = 1 })
      local stone_cell = barrier:get_cell(11, 10)
      stone_cell.material_id, stone_cell.current_integrity = "material.terrain.stone", 4
      assert(Fire.ignite_terrain(barrier, 10, 10, { source = "test" }).applied)
      Fire.tick(barrier)
      Fire.tick(barrier)
      assert(not barrier:fire_at_target("terrain:" .. key(11, 10)))
    end,
  },
  {
    name = "burning movable crate retains one fire identity at its new physical position",
    run = function()
      local session = prepare_session(3706, "cave", nil)
      local world = session.state.world
      local crate = assert(world:place_object(CRATE, 8, 8))
      local fire = assert(session:ignite_world_object(crate, { source = "test" }).fire)
      local moved = session:apply_force(crate, { dx = 1, dy = 0, distance = 1, cause = "test" })
      assert(moved.applied and crate.x == 9 and crate.y == 8)
      local x, y = world:fire_position(fire)
      assert(x == 9 and y == 8 and #world:fires_at(8, 8) == 0)
      assert(world:fires_at(9, 8)[1].id == fire.id)
    end,
  },
  {
    name = "fire exposure damages an occupying player body without persistent actor burning",
    run = function()
      local session = prepare_session(3707, "forest", {
        { 10, 10 }, { 11, 10 }, { 12, 10 }, { 13, 10 }, { 14, 10 }, { 15, 10 },
      })
      local state, player = session.state, session.state.player
      player.x, player.y = 10, 10
      assert(session:ignite_terrain(10, 10, { source = "test" }).applied)
      local health, integrity = player.health, body_integrity(player)
      session:_update_fire()
      assert(player.health == health and body_integrity(player) == integrity)
      session:_update_fire()
      assert(player.health == health - 1 and body_integrity(player) == integrity - 1)
      local moved = session:apply_force(player, { dx = 1, dy = 0, distance = 5, cause = "test" })
      assert(moved.applied and player.x == 15)
      session:_update_fire()
      assert(player.health == health - 1 and body_integrity(player) == integrity - 1)
    end,
  },
  {
    name = "fire death of a body-bearing enemy creates an identity-preserving corpse",
    run = function()
      local session = prepare_session(3708, "forest", { { 10, 10 } })
      local state = session.state
      local enemy = session:_make_enemy("bomber", { x = 10, y = 10 })
      enemy.health = 1
      local component_id = enemy.body:get_component("internal_1").id
      state.enemies = { enemy }
      assert(session:ignite_terrain(10, 10, { source = "test" }).applied)
      session:_update_fire()
      session:_update_fire()
      assert(#state.enemies == 0 and #state.corpses == 1)
      assert(state.corpses[1].body:get_component("internal_1").id == component_id)
      session:validate_physical_ownership()
    end,
  },
  {
    name = "enemy pathing avoids active fire when safe and falls back through it when necessary",
    run = function()
      local session = prepare_session(3709, "forest", {
        { 10, 10 }, { 11, 10 }, { 12, 10 }, { 10, 11 }, { 11, 11 }, { 12, 11 },
      })
      local world = session.state.world
      assert(session:ignite_terrain(11, 10, { source = "test" }).applied)
      local route, safe = session:_hazard_aware_path(Grid.cell(10, 10), Grid.cell(12, 10), {})
      assert(safe and #route == 5)
      for _, point in ipairs(route) do
        assert(not (point.x == 11 and point.y == 10))
      end

      session.state.world = World.new(session.registry, "forest", layout({ { 10, 10 }, { 11, 10 }, { 12, 10 } }), session.state)
      assert(session:ignite_terrain(11, 10, { source = "test" }).applied)
      local fallback, fallback_safe = session:_hazard_aware_path(Grid.cell(10, 10), Grid.cell(12, 10), {})
      assert(not fallback_safe and #fallback == 3 and fallback[2].x == 11)
      assert(world ~= session.state.world)
    end,
  },
  {
    name = "flares ignite surviving fuel but new fire waits until a later world tick",
    run = function()
      local session = prepare_session(3710, "forest", { { 10, 10 }, { 11, 10 } })
      local state, world = session.state, session.state.world
      state.flares = { { kind = "flare", x = 10, y = 10, fuse = 1, radius = 1, stun = 2, source_actor_id = "actor.test" } }
      local integrity = world:get_cell(10, 10).current_integrity
      session:_update_flares()
      local fire = assert(world:fire_at_target("terrain:" .. key(10, 10)))
      assert(fire.provenance.source == "flare" and world:get_cell(10, 10).current_integrity == integrity)
      session:_update_fire()
      assert(world:get_cell(10, 10).current_integrity == integrity)
      session:_update_fire()
      assert(world:get_cell(10, 10).current_integrity == integrity - 1)
    end,
  },
  {
    name = "fire serialization and validation preserve deterministic state and reject impossible active targets",
    run = function()
      local session = prepare_session(3711, "forest", { { 8, 8 } })
      local world = session.state.world
      local fire = assert(session:ignite_terrain(8, 8, { source = "test", source_actor_id = "actor.test" }).fire)
      local data = world:to_data()
      assert(data.fire_tick == 0 and data.fires[1].id == fire.id and data.fires[1].target_key == fire.target_key)
      assert(session:run_data().progression.next_fire_sequence == session.state.next_fire_sequence)
      assert(world:inspect_cell(8, 8).fires[1].id == fire.id)
      assert(world:describe_cell(8, 8):find(fire.id, 1, true))
      assert(world:validate())
      local duplicate = {}
      for name, value in pairs(fire) do
        duplicate[name] = value
      end
      duplicate.id = "fire:999999"
      world.fires[duplicate.id] = duplicate
      world.fire_order[#world.fire_order + 1] = duplicate.id
      assert(not pcall(function() world:validate() end))
      world.fires[duplicate.id] = nil
      table.remove(world.fire_order)
      fire.age = -1
      assert(not pcall(function() world:validate() end))
      fire.age = 0
      world:get_cell(8, 8).material_id = "material.terrain.air"
      world:get_cell(8, 8).current_integrity = nil
      assert(not pcall(function() world:validate() end))
      world:get_cell(8, 8).material_id = "material.terrain.leaf_litter"
      world:get_cell(8, 8).current_integrity = 3
      fire.target_kind = "object"
      fire.target_id = "world_object:missing"
      assert(not pcall(function() world:validate() end))
    end,
  },
  {
    name = "fire ordering and spread state reproduce without consuming gameplay RNG",
    run = function()
      local function simulate(seed)
        local session = new_session(seed)
        local world = World.new(session.registry, "forest", {}, { next_fire_sequence = 1 })
        assert(Fire.ignite_terrain(world, 10, 10, { source = "test" }).applied)
        Fire.tick(world)
        Fire.tick(world)
        return fire_snapshot(world)
      end
      assert(simulate(3712) == simulate(3712))
    end,
  },
}
