local Campaign = require("src.campaign.campaign")
local CampaignPersistence = require("src.persistence.campaign")
local SaveStore = require("src.persistence.save_store")
local ZoneKey = require("src.campaign.zone_key")
local InspectionFloor = require("src.generation.inspection_floor")

local function new_campaign(seed, id)
  local campaign = Campaign.new({ seed = seed or 904001, campaign_id = id or "campaign:904001" })
  local directory = SaveStore.memory_directory()
  campaign:set_persistence_directory(directory)
  assert(CampaignPersistence.save(campaign, directory))
  return campaign, directory
end

local function move_to_edge(campaign, direction)
  local connection = assert(campaign.active_zone.connections[direction])
  campaign.session.state.player.x, campaign.session.state.player.y = connection.boundary.x, connection.boundary.y
  local input = ({ north = "w", east = "d", south = "s", west = "a" })[direction]
  assert(campaign.session:turn(input) == "zone_transition")
end

local function kill(campaign, cause)
  local result, failure = campaign:handle_player_death({ cause = cause or "test" })
  assert(result, failure and failure.reason)
  return result
end

local function descend(campaign)
  local connection = assert(campaign.active_zone.connections.down, "expected deterministic downward connection")
  local object = assert(campaign.session.state.world:object_at(connection.cell.x, connection.cell.y))
  campaign.session.state.player.x, campaign.session.state.player.y, campaign.session.state.player.direction = object.x + 1, object.y, "a"
  assert(campaign.session:turn("interact") == "zone_transition")
end

return {
  {
    name = "campaign death leaves an exact player corpse and creates one fresh successor without retiring the world",
    run = function()
      local campaign, directory = new_campaign(904001, "campaign:904001")
      local player = campaign.session.state.player
      local old_actor, old_component, wallet = player.actor_id, player.body:list_components()[1].id, campaign.session.state.scrap
      local result = kill(campaign, "kinetic")
      assert(result.code == "campaign_succession")
      assert(campaign.state.body_death_count == 1 and campaign.session.state.scrap == wallet)
      local corpse = assert(campaign.session.state.corpses[1])
      assert(corpse.source_kind == "player" and corpse.source_body_id == old_actor and corpse.death_cause == "kinetic")
      assert(corpse.id:match("^corpse:campaign:904001:zone:0:0:0:"))
      assert(corpse.body:list_components()[1].id == old_component)
      assert(campaign.session.state.player.actor_id ~= old_actor)
      assert(campaign.session.state.player.body:list_components()[1].id ~= old_component)
      assert(CampaignPersistence.load(directory):validate())
    end,
  },
  {
    name = "fatal simulation damage takes the campaign succession branch while legacy death behavior remains separate",
    run = function()
      local campaign = new_campaign(904011, "campaign:904011")
      local old = campaign.session.state.player
      old.health = 1
      local result = campaign.session:_apply_world_actor_damage(old, 1, nil, { cause = "hazard", source = "test" })
      assert(result.dead and campaign.session ~= nil)
      assert(campaign.session.state.player and campaign.session.state.player.actor_id ~= old.actor_id)
      assert(campaign.state.body_death_count == 1 and campaign:validate())
    end,
  },
  {
    name = "campaign death transfers carried components to the corpse and partial recovery persists",
    run = function()
      local campaign, directory = new_campaign(904002, "campaign:904002")
      local session, player = campaign.session, campaign.session.state.player
      local enemy = session.state.enemies[1]
      enemy.x, enemy.y = player.x + 1, player.y
      local slot = enemy.body:list_installed_slots()[1]
      local carried_id = slot.component.id
      session:_destroy_enemy(1)
      local enemy_corpse = session.state.corpses[#session.state.corpses]
      assert(session:salvage_corpse_component(enemy_corpse.id, slot.slot_id).applied)
      assert(session.state.inventory:get(carried_id))
      kill(campaign, "fire")
      local corpse = assert(campaign.session.state.corpses[#campaign.session.state.corpses])
      assert(corpse.carried_inventory:get(carried_id))
      local successor = campaign.session.state.player
      successor.x, successor.y = corpse.x + 1, corpse.y
      assert(campaign.session:salvage_corpse_carried_component(corpse.id, carried_id).applied)
      assert(campaign.session.state.inventory:get(carried_id) and not corpse.carried_inventory:get(carried_id))
      assert(CampaignPersistence.save(campaign, directory))
      campaign = assert(CampaignPersistence.load(directory))
      assert(not campaign.session.state.corpses[#campaign.session.state.corpses].carried_inventory:get(carried_id))
    end,
  },
  {
    name = "remote campaign death freezes a corpse zone and successor returns to the persistent anchor",
    run = function()
      local campaign, directory = new_campaign(904003, "campaign:904003")
      local anchor = campaign.state.reconstruction_anchor
      assert(ZoneKey.equal(anchor.zone_key, ZoneKey.new(0, 0, 0)))
      move_to_edge(campaign, "east")
      local death_zone = ZoneKey.from_data(campaign.state.current_zone)
      local result = kill(campaign, "gas")
      assert(ZoneKey.equal(campaign.state.current_zone, anchor.zone_key))
      assert(not ZoneKey.equal(death_zone, campaign.state.current_zone))
      move_to_edge(campaign, "east")
      assert(ZoneKey.equal(campaign.state.current_zone, death_zone))
      local corpse = campaign.session.state.corpses[#campaign.session.state.corpses]
      assert(corpse and corpse.source_kind == "player" and corpse.death_cause == "gas")
      assert(result.corpse_id == corpse.id)
      assert(CampaignPersistence.save(campaign, directory))
    end,
  },
  {
    name = "deep-cave death persists at its exact Z zone while the successor returns to the surface anchor",
    run = function()
      local campaign, directory = new_campaign(904013, "campaign:904013")
      descend(campaign); descend(campaign)
      assert(ZoneKey.equal(campaign.state.current_zone, ZoneKey.new(0, 0, -2)))
      local death_position = { x = campaign.session.state.player.x, y = campaign.session.state.player.y }
      kill(campaign, "deep_cave")
      assert(ZoneKey.equal(campaign.state.current_zone, ZoneKey.new(0, 0, 0)))
      descend(campaign); descend(campaign)
      local corpse = assert(campaign.session.state.corpses[#campaign.session.state.corpses])
      assert(corpse.x == death_position.x and corpse.y == death_position.y and corpse.death_zone_key.z == -2)
      assert(CampaignPersistence.save(campaign, directory))
    end,
  },
  {
    name = "multiple campaign deaths retain distinct bodies while successor consumables and charms reset",
    run = function()
      local campaign = new_campaign(904004, "campaign:904004")
      campaign.session.state.charms.slots[1] = "charm.legacy.iron_heart"
      campaign.session.state.player.ammo, campaign.session.state.player.bombs, campaign.session.state.player.flares = 0, 0, 0
      local first = kill(campaign, "explosive")
      assert(#campaign.session.state.corpses == 1 and #campaign.session.state.charms.slots == 0)
      local successor = campaign.session.state.player
      assert(successor.ammo == 0 and campaign.session:ammo_reserve("resource.ammo.bullets") == campaign.session.state.settings.ammo
        and successor.bombs == campaign.session.state.settings.bombs)
      local second = kill(campaign, "kinetic")
      assert(#campaign.session.state.corpses == 2 and first.corpse_id ~= second.corpse_id)
      assert(campaign:validate())
    end,
  },
  {
    name = "reconstruction anchor is a protected persistent station and enables the existing reconstruction transaction boundary",
    run = function()
      local campaign = new_campaign(904005, "campaign:904005")
      local anchor = campaign.state.reconstruction_anchor
      local station = assert(campaign.session.state.world:get_object(anchor.station_object_id))
      assert(station.interaction_role == "reconstruction_station")
      assert(campaign.session.state.world:damage_object(station, { amount = station.current_integrity, cause = "test" }).code == "protected_station")
      local player = campaign.session.state.player
      player.x, player.y, player.direction = station.x + 1, station.y, "a"
      assert(campaign.session:turn("interact") == "reconstruction")
      assert(campaign.session:_reconstruction_allowed())
      assert(campaign.session:complete_reconstruction().applied)
    end,
  },
  {
    name = "campaign-zone inspection exposes the reconstruction anchor and physical station",
    run = function()
      local floor = assert(InspectionFloor.generate_campaign_zone({ campaign_seed = 904015, world_x = 0, world_y = 0, z = 0 }))
      local anchor = assert(floor.reconstruction_anchor)
      local station = assert(floor.world:get_object(anchor.station_object_id))
      assert(station.interaction_role == "reconstruction_station" and #floor.player_corpses == 0)
    end,
  },
  {
    name = "pending succession reconciliation is idempotent after final manifest failure",
    run = function()
      local campaign, directory = new_campaign(904006, "campaign:904006")
      local original_file, manifest_writes = directory.file, 0
      directory.file = function(self, name)
        local handle = original_file(self, name)
        if name == "manifest.json" then
          local write = handle.write
          handle.write = function(_, payload)
            manifest_writes = manifest_writes + 1
            if manifest_writes == 2 then return nil, { code = "write_failed", reason = "injected final manifest failure" } end
            return write(handle, payload)
          end
        end
        return handle
      end
      local result, failure = campaign:handle_player_death({ cause = "test" })
      assert(not result and failure.code == "persistence_failed")
      directory.file = original_file
      local restored = assert(CampaignPersistence.load(directory))
      assert(restored.state.pending_successor == nil and restored.state.body_death_count == 1)
      assert(#restored.session.state.corpses == 1 and restored:validate())
      assert(CampaignPersistence.save(restored, directory))
    end,
  },
  {
    name = "pending succession reconciliation also repairs a failed death-zone shard write",
    run = function()
      local campaign, directory = new_campaign(904007, "campaign:904007")
      local original_file = directory.file
      directory.file = function(self, name)
        local handle = original_file(self, name)
        if name:match("^zones/") then
          handle.write = function() return nil, { code = "write_failed", reason = "injected corpse-zone failure" } end
        end
        return handle
      end
      local result, failure = campaign:handle_player_death({ cause = "test" })
      assert(not result and failure.code == "persistence_failed")
      directory.file = original_file
      local restored = assert(CampaignPersistence.load(directory))
      assert(restored.state.body_death_count == 1 and #restored.session.state.corpses == 1 and restored:validate())
    end,
  },
}
