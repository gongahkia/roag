-- Deterministic compact Expedition boards.  A Chamber deliberately occupies a
-- small playable island inside the legacy fixed-size World so the combat
-- simulation, materials, Force, fire, electricity, and save helpers remain
-- shared with Sandbox.  Everything outside the bounds is reinforced wall and
-- therefore cannot become empty traversal space.
local Grid = require("src.world.grid")

local Chambers = {}

Chambers.TOPOLOGIES = {
  "open", "lane", "cross", "choke", "pinball", "pockets", "conductive", "volatile", "breakable",
}

local function key(x, y) return Grid.key(x, y) end

local function copy_cell(cell)
  return { x = cell.x, y = cell.y }
end

local function clamp(value, minimum, maximum)
  return math.max(minimum, math.min(maximum, value))
end

local function interior_bounds(width, height)
  local min_x = math.floor((Grid.width - width) / 2)
  local min_y = math.floor((Grid.height - height) / 2)
  return {
    min_x = min_x,
    max_x = min_x + width - 1,
    min_y = min_y,
    max_y = min_y + height - 1,
    width = width,
    height = height,
  }
end

local function point_in(bounds, x, y)
  return x >= bounds.min_x and x <= bounds.max_x and y >= bounds.min_y and y <= bounds.max_y
end

local function cardinal_neighbours(x, y)
  return { { x = x, y = y + 1 }, { x = x + 1, y = y }, { x = x, y = y - 1 }, { x = x - 1, y = y } }
end

local function reachable(layout, start)
  local seen, queue, head = {}, { copy_cell(start) }, 1
  seen[key(start.x, start.y)] = true
  while queue[head] do
    local current = queue[head]
    head = head + 1
    for _, neighbour in ipairs(cardinal_neighbours(current.x, current.y)) do
      local id = key(neighbour.x, neighbour.y)
      if layout[id] and not seen[id] then
        seen[id] = true
        queue[#queue + 1] = neighbour
      end
    end
  end
  return seen
end

local function open_cell(layout, x, y)
  if Grid.in_bounds(x, y) then layout[key(x, y)] = true end
end

local function close_cell(layout, x, y)
  if Grid.in_bounds(x, y) then layout[key(x, y)] = nil end
end

local function set_material(materials, x, y, material)
  if Grid.in_bounds(x, y) then materials[key(x, y)] = material end
end

local function choose_dimensions(topology, index)
  local sizes = {
    open = { { 11, 9 }, { 12, 9 }, { 12, 10 } },
    lane = { { 12, 8 }, { 13, 8 }, { 14, 9 } },
    cross = { { 11, 9 }, { 12, 9 } },
    choke = { { 10, 8 }, { 11, 9 } },
    pinball = { { 10, 8 }, { 11, 9 } },
    pockets = { { 12, 9 }, { 13, 9 } },
    conductive = { { 11, 9 }, { 12, 9 } },
    volatile = { { 10, 8 }, { 11, 9 } },
    breakable = { { 11, 9 }, { 12, 9 } },
    boss = { { 14, 10 } },
  }
  local selected = (sizes[topology] or sizes.open)[((index or 1) - 1) % #(sizes[topology] or sizes.open) + 1]
  return selected[1], selected[2]
end

local function add_open_rectangle(layout, bounds)
  for x = bounds.min_x, bounds.max_x do
    for y = bounds.min_y, bounds.max_y do open_cell(layout, x, y) end
  end
end

local function add_internal_wall(layout, materials, x, y, material)
  close_cell(layout, x, y)
  set_material(materials, x, y, material or "material.structure.reinforced")
end

local function feature_cells(bounds, topology, layout, materials)
  local center_x = math.floor((bounds.min_x + bounds.max_x) / 2)
  local center_y = math.floor((bounds.min_y + bounds.max_y) / 2)
  local features = { liquids = {}, hazards = {}, topology = topology }
  local function wall(x, y, material) add_internal_wall(layout, materials, x, y, material) end
  local function water(x, y)
    if layout[key(x, y)] then
      features.liquids[#features.liquids + 1] = { x = x, y = y }
      set_material(materials, x, y, "material.floor.conductive_metal")
    end
  end

  if topology == "lane" then
    -- Low side ribs leave an obvious central firing lane without making the
    -- chamber a corridor.  There is always room to step around a pursuer.
    for x = bounds.min_x + 2, bounds.max_x - 2, 3 do
      wall(x, bounds.min_y + 1)
      wall(x, bounds.max_y - 1)
    end
  elseif topology == "cross" then
    for y = bounds.min_y + 1, bounds.max_y - 1 do
      if y ~= center_y then wall(center_x - 2, y) end
    end
    for x = bounds.min_x + 1, bounds.max_x - 1 do
      if x ~= center_x then wall(x, center_y - 2) end
    end
  elseif topology == "choke" then
    for y = bounds.min_y, bounds.max_y do
      if y ~= center_y then wall(center_x, y, "material.structure.masonry") end
    end
  elseif topology == "pinball" then
    for _, point in ipairs({
      { center_x - 2, center_y - 1 }, { center_x + 2, center_y + 1 },
      { center_x - 1, center_y + 2 }, { center_x + 1, center_y - 2 },
    }) do wall(point[1], point[2], "material.structure.masonry") end
  elseif topology == "pockets" then
    for y = bounds.min_y + 1, bounds.max_y - 1 do
      if y ~= center_y + 1 then wall(center_x, y, "material.structure.masonry") end
    end
    for x = bounds.min_x + 2, bounds.max_x - 2 do
      if x ~= center_x - 1 then wall(x, center_y - 2, "material.structure.masonry") end
    end
  elseif topology == "conductive" then
    for offset = -2, 2 do
      water(center_x + offset, center_y)
      if offset ~= 0 then water(center_x, center_y + offset) end
    end
  elseif topology == "volatile" then
    for _, point in ipairs({
      { center_x - 2, center_y }, { center_x + 2, center_y },
      { center_x, center_y - 2 }, { center_x, center_y + 2 },
    }) do
      wall(point[1], point[2], "material.structure.wood")
    end
  elseif topology == "breakable" then
    for y = bounds.min_y + 1, bounds.max_y - 1 do
      if y ~= center_y then wall(center_x, y, "material.structure.masonry") end
    end
  elseif topology == "boss" then
    for _, point in ipairs({
      { center_x - 3, center_y - 2 }, { center_x - 3, center_y + 2 },
      { center_x + 3, center_y - 2 }, { center_x + 3, center_y + 2 },
    }) do wall(point[1], point[2], "material.structure.reinforced_panel") end
  end
  return features
end

local function nearest_open(layout, bounds, desired, occupied)
  local best, best_distance
  for x = bounds.min_x, bounds.max_x do
    for y = bounds.min_y, bounds.max_y do
      local id = key(x, y)
      if layout[id] and not (occupied and occupied[id]) then
        local distance = math.abs(x - desired.x) + math.abs(y - desired.y)
        if not best or distance < best_distance or (distance == best_distance and (y < best.y or (y == best.y and x < best.x))) then
          best, best_distance = { x = x, y = y }, distance
        end
      end
    end
  end
  return best
end

-- Returns a fully deterministic, connected board descriptor.  `layout` uses
-- the regular generator convention: true means passable terrain.
function Chambers.generate(options)
  options = options or {}
  local topology = options.topology or "open"
  local width, height = options.width or nil, options.height or nil
  if not width or not height then width, height = choose_dimensions(topology, options.index) end
  assert(width >= 8 and width <= 16 and height >= 7 and height <= 12, "invalid chamber dimensions")
  local bounds, layout, materials = interior_bounds(width, height), {}, {}
  add_open_rectangle(layout, bounds)
  local features = feature_cells(bounds, topology, layout, materials)
  local desired_spawn = { x = math.floor((bounds.min_x + bounds.max_x) / 2), y = math.floor((bounds.min_y + bounds.max_y) / 2) }
  -- Prefer the west half so east/north/south spawn groups have an immediate,
  -- visible approach vector.  The fallback keeps every topology connected.
  desired_spawn.x = clamp(desired_spawn.x - 1, bounds.min_x, bounds.max_x)
  local player_spawn = assert(nearest_open(layout, bounds, desired_spawn), "chamber has no player spawn")
  local connected = reachable(layout, player_spawn)
  local cell_count = 0
  for x = bounds.min_x, bounds.max_x do
    for y = bounds.min_y, bounds.max_y do
      if layout[key(x, y)] then
        cell_count = cell_count + 1
        assert(connected[key(x, y)], "chamber topology disconnected its board")
      end
    end
  end
  assert(cell_count >= math.floor(width * height * 0.55), "chamber topology is too dense")
  return {
    topology = topology,
    bounds = bounds,
    layout = layout,
    material_layout = materials,
    features = features,
    player_spawn = player_spawn,
    cell_count = cell_count,
  }
end

function Chambers.spawn_point(chamber, group, ordinal, occupied)
  local bounds, layout = chamber.bounds, chamber.layout
  local middle_x = math.floor((bounds.min_x + bounds.max_x) / 2)
  local middle_y = math.floor((bounds.min_y + bounds.max_y) / 2)
  local desired = { x = middle_x, y = middle_y }
  if group == "north" then desired.y = bounds.max_y
  elseif group == "south" then desired.y = bounds.min_y
  elseif group == "east" then desired.x = bounds.max_x
  elseif group == "west" then desired.x = bounds.min_x
  elseif group == "ring" then
    local ring = {
      { bounds.max_x, middle_y }, { middle_x, bounds.max_y },
      { bounds.min_x, middle_y }, { middle_x, bounds.min_y },
    }
    local selected = ring[((ordinal or 1) - 1) % #ring + 1]
    desired = { x = selected[1], y = selected[2] }
  end
  local result = nearest_open(layout, bounds, desired, occupied)
  assert(result, "chamber has no spawn point")
  return result
end

function Chambers.contains(bounds, x, y)
  return point_in(bounds, x, y)
end

function Chambers.visible_tiles(bounds, boss)
  -- FEEL-02 frames the whole compact chamber as a board. Bosses use the same
  -- 16-wide useful view because the current boss room is also 14x10.
  return 16
end

return Chambers
