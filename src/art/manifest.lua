-- Versioned, data-only art-manifest validation shared by local art tools and
-- lightweight runtime metadata tests. Runtime never opens .aseprite sources.
local Json = require("src.persistence.json")

local Manifest = {}

local function issue(list, path, reason)
  list[#list + 1] = { path = path, reason = reason }
end

local function read_file(path)
  local file, error_message = io.open(path, "rb")
  if not file then return nil, error_message end
  local contents = file:read("*a")
  file:close()
  return contents
end

local function is_integer(value)
  return type(value) == "number" and value % 1 == 0
end

local function required_string(value, errors, path)
  if type(value) ~= "string" or value == "" then issue(errors, path, "must be a non-empty string") end
end

local function check_tags(tags, errors, path)
  if type(tags) ~= "table" or #tags == 0 then
    issue(errors, path, "must be a non-empty array")
    return
  end
  local seen = {}
  for index, tag in ipairs(tags) do
    required_string(tag, errors, path .. "[" .. index .. "]")
    if seen[tag] then issue(errors, path .. "[" .. index .. "]", "duplicates tag '" .. tostring(tag) .. "'") end
    seen[tag] = true
  end
end

local function check_pivot(asset, errors, path)
  local pivot = asset.pivot
  if type(pivot) ~= "table" then
    issue(errors, path, "must be an object")
    return
  end
  if not is_integer(pivot.x) or not is_integer(pivot.y) then
    issue(errors, path, "coordinates must be integers")
  elseif pivot.x < 0 or pivot.y < 0 or pivot.x >= asset.native_width or pivot.y >= asset.native_height then
    issue(errors, path, "must lie inside the native canvas")
  end
end

function Manifest.read(path)
  local contents, error_message = read_file(path)
  if not contents then return nil, { { path = path, reason = error_message } } end
  local value, decode_error = Json.decode(contents)
  if not value then return nil, { { path = path, reason = decode_error } } end
  return value
end

function Manifest.validate(definition, options)
  options = options or {}
  local exists = options.exists or function(path)
    local file = io.open(path, "rb")
    if not file then return false end
    file:close()
    return true
  end
  local errors, warnings = {}, {}
  if type(definition) ~= "table" then return nil, { { path = "manifest", reason = "must be an object" } }, warnings end
  if definition.schema_version ~= 1 then issue(errors, "schema_version", "must equal supported version 1") end
  if type(definition.assets) ~= "table" then issue(errors, "assets", "must be an array") end
  if type(definition.planned_assets) ~= "table" then issue(errors, "planned_assets", "must be an array") end
  local ids = {}
  local function check_asset(asset, index, planned)
    local prefix = (planned and "planned_assets" or "assets") .. "[" .. index .. "]"
    if type(asset) ~= "table" then issue(errors, prefix, "must be an object"); return end
    required_string(asset.id, errors, prefix .. ".id")
    if type(asset.id) == "string" and asset.id ~= "" then
      if ids[asset.id] then issue(errors, prefix .. ".id", "duplicates asset ID '" .. asset.id .. "'") end
      ids[asset.id] = true
    end
    required_string(asset.kind, errors, prefix .. ".kind")
    required_string(asset.source, errors, prefix .. ".source")
    if not is_integer(asset.native_width) or asset.native_width < 1 then issue(errors, prefix .. ".native_width", "must be a positive integer") end
    if not is_integer(asset.native_height) or asset.native_height < 1 then issue(errors, prefix .. ".native_height", "must be a positive integer") end
    if is_integer(asset.native_width) and is_integer(asset.native_height) then check_pivot(asset, errors, prefix .. ".pivot") end
    check_tags(asset.required_tags, errors, prefix .. ".required_tags")
    if planned then
      required_string(asset.status, errors, prefix .. ".status")
      required_string(asset.block_reason, errors, prefix .. ".block_reason")
      if asset.source and not exists(asset.source) then
        warnings[#warnings + 1] = { path = prefix .. ".source", reason = "planned source is not present yet" }
      end
      return
    end
    required_string(asset.runtime_sheet, errors, prefix .. ".runtime_sheet")
    required_string(asset.runtime_metadata, errors, prefix .. ".runtime_metadata")
    if asset.source and not exists(asset.source) then issue(errors, prefix .. ".source", "source file is missing") end
    if not options.allow_missing_runtime then
      if asset.runtime_sheet and not exists(asset.runtime_sheet) then issue(errors, prefix .. ".runtime_sheet", "runtime sheet is missing") end
      if asset.runtime_metadata and not exists(asset.runtime_metadata) then issue(errors, prefix .. ".runtime_metadata", "runtime metadata is missing") end
    end
  end
  for index, asset in ipairs(definition.assets or {}) do check_asset(asset, index, false) end
  for index, asset in ipairs(definition.planned_assets or {}) do check_asset(asset, index, true) end
  if #errors > 0 then return nil, errors, warnings end
  return true, errors, warnings
end

function Manifest.validate_palette(palette)
  local errors, seen = {}, {}
  if type(palette) ~= "table" or palette.schema_version ~= 1 then
    return nil, { { path = "palette.schema_version", reason = "must equal supported version 1" } }
  end
  required_string(palette.id, errors, "palette.id")
  if type(palette.colors) ~= "table" or #palette.colors < 1 then issue(errors, "palette.colors", "must be a non-empty array") end
  for index, color in ipairs(palette.colors or {}) do
    if type(color) ~= "string" or not color:match("^#%x%x%x%x%x%x$") then
      issue(errors, "palette.colors[" .. index .. "]", "must be #RRGGBB")
    elseif seen[color:upper()] then
      issue(errors, "palette.colors[" .. index .. "]", "duplicates colour '" .. color .. "'")
    end
    seen[tostring(color):upper()] = true
  end
  if #errors > 0 then return nil, errors end
  return true, errors
end

function Manifest.validate_metadata(asset, metadata)
  local errors = {}
  if type(metadata) ~= "table" then return nil, { { path = "metadata", reason = "must be an object" } } end
  local frames = metadata.frames
  if type(frames) ~= "table" or #frames == 0 then issue(errors, "metadata.frames", "must be a non-empty array") end
  for index, frame in ipairs(frames or {}) do
    local rect = frame.frame
    local prefix = "metadata.frames[" .. index .. "]"
    if type(rect) ~= "table" then
      issue(errors, prefix .. ".frame", "must be a rectangle")
    elseif not is_integer(rect.x) or not is_integer(rect.y) or not is_integer(rect.w) or not is_integer(rect.h)
      or rect.x < 0 or rect.y < 0 or rect.w ~= asset.native_width or rect.h ~= asset.native_height then
      issue(errors, prefix .. ".frame", "must be a non-negative native-size rectangle")
    end
    if type(frame.duration) ~= "number" or frame.duration <= 0 then issue(errors, prefix .. ".duration", "must be greater than zero") end
  end
  local tags = metadata.meta and metadata.meta.frameTags
  if type(tags) ~= "table" then
    issue(errors, "metadata.meta.frameTags", "must be an array")
  else
    local seen, present = {}, {}
    for index, tag in ipairs(tags) do
      local prefix = "metadata.meta.frameTags[" .. index .. "]"
      if type(tag) ~= "table" or type(tag.name) ~= "string" then
        issue(errors, prefix, "must have a name")
      else
        if seen[tag.name] then issue(errors, prefix .. ".name", "duplicates tag '" .. tag.name .. "'") end
        seen[tag.name], present[tag.name] = true, true
      end
      if type(tag) ~= "table" or not is_integer(tag.from) or not is_integer(tag.to) or tag.from < 0 or tag.to < tag.from or tag.to >= #(frames or {}) then
        issue(errors, prefix, "must reference an in-range inclusive frame interval")
      end
    end
    for _, required in ipairs(asset.required_tags or {}) do
      if not present[required] then issue(errors, "metadata.meta.frameTags", "required tag '" .. required .. "' is missing") end
    end
  end
  if #errors > 0 then return nil, errors end
  return true, errors
end

return Manifest
