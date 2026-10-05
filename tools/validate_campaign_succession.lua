-- Headless OW-04 succession analyzer. It uses the exact campaign persistence
-- and travel paths; no LÖVE state or user save storage is opened.
package.path = "./?.lua;./?/init.lua;" .. package.path

local Campaign = require("src.campaign.campaign")
local CampaignPersistence = require("src.persistence.campaign")
local SaveStore = require("src.persistence.save_store")
local ZoneKey = require("src.campaign.zone_key")

local function parse(arguments)
  local result, index = { count = 200, seed = 960000 }, 1
  while arguments[index] do
    local flag, value = arguments[index], arguments[index + 1]
    if flag == "--help" or flag == "-h" then return nil, "help" end
    if (flag ~= "--count" and flag ~= "--seed") or not value then
      return nil, "Usage: luajit tools/validate_campaign_succession.lua [--count 200] [--seed N]"
    end
    local number = tonumber(value)
    if not number or number < 1 or number % 1 ~= 0 then return nil, "Invalid value for " .. flag end
    result[flag == "--count" and "count" or "seed"] = number
    index = index + 2
  end
  return result
end

local function descend(campaign)
  local connection = campaign.active_zone.connections.down
  if not connection then return false end
  local object = campaign.session.state.world:object_at(connection.cell.x, connection.cell.y)
  if not object then return false end
  local player = campaign.session.state.player
  player.x, player.y, player.direction = object.x + 1, object.y, "a"
  return campaign.session:turn("interact") == "zone_transition"
end

local options, error_message = parse(arg or {})
if not options then
  io.stderr:write((error_message == "help" and "" or "Error: " .. error_message .. "\n")
    .. "Usage: luajit tools/validate_campaign_succession.lua [--count 200] [--seed N]\n")
  os.exit(error_message == "help" and 0 or 2)
end

local failures = {}
for index = 1, options.count do
  local directory = SaveStore.memory_directory()
  local campaign = Campaign.new({ seed = options.seed + index, campaign_id = string.format("campaign:%06d", 960000 + index) })
  campaign:set_persistence_directory(directory)
  local ok, reason = CampaignPersistence.save(campaign, directory)
  if not ok then
    failures[#failures + 1] = "initial save " .. index .. ": " .. tostring(reason and reason.code)
  else
    if index % 3 == 0 then assert(descend(campaign), "surface descent failed") end
    if index % 5 == 0 then assert(descend(campaign), "deep descent failed") end
    local source_zone = ZoneKey.to_data(campaign.state.current_zone)
    local old = campaign.session.state.player
    local old_component = old.body:list_components()[1].id
    local succession, failure = campaign:handle_player_death({ cause = "analyzer" })
    if not succession then
      failures[#failures + 1] = "succession " .. index .. ": " .. tostring(failure and failure.code)
    else
      local corpse_zone = ZoneKey.from_data(source_zone)
      if campaign.state.body_death_count ~= 1 or campaign.session.state.player.body:list_components()[1].id == old_component
        or not campaign:zone_record(corpse_zone) then
        failures[#failures + 1] = "identity/state failure " .. index
      end
      if index % 2 == 0 then
        local loaded, load_error = CampaignPersistence.load(directory)
        if not loaded then failures[#failures + 1] = "reload " .. index .. ": " .. tostring(load_error and load_error.code)
        else
          loaded:set_persistence_directory(directory)
          local valid = pcall(function() loaded:validate() end)
          if not valid then failures[#failures + 1] = "validation after reload " .. index end
        end
      end
    end
  end
end

io.write(string.format("campaign-succession scenarios=%d failures=%d\n", options.count, #failures))
for _, failure in ipairs(failures) do io.write("  ", failure, "\n") end
if #failures > 0 then os.exit(1) end
