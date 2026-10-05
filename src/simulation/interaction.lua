-- Small authoritative boundary for adjacent world-object interactions. Input
-- asks for an action; this module validates and mutates simulation state.
local Interaction = {}

local ROLE_PRIORITY = { zone_connection = 0, reconstruction_station = 1, storage = 2, door = 3, generator = 4, breaker = 5, service = 6, traversal = 7, discovery = 8, clue = 9 }

local function result(applied, code, reason, extra)
  local value = {
    applied = applied,
    code = code,
    reason = reason,
  }
  for key, item in pairs(extra or {}) do
    value[key] = item
  end
  return value
end

local function in_range(actor, object)
  return actor and object
    and math.abs(actor.x - object.x) <= 1
    and math.abs(actor.y - object.y) <= 1
    and not (actor.x == object.x and actor.y == object.y)
end

local function on_cell(object, x, y)
  return object and object.x == x and object.y == y
end

local function action(id, label, available, reason)
  return {
    id = id,
    label = label,
    available = available,
    reason = reason,
  }
end

function Interaction.actions_for(world, object, session)
  if not object or object.destroyed or not object.interaction_role then
    return {}
  end
  local powered = object.circuit_id and world:is_circuit_powered(object.circuit_id) or false
  local definition = session and session.registry:get_world_object(object.definition_id) or nil
  if object.interaction_role == "door" then
    local operational = not definition or definition.power_required ~= true or powered
    if object.door_state == "open" then
      local gas_occupied = world:is_gas_cell(object.x, object.y)
      return { action("door.close", "CLOSE DOOR", operational and not gas_occupied,
        gas_occupied and "GAS IN DOORWAY" or (operational and nil or "NO POWER")) }
    end
    return { action("door.open", "OPEN DOOR", operational, operational and nil or "NO POWER") }
  elseif object.interaction_role == "generator" then
    return {
      action("generator.toggle", object.generator_online and "TURN GENERATOR OFF" or "TURN GENERATOR ON", true),
    }
  elseif object.interaction_role == "breaker" then
    local circuit = world:get_circuit(object.circuit_id)
    return {
      action("breaker.toggle", circuit.enabled and "DISABLE CIRCUIT" or "ENABLE CIRCUIT", true),
    }
  elseif object.interaction_role == "service" then
    return { action("service.open", "ACCESS SERVICE", true) }
  elseif object.interaction_role == "traversal" then
    local available = session and session:has_meta_unlock(object.required_unlock)
    local maintenance = object.required_unlock == "unlock.traversal.maintenance_override"
    local label = maintenance and "OVERRIDE HATCH" or "BREACH"
    local reason = maintenance and "SEALED MAINTENANCE HATCH — OVERRIDE RESEARCH REQUIRED"
      or "REINFORCED BREACH RESEARCH REQUIRED"
    return { action("traversal.breach", available and label or "RESEARCH REQUIRED", available, available and nil or reason) }
  elseif object.interaction_role == "zone_connection" then
    return { action("zone_connection.use", session and session:zone_connection_label(object) or "TRAVEL", true) }
  elseif object.interaction_role == "reconstruction_station" then
    if session and session.campaign and not session:is_reconstruction_anchor(object) then
      return {
        action("reconstruction.set_anchor", "SET AS RECONSTRUCTION ANCHOR", true),
        action("reconstruction.open", "RECONSTRUCT BODY", true),
      }
    end
    return { action("reconstruction.open", "RECONSTRUCT BODY", session and session.campaign ~= nil,
      session and session.campaign and nil or "CAMPAIGN STATION REQUIRED") }
  elseif object.interaction_role == "storage" then
    return { action("storage.open", "OPEN STORAGE", session and session.campaign ~= nil,
      session and session.campaign and nil or "CAMPAIGN STORAGE REQUIRED") }
  elseif object.interaction_role == "discovery" then
    local claimed = object.discovery_claimed == true
    return { action("discovery.claim", claimed and "CACHE CLAIMED" or "RECOVER DISCOVERY", not claimed,
      claimed and "DISCOVERY ALREADY CLAIMED" or nil) }
  elseif object.interaction_role == "clue" then
    return { action("clue.read", "READ ACCESS MARKING", true) }
  end
  return {}
end

-- The compact UI has no chooser yet. Fallback targeting therefore uses a
-- stable role priority (door, generator, breaker), then allocation order.
function Interaction.available(session, actor)
  local world = session and session.state and session.state.world
  if not world or not actor then
    return {}
  end
  local available = {}
  for order, object in ipairs(world:list_objects()) do
    if object.interaction_role and in_range(actor, object) then
      available[#available + 1] = {
        object_id = object.id,
        display_name = session.registry:get_world_object(object.definition_id).display_name,
        interaction_role = object.interaction_role,
        x = object.x,
        y = object.y,
        circuit_id = object.circuit_id,
        powered = world:is_circuit_powered(object.circuit_id),
        actions = Interaction.actions_for(world, object, session),
        _order = order,
      }
    end
  end
  table.sort(available, function(left, right)
    local left_priority = ROLE_PRIORITY[left.interaction_role] or 99
    local right_priority = ROLE_PRIORITY[right.interaction_role] or 99
    if left_priority ~= right_priority then
      return left_priority < right_priority
    end
    return left._order < right._order
  end)
  for _, entry in ipairs(available) do
    entry._order = nil
  end
  return available
end

-- Campaign context use is deliberately narrower than the legacy adjacent
-- interaction query.  The input layer supplies no target selection: the
-- current facing direction selects exactly one cell, and this helper gives
-- that cell the same stable object/action ordering as the older query.
function Interaction.available_at(session, actor, x, y)
  local world = session and session.state and session.state.world
  if not world or not actor or x == nil or y == nil then
    return {}
  end
  local available = {}
  for order, object in ipairs(world:list_objects()) do
    if object.interaction_role and on_cell(object, x, y) then
      available[#available + 1] = {
        object_id = object.id,
        display_name = session.registry:get_world_object(object.definition_id).display_name,
        interaction_role = object.interaction_role,
        x = object.x,
        y = object.y,
        circuit_id = object.circuit_id,
        powered = world:is_circuit_powered(object.circuit_id),
        actions = Interaction.actions_for(world, object, session),
        _order = order,
      }
    end
  end
  table.sort(available, function(left, right)
    local left_priority = ROLE_PRIORITY[left.interaction_role] or 99
    local right_priority = ROLE_PRIORITY[right.interaction_role] or 99
    if left_priority ~= right_priority then
      return left_priority < right_priority
    end
    return left._order < right._order
  end)
  for _, entry in ipairs(available) do
    entry._order = nil
  end
  return available
end

function Interaction.perform(session, actor, object_id, action_id)
  local world = session and session.state and session.state.world
  if not world or not actor then
    return result(false, "no_world", "No active world")
  end
  local object = world:get_object(object_id)
  if not object then
    return result(false, "invalid_target", "Unknown world object", { object_id = object_id, action_id = action_id })
  end
  if not object.interaction_role then
    return result(false, "not_interactable", "Object cannot be interacted with", { object_id = object.id, action_id = action_id })
  end
  if object.destroyed then
    return result(false, "destroyed", "Object is destroyed", { object_id = object.id, action_id = action_id })
  end
  if not in_range(actor, object) then
    return result(false, "out_of_range", "Object is not adjacent", { object_id = object.id, action_id = action_id })
  end

  local world_result
  if action_id == "door.open" and object.interaction_role == "door" then
    world_result = world:set_door_state(object, "open")
  elseif action_id == "door.close" and object.interaction_role == "door" then
    if session:_actor_at(object.x, object.y) then
      return result(false, "occupied", "Doorway is occupied", { object_id = object.id, action_id = action_id })
    end
    world_result = world:set_door_state(object, "closed")
  elseif action_id == "generator.toggle" and object.interaction_role == "generator" then
    world_result = world:set_generator_online(object, not object.generator_online)
  elseif action_id == "breaker.toggle" and object.interaction_role == "breaker" then
    local circuit = world:get_circuit(object.circuit_id)
    world_result = world:set_circuit_enabled(object.circuit_id, not circuit.enabled)
  elseif action_id == "service.open" and object.interaction_role == "service" then
    world_result = result(true, "service_open", nil, {
      service_id = object.service_id,
      service_object_id = object.id,
    })
  elseif action_id == "traversal.breach" and object.interaction_role == "traversal" then
    if not session:has_meta_unlock(object.required_unlock) then
      local maintenance = object.required_unlock == "unlock.traversal.maintenance_override"
      return result(false, "requires_unlock", maintenance and "SEALED MAINTENANCE HATCH — OVERRIDE RESEARCH REQUIRED"
        or "REINFORCED BREACH RESEARCH REQUIRED", { object_id = object.id, action_id = action_id })
    end
    world_result = world:damage_object(object, { amount = object.current_integrity, cause = "traversal",
      source = object.required_unlock == "unlock.traversal.maintenance_override" and "maintenance_override" or "reinforced_breach" })
  elseif action_id == "zone_connection.use" and object.interaction_role == "zone_connection" then
    world_result = session:use_zone_connection(object)
  elseif action_id == "reconstruction.open" and object.interaction_role == "reconstruction_station" then
    world_result = session:open_reconstruction_station(object)
  elseif action_id == "reconstruction.set_anchor" and object.interaction_role == "reconstruction_station" then
    world_result = session:set_reconstruction_anchor(object)
  elseif action_id == "storage.open" and object.interaction_role == "storage" then
    world_result = session:open_storage(object)
  elseif action_id == "discovery.claim" and object.interaction_role == "discovery" then
    world_result = session:claim_discovery(object)
  elseif action_id == "clue.read" and object.interaction_role == "clue" then
    local discovery = session.registry:get_discovery(object.discovery_id)
    world_result = result(true, "clue_read", discovery.presentation.clue, {
      discovery_id = discovery.id,
      clue = discovery.presentation.clue,
    })
  else
    return result(false, "invalid_action", "Action is not available for this object", {
      object_id = object.id,
      action_id = action_id,
    })
  end

  world_result.object_id = object.id
  world_result.action_id = action_id
  return world_result
end

function Interaction.primary(session, actor)
  local nearby = Interaction.available(session, actor)
  if #nearby == 0 then
    return result(false, "not_interactable", "No adjacent interactable object")
  end
  local target = nearby[1]
  local selected = target.actions[1]
  if not selected then
    return result(false, "not_interactable", "Object has no available interaction", { object_id = target.object_id })
  end
  return Interaction.perform(session, actor, target.object_id, selected.id)
end

function Interaction.primary_at(session, actor, x, y)
  local targets = Interaction.available_at(session, actor, x, y)
  if #targets == 0 then
    return result(false, "not_interactable", "Nothing to interact with")
  end
  local target = targets[1]
  local selected = target.actions[1]
  if not selected then
    return result(false, "not_interactable", "Object has no available interaction", { object_id = target.object_id })
  end
  return Interaction.perform(session, actor, target.object_id, selected.id)
end

function Interaction.is_adjacent(actor, object)
  return in_range(actor, object)
end

return Interaction
