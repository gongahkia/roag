-- Presentation preferences are intentionally separate from active-run,
-- account, and fallen-character persistence. A damaged choice cannot affect
-- deterministic game state or make an active run unloadable.
local Json = require("src.persistence.json")
local ArtPacks = require("src.rendering.art_packs")

local ArtPackSettings = {
  FORMAT = "roag.art_pack_settings",
  VERSION = 1,
}

local function failure(code, reason)
  return nil, { code = code, reason = reason }
end

function ArtPackSettings.new()
  return { art_pack_id = ArtPacks.DEFAULT_ID }
end

function ArtPackSettings.copy(settings)
  return { art_pack_id = settings.art_pack_id }
end

function ArtPackSettings.validate(settings)
  assert(type(settings) == "table", "Art-pack settings must be an object")
  assert(type(settings.art_pack_id) == "string" and ArtPacks.has(settings.art_pack_id), "Art-pack settings reference an unknown art pack")
  return true
end

function ArtPackSettings.encode(settings)
  local ok, reason = pcall(ArtPackSettings.validate, settings)
  if not ok then return failure("invalid_state", tostring(reason)) end
  local payload, encode_reason = Json.encode({ format = ArtPackSettings.FORMAT, version = ArtPackSettings.VERSION, settings = ArtPackSettings.copy(settings) })
  if not payload then return failure("encode_failed", tostring(encode_reason)) end
  return payload
end

function ArtPackSettings.decode(payload)
  local envelope, reason = Json.decode(payload)
  if not envelope then return failure("invalid_json", tostring(reason)) end
  if type(envelope) ~= "table" then return failure("invalid_state", "Art-pack settings envelope must be an object") end
  if envelope.format ~= ArtPackSettings.FORMAT then return failure("unsupported_format", "Save format is not roag.art_pack_settings") end
  if envelope.version ~= ArtPackSettings.VERSION then return failure("unsupported_version", "Art-pack settings version is not supported") end
  local ok, validation = pcall(ArtPackSettings.validate, envelope.settings)
  if not ok then return failure("invalid_state", tostring(validation)) end
  return ArtPackSettings.copy(envelope.settings)
end

function ArtPackSettings.load(store)
  local payload, error_data = store:read()
  if not payload and error_data and error_data.code == "missing_file" then return ArtPackSettings.new(), { fresh = true } end
  if not payload then return nil, error_data end
  return ArtPackSettings.decode(payload)
end

function ArtPackSettings.save(settings, store)
  local payload, error_data = ArtPackSettings.encode(settings)
  if not payload then return nil, error_data end
  local written, write_error = store:write(payload)
  if not written then return failure("write_failed", tostring(write_error and write_error.reason or "Could not save art-pack preference")) end
  return true
end

return ArtPackSettings
