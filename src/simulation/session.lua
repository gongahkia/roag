-- Authoritative current-run simulation. This module intentionally has no
-- dependency on LÖVE so it can be created and stepped by tests or tools.
local Content = require("src.content.legacy")
local Body = require("src.body.body")
local Component = require("src.body.component")
local ComponentFactory = require("src.body.component_factory")
local Registry = require("src.content.registry")
local BodyDamage = require("src.simulation.body_damage")
local Locomotion = require("src.simulation.locomotion")
local Inventory = require("src.inventory.inventory")
local PhysicalItem = require("src.inventory.physical_item")
local Tool = require("src.inventory.tool")
local CorpseLootGrid = require("src.inventory.corpse_loot_grid")
local Corpse = require("src.world.corpse")
local Salvage = require("src.simulation.salvage")
local Reconstruction = require("src.simulation.reconstruction")
local EnvironmentDamage = require("src.simulation.environment_damage")
local Force = require("src.simulation.force")
local Hazards = require("src.simulation.hazards")
local Impact = require("src.simulation.impact")
local Fire = require("src.simulation.fire")
local Liquid = require("src.simulation.liquid")
local Electricity = require("src.simulation.electricity")
local Gas = require("src.simulation.gas")
local Interaction = require("src.simulation.interaction")
local Factions = require("src.simulation.factions")
local ReinforcementSimulation = require("src.simulation.reinforcements")
local Economy = require("src.simulation.economy")
local RunModifiers = require("src.simulation.run_modifiers")
local FallenRecurrence = require("src.simulation.fallen_recurrence")
local EnvironmentObjects = require("src.generation.environment_objects")
local HazardGeneration = require("src.generation.hazards")
local LiquidGeneration = require("src.generation.liquids")
local GasGeneration = require("src.generation.gases")
local FireGeneration = require("src.generation.fires")
local PoweredDevices = require("src.generation.powered_devices")
local DiscoveryGeneration = require("src.generation.discoveries")
local ReinforcementGeneration = require("src.generation.reinforcements")
local LandmarkGeneration = require("src.generation.landmarks")
local Generator = require("src.generation.map")
local Grid = require("src.world.grid")
local World = require("src.world.world")
local Rng = require("src.rng")
local RouteDefinitions = require("src.routes.definitions")
local RouteGraph = require("src.routes.graph")
local WorldTopology = require("src.campaign.world_topology")
local Building = require("src.construction.building")
local Loadout = require("src.simulation.loadout")

local Session = {}
Session.__index = Session

-- These values describe the enduring body/cargo/progression layer. In a
-- legacy standalone Session they remain ordinary fields. Campaign supplies a
-- single backing table, so the simulator has references to campaign state
-- without maintaining second mutable copies of the active body, inventory,
-- SCRAP, or account snapshot.
local CAMPAIGN_STATE_FIELDS = {
  stage = true, score = true, scrap = true, charms = true, curse = true, curse_id = true,
  curse_bag = true, curse_options = true, route = true, run = true, inventory = true,
  run_id = true, meta_snapshot = true, meta_reward_events = true,
  fallen_recurrence = true, death_pending_archive = true,
  discovery_state = true, reinforcement_state = true,
  loadout = true,
}

local function attach_campaign_state(state, campaign_state)
  if not campaign_state then return state end
  assert(type(campaign_state) == "table", "Campaign state must be a table")
  for name in pairs(CAMPAIGN_STATE_FIELDS) do
    if campaign_state[name] == nil then campaign_state[name] = state[name] end
    state[name] = nil
  end
  return setmetatable(state, {
    __index = function(_, name)
      if CAMPAIGN_STATE_FIELDS[name] then return campaign_state[name] end
      return nil
    end,
    __newindex = function(target, name, value)
      if CAMPAIGN_STATE_FIELDS[name] then
        campaign_state[name] = value
      else
        rawset(target, name, value)
      end
    end,
  })
end

local DIRECTIONS = {
  w = { 0, 1, "N" },
  a = { -1, 0, "W" },
  s = { 0, -1, "S" },
  d = { 1, 0, "E" },
  nw = { -1, 1, "NW" },
  ne = { 1, 1, "NE" },
  sw = { -1, -1, "SW" },
  se = { 1, -1, "SE" },
}
-- Clockwise order gives the scatter weapon a predictable, symmetric fan
-- without consuming run RNG or making save/replay outcomes drift.
local DIRECTION_RING = { "w", "ne", "d", "se", "s", "sw", "a", "nw" }
local SELF_DESTRUCT_ABILITY = "ability.explosive.self_destruct"
local BASIC_PROJECTILE_ABILITY = "ability.weapon.projectile.basic"
local ARCANE_BURST_ABILITY = "ability.arcane.burst"
local ELECTRICAL_DISCHARGE_ABILITY = "ability.electrical.discharge"
local LOCOMOTION_ABILITY = Locomotion.ABILITY_ID
local PLAYER_ACTOR_ID = "actor.player.legacy"
-- OW-01 uses the existing floor generators as zone profiles. These are
-- deliberately a small mapping rather than macro geography; OW-02 will own
-- which profile a neighboring coordinate receives.
local CAMPAIGN_ZONE_PROFILES = {
  ["zone_profile.legacy.forest"] = { biome_id = "biome.legacy.forest", tier_id = "tier.legacy.1", ecology_profile_id = "ecology.surface.feral" },
  ["zone_profile.legacy.cave"] = { biome_id = "biome.legacy.cave", tier_id = "tier.legacy.2", ecology_profile_id = "ecology.cave.feral_cult" },
  -- Compatibility tiers only: Z/profile is the campaign authority, not the
  -- legacy route depth encoded by these existing Cave generators.
  ["zone_profile.legacy.deep_cave"] = { biome_id = "biome.legacy.cave", tier_id = "tier.legacy.3", ecology_profile_id = "ecology.cave.feral_cult" },
  ["zone_profile.legacy.dungeon"] = { biome_id = "biome.legacy.dungeon", tier_id = "tier.legacy.3", ecology_profile_id = "ecology.ruin.machine_feral" },
  ["zone_profile.legacy.reactor"] = { biome_id = "biome.legacy.reactor", tier_id = "tier.legacy.3", ecology_profile_id = "ecology.reactor.machine" },
  -- Semantic campaign profiles keep the reusable legacy generators and tiers
  -- as a compatibility implementation detail rather than route progression.
  ["zone_profile.world.dungeon"] = { biome_id = "biome.legacy.dungeon", tier_id = "tier.legacy.3", ecology_profile_id = "ecology.ruin.machine_feral" },
  ["zone_profile.world.deep_dungeon"] = { biome_id = "biome.legacy.dungeon", tier_id = "tier.legacy.3", ecology_profile_id = "ecology.ruin.machine_feral" },
  ["zone_profile.world.reactor"] = { biome_id = "biome.legacy.reactor", tier_id = "tier.legacy.3", ecology_profile_id = "ecology.reactor.machine" },
  ["zone_profile.world.reactor_core"] = { biome_id = "biome.legacy.reactor", tier_id = "tier.legacy.3", ecology_profile_id = "ecology.reactor.machine" },
  ["zone_profile.world.boss_lair"] = { biome_id = "biome.legacy.forest", tier_id = "tier.legacy.3", ecology_profile_id = "ecology.boss_lair" },
}
local ENEMY_CONTENT_IDS = {
  bomber = "enemy.legacy.bomber",
  cultist = "enemy.legacy.cultist",
}
local META_MODIFIER_KEYS = {
  max_health = true, dash_cooldown = true, charm_slots = true, inventory_rows = true, starting_scrap = true,
  melee_force = true, projectile_damage = true,
}

local function clamp(value, minimum, maximum)
  return math.max(minimum, math.min(maximum, value))
end

local function entity(kind, x, y, values)
  local result = { kind = kind, x = x, y = y }
  for name, value in pairs(values or {}) do
    result[name] = value
  end
  return result
end

local function remove(values, index)
  local value = values[index]
  table.remove(values, index)
  return value
end

local function copy_plain(value)
  local result = {}
  for name, field in pairs(value or {}) do
    local kind = type(field)
    assert(kind == "string" or kind == "number" or kind == "boolean" or field == nil,
      "Active-run entity state must be plain scalar data")
    result[name] = field
  end
  return result
end

local function copy_meta_snapshot(snapshot)
  local result = { unlocked_research_ids = {}, unlock_ids = {}, modifiers = {}, discovered_discovery_ids = {} }
  local seen_research, seen_unlocks, seen_discoveries = {}, {}, {}
  for _, id in ipairs(snapshot and snapshot.unlocked_research_ids or {}) do
    assert(type(id) == "string" and id:match("^research%.[a-z0-9_%.]+$") and not seen_research[id], "Run meta snapshot research ID is invalid")
    seen_research[id] = true; result.unlocked_research_ids[#result.unlocked_research_ids + 1] = id
  end
  for _, id in ipairs(snapshot and snapshot.unlock_ids or {}) do
    assert(type(id) == "string" and id:match("^unlock%.[a-z0-9_%.]+$") and not seen_unlocks[id], "Run meta snapshot unlock ID is invalid")
    seen_unlocks[id] = true; result.unlock_ids[#result.unlock_ids + 1] = id
  end
  for _, id in ipairs(snapshot and snapshot.discovered_discovery_ids or {}) do
    assert(type(id) == "string" and id:match("^discovery%.[a-z0-9_%.]+$") and not seen_discoveries[id],
      "Run meta snapshot discovery ID is invalid")
    seen_discoveries[id] = true
    result.discovered_discovery_ids[#result.discovered_discovery_ids + 1] = id
  end
  for key, value in pairs(snapshot and snapshot.modifiers or {}) do
    assert(META_MODIFIER_KEYS[key] and type(value) == "number" and value % 1 == 0, "Run meta snapshot modifier is invalid")
    result.modifiers[key] = value
  end
  table.sort(result.unlocked_research_ids)
  table.sort(result.unlock_ids)
  table.sort(result.discovered_discovery_ids)
  return result
end

local function copy_discovery_state(value, legacy_disabled)
  if value == nil then
    -- New synthetic inspection floors may explicitly opt in with
    -- legacy_disabled=false. Ordinary constructors and old active saves stay
    -- disabled unless their serialized progression already carries the field.
    return { enabled = legacy_disabled == false, assigned_discovery_ids = {} }
  end
  assert(type(value) == "table" and type(value.enabled) == "boolean", "Discovery state is invalid")
  local assigned, seen = {}, {}
  for _, id in ipairs(value.assigned_discovery_ids or {}) do
    assert(type(id) == "string" and id:match("^discovery%.[a-z0-9_%.]+$") and not seen[id], "Discovery assignment is invalid")
    seen[id] = true
    assigned[#assigned + 1] = id
  end
  table.sort(assigned)
  return { enabled = value.enabled, assigned_discovery_ids = assigned }
end

local function copy_reinforcement_state(value, legacy_disabled)
  if value == nil then return { enabled = legacy_disabled == false } end
  assert(type(value) == "table" and type(value.enabled) == "boolean", "Reinforcement state is invalid")
  return { enabled = value.enabled }
end

local function copy_string_list(values, label)
  local result = {}
  for _, value in ipairs(values or {}) do
    assert(type(value) == "string", label .. " contains an invalid value")
    result[#result + 1] = value
  end
  return result
end

local function copy_death_pending(snapshot, registry)
  if not snapshot then return nil end
  assert(type(snapshot) == "table" and type(snapshot.source_run_id) == "string"
    and snapshot.source_run_id:match("^run:%d+$"), "Pending fallen archive source run is invalid")
  assert(type(snapshot.body) == "table", "Pending fallen archive body is invalid")
  -- Rebuild through Body to validate topology, slot compatibility, component
  -- IDs, and integrity without making the archived snapshot live ownership.
  local body = Body.from_data(registry, snapshot.body)
  local metadata = snapshot.metadata or {}
  assert(type(metadata) == "table", "Pending fallen archive metadata is invalid")
  assert(metadata.route_depth == nil or (type(metadata.route_depth) == "number" and metadata.route_depth >= 1
    and metadata.route_depth % 1 == 0), "Pending fallen archive route depth is invalid")
  for _, field in ipairs({ "route_node_id", "biome_id", "tier_id", "death_cause" }) do
    assert(metadata[field] == nil or type(metadata[field]) == "string", "Pending fallen archive metadata is invalid")
  end
  return {
    source_run_id = snapshot.source_run_id,
    body = body:to_data(),
    metadata = {
      route_node_id = metadata.route_node_id,
      biome_id = metadata.biome_id,
      tier_id = metadata.tier_id,
      route_depth = metadata.route_depth,
      route_path = copy_string_list(metadata.route_path, "Pending fallen route path"),
      charm_ids = copy_string_list(metadata.charm_ids, "Pending fallen charms"),
      research_ids = copy_string_list(metadata.research_ids, "Pending fallen research"),
      death_cause = metadata.death_cause,
    },
  }
end

local function named_content(values, name, label)
  if name == nil then return nil end
  for _, value in ipairs(values or {}) do
    if value.name == name then return value end
  end
  error("Missing " .. label .. " content '" .. tostring(name) .. "'")
end

function Session.new(options)
  options = options or {}
  local self = setmetatable({}, Session)
  self.content = options.content or Content
  self.registry = options.registry or Registry.load()
  self.route_definitions = options.route_definitions or RouteDefinitions.load()
  self.registry:validate_encounter_pools(self.route_definitions)
  self.registry:validate_discoveries(self.route_definitions)
  self.registry:validate_reinforcement_profiles(self.route_definitions)
  self.seed = options.seed or 1
  self.rng = options.rng or Rng.new(self.seed)
  self.seed = self.rng.seed
  -- In campaign mode this is a zone-scoped allocator. Legacy Session users
  -- retain their established run-local counters and ID strings.
  self.identity_allocator = options.identity_allocator
  self.campaign = options.campaign
  self.emit = options.emit or function() end
  self.meta_reward_handler = options.on_meta_reward
  local meta_snapshot = copy_meta_snapshot(options.meta_snapshot)
  local inventory = Inventory.new()
  self.state = {
    stage = 1,
    -- score remains a legacy save compatibility field only. Objective progress
    -- is per-floor on the actor; SCRAP is the sole run-wide currency.
    score = 0,
    scrap = 0,
    charms = { slots = { nil, nil, nil } },
    curse_id = nil,
    log = {},
    curse_bag = {},
    effects = {},
    electrical_effects = {},
    next_component_sequence = 1,
    next_actor_sequence = 1,
    next_corpse_sequence = 1,
    next_world_object_sequence = 1,
    next_hazard_sequence = 1,
    next_fire_sequence = 1,
    next_item_sequence = 1,
    route = nil,
    floor_seed = nil,
    transition_next = nil,
    active_service_object_id = nil,
    service_return_phase = nil,
    final_service_hub = nil,
    boss_completed = nil,
    run_id = options.run_id or ("legacy:" .. tostring(self.seed)),
    meta_snapshot = meta_snapshot,
    meta_reward_events = {},
    -- New runs opt in from start_run. A Session constructed only to restore a
    -- pre-8J active save keeps this disabled unless persisted state says
    -- otherwise, avoiding retroactive future-floor injection.
    discovery_state = copy_discovery_state(nil),
    -- As with discoveries, old active saves remain ecology-free rather than
    -- silently receiving sources on later deterministic floors.
    reinforcement_state = copy_reinforcement_state(nil),
    -- This snapshot is selected at New Run, never by consulting the archive
    -- during a live run. It therefore remains deterministic across resumes.
    fallen_recurrence = FallenRecurrence.copy_spec(options.fallen_recurrence),
    death_pending_archive = nil,
    generation_warnings = {},
    loadout = nil,
    corpses = {},
    -- A run owns the long-lived player and cargo. Floor construction is only
    -- allowed to reposition this actor and create floor-local world state.
    run = {
      player = nil,
      inventory = inventory,
    },
    -- Compatibility alias while older presentation/tests still read this
    -- location. It always points at run.inventory, never a floor inventory.
    inventory = inventory,
  }
  self.state = attach_campaign_state(self.state, options.campaign_state)
  self.component_factory = ComponentFactory.new(self.registry, self.state, self.identity_allocator)
  return self
end

function Session:_event(event_type, value)
  self.emit({ type = event_type, value = value })
end

function Session:_sound(name)
  self:_event("sound", name)
end

function Session:_log(message)
  table.insert(self.state.log, 1, message)
  while #self.state.log > 5 do
    table.remove(self.state.log)
  end
end

function Session:_open(x, y)
  return self.state.world and self.state.world:is_passable(x, y) or false
end

function Session:_blocks_vision(x, y)
  return not self.state.world or self.state.world:blocks_vision(x, y)
end

function Session:inspect_terrain(x, y)
  if not self.state.world then
    return nil, "No active world"
  end
  return self.state.world:inspect_cell(x, y)
end

function Session:describe_terrain(x, y)
  return self.state.world and self.state.world:describe_cell(x, y) or "No active world"
end

function Session:damage_terrain(x, y, spec)
  if not self.state.world then
    return { applied = false, code = "no_world", x = x, y = y, reason = "No active world" }
  end
  return EnvironmentDamage.apply_to_terrain(self.state.world, x, y, spec)
end

function Session:inspect_world_object(object_id)
  if not self.state.world then
    return nil, "No active world"
  end
  return self.state.world:inspect_object(object_id)
end

function Session:inspect_hazard(hazard_id)
  if not self.state.world then
    return nil, "No active world"
  end
  return self.state.world:inspect_hazard(hazard_id)
end

function Session:available_interactions(actor)
  return Interaction.available(self, actor or self.state.player)
end

function Session:faced_cell(actor)
  actor = actor or self.state.player
  if not self.campaign or not actor then return nil end
  local delta = DIRECTIONS[actor.direction]
  -- Campaign facing is cardinal.  Treat a diagonal direction carried by an
  -- old save as unavailable rather than quietly making directional USE scan a
  -- diagonal cell.
  if not delta or math.abs(delta[1]) + math.abs(delta[2]) ~= 1 then return nil end
  local x, y = actor.x + delta[1], actor.y + delta[2]
  if not Grid.in_bounds(x, y) then return nil end
  return { x = x, y = y, direction = actor.direction }
end

function Session:faced_interactions(actor)
  local cell = self:faced_cell(actor)
  if not cell then return {} end
  return Interaction.available_at(self, actor or self.state.player, cell.x, cell.y)
end

function Session:interact(actor, object_id, action_id)
  return Interaction.perform(self, actor or self.state.player, object_id, action_id)
end

function Session:modifier_value(key)
  return RunModifiers.value(self.state, self.registry, key)
end

function Session:has_meta_unlock(unlock_id)
  for _, id in ipairs(self.state.meta_snapshot and self.state.meta_snapshot.unlock_ids or {}) do
    if id == unlock_id then return true end
  end
  return false
end

function Session:_claim_research_reward(milestone, amount, metadata)
  local state = self.state
  local reward_id = state.run_id .. ":" .. milestone
  for _, event in ipairs(state.meta_reward_events) do
    if event.id == reward_id then return event end
  end
  local event = { id = reward_id, amount = amount, claimed = false }
  if metadata then
    event.kind = metadata.kind
    event.discovery_id = metadata.discovery_id
  end
  state.meta_reward_events[#state.meta_reward_events + 1] = event
  if not self.meta_reward_handler then
    event.claimed = true
    return event
  end
  local result = self.meta_reward_handler(event.id, event.amount, event)
  if result and (result.applied or result.code == "already_claimed" or result.code == "already_discovered") then event.claimed = true end
  return event
end

function Session:reconcile_meta_rewards()
  local settled = true
  for _, event in ipairs(self.state.meta_reward_events or {}) do
    if not event.claimed and self.meta_reward_handler then
      local result = self.meta_reward_handler(event.id, event.amount, event)
      if result and (result.applied or result.code == "already_claimed" or result.code == "already_discovered") then event.claimed = true else settled = false end
    elseif not event.claimed then
      settled = false
    end
  end
  return settled
end

function Session:_snapshot_discovery(discovery_id)
  local ids = self.state.meta_snapshot.discovered_discovery_ids
  for _, id in ipairs(ids) do if id == discovery_id then return false end end
  ids[#ids + 1] = discovery_id
  table.sort(ids)
  return true
end

-- Discovery caches are physical one-use world objects. The persistent DATA
-- event uses the same saved/reconciled pipeline as boss and floor rewards;
-- a profile write failure cannot create a second reward on later Continue.
function Session:claim_discovery(object)
  local world = self.state.world
  if not object or world:get_object(object.id) ~= object or object.interaction_role ~= "discovery" then
    return { applied = false, code = "invalid_discovery", reason = "Discovery cache is unavailable" }
  end
  if object.destroyed then return { applied = false, code = "destroyed", reason = "Discovery cache is destroyed" } end
  if object.discovery_claimed then
    return { applied = false, code = "already_claimed", reason = "Discovery cache already claimed", discovery_id = object.discovery_id }
  end
  local definition = self.registry:get_discovery(object.discovery_id)
  object.discovery_claimed = true
  local known = false
  for _, id in ipairs(self.state.meta_snapshot.discovered_discovery_ids or {}) do
    if id == definition.id then known = true; break end
  end
  if known then
    self.state.scrap = self.state.scrap + definition.repeat_scrap_reward
    return {
      applied = true, code = "repeat_discovery", discovery_id = definition.id,
      scrap = definition.repeat_scrap_reward, reason = "Known discovery recovered",
    }
  end
  self:_snapshot_discovery(definition.id)
  local event = self:_claim_research_reward("discovery:" .. definition.id, definition.first_data_reward, {
    kind = "discovery", discovery_id = definition.id,
  })
  return {
    applied = true, code = "first_discovery", discovery_id = definition.id,
    data = definition.first_data_reward, event_id = event.id, event_claimed = event.claimed,
    reason = "New discovery recorded",
  }
end

function Session:_fallen_metadata(provenance)
  local state = self.state
  local node = state.route and state.route:node(state.route.current_node_id) or nil
  local charms = {}
  for _, charm_id in pairs(state.charms and state.charms.slots or {}) do
    if charm_id then charms[#charms + 1] = charm_id end
  end
  table.sort(charms)
  local route_path = state.route and state.route:to_data().path or {}
  return {
    route_node_id = node and node.id or nil,
    biome_id = node and node.biome_id or (state.settings and state.settings.biome_id) or nil,
    tier_id = node and node.tier_id or (state.settings and state.settings.tier_id) or nil,
    route_depth = node and node.depth or state.stage,
    route_path = route_path,
    charm_ids = charms,
    research_ids = copy_string_list(state.meta_snapshot and state.meta_snapshot.unlocked_research_ids, "Run meta research"),
    death_cause = provenance and (provenance.cause or provenance.source) or "unknown",
  }
end

-- Fatal state is captured before App retires the active save. The data is a
-- detached body snapshot, so later archive retries cannot observe a mutable
-- actor table or accidentally revive its components.
function Session:_mark_player_dead(provenance)
  local state = self.state
  -- A campaign owns the world, not this particular actor. Fatal resolution
  -- is a durable campaign transaction; legacy standalone runs stay below.
  if self.campaign then
    local result, failure = self.campaign:handle_player_death(provenance or {})
    if result and result.applied then
      state.ended = "campaign_succession"
      return result
    end
    state.ended = "campaign_pending"
    self:_log("BODY LOST — SUCCESSION WILL RESUME FROM THE LAST COMMITTED CAMPAIGN.")
    return failure
  end
  state.ended = "gameover"
  if state.death_pending_archive or not state.player or not state.player.body
    or not tostring(state.run_id):match("^run:%d+$") then
    return state.death_pending_archive
  end
  state.death_pending_archive = {
    source_run_id = state.run_id,
    body = state.player.body:to_data(),
    metadata = self:_fallen_metadata(provenance),
  }
  return state.death_pending_archive
end

function Session:refresh_derived_player_stats()
  local player = self.state.player or self.state.run.player
  if not player then return end
  local values = RunModifiers.values(self.state, self.registry)
  player.max_health = math.max(1, (player.base_max_health or 5) + (values.max_health or 0))
  player.health = math.min(player.health, player.max_health)
  player.dash_base = math.max(1, (player.base_dash_cooldown or 3) + (values.dash_cooldown or 0))
  player.bomb_radius = math.max(1, (player.base_bomb_radius or 2) + (values.bomb_radius or 0))
  player.flare_light = math.max(1, (player.base_flare_light or 3) + (values.flare_light or 0))
  player.reload_bonus = values.reload_bonus or 0
end

function Session:create_resource_stack(resource_id, quantity, scope)
  local item_id
  if self.identity_allocator then
    item_id = self.identity_allocator:allocate_item_id(scope or "zone")
  else
    local sequence = self.state.next_item_sequence
    self.state.next_item_sequence = sequence + 1
    item_id = string.format("item:%06d", sequence)
  end
  return PhysicalItem.from_resource(resource_id, quantity, item_id, self.registry)
end

function Session:create_tool(tool_definition_id, scope)
  local item_id
  if self.identity_allocator then
    item_id = self.identity_allocator:allocate_item_id(scope or "zone")
  else
    local sequence = self.state.next_item_sequence
    self.state.next_item_sequence = sequence + 1
    item_id = string.format("item:%06d", sequence)
  end
  return PhysicalItem.from_tool(Tool.new(self.registry:get_tool(tool_definition_id), item_id), self.registry)
end

-- Campaign loadout state is deliberately body-identity-bound. Zone travel
-- retains it, while physical succession receives a fresh deterministic setup.
function Session:ensure_campaign_loadout()
  if not self.campaign then return nil end
  local state, player = self.state, self.state.player
  if not player or not player.body then return nil end
  local loadout = Loadout.from_data(state.loadout)
  if not loadout or loadout.body_actor_id ~= player.actor_id then
    loadout = Loadout.new_for(self, player)
    state.loadout = loadout
  else
    state.loadout = loadout
  end
  -- Keep an uninstalled owned part bound so a reconstruction can restore the
  -- same physical provider, but never leave a dangling reference once that
  -- part has actually left player ownership (for example through sale).
  for _, kind in ipairs({ "weapon", "ability" }) do
    local slots = kind == "weapon" and loadout.weapon_slots or loadout.ability_slots
    for index = 1, Loadout.SLOT_COUNT do
      local binding = slots[index]
      if binding and binding.source_kind == "component"
        and not player.body:find_component(binding.physical_id)
        and not state.inventory:get(binding.physical_id) then
        slots[index] = nil
      end
    end
  end
  -- A historical Campaign's scalar reserve is converted once per physical
  -- player body. No ranged provider means no fabricated arbitrary ammo.
  if loadout.ammo_migration_actor_id ~= player.actor_id and (player.ammo or 0) > 0 then
    local selected
    for index = 1, Loadout.SLOT_COUNT do
      local resolved = Loadout.resolve(self, player, loadout.weapon_slots[index], "weapon")
      if resolved and resolved.ability and resolved.ability.ammo then selected = resolved; break end
    end
    if selected then
      -- Historical reserve values were unbounded scalars. Convert them in
      -- physical stack-sized chunks so a large legacy save never creates an
      -- invalid stack or loses reserve when cargo is partly full.
      local definition = self.registry:get_resource(selected.ability.ammo.family)
      while player.ammo > 0 do
        local quantity = math.min(player.ammo, definition.max_stack)
        local item = self:create_resource_stack(selected.ability.ammo.family, quantity, "campaign")
        if not state.inventory:auto_place(item) then break end
        player.ammo = player.ammo - quantity
      end
      if player.ammo == 0 then
        loadout.ammo_migration_actor_id = player.actor_id
      end
    end
  elseif loadout.ammo_migration_actor_id == nil and (player.ammo or 0) == 0 then
    loadout.ammo_migration_actor_id = player.actor_id
  end
  for index = 1, Loadout.SLOT_COUNT do
    local resolved = Loadout.resolve(self, player, loadout.weapon_slots[index], "weapon")
    if resolved and resolved.ability and resolved.ability.ammo then self:weapon_magazine(resolved.provider, resolved.ability) end
  end
  return loadout
end

function Session:campaign_loadout()
  return self:ensure_campaign_loadout()
end

function Session:quick_slot(kind, index)
  local loadout = self:ensure_campaign_loadout()
  if not loadout then return nil end
  index = tonumber(index) or (kind == "weapon" and loadout.active_weapon or loadout.active_ability)
  if index ~= 1 and index ~= 2 then return nil end
  return kind == "weapon" and loadout.weapon_slots[index] or loadout.ability_slots[index]
end

function Session:loadout_candidates(kind)
  return Loadout.candidates(self, self.state.player, kind)
end

function Session:assign_campaign_loadout(kind, index, binding)
  local loadout = self:ensure_campaign_loadout()
  if not loadout or (kind ~= "weapon" and kind ~= "ability") or (index ~= 1 and index ~= 2) then
    return { applied = false, code = "invalid_loadout_slot", reason = "Loadout slot is unavailable" }
  end
  if binding ~= nil then
    local valid = false
    for _, candidate in ipairs(self:loadout_candidates(kind)) do
      if candidate.source_kind == binding.source_kind and candidate.physical_id == binding.physical_id
        and candidate.ability_id == binding.ability_id then valid = true; break end
    end
    if not valid then return { applied = false, code = "invalid_loadout_binding", reason = "That source cannot fill this quick slot" } end
    binding = { source_kind = binding.source_kind, physical_id = binding.physical_id, ability_id = binding.ability_id,
      attack_id = binding.attack_id, tool_definition_id = binding.tool_definition_id }
  end
  local slots = kind == "weapon" and loadout.weapon_slots or loadout.ability_slots
  slots[index] = binding
  return { applied = true, kind = kind, index = index, binding = binding }
end

function Session:weapon_magazine(component, ability)
  local ammo = ability and ability.ammo
  if not ammo or not component then return nil end
  component.weapon_state = component.weapon_state or {}
  local key = ammo.magazine_key or ability.id
  local magazine = component.weapon_state[key]
  if not magazine then
    magazine = { loaded = ammo.magazine_capacity }
    component.weapon_state[key] = magazine
  end
  magazine.loaded = math.max(0, math.min(ammo.magazine_capacity, math.floor(magazine.loaded or 0)))
  return magazine, key
end

function Session:ammo_reserve(resource_id)
  return self.state.inventory and self.state.inventory:resource_quantity(resource_id) or 0
end

function Session:_reload_weapon(provider, ability)
  local ammo, inventory = ability.ammo, self.state.inventory
  local magazine = self:weapon_magazine(provider, ability)
  local missing = ammo.magazine_capacity - magazine.loaded
  local reserve = inventory:resource_quantity(ammo.family)
  if missing <= 0 then return { applied = false, code = "magazine_full", reason = "Magazine is already full" } end
  if reserve <= 0 then
    return self:_ability_failure(ability.id, "no_compatible_ammo", "No compatible " .. self.registry:get_resource(ammo.family).display_name)
  end
  local amount = math.min(missing, reserve)
  local consumed, reason = inventory:consume_resources({ [ammo.family] = amount })
  if not consumed then return self:_ability_failure(ability.id, "no_compatible_ammo", reason) end
  magazine.loaded = magazine.loaded + amount
  local family = string.upper(self.registry:get_resource(ammo.family).display_name)
  self:_log("RELOADED — " .. magazine.loaded .. "/" .. ammo.magazine_capacity .. " " .. family .. ".")
  self:_sound("pickup")
  return { applied = true, code = "reloaded", ability_id = ability.id, component_id = provider.id,
    loaded = magazine.loaded, capacity = ammo.magazine_capacity, consumed = amount, family = ammo.family }
end

function Session:attack_active_weapon()
  local loadout, player = self:ensure_campaign_loadout(), self.state.player
  if not loadout then return self:_ability_failure("attack", "missing_loadout", "No Campaign loadout is available") end
  local resolved, failure = Loadout.resolve(self, player, loadout.weapon_slots[loadout.active_weapon], "weapon")
  if not resolved then
    self:_log((failure and failure.reason) or "WEAPON SLOT UNAVAILABLE.")
    return self:_ability_failure("attack", failure and failure.code or "slot_unavailable", failure and failure.reason or "Weapon slot is unavailable")
  end
  if resolved.source_kind == "tool" then return self:attack_tool(resolved.tool, resolved.tool_definition) end
  local ability = resolved.ability
  if ability.ammo then
    local magazine = self:weapon_magazine(resolved.provider, ability)
    local cost = ability.ammo.ammo_per_attack
    if magazine.loaded < cost then return self:_reload_weapon(resolved.provider, ability) end
    magazine.loaded = magazine.loaded - cost
    local result = self:activate_actor_ability(player, ability.id, {
      direction = player.direction, provider_component_id = resolved.provider.id, skip_resource = true,
    })
    if not result.applied then magazine.loaded = magazine.loaded + cost end
    return result
  end
  return self:activate_actor_ability(player, ability.id, {
    direction = player.direction, provider_component_id = resolved.provider.id,
  })
end

-- A conventional tool is a carried weapon source.  It intentionally has no
-- body ability/provider: the high-level ATTACK seam resolves it here and
-- keeps normal actor, object, and terrain damage authoritative below it.
function Session:_wear_tool(tool, definition)
  if tool.current_durability <= 0 then return false end
  tool.current_durability = math.max(0, tool.current_durability - 1)
  if tool.current_durability == 0 then
    self:_log(string.upper(definition.display_name) .. " BROKE.")
  end
  return true
end

function Session:_tool_target_protected(x, y, object)
  local state = self.state
  local cell_key = Grid.key(x, y)
  if state.surface_connector_cells and state.surface_connector_cells[cell_key] then
    return true, "Protected campaign connection"
  end
  if state.protected_content_cells and state.protected_content_cells[cell_key] then
    return true, "Protected critical site"
  end
  if object and (object.zone_connection_id or object.interaction_role == "zone_connection"
      or object.interaction_role == "traversal" or object.interaction_role == "reconstruction_station") then
    return true, "Protected world infrastructure"
  end
  return false
end

function Session:_tool_impact(x, y, target_kind, material_id, result, definition)
  self:_event("tool_impact", {
    x = x, y = y, target_kind = target_kind, material_id = material_id,
    applied = result and result.applied == true, destroyed = result and result.destroyed == true,
    code = result and result.code, tool_family = definition.family,
  })
end

function Session:_tool_modify_target(tool, definition, x, y, object)
  local material_id = object and object.material_id or (self.state.world:get_cell(x, y) or {}).material_id
  local material = material_id and self.registry:get_material(material_id) or nil
  if not material then return { applied = false, code = "no_physical_target", reason = "Nothing physical is in reach" } end
  local protected, protected_reason = self:_tool_target_protected(x, y, object)
  local result
  if protected then
    result = { applied = false, code = "protected_target", reason = protected_reason, x = x, y = y, material_id = material.id }
  else
    local effectiveness = material.tool_effectiveness and material.tool_effectiveness[definition.family] or nil
    if not effectiveness then
      result = { applied = false, code = "tool_no_effect", reason = "The " .. definition.display_name .. " cannot affect " .. material.display_name,
        x = x, y = y, material_id = material.id }
    else
      local amount = math.max(1, math.floor(definition.modification.damage * effectiveness + 0.0001))
      local spec = { amount = amount, cause = "kinetic", source = "tool", source_actor = self.state.player,
        source_actor_id = self.state.player and self.state.player.actor_id, source_tool_id = tool.id,
        tool_family = definition.family }
      result = object and self:damage_world_object(object, spec) or self:damage_terrain(x, y, spec)
    end
  end
  self:_wear_tool(tool, definition)
  self:_tool_impact(x, y, object and "world_object" or "terrain", material.id, result, definition)
  self:_sound(result.applied and "hit" or "select")
  if result.destroyed then
    self:_log(string.upper(material.display_name) .. " BROKEN.")
  elseif result.applied then
    self:_log(string.upper(material.display_name) .. " DAMAGED.")
  else
    self:_log(result.reason or "CLANG — NO EFFECT.")
  end
  result.tool_id, result.tool_family = tool.id, definition.family
  result.committed = true
  return result
end

function Session:attack_tool(tool, definition)
  definition = definition or (tool and self.registry:get_tool(tool.definition_id))
  if not tool or not definition or not Tool.is_functional(tool) then
    self:_log("BROKEN TOOL.")
    return self:_ability_failure("tool.attack", "tool_broken", "Assigned tool is broken")
  end
  local player, cell = self.state.player, self:faced_cell(self.state.player)
  if not player or not cell then
    return self:_ability_failure("tool.attack", "invalid_direction", "A cardinal facing direction is required")
  end
  local actor = self:_actor_at(cell.x, cell.y, player)
  if actor then
    if not self:are_hostile(player, actor) then
      self:_log("TOOL STRIKE HAS NO HOSTILE TARGET.")
      return self:_ability_failure("tool.attack", "friendly_target", "That actor is not hostile")
    end
    local damage = self:_apply_world_actor_damage(actor, definition.combat.damage, nil, {
      cause = "kinetic", source = "tool", source_actor = player, source_actor_id = player.actor_id,
      source_tool_id = tool.id, tool_family = definition.family,
    })
    local force = nil
    if not damage.dead and (definition.combat.force or 0) > 0 then
      local delta = DIRECTIONS[player.direction]
      force = self:apply_force(actor, { dx = delta[1], dy = delta[2], distance = definition.combat.force,
        cause = "kinetic", source_actor = player, source_actor_id = player.actor_id, source_tool_id = tool.id })
    end
    self:_wear_tool(tool, definition)
    self:_sound("hit")
    self:_log(string.upper(definition.display_name) .. " STRIKE LANDED.")
    return { applied = true, code = "tool_attack", tool_id = tool.id, target = actor, damage = damage, force = force,
      durability = tool.current_durability }
  end
  local object = self.state.world:object_at(cell.x, cell.y)
  if object and not object.destroyed then
    return self:_tool_modify_target(tool, definition, cell.x, cell.y, object)
  end
  local terrain = self.state.world:get_cell(cell.x, cell.y)
  local material = terrain and self.registry:get_material(terrain.material_id)
  if material and (material.solid or material.destructible) then
    return self:_tool_modify_target(tool, definition, cell.x, cell.y, nil)
  end
  self:_log("NOTHING PHYSICAL TO STRIKE.")
  return self:_ability_failure("tool.attack", "no_physical_target", "Nothing physical is in reach")
end

function Session:activate_active_ability()
  local loadout, player = self:ensure_campaign_loadout(), self.state.player
  if not loadout then return self:_ability_failure("ability", "missing_loadout", "No Campaign loadout is available") end
  local resolved, failure = Loadout.resolve(self, player, loadout.ability_slots[loadout.active_ability], "ability")
  if not resolved then
    self:_log((failure and failure.reason) or "ABILITY SLOT UNAVAILABLE.")
    return self:_ability_failure("ability", failure and failure.code or "slot_unavailable", failure and failure.reason or "Ability slot is unavailable")
  end
  if resolved.ability.implementation == "dash" then return self:_dash() end
  return self:activate_actor_ability(player, resolved.ability.id, {
    direction = player.direction, provider_component_id = resolved.provider.id,
  })
end

function Session:swap_campaign_loadout(kind)
  local loadout, player = self:ensure_campaign_loadout(), self.state.player
  if not loadout then return { applied = false, code = "missing_loadout", reason = "No Campaign loadout is available" } end
  local active_key = kind == "weapon" and "active_weapon" or "active_ability"
  local other = loadout[active_key] == 1 and 2 or 1
  local slots = kind == "weapon" and loadout.weapon_slots or loadout.ability_slots
  local resolved, failure = Loadout.resolve(self, player, slots[other], kind)
  if not resolved then
    self:_log((failure and failure.reason) or "ALTERNATE SLOT UNAVAILABLE.")
    return { applied = false, code = failure and failure.code or "slot_unavailable", reason = failure and failure.reason or "Alternate slot is unavailable" }
  end
  loadout[active_key] = other
  self:_log(string.upper(kind) .. " → " .. string.upper(resolved.display_name or resolved.ability.display_name) .. ".")
  self:_sound("select")
  return { applied = true, kind = kind, active = other, ability_id = resolved.ability and resolved.ability.id,
    component_id = resolved.provider and resolved.provider.id, tool_id = resolved.tool and resolved.tool.id }
end

function Session:campaign_loadout_swap_available(kind)
  local loadout, player = self:ensure_campaign_loadout(), self.state.player
  if not loadout then
    return nil, { applied = false, code = "missing_loadout", reason = "No Campaign loadout is available" }
  end
  local active_key = kind == "weapon" and "active_weapon" or kind == "ability" and "active_ability" or nil
  if not active_key then
    return nil, { applied = false, code = "invalid_loadout_slot", reason = "Loadout slot is unavailable" }
  end
  local other = loadout[active_key] == 1 and 2 or 1
  local slots = kind == "weapon" and loadout.weapon_slots or loadout.ability_slots
  local resolved, failure = Loadout.resolve(self, player, slots[other], kind)
  if not resolved then
    return nil, { applied = false, code = failure and failure.code or "slot_unavailable",
      reason = failure and failure.reason or "Alternate slot is unavailable" }
  end
  return resolved
end

function Session:damage_world_object(object_or_id, spec)
  if not self.state.world then
    return { applied = false, code = "no_world", reason = "No active world" }
  end
  return EnvironmentDamage.apply_to_object(self.state.world, object_or_id, spec)
end

function Session:actor_faction_id(actor)
  return Factions.actor_id(self.registry, actor, self.state.player)
end

function Session:are_hostile(first, second)
  return Factions.are_hostile(self.registry, first, second, self.state.player)
end

function Session:ignite_terrain(x, y, context)
  if not self.state.world then
    return { applied = false, code = "no_world", reason = "No active world" }
  end
  return Fire.ignite_terrain(self.state.world, x, y, context)
end

function Session:ignite_world_object(object_or_id, context)
  if not self.state.world then
    return { applied = false, code = "no_world", reason = "No active world" }
  end
  return Fire.ignite_object(self.state.world, object_or_id, context)
end

function Session:_move_entity(value, x, y)
  value.x, value.y = x, y
end

function Session:_actor_at(x, y, excluded)
  local state = self.state
  if state.player and state.player ~= excluded and state.player.x == x and state.player.y == y then
    return state.player
  end
  for _, enemy in ipairs(state.enemies or {}) do
    if enemy ~= excluded and enemy.x == x and enemy.y == y then
      return enemy
    end
  end
  if state.boss and state.boss ~= excluded and state.boss.x == x and state.boss.y == y then
    return state.boss
  end
  return nil
end

function Session:_living_actors()
  local state, actors = self.state, {}
  if state.player and (state.player.health == nil or state.player.health > 0) then actors[#actors + 1] = state.player end
  for _, enemy in ipairs(state.enemies or {}) do
    if enemy.health == nil or enemy.health > 0 then actors[#actors + 1] = enemy end
  end
  if state.boss and (state.boss.health == nil or state.boss.health > 0) then actors[#actors + 1] = state.boss end
  return actors
end

function Session:_actor_stable_id(actor)
  if actor == self.state.player then return "actor:player" end
  if actor == self.state.boss then return "actor:boss:" .. tostring(actor.boss_id or actor.content_id or actor.kind) end
  local component = actor.body and actor.body:list_components()[1] or nil
  return "actor:" .. tostring(component and component.id or actor.content_id or actor.kind or "unknown")
end

function Session:_navigation_begin()
  self._navigation = {
    hostile_fields = {},
    route_fields = {},
    reserved = {},
    stats = { hostile_fields = 0, route_fields = 0, fallbacks = 0 },
  }
end

function Session:_navigation_finish()
  if self._navigation then
    self._last_navigation_stats = self._navigation.stats
    self._navigation = nil
  end
end

function Session:navigation_stats()
  local stats = (self._navigation and self._navigation.stats) or self._last_navigation_stats or {}
  return {
    hostile_fields = stats.hostile_fields or 0,
    route_fields = stats.route_fields or 0,
    fallbacks = stats.fallbacks or 0,
  }
end

function Session:_navigation_is_dangerous(x, y)
  return self.state.world:is_hazardous(x, y)
    or #self.state.world:fires_at(x, y) > 0
    or self.state.world:is_harmful_gas_at(x, y)
    or self:is_flare_controlled(x, y)
end

-- A multi-source reverse BFS is shared by every enemy of a faction during a
-- turn. The old per-enemy search walked most of the 80x50 map repeatedly;
-- this keeps the same nearest-reachable-hostile rule while doing that terrain
-- work once per faction instead.
function Session:_navigation_hostile_field(actor)
  local navigation = self._navigation
  if not navigation then return nil end
  local faction_id = self:actor_faction_id(actor)
  if navigation.hostile_fields[faction_id] then return navigation.hostile_fields[faction_id] end
  local sources = {}
  for _, target in ipairs(self:_living_actors()) do
    if target ~= actor and self:are_hostile(actor, target) then sources[#sources + 1] = target end
  end
  table.sort(sources, function(first, second)
    local first_id, second_id = self:_actor_stable_id(first), self:_actor_stable_id(second)
    if first_id ~= second_id then return first_id < second_id end
    if first.y ~= second.y then return first.y < second.y end
    return first.x < second.x
  end)
  local field, queue, head = {}, {}, 1
  for _, target in ipairs(sources) do
    local key = Grid.key(target.x, target.y)
    if not field[key] then
      field[key] = { distance = 0, target = target }
      queue[#queue + 1] = { x = target.x, y = target.y, target = target }
    end
  end
  while queue[head] do
    local point = queue[head]
    head = head + 1
    local here = field[Grid.key(point.x, point.y)]
    for _, neighbour in ipairs(Grid.neighbours(point)) do
      local key = Grid.key(neighbour.x, neighbour.y)
      if Grid.in_bounds(neighbour.x, neighbour.y) and self:_open(neighbour.x, neighbour.y) and not field[key] then
        field[key] = { distance = here.distance + 1, target = here.target }
        queue[#queue + 1] = { x = neighbour.x, y = neighbour.y, target = here.target }
      end
    end
  end
  navigation.hostile_fields[faction_id] = field
  navigation.stats.hostile_fields = navigation.stats.hostile_fields + 1
  return field
end

function Session:_navigation_route_field(finish, avoid_hazards)
  local navigation = self._navigation
  if not navigation then return nil end
  local key = Grid.key(finish.x, finish.y) .. ":" .. (avoid_hazards and "safe" or "unsafe")
  if navigation.route_fields[key] then return navigation.route_fields[key] end
  local field, queue, head = { [Grid.key(finish.x, finish.y)] = 0 }, { Grid.cell(finish.x, finish.y) }, 1
  while queue[head] do
    local point = queue[head]
    head = head + 1
    local distance = field[Grid.key(point.x, point.y)]
    for _, neighbour in ipairs(Grid.neighbours(point)) do
      local neighbour_key = Grid.key(neighbour.x, neighbour.y)
      if Grid.in_bounds(neighbour.x, neighbour.y) and self:_open(neighbour.x, neighbour.y) and field[neighbour_key] == nil
        and (not avoid_hazards or not self:_navigation_is_dangerous(neighbour.x, neighbour.y)) then
        field[neighbour_key] = distance + 1
        queue[#queue + 1] = neighbour
      end
    end
  end
  navigation.route_fields[key] = field
  navigation.stats.route_fields = navigation.stats.route_fields + 1
  return field
end

function Session:_navigation_route(actor, finish, blocked, avoid_hazards)
  local field = self:_navigation_route_field(finish, avoid_hazards)
  if not field then return nil end
  local route, point = { Grid.cell(actor.x, actor.y) }, Grid.cell(actor.x, actor.y)
  local maximum_steps = Grid.width * Grid.height
  while point.x ~= finish.x or point.y ~= finish.y do
    local distance = field[Grid.key(point.x, point.y)]
    if distance == nil or distance <= 0 or #route > maximum_steps then return nil end
    local options = {}
    for _, neighbour in ipairs(Grid.neighbours(point)) do
      local key = Grid.key(neighbour.x, neighbour.y)
      if field[key] == distance - 1 and not blocked[key] and not self._navigation.reserved[key] then
        options[#options + 1] = neighbour
      end
    end
    if #options == 0 then return nil end
    -- Flankers rotate otherwise-equal gradients. It creates a readable side
    -- approach without random number consumption or diagonal clipping.
    local choice = 1
    if actor.ai_role == "flanker" and #options > 1 then
      choice = ((actor.ai_phase or 0) + (actor.ai_cycle or 0)) % #options + 1
    end
    point = options[choice]
    route[#route + 1] = Grid.cell(point.x, point.y)
  end
  return route
end

function Session:_navigation_hazard_aware_path(actor, finish, blocked)
  local route = self:_navigation_route(actor, finish, blocked, true)
  if route then return route, true end
  route = self:_navigation_route(actor, finish, blocked, false)
  if route then return route, false end
  self._navigation.stats.fallbacks = self._navigation.stats.fallbacks + 1
  return self:_hazard_aware_path(actor, finish, blocked)
end

-- Nearest reachable hostile target with a semantic/physical stable tie-break.
-- The player receives no hidden universal priority: a nearby rival is a valid
-- ecology target, while a reachable player remains an equally ordinary foe.
function Session:_nearest_hostile_target(actor)
  if self._navigation then
    local field = self:_navigation_hostile_field(actor)
    local entry = field and field[Grid.key(actor.x, actor.y)]
    local chosen = entry and entry.target
    if not chosen or chosen.health == 0 then return nil, {} end
    local blocked = {}
    for _, other in ipairs(self:_living_actors()) do
      if other ~= actor and other ~= chosen then blocked[Grid.key(other.x, other.y)] = true end
    end
    return chosen, self:_navigation_hazard_aware_path(actor, chosen, blocked)
  end
  -- One terrain BFS finds the nearest reachable hostile layer.  We then run
  -- the existing hazard-aware path only for the winner, avoiding an expensive
  -- full-map path solve for every actor pair each ordinary turn.
  local targets_by_cell = {}
  for _, target in ipairs(self:_living_actors()) do
    if target ~= actor and self:are_hostile(actor, target) then
      local location_key = Grid.key(target.x, target.y)
      targets_by_cell[location_key] = targets_by_cell[location_key] or {}
      targets_by_cell[location_key][#targets_by_cell[location_key] + 1] = target
    end
  end
  local candidates, seen, queue, cursor, nearest = {}, { [Grid.key(actor.x, actor.y)] = 0 },
    { { x = actor.x, y = actor.y } }, 1, nil
  while queue[cursor] do
    local point = queue[cursor]
    cursor = cursor + 1
    local distance = seen[Grid.key(point.x, point.y)]
    if nearest and distance > nearest then break end
    for _, target in ipairs(targets_by_cell[Grid.key(point.x, point.y)] or {}) do
      nearest = distance
      candidates[#candidates + 1] = { actor = target, distance = distance, stable_id = self:_actor_stable_id(target) }
    end
    for _, neighbour in ipairs(Grid.neighbours(point)) do
      local location_key = Grid.key(neighbour.x, neighbour.y)
      if not nearest and Grid.in_bounds(neighbour.x, neighbour.y) and self.state.world:is_passable(neighbour.x, neighbour.y)
        and seen[location_key] == nil then
        seen[location_key] = distance + 1
        queue[#queue + 1] = neighbour
      end
    end
  end
  table.sort(candidates, function(first, second)
    if first.distance ~= second.distance then return first.distance < second.distance end
    return first.stable_id < second.stable_id
  end)
  local chosen = candidates[1] and candidates[1].actor
  if not chosen then return nil, {} end
  local blocked = {}
  for _, other in ipairs(self:_living_actors()) do
    if other ~= actor and other ~= chosen then blocked[Grid.key(other.x, other.y)] = true end
  end
  return chosen, self:_hazard_aware_path(actor, chosen, blocked)
end

function Session:_notify_combat(first, second)
  if self.state.reinforcement_state and self.state.reinforcement_state.enabled then
    return ReinforcementSimulation.arm_for_combat(self, first, second)
  end
  return { applied = false, code = "disabled" }
end

function Session:apply_force(target, force_spec)
  local world = self.state.world
  if not world then
    return { applied = false, code = "no_world", reason = "No active world" }
  end
  local is_object = target and target.id and world:get_object(target.id) == target
  if is_object and not target.movable_by_force then
    return { applied = false, code = "immovable", reason = "World object cannot be displaced" }
  end
  local spec = {}
  for name, value in pairs(force_spec or {}) do
    spec[name] = value
  end
  spec.is_blocked = function(x, y, moving)
    if not world:is_passable(x, y) then
      return true, "blocked_world"
    end
    if self:_actor_at(x, y, moving) then
      return true, "blocked_actor"
    end
    return false
  end
  spec.move = function(value, x, y)
    if is_object then
      return world:move_object(value, x, y)
    end
    self:_move_entity(value, x, y)
    return { applied = true }
  end
  local supplied_on_step = spec.on_step
  if not is_object then
    spec.on_step = function(value, x, y, step)
      local entry = self:_resolve_actor_hazard_entry(value, x, y, {
        movement = "force",
        force = force_spec,
        force_step = step.index,
      })
      if entry.dead then
        return { stop = true, code = "target_destroyed" }
      end
      if supplied_on_step then
        return supplied_on_step(value, x, y, step)
      end
    end
  end
  local result = Force.apply(world, target, spec)
  if not is_object and result.code ~= "target_destroyed" then
    result.impact = self:_resolve_force_impact(target, result, force_spec or {})
  end
  return result
end

function Session:_allocate_actor_id(scope)
  if self.identity_allocator then
    return self.identity_allocator:allocate_actor(scope or "zone")
  end
  local sequence = self.state.next_actor_sequence
  self.state.next_actor_sequence = sequence + 1
  return string.format("actor:legacy:%06d", sequence)
end

function Session:_build_body(actor_definition, scope)
  local body = Body.new(self.registry, actor_definition.body_topology_id)
  for _, installation in ipairs(actor_definition.installed_components) do
    local component = self.component_factory:create(installation.component_id, scope or "zone")
    local installed, reason = body:install(installation.slot_id, component)
    assert(installed, reason)
  end
  return body
end

function Session:actor_has_capability(actor, ability_id)
  return actor.body and actor.body:has_capability(ability_id) or false
end

function Session:locomotion_state(actor)
  return Locomotion.derive(actor and actor.body or nil)
end

function Session:locomotion_provider_count(actor)
  return self:locomotion_state(actor).provider_count
end

function Session:validate_actor_movement(actor, dx, dy)
  local result = Locomotion.validate_move(self:locomotion_state(actor), dx, dy)
  if not result.applied then
    return result
  end
  if not self:_open(actor.x + dx, actor.y + dy) then
    return {
      applied = false,
      code = "blocked_terrain",
      reason = "A wall blocks the path",
      locomotion = result.locomotion,
    }
  end
  return result
end

function Session:_move_actor(actor, x, y)
  local result = self:validate_actor_movement(actor, x - actor.x, y - actor.y)
  if not result.applied then
    return result
  end
  -- Legacy enemies move only on cardinal path tiles. A legless body can still
  -- crawl, but it advances only every other attempted chase step rather than
  -- retaining its former full pursuit rate. Player crawling never skips turns.
  if actor ~= self.state.player and result.locomotion.body_derived
    and result.locomotion.state == Locomotion.CRAWLING then
    actor.crawl_stride = (actor.crawl_stride or 0) + 1
    if actor.crawl_stride % 2 == 0 then
      return {
        applied = false,
        code = "crawl_recovering",
        reason = "Actor is dragging itself forward",
        locomotion = result.locomotion,
      }
    end
  end
  self:_move_entity(actor, x, y)
  result.hazard = self:_resolve_actor_hazard_entry(actor, x, y, { movement = "voluntary" })
  if result.hazard.dead then
    result.dead = true
  end
  return result
end

function Session:actor_ability_provider(actor, ability_id, provider_component_id)
  if not actor or not actor.body then
    return nil
  end
  -- Body capability_providers follows topology slot_order, which is the
  -- deterministic first-provider rule until equipment selection exists.
  local providers = actor.body:capability_providers(ability_id)
  local provider = providers[1]
  if provider_component_id then
    provider = nil
    for _, candidate in ipairs(providers) do
      if candidate.id == provider_component_id then
        provider = candidate
        break
      end
    end
  end
  if not provider then
    return nil
  end
  local installed = actor.body:find_component(provider.id)
  return installed and {
    component = provider,
    slot_id = installed.slot_id,
  } or nil
end

function Session:available_actor_abilities(actor, activation_type)
  if not actor or not actor.body then
    return {}
  end
  local result = {}
  for _, ability_id in ipairs(actor.body:list_capabilities()) do
    local ability = self.registry:get_ability(ability_id)
    if not activation_type or (ability.activation_type or "body") == activation_type then
      result[#result + 1] = ability_id
    end
  end
  return result
end

function Session:actor_ability_by_implementation(actor, implementation)
  if not actor or not actor.body then return nil end
  for _, ability_id in ipairs(actor.body:list_capabilities()) do
    if self.registry:get_ability(ability_id).implementation == implementation then
      return ability_id
    end
  end
  return nil
end

function Session:actor_ability_by_implementations(actor, implementations)
  if not actor or not actor.body then return nil end
  local wanted = {}
  for _, implementation in ipairs(implementations or {}) do wanted[implementation] = true end
  for _, ability_id in ipairs(actor.body:list_capabilities()) do
    if wanted[self.registry:get_ability(ability_id).implementation] then return ability_id end
  end
  return nil
end

function Session:actor_known_ability_by_implementation(actor, implementation)
  if not actor or not actor.body then return nil end
  for _, component in ipairs(actor.body:list_components()) do
    for _, ability_id in ipairs(self.registry:get_component(component.definition_id).abilities) do
      if self.registry:get_ability(ability_id).implementation == implementation then
        return ability_id
      end
    end
  end
  return nil
end

function Session:_player_can_activate_body_abilities()
  local phase = self.state.phase
  return phase == "combat" or phase == "exit" or phase == "boss"
end

function Session:_remove_enemy_actor(actor)
  for index = #self.state.enemies, 1, -1 do
    if self.state.enemies[index] == actor then
      local removed = remove(self.state.enemies, index)
      self:_create_corpse(removed)
      return true
    end
  end
  return false
end

function Session:_execute_self_destruct(actor, provider, wear)
  local state = self.state
  local radius = 1
  self:_damage_environment_radius(actor, radius, {
    amount = 2,
    cause = "explosive",
    source = "self_destruct",
    source_actor = actor,
    source_actor_id = actor.actor_id,
    source_actor_id = actor.content_id or actor.kind,
    source_component_id = provider and provider.id or nil,
    ability_id = SELF_DESTRUCT_ABILITY,
  })
  local cells = self:_blast(actor, radius)
  for location_key in pairs(cells) do
    state.effects[location_key] = true
  end
  self:_sound("boom")

  -- Explosions deliberately remain faction-impartial.  The same authoritative
  -- body damage path handles player, rival, and boss casualties.
  for _, target in ipairs(self:_living_actors()) do
    if target ~= actor and cells[Grid.key(target.x, target.y)] then
      self:_apply_world_actor_damage(target, target == state.player and 1 or 2,
        target == state.player and "A bomber detonated beside you." or nil, {
          cause = "explosive",
          source = "self_destruct",
          source_actor = actor,
          source_actor_id = actor.content_id or actor.kind,
          source_component_id = provider and provider.id or nil,
          ability_id = SELF_DESTRUCT_ABILITY,
          skip_body_damage = true,
        })
    end
  end

  self:_apply_explosion_force(actor, radius, cells, {
    distance = 1,
    cause = "explosive",
    source_actor = actor,
    source_actor_id = actor.content_id or actor.kind,
    source_component_id = provider and provider.id or nil,
    ability_id = SELF_DESTRUCT_ABILITY,
  })

  if actor == state.player then
    actor.health = 0
    actor.impact = 2
    self:_mark_player_dead({ cause = "explosive", source = "self_destruct" })
    self:_event("hit")
    self:_log("Your volatile charge detonated.")
  else
    self:_remove_enemy_actor(actor)
    self:_log("A bomber exploded nearby!")
  end
  self:validate_physical_ownership()
  local data = {
    applied = true,
    ability_id = SELF_DESTRUCT_ABILITY,
    implementation = "self_destruct",
    actor = actor,
    component_id = provider.id,
    wear = wear,
    radius = radius,
  }
  return data
end

function Session:_actor_side(actor)
  return actor == self.state.player and "player" or "enemy"
end

function Session:_spawn_projectile(actor, provider, ability, direction)
  local modifier = actor == self.state.player and RunModifiers.value(self.state, self.registry, "projectile_damage") or 0
  local bullet = entity("bullet", actor.x, actor.y, {
    direction = direction,
    active = false,
    travel = 1,
    max = ability.range or actor.bullet_range,
    light = 2,
    source_actor = actor,
    source_actor_kind = actor.kind,
    source_side = self:_actor_side(actor),
    source_component_id = provider.id,
    ability_id = ability.id,
    damage = math.max(1, (ability.damage or 1) + modifier),
    pierce_remaining = ability.pierce or 0,
  })
  self.state.bullets[#self.state.bullets + 1] = bullet
  return bullet
end

function Session:_execute_projectile(actor, provider, wear, ability, request)
  local bullet = self:_spawn_projectile(actor, provider, ability, request.direction)
  self:_sound("shoot")
  if actor == self.state.player then
    self:_log("Fired " .. DIRECTIONS[request.direction][3] .. ".")
  end
  return {
    applied = true,
    ability_id = ability.id,
    implementation = ability.implementation,
    actor = actor,
    component_id = provider.id,
    wear = wear,
    projectile = bullet,
  }
end

function Session:_execute_scattershot(actor, provider, wear, ability, request)
  local center_index = 1
  for index, direction in ipairs(DIRECTION_RING) do
    if direction == request.direction then center_index = index; break end
  end
  local pellets, count = {}, math.max(1, ability.pellets or 3)
  local offset_start = -math.floor(count / 2)
  for offset = offset_start, offset_start + count - 1 do
    local index = ((center_index - 1 + offset) % #DIRECTION_RING) + 1
    pellets[#pellets + 1] = self:_spawn_projectile(actor, provider, ability, DIRECTION_RING[index])
  end
  self:_sound("shoot")
  if actor == self.state.player then
    self:_log("Scatter fired " .. DIRECTIONS[request.direction][3] .. ".")
  end
  return {
    applied = true,
    ability_id = ability.id,
    implementation = "scattershot",
    actor = actor,
    component_id = provider.id,
    wear = wear,
    projectiles = pellets,
  }
end

-- Melee is deliberately a short shared ability rather than an enemy attack
-- shortcut.  It uses normal localized damage then the existing force service,
-- so hazards and structural impacts remain authoritative downstream effects.
function Session:_execute_melee(actor, provider, wear, ability, request)
  local target = request.target
  local damage_modifier = actor == self.state.player and RunModifiers.value(self.state, self.registry, "melee_damage") or 0
  local force_modifier = actor == self.state.player and RunModifiers.value(self.state, self.registry, "melee_force") or 0
  local damage = math.max(1, ability.damage + damage_modifier)
  local force_distance = math.max(1, ability.force + force_modifier)
  local target_damage = self:_apply_world_actor_damage(target, damage,
    target == self.state.player and "A close strike tears into you." or nil, {
      cause = "kinetic",
      source = "melee",
      source_actor = actor,
      source_actor_id = actor.content_id or actor.kind,
      source_component_id = provider.id,
      ability_id = ability.id,
    })
  local force = nil
  if not target_damage.dead then
    force = self:apply_force(target, {
      dx = target.x - actor.x,
      dy = target.y - actor.y,
      distance = force_distance,
      cause = "kinetic",
      source_actor = actor,
      source_actor_id = actor.content_id or actor.kind,
      source_component_id = provider.id,
      ability_id = ability.id,
    })
  end
  if actor == self.state.player then self:_log("Impact strike landed.") end
  return {
    applied = true,
    ability_id = ability.id,
    implementation = "melee",
    actor = actor,
    target = target,
    component_id = provider.id,
    wear = wear,
    damage = target_damage,
    force = force,
  }
end

function Session:_execute_area_burst(actor, provider, wear, ability, request)
  local target = request.target
  local burst = {
    kind = "arcane_burst",
    -- Enemy AI invokes after the turn's updates, so it is immediately armed
    -- for the next player turn. Player activation occurs before updates and
    -- uses one arming tick to keep the same three-turn delay.
    active = actor ~= self.state.player,
    source_actor = actor,
    source_actor_kind = actor.kind,
    source_side = self:_actor_side(actor),
    source_component_id = provider.id,
    ability_id = ability.id,
    x = target.x,
    y = target.y,
    radius = ability.radius,
    remaining = ability.delay,
  }
  self.state.area_attacks[#self.state.area_attacks + 1] = burst
  if actor == self.state.player then
    self:_log("Arcane burst primed " .. DIRECTIONS[request.direction][3] .. ".")
  end
  return {
    applied = true,
    ability_id = ability.id,
    implementation = "area_burst",
    actor = actor,
    component_id = provider.id,
    wear = wear,
    burst = burst,
  }
end

function Session:_electrical_actor_id(actor)
  if actor == self.state.player then
    return "actor:player"
  end
  local component = actor.body and actor.body:list_components()[1] or nil
  if component then
    return "actor:" .. component.id
  end
  local index = self:_enemy_index(actor)
  return string.format("actor:%s:%04d", actor.content_id or actor.kind or "unknown", index or 0)
end

-- A discharge is a transient conductive-network event. It does not query or
-- mutate World circuits, generators, breakers, or powered-door state.
function Session:_execute_electrical_discharge(actor, provider, wear, ability, request)
  local state = self.state
  local discharge = Electricity.discharge(state.world, request.target, {
    max_cells = ability.max_cells,
    damage = ability.damage,
    source_actor_id = self:_electrical_actor_id(actor),
    source_component_id = provider.id,
    ability_id = ability.id,
    cause = "electrical",
  }, {
    actors_at = function(x, y)
      return self:_actors_at(x, y)
    end,
    actor_id = function(target)
      return self:_electrical_actor_id(target)
    end,
    on_actor_reached = function(target, cell)
      return self:_apply_world_actor_damage(target, ability.damage,
        target == state.player and "ELECTRICITY RIPS THROUGH YOU." or nil, {
          cause = "electrical",
          source = "electrical_discharge",
          source_actor = actor,
          source_actor_id = self:_electrical_actor_id(actor),
          source_component_id = provider.id,
          ability_id = ability.id,
          x = cell.x,
          y = cell.y,
        })
    end,
  })
  state.electrical_effects = discharge.reached_cells
  self:_event("electricity", discharge)
  if actor == state.player then
    self:_log("Electrical discharge released.")
  end
  return {
    applied = true,
    ability_id = ability.id,
    implementation = "electrical_discharge",
    actor = actor,
    component_id = provider.id,
    wear = wear,
    discharge = discharge,
  }
end

function Session:_ability_failure(ability_id, code, reason)
  return { applied = false, ability_id = ability_id, code = code, reason = reason }
end

function Session:_validate_ability_request(actor, ability, params)
  if ability.implementation == "projectile" or ability.implementation == "scattershot"
    or ability.implementation == "piercing_projectile" then
    if not params or not DIRECTIONS[params.direction] then
      return nil, self:_ability_failure(ability.id, "invalid_direction", "A valid firing direction is required")
    end
    return { direction = params.direction }
  end
  if ability.implementation == "area_burst" then
    local target = params and params.target
    local direction = params and params.direction
    if target then
      if type(target.x) ~= "number" or type(target.y) ~= "number" then
        return nil, self:_ability_failure(ability.id, "invalid_target", "A valid burst target is required")
      end
      target = { x = target.x, y = target.y }
    elseif DIRECTIONS[direction] then
      local delta = DIRECTIONS[direction]
      target = {
        x = actor.x + delta[1] * ability.range,
        y = actor.y + delta[2] * ability.range,
      }
    else
      return nil, self:_ability_failure(ability.id, "invalid_target", "A burst target or direction is required")
    end
    if not Grid.in_bounds(target.x, target.y) then
      return nil, self:_ability_failure(ability.id, "invalid_target", "Burst target is outside the floor")
    end
    return { target = target, direction = direction }
  end
  if ability.implementation == "electrical_discharge" then
    local direction = params and params.direction
    if not DIRECTIONS[direction] then
      return nil, self:_ability_failure(ability.id, "invalid_direction", "A valid discharge direction is required")
    end
    local delta = DIRECTIONS[direction]
    local target = { x = actor.x + delta[1], y = actor.y + delta[2] }
    if not Grid.in_bounds(target.x, target.y) then
      return nil, self:_ability_failure(ability.id, "invalid_target", "Discharge target is outside the floor")
    end
    local trace = Electricity.trace(self.state.world, target, { max_cells = ability.max_cells })
    if not trace.applied then
      return nil, self:_ability_failure(ability.id, trace.code, "No conductive network reaches that discharge")
    end
    return { target = target, direction = direction }
  end
  if ability.implementation == "melee" then
    local direction = params and params.direction
    if not DIRECTIONS[direction] then
      return nil, self:_ability_failure(ability.id, "invalid_direction", "A valid strike direction is required")
    end
    local delta = DIRECTIONS[direction]
    local x, y = actor.x + delta[1], actor.y + delta[2]
    if not Grid.in_bounds(x, y) then
      return nil, self:_ability_failure(ability.id, "invalid_target", "Strike target is outside the floor")
    end
    local target = self:_actor_at(x, y, actor)
    if not target then
      return nil, self:_ability_failure(ability.id, "no_adjacent_target", "No adjacent target in that direction")
    end
    if not self:are_hostile(actor, target) then
      return nil, self:_ability_failure(ability.id, "friendly_target", "That actor is not hostile")
    end
    return { target = target, direction = direction }
  end
  return {}
end

function Session:_consume_ability_resource(actor, ability)
  local resource = ability.resource
  if not resource then
    return true
  end
  local available = actor[resource.name]
  if type(available) ~= "number" or available < resource.amount then
    return nil, self:_ability_failure(ability.id, "insufficient_" .. resource.name, "Insufficient " .. resource.name)
  end
  actor[resource.name] = available - resource.amount
  return true
end

-- One activation entry point for player and AI. Decision policy lives in the
-- input/AI callers; the ability effect and component wear live here.
function Session:activate_actor_ability(actor, ability_id, params)
  if not actor or not actor.body then
    return self:_ability_failure(ability_id, "missing_capability", "Actor has no body")
  end
  if actor == self.state.player and not self:_player_can_activate_body_abilities() then
    return self:_ability_failure(ability_id, "invalid_phase", "Body abilities cannot be activated outside a floor")
  end
  local ability = self.registry:get_ability(ability_id)
  if ability_id == LOCOMOTION_ABILITY then
    return self:_ability_failure(ability_id, "direct_action", "Locomotion is invoked through movement input")
  end
  local selected = self:actor_ability_provider(actor, ability_id, params and params.provider_component_id)
  if not selected then
    local broken_provider = false
    for _, component in ipairs(actor.body:list_components()) do
      local definition = self.registry:get_component(component.definition_id)
      for _, provided_ability in ipairs(definition.abilities) do
        if provided_ability == ability_id then
          broken_provider = true
          break
        end
      end
    end
    return self:_ability_failure(ability_id, broken_provider and "provider_broken" or "missing_capability",
      broken_provider and "No functional provider for this ability" or "Actor lacks a functional provider for this ability")
  end
  local request, failure = self:_validate_ability_request(actor, ability, params or {})
  if not request then
    return failure
  end
  if not (params and params.skip_resource) then
    local consumed, resource_failure = self:_consume_ability_resource(actor, ability)
    if not consumed then return resource_failure end
  end
  local wear = self:wear_actor_component(actor, selected.slot_id, ability_id)
  if ability.implementation == "self_destruct" then
    return self:_execute_self_destruct(actor, selected.component, wear)
  elseif ability.implementation == "projectile" or ability.implementation == "piercing_projectile" then
    return self:_execute_projectile(actor, selected.component, wear, ability, request)
  elseif ability.implementation == "scattershot" then
    return self:_execute_scattershot(actor, selected.component, wear, ability, request)
  elseif ability.implementation == "area_burst" then
    return self:_execute_area_burst(actor, selected.component, wear, ability, request)
  elseif ability.implementation == "electrical_discharge" then
    return self:_execute_electrical_discharge(actor, selected.component, wear, ability, request)
  elseif ability.implementation == "melee" then
    return self:_execute_melee(actor, selected.component, wear, ability, request)
  end
  return self:_ability_failure(ability_id, "unbound_implementation", "No runtime binding for ability")
end

function Session:_reconstruction_allowed()
  if self.state.phase == "reconstruction" then return true end
  local station_id = self.state.active_reconstruction_station_id
  if not self.campaign or not station_id or not self.state.player or not self.state.world then return false end
  local station = self.state.world:get_object(station_id)
  return station and not station.destroyed and station.interaction_role == "reconstruction_station"
    and Interaction.is_adjacent(self.state.player, station)
end

function Session:reconstruction_compatibility(component_id, slot_id)
  if not self:_reconstruction_allowed() then
    return { applied = false, compatible = false, component_id = component_id, slot_id = slot_id, reason = "Reconstruction is only available between floors" }
  end
  return Reconstruction.compatibility(self.state.player.body, self.state.run.inventory, component_id, slot_id)
end

function Session:install_inventory_component(component_id, slot_id)
  if not self:_reconstruction_allowed() then
    return { applied = false, component_id = component_id, slot_id = slot_id, reason = "Reconstruction is only available between floors" }
  end
  local previous_locomotion = self:locomotion_state(self.state.player)
  local result = Reconstruction.install(self.state.player.body, self.state.run.inventory, component_id, slot_id)
  if result.applied then
    self:_log("Installed " .. self.registry:get_component(result.definition_id).display_name .. ".")
    self:_log_locomotion_transition(self.state.player, previous_locomotion, self:locomotion_state(self.state.player))
    self:_sound("pickup")
    self:validate_physical_ownership()
  else
    self:_log(result.reason)
  end
  return result
end

function Session:uninstall_body_component(slot_id)
  if not self:_reconstruction_allowed() then
    return { applied = false, slot_id = slot_id, reason = "Reconstruction is only available between floors" }
  end
  local previous_locomotion = self:locomotion_state(self.state.player)
  local result = Reconstruction.uninstall(self.state.player.body, self.state.run.inventory, self.registry, slot_id)
  if result.applied then
    self:_log("Uninstalled " .. self.registry:get_component(result.definition_id).display_name .. ".")
    self:_log_locomotion_transition(self.state.player, previous_locomotion, self:locomotion_state(self.state.player))
    self:_sound("pickup")
    self:validate_physical_ownership()
  else
    self:_log(result.reason)
  end
  return result
end

function Session:run_data()
  local player = self.state.run.player
  local charm_slots = {}
  for index = 1, RunModifiers.charm_slots(self.state) do
    if self.state.charms and self.state.charms.slots[index] then charm_slots[#charm_slots + 1] = { slot = index, charm_id = self.state.charms.slots[index] } end
  end
  local data = {
    progression = {
      stage = self.state.stage,
      score = self.state.score,
      scrap = self.state.scrap,
      charm_slots = charm_slots,
      curse_id = self.state.curse_id,
      class_name = self.state.legacy_class and self.state.legacy_class.name or nil,
      boon_name = self.state.legacy_boon and self.state.legacy_boon.name or nil,
      curse_name = self.state.curse and (self.state.curse.display_name or self.state.curse.name) or nil,
      next_component_sequence = self.state.next_component_sequence,
      next_corpse_sequence = self.state.next_corpse_sequence,
      next_world_object_sequence = self.state.next_world_object_sequence,
      next_hazard_sequence = self.state.next_hazard_sequence,
      next_fire_sequence = self.state.next_fire_sequence,
      next_item_sequence = self.state.next_item_sequence,
      route = self.state.route and self.state.route:to_data() or nil,
      run_id = self.state.run_id,
      meta_snapshot = copy_meta_snapshot(self.state.meta_snapshot),
      meta_reward_events = {},
      fallen_recurrence = FallenRecurrence.copy_spec(self.state.fallen_recurrence),
      death_pending_archive = copy_death_pending(self.state.death_pending_archive, self.registry),
    },
    body = player and player.body and player.body:to_data() or nil,
    inventory = self.state.run.inventory:to_data(),
    -- World serialisation is intentionally plain data.  A later save system
    -- can store only World:mutation_data() beside the generated floor seed.
    world = self.state.world and self.state.world:to_data() or nil,
    player = player and {
      health = player.health,
      ammo = player.ammo,
      bombs = player.bombs,
      flares = player.flares,
    } or nil,
  }
  for _, event in ipairs(self.state.meta_reward_events or {}) do
    data.progression.meta_reward_events[#data.progression.meta_reward_events + 1] = {
      id = event.id,
      amount = event.amount,
      claimed = event.claimed == true,
    }
  end
  return data
end

-- Active-run persistence deliberately captures only authoritative state.  The
-- renderer rebuilds interpolation, camera, particles, screen shake, and the
-- transient electrical flash after restoration instead of treating them as
-- simulation data.
function Session:_saved_actor_ref(actor)
  if actor and actor.actor_id then
    return actor.actor_id
  end
  if actor == self.state.player then
    return "player"
  end
  for index, enemy in ipairs(self.state.enemies or {}) do
    if enemy == actor then
      return "enemy:" .. index
    end
  end
  if actor == self.state.boss then
    return "boss"
  end
  return nil
end

local function copy_pending_telegraph(telegraph)
  if not telegraph then return nil end
  assert(type(telegraph) == "table", "Boss telegraph must be a table")
  assert(type(telegraph.ability_id) == "string" and type(telegraph.provider_component_id) == "string",
    "Boss telegraph is missing its ability provider")
  assert(type(telegraph.remaining) == "number" and telegraph.remaining >= 1 and telegraph.remaining % 1 == 0,
    "Boss telegraph has an invalid delay")
  local result = {
    ability_id = telegraph.ability_id,
    provider_component_id = telegraph.provider_component_id,
    remaining = telegraph.remaining,
    direction = telegraph.direction,
    target_x = telegraph.target_x,
    target_y = telegraph.target_y,
  }
  assert(result.direction == nil or DIRECTIONS[result.direction], "Boss telegraph has an invalid direction")
  assert(result.target_x == nil or (type(result.target_x) == "number" and result.target_x % 1 == 0), "Boss telegraph has an invalid target")
  assert(result.target_y == nil or (type(result.target_y) == "number" and result.target_y % 1 == 0), "Boss telegraph has an invalid target")
  return result
end

function Session:_actor_to_data(actor)
  if not actor then return nil end
  local data = {}
  for name, field in pairs(actor) do
    if name ~= "body" and name ~= "source_actor" and name ~= "pending_telegraph" then
      local kind = type(field)
      assert(kind == "string" or kind == "number" or kind == "boolean" or field == nil,
        "Active-run entity state must be plain scalar data")
      data[name] = field
    end
  end
  data.body = actor.body and actor.body:to_data() or nil
  data.pending_telegraph = copy_pending_telegraph(actor.pending_telegraph)
  if actor.source_actor then
    data.source_actor_ref = assert(self:_saved_actor_ref(actor.source_actor), "Saved effect references an unknown actor")
  end
  data.source_actor = nil
  return data
end

function Session:_actor_from_data(data)
  if not data then return nil end
  assert(type(data) == "table" and type(data.kind) == "string", "Actor data is invalid")
  local actor = {}
  for name, field in pairs(data) do
    if name ~= "body" and name ~= "source_actor_ref" and name ~= "pending_telegraph" then
      local kind = type(field)
      assert(kind == "string" or kind == "number" or kind == "boolean" or field == nil,
        "Saved entity state must be plain scalar data")
      actor[name] = field
    end
  end
  actor.body = data.body and Body.from_data(self.registry, data.body) or nil
  actor.pending_telegraph = copy_pending_telegraph(data.pending_telegraph)
  actor.source_actor = nil
  actor.source_actor_ref = data.source_actor_ref
  return actor
end

local function list_actor_data(session, values)
  local result = {}
  for _, value in ipairs(values or {}) do result[#result + 1] = session:_actor_to_data(value) end
  return result
end

-- Pre-route v1 saves restore their current world verbatim.  This migration
-- merely surrounds that already-authoritative world with the canonical
-- legacy forest -> cave -> dungeon route; it never regenerates a floor.
function Session:_migrate_legacy_route(progression)
  local graph = RouteGraph.new(self.seed, self.route_definitions, "route_profile.legacy.base", self.state.meta_snapshot.unlock_ids)
  local by_key = {}
  for _, id in ipairs(graph.node_order) do by_key[graph.nodes[id].key] = graph.nodes[id] end
  -- A pre-route active save has no milestone encounters in its authoritative
  -- route. Keep that shape rather than injecting either 8G or 8H bosses into
  -- a run whose current world and transition state already exist.
  local removed = {}
  for _, key in ipairs({
    "forest_milestone_boss", "cave_milestone_boss",
    "wild_second_milestone_boss", "industrial_second_milestone_boss",
    "wild_apex_boss", "industrial_apex_boss", "industrial_final_boss",
    "industrial_final_hub",
  }) do
    -- Keys introduced after a save's original route are absent only in
    -- synthetic compatibility fixtures; real 8I graphs contain all of them.
    if by_key[key] then removed[by_key[key].id] = true end
  end
  local retained = {}
  for _, id in ipairs(graph.node_order) do if not removed[id] then retained[#retained + 1] = id else graph.nodes[id] = nil end end
  graph.node_order = retained
  local direct = {}
  for _, edge in ipairs(graph.edges) do
    if not removed[edge.from] and not removed[edge.to] then direct[#direct + 1] = edge end
  end
  for _, from in ipairs({ by_key.forest_tier_2, by_key.cave_tier_2 }) do
    for _, to in ipairs({ by_key.cave_tier_3, by_key.dungeon_tier_3, by_key.reactor_tier_3, by_key.forest_tier_3_breach }) do
      direct[#direct + 1] = { from = from.id, to = to.id,
        requires_unlock = to.key == "forest_tier_3_breach" and "unlock.traversal.reinforced_breach" or nil }
    end
  end
  for _, from in ipairs({ by_key.cave_tier_3, by_key.dungeon_tier_3, by_key.reactor_tier_3, by_key.forest_tier_3_breach }) do
    direct[#direct + 1] = { from = from.id, to = by_key.legacy_shop.id }
  end
  direct[#direct + 1] = { from = by_key.legacy_shop.id, to = by_key.legacy_final_boss.id }
  graph.edges = direct
  local opening, cave, dungeon = by_key.opening_forest, by_key.cave_tier_2, by_key.dungeon_tier_3
  local shop, boss = by_key.legacy_shop, by_key.legacy_final_boss
  local function set_path(nodes, completed_count)
    graph.path, graph.completed_node_ids = {}, {}
    for index, node in ipairs(nodes) do
      graph.path[#graph.path + 1] = node.id
      if index <= completed_count then graph.completed_node_ids[node.id] = true end
    end
    graph.current_node_id = nodes[#nodes].id
  end
  local pending = progression.reconstruction_next or progression.transition_next
  if progression.phase == "boss" then
    set_path({ opening, cave, dungeon, shop, boss }, 4)
  elseif pending == "shop" then
    set_path({ opening, cave, dungeon }, 3)
  elseif pending == "curse" then
    -- The old implementation incremented stage before reconstruction. A
    -- pending second floor therefore means forest just completed, etc.
    if progression.stage <= 2 then set_path({ opening }, 1) else set_path({ opening, cave }, 2) end
  elseif progression.stage <= 1 then
    set_path({ opening }, 0)
  elseif progression.stage == 2 then
    set_path({ opening, cave }, 1)
  else
    set_path({ opening, cave, dungeon }, 2)
  end
  assert(graph:validate(self.route_definitions))
  return graph
end

function Session:to_data()
  self:validate_physical_ownership()
  if self.state.world then self:validate_world() end
  local state = self.state
  local charm_slots = {}
  for index = 1, RunModifiers.charm_slots(state) do
    if state.charms and state.charms.slots[index] then charm_slots[#charm_slots + 1] = { slot = index, charm_id = state.charms.slots[index] } end
  end
  local data = {
    seed = self.seed,
    rng = self.rng:to_data(),
    progression = {
      stage = state.stage,
      score = state.score,
      scrap = state.scrap,
      charm_slots = charm_slots,
      curse_id = state.curse_id,
      phase = state.phase,
      ended = state.ended,
      reconstruction_next = state.reconstruction_next,
      transition_next = state.transition_next,
      class_name = state.legacy_class and state.legacy_class.name or nil,
      boon_name = state.legacy_boon and state.legacy_boon.name or nil,
      curse_name = state.curse and (state.curse.display_name or state.curse.name) or nil,
      active_service_object_id = state.active_service_object_id,
      service_return_phase = state.service_return_phase,
      final_service_hub = state.final_service_hub,
      boss_completed = state.boss_completed,
      campaign_boss_site_id = state.campaign_boss_site_id,
      curse_bag = {},
      curse_options = {},
      next_component_sequence = state.next_component_sequence,
      next_actor_sequence = state.next_actor_sequence,
      next_corpse_sequence = state.next_corpse_sequence,
      next_world_object_sequence = state.next_world_object_sequence,
      next_hazard_sequence = state.next_hazard_sequence,
      next_fire_sequence = state.next_fire_sequence,
      next_item_sequence = state.next_item_sequence,
      run_id = state.run_id,
      meta_snapshot = copy_meta_snapshot(state.meta_snapshot),
      discovery_state = copy_discovery_state(state.discovery_state),
      reinforcement_state = copy_reinforcement_state(state.reinforcement_state),
      meta_reward_events = {},
      fallen_recurrence = FallenRecurrence.copy_spec(state.fallen_recurrence),
      death_pending_archive = copy_death_pending(state.death_pending_archive, self.registry),
    },
    settings = copy_plain(state.settings or {}),
    log = {},
    player = self:_actor_to_data(state.player),
    inventory = state.run.inventory:to_data(),
    enemies = list_actor_data(self, state.enemies),
    targets = list_actor_data(self, state.targets),
    bullets = list_actor_data(self, state.bullets),
    bombs = list_actor_data(self, state.bombs),
    flares = list_actor_data(self, state.flares),
    torches = list_actor_data(self, state.torches),
    area_attacks = list_actor_data(self, state.area_attacks),
    corpses = {},
    world = state.world and state.world:to_data() or nil,
    ammo = self:_actor_to_data(state.ammo),
    exit = self:_actor_to_data(state.exit),
    boss = self:_actor_to_data(state.boss),
    route = state.route and state.route:to_data() or nil,
    surface_connector_cells = copy_plain(state.surface_connector_cells or {}),
    protected_content_cells = copy_plain(state.protected_content_cells or {}),
  }
  for _, curse in ipairs(state.curse_bag or {}) do data.progression.curse_bag[#data.progression.curse_bag + 1] = curse.id or curse.name end
  for _, curse in ipairs(state.curse_options or {}) do data.progression.curse_options[#data.progression.curse_options + 1] = curse.id or curse.name end
  for _, message in ipairs(state.log or {}) do data.log[#data.log + 1] = message end
  for _, corpse in ipairs(state.corpses or {}) do data.corpses[#data.corpses + 1] = corpse:to_data() end
  for _, event in ipairs(state.meta_reward_events or {}) do
    data.progression.meta_reward_events[#data.progression.meta_reward_events + 1] = {
      id = event.id, amount = event.amount, claimed = event.claimed == true,
      kind = event.kind, discovery_id = event.discovery_id,
    }
  end
  table.sort(data.corpses, function(first, second) return first.id < second.id end)
  return data
end

function Session.from_data(data, options)
  options = options or {}
  assert(type(data) == "table" and type(data.progression) == "table", "Active run data is invalid")
  assert(type(data.seed) == "number" and type(data.rng) == "table", "Active run is missing RNG state")
  local session = Session.new({
    seed = data.seed,
    rng = Rng.from_data(data.rng),
    content = options.content or Content,
    registry = options.registry,
    route_definitions = options.route_definitions,
    emit = options.emit,
    run_id = options.run_id,
    meta_snapshot = options.meta_snapshot,
    on_meta_reward = options.on_meta_reward,
    identity_allocator = options.identity_allocator,
    campaign = options.campaign,
    campaign_state = options.campaign_state,
  })
  local progression = data.progression
  local state = session.state
  local sequences = {
    "next_component_sequence", "next_corpse_sequence", "next_world_object_sequence", "next_hazard_sequence", "next_fire_sequence",
  }
  for _, name in ipairs(sequences) do
    local value = progression[name]
    assert(type(value) == "number" and value >= 1 and value % 1 == 0, "Active run has an invalid " .. name)
    state[name] = value
  end
  state.next_item_sequence = progression.next_item_sequence or 1
  assert(type(state.next_item_sequence) == "number" and state.next_item_sequence >= 1 and state.next_item_sequence % 1 == 0,
    "Active run has an invalid next_item_sequence")
  state.next_actor_sequence = progression.next_actor_sequence or 1
  assert(type(state.next_actor_sequence) == "number" and state.next_actor_sequence >= 1
    and state.next_actor_sequence % 1 == 0, "Active run has an invalid next_actor_sequence")
  assert(type(progression.stage) == "number" and progression.stage >= 1 and progression.stage % 1 == 0,
    "Active run has an invalid stage")
  assert(type(progression.phase) == "string", "Active run has no phase")
  state.stage, state.score, state.phase = progression.stage, progression.score or 0, progression.phase
  state.scrap = progression.scrap or progression.score or 0
  state.ended, state.reconstruction_next, state.transition_next = progression.ended, progression.reconstruction_next, progression.transition_next
  state.boss_completed = progression.boss_completed
  state.campaign_boss_site_id = progression.campaign_boss_site_id
  state.legacy_class = named_content(session.content.classes, progression.class_name, "class")
  state.legacy_boon = named_content(session.content.boons, progression.boon_name, "boon")
  state.class, state.boon = nil, nil
  state.run_id = progression.run_id or options.run_id or ("legacy:" .. tostring(data.seed))
  assert(type(state.run_id) == "string" and state.run_id:match("^[%w:_%.%-]+$"), "Active run has an invalid run ID")
  state.meta_snapshot = copy_meta_snapshot(progression.meta_snapshot or options.meta_snapshot)
  for _, id in ipairs(state.meta_snapshot.unlocked_research_ids) do assert(session.registry.research[id], "Active run references unknown research ID '" .. id .. "'") end
  state.fallen_recurrence = FallenRecurrence.copy_spec(progression.fallen_recurrence)
  -- Absent state identifies a pre-8J active run. Keep that run and all of
  -- its future route floors discovery-free rather than changing its stored
  -- deterministic content midway through a descent.
  state.discovery_state = copy_discovery_state(progression.discovery_state)
  -- An absent field identifies a pre-8K active run.  It remains ecology-free
  -- on later floors rather than changing a saved deterministic descent.
  state.reinforcement_state = copy_reinforcement_state(progression.reinforcement_state)
  local recurrence_ok, recurrence_reason = FallenRecurrence.validate_spec(state.fallen_recurrence, session.registry)
  assert(recurrence_ok, recurrence_reason)
  state.death_pending_archive = copy_death_pending(progression.death_pending_archive, session.registry)
  local function resolve_curse(value)
    if not value then return nil end
    if session.registry.curses[value] then return session.registry:get_curse(value) end
    for _, curse in pairs(session.registry.curses) do if curse.display_name == value then return curse end end
    return named_content(session.content.curses, value, "curse")
  end
  state.curse = resolve_curse(progression.curse_id or progression.curse_name)
  state.curse_id = state.curse and state.curse.id or nil
  state.charms = { slots = {} }
  for index, saved in ipairs(progression.charm_slots or {}) do
    local slot, charm_id = saved.slot or index, saved.charm_id or saved
    assert(type(slot) == "number" and slot >= 1 and slot <= RunModifiers.charm_slots(state) and slot % 1 == 0
      and type(charm_id) == "string" and session.registry.charms[charm_id] and not state.charms.slots[slot], "Active run has an invalid charm slot")
    state.charms.slots[slot] = charm_id
  end
  state.active_service_object_id = progression.active_service_object_id
  state.service_return_phase = progression.service_return_phase
  state.final_service_hub = progression.final_service_hub
  state.meta_reward_events = {}
  for _, event in ipairs(progression.meta_reward_events or {}) do
    assert(type(event) == "table" and type(event.id) == "string" and event.id:match("^[%w:_%.%-]+$")
      and type(event.amount) == "number" and event.amount >= 0 and event.amount % 1 == 0,
      "Active run has an invalid meta reward event")
    if event.kind ~= nil then
      assert(event.kind == "discovery" and type(event.discovery_id) == "string"
        and event.discovery_id:match("^discovery%.[a-z0-9_%.]+$"), "Active run has an invalid meta reward event")
    elseif event.discovery_id ~= nil then
      assert(false, "Active run has an invalid meta reward event")
    end
    state.meta_reward_events[#state.meta_reward_events + 1] = {
      id = event.id, amount = event.amount, claimed = event.claimed == true,
      kind = event.kind, discovery_id = event.discovery_id,
    }
  end
  state.curse_bag, state.curse_options = {}, {}
  for _, name in ipairs(progression.curse_bag or {}) do state.curse_bag[#state.curse_bag + 1] = resolve_curse(name) end
  for _, name in ipairs(progression.curse_options or {}) do state.curse_options[#state.curse_options + 1] = resolve_curse(name) end
  if data.route then
    local graph, route_failure = RouteGraph.from_data(data.route, session.route_definitions)
    assert(graph, route_failure and route_failure.reason or "Active run route is invalid")
    state.route = graph
  else
    state.route = session:_migrate_legacy_route(progression)
  end
  state.route_node_id = state.route.current_node_id
  local route_node = state.route:node(state.route_node_id)
  state.floor_seed = route_node and (route_node.floor_seed or route_node.encounter_seed) or nil
  state.settings = copy_plain(data.settings or {})
  state.surface_connector_cells = copy_plain(data.surface_connector_cells or {})
  state.protected_content_cells = copy_plain(data.protected_content_cells or {})
  state.log = {}
  for _, message in ipairs(data.log or {}) do assert(type(message) == "string", "Active run log contains invalid data"); state.log[#state.log + 1] = message end

  -- A zone reload reuses Campaign's exact active instance. The zone shard
  -- intentionally has no player/cargo copy, so this branch is the critical
  -- no-duplication seam for cross-zone traversal.
  state.player = options.active_player or session:_actor_from_data(assert(data.player, "Active run has no player"))
  state.run.player = state.player
  state.run.inventory = options.carried_inventory or Inventory.from_data(assert(data.inventory, "Active run has no inventory"), function(item)
    return PhysicalItem.from_data(item, session.registry)
  end)
  state.inventory = state.run.inventory
  state.world = World.from_data(session.registry, assert(data.world, "Active run has no world"), session.identity_allocator or state)
  state.enemies = {}
  for _, saved in ipairs(data.enemies or {}) do state.enemies[#state.enemies + 1] = session:_actor_from_data(saved) end
  state.targets, state.bullets, state.bombs, state.flares, state.torches, state.area_attacks = {}, {}, {}, {}, {}, {}
  for _, name in ipairs({ "targets", "bullets", "bombs", "flares", "torches", "area_attacks" }) do
    for _, saved in ipairs(data[name] or {}) do state[name][#state[name] + 1] = session:_actor_from_data(saved) end
  end
  state.ammo = session:_actor_from_data(data.ammo)
  state.exit = session:_actor_from_data(data.exit)
  state.boss = session:_actor_from_data(data.boss)
  -- The legacy final encounter stored a static hitbox with no Body. Preserve
  -- the saved arena/world, but promote that actor to the physical final-boss
  -- definition exactly once on load so old mid-boss saves remain playable.
  if state.phase == "boss" and state.boss and not state.boss.body then
    local legacy_health = state.boss.health or session.registry:get_boss("boss.legacy.final").health
    local definition = session.registry:get_boss("boss.legacy.final")
    local profile = session.registry:get_boss_arena(definition.arena_profile_id)
    local upgraded = session:_make_boss(definition.id, profile.boss_spawn)
    upgraded.health = math.max(1, math.min(upgraded.max_health, legacy_health))
    state.boss = upgraded
  end
  state.corpses = {}
  for _, saved in ipairs(data.corpses or {}) do state.corpses[#state.corpses + 1] = Corpse.from_data(session.registry, saved) end
  table.sort(state.corpses, function(first, second) return first.id < second.id end)

  local function actor_from_ref(reference)
    if reference == nil then return nil end
    for _, actor in ipairs(session:_living_actors()) do
      if actor.actor_id == reference then return actor end
    end
    if reference == "player" then return state.player end
    if reference == "boss" then
      assert(state.boss, "Saved effect references a missing boss")
      return state.boss
    end
    local index = reference:match("^enemy:(%d+)$")
    assert(index and state.enemies[tonumber(index)], "Saved effect references an unknown actor")
    return state.enemies[tonumber(index)]
  end
  for _, values in ipairs({ state.bullets, state.area_attacks }) do
    for _, value in ipairs(values) do
      value.source_actor = actor_from_ref(value.source_actor_ref)
      value.source_actor_ref = nil
    end
  end
  -- Old active saves encoded enemy references by array index and did not have
  -- actor IDs. Restore them safely, then assign deterministic legacy IDs so
  -- all subsequent saves use stable references.
  for _, actor in ipairs(session:_living_actors()) do
    if not actor.actor_id then actor.actor_id = session:_allocate_actor_id(actor == state.player and "campaign" or "zone") end
  end
  -- Presentation maps/effects are intentionally rebuilt cleanly after load.
  state.effects, state.electrical_effects = {}, {}
  session:refresh_derived_player_stats()
  session:ensure_campaign_loadout()
  -- Tactical perception is transient and is rebuilt after loading. Terrain
  -- itself is never hidden or remembered as an exploration layer.
  session:refresh_visibility()
  session:validate_world()
  session:validate_physical_ownership()
  session:reconcile_meta_rewards()
  return session
end

function Session:validate_world()
  if not self.state.world then
    error("No active world to validate")
  end
  return self.state.world:validate()
end

function Session:validate_physical_ownership()
  local owners = {}
  local function record(component, owner)
    if owners[component.id] then
      error("Physical component '" .. component.id .. "' is owned by both " .. owners[component.id] .. " and " .. owner)
    end
    owners[component.id] = owner
  end
  local function record_body(body, owner)
    if body then
      for _, component in ipairs(body:list_components()) do
        record(component, owner)
      end
    end
  end

  local identities = {}
  local function record_identity(value, owner)
    assert(type(value) == "string" and value ~= "", owner .. " has no stable ID")
    if identities[value] then error("Physical identity '" .. value .. "' is used by both " .. identities[value] .. " and " .. owner) end
    identities[value] = owner
  end

  local function record_item(item, owner)
    assert(item and type(item.physical_id) == "string", owner .. " has an invalid physical item")
    record_identity(item.physical_id, owner)
    if item.item_type == "component" then record(item.object, owner) end
  end

  record_body(self.state.player and self.state.player.body, "player body")
  if self.state.player then record_identity(self.state.player.actor_id, "player actor") end
  for _, enemy in ipairs(self.state.enemies or {}) do
    record_body(enemy.body, "living enemy '" .. enemy.kind .. "'")
    record_identity(enemy.actor_id, "living enemy '" .. enemy.kind .. "'")
  end
  record_body(self.state.boss and self.state.boss.body, "living boss")
  if self.state.boss then record_identity(self.state.boss.actor_id, "living boss") end
  for _, corpse in ipairs(self.state.corpses or {}) do
    record_body(corpse.body, "corpse '" .. corpse.id .. "'")
    record_identity(corpse.id, "corpse")
    for _, entry in ipairs(corpse.carried_inventory and corpse.carried_inventory.entries or {}) do
      record_item(entry.item, "corpse cargo '" .. corpse.id .. "'")
    end
    if corpse.carried_inventory then corpse.carried_inventory:validate() end
  end
  if self.state.world then
    for _, object in ipairs(self.state.world:list_objects(true)) do record_identity(object.id, "world object") end
    for _, hazard in ipairs(self.state.world:list_hazards(true)) do record_identity(hazard.id, "hazard") end
    for _, fire in ipairs(self.state.world:list_fires(true)) do record_identity(fire.id, "fire") end
  end
  for _, entry in ipairs(self.state.inventory and self.state.inventory.entries or {}) do record_item(entry.item, "inventory") end
  for _, object in ipairs(self.state.world and self.state.world:list_objects(true) or {}) do
    for _, entry in ipairs(object.storage_inventory and object.storage_inventory.entries or {}) do
      record_item(entry.item, "storage '" .. object.id .. "'")
    end
  end
  for _, ground in ipairs(self.state.world and self.state.world:list_ground_items() or {}) do
    record_item(ground.item, "ground item '" .. ground.id .. "'")
  end
  for _, object in ipairs(self.state.world and self.state.world:list_objects(true) or {}) do
    local stock = object.service_stock
    if object.interaction_role == "service" and stock and stock.kind == "salvager" then
      for _, offer in ipairs(stock.offers or {}) do
        if not offer.sold and offer.component then
          record({ id = offer.component.id }, "service stock '" .. object.id .. "'")
        end
      end
    end
  end
  local hub = self.state.final_service_hub
  for service_id, stock in pairs(hub and hub.stocks or {}) do
    if stock.kind == "salvager" then
      for _, offer in ipairs(stock.offers or {}) do
        if not offer.sold and offer.component then record({ id = offer.component.id }, "final service stock '" .. service_id .. "'") end
      end
    end
  end
  if self.state.player and self.state.player.body then
    self.state.player.body:validate()
  end
  if self.state.run and self.state.run.inventory then
    self.state.run.inventory:validate()
  end
  return true
end

function Session:_create_corpse(actor, carried_inventory, provenance)
  if not actor.body then
    return nil
  end
  local corpse_id
  if self.identity_allocator then
    corpse_id = self.identity_allocator:allocate_corpse_id()
  else
    local sequence = self.state.next_corpse_sequence
    self.state.next_corpse_sequence = sequence + 1
    corpse_id = string.format("corpse:%06d", sequence)
  end
  local corpse = Corpse.from_actor(corpse_id, actor, carried_inventory, provenance)
  self.state.corpses[#self.state.corpses + 1] = corpse
  return corpse
end

function Session:find_corpse(corpse_id)
  for _, corpse in ipairs(self.state.corpses or {}) do
    if corpse.id == corpse_id then
      return corpse
    end
  end
  return nil
end

function Session:faced_corpse()
  local cell = self:faced_cell()
  if not cell then return nil end
  for _, corpse in ipairs(self.state.corpses or {}) do
    if corpse.x == cell.x and corpse.y == cell.y then return corpse end
  end
  return nil
end

function Session:nearby_corpse()
  local player = self.state.player
  if not player then
    return nil
  end
  for _, corpse in ipairs(self.state.corpses or {}) do
    if Grid.distance(player, corpse) <= 1 then
      return corpse
    end
  end
  return nil
end

function Session:nearby_ground_item()
  local player, world = self.state.player, self.state.world
  if not player or not world then return nil end
  local candidates = {}
  for y = player.y - 1, player.y + 1 do
    for x = player.x - 1, player.x + 1 do
      if Grid.in_bounds(x, y) then
        for _, ground in ipairs(world:ground_items_at(x, y)) do candidates[#candidates + 1] = ground end
      end
    end
  end
  table.sort(candidates, function(left, right)
    local left_distance = math.max(math.abs(left.x - player.x), math.abs(left.y - player.y))
    local right_distance = math.max(math.abs(right.x - player.x), math.abs(right.y - player.y))
    if left_distance ~= right_distance then return left_distance < right_distance end
    return left.id < right.id
  end)
  return candidates[1]
end

function Session:faced_ground_item()
  local cell, world = self:faced_cell(), self.state.world
  if not cell or not world then return nil end
  local items = world:ground_items_at(cell.x, cell.y)
  table.sort(items, function(left, right)
    -- Campaign supplies are automatic on arrival, so a deliberate physical
    -- component wins the faced-use target when both happen to share a cell.
    local left_supply = left.item.item_type == "resource_stack"
    local right_supply = right.item.item_type == "resource_stack"
    if left_supply ~= right_supply then return not left_supply end
    return left.id < right.id
  end)
  return items[1]
end

function Session:pickup_ground_item(ground_item_id)
  local ground = self.state.world and self.state.world:get_ground_item(ground_item_id)
  if not ground then return { applied = false, code = "unknown_ground_item", reason = "Ground item is unavailable" } end
  if not self.state.player or Grid.distance(self.state.player, ground) > 1 then
    return { applied = false, code = "out_of_range", reason = "Ground item is not within pickup range" }
  end
  local placement, reason = self.state.inventory:find_first_fit(ground.item)
  if not placement then return { applied = false, code = "inventory_full", reason = reason } end
  local item, removed = self.state.world:remove_ground_item(ground)
  assert(item and removed.applied, "Ground item removal failed")
  local entry, place_reason = self.state.inventory:place(item, placement.x, placement.y, placement.rotated)
  if not entry then
    local restored, restore_reason = self.state.world:place_ground_item(item, ground.x, ground.y)
    assert(restored, restore_reason and restore_reason.reason)
    return { applied = false, code = "inventory_full", reason = place_reason }
  end
  self:validate_physical_ownership()
  return { applied = true, code = "picked_up", ground_item_id = ground.id, physical_id = item.physical_id, entry = entry }
end

-- Projecting a corpse is intentionally read-only.  The dual-grid UI can be
-- rebuilt at any time from the current Body/cargo owners, so no modal layout
-- state is serialized and no item gains a second owner merely for display.
function Session:corpse_loot_grid(corpse_id)
  local corpse = self:find_corpse(corpse_id)
  if not corpse then return nil, "Unknown corpse" end
  return CorpseLootGrid.project(corpse, self.registry)
end

local function salvage_distance_allowed(session, corpse)
  return session.state.player and Grid.distance(session.state.player, corpse) <= 1
end

function Session:salvage_corpse_to_inventory(corpse_id, physical_id, placement)
  local corpse = self:find_corpse(corpse_id)
  if not corpse then return { applied = false, code = "unknown_corpse", reason = "Unknown corpse" } end
  if not salvage_distance_allowed(self, corpse) then
    return { applied = false, code = "out_of_range", reason = "Corpse is not within salvage range" }
  end
  if type(placement) ~= "table" then
    return { applied = false, code = "invalid_placement", reason = "Salvage placement is invalid" }
  end
  local projection = CorpseLootGrid.project(corpse, self.registry)
  local source, projected = projection:source(physical_id), projection.inventory:get(physical_id)
  if not source or not projected then
    return { applied = false, code = "unknown_salvage", reason = "Corpse item is unavailable" }
  end
  local item
  if source.source_kind == "body" then
    local component = corpse.body:get_component(source.slot_id)
    if not component or component.id ~= physical_id then
      return { applied = false, code = "unknown_salvage", reason = "Corpse component is unavailable" }
    end
    item = PhysicalItem.from_component(component, self.registry)
  else
    local entry = corpse.carried_inventory and corpse.carried_inventory:get(physical_id)
    if not entry then return { applied = false, code = "unknown_salvage", reason = "Corpse cargo is unavailable" } end
    item = entry.item
  end
  local allowed, reason = self.state.inventory:can_place(item, placement.x, placement.y, placement.rotated == true)
  if not allowed then return { applied = false, code = "inventory_full", reason = reason } end

  local placed
  if source.source_kind == "body" then
    -- Placement has already been authoritatively validated.  Only now is the
    -- installed part detached, so an invalid UI drop cannot strip a corpse.
    local detached, detach_reason = corpse.body:detach(source.slot_id)
    if not detached then return { applied = false, code = "detach_failed", reason = detach_reason } end
    assert(detached.id == physical_id, "Corpse detached a different physical component")
    placed, reason = self.state.inventory:place(item, placement.x, placement.y, placement.rotated == true)
    if not placed then
      local restored, restore_reason = corpse.body:install(source.slot_id, detached)
      assert(restored, restore_reason)
      return { applied = false, code = "inventory_full", reason = reason }
    end
  else
    local entry = assert(corpse.carried_inventory:get(physical_id), "Corpse cargo changed during salvage")
    local removed = corpse.carried_inventory:remove(physical_id)
    assert(removed == item, "Corpse cargo removal lost its physical item")
    placed, reason = self.state.inventory:place(item, placement.x, placement.y, placement.rotated == true)
    if not placed then
      local restored, restore_reason = corpse.carried_inventory:place(item, entry.x, entry.y, entry.rotated)
      assert(restored, restore_reason)
      return { applied = false, code = "inventory_full", reason = reason }
    end
  end
  local definition = item.item_type == "component" and self.registry:get_component(item.object.definition_id) or nil
  self:_log((source.source_kind == "body" and "SALVAGED " or "RECOVERED ")
    .. string.upper(definition and definition.display_name or item.display_name) .. ".")
  self:_sound("pickup")
  local recurrence = self.state.fallen_recurrence
  if corpse.fallen_archive_id and recurrence and recurrence.archive_id == corpse.fallen_archive_id
    and #corpse:list_components() == 0 then recurrence.resolved = true end
  self:validate_physical_ownership()
  return {
    applied = true, corpse_id = corpse_id, physical_id = physical_id,
    source_kind = source.source_kind, slot_id = source.slot_id, entry = placed,
  }
end

local function copy_inventory_for_pickup(inventory)
  local preview = Inventory.new({ width = inventory.width, height = inventory.height, thresholds = inventory.thresholds })
  for _, entry in ipairs(inventory.entries) do
    local placed, reason = preview:place(entry.item, entry.x, entry.y, entry.rotated)
    assert(placed, reason)
  end
  return preview
end

local function resource_entries_in_order(inventory, resource_id)
  local values = {}
  for _, entry in ipairs(inventory.entries) do
    if entry.item.item_type == "resource_stack" and entry.item.resource_id == resource_id then values[#values + 1] = entry end
  end
  table.sort(values, function(left, right)
    if left.y ~= right.y then return left.y < right.y end
    if left.x ~= right.x then return left.x < right.x end
    return left.physical_id < right.physical_id
  end)
  return values
end

-- A walk-over resource pickup is planned in full before either owner mutates.
-- Existing matching stacks are filled first, then deterministic empty cells
-- are considered.  A source stack is never partially taken from the ground.
function Session:_plan_resource_pickup(item)
  if not item or item.item_type ~= "resource_stack" then return nil, "Ground item is not an auto-pickup supply" end
  local inventory, remaining, merge = self.state.inventory, item.quantity, {}
  for _, entry in ipairs(resource_entries_in_order(inventory, item.resource_id)) do
    if remaining <= 0 then break end
    local amount = math.min(remaining, entry.item.max_stack - entry.item.quantity)
    if amount > 0 then
      merge[#merge + 1] = { entry = entry, amount = amount }
      remaining = remaining - amount
    end
  end
  local preview, placements, preview_index = copy_inventory_for_pickup(inventory), {}, 0
  while remaining > 0 do
    local quantity = math.min(remaining, item.max_stack)
    preview_index = preview_index + 1
    local ghost = PhysicalItem.from_resource(item.resource_id, quantity, "pickup-preview:" .. preview_index, self.registry)
    local placement = preview:find_first_fit(ghost)
    if not placement then return nil, "No space for the full supply stack" end
    local placed, reason = preview:place(ghost, placement.x, placement.y, placement.rotated)
    assert(placed, reason)
    placements[#placements + 1] = { quantity = quantity, placement = placement }
    remaining = remaining - quantity
  end
  return { merge = merge, placements = placements }
end

function Session:_pickup_resource_stack(ground)
  local plan, reason = self:_plan_resource_pickup(ground.item)
  if not plan then return { applied = false, code = "inventory_full", reason = reason } end
  local world, source, original_quantity = self.state.world, ground.item, ground.item.quantity
  local removed, remove_result = world:remove_ground_item(ground)
  assert(removed == source and remove_result.applied, "Ground resource removal failed")
  for _, change in ipairs(plan.merge) do change.entry.item:set_quantity(change.entry.item.quantity + change.amount) end
  for index, allocation in ipairs(plan.placements) do
    local item = index == 1 and source or self:create_resource_stack(source.resource_id, allocation.quantity, "zone")
    item:set_quantity(allocation.quantity)
    local placed, place_reason = self.state.inventory:place(item, allocation.placement.x, allocation.placement.y,
      allocation.placement.rotated)
    assert(placed, place_reason)
  end
  self:validate_physical_ownership()
  return { applied = true, code = "auto_picked_up", quantity = original_quantity, resource_id = source.resource_id,
    physical_id = source.physical_id }
end

function Session:_auto_pickup_ground_supplies(x, y)
  if not self.campaign or not self.state.world then return {} end
  local picked, blocked = {}, nil
  for _, ground in ipairs(self.state.world:ground_items_at(x, y)) do
    if ground.item.item_type == "resource_stack" then
      local quantity, name = ground.item.quantity, ground.item.display_name
      local result = self:_pickup_resource_stack(ground)
      if result.applied then
        picked[#picked + 1] = result
      elseif not blocked then
        blocked = { quantity = quantity, name = name }
      end
    end
  end
  if #picked > 0 then
    for _, result in ipairs(picked) do
      local definition = self.registry:get_resource(result.resource_id)
      self:_log("+" .. result.quantity .. " " .. string.upper(definition.display_name) .. ".")
    end
    self:_sound("pickup")
  end
  if blocked then self:_log("INVENTORY FULL — " .. string.upper(blocked.name) .. " LEFT ON GROUND.") end
  return picked
end

function Session:drop_inventory_item(physical_id)
  local player, inventory, world = self.state.player, self.state.inventory, self.state.world
  local entry = inventory and inventory:get(physical_id)
  if not player or not world or not entry then
    return { applied = false, code = "unknown_inventory_item", reason = "Inventory item is unavailable" }
  end
  -- World placement is checked before removal.  The only remaining mutation
  -- can then be rolled back exactly if a future world rule rejects it.
  if not Grid.in_bounds(player.x, player.y) or not world:terrain_is_passable(player.x, player.y) then
    return { applied = false, code = "blocked_terrain", reason = "Cannot drop cargo here" }
  end
  if world:get_ground_item(physical_id) then
    return { applied = false, code = "duplicate_id", reason = "Ground cargo identity is unavailable" }
  end
  local item, original = inventory:remove(physical_id)
  assert(item == entry.item, "Inventory removal lost its physical item")
  local ground, result = world:place_ground_item(item, player.x, player.y)
  if not ground then
    local restored, restore_reason = inventory:place(item, original.x, original.y, original.rotated)
    assert(restored, restore_reason)
    return { applied = false, code = result and result.code or "drop_failed", reason = result and result.reason or "Cannot drop cargo" }
  end
  self:validate_physical_ownership()
  self:_log("DROPPED " .. string.upper(item.display_name) .. ".")
  self:_sound("pickup")
  return { applied = true, physical_id = physical_id, item = item, ground_item_id = ground.id }
end

function Session:salvage_corpse_component(corpse_id, slot_id)
  local corpse = self:find_corpse(corpse_id)
  if not corpse then
    return { applied = false, corpse_id = corpse_id, slot_id = slot_id, reason = "Unknown corpse" }
  end
  if not self.state.player or Grid.distance(self.state.player, corpse) > 1 then
    return { applied = false, corpse_id = corpse_id, slot_id = slot_id, reason = "Corpse is not within salvage range" }
  end
  local result = Salvage.component(corpse, slot_id, self.state.inventory, self.registry)
  if result.applied then
    local definition = self.registry:get_component(result.definition_id)
    self:_log("Salvaged " .. definition.display_name .. ".")
    self:_sound("pickup")
    local recurrence = self.state.fallen_recurrence
    if corpse.fallen_archive_id and recurrence and recurrence.archive_id == corpse.fallen_archive_id
      and #corpse:list_components() == 0 then
      recurrence.resolved = true
    end
    self:validate_physical_ownership()
  else
    self:_log(result.reason)
  end
  return result
end

function Session:salvage_corpse_carried_component(corpse_id, component_id)
  local corpse = self:find_corpse(corpse_id)
  if not corpse then
    return { applied = false, corpse_id = corpse_id, component_id = component_id, reason = "Unknown corpse" }
  end
  if not self.state.player or Grid.distance(self.state.player, corpse) > 1 then
    return { applied = false, corpse_id = corpse_id, component_id = component_id, reason = "Corpse is not within salvage range" }
  end
  local result = Salvage.carried_component(corpse, component_id, self.state.inventory)
  if result.applied then
    local definition = result.definition_id and self.registry:get_component(result.definition_id)
    self:_log("Recovered " .. (definition and definition.display_name or "carried component") .. ".")
    self:_sound("pickup")
    self:validate_physical_ownership()
  else
    self:_log(result.reason)
  end
  return result
end

function Session:salvage_corpse_carried_item(corpse_id, physical_id)
  local corpse = self:find_corpse(corpse_id)
  if not corpse then return { applied = false, corpse_id = corpse_id, physical_id = physical_id, reason = "Unknown corpse" } end
  if not self.state.player or Grid.distance(self.state.player, corpse) > 1 then
    return { applied = false, corpse_id = corpse_id, physical_id = physical_id, reason = "Corpse is not within salvage range" }
  end
  local result = Salvage.carried_item(corpse, physical_id, self.state.inventory)
  if result.applied then
    self:_log("Recovered " .. result.entry.item.display_name .. ".")
    self:_sound("pickup")
    self:validate_physical_ownership()
  else
    self:_log(result.reason)
  end
  return result
end

function Session:_log_component_transition(actor, result)
  if not result.applied or not result.condition_changed then
    return
  end
  local definition = self.registry:get_component(result.definition_id)
  local owner
  if actor == self.state.player then
    owner = string.upper((result.slot_id or "body"):gsub("_", " ")) .. " — "
  else
    local enemy = actor.content_id and self.registry.enemies[actor.content_id]
    owner = string.upper((enemy and enemy.display_name) or actor.kind or "ENEMY") .. " — "
  end
  self:_log(owner .. string.upper(definition.display_name) .. " " .. string.upper(result.new_condition))
end

function Session:_log_capability_loss(actor, before, after)
  local current = {}
  for _, ability_id in ipairs(after or {}) do current[ability_id] = true end
  for _, ability_id in ipairs(before or {}) do
    if not current[ability_id] then
      local ability = self.registry:get_ability(ability_id)
      local owner = actor == self.state.player and "" or (string.upper(actor.kind or "ENEMY") .. " — ")
      self:_log(owner .. string.upper(ability.display_name) .. " OFFLINE")
    end
  end
end

-- Economy repair is the only ordinary path that can restore an installed
-- provider during a run.  Keep the message at the Session boundary so the
-- renderer does not have to infer gameplay state from a repair receipt.
function Session:_log_capability_restoration(actor, before, after)
  local previous = {}
  for _, ability_id in ipairs(before or {}) do previous[ability_id] = true end
  for _, ability_id in ipairs(after or {}) do
    if not previous[ability_id] then
      local ability = self.registry:get_ability(ability_id)
      local owner = actor == self.state.player and "" or (string.upper(actor.kind or "ENEMY") .. " — ")
      self:_log(owner .. string.upper(ability.display_name) .. " RESTORED")
    end
  end
end

function Session:_log_locomotion_transition(actor, previous, current)
  if actor ~= self.state.player or not previous or previous.state == current.state then
    return
  end
  if current.state == Locomotion.NORMAL then
    self:_log("LOCOMOTION NORMAL.")
  elseif current.state == Locomotion.IMPAIRED then
    self:_log("LOCOMOTION IMPAIRED.")
  else
    self:_log("CRAWLING.")
  end
end

function Session:damage_actor_body(actor, damage_spec)
  if not actor.body then
    return {
      applied = false,
      cause = damage_spec and damage_spec.cause or "kinetic",
      reason = "Actor has no body",
    }
  end
  local spec = {}
  for key, value in pairs(damage_spec or {}) do
    spec[key] = value
  end
  spec.rng = spec.rng or self.rng
  local previous_locomotion = self:locomotion_state(actor)
  local before_capabilities = actor.body:list_capabilities()
  local result = BodyDamage.apply(actor.body, spec)
  self:_log_component_transition(actor, result)
  if result.became_broken then
    self:_log_capability_loss(actor, before_capabilities, actor.body:list_capabilities())
  end
  local current_locomotion = self:locomotion_state(actor)
  result.previous_locomotion = previous_locomotion.state
  result.locomotion = current_locomotion.state
  result.locomotion_provider_count = current_locomotion.provider_count
  self:_log_locomotion_transition(actor, previous_locomotion, current_locomotion)
  return result
end

function Session:_enemy_index(actor)
  for index, enemy in ipairs(self.state.enemies or {}) do
    if enemy == actor then
      return index
    end
  end
  return nil
end

-- Hazards and impacts are world-caused injury, but they deliberately retain
-- the current staged HP model while also using localized body damage.
function Session:_apply_world_actor_damage(actor, amount, message, provenance)
  provenance = provenance or {}
  if provenance.source_actor and provenance.source_actor ~= actor then
    self:_notify_combat(provenance.source_actor, actor)
  end
  -- Environmental kinetic injury chooses among components that can still take
  -- integrity damage. This keeps a wall slam or spike entry meaningful even
  -- after a different limb has already failed, while remaining deterministic.
  local damage_spec = {
    amount = amount,
    cause = provenance.cause or "kinetic",
    source = provenance.source,
    source_actor_id = provenance.source_actor_id,
    source_component_id = provenance.source_component_id,
    ability_id = provenance.ability_id,
  }
  if actor.body then
    local candidates = {}
    for _, slot in ipairs(actor.body:list_installed_slots()) do
      if slot.component.current_integrity > 0 then
        candidates[#candidates + 1] = slot
      end
    end
    if #candidates > 0 then
      -- Gas exposure must not consume combat/session RNG: diffusion itself is
      -- RNG-free and an environmental medium should not perturb later rolls.
      damage_spec.slot_id = provenance.deterministic_target and candidates[1].slot_id
        or self.rng:choice(candidates).slot_id
    end
  end
  -- Legacy bombs and self-destruct blasts apply direct health damage before
  -- their force/impact body injury. Shared weapons still take the normal
  -- localized-damage path.
  local body_damage = provenance.skip_body_damage and { applied = false, skipped = true }
    or self:damage_actor_body(actor, damage_spec)
  local state = self.state
  local dead = false
  if actor == state.player then
    actor.health = math.max(0, actor.health - amount)
    -- Only blocked external force creates the short impact recovery state.
    -- Electrical, fire, and gas exposure remain direct environmental damage.
    if provenance.source == "impact" then
      actor.impact = 2
    end
    self:_event("hit")
    self:_sound("hurt")
    if message then
      self:_log(message)
    end
    if actor.health == 0 then
      self:_mark_player_dead(provenance)
      dead = true
    end
  else
    actor.health = math.max(0, (actor.health or 0) - amount)
    if actor.health == 0 then
      if actor == state.boss then
        self:_defeat_boss(actor)
      else
        local index = self:_enemy_index(actor)
        if index then
          self:_destroy_enemy(index, { player_caused = provenance.source_actor == state.player })
        end
      end
      dead = true
    end
  end
  local data = {
    applied = true,
    amount = amount,
    body_damage = body_damage,
    dead = dead,
    provenance = provenance,
  }
  return data
end

function Session:_apply_hazard_effect(actor, hazard, definition, context)
  local effect = definition.effect
  assert(effect.type == "kinetic_damage", "Unsupported hazard effect '" .. tostring(effect.type) .. "'")
  local result = self:_apply_world_actor_damage(actor, effect.amount,
    actor == self.state.player and "SPIKES TEAR INTO YOU." or nil, {
      cause = "kinetic",
      source = "hazard",
      hazard_id = hazard.id,
      hazard_definition_id = definition.id,
      source_actor = context and context.force and context.force.source_actor or nil,
      source_actor_id = context and context.force and context.force.source_actor_id or nil,
      source_component_id = context and context.force and context.force.source_component_id or nil,
      ability_id = context and context.force and context.force.ability_id or nil,
      x = hazard.x,
      y = hazard.y,
      context = context,
    })
  result.hazard_id = hazard.id
  result.hazard_definition_id = definition.id
  result.x, result.y = hazard.x, hazard.y
  return result
end

function Session:_resolve_actor_hazard_entry(actor, x, y, context)
  local result = Hazards.on_actor_enter(self.state.world, actor, x, y, context,
    function(target, hazard, definition, entry_context)
      return self:_apply_hazard_effect(target, hazard, definition, entry_context)
    end)
  return result
end

function Session:_actors_at(x, y)
  local actors = {}
  local player = self.state.player
  if player and player.x == x and player.y == y then
    actors[#actors + 1] = player
  end
  for _, enemy in ipairs(self.state.enemies or {}) do
    if enemy.x == x and enemy.y == y then
      actors[#actors + 1] = enemy
    end
  end
  local boss = self.state.boss
  if boss and boss.x == x and boss.y == y then
    actors[#actors + 1] = boss
  end
  return actors
end

function Session:_apply_fire_exposure(actor, fire, target, x, y)
  return self:_apply_world_actor_damage(actor, 1,
    actor == self.state.player and "FIRE SCORCHES YOU." or nil, {
      cause = "thermal",
      source = "fire",
      fire_id = fire.id,
      fire_target_kind = target.target_kind,
      fire_target_id = target.target_id,
      x = x,
      y = y,
      ignition_provenance = fire.provenance,
    })
end

function Session:_update_fire()
  if not self.state.world then
    return { applied = false, code = "no_world" }
  end
  return Fire.tick(self.state.world, {
    actors_at = function(x, y)
      return self:_actors_at(x, y)
    end,
    on_actor_exposed = function(actor, fire, target, x, y)
      return self:_apply_fire_exposure(actor, fire, target, x, y)
    end,
  })
end

function Session:_update_liquids()
  if not self.state.world then
    return { applied = false, code = "no_world" }
  end
  local flow = Liquid.tick(self.state.world)
  local suppression = Liquid.suppress_fires(self.state.world)
  return { applied = flow.applied or suppression.applied, flow = flow, suppression = suppression }
end

function Session:_apply_gas_exposure(actor, gas, definition)
  return self:_apply_world_actor_damage(actor, definition.damage,
    actor == self.state.player and "TOXIC GAS BURNS YOU." or nil, {
      cause = "toxic",
      source = "gas",
      gas_id = definition.id,
      concentration = gas.concentration,
      x = gas.x,
      y = gas.y,
      -- See _apply_world_actor_damage: this preserves gas's no-RNG contract.
      deterministic_target = true,
    })
end

-- Gas first commits its synchronous diffusion, then exposes living actors at
-- post-diffusion concentration. Corpses and actors removed by earlier fire
-- processing never appear in _actors_at and therefore receive no exposure.
function Session:_update_gas()
  local world = self.state.world
  if not world then
    return { applied = false, code = "no_world" }
  end
  local diffusion = Gas.tick(world)
  local exposures = {}
  if not self.state.ended then
    for _, gas in ipairs(world:list_gases()) do
        local definition = self.registry:get_gas(gas.gas_id)
        if gas.concentration >= definition.exposure_threshold and definition.damage > 0 then
          for _, actor in ipairs(self:_actors_at(gas.x, gas.y)) do
          if not self.state.ended and (actor.health == nil or actor.health > 0) then
            local exposure = self:_apply_gas_exposure(actor, gas, definition)
            exposures[#exposures + 1] = exposure
          end
        end
      end
    end
  end
  return {
    applied = diffusion.applied or #exposures > 0,
    diffusion = diffusion,
    exposures = exposures,
  }
end

function Session:_resolve_force_impact(actor, force_result, force_spec)
  local impact = Impact.from_force(actor, force_result, force_spec)
  if not impact.applied then
    return impact
  end
  local damage = self:_apply_world_actor_damage(actor, impact.severity,
    actor == self.state.player and "YOU SLAM INTO THE WALL." or nil, {
      cause = impact.cause,
      source = "impact",
      source_actor = force_spec and force_spec.source_actor or nil,
      source_actor_id = impact.source_actor_id,
      source_component_id = impact.source_component_id,
      ability_id = impact.ability_id,
      force_cause = impact.force_cause,
      blocker_code = impact.blocker_code,
      x = impact.x,
      y = impact.y,
    })
  impact.damage = damage
  impact.dead = damage.dead
  return impact
end

function Session:wear_actor_component(actor, slot_id, source)
  if not actor.body then
    return {
      applied = false,
      cause = "wear",
      slot_id = slot_id,
      reason = "Actor has no body",
    }
  end
  local component = actor.body:get_component(slot_id)
  if not component then
    return {
      applied = false,
      cause = "wear",
      slot_id = slot_id,
      reason = "Body slot '" .. tostring(slot_id) .. "' is empty",
    }
  end
  local definition = self.registry:get_component(component.definition_id)
  if definition.wear_per_use == 0 then
    return {
      applied = false,
      cause = "wear",
      slot_id = slot_id,
      component_id = component.id,
      definition_id = component.definition_id,
      reason = "Component has no usage wear",
    }
  end
  local previous_locomotion = self:locomotion_state(actor)
  local before_capabilities = actor.body:list_capabilities()
  local result = BodyDamage.apply_wear(actor.body, {
    amount = definition.wear_per_use,
    slot_id = slot_id,
    source = source,
  })
  self:_log_component_transition(actor, result)
  if result.became_broken then
    self:_log_capability_loss(actor, before_capabilities, actor.body:list_capabilities())
  end
  local current_locomotion = self:locomotion_state(actor)
  result.previous_locomotion = previous_locomotion.state
  result.locomotion = current_locomotion.state
  result.locomotion_provider_count = current_locomotion.provider_count
  self:_log_locomotion_transition(actor, previous_locomotion, current_locomotion)
  return result
end

function Session:_apply(settings, modifiers)
  settings.health = settings.health + (modifiers.health or 0)
  for name, value in pairs(modifiers) do
    if name == "score" then
      settings.objective_required = (settings.objective_required or 0) + value
    elseif name ~= "health" then
      settings[name] = (settings[name] or 0) + value
    end
  end
  settings.health = clamp(settings.health, 1, 5)
  settings.ammo = math.max(0, settings.ammo)
  settings.bombs = math.max(0, settings.bombs)
  settings.flares = math.max(0, settings.flares)
  settings.vision = math.max(1, settings.vision)
  settings.enemies = math.max(0, settings.enemies)
  settings.torches = math.max(0, settings.torches)
  settings.objective_required = math.max(1, settings.objective_required)
  settings.score = settings.objective_required -- legacy test/tool compatibility, never currency.
  settings.dash_cooldown = math.max(1, settings.dash_cooldown)
end

-- Legacy direct-stage construction remains available to old tests and the
-- developer inspector. It maps the former numbered prototype stages onto the
-- canonical biome/tier pairing; normal runs select explicit route nodes.
function Session:_legacy_floor_reference(stage)
  local legacy = self.content.stages[stage]
  assert(legacy, "Unknown legacy stage " .. tostring(stage))
  local biome_id = "biome.legacy." .. legacy.terrain
  local tier_id = "tier.legacy." .. tostring(stage)
  return self.route_definitions:get_biome(biome_id), self.route_definitions:get_tier(tier_id)
end

function Session:_settings_for_floor(biome, tier)
  assert(self.route_definitions:biome_supports_tier(biome.id, tier.id),
    "Biome/tier combination is not supported: " .. biome.id .. " / " .. tier.id)
  local settings = Grid.copy(tier.settings)
  settings.terrain = biome.terrain
  settings.biome_id = biome.id
  settings.biome_display_name = biome.display_name
  settings.tier_id = tier.id
  settings.tier = tier.number
  settings.enemy_family = biome.enemy_family
  settings.wilds = biome.enemy_family == "wilds"
  settings.cultists = biome.enemy_family == "cultists"
  settings.industrial = biome.enemy_family == "industrial"
  settings.room_corpus_id = biome.room_corpus_id
  settings.health, settings.bombs, settings.flares = 2, 1, 1
  settings.torch_radius, settings.dash_cooldown = 4, 3
  settings.bomb_radius, settings.bomb_fuse = 2, 3
  settings.bullet_range, settings.reload_penalty = nil, 0
  local modifiers = RunModifiers.values(self.state, self.registry)
  for key, value in pairs(modifiers) do
    if key == "objective_required" or key == "dash_cooldown" or key == "bomb_radius" or key == "flare_light" or key == "reload_bonus" then
      settings[key] = (settings[key] or 0) + value
    end
  end
  -- Existing saved class/boon selections remain mechanically valid for that
  -- saved run. New runs never create these fields.
  if self.state.legacy_class then self:_apply(settings, self.state.legacy_class.modifiers) end
  if self.state.legacy_boon then self:_apply(settings, self.state.legacy_boon.modifiers) end
  if self.state.curse and not self.state.curse_id then self:_apply(settings, self.state.curse.modifiers) end
  settings.objective_required = math.max(1, settings.objective_required)
  settings.score = settings.objective_required
  return settings
end

function Session:_settings_for_stage()
  local biome, tier = self:_legacy_floor_reference(self.state.stage)
  return self:_settings_for_floor(biome, tier)
end

function Session:_occupied(include_boss)
  local state = self.state
  local occupied = { [Grid.key(state.player.x, state.player.y)] = true }
  for _, values in ipairs({
    state.targets, state.enemies, state.corpses, state.bullets, state.bombs, state.flares, state.torches,
  }) do
    for _, value in ipairs(values) do
      occupied[Grid.key(value.x, value.y)] = true
    end
  end
  for _, object in ipairs(state.world and state.world:list_objects() or {}) do
    occupied[Grid.key(object.x, object.y)] = true
  end
  if state.ammo then
    occupied[Grid.key(state.ammo.x, state.ammo.y)] = true
  end
  if state.exit then
    occupied[Grid.key(state.exit.x, state.exit.y)] = true
  end
  if include_boss then
    local boss = state.boss
    if boss then occupied[Grid.key(boss.x, boss.y)] = true end
  end
  return occupied
end

function Session:_open_location(used, minimum, avoid_hazards, rng)
  local options = {}
  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do
      local location_key = Grid.key(x, y)
      local point = Grid.cell(x, y)
      if self:_open(x, y) and not used[location_key]
        and not (self.state.surface_connector_cells and self.state.surface_connector_cells[location_key])
        and (not avoid_hazards or not self.state.world:is_hazardous(x, y))
        and (not minimum or Grid.distance(point, self.state.player) >= minimum) then
        options[#options + 1] = point
      end
    end
  end
  assert(#options > 0, "No spawn location available")
  return (rng or self.rng):choice(options)
end

function Session:_reachable_floor_cells()
  local world, player = self.state.world, self.state.player
  if not world or not player then return {} end
  local start = Grid.cell(player.x, player.y)
  if not world:is_passable(start.x, start.y) then return {} end
  local visited, queue, cursor = { [Grid.key(start.x, start.y)] = true }, { start }, 1
  while queue[cursor] do
    local current = queue[cursor]
    cursor = cursor + 1
    for _, neighbour in ipairs(Grid.neighbours(current)) do
      local cell_key = Grid.key(neighbour.x, neighbour.y)
      if Grid.in_bounds(neighbour.x, neighbour.y) and world:is_passable(neighbour.x, neighbour.y) and not visited[cell_key] then
        visited[cell_key] = true
        queue[#queue + 1] = neighbour
      end
    end
  end
  return queue
end

function Session:_current_floor_depth()
  local route = self.state.route
  local node = route and route:node(route.current_node_id)
  if node and node.type == "floor" then
    -- Route graph depth includes non-floor milestone nodes. Recurrence depth
    -- is intentionally the player-facing normal-floor ordinal (tier 1–3),
    -- so inserting bosses never moves a stored future encounter.
    return self.route_definitions:get_tier(node.tier_id).number, node
  end
  -- Read-only generation tooling may inject a synthetic recurrence without
  -- constructing or mutating a real route graph.
  if self.state.inspection_floor_depth then
    return self.state.inspection_floor_depth, { id = "inspection", type = "floor", depth = self.state.inspection_floor_depth }
  end
  return nil, nil
end

function Session:_spawn_fallen_recurrence()
  local state, spec = self.state, self.state.fallen_recurrence
  if not spec or spec.spawned or spec.resolved then return nil end
  local depth, node = self:_current_floor_depth()
  if not depth or depth ~= spec.target_depth then return nil end
  local occupied, candidates = self:_occupied(), {}
  for _, point in ipairs(self:_reachable_floor_cells()) do
    local cell_key = Grid.key(point.x, point.y)
    if not occupied[cell_key] and Grid.distance(state.player, point) >= 6
      and not state.world:is_hazardous(point.x, point.y)
      and not state.world:is_harmful_gas_at(point.x, point.y)
      and #state.world:fires_at(point.x, point.y) == 0 then
      candidates[#candidates + 1] = point
    end
  end
  local placement_rng = Rng.new(spec.encounter_seed):derive("placement." .. node.id)
  local selected
  for _, point in ipairs(placement_rng:shuffle(candidates)) do
    selected = point
    break
  end
  if not selected then
    spec.spawned, spec.resolved, spec.skipped = true, true, true
    spec.placement_failure = { code = "no_valid_recurrence_placement", reason = "No reachable safe recurrence location" }
    state.generation_warnings[#state.generation_warnings + 1] = spec.placement_failure
    return nil, spec.placement_failure
  end
  local body = FallenRecurrence.materialize_body(self, spec)
  if spec.mode == "corpse" then
    local shell = entity("player", selected.x, selected.y, {
      actor_id = self:_allocate_actor_id("zone"),
      body = body,
      fallen_archive_id = spec.archive_id,
      fallen_source_run_id = spec.source_run_id,
    })
    self:_create_corpse(shell)
  else
    state.enemies[#state.enemies + 1] = entity("fallen_echo", selected.x, selected.y, {
      actor_id = self:_allocate_actor_id("zone"),
      health = FallenRecurrence.ECHO_HEALTH,
      ammo = 4,
      attack = 0,
      attack_kind = nil,
      attack_windup = 0,
      attack_x = nil,
      attack_y = nil,
      radius = 1,
      stun = 0,
      crawl_stride = 0,
      scrap_award = false,
      content_id = "enemy.dynamic.fallen_echo",
      faction_id = Factions.ECHO_ID,
      body = body,
      fallen_archive_id = spec.archive_id,
      fallen_source_run_id = spec.source_run_id,
    })
  end
  spec.spawned = true
  spec.placement = { x = selected.x, y = selected.y, node_id = node.id }
  state.generation_metadata = state.generation_metadata or {}
  state.generation_metadata.fallen_recurrence = {
    archive_id = spec.archive_id,
    mode = spec.mode,
    x = selected.x,
    y = selected.y,
    node_id = node.id,
  }
  self:validate_physical_ownership()
  return { applied = true, archive_id = spec.archive_id, mode = spec.mode, x = selected.x, y = selected.y }
end

function Session:_enemy_type(index, rng)
  local settings = self.state.settings or {}
  -- Direct numbered-stage construction is a retained developer/test
  -- compatibility surface.  Normal play and biome/tier inspector requests
  -- always select authored encounter pools; preserving this tiny historical
  -- mapping keeps old exact-stage fixtures reproducible without making a
  -- stage number authoritative for route floors.
  if self.state.legacy_stage_generation then
    local legacy = self.content.stages[self.state.stage] or {}
    if legacy.wilds then
      return index % 3 == 0 and "bomber" or "wolf"
    end
    return legacy.cultists and "cultist" or "necromancer"
  end
  local pool = self.registry:encounter_pool_for(settings.biome_id, settings.tier_id)
  assert(pool, "Missing encounter pool for active biome/tier")
  local total = 0
  for _, entry in ipairs(pool.entries) do total = total + entry.weight end
  local roll = (rng or self.rng):int(1, total)
  for _, entry in ipairs(pool.entries) do
    roll = roll - entry.weight
    if roll <= 0 then return entry.enemy_id end
  end
  error("Encounter pool weight selection fell through")
end

function Session:_infer_ai_role(enemy)
  if self:actor_has_capability(enemy, SELF_DESTRUCT_ABILITY) then return "rusher" end
  if self:actor_has_capability(enemy, ELECTRICAL_DISCHARGE_ABILITY)
    or self:actor_has_capability(enemy, ARCANE_BURST_ABILITY) then return "controller" end
  if self:actor_ability_by_implementation(enemy, "melee")
    and self:actor_ability_by_implementations(enemy, { "projectile", "scattershot", "piercing_projectile" }) then
    return "flanker"
  end
  if self:actor_ability_by_implementations(enemy, { "projectile", "scattershot", "piercing_projectile" }) then
    return "skirmisher"
  end
  if self:actor_ability_by_implementation(enemy, "melee") then return "heavy" end
  return "rusher"
end

local function ai_phase_for(actor_id)
  local total = 0
  for index = 1, #tostring(actor_id) do total = total + string.byte(tostring(actor_id), index) end
  return total % 8
end

function Session:_make_enemy(kind_or_id, point, options)
  local enemy_id = self.registry.enemies[kind_or_id] and kind_or_id or ENEMY_CONTENT_IDS[kind_or_id]
  local definition = enemy_id and self.registry:get_enemy(enemy_id) or nil
  local kind = definition and definition.kind or kind_or_id
  local enemy = entity(kind, point.x, point.y, {
    actor_id = self:_allocate_actor_id("zone"),
    health = 1,
    attack = 0,
    attack_kind = nil,
    attack_windup = 0,
    attack_x = nil,
    attack_y = nil,
    radius = 1,
    stun = 0,
    crawl_stride = 0,
    scrap_award = options and options.scrap_award == true or false,
  })
  if definition then
    enemy.content_id = definition.id
    enemy.faction_id = definition.faction_id
    enemy.body = self:_build_body(definition, "zone")
    enemy.ammo = definition.ammo
    enemy.elite = definition.elite
    enemy.ai_role = definition.ai_role or self:_infer_ai_role(enemy)
  else
    enemy.ai_role = self:_infer_ai_role(enemy)
  end
  enemy.ai_phase = ai_phase_for(enemy.actor_id)
  enemy.ai_cycle = 0
  return enemy
end

function Session:_spawn_entities(rng)
  local state = self.state
  state.targets, state.enemies, state.bullets, state.area_attacks = {}, {}, {}, {}
  state.bombs, state.flares, state.torches = {}, {}, {}
  for _ = 1, state.settings.torches do
    local point = self:_open_location(self:_occupied(), nil, nil, rng)
    state.torches[#state.torches + 1] = entity("torch", point.x, point.y, { light = state.settings.torch_radius })
  end
  for _ = 1, state.settings.targets do
    local point = self:_open_location(self:_occupied(), nil, nil, rng)
    state.targets[#state.targets + 1] = entity("target", point.x, point.y, { scrap_award = true })
  end
  local enemy_rng = rng:derive("encounter_pool")
  for index = 1, state.settings.enemies do
    local point = self:_open_location(self:_occupied(), nil, nil, rng)
    state.enemies[#state.enemies + 1] = self:_make_enemy(self:_enemy_type(index, enemy_rng), point, { scrap_award = true })
  end
  local point = self:_open_location(self:_occupied(), nil, nil, rng)
  if self.campaign then
    local item = self:create_resource_stack("resource.ammo.bullets", 4, "zone")
    local ground, result = state.world:place_ground_item(item, point.x, point.y)
    assert(ground, result and result.reason)
    state.ammo = nil
  else
    state.ammo = entity("ammo", point.x, point.y)
  end
end

function Session:_refill_entities()
  local state = self.state
  while #state.targets < state.settings.targets do
    local point = self:_open_location(self:_occupied())
    state.targets[#state.targets + 1] = entity("target", point.x, point.y, { scrap_award = false })
  end
  while #state.enemies < state.settings.enemies do
    local point = self:_open_location(self:_occupied())
    state.enemies[#state.enemies + 1] = self:_make_enemy(self:_enemy_type(#state.enemies + 1, self.rng), point, { scrap_award = false })
  end
  if not self.campaign and not state.ammo then
    local point = self:_open_location(self:_occupied())
    state.ammo = entity("ammo", point.x, point.y)
  end
end

function Session:start_run(class, boon, defer_initial_floor)
  local state = self.state
  self.state.class, self.state.boon = nil, nil
  self.state.legacy_class, self.state.legacy_boon = class, boon
  self.state.stage = 1
  self.state.score, self.state.scrap = 0, 0
  self.state.curse, self.state.curse_id = nil, nil
  self.state.charms = { slots = {} }
  self.state.curse_bag = {}
  self.state.curse_options = {}
  self.state.transition_next = nil
  self.state.final_service_hub = nil
  self.state.active_service_object_id = nil
  self.state.service_return_phase = nil
  self.state.ended = nil
  self.state.death_pending_archive = nil
  self.state.generation_warnings = {}
  state.next_component_sequence = 1
  state.next_actor_sequence = 1
  state.next_corpse_sequence = 1
  state.next_world_object_sequence = 1
  state.next_hazard_sequence = 1
  state.next_fire_sequence = 1
  state.next_item_sequence = 1
  state.run.player = nil
  state.run.inventory = Inventory.new({
    height = Inventory.DEFAULT_HEIGHT + ((state.meta_snapshot.modifiers and state.meta_snapshot.modifiers.inventory_rows) or 0),
  })
  state.inventory = state.run.inventory
  state.scrap = math.max(0, (state.meta_snapshot.modifiers and state.meta_snapshot.modifiers.starting_scrap) or 0)
  state.meta_reward_events = {}
  state.discovery_state = { enabled = true, assigned_discovery_ids = {} }
  state.reinforcement_state = { enabled = true }
  state.route = RouteGraph.new(self.seed, self.route_definitions, "route_profile.legacy.base", state.meta_snapshot.unlock_ids)
  if not defer_initial_floor then
    self:start_route_node(state.route.start_node_id)
  end
end

-- OW-01 keeps the temporary route graph available, but establishes a
-- campaign-derived seed as the authority for the active physical zone.  The
-- route node remains legacy progression metadata; it no longer supplies this
-- zone's identity or generation root.
function Session:_place_campaign_world_content(zone_sites, zone_seed)
  local state = self.state
  local metadata = { sites = {} }
  for _, site in ipairs(zone_sites or {}) do
    local rng = Rng.new(zone_seed):derive("campaign.world_site." .. site.id)
    if site.type == "service_outpost" then
      local placed, anchor = {}, nil
      for _, service_id in ipairs(site.service_ids or {}) do
        local point
        if anchor then
          local nearby = {}
          local occupied = self:_occupied()
          for y = anchor.y - 3, anchor.y + 3 do
            for x = anchor.x - 3, anchor.x + 3 do
              if math.max(math.abs(x - anchor.x), math.abs(y - anchor.y)) <= 3 and Grid.in_bounds(x, y)
                and state.world:is_passable(x, y) and not state.world:object_at(x, y)
                and not state.world:is_hazardous(x, y) and not state.world:is_harmful_gas_at(x, y)
                and #state.world:fires_at(x, y) == 0 then
                if not occupied[Grid.key(x, y)] then nearby[#nearby + 1] = { x = x, y = y } end
              end
            end
          end
          table.sort(nearby, function(a, b) return a.y == b.y and a.x < b.x or a.y < b.y end)
          if #nearby > 0 then point = rng:derive(service_id .. ".cluster"):choice(nearby) end
        end
        point = point or self:_open_location(self:_occupied(), 5, true, rng:derive(service_id))
        local stock = Economy.create_stock(self, service_id, rng:derive(service_id .. ".stock"), false)
        local kiosk, result = state.world:place_object("world_object.service.kiosk", point.x, point.y, {
          service_id = service_id, service_stock = stock, service_origin = site.id,
          world_content_site_id = site.id,
        })
        assert(kiosk, result and result.reason)
        anchor = anchor or { x = point.x, y = point.y }
        placed[#placed + 1] = kiosk.id
      end
      metadata.sites[#metadata.sites + 1] = { id = site.id, type = site.type, object_ids = placed }
    elseif site.type == "discovery_site" then
      state.discovery_state.enabled = true
      local discovery, reason = DiscoveryGeneration.place(self, rng, "campaign." .. site.id, site.discovery_id, true)
      assert(discovery, "Campaign discovery site could not be placed: " .. site.discovery_id .. " (" .. tostring(reason) .. ")")
      metadata.sites[#metadata.sites + 1] = { id = site.id, type = site.type, discovery_id = site.discovery_id,
        cache_object_id = discovery.cache_object_id }
    end
  end
  state.generation_metadata = state.generation_metadata or {}
  state.generation_metadata.world_content = metadata
end

function Session:apply_pending_campaign_world_content()
  local state = self.state
  if not state.pending_world_content_sites then return false end
  local sites, seed = state.pending_world_content_sites, state.floor_seed
  state.pending_world_content_sites = nil
  self:_place_campaign_world_content(sites, seed)
  state.discovery_state.enabled = true
  self:validate_world()
  self:validate_physical_ownership()
  return true
end

function Session:_start_campaign_boss_lair(boss_site, zone_seed)
  local state, player = self.state, self.state.player
  local definition = self.registry:get_boss(assert(boss_site.boss_id, "Boss site has no boss ID"))
  local profile = self.registry:get_boss_arena(definition.arena_profile_id)
  local arena_rng = Rng.new(zone_seed):derive("campaign.boss_arena." .. definition.id)
  player.x, player.y, player.direction, player.score, player.objective_progress = profile.player_spawn.x, profile.player_spawn.y, "w", 0, 0
  player.dash = 0
  state.settings.terrain, state.settings.vision, state.settings.arena_profile_id = profile.terrain, 99, profile.id
  state.settings.location_name = definition.display_name
  state.world = World.new(self.registry, profile.terrain, Generator.generate("arena", player, arena_rng, true), self.identity_allocator or state,
    self:_boss_arena_material_layout(profile))
  self:_apply_boss_arena_profile(profile)
  state.targets, state.enemies, state.bullets, state.area_attacks = {}, {}, {}, {}
  state.bombs, state.flares, state.torches = {}, {}, {}
  state.effects, state.electrical_effects, state.exit, state.corpses = {}, {}, nil, {}
  state.boss = self:_make_boss(definition.id, profile.boss_spawn)
  state.boss_completed = nil
  state.campaign_boss_site_id = boss_site.id
  state.protected_content_cells = { [Grid.key(profile.boss_spawn.x, profile.boss_spawn.y)] = true }
  local point = self:_open_location(self:_occupied(true))
  state.ammo = entity("ammo", point.x, point.y)
  state.phase = "boss"
  state.generation_metadata = { world_content = { sites = { { id = boss_site.id, type = "boss_site", boss_id = definition.id } } } }
  self:_log(definition.display_name .. " AWAITS.")
  self:refresh_visibility()
  self:validate_world()
  self:validate_physical_ownership()
end

function Session:start_campaign_zone(zone_key, zone_seed, profile_id, class, boon, options)
  options = options or {}
  assert(self.identity_allocator, "Campaign zones require a zone identity allocator")
  assert(type(zone_seed) == "number" and zone_seed % 1 == 0 and zone_seed > 0, "Campaign zone seed is invalid")
  self:start_run(class, boon, true)
  local state, route = self.state, self.state.route
  local node = assert(route and route:node(route.current_node_id), "Campaign zone requires an opening route node")
  local profile = assert(CAMPAIGN_ZONE_PROFILES[profile_id], "Unknown campaign zone profile '" .. tostring(profile_id) .. "'")
  local biome = self.route_definitions:get_biome(profile.biome_id)
  local tier = self.route_definitions:get_tier(profile.tier_id)
  local settings = self:_settings_for_floor(biome, tier)
  -- In campaign mode services and discoveries are placed only through the
  -- persistent world plan. Legacy route floors retain their own generation.
  settings.location_name = options.location_name
  settings.ecology_profile_id = profile.ecology_profile_id
  state.stage = tier.number
  state.floor_seed = zone_seed
  state.zone_key = { world_x = zone_key.world_x, world_y = zone_key.world_y, z = zone_key.z }
  state.zone_profile_id = profile_id
  state.discovery_state = { enabled = false, assigned_discovery_ids = state.discovery_state.assigned_discovery_ids or {} }
  self:_start_floor(settings, Rng.new(zone_seed), "campaign." .. tostring(zone_key))
  self:ensure_campaign_loadout()
  if options.boss_site then
    self:_start_campaign_boss_lair(options.boss_site, zone_seed)
  else
    -- Campaign installs its protected physical topology after this call.
    -- Apply POI contents only afterwards so caches/kiosks never occupy a
    -- connector throat that will be carved into the generated terrain.
    state.pending_world_content_sites = options.zone_sites or {}
  end
  return state.world
end

function Session:_create_run_player(settings)
  local player_definition = self.registry:get_actor(PLAYER_ACTOR_ID)
  local player = entity("player", math.floor(Grid.width / 2), math.floor(Grid.height / 2), {
    actor_id = self:_allocate_actor_id("campaign"),
    direction = "w",
    health = settings.health,
    ammo = settings.ammo,
    bombs = settings.bombs,
    flares = settings.flares,
    score = 0,
    objective_progress = 0,
    base_max_health = 5,
    max_health = 5,
    base_dash_cooldown = settings.dash_cooldown,
    base_bomb_radius = settings.bomb_radius,
    base_flare_light = 3,
    flare_light = 3,
    reload_bonus = 0,
    dash = 0,
    dash_base = settings.dash_cooldown,
    bomb_radius = settings.bomb_radius,
    bomb_fuse = settings.bomb_fuse,
    bullet_range = settings.bullet_range,
    reload_penalty = settings.reload_penalty,
    impact = 0,
    content_id = player_definition.id,
    faction_id = Factions.PLAYER_ID,
    body = self:_build_body(player_definition, "campaign"),
  })
  self.state.run.player = player
  return player
end

-- This deliberately does not call start_run: a successor gets a fresh body
-- and baseline consumables while the campaign wallet, map, and one-time
-- campaign-start rewards remain untouched.
function Session:make_campaign_successor()
  assert(self.campaign and self.identity_allocator, "Campaign successor requires campaign identity")
  local settings = assert(self.state.settings, "Campaign successor requires active zone settings")
  local player_definition = self.registry:get_actor(PLAYER_ACTOR_ID)
  local player = entity("player", math.floor(Grid.width / 2), math.floor(Grid.height / 2), {
    actor_id = self:_allocate_actor_id("campaign"), direction = "w",
    health = settings.health, ammo = settings.ammo, bombs = settings.bombs, flares = settings.flares,
    score = 0, objective_progress = 0, base_max_health = 5, max_health = 5,
    base_dash_cooldown = settings.dash_cooldown, base_bomb_radius = settings.bomb_radius,
    base_flare_light = 3, flare_light = 3, reload_bonus = 0, dash = 0,
    dash_base = settings.dash_cooldown, bomb_radius = settings.bomb_radius,
    bomb_fuse = settings.bomb_fuse, bullet_range = settings.bullet_range,
    reload_penalty = settings.reload_penalty, impact = 0, content_id = player_definition.id,
    faction_id = Factions.PLAYER_ID, body = self:_build_body(player_definition, "campaign"),
  })
  local modifiers = RunModifiers.values(self.state, self.registry)
  player.max_health = math.max(1, player.base_max_health + (modifiers.max_health or 0))
  player.health = player.max_health
  player.dash_base = math.max(1, player.base_dash_cooldown + (modifiers.dash_cooldown or 0))
  player.bomb_radius = math.max(1, player.base_bomb_radius + (modifiers.bomb_radius or 0))
  player.flare_light = math.max(1, player.base_flare_light + (modifiers.flare_light or 0))
  player.reload_bonus = modifiers.reload_bonus or 0
  local inventory = Inventory.new({
    height = Inventory.DEFAULT_HEIGHT + ((self.state.meta_snapshot.modifiers and self.state.meta_snapshot.modifiers.inventory_rows) or 0),
  })
  return player, inventory
end

function Session:_apply_explicit_curse_resource_effects(player)
  self:refresh_derived_player_stats()
end

function Session:_prepare_run_player_for_stage(settings)
  local persistent = self.state.run.player
  local player = persistent or self:_create_run_player(settings)
  if persistent then
    self:_apply_explicit_curse_resource_effects(player)
  end
  player.x, player.y, player.direction, player.score, player.objective_progress = math.floor(Grid.width / 2), math.floor(Grid.height / 2), "w", 0, 0
  player.dash = 0
  player.base_dash_cooldown = settings.dash_cooldown
  player.base_bomb_radius = settings.bomb_radius
  player.bomb_fuse = settings.bomb_fuse
  player.bullet_range = settings.bullet_range
  player.reload_penalty = settings.reload_penalty
  self:refresh_derived_player_stats()
  if not persistent then player.health = player.max_health end
  player.impact = 0
  self.state.player = player
  return player
end

-- Common floor construction.  Route floors receive an isolated node seed so
-- prior branch choices and combat RNG consumption cannot alter their terrain
-- or initial placements. Legacy direct-stage callers retain their historical
-- use of the session stream for tools/tests.
function Session:_start_floor(settings, floor_rng, stream_prefix)
  local state = self.state
  state.settings = settings
  self:_prepare_run_player_for_stage(settings)
  state.effects, state.electrical_effects, state.corpses = {}, {}, {}
  state.exit, state.boss = nil, nil
  state.log = {}
  state.transition_next = nil
  state.phase = "combat"
  local layout, generation_metadata = Generator.generate(settings.terrain, state.player, floor_rng, nil, {
    registry = self.registry,
    room_rng = (settings.terrain == "dungeon" or settings.terrain == "reactor") and floor_rng:derive(stream_prefix .. ".rooms") or nil,
    landmark_layout_rng = floor_rng:derive(stream_prefix .. ".landmarks.layout"),
  })
  state.generation_metadata = generation_metadata
  if generation_metadata and generation_metadata.player_spawn then
    state.player.x, state.player.y = generation_metadata.player_spawn.x, generation_metadata.player_spawn.y
  end
  state.world = World.new(self.registry, settings.terrain, layout, self.identity_allocator or state,
    generation_metadata and generation_metadata.material_layout)
  -- Terrain landmarks are deliberately earlier than ordinary cover/media so
  -- all later systems see their real physical geometry without sharing RNG.
  local landmarks = LandmarkGeneration.place(state.world, settings.terrain, state.player,
    floor_rng:derive(stream_prefix .. ".landmarks.objects"), generation_metadata and generation_metadata.landmark_layout)
  state.generation_metadata = state.generation_metadata or {}
  state.generation_metadata.landmarks = landmarks
  -- Cover uses a named deterministic stream so introducing environmental
  -- placement cannot perturb legacy actor/content RNG decisions.
  EnvironmentObjects.place(state.world, settings.terrain, state.player,
    floor_rng:derive(stream_prefix .. ".world_objects"))
  HazardGeneration.place(state.world, settings.terrain, state.player,
    floor_rng:derive(stream_prefix .. ".hazards"))
  LiquidGeneration.place(state.world, settings.terrain, state.player,
    floor_rng:derive(stream_prefix .. ".liquids"))
  GasGeneration.place(state.world, settings.terrain, state.player,
    floor_rng:derive(stream_prefix .. ".gases"))
  -- Persistent logical power is generated after independent environmental
  -- layers with its own stream, so it cannot perturb their layouts.
  PoweredDevices.place(state.world, settings.terrain, state.player,
    floor_rng:derive(stream_prefix .. ".power_devices"))
  FireGeneration.place(state.world, settings.terrain, state.player,
    floor_rng:derive(stream_prefix .. ".fires"))
  self:validate_world()
  self:_spawn_entities(floor_rng:derive(stream_prefix .. ".entities"))
  if settings.service_id then
    local service_rng = floor_rng:derive(stream_prefix .. ".services")
    local point = self:_open_location(self:_occupied(), 3, true, service_rng)
    local stock = Economy.create_stock(self, settings.service_id, service_rng, false)
    local kiosk, placed = state.world:place_object("world_object.service.kiosk", point.x, point.y, {
      service_id = settings.service_id,
      service_stock = stock,
      service_origin = settings.service_origin,
    })
    assert(kiosk, placed and placed.reason)
  end
  -- Historical recurrence is intentionally last: it observes final geometry,
  -- ordinary spawns, and the service kiosk without perturbing their streams.
  self:_spawn_fallen_recurrence()
  -- Discoveries are normal-floor-only optional content. They use their own
  -- named stream and run after recurrence so neither placement can collide.
  local discovery, discovery_reason = DiscoveryGeneration.place(self,
    floor_rng:derive(stream_prefix .. ".discoveries"), stream_prefix .. ".discoveries")
  state.generation_metadata = state.generation_metadata or {}
  state.generation_metadata.discovery = discovery
  state.generation_metadata.discovery_reason = discovery_reason
  local reinforcement, reinforcement_reason = ReinforcementGeneration.place(self,
    floor_rng:derive(stream_prefix .. ".reinforcements"), stream_prefix .. ".reinforcements")
  state.generation_metadata.reinforcement = reinforcement
  state.generation_metadata.reinforcement_reason = reinforcement_reason
  self:validate_world()
  self:validate_physical_ownership()
  self:_log("Descend into the " .. (settings.biome_display_name or settings.terrain) .. ".")
  self:refresh_visibility()
  self:validate_physical_ownership()
end

function Session:start_stage()
  local state = self.state
  state.floor_seed = nil
  state.route_node_id = nil
  state.legacy_stage_generation = true
  self:_start_floor(self:_settings_for_stage(), self.rng, "stage." .. state.stage)
end

function Session:start_route_node(node_id)
  local state, route = self.state, self.state.route
  assert(route, "Cannot start a route floor without a route graph")
  local node = assert(route:node(node_id), "Unknown route node " .. tostring(node_id))
  assert(node.type == "floor", "Route node '" .. node_id .. "' is not a floor")
  assert(route.current_node_id == node.id, "Route floor must be the current node")
  local biome = self.route_definitions:get_biome(node.biome_id)
  local tier = self.route_definitions:get_tier(node.tier_id)
  state.stage = tier.number -- compatibility depth for older HUD/tests only.
  state.route_node_id, state.floor_seed = node.id, node.floor_seed
  state.legacy_stage_generation = false
  local settings = self:_settings_for_floor(biome, tier)
  settings.service_id, settings.service_origin = node.service_id, node.id
  self:_start_floor(settings, Rng.new(node.floor_seed), "route." .. node.id)
  return node
end

-- Developer tooling may inspect any legitimate biome/tier pair without
-- constructing a full route graph. This shares the exact floor builder used
-- by route nodes and never touches active-run persistence.
function Session:start_biome_tier(biome_id, tier_id, floor_seed, service_id, options)
  local state = self.state
  options = options or {}
  local biome = self.route_definitions:get_biome(biome_id)
  local tier = self.route_definitions:get_tier(tier_id)
  floor_seed = floor_seed or self.seed
  state.route, state.route_node_id = nil, nil
  -- Inspector/batch floors are fresh synthetic constructions, never restored
  -- active runs. They therefore exercise discovery generation without any
  -- persistent profile access.
  state.discovery_state = copy_discovery_state(options.discovery_state, false)
  -- Existing isolated fixtures remain ecology-free unless tooling explicitly
  -- opts in, matching the established discovery-inspector compatibility seam.
  state.reinforcement_state = copy_reinforcement_state(options.reinforcement_state or { enabled = false }, false)
  state.legacy_stage_generation = false
  state.inspection_floor_depth = options.recurrence_depth
  state.stage, state.floor_seed = tier.number, Rng.new(floor_seed).seed
  local settings = self:_settings_for_floor(biome, tier)
  settings.service_id, settings.service_origin = service_id, "inspection"
  self:_start_floor(settings, Rng.new(floor_seed),
    "inspection." .. biome.id .. "." .. tier.id)
  return { biome = biome, tier = tier, floor_seed = state.floor_seed }
end

function Session:choose_boons(count)
  local options = self.rng:shuffle(self.content.boons)
  local result = {}
  for index = 1, math.min(count or 3, #options) do
    result[index] = options[index]
  end
  return result
end

function Session:draw_curses()
  local source = {}
  for _, curse in pairs(self.registry.curses) do source[#source + 1] = curse end
  table.sort(source, function(a, b) return a.id < b.id end)
  if #self.state.curse_bag < 3 then
    self.state.curse_bag = self.rng:shuffle(source)
  end
  self.state.curse_options = {
    table.remove(self.state.curse_bag, 1),
    table.remove(self.state.curse_bag, 1),
    table.remove(self.state.curse_bag, 1),
  }
  return self.state.curse_options
end

function Session:choose_curse(curse)
  if curse and curse.id and self.registry.curses[curse.id] then
    self.state.curse, self.state.curse_id = curse, curse.id
  else
    -- Compatibility direct callers may still pass a legacy display record.
    local found
    for _, value in pairs(self.registry.curses) do
      if string.upper(value.display_name) == string.upper(curse and curse.name or "") then found = value break end
    end
    -- Old direct callers and pre-8B saves can still carry a legacy curse
    -- record that has no equivalent in the deliberately smaller v1 corpus.
    self.state.curse, self.state.curse_id = found or curse, found and found.id or nil
  end
  local route = self.state.route
  if not route then
    self:start_stage()
    return { applied = true, next = "combat" }
  end
  local choices = route:available()
  if #choices == 1 then
    local selected = route:select(choices[1].id)
    assert(selected.applied, selected.reason)
    assert(selected.node.type == "floor", "Curse transition must lead to a floor")
    self:start_route_node(selected.node.id)
    return { applied = true, next = "combat", node = selected.node }
  end
  assert(#choices >= 2, "A curse transition requires one or more route choices")
  self.state.phase = "route"
  self.state.transition_next = nil
  self:_log("Choose the next route.")
  return { applied = true, next = "route", choices = choices }
end

function Session:available_route_nodes()
  local route = self.state.route
  return route and route:available() or {}
end

function Session:select_route_node(node_id)
  local state, route = self.state, self.state.route
  if not route then return { applied = false, code = "no_route", reason = "No route is active" } end
  if state.phase ~= "route" then return { applied = false, code = "invalid_phase", reason = "Route selection is not active" } end
  local selected, failure = route:select(node_id)
  if not selected then return { applied = false, code = failure.code, reason = failure.reason } end
  if selected.node.type ~= "floor" then return { applied = false, code = "invalid_node_type", reason = "Only floor nodes may be selected here" } end
  self:start_route_node(selected.node.id)
  return { applied = true, node = selected.node }
end

function Session:_reload(amount, cursed)
  local player = self.state.player
  player.ammo = player.ammo + math.max(0, amount - (cursed and player.reload_penalty or 0) + (player.reload_bonus or 0))
end

function Session:_hurt(message)
  local state = self.state
  state.player.health = math.max(0, state.player.health - 1)
  state.player.impact = 2
  self:_event("hit")
  self:_sound("hurt")
  self:_log(message)
  if state.player.health == 0 then
    self:_mark_player_dead({ cause = "kinetic", source = "enemy_attack" })
  end
end

function Session:_destroy_target(index)
  local target = remove(self.state.targets, index)
  self.state.player.objective_progress = self.state.player.objective_progress + 1
  self.state.player.score = self.state.player.objective_progress -- legacy data alias.
  if target.scrap_award then self.state.scrap = self.state.scrap + 1 end
  self:_reload(1, true)
  self:_sound("hit")
  self:_log("Destroyed a target.")
end

function Session:_destroy_enemy(index, context)
  local enemy = remove(self.state.enemies, index)
  self:_create_corpse(enemy)
  -- Only player-caused kills retain the historic progression/reload/SCRAP
  -- reward.  Faction-on-faction deaths still leave complete salvageable
  -- corpses, but cannot become an off-screen currency farm.
  local player_caused = context == nil or context.player_caused ~= false
  if player_caused then
    self.state.player.objective_progress = self.state.player.objective_progress + 1
    self.state.player.score = self.state.player.objective_progress
    if enemy.scrap_award then self.state.scrap = self.state.scrap + 1 end
    self:_reload(2, true)
    self:_sound("hit")
    self:_log("Defeated an enemy.")
  else
    self:_log("Hostile bodies collapse into salvage.")
  end
  self:validate_physical_ownership()
end

function Session:_damage_boss(amount)
  local boss = self.state.boss
  if not boss then return { applied = false, code = "no_boss" } end
  self:_sound("hit")
  return self:_apply_world_actor_damage(boss, amount, nil, { cause = "kinetic", source = "legacy_boss_damage" })
end

function Session:_path(start, finish, blocked, avoid_hazards)
  blocked = blocked or {}
  local queue, head = { Grid.cell(start.x, start.y) }, 1
  local previous = { [Grid.key(start.x, start.y)] = false }
  blocked[Grid.key(start.x, start.y)] = nil
  blocked[Grid.key(finish.x, finish.y)] = nil

  while queue[head] do
    local point = queue[head]
    head = head + 1
    if point.x == finish.x and point.y == finish.y then
      local result = {}
      while point do
        table.insert(result, 1, point)
        point = previous[Grid.key(point.x, point.y)]
      end
      return result
    end
    for _, neighbour in ipairs(Grid.neighbours(point)) do
      local location_key = Grid.key(neighbour.x, neighbour.y)
      local is_destination = neighbour.x == finish.x and neighbour.y == finish.y
      local dangerous = self.state.world:is_hazardous(neighbour.x, neighbour.y)
        or #self.state.world:fires_at(neighbour.x, neighbour.y) > 0
        or self.state.world:is_harmful_gas_at(neighbour.x, neighbour.y)
        or self:is_flare_controlled(neighbour.x, neighbour.y)
      if self:_open(neighbour.x, neighbour.y) and not blocked[location_key] and previous[location_key] == nil
        and (not avoid_hazards or is_destination or not dangerous) then
        previous[location_key] = point
        queue[#queue + 1] = neighbour
      end
    end
  end
  return {}
end

function Session:_hazard_aware_path(start, finish, blocked)
  local safe = self:_path(start, finish, blocked, true)
  if #safe > 0 then
    return safe, true
  end
  return self:_path(start, finish, blocked, false), false
end

-- A flare's afterglow is an enemy-denial zone. The player can cross it
-- freely, while hostile route selection avoids it whenever a safe path exists.
function Session:is_flare_controlled(x, y)
  for _, flare in ipairs(self.state.flares or {}) do
    if flare.detonated and (flare.light_remaining or 0) > 0
      and Grid.distance(flare, { x = x, y = y }) <= (flare.radius or 0) then
      return true
    end
  end
  return false
end

function Session:_blast(origin, radius)
  local result, queue, head = {}, { Grid.cell(origin.x, origin.y) }, 1
  local steps = { [Grid.key(origin.x, origin.y)] = 0 }
  while queue[head] do
    local point = queue[head]
    head = head + 1
    local location_key = Grid.key(point.x, point.y)
    local distance = steps[location_key]
    if distance <= radius and self:_open(point.x, point.y) and not result[location_key] then
      result[location_key] = true
      for _, neighbour in ipairs(Grid.neighbours(point)) do
        local neighbour_key = Grid.key(neighbour.x, neighbour.y)
        if Grid.in_bounds(neighbour.x, neighbour.y) and not steps[neighbour_key] then
          steps[neighbour_key] = distance + 1
          queue[#queue + 1] = neighbour
        end
      end
    end
  end
  return result
end

function Session:_damage_terrain_radius(origin, radius, damage_spec)
  local results = {}
  for x = origin.x - radius, origin.x + radius do
    for y = origin.y - radius, origin.y + radius do
      if Grid.in_bounds(x, y) and math.abs(x - origin.x) + math.abs(y - origin.y) <= radius then
        local result = self:damage_terrain(x, y, damage_spec)
        if result.applied or result.code == "indestructible" then
          results[#results + 1] = result
        end
      end
    end
  end
  return results
end

function Session:_damage_environment_radius(origin, radius, damage_spec)
  local results = {
    terrain = self:_damage_terrain_radius(origin, radius, damage_spec),
    objects = {},
  }
  for _, object in ipairs(self.state.world:list_objects()) do
    if Grid.distance(origin, object) <= radius then
      local result = self:damage_world_object(object, damage_spec)
      if result.applied then
        results.objects[#results.objects + 1] = result
      end
    end
  end
  return results
end

function Session:_ignite_flammable_radius(origin, radius, provenance)
  local results = {}
  for x = origin.x - radius, origin.x + radius do
    for y = origin.y - radius, origin.y + radius do
      if Grid.in_bounds(x, y) and math.abs(x - origin.x) + math.abs(y - origin.y) <= radius then
        local terrain = self:ignite_terrain(x, y, provenance)
        if terrain.applied then
          results[#results + 1] = terrain
        end
        local object = self.state.world:object_at(x, y)
        if object then
          local object_result = self:ignite_world_object(object, provenance)
          if object_result.applied then
            results[#results + 1] = object_result
          end
        end
      end
    end
  end
  return results
end

function Session:_apply_explosion_force(origin, radius, blast_cells, force_spec)
  local results, state = { actors = {}, objects = {} }, self.state
  local actors = { state.player }
  for _, enemy in ipairs(state.enemies) do
    actors[#actors + 1] = enemy
  end
  if state.boss then actors[#actors + 1] = state.boss end
  -- Actor ordering is deterministic: player first, then living-enemy list
  -- order. A blocked destination simply ends that target's displacement;
  -- there is intentionally no actor-chain pushing.
  for _, actor in ipairs(actors) do
    if actor and blast_cells[Grid.key(actor.x, actor.y)] then
      local result = self:apply_force(actor, {
        dx = actor.x - origin.x,
        dy = actor.y - origin.y,
        distance = force_spec.distance,
        cause = force_spec.cause,
        source_actor = force_spec.source_actor,
        source_actor_id = force_spec.source_actor_id,
        source_component_id = force_spec.source_component_id,
        ability_id = force_spec.ability_id,
      })
      results.actors[#results.actors + 1] = { target = actor, result = result }
    end
  end
  -- Objects use the same discrete service. They are considered geometrically
  -- within the blast even though intact cover itself blocks blast propagation.
  for _, object in ipairs(self.state.world:list_objects()) do
    if Grid.distance(origin, object) <= radius then
      local result = self:apply_force(object, {
        dx = object.x - origin.x,
        dy = object.y - origin.y,
        distance = force_spec.distance,
        cause = force_spec.cause,
        source_actor = force_spec.source_actor,
        source_actor_id = force_spec.source_actor_id,
        source_component_id = force_spec.source_component_id,
        ability_id = force_spec.ability_id,
      })
      results.objects[#results.objects + 1] = { target = object, result = result }
    end
  end
  return results
end

function Session:_update_bullets()
  local state, remaining = self.state, {}
  for _, bullet in ipairs(state.bullets) do
    -- Old active saves and compatibility fixtures predate persisted effect
    -- actor references. Those bullets represented ordinary player fire; give
    -- them that bounded legacy meaning instead of making them inert. New
    -- ecology projectiles always retain their exact source actor.
    local source_actor = bullet.source_actor
    if not source_actor and bullet.source_side ~= "enemy" then
      source_actor = state.player
      bullet.source_actor = source_actor
    end
    if bullet.active then
      if bullet.max and bullet.travel >= bullet.max then
        bullet.expired = true
      else
        local direction = DIRECTIONS[bullet.direction]
        self:_move_entity(bullet, bullet.x + direction[1], bullet.y + direction[2])
        bullet.travel = bullet.travel + 1
      end
    else
      bullet.active = true
    end

    local hit = bullet.expired
    if not hit then
      local cover = state.world:object_at(bullet.x, bullet.y)
      if cover and cover.blocks_projectiles then
        local result = self:damage_world_object(cover, {
          amount = bullet.damage or 1,
          cause = "kinetic",
          source = bullet.ability_id or "bullet",
          source_actor = source_actor,
          source_actor_id = source_actor and (source_actor.content_id or source_actor.kind) or nil,
          source_component_id = bullet.source_component_id,
          ability_id = bullet.ability_id,
        })
        if result.applied then
          state.effects[Grid.key(bullet.x, bullet.y)] = true
          if result.destroyed then
            self:_log("Destroyed " .. self.registry:get_world_object(result.definition_id).display_name .. ".")
          end
        end
        hit = true
      elseif state.world:blocks_projectile(bullet.x, bullet.y) then
        hit = true
      end
    end
    local player_owned = source_actor == state.player or bullet.source_side == "player"
    if player_owned then
      for index = #state.targets, 1, -1 do
        local target = state.targets[index]
        if target.x == bullet.x and target.y == bullet.y then
          self:_destroy_target(index)
          hit = true
          break
        end
      end
    end
    local target = not hit and self:_actor_at(bullet.x, bullet.y, source_actor) or nil
    if target and source_actor and self:are_hostile(source_actor, target) then
      self:_apply_world_actor_damage(target, bullet.damage or 1,
        target == state.player and "An enemy projectile struck you." or nil, {
          cause = "kinetic",
          source = bullet.ability_id or "bullet",
          source_actor = source_actor,
          source_actor_id = source_actor.content_id or source_actor.kind,
          source_component_id = bullet.source_component_id,
          ability_id = bullet.ability_id,
        })
      -- A piercing lance crosses one damaged body, but intact terrain and
      -- projectile-blocking cover above always terminate it immediately.
      -- The projectile advances on later turns, so this cannot hit the same
      -- actor a second time while remaining in its cell.
      if (bullet.pierce_remaining or 0) > 0 then
        bullet.pierce_remaining = bullet.pierce_remaining - 1
      else
        hit = true
      end
    end
    if not hit then
      remaining[#remaining + 1] = bullet
    end
  end
  state.bullets = remaining
end

function Session:_update_bombs()
  local state, remaining = self.state, {}
  for _, bomb in ipairs(state.bombs) do
    local source_actor = bomb.source_actor or state.player
    bomb.source_actor = source_actor
    local source_actor_id = bomb.source_actor_id or (source_actor.content_id or source_actor.kind)
    bomb.fuse = bomb.fuse - 1
    if bomb.fuse > 0 then
      remaining[#remaining + 1] = bomb
    else
      self:_damage_environment_radius(bomb, bomb.radius, {
        amount = 2,
        cause = "explosive",
        source = "bomb",
        source_actor = source_actor,
        source_actor_id = source_actor_id,
      })
      local cells = self:_blast(bomb, bomb.radius)
      for location_key in pairs(cells) do
        state.effects[location_key] = true
      end
      self:_sound("boom")
      for index = #state.targets, 1, -1 do
        local target = state.targets[index]
        if cells[Grid.key(target.x, target.y)] then
          self:_destroy_target(index)
        end
      end
      for _, actor in ipairs(self:_living_actors()) do
        if cells[Grid.key(actor.x, actor.y)] then
          self:_apply_world_actor_damage(actor, actor == state.player and 1 or 2,
            actor == state.player and "You were caught in the blast." or nil, {
              cause = "explosive", source = "bomb", source_actor = source_actor,
              source_actor_id = source_actor_id,
              skip_body_damage = true,
            })
        end
      end
      -- Explosion ordering is deliberate: environment damage, direct blast
      -- actor damage, then stepwise force. Each force step can resolve an
      -- on-enter hazard; a structural block then resolves its actor impact.
      self:_apply_explosion_force(bomb, bomb.radius, cells, {
        distance = 1,
        cause = "explosive",
        source_actor = source_actor,
        source_actor_id = source_actor_id,
      })
    end
  end
  state.bombs = remaining
end

function Session:_clear_enemy_attack(enemy)
  enemy.attack, enemy.attack_kind, enemy.attack_windup = 0, nil, 0
  enemy.attack_x, enemy.attack_y = nil, nil
end

function Session:_detonate_flare(flare)
  local state, cells = self.state, self:_blast(flare, flare.radius)
  for location_key in pairs(cells) do
    state.effects[location_key] = true
  end
  self:_sound("flare")

  local stunned = 0
  for _, enemy in ipairs(state.enemies) do
    if cells[Grid.key(enemy.x, enemy.y)] then
      enemy.stun = math.max(enemy.stun or 0, flare.stun)
      self:_clear_enemy_attack(enemy)
      self:_cancel_area_attacks_from(enemy)
      stunned = stunned + 1
    end
  end
  local boss_stunned = false
  if state.boss and cells[Grid.key(state.boss.x, state.boss.y)] then
    state.boss.stun = math.max(state.boss.stun or 0, flare.boss_stun or 1)
    state.boss.pending_telegraph = nil
    self:_cancel_area_attacks_from(state.boss)
    boss_stunned = true
  end

  -- Flares ignite surviving material fuel without applying explosive damage.
  -- Newly created fires wait until a later world fire tick.
  self:_ignite_flammable_radius(flare, flare.radius, {
    source = "flare",
    source_actor_id = flare.source_actor_id,
  })
  flare.detonated = true
  flare.light_remaining = flare.light_duration or 3
  self:_log("FLARE FLASH — " .. stunned .. " HOSTILES STUNNED" .. (boss_stunned and " • BOSS DISRUPTED" or "") .. ".")
end

function Session:_update_flares()
  local state, remaining = self.state, {}
  for _, flare in ipairs(state.flares) do
    if not flare.detonated then
      -- Old saves can retain a pre-overhaul lit flare. Honour its remaining
      -- fuse once; newly deployed flares use fuse 0 and burst immediately.
      flare.fuse = flare.fuse or 0
      if flare.fuse > 0 then
        flare.fuse = flare.fuse - 1
      end
      if flare.fuse <= 0 then self:_detonate_flare(flare) end
    elseif flare.light_remaining then
      flare.light_remaining = flare.light_remaining - 1
    end
    if flare.detonated and (flare.light_remaining or 0) > 0 then
      remaining[#remaining + 1] = flare
    elseif not flare.detonated then
      remaining[#remaining + 1] = flare
    end
  end
  state.flares = remaining
end

function Session:_area_attack_cells(attack)
  local cells = {}
  for x = attack.x - attack.radius, attack.x + attack.radius do
    for y = attack.y - attack.radius, attack.y + attack.radius do
      if Grid.in_bounds(x, y) then
        cells[Grid.key(x, y)] = true
      end
    end
  end
  return cells
end

function Session:_actor_has_pending_area_attack(actor)
  for _, attack in ipairs(self.state.area_attacks or {}) do
    if attack.source_actor == actor then
      return true
    end
  end
  return false
end

function Session:_cancel_area_attacks_from(actor)
  local remaining = {}
  for _, attack in ipairs(self.state.area_attacks or {}) do
    if attack.source_actor ~= actor then
      remaining[#remaining + 1] = attack
    end
  end
  self.state.area_attacks = remaining
end

function Session:_resolve_area_attack(attack)
  local state = self.state
  local cells = self:_area_attack_cells(attack)
  for location_key in pairs(cells) do
    state.effects[location_key] = true
  end
  if attack.source_actor == state.player or attack.source_side == "player" then
    for index = #state.targets, 1, -1 do
      local target = state.targets[index]
      if cells[Grid.key(target.x, target.y)] then
        self:_destroy_target(index)
      end
    end
  end
  for _, target in ipairs(self:_living_actors()) do
    if target ~= attack.source_actor and cells[Grid.key(target.x, target.y)]
      and attack.source_actor and self:are_hostile(attack.source_actor, target) then
      self:_apply_world_actor_damage(target, 1,
        target == state.player and "A " .. attack.source_actor_kind .. " spell struck you." or nil, {
          cause = "arcane", source = attack.ability_id, source_actor = attack.source_actor,
          source_actor_id = attack.source_actor.content_id or attack.source_actor.kind,
          source_component_id = attack.source_component_id, ability_id = attack.ability_id,
        })
    end
  end
end

function Session:_update_area_attacks()
  local remaining = {}
  for _, attack in ipairs(self.state.area_attacks or {}) do
    if attack.active then
      attack.remaining = attack.remaining - 1
    else
      attack.active = true
    end
    if attack.remaining <= 0 then
      self:_resolve_area_attack(attack)
    else
      remaining[#remaining + 1] = attack
    end
  end
  self.state.area_attacks = remaining
end

function Session:_attack_cells(enemy)
  local cells = {}
  if enemy.attack == 0 then
    return cells
  end
  for x = enemy.attack_x - enemy.radius, enemy.attack_x + enemy.radius do
    for y = enemy.attack_y - enemy.radius, enemy.attack_y + enemy.radius do
      if Grid.in_bounds(x, y) then
        cells[Grid.key(x, y)] = true
      end
    end
  end
  return cells
end

function Session:_begin_enemy_attack(enemy, kind, target, radius, windup)
  enemy.attack, enemy.attack_kind, enemy.attack_windup = 1, kind, windup
  enemy.attack_x, enemy.attack_y, enemy.radius = target.x, target.y, radius
end

function Session:_resolve_enemy_attack(enemy, index)
  local cells = self:_attack_cells(enemy)
  if enemy.attack_kind ~= "detonate" then
    for _, target in ipairs(self:_living_actors()) do
      if target ~= enemy and cells[Grid.key(target.x, target.y)] and self:are_hostile(enemy, target) then
        self:_apply_world_actor_damage(target, 1,
          target == self.state.player and "A " .. enemy.kind .. " spell struck you." or nil, {
            cause = "arcane", source = "legacy_enemy_attack", source_actor = enemy,
            source_actor_id = enemy.content_id or enemy.kind,
          })
      end
    end
  end
  if enemy.attack_kind == "detonate" then
    self:activate_actor_ability(enemy, SELF_DESTRUCT_ABILITY)
  else
    self:_clear_enemy_attack(enemy)
  end
end

function Session:_projectile_direction_to(actor, target)
  local dx, dy = target.x - actor.x, target.y - actor.y
  if dx == 0 and dy == 0 then return nil end
  local horizontal = dx == 0 and "" or (dx > 0 and "d" or "a")
  local vertical = dy == 0 and "" or (dy > 0 and "w" or "s")
  local direction
  if dx == 0 then direction = vertical
  elseif dy == 0 then direction = horizontal
  elseif math.abs(dx) == math.abs(dy) then direction = vertical .. horizontal
  end
  if direction and DIRECTIONS[direction] and self:_has_line_of_sight(actor.x, actor.y, target.x, target.y) then
    return direction
  end
  return nil
end

function Session:_melee_direction_to(actor, target)
  if Grid.distance(actor, target) > 1 then return nil end
  local dx, dy = target.x - actor.x, target.y - actor.y
  local horizontal = dx == 0 and "" or (dx > 0 and "d" or "a")
  local vertical = dy == 0 and "" or (dy > 0 and "w" or "s")
  local direction = vertical .. horizontal
  return DIRECTIONS[direction] and direction or nil
end

-- One capability-driven policy covers authored enemies and fallen echoes.
-- Content changes body configuration; a broken provider therefore removes the
-- corresponding decision without a new enemy-kind branch.
function Session:_move_enemy_route(enemy, route)
  if not route or #route <= 1 then return { applied = false, code = "no_route" } end
  local destination = route[2]
  local destination_key = Grid.key(destination.x, destination.y)
  if self:_actor_at(destination.x, destination.y, enemy)
    or (self._navigation and self._navigation.reserved[destination_key]) then
    return { applied = false, code = "blocked_actor" }
  end
  local result = self:_move_actor(enemy, destination.x, destination.y)
  if result.applied and self._navigation then self._navigation.reserved[destination_key] = true end
  return result
end

function Session:_retreat_enemy(enemy, target)
  local options, current_distance = {}, Grid.distance(enemy, target)
  for _, neighbour in ipairs(Grid.neighbours(enemy)) do
    local key = Grid.key(neighbour.x, neighbour.y)
    if Grid.in_bounds(neighbour.x, neighbour.y) and self:_open(neighbour.x, neighbour.y)
      and not self:_navigation_is_dangerous(neighbour.x, neighbour.y)
      and not self:_actor_at(neighbour.x, neighbour.y, enemy)
      and not (self._navigation and self._navigation.reserved[key])
      and Grid.distance(neighbour, target) > current_distance then
      options[#options + 1] = neighbour
    end
  end
  if #options == 0 then return { applied = false, code = "no_retreat" } end
  local index = ((enemy.ai_phase or 0) + (enemy.ai_cycle or 0)) % #options + 1
  local destination = options[index]
  local result = self:_move_actor(enemy, destination.x, destination.y)
  if result.applied and self._navigation then self._navigation.reserved[Grid.key(destination.x, destination.y)] = true end
  return result
end

function Session:_body_enemy_turn(enemy, target, route, electrical_direction)
  local projectile_direction = self:_projectile_direction_to(enemy, target)
  local projectile_ability = self:actor_ability_by_implementations(enemy, { "projectile", "scattershot", "piercing_projectile" })
  local melee_ability = self:actor_ability_by_implementation(enemy, "melee")
  local melee_direction = self:_melee_direction_to(enemy, target)
  local role = enemy.ai_role or self:_infer_ai_role(enemy)
  local distance = math.max(0, #route - 1)
  local can_fire = projectile_ability and projectile_direction
  if can_fire then
    local ability = self.registry:get_ability(projectile_ability)
    can_fire = (enemy.ammo or 0) >= (ability.resource and ability.resource.amount or 0)
  end
  local function fire()
    return self:activate_actor_ability(enemy, projectile_ability, { direction = projectile_direction })
  end
  if self:_actor_has_pending_area_attack(enemy) then
    return
  elseif self:actor_has_capability(enemy, SELF_DESTRUCT_ABILITY) and distance <= 1 then
    self:_begin_enemy_attack(enemy, "detonate", target, 0, 1)
  elseif role == "controller" and electrical_direction and self:actor_has_capability(enemy, ELECTRICAL_DISCHARGE_ABILITY) then
    self:activate_actor_ability(enemy, ELECTRICAL_DISCHARGE_ABILITY, { direction = electrical_direction })
  elseif role == "controller" and self:actor_has_capability(enemy, ARCANE_BURST_ABILITY) and distance <= 4 then
    self:activate_actor_ability(enemy, ARCANE_BURST_ABILITY, { target = target })
  elseif melee_ability and melee_direction then
    self:activate_actor_ability(enemy, melee_ability, { direction = melee_direction })
  elseif (role == "skirmisher" or role == "controller") and distance <= 2 then
    self:_retreat_enemy(enemy, target)
  elseif role == "flanker" and (enemy.ai_cycle or 0) % 3 ~= 0 then
    self:_move_enemy_route(enemy, route)
  elseif can_fire then
    fire()
  else
    self:_move_enemy_route(enemy, route)
  end
end

function Session:_enemy_turn()
  self:_navigation_begin()
  for index = #self.state.enemies, 1, -1 do
    local enemy = self.state.enemies[index]
    if enemy.stun > 0 then
      enemy.stun = enemy.stun - 1
    elseif enemy.attack == 0 then
      enemy.ai_cycle = (enemy.ai_cycle or 0) + 1
      local target, route = self:_nearest_hostile_target(enemy)
      if target and enemy.body then
        self:_body_enemy_turn(enemy, target, route, self:_electrical_direction_to(enemy, target))
      elseif self:_actor_has_pending_area_attack(enemy) then
      elseif target and #route > 0 and #route - 1 <= 4 then
        self:_begin_enemy_attack(enemy, "spell", target, 1, 3)
      elseif target and #route > 1 then
        self:_move_enemy_route(enemy, route)
      end
    else
      enemy.attack = enemy.attack + 1
      if enemy.attack > enemy.attack_windup then
        self:_resolve_enemy_attack(enemy, index)
      end
    end
  end
  self:_navigation_finish()
end

local ELECTRICAL_AIM_ORDER = { "w", "d", "s", "a", "ne", "se", "sw", "nw" }

function Session:_electrical_direction_to(actor, target)
  if not actor or not target or not self:actor_has_capability(actor, ELECTRICAL_DISCHARGE_ABILITY) then
    return nil
  end
  local ability = self.registry:get_ability(ELECTRICAL_DISCHARGE_ABILITY)
  for _, direction in ipairs(ELECTRICAL_AIM_ORDER) do
    local delta = DIRECTIONS[direction]
    local origin = { x = actor.x + delta[1], y = actor.y + delta[2] }
    if Grid.in_bounds(origin.x, origin.y) then
      local trace = Electricity.trace(self.state.world, origin, { max_cells = ability.max_cells })
      local reaches_target, reaches_actor = false, false
      for _, cell in ipairs(trace.reached_cells or {}) do
        reaches_target = reaches_target or (cell.x == target.x and cell.y == target.y)
        reaches_actor = reaches_actor or (cell.x == actor.x and cell.y == actor.y)
      end
      if reaches_target and not reaches_actor then
        return direction
      end
    end
  end
  return nil
end

function Session:_boss_definition(boss)
  return self.registry:get_boss((boss and boss.boss_id) or "boss.legacy.final")
end

function Session:_boss_telegraph_cells(boss)
  local pending, cells = boss and boss.pending_telegraph, {}
  if not pending then return cells end
  local ability = self.registry:get_ability(pending.ability_id)
  if ability.implementation == "area_burst" then
    local radius = ability.radius or 1
    for x = pending.target_x - radius, pending.target_x + radius do
      for y = pending.target_y - radius, pending.target_y + radius do
        if Grid.in_bounds(x, y) then cells[Grid.key(x, y)] = true end
      end
    end
  elseif ability.implementation == "electrical_discharge" and pending.direction then
    local delta = DIRECTIONS[pending.direction]
    local trace = Electricity.trace(self.state.world, { x = boss.x + delta[1], y = boss.y + delta[2] }, { max_cells = ability.max_cells })
    for _, cell in ipairs(trace.reached_cells or {}) do cells[Grid.key(cell.x, cell.y)] = true end
  elseif (ability.implementation == "projectile" or ability.implementation == "scattershot"
    or ability.implementation == "piercing_projectile") and pending.direction then
    local delta = DIRECTIONS[pending.direction]
    local x, y = boss.x, boss.y
    for _ = 1, ability.range or Grid.width do
      x, y = x + delta[1], y + delta[2]
      if not Grid.in_bounds(x, y) or self.state.world:blocks_projectile(x, y) then break end
      cells[Grid.key(x, y)] = true
    end
  elseif ability.implementation == "melee" and pending.direction then
    local delta = DIRECTIONS[pending.direction]
    cells[Grid.key(boss.x + delta[1], boss.y + delta[2])] = true
  end
  return cells
end

function Session:_schedule_boss_telegraph(boss, ability_id, params)
  local provider = self:actor_ability_provider(boss, ability_id)
  if not provider then return false end
  local ability = self.registry:get_ability(ability_id)
  boss.pending_telegraph = {
    ability_id = ability_id,
    provider_component_id = provider.component.id,
    direction = params.direction,
    target_x = params.target and params.target.x or nil,
    target_y = params.target and params.target.y or nil,
    remaining = 1,
  }
  self:_log((self:_boss_definition(boss).display_name or "BOSS") .. " TELEGRAPHS " .. string.upper(ability.display_name) .. ".")
  return true
end

function Session:_resolve_boss_telegraph(boss)
  local pending = boss.pending_telegraph
  if not pending then return false end
  local component = boss.body and boss.body:find_component(pending.provider_component_id)
  if not component or not Component.is_functional(component.component) then
    boss.pending_telegraph = nil
    self:_log("BOSS TELEGRAPH CANCELLED — PROVIDER DISABLED.")
    return true
  end
  pending.remaining = pending.remaining - 1
  if pending.remaining > 0 then return true end
  local params = {
    provider_component_id = pending.provider_component_id,
    direction = pending.direction,
    target = pending.target_x and { x = pending.target_x, y = pending.target_y } or nil,
  }
  boss.pending_telegraph = nil
  local result = self:activate_actor_ability(boss, pending.ability_id, params)
  if not result.applied then self:_log("BOSS TELEGRAPH FIZZLED.") end
  return true
end

function Session:_boss_telegraph_request(boss, ability_id)
  local ability = self.registry:get_ability(ability_id)
  local target = self.state.player
  if ability.implementation == "projectile" or ability.implementation == "scattershot"
    or ability.implementation == "piercing_projectile" then
    local direction = self:_projectile_direction_to(boss, target)
    if direction then return { direction = direction } end
  elseif ability.implementation == "electrical_discharge" then
    local direction = self:_electrical_direction_to(boss, target)
    if direction then return { direction = direction } end
  elseif ability.implementation == "area_burst" then
    if Grid.distance(boss, target) <= (ability.range or 5) then return { target = { x = target.x, y = target.y } } end
  elseif ability.implementation == "melee" then
    local direction = self:_melee_direction_to(boss, target)
    if direction then return { direction = direction } end
  end
  return nil
end

function Session:_boss_turn()
  local boss = self.state.boss
  if not boss then return end
  if (boss.stun or 0) > 0 then
    boss.stun = boss.stun - 1
    return
  end
  if boss.pending_telegraph then
    self:_resolve_boss_telegraph(boss)
    return
  end
  local profile = self:_boss_definition(boss).ai_profile
  for _, ability_id in ipairs(profile.telegraph_ability_ids or {}) do
    local request = self:_boss_telegraph_request(boss, ability_id)
    if request and self:_schedule_boss_telegraph(boss, ability_id, request) then return end
  end
  local blocked = {}
  for _, enemy in ipairs(self.state.enemies or {}) do blocked[Grid.key(enemy.x, enemy.y)] = true end
  local route = self:_hazard_aware_path(boss, self.state.player, blocked)
  self:_body_enemy_turn(boss, self.state.player, route, self:_electrical_direction_to(boss, self.state.player))
end

function Session:_collect_ammo()
  local state = self.state
  if self.campaign then return end
  if state.ammo and state.player.x == state.ammo.x and state.player.y == state.ammo.y then
    self:_reload(1, false)
    state.ammo = nil
    self:_sound("pickup")
    self:_log("Collected ammo.")
  end
end

function Session:_move_player(direction)
  local player, delta = self.state.player, DIRECTIONS[direction]
  if not delta then
    return { applied = false, code = "invalid_direction", reason = "Unknown movement direction" }
  end
  if self.campaign and math.abs(delta[1]) + math.abs(delta[2]) ~= 1 then
    return { applied = false, code = "invalid_player_direction", reason = "Campaign movement is cardinal only" }
  end

  local destination_x, destination_y = player.x + delta[1], player.y + delta[2]
  -- Terrain failure is deliberately checked before facing changes.  Walking
  -- into a wall is not a turn-in-place action in the Campaign control model.
  local movement = self:validate_actor_movement(player, delta[1], delta[2])
  if not movement.applied then
    if movement.code == "blocked_terrain" then
      self:_log("A wall blocks your path.")
    elseif movement.code == "crawl_cannot_move_diagonally" then
      self:_log("CRAWLING: cardinal movement only.")
    else
      self:_log(movement.reason)
    end
    return movement
  end

  local occupant = self:_actor_at(destination_x, destination_y, player)
  if occupant and (occupant.health == nil or occupant.health > 0) then
    -- Occupancy is physical rather than an implicit attack.  The action is
    -- still consumed by Session:turn, giving the existing enemy response
    -- pipeline its ordinary opportunity to act.
    player.direction = direction
    local hostile = self:are_hostile(player, occupant)
    self:_event("bump", { direction = direction, hostile = hostile, actor = occupant })
    self:_sound("select")
    self:_log(hostile and "BLOCKED BY HOSTILE." or "BLOCKED BY ACTOR.")
    return {
      applied = false,
      consumed = true,
      code = hostile and "enemy_bump" or "actor_blocked",
      reason = hostile and "A hostile blocks the path" or "An actor blocks the path",
      actor = occupant,
      direction = direction,
    }
  end

  player.direction = direction
  local result = self:_move_actor(player, destination_x, destination_y)
  if result.applied then
    self:_log("Moved " .. delta[3] .. ".")
    -- Supply collection is part of arriving on this successful movement
    -- action. It never invokes Session:turn again, so enemy/world response
    -- still occurs exactly once for the tile step.
    result.auto_pickup = self:_auto_pickup_ground_supplies(player.x, player.y)
  elseif result.code == "blocked_terrain" then
    self:_log("A wall blocks your path.")
  elseif result.code == "crawl_cannot_move_diagonally" then
    self:_log("CRAWLING: cardinal movement only.")
  else
    self:_log(result.reason)
  end
  return result
end

local SURFACE_DIRECTION_BY_INPUT = { w = "north", a = "west", s = "south", d = "east" }

function Session:zone_connection_label(object)
  if not self.campaign or not object or not object.zone_connection_id then return "TRAVEL" end
  local direction = object.zone_connection_direction
  local record = self.campaign.active_zone
  local connection = record and WorldTopology.connection_at(record, direction)
  if not connection or connection.id ~= object.zone_connection_id then return "TRAVEL" end
  return WorldTopology.presentation_label(connection)
end

function Session:use_zone_connection(object)
  if not self.campaign or not object or object.destroyed or not object.zone_connection_id then
    return { applied = false, code = "no_zone_connection", reason = "No active zone connection is available" }
  end
  local direction = object.zone_connection_direction
  local connection = WorldTopology.connection_at(self.campaign.active_zone, direction)
  if not connection or connection.id ~= object.zone_connection_id then
    return { applied = false, code = "no_zone_connection", reason = "Zone connection metadata is unavailable" }
  end
  local transitioned, failure = self.campaign:request_transition(direction)
  if transitioned and transitioned.applied then return transitioned end
  return failure or { applied = false, code = "no_zone_connection", reason = "Zone travel failed" }
end

function Session:open_reconstruction_station(object)
  if not self.campaign or not object or object.destroyed or object.interaction_role ~= "reconstruction_station" then
    return { applied = false, code = "invalid_station", reason = "No reconstruction station is available" }
  end
  if not Interaction.is_adjacent(self.state.player, object) then
    return { applied = false, code = "out_of_range", reason = "Reconstruction station is not adjacent" }
  end
  self.state.pending_reconstruction_station_id = object.id
  return { applied = true, code = "reconstruction_open", station_object_id = object.id }
end

function Session:is_reconstruction_anchor(object)
  local anchor = self.campaign and self.campaign.state.reconstruction_anchor
  return anchor and self.campaign.active_zone and anchor.station_object_id == object.id
    and anchor.zone_key.world_x == self.campaign.active_zone.key.world_x
    and anchor.zone_key.world_y == self.campaign.active_zone.key.world_y
    and anchor.zone_key.z == self.campaign.active_zone.key.z or false
end

function Session:set_reconstruction_anchor(object)
  if not self.campaign or not object or object.destroyed or object.interaction_role ~= "reconstruction_station" then
    return { applied = false, code = "invalid_station", reason = "No reconstruction station is available" }
  end
  if not Interaction.is_adjacent(self.state.player, object) then
    return { applied = false, code = "out_of_range", reason = "Reconstruction station is not adjacent" }
  end
  return self.campaign:set_reconstruction_anchor(object)
end

function Session:open_storage(object)
  if not self.campaign or not object or object.destroyed or object.interaction_role ~= "storage" or not object.storage_inventory then
    return { applied = false, code = "invalid_storage", reason = "Storage is unavailable" }
  end
  if not Interaction.is_adjacent(self.state.player, object) then
    return { applied = false, code = "out_of_range", reason = "Storage is not adjacent" }
  end
  self.state.pending_storage_object_id = object.id
  return { applied = true, code = "storage_open", storage_object_id = object.id }
end

function Session:storage_transfer(object_id, physical_id, direction)
  local world = self.state.world
  local object = world and world:get_object(object_id)
  if not object or object.destroyed or object.interaction_role ~= "storage" or not object.storage_inventory then
    return { applied = false, code = "invalid_storage", reason = "Storage is unavailable" }
  end
  local from, to = direction == "to_storage" and self.state.inventory or object.storage_inventory,
    direction == "to_storage" and object.storage_inventory or self.state.inventory
  if direction ~= "to_storage" and direction ~= "to_player" then
    return { applied = false, code = "invalid_direction", reason = "Storage transfer direction is invalid" }
  end
  local result, reason = from:transfer_to(to, physical_id)
  if not result then return { applied = false, code = "inventory_full", reason = reason } end
  self:validate_physical_ownership()
  return result
end

function Session:_nearby_vertical_connection()
  if not self.campaign then return nil end
  local cell, world = self:faced_cell(), self.state.world
  if not cell or not world then return nil end
  for _, object in ipairs(world:list_objects()) do
    if object.interaction_role == "zone_connection" and object.zone_connection_id
      and object.x == cell.x and object.y == cell.y then
      return object
    end
  end
  return nil
end

-- Like the cardinal edge path, explicit U travel commits before an ordinary
-- simulation turn begins. It cannot advance AI/media/reinforcements or add a
-- hidden tick; failed transfers leave the source simulator untouched.
function Session:_try_vertical_transition(input)
  if input ~= "interact" then return false end
  local object = self:_nearby_vertical_connection()
  if not object then return false end
  local transitioned = self:use_zone_connection(object)
  if transitioned and transitioned.applied then return true, transitioned end
  self:_log((transitioned and transitioned.reason) or "Travel failed.")
  return true, transitioned or { applied = false, code = "no_zone_connection", reason = "Travel failed" }
end

-- A successful campaign edge transfer is deliberately handled before an
-- ordinary turn starts. It therefore cannot advance AI, media, effects,
-- reinforcement timers, player cooldowns, or the source-zone RNG.
function Session:_try_surface_transition(input)
  local direction, player, delta = SURFACE_DIRECTION_BY_INPUT[input], self.state.player, DIRECTIONS[input]
  if not direction or not self.campaign or not player or not delta then return false end
  if Grid.in_bounds(player.x + delta[1], player.y + delta[2]) then return false end
  local transitioned, failure = self.campaign:request_transition(direction)
  if transitioned and transitioned.applied then
    self.campaign.session.state.player.direction = input
    return true, transitioned
  end
  -- An attempted connector must never spill into a normal blocked movement
  -- turn. Persistence/arrival errors leave the current zone entirely frozen.
  if failure then self:_log(failure.reason or "Travel failed.") end
  return true, failure or { applied = false, code = "no_zone_connection", reason = "Travel failed" }
end

-- A solid tile is not an action in the Campaign control model.  Run this
-- before turn bookkeeping so a wall bump neither advances the world nor
-- changes facing.  Living actors are intentionally excluded here: attempting
-- their occupied cell is the meaningful enemy-bump action handled by
-- _move_player during the normal turn.
function Session:_campaign_movement_preflight(direction)
  if not self.campaign or not DIRECTIONS[direction] then return nil end
  local player, delta = self.state.player, DIRECTIONS[direction]
  if math.abs(delta[1]) + math.abs(delta[2]) ~= 1 then
    return { applied = false, code = "invalid_player_direction", reason = "Campaign movement is cardinal only" }
  end
  local movement = self:validate_actor_movement(player, delta[1], delta[2])
  if not movement.applied then return movement end
  return nil
end

function Session:can_move(direction)
  local player, delta = self.state.player, DIRECTIONS[direction]
  if not player or not delta then
    return false, { applied = false, code = "invalid_direction", reason = "Unknown movement direction" }
  end
  if self.campaign and math.abs(delta[1]) + math.abs(delta[2]) ~= 1 then
    return false, { applied = false, code = "invalid_player_direction", reason = "Campaign movement is cardinal only" }
  end
  if self.campaign and SURFACE_DIRECTION_BY_INPUT[direction] and not Grid.in_bounds(player.x + delta[1], player.y + delta[2]) then
    local connection = self.campaign.active_zone.connections and self.campaign.active_zone.connections[SURFACE_DIRECTION_BY_INPUT[direction]]
    if connection and player.x == connection.boundary.x and player.y == connection.boundary.y then
      return true, { applied = true, code = "zone_connection", direction = SURFACE_DIRECTION_BY_INPUT[direction] }
    end
  end
  local result = self:validate_actor_movement(player, delta[1], delta[2])
  return result.applied, result
end

function Session:_dash()
  local player = self.state.player
  local locomotion = Locomotion.validate_dash(self:locomotion_state(player))
  if not locomotion.applied then
    self:_log(locomotion.reason .. ".")
    return locomotion
  end
  if player.dash > 0 then
    self:_log("Dash is recharging.")
    return { applied = false, code = "dash_recharging", reason = "Dash is recharging", locomotion = locomotion.locomotion }
  end
  local delta = DIRECTIONS[player.direction]
  local original_x, original_y = player.x, player.y
  for _ = 1, 2 do
    if self:_open(player.x + delta[1], player.y + delta[2]) then
      self:_move_entity(player, player.x + delta[1], player.y + delta[2])
      local entry = self:_resolve_actor_hazard_entry(player, player.x, player.y, { movement = "dash" })
      if entry.dead then
        break
      end
    else
      break
    end
  end
  if player.x == original_x and player.y == original_y then
    self:_log("Dash blocked.")
    return { applied = false, code = "blocked_terrain", reason = "Dash blocked", locomotion = locomotion.locomotion }
  end
  player.dash = player.dash_base
  self:_sound("step")
  self:_log("Dashed forward.")
  return { applied = true, locomotion = locomotion.locomotion }
end

function Session:_shoot(direction)
  local player = self.state.player
  if self.campaign then
    if direction then player.direction = direction end
    return self:attack_active_weapon()
  end
  player.direction = direction or player.direction
  local ranged_implementations = { "projectile", "scattershot", "piercing_projectile" }
  local ability_id = self:actor_ability_by_implementations(player, ranged_implementations)
  if not ability_id then
    for _, implementation in ipairs(ranged_implementations) do
      ability_id = self:actor_known_ability_by_implementation(player, implementation)
      if ability_id then break end
    end
  end
  if not ability_id then
    self:_log("No functional ranged weapon.")
    return self:_ability_failure(BASIC_PROJECTILE_ABILITY, "missing_capability", "No functional ranged weapon")
  end
  local result = self:activate_actor_ability(player, ability_id, {
    direction = player.direction,
  })
  if result.applied then
    return result
  end
  if result.code == "insufficient_ammo" then
    self:_log("No more ammo, find more to shoot.")
  elseif result.code == "missing_capability" or result.code == "provider_broken" then
    self:_log("No functional ranged weapon.")
  else
    self:_log(result.reason)
  end
  return result
end

function Session:_interact_player()
  local ground, result
  if self.campaign then
    local cell = self:faced_cell()
    if not cell then
      result = { applied = false, code = "not_interactable", reason = "Nothing to interact with" }
    else
      local interactions = Interaction.available_at(self, self.state.player, cell.x, cell.y)
      if #interactions > 0 then
        result = Interaction.primary_at(self, self.state.player, cell.x, cell.y)
      else
        ground = self:faced_ground_item()
      end
    end
  else
    ground = self:nearby_ground_item()
  end
  if ground then
    local pickup = self:pickup_ground_item(ground.id)
    if pickup.applied then
      self:_log("PICKED UP " .. string.upper(ground.item.display_name) .. ".")
      self:_sound("pickup")
    else
      self:_log(pickup.reason or "INVENTORY FULL.")
    end
    return pickup
  end
  result = result or Interaction.primary(self, self.state.player)
  if result.applied then
    local object = self.state.world:get_object(result.object_id)
    if result.action_id == "door.open" then
      self:_log("OPENED " .. string.upper(self.registry:get_world_object(object.definition_id).display_name) .. ".")
    elseif result.action_id == "door.close" then
      self:_log("CLOSED " .. string.upper(self.registry:get_world_object(object.definition_id).display_name) .. ".")
    elseif result.action_id == "generator.toggle" then
      self:_log(object.generator_online and "GENERATOR ONLINE." or "GENERATOR OFFLINE.")
    elseif result.action_id == "breaker.toggle" then
      local circuit = self.state.world:get_circuit(object.circuit_id)
      self:_log(circuit.enabled and "CIRCUIT ENABLED." or "CIRCUIT DISABLED.")
    elseif result.action_id == "service.open" then
      self.state.pending_service_object_id = result.service_object_id
      self:_log("SERVICE ACCESSING AFTER THIS TURN.")
    elseif result.action_id == "reconstruction.open" then
      self:_log("RECONSTRUCTION STATION READY.")
    elseif result.action_id == "reconstruction.set_anchor" then
      self:_log("RECONSTRUCTION ANCHOR SET.")
    elseif result.action_id == "storage.open" then
      self:_log("STORAGE OPENING AFTER THIS TURN.")
    elseif result.action_id == "traversal.breach" then
      self:_log(object.required_unlock == "unlock.traversal.maintenance_override"
        and "MAINTENANCE HATCH OVERRIDDEN." or "REINFORCED BARRIER BREACHED.")
    elseif result.action_id == "discovery.claim" then
      local discovery = self.registry:get_discovery(result.discovery_id)
      if result.code == "first_discovery" then
        self:_log("DISCOVERY RECORDED: " .. string.upper(discovery.display_name) .. "  +" .. result.data .. " DATA.")
      else
        self:_log("KNOWN DISCOVERY RECOVERED: +" .. result.scrap .. " SCRAP.")
      end
    elseif result.action_id == "clue.read" then
      self:_log(result.clue or result.reason or "ACCESS MARKING UNREADABLE.")
    end
    self:_sound("select")
  elseif result.code == "requires_power" then
    self:_log("NO POWER.")
  elseif result.code == "requires_unlock" then
    self:_log(result.reason or "RESEARCH REQUIRED.")
  elseif result.code == "not_interactable" then
    self:_log("NOTHING TO INTERACT WITH.")
  else
    self:_log(result.reason or "INTERACTION FAILED.")
  end
  return result
end

function Session:_service_stock(object_id)
  local service_id = type(object_id) == "string" and object_id:match("^hub:(.+)$") or nil
  if service_id then
    local hub = self.state.final_service_hub
    local stock = hub and hub.stocks and hub.stocks[service_id]
    if not stock then return nil, "invalid_service" end
    return stock, { id = object_id, service_id = service_id, interaction_role = "service", service_stock = stock }
  end
  local object = self.state.world and self.state.world:get_object(object_id)
  if not object or object.destroyed or object.interaction_role ~= "service" then return nil, "invalid_service" end
  return object.service_stock, object
end

function Session:_ensure_final_service_hub()
  if self.state.final_service_hub then return self.state.final_service_hub end
  local rng = Rng.new(self.seed):derive("economy.final_service_hub")
  local hub = { stocks = {} }
  for _, service_id in ipairs({ "service.supply.legacy", "service.repair.legacy", "service.salvager.legacy", "service.charm_vendor.legacy" }) do
    hub.stocks[service_id] = Economy.create_stock(self, service_id, rng:derive(service_id), true)
  end
  self.state.final_service_hub = hub
  return hub
end

function Session:open_service(object_id)
  local stock, object = self:_service_stock(object_id)
  if not stock then return { applied = false, code = object, reason = "Service kiosk is unavailable" } end
  self.state.active_service_object_id = object_id
  self.state.service_return_phase = tostring(object_id):match("^hub:") and "service_hub" or "combat"
  self.state.pending_service_object_id = nil
  self.state.phase = "service"
  return { applied = true, service_id = object.service_id, object_id = object_id, stock = stock }
end

function Session:close_service()
  if self.state.phase ~= "service" then return { applied = false, code = "invalid_phase", reason = "No service is open" } end
  local return_to_hub = self.state.service_return_phase == "service_hub"
  self.state.phase, self.state.active_service_object_id, self.state.service_return_phase = return_to_hub and "transition" or "combat", nil, nil
  return { applied = true, return_to_hub = return_to_hub }
end

function Session:service_options(object_id)
  local stock, object = self:_service_stock(object_id or self.state.active_service_object_id)
  if not stock then return {} end
  local service = self.registry:get_service(object.service_id)
  local options = {}
  if service.role == "supply" then
    for index, offer in ipairs(stock.offers) do options[#options + 1] = { action = "buy_supply", index = index, label = offer.label, price = offer.price, remaining = offer.remaining } end
  elseif service.role == "repair" then
    for _, component in ipairs(self.state.player.body:list_components()) do options[#options + 1] = { action = "repair", component_id = component.id, label = self.registry:get_component(component.definition_id).display_name, integrity = component.current_integrity, max_integrity = component.max_integrity, price = stock.price } end
    for _, entry in ipairs(self.state.run.inventory.entries) do
      if entry.item.item_type == "component" then
        local component = entry.item.object
        options[#options + 1] = { action = "repair", component_id = component.id, label = entry.item.display_name,
          integrity = component.current_integrity, max_integrity = component.max_integrity, price = stock.price }
      elseif entry.item.item_type == "tool" then
        local tool = entry.item.object
        options[#options + 1] = { action = "repair_tool", tool_id = tool.id, label = entry.item.display_name,
          durability = tool.current_durability, max_durability = tool.maximum_durability, price = stock.price }
      end
    end
  elseif service.role == "salvager" then
    for index, offer in ipairs(stock.offers) do
      local component = offer.component
      options[#options + 1] = { action = "buy_component", index = index, component_id = component.id, label = self.registry:get_component(component.definition_id).display_name, integrity = component.current_integrity, max_integrity = component.max_integrity, price = offer.price, sold = offer.sold }
    end
    for _, entry in ipairs(self.state.run.inventory.entries) do
      options[#options + 1] = { action = "sell_component", component_id = entry.physical_id, label = "SELL " .. entry.item.display_name, price = nil }
    end
  elseif service.role == "charm_vendor" then
    for index, offer in ipairs(stock.offers) do
      local charm = self.registry:get_charm(offer.charm_id)
      options[#options + 1] = { action = "buy_charm", index = index, charm_id = charm.id, label = charm.display_name, description = charm.description, price = charm.price, sold = offer.sold }
    end
    for slot, charm_id in ipairs(self.state.charms.slots) do
      if charm_id then options[#options + 1] = { action = "remove_charm", slot = slot, charm_id = charm_id, label = "REMOVE " .. self.registry:get_charm(charm_id).display_name } end
    end
  end
  return options
end

function Session:service_execute(option, object_id)
  if self.state.phase ~= "service" then return { applied = false, code = "invalid_phase", reason = "Service is not open" } end
  local stock, object = self:_service_stock(object_id or self.state.active_service_object_id)
  if not stock then return { applied = false, code = object, reason = "Service kiosk is unavailable" } end
  local service = self.registry:get_service(object.service_id)
  local result
  if option.action == "buy_supply" and service.role == "supply" then result = Economy.supply(self, stock, option.index)
  elseif option.action == "repair" and service.role == "repair" then result = Economy.repair(self, stock, option.component_id)
  elseif option.action == "repair_tool" and service.role == "repair" then result = Economy.repair_tool(self, stock, option.tool_id)
  elseif option.action == "buy_component" and service.role == "salvager" then result = Economy.buy_component(self, stock, option.index)
  elseif option.action == "sell_component" and service.role == "salvager" then result = Economy.sell_component(self, option.component_id)
  elseif option.action == "buy_charm" and service.role == "charm_vendor" then result = Economy.buy_charm(self, stock, option.index)
  elseif option.action == "remove_charm" and service.role == "charm_vendor" then result = Economy.remove_charm(self, option.slot)
  else return { applied = false, code = "invalid_action", reason = "Service action is unavailable" } end
  if result.applied then self:_sound("pickup"); self:validate_physical_ownership() else self:_log(result.reason) end
  return result
end

function Session:_action(input)
  local player = self.state.player
  if DIRECTIONS[input] then
    local result = self:_move_player(input)
    if result.applied then self:_sound("step") end
    return result
  end
  if player.impact > 0 then
    self:_log("You are recovering from the hit.")
    return { applied = false, code = "recovering", reason = "You are recovering from the hit" }
  end
  if input == "q" then
    return self.campaign and self:activate_active_ability() or self:_dash()
  elseif input == "swap_weapon" then
    return self.campaign and self:swap_campaign_loadout("weapon")
      or { applied = false, code = "campaign_only", reason = "Weapon swapping is available in Campaign" }
  elseif input == "swap_ability" then
    return self.campaign and self:swap_campaign_loadout("ability")
      or { applied = false, code = "campaign_only", reason = "Ability swapping is available in Campaign" }
  elseif input == "interact" then
    return self:_interact_player()
  elseif input == "e" or input == "attack" then
    return self:_shoot()
  elseif input:match("^shoot_[wasd]$") then
    return self:_shoot(input:sub(-1))
  elseif input == "b" then
    if player.bombs <= 0 then
      self:_log("No bombs left. Buy bombs in the shop.")
    else
      player.bombs = player.bombs - 1
      self.state.bombs[#self.state.bombs + 1] = entity("bomb", player.x, player.y, {
        fuse = player.bomb_fuse,
        radius = player.bomb_radius,
        light = 3,
        source_actor = player,
        source_actor_id = player.content_id or PLAYER_ACTOR_ID,
      })
      self:_sound("select")
      self:_log("Bomb armed. Move away before it explodes.")
      return { applied = true, code = "bomb_armed" }
    end
  elseif input == "f" then
    if player.flares <= 0 then
      self:_log("No flares left. Buy flares in the shop.")
    else
      player.flares = player.flares - 1
      self.state.flares[#self.state.flares + 1] = entity("flare", player.x, player.y, {
        -- The flash resolves during this turn before enemies can answer it.
        -- Its afterglow remains as a visible, hostile-avoided light zone.
        fuse = 0,
        radius = 2,
        stun = 3,
        boss_stun = 1,
        light = player.flare_light,
        light_duration = 3,
        source_actor_id = player.content_id or PLAYER_ACTOR_ID,
      })
      self:_log("Flare primed — immediate flash and afterglow.")
      return { applied = true, code = "flare_primed" }
    end
  elseif input:match("^activate_ability:") then
    return self:activate_actor_ability(player, input:sub(#"activate_ability:" + 1), {
      direction = player.direction,
    })
  else
    local recipe_id, x, y = input:match("^build:([%w%._]+):(%-?%d+):(%-?%d+)$")
    if recipe_id then
      local result = Building.place(self, recipe_id, tonumber(x), tonumber(y))
      self.state.last_build_result = result
      if result.applied then
        self:_log("BUILT " .. string.upper(self.registry:get_construction_recipe(recipe_id).display_name) .. ".")
        self:_sound("select")
      else
        self:_log(result.reason or "BUILD FAILED.")
      end
      return result
    end
  end
  return { applied = false, code = "unknown_action", reason = "Unknown action" }
end

function Session:_begin_exit()
  local point = self:_open_location(self:_occupied(), 6, true)
  self.state.exit = entity("door", point.x, point.y)
  self.state.phase = "exit"
  self:_log("All targets are down. Find the exit.")
end

function Session:_complete_stage()
  local state = self.state
  state.scrap = state.scrap + (state.settings.completion_scrap_reward or 3) -- finite authored floor-completion award.
  if state.route then
    local completed = state.route:complete_current()
    assert(completed.applied, completed.reason)
    -- Floor research rewards are persistent account milestones, not combat
    -- drops.  The event itself is saved with the run so a later resume can
    -- reconcile a profile write without granting it twice.
    self:_claim_research_reward(completed.node.id, state.settings.completion_data_reward or 1)
    local next_nodes = completed.outgoing
    assert(#next_nodes > 0, "Completed route node has no forward continuation")
    if #next_nodes == 1 and next_nodes[1].type == "boss" then
      -- A selected tier-two route feeds a forced milestone encounter. Curses
      -- affect normal floors only, so nothing leaks into this arena.
      state.curse, state.curse_id = nil, nil
      state.reconstruction_next = "boss"
    elseif #next_nodes == 1 and next_nodes[1].type == "shop" then
      -- Curses are scoped to the normal floor just completed; the service
      -- hub and boss do not inherit an expired branch burden.
      state.curse, state.curse_id = nil, nil
      state.reconstruction_next = "shop"
    else
      for _, node in ipairs(next_nodes) do assert(node.type == "floor", "Normal-floor continuation must be a floor route node") end
      self:draw_curses()
      state.reconstruction_next = "curse"
    end
  elseif state.stage < #self.content.stages then
    state.stage = state.stage + 1
    self:draw_curses()
    state.reconstruction_next = "curse"
  else
    state.reconstruction_next = "shop"
  end
  state.phase = "reconstruction"
  state.exit = nil
  self:validate_physical_ownership()
  self:_log("Reconstruction available. Reconfigure your body before continuing.")
  return "reconstruction"
end

function Session:complete_reconstruction()
  if self.campaign and self.state.active_reconstruction_station_id then
    if not self:_reconstruction_allowed() then
      return { applied = false, reason = "Reconstruction station is no longer accessible" }
    end
    self:validate_physical_ownership()
    self.state.active_reconstruction_station_id = nil
    return { applied = true, next = "combat" }
  end
  if self.state.phase ~= "reconstruction" then
    return { applied = false, reason = "No reconstruction phase is active" }
  end
  self:validate_physical_ownership()
  local result = self.state.reconstruction_next
  assert(result == "curse" or result == "shop" or result == "boss", "Reconstruction has no valid continuation")
  if result == "boss" and self.state.route then
    local choices = self.state.route:available()
    assert(#choices == 1 and choices[1].type == "boss", "Route has no milestone boss continuation")
    local selected = self.state.route:select(choices[1].id)
    assert(selected.applied, selected.reason)
    self.state.route_node_id, self.state.floor_seed = selected.node.id, selected.node.encounter_seed
    self.state.reconstruction_next, self.state.transition_next = nil, nil
    self:start_boss()
    return { applied = true, next = "boss", node = selected.node }
  end
  if result == "shop" and self.state.route then
    local choices = self.state.route:available()
    assert(#choices == 1 and choices[1].type == "shop", "Route has no shop continuation")
    local selected = self.state.route:select(choices[1].id)
    assert(selected.applied, selected.reason)
    self.state.route_node_id = selected.node.id
    self:_ensure_final_service_hub()
  end
  self.state.phase = "transition"
  self.state.reconstruction_next = nil
  self.state.transition_next = result
  return { applied = true, next = result }
end

function Session:_boss_arena_material_layout(profile)
  local layout = {}
  for _, placement in ipairs(profile.terrain_cells or {}) do
    layout[Grid.key(placement.x, placement.y)] = placement.material_id
  end
  return next(layout) and layout or nil
end

function Session:_apply_boss_arena_profile(profile)
  local world = self.state.world
  for _, circuit in ipairs(profile.circuits or {}) do
    local result = world:register_circuit(circuit.id, { enabled = circuit.enabled })
    assert(result.applied, result.reason)
  end
  for _, placement in ipairs(profile.devices or {}) do
    local object, result = world:place_object(placement.definition_id, placement.x, placement.y, {
      circuit_id = placement.circuit_id,
      door_state = placement.door_state,
      generator_online = placement.generator_online,
    })
    assert(object, result and result.reason)
  end
  for _, placement in ipairs(profile.cover or {}) do
    local object, result = world:place_object(placement.definition_id, placement.x, placement.y)
    assert(object, result and result.reason)
  end
  for _, placement in ipairs(profile.hazards or {}) do
    local hazard, result = world:place_hazard(placement.definition_id, placement.x, placement.y)
    assert(hazard, result and result.reason)
  end
  for _, placement in ipairs(profile.liquid or {}) do
    local result = world:set_liquid(placement.x, placement.y, placement.liquid_id, placement.amount)
    assert(result.applied or result.code == "unchanged", result.reason)
  end
  for _, placement in ipairs(profile.gas or {}) do
    local result = world:set_gas(placement.x, placement.y, placement.gas_id, placement.concentration)
    assert(result.applied or result.code == "unchanged", result.reason)
  end
  for _, placement in ipairs(profile.fires or {}) do
    local target = placement.target_kind == "object" and world:object_at(placement.x, placement.y) or nil
    assert(target, "Boss arena fire requires an object target")
    local result = self:ignite_world_object(target, {
      source = "boss_arena.initial_fire",
      arena_profile_id = profile.id,
    })
    assert(result.applied, result.reason)
  end
end

function Session:_make_boss(boss_id, point)
  local definition = self.registry:get_boss(boss_id)
  local boss = entity("boss", point.x, point.y, {
    actor_id = self:_allocate_actor_id("zone"),
    content_id = definition.id,
    boss_id = definition.id,
    display_name = definition.display_name,
    health = definition.health,
    max_health = definition.health,
    ammo = definition.ammo or 0,
    direction = "a",
    stun = 0,
    crawl_stride = 0,
    pending_telegraph = nil,
    body = self:_build_body(definition, "zone"),
  })
  return boss
end

function Session:start_boss()
  local state, player = self.state, self.state.player
  if state.route then
    local current = assert(state.route:node(state.route.current_node_id), "Route current node is missing")
    if current.type == "shop" then
      local completed = state.route:complete_current()
      assert(completed.applied, completed.reason)
      assert(#completed.outgoing == 1 and completed.outgoing[1].type == "boss", "Route shop has no boss continuation")
      local selected = state.route:select(completed.outgoing[1].id)
      assert(selected.applied, selected.reason)
      current = selected.node
    end
    assert(current.type == "boss", "Boss may only start from a route boss node")
    state.route_node_id, state.floor_seed = current.id, current.encounter_seed
  end
  local node = state.route and state.route:node(state.route.current_node_id) or nil
  local boss_id = (node and node.boss_id) or "boss.legacy.final"
  local definition = self.registry:get_boss(boss_id)
  local profile = self.registry:get_boss_arena(definition.arena_profile_id)
  local arena_rng = Rng.new((node and node.encounter_seed) or self.seed):derive("boss_arena." .. definition.id)
  state.transition_next = nil
  player.x, player.y, player.direction, player.score, player.objective_progress = profile.player_spawn.x, profile.player_spawn.y, "w", 0, 0
  player.bombs, player.flares = math.max(1, player.bombs), math.max(1, player.flares)
  player.dash = 0
  player.base_dash_cooldown, player.base_bomb_radius, player.base_flare_light = 3, 2, 3
  self:refresh_derived_player_stats()
  player.bomb_fuse, player.bullet_range, player.reload_penalty = 3, nil, 0
  state.settings = { terrain = profile.terrain, vision = 99, objective_required = 10, arena_profile_id = profile.id }
  state.world = World.new(self.registry, profile.terrain, Generator.generate("arena", player, arena_rng, true), self.identity_allocator or state,
    self:_boss_arena_material_layout(profile))
  self:_apply_boss_arena_profile(profile)
  self:validate_world()
  state.targets, state.enemies, state.bullets, state.area_attacks = {}, {}, {}, {}
  state.bombs, state.flares, state.torches = {}, {}, {}
  state.effects, state.electrical_effects, state.exit, state.corpses = {}, {}, nil, {}
  state.boss = self:_make_boss(boss_id, profile.boss_spawn)
  state.boss_completed = nil
  local point = self:_open_location(self:_occupied(true))
  state.ammo = entity("ammo", point.x, point.y)
  state.phase = "boss"
  self:_log(definition.display_name .. " AWAITS.")
  self:refresh_visibility()
  self:validate_physical_ownership()
end

function Session:_defeat_boss(boss)
  local state = self.state
  if state.boss ~= boss then return false end
  local definition = self:_boss_definition(boss)
  boss.pending_telegraph = nil
  self:_cancel_area_attacks_from(boss)
  -- A campaign boss is an ordinary persistent zone resident.  Its defeat
  -- leaves the physical corpse in place, records a one-time semantic reward,
  -- and returns the zone to normal exploration without touching the route.
  if self.campaign and state.campaign_boss_site_id then
    local corpse = self:_create_corpse(boss)
    state.boss = nil
    state.boss_completed = definition.id
    state.phase = "combat"
    local reward = definition.final_data_reward or definition.milestone_data_reward or 2
    self:_claim_research_reward("world_boss." .. state.campaign_boss_site_id, reward)
    self:_sound("door")
    self:_log("MAJOR THREAT ELIMINATED.")
    self:validate_physical_ownership()
    return corpse
  end
  local node = state.route and state.route:node(state.route.current_node_id) or nil
  if node and not state.route.completed_node_ids[node.id] then
    local completed = state.route:complete_current()
    assert(completed.applied, completed.reason)
  end
  local is_final = not node or #state.route:outgoing(node.id) == 0
  if is_final then
    self:_claim_research_reward(node and node.id or "boss", definition.final_data_reward or 4)
    state.boss = nil
    state.ended = "victory"
    self:_sound("door")
    self:_log("THE LEGACY YIELDS.")
    return true
  end
  local corpse = self:_create_corpse(boss)
  state.boss = nil
  state.boss_completed = definition.id
  self:_claim_research_reward(node and node.id or definition.id, definition.milestone_data_reward or 2)
  local point = self:_open_location(self:_occupied(), 4, true)
  state.exit = entity("door", point.x, point.y)
  state.phase = "boss_exit"
  self:_sound("door")
  self:_log("BOSS DEFEATED. SALVAGE, THEN EXIT.")
  self:validate_physical_ownership()
  return corpse
end

function Session:_complete_boss_exit()
  local state, route = self.state, self.state.route
  assert(route and route:node(route.current_node_id) and route:node(route.current_node_id).type == "boss",
    "Boss exit requires a completed route boss")
  local choices = route:available()
  assert(#choices >= 1, "Milestone boss must have a forward continuation")
  state.curse, state.curse_id = nil, nil
  if choices[1].type == "floor" then
    -- The first milestone still opens the player-facing tier-three route
    -- selection, with a fresh curse scoped to that next normal floor.
    for _, choice in ipairs(choices) do
      assert(choice.type == "floor", "First milestone may only branch to normal floors")
    end
    self:draw_curses()
    state.phase, state.exit, state.reconstruction_next, state.transition_next = "reconstruction", nil, "curse", nil
    self:_log("Reconstruction available. Reconfigure before the next descent.")
  elseif #choices == 1 and choices[1].type == "shop" then
    -- The second milestone is followed by reconstruction, then the final
    -- service hub. No curse leaks into either special node.
    state.phase, state.exit, state.reconstruction_next, state.transition_next = "reconstruction", nil, "shop", nil
    self:_log("Reconstruction available. Reconfigure before final services.")
  elseif #choices == 1 and choices[1].type == "boss" then
    -- The universal Apex is a milestone, so its corpse remains salvageable.
    -- Its only successor is the route-predetermined terminal boss; enter a
    -- normal reconstruction phase first, with no second service hub.
    state.phase, state.exit, state.reconstruction_next, state.transition_next = "reconstruction", nil, "boss", nil
    self:_log("Reconstruction available. Reconfigure before the terminal threat.")
  else
    error("Milestone boss must continue to a floor, service hub, or terminal boss")
  end
  self:validate_physical_ownership()
  return "reconstruction"
end

function Session:buy(item)
  local player = self.state.player
  if self.state.scrap > 0 and player[item.key] < 5 then
    player[item.key] = player[item.key] + 1
    self.state.scrap = self.state.scrap - 1
    self:_sound("pickup")
    return true
  end
  self:_log("Cannot buy that item.")
  return false
end

function Session:sell(item)
  local player = self.state.player
  if player[item.key] > item.minimum then
    player[item.key] = player[item.key] - 1
    self.state.scrap = self.state.scrap + 1
    self:_sound("select")
    return true
  end
  self:_log("That item cannot be sold.")
  return false
end

function Session:turn(input)
  local state = self.state
  assert(state.player, "A run must be started before it can advance")
  self.last_action_result = nil
  if state.ended then
    return state.ended
  end
  local transitioned, transition_result = self:_try_surface_transition(input)
  if transitioned then
    return transition_result and transition_result.applied and "zone_transition" or "zone_transition_failed"
  end
  local vertical_transitioned, vertical_result = self:_try_vertical_transition(input)
  if vertical_transitioned then
    return vertical_result and vertical_result.applied and "zone_transition" or "zone_transition_failed"
  end
  -- Salvage opens the existing modal without advancing simulation.  It is a
  -- Campaign-only faced-tile action; the legacy G/nearby flow remains intact.
  if self.campaign and input == "interact" then
    local corpse = self:faced_corpse()
    if corpse then
      self.pending_salvage_corpse_id = corpse.id
      return "salvage"
    end
    -- Supplies are deliberately collected by entering their cell, never by
    -- spending a separate use turn.  Loose components still continue through
    -- the faced ground-item path below.
    local cell = self:faced_cell()
    if cell and #Interaction.available_at(self, self.state.player, cell.x, cell.y) == 0 then
      local ground = self:faced_ground_item()
      if ground and ground.item.item_type == "resource_stack" then
        self.last_action_result = { applied = false, code = "walk_over_pickup", reason = "Walk over supplies to collect them" }
        self:_log("WALK OVER SUPPLIES TO COLLECT THEM.")
        self:refresh_visibility()
        return "no_action"
      end
    end
  end
  local blocked_movement = self:_campaign_movement_preflight(input)
  if blocked_movement then
    self.last_action_result = self:_move_player(input)
    self:refresh_visibility()
    return "no_action"
  end
  -- A field quick-slot swap is an ordinary turn only when it actually has a
  -- valid alternate source. Empty/broken alternates are a readable no-op,
  -- matching the responsive Campaign input contract.
  if self.campaign and (input == "swap_weapon" or input == "swap_ability") then
    local kind = input == "swap_weapon" and "weapon" or "ability"
    local _, failure = self:campaign_loadout_swap_available(kind)
    if failure then
      self.last_action_result = failure
      self:_log(failure.reason)
      self:refresh_visibility()
      return "no_action"
    end
  end
  state.effects, state.electrical_effects = {}, {}
  state.player.dash = math.max(0, state.player.dash - 1)
  state.player.impact = math.max(0, state.player.impact - 1)

  if state.phase == "reconstruction" or state.phase == "transition" or state.phase == "route" or state.phase == "service" then
    return "reconstruction"
  end

  if state.phase == "exit" or state.phase == "boss_exit" then
    if DIRECTIONS[input] then
      self.last_action_result = self:_move_player(input)
    elseif input == "q" then
      self.last_action_result = self:_dash()
    end
    if not state.ended then ReinforcementSimulation.tick(self) end
    self:_update_liquids()
    self:_update_fire()
    if not state.ended then
      self:_update_gas()
    end
    if state.ended then
      self:refresh_visibility()
      return state.ended
    end
    if state.exit and state.player.x == state.exit.x and state.player.y == state.exit.y then
      local result = state.phase == "boss_exit" and self:_complete_boss_exit() or self:_complete_stage()
      self:refresh_visibility()
      return result
    end
    self:refresh_visibility()
    return nil
  end

  self.last_action_result = self:_action(input)
  -- Station reconstruction opens before enemy/environment updates. Browsing
  -- the modal thereafter runs no simulation ticks.
  if state.pending_reconstruction_station_id and not state.ended then
    state.active_reconstruction_station_id = state.pending_reconstruction_station_id
    state.pending_reconstruction_station_id = nil
    self:refresh_visibility()
    return "reconstruction"
  end
  if state.pending_storage_object_id and not state.ended then
    state.active_storage_object_id = state.pending_storage_object_id
    state.pending_storage_object_id = nil
    self:refresh_visibility()
    return "storage"
  end
  if state.ended then
    self:refresh_visibility()
    return state.ended
  end
  self:_collect_ammo()
  self:_update_bullets()
  self:_update_bombs()
  self:_update_flares()
  self:_update_area_attacks()
  if state.ended then
    self:refresh_visibility()
    return state.ended
  end

  local result
  if state.phase == "boss_exit" then
    -- The kill action may have transitioned into the salvageable arena-exit
    -- state. It receives no extra ordinary-floor AI/refill work this turn.
    result = nil
  elseif state.phase == "boss" then
    if state.boss and state.boss.health <= 0 then
      self:_defeat_boss(state.boss)
      result = state.ended
    elseif state.boss then
      self:_boss_turn()
      if state.boss and not state.ended then
        self:_enemy_turn()
      end
    end
    result = state.ended
  elseif not self.campaign and state.player.objective_progress >= (state.settings.score or state.settings.objective_required) then
    self:_begin_exit()
  else
    self:_enemy_turn()
    -- Finite campaign zones stay cleared. The legacy run retains the old
    -- floor-refill pressure as a compatibility behaviour.
    if not self.campaign then self:_refill_entities() end
  end
  if not state.ended then ReinforcementSimulation.tick(self) end
  -- World processes run after immediate actions and enemy response. Liquid
  -- redistribution/suppression precedes fire, so newly arrived water can save
  -- fuel before that turn's burn tick. New fires still wait by ready_tick.
  self:_update_liquids()
  self:_update_fire()
  if not state.ended then
    self:_update_gas()
  end
  if state.pending_service_object_id and not state.ended then
    local opened = self:open_service(state.pending_service_object_id)
    if opened.applied then
      self:refresh_visibility()
      return "service"
    end
    state.pending_service_object_id = nil
  end
  self:refresh_visibility()
  return result or state.ended
end

function Session:_has_line_of_sight(x0, y0, x1, y1)
  local delta_x, delta_y = math.abs(x1 - x0), math.abs(y1 - y0)
  local step_x, step_y = x0 < x1 and 1 or -1, y0 < y1 and 1 or -1
  local error_value = delta_x - delta_y
  while x0 ~= x1 or y0 ~= y1 do
    local twice = error_value * 2
    if twice > -delta_y then
      error_value = error_value - delta_y
      x0 = x0 + step_x
    end
    if twice < delta_x then
      error_value = error_value + delta_x
      y0 = y0 + step_y
    end
    if (x0 ~= x1 or y0 ~= y1) and self:_blocks_vision(x0, y0) then
      return false
    end
  end
  return true
end

function Session:_light_area(source, radius, visible)
  if radius >= math.max(Grid.width, Grid.height) then
    for x = 0, Grid.width - 1 do
      for y = 0, Grid.height - 1 do
        visible[Grid.key(x, y)] = true
      end
    end
    return
  end
  for x = source.x - radius, source.x + radius do
    for y = source.y - radius, source.y + radius do
      local delta_x, delta_y = x - source.x, y - source.y
      if Grid.in_bounds(x, y) and delta_x * delta_x + delta_y * delta_y <= radius * radius
        and self:_has_line_of_sight(source.x, source.y, x, y) then
        visible[Grid.key(x, y)] = true
      end
    end
  end
end

function Session:refresh_visibility()
  local state = self.state
  if not state.player or not state.settings then
    return
  end
  local visible = {}
  self:_light_area(state.player, math.max(1, state.settings.vision or 1), visible)
  for _, torch in ipairs(state.torches or {}) do
    self:_light_area(torch, math.max(1, torch.light or 1), visible)
  end
  for _, flare in ipairs(state.flares or {}) do
    if flare.detonated and (flare.light_remaining or 0) > 0 then
      self:_light_area(flare, math.max(1, flare.light or 1), visible)
    end
  end
  for _, bomb in ipairs(state.bombs or {}) do
    self:_light_area(bomb, math.max(1, bomb.light or 1), visible)
  end
  for _, fire in ipairs(state.world and state.world:list_fires() or {}) do
    local x, y = state.world:fire_position(fire)
    if x then self:_light_area({ x = x, y = y }, 2, visible) end
  end
  -- This is tactical line-of-sight only. Renderer terrain is deliberately
  -- independent from it: ROAG has no exploration/discovery fog-of-war.
  state.visible = visible
end

function Session:telegraphs()
  local result = {}
  for _, enemy in ipairs(self.state.enemies or {}) do
    if enemy.attack > 0 then
      for location_key in pairs(self:_attack_cells(enemy)) do
        result[location_key] = enemy.attack >= enemy.attack_windup and "danger" or "warn"
      end
    end
  end
  for _, attack in ipairs(self.state.area_attacks or {}) do
    for location_key in pairs(self:_area_attack_cells(attack)) do
      result[location_key] = attack.remaining <= 1 and "danger" or "warn"
    end
  end
  if self.state.boss then
    local pending = self.state.boss.pending_telegraph
    for location_key in pairs(self:_boss_telegraph_cells(self.state.boss)) do
      result[location_key] = pending and pending.remaining <= 1 and "danger" or "warn"
    end
  end
  return result
end

function Session:enemy_intent(enemy)
  if enemy.stun > 0 then
    return "STUNNED"
  end
  if enemy.attack > 0 then
    return string.upper(enemy.attack_kind) .. " IN " .. math.max(1, enemy.attack_windup - enemy.attack + 1)
  end
  for _, attack in ipairs(self.state.area_attacks or {}) do
    if attack.source_actor == enemy then
      return "ARCANE BURST IN " .. math.max(1, attack.remaining)
    end
  end
  if self:actor_has_capability(enemy, SELF_DESTRUCT_ABILITY) then
    return "DETONATE"
  end
  if self:actor_ability_by_implementations(enemy, { "projectile", "scattershot", "piercing_projectile" }) then return "RANGED" end
  if self:actor_ability_by_implementation(enemy, "melee") then return "MELEE" end
  return "ADVANCE"
end

return Session
