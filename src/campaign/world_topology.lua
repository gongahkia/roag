-- Campaign-scale topology.  A ZoneKey selects one ordinary two-dimensional
-- simulation; this module deliberately never gives cells a Z coordinate.
-- It expands OW-02's shared surface edges with deterministic, reciprocal
-- vertical landmarks while retaining the existing surface edge format.
local Rng = require("src.rng")
local Grid = require("src.world.grid")
local ZoneKey = require("src.campaign.zone_key")
local SurfaceWorld = require("src.campaign.surface_world")

local WorldTopology = {
  MIN_COORDINATE = SurfaceWorld.MIN_COORDINATE,
  MAX_COORDINATE = SurfaceWorld.MAX_COORDINATE,
  MIN_Z = -2,
  MAX_Z = 1,
  SURFACE_Z = 0,
  CAVE_RATE_PERCENT = 28,
  DEEP_CAVE_RATE_PERCENT = 32,
  PROFILE_SURFACE = "zone_profile.legacy.forest",
  PROFILE_CAVE = "zone_profile.legacy.cave",
  PROFILE_DEEP_CAVE = "zone_profile.legacy.deep_cave",
}

-- World-space convention remains north = y - 1, east = x + 1.  Up/down
-- change only the ZoneKey, never an x/y cell inside a World.
WorldTopology.DIRECTIONS = {
  north = { dx = 0, dy = -1, dz = 0, opposite = "south" },
  east = { dx = 1, dy = 0, dz = 0, opposite = "west" },
  south = { dx = 0, dy = 1, dz = 0, opposite = "north" },
  west = { dx = -1, dy = 0, dz = 0, opposite = "east" },
  up = { dx = 0, dy = 0, dz = 1, opposite = "down" },
  down = { dx = 0, dy = 0, dz = -1, opposite = "up" },
}
WorldTopology.HORIZONTAL_DIRECTIONS = { "north", "east", "south", "west" }
WorldTopology.VERTICAL_DIRECTIONS = { "up", "down" }
WorldTopology.DIRECTION_ORDER = { "north", "east", "south", "west", "up", "down" }
WorldTopology.CONNECTION_TYPES = {
  cave_mouth = true,
  stairs = true,
  ladder = true,
  elevator = true,
  shaft = true,
  cardinal = true,
}

local function copy_cell(cell)
  return { x = cell.x, y = cell.y }
end

local function sorted_endpoint_id(first, second)
  local a, b = ZoneKey.encode(first), ZoneKey.encode(second)
  if b < a then a, b = b, a end
  return a, b
end

function WorldTopology.is_zone_in_bounds(key)
  ZoneKey.validate(key)
  return key.world_x >= WorldTopology.MIN_COORDINATE and key.world_x <= WorldTopology.MAX_COORDINATE
    and key.world_y >= WorldTopology.MIN_COORDINATE and key.world_y <= WorldTopology.MAX_COORDINATE
    and key.z >= WorldTopology.MIN_Z and key.z <= WorldTopology.MAX_Z
end

function WorldTopology.neighbor(key, direction)
  ZoneKey.validate(key)
  local delta = assert(WorldTopology.DIRECTIONS[direction], "Unknown zone direction '" .. tostring(direction) .. "'")
  return ZoneKey.new(key.world_x + delta.dx, key.world_y + delta.dy, key.z + delta.dz)
end

function WorldTopology.opposite(direction)
  local delta = assert(WorldTopology.DIRECTIONS[direction], "Unknown zone direction '" .. tostring(direction) .. "'")
  return delta.opposite
end

function WorldTopology.is_vertical(direction)
  return direction == "up" or direction == "down"
end

function WorldTopology.canonical_vertical_id(first, second, connection_type)
  ZoneKey.validate(first)
  ZoneKey.validate(second)
  assert(WorldTopology.CONNECTION_TYPES[connection_type] and connection_type ~= "cardinal",
    "Vertical connection type is invalid")
  assert(first.world_x == second.world_x and first.world_y == second.world_y and math.abs(first.z - second.z) == 1,
    "Vertical connection endpoints must be adjacent in one world column")
  local a, b = sorted_endpoint_id(first, second)
  return "vconn:" .. a .. "|" .. b .. ":" .. connection_type
end

local function column_rng(seed, x, y, stream)
  return Rng.new(seed):derive(string.format("vertical.column:%d:%d:%s", x, y, stream))
end

-- The starting column is intentionally a guaranteed vertical fixture. It
-- makes the first campaign playable as surface -> cave -> deep cave while
-- the rest of the finite surface remains sparse and deterministic.
function WorldTopology.has_surface_cave(seed, x, y)
  if x == 0 and y == 0 then return true end
  return column_rng(seed, x, y, "cave_mouth"):int(1, 100) <= WorldTopology.CAVE_RATE_PERCENT
end

function WorldTopology.has_deep_cave(seed, x, y)
  if not WorldTopology.has_surface_cave(seed, x, y) then return false end
  if x == 0 and y == 0 then return true end
  return column_rng(seed, x, y, "deep_cave"):int(1, 100) <= WorldTopology.DEEP_CAVE_RATE_PERCENT
end

function WorldTopology.profile_for(key, world_content_plan)
  assert(WorldTopology.is_zone_in_bounds(key), "Zone profile requested outside campaign bounds")
  -- The persisted campaign content plan may reserve a column for an authored
  -- interior.  It is an optional argument so legacy runs and OW-01..05 test
  -- fixtures retain their original topology exactly.
  if world_content_plan and world_content_plan.zone_profiles then
    local planned = world_content_plan.zone_profiles[ZoneKey.encode(key)]
    if planned then return planned end
  end
  if key.z == 0 then return WorldTopology.PROFILE_SURFACE end
  if key.z == -1 then return WorldTopology.PROFILE_CAVE end
  if key.z == -2 then return WorldTopology.PROFILE_DEEP_CAVE end
  -- +1 is a supported address for authored towers/rooftops later. There is
  -- deliberately no ordinary production classification there yet.
  return WorldTopology.PROFILE_SURFACE
end

local function planned_vertical_link(world_content_plan, key, direction)
  if not world_content_plan then return nil end
  local encoded = ZoneKey.encode(key)
  for _, link in ipairs(world_content_plan.vertical_links or {}) do
    if ZoneKey.encode(ZoneKey.from_data(link.source)) == encoded and direction == "down" then return link, true end
    if ZoneKey.encode(ZoneKey.from_data(link.destination)) == encoded and direction == "up" then return link, false end
  end
  return nil
end

local function planned_column(world_content_plan, key)
  if not world_content_plan then return false end
  for _, link in ipairs(world_content_plan.vertical_links or {}) do
    local source = ZoneKey.from_data(link.source)
    if source.world_x == key.world_x and source.world_y == key.world_y then return true end
  end
  return false
end

local function vertical_type(seed, key, direction, world_content_plan)
  local link = planned_vertical_link(world_content_plan, key, direction)
  if link then return link.connection_type, link end
  -- Authored structure columns own their complete vertical topology. Suppress
  -- ordinary cave mouths there so an entrance can never have competing DOWN
  -- destinations.
  if planned_column(world_content_plan, key) then return nil end
  if direction == "down" and key.z == 0 and WorldTopology.has_surface_cave(seed, key.world_x, key.world_y) then
    return "cave_mouth"
  elseif direction == "up" and key.z == -1 and WorldTopology.has_surface_cave(seed, key.world_x, key.world_y) then
    return "cave_mouth"
  elseif direction == "down" and key.z == -1 and WorldTopology.has_deep_cave(seed, key.world_x, key.world_y) then
    -- A laddered shaft is bidirectional in v1, so it cannot generate an
    -- accidental irreversible drop.
    return "shaft"
  elseif direction == "up" and key.z == -2 and WorldTopology.has_deep_cave(seed, key.world_x, key.world_y) then
    return "shaft"
  end
  return nil
end

local function vertical_endpoint_candidate(seed, connection_id, key, direction, attempt)
  -- A broad interior margin leaves an ordinary throat around the landmark and
  -- makes placement independent of cardinal edge geometry.
  local rng = Rng.new(seed):derive(connection_id .. ".endpoint." .. ZoneKey.encode(key) .. "." .. direction .. "." .. attempt)
  return { x = rng:int(7, Grid.width - 8), y = rng:int(7, Grid.height - 8) }
end

local function vertical_specs(seed, key, world_content_plan)
  local specs = {}
  for _, direction in ipairs(WorldTopology.VERTICAL_DIRECTIONS) do
    local kind = vertical_type(seed, key, direction, world_content_plan)
    if kind then
      local destination = WorldTopology.neighbor(key, direction)
      specs[#specs + 1] = {
        id = WorldTopology.canonical_vertical_id(key, destination, kind),
        direction = direction,
        connection_type = kind,
      }
    end
  end
  table.sort(specs, function(left, right)
    if left.id ~= right.id then return left.id < right.id end
    return left.direction < right.direction
  end)
  return specs
end

local function too_close(left, right)
  -- Interactions are adjacent (including diagonals), so leave more than one
  -- cell between physical landmarks. This prevents an explicit U beside one
  -- link from ambiguously selecting another link in the same cave.
  return math.max(math.abs(left.x - right.x), math.abs(left.y - right.y)) <= 2
end

local function vertical_endpoint(seed, connection_id, key, direction, world_content_plan)
  -- Existing boss arenas intentionally use hard containment walls around a
  -- compact authored playfield. Their reciprocal campaign exit therefore has
  -- a fixed, visible cell inside that playfield instead of attempting to
  -- carve an indestructible arena boundary.
  if world_content_plan and world_content_plan.zone_profiles
    and world_content_plan.zone_profiles[ZoneKey.encode(key)] == "zone_profile.world.boss_lair" then
    return { x = 5, y = 9 }
  end
  local allocated = {}
  for _, spec in ipairs(vertical_specs(seed, key, world_content_plan)) do
    local selected
    for attempt = 1, 128 do
      local candidate = vertical_endpoint_candidate(seed, spec.id, key, spec.direction, attempt)
      local legal = true
      for _, occupied in ipairs(allocated) do
        if too_close(candidate, occupied.cell) then legal = false break end
      end
      -- At z=0, keep an interior vertical landmark away from deterministic
      -- cardinal edge openings and their immediate throat.
      if legal and key.z == 0 then
        for _, cardinal_direction in ipairs(WorldTopology.HORIZONTAL_DIRECTIONS) do
          local cardinal = SurfaceWorld.connection(seed, key, cardinal_direction)
          if cardinal and (too_close(candidate, cardinal.boundary) or too_close(candidate, cardinal.interior)) then
            legal = false
            break
          end
        end
      end
      if legal then selected = candidate break end
    end
    assert(selected, "Unable to allocate a separated vertical endpoint")
    allocated[#allocated + 1] = { id = spec.id, direction = spec.direction, cell = selected }
    if spec.id == connection_id and spec.direction == direction then return selected end
  end
  -- Generic authored/test links are not part of production cave metadata.
  -- They retain a stable endpoint without affecting real generated topology.
  return vertical_endpoint_candidate(seed, connection_id, key, direction, 1)
end

function WorldTopology.object_definition_for(connection_type, connection)
  if connection and connection.object_definition_id then return connection.object_definition_id end
  local definitions = {
    cave_mouth = "world_object.connection.cave_mouth",
    stairs = "world_object.connection.stairs",
    ladder = "world_object.connection.ladder",
    elevator = "world_object.connection.elevator",
    shaft = "world_object.connection.shaft",
  }
  return assert(definitions[connection_type], "No world object for connection type '" .. tostring(connection_type) .. "'")
end

function WorldTopology.presentation_label(connection)
  if connection.presentation_label then return connection.presentation_label end
  local direction, kind = connection.direction, connection.connection_type
  if kind == "cave_mouth" then
    return direction == "down" and "DESCEND INTO CAVE" or "ASCEND TO SURFACE"
  elseif kind == "stairs" then
    return "USE STAIRS"
  elseif kind == "ladder" then
    return "USE LADDER"
  elseif kind == "elevator" then
    return "USE ELEVATOR"
  elseif kind == "shaft" then
    return direction == "down" and "DESCEND SHAFT" or "CLIMB SHAFT"
  end
  return "TRAVEL"
end

function WorldTopology.generic_vertical_connection(seed, key, direction, connection_type, options)
  options = options or {}
  ZoneKey.validate(key)
  assert(direction == "up" or direction == "down", "Generic vertical connections require up or down")
  assert(WorldTopology.CONNECTION_TYPES[connection_type] and connection_type ~= "cardinal",
    "Generic vertical connection type is invalid")
  local destination = WorldTopology.neighbor(key, direction)
  if not WorldTopology.is_zone_in_bounds(key) or not WorldTopology.is_zone_in_bounds(destination) then
    return nil, { code = "world_boundary", reason = "No zone exists beyond this vertical world boundary" }
  end
  local id = WorldTopology.canonical_vertical_id(key, destination, connection_type)
  local cell = vertical_endpoint(seed, id, key, direction, options.world_content_plan)
  local destination_cell = vertical_endpoint(seed, id, destination, WorldTopology.opposite(direction), options.world_content_plan)
  return {
    id = id,
    connection_id = id,
    connection_type = connection_type,
    direction = direction,
    reciprocal_direction = WorldTopology.opposite(direction),
    source = ZoneKey.to_data(key),
    destination = ZoneKey.to_data(destination),
    source_cell = cell,
    destination_cell = destination_cell,
    -- `cell` is the local physical landmark. `interior` is retained as a
    -- common arrival field so the staged OW-02 transaction stays shared.
    cell = copy_cell(cell),
    interior = copy_cell(cell),
    boundary = copy_cell(cell),
    bidirectional = true,
    presentation_role = options.presentation_role or connection_type,
    presentation_label = options.presentation_label,
    object_definition_id = options.object_definition_id,
  }
end

local function horizontal_connection(seed, key, direction)
  local legacy, error_data = SurfaceWorld.connection(seed, key, direction)
  if not legacy then return nil, error_data end
  return {
    id = legacy.id,
    connection_id = legacy.id,
    connection_type = "cardinal",
    direction = direction,
    reciprocal_direction = WorldTopology.opposite(direction),
    source = ZoneKey.to_data(key),
    destination = ZoneKey.to_data(legacy.destination),
    source_cell = copy_cell(legacy.boundary),
    destination_cell = copy_cell(legacy.interior),
    boundary = copy_cell(legacy.boundary),
    interior = copy_cell(legacy.interior),
    bidirectional = true,
    presentation_role = "surface_edge",
  }
end

function WorldTopology.connection(seed, key, direction, world_content_plan)
  ZoneKey.validate(key)
  assert(WorldTopology.DIRECTIONS[direction], "Unknown zone direction '" .. tostring(direction) .. "'")
  if not WorldTopology.is_zone_in_bounds(key) then
    return nil, { code = "world_boundary", reason = "Zone is outside campaign world bounds" }
  end
  if not WorldTopology.is_vertical(direction) then
    if key.z ~= 0 then return nil, { code = "no_zone_connection", reason = "No horizontal connection exists at this level" } end
    return horizontal_connection(seed, key, direction)
  end
  local kind, link = vertical_type(seed, key, direction, world_content_plan)
  if not kind then
    local destination = WorldTopology.neighbor(key, direction)
    return nil, {
      code = WorldTopology.is_zone_in_bounds(destination) and "no_zone_connection" or "world_boundary",
      reason = "No vertical connection exists here",
    }
  end
  return WorldTopology.generic_vertical_connection(seed, key, direction, kind, {
    world_content_plan = world_content_plan,
    presentation_role = link and (link.presentation_role or kind) or kind,
    presentation_label = link and ((key.z > ZoneKey.from_data(link.destination).z) and link.source_label or link.destination_label) or nil,
    object_definition_id = link and ((key.z > ZoneKey.from_data(link.destination).z) and link.source_object_definition_id or link.destination_object_definition_id) or nil,
  })
end

function WorldTopology.connections(seed, key, world_content_plan)
  ZoneKey.validate(key)
  local result = {}
  if not WorldTopology.is_zone_in_bounds(key) then return result end
  for _, direction in ipairs(WorldTopology.DIRECTION_ORDER) do
    local connection = WorldTopology.connection(seed, key, direction, world_content_plan)
    if connection then result[direction] = connection end
  end
  return result
end

function WorldTopology.connection_from_data(data)
  assert(type(data) == "table" and type(data.id) == "string", "Zone connection is invalid")
  assert(WorldTopology.DIRECTIONS[data.direction], "Zone connection direction is invalid")
  local destination = ZoneKey.from_data(data.destination)
  local boundary = data.boundary or data.cell or data.source_cell
  local interior = data.interior or data.cell or data.source_cell
  assert(type(boundary) == "table" and type(interior) == "table", "Zone connection cells are invalid")
  assert(Grid.in_bounds(boundary.x, boundary.y) and Grid.in_bounds(interior.x, interior.y),
    "Zone connection cells are out of bounds")
  local kind = data.connection_type or "cardinal"
  assert(WorldTopology.CONNECTION_TYPES[kind], "Zone connection type is invalid")
  return {
    id = data.id,
    connection_id = data.connection_id or data.id,
    connection_type = kind,
    direction = data.direction,
    reciprocal_direction = data.reciprocal_direction or WorldTopology.opposite(data.direction),
    source = data.source and ZoneKey.to_data(ZoneKey.from_data(data.source)) or nil,
    destination = destination,
    source_cell = copy_cell(data.source_cell or boundary),
    destination_cell = data.destination_cell and copy_cell(data.destination_cell) or nil,
    boundary = copy_cell(boundary),
    interior = copy_cell(interior),
    cell = copy_cell(data.cell or boundary),
    bidirectional = data.bidirectional ~= false,
    presentation_role = data.presentation_role or kind,
    presentation_label = data.presentation_label,
    object_definition_id = data.object_definition_id,
  }
end

function WorldTopology.connection_at(record, direction)
  local data = record and record.connections and record.connections[direction]
  return data and WorldTopology.connection_from_data(data) or nil
end

function WorldTopology.connection_matches(seed, key, data, world_content_plan)
  local actual = WorldTopology.connection_from_data(data)
  local expected = WorldTopology.connection(seed, key, actual.direction, world_content_plan)
  if not expected then return false end
  return actual.id == expected.id and actual.connection_type == expected.connection_type
    and ZoneKey.equal(actual.destination, expected.destination)
    and actual.boundary.x == expected.boundary.x and actual.boundary.y == expected.boundary.y
    and actual.interior.x == expected.interior.x and actual.interior.y == expected.interior.y
    and actual.presentation_label == expected.presentation_label
end

return WorldTopology
