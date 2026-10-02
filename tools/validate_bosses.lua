-- Headless physical-boss report. Registry construction performs the actual
-- schema/provider validation; this tool makes the resulting facts inspectable.
local Registry = require("src.content.registry")
local Definitions = require("src.routes.definitions")

local registry, routes = Registry.load(), Definitions.load()
registry:validate_encounter_pools(routes)
registry:validate_boss_routes(routes)

local usage = {}
for _, profile_id in ipairs(routes.profile_order) do
  for _, layer in ipairs(routes:get_profile(profile_id).layers) do
    for _, node in ipairs(layer.nodes) do
      if node.boss_id then
        usage[node.boss_id] = usage[node.boss_id] or {}
        usage[node.boss_id][#usage[node.boss_id] + 1] = profile_id .. ":" .. node.key
      end
    end
  end
end

local ids = {}
for id in pairs(registry.bosses) do ids[#ids + 1] = id end
table.sort(ids)
for _, id in ipairs(ids) do
  local boss = registry:get_boss(id)
  local arena = registry:get_boss_arena(boss.arena_profile_id)
  local providers = {}
  for _, installation in ipairs(boss.installed_components) do
    local component = registry:get_component(installation.component_id)
    for _, ability_id in ipairs(component.abilities) do
      providers[ability_id] = providers[ability_id] or {}
      providers[ability_id][#providers[ability_id] + 1] = component.display_name
    end
  end
  io.write(string.format("%s — %s\n", id, boss.display_name))
  io.write(string.format("  arena: %s (%s)  hp: %d  ammo: %d\n", arena.id, arena.display_name, boss.health, boss.ammo))
  for _, installation in ipairs(boss.installed_components) do
    io.write(string.format("  %s: %s\n", installation.slot_id, registry:get_component(installation.component_id).display_name))
  end
  local ability_ids = {}
  for ability_id in pairs(providers) do ability_ids[#ability_ids + 1] = ability_id end
  table.sort(ability_ids)
  for _, ability_id in ipairs(ability_ids) do
    io.write(string.format("  %s providers: %s\n", ability_id, table.concat(providers[ability_id], ", ")))
  end
  io.write("  routes: " .. table.concat(usage[id] or {}, ", ") .. "\n")
end
local arena_count = 0
for _ in pairs(registry.boss_arenas) do arena_count = arena_count + 1 end
io.write(string.format("Boss content valid: %d definitions, %d arena profiles\n", #ids, arena_count))
