-- Persistent OW-06 content scenarios. These use campaign-zone shards and the
-- normal save codec, not a renderer or a user's writable save directory.
package.path = "./?.lua;./?/init.lua;" .. package.path

local Campaign = require("src.campaign.campaign")
local CampaignPersistence = require("src.persistence.campaign")
local SaveStore = require("src.persistence.save_store")
local ZoneKey = require("src.campaign.zone_key")
local Building = require("src.construction.building")
local Grid = require("src.world.grid")

local counts = { services = 1, discoveries = 1, bosses = 1, succession = 1, construction = 1, seed = 990000 }
for index = 1, #arg do
  local keys = { ["--services"] = "services", ["--discoveries"] = "discoveries", ["--bosses"] = "bosses",
    ["--succession"] = "succession", ["--construction"] = "construction", ["--seed"] = "seed" }
  if keys[arg[index]] then counts[keys[arg[index]]] = assert(tonumber(arg[index + 1])) end
end

local function plan_campaign(seed)
  return Campaign.new({ seed = seed, campaign_id = string.format("campaign:%06d", seed) })
end

local function site(campaign, kind, predicate)
  for _, value in ipairs(campaign.state.world_content_plan.sites) do
    if value.type == kind and (not predicate or predicate(value)) then return value end
  end
  error("Missing world-content site " .. kind)
end

local function campaign_at(seed, target, key)
  return Campaign.new({ seed = seed, campaign_id = string.format("campaign:%06d", seed + 500000),
    current_zone = ZoneKey.from_data(key), world_content_plan = target.state.world_content_plan })
end

local function saved(campaign)
  local directory = SaveStore.memory_directory()
  campaign:set_persistence_directory(directory)
  assert(CampaignPersistence.save(campaign, directory))
  return assert(CampaignPersistence.load(directory))
end

local function add_resources(session)
  for _, resource in ipairs({ "resource.material.timber", "resource.material.masonry", "resource.material.metal" }) do
    local remaining, maximum = 20, session.registry:get_resource(resource).max_stack
    while remaining > 0 do
      local quantity = math.min(remaining, maximum)
      assert(session.state.inventory:auto_place(session:create_resource_stack(resource, quantity, "campaign")))
      remaining = remaining - quantity
    end
  end
end

local function build(session)
  for _, point in ipairs(session:_reachable_floor_cells()) do
    session.state.player.x, session.state.player.y = point.x, point.y
    for _, cell in ipairs(Grid.neighbours(point)) do
      local result = Building.place(session, "construction.barricade", cell.x, cell.y)
      if result.applied then return result.object_id end
    end
  end
  error("No legal construction position in POI")
end

local failures = 0
local function scenario(label, count, run)
  for index = 1, count do
    local ok, reason = xpcall(function() run(counts.seed + index * 37, index) end, debug.traceback)
    if not ok then failures = failures + 1; io.write("FAIL ", label, " ", index, " ", tostring(reason), "\n") end
  end
end

scenario("service", counts.services, function(seed)
  local origin = plan_campaign(seed)
  local outpost = site(origin, "service_outpost", function(value) return value.service_ids[1] == "service.supply.legacy" end)
  local campaign = campaign_at(seed, origin, outpost.zone_key)
  local kiosk
  for _, object in ipairs(campaign.session.state.world:list_objects()) do
    if object.interaction_role == "service" and object.service_id == "service.supply.legacy" then kiosk = object; break end
  end
  assert(kiosk)
  campaign.session.state.scrap = 99
  assert(campaign.session:open_service(kiosk.id).applied)
  assert(campaign.session:service_execute(campaign.session:service_options()[1]).applied)
  local remaining = kiosk.service_stock.offers[1].remaining
  assert(campaign.session:close_service().applied)
  local restored = saved(campaign)
  assert(restored.session.state.world:get_object(kiosk.id).service_stock.offers[1].remaining == remaining)
end)

scenario("discovery", counts.discoveries, function(seed)
  local origin = plan_campaign(seed)
  local target = site(origin, "discovery_site", function(value) return value.discovery_id == "discovery.forest.survey_cache" end)
  local campaign = campaign_at(seed, origin, target.zone_key)
  local cache
  for _, object in ipairs(campaign.session.state.world:list_objects()) do if object.interaction_role == "discovery" then cache = object; break end end
  assert(cache and campaign.session:claim_discovery(cache).applied)
  local restored = saved(campaign)
  assert(restored.session.state.world:get_object(cache.id).discovery_claimed == true)
end)

scenario("boss", counts.bosses, function(seed, index)
  local origin = plan_campaign(seed)
  local boss_sites = {}
  for _, value in ipairs(origin.state.world_content_plan.sites) do if value.type == "boss_site" then boss_sites[#boss_sites + 1] = value end end
  local target = boss_sites[((index - 1) % #boss_sites) + 1]
  local campaign = campaign_at(seed, origin, target.zone_key)
  campaign.session.state.boss.health = campaign.session.state.boss.health - 2
  local hp = campaign.session.state.boss.health
  local restored = saved(campaign)
  assert(restored.session.state.phase == "boss" and restored.session.state.boss.health == hp)
  assert(restored.session:_defeat_boss(restored.session.state.boss))
  local dead = saved(restored)
  assert(dead.session.state.boss == nil and dead.session.state.boss_completed == target.boss_id)
end)

scenario("succession", counts.succession, function(seed, index)
  local origin = plan_campaign(seed)
  local boss_sites = {}
  for _, value in ipairs(origin.state.world_content_plan.sites) do if value.type == "boss_site" then boss_sites[#boss_sites + 1] = value end end
  local target = boss_sites[((index - 1) % #boss_sites) + 1]
  local campaign = campaign_at(seed, origin, target.zone_key)
  campaign:set_persistence_directory(SaveStore.memory_directory())
  assert(CampaignPersistence.save(campaign, campaign.persistence_directory))
  campaign.session.state.boss.health = campaign.session.state.boss.health - 1
  local hp = campaign.session.state.boss.health
  assert(campaign:handle_player_death({ cause = "validation" }).applied)
  assert(campaign.session.state.player and campaign.session.state.boss and campaign.session.state.boss.health == hp)
  campaign:validate()
end)

scenario("construction", counts.construction, function(seed)
  local origin = plan_campaign(seed)
  local ruin = site(origin, "ruin_complex")
  local campaign = campaign_at(seed, origin, ruin.interior_keys[1])
  add_resources(campaign.session)
  local object_id = build(campaign.session)
  local restored = saved(campaign)
  assert(restored.session.state.world:get_object(object_id))
end)

io.write(string.format("world-content persistence services=%d discoveries=%d bosses=%d succession=%d construction=%d failures=%d\n",
  counts.services, counts.discoveries, counts.bosses, counts.succession, counts.construction, failures))
if failures > 0 then os.exit(1) end
