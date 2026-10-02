-- Finite combat-alarm reinforcement lifecycle.  Sources are authoritative
-- world objects; this module only changes their ordinary saved state and
-- creates normal enemy actors at their visible origin.
local Grid = require("src.world.grid")
local Balance = require("content.balance.legacy")

local Reinforcements = { WARNING_DELAY = Balance.reinforcements.warning_delay_turns }
local CARDINAL = { { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 } }

local function source_objects(session)
  local result = {}
  for _, object in ipairs(session.state.world and session.state.world:list_objects() or {}) do
    if object.interaction_role == "reinforcement" then result[#result + 1] = object end
  end
  return result
end

local function message_for(source, armed)
  if source.reinforcement_profile_id and source.reinforcement_profile_id:find("feral", 1, true) then
    return armed and "NEST DISTURBED." or "NEST FALLS SILENT."
  elseif source.reinforcement_profile_id and source.reinforcement_profile_id:find("cult", 1, true) then
    return armed and "ACCESS BREACH DETECTED." or "ACCESS BREACH COLLAPSED."
  end
  return armed and "LIFT ACTIVATING." or "LIFT DISABLED."
end

function Reinforcements.arm_for_combat(session, first, second)
  if not first or not second or not session:are_hostile(first, second) then return { applied = false, code = "not_hostile" } end
  local first_faction, second_faction = session:actor_faction_id(first), session:actor_faction_id(second)
  for _, source in ipairs(source_objects(session)) do
    if not source.destroyed and source.reinforcement_state == "idle" and source.reinforcement_charges == 1
      and (source.reinforcement_faction_id == first_faction or source.reinforcement_faction_id == second_faction) then
      source.reinforcement_state = "armed"
      source.reinforcement_delay = Reinforcements.WARNING_DELAY
      source.reinforcement_just_armed = true
      session:_log(message_for(source, true))
      return { applied = true, source_id = source.id, delay = source.reinforcement_delay,
        faction_id = source.reinforcement_faction_id }
    end
  end
  return { applied = false, code = "no_matching_source" }
end

local function legal_arrival_cells(session, source)
  local cells, occupied, world = {}, session:_occupied(), session.state.world
  for _, delta in ipairs(CARDINAL) do
    local point = { x = source.x + delta[1], y = source.y + delta[2] }
    if Grid.in_bounds(point.x, point.y) and world:is_passable(point.x, point.y)
      and not occupied[Grid.key(point.x, point.y)]
      and not world:is_hazardous(point.x, point.y)
      and not world:is_harmful_gas_at(point.x, point.y)
      and not world:is_liquid_cell(point.x, point.y)
      and #world:fires_at(point.x, point.y) == 0 then
      cells[#cells + 1] = point
    end
  end
  return cells
end

function Reinforcements.tick(session)
  local results = {}
  for _, source in ipairs(source_objects(session)) do
    if source.reinforcement_state == "armed" then
      if source.destroyed or source.reinforcement_charges ~= 1 then
        source.reinforcement_state, source.reinforcement_charges, source.reinforcement_delay, source.reinforcement_just_armed = "cancelled", 0, nil, false
        session:_log(message_for(source, false))
        results[#results + 1] = { applied = true, code = "cancelled", source_id = source.id }
      elseif source.reinforcement_just_armed then
        source.reinforcement_just_armed = false
        results[#results + 1] = { applied = true, code = "armed", source_id = source.id, delay = source.reinforcement_delay }
      else
        source.reinforcement_delay = source.reinforcement_delay - 1
        if source.reinforcement_delay > 0 then
          results[#results + 1] = { applied = true, code = "countdown", source_id = source.id, delay = source.reinforcement_delay }
        else
          local cells, wave = legal_arrival_cells(session, source), source.reinforcement_wave_enemy_ids
          source.reinforcement_delay, source.reinforcement_charges, source.reinforcement_state, source.reinforcement_just_armed = nil, 0, "spent", false
          if #cells >= #wave then
            local spawned = {}
            for index, enemy_id in ipairs(wave) do
              local point = cells[index]
              local enemy = session:_make_enemy(enemy_id, point, { scrap_award = true, reinforcement_source_id = source.id })
              session.state.enemies[#session.state.enemies + 1] = enemy
              spawned[#spawned + 1] = enemy
            end
            session:_log(#spawned == 1 and "REINFORCEMENT ARRIVED." or "REINFORCEMENTS ARRIVED.")
            results[#results + 1] = { applied = true, code = "arrived", source_id = source.id, actors = spawned }
          else
            session:_log("REINFORCEMENT ARRIVAL BLOCKED.")
            results[#results + 1] = { applied = false, code = "arrival_blocked", source_id = source.id }
          end
        end
      end
    end
  end
  if #results > 0 then session:validate_physical_ownership() end
  return results
end

return Reinforcements
