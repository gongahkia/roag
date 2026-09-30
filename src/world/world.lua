-- Authoritative terrain and environmental-object state for one floor. The
-- generator's boolean layout is construction input only; physical state lives
-- here and is queried by every simulation consumer.
local Grid = require("src.world.grid")

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

function World.new(registry, terrain, open_layout, sequence_owner)
  sequence_owner = sequence_owner or { next_world_object_sequence = 1, next_hazard_sequence = 1, next_fire_sequence = 1 }
  sequence_owner.next_world_object_sequence = sequence_owner.next_world_object_sequence or 1
  sequence_owner.next_hazard_sequence = sequence_owner.next_hazard_sequence or 1
  sequence_owner.next_fire_sequence = sequence_owner.next_fire_sequence or 1
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
    fires = {},
    fire_order = {},
    fires_by_target = {},
    fire_tick = 0,
    fire_ticking = false,
    sequence_owner = sequence_owner,
  }, World)
  local solid_material_id = SOLID_MATERIAL_BY_TERRAIN[terrain] or "material.terrain.stone"
  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do
      local material_id = open_layout[key(x, y)] and (OPEN_MATERIAL_BY_TERRAIN[terrain] or AIR) or solid_material_id
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

function World:_next_fire_id()
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

function World:_next_object_id()
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
  local id = options.id or self:_next_object_id()
  if self.objects[id] then
    return nil, { applied = false, code = "duplicate_id", reason = "World object ID already exists" }
  end
  local integrity = options.current_integrity or material.max_integrity
  if type(integrity) ~= "number" or integrity <= 0 or integrity > material.max_integrity then
    return nil, { applied = false, code = "invalid_integrity", reason = "World object integrity is invalid" }
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
    blocks_movement = definition.blocks_movement,
    blocks_vision = definition.blocks_vision,
    blocks_projectiles = definition.blocks_projectiles,
    movable_by_force = definition.movable_by_force,
  }
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
    object.blocks_movement = false
    object.blocks_vision = false
    object.blocks_projectiles = false
    self.objects_by_cell[key(object.x, object.y)] = nil
    self:_deactivate_target_fire("object", object.id, nil, nil, "target_destroyed")
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
    movable_by_force = object.movable_by_force,
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
    destroyed = cell.destroyed,
    destroyed_from_material_id = cell.destroyed_from_material_id,
    objects = objects,
    hazards = hazards,
    liquid = liquid_data,
    fires = fires,
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
  return string.format("%d,%d %s passable=%s blocks_vision=%s blocks_projectile=%s integrity=%s destructible=%s destroyed=%s objects=%s hazards=%s liquid=%s fires=%s",
    x, y, inspected.material_id, tostring(inspected.passable), tostring(inspected.blocks_vision),
    tostring(inspected.blocks_projectile), integrity, tostring(inspected.destructible), tostring(inspected.destroyed),
    table.concat(object_ids, ","), table.concat(hazard_ids, ","), liquid, table.concat(fire_ids, ","))
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

function World:object_data()
  local objects = {}
  for _, object in ipairs(self:list_objects(true)) do
    objects[#objects + 1] = copy_object(object)
  end
  return objects
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
    mutations = self:mutation_data(),
    objects = self:object_data(),
    hazards = self:hazard_data(),
    liquids = self:liquid_data(),
    liquid_tick = self.liquid_tick,
    fires = self:fire_data(),
    fire_tick = self.fire_tick,
  }
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
    if object.destroyed then
      assert(object.current_integrity == 0, "Destroyed world object integrity must be zero")
      assert(not object.blocks_movement and not object.blocks_vision and not object.blocks_projectiles,
        "Destroyed world object cannot retain blocking state")
      assert(self.objects_by_cell[location_key] ~= object, "Destroyed world object cannot occupy a cell")
    else
      assert(self:terrain_is_passable(object.x, object.y), "World object is placed in impassable terrain")
      assert(object.blocks_movement == definition.blocks_movement and object.blocks_vision == definition.blocks_vision
        and object.blocks_projectiles == definition.blocks_projectiles, "World object blocking state does not match definition")
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
