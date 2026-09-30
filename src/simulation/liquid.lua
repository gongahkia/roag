-- Deterministic finite shallow-liquid flow. World owns liquid cells; this
-- module only resolves synchronous redistribution and fire suppression.
local Grid = require("src.world.grid")

local Liquid = {}

-- Stable cardinal order: north, east, south, west. This is deliberately
-- explicit so equal shallow pools resolve identically on every Lua runtime.
local CARDINAL_DIRECTIONS = {
  { 0, 1 },
  { 1, 0 },
  { 0, -1 },
  { -1, 0 },
}

local function key(x, y)
  return Grid.key(x, y)
end

local function snapshot_liquids(world)
  local values = {}
  for _, liquid in ipairs(world:list_liquids()) do
    values[key(liquid.x, liquid.y)] = {
      liquid_id = liquid.liquid_id,
      amount = liquid.amount,
      x = liquid.x,
      y = liquid.y,
    }
  end
  return values
end

-- A cell donates one unit only when its snapshot depth is at least two above
-- the neighbor. Queued deltas keep the update synchronous: new liquid cannot
-- become another source until the next world tick.
function Liquid.tick(world)
  if not world then
    return { applied = false, code = "no_world", reason = "No active world" }
  end
  local snapshot = snapshot_liquids(world)
  local deltas, planned_liquid_ids, transfers = {}, {}, {}
  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do
      local source = snapshot[key(x, y)]
      if source then
        local definition = world.registry:get_liquid(source.liquid_id)
        local available = source.amount
        for _, direction in ipairs(CARDINAL_DIRECTIONS) do
          local target_x, target_y = x + direction[1], y + direction[2]
          local target_key = key(target_x, target_y)
          local target = Grid.in_bounds(target_x, target_y) and snapshot[target_key] or nil
          local planned_id = target and target.liquid_id or planned_liquid_ids[target_key]
          local target_amount = target and target.amount or 0
          local queued_amount = deltas[target_key] or 0
          if Grid.in_bounds(target_x, target_y) and world:terrain_is_passable(target_x, target_y)
            and (not planned_id or planned_id == source.liquid_id)
            and available >= target_amount + 2
            and target_amount + queued_amount < definition.max_depth then
            deltas[key(x, y)] = (deltas[key(x, y)] or 0) - 1
            deltas[target_key] = queued_amount + 1
            planned_liquid_ids[target_key] = source.liquid_id
            available = available - 1
            transfers[#transfers + 1] = {
              liquid_id = source.liquid_id,
              from_x = x,
              from_y = y,
              to_x = target_x,
              to_y = target_y,
              amount = 1,
            }
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
        local liquid_id = prior and prior.liquid_id or planned_liquid_ids[location_key]
        local amount = (prior and prior.amount or 0) + delta
        local result = world:set_liquid(x, y, liquid_id, amount)
        assert(result.applied, result.reason or result.code)
      end
    end
  end
  world.liquid_tick = world.liquid_tick + 1
  return {
    applied = #transfers > 0,
    code = "ticked",
    tick = world.liquid_tick,
    transfers = transfers,
  }
end

-- Suppression is a distinct world-process phase. Fire still owns its own
-- lifecycle; liquid merely deactivates active fire whose current physical
-- target coordinate is covered by an extinguishing liquid.
function Liquid.suppress_fires(world)
  if not world then
    return { applied = false, code = "no_world", reason = "No active world" }
  end
  local extinguished = {}
  for _, fire in ipairs(world:list_fires()) do
    local x, y = world:fire_position(fire)
    if x and world:liquid_extinguishes_fire_at(x, y) then
      local result = world:deactivate_fire(fire, "suppressed_by_liquid")
      if result.applied then
        extinguished[#extinguished + 1] = result
      end
    end
  end
  return {
    applied = #extinguished > 0,
    code = "suppressed",
    extinguished = extinguished,
  }
end

return Liquid
