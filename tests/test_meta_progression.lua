local Registry = require("src.content.registry")
local MetaProfile = require("src.persistence.meta_profile")
local SaveStore = require("src.persistence.save_store")
local ActiveRun = require("src.persistence.active_run")
local Session = require("src.simulation.session")
local App = require("src.app.app")
local Grid = require("src.world.grid")
local RouteGraph = require("src.routes.graph")
local RouteDefinitions = require("src.routes.definitions")
local RouteAnalysis = require("src.routes.analysis")
local Json = require("src.persistence.json")

local function profile_with(ids, data)
  return { research_data = data or 0, unlocked_research_ids = ids or {}, next_run_sequence = 1, claimed_reward_ids = {} }
end

local function new_session(seed, profile, handler)
  local registry = Registry.load()
  local session = Session.new({ seed = seed or 99001, registry = registry, run_id = "run:000099",
    meta_snapshot = MetaProfile.snapshot(profile or MetaProfile.new(), registry), on_meta_reward = handler })
  session:start_run()
  return session, registry
end

local function adjacent_open(session)
  for _, point in ipairs(Grid.neighbours(session.state.player)) do
    if session.state.world:is_passable(point.x, point.y) and not session.state.world:object_at(point.x, point.y) then return point end
  end
  error("Expected an adjacent open cell")
end

return {
  {
    name = "versioned meta profiles round trip independently and reject malformed state",
    run = function()
      local registry, store = Registry.load(), SaveStore.memory()
      local profile = profile_with({ "research.body.hardened_frame_i" }, 7)
      profile.next_run_sequence, profile.claimed_reward_ids = 4, { "run:000003:route_node:000002" }
      assert(MetaProfile.save(profile, store, registry))
      local restored = assert(MetaProfile.load(store, registry))
      assert(restored.research_data == 7 and restored.next_run_sequence == 4)
      assert(restored.unlocked_research_ids[1] == "research.body.hardened_frame_i")
      local invalid = SaveStore.memory('{"format":"roag.meta_profile","version":2,"profile":{}}')
      local loaded, failure = MetaProfile.load(invalid, registry)
      assert(not loaded and failure.code == "unsupported_version")
      local fresh = assert(MetaProfile.load(SaveStore.memory(), registry))
      assert(fresh.research_data == 0 and fresh.next_run_sequence == 1)
    end,
  },
  {
    name = "research purchase validates prerequisites and commits profile atomically",
    run = function()
      local registry, profile = Registry.load(), MetaProfile.new()
      profile.research_data = 10
      local missing, missing_error = MetaProfile.purchase(profile, registry, "research.body.hardened_frame_ii")
      assert(not missing and missing_error.code == "missing_prerequisite")
      local first = assert(MetaProfile.purchase(profile, registry, "research.body.hardened_frame_i"))
      assert(first.applied and profile.research_data == 9)
      local duplicate, duplicate_error = MetaProfile.purchase(profile, registry, "research.body.hardened_frame_i")
      assert(not duplicate and duplicate_error.code == "already_unlocked")
      local second = assert(MetaProfile.purchase(profile, registry, "research.body.hardened_frame_ii"))
      assert(second.applied)
      profile.research_data = 0
      local poor, poor_error = MetaProfile.purchase(profile, registry, "research.body.hardened_frame_iii")
      assert(not poor and poor_error.code == "insufficient_research_data")
    end,
  },
  {
    name = "research content rejects prerequisite cycles and profile unknown IDs",
    run = function()
      local function source_set(research)
        return {
          abilities = require("content.abilities.legacy"), materials = require("content.materials.legacy"), liquids = require("content.liquids.legacy"),
          gases = require("content.gases.legacy"), world_objects = require("content.world_objects.legacy"), hazards = require("content.hazards.legacy"),
          components = require("content.components.legacy"), topologies = require("content.body_topologies.normal"), actors = require("content.actors.player_legacy"),
          factions = require("content.factions.legacy"),
          enemies = require("content.enemies.legacy"), services = require("content.services.legacy"), boons = require("content.boons.legacy"),
          charms = require("content.charms.legacy"), curses = require("content.curses.legacy"), research = research,
        }
      end
      local nodes = {}
      for _, node in ipairs(require("content.research.legacy")) do
        local copy = {}; for key, value in pairs(node) do copy[key] = value end
        local prerequisites = {}; for _, id in ipairs(node.prerequisites) do prerequisites[#prerequisites + 1] = id end
        copy.prerequisites = prerequisites
        nodes[#nodes + 1] = copy
      end
      nodes[1].prerequisites = { nodes[2].id }
      nodes[2].prerequisites = { nodes[1].id }
      local ok, error_data = pcall(Registry.new, source_set(nodes))
      assert(not ok and tostring(error_data):find("cycle", 1, true))
      local profile = profile_with({ "research.body.unknown" })
      local valid, failure = pcall(MetaProfile.validate, profile, Registry.load())
      assert(not valid and tostring(failure):find("unknown research ID", 1, true))
    end,
  },
  {
    name = "research snapshots apply every production effect only to new runs",
    run = function()
      local registry = Registry.load()
      local profile = profile_with({
        "research.body.hardened_frame_i", "research.body.hardened_frame_ii", "research.body.hardened_frame_iii",
        "research.mobility.efficient_actuators_i", "research.mobility.efficient_actuators_ii",
        "research.loadout.expanded_charm_lattice", "research.loadout.cargo_frame_i", "research.loadout.cargo_frame_ii",
        "research.traversal.reinforced_breach", "research.preparation.salvage_reserve",
      })
      local session = Session.new({ seed = 99002, registry = registry, run_id = "run:000100", meta_snapshot = MetaProfile.snapshot(profile, registry) })
      session:start_run()
      assert(session.state.player.max_health == 8 and session.state.player.health == 8)
      assert(session.state.player.dash_base == 1)
      assert(session.state.inventory.height == 6 and session.state.scrap == 3)
      assert(session:has_meta_unlock("unlock.traversal.reinforced_breach"))
      local existing_dash, existing_slots = session.state.player.dash_base, require("src.simulation.run_modifiers").charm_slots(session.state)
      profile.research_data = 10
      local duplicate, duplicate_error = MetaProfile.purchase(profile, registry, "research.preparation.salvage_reserve")
      assert(not duplicate and duplicate_error.code == "already_unlocked")
      assert(session.state.player.dash_base == existing_dash and require("src.simulation.run_modifiers").charm_slots(session.state) == existing_slots)
      local impaired = session.state.player
      local legs = impaired.body:list_components()
      for _, component in ipairs(legs) do
        if registry:get_component(component.definition_id).compatible_slots[1] == "leg" then component.current_integrity = 0 end
      end
      assert(session:locomotion_state(impaired).state ~= "NORMAL")
    end,
  },
  {
    name = "research rewards use stable run milestones and reconcile once after resume",
    run = function()
      local registry, store = Registry.load(), SaveStore.memory()
      local holder = { profile = MetaProfile.new() }
      local function claim(id, amount)
        local candidate = MetaProfile.copy(holder.profile)
        local result = MetaProfile.claim_reward(candidate, id, amount)
        if result.applied then assert(MetaProfile.save(candidate, store, registry)); holder.profile = candidate end
        return result
      end
      local session = Session.new({ seed = 99003, registry = registry, run_id = "run:000101", meta_snapshot = MetaProfile.snapshot(holder.profile, registry), on_meta_reward = claim })
      session:start_run()
      assert(session:_complete_stage() == "reconstruction")
      assert(holder.profile.research_data == 1 and #holder.profile.claimed_reward_ids == 1)
      local active = SaveStore.memory(); assert(ActiveRun.save(session, active))
      local restored = assert(ActiveRun.load(active, { registry = registry, on_meta_reward = claim }))
      assert(restored:reconcile_meta_rewards() and holder.profile.research_data == 1)
      -- Exercise the terminal victory reward independently of the route
      -- traversal fixture above; route-node rewards are already claimed.
      restored.state.route = nil
      restored:start_boss(); restored.state.boss.health = 0
      assert(restored:turn("wait") == "victory")
      assert(holder.profile.research_data == 5 and #holder.profile.claimed_reward_ids == 2)
      assert(MetaProfile.load(store, registry).research_data == 5)
    end,
  },
  {
    name = "reinforced barriers require snapshot unlock and breach through ordinary world state",
    run = function()
      local fresh = new_session(99004, MetaProfile.new())
      local point = adjacent_open(fresh)
      local barrier = assert(fresh.state.world:place_object("world_object.traversal.reinforced_barrier", point.x, point.y))
      local blocked = fresh:interact(fresh.state.player, barrier.id, "traversal.breach")
      assert(not blocked.applied and blocked.code == "requires_unlock" and not barrier.destroyed)
      assert(fresh:damage_world_object(barrier, { amount = 2, cause = "kinetic" }).new_integrity == 97 and not barrier.destroyed)
      local profile = profile_with({ "research.traversal.reinforced_breach" })
      local unlocked = new_session(99005, profile)
      point = adjacent_open(unlocked)
      barrier = assert(unlocked.state.world:place_object("world_object.traversal.reinforced_barrier", point.x, point.y))
      assert(not unlocked.state.world:allows_gas_at(point.x, point.y))
      local breached = unlocked:interact(unlocked.state.player, barrier.id, "traversal.breach")
      assert(breached.applied and barrier.destroyed and unlocked.state.world:is_passable(point.x, point.y)
        and not unlocked.state.world:blocks_vision(point.x, point.y) and not unlocked.state.world:blocks_projectile(point.x, point.y)
        and unlocked.state.world:allows_gas_at(point.x, point.y))
      local saved = SaveStore.memory(); assert(ActiveRun.save(unlocked, saved))
      assert(ActiveRun.load(saved, { registry = unlocked.registry }).state.world:get_object(barrier.id).destroyed)
    end,
  },
  {
    name = "route unlock gates one optional branch while fresh and unlocked routes reach boss",
    run = function()
      local definitions = RouteDefinitions.load()
      local fresh = RouteGraph.new(99006, definitions)
      assert(fresh:complete_current().applied)
      local forest
      for _, node in ipairs(fresh:available()) do if node.key == "forest_tier_2" then forest = node end end
      assert(forest and fresh:select(forest.id).applied and fresh:complete_current().applied)
      assert(#fresh:available() == 1 and fresh:available()[1].type == "boss")
      assert(fresh:select(fresh:available()[1].id).applied and fresh:complete_current().applied)
      assert(#fresh:available() == 3)
      local unlocked = RouteGraph.new(99006, definitions, nil, { "unlock.traversal.reinforced_breach" })
      assert(unlocked:complete_current().applied)
      for _, node in ipairs(unlocked:available()) do if node.key == "forest_tier_2" then assert(unlocked:select(node.id).applied) end end
      assert(unlocked:complete_current().applied and #unlocked:available() == 1)
      assert(unlocked:select(unlocked:available()[1].id).applied and unlocked:complete_current().applied)
      assert(#unlocked:available() == 4)
      assert(RouteAnalysis.analyze(fresh, definitions).valid and RouteAnalysis.analyze(unlocked, definitions).valid)
      assert(RouteAnalysis.batch({ definitions = definitions, seed = 99000, count = 20 }).summary.failures == 0)
      assert(RouteAnalysis.batch({ definitions = definitions, seed = 99000, count = 20, unlock_ids = { "unlock.traversal.reinforced_breach" } }).summary.failures == 0)
    end,
  },
  {
    name = "research title view model scales and profile storage stays isolated from active runs",
    run = function()
      local active, meta = SaveStore.memory("active"), SaveStore.memory()
      local app = App.new({ seed = 99007, save_store = active, meta_store = meta })
      for index = 1, 30 do
        app.registry.research[string.format("research.body.fixture_%02d", index)] = {
          id = string.format("research.body.fixture_%02d", index), display_name = "Fixture " .. index,
          category = "body", description = "Fixture", cost = 1, prerequisites = {}, modifiers = { max_health = 1 },
        }
      end
      assert(app:open_research())
      assert(#app:research_options("body") >= 33)
      app.meta_profile.research_data = 2
      assert(MetaProfile.save(app.meta_profile, meta, app.registry))
      local option
      for _, value in ipairs(app:research_options("body")) do if value.id == "research.body.hardened_frame_i" then option = value end end
      assert(option and option.available)
      assert(active:read() == "active")
      assert(meta:read() ~= "active")
    end,
  },
  {
    name = "research purchases during an active run apply only to a later snapshot and legacy saves migrate safely",
    run = function()
      local active, meta = SaveStore.memory(), SaveStore.memory()
      local app = App.new({ seed = 99008, save_store = active, meta_store = meta })
      app.meta_profile.research_data = 5
      assert(MetaProfile.save(app.meta_profile, meta, app.registry))
      assert(app:request_new_run())
      local current_health = app.session.state.player.max_health
      app:open_research()
      app.research_category_index, app.research_node_index = 1, 1 -- BODY / Hardened Frame I
      local bought = app:purchase_selected_research()
      assert(bought.applied and app.session.state.player.max_health == current_health)
      local future = Session.new({ seed = 99009, registry = app.registry, run_id = "run:000200", meta_snapshot = MetaProfile.snapshot(app.meta_profile, app.registry) })
      future:start_run()
      assert(future.state.player.max_health == current_health + 1)
      local old_data = app.session:to_data()
      old_data.progression.meta_snapshot, old_data.progression.run_id, old_data.progression.meta_reward_events = nil, nil, nil
      local world_text = assert(Json.encode(old_data.world))
      local restored = Session.from_data(old_data, { registry = app.registry, meta_snapshot = MetaProfile.snapshot(app.meta_profile, app.registry) })
      assert(assert(Json.encode(restored.state.world:to_data())) == world_text)
      assert(restored.state.meta_snapshot.modifiers.max_health == 1)
    end,
  },
}
