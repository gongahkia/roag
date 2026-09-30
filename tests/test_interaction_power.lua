local Content = require("src.content.legacy")
local EnvironmentDamage = require("src.simulation.environment_damage")
local Gas = require("src.simulation.gas")
local Grid = require("src.world.grid")
local Interaction = require("src.simulation.interaction")
local Session = require("src.simulation.session")
local World = require("src.world.world")

local DOOR = "world_object.door.powered_legacy"
local GENERATOR = "world_object.power.generator_legacy"
local BREAKER = "world_object.power.breaker_legacy"
local TOXIC = "gas.toxic.legacy"

local function key(x, y)
  return Grid.key(x, y)
end

local function layout(points)
  local result = {}
  for _, point in ipairs(points) do
    result[key(point[1], point[2])] = true
  end
  return result
end

local function new_session(seed)
  local session = Session.new({ seed = seed or 7101 })
  session:start_run(Content.classes[1], Content.boons[1])
  return session
end

-- A narrow north/south gas corridor keeps the deterministic north tie-break
-- focused on the door. Generator and breaker are beside the player, not in
-- the diffusion path.
local function fixture(seed)
  local session = new_session(seed)
  local state = session.state
  state.world = World.new(session.registry, "cave", layout({
    { 9, 9 }, { 9, 10 }, { 9, 11 },
    { 10, 10 }, { 10, 11 }, { 10, 12 },
  }), state)
  state.enemies, state.targets, state.bullets, state.bombs = {}, {}, {}, {}
  state.flares, state.torches, state.area_attacks, state.effects = {}, {}, {}, {}
  state.ammo, state.exit, state.corpses = nil, nil, {}
  state.settings.score, state.settings.targets, state.settings.enemies, state.settings.torches = 99, 0, 0, 0
  state.player.x, state.player.y = 9, 10
  assert(state.world:register_circuit("power.circuit.test", { enabled = true }).applied)
  local generator = assert(state.world:place_object(GENERATOR, 9, 9, {
    circuit_id = "power.circuit.test", generator_online = true,
  }))
  local breaker = assert(state.world:place_object(BREAKER, 9, 11, {
    circuit_id = "power.circuit.test",
  }))
  local door = assert(state.world:place_object(DOOR, 10, 11, {
    circuit_id = "power.circuit.test", door_state = "closed",
  }))
  return session, generator, breaker, door
end

local function projectile(session, direction)
  local player = session.state.player
  session.state.bullets = {
    {
      kind = "bullet", x = player.x, y = player.y, direction = direction,
      active = true, travel = 1, max = 4, damage = 1,
      source_actor = player, source_side = "player", source_component_id = "component:test",
      ability_id = "ability.weapon.projectile.basic",
    },
  }
  session:_update_bullets()
end

local function object_snapshot(session)
  local values = {}
  for _, object in ipairs(session.state.world:list_objects(true)) do
    values[#values + 1] = table.concat({
      object.id, object.definition_id, object.x, object.y,
      object.interaction_role or "", object.circuit_id or "", object.door_state or "",
      tostring(object.generator_online), tostring(object.destroyed),
    }, ":")
  end
  return table.concat(values, "|")
end

return {
  {
    name = "adjacent interaction discovery uses stable world-object order and validates range",
    run = function()
      local session, generator, breaker, door = fixture(7102)
      local interactions = session:available_interactions()
      assert(#interactions == 3)
      assert(interactions[1].object_id == door.id and interactions[1].actions[1].id == "door.open")
      assert(interactions[2].object_id == generator.id and interactions[2].actions[1].id == "generator.toggle")
      assert(interactions[3].object_id == breaker.id and interactions[3].actions[1].id == "breaker.toggle")
      session.state.player.x, session.state.player.y = 1, 1
      assert(#session:available_interactions() == 0)
      assert(Interaction.perform(session, session.state.player, door.id, "door.open").code == "out_of_range")
    end,
  },
  {
    name = "closed and open powered doors update movement LOS projectiles gas and paths through world queries",
    run = function()
      local session, _, _, door = fixture(7103)
      local world = session.state.world
      assert(not world:is_passable(10, 11) and world:blocks_vision(10, 11)
        and world:blocks_projectile(10, 11) and not world:allows_gas_at(10, 11))
      assert(not session:_has_line_of_sight(10, 10, 10, 12))
      assert(#session:_path(Grid.cell(10, 10), Grid.cell(10, 12)) == 0)
      assert(session:interact(nil, door.id, "door.open").applied)
      assert(world:is_passable(10, 11) and not world:blocks_vision(10, 11)
        and not world:blocks_projectile(10, 11) and world:allows_gas_at(10, 11))
      assert(session:_has_line_of_sight(10, 10, 10, 12))
      assert(#session:_path(Grid.cell(10, 10), Grid.cell(10, 12)) == 3)
      assert(world:validate())
    end,
  },
  {
    name = "closed doors stop projectiles while open doors permit their path",
    run = function()
      local closed, _, _, door = fixture(7104)
      closed.state.player.x, closed.state.player.y = 10, 10
      projectile(closed, "w")
      assert(#closed.state.bullets == 0 and closed.state.world:get_object(door.id).current_integrity == 1)

      local opened, _, _, opened_door = fixture(7105)
      opened.state.player.x, opened.state.player.y = 9, 10
      assert(opened:interact(nil, opened_door.id, "door.open").applied)
      opened.state.player.x, opened.state.player.y = 10, 10
      projectile(opened, "w")
      assert(#opened.state.bullets == 1 and opened.state.bullets[1].x == 10 and opened.state.bullets[1].y == 11)
    end,
  },
  {
    name = "generator breaker and circuit state derive persistent powered status",
    run = function()
      local session, generator, breaker = fixture(7106)
      local world = session.state.world
      assert(world:is_circuit_powered("power.circuit.test"))
      assert(session:interact(nil, breaker.id, "breaker.toggle").applied)
      assert(not world:get_circuit("power.circuit.test").enabled and not world:is_circuit_powered("power.circuit.test"))
      assert(session:interact(nil, breaker.id, "breaker.toggle").applied and world:is_circuit_powered("power.circuit.test"))
      assert(session:interact(nil, generator.id, "generator.toggle").applied)
      assert(not generator.generator_online and not world:is_circuit_powered("power.circuit.test"))
      assert(session:interact(nil, generator.id, "generator.toggle").applied and world:is_circuit_powered("power.circuit.test"))
      local inspected = assert(world:inspect_circuit("power.circuit.test"))
      assert(inspected.powered and #inspected.source_ids == 1 and #inspected.consumer_ids == 1)
    end,
  },
  {
    name = "powered doors require an enabled online source and open state survives power loss",
    run = function()
      local session, _, breaker, door = fixture(7107)
      local world = session.state.world
      assert(session:interact(nil, breaker.id, "breaker.toggle").applied)
      local rejected = session:interact(nil, door.id, "door.open")
      assert(not rejected.applied and rejected.code == "requires_power" and door.door_state == "closed")
      assert(session:interact(nil, breaker.id, "breaker.toggle").applied)
      assert(session:interact(nil, door.id, "door.open").applied and door.door_state == "open")
      assert(session:interact(nil, breaker.id, "breaker.toggle").applied)
      assert(door.door_state == "open" and world:is_passable(door.x, door.y))
      local close = session:interact(nil, door.id, "door.close")
      assert(not close.applied and close.code == "requires_power")
      assert(session:interact(nil, breaker.id, "breaker.toggle").applied)
      assert(session:interact(nil, door.id, "door.close").applied and door.door_state == "closed")
    end,
  },
  {
    name = "door closing rejects occupied doorway without crushing actors",
    run = function()
      local session, _, _, door = fixture(7108)
      assert(session:interact(nil, door.id, "door.open").applied)
      local enemy = session:_make_enemy("bomber", { x = door.x, y = door.y })
      session.state.enemies = { enemy }
      local close = session:interact(nil, door.id, "door.close")
      assert(not close.applied and close.code == "occupied" and door.door_state == "open")
      assert(enemy.x == door.x and enemy.y == door.y)
    end,
  },
  {
    name = "door closing refuses to delete finite gas trapped in an open doorway",
    run = function()
      local session, _, _, door = fixture(71081)
      local world = session.state.world
      assert(session:interact(nil, door.id, "door.open").applied)
      assert(world:set_gas(door.x, door.y, TOXIC, 1).applied)
      local close = session:interact(nil, door.id, "door.close")
      assert(not close.applied and close.code == "gas_occupied" and door.door_state == "open")
      assert(world:gas_concentration(door.x, door.y) == 1 and world:validate())
    end,
  },
  {
    name = "generator and breaker destruction immediately leaves the circuit unpowered",
    run = function()
      local session, generator, breaker = fixture(7109)
      local world = session.state.world
      assert(EnvironmentDamage.apply_to_object(world, generator, { amount = 2, cause = "kinetic" }).destroyed)
      assert(generator.destroyed and not world:is_circuit_powered("power.circuit.test"))

      local second, _, second_breaker = fixture(7110)
      assert(EnvironmentDamage.apply_to_object(second.state.world, second_breaker, { amount = 2, cause = "kinetic" }).destroyed)
      assert(not second.state.world:get_circuit("power.circuit.test").enabled
        and not second.state.world:is_circuit_powered("power.circuit.test"))
    end,
  },
  {
    name = "destroying an unpowered door bypasses interaction power and opens every physical query",
    run = function()
      local session, _, breaker, door = fixture(7111)
      local world = session.state.world
      assert(session:interact(nil, breaker.id, "breaker.toggle").applied)
      assert(not session:interact(nil, door.id, "door.open").applied)
      local destroyed = EnvironmentDamage.apply_to_object(world, door, { amount = 2, cause = "kinetic" })
      assert(destroyed.destroyed and door.door_state == "destroyed")
      assert(world:is_passable(10, 11) and not world:blocks_vision(10, 11)
        and not world:blocks_projectile(10, 11) and world:allows_gas_at(10, 11))
      assert(world:validate())
    end,
  },
  {
    name = "closed doors contain gas until a later tick after opening or destruction",
    run = function()
      local session, _, _, door = fixture(7112)
      local world = session.state.world
      assert(world:set_gas(10, 10, TOXIC, 3).applied)
      Gas.tick(world)
      assert(world:gas_concentration(10, 12) == 0)
      assert(session:interact(nil, door.id, "door.open").applied)
      assert(world:gas_concentration(10, 11) == 0, "opening itself does not move gas")
      assert(Gas.tick(world).applied and world:gas_concentration(10, 11) == 1)

      local second, _, _, second_door = fixture(7113)
      local second_world = second.state.world
      assert(second_world:set_gas(10, 10, TOXIC, 3).applied)
      assert(EnvironmentDamage.apply_to_object(second_world, second_door, { amount = 2, cause = "kinetic" }).destroyed)
      assert(second_world:gas_concentration(10, 11) == 0)
      assert(Gas.tick(second_world).applied and second_world:gas_concentration(10, 11) == 1)
    end,
  },
  {
    name = "interaction actions consume a normal turn and run post-action world processing",
    run = function()
      local session, _, _, door = fixture(7114)
      local world = session.state.world
      assert(world:set_gas(10, 10, TOXIC, 3).applied)
      assert(session:turn("interact") == nil)
      assert(door.door_state == "open" and world:gas_concentration(10, 11) == 1)
    end,
  },
  {
    name = "logical power has no transient electrical activation path",
    run = function()
      local session, _, breaker, door = fixture(7115)
      local world = session.state.world
      assert(session:interact(nil, breaker.id, "breaker.toggle").applied)
      assert(not world:is_circuit_powered("power.circuit.test"))
      -- Persistent circuit power is only derived from breaker state and online
      -- generators; no transient-electrical API can mutate this state.
      assert(not session:interact(nil, door.id, "door.open").applied)
      assert(world:inspect_circuit("power.circuit.test").powered == false)
    end,
  },
  {
    name = "world validation rejects malformed device circuit state and references",
    run = function()
      local session, _, _, door = fixture(71151)
      local world = session.state.world
      door.circuit_id = "power.circuit.missing"
      assert(not pcall(function() world:validate() end))
      door.circuit_id = "power.circuit.test"
      world:get_circuit("power.circuit.test").enabled = "yes"
      assert(not pcall(function() world:validate() end))
      world:get_circuit("power.circuit.test").enabled = true
      assert(world:validate())
    end,
  },
  {
    name = "dungeon powered device placement and plain data reproduce without changing IDs",
    run = function()
      local first, second = new_session(7116), new_session(7116)
      first.state.stage, second.state.stage = 3, 3
      first:start_stage()
      second:start_stage()
      assert(#first.state.world:list_circuits() == 1 and #second.state.world:list_circuits() == 1)
      assert(object_snapshot(first) == object_snapshot(second))
      local data = first.state.world:to_data()
      assert(#data.circuits == 1 and data.circuits[1].id == "power.circuit.stage_dungeon_maintenance")
      local found_door, found_generator, found_breaker = false, false, false
      for _, object in ipairs(data.objects) do
        if object.definition_id == DOOR then
          found_door = object.door_state == "closed" and object.circuit_id == data.circuits[1].id
        elseif object.definition_id == GENERATOR then
          found_generator = object.generator_online == true and object.circuit_id == data.circuits[1].id
        elseif object.definition_id == BREAKER then
          found_breaker = object.circuit_id == data.circuits[1].id
        end
      end
      assert(found_door and found_generator and found_breaker and first:validate_world())
    end,
  },
}
