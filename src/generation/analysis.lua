-- Pure, deterministic generation diagnostics.  This is intentionally kept
-- separate from the renderer and command line so the interactive inspector
-- and batch analyzer observe the same world facts.
local Grid = require("src.world.grid")
local RoomTemplate = require("src.rooms.template")
local Component = require("src.body.component")

local Analysis = {}

local CARDINAL = {
  { 0, 1 }, -- north
  { 1, 0 }, -- east
  { 0, -1 }, -- south
  { -1, 0 }, -- west
}

local function increment(values, name, amount)
  values[name] = (values[name] or 0) + (amount or 1)
end

local function sorted_keys(values)
  local result = {}
  for value in pairs(values) do result[#result + 1] = value end
  table.sort(result)
  return result
end

local function point_data(point)
  return point and { x = point.x, y = point.y } or nil
end

local function actor_data(session, actor, provenance)
  local components, capabilities = {}, {}
  if actor.body then
    for _, component in ipairs(actor.body:list_components()) do
      components[#components + 1] = {
        id = component.id,
        definition_id = component.definition_id,
        slot_id = component.slot_id,
        integrity = component.current_integrity,
        max_integrity = component.max_integrity,
        condition = Component.condition(component),
      }
    end
    for _, ability in ipairs(session:available_actor_abilities(actor)) do
      capabilities[#capabilities + 1] = ability
    end
  end
  local locomotion = session:locomotion_state(actor)
  return {
    kind = actor.kind,
    semantic_id = actor.content_id or actor.kind,
    elite = actor.elite == true,
    x = actor.x,
    y = actor.y,
    health = actor.health,
    locomotion = locomotion and locomotion.state or nil,
    components = components,
    capabilities = capabilities,
    placed_by = provenance,
  }
end

local function connectivity(world)
  local component_by_cell, components, seen = {}, {}, {}
  local sequence = 0
  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do
      local start_key = Grid.key(x, y)
      if world:is_passable(x, y) and not seen[start_key] then
        sequence = sequence + 1
        local id = string.format("region:%03d", sequence)
        local queue, cursor = { { x = x, y = y } }, 1
        seen[start_key] = true
        local cells = {}
        while queue[cursor] do
          local point = queue[cursor]
          cursor = cursor + 1
          component_by_cell[Grid.key(point.x, point.y)] = id
          cells[#cells + 1] = { x = point.x, y = point.y }
          for _, delta in ipairs(CARDINAL) do
            local nx, ny = point.x + delta[1], point.y + delta[2]
            local neighbour_key = Grid.key(nx, ny)
            if Grid.in_bounds(nx, ny) and world:is_passable(nx, ny) and not seen[neighbour_key] then
              seen[neighbour_key] = true
              queue[#queue + 1] = { x = nx, y = ny }
            end
          end
        end
        components[#components + 1] = { id = id, size = #cells, cells = cells }
      end
    end
  end
  return component_by_cell, components
end

local function nearest_distance(origin, values)
  if not origin or #values == 0 then return nil end
  local nearest
  for _, value in ipairs(values) do
    local distance = Grid.distance(origin, value)
    if not nearest or distance < nearest then nearest = distance end
  end
  return nearest
end

function Analysis.analyze(world, metadata)
  assert(world, "Generation analysis requires a world")
  metadata = metadata or {}
  local state, session = metadata.state or {}, metadata.session
  local player = metadata.player or state.player
  local errors, warnings = {}, {}
  local world_valid, validation_error = pcall(function() return world:validate() end)
  if not world_valid then
    errors[#errors + 1] = { code = "invalid_world", message = tostring(validation_error) }
  end
  local material_counts = {}
  local passable_cells, solid_destructible, solid_indestructible = 0, 0, 0
  local flammable_cells, conductive_cells = 0, 0
  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do
      local material = world:get_material(x, y)
      increment(material_counts, material.id)
      if world:is_passable(x, y) then passable_cells = passable_cells + 1 end
      if material.blocks_movement then
        if material.destructible then solid_destructible = solid_destructible + 1 else solid_indestructible = solid_indestructible + 1 end
      end
      if material.flammable then flammable_cells = flammable_cells + 1 end
      if world:is_conductive_at(x, y) then conductive_cells = conductive_cells + 1 end
    end
  end

  local component_by_cell, components = connectivity(world)
  local player_component, player_exits = nil, 0
  if not player or not Grid.in_bounds(player.x, player.y) or not world:is_passable(player.x, player.y) then
    errors[#errors + 1] = { code = "invalid_player_spawn", message = "Player spawn is not on an initially passable cell", point = point_data(player) }
  else
    player_component = component_by_cell[Grid.key(player.x, player.y)]
    for _, delta in ipairs(CARDINAL) do
      if world:is_passable(player.x + delta[1], player.y + delta[2]) then player_exits = player_exits + 1 end
    end
    if player_exits <= 1 then
      warnings[#warnings + 1] = { code = "spawn_low_exit_count", message = "Player spawn has " .. player_exits .. " cardinal exit(s)", point = point_data(player) }
    end
  end

  local targets, enemies = {}, {}
  for index, target in ipairs(metadata.targets or state.targets or {}) do
    targets[#targets + 1] = { x = target.x, y = target.y, kind = target.kind or "target", index = index,
      placed_by = metadata.provenance and metadata.provenance.targets and metadata.provenance.targets[index] }
    if not world:is_passable(target.x, target.y) then
      errors[#errors + 1] = { code = "invalid_target_placement", message = "Required target is not on an initially passable cell", point = point_data(target), index = index }
    end
    if player_component and component_by_cell[Grid.key(target.x, target.y)] ~= player_component then
      errors[#errors + 1] = { code = "unreachable_target", message = "Required target is unreachable from player spawn", point = point_data(target), index = index }
    end
  end
  for index, enemy in ipairs(metadata.enemies or state.enemies or {}) do
    enemies[#enemies + 1] = session and actor_data(session, enemy, metadata.provenance and metadata.provenance.enemies[index])
      or { kind = enemy.kind, semantic_id = enemy.content_id or enemy.kind, x = enemy.x, y = enemy.y, health = enemy.health }
  end

  local exit = metadata.exit or state.exit
  local exit_status = "runtime_spawned_after_objectives"
  if exit then
    exit_status = component_by_cell[Grid.key(exit.x, exit.y)] == player_component and "reachable" or "unreachable"
    if exit_status == "unreachable" then
      errors[#errors + 1] = { code = "unreachable_exit", message = "Exit is unreachable from player spawn", point = point_data(exit) }
    end
  end

  local unreachable_cells, optional_regions = 0, {}
  for _, component in ipairs(components) do
    if component.id ~= player_component then
      unreachable_cells = unreachable_cells + component.size
      optional_regions[#optional_regions + 1] = { id = component.id, size = component.size, representative = component.cells[1] }
    end
  end
  if #optional_regions > 0 then
    warnings[#warnings + 1] = { code = "isolated_passable_regions", message = #optional_regions .. " initially isolated passable region(s)", regions = optional_regions }
  end

  local objects, object_counts, object_material_counts, roles = {}, {}, {}, {}
  for _, object in ipairs(world:list_objects(true)) do
    local inspected = assert(world:inspect_object(object))
    inspected.placed_by = metadata.provenance and metadata.provenance.objects and metadata.provenance.objects[object.id]
    objects[#objects + 1] = inspected
    increment(object_counts, inspected.definition_id)
    increment(object_material_counts, inspected.material_id)
    if inspected.interaction_role then increment(roles, inspected.interaction_role) end
    if not Grid.in_bounds(object.x, object.y) then
      errors[#errors + 1] = { code = "object_out_of_bounds", message = "Object is outside world bounds", object_id = object.id }
    end
  end

  -- Closed doors are optional logical gates rather than evidence that every
  -- currently unreachable passable cell is a generator defect.  We do not
  -- solve interactions here; this small classification only identifies an
  -- isolation directly separated by an initial closed door.
  local gated_regions = {}
  for _, object in ipairs(objects) do
    if object.interaction_role == "door" and object.door_state == "closed" then
      local adjacent = {}
      for _, delta in ipairs(CARDINAL) do
        local region_id = component_by_cell[Grid.key(object.x + delta[1], object.y + delta[2])]
        if region_id then adjacent[region_id] = true end
      end
      if player_component and adjacent[player_component] then
        for region_id in pairs(adjacent) do
          if region_id ~= player_component then
            gated_regions[region_id] = gated_regions[region_id] or {}
            gated_regions[region_id][#gated_regions[region_id] + 1] = object.id
          end
        end
      end
    end
  end
  for _, region in ipairs(optional_regions) do
    region.gated_by_door_ids = gated_regions[region.id] or {}
    region.classification = #region.gated_by_door_ids > 0 and "optional_gated" or "isolated"
  end

  local hazards = {}
  for _, hazard in ipairs(world:list_hazards(true)) do
    local inspected = assert(world:inspect_hazard(hazard))
    inspected.placed_by = metadata.provenance and metadata.provenance.hazards and metadata.provenance.hazards[hazard.id]
    hazards[#hazards + 1] = inspected
  end
  local liquids, liquid_volume, maximum_depth = {}, 0, 0
  for _, liquid in ipairs(world:list_liquids()) do
    local definition = world.registry:get_liquid(liquid.liquid_id)
    liquids[#liquids + 1] = { x = liquid.x, y = liquid.y, liquid_id = liquid.liquid_id, amount = liquid.amount,
      max_depth = definition.max_depth, conductive = definition.conductive, extinguishes_fire = definition.extinguishes_fire,
      placed_by = metadata.provenance and metadata.provenance.liquids and metadata.provenance.liquids[Grid.key(liquid.x, liquid.y)] }
    liquid_volume = liquid_volume + liquid.amount
    maximum_depth = math.max(maximum_depth, liquid.amount)
  end
  local gases, gas_volume, harmful_gas_cells = {}, 0, 0
  for _, gas in ipairs(world:list_gases()) do
    local definition = world.registry:get_gas(gas.gas_id)
    local harmful = gas.concentration >= definition.exposure_threshold
    gases[#gases + 1] = { x = gas.x, y = gas.y, gas_id = gas.gas_id, concentration = gas.concentration,
      max_concentration = definition.max_concentration, exposure_threshold = definition.exposure_threshold, damage = definition.damage,
      harmful = harmful, placed_by = metadata.provenance and metadata.provenance.gases and metadata.provenance.gases[Grid.key(gas.x, gas.y)] }
    gas_volume = gas_volume + gas.concentration
    if harmful then harmful_gas_cells = harmful_gas_cells + 1 end
  end
  local circuits = {}
  for _, circuit in ipairs(world:list_circuits()) do circuits[#circuits + 1] = assert(world:inspect_circuit(circuit.id)) end
  local fires = {}
  for _, fire in ipairs(world:list_fires(true)) do fires[#fires + 1] = assert(world:inspect_fire(fire)) end

  local enemy_counts, enemy_capabilities, elite_count = {}, {}, 0
  for _, enemy in ipairs(enemies) do
    increment(enemy_counts, enemy.semantic_id)
    if enemy.elite then elite_count = elite_count + 1 end
    for _, ability_id in ipairs(enemy.capabilities or {}) do increment(enemy_capabilities, ability_id) end
  end
  local room_metadata = metadata.provenance and metadata.provenance.rooms or nil
  local rooms, template_usage, rotation_counts, connector_patterns, degree_counts = {}, {}, {}, {}, {}
  for _, room in ipairs(room_metadata and room_metadata.rooms or {}) do
    rooms[#rooms + 1] = room
    increment(template_usage, room.template_id)
    increment(rotation_counts, tostring(room.rotation))
    increment(connector_patterns, RoomTemplate.pattern_key(room.required_connectors))
    increment(degree_counts, tostring(#(room.required_connectors or {})))
  end
  local valid = #errors == 0
  return {
    format = "roag.generation_report",
    version = 1,
    valid = valid,
    seed = metadata.seed,
    stage = metadata.stage,
    biome_id = metadata.biome_id or (state.settings and state.settings.biome_id),
    tier_id = metadata.tier_id or (state.settings and state.settings.tier_id),
    terrain = metadata.terrain or world.terrain,
    generation_streams = metadata.provenance and metadata.provenance.streams or {},
    room_provenance = room_metadata,
    errors = errors,
    warnings = warnings,
    player_spawn = player and { x = player.x, y = player.y, nearby_passable_cells = player_exits, component_id = player_component } or nil,
    exit = point_data(exit),
    exit_status = exit_status,
    objectives = targets,
    connectivity = { components = components, cell_components = component_by_cell, player_component = player_component,
      unreachable_passable_cells = unreachable_cells, optional_regions = optional_regions },
    metrics = {
      terrain_cells = Grid.width * Grid.height,
      passable_cells = passable_cells,
      connected_region_count = #components,
      material_counts = material_counts,
      solid_destructible_cells = solid_destructible,
      solid_indestructible_cells = solid_indestructible,
      flammable_cells = flammable_cells,
      conductive_cells = conductive_cells,
      enemies = #enemies,
      enemy_types = enemy_counts,
      elite_enemies = elite_count,
      enemy_capabilities = enemy_capabilities,
      nearest_enemy_distance = nearest_distance(player, enemies),
      objects = #objects,
      object_types = object_counts,
      object_materials = object_material_counts,
      object_roles = roles,
      hazards = #hazards,
      harmful_hazard_cells = #hazards,
      nearest_hazard_distance = nearest_distance(player, hazards),
      liquid_cells = #liquids,
      liquid_volume = liquid_volume,
      maximum_liquid_depth = maximum_depth,
      gas_cells = #gases,
      gas_volume = gas_volume,
      harmful_gas_cells = harmful_gas_cells,
      circuits = #circuits,
      powered_circuits = (function()
        local count = 0
        for _, circuit in ipairs(circuits) do if circuit.powered then count = count + 1 end end
        return count
      end)(),
      room_count = #rooms,
      template_usage = template_usage,
      rotation_counts = rotation_counts,
      connector_pattern_counts = connector_patterns,
      graph_degree_counts = degree_counts,
    },
    objects = objects,
    enemies = enemies,
    hazards = hazards,
    liquids = liquids,
    gases = gases,
    circuits = circuits,
    fires = fires,
    rooms = rooms,
  }
end

-- A renderer-neutral overlay model.  It intentionally retains semantic and
-- physical identifiers so UI tests can prove layer data without pixel tests.
function Analysis.overlay_model(world, report)
  local terrain, conductivity = {}, {}
  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do
      local cell = assert(world:inspect_cell(x, y))
      terrain[#terrain + 1] = {
        x = x, y = y, material_id = cell.material_id, passable = cell.passable,
        component_id = report.connectivity.cell_components[Grid.key(x, y)],
      }
      if cell.conductivity.conductive then
        conductivity[#conductivity + 1] = { x = x, y = y, contributors = cell.conductivity }
      end
    end
  end
  return {
    terrain = terrain,
    connectivity = report.connectivity,
    objects = report.objects,
    actors = report.enemies,
    hazards = report.hazards,
    liquids = report.liquids,
    gases = report.gases,
    power = report.circuits,
    objectives = { player_spawn = report.player_spawn, exit = report.exit, targets = report.objectives },
    conductivity = conductivity,
    rooms = report.rooms,
    room_provenance = report.room_provenance,
  }
end

function Analysis.sorted_metric_keys(values)
  return sorted_keys(values)
end

return Analysis
