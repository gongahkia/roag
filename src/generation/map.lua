local Grid = require("src.world.grid")
local RoomAssembler = require("src.generation.dungeon_rooms")
local Corpora = require("src.rooms.corpora")
local Landmarks = require("src.generation.landmarks")

local Map = {}

local function carve(space, x, y, width, height)
  for point_x = x, x + width - 1 do
    for point_y = y, y + height - 1 do
      if point_x > 0 and point_x < Grid.width - 1 and point_y > 0 and point_y < Grid.height - 1 then
        space[Grid.key(point_x, point_y)] = true
      end
    end
  end
end

local function corridor(space, from, to, rng)
  local x, y = from.x, from.y
  local function horizontal()
    while x ~= to.x do
      space[Grid.key(x, y)] = true
      x = x + (to.x > x and 1 or -1)
    end
  end
  local function vertical()
    while y ~= to.y do
      space[Grid.key(x, y)] = true
      y = y + (to.y > y and 1 or -1)
    end
  end
  if rng:float() < 0.5 then
    horizontal()
    vertical()
  else
    vertical()
    horizontal()
  end
  space[Grid.key(x, y)] = true
end

local function walls_near(space, x, y)
  local walls = 0
  for delta_x = -1, 1 do
    for delta_y = -1, 1 do
      if (delta_x ~= 0 or delta_y ~= 0) and not space[Grid.key(x + delta_x, y + delta_y)] then
        walls = walls + 1
      end
    end
  end
  return walls
end

local function carve_ellipse(space, center_x, center_y, radius_x, radius_y)
  for x = center_x - radius_x, center_x + radius_x do
    for y = center_y - radius_y, center_y + radius_y do
      local horizontal = (x - center_x) / radius_x
      local vertical = (y - center_y) / radius_y
      if horizontal * horizontal + vertical * vertical <= 1
        and x > 0 and x < Grid.width - 1 and y > 0 and y < Grid.height - 1 then
        space[Grid.key(x, y)] = true
      end
    end
  end
end

-- Forests use connected clearings and narrow, meandering tracks instead of
-- the previous almost-fully-open cellular field.  This retains a deterministic
-- open-biome generator while giving trees, ridges, streams, and enemies room
-- to create deliberate sightlines and flanking choices.
local function forest_layout(start, rng)
  local space, clearings = {}, {}
  local function add_clearing(point, radius_x, radius_y, kind)
    carve_ellipse(space, point.x, point.y, radius_x, radius_y)
    clearings[#clearings + 1] = { x = point.x, y = point.y, radius_x = radius_x, radius_y = radius_y, kind = kind }
  end
  local function trail(from, to)
    local x, y = from.x, from.y
    carve_ellipse(space, x, y, 1, 1)
    while x ~= to.x or y ~= to.y do
      local horizontal, vertical = math.abs(to.x - x), math.abs(to.y - y)
      local choose_horizontal = horizontal > 0 and (vertical == 0 or horizontal >= vertical or rng:float() < 0.28)
      if choose_horizontal then x = x + (to.x > x and 1 or -1)
      else y = y + (to.y > y and 1 or -1) end
      carve_ellipse(space, x, y, 1, 1)
    end
  end
  add_clearing(start, 4, 4, "arrival_clearing")
  local anchors = { { x = start.x, y = start.y } }
  for index = 1, 11 do
    local point = { x = rng:int(5, Grid.width - 6), y = rng:int(5, Grid.height - 6) }
    local broad = index % 4 == 0
    local radius_x = broad and rng:int(4, 5) or rng:int(2, 4)
    local radius_y = broad and rng:int(3, 4) or rng:int(2, 4)
    local origin = anchors[rng:int(1, #anchors)]
    trail(origin, point)
    add_clearing(point, radius_x, radius_y, broad and "meadow" or "clearing")
    anchors[#anchors + 1] = point
  end
  return space, clearings
end

function Map.generate(terrain, start, rng, arena, options)
  options = options or {}
  local space = {}
  if arena then
    carve(space, 2, 2, 37, 15)
    return space
  end

  if terrain == "dungeon" or terrain == "reactor" then
    local room_options = {}
    for name, value in pairs(options) do room_options[name] = value end
    room_options.room_config = options.room_config or Corpora.for_biome(terrain)
    local layout, metadata = RoomAssembler.generate(start, options.room_rng or rng, room_options)
    assert(layout, metadata and metadata.reason or "Room assembly failed")
    local material_layout, planned = Landmarks.plan_layout(terrain, layout, start,
      options.landmark_layout_rng or rng:derive("landmarks.layout"), metadata.material_layout)
    metadata.material_layout, metadata.landmark_layout = material_layout, planned
    return layout, metadata
  end

  local metadata = {}
  if terrain == "forest" then
    space, metadata.clearings = forest_layout(start, rng)
  else
    local chance = 0.43
    for x = 1, Grid.width - 2 do
      for y = 1, Grid.height - 2 do
        if rng:float() > chance then
          space[Grid.key(x, y)] = true
        end
      end
    end
    for _ = 1, 5 do
      local next_space = {}
      for x = 1, Grid.width - 2 do
        for y = 1, Grid.height - 2 do
          if walls_near(space, x, y) < 5 then
            next_space[Grid.key(x, y)] = true
          end
        end
      end
      space = next_space
    end
  end
  carve(space, start.x - 3, start.y - 3, 7, 7)
  local material_layout, planned = Landmarks.plan_layout(terrain, space, start,
    options.landmark_layout_rng or rng:derive("landmarks.layout"))
  metadata.material_layout, metadata.landmark_layout = material_layout, planned
  return space, metadata
end

return Map
