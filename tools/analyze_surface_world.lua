-- Headless OW-02 validation for finite surface metadata, generated connector
-- geometry, and staged player-only round trips. No LÖVE or user save storage.
package.path = "./?.lua;./?/init.lua;" .. package.path

local Campaign = require("src.campaign.campaign")
local SurfaceWorld = require("src.campaign.surface_world")
local ZoneKey = require("src.campaign.zone_key")
local SaveStore = require("src.persistence.save_store")
local CampaignPersistence = require("src.persistence.campaign")
local Json = require("src.persistence.json")

local function parse(arguments)
  local result, index = { seed = 1, radius = 4, campaigns = 1, full_zones = 0, transitions = 0 }, 1
  while arguments[index] do
    local flag, value = arguments[index], arguments[index + 1]
    if flag == "--help" or flag == "-h" then return nil, "help" end
    if not ({ ["--seed"] = true, ["--radius"] = true, ["--campaigns"] = true, ["--full-zones"] = true, ["--transitions"] = true })[flag] or not value then
      return nil, "Usage: --seed N [--radius 0..4] [--campaigns N] [--full-zones N] [--transitions N]"
    end
    local key = ({ ["--seed"] = "seed", ["--radius"] = "radius", ["--campaigns"] = "campaigns", ["--full-zones"] = "full_zones", ["--transitions"] = "transitions" })[flag]
    result[key] = tonumber(value)
    if not result[key] or result[key] % 1 ~= 0 or result[key] < 0 then return nil, "Invalid value for " .. flag end
    index = index + 2
  end
  if result.radius > 4 then return nil, "--radius must be within the finite -4..4 surface" end
  return result
end

local function fail(failures, message)
  failures[#failures + 1] = message
end

local function metadata_check(seed, radius, failures)
  for x = -radius, radius do
    for y = -radius, radius do
      local key = ZoneKey.new(x, y, 0)
      local connections = SurfaceWorld.connections(seed, key)
      for direction, connection in pairs(connections) do
        local destination = ZoneKey.from_data(connection.destination)
        if not SurfaceWorld.is_zone_in_bounds(destination) then fail(failures, "out-of-bounds connection " .. ZoneKey.encode(key)) end
        local reciprocal = SurfaceWorld.connection(seed, destination, SurfaceWorld.opposite(direction))
        if not reciprocal or reciprocal.id ~= connection.id then
          fail(failures, "nonreciprocal edge " .. connection.id)
        else
          -- Same shared offset is required, while each endpoint remains on
          -- its own opposite boundary side.
          if (direction == "east" or direction == "west") and reciprocal.boundary.y ~= connection.boundary.y then
            fail(failures, "mismatched horizontal edge offset " .. connection.id)
          elseif (direction == "north" or direction == "south") and reciprocal.boundary.x ~= connection.boundary.x then
            fail(failures, "mismatched vertical edge offset " .. connection.id)
          end
        end
      end
      if x == -4 and connections.west then fail(failures, "west boundary opened") end
      if x == 4 and connections.east then fail(failures, "east boundary opened") end
      if y == -4 and connections.north then fail(failures, "north boundary opened") end
      if y == 4 and connections.south then fail(failures, "south boundary opened") end
    end
  end
end

local function full_zone_check(seed, x, y, failures)
  local campaign = Campaign.new({ seed = seed, campaign_id = "campaign:900001", current_zone = ZoneKey.new(x, y, 0) })
  local world, record = campaign.session.state.world, campaign.active_zone
  local seen = {}
  for _, values in ipairs({ world:list_objects(true), world:list_hazards(true), world:list_fires(true) }) do
    for _, value in ipairs(values) do
      if seen[value.id] then fail(failures, "duplicate physical ID " .. value.id) end
      seen[value.id] = true
    end
  end
  for _, connection in pairs(record.connections) do
    if not world:is_passable(connection.boundary.x, connection.boundary.y)
      or not world:is_passable(connection.interior.x, connection.interior.y)
      or not SurfaceWorld.reachable_from_connection(world, connection)
      or not SurfaceWorld.connection_reaches_primary(campaign.session, connection) then
      fail(failures, "unreachable connector " .. connection.id)
    end
  end
  local permitted = {}
  for _, connection in pairs(record.connections) do permitted[connection.boundary.x .. ":" .. connection.boundary.y] = true end
  for x = 0, 79 do
    for _, y in ipairs({ 0, 49 }) do
      if world:is_passable(x, y) and not permitted[x .. ":" .. y] then fail(failures, "unexpected outer opening") end
    end
  end
  for y = 1, 48 do
    for _, x in ipairs({ 0, 79 }) do
      if world:is_passable(x, y) and not permitted[x .. ":" .. y] then fail(failures, "unexpected outer opening") end
    end
  end
end

local function encoded_source_state(campaign)
  local simulation = campaign:to_zone_data().simulation
  return assert(Json.encode({
    world = simulation.world, enemies = simulation.enemies, targets = simulation.targets,
    corpses = simulation.corpses, bullets = simulation.bullets, bombs = simulation.bombs,
    flares = simulation.flares, torches = simulation.torches, area_attacks = simulation.area_attacks,
  }))
end

local function transition_check(seed, number, failures)
  local directory = SaveStore.memory_directory()
  local campaign = Campaign.new({ seed = seed, campaign_id = string.format("campaign:%06d", 910000 + number) })
  campaign:set_persistence_directory(directory)
  if not CampaignPersistence.save(campaign, directory) then return fail(failures, "initial save failed") end
  local east = SurfaceWorld.connection_at(campaign.active_zone, "east")
  campaign.session.state.player.x, campaign.session.state.player.y = east.boundary.x, east.boundary.y
  local source = encoded_source_state(campaign)
  local entered, enter_error = campaign:transition("east")
  if not entered then return fail(failures, "east transition failed: " .. tostring(enter_error and enter_error.code)) end
  if number % 5 == 0 then
    -- Exact reload in the middle proves previously visited-zone metadata is
    -- sufficient to later return without generation or identity drift.
    campaign = assert(CampaignPersistence.load(directory))
    campaign:set_persistence_directory(directory)
  end
  local west = SurfaceWorld.connection_at(campaign.active_zone, "west")
  campaign.session.state.player.x, campaign.session.state.player.y = west.boundary.x, west.boundary.y
  local returned, return_error = campaign:transition("west")
  if not returned then return fail(failures, "west transition failed: " .. tostring(return_error and return_error.code)) end
  if encoded_source_state(campaign) ~= source then fail(failures, "source zone advanced offscreen") end
  local valid, validation_error = pcall(function() campaign:validate() end)
  if not valid then fail(failures, "ownership validation failed: " .. tostring(validation_error)) end
end

local options, error_message = parse(arg or {})
if not options then
  io.stderr:write((error_message == "help" and "" or "Error: " .. error_message .. "\n")
    .. "Usage: luajit tools/analyze_surface_world.lua --seed N [--radius 4] [--campaigns 100] [--full-zones 100] [--transitions 200]\n")
  os.exit(error_message == "help" and 0 or 2)
end

local failures = {}
for index = 0, options.campaigns - 1 do metadata_check(options.seed + index, options.radius, failures) end
for index = 0, options.full_zones - 1 do
  local x = (index % 9) - 4
  local y = (math.floor(index / 9) % 9) - 4
  full_zone_check(options.seed + index, x, y, failures)
end
for index = 1, options.transitions do transition_check(options.seed + index, index, failures) end

io.write(string.format("surface-world metadata campaigns=%d full-zones=%d transitions=%d failures=%d\n",
  options.campaigns, options.full_zones, options.transitions, #failures))
for _, message in ipairs(failures) do io.write("  ", message, "\n") end
if #failures > 0 then os.exit(1) end
