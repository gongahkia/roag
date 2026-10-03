-- OW-02's deliberately small macro-world contract.  A surface zone remains
-- an ordinary 2D World; this module owns only the finite z=0 address space
-- and the deterministic physical openings shared by neighbouring zones.
local Rng = require("src.rng")
local Grid = require("src.world.grid")
local ZoneKey = require("src.campaign.zone_key")

local SurfaceWorld = {
  MIN_COORDINATE = -4,
  MAX_COORDINATE = 4,
  SURFACE_Z = 0,
  PROFILE_ID = "zone_profile.legacy.forest",
}

-- Keep this convention in one place: north is y - 1, east is x + 1.
SurfaceWorld.DIRECTIONS = {
  north = { dx = 0, dy = -1, opposite = "south" },
  east = { dx = 1, dy = 0, opposite = "west" },
  south = { dx = 0, dy = 1, opposite = "north" },
  west = { dx = -1, dy = 0, opposite = "east" },
}
SurfaceWorld.DIRECTION_ORDER = { "north", "east", "south", "west" }

function SurfaceWorld.is_zone_in_bounds(key)
  ZoneKey.validate(key)
  return key.z == SurfaceWorld.SURFACE_Z
    and key.world_x >= SurfaceWorld.MIN_COORDINATE and key.world_x <= SurfaceWorld.MAX_COORDINATE
    and key.world_y >= SurfaceWorld.MIN_COORDINATE and key.world_y <= SurfaceWorld.MAX_COORDINATE
end

function SurfaceWorld.neighbor(key, direction)
  ZoneKey.validate(key)
  local delta = SurfaceWorld.DIRECTIONS[direction]
  assert(delta, "Unknown surface direction '" .. tostring(direction) .. "'")
  return ZoneKey.new(key.world_x + delta.dx, key.world_y + delta.dy, key.z)
end

function SurfaceWorld.opposite(direction)
  local delta = SurfaceWorld.DIRECTIONS[direction]
  assert(delta, "Unknown surface direction '" .. tostring(direction) .. "'")
  return delta.opposite
end

function SurfaceWorld.canonical_edge_id(first, second)
  ZoneKey.validate(first)
  ZoneKey.validate(second)
  assert(first.z == SurfaceWorld.SURFACE_Z and second.z == SurfaceWorld.SURFACE_Z,
    "Surface edge keys must be at z=0")
  local a, b = ZoneKey.encode(first), ZoneKey.encode(second)
  assert(a ~= b, "A surface edge requires two different zones")
  if b < a then a, b = b, a end
  return "edge:" .. a .. "|" .. b
end

local function edge_offset(campaign_seed, edge_id, direction)
  -- A six-cell margin leaves room for the existing containment geometry and
  -- avoids corners/visually awkward one-cell openings.  The coordinate is
  -- chosen from the canonical edge, never from either zone's generation RNG.
  local minimum, maximum
  if direction == "north" or direction == "south" then
    minimum, maximum = 6, Grid.width - 7
  else
    minimum, maximum = 6, Grid.height - 7
  end
  return Rng.new(campaign_seed):derive(edge_id .. ".connector"):int(minimum, maximum)
end

local function endpoint_cells(direction, offset)
  if direction == "north" then
    -- Grid's legacy render-space Y axis is inverted relative to ZoneKey's
    -- world-space convention: pressing north increases local Y. ZoneKey
    -- still follows north = world_y - 1 everywhere at macro level.
    return { x = offset, y = Grid.height - 1 }, { x = offset, y = Grid.height - 2 }
  elseif direction == "east" then
    return { x = Grid.width - 1, y = offset }, { x = Grid.width - 2, y = offset }
  elseif direction == "south" then
    return { x = offset, y = 0 }, { x = offset, y = 1 }
  end
  return { x = 0, y = offset }, { x = 1, y = offset }
end

local function plain_connection(connection)
  return {
    id = connection.id,
    direction = connection.direction,
    destination = ZoneKey.to_data(connection.destination),
    boundary = { x = connection.boundary.x, y = connection.boundary.y },
    interior = { x = connection.interior.x, y = connection.interior.y },
  }
end

function SurfaceWorld.connection(campaign_seed, key, direction)
  ZoneKey.validate(key)
  local destination = SurfaceWorld.neighbor(key, direction)
  if not SurfaceWorld.is_zone_in_bounds(key) or not SurfaceWorld.is_zone_in_bounds(destination) then
    return nil, { code = "world_boundary", reason = "No surface zone exists beyond this world boundary" }
  end
  local id = SurfaceWorld.canonical_edge_id(key, destination)
  local offset = edge_offset(campaign_seed, id, direction)
  local boundary, interior = endpoint_cells(direction, offset)
  return {
    id = id,
    direction = direction,
    destination = destination,
    boundary = boundary,
    interior = interior,
  }
end

function SurfaceWorld.connections(campaign_seed, key)
  ZoneKey.validate(key)
  local result = {}
  if not SurfaceWorld.is_zone_in_bounds(key) then return result end
  for _, direction in ipairs(SurfaceWorld.DIRECTION_ORDER) do
    local connection = SurfaceWorld.connection(campaign_seed, key, direction)
    if connection then result[direction] = plain_connection(connection) end
  end
  return result
end

function SurfaceWorld.connection_from_data(data)
  assert(type(data) == "table" and type(data.id) == "string", "Surface connection is invalid")
  -- Compatibility helpers remain useful to existing surface diagnostics even
  -- when a ZoneRecord now also exposes an interior up/down landmark. They use
  -- the same local passability/reachability contract, not cell-level Z.
  local vertical = data.direction == "up" or data.direction == "down"
  assert(SurfaceWorld.DIRECTIONS[data.direction] or vertical, "Surface connection direction is invalid")
  assert(type(data.boundary or data.cell) == "table" and type(data.interior or data.cell) == "table", "Surface connection cells are invalid")
  local destination = ZoneKey.from_data(data.destination)
  local boundary_source, interior_source = data.boundary or data.cell, data.interior or data.cell
  local boundary = { x = boundary_source.x, y = boundary_source.y }
  local interior = { x = interior_source.x, y = interior_source.y }
  assert(Grid.in_bounds(boundary.x, boundary.y) and Grid.in_bounds(interior.x, interior.y),
    "Surface connection cells are out of bounds")
  return { id = data.id, direction = data.direction, destination = destination, boundary = boundary, interior = interior }
end

function SurfaceWorld.connection_at(record, direction)
  local data = record and record.connections and record.connections[direction]
  return data and SurfaceWorld.connection_from_data(data) or nil
end

local function cell_key(cell)
  return Grid.key(cell.x, cell.y)
end

local function relocation_candidates(cell, maximum_radius)
  local candidates = {}
  for radius = 1, maximum_radius do
    for y = cell.y - radius, cell.y + radius do
      for x = cell.x - radius, cell.x + radius do
        if Grid.in_bounds(x, y) and math.max(math.abs(x - cell.x), math.abs(y - cell.y)) == radius then
          candidates[#candidates + 1] = { x = x, y = y }
        end
      end
    end
  end
  return candidates
end

local function choose_nearest_open(session, start)
  -- The connector must meet the player's primary generated region, not merely
  -- the closest isolated passable pocket. Session already owns the same
  -- deterministic passability BFS used by generation diagnostics.
  local candidates = session:_reachable_floor_cells()
  table.sort(candidates, function(a, b)
    local ad = math.abs(a.x - start.x) + math.abs(a.y - start.y)
    local bd = math.abs(b.x - start.x) + math.abs(b.y - start.y)
    if ad ~= bd then return ad < bd end
    if a.y ~= b.y then return a.y < b.y end
    return a.x < b.x
  end)
  return candidates[1]
end

local function carve_cell(world, cell)
  local current = world:get_cell(cell.x, cell.y)
  assert(current, "Surface connector attempted an out-of-bounds carve")
  if world:terrain_is_passable(cell.x, cell.y) then return true end
  local material = world:get_material(cell.x, cell.y)
  local result = world:damage_terrain(cell.x, cell.y, {
    amount = material.max_integrity,
    cause = "surface_connection",
    source = "surface_connection",
  })
  assert(result.applied and world:terrain_is_passable(cell.x, cell.y), "Surface connector terrain cannot be carved")
  return true
end

local function clear_blocker(session, cell, reserved)
  local world = session.state.world
  reserved = reserved or {}
  local object = world:object_at(cell.x, cell.y)
  if object then
    -- A later vertical throat may meet an already-installed reciprocal
    -- landmark in a dense authored interior.  The landmark is itself
    -- passable, so it is a valid piece of the shared approach; moving it
    -- would desynchronise the durable connection metadata from the physical
    -- object and make the zone impossible to validate or traverse.
    if object.zone_connection_id then return end
    -- Existing generation has no reservation input.  Preserve rather than
    -- erase generated content by moving an incidental blocker to the first
    -- deterministic adjacent legal tile. This is only a post-generation
    -- safety seam until generators consume connector reservations directly.
    for _, candidate in ipairs(relocation_candidates(cell, 4)) do
      if Grid.in_bounds(candidate.x, candidate.y) and world:is_passable(candidate.x, candidate.y)
        and not world:object_at(candidate.x, candidate.y) and not world:is_hazardous(candidate.x, candidate.y)
        and not reserved[cell_key(candidate)] then
        assert(world:move_object(object, candidate.x, candidate.y).applied)
        break
      end
    end
    assert(not world:object_at(cell.x, cell.y), "Surface connector blocker cannot be relocated")
  end
  -- Vertical landmarks are placed after ordinary generation. A generated
  -- hazard at the reserved landmark would make the physical object invalid,
  -- so retain its historical entry but deactivate it deterministically.
  for _, hazard in ipairs(world:hazards_at(cell.x, cell.y, true)) do
    hazard.active = false
  end
  for _, collection in ipairs({ session.state.targets, session.state.enemies, session.state.corpses,
    session.state.bullets, session.state.bombs, session.state.flares, session.state.torches }) do
    for _, actor in ipairs(collection or {}) do
      if actor.x == cell.x and actor.y == cell.y then
        -- Connector creation happens only in a fresh generated zone. Move
        -- dynamic occupants into the first deterministic nearby legal cell.
        local moved = false
        for _, candidate in ipairs(relocation_candidates(cell, 4)) do
          if Grid.in_bounds(candidate.x, candidate.y) and world:is_passable(candidate.x, candidate.y)
            and not world:is_hazardous(candidate.x, candidate.y)
            and not session:_actor_at(candidate.x, candidate.y, actor) and not world:object_at(candidate.x, candidate.y)
            and not reserved[cell_key(candidate)] then
            actor.x, actor.y, moved = candidate.x, candidate.y, true
            break
          end
        end
        assert(moved, "Surface connector occupant cannot be relocated")
      end
    end
  end
end

local function carve_path(world, from, to)
  local x, y = from.x, from.y
  carve_cell(world, { x = x, y = y })
  while x ~= to.x do
    x = x + (to.x > x and 1 or -1)
    carve_cell(world, { x = x, y = y })
  end
  while y ~= to.y do
    y = y + (to.y > y and 1 or -1)
    carve_cell(world, { x = x, y = y })
  end
end

function SurfaceWorld.carve_connections(session, connections)
  local world = assert(session and session.state and session.state.world, "Surface connector requires a World")
  local reserved = {}
  for _, direction in ipairs(SurfaceWorld.DIRECTION_ORDER) do
    local data = connections and connections[direction]
    if data then
      local connection = SurfaceWorld.connection_from_data(data)
      local nearest = choose_nearest_open(session, connection.interior)
      assert(nearest, "Surface world generated no traversable terrain")
      carve_cell(world, connection.boundary)
      carve_path(world, connection.interior, nearest)
      -- Clear only after the physical path exists, so relocation has legal
      -- candidates. The connector's primary-region guarantee is then the
      -- actual World passability graph, not travel metadata.
      local x, y = connection.boundary.x, connection.boundary.y
      while true do
        local throat_cell = { x = x, y = y }
        reserved[cell_key(throat_cell)] = true
        clear_blocker(session, throat_cell, reserved)
        if x == nearest.x and y == nearest.y then break end
        if x ~= nearest.x then x = x + (nearest.x > x and 1 or -1)
        else y = y + (nearest.y > y and 1 or -1) end
      end
    end
  end
  session.state.surface_connector_cells = reserved
  session:validate_world()
  return reserved
end

-- Reusable deterministic seam for an interior landmark (cave mouth, stairs,
-- ladder, elevator, shaft). It uses the exact same path/material APIs as a
-- surface edge and reserves the entire throat against normal placement.
function SurfaceWorld.reserve_interior_connection(session, cell, reserved)
  local world = assert(session and session.state and session.state.world, "Zone connector requires a World")
  assert(type(cell) == "table" and Grid.in_bounds(cell.x, cell.y), "Zone connector cell is out of bounds")
  reserved = reserved or session.state.surface_connector_cells or {}
  local nearest = choose_nearest_open(session, cell)
  assert(nearest, "Zone generated no traversable terrain")
  carve_path(world, cell, nearest)
  local x, y = cell.x, cell.y
  while true do
    local throat_cell = { x = x, y = y }
    reserved[cell_key(throat_cell)] = true
    clear_blocker(session, throat_cell, reserved)
    if x == nearest.x and y == nearest.y then break end
    if x ~= nearest.x then x = x + (nearest.x > x and 1 or -1)
    else y = y + (nearest.y > y and 1 or -1) end
  end
  session.state.surface_connector_cells = reserved
  session:validate_world()
  return reserved
end

function SurfaceWorld.reachable_from_connection(world, connection)
  connection = SurfaceWorld.connection_from_data(connection)
  local start = connection.interior
  if not world:is_passable(start.x, start.y) then return false end
  local visited, queue, cursor = { [Grid.key(start.x, start.y)] = true }, { start }, 1
  while queue[cursor] do
    local current = queue[cursor]
    cursor = cursor + 1
    for _, neighbour in ipairs(Grid.neighbours(current)) do
      local id = Grid.key(neighbour.x, neighbour.y)
      if Grid.in_bounds(neighbour.x, neighbour.y) and world:is_passable(neighbour.x, neighbour.y) and not visited[id] then
        visited[id] = true
        queue[#queue + 1] = neighbour
      end
    end
  end
  return #queue > 2
end

function SurfaceWorld.connection_reaches_primary(session, connection)
  connection = SurfaceWorld.connection_from_data(connection)
  for _, cell in ipairs(session:_reachable_floor_cells()) do
    if cell.x == connection.interior.x and cell.y == connection.interior.y then return true end
  end
  return false
end

function SurfaceWorld.profile_for(key)
  assert(SurfaceWorld.is_zone_in_bounds(key), "Surface profile requested outside finite z=0 world")
  return SurfaceWorld.PROFILE_ID
end

return SurfaceWorld
