-- Rendering asset boundary. Art packs only choose presentation sprites; they
-- cannot influence deterministic simulation, body ownership, or save data.
local ArtPacks = require("src.rendering.art_packs")

local Assets = {}
Assets.__index = Assets

local function clone_sprites(source)
  return ArtPacks.clone_sprites(source)
end

local function copy_catalog()
  return ArtPacks.list()
end

local function key(column, row)
  return tostring(column) .. ":" .. tostring(row)
end

function Assets.new(options)
  options = options or {}
  local art_pack_id = options.art_pack_id or ArtPacks.DEFAULT_ID
  if not ArtPacks.has(art_pack_id) then art_pack_id = ArtPacks.DEFAULT_ID end
  local definition = ArtPacks.get(art_pack_id)
  return setmetatable({
    art_pack_id = art_pack_id,
    art_pack = definition,
    sprites = clone_sprites(definition.sprites),
    default_sprites = clone_sprites(ArtPacks.get(ArtPacks.DEFAULT_ID).sprites),
    loaded = false,
    sheets = {},
    quads = {},
  }, Assets)
end

function Assets:available_art_packs()
  return copy_catalog()
end

function Assets:current_art_pack()
  return self.art_pack
end

function Assets:_mapping_contents()
  if not love or not love.filesystem then return nil end
  local source = love.filesystem.getSource()
  if source:sub(-5):lower() == ".love" then
    local path = love.filesystem.getSourceBaseDirectory() .. "/sprite_editor/mappings.json"
    local file = io.open(path, "rb")
    if file then
      local contents = file:read("*a")
      file:close()
      return contents
    end
  end
  return love.filesystem.read("sprite_editor/mappings.json")
end

-- The standalone Sprite Editor edits the original ROAG 1-bit mapping.  Other
-- third-party packs remain deterministic built-in starter mappings, so a
-- sheet-specific editor is never allowed to leak into normal run state.
function Assets:refresh_sprite_mappings()
  if self.art_pack_id ~= ArtPacks.DEFAULT_ID then return false end
  local contents = self:_mapping_contents()
  if not contents then return false end
  local main = self.art_pack.sheets.main
  for kind, column, row in contents:gmatch('\"([%w_]+)\"%s*:%s*{%s*\"column\"%s*:%s*(%d+)%s*,%s*\"row\"%s*:%s*(%d+)%s*}') do
    column, row = tonumber(column), tonumber(row)
    if self.default_sprites[kind]
      and column >= 1 and column <= main.columns and row >= 1 and row <= main.rows then
      self.sprites[kind] = { column, row, sheet = "main" }
    end
  end
  return true
end

function Assets:reset_sprite(kind)
  if self.art_pack_id ~= ArtPacks.DEFAULT_ID then return nil, { code = "not_editable", reason = "Only the original ROAG 1-bit pack has editable mappings" } end
  local tile = self.default_sprites[kind]
  if not tile then return nil, { code = "unknown_sprite", reason = "Unknown sprite role" } end
  self.sprites[kind] = { tile[1], tile[2], sheet = tile.sheet }
  return true
end

function Assets:_load_sheet(id, definition)
  local ok, image_or_error = pcall(love.graphics.newImage, definition.path)
  if not ok then return nil, { code = "art_pack_load_failed", reason = tostring(image_or_error), path = definition.path } end
  local image = image_or_error
  local actual_width, actual_height = image:getDimensions()
  local step_x = definition.tile_width + (definition.spacing or 0)
  local step_y = definition.tile_height + (definition.spacing or 0)
  local required_width = (definition.columns - 1) * step_x + definition.tile_width
  local required_height = (definition.rows - 1) * step_y + definition.tile_height
  if actual_width < required_width or actual_height < required_height then
    return nil, { code = "art_pack_sheet_invalid", reason = "Sheet dimensions are smaller than its declared grid", path = definition.path }
  end
  self.sheets[id] = { image = image, definition = definition }
  self.quads[id] = {}
  for column = 1, definition.columns do
    for row = 1, definition.rows do
      self.quads[id][key(column, row)] = love.graphics.newQuad(
        (column - 1) * step_x, (row - 1) * step_y,
        definition.tile_width, definition.tile_height, actual_width, actual_height
      )
    end
  end
  return true
end

function Assets:_load_art_pack()
  self.sheets, self.quads = {}, {}
  for id, definition in pairs(self.art_pack.sheets) do
    local loaded, error_data = self:_load_sheet(id, definition)
    if not loaded then
      self.sheets, self.quads = {}, {}
      return nil, error_data
    end
  end
  self.sprites = clone_sprites(self.art_pack.sprites)
  self.sheet = self.sheets.main and self.sheets.main.image
  if self.art_pack_id == ArtPacks.DEFAULT_ID then self:refresh_sprite_mappings() end
  return true
end

function Assets:load()
  if not love or not love.graphics then return nil, { code = "graphics_unavailable", reason = "LÖVE graphics is unavailable" } end
  love.graphics.setDefaultFilter("nearest", "nearest")
  self.font = love.graphics.newFont("assets/fonts/BigBlueTermPlusNerdFontMono-Regular.ttf", 16)
  love.graphics.setFont(self.font)
  local loaded, error_data = self:_load_art_pack()
  if not loaded then return nil, error_data end
  self.loaded = true
  return true
end

function Assets:select_art_pack(id)
  local definition = ArtPacks.get(id)
  if not definition then return nil, { code = "unknown_art_pack", reason = "Unknown art pack" } end
  if id == self.art_pack_id then return true end
  local old_id, old_definition, old_sprites = self.art_pack_id, self.art_pack, self.sprites
  self.art_pack_id, self.art_pack = id, definition
  if self.loaded then
    local loaded, error_data = self:_load_art_pack()
    if not loaded then
      self.art_pack_id, self.art_pack, self.sprites = old_id, old_definition, old_sprites
      self:_load_art_pack()
      return nil, error_data
    end
  else
    self.sprites = clone_sprites(definition.sprites)
  end
  return true
end

function Assets:_sprite_for(kind)
  return self.sprites[kind]
end

function Assets:_quad_for(sprite)
  if not sprite then return nil end
  local sheet_id = sprite.sheet or "main"
  return self.quads[sheet_id] and self.quads[sheet_id][key(sprite[1], sprite[2])]
end

function Assets:quad(kind)
  return self:_quad_for(self:_sprite_for(kind))
end

function Assets:_draw_mapping(sprite, x, y, size, tint)
  if not sprite then return false end
  local sheet_id = sprite.sheet or "main"
  local sheet, quad = self.sheets[sheet_id], self:_quad_for(sprite)
  if not sheet or not quad then return false end
  if tint then
    love.graphics.setColor(tint[1], tint[2], tint[3], tint[4] or 1)
  else
    love.graphics.setColor(1, 1, 1)
  end
  local definition = sheet.definition
  love.graphics.draw(sheet.image, quad, x, y, 0, size / definition.tile_width, size / definition.tile_height)
  love.graphics.setColor(1, 1, 1)
  return true
end

function Assets:draw_sprite(kind, x, y, size, tint)
  return self:_draw_mapping(self:_sprite_for(kind), x, y, size, tint)
end

function Assets:draw_terrain(kind, x, y, size, tint)
  local terrain = self.art_pack.terrain
  if not terrain then return false end
  -- Third-party packs can define a generic wall tile.  The original ROAG pack
  -- instead references optional editor roles for directional wall faces.
  local mapping = terrain[kind]
  if not mapping and kind:match("^wall_") then mapping = terrain.wall end
  if mapping and mapping.role then mapping = self.sprites[mapping.role] end
  return self:_draw_mapping(mapping, x, y, size, tint)
end

return Assets
