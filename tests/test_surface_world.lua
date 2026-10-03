local Campaign = require("src.campaign.campaign")
local ZoneKey = require("src.campaign.zone_key")
local SurfaceWorld = require("src.campaign.surface_world")
local CampaignPersistence = require("src.persistence.campaign")
local SaveStore = require("src.persistence.save_store")
local Json = require("src.persistence.json")
local InspectionFloor = require("src.generation.inspection_floor")
local Inspector = require("src.tools.generation_inspector")

local function encoded(value)
  return assert(Json.encode(value))
end

local function new_campaign(seed, id)
  local campaign = Campaign.new({ seed = seed or 880001, campaign_id = id or "campaign:880001" })
  local directory = SaveStore.memory_directory()
  campaign:set_persistence_directory(directory)
  assert(CampaignPersistence.save(campaign, directory))
  return campaign, directory
end

local function connection(campaign, direction)
  return assert(SurfaceWorld.connection_at(campaign.active_zone, direction))
end

local function move_to_boundary(campaign, direction)
  local value = connection(campaign, direction)
  campaign.session.state.player.x, campaign.session.state.player.y = value.boundary.x, value.boundary.y
  return value
end

local function frozen_snapshot(campaign)
  local simulation = campaign:to_zone_data().simulation
  return encoded({
    rng = simulation.rng, world = simulation.world, enemies = simulation.enemies, targets = simulation.targets,
    corpses = simulation.corpses, bullets = simulation.bullets, bombs = simulation.bombs,
    flares = simulation.flares, torches = simulation.torches, area_attacks = simulation.area_attacks,
  })
end

return {
  {
    name = "finite surface world bounds use north y-minus east x-plus and reciprocal canonical edges",
    run = function()
      assert(SurfaceWorld.is_zone_in_bounds(ZoneKey.new(-4, -4, 0)))
      assert(SurfaceWorld.is_zone_in_bounds(ZoneKey.new(4, 4, 0)))
      assert(not SurfaceWorld.is_zone_in_bounds(ZoneKey.new(5, 0, 0)))
      assert(not SurfaceWorld.is_zone_in_bounds(ZoneKey.new(0, 0, -1)))
      local origin = ZoneKey.new(0, 0, 0)
      assert(ZoneKey.equal(SurfaceWorld.neighbor(origin, "north"), ZoneKey.new(0, -1, 0)))
      assert(ZoneKey.equal(SurfaceWorld.neighbor(origin, "east"), ZoneKey.new(1, 0, 0)))
      local east = assert(SurfaceWorld.connection(880002, origin, "east"))
      local west = assert(SurfaceWorld.connection(880002, east.destination, "west"))
      assert(east.id == west.id and east.boundary.y == west.boundary.y)
      assert(not SurfaceWorld.connection(880002, ZoneKey.new(4, 0, 0), "east"))
    end,
  },
  {
    name = "surface zone profiles connections and generated IDs are visit-order independent",
    run = function()
      local a, b, c = ZoneKey.new(-1, 2, 0), ZoneKey.new(2, -3, 0), ZoneKey.new(4, 4, 0)
      local first, second = {}, {}
      for _, key in ipairs({ a, b, c }) do first[ZoneKey.encode(key)] = { Campaign.generate_zone(880003, "campaign:880003", key) } end
      for _, key in ipairs({ c, a, b }) do second[ZoneKey.encode(key)] = { Campaign.generate_zone(880003, "campaign:880003", key) } end
      for _, key in ipairs({ a, b, c }) do
        local id = ZoneKey.encode(key)
        assert(encoded(first[id][1]) == encoded(second[id][1]))
        assert(encoded(first[id][2].world) == encoded(second[id][2].world))
      end
    end,
  },
  {
    name = "campaign surface connectors are physically carved and reserved from later normal spawns",
    run = function()
      local campaign = Campaign.new({ seed = 880004, campaign_id = "campaign:880004" })
      for _, data in pairs(campaign.active_zone.connections) do
        local world = campaign.session.state.world
        assert(world:is_passable(data.boundary.x, data.boundary.y))
        assert(world:is_passable(data.interior.x, data.interior.y))
        assert(SurfaceWorld.reachable_from_connection(world, data))
        assert(SurfaceWorld.connection_reaches_primary(campaign.session, data))
        assert(campaign.session.state.surface_connector_cells[data.boundary.x .. ":" .. data.boundary.y])
      end
    end,
  },
  {
    name = "ordinary cardinal movement commits a no-tick player-only surface transition",
    run = function()
      local campaign = new_campaign(880005, "campaign:880005")
      local east = move_to_boundary(campaign, "east")
      local source_session, source_rng = campaign.session, campaign.session.rng:to_data()
      local component_id = source_session.state.player.body:list_components()[1].id
      assert(source_session:turn("d") == "zone_transition")
      assert(ZoneKey.equal(campaign.state.current_zone, east.destination))
      assert(campaign.session.state.player.body:list_components()[1].id == component_id)
      assert(source_session.rng.state == source_rng.state)
      assert(campaign.session.state.phase == "combat")
    end,
  },
  {
    name = "north east south and west input each use their reciprocal surface connector",
    run = function()
      for _, specification in ipairs({
        { direction = "north", input = "w" }, { direction = "east", input = "d" },
        { direction = "south", input = "s" }, { direction = "west", input = "a" },
      }) do
        local campaign = new_campaign(880050 + #specification.direction, "campaign:" .. tostring(880050 + #specification.direction))
        local source = campaign.active_zone.key
        local edge = move_to_boundary(campaign, specification.direction)
        assert(campaign.session:turn(specification.input) == "zone_transition")
        assert(ZoneKey.equal(campaign.state.current_zone, edge.destination))
        local opposite = connection(campaign, SurfaceWorld.opposite(specification.direction))
        assert(opposite.id == edge.id)
        assert(not ZoneKey.equal(campaign.state.current_zone, source))
      end
    end,
  },
  {
    name = "invalid edges remain blocked and objective completion cannot gate campaign travel or route flow",
    run = function()
      local campaign, directory = new_campaign(880006, "campaign:880006")
      local player = campaign.session.state.player
      player.objective_progress = campaign.session.state.settings.objective_required
      move_to_boundary(campaign, "east")
      assert(campaign.session:turn("d") == "zone_transition")
      assert(campaign.session.state.phase == "combat")
      assert(#campaign:zone_records() == 2)
      -- An outer world edge has no metadata and never wraps.
      local edge = Campaign.new({ seed = 880006, campaign_id = "campaign:880106", current_zone = ZoneKey.new(4, 0, 0) })
      edge:set_persistence_directory(directory)
      local result, failure = edge:transition("east", directory)
      assert(not result and failure.code == "world_boundary")
    end,
  },
  {
    name = "A to B to A freezes full source-zone simulation while body cargo remains campaign-owned",
    run = function()
      local campaign = new_campaign(880007, "campaign:880007")
      move_to_boundary(campaign, "east")
      local source = frozen_snapshot(campaign)
      assert(campaign:transition("east"))
      -- Spend a normal destination action; only B may advance.
      campaign.session:turn("wait")
      move_to_boundary(campaign, "west")
      assert(campaign:transition("west"))
      assert(frozen_snapshot(campaign) == source)
      assert(campaign:validate())
    end,
  },
  {
    name = "occupied preferred arrival uses deterministic nearby fallback and blocked arrival fails in source zone",
    run = function()
      local campaign = new_campaign(880008, "campaign:880008")
      assert(campaign:transition("east", (function() move_to_boundary(campaign, "east"); return campaign.persistence_directory end)()))
      local west = connection(campaign, "west")
      local blocker = campaign.session.state.enemies[1]
      blocker.x, blocker.y = west.interior.x, west.interior.y
      move_to_boundary(campaign, "west")
      assert(campaign:transition("west"))
      move_to_boundary(campaign, "east")
      local entered = assert(campaign:transition("east"))
      assert(entered.arrival.x ~= west.interior.x or entered.arrival.y ~= west.interior.y)

      local destination = campaign.session
      local conn = connection(campaign, "west")
      for radius = 0, 4 do
        for y = conn.interior.y - radius, conn.interior.y + radius do
          for x = conn.interior.x - radius, conn.interior.x + radius do
            if math.abs(x - conn.interior.x) + math.abs(y - conn.interior.y) == radius and x >= 0 and y >= 0 then
              destination.state.targets[#destination.state.targets + 1] = { kind = "target", x = x, y = y }
            end
          end
        end
      end
      local arrival, failure = campaign:_resolve_arrival(destination, conn)
      assert(not arrival and failure.code == "arrival_blocked")
    end,
  },
  {
    name = "transition zone and manifest write failures retain the committed source campaign",
    run = function()
      local campaign, directory = new_campaign(880009, "campaign:880009")
      local original = assert(directory:file("manifest.json"):read())
      local original_file = directory.file
      directory.file = function(self, name)
        local handle = original_file(self, name)
        if name:match("zone_0_0_0") then handle.write = function() return nil, { code = "write_failed", reason = "injected source failure" } end end
        return handle
      end
      move_to_boundary(campaign, "east")
      local transitioned, failure = campaign:transition("east")
      assert(not transitioned and failure.code == "persistence_failed" and failure.detail == "zone_write_failed")
      assert(ZoneKey.equal(campaign.state.current_zone, ZoneKey.new(0, 0, 0)))
      assert(assert(directory:file("manifest.json"):read()) == original)

      directory.file = original_file
      campaign, directory = new_campaign(880109, "campaign:880109")
      original, original_file = assert(directory:file("manifest.json"):read()), directory.file
      directory.file = function(self, name)
        local handle = original_file(self, name)
        if name:match("zone_1_0_0") then handle.write = function() return nil, { code = "write_failed", reason = "injected destination failure" } end end
        return handle
      end
      move_to_boundary(campaign, "east")
      transitioned, failure = campaign:transition("east")
      assert(not transitioned and failure.code == "persistence_failed")
      assert(ZoneKey.equal(campaign.state.current_zone, ZoneKey.new(0, 0, 0)))
      assert(assert(directory:file("manifest.json"):read()) == original)
      directory.file = original_file

      campaign, directory = new_campaign(880209, "campaign:880209")
      original, original_file = assert(directory:file("manifest.json"):read()), directory.file
      directory.file = function(self, name)
        local handle = original_file(self, name)
        if name == "manifest.json" then handle.write = function() return nil, { code = "write_failed", reason = "injected manifest failure" } end end
        return handle
      end
      move_to_boundary(campaign, "east")
      transitioned, failure = campaign:transition("east")
      assert(not transitioned and failure.code == "persistence_failed" and failure.detail == "manifest_write_failed")
      assert(ZoneKey.equal(campaign.state.current_zone, ZoneKey.new(0, 0, 0)))
      assert(assert(directory:file("manifest.json"):read()) == original)
      directory.file = original_file
      assert(CampaignPersistence.load(directory))
    end,
  },
  {
    name = "campaign save reload retains both visited shard heads and restores an earlier zone exactly",
    run = function()
      local campaign, directory = new_campaign(880011, "campaign:880011")
      move_to_boundary(campaign, "east")
      assert(campaign:transition("east"))
      local before = frozen_snapshot(campaign)
      assert(CampaignPersistence.save(campaign, directory))
      campaign = assert(CampaignPersistence.load(directory))
      campaign:set_persistence_directory(directory)
      move_to_boundary(campaign, "west")
      assert(campaign:transition("west"))
      move_to_boundary(campaign, "east")
      assert(campaign:transition("east"))
      assert(frozen_snapshot(campaign) == before)
      assert(#campaign:zone_records() == 2 and campaign:validate())
    end,
  },
  {
    name = "a manifest-indexed missing visited shard fails travel without regenerating history",
    run = function()
      local campaign, directory = new_campaign(880012, "campaign:880012")
      move_to_boundary(campaign, "east")
      assert(campaign:transition("east"))
      move_to_boundary(campaign, "west")
      assert(campaign:transition("west"))
      local destination = assert(campaign:zone_record(ZoneKey.new(1, 0, 0)))
      assert(directory:file(CampaignPersistence.zone_filename(destination.key, destination.shard_revision)):delete())
      move_to_boundary(campaign, "east")
      local transitioned, failure = campaign:transition("east")
      assert(not transitioned and failure.code == "destination_load_failed")
      assert(ZoneKey.equal(campaign.state.current_zone, ZoneKey.new(0, 0, 0)))
    end,
  },
  {
    name = "campaign-zone inspector exposes zone provenance and connection overlay data without LÖVE",
    run = function()
      local floor = assert(InspectionFloor.generate_campaign_zone({ campaign_seed = 880010, world_x = -2, world_y = 3, z = 0 }))
      assert(floor.profile_id == "zone_profile.legacy.forest")
      assert(floor.connections.north and floor.connections.east and floor.connections.south and floor.connections.west)
      assert(floor.provenance.zone_key == "zone:-2:3:0")
      local inspector = Inspector.new({ campaign_seed = 880010, world_x = -2, world_y = 3, z = 0 })
      assert(inspector.overlays.surface_connections.east.id == floor.connections.east.id)
    end,
  },
}
