-- Reproducible configuration/economy diagnostic. It is deliberately not an
-- auto-player and reports its simplified scenario assumptions in the output.
package.path = "./?.lua;./?/init.lua;" .. package.path

local Report = require("src.balance.report")
local Json = require("src.persistence.json")

local options, index = {}, 1
while (arg or {})[index] do
  local flag, value = arg[index], arg[index + 1]
  if flag == "--json" and value then options.json = value; index = index + 2
  elseif flag == "--help" or flag == "-h" then
    io.write("Usage: luajit tools/analyze_balance.lua [--json <path>]\n")
    os.exit(0)
  else
    io.stderr:write("Usage: luajit tools/analyze_balance.lua [--json <path>]\n")
    os.exit(2)
  end
end

local report = Report.build()
io.write("ROAG balance report — configuration facts plus explicit non-tactical scenarios\n")
io.write(string.format("Run: %d normal floors, %d milestones, %d terminal boss (%d bosses total)\n",
  report.run_structure.normal_floors, report.run_structure.milestone_bosses, report.run_structure.terminal_bosses, report.run_structure.total_bosses))
io.write("Tier pacing:\n")
for _, tier in ipairs(report.tiers) do
  io.write(string.format("  tier %d: targets=%d enemies=%d objective=%d initial-reward=%d SCRAP conservative/moderate/aggressive=%d/%d/%d\n",
    tier.number, tier.settings.targets, tier.settings.enemies, tier.settings.objective_required, tier.initial_reward_eligible,
    tier.conservative_scrap, tier.moderate_scrap, tier.aggressive_scrap))
end
io.write("Economy scenarios (before sales and service spending):\n")
for _, name in ipairs({ "fresh_conservative", "fresh_moderate", "fresh_aggressive", "research_rich_moderate" }) do
  local scenario = report.economy_scenarios[name]
  io.write(string.format("  %s: start=%s expected SCRAP=%s persistent DATA=%s", name, scenario.starting_scrap,
    scenario.expected_scrap_before_sales, scenario.persistent_data_before_discoveries))
  if scenario.expected_first_discovery_data then io.write(string.format(" + expected first-discovery DATA %.2f", scenario.expected_first_discovery_data)) end
  if scenario.expected_known_discovery_scrap then io.write(string.format(" + expected repeat-discovery SCRAP %.2f", scenario.expected_known_discovery_scrap)) end
  io.write("\n")
end
io.write(string.format("Research: %d nodes, %d total DATA cost. Discovery %.0f%%; reinforcement %.0f%%.\n",
  report.research.node_count, report.research.total_cost, report.tuning.discoveries.placement_percent, report.tuning.reinforcements.placement_percent))
io.write(string.format("Components: %d, integrity %d..%d, %d with usage wear.\n", report.components.count,
  report.components.integrity_range.min, report.components.integrity_range.max, report.components.worn_component_count))
io.write("Assumptions:\n")
for _, key in ipairs({ "description", "conservative", "moderate", "aggressive", "discovery_expectation" }) do
  io.write("  " .. report.assumptions[key] .. "\n")
end
if options.json then
  local encoded, reason = Json.encode(report)
  if not encoded then io.stderr:write("Could not encode report: " .. tostring(reason) .. "\n"); os.exit(3) end
  local handle, failure = io.open(options.json, "wb")
  if not handle then io.stderr:write("Could not write report: " .. tostring(failure) .. "\n"); os.exit(3) end
  handle:write(encoded); handle:close()
  io.write("JSON report: " .. options.json .. "\n")
end
