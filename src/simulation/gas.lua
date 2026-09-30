-- Deterministic finite gas diffusion. Gas belongs to World; this module only
-- resolves one synchronous redistribution step and deliberately uses no RNG.
local Grid = require("src.world.grid")

local Gas = {}

-- Stable tie-break order: north, east, south, west.
local CARDINAL_DIRECTIONS = {
  { 0, 1 },
  { 1, 0 },
  { 0, -1 },
  { -1, 0 },
}

local function key(x, y)
  return Grid.key(x, y)
end

local function snapshot_gases(world)
  local values = {}
  for _, gas in ipairs(world:list_gases()) do
    values[key(gas.x, gas.y)] = {
      gas_id = gas.gas_id,
      concentration = gas.concentration,
      x = gas.x,
      y = gas.y,
    }
  end
  return values
end

-- Each eligible source gives at most one unit per tick. A source must have at
-- least two units and be at least two units denser than its destination. The
-- strict gradient prevents trace gas from wandering forever; a distribution
-- whose neighbours differ by at most one is stable. All choices use the
-- snapshot, then queued deltas commit together, so new gas never chains
-- onward during the same tick.
function Gas.tick(world)
  if not world then
    return { applied = false, code = "no_world", reason = "No active world" }
  end
  local snapshot = snapshot_gases(world)
  local deltas, planned_gas_ids, transfers = {}, {}, {}
  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do
      local source = snapshot[key(x, y)]
      if source and source.concentration >= 2 then
        local definition = world.registry:get_gas(source.gas_id)
        for _, direction in ipairs(CARDINAL_DIRECTIONS) do
          local target_x, target_y = x + direction[1], y + direction[2]
          local target_key = key(target_x, target_y)
          local target = Grid.in_bounds(target_x, target_y) and snapshot[target_key] or nil
          local planned_id = target and target.gas_id or planned_gas_ids[target_key]
          local target_concentration = target and target.concentration or 0
          local queued_concentration = deltas[target_key] or 0
          if Grid.in_bounds(target_x, target_y) and world:allows_gas_at(target_x, target_y)
            and (not planned_id or planned_id == source.gas_id)
            and source.concentration >= target_concentration + 2
            and target_concentration + queued_concentration < definition.max_concentration then
            deltas[key(x, y)] = (deltas[key(x, y)] or 0) - 1
            deltas[target_key] = queued_concentration + 1
            planned_gas_ids[target_key] = source.gas_id
            transfers[#transfers + 1] = {
              gas_id = source.gas_id,
              from_x = x,
              from_y = y,
              to_x = target_x,
              to_y = target_y,
              concentration = 1,
            }
            break
          end
        end
      end
    end
  end

  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do
      local location_key = key(x, y)
      local delta = deltas[location_key]
      if delta and delta ~= 0 then
        local prior = snapshot[location_key]
        local gas_id = prior and prior.gas_id or planned_gas_ids[location_key]
        local concentration = (prior and prior.concentration or 0) + delta
        local result = world:set_gas(x, y, gas_id, concentration)
        assert(result.applied, result.reason or result.code)
      end
    end
  end
  return {
    applied = #transfers > 0,
    code = "ticked",
    transfers = transfers,
  }
end

return Gas
