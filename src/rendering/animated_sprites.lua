-- Small exported-Aseprite runtime reader. It consumes only committed PNG/JSON
-- runtime assets; source .aseprite files remain a development-tool concern.
local Json = require("src.persistence.json")

local AnimatedSprites = {}
AnimatedSprites.__index = AnimatedSprites

local function sorted_assets(values)
  local result = {}
  for _, value in ipairs(values or {}) do result[#result + 1] = value end
  table.sort(result, function(a, b) return a.id < b.id end)
  return result
end

local function compile_tags(metadata)
  local result = {}
  for _, tag in ipairs(metadata.meta and metadata.meta.frameTags or {}) do result[tag.name] = tag end
  return result
end

function AnimatedSprites.new(options)
  options = options or {}
  return setmetatable({
    manifest_path = options.manifest_path or "assets/sprites/manifest.json",
    assets = {}, character_bindings = {}, warnings = {}, loaded = false,
  }, AnimatedSprites)
end

function AnimatedSprites.frame_for_elapsed(asset, tag_name, elapsed_seconds)
  local tag = asset.tags and asset.tags[tag_name]
  if not tag then return nil end
  local start, finish = tag.from + 1, tag.to + 1
  local total = 0
  for index = start, finish do total = total + (asset.frames[index].duration or 0) end
  if total <= 0 then return start end
  local elapsed = math.floor(math.max(0, elapsed_seconds or 0) * 1000) % total
  for index = start, finish do
    elapsed = elapsed - asset.frames[index].duration
    if elapsed < 0 then return index end
  end
  return finish
end

function AnimatedSprites:load()
  if not love or not love.filesystem or not love.graphics then return true end
  local contents = love.filesystem.read(self.manifest_path)
  if not contents then self.warnings[#self.warnings + 1] = "runtime sprite manifest is unavailable"; return false end
  local manifest, decode_error = Json.decode(contents)
  if not manifest or manifest.schema_version ~= 1 then
    self.warnings[#self.warnings + 1] = "runtime sprite manifest is invalid: " .. tostring(decode_error or "unsupported schema")
    return false
  end
  self.character_bindings = manifest.character_bindings or {}
  for _, definition in ipairs(sorted_assets(manifest.assets)) do
    local metadata_contents = love.filesystem.read(definition.runtime_metadata)
    local metadata = metadata_contents and Json.decode(metadata_contents)
    local image_ok, image = pcall(love.graphics.newImage, definition.runtime_sheet)
    if not metadata or not image_ok then
      self.warnings[#self.warnings + 1] = "failed to load runtime sprite '" .. tostring(definition.id) .. "'"
    else
      image:setFilter("nearest", "nearest")
      local image_width, image_height = image:getDimensions()
      local compiled = { definition = definition, image = image, frames = metadata.frames or {}, tags = compile_tags(metadata), quads = {} }
      for index, frame in ipairs(compiled.frames) do
        local rect = frame.frame
        if rect then compiled.quads[index] = love.graphics.newQuad(rect.x, rect.y, rect.w, rect.h, image_width, image_height) end
      end
      self.assets[definition.id] = compiled
    end
  end
  self.loaded = true
  return true
end

function AnimatedSprites:character_asset_id(character_id)
  return self.character_bindings and self.character_bindings[character_id] or nil
end

function AnimatedSprites:draw_character(character_id, tag_name, elapsed_seconds, x, y, size, tint, transform)
  local asset = self.assets[self:character_asset_id(character_id)]
  if not asset then return false end
  local frame_index = AnimatedSprites.frame_for_elapsed(asset, tag_name, elapsed_seconds)
    or AnimatedSprites.frame_for_elapsed(asset, "idle", elapsed_seconds)
  local quad = frame_index and asset.quads[frame_index]
  if not quad then return false end
  transform = transform or {}
  local native_width, native_height = asset.definition.native_width, asset.definition.native_height
  local pivot = asset.definition.pivot
  local scale_x = size / native_width * (transform.scale_x or 1)
  local scale_y = size / native_height * (transform.scale_y or 1)
  if tint then love.graphics.setColor(tint[1], tint[2], tint[3], tint[4] or 1) else love.graphics.setColor(1, 1, 1) end
  love.graphics.draw(asset.image, quad,
    x + size * 0.5 - pivot.x * scale_x + (transform.offset_x or 0),
    y + size - pivot.y * scale_y + (transform.offset_y or 0),
    0, scale_x, scale_y)
  love.graphics.setColor(1, 1, 1)
  return true
end

return AnimatedSprites
