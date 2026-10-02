-- Source-controlled presentation selection.  The companion authoring project
-- writes this small data file; no active run, profile, or fallen archive is
-- consulted.  A legacy personal preference remains readable only as an
-- explicit App API compatibility concern, never as the normal title UI.
local Json = require("src.persistence.json")
local ArtPacks = require("src.rendering.art_packs")

local Config = {}
Config.FORMAT = "roag.presentation_art_pack"
Config.VERSION = 1
Config.DEFAULT_PATH = "content/presentation/art_pack.json"

local function failure(code, reason)
  return nil, { code = code, reason = reason }
end

function Config.default()
  return { art_pack_id = ArtPacks.DEFAULT_ID }
end

function Config.validate(data)
  if type(data) ~= "table" then return failure("invalid_presentation_art_pack", "Art pack config must be an object") end
  if data.format ~= Config.FORMAT or data.version ~= Config.VERSION then
    return failure("invalid_presentation_art_pack_format", "Expected " .. Config.FORMAT .. " v" .. Config.VERSION)
  end
  if not ArtPacks.has(data.art_pack_id) then return failure("unknown_presentation_art_pack", "Unknown art pack") end
  return { art_pack_id = data.art_pack_id }
end

function Config.decode(payload)
  local data, reason = Json.decode(payload)
  if not data then return failure("invalid_presentation_art_pack_json", tostring(reason)) end
  return Config.validate(data)
end

function Config.encode(config)
  local valid, reason = Config.validate({ format = Config.FORMAT, version = Config.VERSION, art_pack_id = config.art_pack_id })
  if not valid then return nil, reason end
  return Json.encode({ format = Config.FORMAT, version = Config.VERSION, art_pack_id = valid.art_pack_id })
end

local function read_file(path)
  if love and love.filesystem then return love.filesystem.read(path) end
  local file, reason = io.open(path, "rb")
  if not file then return nil, reason end
  local payload = file:read("*a")
  file:close()
  return payload
end

function Config.load(options)
  options = options or {}
  local payload, reason = options.payload, nil
  if not payload then payload, reason = (options.read or read_file)(options.path or Config.DEFAULT_PATH) end
  if not payload then
    if options.strict then return failure("presentation_art_pack_read_failed", tostring(reason)) end
    return Config.default(), { fresh = true, reason = reason }
  end
  local config, failure_data = Config.decode(payload)
  if not config then
    if options.strict then return nil, failure_data end
    return Config.default(), failure_data
  end
  return config
end

return Config
