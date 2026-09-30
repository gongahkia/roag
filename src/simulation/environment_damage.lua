-- Terrain has different invariants from actors, so this remains separate from
-- BodyDamage while sharing small semantic damage descriptors.
local EnvironmentDamage = {}

local function validate_spec(x, y, spec)
  if type(spec) ~= "table" then
    return { applied = false, code = "invalid_damage", x = x, y = y, reason = "Terrain damage specification must be a table" }
  end
  if type(spec.amount) ~= "number" or spec.amount <= 0 then
    return { applied = false, code = "invalid_damage", x = x, y = y, reason = "Terrain damage amount must be positive" }
  end
  if spec.cause ~= "explosive" and spec.cause ~= "kinetic" then
    return { applied = false, code = "invalid_cause", x = x, y = y, reason = "Terrain damage cause must be explosive or kinetic" }
  end
  return nil
end

function EnvironmentDamage.apply_to_terrain(world, x, y, spec)
  local failure = validate_spec(x, y, spec)
  if failure then
    return failure
  end
  return world:damage_terrain(x, y, spec)
end

function EnvironmentDamage.apply_to_object(world, object_or_id, spec)
  local failure = validate_spec(nil, nil, spec)
  if failure then
    failure.target_type = "world_object"
    return failure
  end
  return world:damage_object(object_or_id, spec)
end

-- Compatibility entry point retained for terrain callers from Tranche 6A.
function EnvironmentDamage.apply(world, x, y, spec)
  return EnvironmentDamage.apply_to_terrain(world, x, y, spec)
end

return EnvironmentDamage
