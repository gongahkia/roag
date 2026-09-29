local Body = require("src.body.body")
local Component = require("src.body.component")
local ComponentFactory = require("src.body.component_factory")
local Content = require("src.content.legacy")
local Registry = require("src.content.registry")
local Session = require("src.simulation.session")

local VOLATILE_ABILITY = "ability.explosive.self_destruct"
local VOLATILE_COMPONENT = "component.internal.legacy_volatile_charge"

local function new_body()
  local registry = Registry.load()
  local owner = { next_component_sequence = 1 }
  return registry, ComponentFactory.new(registry, owner), Body.new(registry, "body.topology.normal")
end

return {
  {
    name = "normal body exposes the baseline topology",
    run = function()
      local _, _, body = new_body()
      for _, slot_id in ipairs({
        "head", "torso_core", "left_arm", "right_arm",
        "left_leg", "right_leg", "internal_1", "internal_2",
      }) do
        assert(body:get_slot(slot_id), "Missing slot " .. slot_id)
      end
      assert(#body:list_components() == 0)
    end,
  },
  {
    name = "body installation enforces compatibility and occupancy",
    run = function()
      local _, factory, body = new_body()
      local head = factory:create("component.head.legacy_optic")
      assert(body:install("head", head) == head)
      assert(body:get_component("head") == head)
      local _, occupied = body:install("head", factory:create("component.head.legacy_optic"))
      assert(occupied == "Body slot 'head' is already occupied")
      local _, incompatible = body:install("right_arm", factory:create("component.leg.legacy_locomotor"))
      assert(incompatible:find("incompatible", 1, true))
      assert(body:uninstall("head") == head)
      assert(body:get_component("head") == nil)
    end,
  },
  {
    name = "component instances have independent integrity and preserve definitions",
    run = function()
      local registry, factory = new_body()
      local first = factory:create("component.arm.legacy_manipulator")
      local second = factory:create("component.arm.legacy_manipulator")
      first.current_integrity = 1
      assert(second.current_integrity == 3)
      assert(registry:get_component(first.definition_id).max_integrity == 3)
      assert(Component.condition(first) == "critical")
      assert(Component.condition(second) == "healthy")
    end,
  },
  {
    name = "component instance IDs are deterministic and unique per sequence",
    run = function()
      local registry = Registry.load()
      local first_owner, second_owner = { next_component_sequence = 1 }, { next_component_sequence = 1 }
      local first = ComponentFactory.new(registry, first_owner)
      local second = ComponentFactory.new(registry, second_owner)
      assert(first:create("component.head.legacy_optic").id == "component:000001")
      assert(first:create("component.head.legacy_optic").id == "component:000002")
      assert(second:create("component.head.legacy_optic").id == "component:000001")
    end,
  },
  {
    name = "equivalent sessions allocate the same body instance identities",
    run = function()
      local first, second = Session.new({ seed = 103 }), Session.new({ seed = 103 })
      first:start_run(Content.classes[1], Content.boons[1])
      second:start_run(Content.classes[1], Content.boons[1])
      local first_components = first.state.player.body:list_components()
      local second_components = second.state.player.body:list_components()
      assert(#first_components == #second_components)
      for index, component in ipairs(first_components) do
        assert(component.id == second_components[index].id)
      end
      local first_bomber = first:_make_enemy("bomber", { x = 40, y = 26 })
      local second_bomber = second:_make_enemy("bomber", { x = 40, y = 26 })
      assert(first_bomber.body:get_component("internal_1").id == second_bomber.body:get_component("internal_1").id)
    end,
  },
  {
    name = "capabilities appear and disappear with component installation",
    run = function()
      local _, factory, body = new_body()
      local charge = factory:create(VOLATILE_COMPONENT)
      assert(not body:has_capability(VOLATILE_ABILITY))
      assert(body:install("internal_2", charge))
      assert(body:has_capability(VOLATILE_ABILITY))
      assert(body:capability_providers(VOLATILE_ABILITY)[1] == charge)
      assert(body:list_capabilities()[1] == VOLATILE_ABILITY)
      assert(body:installed_mass() == 1)
      assert(body:uninstall("internal_2") == charge)
      assert(not body:has_capability(VOLATILE_ABILITY))
    end,
  },
  {
    name = "the current player body can receive the bomber component",
    run = function()
      local session = Session.new({ seed = 101 })
      session:start_run(Content.classes[1], Content.boons[1])
      local player = session.state.player
      assert(player.body and not session:actor_has_capability(player, VOLATILE_ABILITY))
      local charge = session.component_factory:create(VOLATILE_COMPONENT)
      assert(player.body:install("internal_2", charge))
      assert(session:actor_has_capability(player, VOLATILE_ABILITY))
      player.body:uninstall("internal_2")
      assert(not session:actor_has_capability(player, VOLATILE_ABILITY))
    end,
  },
  {
    name = "a normal run gives the player and spawned bombers real bodies",
    run = function()
      local session = Session.new({ seed = 104 })
      session:start_run(Content.classes[1], Content.boons[1])
      assert(session.state.player.content_id == "actor.player.legacy")
      assert(session.state.player.body:get_component("torso_core"))
      local bomber_count = 0
      for _, enemy in ipairs(session.state.enemies) do
        if enemy.kind == "bomber" then
          bomber_count = bomber_count + 1
          assert(enemy.content_id == "enemy.legacy.bomber")
          assert(session:actor_has_capability(enemy, VOLATILE_ABILITY))
        end
      end
      assert(bomber_count > 0)
    end,
  },
  {
    name = "bomber detonation is granted by its installed component",
    run = function()
      local session = Session.new({ seed = 102 })
      session:start_run(Content.classes[1], Content.boons[1])
      local player = session.state.player
      local bomber = session:_make_enemy("bomber", { x = player.x, y = player.y + 1 })
      session.state.enemies = { bomber }
      assert(bomber.body and session:actor_has_capability(bomber, VOLATILE_ABILITY))
      session:_enemy_turn()
      assert(bomber.attack_kind == "detonate")

      assert(bomber.body:uninstall("internal_1"))
      bomber.attack, bomber.attack_kind, bomber.attack_windup = 0, nil, 0
      session:_enemy_turn()
      assert(bomber.attack_kind == nil)
    end,
  },
}
