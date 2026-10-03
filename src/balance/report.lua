-- Plain-data production balance diagnostics. This does not attempt to play a
-- tactical run: its scenarios make their kill/reward assumptions explicit so
-- numerical tuning remains reproducible and honest about its limits.
local Registry = require("src.content.registry")
local RouteDefinitions = require("src.routes.definitions")
local Inventory = require("src.inventory.inventory")
local Balance = require("content.balance.legacy")

local Report = {}

local function sorted_keys(values)
  local result = {}
  for key in pairs(values) do result[#result + 1] = key end
  table.sort(result)
  return result
end

local function count(values)
  local result = 0
  for _ in pairs(values) do result = result + 1 end
  return result
end

local function copy_scalars(values)
  local result = {}
  for key, value in pairs(values or {}) do
    if type(value) ~= "table" then result[key] = value end
  end
  return result
end

local function validate_positive_integer(value, label)
  assert(type(value) == "number" and value > 0 and value % 1 == 0, label .. " must be a positive integer")
end

function Report.validate_config(config)
  assert(type(config) == "table" and config.id == "balance.legacy.production", "Balance config has invalid ID")
  for name, profile in pairs({ discoveries = config.discoveries, reinforcements = config.reinforcements }) do
    assert(type(profile) == "table", "Balance config " .. name .. " must be a table")
    assert(type(profile.placement_percent) == "number" and profile.placement_percent >= 0 and profile.placement_percent <= 100
      and profile.placement_percent % 1 == 0, "Balance config " .. name .. " placement_percent must be 0..100")
  end
  validate_positive_integer(config.discoveries.max_sites_per_floor, "Balance config discovery max_sites_per_floor")
  validate_positive_integer(config.reinforcements.max_sources_per_floor, "Balance config reinforcement max_sources_per_floor")
  validate_positive_integer(config.reinforcements.warning_delay_turns, "Balance config reinforcement warning_delay_turns")
  assert(type(config.economy) == "table", "Balance config economy must be a table")
  for _, key in ipairs({ "resale_base_fraction", "resale_condition_fraction" }) do
    assert(type(config.economy[key]) == "number" and config.economy[key] >= 0,
      "Balance config economy." .. key .. " must be non-negative")
  end
  assert(config.economy.resale_base_fraction + config.economy.resale_condition_fraction <= 1,
    "Balance config resale cannot exceed full purchase value")
  assert(type(config.scenarios) == "table" and type(config.scenarios.moderate_clear_remaining_fraction) == "number"
      and config.scenarios.moderate_clear_remaining_fraction >= 0 and config.scenarios.moderate_clear_remaining_fraction <= 1,
    "Balance config moderate scenario fraction must be 0..1")
  return true
end

local function component_summary(registry)
  local rows, min_integrity, max_integrity, worn = {}, nil, nil, 0
  for _, id in ipairs(sorted_keys(registry.components)) do
    local component = registry.components[id]
    min_integrity = not min_integrity and component.max_integrity or math.min(min_integrity, component.max_integrity)
    max_integrity = not max_integrity and component.max_integrity or math.max(max_integrity, component.max_integrity)
    if component.wear_per_use > 0 then worn = worn + 1 end
    rows[#rows + 1] = {
      id = id, display_name = component.display_name, slot = component.compatible_slots[1],
      integrity = component.max_integrity, mass = component.mass, scrap_value = component.scrap_value,
      wear_per_use = component.wear_per_use, footprint = {
        width = component.inventory.width, height = component.inventory.height, rotatable = component.inventory.rotatable,
      }, abilities = component.abilities,
    }
  end
  return { count = #rows, integrity_range = { min = min_integrity, max = max_integrity }, worn_component_count = worn, entries = rows }
end

local function service_summary(registry)
  local result = {}
  for _, id in ipairs(sorted_keys(registry.services)) do
    local service = registry.services[id]
    result[#result + 1] = { id = id, role = service.role, normal = service.stock.normal, final_hub = service.stock.final_hub }
  end
  return result
end

local function modifier_rows(registry, collection, grant_boons)
  local rows = {}
  for _, id in ipairs(sorted_keys(collection)) do
    local value = collection[id]
    local modifiers = value.modifiers
    if grant_boons then
      modifiers = {}
      for _, boon_id in ipairs(value.granted_boon_ids) do
        for key, amount in pairs(registry:get_boon(boon_id).modifiers) do modifiers[key] = (modifiers[key] or 0) + amount end
      end
    end
    rows[#rows + 1] = { id = id, display_name = value.display_name, price = value.price, cost = value.cost,
      prerequisites = value.prerequisites, unlocks = value.unlocks, modifiers = modifiers, description = value.description }
  end
  return rows
end

local function boss_summary(registry)
  local result = {}
  for _, id in ipairs(sorted_keys(registry.bosses)) do
    local boss = registry.bosses[id]
    local components = {}
    for _, installed in ipairs(boss.installed_components) do
      components[#components + 1] = { slot_id = installed.slot_id, component_id = installed.component_id }
    end
    result[#result + 1] = {
      id = id, display_name = boss.display_name, health = boss.health, ammo = boss.ammo,
      milestone_data_reward = boss.milestone_data_reward, final_data_reward = boss.final_data_reward,
      arena_profile_id = boss.arena_profile_id, components = components,
    }
  end
  return result
end

local function tier_summary(definitions)
  local tiers, total_initial, conservative_scrap, moderate_scrap, aggressive_scrap, floor_data = {}, 0, 0, 0, 0, 0
  for _, id in ipairs(definitions.tier_order) do
    local tier = definitions:get_tier(id)
    local settings = tier.settings
    local initial = settings.targets + settings.enemies
    local remaining = math.max(0, initial - settings.objective_required)
    local conservative = settings.objective_required + settings.completion_scrap_reward
    local moderate = conservative + math.floor(remaining * Balance.scenarios.moderate_clear_remaining_fraction)
    local aggressive = initial + settings.completion_scrap_reward
    tiers[#tiers + 1] = {
      id = id, number = tier.number, settings = copy_scalars(settings), initial_reward_eligible = initial,
      conservative_scrap = conservative, moderate_scrap = moderate, aggressive_scrap = aggressive,
    }
    total_initial = total_initial + initial
    conservative_scrap, moderate_scrap, aggressive_scrap = conservative_scrap + conservative, moderate_scrap + moderate, aggressive_scrap + aggressive
    floor_data = floor_data + settings.completion_data_reward
  end
  return tiers, {
    initial_reward_eligible = total_initial,
    conservative_scrap = conservative_scrap,
    moderate_scrap = moderate_scrap,
    aggressive_scrap = aggressive_scrap,
    floor_data = floor_data,
  }
end

function Report.build(options)
  options = options or {}
  local registry, definitions = options.registry or Registry.load(), options.route_definitions or RouteDefinitions.load()
  Report.validate_config(Balance)
  local tiers, economy = tier_summary(definitions)
  local normal_floor_count = #tiers
  local function reward(id, key)
    return assert(registry:get_boss(id)[key], "Boss '" .. id .. "' has no " .. key)
  end
  local first_milestone = reward("boss.forest.iron_colossus", "milestone_data_reward")
  local second_milestone = reward("boss.wild.ash_mauler", "milestone_data_reward")
  local apex = reward("boss.apex.kinetic_harbinger", "milestone_data_reward")
  local final = reward("boss.legacy.final", "final_data_reward")
  local discovery_expected = normal_floor_count * (Balance.discoveries.placement_percent / 100)
  local repeat_scrap_reward, first_data_reward = 2, 2
  local first_discovery_data = discovery_expected * first_data_reward
  local known_repeat_scrap = discovery_expected * repeat_scrap_reward
  local research_rich_starting_scrap = 6
  return {
    format = "roag.balance_report",
    version = 1,
    assumptions = {
      description = "Scenarios model completion/resource awards only; they do not simulate tactical damage, missed shots, player deaths, salvage sales, or service purchases.",
      conservative = "Completes only each tier objective from first-generation eligible actors; no repeat discoveries.",
      moderate = "Completes objectives plus half of each tier's remaining first-generation eligible actors; known-account repeat discovery expectation included separately.",
      aggressive = "Clears every first-generation eligible target/enemy; ecology-on-ecology kills remain unrewarded.",
      discovery_expectation = "Expected value uses normal-floor count × configured discovery placement percent; it is not a guarantee in a single run.",
    },
    run_structure = { normal_floors = normal_floor_count, milestone_bosses = 3, terminal_bosses = 1, total_bosses = 4 },
    tuning = Balance,
    tiers = tiers,
    encounter_economy = economy,
    economy_scenarios = {
      fresh_conservative = { starting_scrap = 0, expected_scrap_before_sales = economy.conservative_scrap, persistent_data_before_discoveries = economy.floor_data + first_milestone + second_milestone + apex + final,
        expected_first_discovery_data = first_discovery_data },
      fresh_moderate = { starting_scrap = 0, expected_scrap_before_sales = economy.moderate_scrap, persistent_data_before_discoveries = economy.floor_data + first_milestone + second_milestone + apex + final,
        expected_first_discovery_data = first_discovery_data },
      fresh_aggressive = { starting_scrap = 0, expected_scrap_before_sales = economy.aggressive_scrap, persistent_data_before_discoveries = economy.floor_data + first_milestone + second_milestone + apex + final,
        expected_first_discovery_data = first_discovery_data },
      research_rich_moderate = { starting_scrap = research_rich_starting_scrap, expected_scrap_before_sales = research_rich_starting_scrap + economy.moderate_scrap,
        expected_known_discovery_scrap = known_repeat_scrap, persistent_data_before_discoveries = economy.floor_data + first_milestone + second_milestone + apex + final },
      heavy_boss_part_build = { component_mass_range = { min = 6, max = 8 }, footprint_cells = 9,
        note = "Large weapons use irregular 3x3 cargo footprints, so their rotation and holes matter when packing the baseline 7x7 inventory." },
    },
    inventory = { width = Inventory.DEFAULT_WIDTH, height = Inventory.DEFAULT_HEIGHT, thresholds = Inventory.DEFAULT_THRESHOLDS,
      note = "Encumbrance is currently an informational cargo classification; it has no movement penalty." },
    services = service_summary(registry),
    components = component_summary(registry),
    charms = modifier_rows(registry, registry.charms, true),
    curses = modifier_rows(registry, registry.curses, false),
    research = { node_count = count(registry.research), total_cost = (function()
      local total = 0; for _, node in pairs(registry.research) do total = total + node.cost end; return total
    end)(), nodes = modifier_rows(registry, registry.research, false) },
    bosses = boss_summary(registry),
  }
end

return Report
