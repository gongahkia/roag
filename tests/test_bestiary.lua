-- Tranche 8E contracts: authored bodies, shared melee, encounter pools, and
-- charm modifiers all use the production Session path rather than test-only
-- combat shortcuts.
local Content = require("src.content.legacy")
local Grid = require("src.world.grid")
local World = require("src.world.world")
local Session = require("src.simulation.session")
local Registry = require("src.content.registry")
local PhysicalItem = require("src.inventory.physical_item")
local InspectionFloor = require("src.generation.inspection_floor")
local Analysis = require("src.generation.analysis")
local Batch = require("src.generation.batch_analysis")
local ActiveRun = require("src.persistence.active_run")
local SaveStore = require("src.persistence.save_store")
local Recurrence = require("src.simulation.fallen_recurrence")
local Json = require("src.persistence.json")

local MELEE = "ability.weapon.melee.basic"
local RAM = "ability.weapon.melee.ram"
local HEAVY_PROJECTILE = "ability.weapon.projectile.heavy"
local BASIC_PROJECTILE = "ability.weapon.projectile.basic"
local SPIKES = "hazard.legacy.spike_field"

local function new_session(seed)
  local session = Session.new({ seed = seed or 108000 })
  session:start_run(Content.classes[1], Content.boons[1])
  return session
end

local function open_world(session, closed)
  local layout, blocked = {}, {}
  for _, point in ipairs(closed or {}) do blocked[Grid.key(point[1], point[2])] = true end
  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do
      if not blocked[Grid.key(x, y)] then layout[Grid.key(x, y)] = true end
    end
  end
  local state = session.state
  state.world = World.new(session.registry, "forest", layout, state)
  state.enemies, state.targets, state.corpses = {}, {}, {}
  state.bullets, state.bombs, state.flares, state.area_attacks, state.effects = {}, {}, {}, {}, {}
  state.torches, state.ammo, state.exit = {}, nil, nil
  state.phase = "combat"
  state.player.x, state.player.y = 10, 10
  return session
end

local function replace_component(session, slot_id, definition_id)
  local body = session.state.player.body
  local old = assert(body:detach(slot_id))
  assert(session.state.inventory:auto_place(PhysicalItem.from_component(old, session.registry)))
  local component = session.component_factory:create(definition_id)
  assert(body:install(slot_id, component))
  return component
end

local function enemy_snapshot(floor)
  local values = {}
  for _, enemy in ipairs(floor.state.enemies) do
    values[#values + 1] = table.concat({ enemy.content_id, enemy.x, enemy.y, tostring(enemy.elite) }, ":")
  end
  return table.concat(values, "|")
end

local function sorted_ids(values)
  local result = {}
  for id in pairs(values) do result[#result + 1] = id end
  table.sort(result)
  return result
end

local function non_enemy_snapshot(floor)
  local values = { floor.world:to_data().terrain, "targets" }
  for _, target in ipairs(floor.state.targets) do values[#values + 1] = target.x .. ":" .. target.y end
  for _, object in ipairs(floor.world:list_objects(true)) do values[#values + 1] = object.id .. ":" .. object.x .. ":" .. object.y end
  return table.concat(values, "|")
end

local function enter_depth_two(session)
  assert(session:_complete_stage() == "reconstruction")
  assert(session:complete_reconstruction().next == "curse")
  assert(session:choose_curse(session.state.curse_options[1]).next == "route")
  assert(session:select_route_node(session:available_route_nodes()[1].id).applied)
end

local function registry_with_forest_pool_weights(first_weight, second_weight)
  local pools = {}
  for index, pool in ipairs(require("content.encounters.legacy")) do
    local copied = { id = pool.id, biome_id = pool.biome_id, tier_id = pool.tier_id, entries = {} }
    for entry_index, entry in ipairs(pool.entries) do
      copied.entries[entry_index] = { enemy_id = entry.enemy_id, weight = entry.weight }
    end
    if copied.biome_id == "biome.legacy.forest" and copied.tier_id == "tier.legacy.1" then
      copied.entries[1].weight, copied.entries[2].weight = first_weight, second_weight
    end
    pools[index] = copied
  end
  return Registry.new({
    abilities = require("content.abilities.legacy"), materials = require("content.materials.legacy"),
    liquids = require("content.liquids.legacy"), gases = require("content.gases.legacy"),
    world_objects = require("content.world_objects.legacy"), hazards = require("content.hazards.legacy"),
    components = require("content.components.legacy"), topologies = require("content.body_topologies.normal"),
    actors = require("content.actors.player_legacy"), enemies = require("content.enemies.legacy"), encounter_pools = pools,
    services = require("content.services.legacy"), boons = require("content.boons.legacy"), charms = require("content.charms.legacy"),
    curses = require("content.curses.legacy"), research = require("content.research.legacy"),
  })
end

local function non_enemy_state_snapshot(session)
  local state = session.state
  local values = { Json.encode(state.world:to_data()), state.player.x .. ":" .. state.player.y }
  for _, target in ipairs(state.targets) do values[#values + 1] = "target:" .. target.x .. ":" .. target.y end
  for _, torch in ipairs(state.torches) do values[#values + 1] = "torch:" .. torch.x .. ":" .. torch.y end
  values[#values + 1] = "ammo:" .. state.ammo.x .. ":" .. state.ammo.y
  return table.concat(values, "|")
end

return {
  {
    name = "authored bestiary bodies and pools resolve every production biome tier without dead enemy content",
    run = function()
      local session, registry = new_session(108001), nil
      registry = session.registry
      local component_ids, enemy_ids, charm_ids = sorted_ids(registry.components), sorted_ids(registry.enemies), sorted_ids(registry.charms)
      assert(#component_ids == 16 and #enemy_ids == 10 and #charm_ids == 9)
      local ordinary, elite, referenced = 0, 0, {}
      for _, enemy_id in ipairs(enemy_ids) do
        local definition = registry:get_enemy(enemy_id)
        if definition.elite then elite = elite + 1 else ordinary = ordinary + 1 end
        local actor = session:_make_enemy(enemy_id, { x = 20, y = 20 })
        assert(actor.body and actor.content_id == enemy_id)
        assert(session:locomotion_state(actor).state == "NORMAL")
      end
      assert(ordinary == 7 and elite == 3)
      for _, biome_id in ipairs({ "biome.legacy.forest", "biome.legacy.cave", "biome.legacy.dungeon" }) do
        for tier = 1, 3 do
          local pool = assert(registry:encounter_pool_for(biome_id, "tier.legacy." .. tier))
          assert(#pool.entries > 0)
          for _, entry in ipairs(pool.entries) do referenced[entry.enemy_id] = true end
        end
      end
      for _, enemy_id in ipairs(enemy_ids) do assert(referenced[enemy_id], enemy_id .. " is unreachable content") end
      assert(session:actor_has_capability(session:_make_enemy("enemy.cave.conductor", { x = 20, y = 20 }), MELEE))
      assert(session:actor_has_capability(session:_make_enemy("enemy.cave.conductor", { x = 20, y = 20 }), "ability.electrical.discharge"))
      local reclaimer = session:_make_enemy("enemy.dungeon.reclaimer", { x = 20, y = 20 })
      assert(session:actor_has_capability(reclaimer, MELEE) and session:actor_has_capability(reclaimer, HEAVY_PROJECTILE))
    end,
  },
  {
    name = "melee activation is shared by player and enemy and force resolves normal spike and impact systems",
    run = function()
      local session = open_world(new_session(108002))
      local player = session.state.player
      local blade = replace_component(session, "left_arm", "component.arm.impact_blade")
      local enemy = session:_make_enemy("enemy.wild.ripper", { x = 11, y = 10 })
      enemy.health = 3
      session.state.enemies = { enemy }
      local strike = session:activate_actor_ability(player, MELEE, { direction = "d" })
      assert(strike.applied and strike.implementation == "melee" and strike.component_id == blade.id)
      assert(enemy.health == 2 and enemy.x == 12 and strike.force.applied)
      assert(blade.current_integrity == 2)

      local hazard_session = open_world(new_session(108003))
      replace_component(hazard_session, "left_arm", "component.arm.impact_blade")
      local victim = hazard_session:_make_enemy("enemy.wild.ripper", { x = 11, y = 10 })
      victim.health = 2
      hazard_session.state.enemies = { victim }
      assert(hazard_session.state.world:place_hazard(SPIKES, 12, 10))
      local lethal = hazard_session:activate_actor_ability(hazard_session.state.player, MELEE, { direction = "d" })
      assert(lethal.applied and #hazard_session.state.enemies == 0 and #hazard_session.state.corpses == 1)
      assert(hazard_session.state.corpses[1].x == 12 and hazard_session:validate_physical_ownership())

      local impact_session = open_world(new_session(108004), { { 12, 10 } })
      replace_component(impact_session, "left_arm", "component.arm.hydraulic_ram")
      local impact_target = impact_session:_make_enemy("enemy.dungeon.bulwark", { x = 11, y = 10 })
      impact_target.health = 5
      impact_session.state.enemies = { impact_target }
      local impact = impact_session:activate_actor_ability(impact_session.state.player, RAM, { direction = "d" })
      assert(impact.applied and impact.force.blocked and impact.force.impact.applied)
      assert(impact_target.health <= 2, "ram direct strike plus structural impact must both apply")

      local ai_session = open_world(new_session(108005))
      ai_session.state.player.x, ai_session.state.player.y = 10, 10
      local ripper = ai_session:_make_enemy("enemy.wild.ripper", { x = 11, y = 10 })
      ai_session.state.enemies = { ripper }
      local health = ai_session.state.player.health
      ai_session:_enemy_turn()
      assert(ai_session.state.player.health == health - 1 and ai_session.state.player.x == 9)
      assert(ripper.body:get_component("left_arm").current_integrity == 2)
    end,
  },
  {
    name = "broken melee and redundant elite providers change live capabilities without enemy-specific exceptions",
    run = function()
      local session = open_world(new_session(108006))
      replace_component(session, "left_arm", "component.arm.impact_blade")
      local player = session.state.player
      assert(session:damage_actor_body(player, { amount = 3, slot_id = "left_arm", cause = "fixture" }).became_broken)
      local broken = session:activate_actor_ability(player, MELEE, { direction = "d" })
      assert(not broken.applied and broken.code == "provider_broken")

      local reclaimer = session:_make_enemy("enemy.dungeon.reclaimer", { x = 20, y = 20 })
      assert(session:actor_has_capability(reclaimer, MELEE) and session:actor_has_capability(reclaimer, HEAVY_PROJECTILE))
      assert(session:damage_actor_body(reclaimer, { amount = 5, slot_id = "right_arm", cause = "fixture" }).became_broken)
      assert(session:actor_has_capability(reclaimer, MELEE) and not session:actor_has_capability(reclaimer, HEAVY_PROJECTILE))

      local gunner = session:_make_enemy("enemy.elite.redundant_gunner", { x = 21, y = 20 })
      assert(session:actor_ability_provider(gunner, BASIC_PROJECTILE).slot_id == "left_arm")
      assert(session:damage_actor_body(gunner, { amount = 3, slot_id = "left_arm", cause = "fixture" }).became_broken)
      assert(session:actor_ability_provider(gunner, BASIC_PROJECTILE).slot_id == "right_arm")
      assert(session:damage_actor_body(gunner, { amount = 3, slot_id = "right_arm", cause = "fixture" }).became_broken)
      assert(not session:actor_has_capability(gunner, BASIC_PROJECTILE))

      local bruiser = session:_make_enemy("enemy.dungeon.bulwark", { x = 22, y = 20 })
      assert(session:damage_actor_body(bruiser, { amount = 6, slot_id = "left_leg", cause = "fixture" }).became_broken)
      assert(session:locomotion_state(bruiser).state == "IMPAIRED")
      assert(session:damage_actor_body(bruiser, { amount = 6, slot_id = "right_leg", cause = "fixture" }).became_broken)
      assert(session:locomotion_state(bruiser).state == "CRAWLING")
    end,
  },
  {
    name = "new physical melee and heavy projectile parts survive corpse salvage reconstruction with their live IDs",
    run = function()
      local session = open_world(new_session(108007))
      local reclaimer = session:_make_enemy("enemy.dungeon.reclaimer", { x = 11, y = 10 })
      local blade_id = reclaimer.body:get_component("left_arm").id
      local emitter_id = reclaimer.body:get_component("right_arm").id
      session.state.enemies = { reclaimer }
      session:_destroy_enemy(1)
      local corpse = session.state.corpses[1]
      assert(session:salvage_corpse_component(corpse.id, "left_arm").applied)
      assert(session:salvage_corpse_component(corpse.id, "right_arm").applied)
      session.state.phase = "reconstruction"
      assert(session:uninstall_body_component("left_arm").applied)
      assert(session:uninstall_body_component("right_arm").applied)
      assert(session:install_inventory_component(blade_id, "left_arm").applied)
      assert(session:install_inventory_component(emitter_id, "right_arm").applied)
      assert(session.state.player.body:get_component("left_arm").id == blade_id)
      assert(session.state.player.body:get_component("right_arm").id == emitter_id)
      assert(session:actor_has_capability(session.state.player, MELEE))
      assert(session:actor_has_capability(session.state.player, HEAVY_PROJECTILE))
      assert(session:validate_physical_ownership())
    end,
  },
  {
    name = "new charms compose through the common modifier layer and semantic IDs survive active-run restoration",
    run = function()
      local session = open_world(new_session(108008))
      replace_component(session, "left_arm", "component.arm.impact_blade")
      session.state.charms.slots = {
        "charm.legacy.kinetic_capacitor",
        "charm.legacy.edge_tuning",
        "charm.legacy.ballistic_lens",
      }
      session:refresh_derived_player_stats()
      local enemy = session:_make_enemy("enemy.wild.ripper", { x = 11, y = 10 })
      enemy.health = 4
      session.state.enemies = { enemy }
      local melee = session:activate_actor_ability(session.state.player, MELEE, { direction = "d" })
      assert(melee.applied and melee.damage.amount == 2 and melee.force.moved_distance == 2)
      local projectile = session:activate_actor_ability(session.state.player, BASIC_PROJECTILE, { direction = "w" })
      assert(projectile.applied and projectile.projectile.damage == 2)
      local store = SaveStore.memory()
      assert(ActiveRun.save(session, store))
      local restored = assert(ActiveRun.load(store))
      assert(restored.state.charms.slots[1] == "charm.legacy.kinetic_capacitor")
      assert(restored.state.charms.slots[2] == "charm.legacy.edge_tuning")
      assert(restored.state.charms.slots[3] == "charm.legacy.ballistic_lens")

      local pathfinder = new_session(108009)
      pathfinder.state.charms.slots = { "charm.legacy.pathfinder" }
      pathfinder:refresh_derived_player_stats()
      local settings = pathfinder:_settings_for_floor(pathfinder.route_definitions:get_biome("biome.legacy.forest"),
        pathfinder.route_definitions:get_tier("tier.legacy.1"))
      assert(settings.objective_required == 4 and settings.score == 4)
    end,
  },
  {
    name = "biome tier encounter construction is deterministic and inspector metrics expose archetypes elites and capabilities",
    run = function()
      local first = assert(InspectionFloor.generate({ biome = "biome.legacy.dungeon", tier = "tier.legacy.3", seed = 108010 }))
      local second = assert(InspectionFloor.generate({ biome = "biome.legacy.dungeon", tier = "tier.legacy.3", seed = 108010 }))
      assert(enemy_snapshot(first) == enemy_snapshot(second))
      assert(non_enemy_snapshot(first) == non_enemy_snapshot(second))
      local report = Analysis.analyze(first.world, { state = first.state, session = first.session, provenance = first.provenance })
      assert(report.valid and report.metrics.enemy_types and report.metrics.enemy_capabilities)
      local batch = assert(Batch.run({ biome = "biome.legacy.dungeon", tier = "tier.legacy.3", seed = 108020, count = 16 }))
      assert(batch.summary.failures == 0 and next(batch.summary.enemy_counts) and next(batch.summary.enemy_capabilities))
      assert((batch.summary.elite_enemies and batch.summary.elite_enemies.average or 0) >= 0)
    end,
  },
  {
    name = "encounter-pool choices use an isolated stream and cannot perturb terrain, objects, objectives, or spawn locations",
    run = function()
      local first = Session.new({ seed = 108025, registry = registry_with_forest_pool_weights(100, 1) })
      local second = Session.new({ seed = 108025, registry = registry_with_forest_pool_weights(1, 100) })
      first:start_biome_tier("biome.legacy.forest", "tier.legacy.1", 108025)
      second:start_biome_tier("biome.legacy.forest", "tier.legacy.1", 108025)
      assert(non_enemy_state_snapshot(first) == non_enemy_state_snapshot(second))
      local positions = {}
      for index, enemy in ipairs(first.state.enemies) do positions[index] = enemy.x .. ":" .. enemy.y end
      for index, enemy in ipairs(second.state.enemies) do assert(positions[index] == enemy.x .. ":" .. enemy.y) end
      assert(first.state.enemies[1].content_id ~= second.state.enemies[1].content_id)
    end,
  },
  {
    name = "fallen echoes materialize new melee and heavy projectile parts as ordinary shared body capabilities",
    run = function()
      local source = new_session(108030)
      replace_component(source, "left_arm", "component.arm.impact_blade")
      replace_component(source, "right_arm", "component.arm.heavy_projectile_emitter")
      local record = {
        id = "fallen:000081",
        source_run_id = "run:108030",
        body = source.state.player.body:to_data(),
        metadata = { route_path = {}, charm_ids = {}, research_ids = {}, biome_id = "biome.legacy.forest", route_depth = 1 },
      }
      local spec = assert(Recurrence.assign("run:108031", 108031, { record }))
      spec.mode, spec.target_depth = "hostile", 2
      local future = Session.new({ seed = 108031, registry = source.registry, run_id = "run:108031", fallen_recurrence = spec })
      future:start_run(); enter_depth_two(future)
      local echo
      for _, enemy in ipairs(future.state.enemies) do if enemy.kind == "fallen_echo" then echo = enemy end end
      assert(echo and future:actor_has_capability(echo, MELEE) and future:actor_has_capability(echo, HEAVY_PROJECTILE))
      echo.x, echo.y = future.state.player.x + 1, future.state.player.y
      future.state.enemies = { echo }
      local health = future.state.player.health
      local strike = future:activate_actor_ability(echo, MELEE, { direction = "a" })
      assert(strike.applied and future.state.player.health == health - 1)
      assert(echo.body:get_component("left_arm").origin.archive_id == record.id)
    end,
  },
}
