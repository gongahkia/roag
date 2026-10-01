-- Headless synthetic fallen-body recurrence validation.  This intentionally
-- does not read or write the user's persistent fallen archive.
package.path = "./?.lua;./?/init.lua;" .. package.path

local Analysis = require("src.simulation.fallen_recurrence_analysis")

local function usage()
  io.stderr:write("Usage: luajit tools/validate_fallen_recurrences.lua --seed <integer> --count <positive integer>\n")
end

local function parse(arguments)
  local result, index = {}, 1
  while arguments[index] do
    local flag, value = arguments[index], arguments[index + 1]
    if flag == "--help" or flag == "-h" then return nil, "help" end
    if (flag ~= "--seed" and flag ~= "--count") or not value then return nil, "Expected --seed <integer> --count <positive integer>" end
    result[flag:sub(3)] = value
    index = index + 2
  end
  if not result.seed or not result.count then return nil, "--seed and --count are required" end
  return result
end

local options, failure = parse(arg or {})
if not options then
  if failure ~= "help" then io.stderr:write("Error: " .. failure .. "\n") end
  usage(); os.exit(failure == "help" and 0 or 2)
end
local report, error_data = Analysis.batch(options)
if not report then io.stderr:write("Error: " .. error_data.reason .. "\n"); os.exit(2) end
io.write(string.format("ROAG fallen recurrence validation — %d generated, %d corpse, %d hostile\n",
  report.summary.generated, report.summary.corpse, report.summary.hostile))
io.write(string.format("Placement failures: %d  Critical placement failures: %d\n",
  report.summary.placement_failures, report.summary.critical_failures))
for _, item in ipairs(report.failures) do io.write("  seed " .. item.seed .. "  " .. item.code .. "\n") end
if report.summary.placement_failures > 0 or report.summary.critical_failures > 0 then os.exit(1) end
