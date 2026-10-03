local Campaign = require("src.campaign.campaign")
local CampaignPersistence = require("src.persistence.campaign")
local SaveStore = require("src.persistence.save_store")
local WorldContent = require("src.campaign.world_content")
local ZoneKey = require("src.campaign.zone_key")
local Json = require("src.persistence.json")

local function fingerprint(plan)
  return assert(Json.encode(plan))
end

local function campaign(seed)
  return Campaign.new({ seed = seed, campaign_id = string.format("campaign:%06d", seed) })
end

return {
  {
    name = "world-content plans are deterministic, complete, and protect the start",
    run = function()
      local first, second = campaign(991101), campaign(991101)
      assert(fingerprint(first.state.world_content_plan) == fingerprint(second.state.world_content_plan))
      local summary = WorldContent.summary(first.state.world_content_plan)
      assert(summary.services == 5 and summary.ruins == 3 and summary.reactors == 2)
      assert(summary.bosses == 7 and summary.discoveries == 8)
      for _, site in ipairs(first.state.world_content_plan.sites) do
        local key = site.zone_key or site.surface_key
        if key then assert(ZoneKey.encode(ZoneKey.from_data(key)) ~= "zone:0:0:0") end
      end
    end,
  },
  {
    name = "world-content plan persists in a campaign manifest without a zone snapshot copy",
    run = function()
      local source, directory = campaign(991102), SaveStore.memory_directory()
      assert(CampaignPersistence.save(source, directory))
      local manifest = assert(CampaignPersistence.decode_manifest(assert(directory:file("manifest.json"):read())))
      assert(manifest.world_content_plan and manifest.world_content_plan.version == 1)
      local restored = assert(CampaignPersistence.load(directory))
      assert(fingerprint(restored.state.world_content_plan) == fingerprint(source.state.world_content_plan))
      local shard = assert(restored:to_zone_data())
      assert(shard.simulation.world_content_plan == nil)
    end,
  },
  {
    name = "old campaign manifests gain a compatible plan without rewriting the current visited profile",
    run = function()
      local source = campaign(991103)
      local manifest, shard = source:to_manifest_data(), source:to_zone_data()
      manifest.world_content_plan = nil
      local restored = Campaign.from_data(manifest, shard)
      assert(restored.state.world_content_plan and restored.active_zone.profile_id == "zone_profile.legacy.forest")
      assert(restored:validate())
    end,
  },
  {
    name = "planned authored zones generate persistent services discoveries and bosses without route completion",
    run = function()
      local value = campaign(991104)
      local service, boss
      for _, site in ipairs(value.state.world_content_plan.sites) do
        if site.type == "service_outpost" then service = service or site end
        if site.type == "boss_site" then boss = boss or site end
      end
      local service_record = value:_new_record(ZoneKey.from_data(service.zone_key))
      local service_session = select(1, value:_generate_zone_session(service_record))
      local kiosk_count = 0
      for _, object in ipairs(service_session.state.world:list_objects()) do if object.interaction_role == "service" then kiosk_count = kiosk_count + 1 end end
      assert(kiosk_count >= 1)
      local boss_record = value:_new_record(ZoneKey.from_data(boss.zone_key))
      local boss_session = select(1, value:_generate_zone_session(boss_record))
      assert(boss_session.state.boss and boss_session.state.boss.boss_id == boss.boss_id)
      local defeated = boss_session:_defeat_boss(boss_session.state.boss)
      assert(defeated and boss_session.state.boss == nil and boss_session.state.phase == "combat")
      assert(not boss_session.state.ended and boss_session.state.exit == nil)
    end,
  },
  {
    name = "boss-lair shard save reload retains damaged boss state and the campaign encounter phase",
    run = function()
      local seed, origin = 991105, campaign(991105)
      local site
      for _, value in ipairs(origin.state.world_content_plan.sites) do if value.type == "boss_site" then site = value; break end end
      local value = Campaign.new({ seed = seed, campaign_id = "campaign:991106", current_zone = ZoneKey.from_data(site.zone_key),
        world_content_plan = origin.state.world_content_plan })
      value.session.state.boss.health = value.session.state.boss.health - 3
      local directory = SaveStore.memory_directory()
      value:set_persistence_directory(directory)
      assert(CampaignPersistence.save(value, directory))
      local restored = assert(CampaignPersistence.load(directory))
      assert(restored.session.state.phase == "boss" and restored.session.state.campaign_boss_site_id == site.id)
      assert(restored.session.state.boss and restored.session.state.boss.health == value.session.state.boss.health)
      assert(not restored.session.state.ended and restored.session.state.exit == nil)
    end,
  },
}
