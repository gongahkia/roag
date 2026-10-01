-- Small authoritative boundary for adjacent world-object interactions. Input
-- asks for an action; this module validates and mutates simulation state.
local Interaction = {}

local ROLE_PRIORITY = { door = 1, generator = 2, breaker = 3, service = 4, traversal = 5 }

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
  if object.interaction_role == "door" then
    if object.door_state == "open" then
      local gas_occupied = world:is_gas_cell(object.x, object.y)
      return { action("door.close", "CLOSE DOOR", powered and not gas_occupied,
        gas_occupied and "GAS IN DOORWAY" or (powered and nil or "NO POWER")) }
    end
    return { action("door.open", "OPEN DOOR", powered, powered and nil or "NO POWER") }
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
    return { action("traversal.breach", available and "BREACH" or "RESEARCH REQUIRED", available,
      available and nil or "REINFORCED BREACH RESEARCH REQUIRED") }
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
      return result(false, "requires_unlock", "REINFORCED BREACH RESEARCH REQUIRED", { object_id = object.id, action_id = action_id })
    end
    world_result = world:damage_object(object, { amount = object.current_integrity, cause = "traversal", source = "reinforced_breach" })
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

function Interaction.is_adjacent(actor, object)
  return in_range(actor, object)
end

return Interaction
