-- Authoritative localized component integrity changes. It is intentionally
-- independent of actor type: callers provide any shared Body instance.
local Component = require("src.body.component")

local BodyDamage = {}

local function failure(spec, reason)
  return {
    applied = false,
    cause = spec.cause or "kinetic",
    source = spec.source,
    slot_id = spec.slot_id,
    component_id = spec.component_id,
    reason = reason,
  }
end

local function resolve_target(body, spec)
  local target
  if spec.slot_id then
    local slot = body:get_slot(spec.slot_id)
    if not slot then
      return nil, failure(spec, "Unknown body slot '" .. spec.slot_id .. "'")
    end
    if not slot.component then
      return nil, failure(spec, "Body slot '" .. spec.slot_id .. "' is empty")
    end
    target = {
      slot_id = spec.slot_id,
      slot = slot,
      component = slot.component,
    }
    if spec.component_id and target.component.id ~= spec.component_id then
      return nil, failure(spec, "Component '" .. spec.component_id .. "' is not installed in slot '" .. spec.slot_id .. "'")
    end
  elseif spec.component_id then
    target = body:find_component(spec.component_id)
    if not target then
      return nil, failure(spec, "Component instance '" .. spec.component_id .. "' is not installed")
    end
  else
    local candidates = body:list_installed_slots()
    if #candidates == 0 then
      return nil, failure(spec, "Body has no installed components to target")
    end
    if not spec.rng then
      return nil, failure(spec, "Automatic body damage requires a deterministic RNG")
    end
    target = spec.rng:choice(candidates)
  end
  return target
end

local function apply_change(body, spec, direction)
  if type(spec) ~= "table" then
    return failure({}, "Damage specification must be a table")
  end
  if type(spec.amount) ~= "number" or spec.amount <= 0 then
    return failure(spec, "Damage amount must be a positive number")
  end

  local target, target_failure = resolve_target(body, spec)
  if not target then
    return target_failure
  end
  local component = target.component
  local previous_integrity = component.current_integrity
  local previous_condition = Component.condition(component)
  local new_integrity
  if direction < 0 then
    new_integrity = math.max(0, previous_integrity - spec.amount)
  else
    new_integrity = math.min(component.max_integrity, previous_integrity + spec.amount)
  end
  component.current_integrity = new_integrity
  local new_condition = Component.condition(component)

  return {
    applied = new_integrity ~= previous_integrity,
    cause = spec.cause or (direction < 0 and "kinetic" or "repair"),
    source = spec.source,
    amount = spec.amount,
    slot_id = target.slot_id,
    component_id = component.id,
    definition_id = component.definition_id,
    previous_integrity = previous_integrity,
    new_integrity = new_integrity,
    previous_condition = previous_condition,
    new_condition = new_condition,
    condition_changed = previous_condition ~= new_condition,
    became_broken = previous_condition ~= "broken" and new_condition == "broken",
    became_functional = previous_condition == "broken" and new_condition ~= "broken",
  }
end

function BodyDamage.apply(body, spec)
  return apply_change(body, spec, -1)
end

function BodyDamage.apply_wear(body, spec)
  local wear_spec = {}
  for key, value in pairs(spec or {}) do
    wear_spec[key] = value
  end
  wear_spec.cause = "wear"
  return apply_change(body, wear_spec, -1)
end

function BodyDamage.restore(body, spec)
  return apply_change(body, spec, 1)
end

return BodyDamage
