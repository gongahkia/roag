-- Headless OW-06 campaign-content validator. It checks semantic plans before
-- zones are visited, then samples the actual persistent generators that will
-- materialize those plans. No LÖVE or user persistence directory is used.
package.path = "./?.lua;./?/init.lua;" .. package.path

local Campaign = require("src.campaign.campaign")
local WorldContent = require("src.campaign.world_content")
local ZoneKey = require("src.campaign.zone_key")
local Registry = require("src.content.registry")

local function parse(arguments)
  local result, index = { seed = 1, campaigns = 1, zones = 0 }, 1
  while arguments[index] do
    local flag, value = arguments[index], arguments[index + 1]
    if flag == "--help" or flag == "-h" then return nil, "help" end
    local name = ({ ["--seed"] = "seed", ["--campaigns"] = "campaigns", ["--zones"] = "zones" })[flag]
    if not name or not value then return nil, "Usage: --seed N [--campaigns N] [--zones N]" end
    result[name] = tonumber(value)
    if not result[name] or result[name] < 0 or result[name] % 1 ~= 0 then return nil, "Invalid value for " .. flag end
    index = index + 2
  end
  return result
end

local function plan_fingerprint(plan)
  local parts = { tostring(plan.version) }
  for _, site in ipairs(plan.sites) do
    parts[#parts + 1] = site.id
    if site.zone_key then parts[#parts + 1] = ZoneKey.encode(ZoneKey.from_data(site.zone_key)) end
    if site.surface_key then parts[#parts + 1] = ZoneKey.encode(ZoneKey.from_data(site.surface_key)) end
    parts[#parts + 1] = site.boss_id or site.discovery_id or ""
  end
  for _, link in ipairs(plan.vertical_links) do
    parts[#parts + 1] = link.id .. ":" .. ZoneKey.encode(ZoneKey.from_data(link.source))
      .. ">" .. ZoneKey.encode(ZoneKey.from_data(link.destination)) .. ":" .. link.connection_type
  end
  return table.concat(parts, "|")
end

local function structural_plan_check(plan)
  local summary = WorldContent.summary(plan)
  assert(summary.services == 5, "expected five service outposts")
  assert(summary.ruins == 3, "expected three ruin complexes")
  assert(summary.reactors == 2, "expected two reactor complexes")
  assert(summary.bosses == 7, "expected seven boss sites")
  assert(summary.discoveries == 8, "expected eight discovery sites")
  local early = false
  for _, site in ipairs(plan.sites) do
    if site.type == "service_outpost" then
      local key = ZoneKey.from_data(site.zone_key)
      if math.abs(key.world_x) + math.abs(key.world_y) <= 2 then early = true end
    end
  end
  assert(early, "plan has no early service outpost")
end

local options, error_message = parse(arg or {})
if not options then
  io.stderr:write((error_message == "help" and "" or "Error: " .. error_message .. "\n")
    .. "Usage: luajit tools/analyze_world_content.lua --seed 1234 --campaigns 200 --zones 300\n")
  os.exit(error_message == "help" and 0 or 2)
end

local registry, failures, generated = Registry.load(), {}, 0
for index = 0, options.campaigns - 1 do
  local seed = options.seed + index
  local first = WorldContent.new(seed, registry)
  local second = WorldContent.new(seed, registry)
  local ok, reason = pcall(function()
    WorldContent.validate(first, registry)
    structural_plan_check(first)
    assert(plan_fingerprint(first) == plan_fingerprint(second), "visit-order plan fingerprint drift")
  end)
  if not ok then failures[#failures + 1] = string.format("plan seed=%d: %s", seed, tostring(reason)) end
end

for index = 0, options.zones - 1 do
  local seed = options.seed + index
  local campaign = Campaign.new({ seed = seed, campaign_id = string.format("campaign:%06d", 980000 + index), registry = registry })
  local zones, seen = {}, {}
  for _, site in ipairs(campaign.state.world_content_plan.sites) do
    for _, key in ipairs({ site.zone_key, site.surface_key }) do
      if key and not seen[ZoneKey.encode(ZoneKey.from_data(key))] then zones[#zones + 1], seen[ZoneKey.encode(ZoneKey.from_data(key))] = { site = site, key = key }, true end
    end
    for _, key in ipairs(site.interior_keys or {}) do
      if not seen[ZoneKey.encode(ZoneKey.from_data(key))] then zones[#zones + 1], seen[ZoneKey.encode(ZoneKey.from_data(key))] = { site = site, key = key }, true end
    end
  end
  local selected = zones[(index % #zones) + 1]
  if selected then
    local key = selected.key
    local record = campaign:zone_record(ZoneKey.from_data(key)) or campaign:_new_record(ZoneKey.from_data(key))
    local ok, generated_session = pcall(function() return campaign:_generate_zone_session(record) end)
    if not ok then failures[#failures + 1] = string.format("zone seed=%d site=%s: %s", seed, selected.site.id, tostring(generated_session))
    else
      local valid, reason = pcall(function() generated_session:validate_world(); generated_session:validate_physical_ownership() end)
      if not valid then failures[#failures + 1] = string.format("zone validation seed=%d site=%s: %s", seed, selected.site.id, tostring(reason)) end
      generated = generated + 1
    end
  end
end

io.write(string.format("world-content plans=%d zones=%d failures=%d\n", options.campaigns, generated, #failures))
for _, failure in ipairs(failures) do io.write("  ", failure, "\n") end
if #failures > 0 then os.exit(1) end
