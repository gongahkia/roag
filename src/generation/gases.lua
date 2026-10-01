-- Sparse, finite toxic-gas pockets. This receives a derived generation stream
-- from Session so changing gas placement cannot perturb other generation.
local Grid = require("src.world.grid")

local GasGeneration = {}

local PLANS = {
  cave = { gas_id = "gas.toxic.legacy", count = 2 },
  reactor = { gas_id = "gas.toxic.legacy", count = 2 },
}

local CARDINAL_DIRECTIONS = {
  { 0, 1 },
  { 1, 0 },
  { 0, -1 },
  { -1, 0 },
}

local function accessible_neighbours(world, x, y)
  local count = 0
  for _, direction in ipairs(CARDINAL_DIRECTIONS) do
    if world:allows_gas_at(x + direction[1], y + direction[2]) then
      count = count + 1
    end
  end
  return count
end

local function candidates(world, player)
  local result = {}
  for x = 1, Grid.width - 2 do
    for y = 1, Grid.height - 2 do
      if world:allows_gas_at(x, y) and not world:object_at(x, y) and not world:is_hazardous(x, y)
        and Grid.distance(player, Grid.cell(x, y)) >= 7 and accessible_neighbours(world, x, y) >= 2 then
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
  local placed, options, pockets = {}, rng:shuffle(candidates(world, player)), 0
  for _, point in ipairs(options) do
    if pockets >= plan.count then
      break
    end
    if not world:is_gas_cell(point.x, point.y) then
      local seed = world:add_gas(point.x, point.y, definition.id, definition.max_concentration)
      if seed.applied then
        pockets = pockets + 1
        placed[#placed + 1] = world:gas_at(point.x, point.y)
        -- A neighboring dense cell makes a pocket readable immediately while
        -- remaining finite and wholly governed by ordinary diffusion later.
        for _, direction in ipairs(CARDINAL_DIRECTIONS) do
          local x, y = point.x + direction[1], point.y + direction[2]
          if world:allows_gas_at(x, y) and not world:is_gas_cell(x, y)
            and Grid.distance(player, Grid.cell(x, y)) >= 7 then
            local added = world:add_gas(x, y, definition.id, definition.max_concentration - 1)
            if added.applied then
              placed[#placed + 1] = world:gas_at(x, y)
            end
            break
          end
        end
      end
    end
  end
  return placed
end

return GasGeneration
