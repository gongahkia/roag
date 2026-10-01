package.path = "./?.lua;./?/init.lua;" .. package.path

local Registry = require("src.content.registry")
local RoomRegistry = require("src.rooms.registry")
local Corpora = require("src.rooms.corpora")
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
local pools_ok, pools_error = pcall(function() return registry:validate_encounter_pools(routes) end)
if not pools_ok then
  io.stderr:write(tostring(pools_error), "\n")
  os.exit(1)
end
local room_counts, room_coverages = {}, {}
for _, config in ipairs(Corpora.list()) do
  local rooms, room_failure = RoomRegistry.load({ registry = registry, config = config })
  if not rooms then
    io.stderr:write((room_failure.reason or room_failure.code or "Room corpus validation failed"), "\n")
    if room_failure.errors then
      for _, error in ipairs(room_failure.errors) do io.stderr:write("  ", error.code, ": ", error.message, "\n") end
    end
    os.exit(1)
  end
  room_counts[config.BIOME], room_coverages[config.BIOME] = #rooms.order, rooms:coverage()
end
io.write(string.format(
  "Content valid: %d abilities, %d materials, %d liquids, %d gases, %d world objects, %d hazards, %d components, %d services, %d charms, %d boons, %d curses, %d research nodes, %d topologies, %d actors, %d enemies, %d encounter pools, %d biomes, %d tiers, %d route profiles, %d dungeon room templates, %d reactor room templates\n",
  (function() local count = 0 for _ in pairs(registry.abilities) do count = count + 1 end return count end)(),
  (function() local count = 0 for _ in pairs(registry.materials) do count = count + 1 end return count end)(),
  (function() local count = 0 for _ in pairs(registry.liquids) do count = count + 1 end return count end)(),
  (function() local count = 0 for _ in pairs(registry.gases) do count = count + 1 end return count end)(),
  (function() local count = 0 for _ in pairs(registry.world_objects) do count = count + 1 end return count end)(),
  (function() local count = 0 for _ in pairs(registry.hazards) do count = count + 1 end return count end)(),
  (function() local count = 0 for _ in pairs(registry.components) do count = count + 1 end return count end)(),
  (function() local count = 0 for _ in pairs(registry.services) do count = count + 1 end return count end)(),
  (function() local count = 0 for _ in pairs(registry.charms) do count = count + 1 end return count end)(),
  (function() local count = 0 for _ in pairs(registry.boons) do count = count + 1 end return count end)(),
  (function() local count = 0 for _ in pairs(registry.curses) do count = count + 1 end return count end)(),
  (function() local count = 0 for _ in pairs(registry.research) do count = count + 1 end return count end)(),
  (function() local count = 0 for _ in pairs(registry.topologies) do count = count + 1 end return count end)(),
  (function() local count = 0 for _ in pairs(registry.actors) do count = count + 1 end return count end)(),
  (function() local count = 0 for _ in pairs(registry.enemies) do count = count + 1 end return count end)(),
  (function() local count = 0 for _ in pairs(registry.encounter_pools) do count = count + 1 end return count end)(),
  #routes.biome_order,
  #routes.tier_order,
  #routes.profile_order,
  room_counts.dungeon or 0,
  room_counts.reactor or 0
))
for _, config in ipairs(Corpora.list()) do
  local pattern_keys, coverage = {}, room_coverages[config.BIOME]
  for pattern in pairs(coverage.patterns) do pattern_keys[#pattern_keys + 1] = pattern end
  table.sort(pattern_keys)
  local pattern_counts = {}
  for _, pattern in ipairs(pattern_keys) do pattern_counts[#pattern_counts + 1] = pattern .. "=" .. coverage.patterns[pattern] end
  io.write(config.BIOME:gsub("^%l", string.upper) .. " room connector coverage: " .. table.concat(pattern_counts, ", ") .. "\n")
end
