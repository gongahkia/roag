-- Optional, deterministic discovery placement. This module deliberately runs
-- after normal occupants and services have been placed, so a secret can never
-- steal a required target, kiosk, recurrence, or spawn cell.
local Grid = require("src.world.grid")
local Balance = require("content.balance.legacy")

local Discoveries = {
  PLACEMENT_PERCENT = Balance.discoveries.placement_percent,
  MAX_SITES_PER_FLOOR = Balance.discoveries.max_sites_per_floor,
}

local CARDINAL = {
  { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 },
}

local function key(point)
  return Grid.key(point.x, point.y)
end

local function sorted_discoveries(registry, biome_id, assigned, known)
  local all, unseen_unassigned, unassigned = {}, {}, {}
  for id, definition in pairs(registry.discoveries) do
    local compatible = false
    for _, allowed in ipairs(definition.allowed_biome_ids) do
      if allowed == biome_id then compatible = true; break end
    end
    if compatible then
      all[#all + 1] = definition
      if not assigned[id] then
        unassigned[#unassigned + 1] = definition
        if not known[id] then unseen_unassigned[#unseen_unassigned + 1] = definition end
      end
    end
  end
  local chosen = #unseen_unassigned > 0 and unseen_unassigned or (#unassigned > 0 and unassigned or all)
  table.sort(chosen, function(left, right) return left.id < right.id end)
  return chosen
end

local function safe_free(session, point, occupied, minimum_distance)
  local world, player = session.state.world, session.state.player
  if not Grid.in_bounds(point.x, point.y) or not world:is_passable(point.x, point.y) then return false end
  if occupied[Grid.key(point.x, point.y)] or world:object_at(point.x, point.y)
    or world:is_hazardous(point.x, point.y) or world:is_harmful_gas_at(point.x, point.y)
    or #world:fires_at(point.x, point.y) > 0 or world:is_liquid_cell(point.x, point.y) then
    return false
  end
  return not minimum_distance or Grid.distance(player, point) >= minimum_distance
end

local function free_cells(session, occupied, minimum_distance)
  local result = {}
  for x = 1, Grid.width - 2 do
    for y = 1, Grid.height - 2 do
      local point = { x = x, y = y }
      if safe_free(session, point, occupied, minimum_distance) then result[#result + 1] = point end
    end
  end
  return result
end

local function reachable_from(world, start, blocked_key)
  local result, queue, cursor = {}, {}, 1
  if not start or not world:is_passable(start.x, start.y) or Grid.key(start.x, start.y) == blocked_key then return result end
  result[key(start)] = true
  queue[1] = { x = start.x, y = start.y }
  while queue[cursor] do
    local point = queue[cursor]
    cursor = cursor + 1
    for _, direction in ipairs(CARDINAL) do
      local next_point = { x = point.x + direction[1], y = point.y + direction[2] }
      local next_key = key(next_point)
      if Grid.in_bounds(next_point.x, next_point.y) and next_key ~= blocked_key
        and world:is_passable(next_point.x, next_point.y) and not result[next_key] then
        result[next_key] = true
        queue[#queue + 1] = next_point
      end
    end
  end
  return result
end

local function has_occupied_cell(reachable, occupied)
  for location_key in pairs(reachable) do
    if occupied[location_key] then return true end
  end
  return false
end

-- Find a real optional side region. Closing the selected passable cell must
-- isolate only unoccupied, noncritical terrain. When the gate opens/dies,
-- the cache becomes reachable through ordinary world queries.
local function gate_layout(session, occupied, rng)
  local world, player = session.state.world, session.state.player
  local open_reachable = reachable_from(world, player)
  -- Compute the legal free-space catalogue once.  Discovery placement runs
  -- on generated floors, so repeatedly scanning the full map for every
  -- possible gate would make large diagnostic batches needlessly expensive.
  local all_free = free_cells(session, occupied, nil)
  local gate_options = {}
  for _, point in ipairs(all_free) do
    if Grid.distance(player, point) >= 8 then
    local neighbours = 0
    for _, direction in ipairs(CARDINAL) do
      if world:is_passable(point.x + direction[1], point.y + direction[2]) then neighbours = neighbours + 1 end
    end
      if neighbours >= 2 then gate_options[#gate_options + 1] = point end
    end
  end
  gate_options = rng:shuffle(gate_options)
  -- A dozen deterministic articulation candidates is ample on the current
  -- floor dimensions and keeps the optional phase bounded for hundreds of
  -- headless constructions.
  local attempts = math.min(#gate_options, 12)
  for index = 1, attempts do
    local gate = gate_options[index]
    local closed_reachable = reachable_from(world, player, key(gate))
    local isolated = {}
    for location_key in pairs(open_reachable) do
      if not closed_reachable[location_key] then isolated[location_key] = true end
    end
    if next(isolated) and not has_occupied_cell(isolated, occupied) then
      local cache_options = {}
      for _, point in ipairs(all_free) do
        local location_key = key(point)
        -- The virtual closed-region difference includes the gate cell itself;
        -- it is not an optional room cell and must never also host the cache.
        if location_key ~= key(gate) and isolated[location_key] then cache_options[#cache_options + 1] = point end
      end
      local clue_options = {}
      for _, direction in ipairs(CARDINAL) do
        local point = { x = gate.x + direction[1], y = gate.y + direction[2] }
        if closed_reachable[key(point)] and safe_free(session, point, occupied, nil) then
          clue_options[#clue_options + 1] = point
        end
      end
      if #cache_options > 0 and #clue_options > 0 then
        return {
          gate = gate,
          cache = rng:choice(cache_options),
          clue = rng:choice(clue_options),
          player_region = closed_reachable,
          free_cells = all_free,
        }
      end
    end
  end
  return nil
end

local function place_cache(world, definition, point, provenance)
  return world:place_object("world_object.discovery.cache", point.x, point.y, {
    discovery_id = definition.id,
    discovery_access_profile_id = definition.access_profile_id,
    discovery_provenance = provenance,
  })
end

local function place_clue(world, definition, point, provenance)
  return world:place_object("world_object.discovery.clue", point.x, point.y, {
    discovery_id = definition.id,
    discovery_access_profile_id = definition.access_profile_id,
    discovery_provenance = provenance,
  })
end

local function place_open(session, definition, occupied, rng, provenance)
  local choices = rng:shuffle(free_cells(session, occupied, 9))
  local point = choices[1]
  if not point then return nil, "no_open_site" end
  local cache, failure = place_cache(session.state.world, definition, point, provenance)
  if not cache then return nil, failure.reason end
  return { cache_object_id = cache.id, x = point.x, y = point.y }
end

local function place_breachable(session, definition, occupied, rng, provenance)
  local layout = gate_layout(session, occupied, rng)
  if not layout then return nil, "no_optional_breach" end
  local world = session.state.world
  local cache, cache_failure = place_cache(world, definition, layout.cache, provenance)
  if not cache then return nil, cache_failure.reason end
  local clue, clue_failure = place_clue(world, definition, layout.clue, provenance)
  if not clue then return nil, clue_failure.reason end
  local gate, gate_failure = world:place_object("world_object.cover.masonry_barricade", layout.gate.x, layout.gate.y, {
    discovery_id = definition.id,
    discovery_access_profile_id = definition.access_profile_id,
    discovery_provenance = provenance,
  })
  if not gate then return nil, gate_failure.reason end
  return { cache_object_id = cache.id, gate_object_id = gate.id, clue_object_id = clue.id, x = layout.cache.x, y = layout.cache.y,
    gate_x = layout.gate.x, gate_y = layout.gate.y, clue_x = layout.clue.x, clue_y = layout.clue.y }
end

local function controls_for(session, layout, occupied, rng)
  local choices = {}
  for _, point in ipairs(layout.free_cells or free_cells(session, occupied, nil)) do
    local location_key = key(point)
    if layout.player_region[location_key] and location_key ~= key(layout.clue) and location_key ~= key(layout.gate) then
      choices[#choices + 1] = point
    end
  end
  choices = rng:shuffle(choices)
  if #choices < 2 then return nil end
  return choices[1], choices[2]
end

local function place_powered(session, definition, occupied, rng, provenance)
  local layout = gate_layout(session, occupied, rng)
  if not layout then return nil, "no_optional_powered_site" end
  local generator_point, breaker_point = controls_for(session, layout, occupied, rng)
  if not generator_point then return nil, "no_power_controls" end
  local world = session.state.world
  local cache, cache_failure = place_cache(world, definition, layout.cache, provenance)
  if not cache then return nil, cache_failure.reason end
  local clue, clue_failure = place_clue(world, definition, layout.clue, provenance)
  if not clue then return nil, clue_failure.reason end
  local circuit_id = "power.circuit.discovery." .. definition.id:gsub("^discovery%.", ""):gsub("%.", "_")
  local circuit = world:register_circuit(circuit_id, { enabled = false })
  if not circuit.applied then return nil, circuit.reason end
  local generator, generator_failure = world:place_object("world_object.power.generator_legacy", generator_point.x, generator_point.y, {
    circuit_id = circuit_id, generator_online = true, discovery_id = definition.id,
    discovery_access_profile_id = definition.access_profile_id, discovery_provenance = provenance,
  })
  if not generator then return nil, generator_failure.reason end
  local breaker, breaker_failure = world:place_object("world_object.power.breaker_legacy", breaker_point.x, breaker_point.y, {
    circuit_id = circuit_id, discovery_id = definition.id, discovery_access_profile_id = definition.access_profile_id,
    discovery_provenance = provenance,
  })
  if not breaker then return nil, breaker_failure.reason end
  local gate, gate_failure = world:place_object("world_object.door.powered_legacy", layout.gate.x, layout.gate.y, {
    circuit_id = circuit_id, door_state = "closed", discovery_id = definition.id,
    discovery_access_profile_id = definition.access_profile_id, discovery_provenance = provenance,
  })
  if not gate then return nil, gate_failure.reason end
  return { cache_object_id = cache.id, gate_object_id = gate.id, clue_object_id = clue.id, generator_object_id = generator.id,
    breaker_object_id = breaker.id, circuit_id = circuit_id, x = layout.cache.x, y = layout.cache.y,
    gate_x = layout.gate.x, gate_y = layout.gate.y, clue_x = layout.clue.x, clue_y = layout.clue.y }
end

local function place_hatch(session, definition, occupied, rng, provenance)
  local layout = gate_layout(session, occupied, rng)
  if not layout then return nil, "no_optional_hatch_site" end
  local world = session.state.world
  local cache, cache_failure = place_cache(world, definition, layout.cache, provenance)
  if not cache then return nil, cache_failure.reason end
  local clue, clue_failure = place_clue(world, definition, layout.clue, provenance)
  if not clue then return nil, clue_failure.reason end
  local gate, gate_failure = world:place_object("world_object.traversal.maintenance_hatch", layout.gate.x, layout.gate.y, {
    discovery_id = definition.id, discovery_access_profile_id = definition.access_profile_id, discovery_provenance = provenance,
  })
  if not gate then return nil, gate_failure.reason end
  return { cache_object_id = cache.id, gate_object_id = gate.id, clue_object_id = clue.id, x = layout.cache.x, y = layout.cache.y,
    gate_x = layout.gate.x, gate_y = layout.gate.y, clue_x = layout.clue.x, clue_y = layout.clue.y }
end

local PLACERS = {
  ["access_profile.discovery.open"] = place_open,
  ["access_profile.discovery.breachable"] = place_breachable,
  ["access_profile.discovery.powered"] = place_powered,
  ["access_profile.discovery.maintenance_hatch"] = place_hatch,
}

function Discoveries.place(session, rng, provenance)
  local state = session.state
  local tracker = state.discovery_state
  if not tracker or tracker.enabled ~= true then return nil, "disabled" end
  if rng:int(1, 100) > Discoveries.PLACEMENT_PERCENT then return nil, "not_selected" end
  local known, assigned = {}, {}
  for _, id in ipairs(state.meta_snapshot.discovered_discovery_ids or {}) do known[id] = true end
  for _, id in ipairs(tracker.assigned_discovery_ids or {}) do assigned[id] = true end
  local definitions = sorted_discoveries(session.registry, state.settings.biome_id, assigned, known)
  local occupied = session:_occupied()
  for _, definition in ipairs(rng:shuffle(definitions)) do
    local placer = PLACERS[definition.access_profile_id]
    local site, reason = placer(session, definition, occupied, rng:derive(definition.id), provenance)
    if site then
      tracker.assigned_discovery_ids[#tracker.assigned_discovery_ids + 1] = definition.id
      table.sort(tracker.assigned_discovery_ids)
      site.discovery_id = definition.id
      site.access_profile_id = definition.access_profile_id
      site.first_time = not known[definition.id]
      site.provenance = provenance
      return site
    end
  end
  return nil, "no_legal_site"
end

return Discoveries
