-- Adapters turn physical simulation objects into inventory entries. Inventory
-- only relies on this small data contract, so conventional items can join it
-- later without making inventory component-specific.
local Component = require("src.body.component")

local PhysicalItem = {}

function PhysicalItem.from_component(component, registry)
  local definition = registry:get_component(component.definition_id)
  return {
    item_type = "component",
    physical_id = component.id,
    display_name = definition.display_name,
    footprint = {
      width = definition.inventory.width,
      height = definition.inventory.height,
      rotatable = definition.inventory.rotatable,
    },
    mass = definition.mass,
    object = component,
    to_data = function(item)
      return {
        item_type = item.item_type,
        physical_id = item.physical_id,
        component = Component.to_data(item.object),
      }
    end,
  }
end

function PhysicalItem.from_data(data, registry)
  assert(type(data) == "table" and data.item_type == "component", "Unsupported physical item data")
  local component = Component.from_data(registry:get_component(data.component.definition_id), data.component)
  assert(component.id == data.physical_id, "Physical item ID does not match component ID")
  return PhysicalItem.from_component(component, registry)
end

return PhysicalItem
