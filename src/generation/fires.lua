-- Initial damaged-infrastructure fire is a small Reactor-only composition of
-- the existing finite fire system.  It ignites one ordinary flammable world
-- object, never creates a source, and uses its own generation stream.
local Grid = require("src.world.grid")

local FireGeneration = {}

function FireGeneration.place(world, terrain, player, rng)
  if terrain ~= "reactor" then return {} end
  local candidates = {}
  for _, object in ipairs(world:list_objects()) do
    local material = world.registry:get_material(object.material_id)
    if material.flammable and Grid.distance(player, object) >= 7 and not world:is_hazardous(object.x, object.y)
      and not world:is_liquid_cell(object.x, object.y) and not world:is_gas_cell(object.x, object.y) then
      candidates[#candidates + 1] = object
    end
  end
  local placed = {}
  local selected = rng:shuffle(candidates)[1]
  if selected then
    local fire, result = world:create_fire({ kind = "object", target_id = selected.id }, {
      source = "generation.reactor_damaged_infrastructure",
      terrain = terrain,
    })
    assert(fire, result and result.reason)
    placed[#placed + 1] = fire
  end
  return placed
end

return FireGeneration
