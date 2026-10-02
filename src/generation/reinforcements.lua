-- Deterministic, optional placement for visible finite reinforcement origins.
-- This phase owns its own named RNG stream and never changes encounter,
-- service, discovery, recurrence, media, or room streams.
local Grid = require("src.world.grid")
local Balance = require("content.balance.legacy")

local Reinforcements = {
  PLACEMENT_PERCENT = Balance.reinforcements.placement_percent,
  MAX_SOURCES_PER_FLOOR = Balance.reinforcements.max_sources_per_floor,
}

local CARDINAL = { { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 } }
local SOURCE_DEFINITION_BY_TYPE = {
  nest = "world_object.reinforcement.nest",
  lift = "world_object.reinforcement.lift",
}

local function contains(values, expected)
  for _, value in ipairs(values or {}) do if value == expected then return true end end
  return false
end

local function sorted_profiles(session)
  local state, available_factions, result = session.state, {}, {}
  for _, enemy in ipairs(state.enemies or {}) do
    available_factions[session:actor_faction_id(enemy)] = true
  end
  for _, profile in pairs(session.registry.reinforcement_profiles) do
    if available_factions[profile.faction_id]
      and contains(profile.allowed_biome_ids, state.settings.biome_id)
      and contains(profile.allowed_tier_ids, state.settings.tier_id) then
      result[#result + 1] = profile
    end
  end
  table.sort(result, function(first, second) return first.id < second.id end)
  return result
end

local function safe_open(world, point, occupied, player)
  local location_key = Grid.key(point.x, point.y)
  return Grid.in_bounds(point.x, point.y)
    and world:is_passable(point.x, point.y)
    and not occupied[location_key]
    and not world:is_hazardous(point.x, point.y)
    and not world:is_harmful_gas_at(point.x, point.y)
    and not world:is_liquid_cell(point.x, point.y)
    and #world:fires_at(point.x, point.y) == 0
    and Grid.distance(player, point) >= 8
end

local function reachable_without(world, player, blocked_key)
  local seen, queue, cursor = {}, {}, 1
  if not player or not world:is_passable(player.x, player.y) then return seen end
  seen[Grid.key(player.x, player.y)] = true
  queue[1] = { x = player.x, y = player.y }
  while queue[cursor] do
    local current = queue[cursor]
    cursor = cursor + 1
    for _, delta in ipairs(CARDINAL) do
      local point = { x = current.x + delta[1], y = current.y + delta[2] }
      local location_key = Grid.key(point.x, point.y)
      if Grid.in_bounds(point.x, point.y) and location_key ~= blocked_key
        and world:is_passable(point.x, point.y) and not seen[location_key] then
        seen[location_key] = true
        queue[#queue + 1] = point
      end
    end
  end
  return seen
end

local function preserves_critical_access(session, point)
  local state = session.state
  local reachable = reachable_without(state.world, state.player, Grid.key(point.x, point.y))
  for _, target in ipairs(state.targets or {}) do
    if not reachable[Grid.key(target.x, target.y)] then return false end
  end
  -- A service kiosk is optional for completion but must not be sealed behind
  -- a newly placed source; its location is already excluded by occupancy.
  for _, object in ipairs(state.world:list_objects()) do
    if object.interaction_role == "service" then
      local adjacent = false
      for _, delta in ipairs(CARDINAL) do
        if reachable[Grid.key(object.x + delta[1], object.y + delta[2])] then adjacent = true; break end
      end
      if not adjacent then return false end
    end
  end
  -- Discovery sites remain optional, but an origin may not turn an already
  -- generated open cache or its visible gate/clue into an inaccessible
  -- object.  Gated caches deliberately sit beyond their gate, so validate
  -- the reachable access face and clue rather than the sealed cache cell.
  local discovery_objects = {}
  for _, object in ipairs(state.world:list_objects()) do
    if object.discovery_id then
      local group = discovery_objects[object.discovery_id] or {}
      group[#group + 1] = object
      discovery_objects[object.discovery_id] = group
    end
  end
  for _, related in pairs(discovery_objects) do
    local cache, gate, clue
    for _, object in ipairs(related) do
      if object.interaction_role == "discovery" then cache = object
      elseif object.interaction_role == "clue" then clue = object
      elseif object.blocks_movement then gate = object end
    end
    if gate then
      local gate_reachable = false
      for _, delta in ipairs(CARDINAL) do
        if reachable[Grid.key(gate.x + delta[1], gate.y + delta[2])] then gate_reachable = true; break end
      end
      if not gate_reachable or not clue or not reachable[Grid.key(clue.x, clue.y)] then return false end
    elseif cache and not reachable[Grid.key(cache.x, cache.y)] then
      return false
    end
  end
  return true
end

local function has_arrival_space(session, point, wave_size, occupied)
  local available = 0
  for _, delta in ipairs(CARDINAL) do
    local candidate = { x = point.x + delta[1], y = point.y + delta[2] }
    if Grid.in_bounds(candidate.x, candidate.y)
      and session.state.world:is_passable(candidate.x, candidate.y)
      and not occupied[Grid.key(candidate.x, candidate.y)]
      and not session.state.world:is_hazardous(candidate.x, candidate.y)
      and not session.state.world:is_harmful_gas_at(candidate.x, candidate.y)
      and not session.state.world:is_liquid_cell(candidate.x, candidate.y)
      and #session.state.world:fires_at(candidate.x, candidate.y) == 0 then
      available = available + 1
    end
  end
  return available >= wave_size
end

local function choose_wave(profile, rng)
  local total = 0
  for _, entry in ipairs(profile.entries) do total = total + entry.weight end
  local wave = {}
  for index = 1, profile.wave_size do
    local roll = rng:int(1, total)
    for _, entry in ipairs(profile.entries) do
      roll = roll - entry.weight
      if roll <= 0 then wave[index] = entry.enemy_id; break end
    end
  end
  return wave
end

function Reinforcements.place(session, rng, provenance)
  local state = session.state
  if not state.reinforcement_state or state.reinforcement_state.enabled ~= true then return nil, "disabled" end
  for _, object in ipairs(state.world:list_objects()) do
    if object.interaction_role == "reinforcement" then return nil, "source_already_placed" end
  end
  if rng:int(1, 100) > Reinforcements.PLACEMENT_PERCENT then return nil, "not_selected" end
  local profiles = sorted_profiles(session)
  if #profiles == 0 then return nil, "no_eligible_profile" end
  local profile = rng:choice(profiles)
  local wave = choose_wave(profile, rng:derive(profile.id .. ".wave"))
  local occupied, candidates = session:_occupied(), {}
  for _, point in ipairs(session:_reachable_floor_cells()) do
    if safe_open(state.world, point, occupied, state.player)
      and has_arrival_space(session, point, profile.wave_size, occupied) then
      candidates[#candidates + 1] = point
    end
  end
  if #candidates == 0 then return nil, "no_safe_source_site" end
  -- Articulation checks are intentionally bounded.  Every candidate remains
  -- safe and reachable; inspecting a small deterministic sample is enough to
  -- reject a route-blocking placement without turning a batch into thousands
  -- of full-grid flood fills.
  local point
  local shuffled = rng:derive(profile.id .. ".placement"):shuffle(candidates)
  for index = 1, math.min(#shuffled, 16) do
    if preserves_critical_access(session, shuffled[index]) then point = shuffled[index]; break end
  end
  if not point then return nil, "no_noncritical_source_site" end
  local definition_id = assert(SOURCE_DEFINITION_BY_TYPE[profile.source_type], "Unknown reinforcement source type")
  local source, failure = state.world:place_object(definition_id, point.x, point.y, {
    reinforcement_profile_id = profile.id,
    reinforcement_faction_id = profile.faction_id,
    reinforcement_charges = 1,
    reinforcement_state = "idle",
    reinforcement_just_armed = false,
    reinforcement_wave_enemy_ids = wave,
    reinforcement_provenance = provenance,
  })
  if not source then return nil, failure.reason end
  return {
    source_object_id = source.id,
    source_type = profile.source_type,
    faction_id = profile.faction_id,
    profile_id = profile.id,
    charges = source.reinforcement_charges,
    wave_enemy_ids = wave,
    provenance = provenance,
    x = point.x,
    y = point.y,
  }
end

return Reinforcements
