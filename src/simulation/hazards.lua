-- Declarative on-enter hazard dispatch. This module owns deterministic hazard
-- ordering; Session supplies the actor-damage/death policy at the boundary.
local Hazards = {}

function Hazards.on_actor_enter(world, actor, x, y, context, apply_effect)
  local results = {}
  if not world or not actor then
    return { applied = false, code = "invalid_entry", results = results }
  end
  for _, hazard in ipairs(world:hazards_at(x, y)) do
    local definition = world.registry:get_hazard(hazard.definition_id)
    if definition.trigger == "on_enter" then
      local result = apply_effect(actor, hazard, definition, context or {})
      results[#results + 1] = result
      if result and result.dead then
        return { applied = #results > 0, code = "actor_destroyed", results = results, dead = true }
      end
    end
  end
  return { applied = #results > 0, code = #results > 0 and "applied" or "no_hazard", results = results }
end

return Hazards
