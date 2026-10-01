local Grid = require("src.world.grid")
local Component = require("src.body.component")
local PhysicalItem = require("src.inventory.physical_item")

local Renderer = {}
Renderer.__index = Renderer

local VIEW_WIDTH, VIEW_HEIGHT = 39, 25
local BOSS_WINDUP = 4
local SPRITE_ORDER = {
  { kind = "player", label = "PLAYER" },
  { kind = "target", label = "TARGET" },
  { kind = "ammo", label = "AMMO" },
  { kind = "torch", label = "TORCH" },
  { kind = "door", label = "EXIT DOOR" },
  { kind = "bullet", label = "BULLET" },
  { kind = "bomb", label = "BOMB" },
  { kind = "flare", label = "FLARE" },
  { kind = "wolf", label = "WOLF" },
  { kind = "bomber", label = "BOMBER" },
  { kind = "necromancer", label = "NECROMANCER" },
  { kind = "cultist", label = "CULTIST" },
  { kind = "boss", label = "BOSS" },
}

local function clamp(value, minimum, maximum)
  return math.max(minimum, math.min(maximum, value))
end

function Renderer.new(assets)
  return setmetatable({ assets = assets }, Renderer)
end

function Renderer:_color(color)
  love.graphics.setColor(color[1], color[2], color[3], color[4] or 1)
end

function Renderer:_text(value, x, y, scale, color)
  if type(scale) == "table" then
    color, scale = scale, 1
  end
  self:_color(color or { 1, 1, 1 })
  love.graphics.print(value, x, y, 0, scale or 1)
  love.graphics.setColor(1, 1, 1)
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

function Renderer:_screen_position(presentation, player, x, y, size, offset_x, offset_y)
  local camera_x, camera_y = self:_camera_position(presentation, player)
  local screen_x = x - camera_x + math.floor((VIEW_WIDTH - 1) / 2)
  local screen_y = y - camera_y + math.floor((VIEW_HEIGHT - 1) / 2)
  if screen_x < -1 or screen_x >= VIEW_WIDTH + 1 or screen_y < -1 or screen_y >= VIEW_HEIGHT + 1 then
    return nil
  end
  return offset_x + screen_x * size, offset_y + (VIEW_HEIGHT - 1 - screen_y) * size
end

function Renderer:_draw_game(app)
  local session, state, presentation = app.session, app.session.state, app.presentation
  local size, offset_x, offset_y, hud = self:_layout()
  love.graphics.clear(0.025, 0.035, 0.055)
  local shake = (presentation.hit_shake or 0) / 0.24
  local time = love.timer.getTime()
  offset_x = offset_x + math.sin(time * 78) * 6 * shake
  offset_y = offset_y + math.cos(time * 93) * 4 * shake
  self:_color({ 0.08, 0.1, 0.14 })
  love.graphics.rectangle("fill", offset_x - 4, offset_y - 4, VIEW_WIDTH * size + 8, VIEW_HEIGHT * size + 8)

  local floor = {
    forest = { 0.09, 0.19, 0.13 },
    cave = { 0.12, 0.14, 0.18 },
    dungeon = { 0.16, 0.12, 0.18 },
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
      local location_key = Grid.key(x, y)
      local passable = state.world and state.world:is_passable(x, y)
      if Grid.in_bounds(x, y) and state.visible[location_key] then
        if passable then
          self:_color(floor)
          love.graphics.rectangle("fill", pixel_x, pixel_y, size, size)
          if (x * 7 + y * 11) % 5 == 0 then
            self:_color({ floor[1] * 1.55, floor[2] * 1.55, floor[3] * 1.55 })
            love.graphics.rectangle("fill", pixel_x + size * 0.35, pixel_y + size * 0.35, math.max(1, size * 0.12), math.max(1, size * 0.12))
          end
        else
          self:_color({ 0.11, 0.075, 0.13 })
          love.graphics.rectangle("fill", pixel_x, pixel_y, size, size)
          self:_color({ 0.22, 0.13, 0.22 })
          love.graphics.rectangle("line", pixel_x, pixel_y, size, size)
        end
      elseif Grid.in_bounds(x, y) and state.explored[location_key] then
        self:_color(passable and { floor[1] * 0.28, floor[2] * 0.28, floor[3] * 0.28 } or { 0.035, 0.025, 0.04 })
        love.graphics.rectangle("fill", pixel_x, pixel_y, size, size)
      else
        self:_color({ 0.008, 0.011, 0.017 })
        love.graphics.rectangle("fill", pixel_x, pixel_y, size, size)
      end
    end
  end

  -- Liquid is a simulation-owned layer over passable terrain. The restrained
  -- depth tint is presentation only; it never affects the authoritative flow.
  for _, liquid in ipairs(state.world and state.world:list_liquids() or {}) do
    if state.visible[Grid.key(liquid.x, liquid.y)] then
      local pixel_x, pixel_y = self:_screen_position(presentation, state.player, liquid.x, liquid.y, size, offset_x, offset_y)
      if pixel_x then
        local definition = session.registry:get_liquid(liquid.liquid_id)
        local depth = liquid.amount / definition.max_depth
        local shimmer = 0.03 + math.sin(time * 3 + liquid.x * 2 + liquid.y * 5) * 0.02
        self:_color({ 0.08, 0.36 + depth * 0.14, 0.72 + shimmer, 0.38 + depth * 0.3 })
        love.graphics.rectangle("fill", pixel_x + size * 0.06, pixel_y + size * (0.62 - depth * 0.16), size * 0.88, size * (0.3 + depth * 0.16))
        self:_color({ 0.42, 0.78, 1, 0.45 + depth * 0.2 })
        love.graphics.line(pixel_x + size * 0.18, pixel_y + size * 0.65, pixel_x + size * 0.78, pixel_y + size * 0.65)
      end
    end
  end

  -- Gas is an authoritative coordinate layer, rendered after liquid so both
  -- can coexist visibly. The drifting tint depends only on wall-clock time
  -- and coordinates; it neither uses simulation RNG nor affects no-fog LOS.
  for _, gas in ipairs(state.world and state.world:list_gases() or {}) do
    if state.visible[Grid.key(gas.x, gas.y)] then
      local pixel_x, pixel_y = self:_screen_position(presentation, state.player, gas.x, gas.y, size, offset_x, offset_y)
      if pixel_x then
        local definition = session.registry:get_gas(gas.gas_id)
        local density = gas.concentration / definition.max_concentration
        local drift = math.sin(time * 2.7 + gas.x * 1.9 + gas.y * 3.1) * size * 0.06
        self:_color({ 0.36, 0.86, 0.3, 0.12 + density * 0.32 })
        love.graphics.circle("fill", pixel_x + size * 0.36 + drift, pixel_y + size * 0.55, math.max(1, size * (0.16 + density * 0.22)))
        self:_color({ 0.56, 1, 0.4, 0.08 + density * 0.24 })
        love.graphics.circle("fill", pixel_x + size * 0.65 - drift, pixel_y + size * 0.38, math.max(1, size * (0.12 + density * 0.18)))
      end
    end
  end

  -- Hazards are a passable, simulation-owned floor layer. The compact crossed
  -- spike marker makes danger readable without introducing a new art set.
  for _, hazard in ipairs(state.world and state.world:list_hazards() or {}) do
    if state.visible[Grid.key(hazard.x, hazard.y)] then
      local pixel_x, pixel_y = self:_screen_position(presentation, state.player, hazard.x, hazard.y, size, offset_x, offset_y)
      if pixel_x then
        self:_color({ 0.9, 0.22, 0.18, 0.92 })
        love.graphics.line(pixel_x + size * 0.18, pixel_y + size * 0.78, pixel_x + size * 0.48, pixel_y + size * 0.22)
        love.graphics.line(pixel_x + size * 0.48, pixel_y + size * 0.78, pixel_x + size * 0.72, pixel_y + size * 0.22)
        love.graphics.line(pixel_x + size * 0.76, pixel_y + size * 0.78, pixel_x + size * 0.9, pixel_y + size * 0.42)
      end
    end
  end

  -- Fire is authoritative world state. Flicker is presentation-only and uses
  -- wall-clock time, never the deterministic simulation RNG.
  for _, fire in ipairs(state.world and state.world:list_fires() or {}) do
    local fire_x, fire_y = state.world:fire_position(fire)
    if fire_x and state.visible[Grid.key(fire_x, fire_y)] then
      local pixel_x, pixel_y = self:_screen_position(presentation, state.player, fire_x, fire_y, size, offset_x, offset_y)
      if pixel_x then
        local flicker = 0.06 + math.sin(time * 11 + fire_x * 3 + fire_y * 5) * 0.035
        self:_color({ 1, 0.28 + flicker, 0.05, 0.92 })
        love.graphics.polygon("fill",
          pixel_x + size * 0.28, pixel_y + size * 0.78,
          pixel_x + size * 0.5, pixel_y + size * 0.14,
          pixel_x + size * 0.74, pixel_y + size * 0.78)
        self:_color({ 1, 0.82, 0.25, 0.95 })
        love.graphics.circle("fill", pixel_x + size * 0.5, pixel_y + size * 0.59, math.max(1, size * 0.13))
      end
    end
  end

  -- World objects are simulation-owned physical state. These compact markers
  -- preserve the sprite language while making door/device state readable.
  for _, object in ipairs(state.world and state.world:list_objects() or {}) do
    if state.visible[Grid.key(object.x, object.y)] then
      local pixel_x, pixel_y = self:_screen_position(presentation, state.player, object.x, object.y, size, offset_x, offset_y)
      if pixel_x then
        local definition = session.registry:get_world_object(object.definition_id)
        if object.interaction_role == "door" then
          if object.door_state == "open" then
            self:_color({ 0.35, 0.9, 0.85, 0.85 })
            love.graphics.line(pixel_x + size * 0.15, pixel_y + size * 0.15, pixel_x + size * 0.15, pixel_y + size * 0.85)
            love.graphics.line(pixel_x + size * 0.85, pixel_y + size * 0.15, pixel_x + size * 0.85, pixel_y + size * 0.85)
          else
            self:_color({ 0.24, 0.42, 0.58 })
            love.graphics.rectangle("fill", pixel_x + size * 0.1, pixel_y + size * 0.08, size * 0.8, size * 0.84)
            self:_color({ 0.75, 0.9, 1 })
            love.graphics.rectangle("line", pixel_x + size * 0.1, pixel_y + size * 0.08, size * 0.8, size * 0.84)
            self:_color(state.world:is_circuit_powered(object.circuit_id) and { 0.25, 1, 0.55 } or { 1, 0.38, 0.22 })
            love.graphics.circle("fill", pixel_x + size * 0.72, pixel_y + size * 0.25, math.max(1, size * 0.07))
          end
        elseif object.interaction_role == "generator" then
          self:_color(object.generator_online and { 0.18, 0.62, 0.36 } or { 0.3, 0.31, 0.34 })
          love.graphics.rectangle("fill", pixel_x + size * 0.19, pixel_y + size * 0.24, size * 0.62, size * 0.52)
          self:_color({ 0.72, 0.9, 0.82 })
          love.graphics.rectangle("line", pixel_x + size * 0.19, pixel_y + size * 0.24, size * 0.62, size * 0.52)
        elseif object.interaction_role == "breaker" then
          local circuit = state.world:get_circuit(object.circuit_id)
          self:_color(circuit.enabled and { 0.92, 0.68, 0.18 } or { 0.48, 0.22, 0.18 })
          love.graphics.rectangle("fill", pixel_x + size * 0.3, pixel_y + size * 0.16, size * 0.4, size * 0.68)
          self:_color({ 0.95, 0.88, 0.5 })
          love.graphics.line(pixel_x + size * 0.5, pixel_y + size * 0.26, pixel_x + size * 0.5, pixel_y + size * 0.73)
        elseif object.interaction_role == "service" then
          self:_color({ 0.56, 0.28, 0.72 })
          love.graphics.rectangle("fill", pixel_x + size * 0.2, pixel_y + size * 0.2, size * 0.6, size * 0.6)
          self:_color({ 0.95, 0.78, 1 })
          love.graphics.rectangle("line", pixel_x + size * 0.2, pixel_y + size * 0.2, size * 0.6, size * 0.6)
        else
          local tint = definition.render_style == "crate" and { 0.56, 0.34, 0.14 } or { 0.42, 0.44, 0.49 }
          self:_color(tint)
          love.graphics.rectangle("fill", pixel_x + size * 0.17, pixel_y + size * 0.17, size * 0.66, size * 0.66)
          self:_color({ 0.9, 0.78, 0.5 })
          love.graphics.rectangle("line", pixel_x + size * 0.17, pixel_y + size * 0.17, size * 0.66, size * 0.66)
        end
      end
    end
  end

  for location_key, style in pairs(session:telegraphs()) do
    local x, y = location_key:match("(%d+):(%d+)")
    local pixel_x, pixel_y = self:_screen_position(presentation, state.player, tonumber(x), tonumber(y), size, offset_x, offset_y)
    if pixel_x then
      self:_color(style == "danger" and { 1, 0.2, 0.15, 0.4 } or { 1, 0.75, 0.15, 0.28 })
      love.graphics.rectangle("fill", pixel_x, pixel_y, size, size)
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
    if value == state.player and presentation.hit_flash > 0 then
      tint = { 1, 0.35, 0.35 }
    end
    self.assets:draw_sprite(value.kind, pixel_x, pixel_y, size, tint)
  end

  for _, values in ipairs({ state.torches, state.targets, state.enemies, state.bullets, state.bombs, state.flares }) do
    for _, value in ipairs(values) do
      actor(value)
    end
  end
  for _, corpse in ipairs(state.corpses or {}) do
    local remains = { kind = corpse.source_kind, x = corpse.x, y = corpse.y }
    actor(remains, false, { 0.32, 0.28, 0.4, 0.8 })
    local pixel_x, pixel_y = self:_screen_position(presentation, state.player, corpse.x, corpse.y, size, offset_x, offset_y)
    if pixel_x then
      self:_color({ 0.85, 0.3, 0.45, 0.75 })
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
      self:_color({ 0.35, 0.85, 1, pulse })
      love.graphics.line(effect_x + size * 0.16, effect_y + size * 0.52, effect_x + size * 0.42, effect_y + size * 0.28)
      love.graphics.line(effect_x + size * 0.42, effect_y + size * 0.28, effect_x + size * 0.58, effect_y + size * 0.69)
      love.graphics.line(effect_x + size * 0.58, effect_y + size * 0.69, effect_x + size * 0.84, effect_y + size * 0.43)
    end
  end
  if state.boss then
    for x = 16, 24 do
      actor({ kind = "boss", x = x, y = 1 })
    end
  end
  actor(state.player, true)
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

  self:_text("ROAG", hud, offset_y, 2, { 0.7, 0.9, 1 })
  self:_text("HP    " .. string.rep("♥", state.player.health), hud, offset_y + 42, 1 + presentation.hit_flash * 0.8, { 1, 0.35, 0.35 })
  self:_text("AMMO  " .. state.player.ammo, hud, offset_y + 64)
  self:_text("BOMBS " .. state.player.bombs .. "  ARMED " .. #state.bombs, hud, offset_y + 84)
  self:_text("FLARES " .. state.player.flares .. "  LIT " .. #state.flares, hud, offset_y + 104)
  self:_text("DASH  " .. (state.player.dash == 0 and "READY" or "RECHARGING"), hud, offset_y + 124)
  self:_text(state.boss and "BOSS " .. state.boss.health .. " / 10" or "OBJECTIVE " .. (state.player.objective_progress or state.player.score) .. " / " .. state.settings.objective_required, hud, offset_y + 150, 0.9, { 0.95, 0.85, 0.25 })
  local inventory = state.inventory
  self:_text("CARGO " .. inventory:total_mass() .. "  " .. inventory:encumbrance(), hud, offset_y + 174, 0.88,
    inventory:encumbrance() == "LIGHT" and { 0.65, 0.9, 0.8 } or { 0.95, 0.72, 0.35 })
  local charm_count = 0
  for index = 1, 3 do if state.charms and state.charms.slots[index] then charm_count = charm_count + 1 end end
  self:_text("SCRAP " .. (state.scrap or 0) .. "  CHARMS " .. charm_count .. "/3", hud, offset_y + 196, 0.82, { 0.65, 0.9, 0.8 })
  if state.curse then
    self:_text("CURSE " .. (state.curse.display_name or state.curse.name), hud, offset_y + 216, 0.82, { 0.9, 0.4, 0.8 })
  end
  local status_y = offset_y + 238
  local locomotion = session:locomotion_state(state.player)
  if locomotion.state ~= "NORMAL" then
    local locomotion_color = locomotion.state == "IMPAIRED" and { 1, 0.72, 0.3 } or { 1, 0.42, 0.42 }
    self:_text("LOCOMOTION " .. locomotion.state, hud, status_y, 0.76, locomotion_color)
    status_y = status_y + 18
  end
  local ranged = session:actor_ability_provider(state.player, "ability.weapon.projectile.basic")
  if ranged then
    local definition = session.registry:get_component(ranged.component.definition_id)
    self:_text("WEAPON " .. string.upper(definition.display_name), hud, status_y, 0.68, { 0.65, 0.9, 0.8 })
  else
    self:_text("WEAPON OFFLINE", hud, status_y, 0.72, { 1, 0.42, 0.42 })
  end
  status_y = status_y + 18
  if state.boss then
    self:_text("BOSS " .. state.boss.name .. " IN " .. math.max(0, BOSS_WINDUP - state.boss.attack), hud, status_y, 0.8, { 1, 0.6, 0.35 })
    status_y = status_y + 18
  end
  local abilities = session:available_actor_abilities(state.player, "body")
  if #abilities > 0 then
    local ability = session.registry:get_ability(abilities[1])
    self:_text("BODY X: " .. string.upper(ability.display_name), hud, status_y, 0.72, { 0.95, 0.65, 0.35 })
    status_y = status_y + 18
  end
  local interactions = session:available_interactions(state.player)
  if #interactions > 0 and interactions[1].actions[1] then
    local primary = interactions[1]
    local action = primary.actions[1]
    self:_text("U " .. action.label .. (action.available and "" or " — " .. (action.reason or "UNAVAILABLE")),
      hud, status_y, 0.68, action.available and { 0.6, 0.9, 0.75 } or { 1, 0.48, 0.32 })
    status_y = status_y + 18
  end
  local controls_y = math.max(offset_y + 278, status_y + 8)
  self:_text("CONTROLS", hud, controls_y, 1, { 0.6, 0.8, 1 })
  self:_text("WASD MOVE / HOLD", hud, controls_y + 20, 0.85)
  self:_text("ARROWS SHOOT   E FORWARD", hud, controls_y + 38, 0.75)
  self:_text("Q dash   B bomb   F flare", hud, controls_y + 56, 0.75)
  self:_text("G salvage   I inventory   U interact", hud, controls_y + 74, 0.68)
  self:_text("INTENTS", hud, controls_y + 106, 1, { 0.9, 0.7, 0.4 })
  for index, enemy in ipairs(state.enemies) do
    if index > 5 then
      break
    end
    self:_text(string.upper(enemy.kind) .. ": " .. session:enemy_intent(enemy), hud, controls_y + 124 + index * 17, 0.75)
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
  self:_text("ID " .. component.id, x, y + 24, 0.68, { 0.68, 0.76, 0.88 })
  self:_text("SLOTS " .. table.concat(definition.compatible_slots, ", "), x, y + 43, 0.72)
  self:_text("INTEGRITY " .. component.current_integrity .. " / " .. component.max_integrity, x, y + 61, 0.78)
  self:_text("CONDITION " .. string.upper(Component.condition(component)), x, y + 79, 0.78,
    Component.is_functional(component) and { 0.6, 0.9, 0.75 } or { 1, 0.35, 0.35 })
  self:_text("MASS " .. definition.mass .. "   SIZE " .. definition.inventory.width .. "×" .. definition.inventory.height, x, y + 97, 0.75)
  if abilities then
    local labels = {}
    for _, ability_id in ipairs(abilities) do
      labels[#labels + 1] = session.registry:get_ability(ability_id).display_name
    end
    self:_text("ABILITY " .. table.concat(labels, ", "), x, y + 115, 0.7, { 0.95, 0.65, 0.35 })
  else
    self:_text("ABILITY NONE", x, y + 115, 0.7, { 0.65, 0.72, 0.82 })
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
    self:_text(string.upper(slot.id:gsub("_", " ")), x + 5, y + 5, 0.61, { 0.7, 0.8, 0.92 })
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
    local item_width, item_height = inventory:footprint(entry.item, entry.rotated)
    local px, py = inventory_x + (entry.x - 1) * cell + 2, grid_y + (entry.y - 1) * cell + 2
    local selected = app.reconstruction_focus == "inventory" and index == app.reconstruction_inventory_index
    local queued = app.reconstruction_selected_id == entry.physical_id
    self:_color((selected or queued) and { 0.92, 0.7, 0.2 } or { 0.18, 0.45, 0.58 })
    love.graphics.rectangle("fill", px, py, item_width * cell - 7, item_height * cell - 7)
    self:_color({ 0.8, 0.9, 1 })
    love.graphics.rectangle("line", px, py, item_width * cell - 7, item_height * cell - 7)
    self:_text(entry.item.display_name, px + 4, py + 5, 0.58, { 0.95, 0.97, 1 })
  end

  local selected_slot = app:reconstruction_slot()
  local selected_entry = app.reconstruction_selected_id and inventory:get(app.reconstruction_selected_id) or app:reconstruction_inventory_entry()
  local detail_component = app.reconstruction_focus == "body" and selected_slot and selected_slot.component
    or selected_entry and selected_entry.item.object
  self:_draw_component_detail(session, detail_component, inventory_x, grid_y + inventory.height * cell + 28)

  local feedback = app:reconstruction_feedback()
  local feedback_color = feedback.compatible and { 0.6, 0.9, 0.75 } or { 1, 0.42, 0.42 }
  self:_text(feedback.compatible and "COMPATIBLE" or "INCOMPATIBLE", body_x, height - 92, 1, feedback_color)
  self:_text(feedback.reason or "", body_x + 145, height - 92, 0.78, feedback_color)
  self:_text("TAB FOCUS   W/S SELECT   ENTER INSTALL / UNINSTALL   R ROTATE INVENTORY", body_x, height - 62, 0.7, { 0.75, 0.82, 0.92 })
  self:_text("F FINISH RECONSTRUCTION", body_x, height - 38, 0.76, { 0.95, 0.85, 0.3 })
end

function Renderer:_draw_body_abilities(app)
  local session = app.session
  local width, height = love.graphics.getDimensions()
  love.graphics.clear(0.025, 0.035, 0.055)
  self:_text("BODY ABILITIES", width * 0.18, 64, 2, { 0.7, 0.9, 1 })
  for index, ability_id in ipairs(app.body_ability_options or {}) do
    local ability = session.registry:get_ability(ability_id)
    local y = 142 + (index - 1) * 76
    local selected = index == app.menu
    self:_color(selected and { 0.23, 0.16, 0.12 } or { 0.06, 0.08, 0.12 })
    love.graphics.rectangle("fill", width * 0.18, y, width * 0.64, 58)
    self:_text((selected and "> " or "  ") .. ability.display_name, width * 0.21, y + 12, 1.05,
      selected and { 1, 0.65, 0.35 } or { 1, 1, 1 })
  end
  if app.body_ability_confirming then
    self:_text("CONFIRM ACTIVATION? THIS DESTROYS YOUR CURRENT BODY. PRESS ENTER.", width * 0.18, height - 104, 0.82, { 1, 0.38, 0.32 })
  else
    self:_text("SELECT AN ABILITY, THEN PRESS ENTER TO ARM CONFIRMATION.", width * 0.18, height - 104, 0.78, { 0.75, 0.82, 0.92 })
  end
  self:_text("W/S SELECT     ENTER CONFIRM     X / ESC CANCEL", width * 0.18, height - 62, 0.82, { 0.75, 0.82, 0.92 })
end

function Renderer:_draw_inventory(app)
  local inventory = app.session.state.inventory
  local width, height = love.graphics.getDimensions()
  local cell = math.max(42, math.min(72, math.floor(math.min((width * 0.52) / inventory.width, (height - 210) / inventory.height))))
  local grid_x = math.floor(width * 0.12)
  local grid_y = math.floor((height - inventory.height * cell) / 2) + 35
  local cursor = app.inventory_cursor or { x = 1, y = 1 }

  love.graphics.clear(0.025, 0.035, 0.055)
  self:_text("CARRIED INVENTORY", grid_x, 38, 2, { 0.7, 0.9, 1 })
  self:_text(inventory:total_mass() .. " MASS  •  " .. inventory:encumbrance(), grid_x, 76, 1, { 0.95, 0.85, 0.3 })
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
    local item_width, item_height = inventory:footprint(entry.item, entry.rotated)
    local pixel_x = grid_x + (entry.x - 1) * cell + 2
    local pixel_y = grid_y + (entry.y - 1) * cell + 2
    local selected = app.inventory_selected_id == entry.physical_id
    self:_color(selected and { 0.92, 0.7, 0.2 } or { 0.18, 0.45, 0.58 })
    love.graphics.rectangle("fill", pixel_x, pixel_y, item_width * cell - 7, item_height * cell - 7)
    self:_color({ 0.8, 0.9, 1 })
    love.graphics.rectangle("line", pixel_x, pixel_y, item_width * cell - 7, item_height * cell - 7)
    self:_text(entry.item.display_name, pixel_x + 5, pixel_y + 6, 0.7, { 0.95, 0.97, 1 })
  end
  local cursor_x = grid_x + (cursor.x - 1) * cell
  local cursor_y = grid_y + (cursor.y - 1) * cell
  self:_color({ 1, 0.85, 0.2 })
  love.graphics.setLineWidth(3)
  love.graphics.rectangle("line", cursor_x - 2, cursor_y - 2, cell + 1, cell + 1)
  love.graphics.setLineWidth(1)

  local entry = app.inventory_selected_id and inventory:get(app.inventory_selected_id) or inventory:item_at(cursor.x, cursor.y)
  local detail_x = grid_x + inventory.width * cell + 42
  if entry then
    local item, component = entry.item, entry.item.object
    local footprint_width, footprint_height = inventory:footprint(item, entry.rotated)
    self:_text(item.display_name, detail_x, grid_y, 1.25, { 0.95, 0.85, 0.3 })
    self:_text("ID " .. item.physical_id, detail_x, grid_y + 32, 0.72, { 0.68, 0.76, 0.88 })
    self:_text("MASS " .. item.mass, detail_x, grid_y + 52, 0.9)
    self:_text("SIZE " .. footprint_width .. "×" .. footprint_height .. " CELLS", detail_x, grid_y + 74, 0.85)
    if component then
      self:_text("INTEGRITY " .. component.current_integrity .. " / " .. component.max_integrity, detail_x, grid_y + 98, 0.85)
      self:_text(string.upper(Component.condition(component)), detail_x, grid_y + 119, 0.85,
        Component.is_functional(component) and { 0.6, 0.9, 0.75 } or { 1, 0.35, 0.35 })
    end
  else
    self:_text("EMPTY CELL", detail_x, grid_y, 1, { 0.65, 0.75, 0.9 })
  end
  self:_text("WASD / ARROWS MOVE CURSOR", grid_x, height - 96, 0.8, { 0.75, 0.82, 0.92 })
  self:_text("ENTER SELECT / PLACE     R ROTATE     I / ESC CLOSE", grid_x, height - 70, 0.8, { 0.75, 0.82, 0.92 })
end

function Renderer:_draw_salvage(app)
  local session = app.session
  local corpse = session:find_corpse(app.salvage_corpse_id)
  local options = app:salvage_options()
  local width, height = love.graphics.getDimensions()
  love.graphics.clear(0.025, 0.035, 0.055)
  self:_text("CORPSE SALVAGE", width * 0.18, 58, 2, { 0.7, 0.9, 1 })
  if not corpse then
    self:_text("CORPSE NO LONGER AVAILABLE", width * 0.18, 136, 1, { 1, 0.4, 0.4 })
  elseif #options == 0 then
    self:_text("NO SALVAGEABLE COMPONENTS REMAIN", width * 0.18, 136, 1, { 0.72, 0.76, 0.84 })
  else
    self:_text(string.upper(corpse.source_kind) .. " REMAINS  " .. corpse.id, width * 0.18, 101, 0.85, { 0.95, 0.85, 0.3 })
    for index, installed in ipairs(options) do
      local component = installed.component
      local definition = session.registry:get_component(component.definition_id)
      local placement = session.state.inventory:find_first_fit(PhysicalItem.from_component(component, session.registry))
      local y = 136 + (index - 1) * 88
      local selected = index == app.menu
      self:_color(selected and { 0.13, 0.22, 0.3 } or { 0.06, 0.08, 0.12 })
      love.graphics.rectangle("fill", width * 0.18, y, width * 0.64, 72)
      self:_text((selected and "> " or "  ") .. definition.display_name, width * 0.21, y + 9, 1.1,
        selected and { 0.95, 0.85, 0.3 } or { 1, 1, 1 })
      self:_text(string.upper(installed.slot_id) .. "  •  " .. component.current_integrity .. "/" .. component.max_integrity
        .. " " .. string.upper(Component.condition(component)) .. "  •  MASS " .. definition.mass
        .. "  •  " .. definition.inventory.width .. "×" .. definition.inventory.height,
        width * 0.21, y + 39, 0.78, { 0.72, 0.76, 0.84 })
      self:_text(placement and "FITS INVENTORY" or "NO INVENTORY SPACE", width * 0.67, y + 9, 0.72,
        placement and { 0.6, 0.9, 0.75 } or { 1, 0.4, 0.4 })
    end
  end
  self:_text("W/S SELECT     ENTER SALVAGE     G / ESC CLOSE", width * 0.18, height - 58, 0.85, { 0.75, 0.82, 0.92 })
end

function Renderer:_menu(title, items, selected, footer)
  love.graphics.clear(0.025, 0.035, 0.055)
  local width, height = love.graphics.getDimensions()
  self:_text(title, width / 2 - #title * 8, 70, 2, { 0.7, 0.9, 1 })
  for index, item in ipairs(items) do
    local y = 150 + (index - 1) * 84
    local is_selected = index == selected
    self:_color(is_selected and { 0.13, 0.22, 0.3 } or { 0.06, 0.08, 0.12 })
    love.graphics.rectangle("fill", width * 0.18, y, width * 0.64, 68)
    self:_text((is_selected and "> " or "  ") .. item.name, width * 0.21, y + 9, 1.25, is_selected and { 0.95, 0.85, 0.3 } or { 1, 1, 1 })
    self:_text(item.description or "", width * 0.21, y + 37, 0.85, { 0.72, 0.76, 0.84 })
  end
  self:_text(footer or "W/S SELECT     ENTER CONFIRM", width / 2 - 150, height - 52, 1, { 0.65, 0.75, 0.9 })
end

function Renderer:_draw_title(app)
  love.graphics.clear(0.025, 0.035, 0.055)
  local width, height = love.graphics.getDimensions()
  self:_text("ROAG", width / 2 - 104, height / 2 - 100, 4, { 0.7, 0.9, 1 })
  self:_text("A ONE-BIT DESCENT", width / 2 - 110, height / 2 - 34, 1.2, { 0.7, 0.75, 0.85 })
  local options = app:title_options()
  for index, option in ipairs(options) do
    self:_text((index == app.menu and "> " or "  ") .. option.name, width / 2 - 68, height / 2 + 26 + index * 29,
      1, index == app.menu and { 0.95, 0.85, 0.3 } or { 0.78, 0.83, 0.9 })
  end
  local message = app.title_error and "SAVE UNAVAILABLE — START A NEW RUN" or "W/S SELECT     ENTER CONFIRM"
  self:_text(message, width / 2 - #message * 4, height / 2 + 112, 0.78, { 0.75, 0.82, 0.92 })
  self:_text("P: SPRITE LAB", width / 2 - 62, height / 2 + 138, 0.82, { 0.75, 0.82, 0.92 })
end

-- Route state is authoritative data from the session graph.  This renderer
-- only lays out compact cards and edges; it never decides availability or
-- progression, keeping the map safe to rebuild after save/load.
function Renderer:_draw_route(app)
  local session, route = app.session, app.session.state.route
  local width, height = love.graphics.getDimensions()
  love.graphics.clear(0.025, 0.035, 0.055)
  self:_text("CHOOSE YOUR DESCENT", width * 0.5 - 150, 42, 1.7, { 0.7, 0.9, 1 })
  self:_text("THE ROUTE IS ONE WAY", width * 0.5 - 96, 77, 0.8, { 0.7, 0.76, 0.86 })

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
      self:_color(completed and { 0.42, 0.74, 0.68, 0.9 } or { 0.22, 0.31, 0.42, 0.85 })
      love.graphics.setLineWidth(completed and 3 or 2)
      love.graphics.line(first.x + 62, first.y, second.x - 62, second.y)
    end
  end
  love.graphics.setLineWidth(1)

  local options = app:route_options()
  local selected_id = options[app.menu] and options[app.menu].node_id
  for _, id in ipairs(route.node_order) do
    local node, position = route:node(id), positions[id]
    local status = route:status(id)
    local tint = {
      future = { 0.12, 0.16, 0.23 }, completed = { 0.12, 0.29, 0.25 },
      completed_current = { 0.12, 0.29, 0.25 }, current = { 0.16, 0.3, 0.42 },
      available = { 0.26, 0.35, 0.16 },
    }
    local text_tint = {
      future = { 0.47, 0.55, 0.65 }, completed = { 0.58, 0.9, 0.76 },
      completed_current = { 0.58, 0.9, 0.76 }, current = { 0.78, 0.9, 1 },
      available = { 1, 0.88, 0.3 },
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
      label, detail = biome.display_name, "TIER " .. tier.number .. " • " .. session.registry:get_service(node.service_id).display_name
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
  self:_text("W/S SELECT     ENTER / E DESCEND", width * 0.5 - 150, height - 44, 0.85, { 0.72, 0.8, 0.92 })
end

function Renderer:_draw_sprite_lab(app)
  local lab, sprites = app.sprite_lab, self.assets.sprites
  love.graphics.clear(0.025, 0.035, 0.055)
  local width, height = love.graphics.getDimensions()
  local scale = math.min(1, math.max(0.5, math.min((width - 460) / (49 * 16), (height - 130) / (22 * 16))))
  local tile_size = 16 * scale
  local sheet_width = 49 * tile_size
  local sheet_x, sheet_y = width - sheet_width - 28, 88
  local current = SPRITE_ORDER[lab.slot]

  self:_text("SPRITE LAB", 28, 26, 2, { 0.7, 0.9, 1 })
  self:_text("EDITING " .. current.label, 28, 72, 1.15, { 0.95, 0.85, 0.3 })
  self:_text("HIGHLIGHTED TILE: COLUMN " .. lab.x .. "  ROW " .. lab.y, 28, 98, 0.8, { 0.8, 0.85, 0.9 })
  love.graphics.draw(self.assets.sheet, self.assets.quads[lab.x .. ":" .. lab.y], 28, 122, 0, 4, 4)
  for index, item in ipairs(SPRITE_ORDER) do
    local y = 198 + (index - 1) * 29
    if index == lab.slot then
      self:_color({ 0.13, 0.22, 0.3 })
      love.graphics.rectangle("fill", 24, y - 3, 390, 26)
    end
    self.assets:draw_sprite(item.kind, 30, y, 24)
    local tile = sprites[item.kind]
    self:_text(item.label .. "  [" .. tile[1] .. ", " .. tile[2] .. "]", 62, y + 2, 0.82, index == lab.slot and { 0.95, 0.85, 0.3 } or { 0.78, 0.83, 0.9 })
  end
  love.graphics.draw(self.assets.sheet, sheet_x, sheet_y, 0, scale, scale)
  local function outline(column, row, tint, line_width)
    self:_color(tint)
    love.graphics.setLineWidth(line_width)
    love.graphics.rectangle("line", sheet_x + (column - 1) * tile_size, sheet_y + (row - 1) * tile_size, tile_size, tile_size)
  end
  local assigned = sprites[current.kind]
  outline(assigned[1], assigned[2], { 0.15, 0.9, 1 }, 2)
  outline(lab.x, lab.y, { 1, 0.85, 0.2 }, 3)
  love.graphics.setLineWidth(1)
  self:_text("WASD / ARROWS: BROWSE TILE", 28, height - 104, 0.78, { 0.75, 0.82, 0.92 })
  self:_text("Q / E: CHANGE CHARACTER     ENTER: ASSIGN LIVE", 28, height - 82, 0.78, { 0.75, 0.82, 0.92 })
  self:_text("R: RESET CHARACTER     X: RESET ALL     P / ESC: RETURN", 28, height - 60, 0.78, { 0.75, 0.82, 0.92 })
  self:_text("PREVIEW ONLY — SAVE MAPPINGS IN THE STANDALONE SPRITE EDITOR", 28, height - 34, 0.72, { 0.95, 0.65, 0.45 })
end

function Renderer:draw(app)
  if app.screen == "game" then
    self:_draw_game(app)
  elseif app.screen == "title" then
    self:_draw_title(app)
  elseif app.screen == "replace_save" then
    self:_menu("REPLACE ACTIVE RUN?", { { name = "START NEW RUN", description = "The current active run will be replaced after setup." } }, app.menu,
      "ENTER CONFIRM     ESC CANCEL")
  elseif app.screen == "sprite_lab" then
    self:_draw_sprite_lab(app)
  elseif app.screen == "curse" then
    self:_menu("CHOOSE A CURSE", app.session.state.curse_options, app.menu, "W/S SELECT     ENTER ACCEPT BURDEN")
  elseif app.screen == "route" then
    self:_draw_route(app)
  elseif app.screen == "service_hub" then
    self:_menu("FINAL SERVICE HUB — SCRAP " .. app.session.state.scrap, app:service_hub_options(), app.menu, "W/S SELECT     ENTER ACCESS / ENTER BOSS")
  elseif app.screen == "service" then
    local items = {}
    for _, option in ipairs(app:service_options()) do
      local suffix = option.price and ("  •  " .. option.price .. " SCRAP") or ""
      if option.remaining ~= nil then suffix = suffix .. "  •  " .. option.remaining .. " LEFT" end
      items[#items + 1] = { name = option.label .. suffix, description = option.description or (option.integrity and ("INTEGRITY " .. option.integrity .. "/" .. option.max_integrity) or "") }
    end
    self:_menu("SERVICE — SCRAP " .. app.session.state.scrap, items, app.menu, "W/S SELECT     ENTER/B/V TRANSACT     ESC CLOSE")
  elseif app.screen == "inventory" then
    self:_draw_inventory(app)
  elseif app.screen == "salvage" then
    self:_draw_salvage(app)
  elseif app.screen == "reconstruction" then
    self:_draw_reconstruction(app)
  elseif app.screen == "body_abilities" then
    self:_draw_body_abilities(app)
  elseif app.screen == "gameover" then
    self:_menu("YOU DIED", { { name = "RETURN TO TITLE", description = "Press Enter to begin a new descent." } }, app.menu, "")
  elseif app.screen == "victory" then
    self:_menu("YOU HAVE WON", { { name = "THE DESCENT IS OVER", description = "Press Enter to return to the title." } }, app.menu, "")
  end
end

return Renderer
