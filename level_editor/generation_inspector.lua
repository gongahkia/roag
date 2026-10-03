-- Developer-only LÖVE presentation for isolated initial-floor diagnostics.
-- The controller owns no gameplay/save state: its only mutable simulation
-- reference is an inspection world freshly constructed for the displayed
-- seed and stage.
local Grid = require("src.world.grid")
local InspectionFloor = require("src.generation.inspection_floor")
local Analysis = require("src.generation.analysis")

local Inspector = {}
Inspector.__index = Inspector

local LAYER_KEYS = {
  ["1"] = "terrain", ["2"] = "connectivity", ["3"] = "actors", ["4"] = "objects",
  ["5"] = "hazards", ["6"] = "liquids", ["7"] = "gas", ["8"] = "power",
  ["9"] = "objectives", ["0"] = "fires", c = "conductivity", m = "metadata", t = "rooms", v = "discoveries", z = "ecology", l = "landmarks", p = "connections",
}

local REGION_COLORS = {
  { 0.2, 0.55, 0.95, 0.32 }, { 0.9, 0.42, 0.24, 0.32 }, { 0.35, 0.8, 0.42, 0.32 },
  { 0.76, 0.36, 0.9, 0.32 }, { 0.95, 0.75, 0.2, 0.32 }, { 0.25, 0.85, 0.8, 0.32 },
}

local function clamp(value, minimum, maximum)
  return math.max(minimum, math.min(maximum, value))
end

local function key(x, y)
  return Grid.key(x, y)
end

local function point_equal(point, x, y)
  return point and point.x == x and point.y == y
end

local function region_color(component_id)
  if not component_id then return { 0, 0, 0, 0 } end
  local index = tonumber(component_id:match("(%d+)$")) or 1
  return REGION_COLORS[(index - 1) % #REGION_COLORS + 1]
end

function Inspector.new(options)
  options = options or {}
  local stages = InspectionFloor.stages()
  local stage_index = InspectionFloor.resolve_stage(options.stage or 1) or 1
  local stage = stages[stage_index]
  local campaign_mode = options.campaign_seed ~= nil
  local self = setmetatable({
    stages = stages,
    tiers = InspectionFloor.tiers(),
    stage = stage_index,
    biome_id = options.biome or stage.biome_id,
    tier_id = options.tier or stage.tier_id,
    seed = tonumber(options.seed) or 1,
    seed_text = tostring(math.floor(tonumber(options.seed) or 1)),
    campaign_mode = campaign_mode,
    campaign_seed = tonumber(options.campaign_seed),
    world_x = tonumber(options.world_x) or 0,
    world_y = tonumber(options.world_y) or 0,
    world_z = tonumber(options.z) or 0,
    layers = {
      terrain = true, connectivity = false, actors = true, objects = true, hazards = true,
      liquids = true, gas = true, fires = true, power = true, objectives = true, conductivity = false, metadata = false, rooms = false, discoveries = true, ecology = true, landmarks = false,
      connections = true,
    },
    zoom = 10,
    pan_x = 0,
    pan_y = 0,
    help = true,
    hover = nil,
    selected = nil,
    error = nil,
    dragging = false,
    viewport = { x = 10, y = 54, width = 600, height = 400 },
  }, Inspector)
  self:regenerate()
  return self
end

function Inspector:regenerate()
  local floor, failure
  if self.campaign_mode then
    floor, failure = InspectionFloor.generate_campaign_zone({
      campaign_seed = self.campaign_seed, world_x = self.world_x, world_y = self.world_y, z = self.world_z,
    })
  else
    floor, failure = InspectionFloor.generate({
      biome = self.biome_id, tier = self.tier_id, seed = self.seed,
      discovery_state = { enabled = true, assigned_discovery_ids = {} },
      reinforcement_state = { enabled = true },
    })
  end
  if not floor then
    self.error, self.floor, self.report, self.overlays = failure.reason or failure.code, nil, nil, nil
    return nil, failure
  end
  self.floor = floor
  self.report = Analysis.analyze(floor.world, {
    seed = floor.seed, stage = floor.stage, biome_id = floor.biome_id, tier_id = floor.tier_id, terrain = floor.terrain, state = floor.state,
    session = floor.session, provenance = floor.provenance,
  })
  self.overlays = Analysis.overlay_model(floor.world, self.report)
  -- `surface_connections` remains an alias for existing inspector callers;
  -- campaign overlays now include up/down endpoints as well.
  self.overlays.zone_connections = floor.connections or {}
  self.overlays.surface_connections = self.overlays.zone_connections
  self.seed = floor.seed
  self.seed_text = tostring(floor.seed)
  self.selected, self.hover, self.error = nil, nil, nil
  return floor
end

function Inspector:set_seed_text(value)
  self.seed_text = value
end

function Inspector:commit_seed()
  local seed = tonumber(self.seed_text)
  if not seed or seed % 1 ~= 0 then
    self.error = "Seed must be an integer."
    return nil, { code = "invalid_seed" }
  end
  self.seed = seed
  return self:regenerate()
end

function Inspector:next_seed()
  self.seed = self.seed + 1
  return self:regenerate()
end

function Inspector:select_stage(delta)
  self.stage = ((self.stage - 1 + delta) % #self.stages) + 1
  local stage = self.stages[self.stage]
  self.biome_id, self.tier_id = stage.biome_id, stage.tier_id
  return self:regenerate()
end

function Inspector:select_tier(delta)
  local index = 1
  for candidate, tier in ipairs(self.tiers) do if tier.id == self.tier_id then index = candidate break end end
  index = ((index - 1 + delta) % #self.tiers) + 1
  self.tier_id = self.tiers[index].id
  return self:regenerate()
end

function Inspector:toggle_layer(layer)
  if self.layers[layer] ~= nil then self.layers[layer] = not self.layers[layer] end
end

function Inspector:fit(width, height)
  local panel_width = math.min(390, math.max(300, math.floor(width * 0.31)))
  self.viewport = { x = 12, y = 54, width = math.max(120, width - panel_width - 28), height = math.max(120, height - 66) }
  self.zoom = math.max(2, math.floor(math.min(self.viewport.width / Grid.width, self.viewport.height / Grid.height)))
  self.pan_x, self.pan_y = 0, 0
end

function Inspector:_screen_point(x, y)
  return self.viewport.x + (x - self.pan_x) * self.zoom,
    self.viewport.y + (Grid.height - 1 - y - self.pan_y) * self.zoom
end

function Inspector:screen_to_world(screen_x, screen_y)
  local view = self.viewport
  if screen_x < view.x or screen_y < view.y or screen_x >= view.x + view.width or screen_y >= view.y + view.height then
    return nil
  end
  local x = math.floor((screen_x - view.x) / self.zoom + self.pan_x)
  local y = Grid.height - 1 - math.floor((screen_y - view.y) / self.zoom) - self.pan_y
  if not Grid.in_bounds(x, y) then return nil end
  return { x = x, y = y }
end

function Inspector:inspect_at(x, y)
  if not self.floor or not Grid.in_bounds(x, y) then return nil end
  local world, state = self.floor.world, self.floor.state
  local cell = assert(world:inspect_cell(x, y))
  local actors = {}
  local function add_actor(actor, category, index)
    if actor and actor.x == x and actor.y == y then
      local data = { category = category, kind = actor.kind, semantic_id = actor.content_id or actor.kind, health = actor.health, index = index,
        faction_id = self.floor.session:actor_faction_id(actor),
        faction_display_name = self.floor.session.registry:get_faction(self.floor.session:actor_faction_id(actor)).display_name }
      if actor.body then
        data.components = {}
        for _, component in ipairs(actor.body:list_components()) do
          data.components[#data.components + 1] = component.id .. " (" .. component.definition_id .. ", " .. tostring(component.condition) .. ")"
        end
        data.capabilities = self.floor.session:available_actor_abilities(actor)
        data.locomotion = self.floor.session:locomotion_state(actor).state
      end
      actors[#actors + 1] = data
    end
  end
  add_actor(state.player, "player")
  for index, actor in ipairs(state.enemies) do add_actor(actor, "enemy", index) end
  for index, actor in ipairs(state.targets) do add_actor(actor, "target", index) end
  return {
    cell = cell,
    actors = actors,
    connectivity_component = self.report.connectivity.cell_components[key(x, y)],
    provenance = {
      liquid = self.floor.provenance.liquids[key(x, y)],
      gas = self.floor.provenance.gases[key(x, y)],
      objects = (function()
        local values = {}
        for _, object in ipairs(cell.objects) do values[#values + 1] = self.floor.provenance.objects[object.id] end
        return values
      end)(),
      hazards = (function()
        local values = {}
        for _, hazard in ipairs(cell.hazards) do values[#values + 1] = self.floor.provenance.hazards[hazard.id] end
        return values
      end)(),
      room = self.floor.provenance.rooms and self.floor.provenance.rooms.cell_provenance
        and self.floor.provenance.rooms.cell_provenance[key(x, y)] or nil,
    },
  }
end

function Inspector:_draw_rooms()
  local metadata = self.floor.provenance.rooms
  if not self.layers.rooms or not metadata or not metadata.rooms then return end
  local width = metadata.room_config and metadata.room_config.width or 10
  local height = metadata.room_config and metadata.room_config.height or width
  for _, room in ipairs(metadata.rooms) do
    local sx, sy = self:_screen_point(room.origin.x, room.origin.y + height - 1)
    self:_set_color({ 0.96, 0.6, 0.18, 0.9 })
    love.graphics.rectangle("line", sx, sy, width * self.zoom, height * self.zoom)
    for _, connector in ipairs(room.connectors or {}) do
      local cx, cy = self:_screen_point(connector.x, connector.y)
      self:_set_color({ 1, 0.94, 0.25, 1 })
      love.graphics.rectangle("fill", cx + self.zoom * 0.25, cy + self.zoom * 0.25, self.zoom * 0.5, self.zoom * 0.5)
    end
    if self.zoom >= 10 then self:_draw_text(room.template_id .. " @" .. room.rotation, sx + 2, sy + 2, { 1, 0.78, 0.3 }, width * self.zoom - 4) end
  end
end

function Inspector:_draw_landmarks()
  if not self.layers.landmarks then return end
  for _, landmark in ipairs(self.report.landmarks or {}) do
    local x, y = landmark.x, landmark.y
    if x and y then
      local sx, sy = self:_screen_point(x, y)
      self:_set_color({ 0.98, 0.78, 0.22, 0.92 })
      love.graphics.circle("line", sx + self.zoom * 0.5, sy + self.zoom * 0.5, math.max(3, self.zoom * 0.7))
      if self.zoom >= 9 then
        self:_draw_text((landmark.kind or landmark.id):gsub("^landmark%.[^.]+%.", ""), sx + self.zoom + 2, sy, { 1, 0.82, 0.32 }, 112)
      end
    end
  end
end

function Inspector:_set_color(color)
  love.graphics.setColor(color[1], color[2], color[3], color[4] or 1)
end

function Inspector:_draw_text(value, x, y, color, limit)
  self:_set_color(color or { 0.92, 0.94, 0.98 })
  if limit then love.graphics.printf(value, x, y, limit) else love.graphics.print(value, x, y) end
end

function Inspector:_draw_base()
  local world = self.floor.world
  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do
      local sx, sy = self:_screen_point(x, y)
      if sx + self.zoom >= self.viewport.x and sx <= self.viewport.x + self.viewport.width
        and sy + self.zoom >= self.viewport.y and sy <= self.viewport.y + self.viewport.height then
        local cell = world:inspect_cell(x, y)
        if self.layers.terrain then
          local color
          if cell.passable then
            color = cell.material_id == "material.terrain.leaf_litter" and { 0.12, 0.25, 0.16 }
              or { 0.11, 0.15, 0.2 }
          else
            color = cell.material_id == "material.terrain.brush" and { 0.09, 0.19, 0.1 }
              or { 0.17, 0.11, 0.18 }
          end
          self:_set_color(color)
          love.graphics.rectangle("fill", sx, sy, self.zoom, self.zoom)
          self:_set_color({ 0.18, 0.21, 0.27, 0.55 })
          love.graphics.rectangle("line", sx, sy, self.zoom, self.zoom)
        end
        if self.layers.connectivity and cell.passable then
          self:_set_color(region_color(self.report.connectivity.cell_components[key(x, y)]))
          love.graphics.rectangle("fill", sx + 1, sy + 1, math.max(1, self.zoom - 2), math.max(1, self.zoom - 2))
        end
        if self.layers.conductivity and cell.conductivity.conductive then
          self:_set_color({ 0.25, 0.9, 1, 0.85 })
          love.graphics.rectangle("line", sx + 2, sy + 2, math.max(1, self.zoom - 4), math.max(1, self.zoom - 4))
        end
      end
    end
  end
end

function Inspector:_draw_media()
  local world = self.floor.world
  if self.layers.liquids then
    for _, liquid in ipairs(self.report.liquids) do
      local sx, sy = self:_screen_point(liquid.x, liquid.y)
      local ratio = liquid.amount / liquid.max_depth
      self:_set_color({ 0.08, 0.38 + ratio * 0.15, 0.92, 0.32 + ratio * 0.4 })
      love.graphics.rectangle("fill", sx + self.zoom * 0.08, sy + self.zoom * 0.44, self.zoom * 0.84, self.zoom * 0.48)
    end
  end
  if self.layers.gas then
    for _, gas in ipairs(self.report.gases) do
      local sx, sy = self:_screen_point(gas.x, gas.y)
      local ratio = gas.concentration / gas.max_concentration
      self:_set_color({ 0.35, 0.95, 0.28, 0.12 + ratio * 0.38 })
      love.graphics.circle("fill", sx + self.zoom * 0.5, sy + self.zoom * 0.5, math.max(1, self.zoom * (0.16 + ratio * 0.28)))
    end
  end
  if self.layers.fires then
    for _, fire in ipairs(self.report.fires) do
      if fire.active then
        local sx, sy = self:_screen_point(fire.x, fire.y)
        self:_set_color({ 1, 0.34, 0.08, 0.9 })
        love.graphics.polygon("fill", sx + self.zoom * 0.22, sy + self.zoom * 0.8,
          sx + self.zoom * 0.5, sy + self.zoom * 0.13, sx + self.zoom * 0.78, sy + self.zoom * 0.8)
      end
    end
  end
  if self.layers.hazards then
    for _, hazard in ipairs(self.report.hazards) do
      if hazard.active then
        local sx, sy = self:_screen_point(hazard.x, hazard.y)
        self:_set_color({ 1, 0.25, 0.2, 0.95 })
        love.graphics.line(sx + self.zoom * 0.18, sy + self.zoom * 0.82, sx + self.zoom * 0.5, sy + self.zoom * 0.18)
        love.graphics.line(sx + self.zoom * 0.5, sy + self.zoom * 0.82, sx + self.zoom * 0.76, sy + self.zoom * 0.22)
      end
    end
  end
  if self.layers.objects then
    for _, object in ipairs(self.report.objects) do
      local sx, sy = self:_screen_point(object.x, object.y)
      local color = object.destroyed and { 0.35, 0.35, 0.35, 0.4 }
        or object.interaction_role == "discovery" and { 0.22, 0.92, 0.78, 0.98 }
        or object.interaction_role == "clue" and { 1, 0.72, 0.18, 0.98 }
        or object.interaction_role == "door" and { 0.3, 0.58, 0.9, 0.95 }
        or object.interaction_role == "reinforcement" and (object.reinforcement_state == "armed" and { 1, 0.28, 0.14, 0.98 } or { 0.94, 0.46, 0.18, 0.98 })
        or object.conductive and { 0.25, 0.85, 0.95, 0.95 }
        or { 0.68, 0.43, 0.2, 0.95 }
      self:_set_color(color)
      love.graphics.rectangle("fill", sx + self.zoom * 0.18, sy + self.zoom * 0.18, self.zoom * 0.64, self.zoom * 0.64)
      if self.layers.ecology and object.interaction_role == "reinforcement" then
        self:_set_color({ 1, 0.9, 0.35, 1 })
        love.graphics.circle("line", sx + self.zoom * 0.5, sy + self.zoom * 0.5, math.max(2, self.zoom * 0.38))
      end
      if self.layers.power and object.circuit_id then
        self:_set_color(object.circuit_powered and { 0.2, 1, 0.4, 1 } or { 1, 0.42, 0.18, 1 })
        love.graphics.circle("fill", sx + self.zoom * 0.75, sy + self.zoom * 0.25, math.max(1, self.zoom * 0.09))
      end
    end
  end
  if self.layers.discoveries then
    for _, discovery in ipairs(self.report.discoveries or {}) do
      local point = discovery.cache
      if point then
        local sx, sy = self:_screen_point(point.x, point.y)
        self:_set_color(discovery.first_time and { 0.2, 1, 0.72, 0.95 } or { 0.62, 0.75, 0.9, 0.9 })
        love.graphics.rectangle("line", sx + self.zoom * 0.08, sy + self.zoom * 0.08, self.zoom * 0.84, self.zoom * 0.84)
      end
    end
  end
end

function Inspector:_draw_surface_connections()
  if not self.layers.connections then return end
  for direction, connection in pairs(self.overlays.zone_connections or self.overlays.surface_connections or {}) do
    local sx, sy = self:_screen_point(connection.boundary.x, connection.boundary.y)
    local vertical = direction == "up" or direction == "down"
    self:_set_color(vertical and { 0.78, 0.54, 1, 1 } or { 0.22, 0.95, 0.72, 1 })
    love.graphics.setLineWidth(math.max(1, self.zoom * 0.12))
    love.graphics.rectangle("line", sx + self.zoom * 0.08, sy + self.zoom * 0.08, self.zoom * 0.84, self.zoom * 0.84)
    love.graphics.setLineWidth(1)
    if self.layers.metadata then
      local destination = connection.destination
      local role = connection.connection_type and (" " .. connection.connection_type) or ""
      self:_draw_text(string.format("%s%s → %s [%s]", direction:upper(), role,
        destination.world_x .. ":" .. destination.world_y .. ":" .. destination.z, connection.id), sx + 2, sy + 2,
        vertical and { 0.9, 0.76, 1 } or { 0.72, 1, 0.88 })
    end
  end
end

function Inspector:_draw_actors_and_objectives()
  local state = self.floor.state
  if self.layers.objectives then
    local sx, sy = self:_screen_point(state.player.x, state.player.y)
    self:_set_color({ 1, 0.92, 0.25, 1 })
    love.graphics.circle("fill", sx + self.zoom * 0.5, sy + self.zoom * 0.5, math.max(2, self.zoom * 0.27))
    for _, target in ipairs(state.targets) do
      sx, sy = self:_screen_point(target.x, target.y)
      self:_set_color({ 1, 0.72, 0.15, 1 })
      love.graphics.rectangle("line", sx + self.zoom * 0.2, sy + self.zoom * 0.2, self.zoom * 0.6, self.zoom * 0.6)
    end
    if state.exit then
      sx, sy = self:_screen_point(state.exit.x, state.exit.y)
      self:_set_color({ 0.65, 0.92, 1, 1 })
      love.graphics.rectangle("line", sx + self.zoom * 0.1, sy + self.zoom * 0.1, self.zoom * 0.8, self.zoom * 0.8)
    end
  end
  if self.layers.actors then
    for _, enemy in ipairs(self.report.enemies) do
      local sx, sy = self:_screen_point(enemy.x, enemy.y)
      local faction = enemy.faction_id
      self:_set_color(faction == "faction.machine" and { 0.4, 0.8, 0.95, 1 }
        or faction == "faction.cult" and { 0.8, 0.42, 0.96, 1 }
        or { 0.96, 0.4, 0.56, 1 })
      love.graphics.circle("fill", sx + self.zoom * 0.5, sy + self.zoom * 0.5, math.max(2, self.zoom * 0.22))
    end
  end
  if self.layers.metadata then
    for _, object in ipairs(self.report.objects) do
      local sx, sy = self:_screen_point(object.x, object.y)
      self:_set_color({ 1, 1, 1, 0.85 })
      love.graphics.points(sx + self.zoom * 0.5, sy + self.zoom * 0.5)
    end
  end
end

local function append(lines, value)
  if value ~= nil then lines[#lines + 1] = tostring(value) end
end

function Inspector:_detail_lines(point)
  local lines = {}
  if not point then
    append(lines, "Hover or click a cell.")
    return lines
  end
  local detail = self:inspect_at(point.x, point.y)
  local cell = detail.cell
  append(lines, string.format("CELL %d, %d", point.x, point.y))
  append(lines, cell.material_id)
  append(lines, string.format("passable=%s  vision=%s", tostring(cell.passable), tostring(not cell.blocks_vision)))
  append(lines, string.format("destructible=%s  integrity=%s/%s", tostring(cell.destructible), tostring(cell.current_integrity or "-"), tostring(cell.max_integrity or "-")))
  append(lines, "region: " .. tostring(detail.connectivity_component or "none"))
  if cell.conductivity.conductive then
    append(lines, "conductive: terrain=" .. tostring(cell.conductivity.terrain) .. " liquid=" .. tostring(cell.conductivity.liquid) .. " object=" .. tostring(cell.conductivity.object))
  end
  if cell.liquid then append(lines, string.format("liquid: %s depth %d/%d conductive=%s", cell.liquid.liquid_id, cell.liquid.amount, cell.liquid.max_depth, tostring(cell.liquid.conductive))) end
  if cell.gas then append(lines, string.format("gas: %s concentration %d/%d harmful=%s", cell.gas.gas_id, cell.gas.concentration, cell.gas.max_concentration, tostring(cell.gas.harmful))) end
  for _, hazard in ipairs(cell.hazards) do append(lines, "hazard: " .. hazard.id .. " " .. hazard.definition_id .. " " .. hazard.trigger) end
  for _, object in ipairs(cell.objects) do
    append(lines, string.format("object: %s", object.id))
    append(lines, string.format("  %s material=%s integrity=%s/%s", object.definition_id, object.material_id, tostring(object.current_integrity), tostring(object.max_integrity)))
    append(lines, string.format("  movable=%s conductive=%s gas_block=%s", tostring(object.movable_by_force), tostring(object.conductive), tostring(object.blocks_gas)))
    if object.interaction_role then append(lines, "  role=" .. object.interaction_role .. " circuit=" .. tostring(object.circuit_id) .. " powered=" .. tostring(object.circuit_powered)) end
    if object.door_state then append(lines, "  door=" .. object.door_state) end
    if object.discovery_id then
      append(lines, "  discovery=" .. object.discovery_id .. " profile=" .. tostring(object.discovery_access_profile_id))
      append(lines, "  claimed=" .. tostring(object.discovery_claimed) .. " provenance=" .. tostring(object.discovery_provenance))
    end
    if object.reinforcement_profile_id then
      append(lines, "  reinforcement=" .. object.reinforcement_profile_id .. " faction=" .. tostring(object.reinforcement_faction_id))
      append(lines, "  state=" .. tostring(object.reinforcement_state) .. " charges=" .. tostring(object.reinforcement_charges)
        .. " delay=" .. tostring(object.reinforcement_delay) .. " provenance=" .. tostring(object.reinforcement_provenance))
    end
  end
  for _, actor in ipairs(detail.actors) do
    append(lines, "actor: " .. actor.category .. " " .. actor.semantic_id .. " faction=" .. tostring(actor.faction_display_name) .. " hp=" .. tostring(actor.health) .. " locomotion=" .. tostring(actor.locomotion))
    for _, component in ipairs(actor.components or {}) do append(lines, "  " .. component) end
    if actor.capabilities and #actor.capabilities > 0 then append(lines, "  abilities: " .. table.concat(actor.capabilities, ", ")) end
  end
  for _, fire in ipairs(cell.fires) do append(lines, "fire: " .. fire.id .. " target=" .. fire.target_kind .. " age=" .. fire.age) end
  if detail.provenance.room then
    local room = detail.provenance.room
    append(lines, "room: " .. room.template_id .. " @" .. tostring(room.rotation))
    append(lines, string.format("  slot=(%d,%d) local=(%d,%d)", room.slot.x, room.slot.y, room.local_x, room.local_y))
  end
  if detail.provenance.liquid then append(lines, "placed by: " .. detail.provenance.liquid) end
  if detail.provenance.gas then append(lines, "placed by: " .. detail.provenance.gas) end
  for _, source in ipairs(detail.provenance.objects) do if source then append(lines, "placed by: " .. source) end end
  for _, source in ipairs(detail.provenance.hazards) do if source then append(lines, "placed by: " .. source) end end
  return lines
end

function Inspector:draw()
  local width, height = love.graphics.getDimensions()
  if self.viewport.width == 600 then self:fit(width, height) end
  love.graphics.clear(0.025, 0.035, 0.055)
  if not self.floor then
    self:_draw_text("Generation Inspector error: " .. tostring(self.error), 20, 20, { 1, 0.35, 0.3 })
    return
  end
  self:_draw_base()
  self:_draw_surface_connections()
  self:_draw_media()
  self:_draw_rooms()
  self:_draw_landmarks()
  self:_draw_actors_and_objectives()
  if self.selected then
    local sx, sy = self:_screen_point(self.selected.x, self.selected.y)
    self:_set_color({ 1, 1, 1, 0.95 })
    love.graphics.rectangle("line", sx - 1, sy - 1, self.zoom + 2, self.zoom + 2)
  elseif self.hover then
    local sx, sy = self:_screen_point(self.hover.x, self.hover.y)
    self:_set_color({ 1, 1, 1, 0.5 })
    love.graphics.rectangle("line", sx, sy, self.zoom, self.zoom)
  end

  local panel_x = self.viewport.x + self.viewport.width + 12
  self:_set_color({ 0.06, 0.075, 0.11, 0.96 })
  love.graphics.rectangle("fill", panel_x - 8, 8, width - panel_x, height - 16)
  local biome = InspectionFloor.resolve_biome(self.biome_id)
  local tier = InspectionFloor.resolve_tier(self.tier_id)
  self:_draw_text("GENERATION INSPECTOR", panel_x, 18, { 0.45, 0.9, 1 })
  if self.campaign_mode then
    local key = self.floor.zone_key
    self:_draw_text(string.format("campaign: %d  zone: %d,%d,%d", self.floor.campaign_seed, key.world_x, key.world_y, key.z), panel_x, 38)
    self:_draw_text("profile: " .. tostring(self.floor.profile_id), panel_x, 56)
    self:_draw_text("location: " .. tostring(self.floor.location_name or "WILDERNESS"), panel_x, 72, { 0.78, 0.78, 0.6 })
  else
    self:_draw_text("biome: " .. biome.terrain .. " [ / ]", panel_x, 38)
    self:_draw_text("tier: " .. tier.number .. "  , / .", panel_x, 56)
  end
  local y_offset = self.campaign_mode and 18 or 0
  self:_draw_text("seed: " .. self.seed_text .. "  [enter]", panel_x, 74 + y_offset, { 1, 0.9, 0.4 })
  self:_draw_text("zoom: " .. tostring(self.zoom) .. "  regions: " .. self.report.metrics.connected_region_count, panel_x, 92 + y_offset)
  self:_draw_text("exit: " .. self.report.exit_status, panel_x, 110 + y_offset)
  local active_layers = {}
  for _, layer in ipairs({ "terrain", "connectivity", "actors", "objects", "hazards", "liquids", "gas", "fires", "power", "objectives", "conductivity", "metadata", "rooms", "discoveries", "ecology", "landmarks" }) do
    active_layers[#active_layers + 1] = (self.layers[layer] and "+" or "-") .. layer
  end
  self:_draw_text(table.concat(active_layers, " "), panel_x, 126 + y_offset, { 0.62, 0.75, 0.86 }, width - panel_x - 10)
  local lines = self:_detail_lines(self.selected or self.hover)
  local y = 152 + y_offset
  for _, line in ipairs(lines) do
    self:_draw_text(line, panel_x, y, { 0.88, 0.9, 0.96 }, width - panel_x - 10)
    y = y + 16
    if y > height - 130 then break end
  end
  if #self.report.errors > 0 or #self.report.warnings > 0 then
    y = math.max(y + 6, height - 116)
    self:_draw_text("WARNINGS", panel_x, y, { 1, 0.68, 0.24 })
    y = y + 16
    for _, warning in ipairs(self.report.errors) do self:_draw_text("ERROR " .. warning.code, panel_x, y, { 1, 0.3, 0.25 }); y = y + 15 end
    for _, warning in ipairs(self.report.warnings) do self:_draw_text(warning.code, panel_x, y, { 1, 0.72, 0.3 }); y = y + 15 end
  end
  if self.help then
    self:_draw_text("1 terrain  2 connectivity  3 actors  4 objects  5 hazards", self.viewport.x, height - 34, { 0.75, 0.82, 0.9 })
    self:_draw_text("6 liquid  7 gas  8 power  9 objectives  0 fire  C conductivity  M provenance  T rooms  V discoveries  Z ecology  L landmarks | wheel zoom | middle drag/WASD pan | F fit", self.viewport.x, height - 18, { 0.75, 0.82, 0.9 })
  end
  if self.error then self:_draw_text(self.error, self.viewport.x, 18, { 1, 0.35, 0.3 }) end
end

function Inspector:update() end

function Inspector:resize(width, height)
  self:fit(width, height)
end

function Inspector:keypressed(key)
  if key == "escape" then love.event.quit(); return end
  if LAYER_KEYS[key] then self:toggle_layer(LAYER_KEYS[key]); return end
  if key == "h" then self.help = not self.help; return end
  if key == "r" then self:regenerate(); return end
  if key == "n" then self:next_seed(); return end
  if key == "[" then self:select_stage(-1); return end
  if key == "]" then self:select_stage(1); return end
  if key == "," then self:select_tier(-1); return end
  if key == "." then self:select_tier(1); return end
  if key == "f" then local width, height = love.graphics.getDimensions(); self:fit(width, height); return end
  if key == "return" or key == "kpenter" then self:commit_seed(); return end
  if key == "backspace" then self.seed_text = self.seed_text:sub(1, -2); return end
  if key == "delete" then self.seed_text = ""; return end
  if key:match("^%d$") or (key == "-" and self.seed_text == "") then self.seed_text = self.seed_text .. key; return end
  local delta = ({ left = { -3, 0 }, a = { -3, 0 }, right = { 3, 0 }, d = { 3, 0 }, up = { 0, 3 }, w = { 0, 3 }, down = { 0, -3 }, s = { 0, -3 } })[key]
  if delta then self.pan_x, self.pan_y = self.pan_x + delta[1], self.pan_y + delta[2] end
end

function Inspector:wheelmoved(_, y)
  self.zoom = clamp(self.zoom + y, 2, 48)
end

function Inspector:mousemoved(x, y, dx, dy)
  if self.dragging then
    self.pan_x = self.pan_x - dx / self.zoom
    self.pan_y = self.pan_y + dy / self.zoom
  else
    self.hover = self:screen_to_world(x, y)
  end
end

function Inspector:mousepressed(x, y, button)
  if button == 1 then self.selected = self:screen_to_world(x, y) end
  if button == 3 then self.dragging = true end
end

function Inspector:mousereleased(_, _, button)
  if button == 3 then self.dragging = false end
end

return Inspector
