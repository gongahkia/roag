-- Headless batch structural validation for deterministic route graphs.
package.path = "./?.lua;./?/init.lua;" .. package.path

local Definitions = require("src.routes.definitions")
local Analysis = require("src.routes.analysis")
local Json = require("src.persistence.json")

local function usage()
  io.stderr:write("Usage: luajit tools/analyze_routes.lua --seed <integer> --count <positive integer> [--profile <id>] [--unlock <id>] [--json <path>]\n")
end

local function parse(arguments)
  local result, index = {}, 1
  while arguments[index] do
    local flag = arguments[index]
    if flag == "--help" or flag == "-h" then return nil, "help" end
    if flag ~= "--seed" and flag ~= "--count" and flag ~= "--profile" and flag ~= "--unlock" and flag ~= "--json" then return nil, "Unknown argument '" .. tostring(flag) .. "'" end
    local value = arguments[index + 1]
    if not value then return nil, "Missing value for " .. flag end
    if flag == "--unlock" then
      result.unlock_ids = result.unlock_ids or {}
      result.unlock_ids[#result.unlock_ids + 1] = value
    else
      result[flag:sub(3)] = value
    end
    index = index + 2
  end
  if not result.seed or not result.count then return nil, "--seed and --count are required" end
  return result
end

local options, reason = parse(arg or {})
if not options then
  if reason ~= "help" then io.stderr:write("Error: " .. reason .. "\n") end
  usage()
  os.exit(reason == "help" and 0 or 2)
end
local report, failure = Analysis.batch({ definitions = Definitions.load(), seed = options.seed, count = options.count, profile_id = options.profile, unlock_ids = options.unlock_ids })
if not report then io.stderr:write("Error: " .. (failure.reason or failure.code) .. "\n"); os.exit(2) end
io.write(string.format("ROAG route analysis — %s, seeds %s..%d\n", report.options.profile_id, report.options.seed, tonumber(report.options.seed) + tonumber(report.options.count) - 1))
io.write(string.format("Generated: %d  Structural failures: %d  Avg branches: %.2f  Avg convergence: %.2f\n",
  report.summary.generated, report.summary.failures, report.summary.average_branch_count, report.summary.average_convergence_count))
local depths = {}
for depth in pairs(report.summary.biome_by_depth) do depths[#depths + 1] = depth end
table.sort(depths)
for _, depth in ipairs(depths) do
  local parts, values = {}, report.summary.biome_by_depth[depth]
  local ids = {}; for id in pairs(values) do ids[#ids + 1] = id end; table.sort(ids)
  for _, id in ipairs(ids) do parts[#parts + 1] = id .. "=" .. values[id] end
  io.write("  depth " .. depth .. ": " .. table.concat(parts, ", ") .. "\n")
  local service_parts, services = {}, report.summary.services_by_depth[depth] or {}
  local service_ids = {}; for id in pairs(services) do service_ids[#service_ids + 1] = id end; table.sort(service_ids)
  for _, id in ipairs(service_ids) do service_parts[#service_parts + 1] = id .. "=" .. services[id] end
  if #service_parts > 0 then io.write("    services: " .. table.concat(service_parts, ", ") .. "\n") end
end
local boss_depths = {}
for depth in pairs(report.summary.boss_by_depth or {}) do boss_depths[#boss_depths + 1] = depth end
table.sort(boss_depths, function(left, right) return tonumber(left) < tonumber(right) end)
for _, depth in ipairs(boss_depths) do
  local parts, values = {}, report.summary.boss_by_depth[depth]
  local ids = {}; for id in pairs(values) do ids[#ids + 1] = id end; table.sort(ids)
  for _, id in ipairs(ids) do parts[#parts + 1] = id .. "=" .. values[id] end
  io.write("  boss depth " .. depth .. ": " .. table.concat(parts, ", ") .. "\n")
end
if #report.failures > 0 then
  io.write("Failure seeds:\n")
  for _, item in ipairs(report.failures) do io.write("  " .. item.seed .. "  " .. (item.code or "invalid_route") .. "\n") end
end
if options.json then
  local text, encode_error = Json.encode(report)
  if not text then io.stderr:write("Error: " .. tostring(encode_error) .. "\n"); os.exit(3) end
  local handle, open_error = io.open(options.json, "wb")
  if not handle then io.stderr:write("Error: " .. tostring(open_error) .. "\n"); os.exit(3) end
  handle:write(text); handle:close()
  io.write("JSON report: " .. options.json .. "\n")
end
