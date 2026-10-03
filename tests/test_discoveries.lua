local ActiveRun = require("src.persistence.active_run")
local App = require("src.app.app")
local Analysis = require("src.generation.analysis")
local Grid = require("src.world.grid")
local InspectionFloor = require("src.generation.inspection_floor")
local Json = require("src.persistence.json")
local MetaProfile = require("src.persistence.meta_profile")
local Registry = require("src.content.registry")
local SaveStore = require("src.persistence.save_store")
local Session = require("src.simulation.session")

local function profile_with(ids, data)
  return { research_data = data or 0, unlocked_research_ids = ids or {}, next_run_sequence = 1, claimed_reward_ids = {}, discovered_discovery_ids = {} }
end

local function new_session(profile, handler)
  local registry = Registry.load()
  local session = Session.new({
    seed = 716001,
    registry = registry,
    run_id = "run:discovery_fixture",
    meta_snapshot = MetaProfile.snapshot(profile or MetaProfile.new(), registry),
    on_meta_reward = handler,
  })
  session:start_run()
  return session, registry
end

local function adjacent_open(session)
  for _, point in ipairs(Grid.neighbours(session.state.player)) do
    if session.state.world:is_passable(point.x, point.y) and not session.state.world:object_at(point.x, point.y) then return point end
  end
  error("Expected an adjacent open cell")
end

local function discovery_cache(session, discovery_id)
  local point = adjacent_open(session)
  return assert(session.state.world:place_object("world_object.discovery.cache", point.x, point.y, {
    discovery_id = discovery_id,
    discovery_access_profile_id = session.registry:get_discovery(discovery_id).access_profile_id,
    discovery_provenance = "test.discoveries",
  }))
end

local function claim_handler(holder, registry)
  return function(reward_id, amount, event)
    local candidate = MetaProfile.copy(holder.profile)
    local result
    if event and event.kind == "discovery" then
      result = MetaProfile.claim_discovery(candidate, event.discovery_id, reward_id, amount)
    else
      result = MetaProfile.claim_reward(candidate, reward_id, amount)
    end
    if result.applied or result.already_claimed or result.already_discovered then holder.profile = candidate end
    return result
  end
end

local function report_for(floor)
  return Analysis.analyze(floor.world, {
    seed = floor.seed, stage = floor.stage, biome_id = floor.biome_id, tier_id = floor.tier_id,
    terrain = floor.terrain, state = floor.state, session = floor.session, provenance = floor.provenance,
  })
end

local function floor_with_discovery(options)
  for seed = options.first_seed, options.first_seed + 80 do
    local floor = assert(InspectionFloor.generate({
      biome = options.biome, tier = options.tier, seed = seed,
      meta_snapshot = options.meta_snapshot,
      discovery_state = { enabled = true, assigned_discovery_ids = {} },
    }))
    if floor.state.generation_metadata.discovery then return floor end
  end
  error("Expected a generated discovery site")
end

return {
  {
    name = "discovery content is declarative, validated, and exposes every bounded access profile",
    run = function()
      local registry = Registry.load()
      assert(registry:validate_discoveries(require("src.routes.definitions").load()))
      local profiles, count = {}, 0
      for _, definition in pairs(registry.discoveries) do
        count = count + 1
        profiles[definition.access_profile_id] = true
        assert(definition.first_data_reward == 2 and definition.repeat_scrap_reward == 2)
      end
      assert(count == 8)
      assert(profiles["access_profile.discovery.open"] and profiles["access_profile.discovery.breachable"])
      assert(profiles["access_profile.discovery.powered"] and profiles["access_profile.discovery.maintenance_hatch"])
    end,
  },
  {
    name = "discovery generation is deterministic, isolated, and reports optional access provenance",
    run = function()
      local first = floor_with_discovery({ biome = "biome.legacy.dungeon", tier = "tier.legacy.3", first_seed = 716100 })
      local second = assert(InspectionFloor.generate({
        biome = first.biome_id, tier = first.tier_id, seed = first.seed,
        discovery_state = { enabled = true, assigned_discovery_ids = {} },
      }))
      assert(Json.encode(first.state.generation_metadata.discovery) == Json.encode(second.state.generation_metadata.discovery))
      local report = report_for(first)
      assert(report.valid and report.metrics.discovery_sites == 1)
      assert(#report.discoveries == 1 and report.discoveries[1].placement_provenance:match("%.discoveries$"))
      assert(report.metrics.unreachable_discovery_gates == 0)

      local without = assert(InspectionFloor.generate({ biome = first.biome_id, tier = first.tier_id, seed = first.seed,
        discovery_state = { enabled = false, assigned_discovery_ids = {} } }))
      local function stable_floor_state(floor)
        local data = floor.world:to_data()
        local objects, circuits = {}, {}
        for _, object in ipairs(data.objects) do if not object.discovery_id then objects[#objects + 1] = object end end
        for _, circuit in ipairs(data.circuits) do if not circuit.id:match("^power%.circuit%.discovery%.") then circuits[#circuits + 1] = circuit end end
        data.objects, data.circuits = objects, circuits
        return { world = data, targets = floor.state.targets, enemies = floor.state.enemies, player = floor.state.player }
      end
      assert(Json.encode(stable_floor_state(first)) == Json.encode(stable_floor_state(without)))
    end,
  },
  {
    name = "first discovery persists DATA once while known rediscovery pays only current-run SCRAP",
    run = function()
      local registry = Registry.load()
      local holder = { profile = MetaProfile.new() }
      local handler = claim_handler(holder, registry)
      local session = assert(new_session(holder.profile, handler))
      local cache = discovery_cache(session, "discovery.forest.survey_cache")
      local first = session:interact(session.state.player, cache.id, "discovery.claim")
      assert(first.applied and first.code == "first_discovery" and holder.profile.research_data == 2)
      assert(MetaProfile.has_discovery(holder.profile, cache.discovery_id) and cache.discovery_claimed)
      local duplicate = session:interact(session.state.player, cache.id, "discovery.claim")
      assert(not duplicate.applied and duplicate.code == "already_claimed")

      local known_session = assert(new_session(holder.profile, handler))
      local repeat_cache = discovery_cache(known_session, "discovery.forest.survey_cache")
      local before_data, before_scrap = holder.profile.research_data, known_session.state.scrap
      local known = known_session:interact(known_session.state.player, repeat_cache.id, "discovery.claim")
      assert(known.applied and known.code == "repeat_discovery" and holder.profile.research_data == before_data)
      assert(known_session.state.scrap == before_scrap + 2 and repeat_cache.discovery_claimed)
    end,
  },
  {
    name = "maintenance hatch respects future-run unlock snapshots and persists ordinary opened world state",
    run = function()
      local fresh_profile = MetaProfile.new()
      local fresh = assert(new_session(fresh_profile))
      local point = adjacent_open(fresh)
      local hatch = assert(fresh.state.world:place_object("world_object.traversal.maintenance_hatch", point.x, point.y))
      assert(not fresh.state.world:allows_gas_at(point.x, point.y))
      local locked = fresh:interact(fresh.state.player, hatch.id, "traversal.breach")
      assert(not locked.applied and locked.code == "requires_unlock" and not hatch.destroyed)
      fresh_profile.research_data = 20
      assert(MetaProfile.purchase(fresh_profile, fresh.registry, "research.traversal.reinforced_breach"))
      assert(MetaProfile.purchase(fresh_profile, fresh.registry, "research.traversal.maintenance_override"))
      assert(not fresh:has_meta_unlock("unlock.traversal.maintenance_override"), "current run must keep its original snapshot")

      local later = assert(new_session(fresh_profile))
      point = adjacent_open(later)
      hatch = assert(later.state.world:place_object("world_object.traversal.maintenance_hatch", point.x, point.y))
      local opened = later:interact(later.state.player, hatch.id, "traversal.breach")
      assert(opened.applied and hatch.destroyed and later.state.world:is_passable(point.x, point.y))
      assert(not later.state.world:blocks_vision(point.x, point.y) and not later.state.world:blocks_projectile(point.x, point.y)
        and later.state.world:allows_gas_at(point.x, point.y))
      local store = SaveStore.memory(); assert(ActiveRun.save(later, store))
      assert(ActiveRun.load(store, { registry = later.registry }).state.world:get_object(hatch.id).destroyed)
    end,
  },
  {
    name = "discovery clues are explicit and a pending first-claim reconciles without duplicate DATA",
    run = function()
      local holder = { profile = MetaProfile.new(), fail_once = true }
      local session, registry = new_session(holder.profile, function(reward_id, amount, event)
        if holder.fail_once then holder.fail_once = false; return { applied = false, code = "write_failed" } end
        local candidate = MetaProfile.copy(holder.profile)
        local result = MetaProfile.claim_discovery(candidate, event.discovery_id, reward_id, amount)
        if result.applied or result.already_discovered then holder.profile = candidate end
        return result
      end)
      local point = adjacent_open(session)
      local definition = registry:get_discovery("discovery.cave.echo_vault")
      local clue = assert(session.state.world:place_object("world_object.discovery.clue", point.x, point.y, {
        discovery_id = definition.id, discovery_access_profile_id = definition.access_profile_id, discovery_provenance = "test.clue",
      }))
      local read = session:interact(session.state.player, clue.id, "clue.read")
      assert(read.applied and read.code == "clue_read" and read.reason == definition.presentation.clue)
      local cache = discovery_cache(session, definition.id)
      local claimed = session:interact(session.state.player, cache.id, "discovery.claim")
      assert(claimed.applied and holder.profile.research_data == 0 and #session.state.meta_reward_events == 1)
      local store = SaveStore.memory(); assert(ActiveRun.save(session, store))
      local restored = assert(ActiveRun.load(store, { registry = registry, on_meta_reward = function(reward_id, amount, event)
        local candidate = MetaProfile.copy(holder.profile)
        local result = MetaProfile.claim_discovery(candidate, event.discovery_id, reward_id, amount)
        if result.applied or result.already_discovered then holder.profile = candidate end
        return result
      end }))
      assert(restored:reconcile_meta_rewards() and holder.profile.research_data == 2)
      assert(restored:reconcile_meta_rewards() and holder.profile.research_data == 2)
    end,
  },
  {
    name = "old active-run progression restores discovery generation disabled rather than changing future floors",
    run = function()
      local registry = Registry.load()
      local legacy = Session.new({ seed = 716009, registry = registry, run_id = "run:pre_8j" })
      legacy:start_stage()
      local data = legacy:to_data()
      data.progression.discovery_state = nil
      local restored = assert(Session.from_data(data, { registry = registry }))
      assert(restored.state.discovery_state.enabled == false)
      for _, object in ipairs(restored.state.world:list_objects()) do assert(not object.discovery_id) end
    end,
  },
  {
    name = "expanded research composes through the ordinary modifier and inventory paths",
    run = function()
      local profile = profile_with({
        "research.body.hardened_frame_i", "research.body.hardened_frame_ii", "research.body.hardened_frame_iii", "research.body.hardened_frame_iv",
        "research.mobility.efficient_actuators_i", "research.mobility.efficient_actuators_ii", "research.mobility.efficient_actuators_iii",
        "research.loadout.expanded_charm_lattice", "research.loadout.expanded_charm_lattice_ii",
        "research.loadout.cargo_frame_i", "research.loadout.cargo_frame_ii", "research.loadout.cargo_frame_iii",
        "research.preparation.salvage_reserve", "research.preparation.salvage_reserve_ii",
        "research.combat.kinetic_training", "research.combat.ballistic_calibration",
      })
      local session = assert(new_session(profile))
      local modifiers = require("src.simulation.run_modifiers")
      assert(session.state.player.max_health == 9 and session.state.inventory.height == 10 and session.state.scrap == 6)
      assert(modifiers.charm_slots(session.state) == 5 and session:available_actor_abilities(session.state.player))
      assert(modifiers.value(session.state, session.registry, "melee_force") == 1
        and modifiers.value(session.state, session.registry, "projectile_damage") == 1)
      local missing = MetaProfile.new(); missing.research_data = 20
      local result, error_data = MetaProfile.purchase(missing, session.registry, "research.body.hardened_frame_iv")
      assert(not result and error_data.code == "missing_prerequisite")
    end,
  },
  {
    name = "research view model exposes a stable discovery ledger without revealing unknown records",
    run = function()
      local app = App.new({ seed = 716099, save_store = SaveStore.memory(), meta_store = SaveStore.memory(), archive_store = SaveStore.memory() })
      local history = app:discovery_history()
      assert(#history == 8)
      for _, entry in ipairs(history) do assert(entry.discovered == false and entry.description == nil) end
      app.meta_profile.discovered_discovery_ids = { "discovery.reactor.service_blackbox" }
      history = app:discovery_history()
      local found
      for _, entry in ipairs(history) do if entry.id == "discovery.reactor.service_blackbox" then found = entry end end
      assert(found and found.discovered and found.description and found.biome_id == "biome.legacy.reactor")
    end,
  },
}
