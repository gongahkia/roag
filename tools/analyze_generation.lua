-- Headless batch generation diagnostics.  It deliberately requires no LÖVE
-- modules, assets, audio, or active-run storage.
package.path = "./?.lua;./?/init.lua;" .. package.path

local Batch = require("src.generation.batch_analysis")
local Json = require("src.persistence.json")

local function usage()
  io.stderr:write([[Usage: luajit tools/analyze_generation.lua (--stage <forest|cave|dungeon|index> | --biome <semantic-id|terrain> --tier <tier>) --seed <integer> --count <positive integer> [--json <path>]

Generates isolated initial floors and reports structural facts. Failed seeds
can be pasted into: love . --generation-inspector
]])
end

local function parse(arguments)
  local result, index = {}, 1
  while arguments[index] do
    local flag = arguments[index]
    if flag == "--help" or flag == "-h" then return nil, "help" end
    if flag ~= "--stage" and flag ~= "--biome" and flag ~= "--tier" and flag ~= "--seed" and flag ~= "--count" and flag ~= "--json" then
      return nil, "Unknown argument '" .. tostring(flag) .. "'"
    end
    local value = arguments[index + 1]
    if not value then return nil, "Missing value for " .. flag end
    result[flag:sub(3)] = value
    index = index + 2
  end
  if (not result.stage and not (result.biome and result.tier)) or not result.seed or not result.count then
    return nil, "--stage (or --biome with --tier), --seed, and --count are required"
  end
  return result
end

local function print_range(label, range)
  io.write(string.format("  %-14s avg %8.2f  min %d (seed %s)  max %d (seed %s)\n",
    label, range.average, range.min, tostring(range.min_seed), range.max, tostring(range.max_seed)))
end

local function print_counts(label, values)
  local keys = {}
  for key in pairs(values) do keys[#keys + 1] = key end
  table.sort(keys)
  if #keys == 0 then return end
  local parts = {}
  for _, key in ipairs(keys) do parts[#parts + 1] = key .. "=" .. values[key] end
  io.write("  " .. label .. ": " .. table.concat(parts, ", ") .. "\n")
end

local options, error_message = parse(arg or {})
if not options then
  if error_message ~= "help" then io.stderr:write("Error: " .. error_message .. "\n") end
  usage()
  os.exit(error_message == "help" and 0 or 2)
end

local report, failure = Batch.run(options)
if not report then
  io.stderr:write("Error: " .. (failure.reason or failure.code or "generation analysis failed") .. "\n")
  os.exit(2)
end

local summary = report.summary
io.write(string.format("ROAG generation analysis — %s tier %s (%s), seeds %d..%d\n",
  tostring(report.options.biome_id or report.options.stage), tostring(report.options.tier_id or "legacy"), tostring(report.options.terrain), report.options.seed,
  report.options.seed + report.options.count - 1))
io.write(string.format("Generated: %d  Structural failures: %d\n", summary.generated, summary.failures))
io.write("Metrics:\n")
for _, name in ipairs({ "passable_cells", "enemies", "hazards", "liquid_volume", "gas_volume", "circuits", "powered_circuits", "room_count" }) do
  print_range(name, summary.statistics[name])
end
print_counts("materials", summary.material_counts)
print_counts("objects", summary.object_counts)
print_counts("room templates", summary.template_usage)
print_counts("room rotations", summary.rotation_counts)
print_counts("connector patterns", summary.connector_pattern_counts)
io.write(string.format("Outliers: smallest-area=%s, largest-area=%s, most-enemies=%s, most-hazards=%s, most-liquid=%s, most-gas=%s\n",
  tostring(summary.outliers.smallest_passable_area.seed), tostring(summary.outliers.largest_passable_area.seed),
  tostring(summary.outliers.most_enemies.seed), tostring(summary.outliers.most_hazards.seed),
  tostring(summary.outliers.most_liquid.seed), tostring(summary.outliers.most_gas.seed)))
if #report.failures > 0 then
  io.write("Failure seeds:\n")
  for _, item in ipairs(report.failures) do
    local reason = item.code or (item.errors and item.errors[1] and item.errors[1].code) or "invalid_generation"
    io.write(string.format("  %s  %s\n", tostring(item.seed), reason))
  end
end

if options.json then
  local encoded, encode_error = Json.encode(report)
  if not encoded then
    io.stderr:write("Error: could not encode JSON report: " .. tostring(encode_error) .. "\n")
    os.exit(3)
  end
  local handle, open_error = io.open(options.json, "wb")
  if not handle then
    io.stderr:write("Error: could not write JSON report: " .. tostring(open_error) .. "\n")
    os.exit(3)
  end
  handle:write(encoded)
  handle:close()
  io.write("JSON report: " .. options.json .. "\n")
end
