-- OW-01 campaign authority.  A Campaign owns the persistent identity,
-- current body/cargo reference, progression compatibility data, and zone
-- index. Session remains the deliberately local 2D simulation engine.
local Rng = require("src.rng")
local Session = require("src.simulation.session")
local ZoneKey = require("src.campaign.zone_key")
local ZoneRecord = require("src.campaign.zone_record")
local Identity = require("src.campaign.identity")

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

function Campaign.derive_zone_seed(seed, key)
  ZoneKey.validate(key)
  local derived = Rng.new(seed):derive(ZoneKey.encode(key))
  return derived.seed
end

function Campaign.zone_generation_rng(seed, key, subsystem)
  local root = Rng.new(Campaign.derive_zone_seed(seed, key))
  return root:derive(subsystem or "root")
end

local function record_map(record)
  return { [ZoneKey.encode(record.key)] = record }
end

function Campaign.new(options)
  options = options or {}
  local seed = Rng.new(options.seed or 1).seed
  local id = campaign_id(options.campaign_id or "campaign:000001")
  local key = options.current_zone and ZoneKey.from_data(options.current_zone) or ZoneKey.new(0, 0, 0)
  local profile_id = options.profile_id or Campaign.INITIAL_PROFILE_ID
  local generation_seed = Campaign.derive_zone_seed(seed, key)
  local campaign_identity = Identity.campaign_state_data(options.identity_state)
  local record = ZoneRecord.new(key, profile_id, generation_seed, options.zone_identity_state, { campaign_id = id, visited = true })
  local identity = Identity.new(id, key, campaign_identity, record.identity_state)
  -- The record keeps the exact mutable local allocator state used by the
  -- current zone. It is persisted in the zone shard, never globally shared.
  record.identity_state = identity.zone_state
  local self = setmetatable({
    state = {
      campaign_id = id,
      seed = seed,
      current_zone = key,
      run_id = id,
      meta_snapshot = options.meta_snapshot or { unlocked_research_ids = {}, unlock_ids = {}, modifiers = {}, discovered_discovery_ids = {} },
      identity_state = campaign_identity,
      zone_records = record_map(record),
      legacy_progression = nil,
      legacy_route = nil,
    },
    active_zone = record,
    identity = identity,
  }, Campaign)
  local session = Session.new({
    seed = seed,
    rng = Rng.new(generation_seed),
    content = options.content,
    registry = options.registry,
    route_definitions = options.route_definitions,
    meta_snapshot = self.state.meta_snapshot,
    identity_allocator = identity,
    campaign = self,
    campaign_state = self.state,
    on_meta_reward = options.on_meta_reward,
    emit = options.emit,
    run_id = id,
  })
  session:start_campaign_zone(key, generation_seed, profile_id, options.class, options.boon)
  self.session = session
  self:sync_active_references()
  self:validate()
  return self
end

function Campaign:sync_active_references()
  -- These are aliases, not copied serialized bodies. The one active body and
  -- cargo have a single runtime instance owned through the campaign/session
  -- boundary while ZoneRecord owns only local simulation state.
  self.state.active_body = self.session and self.session.state.run.player or nil
  self.state.carried_inventory = self.session and self.session.state.run.inventory or nil
  self.state.legacy_progression = self.session and self.session:to_data().progression or self.state.legacy_progression
  self.state.legacy_route = self.session and self.session.state.route and self.session.state.route:to_data() or self.state.legacy_route
end

function Campaign:zone_record(key)
  local canonical = ZoneKey.encode(key)
  return self.state.zone_records[canonical]
end

function Campaign:validate()
  local state = self.state
  campaign_id(state.campaign_id)
  assert(type(state.seed) == "number" and state.seed > 0 and state.seed % 1 == 0, "Campaign seed is invalid")
  ZoneKey.validate(state.current_zone)
  assert(self.active_zone and ZoneKey.equal(self.active_zone.key, state.current_zone), "Campaign current zone is missing")
  assert(self:zone_record(state.current_zone) == self.active_zone, "Campaign zone index is inconsistent")
  Identity.campaign_state_data(state.identity_state)
  assert(self.session and self.session.state.player == state.active_body, "Campaign active body is not canonical")
  assert(self.session.state.run.inventory == state.carried_inventory, "Campaign carried inventory is not canonical")
  assert(self.session.state.scrap == state.scrap, "Campaign SCRAP ownership is inconsistent")
  self.session:validate_physical_ownership()
  return true
end

-- A headless entry point for the future inspector/world generator.  It uses
-- the exact campaign zone root and named streams without creating any macro
-- map or traversal behavior.
function Campaign.generate_zone(seed, campaign_id_value, key, profile_id, options)
  options = options or {}
  local campaign = Campaign.new({
    seed = seed,
    campaign_id = campaign_id_value,
    current_zone = key,
    profile_id = profile_id,
    registry = options.registry,
    content = options.content,
    route_definitions = options.route_definitions,
    meta_snapshot = options.meta_snapshot,
  })
  return campaign.active_zone:to_data(), campaign.session:to_data()
end

function Campaign:to_manifest_data()
  self:sync_active_references()
  self:validate()
  local session_data = self.session:to_data()
  return {
    campaign_id = self.state.campaign_id,
    seed = self.state.seed,
    current_zone = ZoneKey.to_data(self.state.current_zone),
    meta_snapshot = self.state.meta_snapshot,
    identity_state = Identity.campaign_state_data(self.state.identity_state),
    -- Player and inventory are authored exactly once: in the campaign
    -- manifest. The zone shard only holds resident/local entities.
    active_body = session_data.player,
    carried_inventory = session_data.inventory,
    legacy_progression = session_data.progression,
    legacy_route = session_data.route,
    zones = { self.active_zone:to_data() },
  }
end

function Campaign:to_zone_data()
  self:sync_active_references()
  local snapshot = self.session:to_data()
  -- The active body/cargo and compatibility progression are campaign-owned.
  -- Everything left here is one fully authoritative zone simulation snapshot.
  snapshot.seed = nil
  snapshot.player = nil
  snapshot.inventory = nil
  snapshot.progression = nil
  snapshot.route = nil
  return {
    key = ZoneKey.to_data(self.active_zone.key),
    profile_id = self.active_zone.profile_id,
    generation_seed = self.active_zone.generation_seed,
    visited = self.active_zone.visited == true,
    identity_state = self.identity:zone_state_data(),
    simulation = snapshot,
  }
end

local function fake_session_data(manifest, shard)
  local simulation = plain_copy(shard.simulation)
  simulation.seed = manifest.seed
  simulation.player = manifest.active_body
  simulation.inventory = manifest.carried_inventory
  simulation.progression = manifest.legacy_progression
  simulation.route = manifest.legacy_route
  return simulation
end

function Campaign.from_data(manifest, shard, options)
  options = options or {}
  assert(type(manifest) == "table", "Campaign manifest must be a table")
  local id = campaign_id(manifest.campaign_id)
  local key = ZoneKey.from_data(manifest.current_zone)
  assert(type(manifest.zones) == "table" and #manifest.zones >= 1, "Campaign manifest has no zone index")
  local record
  for _, data in ipairs(manifest.zones) do
    local candidate = ZoneRecord.from_data(data, id)
    if ZoneKey.equal(candidate.key, key) then record = candidate break end
  end
  assert(record, "Campaign manifest does not index its current zone")
  assert(type(shard) == "table" and ZoneKey.equal(ZoneKey.from_data(shard.key), key), "Zone shard key does not match manifest")
  assert(shard.profile_id == record.profile_id and shard.generation_seed == record.generation_seed,
    "Zone shard metadata does not match manifest")
  record.identity_state = shard.identity_state or record.identity_state
  local identity_state = Identity.campaign_state_data(manifest.identity_state)
  local identity = Identity.new(id, key, identity_state, record.identity_state)
  record.identity_state = identity.zone_state
  local self = setmetatable({
    state = {
      campaign_id = id,
      seed = Rng.new(manifest.seed).seed,
      current_zone = key,
      run_id = id,
      meta_snapshot = manifest.meta_snapshot or {},
      identity_state = identity_state,
      zone_records = record_map(record),
      legacy_progression = manifest.legacy_progression,
      legacy_route = manifest.legacy_route,
    },
    active_zone = record,
    identity = identity,
  }, Campaign)
  local session = Session.from_data(fake_session_data(manifest, shard), {
    content = options.content,
    registry = options.registry,
    route_definitions = options.route_definitions,
    meta_snapshot = self.state.meta_snapshot,
    identity_allocator = identity,
    campaign = self,
    campaign_state = self.state,
    on_meta_reward = options.on_meta_reward,
    emit = options.emit,
  })
  self.session = session
  self:sync_active_references()
  self:validate()
  return self
end

return Campaign
