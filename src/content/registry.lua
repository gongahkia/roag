-- Small first-party content loader and validator. It indexes declarative Lua
-- data; behavior remains in simulation systems.
local Registry = {}
Registry.__index = Registry

local KNOWN_ABILITY_IMPLEMENTATIONS = {
  self_destruct = true,
  projectile = true,
  area_burst = true,
  locomotion = true,
}

local function content_error(message)
  error("Content validation failed: " .. message, 3)
end

local function require_string(value, label)
  if type(value) ~= "string" or value == "" then
    content_error(label .. " must be a non-empty string")
  end
end

local function require_positive_number(value, label)
  if type(value) ~= "number" or value <= 0 then
    content_error(label .. " must be a positive number")
  end
end

local function require_nonnegative_number(value, label)
  if type(value) ~= "number" or value < 0 then
    content_error(label .. " must be a non-negative number")
  end
end

local function require_positive_integer(value, label)
  if type(value) ~= "number" or value <= 0 or value % 1 ~= 0 then
    content_error(label .. " must be a positive integer")
  end
end

local function validate_declarative(value, path, seen)
  local value_type = type(value)
  if value_type == "function" then
    content_error(path .. " must be declarative data, not a function")
  end
  if value_type ~= "table" then
    return
  end
  seen = seen or {}
  if seen[value] then
    content_error(path .. " contains a cyclic table")
  end
  seen[value] = true
  for key, child in pairs(value) do
    validate_declarative(child, path .. "." .. tostring(key), seen)
  end
  seen[value] = nil
end

local function valid_id(id, namespace)
  local escaped_namespace = namespace:gsub("([^%w])", "%%%1")
  return type(id) == "string" and id:match("^" .. escaped_namespace .. "%.[a-z0-9_%.]+$") ~= nil
end

local function list_contains(values, expected)
  for _, value in ipairs(values) do
    if value == expected then
      return true
    end
  end
  return false
end

local function sorted_keys(values)
  local keys = {}
  for key in pairs(values) do
    keys[#keys + 1] = key
  end
  table.sort(keys)
  return keys
end

function Registry.new(sources)
  local self = setmetatable({
    abilities = {},
    materials = {},
    world_objects = {},
    hazards = {},
    components = {},
    topologies = {},
    actors = {},
    enemies = {},
  }, Registry)
  self:_index("ability", sources.abilities, self.abilities)
  self:_index("material", sources.materials, self.materials)
  self:_index("world_object", sources.world_objects, self.world_objects)
  self:_index("hazard", sources.hazards, self.hazards)
  self:_index("component", sources.components, self.components)
  self:_index("body.topology", sources.topologies, self.topologies)
  self:_index("actor", sources.actors, self.actors)
  self:_index("enemy", sources.enemies, self.enemies)
  self:validate()
  return self
end

function Registry.load()
  return Registry.new({
    abilities = require("content.abilities.legacy"),
    materials = require("content.materials.legacy"),
    world_objects = require("content.world_objects.legacy"),
    hazards = require("content.hazards.legacy"),
    components = require("content.components.legacy"),
    topologies = require("content.body_topologies.normal"),
    actors = require("content.actors.player_legacy"),
    enemies = require("content.enemies.legacy"),
  })
end

function Registry:_index(namespace, definitions, destination)
  if type(definitions) ~= "table" then
    content_error(namespace .. " definitions must be a list")
  end
  for index, definition in ipairs(definitions) do
    validate_declarative(definition, namespace .. "[" .. index .. "]")
    if type(definition) ~= "table" then
      content_error(namespace .. "[" .. index .. "] must be a table")
    end
    if not valid_id(definition.id, namespace) then
      content_error(namespace .. "[" .. index .. "] has invalid semantic ID " .. tostring(definition.id))
    end
    if destination[definition.id] then
      content_error("Duplicate " .. namespace .. " ID '" .. definition.id .. "'")
    end
    destination[definition.id] = definition
  end
end

function Registry:_get(collection, kind, id)
  local definition = collection[id]
  if not definition then
    content_error("Unknown " .. kind .. " ID '" .. tostring(id) .. "'")
  end
  return definition
end

function Registry:get_ability(id)
  return self:_get(self.abilities, "ability", id)
end

function Registry:get_material(id)
  return self:_get(self.materials, "material", id)
end

function Registry:get_world_object(id)
  return self:_get(self.world_objects, "world object", id)
end

function Registry:get_hazard(id)
  return self:_get(self.hazards, "hazard", id)
end

function Registry:get_component(id)
  return self:_get(self.components, "component", id)
end

function Registry:get_topology(id)
  return self:_get(self.topologies, "body topology", id)
end

function Registry:get_actor(id)
  return self:_get(self.actors, "actor", id)
end

function Registry:get_enemy(id)
  return self:_get(self.enemies, "enemy", id)
end

function Registry:validate()
  for _, id in ipairs(sorted_keys(self.materials)) do
    local material = self.materials[id]
    require_string(material.display_name, "Material '" .. id .. "' display_name")
    for _, field in ipairs({ "solid", "blocks_movement", "blocks_vision", "destructible" }) do
      if type(material[field]) ~= "boolean" then
        content_error("Material '" .. id .. "' " .. field .. " must be a boolean")
      end
    end
    if material.destructible then
      require_positive_number(material.max_integrity, "Material '" .. id .. "' max_integrity")
      require_string(material.destruction_material_id, "Material '" .. id .. "' destruction_material_id")
      self:get_material(material.destruction_material_id)
    elseif material.max_integrity ~= nil or material.destruction_material_id ~= nil then
      content_error("Indestructible material '" .. id .. "' cannot define mutable destruction fields")
    end
  end

  for _, id in ipairs(sorted_keys(self.world_objects)) do
    local object = self.world_objects[id]
    require_string(object.display_name, "World object '" .. id .. "' display_name")
    require_string(object.material_id, "World object '" .. id .. "' material_id")
    local material = self:get_material(object.material_id)
    if not material.destructible then
      content_error("World object '" .. id .. "' must reference a destructible material")
    end
    for _, field in ipairs({ "blocks_movement", "blocks_vision", "blocks_projectiles", "movable_by_force" }) do
      if type(object[field]) ~= "boolean" then
        content_error("World object '" .. id .. "' " .. field .. " must be a boolean")
      end
    end
    require_string(object.render_style, "World object '" .. id .. "' render_style")
  end

  for _, id in ipairs(sorted_keys(self.hazards)) do
    local hazard = self.hazards[id]
    require_string(hazard.display_name, "Hazard '" .. id .. "' display_name")
    if hazard.trigger ~= "on_enter" then
      content_error("Hazard '" .. id .. "' trigger must be 'on_enter'")
    end
    if type(hazard.effect) ~= "table" then
      content_error("Hazard '" .. id .. "' effect must be a table")
    end
    if hazard.effect.type ~= "kinetic_damage" then
      content_error("Hazard '" .. id .. "' effect.type must be 'kinetic_damage'")
    end
    require_positive_integer(hazard.effect.amount, "Hazard '" .. id .. "' effect.amount")
    require_string(hazard.render_style, "Hazard '" .. id .. "' render_style")
  end

  local known_slot_kinds = {}
  for _, id in ipairs(sorted_keys(self.topologies)) do
    local topology = self.topologies[id]
    require_string(topology.display_name, "Topology '" .. id .. "' display_name")
    if type(topology.slots) ~= "table" or #topology.slots == 0 then
      content_error("Topology '" .. id .. "' must define slots")
    end
    local slot_ids = {}
    for index, slot in ipairs(topology.slots) do
      if type(slot) ~= "table" then
        content_error("Topology '" .. id .. "' slot " .. index .. " must be a table")
      end
      require_string(slot.id, "Topology '" .. id .. "' slot " .. index .. " id")
      require_string(slot.kind, "Topology '" .. id .. "' slot '" .. slot.id .. "' kind")
      if slot_ids[slot.id] then
        content_error("Topology '" .. id .. "' has duplicate slot ID '" .. slot.id .. "'")
      end
      slot_ids[slot.id] = slot
      known_slot_kinds[slot.kind] = true
    end
  end

  for _, id in ipairs(sorted_keys(self.components)) do
    local component = self.components[id]
    require_string(component.display_name, "Component '" .. id .. "' display_name")
    require_positive_number(component.max_integrity, "Component '" .. id .. "' max_integrity")
    require_nonnegative_number(component.mass, "Component '" .. id .. "' mass")
    require_nonnegative_number(component.wear_per_use, "Component '" .. id .. "' wear_per_use")
    if type(component.inventory) ~= "table" then
      content_error("Component '" .. id .. "' inventory must be a table")
    end
    require_positive_integer(component.inventory.width, "Component '" .. id .. "' inventory.width")
    require_positive_integer(component.inventory.height, "Component '" .. id .. "' inventory.height")
    if type(component.inventory.rotatable) ~= "boolean" then
      content_error("Component '" .. id .. "' inventory.rotatable must be a boolean")
    end
    if type(component.compatible_slots) ~= "table" or #component.compatible_slots == 0 then
      content_error("Component '" .. id .. "' must list compatible_slots")
    end
    for _, slot_kind in ipairs(component.compatible_slots) do
      require_string(slot_kind, "Component '" .. id .. "' compatible slot")
      if not known_slot_kinds[slot_kind] then
        content_error("Component '" .. id .. "' references unknown slot kind '" .. slot_kind .. "'")
      end
    end
    if type(component.abilities) ~= "table" then
      content_error("Component '" .. id .. "' abilities must be a list")
    end
    for _, ability_id in ipairs(component.abilities) do
      self:get_ability(ability_id)
    end
  end

  for _, id in ipairs(sorted_keys(self.abilities)) do
    local ability = self.abilities[id]
    require_string(ability.display_name, "Ability '" .. id .. "' display_name")
    require_string(ability.implementation, "Ability '" .. id .. "' implementation")
    if not KNOWN_ABILITY_IMPLEMENTATIONS[ability.implementation] then
      content_error("Ability '" .. id .. "' has unknown implementation '" .. ability.implementation .. "'")
    end
    if ability.activation_type ~= nil and ability.activation_type ~= "direct" and ability.activation_type ~= "body" then
      content_error("Ability '" .. id .. "' activation_type must be 'direct' or 'body'")
    end
    if ability.resource ~= nil then
      if type(ability.resource) ~= "table" then
        content_error("Ability '" .. id .. "' resource must be a table")
      end
      require_string(ability.resource.name, "Ability '" .. id .. "' resource.name")
      require_positive_integer(ability.resource.amount, "Ability '" .. id .. "' resource.amount")
    end
    for _, field in ipairs({ "range", "radius", "delay" }) do
      if ability[field] ~= nil then
        require_positive_integer(ability[field], "Ability '" .. id .. "' " .. field)
      end
    end
  end

  self:_validate_actor_collection("Actor", self.actors)
  self:_validate_actor_collection("Enemy", self.enemies)
  return true
end

function Registry:_validate_actor_collection(kind, definitions)
  for _, id in ipairs(sorted_keys(definitions)) do
    local definition = definitions[id]
    require_string(definition.display_name, kind .. " '" .. id .. "' display_name")
    local topology = self:get_topology(definition.body_topology_id)
    if type(definition.installed_components) ~= "table" then
      content_error(kind .. " '" .. id .. "' installed_components must be a list")
    end
    local slots = {}
    for _, slot in ipairs(topology.slots) do
      slots[slot.id] = slot
    end
    local occupied = {}
    for index, installation in ipairs(definition.installed_components) do
      if type(installation) ~= "table" then
        content_error(kind .. " '" .. id .. "' installation " .. index .. " must be a table")
      end
      require_string(installation.slot_id, kind .. " '" .. id .. "' installation slot_id")
      require_string(installation.component_id, kind .. " '" .. id .. "' installation component_id")
      local slot = slots[installation.slot_id]
      if not slot then
        content_error(kind .. " '" .. id .. "' references unknown slot '" .. installation.slot_id .. "'")
      end
      if occupied[installation.slot_id] then
        content_error(kind .. " '" .. id .. "' installs multiple components in slot '" .. installation.slot_id .. "'")
      end
      occupied[installation.slot_id] = true
      local component = self:get_component(installation.component_id)
      if not list_contains(component.compatible_slots, slot.kind) then
        content_error(kind .. " '" .. id .. "' installs incompatible component '" .. installation.component_id .. "' in slot '" .. installation.slot_id .. "'")
      end
    end
  end
end

return Registry
