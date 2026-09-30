-- Sparse, deterministic floor-hazard placement. This is intentionally a
-- separate generation seam from terrain and cover so later content can add
-- hazards without perturbing unrelated RNG streams.
local Grid = require("src.world.grid")

local HazardGeneration = {}

local PLANS = {
  forest = { definition_id = "hazard.legacy.spike_field", count = 2 },
  cave = { definition_id = "hazard.legacy.spike_field", count = 2 },
  dungeon = { definition_id = "hazard.legacy.spike_field", count = 3 },
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
      if world:is_passable(x, y) and not world:object_at(x, y) and not world:is_hazardous(x, y)
        and Grid.distance(player, Grid.cell(x, y)) >= 7 and terrain_neighbours(world, x, y) >= 2 then
        result[#result + 1] = Grid.cell(x, y)
      end
    end
  end
  return result
end

function HazardGeneration.place(world, terrain, player, rng)
  local plan = PLANS[terrain]
  if not plan then
    return {}
  end
  local placed = {}
  local options = rng:shuffle(candidates(world, player))
  for index = 1, math.min(plan.count, #options) do
    local point = options[index]
    local hazard, result = world:place_hazard(plan.definition_id, point.x, point.y)
    assert(hazard, result.reason)
    placed[#placed + 1] = hazard
  end
  return placed
end

return HazardGeneration
