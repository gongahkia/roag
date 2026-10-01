-- Atomic, headless transactions for the one run-wide SCRAP economy.
local Component = require("src.body.component")
local PhysicalItem = require("src.inventory.physical_item")
local RunModifiers = require("src.simulation.run_modifiers")

local Economy = {}

local function fail(code, reason)
  return { applied = false, code = code, reason = reason }
end

local function spend(session, cost)
  if session.state.scrap < cost then return nil, fail("insufficient_currency", "Insufficient SCRAP") end
  session.state.scrap = session.state.scrap - cost
  return true
end

local function component_value(session, component)
  local definition = session.registry:get_component(component.definition_id)
  local base = definition.scrap_value or definition.max_integrity
  local condition = component.max_integrity > 0 and component.current_integrity / component.max_integrity or 0
  return math.max(1, math.floor(base * (0.25 + 0.75 * condition)))
end

function Economy.create_stock(session, service_id, rng, final_hub)
  local service = session.registry:get_service(service_id)
  if service.role == "supply" then
    return { kind = "supply", offers = {
      { key = "ammo", label = "AMMO", price = 2, remaining = final_hub and 6 or 3 },
      { key = "bombs", label = "BOMB", price = 4, remaining = final_hub and 3 or 1 },
      { key = "flares", label = "FLARE", price = 3, remaining = final_hub and 3 or 1 },
    } }
  elseif service.role == "repair" then
    return { kind = "repair", price = 2, remaining = final_hub and 6 or 3 }
  elseif service.role == "salvager" then
    local pool = {
      "component.arm.legacy_projectile_emitter", "component.arm.legacy_arcane_projector",
      "component.leg.legacy_locomotor", "component.internal.legacy_support",
      "component.internal.legacy_shock_coil",
    }
    local offers = {}
    local shuffled = rng:shuffle(pool)
    for index = 1, math.min(final_hub and 4 or 2, #shuffled) do
      local component = session.component_factory:create(shuffled[index])
      local definition = session.registry:get_component(component.definition_id)
      offers[#offers + 1] = { component = Component.to_data(component), price = definition.scrap_value or definition.max_integrity, sold = false }
    end
    return { kind = "salvager", offers = offers }
  elseif service.role == "charm_vendor" then
    local ids = {}
    for id in pairs(session.registry.charms) do ids[#ids + 1] = id end
    table.sort(ids)
    ids = rng:shuffle(ids)
    local offers = {}
    for index = 1, math.min(final_hub and 5 or 3, #ids) do offers[#offers + 1] = { charm_id = ids[index], sold = false } end
    return { kind = "charm_vendor", offers = offers }
  end
  error("Unsupported service role " .. service.role)
end

function Economy.supply(session, stock, offer_index)
  local offer = stock and stock.offers and stock.offers[offer_index]
  if not offer or offer.remaining <= 0 then return fail("out_of_stock", "That supply is out of stock") end
  local player = session.state.player
  local ok, failure = spend(session, offer.price)
  if not ok then return failure end
  player[offer.key] = (player[offer.key] or 0) + 1
  offer.remaining = offer.remaining - 1
  return { applied = true, code = "purchased", key = offer.key, price = offer.price }
end

function Economy.repair(session, stock, component_id)
  if not stock or stock.remaining <= 0 then return fail("out_of_stock", "No repair operations remain") end
  local component
  for _, installed in ipairs(session.state.player.body:list_components()) do if installed.id == component_id then component = installed break end end
  if not component then
    local entry = session.state.run.inventory:get(component_id)
    component = entry and entry.item.object or nil
  end
  if not component then return fail("invalid_component", "Component is not owned by the player") end
  if component.current_integrity >= component.max_integrity then return fail("already_full_integrity", "Component is already at full integrity") end
  local price = stock.price or 2
  local ok, failure = spend(session, price)
  if not ok then return failure end
  component.current_integrity = math.min(component.max_integrity, component.current_integrity + 1)
  stock.remaining = stock.remaining - 1
  return { applied = true, code = "repaired", component_id = component.id, price = price, integrity = component.current_integrity }
end

function Economy.buy_component(session, stock, offer_index)
  local offer = stock and stock.offers and stock.offers[offer_index]
  if not offer or offer.sold then return fail("out_of_stock", "That component is no longer available") end
  local component = Component.from_data(session.registry:get_component(offer.component.definition_id), offer.component)
  local item = PhysicalItem.from_component(component, session.registry)
  local placement = session.state.run.inventory:find_first_fit(item)
  if not placement then return fail("inventory_full", "Inventory has no room for that component") end
  local ok, failure = spend(session, offer.price)
  if not ok then return failure end
  local entry, reason = session.state.run.inventory:place(item, placement.x, placement.y, placement.rotated)
  if not entry then
    session.state.scrap = session.state.scrap + offer.price
    return fail("inventory_full", reason)
  end
  offer.sold = true
  return { applied = true, code = "purchased", component_id = component.id, price = offer.price }
end

function Economy.sell_component(session, component_id)
  local inventory = session.state.run.inventory
  local entry = inventory:get(component_id)
  if not entry or entry.item.item_type ~= "component" then return fail("invalid_item", "Only carried components can be sold") end
  local value = component_value(session, entry.item.object)
  local item = assert(inventory:remove(component_id))
  assert(item.object.id == component_id, "Sold a different component")
  session.state.scrap = session.state.scrap + value
  return { applied = true, code = "sold", component_id = component_id, value = value }
end

function Economy.buy_charm(session, stock, offer_index)
  local offer = stock and stock.offers and stock.offers[offer_index]
  if not offer or offer.sold then return fail("out_of_stock", "That charm is no longer available") end
  local slots = session.state.charms.slots
  local slot
  for index = 1, RunModifiers.charm_slots(session.state) do if not slots[index] then slot = index break end end
  if not slot then return fail("charm_slots_full", "All charm slots are occupied") end
  local charm = session.registry:get_charm(offer.charm_id)
  local ok, failure = spend(session, charm.price)
  if not ok then return failure end
  slots[slot], offer.sold = charm.id, true
  session:refresh_derived_player_stats()
  return { applied = true, code = "equipped", charm_id = charm.id, slot = slot, price = charm.price }
end

function Economy.remove_charm(session, slot)
  local slots = session.state.charms.slots
  if type(slot) ~= "number" or not slots[slot] then return fail("invalid_item", "Charm slot is empty") end
  local charm_id = slots[slot]
  slots[slot] = nil
  session:refresh_derived_player_stats()
  return { applied = true, code = "removed", charm_id = charm_id, slot = slot }
end

return Economy
