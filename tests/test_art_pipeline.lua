local AnimatedSprites = require("src.rendering.animated_sprites")
local Manifest = require("src.art.manifest")

local function production_asset()
  return {
    id = "character.fixture", kind = "character", source = "fixture.aseprite",
    runtime_sheet = "fixture.png", runtime_metadata = "fixture.json",
    native_width = 24, native_height = 24, pivot = { x = 12, y = 21 },
    required_tags = { "idle", "move", "attack", "hurt" },
  }
end

local function metadata()
  return {
    frames = {
      { frame = { x = 0, y = 0, w = 24, h = 24 }, duration = 100 },
      { frame = { x = 24, y = 0, w = 24, h = 24 }, duration = 200 },
      { frame = { x = 48, y = 0, w = 24, h = 24 }, duration = 100 },
      { frame = { x = 72, y = 0, w = 24, h = 24 }, duration = 100 },
    },
    meta = { frameTags = {
      { name = "idle", from = 0, to = 1 }, { name = "move", from = 0, to = 1 },
      { name = "attack", from = 2, to = 2 }, { name = "hurt", from = 3, to = 3 },
    } },
  }
end

return {
  {
    name = "ART-01 production Gunner manifest source exports and palette validate together",
    run = function()
      local definition = require("src.persistence.json").decode(assert(io.open("art/assets.json", "rb")):read("*a"))
      local valid, errors, warnings = Manifest.validate(definition)
      assert(valid and #errors == 0 and #warnings == 0)
      assert(#definition.assets == 1 and definition.assets[1].id == "character.gunner")
      local palette = require("src.persistence.json").decode(assert(io.open("art/palettes/roag-base.json", "rb")):read("*a"))
      assert(Manifest.validate_palette(palette))
      local metadata = require("src.persistence.json").decode(assert(io.open("assets/sprites/characters/gunner.json", "rb")):read("*a"))
      assert(Manifest.validate_metadata(definition.assets[1], metadata))
    end,
  },
  {
    name = "ART-01 manifest rejects duplicate IDs missing production exports and invalid pivots",
    run = function()
      local asset = production_asset()
      asset.pivot = { x = 24, y = 21 }
      local valid, errors = Manifest.validate({ schema_version = 1, assets = { asset, production_asset() }, planned_assets = {} }, {
        exists = function() return false end,
      })
      assert(not valid and #errors >= 4)
    end,
  },
  {
    name = "ART-01 metadata requires source tags native frames and positive timing",
    run = function()
      local asset, value = production_asset(), metadata()
      assert(Manifest.validate_metadata(asset, value))
      value.frames[2].duration = 0
      value.meta.frameTags[4].name = "attack"
      local valid, errors = Manifest.validate_metadata(asset, value)
      assert(not valid and #errors >= 2)
    end,
  },
  {
    name = "ART-01 animation frame selection is deterministic and runtime fallback has no class branch",
    run = function()
      local value = metadata()
      local asset = { frames = value.frames, tags = { idle = value.meta.frameTags[1] } }
      assert(AnimatedSprites.frame_for_elapsed(asset, "idle", 0) == 1)
      assert(AnimatedSprites.frame_for_elapsed(asset, "idle", 0.10) == 2)
      assert(AnimatedSprites.frame_for_elapsed(asset, "idle", 0.30) == 1)
      local runtime = AnimatedSprites.new()
      assert(runtime:character_asset_id("expedition.gunner") == nil)
    end,
  },
}
