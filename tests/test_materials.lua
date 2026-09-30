local Content = require("src.content.legacy")
local Registry = require("src.content.registry")
local EnvironmentDamage = require("src.simulation.environment_damage")
local Generator = require("src.generation.map")
local Rng = require("src.rng")
local Session = require("src.simulation.session")
local Grid = require("src.world.grid")
local World = require("src.world.world")

local function key(x, y)
  return Grid.key(x, y)
end

local function open_layout(points)
  local layout = {}
  for _, point in ipairs(points or {}) do
    layout[key(point[1], point[2])] = true
  end
  return layout
end

local function open_everywhere_except(points)
  local layout, closed = {}, {}
  for _, point in ipairs(points or {}) do
    closed[key(point[1], point[2])] = true
  end
  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do
      if not closed[key(x, y)] then
        layout[key(x, y)] = true
      end
    end
  end
  return layout
end

local function new_session(seed)
  local session = Session.new({ seed = seed or 2401 })
  session:start_run(Content.classes[1], Content.boons[1])
  return session
end

local function world_fingerprint(world)
  local values = {}
  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do
      local cell = assert(world:inspect_cell(x, y))
      values[#values + 1] = table.concat({
        cell.material_id,
        tostring(cell.current_integrity),
        tostring(cell.destroyed),
      }, ":")
    end
  end
  return table.concat(values, "|")
end

return {
  {
    name = "terrain instances retain material identity and mutable integrity without changing content",
    run = function()
      local registry = Registry.load()
      local world = World.new(registry, "forest", {})
      local before = assert(world:inspect_cell(4, 4))
      assert(before.material_id == "material.terrain.brush")
      assert(before.current_integrity == 2 and not before.passable and before.blocks_vision)

      local result = EnvironmentDamage.apply(world, 4, 4, {
        amount = 1,
        cause = "kinetic",
        source = "test",
        source_actor_id = "actor.test",
        source_component_id = "component:test",
        ability_id = "ability.test.kinetic",
      })
      assert(result.applied and not result.destroyed and result.new_integrity == 1)
      assert(result.source_actor_id == "actor.test" and result.source_component_id == "component:test")
      assert(world:get_cell(4, 4).current_integrity == 1)
      assert(registry:get_material("material.terrain.brush").max_integrity == 2)
      assert(world:describe_cell(4, 4):find("material.terrain.brush", 1, true))
      assert(#world:mutation_data() == 1)
      assert(world:to_data().terrain == "forest")
      assert(world:validate())
    end,
  },
  {
    name = "weak terrain breaches and stops blocking movement and vision",
    run = function()
      local world = World.new(Registry.load(), "forest", {})
      local result = EnvironmentDamage.apply(world, 5, 5, {
        amount = 2,
        cause = "explosive",
        source = "test_bomb",
      })
      assert(result.applied and result.destroyed)
      assert(result.material_id == "material.terrain.brush")
      assert(result.destroyed_material_id == "material.terrain.air")
      local cell = assert(world:inspect_cell(5, 5))
      assert(cell.destroyed and cell.current_integrity == 0)
      assert(cell.passable and not cell.blocks_vision)
      assert(world:validate())
    end,
  },
  {
    name = "durable terrain accumulates deterministic explosive damage",
    run = function()
      local registry = Registry.load()
      local world = World.new(registry, "cave", {})
      local first = EnvironmentDamage.apply(world, 6, 6, { amount = 2, cause = "explosive", source = "first" })
      assert(first.applied and not first.destroyed and first.new_integrity == 2)
      assert(not world:is_passable(6, 6) and world:blocks_vision(6, 6))
      local second = EnvironmentDamage.apply(world, 6, 6, { amount = 2, cause = "explosive", source = "second" })
      assert(second.applied and second.destroyed)
      assert(world:is_passable(6, 6) and not world:blocks_vision(6, 6))

      local layout = Generator.generate("cave", Grid.cell(40, 25), Rng.new(2402))
      local first_generated = World.new(registry, "cave", layout)
      local second_generated = World.new(registry, "cave", Generator.generate("cave", Grid.cell(40, 25), Rng.new(2402)))
      assert(world_fingerprint(first_generated) == world_fingerprint(second_generated))
      assert(world:validate())
    end,
  },
  {
    name = "indestructible material rejects terrain damage without corrupting state",
    run = function()
      local world = World.new(Registry.load(), "arena", {})
      local result = EnvironmentDamage.apply(world, 7, 7, { amount = 99, cause = "explosive", source = "test" })
      assert(not result.applied and result.code == "indestructible")
      local cell = assert(world:inspect_cell(7, 7))
      assert(cell.material_id == "material.structure.reinforced")
      assert(cell.current_integrity == nil and not cell.passable and cell.blocks_vision)
      assert(world:validate())
    end,
  },
  {
    name = "bomb terrain damage uses material integrity while actor blast damage remains active",
    run = function()
      local session = new_session(2403)
      local state = session.state
      state.world = World.new(session.registry, "forest", open_everywhere_except({ { 10, 12 } }))
      state.player.x, state.player.y = 10, 10
      state.targets, state.effects, state.bullets, state.flares, state.area_attacks = {}, {}, {}, {}, {}
      state.enemies = { session:_make_enemy("bomber", Grid.cell(11, 10)) }
      state.bombs = { { kind = "bomb", x = 10, y = 10, radius = 2, fuse = 1 } }
      local player_health = state.player.health

      session:_update_bombs()

      assert(state.player.health == player_health - 1)
      assert(#state.enemies == 0 and #state.corpses == 1)
      assert(state.world:is_passable(10, 12))
      assert(state.world:get_material(10, 12).id == "material.terrain.air")
      assert(session:validate_world())
    end,
  },
  {
    name = "breached terrain immediately changes player movement pathfinding and visibility",
    run = function()
      local session = new_session(2404)
      local state = session.state
      state.world = World.new(session.registry, "forest", open_layout({ { 10, 10 }, { 12, 10 } }))
      state.player.x, state.player.y = 10, 10
      state.settings.vision = 4
      state.enemies, state.targets, state.effects, state.torches, state.bombs, state.flares, state.bullets = {}, {}, {}, {}, {}, {}, {}
      local enemy = session:_make_enemy("bomber", Grid.cell(10, 10))

      session:refresh_visibility()
      assert(not session:_has_line_of_sight(10, 10, 12, 10))
      assert(#session:_path(enemy, Grid.cell(12, 10)) == 0)
      assert(not session:can_move("d"))

      local breach = session:damage_terrain(11, 10, { amount = 2, cause = "explosive", source = "test" })
      assert(breach.applied and breach.destroyed)
      session:refresh_visibility()
      assert(session:_has_line_of_sight(10, 10, 12, 10))
      assert(#session:_path(enemy, Grid.cell(12, 10)) == 3)
      local moved = session:_move_player("d")
      assert(moved.applied and state.player.x == 11 and state.player.y == 10)
      assert(session:validate_world())
    end,
  },
}
