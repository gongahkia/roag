local BodyDamage = require("src.simulation.body_damage")
local Content = require("src.content.legacy")
local Inventory = require("src.inventory.inventory")
local PhysicalItem = require("src.inventory.physical_item")
local Session = require("src.simulation.session")

local CHARGE = "component.internal.legacy_volatile_charge"
local ABILITY = "ability.explosive.self_destruct"

local function new_run(seed)
  local session = Session.new({ seed = seed or 1201 })
  session:start_run(Content.classes[1], Content.boons[1])
  return session
end

local function salvage_charge(session, damage)
  local player = session.state.player
  local bomber = session:_make_enemy("bomber", { x = player.x, y = player.y + 1 })
  session.state.enemies[#session.state.enemies + 1] = bomber
  local index = #session.state.enemies
  local charge = bomber.body:get_component("internal_1")
  if damage then
    assert(session:damage_actor_body(bomber, { amount = damage, slot_id = "internal_1", cause = "test" }).applied)
  end
  session:_destroy_enemy(index)
  local corpse = session.state.corpses[#session.state.corpses]
  local result = session:salvage_corpse_component(corpse.id, "internal_1")
  assert(result.applied)
  return charge, corpse
end

local function enter_reconstruction(session)
  assert(session:_complete_stage() == "reconstruction")
  assert(session.state.phase == "reconstruction")
end

local function choose_next_route_floor(session, curse)
  local result = session:choose_curse(curse)
  if result.next == "route" then
    local choice = session:available_route_nodes()[1]
    assert(choice and session:select_route_node(choice.id).applied)
  end
  return result
end

local function snapshot(session)
  local values = {}
  for _, slot_id in ipairs(session.state.player.body.slot_order) do
    local component = session.state.player.body:get_component(slot_id)
    values[#values + 1] = slot_id .. ":" .. (component and (component.id .. ":" .. component.current_integrity) or "empty")
  end
  for _, entry in ipairs(session.state.inventory.entries) do
    values[#values + 1] = table.concat({
      entry.physical_id, entry.x, entry.y, tostring(entry.rotated), entry.item.object and entry.item.object.current_integrity or "item",
    }, ":")
  end
  return table.concat(values, "|")
end

return {
  {
    name = "reconstruction installs a compatible exact inventory component transactionally",
    run = function()
      local session = new_run(1202)
      local charge = salvage_charge(session)
      enter_reconstruction(session)
      local before = session.state.inventory:get(charge.id)
      local result = session:install_inventory_component(charge.id, "internal_2")
      assert(result.applied and result.component_id == charge.id)
      assert(not session.state.inventory:get(charge.id))
      assert(session.state.player.body:get_component("internal_2") == charge)
      assert(before.item.object == charge)
      assert(session:validate_physical_ownership())
    end,
  },
  {
    name = "reconstruction rejects field surgery and incompatible or occupied installs without ownership changes",
    run = function()
      local session = new_run(1203)
      local charge = salvage_charge(session)
      local combat_before = snapshot(session)
      local field = session:install_inventory_component(charge.id, "internal_2")
      assert(not field.applied and snapshot(session) == combat_before)

      enter_reconstruction(session)
      assert(session:uninstall_body_component("head").applied)
      local before = snapshot(session)
      local incompatible = session:install_inventory_component(charge.id, "head")
      assert(not incompatible.applied and incompatible.reason:find("incompatible", 1, true))
      assert(snapshot(session) == before)
      local occupied = session:install_inventory_component(charge.id, "internal_1")
      assert(not occupied.applied and occupied.reason:find("occupied", 1, true))
      assert(snapshot(session) == before)
      assert(session:validate_physical_ownership())
    end,
  },
  {
    name = "reconstruction uninstalls exact components and full inventory failure rolls back",
    run = function()
      local session = new_run(1204)
      enter_reconstruction(session)
      local optic = session.state.player.body:get_component("head")
      local success = session:uninstall_body_component("head")
      assert(success.applied and success.component_id == optic.id)
      assert(session.state.inventory:get(optic.id).item.object == optic)
      assert(not session.state.player.body:get_component("head"))

      local full = Inventory.new({ width = 1, height = 1 })
      local blocker = session.component_factory:create("component.internal.legacy_support")
      assert(full:auto_place(PhysicalItem.from_component(blocker, session.registry)))
      session.state.run.inventory, session.state.inventory = full, full
      local installed = session.state.player.body:get_component("internal_1")
      local before = snapshot(session)
      local failed = session:uninstall_body_component("internal_1")
      assert(not failed.applied)
      assert(session.state.player.body:get_component("internal_1") == installed)
      assert(snapshot(session) == before)
      assert(session:validate_physical_ownership())
    end,
  },
  {
    name = "physical ownership validation detects duplicate body and inventory component references",
    run = function()
      local session = new_run(1211)
      local body = session.state.player.body
      local support = body:get_component("internal_1")
      body:get_slot("internal_2").component = support
      local ok, err = pcall(function() session:validate_physical_ownership() end)
      assert(not ok and tostring(err):find("owned by both", 1, true))
      body:get_slot("internal_2").component = nil

      assert(session.state.inventory:auto_place(PhysicalItem.from_component(support, session.registry)))
      ok, err = pcall(function() session:validate_physical_ownership() end)
      assert(not ok and tostring(err):find("owned by both", 1, true))
      assert(session.state.inventory:remove(support.id))
      assert(session:validate_physical_ownership())
    end,
  },
  {
    name = "broken components remain installable but grant no body capability",
    run = function()
      local session = new_run(1205)
      local player = session.state.player
      local bomber = session:_make_enemy("bomber", { x = player.x, y = player.y + 1 })
      session.state.enemies[#session.state.enemies + 1] = bomber
      local charge = bomber.body:get_component("internal_1")
      assert(BodyDamage.apply(bomber.body, { amount = 3, slot_id = "internal_1" }).became_broken)
      session:_destroy_enemy(#session.state.enemies)
      assert(session:salvage_corpse_component(session.state.corpses[#session.state.corpses].id, "internal_1").applied)
      enter_reconstruction(session)
      assert(session:install_inventory_component(charge.id, "internal_2").applied)
      assert(charge.current_integrity == 0)
      assert(not session:actor_has_capability(session.state.player, ABILITY))
      assert(session:validate_physical_ownership())
    end,
  },
  {
    name = "run player body inventory and empty slots preserve exact state across a normal stage",
    run = function()
      local session = new_run(1206)
      local player, body = session.state.player, session.state.player.body
      player.health, player.ammo, player.bombs, player.flares = 3, 4, 2, 1
      local arm = session.component_factory:create("component.arm.legacy_manipulator")
      local inventory = session.state.inventory
      assert(inventory:place(PhysicalItem.from_component(arm, session.registry), 2, 2, true))
      local initial_data = session:run_data()
      assert(initial_data.body.slots[8].component == nil)
      enter_reconstruction(session)
      assert(session:complete_reconstruction().next == "curse")
      choose_next_route_floor(session, Content.curses[2]) -- Darkness changes floor vision, not carried resources.
      local entry = session.state.inventory:get(arm.id)
      assert(session.state.player == player and session.state.player.body == body)
      assert(session.state.player.body:get_component("internal_2") == nil)
      assert(entry and entry.x == 2 and entry.y == 2 and entry.rotated)
      assert(player.health == 3 and player.ammo == 4 and player.bombs == 2 and player.flares == 1)
      assert(session:validate_physical_ownership())
    end,
  },
  {
    name = "vertical slice preserves one bomber volatile charge from corpse through next floor",
    run = function()
      local session = new_run(1207)
      local charge, corpse = salvage_charge(session, 1)
      local id, integrity = charge.id, charge.current_integrity
      assert(corpse.body:get_component("internal_1") == nil)
      assert(session.state.inventory:get(id).item.object == charge)
      enter_reconstruction(session)
      assert(session:install_inventory_component(id, "internal_2").applied)
      assert(session.state.player.body:get_component("internal_2") == charge)
      assert(session:actor_has_capability(session.state.player, ABILITY))
      assert(session:complete_reconstruction().next == "curse")
      choose_next_route_floor(session, session.state.curse_options[1])
      assert(session.state.player.body:get_component("internal_2") == charge)
      assert(charge.id == id and charge.current_integrity == integrity)
      assert(session:actor_has_capability(session.state.player, ABILITY))
      assert(session:validate_physical_ownership())
    end,
  },
  {
    name = "reconstruction operations reproduce from the same seed and inputs",
    run = function()
      local first, second = new_run(1212), new_run(1212)
      local first_charge = salvage_charge(first, 1)
      local second_charge = salvage_charge(second, 1)
      enter_reconstruction(first)
      enter_reconstruction(second)
      assert(first:install_inventory_component(first_charge.id, "internal_2").applied)
      assert(second:install_inventory_component(second_charge.id, "internal_2").applied)
      assert(first:complete_reconstruction().next == second:complete_reconstruction().next)
      choose_next_route_floor(first, first.state.curse_options[1])
      choose_next_route_floor(second, second.state.curse_options[1])
      assert(first_charge.id == second_charge.id)
      assert(snapshot(first) == snapshot(second))
      assert(first:actor_has_capability(first.state.player, ABILITY) == second:actor_has_capability(second.state.player, ABILITY))
    end,
  },
  {
    name = "normal route reconstruction reaches a physical milestone boss before tier three",
    run = function()
      local session = new_run(1213)
      local body = session.state.player.body
      enter_reconstruction(session)
      assert(session:complete_reconstruction().next == "curse")
      choose_next_route_floor(session, session.state.curse_options[1])
      assert(session.state.stage == 2 and session.state.phase == "combat")
      enter_reconstruction(session)
      assert(session:complete_reconstruction().next == "boss")
      assert(session.state.phase == "boss" and session.state.player.body == body)
      assert(session.state.boss and session.state.boss.body and session.state.boss.boss_id)
      assert(session:validate_physical_ownership())
    end,
  },
  {
    name = "player and bomber invoke the shared self destruct implementation and broken charges reject activation",
    run = function()
      local enemy_session = new_run(1210)
      local enemy_player = enemy_session.state.player
      local bomber = enemy_session:_make_enemy("bomber", { x = enemy_player.x, y = enemy_player.y + 1 })
      enemy_session.state.enemies = { bomber }
      local bomber_charge = bomber.body:get_component("internal_1")
      local bomber_result = enemy_session:activate_actor_ability(bomber, ABILITY)
      assert(bomber_result.applied and bomber_result.implementation == "self_destruct")
      assert(bomber_result.component_id == bomber_charge.id)
      assert(enemy_session.state.corpses[1].body:get_component("internal_1") == bomber_charge)

      local session = new_run(1208)
      local charge = salvage_charge(session)
      enter_reconstruction(session)
      assert(session:install_inventory_component(charge.id, "internal_2").applied)
      assert(session:complete_reconstruction().next == "curse")
      choose_next_route_floor(session, session.state.curse_options[1])
      assert(session:actor_has_capability(session.state.player, ABILITY))
      local player_result = session:activate_actor_ability(session.state.player, ABILITY)
      assert(player_result.applied and player_result.implementation == "self_destruct")
      assert(player_result.component_id == charge.id and player_result.wear.applied)
      assert(session.state.ended == "gameover")

      local broken = new_run(1209)
      local broken_charge = salvage_charge(broken)
      enter_reconstruction(broken)
      assert(broken:install_inventory_component(broken_charge.id, "internal_2").applied)
      assert(BodyDamage.apply(broken.state.player.body, { amount = 3, slot_id = "internal_2" }).became_broken)
      assert(not broken:actor_has_capability(broken.state.player, ABILITY))
      assert(broken:complete_reconstruction().next == "curse")
      choose_next_route_floor(broken, broken.state.curse_options[1])
      local rejected = broken:activate_actor_ability(broken.state.player, ABILITY)
      assert(not rejected.applied and rejected.reason:find("functional provider", 1, true))
    end,
  },
}
