-- Read-only, isolated construction of an initial normal floor for developer
-- tooling.  It deliberately uses the authoritative Session stage builder so
-- inspector and batch output cannot drift from actual generation semantics.
-- No App, save store, renderer, or active game session is involved.
local Content = require("src.content.legacy")
local Session = require("src.simulation.session")
local RouteDefinitions = require("src.routes.definitions")

local InspectionFloor = {}

local Definitions = RouteDefinitions.load()

local function stage_index(value)
  if type(value) == "number" and value % 1 == 0 and Content.stages[value] then
    return value
  end
  if type(value) == "string" then
    local numeric = tonumber(value)
    if numeric and numeric % 1 == 0 and Content.stages[numeric] then
      return numeric
    end
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
    "inspection." .. tostring(biome_id) .. "." .. tostring(tier_id) .. ".world_objects",
    "inspection." .. tostring(biome_id) .. "." .. tostring(tier_id) .. ".hazards",
    "inspection." .. tostring(biome_id) .. "." .. tostring(tier_id) .. ".liquids",
    "inspection." .. tostring(biome_id) .. "." .. tostring(tier_id) .. ".gases",
    "inspection." .. tostring(biome_id) .. "." .. tostring(tier_id) .. ".power_devices",
    "inspection." .. tostring(biome_id) .. "." .. tostring(tier_id) .. ".traversal",
    "inspection." .. tostring(biome_id) .. "." .. tostring(tier_id) .. ".services",
    "inspection." .. tostring(biome_id) .. "." .. tostring(tier_id) .. ".entities",
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
  }
  local world, state = session.state.world, session.state
  result.rooms = state.generation_metadata
  for _, object in ipairs(world:list_objects()) do
    result.objects[object.id] = object.interaction_role == "service" and "inspection.services"
      or (object.interaction_role == "traversal" and "inspection.traversal"
        or (object.interaction_role and "inspection.power_devices" or "inspection.world_objects"))
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
    biome = Definitions:get_biome("biome.legacy." .. Content.stages[stage].terrain)
    tier = Definitions:get_tier("tier.legacy." .. stage)
  else
    biome, tier = biome_definition(options.biome), tier_definition(options.tier or 1)
  end
  if not biome then return nil, { code = "invalid_biome", reason = "Unknown generated biome '" .. tostring(options.biome or options.stage) .. "'" } end
  if not tier then return nil, { code = "invalid_tier", reason = "Unknown generated tier '" .. tostring(options.tier) .. "'" } end
  local seed = tonumber(options.seed)
  if not seed or seed % 1 ~= 0 then
    return nil, { code = "invalid_seed", reason = "Seed must be an integer" }
  end
  local session = Session.new({ seed = seed, content = options.content })
  session.state.class = (options.content or Content).classes[1]
  session.state.boon = (options.content or Content).boons[1]
  session:start_biome_tier(biome.id, tier.id, seed, options.service_id)
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
  }
end

return InspectionFloor
