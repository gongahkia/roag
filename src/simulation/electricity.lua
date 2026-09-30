-- Transient deterministic electrical-network resolution. Conductivity stays
-- a live World query; no energized cells or cached graph survive a discharge.
local Grid = require("src.world.grid")

local Electricity = {
  DEFAULT_MAX_CELLS = 12,
}

local CARDINAL_DIRECTIONS = {
  { 0, 1 }, -- north
  { 1, 0 }, -- east
  { 0, -1 }, -- south
  { -1, 0 }, -- west
}

local function key(x, y)
  return Grid.key(x, y)
end

local function validate_origin(origin)
  return type(origin) == "table" and type(origin.x) == "number" and type(origin.y) == "number"
    and origin.x % 1 == 0 and origin.y % 1 == 0 and Grid.in_bounds(origin.x, origin.y)
end

local function validate_max_cells(value)
  return type(value) == "number" and value > 0 and value % 1 == 0
end

-- Injection can begin on a conductive origin or any cardinal conductive
-- neighbor. This lets a device energize adjacent water/metal without making
-- the nonconductive device coordinate itself part of the network.
function Electricity.trace(world, origin, spec)
  spec = spec or {}
  if not world then
    return { applied = false, code = "no_world", reason = "No active world" }
  end
  if not validate_origin(origin) then
    return { applied = false, code = "invalid_origin", reason = "Electrical origin is invalid" }
  end
  local max_cells = spec.max_cells or Electricity.DEFAULT_MAX_CELLS
  if not validate_max_cells(max_cells) then
    return { applied = false, code = "invalid_bound", reason = "Electrical propagation bound is invalid" }
  end

  local queue, visited, reached = {}, {}, {}
  local function enqueue(x, y, distance)
    local location_key = key(x, y)
    if Grid.in_bounds(x, y) and not visited[location_key] and world:is_conductive_at(x, y) then
      visited[location_key] = true
      queue[#queue + 1] = { x = x, y = y, distance = distance }
    end
  end

  enqueue(origin.x, origin.y, 0)
  for _, direction in ipairs(CARDINAL_DIRECTIONS) do
    enqueue(origin.x + direction[1], origin.y + direction[2], 1)
  end

  local head, truncated = 1, false
  while head <= #queue do
    if #reached >= max_cells then
      truncated = true
      break
    end
    local current = queue[head]
    head = head + 1
    reached[#reached + 1] = current
    for _, direction in ipairs(CARDINAL_DIRECTIONS) do
      enqueue(current.x + direction[1], current.y + direction[2], current.distance + 1)
    end
  end
  if head <= #queue then
    truncated = true
  end
  return {
    applied = #reached > 0,
    code = #reached > 0 and "traced" or "no_conductive_network",
    origin = { x = origin.x, y = origin.y },
    max_cells = max_cells,
    reached_cells = reached,
    network_size = #reached,
    truncated = truncated,
    source = {
      actor_id = spec.source_actor_id,
      component_id = spec.source_component_id,
      ability_id = spec.ability_id,
      cause = spec.cause,
    },
  }
end

-- Hooks retain actor-damage ownership in Session while the traversal itself
-- remains reusable and actor-agnostic. actors_at must already be stable.
function Electricity.discharge(world, origin, spec, hooks)
  local result = Electricity.trace(world, origin, spec)
  if not result.applied then
    result.affected_actor_ids = {}
    result.damage_results = {}
    return result
  end
  hooks = hooks or {}
  local affected, damage_results, seen = {}, {}, {}
  if hooks.actors_at and hooks.on_actor_reached then
    for _, cell in ipairs(result.reached_cells) do
      local actors = {}
      for _, actor in ipairs(hooks.actors_at(cell.x, cell.y)) do
        actors[#actors + 1] = actor
      end
      if hooks.actor_id then
        table.sort(actors, function(left, right)
          return hooks.actor_id(left) < hooks.actor_id(right)
        end)
      end
      for _, actor in ipairs(actors) do
        if not seen[actor] then
          seen[actor] = true
          local actor_id = hooks.actor_id and hooks.actor_id(actor) or tostring(actor)
          local damage = hooks.on_actor_reached(actor, cell, result)
          affected[#affected + 1] = actor_id
          damage_results[#damage_results + 1] = {
            actor_id = actor_id,
            x = cell.x,
            y = cell.y,
            damage = damage,
          }
        end
      end
    end
  end
  result.code = "discharged"
  result.affected_actor_ids = affected
  result.damage_results = damage_results
  return result
end

return Electricity
