-- Optional, self-contained logical-power fixture generation. It deliberately
-- uses a derived stream after other floor features so device placement never
-- perturbs terrain, enemies, media, or combat RNG.
local Grid = require("src.world.grid")

local PoweredDevices = {}

local CARDINAL = {
  { 0, 1 },
  { 1, 0 },
  { 0, -1 },
  { -1, 0 },
}

local function free_floor(world, x, y, player)
  return Grid.in_bounds(x, y)
    and world:is_passable(x, y)
    and not world:object_at(x, y)
    and not world:is_hazardous(x, y)
    and not world:is_harmful_gas_at(x, y)
    and #world:fires_at(x, y) == 0
    and Grid.distance({ x = x, y = y }, player) >= 4
end

local function candidates(world, player)
  local result = {}
  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do
      if free_floor(world, x, y, player) then
        result[#result + 1] = { x = x, y = y }
      end
    end
  end
  return result
end

local function nearby_controls(world, door, player)
  local controls = {}
  for _, direction in ipairs(CARDINAL) do
    local x, y = door.x + direction[1], door.y + direction[2]
    if free_floor(world, x, y, player) then
      controls[#controls + 1] = { x = x, y = y }
    end
  end
  return controls
end

-- A generated bulkhead is optional infrastructure, not a progression gate.
-- Evaluate its current physical cell as closed before choosing it so normal
-- target/enemy placement can continue using the same legacy rules without
-- creating an unreachable required target in a room-template branch.
local function remains_connected_when_closed(world, door)
  local total, first = 0, nil
  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do
      if (x ~= door.x or y ~= door.y) and world:is_passable(x, y) then
        total = total + 1
        first = first or { x = x, y = y }
      end
    end
  end
  if not first then return false end
  local visited, queue, cursor = { [Grid.key(first.x, first.y)] = true }, { first }, 1
  while queue[cursor] do
    local point = queue[cursor]
    cursor = cursor + 1
    for _, direction in ipairs(CARDINAL) do
      local x, y = point.x + direction[1], point.y + direction[2]
      local cell_key = Grid.key(x, y)
      if Grid.in_bounds(x, y) and (x ~= door.x or y ~= door.y) and world:is_passable(x, y) and not visited[cell_key] then
        visited[cell_key] = true
        queue[#queue + 1] = { x = x, y = y }
      end
    end
  end
  local count = 0
  for _ in pairs(visited) do count = count + 1 end
  return count == total
end

-- Generated devices are optional physical obstacles in an open room, with
-- their controls next to them. Requiring three accessible neighbours prevents
-- them from becoming a critical-path gate in either authored interior corpus.
function PoweredDevices.place(world, terrain, player, rng)
  local plan = terrain == "dungeon" and { count = 1, prefix = "dungeon_maintenance" }
    or terrain == "reactor" and { count = 2, prefix = "reactor_subsystem" }
  if not plan then
    return nil
  end
  local placed = {}
  for circuit_index = 1, plan.count do
    local door_candidates = rng:shuffle(candidates(world, player))
    for _, door in ipairs(door_candidates) do
      local controls = nearby_controls(world, door, player)
      if #controls >= 3 and remains_connected_when_closed(world, door) then
        -- The original dungeon fixture has a public, tested circuit ID. Keep
        -- that compatibility surface while Reactor receives bounded numbered
        -- subsystems.
        local circuit_id = terrain == "dungeon" and "power.circuit.stage_dungeon_maintenance"
          or string.format("power.circuit.stage_%s_%d", plan.prefix, circuit_index)
        local registered = world:register_circuit(circuit_id, { enabled = true })
        assert(registered.applied, registered.reason)
        local generator, generator_result = world:place_object("world_object.power.generator_legacy", controls[1].x, controls[1].y, {
          circuit_id = circuit_id,
          generator_online = true,
        })
        assert(generator, generator_result.reason)
        local breaker, breaker_result = world:place_object("world_object.power.breaker_legacy", controls[2].x, controls[2].y, {
          circuit_id = circuit_id,
        })
        assert(breaker, breaker_result.reason)
        local bulkhead, door_result = world:place_object("world_object.door.powered_legacy", door.x, door.y, {
          circuit_id = circuit_id,
          door_state = "closed",
        })
        assert(bulkhead, door_result.reason)
        placed[#placed + 1] = {
          circuit_id = circuit_id,
          generator_id = generator.id,
          breaker_id = breaker.id,
          door_id = bulkhead.id,
        }
        break
      end
    end
  end
  return #placed > 0 and placed or nil
end

return PoweredDevices
