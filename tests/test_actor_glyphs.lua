local ActorGlyphs = require("src.rendering.actor_glyphs")
local PresentationAssets = require("src.rendering.presentation_assets")

local function optional_manifest()
  return {
    schema_version = 1,
    assets = {
      {
        id = "character.fixture", image = "assets/presentation/fixture.png",
        frame_width = 24, frame_height = 24, pivot = { 12, 21 },
        animations = {
          idle = { frames = { 0, 1 }, durations_ms = { 100, 200 } },
          move = { frames = { 2, 3 }, durations_ms = { 90, 90 } },
        },
      },
    },
    character_bindings = { ["expedition.gunner"] = "character.fixture" },
    role_bindings = { ["terrain.floor"] = "character.fixture" },
  }
end

return {
  {
    name = "ART-02 class glyphs are distinct presentation mappings without simulation shape state",
    run = function()
      local state = { expedition = {} }
      local player = { kind = "player", direction = "d" }
      state.player = player
      local shapes = {}
      for _, character_id in ipairs({ "expedition.gunner", "expedition.bruiser", "expedition.conductor", "expedition.demolitionist" }) do
        state.expedition.character_id = character_id
        local glyph = ActorGlyphs.definition(player, state)
        assert(glyph.shape and glyph.cue and glyph.direction[1] == 1)
        shapes[glyph.shape] = true
      end
      assert(shapes.circle and shapes.square and shapes.diamond and shapes.hexagon)
      assert(player.shape == nil and player.glyph == nil)
    end,
  },
  {
    name = "ART-02 enemy glyphs encode mechanical roles and preserve elite boss treatment",
    run = function()
      local state = { player = {} }
      local expected = {
        rusher = "triangle", skirmisher = "diamond", flanker = "angled", controller = "hexagon", heavy = "square",
      }
      for role, shape in pairs(expected) do
        local glyph = ActorGlyphs.definition({ ai_role = role, direction = "w" }, state)
        assert(glyph.shape == shape)
      end
      assert(ActorGlyphs.definition({ ai_role = "heavy", elite = true }, state).elite)
      assert(ActorGlyphs.definition({ boss = true }, state).shape == "boss")
    end,
  },
  {
    name = "ART-02 optional presentation metadata is source-tool agnostic and deterministic",
    run = function()
      local manifest = optional_manifest()
      assert(PresentationAssets.validate_manifest(manifest))
      local animation = manifest.assets[1].animations.idle
      assert(PresentationAssets.frame_for_elapsed(animation, 0) == 0)
      assert(PresentationAssets.frame_for_elapsed(animation, 0.10) == 1)
      assert(PresentationAssets.frame_for_elapsed(animation, 0.30) == 0)
      manifest.assets[1].animations.idle.durations_ms[2] = 0
      manifest.character_bindings["expedition.bruiser"] = "missing"
      manifest.role_bindings["object.door"] = "missing"
      local valid, errors = PresentationAssets.validate_manifest(manifest)
      assert(not valid and #errors >= 3)
    end,
  },
  {
    name = "ART-02 shape-first fallback covers projectiles and non-Expedition actors without a sprite binding",
    run = function()
      local state = { player = { kind = "player" } }
      assert(ActorGlyphs.definition({ kind = "bullet", direction = "a" }, state).shape == "projectile")
      assert(ActorGlyphs.definition({ kind = "bomb" }, state).cue == "explosive_core")
      assert(ActorGlyphs.definition({ kind = "unknown" }, state).shape == "circle")
      local runtime = PresentationAssets.new({ manifest_path = "assets/presentation/missing.json" })
      assert(runtime:draw_character("expedition.gunner", "idle", 0, 0, 0, 24) == false)
    end,
  },
}
