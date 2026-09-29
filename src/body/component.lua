local Component = {}

function Component.new(definition, instance_id)
  return {
    id = instance_id,
    definition_id = definition.id,
    max_integrity = definition.max_integrity,
    current_integrity = definition.max_integrity,
  }
end

function Component.condition(instance)
  local ratio = instance.current_integrity / instance.max_integrity
  if instance.current_integrity <= 0 then
    return "broken"
  elseif ratio <= 0.25 then
    return "critical"
  elseif ratio < 1 then
    return "damaged"
  end
  return "healthy"
end

function Component.to_data(instance)
  return {
    id = instance.id,
    definition_id = instance.definition_id,
    max_integrity = instance.max_integrity,
    current_integrity = instance.current_integrity,
  }
end

return Component
