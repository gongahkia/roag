local Component = require("src.body.component")

local Body = {}
Body.__index = Body

local function copy_slot(slot)
  return {
    id = slot.id,
    kind = slot.kind,
    component = nil,
  }
end

local function supports_slot(component_definition, slot_kind)
  for _, compatible_kind in ipairs(component_definition.compatible_slots) do
    if compatible_kind == slot_kind then
      return true
    end
  end
  return false
end

function Body.new(registry, topology_id)
  local topology = registry:get_topology(topology_id)
  local self = setmetatable({
    registry = registry,
    topology_id = topology.id,
    slots = {},
    slot_order = {},
  }, Body)
  for _, slot in ipairs(topology.slots) do
    self.slots[slot.id] = copy_slot(slot)
    self.slot_order[#self.slot_order + 1] = slot.id
  end
  return self
end

function Body:get_slot(slot_id)
  return self.slots[slot_id]
end

function Body:get_component(slot_id)
  local slot = self:get_slot(slot_id)
  return slot and slot.component or nil
end

function Body:install(slot_id, component)
  local slot = self:get_slot(slot_id)
  if not slot then
    return nil, "Unknown body slot '" .. tostring(slot_id) .. "'"
  end
  if slot.component then
    return nil, "Body slot '" .. slot_id .. "' is already occupied"
  end
  local definition = self.registry:get_component(component.definition_id)
  if not supports_slot(definition, slot.kind) then
    return nil, "Component '" .. component.definition_id .. "' is incompatible with slot '" .. slot_id .. "'"
  end
  slot.component = component
  return component
end

function Body:uninstall(slot_id)
  local slot = self:get_slot(slot_id)
  if not slot then
    return nil, "Unknown body slot '" .. tostring(slot_id) .. "'"
  end
  local component = slot.component
  slot.component = nil
  return component
end

function Body:list_components()
  local components = {}
  for _, slot_id in ipairs(self.slot_order) do
    local component = self.slots[slot_id].component
    if component then
      components[#components + 1] = component
    end
  end
  return components
end

function Body:list_installed_slots()
  local installed = {}
  for _, slot_id in ipairs(self.slot_order) do
    local slot = self.slots[slot_id]
    if slot.component then
      installed[#installed + 1] = {
        slot_id = slot_id,
        slot = slot,
        component = slot.component,
      }
    end
  end
  return installed
end

function Body:find_component(component_id)
  for _, installed in ipairs(self:list_installed_slots()) do
    if installed.component.id == component_id then
      return installed
    end
  end
  return nil
end

function Body:detach(slot_id)
  local slot = self:get_slot(slot_id)
  if not slot then
    return nil, "Unknown body slot '" .. tostring(slot_id) .. "'"
  end
  if not slot.component then
    return nil, "Body slot '" .. slot_id .. "' is empty"
  end
  local component = slot.component
  slot.component = nil
  return component
end

function Body:capability_providers(ability_id)
  local providers = {}
  for _, slot_id in ipairs(self.slot_order) do
    local component = self.slots[slot_id].component
    if component and Component.is_functional(component) then
      local definition = self.registry:get_component(component.definition_id)
      for _, provided_ability in ipairs(definition.abilities) do
        if provided_ability == ability_id then
          providers[#providers + 1] = component
          break
        end
      end
    end
  end
  return providers
end

function Body:has_capability(ability_id)
  return #self:capability_providers(ability_id) > 0
end

function Body:list_capabilities()
  local abilities, seen = {}, {}
  for _, component in ipairs(self:list_components()) do
    local definition = self.registry:get_component(component.definition_id)
    for _, ability_id in ipairs(definition.abilities) do
      if not seen[ability_id] then
        abilities[#abilities + 1] = ability_id
        seen[ability_id] = true
      end
    end
  end
  return abilities
end

function Body:installed_mass()
  local mass = 0
  for _, component in ipairs(self:list_components()) do
    mass = mass + self.registry:get_component(component.definition_id).mass
  end
  return mass
end

function Body:to_data()
  local slots = {}
  for _, slot_id in ipairs(self.slot_order) do
    local component = self.slots[slot_id].component
    slots[#slots + 1] = {
      slot_id = slot_id,
      component = component and Component.to_data(component) or nil,
    }
  end
  return {
    topology_id = self.topology_id,
    slots = slots,
  }
end

return Body
