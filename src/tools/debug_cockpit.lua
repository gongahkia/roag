-- Deterministic, read-only developer diagnostics. These helpers deliberately
-- construct isolated state and never open an active run, profile, or archive.
local LegacyContent = require("src.content.legacy")
local Registry = require("src.content.registry")
local ExpeditionContent = require("src.expedition.content")
local ExpeditionRun = require("src.expedition.run")
local ModifierDefinitions = require("src.expedition.modifiers")
local ModifierSimulator = require("src.expedition.modifier_simulator")
local MetaProfile = require("src.persistence.meta_profile")
local Presentation = require("src.rendering.presentation")
local Session = require("src.simulation.session")
local Grid = require("src.world.grid")
local World = require("src.world.world")

local DebugCockpit = {}

local function count(values)
  local total = 0
  for _ in pairs(values or {}) do total = total + 1 end
  return total
end

local function as_number(value, default)
  local number = tonumber(value)
  return number and number == number and number ~= math.huge and number ~= -math.huge and number or default
end

local function csv_set(value, fallback)
  local result = {}
  for token in tostring(value or ""):gmatch("[^,%s]+") do result[token] = true end
  if next(result) == nil then
    for _, token in ipairs(fallback or {}) do result[token] = true end
  end
  return result
end

local function actor_summary(actor)
  if type(actor) ~= "table" then return nil end
  return {
    actor_id = actor.actor_id,
    content_id = actor.content_id,
    kind = actor.kind,
    x = actor.x,
    y = actor.y,
    health = actor.health,
    max_health = actor.max_health or actor.base_max_health,
  }
end

local function event_summary(event)
  local value = event.value or {}
  return {
    type = event.type,
    x = value.x,
    y = value.y,
    amount = value.amount,
    cause = value.cause,
    direction = value.direction,
    source = value.source,
    target = actor_summary(value.target),
    source_actor = actor_summary(value.source_actor),
  }
end

local function open_world(registry, owner)
  local passable = {}
  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do passable[Grid.key(x, y)] = true end
  end
  return World.new(registry, "forest", passable, owner)
end

function DebugCockpit.context()
  local registry = Registry.load()
  ExpeditionContent.validate(registry)
  local modifiers = assert(ModifierDefinitions.load({ registry = registry }))
  return { registry = registry, modifiers = modifiers }
end

function DebugCockpit.doctor()
  local context = DebugCockpit.context()
  local profile = MetaProfile.new()
  local reactive, static = 0, 0
  for _, definition in ipairs(context.modifiers:list()) do
    if #(definition.hooks or {}) > 0 then reactive = reactive + 1 end
    if #(definition.static_effects or {}) > 0 then static = static + 1 end
  end
  return {
    ok = true,
    content = {
      abilities = count(context.registry.abilities),
      components = count(context.registry.components),
      enemies = count(context.registry.enemies),
      materials = count(context.registry.materials),
      construction_recipes = count(context.registry.construction_recipes),
      characters = #ExpeditionContent.CHARACTERS,
      encounter_archetypes = #ExpeditionContent.ENCOUNTERS,
    },
    modifiers = {
      definitions = #context.modifiers:list(),
      enabled_for_fresh_profile = #context.modifiers:enabled(profile),
      reactive = reactive,
      static = static,
    },
    contracts = {
      simulation_isolated = true,
      active_save_opened = false,
      meta_profile_opened = false,
      modifier_registry_source = "content/expedition/modifiers/manifest.json",
    },
  }
end

function DebugCockpit.modifier(options)
  options = options or {}
  local context = DebugCockpit.context()
  local id = options.id or "expedition.passive.arc_relay"
  local definition = assert(context.modifiers:get(id), "Unknown Expedition modifier '" .. tostring(id) .. "'")
  local stack_count = math.max(1, math.floor(as_number(options.stacks, 1)))
  local trigger = options.trigger or ((definition.hooks or {})[1] and definition.hooks[1].trigger) or "on_attack"
  local result = ModifierSimulator.run({
    definition = definition,
    modifier_registry = context.modifiers,
    stack_count = stack_count,
    trigger = trigger,
    character_id = options.character or "expedition.gunner",
    attack_tags = csv_set(options.tags, { "projectile" }),
    capabilities = csv_set(options.capabilities, {}),
    target = options.target ~= "false",
    target_hp = as_number(options.target_hp, 8),
    damage_type = options.damage_type,
    label = "DEBUG " .. definition.name:upper(),
  })
  return {
    definition = {
      id = definition.id,
      name = definition.name,
      category = definition.category,
      tags = definition.tags,
      source_file = context.modifiers:filename_for(definition.id),
    },
    input = {
      stacks = stack_count,
      trigger = trigger,
      attack_tags = csv_set(options.tags, { "projectile" }),
      capabilities = csv_set(options.capabilities, {}),
    },
    stack_preview = ModifierDefinitions.stack_preview(definition, stack_count),
    result = result,
  }
end

function DebugCockpit.expedition(options)
  options = options or {}
  local context = DebugCockpit.context()
  local profile = MetaProfile.new()
  -- Diagnostics deliberately make every prototype character selectable, but
  -- this profile exists only in memory and cannot change account unlocks.
  for _, character in ipairs(ExpeditionContent.CHARACTERS) do
    profile.expedition_unlock_ids[#profile.expedition_unlock_ids + 1] = character.unlock
  end
  table.sort(profile.expedition_unlock_ids)
  local character_id = options.character or "expedition.gunner"
  local run = ExpeditionRun.new({
    seed = math.floor(as_number(options.seed, 1337)),
    character_id = character_id,
    meta_profile = profile,
    registry = context.registry,
  })
  local offered = run:reward_options(3)
  local choices = {}
  for index, modifier in ipairs(offered) do
    choices[index] = {
      id = modifier.id,
      name = modifier.name,
      category = modifier.category,
      weight = modifier.pool.weight,
      preview = ModifierDefinitions.stack_preview(modifier, 1),
    }
  end
  return {
    seed = run.seed,
    character = { id = run.character.id, name = run.character.display_name, base_hp = run.character.base_hp },
    encounter_plan = run.plan,
    initial_reward_preview = choices,
    active_encounter = {
      index = run.session.state.expedition.encounter_index,
      stage = run.session.state.expedition.stage,
      hostiles = #(run.session.state.enemies or {}),
      profile_id = run.session.state.settings and run.session.state.settings.biome_id,
    },
    isolated = true,
  }
end

function DebugCockpit.bomb_self(options)
  options = options or {}
  local events = {}
  local context = DebugCockpit.context()
  local session = Session.new({
    seed = math.floor(as_number(options.seed, 44001)),
    registry = context.registry,
    emit = function(event) events[#events + 1] = event end,
  })
  session:start_run(LegacyContent.classes[1], LegacyContent.boons[1])
  local state = session.state
  state.world = open_world(session.registry, state)
  state.enemies, state.targets, state.bullets, state.bombs = {}, {}, {}, {}
  state.flares, state.torches, state.area_attacks, state.effects = {}, {}, {}, {}
  state.ammo, state.exit, state.corpses = nil, nil, {}
  local player = state.player
  player.x, player.y = 10, 10
  state.bombs = { { kind = "bomb", x = 10, y = 10, radius = 1, fuse = 1, source_actor = player } }

  local before = player.health
  session:_update_bombs()
  local presentation = Presentation.new()
  presentation:reset(session)
  for _, event in ipairs(events) do
    if event.type == "actor_hit" then presentation:actor_hit(event.value) end
  end
  local reaction = presentation.reactions[player]
  local valid = pcall(session.validate_world, session)
  local summarized_events = {}
  for index, event in ipairs(events) do summarized_events[index] = event_summary(event) end
  return {
    scenario = "bomb-self",
    health_before = before,
    health_after = player.health,
    player = actor_summary(player),
    events = summarized_events,
    presentation = {
      damage_numbers = #presentation.damage_numbers,
      hit_stop_requested = presentation.hit_stop_remaining > 0,
      recoil = reaction and { dx = reaction.dx, dy = reaction.dy } or nil,
    },
    world_valid = valid,
  }
end

function DebugCockpit.determinism(options)
  options = options or {}
  local Json = require("src.persistence.json")
  local function encoded(value) return assert(Json.encode(value)) end
  local bomb_a, bomb_b = DebugCockpit.bomb_self(options), DebugCockpit.bomb_self(options)
  local expedition_a, expedition_b = DebugCockpit.expedition(options), DebugCockpit.expedition(options)
  return {
    bomb_self = encoded(bomb_a) == encoded(bomb_b),
    expedition_plan = encoded(expedition_a) == encoded(expedition_b),
    seed = math.floor(as_number(options.seed, 1337)),
  }
end

return DebugCockpit
