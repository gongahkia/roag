-- Disposable combat-first Expedition controller.  It deliberately reuses the
-- authoritative Session combat/world systems while keeping progression,
-- rewards and death separate from Campaign/Sandbox persistence.
local Session = require("src.simulation.session")
local Rng = require("src.rng")
local Grid = require("src.world.grid")
local ReinforcementGeneration = require("src.generation.reinforcements")
local RunModifiers = require("src.simulation.run_modifiers")
local Content = require("src.expedition.content")

local ExpeditionRun = {}
ExpeditionRun.__index = ExpeditionRun

ExpeditionRun.REGULAR_ENCOUNTERS = 12
-- The first budget already supports the most demanding early grammar
-- constraint (two Crossfire ranged enemies or a Hazard controller+rusher).
-- Escalation comes chiefly from composition and arena pressure rather than
-- turning the initial encounters into one-enemy chores.
ExpeditionRun.THREAT_BUDGETS = { 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }
ExpeditionRun.REWARD_ENCOUNTERS = { [1] = "choice", [3] = "choice", [4] = "choice", [5] = "chest", [6] = "choice", [8] = "choice", [9] = "choice", [10] = "chest", [12] = "choice" }
ExpeditionRun.ARCHETYPE_ORDER = {
  "expedition.encounter.swarm",
  "expedition.encounter.crossfire",
  "expedition.encounter.pincer",
  "expedition.encounter.hazard",
  "expedition.encounter.elite_hunt",
  "expedition.encounter.reinforcement_pressure",
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
  while spent < budget do
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

local function encounter_plan(seed, index)
  local stage = stage_for(index)
  -- Cycling with one deterministic seed offset guarantees all six grammar
  -- families appear and prevents repetition streaks while still changing run
  -- order by seed.
  local rng = Rng.new(seed):derive("expedition.encounter." .. index)
  local offset = Rng.new(seed):derive("expedition.archetype_order"):int(#ExpeditionRun.ARCHETYPE_ORDER)
  local template_id = ExpeditionRun.ARCHETYPE_ORDER[((index - 1 + offset - 1) % #ExpeditionRun.ARCHETYPE_ORDER) + 1]
  local template = assert(Content.encounter(template_id))
  local roles, spent = role_sequence(template, ExpeditionRun.THREAT_BUDGETS[index], rng:derive("roles"))
  local enemies = {}
  for enemy_index, role in ipairs(roles) do
    local choices = Content.ENEMY_BY_ROLE[role]
    enemies[#enemies + 1] = {
      role = role,
      enemy_id = choices[rng:derive("enemy." .. enemy_index):int(#choices)],
      spawn_group = template.kind == "pincer" and (enemy_index % 2 == 0 and "east" or "west")
        or template.kind == "crossfire" and (enemy_index % 2 == 0 and "north" or "south")
        or "ring",
      elite = template.elite and enemy_index == 1,
    }
  end
  return {
    index = index,
    stage = stage,
    id = template.id,
    kind = template.kind,
    template = template,
    profile_id = profile_for(template, stage, rng:derive("profile")),
    tier_id = STAGE_TIER[stage],
    budget = ExpeditionRun.THREAT_BUDGETS[index],
    actual_budget = spent,
    enemies = enemies,
    reward_kind = ExpeditionRun.REWARD_ENCOUNTERS[index],
    elite = template.elite,
    hazard = template.hazard,
    reinforcement = template.reinforcement,
  }
end

function ExpeditionRun.plan(seed)
  local result = {}
  for index = 1, ExpeditionRun.REGULAR_ENCOUNTERS do result[index] = encounter_plan(seed, index) end
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
  session:refresh_derived_player_stats()
  player.health = player.max_health
end

function ExpeditionRun:_open_near(group, ordinal)
  local state, player, world = self.session.state, self.session.state.player, self.session.state.world
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
  session:start_biome_tier(plan.profile_id, plan.tier_id, Rng.new(self.seed):derive("arena." .. index).seed, nil, {
    discovery_state = { enabled = false }, reinforcement_state = { enabled = false },
  })
  if index == 1 then self:_configure_player() end
  state.targets, state.enemies, state.bullets, state.area_attacks = {}, {}, {}, {}
  state.bombs, state.flares, state.torches, state.corpses = {}, {}, {}, {}
  state.ammo, state.boss, state.exit = nil, nil, nil
  state.phase, state.ended = "combat", nil
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
  session:refresh_derived_player_stats()
  session:refresh_visibility()
  session:validate_physical_ownership()
  self.on_event({ type = "expedition_encounter", value = plan })
  return { applied = true, code = "encounter_started", plan = plan }
end

function ExpeditionRun:_begin_boss()
  local state = self.session.state
  state.reinforcement_state = { enabled = false }
  self.session:start_boss()
  -- Existing boss bodies remain authoritative. A small stage-local health
  -- bump keeps the finale from evaporating without globally changing bosses.
  if state.boss then state.boss.health = (state.boss.health or 1) + 3 end
  state.expedition.stage = 3
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

function ExpeditionRun:reward_options(count)
  local pool = self:compatible_passives()
  local rng = self:_reward_rng("choice." .. self.session.state.expedition.encounter_index)
  -- Duplicates are deliberately allowed both inside choices and across a run.
  local result = {}
  for index = 1, count or 3 do result[#result + 1] = pool[rng:int(#pool)] end
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
  return 4 + (exp.chests_opened or 0) * 3 + (exp.stage or 1) * 2
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
  local reward_kind = plan.reward_kind
  if reward_kind == "choice" then
    self.pending_reward = self:reward_options(3)
    return "expedition_reward"
  elseif reward_kind == "chest" then
    self.pending_chest = { cost = self:_chest_cost(), options = self:reward_options(1) }
    return "expedition_chest"
  end
  self:_begin_encounter(index + 1)
  return "expedition_next_encounter"
end

function ExpeditionRun:choose_reward(index)
  local passive = self.pending_reward and self.pending_reward[index]
  if not passive then return { applied = false, code = "invalid_reward", reason = "Reward choice is unavailable" } end
  local result = self:add_passive(passive.id)
  self.pending_reward = nil
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
  if self.session.state.phase == "combat" and #self.session.state.enemies == 0 and not self.session.state.boss then
    -- A finite reinforcement source is part of encounter completion until it
    -- has fired or been destroyed. Normal corpse visuals do not block flow.
    for _, object in ipairs(self.session.state.world:list_objects()) do
      if object.interaction_role == "reinforcement" and not object.destroyed
        and (object.reinforcement_state == "armed" or object.reinforcement_state == "idle") then
        return result
      end
    end
    return self:_complete_encounter()
  end
  return result
end

return ExpeditionRun
