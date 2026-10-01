local Grid = require("src.world.grid")
local RoomAssembler = require("src.generation.dungeon_rooms")
local Corpora = require("src.rooms.corpora")

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

function Map.generate(terrain, start, rng, arena, options)
  local space = {}
  if arena then
    carve(space, 2, 2, 37, 15)
    return space
  end

  if terrain == "dungeon" or terrain == "reactor" then
    options = options or {}
    local room_options = {}
    for name, value in pairs(options) do room_options[name] = value end
    room_options.room_config = options.room_config or Corpora.for_biome(terrain)
    local layout, metadata = RoomAssembler.generate(start, options.room_rng or rng, room_options)
    assert(layout, metadata and metadata.reason or "Room assembly failed")
    return layout, metadata
  end

  local chance = terrain == "forest" and 0.25 or 0.43
  for x = 1, Grid.width - 2 do
    for y = 1, Grid.height - 2 do
      if rng:float() > chance then
        space[Grid.key(x, y)] = true
      end
    end
  end
  for _ = 1, terrain == "forest" and 2 or 5 do
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
  carve(space, start.x - 3, start.y - 3, 7, 7)
  return space
end

return Map
