local BodyDamage = require("src.simulation.body_damage")
local Content = require("src.content.legacy")
local PhysicalItem = require("src.inventory.physical_item")
local Session = require("src.simulation.session")

local PROJECTILE = "ability.weapon.projectile.basic"
local ARCANE_BURST = "ability.arcane.burst"

local function new_run(seed)
  local session = Session.new({ seed = seed or 1501 })
  session:start_run(Content.classes[1], Content.boons[1])
  return session
end

local function enter_reconstruction(session)
  assert(session:_complete_stage() == "reconstruction")
end

local function finish_normal_reconstruction(session)
  assert(session:complete_reconstruction().next == "curse")
  session:choose_curse(Content.curses[2]) -- Darkness does not alter ammo.
end

return {
  {
    name = "ordinary player shooting requires a functional installed ranged emitter",
    run = function()
      local session = new_run(1502)
      local player = session.state.player
      local emitter = player.body:get_component("right_arm")
      local ammo = player.ammo
      local fired = session:turn("shoot_w")
      assert(fired == nil and #session.state.bullets == 1)
      assert(player.ammo == ammo - 1)
      local bullet = session.state.bullets[1]
      assert(bullet.ability_id == PROJECTILE and bullet.source_component_id == emitter.id)
      assert(bullet.source_actor == player and bullet.source_side == "player")

      session.state.bullets = {}
      assert(session:damage_actor_body(player, { amount = 3, slot_id = "right_arm", cause = "test" }).became_broken)
      local before_ammo = player.ammo
      local rejected = session:_shoot("w")
      assert(not rejected.applied and rejected.code == "provider_broken")
      assert(#session.state.bullets == 0 and player.ammo == before_ammo)

      local broken = assert(player.body:detach("right_arm"))
      assert(session.state.inventory:auto_place(PhysicalItem.from_component(broken, session.registry)))
      local replacement = session.component_factory:create("component.arm.legacy_projectile_emitter")
      assert(player.body:install("right_arm", replacement))
      local restored = session:_shoot("w")
      assert(restored.applied and restored.component_id == replacement.id)
      assert(session:validate_physical_ownership())
    end,
  },
  {
    name = "basic projectile validation is atomic for invalid direction and insufficient ammo",
    run = function()
      local session = new_run(1503)
      local player = session.state.player
      local initial_ammo = player.ammo
      local invalid = session:activate_actor_ability(player, PROJECTILE, { direction = "diagonal" })
      assert(not invalid.applied and invalid.code == "invalid_direction")
      assert(player.ammo == initial_ammo and #session.state.bullets == 0)
      player.ammo = 0
      local empty = session:activate_actor_ability(player, PROJECTILE, { direction = "w" })
      assert(not empty.applied and empty.code == "insufficient_ammo")
      assert(player.ammo == 0 and #session.state.bullets == 0)
    end,
  },
  {
    name = "projectile provider selection uses slot order and falls back after failure",
    run = function()
      local session = new_run(1504)
      local player = session.state.player
      local old_left = assert(player.body:detach("left_arm"))
      assert(session.state.inventory:auto_place(PhysicalItem.from_component(old_left, session.registry)))
      local left_emitter = session.component_factory:create("component.arm.legacy_projectile_emitter")
      assert(player.body:install("left_arm", left_emitter))
      local right_emitter = player.body:get_component("right_arm")
      assert(session:actor_ability_provider(player, PROJECTILE).component == left_emitter)

      local definition = session.registry:get_component(left_emitter.definition_id)
      local original_wear = definition.wear_per_use
      definition.wear_per_use = 1
      local first = session:_shoot("w")
      definition.wear_per_use = original_wear
      assert(first.applied and first.component_id == left_emitter.id)
      assert(left_emitter.current_integrity == 2 and right_emitter.current_integrity == 3)
      assert(session:damage_actor_body(player, { amount = 2, slot_id = "left_arm", cause = "test" }).became_broken)
      assert(session:actor_ability_provider(player, PROJECTILE).component == right_emitter)
      local second = session:_shoot("w")
      assert(second.applied and second.component_id == right_emitter.id)
      assert(session:validate_physical_ownership())
    end,
  },
  {
    name = "removing the ranged emitter in reconstruction disables normal fire until it is reinstalled",
    run = function()
      local session = new_run(1505)
      local player = session.state.player
      local emitter = player.body:get_component("right_arm")
      enter_reconstruction(session)
      local removed = session:uninstall_body_component("right_arm")
      assert(removed.applied and removed.component_id == emitter.id)
      assert(session.state.inventory:get(emitter.id).item.object == emitter)
      finish_normal_reconstruction(session)
      local before_ammo = player.ammo
      local rejected = session:_shoot("w")
      assert(not rejected.applied and rejected.code == "missing_capability")
      assert(player.ammo == before_ammo and #session.state.bullets == 0)

      enter_reconstruction(session)
      assert(session:install_inventory_component(emitter.id, "right_arm").applied)
      finish_normal_reconstruction(session)
      local restored = session:_shoot("w")
      assert(restored.applied and restored.component_id == emitter.id)
      assert(session:validate_physical_ownership())
    end,
  },
  {
    name = "cultist arcane projector controls shared non destructive enemy ability",
    run = function()
      local session = new_run(1506)
      local player = session.state.player
      local cultist = session:_make_enemy("cultist", { x = player.x, y = player.y + 3 })
      session.state.enemies = { cultist }
      local projector = cultist.body:get_component("right_arm")
      assert(cultist.content_id == "enemy.legacy.cultist")
      assert(session:actor_has_capability(cultist, ARCANE_BURST))
      session:_enemy_turn()
      assert(#session.state.area_attacks == 1)
      assert(session.state.area_attacks[1].ability_id == ARCANE_BURST)
      assert(session.state.area_attacks[1].source_component_id == projector.id)
      local health = player.health
      session:_update_area_attacks()
      session:_update_area_attacks()
      session:_update_area_attacks()
      assert(player.health == health - 1 and #session.state.area_attacks == 0)
      assert(session:damage_actor_body(cultist, { amount = 3, slot_id = "right_arm", cause = "test" }).became_broken)
      assert(not session:actor_has_capability(cultist, ARCANE_BURST))
      session:_enemy_turn()
      assert(#session.state.area_attacks == 0 and cultist.attack_kind == nil)
    end,
  },
  {
    name = "cultist projector keeps exact identity through corpse salvage reconstruction and player activation",
    run = function()
      local session = new_run(1507)
      local player = session.state.player
      local cultist = session:_make_enemy("cultist", { x = player.x, y = player.y + 1 })
      session.state.enemies[#session.state.enemies + 1] = cultist
      local projector = cultist.body:get_component("right_arm")
      local id = projector.id
      session:_destroy_enemy(#session.state.enemies)
      local corpse = session.state.corpses[#session.state.corpses]
      assert(corpse.body:get_component("right_arm") == projector)
      assert(session:salvage_corpse_component(corpse.id, "right_arm").applied)
      enter_reconstruction(session)
      assert(session:uninstall_body_component("left_arm").applied)
      assert(session:install_inventory_component(id, "left_arm").applied)
      finish_normal_reconstruction(session)
      assert(player.body:get_component("left_arm") == projector)
      assert(session:actor_has_capability(player, ARCANE_BURST))
      local burst = session:activate_actor_ability(player, ARCANE_BURST, { direction = "w" })
      assert(burst.applied and burst.implementation == "area_burst")
      assert(burst.component_id == id and burst.burst.source_component_id == id)
      assert(session:validate_physical_ownership())
    end,
  },
  {
    name = "shared projectile implementation accepts an enemy source without player assumptions",
    run = function()
      local session = new_run(1508)
      local player = session.state.player
      local cultist = session:_make_enemy("cultist", { x = player.x, y = player.y + 4 })
      local old_projector = assert(cultist.body:detach("right_arm"))
      local emitter = session.component_factory:create("component.arm.legacy_projectile_emitter")
      assert(cultist.body:install("right_arm", emitter))
      cultist.ammo = 1
      local result = session:activate_actor_ability(cultist, PROJECTILE, { direction = "s" })
      assert(result.applied and result.projectile.source_actor == cultist)
      assert(result.projectile.source_side == "enemy")
      assert(result.projectile.source_component_id == emitter.id)
      -- Test-only replacement has deliberately removed the old projector from
      -- this isolated actor; it is not part of session ownership.
      assert(old_projector.id ~= emitter.id)
    end,
  },
}
