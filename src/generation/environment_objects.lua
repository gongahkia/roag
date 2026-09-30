-- Deterministic, conservative placement seam for physical world objects.
-- It deliberately places only a few objects on roomy, non-start tiles.
local Grid = require("src.world.grid")

local EnvironmentObjects = {}

local PLANS = {
  forest = {
    { definition_id = "world_object.cover.timber_crate", count = 2 },
  },
  cave = {
    { definition_id = "world_object.cover.timber_crate", count = 1 },
    { definition_id = "world_object.cover.masonry_barricade", count = 1 },
    { definition_id = "world_object.cover.conductive_metal_crate", count = 1 },
  },
  dungeon = {
    { definition_id = "world_object.cover.timber_crate", count = 1 },
    { definition_id = "world_object.cover.masonry_barricade", count = 2 },
    { definition_id = "world_object.cover.conductive_metal_crate", count = 1 },
  },
}

local function terrain_neighbours(world, x, y)
  local count = 0
  for _, point in ipairs(Grid.neighbours(Grid.cell(x, y))) do
    if world:terrain_is_passable(point.x, point.y) then
      count = count + 1
    end
  end
  return count
end

local function candidates(world, player)
  local result = {}
  for x = 1, Grid.width - 2 do
    for y = 1, Grid.height - 2 do
      if world:is_passable(x, y) and Grid.distance(player, Grid.cell(x, y)) >= 7
        and terrain_neighbours(world, x, y) >= 3 then
        result[#result + 1] = Grid.cell(x, y)
      end
    end
  end
  return result
end

function EnvironmentObjects.place(world, terrain, player, rng)
  local placed = {}
  for _, plan in ipairs(PLANS[terrain] or {}) do
    local options = rng:shuffle(candidates(world, player))
    for index = 1, math.min(plan.count, #options) do
      local point = options[index]
      local object, result = world:place_object(plan.definition_id, point.x, point.y)
      if object then
        placed[#placed + 1] = object
      else
        assert(result.code == "occupied", result.reason)
      end
    end
  end
  return placed
end

return EnvironmentObjects
