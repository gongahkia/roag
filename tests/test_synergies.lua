local BuildEffects = require("src.simulation.build_effects")
local Campaign = require("src.campaign.campaign")
local Content = require("src.content.legacy")
local GameplayUI = require("src.presentation.gameplay_ui")
local Grid = require("src.world.grid")
local Loadout = require("src.simulation.loadout")
local Session = require("src.simulation.session")
local World = require("src.world.world")

local function open_session(seed, blocked)
  local session = Session.new({ seed = seed })
  session:start_run(Content.classes[1], Content.boons[1])
  local cells = {}
  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do
      if not (blocked and blocked[Grid.key(x, y)]) then cells[Grid.key(x, y)] = true end
    end
  end
  session.state.world = World.new(session.registry, "cave", cells, session.state)
  session.state.phase, session.state.enemies, session.state.bullets = "combat", {}, {}
  session.state.player.x, session.state.player.y = 10, 10
  return session
end

local function install(session, slot, definition_id)
  session.state.player.body:detach(slot)
  local component = session.component_factory:create(definition_id)
  assert(session.state.player.body:install(slot, component))
  return component
end

local function enemy(session, id, x, y, health)
  local result = session:_make_enemy(id, { x = x, y = y })
  result.health = health or result.health
  return result
end

local function trace_has(session, effect_id)
  for _, entry in ipairs(session:build_effect_trace()) do
    if entry.kind == "effect" and entry.effect_id == effect_id then return true end
  end
  return false
end

local function arc_result(seed, conductive)
  local session = open_session(seed)
  install(session, "right_arm", "component.arm.piercing_lance")
  install(session, "internal_2", "component.internal.legacy_shock_coil")
  session.state.charms.slots = { "charm.legacy.arc_relay" }
  if conductive then
    for x = 11, 14 do assert(session.state.world:add_liquid(x, 10, "liquid.water.legacy", 1).applied) end
  end
  local first, second = enemy(session, "enemy.wild.ripper", 11, 10, 5), enemy(session, "enemy.wild.ripper", 13, 10, 5)
  session.state.enemies = { first, second }
  assert(session:activate_actor_ability(session.state.player, "ability.weapon.piercing_lance", { direction = "d" }).applied)
  for _ = 1, 5 do session:_update_bullets() end
  return session, first.health, second.health
end

return {
  {
    name = "build-effect content declares stable tags and validated reactive charm definitions",
    run = function()
      local registry = require("src.content.registry").load()
      assert(registry:get_ability("ability.weapon.piercing_lance").attack_tags[2] == "piercing")
      assert(registry:get_charm("charm.legacy.arc_relay").reactive_effects[1].trigger == "on_pierce")
      assert(registry:get_charm("charm.legacy.recycler").reactive_effects[1].effect.kind == "magazine_refund")
      assert(BuildEffects.ATTACK_TAGS.forceful and BuildEffects.TRIGGERS.on_component_break)
    end,
  },
  {
    name = "Arc Relay uses the actual conductive network after a piercing hit",
    run = function()
      local wet, _, wet_second = arc_result(950001, true)
      local dry, _, dry_second = arc_result(950001, false)
      assert(wet_second < dry_second and trace_has(wet, "arc_relay"))
      assert(trace_has(dry, "arc_relay"), "the deterministic charm still resolves on a valid pierce")
      assert(dry_second == 3, "dry geometry supplies no conductive secondary damage")
    end,
  },
  {
    name = "Kinetic Feedback is driven by real structural Force collision and guards self recursion",
    run = function()
      local session = open_session(950002, { [Grid.key(13, 10)] = true })
      install(session, "left_arm", "component.arm.hydraulic_ram")
      session.state.charms.slots = { "charm.legacy.kinetic_feedback", "charm.legacy.kinetic_capacitor" }
      local slammed, nearby = enemy(session, "enemy.wild.ripper", 11, 10, 10), enemy(session, "enemy.wild.ripper", 12, 11, 10)
      session.state.enemies = { slammed, nearby }
      assert(session:activate_actor_ability(session.state.player, "ability.weapon.melee.ram", { direction = "d" }).applied)
      assert(slammed.x == 12 and trace_has(session, "kinetic_feedback"))
      local effects = 0
      for _, entry in ipairs(session:build_effect_trace()) do if entry.effect_id == "kinetic_feedback" then effects = effects + 1 end end
      assert(effects == 1, "a derived collision may occur but the same charm cannot recurse into itself")
    end,
  },
  {
    name = "Recycler refunds only active physical ranged magazines and may refund multiple kills",
    run = function()
      local campaign = Campaign.new({ seed = 950003, campaign_id = "campaign:950003" })
      local session = campaign.session
      session.state.charms.slots = { "charm.legacy.recycler" }
      local loadout = session:campaign_loadout()
      local resolved = assert(Loadout.resolve(session, session.state.player, loadout.weapon_slots[loadout.active_weapon], "weapon"))
      local magazine = session:weapon_magazine(resolved.provider, resolved.ability)
      magazine.loaded = 0
      for _ = 1, 3 do
        assert(session:_emit_build_event({ type = "on_kill", source_actor = session.state.player,
          attack_tags = { projectile = true }, weapon_ability = resolved.ability, provider = resolved.provider }).applied)
      end
      assert(magazine.loaded == 3 and session:ammo_reserve(resolved.ability.ammo.family) >= 0)
      assert(magazine.loaded <= resolved.ability.ammo.magazine_capacity)
    end,
  },
  {
    name = "a pierced Arc Relay kill remains player-attributed and chains into Recycler",
    run = function()
      local campaign = Campaign.new({ seed = 950030, campaign_id = "campaign:950030" })
      local session, player = campaign.session, campaign.session.state.player
      local cells = {}
      for x = 0, Grid.width - 1 do
        for y = 0, Grid.height - 1 do cells[Grid.key(x, y)] = true end
      end
      session.state.world = World.new(session.registry, "cave", cells, session.state)
      session.state.phase, session.state.enemies, session.state.bullets = "combat", {}, {}
      player.x, player.y, player.direction = 10, 10, "d"
      local lance = install(session, "right_arm", "component.arm.piercing_lance")
      install(session, "internal_2", "component.internal.legacy_shock_coil")
      assert(session:assign_campaign_loadout("weapon", 1, {
        source_kind = "component", physical_id = lance.id, ability_id = "ability.weapon.piercing_lance",
      }).applied)
      session:campaign_loadout().active_weapon = 1
      local resolved = assert(Loadout.resolve(session, player, session:campaign_loadout().weapon_slots[1], "weapon"))
      local magazine = session:weapon_magazine(resolved.provider, resolved.ability)
      magazine.loaded = 1
      session.state.charms.slots = { "charm.legacy.arc_relay", "charm.legacy.recycler" }
      for x = 11, 14 do assert(session.state.world:add_liquid(x, 10, "liquid.water.legacy", 1).applied) end
      local pierced = enemy(session, "enemy.wild.ripper", 11, 10, 3)
      local shocked = enemy(session, "enemy.wild.ripper", 13, 10, 1)
      session.state.enemies = { pierced, shocked }
      assert(session:attack_active_weapon().applied)
      for _ = 1, 5 do session:_update_bullets() end
      assert(pierced.health == 0 and shocked.health == 0)
      assert(magazine.loaded == 2, "each player-attributed kill may return one loaded round")
      assert(trace_has(session, "arc_relay") and trace_has(session, "recycler"))
    end,
  },
  {
    name = "Quick Reload observes successful physical reloads without firing and reduces Dash recharge",
    run = function()
      local campaign = Campaign.new({ seed = 950004, campaign_id = "campaign:950004" })
      local session = campaign.session
      session.state.charms.slots = { "charm.legacy.quick_reload" }
      local loadout = session:campaign_loadout()
      local resolved = assert(Loadout.resolve(session, session.state.player, loadout.weapon_slots[loadout.active_weapon], "weapon"))
      local magazine = session:weapon_magazine(resolved.provider, resolved.ability)
      magazine.loaded, session.state.player.dash, session.state.bullets = 0, 2, {}
      local result = session:attack_active_weapon()
      assert(result.code == "reloaded" and #session.state.bullets == 0 and session.state.player.dash == 1)
      assert(trace_has(session, "quick_reload"))
      for _, entry in ipairs(session:build_effect_trace()) do
        assert(entry.kind ~= "on_attack", "an empty magazine reload is not an attack event")
      end
    end,
  },
  {
    name = "Rupture Core reacts to actual enemy component break transitions once",
    run = function()
      local session = open_session(950005)
      install(session, "left_arm", "component.arm.impact_blade")
      install(session, "internal_2", "component.internal.legacy_volatile_charge")
      session.state.charms.slots = { "charm.legacy.rupture_core" }
      local target, neighbour = enemy(session, "enemy.wild.ripper", 11, 10, 10), enemy(session, "enemy.wild.ripper", 12, 10, 10)
      for _, component in ipairs(target.body:list_components()) do component.current_integrity = 1 end
      session.state.enemies = { target, neighbour }
      assert(session:activate_actor_ability(session.state.player, "ability.weapon.melee.basic", { direction = "d" }).applied)
      assert(target.health <= 8 and neighbour.health < 10 and trace_has(session, "rupture_core"))
      local effects = 0
      for _, entry in ipairs(session:build_effect_trace()) do if entry.effect_id == "rupture_core" then effects = effects + 1 end end
      assert(effects == 1)
    end,
  },
  {
    name = "build-effect requirements update live and the Inventory model exposes inactive explanations",
    run = function()
      local session = open_session(950006)
      session.campaign = { state = {} }
      session.state.charms.slots = { "charm.legacy.arc_relay" }
      local view = GameplayUI.build_effects(session)
      assert(#view == 1 and not view[1].active and view[1].reason:match("NO"))
      install(session, "internal_2", "component.internal.legacy_shock_coil")
      view = GameplayUI.build_effects(session)
      assert(view[1].active and view[1].description:match("projectile pierce"))
    end,
  },
  {
    name = "build chains are deterministic, ancestry-safe, and have a generous explicit budget",
    run = function()
      local session = open_session(950007)
      install(session, "internal_2", "component.internal.legacy_shock_coil")
      session.state.charms.slots = { "charm.legacy.arc_relay" }
      local first = session:_emit_build_event({ type = "on_pierce", source_actor = session.state.player,
        target_cell = { x = 11, y = 10 }, attack_tags = { projectile = true, piercing = true } })
      local second = session:_emit_build_event({ type = "on_pierce", source_actor = session.state.player,
        target_cell = { x = 11, y = 10 }, attack_tags = { projectile = true, piercing = true } })
      assert(#first.chain.budget.trace == #second.chain.budget.trace)
      local guarded = BuildEffects.derive_chain(first.chain, "charm.legacy.arc_relay:arc_relay")
      local blocked = session:_emit_build_event({ type = "on_pierce", source_actor = session.state.player,
        target_cell = { x = 11, y = 10 }, attack_tags = { projectile = true, piercing = true }, build_chain = guarded })
      assert(not blocked.applied)
      assert(BuildEffects.MAX_CHAIN_DEPTH >= 6 and BuildEffects.MAX_EXECUTIONS >= 128)
    end,
  },
}
