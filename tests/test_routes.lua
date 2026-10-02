local Content = require("src.content.legacy")
local Json = require("src.persistence.json")
local ActiveRun = require("src.persistence.active_run")
local SaveStore = require("src.persistence.save_store")
local App = require("src.app.app")
local Session = require("src.simulation.session")
local Definitions = require("src.routes.definitions")
local Graph = require("src.routes.graph")
local RouteAnalysis = require("src.routes.analysis")
local InspectionFloor = require("src.generation.inspection_floor")
local GenerationAnalysis = require("src.generation.analysis")

local function definitions()
  return Definitions.load()
end

local function new_run(seed)
  local session = Session.new({ seed = seed or 88001 })
  session:start_run(Content.classes[1], Content.boons[1])
  return session
end

local function encode(value)
  local text, reason = Json.encode(value)
  assert(text, reason)
  return text
end

local function finish_to_route(session)
  assert(session:_complete_stage() == "reconstruction")
  assert(session:complete_reconstruction().next == "curse")
  local result = session:choose_curse(session.state.curse_options[1])
  assert(result.next == "route" and session.state.phase == "route")
  return session:available_route_nodes()
end

local function choose(session, predicate)
  for _, node in ipairs(session:available_route_nodes()) do
    if predicate(node) then
      local result = session:select_route_node(node.id)
      assert(result.applied)
      return result
    end
  end
  error("Expected route node was unavailable")
end

return {
  {
    name = "route content validates semantic biomes tiers and a production profile",
    run = function()
      local content = definitions()
      assert(content:get_biome("biome.legacy.forest").terrain == "forest")
      assert(content:get_tier("tier.legacy.3").number == 3)
      assert(content:get_profile("route_profile.legacy.base").layers[1].nodes[1].biome_id == "biome.legacy.forest")
      local bad_biomes = { { id = "biome.legacy.test", display_name = "TEST", generator = "test", terrain = "test", enemy_family = "wilds" } }
      local bad_tiers = { { id = "tier.legacy.1", number = 1, settings = { targets = 1, enemies = 1, score = 1, ammo = 1, vision = 1, torches = 1 } } }
      local bad_profiles = { { id = "route_profile.legacy.bad", display_name = "BAD", layers = {
        { type = "floor", nodes = { { key = "start", biome_id = "biome.legacy.missing", tier_id = "tier.legacy.1" } } },
        { type = "shop", nodes = { { key = "shop" } } }, { type = "boss", nodes = { { key = "boss" } } },
      } } }
      local ok, err = pcall(function() Definitions.new({ biomes = bad_biomes, tiers = bad_tiers, profiles = bad_profiles }) end)
      assert(not ok and tostring(err):find("Unknown biome ID", 1, true))
    end,
  },
  {
    name = "route graph is a deterministic forward DAG with real branch choices",
    run = function()
      local content = definitions()
      local first, second = Graph.new(88002, content), Graph.new(88002, content)
      assert(encode(first:to_data()) == encode(second:to_data()))
      local report = RouteAnalysis.analyze(first, content)
      assert(report.valid and report.metrics.node_count == 11 and report.metrics.edge_count == 17)
      assert(report.metrics.branch_count >= 1 and report.metrics.convergence_count >= 1)
      assert(first:node(first.start_node_id).biome_id == "biome.legacy.forest")
      assert(first:node(first.start_node_id).tier_id == "tier.legacy.1")
    end,
  },
  {
    name = "route creation uses no session RNG and floor seeds exist before entry",
    run = function()
      local session = Session.new({ seed = 88003 })
      local before = encode(session.rng:to_data())
      local graph = Graph.new(session.seed, session.route_definitions)
      assert(encode(session.rng:to_data()) == before)
      for _, id in ipairs(graph.node_order) do
        local node = graph:node(id)
        if node.type == "floor" then assert(type(node.floor_seed) == "number") end
      end
    end,
  },
  {
    name = "route selection only permits a completed current node and a forward edge once",
    run = function()
      local graph = Graph.new(88004, definitions())
      local boss
      for _, id in ipairs(graph.node_order) do if graph:node(id).type == "boss" then boss = id end end
      local blocked, blocked_failure = graph:select(boss)
      assert(not blocked and blocked_failure.code == "current_incomplete")
      assert(graph:complete_current().applied)
      local invalid, invalid_failure = graph:select(boss)
      assert(not invalid and invalid_failure.code == "not_connected")
      local selected = graph:select(graph:available()[1].id)
      assert(selected.applied and graph.current_node_id == selected.node.id)
      local duplicate, duplicate_failure = graph:select(selected.node.id)
      assert(not duplicate and duplicate_failure.code == "current_incomplete")
    end,
  },
  {
    name = "opening completion preserves reconstruction curse then explicit biome route choice",
    run = function()
      local session = new_run(88005)
      assert(session.state.settings.biome_id == "biome.legacy.forest" and session.state.settings.tier_id == "tier.legacy.1")
      local choices = finish_to_route(session)
      assert(#choices == 2 and choices[1].tier_id == "tier.legacy.2" and choices[2].tier_id == "tier.legacy.2")
      local cave = choose(session, function(node) return node.biome_id == "biome.legacy.cave" end)
      assert(cave.node.biome_id == "biome.legacy.cave")
      assert(session.state.phase == "combat" and session.state.settings.terrain == "cave" and session.state.settings.tier == 2)
      assert(session.state.floor_seed == cave.node.floor_seed)
    end,
  },
  {
    name = "tier-two branch forces its physical milestone before tier-three choice and final hub",
    run = function()
      local session = new_run(88006)
      finish_to_route(session)
      choose(session, function(node) return node.biome_id == "biome.legacy.cave" end)
      assert(session:_complete_stage() == "reconstruction")
      assert(session:complete_reconstruction().next == "boss")
      assert(session.state.boss.boss_id == "boss.cave.flooded_conductor")
      session.state.boss.health = 1
      assert(session:_damage_boss(1).dead)
      assert(session.state.phase == "boss_exit" and #session.state.corpses == 1)
      session.state.player.x, session.state.player.y = session.state.exit.x, session.state.exit.y
      assert(session:turn("") == "reconstruction")
      assert(session:complete_reconstruction().next == "curse")
      local curse = session:choose_curse(session.state.curse_options[1])
      assert(curse.next == "route")
      local choices = session:available_route_nodes()
      assert(#choices == 3)
      choose(session, function(node) return node.biome_id == "biome.legacy.dungeon" end)
      assert(session.state.settings.terrain == "dungeon" and session.state.settings.tier == 3)
      assert(session:_complete_stage() == "reconstruction")
      assert(session:complete_reconstruction().next == "shop")
      assert(session.state.route:node(session.state.route.current_node_id).type == "shop")
      session:start_boss()
      assert(session.state.phase == "boss" and session.state.route:node(session.state.route.current_node_id).type == "boss")
    end,
  },
  {
    name = "biome remains semantic while tier changes numerical progression",
    run = function()
      local forest = assert(InspectionFloor.generate({ biome = "biome.legacy.forest", tier = "tier.legacy.2", seed = 88007 }))
      local cave = assert(InspectionFloor.generate({ biome = "biome.legacy.cave", tier = "tier.legacy.3", seed = 88007 }))
      assert(forest.terrain == "forest" and forest.state.settings.tier == 2 and forest.state.settings.wilds)
      assert(cave.terrain == "cave" and cave.state.settings.tier == 3 and cave.state.settings.cultists)
      assert(not forest.state.settings.cultists and not cave.state.settings.wilds)
      local reactor = assert(InspectionFloor.generate({ biome = "biome.legacy.reactor", tier = "tier.legacy.3", seed = 88007 }))
      assert(reactor.terrain == "reactor" and reactor.state.settings.tier == 3 and reactor.state.settings.industrial)
    end,
  },
  {
    name = "node floor generation is reproducible independent of other floor construction",
    run = function()
      local graph = Graph.new(88008, definitions())
      local dungeon
      for _, id in ipairs(graph.node_order) do
        local node = graph:node(id)
        if node.biome_id == "biome.legacy.dungeon" then dungeon = node end
      end
      local function report()
        local floor = assert(InspectionFloor.generate({ biome = dungeon.biome_id, tier = dungeon.tier_id, seed = dungeon.floor_seed }))
        return GenerationAnalysis.analyze(floor.world, { seed = floor.seed, state = floor.state, session = floor.session, provenance = floor.provenance })
      end
      local first = encode(report())
      assert(InspectionFloor.generate({ biome = "biome.legacy.forest", tier = "tier.legacy.1", seed = 1 }))
      assert(first == encode(report()))
    end,
  },
  {
    name = "route graph and pending route choice survive active-run save resume",
    run = function()
      local session = new_run(88009)
      finish_to_route(session)
      local store = SaveStore.memory()
      assert(ActiveRun.save(session, store))
      local restored = assert(ActiveRun.load(store))
      assert(restored.state.phase == "route")
      assert(encode(restored.state.route:to_data()) == encode(session.state.route:to_data()))
      local selected = restored:select_route_node(restored:available_route_nodes()[2].id)
      assert(selected.applied and restored.state.phase == "combat")
      local resumed = assert(ActiveRun.decode_session(assert(ActiveRun.encode_session(restored))))
      assert(resumed.state.route.current_node_id == restored.state.route.current_node_id)
      assert(resumed.state.floor_seed == restored.state.floor_seed)
    end,
  },
  {
    name = "legacy v1 save without route state restores its exact world and attaches canonical route context",
    run = function()
      local session = new_run(88010)
      local data = session:to_data()
      data.route = nil
      local world_before = encode(data.world)
      local restored = Session.from_data(data)
      assert(encode(restored.state.world:to_data()) == world_before)
      assert(restored.state.route and restored.state.route:node(restored.state.route.current_node_id).key == "opening_forest")
      assert(restored.state.phase == "combat")
    end,
  },
  {
    name = "legacy cave and dungeon saves retain their current world while migration attaches route context",
    run = function()
      for _, stage in ipairs({ 2, 3 }) do
        local session = Session.new({ seed = 88100 + stage })
        session.state.class, session.state.boon = Content.classes[1], Content.boons[1]
        session.state.stage = stage
        session:start_stage()
        local data = session:to_data()
        data.route = nil
        local world_before = encode(data.world)
        local restored = Session.from_data(data)
        assert(encode(restored.state.world:to_data()) == world_before)
        assert(restored.state.phase == "combat")
        local current = restored.state.route:node(restored.state.route.current_node_id)
        assert(current.type == "floor" and current.tier_id == "tier.legacy." .. stage)
      end
    end,
  },
  {
    name = "Continue restores a pending route-selection screen without choosing a branch",
    run = function()
      local session = new_run(88011)
      finish_to_route(session)
      local store = SaveStore.memory()
      assert(ActiveRun.save(session, store))
      local app = App.new({ seed = 88012, save_store = store })
      assert(app.continue_available and app:continue_run())
      assert(app.screen == "route" and #app:route_options() == 2)
    end,
  },
  {
    name = "headless route batch is reproducible and reports concrete failure-free seeds",
    run = function()
      local content = definitions()
      local first = assert(RouteAnalysis.batch({ definitions = content, seed = 88013, count = 40 }))
      local second = assert(RouteAnalysis.batch({ definitions = content, seed = 88013, count = 40 }))
      assert(first.summary.failures == 0 and first.summary.generated == 40)
      assert(encode(first) == encode(second))
      assert(first.summary.biome_by_depth[2]["biome.legacy.forest"] == 40)
      assert(first.summary.biome_by_depth[2]["biome.legacy.cave"] == 40)
      assert(first.summary.biome_by_depth[4]["biome.legacy.reactor"] == 40)
    end,
  },
}
