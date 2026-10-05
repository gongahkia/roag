local Grid = require("src.world.grid")
local Component = require("src.body.component")
local GameplayUI = require("src.presentation.gameplay_ui")
local Loadout = require("src.simulation.loadout")
local InventoryLayout = require("src.ui.inventory_layout")
local SalvageLayout = require("src.ui.salvage_layout")
local Tuning = require("src.rendering.tuning")

local Renderer = {}
Renderer.__index = Renderer

local VIEW_WIDTH, VIEW_HEIGHT = 39, 25
local SCREEN_ACCENTS = {
  cyan = { 0.7, 0.9, 1 }, amber = { 0.95, 0.85, 0.3 }, mint = { 0.58, 0.9, 0.76 },
  coral = { 1, 0.48, 0.32 }, violet = { 0.84, 0.58, 0.95 },
}
local FACING_MARKER_OFFSETS = {
  w = { 0.5, 0.10 }, a = { 0.10, 0.5 }, s = { 0.5, 0.90 }, d = { 0.90, 0.5 },
}
local function clamp(value, minimum, maximum)
  return math.max(minimum, math.min(maximum, value))
end

function Renderer.new(assets)
  return setmetatable({ assets = assets }, Renderer)
end

function Renderer:outline_role(value, state)
  if value == (state and state.player) then return "player" end
  if value == (state and state.boss) then return "hostile" end
  for _, enemy in ipairs(state and state.enemies or {}) do if value == enemy then return "hostile" end end
  if value and value.kind == "bullet" then return "projectile" end
  return nil
end

function Renderer:_color(color)
  love.graphics.setColor(color[1], color[2], color[3], color[4] or 1)
end

function Renderer:_screen_definition(app, id)
  local screen = app and app.screens and app.screens:screen(id) or nil
  local accent = screen and SCREEN_ACCENTS[screen.accent] or SCREEN_ACCENTS.cyan
  return screen, accent
end

function Renderer:_screen_text(app, id, field, fallback)
  local screen = app and app.screens and app.screens:screen(id) or nil
  return screen and screen[field] or fallback
end

function Renderer:_text(value, x, y, scale, color)
  if type(scale) == "table" then
    color, scale = scale, 1
  end
  self:_color(color or { 1, 1, 1 })
  love.graphics.print(value, x, y, 0, scale or 1)
  love.graphics.setColor(1, 1, 1)
end

-- Menu callers span lightweight UI models (`name`), declarative content
-- (`display_name`), and service entries (`label`). Keep that schema boundary
-- here so a content-backed menu can never crash while it is being drawn.
function Renderer:menu_item_label(item)
  item = item or {}
  return item.name or item.display_name or item.label or item.id or "UNNAMED OPTION"
end

function Renderer:_layout()
  local width, height = love.graphics.getDimensions()
  local margin, sidebar, gap = 20, 270, 20
  local size = math.max(10, math.min(24, math.floor(math.min(
    (width - sidebar - gap - margin * 2) / VIEW_WIDTH,
    (height - 130) / VIEW_HEIGHT
  ))))
  return size, margin, margin, margin + VIEW_WIDTH * size + gap
end

function Renderer:_camera_position(presentation, player)
  return presentation.camera_x or player.x, presentation.camera_y or player.y
end

-- Only terrain decides a wall face: a closed door, crate, or kiosk must not
-- cause its floor cell to be styled as solid map geometry.  All wall roles
-- map to real editor-assigned sheet tiles.  Corner roles are used for a single
-- exposed diagonal pair; more complex silhouettes retain the established
-- cardinal priority rather than implying an unsupported T-junction sprite.
function Renderer:wall_terrain_kind(world, x, y)
  local north = world:terrain_is_passable(x, y + 1)
  local south = world:terrain_is_passable(x, y - 1)
  local west = world:terrain_is_passable(x - 1, y)
  local east = world:terrain_is_passable(x + 1, y)
  if north and west and not south and not east then return "wall_top_left" end
  if north and east and not south and not west then return "wall_top_right" end
  if south and west and not north and not east then return "wall_bottom_left" end
  if south and east and not north and not west then return "wall_bottom_right" end
  if north then return "wall_up" end
  if south then return "wall_down" end
  if west then return "wall_left" end
  if east then return "wall_right" end
  return "wall_center"
end

-- Material identity remains authoritative in World, but the palette below
-- gives the new natural/industrial landmarks readable silhouettes even when
-- an art pack has only a generic floor and wall assignment.  It is purely
-- presentational: sprite packs may still replace the underlying tile art.
function Renderer:terrain_tint(world, x, y, fallback, passable)
  local material = world and world:get_material(x, y)
  local id = material and material.id or ""
  if passable then
    if id == "material.terrain.forest_soil" then return { 0.23, 0.18, 0.1 } end
    if id == "material.floor.conductive_metal" then return { 0.09, 0.28, 0.31 } end
    if id == "material.terrain.air" then return fallback end
  else
    if id == "material.terrain.brush" then return { 0.055, 0.22, 0.095 } end
    if id == "material.terrain.granite" then return { 0.18, 0.2, 0.25 } end
    if id == "material.terrain.stone" then return { 0.17, 0.18, 0.22 } end
    if id == "material.structure.industrial_bulkhead" then return { 0.08, 0.2, 0.25 } end
    if id == "material.structure.masonry" then return { 0.18, 0.13, 0.16 } end
  end
  return fallback
end

-- Terrain is always readable inside the world bounds. Tactical line-of-sight
-- controls information such as enemies and telegraphs, never map discovery.
function Renderer:terrain_is_renderable(x, y)
  return Grid.in_bounds(x, y)
end

function Renderer:_screen_position(presentation, player, x, y, size, offset_x, offset_y)
  local camera_x, camera_y = self:_camera_position(presentation, player)
  local screen_x = x - camera_x + math.floor((VIEW_WIDTH - 1) / 2)
  local screen_y = y - camera_y + math.floor((VIEW_HEIGHT - 1) / 2)
  if screen_x < -1 or screen_x >= VIEW_WIDTH + 1 or screen_y < -1 or screen_y >= VIEW_HEIGHT + 1 then
    return nil
  end
  return offset_x + screen_x * size, offset_y + (VIEW_HEIGHT - 1 - screen_y) * size
end

-- A small art-independent pip makes the movement-derived facing readable for
-- both ATTACK and directional USE without turning it into a large HUD arrow.
function Renderer:facing_marker_bounds(direction, x, y, size)
  local offset = FACING_MARKER_OFFSETS[direction]
  if not offset then return nil end
  local marker = math.max(2, math.floor(size * Tuning.facing_marker_ratio))
  return x + size * offset[1] - marker / 2, y + size * offset[2] - marker / 2, marker
end

function Renderer:_draw_facing_marker(direction, x, y, size)
  local marker_x, marker_y, marker = self:facing_marker_bounds(direction, x, y, size)
  if not marker_x then return end
  self:_color({ 0.03, 0.05, 0.09, 0.9 })
  love.graphics.rectangle("fill", marker_x - 1, marker_y - 1, marker + 2, marker + 2)
  self:_color({ 0.96, 0.84, 0.28, 0.9 })
  love.graphics.rectangle("fill", marker_x, marker_y, marker, marker)
  love.graphics.setColor(1, 1, 1)
end

function Renderer:_draw_outline(sprite_kind, x, y, size, color, transform)
  if not self.assets or not self.assets.draw_sprite then return end
  local thickness = math.max(1, math.floor(size * Tuning.outline_ratio))
  for _, offset in ipairs({ { -thickness, 0 }, { thickness, 0 }, { 0, -thickness }, { 0, thickness } }) do
    local outline_transform = {
      offset_x = (transform and transform.offset_x or 0) + offset[1],
      offset_y = (transform and transform.offset_y or 0) + offset[2],
      scale_x = transform and transform.scale_x or 1,
      scale_y = transform and transform.scale_y or 1,
    }
    self.assets:draw_sprite(sprite_kind, x, y, size, color, outline_transform)
  end
end

function Renderer:_draw_footprint(x, y, size, style, time)
  local danger = style == "threat"
  local pulse = 0.55 + math.sin((time or 0) * (danger and 8 or 6)) * 0.12
  local color = danger and { 1, 0.2, 0.18, Tuning.threat_preview_alpha * pulse }
    or { 0.22, 0.86, 1, Tuning.attack_preview_alpha * pulse }
  self:_color(color)
  love.graphics.setLineWidth(math.max(1, math.floor(size * 0.075)))
  local inset = math.max(1, size * 0.13)
  love.graphics.rectangle("line", x + inset, y + inset, size - inset * 2, size - inset * 2)
  love.graphics.setLineWidth(1)
end

function Renderer:_draw_weapon_orientation(preview, player, x, y, size)
  if not preview or not preview.direction then return end
  local delta = FACING_MARKER_OFFSETS[preview.direction]
  if not delta then return end
  local center_x, center_y = x + size * 0.5, y + size * 0.5
  local dx, dy = delta[1] - 0.5, delta[2] - 0.5
  local ranged = preview.implementation == "projectile" or preview.implementation == "piercing_projectile" or preview.implementation == "scattershot"
  local length = size * (ranged and 0.44 or 0.3)
  self:_color(ranged and { 1, 0.84, 0.3, 0.95 } or { 0.72, 0.94, 1, 0.95 })
  love.graphics.setLineWidth(math.max(1, math.floor(size * 0.09)))
  love.graphics.line(center_x + dx * size * 0.1, center_y + dy * size * 0.1, center_x + dx * length, center_y + dy * length)
  love.graphics.setLineWidth(1)
end

function Renderer:_draw_tracer(presentation, player, tracer, size, offset_x, offset_y)
  local from_x, from_y = self:_screen_position(presentation, player, tracer.from_x, tracer.from_y, size, offset_x, offset_y)
  local to_x, to_y = self:_screen_position(presentation, player, tracer.to_x, tracer.to_y, size, offset_x, offset_y)
  if not from_x or not to_x then return end
  local alpha = math.max(0, tracer.time / Tuning.tracer_lifetime)
  self:_color(tracer.piercing and { 0.4, 0.95, 1, alpha } or { 1, 0.85, 0.25, alpha })
  love.graphics.setLineWidth(math.max(1, math.floor(size * 0.09)))
  love.graphics.line(from_x + size * 0.5, from_y + size * 0.5, to_x + size * 0.5, to_y + size * 0.5)
  love.graphics.setLineWidth(1)
end

function Renderer:_draw_presentation_effects(presentation, player, size, offset_x, offset_y)
  for _, tracer in ipairs(presentation.tracers or {}) do self:_draw_tracer(presentation, player, tracer, size, offset_x, offset_y) end
  for _, particle in ipairs(presentation.particles or {}) do
    local x, y = self:_screen_position(presentation, player, particle.x, particle.y, size, offset_x, offset_y)
    if x then
      local alpha = math.max(0, particle.time / Tuning.particle_lifetime)
      local color = particle.cause == "electrical" and { 0.3, 0.9, 1, alpha }
        or particle.cause == "explosive" and { 1, 0.46, 0.16, alpha }
        or { 1, 0.84, 0.42, alpha }
      self:_color(color)
      love.graphics.rectangle("fill", x + size * 0.44, y + size * 0.44, math.max(1, size * 0.12), math.max(1, size * 0.12))
    end
  end
  for _, death in ipairs(presentation.deaths or {}) do
    local x, y = self:_screen_position(presentation, player, death.x, death.y, size, offset_x, offset_y)
    if x then
      self:_color({ 1, 0.3, 0.24, math.max(0, death.time / Tuning.particle_lifetime) })
      love.graphics.setLineWidth(math.max(1, math.floor(size * 0.08)))
      love.graphics.rectangle("line", x + size * 0.17, y + size * 0.17, size * 0.66, size * 0.66)
      love.graphics.setLineWidth(1)
    end
  end
  for _, number in ipairs(presentation.damage_numbers or {}) do
    local x, y = self:_screen_position(presentation, player, number.x, number.y, size, offset_x, offset_y)
    if x then
      local progress = 1 - number.time / Tuning.damage_number_lifetime
      local color = number.cause == "electrical" and { 0.38, 0.92, 1, 1 - progress }
        or number.cause == "explosive" and { 1, 0.58, 0.22, 1 - progress }
        or { 1, 0.94, 0.76, 1 - progress }
      self:_text(tostring(number.amount), x + size * 0.5 + number.lane * size * 0.12, y - size * (0.18 + progress * 0.5), 0.76, color)
    end
  end
  for _, label in ipairs(presentation.break_labels or {}) do
    local x, y = self:_screen_position(presentation, player, label.x, label.y, size, offset_x, offset_y)
    if x then self:_text(label.text, x + size * 0.08, y - size * 0.32, 0.48, { 1, 0.34, 0.24, label.time / Tuning.break_label_lifetime }) end
  end
  for index, chain in ipairs(presentation.modifier_chain or {}) do
    local progress = 1 - chain.time / require("src.rendering.tuning").modifier_chain_lifetime
    local label = chain.name .. (chain.stacks and " ×" .. chain.stacks or "")
      .. (chain.count > 1 and " TRIGGERS ×" .. chain.count or "")
    self:_text(label, 18, 106 + (index - 1) * 26 + progress * 6, 0.62, { 0.95, 0.79, 0.3, 1 - progress * 0.45 })
    self:_text("→ " .. chain.summary, 28, 121 + (index - 1) * 26 + progress * 6, 0.47, { 0.73, 0.87, 1, 1 - progress * 0.45 })
  end
end

function Renderer:_draw_forecast_fill(x, y, size, style, time)
  local danger = style == "danger"
  local pulse = 0.5 + math.sin((time or 0) * (danger and 10 or 7)) * 0.12
  self:_color(danger and { 1, 0.16, 0.1, 0.2 + pulse * 0.2 } or { 1, 0.72, 0.12, 0.13 + pulse * 0.15 })
  love.graphics.rectangle("fill", x, y, size, size)
end

function Renderer:_draw_forecast_outline(x, y, size, style, time)
  local danger = style == "danger"
  local pulse = 0.5 + math.sin((time or 0) * (danger and 10 or 7)) * 0.2
  self:_color(danger and { 1, 0.24, 0.16, 0.68 + pulse * 0.25 } or { 1, 0.82, 0.22, 0.58 + pulse * 0.25 })
  love.graphics.setLineWidth(math.max(1, math.floor(size * 0.1)))
  local inset = math.max(1, size * 0.09)
  love.graphics.rectangle("line", x + inset, y + inset, size - inset * 2, size - inset * 2)
  love.graphics.setLineWidth(1)
end

function Renderer:_integrity_severity(current, maximum)
  if not current or not maximum or maximum <= 0 or current >= maximum or current <= 0 then return 0 end
  local ratio = current / maximum
  return ratio <= 1 / 3 and 2 or 1
end

-- Generic geometry, not a damaged-sprite requirement. One base sprite can
-- therefore communicate persistent structural damage across every material.
function Renderer:_draw_damage_overlay(x, y, size, severity)
  if severity <= 0 then return end
  self:_color({ 0.04, 0.025, 0.035, severity == 2 and 0.82 or 0.55 })
  love.graphics.setLineWidth(math.max(1, math.floor(size * 0.09)))
  love.graphics.line(x + size * 0.2, y + size * 0.16, x + size * 0.53, y + size * 0.48, x + size * 0.4, y + size * 0.78)
  if severity == 2 then love.graphics.line(x + size * 0.64, y + size * 0.2, x + size * 0.49, y + size * 0.49, x + size * 0.78, y + size * 0.7) end
  love.graphics.setLineWidth(1)
end

function Renderer:_draw_tool_impact(x, y, size, impact)
  local pulse = math.max(0, math.min(1, (impact.time or 0) / 0.18))
  local color = impact.applied and { 1, 0.78, 0.32, 0.35 + pulse * 0.58 } or { 0.75, 0.84, 0.95, 0.28 + pulse * 0.42 }
  self:_color(color)
  local inset = size * (0.18 + (1 - pulse) * 0.12)
  love.graphics.setLineWidth(math.max(1, math.floor(size * 0.1)))
  love.graphics.line(x + inset, y + inset, x + size - inset, y + size - inset)
  love.graphics.line(x + size - inset, y + inset, x + inset, y + size - inset)
  love.graphics.setLineWidth(1)
end

function Renderer:_draw_game(app)
  local session, state, presentation = app.session, app.session.state, app.presentation
  local size, offset_x, offset_y, hud = self:_layout()
  love.graphics.clear(0.025, 0.035, 0.055)
  local shake = presentation:screen_shake()
  local time = love.timer.getTime()
  offset_x = offset_x + math.sin(time * 78) * size * 0.45 * shake
  offset_y = offset_y + math.cos(time * 93) * size * 0.3 * shake
  self:_color({ 0.08, 0.1, 0.14 })
  love.graphics.rectangle("fill", offset_x - 4, offset_y - 4, VIEW_WIDTH * size + 8, VIEW_HEIGHT * size + 8)

  local floor = {
    forest = { 0.09, 0.19, 0.13 },
    cave = { 0.12, 0.14, 0.18 },
    dungeon = { 0.16, 0.12, 0.18 },
    reactor = { 0.08, 0.18, 0.22 },
    arena = { 0.14, 0.12, 0.17 },
  }
  floor = floor[state.settings.terrain]

  local camera_x, camera_y = self:_camera_position(presentation, state.player)
  local base_x = math.floor(camera_x) - math.floor((VIEW_WIDTH - 1) / 2)
  local base_y = math.floor(camera_y) - math.floor((VIEW_HEIGHT - 1) / 2)
  local camera_offset_x, camera_offset_y = camera_x - math.floor(camera_x), camera_y - math.floor(camera_y)
  for view_x = -1, VIEW_WIDTH do
    for view_y = -1, VIEW_HEIGHT do
      local x, y = base_x + view_x, base_y + view_y
      local pixel_x = offset_x + (view_x - camera_offset_x) * size
      local pixel_y = offset_y + (VIEW_HEIGHT - 1 - view_y + camera_offset_y) * size
      local passable = state.world and state.world:is_passable(x, y)
      if self:terrain_is_renderable(x, y) then
        local terrain_cell = state.world and state.world:get_cell(x, y)
        if passable then
          local floor_tint = self:terrain_tint(state.world, x, y, floor, true)
          self:_color(floor_tint)
          love.graphics.rectangle("fill", pixel_x, pixel_y, size, size)
          local terrain_drawn = self.assets:draw_terrain("floor", pixel_x, pixel_y, size, floor_tint)
          if not terrain_drawn and (x * 7 + y * 11) % 5 == 0 then
            self:_color({ floor_tint[1] * 1.55, floor_tint[2] * 1.55, floor_tint[3] * 1.55 })
            love.graphics.rectangle("fill", pixel_x + size * 0.35, pixel_y + size * 0.35, math.max(1, size * 0.12), math.max(1, size * 0.12))
          end
        else
          local wall_tint = self:terrain_tint(state.world, x, y, { 0.11, 0.075, 0.13 }, false)
          self:_color(wall_tint)
          love.graphics.rectangle("fill", pixel_x, pixel_y, size, size)
          local terrain_drawn = self.assets:draw_terrain(self:wall_terrain_kind(state.world, x, y), pixel_x, pixel_y, size, wall_tint)
          if not terrain_drawn then
            self:_color({ wall_tint[1] * 1.4, wall_tint[2] * 1.4, wall_tint[3] * 1.4 })
            love.graphics.rectangle("line", pixel_x, pixel_y, size, size)
          end
        end
        if terrain_cell then
          local terrain_material = session.registry:get_material(terrain_cell.material_id)
          self:_draw_damage_overlay(pixel_x, pixel_y, size,
            self:_integrity_severity(terrain_cell.current_integrity, terrain_material.max_integrity))
        end
      end
    end
  end

  -- Liquid is a simulation-owned layer over passable terrain. Its sprite role
  -- and depth tint are presentation only; they never affect the flow.
  for _, liquid in ipairs(state.world and state.world:list_liquids() or {}) do
    if state.visible[Grid.key(liquid.x, liquid.y)] then
      local pixel_x, pixel_y = self:_screen_position(presentation, state.player, liquid.x, liquid.y, size, offset_x, offset_y)
      if pixel_x then
        local definition = session.registry:get_liquid(liquid.liquid_id)
        local depth = liquid.amount / definition.max_depth
        self.assets:draw_sprite(depth >= 0.66 and "water_deep" or "water_shallow", pixel_x, pixel_y, size,
          { 0.32, 0.78, 1, 0.42 + depth * 0.28 })
      end
    end
  end

  -- Gas is an authoritative coordinate layer, rendered after liquid so both
  -- can coexist visibly. Its sprite drift depends only on wall-clock time and
  -- coordinates; it neither uses simulation RNG nor affects no-fog LOS.
  for _, gas in ipairs(state.world and state.world:list_gases() or {}) do
    if state.visible[Grid.key(gas.x, gas.y)] then
      local pixel_x, pixel_y = self:_screen_position(presentation, state.player, gas.x, gas.y, size, offset_x, offset_y)
      if pixel_x then
        local definition = session.registry:get_gas(gas.gas_id)
        local density = gas.concentration / definition.max_concentration
        local drift = math.sin(time * 2.7 + gas.x * 1.9 + gas.y * 3.1) * size * 0.06
        self.assets:draw_sprite("gas", pixel_x, pixel_y, size, { 0.56, 1, 0.4, 0.16 + density * 0.38 }, {
          offset_x = drift, offset_y = -drift * 0.35, scale_x = 0.92 + density * 0.12, scale_y = 0.92 + density * 0.12,
        })
      end
    end
  end

  -- Hazards are a passable, simulation-owned floor layer. Their silhouette is
  -- an editable art-pack role rather than renderer-authored line geometry.
  for _, hazard in ipairs(state.world and state.world:list_hazards() or {}) do
    if state.visible[Grid.key(hazard.x, hazard.y)] then
      local pixel_x, pixel_y = self:_screen_position(presentation, state.player, hazard.x, hazard.y, size, offset_x, offset_y)
      if pixel_x then
        self.assets:draw_sprite("spikes", pixel_x, pixel_y, size, { 1, 0.35, 0.25, 0.92 })
      end
    end
  end

  -- Fire is authoritative world state. Its sprite flicker is presentation-only
  -- and uses wall-clock time, never the deterministic simulation RNG.
  for _, fire in ipairs(state.world and state.world:list_fires() or {}) do
    local fire_x, fire_y = state.world:fire_position(fire)
    if fire_x and state.visible[Grid.key(fire_x, fire_y)] then
      local pixel_x, pixel_y = self:_screen_position(presentation, state.player, fire_x, fire_y, size, offset_x, offset_y)
      if pixel_x then
        local flicker = 0.06 + math.sin(time * 11 + fire_x * 3 + fire_y * 5) * 0.035
        self.assets:draw_sprite("fire", pixel_x, pixel_y, size, { 1, 0.42 + flicker, 0.12, 0.94 }, {
          offset_y = -size * flicker, scale_x = 0.94, scale_y = 1.02 + flicker,
        })
      end
    end
  end

  -- A detonated flare remains a short control zone. Its low-key amber field
  -- makes the mechanical enemy-avoidance radius readable without exposing
  -- tactical information outside the player's current perception.
  for _, flare in ipairs(state.flares or {}) do
    if flare.detonated and (flare.light_remaining or 0) > 0 then
      local pulse = 0.08 + math.sin(time * 8 + flare.x * 3 + flare.y) * 0.025
      local radius = flare.radius or 0
      for x = flare.x - radius, flare.x + radius do
        for y = flare.y - radius, flare.y + radius do
          if Grid.in_bounds(x, y) and Grid.distance(flare, { x = x, y = y }) <= radius
            and state.visible[Grid.key(x, y)] then
            local pixel_x, pixel_y = self:_screen_position(presentation, state.player, x, y, size, offset_x, offset_y)
            if pixel_x then
              self:_color({ 1, 0.64, 0.12, pulse })
              love.graphics.rectangle("fill", pixel_x, pixel_y, size, size)
            end
          end
        end
      end
    end
  end

  -- Direct player intent and visible hostile intent share the same grounded
  -- tile-outline grammar. Geometry comes from Session's authoritative attack
  -- helpers, never duplicated renderer guesses.
  if not app:is_build_stance() then
    local preview = session:player_attack_preview()
    for _, cell in ipairs(preview and preview.cells or {}) do
      local x, y = self:_screen_position(presentation, state.player, cell.x, cell.y, size, offset_x, offset_y)
      if x then self:_draw_footprint(x, y, size, "player", time) end
    end
  end
  for _, threat in ipairs(session:visible_enemy_threats()) do
    for _, cell in ipairs(threat.preview.cells or {}) do
      local x, y = self:_screen_position(presentation, state.player, cell.x, cell.y, size, offset_x, offset_y)
      if x then self:_draw_footprint(x, y, size, "threat", time) end
    end
  end

  -- Every physical world object is a named art-pack role.  State remains
  -- readable through a restrained tint, but landmarks and fixtures never fall
  -- back to renderer-made rectangles, lines, or polygons.
  for _, object in ipairs(state.world and state.world:list_objects() or {}) do
    if state.visible[Grid.key(object.x, object.y)] then
      local pixel_x, pixel_y = self:_screen_position(presentation, state.player, object.x, object.y, size, offset_x, offset_y)
      if pixel_x then
        local definition = session.registry:get_world_object(object.definition_id)
        local tint = { 1, 1, 1 }
        if object.interaction_role == "door" then
          local operational = definition.power_required ~= true or state.world:is_circuit_powered(object.circuit_id)
          tint = object.door_state == "open" and { 0.42, 0.9, 0.86, 0.5 }
            or (operational and { 0.65, 1, 0.83 } or { 1, 0.46, 0.3 })
        elseif object.interaction_role == "generator" then
          tint = object.generator_online and { 0.43, 1, 0.65 } or { 0.5, 0.55, 0.6 }
        elseif object.interaction_role == "breaker" then
          local circuit = state.world:get_circuit(object.circuit_id)
          tint = circuit.enabled and { 1, 0.8, 0.35 } or { 0.85, 0.38, 0.3 }
        elseif object.interaction_role == "discovery" then
          tint = object.discovery_claimed and { 0.48, 0.55, 0.6 } or { 1, 0.85, 0.3 }
        elseif object.interaction_role == "reinforcement" then
          local spent = object.reinforcement_state == "spent" or object.reinforcement_state == "cancelled"
          tint = spent and { 0.38, 0.42, 0.48 } or (object.reinforcement_state == "armed" and { 1, 0.35, 0.2 } or { 1, 0.68, 0.3 })
        elseif object.interaction_role == "traversal" then
          tint = object.required_unlock == "unlock.traversal.maintenance_override" and { 0.38, 0.95, 0.9 } or { 1, 0.63, 0.28 }
        elseif object.interaction_role == "zone_connection" then
          tint = object.zone_connection_direction == "down" and { 0.72, 0.54, 1 } or { 0.45, 0.9, 1 }
        elseif object.interaction_role == "reconstruction_station" then
          tint = { 0.48, 0.95, 1 }
        elseif object.interaction_role == "clue" then
          tint = { 1, 0.78, 0.28 }
        end
        self.assets:draw_sprite(definition.render_style, pixel_x, pixel_y, size, tint)
        self:_draw_damage_overlay(pixel_x, pixel_y, size,
          self:_integrity_severity(object.current_integrity, session.registry:get_material(object.material_id).max_integrity))
      end
    end
  end

  -- Ground cargo is authoritative zone state. A compact glyph keeps stacks
  -- visible without inventing a second object/physics layer or new art role.
  for _, ground in ipairs(state.world and state.world:list_ground_items() or {}) do
    if state.visible[Grid.key(ground.x, ground.y)] then
      local pixel_x, pixel_y = self:_screen_position(presentation, state.player, ground.x, ground.y, size, offset_x, offset_y)
      if pixel_x then
        local tint = ground.item.item_type == "resource_stack" and { 0.95, 0.82, 0.32 } or { 0.7, 0.9, 1 }
        self:_text("+", pixel_x + size * 0.32, pixel_y + size * 0.2, 0.7, tint)
      end
    end
  end

  -- Construction is a faced-cell field stance.  The ghost is entirely
  -- presentation state; Building.validate remains authoritative and no map
  -- discovery/shroud state is involved in deciding whether it is drawn.
  if app:is_build_stance() then
    local recipe, target = app:active_build_recipe(), app:build_target()
    local preview = app:build_preview()
    if recipe and target then
      local preview_x, preview_y = self:_screen_position(presentation, state.player, target.x, target.y, size, offset_x, offset_y)
      if preview_x then
        local definition = session.registry:get_world_object(recipe.world_object_id)
        local tint = preview.applied and { 0.35, 1, 0.62, 0.48 } or { 1, 0.26, 0.25, 0.48 }
        self.assets:draw_sprite(definition.render_style, preview_x, preview_y, size, tint)
        self:_color(preview.applied and { 0.35, 1, 0.62, 0.95 } or { 1, 0.3, 0.28, 0.95 })
        love.graphics.setLineWidth(2)
        love.graphics.rectangle("line", preview_x + 2, preview_y + 2, size - 4, size - 4)
        love.graphics.setLineWidth(1)
      end
    end
  end

  for _, impact in ipairs(presentation.impacts or {}) do
    local pixel_x, pixel_y = self:_screen_position(presentation, state.player, impact.x, impact.y, size, offset_x, offset_y)
    if pixel_x then self:_draw_tool_impact(pixel_x, pixel_y, size, impact) end
  end

  local forecasts = session:telegraphs()
  for location_key, style in pairs(forecasts) do
    local x, y = location_key:match("(%d+):(%d+)")
    local forecast_x, forecast_y = tonumber(x), tonumber(y)
    if state.visible[Grid.key(forecast_x, forecast_y)] then
      local pixel_x, pixel_y = self:_screen_position(presentation, state.player, forecast_x, forecast_y, size, offset_x, offset_y)
      if pixel_x then
        self:_draw_forecast_fill(pixel_x, pixel_y, size, style, time)
      end
    end
  end

  local function actor(value, always_visible, override_tint)
    if not value or (not always_visible and not state.visible[Grid.key(value.x, value.y)]) then
      return
    end
    local render_x, render_y = presentation:position(value)
    local pixel_x, pixel_y = self:_screen_position(presentation, state.player, render_x, render_y, size, offset_x, offset_y)
    if not pixel_x then
      return
    end
    local tint = override_tint or (value.stun and value.stun > 0 and { 1, 0.85, 0.2 } or nil)
    if not tint and value ~= state.player then
      tint = value.faction_id == "faction.machine" and { 0.52, 0.82, 0.96 }
        or value.faction_id == "faction.cult" and { 0.82, 0.5, 0.96 }
        or value.faction_id == "faction.feral" and { 0.96, 0.58, 0.32 }
        or nil
    end
    if value == state.player and presentation.hit_flash > 0 then
      tint = { 1, 0.35, 0.35 }
    end
    if presentation:reaction_flash(value) then
      tint = value == state.player and { 1, 0.42, 0.42 } or { 1, 0.93, 0.72 }
    end
    -- Historical echoes intentionally share the player body silhouette; the
    -- corrupt violet tint is presentation only and their actual capabilities
    -- remain body-derived in the simulation.
    local sprite_kind = value.kind == "fallen_echo" and "player" or value.kind
    if value.kind == "fallen_echo" and not tint then tint = { 0.76, 0.34, 0.88 } end
    local transform = value.body and (presentation:movement_transform(value)
      or presentation:idle_transform(session, value, time, size)) or nil
    transform = presentation.merge_transforms(transform, presentation:reaction_transform(value, size))
    transform = presentation.merge_transforms(transform, presentation:attack_transform(value, size))
    if value == state.player then
      transform = presentation.merge_transforms(transform, presentation:facing_transform(value, size))
      transform = presentation.merge_transforms(transform, presentation:player_bump_transform(size))
    end
    local role = self:outline_role(value, state)
    if role == "player" then
      self:_draw_outline(sprite_kind, pixel_x, pixel_y, size, { 0.9, 0.98, 1, 0.82 }, transform)
    elseif role == "hostile" then
      self:_draw_outline(sprite_kind, pixel_x, pixel_y, size, { 1, 0.22, 0.18, 0.78 }, transform)
    elseif role == "projectile" then
      self:_draw_outline(sprite_kind, pixel_x, pixel_y, size, { 1, 0.86, 0.25, 0.9 }, transform)
    end
    self.assets:draw_sprite(sprite_kind, pixel_x, pixel_y, size, tint, transform)
    if value == state.player then
      if not app:is_build_stance() then self:_draw_weapon_orientation(session:player_attack_preview(), value, pixel_x, pixel_y, size) end
      self:_draw_facing_marker(value.direction, pixel_x, pixel_y, size)
    end
  end

  for _, values in ipairs({ state.torches, state.targets, state.enemies, state.bullets, state.bombs, state.flares }) do
    for _, value in ipairs(values) do
      actor(value)
    end
  end
  for _, corpse in ipairs(state.corpses or {}) do
    local remains = { kind = corpse.source_kind, x = corpse.x, y = corpse.y }
    actor(remains, false, corpse.fallen_archive_id and { 0.5, 0.24, 0.66, 0.9 } or { 0.32, 0.28, 0.4, 0.8 })
    local pixel_x, pixel_y = self:_screen_position(presentation, state.player, corpse.x, corpse.y, size, offset_x, offset_y)
    if pixel_x then
      self:_color(corpse.fallen_archive_id and { 0.98, 0.56, 0.9, 0.9 } or { 0.85, 0.3, 0.45, 0.75 })
      love.graphics.rectangle("line", pixel_x + size * 0.2, pixel_y + size * 0.2, size * 0.6, size * 0.6)
    end
  end
  actor(state.ammo)
  actor(state.exit)
  for location_key in pairs(state.effects) do
    local x, y = location_key:match("(%d+):(%d+)")
    actor({ kind = "flare", x = tonumber(x), y = tonumber(y) })
  end
  -- A discharge result is presentation-only. The simulation has already
  -- resolved the network and clears this brief cyan trace on the next turn.
  for _, cell in ipairs(state.electrical_effects or {}) do
    local effect_x, effect_y = self:_screen_position(presentation, state.player, cell.x, cell.y, size, offset_x, offset_y)
    if effect_x then
      local pulse = 0.48 + math.sin(time * 21 + cell.x * 5 + cell.y * 7) * 0.18
      self.assets:draw_sprite("electric_arc", effect_x, effect_y, size, { 0.35, 0.85, 1, pulse })
    end
  end
  if state.boss then
    actor(state.boss)
  end
  actor(state.player, true)
  self:_draw_presentation_effects(presentation, state.player, size, offset_x, offset_y)
  -- Draw the forecast border after actors so a target cell stays legible even
  -- when the player or another actor occupies it. The player selection box is
  -- still drawn last.
  for location_key, style in pairs(forecasts) do
    local x, y = location_key:match("(%d+):(%d+)")
    local forecast_x, forecast_y = tonumber(x), tonumber(y)
    if state.visible[Grid.key(forecast_x, forecast_y)] then
      local pixel_x, pixel_y = self:_screen_position(presentation, state.player, forecast_x, forecast_y, size, offset_x, offset_y)
      if pixel_x then self:_draw_forecast_outline(pixel_x, pixel_y, size, style, time) end
    end
  end
  local player_x, player_y = presentation:position(state.player)
  local pixel_x, pixel_y = self:_screen_position(presentation, state.player, player_x, player_y, size, offset_x, offset_y)
  if pixel_x then
    self:_color({ 0.15, 0.9, 1 })
    love.graphics.rectangle("line", pixel_x, pixel_y, size, size)
    if presentation.hit_flash > 0 then
      self:_color({ 1, 0.2, 0.2, 0.75 })
      love.graphics.setLineWidth(2)
      love.graphics.rectangle("line", pixel_x - size * 0.12, pixel_y - size * 0.12, size * 1.24, size * 1.24)
      love.graphics.setLineWidth(1)
    end
  end

  local ui = GameplayUI.hud(session)
  self:_text(ui.expedition and "EXPEDITION" or "ROAG", hud, offset_y, 2, { 0.7, 0.9, 1 })
  if ui.location then self:_text(ui.location, hud, offset_y + 23, 0.62, { 0.78, 0.78, 0.6 }) end
  if ui.reconstruction_anchor then
    self:_text(ui.reconstruction_anchor.current_zone and "RESPAWN ANCHOR: THIS ZONE" or "RESPAWN ANCHOR: REMOTE ZONE",
      hud, offset_y + 34, 0.46, ui.reconstruction_anchor.current_zone and { 0.58, 0.88, 0.78 } or { 0.95, 0.72, 0.35 })
  end
  self:_text("HP " .. ui.health .. " / " .. ui.max_health .. "   " .. string.rep("♥", ui.health), hud, offset_y + 42,
    1 + presentation.hit_flash * 0.8, { 1, 0.35, 0.35 })
  if ui.quick then
    local active_weapon = ui.quick.weapons[1].active and ui.quick.weapons[1] or ui.quick.weapons[2]
    local active_ability = ui.quick.abilities[1].active and ui.quick.abilities[1] or ui.quick.abilities[2]
    local function quick_label(prefix, slot, index)
      return prefix .. " " .. string.char(string.byte("A") + index - 1) .. (slot.active and " * " or "   ") .. string.upper(slot.label)
    end
    self:_text(quick_label("W", ui.quick.weapons[1], 1), hud, offset_y + 64, 0.66,
      ui.quick.weapons[1].active and { 0.95, 0.85, 0.3 } or { 0.72, 0.8, 0.92 })
    self:_text(quick_label("W", ui.quick.weapons[2], 2), hud, offset_y + 80, 0.66,
      ui.quick.weapons[2].active and { 0.95, 0.85, 0.3 } or { 0.72, 0.8, 0.92 })
    if active_weapon and active_weapon.ammo then
      local ammo = active_weapon.ammo
      self:_text("MAG " .. ammo.loaded .. "/" .. ammo.capacity .. " " .. string.upper(session.registry:get_resource(ammo.family).display_name)
        .. " · " .. ammo.reserve .. " RESERVE", hud, offset_y + 96, 0.62, { 0.6, 0.9, 0.75 })
    else
      self:_text("MELEE / NO MAGAZINE", hud, offset_y + 96, 0.62, { 0.72, 0.8, 0.92 })
    end
    self:_text(quick_label("A", ui.quick.abilities[1], 1), hud, offset_y + 112, 0.64,
      ui.quick.abilities[1].active and { 0.95, 0.65, 0.35 } or { 0.72, 0.8, 0.92 })
    self:_text(quick_label("A", ui.quick.abilities[2], 2), hud, offset_y + 128, 0.64,
      ui.quick.abilities[2].active and { 0.95, 0.65, 0.35 } or { 0.72, 0.8, 0.92 })
    if active_ability and not active_ability.available then
      self:_text("ABILITY OFFLINE", hud, offset_y + 144, 0.6, { 1, 0.42, 0.42 })
    end
  else
    self:_text("AMMO " .. ui.ammo .. "   BOMBS " .. ui.bombs .. " (" .. ui.armed_bombs .. " ARMED)", hud, offset_y + 64)
    self:_text("FLARES " .. ui.flares .. " (" .. ui.lit_flares .. " LIT)   DASH " .. ui.dash, hud, offset_y + 84, 0.84)
  end
  local primary_status = state.boss and ("BOSS HP " .. state.boss.health .. " / " .. state.boss.max_health)
    or (ui.expedition and ("STAGE " .. ui.expedition.stage .. "   ENCOUNTER " .. ui.expedition.encounter))
    or (ui.objective_required and ("OBJECTIVE " .. ui.objective_progress .. " / " .. ui.objective_required))
  if primary_status then
    self:_text(primary_status, hud, offset_y + 112, 0.88, { 0.95, 0.85, 0.25 })
  end
  local economy_y = ui.quick and offset_y + 162 or offset_y + 134
  if ui.expedition then
    self:_text("SCRAP " .. ui.expedition.currency .. "   PASSIVE STACKS " .. ui.expedition.passive_stacks,
      hud, economy_y, 0.82, { 0.65, 0.9, 0.8 })
    self:_text("I RUN BUILD   E WEAPON   Q ABILITY", hud, economy_y + 20, 0.66, { 0.72, 0.8, 0.92 })
  else
    self:_text("SCRAP " .. ui.scrap .. "   CHARMS " .. ui.charm_count .. "/" .. ui.charm_slots, hud, economy_y, 0.82, { 0.65, 0.9, 0.8 })
    self:_text("CARGO " .. ui.cargo_mass .. "  " .. ui.encumbrance, hud, economy_y + 20, 0.8,
      ui.encumbrance == "LIGHT" and { 0.65, 0.9, 0.8 } or { 0.95, 0.72, 0.35 })
  end
  if ui.curse then
    self:_text("CURSE " .. ui.curse, hud, economy_y + 40, 0.76, { 0.9, 0.4, 0.8 })
  end
  local status_y = ui.quick and economy_y + 64 or offset_y + 198
  if ui.locomotion ~= "NORMAL" then
    local locomotion_color = ui.locomotion == "IMPAIRED" and { 1, 0.72, 0.3 } or { 1, 0.42, 0.42 }
    self:_text("LOCOMOTION " .. ui.locomotion, hud, status_y, 0.8, locomotion_color)
    status_y = status_y + 18
  end
  if not ui.quick then
    local ranged_ability = session:actor_ability_by_implementation(state.player, "projectile")
    local ranged = ranged_ability and session:actor_ability_provider(state.player, ranged_ability) or nil
    if ranged then
      local definition = session.registry:get_component(ranged.component.definition_id)
      self:_text("WEAPON " .. string.upper(definition.display_name), hud, status_y, 0.68, { 0.65, 0.9, 0.8 })
    else
      self:_text("WEAPON OFFLINE", hud, status_y, 0.72, { 1, 0.42, 0.42 })
    end
    status_y = status_y + 18
  end
  if state.boss then
    local boss = state.boss
    local boss_model = GameplayUI.boss(session, boss)
    self:_text(boss_model.name, hud, status_y, 0.8, { 1, 0.6, 0.35 })
    status_y = status_y + 18
    if boss_model.telegraph then
      local telegraph = boss_model.telegraph
      self:_text((telegraph.cancelled and "TELEGRAPH CANCELLED — PROVIDER DESTROYED" or
        (string.upper(telegraph.ability) .. " — CHARGING " .. telegraph.remaining .. " • BREAK " .. string.upper(telegraph.provider))),
        hud, status_y, 0.56, { 1, 0.5, 0.28 })
      status_y = status_y + 18
    end
    local shown = 0
    for _, subsystem in ipairs(boss_model.subsystems) do
      if shown < 3 then
        self:_text(string.upper(subsystem.name) .. " " .. subsystem.condition, hud, status_y, 0.58,
          subsystem.functional and { 0.75, 0.84, 0.94 } or { 1, 0.4, 0.35 })
        status_y = status_y + 15
        shown = shown + 1
      end
    end
    local boss_locomotion = session:locomotion_state(boss)
    self:_text("BOSS LOCOMOTION " .. boss_locomotion.state, hud, status_y, 0.58, { 0.72, 0.82, 0.9 })
    status_y = status_y + 15
  end
  local abilities = session:available_actor_abilities(state.player, "body")
  if not ui.quick and #abilities > 0 then
    local ability = session.registry:get_ability(abilities[1])
    self:_text("BODY X: " .. string.upper(ability.display_name), hud, status_y, 0.72, { 0.95, 0.65, 0.35 })
    status_y = status_y + 18
  end
  local context = GameplayUI.context_action(session)
  if context then
    self:_text(context.key .. " " .. context.label .. (context.available and "" or " — " .. context.reason),
      hud, status_y, 0.68, context.available and { 0.6, 0.9, 0.75 } or { 1, 0.48, 0.32 })
    status_y = status_y + 18
  end
  if app:is_build_stance() then
    local recipe, preview = app:active_build_recipe(), app:build_preview()
    if recipe then
      self:_text("BUILD — " .. string.upper(recipe.display_name), hud, status_y, 0.72, { 0.95, 0.85, 0.3 })
      status_y = status_y + 18
      local costs = {}
      local counts = state.inventory:resource_counts()
      for _, cost in ipairs(recipe.costs) do
        local resource = session.registry:get_resource(cost.resource_id)
        costs[#costs + 1] = string.upper(resource.display_name) .. " " .. (counts[cost.resource_id] or 0) .. "/" .. cost.amount
      end
      self:_text(table.concat(costs, "  "), hud, status_y, 0.58, preview.applied and { 0.6, 0.9, 0.75 } or { 1, 0.48, 0.32 })
      status_y = status_y + 17
      if not preview.applied then
        self:_text(GameplayUI.failure_text(preview), hud, status_y, 0.58, { 1, 0.48, 0.32 })
        status_y = status_y + 17
      end
    end
  end
  local controls_y = math.max(ui.quick and offset_y + 342 or offset_y + 278, status_y + 8)
  self:_text("CONTROLS", hud, controls_y, 1, { 0.6, 0.8, 1 })
  self:_text("WASD MOVE / HOLD", hud, controls_y + 20, 0.85)
  if session.campaign and app:is_build_stance() then
    self:_text("E PLACE   R NEXT RECIPE   X PREVIOUS", hud, controls_y + 38, 0.75, { 0.95, 0.85, 0.3 })
    self:_text("Q ABILITY   U USE   I INVENTORY   C / ESC EXIT", hud, controls_y + 56, 0.68)
  else
    self:_text(session.campaign and "E ATTACK   R SWAP WEAPON" or "ARROWS SHOOT   E FORWARD", hud, controls_y + 38, 0.75)
    self:_text(session.campaign and "Q ABILITY   X SWAP ABILITY   B bomb   F flare" or "Q dash   B bomb   F flare", hud, controls_y + 56, 0.68)
    self:_text(session.campaign and "U USE / SALVAGE   I inventory   C build" or "G salvage   I inventory   U interact", hud,
      controls_y + 74, 0.68)
  end
  self:_text("NEARBY THREATS", hud, controls_y + 106, 0.88, { 0.9, 0.7, 0.4 })
  local shown_threats = 0
  for _, enemy in ipairs(state.enemies) do
    if state.visible[Grid.key(enemy.x, enemy.y)] then
      shown_threats = shown_threats + 1
      if shown_threats > 5 then break end
      local enemy_model = GameplayUI.enemy(session, enemy)
      local faction = enemy_model.faction and (" — " .. string.upper(enemy_model.faction)) or ""
      local loadout = enemy_model.weapon and (" [" .. enemy_model.role .. " / " .. string.upper(enemy_model.weapon) .. "]")
        or (" [" .. enemy_model.role .. "]")
      self:_text(enemy_model.name .. faction .. loadout .. ": " .. enemy_model.intent,
        hud, controls_y + 124 + shown_threats * 17, 0.62)
    end
  end
  for index, message in ipairs(state.log) do
    self:_text(message, 20, offset_y + VIEW_HEIGHT * size + 16 + (index - 1) * 17, 0.78, { 0.8, 0.85, 0.9 })
  end
  if presentation.hit_flash > 0 then
    local width, height = love.graphics.getDimensions()
    self:_color({ 1, 0.04, 0.04, (presentation.hit_flash / 0.32) * 0.16 })
    love.graphics.rectangle("fill", 0, 0, width, height)
  end
end

function Renderer:_draw_component_detail(session, component, x, y)
  if not component then
    self:_text("EMPTY SLOT", x, y, 1, { 0.65, 0.75, 0.9 })
    return
  end
  local definition = session.registry:get_component(component.definition_id)
  local abilities = #definition.abilities > 0 and definition.abilities or nil
  self:_text(definition.display_name, x, y, 1.15, { 0.95, 0.85, 0.3 })
  local slots = {}
  for _, slot_id in ipairs(definition.compatible_slots) do slots[#slots + 1] = GameplayUI.slot_label(slot_id) end
  self:_text("FITS " .. table.concat(slots, ", "), x, y + 24, 0.72)
  self:_text("INTEGRITY " .. component.current_integrity .. " / " .. component.max_integrity, x, y + 43, 0.78)
  self:_text("CONDITION " .. GameplayUI.condition_label(component), x, y + 61, 0.78,
    Component.is_functional(component) and { 0.6, 0.9, 0.75 } or { 1, 0.35, 0.35 })
  self:_text("MASS " .. definition.mass .. "   SIZE " .. definition.inventory.width .. "×" .. definition.inventory.height, x, y + 79, 0.75)
  if abilities then
    local labels = {}
    for _, ability_id in ipairs(abilities) do
      labels[#labels + 1] = session.registry:get_ability(ability_id).display_name
    end
    self:_text("ABILITY " .. table.concat(labels, ", "), x, y + 97, 0.7, { 0.95, 0.65, 0.35 })
  else
    self:_text("ABILITY NONE", x, y + 97, 0.7, { 0.65, 0.72, 0.82 })
  end
end

function Renderer:_draw_reconstruction(app)
  local session, state = app.session, app.session.state
  local inventory, body = state.inventory, state.player.body
  local width, height = love.graphics.getDimensions()
  local body_x, body_y = math.floor(width * 0.055), 112
  local inventory_x = math.floor(width * 0.58)
  local body_width, body_height = math.max(100, math.floor(width * 0.125)), 48
  local layout = {
    head = { 1.2, 0 },
    left_arm = { 0, 1.25 }, torso_core = { 1.2, 1.25 }, right_arm = { 2.4, 1.25 },
    internal_1 = { 0.8, 2.45 }, internal_2 = { 1.65, 2.45 },
    left_leg = { 0.35, 3.65 }, right_leg = { 2.05, 3.65 },
  }

  love.graphics.clear(0.025, 0.035, 0.055)
  self:_text("RECONSTRUCTION", body_x, 28, 2, { 0.7, 0.9, 1 })
  self:_text("PAUSED — BODY CHANGES ARE FREE UNTIL YOU FINISH", body_x, 51, 0.58, { 0.68, 0.78, 0.9 })
  self:_text("Installed mass " .. body:installed_mass() .. "   •   Cargo " .. inventory:total_mass() .. " " .. inventory:encumbrance(), body_x, 68, 0.88, { 0.95, 0.85, 0.3 })
  local locomotion = session:locomotion_state(state.player)
  local locomotion_color = locomotion.state == "NORMAL" and { 0.65, 0.9, 0.8 }
    or locomotion.state == "IMPAIRED" and { 1, 0.72, 0.3 }
    or { 1, 0.42, 0.42 }
  self:_text("LOCOMOTION " .. locomotion.state .. " (" .. locomotion.provider_count .. ")", inventory_x, 68, 0.72, locomotion_color)
  self:_text("BODY", body_x, 92, 1.15, app.reconstruction_focus == "body" and { 0.95, 0.85, 0.3 } or { 0.65, 0.75, 0.9 })
  self:_text("INVENTORY", inventory_x, 92, 1.15, app.reconstruction_focus == "inventory" and { 0.95, 0.85, 0.3 } or { 0.65, 0.75, 0.9 })

  local slots = app:reconstruction_slots()
  for index, slot in ipairs(slots) do
    local position = layout[slot.id] or { 0, index - 1 }
    local x = body_x + position[1] * body_width
    local y = body_y + position[2] * body_height
    local selected = app.reconstruction_focus == "body" and index == app.reconstruction_slot_index
    self:_color(selected and { 0.2, 0.36, 0.46 } or { 0.06, 0.09, 0.14 })
    love.graphics.rectangle("fill", x, y, body_width - 8, body_height - 6)
    self:_color(selected and { 1, 0.85, 0.2 } or { 0.23, 0.35, 0.46 })
    love.graphics.rectangle("line", x, y, body_width - 8, body_height - 6)
    self:_text(GameplayUI.slot_label(slot.id), x + 5, y + 5, 0.61, { 0.7, 0.8, 0.92 })
    self:_text(slot.component and session.registry:get_component(slot.component.definition_id).display_name or "EMPTY", x + 5, y + 22, 0.62,
      slot.component and { 0.9, 0.94, 1 } or { 0.48, 0.56, 0.67 })
  end

  local cell = math.max(30, math.min(52, math.floor(math.min((width - inventory_x - 34) / inventory.width, 230 / inventory.height))))
  local grid_y = 122
  for y = 1, inventory.height do
    for x = 1, inventory.width do
      local px, py = inventory_x + (x - 1) * cell, grid_y + (y - 1) * cell
      self:_color({ 0.055, 0.08, 0.12 })
      love.graphics.rectangle("fill", px, py, cell - 3, cell - 3)
      self:_color({ 0.18, 0.28, 0.38 })
      love.graphics.rectangle("line", px, py, cell - 3, cell - 3)
    end
  end
  for index, entry in ipairs(inventory.entries) do
    local selected = app.reconstruction_focus == "inventory" and index == app.reconstruction_inventory_index
    local queued = app.reconstruction_selected_id == entry.physical_id
    local color = (selected or queued) and { 0.92, 0.7, 0.2 } or { 0.18, 0.45, 0.58 }
    for _, shape_cell in ipairs(inventory:footprint_cells(entry.item, entry.rotated)) do
      local px = inventory_x + (entry.x - 1 + shape_cell.x) * cell + 2
      local py = grid_y + (entry.y - 1 + shape_cell.y) * cell + 2
      self:_color(color)
      love.graphics.rectangle("fill", px, py, cell - 7, cell - 7)
      self:_color({ 0.8, 0.9, 1 })
      love.graphics.rectangle("line", px, py, cell - 7, cell - 7)
    end
    self:_text(entry.item.display_name, inventory_x + (entry.x - 1) * cell + 4, grid_y + (entry.y - 1) * cell + 5, 0.58, { 0.95, 0.97, 1 })
  end

  local selected_slot = app:reconstruction_slot()
  local selected_entry = app.reconstruction_selected_id and inventory:get(app.reconstruction_selected_id) or app:reconstruction_inventory_entry()
  local detail_component = app.reconstruction_focus == "body" and selected_slot and selected_slot.component
    or selected_entry and selected_entry.item.object
  self:_draw_component_detail(session, detail_component, inventory_x, grid_y + inventory.height * cell + 28)

  local feedback = app:reconstruction_feedback()
  local feedback_color = feedback.compatible and { 0.6, 0.9, 0.75 } or { 1, 0.42, 0.42 }
  self:_text(feedback.compatible and "COMPATIBLE" or "INCOMPATIBLE", body_x, height - 92, 1, feedback_color)
  self:_text(GameplayUI.failure_text(feedback), body_x + 145, height - 92, 0.78, feedback_color)
  self:_text("TAB FOCUS   W/S SELECT   ENTER INSTALL / UNINSTALL   R ROTATE INVENTORY", body_x, height - 62, 0.7, { 0.75, 0.82, 0.92 })
  local finish_text = session.campaign and state.active_reconstruction_station_id
    and "F / ESC FINISH RECONSTRUCTION" or "F FINISH RECONSTRUCTION"
  self:_text(finish_text, body_x, height - 38, 0.76, { 0.95, 0.85, 0.3 })
end

function Renderer:_draw_body_abilities(app)
  local session = app.session
  local width, height = love.graphics.getDimensions()
  love.graphics.clear(0.025, 0.035, 0.055)
  self:_text("BODY ABILITIES", width * 0.18, 64, 2, { 0.7, 0.9, 1 })
  for index, ability_id in ipairs(app.body_ability_options or {}) do
    local model = GameplayUI.ability(session, session.state.player, ability_id)
    local y = 142 + (index - 1) * 88
    local selected = index == app.menu
    self:_color(selected and { 0.23, 0.16, 0.12 } or { 0.06, 0.08, 0.12 })
    love.graphics.rectangle("fill", width * 0.18, y, width * 0.64, 70)
    self:_text((selected and "> " or "  ") .. model.name, width * 0.21, y + 8, 1.05,
      selected and { 1, 0.65, 0.35 } or { 1, 1, 1 })
    self:_text("PROVIDER " .. (model.provider and model.provider.name or "BROKEN") .. "   " .. model.resource
      .. (model.wear > 0 and ("   WEAR " .. model.wear) or ""), width * 0.21, y + 32, 0.68, { 0.72, 0.8, 0.92 })
    local effect = {}
    if model.damage then effect[#effect + 1] = "DAMAGE " .. model.damage end
    if model.force then effect[#effect + 1] = "FORCE " .. model.force end
    if model.range then effect[#effect + 1] = "RANGE " .. model.range end
    self:_text(#effect > 0 and table.concat(effect, "   ") or "BODY-DRIVEN ACTION", width * 0.21, y + 50, 0.62, { 0.95, 0.78, 0.42 })
  end
  local selected_id = app.body_ability_options and app.body_ability_options[app.menu]
  local selected = selected_id and session.registry:get_ability(selected_id) or nil
  if app.body_ability_confirming then
    self:_text("CONFIRM ACTIVATION? THIS DESTROYS YOUR CURRENT BODY. PRESS ENTER.", width * 0.18, height - 104, 0.82, { 1, 0.38, 0.32 })
  elseif selected and selected.implementation == "self_destruct" then
    self:_text("SELECT SELF-DESTRUCT, THEN PRESS ENTER TO ARM CONFIRMATION.", width * 0.18, height - 104, 0.78, { 0.75, 0.82, 0.92 })
  else
    self:_text("SELECT AN ABILITY, THEN PRESS ENTER TO ACTIVATE USING YOUR FACING.", width * 0.18, height - 104, 0.78, { 0.75, 0.82, 0.92 })
  end
  self:_text("W/S SELECT     ENTER ACTIVATE     X / ESC CANCEL", width * 0.18, height - 62, 0.82, { 0.75, 0.82, 0.92 })
end

function Renderer:_draw_inventory(app)
  local inventory = app.session.state.inventory
  local width, height = love.graphics.getDimensions()
  local layout = InventoryLayout.for_viewport(inventory, width, height)
  local cell, grid_x, grid_y = layout.cell, layout.grid_x, layout.grid_y
  local cursor = app.inventory_cursor or { x = 1, y = 1 }

  local function draw_item(entry, anchor_x, anchor_y, color, label_color)
    local cells = inventory:footprint_cells(entry.item, entry.rotated)
    for _, shape_cell in ipairs(cells) do
      local pixel_x = grid_x + (anchor_x - 1 + shape_cell.x) * cell + 2
      local pixel_y = grid_y + (anchor_y - 1 + shape_cell.y) * cell + 2
      self:_color(color)
      love.graphics.rectangle("fill", pixel_x, pixel_y, cell - 7, cell - 7)
      self:_color({ 0.8, 0.9, 1, color[4] or 1 })
      love.graphics.rectangle("line", pixel_x, pixel_y, cell - 7, cell - 7)
    end
    self:_text(entry.item.display_name, grid_x + (anchor_x - 1) * cell + 6, grid_y + (anchor_y - 1) * cell + 7,
      math.min(0.7, math.max(0.38, cell / 90)), label_color or { 0.95, 0.97, 1, color[4] or 1 })
  end

  love.graphics.clear(0.025, 0.035, 0.055)
  self:_text("CARRIED INVENTORY — PAUSED", grid_x, math.max(18, grid_y - 58), 1.35, { 0.7, 0.9, 1 })
  self:_text(inventory:total_mass() .. " MASS  •  " .. inventory:encumbrance(), grid_x, math.max(38, grid_y - 31), 0.72, { 0.95, 0.85, 0.3 })
  for y = 1, inventory.height do
    for x = 1, inventory.width do
      local pixel_x = grid_x + (x - 1) * cell
      local pixel_y = grid_y + (y - 1) * cell
      self:_color({ 0.055, 0.08, 0.12 })
      love.graphics.rectangle("fill", pixel_x, pixel_y, cell - 3, cell - 3)
      self:_color({ 0.18, 0.28, 0.38 })
      love.graphics.rectangle("line", pixel_x, pixel_y, cell - 3, cell - 3)
    end
  end
  for _, entry in ipairs(inventory.entries) do
    local dragging = app.inventory_drag and app.inventory_drag.physical_id == entry.physical_id
    if not dragging then
      local selected = app.inventory_selected_id == entry.physical_id
      draw_item(entry, entry.x, entry.y, selected and { 0.92, 0.7, 0.2 } or { 0.18, 0.45, 0.58 })
    end
  end
  local drag = app.inventory_drag
  if drag then
    local entry = inventory:get(drag.physical_id)
    if entry then
      local preview = { item = entry.item, rotated = drag.rotated }
      draw_item(preview, drag.x, drag.y,
        drag.valid and { 0.45, 0.92, 0.74, 0.8 } or { 1, 0.28, 0.22, 0.7 },
        drag.valid and { 0.9, 1, 0.94, 0.9 } or { 1, 0.82, 0.8, 0.9 })
    end
  end
  local cursor_x = grid_x + (cursor.x - 1) * cell
  local cursor_y = grid_y + (cursor.y - 1) * cell
  self:_color({ 1, 0.85, 0.2 })
  love.graphics.setLineWidth(3)
  love.graphics.rectangle("line", cursor_x - 2, cursor_y - 2, cell + 1, cell + 1)
  love.graphics.setLineWidth(1)

  local entry = app.inventory_selected_id and inventory:get(app.inventory_selected_id) or inventory:item_at(cursor.x, cursor.y)
  local detail_entry = entry
  if drag and entry and drag.physical_id == entry.physical_id then
    detail_entry = { item = entry.item, rotated = drag.rotated }
  end
  local detail_x = grid_x + math.floor(layout.width * 0.52)
  local detail_y = math.max(18, grid_y - 58)
  if detail_entry then
    local model = GameplayUI.inventory_entry(app.session, detail_entry, inventory)
    self:_text(model.name, detail_x, detail_y, 0.82, { 0.95, 0.85, 0.3 })
    self:_text("MASS " .. model.mass .. "   BOUNDS " .. model.width .. "×" .. model.height .. (model.rotated and " (ROTATED)" or ""), detail_x, detail_y + 17, 0.54)
    if model.item_type == "resource" or model.item_type == "ammo" then
      self:_text((model.category or "RESOURCE") .. " ×" .. model.quantity, detail_x, detail_y + 33, 0.58,
        model.item_type == "ammo" and { 0.95, 0.7, 0.35 } or { 0.6, 0.9, 0.75 })
    elseif model.item_type == "tool" then
      self:_text("DURABILITY " .. model.current_durability .. " / " .. model.max_durability, detail_x, detail_y + 33, 0.58)
      self:_text(model.condition .. "  •  " .. string.upper(model.family) .. " TOOL", detail_x, detail_y + 46, 0.52,
        model.functional and { 0.6, 0.9, 0.75 } or { 1, 0.35, 0.35 })
      self:_text("COMBAT " .. model.combat_damage .. "  •  TERRAIN " .. model.modification_damage, detail_x, detail_y + 59, 0.48,
        { 0.95, 0.72, 0.35 })
    else
      self:_text("INTEGRITY " .. model.current_integrity .. " / " .. model.max_integrity, detail_x, detail_y + 33, 0.58)
      self:_text(model.condition, detail_x, detail_y + 46, 0.58,
        model.functional and { 0.6, 0.9, 0.75 } or { 1, 0.35, 0.35 })
      if model.magazine then
        self:_text("MAG " .. model.magazine.loaded .. "/" .. model.magazine.capacity, detail_x, detail_y + 59, 0.52,
          { 0.95, 0.72, 0.35 })
      end
    end
  else
    self:_text("EMPTY CELL", detail_x, detail_y, 0.72, { 0.65, 0.75, 0.9 })
  end
  local drop = app:inventory_drop_target_layout(width, height)
  if drop then
    self:_color({ 0.22, 0.11, 0.1 })
    love.graphics.rectangle("fill", drop.x, drop.y, drop.width, drop.height)
    self:_color({ 0.9, 0.38, 0.3 })
    love.graphics.rectangle("line", drop.x, drop.y, drop.width, drop.height)
    self:_text("DROP SELECTED", drop.x + 12, drop.y + 7, 0.55, { 1, 0.72, 0.66 })
  end
  self:_text("DRAG TO REPACK  •  R ROTATE  •  DEL DROP  •  WASD / ARROWS MOVE CURSOR", grid_x, height - 58, 0.68, { 0.75, 0.82, 0.92 })
  self:_text(app:is_campaign_mode() and "TAB LOADOUT     ENTER SELECT / PLACE     I / ESC CLOSE" or "ENTER SELECT / PLACE     I / ESC CLOSE",
    grid_x, height - 36, 0.68, { 0.75, 0.82, 0.92 })
  local body_y = grid_y + layout.height + 10
  local body_x = grid_x
  self:_text("EQUIPPED BODY", body_x, body_y, 0.68, { 0.7, 0.9, 1 })
  for index, component in ipairs(GameplayUI.body(app.session, app.session.state.player)) do
    if body_y + index * 14 < height - 62 then
      self:_text((component.slot or "SLOT") .. "  " .. (component.empty and "EMPTY" or component.name) .. " — " .. component.condition,
        body_x, body_y + index * 14, 0.45, component.empty and { 0.48, 0.55, 0.65 }
          or (component.functional and { 0.75, 0.84, 0.94 } or { 1, 0.42, 0.35 }))
    end
  end
  if app:is_campaign_mode() then
    local loadout = app.session:campaign_loadout()
    local panel_x = grid_x + math.floor(layout.width * 0.55)
    local panel_y = body_y + 8
    local slots = {
      { kind = "weapon", index = 1, label = "WEAPON A" },
      { kind = "weapon", index = 2, label = "WEAPON B" },
      { kind = "ability", index = 1, label = "ABILITY A" },
      { kind = "ability", index = 2, label = "ABILITY B" },
    }
    self:_text("QUICK LOADOUT", panel_x, panel_y, 0.68, { 0.7, 0.9, 1 })
    for focus, slot in ipairs(slots) do
      local binding = slot.kind == "weapon" and loadout.weapon_slots[slot.index] or loadout.ability_slots[slot.index]
      local resolved = binding and select(1, Loadout.resolve(app.session, app.session.state.player, binding, slot.kind)) or nil
      local label = resolved and (resolved.display_name or (resolved.ability and resolved.ability.display_name))
      if not label and binding and binding.source_kind == "tool" and binding.tool_definition_id
        and app.session.registry.tools[binding.tool_definition_id] then
        label = app.session.registry:get_tool(binding.tool_definition_id).display_name
      end
      local active = slot.index == (slot.kind == "weapon" and loadout.active_weapon or loadout.active_ability)
      local selected = app.inventory_panel == "loadout" and focus == (app.loadout_focus or 1)
      local color = selected and { 0.95, 0.85, 0.3 } or active and { 0.6, 0.9, 0.75 } or { 0.72, 0.8, 0.92 }
      self:_text((selected and "> " or "  ") .. slot.label .. (active and " * " or "   ")
        .. string.upper(label or "EMPTY"), panel_x, panel_y + focus * 15, 0.5, color)
    end
    if app.inventory_panel == "loadout" then
      local target = app:loadout_target()
      local options = app:loadout_options()
      local option_y = panel_y + 82
      self:_text("ASSIGN " .. target.label, panel_x, option_y, 0.56, { 0.95, 0.85, 0.3 })
      for index, option in ipairs(options) do
        if option_y + index * 14 < height - 34 then
          local selected = index == (app.loadout_selection or 1)
          local color = selected and { 0.95, 0.85, 0.3 }
            or option.available and { 0.75, 0.84, 0.94 } or { 1, 0.42, 0.35 }
          local durability = option.current_durability and (" " .. option.current_durability .. "/" .. option.max_durability) or ""
          self:_text((selected and "> " or "  ") .. string.upper(option.display_name) .. " — "
            .. string.upper(option.provider_name) .. " " .. string.upper(option.condition) .. durability,
            panel_x, option_y + index * 14, 0.44, color)
        end
      end
      self:_text("A/D SLOT  W/S ACTION  ENTER ASSIGN  TAB CLOSE", panel_x, height - 20, 0.44, { 0.75, 0.82, 0.92 })
    end
    -- The assignment list may be taller than the panel on smaller windows.
    -- Keep build readability on the normal paused Inventory surface instead
    -- of drawing it on top of a selectable loadout option.
    local effects = app.inventory_panel ~= "loadout" and GameplayUI.build_effects(app.session) or {}
    local effects_y = panel_y + 86
    if effects_y < height - 56 then
      self:_text("BUILD EFFECTS", panel_x, effects_y, 0.62, { 0.7, 0.9, 1 })
      for index, effect in ipairs(effects) do
        if effects_y + index * 23 < height - 34 then
          local color = effect.active and { 0.6, 0.9, 0.75 } or { 1, 0.55, 0.34 }
          self:_text((effect.active and "ACTIVE " or "INACTIVE ") .. string.upper(effect.charm_name),
            panel_x, effects_y + index * 23, 0.43, color)
          self:_text(effect.active and effect.description or effect.reason,
            panel_x, effects_y + index * 23 + 9, 0.34, { 0.72, 0.8, 0.9 })
        end
      end
    end
  end
end

function Renderer:_draw_salvage(app)
  local session = app.session
  local corpse = session:find_corpse(app.salvage_corpse_id)
  local options = app:salvage_options()
  local width, height = love.graphics.getDimensions()
  if session.campaign then
    local projection = app:salvage_grid()
    local layout = projection and app:salvage_layout(width, height) or nil
    love.graphics.clear(0.025, 0.035, 0.055)
    self:_text("CORPSE SALVAGE", 28, 24, 1.35, { 0.7, 0.9, 1 })
    self:_text("PAUSED — TRANSFERS DO NOT ADVANCE TIME", 28, 43, 0.46, { 0.68, 0.78, 0.9 })
    if not corpse or not projection or not layout then
      self:_text("CORPSE NO LONGER AVAILABLE", 28, 74, 0.9, { 1, 0.4, 0.4 })
      return
    end
    local source = corpse.source_kind and session.registry.enemies["enemy.legacy." .. corpse.source_kind]
    local label = corpse.source_kind == "player" and "FALLEN BODY"
      or (corpse.fallen_archive_id and "FALLEN SHELL")
      or ((source and source.display_name or "ENEMY") .. " REMAINS")
    self:_text(label, 28, 51, 0.64, { 0.95, 0.85, 0.3 })

    local function draw_grid(title, inventory, geometry, base_color, focus, cursor, hide_id)
      self:_text(title, geometry.grid_x, geometry.grid_y - 27, 0.72,
        focus and { 0.95, 0.85, 0.3 } or { 0.65, 0.78, 0.92 })
      for y = 1, inventory.height do
        for x = 1, inventory.width do
          local pixel_x = geometry.grid_x + (x - 1) * layout.cell
          local pixel_y = geometry.grid_y + (y - 1) * layout.cell
          self:_color({ 0.055, 0.08, 0.12 })
          love.graphics.rectangle("fill", pixel_x, pixel_y, layout.cell - 2, layout.cell - 2)
          self:_color({ 0.18, 0.28, 0.38 })
          love.graphics.rectangle("line", pixel_x, pixel_y, layout.cell - 2, layout.cell - 2)
        end
      end
      for _, entry in ipairs(inventory.entries) do
        if entry.physical_id ~= hide_id then
          local cells = inventory:footprint_cells(entry.item, entry.rotated)
          local selected = app.salvage_selected_id == entry.physical_id
          for _, shape_cell in ipairs(cells) do
            local pixel_x = geometry.grid_x + (entry.x - 1 + shape_cell.x) * layout.cell + 2
            local pixel_y = geometry.grid_y + (entry.y - 1 + shape_cell.y) * layout.cell + 2
            self:_color(selected and { 0.92, 0.7, 0.2 } or base_color)
            love.graphics.rectangle("fill", pixel_x, pixel_y, layout.cell - 6, layout.cell - 6)
            self:_color({ 0.8, 0.9, 1 })
            love.graphics.rectangle("line", pixel_x, pixel_y, layout.cell - 6, layout.cell - 6)
          end
          self:_text(entry.item.display_name, geometry.grid_x + (entry.x - 1) * layout.cell + 3,
            geometry.grid_y + (entry.y - 1) * layout.cell + 4, math.max(0.27, math.min(0.48, layout.cell / 75)),
            { 0.95, 0.97, 1 })
        end
      end
      if cursor then
        local pixel_x = geometry.grid_x + (cursor.x - 1) * layout.cell
        local pixel_y = geometry.grid_y + (cursor.y - 1) * layout.cell
        self:_color(focus and { 1, 0.85, 0.2 } or { 0.4, 0.6, 0.78 })
        love.graphics.setLineWidth(2)
        love.graphics.rectangle("line", pixel_x - 1, pixel_y - 1, layout.cell, layout.cell)
        love.graphics.setLineWidth(1)
      end
    end

    local cursors = app.salvage_cursor or { corpse = { x = 1, y = 1 }, player = { x = 1, y = 1 } }
    local drag = app.salvage_drag
    draw_grid("FALLEN CARGO", projection.inventory, layout.corpse, { 0.36, 0.43, 0.7 }, app.salvage_focus == "corpse",
      cursors.corpse, drag and drag.physical_id)
    draw_grid("YOUR INVENTORY", session.state.inventory, layout.player, { 0.18, 0.45, 0.58 }, app.salvage_focus == "player",
      cursors.player, nil)
    if drag and drag.x and drag.y then
      local entry = projection.inventory:get(drag.physical_id)
      if entry then
        local color = drag.valid and { 0.45, 0.92, 0.74, 0.8 } or { 1, 0.28, 0.22, 0.7 }
        for _, shape_cell in ipairs(session.state.inventory:footprint_cells(entry.item, drag.rotated)) do
          local pixel_x = layout.player.grid_x + (drag.x - 1 + shape_cell.x) * layout.cell + 2
          local pixel_y = layout.player.grid_y + (drag.y - 1 + shape_cell.y) * layout.cell + 2
          self:_color(color)
          love.graphics.rectangle("fill", pixel_x, pixel_y, layout.cell - 6, layout.cell - 6)
        end
      end
    end
    local detail = drag and projection.inventory:get(drag.physical_id)
      or (app.salvage_selected_id and projection.inventory:get(app.salvage_selected_id))
      or projection.inventory:item_at(cursors.corpse.x, cursors.corpse.y)
    if detail then
      local model = GameplayUI.inventory_entry(session, { item = detail.item, rotated = drag and drag.rotated or detail.rotated }, projection.inventory)
      local details_y = math.max(62, layout.corpse.grid_y - 50)
      self:_text(string.upper(model.category or "COMPONENT") .. "  " .. string.upper(model.name), width * 0.47, details_y, 0.56,
        { 0.95, 0.85, 0.3 })
      self:_text("MASS " .. model.mass .. "  •  " .. model.width .. "×" .. model.height
        .. (((drag and drag.rotated) or model.rotated) and " ROTATED" or ""), width * 0.47, details_y + 15, 0.45,
        { 0.75, 0.82, 0.92 })
      if model.item_type == "resource" or model.item_type == "ammo" then
        self:_text((model.category or "RESOURCE") .. " ×" .. model.quantity, width * 0.47, details_y + 29, 0.45,
          { 0.6, 0.9, 0.75 })
      elseif model.item_type == "tool" then
        self:_text("DURABILITY " .. model.condition .. "  " .. model.current_durability .. "/" .. model.max_durability,
          width * 0.47, details_y + 29, 0.45, model.functional and { 0.6, 0.9, 0.75 } or { 1, 0.4, 0.35 })
      else
        self:_text("CONDITION " .. model.condition .. "  " .. model.current_integrity .. "/" .. model.max_integrity,
          width * 0.47, details_y + 29, 0.45, model.functional and { 0.6, 0.9, 0.75 } or { 1, 0.4, 0.35 })
        if model.magazine then self:_text("MAG " .. model.magazine.loaded .. "/" .. model.magazine.capacity,
          width * 0.47, details_y + 43, 0.45, { 0.95, 0.72, 0.35 }) end
      end
    else
      self:_text("SELECT A CORPSE ITEM", width * 0.47, math.max(62, layout.corpse.grid_y - 50), 0.55, { 0.65, 0.75, 0.9 })
    end
    self:_text("DRAG CORPSE CARGO TO YOUR GRID  •  R ROTATE  •  TAB SWITCH GRID", 28, height - 52, 0.58,
      { 0.75, 0.82, 0.92 })
    self:_text("ENTER SELECT / PLACE  •  U / ESC CLOSE  •  SALVAGE PAUSES THE WORLD", 28, height - 31, 0.56,
      { 0.75, 0.82, 0.92 })
    return
  end
  love.graphics.clear(0.025, 0.035, 0.055)
  self:_text("CORPSE SALVAGE", width * 0.18, 58, 2, { 0.7, 0.9, 1 })
  if not corpse then
    self:_text("CORPSE NO LONGER AVAILABLE", width * 0.18, 136, 1, { 1, 0.4, 0.4 })
  elseif #options == 0 then
    self:_text("NO SALVAGEABLE COMPONENTS REMAIN", width * 0.18, 136, 1, { 0.72, 0.76, 0.84 })
  else
    local source = corpse.source_kind and session.registry.enemies["enemy.legacy." .. corpse.source_kind]
    local label = corpse.source_kind == "player" and "FALLEN BODY"
      or (corpse.fallen_archive_id and "FALLEN SHELL")
      or ((source and source.display_name or "ENEMY") .. " REMAINS")
    self:_text(label, width * 0.18, 101, 0.85, { 0.95, 0.85, 0.3 })
    for index, installed in ipairs(options) do
      local y = 136 + (index - 1) * 102
      local selected = index == app.menu
      self:_color(selected and { 0.13, 0.22, 0.3 } or { 0.06, 0.08, 0.12 })
      love.graphics.rectangle("fill", width * 0.18, y, width * 0.64, 88)
      if installed.item and installed.item.item_type == "resource_stack" then
        local placement = session.state.inventory:find_first_fit(installed.item)
        self:_text((selected and "> " or "  ") .. installed.item.display_name, width * 0.21, y + 8, 1.1,
          selected and { 0.95, 0.85, 0.3 } or { 1, 1, 1 })
        self:_text("CARRIED RESOURCE  •  MASS " .. installed.item.mass .. "  •  1×1", width * 0.21, y + 35, 0.72, { 0.72, 0.76, 0.84 })
        self:_text(placement and "FITS INVENTORY" or "NO INVENTORY SPACE", width * 0.67, y + 9, 0.72,
          placement and { 0.6, 0.9, 0.75 } or { 1, 0.4, 0.4 })
      else
        local model = GameplayUI.salvage(session, installed)
        local component = model.component
        self:_text((selected and "> " or "  ") .. component.name, width * 0.21, y + 8, 1.1,
          selected and { 0.95, 0.85, 0.3 } or { 1, 1, 1 })
        self:_text(component.slot .. "  •  " .. component.current_integrity .. "/" .. component.max_integrity
          .. " " .. component.condition .. "  •  MASS " .. component.mass .. "  •  " .. component.width .. "×" .. component.height,
          width * 0.21, y + 33, 0.72, { 0.72, 0.76, 0.84 })
        self:_text("ABILITY " .. component.ability_text, width * 0.21, y + 53, 0.66, { 0.95, 0.72, 0.35 })
        if model.current then
          self:_text("CURRENT " .. model.current.name .. " " .. model.current.current_integrity .. "/" .. model.current.max_integrity,
            width * 0.21, y + 69, 0.59, { 0.6, 0.72, 0.84 })
        end
        self:_text(model.fit_text, width * 0.67, y + 9, 0.72,
          model.fits and { 0.6, 0.9, 0.75 } or { 1, 0.4, 0.4 })
      end
    end
  end
  self:_text("W/S SELECT     ENTER SALVAGE     G / ESC CLOSE", width * 0.18, height - 58, 0.85, { 0.75, 0.82, 0.92 })
end

function Renderer:_draw_storage(app)
  local width, height = love.graphics.getDimensions()
  local player_entries = app.session.state.inventory.entries
  local storage = app:storage_inventory()
  love.graphics.clear(0.025, 0.035, 0.055)
  self:_text("STORAGE", width * 0.15, 48, 2, { 0.7, 0.9, 1 })
  self:_text("PAUSED — TRANSFERS DO NOT ADVANCE TIME", width * 0.15, 78, 0.58, { 0.68, 0.78, 0.9 })
  local function column(title, entries, x, selected)
    self:_text(title, x, 108, 1.15, selected and { 0.95, 0.85, 0.3 } or { 0.65, 0.75, 0.9 })
    if #entries == 0 then self:_text("EMPTY", x, 145, 0.8, { 0.48, 0.55, 0.65 }) end
    for index, entry in ipairs(entries) do
      local active = selected and index == app.storage_index
      self:_text((active and "> " or "  ") .. entry.item.display_name, x, 145 + (index - 1) * 27, 0.78,
        active and { 0.95, 0.85, 0.3 } or { 0.88, 0.92, 1 })
    end
  end
  column("PLAYER", player_entries, width * 0.15, app.storage_focus == "player")
  column("STORAGE", storage and storage.entries or {}, width * 0.55, app.storage_focus == "storage")
  self:_text("TAB SWITCH     W/S SELECT     ENTER TRANSFER     U / ESC CLOSE", width * 0.15, height - 58, 0.78, { 0.75, 0.82, 0.92 })
end

function Renderer:_menu(title, items, selected, footer)
  love.graphics.clear(0.025, 0.035, 0.055)
  local width, height = love.graphics.getDimensions()
  self:_text(title, width / 2 - #title * 8, 70, 2, { 0.7, 0.9, 1 })
  local first_y, footer_y = 138, height - 58
  local card_height, gap = 58, 12
  local visible = math.max(1, math.floor((footer_y - first_y) / (card_height + gap)))
  local first = math.max(1, math.min(math.max(1, #items - visible + 1), selected - math.floor(visible / 2)))
  local last = math.min(#items, first + visible - 1)
  for index = first, last do
    local item = items[index]
    local y = first_y + (index - first) * (card_height + gap)
    local is_selected = index == selected
    self:_color(is_selected and { 0.13, 0.22, 0.3 } or { 0.06, 0.08, 0.12 })
    love.graphics.rectangle("fill", width * 0.18, y, width * 0.64, card_height)
    self:_text((is_selected and "> " or "  ") .. self:menu_item_label(item), width * 0.21, y + 7, 1.05,
      is_selected and { 0.95, 0.85, 0.3 } or { 1, 1, 1 })
    local description = item.description or ""
    if #description > 86 then description = description:sub(1, 83) .. "..." end
    self:_text(description, width * 0.21, y + 31, 0.7, { 0.72, 0.76, 0.84 })
  end
  if #items > visible then self:_text(first .. "–" .. last .. " / " .. #items, width * 0.78, 103, 0.7, { 0.65, 0.75, 0.9 }) end
  self:_text(footer or "W/S SELECT     ENTER CONFIRM", width / 2 - 150, height - 52, 1, { 0.65, 0.75, 0.9 })
end

function Renderer:_draw_title(app)
  love.graphics.clear(0.025, 0.035, 0.055)
  local width, height = love.graphics.getDimensions()
  local screen, accent = self:_screen_definition(app, "title")
  local title = self:_screen_text(app, "title", "title", "ROAG")
  local subtitle = self:_screen_text(app, "title", "subtitle", "A ONE-BIT DESCENT")
  self:_text(title, width / 2 - #title * 13, height / 2 - 100, 4, accent)
  self:_text(subtitle, width / 2 - #subtitle * 4.5, height / 2 - 34, 1.2, { 0.7, 0.75, 0.85 })
  local options = app:title_options()
  for index, option in ipairs(options) do
    self:_text((index == app.menu and "> " or "  ") .. option.name, width / 2 - 68, height / 2 + 26 + index * 29,
      1, index == app.menu and { 0.95, 0.85, 0.3 } or { 0.78, 0.83, 0.9 })
  end
  local message = app.death_archive_error and "FALLEN ARCHIVE WRITE FAILED — DEAD RUN RETAINED"
    or (app.archive_error and "FALLEN ARCHIVE UNAVAILABLE — RUN SAVES REMAIN SAFE")
    or (app.meta_error and "RESEARCH PROFILE UNAVAILABLE — RUN SAVES REMAIN SAFE")
    or (app.screen_definition_error and "SCREEN DEFINITIONS INVALID — USING SAFE FALLBACK")
    or (app.presentation_flow_error and "PRESENTATION FLOW INVALID — USING SAFE FALLBACK")
    or (app.art_pack_config_error and "PRESENTATION ART PACK INVALID — USING DEFAULT")
    or (app.title_error and "SAVE UNAVAILABLE — START A NEW RUN" or (screen and screen.footer or "W/S SELECT     ENTER CONFIRM"))
  -- Keep title feedback below the longest normal menu so valid title states
  -- remain screenshot-readable as presentation options are added.
  self:_text(message, width / 2 - #message * 4, height - 72, 0.78, { 0.75, 0.82, 0.92 })
end

function Renderer:_draw_expedition_character_select(app)
  local items = {}
  for _, option in ipairs(app:expedition_character_options_list()) do
    local definition = option.definition
    local state = option.unlocked and "READY" or ("LOCKED — " .. (definition.unlock_description or "KEEP EXPLORING"))
    items[#items + 1] = {
      name = definition.display_name .. "  [" .. state .. "]",
      description = "HP " .. definition.base_hp .. "  •  " .. definition.description,
    }
  end
  self:_menu("CHOOSE EXPEDITION CHARACTER", items, app.menu, "W/S SELECT     ENTER START     ESC TITLE")
end

function Renderer:_draw_expedition_reward(app)
  local items = {}
  for _, passive in ipairs(app.expedition and app.expedition.pending_reward or {}) do
    local current = app.session.state.expedition.passive_stacks[passive.id] or 0
    items[#items + 1] = {
      name = passive.name .. "  ×" .. current .. " → ×" .. (current + 1),
      description = passive.description,
    }
  end
  self:_menu("CHOOSE A PASSIVE", items, app.menu, "W/S SELECT     ENTER TAKE")
end

function Renderer:_draw_expedition_chest(app)
  local chest = app.expedition and app.expedition.pending_chest or {}
  local reward = chest.options and chest.options[1]
  local currency = app.session and app.session.state.expedition and app.session.state.expedition.currency or 0
  local description = reward and (reward.name .. " — " .. reward.description) or "Random passive item"
  self:_menu("PAID CACHE — " .. tostring(chest.cost or "?") .. " SCRAP", {
    { name = "OPEN CACHE", description = description .. "  •  HAVE " .. currency .. " SCRAP" },
    { name = "LEAVE CACHE", description = "Keep your SCRAP and enter the next encounter." },
  }, 1, "ENTER OPEN     ESC / X LEAVE")
end

function Renderer:_draw_expedition_build(app)
  love.graphics.clear(0.025, 0.035, 0.055)
  local width, height = love.graphics.getDimensions()
  local summary = app:expedition_build_summary() or {}
  self:_text("RUN BUILD", 42, 34, 2, { 0.7, 0.9, 1 })
  self:_text((summary.character and summary.character.display_name or "UNKNOWN") .. "  •  STAGE " .. tostring(summary.stage or 1)
    .. "  •  ENCOUNTER " .. tostring(summary.encounter or 0), 42, 68, 0.76, { 0.72, 0.8, 0.92 })
  self:_text("SCRAP " .. tostring(summary.currency or 0), 42, 92, 0.9, { 0.65, 0.9, 0.8 })
  self:_text("WEAPON: " .. string.upper(summary.weapon and summary.weapon.display_name or "OFFLINE"), 42, 126, 0.8, { 0.95, 0.85, 0.3 })
  self:_text("ABILITY: " .. string.upper(summary.ability and summary.ability.display_name or "OFFLINE"), 42, 148, 0.8, { 0.95, 0.65, 0.35 })
  self:_text("PASSIVES", 42, 190, 0.95, { 0.95, 0.85, 0.3 })
  local y = 216
  for _, passive in ipairs(summary.passives or {}) do
    self:_text(passive.display_name .. " ×" .. passive.count, 58, y, 0.78, { 0.82, 0.88, 0.98 })
    self:_text(passive.description, 58, y + 15, 0.56, { 0.62, 0.72, 0.84 })
    y = y + 42
    if y > height - 68 then break end
  end
  if #(summary.passives or {}) == 0 then self:_text("NO PASSIVES YET — CLEAR ENCOUNTERS.", 58, y, 0.72, { 0.62, 0.72, 0.84 }) end
  self:_text("I / ENTER / ESC RETURN TO COMBAT", 42, height - 42, 0.78, { 0.75, 0.82, 0.92 })
end

function Renderer:_draw_expedition_summary(app)
  local summary = app.expedition and app.expedition.summary_data or {}
  local title = summary.victory and "EXPEDITION COMPLETE" or "EXPEDITION LOST"
  local items = {
    { name = summary.character or "UNKNOWN", description = "STAGE " .. tostring(summary.stage or 1) .. " • ENCOUNTER " .. tostring(summary.encounter or 0) },
    { name = "KILLS " .. tostring(summary.kills or 0) .. "   COMPONENT BREAKS " .. tostring(summary.component_breaks or 0),
      description = "TOP PASSIVES: " .. table.concat((function()
        local labels = {}
        for index = 1, math.min(3, #(summary.passive_stacks or {})) do
          local entry = summary.passive_stacks[index]
          labels[#labels + 1] = entry.display_name .. " ×" .. entry.count
        end
        return labels
      end)(), ", ") },
  }
  if #(summary.new_unlocks or {}) > 0 then
    items[#items + 1] = { name = "NEW UNLOCKS", description = table.concat(summary.new_unlocks, ", ") }
  end
  self:_menu(title, items, 1, "ENTER RETURN TO TITLE")
end

function Renderer:_draw_help(app)
  love.graphics.clear(0.025, 0.035, 0.055)
  local width, height = love.graphics.getDimensions()
  local screen, accent = self:_screen_definition(app, "help")
  self:_text(self:_screen_text(app, "help", "title", "HOW TO PLAY"), 42, 34, 2, accent)
  self:_text(self:_screen_text(app, "help", "subtitle", "THE BODY IS TEMPORARY."), 42, 68, 0.72, { 0.68, 0.76, 0.88 })
  local y = 112
  for _, section in ipairs(app:help_sections()) do
    self:_text(section.title, 58, y, 0.92, { 0.95, 0.85, 0.3 })
    local text = section.text
    if #text > 118 then text = text:sub(1, 115) .. "..." end
    self:_text(text, 58, y + 20, 0.68, { 0.76, 0.83, 0.93 })
    y = y + 70
    if y > height - 84 then break end
  end
  self:_text(self:_screen_text(app, "help", "footer", "ENTER / ESC TITLE"), 42, height - 42, 0.8, { 0.75, 0.82, 0.92 })
end

function Renderer:_draw_onboarding(app)
  love.graphics.clear(0.025, 0.035, 0.055)
  local width, height = love.graphics.getDimensions()
  local screen, accent = self:_screen_definition(app, "onboarding")
  self:_text(self:_screen_text(app, "onboarding", "title", "YOUR BODY IS TEMPORARY"), width * 0.18, height * 0.2, 2.15, accent)
  local y = height * 0.36
  for index, line in ipairs(app:onboarding_sections()) do
    self:_text(line, width * 0.18, y + (index - 1) * 44, index == 1 and 1.05 or 0.76,
      index == 1 and { 0.95, 0.85, 0.3 } or { 0.76, 0.84, 0.94 })
  end
  self:_text(self:_screen_text(app, "onboarding", "footer", "ENTER BEGIN DESCENT     ESC TITLE"), width * 0.18, height - 68, 0.82, { 0.75, 0.82, 0.92 })
end

function Renderer:_draw_campaign_succession(app)
  local width, height = love.graphics.getDimensions()
  local notice = app.campaign_succession_notice or {}
  love.graphics.clear(0.025, 0.035, 0.055)
  local screen, accent = self:_screen_definition(app, "campaign_succession")
  self:_text(self:_screen_text(app, "campaign_succession", "title", "BODY LOST"), width * 0.16, 54, 2.25, accent)
  self:_text(self:_screen_text(app, "campaign_succession", "subtitle", "SUCCESSOR RECONSTRUCTED AT THE ACTIVE ANCHOR"),
    width * 0.16, 91, 0.78, { 0.68, 0.78, 0.9 })

  local cards = {
    { title = "DEATH SITE", text = notice.death_location or "UNKNOWN LOCATION", color = { 1, 0.48, 0.4 } },
    { title = "CURRENT RECONSTRUCTION ANCHOR", text = notice.anchor_location or "UNKNOWN LOCATION", color = { 0.48, 0.92, 1 } },
  }
  for index, card in ipairs(cards) do
    local y = 142 + (index - 1) * 74
    self:_color({ 0.06, 0.1, 0.15 })
    love.graphics.rectangle("fill", width * 0.16, y, width * 0.68, 58)
    self:_color(card.color)
    love.graphics.rectangle("line", width * 0.16, y, width * 0.68, 58)
    self:_text(card.title, width * 0.18, y + 8, 0.64, card.color)
    self:_text(card.text, width * 0.18, y + 29, 0.75, { 0.9, 0.94, 1 })
  end
  self:_text(notice.summary or "A FRESH BODY WAS RECONSTRUCTED AT YOUR ACTIVE ANCHOR.", width * 0.16, 310, 0.78, { 0.95, 0.85, 0.3 })
  self:_text(notice.recovery or "YOUR LOST BODY AND CARGO REMAIN AT THE DEATH SITE.", width * 0.16, 346, 0.68, { 0.78, 0.85, 0.94 })
  self:_text(notice.loss or "BASES, STORAGE, SCRAP, AND WORLD CHANGES PERSIST.", width * 0.16, 374, 0.68, { 0.78, 0.85, 0.94 })
  self:_text(notice.guidance or "FACE A RECONSTRUCTION STATION AND USE U TO SET A DIFFERENT FUTURE ANCHOR.",
    width * 0.16, 426, 0.68, { 0.6, 0.9, 0.75 })
  self:_text(self:_screen_text(app, "campaign_succession", "footer", "ENTER / ESC CONTINUE"),
    width * 0.16, height - 58, 0.82, { 0.75, 0.82, 0.92 })
end

function Renderer:_draw_fallen_archive(app)
  love.graphics.clear(0.025, 0.035, 0.055)
  local width, height = love.graphics.getDimensions()
  local entries = app:fallen_archive_entries()
  local screen, accent = self:_screen_definition(app, "fallen_archive")
  self:_text(self:_screen_text(app, "fallen_archive", "title", "FALLEN ARCHIVE"), 42, 34, 2, accent)
  self:_text(self:_screen_text(app, "fallen_archive", "subtitle", "HISTORICAL BODIES — NOT A LIVE INVENTORY"), 42, 68, 0.72, { 0.68, 0.76, 0.88 })
  if #entries == 0 then
    self:_text("NO BODIES HAVE BEEN ARCHIVED", 42, 106, 1.05, { 0.72, 0.78, 0.88 })
    self:_text("A DEAD BODY MAY RETURN IN A FUTURE DESCENT.", 42, 138, 0.78, { 0.55, 0.64, 0.75 })
  else
    for index, entry in ipairs(entries) do
      local y, selected = 100 + (index - 1) * 62, index == app.menu
      self:_color(selected and { 0.13, 0.22, 0.3 } or { 0.06, 0.08, 0.12 })
      love.graphics.rectangle("fill", 36, y, width * 0.5, 52)
      self:_text((selected and "> " or "  ") .. entry.name, 50, y + 7, 0.9,
        selected and { 0.95, 0.85, 0.3 } or { 0.82, 0.86, 0.94 })
      self:_text(entry.description, 50, y + 29, 0.7, entry.compatible and { 0.58, 0.9, 0.76 } or { 1, 0.45, 0.35 })
    end
    local selected = app:current_fallen_entry()
    if selected then
      local record, x, y = selected.record, width * 0.6, 108
      local run_number = tostring(record.source_run_id or ""):match("(%d+)$") or "?"
      local biome = tostring(record.metadata.biome_id or "UNKNOWN"):gsub("^biome%.legacy%.", ""):gsub("_", " "):upper()
      self:_text("FALLEN RUN " .. run_number, x, y, 1.05, { 0.95, 0.85, 0.3 })
      self:_text("RECURRENCE " .. (selected.compatible and "READY" or "UNAVAILABLE"), x, y + 27, 0.78,
        selected.compatible and { 0.58, 0.9, 0.76 } or { 1, 0.45, 0.35 })
      self:_text("BIOME " .. biome, x, y + 50, 0.78, { 0.72, 0.78, 0.88 })
      self:_text("DEPTH " .. tostring(record.metadata.route_depth or "?"), x, y + 72, 0.78, { 0.72, 0.78, 0.88 })
      local line = y + 108
      self:_text("BODY", x, line, 0.9, { 0.7, 0.9, 1 })
      for _, slot in ipairs(record.body.slots or {}) do
        local definition = slot.component and app.registry:get_component(slot.component.definition_id) or nil
        local value = definition and (GameplayUI.slot_label(slot.slot_id) .. ": " .. definition.display_name .. "  "
          .. slot.component.current_integrity .. "/" .. slot.component.max_integrity .. " "
          .. GameplayUI.condition_label(slot.component)) or (GameplayUI.slot_label(slot.slot_id) .. ": EMPTY")
        self:_text(value, x, line + 24, 0.68, slot.component and { 0.78, 0.83, 0.92 } or { 0.48, 0.55, 0.65 })
        line = line + 24
      end
      if #(record.metadata.charm_ids or {}) > 0 then
        local charms = {}
        for _, charm_id in ipairs(record.metadata.charm_ids) do
          local charm = app.registry.charms[charm_id]
          charms[#charms + 1] = charm and charm.display_name or "UNKNOWN CHARM"
        end
        self:_text("CHARMS: " .. table.concat(charms, ", "), x, line + 10, 0.67, { 0.66, 0.73, 0.84 })
        line = line + 22
      end
      if #(record.metadata.route_path or {}) > 0 then
        self:_text("ROUTE NODES COMPLETED: " .. #record.metadata.route_path, x, line + 10, 0.62, { 0.58, 0.67, 0.78 })
      end
    end
  end
  self:_text((screen and screen.footer) or "W/S SELECT     ESC TITLE", 42, height - 42, 0.8, { 0.75, 0.82, 0.92 })
end

function Renderer:_draw_research(app)
  love.graphics.clear(0.025, 0.035, 0.055)
  local width, height = love.graphics.getDimensions()
  local category = app:current_research_category()
  local categories = app:research_categories()
  local options = app:research_options(category)
  local screen, accent = self:_screen_definition(app, "research")
  self:_text(self:_screen_text(app, "research", "title", "RESEARCH"), 44, 34, 2, accent)
  self:_text(self:_screen_text(app, "research", "subtitle", "PERMANENT ACCOUNT PROGRESSION"), 44, 60, 0.68, { 0.68, 0.76, 0.88 })
  self:_text("RESEARCH DATA: " .. app.meta_profile.research_data, 44, 72, 1.05, { 0.95, 0.85, 0.3 })
  self:_text(app.continue_available and "PURCHASES APPLY TO FUTURE RUNS" or "UNLOCKS APPLY TO YOUR NEXT RUN", 44, 98, 0.78, { 0.72, 0.78, 0.88 })
  for index, value in ipairs(categories) do
    local selected = index == app.research_category_index
    self:_text((selected and "> " or "  ") .. string.upper(value), 44, 145 + (index - 1) * 29, 0.92,
      selected and { 0.95, 0.85, 0.3 } or { 0.72, 0.78, 0.88 })
  end
  -- Persistent discoveries live beside research rather than becoming another
  -- title-screen branch. The compact ledger exposes account progress without
  -- revealing the name or description of content the player has not found.
  local history = app:discovery_history()
  local discovered = 0
  for _, entry in ipairs(history) do if entry.discovered then discovered = discovered + 1 end end
  local history_x, history_y, history_width = 38, 326, math.max(230, width * 0.21)
  self:_color({ 0.045, 0.07, 0.1 })
  love.graphics.rectangle("fill", history_x, history_y, history_width, math.min(height - history_y - 58, 292))
  self:_color({ 0.16, 0.3, 0.37 })
  love.graphics.rectangle("line", history_x, history_y, history_width, math.min(height - history_y - 58, 292))
  self:_text("DISCOVERIES " .. discovered .. " / " .. #history, history_x + 10, history_y + 9, 0.72, { 0.55, 0.9, 0.82 })
  for index, entry in ipairs(history) do
    local y = history_y + 34 + (index - 1) * 31
    if y + 25 < height - 48 then
      local biome = (entry.biome_id or "UNKNOWN"):gsub("^biome%.legacy%.", "")
      local title = entry.discovered and entry.name or "???"
      local detail = entry.discovered and entry.description or "UNRECOVERED"
      if #detail > 46 then detail = detail:sub(1, 43) .. "..." end
      self:_text(title, history_x + 10, y, 0.61, entry.discovered and { 0.78, 0.88, 0.96 } or { 0.48, 0.55, 0.64 })
      self:_text((entry.discovered and string.upper(biome) .. " — " or "") .. detail, history_x + 10, y + 14, 0.46,
        entry.discovered and { 0.55, 0.69, 0.77 } or { 0.38, 0.43, 0.5 })
    end
  end
  local start_y = 146
  for index, option in ipairs(options) do
    local selected = index == app.research_node_index
    local state = option.unlocked and "UNLOCKED" or (option.available and (option.cost .. " DATA") or "PREREQUISITE LOCKED")
    local tint = option.unlocked and { 0.45, 0.9, 0.67 } or (option.available and { 0.95, 0.85, 0.3 } or { 0.56, 0.62, 0.72 })
    local y = start_y + (index - 1) * 62
    self:_color(selected and { 0.13, 0.22, 0.3 } or { 0.06, 0.08, 0.12 })
    love.graphics.rectangle("fill", width * 0.28, y, width * 0.62, 52)
    self:_text((selected and "> " or "  ") .. option.name, width * 0.3, y + 7, 0.95, tint)
    self:_text(option.description, width * 0.3, y + 27, 0.69, { 0.72, 0.78, 0.88 })
    self:_text(state, width * 0.79, y + 8, 0.69, tint)
  end
  local selected = options[app.research_node_index or 1]
  if selected then
    local prerequisite = #selected.prerequisite_names > 0 and ("REQUIRES " .. table.concat(selected.prerequisite_names, ", ")) or "NO PREREQUISITE"
    self:_text(prerequisite, width * 0.3, math.min(height - 70, start_y + #options * 62 + 8), 0.62, { 0.62, 0.72, 0.84 })
  end
  local footer = app.meta_error and ("PROFILE ERROR: " .. tostring(app.meta_error.reason)) or (screen and screen.footer or "A/D CATEGORY     W/S NODE     ENTER PURCHASE     ESC TITLE")
  self:_text(footer, 44, height - 42, 0.78, { 0.75, 0.82, 0.92 })
end

-- Route state is authoritative data from the session graph.  This renderer
-- only lays out compact cards and edges; it never decides availability or
-- progression, keeping the map safe to rebuild after save/load.
function Renderer:_draw_route(app)
  local session, route = app.session, app.session.state.route
  local width, height = love.graphics.getDimensions()
  love.graphics.clear(0.025, 0.035, 0.055)
  local screen, accent = self:_screen_definition(app, "route")
  local title = self:_screen_text(app, "route", "title", "CHOOSE YOUR DESCENT")
  local subtitle = self:_screen_text(app, "route", "subtitle", "THE ROUTE IS ONE WAY")
  self:_text(title, width * 0.5 - #title * 5, 42, 1.7, accent)
  self:_text(subtitle, width * 0.5 - #subtitle * 3.2, 77, 0.8, { 0.7, 0.76, 0.86 })

  local by_depth, depth_count = {}, 0
  for _, id in ipairs(route.node_order) do
    local node = route:node(id)
    by_depth[node.depth] = by_depth[node.depth] or {}
    by_depth[node.depth][#by_depth[node.depth] + 1] = node
    depth_count = math.max(depth_count, node.depth)
  end
  local positions = {}
  local margin_x, top, bottom = 110, 142, height - 112
  for depth = 1, depth_count do
    local nodes = by_depth[depth] or {}
    table.sort(nodes, function(first, second) return first.id < second.id end)
    local x = margin_x + (depth - 1) * ((width - margin_x * 2) / math.max(1, depth_count - 1))
    for index, node in ipairs(nodes) do
      local y = top + (index - 1) * ((bottom - top) / math.max(1, #nodes - 1))
      positions[node.id] = { x = x, y = y }
    end
  end
  for _, edge in ipairs(route.edges) do
    local first, second = positions[edge.from], positions[edge.to]
    if first and second then
      local completed = route.completed_node_ids[edge.from]
      local locked = edge.requires_unlock and not route:has_unlock(edge.requires_unlock)
      self:_color(locked and { 0.62, 0.24, 0.18, 0.9 } or (completed and { 0.42, 0.74, 0.68, 0.9 } or { 0.22, 0.31, 0.42, 0.85 }))
      love.graphics.setLineWidth(completed and 3 or 2)
      love.graphics.line(first.x + 62, first.y, second.x - 62, second.y)
    end
  end
  love.graphics.setLineWidth(1)

  local options = app:route_options()
  local selected_id = options[app.menu] and options[app.menu].node_id
  local locked_current = {}
  if route.completed_node_ids[route.current_node_id] then
    for _, entry in ipairs(route:locked_outgoing(route.current_node_id)) do locked_current[entry.node.id] = entry.requires_unlock end
  end
  for _, id in ipairs(route.node_order) do
    local node, position = route:node(id), positions[id]
    local status = locked_current[id] and "locked" or route:status(id)
    local tint = {
      future = { 0.12, 0.16, 0.23 }, completed = { 0.12, 0.29, 0.25 },
      completed_current = { 0.12, 0.29, 0.25 }, current = { 0.16, 0.3, 0.42 },
      available = { 0.26, 0.35, 0.16 },
      locked = { 0.32, 0.13, 0.12 },
    }
    local text_tint = {
      future = { 0.47, 0.55, 0.65 }, completed = { 0.58, 0.9, 0.76 },
      completed_current = { 0.58, 0.9, 0.76 }, current = { 0.78, 0.9, 1 },
      available = { 1, 0.88, 0.3 },
      locked = { 1, 0.48, 0.32 },
    }
    local selected = node.id == selected_id
    self:_color(tint[status] or tint.future)
    love.graphics.rectangle("fill", position.x - 62, position.y - 28, 124, 56)
    self:_color(selected and { 1, 0.86, 0.22 } or text_tint[status] or text_tint.future)
    love.graphics.setLineWidth(selected and 3 or 1)
    love.graphics.rectangle("line", position.x - 62, position.y - 28, 124, 56)
    love.graphics.setLineWidth(1)
    local label, detail
    if node.type == "floor" then
      local biome = session.route_definitions:get_biome(node.biome_id)
      local tier = session.route_definitions:get_tier(node.tier_id)
      label, detail = biome.display_name, locked_current[id] and ("REQUIRES " .. string.upper(app:unlock_display_name(locked_current[id])))
        or ("TIER " .. tier.number .. " • " .. session.registry:get_service(node.service_id).display_name)
    elseif node.type == "boss" then
      local boss = session.registry:get_boss(node.boss_id or "boss.legacy.final")
      label, detail = boss.display_name, status == "future" and "LOCKED" or "BOSS • " .. string.upper(status)
    else
      label, detail = string.upper(node.type), status == "future" and "LOCKED" or string.upper(status)
    end
    self:_text(label, position.x - math.floor(#label * 3.8), position.y - 16, 0.9, text_tint[status] or text_tint.future)
    self:_text(detail, position.x - math.floor(#detail * 2.8), position.y + 5, 0.65, { 0.74, 0.81, 0.9 })
  end
  local selected = options[app.menu]
  if selected then
    self:_text("SELECTED: " .. selected.name .. " — " .. selected.description, width * 0.18, height - 76, 0.9, { 0.95, 0.85, 0.3 })
  end
  self:_text((screen and screen.footer) or "W/S SELECT     ENTER / E DESCEND", width * 0.5 - 150, height - 44, 0.85, { 0.72, 0.8, 0.92 })
end

function Renderer:draw(app)
  if app.screen == "game" then
    self:_draw_game(app)
  elseif app.screen == "title" then
    self:_draw_title(app)
  elseif app.screen == "expedition_character_select" then
    self:_draw_expedition_character_select(app)
  elseif app.screen == "expedition_reward" then
    self:_draw_expedition_reward(app)
  elseif app.screen == "expedition_chest" then
    self:_draw_expedition_chest(app)
  elseif app.screen == "expedition_build" then
    self:_draw_expedition_build(app)
  elseif app.screen == "expedition_summary" then
    self:_draw_expedition_summary(app)
  elseif app.screen == "help" then
    self:_draw_help(app)
  elseif app.screen == "onboarding" then
    self:_draw_onboarding(app)
  elseif app.screen == "campaign_succession" then
    self:_draw_campaign_succession(app)
  elseif app.screen == "replace_save" then
    self:_menu(self:_screen_text(app, "replace_save", "title", "REPLACE ACTIVE RUN?"), { { name = "START NEW RUN", description = "The current active run will be replaced after setup." } }, app.menu,
      self:_screen_text(app, "replace_save", "footer", "ENTER CONFIRM     ESC CANCEL"))
  elseif app.screen == "campaign_slots" then
    local choosing_new = app.campaign_slot_mode == "new"
    self:_menu(choosing_new and "CAMPAIGN SLOTS — REPLACE ONE" or "CAMPAIGN SLOTS", app:campaign_slot_options(), app.menu,
      choosing_new and "W/S OR CLICK SELECT SLOT     ESC TITLE" or "W/S OR CLICK SELECT SLOT     ESC TITLE")
  elseif app.screen == "replace_campaign" then
    local slot = app.campaign_replace_slot or "?"
    self:_menu("REPLACE SLOT " .. slot .. "?", { { name = "START FRESH CAMPAIGN", description = "Only this slot's campaign will be replaced." } }, app.menu,
      "CLICK / ENTER REPLACE     ESC CANCEL")
  elseif app.screen == "curse" then
    self:_menu(self:_screen_text(app, "curse", "title", "CHOOSE A CURSE"), app.session.state.curse_options, app.menu,
      self:_screen_text(app, "curse", "footer", "W/S SELECT     ENTER ACCEPT BURDEN"))
  elseif app.screen == "route" then
    self:_draw_route(app)
  elseif app.screen == "research" then
    self:_draw_research(app)
  elseif app.screen == "fallen_archive" then
    self:_draw_fallen_archive(app)
  elseif app.screen == "service_hub" then
    self:_menu(self:_screen_text(app, "service_hub", "title", "FINAL SERVICE HUB") .. " — SCRAP " .. app.session.state.scrap, app:service_hub_options(), app.menu,
      self:_screen_text(app, "service_hub", "footer", "W/S SELECT     ENTER ACCESS / ENTER BOSS"))
  elseif app.screen == "service" then
    local items = {}
    for _, option in ipairs(app:service_options()) do
      local model = GameplayUI.service_option(app.session, option)
      local suffix = model.price and ("  •  " .. model.price .. " SCRAP") or ""
      if model.remaining ~= nil then suffix = suffix .. "  •  " .. model.remaining .. " LEFT" end
      if model.sold then suffix = suffix .. "  •  SOLD" end
      items[#items + 1] = {
        name = model.name .. suffix,
        description = (model.affordable and "AFFORDABLE — " or "NOT ENOUGH SCRAP — ") .. (model.description or "SERVICE OPTION"),
      }
    end
    self:_menu(self:_screen_text(app, "service", "title", "SERVICE") .. " — SCRAP " .. app.session.state.scrap, items, app.menu,
      self:_screen_text(app, "service", "footer", "W/S SELECT     ENTER/B/V TRANSACT     ESC CLOSE"))
  elseif app.screen == "inventory" then
    self:_draw_inventory(app)
  elseif app.screen == "salvage" then
    self:_draw_salvage(app)
  elseif app.screen == "reconstruction" then
    self:_draw_reconstruction(app)
  elseif app.screen == "body_abilities" then
    self:_draw_body_abilities(app)
  elseif app.screen == "storage" then
    self:_draw_storage(app)
  elseif app.screen == "gameover" then
    self:_menu(self:_screen_text(app, "gameover", "title", "YOU DIED"), { { name = "RETURN TO TITLE", description = self:_screen_text(app, "gameover", "subtitle", "The body is gone.") } }, app.menu,
      self:_screen_text(app, "gameover", "footer", "ENTER RETURN TO TITLE"))
  elseif app.screen == "victory" then
    self:_menu(self:_screen_text(app, "victory", "title", "YOU HAVE WON"), { { name = "THE DESCENT IS OVER", description = self:_screen_text(app, "victory", "subtitle", "The descent is over.") } }, app.menu,
      self:_screen_text(app, "victory", "footer", "ENTER RETURN TO TITLE"))
  end
end

return Renderer
