package.path = "./?.lua;./?/init.lua;" .. package.path

local Registry = require("src.content.registry")
local RoomRegistry = require("src.rooms.registry")
local RouteDefinitions = require("src.routes.definitions")

local ok, registry_or_error = xpcall(Registry.load, debug.traceback)
if not ok then
  io.stderr:write(registry_or_error, "\n")
  os.exit(1)
end

local registry = registry_or_error
local routes_ok, routes_or_error = xpcall(RouteDefinitions.load, debug.traceback)
if not routes_ok then
  io.stderr:write(routes_or_error, "\n")
  os.exit(1)
end
local routes = routes_or_error
local rooms, room_failure = RoomRegistry.load({ registry = registry })
if not rooms then
  io.stderr:write((room_failure.reason or room_failure.code or "Room corpus validation failed"), "\n")
  if room_failure.errors then
    for _, error in ipairs(room_failure.errors) do io.stderr:write("  ", error.code, ": ", error.message, "\n") end
  end
  os.exit(1)
end
local coverage = rooms:coverage()
io.write(string.format(
  "Content valid: %d abilities, %d materials, %d liquids, %d gases, %d world objects, %d hazards, %d components, %d topologies, %d actors, %d enemies, %d biomes, %d tiers, %d route profiles, %d dungeon room templates, %d connector patterns\n",
  (function() local count = 0 for _ in pairs(registry.abilities) do count = count + 1 end return count end)(),
  (function() local count = 0 for _ in pairs(registry.materials) do count = count + 1 end return count end)(),
  (function() local count = 0 for _ in pairs(registry.liquids) do count = count + 1 end return count end)(),
  (function() local count = 0 for _ in pairs(registry.gases) do count = count + 1 end return count end)(),
  (function() local count = 0 for _ in pairs(registry.world_objects) do count = count + 1 end return count end)(),
  (function() local count = 0 for _ in pairs(registry.hazards) do count = count + 1 end return count end)(),
  (function() local count = 0 for _ in pairs(registry.components) do count = count + 1 end return count end)(),
  (function() local count = 0 for _ in pairs(registry.topologies) do count = count + 1 end return count end)(),
  (function() local count = 0 for _ in pairs(registry.actors) do count = count + 1 end return count end)(),
  (function() local count = 0 for _ in pairs(registry.enemies) do count = count + 1 end return count end)(),
  #routes.biome_order,
  #routes.tier_order,
  #routes.profile_order,
  #rooms.order,
  (function() local count = 0 for _ in pairs(coverage.patterns) do count = count + 1 end return count end)()
))
local pattern_keys = {}
for pattern in pairs(coverage.patterns) do pattern_keys[#pattern_keys + 1] = pattern end
table.sort(pattern_keys)
local pattern_counts = {}
for _, pattern in ipairs(pattern_keys) do pattern_counts[#pattern_counts + 1] = pattern .. "=" .. coverage.patterns[pattern] end
io.write("Dungeon room connector coverage: " .. table.concat(pattern_counts, ", ") .. "\n")
