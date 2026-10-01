-- Sparse, deterministic shallow-pool placement. It is a separate generation
-- stream so liquid tuning never perturbs actors, cover, hazards, or combat.
local Grid = require("src.world.grid")

local LiquidGeneration = {}

local PLANS = {
  cave = { liquid_id = "liquid.water.legacy", count = 3 },
  dungeon = { liquid_id = "liquid.water.legacy", count = 2 },
  reactor = { liquid_id = "liquid.water.legacy", count = 5 },
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
      if world:terrain_is_passable(x, y) and not world:object_at(x, y) and not world:is_hazardous(x, y)
        and Grid.distance(player, Grid.cell(x, y)) >= 7 and terrain_neighbours(world, x, y) >= 2 then
        result[#result + 1] = Grid.cell(x, y)
      end
    end
  end
  return result
end

function LiquidGeneration.place(world, terrain, player, rng)
  local plan = PLANS[terrain]
  if not plan then
    return {}
  end
  local definition = world.registry:get_liquid(plan.liquid_id)
  local placed = {}
  local options = rng:shuffle(candidates(world, player))
  for index = 1, math.min(plan.count, #options) do
    local point = options[index]
    local result = world:add_liquid(point.x, point.y, definition.id, definition.max_depth)
    assert(result.applied, result.reason or result.code)
    placed[#placed + 1] = world:liquid_at(point.x, point.y)
  end
  return placed
end

return LiquidGeneration
