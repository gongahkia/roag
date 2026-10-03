-- Compact authoritative OW-05 construction transactions. UI only selects a
-- recipe/cell; this module owns affordability, protected topology cells, and
-- ordinary World object placement.
local Grid = require("src.world.grid")

local Building = {
  RECOVERY_NUMERATOR = 1,
  RECOVERY_DENOMINATOR = 2,
}

local function sorted_costs(costs)
  local values = {}
  for resource_id, amount in pairs(costs or {}) do values[#values + 1] = { resource_id = resource_id, amount = amount } end
  table.sort(values, function(left, right) return left.resource_id < right.resource_id end)
  return values
end

function Building.recipes(registry)
  local values = {}
  for id, recipe in pairs(registry.construction_recipes) do
    values[#values + 1] = { id = id, display_name = recipe.display_name, world_object_id = recipe.world_object_id,
      costs = sorted_costs(recipe.costs) }
  end
  table.sort(values, function(left, right) return left.id < right.id end)
  return values
end

function Building.recovery_yields(recipe)
  local yields = {}
  for resource_id, amount in pairs(recipe.costs) do
    local recovered = math.floor(amount * Building.RECOVERY_NUMERATOR / Building.RECOVERY_DENOMINATOR)
    if recovered >= 1 then yields[#yields + 1] = { resource_id = resource_id, amount = recovered } end
  end
  table.sort(yields, function(left, right) return left.resource_id < right.resource_id end)
  return yields
end

function Building.resource_counts(inventory)
  return inventory:resource_counts()
end

local function protected_cell(session, x, y)
  local state = session.state
  if state.surface_connector_cells and state.surface_connector_cells[Grid.key(x, y)] then return true, "blocks_travel_connection" end
  if state.protected_content_cells and state.protected_content_cells[Grid.key(x, y)] then
    return true, "blocks_world_content"
  end
  local campaign = session.campaign
  if campaign and campaign.state and campaign.state.reconstruction_anchor
    and campaign.active_zone and campaign.active_zone.key
    and campaign.state.reconstruction_anchor.zone_key
    and campaign.state.reconstruction_anchor.zone_key.world_x == campaign.active_zone.key.world_x
    and campaign.state.reconstruction_anchor.zone_key.world_y == campaign.active_zone.key.world_y
    and campaign.state.reconstruction_anchor.zone_key.z == campaign.active_zone.key.z then
    local station = state.world:get_object(campaign.state.reconstruction_anchor.station_object_id)
    if station and math.max(math.abs(station.x - x), math.abs(station.y - y)) <= 1 then
      return true, "blocks_reconstruction_anchor"
    end
  end
  return false
end

function Building.validate(session, recipe_id, x, y)
  if not session or not session.campaign then
    return { applied = false, code = "campaign_only", reason = "Construction is available only in a campaign" }
  end
  local recipe = session.registry.construction_recipes[recipe_id]
  if not recipe then return { applied = false, code = "unknown_recipe", reason = "Unknown construction recipe" } end
  if type(x) ~= "number" or type(y) ~= "number" or x % 1 ~= 0 or y % 1 ~= 0 or not Grid.in_bounds(x, y) then
    return { applied = false, code = "out_of_bounds", reason = "Build location is outside the zone" }
  end
  local player, world = session.state.player, session.state.world
  if not player or math.max(math.abs(player.x - x), math.abs(player.y - y)) > 1 then
    return { applied = false, code = "out_of_range", reason = "Build location must be adjacent" }
  end
  if player.x == x and player.y == y then
    return { applied = false, code = "player_cell", reason = "Cannot build on your current cell" }
  end
  local protected, code = protected_cell(session, x, y)
  if protected then return { applied = false, code = code, reason = "Build location is protected for world travel" } end
  if not world:terrain_is_passable(x, y) then return { applied = false, code = "blocked_terrain", reason = "Build location is blocked by terrain" } end
  if world:object_at(x, y) then return { applied = false, code = "occupied", reason = "Build location is occupied" } end
  if world:is_hazardous(x, y) then return { applied = false, code = "occupied_hazard", reason = "Build location contains a hazard" } end
  if #world:ground_items_at(x, y) > 0 then return { applied = false, code = "occupied_ground_item", reason = "Clear ground items before building here" } end
  if session:_actor_at(x, y) then return { applied = false, code = "occupied_actor", reason = "Build location is occupied by an actor" } end
  for _, corpse in ipairs(session.state.corpses or {}) do
    if corpse.x == x and corpse.y == y then return { applied = false, code = "occupied_corpse", reason = "Build location is occupied by remains" } end
  end
  local affordable, missing = session.state.inventory:can_afford(recipe.costs)
  if not affordable then return { applied = false, code = "insufficient_material", reason = "Not enough " .. session.registry:get_resource(missing).display_name } end
  return { applied = true, recipe = recipe, x = x, y = y, code = "valid" }
end

function Building.circuit_id(session)
  local identity = assert(session.identity_allocator, "Campaign construction requires a zone identity allocator")
  local key = identity.key
  return string.format("circuit:%s:zone:%d:%d:%d:construction", identity.campaign_id, key.world_x, key.world_y, key.z)
end

function Building.place(session, recipe_id, x, y)
  local validation = Building.validate(session, recipe_id, x, y)
  if not validation.applied then return validation end
  local recipe, world = validation.recipe, session.state.world
  local definition = session.registry:get_world_object(recipe.world_object_id)
  local options = {
    constructed = true,
    construction_recipe_id = recipe.id,
    construction_campaign_id = session.campaign.state.campaign_id,
    construction_recovery_yields = Building.recovery_yields(recipe),
  }
  local created_circuit = false
  if definition.interaction_role == "generator" or definition.interaction_role == "breaker" then
    local circuit_id = Building.circuit_id(session)
    if not world:get_circuit(circuit_id) then
      local circuit = world:register_circuit(circuit_id, { enabled = true })
      if not circuit.applied then return circuit end
      created_circuit = true
    end
    options.circuit_id = circuit_id
    if definition.interaction_role == "generator" then options.generator_online = true end
  end
  local consumed, consumption_error = session.state.inventory:consume_resources(recipe.costs)
  if not consumed then return { applied = false, code = "insufficient_material", reason = consumption_error } end
  local object, placement = world:place_object(recipe.world_object_id, x, y, options)
  if not object then
    session.state.inventory:restore_consumed_resources(consumed)
    if created_circuit then world:remove_empty_circuit(options.circuit_id) end
    return { applied = false, code = placement.code or "placement_failed", reason = placement.reason or "Build placement failed" }
  end
  session:validate_physical_ownership()
  return { applied = true, code = "built", object_id = object.id, recipe_id = recipe.id, x = x, y = y,
    circuit_id = object.circuit_id, consumed = sorted_costs(recipe.costs) }
end

return Building
