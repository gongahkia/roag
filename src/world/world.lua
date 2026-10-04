-- Authoritative terrain and environmental-object state for one floor. The
-- generator's boolean layout is construction input only; physical state lives
-- here and is queried by every simulation consumer.
local Grid = require("src.world.grid")
local Inventory = require("src.inventory.inventory")
local PhysicalItem = require("src.inventory.physical_item")
local Tool = require("src.inventory.tool")

local World = {}
World.__index = World

local AIR = "material.terrain.air"
local SOLID_MATERIAL_BY_TERRAIN = {
  forest = "material.terrain.brush",
  cave = "material.terrain.stone",
  dungeon = "material.structure.masonry",
  arena = "material.structure.reinforced",
}
local OPEN_MATERIAL_BY_TERRAIN = {
  forest = "material.terrain.leaf_litter",
}
local DOOR_STATES = { open = true, closed = true, destroyed = true }
local CIRCUIT_ROLES = { door = true, generator = true, breaker = true }
local REINFORCEMENT_STATES = { idle = true, armed = true, spent = true, cancelled = true }

local function key(x, y)
  return Grid.key(x, y)
end

local function copy_object(object)
  local result = {}
  for name, value in pairs(object) do
    result[name] = value
  end
  return result
end

local function copy_yields(values)
  local result = {}
  for _, value in ipairs(values or {}) do
    result[#result + 1] = { resource_id = value.resource_id, amount = value.amount }
  end
  return result
end

local function requires_circuit(definition)
  return definition.interaction_role == "generator" or definition.interaction_role == "breaker"
    or (definition.interaction_role == "door" and definition.power_required == true)
end

local function construction_fields(definition, options)
  local constructed = options.constructed == true
  if not constructed then
    assert(options.construction_recipe_id == nil and options.construction_campaign_id == nil
      and options.construction_recovery_yields == nil, "Construction provenance requires a constructed world object")
    return { constructed = false, construction_recipe_id = nil, construction_campaign_id = nil, construction_recovery_yields = {} }
  end
  assert(type(options.construction_recipe_id) == "string" and options.construction_recipe_id ~= "",
    "Constructed world object requires a recipe ID")
  assert(type(options.construction_campaign_id) == "string" and options.construction_campaign_id ~= "",
    "Constructed world object requires a campaign ID")
  local yields = copy_yields(options.construction_recovery_yields)
  for _, yield in ipairs(yields) do
    assert(type(yield.resource_id) == "string" and definition and yield.resource_id:match("^resource%.[a-z0-9_%.]+$"),
      "Construction recovery resource ID is invalid")
    assert(type(yield.amount) == "number" and yield.amount >= 1 and yield.amount % 1 == 0,
      "Construction recovery amount is invalid")
  end
  return {
    constructed = true,
    construction_recipe_id = options.construction_recipe_id,
    construction_campaign_id = options.construction_campaign_id,
    construction_recovery_yields = yields,
  }
end

local function object_blocks_for_state(definition, door_state)
  if definition.interaction_role == "door" and door_state == "open" then
    return false, false, false, false
  end
  return definition.blocks_movement, definition.blocks_vision, definition.blocks_projectiles, definition.blocks_gas
end

-- Discovery state remains ordinary world-object state. The cache, clue, and
-- any gate all carry the same semantic ID so inspection and save restoration
-- never need a second object graph.
local function discovery_fields(registry, definition, source)
  source = source or {}
  local discovery_id = source.discovery_id
  local access_profile_id = source.discovery_access_profile_id
  local provenance = source.discovery_provenance
  local claimed = source.discovery_claimed
  local requires_discovery = definition.interaction_role == "discovery" or definition.interaction_role == "clue"
  if requires_discovery and type(discovery_id) ~= "string" then
    return nil, "Discovery object requires a discovery ID"
  end
  if discovery_id ~= nil then
    if type(discovery_id) ~= "string" or not registry.discoveries[discovery_id] then
      return nil, "World object references an unknown discovery"
    end
    local discovery = registry:get_discovery(discovery_id)
    if type(access_profile_id) ~= "string" or access_profile_id ~= discovery.access_profile_id then
      return nil, "World object discovery access profile is invalid"
    end
  elseif access_profile_id ~= nil or provenance ~= nil or claimed ~= nil then
    return nil, "World object discovery metadata requires a discovery ID"
  end
  if provenance ~= nil and type(provenance) ~= "string" then
    return nil, "Discovery provenance must be a string"
  end
  if definition.interaction_role == "discovery" then
    if claimed == nil then claimed = false end
    if type(claimed) ~= "boolean" then return nil, "Discovery claim state must be boolean" end
  elseif claimed ~= nil then
    return nil, "Only discovery caches may store claim state"
  end
  return {
    discovery_id = discovery_id,
    discovery_access_profile_id = access_profile_id,
    discovery_provenance = provenance,
    discovery_claimed = claimed,
  }
end

-- Reinforcement origins are ordinary physical world objects.  Their bounded
-- deployment state lives beside doors, discovery caches, and service stock so
-- active saves require no parallel ecology object graph.
local function reinforcement_fields(registry, definition, source)
  source = source or {}
  local profile_id = source.reinforcement_profile_id
  local faction_id = source.reinforcement_faction_id
  local charges = source.reinforcement_charges
  local state = source.reinforcement_state
  local delay = source.reinforcement_delay
  local just_armed = source.reinforcement_just_armed
  local wave = source.reinforcement_wave_enemy_ids
  local provenance = source.reinforcement_provenance
  local requires_reinforcement = definition.interaction_role == "reinforcement"
  if not requires_reinforcement then
    if profile_id ~= nil or faction_id ~= nil or charges ~= nil or state ~= nil or delay ~= nil or just_armed ~= nil or wave ~= nil or provenance ~= nil then
      return nil, "World object reinforcement metadata requires a reinforcement source"
    end
    return {
      reinforcement_profile_id = nil, reinforcement_faction_id = nil, reinforcement_charges = nil,
      reinforcement_state = nil, reinforcement_delay = nil, reinforcement_just_armed = nil, reinforcement_wave_enemy_ids = nil,
      reinforcement_provenance = nil,
    }
  end
  if type(profile_id) ~= "string" or not registry.reinforcement_profiles[profile_id] then
    return nil, "Reinforcement source requires a valid profile"
  end
  local profile = registry:get_reinforcement_profile(profile_id)
  if profile.source_type ~= definition.reinforcement_source_type then
    return nil, "Reinforcement source profile does not match source type"
  end
  if faction_id ~= profile.faction_id or not registry.factions[faction_id] then
    return nil, "Reinforcement source faction does not match profile"
  end
  if type(charges) ~= "number" or charges % 1 ~= 0 or charges < 0 or charges > 1 then
    return nil, "Reinforcement source charges must be zero or one"
  end
  if type(state) ~= "string" or not REINFORCEMENT_STATES[state] then
    return nil, "Reinforcement source state is invalid"
  end
  if (state == "idle" or state == "armed") and charges ~= 1 then
    return nil, "Active reinforcement source must retain one charge"
  end
  if (state == "spent" or state == "cancelled") and charges ~= 0 then
    return nil, "Spent reinforcement source cannot retain a charge"
  end
  if state == "armed" then
    if type(delay) ~= "number" or delay % 1 ~= 0 or delay < 1 then
      return nil, "Armed reinforcement source requires a positive delay"
    end
  elseif delay ~= nil then
    return nil, "Only armed reinforcement source may retain a delay"
  end
  if just_armed ~= nil and type(just_armed) ~= "boolean" then
    return nil, "Reinforcement source arm state is invalid"
  end
  if just_armed == true and state ~= "armed" then
    return nil, "Only armed reinforcement source may defer its countdown"
  end
  if type(wave) ~= "table" or #wave ~= profile.wave_size then
    return nil, "Reinforcement source wave does not match its profile"
  end
  local copied_wave = {}
  for index, enemy_id in ipairs(wave) do
    if type(enemy_id) ~= "string" then return nil, "Reinforcement wave entry is invalid" end
    local enemy = registry:get_enemy(enemy_id)
    if enemy.elite or enemy.faction_id ~= faction_id then
      return nil, "Reinforcement wave contains an invalid enemy"
    end
    copied_wave[index] = enemy_id
  end
  if type(provenance) ~= "string" then return nil, "Reinforcement provenance must be a string" end
  return {
    reinforcement_profile_id = profile_id,
    reinforcement_faction_id = faction_id,
    reinforcement_charges = charges,
    reinforcement_state = state,
    reinforcement_delay = delay,
    reinforcement_just_armed = just_armed == true,
    reinforcement_wave_enemy_ids = copied_wave,
    reinforcement_provenance = provenance,
  }
end

function World.new(registry, terrain, open_layout, sequence_owner, material_layout)
  sequence_owner = sequence_owner or { next_world_object_sequence = 1, next_hazard_sequence = 1, next_fire_sequence = 1, next_item_sequence = 1 }
  sequence_owner.next_world_object_sequence = sequence_owner.next_world_object_sequence or 1
  sequence_owner.next_hazard_sequence = sequence_owner.next_hazard_sequence or 1
  sequence_owner.next_fire_sequence = sequence_owner.next_fire_sequence or 1
  sequence_owner.next_item_sequence = sequence_owner.next_item_sequence or 1
  local self = setmetatable({
    registry = registry,
    terrain = terrain,
    cells = {},
    objects = {},
    object_order = {},
    objects_by_cell = {},
    hazards = {},
    hazard_order = {},
    hazards_by_cell = {},
    liquids = {},
    liquid_tick = 0,
    -- Gas is a coordinate-owned medium like liquid, but remains independent
    -- from terrain, hazards, objects, fire, and liquid state.
    gases = {},
    circuits = {},
    circuit_order = {},
    fires = {},
    fire_order = {},
    fires_by_target = {},
    ground_items = {},
    ground_item_order = {},
    ground_items_by_cell = {},
    fire_tick = 0,
    fire_ticking = false,
    sequence_owner = sequence_owner,
  }, World)
  local solid_material_id = SOLID_MATERIAL_BY_TERRAIN[terrain] or "material.terrain.stone"
  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do
      local material_id = (material_layout and material_layout[key(x, y)])
        or (open_layout[key(x, y)] and (OPEN_MATERIAL_BY_TERRAIN[terrain] or AIR) or solid_material_id)
      local material = registry:get_material(material_id)
      self.cells[key(x, y)] = {
        material_id = material_id,
        current_integrity = material.destructible and material.max_integrity or nil,
        destroyed = false,
      }
    end
  end

  return self
end

function World:get_cell(x, y)
  if not Grid.in_bounds(x, y) then
    return nil
  end
  return self.cells[key(x, y)]
end

function World:get_material(x, y)
  local cell = self:get_cell(x, y)
  return cell and self.registry:get_material(cell.material_id) or nil
end

function World:terrain_is_passable(x, y)
  local material = self:get_material(x, y)
  return material and not material.blocks_movement or false
end

function World:get_object(id)
  return self.objects[id]
end

-- Persistent circuits are deliberately logical floor infrastructure, not a
-- second electrical-fluid simulation. Power is derived from enabled circuit
-- state and online physical generator objects each time it is queried.
function World:register_circuit(circuit_id, options)
  options = options or {}
  if type(circuit_id) ~= "string" or circuit_id == "" then
    return { applied = false, code = "invalid_circuit", reason = "Circuit ID must be a non-empty string" }
  end
  if self.circuits[circuit_id] then
    return { applied = false, code = "duplicate_circuit", reason = "Circuit ID already exists" }
  end
  local enabled = options.enabled
  if enabled == nil then
    enabled = true
  end
  if type(enabled) ~= "boolean" then
    return { applied = false, code = "invalid_enabled", reason = "Circuit enabled state must be boolean" }
  end
  local circuit = { id = circuit_id, enabled = enabled }
  self.circuits[circuit_id] = circuit
  self.circuit_order[#self.circuit_order + 1] = circuit_id
  return { applied = true, circuit = circuit, circuit_id = circuit_id }
end

function World:get_circuit(circuit_id)
  return self.circuits[circuit_id]
end

function World:remove_empty_circuit(circuit_id)
  local circuit = self.circuits[circuit_id]
  if not circuit then return { applied = false, code = "unknown_circuit", reason = "Unknown circuit" } end
  for _, object in ipairs(self:list_objects(true)) do
    if object.circuit_id == circuit_id then
      return { applied = false, code = "circuit_in_use", reason = "Circuit is in use" }
    end
  end
  self.circuits[circuit_id] = nil
  for index, id in ipairs(self.circuit_order) do
    if id == circuit_id then table.remove(self.circuit_order, index); break end
  end
  return { applied = true, code = "removed", circuit_id = circuit_id }
end

function World:list_circuits()
  local result = {}
  for _, circuit_id in ipairs(self.circuit_order) do
    result[#result + 1] = self.circuits[circuit_id]
  end
  return result
end

function World:circuit_sources(circuit_id, online_only)
  local sources = {}
  for _, object in ipairs(self:list_objects(true)) do
    if object.interaction_role == "generator" and object.circuit_id == circuit_id
      and (not online_only or (not object.destroyed and object.generator_online)) then
      sources[#sources + 1] = object
    end
  end
  return sources
end

function World:circuit_consumers(circuit_id)
  local consumers = {}
  for _, object in ipairs(self:list_objects()) do
    if object.interaction_role == "door" and object.circuit_id == circuit_id then
      consumers[#consumers + 1] = object
    end
  end
  return consumers
end

function World:is_circuit_powered(circuit_id)
  local circuit = self.circuits[circuit_id]
  return circuit and circuit.enabled and #self:circuit_sources(circuit_id, true) > 0 or false
end

function World:set_circuit_enabled(circuit_id, enabled)
  local circuit = self.circuits[circuit_id]
  if not circuit then
    return { applied = false, code = "unknown_circuit", reason = "Unknown circuit ID '" .. tostring(circuit_id) .. "'" }
  end
  if type(enabled) ~= "boolean" then
    return { applied = false, code = "invalid_enabled", reason = "Circuit enabled state must be boolean" }
  end
  local changed = circuit.enabled ~= enabled
  circuit.enabled = enabled
  return { applied = changed, code = changed and "set" or "unchanged", circuit_id = circuit_id, enabled = enabled,
    powered = self:is_circuit_powered(circuit_id) }
end

function World:inspect_circuit(circuit_id)
  local circuit = self.circuits[circuit_id]
  if not circuit then
    return nil, "Unknown circuit"
  end
  local sources, online_sources, consumers = self:circuit_sources(circuit_id), self:circuit_sources(circuit_id, true), self:circuit_consumers(circuit_id)
  local source_ids, online_source_ids, consumer_ids = {}, {}, {}
  for _, source in ipairs(sources) do
    source_ids[#source_ids + 1] = source.id
  end
  for _, source in ipairs(online_sources) do
    online_source_ids[#online_source_ids + 1] = source.id
  end
  for _, consumer in ipairs(consumers) do
    consumer_ids[#consumer_ids + 1] = consumer.id
  end
  return {
    id = circuit.id,
    enabled = circuit.enabled,
    powered = self:is_circuit_powered(circuit_id),
    source_ids = source_ids,
    online_source_ids = online_source_ids,
    consumer_ids = consumer_ids,
  }
end

function World:object_at(x, y)
  return self.objects_by_cell[key(x, y)]
end

function World:objects_at(x, y, include_destroyed)
  local result = {}
  local active = self:object_at(x, y)
  if active then
    result[#result + 1] = active
  end
  if include_destroyed then
    for _, id in ipairs(self.object_order) do
      local object = self.objects[id]
      if object.destroyed and object.x == x and object.y == y then
        result[#result + 1] = object
      end
    end
  end
  return result
end

function World:list_objects(include_destroyed)
  local result = {}
  for _, id in ipairs(self.object_order) do
    local object = self.objects[id]
    if include_destroyed or not object.destroyed then
      result[#result + 1] = object
    end
  end
  return result
end

function World:_next_item_id()
  if self.sequence_owner.allocate_item_id then
    return self.sequence_owner:allocate_item_id()
  end
  local sequence = self.sequence_owner.next_item_sequence
  self.sequence_owner.next_item_sequence = sequence + 1
  return string.format("item:%06d", sequence)
end

function World:ground_items_at(x, y)
  if not Grid.in_bounds(x, y) then return {} end
  local values = {}
  for _, item in ipairs(self.ground_items_by_cell[key(x, y)] or {}) do values[#values + 1] = item end
  table.sort(values, function(left, right) return left.id < right.id end)
  return values
end

function World:get_ground_item(id)
  return self.ground_items[id]
end

function World:list_ground_items()
  local values = {}
  for _, id in ipairs(self.ground_item_order) do values[#values + 1] = self.ground_items[id] end
  return values
end

function World:place_ground_item(item, x, y)
  if not Grid.in_bounds(x, y) then return nil, { applied = false, code = "out_of_bounds", reason = "Ground item is outside the world" } end
  if not self:terrain_is_passable(x, y) then return nil, { applied = false, code = "blocked_terrain", reason = "Ground item requires passable terrain" } end
  if type(item) ~= "table" or type(item.physical_id) ~= "string" or item.physical_id == "" or type(item.to_data) ~= "function" then
    return nil, { applied = false, code = "invalid_item", reason = "Ground item requires a physical inventory item" }
  end
  if self.ground_items[item.physical_id] then
    return nil, { applied = false, code = "duplicate_id", reason = "Ground item ID already exists" }
  end
  local ground = { id = item.physical_id, kind = "ground_item", item = item, x = x, y = y }
  self.ground_items[ground.id] = ground
  self.ground_item_order[#self.ground_item_order + 1] = ground.id
  local cell_items = self.ground_items_by_cell[key(x, y)] or {}
  cell_items[#cell_items + 1] = ground
  table.sort(cell_items, function(left, right) return left.id < right.id end)
  self.ground_items_by_cell[key(x, y)] = cell_items
  return ground, { applied = true, ground_item_id = ground.id }
end

function World:remove_ground_item(item_or_id)
  local ground = type(item_or_id) == "table" and item_or_id or self.ground_items[item_or_id]
  if not ground or self.ground_items[ground.id] ~= ground then
    return nil, { applied = false, code = "unknown_ground_item", reason = "Ground item is unavailable" }
  end
  self.ground_items[ground.id] = nil
  for index, id in ipairs(self.ground_item_order) do
    if id == ground.id then table.remove(self.ground_item_order, index); break end
  end
  local cell_items = self.ground_items_by_cell[key(ground.x, ground.y)] or {}
  for index, value in ipairs(cell_items) do
    if value == ground then table.remove(cell_items, index); break end
  end
  if #cell_items == 0 then self.ground_items_by_cell[key(ground.x, ground.y)] = nil end
  return ground.item, { applied = true, ground_item_id = ground.id }
end

function World:_drop_resource(resource_id, amount, x, y)
  local definition = self.registry:get_resource(resource_id)
  local dropped = {}
  while amount > 0 do
    local quantity = math.min(amount, definition.max_stack)
    local item = PhysicalItem.from_resource(resource_id, quantity, self:_next_item_id(), self.registry)
    local ground, result = self:place_ground_item(item, x, y)
    assert(ground, result.reason)
    dropped[#dropped + 1] = ground
    amount = amount - quantity
  end
  return dropped
end

function World:_drop_yields(yields, x, y)
  local dropped = {}
  for _, yield in ipairs(yields or {}) do
    for _, ground in ipairs(self:_drop_resource(yield.resource_id, yield.amount, x, y)) do dropped[#dropped + 1] = ground end
  end
  return dropped
end

-- Liquid is a coordinate-owned medium rather than a terrain material or an
-- object property. Iterating the fixed grid gives stable coordinate order
-- without depending on Lua table traversal.
function World:liquid_at(x, y)
  return Grid.in_bounds(x, y) and self.liquids[key(x, y)] or nil
end

function World:liquid_amount(x, y)
  local liquid = self:liquid_at(x, y)
  return liquid and liquid.amount or 0
end

function World:is_liquid_cell(x, y)
  return self:liquid_at(x, y) ~= nil
end

function World:list_liquids()
  local result = {}
  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do
      local liquid = self:liquid_at(x, y)
      if liquid then
        result[#result + 1] = liquid
      end
    end
  end
  return result
end

function World:set_liquid(x, y, liquid_id, amount)
  if not Grid.in_bounds(x, y) then
    return { applied = false, code = "out_of_bounds", reason = "Liquid position is outside the world" }
  end
  if type(amount) ~= "number" or amount < 0 or amount % 1 ~= 0 then
    return { applied = false, code = "invalid_amount", reason = "Liquid amount must be a non-negative integer" }
  end
  local location_key = key(x, y)
  local existing = self.liquids[location_key]
  if amount == 0 then
    if existing then
      self.liquids[location_key] = nil
      return { applied = true, code = "removed", x = x, y = y, liquid_id = existing.liquid_id, amount = 0 }
    end
    return { applied = false, code = "already_dry", x = x, y = y, amount = 0 }
  end
  local definition = self.registry.liquids[liquid_id]
  if not definition then
    return { applied = false, code = "unknown_liquid", reason = "Unknown liquid ID '" .. tostring(liquid_id) .. "'" }
  end
  if not self:terrain_is_passable(x, y) then
    return { applied = false, code = "blocked_terrain", reason = "Liquid requires passable terrain" }
  end
  if existing and existing.liquid_id ~= definition.id then
    return { applied = false, code = "different_liquid", reason = "Liquid mixing is not supported" }
  end
  if amount > definition.max_depth then
    return { applied = false, code = "capacity_exceeded", reason = "Liquid amount exceeds max depth" }
  end
  local changed = not existing or existing.amount ~= amount
  self.liquids[location_key] = {
    liquid_id = definition.id,
    amount = amount,
    x = x,
    y = y,
  }
  return { applied = changed, code = changed and "set" or "unchanged", x = x, y = y, liquid_id = definition.id, amount = amount }
end

function World:add_liquid(x, y, liquid_id, amount)
  if type(amount) ~= "number" or amount <= 0 or amount % 1 ~= 0 then
    return { applied = false, code = "invalid_amount", reason = "Liquid addition must be a positive integer" }
  end
  local existing = self:liquid_at(x, y)
  if existing and existing.liquid_id ~= liquid_id then
    return { applied = false, code = "different_liquid", reason = "Liquid mixing is not supported" }
  end
  return self:set_liquid(x, y, liquid_id, (existing and existing.amount or 0) + amount)
end

function World:remove_liquid(x, y, amount)
  if type(amount) ~= "number" or amount <= 0 or amount % 1 ~= 0 then
    return { applied = false, code = "invalid_amount", reason = "Liquid removal must be a positive integer" }
  end
  local existing = self:liquid_at(x, y)
  if not existing then
    return { applied = false, code = "dry", reason = "Liquid cell is dry" }
  end
  if amount > existing.amount then
    return { applied = false, code = "insufficient_liquid", reason = "Liquid cell does not contain that amount" }
  end
  return self:set_liquid(x, y, existing.liquid_id, existing.amount - amount)
end

function World:total_liquid_amount(liquid_id)
  local total = 0
  for _, liquid in ipairs(self:list_liquids()) do
    if liquid_id == nil or liquid.liquid_id == liquid_id then
      total = total + liquid.amount
    end
  end
  return total
end

function World:liquid_extinguishes_fire_at(x, y)
  local liquid = self:liquid_at(x, y)
  if not liquid then
    return false
  end
  return self.registry:get_liquid(liquid.liquid_id).extinguishes_fire
end

-- Conductivity is derived from the live physical layers at a coordinate.
-- No graph is cached: liquid flow and object movement/destruction therefore
-- alter a later transient discharge automatically.
function World:conductivity_at(x, y)
  if not Grid.in_bounds(x, y) then
    return { conductive = false, terrain = nil, liquid = nil, object = nil }
  end
  local material = self:get_material(x, y)
  local liquid = self:liquid_at(x, y)
  local object = self:object_at(x, y)
  local liquid_definition = liquid and self.registry:get_liquid(liquid.liquid_id) or nil
  local object_material = object and self.registry:get_material(object.material_id) or nil
  return {
    conductive = material.conductive or (liquid_definition and liquid_definition.conductive)
      or (object_material and object_material.conductive) or false,
    terrain = material.conductive and material.id or nil,
    liquid = liquid_definition and liquid_definition.conductive and liquid_definition.id or nil,
    object = object_material and object_material.conductive and object.id or nil,
  }
end

function World:is_conductive_at(x, y)
  return self:conductivity_at(x, y).conductive
end

-- Gas is finite, integer concentration state. Iterating the fixed grid keeps
-- inspection, serialization, and diffusion source order deterministic.
function World:gas_at(x, y)
  return Grid.in_bounds(x, y) and self.gases[key(x, y)] or nil
end

function World:gas_concentration(x, y)
  local gas = self:gas_at(x, y)
  return gas and gas.concentration or 0
end

function World:is_gas_cell(x, y)
  return self:gas_at(x, y) ~= nil
end

function World:is_harmful_gas_at(x, y)
  local gas = self:gas_at(x, y)
  if not gas then
    return false
  end
  local definition = self.registry:get_gas(gas.gas_id)
  return gas.concentration >= definition.exposure_threshold
end

function World:list_gases()
  local result = {}
  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do
      local gas = self:gas_at(x, y)
      if gas then
        result[#result + 1] = gas
      end
    end
  end
  return result
end

-- The object query is intentionally separate from ordinary movement. Current
-- cover is permeable, while a later closed bulkhead can set blocks_gas=true
-- without changing the gas solver.
function World:allows_gas_at(x, y)
  if not Grid.in_bounds(x, y) or not self:terrain_is_passable(x, y) then
    return false
  end
  local object = self:object_at(x, y)
  return not (object and object.blocks_gas)
end

function World:set_gas(x, y, gas_id, concentration)
  if not Grid.in_bounds(x, y) then
    return { applied = false, code = "out_of_bounds", reason = "Gas position is outside the world" }
  end
  if type(concentration) ~= "number" or concentration < 0 or concentration % 1 ~= 0 then
    return { applied = false, code = "invalid_concentration", reason = "Gas concentration must be a non-negative integer" }
  end
  local location_key = key(x, y)
  local existing = self.gases[location_key]
  if concentration == 0 then
    if existing then
      self.gases[location_key] = nil
      return { applied = true, code = "removed", x = x, y = y, gas_id = existing.gas_id, concentration = 0 }
    end
    return { applied = false, code = "already_clear", x = x, y = y, concentration = 0 }
  end
  local definition = self.registry.gases[gas_id]
  if not definition then
    return { applied = false, code = "unknown_gas", reason = "Unknown gas ID '" .. tostring(gas_id) .. "'" }
  end
  if not self:allows_gas_at(x, y) then
    return { applied = false, code = "blocked_terrain", reason = "Gas requires gas-accessible terrain" }
  end
  if existing and existing.gas_id ~= definition.id then
    return { applied = false, code = "different_gas", reason = "Gas mixing is not supported" }
  end
  if concentration > definition.max_concentration then
    return { applied = false, code = "capacity_exceeded", reason = "Gas concentration exceeds max concentration" }
  end
  local changed = not existing or existing.concentration ~= concentration
  self.gases[location_key] = {
    gas_id = definition.id,
    concentration = concentration,
    x = x,
    y = y,
  }
  return {
    applied = changed,
    code = changed and "set" or "unchanged",
    x = x,
    y = y,
    gas_id = definition.id,
    concentration = concentration,
  }
end

function World:add_gas(x, y, gas_id, concentration)
  if type(concentration) ~= "number" or concentration <= 0 or concentration % 1 ~= 0 then
    return { applied = false, code = "invalid_concentration", reason = "Gas addition must be a positive integer" }
  end
  local existing = self:gas_at(x, y)
  if existing and existing.gas_id ~= gas_id then
    return { applied = false, code = "different_gas", reason = "Gas mixing is not supported" }
  end
  return self:set_gas(x, y, gas_id, (existing and existing.concentration or 0) + concentration)
end

function World:remove_gas(x, y, concentration)
  if type(concentration) ~= "number" or concentration <= 0 or concentration % 1 ~= 0 then
    return { applied = false, code = "invalid_concentration", reason = "Gas removal must be a positive integer" }
  end
  local existing = self:gas_at(x, y)
  if not existing then
    return { applied = false, code = "clear", reason = "Gas cell is clear" }
  end
  if concentration > existing.concentration then
    return { applied = false, code = "insufficient_gas", reason = "Gas cell does not contain that concentration" }
  end
  return self:set_gas(x, y, existing.gas_id, existing.concentration - concentration)
end

function World:total_gas_amount(gas_id)
  local total = 0
  for _, gas in ipairs(self:list_gases()) do
    if gas_id == nil or gas.gas_id == gas_id then
      total = total + gas.concentration
    end
  end
  return total
end

function World:_next_fire_id()
  if self.sequence_owner.allocate_fire_id then
    return self.sequence_owner:allocate_fire_id()
  end
  local sequence = self.sequence_owner.next_fire_sequence
  self.sequence_owner.next_fire_sequence = sequence + 1
  return string.format("fire:%06d", sequence)
end

function World:_fire_target_key(target_kind, target_id, x, y)
  if target_kind == "terrain" then
    return "terrain:" .. key(x, y)
  elseif target_kind == "object" then
    return "object:" .. tostring(target_id)
  end
  return nil
end

function World:resolve_fire_target(target_or_fire)
  local target = target_or_fire
  if target_or_fire and target_or_fire.target_kind then
    target = {
      kind = target_or_fire.target_kind,
      target_id = target_or_fire.target_id,
      x = target_or_fire.x,
      y = target_or_fire.y,
    }
  end
  if type(target) ~= "table" then
    return nil, "invalid_target"
  end
  if target.kind == "terrain" then
    local cell = self:get_cell(target.x, target.y)
    if not cell then
      return nil, "no_target"
    end
    if cell.destroyed then
      return nil, "target_destroyed"
    end
    local material = self.registry:get_material(cell.material_id)
    if not material.flammable then
      return nil, "not_flammable"
    end
    return {
      target_kind = "terrain",
      target_key = self:_fire_target_key("terrain", nil, target.x, target.y),
      x = target.x,
      y = target.y,
      material = material,
    }
  elseif target.kind == "object" then
    local object = self.objects[target.target_id]
    if not object then
      return nil, "no_target"
    end
    if object.destroyed then
      return nil, "target_destroyed"
    end
    local material = self.registry:get_material(object.material_id)
    if not material.flammable then
      return nil, "not_flammable"
    end
    return {
      target_kind = "object",
      target_key = self:_fire_target_key("object", object.id),
      target_id = object.id,
      object = object,
      x = object.x,
      y = object.y,
      material = material,
    }
  end
  return nil, "invalid_target"
end

function World:get_fire(id)
  return self.fires[id]
end

function World:list_fires(include_inactive)
  local result = {}
  for _, id in ipairs(self.fire_order) do
    local fire = self.fires[id]
    if include_inactive or fire.active then
      result[#result + 1] = fire
    end
  end
  return result
end

function World:fire_position(fire_or_id)
  local fire = type(fire_or_id) == "table" and fire_or_id or self.fires[fire_or_id]
  if not fire or self.fires[fire.id] ~= fire then
    return nil
  end
  local target = self:resolve_fire_target(fire)
  if target then
    return target.x, target.y
  end
  return fire.x, fire.y
end

function World:fire_at_target(target_key)
  return self.fires_by_target[target_key]
end

function World:fires_at(x, y, include_inactive)
  local result = {}
  for _, fire in ipairs(self:list_fires(include_inactive)) do
    local fire_x, fire_y = self:fire_position(fire)
    if fire_x == x and fire_y == y then
      result[#result + 1] = fire
    end
  end
  return result
end

function World:create_fire(target, provenance)
  local resolved, code = self:resolve_fire_target(target)
  if not resolved then
    return nil, { applied = false, code = code, reason = "Fire target cannot ignite" }
  end
  if self:fire_at_target(resolved.target_key) then
    return nil, { applied = false, code = "already_burning", reason = "Physical target is already burning" }
  end
  if self:liquid_extinguishes_fire_at(resolved.x, resolved.y) then
    return nil, { applied = false, code = "suppressed_by_liquid", reason = "Extinguishing liquid covers this fuel" }
  end
  local id = self:_next_fire_id()
  local fire = {
    id = id,
    kind = "fire",
    target_kind = resolved.target_kind,
    target_id = resolved.target_id,
    target_key = resolved.target_key,
    x = resolved.x,
    y = resolved.y,
    age = 0,
    ready_tick = self.fire_tick + (self.fire_ticking and 1 or 2),
    active = true,
    provenance = copy_object(provenance or {}),
  }
  self.fires[id] = fire
  self.fire_order[#self.fire_order + 1] = id
  self.fires_by_target[fire.target_key] = fire
  return fire, { applied = true, fire_id = id }
end

function World:deactivate_fire(fire_or_id, reason)
  local fire = type(fire_or_id) == "table" and fire_or_id or self.fires[fire_or_id]
  if not fire or self.fires[fire.id] ~= fire then
    return { applied = false, code = "no_target", reason = "Unknown fire" }
  end
  if not fire.active then
    return { applied = false, code = "inactive", fire_id = fire.id }
  end
  fire.active = false
  fire.extinguished_reason = reason or "extinguished"
  if self.fires_by_target[fire.target_key] == fire then
    self.fires_by_target[fire.target_key] = nil
  end
  return { applied = true, fire_id = fire.id, code = "extinguished", reason = fire.extinguished_reason }
end

function World:_deactivate_target_fire(target_kind, target_id, x, y, reason)
  local target_key = self:_fire_target_key(target_kind, target_id, x, y)
  local fire = target_key and self:fire_at_target(target_key) or nil
  if fire then
    self:deactivate_fire(fire, reason)
  end
end

function World:begin_fire_tick()
  self.fire_tick = self.fire_tick + 1
  self.fire_ticking = true
  return self.fire_tick
end

function World:end_fire_tick()
  self.fire_ticking = false
end

function World:inspect_fire(fire_or_id)
  local fire = type(fire_or_id) == "table" and fire_or_id or self.fires[fire_or_id]
  if not fire or self.fires[fire.id] ~= fire then
    return nil, "Unknown fire"
  end
  local target = self:resolve_fire_target(fire)
  local x, y = self:fire_position(fire)
  return {
    id = fire.id,
    target_kind = fire.target_kind,
    target_id = fire.target_id,
    target_key = fire.target_key,
    x = x,
    y = y,
    material_id = target and target.material.id or nil,
    age = fire.age,
    ready_tick = fire.ready_tick,
    active = fire.active,
    extinguished_reason = fire.extinguished_reason,
    provenance = copy_object(fire.provenance),
  }
end

function World:_next_hazard_id()
  if self.sequence_owner.allocate_hazard_id then
    return self.sequence_owner:allocate_hazard_id()
  end
  local sequence = self.sequence_owner.next_hazard_sequence
  self.sequence_owner.next_hazard_sequence = sequence + 1
  return string.format("hazard:%06d", sequence)
end

function World:hazards_at(x, y, include_inactive)
  local result = {}
  for _, hazard in ipairs(self.hazards_by_cell[key(x, y)] or {}) do
    if include_inactive or hazard.active then
      result[#result + 1] = hazard
    end
  end
  return result
end

function World:is_hazardous(x, y)
  return #self:hazards_at(x, y) > 0
end

function World:list_hazards(include_inactive)
  local result = {}
  for _, id in ipairs(self.hazard_order) do
    local hazard = self.hazards[id]
    if include_inactive or hazard.active then
      result[#result + 1] = hazard
    end
  end
  return result
end

function World:place_hazard(definition_id, x, y, options)
  options = options or {}
  if not Grid.in_bounds(x, y) then
    return nil, { applied = false, code = "out_of_bounds", reason = "Hazard position is outside the world" }
  end
  if not self:terrain_is_passable(x, y) then
    return nil, { applied = false, code = "blocked_terrain", reason = "Hazard requires passable terrain" }
  end
  if self:object_at(x, y) then
    return nil, { applied = false, code = "occupied_object", reason = "Hazard cannot overlap a world object" }
  end
  if self:is_hazardous(x, y) then
    return nil, { applied = false, code = "occupied_hazard", reason = "Hazard tile already contains an active hazard" }
  end
  local definition = self.registry:get_hazard(definition_id)
  local id = options.id or self:_next_hazard_id()
  if self.hazards[id] then
    return nil, { applied = false, code = "duplicate_id", reason = "Hazard ID already exists" }
  end
  local active = options.active
  if active == nil then
    active = true
  end
  if type(active) ~= "boolean" then
    return nil, { applied = false, code = "invalid_active", reason = "Hazard active state must be boolean" }
  end
  local hazard = {
    id = id,
    kind = "hazard",
    definition_id = definition.id,
    x = x,
    y = y,
    active = active,
  }
  self.hazards[id] = hazard
  self.hazard_order[#self.hazard_order + 1] = id
  local cell_hazards = self.hazards_by_cell[key(x, y)] or {}
  cell_hazards[#cell_hazards + 1] = hazard
  self.hazards_by_cell[key(x, y)] = cell_hazards
  return hazard, { applied = true, hazard_id = id }
end

function World:inspect_hazard(hazard_or_id)
  local hazard = type(hazard_or_id) == "table" and hazard_or_id or self.hazards[hazard_or_id]
  if not hazard or self.hazards[hazard.id] ~= hazard then
    return nil, "Unknown hazard"
  end
  local definition = self.registry:get_hazard(hazard.definition_id)
  return {
    id = hazard.id,
    definition_id = definition.id,
    display_name = definition.display_name,
    x = hazard.x,
    y = hazard.y,
    active = hazard.active,
    trigger = definition.trigger,
    effect = copy_object(definition.effect),
    render_style = definition.render_style,
  }
end

function World:is_passable(x, y)
  local object = self:object_at(x, y)
  return self:terrain_is_passable(x, y) and not (object and object.blocks_movement)
end

function World:blocks_vision(x, y)
  local material = self:get_material(x, y)
  if not material or material.blocks_vision then
    return true
  end
  local object = self:object_at(x, y)
  return object and object.blocks_vision or false
end

function World:blocks_projectile(x, y)
  local material = self:get_material(x, y)
  if not material or material.blocks_movement then
    return true
  end
  local object = self:object_at(x, y)
  return object and object.blocks_projectiles or false
end

function World:set_door_state(object_or_id, door_state)
  local object = type(object_or_id) == "table" and object_or_id or self.objects[object_or_id]
  if not object or self.objects[object.id] ~= object then
    return { applied = false, code = "invalid_target", reason = "Unknown world object" }
  end
  if object.interaction_role ~= "door" then
    return { applied = false, code = "not_interactable", reason = "Object is not a door" }
  end
  if object.destroyed then
    return { applied = false, code = "destroyed", reason = "Door is destroyed" }
  end
  if door_state ~= "open" and door_state ~= "closed" then
    return { applied = false, code = "invalid_door_state", reason = "Door state is invalid" }
  end
  if object.door_state == door_state then
    return { applied = false, code = door_state == "open" and "already_open" or "already_closed",
      reason = door_state == "open" and "Door is already open" or "Door is already closed" }
  end
  -- A closed door is gas-inaccessible. Refuse to corrupt finite gas state by
  -- closing onto an occupied doorway; later doors may model a small sealed
  -- chamber explicitly, but this compact first pass does not delete mass.
  if door_state == "closed" and self:is_gas_cell(object.x, object.y) then
    return { applied = false, code = "gas_occupied", reason = "Gas fills the doorway" }
  end
  local definition = self.registry:get_world_object(object.definition_id)
  if definition.power_required and not self:is_circuit_powered(object.circuit_id) then
    return { applied = false, code = "requires_power", reason = "Door requires circuit power", circuit_id = object.circuit_id }
  end
  object.door_state = door_state
  object.blocks_movement, object.blocks_vision, object.blocks_projectiles, object.blocks_gas =
    object_blocks_for_state(definition, door_state)
  return { applied = true, code = door_state, object_id = object.id, door_state = door_state,
    circuit_id = object.circuit_id, powered = self:is_circuit_powered(object.circuit_id) }
end

function World:set_generator_online(object_or_id, online)
  local object = type(object_or_id) == "table" and object_or_id or self.objects[object_or_id]
  if not object or self.objects[object.id] ~= object then
    return { applied = false, code = "invalid_target", reason = "Unknown world object" }
  end
  if object.interaction_role ~= "generator" then
    return { applied = false, code = "not_interactable", reason = "Object is not a generator" }
  end
  if object.destroyed then
    return { applied = false, code = "destroyed", reason = "Generator is destroyed" }
  end
  if type(online) ~= "boolean" then
    return { applied = false, code = "invalid_generator_state", reason = "Generator online state must be boolean" }
  end
  local changed = object.generator_online ~= online
  object.generator_online = online
  return { applied = changed, code = changed and "set" or "unchanged", object_id = object.id,
    online = online, circuit_id = object.circuit_id, powered = self:is_circuit_powered(object.circuit_id) }
end

function World:_next_object_id()
  if self.sequence_owner.allocate_world_object_id then
    return self.sequence_owner:allocate_world_object_id()
  end
  local sequence = self.sequence_owner.next_world_object_sequence
  self.sequence_owner.next_world_object_sequence = sequence + 1
  return string.format("world_object:%06d", sequence)
end

function World:place_object(definition_id, x, y, options)
  options = options or {}
  if not Grid.in_bounds(x, y) then
    return nil, { applied = false, code = "out_of_bounds", reason = "World object position is outside the world" }
  end
  if not self:terrain_is_passable(x, y) then
    return nil, { applied = false, code = "blocked_terrain", reason = "World object requires passable terrain" }
  end
  if self:object_at(x, y) then
    return nil, { applied = false, code = "occupied", reason = "World object tile is occupied" }
  end
  if self:is_hazardous(x, y) then
    return nil, { applied = false, code = "occupied_hazard", reason = "World object cannot overlap an active hazard" }
  end
  local definition = self.registry:get_world_object(definition_id)
  local material = self.registry:get_material(definition.material_id)
  local role = definition.interaction_role
  if role == "zone_connection" then
    if type(options.zone_connection_id) ~= "string" or options.zone_connection_id == ""
      or type(options.zone_connection_type) ~= "string" or options.zone_connection_type == ""
      or type(options.zone_connection_direction) ~= "string" or options.zone_connection_direction == "" then
      return nil, { applied = false, code = "invalid_zone_connection", reason = "Zone connection requires durable topology metadata" }
    end
  elseif options.zone_connection_id ~= nil or options.zone_connection_type ~= nil or options.zone_connection_direction ~= nil then
    return nil, { applied = false, code = "invalid_zone_connection", reason = "Only zone connection objects may carry topology metadata" }
  end
  local circuit_id = options.circuit_id
  if requires_circuit(definition) and (type(circuit_id) ~= "string" or not self.circuits[circuit_id]) then
    return nil, { applied = false, code = "unknown_circuit", reason = "Interactive world object requires an existing circuit" }
  end
  if not requires_circuit(definition) and circuit_id ~= nil then
    return nil, { applied = false, code = "unexpected_circuit", reason = "World object does not use a circuit" }
  end
  local door_state = options.door_state or definition.default_door_state
  if role == "door" and not DOOR_STATES[door_state] then
    return nil, { applied = false, code = "invalid_door_state", reason = "Door state is invalid" }
  end
  if role == "door" and door_state == "destroyed" then
    return nil, { applied = false, code = "invalid_door_state", reason = "New doors cannot be created destroyed" }
  end
  local generator_online = options.generator_online
  if role == "generator" and generator_online == nil then
    generator_online = true
  end
  if role == "generator" and type(generator_online) ~= "boolean" then
    return nil, { applied = false, code = "invalid_generator_state", reason = "Generator online state must be boolean" }
  end
  local discovery, discovery_reason = discovery_fields(self.registry, definition, options)
  if not discovery then
    return nil, { applied = false, code = "invalid_discovery", reason = discovery_reason }
  end
  local reinforcement, reinforcement_reason = reinforcement_fields(self.registry, definition, options)
  if not reinforcement then
    return nil, { applied = false, code = "invalid_reinforcement", reason = reinforcement_reason }
  end
  local id = options.id or self:_next_object_id()
  if self.objects[id] then
    return nil, { applied = false, code = "duplicate_id", reason = "World object ID already exists" }
  end
  local integrity = options.current_integrity or material.max_integrity
  if type(integrity) ~= "number" or integrity <= 0 or integrity > material.max_integrity then
    return nil, { applied = false, code = "invalid_integrity", reason = "World object integrity is invalid" }
  end
  local construction = construction_fields(definition, options)
  local storage_inventory
  if role == "storage" then
    storage_inventory = options.storage_inventory or Inventory.new({ width = definition.storage_width, height = definition.storage_height })
    assert(getmetatable(storage_inventory) == Inventory, "Storage object requires an Inventory")
    storage_inventory:validate()
  elseif options.storage_inventory ~= nil then
    return nil, { applied = false, code = "invalid_storage", reason = "Only storage objects may own an inventory" }
  end
  local anchor_protected = options.anchor_protected == true
  if anchor_protected and role ~= "reconstruction_station" then
    return nil, { applied = false, code = "invalid_anchor", reason = "Only reconstruction stations may be protected anchors" }
  end
  local object = {
    id = id,
    kind = "world_object",
    definition_id = definition.id,
    material_id = material.id,
    x = x,
    y = y,
    current_integrity = integrity,
    destroyed = false,
    interaction_role = role,
    circuit_id = circuit_id,
    door_state = role == "door" and door_state or nil,
    -- Preserve an explicit offline generator; Lua's and/or idiom would turn
    -- false into nil and make a legitimate persistent power state invalid.
    generator_online = role == "generator" and generator_online or nil,
    service_id = role == "service" and options.service_id or nil,
    service_stock = role == "service" and options.service_stock or nil,
    service_origin = role == "service" and options.service_origin or nil,
    world_content_site_id = options.world_content_site_id,
    required_unlock = role == "traversal" and definition.required_unlock or nil,
    zone_connection_id = role == "zone_connection" and options.zone_connection_id or nil,
    zone_connection_type = role == "zone_connection" and options.zone_connection_type or nil,
    zone_connection_direction = role == "zone_connection" and options.zone_connection_direction or nil,
    discovery_id = discovery.discovery_id,
    discovery_access_profile_id = discovery.discovery_access_profile_id,
    discovery_provenance = discovery.discovery_provenance,
    discovery_claimed = discovery.discovery_claimed,
    reinforcement_profile_id = reinforcement.reinforcement_profile_id,
    reinforcement_faction_id = reinforcement.reinforcement_faction_id,
    reinforcement_charges = reinforcement.reinforcement_charges,
    reinforcement_state = reinforcement.reinforcement_state,
    reinforcement_delay = reinforcement.reinforcement_delay,
    reinforcement_just_armed = reinforcement.reinforcement_just_armed,
    reinforcement_wave_enemy_ids = reinforcement.reinforcement_wave_enemy_ids,
    reinforcement_provenance = reinforcement.reinforcement_provenance,
    movable_by_force = definition.movable_by_force,
    constructed = construction.constructed,
    construction_recipe_id = construction.construction_recipe_id,
    construction_campaign_id = construction.construction_campaign_id,
    construction_recovery_yields = construction.construction_recovery_yields,
    storage_inventory = storage_inventory,
    anchor_protected = role == "reconstruction_station" and anchor_protected or nil,
  }
  if role == "service" then
    if type(object.service_id) ~= "string" then
      return nil, { applied = false, code = "invalid_service", reason = "Service kiosk requires a service ID" }
    end
    self.registry:get_service(object.service_id)
    if type(object.service_stock) ~= "table" then
      return nil, { applied = false, code = "invalid_stock", reason = "Service kiosk requires persistent stock" }
    end
  end
  if role == "generator" then
    object.generator_online = generator_online
  end
  object.blocks_movement, object.blocks_vision, object.blocks_projectiles, object.blocks_gas =
    object_blocks_for_state(definition, object.door_state)
  self.objects[id] = object
  self.object_order[#self.object_order + 1] = id
  self.objects_by_cell[key(x, y)] = object
  return object, { applied = true, object_id = id }
end

function World:move_object(object_or_id, x, y)
  local object = type(object_or_id) == "table" and object_or_id or self.objects[object_or_id]
  if not object or self.objects[object.id] ~= object then
    return { applied = false, code = "unknown_object", reason = "Unknown world object" }
  end
  if object.destroyed then
    return { applied = false, code = "destroyed", reason = "Destroyed world objects cannot move" }
  end
  if not Grid.in_bounds(x, y) then
    return { applied = false, code = "out_of_bounds", reason = "World object destination is outside the world" }
  end
  if not self:terrain_is_passable(x, y) then
    return { applied = false, code = "blocked_terrain", reason = "World object destination is blocked by terrain" }
  end
  local occupant = self:object_at(x, y)
  if occupant and occupant ~= object then
    return { applied = false, code = "blocked_object", reason = "World object destination is occupied" }
  end
  if self:is_hazardous(x, y) then
    return { applied = false, code = "blocked_hazard", reason = "World object destination contains an active hazard" }
  end
  self.objects_by_cell[key(object.x, object.y)] = nil
  object.x, object.y = x, y
  self.objects_by_cell[key(x, y)] = object
  return { applied = true, object_id = object.id, x = x, y = y }
end

function World:damage_terrain(x, y, spec)
  local cell = self:get_cell(x, y)
  if not cell then
    return { applied = false, code = "out_of_bounds", x = x, y = y, reason = "Terrain position is outside the world" }
  end
  local material = self.registry:get_material(cell.material_id)
  if not material.destructible then
    return {
      applied = false,
      code = material.solid and "indestructible" or "not_destructible",
      x = x,
      y = y,
      material_id = material.id,
      reason = material.solid and "Terrain material is indestructible" or "Terrain has no destructible physical fuel",
    }
  end
  local previous_integrity = cell.current_integrity
  local new_integrity = math.max(0, previous_integrity - spec.amount)
  cell.current_integrity = new_integrity
  local result = {
    applied = new_integrity ~= previous_integrity,
    target_type = "terrain",
    x = x,
    y = y,
    material_id = material.id,
    cause = spec.cause,
    source = spec.source,
    source_actor_id = spec.source_actor_id,
    source_component_id = spec.source_component_id,
    ability_id = spec.ability_id,
    previous_integrity = previous_integrity,
    new_integrity = new_integrity,
    max_integrity = material.max_integrity,
    destroyed = false,
  }
  if new_integrity == 0 then
    cell.destroyed = true
    cell.destroyed_from_material_id = material.id
    cell.material_id = material.destruction_material_id
    self:_deactivate_target_fire("terrain", nil, x, y, "target_destroyed")
    result.destroyed = true
    result.destroyed_material_id = cell.material_id
    result.harvest_drops = self:_drop_yields(material.harvest_yield and { material.harvest_yield } or {}, x, y)
  end
  return result
end

function World:damage_object(object_or_id, spec)
  local object = type(object_or_id) == "table" and object_or_id or self.objects[object_or_id]
  if not object or self.objects[object.id] ~= object then
    return { applied = false, code = "unknown_object", reason = "Unknown world object" }
  end
  if object.destroyed then
    return { applied = false, code = "destroyed", object_id = object.id, reason = "World object is already destroyed" }
  end
  if object.zone_connection_id or (object.interaction_role == "reconstruction_station" and object.anchor_protected) then
    -- Required campaign links are protected infrastructure. Future player
    -- construction validation will reserve the same footprint; combat cannot
    -- quietly turn a generated return route into a softlock in this tranche.
    return { applied = false, code = object.zone_connection_id and "protected_connection" or "protected_station", object_id = object.id,
      reason = object.zone_connection_id and "Persistent zone connection cannot be destroyed" or "Active reconstruction station cannot be destroyed" }
  end
  local material = self.registry:get_material(object.material_id)
  local previous_integrity = object.current_integrity
  local new_integrity = math.max(0, previous_integrity - spec.amount)
  object.current_integrity = new_integrity
  local result = {
    applied = new_integrity ~= previous_integrity,
    target_type = "world_object",
    object_id = object.id,
    definition_id = object.definition_id,
    x = object.x,
    y = object.y,
    material_id = material.id,
    cause = spec.cause,
    source = spec.source,
    source_actor_id = spec.source_actor_id,
    source_component_id = spec.source_component_id,
    ability_id = spec.ability_id,
    previous_integrity = previous_integrity,
    new_integrity = new_integrity,
    max_integrity = material.max_integrity,
    destroyed = false,
  }
  if new_integrity == 0 then
    object.destroyed = true
    if object.interaction_role == "door" then
      object.door_state = "destroyed"
    elseif object.interaction_role == "breaker" then
      -- A destroyed breaker leaves its circuit safely disabled. Generator
      -- source state remains physical and can later serve another circuit.
      self:set_circuit_enabled(object.circuit_id, false)
    elseif object.interaction_role == "reinforcement" then
      object.reinforcement_charges = 0
      if object.reinforcement_state ~= "spent" then object.reinforcement_state = "cancelled" end
      object.reinforcement_delay = nil
      object.reinforcement_just_armed = false
    end
    object.blocks_movement = false
    object.blocks_vision = false
    object.blocks_projectiles = false
    object.blocks_gas = false
    self.objects_by_cell[key(object.x, object.y)] = nil
    self:_deactivate_target_fire("object", object.id, nil, nil, "target_destroyed")
    local definition = self.registry:get_world_object(object.definition_id)
    local yields = object.constructed and object.construction_recovery_yields or (definition.harvest_yield and { definition.harvest_yield } or {})
    result.harvest_drops = self:_drop_yields(yields, object.x, object.y)
    result.storage_drops = {}
    if object.storage_inventory then
      local entries = {}
      for _, entry in ipairs(object.storage_inventory.entries) do entries[#entries + 1] = entry end
      for _, entry in ipairs(entries) do
        local item = assert(object.storage_inventory:remove(entry.physical_id))
        local ground, ground_result = self:place_ground_item(item, object.x, object.y)
        assert(ground, ground_result.reason)
        result.storage_drops[#result.storage_drops + 1] = ground
      end
    end
    result.destroyed = true
  end
  return result
end

function World:inspect_object(object_or_id)
  local object = type(object_or_id) == "table" and object_or_id or self.objects[object_or_id]
  if not object or self.objects[object.id] ~= object then
    return nil, "Unknown world object"
  end
  local definition = self.registry:get_world_object(object.definition_id)
  local material = self.registry:get_material(object.material_id)
  return {
    id = object.id,
    definition_id = definition.id,
    display_name = definition.display_name,
    material_id = material.id,
    x = object.x,
    y = object.y,
    current_integrity = object.current_integrity,
    max_integrity = material.max_integrity,
    destroyed = object.destroyed,
    blocks_movement = not object.destroyed and object.blocks_movement or false,
    blocks_vision = not object.destroyed and object.blocks_vision or false,
    blocks_projectiles = not object.destroyed and object.blocks_projectiles or false,
    blocks_gas = not object.destroyed and object.blocks_gas or false,
    movable_by_force = object.movable_by_force,
    interaction_role = object.interaction_role,
    circuit_id = object.circuit_id,
    door_state = object.door_state,
    generator_online = object.generator_online,
    service_id = object.service_id,
    service_stock = object.service_stock,
    service_origin = object.service_origin,
    world_content_site_id = object.world_content_site_id,
    required_unlock = object.required_unlock,
    zone_connection_id = object.zone_connection_id,
    zone_connection_type = object.zone_connection_type,
    zone_connection_direction = object.zone_connection_direction,
    discovery_id = object.discovery_id,
    discovery_access_profile_id = object.discovery_access_profile_id,
    discovery_provenance = object.discovery_provenance,
    discovery_claimed = object.discovery_claimed,
    reinforcement_profile_id = object.reinforcement_profile_id,
    reinforcement_faction_id = object.reinforcement_faction_id,
    reinforcement_charges = object.reinforcement_charges,
    reinforcement_state = object.reinforcement_state,
    reinforcement_delay = object.reinforcement_delay,
    reinforcement_just_armed = object.reinforcement_just_armed,
    reinforcement_wave_enemy_ids = object.reinforcement_wave_enemy_ids,
    reinforcement_provenance = object.reinforcement_provenance,
    circuit_powered = object.circuit_id and self:is_circuit_powered(object.circuit_id) or nil,
    circuit_enabled = object.circuit_id and self.circuits[object.circuit_id].enabled or nil,
    constructed = object.constructed == true,
    construction_recipe_id = object.construction_recipe_id,
    construction_campaign_id = object.construction_campaign_id,
    storage_item_count = object.storage_inventory and #object.storage_inventory.entries or nil,
    anchor_protected = object.anchor_protected == true,
    conductive = material.conductive,
    tool_effectiveness = material.tool_effectiveness,
    fires = (function()
      local fires = {}
      for _, fire in ipairs(self:list_fires(true)) do
        if fire.target_kind == "object" and fire.target_id == object.id then
          fires[#fires + 1] = assert(self:inspect_fire(fire))
        end
      end
      return fires
    end)(),
  }
end

function World:inspect_cell(x, y)
  local cell = self:get_cell(x, y)
  if not cell then
    return nil, "Terrain position is outside the world"
  end
  local material = self.registry:get_material(cell.material_id)
  local objects = {}
  for _, object in ipairs(self:objects_at(x, y, true)) do
    objects[#objects + 1] = assert(self:inspect_object(object))
  end
  local hazards = {}
  for _, hazard in ipairs(self:hazards_at(x, y, true)) do
    hazards[#hazards + 1] = assert(self:inspect_hazard(hazard))
  end
  local fires = {}
  for _, fire in ipairs(self:fires_at(x, y, true)) do
    fires[#fires + 1] = assert(self:inspect_fire(fire))
  end
  local ground_items = {}
  for _, ground in ipairs(self:ground_items_at(x, y)) do
    local value = { id = ground.id, item_type = ground.item.item_type,
      display_name = ground.item.display_name, resource_id = ground.item.resource_id, quantity = ground.item.quantity }
    if ground.item.item_type == "tool" then
      local tool = ground.item.object
      local definition = self.registry:get_tool(tool.definition_id)
      value.tool_definition_id, value.tool_family = definition.id, definition.family
      value.current_durability, value.maximum_durability, value.condition = tool.current_durability, tool.maximum_durability, Tool.condition(tool)
      value.modification = definition.modification
    end
    ground_items[#ground_items + 1] = value
  end
  local liquid = self:liquid_at(x, y)
  local liquid_data
  if liquid then
    local definition = self.registry:get_liquid(liquid.liquid_id)
    liquid_data = {
      liquid_id = definition.id,
      display_name = definition.display_name,
      amount = liquid.amount,
      max_depth = definition.max_depth,
      extinguishes_fire = definition.extinguishes_fire,
      conductive = definition.conductive,
    }
  end
  local gas = self:gas_at(x, y)
  local gas_data
  if gas then
    local definition = self.registry:get_gas(gas.gas_id)
    gas_data = {
      gas_id = definition.id,
      display_name = definition.display_name,
      concentration = gas.concentration,
      max_concentration = definition.max_concentration,
      exposure_threshold = definition.exposure_threshold,
      damage = definition.damage,
      harmful = gas.concentration >= definition.exposure_threshold,
    }
  end
  return {
    x = x,
    y = y,
    material_id = material.id,
    passable = self:is_passable(x, y),
    blocks_vision = self:blocks_vision(x, y),
    blocks_projectile = self:blocks_projectile(x, y),
    current_integrity = cell.current_integrity,
    max_integrity = material.max_integrity,
    destructible = material.destructible,
    tool_effectiveness = material.tool_effectiveness,
    destroyed = cell.destroyed,
    destroyed_from_material_id = cell.destroyed_from_material_id,
    objects = objects,
    hazards = hazards,
    liquid = liquid_data,
    gas = gas_data,
    conductivity = self:conductivity_at(x, y),
    fires = fires,
    ground_items = ground_items,
  }
end

function World:describe_cell(x, y)
  local inspected, reason = self:inspect_cell(x, y)
  if not inspected then
    return reason
  end
  local integrity = inspected.current_integrity and (inspected.current_integrity .. "/" .. inspected.max_integrity) or "n/a"
  local object_ids = {}
  for _, object in ipairs(inspected.objects) do
    object_ids[#object_ids + 1] = object.id
  end
  local hazard_ids = {}
  for _, hazard in ipairs(inspected.hazards) do
    hazard_ids[#hazard_ids + 1] = hazard.id
  end
  local fire_ids = {}
  for _, fire in ipairs(inspected.fires) do
    fire_ids[#fire_ids + 1] = fire.id
  end
  local liquid = inspected.liquid and (inspected.liquid.liquid_id .. "@" .. inspected.liquid.amount) or ""
  local gas = inspected.gas and (inspected.gas.gas_id .. "@" .. inspected.gas.concentration) or ""
  return string.format("%d,%d %s passable=%s blocks_vision=%s blocks_projectile=%s integrity=%s destructible=%s destroyed=%s conductive=%s objects=%s hazards=%s liquid=%s gas=%s fires=%s",
    x, y, inspected.material_id, tostring(inspected.passable), tostring(inspected.blocks_vision),
    tostring(inspected.blocks_projectile), integrity, tostring(inspected.destructible), tostring(inspected.destroyed),
    tostring(inspected.conductivity.conductive), table.concat(object_ids, ","), table.concat(hazard_ids, ","), liquid, gas, table.concat(fire_ids, ","))
end

function World:mutation_data()
  local mutations = {}
  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do
      local cell = self.cells[key(x, y)]
      if cell.destroyed or cell.current_integrity ~= nil then
        local material = self.registry:get_material(cell.material_id)
        local is_full_integrity = cell.current_integrity == material.max_integrity
        if cell.destroyed or not is_full_integrity then
          mutations[#mutations + 1] = {
            x = x,
            y = y,
            material_id = cell.material_id,
            current_integrity = cell.current_integrity,
            destroyed = cell.destroyed,
            destroyed_from_material_id = cell.destroyed_from_material_id,
          }
        end
      end
    end
  end
  return mutations
end

-- A generated floor is only construction input.  Active-run saves need every
-- current terrain cell so restoration cannot consume RNG or accidentally
-- recreate an already-mutated floor from a seed.
function World:cell_data()
  local cells = {}
  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do
      local cell = self.cells[key(x, y)]
      cells[#cells + 1] = {
        x = x,
        y = y,
        material_id = cell.material_id,
        current_integrity = cell.current_integrity,
        destroyed = cell.destroyed,
        destroyed_from_material_id = cell.destroyed_from_material_id,
      }
    end
  end
  return cells
end

function World:object_data()
  local objects = {}
  for _, object in ipairs(self:list_objects(true)) do
    local saved = copy_object(object)
    saved.construction_recovery_yields = copy_yields(object.construction_recovery_yields)
    saved.storage_inventory = object.storage_inventory and object.storage_inventory:to_data() or nil
    objects[#objects + 1] = saved
  end
  return objects
end

function World:ground_item_data()
  local values = {}
  for _, ground in ipairs(self:list_ground_items()) do
    values[#values + 1] = {
      id = ground.id,
      x = ground.x,
      y = ground.y,
      item = ground.item:to_data(),
    }
  end
  return values
end

function World:hazard_data()
  local hazards = {}
  for _, hazard in ipairs(self:list_hazards(true)) do
    hazards[#hazards + 1] = copy_object(hazard)
  end
  return hazards
end

function World:liquid_data()
  local liquids = {}
  for _, liquid in ipairs(self:list_liquids()) do
    liquids[#liquids + 1] = copy_object(liquid)
  end
  return liquids
end

function World:gas_data()
  local gases = {}
  for _, gas in ipairs(self:list_gases()) do
    gases[#gases + 1] = copy_object(gas)
  end
  return gases
end

function World:circuit_data()
  local circuits = {}
  for _, circuit in ipairs(self:list_circuits()) do
    circuits[#circuits + 1] = { id = circuit.id, enabled = circuit.enabled }
  end
  return circuits
end

function World:fire_data()
  local fires = {}
  for _, fire in ipairs(self:list_fires(true)) do
    fires[#fires + 1] = copy_object(fire)
    fires[#fires].provenance = copy_object(fire.provenance)
  end
  return fires
end

function World:to_data()
  return {
    terrain = self.terrain,
    cells = self:cell_data(),
    mutations = self:mutation_data(),
    objects = self:object_data(),
    hazards = self:hazard_data(),
    circuits = self:circuit_data(),
    liquids = self:liquid_data(),
    liquid_tick = self.liquid_tick,
    gases = self:gas_data(),
    fires = self:fire_data(),
    fire_tick = self.fire_tick,
    ground_items = self:ground_item_data(),
  }
end

-- Restoration deliberately bypasses generation and sequence allocation.  It
-- rebuilds the ordinary runtime indexes from immutable/plain state, then the
-- normal World validation enforces all material, object, medium, and fire
-- contracts.
function World.from_data(registry, data, sequence_owner)
  assert(type(data) == "table" and type(data.terrain) == "string", "World data must include terrain")
  assert(type(data.cells) == "table", "World data must include a full cell snapshot")
  local closed_layout = {}
  local world = World.new(registry, data.terrain, closed_layout, sequence_owner)
  local seen_cells = {}
  for _, saved in ipairs(data.cells) do
    assert(type(saved) == "table" and type(saved.x) == "number" and type(saved.y) == "number"
      and saved.x % 1 == 0 and saved.y % 1 == 0 and Grid.in_bounds(saved.x, saved.y), "World cell is invalid")
    local location_key = key(saved.x, saved.y)
    assert(not seen_cells[location_key], "World cell snapshot contains a duplicate coordinate")
    seen_cells[location_key] = true
    registry:get_material(saved.material_id)
    world.cells[location_key] = {
      material_id = saved.material_id,
      current_integrity = saved.current_integrity,
      destroyed = saved.destroyed == true,
      destroyed_from_material_id = saved.destroyed_from_material_id,
    }
  end
  assert(#data.cells == Grid.width * Grid.height, "World cell snapshot has an invalid size")
  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do
      assert(seen_cells[key(x, y)], "World cell snapshot is missing a coordinate")
    end
  end

  for _, saved in ipairs(data.circuits or {}) do
    local result = world:register_circuit(saved.id, { enabled = saved.enabled })
    assert(result.applied, result.reason)
  end

  for _, saved in ipairs(data.objects or {}) do
    local definition = registry:get_world_object(saved.definition_id)
    local material = registry:get_material(saved.material_id)
    assert(material.id == definition.material_id, "World object material does not match its definition")
    assert(type(saved.id) == "string" and saved.id ~= "", "World object has an invalid ID")
    assert(not world.objects[saved.id], "World object ID is duplicated")
    assert(Grid.in_bounds(saved.x, saved.y), "World object is outside the world")
    local object = {
      id = saved.id,
      kind = "world_object",
      definition_id = definition.id,
      material_id = material.id,
      x = saved.x,
      y = saved.y,
      current_integrity = saved.current_integrity,
      destroyed = saved.destroyed == true,
      interaction_role = definition.interaction_role,
      circuit_id = saved.circuit_id,
      door_state = saved.door_state,
      generator_online = saved.generator_online,
      service_id = saved.service_id,
      service_stock = saved.service_stock,
      service_origin = saved.service_origin,
      world_content_site_id = saved.world_content_site_id,
      required_unlock = definition.interaction_role == "traversal" and definition.required_unlock or nil,
      zone_connection_id = definition.interaction_role == "zone_connection" and saved.zone_connection_id or nil,
      zone_connection_type = definition.interaction_role == "zone_connection" and saved.zone_connection_type or nil,
      zone_connection_direction = definition.interaction_role == "zone_connection" and saved.zone_connection_direction or nil,
      discovery_id = saved.discovery_id,
      discovery_access_profile_id = saved.discovery_access_profile_id,
      discovery_provenance = saved.discovery_provenance,
      discovery_claimed = saved.discovery_claimed,
      reinforcement_profile_id = saved.reinforcement_profile_id,
      reinforcement_faction_id = saved.reinforcement_faction_id,
      reinforcement_charges = saved.reinforcement_charges,
      reinforcement_state = saved.reinforcement_state,
      reinforcement_delay = saved.reinforcement_delay,
      reinforcement_just_armed = saved.reinforcement_just_armed,
      reinforcement_wave_enemy_ids = saved.reinforcement_wave_enemy_ids,
      reinforcement_provenance = saved.reinforcement_provenance,
      movable_by_force = definition.movable_by_force,
      constructed = saved.constructed == true,
      construction_recipe_id = saved.construction_recipe_id,
      construction_campaign_id = saved.construction_campaign_id,
      construction_recovery_yields = copy_yields(saved.construction_recovery_yields),
      storage_inventory = saved.storage_inventory and Inventory.from_data(saved.storage_inventory, function(item)
        return PhysicalItem.from_data(item, registry)
      end) or nil,
      anchor_protected = definition.interaction_role == "reconstruction_station" and saved.anchor_protected == true or nil,
    }
    local discovery, discovery_reason = discovery_fields(registry, definition, object)
    assert(discovery, discovery_reason)
    object.discovery_id = discovery.discovery_id
    object.discovery_access_profile_id = discovery.discovery_access_profile_id
    object.discovery_provenance = discovery.discovery_provenance
    object.discovery_claimed = discovery.discovery_claimed
    local reinforcement, reinforcement_reason = reinforcement_fields(registry, definition, object)
    assert(reinforcement, reinforcement_reason)
    object.reinforcement_profile_id = reinforcement.reinforcement_profile_id
    object.reinforcement_faction_id = reinforcement.reinforcement_faction_id
    object.reinforcement_charges = reinforcement.reinforcement_charges
    object.reinforcement_state = reinforcement.reinforcement_state
    object.reinforcement_delay = reinforcement.reinforcement_delay
    object.reinforcement_just_armed = reinforcement.reinforcement_just_armed
    object.reinforcement_wave_enemy_ids = reinforcement.reinforcement_wave_enemy_ids
    object.reinforcement_provenance = reinforcement.reinforcement_provenance
    if requires_circuit(definition) then
      assert(type(object.circuit_id) == "string" and world.circuits[object.circuit_id],
        "Interactive world object references an unknown circuit")
    else
      assert(object.circuit_id == nil, "World object unexpectedly references a circuit")
    end
    if object.interaction_role == "door" and object.destroyed then
      object.door_state = "destroyed"
    end
    if object.destroyed then
      object.current_integrity = 0
      object.blocks_movement, object.blocks_vision, object.blocks_projectiles, object.blocks_gas = false, false, false, false
    else
      object.blocks_movement, object.blocks_vision, object.blocks_projectiles, object.blocks_gas =
        object_blocks_for_state(definition, object.door_state)
      assert(not world.objects_by_cell[key(object.x, object.y)], "Multiple live world objects occupy one cell")
      world.objects_by_cell[key(object.x, object.y)] = object
    end
    world.objects[object.id] = object
    world.object_order[#world.object_order + 1] = object.id
  end

  for _, saved in ipairs(data.ground_items or {}) do
    assert(type(saved) == "table" and type(saved.id) == "string" and saved.id ~= "" and Grid.in_bounds(saved.x, saved.y),
      "Ground item data is invalid")
    local item = PhysicalItem.from_data(saved.item, registry)
    assert(item.physical_id == saved.id, "Ground item ID does not match item")
    local ground, result = world:place_ground_item(item, saved.x, saved.y)
    assert(ground, result.reason)
  end

  for _, saved in ipairs(data.hazards or {}) do
    local definition = registry:get_hazard(saved.definition_id)
    assert(type(saved.id) == "string" and saved.id ~= "" and not world.hazards[saved.id], "Hazard ID is invalid")
    assert(Grid.in_bounds(saved.x, saved.y), "Hazard is outside the world")
    local hazard = { id = saved.id, kind = "hazard", definition_id = definition.id, x = saved.x, y = saved.y, active = saved.active }
    world.hazards[hazard.id] = hazard
    world.hazard_order[#world.hazard_order + 1] = hazard.id
    local list = world.hazards_by_cell[key(hazard.x, hazard.y)] or {}
    list[#list + 1] = hazard
    world.hazards_by_cell[key(hazard.x, hazard.y)] = list
  end

  for _, saved in ipairs(data.liquids or {}) do
    local result = world:set_liquid(saved.x, saved.y, saved.liquid_id, saved.amount)
    assert(result.applied, result.reason)
  end
  world.liquid_tick = data.liquid_tick or 0
  for _, saved in ipairs(data.gases or {}) do
    local result = world:set_gas(saved.x, saved.y, saved.gas_id, saved.concentration)
    assert(result.applied, result.reason)
  end

  world.fire_tick = data.fire_tick or 0
  world.fire_ticking = false
  for _, saved in ipairs(data.fires or {}) do
    assert(type(saved.id) == "string" and saved.id ~= "" and not world.fires[saved.id], "Fire ID is invalid")
    local fire = {
      id = saved.id,
      kind = "fire",
      target_kind = saved.target_kind,
      target_id = saved.target_id,
      target_key = saved.target_key,
      x = saved.x,
      y = saved.y,
      age = saved.age,
      ready_tick = saved.ready_tick,
      active = saved.active,
      extinguished_reason = saved.extinguished_reason,
      provenance = copy_object(saved.provenance or {}),
    }
    world.fires[fire.id] = fire
    world.fire_order[#world.fire_order + 1] = fire.id
    if fire.active then
      assert(not world.fires_by_target[fire.target_key], "Multiple active fires target one physical fuel")
      world.fires_by_target[fire.target_key] = fire
    end
  end
  world:validate()
  return world
end

function World:validate()
  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do
      local cell = self.cells[key(x, y)]
      assert(cell, "World is missing terrain cell " .. x .. "," .. y)
      local material = self.registry:get_material(cell.material_id)
      if cell.destroyed then
        assert(not material.solid, "Destroyed terrain must become non-solid")
        assert(cell.current_integrity == 0, "Destroyed terrain integrity must be zero")
      elseif material.destructible then
        assert(type(cell.current_integrity) == "number" and cell.current_integrity >= 0
          and cell.current_integrity <= material.max_integrity, "Terrain integrity is invalid")
      else
        assert(cell.current_integrity == nil, "Indestructible terrain cannot have mutable integrity")
      end
    end
  end

  local circuit_ids = {}
  for _, circuit_id in ipairs(self.circuit_order) do
    local circuit = self.circuits[circuit_id]
    assert(type(circuit_id) == "string" and circuit_id ~= "", "Circuit ID is invalid")
    assert(circuit and circuit.id == circuit_id, "Circuit order references invalid circuit")
    assert(not circuit_ids[circuit_id], "Duplicate circuit ID '" .. circuit_id .. "'")
    circuit_ids[circuit_id] = true
    assert(type(circuit.enabled) == "boolean", "Circuit enabled state must be boolean")
  end
  for circuit_id in pairs(self.circuits) do
    assert(circuit_ids[circuit_id], "Circuit is missing from deterministic order")
  end

  assert(type(self.liquid_tick) == "number" and self.liquid_tick >= 0 and self.liquid_tick % 1 == 0,
    "World liquid tick must be a non-negative integer")
  for location_key, liquid in pairs(self.liquids) do
    assert(type(liquid) == "table", "Liquid state must be a table")
    assert(type(liquid.x) == "number" and type(liquid.y) == "number"
      and liquid.x % 1 == 0 and liquid.y % 1 == 0, "Liquid position is invalid")
    assert(Grid.in_bounds(liquid.x, liquid.y), "Liquid is outside world bounds")
    assert(key(liquid.x, liquid.y) == location_key, "Liquid cell key does not match its position")
    local definition = self.registry.liquids[liquid.liquid_id]
    assert(definition, "Liquid references unknown definition")
    assert(type(liquid.amount) == "number" and liquid.amount > 0 and liquid.amount % 1 == 0
      and liquid.amount <= definition.max_depth, "Liquid amount is invalid")
    assert(self:terrain_is_passable(liquid.x, liquid.y), "Liquid occupies impassable terrain")
  end

  for location_key, gas in pairs(self.gases) do
    assert(type(gas) == "table", "Gas state must be a table")
    assert(type(gas.x) == "number" and type(gas.y) == "number"
      and gas.x % 1 == 0 and gas.y % 1 == 0, "Gas position is invalid")
    assert(Grid.in_bounds(gas.x, gas.y), "Gas is outside world bounds")
    assert(key(gas.x, gas.y) == location_key, "Gas cell key does not match its position")
    local definition = self.registry.gases[gas.gas_id]
    assert(definition, "Gas references unknown definition")
    assert(type(gas.concentration) == "number" and gas.concentration > 0 and gas.concentration % 1 == 0
      and gas.concentration <= definition.max_concentration, "Gas concentration is invalid")
    assert(self:allows_gas_at(gas.x, gas.y), "Gas occupies inaccessible terrain")
  end

  local object_ids, occupied = {}, {}
  for _, id in ipairs(self.object_order) do
    local object = self.objects[id]
    assert(object and object.id == id, "World object order references an invalid object")
    assert(not object_ids[id], "Duplicate world object ID '" .. id .. "'")
    object_ids[id] = true
    assert(Grid.in_bounds(object.x, object.y), "World object is outside world bounds")
    local definition = self.registry:get_world_object(object.definition_id)
    local material = self.registry:get_material(object.material_id)
    assert(object.material_id == definition.material_id, "World object material does not match definition")
    assert(type(object.current_integrity) == "number" and object.current_integrity >= 0
      and object.current_integrity <= material.max_integrity, "World object integrity is invalid")
    local location_key = key(object.x, object.y)
    if object.interaction_role then
      assert(object.interaction_role == definition.interaction_role, "World object interaction role does not match definition")
    end
    local discovery, discovery_reason = discovery_fields(self.registry, definition, object)
    assert(discovery, discovery_reason)
    local reinforcement, reinforcement_reason = reinforcement_fields(self.registry, definition, object)
    assert(reinforcement, reinforcement_reason)
    if requires_circuit(definition) then
      assert(type(object.circuit_id) == "string" and self.circuits[object.circuit_id],
        "Interactive world object references unknown circuit")
    else
      assert(object.circuit_id == nil, "World object cannot reference an unused circuit")
    end
    if object.interaction_role == "service" then
      self.registry:get_service(object.service_id)
      assert(type(object.service_stock) == "table", "Service kiosk has invalid stock")
    end
    assert(object.world_content_site_id == nil or (type(object.world_content_site_id) == "string" and object.world_content_site_id ~= ""),
      "World object has invalid world content provenance")
    if object.interaction_role == "storage" then
      assert(object.storage_inventory and getmetatable(object.storage_inventory) == Inventory,
        "Storage object is missing its inventory")
      object.storage_inventory:validate()
    else
      assert(object.storage_inventory == nil, "Only storage object may retain an inventory")
    end
    if object.constructed then
      assert(type(object.construction_recipe_id) == "string" and object.construction_recipe_id ~= ""
        and type(object.construction_campaign_id) == "string" and object.construction_campaign_id ~= "",
        "Constructed world object has invalid provenance")
      for _, yield in ipairs(object.construction_recovery_yields or {}) do
        self.registry:get_resource(yield.resource_id)
        assert(type(yield.amount) == "number" and yield.amount >= 1 and yield.amount % 1 == 0,
          "Constructed world object has invalid recovery yield")
      end
    else
      assert(object.construction_recipe_id == nil and object.construction_campaign_id == nil,
        "Generated world object cannot carry construction provenance")
    end
    if object.interaction_role == "reconstruction_station" then
      assert(object.anchor_protected == nil or type(object.anchor_protected) == "boolean",
        "Reconstruction station anchor protection is invalid")
    else
      assert(object.anchor_protected == nil, "Only reconstruction stations may be anchor protected")
    end
    if object.interaction_role == "zone_connection" then
      assert(type(object.zone_connection_id) == "string" and object.zone_connection_id ~= ""
        and type(object.zone_connection_type) == "string" and object.zone_connection_type ~= ""
        and type(object.zone_connection_direction) == "string" and object.zone_connection_direction ~= "",
        "Zone connection object has invalid topology metadata")
    else
      assert(object.zone_connection_id == nil and object.zone_connection_type == nil and object.zone_connection_direction == nil,
        "Only zone connection objects may carry topology metadata")
    end
    if object.destroyed then
      assert(object.current_integrity == 0, "Destroyed world object integrity must be zero")
      assert(not object.blocks_movement and not object.blocks_vision and not object.blocks_projectiles and not object.blocks_gas,
        "Destroyed world object cannot retain blocking state")
      if object.interaction_role == "door" then
        assert(object.door_state == "destroyed", "Destroyed door must retain destroyed state")
      end
      assert(self.objects_by_cell[location_key] ~= object, "Destroyed world object cannot occupy a cell")
    else
      assert(self:terrain_is_passable(object.x, object.y), "World object is placed in impassable terrain")
      if object.interaction_role ~= "door" then
        assert(object.blocks_movement == definition.blocks_movement and object.blocks_vision == definition.blocks_vision
          and object.blocks_projectiles == definition.blocks_projectiles and object.blocks_gas == definition.blocks_gas,
          "World object blocking state does not match definition")
      end
      if object.interaction_role == "door" then
        assert(object.door_state == "open" or object.door_state == "closed", "Door state is invalid")
        local movement, vision, projectile, gas = object_blocks_for_state(definition, object.door_state)
        assert(object.blocks_movement == movement and object.blocks_vision == vision and object.blocks_projectiles == projectile
          and object.blocks_gas == gas, "Door blocking state does not match door state")
      elseif object.interaction_role == "generator" then
        assert(type(object.generator_online) == "boolean" and object.door_state == nil,
          "Generator has invalid device state")
      elseif object.interaction_role == "breaker" then
        assert(object.generator_online == nil and object.door_state == nil, "Breaker has invalid device state")
      end
      assert(not occupied[location_key], "Multiple live world objects occupy one cell")
      occupied[location_key] = true
      assert(self.objects_by_cell[location_key] == object, "World object cell index is invalid")
    end
  end
  for id in pairs(self.objects) do
    assert(object_ids[id], "World object is missing from deterministic object order")
  end
  for location_key, object in pairs(self.objects_by_cell) do
    assert(object_ids[object.id] and not object.destroyed and key(object.x, object.y) == location_key,
      "World object cell index references invalid state")
  end
  local ground_ids = {}
  for _, id in ipairs(self.ground_item_order) do
    local ground = self.ground_items[id]
    assert(ground and ground.id == id and ground.item and ground.item.physical_id == id,
      "Ground item order references invalid state")
    assert(not ground_ids[id], "Duplicate ground item ID '" .. id .. "'")
    ground_ids[id] = true
    assert(Grid.in_bounds(ground.x, ground.y) and self:terrain_is_passable(ground.x, ground.y),
      "Ground item is not on passable terrain")
    local found = false
    for _, cell_item in ipairs(self.ground_items_by_cell[key(ground.x, ground.y)] or {}) do
      if cell_item == ground then found = true; break end
    end
    assert(found, "Ground item cell index is invalid")
  end
  for id, ground in pairs(self.ground_items) do
    assert(ground_ids[id] and ground.id == id, "Ground item is missing from deterministic order")
  end
  for location_key, values in pairs(self.ground_items_by_cell) do
    for _, ground in ipairs(values) do
      assert(ground_ids[ground.id] and key(ground.x, ground.y) == location_key,
        "Ground item cell index references invalid state")
    end
  end
  local hazard_ids = {}
  for _, id in ipairs(self.hazard_order) do
    local hazard = self.hazards[id]
    assert(hazard and hazard.id == id, "Hazard order references an invalid hazard")
    assert(not hazard_ids[id], "Duplicate hazard ID '" .. id .. "'")
    hazard_ids[id] = true
    assert(Grid.in_bounds(hazard.x, hazard.y), "Hazard is outside world bounds")
    self.registry:get_hazard(hazard.definition_id)
    assert(type(hazard.active) == "boolean", "Hazard active state must be boolean")
    if hazard.active then
      assert(self:terrain_is_passable(hazard.x, hazard.y), "Active hazard is placed in impassable terrain")
      assert(not self:object_at(hazard.x, hazard.y), "Active hazard overlaps a world object")
    end
    local indexed = false
    for _, indexed_hazard in ipairs(self.hazards_by_cell[key(hazard.x, hazard.y)] or {}) do
      if indexed_hazard == hazard then
        indexed = true
        break
      end
    end
    assert(indexed, "Hazard cell index is invalid")
  end
  for id in pairs(self.hazards) do
    assert(hazard_ids[id], "Hazard is missing from deterministic hazard order")
  end
  for location_key, hazards in pairs(self.hazards_by_cell) do
    for _, hazard in ipairs(hazards) do
      assert(hazard_ids[hazard.id] and key(hazard.x, hazard.y) == location_key,
        "Hazard cell index references invalid state")
    end
  end
  assert(type(self.fire_tick) == "number" and self.fire_tick >= 0 and self.fire_tick % 1 == 0,
    "World fire tick must be a non-negative integer")
  assert(type(self.fire_ticking) == "boolean", "World fire update state must be boolean")
  local fire_ids, burning_targets = {}, {}
  for _, id in ipairs(self.fire_order) do
    local fire = self.fires[id]
    assert(fire and fire.id == id, "Fire order references an invalid fire")
    assert(not fire_ids[id], "Duplicate fire ID '" .. id .. "'")
    fire_ids[id] = true
    assert(type(fire.active) == "boolean", "Fire active state must be boolean")
    assert(type(fire.age) == "number" and fire.age >= 0 and fire.age % 1 == 0,
      "Fire age must be a non-negative integer")
    assert(type(fire.ready_tick) == "number" and fire.ready_tick >= 0 and fire.ready_tick % 1 == 0,
      "Fire ready tick must be a non-negative integer")
    assert(type(fire.provenance) == "table", "Fire provenance must be a table")
    assert(type(fire.target_key) == "string", "Fire target key must be a string")
    if fire.active then
      local target, code = self:resolve_fire_target(fire)
      assert(target, "Active fire has invalid target: " .. tostring(code))
      assert(target.target_key == fire.target_key, "Fire target key does not match target")
      assert(not burning_targets[fire.target_key], "Multiple active fires target '" .. fire.target_key .. "'")
      burning_targets[fire.target_key] = fire
      assert(self.fires_by_target[fire.target_key] == fire, "Fire target index is invalid")
    else
      assert(self.fires_by_target[fire.target_key] ~= fire, "Inactive fire cannot remain target-indexed")
    end
  end
  for id in pairs(self.fires) do
    assert(fire_ids[id], "Fire is missing from deterministic fire order")
  end
  for target_key, fire in pairs(self.fires_by_target) do
    assert(fire_ids[fire.id] and fire.active and fire.target_key == target_key,
      "Fire target index references invalid state")
  end
  return true
end

return World
