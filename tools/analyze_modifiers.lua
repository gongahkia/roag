-- Headless serialized-modifier report. Run with `luajit tools/analyze_modifiers.lua`.
package.path = "./?.lua;./?/init.lua;" .. package.path

local Definitions = require("src.expedition.modifiers")
local Registry = require("src.content.registry")
local Simulator = require("src.expedition.modifier_simulator")
local Json = require("src.persistence.json")

local registry = assert(Definitions.load({ registry = Registry.load() }))
local counts = { triggers = {}, effects = {}, conditions = {}, expressions = {} }
local function add(group, id) group[id] = (group[id] or 0) + 1 end
local function expression(group, value)
  if type(value) == "table" and value.kind and Definitions.STACK_EXPRESSIONS[value.kind] then add(group, value.kind) end
end
for _, definition in ipairs(registry.ordered) do
  for _, effect in ipairs(definition.static_effects or {}) do expression(counts.expressions, effect.value); add(counts.effects, effect.kind) end
  for _, hook in ipairs(definition.hooks or {}) do
    add(counts.triggers, hook.trigger)
    for _, condition in ipairs(hook.conditions or {}) do add(counts.conditions, condition.kind) end
    for _, effect in ipairs(hook.effects) do
      add(counts.effects, effect.kind)
      for _, value in pairs(effect) do expression(counts.expressions, value) end
    end
  end
end
local function print_group(label, group)
  print(label .. ":")
  local keys = {}; for key in pairs(group) do keys[#keys + 1] = key end; table.sort(keys)
  for _, key in ipairs(keys) do print("  " .. key .. " " .. group[key]) end
end
print("MODIFIER COUNT " .. #registry.ordered)
local enabled, locked = 0, 0
for _, definition in ipairs(registry.ordered) do if definition.pool.enabled then enabled = enabled + 1 end; if definition.unlock and definition.unlock.kind == "meta_unlock" then locked = locked + 1 end end
print("POOL ENABLED " .. enabled .. "  LOCKED " .. locked)
print_group("TRIGGERS", counts.triggers); print_group("EFFECTS", counts.effects); print_group("CONDITIONS", counts.conditions); print_group("STACK EXPRESSIONS", counts.expressions)
local deterministic_failures, round_trip_failures = 0, 0
for _, definition in ipairs(registry.ordered) do
  if not Definitions.round_trip(definition, { registry = Registry.load() }) then round_trip_failures = round_trip_failures + 1 end
  local config = { definition = definition, modifier_registry = registry, stack_count = 5, trigger = (definition.hooks[1] and definition.hooks[1].trigger) or "on_attack", attack_tags = { projectile = true, melee = true, electric = true, explosive = true }, capabilities = { ["ability.electrical.discharge"] = true } }
  local first, second = Simulator.run(config), Simulator.run(config)
  if Json.encode(first.trace) ~= Json.encode(second.trace) then deterministic_failures = deterministic_failures + 1 end
end
local unused = {}; for id in pairs(Definitions.EFFECTS) do if not counts.effects[id] then unused[#unused + 1] = id end end; table.sort(unused)
print("ROUND TRIP FAILURES " .. round_trip_failures)
print("DETERMINISM FAILURES " .. deterministic_failures)
print("UNUSED EFFECT PRIMITIVES " .. (#unused > 0 and table.concat(unused, ", ") or "none"))
if round_trip_failures > 0 or deterministic_failures > 0 then os.exit(1) end
