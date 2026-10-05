-- Small first-party content loader and validator. It indexes declarative Lua
-- data; behavior remains in simulation systems.
local Registry = {}
Registry.__index = Registry

local KNOWN_ABILITY_IMPLEMENTATIONS = {
  self_destruct = true,
  projectile = true,
  scattershot = true,
  piercing_projectile = true,
  area_burst = true,
  locomotion = true,
  electrical_discharge = true,
  melee = true,
  dash = true,
}

local KNOWN_INTERACTION_ROLES = {
  door = true,
  generator = true,
  breaker = true,
  service = true,
  traversal = true,
  discovery = true,
  clue = true,
  reinforcement = true,
  zone_connection = true,
  reconstruction_station = true,
  storage = true,
}

local KNOWN_REINFORCEMENT_SOURCE_TYPES = { nest = true, lift = true }
local KNOWN_AI_ROLES = { rusher = true, flanker = true, skirmisher = true, controller = true, heavy = true }

local KNOWN_DISCOVERY_ACCESS_PROFILES = {
  ["access_profile.discovery.open"] = true,
  ["access_profile.discovery.breachable"] = true,
  ["access_profile.discovery.powered"] = true,
  ["access_profile.discovery.maintenance_hatch"] = true,
}

local KNOWN_SERVICE_ROLES = { supply = true, repair = true, salvager = true, charm_vendor = true }
local KNOWN_TOOL_FAMILIES = { axe = true, pickaxe = true, cutter = true, drill = true }
local KNOWN_ATTACK_TAGS = {
  melee = true, projectile = true, scatter = true, piercing = true,
  electric = true, explosive = true, fire = true, forceful = true, tool = true,
}
local KNOWN_BUILD_EFFECT_TRIGGERS = {
  on_attack = true, on_hit = true, on_kill = true, on_component_break = true,
  on_pierce = true, on_push_collision = true, on_reload = true, on_terrain_break = true,
}
local KNOWN_BUILD_EFFECT_KINDS = {
  chain_electricity = true, kinetic_burst = true, small_explosion = true,
  magazine_refund = true, cooldown_reduction = true, ignite = true,
}
local KNOWN_MODIFIERS = {
  max_health = true,
  dash_cooldown = true,
  bomb_radius = true,
  flare_light = true,
  reload_bonus = true,
  objective_required = true,
  charm_slots = true,
  inventory_rows = true,
  starting_scrap = true,
  melee_damage = true,
  melee_force = true,
  projectile_damage = true,
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

local function validate_inventory_shape(inventory, label)
  local shape = inventory.shape
  if shape == nil then return end
  if type(shape) ~= "table" or #shape ~= inventory.height then
    content_error(label .. ".shape must have exactly inventory.height rows")
  end
  local occupied = false
  for row_index, row in ipairs(shape) do
    if type(row) ~= "string" or #row ~= inventory.width or row:find("[^01]") then
      content_error(label .. ".shape row " .. row_index .. " must be a " .. inventory.width .. "-character 0/1 string")
    end
    occupied = occupied or row:find("1", 1, true) ~= nil
  end
  if not occupied then
    content_error(label .. ".shape must occupy at least one cell")
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
    liquids = {},
    gases = {},
    world_objects = {},
    hazards = {},
    components = {},
    topologies = {},
    actors = {},
    factions = {},
    enemies = {},
    reinforcement_profiles = {},
    bosses = {},
    boss_arenas = {},
    encounter_pools = {},
    services = {},
    boons = {},
    charms = {},
    curses = {},
    research = {},
    discoveries = {},
    resources = {},
    tools = {},
    construction_recipes = {},
  }, Registry)
  self:_index("ability", sources.abilities, self.abilities)
  self:_index("material", sources.materials, self.materials)
  self:_index("liquid", sources.liquids, self.liquids)
  self:_index("gas", sources.gases, self.gases)
  self:_index("world_object", sources.world_objects, self.world_objects)
  self:_index("hazard", sources.hazards, self.hazards)
  self:_index("component", sources.components, self.components)
  self:_index("body.topology", sources.topologies, self.topologies)
  self:_index("actor", sources.actors, self.actors)
  self:_index("faction", sources.factions or {}, self.factions)
  self:_index("enemy", sources.enemies, self.enemies)
  self:_index("reinforcement_profile", sources.reinforcement_profiles or {}, self.reinforcement_profiles)
  self:_index("boss", sources.bosses or {}, self.bosses)
  self:_index("boss_arena", sources.boss_arenas or {}, self.boss_arenas)
  self:_index("encounter_pool", sources.encounter_pools or {}, self.encounter_pools)
  self:_index("service", sources.services or {}, self.services)
  self:_index("boon", sources.boons or {}, self.boons)
  self:_index("charm", sources.charms or {}, self.charms)
  self:_index("curse", sources.curses or {}, self.curses)
  self:_index("research", sources.research or {}, self.research)
  self:_index("discovery", sources.discoveries or {}, self.discoveries)
  -- Test/content callers historically supplied only the original source
  -- tables. Keep that contract while construction content supplies its own
  -- stable base vocabulary unless a caller explicitly overrides it.
  self:_index("resource", sources.resources or require("content.resources.legacy"), self.resources)
  self:_index("tool", sources.tools or require("content.tools.legacy"), self.tools)
  self:_index("construction", sources.construction_recipes or require("content.construction.legacy"), self.construction_recipes)
  self:validate()
  return self
end

function Registry.load()
  return Registry.new({
    abilities = require("content.abilities.legacy"),
    materials = require("content.materials.legacy"),
    liquids = require("content.liquids.legacy"),
    gases = require("content.gases.legacy"),
    world_objects = require("content.world_objects.legacy"),
    hazards = require("content.hazards.legacy"),
    components = require("content.components.legacy"),
    topologies = require("content.body_topologies.normal"),
    actors = require("content.actors.player_legacy"),
    factions = require("content.factions.legacy"),
    enemies = require("content.enemies.legacy"),
    reinforcement_profiles = require("content.reinforcements.legacy"),
    bosses = require("content.bosses.legacy"),
    boss_arenas = require("content.boss_arenas.legacy"),
    encounter_pools = require("content.encounters.legacy"),
    services = require("content.services.legacy"),
    boons = require("content.boons.legacy"),
    charms = require("content.charms.legacy"),
    curses = require("content.curses.legacy"),
    research = require("content.research.legacy"),
    discoveries = require("content.discoveries.legacy"),
    resources = require("content.resources.legacy"),
    tools = require("content.tools.legacy"),
    construction_recipes = require("content.construction.legacy"),
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

function Registry:get_liquid(id)
  return self:_get(self.liquids, "liquid", id)
end

function Registry:get_gas(id)
  return self:_get(self.gases, "gas", id)
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

function Registry:get_faction(id)
  return self:_get(self.factions, "faction", id)
end

function Registry:get_enemy(id)
  return self:_get(self.enemies, "enemy", id)
end

function Registry:get_boss(id)
  return self:_get(self.bosses, "boss", id)
end

function Registry:get_boss_arena(id)
  return self:_get(self.boss_arenas, "boss arena", id)
end

-- Route content lives in a separate registry, so this explicit join keeps a
-- boss node from silently pointing at missing physical content.
function Registry:validate_boss_routes(route_definitions)
  assert(route_definitions and route_definitions.profile_order, "Boss route validation requires route definitions")
  local referenced = {}
  for _, profile_id in ipairs(route_definitions.profile_order) do
    local profile = route_definitions:get_profile(profile_id)
    for _, layer in ipairs(profile.layers) do
      if layer.type == "boss" then
        for _, node in ipairs(layer.nodes) do
          local boss = self:get_boss(node.boss_id)
          self:get_boss_arena(boss.arena_profile_id)
          referenced[boss.id] = true
        end
      end
    end
  end
  for id in pairs(self.bosses) do
    if not referenced[id] then
      content_error("Boss '" .. id .. "' is production content but is unused by every route profile")
    end
  end
  return true
end

function Registry:get_encounter_pool(id)
  return self:_get(self.encounter_pools, "encounter pool", id)
end

function Registry:get_reinforcement_profile(id)
  return self:_get(self.reinforcement_profiles, "reinforcement profile", id)
end

function Registry:encounter_pool_for(biome_id, tier_id)
  return self.encounter_pools_by_pair and self.encounter_pools_by_pair[biome_id .. ":" .. tier_id] or nil
end

function Registry:get_service(id) return self:_get(self.services, "service", id) end
function Registry:get_boon(id) return self:_get(self.boons, "boon", id) end
function Registry:get_charm(id) return self:_get(self.charms, "charm", id) end
function Registry:get_curse(id) return self:_get(self.curses, "curse", id) end
function Registry:get_research(id) return self:_get(self.research, "research", id) end
function Registry:get_discovery(id) return self:_get(self.discoveries, "discovery", id) end
function Registry:get_resource(id) return self:_get(self.resources, "resource", id) end
function Registry:get_tool(id) return self:_get(self.tools, "tool", id) end
function Registry:get_construction_recipe(id) return self:_get(self.construction_recipes, "construction recipe", id) end

function Registry:validate()
  -- Faction relationships are a small explicit matrix.  Different IDs are
  -- not implicitly hostile: this keeps target selection content-driven.
  for _, id in ipairs(sorted_keys(self.factions)) do
    local faction = self.factions[id]
    require_string(faction.display_name, "Faction '" .. id .. "' display_name")
    if type(faction.hostile_faction_ids) ~= "table" then
      content_error("Faction '" .. id .. "' hostile_faction_ids must be a list")
    end
    local seen = {}
    for _, hostile_id in ipairs(faction.hostile_faction_ids) do
      require_string(hostile_id, "Faction '" .. id .. "' hostile faction")
      if hostile_id == id or seen[hostile_id] then
        content_error("Faction '" .. id .. "' has an invalid hostile relationship")
      end
      self:get_faction(hostile_id)
      seen[hostile_id] = true
    end
    if faction.synthetic ~= nil and type(faction.synthetic) ~= "boolean" then
      content_error("Faction '" .. id .. "' synthetic must be a boolean")
    end
    if type(faction.presentation) ~= "table" then
      content_error("Faction '" .. id .. "' presentation must be a table")
    end
    require_string(faction.presentation.color, "Faction '" .. id .. "' presentation.color")
  end
  for _, id in ipairs(sorted_keys(self.factions)) do
    for _, hostile_id in ipairs(self.factions[id].hostile_faction_ids) do
      if not list_contains(self.factions[hostile_id].hostile_faction_ids, id) then
        content_error("Faction hostility must be symmetric: '" .. id .. "' -> '" .. hostile_id .. "'")
      end
    end
  end

  for _, id in ipairs(sorted_keys(self.materials)) do
    local material = self.materials[id]
    require_string(material.display_name, "Material '" .. id .. "' display_name")
    for _, field in ipairs({ "solid", "blocks_movement", "blocks_vision", "destructible", "flammable", "conductive" }) do
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
    if material.flammable then
      if not material.destructible then
        content_error("Flammable material '" .. id .. "' must be destructible")
      end
      require_positive_number(material.burn_rate, "Material '" .. id .. "' burn_rate")
    elseif material.burn_rate ~= nil then
      content_error("Nonflammable material '" .. id .. "' cannot define burn_rate")
    end
    if material.harvest_yield ~= nil then
      if type(material.harvest_yield) ~= "table" then
        content_error("Material '" .. id .. "' harvest_yield must be a table")
      end
      require_string(material.harvest_yield.resource_id, "Material '" .. id .. "' harvest_yield.resource_id")
      self:get_resource(material.harvest_yield.resource_id)
      require_positive_integer(material.harvest_yield.amount, "Material '" .. id .. "' harvest_yield.amount")
    end
    if material.tool_effectiveness ~= nil then
      if not material.destructible or type(material.tool_effectiveness) ~= "table" then
        content_error("Material '" .. id .. "' tool_effectiveness requires a destructible material")
      end
      for family, effectiveness in pairs(material.tool_effectiveness) do
        if not KNOWN_TOOL_FAMILIES[family] then
          content_error("Material '" .. id .. "' references unknown tool family '" .. tostring(family) .. "'")
        end
        require_positive_number(effectiveness, "Material '" .. id .. "' tool effectiveness")
      end
    end
  end

  for _, id in ipairs(sorted_keys(self.resources)) do
    local resource = self.resources[id]
    require_string(resource.display_name, "Resource '" .. id .. "' display_name")
    require_positive_number(resource.mass_per_unit, "Resource '" .. id .. "' mass_per_unit")
    require_positive_integer(resource.max_stack, "Resource '" .. id .. "' max_stack")
    if type(resource.inventory) ~= "table" then
      content_error("Resource '" .. id .. "' inventory must be a table")
    end
    require_positive_integer(resource.inventory.width, "Resource '" .. id .. "' inventory.width")
    require_positive_integer(resource.inventory.height, "Resource '" .. id .. "' inventory.height")
    if type(resource.inventory.rotatable) ~= "boolean" then
      content_error("Resource '" .. id .. "' inventory.rotatable must be a boolean")
    end
    validate_inventory_shape(resource.inventory, "Resource '" .. id .. "' inventory")
  end

  for _, id in ipairs(sorted_keys(self.tools)) do
    local tool = self.tools[id]
    require_string(tool.display_name, "Tool '" .. id .. "' display_name")
    if not KNOWN_TOOL_FAMILIES[tool.family] then content_error("Tool '" .. id .. "' has unknown family") end
    require_positive_number(tool.mass, "Tool '" .. id .. "' mass")
    require_positive_integer(tool.max_durability, "Tool '" .. id .. "' max_durability")
    if type(tool.inventory) ~= "table" then content_error("Tool '" .. id .. "' inventory must be a table") end
    require_positive_integer(tool.inventory.width, "Tool '" .. id .. "' inventory.width")
    require_positive_integer(tool.inventory.height, "Tool '" .. id .. "' inventory.height")
    if type(tool.inventory.rotatable) ~= "boolean" then content_error("Tool '" .. id .. "' inventory.rotatable must be a boolean") end
    validate_inventory_shape(tool.inventory, "Tool '" .. id .. "' inventory")
    if type(tool.combat) ~= "table" then content_error("Tool '" .. id .. "' combat must be a table") end
    require_positive_integer(tool.combat.damage, "Tool '" .. id .. "' combat.damage")
    require_nonnegative_number(tool.combat.force, "Tool '" .. id .. "' combat.force")
    if type(tool.modification) ~= "table" then content_error("Tool '" .. id .. "' modification must be a table") end
    require_positive_number(tool.modification.damage, "Tool '" .. id .. "' modification.damage")
  end

  for _, id in ipairs(sorted_keys(self.liquids)) do
    local liquid = self.liquids[id]
    require_string(liquid.display_name, "Liquid '" .. id .. "' display_name")
    require_positive_integer(liquid.max_depth, "Liquid '" .. id .. "' max_depth")
    if type(liquid.extinguishes_fire) ~= "boolean" then
      content_error("Liquid '" .. id .. "' extinguishes_fire must be a boolean")
    end
    if type(liquid.conductive) ~= "boolean" then
      content_error("Liquid '" .. id .. "' conductive must be a boolean")
    end
    require_string(liquid.render_style, "Liquid '" .. id .. "' render_style")
  end

  for _, id in ipairs(sorted_keys(self.gases)) do
    local gas = self.gases[id]
    require_string(gas.display_name, "Gas '" .. id .. "' display_name")
    require_positive_integer(gas.max_concentration, "Gas '" .. id .. "' max_concentration")
    require_positive_integer(gas.exposure_threshold, "Gas '" .. id .. "' exposure_threshold")
    if gas.exposure_threshold > gas.max_concentration then
      content_error("Gas '" .. id .. "' exposure_threshold cannot exceed max_concentration")
    end
    require_nonnegative_number(gas.damage, "Gas '" .. id .. "' damage")
    require_string(gas.render_style, "Gas '" .. id .. "' render_style")
  end

  for _, id in ipairs(sorted_keys(self.world_objects)) do
    local object = self.world_objects[id]
    require_string(object.display_name, "World object '" .. id .. "' display_name")
    require_string(object.material_id, "World object '" .. id .. "' material_id")
    local material = self:get_material(object.material_id)
    if not material.destructible then
      content_error("World object '" .. id .. "' must reference a destructible material")
    end
    for _, field in ipairs({ "blocks_movement", "blocks_vision", "blocks_projectiles", "blocks_gas", "movable_by_force" }) do
      if type(object[field]) ~= "boolean" then
        content_error("World object '" .. id .. "' " .. field .. " must be a boolean")
      end
    end
    if object.interaction_role ~= nil then
      require_string(object.interaction_role, "World object '" .. id .. "' interaction_role")
      if not KNOWN_INTERACTION_ROLES[object.interaction_role] then
        content_error("World object '" .. id .. "' has unknown interaction_role '" .. object.interaction_role .. "'")
      end
      if object.interaction_role == "door" then
        if type(object.power_required) ~= "boolean" then
          content_error("World object '" .. id .. "' power_required must be a boolean")
        end
        if object.default_door_state ~= "closed" then
          content_error("World object '" .. id .. "' default_door_state must be 'closed'")
        end
        if not object.blocks_movement or not object.blocks_vision or not object.blocks_projectiles or not object.blocks_gas then
          content_error("Door world object '" .. id .. "' must block movement, vision, projectiles, and gas while closed")
        end
      elseif object.interaction_role == "traversal" then
        require_string(object.required_unlock, "Traversal world object '" .. id .. "' required_unlock")
        if not object.required_unlock:match("^unlock%.[a-z0-9_%.]+$") then
          content_error("Traversal world object '" .. id .. "' required_unlock must be a stable unlock ID")
        end
        if not object.blocks_movement or not object.blocks_vision or not object.blocks_projectiles or not object.blocks_gas then
          content_error("Traversal world object '" .. id .. "' must block movement, vision, projectiles, and gas")
        end
      elseif object.interaction_role == "zone_connection" then
        if object.blocks_movement or object.blocks_vision or object.blocks_projectiles or object.blocks_gas then
          content_error("Zone connection world object '" .. id .. "' must remain passable and non-blocking")
        end
      elseif object.interaction_role == "reconstruction_station" then
        if object.blocks_movement or object.blocks_vision or object.blocks_projectiles or object.blocks_gas then
          content_error("Reconstruction station world object '" .. id .. "' must remain passable and non-blocking")
        end
      elseif object.interaction_role == "storage" then
        require_positive_integer(object.storage_width, "Storage world object '" .. id .. "' storage_width")
        require_positive_integer(object.storage_height, "Storage world object '" .. id .. "' storage_height")
      elseif object.interaction_role == "reinforcement" then
        require_string(object.reinforcement_source_type, "Reinforcement world object '" .. id .. "' reinforcement_source_type")
        if not KNOWN_REINFORCEMENT_SOURCE_TYPES[object.reinforcement_source_type] then
          content_error("Reinforcement world object '" .. id .. "' has unknown source type")
        end
        if not object.blocks_movement or not object.blocks_vision or not object.blocks_projectiles then
          content_error("Reinforcement world object '" .. id .. "' must visibly block movement, vision, and projectiles")
        end
      elseif object.power_required ~= nil or object.default_door_state ~= nil or object.required_unlock ~= nil then
        content_error("Non-door world object '" .. id .. "' cannot define door power metadata")
      end
      if object.interaction_role ~= "reinforcement" and object.reinforcement_source_type ~= nil then
        content_error("Non-reinforcement world object '" .. id .. "' cannot define reinforcement metadata")
      end
    elseif object.power_required ~= nil or object.default_door_state ~= nil or object.required_unlock ~= nil
      or object.reinforcement_source_type ~= nil then
      content_error("Non-interactable world object '" .. id .. "' cannot define door power metadata")
    end
    if object.harvest_yield ~= nil then
      if type(object.harvest_yield) ~= "table" then
        content_error("World object '" .. id .. "' harvest_yield must be a table")
      end
      require_string(object.harvest_yield.resource_id, "World object '" .. id .. "' harvest_yield.resource_id")
      self:get_resource(object.harvest_yield.resource_id)
      require_positive_integer(object.harvest_yield.amount, "World object '" .. id .. "' harvest_yield.amount")
    end
    require_string(object.render_style, "World object '" .. id .. "' render_style")
  end

  for _, id in ipairs(sorted_keys(self.construction_recipes)) do
    local recipe = self.construction_recipes[id]
    require_string(recipe.display_name, "Construction recipe '" .. id .. "' display_name")
    if recipe.kind ~= "structural_piece" and recipe.kind ~= "functional_device" then
      content_error("Construction recipe '" .. id .. "' kind must be structural_piece or functional_device")
    end
    require_string(recipe.description, "Construction recipe '" .. id .. "' description")
    require_string(recipe.world_object_id, "Construction recipe '" .. id .. "' world_object_id")
    self:get_world_object(recipe.world_object_id)
    if type(recipe.costs) ~= "table" or next(recipe.costs) == nil then
      content_error("Construction recipe '" .. id .. "' costs must be a non-empty map")
    end
    for resource_id, amount in pairs(recipe.costs) do
      self:get_resource(resource_id)
      require_positive_integer(amount, "Construction recipe '" .. id .. "' cost for '" .. tostring(resource_id) .. "'")
    end
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

  for _, id in ipairs(sorted_keys(self.services)) do
    local service = self.services[id]
    require_string(service.display_name, "Service '" .. id .. "' display_name")
    require_string(service.role, "Service '" .. id .. "' role")
    if not KNOWN_SERVICE_ROLES[service.role] then
      content_error("Service '" .. id .. "' has unknown role '" .. service.role .. "'")
    end
    require_string(service.stock_profile, "Service '" .. id .. "' stock_profile")
    require_string(service.render_style, "Service '" .. id .. "' render_style")
    if type(service.stock) ~= "table" then
      content_error("Service '" .. id .. "' stock must be a table")
    end
    for _, profile_id in ipairs({ "normal", "final_hub" }) do
      local stock = service.stock[profile_id]
      if type(stock) ~= "table" then
        content_error("Service '" .. id .. "' stock." .. profile_id .. " must be a table")
      end
      if service.role == "supply" then
        if type(stock.offers) ~= "table" or #stock.offers == 0 then
          content_error("Supply service '" .. id .. "' stock." .. profile_id .. ".offers must be a non-empty list")
        end
        for index, offer in ipairs(stock.offers) do
          if offer.resource_id ~= nil then
            require_string(offer.resource_id, "Supply service '" .. id .. "' offer " .. index .. " resource_id")
            if not self.resources[offer.resource_id] then
              content_error("Supply service '" .. id .. "' offer " .. index .. " references unknown resource '" .. offer.resource_id .. "'")
            end
            require_positive_integer(offer.quantity, "Supply service '" .. id .. "' offer " .. index .. " quantity")
            if offer.quantity > self.resources[offer.resource_id].max_stack then
              content_error("Supply service '" .. id .. "' offer " .. index .. " quantity exceeds resource stack limit")
            end
          elseif offer.tool_definition_id ~= nil then
            require_string(offer.tool_definition_id, "Supply service '" .. id .. "' offer " .. index .. " tool_definition_id")
            self:get_tool(offer.tool_definition_id)
          else
            require_string(offer.key, "Supply service '" .. id .. "' offer " .. index .. " key")
          end
          require_string(offer.label, "Supply service '" .. id .. "' offer " .. index .. " label")
          require_positive_integer(offer.price, "Supply service '" .. id .. "' offer " .. index .. " price")
          require_positive_integer(offer.remaining, "Supply service '" .. id .. "' offer " .. index .. " remaining")
        end
      elseif service.role == "repair" then
        require_positive_integer(stock.price, "Repair service '" .. id .. "' stock." .. profile_id .. " price")
        require_positive_integer(stock.remaining, "Repair service '" .. id .. "' stock." .. profile_id .. " remaining")
      else
        require_positive_integer(stock.offer_count, "Service '" .. id .. "' stock." .. profile_id .. " offer_count")
      end
    end
  end

  local function validate_modifiers(kind, id, modifiers)
    if type(modifiers) ~= "table" or next(modifiers) == nil then
      content_error(kind .. " '" .. id .. "' modifiers must be a non-empty table")
    end
    for key, value in pairs(modifiers) do
      if not KNOWN_MODIFIERS[key] then
        content_error(kind .. " '" .. id .. "' has unknown modifier '" .. tostring(key) .. "'")
      end
      if type(value) ~= "number" or value % 1 ~= 0 then
        content_error(kind .. " '" .. id .. "' modifier '" .. tostring(key) .. "' must be an integer")
      end
    end
  end
  for _, id in ipairs(sorted_keys(self.boons)) do
    local boon = self.boons[id]
    require_string(boon.display_name, "Boon '" .. id .. "' display_name")
    require_string(boon.description, "Boon '" .. id .. "' description")
    validate_modifiers("Boon", id, boon.modifiers)
  end
  for _, id in ipairs(sorted_keys(self.charms)) do
    local charm = self.charms[id]
    require_string(charm.display_name, "Charm '" .. id .. "' display_name")
    require_string(charm.description, "Charm '" .. id .. "' description")
    require_positive_number(charm.price, "Charm '" .. id .. "' price")
    require_string(charm.render_style, "Charm '" .. id .. "' render_style")
    if type(charm.granted_boon_ids) ~= "table" then
      content_error("Charm '" .. id .. "' granted_boon_ids must be a list")
    end
    for _, boon_id in ipairs(charm.granted_boon_ids) do self:get_boon(boon_id) end
    local reactive = charm.reactive_effects or {}
    if type(reactive) ~= "table" then content_error("Charm '" .. id .. "' reactive_effects must be a list") end
    if #charm.granted_boon_ids == 0 and #reactive == 0 then
      content_error("Charm '" .. id .. "' must grant a boon or reactive effect")
    end
    local seen_effects = {}
    for index, effect in ipairs(reactive) do
      if type(effect) ~= "table" then content_error("Charm '" .. id .. "' reactive effect " .. index .. " must be a table") end
      require_string(effect.id, "Charm '" .. id .. "' reactive effect " .. index .. " id")
      if seen_effects[effect.id] then content_error("Charm '" .. id .. "' duplicates reactive effect '" .. effect.id .. "'") end
      seen_effects[effect.id] = true
      if not KNOWN_BUILD_EFFECT_TRIGGERS[effect.trigger] then
        content_error("Charm '" .. id .. "' reactive effect '" .. effect.id .. "' has unknown trigger")
      end
      require_string(effect.description, "Charm '" .. id .. "' reactive effect '" .. effect.id .. "' description")
      if type(effect.effect) ~= "table" or not KNOWN_BUILD_EFFECT_KINDS[effect.effect.kind] then
        content_error("Charm '" .. id .. "' reactive effect '" .. effect.id .. "' has unknown effect kind")
      end
      local conditions = effect.conditions or {}
      if type(conditions) ~= "table" then content_error("Charm '" .. id .. "' reactive effect '" .. effect.id .. "' conditions must be a table") end
      if conditions.attack_tag ~= nil and not KNOWN_ATTACK_TAGS[conditions.attack_tag] then
        content_error("Charm '" .. id .. "' reactive effect '" .. effect.id .. "' has unknown attack tag")
      end
      if conditions.requires_capability ~= nil then self:get_ability(conditions.requires_capability) end
      if conditions.requires_ranged ~= nil and type(conditions.requires_ranged) ~= "boolean" then
        content_error("Charm '" .. id .. "' reactive effect '" .. effect.id .. "' requires_ranged must be a boolean")
      end
      for _, field in ipairs({ "amount", "max_cells", "damage", "radius", "force" }) do
        if effect.effect[field] ~= nil then
          require_positive_integer(effect.effect[field], "Charm '" .. id .. "' reactive effect '" .. effect.id .. "' " .. field)
        end
      end
      if effect.effect.ignite ~= nil and type(effect.effect.ignite) ~= "boolean" then
        content_error("Charm '" .. id .. "' reactive effect '" .. effect.id .. "' ignite must be a boolean")
      end
    end
  end
  for _, id in ipairs(sorted_keys(self.curses)) do
    local curse = self.curses[id]
    require_string(curse.display_name, "Curse '" .. id .. "' display_name")
    require_string(curse.description, "Curse '" .. id .. "' description")
    validate_modifiers("Curse", id, curse.modifiers)
  end

  local research_categories = { body = true, mobility = true, loadout = true, traversal = true, preparation = true, combat = true }
  for _, id in ipairs(sorted_keys(self.research)) do
    local node = self.research[id]
    require_string(node.display_name, "Research '" .. id .. "' display_name")
    require_string(node.description, "Research '" .. id .. "' description")
    if not research_categories[node.category] then
      content_error("Research '" .. id .. "' has unknown category '" .. tostring(node.category) .. "'")
    end
    require_positive_integer(node.cost, "Research '" .. id .. "' cost")
    if type(node.prerequisites) ~= "table" then content_error("Research '" .. id .. "' prerequisites must be a list") end
    local prerequisite_seen = {}
    for _, prerequisite in ipairs(node.prerequisites or {}) do
      if type(prerequisite) ~= "string" or not self.research[prerequisite] then
        content_error("Research '" .. id .. "' references unknown prerequisite '" .. tostring(prerequisite) .. "'")
      end
      if prerequisite == id or prerequisite_seen[prerequisite] then
        content_error("Research '" .. id .. "' has invalid prerequisite '" .. prerequisite .. "'")
      end
      prerequisite_seen[prerequisite] = true
    end
    local has_effect = false
    if node.modifiers ~= nil then validate_modifiers("Research", id, node.modifiers); has_effect = true end
    if node.unlocks ~= nil then
      if type(node.unlocks) ~= "table" or #node.unlocks == 0 then content_error("Research '" .. id .. "' unlocks must be a non-empty list") end
      local seen = {}
      for _, unlock in ipairs(node.unlocks) do
        if type(unlock) ~= "string" or not unlock:match("^unlock%.[a-z0-9_%.]+$") or seen[unlock] then
          content_error("Research '" .. id .. "' has invalid unlock '" .. tostring(unlock) .. "'")
        end
        seen[unlock] = true
      end
      has_effect = true
    end
    if not has_effect then content_error("Research '" .. id .. "' must grant a modifier or unlock") end
  end
  local visiting, visited = {}, {}
  local function visit(id)
    if visiting[id] then content_error("Research prerequisites contain a cycle at '" .. id .. "'") end
    if visited[id] then return end
    visiting[id] = true
    for _, prerequisite in ipairs(self.research[id].prerequisites or {}) do visit(prerequisite) end
    visiting[id], visited[id] = nil, true
  end
  for _, id in ipairs(sorted_keys(self.research)) do visit(id) end

  for _, id in ipairs(sorted_keys(self.discoveries)) do
    local discovery = self.discoveries[id]
    require_string(discovery.display_name, "Discovery '" .. id .. "' display_name")
    require_string(discovery.description, "Discovery '" .. id .. "' description")
    if type(discovery.allowed_biome_ids) ~= "table" or #discovery.allowed_biome_ids == 0 then
      content_error("Discovery '" .. id .. "' allowed_biome_ids must be a non-empty list")
    end
    local biome_ids = {}
    for _, biome_id in ipairs(discovery.allowed_biome_ids) do
      if type(biome_id) ~= "string" or not biome_id:match("^biome%.[a-z0-9_%.]+$") or biome_ids[biome_id] then
        content_error("Discovery '" .. id .. "' has invalid allowed biome '" .. tostring(biome_id) .. "'")
      end
      biome_ids[biome_id] = true
    end
    require_string(discovery.access_profile_id, "Discovery '" .. id .. "' access_profile_id")
    if not KNOWN_DISCOVERY_ACCESS_PROFILES[discovery.access_profile_id] then
      content_error("Discovery '" .. id .. "' has unknown access_profile_id '" .. discovery.access_profile_id .. "'")
    end
    require_positive_integer(discovery.first_data_reward, "Discovery '" .. id .. "' first_data_reward")
    require_nonnegative_number(discovery.repeat_scrap_reward, "Discovery '" .. id .. "' repeat_scrap_reward")
    if discovery.repeat_scrap_reward % 1 ~= 0 then
      content_error("Discovery '" .. id .. "' repeat_scrap_reward must be an integer")
    end
    if type(discovery.presentation) ~= "table" then
      content_error("Discovery '" .. id .. "' presentation must be a table")
    end
    require_string(discovery.presentation.clue, "Discovery '" .. id .. "' presentation.clue")
    require_string(discovery.presentation.render_style, "Discovery '" .. id .. "' presentation.render_style")
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
    validate_inventory_shape(component.inventory, "Component '" .. id .. "' inventory")
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
    if ability.loadout_kind ~= nil and ability.loadout_kind ~= "weapon" and ability.loadout_kind ~= "ability" then
      content_error("Ability '" .. id .. "' loadout_kind must be 'weapon' or 'ability'")
    end
    if ability.attack_tags ~= nil then
      if type(ability.attack_tags) ~= "table" or #ability.attack_tags == 0 then
        content_error("Ability '" .. id .. "' attack_tags must be a non-empty list")
      end
      local seen_tags = {}
      for _, tag in ipairs(ability.attack_tags) do
        if not KNOWN_ATTACK_TAGS[tag] or seen_tags[tag] then
          content_error("Ability '" .. id .. "' has invalid attack tag '" .. tostring(tag) .. "'")
        end
        seen_tags[tag] = true
      end
    end
    if ability.resource ~= nil then
      if type(ability.resource) ~= "table" then
        content_error("Ability '" .. id .. "' resource must be a table")
      end
      require_string(ability.resource.name, "Ability '" .. id .. "' resource.name")
      require_positive_integer(ability.resource.amount, "Ability '" .. id .. "' resource.amount")
    end
    for _, field in ipairs({ "range", "radius", "delay", "max_cells", "damage", "force", "pellets", "pierce" }) do
      if ability[field] ~= nil then
        require_positive_integer(ability[field], "Ability '" .. id .. "' " .. field)
      end
    end
    if ability.implementation == "electrical_discharge" then
      require_positive_integer(ability.max_cells, "Ability '" .. id .. "' max_cells")
      require_positive_integer(ability.damage, "Ability '" .. id .. "' damage")
    end
    if ability.implementation == "melee" then
      require_positive_integer(ability.damage, "Ability '" .. id .. "' damage")
      require_positive_integer(ability.force, "Ability '" .. id .. "' force")
      if ability.activation_type ~= "body" then
        content_error("Melee ability '" .. id .. "' activation_type must be 'body'")
      end
    end
    if ability.implementation == "scattershot" then
      require_positive_integer(ability.range, "Ability '" .. id .. "' range")
      require_positive_integer(ability.damage, "Ability '" .. id .. "' damage")
      require_positive_integer(ability.pellets, "Ability '" .. id .. "' pellets")
    end
    if ability.implementation == "piercing_projectile" then
      require_positive_integer(ability.range, "Ability '" .. id .. "' range")
      require_positive_integer(ability.damage, "Ability '" .. id .. "' damage")
      require_positive_integer(ability.pierce, "Ability '" .. id .. "' pierce")
    end
    local ranged = ability.implementation == "projectile" or ability.implementation == "scattershot"
      or ability.implementation == "piercing_projectile"
    if ranged and ability.loadout_kind == "weapon" then
      if type(ability.ammo) ~= "table" then
        content_error("Ranged weapon ability '" .. id .. "' must declare ammo")
      end
      require_string(ability.ammo.family, "Ability '" .. id .. "' ammo.family")
      self:get_resource(ability.ammo.family)
      require_positive_integer(ability.ammo.magazine_capacity, "Ability '" .. id .. "' ammo.magazine_capacity")
      require_positive_integer(ability.ammo.ammo_per_attack, "Ability '" .. id .. "' ammo.ammo_per_attack")
      if ability.ammo.magazine_key ~= nil then require_string(ability.ammo.magazine_key, "Ability '" .. id .. "' ammo.magazine_key") end
    elseif ability.ammo ~= nil and not ranged then
      content_error("Non-ranged ability '" .. id .. "' cannot declare weapon ammo")
    end
  end

  self:_validate_actor_collection("Actor", self.actors)
  self:_validate_actor_collection("Enemy", self.enemies)
  self:_validate_actor_collection("Boss", self.bosses)
  for _, id in ipairs(sorted_keys(self.reinforcement_profiles)) do
    local profile = self.reinforcement_profiles[id]
    require_string(profile.display_name, "Reinforcement profile '" .. id .. "' display_name")
    require_string(profile.source_type, "Reinforcement profile '" .. id .. "' source_type")
    if not KNOWN_REINFORCEMENT_SOURCE_TYPES[profile.source_type] then
      content_error("Reinforcement profile '" .. id .. "' has unknown source type")
    end
    require_string(profile.faction_id, "Reinforcement profile '" .. id .. "' faction_id")
    self:get_faction(profile.faction_id)
    require_positive_integer(profile.wave_size, "Reinforcement profile '" .. id .. "' wave_size")
    if profile.wave_size > 2 then content_error("Reinforcement profile '" .. id .. "' wave_size must remain bounded at two") end
    for _, field in ipairs({ "allowed_biome_ids", "allowed_tier_ids", "entries" }) do
      if type(profile[field]) ~= "table" or #profile[field] == 0 then
        content_error("Reinforcement profile '" .. id .. "' " .. field .. " must be a non-empty list")
      end
    end
    local biomes, tiers, enemies = {}, {}, {}
    for _, biome_id in ipairs(profile.allowed_biome_ids) do
      if type(biome_id) ~= "string" or not biome_id:match("^biome%.[a-z0-9_%.]+$") or biomes[biome_id] then
        content_error("Reinforcement profile '" .. id .. "' has invalid biome")
      end
      biomes[biome_id] = true
    end
    for _, tier_id in ipairs(profile.allowed_tier_ids) do
      if type(tier_id) ~= "string" or not tier_id:match("^tier%.[a-z0-9_%.]+$") or tiers[tier_id] then
        content_error("Reinforcement profile '" .. id .. "' has invalid tier")
      end
      tiers[tier_id] = true
    end
    for index, entry in ipairs(profile.entries) do
      if type(entry) ~= "table" then content_error("Reinforcement profile '" .. id .. "' entry " .. index .. " must be a table") end
      require_string(entry.enemy_id, "Reinforcement profile '" .. id .. "' entry enemy_id")
      local enemy = self:get_enemy(entry.enemy_id)
      if enemy.elite then content_error("Reinforcement profile '" .. id .. "' cannot deploy elite '" .. enemy.id .. "'") end
      if enemy.faction_id ~= profile.faction_id then
        content_error("Reinforcement profile '" .. id .. "' enemy faction does not match profile")
      end
      if enemies[enemy.id] then content_error("Reinforcement profile '" .. id .. "' repeats enemy '" .. enemy.id .. "'") end
      enemies[enemy.id] = true
      require_positive_integer(entry.weight, "Reinforcement profile '" .. id .. "' entry weight")
    end
  end
  for _, id in ipairs(sorted_keys(self.boss_arenas)) do
    local arena = self.boss_arenas[id]
    require_string(arena.display_name, "Boss arena '" .. id .. "' display_name")
    require_string(arena.terrain, "Boss arena '" .. id .. "' terrain")
    for _, field in ipairs({ "player_spawn", "boss_spawn" }) do
      local point = arena[field]
      if type(point) ~= "table" or type(point.x) ~= "number" or point.x % 1 ~= 0
        or type(point.y) ~= "number" or point.y % 1 ~= 0 then
        content_error("Boss arena '" .. id .. "' " .. field .. " must be an integer coordinate")
      end
    end
    for _, placement in ipairs(arena.cover or {}) do
      if type(placement) ~= "table" then content_error("Boss arena '" .. id .. "' cover entry must be a table") end
      self:get_world_object(placement.definition_id)
      if type(placement.x) ~= "number" or type(placement.y) ~= "number" then content_error("Boss arena '" .. id .. "' cover placement is invalid") end
    end
    for _, placement in ipairs(arena.hazards or {}) do
      if type(placement) ~= "table" then content_error("Boss arena '" .. id .. "' hazard entry must be a table") end
      self:get_hazard(placement.definition_id)
      if type(placement.x) ~= "number" or type(placement.y) ~= "number" then content_error("Boss arena '" .. id .. "' hazard placement is invalid") end
    end
    for _, placement in ipairs(arena.liquid or {}) do
      if type(placement) ~= "table" then content_error("Boss arena '" .. id .. "' liquid entry must be a table") end
      local liquid = self:get_liquid(placement.liquid_id)
      require_positive_integer(placement.amount, "Boss arena '" .. id .. "' liquid amount")
      if placement.amount > liquid.max_depth then content_error("Boss arena '" .. id .. "' liquid amount exceeds max depth") end
      if type(placement.x) ~= "number" or type(placement.y) ~= "number" then content_error("Boss arena '" .. id .. "' liquid placement is invalid") end
    end
    for _, placement in ipairs(arena.terrain_cells or {}) do
      if type(placement) ~= "table" then content_error("Boss arena '" .. id .. "' terrain cell must be a table") end
      self:get_material(placement.material_id)
      if type(placement.x) ~= "number" or placement.x % 1 ~= 0 or type(placement.y) ~= "number" or placement.y % 1 ~= 0 then
        content_error("Boss arena '" .. id .. "' terrain cell coordinate is invalid")
      end
    end
    for _, placement in ipairs(arena.gas or {}) do
      if type(placement) ~= "table" then content_error("Boss arena '" .. id .. "' gas entry must be a table") end
      local gas = self:get_gas(placement.gas_id)
      require_positive_integer(placement.concentration, "Boss arena '" .. id .. "' gas concentration")
      if placement.concentration > gas.max_concentration then content_error("Boss arena '" .. id .. "' gas concentration exceeds max") end
      if type(placement.x) ~= "number" or placement.x % 1 ~= 0 or type(placement.y) ~= "number" or placement.y % 1 ~= 0 then
        content_error("Boss arena '" .. id .. "' gas placement is invalid")
      end
    end
    local circuits = {}
    for _, circuit in ipairs(arena.circuits or {}) do
      if type(circuit) ~= "table" or type(circuit.id) ~= "string" or not circuit.id:match("^power%.circuit%.[a-z0-9_%.]+$") then
        content_error("Boss arena '" .. id .. "' circuit ID is invalid")
      end
      if circuits[circuit.id] then content_error("Boss arena '" .. id .. "' repeats circuit '" .. circuit.id .. "'") end
      if circuit.enabled ~= nil and type(circuit.enabled) ~= "boolean" then content_error("Boss arena '" .. id .. "' circuit enabled state is invalid") end
      circuits[circuit.id] = true
    end
    for _, device in ipairs(arena.devices or {}) do
      if type(device) ~= "table" then content_error("Boss arena '" .. id .. "' device entry must be a table") end
      local object = self:get_world_object(device.definition_id)
      if type(device.x) ~= "number" or device.x % 1 ~= 0 or type(device.y) ~= "number" or device.y % 1 ~= 0 then
        content_error("Boss arena '" .. id .. "' device coordinate is invalid")
      end
      if object.interaction_role == "door" or object.interaction_role == "generator" or object.interaction_role == "breaker" then
        if not circuits[device.circuit_id] then content_error("Boss arena '" .. id .. "' device references an unknown circuit") end
      end
      if device.door_state ~= nil and device.door_state ~= "open" and device.door_state ~= "closed" then
        content_error("Boss arena '" .. id .. "' device door state is invalid")
      end
      if device.generator_online ~= nil and type(device.generator_online) ~= "boolean" then
        content_error("Boss arena '" .. id .. "' device generator state is invalid")
      end
    end
    for _, fire in ipairs(arena.fires or {}) do
      if type(fire) ~= "table" or fire.target_kind ~= "object"
        or type(fire.x) ~= "number" or fire.x % 1 ~= 0 or type(fire.y) ~= "number" or fire.y % 1 ~= 0 then
        content_error("Boss arena '" .. id .. "' fire entry is invalid")
      end
    end
  end
  for _, id in ipairs(sorted_keys(self.bosses)) do
    local boss = self.bosses[id]
    require_positive_integer(boss.health, "Boss '" .. id .. "' health")
    self:get_boss_arena(boss.arena_profile_id)
    if boss.world_site_family ~= nil and boss.world_site_family ~= "ruin"
      and boss.world_site_family ~= "reactor" and boss.world_site_family ~= "cave" then
      content_error("Boss '" .. id .. "' world_site_family is invalid")
    end
    if type(boss.ai_profile) ~= "table" or type(boss.ai_profile.preferred_range) ~= "number"
      or boss.ai_profile.preferred_range <= 0 then
      content_error("Boss '" .. id .. "' ai_profile must define a positive preferred_range")
    end
    if type(boss.ai_profile.telegraph_ability_ids) ~= "table" or #boss.ai_profile.telegraph_ability_ids == 0 then
      content_error("Boss '" .. id .. "' must declare telegraph_ability_ids")
    end
    local capabilities = {}
    for _, installation in ipairs(boss.installed_components) do
      for _, ability_id in ipairs(self:get_component(installation.component_id).abilities) do capabilities[ability_id] = true end
    end
    local seen = {}
    for _, ability_id in ipairs(boss.ai_profile.telegraph_ability_ids) do
      if seen[ability_id] or not capabilities[ability_id] then
        content_error("Boss '" .. id .. "' telegraph ability has no physical provider '" .. tostring(ability_id) .. "'")
      end
      seen[ability_id] = true
      self:get_ability(ability_id)
    end
  end
  self.encounter_pools_by_pair = {}
  for _, id in ipairs(sorted_keys(self.encounter_pools)) do
    local pool = self.encounter_pools[id]
    require_string(pool.biome_id, "Encounter pool '" .. id .. "' biome_id")
    require_string(pool.tier_id, "Encounter pool '" .. id .. "' tier_id")
    if not pool.biome_id:match("^biome%.[a-z0-9_%.]+$") or not pool.tier_id:match("^tier%.[a-z0-9_%.]+$") then
      content_error("Encounter pool '" .. id .. "' has invalid biome or tier semantic ID")
    end
    if type(pool.entries) ~= "table" or #pool.entries == 0 then
      content_error("Encounter pool '" .. id .. "' entries must be a non-empty list")
    end
    local seen_enemy, total_weight = {}, 0
    for index, entry in ipairs(pool.entries) do
      if type(entry) ~= "table" then content_error("Encounter pool '" .. id .. "' entry " .. index .. " must be a table") end
      require_string(entry.enemy_id, "Encounter pool '" .. id .. "' entry enemy_id")
      self:get_enemy(entry.enemy_id)
      if seen_enemy[entry.enemy_id] then
        content_error("Encounter pool '" .. id .. "' repeats enemy '" .. entry.enemy_id .. "'")
      end
      seen_enemy[entry.enemy_id] = true
      require_positive_integer(entry.weight, "Encounter pool '" .. id .. "' entry weight")
      total_weight = total_weight + entry.weight
    end
    assert(total_weight > 0)
    local key = pool.biome_id .. ":" .. pool.tier_id
    if self.encounter_pools_by_pair[key] then
      content_error("Encounter pools duplicate biome/tier pair '" .. key .. "'")
    end
    self.encounter_pools_by_pair[key] = pool
  end
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
    local capabilities = {}
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
      for _, ability_id in ipairs(component.abilities or {}) do capabilities[ability_id] = true end
    end
    if kind == "Enemy" or kind == "Boss" then
      require_string(definition.kind, kind .. " '" .. id .. "' kind")
      if not definition.kind:match("^[a-z0-9_]+$") then
        content_error(kind .. " '" .. id .. "' kind must be a simple render identity")
      end
      if kind == "Enemy" and type(definition.elite) ~= "boolean" then
        content_error(kind .. " '" .. id .. "' elite must be a boolean")
      end
      if kind == "Enemy" then
        require_string(definition.faction_id, "Enemy '" .. id .. "' faction_id")
        self:get_faction(definition.faction_id)
        if definition.ai_role ~= nil and not KNOWN_AI_ROLES[definition.ai_role] then
          content_error("Enemy '" .. id .. "' has unknown ai_role '" .. tostring(definition.ai_role) .. "'")
        end
      end
      require_nonnegative_number(definition.ammo, kind .. " '" .. id .. "' ammo")
      if definition.ammo % 1 ~= 0 then
        content_error(kind .. " '" .. id .. "' ammo must be an integer")
      end
      if definition.requires_locomotion ~= false and not capabilities["ability.locomotion.move"] then
        content_error(kind .. " '" .. id .. "' must have a locomotion provider")
      end
    end
  end
end

function Registry:validate_reinforcement_profiles(route_definitions)
  assert(route_definitions, "Reinforcement-profile validation requires route definitions")
  for _, id in ipairs(sorted_keys(self.reinforcement_profiles)) do
    local profile = self.reinforcement_profiles[id]
    local usable = false
    for _, biome_id in ipairs(profile.allowed_biome_ids) do
      route_definitions:get_biome(biome_id)
      for _, tier_id in ipairs(profile.allowed_tier_ids) do
        route_definitions:get_tier(tier_id)
        if route_definitions:biome_supports_tier(biome_id, tier_id) then usable = true end
      end
    end
    if not usable then content_error("Reinforcement profile '" .. id .. "' has no supported biome/tier pair") end
  end
  return true
end

-- Route content owns the valid biome/tier vocabulary, while this registry
-- owns the enemy definitions.  Keep the cross-content validation explicit so
-- either source remains headless and independently useful.
function Registry:validate_encounter_pools(route_definitions)
  assert(route_definitions, "Encounter-pool validation requires route definitions")
  for _, biome_id in ipairs(route_definitions.biome_order or {}) do
    for _, tier_id in ipairs(route_definitions.tier_order or {}) do
      if route_definitions:biome_supports_tier(biome_id, tier_id) and not self:encounter_pool_for(biome_id, tier_id) then
        content_error("Missing encounter pool for '" .. biome_id .. ":" .. tier_id .. "'")
      end
    end
  end
  for _, pool in pairs(self.encounter_pools) do
    route_definitions:get_biome(pool.biome_id)
    route_definitions:get_tier(pool.tier_id)
    if not route_definitions:biome_supports_tier(pool.biome_id, pool.tier_id) then
      content_error("Encounter pool uses unsupported biome/tier pair '" .. pool.biome_id .. ":" .. pool.tier_id .. "'")
    end
  end
  return true
end

-- Discoveries are account-facing content but their biome vocabulary belongs
-- to the route registry. Keep this join explicit, as with encounter pools.
function Registry:validate_discoveries(route_definitions)
  assert(route_definitions, "Discovery validation requires route definitions")
  for _, discovery_id in ipairs(sorted_keys(self.discoveries)) do
    local discovery = self.discoveries[discovery_id]
    local usable = false
    for _, biome_id in ipairs(discovery.allowed_biome_ids) do
      route_definitions:get_biome(biome_id)
      usable = true
    end
    if not usable then
      content_error("Discovery '" .. discovery_id .. "' is production content but has no usable biome")
    end
  end
  return true
end

return Registry
