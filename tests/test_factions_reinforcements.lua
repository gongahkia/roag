-- Bounded ecology contracts: declarative hostility, ordinary shared combat,
-- and finite physical reinforcement origins.
local Content = require("src.content.legacy")
local Analysis = require("src.generation.analysis")
local Grid = require("src.world.grid")
local InspectionFloor = require("src.generation.inspection_floor")
local ReinforcementSimulation = require("src.simulation.reinforcements")
local Session = require("src.simulation.session")
local World = require("src.world.world")

local BASIC_PROJECTILE = "ability.weapon.projectile.basic"
local BASIC_MELEE = "ability.weapon.melee.basic"
local SHOCK = "ability.electrical.discharge"
local WATER = "liquid.water.legacy"

local function new_session(seed)
  local session = Session.new({ seed = seed or 81001 })
  session:start_run(Content.classes[1], Content.boons[1])
  return session
end

local function open_world(session)
  local layout = {}
  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do layout[Grid.key(x, y)] = true end
  end
  local state = session.state
  state.world = World.new(session.registry, "forest", layout, state)
  state.enemies, state.targets, state.corpses = {}, {}, {}
  state.bullets, state.bombs, state.flares, state.area_attacks, state.effects = {}, {}, {}, {}, {}
  state.torches, state.ammo, state.exit, state.boss = {}, nil, nil, nil
  state.phase, state.ended = "combat", nil
  state.player.x, state.player.y = 10, 10
  state.player.health, state.player.impact = 5, 0
  return session
end

local function profile(id)
  for _, value in ipairs(require("content.reinforcements.legacy")) do
    if value.id == id then return value end
  end
  error("Unknown reinforcement fixture profile " .. tostring(id))
end

local function source_for(session, profile_id, x, y)
  local source_profile = profile(profile_id)
  local definition_id = source_profile.source_type == "lift"
    and "world_object.reinforcement.lift" or "world_object.reinforcement.nest"
  local wave = {}
  for index = 1, source_profile.wave_size do wave[index] = source_profile.entries[1].enemy_id end
  return assert(session.state.world:place_object(definition_id, x, y, {
    reinforcement_profile_id = source_profile.id,
    reinforcement_faction_id = source_profile.faction_id,
    reinforcement_charges = 1,
    reinforcement_state = "idle",
    reinforcement_just_armed = false,
    reinforcement_wave_enemy_ids = wave,
    reinforcement_provenance = "test.reinforcement.fixture",
  }))
end

local function hostile_fixture(session)
  local feral = session:_make_enemy("enemy.wild.skirmisher", { x = 13, y = 10 }, { scrap_award = true })
  local cult = session:_make_enemy("enemy.legacy.cultist", { x = 17, y = 10 }, { scrap_award = true })
  return feral, cult
end

local function ordinary_floor_snapshot(floor)
  local state, values = floor.state, {}
  for _, enemy in ipairs(state.enemies) do
    values[#values + 1] = table.concat({ enemy.content_id, enemy.x, enemy.y, enemy.faction_id }, ":")
  end
  table.sort(values)
  local objects = {}
  for _, object in ipairs(state.world:list_objects(true)) do
    if object.interaction_role ~= "reinforcement" then
      objects[#objects + 1] = table.concat({ object.definition_id, object.x, object.y }, ":")
    end
  end
  table.sort(objects)
  return table.concat({
    state.player.x .. ":" .. state.player.y,
    table.concat(values, "|"),
    table.concat(objects, "|"),
    tostring(state.world:to_data().terrain),
  }, "#")
end

return {
  {
    name = "factions are explicit symmetric content and every authored enemy belongs to one",
    run = function()
      local session, registry = new_session(81002), nil
      registry = session.registry
      assert(registry:get_faction("faction.player").display_name == "Reclaimer")
      for _, first_id in ipairs({ "faction.player", "faction.feral", "faction.cult", "faction.machine", "faction.echo" }) do
        local first = registry:get_faction(first_id)
        for _, second_id in ipairs(first.hostile_faction_ids) do
          local reverse = registry:get_faction(second_id).hostile_faction_ids
          local found = false
          for _, value in ipairs(reverse) do if value == first_id then found = true end end
          assert(found, "hostility must remain symmetric")
        end
      end
      for _, enemy in pairs(registry.enemies) do assert(registry.factions[enemy.faction_id]) end
    end,
  },
  {
    name = "hostile target selection is faction-driven, stable, and enemy combat uses shared projectile melee and corpse paths",
    run = function()
      local session = open_world(new_session(81003))
      session.state.player.x, session.state.player.y = 2, 10
      local skirmisher, cult = hostile_fixture(session)
      local same_faction = session:_make_enemy("enemy.wild.ripper", { x = 14, y = 10 }, { scrap_award = true })
      session.state.enemies = { skirmisher, same_faction, cult }
      assert(not session:are_hostile(skirmisher, same_faction))
      assert(session:are_hostile(skirmisher, cult))
      local target, route = session:_nearest_hostile_target(skirmisher)
      assert(target == cult and #route > 0, "nearest hostile faction actor must be selected")
      local repeat_target = select(1, session:_nearest_hostile_target(skirmisher))
      assert(repeat_target == target, "target selection must not depend on table/hash order")

      -- A physical projectile follows the same bullet update path and an
      -- ecology kill yields a normal corpse but no player currency.
      cult.x, cult.y, cult.health = 16, 10, 1
      local scrap, objectives = session.state.scrap, session.state.player.objective_progress
      assert(session:activate_actor_ability(skirmisher, BASIC_PROJECTILE, { direction = "d" }).applied)
      for _ = 1, 4 do session:_update_bullets() end
      assert(#session.state.enemies == 2 and #session.state.corpses == 1)
      assert(session.state.scrap == scrap and session.state.player.objective_progress == objectives)
      assert(session.state.corpses[1].body and session.state.corpses[1].source_actor_id == "enemy.legacy.cultist")

      local melee = session:_make_enemy("enemy.wild.ripper", { x = 20, y = 20 }, { scrap_award = true })
      local rival = session:_make_enemy("enemy.legacy.cultist", { x = 21, y = 20 }, { scrap_award = true })
      rival.health = 3
      session.state.enemies = { melee, rival }
      local strike = session:activate_actor_ability(melee, BASIC_MELEE, { direction = "d" })
      assert(strike.applied and strike.force and strike.force.applied and rival.health == 2)
    end,
  },
  {
    name = "combat alarm sources have one finite visible arrival and can be destroyed before deployment",
    run = function()
      local session = open_world(new_session(81004))
      local source = source_for(session, "reinforcement_profile.feral.forest", 30, 30)
      local feral = session:_make_enemy("enemy.wild.ripper", { x = 11, y = 10 }, { scrap_award = true })
      session.state.enemies = { feral }
      assert(session:_notify_combat(session.state.player, feral).applied)
      assert(source.reinforcement_state == "armed" and source.reinforcement_delay == 2)
      assert(#ReinforcementSimulation.tick(session) == 1 and source.reinforcement_delay == 2)
      ReinforcementSimulation.tick(session)
      assert(source.reinforcement_delay == 1 and #session.state.enemies == 1)
      ReinforcementSimulation.tick(session)
      assert(source.reinforcement_state == "spent" and source.reinforcement_charges == 0 and #session.state.enemies == 2)
      ReinforcementSimulation.tick(session)
      assert(#session.state.enemies == 2, "spent source must never deploy another wave")

      local idle = source_for(session, "reinforcement_profile.feral.forest", 35, 30)
      assert(session:damage_world_object(idle, { amount = idle.current_integrity, cause = "kinetic" }).destroyed)
      assert(idle.destroyed and idle.reinforcement_state == "cancelled")
      assert(not session:_notify_combat(session.state.player, feral).applied,
        "a destroyed idle origin can never arm")

      local cancelled = source_for(session, "reinforcement_profile.feral.forest", 40, 30)
      assert(session:_notify_combat(session.state.player, feral).applied)
      assert(cancelled.reinforcement_state == "armed")
      assert(session:damage_world_object(cancelled, { amount = cancelled.current_integrity, cause = "kinetic" }).destroyed)
      assert(cancelled.destroyed and cancelled.reinforcement_state == "cancelled")
      local before = #session.state.enemies
      ReinforcementSimulation.tick(session)
      assert(#session.state.enemies == before, "destroying an armed source cancels its pending wave")
    end,
  },
  {
    name = "cross-faction electricity and force remain physically impartial shared combat",
    run = function()
      local session = open_world(new_session(810045))
      session.state.player.x, session.state.player.y = 2, 2
      local conductor = session:_make_enemy("enemy.cave.conductor", { x = 9, y = 10 }, { scrap_award = true })
      local rival = session:_make_enemy("enemy.wild.ripper", { x = 12, y = 10 }, { scrap_award = true })
      rival.health = 3
      session.state.enemies = { conductor, rival }
      for x = 10, 12 do assert(session.state.world:add_liquid(x, 10, WATER, 1).applied) end
      assert(session:activate_actor_ability(conductor, SHOCK, { direction = "d" }).applied)
      assert(rival.health == 2, "electricity must reach a hostile non-player actor through ordinary water")

      local mauler = session:_make_enemy("enemy.wild.ripper", { x = 20, y = 20 }, { scrap_award = true })
      local victim = session:_make_enemy("enemy.legacy.cultist", { x = 21, y = 20 }, { scrap_award = true })
      victim.health = 2
      session.state.enemies = { mauler, victim }
      assert(session.state.world:place_hazard("hazard.legacy.spike_field", 22, 20))
      assert(session:activate_actor_ability(mauler, BASIC_MELEE, { direction = "d" }).applied)
      assert(#session.state.enemies == 1 and #session.state.corpses == 1,
        "hostile force into an existing hazard must produce the same normal corpse path")
    end,
  },
  {
    name = "armed reinforcement sources and faction actors persist exactly through active-run restoration",
    run = function()
      local session = open_world(new_session(81005))
      local source = source_for(session, "reinforcement_profile.feral.forest", 30, 30)
      local feral = session:_make_enemy("enemy.wild.ripper", { x = 11, y = 10 }, { scrap_award = true })
      session.state.enemies = { feral }
      assert(session:_notify_combat(session.state.player, feral).applied)
      ReinforcementSimulation.tick(session) -- consume the arm notification, retain delay two.
      local restored = Session.from_data(session:to_data())
      local loaded = assert(restored.state.world:get_object(source.id))
      assert(restored:actor_faction_id(restored.state.enemies[1]) == "faction.feral")
      assert(loaded.reinforcement_state == "armed" and loaded.reinforcement_delay == 2 and not loaded.reinforcement_just_armed)
      ReinforcementSimulation.tick(restored)
      assert(loaded.reinforcement_delay == 1)
      ReinforcementSimulation.tick(restored)
      assert(loaded.reinforcement_state == "spent" and #restored.state.enemies == 2)

      local legacy = open_world(new_session(81006)):to_data()
      legacy.progression.reinforcement_state = nil
      local legacy_restored = Session.from_data(legacy)
      assert(legacy_restored.state.reinforcement_state.enabled == false,
        "pre-8K active saves must stay ecology-free instead of receiving future sources")
    end,
  },
  {
    name = "reinforcement generation is deterministic isolated optional content with at most one safe source",
    run = function()
      local chosen_seed
      for seed = 81010, 81110 do
        local floor = assert(InspectionFloor.generate({ biome = "biome.legacy.forest", tier = "tier.legacy.1", seed = seed,
          discovery_state = { enabled = false, assigned_discovery_ids = {} }, reinforcement_state = { enabled = true } }))
        if floor.state.generation_metadata.reinforcement then chosen_seed = seed; break end
      end
      assert(chosen_seed, "fixture range should include a selected source")
      local options = { biome = "biome.legacy.forest", tier = "tier.legacy.1", seed = chosen_seed,
        discovery_state = { enabled = false, assigned_discovery_ids = {} }, reinforcement_state = { enabled = true } }
      local first, second = assert(InspectionFloor.generate(options)), assert(InspectionFloor.generate(options))
      local first_source, second_source = first.state.generation_metadata.reinforcement, second.state.generation_metadata.reinforcement
      assert(first_source.source_object_id == second_source.source_object_id and first_source.x == second_source.x and first_source.y == second_source.y)
      local source_count = 0
      for _, object in ipairs(first.state.world:list_objects()) do if object.interaction_role == "reinforcement" then source_count = source_count + 1 end end
      assert(source_count == 1)
      local without = assert(InspectionFloor.generate({ biome = "biome.legacy.forest", tier = "tier.legacy.1", seed = chosen_seed,
        discovery_state = { enabled = false, assigned_discovery_ids = {} }, reinforcement_state = { enabled = false } }))
      assert(ordinary_floor_snapshot(first) == ordinary_floor_snapshot(without), "source stream must not perturb existing generation")
    end,
  },
  {
    name = "a reinforcement origin preserves any previously generated discovery access route",
    run = function()
      -- This structured Reactor seed contains an open discovery in a narrow
      -- room layout. It protects the post-discovery source-placement check
      -- from regressing into an optional-site soft lock.
      local floor = assert(InspectionFloor.generate({
        biome = "biome.legacy.reactor", tier = "tier.legacy.3", seed = 81539,
        discovery_state = { enabled = true, assigned_discovery_ids = {} },
        reinforcement_state = { enabled = true },
      }))
      local report = Analysis.analyze(floor.world, {
        seed = floor.seed, stage = floor.stage, biome_id = floor.biome_id, tier_id = floor.tier_id,
        terrain = floor.terrain, state = floor.state, session = floor.session, provenance = floor.provenance,
      })
      assert(report.valid)
      assert(report.metrics.discovery_sites == 1 and report.metrics.reinforcement_sources <= 1)
    end,
  },
}
