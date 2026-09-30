local Content = require("src.content.legacy")
local Grid = require("src.world.grid")
local Session = require("src.simulation.session")
local World = require("src.world.world")

local BARRICADE = "world_object.cover.masonry_barricade"
local CRATE = "world_object.cover.timber_crate"

local function key(x, y)
  return Grid.key(x, y)
end

local function layout(open_cells)
  local result = {}
  for _, point in ipairs(open_cells or {}) do
    result[key(point[1], point[2])] = true
  end
  return result
end

local function open_world(registry, sequence_owner, closed)
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
  return World.new(registry, "forest", result, sequence_owner)
end

local function new_session(seed)
  local session = Session.new({ seed = seed or 3101 })
  session:start_run(Content.classes[1], Content.boons[1])
  return session
end

local function prepare_open_session(seed, closed)
  local session = new_session(seed)
  local state = session.state
  state.world = open_world(session.registry, state, closed)
  state.enemies, state.targets, state.bullets, state.bombs = {}, {}, {}, {}
  state.flares, state.torches, state.area_attacks, state.effects = {}, {}, {}, {}
  state.ammo = nil
  return session
end

local function object_snapshot(session)
  local result = {}
  for _, object in ipairs(session.state.world:list_objects(true)) do
    result[#result + 1] = table.concat({
      object.id,
      object.definition_id,
      object.material_id,
      object.x,
      object.y,
      object.current_integrity,
      tostring(object.destroyed),
    }, ":")
  end
  return table.concat(result, "|")
end

return {
  {
    name = "world objects have deterministic physical identity material-backed integrity and serializable state",
    run = function()
      local owner = { next_world_object_sequence = 1 }
      local session = new_session(3102)
      local world = open_world(session.registry, owner)
      local barricade = assert(world:place_object(BARRICADE, 6, 6))
      assert(barricade.id == "world_object:000001")
      assert(barricade.material_id == "material.structure.masonry" and barricade.current_integrity == 2)
      assert(not world:is_passable(6, 6) and world:blocks_vision(6, 6) and world:blocks_projectile(6, 6))
      local inspected = assert(world:inspect_cell(6, 6))
      assert(inspected.objects[1].id == barricade.id and inspected.blocks_projectile)
      assert(world:to_data().objects[1].id == barricade.id)
      local _, duplicate = world:place_object(CRATE, 7, 6, { id = barricade.id })
      assert(duplicate.code == "duplicate_id")
      local _, out_of_bounds = world:place_object(CRATE, -1, 6)
      assert(out_of_bounds.code == "out_of_bounds")
      barricade.current_integrity = 3
      assert(not pcall(function() world:validate() end))
      barricade.current_integrity = 2
      assert(world:validate())
    end,
  },
  {
    name = "force resolves stepwise against terrain without voluntary locomotion",
    run = function()
      local session = prepare_open_session(3103, { { 8, 5 } })
      local player = session.state.player
      player.x, player.y = 5, 5
      player.dash = 2
      local result = session:apply_force(player, { dx = 9, dy = 0, distance = 3, cause = "test" })
      assert(result.applied and result.moved_distance == 2 and result.code == "blocked_world")
      assert(#result.path == 2 and result.path[1].x == 6 and result.path[2].x == 7)
      assert(player.x == 7 and player.y == 5)
      assert(player.dash == 2)

      local blocked = session:apply_force(player, { dx = 1, dy = 0, distance = 1, cause = "test" })
      assert(not blocked.applied and blocked.code == "blocked_world" and player.x == 7)

      player.x, player.y = 5, 6
      local blocker = session:_make_enemy("bomber", { x = 6, y = 6 })
      session.state.enemies = { blocker }
      local actor_blocked = session:apply_force(player, { dx = 1, dy = 0, distance = 1, cause = "test" })
      assert(not actor_blocked.applied and actor_blocked.code == "blocked_actor")
      assert(player.x == 5 and player.y == 6)
      assert(session:validate_world())
    end,
  },
  {
    name = "explosion knockback displaces a crawling player without locomotion permission",
    run = function()
      local session = prepare_open_session(3104)
      local player = session.state.player
      player.x, player.y = 10, 10
      assert(session:damage_actor_body(player, { amount = 3, slot_id = "left_leg", cause = "test" }).became_broken)
      assert(session:damage_actor_body(player, { amount = 3, slot_id = "right_leg", cause = "test" }).became_broken)
      assert(session:locomotion_state(player).state == "CRAWLING")
      session.state.bombs = { { kind = "bomb", x = 9, y = 10, radius = 1, fuse = 1, source_actor_id = "actor.test" } }
      local health = player.health
      session:_update_bombs()
      assert(player.health == health - 1)
      assert(player.x == 11 and player.y == 10)
      assert(session:locomotion_state(player).state == "CRAWLING")
    end,
  },
  {
    name = "movable crate force updates old and new world occupancy atomically",
    run = function()
      local session = prepare_open_session(3105)
      local world = session.state.world
      local crate = assert(world:place_object(CRATE, 5, 5))
      local moved = session:apply_force(crate, { dx = 1, dy = 0, distance = 1, cause = "test" })
      assert(moved.applied and crate.x == 6 and crate.y == 5)
      assert(world:is_passable(5, 5) and not world:is_passable(6, 5))
      local data = world:to_data().objects[1]
      assert(data.id == crate.id and data.x == 6 and data.y == 5)
      assert(world:validate())
    end,
  },
  {
    name = "movable crate force stops at terrain and fixed cover refuses displacement",
    run = function()
      local session = prepare_open_session(3106, { { 8, 5 } })
      local world = session.state.world
      local crate = assert(world:place_object(CRATE, 7, 5))
      local blocked = session:apply_force(crate, { dx = 1, dy = 0, distance = 2, cause = "test" })
      assert(not blocked.applied and blocked.code == "blocked_world")
      assert(crate.x == 7 and crate.y == 5)
      local barricade = assert(world:place_object(BARRICADE, 5, 5))
      local fixed = session:apply_force(barricade, { dx = 1, dy = 0, distance = 1, cause = "test" })
      assert(not fixed.applied and fixed.code == "immovable" and barricade.x == 5)
      assert(world:validate())
    end,
  },
  {
    name = "projectiles damage physical cover and terminate before actors behind it",
    run = function()
      local session = prepare_open_session(3107)
      local state, player = session.state, session.state.player
      player.x, player.y = 10, 10
      local cover = assert(state.world:place_object(BARRICADE, 10, 11))
      local enemy = session:_make_enemy("bomber", { x = 10, y = 12 })
      state.enemies = { enemy }
      local function shoot_cover()
        state.bullets = {
          {
            kind = "bullet", x = player.x, y = player.y, direction = "w", active = true, travel = 1,
            source_actor = player, source_side = "player", source_component_id = "component:test",
            ability_id = "ability.weapon.projectile.basic", damage = 1,
          },
        }
        session:_update_bullets()
      end

      shoot_cover()
      assert(#state.bullets == 0 and cover.current_integrity == 1 and not cover.destroyed)
      assert(#state.enemies == 1 and enemy.health == 1)
      shoot_cover()
      assert(cover.destroyed and state.world:is_passable(10, 11))
      assert(session.registry:get_material("material.structure.masonry").max_integrity == 2)
      assert(#state.bullets == 0 and #state.enemies == 1)
      shoot_cover()
      session:_update_bullets()
      assert(#state.enemies == 0 and #state.corpses == 1)
    end,
  },
  {
    name = "bomb destroys fixed cover and can push surviving movable cover through shared systems",
    run = function()
      local session = prepare_open_session(3108)
      local state, player = session.state, session.state.player
      player.x, player.y = 10, 10
      local barricade = assert(state.world:place_object(BARRICADE, 11, 10))
      local enemy = session:_make_enemy("bomber", { x = 10, y = 11 })
      state.enemies = { enemy }
      state.bombs = { { kind = "bomb", x = 10, y = 10, radius = 1, fuse = 1 } }
      session:_update_bombs()
      assert(barricade.destroyed and state.world:is_passable(11, 10))
      assert(#state.enemies == 0 and #state.corpses == 1)
      assert(session:can_move("d"))

      local second = prepare_open_session(3109)
      second.state.player.x, second.state.player.y = 10, 10
      local crate = assert(second.state.world:place_object(CRATE, 11, 10))
      second.state.bombs = { { kind = "bomb", x = 10, y = 10, radius = 1, fuse = 1 } }
      second:_update_bombs()
      assert(not crate.destroyed and crate.current_integrity == 1)
      assert(crate.x == 12 and crate.y == 10)
      assert(second.state.world:is_passable(11, 10) and not second.state.world:is_passable(12, 10))
    end,
  },
  {
    name = "cover movement immediately updates line of sight and pathfinding",
    run = function()
      local session = new_session(3110)
      local state = session.state
      state.world = World.new(session.registry, "forest", layout({ { 10, 10 }, { 11, 10 }, { 12, 10 }, { 11, 11 } }), state)
      state.player.x, state.player.y = 10, 10
      state.settings.vision = 4
      state.enemies, state.targets, state.effects, state.torches, state.bombs, state.flares, state.bullets = {}, {}, {}, {}, {}, {}, {}
      local crate = assert(state.world:place_object(CRATE, 11, 10))
      local enemy = session:_make_enemy("bomber", { x = 10, y = 10 })

      session:refresh_visibility()
      assert(not state.visible[key(12, 10)])
      assert(#session:_path(enemy, Grid.cell(12, 10)) == 0)
      assert(not session:can_move("d"))

      assert(session:apply_force(crate, { dx = 0, dy = 1, distance = 1, cause = "test" }).applied)
      session:refresh_visibility()
      assert(state.visible[key(12, 10)])
      assert(#session:_path(enemy, Grid.cell(12, 10)) == 3)
      assert(session:can_move("d"))
      assert(session:validate_world())
    end,
  },
  {
    name = "generated cover placement and force outcomes reproduce from the same seed",
    run = function()
      local first, second = new_session(3111), new_session(3111)
      assert(#first.state.world:list_objects() > 0)
      assert(object_snapshot(first) == object_snapshot(second))

      local one, two = prepare_open_session(3112), prepare_open_session(3112)
      one.state.player.x, one.state.player.y = 5, 5
      two.state.player.x, two.state.player.y = 5, 5
      local one_result = one:apply_force(one.state.player, { dx = -3, dy = 2, distance = 3, cause = "test" })
      local two_result = two:apply_force(two.state.player, { dx = -3, dy = 2, distance = 3, cause = "test" })
      assert(one.state.player.x == two.state.player.x and one.state.player.y == two.state.player.y)
      assert(one_result.moved_distance == two_result.moved_distance)
      for index, point in ipairs(one_result.path) do
        assert(point.x == two_result.path[index].x and point.y == two_result.path[index].y)
      end
    end,
  },
}
