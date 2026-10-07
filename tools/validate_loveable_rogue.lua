-- Headless visual-contract validator. It deliberately uses only Lua's PNG
-- header view: runtime artwork is loaded by LÖVE, while this tool verifies the
-- committed source/extract/metadata relationship without a graphics context.
package.path = "./?.lua;./?/init.lua;" .. package.path

local Assets = require("src.rendering.loveable_rogue_assets")

local function png_dimensions(path)
  local file = assert(io.open(path, "rb"), "missing PNG: " .. path)
  local header = file:read(24)
  file:close()
  assert(header and header:sub(1, 8) == "\137PNG\r\n\26\n", "not a PNG: " .. path)
  local function u32(offset)
    local a, b, c, d = header:byte(offset, offset + 3)
    return ((a * 256 + b) * 256 + c) * 256 + d
  end
  return u32(17), u32(21)
end

local source = "assets/art_packs/loveable_rogue.png"
local runtime = "assets/visual/loveable_rogue_atlas.png"
local metadata = assert(Assets.load_metadata())
local source_width, source_height = png_dimensions(source)
local runtime_width, runtime_height = png_dimensions(runtime)
assert(source_width == 1024 and source_height == 1024, "unexpected local Loveable Rogue source dimensions")
assert(runtime_width == metadata.atlas.width and runtime_height == metadata.atlas.height,
  "runtime atlas dimensions disagree with metadata")
assert(metadata.atlas.image == runtime, "metadata must reference the committed runtime atlas")
io.write(string.format("Loveable Rogue visuals valid: source %dx%d, runtime %dx%d, %d sprites\n",
  source_width, source_height, runtime_width, runtime_height,
  (function() local count = 0 for _ in pairs(metadata.sprites) do count = count + 1 end return count end)()))
