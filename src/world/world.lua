-- Authoritative terrain instances for one generated floor.  The generator's
-- boolean layout is only input; material and integrity state live here.
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

local function key(x, y)
  return Grid.key(x, y)
end

function World.new(registry, terrain, open_layout)
  local self = setmetatable({
    registry = registry,
    terrain = terrain,
    cells = {},
  }, World)
  local solid_material_id = SOLID_MATERIAL_BY_TERRAIN[terrain] or "material.terrain.stone"
  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do
      local material_id = open_layout[key(x, y)] and AIR or solid_material_id
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

function World:is_passable(x, y)
  local material = self:get_material(x, y)
  return material and not material.blocks_movement or false
end

function World:blocks_vision(x, y)
  local material = self:get_material(x, y)
  if not material then
    return true
  end
  return material.blocks_vision
end

function World:damage_terrain(x, y, spec)
  local cell = self:get_cell(x, y)
  if not cell then
    return { applied = false, code = "out_of_bounds", x = x, y = y, reason = "Terrain position is outside the world" }
  end
  local material = self.registry:get_material(cell.material_id)
  if not material.solid then
    return { applied = false, code = "not_solid", x = x, y = y, material_id = material.id, reason = "Terrain is already open" }
  end
  if not material.destructible then
    return { applied = false, code = "indestructible", x = x, y = y, material_id = material.id, reason = "Terrain material is indestructible" }
  end

  local previous_integrity = cell.current_integrity
  local new_integrity = math.max(0, previous_integrity - spec.amount)
  cell.current_integrity = new_integrity
  local result = {
    applied = new_integrity ~= previous_integrity,
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
    result.destroyed = true
    result.destroyed_material_id = cell.material_id
  end
  return result
end

function World:inspect_cell(x, y)
  local cell = self:get_cell(x, y)
  if not cell then
    return nil, "Terrain position is outside the world"
  end
  local material = self.registry:get_material(cell.material_id)
  return {
    x = x,
    y = y,
    material_id = material.id,
    passable = not material.blocks_movement,
    blocks_vision = material.blocks_vision,
    current_integrity = cell.current_integrity,
    max_integrity = material.max_integrity,
    destructible = material.destructible,
    destroyed = cell.destroyed,
    destroyed_from_material_id = cell.destroyed_from_material_id,
  }
end

function World:describe_cell(x, y)
  local inspected, reason = self:inspect_cell(x, y)
  if not inspected then
    return reason
  end
  local integrity = inspected.current_integrity and (inspected.current_integrity .. "/" .. inspected.max_integrity) or "n/a"
  return string.format("%d,%d %s passable=%s blocks_vision=%s integrity=%s destructible=%s destroyed=%s",
    x, y, inspected.material_id, tostring(inspected.passable), tostring(inspected.blocks_vision), integrity,
    tostring(inspected.destructible), tostring(inspected.destroyed))
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

function World:to_data()
  return {
    terrain = self.terrain,
    mutations = self:mutation_data(),
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
  return true
end

return World
