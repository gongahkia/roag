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
      if Grid.in_bounds(x, y) and state.visible[location_key] then
        if state.space[location_key] then
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
        self:_color(state.space[location_key] and { floor[1] * 0.28, floor[2] * 0.28, floor[3] * 0.28 } or { 0.035, 0.025, 0.04 })
        love.graphics.rectangle("fill", pixel_x, pixel_y, size, size)
      else
        self:_color({ 0.008, 0.011, 0.017 })
        love.graphics.rectangle("fill", pixel_x, pixel_y, size, size)
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
  self:_text(state.boss and "BOSS " .. state.boss.health .. " / 10" or "SCORE " .. state.player.score .. " / " .. state.settings.score, hud, offset_y + 150, 1, { 0.95, 0.85, 0.25 })
  local inventory = state.inventory
  self:_text("CARGO " .. inventory:total_mass() .. "  " .. inventory:encumbrance(), hud, offset_y + 174, 0.88,
    inventory:encumbrance() == "LIGHT" and { 0.65, 0.9, 0.8 } or { 0.95, 0.72, 0.35 })
  if state.curse then
    self:_text("CURSE " .. state.curse.name, hud, offset_y + 196, 1, { 0.9, 0.4, 0.8 })
  end
  if state.boss then
    self:_text("BOSS " .. state.boss.name .. " IN " .. math.max(0, BOSS_WINDUP - state.boss.attack), hud, offset_y + 218, 0.8, { 1, 0.6, 0.35 })
  end
  self:_text("CONTROLS", hud, offset_y + 246, 1, { 0.6, 0.8, 1 })
  self:_text("WASD MOVE / HOLD", hud, offset_y + 266, 0.85)
  self:_text("ARROWS SHOOT   E FORWARD", hud, offset_y + 284, 0.75)
  self:_text("Q dash   B bomb   F flare", hud, offset_y + 302, 0.75)
  self:_text("G salvage   I inventory", hud, offset_y + 320, 0.75)
  self:_text("INTENTS", hud, offset_y + 352, 1, { 0.9, 0.7, 0.4 })
  for index, enemy in ipairs(state.enemies) do
    if index > 5 then
      break
    end
    self:_text(string.upper(enemy.kind) .. ": " .. session:enemy_intent(enemy), hud, offset_y + 370 + index * 17, 0.75)
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

function Renderer:_draw_title()
  love.graphics.clear(0.025, 0.035, 0.055)
  local width, height = love.graphics.getDimensions()
  self:_text("ROAG", width / 2 - 104, height / 2 - 100, 4, { 0.7, 0.9, 1 })
  self:_text("A ONE-BIT DESCENT", width / 2 - 110, height / 2 - 34, 1.2, { 0.7, 0.75, 0.85 })
  self:_text("PRESS ENTER TO BEGIN", width / 2 - 115, height / 2 + 48, 1, { 0.95, 0.85, 0.3 })
  self:_text("P: SPRITE LAB", width / 2 - 62, height / 2 + 78, 0.82, { 0.75, 0.82, 0.92 })
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
    self:_draw_title()
  elseif app.screen == "sprite_lab" then
    self:_draw_sprite_lab(app)
  elseif app.screen == "class" then
    self:_menu("CHOOSE YOUR CLASS", app.content.classes, app.menu)
  elseif app.screen == "boon" then
    self:_menu("CHOOSE A BOON", app.boon_options, app.menu)
  elseif app.screen == "curse" then
    self:_menu("CHOOSE A CURSE", app.session.state.curse_options, app.menu, "W/S SELECT     ENTER ACCEPT BURDEN")
  elseif app.screen == "shop" then
    self:_menu("SHOP — POINTS " .. app.session.state.score, app.content.shop, app.menu, "W/S SELECT     B BUY     V SELL     ENTER FIGHT BOSS")
  elseif app.screen == "inventory" then
    self:_draw_inventory(app)
  elseif app.screen == "salvage" then
    self:_draw_salvage(app)
  elseif app.screen == "gameover" then
    self:_menu("YOU DIED", { { name = "RETURN TO TITLE", description = "Press Enter to begin a new descent." } }, app.menu, "")
  elseif app.screen == "victory" then
    self:_menu("YOU HAVE WON", { { name = "THE DESCENT IS OVER", description = "Press Enter to return to the title." } }, app.menu, "")
  end
end

return Renderer
