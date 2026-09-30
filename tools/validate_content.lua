package.path = "./?.lua;./?/init.lua;" .. package.path

local Registry = require("src.content.registry")

local ok, registry_or_error = xpcall(Registry.load, debug.traceback)
if not ok then
  io.stderr:write(registry_or_error, "\n")
  os.exit(1)
end

local registry = registry_or_error
io.write(string.format(
  "Content valid: %d abilities, %d materials, %d liquids, %d world objects, %d hazards, %d components, %d topologies, %d actors, %d enemies\n",
  (function() local count = 0 for _ in pairs(registry.abilities) do count = count + 1 end return count end)(),
  (function() local count = 0 for _ in pairs(registry.materials) do count = count + 1 end return count end)(),
  (function() local count = 0 for _ in pairs(registry.liquids) do count = count + 1 end return count end)(),
  (function() local count = 0 for _ in pairs(registry.world_objects) do count = count + 1 end return count end)(),
  (function() local count = 0 for _ in pairs(registry.hazards) do count = count + 1 end return count end)(),
  (function() local count = 0 for _ in pairs(registry.components) do count = count + 1 end return count end)(),
  (function() local count = 0 for _ in pairs(registry.topologies) do count = count + 1 end return count end)(),
  (function() local count = 0 for _ in pairs(registry.actors) do count = count + 1 end return count end)(),
  (function() local count = 0 for _ in pairs(registry.enemies) do count = count + 1 end return count end)()
))
