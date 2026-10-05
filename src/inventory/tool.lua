-- Conventional carried tools deliberately remain smaller than Body components.
-- They are physical inventory objects with one stable identity, independent
-- durability, and declarative content-provided combat/modification profiles.
local Tool = {}

function Tool.new(definition, instance_id)
  assert(type(instance_id) == "string" and instance_id ~= "", "Tool requires a stable physical ID")
  return {
    id = instance_id,
    definition_id = definition.id,
    maximum_durability = definition.max_durability,
    current_durability = definition.max_durability,
  }
end

function Tool.from_data(definition, data)
  assert(type(data) == "table", "Tool data must be a table")
  assert(data.definition_id == definition.id, "Tool data definition does not match")
  assert(type(data.id) == "string" and data.id ~= "", "Tool data must include a stable ID")
  assert(type(data.current_durability) == "number", "Tool data must include current durability")
  return {
    id = data.id,
    definition_id = definition.id,
    maximum_durability = definition.max_durability,
    current_durability = math.max(0, math.min(definition.max_durability, math.floor(data.current_durability))),
  }
end

function Tool.condition(instance)
  if instance.current_durability <= 0 then return "broken" end
  local ratio = instance.current_durability / instance.maximum_durability
  if ratio <= 1 / 3 then return "critical" end
  if ratio <= 2 / 3 then return "damaged" end
  return "healthy"
end

function Tool.is_functional(instance)
  return instance and instance.current_durability > 0
end

function Tool.to_data(instance)
  return {
    id = instance.id,
    definition_id = instance.definition_id,
    maximum_durability = instance.maximum_durability,
    current_durability = instance.current_durability,
    condition = Tool.condition(instance),
  }
end

return Tool
