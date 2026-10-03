-- Transactional transfer from corpse body to shared inventory. The source is
-- detached only after a deterministic placement has been found.
local PhysicalItem = require("src.inventory.physical_item")

local Salvage = {}

function Salvage.component(corpse, slot_id, inventory, registry)
  if not corpse or not corpse.body then
    return { applied = false, reason = "Corpse has no salvageable body" }
  end
  local component = corpse.body:get_component(slot_id)
  if not component then
    return { applied = false, corpse_id = corpse.id, slot_id = slot_id, reason = "Corpse slot is empty" }
  end
  local item = PhysicalItem.from_component(component, registry)
  local placement, reason = inventory:find_first_fit(item)
  if not placement then
    return {
      applied = false,
      corpse_id = corpse.id,
      slot_id = slot_id,
      component_id = component.id,
      reason = reason,
    }
  end

  local detached, detach_reason = corpse.body:detach(slot_id)
  if not detached then
    return { applied = false, corpse_id = corpse.id, slot_id = slot_id, reason = detach_reason }
  end
  assert(detached == component, "Corpse detached a different physical component")
  local entry, placement_reason = inventory:place(item, placement.x, placement.y, placement.rotated)
  if not entry then
    local restored, restore_reason = corpse.body:install(slot_id, detached)
    assert(restored, restore_reason)
    return { applied = false, corpse_id = corpse.id, slot_id = slot_id, component_id = component.id, reason = placement_reason }
  end
  return {
    applied = true,
    corpse_id = corpse.id,
    slot_id = slot_id,
    component_id = component.id,
    definition_id = component.definition_id,
    entry = entry,
  }
end

-- Campaign fallen bodies may additionally contain the physical cargo that
-- was being carried at death.  This follows the same remove-only-after-fit
-- transaction as installed-component salvage, retaining the item's original
-- physical ID and requiring ordinary inventory capacity.
function Salvage.carried_item(corpse, physical_id, inventory)
  if not corpse or not corpse.carried_inventory then
    return { applied = false, reason = "Corpse has no carried cargo" }
  end
  local entry = corpse.carried_inventory:get(physical_id)
  if not entry then
    return { applied = false, corpse_id = corpse.id, component_id = physical_id, reason = "Corpse cargo item is missing" }
  end
  local placement, reason = inventory:find_first_fit(entry.item)
  if not placement then
    return { applied = false, corpse_id = corpse.id, component_id = physical_id, reason = reason }
  end
  local item, removed = corpse.carried_inventory:remove(physical_id)
  assert(item == entry.item and removed, "Corpse cargo removal lost its physical item")
  local placed, placement_reason = inventory:place(item, placement.x, placement.y, placement.rotated)
  if not placed then
    local restored, restore_reason = corpse.carried_inventory:place(item, entry.x, entry.y, entry.rotated)
    assert(restored, restore_reason)
    return { applied = false, corpse_id = corpse.id, component_id = physical_id, reason = placement_reason }
  end
  return {
    applied = true, corpse_id = corpse.id, component_id = physical_id,
    definition_id = item.object and item.object.definition_id or nil, entry = placed,
  }
end

function Salvage.carried_component(corpse, physical_id, inventory)
  local entry = corpse and corpse.carried_inventory and corpse.carried_inventory:get(physical_id)
  if entry and entry.item.item_type ~= "component" then
    return { applied = false, corpse_id = corpse.id, component_id = physical_id, reason = "Corpse cargo item is not a component" }
  end
  return Salvage.carried_item(corpse, physical_id, inventory)
end

return Salvage
