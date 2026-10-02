local Content = require("src.content.legacy")
local Grid = require("src.world.grid")
local World = require("src.world.world")
local Session = require("src.simulation.session")
local Component = require("src.body.component")
local ActiveRun = require("src.persistence.active_run")
local SaveStore = require("src.persistence.save_store")
local App = require("src.app.app")

local function new_run(seed)
  local session = Session.new({ seed = seed })
  session:start_run(Content.classes[1], Content.boons[1])
  return session
end

local function store_round_trip(session)
  local store = SaveStore.memory()
  assert(ActiveRun.save(session, store))
  return assert(ActiveRun.load(store)), store
end

local function open_cell(world, avoid)
  for x = 1, Grid.width - 2 do
    for y = 1, Grid.height - 2 do
      if world:is_passable(x, y) and not world:is_hazardous(x, y)
        and (not avoid or x ~= avoid.x or y ~= avoid.y) then
        return { x = x, y = y }
      end
    end
  end
  error("No open cell in fixture")
end

local function flammable_cell(world)
  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do
      local material = world:get_material(x, y)
      if material.flammable and not world:get_cell(x, y).destroyed then return { x = x, y = y } end
    end
  end
  error("No flammable cell in fixture")
end

local function all_open_layout()
  local layout = {}
  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do layout[Grid.key(x, y)] = true end
  end
  return layout
end

return {
  {
    name = "active-run JSON codec validates versioned envelopes and corrupt input",
    run = function()
      local session = new_run(9701)
      local encoded = assert(ActiveRun.encode_session(session))
      assert(encoded:find('"format":"roag.active_run"', 1, true))
      assert(assert(ActiveRun.decode_envelope(encoded)).version == 1)
      local _, invalid = ActiveRun.decode_envelope("{")
      assert(invalid.code == "invalid_json")
      local _, format = ActiveRun.decode_envelope('{"format":"other","version":1,"run":{}}')
      assert(format.code == "unsupported_format")
      local _, version = ActiveRun.decode_envelope('{"format":"roag.active_run","version":2,"run":{}}')
      assert(version.code == "unsupported_version")
    end,
  },
  {
    name = "active-run storage has one replaceable headless slot and retires cleanly",
    run = function()
      local store = SaveStore.memory()
      local _, missing = store:read()
      assert(missing.code == "missing_file" and not store:exists())
      assert(store:write("first")); assert(store:write("second")); assert(store:read() == "second")
      assert(ActiveRun.retire(store)); assert(not store:exists())
    end,
  },
  {
    name = "full active-run round trip preserves bodies inventory corpses effects and world layers",
    run = function()
      local session = new_run(9702)
      local player = session.state.player
      player.body:get_component("left_arm").current_integrity = 1
      local corpse_enemy = session:_make_enemy("bomber", { x = player.x + 1, y = player.y })
      session.state.enemies[#session.state.enemies + 1] = corpse_enemy
      session:_destroy_enemy(#session.state.enemies)
      assert(session:salvage_corpse_component(session.state.corpses[1].id, "internal_1").applied)
      -- The north edge of the richer forest spawn clearing may now contain a
      -- real landmark.  Shoot along the open west side of this fixed fixture
      -- so a live bullet, bomb, and flare all exercise serialization.
      session:turn("shoot_a")
      session:turn("b")
      session.state.player.flares = 1
      session:turn("f")
      local world = session.state.world
      local water = open_cell(world, player)
      assert(world:set_liquid(water.x, water.y, "liquid.water.legacy", 3).applied)
      assert(world:set_gas(water.x + 1, water.y, "gas.toxic.legacy", 3).applied)
      local fuel = flammable_cell(world)
      assert(session:ignite_terrain(fuel.x, fuel.y, { source = "test" }).applied)
      local before = assert(ActiveRun.encode_session(session))
      local store = SaveStore.memory()
      assert(ActiveRun.save(session, store))
      assert(ActiveRun.encode_session(session) == before)
      local restored = assert(ActiveRun.load(store))
      assert(ActiveRun.encode_session(restored) == before)
      assert(restored.state.player.body:get_component("left_arm").current_integrity == 1)
      assert(#restored.state.inventory.entries == 1 and #restored.state.corpses == 1)
      assert(#restored.state.bullets >= 1 and #restored.state.bombs >= 1 and #restored.state.flares >= 1)
      assert(restored.state.world:liquid_amount(water.x, water.y) == 3)
      assert(restored.state.world:gas_concentration(water.x + 1, water.y) == 3)
      assert(#restored.state.world:list_fires() == 1)
    end,
  },
  {
    name = "save resume continuation preserves RNG future effects and next physical identities",
    run = function()
      local control, saved = new_run(9703), new_run(9703)
      for _, action in ipairs({ "w", "b", "shoot_w", "d" }) do control:turn(action); saved:turn(action) end
      local restored = assert(store_round_trip(saved))
      for _, action in ipairs({ "w", "f", "shoot_d", "a", "b" }) do control:turn(action); restored:turn(action) end
      assert(ActiveRun.encode_session(control) == ActiveRun.encode_session(restored))
      local first = control.component_factory:create("component.arm.legacy_projectile_emitter")
      local second = restored.component_factory:create("component.arm.legacy_projectile_emitter")
      assert(first.id == second.id)
    end,
  },
  {
    name = "active fire liquid and gas continue in the same world-process order after restore",
    run = function()
      local control, saved = new_run(9704), new_run(9704)
      for _, session in ipairs({ control, saved }) do
        local world = session.state.world
        local fuel = flammable_cell(world)
        assert(session:ignite_terrain(fuel.x, fuel.y, { source = "test" }).applied)
        local cell = open_cell(world, session.state.player)
        assert(world:set_liquid(cell.x, cell.y, "liquid.water.legacy", 3).applied)
        assert(world:set_gas(cell.x + 1, cell.y, "gas.toxic.legacy", 4).applied)
      end
      saved = assert(store_round_trip(saved))
      control:turn("w"); saved:turn("w")
      assert(ActiveRun.encode_session(control) == ActiveRun.encode_session(saved))
    end,
  },
  {
    name = "reconstruction and rotated inventory resume without repacking physical parts",
    run = function()
      local session = new_run(9705)
      local player = session.state.player
      local enemy = session:_make_enemy("bomber", { x = player.x + 1, y = player.y })
      session.state.enemies[#session.state.enemies + 1] = enemy
      session:_destroy_enemy(#session.state.enemies)
      local corpse = session.state.corpses[1]
      local component_id = corpse.body:get_component("left_leg").id
      assert(session:salvage_corpse_component(corpse.id, "left_leg").applied)
      local entry = session.state.inventory:get(component_id)
      assert(session.state.inventory:rotate(component_id))
      assert(session:_complete_stage() == "reconstruction")
      local restored = assert(store_round_trip(session))
      local restored_entry = restored.state.inventory:get(component_id)
      assert(restored.state.phase == "reconstruction" and restored_entry.rotated == entry.rotated)
      assert(restored.state.inventory:validate() and restored:validate_physical_ownership())
      assert(restored:complete_reconstruction().next == "curse")
      local transitioned = assert(store_round_trip(restored))
      assert(transitioned.state.phase == "transition" and transitioned.state.transition_next == "curse")
    end,
  },
  {
    name = "world restoration preserves moved objects damaged terrain and logical door power state",
    run = function()
      local session = new_run(9706)
      local world = World.new(session.registry, "dungeon", all_open_layout(), session.state)
      session.state.world = world
      world.cells[Grid.key(14, 10)] = {
        material_id = "material.structure.masonry", current_integrity = 2, destroyed = false,
      }
      world.cells[Grid.key(15, 10)] = {
        material_id = "material.structure.masonry", current_integrity = 2, destroyed = false,
      }
      assert(world:register_circuit("circuit:save", { enabled = true }).applied)
      local breaker = assert(world:place_object("world_object.power.breaker_legacy", 9, 10, { circuit_id = "circuit:save" }))
      local generator = assert(world:place_object("world_object.power.generator_legacy", 10, 10, { circuit_id = "circuit:save", generator_online = true }))
      local door = assert(world:place_object("world_object.door.powered_legacy", 11, 10, { circuit_id = "circuit:save" }))
      local crate = assert(world:place_object("world_object.cover.timber_crate", 12, 10))
      assert(world:move_object(crate, 13, 10).applied)
      assert(world:damage_terrain(14, 10, { amount = 1, cause = "test" }).applied)
      assert(world:damage_terrain(15, 10, { amount = 2, cause = "test" }).destroyed)
      assert(world:set_door_state(door, "open").applied)
      assert(world:set_generator_online(generator, false).applied)
      local restored = assert(store_round_trip(session))
      local saved_world = restored.state.world
      assert(saved_world:get_object(generator.id).generator_online == false)
      assert(saved_world:get_object(breaker.id).circuit_id == "circuit:save")
      assert(saved_world:get_object(door.id).door_state == "open")
      assert(not saved_world:is_circuit_powered("circuit:save"))
      assert(saved_world:get_object(crate.id).x == 13)
      assert(saved_world:get_cell(14, 10).current_integrity == 1)
      assert(saved_world:is_passable(15, 10))
    end,
  },
  {
    name = "conductivity derives from restored liquid and objects with no transient discharge save state",
    run = function()
      local session = new_run(9707)
      local world = session.state.world
      local cell = open_cell(world, session.state.player)
      assert(world:set_liquid(cell.x, cell.y, "liquid.water.legacy", 1).applied)
      local restored = assert(store_round_trip(session))
      assert(restored.state.world:is_conductive_at(cell.x, cell.y))
      assert(#restored.state.electrical_effects == 0)
    end,
  },
  {
    name = "title Continue accepts only valid saves and death retires the active run",
    run = function()
      local store = SaveStore.memory()
      local app = App.new({ seed = 9708, save_store = store })
      assert(not app.continue_available)
      app:select_class(app.content.classes[1]); app:select_boon(app.boon_options[1])
      assert(app.continue_available)
      local resumed = App.new({ seed = 9709, save_store = store })
      assert(resumed.continue_available and resumed:continue_run())
      resumed.session.state.ended = "gameover"
      assert(resumed:autosave("death"))
      assert(not store:exists())
      local corrupt = SaveStore.memory("{")
      local invalid = App.new({ seed = 9710, save_store = corrupt })
      assert(not invalid.continue_available and invalid.title_error.code == "invalid_json")
    end,
  },
}
