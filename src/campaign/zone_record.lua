-- Persisted index entry for one zone. The full authoritative simulation is
-- stored in its separate shard; this record is the small campaign manifest
-- reference that can grow into a world index in OW-02.
local ZoneKey = require("src.campaign.zone_key")
local Identity = require("src.campaign.identity")

local ZoneRecord = {}
ZoneRecord.__index = ZoneRecord

function ZoneRecord.new(key, profile_id, generation_seed, identity_state, options)
  options = options or {}
  assert(type(profile_id) == "string" and profile_id ~= "", "ZoneRecord profile ID is required")
  assert(type(generation_seed) == "number" and generation_seed % 1 == 0 and generation_seed > 0,
    "ZoneRecord generation seed is invalid")
  assert(type(options.shard_revision or 0) == "number" and (options.shard_revision or 0) >= 0
    and (options.shard_revision or 0) % 1 == 0, "ZoneRecord shard revision is invalid")
  return setmetatable({
    key = ZoneKey.from_data(key),
    profile_id = profile_id,
    generation_seed = generation_seed,
    visited = options.visited ~= false,
    -- The manifest points at an immutable shard revision. A failed manifest
    -- commit can therefore leave a harmless orphan without changing what the
    -- previous manifest loads.
    shard_revision = options.shard_revision or 0,
    identity_state = identity_state and Identity.new(options.campaign_id or "campaign:000001", key, {}, identity_state):zone_state_data()
      or Identity.new(options.campaign_id or "campaign:000001", key, {}, nil):zone_state_data(),
  }, ZoneRecord)
end

function ZoneRecord:to_data()
  return {
    key = ZoneKey.to_data(self.key),
    profile_id = self.profile_id,
    generation_seed = self.generation_seed,
    visited = self.visited == true,
    shard_revision = self.shard_revision,
    identity_state = self.identity_state,
  }
end

function ZoneRecord.from_data(data, campaign_id)
  assert(type(data) == "table", "ZoneRecord must be a table")
  return ZoneRecord.new(ZoneKey.from_data(data.key), data.profile_id, data.generation_seed, data.identity_state, {
    visited = data.visited == true,
    shard_revision = data.shard_revision or 0,
    campaign_id = campaign_id,
  })
end

return ZoneRecord
