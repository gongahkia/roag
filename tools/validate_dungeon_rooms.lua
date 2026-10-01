-- Structural validation batch for the authored-room dungeon corpus.
package.path = "./?.lua;./?/init.lua;" .. package.path

local Batch = require("src.generation.batch_analysis")

local count = tonumber((arg or {})[1]) or 500
if count <= 0 or count % 1 ~= 0 then
  io.stderr:write("Usage: luajit tools/validate_dungeon_rooms.lua [positive-seed-count]\n")
  os.exit(2)
end

local report, failure = Batch.run({ stage = "dungeon", seed = 1, count = count, include_reports = false })
if not report then
  io.stderr:write("Dungeon room validation failed: ", failure.reason or failure.code or "unknown failure", "\n")
  os.exit(1)
end

io.write(string.format("Dungeon room batch: %d seeds, %d structural failures\n", report.summary.generated, report.summary.failures))
local keys = {}
for id in pairs(report.summary.template_usage) do keys[#keys + 1] = id end
table.sort(keys)
for _, id in ipairs(keys) do io.write(string.format("  %s = %d\n", id, report.summary.template_usage[id])) end
if report.summary.failures > 0 then
  for _, item in ipairs(report.failures) do io.write(string.format("  seed %s: %s\n", tostring(item.seed), tostring(item.code or (item.errors and item.errors[1] and item.errors[1].code)))) end
  os.exit(1)
end
