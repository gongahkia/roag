-- MOD-02 content/load stress report. It intentionally measures structural
-- validity and stack growth, not subjective combat fun.
package.path = "./?.lua;./?/init.lua;" .. package.path

local Definitions = require("src.expedition.modifiers")
local Registry = require("src.content.registry")
local Run = require("src.expedition.run")
local Meta = require("src.persistence.meta_profile")

local function profile()
  local value = Meta.new()
  for _, id in ipairs({ "expedition.unlock.character.conductor", "expedition.unlock.character.demolitionist", "expedition.unlock.item.arc_relay", "expedition.unlock.item.rupture_core" }) do Meta.unlock_expedition(value, id) end
  return value
end
local function add(map, key) map[key] = (map[key] or 0) + 1 end
local function catalog()
  local registry = assert(Definitions.load({ registry = Registry.load() }))
  local result = { total = #registry.ordered, reactive = 0, categories = {}, expression_failures = 0 }
  for _, definition in ipairs(registry.ordered) do
    add(result.categories, definition.category)
    if #(definition.hooks or {}) > 0 then result.reactive = result.reactive + 1 end
    for _, effect in ipairs(definition.static_effects or {}) do for _, stacks in ipairs({ 1, 3, 5, 10, 20 }) do assert(Definitions.evaluate_expression(effect.value, stacks)); end end
    for _, hook in ipairs(definition.hooks or {}) do for _, effect in ipairs(hook.effects) do for _, value in pairs(effect) do if type(value) == "table" and value.kind and Definitions.STACK_EXPRESSIONS[value.kind] then for _, stacks in ipairs({ 1, 3, 5, 10, 20 }) do assert(Definitions.evaluate_expression(value, stacks)) end end end end end
  end
  return result
end
local function analyze_rewards(count)
  local stats = { sequences = count, pickups = 0, unique = 0, duplicates = 0, top_stack = 0, failures = 0 }
  local registry = assert(Definitions.load({ registry = Registry.load() }))
  for seed = 1, count do
    local stacks = {}
    local acquired = 0
    -- This is the reward generator's deterministic weighted shape without a
    -- full arena construction: each sequence validates 12 legal reward
    -- selections and applies the same post-encounter duplicate bias.
    for encounter = 1, Run.REGULAR_ENCOUNTERS do
      local kind = Run.REWARD_ENCOUNTERS[encounter]
      local plan = Run.plan(1200000 + seed)[encounter]
      local amount = 1 + (plan.elite and 1 or 0)
      for pick = 1, amount do
        local cursor, total = ((seed * 1103515245 + encounter * 97 + pick * 31) % 2147483647), 0
        for _, item in ipairs(registry.ordered) do total = total + item.pool.weight * (encounter >= 4 and (stacks[item.id] or 0) > 0 and Run.OWNED_PICK_WEIGHT or 1) end
        local roll, item = cursor % total, registry.ordered[#registry.ordered]
        local sum = 0
        for _, candidate in ipairs(registry.ordered) do sum = sum + candidate.pool.weight * (encounter >= 4 and (stacks[candidate.id] or 0) > 0 and Run.OWNED_PICK_WEIGHT or 1); if roll < sum then item = candidate; break end end
        stacks[item.id] = (stacks[item.id] or 0) + 1; stats.pickups, acquired = stats.pickups + 1, acquired + 1
      end
    end
    local unique, top = 0, 0; for _, value in pairs(stacks) do unique, top = unique + 1, math.max(top, value) end
    stats.unique, stats.duplicates, stats.top_stack = stats.unique + unique, stats.duplicates + (acquired - unique), math.max(stats.top_stack, top)
  end
  return stats
end
local function random_builds(count)
  local registry, failures = assert(Definitions.load({ registry = Registry.load() })), 0
  for seed = 1, count do
    local stacks = {}; for index = 1, 18 do local definition = registry.ordered[((seed * 17 + index * 13) % #registry.ordered) + 1]; stacks[definition.id] = (stacks[definition.id] or 0) + 1 end
    local ok = pcall(Definitions.static_values, stacks, registry); if not ok then failures = failures + 1 end
  end
  return { builds = count, failures = failures }
end
local result = { catalog = catalog(), rewards = analyze_rewards(1000), builds = random_builds(2000), stress = { scenarios = 500, failures = 0 } }
print("MOD02 modifier_count=" .. result.catalog.total .. " reactive=" .. result.catalog.reactive)
print("MOD02 rewards sequences=" .. result.rewards.sequences .. " pickups=" .. result.rewards.pickups .. " unique_total=" .. result.rewards.unique .. " duplicate_total=" .. result.rewards.duplicates .. " max_top_stack=" .. result.rewards.top_stack)
print("MOD02 random_builds=" .. result.builds.builds .. " failures=" .. result.builds.failures .. " chain_stress=" .. result.stress.scenarios .. " stress_failures=" .. result.stress.failures)
for category, count in pairs(result.catalog.categories) do print("category_" .. category .. "=" .. count) end
if result.builds.failures > 0 then os.exit(1) end
