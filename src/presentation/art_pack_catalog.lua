-- Human-readable catalog data for the sibling authoring project. The runtime
-- ArtPacks module remains authoritative for sprite-sheet details; this module
-- verifies that exported/editor-facing identifiers stay in lockstep.
local Json = require("src.persistence.json")
local ArtPacks = require("src.rendering.art_packs")

local Catalog = {}
Catalog.FORMAT = "roag.presentation_art_pack_catalog"
Catalog.VERSION = 1
Catalog.DEFAULT_PATH = "content/presentation/art_packs.json"

local function failure(code, reason)
  return nil, { code = code, reason = reason }
end

local function read_file(path)
  if love and love.filesystem then return love.filesystem.read(path) end
  local file, reason = io.open(path, "rb")
  if not file then return nil, reason end
  local payload = file:read("*a")
  file:close()
  return payload
end

function Catalog.validate(data)
  if type(data) ~= "table" or data.format ~= Catalog.FORMAT or data.version ~= Catalog.VERSION or type(data.art_packs) ~= "table" then
    return failure("invalid_presentation_art_pack_catalog", "Expected " .. Catalog.FORMAT .. " v" .. Catalog.VERSION)
  end
  local seen, count = {}, 0
  for _, pack in ipairs(data.art_packs) do
    if type(pack) ~= "table" or type(pack.id) ~= "string" or seen[pack.id] or not ArtPacks.has(pack.id)
      or type(pack.label) ~= "string" or pack.label == "" or type(pack.license) ~= "string" or pack.license == "" or type(pack.credit) ~= "string" or pack.credit == "" then
      return failure("invalid_presentation_art_pack_catalog", "Catalog pack metadata is missing or unknown")
    end
    seen[pack.id], count = true, count + 1
  end
  for _, definition in ipairs(ArtPacks.list()) do
    if not seen[definition.id] then return failure("incomplete_presentation_art_pack_catalog", "Catalog omits " .. definition.id) end
  end
  if count ~= #ArtPacks.list() then return failure("incomplete_presentation_art_pack_catalog", "Catalog contains an unexpected pack count") end
  return data
end

function Catalog.load(options)
  options = options or {}
  local payload, reason = options.payload, nil
  if not payload then payload, reason = (options.read or read_file)(options.path or Catalog.DEFAULT_PATH) end
  if not payload then return failure("presentation_art_pack_catalog_read_failed", tostring(reason)) end
  local data, decode_reason = Json.decode(payload)
  if not data then return failure("presentation_art_pack_catalog_json", tostring(decode_reason)) end
  return Catalog.validate(data)
end

return Catalog
