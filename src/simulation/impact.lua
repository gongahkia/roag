-- Collision consequences remain separate from Force. Force reports what was
-- physically blocked; this small policy-neutral service derives impact data.
local Impact = {}

function Impact.from_force(actor, force_result, force_spec)
  if not actor or not force_result or not force_result.blocked
    or (force_result.remaining_distance or 0) <= 0 then
    return { applied = false, code = "no_impact" }
  end
  -- Actor-to-actor collision consequences intentionally remain deferred. A
  -- terrain or cover blocker is a stable physical impact surface for 6C.
  if force_result.blocker_code ~= "blocked_world" then
    return { applied = false, code = "nonstructural_blocker" }
  end
  local severity = force_result.remaining_distance
  return {
    applied = true,
    code = "impact",
    severity = severity,
    cause = "kinetic",
    source_actor_id = force_spec and force_spec.source_actor_id,
    source_component_id = force_spec and force_spec.source_component_id,
    ability_id = force_spec and force_spec.ability_id,
    force_cause = force_spec and force_spec.cause,
    blocker_code = force_result.blocker_code,
    x = force_result.final_x,
    y = force_result.final_y,
  }
end

return Impact
