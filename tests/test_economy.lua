local Session = require("src.simulation.session")
local Economy = require("src.simulation.economy")
local PhysicalItem = require("src.inventory.physical_item")
local ActiveRun = require("src.persistence.active_run")
local SaveStore = require("src.persistence.save_store")
local App = require("src.app.app")

local function session(seed)
  local value = Session.new({ seed = seed or 98001 })
  value:start_run()
  return value
end

return {
  {
    name = "scrap is distinct from floor objective progress and first-generation income is bounded",
    run = function()
      local value = session(98001)
      local target = value.state.targets[1]
      local before_scrap, before_objective = value.state.scrap, value.state.player.objective_progress
      value:_destroy_target(1)
      assert(value.state.scrap == before_scrap + 1)
      assert(value.state.player.objective_progress == before_objective + 1)
      value:_refill_entities()
      local replenished = value.state.targets[#value.state.targets]
      assert(replenished.scrap_award == false)
      value:_destroy_target(#value.state.targets)
      assert(value.state.scrap == before_scrap + 1)
      assert(target ~= replenished)
    end,
  },
  {
    name = "supply repair and salvager transactions are atomic and preserve component identity",
    run = function()
      local value = session(98002)
      value.state.scrap = 30
      local supply = Economy.create_stock(value, "service.supply.legacy", value.rng:derive("supply"))
      local ammo = value.state.player.ammo
      assert(Economy.supply(value, supply, 1).applied)
      assert(value.state.player.ammo == ammo + 1 and supply.offers[1].remaining == 1)
      local component = value.state.player.body:list_components()[1]
      component.current_integrity = component.current_integrity - 1
      local repair = Economy.create_stock(value, "service.repair.legacy", value.rng:derive("repair"))
      assert(Economy.repair(value, repair, component.id).applied)
      assert(component.current_integrity == component.max_integrity)
      local salvager = Economy.create_stock(value, "service.salvager.legacy", value.rng:derive("salvager"))
      local saved_id = salvager.offers[1].component.id
      assert(Economy.buy_component(value, salvager, 1).applied)
      assert(value.state.inventory:get(saved_id) and salvager.offers[1].sold)
      local currency = value.state.scrap
      assert(Economy.sell_component(value, saved_id).applied)
      assert(not value.state.inventory:get(saved_id) and value.state.scrap > currency)
      assert(value:validate_physical_ownership())
    end,
  },
  {
    name = "failed component purchase leaves scrap and stock unchanged",
    run = function()
      local value = session(98003)
      value.state.scrap = 99
      while true do
        local component = value.component_factory:create("component.internal.legacy_support")
        if not value.state.inventory:auto_place(PhysicalItem.from_component(component, value.registry)) then break end
      end
      local stock = Economy.create_stock(value, "service.salvager.legacy", value.rng:derive("full"))
      local scrap = value.state.scrap
      local result = Economy.buy_component(value, stock, 1)
      assert(not result.applied and result.code == "inventory_full")
      assert(value.state.scrap == scrap and not stock.offers[1].sold)
    end,
  },
  {
    name = "charms derive modifiers compose with curses and survive save resume",
    run = function()
      local value = session(98004)
      value.state.scrap = 99
      local stock = Economy.create_stock(value, "service.charm_vendor.legacy", value.rng:derive("charms"))
      local windwalker
      for index, offer in ipairs(stock.offers) do if offer.charm_id == "charm.legacy.windwalker" then windwalker = index end end
      if not windwalker then stock.offers[1] = { charm_id = "charm.legacy.windwalker", sold = false }; windwalker = 1 end
      assert(Economy.buy_charm(value, stock, windwalker).applied)
      assert(value.state.player.dash_base == 2)
      value.state.curse = value.registry:get_curse("curse.legacy.slow_dash")
      value.state.curse_id = value.state.curse.id
      value:refresh_derived_player_stats()
      assert(value.state.player.dash_base == 3)
      local store = SaveStore.memory()
      assert(ActiveRun.save(value, store))
      local restored = assert(ActiveRun.load(store))
      assert(restored.state.charms.slots[1] == "charm.legacy.windwalker")
      assert(restored.state.player.dash_base == 3)
      assert(Economy.remove_charm(restored, 1).applied)
      assert(restored.state.player.dash_base == 4)
    end,
  },
  {
    name = "charms use three run slots and new runs bypass class and free-boon screens",
    run = function()
      local value = session(980045)
      value.state.scrap = 99
      local stock = { offers = {
        { charm_id = "charm.legacy.iron_heart", sold = false },
        { charm_id = "charm.legacy.windwalker", sold = false },
        { charm_id = "charm.legacy.demolition", sold = false },
        { charm_id = "charm.legacy.flare_lens", sold = false },
      } }
      assert(Economy.buy_charm(value, stock, 1).applied)
      assert(Economy.buy_charm(value, stock, 2).applied)
      assert(Economy.buy_charm(value, stock, 3).applied)
      local fourth = Economy.buy_charm(value, stock, 4)
      assert(not fourth.applied and fourth.code == "charm_slots_full")
      local app = App.new({ seed = 980046, save_store = SaveStore.memory() })
      assert(app:request_new_run())
      assert(app.screen == "game" and app.session.state.legacy_class == nil and app.session.state.legacy_boon == nil)
    end,
  },
  {
    name = "route services are stable and branch choices advertise different utility",
    run = function()
      local first, second = session(98005), session(98005)
      local one, two = first.state.route:to_data(), second.state.route:to_data()
      assert(require("src.persistence.json").encode(one) == require("src.persistence.json").encode(two))
      assert(first.state.route:node(first.state.route.start_node_id).service_id == "service.supply.legacy")
      assert(first.state.route:complete_current().applied)
      local choices = first.state.route:available()
      assert(#choices == 2 and choices[1].service_id ~= choices[2].service_id)
    end,
  },
  {
    name = "world kiosk stock persists exactly through active-run save resume",
    run = function()
      local value = session(98006)
      local kiosk
      for _, object in ipairs(value.state.world:list_objects()) do if object.interaction_role == "service" then kiosk = object break end end
      assert(kiosk and kiosk.service_id == "service.supply.legacy")
      value.state.scrap = 10
      assert(value:open_service(kiosk.id).applied)
      assert(value:service_execute(value:service_options(kiosk.id)[1], kiosk.id).applied)
      local remaining = kiosk.service_stock.offers[1].remaining
      local store = SaveStore.memory()
      assert(ActiveRun.save(value, store))
      local restored = assert(ActiveRun.load(store))
      local restored_kiosk = assert(restored.state.world:get_object(kiosk.id))
      assert(restored.state.phase == "service")
      assert(restored_kiosk.service_stock.offers[1].remaining == remaining)
      assert(restored.state.scrap == value.state.scrap)
    end,
  },
  {
    name = "final route shop reuses persistent service stock and scrap",
    run = function()
      local value = session(98007)
      local function advance(choice)
        assert(value:_complete_stage() == "reconstruction")
        local next_step = assert(value:complete_reconstruction()).next
        if next_step == "curse" then
          assert(value:choose_curse(value.state.curse_options[1]).next == "route")
          assert(value:select_route_node(value:available_route_nodes()[choice or 1].id).applied)
        end
      end
      advance(1)
      assert(value:_complete_stage() == "reconstruction")
      assert(value:complete_reconstruction().next == "boss")
      value.state.boss.health = 1
      assert(value:_damage_boss(1).dead and value.state.phase == "boss_exit")
      value.state.player.x, value.state.player.y = value.state.exit.x, value.state.exit.y
      assert(value:turn("") == "reconstruction")
      assert(value:complete_reconstruction().next == "curse")
      assert(value:choose_curse(value.state.curse_options[1]).next == "route")
      assert(value:select_route_node(value:available_route_nodes()[1].id).applied)
      assert(value:_complete_stage() == "reconstruction")
      assert(value:complete_reconstruction().next == "boss")
      -- Every new production route now resolves its deterministic second
      -- milestone before opening the final service hub.
      value.state.boss.health = 1
      assert(value:_damage_boss(1).dead and value.state.phase == "boss_exit")
      value.state.player.x, value.state.player.y = value.state.exit.x, value.state.exit.y
      assert(value:turn("") == "reconstruction")
      assert(value:complete_reconstruction().next == "shop")
      assert(value.state.final_service_hub and value.state.final_service_hub.stocks["service.salvager.legacy"])
      value.state.scrap = 30
      assert(value:open_service("hub:service.salvager.legacy").applied)
      assert(value:service_execute(value:service_options("hub:service.salvager.legacy")[1], "hub:service.salvager.legacy").applied)
      assert(value:close_service().return_to_hub)
      local store = SaveStore.memory(); assert(ActiveRun.save(value, store))
      local restored = assert(ActiveRun.load(store))
      assert(restored.state.final_service_hub.stocks["service.salvager.legacy"].offers[1].sold)
    end,
  },
}
