-- Disposable combat-first Expedition controller.  It deliberately reuses the
-- authoritative Session combat/world systems while keeping progression,
-- rewards and death separate from Campaign/Sandbox persistence.
local Session = require("src.simulation.session")
local Rng = require("src.rng")
local Grid = require("src.world.grid")
local ReinforcementGeneration = require("src.generation.reinforcements")
local RunModifiers = require("src.simulation.run_modifiers")
local Content = require("src.expedition.content")
local Chambers = require("src.expedition.chambers")

local ExpeditionRun = {}
ExpeditionRun.__index = ExpeditionRun

ExpeditionRun.REGULAR_ENCOUNTERS = 12
-- The first budget already supports the most demanding early grammar
-- constraint (two Crossfire ranged enemies or a Hazard controller+rusher).
-- Escalation comes chiefly from composition and arena pressure rather than
-- turning the initial encounters into one-enemy chores.
ExpeditionRun.THREAT_BUDGETS = { 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }
-- XP level-ups are the primary deliberate modifier choice.  A few fast
-- encounter drops and paid caches keep surprise/discretion in the loop
-- without putting a modal after every chamber.
ExpeditionRun.REWARD_ENCOUNTERS = { [2] = "random", [4] = "cache", [6] = "random", [8] = "cache", [10] = "random", [11] = "cache", [12] = "random" }
ExpeditionRun.OWNED_PICK_WEIGHT = 2.7
ExpeditionRun.OWNED_STACK_MOMENTUM = 0.18
ExpeditionRun.XP_THRESHOLDS = { 8, 11, 14, 17, 20, 23, 26, 29, 32, 36 }
-- Every stage draws each of its four authored pressure models once.  The
-- seed changes their ordering/topology, never collapses a run into a single
-- encounter type.
ExpeditionRun.ARCHETYPE_STAGES = {
  {
    "expedition.encounter.swarm", "expedition.encounter.crossfire",
    "expedition.encounter.pincer", "expedition.encounter.duel",
  },
  {
    "expedition.encounter.hazard", "expedition.encounter.breach",
    "expedition.encounter.encirclement", "expedition.encounter.hunter_kite",
  },
  {
    "expedition.encounter.elite_hunt", "expedition.encounter.reinforcement_pressure",
    "expedition.encounter.volatile_arena", "expedition.encounter.breakpoint",
  },
}

local STAGE_TIER = { "tier.legacy.1", "tier.legacy.2", "tier.legacy.3" }

local function copy_map(values)
  local result = {}
  for key, value in pairs(values or {}) do result[key] = value end
  return result
end

local function sorted_pairs(values)
  local keys = {}
  for key in pairs(values or {}) do keys[#keys + 1] = key end
  table.sort(keys)
  local index = 0
  return function()
    index = index + 1
    local key = keys[index]
    if key then return key, values[key] end
  end
end

local function stage_for(index)
  return math.min(3, math.floor((index - 1) / 4) + 1)
end

local function profile_for(template, stage, rng)
  local allowed = stage == 1 and { ["biome.legacy.forest"] = true, ["biome.legacy.cave"] = true }
    or stage == 2 and { ["biome.legacy.forest"] = true, ["biome.legacy.cave"] = true, ["biome.legacy.dungeon"] = true }
    or { ["biome.legacy.forest"] = true, ["biome.legacy.cave"] = true, ["biome.legacy.dungeon"] = true, ["biome.legacy.reactor"] = true }
  local candidates = {}
  for _, id in ipairs(template.profiles) do if allowed[id] then candidates[#candidates + 1] = id end end
  -- Some grammar families debut before their ideal biome tier. Preserve the
  -- mechanical encounter while using a compatible early terrain profile.
  if #candidates == 0 then
    candidates = stage == 1 and { "biome.legacy.forest", "biome.legacy.cave" }
      or stage == 2 and { "biome.legacy.cave", "biome.legacy.dungeon" }
      or { "biome.legacy.dungeon", "biome.legacy.reactor" }
  end
  return rng:choice(candidates)
end

local function role_sequence(template, budget, rng)
  local roles, spent = {}, 0
  local available = {}
  for _, role in ipairs(template.roles) do available[#available + 1] = role end
  for role, required in sorted_pairs(template.minimum_roles) do
    for _ = 1, required do
      roles[#roles + 1], spent = role, spent + Content.ENEMY_COSTS[role]
    end
  end
  local cursor = 1
  while spent < budget and #roles < template.max_enemies do
    local role = available[((rng:int(#available) + cursor - 2) % #available) + 1]
    local cost = Content.ENEMY_COSTS[role]
    if spent + cost > budget then
      role, cost = "rusher", Content.ENEMY_COSTS.rusher
      if spent + cost > budget then break end
    end
    roles[#roles + 1], spent = role, spent + cost
    cursor = cursor + 1
  end
  return roles, spent
end

local function stage_template_id(seed, stage, slot)
  local ids = ExpeditionRun.ARCHETYPE_STAGES[stage]
  local offset = Rng.new(seed):derive("expedition.archetype.stage." .. stage):int(#ids)
  return ids[((slot - 1 + offset - 1) % #ids) + 1]
end

local function encounter_plan(seed, index, prior_topology)
  local stage = stage_for(index)
  local rng = Rng.new(seed):derive("expedition.encounter." .. index)
  local template_id = stage_template_id(seed, stage, ((index - 1) % 4) + 1)
  local template = assert(Content.encounter(template_id))
  local roles, spent = role_sequence(template, ExpeditionRun.THREAT_BUDGETS[index], rng:derive("roles"))
  local topology_choices = template.topologies
  local topology = topology_choices[rng:derive("topology"):int(#topology_choices)]
  if #topology_choices > 1 and topology == prior_topology then
    topology = topology_choices[(rng:derive("topology.fallback"):int(#topology_choices - 1) % #topology_choices) + 1]
    if topology == prior_topology then topology = topology_choices[1] == prior_topology and topology_choices[2] or topology_choices[1] end
  end
  local enemies = {}
  local function spawn_group_for(enemy_index)
    if template.kind == "pincer" then return enemy_index % 2 == 0 and "east" or "west" end
    if template.kind == "crossfire" then return enemy_index % 2 == 0 and "north" or "south" end
    if template.kind == "breach" or template.kind == "duel" then return "east" end
    if template.kind == "hunter_kite" then return enemy_index % 2 == 0 and "north" or "east" end
    if template.kind == "encirclement" or template.kind == "breakpoint" then return "ring" end
    return "ring"
  end
  for enemy_index, role in ipairs(roles) do
    local choices = Content.ENEMY_BY_ROLE[role]
    enemies[#enemies + 1] = {
      role = role,
      enemy_id = choices[rng:derive("enemy." .. enemy_index):int(#choices)],
      spawn_group = spawn_group_for(enemy_index),
      elite = template.elite and enemy_index == 1,
    }
  end
  return {
    index = index,
    stage = stage,
    id = template.id,
    kind = template.kind,
    topology = topology,
    template = template,
    profile_id = profile_for(template, stage, rng:derive("profile")),
    tier_id = STAGE_TIER[stage],
    budget = ExpeditionRun.THREAT_BUDGETS[index],
    actual_budget = spent,
    difficulty = {
      threat_budget = ExpeditionRun.THREAT_BUDGETS[index],
      max_simultaneous_enemies = template.max_enemies,
      role_complexity = #template.roles,
      hazard_complexity = template.hazard and 1 or 0,
      spawn_pressure = stage,
    },
    enemies = enemies,
    reward_kind = ExpeditionRun.REWARD_ENCOUNTERS[index] or "none",
    elite = template.elite,
    hazard = template.hazard,
    reinforcement = template.reinforcement,
  }
end

function ExpeditionRun.plan(seed)
  local result, prior_topology = {}, nil
  for index = 1, ExpeditionRun.REGULAR_ENCOUNTERS do
    result[index] = encounter_plan(seed, index, prior_topology)
    prior_topology = result[index].topology
  end
  return result
end

function ExpeditionRun.character_unlocked(profile, character)
  return character and character.unlock and require("src.persistence.meta_profile").has_expedition_unlock(profile, character.unlock)
end

function ExpeditionRun.available_characters(profile)
  local result = {}
  for _, character in ipairs(Content.CHARACTERS) do
    result[#result + 1] = {
      definition = character,
      unlocked = ExpeditionRun.character_unlocked(profile, character),
      unlock = character.unlock,
    }
  end
  return result
end

function ExpeditionRun.new(options)
  options = options or {}
  local character = assert(Content.character(assert(options.character_id, "Expedition requires character_id")))
  local profile = assert(options.meta_profile, "Expedition requires meta profile")
  assert(ExpeditionRun.character_unlocked(profile, character), "Character is locked")
  local seed = Rng.new(options.seed or 1).seed
  local self = setmetatable({
    seed = seed,
    character = character,
    profile = profile,
    on_unlock = options.on_unlock or function() end,
    on_event = options.on_event or function() end,
    plan = ExpeditionRun.plan(seed),
    pending_reward = nil,
    pending_chest = nil,
    completed = false,
    summary_data = nil,
    newly_unlocked = {},
  }, ExpeditionRun)
  Content.validate(options.registry or require("src.content.registry").load())
  local expedition_state = {
    seed = seed,
    character_id = character.id,
    stage = 1,
    encounter_index = 0,
    currency = 0,
    xp = 0,
    level = 1,
    level_threshold_index = 1,
    xp_thresholds = copy_map(ExpeditionRun.XP_THRESHOLDS),
    pending_level_ups = 0,
    total_xp = 0,
    kills = 0,
    component_breaks = 0,
    passive_stacks = {},
    reserve_ammo = {},
    base_modifiers = copy_map(character.base_modifiers),
  }
  self.session = Session.new({
    seed = seed,
    registry = options.registry,
    content = options.content,
    run_id = "expedition:" .. tostring(seed),
    expedition = true,
    expedition_state = expedition_state,
    modifier_registry = Content.modifier_registry,
    emit = function(event) self.on_event(event) end,
  })
  self:_begin_encounter(1)
  return self
end

function ExpeditionRun:_configure_player()
  local session, state, player = self.session, self.session.state, self.session.state.player
  local body = player.body
  -- Refit the normal physical body instead of creating a parallel class-actor
  -- system. The instance IDs remain ordinary body identities for combat.
  body:uninstall("right_arm")
  body:uninstall("internal_1")
  local weapon = session.component_factory:create(self.character.weapon_component, "expedition")
  local ability = session.component_factory:create(self.character.ability_component, "expedition")
  assert(body:install("right_arm", weapon))
  assert(body:install("internal_1", ability))
  state.expedition.weapon_provider_id = weapon.id
  state.expedition.weapon_ability_id = self.character.weapon_ability
  state.expedition.ability_provider_id = ability.id
  state.expedition.active_ability_id = self.character.active_ability
  state.expedition.reserve_ammo = {}
  local weapon_definition = session.registry:get_ability(self.character.weapon_ability)
  if weapon_definition.ammo then
    state.expedition.reserve_ammo[weapon_definition.ammo.family] = self.character.starting_reserve
    local magazine = session:weapon_magazine(weapon, weapon_definition)
    magazine.loaded = weapon_definition.ammo.magazine_capacity
  end
  for _, passive_id in ipairs(self.character.starting_passives or {}) do
    state.expedition.passive_stacks[passive_id] = (state.expedition.passive_stacks[passive_id] or 0) + 1
  end
  player.base_max_health = self.character.base_hp
  player.health = self.character.base_hp
  player.base_dash_cooldown = 3
  player.presentation_profile = ({
    ["expedition.gunner"] = "light",
    ["expedition.bruiser"] = "heavy",
    ["expedition.conductor"] = "electric",
    ["expedition.demolitionist"] = "demolition",
  })[self.character.id]
  session:refresh_derived_player_stats()
  player.health = player.max_health
end

function ExpeditionRun:_open_near(group, ordinal)
  local state, player, world = self.session.state, self.session.state.player, self.session.state.world
  local chamber = state.expedition and state.expedition.chamber
  if chamber then return Chambers.spawn_point(chamber, group, ordinal, self.session:_occupied()) end
  local directions = {
    north = { 0, 1 }, south = { 0, -1 }, east = { 1, 0 }, west = { -1, 0 },
    ring = { ((ordinal % 3) - 1), ((math.floor(ordinal / 3) % 3) - 1) },
  }
  local delta = directions[group] or directions.ring
  local base_x, base_y = player.x + delta[1] * 4, player.y + delta[2] * 4
  local occupied = self.session:_occupied()
  for radius = 0, 7 do
    for y = base_y - radius, base_y + radius do
      for x = base_x - radius, base_x + radius do
        if Grid.in_bounds(x, y) and world:is_passable(x, y) and not occupied[Grid.key(x, y)] then
          return { x = x, y = y }
        end
      end
    end
  end
  return self.session:_open_location(occupied, 3, true, Rng.new(self.seed):derive("fallback." .. ordinal))
end

function ExpeditionRun:_place_hazard(plan)
  if not plan.hazard then return end
  local world, player = self.session.state.world, self.session.state.player
  -- A small controlled water patch makes the existing conductivity system a
  -- tactical choice rather than a decorative arena label.
  for index = 1, 3 do
    local x, y = player.x + 3 + index, player.y - 1
    if world:is_passable(x, y) then world:set_liquid(x, y, "liquid.water.legacy", 1) end
  end
end

function ExpeditionRun:_place_reinforcement(plan)
  if not plan.reinforcement then return end
  local state = self.session.state
  state.reinforcement_state = { enabled = true }
  local placed
  for attempt = 1, 12 do
    local rng = Rng.new(self.seed):derive("reinforcement." .. plan.index .. "." .. attempt)
    placed = ReinforcementGeneration.place(self.session, rng, "expedition." .. plan.index)
    if placed then break end
  end
  local source = placed and state.world:get_object(placed.source_object_id) or nil
  if source then
    -- Let the source warn/resolve via the real finite lifecycle on the first
    -- combat actions, rather than spawning a bespoke scripted wave.
    source.reinforcement_state = "armed"
    source.reinforcement_delay = 2
    source.reinforcement_just_armed = true
  end
end

function ExpeditionRun:_begin_encounter(index)
  local plan = self.plan[index]
  if not plan then return self:_begin_boss() end
  local session, state = self.session, self.session.state
  session:start_expedition_chamber(plan.profile_id, plan.tier_id, Rng.new(self.seed):derive("chamber." .. index).seed, {
    topology = plan.topology,
    index = index,
  })
  if index == 1 then self:_configure_player() end
  state.expedition.encounter_index = index
  state.expedition.stage = plan.stage
  state.stage = plan.stage
  state.reinforcement_state = { enabled = false }
  self:_place_hazard(plan)
  for enemy_index, spec in ipairs(plan.enemies) do
    local point = self:_open_near(spec.spawn_group, enemy_index)
    local enemy = session:_make_enemy(spec.enemy_id, point, { scrap_award = true })
    enemy.elite = spec.elite or enemy.elite
    -- Modest late scaling makes role composition the main escalation and
    -- avoids the HP-sponge trap. Elite durability remains physically local.
    enemy.health = 1 + math.floor((plan.stage - 1) / 2) + (enemy.elite and 2 or 0)
    state.enemies[#state.enemies + 1] = enemy
  end
  self:_place_reinforcement(plan)
  state.expedition.current_encounter_id = plan.id
  state.expedition.current_topology = plan.topology
  session:_log(string.upper(plan.template.display_name) .. " — " .. string.upper(plan.topology) .. " CHAMBER.")
  session:refresh_derived_player_stats()
  session:refresh_visibility()
  session:validate_physical_ownership()
  self.on_event({ type = "expedition_encounter", value = plan })
  return { applied = true, code = "encounter_started", plan = plan }
end

function ExpeditionRun:_begin_boss()
  local state = self.session.state
  state.reinforcement_state = { enabled = false }
  self.session:start_expedition_boss_chamber(Rng.new(self.seed):derive("boss.chamber").seed)
  -- Existing boss bodies remain authoritative. A small stage-local health
  -- bump keeps the finale from evaporating without globally changing bosses.
  if state.boss then state.boss.health = (state.boss.health or 1) + 3 end
  state.expedition.stage = 3
  state.expedition.current_topology = "boss"
  self.on_event({ type = "expedition_boss" })
  return { applied = true, code = "boss_started" }
end

function ExpeditionRun:available_passives()
  return Content.modifier_registry:enabled(self.profile)
end

local function character_uses_projectiles(character)
  return character.ammo_family ~= nil
end

local function character_uses_melee(character)
  return character.id == "expedition.bruiser"
end

-- Rewards may still surprise the player, and cross-archetype effects remain
-- legal, but avoid filling a choice with effects that literally cannot apply
-- to this character's current basic kit. If a future character has an unusual
-- hybrid kit, the fallback intentionally keeps the full pool available.
function ExpeditionRun:compatible_passives()
  local all, compatible = self:available_passives(), {}
  for _, passive in ipairs(all) do
    local tags = {}; for _, tag in ipairs(passive.tags or {}) do tags[tag] = true end
    local needs_projectile = tags.projectile or tags.ranged or tags.ammo or tags.magazine
    local needs_melee = tags.melee
    if (not needs_projectile or character_uses_projectiles(self.character))
      and (not needs_melee or character_uses_melee(self.character)) then
      compatible[#compatible + 1] = passive
    end
  end
  return #compatible >= 3 and compatible or all
end

function ExpeditionRun:_reward_rng(label)
  return Rng.new(self.seed):derive("reward." .. tostring(label))
end

function ExpeditionRun:reward_options(count, purpose)
  local pool = self:compatible_passives()
  local exp = self.session.state.expedition
  local rng = self:_reward_rng((purpose or "choice") .. "." .. exp.encounter_index .. "." .. (exp.level or 1) .. "." .. (exp.pending_level_ups or 0))
  local stacks, later = exp.passive_stacks, exp.encounter_index >= 4
  local function pick(intent, excluded)
    local candidates = {}
    for _, passive in ipairs(pool) do
      local owned = stacks[passive.id] or 0
      local matches = intent == "wildcard" or (intent == "reinforce" and owned > 0) or (intent == "discover" and owned == 0)
      if matches and not excluded[passive.id] then candidates[#candidates + 1] = passive end
    end
    if #candidates == 0 then
      for _, passive in ipairs(pool) do if not excluded[passive.id] then candidates[#candidates + 1] = passive end end
    end
    if #candidates == 0 then candidates = pool end
    local total = 0
    for _, passive in ipairs(candidates) do
      local owned = stacks[passive.id] or 0
      local weight = passive.pool.weight
      if later and owned > 0 and intent ~= "discover" then
        weight = weight * (ExpeditionRun.OWNED_PICK_WEIGHT + math.min(0.7, math.max(0, owned - 1) * ExpeditionRun.OWNED_STACK_MOMENTUM))
      end
      total = total + weight
    end
    local roll, cumulative = rng:float() * total, 0
    for _, passive in ipairs(candidates) do
      local owned = stacks[passive.id] or 0
      local weight = passive.pool.weight
      if later and owned > 0 and intent ~= "discover" then
        weight = weight * (ExpeditionRun.OWNED_PICK_WEIGHT + math.min(0.7, math.max(0, owned - 1) * ExpeditionRun.OWNED_STACK_MOMENTUM))
      end
      cumulative = cumulative + weight
      if roll < cumulative then return passive end
    end
    return candidates[#candidates]
  end
  local result, excluded = {}, {}
  local requested = count or 3
  if requested == 3 then
    local early = exp.encounter_index <= 2 and (exp.level or 1) <= 3
    local intents = early and { "discover", "discover", "wildcard" } or { "reinforce", "discover", "wildcard" }
    for index, intent in ipairs(intents) do
      local passive = pick(intent, excluded)
      result[index], excluded[passive.id] = passive, true
    end
  else
    for index = 1, requested do result[index] = pick(purpose == "discover" and "discover" or "wildcard", {}) end
  end
  return result
end

function ExpeditionRun:add_passive(passive_id)
  local passive = assert(Content.passive(passive_id), "Unknown Expedition passive")
  local stacks = self.session.state.expedition.passive_stacks
  stacks[passive.id] = (stacks[passive.id] or 0) + 1
  self.session:refresh_derived_player_stats()
  self.session:_log(passive.name:upper() .. " ×" .. stacks[passive.id] .. ".")
  self.on_event({ type = "expedition_pickup", value = { passive = passive, count = stacks[passive.id] } })
  return { applied = true, passive = passive, count = stacks[passive.id] }
end

function ExpeditionRun:_chest_cost()
  local exp = self.session.state.expedition
  -- Three escalating caches deliberately cannot all be bought in an ordinary
  -- run.  Saving cash for the late cache is a real alternative to taking the
  -- earlier reinforcement spike.
  return 6 + (exp.chests_opened or 0) * 16 + (exp.stage or 1) * 2
end

function ExpeditionRun:_unlock(id)
  local result = self.on_unlock(id)
  if result and result.applied then self.newly_unlocked[#self.newly_unlocked + 1] = id end
  return result
end

function ExpeditionRun:_check_unlocks()
  local exp = self.session.state.expedition
  if exp.stage >= 2 then self:_unlock("expedition.unlock.character.conductor"); self:_unlock("expedition.unlock.item.arc_relay") end
  if (exp.component_breaks or 0) >= 5 then self:_unlock("expedition.unlock.item.rupture_core") end
end

function ExpeditionRun:_combat_is_clear()
  local state = self.session.state
  if state.phase ~= "combat" or #state.enemies > 0 or state.boss then return false end
  for _, object in ipairs(state.world:list_objects()) do
    if object.interaction_role == "reinforcement" and not object.destroyed
      and (object.reinforcement_state == "armed" or object.reinforcement_state == "idle") then
      return false
    end
  end
  return true
end

function ExpeditionRun:_open_level_reward()
  local exp = self.session.state.expedition
  if (exp.pending_level_ups or 0) <= 0 then return nil end
  self.pending_reward = self:reward_options(3, "level")
  self.pending_reward_context = "level"
  self.on_event({ type = "expedition_level_up", value = { level = exp.level, pending = exp.pending_level_ups } })
  return "expedition_reward"
end

function ExpeditionRun:_complete_encounter()
  local exp, index = self.session.state.expedition, self.session.state.expedition.encounter_index
  local plan = self.plan[index]
  exp.clears = (exp.clears or 0) + 1
  local heal = RunModifiers.value(self.session.state, self.session.registry, "clear_heal")
  if heal > 0 then
    local player = self.session.state.player
    player.health = math.min(player.max_health, player.health + heal)
  end
  self:_check_unlocks()
  self.session:_log("ENCOUNTER CLEAR — " .. string.upper(plan.kind) .. ".")
  -- Elite value is immediate even when its scheduled reward is a paused
  -- choice/cache. This creates two earned spikes without adding more menus.
  if plan.elite then self:add_passive(self:reward_options(1, "elite")[1].id) end
  local reward_kind = plan.reward_kind
  if reward_kind == "cache" then
    self.pending_chest = { cost = self:_chest_cost(), options = self:reward_options(1, "cache") }
    return "expedition_chest"
  elseif reward_kind == "random" then
    local pickup = self:add_passive(self:reward_options(1, "random")[1].id)
    self:_begin_encounter(index + 1)
    return { code = "expedition_next_encounter", pickup = pickup }
  end
  local pickup = nil
  self:_begin_encounter(index + 1)
  return { code = "expedition_next_encounter", pickup = pickup }
end

function ExpeditionRun:choose_reward(index)
  local passive = self.pending_reward and self.pending_reward[index]
  if not passive then return { applied = false, code = "invalid_reward", reason = "Reward choice is unavailable" } end
  local result = self:add_passive(passive.id)
  self.pending_reward = nil
  local context = self.pending_reward_context
  self.pending_reward_context = nil
  if context == "level" then
    local exp = self.session.state.expedition
    exp.pending_level_ups = math.max(0, (exp.pending_level_ups or 0) - 1)
    if exp.pending_level_ups > 0 then
      self:_open_level_reward()
      return { applied = true, pickup = result, code = "level_reward_chosen", next_reward = true }
    end
    if self:_combat_is_clear() then
      local next_result = self:_complete_encounter()
      return { applied = true, pickup = result, code = "level_reward_chosen", transition = next_result }
    end
    return { applied = true, pickup = result, code = "level_reward_chosen" }
  end
  self:_begin_encounter(self.session.state.expedition.encounter_index + 1)
  return { applied = true, pickup = result, code = "reward_chosen" }
end

function ExpeditionRun:open_chest()
  local chest, exp = self.pending_chest, self.session.state.expedition
  if not chest then return { applied = false, code = "no_chest", reason = "No chest is available" } end
  if exp.currency < chest.cost then return { applied = false, code = "insufficient_currency", reason = "Not enough SCRAP" } end
  exp.currency = exp.currency - chest.cost
  self.session.state.scrap = exp.currency
  exp.chests_opened = (exp.chests_opened or 0) + 1
  local result = self:add_passive(chest.options[1].id)
  self.pending_chest = nil
  self:_begin_encounter(exp.encounter_index + 1)
  return { applied = true, pickup = result, code = "chest_opened" }
end

function ExpeditionRun:skip_chest()
  if not self.pending_chest then return { applied = false, code = "no_chest" } end
  self.pending_chest = nil
  self:_begin_encounter(self.session.state.expedition.encounter_index + 1)
  return { applied = true, code = "chest_skipped" }
end

function ExpeditionRun:_finish(victory)
  if self.completed then return self.summary_data end
  self.completed = true
  if victory then self:_unlock("expedition.unlock.character.demolitionist") end
  self:_check_unlocks()
  local stacks = {}
  for id, count in sorted_pairs(self.session.state.expedition.passive_stacks) do
    local passive = Content.passive(id)
    stacks[#stacks + 1] = { id = id, display_name = passive.name, count = count }
  end
  table.sort(stacks, function(a, b) return a.count == b.count and a.id < b.id or a.count > b.count end)
  self.summary_data = {
    victory = victory,
    character = self.character.display_name,
    stage = self.session.state.expedition.stage,
    encounter = self.session.state.expedition.encounter_index,
    kills = self.session.state.expedition.kills or 0,
    component_breaks = self.session.state.expedition.component_breaks or 0,
    currency = self.session.state.expedition.currency or 0,
    level = self.session.state.expedition.level or 1,
    total_xp = self.session.state.expedition.total_xp or 0,
    passive_stacks = stacks,
    new_unlocks = self.newly_unlocked,
  }
  return self.summary_data
end

function ExpeditionRun:build_summary()
  local exp, active_weapon, active_ability = self.session.state.expedition, self.session:expedition_active_weapon(), self.session:expedition_active_ability()
  local entries = {}
  for id, count in sorted_pairs(exp.passive_stacks) do
    local passive = Content.passive(id)
    local preview = require("src.expedition.modifiers").stack_preview(passive, count)
    entries[#entries + 1] = { id = id, display_name = passive.name, count = count, description = passive.description, current_effect = preview.current, next_effect = preview.next }
  end
  table.sort(entries, function(a, b) return a.display_name < b.display_name end)
  return {
    character = self.character,
    stage = exp.stage, encounter = exp.encounter_index, currency = exp.currency,
    level = exp.level or 1, xp = exp.xp or 0,
    xp_to_next = (exp.xp_thresholds or {})[exp.level_threshold_index or 1],
    weapon = active_weapon and active_weapon.ability, ability = active_ability and active_ability.ability,
    reserve_ammo = copy_map(exp.reserve_ammo), passives = entries,
  }
end

function ExpeditionRun:turn(input)
  if self.completed then return "expedition_complete" end
  if self.pending_reward or self.pending_chest then return "expedition_paused" end
  local result = self.session:turn(input)
  if self.session.state.ended == "expedition_dead" then
    self:_finish(false)
    return "expedition_dead"
  elseif self.session.state.ended == "expedition_victory" then
    self:_finish(true)
    return "expedition_victory"
  end
  if (self.session.state.expedition.pending_level_ups or 0) > 0 then return self:_open_level_reward() end
  if self:_combat_is_clear() then return self:_complete_encounter() end
  return result
end

return ExpeditionRun
