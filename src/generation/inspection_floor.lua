-- Read-only, isolated construction of an initial normal floor for developer
-- tooling.  It deliberately uses the authoritative Session stage builder so
-- inspector and batch output cannot drift from actual generation semantics.
-- No App, save store, renderer, or active game session is involved.
local Content = require("src.content.legacy")
local Session = require("src.simulation.session")

local InspectionFloor = {}

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

function InspectionFloor.stages()
  local result = {}
  for index, stage in ipairs(Content.stages) do
    result[#result + 1] = {
      index = index,
      level = stage.level,
      terrain = stage.terrain,
      label = string.format("%d: %s", index, stage.terrain),
    }
  end
  return result
end

function InspectionFloor.resolve_stage(value)
  return stage_index(value)
end

local function provenance_for(session)
  local stage = session.state.stage
  local streams = {
    "terrain.root_rng",
    "world_objects.stage." .. stage,
    "hazards.stage." .. stage,
    "liquids.stage." .. stage,
    "gases.stage." .. stage,
    "power_devices.stage." .. stage,
    "entities.root_rng",
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
  for _, object in ipairs(world:list_objects()) do
    result.objects[object.id] = object.interaction_role and ("power_devices.stage." .. stage)
      or ("world_objects.stage." .. stage)
  end
  for _, hazard in ipairs(world:list_hazards()) do
    result.hazards[hazard.id] = "hazards.stage." .. stage
  end
  for _, liquid in ipairs(world:list_liquids()) do
    result.liquids[liquid.x .. ":" .. liquid.y] = "liquids.stage." .. stage
  end
  for _, gas in ipairs(world:list_gases()) do
    result.gases[gas.x .. ":" .. gas.y] = "gases.stage." .. stage
  end
  for index in ipairs(state.targets) do
    result.targets[index] = "entities.root_rng.targets"
  end
  for index in ipairs(state.enemies) do
    result.enemies[index] = "entities.root_rng.enemies"
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
  local stage = stage_index(options.stage or 1)
  if not stage then
    return nil, { code = "invalid_stage", reason = "Unknown generated stage '" .. tostring(options.stage) .. "'" }
  end
  local seed = tonumber(options.seed)
  if not seed or seed % 1 ~= 0 then
    return nil, { code = "invalid_seed", reason = "Seed must be an integer" }
  end
  local session = Session.new({ seed = seed, content = options.content })
  session.state.class = (options.content or Content).classes[1]
  session.state.boon = (options.content or Content).boons[1]
  session.state.stage = stage
  session:start_stage()
  return {
    seed = session.seed,
    stage = stage,
    terrain = session.state.settings.terrain,
    session = session,
    world = session.state.world,
    state = session.state,
    provenance = provenance_for(session),
  }
end

return InspectionFloor
