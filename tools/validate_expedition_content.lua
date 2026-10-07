-- Headless validation/report for authored Expedition boards and encounters.
local Definitions = require("src.expedition.content_definitions")

local registry, failure = Definitions.load()
if not registry then
  io.stderr:write(string.format("EXPEDITION CONTENT INVALID: %s: %s\n", tostring(failure.file), tostring(failure.message)))
  os.exit(1)
end

local topology, archetype, compatible, excluded = {}, {}, 0, 0
for _, chamber in ipairs(registry.chambers) do for _, tag in ipairs(chamber.topology_tags) do topology[tag] = (topology[tag] or 0) + 1 end end
for _, encounter in ipairs(registry.encounters) do
  archetype[encounter.archetype] = (archetype[encounter.archetype] or 0) + 1
  for _, chamber in ipairs(registry.chambers) do
    if not chamber.boss_compatible then
      local okay = Definitions.compatibility(chamber, encounter)
      if okay then compatible = compatible + 1 else excluded = excluded + 1 end
    end
  end
end
local function line(label, values)
  local keys = {}; for key in pairs(values) do keys[#keys + 1] = key end; table.sort(keys)
  local pairs = {}; for _, key in ipairs(keys) do pairs[#pairs + 1] = key .. "=" .. values[key] end
  print(label .. ": " .. table.concat(pairs, ", "))
end
print("EXPEDITION CONTENT VALID")
print("chambers=" .. #registry.chambers .. " encounters=" .. #registry.encounters)
line("topologies", topology); line("archetypes", archetype)
print("compatibility_pairs=" .. compatible .. " excluded_pairs=" .. excluded)
