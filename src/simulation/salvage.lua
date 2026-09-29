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

return Salvage
