local Component = {}

function Component.new(definition, instance_id)
  return {
    id = instance_id,
    definition_id = definition.id,
    max_integrity = definition.max_integrity,
    current_integrity = definition.max_integrity,
  }
end

function Component.from_data(definition, data)
  assert(type(data) == "table", "Component data must be a table")
  assert(data.definition_id == definition.id, "Component data definition does not match")
  assert(type(data.id) == "string" and data.id ~= "", "Component data must include a stable ID")
  assert(type(data.current_integrity) == "number", "Component data must include current integrity")
  local origin = nil
  if data.origin ~= nil then
    assert(type(data.origin) == "table"
      and type(data.origin.archive_id) == "string" and data.origin.archive_id:match("^fallen:%d+$")
      and type(data.origin.source_run_id) == "string" and data.origin.source_run_id:match("^run:%d+$")
      and type(data.origin.source_component_id) == "string" and data.origin.source_component_id:match("^component:%d+$"),
      "Component provenance is invalid")
    origin = {
      archive_id = data.origin.archive_id,
      source_run_id = data.origin.source_run_id,
      source_component_id = data.origin.source_component_id,
    }
  end
  return {
    id = data.id,
    definition_id = definition.id,
    max_integrity = definition.max_integrity,
    current_integrity = math.max(0, math.min(definition.max_integrity, data.current_integrity)),
    origin = origin,
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
  local data = {
    id = instance.id,
    definition_id = instance.definition_id,
    max_integrity = instance.max_integrity,
    current_integrity = instance.current_integrity,
    condition = Component.condition(instance),
  }
  if instance.origin then
    data.origin = {
      archive_id = instance.origin.archive_id,
      source_run_id = instance.origin.source_run_id,
      source_component_id = instance.origin.source_component_id,
    }
  end
  return data
end

return Component
