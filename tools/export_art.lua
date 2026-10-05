-- Deterministic Aseprite CLI exporter. This is intentionally a host-tooling
-- command because a licensed Aseprite executable is never part of roag.love.
package.path = "./?.lua;./?/init.lua;" .. package.path

local Json = require("src.persistence.json")
local Manifest = require("src.art.manifest")

local function quote(value)
  return "'" .. tostring(value):gsub("'", "'\\''") .. "'"
end

local function read_json(path)
  local value, errors = Manifest.read(path)
  if not value then error(path .. ": " .. errors[1].reason) end
  return value
end

local function write_json(path, value)
  local directory = path:match("^(.*)/[^/]+$")
  if directory then assert(os.execute("mkdir -p " .. quote(directory))) end
  local file = assert(io.open(path, "wb"))
  assert(file:write(assert(Json.encode(value)) .. "\n"))
  file:close()
end

local function run(command)
  local result = os.execute(command)
  if result ~= true and result ~= 0 then error("Aseprite command failed: " .. command) end
end

local manifest = read_json("art/assets.json")
local valid, errors = Manifest.validate(manifest, { allow_missing_runtime = true })
if not valid then
  for _, issue in ipairs(errors) do io.stderr:write(issue.path, ": ", issue.reason, "\n") end
  os.exit(1)
end

if #(manifest.assets or {}) == 0 then
  io.write("No production art assets are declared. Planned assets remain intentionally unexported.\n")
  os.exit(0)
end

local aseprite = os.getenv("ASEPRITE_BIN")
if not aseprite or aseprite == "" then
  io.stderr:write("ASEPRITE_BIN is required when production art assets are declared.\n")
  os.exit(2)
end

local palette = read_json(manifest.palette)
local palette_valid, palette_errors = Manifest.validate_palette(palette)
if not palette_valid then
  for _, issue in ipairs(palette_errors) do io.stderr:write(issue.path, ": ", issue.reason, "\n") end
  os.exit(1)
end

local runtime = { schema_version = 1, assets = {}, character_bindings = {} }
for _, asset in ipairs(manifest.assets) do
  local tags = table.concat(asset.required_tags, ",")
  local colours = table.concat(palette.colors, ",")
  run(table.concat({
    quote(aseprite), "-b", quote(asset.source), "--script", quote("tools/aseprite_validate.lua"),
    "--script-param", quote("width=" .. asset.native_width),
    "--script-param", quote("height=" .. asset.native_height),
    "--script-param", quote("required_tags=" .. tags),
    "--script-param", quote("palette=" .. colours),
  }, " "))
  run(table.concat({
    quote(aseprite), "-b", quote(asset.source),
    "--sheet", quote(asset.runtime_sheet),
    "--data", quote(asset.runtime_metadata),
    "--format", "json-array", "--sheet-type", "horizontal", "--list-tags",
  }, " "))
  local metadata = read_json(asset.runtime_metadata)
  local metadata_valid, metadata_errors = Manifest.validate_metadata(asset, metadata)
  if not metadata_valid then
    for _, issue in ipairs(metadata_errors) do io.stderr:write(asset.id, " ", issue.path, ": ", issue.reason, "\n") end
    os.exit(1)
  end
  -- Json.encode sorts object keys, stripping any non-deterministic metadata
  -- ordering without adding absolute source-machine paths.
  write_json(asset.runtime_metadata, metadata)
  runtime.assets[#runtime.assets + 1] = {
    id = asset.id, kind = asset.kind, runtime_sheet = asset.runtime_sheet,
    runtime_metadata = asset.runtime_metadata, native_width = asset.native_width,
    native_height = asset.native_height, pivot = asset.pivot,
  }
  if asset.runtime_binding and asset.runtime_binding.character_id then
    runtime.character_bindings[asset.runtime_binding.character_id] = asset.id
  end
end
table.sort(runtime.assets, function(a, b) return a.id < b.id end)
write_json("assets/sprites/manifest.json", runtime)
io.write("Exported ", #runtime.assets, " production art asset(s).\n")
