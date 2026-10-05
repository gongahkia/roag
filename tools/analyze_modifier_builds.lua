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
local XP_BY_ROLE = { rusher = 2, flanker = 3, ranged = 3, controller = 4, heavy = 5 }

local function analyze_rewards(count)
  local stats = { sequences = count, pickups = 0, unique = 0, duplicates = 0, top_stack = 0, failures = 0 }
  for seed = 1, count do
    local run = Run.new({ seed = 1200000 + seed, character_id = "expedition.gunner", meta_profile = profile() })
    local exp, acquired, caches_bought = run.session.state.expedition, 0, 0
    for _, plan in ipairs(run.plan) do
      for _, enemy in ipairs(plan.enemies) do
        exp.xp = exp.xp + (XP_BY_ROLE[enemy.role] or 2) + (enemy.elite and 7 or 0)
      end
      while exp.xp_thresholds[exp.level_threshold_index] and exp.xp >= exp.xp_thresholds[exp.level_threshold_index] do
        exp.xp = exp.xp - exp.xp_thresholds[exp.level_threshold_index]
        exp.level, exp.level_threshold_index = exp.level + 1, exp.level_threshold_index + 1
        local pick = run:reward_options(3, "level")[1]
        run:add_passive(pick.id)
        acquired = acquired + 1
      end
      if plan.reward_kind == "random" or plan.elite then
        local pick = run:reward_options(1, plan.elite and "elite" or "random")[1]
        run:add_passive(pick.id)
        acquired = acquired + 1
      elseif plan.reward_kind == "cache" and caches_bought < 2 then
        -- The FEEL-01 cash curve intentionally supports roughly two of three
        -- cache purchases, so this headless distribution models a successful
        -- spend-now/save-later route without inventing an unrelated picker.
        local pick = run:reward_options(1, "cache")[1]
        run:add_passive(pick.id)
        acquired, caches_bought = acquired + 1, caches_bought + 1
      end
    end
    local unique, top = 0, 0; for _, value in pairs(exp.passive_stacks) do unique, top = unique + 1, math.max(top, value) end
    stats.unique, stats.duplicates, stats.top_stack = stats.unique + unique, stats.duplicates + (acquired - unique), math.max(stats.top_stack, top)
    stats.pickups = stats.pickups + acquired
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
