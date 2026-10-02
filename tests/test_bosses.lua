local ActiveRun = require("src.persistence.active_run")
local Content = require("src.content.legacy")
local Component = require("src.body.component")
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
    name = "legacy final boss node resolves to a physical final boss without a separate hitbox path",
    run = function()
      local session = new_run(97008)
      local route = session.state.route
      local shop, final
      for _, id in ipairs(route.node_order) do
        local node = route:node(id)
        if node.key == "legacy_shop" then shop = node end
        if node.key == "legacy_final_boss" then final = node end
      end
      route.current_node_id = shop.id
      route.completed_node_ids[shop.id] = nil
      route.path = { shop.id }
      session:start_boss()
      assert(session.state.route.current_node_id == final.id)
      assert(session.state.boss.boss_id == "boss.legacy.final")
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
}
