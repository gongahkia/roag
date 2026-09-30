-- Sparse deterministic finite gas pockets. This isolated generation stream
-- leaves terrain, cover, hazards, liquids, enemies, and combat RNG untouched.
local Grid = require("src.world.grid")

local GasGeneration = {}

local PLANS = {
  dungeon = { gas_id = "gas.toxic.legacy", count = 1, concentration = 4 },
}

local function terrain_neighbours(world, x, y)
  local count = 0
  for _, point in ipairs(Grid.neighbours(Grid.cell(x, y))) do
    if world:allows_gas_at(point.x, point.y) then
      count = count + 1
    end
  end
  return count
end

local function candidates(world, player)
  local result = {}
  for x = 1, Grid.width - 2 do
    for y = 1, Grid.height - 2 do
      if world:allows_gas_at(x, y) and Grid.distance(player, Grid.cell(x, y)) >= 7
        and terrain_neighbours(world, x, y) >= 2 then
        result[#result + 1] = Grid.cell(x, y)
      end
    end
  end
  return result
end

function GasGeneration.place(world, terrain, player, rng)
  local plan = PLANS[terrain]
  if not plan then
    return {}
  end
  local definition = world.registry:get_gas(plan.gas_id)
  local placed = {}
  local options = rng:shuffle(candidates(world, player))
  for index = 1, math.min(plan.count, #options) do
    local point = options[index]
    local result = world:add_gas(point.x, point.y, definition.id, plan.concentration)
    assert(result.applied, result.reason or result.code)
    placed[#placed + 1] = world:gas_at(point.x, point.y)
  end
  return placed
end

return GasGeneration
