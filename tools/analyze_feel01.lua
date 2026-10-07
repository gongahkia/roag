-- FEEL-01 structural audit.  This validates chamber/topology, deterministic
-- XP/cash progression and reward-shape properties; it intentionally cannot
-- determine whether a human finds a chamber fun.
package.path = "./?.lua;./?/init.lua;" .. package.path

local Chambers = require("src.expedition.chambers")
local Content = require("src.expedition.content")
local Run = require("src.expedition.run")
local MetaProfile = require("src.persistence.meta_profile")

local Analyzer = {}

local XP_BY_ROLE = { rusher = 2, flanker = 3, ranged = 3, controller = 4, heavy = 5 }
local CASH_BY_ROLE = { rusher = 1, flanker = 1, ranged = 1, controller = 2, heavy = 2 }

local function profile()
  local value = MetaProfile.new()
  for _, id in ipairs({
    "expedition.unlock.character.conductor", "expedition.unlock.character.demolitionist",
    "expedition.unlock.item.arc_relay", "expedition.unlock.item.rupture_core",
  }) do MetaProfile.unlock_expedition(value, id) end
  return value
end

local function add(map, id, value)
  map[id] = (map[id] or 0) + (value or 1)
end

local function level_progress(total)
  local remainder, levels, first = total, 0, nil
  for index, threshold in ipairs(Run.XP_THRESHOLDS) do
    if remainder < threshold then break end
    remainder = remainder - threshold
    levels = levels + 1
    first = first or index
  end
  return levels, remainder
end

local function chamber_ok(chamber)
  local bounds, layout = chamber.bounds, chamber.layout
  local seen, queue, head = {}, { chamber.player_spawn }, 1
  seen[chamber.player_spawn.x .. ":" .. chamber.player_spawn.y] = true
  while queue[head] do
    local point = queue[head]
    head = head + 1
    for _, delta in ipairs({ { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 } }) do
      local x, y, id = point.x + delta[1], point.y + delta[2], (point.x + delta[1]) .. ":" .. (point.y + delta[2])
      if layout[id] and not seen[id] then seen[id], queue[#queue + 1] = true, { x = x, y = y } end
    end
  end
  local count = 0
  for id in pairs(layout) do count = count + 1; if not seen[id] then return false end end
  return count == chamber.cell_count and bounds.width >= 8 and bounds.width <= 16 and bounds.height >= 7 and bounds.height <= 12
end

local function reward_shape(seed, plan, run_profile)
  local run = Run.new({ seed = 2100000 + seed, character_id = "expedition.gunner", meta_profile = run_profile })
  local exp, acquired, level_choices = run.session.state.expedition, 0, 0
  -- Feed expected chamber XP directly through the same threshold state, then
  -- consume the same real reward generator (not a parallel weighting model).
  for _, encounter in ipairs(plan) do
    local xp = 0
    for _, enemy in ipairs(encounter.enemies) do xp = xp + (XP_BY_ROLE[enemy.role] or 2) + (enemy.elite and 7 or 0) end
    exp.xp = exp.xp + xp
    while (exp.xp_thresholds or {})[exp.level_threshold_index] and exp.xp >= exp.xp_thresholds[exp.level_threshold_index] do
      exp.xp = exp.xp - exp.xp_thresholds[exp.level_threshold_index]
      exp.level, exp.level_threshold_index, acquired = exp.level + 1, exp.level_threshold_index + 1, acquired + 1
      level_choices = level_choices + 1
      -- Model a deliberate player: establish ingredients first, then take
      -- two reinforcement choices for every later discovery.  The game still
      -- presents all three choices; this is only a reproducible analyzer
      -- policy, rather than silently assuming every player always chooses A.
      local options = run:reward_options(3, "level")
      local discovery = level_choices <= 3 or level_choices % 3 == 0
      local option = options[discovery and 2 or 1]
      run:add_passive(option.id)
    end
    if encounter.reward_kind == "random" then
      acquired = acquired + 1
      run:add_passive(run:reward_options(1, "random")[1].id)
    elseif encounter.elite then
      acquired = acquired + 1
      run:add_passive(run:reward_options(1, "elite")[1].id)
    end
  end
  -- A normal successful cash curve can afford two of the three escalating
  -- caches.  This count is deliberately a planning estimate; it is not a
  -- player-behaviour claim.
  acquired = acquired + 2
  local unique, top, top_three = 0, 0, {}
  for _, stacks in pairs(exp.passive_stacks) do unique = unique + 1; top_three[#top_three + 1] = stacks; top = math.max(top, stacks) end
  table.sort(top_three, function(a, b) return a > b end)
  return { acquired = acquired, unique = unique, duplicates = acquired - unique, top = top,
    top_three = { top_three[1] or 0, top_three[2] or 0, top_three[3] or 0 } }
end

function Analyzer.analyze(seed_count)
  seed_count = seed_count or 2000
  local result = {
    seeds = seed_count, structural_failures = 0, offscreen_threat_failures = 0,
    topologies = {}, dimensions = {}, archetypes = {}, hazards = {}, xp_per_chamber = {},
    role_diversity = {}, elites_by_stage = { 0, 0, 0 }, hazards_by_stage = { 0, 0, 0 },
    first_level_chambers = {}, levels = 0, xp = 0, cash = 0, affordable_caches = 0,
    pickups = 0, unique = 0, duplicates = 0, top_stack = 0, top_three = { 0, 0, 0 },
  }
  local all = profile()
  for seed = 1, seed_count do
    local plan = Run.plan(1800000 + seed)
    local cumulative_xp, cumulative_cash, first_level = 0, 0, nil
    local topology_streak, prior_topology = 0, nil
    for index, encounter in ipairs(plan) do
      -- Analyze the exact authored board selected by the production planner;
      -- do not regenerate a second topology in the analyzer.
      local chamber = Chambers.instantiate(encounter.chamber)
      if not chamber_ok(chamber) then result.structural_failures = result.structural_failures + 1 end
      add(result.topologies, encounter.topology)
      add(result.dimensions, chamber.bounds.width .. "x" .. chamber.bounds.height)
      add(result.archetypes, encounter.kind)
      if encounter.topology == "conductive" or encounter.topology == "volatile" then add(result.hazards, encounter.topology) end
      local roles = {}
      for _, enemy in ipairs(encounter.enemies) do roles[enemy.role] = true end
      local diversity = 0; for _ in pairs(roles) do diversity = diversity + 1 end
      result.role_diversity[index] = (result.role_diversity[index] or 0) + diversity
      if encounter.elite then result.elites_by_stage[encounter.stage] = result.elites_by_stage[encounter.stage] + 1 end
      if encounter.topology == "conductive" or encounter.topology == "volatile" then result.hazards_by_stage[encounter.stage] = result.hazards_by_stage[encounter.stage] + 1 end
      topology_streak = encounter.topology == prior_topology and topology_streak + 1 or 1
      if topology_streak > 2 then result.structural_failures = result.structural_failures + 1 end
      prior_topology = encounter.topology
      local visible = Chambers.visible_tiles(chamber.bounds, false)
      for ordinal, enemy in ipairs(encounter.enemies) do
        local point = Chambers.spawn_point(chamber, enemy.spawn_group, ordinal, nil)
        local distance = math.abs(point.x - chamber.player_spawn.x) + math.abs(point.y - chamber.player_spawn.y)
        if distance > visible then result.offscreen_threat_failures = result.offscreen_threat_failures + 1 end
        local xp = (XP_BY_ROLE[enemy.role] or 2) + (enemy.elite and 7 or 0)
        local cash = (CASH_BY_ROLE[enemy.role] or 1) + (enemy.elite and 3 or 0)
        cumulative_xp, cumulative_cash = cumulative_xp + xp, cumulative_cash + cash
        result.xp_per_chamber[index] = (result.xp_per_chamber[index] or 0) + xp
      end
      if not first_level and cumulative_xp >= Run.XP_THRESHOLDS[1] then first_level = index end
    end
    cumulative_xp, cumulative_cash = cumulative_xp + 24, cumulative_cash + 10
    local levels = level_progress(cumulative_xp)
    result.levels, result.xp, result.cash = result.levels + levels, result.xp + cumulative_xp, result.cash + cumulative_cash
    add(result.first_level_chambers, tostring(first_level or 99))
    -- Cash cache model: 8, 26, 44. Buy greedily only when affordable;
    -- recording this shows whether the economy presents more than a fake
    -- always-buy decision.
    local wallet, bought = 0, 0
    for index, encounter in ipairs(plan) do
      for _, enemy in ipairs(encounter.enemies) do wallet = wallet + (CASH_BY_ROLE[enemy.role] or 1) + (enemy.elite and 3 or 0) end
      if encounter.reward_kind == "cache" then
        local cost = 6 + bought * 16 + encounter.stage * 2
        if wallet >= cost then wallet, bought = wallet - cost, bought + 1 end
      end
    end
    result.affordable_caches = result.affordable_caches + bought
    local shape = reward_shape(seed, plan, all)
    result.pickups, result.unique, result.duplicates = result.pickups + shape.acquired, result.unique + shape.unique, result.duplicates + shape.duplicates
    result.top_stack = math.max(result.top_stack, shape.top)
    for index = 1, 3 do result.top_three[index] = result.top_three[index] + shape.top_three[index] end
  end
  return result
end

function Analyzer.report(result)
  print("FEEL01 chamber_sequences=" .. result.seeds .. " structural_failures=" .. result.structural_failures
    .. " offscreen_threat_failures=" .. result.offscreen_threat_failures)
  print("FEEL01 avg_levels=" .. (result.levels / result.seeds) .. " avg_xp=" .. (result.xp / result.seeds)
    .. " avg_cash=" .. (result.cash / result.seeds) .. " avg_affordable_caches=" .. (result.affordable_caches / result.seeds))
  print("FEEL01 avg_pickups=" .. (result.pickups / result.seeds) .. " avg_unique=" .. (result.unique / result.seeds)
    .. " avg_duplicates=" .. (result.duplicates / result.seeds) .. " max_top_stack=" .. result.top_stack)
  print("FEEL01 reward_choice_policy=discover_first_then_two_reinforce")
  print("FEEL01 stage_elites=" .. table.concat(result.elites_by_stage, ",")
    .. " stage_hazards=" .. table.concat(result.hazards_by_stage, ","))
  for _, label in ipairs(Chambers.TOPOLOGIES) do print("topology_" .. label .. "=" .. (result.topologies[label] or 0)) end
  for index = 1, Run.REGULAR_ENCOUNTERS do
    print("chamber_" .. index .. "_avg_xp=" .. ((result.xp_per_chamber[index] or 0) / result.seeds)
      .. " avg_role_diversity=" .. ((result.role_diversity[index] or 0) / result.seeds))
  end
end

if ... == nil then
  local result = Analyzer.analyze(2000)
  Analyzer.report(result)
  if result.structural_failures > 0 or result.offscreen_threat_failures > 0 then os.exit(1) end
end

return Analyzer
