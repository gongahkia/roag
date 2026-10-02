-- Tranche 8F contracts. Reactor is deliberately an authored-room biome that
-- composes existing material, media, power, recurrence, and body systems.
local Content = require("src.content.legacy")
local Definitions = require("src.routes.definitions")
local Electricity = require("src.simulation.electricity")
local Grid = require("src.world.grid")
local InspectionFloor = require("src.generation.inspection_floor")
local Analysis = require("src.generation.analysis")
local Batch = require("src.generation.batch_analysis")
local Json = require("src.persistence.json")
local Graph = require("src.routes.graph")
local Recurrence = require("src.simulation.fallen_recurrence")
local RoomRegistry = require("src.rooms.registry")
local ReactorConfig = require("src.rooms.reactor_config")
local EditorModel = require("src.tools.room_editor_model")
local Session = require("src.simulation.session")
local World = require("src.world.world")

local REACTOR = "biome.legacy.reactor"
local TIER_TWO = "tier.legacy.2"
local TIER_THREE = "tier.legacy.3"
local MELEE = "ability.weapon.melee.basic"
local SHOCK = "ability.electrical.discharge"
local HEAVY_PROJECTILE = "ability.weapon.projectile.heavy"

local function encode(value)
  local text, failure = Json.encode(value)
  assert(text, failure)
  return text
end

local function report_for(floor)
  return Analysis.analyze(floor.world, {
    seed = floor.seed,
    biome_id = floor.biome_id,
    tier_id = floor.tier_id,
    terrain = floor.terrain,
    state = floor.state,
    session = floor.session,
    provenance = floor.provenance,
  })
end

local function key(x, y)
  return Grid.key(x, y)
end

return {
  {
    name = "Reactor registers as one biome across tiers two and three with deterministic authored-room assembly",
    run = function()
      local definitions = Definitions.load()
      local biome = definitions:get_biome(REACTOR)
      assert(biome.display_name == "REACTOR COMPLEX" and biome.room_corpus_id == ReactorConfig.CORPUS_ID)
      assert(definitions:biome_supports_tier(REACTOR, TIER_TWO))
      assert(definitions:biome_supports_tier(REACTOR, TIER_THREE))
      assert(not definitions:biome_supports_tier(REACTOR, "tier.legacy.1"))

      local first = assert(InspectionFloor.generate({ biome = REACTOR, tier = TIER_THREE, seed = 88601 }))
      local second = assert(InspectionFloor.generate({ biome = REACTOR, tier = TIER_THREE, seed = 88601 }))
      assert(first.terrain == "reactor")
      assert(encode(first.state.generation_metadata.rooms) == encode(second.state.generation_metadata.rooms))
      assert(encode(first.state.generation_metadata.material_layout) == encode(second.state.generation_metadata.material_layout))
      local report = report_for(first)
      assert(report.valid and report.metrics.room_count == ReactorConfig.ROOM_COUNT)
      assert((report.metrics.material_counts["material.floor.conductive_metal"] or 0) > 0)
      assert((report.metrics.material_counts["material.structure.industrial_bulkhead"] or 0) > 0)
    end,
  },
  {
    name = "Reactor composes finite coolant gas fire and two optional circuits without unsafe spawn placement",
    run = function()
      for _, tier in ipairs({ TIER_TWO, TIER_THREE }) do
        local floor = assert(InspectionFloor.generate({ biome = REACTOR, tier = tier, seed = 88610 + (tier == TIER_THREE and 1 or 0),
          service_id = "service.supply.legacy" }))
        local report = report_for(floor)
        local player = floor.state.player
        assert(report.valid, report.errors[1] and report.errors[1].code)
        assert(report.metrics.liquid_volume > 0 and report.metrics.gas_volume > 0)
        assert(report.metrics.active_fires == 1 and report.metrics.circuits == 2)
        assert(#floor.world:fires_at(player.x, player.y) == 0)
        assert(not floor.world:is_harmful_gas_at(player.x, player.y))
        local service
        for _, object in ipairs(floor.world:list_objects()) do
          if object.interaction_role == "service" then service = object end
        end
        assert(service and service.service_id == "service.supply.legacy")
        assert(floor.world:is_passable(service.x, service.y) and Grid.distance(player, service) >= 3)
      end
    end,
  },
  {
    name = "Reactor conductive metal uses the ordinary electricity query and an air interruption stops the network",
    run = function()
      local session = Session.new({ seed = 88620 })
      session:start_run(Content.classes[1], Content.boons[1])
      local layout = { [key(10, 10)] = true, [key(11, 10)] = true, [key(12, 10)] = true }
      local materials = {
        [key(10, 10)] = "material.floor.conductive_metal",
        [key(11, 10)] = "material.floor.conductive_metal",
        [key(12, 10)] = "material.terrain.air",
      }
      local world = World.new(session.registry, "reactor", layout, session.state, materials)
      assert(world:is_conductive_at(10, 10) and world:is_conductive_at(11, 10))
      assert(not world:is_conductive_at(12, 10))
      local trace = Electricity.trace(world, { x = 9, y = 10 })
      assert(trace.applied and trace.network_size == 2)
      assert(trace.reached_cells[1].x == 10 and trace.reached_cells[2].x == 11)
    end,
  },
  {
    name = "Reactor bodies expose only installed shared capabilities and breaking Arc Blade removes both of its providers",
    run = function()
      local floor = assert(InspectionFloor.generate({ biome = REACTOR, tier = TIER_THREE, seed = 88630 }))
      local session = floor.session
      local cutter = session:_make_enemy("enemy.reactor.arc_cutter", { x = 20, y = 20 })
      assert(session:actor_has_capability(cutter, MELEE) and session:actor_has_capability(cutter, SHOCK))
      assert(session:damage_actor_body(cutter, { amount = 5, slot_id = "left_arm", cause = "fixture" }).became_broken)
      assert(not session:actor_has_capability(cutter, MELEE) and not session:actor_has_capability(cutter, SHOCK))

      local warden = session:_make_enemy("enemy.elite.arc_warden", { x = 21, y = 20 })
      assert(session:actor_has_capability(warden, MELEE))
      assert(session:actor_has_capability(warden, SHOCK))
      assert(session:actor_has_capability(warden, HEAVY_PROJECTILE))
      assert(warden.body:get_component("torso_core").definition_id == "component.core.reactor_frame")
    end,
  },
  {
    name = "Reactor is a third-floor route choice, shares the editor corpus model, and supports isolated recurrence placement",
    run = function()
      local graph = Graph.new(88640, Definitions.load())
      local reactor
      for _, node_id in ipairs(graph.node_order) do
        local node = graph:node(node_id)
        if node.biome_id == REACTOR then reactor = node end
      end
      assert(reactor and reactor.type == "floor" and reactor.depth == 4 and reactor.tier_id == TIER_THREE)
      local normal = 0
      for _, node_id in ipairs(graph.node_order) do if graph:node(node_id).type == "floor" then normal = normal + 1 end end
      assert(normal == 7, "profile alternatives must not add a fourth floor to any path")

      local rooms = assert(RoomRegistry.load({ config = ReactorConfig }))
      assert(#rooms.order == 10 and rooms:coverage().valid)
      local editor = assert(EditorModel.new({ config = ReactorConfig }))
      assert(#editor:corpora() == 2 and #editor:list() == 10)
      assert(editor:open(editor:list()[1].id) and editor:validation().valid)

      local source = Session.new({ seed = 88641, run_id = "run:88641" })
      source:start_run()
      local record = {
        id = "fallen:88641", source_run_id = "run:88641", body = source.state.player.body:to_data(),
        metadata = { route_path = {}, charm_ids = {}, research_ids = {}, biome_id = REACTOR, route_depth = 3 },
      }
      local spec = assert(Recurrence.assign("run:88642", 88642, { record }))
      spec.mode, spec.target_depth = "corpse", 3
      local floor = assert(InspectionFloor.generate({ biome = REACTOR, tier = TIER_THREE, seed = 88642,
        fallen_recurrence = spec, recurrence_depth = 3 }))
      assert(floor.state.fallen_recurrence.spawned and #floor.state.corpses == 1)
      assert(not floor.world:is_harmful_gas_at(floor.state.corpses[1].x, floor.state.corpses[1].y))
      assert(#floor.world:fires_at(floor.state.corpses[1].x, floor.state.corpses[1].y) == 0)

      local batch = assert(Batch.run({ biome = REACTOR, tier = TIER_TWO, seed = 88650, count = 4 }))
      assert(batch.summary.failures == 0 and batch.summary.fire_floor_count == 4)
    end,
  },
}
