-- Headless EXP-01 generation audit. This measures structural pacing and
-- deterministic content coverage; it intentionally does not claim to measure
-- whether an arena is fun to play.
package.path = "./?.lua;./?/init.lua;" .. package.path

local Run = require("src.expedition.run")
local Content = require("src.expedition.content")
local MetaProfile = require("src.persistence.meta_profile")

local Analyzer = {}

local function all_unlocked_profile()
  local profile = MetaProfile.new()
  for _, id in ipairs({
    "expedition.unlock.character.conductor", "expedition.unlock.character.demolitionist",
    "expedition.unlock.item.arc_relay", "expedition.unlock.item.rupture_core",
  }) do MetaProfile.unlock_expedition(profile, id) end
  return profile
end

function Analyzer.analyze(seed_count)
  seed_count = seed_count or 500
  local result = {
    seeds = seed_count, failures = 0, encounters = 0, rewards = 0, chests = 0,
    archetypes = {}, topologies = {}, budgets = {}, max_streak = 0, first_reward_max = 0,
    enemies = 0, elites = 0, bosses = seed_count, curated = {},
  }
  for seed = 1, seed_count do
    local ok, plans = pcall(Run.plan, 910000 + seed)
    if not ok then
      result.failures = result.failures + 1
    else
      local prior, streak, first_reward, xp, threshold_index = nil, 0, nil, 0, 1
      for _, plan in ipairs(plans) do
        local valid = plan.actual_budget <= plan.budget and #plan.enemies > 0
        for role, required in pairs(plan.template.minimum_roles) do
          local found = 0
          for _, enemy in ipairs(plan.enemies) do if enemy.role == role then found = found + 1 end end
          valid = valid and found >= required
        end
        if not valid then result.failures = result.failures + 1 end
        result.encounters = result.encounters + 1
        result.archetypes[plan.kind] = (result.archetypes[plan.kind] or 0) + 1
        result.topologies[plan.topology] = (result.topologies[plan.topology] or 0) + 1
        result.budgets[plan.index] = result.budgets[plan.index] or { total = 0, samples = 0 }
        result.budgets[plan.index].total = result.budgets[plan.index].total + plan.budget
        result.budgets[plan.index].samples = result.budgets[plan.index].samples + 1
        result.enemies = result.enemies + #plan.enemies
        if plan.elite then result.elites = result.elites + 1 end
        if plan.reward_kind == "random" then result.rewards = result.rewards + 1; first_reward = first_reward or plan.index end
        if plan.reward_kind == "cache" then result.chests = result.chests + 1 end
        for _, enemy in ipairs(plan.enemies) do
          local values = { rusher = 2, flanker = 3, ranged = 3, controller = 4, heavy = 5 }
          xp = xp + (values[enemy.role] or 2) + (enemy.elite and 7 or 0)
        end
        while Run.XP_THRESHOLDS[threshold_index] and xp >= Run.XP_THRESHOLDS[threshold_index] do
          xp, threshold_index = xp - Run.XP_THRESHOLDS[threshold_index], threshold_index + 1
          first_reward = first_reward or plan.index
        end
        streak = prior == plan.id and streak + 1 or 1
        result.max_streak = math.max(result.max_streak, streak)
        prior = plan.id
      end
      result.first_reward_max = math.max(result.first_reward_max, first_reward or math.huge)
    end
  end
  -- Curated high-stack probes show that build growth is material rather than
  -- a hidden numerical no-op. They reuse the real modifier/effect state.
  local profile = all_unlocked_profile()
  local recipes = {
    projectile_storm = { "expedition.passive.ballistic_lens", "expedition.passive.scatter_matrix", "expedition.passive.piercing_rounds", "expedition.passive.recycler" },
    electric_chain = { "expedition.passive.arc_relay", "expedition.passive.conductive_payload", "expedition.passive.piercing_rounds" },
    kinetic_cascade = { "expedition.passive.kinetic_capacitor", "expedition.passive.kinetic_feedback", "expedition.passive.shockwave_emitter" },
    rupture_burst = { "expedition.passive.rupture_core", "expedition.passive.shrapnel_core", "expedition.passive.demolition_kit" },
  }
  for label, ids in pairs(recipes) do
    local character = label == "kinetic_cascade" and "expedition.bruiser" or (label == "electric_chain" and "expedition.conductor" or "expedition.gunner")
    local run = Run.new({ seed = 990000 + #label, character_id = character, meta_profile = profile })
    for _, id in ipairs(ids) do for _ = 1, 5 do run:add_passive(id) end end
    result.curated[label] = {
      projectile_damage = run.session:modifier_value("projectile_damage"),
      projectile_count = run.session:modifier_value("projectile_count"),
      projectile_pierce = run.session:modifier_value("projectile_pierce"),
      melee_force = run.session:modifier_value("melee_force"),
      reactive_effects = #run.session:build_effects(),
    }
  end
  return result
end

function Analyzer.report(result)
  print("EXPEDITION ANALYZER")
  print("seeds=" .. result.seeds .. " failures=" .. result.failures .. " encounters=" .. result.encounters
    .. " enemies=" .. result.enemies .. " elites=" .. result.elites .. " bosses=" .. result.bosses)
  print("first_build_reward_max=" .. result.first_reward_max .. " instant_rewards=" .. result.rewards .. " caches=" .. result.chests
    .. " max_same_archetype_streak=" .. result.max_streak)
  for _, template in ipairs(Content.ENCOUNTERS) do print(template.kind .. "=" .. (result.archetypes[template.kind] or 0)) end
  for topology, count in pairs(result.topologies) do print("topology_" .. topology .. "=" .. count) end
  for index, data in ipairs(result.budgets) do print("encounter_" .. index .. "_avg_budget=" .. (data.total / data.samples)) end
  local labels = {}
  for label in pairs(result.curated) do labels[#labels + 1] = label end
  table.sort(labels)
  for _, label in ipairs(labels) do
    local value = result.curated[label]
    print("curated_" .. label .. " projectile_damage=" .. value.projectile_damage .. " projectile_count=" .. value.projectile_count
      .. " projectile_pierce=" .. value.projectile_pierce .. " melee_force=" .. value.melee_force .. " reactive=" .. value.reactive_effects)
  end
end

if ... == nil then Analyzer.report(Analyzer.analyze(500)) end

return Analyzer
