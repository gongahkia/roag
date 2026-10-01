-- Deterministic v1 authored-room dungeon assembly.  Graph construction is
-- deliberately small and inspectable: a connected branchy chunk tree, exact
-- connector matching, and no procedural-rectangle fallback on failure.
local Config = require("src.rooms.config")
local Registry = require("src.rooms.registry")
local Template = require("src.rooms.template")
local Grid = require("src.world.grid")

local DungeonRooms = {}

local DIRECTIONS = {
  north = { 0, 1, "south" }, east = { 1, 0, "west" },
  south = { 0, -1, "north" }, west = { -1, 0, "east" },
}

local function slot_key(x, y)
  return x .. ":" .. y
end

local function sorted_slots(slots)
  local result = {}
  for _, slot in pairs(slots) do result[#result + 1] = slot end
  table.sort(result, function(a, b) return a.y == b.y and a.x < b.x or a.y < b.y end)
  return result
end

local function available_sides(slot, slots, include_occupied)
  local result = {}
  for _, side in ipairs(Config.SIDE_ORDER) do
    local delta = DIRECTIONS[side]
    local x, y = slot.x + delta[1], slot.y + delta[2]
    if x >= 0 and x < Config.GRID_WIDTH and y >= 0 and y < Config.GRID_HEIGHT then
      local occupied = slots[slot_key(x, y)] ~= nil
      if occupied == include_occupied then result[#result + 1] = side end
    end
  end
  return result
end

local function add_edge(a, b)
  local dx, dy = b.x - a.x, b.y - a.y
  local side = dx == 1 and "east" or dx == -1 and "west" or dy == 1 and "north" or "south"
  a.links[side] = true
  b.links[DIRECTIONS[side][3]] = true
end

local function make_graph(rng)
  local slots = {}
  local entrance = { x = 3, y = 1, links = {}, entrance = true }
  slots[slot_key(entrance.x, entrance.y)] = entrance
  -- Entrance always has a two-link corner pattern. It therefore always has a
  -- matching entrance template while retaining four rotated starting shapes.
  local corners = {
    { "north", "east" }, { "north", "west" }, { "south", "east" }, { "south", "west" },
  }
  local initial = rng:choice(corners)
  for _, side in ipairs(initial) do
    local delta = DIRECTIONS[side]
    local child = { x = entrance.x + delta[1], y = entrance.y + delta[2], links = {} }
    slots[slot_key(child.x, child.y)] = child
    add_edge(entrance, child)
  end
  while #sorted_slots(slots) < Config.ROOM_COUNT do
    local options = {}
    for _, slot in ipairs(sorted_slots(slots)) do
      if not slot.entrance then
        for _, side in ipairs(available_sides(slot, slots, false)) do options[#options + 1] = { slot = slot, side = side } end
      end
    end
    if #options == 0 then
      return nil, { code = "graph_capacity", reason = "Dungeon room graph has no open chunk edge" }
    end
    local choice = rng:choice(options)
    local delta = DIRECTIONS[choice.side]
    local child = { x = choice.slot.x + delta[1], y = choice.slot.y + delta[2], links = {} }
    slots[slot_key(child.x, child.y)] = child
    add_edge(choice.slot, child)
  end
  -- A restrained optional loop keeps template dungeons from collapsing into
  -- one tree shape forever. Entrance links remain fixed to the entrance
  -- template's exact two-connector contract; all other supported patterns
  -- are corpus-validated before this generator can run.
  local loop_options = {}
  for _, slot in ipairs(sorted_slots(slots)) do
    if not slot.entrance then
      for _, side in ipairs({ "north", "east" }) do
        local delta = DIRECTIONS[side]
        local neighbour = slots[slot_key(slot.x + delta[1], slot.y + delta[2])]
        if neighbour and not neighbour.entrance and not slot.links[side] then
          loop_options[#loop_options + 1] = { from = slot, to = neighbour }
        end
      end
    end
  end
  if #loop_options > 0 and rng:float() < 0.35 then
    local loop = rng:choice(loop_options)
    add_edge(loop.from, loop.to)
  end
  return slots, entrance
end

local function required_sides(slot)
  local result = {}
  for _, side in ipairs(Config.SIDE_ORDER) do if slot.links[side] then result[#result + 1] = side end end
  return result
end

local function choose_weighted(rng, candidates)
  local total = 0
  for _, candidate in ipairs(candidates) do total = total + candidate.weight end
  local needle, running = rng:float() * total, 0
  for _, candidate in ipairs(candidates) do
    running = running + candidate.weight
    if needle < running then return candidate end
  end
  return candidates[#candidates]
end

local function local_spawn(template)
  local ideal_x, ideal_y, best = math.floor(template.width / 2), math.floor(template.height / 2), nil
  for x = 1, template.width - 2 do
    for y = 1, template.height - 2 do
      local glyph = Template.glyph_at(template, x, y)
      local material_id = template.legend[glyph]
      if material_id == "material.terrain.air" then
        local distance = math.abs(x - ideal_x) + math.abs(y - ideal_y)
        if not best or distance < best.distance then best = { x = x, y = y, distance = distance } end
      end
    end
  end
  return best
end

local function graph_edges(slots)
  local result = {}
  for _, slot in ipairs(sorted_slots(slots)) do
    for _, side in ipairs({ "north", "east" }) do
      if slot.links[side] then
        local delta = DIRECTIONS[side]
        result[#result + 1] = { from = { x = slot.x, y = slot.y }, to = { x = slot.x + delta[1], y = slot.y + delta[2] }, side = side }
      end
    end
  end
  return result
end

function DungeonRooms.generate(start, rng, options)
  options = options or {}
  local room_registry, registry_failure = options.room_registry or Registry.load({ registry = options.registry })
  if not room_registry then return nil, registry_failure end
  local slots, entrance = make_graph(rng)
  if not slots then return nil, entrance end
  local selected, layout, room_entries, cell_provenance = {}, {}, {}, {}
  for _, slot in ipairs(sorted_slots(slots)) do
    local required = required_sides(slot)
    local tags = slot.entrance and { "entrance" } or nil
    local excluded_tags = nil
    if not slot.entrance then excluded_tags = { "entrance" } end
    local candidates = room_registry:candidates(required, tags, excluded_tags)
    if #candidates == 0 then
      return nil, {
        code = "missing_template", reason = "No room template matches " .. Template.pattern_key(required),
        slot = { x = slot.x, y = slot.y }, required_connectors = required, corpus_size = #room_registry.order,
      }
    end
    -- Adjacent identical transformed rooms are avoided when the valid corpus
    -- offers another candidate; this is purely cosmetic and never overrides
    -- connector correctness.
    local adjacent = {}
    for _, side in ipairs(Config.SIDE_ORDER) do
      local delta = DIRECTIONS[side]
      local neighbour = selected[slot_key(slot.x + delta[1], slot.y + delta[2])]
      if neighbour then adjacent[neighbour.base_id .. "@" .. neighbour.rotation] = true end
    end
    local alternatives = {}
    for _, candidate in ipairs(candidates) do
      if not adjacent[candidate.base_id .. "@" .. candidate.rotation] then alternatives[#alternatives + 1] = candidate end
    end
    local chosen = choose_weighted(rng, #alternatives > 0 and alternatives or candidates)
    selected[slot_key(slot.x, slot.y)] = chosen
    slot.template_id, slot.rotation = chosen.base_id, chosen.rotation
    local origin_x, origin_y = Config.ORIGIN_X + slot.x * Config.WIDTH, Config.ORIGIN_Y + slot.y * Config.HEIGHT
    local entry = {
      slot = { x = slot.x, y = slot.y }, template_id = chosen.base_id, rotation = chosen.rotation,
      origin = { x = origin_x, y = origin_y }, connectors = {}, required_connectors = required,
    }
    for _, connector in ipairs(chosen.connectors) do
      local local_x, local_y = Template.connector_position(chosen, connector)
      entry.connectors[#entry.connectors + 1] = { side = connector.side, offset = connector.offset,
        x = origin_x + local_x, y = origin_y + local_y }
    end
    for local_x = 0, Config.WIDTH - 1 do
      for local_y = 0, Config.HEIGHT - 1 do
        local glyph = Template.glyph_at(chosen, local_x, local_y)
        local material_id = chosen.legend[glyph]
        local material = room_registry.registry:get_material(material_id)
        local world_x, world_y = origin_x + local_x, origin_y + local_y
        if not material.blocks_movement then layout[Grid.key(world_x, world_y)] = true end
        cell_provenance[Grid.key(world_x, world_y)] = {
          slot = { x = slot.x, y = slot.y }, template_id = chosen.base_id, rotation = chosen.rotation,
          local_x = local_x, local_y = local_y,
        }
      end
    end
    room_entries[#room_entries + 1] = entry
  end
  local entrance_template = selected[slot_key(entrance.x, entrance.y)]
  local spawn = local_spawn(entrance_template)
  if not spawn then return nil, { code = "invalid_entrance", reason = "Entrance template has no legal interior spawn" } end
  return layout, {
    generator = "dungeon_room_templates_v1",
    room_config = { width = Config.WIDTH, height = Config.HEIGHT, grid_width = Config.GRID_WIDTH, grid_height = Config.GRID_HEIGHT },
    rooms = room_entries,
    graph_edges = graph_edges(slots),
    cell_provenance = cell_provenance,
    player_spawn = { x = Config.ORIGIN_X + entrance.x * Config.WIDTH + spawn.x, y = Config.ORIGIN_Y + entrance.y * Config.HEIGHT + spawn.y },
  }
end

return DungeonRooms
