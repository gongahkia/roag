local Campaign = require("src.campaign.campaign")
local WorldTopology = require("src.campaign.world_topology")
local ZoneKey = require("src.campaign.zone_key")
local Grid = require("src.world.grid")
local SaveStore = require("src.persistence.save_store")
local CampaignPersistence = require("src.persistence.campaign")
local Json = require("src.persistence.json")
local InspectionFloor = require("src.generation.inspection_floor")
local Inspector = require("src.tools.generation_inspector")

local function encoded(value)
  return assert(Json.encode(value))
end

local function new_campaign(seed, id)
  local campaign = Campaign.new({ seed = seed or 890001, campaign_id = id or "campaign:890001" })
  local directory = SaveStore.memory_directory()
  campaign:set_persistence_directory(directory)
  assert(CampaignPersistence.save(campaign, directory))
  return campaign, directory
end

local function vertical_connection(campaign, direction)
  return assert(WorldTopology.connection_at(campaign.active_zone, direction))
end

local function vertical_object(campaign, direction)
  local connection = vertical_connection(campaign, direction)
  local object = campaign.session.state.world:object_at(connection.cell.x, connection.cell.y)
  assert(object and object.zone_connection_id == connection.id)
  return object, connection
end

local function use(campaign, direction)
  local object = vertical_object(campaign, direction)
  local player = campaign.session.state.player
  player.x, player.y = object.x + 1, object.y
  return campaign.session:turn("interact")
end

local function zone_fingerprint(campaign)
  local simulation = campaign:to_zone_data().simulation
  return encoded({ rng = simulation.rng, world = simulation.world, enemies = simulation.enemies, targets = simulation.targets,
    corpses = simulation.corpses, bullets = simulation.bullets, bombs = simulation.bombs, flares = simulation.flares,
    torches = simulation.torches, area_attacks = simulation.area_attacks })
end

local function reachable(world, start)
  if not world:is_passable(start.x, start.y) then return false end
  local seen, queue, index = { [Grid.key(start.x, start.y)] = true }, { start }, 1
  while queue[index] do
    local current = queue[index]
    index = index + 1
    for _, next_cell in ipairs(Grid.neighbours(current)) do
      local key = Grid.key(next_cell.x, next_cell.y)
      if Grid.in_bounds(next_cell.x, next_cell.y) and world:is_passable(next_cell.x, next_cell.y) and not seen[key] then
        seen[key] = true
        queue[#queue + 1] = next_cell
      end
    end
  end
  return #queue > 2
end

return {
  {
    name = "vertical campaign bounds, cave columns, profiles, and generic types are deterministic",
    run = function()
      assert(WorldTopology.is_zone_in_bounds(ZoneKey.new(-4, 4, -2)))
      assert(WorldTopology.is_zone_in_bounds(ZoneKey.new(4, -4, 1)))
      assert(not WorldTopology.is_zone_in_bounds(ZoneKey.new(0, 0, -3)))
      assert(not WorldTopology.is_zone_in_bounds(ZoneKey.new(0, 0, 2)))
      local surface, cave, deep = ZoneKey.new(0, 0, 0), ZoneKey.new(0, 0, -1), ZoneKey.new(0, 0, -2)
      assert(WorldTopology.profile_for(surface) == "zone_profile.legacy.forest")
      assert(WorldTopology.profile_for(cave) == "zone_profile.legacy.cave")
      assert(WorldTopology.profile_for(deep) == "zone_profile.legacy.deep_cave")
      local down = assert(WorldTopology.connection(890002, surface, "down"))
      local up = assert(WorldTopology.connection(890002, cave, "up"))
      assert(down.id == up.id and down.connection_type == "cave_mouth")
      local deep_down = assert(WorldTopology.connection(890002, cave, "down"))
      assert(deep_down.id == assert(WorldTopology.connection(890002, deep, "up")).id)
      for _, kind in ipairs({ "stairs", "ladder", "elevator", "shaft" }) do
        local generic = assert(WorldTopology.generic_vertical_connection(890002, ZoneKey.new(1, 1, 0), "down", kind))
        assert(generic.connection_type == kind and generic.id:match("^vconn:"))
        assert(WorldTopology.presentation_label(generic) ~= "TRAVEL")
      end
      for seed = 890100, 890199 do
        local up = assert(WorldTopology.connection(seed, cave, "up"))
        local down_link = assert(WorldTopology.connection(seed, cave, "down"))
        assert(math.max(math.abs(up.cell.x - down_link.cell.x), math.abs(up.cell.y - down_link.cell.y)) > 2,
          "vertical landmarks must not share an interaction footprint")
      end
    end,
  },
  {
    name = "vertical landmarks are physical passable protected objects with reachable reserved throats",
    run = function()
      local campaign = Campaign.new({ seed = 890003, campaign_id = "campaign:890003" })
      for _, direction in ipairs({ "down" }) do
        local object, connection = vertical_object(campaign, direction)
        assert(object.interaction_role == "zone_connection")
        assert(object.zone_connection_type == connection.connection_type)
        assert(campaign.session.state.world:is_passable(object.x, object.y))
        assert(reachable(campaign.session.state.world, connection.cell))
        assert(campaign.session.state.surface_connector_cells[Grid.key(connection.cell.x, connection.cell.y)])
        assert(campaign.session.state.world:damage_object(object, { amount = object.current_integrity, cause = "test" }).code == "protected_connection")
      end
    end,
  },
  {
    name = "cave-mouth U interaction uses the shared no-extra-tick campaign transition",
    run = function()
      local campaign = new_campaign(890004, "campaign:890004")
      local source, rng = campaign.session, campaign.session.rng:to_data()
      local object = vertical_object(campaign, "down")
      local interactions = campaign.session:available_interactions({ x = object.x + 1, y = object.y })
      assert(interactions[1].actions[1].label == "DESCEND INTO CAVE")
      assert(use(campaign, "down") == "zone_transition")
      assert(ZoneKey.equal(campaign.state.current_zone, ZoneKey.new(0, 0, -1)))
      assert(source.rng.state == rng.state)
      assert(campaign.session.state.phase == "combat")
      assert(campaign.session.state.player.objective_progress == 0)
    end,
  },
  {
    name = "campaign vertical travel is objective-independent and cannot enter legacy route reconstruction flow",
    run = function()
      local campaign = new_campaign(890041, "campaign:890041")
      campaign.session.state.player.objective_progress = campaign.session.state.settings.objective_required
      local scrap = campaign.session.state.scrap
      assert(use(campaign, "down") == "zone_transition")
      assert(campaign.session.state.phase == "combat")
      assert(campaign.session.state.scrap == scrap)
      assert(campaign.session.state.current_zone == nil or campaign.session.state.phase ~= "route")
    end,
  },
  {
    name = "surface cave deep-cave vertical round trip freezes every departed zone",
    run = function()
      local campaign = new_campaign(890005, "campaign:890005")
      local surface_before = zone_fingerprint(campaign)
      local player_component = campaign.session.state.player.body:list_components()[1].id
      assert(use(campaign, "down") == "zone_transition")
      local cave_before = zone_fingerprint(campaign)
      assert(use(campaign, "down") == "zone_transition")
      assert(ZoneKey.equal(campaign.state.current_zone, ZoneKey.new(0, 0, -2)))
      assert(use(campaign, "up") == "zone_transition")
      assert(zone_fingerprint(campaign) == cave_before)
      assert(use(campaign, "up") == "zone_transition")
      assert(zone_fingerprint(campaign) == surface_before)
      assert(campaign.session.state.player.body:list_components()[1].id == player_component)
      assert(campaign:validate())
    end,
  },
  {
    name = "vertical history persists through save reload and preserves cross-z physical identities",
    run = function()
      local campaign, directory = new_campaign(890006, "campaign:890006")
      local physical_id = campaign.session.state.player.body:list_components()[1].id
      assert(use(campaign, "down") == "zone_transition")
      local cave_state = zone_fingerprint(campaign)
      assert(use(campaign, "down") == "zone_transition")
      assert(CampaignPersistence.save(campaign, directory))
      campaign = assert(CampaignPersistence.load(directory))
      campaign:set_persistence_directory(directory)
      assert(use(campaign, "up") == "zone_transition")
      assert(zone_fingerprint(campaign) == cave_state)
      assert(campaign.session.state.player.body:list_components()[1].id == physical_id)
      assert(use(campaign, "up") == "zone_transition")
      assert(#campaign:zone_records() == 3 and campaign:validate())
    end,
  },
  {
    name = "OW-02 campaign v1 shard metadata gains additive vertical landmarks without losing its saved zone",
    run = function()
      local campaign, directory = new_campaign(890061, "campaign:890061")
      local manifest_text = assert(directory:file("manifest.json"):read())
      local manifest = assert(CampaignPersistence.decode_manifest(manifest_text))
      manifest.zones[1].connections.down = nil
      local shard_name = CampaignPersistence.zone_filename(ZoneKey.new(0, 0, 0), manifest.zones[1].shard_revision)
      local shard_text = assert(directory:file(shard_name):read())
      local shard = assert(CampaignPersistence.decode_zone(shard_text))
      for index = #shard.simulation.world.objects, 1, -1 do
        if shard.simulation.world.objects[index].zone_connection_id then
          table.remove(shard.simulation.world.objects, index)
        end
      end
      assert(directory:file("manifest.json"):write(assert(Json.encode({ format = "roag.campaign", version = 1, campaign = manifest }))))
      assert(directory:file(shard_name):write(assert(Json.encode({ format = "roag.campaign_zone", version = 1, zone = shard }))))
      campaign = assert(CampaignPersistence.load(directory))
      campaign:set_persistence_directory(directory)
      local object = vertical_object(campaign, "down")
      assert(object.zone_connection_type == "cave_mouth")
      assert(CampaignPersistence.save(campaign, directory))
      assert(CampaignPersistence.load(directory):validate())
    end,
  },
  {
    name = "vertical arrival fallback is bounded and a fully blocked reciprocal throat fails without a teleport",
    run = function()
      local campaign = new_campaign(890062, "campaign:890062")
      local source = vertical_connection(campaign, "down")
      local record = campaign:_new_record(source.destination)
      local destination = assert(campaign:_generate_zone_session(record))
      local reciprocal = assert(WorldTopology.connection_at(record, "up"))
      destination.state.enemies[#destination.state.enemies + 1] = { x = reciprocal.interior.x, y = reciprocal.interior.y }
      local fallback = assert(campaign:_resolve_arrival(destination, reciprocal))
      assert(math.abs(fallback.x - reciprocal.interior.x) + math.abs(fallback.y - reciprocal.interior.y) <= 4)
      for y = reciprocal.interior.y - 4, reciprocal.interior.y + 4 do
        for x = reciprocal.interior.x - 4, reciprocal.interior.x + 4 do
          if Grid.in_bounds(x, y) and math.abs(x - reciprocal.interior.x) + math.abs(y - reciprocal.interior.y) <= 4 then
            destination.state.enemies[#destination.state.enemies + 1] = { x = x, y = y }
          end
        end
      end
      local _, failure = campaign:_resolve_arrival(destination, reciprocal)
      assert(failure.code == "arrival_blocked")
    end,
  },
  {
    name = "vertical transition write failures retain the source zone and canonical active body",
    run = function()
      local function fixture(number)
        local campaign, directory = new_campaign(890070 + number, "campaign:" .. tostring(890070 + number))
        local object = vertical_object(campaign, "down")
        campaign.session.state.player.x, campaign.session.state.player.y = object.x + 1, object.y
        return campaign, directory
      end
      for _, failure_kind in ipairs({ "source", "destination", "manifest" }) do
        local campaign, directory = fixture(#failure_kind)
        local before = ZoneKey.to_data(campaign.state.current_zone)
        local original_file, zone_writes = directory.file, 0
        directory.file = function(self, name)
          local handle = original_file(self, name)
          if name:match("^zones/") then
            local write = handle.write
            handle.write = function(...)
              zone_writes = zone_writes + 1
              if (failure_kind == "source" and zone_writes == 1) or (failure_kind == "destination" and zone_writes == 2) then
                return nil, { code = "write_failed", reason = "injected " .. failure_kind .. " failure" }
              end
              return write(...)
            end
          elseif name == "manifest.json" and failure_kind == "manifest" then
            handle.write = function() return nil, { code = "write_failed", reason = "injected manifest failure" } end
          end
          return handle
        end
        local transitioned, error_data = campaign:transition("down")
        assert(not transitioned and error_data.code == "persistence_failed")
        assert(ZoneKey.equal(campaign.state.current_zone, ZoneKey.from_data(before)))
        assert(campaign.session.state.player == campaign.state.active_body and campaign:validate())
      end
    end,
  },
  {
    name = "vertical endpoint placement and zone IDs are independent of full-column generation order",
    run = function()
      local keys = { ZoneKey.new(0, 0, 0), ZoneKey.new(0, 0, -1), ZoneKey.new(0, 0, -2) }
      local first, second = {}, {}
      for _, key in ipairs(keys) do first[ZoneKey.encode(key)] = { Campaign.generate_zone(890007, "campaign:890007", key) } end
      for _, key in ipairs({ keys[3], keys[1], keys[2] }) do second[ZoneKey.encode(key)] = { Campaign.generate_zone(890007, "campaign:890007", key) } end
      for _, key in ipairs(keys) do
        local encoded_key = ZoneKey.encode(key)
        assert(encoded(first[encoded_key][1]) == encoded(second[encoded_key][1]))
        assert(encoded(first[encoded_key][2].world) == encoded(second[encoded_key][2].world))
      end
    end,
  },
  {
    name = "campaign-zone inspector exposes vertical profile and reciprocal connection overlay metadata",
    run = function()
      local floor = assert(InspectionFloor.generate_campaign_zone({ campaign_seed = 890008, world_x = 0, world_y = 0, z = -1 }))
      assert(floor.profile_id == "zone_profile.legacy.cave")
      assert(floor.connections.up and floor.connections.down)
      local inspector = Inspector.new({ campaign_seed = 890008, world_x = 0, world_y = 0, z = -1 })
      assert(inspector.overlays.zone_connections.up.id == floor.connections.up.id)
      assert(inspector.overlays.zone_connections.down.destination.z == -2)
    end,
  },
}
