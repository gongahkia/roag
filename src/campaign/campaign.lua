-- Campaign owns durable macro state and one active 2D Session.  OW-02 keeps
-- the simulation local: inactive ZoneRecords are only revisioned shards.
local Rng = require("src.rng")
local Session = require("src.simulation.session")
local ZoneKey = require("src.campaign.zone_key")
local ZoneRecord = require("src.campaign.zone_record")
local Identity = require("src.campaign.identity")
local SurfaceWorld = require("src.campaign.surface_world")
local WorldTopology = require("src.campaign.world_topology")
local Grid = require("src.world.grid")
local Corpse = require("src.world.corpse")
local Inventory = require("src.inventory.inventory")
local PhysicalItem = require("src.inventory.physical_item")

local Campaign = {}
Campaign.__index = Campaign

Campaign.INITIAL_PROFILE_ID = "zone_profile.legacy.forest"

local function plain_copy(source)
  local result = {}
  for key, value in pairs(source or {}) do result[key] = value end
  return result
end

local function campaign_id(value)
  assert(Identity.is_campaign_id(value), "Campaign ID is invalid")
  return value
end

local function record_map(records)
  local result = {}
  for _, record in ipairs(records) do result[ZoneKey.encode(record.key)] = record end
  return result
end

local function sorted_records(records)
  local result = {}
  for _, record in pairs(records or {}) do result[#result + 1] = record end
  table.sort(result, function(a, b) return ZoneKey.encode(a.key) < ZoneKey.encode(b.key) end)
  return result
end

function Campaign.derive_zone_seed(seed, key)
  ZoneKey.validate(key)
  return Rng.new(seed):derive(ZoneKey.encode(key)).seed
end

function Campaign.zone_generation_rng(seed, key, subsystem)
  return Rng.new(Campaign.derive_zone_seed(seed, key)):derive(subsystem or "root")
end

function Campaign.is_zone_in_bounds(key)
  return WorldTopology.is_zone_in_bounds(key)
end

local function zone_connections(seed, key)
  return WorldTopology.is_zone_in_bounds(key) and WorldTopology.connections(seed, key) or {}
end

local function ensure_record_connections(seed, record)
  -- Connection metadata is derived only from campaign seed + ZoneKey. This
  -- makes OW-01/02 records safely additive: a former surface record gains its
  -- vertical landmark index without changing its established edge geometry.
  record.connections = zone_connections(seed, record.key)
  return record.connections
end

local function install_zone_connections(session, record)
  local connections = record.connections or {}
  local world = session.state.world
  local needs_surface_carve = false
  for _, direction in ipairs(SurfaceWorld.DIRECTION_ORDER) do
    local connection = WorldTopology.connection_at(record, direction)
    if connection and (not world:is_passable(connection.boundary.x, connection.boundary.y)
      or not world:is_passable(connection.interior.x, connection.interior.y)) then
      needs_surface_carve = true
      break
    end
  end
  -- Restored shards already own their physical throats. Re-carving one while
  -- staging a destination would use the travelling body's temporary local
  -- coordinates, so carve only fresh or compatibility-migrated terrain.
  if needs_surface_carve then SurfaceWorld.carve_connections(session, connections) end
  local reserved = session.state.surface_connector_cells or {}
  local player = session.state.player
  local restore_player
  -- A staged visited destination receives the body before final arrival is
  -- resolved. Its prior local coordinates can be solid in this different
  -- terrain, so give connection carving a deterministic temporary anchor.
  if player and not world:is_passable(player.x, player.y) then
    for y = 0, Grid.height - 1 do
      for x = 0, Grid.width - 1 do
        if world:is_passable(x, y) then
          restore_player = { x = player.x, y = player.y }
          player.x, player.y = x, y
          break
        end
      end
      if restore_player then break end
    end
    assert(restore_player, "Zone has no passable temporary connection anchor")
  end
  for _, direction in ipairs(WorldTopology.VERTICAL_DIRECTIONS) do
    local connection = WorldTopology.connection_at(record, direction)
    if connection then
      local cell = connection.cell
      local object = world:object_at(cell.x, cell.y)
      if object and object.zone_connection_id == connection.id then
        reserved[Grid.key(cell.x, cell.y)] = true
      else
        SurfaceWorld.reserve_interior_connection(session, cell, reserved)
        object = world:object_at(cell.x, cell.y)
        assert(not object, "Vertical connection cell could not be reserved")
        local placed, placement = world:place_object(WorldTopology.object_definition_for(connection.connection_type), cell.x, cell.y, {
          zone_connection_id = connection.id,
          zone_connection_type = connection.connection_type,
          zone_connection_direction = connection.direction,
        })
        assert(placed, placement and placement.reason)
      end
    end
  end
  if restore_player then player.x, player.y = restore_player.x, restore_player.y end
  session.state.surface_connector_cells = reserved
  session:validate_world()
end

local function anchor_data(anchor)
  if not anchor then return nil end
  return {
    zone_key = ZoneKey.to_data(anchor.zone_key),
    station_object_id = anchor.station_object_id,
  }
end

local function parse_anchor(data)
  if not data then return nil end
  assert(type(data) == "table" and type(data.station_object_id) == "string" and data.station_object_id ~= "",
    "Campaign reconstruction anchor is invalid")
  return { zone_key = ZoneKey.from_data(data.zone_key), station_object_id = data.station_object_id }
end

local function anchor_candidate(session, record, seed)
  local player, world = session.state.player, session.state.world
  local candidates = {}
  for _, point in ipairs(session:_reachable_floor_cells()) do
    local blocked = world:object_at(point.x, point.y) or world:is_hazardous(point.x, point.y)
    if not blocked and (not player or point.x ~= player.x or point.y ~= player.y) then
      local clear = true
      for _, connection in pairs(record.connections or {}) do
        local cell = connection.cell or connection.interior
        if cell and math.max(math.abs(point.x - cell.x), math.abs(point.y - cell.y)) <= 2 then clear = false; break end
      end
      if clear then candidates[#candidates + 1] = point end
    end
  end
  assert(#candidates > 0, "Campaign start has no legal reconstruction station location")
  return Rng.new(seed):derive("reconstruction_anchor." .. ZoneKey.encode(record.key)):shuffle(candidates)[1]
end

-- An anchor is a physical object, not a special menu state. Old campaign
-- saves receive one additively in their currently loaded zone; new campaigns
-- always create it at the initial surface zone.
local function ensure_reconstruction_anchor(campaign, record, session)
  local anchor = campaign.state.reconstruction_anchor
  if anchor then
    if ZoneKey.equal(anchor.zone_key, record.key) then
      local object = session.state.world:get_object(anchor.station_object_id)
      assert(object and object.interaction_role == "reconstruction_station" and not object.destroyed,
        "Campaign reconstruction anchor object is missing")
      object.anchor_protected = true
    end
    return anchor
  end
  local point = anchor_candidate(session, record, campaign.state.seed)
  local station, placement = session.state.world:place_object("world_object.station.reconstruction", point.x, point.y, {
    anchor_protected = true,
  })
  assert(station, placement and placement.reason)
  anchor = { zone_key = ZoneKey.from_data(record.key), station_object_id = station.id }
  campaign.state.reconstruction_anchor = anchor
  return anchor
end

function Campaign.new(options)
  options = options or {}
  local seed = Rng.new(options.seed or 1).seed
  local id = campaign_id(options.campaign_id or "campaign:000001")
  local key = options.current_zone and ZoneKey.from_data(options.current_zone) or ZoneKey.new(0, 0, 0)
  assert(WorldTopology.is_zone_in_bounds(key), "Campaign initial ZoneKey is outside world bounds")
  local profile_id = options.profile_id or WorldTopology.profile_for(key)
  local campaign_identity = Identity.campaign_state_data(options.identity_state)
  local record = ZoneRecord.new(key, profile_id, Campaign.derive_zone_seed(seed, key), options.zone_identity_state, {
    campaign_id = id, visited = true, connections = zone_connections(seed, key),
  })
  local identity = Identity.new(id, key, campaign_identity, record.identity_state)
  record.identity_state = identity.zone_state
  local self = setmetatable({
    state = {
      campaign_id = id, seed = seed, current_zone = key, run_id = id,
      meta_snapshot = options.meta_snapshot or { unlocked_research_ids = {}, unlock_ids = {}, modifiers = {}, discovered_discovery_ids = {} },
      identity_state = campaign_identity,
      zone_records = record_map({ record }),
      legacy_progression = nil, legacy_route = nil,
      reconstruction_anchor = parse_anchor(options.reconstruction_anchor),
      pending_successor = options.pending_successor,
      body_death_count = options.body_death_count or 0,
    },
    active_zone = record,
    identity = identity,
    _runtime_options = {
      content = options.content, registry = options.registry, route_definitions = options.route_definitions,
      on_meta_reward = options.on_meta_reward, emit = options.emit,
    },
  }, Campaign)
  local session = Session.new({
    seed = seed, rng = Rng.new(record.generation_seed), content = options.content, registry = options.registry,
    route_definitions = options.route_definitions, meta_snapshot = self.state.meta_snapshot,
    identity_allocator = identity, campaign = self, campaign_state = self.state,
    on_meta_reward = options.on_meta_reward, emit = options.emit, run_id = id,
  })
  session:start_campaign_zone(key, record.generation_seed, profile_id, options.class, options.boon)
  install_zone_connections(session, record)
  self.session = session
  ensure_reconstruction_anchor(self, record, session)
  self:sync_active_references()
  self:validate()
  return self
end

function Campaign:set_persistence_directory(directory)
  self.persistence_directory = directory
end

function Campaign:sync_active_references()
  if not self.session then return end
  self.state.active_body = self.session.state.run.player
  self.state.carried_inventory = self.session.state.run.inventory
  self.state.legacy_progression = self.session:to_data().progression
  self.state.legacy_route = self.session.state.route and self.session.state.route:to_data() or nil
end

function Campaign:zone_record(key)
  return self.state.zone_records[ZoneKey.encode(key)]
end

function Campaign:zone_records()
  return sorted_records(self.state.zone_records)
end

-- An anchor is a campaign reference to an ordinary station object. The newly
-- selected station is protected immediately; a former unloaded anchor may
-- remain conservatively protected until that frozen zone is next restored,
-- which is safe and cannot create a successor softlock.
function Campaign:set_reconstruction_anchor(station)
  local world = self.session and self.session.state.world
  if not world or not station or world:get_object(station.id) ~= station
    or station.destroyed or station.interaction_role ~= "reconstruction_station" then
    return { applied = false, code = "invalid_station", reason = "Reconstruction station is unavailable" }
  end
  local previous = self.state.reconstruction_anchor
  if previous and ZoneKey.equal(previous.zone_key, self.active_zone.key) then
    local old = world:get_object(previous.station_object_id)
    if old and old ~= station then old.anchor_protected = false end
  end
  station.anchor_protected = true
  self.state.reconstruction_anchor = { zone_key = ZoneKey.from_data(self.active_zone.key), station_object_id = station.id }
  self:sync_active_references()
  self:validate()
  return { applied = true, code = "anchor_set", station_object_id = station.id, zone_key = ZoneKey.to_data(self.active_zone.key) }
end

function Campaign:_new_record(key)
  assert(WorldTopology.is_zone_in_bounds(key), "Campaign zone is outside finite world bounds")
  local record = ZoneRecord.new(key, WorldTopology.profile_for(key), Campaign.derive_zone_seed(self.state.seed, key), nil, {
    campaign_id = self.state.campaign_id, visited = true, connections = zone_connections(self.state.seed, key),
  })
  self.state.zone_records[ZoneKey.encode(key)] = record
  return record
end

function Campaign:validate()
  local state = self.state
  campaign_id(state.campaign_id)
  assert(type(state.seed) == "number" and state.seed > 0 and state.seed % 1 == 0, "Campaign seed is invalid")
  ZoneKey.validate(state.current_zone)
  assert(self.active_zone and ZoneKey.equal(self.active_zone.key, state.current_zone), "Campaign current zone is missing")
  assert(self:zone_record(state.current_zone) == self.active_zone, "Campaign zone index is inconsistent")
  local anchor = state.reconstruction_anchor
  assert(anchor and WorldTopology.is_zone_in_bounds(anchor.zone_key) and type(anchor.station_object_id) == "string",
    "Campaign reconstruction anchor is invalid")
  assert(self:zone_record(anchor.zone_key), "Campaign reconstruction anchor zone is not indexed")
  Identity.campaign_state_data(state.identity_state)
  for _, record in ipairs(self:zone_records()) do
    ZoneRecord.from_data(record:to_data(), state.campaign_id)
    assert(record.visited, "Campaign zone index contains an unvisited record")
    if WorldTopology.is_zone_in_bounds(record.key) then
      for _, direction in ipairs(WorldTopology.DIRECTION_ORDER) do
        local expected = WorldTopology.connection(state.seed, record.key, direction)
        assert((expected and record.connections[direction]) or (not expected and not record.connections[direction]),
          "Campaign zone connection index is incomplete")
      end
    end
    for direction, data in pairs(record.connections or {}) do
      assert(WorldTopology.connection_matches(state.seed, record.key, data),
        "Campaign zone connection metadata is invalid for " .. tostring(direction))
    end
  end
  if state.pending_successor then
    assert(state.active_body == nil, "Pending successor campaign cannot retain an active body")
  else
    assert(self.session and self.session.state.player == state.active_body, "Campaign active body is not canonical")
    assert(self.session.state.run.inventory == state.carried_inventory, "Campaign carried inventory is not canonical")
  end
  assert(self.session.state.scrap == state.scrap, "Campaign SCRAP ownership is inconsistent")
  for _, direction in ipairs(WorldTopology.VERTICAL_DIRECTIONS) do
    local connection = WorldTopology.connection_at(self.active_zone, direction)
    if connection then
      local object = self.session.state.world:object_at(connection.cell.x, connection.cell.y)
      assert(object and object.interaction_role == "zone_connection" and object.zone_connection_id == connection.id
        and object.zone_connection_type == connection.connection_type and object.zone_connection_direction == direction,
        "Active zone is missing its physical vertical connection landmark")
    end
  end
  self.session:validate_physical_ownership()
  return true
end

-- The one canonical serializer for a zone. It deliberately omits the active
-- body/cargo/progression, whose sole durable representation is the manifest.
function Campaign:_zone_data_for(record, session)
  local snapshot = session:to_data()
  snapshot.seed, snapshot.player, snapshot.inventory = nil, nil, nil
  snapshot.progression, snapshot.route = nil, nil
  return {
    key = ZoneKey.to_data(record.key), profile_id = record.profile_id,
    generation_seed = record.generation_seed, visited = record.visited == true,
    identity_state = session.identity_allocator and session.identity_allocator:zone_state_data() or record.identity_state,
    simulation = snapshot,
  }
end

function Campaign:to_zone_data()
  self:sync_active_references()
  self.active_zone.identity_state = self.identity:zone_state_data()
  return self:_zone_data_for(self.active_zone, self.session)
end

function Campaign:to_manifest_data()
  self:sync_active_references()
  self.active_zone.identity_state = self.identity:zone_state_data()
  self:validate()
  local session_data = self.session:to_data()
  local zones = {}
  for _, record in ipairs(self:zone_records()) do zones[#zones + 1] = record:to_data() end
  return {
    campaign_id = self.state.campaign_id, seed = self.state.seed,
    current_zone = ZoneKey.to_data(self.state.current_zone), meta_snapshot = self.state.meta_snapshot,
    identity_state = Identity.campaign_state_data(self.state.identity_state),
    active_body = session_data.player, carried_inventory = session_data.inventory,
    legacy_progression = session_data.progression, legacy_route = session_data.route,
    reconstruction_anchor = anchor_data(self.state.reconstruction_anchor),
    pending_successor = self.state.pending_successor,
    body_death_count = self.state.body_death_count or 0,
    zones = zones,
  }
end

local function fake_session_data(manifest, shard)
  local simulation = plain_copy(shard.simulation)
  simulation.seed, simulation.player, simulation.inventory = manifest.seed, manifest.active_body, manifest.carried_inventory
  simulation.progression, simulation.route = manifest.legacy_progression, manifest.legacy_route
  return simulation
end

function Campaign:_session_from_shard(record, shard, active_player, carried_inventory)
  assert(ZoneKey.equal(ZoneKey.from_data(shard.key), record.key), "Zone shard key does not match record")
  assert(shard.profile_id == record.profile_id and shard.generation_seed == record.generation_seed,
    "Zone shard metadata does not match record")
  record.identity_state = shard.identity_state or record.identity_state
  local identity = Identity.new(self.state.campaign_id, record.key, self.state.identity_state, record.identity_state)
  record.identity_state = identity.zone_state
  local manifest = {
    seed = self.state.seed, active_body = active_player or self.state.active_body,
    carried_inventory = carried_inventory or self.state.carried_inventory,
    legacy_progression = self.state.legacy_progression, legacy_route = self.state.legacy_route,
  }
  local session = Session.from_data(fake_session_data(manifest, shard), {
    content = self._runtime_options.content, registry = self._runtime_options.registry,
    route_definitions = self._runtime_options.route_definitions, meta_snapshot = self.state.meta_snapshot,
    identity_allocator = identity, campaign = self, campaign_state = self.state,
    on_meta_reward = self._runtime_options.on_meta_reward, emit = self._runtime_options.emit,
    active_player = active_player, carried_inventory = carried_inventory,
  })
  return session, identity
end

-- Generate through an isolated preview campaign allocator. The disposable
-- preview player may consume campaign-scope IDs, but its counters are never
-- committed; zone-local identity counters are committed to the ZoneRecord.
function Campaign:_generate_zone_session(record, active_player, carried_inventory)
  local preview_campaign_identity = Identity.campaign_state_data(self.state.identity_state)
  local identity = Identity.new(self.state.campaign_id, record.key, preview_campaign_identity, record.identity_state)
  local preview_state = {
    campaign_id = self.state.campaign_id, seed = self.state.seed, run_id = self.state.run_id,
    meta_snapshot = self.state.meta_snapshot, identity_state = preview_campaign_identity,
  }
  local generated = Session.new({
    seed = self.state.seed, rng = Rng.new(record.generation_seed), content = self._runtime_options.content,
    registry = self._runtime_options.registry, route_definitions = self._runtime_options.route_definitions,
    meta_snapshot = self.state.meta_snapshot, identity_allocator = identity, campaign_state = preview_state,
    run_id = self.state.run_id,
  })
  generated:start_campaign_zone(record.key, record.generation_seed, record.profile_id)
  install_zone_connections(generated, record)
  record.identity_state = identity:zone_state_data()
  local shard = self:_zone_data_for(record, generated)
  -- Rehydrate with the real existing body; generated's temporary player is
  -- excluded from the shard and therefore never becomes an owned duplicate.
  return self:_session_from_shard(record, shard, active_player or self.state.active_body, carried_inventory or self.state.carried_inventory)
end

function Campaign:_load_zone_session(record, directory, active_player, carried_inventory)
  local Persistence = require("src.persistence.campaign")
  ensure_record_connections(self.state.seed, record)
  if not directory then return nil, { code = "persistence_unavailable", reason = "Campaign persistence is unavailable" } end
  if not record.shard_revision or record.shard_revision < 1 then
    return nil, { code = "destination_load_failed", reason = "Visited zone has no committed shard" }
  end
  local text, read_error = directory:file(Persistence.zone_filename(record.key, record.shard_revision)):read()
  if not text then return nil, { code = "destination_load_failed", reason = read_error and read_error.reason or "Could not read zone shard" } end
  local shard, decode_error = Persistence.decode_zone(text)
  if not shard then return nil, { code = "destination_load_failed", reason = decode_error.reason } end
  local ok, session_or_error, identity = xpcall(function()
    local session, zone_identity = self:_session_from_shard(record, shard, active_player or self.state.active_body,
      carried_inventory or self.state.carried_inventory)
    install_zone_connections(session, record)
    return session, zone_identity
  end, debug.traceback)
  if not ok then return nil, { code = "destination_load_failed", reason = tostring(session_or_error) } end
  return session_or_error, identity
end

local function occupied(session, x, y)
  local state = session.state
  if not state.world:is_passable(x, y) then return true end
  for _, collection in ipairs({ state.enemies, state.targets, state.corpses, state.bullets, state.bombs,
    state.flares, state.torches, state.area_attacks }) do
    for _, value in ipairs(collection or {}) do if value.x == x and value.y == y then return true end end
  end
  if state.boss and state.boss.x == x and state.boss.y == y then return true end
  if state.ammo and state.ammo.x == x and state.ammo.y == y then return true end
  if state.exit and state.exit.x == x and state.exit.y == y then return true end
  return false
end

function Campaign:_resolve_arrival(session, connection)
  local preferred = connection.interior
  -- Stable bounded rings: preferred first, then y/x lexical order within each
  -- Manhattan radius. Arrival never falls back to an arbitrary map center.
  for radius = 0, 4 do
    for y = preferred.y - radius, preferred.y + radius do
      for x = preferred.x - radius, preferred.x + radius do
        if math.abs(x - preferred.x) + math.abs(y - preferred.y) == radius
          and x >= 0 and x < Grid.width and y >= 0 and y < Grid.height
          and not occupied(session, x, y) then
          return { x = x, y = y }
        end
      end
    end
  end
  return nil, { code = "arrival_blocked", reason = "No safe arrival cell exists near the surface connection" }
end

-- During a staged transfer two zone snapshots are momentarily available.
-- Check their serialized identities together with the one campaign body so a
-- future ownership change cannot quietly duplicate a component/actor/world
-- instance across the source and destination boundaries.
function Campaign:_validate_transition_ownership(source, destination)
  local seen = {}
  local function add(id, owner)
    if not id then return end
    if seen[id] then
      error("Persistent identity '" .. id .. "' is owned by both " .. seen[id] .. " and " .. owner)
    end
    seen[id] = owner
  end
  local function body(data, owner)
    for _, slot in ipairs((data and data.slots) or {}) do if slot.component then add(slot.component.id, owner) end end
  end
  local function actor(data, owner)
    if not data then return end
    add(data.actor_id, owner)
    body(data.body, owner)
  end
  local function inventory(data, owner)
    for _, entry in ipairs((data and data.entries) or {}) do
      add(entry.physical_id or (entry.item and entry.item.physical_id)
        or (entry.item and entry.item.component and entry.item.component.id), owner)
    end
  end
  local active = self.session:to_data()
  actor(active.player, "campaign active body")
  inventory(active.inventory, "campaign cargo")
  local function zone(data, owner)
    local simulation = data.simulation
    for _, value in ipairs(simulation.enemies or {}) do actor(value, owner .. " enemy") end
    actor(simulation.boss, owner .. " boss")
    for _, corpse in ipairs(simulation.corpses or {}) do
      add(corpse.id, owner .. " corpse")
      body(corpse.body, owner .. " corpse")
      inventory(corpse.carried_inventory, owner .. " corpse cargo")
    end
    local world = simulation.world or {}
    for _, object in ipairs(world.objects or {}) do
      add(object.id, owner .. " object")
      inventory(object.storage_inventory, owner .. " storage")
    end
    for _, hazard in ipairs(world.hazards or {}) do add(hazard.id, owner .. " hazard") end
    for _, fire in ipairs(world.fires or {}) do add(fire.id, owner .. " fire") end
    for _, ground in ipairs(world.ground_items or {}) do add(ground.id, owner .. " ground item") end
  end
  zone(source, "source zone")
  zone(destination, "destination zone")
  return true
end

function Campaign:transition(direction, directory)
  local source_record, source_session = self.active_zone, self.session
  local connection = WorldTopology.connection_at(source_record, direction)
  if not connection then
    local destination = WorldTopology.neighbor(source_record.key, direction)
    return nil, { code = WorldTopology.is_zone_in_bounds(destination) and "no_zone_connection" or "world_boundary",
      reason = "No zone connection exists in that direction" }
  end
  local player = source_session.state.player
  local at_connection
  if WorldTopology.is_vertical(direction) then
    at_connection = math.abs(player.x - connection.cell.x) <= 1 and math.abs(player.y - connection.cell.y) <= 1
  else
    at_connection = player.x == connection.boundary.x and player.y == connection.boundary.y
  end
  if not at_connection then
    return nil, { code = "no_zone_connection", reason = "Travel requires reaching the zone connection" }
  end
  directory = directory or self.persistence_directory
  if not directory then return nil, { code = "persistence_unavailable", reason = "Campaign persistence is unavailable" } end
  local destination_record = self:zone_record(connection.destination)
  local created = false
  if not destination_record then destination_record, created = self:_new_record(connection.destination), true end
  local destination_session, destination_identity, load_error
  if destination_record.shard_revision and destination_record.shard_revision > 0 then
    destination_session, destination_identity = self:_load_zone_session(destination_record, directory)
  else
    local ok, result, identity_or_error = xpcall(function()
      return self:_generate_zone_session(destination_record)
    end, debug.traceback)
    if ok then destination_session, destination_identity = result, identity_or_error
    else load_error = { code = "destination_generation_failed", reason = tostring(result) } end
  end
  if not destination_session then
    if created then self.state.zone_records[ZoneKey.encode(destination_record.key)] = nil end
    return nil, load_error or { code = "destination_load_failed", reason = "Could not prepare destination zone" }
  end
  local arrival_connection = WorldTopology.connection_at(destination_record, WorldTopology.opposite(direction))
  if not arrival_connection or arrival_connection.id ~= connection.id or arrival_connection.connection_type ~= connection.connection_type then
    if created then self.state.zone_records[ZoneKey.encode(destination_record.key)] = nil end
    return nil, { code = "destination_generation_failed", reason = "Destination zone connection is not reciprocal" }
  end
  local arrival, arrival_error = self:_resolve_arrival(destination_session, arrival_connection)
  if not arrival then
    if created then self.state.zone_records[ZoneKey.encode(destination_record.key)] = nil end
    return nil, arrival_error
  end
  source_record.identity_state = self.identity:zone_state_data()
  destination_record.identity_state = destination_identity:zone_state_data()
  local source_data = self:_zone_data_for(source_record, source_session)
  local destination_data = self:_zone_data_for(destination_record, destination_session)
  self:_validate_transition_ownership(source_data, destination_data)
  local Persistence = require("src.persistence.campaign")
  local source_revision, destination_revision = source_record.shard_revision, destination_record.shard_revision
  local new_source_revision, source_error = Persistence.write_zone_data(source_record, source_data, directory)
  if not new_source_revision then
    if created then self.state.zone_records[ZoneKey.encode(destination_record.key)] = nil end
    return nil, { code = "persistence_failed", reason = source_error.reason, detail = source_error.code }
  end
  local new_destination_revision, destination_error = Persistence.write_zone_data(destination_record, destination_data, directory)
  if not new_destination_revision then
    if created then self.state.zone_records[ZoneKey.encode(destination_record.key)] = nil end
    return nil, { code = "persistence_failed", reason = destination_error.reason, detail = destination_error.code }
  end
  local old_key, old_active, old_identity = self.state.current_zone, self.active_zone, self.identity
  local old_x, old_y, old_direction = player.x, player.y, player.direction
  source_record.shard_revision, destination_record.shard_revision = new_source_revision, new_destination_revision
  self.state.current_zone, self.active_zone, self.identity, self.session = destination_record.key, destination_record, destination_identity, destination_session
  player.x, player.y = arrival.x, arrival.y
  self:sync_active_references()
  local committed, manifest_error = Persistence.write_manifest(self, directory)
  if not committed then
    source_record.shard_revision, destination_record.shard_revision = source_revision, destination_revision
    self.state.current_zone, self.active_zone, self.identity, self.session = old_key, old_active, old_identity, source_session
    player.x, player.y, player.direction = old_x, old_y, old_direction
    self:sync_active_references()
    if created then self.state.zone_records[ZoneKey.encode(destination_record.key)] = nil end
    return nil, { code = "persistence_failed", reason = manifest_error.reason, detail = manifest_error.code }
  end
  self:validate()
  return { applied = true, code = "zone_transition", direction = direction, connection_id = connection.id,
    from = ZoneKey.to_data(source_record.key), to = ZoneKey.to_data(destination_record.key), arrival = arrival,
    generated = created, source_revision = new_source_revision, destination_revision = new_destination_revision }
end

function Campaign:request_transition(direction)
  return self:transition(direction, self.persistence_directory)
end

local function charm_ids(state)
  local values = {}
  for _, charm_id in pairs(state.charms and state.charms.slots or {}) do
    if charm_id then values[#values + 1] = charm_id end
  end
  table.sort(values)
  return values
end

local function corpse_in_session(session, corpse_id)
  for _, corpse in ipairs(session.state.corpses or {}) do
    if corpse.id == corpse_id then return corpse end
  end
  return nil
end

local function inventory_from_data(data, registry)
  return Inventory.from_data(data, function(item) return PhysicalItem.from_data(item, registry) end)
end

function Campaign:_resolve_anchor_arrival(session, station)
  -- The station cell itself remains passable for rendering/interaction, but
  -- a reconstructed body always arrives beside it. Ordering is bounded and
  -- lexical, so an occupied preferred cell cannot create visit-order drift.
  for radius = 1, 4 do
    for y = station.y - radius, station.y + radius do
      for x = station.x - radius, station.x + radius do
        if math.max(math.abs(x - station.x), math.abs(y - station.y)) == radius
          and x >= 0 and x < Grid.width and y >= 0 and y < Grid.height
          and not session.state.world:object_at(x, y) and not occupied(session, x, y) then
          return { x = x, y = y }
        end
      end
    end
  end
  return nil, { code = "arrival_blocked", reason = "No safe reconstruction arrival cell exists" }
end

-- Complete a pending campaign death from either the live fatal handler or a
-- later load reconciliation. The pending manifest already contains both the
-- exact dead body/cargo and a fresh successor identity, making retries
-- idempotent even if a later shard or manifest write failed.
function Campaign:_complete_pending_successor(pending, directory)
  directory = directory or self.persistence_directory
  if not directory then return nil, { code = "persistence_unavailable", reason = "Campaign persistence is unavailable" } end
  assert(type(pending) == "table" and type(pending.corpse) == "table" and type(pending.successor_player) == "table"
    and type(pending.successor_inventory) == "table", "Campaign pending successor is invalid")
  local source_record, source_session = self.active_zone, self.session
  assert(ZoneKey.equal(source_record.key, ZoneKey.from_data(pending.death_zone)), "Pending successor source zone is invalid")

  if not corpse_in_session(source_session, pending.corpse.id) then
    source_session.state.corpses[#source_session.state.corpses + 1] = Corpse.from_data(source_session.registry, pending.corpse)
  end
  source_session.state.player = nil
  source_session.state.run.player = nil
  source_session.state.run.inventory = Inventory.new({
    height = Inventory.DEFAULT_HEIGHT + ((self.state.meta_snapshot.modifiers and self.state.meta_snapshot.modifiers.inventory_rows) or 0),
  })
  source_session.state.inventory = source_session.state.run.inventory
  source_session.state.curse, source_session.state.curse_id = nil, nil
  source_session.state.charms = { slots = {} }
  source_session:validate_physical_ownership()

  local successor = source_session:_actor_from_data(pending.successor_player)
  local successor_inventory = inventory_from_data(pending.successor_inventory, source_session.registry)
  local anchor = assert(self.state.reconstruction_anchor, "Campaign has no reconstruction anchor")
  local anchor_record = assert(self:zone_record(anchor.zone_key), "Campaign anchor zone is not indexed")
  local destination_session, destination_identity
  if ZoneKey.equal(source_record.key, anchor_record.key) then
    destination_session, destination_identity = source_session, self.identity
    destination_session.state.player, destination_session.state.run.player = successor, successor
    destination_session.state.run.inventory, destination_session.state.inventory = successor_inventory, successor_inventory
  elseif anchor_record.shard_revision and anchor_record.shard_revision > 0 then
    destination_session, destination_identity = self:_load_zone_session(anchor_record, directory, successor, successor_inventory)
  else
    local ok, generated, identity_or_error = xpcall(function()
      return self:_generate_zone_session(anchor_record, successor, successor_inventory)
    end, debug.traceback)
    if ok then destination_session, destination_identity = generated, identity_or_error
    else return nil, { code = "anchor_generation_failed", reason = tostring(generated) } end
  end
  if not destination_session then
    return nil, { code = "anchor_load_failed", reason = "Could not load reconstruction anchor zone" }
  end
  local station = destination_session.state.world:get_object(anchor.station_object_id)
  if not station or station.destroyed or station.interaction_role ~= "reconstruction_station" then
    return nil, { code = "anchor_missing", reason = "Campaign reconstruction anchor is unavailable" }
  end
  local arrival, arrival_error = self:_resolve_anchor_arrival(destination_session, station)
  if not arrival then return nil, arrival_error end
  successor.x, successor.y, successor.direction = arrival.x, arrival.y, "w"
  destination_session:refresh_visibility()
  destination_session:validate_physical_ownership()

  source_record.identity_state = self.identity:zone_state_data()
  anchor_record.identity_state = destination_identity:zone_state_data()
  local source_data = self:_zone_data_for(source_record, source_session)
  local destination_data = ZoneKey.equal(source_record.key, anchor_record.key) and source_data
    or self:_zone_data_for(anchor_record, destination_session)
  if not ZoneKey.equal(source_record.key, anchor_record.key) then
    self:_validate_transition_ownership(source_data, destination_data)
  end
  local Persistence = require("src.persistence.campaign")
  local source_previous, anchor_previous = source_record.shard_revision, anchor_record.shard_revision
  local source_revision, source_error = Persistence.write_zone_data(source_record, source_data, directory)
  if not source_revision then return nil, { code = "persistence_failed", reason = source_error.reason, detail = source_error.code } end
  local anchor_revision = source_revision
  if not ZoneKey.equal(source_record.key, anchor_record.key) then
    local error_data
    anchor_revision, error_data = Persistence.write_zone_data(anchor_record, destination_data, directory)
    if not anchor_revision then return nil, { code = "persistence_failed", reason = error_data.reason, detail = error_data.code } end
  end

  local old_key, old_record, old_identity, old_session = self.state.current_zone, self.active_zone, self.identity, self.session
  source_record.shard_revision, anchor_record.shard_revision = source_revision, anchor_revision
  self.state.current_zone, self.active_zone, self.identity, self.session = anchor_record.key, anchor_record, destination_identity, destination_session
  self.state.pending_successor = nil
  self.state.body_death_count = (self.state.body_death_count or 0) + 1
  destination_session:_log("BODY LOST. SUCCESSOR RECONSTRUCTED.")
  self:sync_active_references()
  local committed, manifest_error = Persistence.write_manifest(self, directory)
  if not committed then
    source_record.shard_revision, anchor_record.shard_revision = source_previous, anchor_previous
    self.state.current_zone, self.active_zone, self.identity, self.session = old_key, old_record, old_identity, old_session
    self.state.pending_successor = pending
    self:sync_active_references()
    return nil, { code = "persistence_failed", reason = manifest_error.reason, detail = manifest_error.code }
  end
  self:validate()
  return {
    applied = true, code = "campaign_succession", corpse_id = pending.corpse.id,
    body_id = successor.actor_id, death_zone = pending.death_zone,
    anchor_zone = ZoneKey.to_data(anchor_record.key), arrival = arrival,
    source_revision = source_revision, anchor_revision = anchor_revision,
  }
end

function Campaign:handle_player_death(provenance)
  if self.state.pending_successor then
    return self:_complete_pending_successor(self.state.pending_successor, self.persistence_directory)
  end
  local source = self.session
  assert(source and source.state.player and source.state.player.body, "Campaign death requires active body")
  local player = source.state.player
  local corpse_id = self.identity:allocate_corpse_id()
  local dead_player = source:_actor_to_data(player)
  local dead_inventory = source.state.inventory:to_data()
  local successor, successor_inventory = source:make_campaign_successor()
  local key = ZoneKey.to_data(self.active_zone.key)
  local pending = {
    death_zone = key,
    corpse = {
      id = corpse_id, kind = "corpse", source_kind = "player", source_actor_id = player.content_id,
      x = player.x, y = player.y, body = dead_player.body, carried_inventory = dead_inventory,
      source_body_id = player.actor_id, campaign_id = self.state.campaign_id, death_zone_key = key,
      death_cause = provenance and (provenance.cause or provenance.source) or "unknown",
      lost_charm_ids = charm_ids(source.state),
    },
    successor_player = source:_actor_to_data(successor),
    successor_inventory = successor_inventory:to_data(),
  }
  -- Make the pending manifest authoritative before altering a committed zone.
  -- It has enough detached data to rebuild the corpse and successor exactly.
  source.state.player, source.state.run.player = nil, nil
  source.state.run.inventory = Inventory.new({ height = successor_inventory.height })
  source.state.inventory = source.state.run.inventory
  source.state.curse, source.state.curse_id = nil, nil
  source.state.charms = { slots = {} }
  self.state.pending_successor = pending
  self:sync_active_references()
  local Persistence = require("src.persistence.campaign")
  local marked, marker_error = Persistence.write_manifest(self, self.persistence_directory)
  if not marked then
    return nil, { code = "persistence_failed", reason = marker_error.reason, detail = marker_error.code }
  end
  return self:_complete_pending_successor(pending, self.persistence_directory)
end

-- Headless construction API used by inspectors/analyzers. It uses the exact
-- campaign/ZoneKey root and connection carving without creating macro travel.
function Campaign.generate_zone(seed, campaign_id_value, key, profile_id, options)
  options = options or {}
  local campaign = Campaign.new({ seed = seed, campaign_id = campaign_id_value, current_zone = key,
    profile_id = profile_id, registry = options.registry, content = options.content,
    route_definitions = options.route_definitions, meta_snapshot = options.meta_snapshot })
  return campaign.active_zone:to_data(), campaign.session:to_data()
end

function Campaign.inspect_surface_zone(seed, campaign_id_value, key, options)
  assert(WorldTopology.is_zone_in_bounds(key), "Inspection requires an in-bounds campaign ZoneKey")
  local record, data = Campaign.generate_zone(seed, campaign_id_value or "campaign:000001", key,
    WorldTopology.profile_for(key), options)
  return { record = record, simulation = data, connections = record.connections }
end

function Campaign.from_data(manifest, shard, options)
  options = options or {}
  assert(type(manifest) == "table", "Campaign manifest must be a table")
  local id, key = campaign_id(manifest.campaign_id), ZoneKey.from_data(manifest.current_zone)
  assert(type(manifest.zones) == "table" and #manifest.zones >= 1, "Campaign manifest has no zone index")
  local records = {}
  for _, data in ipairs(manifest.zones) do records[#records + 1] = ZoneRecord.from_data(data, id) end
  -- All records, including frozen OW-02 surface shards, receive the same
  -- deterministic additive vertical index before campaign validation. Their
  -- physical landmark is added only when that specific shard becomes active.
  local manifest_seed = Rng.new(manifest.seed).seed
  for _, candidate in ipairs(records) do ensure_record_connections(manifest_seed, candidate) end
  local map, record = record_map(records), nil
  for _, candidate in ipairs(records) do if ZoneKey.equal(candidate.key, key) then record = candidate break end end
  assert(record, "Campaign manifest does not index its current zone")
  assert(type(shard) == "table" and ZoneKey.equal(ZoneKey.from_data(shard.key), key), "Zone shard key does not match manifest")
  assert(shard.profile_id == record.profile_id and shard.generation_seed == record.generation_seed,
    "Zone shard metadata does not match manifest")
  record.identity_state = shard.identity_state or record.identity_state
  local identity_state = Identity.campaign_state_data(manifest.identity_state)
  local identity = Identity.new(id, key, identity_state, record.identity_state)
  record.identity_state = identity.zone_state
  local self = setmetatable({
    state = { campaign_id = id, seed = manifest_seed, current_zone = key, run_id = id,
      meta_snapshot = manifest.meta_snapshot or {}, identity_state = identity_state, zone_records = map,
      legacy_progression = manifest.legacy_progression, legacy_route = manifest.legacy_route,
      reconstruction_anchor = parse_anchor(manifest.reconstruction_anchor),
      pending_successor = manifest.pending_successor,
      body_death_count = manifest.body_death_count or 0 },
    active_zone = record, identity = identity,
    _runtime_options = { content = options.content, registry = options.registry, route_definitions = options.route_definitions,
      on_meta_reward = options.on_meta_reward, emit = options.emit },
  }, Campaign)
  self.persistence_directory = options.persistence_directory
  -- A pending marker deliberately has no active player in its manifest. Use
  -- the serialized successor only as a temporary parser body while the
  -- reconciliation operation restores the corpse, then injects that same
  -- successor at the anchor exactly once.
  local load_manifest = manifest
  if manifest.pending_successor then
    assert(type(manifest.pending_successor.successor_player) == "table"
      and type(manifest.pending_successor.successor_inventory) == "table", "Pending campaign successor is invalid")
    load_manifest = plain_copy(manifest)
    load_manifest.active_body = manifest.pending_successor.successor_player
    load_manifest.carried_inventory = manifest.pending_successor.successor_inventory
  end
  local session = Session.from_data(fake_session_data(load_manifest, shard), {
    content = options.content, registry = options.registry, route_definitions = options.route_definitions,
    meta_snapshot = self.state.meta_snapshot, identity_allocator = identity, campaign = self, campaign_state = self.state,
    on_meta_reward = options.on_meta_reward, emit = options.emit,
  })
  self.session = session
  -- Existing campaign v1 saves gain deterministic additive topology when a
  -- zone becomes active. Their immutable old shard is retained until normal
  -- save/transition creates the next revision.
  ensure_record_connections(self.state.seed, record)
  install_zone_connections(session, record)
  ensure_reconstruction_anchor(self, record, session)
  if self.state.pending_successor then
    local reconciled, reconciliation_error = self:_complete_pending_successor(self.state.pending_successor, self.persistence_directory)
    assert(reconciled, reconciliation_error and reconciliation_error.reason or "Campaign succession reconciliation failed")
    return self
  end
  self:sync_active_references()
  self:validate()
  return self
end

return Campaign
