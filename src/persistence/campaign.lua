-- Versioned campaign manifest + independently committed zone shards.
local Json = require("src.persistence.json")
local Campaign = require("src.campaign.campaign")
local ZoneKey = require("src.campaign.zone_key")

local CampaignPersistence = {
  FORMAT = "roag.campaign",
  VERSION = 1,
  ZONE_FORMAT = "roag.campaign_zone",
  ZONE_VERSION = 1,
  MANIFEST_FILE = "manifest.json",
}

local function failure(code, reason)
  return nil, { code = code, reason = reason }
end

local function assert_directory(store)
  assert(type(store) == "table" and type(store.file) == "function", "Campaign persistence requires a named-file store")
end

function CampaignPersistence.zone_filename(key)
  return "zones/" .. ZoneKey.filename(key)
end

function CampaignPersistence.encode_manifest(campaign)
  local ok, data_or_error = pcall(function() return campaign:to_manifest_data() end)
  if not ok then return failure("invalid_state", tostring(data_or_error)) end
  local text, reason = Json.encode({
    format = CampaignPersistence.FORMAT,
    version = CampaignPersistence.VERSION,
    campaign = data_or_error,
  })
  if not text then return failure("encode_failed", tostring(reason)) end
  return text
end

function CampaignPersistence.encode_zone(campaign)
  local ok, data_or_error = pcall(function() return campaign:to_zone_data() end)
  if not ok then return failure("invalid_state", tostring(data_or_error)) end
  local text, reason = Json.encode({
    format = CampaignPersistence.ZONE_FORMAT,
    version = CampaignPersistence.ZONE_VERSION,
    zone = data_or_error,
  })
  if not text then return failure("encode_failed", tostring(reason)) end
  return text
end

function CampaignPersistence.decode_manifest(text)
  local envelope, reason = Json.decode(text)
  if not envelope then return failure("invalid_json", tostring(reason)) end
  if type(envelope) ~= "table" then return failure("invalid_state", "Campaign manifest envelope must be an object") end
  if envelope.format ~= CampaignPersistence.FORMAT then return failure("unsupported_format", "Save format is not roag.campaign") end
  if envelope.version ~= CampaignPersistence.VERSION then return failure("unsupported_version", "Campaign version is not supported") end
  if type(envelope.campaign) ~= "table" then return failure("invalid_state", "Campaign manifest is missing campaign data") end
  return envelope.campaign
end

function CampaignPersistence.decode_zone(text)
  local envelope, reason = Json.decode(text)
  if not envelope then return failure("invalid_json", tostring(reason)) end
  if type(envelope) ~= "table" then return failure("invalid_state", "Zone shard envelope must be an object") end
  if envelope.format ~= CampaignPersistence.ZONE_FORMAT then return failure("unsupported_format", "Save format is not roag.campaign_zone") end
  if envelope.version ~= CampaignPersistence.ZONE_VERSION then return failure("unsupported_version", "Zone version is not supported") end
  if type(envelope.zone) ~= "table" then return failure("invalid_state", "Zone shard is missing zone data") end
  return envelope.zone
end

function CampaignPersistence.save(campaign, directory)
  assert_directory(directory)
  local zone_text, zone_error = CampaignPersistence.encode_zone(campaign)
  if not zone_text then return nil, zone_error end
  local zone_store = directory:file(CampaignPersistence.zone_filename(campaign.state.current_zone))
  -- Zone first: the previous manifest remains the committed head whenever a
  -- new zone write fails. A manifest failure may leave an orphan newer shard,
  -- which is harmless because load follows only the old manifest reference.
  local zone_written, zone_write_error = zone_store:write(zone_text)
  if not zone_written then return failure("zone_write_failed", (zone_write_error and zone_write_error.reason) or "Could not write zone shard") end
  local manifest_text, manifest_error = CampaignPersistence.encode_manifest(campaign)
  if not manifest_text then return nil, manifest_error end
  local manifest_written, manifest_write_error = directory:file(CampaignPersistence.MANIFEST_FILE):write(manifest_text)
  if not manifest_written then return failure("manifest_write_failed", (manifest_write_error and manifest_write_error.reason) or "Could not write campaign manifest") end
  return true
end

function CampaignPersistence.load(directory, options)
  assert_directory(directory)
  local manifest_text, manifest_error = directory:file(CampaignPersistence.MANIFEST_FILE):read()
  if not manifest_text then return nil, manifest_error end
  local manifest, decoded_manifest_error = CampaignPersistence.decode_manifest(manifest_text)
  if not manifest then return nil, decoded_manifest_error end
  local ok_key, key_or_error = pcall(ZoneKey.from_data, manifest.current_zone)
  if not ok_key then return failure("invalid_state", tostring(key_or_error)) end
  local zone_text, zone_error = directory:file(CampaignPersistence.zone_filename(key_or_error)):read()
  if not zone_text then return nil, zone_error end
  local shard, decoded_zone_error = CampaignPersistence.decode_zone(zone_text)
  if not shard then return nil, decoded_zone_error end
  local ok, campaign_or_error = xpcall(function()
    return Campaign.from_data(manifest, shard, options)
  end, debug.traceback)
  if not ok then return failure("invalid_state", tostring(campaign_or_error)) end
  return campaign_or_error
end

function CampaignPersistence.has_valid_campaign(directory, options)
  local campaign, error_data = CampaignPersistence.load(directory, options)
  if campaign then return true end
  return false, error_data
end

return CampaignPersistence
