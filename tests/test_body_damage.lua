local Body = require("src.body.body")
local BodyDamage = require("src.simulation.body_damage")
local ComponentFactory = require("src.body.component_factory")
local Content = require("src.content.legacy")
local Registry = require("src.content.registry")
local Rng = require("src.rng")
local Session = require("src.simulation.session")

local VOLATILE_ABILITY = "ability.explosive.self_destruct"
local VOLATILE_COMPONENT = "component.internal.legacy_volatile_charge"

local function new_body()
  local registry = Registry.load()
  local owner = { next_component_sequence = 1 }
  return registry, ComponentFactory.new(registry, owner), Body.new(registry, "body.topology.normal")
end

local function new_session(seed)
  local session = Session.new({ seed = seed })
  session:start_run(Content.classes[1], Content.boons[1])
  return session
end

return {
  {
    name = "localized damage clamps integrity and reports structured transitions",
    run = function()
      local _, factory, body = new_body()
      local arm = factory:create("component.arm.legacy_manipulator")
      assert(body:install("left_arm", arm))

      local damaged = BodyDamage.apply(body, {
        amount = 1,
        slot_id = "left_arm",
        cause = "kinetic",
        source = "test",
      })
      assert(damaged.applied)
      assert(damaged.component_id == arm.id)
      assert(damaged.definition_id == arm.definition_id)
      assert(damaged.previous_integrity == 3 and damaged.new_integrity == 2)
      assert(damaged.previous_condition == "healthy" and damaged.new_condition == "damaged")

      local broken = BodyDamage.apply(body, { amount = 99, slot_id = "left_arm", cause = "explosive" })
      assert(broken.new_integrity == 0)
      assert(broken.new_condition == "broken" and broken.became_broken)

      local restored = BodyDamage.restore(body, { amount = 99, slot_id = "left_arm", source = "test" })
      assert(restored.new_integrity == arm.max_integrity)
      assert(restored.previous_condition == "broken" and restored.new_condition == "healthy")
      assert(restored.became_functional)
    end,
  },
  {
    name = "integrity condition thresholds are derived and ordered",
    run = function()
      local _, factory, body = new_body()
      assert(body:install("left_arm", factory:create("component.arm.legacy_manipulator")))
      assert(BodyDamage.apply(body, { amount = 1, slot_id = "left_arm" }).new_condition == "damaged")
      assert(BodyDamage.apply(body, { amount = 1, slot_id = "left_arm" }).new_condition == "critical")
      assert(BodyDamage.apply(body, { amount = 1, slot_id = "left_arm" }).new_condition == "broken")
    end,
  },
  {
    name = "broken components stop granting capabilities and restoration restores them",
    run = function()
      local _, factory, body = new_body()
      assert(body:install("internal_1", factory:create(VOLATILE_COMPONENT)))
      assert(body:has_capability(VOLATILE_ABILITY))
      assert(BodyDamage.apply(body, { amount = 3, slot_id = "internal_1" }).became_broken)
      assert(not body:has_capability(VOLATILE_ABILITY))
      assert(BodyDamage.restore(body, { amount = 1, slot_id = "internal_1" }).became_functional)
      assert(body:has_capability(VOLATILE_ABILITY))
    end,
  },
  {
    name = "redundant functional providers preserve a capability",
    run = function()
      local _, factory, body = new_body()
      assert(body:install("internal_1", factory:create(VOLATILE_COMPONENT)))
      assert(body:install("internal_2", factory:create(VOLATILE_COMPONENT)))
      assert(BodyDamage.apply(body, { amount = 3, slot_id = "internal_1" }).became_broken)
      assert(body:has_capability(VOLATILE_ABILITY))
      assert(#body:capability_providers(VOLATILE_ABILITY) == 1)
      assert(BodyDamage.apply(body, { amount = 3, slot_id = "internal_2" }).became_broken)
      assert(not body:has_capability(VOLATILE_ABILITY))
    end,
  },
  {
    name = "localized targeting handles valid, empty, and invalid slots",
    run = function()
      local _, factory, body = new_body()
      assert(body:install("left_arm", factory:create("component.arm.legacy_manipulator")))
      assert(BodyDamage.apply(body, { amount = 1, slot_id = "left_arm" }).slot_id == "left_arm")
      local empty = BodyDamage.apply(body, { amount = 1, slot_id = "right_arm" })
      assert(not empty.applied and empty.reason == "Body slot 'right_arm' is empty")
      local invalid = BodyDamage.apply(body, { amount = 1, slot_id = "missing_slot" })
      assert(not invalid.applied and invalid.reason == "Unknown body slot 'missing_slot'")
    end,
  },
  {
    name = "automatic targeting is deterministic and selects installed components only",
    run = function()
      local function target_with(seed)
        local _, factory, body = new_body()
        assert(body:install("head", factory:create("component.head.legacy_optic")))
        assert(body:install("left_arm", factory:create("component.arm.legacy_manipulator")))
        return BodyDamage.apply(body, { amount = 1, rng = Rng.new(seed), cause = "kinetic" })
      end
      local first, second = target_with(501), target_with(501)
      assert(first.applied and second.applied)
      assert(first.slot_id == second.slot_id)
      assert(first.slot_id == "head" or first.slot_id == "left_arm")
      local _, missing_rng_factory, missing_rng_body = new_body()
      assert(missing_rng_body:install("head", missing_rng_factory:create("component.head.legacy_optic")))
      local missing_rng = BodyDamage.apply(missing_rng_body, { amount = 1 })
      assert(not missing_rng.applied and missing_rng.reason:find("requires a deterministic RNG", 1, true))
    end,
  },
  {
    name = "player and enemy bodies use the same localized damage service",
    run = function()
      local session = new_session(601)
      local player_damage = session:damage_actor_body(session.state.player, {
        amount = 1,
        slot_id = "left_arm",
        cause = "kinetic",
      })
      assert(player_damage.applied and player_damage.new_condition == "damaged")
      assert(session.state.log[1] == "LEFT ARM — LEGACY MANIPULATOR DAMAGED")

      local player = session.state.player
      local bomber = session:_make_enemy("bomber", { x = player.x, y = player.y + 1 })
      session.state.enemies = { bomber }
      assert(session:actor_has_capability(bomber, VOLATILE_ABILITY))
      local charge_damage = session:damage_actor_body(bomber, {
        amount = 3,
        slot_id = "internal_1",
        cause = "kinetic",
      })
      assert(charge_damage.became_broken)
      assert(not session:actor_has_capability(bomber, VOLATILE_ABILITY))
      session:_enemy_turn()
      assert(bomber.attack_kind == nil)
    end,
  },
  {
    name = "usage wear shares integrity transitions and can fail a capability",
    run = function()
      local session = new_session(701)
      local player = session.state.player
      assert(player.body:install("internal_2", session.component_factory:create(VOLATILE_COMPONENT)))
      assert(session:actor_has_capability(player, VOLATILE_ABILITY))
      assert(session:wear_actor_component(player, "internal_2", "test_use").new_condition == "damaged")
      assert(session:wear_actor_component(player, "internal_2", "test_use").new_condition == "critical")
      local broken = session:wear_actor_component(player, "internal_2", "test_use")
      assert(broken.became_broken and broken.cause == "wear")
      assert(not session:actor_has_capability(player, VOLATILE_ABILITY))
    end,
  },
  {
    name = "detachment preserves the physical instance and updates body mass",
    run = function()
      local _, factory, body = new_body()
      local charge = factory:create(VOLATILE_COMPONENT)
      assert(body:install("internal_1", charge))
      assert(body:installed_mass() == 1)
      assert(body:has_capability(VOLATILE_ABILITY))
      assert(body:detach("internal_1") == charge)
      assert(body:get_component("internal_1") == nil)
      assert(body:installed_mass() == 0)
      assert(not body:has_capability(VOLATILE_ABILITY))
    end,
  },
}
