-- Executed by a licensed Aseprite CLI during `tools/export_art.lua`. It keeps
-- exact-pixel checks beside the source editor instead of adding a PNG library.
local function split(value, separator)
  local values = {}
  for token in tostring(value or ""):gmatch("[^" .. separator .. "]+") do values[#values + 1] = token end
  return values
end

local arguments = app.params or {}
local sprite = assert(app.activeSprite, "No active Aseprite sprite")
local expected_width, expected_height = tonumber(arguments.width), tonumber(arguments.height)
assert(sprite.width == expected_width and sprite.height == expected_height,
  string.format("canvas must be %dx%d, got %dx%d", expected_width, expected_height, sprite.width, sprite.height))

local tags = {}
for _, tag in ipairs(sprite.tags) do tags[tag.name] = true end
for _, required in ipairs(split(arguments.required_tags, ",")) do assert(tags[required], "required tag '" .. required .. "' is missing") end

local allowed = {}
for _, colour in ipairs(split(arguments.palette, ",")) do allowed[colour:upper()] = true end
for _, layer in ipairs(sprite.layers) do
  for _, cel in ipairs(layer.cels) do
    local image = cel.image
    for y = 0, image.height - 1 do
      for x = 0, image.width - 1 do
        local pixel = image:getPixel(x, y)
        local alpha = app.pixelColor.rgbaA(pixel)
        assert(alpha == 0 or alpha == 255, string.format("layer '%s' has semitransparent pixel at %d,%d", layer.name, x, y))
        if alpha == 255 then
          local colour = string.format("#%02X%02X%02X", app.pixelColor.rgbaR(pixel), app.pixelColor.rgbaG(pixel), app.pixelColor.rgbaB(pixel))
          assert(allowed[colour], string.format("layer '%s' uses colour %s outside declared palette at %d,%d", layer.name, colour, x, y))
        end
      end
    end
  end
end
print("ART_SOURCE_VALID")
