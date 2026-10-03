-- Adapters turn physical simulation objects into inventory entries. Inventory
-- only relies on this small data contract, so conventional items can join it
-- later without making inventory component-specific.
local Component = require("src.body.component")

local PhysicalItem = {}

local function resource_name(definition, quantity)
  return definition.display_name .. " ×" .. quantity
end

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
      shape = definition.inventory.shape,
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

-- Resources deliberately use the same small inventory-item contract as
-- components, while retaining their own lightweight data shape. A stack's
-- physical ID names the stack instance rather than an abstract wallet entry.
function PhysicalItem.from_resource(resource_id, quantity, physical_id, registry)
  local definition = registry:get_resource(resource_id)
  assert(type(physical_id) == "string" and physical_id ~= "", "Resource stack requires a stable physical ID")
  assert(type(quantity) == "number" and quantity >= 1 and quantity % 1 == 0 and quantity <= definition.max_stack,
    "Resource stack quantity is invalid")
  local item = {
    item_type = "resource_stack",
    physical_id = physical_id,
    resource_id = definition.id,
    quantity = quantity,
    display_name = resource_name(definition, quantity),
    footprint = {
      width = definition.inventory.width,
      height = definition.inventory.height,
      rotatable = definition.inventory.rotatable,
      shape = definition.inventory.shape,
    },
    mass = definition.mass_per_unit * quantity,
    max_stack = definition.max_stack,
  }
  item.to_data = function(value)
    return {
      item_type = value.item_type,
      physical_id = value.physical_id,
      resource_id = value.resource_id,
      quantity = value.quantity,
    }
  end
  item.set_quantity = function(value, next_quantity)
    assert(type(next_quantity) == "number" and next_quantity >= 1 and next_quantity % 1 == 0
      and next_quantity <= definition.max_stack, "Resource stack quantity is invalid")
    value.quantity = next_quantity
    value.mass = definition.mass_per_unit * next_quantity
    value.display_name = resource_name(definition, next_quantity)
    return value
  end
  return item
end

function PhysicalItem.is_resource(item)
  return type(item) == "table" and item.item_type == "resource_stack"
end

function PhysicalItem.from_data(data, registry)
  assert(type(data) == "table", "Unsupported physical item data")
  if data.item_type == "component" then
    local component = Component.from_data(registry:get_component(data.component.definition_id), data.component)
    assert(component.id == data.physical_id, "Physical item ID does not match component ID")
    return PhysicalItem.from_component(component, registry)
  elseif data.item_type == "resource_stack" then
    return PhysicalItem.from_resource(data.resource_id, data.quantity, data.physical_id, registry)
  end
  error("Unsupported physical item data")
end

return PhysicalItem
