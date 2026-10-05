package.path = "./?.lua;./?/init.lua;" .. package.path

local Manifest = require("src.art.manifest")

local function print_issues(label, issues)
  for _, issue in ipairs(issues or {}) do io.write(label, " ", issue.path, ": ", issue.reason, "\n") end
end

local manifest, errors = Manifest.read("art/assets.json")
if not manifest then print_issues("ERROR", errors); os.exit(1) end
local valid, manifest_errors, warnings = Manifest.validate(manifest)
if not valid then print_issues("ERROR", manifest_errors); os.exit(1) end
local palette, palette_errors = Manifest.read(manifest.palette)
if not palette then print_issues("ERROR", palette_errors); os.exit(1) end
local palette_valid, palette_validation_errors = Manifest.validate_palette(palette)
if not palette_valid then print_issues("ERROR", palette_validation_errors); os.exit(1) end

local metadata_errors = {}
for _, asset in ipairs(manifest.assets or {}) do
  local metadata, read_errors = Manifest.read(asset.runtime_metadata)
  if not metadata then
    for _, issue in ipairs(read_errors) do metadata_errors[#metadata_errors + 1] = { path = asset.id .. " " .. issue.path, reason = issue.reason } end
  else
    local metadata_valid, errors_for_asset = Manifest.validate_metadata(asset, metadata)
    if not metadata_valid then
      for _, issue in ipairs(errors_for_asset) do metadata_errors[#metadata_errors + 1] = { path = asset.id .. " " .. issue.path, reason = issue.reason } end
    end
  end
end
if #metadata_errors > 0 then print_issues("ERROR", metadata_errors); os.exit(1) end

io.write("ART VALIDATION OK: ", #(manifest.assets or {}), " production asset(s), ", #(manifest.planned_assets or {}), " planned asset(s), ", #(palette.colors or {}), " palette colours.\n")
print_issues("BLOCKED", warnings)
