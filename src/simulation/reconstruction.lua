-- Authoritative, transactional transfers between the run inventory and a
-- Body. UI code only asks this service to perform an operation; it never
-- mutates slots or inventory cells itself.
local PhysicalItem = require("src.inventory.physical_item")

local Reconstruction = {}

local function failed(reason, details)
  local result = details or {}
  result.applied = false
  result.reason = reason
  return result
end

function Reconstruction.compatibility(body, inventory, component_id, slot_id)
  local entry = inventory:get(component_id)
  if not entry then
    return failed("Component is not in inventory", { component_id = component_id, slot_id = slot_id })
  end
  if entry.item.item_type ~= "component" or not entry.item.object then
    return failed("Item is not a body component", { component_id = component_id, slot_id = slot_id })
  end
  if entry.item.object.id ~= component_id then
    return failed("Inventory item physical identity is invalid", { component_id = component_id, slot_id = slot_id })
  end
  local allowed, reason = body:can_install(slot_id, entry.item.object)
  if not allowed then
    return failed(reason, { component_id = component_id, slot_id = slot_id })
  end
  return {
    applied = true,
    compatible = true,
    component_id = component_id,
    slot_id = slot_id,
    component = entry.item.object,
  }
end

function Reconstruction.install(body, inventory, component_id, slot_id)
  local check = Reconstruction.compatibility(body, inventory, component_id, slot_id)
  if not check.applied then
    return check
  end
  local entry = inventory:get(component_id)
  local component = entry.item.object
  local item, removed_entry = inventory:remove(component_id)
  if not item then
    return failed(removed_entry, { component_id = component_id, slot_id = slot_id })
  end
  local installed, install_reason = body:install(slot_id, component)
  if not installed then
    local restored, restore_reason = inventory:place(item, removed_entry.x, removed_entry.y, removed_entry.rotated)
    assert(restored, restore_reason)
    return failed(install_reason, { component_id = component_id, slot_id = slot_id })
  end
  assert(installed == component, "Reconstruction installed a different physical component")
  return {
    applied = true,
    action = "install",
    component_id = component.id,
    definition_id = component.definition_id,
    slot_id = slot_id,
  }
end

function Reconstruction.uninstall(body, inventory, registry, slot_id)
  local component = body:get_component(slot_id)
  if not component then
    return failed("Body slot is empty", { slot_id = slot_id })
  end
  local item = PhysicalItem.from_component(component, registry)
  local placement, placement_reason = inventory:find_first_fit(item)
  if not placement then
    return failed(placement_reason, { component_id = component.id, slot_id = slot_id })
  end
  local detached, detach_reason = body:detach(slot_id)
  if not detached then
    return failed(detach_reason, { component_id = component.id, slot_id = slot_id })
  end
  assert(detached == component, "Reconstruction detached a different physical component")
  local entry, place_reason = inventory:place(item, placement.x, placement.y, placement.rotated)
  if not entry then
    local restored, restore_reason = body:install(slot_id, detached)
    assert(restored, restore_reason)
    return failed(place_reason, { component_id = component.id, slot_id = slot_id })
  end
  return {
    applied = true,
    action = "uninstall",
    component_id = component.id,
    definition_id = component.definition_id,
    slot_id = slot_id,
    entry = entry,
  }
end

return Reconstruction
