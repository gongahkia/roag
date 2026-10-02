local ActiveRun = require("src.persistence.active_run")
local FallenArchive = require("src.persistence.fallen_archive")
local Content = require("src.content.legacy")
local Component = require("src.body.component")
local Recurrence = require("src.simulation.fallen_recurrence")
local Session = require("src.simulation.session")

local function new_run(seed)
  local session = Session.new({ seed = seed or 97001 })
  session:start_run(Content.classes[1], Content.boons[1])
  return session
end

local function enter_milestone(session, biome_id)
  assert(session:_complete_stage() == "reconstruction")
  assert(session:complete_reconstruction().next == "curse")
  local curse = session:choose_curse(session.state.curse_options[1])
  assert(curse.next == "route")
  for _, node in ipairs(session:available_route_nodes()) do
    if node.biome_id == biome_id then
      assert(session:select_route_node(node.id).applied)
      break
    end
  end
  assert(session.state.settings.biome_id == biome_id)
  assert(session:_complete_stage() == "reconstruction")
  assert(session:complete_reconstruction().next == "boss")
  return session.state.boss
end

-- New production routes have a deterministic second milestone immediately
-- after the selected tier-three normal floor.  This deliberately walks the
-- ordinary 8G lifecycle so these tests cover route transition, salvage exit,
-- reconstruction and curse scoping rather than constructing a boss directly.
local function enter_second_milestone(session, first_biome_id, tier_three_biome_id)
  local first = enter_milestone(session, first_biome_id)
  first.health = 1
  assert(session:_damage_boss(1).dead)
  assert(session.state.phase == "boss_exit")
  session.state.player.x, session.state.player.y = session.state.exit.x, session.state.exit.y
  assert(session:turn("") == "reconstruction")
  assert(session:complete_reconstruction().next == "curse")
  assert(session:choose_curse(session.state.curse_options[1]).next == "route")
  for _, node in ipairs(session:available_route_nodes()) do
    if node.biome_id == tier_three_biome_id then
      assert(session:select_route_node(node.id).applied)
      break
    end
  end
  assert(session.state.settings.biome_id == tier_three_biome_id)
  assert(session:_complete_stage() == "reconstruction")
  assert(session:complete_reconstruction().next == "boss")
  return assert(session.state.boss)
end

-- The final service hub now deliberately precedes the universal Apex. This
-- helper walks the real 8I transition, keeping route history explicit for
-- both Wild and Industrial terminal-lineage tests.
local function enter_apex(session, first_biome_id, tier_three_biome_id)
  local second = enter_second_milestone(session, first_biome_id, tier_three_biome_id)
  second.health = 1
  assert(session:_damage_boss(1).dead)
  session.state.player.x, session.state.player.y = session.state.exit.x, session.state.exit.y
  assert(session:turn("") == "reconstruction")
  assert(session:complete_reconstruction().next == "shop")
  assert(session.state.route:node(session.state.route.current_node_id).type == "shop")
  session:start_boss()
  assert(session.state.boss.boss_id == "boss.apex.kinetic_harbinger")
  return session.state.boss
end

local function enter_terminal_after_apex(session, first_biome_id, tier_three_biome_id)
  local apex = enter_apex(session, first_biome_id, tier_three_biome_id)
  apex.health = 1
  assert(session:_damage_boss(1).dead)
  session.state.player.x, session.state.player.y = session.state.exit.x, session.state.exit.y
  assert(session:turn("") == "reconstruction")
  assert(session:complete_reconstruction().next == "boss")
  return assert(session.state.boss)
end

local function count_events(session, suffix)
  local count = 0
  for _, event in ipairs(session.state.meta_reward_events) do
    if event.id:find(suffix, 1, true) then count = count + 1 end
  end
  return count
end

return {
  {
    name = "production milestone bosses construct ordinary physical bodies with live providers",
    run = function()
      local forest = enter_milestone(new_run(97002), "biome.legacy.forest")
      assert(forest.boss_id == "boss.forest.iron_colossus")
      assert(forest.body:get_component("left_arm").definition_id == "component.arm.pile_driver")
      assert(forest.body:get_component("right_arm").definition_id == "component.arm.siege_emitter")
      assert(forest.body:has_capability("ability.weapon.melee.pile_driver"))
      assert(forest.body:has_capability("ability.weapon.projectile.siege"))

      local cave = enter_milestone(new_run(97003), "biome.legacy.cave")
      assert(cave.boss_id == "boss.cave.flooded_conductor")
      assert(cave.body:has_capability("ability.electrical.discharge"))
      assert(cave.body:has_capability("ability.weapon.melee.basic"))
      assert(cave.body:has_capability("ability.weapon.projectile.siege"))
    end,
  },
  {
    name = "boss telegraph is provider-bound cancellable and persists through save resume",
    run = function()
      local session = new_run(97004)
      local boss = enter_milestone(session, "biome.legacy.forest")
      -- An uncluttered horizontal line gives the siege provider a valid shared
      -- projectile request without relying on the authored cover arrangement.
      boss.x, boss.y = 26, 4
      session.state.player.x, session.state.player.y = 8, 4
      session:_boss_turn()
      local pending = assert(boss.pending_telegraph)
      assert(pending.ability_id == "ability.weapon.projectile.siege")
      local saved = assert(ActiveRun.encode_session(session))
      local restored = assert(ActiveRun.decode_session(saved))
      local restored_boss = restored.state.boss
      assert(restored_boss.pending_telegraph.ability_id == pending.ability_id)
      assert(restored_boss.pending_telegraph.provider_component_id == pending.provider_component_id)
      assert(restored_boss.pending_telegraph.remaining == pending.remaining)

      assert(restored:damage_actor_body(restored_boss, { amount = 99, slot_id = "right_arm", cause = "test" }).became_broken)
      restored:_boss_turn()
      assert(restored_boss.pending_telegraph == nil)
      assert(#restored.state.bullets == 0)
      assert(not restored:actor_has_capability(restored_boss, "ability.weapon.projectile.siege"))
    end,
  },
  {
    name = "boss locomotion and electrical capability obey ordinary component state",
    run = function()
      local session = new_run(97005)
      local boss = enter_milestone(session, "biome.legacy.cave")
      assert(session:locomotion_state(boss).state == "NORMAL")
      assert(session:damage_actor_body(boss, { amount = 99, slot_id = "left_leg", cause = "test" }).became_broken)
      assert(session:locomotion_state(boss).state == "IMPAIRED")
      assert(session:damage_actor_body(boss, { amount = 99, slot_id = "right_leg", cause = "test" }).became_broken)
      assert(session:locomotion_state(boss).state == "CRAWLING")
      assert(session:actor_has_capability(boss, "ability.electrical.discharge"))
      assert(session:damage_actor_body(boss, { amount = 99, slot_id = "internal_1", cause = "test" }).became_broken)
      -- Arc Blade is a second real provider on this boss; only breaking both
      -- removes the shared electrical capability.
      assert(session:actor_has_capability(boss, "ability.electrical.discharge"))
      assert(session:damage_actor_body(boss, { amount = 99, slot_id = "left_arm", cause = "test" }).became_broken)
      assert(not session:actor_has_capability(boss, "ability.electrical.discharge"))
    end,
  },
  {
    name = "boss melee uses shared Force and the cave boss can self-shock on its water network",
    run = function()
      local physical_session = new_run(97009)
      local physical = enter_milestone(physical_session, "biome.legacy.forest")
      physical.x, physical.y = 18, 7
      physical_session.state.player.x, physical_session.state.player.y = 19, 7
      physical_session.state.player.health = 5
      local strike = physical_session:activate_actor_ability(physical, "ability.weapon.melee.pile_driver", { direction = "d" })
      assert(strike.applied and strike.force and strike.force.applied)
      assert(physical_session.state.player.x == 22 and physical_session.state.player.y == 7)
      assert(physical_session.state.player.health <= 3) -- melee plus existing spike entry.

      local electrical_session = new_run(97010)
      local electrical = enter_milestone(electrical_session, "biome.legacy.cave")
      electrical.x, electrical.y = 21, 9
      electrical_session.state.player.x, electrical_session.state.player.y = 8, 4
      local health = electrical.health
      local discharge = electrical_session:activate_actor_ability(electrical, "ability.electrical.discharge", { direction = "a" })
      assert(discharge.applied and electrical.health == health - 1)
    end,
  },
  {
    name = "milestone boss death creates a salvageable exact physical corpse and one DATA event",
    run = function()
      local session = new_run(97006)
      local boss = enter_milestone(session, "biome.legacy.forest")
      local pile_driver = boss.body:get_component("left_arm")
      boss.health = 1
      assert(session:_damage_boss(1).dead)
      assert(session.state.phase == "boss_exit" and session.state.boss == nil)
      assert(#session.state.corpses == 1 and count_events(session, "route_node:") == 3)
      local corpse = session.state.corpses[1]
      assert(corpse.body:get_component("left_arm") == pile_driver)
      session.state.player.x, session.state.player.y = corpse.x - 1, corpse.y
      local salvaged = session:salvage_corpse_component(corpse.id, "left_arm")
      assert(salvaged.applied and salvaged.component_id == pile_driver.id)
      assert(session.state.inventory:get(pile_driver.id).item.object == pile_driver)
      assert(Component.condition(pile_driver) == Component.condition(session.state.inventory:get(pile_driver.id).item.object))
      local rewards = count_events(session, "route_node:")
      assert(session:_defeat_boss(boss) == false and count_events(session, "route_node:") == rewards)
    end,
  },
  {
    name = "completed milestone corpse and stripped boss body survive save resume without rematerialization",
    run = function()
      local session = new_run(97007)
      local boss = enter_milestone(session, "biome.legacy.forest")
      local component_id = boss.body:get_component("right_arm").id
      boss.health = 1
      session:_damage_boss(1)
      local corpse = session.state.corpses[1]
      session.state.player.x, session.state.player.y = corpse.x - 1, corpse.y
      assert(session:salvage_corpse_component(corpse.id, "right_arm").applied)
      local restored = assert(ActiveRun.decode_session(assert(ActiveRun.encode_session(session))))
      assert(restored.state.phase == "boss_exit" and restored.state.boss == nil)
      assert(#restored.state.corpses == 1)
      assert(restored.state.corpses[1].body:get_component("right_arm") == nil)
      assert(restored.state.inventory:get(component_id).item.object.id == component_id)
    end,
  },
  {
    name = "the Wild final hub explicitly resolves Apex then the physical Legacy Warden",
    run = function()
      local session = new_run(97008)
      local route = session.state.route
      local shop, apex, final
      for _, id in ipairs(route.node_order) do
        local node = route:node(id)
        if node.key == "legacy_shop" then shop = node end
        if node.key == "wild_apex_boss" then apex = node end
        if node.key == "legacy_final_boss" then final = node end
      end
      route.current_node_id = shop.id
      route.completed_node_ids[shop.id] = nil
      route.path = { shop.id }
      session:start_boss()
      assert(session.state.route.current_node_id == apex.id)
      assert(session.state.boss.boss_id == "boss.apex.kinetic_harbinger")
      session.state.boss.health = 1
      assert(session:_damage_boss(1).dead)
      session.state.player.x, session.state.player.y = session.state.exit.x, session.state.exit.y
      assert(session:turn("") == "reconstruction")
      assert(session:complete_reconstruction().next == "boss")
      assert(session.state.route.current_node_id == final.id and session.state.boss.boss_id == "boss.legacy.final")
      assert(session.state.boss.body:has_capability("ability.arcane.burst"))
      assert(session.state.boss.body:has_capability("ability.weapon.projectile.heavy"))
    end,
  },
  {
    name = "legacy static boss saves upgrade safely to the physical final boss on resume",
    run = function()
      local session = new_run(97011)
      local route = session.state.route
      for _, id in ipairs(route.node_order) do
        local node = route:node(id)
        if node.key == "legacy_shop" then route.current_node_id = node.id; route.completed_node_ids[node.id] = nil end
        if node.key == "legacy_final_boss" then route.path = { node.id } end
      end
      session:start_boss()
      local data = session:to_data()
      data.boss = { kind = "boss", x = 16, y = 1, health = 6, attack = 2, name = "CROSSFIRE" }
      local restored = Session.from_data(data)
      assert(restored.state.boss.body and restored.state.boss.boss_id == "boss.legacy.final")
      assert(restored.state.boss.health == 6 and restored:validate_physical_ownership())
    end,
  },
  {
    name = "wild second milestone has a physical force body, safe environmental arena and cancellable telegraph",
    run = function()
      local session = new_run(97012)
      local boss = enter_second_milestone(session, "biome.legacy.forest", "biome.legacy.cave")
      assert(boss.boss_id == "boss.wild.ash_mauler")
      assert(boss.body:get_component("left_arm").definition_id == "component.arm.impact_maul")
      assert(boss.body:get_component("right_arm").definition_id == "component.arm.legacy_arcane_projector")
      assert(session:actor_has_capability(boss, "ability.weapon.melee.impact_maul"))
      assert(session:actor_has_capability(boss, "ability.arcane.burst"))
      assert(#session.state.world:list_gases() == 2)
      assert(#session.state.world:list_fires() == 1)
      assert(not session.state.world:is_harmful_gas_at(session.state.player.x, session.state.player.y))
      assert(#session.state.world:fires_at(session.state.player.x, session.state.player.y) == 0)

      session.state.player.x, session.state.player.y = boss.x + 1, boss.y
      session:_boss_turn()
      local pending = assert(boss.pending_telegraph)
      assert(pending.ability_id == "ability.weapon.melee.impact_maul")
      assert(session:damage_actor_body(boss, { amount = 99, slot_id = "left_arm", cause = "test" }).became_broken)
      session:_boss_turn()
      assert(boss.pending_telegraph == nil)
      assert(not session:actor_has_capability(boss, "ability.weapon.melee.impact_maul"))
      assert(session:actor_has_capability(boss, "ability.arcane.burst"))
    end,
  },
  {
    name = "industrial second milestone keeps redundant barrage until both real providers break and uses ordinary power",
    run = function()
      local session = new_run(97013)
      local boss = enter_second_milestone(session, "biome.legacy.cave", "biome.legacy.dungeon")
      local world = session.state.world
      assert(boss.boss_id == "boss.industrial.barrage_custodian")
      assert(boss.body:get_component("left_arm").definition_id == "component.arm.barrage_emitter")
      assert(boss.body:get_component("right_arm").definition_id == "component.arm.barrage_emitter")
      assert(world:is_conductive_at(18, 8))
      assert(world:is_circuit_powered("power.circuit.boss_industrial_control"))
      local breaker, door
      for _, object in ipairs(world:list_objects()) do
        if object.interaction_role == "breaker" then breaker = object end
        if object.interaction_role == "door" then door = object end
      end
      assert(breaker and door and door.door_state == "closed")
      session.state.player.x, session.state.player.y = breaker.x - 1, breaker.y
      assert(session:interact(nil, breaker.id, "breaker.toggle").applied)
      assert(not world:is_circuit_powered(breaker.circuit_id))
      assert(session:interact(nil, breaker.id, "breaker.toggle").applied)
      session.state.player.x, session.state.player.y = door.x - 1, door.y
      assert(session:interact(nil, door.id, "door.open").applied)
      assert(door.door_state == "open")

      session.state.player.x, session.state.player.y = 8, 4
      boss.x, boss.y = 26, 4
      session:_boss_turn()
      local pending = assert(boss.pending_telegraph)
      assert(pending.ability_id == "ability.weapon.projectile.barrage")
      assert(session:damage_actor_body(boss, { amount = 99, slot_id = "left_arm", cause = "test" }).became_broken)
      session:_boss_turn()
      assert(boss.pending_telegraph == nil)
      assert(session:actor_has_capability(boss, "ability.weapon.projectile.barrage"))
      session:_boss_turn()
      assert(assert(boss.pending_telegraph).provider_component_id == boss.body:get_component("right_arm").id)
      assert(session:damage_actor_body(boss, { amount = 99, slot_id = "right_arm", cause = "test" }).became_broken)
      assert(not session:actor_has_capability(boss, "ability.weapon.projectile.barrage"))
    end,
  },
  {
    name = "second milestone corpse grants three DATA once and reconstructs exact exceptional salvage before the hub",
    run = function()
      local session = new_run(97014)
      local boss = enter_second_milestone(session, "biome.legacy.forest", "biome.legacy.cave")
      local maul = boss.body:get_component("left_arm")
      boss.health = 1
      assert(session:_damage_boss(1).dead)
      assert(session.state.phase == "boss_exit" and session.state.boss_completed == "boss.wild.ash_mauler")
      local reward
      for _, event in ipairs(session.state.meta_reward_events) do
        if event.id:find(session.state.route.current_node_id, 1, true) then reward = event end
      end
      assert(reward and reward.amount == 3)
      local corpse = session.state.corpses[#session.state.corpses]
      session.state.player.x, session.state.player.y = corpse.x - 1, corpse.y
      assert(session:salvage_corpse_component(corpse.id, "left_arm").component_id == maul.id)
      session.state.player.x, session.state.player.y = session.state.exit.x, session.state.exit.y
      assert(session:turn("") == "reconstruction")
      assert(session:uninstall_body_component("left_arm").applied)
      assert(session:install_inventory_component(maul.id, "left_arm").applied)
      assert(session.state.player.body:get_component("left_arm").id == maul.id)
      assert(session:actor_has_capability(session.state.player, "ability.weapon.melee.impact_maul"))
      -- The salvaged instance drives the ordinary shared player activation,
      -- rather than a boss-only reward implementation.
      session.state.phase = "combat"
      session.state.player.x, session.state.player.y = 8, 9
      session.state.enemies = { session:_make_enemy("enemy.wild.ripper", { x = 9, y = 9 }) }
      session.state.enemies[1].health = 5
      local swing = session:activate_actor_ability(session.state.player, "ability.weapon.melee.impact_maul", { direction = "d" })
      assert(swing.applied and swing.component_id == maul.id and swing.force.applied)
      session.state.phase = "reconstruction"
      assert(session:complete_reconstruction().next == "shop")
      local count = #session.state.meta_reward_events
      assert(session:_defeat_boss(boss) == false and #session.state.meta_reward_events == count)
    end,
  },
  {
    name = "industrial second milestone barrage emitter transfers through corpse and reconstruction into player fire",
    run = function()
      local session = new_run(970141)
      local boss = enter_second_milestone(session, "biome.legacy.cave", "biome.legacy.dungeon")
      local emitter = boss.body:get_component("right_arm")
      boss.health = 1
      assert(session:_damage_boss(1).dead)
      local corpse = session.state.corpses[#session.state.corpses]
      session.state.player.x, session.state.player.y = corpse.x - 1, corpse.y
      assert(session:salvage_corpse_component(corpse.id, "right_arm").component_id == emitter.id)
      session.state.player.x, session.state.player.y = session.state.exit.x, session.state.exit.y
      assert(session:turn("") == "reconstruction")
      assert(session:uninstall_body_component("right_arm").applied)
      assert(session:install_inventory_component(emitter.id, "right_arm").applied)
      assert(session.state.player.body:get_component("right_arm").id == emitter.id)
      session.state.phase = "combat"
      session.state.player.x, session.state.player.y = 8, 4
      local fired = session:activate_actor_ability(session.state.player, "ability.weapon.projectile.barrage", { direction = "d" })
      assert(fired.applied and fired.component_id == emitter.id and #session.state.bullets == 1)
    end,
  },
  {
    name = "second milestone live telegraph and post-death stripped corpse save resume exactly",
    run = function()
      local session = new_run(97015)
      local boss = enter_second_milestone(session, "biome.legacy.cave", "biome.legacy.reactor")
      session.state.player.x, session.state.player.y = 8, 4
      boss.x, boss.y = 26, 4
      session:_boss_turn()
      local pending = assert(boss.pending_telegraph)
      local restored = assert(ActiveRun.decode_session(assert(ActiveRun.encode_session(session))))
      assert(restored.state.boss.boss_id == "boss.industrial.barrage_custodian")
      assert(restored.state.boss.pending_telegraph.ability_id == pending.ability_id)
      local live = restored.state.boss
      local component_id = live.body:get_component("right_arm").id
      live.health = 1
      assert(restored:_damage_boss(1).dead)
      local corpse = restored.state.corpses[#restored.state.corpses]
      restored.state.player.x, restored.state.player.y = corpse.x - 1, corpse.y
      assert(restored:salvage_corpse_component(corpse.id, "right_arm").applied)
      local resumed = assert(ActiveRun.decode_session(assert(ActiveRun.encode_session(restored))))
      assert(resumed.state.phase == "boss_exit" and resumed.state.boss == nil)
      assert(resumed.state.corpses[#resumed.state.corpses].body:get_component("right_arm") == nil)
      assert(resumed.state.inventory:get(component_id).item.object.id == component_id)
    end,
  },
  {
    name = "second milestone boss part survives player death archive and future recurrence lineage",
    run = function()
      local source = Session.new({ seed = 97016, run_id = "run:009716" })
      source:start_run(Content.classes[1], Content.boons[1])
      local boss = enter_second_milestone(source, "biome.legacy.forest", "biome.legacy.cave")
      local maul = boss.body:get_component("left_arm")
      boss.health = 1
      assert(source:_damage_boss(1).dead)
      local corpse = source.state.corpses[#source.state.corpses]
      source.state.player.x, source.state.player.y = corpse.x - 1, corpse.y
      assert(source:salvage_corpse_component(corpse.id, "left_arm").component_id == maul.id)
      source.state.player.x, source.state.player.y = source.state.exit.x, source.state.exit.y
      assert(source:turn("") == "reconstruction")
      assert(source:uninstall_body_component("left_arm").applied)
      assert(source:install_inventory_component(maul.id, "left_arm").applied)
      source:_mark_player_dead({ cause = "boss_lineage" })
      local archive = FallenArchive.new()
      assert(FallenArchive.append(archive, source.state.death_pending_archive).applied)
      local record = archive.characters[1]
      local archived
      for _, slot in ipairs(record.body.slots) do
        if slot.slot_id == "left_arm" then archived = slot.component end
      end
      assert(archived.id == maul.id and archived.definition_id == "component.arm.impact_maul")

      local spec = assert(Recurrence.assign("run:009717", 97017, archive.characters))
      spec.mode, spec.target_depth = "corpse", 2
      local future = Session.new({ seed = 97017, registry = source.registry, run_id = "run:009717", fallen_recurrence = spec })
      future:start_run()
      assert(future:_complete_stage() == "reconstruction")
      assert(future:complete_reconstruction().next == "curse")
      assert(future:choose_curse(future.state.curse_options[1]).next == "route")
      assert(future:select_route_node(future:available_route_nodes()[1].id).applied)
      local echo_corpse = assert(future.state.corpses[1])
      local materialized = echo_corpse.body:get_component("left_arm")
      assert(materialized.definition_id == "component.arm.impact_maul" and materialized.id ~= maul.id)
      assert(materialized.origin.archive_id == record.id and materialized.origin.source_component_id == maul.id)
    end,
  },
  {
    name = "Apex uses shared physical systems, cancels a disabled telegraph, and yields installable Vector Lance salvage",
    run = function()
      local session = new_run(97017)
      local apex = enter_apex(session, "biome.legacy.forest", "biome.legacy.cave")
      local lance = apex.body:get_component("left_arm")
      assert(lance.definition_id == "component.arm.vector_lance")
      assert(session:actor_has_capability(apex, "ability.weapon.melee.basic"))
      assert(session:actor_has_capability(apex, "ability.weapon.projectile.barrage"))
      assert(session:actor_has_capability(apex, "ability.weapon.projectile.siege"))
      assert(session:locomotion_state(apex).state == "NORMAL")

      apex.x, apex.y = 26, 4
      session.state.player.x, session.state.player.y = 8, 4
      session:_boss_turn()
      local pending = assert(apex.pending_telegraph)
      assert(pending.ability_id == "ability.weapon.projectile.siege")
      local restored = assert(ActiveRun.decode_session(assert(ActiveRun.encode_session(session))))
      apex = restored.state.boss
      assert(apex.pending_telegraph.ability_id == "ability.weapon.projectile.siege")
      assert(restored:damage_actor_body(apex, { amount = 99, slot_id = "right_arm", cause = "test" }).became_broken)
      restored:_boss_turn()
      assert(apex.pending_telegraph == nil)
      assert(not restored:actor_has_capability(apex, "ability.weapon.projectile.siege"))
      assert(restored:actor_has_capability(apex, "ability.weapon.melee.basic"))

      apex.health = 1
      assert(restored:_damage_boss(1).dead)
      local corpse = restored.state.corpses[#restored.state.corpses]
      restored.state.player.x, restored.state.player.y = corpse.x - 1, corpse.y
      assert(restored:salvage_corpse_component(corpse.id, "left_arm").component_id == lance.id)
      restored = assert(ActiveRun.decode_session(assert(ActiveRun.encode_session(restored))))
      assert(restored.state.phase == "boss_exit" and restored.state.boss == nil)
      assert(restored.state.corpses[#restored.state.corpses].body:get_component("left_arm") == nil)
      assert(restored.state.inventory:get(lance.id).item.object.id == lance.id)
      restored.state.player.x, restored.state.player.y = restored.state.exit.x, restored.state.exit.y
      assert(restored:turn("") == "reconstruction")
      restored = assert(ActiveRun.decode_session(assert(ActiveRun.encode_session(restored))))
      assert(restored.state.phase == "reconstruction" and restored.state.reconstruction_next == "boss")
      assert(restored:uninstall_body_component("left_arm").applied)
      assert(restored:install_inventory_component(lance.id, "left_arm").applied)
      assert(restored:actor_has_capability(restored.state.player, "ability.weapon.melee.basic"))
      assert(restored:actor_has_capability(restored.state.player, "ability.weapon.projectile.barrage"))
      local reward
      for _, event in ipairs(restored.state.meta_reward_events) do
        if event.id:find(restored.state.route.current_node_id, 1, true) then reward = event end
      end
      assert(reward and reward.amount == 3)
    end,
  },
  {
    name = "Industrial terminal has two provider-backed telegraphs, ordinary powered geometry, redundancy, and final victory",
    run = function()
      local electrical_session = new_run(97018)
      local electrical = enter_terminal_after_apex(electrical_session, "biome.legacy.forest", "biome.legacy.dungeon")
      assert(electrical.boss_id == "boss.industrial.terminal_bastion")
      assert(electrical.body:get_component("left_arm").definition_id == "component.arm.barrage_emitter")
      assert(electrical.body:get_component("right_arm").definition_id == "component.arm.barrage_emitter")
      local world = electrical_session.state.world
      assert(world:is_circuit_powered("power.circuit.boss_terminal_control"))
      local breaker, doors
      doors = 0
      for _, object in ipairs(world:list_objects()) do
        if object.interaction_role == "breaker" then breaker = object end
        if object.interaction_role == "door" then doors = doors + 1 end
      end
      assert(breaker and doors == 2)
      electrical_session.state.player.x, electrical_session.state.player.y = breaker.x - 1, breaker.y
      assert(electrical_session:interact(nil, breaker.id, "breaker.toggle").applied)
      assert(not world:is_circuit_powered(breaker.circuit_id))
      assert(electrical_session:interact(nil, breaker.id, "breaker.toggle").applied)

      electrical.x, electrical.y = 23, 7
      electrical_session.state.player.x, electrical_session.state.player.y = 17, 7
      electrical_session:_boss_turn()
      assert(assert(electrical.pending_telegraph).ability_id == "ability.electrical.discharge")
      electrical_session = assert(ActiveRun.decode_session(assert(ActiveRun.encode_session(electrical_session))))
      electrical = electrical_session.state.boss
      assert(electrical.pending_telegraph and electrical.pending_telegraph.ability_id == "ability.electrical.discharge")
      assert(electrical_session:damage_actor_body(electrical, { amount = 99, slot_id = "internal_1", cause = "test" }).became_broken)
      electrical_session:_boss_turn()
      assert(electrical.pending_telegraph == nil)
      assert(not electrical_session:actor_has_capability(electrical, "ability.electrical.discharge"))

      local barrage_session = new_run(97019)
      local barrage = enter_terminal_after_apex(barrage_session, "biome.legacy.cave", "biome.legacy.reactor")
      barrage.x, barrage.y = 26, 4
      barrage_session.state.player.x, barrage_session.state.player.y = 8, 4
      barrage_session:_boss_turn()
      local pending = assert(barrage.pending_telegraph)
      assert(pending.ability_id == "ability.weapon.projectile.barrage")
      local first_provider = pending.provider_component_id
      assert(barrage_session:damage_actor_body(barrage, { amount = 99, component_id = first_provider, cause = "test" }).became_broken)
      barrage_session:_boss_turn()
      assert(barrage.pending_telegraph == nil)
      assert(barrage_session:actor_has_capability(barrage, "ability.weapon.projectile.barrage"))
      barrage_session:_boss_turn()
      local second_provider = assert(barrage.pending_telegraph).provider_component_id
      assert(second_provider ~= first_provider)
      assert(barrage_session:damage_actor_body(barrage, { amount = 99, component_id = second_provider, cause = "test" }).became_broken)
      assert(not barrage_session:actor_has_capability(barrage, "ability.weapon.projectile.barrage"))
      barrage.health = 1
      assert(barrage_session:_damage_boss(1).dead)
      assert(barrage_session.state.ended == "victory" and barrage_session.state.boss == nil)
      local reward = barrage_session.state.meta_reward_events[#barrage_session.state.meta_reward_events]
      assert(reward.amount == 4)
    end,
  },
  {
    name = "Apex Vector Lance follows corpse to player death archive and future recurrence with fresh live identity",
    run = function()
      local source = Session.new({ seed = 97020, run_id = "run:009720" })
      source:start_run(Content.classes[1], Content.boons[1])
      local apex = enter_apex(source, "biome.legacy.forest", "biome.legacy.cave")
      local lance = apex.body:get_component("left_arm")
      apex.health = 1
      assert(source:_damage_boss(1).dead)
      local corpse = source.state.corpses[#source.state.corpses]
      source.state.player.x, source.state.player.y = corpse.x - 1, corpse.y
      assert(source:salvage_corpse_component(corpse.id, "left_arm").component_id == lance.id)
      source.state.player.x, source.state.player.y = source.state.exit.x, source.state.exit.y
      assert(source:turn("") == "reconstruction")
      assert(source:uninstall_body_component("left_arm").applied)
      assert(source:install_inventory_component(lance.id, "left_arm").applied)
      source:_mark_player_dead({ cause = "apex_lineage" })
      local archive = FallenArchive.new()
      assert(FallenArchive.append(archive, source.state.death_pending_archive).applied)
      local record = archive.characters[1]
      local archived
      for _, slot in ipairs(record.body.slots) do if slot.slot_id == "left_arm" then archived = slot.component end end
      assert(archived.id == lance.id and archived.definition_id == "component.arm.vector_lance")

      local spec = assert(Recurrence.assign("run:009721", 97021, archive.characters))
      spec.mode, spec.target_depth = "corpse", 2
      local future = Session.new({ seed = 97021, registry = source.registry, run_id = "run:009721", fallen_recurrence = spec })
      future:start_run()
      assert(future:_complete_stage() == "reconstruction")
      assert(future:complete_reconstruction().next == "curse")
      assert(future:choose_curse(future.state.curse_options[1]).next == "route")
      assert(future:select_route_node(future:available_route_nodes()[1].id).applied)
      local echo = assert(future.state.corpses[1]).body:get_component("left_arm")
      assert(echo.definition_id == "component.arm.vector_lance" and echo.id ~= lance.id)
      assert(echo.origin.archive_id == record.id and echo.origin.source_component_id == lance.id)
    end,
  },
}
