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
  if instance.current_integrity <= 0 then
    return "broken"
  end
  local ratio = instance.current_integrity / instance.max_integrity
  if ratio <= 1 / 3 then
    return "critical"
  elseif ratio <= 2 / 3 then
    return "damaged"
  end
  return "healthy"
end

function Component.is_functional(instance)
  return instance.current_integrity > 0
end

function Component.to_data(instance)
  return {
    id = instance.id,
    definition_id = instance.definition_id,
    max_integrity = instance.max_integrity,
    current_integrity = instance.current_integrity,
    condition = Component.condition(instance),
  }
end

return Component
