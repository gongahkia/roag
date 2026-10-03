-- Read-only, isolated construction of an initial normal floor for developer
-- tooling.  It deliberately uses the authoritative Session stage builder so
-- inspector and batch output cannot drift from actual generation semantics.
-- No App, save store, renderer, or active game session is involved.
local Content = require("src.content.legacy")
local Session = require("src.simulation.session")
local RouteDefinitions = require("src.routes.definitions")
local Campaign = require("src.campaign.campaign")
local ZoneKey = require("src.campaign.zone_key")

local InspectionFloor = {}

local Definitions = RouteDefinitions.load()

local function stage_index(value)
  if type(value) == "number" and value % 1 == 0 and value >= 1 and value <= 4 then
    return value
  end
  if type(value) == "string" then
    local numeric = tonumber(value)
    if numeric and numeric % 1 == 0 and numeric >= 1 and numeric <= 4 then
      return numeric
    end
    if value == "reactor" then return 4 end
    for index, stage in ipairs(Content.stages) do
      if stage.terrain == value then
        return index
      end
    end
  end
  return nil
end

local function biome_definition(value)
  if type(value) == "string" then
    if Definitions.biomes[value] then return Definitions.biomes[value] end
    for _, id in ipairs(Definitions.biome_order) do
      local biome = Definitions.biomes[id]
      if biome.terrain == value then return biome end
    end
  end
  return nil
end

local function tier_definition(value)
  if type(value) == "number" then
    for _, id in ipairs(Definitions.tier_order) do
      local tier = Definitions.tiers[id]
      if tier.number == value then return tier end
    end
  elseif type(value) == "string" then
    if Definitions.tiers[value] then return Definitions.tiers[value] end
    return tier_definition(tonumber(value))
  end
  return nil
end

function InspectionFloor.stages()
  local result = {}
  for index, stage in ipairs(Content.stages) do
    result[#result + 1] = {
      index = index,
      level = stage.level,
      terrain = stage.terrain,
      biome_id = "biome.legacy." .. stage.terrain,
      tier_id = "tier.legacy." .. index,
      label = string.format("%d: %s", index, stage.terrain),
    }
  end
  result[#result + 1] = {
    index = 4,
    level = 4,
    terrain = "reactor",
    biome_id = "biome.legacy.reactor",
    tier_id = "tier.legacy.3",
    label = "4: reactor",
  }
  return result
end

function InspectionFloor.biomes()
  local result = {}
  for _, id in ipairs(Definitions.biome_order) do result[#result + 1] = Definitions.biomes[id] end
  return result
end

function InspectionFloor.tiers()
  local result = {}
  for _, id in ipairs(Definitions.tier_order) do result[#result + 1] = Definitions.tiers[id] end
  table.sort(result, function(first, second) return first.number < second.number end)
  return result
end

function InspectionFloor.resolve_stage(value)
  return stage_index(value)
end

function InspectionFloor.resolve_biome(value)
  return biome_definition(value)
end

function InspectionFloor.resolve_tier(value)
  return tier_definition(value)
end

local function provenance_for(session)
  local stage = session.state.stage
  local biome_id, tier_id = session.state.settings.biome_id, session.state.settings.tier_id
  local streams = {
    "terrain.root_rng",
    "inspection." .. tostring(biome_id) .. "." .. tostring(tier_id) .. ".rooms",
    "inspection." .. tostring(biome_id) .. "." .. tostring(tier_id) .. ".landmarks.layout",
    "inspection." .. tostring(biome_id) .. "." .. tostring(tier_id) .. ".landmarks.objects",
    "inspection." .. tostring(biome_id) .. "." .. tostring(tier_id) .. ".world_objects",
    "inspection." .. tostring(biome_id) .. "." .. tostring(tier_id) .. ".hazards",
    "inspection." .. tostring(biome_id) .. "." .. tostring(tier_id) .. ".liquids",
    "inspection." .. tostring(biome_id) .. "." .. tostring(tier_id) .. ".gases",
    "inspection." .. tostring(biome_id) .. "." .. tostring(tier_id) .. ".power_devices",
    "inspection." .. tostring(biome_id) .. "." .. tostring(tier_id) .. ".fires",
    "inspection." .. tostring(biome_id) .. "." .. tostring(tier_id) .. ".traversal",
    "inspection." .. tostring(biome_id) .. "." .. tostring(tier_id) .. ".services",
    "inspection." .. tostring(biome_id) .. "." .. tostring(tier_id) .. ".entities",
    "inspection." .. tostring(biome_id) .. "." .. tostring(tier_id) .. ".discoveries",
    "inspection." .. tostring(biome_id) .. "." .. tostring(tier_id) .. ".reinforcements",
  }
  local result = {
    streams = streams,
    player = "stage.player_start",
    targets = {},
    enemies = {},
    objects = {},
    hazards = {},
    liquids = {},
    gases = {},
    fires = {},
  }
  local world, state = session.state.world, session.state
  result.rooms = state.generation_metadata
  if state.generation_metadata and state.generation_metadata.fallen_recurrence then
    result.fallen_recurrence = state.generation_metadata.fallen_recurrence
  end
  for _, object in ipairs(world:list_objects()) do
    result.objects[object.id] = object.discovery_id and "inspection.discoveries"
      or (object.interaction_role == "service" and "inspection.services"
      or (object.interaction_role == "traversal" and "inspection.traversal"
        or (object.interaction_role == "reinforcement" and "inspection.reinforcements"
          or (object.interaction_role and "inspection.power_devices" or "inspection.world_objects"))))
  end
  for _, hazard in ipairs(world:list_hazards()) do
    result.hazards[hazard.id] = "inspection.hazards"
  end
  for _, liquid in ipairs(world:list_liquids()) do
    result.liquids[liquid.x .. ":" .. liquid.y] = "inspection.liquids"
  end
  for _, gas in ipairs(world:list_gases()) do
    result.gases[gas.x .. ":" .. gas.y] = "inspection.gases"
  end
  for _, fire in ipairs(world:list_fires(true)) do
    result.fires[fire.id] = "inspection.fires"
  end
  for index in ipairs(state.targets) do
    result.targets[index] = "inspection.entities.targets"
  end
  for index in ipairs(state.enemies) do
    result.enemies[index] = "inspection.entities.enemies"
  end
  return result
end

-- This intentionally begins a fresh selected stage from the supplied seed.
-- It is not an attempt to reconstruct an arbitrary in-progress run: the
-- inspector's seed is a reproducible floor-construction input.  Base class
-- and boon do not alter map/media/object placement, and their only purpose is
-- satisfying the normal Session stage-settings contract.
function InspectionFloor.generate(options)
  options = options or {}
  local stage = options.stage ~= nil and stage_index(options.stage) or (options.biome == nil and stage_index(1) or nil)
  local biome, tier
  if stage then
    local canonical = InspectionFloor.stages()[stage]
    biome = Definitions:get_biome(canonical.biome_id)
    tier = Definitions:get_tier(canonical.tier_id)
  else
    biome, tier = biome_definition(options.biome), tier_definition(options.tier or 1)
  end
  if not biome then return nil, { code = "invalid_biome", reason = "Unknown generated biome '" .. tostring(options.biome or options.stage) .. "'" } end
  if not tier then return nil, { code = "invalid_tier", reason = "Unknown generated tier '" .. tostring(options.tier) .. "'" } end
  if not Definitions:biome_supports_tier(biome.id, tier.id) then
    return nil, { code = "unsupported_biome_tier", reason = "Biome/tier combination is not supported" }
  end
  local seed = tonumber(options.seed)
  if not seed or seed % 1 ~= 0 then
    return nil, { code = "invalid_seed", reason = "Seed must be an integer" }
  end
  -- Synthetic plain-data recurrence input is intentionally injected only
  -- into this isolated Session. Inspector use never opens persistent user
  -- archive storage or changes an active run.
  local session = Session.new({
    seed = seed,
    content = options.content,
    meta_snapshot = options.meta_snapshot,
    fallen_recurrence = options.fallen_recurrence,
  })
  session.state.class = (options.content or Content).classes[1]
  session.state.boon = (options.content or Content).boons[1]
  session:start_biome_tier(biome.id, tier.id, seed, options.service_id, {
    recurrence_depth = options.recurrence_depth or (options.fallen_recurrence and options.fallen_recurrence.target_depth),
    -- Existing inspector callers intentionally retain their historical,
    -- discovery-free fixture output unless they opt in.  The interactive
    -- inspector and batch analyzer pass this explicitly, so discovery
    -- diagnostics still exercise the same authoritative floor builder.
    discovery_state = options.discovery_state or { enabled = false, assigned_discovery_ids = {} },
    reinforcement_state = options.reinforcement_state or { enabled = false },
  })
  return {
    seed = session.seed,
    stage = tier.number,
    biome_id = biome.id,
    tier_id = tier.id,
    terrain = session.state.settings.terrain,
    session = session,
    world = session.state.world,
    state = session.state,
    provenance = provenance_for(session),
    fallen_recurrence = session.state.fallen_recurrence,
  }
end

-- OW-02 inspection entry point. It deliberately constructs the same
-- campaign-zone generator used by play, but never opens persistence or a
-- renderer. Legacy biome/tier inspection above remains untouched.
function InspectionFloor.generate_campaign_zone(options)
  options = options or {}
  local seed = tonumber(options.campaign_seed or options.seed)
  local x, y, z = tonumber(options.world_x), tonumber(options.world_y), tonumber(options.z or 0)
  if not seed or seed % 1 ~= 0 then return nil, { code = "invalid_seed", reason = "Campaign seed must be an integer" } end
  if not x or x % 1 ~= 0 or not y or y % 1 ~= 0 or not z or z % 1 ~= 0 then
    return nil, { code = "invalid_zone_key", reason = "Campaign zone coordinates must be integers" }
  end
  local key = ZoneKey.new(x, y, z)
  if not Campaign.is_zone_in_bounds(key) then
    return nil, { code = "out_of_bounds", reason = "Campaign zone is outside the finite world bounds" }
  end
  local campaign = Campaign.new({
    seed = seed, campaign_id = options.campaign_id or "campaign:000001", current_zone = key,
    profile_id = options.profile_id, content = options.content, registry = options.registry,
    route_definitions = options.route_definitions, meta_snapshot = options.meta_snapshot,
  })
  local session, record = campaign.session, campaign.active_zone
  local player_corpses = {}
  for _, corpse in ipairs(session.state.corpses or {}) do
    if corpse.source_kind == "player" then
      player_corpses[#player_corpses + 1] = {
        corpse_id = corpse.id, source_body_id = corpse.source_body_id, x = corpse.x, y = corpse.y,
        carried_item_count = #(corpse.carried_inventory and corpse.carried_inventory.entries or {}),
      }
    end
  end
  local constructed, harvestable, storage, ground_items, circuits = {}, {}, {}, {}, {}
  for _, object in ipairs(session.state.world:list_objects(true)) do
    local definition = session.registry:get_world_object(object.definition_id)
    if definition.harvest_yield then
      harvestable[#harvestable + 1] = { object_id = object.id, definition_id = object.definition_id, x = object.x, y = object.y,
        resource_id = definition.harvest_yield.resource_id, amount = definition.harvest_yield.amount }
    end
    if object.constructed then
      constructed[#constructed + 1] = { object_id = object.id, recipe_id = object.construction_recipe_id,
        x = object.x, y = object.y, circuit_id = object.circuit_id }
    end
    if object.storage_inventory then
      storage[#storage + 1] = { object_id = object.id, item_count = #object.storage_inventory.entries,
        mass = object.storage_inventory:total_mass() }
    end
  end
  for _, ground in ipairs(session.state.world:list_ground_items()) do
    ground_items[#ground_items + 1] = { item_id = ground.id, x = ground.x, y = ground.y,
      item_type = ground.item.item_type, resource_id = ground.item.resource_id, quantity = ground.item.quantity }
  end
  for _, circuit in ipairs(session.state.world:list_circuits()) do
    circuits[#circuits + 1] = { id = circuit.id, enabled = circuit.enabled,
      powered = session.state.world:is_circuit_powered(circuit.id) }
  end
  return {
    campaign_seed = campaign.state.seed, zone_key = ZoneKey.to_data(key), profile_id = record.profile_id,
    generation_seed = record.generation_seed, connections = record.connections,
    campaign = campaign, session = session, world = session.state.world, state = session.state,
    reconstruction_anchor = campaign.state.reconstruction_anchor and {
      zone_key = ZoneKey.to_data(campaign.state.reconstruction_anchor.zone_key),
      station_object_id = campaign.state.reconstruction_anchor.station_object_id,
    } or nil,
    player_corpses = player_corpses,
    harvestable_sources = harvestable,
    constructed_objects = constructed,
    storage = storage,
    ground_items = ground_items,
    player_circuits = circuits,
    world_content_plan = campaign.state.world_content_plan,
    world_content_sites = require("src.campaign.world_content").sites_for_zone(campaign.state.world_content_plan, key),
    location_name = session.state.settings.location_name,
    ecology_profile_id = session.state.settings.ecology_profile_id,
    boss_state = session.state.boss and {
      boss_id = session.state.boss.boss_id, actor_id = session.state.boss.actor_id,
      health = session.state.boss.health, max_health = session.state.boss.max_health,
    } or (session.state.boss_completed and { defeated_boss_id = session.state.boss_completed } or nil),
    seed = session.seed, stage = session.state.stage, terrain = session.state.settings.terrain,
    biome_id = session.state.settings.biome_id, tier_id = session.state.settings.tier_id,
    provenance = { zone_connections = record.connections, surface_connections = record.connections, zone_key = ZoneKey.encode(key) },
  }
end

return InspectionFloor
