local Campaign = require("src.campaign.campaign")
local ZoneKey = require("src.campaign.zone_key")
local CampaignPersistence = require("src.persistence.campaign")
local SaveStore = require("src.persistence.save_store")
local MetaProfile = require("src.persistence.meta_profile")
local Json = require("src.persistence.json")
local Grid = require("src.world.grid")
local ActiveRun = require("src.persistence.active_run")
local Session = require("src.simulation.session")
local App = require("src.app.app")

local function encoded(value)
  return assert(Json.encode(value))
end

local function campaign(seed, id)
  return Campaign.new({ seed = seed or 611001, campaign_id = id or "campaign:000001" })
end

local function open_cell(world, avoid)
  for x = 1, Grid.width - 2 do
    for y = 1, Grid.height - 2 do
      if world:is_passable(x, y) and not world:is_hazardous(x, y)
        and (not avoid or x ~= avoid.x or y ~= avoid.y) then return { x = x, y = y } end
    end
  end
  error("No open cell in campaign fixture")
end

local function flammable_cell(world)
  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do
      local material = world:get_material(x, y)
      if material.flammable and not world:get_cell(x, y).destroyed then return { x = x, y = y } end
    end
  end
  error("No flammable campaign fixture cell")
end

local function save_and_load(value, directory)
  directory = directory or SaveStore.memory_directory()
  assert(CampaignPersistence.save(value, directory))
  return assert(CampaignPersistence.load(directory)), directory
end

local function object_ids(data)
  local result = {}
  for _, object in ipairs(data.world.objects or {}) do result[#result + 1] = object.id end
  for _, hazard in ipairs(data.world.hazards or {}) do result[#result + 1] = hazard.id end
  for _, fire in ipairs(data.world.fires or {}) do result[#result + 1] = fire.id end
  table.sort(result)
  return result
end

return {
  {
    name = "ZoneKey has immutable canonical signed-coordinate encoding",
    run = function()
      local key = ZoneKey.new(-2, 5, -1)
      assert(ZoneKey.encode(key) == "zone:-2:5:-1")
      assert(ZoneKey.filename(key) == "zone_-2_5_-1.json")
      assert(ZoneKey.equal(key, ZoneKey.decode("zone:-2:5:-1")))
      assert(encoded(ZoneKey.to_data(key)) == '{"world_x":-2,"world_y":5,"z":-1}')
      assert(not pcall(ZoneKey.decode, "zone:0:bad:0"))
      assert(not pcall(function() key.z = 9 end))
    end,
  },
  {
    name = "campaign IDs use account sequence without changing existing meta history",
    run = function()
      local profile = MetaProfile.new()
      profile.research_data = 7
      profile.discovered_discovery_ids = { "discovery.legacy.reactor" }
      assert(MetaProfile.allocate_campaign(profile) == "campaign:000001")
      assert(MetaProfile.allocate_campaign(profile) == "campaign:000002")
      assert(profile.research_data == 7 and profile.discovered_discovery_ids[1] == "discovery.legacy.reactor")
      local copied = MetaProfile.copy(profile)
      assert(copied.next_campaign_sequence == 3)
    end,
  },
  {
    name = "campaign codecs reject malformed and unsupported envelopes",
    run = function()
      local _, bad_json = CampaignPersistence.decode_manifest("{")
      assert(bad_json.code == "invalid_json")
      local _, bad_format = CampaignPersistence.decode_manifest('{"format":"other","version":1,"campaign":{}}')
      assert(bad_format.code == "unsupported_format")
      local _, bad_version = CampaignPersistence.decode_zone('{"format":"roag.campaign_zone","version":2,"zone":{}}')
      assert(bad_version.code == "unsupported_version")
    end,
  },
  {
    name = "campaign zone generation is isolated by ZoneKey and visit order",
    run = function()
      local a, b = ZoneKey.new(-1, 2, 0), ZoneKey.new(3, -4, -1)
      local _, first_a = Campaign.generate_zone(611003, "campaign:000003", a)
      local _, first_b = Campaign.generate_zone(611003, "campaign:000003", b)
      local _, second_b = Campaign.generate_zone(611003, "campaign:000003", b)
      local _, second_a = Campaign.generate_zone(611003, "campaign:000003", a)
      assert(encoded(first_a.world) == encoded(second_a.world))
      assert(encoded(first_b.world) == encoded(second_b.world))
      assert(Campaign.derive_zone_seed(611003, a) == Campaign.derive_zone_seed(611003, a))
      assert(Campaign.derive_zone_seed(611003, a) ~= Campaign.derive_zone_seed(611003, b))
      local _, cave = Campaign.generate_zone(611003, "campaign:000003", a, "zone_profile.legacy.cave")
      assert(cave.world.terrain == "cave")
      local seen = {}
      for _, id in ipairs(object_ids(first_a)) do seen[id] = true end
      for _, id in ipairs(object_ids(first_b)) do assert(not seen[id], "zone-generated identity collided: " .. id) end
    end,
  },
  {
    name = "campaign physical identities are durable and transferred components keep origin IDs",
    run = function()
      local value = campaign(611004, "campaign:000004")
      local session = value.session
      local player_component = session.state.player.body:list_components()[1]
      local enemy = session.state.enemies[1]
      enemy.x, enemy.y = session.state.player.x + 1, session.state.player.y
      local component = enemy.body:list_components()[1]
      assert(player_component.id:match("^cmp:campaign:000004:campaign:"))
      assert(enemy.actor_id:match("^actor:campaign:000004:zone:0:0:0:"))
      assert(component.id:match("^cmp:campaign:000004:zone:0:0:0:"))
      session:_destroy_enemy(1)
      local corpse = session.state.corpses[#session.state.corpses]
      local slot = corpse:list_components()[1]
      local original = slot.component.id
      assert(session:salvage_corpse_component(corpse.id, slot.slot_id).applied)
      assert(session.state.inventory:get(original).item.object.id == original)
      value:sync_active_references()
      assert(value:validate())
    end,
  },
  {
    name = "campaign save restores stable actor source references after enemy array reorder",
    run = function()
      local value = campaign(611005, "campaign:000005")
      local session = value.session
      local source = session.state.enemies[1]
      session.state.bullets[#session.state.bullets + 1] = {
        kind = "bullet", x = source.x, y = source.y, direction = "w", active = false,
        travel = 1, max = 2, light = 0, source_actor = source,
      }
      table.insert(session.state.enemies, 1, table.remove(session.state.enemies, #session.state.enemies))
      local restored = save_and_load(value)
      assert(restored.session.state.bullets[1].source_actor.actor_id == source.actor_id)
      assert(restored.session.state.bullets[1].source_actor ~= restored.session.state.enemies[1]
        or restored.session.state.enemies[1].actor_id == source.actor_id)
    end,
  },
  {
    name = "campaign manifest and full zone shard round trip preserve mutated physical simulation",
    run = function()
      local value = campaign(611006, "campaign:000006")
      local session, world, player = value.session, value.session.state.world, value.session.state.player
      player.body:list_components()[1].current_integrity = 1
      local corpse_enemy = session:_make_enemy("bomber", { x = player.x + 1, y = player.y })
      session.state.enemies[#session.state.enemies + 1] = corpse_enemy
      session:_destroy_enemy(#session.state.enemies)
      local corpse = session.state.corpses[#session.state.corpses]
      assert(session:salvage_corpse_component(corpse.id, corpse:list_components()[1].slot_id).applied)
      session:turn("shoot_a")
      session:turn("b")
      player.flares = math.max(1, player.flares)
      session:turn("f")
      local water = open_cell(world, player)
      assert(world:set_liquid(water.x, water.y, "liquid.water.legacy", 3).applied)
      assert(world:set_gas(water.x + 1, water.y, "gas.toxic.legacy", 3).applied)
      local fuel = flammable_cell(world)
      assert(session:ignite_terrain(fuel.x, fuel.y, { source = "campaign_test" }).applied)
      local before = encoded(session:to_data())
      local restored, directory = save_and_load(value)
      assert(encoded(restored.session:to_data()) == before)
      assert(directory:file("manifest.json"):read():find('"format":"roag.campaign"', 1, true))
      assert(directory:file("zones/zone_0_0_0.000001.json"):read():find('"format":"roag.campaign_zone"', 1, true))
      assert(restored.session.state.world:liquid_amount(water.x, water.y) == 3)
      assert(restored.session.state.world:gas_concentration(water.x + 1, water.y) == 3)
      assert(#restored.session.state.world:list_fires() >= 1)
    end,
  },
  {
    name = "campaign save atomicity retains manifest-selected zone when writes fail",
    run = function()
      local value = campaign(611007, "campaign:000007")
      local directory = SaveStore.memory_directory()
      assert(CampaignPersistence.save(value, directory))
      local committed = encoded(assert(CampaignPersistence.load(directory)).session:to_data())
      value.session.state.scrap = value.session.state.scrap + 9
      local original_file = directory.file
      directory.file = function(self, name)
        local handle = original_file(self, name)
        if name:match("^zones/") then
          local write = handle.write
          handle.write = function() return nil, { code = "write_failed", reason = "injected zone failure" } end
          handle._write = write
        end
        return handle
      end
      local _, zone_error = CampaignPersistence.save(value, directory)
      assert(zone_error.code == "zone_write_failed")
      assert(encoded(assert(CampaignPersistence.load(directory)).session:to_data()) == committed)
      directory.file = original_file
      directory.file = function(self, name)
        local handle = original_file(self, name)
        if name == "manifest.json" then handle.write = function() return nil, { code = "write_failed", reason = "injected manifest failure" } end end
        return handle
      end
      local _, manifest_error = CampaignPersistence.save(value, directory)
      assert(manifest_error.code == "manifest_write_failed")
      assert(encoded(assert(CampaignPersistence.load(directory)).session:to_data()) == committed)
    end,
  },
  {
    name = "campaign continuation matches uninterrupted local simulation",
    run = function()
      local control = campaign(611008, "campaign:000008")
      local saved = campaign(611008, "campaign:000008")
      for _, action in ipairs({ "w", "b", "shoot_w", "d" }) do control.session:turn(action); saved.session:turn(action) end
      local restored = save_and_load(saved)
      for _, action in ipairs({ "w", "f", "shoot_d", "a", "b" }) do control.session:turn(action); restored.session:turn(action) end
      assert(encoded(control.session:to_data()) == encoded(restored.session:to_data()))
    end,
  },
  {
    name = "campaign runtime path coexists with and never migrates legacy active-run storage",
    run = function()
      local active, meta, archive = SaveStore.memory(), SaveStore.memory(), SaveStore.memory()
      local directory = SaveStore.memory_directory()
      local legacy = Session.new({ seed = 611009 })
      legacy:start_run()
      assert(ActiveRun.save(legacy, active))
      local original_active = assert(active:read())
      local app = App.new({ seed = 611009, save_store = active, meta_store = meta, archive_store = archive, campaign_store = directory })
      assert(app.legacy_active_run_present and not app.campaign_continue_available)
      assert(app:request_new_campaign())
      assert(active:read() == original_active)
      local resumed = App.new({ seed = 611010, save_store = active, meta_store = meta, archive_store = archive, campaign_store = directory })
      assert(resumed.campaign_continue_available and resumed:continue_campaign())
      assert(active:read() == original_active)
    end,
  },
  {
    name = "title starts Expeditions by default while Sandbox campaigns and legacy runs remain accessible",
    run = function()
      local campaign_store = SaveStore.memory_directory()
      local app = App.new({ seed = 611010, save_store = SaveStore.memory(), meta_store = SaveStore.memory(),
        archive_store = SaveStore.memory(), campaign_store = campaign_store })
      assert(app:title_options()[1].id == "expedition")
      assert(app:activate_title_choice() and app.screen == "expedition_character_select")
      -- Sandbox remains an explicit separate title path rather than being
      -- deleted or folded into the disposable Expedition run.
      app.screen, app.menu = "title", 2
      assert(app:activate_title_choice() and app.screen == "campaign_slots")
      assert(app:select_campaign_slot() and app.campaign)
      local resumed = App.new({ seed = 611011, save_store = SaveStore.memory(), meta_store = SaveStore.memory(),
        archive_store = SaveStore.memory(), campaign_store = campaign_store })
      assert(resumed:title_options()[3].name == "CONTINUE CAMPAIGN")

      local legacy_store = SaveStore.memory()
      local legacy = Session.new({ seed = 611012 }); legacy:start_run(); assert(ActiveRun.save(legacy, legacy_store))
      local legacy_app = App.new({ seed = 611012, save_store = legacy_store, meta_store = SaveStore.memory(),
        archive_store = SaveStore.memory(), campaign_store = SaveStore.memory_directory() })
      local options = legacy_app:title_options()
      assert(options[1].name == "EXPEDITION" and options[3].name == "LEGACY RUN")
    end,
  },
}
