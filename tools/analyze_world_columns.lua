-- Headless OW-03 validator for deterministic vertical campaign columns.
-- It deliberately uses campaign generation/transition code, never LÖVE or a
-- user's save directory.
package.path = "./?.lua;./?/init.lua;" .. package.path

local Campaign = require("src.campaign.campaign")
local WorldTopology = require("src.campaign.world_topology")
local ZoneKey = require("src.campaign.zone_key")
local Grid = require("src.world.grid")
local SaveStore = require("src.persistence.save_store")
local CampaignPersistence = require("src.persistence.campaign")
local Json = require("src.persistence.json")

local function parse(arguments)
  local result, index = { seed = 1, columns = 1, full_zones = 0, transitions = 0 }, 1
  while arguments[index] do
    local flag, value = arguments[index], arguments[index + 1]
    if flag == "--help" or flag == "-h" then return nil, "help" end
    local keys = { ["--seed"] = "seed", ["--columns"] = "columns", ["--full-zones"] = "full_zones", ["--transitions"] = "transitions" }
    if not keys[flag] or not value then
      return nil, "Usage: --seed N [--columns N] [--full-zones N] [--transitions N]"
    end
    result[keys[flag]] = tonumber(value)
    if not result[keys[flag]] or result[keys[flag]] % 1 ~= 0 or result[keys[flag]] < 0 then
      return nil, "Invalid value for " .. flag
    end
    index = index + 2
  end
  return result
end

local function fail(failures, message)
  failures[#failures + 1] = message
end

local function column(index)
  return ((index * 5) % 9) - 4, ((index * 7) % 9) - 4
end

local function endpoint_reachable(world, cell)
  if not world:is_passable(cell.x, cell.y) then return false end
  local seen, queue, cursor = { [Grid.key(cell.x, cell.y)] = true }, { cell }, 1
  while queue[cursor] do
    local current = queue[cursor]
    cursor = cursor + 1
    for _, next_cell in ipairs(Grid.neighbours(current)) do
      local id = Grid.key(next_cell.x, next_cell.y)
      if Grid.in_bounds(next_cell.x, next_cell.y) and world:is_passable(next_cell.x, next_cell.y) and not seen[id] then
        seen[id] = true
        queue[#queue + 1] = next_cell
      end
    end
  end
  return #queue > 2
end

local function metadata_check(seed, x, y, failures)
  local surface = ZoneKey.new(x, y, 0)
  assert(WorldTopology.is_zone_in_bounds(surface))
  for _, key in ipairs({ surface, ZoneKey.new(x, y, -1), ZoneKey.new(x, y, -2), ZoneKey.new(x, y, 1) }) do
    local connections = WorldTopology.connections(seed, key)
    for direction, data in pairs(connections) do
      local connection = WorldTopology.connection_from_data(data)
      if not WorldTopology.is_zone_in_bounds(connection.destination) then
        fail(failures, "out-of-bounds connection " .. connection.id)
      end
      local reciprocal = WorldTopology.connection(seed, connection.destination, WorldTopology.opposite(direction))
      if not reciprocal or reciprocal.id ~= connection.id or reciprocal.connection_type ~= connection.connection_type then
        fail(failures, "nonreciprocal connection " .. connection.id)
      end
      if WorldTopology.is_vertical(direction) then
        if not Grid.in_bounds(connection.cell.x, connection.cell.y) then fail(failures, "bad vertical endpoint " .. connection.id) end
      end
    end
  end
  local cave = WorldTopology.has_surface_cave(seed, x, y)
  local down = WorldTopology.connection(seed, surface, "down") ~= nil
  if cave ~= down then fail(failures, "surface cave classification mismatch " .. ZoneKey.encode(surface)) end
  if WorldTopology.has_deep_cave(seed, x, y) and not cave then fail(failures, "orphan deep cave classification") end
end

local function generated_check(seed, number, failures)
  local x, y = column(number)
  local z = ({ 0, -1, -2 })[(number % 3) + 1]
  local key = ZoneKey.new(x, y, z)
  local campaign = Campaign.new({ seed = seed, campaign_id = string.format("campaign:%06d", 930000 + number), current_zone = key })
  local world = campaign.session.state.world
  local ids = {}
  for _, values in ipairs({ world:list_objects(true), world:list_hazards(true), world:list_fires(true) }) do
    for _, value in ipairs(values) do
      if ids[value.id] then fail(failures, "duplicate ID " .. value.id) end
      ids[value.id] = true
    end
  end
  for direction, data in pairs(campaign.active_zone.connections) do
    local connection = WorldTopology.connection_from_data(data)
    if WorldTopology.is_vertical(direction) then
      local object = world:object_at(connection.cell.x, connection.cell.y)
      if not object or object.zone_connection_id ~= connection.id or not endpoint_reachable(world, connection.cell) then
        fail(failures, "bad vertical landmark " .. connection.id)
      end
    end
  end
  local ok, reason = pcall(function() campaign:validate() end)
  if not ok then fail(failures, "campaign validation " .. tostring(reason)) end
end

local function fingerprint(campaign)
  local simulation = campaign:to_zone_data().simulation
  return assert(Json.encode({ world = simulation.world, enemies = simulation.enemies, targets = simulation.targets,
    corpses = simulation.corpses, bullets = simulation.bullets, bombs = simulation.bombs, flares = simulation.flares,
    torches = simulation.torches, area_attacks = simulation.area_attacks, rng = simulation.rng }))
end

local function vertical_object(campaign, direction)
  for _, object in ipairs(campaign.session.state.world:list_objects()) do
    if object.interaction_role == "zone_connection" and object.zone_connection_direction == direction then return object end
  end
end

local function use(campaign, direction)
  local object = assert(vertical_object(campaign, direction), "missing vertical landmark")
  local player = campaign.session.state.player
  player.x, player.y = object.x + 1, object.y
  return campaign.session:turn("interact")
end

local function transition_check(seed, number, failures)
  local directory = SaveStore.memory_directory()
  local campaign = Campaign.new({ seed = seed, campaign_id = string.format("campaign:%06d", 940000 + number) })
  campaign:set_persistence_directory(directory)
  if not CampaignPersistence.save(campaign, directory) then return fail(failures, "initial save failed") end
  local surface_before = fingerprint(campaign)
  local first = use(campaign, "down")
  if first ~= "zone_transition" then return fail(failures, "surface descent failed") end
  local cave_before = fingerprint(campaign)
  local second = use(campaign, "down")
  if second ~= "zone_transition" then return fail(failures, "deep descent failed") end
  if number % 4 == 0 then
    campaign = assert(CampaignPersistence.load(directory))
    campaign:set_persistence_directory(directory)
  end
  if use(campaign, "up") ~= "zone_transition" then return fail(failures, "deep-to-cave return failed") end
  if use(campaign, "up") ~= "zone_transition" then return fail(failures, "cave-to-surface return failed") end
  if fingerprint(campaign) ~= surface_before then fail(failures, "surface advanced offscreen") end
  if use(campaign, "down") ~= "zone_transition" then return fail(failures, "cave revisit failed") end
  if fingerprint(campaign) ~= cave_before then fail(failures, "cave advanced offscreen") end
  local ok, reason = pcall(function() campaign:validate() end)
  if not ok then fail(failures, "post-transition validation " .. tostring(reason)) end
end

local options, error_message = parse(arg or {})
if not options then
  io.stderr:write((error_message == "help" and "" or "Error: " .. error_message .. "\n")
    .. "Usage: luajit tools/analyze_world_columns.lua --seed N [--columns 200] [--full-zones 150] [--transitions 200]\n")
  os.exit(error_message == "help" and 0 or 2)
end

local failures = {}
for index = 0, options.columns - 1 do
  local x, y = column(index)
  metadata_check(options.seed + index, x, y, failures)
end
for index = 0, options.full_zones - 1 do generated_check(options.seed + index, index, failures) end
for index = 1, options.transitions do
  -- Progress stays on stderr so the final stdout contract remains one concise
  -- machine-readable summary; it also identifies an exact failing seed in a
  -- long headless validation run.
  io.stderr:write("transition-scenario=", index, "\n")
  transition_check(options.seed + index, index, failures)
end

io.write(string.format("world-columns columns=%d full-zones=%d transitions=%d failures=%d\n",
  options.columns, options.full_zones, options.transitions, #failures))
for _, message in ipairs(failures) do io.write("  ", message, "\n") end
if #failures > 0 then os.exit(1) end
