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

-- Dungeon-only initially: it is an optional physical obstacle in an open
-- room, with its controls next to it. Requiring three accessible neighbours
-- prevents it from becoming a legacy-stage critical-path gate.
function PoweredDevices.place(world, terrain, player, rng)
  if terrain ~= "dungeon" then
    return nil
  end
  local door_candidates = candidates(world, player)
  rng:shuffle(door_candidates)
  for _, door in ipairs(door_candidates) do
    local controls = nearby_controls(world, door, player)
    if #controls >= 3 and remains_connected_when_closed(world, door) then
      local circuit_id = "power.circuit.stage_dungeon_maintenance"
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
      return {
        circuit_id = circuit_id,
        generator_id = generator.id,
        breaker_id = breaker.id,
        door_id = bulkhead.id,
      }
    end
  end
  return nil
end

return PoweredDevices
