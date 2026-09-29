local Component = require("src.body.component")

local Factory = {}
Factory.__index = Factory

function Factory.new(registry, sequence_owner)
  assert(sequence_owner, "Component instance identity requires a sequence owner")
  sequence_owner.next_component_sequence = sequence_owner.next_component_sequence or 1
  return setmetatable({
    registry = registry,
    sequence_owner = sequence_owner,
  }, Factory)
end

function Factory:create(definition_id)
  local sequence = self.sequence_owner.next_component_sequence
  self.sequence_owner.next_component_sequence = sequence + 1
  local instance_id = string.format("component:%06d", sequence)
  return Component.new(self.registry:get_component(definition_id), instance_id)
end

return Factory
