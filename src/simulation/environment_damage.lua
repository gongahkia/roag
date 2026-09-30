-- Terrain has different invariants from actors, so this remains separate from
-- BodyDamage while sharing small semantic damage descriptors.
local EnvironmentDamage = {}

function EnvironmentDamage.apply(world, x, y, spec)
  if type(spec) ~= "table" then
    return { applied = false, code = "invalid_damage", x = x, y = y, reason = "Terrain damage specification must be a table" }
  end
  if type(spec.amount) ~= "number" or spec.amount <= 0 then
    return { applied = false, code = "invalid_damage", x = x, y = y, reason = "Terrain damage amount must be positive" }
  end
  if spec.cause ~= "explosive" and spec.cause ~= "kinetic" then
    return { applied = false, code = "invalid_cause", x = x, y = y, reason = "Terrain damage cause must be explosive or kinetic" }
  end
  return world:damage_terrain(x, y, spec)
end

return EnvironmentDamage
