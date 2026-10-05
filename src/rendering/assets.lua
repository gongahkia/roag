-- Rendering asset boundary. Optional packs are kept as an offline/import
-- surface, but the active game is procedural and never requires a sheet.
local ArtPacks = require("src.rendering.art_packs")
local PresentationAssets = require("src.rendering.presentation_assets")

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

local function clone_pixel(pixel)
  return { pixel[1], pixel[2], pixel[3], pixel[4] }
end

local function color_key(pixel)
  return table.concat({
    math.floor((pixel[1] or 0) * 255 + 0.5),
    math.floor((pixel[2] or 0) * 255 + 0.5),
    math.floor((pixel[3] or 0) * 255 + 0.5),
    math.floor((pixel[4] or 0) * 255 + 0.5),
  }, ":")
end

local function pixel_index(x, y, width)
  return (y - 1) * width + x
end

-- Some supported sheets already carry transparency, while other source
-- sheets encode an otherwise-transparent sprite over a flat opaque tile.  A
-- corner-connected colour mask removes only that backdrop from sprite roles.
-- Terrain keeps its original full tile, and a solid/outlined prop is retained
-- unless its edge colour demonstrably forms a background field.
function Assets.cutout_background_mask(pixels, width, height)
  local result = {}
  for index, pixel in ipairs(pixels or {}) do result[index] = clone_pixel(pixel) end
  if width < 1 or height < 1 then return result, 0 end

  local corners = {
    result[pixel_index(1, 1, width)], result[pixel_index(width, 1, width)],
    result[pixel_index(1, height, width)], result[pixel_index(width, height, width)],
  }
  local candidates = {}
  for _, pixel in ipairs(corners) do
    if pixel and (pixel[4] or 0) > 0 then
      local candidate = color_key(pixel)
      candidates[candidate] = (candidates[candidate] or 0) + 1
    end
  end
  local background_key, background_count
  for candidate, count in pairs(candidates) do
    if count >= 2 and (not background_count or count > background_count) then
      background_key, background_count = candidate, count
    end
  end
  if not background_key then return result, 0 end

  local function matches_background(x, y)
    local pixel = result[pixel_index(x, y, width)]
    return pixel and (pixel[4] or 0) > 0 and color_key(pixel) == background_key
  end

  local queue, queued, head = {}, {}, 1
  local function enqueue(x, y)
    if x < 1 or x > width or y < 1 or y > height then return end
    local index = pixel_index(x, y, width)
    if not queued[index] and matches_background(x, y) then
      queued[index] = true
      queue[#queue + 1] = { x, y }
    end
  end
  for x = 1, width do enqueue(x, 1); enqueue(x, height) end
  for y = 2, height - 1 do enqueue(1, y); enqueue(width, y) end

  local removed = 0
  while queue[head] do
    local point = queue[head]
    head = head + 1
    local index = pixel_index(point[1], point[2], width)
    if result[index][4] ~= 0 then
      result[index][4] = 0
      removed = removed + 1
    end
    enqueue(point[1] - 1, point[2])
    enqueue(point[1] + 1, point[2])
    enqueue(point[1], point[2] - 1)
    enqueue(point[1], point[2] + 1)
  end
  return result, removed
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
    sprite_cutouts = {},
    presentation_assets = PresentationAssets.new(),
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
  -- A focus refresh can pick up a standalone Sprite Editor save. Rebuild the
  -- presentation-only cutouts too, otherwise the newly assigned tile would
  -- retain the previous role's masked source image until restart.
  if self.loaded then self:_load_sprite_cutouts() end
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

function Assets:_sprite_cutout(role, sprite, source_data)
  if not source_data or not love or not love.image or not love.graphics then return nil end
  local sheet_id = sprite.sheet or "main"
  local sheet = self.sheets[sheet_id]
  if not sheet then return nil end
  local definition = sheet.definition
  local width, height = definition.tile_width, definition.tile_height
  local step_x = width + (definition.spacing or 0)
  local step_y = height + (definition.spacing or 0)
  local source_x, source_y = (sprite[1] - 1) * step_x, (sprite[2] - 1) * step_y
  local pixels = {}
  for y = 1, height do
    for x = 1, width do
      local red, green, blue, alpha = source_data:getPixel(source_x + x - 1, source_y + y - 1)
      pixels[pixel_index(x, y, width)] = { red, green, blue, alpha }
    end
  end
  local masked, removed = Assets.cutout_background_mask(pixels, width, height)
  if removed == 0 then return nil end
  -- A full-tile mark has no evidence of a separate backdrop. Preserve it so
  -- a deliberately solid icon never vanishes merely because it touches every
  -- corner of its source tile.
  if removed == width * height then return nil end
  local cutout = love.image.newImageData(width, height)
  for y = 1, height do
    for x = 1, width do
      local pixel = masked[pixel_index(x, y, width)]
      cutout:setPixel(x - 1, y - 1, pixel[1], pixel[2], pixel[3], pixel[4])
    end
  end
  local image = love.graphics.newImage(cutout)
  image:setFilter("nearest", "nearest")
  return { image = image, width = width, height = height, role = role }
end

function Assets:_load_sprite_cutouts()
  self.sprite_cutouts = {}
  if not love or not love.image or not love.graphics then return true end
  local source_data = {}
  for role, sprite in pairs(self.sprites) do
    local sheet_id = sprite.sheet or "main"
    local sheet = self.sheets[sheet_id]
    if sheet and not source_data[sheet_id] then
      local ok, data = pcall(love.image.newImageData, sheet.definition.path)
      if ok then source_data[sheet_id] = data end
    end
    local cutout = self:_sprite_cutout(role, sprite, source_data[sheet_id])
    if cutout then self.sprite_cutouts[role] = cutout end
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
  self:_load_sprite_cutouts()
  return true
end

function Assets:load()
  if not love or not love.graphics then return nil, { code = "graphics_unavailable", reason = "LÖVE graphics is unavailable" } end
  love.graphics.setDefaultFilter("nearest", "nearest")
  self.font = love.graphics.newFont("assets/fonts/BigBlueTermPlusNerdFontMono-Regular.ttf", 16)
  love.graphics.setFont(self.font)
  -- Do not load any legacy sheet here. The current renderer uses procedural
  -- terrain, objects, effects, and actor glyphs, so a clean install remains
  -- playable even when every optional texture pack is absent.
  self.presentation_assets:load()
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

function Assets:_draw_mapping(sprite, x, y, size, tint, transform)
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
  transform = transform or {}
  local scale_x, scale_y = transform.scale_x or 1, transform.scale_y or 1
  local draw_x = x + (transform.offset_x or 0) + (size - size * scale_x) * 0.5
  -- Preserve bottom alignment while breathing so a sprite does not look as
  -- though it is sliding through the tile beneath it.
  local draw_y = y + (transform.offset_y or 0) + (size - size * scale_y)
  love.graphics.draw(sheet.image, quad, draw_x, draw_y, 0,
    size / definition.tile_width * scale_x, size / definition.tile_height * scale_y)
  love.graphics.setColor(1, 1, 1)
  return true
end

function Assets:_draw_cutout(cutout, x, y, size, tint, transform)
  if not cutout or not cutout.image then return false end
  if tint then
    love.graphics.setColor(tint[1], tint[2], tint[3], tint[4] or 1)
  else
    love.graphics.setColor(1, 1, 1)
  end
  transform = transform or {}
  local scale_x, scale_y = transform.scale_x or 1, transform.scale_y or 1
  local draw_x = x + (transform.offset_x or 0) + (size - size * scale_x) * 0.5
  local draw_y = y + (transform.offset_y or 0) + (size - size * scale_y)
  love.graphics.draw(cutout.image, draw_x, draw_y, 0,
    size / cutout.width * scale_x, size / cutout.height * scale_y)
  love.graphics.setColor(1, 1, 1)
  return true
end

function Assets:draw_sprite(kind, x, y, size, tint, transform)
  return self:_draw_cutout(self.sprite_cutouts[kind], x, y, size, tint, transform)
    or self:_draw_mapping(self:_sprite_for(kind), x, y, size, tint, transform)
end

-- Optional actor art is bound through the small ROAG-owned presentation
-- manifest, not a particular source-editor export.  It deliberately returns
-- false in the current shape-first build so Renderer can draw its glyph.
function Assets:draw_optional_actor_asset(actor, state, animation_tag, elapsed_seconds, x, y, size, tint, transform)
  local expedition = state and state.expedition
  local character_id = actor == (state and state.player) and expedition and expedition.character_id
  if character_id and self.presentation_assets:draw_character(character_id, animation_tag, elapsed_seconds, x, y, size, tint, transform) then
    return true
  end
  return false
end

-- Static terrain/object/effect bindings share the same optional manifest as
-- characters. The current manifest is intentionally absent, so callers use
-- their procedural glyph fallback without ever opening a legacy sheet.
function Assets:draw_optional_role_asset(role_id, x, y, size, tint, transform)
  return self.presentation_assets:draw_role(role_id, x, y, size, tint, transform)
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
