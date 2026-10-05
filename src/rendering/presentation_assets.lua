-- Optional, ROAG-owned runtime metadata for future licensed PNG packs.
-- Current presentation intentionally declares no actor assets and therefore
-- always falls back to actor_glyphs.  This reader has no source-editor format
-- dependency and never participates in simulation or save data.
local Json = require("src.persistence.json")

local PresentationAssets = {}
PresentationAssets.__index = PresentationAssets

local function issue(errors, path, reason)
  errors[#errors + 1] = { path = path, reason = reason }
end

local function positive_integer(value)
  return type(value) == "number" and value % 1 == 0 and value > 0
end

function PresentationAssets.validate_manifest(manifest)
  local errors, ids = {}, {}
  if type(manifest) ~= "table" then return nil, { { path = "manifest", reason = "must be an object" } } end
  if manifest.schema_version ~= 1 then issue(errors, "schema_version", "must equal supported version 1") end
  if type(manifest.assets) ~= "table" then issue(errors, "assets", "must be an array") end
  for _, binding_name in ipairs({ "character_bindings", "role_bindings" }) do
    if manifest[binding_name] ~= nil and type(manifest[binding_name]) ~= "table" then
      issue(errors, binding_name, "must be an object")
    end
  end
  for index, asset in ipairs(manifest.assets or {}) do
    local prefix = "assets[" .. index .. "]"
    if type(asset) ~= "table" then
      issue(errors, prefix, "must be an object")
    else
      if type(asset.id) ~= "string" or asset.id == "" then issue(errors, prefix .. ".id", "must be a non-empty string")
      elseif ids[asset.id] then issue(errors, prefix .. ".id", "duplicates asset ID '" .. asset.id .. "'") else ids[asset.id] = true end
      if type(asset.image) ~= "string" or not asset.image:match("^[^/].*%.png$") or asset.image:find("..", 1, true) then
        issue(errors, prefix .. ".image", "must be a relative PNG path inside assets/presentation")
      end
      if not positive_integer(asset.frame_width) then issue(errors, prefix .. ".frame_width", "must be a positive integer") end
      if not positive_integer(asset.frame_height) then issue(errors, prefix .. ".frame_height", "must be a positive integer") end
      local pivot = asset.pivot
      local pivot_x = type(pivot) == "table" and (pivot.x or pivot[1]) or nil
      local pivot_y = type(pivot) == "table" and (pivot.y or pivot[2]) or nil
      if type(pivot_x) ~= "number" or pivot_x % 1 ~= 0 or pivot_x < 0
        or type(pivot_y) ~= "number" or pivot_y % 1 ~= 0 or pivot_y < 0 then
        issue(errors, prefix .. ".pivot", "must contain non-negative integer coordinates")
      elseif positive_integer(asset.frame_width) and positive_integer(asset.frame_height)
        and (pivot_x >= asset.frame_width or pivot_y >= asset.frame_height) then
        issue(errors, prefix .. ".pivot", "must lie inside the frame")
      end
      if type(asset.animations) ~= "table" then
        issue(errors, prefix .. ".animations", "must be an object")
      else
        for tag, animation in pairs(asset.animations) do
          local animation_path = prefix .. ".animations." .. tostring(tag)
          if type(animation) ~= "table" or type(animation.frames) ~= "table" or #animation.frames == 0 then
            issue(errors, animation_path, "must define at least one frame")
          elseif type(animation.durations_ms) ~= "table" or #animation.durations_ms ~= #animation.frames then
            issue(errors, animation_path .. ".durations_ms", "must match frame count")
          else
            for frame_index, frame in ipairs(animation.frames) do
              if type(frame) ~= "number" or frame % 1 ~= 0 or frame < 0 then issue(errors, animation_path .. ".frames[" .. frame_index .. "]", "must be a non-negative integer") end
              if not positive_integer(animation.durations_ms[frame_index]) then issue(errors, animation_path .. ".durations_ms[" .. frame_index .. "]", "must be a positive integer") end
            end
          end
        end
      end
    end
  end
  for _, binding_name in ipairs({ "character_bindings", "role_bindings" }) do
    for binding_id, asset_id in pairs(manifest[binding_name] or {}) do
      if type(binding_id) ~= "string" or type(asset_id) ~= "string" then issue(errors, binding_name, "keys and values must be strings")
      elseif not ids[asset_id] then issue(errors, binding_name .. "." .. binding_id, "references missing asset '" .. asset_id .. "'") end
    end
  end
  if #errors > 0 then return nil, errors end
  return true, errors
end

function PresentationAssets.frame_for_elapsed(animation, elapsed_seconds)
  if type(animation) ~= "table" then return nil end
  local total = 0
  for _, duration in ipairs(animation.durations_ms or {}) do total = total + duration end
  if total <= 0 then return animation.frames and animation.frames[1] end
  local elapsed = math.floor(math.max(0, elapsed_seconds or 0) * 1000) % total
  for index, duration in ipairs(animation.durations_ms) do
    elapsed = elapsed - duration
    if elapsed < 0 then return animation.frames[index] end
  end
  return animation.frames[#animation.frames]
end

function PresentationAssets.new(options)
  options = options or {}
  return setmetatable({ manifest_path = options.manifest_path or "assets/presentation/manifest.json", assets = {}, character_bindings = {}, role_bindings = {}, warnings = {} }, PresentationAssets)
end

function PresentationAssets:load()
  if not love or not love.filesystem or not love.graphics then return false end
  local contents = love.filesystem.read(self.manifest_path)
  if not contents then return false end -- absent is the normal shape-first configuration.
  local manifest, decode_error = Json.decode(contents)
  local valid, errors = PresentationAssets.validate_manifest(manifest)
  if not valid then
    self.warnings[#self.warnings + 1] = "optional presentation manifest is invalid: " .. tostring(decode_error or (errors[1] and errors[1].reason))
    return false
  end
  self.character_bindings = manifest.character_bindings or {}
  self.role_bindings = manifest.role_bindings or {}
  for _, definition in ipairs(manifest.assets) do
    local ok, image = pcall(love.graphics.newImage, definition.image)
    if ok then
      image:setFilter("nearest", "nearest")
      local width, height = image:getDimensions()
      local columns = math.floor(width / definition.frame_width)
      local rows = math.floor(height / definition.frame_height)
      local quads = {}
      for frame = 0, columns * rows - 1 do
        quads[frame] = love.graphics.newQuad((frame % columns) * definition.frame_width, math.floor(frame / columns) * definition.frame_height,
          definition.frame_width, definition.frame_height, width, height)
      end
      self.assets[definition.id] = { definition = definition, image = image, quads = quads }
    else
      self.warnings[#self.warnings + 1] = "optional presentation asset failed to load: " .. definition.id
    end
  end
  return true
end

function PresentationAssets:draw_asset(asset_id, tag, elapsed_seconds, x, y, size, tint, transform)
  local asset = self.assets[asset_id]
  if not asset then return false end
  local animation = asset.definition.animations[tag] or asset.definition.animations.idle
  local frame = PresentationAssets.frame_for_elapsed(animation, elapsed_seconds)
  local quad = frame and asset.quads[frame]
  if not quad then return false end
  transform = transform or {}
  local pivot = asset.definition.pivot
  local pivot_x, pivot_y = pivot.x or pivot[1], pivot.y or pivot[2]
  local scale_x = size / asset.definition.frame_width * (transform.scale_x or 1)
  local scale_y = size / asset.definition.frame_height * (transform.scale_y or 1)
  local color = tint or { 1, 1, 1, 1 }
  love.graphics.setColor(color[1], color[2], color[3], color[4] or 1)
  love.graphics.draw(asset.image, quad, x + size * 0.5 - pivot_x * scale_x + (transform.offset_x or 0), y + size - pivot_y * scale_y + (transform.offset_y or 0), 0, scale_x, scale_y)
  love.graphics.setColor(1, 1, 1)
  return true
end

function PresentationAssets:draw_character(character_id, tag, elapsed_seconds, x, y, size, tint, transform)
  return self:draw_asset(self.character_bindings[character_id], tag, elapsed_seconds, x, y, size, tint, transform)
end

function PresentationAssets:draw_role(role_id, x, y, size, tint, transform)
  return self:draw_asset(self.role_bindings[role_id], "idle", 0, x, y, size, tint, transform)
end

return PresentationAssets
