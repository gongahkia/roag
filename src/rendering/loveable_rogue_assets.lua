-- The active visual contract.  This module deliberately owns one fixed
-- Loveable Rogue atlas rather than selecting among packs or falling back to
-- procedural actor/world glyphs.  It is also usable headlessly for content
-- validation and tests.
local Json = require("src.persistence.json")

local LoveableRogueAssets = {}
LoveableRogueAssets.__index = LoveableRogueAssets
LoveableRogueAssets.METADATA_PATH = "content/presentation/loveable_rogue_atlas.json"
LoveableRogueAssets.IMAGE_PATH = "assets/visual/loveable_rogue_atlas.png"

local REQUIRED_SPRITES = {
  "terrain.floor", "terrain.floor_alt", "terrain.wall_center", "terrain.wall_up", "terrain.wall_down",
  "terrain.wall_left", "terrain.wall_right", "terrain.wall_top_left", "terrain.wall_top_right",
  "terrain.wall_bottom_left", "terrain.wall_bottom_right", "effect.liquid", "effect.gas", "effect.spikes",
  "effect.fire", "effect.electricity", "object.door_open", "object.door_closed", "object.chest",
  "object.cache", "object.crate", "object.generator", "object.breaker", "object.station", "object.barrier",
  "object.traversal", "object.connection", "object.reinforcement", "item.cash", "item.pickup",
  "projectile.normal", "projectile.explosive", "player.gunner", "player.bruiser", "player.conductor",
  "player.demolitionist", "enemy.rusher", "enemy.ranged", "enemy.flanker", "enemy.controller",
  "enemy.heavy", "enemy.reinforcer", "enemy.elite", "boss.default", "actor.fallen_echo", "actor.target",
  "actor.ammo", "actor.exit",
}

local function read_file(path)
  if love and love.filesystem and love.filesystem.getInfo(path) then return love.filesystem.read(path) end
  local file, reason = io.open(path, "rb")
  if not file then return nil, reason end
  local contents = file:read("*a")
  file:close()
  return contents
end

local function finite_integer(value)
  return type(value) == "number" and value == math.floor(value) and value >= 0
end

local function color_style(color)
  color = color or { 1, 1, 1 }
  local red, green, blue = color[1] or 1, color[2] or 1, color[3] or 1
  if red > green * 1.15 and red > blue * 1.3 then return "amber" end
  if blue > red * 1.08 or green > red * 1.08 then return "blue" end
  if red < 0.7 and green < 0.7 and blue < 0.7 then return "grey" end
  return "white"
end

function LoveableRogueAssets.load_metadata(path)
  local contents, reason = read_file(path or LoveableRogueAssets.METADATA_PATH)
  if not contents then return nil, { code = "loveable_rogue_metadata_read_failed", reason = tostring(reason), path = path } end
  local metadata, decode_reason = Json.decode(contents)
  if not metadata then return nil, { code = "loveable_rogue_metadata_json", reason = tostring(decode_reason), path = path } end
  local valid, errors = LoveableRogueAssets.validate_metadata(metadata)
  if not valid then return nil, { code = "loveable_rogue_metadata_invalid", reason = table.concat(errors, "; "), path = path } end
  return metadata
end

function LoveableRogueAssets.validate_metadata(metadata)
  local errors = {}
  if type(metadata) ~= "table" then return nil, { "metadata must be an object" } end
  if metadata.schema_version ~= 1 then errors[#errors + 1] = "schema_version must be 1" end
  local atlas = metadata.atlas
  if type(atlas) ~= "table" or type(atlas.image) ~= "string" or not finite_integer(atlas.width) or not finite_integer(atlas.height)
    or atlas.width < 1 or atlas.height < 1 then
    errors[#errors + 1] = "atlas image and positive dimensions are required"
  end
  if type(metadata.sprites) ~= "table" then errors[#errors + 1] = "sprites must be an object" end
  for _, id in ipairs(REQUIRED_SPRITES) do
    local rectangle = metadata.sprites and metadata.sprites[id]
    if type(rectangle) ~= "table" or #rectangle ~= 4 then
      errors[#errors + 1] = "missing sprite " .. id
    elseif not finite_integer(rectangle[1]) or not finite_integer(rectangle[2]) or not finite_integer(rectangle[3]) or not finite_integer(rectangle[4])
      or rectangle[3] < 1 or rectangle[4] < 1
      or (atlas and rectangle[1] + rectangle[3] > atlas.width) or (atlas and rectangle[2] + rectangle[4] > atlas.height) then
      errors[#errors + 1] = "sprite " .. id .. " is outside atlas bounds"
    end
  end
  local font = metadata.font
  if type(font) ~= "table" or not finite_integer(font.cell_width) or not finite_integer(font.cell_height)
    or font.cell_width < 1 or font.cell_height < 1 or type(font.rows) ~= "table" then
    errors[#errors + 1] = "font rows and positive cell dimensions are required"
  else
    local seen = {}
    for style, row in pairs(font.rows) do
      if type(row) ~= "table" or not finite_integer(row.y) or type(row.characters) ~= "string" then
        errors[#errors + 1] = "font row " .. tostring(style) .. " is invalid"
      else
        local row_seen = {}
        for index = 1, #row.characters do
          local character = row.characters:sub(index, index)
          if row_seen[character] then errors[#errors + 1] = "font row " .. tostring(style) .. " duplicates " .. character end
          row_seen[character] = true
          if row.y + font.cell_height > (atlas and atlas.height or 0) or (index * font.cell_width) > (atlas and atlas.width or 0) then
            errors[#errors + 1] = "font row " .. tostring(style) .. " is outside atlas bounds"
            break
          end
          seen[style .. ":" .. character] = true
        end
      end
    end
    for _, character in ipairs({ "A", "Z", "0", "9", "?" }) do
      local found = false
      for _, row in pairs(font.rows) do if row.characters:find(character, 1, true) then found = true end end
      if not found then errors[#errors + 1] = "font is missing " .. character end
    end
  end
  local bindings = metadata.bindings
  if type(bindings) ~= "table" or type(bindings.classes) ~= "table" or type(bindings.enemy_kinds) ~= "table"
    or type(bindings.roles) ~= "table" or type(bindings.objects) ~= "table" then
    errors[#errors + 1] = "class, enemy, role, and object bindings are required"
  else
    for _, id in ipairs({ "expedition.gunner", "expedition.bruiser", "expedition.conductor", "expedition.demolitionist" }) do
      if not bindings.classes[id] then errors[#errors + 1] = "missing class binding " .. id end
    end
    for _, role in ipairs({ "rusher", "ranged", "skirmisher", "flanker", "controller", "heavy", "reinforcer" }) do
      if not bindings.roles[role] then errors[#errors + 1] = "missing enemy role binding " .. role end
    end
  end
  return #errors == 0, errors
end

function LoveableRogueAssets.new(options)
  options = options or {}
  local metadata, failure = LoveableRogueAssets.load_metadata(options.metadata_path)
  assert(metadata, failure and failure.reason or "Loveable Rogue metadata is unavailable")
  return setmetatable({ metadata = metadata, image = nil, quads = {}, glyphs = {}, loaded = false }, LoveableRogueAssets)
end

function LoveableRogueAssets:_build_metadata_quads()
  local atlas = self.metadata.atlas
  for id, rectangle in pairs(self.metadata.sprites) do
    self.quads[id] = love.graphics.newQuad(rectangle[1], rectangle[2], rectangle[3], rectangle[4], atlas.width, atlas.height)
  end
  local font = self.metadata.font
  for style, row in pairs(font.rows) do
    self.glyphs[style] = {}
    for index = 1, #row.characters do
      self.glyphs[style][row.characters:sub(index, index)] = love.graphics.newQuad(
        (index - 1) * font.cell_width, row.y, font.cell_width, font.cell_height, atlas.width, atlas.height
      )
    end
  end
end

function LoveableRogueAssets:load()
  if self.loaded or not love or not love.graphics then return true end
  love.graphics.setDefaultFilter("nearest", "nearest")
  local ok, image_or_error = pcall(love.graphics.newImage, self.metadata.atlas.image)
  assert(ok, "Could not load Loveable Rogue atlas: " .. tostring(image_or_error))
  self.image = image_or_error
  self.image:setFilter("nearest", "nearest")
  local width, height = self.image:getDimensions()
  assert(width == self.metadata.atlas.width and height == self.metadata.atlas.height,
    "Loveable Rogue atlas dimensions disagree with metadata")
  self:_build_metadata_quads()
  self.loaded = true
  return true
end

function LoveableRogueAssets:sprite_for_actor(actor, state)
  local bindings = self.metadata.bindings
  if actor == (state and state.player) then
    local id = state and state.expedition and state.expedition.character_id
      or actor.content_id or "actor.player.legacy"
    return assert(bindings.classes[id], "Missing Loveable Rogue class binding: " .. tostring(id))
  end
  if actor and (actor.boss or actor == (state and state.boss)) then return "boss.default" end
  if actor and actor.kind == "bullet" then return "projectile.normal" end
  if actor and actor.kind == "bomb" then return "projectile.explosive" end
  if actor and actor.kind == "flare" then return "effect.fire" end
  if actor and actor.kind == "torch" then return "effect.fire" end
  if actor and actor.kind == "fallen_echo" then return "actor.fallen_echo" end
  if actor and actor.kind == "target" then return "actor.target" end
  if actor and actor.kind == "ammo" then return "actor.ammo" end
  if actor and actor.kind == "exit" then return "actor.exit" end
  if actor and actor.kind and bindings.enemy_kinds[actor.kind] then return bindings.enemy_kinds[actor.kind] end
  if actor and actor.ai_role and bindings.roles[actor.ai_role] then return bindings.roles[actor.ai_role] end
  error("Missing Loveable Rogue actor binding: " .. tostring(actor and actor.kind or "unknown"))
end

function LoveableRogueAssets:sprite_for_object(definition, object)
  if object and object.interaction_role == "door" then
    return object.door_state == "open" and "object.door_open" or "object.door_closed"
  end
  local role = (object and object.interaction_role) or (definition and definition.interaction_role) or "default"
  return assert(self.metadata.bindings.objects[role], "Missing Loveable Rogue object binding: " .. tostring(role))
end

function LoveableRogueAssets:draw_sprite(id, x, y, size, tint, transform)
  assert(self.loaded and self.image, "Loveable Rogue atlas was not loaded")
  local quad = assert(self.quads[id], "Missing Loveable Rogue sprite: " .. tostring(id))
  local rectangle = self.metadata.sprites[id]
  local sx = (size / rectangle[3]) * ((transform and transform.scale_x) or 1)
  local sy = (size / rectangle[4]) * ((transform and transform.scale_y) or 1)
  local ox, oy = (transform and transform.offset_x) or 0, (transform and transform.offset_y) or 0
  local rotation = (transform and transform.rotation) or 0
  if tint then love.graphics.setColor(tint[1], tint[2], tint[3], tint[4] or 1) else love.graphics.setColor(1, 1, 1, 1) end
  love.graphics.draw(self.image, quad, x + ox + size * 0.5, y + oy + size * 0.5, rotation, sx, sy, rectangle[3] * 0.5, rectangle[4] * 0.5)
  love.graphics.setColor(1, 1, 1, 1)
end

function LoveableRogueAssets:draw_actor(actor, state, x, y, size, tint, transform)
  self:draw_sprite(self:sprite_for_actor(actor, state), x, y, size, tint, transform)
end

function LoveableRogueAssets:draw_text(value, x, y, scale, color)
  assert(self.loaded and self.image, "Loveable Rogue font atlas was not loaded")
  local font, numeric_scale = self.metadata.font, math.max(1, math.floor((scale or 1) + 0.5))
  local style = color_style(color)
  local glyphs = self.glyphs[style .. "_upper"] or self.glyphs.white_upper
  local symbols = self.glyphs[style .. "_symbols"] or self.glyphs.white_symbols
  local fallback = symbols[font.fallback] or self.glyphs.white_symbols[font.fallback]
  local cursor = x
  local normalized = tostring(value):upper():gsub("×", "X"):gsub("•", "."):gsub("—", "-"):gsub("→", ">")
  -- Keep unsupported UTF-8 text deterministic: one unsupported character maps
  -- to one source-font fallback glyph, never one glyph per encoded byte.
  normalized = normalized:gsub("[\128-\255]+", "?")
  love.graphics.setColor(1, 1, 1, (color and color[4]) or 1)
  for index = 1, #normalized do
    local character = normalized:sub(index, index)
    if character == " " then
      cursor = cursor + font.cell_width * numeric_scale
    else
      local quad = glyphs[character] or symbols[character] or fallback
      love.graphics.draw(self.image, quad, cursor, y, 0, numeric_scale, numeric_scale)
      cursor = cursor + font.cell_width * numeric_scale
    end
  end
  love.graphics.setColor(1, 1, 1, 1)
  return cursor - x
end

function LoveableRogueAssets:measure_text(value, scale)
  return #tostring(value) * self.metadata.font.cell_width * math.max(1, math.floor((scale or 1) + 0.5))
end

return LoveableRogueAssets
