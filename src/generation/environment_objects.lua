-- Deterministic, conservative placement seam for physical world objects.
-- It deliberately places only a few objects on roomy, non-start tiles.
local Grid = require("src.world.grid")

local EnvironmentObjects = {}

-- Generic cover is optional texture around richer landmark geometry.  A
-- bounded number of full-map checks avoids pathological seeds making batch
-- analysis spend most of its time searching for a third crate placement.
local MAX_CONNECTIVITY_CANDIDATES_PER_PLAN = 4

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
  reactor = {
    { definition_id = "world_object.cover.timber_crate", count = 1 },
    { definition_id = "world_object.cover.masonry_barricade", count = 2 },
    { definition_id = "world_object.cover.conductive_metal_crate", count = 2 },
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
      if world:is_passable(x, y) and not world:is_liquid_cell(x, y) and not world:is_gas_cell(x, y)
        and Grid.distance(player, Grid.cell(x, y)) >= 7
        and terrain_neighbours(world, x, y) >= 3 then
        result[#result + 1] = Grid.cell(x, y)
      end
    end
  end
  return result
end

-- Cover remains a tactical obstacle, never an unintentional map partition.
-- Test only randomized candidates until enough non-articulation cells are
-- found, avoiding an expensive full flood-fill for every open cell.
local function remains_connected_when_occupied(world, point)
  local total, first = 0, nil
  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do
      if (x ~= point.x or y ~= point.y) and world:is_passable(x, y) then
        total = total + 1
        first = first or { x = x, y = y }
      end
    end
  end
  if not first then return false end
  local visited, queue, cursor = { [Grid.key(first.x, first.y)] = true }, { first }, 1
  while queue[cursor] do
    local current = queue[cursor]
    cursor = cursor + 1
    for _, neighbour in ipairs(Grid.neighbours(current)) do
      local cell_key = Grid.key(neighbour.x, neighbour.y)
      if Grid.in_bounds(neighbour.x, neighbour.y) and (neighbour.x ~= point.x or neighbour.y ~= point.y)
        and world:is_passable(neighbour.x, neighbour.y) and not visited[cell_key] then
        visited[cell_key] = true
        queue[#queue + 1] = neighbour
      end
    end
  end
  local reached = 0
  for _ in pairs(visited) do reached = reached + 1 end
  return reached == total
end

function EnvironmentObjects.place(world, terrain, player, rng)
  local placed = {}
  for _, plan in ipairs(PLANS[terrain] or {}) do
    local options = rng:shuffle(candidates(world, player))
    local count, checked = 0, 0
    for _, point in ipairs(options) do
      if count >= plan.count then break end
      checked = checked + 1
      if checked > MAX_CONNECTIVITY_CANDIDATES_PER_PLAN then break end
      -- Open-biome landmarks now create intentional narrow woodland/cavern
      -- routes too, so generic cover receives the same no-partition proof as
      -- authored interiors.  A crate is tactical cover, never a hidden map
      -- split that can strand future generated content.
      if remains_connected_when_occupied(world, point) then
        local object, result = world:place_object(plan.definition_id, point.x, point.y)
        if object then
          placed[#placed + 1] = object
          count = count + 1
        else
          assert(result.code == "occupied", result.reason)
        end
      end
    end
  end
  -- A noncritical dungeon proof. Closing this cell cannot disconnect the
  -- generated floor, so research changes optional traversal only.
  if terrain == "dungeon" or terrain == "reactor" then
    local options = rng:shuffle(candidates(world, player))
    for _, point in ipairs(options) do
      if remains_connected_when_occupied(world, point) then
        local barrier = world:place_object("world_object.traversal.reinforced_barrier", point.x, point.y)
        if barrier then placed[#placed + 1] = barrier end
        break
      end
    end
  end
  return placed
end

return EnvironmentObjects
