-- Developer-only LÖVE front-end for one external dungeon room at a time.
-- Gameplay App/session/save construction is deliberately absent from this
-- module; its model writes only validated room corpus data through Store.
local EditorModel = require("level_editor.room_editor_model")
local Template = require("src.rooms.template")

local Editor = {}
Editor.__index = Editor

local PALETTE = {
  ["#"] = { 0.28, 0.18, 0.31 },
  ["."] = { 0.11, 0.17, 0.23 },
  ["="] = { 0.12, 0.34, 0.4 },
}

local function contains(values, wanted)
  for _, value in ipairs(values or {}) do if value == wanted then return true end end
  return false
end

local function copy(values)
  local result = {}
  for index, value in ipairs(values or {}) do result[index] = value end
  return result
end

local function clamp(value, minimum, maximum)
  return math.max(minimum, math.min(maximum, value))
end

local function sorted_palette(config)
  local result = {}
  for glyph in pairs(config.PALETTE) do result[#result + 1] = glyph end
  table.sort(result)
  return result
end

local function sorted_tags(config)
  local result = {}
  for tag in pairs(config.TAGS) do result[#result + 1] = tag end
  table.sort(result)
  return result
end

function Editor.new(options)
  local model, failure = EditorModel.new(options)
  if not model then error(failure.reason or failure.code) end
  local self = setmetatable({
    model = model,
    selected_glyph = ".",
    rotation = 0,
    selected_id = nil,
    editing_id = false,
    id_text = "",
    dragging = false,
    exit_pending = false,
    message = nil,
    viewport = {},
  }, Editor)
  local rooms = model:list()
  if rooms[1] then
    self.selected_id = rooms[1].id
    assert(model:open(rooms[1].id))
  end
  return self
end

function Editor:_layout(width, height)
  local config = self.model.config
  local browser_width, details_width = 270, 340
  local available_width = math.max(160, width - browser_width - details_width - 48)
  local available_height = math.max(160, height - 104)
  local tile = clamp(math.floor(math.min(available_width / config.WIDTH, available_height / config.HEIGHT)), 14, 48)
  local grid_width, grid_height = tile * config.WIDTH, tile * config.HEIGHT
  self.viewport = {
    browser_x = 12, browser_y = 60, browser_width = browser_width - 20,
    grid_x = browser_width + math.floor((available_width - grid_width) / 2),
    grid_y = 64 + math.floor((available_height - grid_height) / 2),
    tile = tile, grid_width = grid_width, grid_height = grid_height,
    detail_x = width - details_width + 14, detail_width = details_width - 26,
  }
end

function Editor:_set_color(color)
  love.graphics.setColor(color[1], color[2], color[3], color[4] or 1)
end

function Editor:_text(value, x, y, color, limit)
  self:_set_color(color or { 0.9, 0.92, 0.98 })
  if limit then love.graphics.printf(tostring(value), x, y, limit) else love.graphics.print(tostring(value), x, y) end
end

function Editor:_current_preview()
  if not self.model.current then return nil end
  local preview = self.model:rotation_preview(self.rotation)
  return preview or self.model.current
end

function Editor:_world_from_screen(x, y)
  local view = self.viewport
  if x < view.grid_x or y < view.grid_y or x >= view.grid_x + view.grid_width or y >= view.grid_y + view.grid_height then return nil end
  local local_x = math.floor((x - view.grid_x) / view.tile)
  local local_y = self.model.config.HEIGHT - 1 - math.floor((y - view.grid_y) / view.tile)
  return local_x, local_y
end

function Editor:_browser_item_at(x, y)
  local view = self.viewport
  if x < view.browser_x or x >= view.browser_x + view.browser_width or y < view.browser_y then return nil end
  local index = math.floor((y - view.browser_y) / 22) + 1
  return self.model:list()[index]
end

function Editor:_set_message(value)
  self.message = value
end

function Editor:_request_open(id)
  local opened, failure = self.model:open(id)
  if not opened then self:_set_message(failure.reason or failure.code); return nil end
  self.selected_id, self.rotation, self.editing_id = id, 0, false
  self:_set_message(self.model.message)
  return opened
end

function Editor:_unique_id(prefix)
  local index = 1
  while self.model.rooms:get(prefix .. tostring(index)) do index = index + 1 end
  return prefix .. tostring(index)
end

function Editor:_cycle_tag()
  local current = self.model.current
  if not current then return end
  local tags = copy(current.tags)
  local position = 0
  local cycle = sorted_tags(self.model.config)
  for index, tag in ipairs(cycle) do if contains(tags, tag) then position = index; break end end
  local next_tag = cycle[position % #cycle + 1]
  local preserved = {}
  for _, tag in ipairs(tags) do
    local known = false
    for _, candidate in ipairs(cycle) do if tag == candidate then known = true end end
    if not known then preserved[#preserved + 1] = tag end
  end
  preserved[#preserved + 1] = next_tag
  self.model:set_metadata("tags", preserved)
  self:_set_message("Tags: " .. table.concat(preserved, ", "))
end

function Editor:_switch_corpus()
  local corpora, current = self.model:corpora(), self.model.config.CORPUS_ID
  local index = 1
  for position, config in ipairs(corpora) do if config.CORPUS_ID == current then index = position end end
  local next_config = corpora[index % #corpora + 1]
  local ok, failure = self.model:switch_corpus(next_config.CORPUS_ID)
  if not ok then self:_set_message(failure.reason or failure.code); return end
  local rooms = self.model:list()
  self.selected_id, self.rotation = nil, 0
  if rooms[1] then self:_request_open(rooms[1].id) end
  self:_set_message("Corpus: " .. next_config.CORPUS_ID)
end

function Editor:_paint_at(x, y)
  if self.rotation ~= 0 then
    self:_set_message("Painting is available in the 0 degree authoring view.")
    return
  end
  local ok, failure = self.model:paint(x, y, self.selected_glyph)
  if not ok then self:_set_message(failure.reason or failure.code) end
end

function Editor:_toggle_connector_at(x, y)
  if self.rotation ~= 0 then
    self:_set_message("Edit connectors in the 0 degree authoring view.")
    return
  end
  local connector = Template.connector_from_position(self.model.current, x, y)
  if not connector then self:_set_message("Connectors must be on a room boundary."); return end
  local result, failure = self.model:toggle_connector(connector.side, connector.offset)
  if not result then self:_set_message(failure.reason or failure.code) else self:_set_message(result.present and "Connector added" or "Connector removed") end
end

function Editor:draw()
  local width, height = love.graphics.getDimensions()
  self:_layout(width, height)
  love.graphics.clear(0.025, 0.035, 0.055)
  local view = self.viewport
  self:_set_color({ 0.055, 0.07, 0.105, 0.98 })
  love.graphics.rectangle("fill", 6, 48, 270, height - 58)
  love.graphics.rectangle("fill", width - 350, 48, 344, height - 58)
  self:_text("ROOM TEMPLATE EDITOR", 14, 16, { 0.45, 0.9, 1 })
  local config = self.model.config
  self:_text("source corpus: " .. config.DIRECTORY .. (self.model.store:can_write() and "  [writable]" or "  [READ ONLY]"), 14, 34, self.model.store:can_write() and { 0.65, 0.82, 0.78 } or { 1, 0.5, 0.35 })

  self:_text("TEMPLATES", view.browser_x, view.browser_y - 22, { 1, 0.78, 0.3 })
  for index, template in ipairs(self.model:list()) do
    local y = view.browser_y + (index - 1) * 22
    if template.id == self.selected_id then
      self:_set_color({ 0.17, 0.42, 0.58, 0.9 })
      love.graphics.rectangle("fill", view.browser_x - 3, y - 2, view.browser_width + 4, 20)
    end
    self:_text(template.id:gsub("^room%." .. config.BIOME .. "%.", ""), view.browser_x, y, { 0.88, 0.9, 0.96 }, view.browser_width)
  end

  local room = self:_current_preview()
  if room then
    self:_text("authoring view " .. tostring(self.rotation) .. "°", view.grid_x, view.grid_y - 24, { 0.72, 0.8, 0.92 })
    for local_x = 0, config.WIDTH - 1 do
      for local_y = 0, config.HEIGHT - 1 do
        local glyph = Template.glyph_at(room, local_x, local_y)
        local screen_x = view.grid_x + local_x * view.tile
        local screen_y = view.grid_y + (config.HEIGHT - 1 - local_y) * view.tile
        self:_set_color(PALETTE[glyph] or { 0.7, 0.12, 0.12 })
        love.graphics.rectangle("fill", screen_x, screen_y, view.tile, view.tile)
        self:_set_color({ 0.25, 0.31, 0.4, 0.75 })
        love.graphics.rectangle("line", screen_x, screen_y, view.tile, view.tile)
      end
    end
    for _, connector in ipairs(room.connectors or {}) do
      local x, y = Template.connector_position(room, connector)
      local screen_x = view.grid_x + x * view.tile
      local screen_y = view.grid_y + (config.HEIGHT - 1 - y) * view.tile
      self:_set_color({ 0.25, 0.95, 1, 1 })
      love.graphics.rectangle("fill", screen_x + view.tile * 0.24, screen_y + view.tile * 0.24, view.tile * 0.52, view.tile * 0.52)
    end
  end

  local validation = self.model:validation()
  local x, y = view.detail_x, 64
  self:_text("ROOM DETAILS", x, y, { 1, 0.78, 0.3 }); y = y + 24
  if self.model.current then
    self:_text("ID: " .. (self.editing_id and self.id_text .. "_" or self.model.current.id), x, y, { 0.88, 0.92, 1 }, view.detail_width); y = y + 18
    self:_text("tags: " .. table.concat(self.model.current.tags, ", "), x, y, nil, view.detail_width); y = y + 18
    self:_text("weight: " .. tostring(self.model.current.weight) .. "   rotation: " .. tostring(self.model.current.allow_rotation), x, y); y = y + 18
    self:_text("connectors: " .. #self.model.current.connectors .. "   dirty: " .. tostring(self.model.dirty), x, y); y = y + 26
    self:_text("PALETTE", x, y, { 0.45, 0.9, 1 }); y = y + 18
    for index, glyph in ipairs(sorted_palette(config)) do
      local material = self.model.registry:get_material(config.PALETTE[glyph])
      self:_text("[" .. index .. "] " .. glyph .. " " .. material.display_name .. (self.selected_glyph == glyph and "  SELECTED" or ""), x, y)
      y = y + 17
    end
    y = y + 7
    self:_text(validation.valid and "VALID — save enabled" or "VALIDATION ERRORS", x, y, validation.valid and { 0.35, 1, 0.58 } or { 1, 0.35, 0.28 }); y = y + 18
    for _, error in ipairs(validation.errors) do
      self:_text(error.code .. ": " .. error.message, x, y, { 1, 0.5, 0.38 }, view.detail_width)
      y = y + 31
      if y > height - 166 then break end
    end
  end
  local controls_y = height - 152
  self:_text("CONTROLS", x, controls_y, { 0.45, 0.9, 1 }); controls_y = controls_y + 18
  self:_text("click/drag paint | right-click boundary connector", x, controls_y, nil, view.detail_width); controls_y = controls_y + 17
  self:_text("[ ] browse  C corpus  N new  D duplicate  I set ID", x, controls_y, nil, view.detail_width); controls_y = controls_y + 17
  self:_text("G cycle tag  +/- weight  A rotate-enabled  R preview", x, controls_y, nil, view.detail_width); controls_y = controls_y + 17
  self:_text("S save  Y discard confirmation  Esc exit", x, controls_y, nil, view.detail_width)
  if self.message or self.model.message then
    self:_set_color({ 0.08, 0.12, 0.17, 0.96 })
    love.graphics.rectangle("fill", 12, height - 42, width - 24, 28)
    self:_text(self.message or self.model.message, 20, height - 35, { 1, 0.82, 0.38 }, width - 40)
  end
end

function Editor:keypressed(key)
  if self.editing_id then
    if key == "return" or key == "kpenter" then
      local ok, failure = self.model:set_id(self.id_text)
      self:_set_message(ok and "Template ID updated" or failure.reason or failure.code)
      self.editing_id = false
    elseif key == "escape" then self.editing_id = false
    elseif key == "backspace" then self.id_text = self.id_text:sub(1, -2) end
    return
  end
  if key == "y" and self.model.pending then
    local result, failure = self.model:confirm_discard()
    self:_set_message(result and self.model.message or failure.reason or failure.code)
    if result and self.model.current then self.selected_id, self.rotation = self.model.current.id, 0 end
    return
  end
  if key == "y" and self.exit_pending then love.event.quit(); return end
  if key == "escape" then
    if self.model.dirty then self.exit_pending = true; self:_set_message("Unsaved changes. Press S to save or Y to discard and exit.") else love.event.quit() end
    return
  end
  if key == "[" or key == "]" then
    local rooms = self.model:list()
    local current_index = 1
    for index, room in ipairs(rooms) do if room.id == self.selected_id then current_index = index end end
    local next_index = ((current_index - 1 + (key == "]" and 1 or -1)) % #rooms) + 1
    self:_request_open(rooms[next_index].id)
    return
  end
  if key == "c" then self:_switch_corpus(); return end
  if key == "n" then
    local id = self:_unique_id("room." .. self.model.config.BIOME .. ".standard.new_")
    local result, failure = self.model:new_template(id)
    self:_set_message(result and "New template: " .. id or failure.reason or failure.code)
    if result then self.selected_id, self.rotation = id, 0 end
    return
  end
  if key == "d" and self.selected_id then
    local id = self:_unique_id("room." .. self.model.config.BIOME .. ".standard.copy_")
    local result, failure = self.model:duplicate(self.selected_id, id)
    self:_set_message(result and "Duplicate: " .. id or failure.reason or failure.code)
    if result then self.selected_id, self.rotation = id, 0 end
    return
  end
  if key == "i" and self.model.current then
    if self.model.original_id then self:_set_message("Duplicate or create a room to edit its semantic ID.")
    else self.editing_id, self.id_text = true, self.model.current.id end
    return
  end
  local palette_index = tonumber(key)
  if palette_index then
    local glyph = sorted_palette(self.model.config)[palette_index]
    if glyph then self.selected_glyph = glyph; return end
  end
  if key == "g" then self:_cycle_tag(); return end
  if key == "a" and self.model.current then
    self.model:set_metadata("allow_rotation", not self.model.current.allow_rotation)
    if not self.model.current.allow_rotation then self.rotation = 0 end
    return
  end
  if key == "=" or key == "+" then
    self.model:set_metadata("weight", self.model.current.weight + 1); return
  end
  if key == "-" then self.model:set_metadata("weight", math.max(1, self.model.current.weight - 1)); return end
  if key == "r" and self.model.current and self.model.current.allow_rotation then self.rotation = (self.rotation + 90) % 360; return end
  if key == "s" then
    local result, failure = self.model:save()
    self:_set_message(result and ("Saved " .. result.filename) or failure.reason or failure.code)
    if result then self.selected_id = self.model.current.id end
    return
  end
end

function Editor:textinput(text)
  if self.editing_id then self.id_text = self.id_text .. text end
end

function Editor:mousepressed(x, y, button)
  local browser = self:_browser_item_at(x, y)
  if button == 1 and browser then self:_request_open(browser.id); return end
  local local_x, local_y = self:_world_from_screen(x, y)
  if local_x == nil then return end
  if button == 1 then self.dragging = true; self:_paint_at(local_x, local_y)
  elseif button == 2 then self:_toggle_connector_at(local_x, local_y) end
end

function Editor:mousemoved(x, y)
  if self.dragging then
    local local_x, local_y = self:_world_from_screen(x, y)
    if local_x then self:_paint_at(local_x, local_y) end
  end
end

function Editor:mousereleased(_, _, button)
  if button == 1 then self.dragging = false end
end

function Editor:update() end
function Editor:resize() end
function Editor:wheelmoved() end

return Editor
