-- Instantiates authored Expedition chamber definitions into the existing
-- shared World grid. The chamber data owns board geometry; this module owns
-- only deterministic coordinate translation and safe spawn selection.
local Grid = require("src.world.grid")
local Vocabulary = require("src.expedition.vocabulary")

local Chambers = {}
Chambers.TOPOLOGIES = Vocabulary.sorted_keys(Vocabulary.TOPOLOGY_TAGS)

local function key(x, y) return Grid.key(x, y) end
local function copy_point(point) return { x = point.x, y = point.y, group = point.group } end

local function interior_bounds(width, height)
  local min_x = math.floor((Grid.width - width) / 2)
  local min_y = math.floor((Grid.height - height) / 2)
  return { min_x = min_x, max_x = min_x + width - 1, min_y = min_y, max_y = min_y + height - 1, width = width, height = height }
end

local function translate(bounds, point)
  return { x = bounds.min_x + point.x - 1, y = bounds.min_y + point.y - 1, group = point.group }
end

local function nearest_open(layout, bounds, desired, occupied)
  local best, distance
  for y = bounds.min_y, bounds.max_y do
    for x = bounds.min_x, bounds.max_x do
      local id, candidate_distance = key(x, y), math.abs(x - desired.x) + math.abs(y - desired.y)
      if layout[id] and not (occupied and occupied[id])
        and (not best or candidate_distance < distance or (candidate_distance == distance and (y < best.y or (y == best.y and x < best.x)))) then
        best, distance = { x = x, y = y }, candidate_distance
      end
    end
  end
  return best
end

local function marker_sets(definition, bounds)
  local markers = {}
  for name, values in pairs(definition.markers or {}) do
    markers[name] = {}
    for _, point in ipairs(values) do markers[name][#markers[name] + 1] = translate(bounds, point) end
  end
  return markers
end

-- Converts semantic tiles to normal World inputs. The renderer maps the same
-- semantics downstream through Loveable Rogue presentation bindings.
function Chambers.instantiate(definition)
  assert(type(definition) == "table" and definition.width and definition.height, "Expedition chamber definition is required")
  local bounds, layout, material_layout = interior_bounds(definition.width, definition.height), {}, {}
  local features = { liquids = {}, hazards = {}, gases = {}, fires = {}, doors = {}, topology = definition.topology_tags[1], tiles = {} }
  for local_y, row in ipairs(definition.tiles) do
    for local_x = 1, #row do
      local semantic = definition.tile_legend[row:sub(local_x, local_x)]
      local tile = assert(Vocabulary.TILES[semantic], "unknown authored Expedition tile")
      local x, y = bounds.min_x + local_x - 1, bounds.min_y + local_y - 1
      local id = key(x, y)
      -- Closed doors require passable terrain below their blocking world
      -- object; all other wall-like semantics remain terrain structures.
      if tile.walkable or semantic == "door.closed" then layout[id] = true end
      if tile.material then material_layout[id] = tile.material end
      features.tiles[id] = semantic
      if semantic == "water" then
        material_layout[id] = "material.floor.conductive_metal"
        features.liquids[#features.liquids + 1] = { x = x, y = y }
      elseif semantic == "gas" then
        features.gases[#features.gases + 1] = { x = x, y = y, gas_id = "gas.toxic.legacy", concentration = 1 }
      elseif semantic == "spikes" then
        features.hazards[#features.hazards + 1] = { x = x, y = y, definition_id = tile.hazard }
      elseif semantic == "fire" then
        features.fires[#features.fires + 1] = { x = x, y = y }
      elseif semantic == "door.closed" then
        features.doors[#features.doors + 1] = { x = x, y = y, definition_id = "world_object.door.powered_legacy" }
      end
    end
  end
  local markers = marker_sets(definition, bounds)
  local player_spawn = assert(markers.player_spawn and markers.player_spawn[1], "authored chamber lacks player marker")
  local cell_count = 0; for _ in pairs(layout) do cell_count = cell_count + 1 end
  return {
    id = definition.id, definition = definition, topology = definition.topology_tags[1], topology_tags = definition.topology_tags,
    bounds = bounds, layout = layout, material_layout = material_layout, features = features,
    markers = markers, player_spawn = player_spawn, cell_count = cell_count,
  }
end

-- Compatibility alias for callers that used the original generator. New code
-- must pass an authored definition; there is intentionally no topology-only
-- procedural fallback.
function Chambers.generate(options)
  assert(options and options.definition, "Expedition chambers must be instantiated from authored content definitions")
  return Chambers.instantiate(options.definition)
end

function Chambers.spawn_point(chamber, group, ordinal, occupied)
  local candidates = {}
  for _, marker in ipairs(chamber.markers.enemy_spawns or {}) do
    if not group or marker.group == group then candidates[#candidates + 1] = marker end
  end
  if #candidates == 0 then for _, marker in ipairs(chamber.markers.enemy_spawns or {}) do candidates[#candidates + 1] = marker end end
  table.sort(candidates, function(a, b) return a.y == b.y and a.x < b.x or a.y < b.y end)
  if #candidates > 0 then
    local preferred = candidates[((ordinal or 1) - 1) % #candidates + 1]
    local point = nearest_open(chamber.layout, chamber.bounds, preferred, occupied)
    if point then return point end
  end
  local center = { x = math.floor((chamber.bounds.min_x + chamber.bounds.max_x) / 2), y = math.floor((chamber.bounds.min_y + chamber.bounds.max_y) / 2) }
  return assert(nearest_open(chamber.layout, chamber.bounds, center, occupied), "chamber has no spawn point")
end

function Chambers.marker(chamber, kind, ordinal)
  local point = (chamber.markers[kind] or {})[ordinal or 1]
  return point and copy_point(point) or nil
end

function Chambers.contains(bounds, x, y)
  return x >= bounds.min_x and x <= bounds.max_x and y >= bounds.min_y and y <= bounds.max_y
end

function Chambers.visible_tiles(bounds, boss)
  return boss and 16 or math.max(14, math.min(16, bounds.width + 2))
end

return Chambers
