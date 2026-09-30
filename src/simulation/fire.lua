-- Persistent material-backed fire. It owns deterministic ignition, burning,
-- and two-phase propagation; World remains the owner of fire instances.
local EnvironmentDamage = require("src.simulation.environment_damage")

local Fire = {
  -- A fire advances one cardinal layer per world tick at most. Two-phase
  -- creation prevents same-turn cascades despite this readable fast cadence.
  SPREAD_INTERVAL = 1,
}

local CARDINAL_DIRECTIONS = {
  { 0, 1 }, -- north
  { 1, 0 }, -- east
  { 0, -1 }, -- south
  { -1, 0 }, -- west
}

local function ignition_failure(code, reason)
  return { applied = false, code = code, reason = reason }
end

function Fire.ignite_terrain(world, x, y, context)
  if not world then
    return ignition_failure("no_world", "No active world")
  end
  local fire, result = world:create_fire({ kind = "terrain", x = x, y = y }, context)
  if not fire then
    return result
  end
  result.fire = fire
  return result
end

function Fire.ignite_object(world, object_or_id, context)
  if not world then
    return ignition_failure("no_world", "No active world")
  end
  local object = type(object_or_id) == "table" and object_or_id or world:get_object(object_or_id)
  if not object then
    return ignition_failure("no_target", "World object does not exist")
  end
  local fire, result = world:create_fire({ kind = "object", target_id = object.id }, context)
  if not fire then
    return result
  end
  result.fire = fire
  return result
end

local function burn_target(world, fire, target)
  local spec = {
    amount = target.material.burn_rate,
    cause = "thermal",
    source = "fire",
    source_actor_id = fire.provenance.source_actor_id,
    source_component_id = fire.provenance.source_component_id,
    ability_id = fire.provenance.ability_id,
    fire_id = fire.id,
    ignition_source = fire.provenance.source,
  }
  if target.target_kind == "terrain" then
    return EnvironmentDamage.apply_to_terrain(world, target.x, target.y, spec)
  end
  return EnvironmentDamage.apply_to_object(world, target.target_id, spec)
end

local function ignite_neighbours(world, source_fire, x, y, spread_results)
  for _, direction in ipairs(CARDINAL_DIRECTIONS) do
    local target_x, target_y = x + direction[1], y + direction[2]
    local provenance = {
      source = "fire_spread",
      ignited_by_fire_id = source_fire.id,
      root_source = source_fire.provenance.source,
      source_actor_id = source_fire.provenance.source_actor_id,
      source_component_id = source_fire.provenance.source_component_id,
      ability_id = source_fire.provenance.ability_id,
    }
    local terrain = Fire.ignite_terrain(world, target_x, target_y, provenance)
    if terrain.applied then
      spread_results[#spread_results + 1] = terrain
    end
    local object = world:object_at(target_x, target_y)
    if object then
      local object_result = Fire.ignite_object(world, object, provenance)
      if object_result.applied then
        spread_results[#spread_results + 1] = object_result
      end
    end
  end
end

-- hooks.actors_at(x, y) must return a deterministic list. on_actor_exposed
-- receives the fire and its current physical target position.
function Fire.tick(world, hooks)
  hooks = hooks or {}
  if not world then
    return { applied = false, code = "no_world", reason = "No active world" }
  end
  local tick = world:begin_fire_tick()
  local burns, spread_sources, exposures = {}, {}, {}
  -- list_fires is ordered by stable physical fire ID. Newly spread fires are
  -- added only after this snapshot and therefore never recurse this tick.
  local fires = world:list_fires()
  for _, fire in ipairs(fires) do
    if fire.ready_tick <= tick then
      local target, target_code = world:resolve_fire_target(fire)
      if not target then
        world:deactivate_fire(fire, target_code)
      else
        local x, y = target.x, target.y
        local damage = burn_target(world, fire, target)
        fire.age = fire.age + 1
        burns[#burns + 1] = { fire = fire, target = target, damage = damage }
        if not damage.destroyed and fire.active and fire.age % Fire.SPREAD_INTERVAL == 0 then
          spread_sources[#spread_sources + 1] = { fire = fire, x = x, y = y }
        end
        -- Exposure is only from fires that existed at tick start and were
        -- mature enough to burn; new ignitions wait for a later world tick.
        exposures[#exposures + 1] = { fire = fire, x = x, y = y, target = target }
      end
    end
  end

  local exposure_results = {}
  if hooks.actors_at and hooks.on_actor_exposed then
    for _, exposure in ipairs(exposures) do
      for _, actor in ipairs(hooks.actors_at(exposure.x, exposure.y)) do
        local result = hooks.on_actor_exposed(actor, exposure.fire, exposure.target, exposure.x, exposure.y)
        exposure_results[#exposure_results + 1] = result
      end
    end
  end

  local spread_results = {}
  for _, source in ipairs(spread_sources) do
    if source.fire.active then
      ignite_neighbours(world, source.fire, source.x, source.y, spread_results)
    end
  end
  world:end_fire_tick()
  return {
    applied = #burns > 0 or #spread_results > 0 or #exposure_results > 0,
    code = "ticked",
    tick = tick,
    burns = burns,
    spreads = spread_results,
    exposures = exposure_results,
  }
end

return Fire
