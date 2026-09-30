local BodyDamage = require("src.simulation.body_damage")
local Component = require("src.body.component")
local Content = require("src.content.legacy")
local Corpse = require("src.world.corpse")
local Inventory = require("src.inventory.inventory")
local PhysicalItem = require("src.inventory.physical_item")
local Session = require("src.simulation.session")

local CHARGE = "component.internal.legacy_volatile_charge"

local function new_session(seed)
  local session = Session.new({ seed = seed or 801 })
  session:start_run(Content.classes[1], Content.boons[1])
  return session
end

local function bomber_at_player(session)
  local player = session.state.player
  local bomber = session:_make_enemy("bomber", { x = player.x, y = player.y + 1 })
  session.state.enemies[#session.state.enemies + 1] = bomber
  return bomber, #session.state.enemies
end

return {
  {
    name = "killing a body-bearing enemy transfers its actual damaged body to a persistent corpse",
    run = function()
      local session = new_session()
      local bomber, index = bomber_at_player(session)
      local body, charge = bomber.body, bomber.body:get_component("internal_1")
      assert(BodyDamage.apply(body, { amount = 2, slot_id = "internal_1" }).new_integrity == 1)
      session:_destroy_enemy(index)

      assert(#session.state.enemies == index - 1 and #session.state.corpses == 1)
      local corpse = session.state.corpses[1]
      assert(corpse.id == "corpse:000001")
      assert(corpse.body == body and bomber.body == nil)
      assert(corpse.body:get_component("internal_1") == charge)
      assert(charge.id:match("^component:%d%d%d%d%d%d$"))
      assert(charge.current_integrity == 1 and Component.condition(charge) == "critical")
      assert(session:validate_physical_ownership())
    end,
  },
  {
    name = "a player projectile death path creates a corpse with the damaged bomber component",
    run = function()
      local session = new_session(806)
      local player = session.state.player
      for _, enemy in ipairs(session.state.enemies) do
        enemy.x, enemy.y = 0, 0
      end
      session.state.targets = {}
      local bomber = session:_make_enemy("bomber", { x = player.x, y = player.y + 1 })
      session.state.enemies[#session.state.enemies + 1] = bomber
      local charge = bomber.body:get_component("internal_1")
      session.state.bullets = {
        { kind = "bullet", x = player.x, y = player.y, direction = "w", active = true, travel = 1, light = 2 },
      }
      session:_update_bullets()
      assert(#session.state.corpses == 1)
      assert(session.state.corpses[1].body:get_component("internal_1") == charge)
      assert(charge.current_integrity == 2)
    end,
  },
  {
    name = "successful salvage moves the same physical component from corpse to inventory",
    run = function()
      local session = new_session(802)
      local bomber, index = bomber_at_player(session)
      local charge = bomber.body:get_component("internal_1")
      local id = charge.id
      session:_destroy_enemy(index)
      local corpse = session.state.corpses[1]

      local result = session:salvage_corpse_component(corpse.id, "internal_1")
      assert(result.applied and result.component_id == id)
      assert(corpse.body:get_component("internal_1") == nil)
      -- Bomber legs remain physical salvage after its volatile charge moves.
      assert(#corpse:list_components() == 2)
      local entry = session.state.inventory:get(id)
      assert(entry and entry.item.object == charge and entry.item.physical_id == id)
      assert(entry.item.object.current_integrity == charge.current_integrity)
      assert(session.state.inventory:total_mass() == 1)
      assert(session:validate_physical_ownership())
      assert(not session:salvage_corpse_component(corpse.id, "internal_1").applied)
      assert(#session.state.inventory.entries == 1)
    end,
  },
  {
    name = "failed inventory placement leaves the actual component on the corpse",
    run = function()
      local session = new_session(803)
      session.state.inventory = Inventory.new({ width = 1, height = 1 })
      local blocker = session.component_factory:create("component.internal.legacy_support")
      assert(session.state.inventory:auto_place(PhysicalItem.from_component(blocker, session.registry)))
      local bomber, index = bomber_at_player(session)
      local charge = bomber.body:get_component("internal_1")
      session:_destroy_enemy(index)
      local corpse = session.state.corpses[1]

      local result = session:salvage_corpse_component(corpse.id, "internal_1")
      assert(not result.applied)
      assert(corpse.body:get_component("internal_1") == charge)
      assert(not session.state.inventory:get(charge.id))
      assert(session:validate_physical_ownership())
    end,
  },
  {
    name = "broken components remain salvageable and corpse data preserves physical state",
    run = function()
      local session = new_session(804)
      local bomber, index = bomber_at_player(session)
      local charge = bomber.body:get_component("internal_1")
      assert(BodyDamage.apply(bomber.body, { amount = 3, slot_id = "internal_1" }).became_broken)
      session:_destroy_enemy(index)
      local corpse = session.state.corpses[1]
      local restored = Corpse.from_data(session.registry, corpse:to_data())
      local restored_charge = restored.body:get_component("internal_1")
      assert(restored.id == corpse.id)
      assert(restored_charge.id == charge.id and restored_charge.current_integrity == 0)
      assert(Component.condition(restored_charge) == "broken")

      local result = session:salvage_corpse_component(corpse.id, "internal_1")
      assert(result.applied)
      assert(session.state.inventory:get(charge.id).item.object.current_integrity == 0)
    end,
  },
  {
    name = "corpse IDs and salvage packing reproduce in equivalent sessions",
    run = function()
      local first, second = new_session(805), new_session(805)
      local first_bomber, first_index = bomber_at_player(first)
      local second_bomber, second_index = bomber_at_player(second)
      first:_destroy_enemy(first_index)
      second:_destroy_enemy(second_index)
      local first_corpse, second_corpse = first.state.corpses[1], second.state.corpses[1]
      assert(first_corpse.id == second_corpse.id)
      local first_result = first:salvage_corpse_component(first_corpse.id, "internal_1")
      local second_result = second:salvage_corpse_component(second_corpse.id, "internal_1")
      assert(first_result.applied and second_result.applied)
      local first_entry = first.state.inventory:get(first_result.component_id)
      local second_entry = second.state.inventory:get(second_result.component_id)
      assert(first_entry.x == second_entry.x and first_entry.y == second_entry.y and first_entry.rotated == second_entry.rotated)
      assert(first_bomber.body == nil and second_bomber.body == nil)
    end,
  },
}
