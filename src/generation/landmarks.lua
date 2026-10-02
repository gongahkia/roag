-- Bounded, deterministic biome landmarks.  The intent is not decorative
-- noise: each object is ordinary physical cover/terrain and every water or
-- conductor cell uses the existing environmental simulation.  The small set
-- of kinds below is deliberately authored in content/terrain_features rather
-- than exposing a room/puzzle scripting language.
local Grid = require("src.world.grid")
local Profiles = require("content.terrain_features.legacy")

local Landmarks = {}

local KNOWN_KINDS = {
  grove = true,
  ridge = true,
  scatter = true,
  stream = true,
  stream_object = true,
}

local CARDINAL = {
  { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 },
}

local function key(point)
  return Grid.key(point.x, point.y)
end

local function copy_layout(layout)
  local result = {}
  for location_key, material_id in pairs(layout or {}) do result[location_key] = material_id end
  return result
end

local function valid_landmark_id(id)
  return type(id) == "string" and id:match("^landmark%.[a-z0-9_%.]+$") ~= nil
end

-- Forest stone hollows are genuine one-entrance alcoves carved into the
-- ordinary map, not secret sublevels.  They make a forest feel like a place
-- growing around older geology while preserving one connected playable map.
local function forest_stone_hollow(space, start, rng, material_layout)
  local candidates = {}
  for x = 4, Grid.width - 5 do
    for y = 4, Grid.height - 5 do
      if not space[Grid.key(x, y)] and Grid.distance(start, { x = x, y = y }) >= 12 then
        local solid = true
        for dx = -1, 1 do
          for dy = -1, 1 do
            if space[Grid.key(x + dx, y + dy)] then solid = false; break end
          end
          if not solid then break end
        end
        if solid then candidates[#candidates + 1] = { x = x, y = y } end
      end
    end
  end
  for _, center in ipairs(rng:shuffle(candidates)) do
    local entrance, entrance_distance
    for x = math.max(1, center.x - 10), math.min(Grid.width - 2, center.x + 10) do
      for y = math.max(1, center.y - 10), math.min(Grid.height - 2, center.y + 10) do
        if space[Grid.key(x, y)] then
          local distance = math.abs(center.x - x) + math.abs(center.y - y)
          if distance >= 3 and distance <= 10 and (not entrance_distance or distance < entrance_distance) then
            entrance, entrance_distance = { x = x, y = y }, distance
          end
        end
      end
    end
    if entrance then
      for dx = -1, 1 do
        for dy = -1, 1 do
          local x, y = center.x + dx, center.y + dy
          space[Grid.key(x, y)] = true
          material_layout[Grid.key(x, y)] = "material.terrain.forest_soil"
        end
      end
      -- A narrow access corridor makes the hollow a readable location without
      -- disconnecting anything; it only adds ordinary passable terrain.
      local x, y = center.x, center.y
      while x ~= entrance.x do
        x = x + (entrance.x > x and 1 or -1)
        space[Grid.key(x, y)] = true
      end
      while y ~= entrance.y do
        y = y + (entrance.y > y and 1 or -1)
        space[Grid.key(x, y)] = true
      end
      for dx = -2, 2 do
        for dy = -2, 2 do
          local x, y = center.x + dx, center.y + dy
          if Grid.in_bounds(x, y) and not space[Grid.key(x, y)] then
            material_layout[Grid.key(x, y)] = "material.terrain.granite"
          end
        end
      end
      return {
        id = "landmark.forest.stone_hollow",
        kind = "stone_hollow",
        x = center.x,
        y = center.y,
        entrance_x = entrance.x,
        entrance_y = entrance.y,
      }
    end
  end
  return nil
end

-- Map.generate calls this before World construction.  It can therefore
-- author material identity and the one bounded Forest hollow while the
-- physical object/water layer is placed later against the real World.
function Landmarks.plan_layout(terrain, space, start, rng, base_material_layout)
  local material_layout, planned = copy_layout(base_material_layout), {}
  if terrain == "forest" then
    local hollow = forest_stone_hollow(space, start, rng, material_layout)
    if hollow then planned[#planned + 1] = hollow end
  end
  return material_layout, planned
end

function Landmarks.validate(registry)
  for terrain, features in pairs(Profiles) do
    assert(type(terrain) == "string" and type(features) == "table", "Terrain landmark profile is invalid")
    local seen = {}
    for _, feature in ipairs(features) do
      assert(valid_landmark_id(feature.id) and not seen[feature.id], "Terrain landmark ID is invalid or duplicated")
      seen[feature.id] = true
      assert(KNOWN_KINDS[feature.kind], "Terrain landmark kind is invalid")
      assert(type(feature.count) == "number" and feature.count > 0 and feature.count % 1 == 0,
        "Terrain landmark count is invalid")
      if feature.kind == "stream" then
        assert(type(feature.liquid_id) == "string", "Stream landmark requires a liquid")
        registry:get_liquid(feature.liquid_id)
      else
        assert(type(feature.definition_id) == "string", "Object landmark requires a world object")
        registry:get_world_object(feature.definition_id)
      end
    end
  end
  return true
end

local function point_is_free(world, point, player)
  return Grid.in_bounds(point.x, point.y)
    and world:is_passable(point.x, point.y)
    and not world:object_at(point.x, point.y)
    and not world:is_hazardous(point.x, point.y)
    and not world:is_liquid_cell(point.x, point.y)
    and not world:is_gas_cell(point.x, point.y)
    and #world:fires_at(point.x, point.y) == 0
    and Grid.distance(player, point) >= 7
end

local function candidates(world, player)
  local result = {}
  for x = 1, Grid.width - 2 do
    for y = 1, Grid.height - 2 do
      local point = { x = x, y = y }
      if point_is_free(world, point, player) then result[#result + 1] = point end
    end
  end
  return result
end

-- Landmark blockers belong in clearings/chambers, not in a one-cell trail or
-- doorway.  Requiring room around every occupied tile prevents a cluster from
-- sealing the intentionally narrow routes between landmarks while keeping
-- large headless generation batches proportional to map count, not candidate
-- count.  Full critical-path validation remains in generation analysis.
local function has_clearance(world, point, minimum)
  local open = 0
  for dx = -2, 2 do
    for dy = -2, 2 do
      if world:is_passable(point.x + dx, point.y + dy) then open = open + 1 end
    end
  end
  return open >= minimum
end

local function group_offsets(kind, count, rng)
  local result = {}
  if kind == "grove" then
    local patterns = {
      { { 0, 0 }, { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 }, { 1, 1 }, { -1, -1 } },
      { { 0, 0 }, { 1, 0 }, { 2, 0 }, { 0, 1 }, { 1, 1 }, { -1, -1 }, { 0, -1 } },
    }
    for _, offset in ipairs(rng:choice(patterns)) do result[#result + 1] = offset end
  elseif kind == "ridge" then
    local horizontal = rng:float() < 0.5
    for index = 0, count - 1 do
      local bend = index % 3 == 2 and 1 or 0
      result[#result + 1] = horizontal and { index - math.floor(count / 2), bend } or { bend, index - math.floor(count / 2) }
    end
  else
    result[1] = { 0, 0 }
  end
  while #result > count do table.remove(result) end
  return result
end

local function group_points(anchor, offsets)
  local result, seen = {}, {}
  for _, offset in ipairs(offsets) do
    local point = { x = anchor.x + offset[1], y = anchor.y + offset[2] }
    local location_key = key(point)
    if seen[location_key] then return nil end
    seen[location_key] = true
    result[#result + 1] = point
  end
  return result
end

local function select_group(world, player, feature, definition, rng)
  local attempts = 0
  for _, anchor in ipairs(rng:shuffle(candidates(world, player))) do
    attempts = attempts + 1
    if attempts > 6 then break end
    local points = group_points(anchor, group_offsets(feature.kind, feature.count, rng))
    local valid = points ~= nil and has_clearance(world, anchor, feature.kind == "scatter" and 12 or 18)
    if valid then
      for _, point in ipairs(points) do
        if not point_is_free(world, point, player) or not has_clearance(world, point, 10) then valid = false; break end
      end
    end
    if valid then
      return points
    end
  end
  return nil
end

local function place_object_group(world, player, feature, rng)
  local definition = world.registry:get_world_object(feature.definition_id)
  local placed = {}
  if feature.kind == "scatter" then
    for _ = 1, feature.count do
      local point = select_group(world, player, { kind = "scatter", count = 1 }, definition, rng)
      if not point then break end
      local object, failure = world:place_object(feature.definition_id, point[1].x, point[1].y)
      assert(object, failure.reason)
      placed[#placed + 1] = object
    end
  else
    local points = select_group(world, player, feature, definition, rng)
    if points then
      for _, point in ipairs(points) do
        local object, failure = world:place_object(feature.definition_id, point.x, point.y)
        assert(object, failure.reason)
        placed[#placed + 1] = object
      end
    end
  end
  return placed
end

local function stream_cells(world, player, count, rng)
  local starts = rng:shuffle(candidates(world, player))
  for index = 1, math.min(#starts, 16) do
    local current, cells, seen = starts[index], {}, {}
    for _ = 1, count do
      if not point_is_free(world, current, player) or seen[key(current)] then break end
      cells[#cells + 1], seen[key(current)] = { x = current.x, y = current.y }, true
      local next_points = {}
      for _, direction in ipairs(CARDINAL) do
        local point = { x = current.x + direction[1], y = current.y + direction[2] }
        if point_is_free(world, point, player) and not seen[key(point)] then next_points[#next_points + 1] = point end
      end
      if #next_points == 0 then break end
      current = rng:choice(next_points)
    end
    if #cells >= math.max(3, math.floor(count * 0.6)) then return cells end
  end
  return {}
end

local function place_stream(world, player, feature, rng)
  local cells = stream_cells(world, player, feature.count, rng)
  local placed = {}
  if feature.kind == "stream" then
    local liquid = world.registry:get_liquid(feature.liquid_id)
    for _, point in ipairs(cells) do
      local result = world:add_liquid(point.x, point.y, liquid.id, liquid.max_depth)
      if result.applied then placed[#placed + 1] = { x = point.x, y = point.y } end
    end
  else
    for _, point in ipairs(cells) do
      local object, failure = world:place_object(feature.definition_id, point.x, point.y)
      assert(object, failure.reason)
      placed[#placed + 1] = object
    end
  end
  return placed
end

function Landmarks.place(world, terrain, player, rng, layout_features)
  local result = {}
  for _, feature in ipairs(layout_features or {}) do
    result[#result + 1] = {
      id = feature.id, kind = feature.kind, source = "terrain_layout",
      x = feature.x, y = feature.y, entrance_x = feature.entrance_x, entrance_y = feature.entrance_y,
    }
  end
  for _, feature in ipairs(Profiles[terrain] or {}) do
    local placed
    if feature.kind == "stream" or feature.kind == "stream_object" then
      placed = place_stream(world, player, feature, rng:derive(feature.id))
    else
      placed = place_object_group(world, player, feature, rng:derive(feature.id))
    end
    local objects, cells = {}, {}
    for _, value in ipairs(placed) do
      if value.id then objects[#objects + 1] = value.id else cells[#cells + 1] = { x = value.x, y = value.y } end
    end
    result[#result + 1] = {
      id = feature.id,
      kind = feature.kind,
      source = "terrain_landmark",
      object_ids = objects,
      cells = cells,
      count = #placed,
    }
  end
  return result
end

return Landmarks
